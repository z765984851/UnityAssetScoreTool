using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查粒子系统的 Max Particles 总量是否超过 2000。
    /// </summary>
    public class ScoreRule_ParticleMaxParticles2000 : ParticleRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.particle.maxparticles2000"; }
        }

        /// <summary>
        /// 当粒子系统的 Max Particles 总量超过 2000 时返回问题项。
        /// </summary>
        public override AssetIssueResult Check(AssetContext context, ScoreRuleConfig ruleConfig)
        {
            GameObject prefab;
            return GetIssueIfViolated(ruleConfig,
                PrefabRuleUtility.TryLoadPrefab(context, out prefab) &&
                ParticleRuleUtility.GetMaxParticlesTotal(prefab) > 2000);
        }
    }
}
