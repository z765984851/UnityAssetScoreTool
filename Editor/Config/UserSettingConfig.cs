using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 项目共享的资产评分工具设置。
    /// 该资产应保存在 Assets 目录并提交到版本控制。
    /// </summary>
    [CreateAssetMenu(
        fileName = "AssetScoreProjectSettings",
        menuName = "Catfood Tools/Asset Score Tool/创建项目设置")]
    public class UserSettingConfig : ScriptableObject
    {
        [SerializeField] [InspectorName("当前生效规则应用配置")]
        private RuleApplyConfig activeRuleApplyConfig;

        [Header("可选检测")] [SerializeField] [InspectorName("启用 Spine 检测")]
        private bool enableSpineChecks;

        [Header("默认保存位置")] [SerializeField] [InspectorName("检测配置档")]
        private DefaultAsset profileConfigDefaultFolder;

        [SerializeField] [InspectorName("规则应用配置")]
        private DefaultAsset ruleApplyConfigDefaultFolder;

        [SerializeField] [InspectorName("检测规则配置")]
        private DefaultAsset scoreRuleConfigDefaultFolder;

        [SerializeField] [InspectorName("资源类型配置")]
        private DefaultAsset assetTypeConfigDefaultFolder;

        [SerializeField] [InspectorName("CheckRule 脚本")]
        private DefaultAsset checkRuleScriptDefaultFolder;

        [SerializeField] [InspectorName("报告导出目录")]
        private string exportDefaultFolderPath;

        public RuleApplyConfig ActiveRuleApplyConfig
        {
            get { return activeRuleApplyConfig; }
        }

        public bool EnableSpineChecks
        {
            get { return enableSpineChecks; }
        }

        public DefaultAsset ProfileConfigDefaultFolder
        {
            get { return profileConfigDefaultFolder; }
        }

        public DefaultAsset RuleApplyConfigDefaultFolder
        {
            get { return ruleApplyConfigDefaultFolder; }
        }

        public DefaultAsset ScoreRuleConfigDefaultFolder
        {
            get { return scoreRuleConfigDefaultFolder; }
        }

        public DefaultAsset AssetTypeConfigDefaultFolder
        {
            get { return assetTypeConfigDefaultFolder; }
        }

        public DefaultAsset CheckRuleScriptDefaultFolder
        {
            get { return checkRuleScriptDefaultFolder; }
        }

        public string ExportDefaultFolderPath
        {
            get { return exportDefaultFolderPath; }
        }
    }
}
