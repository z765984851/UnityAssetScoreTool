using System.Collections.Generic;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 Prefab 是否存在 Rigidbody、Collider 或 CharacterController 物理组件。
    /// </summary>
    public class ScoreRule_PrefabRigidbody
        : PrefabRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.prefab.rigidbody"; }
        }

        /// <summary>
        /// 当 Prefab 存在物理组件时返回问题项。
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
                PrefabRuleUtility.GetPhysicsComponentLocations(prefab);
            AssetIssueResult result = GetIssueIfViolated(
                ruleConfig,
                locations.Count > 0);

            if (result != null)
            {
                result.Locations.AddRange(locations);
            }

            return result;
        }
    }
}
