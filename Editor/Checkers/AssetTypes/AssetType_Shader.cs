using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 识别以 Shader 为主资产的着色器资源。
    /// </summary>
    public class AssetType_Shader : IAssetTypeChecker
    {
        private const string AssetTypeIdValue = "shader";

        public string AssetTypeId
        {
            get { return AssetTypeIdValue; }
        }

        /// <summary>
        /// 判断资源的主资产是否为 Shader。
        /// </summary>
        public bool IsMatch(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            return AssetDatabase.LoadMainAssetAtPath(assetPath) is Shader;
        }
    }
}
