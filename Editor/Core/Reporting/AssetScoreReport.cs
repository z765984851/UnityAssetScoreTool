using System.Collections.Generic;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 报告结果的排序方式。
    /// </summary>
    public enum EAssetScoreReportSortMode
    {
        Path,
        ScoreAscending,
        ScoreDescending,
        IssueCountDescending,
        SeverityDescending
    }

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

        /// <summary>
        /// 获取报告中所有资源类型的显示名称，并按名称排序。
        /// </summary>
        public List<string> GetAssetTypeNames()
        {
            HashSet<string> typeNames = new HashSet<string>();

            foreach (AssetScanResult result in ResultsByPath.Values)
            {
                if (result.Context == null ||
                    result.Context.AssetTypes == null)
                {
                    continue;
                }

                for (int i = 0;
                     i < result.Context.AssetTypes.Count;
                     i++)
                {
                    AssetTypeConfig assetType =
                        result.Context.AssetTypes[i];

                    if (assetType == null)
                    {
                        continue;
                    }

                    string typeName =
                        string.IsNullOrEmpty(assetType.DisplayName)
                            ? assetType.AssetTypeId
                            : assetType.DisplayName;

                    if (!string.IsNullOrEmpty(typeName))
                    {
                        typeNames.Add(typeName);
                    }
                }
            }

            List<string> sortedTypeNames =
                new List<string>(typeNames);
            sortedTypeNames.Sort();

            return sortedTypeNames;
        }

        /// <summary>
        /// 获取通过筛选且按指定方式排序后的展示根节点结果。
        /// 无问题筛选以根节点汇总问题数为准，其他严重度筛选以节点自身问题为准。
        /// </summary>
        public List<AssetScanResult> GetDisplayRootResults(
            string assetTypeName,
            EIssueLevel? selfIssueLevel,
            bool onlyNoIssues,
            EAssetScoreReportSortMode sortMode)
        {
            List<AssetScanResult> results =
                new List<AssetScanResult>();

            for (int i = 0; i < DisplayRootResults.Count; i++)
            {
                AssetScanResult result = DisplayRootResults[i];

                if (result.Context == null ||
                    !DoesTreePassFilters(
                        result.Context,
                        assetTypeName,
                        selfIssueLevel,
                        onlyNoIssues))
                {
                    continue;
                }

                results.Add(result);
            }

            results.Sort((left, right) =>
                CompareContexts(left.Context, right.Context, sortMode));

            return results;
        }

        /// <summary>
        /// 获取按指定方式排序后的直接依赖列表，不修改原始依赖关系。
        /// </summary>
        public List<AssetContext> GetSortedDependencies(
            AssetContext context,
            EAssetScoreReportSortMode sortMode)
        {
            List<AssetContext> dependencies =
                new List<AssetContext>();

            if (context == null || context.Dependencies == null)
            {
                return dependencies;
            }

            for (int i = 0; i < context.Dependencies.Count; i++)
            {
                AssetContext dependency = context.Dependencies[i];

                if (dependency != null)
                {
                    dependencies.Add(dependency);
                }
            }

            dependencies.Sort((left, right) =>
                CompareContexts(left, right, sortMode));

            return dependencies;
        }

        /// <summary>
        /// 获取指定资源自身问题；报告没有原始数据时返回空列表。
        /// </summary>
        public List<AssetIssueResult> GetSelfIssues(
            AssetContext context)
        {
            if (context == null ||
                string.IsNullOrEmpty(context.AssetPath) ||
                SourceData == null)
            {
                return new List<AssetIssueResult>();
            }

            return SourceData.GetIssues(context.AssetPath);
        }

        /// <summary>
        /// 判断整棵依赖树是否通过当前报告筛选条件。
        /// </summary>
        private bool DoesTreePassFilters(
            AssetContext rootContext,
            string assetTypeName,
            EIssueLevel? selfIssueLevel,
            bool onlyNoIssues)
        {
            if (rootContext == null ||
                string.IsNullOrEmpty(rootContext.AssetPath))
            {
                return false;
            }

            if (onlyNoIssues)
            {
                return DoesRootHaveNoIssues(rootContext) &&
                       DoesTreeContainAssetType(
                           rootContext,
                           assetTypeName,
                           new HashSet<string>());
            }

            return DoesTreeContainMatchingContext(
                rootContext,
                assetTypeName,
                selfIssueLevel,
                new HashSet<string>());
        }

        /// <summary>
        /// 判断依赖树中是否存在同时通过资源类型和自身问题筛选的节点。
        /// </summary>
        private bool DoesTreeContainMatchingContext(
            AssetContext context,
            string assetTypeName,
            EIssueLevel? selfIssueLevel,
            HashSet<string> pathStack)
        {
            if (!TryEnterContext(context, pathStack))
            {
                return false;
            }

            string assetPath =
                ScanToolUtility.NormalizePath(context.AssetPath);

            if (DoesContextMatchAssetType(context, assetTypeName) &&
                DoesContextMatchSelfIssueLevel(context, selfIssueLevel))
            {
                pathStack.Remove(assetPath);
                return true;
            }

            if (context.Dependencies != null)
            {
                for (int i = 0; i < context.Dependencies.Count; i++)
                {
                    if (DoesTreeContainMatchingContext(
                            context.Dependencies[i],
                            assetTypeName,
                            selfIssueLevel,
                            pathStack))
                    {
                        pathStack.Remove(assetPath);
                        return true;
                    }
                }
            }

            pathStack.Remove(assetPath);
            return false;
        }

        /// <summary>
        /// 判断依赖树中是否存在符合资源类型筛选的节点。
        /// </summary>
        private bool DoesTreeContainAssetType(
            AssetContext context,
            string assetTypeName,
            HashSet<string> pathStack)
        {
            if (string.IsNullOrEmpty(assetTypeName))
            {
                return true;
            }

            if (!TryEnterContext(context, pathStack))
            {
                return false;
            }

            string assetPath =
                ScanToolUtility.NormalizePath(context.AssetPath);

            if (DoesContextMatchAssetType(context, assetTypeName))
            {
                pathStack.Remove(assetPath);
                return true;
            }

            if (context.Dependencies != null)
            {
                for (int i = 0; i < context.Dependencies.Count; i++)
                {
                    if (DoesTreeContainAssetType(
                            context.Dependencies[i],
                            assetTypeName,
                            pathStack))
                    {
                        pathStack.Remove(assetPath);
                        return true;
                    }
                }
            }

            pathStack.Remove(assetPath);
            return false;
        }

        /// <summary>
        /// 将有效且未在当前递归路径中的资源加入循环保护集合。
        /// </summary>
        private bool TryEnterContext(
            AssetContext context,
            HashSet<string> pathStack)
        {
            if (context == null ||
                string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            string assetPath =
                ScanToolUtility.NormalizePath(context.AssetPath);

            if (string.IsNullOrEmpty(assetPath) ||
                pathStack.Contains(assetPath))
            {
                return false;
            }

            pathStack.Add(assetPath);
            return true;
        }

        /// <summary>
        /// 判断单个资源是否符合指定的资源类型名称。
        /// </summary>
        private bool DoesContextMatchAssetType(
            AssetContext context,
            string assetTypeName)
        {
            if (string.IsNullOrEmpty(assetTypeName))
            {
                return true;
            }

            if (context == null || context.AssetTypes == null)
            {
                return false;
            }

            for (int i = 0; i < context.AssetTypes.Count; i++)
            {
                AssetTypeConfig assetType = context.AssetTypes[i];

                if (assetType == null)
                {
                    continue;
                }

                string typeName =
                    string.IsNullOrEmpty(assetType.DisplayName)
                        ? assetType.AssetTypeId
                        : assetType.DisplayName;

                if (typeName == assetTypeName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断单个资源自身问题是否符合指定严重度；空值表示不过滤严重度。
        /// </summary>
        private bool DoesContextMatchSelfIssueLevel(
            AssetContext context,
            EIssueLevel? selfIssueLevel)
        {
            if (!selfIssueLevel.HasValue)
            {
                return true;
            }

            AssetScanResult result;

            if (!TryGetResult(context == null
                    ? string.Empty
                    : context.AssetPath,
                    out result) ||
                result.SelfIssueCount <= 0)
            {
                return false;
            }

            return result.SelfHighestIssueLevel ==
                   selfIssueLevel.Value;
        }

        /// <summary>
        /// 判断根节点及其完整依赖树是否没有任何汇总问题。
        /// </summary>
        private bool DoesRootHaveNoIssues(AssetContext rootContext)
        {
            AssetScanResult result;

            return TryGetResult(rootContext.AssetPath, out result) &&
                   result.IssueCount <= 0;
        }

        /// <summary>
        /// 根据指定排序方式比较两个资源上下文。
        /// </summary>
        private int CompareContexts(
            AssetContext left,
            AssetContext right,
            EAssetScoreReportSortMode sortMode)
        {
            AssetScanResult leftResult;
            bool hasLeftResult = TryGetResult(
                left == null ? string.Empty : left.AssetPath,
                out leftResult);

            AssetScanResult rightResult;
            bool hasRightResult = TryGetResult(
                right == null ? string.Empty : right.AssetPath,
                out rightResult);

            int comparison;

            switch (sortMode)
            {
                case EAssetScoreReportSortMode.ScoreAscending:
                    comparison = GetCompareScore(
                        hasLeftResult,
                        leftResult).CompareTo(GetCompareScore(
                        hasRightResult,
                        rightResult));
                    break;

                case EAssetScoreReportSortMode.ScoreDescending:
                    comparison = GetCompareScore(
                        hasRightResult,
                        rightResult).CompareTo(GetCompareScore(
                        hasLeftResult,
                        leftResult));
                    break;

                case EAssetScoreReportSortMode.IssueCountDescending:
                    comparison = GetCompareIssueCount(
                        hasRightResult,
                        rightResult).CompareTo(GetCompareIssueCount(
                        hasLeftResult,
                        leftResult));
                    break;

                case EAssetScoreReportSortMode.SeverityDescending:
                    comparison = GetCompareSeverity(
                        hasRightResult,
                        rightResult).CompareTo(GetCompareSeverity(
                        hasLeftResult,
                        leftResult));
                    break;

                case EAssetScoreReportSortMode.Path:
                default:
                    comparison = 0;
                    break;
            }

            return comparison != 0
                ? comparison
                : CompareContextPath(left, right);
        }

        /// <summary>
        /// 获取资源用于分数排序的最终分数。
        /// </summary>
        private int GetCompareScore(
            bool hasResult,
            AssetScanResult result)
        {
            return hasResult ? result.Score : 0;
        }

        /// <summary>
        /// 获取资源用于问题数排序的汇总问题数量。
        /// </summary>
        private int GetCompareIssueCount(
            bool hasResult,
            AssetScanResult result)
        {
            return hasResult ? result.IssueCount : 0;
        }

        /// <summary>
        /// 获取资源用于严重度排序的汇总严重度权重。
        /// </summary>
        private int GetCompareSeverity(
            bool hasResult,
            AssetScanResult result)
        {
            if (!hasResult || result.IssueCount <= 0)
            {
                return -1;
            }

            switch (result.HighestIssueLevel)
            {
                case EIssueLevel.Info:
                    return 0;

                case EIssueLevel.Warning:
                    return 1;

                case EIssueLevel.Error:
                    return 2;

                case EIssueLevel.Blocker:
                    return 3;

                default:
                    return -1;
            }
        }

        /// <summary>
        /// 使用资源路径作为所有排序方式的稳定次级排序条件。
        /// </summary>
        private int CompareContextPath(
            AssetContext left,
            AssetContext right)
        {
            string leftPath = left == null
                ? string.Empty
                : left.AssetPath;
            string rightPath = right == null
                ? string.Empty
                : right.AssetPath;

            return string.Compare(
                leftPath,
                rightPath,
                System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
