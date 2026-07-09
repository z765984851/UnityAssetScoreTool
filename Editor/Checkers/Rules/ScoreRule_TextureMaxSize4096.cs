﻿using UnityEditor;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查贴图源文件的任一边是否超过 4096 像素。
    /// </summary>
    public class ScoreRule_TextureMaxSize4096 : IScoreRuleChecker
    {
        private const string RuleIdValue = "basic.texture.maxsize4096";
        private const int MaxSourceSize = 4096;

        public string RuleId
        {
            get { return RuleIdValue; }
        }

        /// <summary>
        /// 判断当前资源是否具备执行贴图检查的基本条件。
        /// </summary>
        public bool ShouldCheck(AssetContext context)
        {
            return context != null &&
                   !string.IsNullOrEmpty(context.AssetPath);
        }

        /// <summary>
        /// 当贴图源文件的任一边超过 4096 像素时，返回规则配置中的首个问题项。
        /// </summary>
        public AssetIssueResult Check(AssetContext context, ScoreRuleConfig ruleConfig)
        {
            if (!ShouldCheck(context) ||
                ruleConfig == null ||
                ruleConfig.IssueInfos == null ||
                ruleConfig.IssueInfos.Length == 0)
            {
                return null;
            }

            TextureImporter importer =
                AssetImporter.GetAtPath(context.AssetPath) as TextureImporter;

            if (importer == null)
            {
                return null;
            }

            int width;
            int height;
            importer.GetSourceTextureWidthAndHeight(out width, out height);

            if (width <= MaxSourceSize && height <= MaxSourceSize)
            {
                return null;
            }

            return ruleConfig.IssueInfos[0];
        }
    }
}
