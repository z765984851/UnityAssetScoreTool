using System;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 配置页面共用的标题区域，负责显示页面说明、操作入口和搜索框。
    /// 具体按钮行为与搜索结果处理仍由所属页面负责。
    /// </summary>
    public sealed class PageHeaderView : VisualElement
    {
        private readonly Label titleLabel;
        private readonly Label descriptionLabel;
        private readonly TextField searchField;

        public event Action<string> SearchChanged;

        public VisualElement ActionContainer { get; }

        public string Title
        {
            get => titleLabel.text;
            set => titleLabel.text = value ?? string.Empty;
        }

        public string Description
        {
            get => descriptionLabel.text;
            set
            {
                descriptionLabel.text = value ?? string.Empty;
                UIElementUtility.SetVisible(
                    descriptionLabel,
                    !string.IsNullOrEmpty(descriptionLabel.text));
            }
        }

        public string SearchText
        {
            get => searchField.value;
            set => searchField.SetValueWithoutNotify(
                value ?? string.Empty);
        }

        public bool SearchVisible
        {
            get => searchField.resolvedStyle.display !=
                   DisplayStyle.None;
            set => UIElementUtility.SetVisible(searchField, value);
        }

        public PageHeaderView(
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

            titleLabel =
                content.Q<Label>("PageHeaderTitle");

            descriptionLabel =
                content.Q<Label>("PageHeaderDescription");

            ActionContainer =
                content.Q<VisualElement>(
                    "PageHeaderActionContainer");

            searchField =
                content.Q<TextField>(
                    "PageHeaderSearchField");

            searchField.RegisterValueChangedCallback(evt => SearchChanged?.Invoke(evt.newValue));

            Description = string.Empty;
        }
    }
}
