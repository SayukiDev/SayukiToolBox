#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sayuki.ChannelPackingConverter.Editor
{
    /// <summary>
    /// マスクテクスチャのPacking処理ユーティリティ。
    /// </summary>
    public static class MaskPackingUtility
    {
        /// <summary>
        /// マスク情報。元テクスチャとそのプロパティ名を保持する。
        /// </summary>
        public class MaskInfo
        {
            public Texture2D Texture;
            public string PropertyName;
            /// <summary>解像度 (width * height)</summary>
            public int Resolution => Texture != null ? Texture.width * Texture.height : 0;
            /// <summary>幅</summary>
            public int Width => Texture != null ? Texture.width : 0;
            /// <summary>高さ</summary>
            public int Height => Texture != null ? Texture.height : 0;
        }

        /// <summary>
        /// PackedMaskの結果。テクスチャと各チャンネルのマスク情報を保持する。
        /// </summary>
        public class PackedResult
        {
            public Texture2D PackedTexture;
            public MaskInfo[] Masks; // index 0=R, 1=G, 2=B, 3=A (nullable)
        }

        /// <summary>
        /// マスクテクスチャ群をグループ化してPackする。
        /// </summary>
        /// <param name="masks">解像度降順ソート済みのユニークマスク一覧</param>
        /// <param name="maxSize">サイズ上限 (0=無制限)</param>
        /// <returns>PackedResult のリスト</returns>
        public static System.Collections.Generic.List<PackedResult> PackMasks(
            System.Collections.Generic.List<MaskInfo> masks, int maxSize)
        {
            var results = new System.Collections.Generic.List<PackedResult>();
            if (masks == null || masks.Count == 0) return results;

            int i = 0;
            int total = masks.Count;

            while (i < total)
            {
                int remaining = total - i;
                // 残りが3の倍数ならRGBのみ(3枚)、それ以外はRGBA(4枚)
                int groupSize = (remaining % 3 == 0) ? 3 : System.Math.Min(4, remaining);

                var group = new MaskInfo[groupSize];
                for (int j = 0; j < groupSize; j++)
                {
                    group[j] = masks[i + j];
                }
                i += groupSize;

                // PackedMaskサイズ = min(1枚目のサイズ, maxSize)
                int packedWidth = group[0].Width;
                int packedHeight = group[0].Height;
                if (maxSize > 0)
                {
                    packedWidth = System.Math.Min(packedWidth, maxSize);
                    packedHeight = System.Math.Min(packedHeight, maxSize);
                }

                // ピクセルデータ読み取り
                Color[][] channelPixels = new Color[groupSize][];
                for (int j = 0; j < groupSize; j++)
                {
                    channelPixels[j] = GetReadablePixels(group[j].Texture, packedWidth, packedHeight);
                }

                // Pack — まずRGBA32で作成してピクセル書き込み後に圧縮
                bool hasAlpha = groupSize == 4;
                var packed = new Texture2D(packedWidth, packedHeight, TextureFormat.RGBA32, true, true); // mipmap + linear
                packed.name = $"PackedMask_{results.Count}";

                int pixelCount = packedWidth * packedHeight;
                Color[] packedPixels = new Color[pixelCount];

                for (int p = 0; p < pixelCount; p++)
                {
                    float r = ToGrayscale(channelPixels[0][p]);
                    float g = groupSize > 1 ? ToGrayscale(channelPixels[1][p]) : 1.0f;
                    float b = groupSize > 2 ? ToGrayscale(channelPixels[2][p]) : 1.0f;
                    float a = groupSize > 3 ? ToGrayscale(channelPixels[3][p]) : 1.0f;
                    packedPixels[p] = new Color(r, g, b, a);
                }

                packed.SetPixels(packedPixels);
                packed.Apply();

                // BC1(DXT1) for RGB, BC3(DXT5) for RGBA
                var compressFormat = hasAlpha ? TextureFormat.DXT5 : TextureFormat.DXT1;
                EditorUtility.CompressTexture(packed, compressFormat, TextureCompressionQuality.Normal);
                packed.Apply();

                var maskArray = new MaskInfo[4];
                for (int j = 0; j < groupSize; j++)
                    maskArray[j] = group[j];

                results.Add(new PackedResult
                {
                    PackedTexture = packed,
                    Masks = maskArray
                });
            }

            return results;
        }

        /// <summary>
        /// テクスチャからピクセルデータを読み取る (RenderTexture経由で非読み取り可テクスチャにも対応)
        /// </summary>
        public static Color[] GetReadablePixels(Texture2D source, int targetWidth, int targetHeight)
        {
            if (source == null)
                return CreateSolidPixels(targetWidth, targetHeight, Color.white);

            RenderTexture rt = RenderTexture.GetTemporary(
                targetWidth, targetHeight, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture prevActive = RenderTexture.active;

            Graphics.Blit(source, rt);
            RenderTexture.active = rt;

            Texture2D readable = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, true, true);
            readable.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            readable.Apply();

            Color[] pixels = readable.GetPixels();

            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(readable);

            return pixels;
        }

        /// <summary>
        /// カラーをグレースケールに変換。
        /// </summary>
        public static float ToGrayscale(Color c)
        {
            return (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) * c.a;
        }

        private static Color[] CreateSolidPixels(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            return pixels;
        }
    }
}
#endif
