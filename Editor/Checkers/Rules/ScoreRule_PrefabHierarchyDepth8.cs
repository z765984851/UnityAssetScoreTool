using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 Prefab 节点层级是否超过 8。
    /// </summary>
    public class ScoreRule_PrefabHierarchyDepth8
        : PrefabRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.prefab.hierarchydepth8"; }
        }

        /// <summary>
        /// 当 Prefab 最大节点层级超过 8 时返回问题项。
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
                PrefabRuleUtility.GetMaxHierarchyDepth(prefab) > 8);
        }
    }
}
