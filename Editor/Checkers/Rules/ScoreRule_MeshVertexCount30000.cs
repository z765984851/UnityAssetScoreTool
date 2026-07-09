using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 FBX 模型的 Mesh 顶点总数是否超过 30000。
    /// </summary>
    public class ScoreRule_MeshVertexCount30000 : IScoreRuleChecker
    {
        private const string RuleIdValue = "basic.mesh.meshvertexcount30k";
        private const int MaxVertexCount = 30000;

        internal static int? MaximumVertexCountOverrideForTests;

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

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(context.AssetPath);
            int vertexCount = 0;

            for (int i = 0; i < assets.Length; i++)
            {
                Mesh mesh = assets[i] as Mesh;

                if (mesh != null)
                {
                    vertexCount += mesh.vertexCount;
                }
            }

            int maximumVertexCount = MaximumVertexCountOverrideForTests ??
                MaxVertexCount;

            return vertexCount > maximumVertexCount
                ? ruleConfig.IssueInfos[0]
                : null;
        }
    }
}
