namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 单条资产问题结果。
    /// 
    /// 这是规则检测阶段产出的原始问题数据，
    /// 不包含分数、等级、汇总严重度等评分结果。
    /// </summary>
    public class AssetIssueResult
    {
        /// <summary>
        /// 出现问题的资源上下文。
        /// </summary>
        public AssetContext Context;

        /// <summary>
        /// 触发该问题的规则配置。
        /// </summary>
        public ScoreRuleConfig RuleConfig;

        /// <summary>
        /// 具体问题信息。
        /// 包含问题名称、扣分、严重度、提示信息等。
        /// </summary>
        public IssueInfo IssueInfo;
    }
}