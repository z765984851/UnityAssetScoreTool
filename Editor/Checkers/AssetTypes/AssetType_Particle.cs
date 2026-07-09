using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 识别层级中包含 ParticleSystem 的粒子预制体资源。
    /// </summary>
    public class AssetType_Particle : IAssetTypeChecker
    {
        private const string AssetTypeIdValue = "particle";

        public string AssetTypeId
        {
            get { return AssetTypeIdValue; }
        }

        /// <summary>
        /// 判断预制体自身或任意未激活子节点中是否包含 ParticleSystem。
        /// </summary>
        public bool IsMatch(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            if (!string.Equals(
                    Path.GetExtension(assetPath),
                    ".prefab",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            GameObject prefab =
                AssetDatabase.LoadMainAssetAtPath(assetPath) as GameObject;

            return prefab != null &&
                   prefab.GetComponentInChildren<ParticleSystem>(true) != null;
        }
    }
}
