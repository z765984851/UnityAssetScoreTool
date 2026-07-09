using UnityEditor;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 FBX 模型是否启用 Read/Write。
    /// </summary>
    public class ScoreRule_MeshReadWrite : IScoreRuleChecker
    {
        private const string RuleIdValue = "basic.mesh.readwrite";

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

            return AssetImporter.GetAtPath(context.AssetPath) is ModelImporter;
        }

        public AssetIssueResult Check(AssetContext context, ScoreRuleConfig ruleConfig)
        {
            if (!ShouldCheck(context) ||
                ruleConfig == null ||
                ruleConfig.IssueInfos == null ||
                ruleConfig.IssueInfos.Length == 0)
            {
                return null;
            }

            ModelImporter importer = AssetImporter.GetAtPath(context.AssetPath)
                as ModelImporter;

            return importer != null && importer.isReadable
                ? ruleConfig.IssueInfos[0]
                : null;
        }
    }
}
