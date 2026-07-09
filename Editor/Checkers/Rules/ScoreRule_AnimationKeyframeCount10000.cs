namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 AnimationClip 总关键帧数量是否超过 10000。
    /// </summary>
    public class ScoreRule_AnimationKeyframeCount10000
        : AnimationKeyframeCountRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.animation.keyframecount10000"; }
        }

        protected override int MaximumKeyframeCount
        {
            get { return 10000; }
        }
    }
}