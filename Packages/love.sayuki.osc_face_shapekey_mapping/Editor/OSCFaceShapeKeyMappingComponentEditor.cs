using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sayuki.OSCFaceShapeKeyMapping.Editor
{
    [CustomEditor(typeof(OSCFaceShapeKeyMappingComponent))]
    internal class OSCFaceShapeKeyMappingComponentEditor : UnityEditor.Editor
    {
        private bool _showMapping = true;
        private Vector2 _scroll;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var config = (OSCFaceShapeKeyMappingComponent)target;

            EditorGUILayout.HelpBox(
                "Face のシェイプキーを非同期パラメーター (VRCSBPOSC_*, Float 0〜1) にマッピングします。\n" +
                "OSC: /avatar/parameters/<パラメーター名>",
                MessageType.Info);

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OSCFaceShapeKeyMappingComponent.targetRenderer)));

            var renderer = ShapeKeyMapping.ResolveRenderer(config);
            if (config.targetRenderer == null)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("自動検出", renderer, typeof(SkinnedMeshRenderer), true);
            }

            var modeProp = serializedObject.FindProperty(nameof(OSCFaceShapeKeyMappingComponent.selectMode));
            EditorGUILayout.PropertyField(modeProp);
            if ((ShapeKeySelectMode)modeProp.enumValueIndex == ShapeKeySelectMode.IncludeList)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OSCFaceShapeKeyMappingComponent.includeShapeKeys)), true);
                if (renderer != null && renderer.sharedMesh != null && GUILayout.Button("メッシュの全シェイプキーをリストに追加"))
                {
                    Undo.RecordObject(config, "Add shape keys");
                    var mesh = renderer.sharedMesh;
                    for (var i = 0; i < mesh.blendShapeCount; i++)
                    {
                        var n = mesh.GetBlendShapeName(i);
                        if (!config.includeShapeKeys.Contains(n)) config.includeShapeKeys.Add(n);
                    }
                    EditorUtility.SetDirty(config);
                    serializedObject.Update();
                }
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OSCFaceShapeKeyMappingComponent.excludeShapeKeys)), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OSCFaceShapeKeyMappingComponent.excludeVrcPrefixed)));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OSCFaceShapeKeyMappingComponent.nameMode)));

            var enableProp = serializedObject.FindProperty(nameof(OSCFaceShapeKeyMappingComponent.useEnableParameter));
            EditorGUILayout.PropertyField(enableProp);
            if (enableProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OSCFaceShapeKeyMappingComponent.enabledByDefault)));
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("常に有効になり、対象シェイプキーは他の表情アニメーションより優先されます。", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OSCFaceShapeKeyMappingComponent.useCurrentWeightAsDefault)));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            var entries = ShapeKeyMapping.Build(config, renderer);
            _showMapping = EditorGUILayout.Foldout(_showMapping, $"マッピング ({entries.Count} 個)", true);
            if (_showMapping)
            {
                if (config.useEnableParameter)
                    EditorGUILayout.LabelField(OSCFaceShapeKeyMappingComponent.EnableParameterName, "Bool (ON/OFF)");

                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(240));
                foreach (var e in entries)
                    EditorGUILayout.LabelField(e.ShapeKeyName, e.ParameterName);
                EditorGUILayout.EndScrollView();
            }

            using (new EditorGUI.DisabledScope(entries.Count == 0))
            {
                if (GUILayout.Button("マッピングを JSON でクリップボードにコピー"))
                {
                    EditorGUIUtility.systemCopyBuffer = ShapeKeyMapping.ToJson(entries, config.useEnableParameter);
                }
            }

            var dup = entries.GroupBy(e => e.ShapeKeyName).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (dup.Count > 0)
            {
                EditorGUILayout.HelpBox("同名のシェイプキーがあります（アニメーションは先頭のものにしか効きません）: " +
                                        string.Join(", ", dup), MessageType.Warning);
            }
        }
    }
}
