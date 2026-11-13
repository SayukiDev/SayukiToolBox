using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace Sayuki.OSCFaceShapeKeyMapping
{
    public enum ShapeKeySelectMode
    {
        /// <summary>全シェイプキー（除外リスト適用）</summary>
        All,
        /// <summary>指定したシェイプキーのみ</summary>
        IncludeList,
    }

    public enum ParameterNameMode
    {
        /// <summary>VRCSBPOSC_{シェイプキー名}（OSC予約文字のみ置換）</summary>
        ShapeKeyName = 0,
        /// <summary>VRCSBPOSC_{インデックス}</summary>
        Index = 2, // 既存シーンの互換のため値を維持
    }

    /// <summary>
    /// Face の SkinnedMeshRenderer のシェイプキーを非同期 (unsynced) パラメーター
    /// VRCSBPOSC_* にマッピングし、OSC から制御できるようにする。
    /// ビルド時に NDMF で処理され、このコンポーネント自体は削除される。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Sayuki/OSC Face ShapeKey Mapping")]
    public class OSCFaceShapeKeyMappingComponent : MonoBehaviour, IEditorOnly
    {
        public const string Prefix = "VRCSBPOSC_";
        public const string EnableParameterName = Prefix + "Enable";
        public const string WeightParameterName = Prefix + "_Weight";

        [Tooltip("対象の Face メッシュ。未指定ならアバターの Viseme メッシュ → 'Body' → 'Face' の順で自動検出")]
        public SkinnedMeshRenderer targetRenderer;

        public ShapeKeySelectMode selectMode = ShapeKeySelectMode.All;

        [Tooltip("IncludeList モードで使用するシェイプキー名")]
        public List<string> includeShapeKeys = new List<string>();

        [Tooltip("All モードで除外するシェイプキー名")]
        public List<string> excludeShapeKeys = new List<string>();

        [Tooltip("All モードで 'vrc.' で始まるシェイプキー（Viseme / まばたき等）を除外")]
        public bool excludeVrcPrefixed = true;

        public ParameterNameMode nameMode = ParameterNameMode.ShapeKeyName;

        [Tooltip("VRCSBPOSC_Enable (Bool) で ON/OFF を切り替える。OFF 中は表情など他レイヤーを上書きしない")]
        public bool useEnableParameter = true;

        [Tooltip("VRCSBPOSC_Enable の初期値")]
        public bool enabledByDefault = false;

        [Tooltip("各パラメーターの初期値をメッシュの現在のシェイプキー値にする（OFF なら 0）")]
        public bool useCurrentWeightAsDefault = true;
    }
}
