#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using nadena.dev.ndmf;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Sayuki.ShapeKeyMixer.Editor
{
    /// <summary>
    /// Burstコンパイルされたデルタ加算Job。
    /// ベースのデルタ配列にソースのデルタ配列をウェイト付きで加算する。
    /// SIMD最適化 + ワーカースレッド並列実行される。
    /// </summary>
    [BurstCompile]
    internal struct MixDeltaJob : IJobParallelFor
    {
        public NativeArray<Vector3> baseVertices;
        public NativeArray<Vector3> baseNormals;
        public NativeArray<Vector3> baseTangents;

        [ReadOnly] public NativeArray<Vector3> srcVertices;
        [ReadOnly] public NativeArray<Vector3> srcNormals;
        [ReadOnly] public NativeArray<Vector3> srcTangents;

        public float weight;

        public void Execute(int i)
        {
            baseVertices[i] += srcVertices[i] * weight;
            baseNormals[i] += srcNormals[i] * weight;
            baseTangents[i] += srcTangents[i] * weight;
        }
    }

    /// <summary>
    /// ShapeKeyミックスのコアロジック（最適化版）。
    /// - Dictionary による O(1) 名前検索
    /// - NativeArray + Burst Job によるデルタ加算の SIMD + マルチスレッド化
    /// - ミックス関与BlendShapeのみ NativeArray に保存（部分抽出）
    /// </summary>
    internal static class ShapeKeyMixUtility
    {
        /// <summary>
        /// ミックス処理を実行し、メッシュのBlendShapeを再構築する。
        /// ベースShapeKeyにソースのデルタをウェイト付きで加算して上書きする。
        /// </summary>
        public static void ApplyMix(Mesh mesh, List<ShapeKeyMixEntry> mixEntries)
        {
            if (mesh == null || mixEntries == null || mixEntries.Count == 0)
                return;

            int vertexCount = mesh.vertexCount;
            int blendShapeCount = mesh.blendShapeCount;
            if (blendShapeCount == 0) return;

            // ── Step 1: 名前→インデックスの Dictionary + メタ情報 ──
            var nameToIndex = new Dictionary<string, int>(blendShapeCount);
            var metaNames = new string[blendShapeCount];
            var metaWeights = new float[blendShapeCount];
            for (int i = 0; i < blendShapeCount; i++)
            {
                metaNames[i] = mesh.GetBlendShapeName(i);
                metaWeights[i] = mesh.GetBlendShapeFrameWeight(i, 0);
                nameToIndex[metaNames[i]] = i;
            }

            // ── Step 2: ミックスに関与する全インデックスを収集 ──
            var involvedIndices = new HashSet<int>();
            foreach (var entry in mixEntries)
            {
                if (string.IsNullOrEmpty(entry.baseShapeKeyName)) continue;
                if (nameToIndex.TryGetValue(entry.baseShapeKeyName, out int baseIdx))
                    involvedIndices.Add(baseIdx);
                foreach (var source in entry.sources)
                {
                    if (string.IsNullOrEmpty(source.shapeKeyName)) continue;
                    if (nameToIndex.TryGetValue(source.shapeKeyName, out int srcIdx))
                        involvedIndices.Add(srcIdx);
                }
            }

            // ── Step 3: 全BlendShapeデータの保存 ──
            // 注意: GetBlendShapeFrameVertices / AddBlendShapeFrame は
            //       配列サイズが vertexCount と完全一致することを要求するため、
            //       ArrayPool（サイズ以上の配列を返す）は使用不可。

            // 共有バッファ（vertexCount 固定サイズ、NativeArray ↔ managed 変換用に使い回す）
            var sharedV = new Vector3[vertexCount];
            var sharedN = new Vector3[vertexCount];
            var sharedT = new Vector3[vertexCount];

            // ミックス関与 → NativeArray（Burst Jobで加算処理）
            var nativeData =
                new Dictionary<int, (NativeArray<Vector3> v, NativeArray<Vector3> n, NativeArray<Vector3> t)>(
                    involvedIndices.Count);

            // 非関与 → managed配列（そのまま再追加）
            var managedData =
                new Dictionary<int, (Vector3[] v, Vector3[] n, Vector3[] t)>(
                    blendShapeCount - involvedIndices.Count);

            try
            {
                for (int i = 0; i < blendShapeCount; i++)
                {
                    if (involvedIndices.Contains(i))
                    {
                        // NativeArrayに保存（共有バッファ経由でコピー）
                        mesh.GetBlendShapeFrameVertices(i, 0, sharedV, sharedN, sharedT);
                        var v = new NativeArray<Vector3>(vertexCount, Allocator.TempJob);
                        var n = new NativeArray<Vector3>(vertexCount, Allocator.TempJob);
                        var t = new NativeArray<Vector3>(vertexCount, Allocator.TempJob);
                        NativeArray<Vector3>.Copy(sharedV, v);
                        NativeArray<Vector3>.Copy(sharedN, n);
                        NativeArray<Vector3>.Copy(sharedT, t);
                        nativeData[i] = (v, n, t);
                    }
                    else
                    {
                        // managed配列に保存（new は vertexCount 固定なので API要件を満たす）
                        var v = new Vector3[vertexCount];
                        var n = new Vector3[vertexCount];
                        var t = new Vector3[vertexCount];
                        mesh.GetBlendShapeFrameVertices(i, 0, v, n, t);
                        managedData[i] = (v, n, t);
                    }
                }

                // ── Step 4: Burst Jobでデルタ加算 ──
                foreach (var entry in mixEntries)
                {
                    if (string.IsNullOrEmpty(entry.baseShapeKeyName)) continue;
                    if (!nameToIndex.TryGetValue(entry.baseShapeKeyName, out int baseIdx))
                    {
                        Debug.LogWarning(
                            $"[ShapeKey Mixer] ベースShapeKey '{entry.baseShapeKeyName}' が見つかりません。スキップします。");
                        continue;
                    }

                    if (!nativeData.TryGetValue(baseIdx, out var baseNd)) continue;

                    foreach (var source in entry.sources)
                    {
                        if (string.IsNullOrEmpty(source.shapeKeyName)) continue;
                        if (!nameToIndex.TryGetValue(source.shapeKeyName, out int srcIdx))
                        {
                            Debug.LogWarning(
                                $"[ShapeKey Mixer] ソースShapeKey '{source.shapeKeyName}' が見つかりません。スキップします。");
                            continue;
                        }

                        // ベースとソースが同じ場合はスキップ（NativeArray safety制約）
                        if (srcIdx == baseIdx)
                        {
                            Debug.LogWarning(
                                $"[ShapeKey Mixer] ソース '{source.shapeKeyName}' はベースと同じです。スキップします。");
                            continue;
                        }

                        if (!nativeData.TryGetValue(srcIdx, out var srcNd)) continue;

                        var job = new MixDeltaJob
                        {
                            baseVertices = baseNd.v,
                            baseNormals = baseNd.n,
                            baseTangents = baseNd.t,
                            srcVertices = srcNd.v,
                            srcNormals = srcNd.n,
                            srcTangents = srcNd.t,
                            weight = source.weight,
                        };
                        job.Schedule(vertexCount, 2048).Complete();
                    }
                }

                // ── Step 5-6: ClearBlendShapes + 再構築 ──
                mesh.ClearBlendShapes();

                for (int i = 0; i < blendShapeCount; i++)
                {
                    if (nativeData.TryGetValue(i, out var nd))
                    {
                        // ミックス関与: NativeArray → 共有バッファ → AddBlendShapeFrame
                        nd.v.CopyTo(sharedV);
                        nd.n.CopyTo(sharedN);
                        nd.t.CopyTo(sharedT);
                        mesh.AddBlendShapeFrame(metaNames[i], metaWeights[i], sharedV, sharedN, sharedT);
                    }
                    else if (managedData.TryGetValue(i, out var md))
                    {
                        // 非関与: 保存済み配列 → AddBlendShapeFrame（サイズ一致保証）
                        mesh.AddBlendShapeFrame(metaNames[i], metaWeights[i], md.v, md.n, md.t);
                    }
                }
            }
            finally
            {
                // ── Step 7: Cleanup ──
                foreach (var kv in nativeData)
                {
                    if (kv.Value.v.IsCreated) kv.Value.v.Dispose();
                    if (kv.Value.n.IsCreated) kv.Value.n.Dispose();
                    if (kv.Value.t.IsCreated) kv.Value.t.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// NDMFビルドパス。アバター内の全 ShapeKeyMixerComponent を処理する。
    /// </summary>
    public class ShapeKeyMixerPass : Pass<ShapeKeyMixerPass>
    {
        public override string DisplayName => "ShapeKey Mixer";

        protected override void Execute(BuildContext context)
        {
            var components = context.AvatarRootObject
                .GetComponentsInChildren<ShapeKeyMixerComponent>(true);

            if (components.Length == 0)
                return;

            foreach (var comp in components)
            {
                ProcessComponent(context, comp);
                UnityEngine.Object.DestroyImmediate(comp);
            }
        }

        private void ProcessComponent(BuildContext context, ShapeKeyMixerComponent comp)
        {
            var smr = comp.GetComponent<SkinnedMeshRenderer>();
            if (smr == null || smr.sharedMesh == null)
            {
                Debug.LogWarning(
                    $"[ShapeKey Mixer] '{comp.gameObject.name}' にSkinnedMeshRendererまたはMeshがありません。スキップします。");
                return;
            }

            if (comp.mixEntries == null || comp.mixEntries.Count == 0)
                return;

            // Meshの複製（非破壊）
            Mesh mesh;
            if (!context.IsTemporaryAsset(smr.sharedMesh))
            {
                mesh = UnityEngine.Object.Instantiate(smr.sharedMesh);
                mesh.name = smr.sharedMesh.name;
            }
            else
            {
                mesh = smr.sharedMesh;
            }

            // ミックス適用
            ShapeKeyMixUtility.ApplyMix(mesh, comp.mixEntries);

            // SMRに反映
            smr.sharedMesh = mesh;
        }
    }
}
#endif
