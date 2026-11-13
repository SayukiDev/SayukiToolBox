using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

[assembly: ExportsPlugin(typeof(Sayuki.OSCFaceShapeKeyMapping.Editor.OSCFaceShapeKeyMappingPlugin))]

namespace Sayuki.OSCFaceShapeKeyMapping.Editor
{
    public class OSCFaceShapeKeyMappingPlugin : Plugin<OSCFaceShapeKeyMappingPlugin>
    {
        public override string QualifiedName => "love.sayuki.osc_face_shapekey_mapping";
        public override string DisplayName => "OSC Face ShapeKey Mapping";

        protected override void Configure()
        {
            // BShapeKey Baker（Optimizing, AAO より前）がシェイプキーを焼き込み・削除した後のメッシュを対象にする。
            // AAO がアニメーションを解析できるよう AAO より前に実行する。いずれも任意依存。
            InPhase(BuildPhase.Optimizing)
                .AfterPlugin("love.sayuki.bshapekey-baker")
                .BeforePlugin("com.anatawa12.avatar-optimizer")
                .WithRequiredExtension(typeof(AnimatorServicesContext),
                    seq => seq.Run(OSCFaceShapeKeyMappingPass.Instance));
        }
    }

    public class OSCFaceShapeKeyMappingPass : Pass<OSCFaceShapeKeyMappingPass>
    {
        public override string DisplayName => "Map face shape keys to VRCSBPOSC_ parameters";

        private const string LayerName = "VRCSBPOSC_FaceShapeKeys";

        protected override void Execute(BuildContext ctx)
        {
            var configs = ctx.AvatarRootObject.GetComponentsInChildren<OSCFaceShapeKeyMappingComponent>(true);
            if (configs.Length == 0) return;

            if (configs.Length > 1)
            {
                Debug.LogWarning(
                    $"[OSC Face ShapeKey Mapping] 複数のコンポーネントが見つかりました。'{configs[0].name}' のみ使用します。");
            }

            var config = configs[0];
            try
            {
                Process(ctx, config);
            }
            finally
            {
                foreach (var c in configs) Object.DestroyImmediate(c);
            }
        }

        private static void Process(BuildContext ctx, OSCFaceShapeKeyMappingComponent config)
        {
            var renderer = ShapeKeyMapping.ResolveRenderer(config);
            if (renderer == null || renderer.sharedMesh == null)
            {
                Debug.LogError("[OSC Face ShapeKey Mapping] 対象の Face メッシュが見つかりません。Target Renderer を指定してください。");
                return;
            }

            if (!renderer.transform.IsChildOf(ctx.AvatarRootTransform))
            {
                Debug.LogError($"[OSC Face ShapeKey Mapping] '{renderer.name}' はアバターの子ではありません。");
                return;
            }

            var entries = ShapeKeyMapping.Build(config, renderer);
            if (entries.Count == 0)
            {
                Debug.LogWarning("[OSC Face ShapeKey Mapping] マッピング対象のシェイプキーがありません。");
                return;
            }

            var descriptor = ctx.AvatarRootObject.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null) return;

            var asc = ctx.Extension<AnimatorServicesContext>();
            if (!asc.ControllerContext.Controllers.TryGetValue(VRCAvatarDescriptor.AnimLayerType.FX, out var fx)
                || fx == null)
            {
                Debug.LogError("[OSC Face ShapeKey Mapping] FX コントローラーを取得できませんでした。");
                return;
            }

            var path = asc.ObjectPathRemapper.GetVirtualPathForObject(renderer.transform);

            AddAnimatorParameters(fx, entries, config);
            BuildLayer(fx, entries, config, path);
            AddExpressionParameters(ctx, descriptor, entries, config);

            Debug.Log($"[OSC Face ShapeKey Mapping] '{renderer.name}' のシェイプキー {entries.Count} 個を VRCSBPOSC_ パラメーターにマッピングしました。");
        }

        private static void AddAnimatorParameters(VirtualAnimatorController fx, List<ShapeKeyEntry> entries,
            OSCFaceShapeKeyMappingComponent config)
        {
            fx.SetParameter(OSCFaceShapeKeyMappingComponent.WeightParameterName, new AnimatorControllerParameter
            {
                name = OSCFaceShapeKeyMappingComponent.WeightParameterName,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 1f,
            });

            if (config.useEnableParameter)
            {
                fx.SetParameter(OSCFaceShapeKeyMappingComponent.EnableParameterName, new AnimatorControllerParameter
                {
                    name = OSCFaceShapeKeyMappingComponent.EnableParameterName,
                    type = AnimatorControllerParameterType.Bool,
                    defaultBool = config.enabledByDefault,
                });
            }

            foreach (var e in entries)
            {
                fx.SetParameter(e.ParameterName, new AnimatorControllerParameter
                {
                    name = e.ParameterName,
                    type = AnimatorControllerParameterType.Float,
                    defaultFloat = e.DefaultValue,
                });
            }
        }

