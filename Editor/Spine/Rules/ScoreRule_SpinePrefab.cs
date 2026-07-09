using Spine.Unity;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 为 Spine Prefab 的默认 Atlas 材质数量检查提供统一实现。
    /// </summary>
    public abstract class SpinePrefabMaterialCountRuleCheckerBase
        : IScoreRuleChecker
    {
        protected abstract string RuleIdValue { get; }

        protected abstract int MaximumMaterialCount { get; }

        public string RuleId
        {
            get { return RuleIdValue; }
        }

        /// <summary>
        /// 判断当前资源是否满足该 Spine 规则的适用条件。
        /// </summary>
        public bool ShouldCheck(AssetContext context)
        {
            GameObject prefab;
            return SpineRuleUtility.TryLoadPrefab(context, out prefab) &&
                     SpineRuleUtility.HasSpineComponent(prefab);
        }

        /// <summary>
        /// 当 Spine Prefab 的默认 Atlas 材质数量超过限制时，返回规则配置中的首个问题项。
        /// </summary>
        public AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig)
        {
            GameObject prefab;

            if (!SpineRuleUtility.TryLoadPrefab(context, out prefab) ||
                ruleConfig == null ||
                ruleConfig.IssueInfos == null ||
                ruleConfig.IssueInfos.Length == 0)
            {
                return null;
            }

            return SpineRuleUtility.GetPrefabAtlasMaterialCount(prefab) >
                   MaximumMaterialCount
                ? ruleConfig.IssueInfos[0]
                : null;
        }
    }

    /// <summary>
    /// 检查 Spine Prefab 的默认 Atlas 材质数量是否超过 4。
    /// </summary>
    public class ScoreRule_SpineMaterialCount4
        : SpinePrefabMaterialCountRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.spine.materialcount4"; }
        }

        protected override int MaximumMaterialCount { get { return 4; } }
    }

    /// <summary>
    /// 检查 Spine Prefab 的默认 Atlas 材质数量是否超过 2。
    /// </summary>
    public class ScoreRule_SpineMaterialCount2
        : SpinePrefabMaterialCountRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.spine.materialcount2"; }
        }

        protected override int MaximumMaterialCount { get { return 2; } }
    }
}
