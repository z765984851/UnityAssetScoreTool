using UnityEditor;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查贴图是否开启 Mipmap。
    /// </summary>
    public class ScoreRule_TextureMipMap : IScoreRuleChecker
    {
        private const string RuleIdValue = "basic.texture.mipmap";

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
        /// 当贴图开启 Mipmap 时，返回规则配置中的首个问题项。
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

            if (importer == null || !importer.mipmapEnabled)
            {
                return null;
            }

            return ruleConfig.IssueInfos[0];
        }
    }
}
