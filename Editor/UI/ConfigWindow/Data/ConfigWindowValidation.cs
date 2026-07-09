using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 配置诊断的严重程度；错误阻止保存，警告只用于提示。
    /// </summary>
    internal enum EConfigValidationSeverity
    {
        Error,
        Warning
    }

    /// <summary>
    /// 描述一个配置工作副本中的诊断问题及其可定位字段。
    /// </summary>
    internal sealed class ConfigValidationIssue
    {
        public ScriptableObject WorkingConfig;
        public EConfigPage Page;
        public string PropertyPath;
        public string Message;
        public EConfigValidationSeverity Severity;
    }

    /// <summary>
    /// 执行跨配置唯一性和当前生效规则应用项诊断。
    /// 必填字段与项目目录校验仍由 ConfigWindow 的现有页面校验流程负责。
    /// </summary>
    internal sealed class ConfigWindowValidation
    {
        private sealed class IdLocation
        {
            public ScriptableObject Config;
            public string PropertyPath;
        }

        private readonly Func<RuleApplyConfig>
            getActiveRuleApplyConfig;

        private readonly Func<ScriptableObject, string>
            getConfigAssetPath;

        private readonly Func<ScriptableObject, bool>
            isConfigAvailable;

        private readonly Func<ScriptableObject, ScriptableObject>
            getWorkingConfig;

        /// <summary>
        /// 创建诊断器，并注入当前生效配置及真实资产路径解析方式。
        /// </summary>
        public ConfigWindowValidation(
            Func<RuleApplyConfig> getActiveRuleApplyConfig,
            Func<ScriptableObject, string> getConfigAssetPath,
            Func<ScriptableObject, bool> isConfigAvailable,
            Func<ScriptableObject, ScriptableObject>
                getWorkingConfig)
        {
            this.getActiveRuleApplyConfig =
                getActiveRuleApplyConfig;
            this.getConfigAssetPath = getConfigAssetPath;
            this.isConfigAvailable = isConfigAvailable;
            this.getWorkingConfig = getWorkingConfig;
        }

        /// <summary>
        /// 将跨配置唯一性和当前生效规则应用项问题追加到统一诊断集合。
        /// </summary>
        public void ValidateCrossConfig(
            IEnumerable<ScriptableObject> workingCopies,
            IList<ConfigValidationIssue> issues)
        {
            List<ScriptableObject> configs =
                new List<ScriptableObject>(workingCopies);

            ValidateUniqueIds(
                configs,
                typeof(ScoreRuleProfileConfig),
                "profileId",
                EConfigPage.Profile,
                "配置档 ID 重复：",
                issues);

            ValidateUniqueIds(
                configs,
                typeof(RuleApplyConfig),
                "ruleApplyId",
                EConfigPage.RuleApply,
                "规则应用配置 ID 重复：",
                issues);

            ValidateUniqueIds(
                configs,
                typeof(ScoreRuleConfig),
                "ruleId",
                EConfigPage.Rule,
                "规则 ID 重复：",
                issues);

            ValidateUniqueIds(
                configs,
                typeof(AssetTypeConfig),
                "assetTypeId",
                EConfigPage.AssetType,
                "资源类型 ID 重复：",
                issues);

            ValidateIssueIds(
                configs,
                issues);

            ValidateConfigReferences(
                configs,
                issues);

            ValidateActiveRuleApply(
                issues);
        }

        /// <summary>
        /// 检查所有可编辑工作副本中的跨配置引用和白名单问题 ID。
        /// </summary>
        private void ValidateConfigReferences(
            IList<ScriptableObject> configs,
            IList<ConfigValidationIssue> issues)
        {
            for (int i = 0; i < configs.Count; i++)
            {
                RuleApplyConfig ruleApply =
                    configs[i] as RuleApplyConfig;

                if (ruleApply != null)
                {
                    ValidateRuleApplyReferences(
                        ruleApply,
                        issues);
                    continue;
                }

                UserSettingConfig settings =
                    configs[i] as UserSettingConfig;

                if (settings != null &&
                    settings.ActiveRuleApplyConfig != null &&
                    !IsReferenceAvailable(
                        settings.ActiveRuleApplyConfig))
                {
                    AddIssue(
                        issues,
                        settings,
                        EConfigPage.ProjectSettings,
                        "activeRuleApplyConfig",
                        "当前生效规则应用配置引用无效。",
                        EConfigValidationSeverity.Error);
                }
            }
        }

        /// <summary>
        /// 检查规则应用项和白名单中的配置引用是否完整有效。
        /// </summary>
        private void ValidateRuleApplyReferences(
            RuleApplyConfig config,
            IList<ConfigValidationIssue> issues)
        {
            RuleApplyItem[] ruleApplyItems =
                config.RuleApplyItems;

            if (ruleApplyItems != null)
            {
                for (int i = 0;
                     i < ruleApplyItems.Length;
                     i++)
                {
                    RuleApplyItem item = ruleApplyItems[i];
                    string itemPath =
                        "ruleApplyItems.Array.data[" + i + "]";

                    if (item == null)
                    {
                        AddIssue(
                            issues,
                            config,
                            EConfigPage.RuleApply,
                            itemPath,
                            "规则应用项数据无效。",
                            EConfigValidationSeverity.Error);
                        continue;
                    }

                    ValidateRequiredReference(
                        config,
                        item.RuleConfig,
                        itemPath + ".ruleConfig",
                        "规则应用项引用的规则无效。",
                        issues);

                    ValidateReferenceArray(
                        config,
                        item.AssetTypeConfigs,
                        itemPath + ".assetTypeConfigs",
                        "规则应用项包含无效的资源类型引用。",
                        issues);

                    ValidateReferenceArray(
                        config,
                        item.Profiles,
                        itemPath + ".profiles",
                        "规则应用项包含无效的检测配置档引用。",
                        issues);
                }
            }

            WhiteListItem[] whiteListItems =
                config.WhiteListItems;

            if (whiteListItems == null)
            {
                return;
            }

            for (int i = 0;
                 i < whiteListItems.Length;
                 i++)
            {
                WhiteListItem item = whiteListItems[i];
                string itemPath =
                    "whiteListItems.Array.data[" + i + "]";

                if (item == null)
                {
                    AddIssue(
                        issues,
                        config,
                        EConfigPage.RuleApply,
                        itemPath,
                        "白名单项数据无效。",
                        EConfigValidationSeverity.Error);
                    continue;
                }

                if (item.RuleConfig != null &&
                    !IsReferenceAvailable(item.RuleConfig))
                {
                    AddIssue(
                        issues,
                        config,
                        EConfigPage.RuleApply,
                        itemPath + ".ruleConfig",
                        "白名单引用的规则无效。",
                        EConfigValidationSeverity.Error);
                }

                if (string.IsNullOrEmpty(item.IssueInfoId))
                {
                    continue;
                }

                if (item.RuleConfig == null ||
                    !ContainsIssueId(
                        item.RuleConfig,
                        item.IssueInfoId))
                {
                    AddIssue(
                        issues,
                        config,
                        EConfigPage.RuleApply,
                        itemPath + ".issueInfoId",
                        "白名单引用的问题 ID 无效。",
                        EConfigValidationSeverity.Error);
                }
            }
        }

        /// <summary>
        /// 检查规则应用项必须存在且仍属于当前编辑数据的单个引用。
        /// </summary>
        private void ValidateRequiredReference(
            RuleApplyConfig owner,
            ScriptableObject reference,
            string propertyPath,
            string message,
            IList<ConfigValidationIssue> issues)
        {
            if (reference != null &&
                IsReferenceAvailable(reference))
            {
                return;
            }

            AddIssue(
                issues,
                owner,
                EConfigPage.RuleApply,
                propertyPath,
                message,
                EConfigValidationSeverity.Error);
        }

        /// <summary>
        /// 检查引用数组中的每一项是否仍属于当前编辑数据。
        /// </summary>
        private void ValidateReferenceArray<TConfig>(
            RuleApplyConfig owner,
            TConfig[] references,
            string propertyPath,
            string message,
            IList<ConfigValidationIssue> issues)
            where TConfig : ScriptableObject
        {
            if (references == null)
            {
                return;
            }

            for (int i = 0; i < references.Length; i++)
            {
                if (references[i] != null &&
                    IsReferenceAvailable(references[i]))
                {
                    continue;
                }

                AddIssue(
                    issues,
                    owner,
                    EConfigPage.RuleApply,
                    propertyPath + ".Array.data[" + i + "]",
                    message,
                    EConfigValidationSeverity.Error);
            }
        }

        /// <summary>
        /// 判断规则是否包含指定问题 ID。
        /// </summary>
        private bool ContainsIssueId(
            ScoreRuleConfig ruleConfig,
            string issueInfoId)
        {
            ScoreRuleConfig workingRule =
                getWorkingConfig == null
                    ? ruleConfig
                    : getWorkingConfig(ruleConfig)
                        as ScoreRuleConfig;

            IssueInfo[] issueInfos = workingRule == null
                ? null
                : workingRule.IssueInfos;

            if (issueInfos == null)
            {
                return false;
            }

            for (int i = 0; i < issueInfos.Length; i++)
            {
                if (issueInfos[i] != null &&
                    string.Equals(
                        issueInfos[i].issueInfoId,
                        issueInfoId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 使用数据层提供的当前编辑集合判断对象引用是否有效。
        /// </summary>
        private bool IsReferenceAvailable(
            ScriptableObject config)
        {
            return isConfigAvailable == null ||
                   isConfigAvailable(config);
        }

        /// <summary>
        /// 检查指定配置类型的字符串 ID 是否在工作副本之间重复。
        /// </summary>
        private void ValidateUniqueIds(
            IList<ScriptableObject> configs,
            Type configType,
            string propertyName,
            EConfigPage page,
            string messagePrefix,
            IList<ConfigValidationIssue> issues)
        {
            Dictionary<string, IdLocation> locations =
                new Dictionary<string, IdLocation>(
                    StringComparer.Ordinal);

            for (int i = 0; i < configs.Count; i++)
            {
                ScriptableObject config = configs[i];

                if (config == null ||
                    config.GetType() != configType)
                {
                    continue;
                }

                SerializedObject serializedObject =
                    new SerializedObject(config);

                SerializedProperty property =
                    serializedObject.FindProperty(propertyName);

                if (property == null ||
                    string.IsNullOrWhiteSpace(property.stringValue))
                {
                    continue;
                }

                string id = property.stringValue.Trim();
                IdLocation previous;

                if (locations.TryGetValue(id, out previous))
                {
                    AddIssue(
                        issues,
                        config,
                        page,
                        property.propertyPath,
                        messagePrefix + id + "。",
                        EConfigValidationSeverity.Error);

                    AddIssue(
                        issues,
                        previous.Config,
                        page,
                        previous.PropertyPath,
                        messagePrefix + id + "。",
                        EConfigValidationSeverity.Error);
                    continue;
                }

                locations.Add(
                    id,
                    new IdLocation
                    {
                        Config = config,
                        PropertyPath = property.propertyPath
                    });
            }
        }

        /// <summary>
        /// 检查每条规则内部的问题项 ID 是否重复。
        /// </summary>
        private void ValidateIssueIds(
            IList<ScriptableObject> configs,
            IList<ConfigValidationIssue> issues)
        {
            for (int i = 0; i < configs.Count; i++)
            {
                ScoreRuleConfig config =
                    configs[i] as ScoreRuleConfig;

                if (config == null ||
                    config.IssueInfos == null)
                {
                    continue;
                }

                Dictionary<string, IdLocation> locations =
                    new Dictionary<string, IdLocation>(
                        StringComparer.Ordinal);

                SerializedObject serializedObject =
                    new SerializedObject(config);
                SerializedProperty issueInfos =
                    serializedObject.FindProperty("issueInfos");

                if (issueInfos == null ||
                    !issueInfos.isArray)
                {
                    continue;
                }

                for (int j = 0;
                     j < issueInfos.arraySize;
                     j++)
                {
                    SerializedProperty item =
                        issueInfos.GetArrayElementAtIndex(j);
                    SerializedProperty idProperty =
                        item.FindPropertyRelative("issueInfoId");

                    if (idProperty == null ||
                        string.IsNullOrWhiteSpace(
                            idProperty.stringValue))
                    {
                        continue;
                    }

                    string id = idProperty.stringValue.Trim();
                    IdLocation previous;

                    if (locations.TryGetValue(id, out previous))
                    {
                        AddIssue(
                            issues,
                            config,
                            EConfigPage.Rule,
                            idProperty.propertyPath,
                            "同一规则中的问题 ID 重复：" + id + "。",
                            EConfigValidationSeverity.Error);

                        AddIssue(
                            issues,
                            previous.Config,
                            EConfigPage.Rule,
                            previous.PropertyPath,
                            "同一规则中的问题 ID 重复：" + id + "。",
                            EConfigValidationSeverity.Error);
                        continue;
                    }

                    locations.Add(
                        id,
                        new IdLocation
                        {
                            Config = config,
                            PropertyPath = idProperty.propertyPath
                        });
                }
            }
        }

        /// <summary>
        /// 检查当前生效配置中的启用项，并对可选引用生成非阻断警告。
        /// </summary>
        private void ValidateActiveRuleApply(
            IList<ConfigValidationIssue> issues)
        {
            RuleApplyConfig config =
                getActiveRuleApplyConfig == null
                    ? null
                    : getActiveRuleApplyConfig();

            if (config == null ||
                config.RuleApplyItems == null)
            {
                return;
            }

            bool isPackage =
                IsPackageAsset(
                    getConfigAssetPath == null
                        ? string.Empty
                        : getConfigAssetPath(config));

            Dictionary<string, int> ruleIdIndexes =
                new Dictionary<string, int>(
                    StringComparer.Ordinal);

            for (int i = 0;
                 i < config.RuleApplyItems.Length;
                 i++)
            {
                RuleApplyItem item =
                    config.RuleApplyItems[i];

                if (item == null || !item.IsEnabled)
                {
                    continue;
                }

                string itemPath =
                    "ruleApplyItems.Array.data[" + i + "]";
                string rulePath = itemPath + ".ruleConfig";
                EConfigValidationSeverity activeSeverity =
                    isPackage
                        ? EConfigValidationSeverity.Warning
                        : EConfigValidationSeverity.Error;

                if (item.RuleConfig == null)
                {
                    AddIssue(
                        issues,
                        config,
                        EConfigPage.RuleApply,
                        rulePath,
                        "当前生效配置中的启用项未选择规则。",
                        activeSeverity);
                    continue;
                }

                if (string.IsNullOrEmpty(item.RuleId))
                {
                    AddIssue(
                        issues,
                        config,
                        EConfigPage.RuleApply,
                        rulePath,
                        "当前生效配置中的启用项没有有效 Rule ID。",
                        activeSeverity);
                }
                else
                {
                    int previousIndex;

                    if (ruleIdIndexes.TryGetValue(
                            item.RuleId,
                            out previousIndex))
                    {
                        AddIssue(
                            issues,
                            config,
                            EConfigPage.RuleApply,
                            rulePath,
                            "当前生效配置中的 Rule ID 冲突：" +
                            item.RuleId + "。",
                            activeSeverity);
                    }
                    else
                    {
                        ruleIdIndexes.Add(
                            item.RuleId,
                            i);
                    }
                }

                if (item.AssetTypeConfigs == null ||
                    item.AssetTypeConfigs.Length == 0)
                {
                    AddIssue(
                        issues,
                        config,
                        EConfigPage.RuleApply,
                        itemPath + ".assetTypeConfigs",
                        "资源类型配置为空，该规则不会匹配任何资源类型。",
                        EConfigValidationSeverity.Warning);
                }

                if (item.Profiles == null ||
                    item.Profiles.Length == 0)
                {
                    AddIssue(
                        issues,
                        config,
                        EConfigPage.RuleApply,
                        itemPath + ".profiles",
                        "检测配置档为空，该规则不会匹配任何扫描配置档。",
                        EConfigValidationSeverity.Warning);
                }
            }
        }

        /// <summary>
        /// 将一条诊断问题追加到调用方提供的集合。
        /// </summary>
        private void AddIssue(
            IList<ConfigValidationIssue> issues,
            ScriptableObject config,
            EConfigPage page,
            string propertyPath,
            string message,
            EConfigValidationSeverity severity)
        {
            if (issues == null)
            {
                return;
            }

            issues.Add(
                new ConfigValidationIssue
                {
                    WorkingConfig = config,
                    Page = page,
                    PropertyPath = propertyPath,
                    Message = message,
                    Severity = severity
                });
        }

        /// <summary>
        /// 判断资产路径是否属于 Package，只用于决定当前生效诊断是否阻断保存。
        /// </summary>
        private bool IsPackageAsset(
            string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath) &&
                   assetPath.StartsWith(
                       "Packages/",
                       StringComparison.Ordinal);
        }
    }
}
