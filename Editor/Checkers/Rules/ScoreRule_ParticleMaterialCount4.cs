using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查粒子使用的不同共享材质数量是否超过 4。
    /// </summary>
    public class ScoreRule_ParticleMaterialCount4 : ParticleRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.particle.materialcount4"; }
        }

        /// <summary>
        /// 当不同共享材质数量超过 4 时返回问题项。
        /// </summary>
        public override AssetIssueResult Check(AssetContext context, ScoreRuleConfig ruleConfig)
        {
            GameObject prefab;
            return GetIssueIfViolated(ruleConfig,
                PrefabRuleUtility.TryLoadPrefab(context, out prefab) &&
                ParticleRuleUtility.GetUniqueMaterialCount(prefab) > 4);
        }
    }
}
