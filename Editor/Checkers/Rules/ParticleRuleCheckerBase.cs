using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 提供粒子评分规则共用的资源适用性和问题项返回逻辑。
    /// </summary>
    public abstract class ParticleRuleCheckerBase : IScoreRuleChecker
    {
        protected abstract string RuleIdValue { get; }

        public string RuleId
        {
            get { return RuleIdValue; }
        }

        /// <summary>
        /// 判断当前资源是否为包含粒子系统的可加载 Prefab。
        /// </summary>
        public bool ShouldCheck(AssetContext context)
        {
            GameObject prefab;
            return PrefabRuleUtility.TryLoadPrefab(
                       context,
                       out prefab) &&
                   ParticleRuleUtility.GetParticleSystems(prefab).Length > 0;
        }

        /// <summary>
        /// 在违反规则且配置有效时返回规则的首个问题项。
        /// </summary>
        protected IssueInfo GetIssueIfViolated(
            ScoreRuleConfig ruleConfig,
            bool isViolated)
        {
            if (!isViolated ||
                ruleConfig == null ||
                ruleConfig.IssueInfos == null ||
                ruleConfig.IssueInfos.Length == 0)
            {
                return null;
            }

            return ruleConfig.IssueInfos[0];
        }

        public abstract AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig);
    }
}
