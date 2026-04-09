#if UNITY_EDITOR
using love.sayuki.InstallerImporter.Editor.Process;
using nadena.dev.ndmf;
using UnityEditor;
using love.sayuki.InstallerImporter.Editor.i18n;

[assembly: ExportsPlugin(typeof(love.sayuki.InstallerImporter.Editor.InstallerImporterPlugin))]

namespace love.sayuki.InstallerImporter.Editor
{

    [InitializeOnLoad]
    public class InstallerImporterPlugin : Plugin<InstallerImporterPlugin>
    {

        public override string DisplayName => "Installer Importer";

        protected override void Configure()
        {
            InPhase(BuildPhase.FirstChance).
                BeforePlugin("nadena.dev.modular-avatar")
                .Run(InstallerImporterPass.Instance)
                .Then.Run(RootMoverPass.Instance);
        }
    }
}
#endif
