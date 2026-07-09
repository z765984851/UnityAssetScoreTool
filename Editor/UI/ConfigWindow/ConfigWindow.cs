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
    /// 资源评分工具配置中心。
    /// 窗口负责生命周期、页面编排、公共 UI 和统一保存流程。
    /// </summary>
    public class ConfigWindow : EditorWindow
    {
        private const string DefaultConfigRootFolder =
            "Assets/Tools/asset_score_tool/Configs";

        internal ConfigWindowData configData;
        private ConfigWindowValidation configValidation;
        private ConfigWindowValidationPresenter validationPresenter;
        private ConfigPageUIBuilder pageUIBuilder;
        private ProfileConfigPage profileConfigPage;
        private AssetTypeConfigPage assetTypeConfigPage;
        private RuleConfigPage ruleConfigPage;
        private RuleApplyConfigPage ruleApplyConfigPage;
        private ProjectSettingsPage projectSettingsView;
        private VisualElement profilePageRoot;
        private VisualElement projectSettingsPageRoot;
        private VisualElement assetTypePageRoot;
        private VisualElement rulePageRoot;
        private VisualElement ruleApplyPageRoot;
        private ConfigWindowPageHost pageHost;
        private ConfigWindowDialogHost dialogHost;

        [Header("窗口模板")] [SerializeField] private VisualTreeAsset visualTreeAsset = default;

        [SerializeField] private VisualTreeAsset profilePageVisualTreeAsset = default;
        [SerializeField] private VisualTreeAsset projectSettingsPageVisualTreeAsset = default;
        [SerializeField] private VisualTreeAsset assetTypePageVisualTreeAsset = default;
        [SerializeField] private VisualTreeAsset rulePageVisualTreeAsset = default;
        [SerializeField] private VisualTreeAsset ruleApplyPageVisualTreeAsset = default;

        [Header("通用组件模板")] [SerializeField] private VisualTreeAsset pathInputFieldVisualTreeAsset = default;

        [SerializeField] internal VisualTreeAsset objectReferenceSelectorVisualTreeAsset = default;

        [SerializeField] internal VisualTreeAsset foldoutCardViewVisualTreeAsset = default;

        [Header("弹窗模板")] [SerializeField] private VisualTreeAsset createConfigDialogVisualTreeAsset = default;

        [SerializeField] private VisualTreeAsset createCheckerDialogVisualTreeAsset = default;

        [SerializeField] private VisualTreeAsset createRuleApplyItemDialogVisualTreeAsset = default;

        [SerializeField] private VisualTreeAsset createIssueInfoDialogVisualTreeAsset = default;

        [SerializeField] private VisualTreeAsset createWhiteListDialogVisualTreeAsset = default;

        [SerializeField] private VisualTreeAsset editRuleConfigDialogVisualTreeAsset = default;

        [SerializeField] private VisualTreeAsset validationMessageViewVisualTreeAsset = default;

        [SerializeField] private VisualTreeAsset selectionDialogVisualTreeAsset = default;

        /// <summary>
        /// 提供配置页面可复用的元素构建能力，避免页面直接依赖窗口内部实现。
        /// </summary>
        internal ConfigPageUIBuilder PageUIBuilder => pageUIBuilder;

        [MenuItem("Catfood Tools/Asset Score Tool/配置中心")]
        public static void ShowWindow()
        {
            ConfigWindow window = GetWindow<ConfigWindow>();
            window.titleContent = new GUIContent("资产评分配置中心");
            window.minSize = new Vector2(980f, 620f);
        }

        public void CreateGUI()
        {
            RuleApplyConfigResolver.EnsureProjectConfig();

            configData?.Dispose();
            configData = new ConfigWindowData();
            configValidation =
                new ConfigWindowValidation(
                    GetPreviewActiveRuleApplyConfig,
                    configData.GetConfigAssetPath,
                    configData.IsConfigAvailable,
                    configData.GetWorkingConfig);

            VisualElement root = rootVisualElement;
            root.Clear();
            root.style.flexGrow = new StyleFloat(1);

            if (profilePageVisualTreeAsset == null)
            {
                profilePageVisualTreeAsset =
                    AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                        "Assets/Tools/asset_score_tool/Editor/UI/ConfigWindow/Pages/ProfileConfigPage.uxml");
            }

            if (projectSettingsPageVisualTreeAsset == null)
            {
                projectSettingsPageVisualTreeAsset =
                    AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                        "Assets/Tools/asset_score_tool/Editor/UI/ConfigWindow/Pages/ProjectSettingsPage.uxml");
            }

            assetTypePageVisualTreeAsset ??= AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Tools/asset_score_tool/Editor/UI/ConfigWindow/Pages/AssetTypeConfigPage.uxml");
            rulePageVisualTreeAsset ??= AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Tools/asset_score_tool/Editor/UI/ConfigWindow/Pages/RuleConfigPage.uxml");
            ruleApplyPageVisualTreeAsset ??= AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Tools/asset_score_tool/Editor/UI/ConfigWindow/Pages/RuleApplyConfigPage.uxml");

            // 配合 hasUnsavedChanges 使用。
            // 关闭窗口时 Unity 会提供“保存 / 不保存 / 取消”选择。
            saveChangesMessage =
                "配置中心存在未保存的修改，是否保存后关闭？";

            // 与 ScanWindow 保持一致：
            // UXML 由 Inspector 中配置的 VisualTreeAsset 实例化并填满窗口。
            VisualElement uxml = visualTreeAsset.Instantiate();
            uxml.style.flexGrow = new StyleFloat(1);
            root.Add(uxml);

            pageHost =
                new ConfigWindowPageHost(rootVisualElement);
            profilePageRoot = pageHost.InsertPage(
                profilePageVisualTreeAsset,
                0);
            projectSettingsPageRoot = pageHost.AddPage(
                projectSettingsPageVisualTreeAsset);
            ruleApplyPageRoot = pageHost.InsertPage(
                ruleApplyPageVisualTreeAsset,
                0);
            rulePageRoot = pageHost.InsertPage(
                rulePageVisualTreeAsset,
                0);
            assetTypePageRoot = pageHost.InsertPage(
                assetTypePageVisualTreeAsset,
                0);

            validationPresenter =
                new ConfigWindowValidationPresenter(
                    rootVisualElement,
                    validationMessageViewVisualTreeAsset,
                    GetDisplayName,
                    configData.GetSourceConfig);
            validationPresenter.LocateRequested +=
                FocusValidationIssue;

            pageUIBuilder = new ConfigPageUIBuilder(
                validationPresenter.ApplyFieldVisual,
                GetObjectDisplayName,
                configData.GetSourceConfig);

            BindUI();
            FreshUI();
        }

        private void BindUI()
        {
            BindToolbarUI();

            profileConfigPage = new ProfileConfigPage(
                rootVisualElement,
                configData,
                validationPresenter.GetErrorCount,
                CopyConfigToLocal,
                DeleteConfig,
                TrackWorkingCopyChanges,
                ShowCreateConfigDialog,
                title => pageUIBuilder.CreateSection(title),
                pageUIBuilder.AddPropertyField,
                profilePageRoot);

            assetTypeConfigPage = new AssetTypeConfigPage(
                rootVisualElement,
                configData,
                validationPresenter.GetErrorCount,
                CopyConfigToLocal,
                DeleteConfig,
                TrackWorkingCopyChanges,
                ShowCreateConfigDialog,
                title => pageUIBuilder.CreateSection(title),
                pageUIBuilder.AddPropertyField,
                assetTypePageRoot);

            ruleConfigPage = new RuleConfigPage(
                rootVisualElement,
                configData,
                validationPresenter.GetErrorCount,
                CopyConfigToLocal,
                DeleteConfig,
                TrackWorkingCopyChanges,
                ShowCreateConfigDialog,
                (title, actionButton) =>
                    pageUIBuilder.CreateContentSection(title, actionButton),
                pageUIBuilder.AddPropertyField,
                pageUIBuilder.AddTextField,
                pageUIBuilder.AddRelativeTextField,
                validationPresenter.ApplyFieldVisual,
                (hint, required) =>
                    pageUIBuilder.CreateFieldHint(hint, required),
                validationPresenter.HasErrorUnderPath,
                GetSerializedString,
                rulePageRoot);

            ruleConfigPage.DeleteIssueInfoRequested +=
                delegate(
                    ScoreRuleConfig owner,
                    string arrayPath,
                    int index)
                {
                    SerializedObject serializedObject =
                        new SerializedObject(owner);
                    SerializedProperty arrayProperty =
                        serializedObject.FindProperty(arrayPath);
                    string issueInfoId = string.Empty;

                    if (arrayProperty != null &&
                        arrayProperty.isArray &&
                        index >= 0 &&
                        index < arrayProperty.arraySize)
                    {
                        SerializedProperty item =
                            arrayProperty.GetArrayElementAtIndex(index);
                        SerializedProperty idProperty =
                            item.FindPropertyRelative("issueInfoId");

                        issueInfoId = idProperty == null
                            ? string.Empty
                            : idProperty.stringValue;
                    }

                    DeleteArrayElement(
                        owner,
                        arrayPath,
                        index,
                        "删除问题项",
                        delegate
                        {
                            configData.CleanupReferencesToIssue(
                                owner,
                                issueInfoId);
                            UpdateDirtyState();
                            RefreshRuleConfigViews(owner);
                        });
                };

            ruleApplyConfigPage =
                new RuleApplyConfigPage(
                    this,
                    rootVisualElement,
                    configData,
                    validationPresenter.GetErrorCount,
                    CopyConfigToLocal,
                    DeleteConfig,
                    TrackWorkingCopyChanges,
                    ShowCreateConfigDialog,
                    validationPresenter.ApplyFieldVisual,
                    validationPresenter.HasErrorUnderPath,
                    ruleApplyPageRoot);

            projectSettingsView = new ProjectSettingsPage(
                rootVisualElement,
                pathInputFieldVisualTreeAsset,
                validationPresenter.BuildElementName,
                configData,
                IsValidAssetFolder,
                ToAbsoluteProjectPath,
                delegate(string selectedPath)
                {
                    string unityPath;

                    return TryConvertToUnityPath(
                        selectedPath,
                        out unityPath)
                        ? unityPath
                        : string.Empty;
                },
                projectSettingsPageRoot);

            projectSettingsView.DataChanged +=
                delegate
                {
                    NotifyWorkingCopyChanged(
                        projectSettingsView.WorkingConfig);

                    // 当前生效配置由项目设置工作副本决定。
                    // 设置页修改后立即重建 RuleApply 状态，避免必须手动刷新页面。
                    ruleApplyConfigPage.Fresh();
                };

            BindTabsUI();

            // 所有弹窗均由独立 UXML 模板实例化到统一挂载点。
            BindDialogTemplateUI();
            BindCreateConfigDialogUI();
            BindCheckerDialogUI();
            BindItemCreateDialogUI();
            BindEditRuleConfigDialogUI();

            ruleApplyConfigPage.ConfigureCreatePanels(
                dialogHost,
                createRuleApplyItemPanel,
                createWhiteListPanel);

            ruleConfigPage.ConfigureIssueInfoPanel(
                createIssueInfoPanel,
                dialogHost,
                configData,
                delegate(ScoreRuleConfig owner)
                {
                    NotifyWorkingCopyChanged(owner);
                    RefreshRuleConfigViews(owner);
                });

            ruleConfigPage.CheckerCreated +=
                delegate(ScoreRuleConfig owner)
                {
                    UpdateDirtyState();
                    RefreshRuleConfigViews(owner);
                };

            // 选择弹窗最后实例化，确保从其他弹窗中打开时位于最上层。
            BindSelectionDialogUI();
        }

        private void FreshUI()
        {
            configData.Initialize();
            UpdateDirtyState();
            RefreshConfigurationViews();
            SwitchPage(EConfigPage.RuleApply);
        }

        private void OnDisable()
        {
            configData?.Dispose();
            configData = null;
        }

        private CreateCheckerPanel createCheckerPanel;

        /// <summary>
        /// 创建检测脚本 Panel，并交给检测规则页管理。
        /// </summary>
        private void BindCheckerDialogUI()
        {
            createCheckerPanel =
                new CreateCheckerPanel(
                    createCheckerDialogVisualTreeAsset,
                    pathInputFieldVisualTreeAsset,
                    IsValidAssetFolder,
                    ToAbsoluteProjectPath,
                    delegate(string selectedPath)
                    {
                        string unityPath;

                        return TryConvertToUnityPath(
                            selectedPath,
                            out unityPath)
                            ? unityPath
                            : string.Empty;
                    });

            ruleConfigPage.ConfigureCheckerPanel(
                createCheckerPanel,
                dialogHost,
                configData,
                GetDefaultSaveFolder);
        }

        internal string GetDisplayName(
            ScriptableObject config)
        {
            if (config == null)
            {
                return string.Empty;
            }

            IConfigPage page =
                GetPageByConfigType(config.GetType());

            if (page != null)
            {
                return page.GetConfigDisplayName(config);
            }

            SerializedObject serializedObject =
                new SerializedObject(config);

            SerializedProperty displayNameProperty =
                serializedObject.FindProperty("displayName");

            if (displayNameProperty != null &&
                !string.IsNullOrEmpty(
                    displayNameProperty.stringValue))
            {
                return displayNameProperty.stringValue;
            }

            return config.name;
        }

        /// <summary>
        /// 根据项目设置工作副本解析配置窗口预览中的当前生效 RuleApplyConfig。
        /// 未指定额外配置时沿用默认配置，并将本地配置映射到对应工作副本。
        /// </summary>
        internal RuleApplyConfig GetPreviewActiveRuleApplyConfig()
        {
            if (configData == null)
            {
                return null;
            }

            UserSettingConfig sourceSettings =
                RuleApplyConfigResolver.GetProjectConfig();

            UserSettingConfig workingSettings =
                sourceSettings == null
                    ? null
                    : configData.GetWorkingConfig(
                        sourceSettings);

            RuleApplyConfig sourceConfig =
                workingSettings == null
                    ? null
                    : workingSettings.ActiveRuleApplyConfig;

            if (sourceConfig == null)
            {
                sourceConfig =
                    RuleApplyConfigResolver.GetDefaultConfig();
            }

            return sourceConfig == null
                ? null
                : configData.GetWorkingConfig(sourceConfig)
                    as RuleApplyConfig;
        }

        private IConfigPage GetPageByConfigType(
            Type configType)
        {
            return pageHost.GetPageByConfigType(configType);
        }

        private CreateConfigPanel createConfigPanel;

        private void BindCreateConfigDialogUI()
        {
            createConfigPanel =
                new CreateConfigPanel(
                    createConfigDialogVisualTreeAsset,
                    pathInputFieldVisualTreeAsset,
                    IsValidAssetFolder,
                    ToAbsoluteProjectPath,
                    delegate(string selectedPath)
                    {
                        string unityPath;

                        return TryConvertToUnityPath(
                            selectedPath,
                            out unityPath)
                            ? unityPath
                            : string.Empty;
                    });
        }

        private void ShowCreateConfigDialog(
            IConfigPage page)
        {
            ConfigPageCreateOptions options =
                page.CreateOptions;

            if (options == null)
            {
                return;
            }

            UserSettingConfig projectSettings =
                RuleApplyConfigResolver.GetProjectConfig();

            DefaultAsset defaultFolder =
                options.DefaultFolderProvider == null
                    ? null
                    : options.DefaultFolderProvider(
                        projectSettings);

            createConfigPanel.Open(
                dialogHost,
                options.Title,
                true,
                options.DefaultAssetName,
                GetDefaultSaveFolder(defaultFolder),
                options.IdLabel,
                options.IdHint,
                options.IdPlaceholder,
                options.DisplayNameHint,
                delegate(
                    string assetName,
                    string configId,
                    string displayName,
                    string saveFolder)
                {
                    ScriptableObject config =
                        configData.CreatePendingConfig(
                            page.ConfigType,
                            options.IdPropertyName,
                            assetName,
                            configId,
                            displayName,
                            saveFolder);

                    page.ExpandConfig(
                        configData.GetConfigAssetPath(config));
                    UpdateDirtyState();
                    RefreshConfigurationViews();
                });
        }

        /// <summary>
        /// 绑定主 UXML 中的统一模态面板挂载点。
        /// </summary>
        private void BindDialogTemplateUI()
        {
            dialogHost =
                new ConfigWindowDialogHost(rootVisualElement);
        }
        /// <summary>
        /// 监听序列化字段变化，并把数据层的脏状态同步到窗口工具栏。
        /// </summary>
        internal void TrackWorkingCopyChanges(
            VisualElement container,
            ScriptableObject workingCopy,
            Action onChanged = null)
        {
            if (!configData.IsWorkingCopy(workingCopy))
            {
                return;
            }

            SerializedObject tracker =
                new SerializedObject(workingCopy);

            container.TrackSerializedObjectValue(
                tracker,
                delegate
                {
                    UpdateDirtyState();

                    if (onChanged != null)
                    {
                        onChanged();
                    }

                    // 部分字段变化回调会继续更新工作副本；
                    // 在这些回调完成后重新校验，确保输入过程中的诊断使用最新值。
                    UpdateSaveButtonState();
                });
        }

        internal void NotifyWorkingCopyChanged(
            ScriptableObject workingCopy)
        {
            if (configData.IsWorkingCopy(workingCopy))
            {
                UpdateDirtyState();
            }
        }

        private void UpdateDirtyState()
        {
            int dirtyCount =
                configData == null
                    ? 0
                    : configData.DirtyCount;

            bool hasChanges = dirtyCount > 0;

            toolbarView.SetDirtyState(dirtyCount, hasChanges);
            hasUnsavedChanges = hasChanges;

            UpdateSaveButtonState();
        }

        // --------------------------------------------------------------
        // 规则应用项弹窗
        // --------------------------------------------------------------

        private CreateRuleApplyItemPanel createRuleApplyItemPanel;

        // --------------------------------------------------------------
        // 问题项弹窗
        // --------------------------------------------------------------

        private CreateIssueInfoPanel createIssueInfoPanel;

        // --------------------------------------------------------------
        // 白名单弹窗
        // --------------------------------------------------------------

        private CreateWhiteListPanel createWhiteListPanel;

        private void BindItemCreateDialogUI()
        {
            BindCreateRuleApplyItemDialogUI();
            BindCreateIssueInfoDialogUI();
            BindCreateWhiteListDialogUI();
        }

        private EditRuleConfigPanel editRuleConfigPanel;

        /// <summary>
        /// 创建规则编辑弹窗，并在关闭后刷新所有引用该规则的配置卡片。
        /// </summary>
        private void BindEditRuleConfigDialogUI()
        {
            VisualTreeAsset dialogTemplate =
                editRuleConfigDialogVisualTreeAsset;

            if (dialogTemplate == null)
            {
                // 新增 UXML 在 Unity 首次导入并生成 meta 前没有默认引用，
                // 因此按当前窗口脚本位置解析同目录下的弹窗模板。
                string scriptPath =
                    AssetDatabase.GetAssetPath(
                        MonoScript.FromScriptableObject(this));

                string windowFolder =
                    Path.GetDirectoryName(scriptPath)
                        ?.Replace("\\", "/");

                dialogTemplate =
                    AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                        windowFolder +
                        "/Dialogs/EditRuleConfigPanel.uxml");
            }

            if (dialogTemplate == null)
            {
                return;
            }

            editRuleConfigPanel =
                new EditRuleConfigPanel(
                    dialogTemplate);

            editRuleConfigPanel.Closed +=
                RefreshConfigurationViews;
        }

        /// <summary>
        /// 在独立弹窗中打开规则配置；本地工作副本可编辑，包内配置只读预览。
        /// </summary>
        internal void ShowRuleConfigEditor(
            ScoreRuleConfig ruleConfig)
        {
            if (ruleConfig == null)
            {
                return;
            }

            if (editRuleConfigPanel == null)
            {
                EditorUtility.DisplayDialog(
                    "编辑检测规则配置",
                    "尚未配置规则编辑弹窗模板。",
                    "确定");
                return;
            }

            ScoreRuleConfig sourceRule =
                configData.GetSourceConfig(ruleConfig)
                    as ScoreRuleConfig;

            if (sourceRule == null)
            {
                return;
            }

            ScoreRuleConfig workingRule =
                configData.GetWorkingConfig(sourceRule);

            string assetPath =
                configData.GetConfigAssetPath(sourceRule);

            bool editable =
                !ConfigWindowData.IsPackageAsset(assetPath);

            ruleConfigPage.ReloadCheckerTypes();

            editRuleConfigPanel.Open(
                dialogHost,
                workingRule,
                GetDisplayName(workingRule),
                sourceRule.name,
                editable,
                delegate(
                    ScoreRuleConfig target,
                    VisualElement container,
                    bool canEdit)
                {
                    ruleConfigPage.BuildFields(
                        target,
                        container,
                        canEdit);
                },
                TrackWorkingCopyChanges);
        }

        /// <summary>
        /// 同步刷新规则列表、规则应用页以及正在显示同一规则的编辑弹窗。
        /// </summary>
        private void RefreshRuleConfigViews(
            ScoreRuleConfig ruleConfig)
        {
            RefreshConfigurationViews();
            editRuleConfigPanel?.RefreshIfEditing(
                ruleConfig);
        }

        /// <summary>
        /// 创建规则应用项弹窗，并注入单选和多选配置资产的选择入口。
        /// </summary>
        private void BindCreateRuleApplyItemDialogUI()
        {
            createRuleApplyItemPanel =
                new CreateRuleApplyItemPanel(
                    createRuleApplyItemDialogVisualTreeAsset,
                    objectReferenceSelectorVisualTreeAsset,
                    delegate(ScriptableObject value)
                    {
                        return GetDisplayName(
                            configData.GetWorkingConfig(value));
                    },
                    ShowSelectionDialog,
                    ShowMultiSelectionDialog);
        }

        private void BindCreateIssueInfoDialogUI()
        {
            createIssueInfoPanel =
                new CreateIssueInfoPanel(
                    createIssueInfoDialogVisualTreeAsset);
        }

        private void BindCreateWhiteListDialogUI()
        {
            createWhiteListPanel =
                new CreateWhiteListPanel(
                    createWhiteListDialogVisualTreeAsset,
                    objectReferenceSelectorVisualTreeAsset,
                    delegate(ScriptableObject value)
                    {
                        return GetDisplayName(
                            configData.GetWorkingConfig(value));
                    },
                    delegate(string selectedPath)
                    {
                        string unityPath;

                        return TryConvertToUnityPath(
                            selectedPath,
                            out unityPath)
                            ? unityPath
                            : string.Empty;
                    },
                    ShowSelectionDialog);
        }

        // --------------------------------------------------------------
        // 规则应用项
        // --------------------------------------------------------------

        // --------------------------------------------------------------
        // 问题项
        // --------------------------------------------------------------

        // --------------------------------------------------------------
        // 白名单
        // --------------------------------------------------------------

        /// <summary>
        /// 将所有发生变化的工作副本写回真实配置资产。
        /// 没有变化的配置不会执行 CopySerialized。
        /// </summary>
        private void SaveAllConfigs()
        {
            if (!CommitAllChanges())
            {
                return;
            }

            toolbarView.SetStatus(
                pageHost.CurrentPage.DisplayName +
                "：配置已保存");
        }

        /// <summary>
        /// 验证并提交当前窗口中的全部待保存配置和文件操作。
        /// </summary>
        private bool CommitAllChanges()
        {
            if (!ValidateBeforeSave())
            {
                return false;
            }

            bool hasProjectSettings =
                projectSettingsView != null &&
                projectSettingsView.WorkingConfig != null;
            bool shouldEnableSpineChecks =
                hasProjectSettings &&
                projectSettingsView.WorkingConfig.EnableSpineChecks;

            if (shouldEnableSpineChecks &&
                !AssetScoreToolSpineSettings.IsSpineInstalled)
            {
                EditorUtility.DisplayDialog(
                    "Spine 检测",
                    "未检测到 spine-unity 与 spine-csharp。请先导入 Spine，再启用 Spine 检测。",
                    "确定");
                return false;
            }

            string errorMessage;

            if (!configData.CommitWorkingCopies(
                    out errorMessage))
            {
                hasUnsavedChanges = true;
                UpdateDirtyState();

                EditorUtility.DisplayDialog(
                    "保存配置失败",
                    errorMessage,
                    "确定");
                return false;
            }

            if (hasProjectSettings)
            {
                AssetScoreToolSpineSettings.SetEnabled(
                    shouldEnableSpineChecks);
            }

            ClearValidationErrors();
            UpdateDirtyState();
            RefreshConfigurationViews();
            return true;
        }

        /// <summary>
        /// 丢弃全部工作副本修改，重新从真实配置资产创建副本。
        /// </summary>
        private void RevertAllConfigs()
        {
            editRuleConfigPanel?.Close();
            configData.ResetWorkingCopies();
            ClearValidationErrors();
            UpdateDirtyState();
            RefreshConfigurationViews();

            toolbarView.SetStatus(
                pageHost.CurrentPage.DisplayName +
                "：已还原未保存修改");
        }

        /// <summary>
        /// 关闭窗口时选择“保存”后由 Unity 自动调用。
        /// </summary>
        public override void SaveChanges()
        {
            if (!CommitAllChanges())
            {
                return;
            }

            base.SaveChanges();
        }

        /// <summary>
        /// 关闭窗口时选择“不保存”后由 Unity 自动调用。
        ///
        /// 编辑期间真实资产没有被修改，
        /// 因此这里只需要销毁工作副本。
        /// </summary>
        public override void DiscardChanges()
        {
            configData?.Dispose();
            base.DiscardChanges();
        }

        /// <summary>
        /// 确认删除配置后，只登记待删除状态并清理工作数据中的关联。
        /// </summary>
        private void DeleteConfig(
            ScriptableObject config)
        {
            ScriptableObject sourceConfig =
                configData.GetSourceConfig(config);

            string assetPath =
                configData.GetConfigAssetPath(sourceConfig);

            if (ConfigWindowData.IsPackageAsset(assetPath))
            {
                return;
            }

            bool confirmed =
                EditorUtility.DisplayDialog(
                    "删除配置",
                    "确定删除配置“" +
                    GetDisplayName(
                        configData.GetWorkingConfig(sourceConfig)) +
                    "”吗？\n\n保存后将删除对应文件，" +
                    "并自动清理关联的异常数据。",
                    "删除",
                    "取消");

            if (!confirmed)
            {
                return;
            }

            ScoreRuleConfig workingRule =
                configData.GetWorkingConfig(sourceConfig)
                    as ScoreRuleConfig;

            if (editRuleConfigPanel != null &&
                editRuleConfigPanel.IsEditing(workingRule))
            {
                editRuleConfigPanel.Close();
            }

            if (!configData.MarkConfigForDeletion(sourceConfig))
            {
                return;
            }

            ClearValidationErrors();
            UpdateDirtyState();
            RefreshConfigurationViews();
        }

        /// <summary>
        /// 将 Package 配置登记为待保存的本地副本。
        /// </summary>
        private void CopyConfigToLocal(
            ScriptableObject config)
        {
            ScriptableObject sourceConfig =
                configData.GetSourceConfig(config);

            string targetFolder =
                GetDefaultFolderForConfig(sourceConfig);

            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                targetFolder = "Assets";
            }

            string targetPath =
                EditorUtility.SaveFilePanelInProject(
                    "复制配置到本地",
                    sourceConfig.name,
                    "asset",
                    "选择本地配置保存位置。",
                    targetFolder);

            if (string.IsNullOrEmpty(targetPath))
            {
                return;
            }

            ScriptableObject copiedConfig =
                configData.CreatePendingConfigCopy(
                    sourceConfig,
                    targetPath);

            if (copiedConfig == null)
            {
                EditorUtility.DisplayDialog(
                    "复制配置",
                    "复制配置失败。",
                    "确定");

                return;
            }

            UpdateDirtyState();
            RefreshConfigurationViews();
        }

        private string GetDefaultFolderForConfig(
            ScriptableObject config)
        {
            ScriptableObject sourceConfig =
                configData.GetSourceConfig(config);
            IConfigPage page = GetPageByConfigType(
                sourceConfig == null
                    ? null
                    : sourceConfig.GetType());

            return page == null || page.CreateOptions == null
                ? DefaultConfigRootFolder
                : page.CreateOptions.DefaultLocalFolder;
        }

        private void OpenConfigFolder()
        {
            EnsureAssetFolder(DefaultConfigRootFolder);

            string projectRoot =
                Directory.GetParent(
                    Application.dataPath).FullName;

            string absolutePath =
                Path.Combine(
                    projectRoot,
                    DefaultConfigRootFolder.Replace(
                        "/",
                        Path.DirectorySeparatorChar.ToString()));

            EditorUtility.RevealInFinder(absolutePath);
        }

        private ConfigSelectionPanel configSelectionPanel;

        private void BindSelectionDialogUI()
        {
            configSelectionPanel =
                new ConfigSelectionPanel(
                    selectionDialogVisualTreeAsset);
        }

        /// <summary>
        /// 打开配置单选面板。数据由窗口准备，搜索和临时选择由 Panel 管理。
        /// </summary>
        internal void ShowSelectionDialog(
            string title,
            Type configType,
            ScriptableObject currentValue,
            bool allowNone,
            string noneText,
            Action<ScriptableObject> confirmAction)
        {
            ShowSelectionDialog(
                title,
                configType,
                currentValue,
                allowNone,
                noneText,
                null,
                confirmAction);
        }

        internal void ShowSelectionDialog(
            string title,
            Type configType,
            ScriptableObject currentValue,
            bool allowNone,
            string noneText,
            IReadOnlyCollection<string> excludedRuleIds,
            Action<ScriptableObject> confirmAction)
        {
            configSelectionPanel.Open(
                dialogHost,
                title,
                BuildSelectionOptions(
                    configType,
                    excludedRuleIds),
                currentValue,
                allowNone,
                noneText,
                confirmAction);
        }

        /// <summary>
        /// 打开配置多选面板，并使用指定类型的现有候选项初始化临时选择。
        /// </summary>
        internal void ShowMultiSelectionDialog(
            string title,
            Type configType,
            IReadOnlyList<ScriptableObject> currentValues,
            Action<IReadOnlyList<ScriptableObject>> confirmAction)
        {
            configSelectionPanel.OpenMultiple(
                dialogHost,
                title,
                BuildSelectionOptions(configType),
                currentValues,
                confirmAction);
        }

        private List<ConfigSelectionOption> BuildSelectionOptions(
            Type configType,
            IReadOnlyCollection<string> excludedRuleIds = null)
        {
            List<ScriptableObject> configs =
                configData.LoadConfigsByType(configType);

            List<ConfigSelectionOption> options =
                new List<ConfigSelectionOption>();

            for (int i = 0; i < configs.Count; i++)
            {
                ScriptableObject sourceConfig =
                    configs[i];

                ScriptableObject displayConfig =
                    configData.GetWorkingConfig(sourceConfig);

                if (configType == typeof(ScoreRuleConfig) &&
                    excludedRuleIds != null)
                {
                    ScoreRuleConfig ruleConfig =
                        displayConfig as ScoreRuleConfig;

                    bool isExcluded = false;

                    if (ruleConfig != null)
                    {
                        foreach (string excludedRuleId in excludedRuleIds)
                        {
                            if (string.Equals(
                                    excludedRuleId,
                                    ruleConfig.RuleId,
                                    StringComparison.Ordinal))
                            {
                                isExcluded = true;
                                break;
                            }
                        }
                    }

                    if (isExcluded)
                    {
                        continue;
                    }
                }

                options.Add(
                    new ConfigSelectionOption(
                        sourceConfig,
                        GetDisplayName(displayConfig),
                        sourceConfig.name));
            }

            options.Sort(
                delegate(
                    ConfigSelectionOption left,
                    ConfigSelectionOption right)
                {
                    return string.Compare(
                        left.DisplayName,
                        right.DisplayName,
                        StringComparison.OrdinalIgnoreCase);
                });

            return options;
        }

        internal VisualElement CreateSection(
            string title,
            Button actionButton = null)
        {
            VisualElement section = new VisualElement();
            section.AddToClassList("config-section");

            VisualElement header = new VisualElement();
            header.AddToClassList("config-section-header");

            Label titleLabel = new Label(title);
            titleLabel.AddToClassList("config-section-title");
            header.Add(titleLabel);

            if (actionButton != null)
            {
                header.Add(actionButton);
            }

            section.Add(header);

            VisualElement body = new VisualElement();
            body.name = "SectionBody";
            body.AddToClassList("config-section-body");
            section.Add(body);

            return section;
        }

        /// <summary>
        /// 创建与 HTML content-section 对应的配置内容区块。
        /// </summary>
        internal VisualElement CreateContentSection(
            string title,
            Button actionButton = null)
        {
            VisualElement section =
                CreateSection(title, actionButton);
            section.AddToClassList("html-content-section");
            return section;
        }

        internal VisualElement GetSectionBody(
            VisualElement section)
        {
            return section.Q<VisualElement>("SectionBody");
        }

        internal Label CreateFieldHint(
            string hint,
            bool required = false)
        {
            string text = required
                ? "必填。" + hint
                : hint;

            Label label = new Label(text);
            label.AddToClassList("field-hint");

            if (required)
            {
                label.AddToClassList("required-field-hint");
            }

            return label;
        }

        private PropertyField CreatePropertyField(
            SerializedObject serializedObject,
            string propertyName,
            string displayName,
            bool editable)
        {
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                return null;
            }

            PropertyField field =
                new PropertyField(
                    property.Copy(),
                    displayName);

            field.Bind(serializedObject);
            field.SetEnabled(editable);
            field.AddToClassList("config-property-field");

            validationPresenter.ApplyFieldVisual(
                field,
                serializedObject.targetObject as ScriptableObject,
                property.propertyPath);

            return field;
        }

        internal void AddPropertyField(
            VisualElement container,
            SerializedObject serializedObject,
            string propertyName,
            string displayName,
            bool editable,
            string hint = null,
            bool required = false)
        {
            PropertyField field =
                CreatePropertyField(
                    serializedObject,
                    propertyName,
                    displayName,
                    editable);

            if (field == null)
            {
                return;
            }

            container.Add(field);

            if (!string.IsNullOrEmpty(hint))
            {
                container.Add(
                    CreateFieldHint(hint, required));
            }
        }

        private void AddTextField(
            VisualElement container,
            SerializedObject serializedObject,
            string propertyName,
            string displayName,
            bool editable,
            bool multiline,
            string hint = null,
            bool required = false)
        {
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                return;
            }

            TextField field = new TextField(displayName);
            field.multiline = multiline;
            field.BindProperty(property.Copy());
            field.SetEnabled(editable);
            field.AddToClassList(
                multiline
                    ? "multiline-text-field"
                    : "config-text-field");

            validationPresenter.ApplyFieldVisual(
                field,
                serializedObject.targetObject as ScriptableObject,
                property.propertyPath);

            container.Add(field);

            if (!string.IsNullOrEmpty(hint))
            {
                container.Add(
                    CreateFieldHint(hint, required));
            }
        }

        internal void AddRelativeTextField(
            VisualElement container,
            SerializedProperty parent,
            string relativeName,
            string displayName,
            bool editable,
            bool multiline,
            string hint = null,
            bool required = false)
        {
            SerializedProperty property =
                parent.FindPropertyRelative(relativeName);

            if (property == null)
            {
                return;
            }

            TextField field = new TextField(displayName);
            field.multiline = multiline;
            field.BindProperty(property.Copy());
            field.SetEnabled(editable);
            field.AddToClassList(
                multiline
                    ? "multiline-text-field"
                    : "config-text-field");

            validationPresenter.ApplyFieldVisual(
                field,
                property.serializedObject.targetObject
                    as ScriptableObject,
                property.propertyPath);

            container.Add(field);

            if (!string.IsNullOrEmpty(hint))
            {
                container.Add(
                    CreateFieldHint(hint, required));
            }
        }

        internal void AddRelativePropertyField(
            VisualElement container,
            ScriptableObject owner,
            SerializedObject serializedObject,
            SerializedProperty parent,
            string relativeName,
            string displayName,
            bool editable,
            string hint = null,
            bool required = false)
        {
            SerializedProperty property =
                parent.FindPropertyRelative(relativeName);

            if (property == null)
            {
                return;
            }

            PropertyField field =
                new PropertyField(
                    property.Copy(),
                    displayName);

            field.Bind(serializedObject);
            field.SetEnabled(editable);
            field.AddToClassList("config-property-field");

            validationPresenter.ApplyFieldVisual(
                field,
                owner,
                property.propertyPath);

            container.Add(field);

            if (!string.IsNullOrEmpty(hint))
            {
                container.Add(
                    CreateFieldHint(hint, required));
            }
        }

        internal void SetObjectReference(
            ScriptableObject owner,
            string propertyPath,
            UnityEngine.Object value,
            Action refreshAction)
        {
            Undo.RecordObject(
                owner,
                "修改配置引用");

            SerializedObject serializedObject =
                new SerializedObject(owner);

            SerializedProperty property =
                serializedObject.FindProperty(propertyPath);

            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedProperties();
            NotifyWorkingCopyChanged(owner);

            if (refreshAction != null)
            {
                rootVisualElement.schedule.Execute(
                    refreshAction);
            }
        }

        internal void DeleteArrayElement(
            ScriptableObject owner,
            string arrayPropertyPath,
            int index,
            string undoName,
            Action refreshAction)
        {
            Undo.RecordObject(owner, undoName);

            SerializedObject serializedObject =
                new SerializedObject(owner);

            SerializedProperty arrayProperty =
                serializedObject.FindProperty(
                    arrayPropertyPath);

            if (arrayProperty == null ||
                !arrayProperty.isArray ||
                index < 0 ||
                index >= arrayProperty.arraySize)
            {
                return;
            }

            int oldSize = arrayProperty.arraySize;
            arrayProperty.DeleteArrayElementAtIndex(index);

            if (arrayProperty.arraySize == oldSize &&
                index < arrayProperty.arraySize)
            {
                arrayProperty.DeleteArrayElementAtIndex(index);
            }

            serializedObject.ApplyModifiedProperties();
            NotifyWorkingCopyChanged(owner);

            if (refreshAction != null)
            {
                refreshAction();
            }
        }

        internal string GetSerializedString(
            SerializedProperty parent,
            string relativeName)
        {
            if (parent == null)
            {
                return string.Empty;
            }

            SerializedProperty property =
                parent.FindPropertyRelative(relativeName);

            return property == null
                ? string.Empty
                : property.stringValue;
        }

        private string GetObjectDisplayName(
            ScriptableObject config)
        {
            if (config == null)
            {
                return "未选择";
            }

            return GetDisplayName(
                configData.GetWorkingConfig(config));
        }

        internal VisualElement CreateReferenceDisplay(
            ScriptableObject config)
        {
            VisualElement display = new VisualElement();
            display.AddToClassList("object-reference-display");

            Label icon = new Label("◇");
            icon.AddToClassList("object-reference-icon");
            display.Add(icon);

            VisualElement textContainer =
                new VisualElement();

            textContainer.AddToClassList(
                "object-reference-text");

            Label displayName =
                new Label(GetObjectDisplayName(config));

            displayName.AddToClassList(
                "object-reference-display-name");

            textContainer.Add(displayName);

            ScriptableObject sourceConfig =
                configData.GetSourceConfig(config);

            Label assetName =
                new Label(
                    sourceConfig == null
                        ? "未选择配置"
                        : sourceConfig.name);

            assetName.AddToClassList(
                "object-reference-asset-name");

            textContainer.Add(assetName);
            display.Add(textContainer);

            return display;
        }

        /// <summary>
        /// 注册页面并绑定左侧页签。
        /// </summary>
        private void BindTabsUI()
        {
            pageHost.ClearPages();
            pageHost.RegisterPage(profileConfigPage, SwitchPage);
            pageHost.RegisterPage(ruleApplyConfigPage, SwitchPage);
            pageHost.RegisterPage(ruleConfigPage, SwitchPage);
            pageHost.RegisterPage(assetTypeConfigPage, SwitchPage);
            pageHost.RegisterPage(projectSettingsView, SwitchPage);
        }

        /// <summary>
        /// 切换当前页面，并同步左侧选中状态和底部状态文本。
        /// </summary>
        private void SwitchPage(EConfigPage page)
        {
            if (!pageHost.ActivatePage(page))
            {
                return;
            }

            toolbarView.SetStatus(
                "当前页面：" +
                pageHost.CurrentPage.DisplayName);
        }

        /// <summary>
        /// 重新验证工作副本，并按统一顺序刷新页面和诊断呈现。
        /// </summary>
        private void RefreshConfigurationViews()
        {
            ConfigValidationIssue ignored;
            ValidateAllWorkingCopies(out ignored);
            validationPresenter.SetIssues(validationIssues);
            pageHost.RefreshAll();
            validationPresenter.Refresh();
        }

        private ConfigWindowToolbarView toolbarView;

        /// <summary>
        /// 绑定顶部工具栏和底部状态栏。
        /// </summary>
        private void BindToolbarUI()
        {
            toolbarView = new ConfigWindowToolbarView(
                rootVisualElement,
                SaveAllConfigs,
                RevertAllConfigs,
                OnRefreshClicked,
                OpenConfigFolder);

        }

        private void OnRefreshClicked()
        {
            configData.Sync();
            RefreshConfigurationViews();
            UpdateDirtyState();

            toolbarView.SetStatus(
                pageHost.CurrentPage.DisplayName +
                "：列表已刷新");
        }

        /// <summary>
        /// 按层级创建 Unity 资源目录，避免直接写入不存在的文件夹。
        /// </summary>
        private void EnsureAssetFolder(string folderPath)
        {
            folderPath = folderPath.Replace("\\", "/");

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string childPath =
                    currentPath + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(childPath))
                {
                    AssetDatabase.CreateFolder(
                        currentPath,
                        parts[i]);
                }

                currentPath = childPath;
            }
        }

        internal bool TryConvertToUnityPath(
            string absolutePath,
            out string unityPath)
        {
            unityPath = string.Empty;

            absolutePath =
                Path.GetFullPath(absolutePath)
                    .Replace("\\", "/");

            string projectRoot =
                Directory.GetParent(
                        Application.dataPath)
                    .FullName
                    .Replace("\\", "/");

            if (!absolutePath.StartsWith(
                    projectRoot + "/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            unityPath =
                absolutePath.Substring(
                    projectRoot.Length + 1);

            return true;
        }

        /// <summary>
        /// 判断路径是否为当前项目 Assets 下已存在的有效文件夹。
        /// </summary>
        private bool IsValidAssetFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return false;
            }

            string normalizedPath =
                folderPath.Replace("\\", "/");

            return (normalizedPath == "Assets" ||
                    normalizedPath.StartsWith(
                        "Assets/",
                        StringComparison.Ordinal)) &&
                   AssetDatabase.IsValidFolder(normalizedPath);
        }

        /// <summary>
        /// 读取项目设置指定的默认文件夹；缺失或失效时回退到 Assets。
        /// </summary>
        private string GetDefaultSaveFolder(
            DefaultAsset configuredFolder)
        {
            if (configuredFolder != null)
            {
                string folderPath =
                    AssetDatabase.GetAssetPath(configuredFolder);

                if (IsValidAssetFolder(folderPath))
                {
                    return folderPath;
                }
            }

            return "Assets";
        }

        private string ToAbsoluteProjectPath(
            string unityPath)
        {
            string projectRoot =
                Directory.GetParent(
                    Application.dataPath)
                    ?.FullName;

            return Path.Combine(
                projectRoot,
                unityPath.Replace(
                    "/",
                    Path.DirectorySeparatorChar.ToString()));
        }

        private readonly List<ConfigValidationIssue> validationIssues =
            new List<ConfigValidationIssue>();

        private void ClearValidationErrors()
        {
            validationIssues.Clear();
            validationPresenter.SetIssues(validationIssues);
            validationPresenter.Refresh();
        }

        private bool ValidateAllWorkingCopies(
            out ConfigValidationIssue firstError)
        {
            validationIssues.Clear();

            foreach (ScriptableObject config in
                     configData.WorkingCopies)
            {
                if (config is ScoreRuleProfileConfig)
                {
                    ValidateProfile(
                        (ScoreRuleProfileConfig)config);
                }
                else if (config is RuleApplyConfig)
                {
                    ValidateRuleApply(
                        (RuleApplyConfig)config);
                }
                else if (config is ScoreRuleConfig)
                {
                    ValidateRule(
                        (ScoreRuleConfig)config);
                }
                else if (config is AssetTypeConfig)
                {
                    ValidateAssetType(
                        (AssetTypeConfig)config);
                }
                else if (config is UserSettingConfig)
                {
                    ValidateProjectSettings(
                        (UserSettingConfig)config);
                }
            }

            List<ConfigValidationIssue> crossConfigIssues =
                new List<ConfigValidationIssue>();

            configValidation?.ValidateCrossConfig(
                configData.WorkingCopies,
                crossConfigIssues);

            for (int i = 0; i < crossConfigIssues.Count; i++)
            {
                ConfigValidationIssue issue =
                    crossConfigIssues[i];

                validationIssues.Add(issue);
            }

            firstError = null;

            for (int i = 0; i < validationIssues.Count; i++)
            {
                if (validationIssues[i].Severity ==
                    EConfigValidationSeverity.Error)
                {
                    firstError = validationIssues[i];
                    break;
                }
            }

            return firstError == null;
        }

        private void ValidateProfile(
            ScoreRuleProfileConfig config)
        {
            SerializedObject serializedObject =
                new SerializedObject(config);

            ValidateRequiredString(
                config,
                serializedObject.FindProperty("profileId"),
                EConfigPage.Profile,
                "配置档 ID 不能为空。");

            ValidateIdFormat(
                config,
                serializedObject.FindProperty("profileId"),
                EConfigPage.Profile,
                "配置档 ID");

            ValidateRequiredString(
                config,
                serializedObject.FindProperty("displayName"),
                EConfigPage.Profile,
                "显示名称不能为空。");
        }

        private void ValidateRuleApply(
            RuleApplyConfig config)
        {
            SerializedObject serializedObject =
                new SerializedObject(config);

            ValidateRequiredString(
                config,
                serializedObject.FindProperty("ruleApplyId"),
                EConfigPage.RuleApply,
                "规则应用配置 ID 不能为空。");

            ValidateIdFormat(
                config,
                serializedObject.FindProperty("ruleApplyId"),
                EConfigPage.RuleApply,
                "规则应用配置 ID");

            ValidateRequiredString(
                config,
                serializedObject.FindProperty("displayName"),
                EConfigPage.RuleApply,
                "显示名称不能为空。");

            SerializedProperty whiteListItems =
                serializedObject.FindProperty("whiteListItems");

            if (whiteListItems == null ||
                !whiteListItems.isArray)
            {
                return;
            }

            for (int i = 0; i < whiteListItems.arraySize; i++)
            {
                SerializedProperty item =
                    whiteListItems.GetArrayElementAtIndex(i);

                SerializedProperty assetPath =
                    item.FindPropertyRelative("assetPath");

                ValidateRequiredString(
                    config,
                    assetPath,
                    EConfigPage.RuleApply,
                    "白名单第 " + (i + 1) +
                    " 项的资源路径不能为空。");

                ValidateIdFormat(
                    config,
                    item.FindPropertyRelative("issueInfoId"),
                    EConfigPage.RuleApply,
                    "白名单第 " + (i + 1) + " 项的问题 ID");
            }
        }

        private void ValidateRule(
            ScoreRuleConfig config)
        {
            SerializedObject serializedObject =
                new SerializedObject(config);

            ValidateRequiredString(
                config,
                serializedObject.FindProperty("ruleId"),
                EConfigPage.Rule,
                "规则 ID 不能为空。");

            ValidateIdFormat(
                config,
                serializedObject.FindProperty("ruleId"),
                EConfigPage.Rule,
                "规则 ID");

            ValidateRequiredString(
                config,
                serializedObject.FindProperty("displayName"),
                EConfigPage.Rule,
                "显示名称不能为空。");

            SerializedProperty issueInfos =
                serializedObject.FindProperty("issueInfos");

            if (issueInfos == null ||
                !issueInfos.isArray)
            {
                return;
            }

            for (int i = 0; i < issueInfos.arraySize; i++)
            {
                SerializedProperty item =
                    issueInfos.GetArrayElementAtIndex(i);

                ValidateRequiredString(
                    config,
                    item.FindPropertyRelative("issueInfoId"),
                    EConfigPage.Rule,
                    "问题项第 " + (i + 1) +
                    " 项的问题 ID 不能为空。");

                ValidateIdFormat(
                    config,
                    item.FindPropertyRelative("issueInfoId"),
                    EConfigPage.Rule,
                    "问题项第 " + (i + 1) + " 项的问题 ID");

                ValidateRequiredString(
                    config,
                    item.FindPropertyRelative("displayName"),
                    EConfigPage.Rule,
                    "问题项第 " + (i + 1) +
                    " 项的显示名称不能为空。");

                ValidateRequiredString(
                    config,
                    item.FindPropertyRelative("issueMessage"),
                    EConfigPage.Rule,
                    "问题项第 " + (i + 1) +
                    " 项的问题提示不能为空。");
            }
        }

        private void ValidateAssetType(
            AssetTypeConfig config)
        {
            SerializedObject serializedObject =
                new SerializedObject(config);

            ValidateRequiredString(
                config,
                serializedObject.FindProperty("assetTypeId"),
                EConfigPage.AssetType,
                "资源类型 ID 不能为空。");

            ValidateIdFormat(
                config,
                serializedObject.FindProperty("assetTypeId"),
                EConfigPage.AssetType,
                "资源类型 ID");

            ValidateRequiredString(
                config,
                serializedObject.FindProperty("displayName"),
                EConfigPage.AssetType,
                "显示名称不能为空。");
        }

        private void ValidateProjectSettings(
            UserSettingConfig config)
        {
            SerializedObject serializedObject =
                new SerializedObject(config);

            ValidateDefaultFolder(
                config,
                serializedObject.FindProperty(
                    ProjectSettingsPage.ProfileDefaultFolderProperty),
                ProjectSettingsPage.ProfileDefaultFolderProperty,
                "检测配置档默认保存目录必须是有效文件夹。");

            ValidateDefaultFolder(
                config,
                serializedObject.FindProperty(
                    ProjectSettingsPage.RuleApplyDefaultFolderProperty),
                ProjectSettingsPage.RuleApplyDefaultFolderProperty,
                "规则应用配置默认保存目录必须是有效文件夹。");

            ValidateDefaultFolder(
                config,
                serializedObject.FindProperty(
                    ProjectSettingsPage.ScoreRuleDefaultFolderProperty),
                ProjectSettingsPage.ScoreRuleDefaultFolderProperty,
                "检测规则配置默认保存目录必须是有效文件夹。");

            ValidateDefaultFolder(
                config,
                serializedObject.FindProperty(
                    ProjectSettingsPage.AssetTypeDefaultFolderProperty),
                ProjectSettingsPage.AssetTypeDefaultFolderProperty,
                "资源类型配置默认保存目录必须是有效文件夹。");

            ValidateDefaultFolder(
                config,
                serializedObject.FindProperty(
                    ProjectSettingsPage.CheckRuleDefaultFolderProperty),
                ProjectSettingsPage.CheckRuleDefaultFolderProperty,
                "CheckRule 脚本默认保存目录必须是有效文件夹。");
        }

        private void ValidateDefaultFolder(
            UserSettingConfig config,
            SerializedProperty property,
            string propertyName,
            string message)
        {
            DefaultAsset folder = property == null
                ? null
                : property.objectReferenceValue as DefaultAsset;

            if (folder != null &&
                IsValidAssetFolder(
                    AssetDatabase.GetAssetPath(folder)))
            {
                return;
            }

            ConfigValidationIssue issue =
                new ConfigValidationIssue();

            issue.WorkingConfig = config;
            issue.Page = EConfigPage.ProjectSettings;
            issue.PropertyPath = property == null
                ? propertyName
                : property.propertyPath;
            issue.Message = message;
            issue.Severity = EConfigValidationSeverity.Error;

            validationIssues.Add(issue);
        }

        private void ValidateRequiredString(
            ScriptableObject workingConfig,
            SerializedProperty property,
            EConfigPage page,
            string message)
        {
            if (property == null ||
                !string.IsNullOrWhiteSpace(
                    property.stringValue))
            {
                return;
            }

            ConfigValidationIssue issue =
                new ConfigValidationIssue();

            issue.WorkingConfig = workingConfig;
            issue.Page = page;
            issue.PropertyPath = property.propertyPath;
            issue.Message = message;
            issue.Severity = EConfigValidationSeverity.Error;

            validationIssues.Add(issue);
        }

        /// <summary>
        /// 检查非空 ID 字段是否符合统一命名规范，并将错误定位到对应字段。
        /// </summary>
        private void ValidateIdFormat(
            ScriptableObject workingConfig,
            SerializedProperty property,
            EConfigPage page,
            string displayName)
        {
            if (property == null ||
                string.IsNullOrWhiteSpace(property.stringValue) ||
                ConfigIdNamingUtility.IsValid(property.stringValue))
            {
                return;
            }

            ConfigValidationIssue issue =
                new ConfigValidationIssue();

            issue.WorkingConfig = workingConfig;
            issue.Page = page;
            issue.PropertyPath = property.propertyPath;
            issue.Message = displayName + " 格式不正确。" +
                            ConfigIdNamingUtility.RuleHint;
            issue.Severity = EConfigValidationSeverity.Error;

            validationIssues.Add(issue);
        }

        /// <summary>
        /// 根据脏状态和最新诊断刷新保存按钮与诊断呈现。
        /// </summary>
        private void UpdateSaveButtonState()
        {
            if (toolbarView == null)
            {
                return;
            }

            ConfigValidationIssue ignored;
            bool isValid =
                ValidateAllWorkingCopies(out ignored);

            validationPresenter.SetIssues(validationIssues);

            // 有修改时允许点击按钮，以便在校验失败时定位到具体字段；
            // 真正写入资产前仍会被 ValidateBeforeSave 拦截。
            toolbarView.SetSaveState(hasUnsavedChanges, isValid);
            validationPresenter.Refresh();
        }

        /// <summary>
        /// 在写回真实资产前执行最终验证，并定位首个阻断错误。
        /// </summary>
        private bool ValidateBeforeSave()
        {
            ConfigValidationIssue firstError;

            if (ValidateAllWorkingCopies(
                    out firstError))
            {
                return true;
            }

            hasUnsavedChanges = true;
            UpdateSaveButtonState();
            FocusValidationIssue(firstError);
            return false;
        }

        /// <summary>
        /// 展开诊断所属配置、切换页面，并委托 Presenter 聚焦字段。
        /// </summary>
        private void FocusValidationIssue(
            ConfigValidationIssue issue)
        {
            if (issue == null)
            {
                return;
            }

            string assetPath =
                configData.GetConfigAssetPath(
                    issue.WorkingConfig);

            IConfigPage errorPage;

            if (!pageHost.TryGetPage(
                    issue.Page,
                    out errorPage))
            {
                return;
            }

            errorPage.ExpandConfig(assetPath);
            RefreshConfigurationViews();
            SwitchPage(issue.Page);

            toolbarView.SetStatus(
                (issue.Severity ==
                    EConfigValidationSeverity.Warning
                    ? "配置提示："
                    : "不能保存：") +
                GetDisplayName(issue.WorkingConfig) +
                " - " + issue.Message);

            validationPresenter.FocusField(
                issue,
                errorPage.ScrollView);
        }
    }
}
