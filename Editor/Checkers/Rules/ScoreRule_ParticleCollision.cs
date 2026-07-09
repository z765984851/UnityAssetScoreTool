﻿using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查粒子是否启用了 Collision 模块。
    /// </summary>
    public class ScoreRule_ParticleCollision : ParticleRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.particle.collision"; }
        }

        /// <summary>
        /// 当任一粒子系统启用 Collision 模块时返回问题项。
        /// </summary>
        public override AssetIssueResult Check(AssetContext context, ScoreRuleConfig ruleConfig)
        {
            GameObject prefab;
            return GetIssueIfViolated(ruleConfig,
                PrefabRuleUtility.TryLoadPrefab(context, out prefab) &&
                ParticleRuleUtility.HasCollisionEnabled(prefab));
        }
    }
}
