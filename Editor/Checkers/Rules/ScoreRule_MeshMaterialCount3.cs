using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 FBX 模型中所有 Renderer 使用的材质槽数量是否超过 3。
    /// </summary>
    public class ScoreRule_MeshMaterialCount3 : IScoreRuleChecker
    {
        private const string RuleIdValue = "basic.mesh.meshmaterialcount3";
        private const int MaxMaterialSlotCount = 3;

        internal static int? MaximumMaterialSlotCountOverrideForTests;

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

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(
                context.AssetPath);

            if (model == null)
            {
                return null;
            }

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            int materialSlotCount = 0;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer != null && renderer.sharedMaterials != null)
                {
                    materialSlotCount += renderer.sharedMaterials.Length;
                }
            }

            int maximumMaterialSlotCount =
                MaximumMaterialSlotCountOverrideForTests ??
                MaxMaterialSlotCount;

            return materialSlotCount > maximumMaterialSlotCount
                ? ruleConfig.IssueInfos[0]
                : null;
        }
    }
}
