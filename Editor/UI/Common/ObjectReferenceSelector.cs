using System;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 通用对象引用选择组件，负责显示引用信息并转发选择、清除、定位和编辑操作。
    /// 引用查询、序列化修改和资源操作由所属页面处理。
    /// </summary>
    public sealed class ObjectReferenceSelector : VisualElement
    {
        private readonly Label fieldLabel;
        private readonly Label iconLabel;
        private readonly Label displayNameLabel;
        private readonly Label metadataLabel;
        private readonly Label hintLabel;
        private readonly Button selectButton;
        private readonly Button clearButton;
        private readonly Button pingButton;
        private readonly Button editButton;

        public event Action SelectRequested;

        public event Action ClearRequested;

        public event Action PingRequested;

        public event Action EditRequested;

        public string LabelText
        {
            get => fieldLabel.text;
            set
            {
                fieldLabel.text = value ?? string.Empty;
                UIElementUtility.SetVisible(
                    fieldLabel,
                    !string.IsNullOrEmpty(fieldLabel.text));
            }
        }

        public string IconText
        {
            get => iconLabel.text;
            set
            {
                iconLabel.text = value ?? string.Empty;
                UIElementUtility.SetVisible(
                    iconLabel,
                    !string.IsNullOrEmpty(iconLabel.text));
            }
        }

        public string DisplayName
        {
            get => displayNameLabel.text;
            set
            {
                displayNameLabel.text = value ?? string.Empty;
                UIElementUtility.SetVisible(
                    displayNameLabel,
                    !string.IsNullOrEmpty(displayNameLabel.text));
            }
        }

        public string Metadata
        {
            get => metadataLabel.text;
            set
            {
                metadataLabel.text = value ?? string.Empty;
                UIElementUtility.SetVisible(
                    metadataLabel,
                    !string.IsNullOrEmpty(metadataLabel.text));
            }
        }

        public string HintText
        {
            get => hintLabel.text;
            set
            {
                hintLabel.text = value ?? string.Empty;
                UIElementUtility.SetVisible(
                    hintLabel,
                    !string.IsNullOrEmpty(hintLabel.text));
            }
        }

        public bool SelectButtonVisible
        {
            get => selectButton.resolvedStyle.display !=
                   DisplayStyle.None;
            set => UIElementUtility.SetVisible(selectButton, value);
        }

        public bool ClearButtonVisible
        {
            get => clearButton.resolvedStyle.display !=
                   DisplayStyle.None;
            set => UIElementUtility.SetVisible(clearButton, value);
        }

        public bool PingButtonVisible
        {
            get => pingButton.resolvedStyle.display !=
                   DisplayStyle.None;
            set => UIElementUtility.SetVisible(pingButton, value);
        }

        public bool EditButtonVisible
        {
            get => editButton.resolvedStyle.display !=
                   DisplayStyle.None;
            set => UIElementUtility.SetVisible(editButton, value);
        }

        public bool SelectButtonEnabled
        {
            get => selectButton.enabledSelf;
            set => selectButton.SetEnabled(value);
        }

        public bool ClearButtonEnabled
        {
            get => clearButton.enabledSelf;
            set => clearButton.SetEnabled(value);
        }

        public bool PingButtonEnabled
        {
            get => pingButton.enabledSelf;
            set => pingButton.SetEnabled(value);
        }

        public bool EditButtonEnabled
        {
            get => editButton.enabledSelf;
            set => editButton.SetEnabled(value);
        }

        public string SelectButtonText
        {
            get => selectButton.text;
            set => selectButton.text = value ?? string.Empty;
        }

        public string ClearButtonText
        {
            get => clearButton.text;
            set => clearButton.text = value ?? string.Empty;
        }

        /// <summary>
        /// 实例化固定模板并绑定引用操作按钮。
        /// </summary>
        public ObjectReferenceSelector(
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

            fieldLabel =
                content.Q<Label>(
                    "ObjectReferenceFieldLabel");

            iconLabel =
                content.Q<Label>("ObjectReferenceIcon");

            displayNameLabel =
                content.Q<Label>(
                    "ObjectReferenceDisplayName");

            metadataLabel =
                content.Q<Label>(
                    "ObjectReferenceMetadata");

            hintLabel =
                content.Q<Label>("ObjectReferenceHint");

            selectButton =
                content.Q<Button>(
                    "ObjectReferenceSelectButton");

            clearButton =
                content.Q<Button>(
                    "ObjectReferenceClearButton");

            pingButton =
                content.Q<Button>(
                    "ObjectReferencePingButton");

            editButton =
                content.Q<Button>(
                    "ObjectReferenceEditButton");

            // 组件只发送操作意图，调用方决定具体资源和工作副本处理方式。
            selectButton.clicked +=
                () => SelectRequested?.Invoke();

            clearButton.clicked +=
                () => ClearRequested?.Invoke();

            pingButton.clicked +=
                () => PingRequested?.Invoke();

            editButton.clicked +=
                () => EditRequested?.Invoke();

            LabelText = string.Empty;
            IconText = "SO";
            DisplayName = string.Empty;
            Metadata = string.Empty;
            HintText = string.Empty;

            SelectButtonVisible = true;
            ClearButtonVisible = false;
            PingButtonVisible = false;
            EditButtonVisible = false;
        }
    }
}
