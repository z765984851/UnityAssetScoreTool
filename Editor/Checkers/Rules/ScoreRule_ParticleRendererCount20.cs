using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查粒子 Renderer 数量是否超过 20。
    /// </summary>
    public class ScoreRule_ParticleRendererCount20 : ParticleRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.particle.renderercount20"; }
        }

        /// <summary>
        /// 当粒子 Renderer 数量超过 20 时返回问题项。
        /// </summary>
        public override AssetIssueResult Check(AssetContext context, ScoreRuleConfig ruleConfig)
        {
            GameObject prefab;
            return GetIssueIfViolated(ruleConfig,
                PrefabRuleUtility.TryLoadPrefab(context, out prefab) &&
                ParticleRuleUtility.GetParticleSystemRenderers(prefab).Length > 20);
        }
    }
}
