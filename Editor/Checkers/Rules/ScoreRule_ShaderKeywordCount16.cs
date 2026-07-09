using System.Collections.Generic;

namespace majsoul.tools.assetscoretool.editor
{
    public class ScoreRule_ShaderKeywordCount16 : ShaderRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.shader.keywordcount16"; }
        }

        public override AssetIssueResult Check(AssetContext context, ScoreRuleConfig ruleConfig)
        {
            List<ShaderPragma> pragmas;
            return GetIssueIfViolated(
                ruleConfig,
                TryGetPragmas(context, out pragmas) &&
                ShaderRuleUtility.GetKeywordCount(pragmas) > 16);
        }
    }
}