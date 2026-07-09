using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    [CreateAssetMenu(
        fileName = "ScoreRuleConfig",
        menuName = "资源评分工具/创建规则")]
    public class ScoreRuleConfig : ScriptableObject
    {
        [SerializeField] private string ruleId;
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private int priority;
        [SerializeField] private IssueInfo[] issueInfos;

        /// <summary>
        /// 规则唯一标识。
        /// </summary>
        public string RuleId
        {
            get { return ruleId; }
        }


        /// <summary>
        /// 编辑器中显示的规则名称。
        /// </summary>
        public string DisplayName
        {
            get { return displayName; }
        }

        /// <summary>
        /// 编辑器中显示的规则描述。
        /// </summary>
        public string Description
        {
            get { return description; }
        }

        /// <summary>
        /// 规则执行优先级。
        /// </summary>
        public int Priority
        {
            get { return priority; }
        }

        /// <summary>
        /// 当前规则对应的问题配置
        /// </summary>
        public IssueInfo[] IssueInfos
        {
            get { return issueInfos; }
        }
    }
}