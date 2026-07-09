using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 Prefab 节点数量是否超过 150。
    /// </summary>
    public class ScoreRule_PrefabGameObjectCount150
        : PrefabRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.prefab.gameobjectcount150"; }
        }

        /// <summary>
        /// 当 Prefab 节点数量超过 150 时返回问题项。
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
                prefab.GetComponentsInChildren<Transform>(true).Length > 150);
        }
    }
}
