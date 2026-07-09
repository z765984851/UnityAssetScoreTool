namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 AnimationClip 长度是否超过 10 秒。
    /// </summary>
    public class ScoreRule_AnimationClipLength10
        : AnimationClipLengthRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.animation.cliplength10"; }
        }

        protected override float MaximumClipLength
        {
            get { return 10f; }
        }
    }
}