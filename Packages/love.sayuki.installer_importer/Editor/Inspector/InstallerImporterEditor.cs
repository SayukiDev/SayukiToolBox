#if UNITY_EDITOR
using System;
using love.sayuki.InstallerImporter.Editor.i18n;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;

namespace love.sayuki.InstallerImporter.Editor.Inspector
{
    [CustomEditor(typeof(InstallerImporterComponent))]
    public class InstallerImporterEditor : UnityEditor.Editor
    {
        private SerializedProperty _installTarget;
        private SerializedProperty _installerSource;
        private SerializedProperty _installToMaSubMenu;

        private void OnEnable()
        {
            _installTarget = serializedObject.FindProperty("installTarget");
            _installerSource = serializedObject.FindProperty("installerSource");
            _installToMaSubMenu = serializedObject.FindProperty("installToMaSubMenu");
            var rt = ((InstallerImporterComponent)target);
            if (rt.GetComponent<ModularAvatarMenuItem>() || rt.GetComponent<ModularAvatarMenuGroup>())
            {
                return;
            }

            EditorUtility.DisplayDialog(
                Localization.L.GetLocalizedString("installer-importer.dialog.title"),
                Localization.L.GetLocalizedString("installer-importer.dialog.requires-menu-item"),
                Localization.L.GetLocalizedString("installer-importer.dialog.ok")
            );
            DestroyImmediate(rt);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox(
                Localization.L.GetLocalizedString("installer-importer.inspector.description"),
                MessageType.Info);

            EditorGUILayout.Space();


            // Install Target
            EditorGUILayout.PropertyField(_installToMaSubMenu,
                new GUIContent(
                    Localization.L.GetLocalizedString("installer-importer.inspector.install-to-ma-sub-menu")));
            EditorGUI.BeginDisabledGroup(_installToMaSubMenu.boolValue);
            EditorGUILayout.PropertyField(_installTarget,
                new GUIContent(Localization.L.GetLocalizedString("installer-importer.inspector.install-target")));
            if (_installTarget.objectReferenceValue == null&&
                _installToMaSubMenu.boolValue == false)
            {
                EditorGUILayout.HelpBox(
                    Localization.L.GetLocalizedString("installer-importer.inspector.warn-no-target"),
                    MessageType.Warning);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();
            // Installer Source
            EditorGUILayout.PropertyField(_installerSource,
                new GUIContent(Localization.L.GetLocalizedString("installer-importer.inspector.installer-source")));
            if (_installerSource.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    Localization.L.GetLocalizedString("installer-importer.inspector.warn-no-source"),
                    MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
