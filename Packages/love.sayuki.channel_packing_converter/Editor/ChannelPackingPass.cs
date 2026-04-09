#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace Sayuki.ChannelPackingConverter.Editor
{
    /// <summary>
    /// メイン変換パス。LilToon標準マテリアルのマスクをChannel PackingしてCPSに変換する。
    /// </summary>
    public class ChannelPackingPass : Pass<ChannelPackingPass>
    {
        public override string DisplayName => "Channel Packing Pass";

        // CPS シェーダー名プレフィックス
        private const string CpsPrefix = "Sayuki/ChannelPackedMasks";

        // Channel Packing可能なマスクプロパティ名と対応する _CPM_*Channel プロパティ名
        private static readonly (string maskProp, string channelProp)[] MaskProperties =
        {
            ("_EmissionBlendMask",    "_CPM_Emission1stChannel"),
            ("_Emission2ndBlendMask", "_CPM_Emission2ndChannel"),
            ("_GlitterColorTex",     "_CPM_GlitterChannel"),
            ("_MatCapBlendMask",     "_CPM_MatCapChannel"),
            ("_MatCap2ndBlendMask",  "_CPM_MatCap2ndChannel"),
            ("_RimColorTex",         "_CPM_RimLightChannel"),
            ("_RimShadeMask",        "_CPM_RimShadeChannel"),
            ("_BacklightColorTex",   "_CPM_BacklightChannel"),
            ("_AlphaMask",           "_CPM_AlphaMaskChannel"),
        };

        // LilToon標準シェーダー名 → CPS シェーダー名 の対応表
        // lilShaderManager.cs (jp.lilxyzw.liltoon) の定義に基づく
        private static readonly Dictionary<string, string> ShaderVariantMap = new()
        {
            // --- 基本バリアント ---
            { "lilToon",                                                CpsPrefix + "/lilToon" },
            { "Hidden/lilToonCutout",                                   "Hidden/" + CpsPrefix + "/Cutout" },
            { "Hidden/lilToonTransparent",                              "Hidden/" + CpsPrefix + "/Transparent" },
            { "Hidden/lilToonOnePassTransparent",                       "Hidden/" + CpsPrefix + "/OnePassTransparent" },
            { "Hidden/lilToonTwoPassTransparent",                       "Hidden/" + CpsPrefix + "/TwoPassTransparent" },
            // --- Outline バリアント ---
            { "Hidden/lilToonOutline",                                  "Hidden/" + CpsPrefix + "/OpaqueOutline" },
            { "Hidden/lilToonCutoutOutline",                            "Hidden/" + CpsPrefix + "/CutoutOutline" },
            { "Hidden/lilToonTransparentOutline",                       "Hidden/" + CpsPrefix + "/TransparentOutline" },
            { "Hidden/lilToonOnePassTransparentOutline",                "Hidden/" + CpsPrefix + "/OnePassTransparentOutline" },
            { "Hidden/lilToonTwoPassTransparentOutline",                "Hidden/" + CpsPrefix + "/TwoPassTransparentOutline" },
            // --- OutlineOnly ---
            { "_lil/[Optional] lilToonOutlineOnly",                     CpsPrefix + "/[Optional] OutlineOnly/Opaque" },
            { "_lil/[Optional] lilToonOutlineOnlyCutout",               CpsPrefix + "/[Optional] OutlineOnly/Cutout" },
            { "_lil/[Optional] lilToonOutlineOnlyTransparent",          CpsPrefix + "/[Optional] OutlineOnly/Transparent" },
            // --- Tessellation ---
            { "Hidden/lilToonTessellation",                             "Hidden/" + CpsPrefix + "/Tessellation/Opaque" },
            { "Hidden/lilToonTessellationCutout",                       "Hidden/" + CpsPrefix + "/Tessellation/Cutout" },
            { "Hidden/lilToonTessellationTransparent",                  "Hidden/" + CpsPrefix + "/Tessellation/Transparent" },
            { "Hidden/lilToonTessellationOnePassTransparent",           "Hidden/" + CpsPrefix + "/Tessellation/OnePassTransparent" },
            { "Hidden/lilToonTessellationTwoPassTransparent",           "Hidden/" + CpsPrefix + "/Tessellation/TwoPassTransparent" },
            { "Hidden/lilToonTessellationOutline",                      "Hidden/" + CpsPrefix + "/Tessellation/OpaqueOutline" },
            { "Hidden/lilToonTessellationCutoutOutline",                "Hidden/" + CpsPrefix + "/Tessellation/CutoutOutline" },
            { "Hidden/lilToonTessellationTransparentOutline",           "Hidden/" + CpsPrefix + "/Tessellation/TransparentOutline" },
            { "Hidden/lilToonTessellationOnePassTransparentOutline",    "Hidden/" + CpsPrefix + "/Tessellation/OnePassTransparentOutline" },
            { "Hidden/lilToonTessellationTwoPassTransparentOutline",    "Hidden/" + CpsPrefix + "/Tessellation/TwoPassTransparentOutline" },
            // --- Refraction ---
            { "Hidden/lilToonRefraction",                               "Hidden/" + CpsPrefix + "/Refraction" },
            { "Hidden/lilToonRefractionBlur",                           "Hidden/" + CpsPrefix + "/RefractionBlur" },
            // --- Fur ---
            { "Hidden/lilToonFur",                                      "Hidden/" + CpsPrefix + "/Fur" },
            { "Hidden/lilToonFurCutout",                                "Hidden/" + CpsPrefix + "/FurCutout" },
            { "Hidden/lilToonFurTwoPass",                               "Hidden/" + CpsPrefix + "/FurTwoPass" },
            { "_lil/[Optional] lilToonFurOnlyTransparent",              CpsPrefix + "/[Optional] FurOnly/Transparent" },
            { "_lil/[Optional] lilToonFurOnlyCutout",                   CpsPrefix + "/[Optional] FurOnly/Cutout" },
            { "_lil/[Optional] lilToonFurOnlyTwoPass",                  CpsPrefix + "/[Optional] FurOnly/TwoPass" },
            // --- Gem ---
            { "Hidden/lilToonGem",                                      "Hidden/" + CpsPrefix + "/Gem" },
            // --- FakeShadow ---
            { "_lil/[Optional] lilToonFakeShadow",                      CpsPrefix + "/[Optional] FakeShadow" },
            // --- Overlay ---
            { "_lil/[Optional] lilToonOverlay",                         CpsPrefix + "/[Optional] Overlay" },
            { "_lil/[Optional] lilToonOverlayOnePass",                  CpsPrefix + "/[Optional] OverlayOnePass" },
            // --- Multi ---
            { "_lil/lilToonMulti",                                      CpsPrefix + "/lilToonMulti" },
            { "Hidden/lilToonMultiOutline",                             "Hidden/" + CpsPrefix + "/MultiOutline" },
            { "Hidden/lilToonMultiRefraction",                          "Hidden/" + CpsPrefix + "/MultiRefraction" },
            { "Hidden/lilToonMultiFur",                                 "Hidden/" + CpsPrefix + "/MultiFur" },
            { "Hidden/lilToonMultiGem",                                 "Hidden/" + CpsPrefix + "/MultiGem" },
        };

        protected override void Execute(BuildContext context)
        {
            // 1. コンポーネント取得
            var component = context.AvatarRootObject
                .GetComponentInChildren<ChannelPackingConverterComponent>(true);
            if (component == null) return;

            var excludedMaterials = new HashSet<Material>(
                component.excludedMaterials.Where(m => m != null));
            int maxSize = component.maxPackedTextureSize;

            // 2. マテリアル収集 — LilToon標準のみ、除外リスト適用
            var renderers = context.AvatarRootObject
                .GetComponentsInChildren<Renderer>(true);

            // Renderer → マテリアルの対応を保持
            var targetMaterials = new HashSet<Material>();
            var rendererMaterialPairs = new List<(Renderer renderer, int slotIndex, Material material)>();

            foreach (var renderer in renderers)
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null) continue;
                    if (excludedMaterials.Contains(mat)) continue;
                    if (!IsStandardLilToon(mat)) continue;

                    targetMaterials.Add(mat);
                    rendererMaterialPairs.Add((renderer, i, mat));
                }
            }

            if (targetMaterials.Count == 0)
            {
                Object.DestroyImmediate(component);
                return;
            }

            // 3. 全マテリアル横断マスク収集
            var uniqueMasks = new Dictionary<Texture2D, MaskPackingUtility.MaskInfo>();
            var materialMasks = new Dictionary<Material, List<(string maskProp, string channelProp, Texture2D tex)>>();

            foreach (var mat in targetMaterials)
            {
                var maskList = new List<(string maskProp, string channelProp, Texture2D tex)>();

                foreach (var (maskProp, channelProp) in MaskProperties)
                {
                    if (!mat.HasProperty(maskProp)) continue;
                    var tex = mat.GetTexture(maskProp) as Texture2D;
                    if (tex == null) continue;

                    maskList.Add((maskProp, channelProp, tex));

                    if (!uniqueMasks.ContainsKey(tex))
                    {
                        uniqueMasks[tex] = new MaskPackingUtility.MaskInfo
                        {
                            Texture = tex,
                            PropertyName = maskProp
                        };
                    }
                }

                if (maskList.Count > 0)
                    materialMasks[mat] = maskList;
            }

            if (uniqueMasks.Count == 0)
            {
                Object.DestroyImmediate(component);
                return;
            }

            // 4. 解像度降順ソート
            var sortedMasks = uniqueMasks.Values
                .OrderByDescending(m => m.Resolution)
                .ToList();

            // 5-6. グループ化 & Packing
            var packedResults = MaskPackingUtility.PackMasks(sortedMasks, maxSize);

            // マスクテクスチャ → (PackedTexture, channel) のマッピングを構築
            var maskMapping = new Dictionary<Texture2D, (Texture2D packed, int channel)>();
            foreach (var result in packedResults)
            {
                for (int ch = 0; ch < result.Masks.Length; ch++)
                {
                    if (result.Masks[ch] != null)
                    {
                        maskMapping[result.Masks[ch].Texture] = (result.PackedTexture, ch);
                    }
                }
            }

            // 7. シェーダー変換
            // マテリアルのクローン管理
            var materialClones = new Dictionary<Material, Material>();

            foreach (var (renderer, slotIndex, originalMat) in rendererMaterialPairs)
            {
                if (!materialMasks.ContainsKey(originalMat)) continue;

                // マテリアルクローン (同一マテリアルは一度だけクローン)
                if (!materialClones.TryGetValue(originalMat, out var clonedMat))
                {
                    if (!context.IsTemporaryAsset(originalMat))
                    {
                        clonedMat = Object.Instantiate(originalMat);
                    }
                    else
                    {
                        clonedMat = originalMat;
                    }

                    // シェーダー変換
                    var originalShaderName = originalMat.shader.name;
                    if (ShaderVariantMap.TryGetValue(originalShaderName, out var cpsShaderName))
                    {
                        var cpsShader = Shader.Find(cpsShaderName);
                        if (cpsShader != null)
                        {
                            clonedMat.shader = cpsShader;
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"[ChannelPackingConverter] CPS shader '{cpsShaderName}' not found. " +
                                $"Skipping material '{originalMat.name}'.");
                            continue;
                        }
                    }
                    else
                    {
                        // 対応表にない場合はスキップ (ここには来ないはず)
                        continue;
                    }

                    // マスクスロットにPackedテクスチャを設定
                    var masksForMat = materialMasks[originalMat];

                    foreach (var (maskProp, channelProp, tex) in masksForMat)
                    {
                        if (!maskMapping.TryGetValue(tex, out var mapping)) continue;

                        // マスクスロットにPackedTextureを設定
                        clonedMat.SetTexture(maskProp, mapping.packed);

                        // チャンネル番号設定
                        if (clonedMat.HasProperty(channelProp))
                        {
                            clonedMat.SetInt(channelProp, mapping.channel);
                        }
                    }

                    // Packing対象外のマスクはデフォルト(4=RGB)に設定
                    foreach (var (maskProp, channelProp) in MaskProperties)
                    {
                        if (!clonedMat.HasProperty(channelProp)) continue;

                        bool isPackedMask = masksForMat.Any(m => m.maskProp == maskProp);
                        if (!isPackedMask)
                        {
                            clonedMat.SetInt(channelProp, 4); // RGB default
                        }
                    }

                    materialClones[originalMat] = clonedMat;
                }

                // Rendererにクローンマテリアルを設定
                var currentMats = renderer.sharedMaterials;
                if (slotIndex < currentMats.Length)
                {
                    currentMats[slotIndex] = materialClones[originalMat];
                    renderer.sharedMaterials = currentMats;
                }
            }

            // 8. コンポーネント削除
            Object.DestroyImmediate(component);
        }

        /// <summary>
        /// マテリアルがLilToon標準シェーダーかどうかを判定する。
        /// カスタムシェーダーやLilToon以外のシェーダーはfalseを返す。
        /// </summary>
        private static bool IsStandardLilToon(Material material)
        {
            if (material == null || material.shader == null) return false;
            return ShaderVariantMap.ContainsKey(material.shader.name);
        }
    }
}
#endif
