using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 FBX 模型中所有 Renderer 使用的材质槽数量是否超过 4。
    /// </summary>
    public class ScoreRule_MeshMaterialCount4 : IScoreRuleChecker
    {
        private const string RuleIdValue = "basic.mesh.meshmaterialcount4";
        private const int MaxMaterialSlotCount = 4;

        internal static int? MaximumMaterialSlotCountOverrideForTests;

        public string RuleId
        {
            get { return RuleIdValue; }
        }

        /// <summary>
        /// 判断当前资源是否为可读取导入设置的 FBX 模型。
        /// </summary>
        public bool ShouldCheck(AssetContext context)
        {
            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            return AssetImporter.GetAtPath(context.AssetPath) is ModelImporter;
        }

        /// <summary>
        /// 当模型全部 Renderer 的材质槽总数超过限制时，返回该规则配置的首个问题项。
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

            if (materialSlotCount <= maximumMaterialSlotCount)
            {
                return null;
            }

            return ruleConfig.IssueInfos[0];
        }
    }
}
