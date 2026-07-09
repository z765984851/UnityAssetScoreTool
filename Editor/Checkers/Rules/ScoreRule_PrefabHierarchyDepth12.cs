using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 Prefab 节点层级是否超过 12。
    /// </summary>
    public class ScoreRule_PrefabHierarchyDepth12
        : PrefabRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.prefab.hierarchydepth12"; }
        }

        /// <summary>
        /// 当 Prefab 最大节点层级超过 12 时返回问题项。
        /// </summary>
        public override AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig)
        {
            GameObject prefab;
            return GetIssueIfViolated(
                ruleConfig,
                PrefabRuleUtility.TryLoadPrefab(
                    context,
                    out prefab) &&
                PrefabRuleUtility.GetMaxHierarchyDepth(prefab) > 12);
        }
    }
}
