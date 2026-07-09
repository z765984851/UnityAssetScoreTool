using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检测规则配置页的 UI。
    /// 负责字段和问题项展示，并把新增、删除意图交给窗口的数据操作流程。
    /// </summary>
    internal sealed class RuleConfigPage
        : ConfigPage<ScoreRuleConfig>
    {
        private static readonly ConfigPageCreateOptions createOptions =
            new ConfigPageCreateOptions
            {
                Title = "新建检测规则配置",
                IdLabel = "规则 ID *",
                IdHint =
                    "必填。规则的唯一标识，需要与检测脚本的规则 ID 保持一致。" +
                    ConfigIdNamingUtility.RuleHint,
                IdPlaceholder = "texture.read_write",
                DisplayNameHint =
                    "必填。编辑器中显示的规则名称。",
                DefaultAssetName = "ScoreRule_",
                IdPropertyName = "ruleId",
                DefaultLocalFolder =
                    "Assets/Tools/asset_score_tool/Configs/Rules",
                DefaultFolderProvider =
                    settings => settings == null
                        ? null
                        : settings.ScoreRuleConfigDefaultFolder
            };

        private readonly Func<string, Button, VisualElement>
            createSection;

        private readonly Action<
            VisualElement,
            SerializedObject,
            string,
            string,
            bool,
            string,
            bool> addPropertyField;

        private readonly Action<
            VisualElement,
            SerializedObject,
            string,
            string,
            bool,
            bool,
            string,
            bool> addTextField;

        private readonly Action<
            VisualElement,
            SerializedProperty,
            string,
            string,
            bool,
            bool,
            string,
            bool> addRelativeTextField;

        private readonly Action<
            VisualElement,
            ScriptableObject,
            string> applyValidationVisual;

        private readonly Func<string, bool, Label>
            createFieldHint;

        private readonly Func<ScriptableObject, string, bool>
            hasValidationErrorUnderPath;

        private readonly Func<SerializedProperty, string, string>
            getSerializedString;

        private readonly Dictionary<string, Type> checkerTypesByRuleId =
            new Dictionary<string, Type>();

        private CreateCheckerPanel createCheckerPanel;
        private ConfigWindowDialogHost dialogHost;
        private ConfigWindowData configData;
        private Func<DefaultAsset, string> getDefaultSaveFolder;
        private CreateIssueInfoPanel createIssueInfoPanel;
        private Action<ScoreRuleConfig> issueInfoCreated;

        public event Action<
            ScoreRuleConfig,
            string,
            int> DeleteIssueInfoRequested;

        public event Action<ScoreRuleConfig> CheckerCreated;

        public RuleConfigPage(
            VisualElement windowRoot,
            ConfigWindowData windowData,
            Func<ScriptableObject, int>
                getValidationErrorCount,
            Action<ScriptableObject> copyConfigToLocal,
            Action<ScriptableObject> deleteConfig,
            Action<
                VisualElement,
                ScriptableObject,
                Action> trackWorkingCopyChanges,
            Action<IConfigPage> createConfigRequested,
            Func<string, Button, VisualElement> createSection,
            Action<
                VisualElement,
                SerializedObject,
                string,
                string,
                bool,
                string,
                bool> addPropertyField,
            Action<
                VisualElement,
                SerializedObject,
                string,
                string,
                bool,
                bool,
                string,
                bool> addTextField,
            Action<
                VisualElement,
                SerializedProperty,
                string,
                string,
                bool,
                bool,
                string,
                bool> addRelativeTextField,
            Action<
                VisualElement,
                ScriptableObject,
                string> applyValidationVisual,
            Func<string, bool, Label> createFieldHint,
            Func<ScriptableObject, string, bool>
                hasValidationErrorUnderPath,
            Func<SerializedProperty, string, string>
                getSerializedString,
            VisualElement pageRoot = null)
            : base(
                windowRoot,
                "RulePage",
                "RuleScrollView",
                "RuleTabButton",
                "RuleTabCountLabel",
                "CreateRuleButton",
                "RuleSearchField",
                "RuleListContainer",
                "RuleEmptyLabel",
                windowData,
                getValidationErrorCount,
                copyConfigToLocal,
                deleteConfig,
                trackWorkingCopyChanges,
                createConfigRequested,
                pageRoot)
        {
            this.createSection = createSection;
            this.addPropertyField = addPropertyField;
            this.addTextField = addTextField;
            this.addRelativeTextField = addRelativeTextField;
            this.applyValidationVisual = applyValidationVisual;
            this.createFieldHint = createFieldHint;
            this.hasValidationErrorUnderPath =
                hasValidationErrorUnderPath;
            this.getSerializedString = getSerializedString;
        }

        public override EConfigPage PageType =>
            EConfigPage.Rule;

        public override string DisplayName => "检测规则配置";

        public override ConfigPageCreateOptions CreateOptions =>
            createOptions;

        internal void ConfigureCheckerPanel(
            CreateCheckerPanel panel,
            ConfigWindowDialogHost host,
            ConfigWindowData data,
            Func<DefaultAsset, string> defaultSaveFolderProvider)
        {
            createCheckerPanel = panel;
            dialogHost = host;
            configData = data;
            getDefaultSaveFolder = defaultSaveFolderProvider;
        }

        internal void ConfigureIssueInfoPanel(
            CreateIssueInfoPanel panel,
            ConfigWindowDialogHost host,
            ConfigWindowData data,
            Action<ScoreRuleConfig> onCreated)
        {
            createIssueInfoPanel = panel;
            dialogHost = host;
            configData = data;
            issueInfoCreated = onCreated;
        }

        /// <summary>
        /// 扫描所有检测脚本，并按规则 ID 建立当前页面使用的索引。
        /// </summary>
        public void ReloadCheckerTypes()
        {
            checkerTypesByRuleId.Clear();

            TypeCache.TypeCollection checkerTypes =
                TypeCache.GetTypesDerivedFrom<IScoreRuleChecker>();

            foreach (Type checkerType in checkerTypes)
            {
                if (checkerType == null ||
                    checkerType.IsAbstract ||
                    checkerType.IsInterface ||
                    checkerType.ContainsGenericParameters)
                {
                    continue;
                }

                IScoreRuleChecker checker;

                try
                {
                    checker =
                        Activator.CreateInstance(checkerType)
                            as IScoreRuleChecker;
                }
                catch
                {
                    continue;
                }

                if (checker == null ||
                    string.IsNullOrEmpty(checker.RuleId) ||
                    checkerTypesByRuleId.ContainsKey(checker.RuleId))
                {
                    continue;
                }

                checkerTypesByRuleId.Add(
                    checker.RuleId,
                    checkerType);
            }
        }

        public bool HasChecker(string ruleId)
        {
            return !string.IsNullOrEmpty(ruleId) &&
                   checkerTypesByRuleId.ContainsKey(ruleId);
        }

        protected override void BeforeFresh()
        {
            ReloadCheckerTypes();
        }

        protected override string GetDisplayName(
            ScoreRuleConfig config)
        {
            return string.IsNullOrEmpty(config.DisplayName)
                ? config.name
                : config.DisplayName;
        }

        protected override string GetSummary(
            ScoreRuleConfig config)
        {
            int issueCount = config.IssueInfos == null
                ? 0
                : config.IssueInfos.Length;
            string checkerState = HasChecker(config.RuleId)
                ? "已创建"
                : "未创建";

            return "规则 ID：" +
                   config.RuleId +
                   "　问题项：" +
                   issueCount +
                   "　检测脚本：" +
                   checkerState;
        }

        /// <summary>
        /// 将规则 ID、问题 ID、名称和提示加入页面搜索文本。
        /// </summary>
        protected override string GetSearchText(
            ScoreRuleConfig config)
        {
            List<string> values =
                new List<string>
                {
                    base.GetSearchText(config),
                    config.RuleId,
                    config.Description
                };

            if (config.IssueInfos != null)
            {
                for (int i = 0;
                     i < config.IssueInfos.Length;
                     i++)
                {
                    IssueInfo issueInfo = config.IssueInfos[i];

                    if (issueInfo == null)
                    {
                        continue;
                    }

                    values.Add(issueInfo.issueInfoId);
                    values.Add(issueInfo.displayName);
                    values.Add(issueInfo.issueMessage);
                }
            }

            return string.Join("\n", values);
        }

        protected override void BuildTypedFields(
            ScoreRuleConfig config,
            VisualElement container,
            bool editable)
        {
            SerializedObject serializedObject =
                new SerializedObject(config);

            VisualElement baseSection =
                createSection("规则基础配置", null);
            baseSection.AddToClassList(
                "html-content-section-first");

            VisualElement baseBody =
                baseSection.Q<VisualElement>("SectionBody");

            addPropertyField(
                baseBody,
                serializedObject,
                "ruleId",
                "规则 ID *",
                editable,
                "规则的唯一标识，需要与对应检测脚本的规则 ID 保持一致。" +
                ConfigIdNamingUtility.RuleHint,
                true);

            addPropertyField(
                baseBody,
                serializedObject,
                "displayName",
                "显示名称 *",
                editable,
                "编辑器中显示的规则名称。",
                true);

            addTextField(
                baseBody,
                serializedObject,
                "description",
                "规则说明",
                editable,
                true,
                "编辑器中显示的规则描述，可选。",
                false);

            addPropertyField(
                baseBody,
                serializedObject,
                "priority",
                "执行优先级",
                editable,
                "用于规则执行排序。",
                false);

            container.Add(baseSection);
            container.Add(
                BuildIssueInfosSection(
                    config,
                    serializedObject,
                    editable));
            container.Add(BuildCheckerSection(config));
        }

        private VisualElement BuildCheckerSection(
            ScoreRuleConfig ruleConfig)
        {
            VisualElement section =
                createSection("检测脚本", null);
            section.AddToClassList(
                "html-content-section-divider");

            VisualElement body =
                section.Q<VisualElement>("SectionBody");

            VisualElement stateContainer = new VisualElement();
            stateContainer.AddToClassList("checker-state");

            Type checkerType;

            if (checkerTypesByRuleId.TryGetValue(
                    ruleConfig.RuleId,
                    out checkerType))
            {
                Label stateLabel = new Label("检测脚本已关联");
                stateLabel.AddToClassList("checker-state-title");
                stateContainer.Add(stateLabel);

                Label classLabel = new Label(checkerType.FullName);
                classLabel.AddToClassList(
                    "checker-state-description");
                stateContainer.Add(classLabel);

                Button openButton =
                    new Button(
                        delegate { OpenCheckerScript(checkerType); });

                openButton.text = "打开检测脚本";
                openButton.AddToClassList("small-button");
                stateContainer.Add(openButton);
            }
            else if (configData.HasPendingCheckerForRule(
                         ruleConfig))
            {
                Label stateLabel =
                    new Label("检测脚本等待保存");

                stateLabel.AddToClassList("checker-state-title");
                stateContainer.Add(stateLabel);

                Label descriptionLabel =
                    new Label(
                        "点击“保存全部”后创建检测脚本文件。");

                descriptionLabel.AddToClassList(
                    "checker-state-description");
                stateContainer.Add(descriptionLabel);
            }
            else
            {
                stateContainer.AddToClassList(
                    "checker-state-missing");

                Label stateLabel =
                    new Label("未找到对应的检测脚本");

                stateLabel.AddToClassList(
                    "checker-state-warning-title");
                stateContainer.Add(stateLabel);

                Label descriptionLabel =
                    new Label(
                        "当前规则 ID 没有可用的 " +
                        "IScoreRuleChecker 实现，规则不会执行检测。");

                descriptionLabel.AddToClassList(
                    "checker-state-description");
                stateContainer.Add(descriptionLabel);

                Button createButton =
                    new Button(
                        delegate { ShowCreateCheckerPanel(ruleConfig); });

                createButton.text = "创建检测脚本";
                createButton.AddToClassList("small-button");
                createButton.AddToClassList("primary-button");
                stateContainer.Add(createButton);
            }

            body.Add(stateContainer);
            return section;
        }

        private void OpenCheckerScript(Type checkerType)
        {
            string[] scriptGuids =
                AssetDatabase.FindAssets("t:MonoScript");

            for (int i = 0; i < scriptGuids.Length; i++)
            {
                string scriptPath =
                    AssetDatabase.GUIDToAssetPath(scriptGuids[i]);

                MonoScript script =
                    AssetDatabase.LoadAssetAtPath<MonoScript>(
                        scriptPath);

                if (script == null ||
                    script.GetClass() != checkerType)
                {
                    continue;
                }

                Selection.activeObject = script;
                EditorGUIUtility.PingObject(script);
                AssetDatabase.OpenAsset(script);
                return;
            }

            EditorUtility.DisplayDialog(
                "打开检测脚本",
                "没有找到对应的 MonoScript 资源。",
                "确定");
        }

        private void ShowCreateCheckerPanel(
            ScoreRuleConfig ruleConfig)
        {
            UserSettingConfig projectSettings =
                RuleApplyConfigResolver.GetProjectConfig();

            createCheckerPanel.Open(
                dialogHost,
                configData,
                ruleConfig,
                getDefaultSaveFolder(
                    projectSettings == null
                        ? null
                        : projectSettings.CheckRuleScriptDefaultFolder),
                delegate
                {
                    ReloadCheckerTypes();
                    CheckerCreated?.Invoke(ruleConfig);
                });
        }

        public VisualElement BuildIssueInfoItem(
            ScoreRuleConfig owner,
            SerializedObject serializedObject,
            SerializedProperty itemProperty,
            string arrayPath,
            int index,
            bool editable,
            bool expanded)
        {
            VisualElement item = new VisualElement();
            item.AddToClassList("config-item");

            VisualElement header = new VisualElement();
            header.AddToClassList("config-item-header");

            Label arrow = new Label(expanded ? "▼" : "▶");
            arrow.AddToClassList("fold-arrow");
            header.Add(arrow);

            VisualElement titleContainer =
                new VisualElement();

            titleContainer.AddToClassList(
                "config-item-title-container");

            string displayName =
                getSerializedString(
                    itemProperty,
                    "displayName");

            string issueInfoId =
                getSerializedString(
                    itemProperty,
                    "issueInfoId");

            Label displayNameLabel =
                new Label(
                    string.IsNullOrEmpty(displayName)
                        ? "未命名问题"
                        : displayName);

            displayNameLabel.AddToClassList(
                "config-item-display-name");
            titleContainer.Add(displayNameLabel);

            Label idLabel =
                new Label(
                    string.IsNullOrEmpty(issueInfoId)
                        ? "未填写问题 ID"
                        : "问题 ID：" + issueInfoId);

            idLabel.AddToClassList("config-item-id");
            titleContainer.Add(idLabel);
            header.Add(titleContainer);

            SerializedProperty levelProperty =
                itemProperty.FindPropertyRelative(
                    "issueLevel");

            string levelText = string.Empty;

            if (levelProperty != null &&
                levelProperty.enumValueIndex >= 0 &&
                levelProperty.enumValueIndex <
                levelProperty.enumDisplayNames.Length)
            {
                levelText =
                    levelProperty.enumDisplayNames[
                        levelProperty.enumValueIndex];
            }

            Label levelLabel = new Label(levelText);
            levelLabel.AddToClassList(
                GetIssueLevelClass(levelText));
            header.Add(levelLabel);

            if (editable)
            {
                Button deleteButton = new Button(
                    delegate
                    {
                        DeleteIssueInfoRequested?.Invoke(
                            owner,
                            arrayPath,
                            index);
                    });

                deleteButton.text = "删除";
                deleteButton.AddToClassList("small-button");
                deleteButton.AddToClassList("danger-button");
                deleteButton.RegisterCallback<ClickEvent>(
                    delegate(ClickEvent evt) { evt.StopPropagation(); });
                header.Add(deleteButton);
            }

            item.Add(header);

            VisualElement itemBody = new VisualElement();
            itemBody.AddToClassList("config-item-body");
            UIElementUtility.SetVisible(itemBody, expanded);

            AddRelativePropertyField(
                itemBody,
                owner,
                serializedObject,
                itemProperty,
                "issueInfoId",
                "问题 ID *",
                editable,
                "问题的唯一 ID，例如 is_open。" +
                ConfigIdNamingUtility.RuleHint,
                true);

            AddRelativePropertyField(
                itemBody,
                owner,
                serializedObject,
                itemProperty,
                "displayName",
                "显示名称 *",
                editable,
                "问题的简要描述。",
                true);

            AddRelativePropertyField(
                itemBody,
                owner,
                serializedObject,
                itemProperty,
                "deductScore",
                "扣分",
                editable,
                "命中该问题时扣除的分数，允许为 0。",
                false);

            AddRelativePropertyField(
                itemBody,
                owner,
                serializedObject,
                itemProperty,
                "issueLevel",
                "问题级别",
                editable,
                "问题的严重等级；新增问题项时默认 Warning。",
                false);

            addRelativeTextField(
                itemBody,
                itemProperty,
                "issueMessage",
                "问题提示 *",
                editable,
                true,
                "问题说明，可以包含原因和修改建议。",
                true);

            item.Add(itemBody);

            header.RegisterCallback<ClickEvent>(
                delegate
                {
                    bool expand = !UIElementUtility.IsVisible(itemBody);

                    UIElementUtility.SetVisible(itemBody, expand);

                    arrow.text = expand ? "▼" : "▶";
                });

            return item;
        }

        private VisualElement BuildIssueInfosSection(
            ScoreRuleConfig config,
            SerializedObject serializedObject,
            bool editable)
        {
            SerializedProperty issueInfos =
                serializedObject.FindProperty("issueInfos");

            Button addButton = new Button();
            addButton.text = "+ 添加问题项";
            addButton.AddToClassList("small-button");
            addButton.SetEnabled(
                editable &&
                issueInfos != null &&
                issueInfos.isArray);

            VisualElement section =
                createSection("问题项", addButton);
            section.AddToClassList(
                "html-content-section-divider");

            VisualElement body =
                section.Q<VisualElement>("SectionBody");

            body.Add(
                createFieldHint(
                    "一个规则可以配置多个问题项；新增时会先填写必填内容。",
                    false));

            if (issueInfos == null ||
                !issueInfos.isArray)
            {
                Label missingLabel =
                    new Label(
                        "未找到问题项序列化字段 issueInfos。");

                missingLabel.AddToClassList(
                    "warning-message");
                body.Add(missingLabel);
                addButton.SetEnabled(false);
                return section;
            }

            string arrayPath = issueInfos.propertyPath;

            addButton.clicked +=
                delegate
                {
                    createIssueInfoPanel.Open(
                        dialogHost,
                        configData,
                        config,
                        arrayPath,
                        delegate { issueInfoCreated?.Invoke(config); });
                };

            if (issueInfos.arraySize == 0)
            {
                Label emptyLabel =
                    new Label("没有问题项");

                emptyLabel.AddToClassList(
                    "empty-inline-label");
                body.Add(emptyLabel);
                return section;
            }

            for (int i = 0;
                 i < issueInfos.arraySize;
                 i++)
            {
                SerializedProperty itemProperty =
                    issueInfos
                        .GetArrayElementAtIndex(i)
                        .Copy();

                body.Add(
                    BuildIssueInfoItem(
                        config,
                        serializedObject,
                        itemProperty,
                        arrayPath,
                        i,
                        editable,
                        i == 0 ||
                        hasValidationErrorUnderPath(
                            config,
                            itemProperty.propertyPath)));
            }

            return section;
        }

        private void AddRelativePropertyField(
            VisualElement container,
            ScriptableObject owner,
            SerializedObject serializedObject,
            SerializedProperty parent,
            string relativeName,
            string displayName,
            bool editable,
            string hint,
            bool required)
        {
            SerializedProperty property =
                parent.FindPropertyRelative(relativeName);

            if (property == null)
            {
                return;
            }

            VisualElement field;

            if (property.propertyType == SerializedPropertyType.String &&
                ConfigIdNamingUtility.IsIdPropertyName(property.name))
            {
                TextField idField = new TextField(displayName);
                ConfigIdNamingUtility.RestrictInput(idField);
                idField.BindProperty(property.Copy());
                field = idField;
            }
            else
            {
                PropertyField propertyField =
                    new PropertyField(
                        property.Copy(),
                        displayName);

                propertyField.Bind(serializedObject);
                field = propertyField;
            }

            field.SetEnabled(editable);
            field.AddToClassList("config-property-field");

            applyValidationVisual(
                field,
                owner,
                property.propertyPath);

            VisualElement fieldBlock = new VisualElement();
            fieldBlock.AddToClassList("config-field-block");
            fieldBlock.Add(field);

            if (!string.IsNullOrEmpty(hint))
            {
                fieldBlock.Add(
                    createFieldHint(hint, required));
            }

            container.Add(fieldBlock);
        }

        private string GetIssueLevelClass(string levelText)
        {
            if (string.Equals(
                    levelText,
                    "Blocker",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    levelText,
                    "Error",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "status-error";
            }

            if (string.Equals(
                    levelText,
                    "Warning",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "status-warning";
            }

            return "status-info";
        }
    }
}
