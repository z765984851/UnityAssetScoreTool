using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 识别以 Material 为主资产的材质资源。
    /// </summary>
    public class AssetType_Material : IAssetTypeChecker
    {
        private const string AssetTypeIdValue = "material";

        public string AssetTypeId
        {
            get { return AssetTypeIdValue; }
        }

        /// <summary>
        /// 判断资源的主资产是否为 Material。
        /// </summary>
        public bool IsMatch(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            return AssetDatabase.LoadMainAssetAtPath(assetPath) is Material;
        }
    }
}
