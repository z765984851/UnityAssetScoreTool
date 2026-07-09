namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 Material 主贴图的任一边是否超过 4096 像素。
    /// </summary>
    public class ScoreRule_MaterialMainTextureSize4096
        : MaterialMainTextureSizeRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.material.maintexturesize4096"; }
        }

        protected override int MaximumTextureSize
        {
            get { return 4096; }
        }
    }
}