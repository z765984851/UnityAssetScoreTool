using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 AnimationClip 是否启用了 Legacy 模式。
    /// </summary>
    public class ScoreRule_AnimationLegacy : AnimationRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.animation.legacy"; }
        }

        public override AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig)
        {
            AnimationClip clip;
            return GetIssueIfViolated(
                ruleConfig,
                TryLoadAnimationClip(context, out clip) && clip.legacy);
        }
    }
}
