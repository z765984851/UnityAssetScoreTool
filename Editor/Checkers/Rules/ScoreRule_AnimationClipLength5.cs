namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 AnimationClip 长度是否超过 5 秒。
    /// </summary>
    public class ScoreRule_AnimationClipLength5
        : AnimationClipLengthRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.animation.cliplength5"; }
        }

        protected override float MaximumClipLength
        {
            get { return 5f; }
        }
    }
}