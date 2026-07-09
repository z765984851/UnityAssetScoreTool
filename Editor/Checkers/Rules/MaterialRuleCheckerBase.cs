using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 提供 Material 评分规则共用的资源加载和问题项返回逻辑。
    /// </summary>
    public abstract class MaterialRuleCheckerBase : IScoreRuleChecker
    {
        protected abstract string RuleIdValue { get; }

        public string RuleId
        {
            get { return RuleIdValue; }
        }

        public bool ShouldCheck(AssetContext context)
        {
            Material material;
            return TryLoadMaterial(context, out material);
        }

        protected static bool TryLoadMaterial(
            AssetContext context,
            out Material material)
        {
            material = null;

            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            material = AssetDatabase.LoadAssetAtPath<Material>(
                context.AssetPath);
            return material != null;
        }

        protected IssueInfo GetIssueIfViolated(
            ScoreRuleConfig ruleConfig,
            bool isViolated)
        {
            if (!isViolated ||
                ruleConfig == null ||
                ruleConfig.IssueInfos == null ||
                ruleConfig.IssueInfos.Length == 0)
            {
                return null;
            }

            return ruleConfig.IssueInfos[0];
        }

        public abstract AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig);
    }
}
