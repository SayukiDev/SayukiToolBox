#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Sayuki.ShapeKeyMixer.Editor
{
    /// <summary>
    /// ShapeKeyMixerComponent のカスタムInspector。
    /// ドロップダウンによるShapeKey選択、バリデーション警告を提供する。
    /// </summary>
    [CustomEditor(typeof(ShapeKeyMixerComponent))]
    public class ShapeKeyMixerComponentEditor : UnityEditor.Editor
    {
        private ShapeKeyMixerComponent _component;
        private SkinnedMeshRenderer _smr;
        private string[] _blendShapeNames;

        private void OnEnable()
        {
            _component = (ShapeKeyMixerComponent)target;
            _smr = _component.GetComponent<SkinnedMeshRenderer>();
            RefreshBlendShapeNames();
        }

        /// <summary>
        /// SkinnedMeshRendererからBlendShape名リストを取得する。
        /// </summary>
        private void RefreshBlendShapeNames()
        {
            if (_smr == null || _smr.sharedMesh == null)
            {
                _blendShapeNames = new string[0];
                return;
            }

            var mesh = _smr.sharedMesh;
            int count = mesh.blendShapeCount;
            _blendShapeNames = new string[count];
            for (int i = 0; i < count; i++)
            {
                _blendShapeNames[i] = mesh.GetBlendShapeName(i);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (_smr == null)
            {
                EditorGUILayout.HelpBox("SkinnedMeshRendererが見つかりません。", MessageType.Error);
                return;
            }

            if (_smr.sharedMesh == null)
            {
                EditorGUILayout.HelpBox("Meshが設定されていません。", MessageType.Error);
                return;
            }

            // MixEntriesリスト描画
            DrawMixEntries();

            serializedObject.ApplyModifiedProperties();
        }

        #region Mix Entries UI

        private void DrawMixEntries()
        {
            var entries = _component.mixEntries;

            for (int entryIdx = 0; entryIdx < entries.Count; entryIdx++)
            {
                var entry = entries[entryIdx];

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // ヘッダー: "Mix Entry N" + 削除ボタン
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Mix Entry {entryIdx}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(18)))
                {
                    Undo.RecordObject(_component, "Remove Mix Entry");
                    entries.RemoveAt(entryIdx);
                    EditorUtility.SetDirty(_component);
                    break;
                }

                EditorGUILayout.EndHorizontal();

                // ベースShapeKey選択
                DrawShapeKeyDropdown("Base ShapeKey", ref entry.baseShapeKeyName);

                EditorGUILayout.Space(2);

                // ソースリスト
                EditorGUILayout.LabelField("Sources", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;

                for (int srcIdx = 0; srcIdx < entry.sources.Count; srcIdx++)
                {
                    var source = entry.sources[srcIdx];

                    EditorGUILayout.BeginHorizontal();

                    // ShapeKey選択ドロップダウン
                    DrawShapeKeyDropdownInline(ref source.shapeKeyName, 0.5f);

                    // ウェイトスライダー
                    EditorGUILayout.LabelField("W", GUILayout.Width(14));
                    float newWeight = EditorGUILayout.Slider(source.weight, -2f, 2f);
                    if (newWeight != source.weight)
                    {
                        Undo.RecordObject(_component, "Change Mix Source Weight");
                        source.weight = newWeight;
                        EditorUtility.SetDirty(_component);
                    }

                    // 削除ボタン
                    if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
                    {
                        Undo.RecordObject(_component, "Remove Mix Source");
                        entry.sources.RemoveAt(srcIdx);
                        EditorUtility.SetDirty(_component);
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;

                // ソース追加ボタン
                if (GUILayout.Button("+ Add Source"))
                {
                    Undo.RecordObject(_component, "Add Mix Source");
                    entry.sources.Add(new ShapeKeyMixSource());
                    EditorUtility.SetDirty(_component);
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(2);
            }

            // エントリ追加ボタン
            if (GUILayout.Button("+ Add Mix Entry"))
            {
                Undo.RecordObject(_component, "Add Mix Entry");
                entries.Add(new ShapeKeyMixEntry());
                EditorUtility.SetDirty(_component);
            }

            // バリデーション警告
            DrawValidationWarnings();
        }

        /// <summary>
        /// ShapeKey名のドロップダウンを描画する（フルライン版）。
        /// </summary>
        private void DrawShapeKeyDropdown(string label, ref string currentName)
        {
            if (_blendShapeNames.Length == 0)
            {
                EditorGUILayout.LabelField(label, "(BlendShapeなし)");
                return;
            }

            int currentIndex = System.Array.IndexOf(_blendShapeNames, currentName);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup(label, currentIndex, _blendShapeNames);
            string newName = _blendShapeNames[newIndex];

            if (newName != currentName)
            {
                Undo.RecordObject(_component, $"Change {label}");
                currentName = newName;
                EditorUtility.SetDirty(_component);
            }
        }

        /// <summary>
        /// ShapeKey名のドロップダウンを描画する（インライン版、比率指定）。
        /// </summary>
        private void DrawShapeKeyDropdownInline(ref string currentName, float widthRatio)
        {
            if (_blendShapeNames.Length == 0)
            {
                EditorGUILayout.LabelField("(なし)",
                    GUILayout.Width(EditorGUIUtility.currentViewWidth * widthRatio));
                return;
            }

            int currentIndex = System.Array.IndexOf(_blendShapeNames, currentName);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup(currentIndex, _blendShapeNames,
                GUILayout.Width(EditorGUIUtility.currentViewWidth * widthRatio));
            string newName = _blendShapeNames[newIndex];

            if (newName != currentName)
            {
                Undo.RecordObject(_component, "Change Source ShapeKey");
                currentName = newName;
                EditorUtility.SetDirty(_component);
            }
        }

        #endregion

        #region Validation

        private void DrawValidationWarnings()
        {
            if (_blendShapeNames.Length == 0) return;

            var blendShapeSet = new HashSet<string>(_blendShapeNames);
            var warnings = new List<string>();

            foreach (var entry in _component.mixEntries)
            {
                // ベースが存在しない
                if (!string.IsNullOrEmpty(entry.baseShapeKeyName) &&
                    !blendShapeSet.Contains(entry.baseShapeKeyName))
                {
                    warnings.Add($"ベースShapeKey '{entry.baseShapeKeyName}' は存在しません。");
                }

                foreach (var source in entry.sources)
                {
                    // ソースが存在しない
                    if (!string.IsNullOrEmpty(source.shapeKeyName) &&
                        !blendShapeSet.Contains(source.shapeKeyName))
                    {
                        warnings.Add($"ソースShapeKey '{source.shapeKeyName}' は存在しません。");
                    }

                    // ベースとソースが同じ
                    if (!string.IsNullOrEmpty(source.shapeKeyName) &&
                        source.shapeKeyName == entry.baseShapeKeyName)
                    {
                        warnings.Add(
                            $"ソース '{source.shapeKeyName}' はベースと同じです。自身のデルタが二重加算されます。");
                    }
                }
            }

            if (warnings.Count > 0)
            {
                EditorGUILayout.Space(4);
                foreach (var w in warnings)
                {
                    EditorGUILayout.HelpBox(w, MessageType.Warning);
                }
            }
        }

        #endregion
    }
}
#endif
