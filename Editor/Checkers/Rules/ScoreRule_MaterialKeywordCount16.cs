namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 Material 启用的 Keyword 数量是否超过 16。
    /// </summary>
    public class ScoreRule_MaterialKeywordCount16
        : MaterialKeywordCountRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.material.keywordcount16"; }
        }

        protected override int MaximumKeywordCount
        {
            get { return 16; }
        }
    }
}