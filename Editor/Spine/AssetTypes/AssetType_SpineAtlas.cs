using Spine.Unity;
using UnityEditor;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 识别以 AtlasAssetBase 派生资产为主资产的 Spine 图集资源。
    /// </summary>
    public class AssetType_SpineAtlas : IAssetTypeChecker
    {
        private const string AssetTypeIdValue = "spine_atlas";

        public string AssetTypeId
        {
            get { return AssetTypeIdValue; }
        }

        /// <summary>
        /// 判断主资产是否为 Spine AtlasAssetBase 的具体实现。
        /// </summary>
        public bool IsMatch(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath) &&
                   AssetDatabase.LoadMainAssetAtPath(assetPath)
                       is AtlasAssetBase;
        }
    }
}