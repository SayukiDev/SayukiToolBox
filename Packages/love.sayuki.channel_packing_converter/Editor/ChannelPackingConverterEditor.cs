#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Sayuki.ChannelPackingConverter.Editor
{
    /// <summary>
    /// ChannelPackingConverterComponent のカスタムInspector。
    /// </summary>
    [CustomEditor(typeof(ChannelPackingConverterComponent))]
    public class ChannelPackingConverterEditor : UnityEditor.Editor
    {
        private SerializedProperty excludedMaterialsProp;
        private SerializedProperty maxPackedTextureSizeProp;
        private ReorderableList excludedMaterialsList;

        // サイズ上限の選択肢
        private static readonly int[] sizeOptions = { 0, 256, 512, 1024, 2048, 4096 };
        private static readonly string[] sizeLabels = { "無制限", "256", "512", "1024", "2048", "4096" };

        // プレビュー用
        private bool showPreview = false;

        // マスクプロパティ名
        private static readonly string[] MaskPropertyNames =
        {
            "_EmissionBlendMask",
            "_Emission2ndBlendMask",
            "_GlitterColorTex",
            "_MatCapBlendMask",
            "_MatCap2ndBlendMask",
            "_RimColorTex",
            "_RimShadeMask",
            "_BacklightColorTex",
            "_AlphaMask",
        };

        // LilToon標準シェーダー名（ChannelPackingPassと同じリスト）
        private static readonly HashSet<string> StandardLilToonShaders = new()
        {
            "lilToon",
            "Hidden/lilToonCutout",
            "Hidden/lilToonTransparent",
            "Hidden/lilToonOnePassTransparent",
            "Hidden/lilToonTwoPassTransparent",
            "Hidden/lilToonOutline",
            "Hidden/lilToonCutoutOutline",
            "Hidden/lilToonTransparentOutline",
            "Hidden/lilToonOnePassTransparentOutline",
            "Hidden/lilToonTwoPassTransparentOutline",
            "_lil/[Optional] lilToonOutlineOnly",
            "_lil/[Optional] lilToonOutlineOnlyCutout",
            "_lil/[Optional] lilToonOutlineOnlyTransparent",
            "Hidden/lilToonTessellation",
            "Hidden/lilToonTessellationCutout",
            "Hidden/lilToonTessellationTransparent",
            "Hidden/lilToonTessellationOnePassTransparent",
            "Hidden/lilToonTessellationTwoPassTransparent",
            "Hidden/lilToonTessellationOutline",
            "Hidden/lilToonTessellationCutoutOutline",
            "Hidden/lilToonTessellationTransparentOutline",
            "Hidden/lilToonTessellationOnePassTransparentOutline",
            "Hidden/lilToonTessellationTwoPassTransparentOutline",
            "Hidden/lilToonRefraction",
            "Hidden/lilToonRefractionBlur",
            "Hidden/lilToonFur",
            "Hidden/lilToonFurCutout",
            "Hidden/lilToonFurTwoPass",
            "_lil/[Optional] lilToonFurOnlyTransparent",
            "_lil/[Optional] lilToonFurOnlyCutout",
            "_lil/[Optional] lilToonFurOnlyTwoPass",
            "Hidden/lilToonGem",
            "_lil/[Optional] lilToonFakeShadow",
            "_lil/[Optional] lilToonOverlay",
            "_lil/[Optional] lilToonOverlayOnePass",
            "_lil/lilToonMulti",
            "Hidden/lilToonMultiOutline",
            "Hidden/lilToonMultiRefraction",
            "Hidden/lilToonMultiFur",
            "Hidden/lilToonMultiGem",
        };

        private void OnEnable()
        {
            excludedMaterialsProp = serializedObject.FindProperty("excludedMaterials");
            maxPackedTextureSizeProp = serializedObject.FindProperty("maxPackedTextureSize");

            excludedMaterialsList = new ReorderableList(
                serializedObject, excludedMaterialsProp,
                true, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, "除外マテリアル");
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = excludedMaterialsProp.GetArrayElementAtIndex(index);
                    rect.y += 2;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(rect, element, GUIContent.none);
                },
                elementHeight = EditorGUIUtility.singleLineHeight + 4
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Channel Packing Converter", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "NDMFビルド時にLilToon標準マテリアルのマスクテクスチャを\n" +
                "Channel PackingしてChannel Packing Shaderへ自動変換します。\n" +
                "LilToon以外やカスタムシェーダーはスキップされます。",
                MessageType.Info);

            EditorGUILayout.Space();

            // サイズ上限
            int currentSize = maxPackedTextureSizeProp.intValue;
            int selectedIndex = System.Array.IndexOf(sizeOptions, currentSize);
            if (selectedIndex < 0) selectedIndex = 0;

            selectedIndex = EditorGUILayout.Popup(
                "マスクサイズ上限",
                selectedIndex,
                sizeLabels);

            if (selectedIndex >= 0 && selectedIndex < sizeOptions.Length)
            {
                maxPackedTextureSizeProp.intValue = sizeOptions[selectedIndex];
            }

            EditorGUILayout.Space();

            // 除外マテリアルリスト
            excludedMaterialsList.DoLayoutList();

            EditorGUILayout.Space();

            // プレビュー
            showPreview = EditorGUILayout.Foldout(showPreview, "変換プレビュー", true);
            if (showPreview)
            {
                DrawPreview();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPreview()
        {
            var comp = (ChannelPackingConverterComponent)target;
            var root = comp.gameObject;

            // 除外リスト
            var excluded = new HashSet<Material>(
                comp.excludedMaterials.Where(m => m != null));

            // マテリアル走査
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var targetMaterials = new HashSet<Material>();

            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null) continue;
                    if (excluded.Contains(mat)) continue;
                    if (mat.shader == null) continue;
                    if (!StandardLilToonShaders.Contains(mat.shader.name)) continue;
                    targetMaterials.Add(mat);
                }
            }

            if (targetMaterials.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "対象となるLilToon標準マテリアルが見つかりませんでした。",
                    MessageType.Warning);
                return;
            }

            // マスク数カウント
            int totalMasks = 0;
            var uniqueTextures = new HashSet<Texture2D>();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"対象マテリアル: {targetMaterials.Count}件", EditorStyles.boldLabel);

            foreach (var mat in targetMaterials)
            {
                int maskCount = 0;
                foreach (var propName in MaskPropertyNames)
                {
                    if (!mat.HasProperty(propName)) continue;
                    var tex = mat.GetTexture(propName) as Texture2D;
                    if (tex == null) continue;
                    maskCount++;
                    uniqueTextures.Add(tex);
                }

                EditorGUILayout.LabelField($"  {mat.name}", $"マスク {maskCount}枚");
                totalMasks += maskCount;
            }

            EditorGUILayout.Space();
            int uniqueCount = uniqueTextures.Count;
            string packMode = (uniqueCount % 3 == 0) ? "RGB (3枚/グループ)" : "RGBA (4枚/グループ)";
            int groupCount = 0;
            int remaining = uniqueCount;
            while (remaining > 0)
            {
                int gs = (remaining % 3 == 0) ? 3 : System.Math.Min(4, remaining);
                remaining -= gs;
                groupCount++;
            }

            EditorGUILayout.LabelField($"ユニークマスク: {uniqueCount}枚");
            EditorGUILayout.LabelField($"生成PackedTexture: {groupCount}枚 ({packMode})");

            EditorGUILayout.EndVertical();
        }
    }
}
#endif
