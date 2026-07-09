using Spine.Unity;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 Spine Atlas 使用的贴图尺寸是否超过 4096 像素。
    /// </summary>
    public class ScoreRule_SpineTextureSize4096 : IScoreRuleChecker
    {
        private const string RuleIdValue = "basic.spine.texturesize4096";
        private const int MaximumTextureSize = 4096;

        public string RuleId
        {
            get { return RuleIdValue; }
        }

        /// <summary>
        /// 判断当前资源是否具备执行贴图检查的基本条件。
        /// </summary>
        public bool ShouldCheck(AssetContext context)
        {
            AtlasAssetBase atlasAsset;
            return SpineRuleUtility.TryLoadAtlas(context, out atlasAsset);
        }

        /// <summary>
        /// 当任一 Atlas 默认材质的主贴图尺寸超过限制时返回问题项。
        /// </summary>
        public AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig)
        {
            AtlasAssetBase atlasAsset;

            if (!SpineRuleUtility.TryLoadAtlas(context, out atlasAsset) ||
                atlasAsset.Materials == null ||
                ruleConfig == null ||
                ruleConfig.IssueInfos == null ||
                ruleConfig.IssueInfos.Length == 0)
            {
                return null;
            }

            foreach (Material material in atlasAsset.Materials)
            {
                Texture texture = material == null ? null : material.mainTexture;

                if (texture != null &&
                    (texture.width > MaximumTextureSize ||
                     texture.height > MaximumTextureSize))
                {
                    return ruleConfig.IssueInfos[0];
                }
            }

            return null;
        }
    }
}
