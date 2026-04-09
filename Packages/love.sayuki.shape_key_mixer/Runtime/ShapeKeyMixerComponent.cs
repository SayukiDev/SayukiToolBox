using System;
using System.Collections.Generic;
using UnityEngine;
using nadena.dev.ndmf;

namespace Sayuki.ShapeKeyMixer
{
    /// <summary>
    /// ミックスソース（ShapeKey名 + ウェイト）。
    /// </summary>
    [Serializable]
    public class ShapeKeyMixSource
    {
        /// <summary>ソースShapeKey名</summary>
        public string shapeKeyName = "";

        /// <summary>ミックスウェイト（-2.0〜2.0）</summary>
        [Range(-2f, 2f)]
        public float weight = 1.0f;
    }

    /// <summary>
    /// 1つのミックス操作を定義するデータクラス。
    /// ベースShapeKeyにソースShapeKeyのデルタをウェイト付きで加算し、
    /// ベースShapeKeyを上書きする。
    /// </summary>
    [Serializable]
    public class ShapeKeyMixEntry
    {
        /// <summary>ベースとなるShapeKey名（ミックス結果で上書きされる）</summary>
        public string baseShapeKeyName = "";

        /// <summary>ミックスするソースShapeKeyのリスト</summary>
        public List<ShapeKeyMixSource> sources = new();
    }

    /// <summary>
    /// ShapeKey Mixer コンポーネント。
    /// SkinnedMeshRendererと同じGameObjectに配置し、
    /// NDMFビルド時にBlendShapeのミックスを非破壊的に実行する。
    /// BlenderのNew Shape from Mixと同等の機能を提供する。
    /// </summary>
    [AddComponentMenu("Sayuki/ShapeKey Mixer")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    [DisallowMultipleComponent]
    public class ShapeKeyMixerComponent : MonoBehaviour, INDMFEditorOnly
    {
        /// <summary>複数のミックス操作を定義可能</summary>
        public List<ShapeKeyMixEntry> mixEntries = new();
    }
}
