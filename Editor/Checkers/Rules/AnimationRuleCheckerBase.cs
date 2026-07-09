using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 提供独立 AnimationClip 评分规则共用的资源加载和问题项返回逻辑。
    /// </summary>
    public abstract class AnimationRuleCheckerBase : IScoreRuleChecker
    {
        protected abstract string RuleIdValue { get; }

        public string RuleId
        {
            get { return RuleIdValue; }
        }

        public bool ShouldCheck(AssetContext context)
        {
            AnimationClip clip;
            return TryLoadAnimationClip(context, out clip);
        }

        protected static bool TryLoadAnimationClip(
            AssetContext context,
            out AnimationClip clip)
        {
            clip = null;

            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                context.AssetPath);
            return clip != null;
        }

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
