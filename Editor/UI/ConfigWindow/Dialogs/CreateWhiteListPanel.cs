using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 新增白名单表单 Panel。
    /// 本类管理临时表单和规则引用，不负责路径选择或修改 RuleApplyConfig。
    /// </summary>
    public sealed class CreateWhiteListPanel : ModalPanel
    {
        private readonly VisualTreeAsset referenceVisualTreeAsset;
        private readonly Func<ScriptableObject, string> displayNameProvider;
        private readonly Func<string, string> toUnityPath;

        private readonly Action<
            string,
            Type,
            ScriptableObject,
            bool,
            string,
            Action<ScriptableObject>> selectionRequester;

        private readonly TextField assetPathField;
        private readonly Button selectFileButton;
        private readonly Button selectFolderButton;
        private readonly VisualElement ruleContainer;
        private readonly TextField issueInfoIdField;
        private readonly TextField remarkField;
        private readonly Button cancelButton;
        private readonly Button confirmButton;

        private ScoreRuleConfig ruleConfig;
        private ConfigWindowData configData;
        private RuleApplyConfig owner;
        private Action committed;

        public event Action FileSelectionRequested;

        public event Action FolderSelectionRequested;

        public event Action<ScoreRuleConfig> RuleSelectionRequested;

        public event Action<
            string,
            ScoreRuleConfig,
            string,
            string> Confirmed;

        public event Action CancelRequested;

        public string AssetPath
        {
            get { return assetPathField.value; }
            set
            {
                assetPathField.SetValueWithoutNotify(
                    value ?? string.Empty);
                UpdateConfirmState();
            }
        }

        public CreateWhiteListPanel(
            VisualTreeAsset visualTreeAsset,
            VisualTreeAsset objectReferenceVisualTreeAsset,
            Func<ScriptableObject, string> referenceDisplayNameProvider,
            Func<string, string> toUnityPath,
            Action<
                string,
                Type,
                ScriptableObject,
                bool,
                string,
                Action<ScriptableObject>> requestSelection)
        {
            if (visualTreeAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(visualTreeAsset));
            }

            if (objectReferenceVisualTreeAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(objectReferenceVisualTreeAsset));
            }

            if (referenceDisplayNameProvider == null)
            {
                throw new ArgumentNullException(
                    nameof(referenceDisplayNameProvider));
            }

            referenceVisualTreeAsset =
                objectReferenceVisualTreeAsset;
            displayNameProvider =
                referenceDisplayNameProvider;
            this.toUnityPath = toUnityPath;
            selectionRequester = requestSelection;

            Title = "添加白名单";
            AddToClassList("item-create-dialog-panel");

            TemplateContainer content =
                visualTreeAsset.Instantiate();

            Add(content);

            assetPathField =
                content.Q<TextField>(
                    "CreateWhiteListPathField");

            selectFileButton =
                content.Q<Button>(
                    "CreateWhiteListSelectFileButton");

            selectFolderButton =
                content.Q<Button>(
                    "CreateWhiteListSelectFolderButton");

            ruleContainer =
                content.Q<VisualElement>(
                    "CreateWhiteListRuleContainer");

            issueInfoIdField =
                content.Q<TextField>(
                    "CreateWhiteListIssueIdField");

            ConfigIdNamingUtility.RestrictInput(issueInfoIdField);

            remarkField =
                content.Q<TextField>(
                    "CreateWhiteListRemarkField");

            cancelButton =
                content.Q<Button>(
                    "CreateWhiteListCancelButton");

            confirmButton =
                content.Q<Button>(
                    "CreateWhiteListConfirmButton");

            assetPathField.RegisterValueChangedCallback(
                delegate { UpdateConfirmState(); });

            issueInfoIdField.RegisterValueChangedCallback(
                delegate { UpdateConfirmState(); });

            selectFileButton.clicked +=
                delegate
                {
                    FileSelectionRequested?.Invoke();
                    SelectAssetPath(false);
                };

            selectFolderButton.clicked +=
                delegate
                {
                    FolderSelectionRequested?.Invoke();
                    SelectAssetPath(true);
                };

            cancelButton.clicked +=
                delegate
                {
                    CancelRequested?.Invoke();
                    Close();
                };

            confirmButton.clicked += Confirm;
            Closed += ClearContext;
        }

        /// <summary>
        /// 打开新增白名单 Panel，并保存本次编辑所需的数据上下文。
        /// </summary>
        internal void Open(
            ConfigWindowDialogHost host,
            ConfigWindowData data,
            RuleApplyConfig target,
            Action onCommitted)
        {
            configData = data;
            owner = target;
            committed = onCommitted;

            ResetForm();
            host.Show(this);
        }

        public void ResetForm()
        {
            AssetPath = string.Empty;
            ruleConfig = null;
            issueInfoIdField.SetValueWithoutNotify(
                string.Empty);
            remarkField.SetValueWithoutNotify(
                string.Empty);

            RefreshRule();
            UpdateConfirmState();

            schedule.Execute(assetPathField.Focus)
                .ExecuteLater(0);
        }

        public void SetRuleConfig(
            ScoreRuleConfig value)
        {
            ruleConfig = value;
            RefreshRule();
        }

        private void RefreshRule()
        {
            ruleContainer.Clear();

            ObjectReferenceSelector selector =
                new ObjectReferenceSelector(
                    referenceVisualTreeAsset);

            selector.LabelText = string.Empty;
            selector.DisplayName =
                ruleConfig == null
                    ? "全部规则"
                    : displayNameProvider(ruleConfig);
            selector.Metadata =
                ruleConfig == null
                    ? "未指定规则配置"
                    : ruleConfig.name;
            selector.SelectButtonText = "选择规则";
            selector.SelectButtonVisible = true;
            selector.ClearButtonText = "清除";
            selector.ClearButtonVisible = ruleConfig != null;
            selector.PingButtonVisible = false;
            selector.EditButtonVisible = false;

            selector.SelectRequested +=
                delegate
                {
                    RuleSelectionRequested?.Invoke(ruleConfig);
                    selectionRequester(
                        "选择白名单规则",
                        typeof(ScoreRuleConfig),
                        ruleConfig,
                        true,
                        "全部规则",
                        selected => SetRuleConfig(
                            selected as ScoreRuleConfig));
                };

            selector.ClearRequested +=
                delegate
                {
                    ruleConfig = null;
                    RefreshRule();
                };

            ruleContainer.Add(selector);
        }

        private void Confirm()
        {
            if (!IsFormValid())
            {
                return;
            }

            string assetPath = assetPathField.value.Trim();
            string issueInfoId = issueInfoIdField.value.Trim();
            string remark = remarkField.value.Trim();

            configData.AddWhiteListItem(
                owner,
                assetPath,
                ruleConfig,
                issueInfoId,
                remark);

            Confirmed?.Invoke(
                assetPath,
                ruleConfig,
                issueInfoId,
                remark);

            committed?.Invoke();
            Close();
        }

        private void UpdateConfirmState()
        {
            UIElementUtility.SetEnabled(confirmButton, IsFormValid());
        }

        private bool IsFormValid()
        {
            return !string.IsNullOrWhiteSpace(
                       assetPathField.value) &&
                   (string.IsNullOrWhiteSpace(
                        issueInfoIdField.value) ||
                    ConfigIdNamingUtility.IsValid(
                        issueInfoIdField.value));
        }

        private void ClearContext()
        {
            configData = null;
            owner = null;
            committed = null;
        }

        private void SelectAssetPath(bool selectFolder)
        {
            string selectedPath = selectFolder
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

            string unityPath = toUnityPath(selectedPath);

            if (string.IsNullOrEmpty(unityPath))
            {
                EditorUtility.DisplayDialog(
                    "添加白名单",
                    "请选择当前 Unity 项目内的资源或文件夹。",
                    "确定");
                return;
            }

            AssetPath = unityPath;
        }
    }
}
