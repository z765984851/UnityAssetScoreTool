namespace majsoul.tools.assetscoretool.editor
{
    public interface IScoreRuleChecker
    {
        /// <summary>
        /// 规则唯一标识。和Config中的标识符对应
        /// </summary>
        public string RuleId { get; }

        /// <summary>
        /// 资源的类型是否执行检测
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool ShouldCheck(AssetContext context);

        /// <summary>
        /// 资源检测方法，如果IssueInfo是空，则该资源没有问题
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ruleConfig"></param>
        /// <returns></returns>
        public IssueInfo Check(AssetContext context, ScoreRuleConfig ruleConfig);
    }
}