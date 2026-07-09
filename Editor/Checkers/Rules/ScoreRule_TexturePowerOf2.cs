using UnityEditor;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查贴图源文件的宽高是否均为 2 的幂。
    /// </summary>
    public class ScoreRule_TexturePowerOf2 : IScoreRuleChecker
    {
        private const string RuleIdValue = "basic.texture.powerof2";

        public string RuleId
        {
            get { return RuleIdValue; }
        }

        /// <summary>
        /// 判断当前资源是否具备执行贴图检查的基本条件。
        /// </summary>
        public bool ShouldCheck(AssetContext context)
        {
            return context != null &&
                   !string.IsNullOrEmpty(context.AssetPath);
        }

        /// <summary>
        /// 当贴图源文件的任一边不是 2 的幂时，返回规则配置中的首个问题项。
        /// </summary>
        public AssetIssueResult Check(AssetContext context, ScoreRuleConfig ruleConfig)
        {
            if (!ShouldCheck(context) ||
                ruleConfig == null ||
                ruleConfig.IssueInfos == null ||
                ruleConfig.IssueInfos.Length == 0)
            {
                return null;
            }

            TextureImporter importer =
                AssetImporter.GetAtPath(context.AssetPath) as TextureImporter;

            if (importer == null)
            {
                return null;
            }

            int width;
            int height;
            importer.GetSourceTextureWidthAndHeight(out width, out height);

            if (IsPowerOfTwo(width) && IsPowerOfTwo(height))
            {
                return null;
            }

            return ruleConfig.IssueInfos[0];
        }

        /// <summary>
        /// 判断数值是否为 2 的幂。
        /// </summary>
        private static bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }
    }
}
