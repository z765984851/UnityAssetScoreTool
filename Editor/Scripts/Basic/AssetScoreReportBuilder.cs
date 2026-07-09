using System.Collections.Generic;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 评分报告构建器。
    /// 
    /// 职责：
    /// 1. 从 AssetScanData 读取原始扫描数据。
    /// 2. 为每个资源生成自身评分结果。
    /// 3. 按依赖关系生成汇总评分结果。
    /// 4. 计算左侧树形列表需要显示的根节点。
    /// </summary>
    public static class AssetScoreReportBuilder
    {
        public static AssetScoreReport Build(AssetScanData scanData)
        {
            AssetScoreReport report = new AssetScoreReport(scanData);

            if (scanData == null)
            {
                return report;
            }

            BuildSelfResults(scanData, report);
            BuildAggregateResults(scanData, report);
            BuildDisplayRootResults(scanData, report);

            return report;
        }

        /// <summary>
        /// 为每个资源生成自身评分结果。
        /// 这里只读取资源自身问题，不处理依赖汇总。
        /// </summary>
        private static void BuildSelfResults(
            AssetScanData scanData,
            AssetScoreReport report)
        {
            foreach (KeyValuePair<string, AssetContext> pair in scanData.ContextsByPath)
            {
                string assetPath = pair.Key;
                AssetContext context = pair.Value;

                if (context == null)
                {
                    continue;
                }

                List<AssetIssueResult> issues = scanData.GetIssues(assetPath);

                AssetScanResult selfResult =
                    AssetScoreCalculator.BuildSelfResult(context, issues);

                report.SetResult(selfResult);
            }
        }

        /// <summary>
        /// 根据依赖关系为每个资源生成汇总评分结果。
        /// </summary>
        private static void BuildAggregateResults(
            AssetScanData scanData,
            AssetScoreReport report)
        {
            HashSet<string> completedPaths = new HashSet<string>();
            HashSet<string> visitingPaths = new HashSet<string>();

            for (int i = 0; i < scanData.RootContexts.Count; i++)
            {
                AssetContext rootContext = scanData.RootContexts[i];

                BuildAggregateResultInternal(
                    rootContext,
                    report,
                    completedPaths,
                    visitingPaths);
            }
        }

        /// <summary>
        /// 递归生成单个资源的汇总结果。
        /// 子节点会先处理，父节点再根据完整依赖子树生成汇总结果。
        /// </summary>
        private static bool BuildAggregateResultInternal(
            AssetContext context,
            AssetScoreReport report,
            HashSet<string> completedPaths,
            HashSet<string> visitingPaths)
        {
            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            string assetPath = ScanToolUtility.NormalizePath(context.AssetPath);

            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            if (completedPaths.Contains(assetPath))
            {
                return true;
            }

            // 防止异常循环依赖导致递归死循环。
            if (visitingPaths.Contains(assetPath))
            {
                return report.ResultsByPath.ContainsKey(assetPath);
            }

            visitingPaths.Add(assetPath);

            if (context.Dependencies != null)
            {
                for (int i = 0; i < context.Dependencies.Count; i++)
                {
                    BuildAggregateResultInternal(
                        context.Dependencies[i],
                        report,
                        completedPaths,
                        visitingPaths);
                }
            }

            AssetScanResult selfResult;
            if (!report.TryGetResult(assetPath, out selfResult))
            {
                visitingPaths.Remove(assetPath);
                return false;
            }

            HashSet<string> collectedDependencyPaths = new HashSet<string>();
            List<AssetScanResult> uniqueDependencyResults =
                new List<AssetScanResult>();

            CollectUniqueDependencyResults(
                context,
                report,
                collectedDependencyPaths,
                uniqueDependencyResults);

            AssetScanResult aggregateResult =
                AssetScoreCalculator.BuildAggregateResult(
                    selfResult,
                    uniqueDependencyResults);

            report.SetResult(aggregateResult);

            visitingPaths.Remove(assetPath);
            completedPaths.Add(assetPath);

            return true;
        }

        /// <summary>
        /// 收集当前资源完整依赖子树中的唯一依赖结果。
        /// 
        /// 注意：
        /// 这里收集的是所有唯一依赖资源；
        /// AssetScoreCalculator 会读取这些依赖资源的 SelfXXX 字段，
        /// 避免深层依赖被重复统计。
        /// </summary>
        private static void CollectUniqueDependencyResults(
            AssetContext context,
            AssetScoreReport report,
            HashSet<string> collectedDependencyPaths,
            List<AssetScanResult> results)
        {
            if (context == null || context.Dependencies == null)
            {
                return;
            }

            for (int i = 0; i < context.Dependencies.Count; i++)
            {
                AssetContext dependencyContext = context.Dependencies[i];

                if (dependencyContext == null ||
                    string.IsNullOrEmpty(dependencyContext.AssetPath))
                {
                    continue;
                }

                string dependencyPath =
                    ScanToolUtility.NormalizePath(dependencyContext.AssetPath);

                if (string.IsNullOrEmpty(dependencyPath))
                {
                    continue;
                }

                if (collectedDependencyPaths.Contains(dependencyPath))
                {
                    continue;
                }

                collectedDependencyPaths.Add(dependencyPath);

                AssetScanResult dependencyResult;
                if (report.TryGetResult(dependencyPath, out dependencyResult))
                {
                    results.Add(dependencyResult);
                }

                CollectUniqueDependencyResults(
                    dependencyContext,
                    report,
                    collectedDependencyPaths,
                    results);
            }
        }

        /// <summary>
        /// 构建左侧树形列表第一层展示的根节点。
        /// 如果某个入口资源已经是其他入口资源的依赖，则不重复显示到第一层。
        /// </summary>
        private static void BuildDisplayRootResults(
            AssetScanData scanData,
            AssetScoreReport report)
        {
            HashSet<string> dependencyPaths = new HashSet<string>();
            HashSet<string> visitedPaths = new HashSet<string>();

            for (int i = 0; i < scanData.RootContexts.Count; i++)
            {
                CollectDependencyPaths(
                    scanData.RootContexts[i],
                    dependencyPaths,
                    visitedPaths);
            }

            List<AssetScanResult> fallbackResults = new List<AssetScanResult>();
            HashSet<string> addedRootPaths = new HashSet<string>();

            for (int i = 0; i < scanData.RootContexts.Count; i++)
            {
                AssetContext context = scanData.RootContexts[i];

                if (context == null || string.IsNullOrEmpty(context.AssetPath))
                {
                    continue;
                }

                string assetPath = ScanToolUtility.NormalizePath(context.AssetPath);

                if (addedRootPaths.Contains(assetPath))
                {
                    continue;
                }

                AssetScanResult result;
                if (!report.TryGetResult(assetPath, out result))
                {
                    continue;
                }

                addedRootPaths.Add(assetPath);
                fallbackResults.Add(result);

                if (dependencyPaths.Contains(assetPath))
                {
                    continue;
                }

                report.AddDisplayRootResult(result);
            }

            // 防御性兜底：如果所有入口都被判定为依赖，至少显示原始入口资源。
            if (report.DisplayRootResults.Count == 0)
            {
                for (int i = 0; i < fallbackResults.Count; i++)
                {
                    report.AddDisplayRootResult(fallbackResults[i]);
                }
            }
        }

        /// <summary>
        /// 收集所有依赖路径。
        /// 当前 context 自己不会被加入 dependencyPaths。
        /// </summary>
        private static void CollectDependencyPaths(
            AssetContext context,
            HashSet<string> dependencyPaths,
            HashSet<string> visitedPaths)
        {
            if (context == null || context.Dependencies == null)
            {
                return;
            }

            string ownerPath = ScanToolUtility.NormalizePath(context.AssetPath);

            if (!string.IsNullOrEmpty(ownerPath))
            {
                if (visitedPaths.Contains(ownerPath))
                {
                    return;
                }

                visitedPaths.Add(ownerPath);
            }

            for (int i = 0; i < context.Dependencies.Count; i++)
            {
                AssetContext dependencyContext = context.Dependencies[i];

                if (dependencyContext == null ||
                    string.IsNullOrEmpty(dependencyContext.AssetPath))
                {
                    continue;
                }

                string dependencyPath =
                    ScanToolUtility.NormalizePath(dependencyContext.AssetPath);

                if (string.IsNullOrEmpty(dependencyPath))
                {
                    continue;
                }

                dependencyPaths.Add(dependencyPath);

                CollectDependencyPaths(
                    dependencyContext,
                    dependencyPaths,
                    visitedPaths);
            }
        }
    }
}