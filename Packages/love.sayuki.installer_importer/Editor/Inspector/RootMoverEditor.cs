#if UNITY_EDITOR
using love.sayuki.InstallerImporter.Editor.i18n;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace love.sayuki.InstallerImporter.Editor.Inspector
{
    [CustomEditor(typeof(RootMoverComponent))]
    public class RootMoverEditor : UnityEditor.Editor
    {
        private SerializedProperty _moveChildrenOnly;
        private SerializedProperty _exclusionList;
        private ReorderableList _reorderableList;

        private void OnEnable()
        {
            _moveChildrenOnly = serializedObject.FindProperty("moveChildrenOnly");
            _exclusionList = serializedObject.FindProperty("exclusionList");

            _reorderableList = new ReorderableList(serializedObject, _exclusionList, true, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect,
                        Localization.L.GetLocalizedString("root-mover.inspector.exclusion-list"));
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = _exclusionList.GetArrayElementAtIndex(index);
                    rect.y += 2;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(rect, element, GUIContent.none);
                },
                elementHeightCallback = index => EditorGUIUtility.singleLineHeight + 4
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.HelpBox(
                Localization.L.GetLocalizedString("root-mover.inspector.description"),
                MessageType.Info);

            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(_moveChildrenOnly,
                new GUIContent(Localization.L.GetLocalizedString("root-mover.inspector.move-children-only")));

            EditorGUILayout.Space();

            // 除外リスト（moveChildrenOnly時のみ有効）
            EditorGUI.BeginDisabledGroup(!_moveChildrenOnly.boolValue);

            if (!_moveChildrenOnly.boolValue)
            {
                EditorGUILayout.HelpBox(
                    Localization.L.GetLocalizedString("root-mover.inspector.exclusion-not-applicable"),
                    MessageType.None);
            }

            _reorderableList.DoLayoutList();

            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
