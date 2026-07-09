using System;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    [CreateAssetMenu(
        fileName = "RuleApplyConfig",
        menuName = "Catfood Tools/Asset Score Tool/创建规则应用配置")]
    public class RuleApplyConfig : ScriptableObject
    {
        /// <summary>
        /// 规则应用配置的唯一标识。
        /// </summary>
        [SerializeField] private string ruleApplyId;

        /// <summary>
        /// 编辑器中显示的规则应用配置名称。
        /// </summary>
        [SerializeField] private string displayName;

        /// <summary>
        /// 规则应用列表。
        /// 一条规则可以配置到多个资源类型和多个检测配置档。
        /// </summary>
        [SerializeField] private RuleApplyItem[] ruleApplyItems;

        /// <summary>
        /// 当前规则应用配置使用的白名单。
        /// </summary>
        [SerializeField] private WhiteListItem[] whiteListItems;

        public string RuleApplyId
        {
            get { return ruleApplyId; }
        }

        public string DisplayName
        {
            get { return displayName; }
        }

        public RuleApplyItem[] RuleApplyItems
        {
            get { return ruleApplyItems; }
        }

        public WhiteListItem[] WhiteListItems
        {
            get { return whiteListItems; }
        }
    }

    [Serializable]
    public class RuleApplyItem
    {
        /// <summary>
        /// 对应的规则配置。
        /// </summary>
        [SerializeField] private ScoreRuleConfig ruleConfig;

        /// <summary>
        /// 当前规则适用的资源类型。
        /// 例如 texture、prefab、material。
        /// </summary>
        [SerializeField] private AssetTypeConfig[] assetTypeConfigs;

        /// <summary>
        /// 当前规则适用的检测配置档。
        /// 例如 pc.default、ios.high。
        /// </summary>
        [SerializeField] private ScoreRuleProfileConfig[] profiles;

        /// <summary>
        /// 当前应用配置是否启用。
        /// </summary>
        [SerializeField] private bool isEnabled = true;

        public ScoreRuleConfig RuleConfig
        {
            get { return ruleConfig; }
        }

        public AssetTypeConfig[] AssetTypeConfigs
        {
            get { return assetTypeConfigs; }
        }

        public ScoreRuleProfileConfig[] Profiles
        {
            get { return profiles; }
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
        }

        public string RuleId
        {
            get
            {
                if (ruleConfig == null)
                {
                    return string.Empty;
                }

                return ruleConfig.RuleId;
            }
        }
    }

    /// <summary>
    /// 单条白名单配置。
    ///
    /// 匹配范围：
    /// 1. AssetPath 必须匹配资源路径或资源所在目录。
    /// 2. RuleConfig 为空时，匹配所有规则。
    /// 3. IssueInfoId 为空时，匹配规则下所有问题。
    /// </summary>
    [Serializable]
    public class WhiteListItem
    {
        /// <summary>
        /// Unity 资源路径或文件夹路径。
        ///
        /// 示例：
        /// Assets/Art/UI/Icon.png
        /// Assets/Art/Legacy
        /// </summary>
        [SerializeField] private string assetPath;

        /// <summary>
        /// 指定忽略的规则。
        /// 为空时表示该资源路径下的所有规则。
        /// </summary>
        [SerializeField] private ScoreRuleConfig ruleConfig;

        /// <summary>
        /// 指定忽略的 IssueInfo。
        /// 为空时表示该规则下的所有 IssueInfo。
        /// </summary>
        [SerializeField] private string issueInfoId;

        /// <summary>
        /// 白名单备注。
        /// </summary>
        [SerializeField] [TextArea] private string remark;

        public string AssetPath
        {
            get { return assetPath; }
        }

        public ScoreRuleConfig RuleConfig
        {
            get { return ruleConfig; }
        }

        public string IssueInfoId
        {
            get { return issueInfoId; }
        }

        public string Remark
        {
            get { return remark; }
        }
    }
}
