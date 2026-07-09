namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 AnimationClip 曲线数量是否超过 500。
    /// </summary>
    public class ScoreRule_AnimationCurveCount500
        : AnimationCurveCountRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.animation.curvecount500"; }
        }

        protected override int MaximumCurveCount
        {
            get { return 500; }
        }
    }
}