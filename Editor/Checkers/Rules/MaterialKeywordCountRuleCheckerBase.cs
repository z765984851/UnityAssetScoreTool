using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 提供 Material 启用 Keyword 数量阈值检查的公共实现。
    /// </summary>
    public abstract class MaterialKeywordCountRuleCheckerBase
        : MaterialRuleCheckerBase
    {
        protected abstract int MaximumKeywordCount { get; }

        public override AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig)
        {
            Material material;
            return GetIssueIfViolated(
                ruleConfig,
                TryLoadMaterial(context, out material) &&
                material.shaderKeywords != null &&
                material.shaderKeywords.Length > MaximumKeywordCount);
        }
    }
}
