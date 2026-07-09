using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 提供 AnimationClip 曲线数量阈值检查的公共实现。
    /// </summary>
    public abstract class AnimationCurveCountRuleCheckerBase
        : AnimationRuleCheckerBase
    {
        protected abstract int MaximumCurveCount { get; }

        public override AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig)
        {
            AnimationClip clip;
            return GetIssueIfViolated(
                ruleConfig,
                TryLoadAnimationClip(context, out clip) &&
                AnimationRuleUtility.GetCurveCount(clip) >
                    MaximumCurveCount);
        }
    }
}
