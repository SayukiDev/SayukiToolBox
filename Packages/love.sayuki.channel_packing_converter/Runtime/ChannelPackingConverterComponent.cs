using System.Collections.Generic;
using UnityEngine;
using nadena.dev.ndmf;

namespace Sayuki.ChannelPackingConverter
{
    /// <summary>
    /// アバタールートに付加するコンポーネント。
    /// NDMFビルド時にLilToonマテリアルのマスクをChannel Packingし、
    /// Channel Packing Shader (CPS) へ非破壊的に変換する。
    /// </summary>
    [AddComponentMenu("Sayuki/Channel Packing Converter")]
    [DisallowMultipleComponent]
    public class ChannelPackingConverterComponent : MonoBehaviour, INDMFEditorOnly
    {
        [Tooltip("変換から除外するマテリアル")]
        public List<Material> excludedMaterials = new();

        [Tooltip("PackedMaskテクスチャのサイズ上限 (0=無制限)")]
        [Min(0)]
        public int maxPackedTextureSize = 0;
    }
}
