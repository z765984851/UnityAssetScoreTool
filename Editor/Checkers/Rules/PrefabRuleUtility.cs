using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 为 Prefab 评分规则提供只读的加载、遍历和引用检查能力。
    /// </summary>
    internal static class PrefabRuleUtility
    {
        /// <summary>
        /// 从扫描上下文加载有效的 Prefab 根对象。
        /// </summary>
        internal static bool TryLoadPrefab(
            AssetContext context,
            out GameObject prefab)
        {
            prefab = null;

            if (context == null ||
                string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                context.AssetPath);

            return prefab != null;
        }

        /// <summary>
        /// 获取 Prefab 中每个丢失脚本组件所在的对象位置。
        /// </summary>
        internal static List<PrefabIssueLocation> GetMissingScriptLocations(
            GameObject prefab)
        {
            List<PrefabIssueLocation> locations =
                new List<PrefabIssueLocation>();

            if (prefab == null)
            {
                return locations;
            }

            Transform[] transforms =
                prefab.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < transforms.Length; i++)
            {
                Component[] components = transforms[i].GetComponents<Component>();

                for (int j = 0; j < components.Length; j++)
                {
                    if (components[j] == null)
                    {
                        locations.Add(CreateLocation(
                            prefab.transform,
                            transforms[i],
                            "Missing Script"));
                    }
                }
            }

            return locations;
        }

        /// <summary>
        /// 获取 Prefab 的组件序列化字段中每个失效对象引用的位置。
        /// </summary>
        internal static List<PrefabIssueLocation> GetMissingReferenceLocations(
            GameObject prefab)
        {
            List<PrefabIssueLocation> locations =
                new List<PrefabIssueLocation>();

            if (prefab == null)
            {
                return locations;
            }

            Component[] components =
                prefab.GetComponentsInChildren<Component>(true);

            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];

                if (component == null)
                {
                    continue;
                }

                SerializedObject serializedObject =
                    new SerializedObject(component);

                SerializedProperty property =
                    serializedObject.GetIterator();

                while (property.NextVisible(true))
                {
                    if (property.propertyType ==
                            SerializedPropertyType.ObjectReference &&
                        property.objectReferenceValue == null &&
                        property.objectReferenceInstanceIDValue != 0)
                    {
                        locations.Add(CreateLocation(
                            prefab.transform,
                            component.transform,
                            component.GetType().Name + "." +
                            property.propertyPath));
                    }
                }
            }

            return locations;
        }

        /// <summary>
        /// 获取 Prefab 中指定组件的所有对象位置。
        /// </summary>
        internal static List<PrefabIssueLocation> GetComponentLocations<T>(
            GameObject prefab)
            where T : Component
        {
            List<PrefabIssueLocation> locations =
                new List<PrefabIssueLocation>();

            if (prefab == null)
            {
                return locations;
            }

            T[] components = prefab.GetComponentsInChildren<T>(true);

            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];

                if (component != null)
                {
                    locations.Add(CreateLocation(
                        prefab.transform,
                        component.transform,
                        component.GetType().Name));
                }
            }

            return locations;
        }

        /// <summary>
        /// 获取 Prefab 中全部物理组件所在的对象位置。
        /// </summary>
        internal static List<PrefabIssueLocation> GetPhysicsComponentLocations(
            GameObject prefab)
        {
            List<PrefabIssueLocation> locations =
                new List<PrefabIssueLocation>();

            if (prefab == null)
            {
                return locations;
            }

            Component[] components =
                prefab.GetComponentsInChildren<Component>(true);

            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];

                if (component is Rigidbody ||
                    component is Rigidbody2D ||
                    component is Collider ||
                    component is Collider2D ||
                    component is CharacterController)
                {
                    locations.Add(CreateLocation(
                        prefab.transform,
                        component.transform,
                        component.GetType().Name));
                }
            }

            return locations;
        }

        /// <summary>
        /// 获取 Prefab 从根节点开始计算的最大子节点层级。
        /// </summary>
        internal static int GetMaxHierarchyDepth(GameObject prefab)
        {
            if (prefab == null)
            {
                return 0;
            }

            Transform root = prefab.transform;
            Transform[] transforms =
                prefab.GetComponentsInChildren<Transform>(true);
            int maxDepth = 0;

            for (int i = 0; i < transforms.Length; i++)
            {
                int depth = 0;
                Transform current = transforms[i];

                while (current != null && current != root)
                {
                    depth++;
                    current = current.parent;
                }

                if (depth > maxDepth)
                {
                    maxDepth = depth;
                }
            }

            return maxDepth;
        }

        /// <summary>
        /// 获取 Prefab 中 Animator 和 Legacy Animation 的全部失效绑定位置。
        /// </summary>
        internal static List<PrefabIssueLocation>
            GetMissingAnimationBindingLocations(GameObject prefab)
        {
            List<PrefabIssueLocation> locations =
                new List<PrefabIssueLocation>();

            if (prefab == null)
            {
                return locations;
            }

            Animator[] animators = prefab.GetComponentsInChildren<Animator>(true);

            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];

                if (animator != null)
                {
                    CollectMissingAnimationBindings(
                        prefab.transform,
                        animator.transform,
                        "Animator",
                        GetAnimationClips(
                            animator.runtimeAnimatorController),
                        locations);
                }
            }

            Animation[] legacyAnimations =
                prefab.GetComponentsInChildren<Animation>(true);

            for (int i = 0; i < legacyAnimations.Length; i++)
            {
                Animation legacyAnimation = legacyAnimations[i];

                if (legacyAnimation != null)
                {
                    CollectMissingAnimationBindings(
                        prefab.transform,
                        legacyAnimation.transform,
                        "Animation",
                        GetAnimationClips(legacyAnimation),
                        locations);
                }
            }

            return locations;
        }

        private static AnimationClip[] GetAnimationClips(
            RuntimeAnimatorController controller)
        {
            return controller == null
                ? new AnimationClip[0]
                : controller.animationClips;
        }

        private static AnimationClip[] GetAnimationClips(
            Animation legacyAnimation)
        {
            if (legacyAnimation == null)
            {
                return new AnimationClip[0];
            }

            System.Collections.Generic.HashSet<AnimationClip> clips =
                new System.Collections.Generic.HashSet<AnimationClip>();

            foreach (AnimationState state in legacyAnimation)
            {
                if (state != null && state.clip != null)
                {
                    clips.Add(state.clip);
                }
            }

            if (legacyAnimation.clip != null)
            {
                clips.Add(legacyAnimation.clip);
            }

            AnimationClip[] result = new AnimationClip[clips.Count];
            clips.CopyTo(result);
            return result;
        }

        /// <summary>
        /// 收集一个动画组件引用的 Clip 中无法解析到对象的绑定路径。
        /// </summary>
        private static void CollectMissingAnimationBindings(
            Transform prefabRoot,
            Transform bindingRoot,
            string componentName,
            AnimationClip[] clips,
            List<PrefabIssueLocation> locations)
        {
            if (bindingRoot == null || clips == null || locations == null)
            {
                return;
            }

            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];

                if (clip != null)
                {
                    CollectMissingAnimationBindings(
                        prefabRoot,
                        bindingRoot,
                        componentName,
                        clip,
                        locations);
                }
            }
        }

        /// <summary>
        /// 收集单个动画 Clip 中无法解析到对象的数值与对象引用绑定。
        /// </summary>
        private static void CollectMissingAnimationBindings(
            Transform prefabRoot,
            Transform bindingRoot,
            string componentName,
            AnimationClip clip,
            List<PrefabIssueLocation> locations)
        {
            EditorCurveBinding[] curveBindings =
                AnimationUtility.GetCurveBindings(clip);

            CollectMissingAnimationBindings(
                prefabRoot,
                bindingRoot,
                componentName,
                curveBindings,
                locations);

            EditorCurveBinding[] objectReferenceBindings =
                AnimationUtility.GetObjectReferenceCurveBindings(clip);
            CollectMissingAnimationBindings(
                prefabRoot,
                bindingRoot,
                componentName,
                objectReferenceBindings,
                locations);
        }

        /// <summary>
        /// 把无法解析到当前绑定根节点的路径写入问题定位列表。
        /// </summary>
        private static void CollectMissingAnimationBindings(
            Transform prefabRoot,
            Transform bindingRoot,
            string componentName,
            EditorCurveBinding[] bindings,
            List<PrefabIssueLocation> locations)
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                string path = bindings[i].path;
                Transform target = string.IsNullOrEmpty(path)
                    ? bindingRoot
                    : bindingRoot.Find(path);

                if (target == null)
                {
                    locations.Add(CreateLocation(
                        prefabRoot,
                        bindingRoot,
                        componentName + "." + path));
                }
            }
        }

        /// <summary>
        /// 创建保存对象层级路径与问题说明的定位项。
        /// </summary>
        private static PrefabIssueLocation CreateLocation(
            Transform prefabRoot,
            Transform target,
            string description)
        {
            PrefabIssueLocation location = new PrefabIssueLocation();
            location.HierarchyPath = GetHierarchyPath(prefabRoot, target);
            location.Description = description;
            return location;
        }

        /// <summary>
        /// 获取目标对象相对于 Prefab 根节点的稳定层级路径。
        /// </summary>
        private static string GetHierarchyPath(
            Transform prefabRoot,
            Transform target)
        {
            if (prefabRoot == null || target == null)
            {
                return string.Empty;
            }

            if (target == prefabRoot)
            {
                return string.Empty;
            }

            List<string> names = new List<string>();
            Transform current = target;

            while (current != null && current != prefabRoot)
            {
                names.Add(
                    current.name + "[" + current.GetSiblingIndex() + "]");
                current = current.parent;
            }

            if (current != prefabRoot)
            {
                return string.Empty;
            }

            names.Reverse();
            return string.Join("/", names.ToArray());
        }
    }
}
