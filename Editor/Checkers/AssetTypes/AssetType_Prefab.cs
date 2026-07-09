using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 识别以 GameObject 为主资产的 .prefab 预制体资源。
    /// </summary>
    public class AssetType_Prefab : IAssetTypeChecker
    {
        private const string AssetTypeIdValue = "prefab";

        public string AssetTypeId
        {
            get { return AssetTypeIdValue; }
        }

        /// <summary>
        /// 判断资源是否为独立的 .prefab 预制体资产。
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

            return AssetDatabase.LoadMainAssetAtPath(assetPath) is GameObject;
        }
    }
}
