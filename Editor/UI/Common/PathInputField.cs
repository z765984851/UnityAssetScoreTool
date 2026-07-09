using System;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 通用路径输入组件，负责显示字段标题、可编辑路径、选择按钮和提示信息。
    /// 文件或文件夹选择及路径校验由调用方处理。
    /// </summary>
    public sealed class PathInputField : VisualElement
    {
        private readonly Label fieldLabel;
        private readonly TextField pathField;
        private readonly Button browseButton;
        private readonly Label hintLabel;

        public event Action<string> ValueChanged;

        public event Action BrowseRequested;

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

        public string Value
        {
            get => pathField.value;
            set => pathField.SetValueWithoutNotify(
                value ?? string.Empty);
        }

        public string BrowseButtonText
        {
            get => browseButton.text;
            set => browseButton.text = value ?? string.Empty;
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

        public PathInputField(
            VisualTreeAsset visualTreeAsset)
        {
            if (visualTreeAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(visualTreeAsset));
            }

            // UXML 负责固定视觉层级，本类只绑定控件并转发用户交互。
            TemplateContainer content =
                visualTreeAsset.Instantiate();

            Add(content);

            fieldLabel =
                content.Q<Label>("PathInputLabel");

            pathField =
                content.Q<TextField>(
                    "PathInputTextField");

            browseButton =
                content.Q<Button>(
                    "PathInputBrowseButton");

            hintLabel =
                content.Q<Label>("PathInputHint");

            // 组件只转发用户输入，路径标准化和有效性判断由数据层统一处理。
            pathField.RegisterValueChangedCallback(evt => ValueChanged?.Invoke(evt.newValue));

            browseButton.clicked +=
                () => BrowseRequested?.Invoke();

            LabelText = "路径";
            BrowseButtonText = "选择文件夹";
            HintText = string.Empty;
        }
    }
}
