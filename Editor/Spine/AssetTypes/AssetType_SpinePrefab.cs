using System;
using System.IO;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 识别层级中包含 Spine 骨骼组件的预制体资源。
    /// </summary>
    public class AssetType_SpinePrefab : IAssetTypeChecker
    {
        private const string AssetTypeIdValue = "spine_prefab";

        public string AssetTypeId
        {
            get { return AssetTypeIdValue; }
        }

        /// <summary>
        /// 判断 Prefab 自身或任意未激活子节点中是否包含 Spine 骨骼组件。
        /// </summary>
        public bool IsMatch(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) ||
                !string.Equals(
                    Path.GetExtension(assetPath),
                    ".prefab",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            GameObject prefab =
                AssetDatabase.LoadMainAssetAtPath(assetPath)
                    as GameObject;

            return prefab != null &&
                   SpineRuleUtility.HasSpineComponent(prefab);
        }
    }
}