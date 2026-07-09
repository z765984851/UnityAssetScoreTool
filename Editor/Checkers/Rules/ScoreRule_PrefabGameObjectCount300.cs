using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 Prefab 节点数量是否超过 300。
    /// </summary>
    public class ScoreRule_PrefabGameObjectCount300
        : PrefabRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.prefab.gameobjectcount300"; }
        }

        /// <summary>
        /// 当 Prefab 节点数量超过 300 时返回问题项。
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
                prefab.GetComponentsInChildren<Transform>(true).Length > 300);
        }
    }
}
