using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 Material 的 Render Queue 是否为项目允许的标准队列值。
    /// </summary>
    public class ScoreRule_MaterialRenderQueue
        : MaterialRuleCheckerBase
    {
        private const string RuleIdentifier = "basic.material.renderqueue";
        private const int OpaqueRenderQueue = 2000;
        private const int AlphaTestRenderQueue = 2450;
        private const int TransparentRenderQueue = 3000;

        protected override string RuleIdValue
        {
            get { return RuleIdentifier; }
        }

        public override AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig)
        {
            Material material;
            return GetIssueIfViolated(
                ruleConfig,
                TryLoadMaterial(context, out material) &&
                !IsStandardRenderQueue(material.renderQueue));
        }

        private static bool IsStandardRenderQueue(int renderQueue)
        {
            return renderQueue == OpaqueRenderQueue ||
                   renderQueue == AlphaTestRenderQueue ||
                   renderQueue == TransparentRenderQueue;
        }
    }
}
