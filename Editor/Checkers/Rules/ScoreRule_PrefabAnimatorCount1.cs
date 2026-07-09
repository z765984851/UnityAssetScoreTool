using System.Collections.Generic;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 Prefab Animator 数量是否超过 1。
    /// </summary>
    public class ScoreRule_PrefabAnimatorCount1
        : PrefabRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.prefab.animatorcount1"; }
        }

        /// <summary>
        /// 当 Prefab Animator 数量超过 1 时返回问题项。
        /// </summary>
        public override AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig)
        {
            GameObject prefab;

            if (!PrefabRuleUtility.TryLoadPrefab(context, out prefab))
            {
                return null;
            }

            List<PrefabIssueLocation> locations =
                PrefabRuleUtility.GetComponentLocations<Animator>(prefab);
            AssetIssueResult result = GetIssueIfViolated(
                ruleConfig,
                locations.Count > 1);

            if (result != null)
            {
                result.Locations.AddRange(locations);
            }

            return result;
        }
    }
}
