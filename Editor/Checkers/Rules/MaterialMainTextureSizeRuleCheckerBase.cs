using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 提供 Material 主贴图尺寸阈值检查的公共实现。
    /// </summary>
    public abstract class MaterialMainTextureSizeRuleCheckerBase
        : MaterialRuleCheckerBase
    {
        protected abstract int MaximumTextureSize { get; }

        public override AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig)
        {
            Material material;

            if (!TryLoadMaterial(context, out material))
            {
                return null;
            }

            Texture mainTexture = material.mainTexture;
            bool isTooLarge = mainTexture != null &&
                              (mainTexture.width > MaximumTextureSize ||
                               mainTexture.height > MaximumTextureSize);
            return GetIssueIfViolated(ruleConfig, isTooLarge);
        }
    }
}
