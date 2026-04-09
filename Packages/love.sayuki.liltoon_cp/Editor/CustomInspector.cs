#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace lilToon
{
    public class ChannelPackedMasksInspector : lilToonInspector
    {
        // Shader name prefix (must match ShaderName in lilCustomShaderDatas.lilblock)
        private const string shaderName = "Sayuki/ChannelPackedMasks";

        // Channel selection properties
        MaterialProperty cpmEmission1stChannel;
        MaterialProperty cpmEmission2ndChannel;
        MaterialProperty cpmGlitterChannel;
        MaterialProperty cpmMatCapChannel;
        MaterialProperty cpmMatCap2ndChannel;
        MaterialProperty cpmRimLightChannel;
        MaterialProperty cpmRimShadeChannel;
        MaterialProperty cpmBacklightChannel;
        MaterialProperty cpmAlphaMaskChannel;

        // Channel display names
        private static readonly string[] channelNames = { "R", "G", "B", "A" };
        private static readonly string[] channelNamesWithRGB = { "R", "G", "B", "A", "RGB" };

        // Foldout state
        private static bool isShowChannelPackedInfo;

        // Load custom properties (called by lilToonInspector)
        protected override void LoadCustomProperties(MaterialProperty[] props, Material material)
        {
            isCustomShader = true;
            isShowRenderMode = true;
            ReplaceToCustomShaders();

            cpmEmission1stChannel = FindProperty("_CPM_Emission1stChannel", props, false);
            cpmEmission2ndChannel = FindProperty("_CPM_Emission2ndChannel", props, false);
            cpmGlitterChannel     = FindProperty("_CPM_GlitterChannel", props, false);
            cpmMatCapChannel      = FindProperty("_CPM_MatCapChannel", props, false);
            cpmMatCap2ndChannel   = FindProperty("_CPM_MatCap2ndChannel", props, false);
            cpmRimLightChannel    = FindProperty("_CPM_RimLightChannel", props, false);
            cpmRimShadeChannel    = FindProperty("_CPM_RimShadeChannel", props, false);
            cpmBacklightChannel   = FindProperty("_CPM_BacklightChannel", props, false);
            cpmAlphaMaskChannel   = FindProperty("_CPM_AlphaMaskChannel", props, false);
        }

        // Override shader variant resolution for rendering mode switching.
        // Without this, changing Rendering Mode replaces the shader with standard lilToon.
        protected override void ReplaceToCustomShaders()
        {
            lts         = Shader.Find(shaderName + "/lilToon");
            ltsc        = Shader.Find("Hidden/" + shaderName + "/Cutout");
            ltst        = Shader.Find("Hidden/" + shaderName + "/Transparent");
            ltsot       = Shader.Find("Hidden/" + shaderName + "/OnePassTransparent");
            ltstt       = Shader.Find("Hidden/" + shaderName + "/TwoPassTransparent");

            ltso        = Shader.Find("Hidden/" + shaderName + "/OpaqueOutline");
            ltsco       = Shader.Find("Hidden/" + shaderName + "/CutoutOutline");
            ltsto       = Shader.Find("Hidden/" + shaderName + "/TransparentOutline");
            ltsoto      = Shader.Find("Hidden/" + shaderName + "/OnePassTransparentOutline");
            ltstto      = Shader.Find("Hidden/" + shaderName + "/TwoPassTransparentOutline");

            ltsoo       = Shader.Find(shaderName + "/[Optional] OutlineOnly/Opaque");
            ltscoo      = Shader.Find(shaderName + "/[Optional] OutlineOnly/Cutout");
            ltstoo      = Shader.Find(shaderName + "/[Optional] OutlineOnly/Transparent");

            ltstess     = Shader.Find("Hidden/" + shaderName + "/Tessellation/Opaque");
            ltstessc    = Shader.Find("Hidden/" + shaderName + "/Tessellation/Cutout");
            ltstesst    = Shader.Find("Hidden/" + shaderName + "/Tessellation/Transparent");
            ltstessot   = Shader.Find("Hidden/" + shaderName + "/Tessellation/OnePassTransparent");
            ltstesstt   = Shader.Find("Hidden/" + shaderName + "/Tessellation/TwoPassTransparent");

            ltstesso    = Shader.Find("Hidden/" + shaderName + "/Tessellation/OpaqueOutline");
            ltstessco   = Shader.Find("Hidden/" + shaderName + "/Tessellation/CutoutOutline");
            ltstessto   = Shader.Find("Hidden/" + shaderName + "/Tessellation/TransparentOutline");
            ltstessoto  = Shader.Find("Hidden/" + shaderName + "/Tessellation/OnePassTransparentOutline");
            ltstesstto  = Shader.Find("Hidden/" + shaderName + "/Tessellation/TwoPassTransparentOutline");

            ltsref      = Shader.Find("Hidden/" + shaderName + "/Refraction");
            ltsrefb     = Shader.Find("Hidden/" + shaderName + "/RefractionBlur");
            ltsfur      = Shader.Find("Hidden/" + shaderName + "/Fur");
            ltsfurc     = Shader.Find("Hidden/" + shaderName + "/FurCutout");
            ltsfurtwo   = Shader.Find("Hidden/" + shaderName + "/FurTwoPass");
            ltsfuro     = Shader.Find(shaderName + "/[Optional] FurOnly/Transparent");
            ltsfuroc    = Shader.Find(shaderName + "/[Optional] FurOnly/Cutout");
            ltsfurotwo  = Shader.Find(shaderName + "/[Optional] FurOnly/TwoPass");

            ltsgem      = Shader.Find("Hidden/" + shaderName + "/Gem");

            ltsfs       = Shader.Find(shaderName + "/[Optional] FakeShadow");

            ltsover     = Shader.Find(shaderName + "/[Optional] Overlay");
            ltsoover    = Shader.Find(shaderName + "/[Optional] OverlayOnePass");

            ltsm        = Shader.Find(shaderName + "/lilToonMulti");
            ltsmo       = Shader.Find("Hidden/" + shaderName + "/MultiOutline");
            ltsmref     = Shader.Find("Hidden/" + shaderName + "/MultiRefraction");
            ltsmfur     = Shader.Find("Hidden/" + shaderName + "/MultiFur");
            ltsmgem     = Shader.Find("Hidden/" + shaderName + "/MultiGem");

            ltspo       = Shader.Find("Hidden/" + shaderName + "/ltspass_opaque");
            ltspc       = Shader.Find("Hidden/" + shaderName + "/ltspass_cutout");
            ltspt       = Shader.Find("Hidden/" + shaderName + "/ltspass_transparent");
            ltsptesso   = Shader.Find("Hidden/" + shaderName + "/ltspass_tess_opaque");
            ltsptessc   = Shader.Find("Hidden/" + shaderName + "/ltspass_tess_cutout");
            ltsptesst   = Shader.Find("Hidden/" + shaderName + "/ltspass_tess_transparent");
        }

        // Draw custom GUI (called by lilToonInspector)
        protected override void DrawCustomProperties(Material material)
        {
            isShowChannelPackedInfo = Foldout("Channel Packed Masks", "Channel Packed Masks", isShowChannelPackedInfo);
            if (isShowChannelPackedInfo)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.LabelField("Channel Packed Masks", customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);

                EditorGUILayout.HelpBox(
                    "チャンネルパッキング マスク\n" +
                    "各マスクテクスチャから読み取るチャンネルを選択できます。\n" +
                    "R/G/B/A: 選択チャンネルのスカラー値のみをマスクとして使用（色適用なし）\n" +
                    "RGB: デフォルト動作（テクスチャの色がそのまま適用されます）",
                    MessageType.Info
                );

                EditorGUILayout.Space();

                DrawChannelPopup(cpmEmission1stChannel, "発光 1st マスクチャンネル", "_EmissionBlendMask", true);
                DrawChannelPopup(cpmEmission2ndChannel, "発光 2nd マスクチャンネル", "_Emission2ndBlendMask", true);
                DrawChannelPopup(cpmGlitterChannel,     "ラメ マスクチャンネル", "_GlitterColorTex", true);
                DrawChannelPopup(cpmMatCapChannel,      "マットキャップ 1st マスクチャンネル", "_MatCapBlendMask", true);
                DrawChannelPopup(cpmMatCap2ndChannel,   "マットキャップ 2nd マスクチャンネル", "_MatCap2ndBlendMask", true);
                DrawChannelPopup(cpmRimLightChannel,    "リムライト マスクチャンネル", "_RimColorTex", true);
                DrawChannelPopup(cpmRimShadeChannel,    "リムシェード マスクチャンネル", "_RimShadeMask", false);
                DrawChannelPopup(cpmBacklightChannel,   "バックライト マスクチャンネル", "_BacklightColorTex", true);
                DrawChannelPopup(cpmAlphaMaskChannel,    "アルファマスク チャンネル", "_AlphaMask", false);

                EditorGUILayout.Space();

                if (GUILayout.Button("マスク変換ウィンドウを開く"))
                {
                    ChannelPackingWindow.ShowWindow();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawChannelPopup(MaterialProperty prop, string label, string textureName, bool hasRGB)
        {
            if (prop == null) return;

            string[] names = hasRGB ? channelNamesWithRGB : channelNames;
            int maxVal = hasRGB ? 4 : 3;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.showMixedValue = prop.hasMixedValue;

            EditorGUI.BeginChangeCheck();
            int channel = Mathf.Clamp((int)prop.floatValue, 0, maxVal);
            channel = EditorGUILayout.Popup(label, channel, names);
            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = channel;
            }

            EditorGUI.showMixedValue = false;
            EditorGUILayout.LabelField(textureName, EditorStyles.miniLabel, GUILayout.Width(160));
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
