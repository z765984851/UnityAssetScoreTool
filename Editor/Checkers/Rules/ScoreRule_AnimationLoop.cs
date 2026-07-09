using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 AnimationClip 是否使用非 Once 的循环模式。
    /// </summary>
    public class ScoreRule_AnimationLoop : AnimationRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.animation.loop"; }
        }

        public override AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig)
        {
            AnimationClip clip;
            return GetIssueIfViolated(
                ruleConfig,
                TryLoadAnimationClip(context, out clip) &&
                clip.wrapMode != WrapMode.Once);
        }
    }
}
