using Spine.Unity;
using UnityEditor;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 识别以 SkeletonDataAsset 为主资产的 Spine 骨骼数据资源。
    /// </summary>
    public class AssetType_SpineSkeleton : IAssetTypeChecker
    {
        private const string AssetTypeIdValue = "spine_skeleton";

        public string AssetTypeId
        {
            get { return AssetTypeIdValue; }
        }

        /// <summary>
        /// 判断主资产是否为 Spine SkeletonDataAsset。
        /// </summary>
        public bool IsMatch(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath) &&
                   AssetDatabase.LoadMainAssetAtPath(assetPath)
                       is SkeletonDataAsset;
        }
    }
}