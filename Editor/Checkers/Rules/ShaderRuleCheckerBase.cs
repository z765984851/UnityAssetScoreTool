using System.Collections.Generic;

namespace majsoul.tools.assetscoretool.editor
{
    public abstract class ShaderRuleCheckerBase : IScoreRuleChecker
    {
        protected abstract string RuleIdValue { get; }

        public string RuleId
        {
            get { return RuleIdValue; }
        }

        public bool ShouldCheck(AssetContext context)
        {
            List<ShaderPragma> pragmas;
            return ShaderRuleUtility.TryGetPragmas(context, out pragmas);
        }

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

        protected bool TryGetPragmas(
            AssetContext context,
            out List<ShaderPragma> pragmas)
        {
            return ShaderRuleUtility.TryGetPragmas(context, out pragmas);
        }

        public abstract AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig);
    }
}