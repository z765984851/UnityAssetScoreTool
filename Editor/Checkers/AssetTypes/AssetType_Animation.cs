using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 识别以 AnimationClip 为主资产的独立 .anim 动画资源。
    /// </summary>
    public class AssetType_Animation : IAssetTypeChecker
    {
        private const string AssetTypeIdValue = "animation";

        public string AssetTypeId
        {
            get { return AssetTypeIdValue; }
        }

        /// <summary>
        /// 判断资源是否为独立的 .anim 动画资产。
        /// </summary>
        public bool IsMatch(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            if (!string.Equals(
                    Path.GetExtension(assetPath),
                    ".anim",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return AssetDatabase.LoadMainAssetAtPath(assetPath)
                   is AnimationClip;
        }
    }
}
