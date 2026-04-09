#if UNITY_EDITOR
using System.Collections.Generic;
using love.sayuki.InstallerImporter.Editor.i18n;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using UnityEngine;

namespace love.sayuki.InstallerImporter.Editor.Process
{
    public class RootMoverPass : Pass<RootMoverPass>
    {
        public override string DisplayName => "Move Objects to Root";

        protected override void Execute(BuildContext context)
        {
            var components = context.AvatarRootObject
                .GetComponentsInChildren<RootMoverComponent>(true);

            foreach (var comp in components)
            {
                var go = comp.gameObject;
                var avatarRoot = context.AvatarRootTransform;

                if (comp.moveChildrenOnly)
                {
                    MoveChildren(go.transform, avatarRoot, comp.exclusionList, go.name);
                }
                else
                {
                    MoveObject(go.transform, avatarRoot);
                }

                Object.DestroyImmediate(comp);
            }
        }
        
        private static void MoveObject(Transform target, Transform avatarRoot)
        {
            if (target.parent == avatarRoot)
            {
                ErrorReport.ReportError(Localization.L, ErrorSeverity.Information,
                    "root-mover.info.already-at-root", target.name);
                return;
            }

            var originalPath = GetHierarchyPath(target);
            target.SetParent(avatarRoot, true);

            Debug.LogFormat(Localization.L.GetLocalizedString("root-mover.info.moved-to-root"), originalPath);
        }
        
        private static void MoveChildren(Transform parent, Transform avatarRoot,
            List<Transform> exclusionList, string objectName)
        {
            var exclusionSet = new HashSet<Transform>();
            if (exclusionList != null)
            {
                foreach (var t in exclusionList)
                {
                    if (t != null) exclusionSet.Add(t);
                }
            }
            
            var children = new List<Transform>();
            foreach (Transform child in parent)
            {
                if (exclusionSet.Contains(child))
                {
                    continue;
                }
                var ignore = child.gameObject.GetComponent<IgnoreMoveToRootComponent>();
                if (ignore != null)
                {
                    Object.DestroyImmediate(ignore);
                    continue;
                }
                children.Add(child);
            }

            if (children.Count == 0)
            {
                ErrorReport.ReportError(Localization.L, ErrorSeverity.Information,
                    "root-mover.info.no-children", objectName);
                return;
            }


            foreach (var child in children)
            {
                if (child.parent == avatarRoot)
                {
                    Debug.Log(Localization.L.GetLocalizedString("root-mover.info.already-at-root")+": "+child.name);
                    continue;
                }

                var originalPath = GetHierarchyPath(child);
                child.SetParent(avatarRoot, true);

                
                Debug.LogFormat(Localization.L.GetLocalizedString("root-mover.info.moved-to-root"), originalPath);
                
            }
        }
        
        private static string GetHierarchyPath(Transform t)
        {
            var path = t.name;
            var parent = t.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
#endif
