using System.Collections.Generic;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 资产评分计算器。
    /// 负责当前资源自身评分，以及当前资源加依赖后的汇总评分。
    /// </summary>
    public static class AssetScoreCalculator
    {
        private const int FullScore = 100;

        /// <summary>
        /// 根据当前资源自身问题生成基础扫描结果。
        /// </summary>
        public static AssetScanResult BuildSelfResult(
            AssetContext context,
            List<AssetIssueResult> issues)
        {
            AssetScanResult result = new AssetScanResult();

            result.Context = context;
            result.Issues = issues ?? new List<AssetIssueResult>();

            result.SelfIssueCount = result.Issues.Count;
            result.SelfHighestIssueLevel = CalculateHighestIssueLevel(result.Issues);
            result.SelfScore = CalculateScore(result.Issues);
            result.SelfGrade = CalculateGrade(result.SelfScore, result.SelfHighestIssueLevel);

            // 初始状态下，汇总结果等于自身结果。
            // 后续 AssetScoreReportBuilder 会在依赖扫描完成后再向上汇总。
            result.IssueCount = result.SelfIssueCount;
            result.HighestIssueLevel = result.SelfHighestIssueLevel;
            result.Score = result.SelfScore;
            result.Grade = result.SelfGrade;

            return result;
        }

        /// <summary>
        /// 根据当前资源自身结果和所有已去重的依赖资源自身结果，生成汇总结果。
        /// dependencyResults 应该由 AssetScoreReportBuilder 收集完整依赖子树并按资源路径去重。
        /// 这里必须只读取依赖资源的 SelfXXX 字段，避免深层依赖被重复统计。
        /// </summary>
        public static AssetScanResult BuildAggregateResult(
            AssetScanResult selfResult,
            List<AssetScanResult> dependencyResults)
        {
            int totalDeductScore = FullScore - selfResult.SelfScore;
            int totalIssueCount = selfResult.SelfIssueCount;
            EIssueLevel highestIssueLevel = selfResult.SelfHighestIssueLevel;

            if (dependencyResults != null)
            {
                for (int i = 0; i < dependencyResults.Count; i++)
                {
                    AssetScanResult dependencyResult = dependencyResults[i];

                    totalDeductScore += FullScore - dependencyResult.SelfScore;
                    totalIssueCount += dependencyResult.SelfIssueCount;

                    highestIssueLevel = GetHigherIssueLevel(
                        highestIssueLevel,
                        dependencyResult.SelfHighestIssueLevel);
                }
            }

            selfResult.IssueCount = totalIssueCount;
            selfResult.HighestIssueLevel = totalIssueCount > 0
                ? highestIssueLevel
                : EIssueLevel.Info;

            if (selfResult.HighestIssueLevel == EIssueLevel.Blocker)
            {
                selfResult.Score = 0;
            }
            else
            {
                selfResult.Score = FullScore - totalDeductScore;

                if (selfResult.Score < 0)
                {
                    selfResult.Score = 0;
                }
            }

            selfResult.Grade = CalculateGrade(
                selfResult.Score,
                selfResult.HighestIssueLevel);

            return selfResult;
        }

        private static int CalculateScore(List<AssetIssueResult> issues)
        {
            if (issues == null || issues.Count == 0)
            {
                return FullScore;
            }

            int score = FullScore;
            bool hasBlocker = false;

            for (int i = 0; i < issues.Count; i++)
            {
                AssetIssueResult issueResult = issues[i];

                if (issueResult == null || issueResult.IssueInfo == null)
                {
                    continue;
                }

                IssueInfo issueInfo = issueResult.IssueInfo;

                if (issueInfo.issueLevel == EIssueLevel.Blocker)
                {
                    hasBlocker = true;
                }

                if (issueInfo.deductScore > 0)
                {
                    score -= issueInfo.deductScore;
                }
            }

            if (hasBlocker)
            {
                return 0;
            }

            return score < 0 ? 0 : score;
        }

        private static EAssetGrade CalculateGrade(int score, EIssueLevel highestIssueLevel)
        {
            if (highestIssueLevel == EIssueLevel.Blocker)
            {
                return EAssetGrade.D;
            }

            if (score >= 90)
            {
                return EAssetGrade.A;
            }

            if (score >= 80)
            {
                return EAssetGrade.B;
            }

            if (score >= 60)
            {
                return EAssetGrade.C;
            }

            return EAssetGrade.D;
        }

        private static EIssueLevel CalculateHighestIssueLevel(List<AssetIssueResult> issues)
        {
            if (issues == null || issues.Count == 0)
            {
                return EIssueLevel.Info;
            }

            EIssueLevel highestLevel = EIssueLevel.Info;

            for (int i = 0; i < issues.Count; i++)
            {
                AssetIssueResult issueResult = issues[i];

                if (issueResult == null || issueResult.IssueInfo == null)
                {
                    continue;
                }

                highestLevel = GetHigherIssueLevel(
                    highestLevel,
                    issueResult.IssueInfo.issueLevel);
            }

            return highestLevel;
        }

        private static EIssueLevel GetHigherIssueLevel(
            EIssueLevel left,
            EIssueLevel right)
        {
            int leftPriority = GetIssueLevelPriority(left);
            int rightPriority = GetIssueLevelPriority(right);

            return rightPriority > leftPriority ? right : left;
        }

        private static int GetIssueLevelPriority(EIssueLevel issueLevel)
        {
            switch (issueLevel)
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
                    return 0;
            }
        }
    }
}