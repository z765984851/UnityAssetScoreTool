using System.Collections.Generic;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 单条资产问题结果。
    /// 
    /// 这是规则检测阶段产出的原始问题数据，
    /// 不包含分数、等级、汇总严重度等评分结果。
    /// </summary>
    public class AssetIssueResult
    {
        /// <summary>
        /// 出现问题的资源上下文。
        /// </summary>
        public AssetContext Context;

        /// <summary>
        /// 触发该问题的规则配置。
        /// </summary>
        public ScoreRuleConfig RuleConfig;

        /// <summary>
        /// 具体问题信息。
        /// 包含问题名称、扣分、严重度、提示信息等。
        /// </summary>
        public IssueInfo IssueInfo;

        /// <summary>
        /// Prefab 内部可定位的问题对象列表。
        /// 普通规则与未提供定位信息的规则保持为空。
        /// </summary>
        public readonly List<PrefabIssueLocation> Locations =
            new List<PrefabIssueLocation>();

        /// <summary>
        /// 将原有问题信息包装为扫描问题结果，供未提供对象定位的规则保持原有返回逻辑。
        /// </summary>
        public static implicit operator AssetIssueResult(IssueInfo issueInfo)
        {
            if (issueInfo == null)
            {
                return null;
            }

            AssetIssueResult result = new AssetIssueResult();
            result.IssueInfo = issueInfo;
            return result;
        }
    }

    /// <summary>
    /// 描述 Prefab 问题对应的内部对象与补充说明。
    /// </summary>
    public class PrefabIssueLocation
    {
        /// <summary>
        /// 相对于 Prefab 根节点的层级路径；空字符串表示根节点。
        /// </summary>
        public string HierarchyPath;

        /// <summary>
        /// 展示给用户的组件、属性或动画绑定问题说明。
        /// </summary>
        public string Description;
    }
}
