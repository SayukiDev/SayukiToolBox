#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace lilToon
{
    public class ChannelPackingWindow : EditorWindow
    {
        private Texture2D emissionMask;
        private Texture2D glitterMask;
        private Texture2D matCap2ndMask;
        private Texture2D emission2ndMask;

        [MenuItem("Tools/lilToon/Channel Packing マスク変換")]
        public static void ShowWindow()
        {
            var window = GetWindow<ChannelPackingWindow>("Channel Packing マスク変換");
            window.minSize = new Vector2(400, 320);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Channel Packing マスク変換", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "各チャンネルにマスクテクスチャを指定し、1枚のチャンネルパッキングテクスチャに変換します。",
                MessageType.Info
            );
            EditorGUILayout.Space();

            emissionMask    = (Texture2D)EditorGUILayout.ObjectField("R チャンネル", emissionMask,    typeof(Texture2D), false);
            glitterMask     = (Texture2D)EditorGUILayout.ObjectField("G チャンネル", glitterMask,     typeof(Texture2D), false);
            matCap2ndMask   = (Texture2D)EditorGUILayout.ObjectField("B チャンネル", matCap2ndMask,   typeof(Texture2D), false);
            emission2ndMask = (Texture2D)EditorGUILayout.ObjectField("A チャンネル", emission2ndMask, typeof(Texture2D), false);

            EditorGUILayout.Space();

            // Validate: at least one mask should be set
            bool hasAnyMask = emissionMask != null || glitterMask != null || matCap2ndMask != null || emission2ndMask != null;
            EditorGUI.BeginDisabledGroup(!hasAnyMask);
            if (GUILayout.Button("変換してパックテクスチャを保存", GUILayout.Height(32)))
            {
                PackAndSave();
            }
            EditorGUI.EndDisabledGroup();

            if (!hasAnyMask)
            {
                EditorGUILayout.HelpBox("少なくとも1つのマスクを指定してください。", MessageType.Warning);
            }
        }

        private void PackAndSave()
        {
            // Determine output resolution (use the largest texture dimensions)
            int width = 1;
            int height = 1;
            GetMaxSize(emissionMask, ref width, ref height);
            GetMaxSize(glitterMask, ref width, ref height);
            GetMaxSize(matCap2ndMask, ref width, ref height);
            GetMaxSize(emission2ndMask, ref width, ref height);

            if (width <= 1 || height <= 1)
            {
                EditorUtility.DisplayDialog("エラー", "有効なマスクテクスチャが見つかりませんでした。", "OK");
                return;
            }

            // Open save dialog
            string savePath = EditorUtility.SaveFilePanel(
                "パックテクスチャの保存先",
                "Assets",
                "ChannelPackedMask",
                "png"
            );

            if (string.IsNullOrEmpty(savePath))
                return;

            // Create packed texture
            Texture2D packed = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Read each mask
            Color[] rPixels = GetReadablePixels(emissionMask, width, height);
            Color[] gPixels = GetReadablePixels(glitterMask, width, height);
            Color[] bPixels = GetReadablePixels(matCap2ndMask, width, height);
            Color[] aPixels = GetReadablePixels(emission2ndMask, width, height);

            Color[] packedPixels = new Color[width * height];
            for (int i = 0; i < packedPixels.Length; i++)
            {
                float r = rPixels != null ? ToGrayscale(rPixels[i]) : 1.0f;
                float g = gPixels != null ? ToGrayscale(gPixels[i]) : 1.0f;
                float b = bPixels != null ? ToGrayscale(bPixels[i]) : 1.0f;
                float a = aPixels != null ? ToGrayscale(aPixels[i]) : 1.0f;
                packedPixels[i] = new Color(r, g, b, a);
            }

            packed.SetPixels(packedPixels);
            packed.Apply();

            // Save as PNG
            byte[] pngData = packed.EncodeToPNG();
            DestroyImmediate(packed);

            System.IO.File.WriteAllBytes(savePath, pngData);

            // Import if inside Assets
            if (savePath.StartsWith(Application.dataPath))
            {
                string relativePath = "Assets" + savePath.Substring(Application.dataPath.Length);
                AssetDatabase.ImportAsset(relativePath);

                // Configure import settings for the packed texture
                TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                if (importer != null)
                {
                    importer.sRGBTexture = false;  // Linear for mask data
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    importer.compressionQuality = (int)TextureCompressionQuality.Normal;
                    importer.SaveAndReimport();
                }

                EditorUtility.DisplayDialog("完了", $"パックテクスチャを保存しました:\n{relativePath}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("完了", $"パックテクスチャを保存しました:\n{savePath}", "OK");
            }
        }

        private static void GetMaxSize(Texture2D tex, ref int width, ref int height)
        {
            if (tex != null)
            {
                if (tex.width > width) width = tex.width;
                if (tex.height > height) height = tex.height;
            }
        }

        private static float ToGrayscale(Color c)
        {
            // Standard luminance weights, premultiplied by alpha (alpha=0 → black)
            return (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) * c.a;
        }

        /// <summary>
        /// Read pixels from a texture, handling read/write access via RenderTexture copy.
        /// Returns null if the texture is null.
        /// </summary>
        private static Color[] GetReadablePixels(Texture2D source, int targetWidth, int targetHeight)
        {
            if (source == null)
                return null;

            // Use RenderTexture to handle non-readable textures and resize
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture prevActive = RenderTexture.active;

            Graphics.Blit(source, rt);
            RenderTexture.active = rt;

            Texture2D readable = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false, true);
            readable.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            readable.Apply();

            Color[] pixels = readable.GetPixels();

            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
            DestroyImmediate(readable);

            return pixels;
        }
    }
}
#endif
