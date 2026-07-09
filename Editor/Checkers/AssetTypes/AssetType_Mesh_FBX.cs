using System;
using System.IO;
using UnityEditor;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 识别由 ModelImporter 导入的 FBX 模型资源。
    /// </summary>
    public class AssetType_Mesh_FBX : IAssetTypeChecker
    {
        private const string AssetTypeIdValue = "mesh_fbx";

        public string AssetTypeId
        {
            get { return AssetTypeIdValue; }
        }

        /// <summary>
        /// 判断资源是否为扩展名是 .fbx 的模型导入资源。
        /// </summary>
        public bool IsMatch(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            if (!string.Equals(
                    Path.GetExtension(assetPath),
                    ".fbx",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return AssetImporter.GetAtPath(assetPath) is ModelImporter;
        }
    }
}
