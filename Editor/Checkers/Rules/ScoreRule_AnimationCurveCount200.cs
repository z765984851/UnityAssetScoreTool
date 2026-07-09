namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 AnimationClip 曲线数量是否超过 200。
    /// </summary>
    public class ScoreRule_AnimationCurveCount200
        : AnimationCurveCountRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.animation.curvecount200"; }
        }

        protected override int MaximumCurveCount
        {
            get { return 200; }
        }
    }
}