using System.Collections.Generic;

namespace majsoul.tools.assetscoretool.editor
{
    public class ScoreRule_ShaderKeywordCount8 : ShaderRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.shader.keywordcount8"; }
        }

        public override AssetIssueResult Check(AssetContext context, ScoreRuleConfig ruleConfig)
        {
            List<ShaderPragma> pragmas;
            return GetIssueIfViolated(
                ruleConfig,
                TryGetPragmas(context, out pragmas) &&
                ShaderRuleUtility.GetKeywordCount(pragmas) > 8);
        }
    }
}