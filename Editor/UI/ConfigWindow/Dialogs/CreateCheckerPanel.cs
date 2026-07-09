using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 创建检测脚本表单 Panel。
    /// 本类管理表单显示和基础必填校验，不负责选择目录或写入脚本文件。
    /// </summary>
    public sealed class CreateCheckerPanel : ModalPanel
    {
        private readonly TextField classNameField;
        private readonly PathInputField saveFolderField;
        private readonly Button cancelButton;
        private readonly Button confirmButton;
        private readonly Func<string, bool> isSaveFolderValid;
        private readonly Func<string, string> toAbsoluteProjectPath;
        private readonly Func<string, string> toUnityPath;
        private ConfigWindowData configData;
        private ScoreRuleConfig targetRule;
        private Action created;

        public event Action<string, string> Confirmed;

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

        public CreateCheckerPanel(
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

            Title = "创建检测脚本（CheckRule）";
            AddToClassList("create-checker-dialog-panel");

            this.isSaveFolderValid = isSaveFolderValid;
            this.toAbsoluteProjectPath = toAbsoluteProjectPath;
            this.toUnityPath = toUnityPath;

            TemplateContainer content =
                visualTreeAsset.Instantiate();

            Add(content);

            classNameField =
                content.Q<TextField>(
                    "CheckerClassNameField");

            VisualElement saveFolderContainer =
                content.Q<VisualElement>(
                    "CheckerSaveFolderContainer");

            cancelButton =
                content.Q<Button>(
                    "CreateCheckerCancelButton");

            confirmButton =
                content.Q<Button>(
                    "CreateCheckerConfirmButton");

            saveFolderField =
                new PathInputField(
                    pathInputVisualTreeAsset);

            saveFolderField.LabelText =
                "保存文件夹 *";
            saveFolderField.BrowseButtonText =
                "选择文件夹";
            saveFolderField.HintText =
                "必填。检测脚本必须创建在 Assets 目录中。";

            saveFolderContainer.Add(saveFolderField);

            classNameField.RegisterValueChangedCallback(
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
            Closed += ClearContext;
        }

        /// <summary>
        /// 打开创建 CheckRule Panel，并保存对应的规则配置上下文。
        /// </summary>
        internal void Open(
            ConfigWindowDialogHost host,
            ConfigWindowData data,
            ScoreRuleConfig ruleConfig,
            string defaultSaveFolder,
            Action onCreated)
        {
            configData = data;
            targetRule = ruleConfig;
            created = onCreated;

            SetData(
                data.BuildCheckerClassName(ruleConfig.RuleId),
                defaultSaveFolder);

            host.Show(this);
        }

        public void SetData(
            string defaultClassName,
            string defaultSaveFolder)
        {
            classNameField.SetValueWithoutNotify(
                defaultClassName ?? string.Empty);

            saveFolderField.Value =
                defaultSaveFolder ?? string.Empty;

            UpdateConfirmState();

            schedule.Execute(classNameField.Focus)
                .ExecuteLater(0);
        }

        private void Confirm()
        {
            if (!IsFormValid())
            {
                return;
            }

            string className = classNameField.value.Trim();
            string saveFolder = saveFolderField.Value.Trim();

            string errorMessage;
            bool queued =
                configData.QueueCheckerScript(
                    targetRule,
                    className,
                    saveFolder,
                    out errorMessage);

            if (!queued)
            {
                EditorUtility.DisplayDialog(
                    "创建检测脚本",
                    errorMessage,
                    "确定");
                return;
            }

            Confirmed?.Invoke(className, saveFolder);
            created?.Invoke();
            Close();
        }

        private void UpdateConfirmState()
        {
            UIElementUtility.SetEnabled(confirmButton, IsFormValid());
        }

        private bool IsFormValid()
        {
            return !string.IsNullOrWhiteSpace(
                       classNameField.value) &&
                   isSaveFolderValid(
                       saveFolderField.Value);
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
                    "选择检测脚本保存文件夹",
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
                    "创建检测脚本",
                    "请选择当前 Unity 项目内的目录。",
                    "确定");
                return;
            }

            SaveFolder = unityPath;
        }

        private void ClearContext()
        {
            configData = null;
            targetRule = null;
            created = null;
        }
    }
}
