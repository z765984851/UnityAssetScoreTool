using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 当前生效 RuleApplyConfig 的统一解析入口。
    /// </summary>
    public static class RuleApplyConfigResolver
    {
        private const string DefaultConfigName =
            "RuleApplyConfig_Default";

        private const string DefaultProjectSettingsFolder =
            "Assets/AssetScoreTool/ProjectSettings";

        private const string DefaultProjectSettingsAssetPath =
            DefaultProjectSettingsFolder +
            "/AssetScoreProjectSettings.asset";

        /// <summary>
        /// 获取项目中唯一的 UserSettingConfig。
        /// 没有项目配置或存在多个项目配置时返回 null。
        /// </summary>
        public static UserSettingConfig GetProjectConfig()
        {
            List<UserSettingConfig> projectConfigs =
                ScanToolUtility
                    .LoadScriptableObject<UserSettingConfig>(
                        new string[] { "Assets" });

            if (projectConfigs.Count == 0)
            {
                return null;
            }

            if (projectConfigs.Count > 1)
            {
                Debug.LogError(
                    "发现多个 UserSettingConfig，" +
                    "项目中只能保留一个项目配置。");

                return null;
            }

            return projectConfigs[0];
        }

        /// <summary>
        /// 确保项目 Assets 目录中存在唯一的 UserSettingConfig。
        /// 缺失时创建默认项目设置；存在多个时保持原有错误处理并返回 null。
        /// </summary>
        public static UserSettingConfig EnsureProjectConfig()
        {
            List<UserSettingConfig> projectConfigs =
                ScanToolUtility
                    .LoadScriptableObject<UserSettingConfig>(
                        new string[] { "Assets" });

            if (projectConfigs.Count == 0)
            {
                return CreateProjectConfig();
            }

            if (projectConfigs.Count > 1)
            {
                Debug.LogError(
                    "发现多个 UserSettingConfig，" +
                    "项目中只能保留一个项目配置。");

                return null;
            }

            return projectConfigs[0];
        }

        /// <summary>
        /// 在项目 Assets 目录创建默认的 UserSettingConfig 资产。
        /// </summary>
        private static UserSettingConfig CreateProjectConfig()
        {
            EnsureProjectSettingsFolder();

            UserSettingConfig config =
                ScriptableObject.CreateInstance<UserSettingConfig>();

            string assetPath =
                AssetDatabase.GenerateUniqueAssetPath(
                    DefaultProjectSettingsAssetPath);

            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();

            return config;
        }

        /// <summary>
        /// 逐级创建默认项目设置资产所需的 Assets 子目录。
        /// </summary>
        private static void EnsureProjectSettingsFolder()
        {
            string[] folders =
                DefaultProjectSettingsFolder.Split('/');

            string currentPath = folders[0];

            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = currentPath + "/" + folders[i];

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }

                currentPath = nextPath;
            }
        }

        /// <summary>
        /// 获取当前项目生效的 RuleApplyConfig。
        /// 项目配置未指定时使用默认配置。
        /// </summary>
        public static RuleApplyConfig GetActiveConfig()
        {
            UserSettingConfig projectConfig =
                GetProjectConfig();

            if (projectConfig != null &&
                projectConfig.ActiveRuleApplyConfig != null)
            {
                return projectConfig.ActiveRuleApplyConfig;
            }

            return GetDefaultConfig();
        }

        /// <summary>
        /// 按固定文件名获取默认 RuleApplyConfig。
        /// 不依赖 Package 或本地开发目录的具体路径。
        /// </summary>
        public static RuleApplyConfig GetDefaultConfig()
        {
            List<RuleApplyConfig> configs =
                ScanToolUtility
                    .LoadScriptableObject<RuleApplyConfig>();

            RuleApplyConfig defaultConfig = null;

            for (int i = 0; i < configs.Count; i++)
            {
                RuleApplyConfig config = configs[i];

                if (config == null ||
                    !string.Equals(
                        config.name,
                        DefaultConfigName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (defaultConfig == null)
                {
                    defaultConfig = config;
                    continue;
                }

                Debug.LogError(
                    "发现多个默认规则应用配置：" +
                    DefaultConfigName +
                    "\n配置一：" +
                    AssetDatabase.GetAssetPath(defaultConfig) +
                    "\n配置二：" +
                    AssetDatabase.GetAssetPath(config));

                return null;
            }

            if (defaultConfig == null)
            {
                Debug.LogError(
                    "未找到默认规则应用配置：" +
                    DefaultConfigName +
                    ".asset");
            }

            return defaultConfig;
        }
    }
}
