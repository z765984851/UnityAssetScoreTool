using Spine.Unity;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 为 Spine 评分规则提供规则标识、适用性和问题项返回的公共实现。
    /// </summary>
    public abstract class SpineRuleCheckerBase : IScoreRuleChecker
    {
        protected abstract string RuleIdValue { get; }

        public string RuleId
        {
            get { return RuleIdValue; }
        }

        /// <summary>
        /// 判断当前资源是否满足该 Spine 规则的适用条件。
        /// </summary>
        public virtual bool ShouldCheck(AssetContext context)
        {
            SkeletonDataAsset skeletonDataAsset;
            return SpineRuleUtility.TryLoadSkeleton(
                context,
                out skeletonDataAsset);
        }

        /// <summary>
        /// 在规则违反且配置有效时返回首个问题项。
        /// </summary>
        protected IssueInfo GetIssueIfViolated(
            ScoreRuleConfig ruleConfig,
            bool isViolated)
        {
            if (!isViolated ||
                ruleConfig == null ||
                ruleConfig.IssueInfos == null ||
                ruleConfig.IssueInfos.Length == 0)
            {
                return null;
            }

            return ruleConfig.IssueInfos[0];
        }

        public abstract AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig);
    }
}
