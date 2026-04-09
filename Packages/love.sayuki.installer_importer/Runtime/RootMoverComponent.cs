#if MA_VRCSDK3_AVATARS

using System.Collections.Generic;
using UnityEngine;
using nadena.dev.ndmf;

namespace love.sayuki.InstallerImporter
{
    [AddComponentMenu("Sayuki/InstallerImporter/Move to Root")]
    [DisallowMultipleComponent]
    public class RootMoverComponent : MonoBehaviour, INDMFEditorOnly
    {
        public bool moveChildrenOnly = false;
        public List<Transform> exclusionList = new List<Transform>();
    }
}

#endif
