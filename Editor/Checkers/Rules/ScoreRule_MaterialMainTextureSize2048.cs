namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 Material 主贴图的任一边是否超过 2048 像素。
    /// </summary>
    public class ScoreRule_MaterialMainTextureSize2048
        : MaterialMainTextureSizeRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.material.maintexturesize2048"; }
        }

        protected override int MaximumTextureSize
        {
            get { return 2048; }
        }
    }
}