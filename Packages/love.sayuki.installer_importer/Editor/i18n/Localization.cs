#if UNITY_EDITOR
using System.Collections.Generic;
using nadena.dev.ndmf.localization;
using UnityEditor;

namespace love.sayuki.InstallerImporter.Editor.i18n
{
    static class Localization
    {
        
        private static string BasePath = AssetDatabase.GUIDToAssetPath("023990d349af4414cab6964645461a4f");

        public static readonly Localizer L = new Localizer(
            "ja-JP",
            () => new List<UnityEngine.LocalizationAsset>
            {
                AssetDatabase.LoadAssetAtPath<UnityEngine.LocalizationAsset>($"{BasePath}/en-US.po"),
                AssetDatabase.LoadAssetAtPath<UnityEngine.LocalizationAsset>($"{BasePath}/ja-JP.po"),
            }
        );
    }
}
#endif
