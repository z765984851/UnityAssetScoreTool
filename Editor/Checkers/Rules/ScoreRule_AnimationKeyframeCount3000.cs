namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 AnimationClip 总关键帧数量是否超过 3000。
    /// </summary>
    public class ScoreRule_AnimationKeyframeCount3000
        : AnimationKeyframeCountRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.animation.keyframecount3000"; }
        }

        protected override int MaximumKeyframeCount
        {
            get { return 3000; }
        }
    }
}