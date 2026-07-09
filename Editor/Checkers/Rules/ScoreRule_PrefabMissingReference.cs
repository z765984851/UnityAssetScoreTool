﻿using System.Collections.Generic;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 Prefab 组件字段是否存在 Missing Reference。
    /// </summary>
    public class ScoreRule_PrefabMissingReference
        : PrefabRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.prefab.missingreference"; }
        }

        /// <summary>
        /// 当 Prefab 组件字段存在 Missing Reference 时返回问题项。
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
                PrefabRuleUtility.GetMissingReferenceLocations(prefab);
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
