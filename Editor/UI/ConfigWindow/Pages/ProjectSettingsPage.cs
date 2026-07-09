using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 项目设置页的 UI。
    /// 该类只负责控件展示和用户意图转发，设置资产的工作副本修改由窗口统一处理。
    /// </summary>
    internal sealed class ProjectSettingsPage : IConfigPage
    {
        public const string ActiveRuleApplyConfigProperty =
            "activeRuleApplyConfig";

        public const string EnableSpineChecksProperty =
            "enableSpineChecks";

        public const string ProfileDefaultFolderProperty =
            "profileConfigDefaultFolder";

        public const string RuleApplyDefaultFolderProperty =
            "ruleApplyConfigDefaultFolder";

        public const string ScoreRuleDefaultFolderProperty =
            "scoreRuleConfigDefaultFolder";

        public const string AssetTypeDefaultFolderProperty =
            "assetTypeConfigDefaultFolder";

        public const string CheckRuleDefaultFolderProperty =
            "checkRuleScriptDefaultFolder";

        public const string ExportDefaultFolderPathProperty =
            "exportDefaultFolderPath";

        private readonly VisualElement unavailableView;
        private readonly Label unavailableLabel;
        private readonly VisualElement multipleView;
        private readonly VisualElement multiplePaths;
        private readonly VisualElement settingsForm;
        private readonly Label settingsAssetNameLabel;
        private readonly VisualElement activeRuleApplyHost;
        private readonly VisualElement spineChecksHost;
        private readonly VisualElement profileFolderHost;
        private readonly VisualElement ruleApplyFolderHost;
        private readonly VisualElement ruleFolderHost;
        private readonly VisualElement assetTypeFolderHost;
        private readonly VisualElement checkRuleFolderHost;
        private readonly VisualElement exportFolderHost;
        private readonly Button pingButton;
        private readonly VisualTreeAsset pathInputFieldVisualTreeAsset;

        private readonly Func<ScriptableObject, string, string>
            buildValidationElementName;

        private readonly ConfigWindowData configData;
        private readonly Func<string, bool> isValidAssetFolder;
        private readonly Func<string, string> toAbsoluteProjectPath;
        private readonly Func<string, string> toUnityPath;

        private readonly Dictionary<string, PathInputField> folderFields =
            new Dictionary<string, PathInputField>();

        private UserSettingConfig workingConfig;
        private bool isBound;

        public event Action DataChanged;

        internal ProjectSettingsPage(
            VisualElement windowRoot,
            VisualTreeAsset pathInputFieldVisualTreeAsset,
            Func<ScriptableObject, string, string>
                buildValidationElementName,
            ConfigWindowData configData,
            Func<string, bool> isValidAssetFolder,
            Func<string, string> toAbsoluteProjectPath,
            Func<string, string> toUnityPath,
            VisualElement pageRoot = null)
        {
            VisualElement contentRoot = pageRoot ?? windowRoot;
            Root = pageRoot ??
                   windowRoot.Q<VisualElement>("ProjectSettingsPage");
            ScrollView = contentRoot.Q<ScrollView>(
                "ProjectSettingsScrollView");
            TabButton = windowRoot.Q<Button>(
                "ProjectSettingsTabButton");
            unavailableView = contentRoot.Q<VisualElement>(
                "ProjectSettingsUnavailableView");
            unavailableLabel = contentRoot.Q<Label>(
                "ProjectSettingsUnavailableLabel");
            multipleView = contentRoot.Q<VisualElement>(
                "ProjectSettingsMultipleView");
            multiplePaths = contentRoot.Q<VisualElement>(
                "ProjectSettingsMultiplePaths");
            settingsForm = contentRoot.Q<VisualElement>(
                "ProjectSettingsForm");
            settingsAssetNameLabel = contentRoot.Q<Label>(
                "ProjectSettingsAssetName");
            activeRuleApplyHost = contentRoot.Q<VisualElement>(
                "ProjectSettingsActiveRuleApplyHost");
            spineChecksHost = contentRoot.Q<VisualElement>(
                "ProjectSettingsSpineChecksHost");
            profileFolderHost = contentRoot.Q<VisualElement>(
                "ProjectSettingsProfileFolderHost");
            ruleApplyFolderHost = contentRoot.Q<VisualElement>(
                "ProjectSettingsRuleApplyFolderHost");
            ruleFolderHost = contentRoot.Q<VisualElement>(
                "ProjectSettingsRuleFolderHost");
            assetTypeFolderHost = contentRoot.Q<VisualElement>(
                "ProjectSettingsAssetTypeFolderHost");
            checkRuleFolderHost = contentRoot.Q<VisualElement>(
                "ProjectSettingsCheckRuleFolderHost");
            exportFolderHost = contentRoot.Q<VisualElement>(
                "ProjectSettingsExportFolderHost");
            pingButton = contentRoot.Q<Button>(
                "ProjectSettingsPingButton");
            this.pathInputFieldVisualTreeAsset =
                pathInputFieldVisualTreeAsset;
            this.buildValidationElementName =
                buildValidationElementName;
            this.configData = configData;
            this.isValidAssetFolder = isValidAssetFolder;
            this.toAbsoluteProjectPath = toAbsoluteProjectPath;
            this.toUnityPath = toUnityPath;

        }

        public EConfigPage PageType =>
            EConfigPage.ProjectSettings;

        public Type ConfigType => typeof(UserSettingConfig);

        public string DisplayName => "项目设置";

        public VisualElement Root { get; }

        public ScrollView ScrollView { get; }

        public Button TabButton { get; }

        public ConfigPageCreateOptions CreateOptions => null;

        public UserSettingConfig WorkingConfig => workingConfig;

        public void Bind()
        {
            if (isBound)
            {
                return;
            }

            isBound = true;
            pingButton.clicked += PingSettingsAsset;
        }

        public void SetVisible(bool visible)
        {
            UIElementUtility.SetVisible(Root, visible);
            UIElementUtility.SetClass(TabButton, "selected-tab", visible);
        }

        public void ExpandConfig(string assetPath)
        {
        }

        public string GetConfigDisplayName(
            ScriptableObject config)
        {
            return config == null ? string.Empty : config.name;
        }

        public void BuildFields(
            ScriptableObject config,
            VisualElement container,
            bool editable)
        {
        }

        /// <summary>
        /// 重新读取项目设置及规则应用配置，并重建当前页面。
        /// </summary>
        public void Fresh()
        {
            List<ScriptableObject> configs =
                configData.LoadConfigsByType(
                    typeof(UserSettingConfig));

            List<UserSettingConfig> settings =
                new List<UserSettingConfig>();

            for (int i = 0; i < configs.Count; i++)
            {
                UserSettingConfig setting =
                    configs[i] as UserSettingConfig;

                if (setting == null ||
                    ConfigWindowData.IsPackageAsset(
                        configData.GetConfigAssetPath(setting)))
                {
                    continue;
                }

                settings.Add(setting);
            }

            if (settings.Count == 0)
            {
                workingConfig = null;
                ShowMissing();
                return;
            }

            if (settings.Count > 1)
            {
                workingConfig = null;
                ShowMultiple(settings);
                return;
            }

            workingConfig =
                configData.GetWorkingConfig(settings[0]);

            List<ScriptableObject> ruleApplyAssets =
                configData.LoadConfigsByType(
                    typeof(RuleApplyConfig));

            List<RuleApplyConfig> ruleApplyConfigs =
                new List<RuleApplyConfig>();

            for (int i = 0; i < ruleApplyAssets.Count; i++)
            {
                RuleApplyConfig config =
                    ruleApplyAssets[i] as RuleApplyConfig;

                if (config != null &&
                    !ConfigWindowData.IsPackageAsset(
                        configData.GetConfigAssetPath(config)))
                {
                    ruleApplyConfigs.Add(config);
                }
            }

            ShowSettings(
                workingConfig,
                ruleApplyConfigs,
                RuleApplyConfigResolver.GetDefaultConfig());
        }

        public void ShowMissing()
        {
            ShowUnavailable(
                "未找到项目设置资产。请通过“Assets/Create/Catfood Tools/Asset Score Tool/创建项目设置”创建唯一的 UserSettingConfig。");
        }

        public void ShowMultiple(
            IList<UserSettingConfig> settings)
        {
            folderFields.Clear();
            SetState(false, false, true);
            UIElementUtility.SetEnabled(pingButton, false);
            multiplePaths.Clear();

            for (int i = 0; i < settings.Count; i++)
            {
                Label pathLabel = new Label(
                    configData.GetConfigAssetPath(settings[i]));

                pathLabel.AddToClassList("field-hint");
                multiplePaths.Add(pathLabel);
            }
        }

        public void ShowSettings(
            UserSettingConfig workingConfig,
            IList<RuleApplyConfig> ruleApplyConfigs,
            RuleApplyConfig defaultRuleApplyConfig)
        {
            folderFields.Clear();
            SetState(true, false, false);
            UIElementUtility.SetEnabled(pingButton, true);
            UIElementUtility.SetText(
                settingsAssetNameLabel,
                workingConfig.name);
            activeRuleApplyHost.Clear();
            spineChecksHost.Clear();
            profileFolderHost.Clear();
            ruleApplyFolderHost.Clear();
            ruleFolderHost.Clear();
            assetTypeFolderHost.Clear();
            checkRuleFolderHost.Clear();
            exportFolderHost.Clear();

            SerializedObject serializedObject =
                new SerializedObject(workingConfig);

            BuildActiveRuleApplySelector(
                activeRuleApplyHost,
                workingConfig,
                serializedObject,
                ruleApplyConfigs,
                defaultRuleApplyConfig);

            BuildSpineChecksToggle(spineChecksHost, workingConfig);

            AddFolderField(
                profileFolderHost,
                workingConfig,
                serializedObject,
                ProfileDefaultFolderProperty,
                "检测配置档默认保存目录");

            AddFolderField(
                ruleApplyFolderHost,
                workingConfig,
                serializedObject,
                RuleApplyDefaultFolderProperty,
                "规则应用配置默认保存目录");

            AddFolderField(
                ruleFolderHost,
                workingConfig,
                serializedObject,
                ScoreRuleDefaultFolderProperty,
                "检测规则配置默认保存目录");

            AddFolderField(
                assetTypeFolderHost,
                workingConfig,
                serializedObject,
                AssetTypeDefaultFolderProperty,
                "资源类型配置默认保存目录");

            AddFolderField(
                checkRuleFolderHost,
                workingConfig,
                serializedObject,
                CheckRuleDefaultFolderProperty,
                "CheckRule 脚本默认保存目录");

            AddExternalFolderPathField(
                exportFolderHost,
                workingConfig,
                serializedObject,
                ExportDefaultFolderPathProperty,
                "报告导出默认保存目录");

        }

        public void SetFolderPath(
            string propertyName,
            string folderPath)
        {
            PathInputField field;

            if (folderFields.TryGetValue(
                    propertyName,
                    out field))
            {
                field.Value = folderPath;
            }
        }

        private void ShowUnavailable(string message)
        {
            folderFields.Clear();
            SetState(false, true, false);
            pingButton.SetEnabled(false);
            UIElementUtility.SetText(unavailableLabel, message);
        }

        /// <summary>
        /// 切换项目设置页面的可用、缺失和重复资产三种固定视图状态。
        /// </summary>
        private void SetState(
            bool showSettings,
            bool showUnavailable,
            bool showMultiple)
        {
            UIElementUtility.SetVisible(settingsForm, showSettings);
            UIElementUtility.SetVisible(unavailableView, showUnavailable);
            UIElementUtility.SetVisible(multipleView, showMultiple);
        }

        private void BuildActiveRuleApplySelector(
            VisualElement container,
            UserSettingConfig workingConfig,
            SerializedObject serializedObject,
            IList<RuleApplyConfig> ruleApplyConfigs,
            RuleApplyConfig defaultRuleApplyConfig)
        {
            SerializedProperty property =
                serializedObject.FindProperty(
                    ActiveRuleApplyConfigProperty);

            RuleApplyConfig activeConfig = property == null
                ? null
                : property.objectReferenceValue as RuleApplyConfig;

            List<RuleApplyConfig> choices =
                new List<RuleApplyConfig>();

            choices.Add(defaultRuleApplyConfig);

            for (int i = 0; i < ruleApplyConfigs.Count; i++)
            {
                if (ruleApplyConfigs[i] != defaultRuleApplyConfig)
                {
                    choices.Add(ruleApplyConfigs[i]);
                }
            }

            RuleApplyConfig selectedConfig = activeConfig == null
                ? defaultRuleApplyConfig
                : activeConfig;

            int selectedIndex = choices.IndexOf(selectedConfig);
            selectedIndex = selectedIndex < 0 ? 0 : selectedIndex;

            PopupField<RuleApplyConfig> dropdown =
                new PopupField<RuleApplyConfig>(
                    "生效配置",
                    choices,
                    selectedIndex,
                    GetSelectedRuleApplyDisplayName,
                    GetRuleApplyOptionDisplayName);

            dropdown.name = buildValidationElementName(
                workingConfig,
                ActiveRuleApplyConfigProperty);
            dropdown.AddToClassList("config-property-field");
            dropdown.RegisterValueChangedCallback(evt => SetActiveRuleApplyConfig(
                evt.newValue == defaultRuleApplyConfig
                    ? null
                    : evt.newValue));

            container.Add(dropdown);
            Label hint = new Label(
                "引用为空或失效时，回退到 RuleApplyConfig_Default。");

            hint.AddToClassList("field-hint");
            container.Add(hint);
        }

        private string GetSelectedRuleApplyDisplayName(
            RuleApplyConfig config)
        {
            return config == null
                ? "未找到默认规则应用配置"
                : string.IsNullOrEmpty(config.DisplayName)
                    ? string.IsNullOrEmpty(config.name)
                        ? "未命名配置"
                        : config.name
                    : config.DisplayName;
        }

        private string GetRuleApplyOptionDisplayName(
            RuleApplyConfig config)
        {
            return GetSelectedRuleApplyDisplayName(config);
        }

        private void BuildSpineChecksToggle(
            VisualElement container,
            UserSettingConfig config)
        {
            bool isSpineInstalled =
                AssetScoreToolSpineSettings.IsSpineInstalled;
            VisualElement control = new VisualElement();
            control.AddToClassList("spine-checks-control");

            VisualElement copy = new VisualElement();
            copy.AddToClassList("spine-checks-copy");
            Label title = new Label("Spine 检测");
            title.AddToClassList("spine-checks-title");
            copy.Add(title);

            Label description = new Label(
                isSpineInstalled
                    ? "启用后会编译并加载 Spine 专用检查器。"
                    : "导入 spine-unity 与 spine-csharp 后可启用。"
            );

            description.AddToClassList("spine-checks-description");
            copy.Add(description);
            control.Add(copy);

            VisualElement switchHost = new VisualElement();
            switchHost.AddToClassList("spine-checks-switch-host");
            Label state = new Label();
            state.AddToClassList("spine-checks-state");
            switchHost.Add(state);

            Toggle toggle = new Toggle();
            toggle.name = buildValidationElementName(
                config,
                EnableSpineChecksProperty);
            toggle.tooltip = "启用 Spine 检测";
            toggle.AddToClassList("spine-checks-toggle");
            toggle.SetValueWithoutNotify(config.EnableSpineChecks);
            toggle.RegisterValueChangedCallback(
                evt => SetSpineChecksEnabled(toggle, state, evt.newValue));
            UpdateSpineChecksState(state, config.EnableSpineChecks);
            switchHost.Add(toggle);
            control.Add(switchHost);
            container.Add(control);
        }

        private void SetSpineChecksEnabled(
            Toggle toggle,
            Label state,
            bool enabled)
        {
            if (workingConfig == null)
            {
                return;
            }

            if (enabled && !AssetScoreToolSpineSettings.IsSpineInstalled)
            {
                toggle.SetValueWithoutNotify(false);
                UpdateSpineChecksState(state, false);
                EditorUtility.DisplayDialog(
                    "Spine 检测",
                    "未检测到 spine-unity 与 spine-csharp。请先导入 Spine，再启用 Spine 检测。",
                    "确定");
                return;
            }

            configData.SetBoolean(
                workingConfig,
                EnableSpineChecksProperty,
                enabled);
            UpdateSpineChecksState(state, enabled);
            DataChanged?.Invoke();
        }

        private void UpdateSpineChecksState(Label state, bool enabled)
        {
            UIElementUtility.SetText(state, enabled ? "已启用" : "已关闭");
            UIElementUtility.SetClass(
                state,
                "spine-checks-state-enabled",
                enabled);
        }

        private void AddFolderField(
            VisualElement container,
            UserSettingConfig workingConfig,
            SerializedObject serializedObject,
            string propertyName,
            string label)
        {
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            DefaultAsset folder = property == null
                ? null
                : property.objectReferenceValue as DefaultAsset;

            PathInputField field =
                new PathInputField(
                    pathInputFieldVisualTreeAsset);

            field.name = buildValidationElementName(
                workingConfig,
                propertyName);
            field.focusable = true;
            field.LabelText = label;
            field.BrowseButtonText = "选择文件夹";
            field.HintText = string.Empty;
            field.Value = folder == null
                ? string.Empty
                : AssetDatabase.GetAssetPath(folder);

            field.ValueChanged +=
                value => SetFolderReference(
                    propertyName,
                    value);

            field.BrowseRequested +=
                delegate { BrowseFolder(propertyName); };

            folderFields.Add(propertyName, field);
            container.Add(field);
        }

        /// <summary>
        /// 添加可保存任意本机绝对路径的报告导出目录字段。
        /// </summary>
        private void AddExternalFolderPathField(
            VisualElement container,
            UserSettingConfig workingConfig,
            SerializedObject serializedObject,
            string propertyName,
            string label)
        {
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            PathInputField field = new PathInputField(
                pathInputFieldVisualTreeAsset);

            field.name = buildValidationElementName(
                workingConfig,
                propertyName);
            field.focusable = true;
            field.LabelText = label;
            field.BrowseButtonText = "选择文件夹";
            field.HintText = "可选择或输入项目外的本机绝对路径。";
            string configuredFolderPath = property == null
                ? string.Empty
                : property.stringValue;
            field.Value = string.IsNullOrEmpty(configuredFolderPath)
                ? Path.Combine(
                    Directory.GetParent(Application.dataPath).FullName,
                    "docs")
                : configuredFolderPath;
            field.ValueChanged +=
                value => SetExternalFolderPath(propertyName, value);
            field.BrowseRequested +=
                delegate { BrowseExternalFolder(propertyName); };

            folderFields.Add(propertyName, field);
            container.Add(field);
        }

        /// <summary>
        /// 使用系统文件夹选择器更新报告导出目录，不限制为 Unity Assets 路径。
        /// </summary>
        private void BrowseExternalFolder(string propertyName)
        {
            string currentPath = GetExternalFolderPath(propertyName);
            string selectedFolder = EditorUtility.OpenFolderPanel(
                "选择报告导出默认保存目录",
                currentPath,
                string.Empty);

            if (string.IsNullOrEmpty(selectedFolder))
            {
                return;
            }

            SetFolderPath(propertyName, selectedFolder);
            SetExternalFolderPath(propertyName, selectedFolder);
        }

        /// <summary>
        /// 将报告导出目录作为字符串写入项目设置的工作副本。
        /// </summary>
        private void SetExternalFolderPath(
            string propertyName,
            string folderPath)
        {
            if (workingConfig == null)
            {
                return;
            }

            configData.SetStringProperty(
                workingConfig,
                propertyName,
                folderPath == null ? string.Empty : folderPath.Trim());
            DataChanged?.Invoke();
        }

        /// <summary>
        /// 获取存在的导出目录，供系统文件夹选择器作为初始位置使用。
        /// </summary>
        private string GetExternalFolderPath(string propertyName)
        {
            if (workingConfig == null)
            {
                return string.Empty;
            }

            SerializedObject serializedObject =
                new SerializedObject(workingConfig);
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);
            string folderPath = property == null
                ? string.Empty
                : property.stringValue;

            return Directory.Exists(folderPath)
                ? folderPath
                : string.Empty;
        }

        private void SetActiveRuleApplyConfig(
            RuleApplyConfig selectedConfig)
        {
            if (workingConfig == null)
            {
                return;
            }

            configData.SetObjectReference(
                workingConfig,
                ActiveRuleApplyConfigProperty,
                selectedConfig);

            DataChanged?.Invoke();
            Fresh();
        }

        private void BrowseFolder(string propertyName)
        {
            string selectedFolder =
                EditorUtility.OpenFolderPanel(
                    "选择默认保存文件夹",
                    toAbsoluteProjectPath(
                        GetFolderPath(propertyName)),
                    string.Empty);

            if (string.IsNullOrEmpty(selectedFolder))
            {
                return;
            }

            string unityPath = toUnityPath(selectedFolder);

            if (!isValidAssetFolder(unityPath))
            {
                EditorUtility.DisplayDialog(
                    "项目设置",
                    "请选择当前 Unity 项目 Assets 目录中的有效文件夹。",
                    "确定");
                return;
            }

            SetFolderPath(propertyName, unityPath);
            SetFolderReference(propertyName, unityPath);
        }

        private void SetFolderReference(
            string propertyName,
            string folderPath)
        {
            if (workingConfig == null)
            {
                return;
            }

            string normalizedPath =
                string.IsNullOrEmpty(folderPath)
                    ? string.Empty
                    : folderPath.Replace("\\", "/");

            DefaultAsset folder =
                isValidAssetFolder(normalizedPath)
                    ? AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                        normalizedPath)
                    : null;

            configData.SetObjectReference(
                workingConfig,
                propertyName,
                folder);

            DataChanged?.Invoke();
        }

        private string GetFolderPath(string propertyName)
        {
            if (workingConfig == null)
            {
                return "Assets";
            }

            SerializedObject serializedObject =
                new SerializedObject(workingConfig);

            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            DefaultAsset folder = property == null
                ? null
                : property.objectReferenceValue as DefaultAsset;

            string folderPath = folder == null
                ? string.Empty
                : AssetDatabase.GetAssetPath(folder);

            return isValidAssetFolder(folderPath)
                ? folderPath
                : "Assets";
        }

        private void PingSettingsAsset()
        {
            if (workingConfig == null)
            {
                return;
            }

            ScriptableObject source =
                configData.GetSourceConfig(workingConfig);

            Selection.activeObject = source;
            EditorGUIUtility.PingObject(source);
        }
    }
}
