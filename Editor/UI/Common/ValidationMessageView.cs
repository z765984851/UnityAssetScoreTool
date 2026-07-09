using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 顶部诊断消息的展示级别。
    /// </summary>
    public enum EValidationMessageSeverity
    {
        Error,
        Warning
    }

    /// <summary>
    /// 单条校验错误的显示数据，不包含配置对象或定位逻辑。
    /// </summary>
    public sealed class ValidationMessageData
    {
        /// <summary>
        /// 创建一条带有配置、字段、消息和严重度信息的诊断展示数据。
        /// </summary>
        public ValidationMessageData(
            string configName,
            string fieldName,
            string message,
            EValidationMessageSeverity severity =
                EValidationMessageSeverity.Error)
        {
            ConfigName = configName ?? string.Empty;
            FieldName = fieldName ?? string.Empty;
            Message = message ?? string.Empty;
            Severity = severity;
        }

        public string ConfigName { get; }

        public string FieldName { get; }

        public string Message { get; }

        public EValidationMessageSeverity Severity { get; }
    }

    /// <summary>
    /// 统一校验错误区域，负责显示错误列表并发送定位请求。
    /// 校验规则、页签切换和字段定位由调用方处理。
    /// </summary>
    public sealed class ValidationMessageView : VisualElement
    {
        private readonly VisualElement messageRoot;
        private readonly Label titleLabel;
        private readonly Label countLabel;
        private readonly VisualElement messageList;

        public event Action<int> LocateRequested;

        public string Title
        {
            get => titleLabel.text;
            set => titleLabel.text = value ?? string.Empty;
        }

        /// <summary>
        /// 实例化错误区域模板；运行时默认隐藏，收到错误数据后再显示。
        /// </summary>
        public ValidationMessageView(
            VisualTreeAsset visualTreeAsset)
        {
            if (visualTreeAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(visualTreeAsset));
            }

            TemplateContainer content =
                visualTreeAsset.Instantiate();

            Add(content);

            messageRoot =
                content.Q<VisualElement>(
                    "ValidationMessageRoot");

            titleLabel =
                content.Q<Label>(
                    "ValidationMessageTitle");

            countLabel =
                content.Q<Label>(
                    "ValidationMessageCount");

            messageList =
                content.Q<VisualElement>(
                    "ValidationMessageList");

            UIElementUtility.SetVisible(messageRoot, false);
        }

        /// <summary>
        /// 使用最新错误集合重建显示列表；集合为空时隐藏整个错误区域。
        /// </summary>
        public void SetMessages(
            IReadOnlyList<ValidationMessageData> messages)
        {
            messageList.Clear();

            if (messages == null || messages.Count == 0)
            {
                countLabel.text = string.Empty;
                messageRoot.RemoveFromClassList(
                    "validation-message-view-error");
                messageRoot.RemoveFromClassList(
                    "validation-message-view-warning");
                UIElementUtility.SetVisible(messageRoot, false);
                return;
            }

            int visibleCount = 0;
            bool hasError = false;
            bool hasWarning = false;

            for (int i = 0; i < messages.Count; i++)
            {
                ValidationMessageData message = messages[i];

                if (message == null)
                {
                    continue;
                }

                messageList.Add(
                    BuildMessageRow(
                        message,
                        i,
                        visibleCount == 0));

                hasError |=
                    message.Severity ==
                    EValidationMessageSeverity.Error;
                hasWarning |=
                    message.Severity ==
                    EValidationMessageSeverity.Warning;

                visibleCount++;
            }

            countLabel.text = visibleCount.ToString();
            messageRoot.RemoveFromClassList(
                "validation-message-view-error");
            messageRoot.RemoveFromClassList(
                "validation-message-view-warning");
            messageRoot.AddToClassList(
                hasError
                    ? "validation-message-view-error"
                    : hasWarning
                        ? "validation-message-view-warning"
                        : "validation-message-view-error");
            UIElementUtility.SetVisible(messageRoot, visibleCount > 0);
        }

        /// <summary>
        /// 创建单条错误信息，并保留原集合索引供调用方执行定位。
        /// </summary>
        private VisualElement BuildMessageRow(
            ValidationMessageData message,
            int messageIndex,
            bool isFirst)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("validation-message-row");
            row.AddToClassList(
                message.Severity ==
                    EValidationMessageSeverity.Warning
                    ? "validation-message-row-warning"
                    : "validation-message-row-error");

            if (isFirst)
            {
                row.AddToClassList(
                    "validation-message-row-first");
            }

            VisualElement textContainer =
                new VisualElement();

            textContainer.AddToClassList(
                "validation-message-content");

            string heading = message.ConfigName;

            if (!string.IsNullOrEmpty(message.FieldName))
            {
                heading = string.IsNullOrEmpty(heading)
                    ? message.FieldName
                    : heading + " · " + message.FieldName;
            }

            Label headingLabel = new Label(heading);
            headingLabel.AddToClassList(
                "validation-message-name");

            Label detailLabel =
                new Label(message.Message);

            detailLabel.AddToClassList(
                "validation-message-detail");

            textContainer.Add(headingLabel);
            textContainer.Add(detailLabel);

            Button locateButton = new Button(() => LocateRequested?.Invoke(messageIndex));

            locateButton.text = "定位";
            locateButton.AddToClassList(
                "validation-message-locate-button");

            row.Add(textContainer);
            row.Add(locateButton);

            return row;
        }
    }
}
