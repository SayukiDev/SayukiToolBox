using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Sayuki.OSCFaceShapeKeyMapping.Editor
{
    internal readonly struct ShapeKeyEntry
    {
        public readonly int Index;
        public readonly string ShapeKeyName;
        public readonly string ParameterName;
        public readonly float DefaultValue; // 0..1

        public ShapeKeyEntry(int index, string shapeKeyName, string parameterName, float defaultValue)
        {
            Index = index;
            ShapeKeyName = shapeKeyName;
            ParameterName = parameterName;
            DefaultValue = defaultValue;
        }
    }

    internal static class ShapeKeyMapping
    {
        // OSC アドレスで問題になる文字
        private static readonly HashSet<char> OscReserved = new HashSet<char>
            { ' ', '#', '*', ',', '/', '?', '[', ']', '{', '}', '!', '"', '\\' };

        public static SkinnedMeshRenderer ResolveRenderer(OSCFaceShapeKeyMappingComponent config)
        {
            if (config.targetRenderer != null) return config.targetRenderer;

            var descriptor = config.GetComponentInParent<VRCAvatarDescriptor>(true);
            if (descriptor == null) return null;

            if (descriptor.VisemeSkinnedMesh != null) return descriptor.VisemeSkinnedMesh;

            var renderers = descriptor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            return renderers.FirstOrDefault(r => r.name == "Body")
                   ?? renderers.FirstOrDefault(r => r.name == "Face");
        }

        public static List<ShapeKeyEntry> Build(OSCFaceShapeKeyMappingComponent config, SkinnedMeshRenderer renderer)
        {
            var result = new List<ShapeKeyEntry>();
            if (renderer == null || renderer.sharedMesh == null) return result;

            var mesh = renderer.sharedMesh;
            var include = new HashSet<string>(config.includeShapeKeys.Where(s => !string.IsNullOrEmpty(s)));
            var exclude = new HashSet<string>(config.excludeShapeKeys.Where(s => !string.IsNullOrEmpty(s)));
            var usedNames = new HashSet<string>
            {
                OSCFaceShapeKeyMappingComponent.EnableParameterName,
                OSCFaceShapeKeyMappingComponent.WeightParameterName,
            };

            for (var i = 0; i < mesh.blendShapeCount; i++)
            {
                var name = mesh.GetBlendShapeName(i);

                if (config.selectMode == ShapeKeySelectMode.IncludeList)
                {
                    if (!include.Contains(name)) continue;
                }
                else
                {
                    if (exclude.Contains(name)) continue;
                    if (config.excludeVrcPrefixed && name.StartsWith("vrc.")) continue;
                }

                var paramName = MakeUnique(OSCFaceShapeKeyMappingComponent.Prefix + MakeSuffix(config.nameMode, name, i), usedNames);
                var def = config.useCurrentWeightAsDefault
                    ? Mathf.Clamp01(renderer.GetBlendShapeWeight(i) / 100f)
                    : 0f;

                result.Add(new ShapeKeyEntry(i, name, paramName, def));
            }

            return result;
        }

        private static string MakeSuffix(ParameterNameMode mode, string shapeKeyName, int index)
        {
            return mode == ParameterNameMode.Index ? index.ToString() : Sanitize(shapeKeyName);
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "_";
            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
            {
                sb.Append(char.IsControl(c) || OscReserved.Contains(c) ? '_' : c);
            }
            return sb.ToString();
        }

        private static string MakeUnique(string name, HashSet<string> used)
        {
            var candidate = name;
            var n = 2;
            while (!used.Add(candidate))
            {
                candidate = name + "_" + n++;
            }
            return candidate;
        }

        public static string ToJson(IEnumerable<ShapeKeyEntry> entries, bool useEnableParameter)
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            if (useEnableParameter)
            {
                sb.Append("  \"enable\": { \"parameter\": \"").Append(Escape(OSCFaceShapeKeyMappingComponent.EnableParameterName))
                    .Append("\", \"address\": \"/avatar/parameters/").Append(Escape(OSCFaceShapeKeyMappingComponent.EnableParameterName))
                    .Append("\", \"type\": \"bool\" },\n");
            }
            sb.Append("  \"shapeKeys\": [\n");
            var list = entries.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                var e = list[i];
                sb.Append("    { \"index\": ").Append(e.Index)
                    .Append(", \"shapeKey\": \"").Append(Escape(e.ShapeKeyName))
                    .Append("\", \"parameter\": \"").Append(Escape(e.ParameterName))
                    .Append("\", \"address\": \"/avatar/parameters/").Append(Escape(e.ParameterName))
                    .Append("\", \"type\": \"float\" }")
                    .Append(i < list.Count - 1 ? ",\n" : "\n");
            }
            sb.Append("  ]\n}\n");
            return sb.ToString();
        }

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
