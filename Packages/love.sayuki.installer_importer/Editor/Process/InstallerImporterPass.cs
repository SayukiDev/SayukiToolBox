#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using love.sayuki.InstallerImporter.Editor.i18n;
using nadena.dev.ndmf;
using nadena.dev.modular_avatar.core;
using UnityEngine;

namespace love.sayuki.InstallerImporter.Editor.Process
{
    public class InstallerImporterPass : Pass<InstallerImporterPass>
    {
        public override string DisplayName => "Import Menu Installers";

        // ModularAvatarMenuInstallTarget は internal のためリフレクションでアクセス
        private static readonly Type InstallTargetType;
        private static readonly FieldInfo InstallerField;

        static InstallerImporterPass()
        {
            var assembly = typeof(ModularAvatarMenuInstaller).Assembly;
            InstallTargetType = assembly.GetType("nadena.dev.modular_avatar.core.ModularAvatarMenuInstallTarget");
            if (InstallTargetType != null)
            {
                InstallerField = InstallTargetType.GetField("installer",
                    BindingFlags.Public | BindingFlags.Instance);
            }
        }

        protected override void Execute(BuildContext context)
        {
            if (InstallTargetType == null || InstallerField == null)
            {
                ErrorReport.ReportError(Localization.L, ErrorSeverity.InternalError,
                    "installer-importer.error.reflection-failed");
                return;
            }

            var components = context.AvatarRootObject
                .GetComponentsInChildren<InstallerImporterComponent>(true);

            foreach (var comp in components)
            {
                ErrorReport.WithContextObject(comp, () => ProcessComponent(comp));
                UnityEngine.Object.DestroyImmediate(comp);
            }
        }

        private void ProcessComponent(InstallerImporterComponent comp)
        {
            var installers = new List<ModularAvatarMenuInstaller>();
            
            if (!comp.installTarget)
            {
                if (comp.installToMaSubMenu)
                {
                    var mag = comp.gameObject.GetComponent<ModularAvatarMenuGroup>();
                    if (mag == null)
                    {
                        var mam=comp.gameObject.GetComponent<ModularAvatarMenuItem>();
                        if (mam == null)
                        {
                            ErrorReport.ReportError(Localization.L, ErrorSeverity.Information,
                                "installer-importer.info.no-ma-menu-found", comp.gameObject.name);
                            return;
                        }
                        comp.installTarget = mam.menuSource_otherObjectChildren;
                    }
                    else
                    {
                        comp.installTarget = mag.targetObject;
                    }
                }

                if (!comp.installTarget)
                {
                    comp.installTarget=comp.gameObject;
                }
            }

            if (!comp.installerSource)
            {
                comp.installerSource = comp.gameObject;
            }

            CollectInstallers(comp.installerSource.transform, installers);

            if (installers.Count == 0)
            {
                ErrorReport.ReportError(Localization.L, ErrorSeverity.Information,
                    "installer-importer.info.no-installers-found", comp.gameObject.name);
                return;
            }

            foreach (var installer in installers)
            {
                var targetObj = new GameObject($"InstallTarget_{installer.gameObject.name}");
                targetObj.transform.SetParent(comp.installTarget.transform, false);
                
                var installTarget = targetObj.AddComponent(InstallTargetType);
                InstallerField.SetValue(installTarget, installer);
                targetObj.AddComponent<IgnoreMoveToRootComponent>();

                Debug.LogFormat(
                    Localization.L.GetLocalizedString("installer-importer.info.install-target-created"),
                    installer.gameObject.name,
                    targetObj.name
                    );
            }
        }

        private void CollectInstallers(Transform parent, List<ModularAvatarMenuInstaller> results)
        {
            foreach (Transform child in parent)
            {
                var installer = child.GetComponent<ModularAvatarMenuInstaller>();
                if (installer != null)
                {
                    results.Add(installer);
                }

                // 別のMa Menuを持つオブジェクトなら
                // その子階層は走査しない
                if (child.GetComponent<InstallerImporterComponent>() != null)
                {
                    continue;
                }

                if (child.GetComponent<ModularAvatarMenuItem>() != null)
                {
                    continue;
                }
                
                CollectInstallers(child, results);
            }
        }
    }
}
#endif
