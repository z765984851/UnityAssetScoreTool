using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 为 Spine 评分规则提供只读的资源加载、统计和依赖检查能力。
    /// </summary>
    internal static class SpineRuleUtility
    {
        /// <summary>
        /// 从扫描上下文加载有效的 Prefab 根对象。
        /// </summary>
        internal static bool TryLoadPrefab(
            AssetContext context,
            out GameObject prefab)
        {
            prefab = null;

            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                context.AssetPath);

            return prefab != null;
        }

        /// <summary>
        /// 从扫描上下文加载 Spine SkeletonDataAsset。
        /// </summary>
        internal static bool TryLoadSkeleton(
            AssetContext context,
            out SkeletonDataAsset skeletonDataAsset)
        {
            skeletonDataAsset = null;

            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            skeletonDataAsset =
                AssetDatabase.LoadMainAssetAtPath(context.AssetPath)
                    as SkeletonDataAsset;

            return skeletonDataAsset != null;
        }

        /// <summary>
        /// 从扫描上下文加载 Spine 图集资产。
        /// </summary>
        internal static bool TryLoadAtlas(
            AssetContext context,
            out AtlasAssetBase atlasAsset)
        {
            atlasAsset = null;

            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            atlasAsset =
                AssetDatabase.LoadMainAssetAtPath(context.AssetPath)
                    as AtlasAssetBase;

            return atlasAsset != null;
        }

        /// <summary>
        /// 判断 Prefab 是否包含任意引用 SkeletonDataAsset 的 Spine 组件。
        /// </summary>
        internal static bool HasSpineComponent(GameObject prefab)
        {
            if (prefab == null)
            {
                return false;
            }

            Component[] components =
                prefab.GetComponentsInChildren<Component>(true);

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is IHasSkeletonDataAsset)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取骨骼数据中配置的有效 Atlas 数量。
        /// </summary>
        internal static int GetAtlasCount(
            SkeletonDataAsset skeletonDataAsset)
        {
            if (skeletonDataAsset == null ||
                skeletonDataAsset.atlasAssets == null)
            {
                return 0;
            }

            int count = 0;

            for (int i = 0;
                 i < skeletonDataAsset.atlasAssets.Length;
                 i++)
            {
                if (skeletonDataAsset.atlasAssets[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 尝试解析骨骼数据；解析异常或无效数据均视为失败。
        /// </summary>
        internal static bool TryGetSkeletonData(
            SkeletonDataAsset skeletonDataAsset,
            out SkeletonData skeletonData)
        {
            skeletonData = null;

            if (skeletonDataAsset == null)
            {
                return false;
            }

            try
            {
                skeletonData = skeletonDataAsset.GetSkeletonData(true);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning(
                    "读取 Spine SkeletonDataAsset 失败：" +
                    AssetDatabase.GetAssetPath(skeletonDataAsset) +
                    "\n" + exception.Message);
            }

            return skeletonData != null;
        }

        /// <summary>
        /// 统计所有 Skin 中的 Attachment 数量，及其中 MeshAttachment 数量。
        /// </summary>
        internal static void GetAttachmentCounts(
            SkeletonData skeletonData,
            out int attachmentCount,
            out int meshAttachmentCount)
        {
            attachmentCount = 0;
            meshAttachmentCount = 0;

            if (skeletonData == null || skeletonData.Skins == null)
            {
                return;
            }

            for (int i = 0; i < skeletonData.Skins.Count; i++)
            {
                Skin skin = skeletonData.Skins.Items[i];

                if (skin == null || skin.Attachments == null)
                {
                    continue;
                }

                foreach (Skin.SkinEntry entry in skin.Attachments)
                {
                    attachmentCount++;

                    if (entry.Attachment is MeshAttachment)
                    {
                        meshAttachmentCount++;
                    }
                }
            }
        }

        /// <summary>
        /// 判断 SkeletonDataAsset 的骨骼文件、图集、材质或贴图引用是否缺失。
        /// </summary>
        internal static bool HasMissingDependencies(
            SkeletonDataAsset skeletonDataAsset)
        {
            if (skeletonDataAsset == null ||
                skeletonDataAsset.skeletonJSON == null ||
                skeletonDataAsset.atlasAssets == null ||
                skeletonDataAsset.atlasAssets.Length == 0)
            {
                return true;
            }

            for (int i = 0;
                 i < skeletonDataAsset.atlasAssets.Length;
                 i++)
            {
                AtlasAssetBase atlasAsset =
                    skeletonDataAsset.atlasAssets[i];

                if (atlasAsset == null ||
                    atlasAsset.Materials == null)
                {
                    return true;
                }

                bool hasMaterial = false;

                foreach (Material material in atlasAsset.Materials)
                {
                    if (material == null || material.mainTexture == null)
                    {
                        return true;
                    }

                    hasMaterial = true;
                }

                if (!hasMaterial)
                {
                    return true;
                }
            }

            SkeletonData ignored;
            return !TryGetSkeletonData(skeletonDataAsset, out ignored);
        }

        /// <summary>
        /// 获取 Prefab 内 Spine 组件引用的 Atlas 默认材质，并按对象引用去重。
        /// </summary>
        internal static int GetPrefabAtlasMaterialCount(GameObject prefab)
        {
            if (prefab == null)
            {
                return 0;
            }

            Component[] components =
                prefab.GetComponentsInChildren<Component>(true);
            HashSet<Material> materials = new HashSet<Material>();

            for (int i = 0; i < components.Length; i++)
            {
                IHasSkeletonDataAsset component =
                    components[i] as IHasSkeletonDataAsset;
                SkeletonDataAsset skeletonDataAsset =
                    component == null
                        ? null
                        : component.SkeletonDataAsset;

                if (skeletonDataAsset == null ||
                    skeletonDataAsset.atlasAssets == null)
                {
                    continue;
                }

                for (int atlasIndex = 0;
                     atlasIndex < skeletonDataAsset.atlasAssets.Length;
                     atlasIndex++)
                {
                    AtlasAssetBase atlasAsset =
                        skeletonDataAsset.atlasAssets[atlasIndex];

                    if (atlasAsset == null || atlasAsset.Materials == null)
                    {
                        continue;
                    }

                    foreach (Material material in atlasAsset.Materials)
                    {
                        if (material != null)
                        {
                            materials.Add(material);
                        }
                    }
                }
            }

            return materials.Count;
        }
    }
}