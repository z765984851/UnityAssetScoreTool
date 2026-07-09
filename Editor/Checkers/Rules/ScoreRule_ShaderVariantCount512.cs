using System.Collections.Generic;

namespace majsoul.tools.assetscoretool.editor
{
    public class ScoreRule_ShaderVariantCount512 : ShaderRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.shader.variantcount512"; }
        }

        public override AssetIssueResult Check(AssetContext context, ScoreRuleConfig ruleConfig)
        {
            List<ShaderPragma> pragmas;
            return GetIssueIfViolated(
                ruleConfig,
                TryGetPragmas(context, out pragmas) &&
                ShaderRuleUtility.GetVariantCountUpperBound(pragmas) > 512);
        }
    }
}