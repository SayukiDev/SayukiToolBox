#if UNITY_EDITOR
using nadena.dev.ndmf;

[assembly: ExportsPlugin(typeof(Sayuki.ShapeKeyMixer.Editor.ShapeKeyMixerPlugin))]

namespace Sayuki.ShapeKeyMixer.Editor
{
    /// <summary>
    /// NDMFプラグインエントリポイント。
    /// BuildPhase.OptimizingでAAOより先にShapeKeyミックスを実行する。
    /// </summary>
    public class ShapeKeyMixerPlugin : Plugin<ShapeKeyMixerPlugin>
    {
        public override string DisplayName => "ShapeKey Mixer";

        protected override void Configure()
        {
            InPhase(BuildPhase.Optimizing)
                .BeforePlugin("com.anatawa12.avatar-optimizer")
                .Run(ShapeKeyMixerPass.Instance);
        }
    }
}
#endif
