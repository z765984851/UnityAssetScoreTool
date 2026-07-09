using System;
using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 新建配置表单 Panel。
    /// 本类只管理字段显示、基础必填校验和用户交互，不负责创建配置资产。
    /// </summary>
    public sealed class CreateConfigPanel : ModalPanel
    {
        private readonly TextField assetNameField;
        private readonly PathInputField saveFolderField;
        private readonly VisualElement idRow;
        private readonly Label idLabel;
        private readonly TextField idField;
        private readonly Label idPlaceholderLabel;
        private readonly Label idHintLabel;
        private readonly VisualElement displayNameRow;
        private readonly TextField displayNameField;
        private readonly Label displayNameHintLabel;
        private readonly Button cancelButton;
        private readonly Button confirmButton;
        private readonly Func<string, bool> isSaveFolderValid;
        private readonly Func<string, string> toAbsoluteProjectPath;
        private readonly Func<string, string> toUnityPath;

        private bool requiresDetails;
        private string idPlaceholder;
        private Action<string, string, string, string> createAction;

        public event Action<string, string, string, string> Confirmed;

        public event Action CancelRequested;

        public event Action BrowseRequested;

        public string SaveFolder
        {
            get { return saveFolderField.Value; }
            set
            {
                saveFolderField.Value = value;
                UpdateConfirmState();
            }
        }

        public CreateConfigPanel(
            VisualTreeAsset visualTreeAsset,
            VisualTreeAsset pathInputVisualTreeAsset,
            Func<string, bool> isSaveFolderValid,
            Func<string, string> toAbsoluteProjectPath,
            Func<string, string> toUnityPath)
        {
            if (visualTreeAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(visualTreeAsset));
            }

            if (pathInputVisualTreeAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(pathInputVisualTreeAsset));
            }

            AddToClassList("create-config-dialog-panel");

            this.isSaveFolderValid = isSaveFolderValid;
            this.toAbsoluteProjectPath = toAbsoluteProjectPath;
            this.toUnityPath = toUnityPath;

            TemplateContainer content =
                visualTreeAsset.Instantiate();

            Add(content);

            assetNameField =
                content.Q<TextField>(
                    "CreateConfigAssetNameField");

            VisualElement saveFolderContainer =
                content.Q<VisualElement>(
                    "CreateConfigSaveFolderContainer");

            idRow =
                content.Q<VisualElement>(
                    "CreateConfigIdRow");

            idLabel =
                content.Q<Label>(
                    "CreateConfigIdLabel");

            idField =
                content.Q<TextField>(
                    "CreateConfigIdField");

            ConfigIdNamingUtility.RestrictInput(idField);

            idPlaceholderLabel =
                content.Q<Label>(
                    "CreateConfigIdPlaceholderLabel");

            idHintLabel =
                content.Q<Label>(
                    "CreateConfigIdHintLabel");

            displayNameRow =
                content.Q<VisualElement>(
                    "CreateConfigDisplayNameRow");

            displayNameField =
                content.Q<TextField>(
                    "CreateConfigDisplayNameField");

            displayNameHintLabel =
                content.Q<Label>(
                    "CreateConfigDisplayNameHintLabel");

            cancelButton =
                content.Q<Button>(
                    "CreateConfigCancelButton");

            confirmButton =
                content.Q<Button>(
                    "CreateConfigConfirmButton");

            saveFolderField =
                new PathInputField(
                    pathInputVisualTreeAsset);

            saveFolderField.LabelText =
                "保存文件夹 *";
            saveFolderField.BrowseButtonText =
                "选择文件夹";
            saveFolderField.HintText =
                "必填。必须是 Assets 或其子目录中的有效文件夹。";

            saveFolderContainer.Add(saveFolderField);

            assetNameField.RegisterValueChangedCallback(
                delegate { UpdateConfirmState(); });

            idField.RegisterValueChangedCallback(
                delegate
                {
                    UpdateConfirmState();
                    UpdateIdPlaceholderVisibility();
                });

            displayNameField.RegisterValueChangedCallback(
                delegate { UpdateConfirmState(); });

            saveFolderField.ValueChanged +=
                delegate { UpdateConfirmState(); };

            saveFolderField.BrowseRequested +=
                BrowseFolder;

            cancelButton.clicked +=
                delegate
                {
                    CancelRequested?.Invoke();
                    Close();
                };

            confirmButton.clicked += Confirm;
            Closed +=
                delegate { createAction = null; };
        }

        /// <summary>
        /// 使用指定表单数据打开创建配置 Panel。
        /// </summary>
        public void Open(
            VisualElement host,
            string title,
            bool showDetailFields,
            string defaultAssetName,
            string defaultSaveFolder,
            string configIdLabel,
            string configIdHint,
            string configIdPlaceholder,
            string displayNameHint,
            Action<string, string, string, string> onCreate)
        {
            createAction = onCreate;

            SetData(
                title,
                showDetailFields,
                defaultAssetName,
                defaultSaveFolder,
                configIdLabel,
                configIdHint,
                configIdPlaceholder,
                displayNameHint);

            Show(host);
        }

        /// <summary>
        /// 通过配置窗口弹窗宿主打开创建配置 Panel。
        /// </summary>
        internal void Open(
            ConfigWindowDialogHost host,
            string title,
            bool showDetailFields,
            string defaultAssetName,
            string defaultSaveFolder,
            string configIdLabel,
            string configIdHint,
            string configIdPlaceholder,
            string displayNameHint,
            Action<string, string, string, string> onCreate)
        {
            createAction = onCreate;

            SetData(
                title,
                showDetailFields,
                defaultAssetName,
                defaultSaveFolder,
                configIdLabel,
                configIdHint,
                configIdPlaceholder,
                displayNameHint);

            host.Show(this);
        }

        public void SetData(
            string title,
            bool showDetailFields,
            string defaultAssetName,
            string defaultSaveFolder,
            string configIdLabel,
            string configIdHint,
            string configIdPlaceholder,
            string displayNameHint)
        {
            Title = title;
            requiresDetails = showDetailFields;

            assetNameField.SetValueWithoutNotify(
                defaultAssetName ?? string.Empty);

            saveFolderField.Value =
                defaultSaveFolder ?? string.Empty;
            idField.SetValueWithoutNotify(
                string.Empty);
            idPlaceholder = configIdPlaceholder ?? string.Empty;
            idPlaceholderLabel.text = idPlaceholder;
            displayNameField.SetValueWithoutNotify(
                string.Empty);

            idLabel.text = configIdLabel;
            idHintLabel.text = configIdHint;
            displayNameHintLabel.text = displayNameHint;

            UIElementUtility.SetVisible(idRow, showDetailFields);
            UIElementUtility.SetVisible(displayNameRow, showDetailFields);
            UpdateIdPlaceholderVisibility();

            UpdateConfirmState();

            schedule.Execute(assetNameField.Focus)
                .ExecuteLater(0);
        }

        private void Confirm()
        {
            if (!IsFormValid())
            {
                return;
            }

            string assetName =
                SanitizeAssetFileName(assetNameField.value);
            string configId = idField.value.Trim();
            string displayName = displayNameField.value.Trim();
            string saveFolder = saveFolderField.Value.Trim();

            Confirmed?.Invoke(
                assetName,
                configId,
                displayName,
                saveFolder);

            createAction?.Invoke(
                assetName,
                configId,
                displayName,
                saveFolder);

            Close();
        }

        private void UpdateConfirmState()
        {
            UIElementUtility.SetEnabled(confirmButton, IsFormValid());
        }

        /// <summary>
        /// 根据规则 ID 的实际输入值切换示例占位文本，避免将示例写入配置数据。
        /// </summary>
        private void UpdateIdPlaceholderVisibility()
        {
            UIElementUtility.SetVisible(
                idPlaceholderLabel,
                requiresDetails &&
                string.IsNullOrEmpty(idField.value) &&
                !string.IsNullOrEmpty(idPlaceholder));
        }

        private bool IsFormValid()
        {
            if (string.IsNullOrEmpty(
                    SanitizeAssetFileName(
                        assetNameField.value)))
            {
                return false;
            }

            if (!isSaveFolderValid(
                    saveFolderField.Value))
            {
                return false;
            }

            return !requiresDetails ||
                   (ConfigIdNamingUtility.IsValid(idField.value) &&
                    !string.IsNullOrWhiteSpace(
                        displayNameField.value));
        }

        private string SanitizeAssetFileName(
            string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }

            char[] invalidCharacters =
                Path.GetInvalidFileNameChars();

            string result = fileName.Trim();

            for (int i = 0;
                 i < invalidCharacters.Length;
                 i++)
            {
                result = result.Replace(
                    invalidCharacters[i].ToString(),
                    string.Empty);
            }

            return result;
        }

        private void BrowseFolder()
        {
            BrowseRequested?.Invoke();

            string currentFolder =
                isSaveFolderValid(SaveFolder)
                    ? SaveFolder
                    : "Assets";

            string selectedFolder =
                EditorUtility.OpenFolderPanel(
                    "选择配置保存文件夹",
                    toAbsoluteProjectPath(currentFolder),
                    string.Empty);

            if (string.IsNullOrEmpty(selectedFolder))
            {
                return;
            }

            string unityPath = toUnityPath(selectedFolder);

            if (!isSaveFolderValid(unityPath))
            {
                EditorUtility.DisplayDialog(
                    "新建配置",
                    "请选择当前 Unity 项目 Assets 目录中的有效文件夹。",
                    "确定");
                return;
            }

            SaveFolder = unityPath;
        }
    }
}