        private static void BuildLayer(VirtualAnimatorController fx, List<ShapeKeyEntry> entries,
            OSCFaceShapeKeyMappingComponent config, string path)
        {
            // Direct BlendTree: 各シェイプキーごとに 1D BlendTree (0 → 0, 1 → 100)
            var children = ImmutableList.CreateBuilder<VirtualBlendTree.VirtualChildMotion>();
            foreach (var e in entries)
            {
                var prop = "blendShape." + e.ShapeKeyName;

                var tree1D = VirtualBlendTree.Create(e.ParameterName);
                tree1D.BlendType = BlendTreeType.Simple1D;
                tree1D.BlendParameter = e.ParameterName;
                tree1D.UseAutomaticThresholds = false;
                tree1D.MinThreshold = 0f;
                tree1D.MaxThreshold = 1f;
                tree1D.Children = ImmutableList.Create(
                    new VirtualBlendTree.VirtualChildMotion
                        { Motion = MakeClip($"{e.ParameterName}_0", path, prop, 0f), Threshold = 0f, TimeScale = 1f },
                    new VirtualBlendTree.VirtualChildMotion
                        { Motion = MakeClip($"{e.ParameterName}_100", path, prop, 100f), Threshold = 1f, TimeScale = 1f }
                );

                children.Add(new VirtualBlendTree.VirtualChildMotion
                {
                    Motion = tree1D,
                    DirectBlendParameter = OSCFaceShapeKeyMappingComponent.WeightParameterName,
                    TimeScale = 1f,
                });
            }

            var direct = VirtualBlendTree.Create("VRCSBPOSC_Direct");
            direct.BlendType = BlendTreeType.Direct;
            direct.NormalizedBlendValues = false;
            direct.Children = children.ToImmutable();

            var layer = fx.AddLayer(LayerPriority.Default, LayerName);
            layer.DefaultWeight = 1f;
            var sm = layer.StateMachine;

            var on = sm.AddState("On", direct, new Vector3(300, 120, 0));
            on.WriteDefaultValues = true; // Direct BlendTree は WD ON 前提

            if (!config.useEnableParameter)
            {
                sm.DefaultState = on;
                return;
            }

            var off = sm.AddState("Off", null, new Vector3(300, 0, 0));
            off.WriteDefaultValues = true; // 空ステート: 他レイヤーの表情を上書きしない
            sm.DefaultState = off;

            off.Transitions = ImmutableList.Create(MakeTransition(on, AnimatorConditionMode.If));
            on.Transitions = ImmutableList.Create(MakeTransition(off, AnimatorConditionMode.IfNot));
        }

        private static VirtualStateTransition MakeTransition(VirtualState dest, AnimatorConditionMode mode)
        {
            var t = VirtualStateTransition.Create();
            t.SetDestination(dest);
            t.ExitTime = null;
            t.HasFixedDuration = true;
            t.Duration = 0f;
            t.Conditions = ImmutableList.Create(new AnimatorCondition
            {
                mode = mode,
                parameter = OSCFaceShapeKeyMappingComponent.EnableParameterName,
                threshold = 0f,
            });
            return t;
        }

        private static VirtualClip MakeClip(string name, string path, string prop, float value)
        {
            var clip = VirtualClip.Create(name);
            clip.SetFloatCurve(path, typeof(SkinnedMeshRenderer), prop,
                AnimationCurve.Constant(0f, 1f / 60f, value));
            return clip;
        }

        private static void AddExpressionParameters(BuildContext ctx, VRCAvatarDescriptor descriptor,
            List<ShapeKeyEntry> entries, OSCFaceShapeKeyMappingComponent config)
        {
            var exParams = descriptor.expressionParameters;
            if (exParams == null)
            {
                exParams = ScriptableObject.CreateInstance<VRCExpressionParameters>();
                exParams.name = "VRCSBPOSC_ExpressionParameters";
                exParams.parameters = new VRCExpressionParameters.Parameter[0];
                ctx.AssetSaver.SaveAsset(exParams);
            }
            else if (!ctx.IsTemporaryAsset(exParams))
            {
                exParams = Object.Instantiate(exParams);
                ctx.AssetSaver.SaveAsset(exParams);
            }

            descriptor.customExpressions = true;
            descriptor.expressionParameters = exParams;

            var list = (exParams.parameters ?? new VRCExpressionParameters.Parameter[0]).ToList();
            var existing = new HashSet<string>(list.Select(p => p.name));

            void Add(string name, VRCExpressionParameters.ValueType type, float def)
            {
                if (!existing.Add(name))
                {
                    Debug.LogWarning($"[OSC Face ShapeKey Mapping] パラメーター '{name}' は既に存在するためスキップしました。");
                    return;
                }

                list.Add(new VRCExpressionParameters.Parameter
                {
                    name = name,
                    valueType = type,
                    defaultValue = def,
                    saved = false,
                    networkSynced = false, // 非同期
                });
            }

            if (config.useEnableParameter)
            {
                Add(OSCFaceShapeKeyMappingComponent.EnableParameterName, VRCExpressionParameters.ValueType.Bool,
                    config.enabledByDefault ? 1f : 0f);
            }

            foreach (var e in entries)
            {
                Add(e.ParameterName, VRCExpressionParameters.ValueType.Float, e.DefaultValue);
            }

            exParams.parameters = list.ToArray();
        }
    }
}
