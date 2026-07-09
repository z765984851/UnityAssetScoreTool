using System.Collections.Generic;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 一次扫描后生成的最终评分报告。
    /// 
    /// 注意：
    /// Scanner 只负责生成 AssetScanData；
    /// Report 保存评分、等级、严重度、展示根节点等最终结果。
    /// UI 和导出功能后续都应该读取 Report，而不是直接读取 Scanner。
    /// </summary>
    public class AssetScoreReport
    {
        /// <summary>
        /// 原始扫描数据。
        /// </summary>
        public AssetScanData SourceData { get; private set; }

        /// <summary>
        /// 左侧树形列表第一层显示的结果。
        /// 依赖资源通过 AssetContext.Dependencies 递归展开。
        /// </summary>
        public readonly List<AssetScanResult> DisplayRootResults =
            new List<AssetScanResult>();

        /// <summary>
        /// 所有资源的最终评分结果。
        /// Key 是 Unity 资源路径。
        /// </summary>
        public readonly Dictionary<string, AssetScanResult> ResultsByPath =
            new Dictionary<string, AssetScanResult>();

        public AssetScoreReport(AssetScanData sourceData)
        {
            SourceData = sourceData;
        }

        /// <summary>
        /// 写入某个资源的最终扫描结果。
        /// </summary>
        public void SetResult(AssetScanResult result)
        {
            if (result.Context == null || string.IsNullOrEmpty(result.Context.AssetPath))
            {
                return;
            }

            string assetPath = ScanToolUtility.NormalizePath(result.Context.AssetPath);

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            ResultsByPath[assetPath] = result;
        }

        /// <summary>
        /// 根据资源路径获取最终扫描结果。
        /// </summary>
        public bool TryGetResult(string assetPath, out AssetScanResult result)
        {
            result = default;

            assetPath = ScanToolUtility.NormalizePath(assetPath);

            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            return ResultsByPath.TryGetValue(assetPath, out result);
        }

        /// <summary>
        /// 添加一个左侧列表展示根节点。
        /// </summary>
        public void AddDisplayRootResult(AssetScanResult result)
        {
            if (result.Context == null)
            {
                return;
            }

            DisplayRootResults.Add(result);
        }
    }
}