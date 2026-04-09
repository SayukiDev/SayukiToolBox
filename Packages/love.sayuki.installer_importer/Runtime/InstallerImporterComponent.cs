#if MA_VRCSDK3_AVATARS

using System;
using UnityEngine;
using nadena.dev.ndmf;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine.Assertions;

namespace love.sayuki.InstallerImporter
{
    [AddComponentMenu("Sayuki/InstallerImporter/Installer Importer")]
    [DisallowMultipleComponent]
    public class InstallerImporterComponent : MonoBehaviour, INDMFEditorOnly
    {
        public bool installToMaSubMenu = true;
        public GameObject installTarget;
        public GameObject installerSource;
    }
}

#endif