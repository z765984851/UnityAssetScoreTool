using UnityEditor;

namespace majsoul.tools.assetscoretool.editor
{
    public class ScoreRule_TextureReadWrite : IScoreRuleChecker
    {
        private const string RuleIdValue = "basic.texture.readwrite";

        public string RuleId
        {
            get { return RuleIdValue; }
        }

        public bool ShouldCheck(AssetContext context)
        {
            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            return true;
        }

        public IssueInfo Check(AssetContext context, ScoreRuleConfig ruleConfig)
        {
            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return null;
            }

            if (ruleConfig == null ||
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

            if (!importer.isReadable)
            {
                return null;
            }

            return ruleConfig.IssueInfos[0];
        }
    }
}