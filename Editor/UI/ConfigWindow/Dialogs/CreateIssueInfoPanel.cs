using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 新增问题项表单 Panel。
    /// 本类负责字段显示、默认值和必填校验，不负责修改规则配置。
    /// </summary>
    public sealed class CreateIssueInfoPanel : ModalPanel
    {
        private readonly TextField issueIdField;
        private readonly TextField displayNameField;
        private readonly IntegerField deductScoreField;
        private readonly DropdownField issueLevelField;
        private readonly TextField issueMessageField;
        private readonly Button cancelButton;
        private readonly Button confirmButton;
        private ConfigWindowData configData;
        private ScoreRuleConfig owner;
        private string arrayPath;
        private Action committed;

        public event Action<string, string, int, int, string> Confirmed;

        public event Action CancelRequested;

        public CreateIssueInfoPanel(
            VisualTreeAsset visualTreeAsset)
        {
            if (visualTreeAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(visualTreeAsset));
            }

            Title = "添加问题项";
            AddToClassList("item-create-dialog-panel");

            TemplateContainer content =
                visualTreeAsset.Instantiate();

            Add(content);

            issueIdField =
                content.Q<TextField>(
                    "CreateIssueInfoIdField");

            ConfigIdNamingUtility.RestrictInput(issueIdField);

            displayNameField =
                content.Q<TextField>(
                    "CreateIssueInfoDisplayNameField");

            deductScoreField =
                content.Q<IntegerField>(
                    "CreateIssueInfoDeductScoreField");

            issueLevelField =
                content.Q<DropdownField>(
                    "CreateIssueInfoLevelField");

            issueMessageField =
                content.Q<TextField>(
                    "CreateIssueInfoMessageField");

            cancelButton =
                content.Q<Button>(
                    "CreateIssueInfoCancelButton");

            confirmButton =
                content.Q<Button>(
                    "CreateIssueInfoConfirmButton");

            issueLevelField.choices =
                new List<string>(
                    Enum.GetNames(typeof(EIssueLevel)));

            issueIdField.RegisterValueChangedCallback(
                delegate { UpdateConfirmState(); });

            displayNameField.RegisterValueChangedCallback(
                delegate { UpdateConfirmState(); });

            issueMessageField.RegisterValueChangedCallback(
                delegate { UpdateConfirmState(); });

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
        /// 打开新增问题项 Panel，并保存本次编辑所需的数据上下文。
        /// </summary>
        internal void Open(
            ConfigWindowDialogHost host,
            ConfigWindowData data,
            ScoreRuleConfig target,
            string targetArrayPath,
            Action onCommitted)
        {
            configData = data;
            owner = target;
            arrayPath = targetArrayPath;
            committed = onCommitted;

            ResetForm();
            host.Show(this);
        }

        public void ResetForm()
        {
            issueIdField.SetValueWithoutNotify(
                string.Empty);
            displayNameField.SetValueWithoutNotify(
                string.Empty);
            deductScoreField.SetValueWithoutNotify(0);
            issueLevelField.index =
                (int)EIssueLevel.Warning;
            issueMessageField.SetValueWithoutNotify(
                string.Empty);

            UpdateConfirmState();

            schedule.Execute(issueIdField.Focus)
                .ExecuteLater(0);
        }

        private void Confirm()
        {
            if (!IsFormValid())
            {
                return;
            }

            string issueInfoId = issueIdField.value.Trim();
            string displayName = displayNameField.value.Trim();
            int deductScore = deductScoreField.value;
            int issueLevelIndex = issueLevelField.index;
            string issueMessage = issueMessageField.value.Trim();

            configData.AddIssueInfo(
                owner,
                arrayPath,
                issueInfoId,
                displayName,
                deductScore,
                issueLevelIndex,
                issueMessage);

            Confirmed?.Invoke(
                issueInfoId,
                displayName,
                deductScore,
                issueLevelIndex,
                issueMessage);

            committed?.Invoke();
            Close();
        }

        private void UpdateConfirmState()
        {
            UIElementUtility.SetEnabled(confirmButton, IsFormValid());
        }

        private bool IsFormValid()
        {
            return ConfigIdNamingUtility.IsValid(
                       issueIdField.value) &&
                   !string.IsNullOrWhiteSpace(
                       displayNameField.value) &&
                   !string.IsNullOrWhiteSpace(
                       issueMessageField.value);
        }

        private void ClearContext()
        {
            configData = null;
            owner = null;
            arrayPath = string.Empty;
            committed = null;
        }
    }
}
