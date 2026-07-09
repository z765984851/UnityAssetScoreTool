using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 提供 AnimationClip 总关键帧数量阈值检查的公共实现。
    /// </summary>
    public abstract class AnimationKeyframeCountRuleCheckerBase
        : AnimationRuleCheckerBase
    {
        protected abstract int MaximumKeyframeCount { get; }

        public override AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig)
        {
            AnimationClip clip;
            return GetIssueIfViolated(
                ruleConfig,
                TryLoadAnimationClip(context, out clip) &&
                AnimationRuleUtility.GetKeyframeCount(clip) >
                    MaximumKeyframeCount);
        }
    }
}
