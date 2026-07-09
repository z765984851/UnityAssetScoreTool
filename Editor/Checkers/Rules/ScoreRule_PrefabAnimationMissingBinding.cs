using System.Collections.Generic;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 Prefab 实际使用的动画是否存在失效绑定路径。
    /// </summary>
    public class ScoreRule_PrefabAnimationMissingBinding
        : PrefabRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.prefab.animationmissingbinding"; }
        }

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
                PrefabRuleUtility.GetMissingAnimationBindingLocations(prefab);
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
