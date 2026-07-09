using System.Collections.Generic;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 单个资源的完整扫描结果。
    /// Issues 只保存当前资源自身的问题。
    /// Score / Grade / HighestIssueLevel / IssueCount 保存当前资源及其依赖的汇总结果。
    /// </summary>
    public struct AssetScanResult
    {
        public AssetContext Context;

        /// <summary>
        /// 当前资源自身的问题列表，不包含依赖资源的问题。
        /// </summary>
        public List<AssetIssueResult> Issues;

        /// <summary>
        /// 当前资源自身得分。
        /// </summary>
        public int SelfScore;

        /// <summary>
        /// 当前资源自身等级。
        /// </summary>
        public EAssetGrade SelfGrade;

        /// <summary>
        /// 当前资源自身最高严重度。
        /// </summary>
        public EIssueLevel SelfHighestIssueLevel;

        /// <summary>
        /// 当前资源自身问题数量。
        /// </summary>
        public int SelfIssueCount;

        /// <summary>
        /// 当前资源及其所有依赖后的最终得分。
        /// </summary>
        public int Score;

        /// <summary>
        /// 当前资源及其所有依赖后的最终等级。
        /// </summary>
        public EAssetGrade Grade;

        /// <summary>
        /// 当前资源及其所有依赖后的最高严重度。
        /// </summary>
        public EIssueLevel HighestIssueLevel;

        /// <summary>
        /// 当前资源及其所有依赖后的问题数量。
        /// </summary>
        public int IssueCount;
    }
}