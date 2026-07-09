using System;

namespace majsoul.tools.assetscoretool.editor
{
    [Serializable]
    public class IssueInfo
    {
        /// <summary>
        /// 问题ID。
        /// 例如 isOpen
        /// </summary>
        public string issueInfoId;

        /// <summary>
        /// 显示名称。问题的简要描述
        /// </summary>
        public string displayName;

        /// <summary>
        /// 扣除分数。
        /// </summary>
        public int deductScore;

        /// <summary>
        /// 问题的等级
        /// </summary>
        public EIssueLevel issueLevel;

        /// <summary>
        /// 问题说明。
        /// 可以包含原因和修改建议。
        /// </summary>
        public string issueMessage;
    }
}