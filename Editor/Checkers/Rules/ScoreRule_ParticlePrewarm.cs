using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查粒子是否启用了 Prewarm。
    /// </summary>
    public class ScoreRule_ParticlePrewarm : ParticleRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.particle.prewarm"; }
        }

        /// <summary>
        /// 当任一粒子系统启用 Prewarm 时返回问题项。
        /// </summary>
        public override AssetIssueResult Check(AssetContext context, ScoreRuleConfig ruleConfig)
        {
            GameObject prefab;
            return GetIssueIfViolated(ruleConfig,
                PrefabRuleUtility.TryLoadPrefab(context, out prefab) &&
                ParticleRuleUtility.HasPrewarmEnabled(prefab));
        }
    }
}
