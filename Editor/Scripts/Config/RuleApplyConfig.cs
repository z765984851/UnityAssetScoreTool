using System;
using System.Collections.Generic;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    [CreateAssetMenu(
        fileName = "RuleApplyConfig",
        menuName = "资源评分工具/创建规则应用配置")]
    public class RuleApplyConfig : ScriptableObject
    {
        /// <summary>
        /// 规则应用列表。
        /// 一条规则可以配置到多个资源类型和多个检测配置档。
        /// </summary>
        [SerializeField] private RuleApplyItem[] ruleApplyItems;

        public RuleApplyItem[] RuleApplyItems
        {
            get { return ruleApplyItems; }
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
}