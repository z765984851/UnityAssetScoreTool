﻿using System.Collections.Generic;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 Prefab Renderer 数量是否超过 80。
    /// </summary>
    public class ScoreRule_PrefabRendererCount80
        : PrefabRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.prefab.renderercount80"; }
        }

        /// <summary>
        /// 当 Prefab Renderer 数量超过 80 时返回问题项。
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
                PrefabRuleUtility.GetComponentLocations<Renderer>(prefab);
            AssetIssueResult result = GetIssueIfViolated(
                ruleConfig,
                locations.Count > 80);

            if (result != null)
            {
                result.Locations.AddRange(locations);
            }

            return result;
        }
    }
}
