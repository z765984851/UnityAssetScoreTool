using System;
using System.Collections.Generic;
using UnityEditor;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 资产评分白名单过滤器。
    ///
    /// Scanner 仍然保留完整的原始扫描结果。
    /// ReportBuilder 在计算分数前调用这里，
    /// 生成应用白名单后的 AssetScanData。
    /// </summary>
    public static class AssetScoreWhiteListFilter
    {
        /// <summary>
        /// 根据 RuleApplyConfig 白名单生成报告使用的数据。
        ///
        /// 返回的新 AssetScanData：
        /// 1. 保留原有 RootContexts。
        /// 2. 保留原有 ContextsByPath。
        /// 3. 移除被白名单命中的 AssetIssueResult。
        /// </summary>
        public static AssetScanData Apply(
            AssetScanData sourceData,
            RuleApplyConfig ruleApplyConfig)
        {
            if (sourceData == null)
            {
                return null;
            }

            AssetScanData filteredData = new AssetScanData();

            CopyContexts(sourceData, filteredData);
            CopyFilteredIssues(
                sourceData,
                filteredData,
                ruleApplyConfig);

            return filteredData;
        }

        /// <summary>
        /// 复制入口资源和全部资源上下文。
        /// AssetContext 本身不修改，因此这里复用原对象。
        /// </summary>
        private static void CopyContexts(
            AssetScanData sourceData,
            AssetScanData targetData)
        {
            for (int i = 0; i < sourceData.RootContexts.Count; i++)
            {
                AssetContext rootContext = sourceData.RootContexts[i];

                if (rootContext == null)
                {
                    continue;
                }

                targetData.AddRootContext(rootContext);
            }

            foreach (KeyValuePair<string, AssetContext> pair
                     in sourceData.ContextsByPath)
            {
                AssetContext context = pair.Value;

                if (context == null)
                {
                    continue;
                }

                targetData.AddContext(context);
            }
        }

        /// <summary>
        /// 为每个资源复制应用白名单后的自身问题列表。
        /// </summary>
        private static void CopyFilteredIssues(
            AssetScanData sourceData,
            AssetScanData targetData,
            RuleApplyConfig ruleApplyConfig)
        {
            foreach (KeyValuePair<string, AssetContext> pair
                     in sourceData.ContextsByPath)
            {
                string assetPath =
                    ScanToolUtility.NormalizePath(pair.Key);

                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                List<AssetIssueResult> sourceIssues =
                    sourceData.GetIssues(assetPath);

                List<AssetIssueResult> filteredIssues =
                    FilterIssues(
                        assetPath,
                        sourceIssues,
                        ruleApplyConfig);

                targetData.SetIssues(
                    assetPath,
                    filteredIssues);
            }
        }

        /// <summary>
        /// 过滤单个资源的问题列表。
        /// </summary>
        private static List<AssetIssueResult> FilterIssues(
            string assetPath,
            List<AssetIssueResult> sourceIssues,
            RuleApplyConfig ruleApplyConfig)
        {
            List<AssetIssueResult> filteredIssues =
                new List<AssetIssueResult>();

            if (sourceIssues == null ||
                sourceIssues.Count == 0)
            {
                return filteredIssues;
            }

            WhiteListItem[] whiteListItems =
                ruleApplyConfig == null
                    ? null
                    : ruleApplyConfig.WhiteListItems;

            if (whiteListItems == null ||
                whiteListItems.Length == 0)
            {
                filteredIssues.AddRange(sourceIssues);
                return filteredIssues;
            }

            for (int i = 0; i < sourceIssues.Count; i++)
            {
                AssetIssueResult issueResult =
                    sourceIssues[i];

                if (issueResult == null)
                {
                    continue;
                }

                if (IsIssueIgnored(
                        assetPath,
                        issueResult,
                        whiteListItems))
                {
                    continue;
                }

                filteredIssues.Add(issueResult);
            }

            return filteredIssues;
        }

        /// <summary>
        /// 判断问题是否命中任意一条白名单。
        /// </summary>
        private static bool IsIssueIgnored(
            string assetPath,
            AssetIssueResult issueResult,
            WhiteListItem[] whiteListItems)
        {
            for (int i = 0; i < whiteListItems.Length; i++)
            {
                WhiteListItem whiteListItem = whiteListItems[i];

                if (DoesWhiteListItemMatch(
                        assetPath,
                        issueResult,
                        whiteListItem))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断单条白名单是否命中当前问题。
        /// </summary>
        private static bool DoesWhiteListItemMatch(
            string assetPath,
            AssetIssueResult issueResult,
            WhiteListItem whiteListItem)
        {
            if (whiteListItem == null)
            {
                return false;
            }

            if (!DoesAssetPathMatch(
                    assetPath,
                    whiteListItem.AssetPath))
            {
                return false;
            }

            if (!DoesRuleMatch(
                    issueResult,
                    whiteListItem.RuleConfig))
            {
                return false;
            }

            if (!DoesIssueInfoMatch(
                    issueResult,
                    whiteListItem.IssueInfoId))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断资源路径是否匹配。
        ///
        /// 白名单配置为文件：
        /// 只匹配该文件。
        ///
        /// 白名单配置为文件夹：
        /// 匹配文件夹本身和该文件夹下的所有资源。
        /// </summary>
        private static bool DoesAssetPathMatch(
            string assetPath,
            string whiteListAssetPath)
        {
            assetPath = ScanToolUtility.NormalizePath(assetPath);
            whiteListAssetPath =
                ScanToolUtility.NormalizePath(whiteListAssetPath);

            if (string.IsNullOrEmpty(assetPath) ||
                string.IsNullOrEmpty(whiteListAssetPath))
            {
                return false;
            }

            if (string.Equals(
                    assetPath,
                    whiteListAssetPath,
                    StringComparison.Ordinal))
            {
                return true;
            }

            if (!AssetDatabase.IsValidFolder(whiteListAssetPath))
            {
                return false;
            }

            string folderPrefix = whiteListAssetPath + "/";

            return assetPath.StartsWith(
                folderPrefix,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// 判断规则是否匹配。
        /// 白名单未指定 RuleConfig 时匹配所有规则。
        /// </summary>
        private static bool DoesRuleMatch(
            AssetIssueResult issueResult,
            ScoreRuleConfig whiteListRuleConfig)
        {
            if (whiteListRuleConfig == null)
            {
                return true;
            }

            if (issueResult == null || issueResult.RuleConfig == null)
            {
                return false;
            }

            if (issueResult.RuleConfig == whiteListRuleConfig)
            {
                return true;
            }

            string issueRuleId = issueResult.RuleConfig.RuleId;
            string whiteListRuleId = whiteListRuleConfig.RuleId;

            if (string.IsNullOrEmpty(issueRuleId) ||
                string.IsNullOrEmpty(whiteListRuleId))
            {
                return false;
            }

            return string.Equals(
                issueRuleId,
                whiteListRuleId,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// 判断 IssueInfo 是否匹配。
        /// 白名单未指定 IssueInfoId 时匹配规则下所有问题。
        /// </summary>
        private static bool DoesIssueInfoMatch(
            AssetIssueResult issueResult,
            string whiteListIssueInfoId)
        {
            if (string.IsNullOrEmpty(whiteListIssueInfoId))
            {
                return true;
            }

            if (issueResult == null || issueResult.IssueInfo == null)
            {
                return false;
            }

            return string.Equals(
                issueResult.IssueInfo.issueInfoId,
                whiteListIssueInfoId,
                StringComparison.Ordinal);
        }
    }
}
