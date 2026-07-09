using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 规则应用配置页的 UI。
    /// 负责规则应用项和白名单展示，数据修改继续交给窗口的工作副本流程。
    /// </summary>
    internal sealed class RuleApplyConfigPage
        : ConfigPage<RuleApplyConfig>
    {
        private static readonly ConfigPageCreateOptions createOptions =
            new ConfigPageCreateOptions
            {
                Title = "新建规则应用配置",
                IdLabel = "规则应用配置 ID *",
                IdHint =
                    "必填。规则应用配置的唯一标识，例如 default。" +
                    ConfigIdNamingUtility.RuleHint,
                DisplayNameHint =
                    "必填。编辑器中显示的规则应用配置名称。",
                DefaultAssetName = "RuleApplyConfig_",
                IdPropertyName = "ruleApplyId",
                DefaultLocalFolder =
                    "Assets/Tools/asset_score_tool/Configs/RuleApply",
                DefaultFolderProvider =
                    settings => settings == null
                        ? null
                        : settings.RuleApplyConfigDefaultFolder
            };

        private readonly ConfigWindow host;
        private ConfigWindowDialogHost dialogHost;
        private CreateRuleApplyItemPanel createRuleApplyItemPanel;
        private CreateWhiteListPanel createWhiteListPanel;
        private readonly Action<VisualElement, ScriptableObject, string>
            applyValidationVisual;
        private readonly Func<ScriptableObject, string, bool>
            hasValidationErrorUnderPath;

        /// <summary>
        /// 绑定规则应用配置页面及其工作副本、验证和公共窗口操作。
        /// </summary>
        public RuleApplyConfigPage(
            ConfigWindow host,
            VisualElement windowRoot,
            ConfigWindowData configData,
            Func<ScriptableObject, int>
                getValidationErrorCount,
            Action<ScriptableObject> copyConfigToLocal,
            Action<ScriptableObject> deleteConfig,
            Action<
                VisualElement,
                ScriptableObject,
                Action> trackWorkingCopyChanges,
            Action<IConfigPage> createConfigRequested,
            Action<VisualElement, ScriptableObject, string>
                applyValidationVisual,
            Func<ScriptableObject, string, bool>
                hasValidationErrorUnderPath,
            VisualElement pageRoot = null)
            : base(
                windowRoot,
                "RuleApplyPage",
                "RuleApplyScrollView",
                "RuleApplyTabButton",
                "RuleApplyTabCountLabel",
                "CreateRuleApplyButton",
                "RuleApplySearchField",
                "RuleApplyListContainer",
                "RuleApplyEmptyLabel",
                configData,
                getValidationErrorCount,
                copyConfigToLocal,
                deleteConfig,
                trackWorkingCopyChanges,
                createConfigRequested,
                pageRoot)
        {
            this.host = host;
            this.applyValidationVisual =
                applyValidationVisual;
            this.hasValidationErrorUnderPath =
                hasValidationErrorUnderPath;
        }

        public override EConfigPage PageType =>
            EConfigPage.RuleApply;

        public override string DisplayName => "规则应用配置";

        public override ConfigPageCreateOptions CreateOptions =>
            createOptions;

        internal void ConfigureCreatePanels(
            ConfigWindowDialogHost panelHost,
            CreateRuleApplyItemPanel ruleApplyItemPanel,
            CreateWhiteListPanel whiteListPanel)
        {
            dialogHost = panelHost;
            createRuleApplyItemPanel = ruleApplyItemPanel;
            createWhiteListPanel = whiteListPanel;
        }

        private readonly HashSet<string> activeRuleApplyItemKeys =
            new HashSet<string>();

        private readonly HashSet<string> duplicateRuleApplyItemKeys =
            new HashSet<string>();

        private readonly HashSet<string> expandedRuleApplyItemKeys =
            new HashSet<string>();

        private string activeRuleApplyConfigPath = string.Empty;

        /// <summary>
        /// 为 RuleApplyConfig 构建专用页面。
        ///
        /// 规则应用项编辑 RuleApplyConfig 自身数据；
        /// 白名单区域直接编辑同一个 RuleApplyConfig 工作副本。
        /// </summary>
        protected override void BeforeFresh()
        {
            RebuildStatusCache();
        }

        protected override string GetDisplayName(
            RuleApplyConfig config)
        {
            return string.IsNullOrEmpty(config.DisplayName)
                ? config.name
                : config.DisplayName;
        }

        protected override string GetSummary(
            RuleApplyConfig config)
        {
            int ruleCount = config.RuleApplyItems == null
                ? 0
                : config.RuleApplyItems.Length;

            string activePrefix =
                IsCurrentActiveConfig(config)
                    ? "当前生效　"
                    : string.Empty;

            return activePrefix +
                   "配置 ID：" +
                   config.RuleApplyId +
                   "　规则应用项 " +
                   ruleCount +
                   "　白名单 " +
                   GetWhiteListCount(config);
        }

        /// <summary>
        /// 将规则应用项、引用配置和白名单内容加入页面搜索文本。
        /// </summary>
        protected override string GetSearchText(
            RuleApplyConfig config)
        {
            List<string> values =
                new List<string>
                {
                    base.GetSearchText(config),
                    config.RuleApplyId
                };

            if (config.RuleApplyItems != null)
            {
                for (int i = 0;
                     i < config.RuleApplyItems.Length;
                     i++)
                {
                    RuleApplyItem item = config.RuleApplyItems[i];

                    if (item == null)
                    {
                        continue;
                    }

                    values.Add(item.RuleId);

                    if (item.RuleConfig != null)
                    {
                        values.Add(item.RuleConfig.DisplayName);
                    }

                    AddAssetTypeSearchValues(values, item.AssetTypeConfigs);
                    AddProfileSearchValues(values, item.Profiles);
                }
            }

            if (config.WhiteListItems != null)
            {
                for (int i = 0;
                     i < config.WhiteListItems.Length;
                     i++)
                {
                    WhiteListItem item = config.WhiteListItems[i];

                    if (item == null)
                    {
                        continue;
                    }

                    values.Add(item.AssetPath);
                    values.Add(item.IssueInfoId);
                    values.Add(item.Remark);

                    if (item.RuleConfig != null)
                    {
                        values.Add(item.RuleConfig.RuleId);
                        values.Add(item.RuleConfig.DisplayName);
                    }
                }
            }

            return string.Join("\n", values);
        }

        /// <summary>
        /// 将资源类型的 ID 和显示名称加入搜索文本。
        /// </summary>
        private void AddAssetTypeSearchValues(
            List<string> values,
            AssetTypeConfig[] configs)
        {
            if (configs == null)
            {
                return;
            }

            for (int i = 0; i < configs.Length; i++)
            {
                AssetTypeConfig config = configs[i];

                if (config == null)
                {
                    continue;
                }

                values.Add(config.AssetTypeId);
                values.Add(config.DisplayName);
            }
        }

        /// <summary>
        /// 将检测配置档的 ID 和显示名称加入搜索文本。
        /// </summary>
        private void AddProfileSearchValues(
            List<string> values,
            ScoreRuleProfileConfig[] configs)
        {
            if (configs == null)
            {
                return;
            }

            for (int i = 0; i < configs.Length; i++)
            {
                ScoreRuleProfileConfig config = configs[i];

                if (config == null)
                {
                    continue;
                }

                values.Add(config.ProfileId);
                values.Add(config.DisplayName);
            }
        }

        protected override void BuildTypedFields(
            RuleApplyConfig config,
            VisualElement container,
            bool editable)
        {
            SerializedObject serializedObject =
                new SerializedObject(config);

            VisualElement baseSection =
                host.PageUIBuilder.CreateContentSection("规则应用配置");
            baseSection.AddToClassList(
                "html-content-section-first");

            VisualElement baseBody =
                host.PageUIBuilder.GetSectionBody(baseSection);

            host.PageUIBuilder.AddPropertyField(
                baseBody,
                serializedObject,
                "ruleApplyId",
                "规则应用配置 ID *",
                editable,
                "规则应用配置的唯一标识，例如 default。" +
                ConfigIdNamingUtility.RuleHint,
                true);

            host.PageUIBuilder.AddPropertyField(
                baseBody,
                serializedObject,
                "displayName",
                "显示名称 *",
                editable,
                "编辑器中显示的规则应用配置名称。",
                true);

            container.Add(baseSection);

            SerializedProperty ruleApplyItems =
                serializedObject.FindProperty(
                    "ruleApplyItems");

            container.Add(
                BuildRuleApplyItemsSection(
                    config,
                    ruleApplyItems,
                    editable));

            container.Add(
                BuildRuleApplyWhiteListSection(
                    config,
                    editable));
        }

        private VisualElement BuildRuleApplyItemsSection(
            RuleApplyConfig config,
            SerializedProperty ruleApplyItems,
            bool editable)
        {
            Button addButton = new Button();
            addButton.text = "+ 添加规则应用项";
            addButton.AddToClassList("small-button");
            addButton.SetEnabled(
                editable &&
                ruleApplyItems != null &&
                ruleApplyItems.isArray);

            VisualElement section =
                host.PageUIBuilder.CreateContentSection(
                    "规则应用项",
                    addButton);
            section.AddToClassList(
                "html-content-section-divider");

            VisualElement body =
                host.PageUIBuilder.GetSectionBody(section);

            body.Add(
                host.PageUIBuilder.CreateFieldHint(
                    "规则配置、资源类型配置和检测配置档都允许留空；新增时会先打开填写窗口。"));

            if (ruleApplyItems == null ||
                !ruleApplyItems.isArray)
            {
                Label warning =
                    new Label(
                        "未找到规则应用项序列化字段 ruleApplyItems。");

                warning.AddToClassList("warning-message");
                body.Add(warning);
                return section;
            }

            string arrayPath =
                ruleApplyItems.propertyPath;

            addButton.clicked +=
                delegate
                {
                    createRuleApplyItemPanel.Open(
                        dialogHost,
                        host.configData,
                        config,
                        arrayPath,
                        delegate
                        {
                            host.NotifyWorkingCopyChanged(config);
                            Fresh();
                        });
                };

            if (ruleApplyItems.arraySize == 0)
            {
                Label emptyLabel =
                    new Label("没有规则应用项");

                emptyLabel.AddToClassList(
                    "empty-inline-label");

                body.Add(emptyLabel);
                return section;
            }

            SerializedObject serializedObject =
                new SerializedObject(config);

            for (int i = 0;
                 i < ruleApplyItems.arraySize;
                 i++)
            {
                SerializedProperty item =
                    ruleApplyItems
                        .GetArrayElementAtIndex(i)
                        .Copy();

                body.Add(
                    BuildRuleApplyItem(
                        config,
                        item,
                        arrayPath,
                        i,
                        editable));
            }

            return section;
        }

        /// <summary>
        /// 使用通用折叠卡片构建单条规则应用项及其标题栏操作。
        /// </summary>
        private VisualElement BuildRuleApplyItem(
            RuleApplyConfig owner,
            SerializedProperty itemProperty,
            string arrayPath,
            int index,
            bool editable)
        {
            SerializedProperty ruleConfigProperty =
                itemProperty.FindPropertyRelative(
                    "ruleConfig");

            SerializedProperty assetTypeConfigsProperty =
                itemProperty.FindPropertyRelative(
                    "assetTypeConfigs");

            SerializedProperty profilesProperty =
                itemProperty.FindPropertyRelative(
                    "profiles");

            SerializedProperty isEnabledProperty =
                itemProperty.FindPropertyRelative(
                    "isEnabled");

            ScoreRuleConfig selectedRule =
                ruleConfigProperty == null
                    ? null
                    : ruleConfigProperty
                            .objectReferenceValue
                        as ScoreRuleConfig;

            string statusText;
            string statusClass;

            bool isEnabled =
                isEnabledProperty != null &&
                isEnabledProperty.boolValue;

            GetRuleApplyItemStatus(
                owner,
                index,
                selectedRule,
                isEnabled,
                out statusText,
                out statusClass);

            string itemKey =
                BuildRuleApplyItemKey(owner, index);

            FoldoutCardView card =
                new FoldoutCardView(
                    host.foldoutCardViewVisualTreeAsset);

            card.Title = selectedRule == null
                ? "未选择规则"
                : host.GetDisplayName(selectedRule);
            card.Subtitle = selectedRule == null
                ? "选择规则配置后可展开规则内容"
                : selectedRule.RuleId;
            card.IconText = "RA";
            card.StatusText = statusText;
            card.StatusClass = statusClass;
            card.IsMuted = !isEnabled;
            card.IsExpanded =
                expandedRuleApplyItemKeys.Contains(itemKey) ||
                hasValidationErrorUnderPath(
                    owner,
                    itemProperty.propertyPath);
            card.ExpandedChanged +=
                delegate(bool expanded)
                {
                    if (expanded)
                    {
                        expandedRuleApplyItemKeys.Add(itemKey);
                    }
                    else
                    {
                        expandedRuleApplyItemKeys.Remove(itemKey);
                    }
                };

            if (editable && isEnabledProperty != null)
            {
                Button enabledButton =
                    new Button(
                        delegate
                        {
                            ToggleRuleApplyItemEnabled(
                                owner,
                                isEnabledProperty.propertyPath);
                        });

                enabledButton.text = isEnabled
                    ? "已启用"
                    : "未启用";
                enabledButton.AddToClassList("small-button");
                enabledButton.AddToClassList(
                    "rule-enable-button");
                enabledButton.AddToClassList(
                    isEnabled
                        ? "enabled"
                        : "disabled");
                card.HeaderActionContainer.Add(enabledButton);

                Button deleteButton =
                    new Button(
                        delegate
                        {
                            host.DeleteArrayElement(
                                owner,
                                arrayPath,
                                index,
                                "删除规则应用项",
                                Fresh);
                        });

                deleteButton.text = "删除";
                deleteButton.AddToClassList("small-button");
                deleteButton.AddToClassList("danger-button");
                card.HeaderActionContainer.Add(deleteButton);
            }

            if (ruleConfigProperty != null)
            {
                VisualElement ruleSection = new VisualElement();
                ruleSection.AddToClassList("rule-apply-item-subsection");
                ruleSection.AddToClassList(
                    card.ContentContainer.childCount == 0
                        ? "rule-apply-item-subsection-first"
                        : "rule-apply-item-subsection-divider");

                Label ruleTitle = new Label("规则配置");
                ruleTitle.AddToClassList("subsection-title");
                ruleSection.Add(ruleTitle);
                ruleSection.Add(
                    BuildRuleConfigSelector(
                        owner,
                        ruleConfigProperty.propertyPath,
                        selectedRule,
                        editable));

                card.ContentContainer.Add(
                    ruleSection);
            }

            if (assetTypeConfigsProperty != null)
            {
                VisualElement assetTypeSection =
                    BuildObjectReferenceArray(
                        owner,
                        assetTypeConfigsProperty.propertyPath,
                        "资源类型配置",
                        "用于限定当前规则适用的资源类型；允许为空，但为空时不会匹配任何资源。",
                        typeof(AssetTypeConfig),
                        editable,
                        Fresh);
                assetTypeSection.AddToClassList(
                    card.ContentContainer.childCount == 0
                        ? "rule-apply-item-subsection-first"
                        : "rule-apply-item-subsection-divider");
                card.ContentContainer.Add(assetTypeSection);
            }

            if (profilesProperty != null)
            {
                VisualElement profileSection =
                    BuildObjectReferenceArray(
                        owner,
                        profilesProperty.propertyPath,
                        "检测配置档",
                        "用于限定当前规则适用的检测配置档；允许为空，但为空时不会匹配任何扫描配置档。",
                        typeof(ScoreRuleProfileConfig),
                        editable,
                        Fresh);
                profileSection.AddToClassList(
                    card.ContentContainer.childCount == 0
                        ? "rule-apply-item-subsection-first"
                        : "rule-apply-item-subsection-divider");
                card.ContentContainer.Add(profileSection);
            }

            return card;
        }

        /// <summary>
        /// 切换规则应用项启用状态，并通过窗口统一刷新工作副本和诊断状态。
        /// </summary>
        private void ToggleRuleApplyItemEnabled(
            RuleApplyConfig owner,
            string propertyPath)
        {
            SerializedObject serializedObject =
                new SerializedObject(owner);
            SerializedProperty property =
                serializedObject.FindProperty(propertyPath);

            if (property == null ||
                property.propertyType != SerializedPropertyType.Boolean)
            {
                return;
            }

            Undo.RecordObject(owner, "切换规则应用项状态");
            property.boolValue = !property.boolValue;
            serializedObject.ApplyModifiedProperties();
            host.NotifyWorkingCopyChanged(owner);
            Fresh();
        }

        /// <summary>
        /// 构建规则引用及其选择、定位和独立弹窗编辑入口。
        /// </summary>
        private VisualElement BuildRuleConfigSelector(
            RuleApplyConfig owner,
            string propertyPath,
            ScoreRuleConfig selectedRule,
            bool editable)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("reference-form-row");

            row.Add(host.PageUIBuilder.CreateReferenceDisplay(selectedRule));
            applyValidationVisual(
                row,
                owner,
                propertyPath);

            Button selectButton =
                new Button(
                    delegate
                    {
                        host.ShowSelectionDialog(
                            "选择规则配置",
                            typeof(ScoreRuleConfig),
                            selectedRule,
                            false,
                            string.Empty,
                            delegate(
                                ScriptableObject selected)
                            {
                                host.SetObjectReference(
                                    owner,
                                    propertyPath,
                                    selected,
                                    Fresh);
                            });
                    });

            selectButton.text = "选择";
            selectButton.AddToClassList("small-button");
            selectButton.SetEnabled(editable);
            row.Add(selectButton);

            if (selectedRule != null)
            {
                Button pingButton =
                    new Button(
                        delegate
                        {
                            Selection.activeObject = selectedRule;
                            EditorGUIUtility.PingObject(
                                selectedRule);
                        });

                pingButton.text = "定位";
                pingButton.AddToClassList("small-button");
                row.Add(pingButton);

                Button editButton =
                    new Button(
                        delegate
                        {
                            host.ShowRuleConfigEditor(
                                selectedRule);
                        });

                string rulePath =
                    host.configData.GetConfigAssetPath(
                        selectedRule);

                editButton.text =
                    ConfigWindowData.IsPackageAsset(rulePath)
                        ? "预览规则"
                        : "编辑规则";

                editButton.AddToClassList("small-button");
                editButton.AddToClassList("primary-button");
                row.Add(editButton);

                Button clearButton =
                    new Button(
                        delegate
                        {
                            host.SetObjectReference(
                                owner,
                                propertyPath,
                                null,
                                Fresh);
                        });

                clearButton.text = "清除";
                clearButton.AddToClassList("small-button");
                clearButton.SetEnabled(editable);
                row.Add(clearButton);
            }

            VisualElement block = new VisualElement();
            block.AddToClassList("rule-reference-content");
            block.Add(row);
            block.Add(
                host.PageUIBuilder.CreateFieldHint(
                    "对应的规则配置；允许留空，留空时该应用项不会注册生效。"));
            return block;
        }

        /// <summary>
        /// 构建规则应用项中可整体选择和单项移除的配置引用数组编辑区域。
        /// </summary>
        private VisualElement BuildObjectReferenceArray(
            ScriptableObject owner,
            string arrayPath,
            string displayName,
            string hint,
            Type elementType,
            bool editable,
            Action refreshAction)
        {
            VisualElement field = new VisualElement();
            field.AddToClassList("rule-apply-item-subsection");

            Label label = new Label(displayName);
            label.AddToClassList("subsection-title");

            field.Add(label);

            VisualElement list = new VisualElement();
            list.AddToClassList(
                "reference-array-list");

            field.Add(list);

            if (!string.IsNullOrEmpty(hint))
            {
                field.Add(host.PageUIBuilder.CreateFieldHint(hint));
            }

            applyValidationVisual(
                field,
                owner,
                arrayPath);

            SerializedObject serializedObject =
                new SerializedObject(owner);

            SerializedProperty arrayProperty =
                serializedObject.FindProperty(arrayPath);

            if (arrayProperty == null ||
                !arrayProperty.isArray)
            {
                Label warning =
                    new Label("未找到数组字段。");

                warning.AddToClassList("warning-message");
                list.Add(warning);
                return field;
            }

            List<ScriptableObject> currentValues =
                new List<ScriptableObject>();

            for (int i = 0;
                 i < arrayProperty.arraySize;
                 i++)
            {
                int elementIndex = i;

                SerializedProperty element =
                    arrayProperty
                        .GetArrayElementAtIndex(i);

                ScriptableObject value =
                    element.objectReferenceValue
                        as ScriptableObject;

                if (value != null)
                {
                    currentValues.Add(value);
                }

                VisualElement chip =
                    BuildReferenceChip(
                        value,
                        elementType == typeof(AssetTypeConfig)
                            ? "config-reference-chip-asset-type"
                            : "config-reference-chip-profile");

                if (editable)
                {
                    Button removeButton =
                        new Button(
                            delegate
                            {
                                host.DeleteArrayElement(
                                    owner,
                                    arrayPath,
                                    elementIndex,
                                    "移除配置引用",
                                    refreshAction);
                            });

                    removeButton.text = "×";
                    removeButton.AddToClassList(
                        "config-reference-chip-remove");

                    chip.Add(removeButton);
                }

                list.Add(chip);
            }

            if (editable)
            {
                Button addButton =
                    new Button(
                        delegate
                        {
                            host.ShowMultiSelectionDialog(
                                "选择" + displayName,
                                elementType,
                                currentValues,
                                delegate(
                                    IReadOnlyList<ScriptableObject> selected)
                                {
                                    ReplaceArrayObjectReferences(
                                        owner,
                                        arrayPath,
                                        selected,
                                        refreshAction);
                                });
                        });

                addButton.text = "选择";
                addButton.AddToClassList("small-button");
                list.Add(addButton);
            }

            return field;
        }

        /// <summary>
        /// 构建 HTML 对齐样式的资源类型或检测配置档标签。
        /// </summary>
        private VisualElement BuildReferenceChip(
            ScriptableObject value,
            string chipClass)
        {
            VisualElement chip = new VisualElement();
            chip.AddToClassList("config-reference-chip");
            chip.AddToClassList(chipClass);

            Label label =
                new Label(
                    value == null
                        ? "未选择"
                        : host.GetDisplayName(value));
            label.AddToClassList("config-reference-chip-label");
            chip.Add(label);

            return chip;
        }

        /// <summary>
        /// 使用多选面板确认的引用整体替换指定工作副本中的数组字段。
        /// </summary>
        private void ReplaceArrayObjectReferences(
            ScriptableObject owner,
            string arrayPath,
            IReadOnlyList<ScriptableObject> selected,
            Action refreshAction)
        {
            SerializedObject serializedObject =
                new SerializedObject(owner);

            SerializedProperty arrayProperty =
                serializedObject.FindProperty(arrayPath);

            if (arrayProperty == null ||
                !arrayProperty.isArray)
            {
                return;
            }

            Undo.RecordObject(
                owner,
                "修改配置引用");

            int selectedCount = selected == null
                ? 0
                : selected.Count;
            arrayProperty.arraySize = selectedCount;

            for (int i = 0; i < selectedCount; i++)
            {
                arrayProperty
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue = selected[i];
            }

            serializedObject.ApplyModifiedProperties();
            host.NotifyWorkingCopyChanged(owner);
            refreshAction?.Invoke();
        }

        // ------------------------------------------------------------------
        // 生效状态
        // ------------------------------------------------------------------

        /// <summary>
        /// 只基于配置窗口预览中的当前生效配置建立规则状态缓存。
        /// 未启用项不参与 Rule ID 冲突计算，相同 RuleId 的后续启用项标记为未生效。
        /// </summary>
        public void RebuildStatusCache()
        {
            activeRuleApplyItemKeys.Clear();
            duplicateRuleApplyItemKeys.Clear();
            activeRuleApplyConfigPath = string.Empty;

            HashSet<string> usedRuleIds =
                new HashSet<string>();

            RuleApplyConfig activeConfig =
                host.GetPreviewActiveRuleApplyConfig();

            if (activeConfig == null)
            {
                return;
            }

            activeRuleApplyConfigPath =
                host.configData.GetConfigAssetPath(activeConfig);

            if (activeConfig.RuleApplyItems == null)
            {
                return;
            }

            for (int i = 0;
                 i < activeConfig.RuleApplyItems.Length;
                 i++)
            {
                RuleApplyItem item =
                    activeConfig.RuleApplyItems[i];

                if (item == null ||
                    !item.IsEnabled ||
                    item.RuleConfig == null ||
                    string.IsNullOrEmpty(item.RuleId))
                {
                    continue;
                }

                string itemKey =
                    BuildRuleApplyItemKey(
                        activeConfig,
                        i);

                if (usedRuleIds.Add(item.RuleId))
                {
                    activeRuleApplyItemKeys.Add(itemKey);
                }
                else
                {
                    duplicateRuleApplyItemKeys.Add(itemKey);
                }
            }
        }

        /// <summary>
        /// 判断指定 RuleApplyConfig 是否是配置窗口预览中的当前生效配置。
        /// 通过真实资产路径比较，兼容本地配置工作副本与 Package 配置对象。
        /// </summary>
        private bool IsCurrentActiveConfig(
            RuleApplyConfig config)
        {
            if (config == null ||
                string.IsNullOrEmpty(activeRuleApplyConfigPath))
            {
                return false;
            }

            return string.Equals(
                host.configData.GetConfigAssetPath(config),
                activeRuleApplyConfigPath,
                StringComparison.Ordinal);
        }

        private string BuildRuleApplyItemKey(
            RuleApplyConfig config,
            int index)
        {
            return host.configData.GetConfigAssetPath(config) +
                   "#" +
                   index;
        }

        private void GetRuleApplyItemStatus(
            RuleApplyConfig owner,
            int index,
            ScoreRuleConfig ruleConfig,
            bool enabled,
            out string text,
            out string className)
        {
            if (ruleConfig == null)
            {
                text = "未配置";
                className = FoldoutCardView.StatusWarningClass;
                return;
            }

            if (string.IsNullOrEmpty(ruleConfig.RuleId))
            {
                text = "未配置 · Rule ID 为空";
                className = FoldoutCardView.StatusWarningClass;
                return;
            }

            if (!IsCurrentActiveConfig(owner))
            {
                text = "未生效 · 非当前配置";
                className = FoldoutCardView.StatusActiveClass;
                return;
            }

            string key =
                BuildRuleApplyItemKey(
                    owner,
                    index);

            if (duplicateRuleApplyItemKeys.Contains(key))
            {
                text = "未生效 · 规则 ID 冲突";
                className = FoldoutCardView.StatusWarningClass;
                return;
            }

            if (!enabled)
            {
                text = "未启用";
                className = FoldoutCardView.StatusWarningClass;
                return;
            }

            text = "生效";
            className = FoldoutCardView.StatusSuccessClass;
        }

        /// <summary>
        /// 白名单直接属于当前 RuleApplyConfig，不再按 Profile 分组。
        /// </summary>
        private VisualElement BuildRuleApplyWhiteListSection(
            RuleApplyConfig ruleApplyConfig,
            bool editable)
        {
            Button addButton = new Button(
                delegate
                {
                    createWhiteListPanel.Open(
                        dialogHost,
                        host.configData,
                        ruleApplyConfig,
                        delegate
                        {
                            host.NotifyWorkingCopyChanged(
                                ruleApplyConfig);
                            Fresh();
                        });
                });

            addButton.text = "+ 添加白名单";
            addButton.AddToClassList("small-button");
            addButton.SetEnabled(editable);

            VisualElement section =
                host.PageUIBuilder.CreateContentSection("白名单", addButton);
            section.AddToClassList(
                "html-content-section-divider");

            VisualElement body =
                host.PageUIBuilder.GetSectionBody(section);

            body.Add(
                host.PageUIBuilder.CreateFieldHint(
                    "白名单保存在当前规则应用配置中；规则为空时匹配全部规则，问题 ID 为空时匹配该规则下全部问题。"));

            SerializedObject serializedObject =
                new SerializedObject(ruleApplyConfig);

            SerializedProperty whiteListItems =
                serializedObject.FindProperty(
                    "whiteListItems");

            if (whiteListItems == null ||
                !whiteListItems.isArray)
            {
                Label warning =
                    new Label(
                        "当前规则应用配置中未找到白名单字段 whiteListItems。");

                warning.AddToClassList(
                    "warning-message");
                body.Add(warning);
                addButton.SetEnabled(false);
                return section;
            }

            if (whiteListItems.arraySize == 0)
            {
                Label emptyLabel =
                    new Label("没有白名单");

                emptyLabel.AddToClassList(
                    "empty-inline-label");
                body.Add(emptyLabel);
                return section;
            }

            string arrayPath =
                whiteListItems.propertyPath;

            for (int i = 0;
                 i < whiteListItems.arraySize;
                 i++)
            {
                SerializedProperty itemProperty =
                    whiteListItems
                        .GetArrayElementAtIndex(i)
                        .Copy();

                body.Add(
                    BuildWhiteListItem(
                        ruleApplyConfig,
                        serializedObject,
                        itemProperty,
                        arrayPath,
                        i,
                        editable,
                        i == 0 ||
                        hasValidationErrorUnderPath(
                            ruleApplyConfig,
                            itemProperty.propertyPath)));
            }

            host.TrackWorkingCopyChanges(
                body,
                ruleApplyConfig);

            return section;
        }

        public int GetWhiteListCount(
            RuleApplyConfig ruleApplyConfig)
        {
            RuleApplyConfig workingConfig =
                host.configData.GetWorkingConfig(ruleApplyConfig);

            if (workingConfig == null ||
                workingConfig.WhiteListItems == null)
            {
                return 0;
            }

            return workingConfig.WhiteListItems.Length;
        }

        private VisualElement BuildWhiteListItem(
            RuleApplyConfig owner,
            SerializedObject serializedObject,
            SerializedProperty itemProperty,
            string arrayPath,
            int index,
            bool editable,
            bool expanded)
        {
            string assetPath =
                host.GetSerializedString(
                    itemProperty,
                    "assetPath");

            string title =
                string.IsNullOrEmpty(assetPath)
                    ? "未填写资源路径"
                    : Path.GetFileName(
                        assetPath.TrimEnd('/', '\\'));

            FoldoutCardView card =
                new FoldoutCardView(
                    host.foldoutCardViewVisualTreeAsset);

            card.Title =
                string.IsNullOrEmpty(title)
                    ? assetPath
                    : title;
            card.Subtitle =
                string.IsNullOrEmpty(assetPath)
                    ? "资源路径为空"
                    : assetPath;
            card.IconText = "WL";
            card.StatusText = string.Empty;
            card.IsMuted = false;
            card.IsExpanded = expanded;

            if (editable)
            {
                Button deleteButton =
                    new Button(
                        delegate
                        {
                            host.DeleteArrayElement(
                                owner,
                                arrayPath,
                                index,
                                "删除白名单",
                                Fresh);
                        });

                deleteButton.text = "删除";
                deleteButton.AddToClassList("small-button");
                deleteButton.AddToClassList("danger-button");
                card.HeaderActionContainer.Add(deleteButton);
            }

            SerializedProperty assetPathProperty =
                itemProperty.FindPropertyRelative(
                    "assetPath");

            if (assetPathProperty != null)
            {
                card.ContentContainer.Add(
                    BuildWhiteListAssetPathRow(
                        owner,
                        assetPathProperty,
                        editable));
            }

            SerializedProperty ruleProperty =
                itemProperty.FindPropertyRelative(
                    "ruleConfig");

            if (ruleProperty != null)
            {
                card.ContentContainer.Add(
                    BuildWhiteListRuleSelector(
                        owner,
                        ruleProperty.propertyPath,
                        ruleProperty.objectReferenceValue
                            as ScoreRuleConfig,
                        editable));
            }

            host.PageUIBuilder.AddRelativePropertyField(
                card.ContentContainer,
                owner,
                serializedObject,
                itemProperty,
                "issueInfoId",
                "问题 ID",
                editable,
                "可留空；留空时匹配所选规则下的全部问题。" +
                ConfigIdNamingUtility.RuleHint);

            host.PageUIBuilder.AddRelativeTextField(
                card.ContentContainer,
                itemProperty,
                "remark",
                "备注",
                editable,
                true,
                "可选，用于说明加入白名单的原因。");

            return card;
        }

        private VisualElement BuildWhiteListAssetPathRow(
            RuleApplyConfig owner,
            SerializedProperty assetPathProperty,
            bool editable)
        {
            VisualElement block = new VisualElement();

            VisualElement row = new VisualElement();
            row.AddToClassList("reference-form-row");
            row.AddToClassList("white-list-path-row");

            Label label = new Label("资源路径 *");
            label.AddToClassList("reference-form-label");
            row.Add(label);

            VisualElement content = new VisualElement();
            content.AddToClassList(
                "white-list-path-content");

            TextField pathField = new TextField();
            pathField.BindProperty(
                assetPathProperty.Copy());
            pathField.AddToClassList(
                "white-list-path-field");
            pathField.SetEnabled(editable);

            applyValidationVisual(
                pathField,
                owner,
                assetPathProperty.propertyPath);

            content.Add(pathField);

            VisualElement actions = new VisualElement();
            actions.AddToClassList(
                "white-list-path-actions");

            Button fileButton =
                new Button(
                    delegate
                    {
                        SelectWhiteListPath(
                            owner,
                            assetPathProperty.propertyPath,
                            false);
                    });

            fileButton.text = "选择文件";
            fileButton.AddToClassList("small-button");
            fileButton.AddToClassList(
                "white-list-path-button");
            fileButton.SetEnabled(editable);
            actions.Add(fileButton);

            Button folderButton =
                new Button(
                    delegate
                    {
                        SelectWhiteListPath(
                            owner,
                            assetPathProperty.propertyPath,
                            true);
                    });

            folderButton.text = "选择文件夹";
            folderButton.AddToClassList("small-button");
            folderButton.AddToClassList(
                "white-list-path-button");
            folderButton.SetEnabled(editable);
            actions.Add(folderButton);

            content.Add(actions);
            row.Add(content);
            block.Add(row);
            block.Add(
                host.PageUIBuilder.CreateFieldHint(
                    "可以选择单个资源，也可以选择文件夹递归匹配。",
                    true));

            return block;
        }

        private VisualElement BuildWhiteListRuleSelector(
            RuleApplyConfig owner,
            string propertyPath,
            ScoreRuleConfig ruleConfig,
            bool editable)
        {
            ObjectReferenceSelector selector =
                new ObjectReferenceSelector(
                    host.objectReferenceSelectorVisualTreeAsset);

            selector.LabelText = "规则配置";
            selector.DisplayName =
                ruleConfig == null
                    ? "全部规则"
                    : host.GetDisplayName(
                        host.configData.GetWorkingConfig(ruleConfig));
            selector.Metadata =
                ruleConfig == null
                    ? "未指定规则配置"
                    : ruleConfig.name;
            selector.HintText =
                "可留空；留空时匹配全部规则。";
            selector.SelectButtonText = "选择";
            selector.SelectButtonVisible = true;
            selector.SelectButtonEnabled = editable;
            selector.ClearButtonText = "清除";
            selector.ClearButtonVisible =
                editable && ruleConfig != null;
            selector.PingButtonVisible = ruleConfig != null;
            selector.PingButtonEnabled = true;
            selector.EditButtonVisible = false;

            selector.SelectRequested +=
                delegate
                {
                    host.ShowSelectionDialog(
                        "选择白名单规则",
                        typeof(ScoreRuleConfig),
                        ruleConfig,
                        true,
                        "全部规则",
                        delegate(ScriptableObject selected)
                        {
                            host.SetObjectReference(
                                owner,
                                propertyPath,
                                selected,
                                Fresh);
                        });
                };

            selector.ClearRequested +=
                delegate
                {
                    host.SetObjectReference(
                        owner,
                        propertyPath,
                        null,
                        Fresh);
                };

            selector.PingRequested +=
                delegate
                {
                    Selection.activeObject = ruleConfig;
                    EditorGUIUtility.PingObject(ruleConfig);
                };

            return selector;
        }

        private void SetWhiteListStringValue(
            RuleApplyConfig owner,
            string propertyPath,
            string value)
        {
            Undo.RecordObject(
                owner,
                "修改白名单资源路径");

            SerializedObject serializedObject =
                new SerializedObject(owner);

            SerializedProperty property =
                serializedObject.FindProperty(
                    propertyPath);

            if (property == null)
            {
                return;
            }

            property.stringValue =
                value == null
                    ? string.Empty
                    : value;

            serializedObject.ApplyModifiedProperties();
            host.NotifyWorkingCopyChanged(owner);
        }

        private void SelectWhiteListPath(
            RuleApplyConfig owner,
            string propertyPath,
            bool selectFolder)
        {
            string selectedPath =
                selectFolder
                    ? EditorUtility.OpenFolderPanel(
                        "选择白名单文件夹",
                        Application.dataPath,
                        string.Empty)
                    : EditorUtility.OpenFilePanel(
                        "选择白名单文件",
                        Application.dataPath,
                        string.Empty);

            if (string.IsNullOrEmpty(selectedPath))
            {
                return;
            }

            string unityPath;

            if (!host.TryConvertToUnityPath(
                    selectedPath,
                    out unityPath))
            {
                EditorUtility.DisplayDialog(
                    "白名单",
                    "请选择当前 Unity 项目内的资源或文件夹。",
                    "确定");
                return;
            }

            SetWhiteListStringValue(
                owner,
                propertyPath,
                unityPath);

            Fresh();
        }
    }
}
