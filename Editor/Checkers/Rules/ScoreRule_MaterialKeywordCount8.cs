namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 Material 启用的 Keyword 数量是否超过 8。
    /// </summary>
    public class ScoreRule_MaterialKeywordCount8
        : MaterialKeywordCountRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.material.keywordcount8"; }
        }

        protected override int MaximumKeywordCount
        {
            get { return 8; }
        }
    }
}