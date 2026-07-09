using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 提供 AnimationClip 时长阈值检查的公共实现。
    /// </summary>
    public abstract class AnimationClipLengthRuleCheckerBase
        : AnimationRuleCheckerBase
    {
        protected abstract float MaximumClipLength { get; }

        public override AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig)
        {
            AnimationClip clip;
            return GetIssueIfViolated(
                ruleConfig,
                TryLoadAnimationClip(context, out clip) &&
                clip.length > MaximumClipLength);
        }
    }
}
