using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查粒子系统数量是否超过 10。
    /// </summary>
    public class ScoreRule_ParticleSystemCount10 : ParticleRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.particle.systemcount10"; }
        }

        /// <summary>
        /// 当粒子系统数量超过 10 时返回问题项。
        /// </summary>
        public override AssetIssueResult Check(AssetContext context, ScoreRuleConfig ruleConfig)
        {
            GameObject prefab;
            return GetIssueIfViolated(ruleConfig,
                PrefabRuleUtility.TryLoadPrefab(context, out prefab) &&
                ParticleRuleUtility.GetParticleSystems(prefab).Length > 10);
        }
    }
}
