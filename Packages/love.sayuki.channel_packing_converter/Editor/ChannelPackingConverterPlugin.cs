#if UNITY_EDITOR
using nadena.dev.ndmf;

[assembly: ExportsPlugin(typeof(Sayuki.ChannelPackingConverter.Editor.ChannelPackingConverterPlugin))]

namespace Sayuki.ChannelPackingConverter.Editor
{
    /// <summary>
    /// NDMFプラグイン。BuildPhase.TransformingでChannel Packing変換を実行する。
    /// </summary>
    public class ChannelPackingConverterPlugin : Plugin<ChannelPackingConverterPlugin>
    {
        public override string DisplayName => "Channel Packing Converter";

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                .Run(ChannelPackingPass.Instance);
        }
    }
}
#endif
