using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查粒子持续时间是否超过 5 秒。
    /// </summary>
    public class ScoreRule_ParticleDuration5 : ParticleRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.particle.duration5"; }
        }

        /// <summary>
        /// 当任一粒子系统持续时间超过 5 秒时返回问题项。
        /// </summary>
        public override AssetIssueResult Check(AssetContext context, ScoreRuleConfig ruleConfig)
        {
            GameObject prefab;
            return GetIssueIfViolated(ruleConfig,
                PrefabRuleUtility.TryLoadPrefab(context, out prefab) &&
                ParticleRuleUtility.GetMaxDuration(prefab) > 5f);
        }
    }
}
