using System;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 通用折叠卡片，负责标题栏、状态显示、展开状态和动态内容挂载。
    /// 页面负责提供具体操作按钮和业务字段。
    /// </summary>
    public sealed class FoldoutCardView : VisualElement
    {
        public const string StatusActiveClass =
            "foldout-card-status-active";

        public const string StatusSuccessClass =
            "foldout-card-status-success";

        public const string StatusWarningClass =
            "foldout-card-status-warning";

        public const string StatusErrorClass =
            "foldout-card-status-error";

        private readonly VisualElement cardRoot;
        private readonly Button foldButton;
        private readonly Label iconLabel;
        private readonly Label titleLabel;
        private readonly Label subtitleLabel;
        private readonly Label statusLabel;
        private readonly VisualElement body;

        private bool isExpanded;
        private string statusClass;

        public event Action<bool> ExpandedChanged;

        public VisualElement HeaderActionContainer { get; }

        public VisualElement ContentContainer { get; }

        public string Title
        {
            get => titleLabel.text;
            set => titleLabel.text = value ?? string.Empty;
        }

        public string Subtitle
        {
            get => subtitleLabel.text;
            set
            {
                subtitleLabel.text = value ?? string.Empty;
                UIElementUtility.SetVisible(
                    subtitleLabel,
                    !string.IsNullOrEmpty(subtitleLabel.text));
            }
        }

        public string IconText
        {
            get => iconLabel.text;
            set
            {
                iconLabel.text = value ?? string.Empty;

                bool visible =
                    !string.IsNullOrEmpty(iconLabel.text);

                UIElementUtility.SetVisible(iconLabel, visible);

                if (visible)
                {
                    cardRoot.AddToClassList(
                        "foldout-card-with-icon");
                }
                else
                {
                    cardRoot.RemoveFromClassList(
                        "foldout-card-with-icon");
                }
            }
        }

        public string StatusText
        {
            get => statusLabel.text;
            set
            {
                statusLabel.text = value ?? string.Empty;
                UIElementUtility.SetVisible(
                    statusLabel,
                    !string.IsNullOrEmpty(statusLabel.text));
            }
        }

        public string StatusClass
        {
            get => statusClass;
            set
            {
                if (!string.IsNullOrEmpty(statusClass))
                {
                    cardRoot.RemoveFromClassList(statusClass);
                    statusLabel.RemoveFromClassList(statusClass);
                }

                statusClass = value ?? string.Empty;

                if (!string.IsNullOrEmpty(statusClass))
                {
                    cardRoot.AddToClassList(statusClass);
                    statusLabel.AddToClassList(statusClass);
                }
            }
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set => SetExpanded(value, false);
        }

        public bool IsMuted
        {
            get => cardRoot.ClassListContains(
                "foldout-card-muted");
            set
            {
                if (value)
                {
                    cardRoot.AddToClassList(
                        "foldout-card-muted");
                }
                else
                {
                    cardRoot.RemoveFromClassList(
                        "foldout-card-muted");
                }
            }
        }

        public FoldoutCardView(
            VisualTreeAsset visualTreeAsset)
        {
            if (visualTreeAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(visualTreeAsset));
            }

            // UXML 负责固定视觉层级，本类只管理显示状态和交互事件。
            TemplateContainer content =
                visualTreeAsset.Instantiate();

            Add(content);

            cardRoot =
                content.Q<VisualElement>(
                    "FoldoutCardRoot");

            VisualElement header =
                content.Q<VisualElement>(
                    "FoldoutCardHeader");

            foldButton =
                content.Q<Button>("FoldoutCardButton");

            iconLabel =
                content.Q<Label>("FoldoutCardIcon");

            titleLabel =
                content.Q<Label>("FoldoutCardTitle");

            subtitleLabel =
                content.Q<Label>("FoldoutCardSubtitle");

            statusLabel =
                content.Q<Label>("FoldoutCardStatus");

            HeaderActionContainer =
                content.Q<VisualElement>(
                    "FoldoutCardHeaderActions");

            body =
                content.Q<VisualElement>("FoldoutCardBody");

            ContentContainer =
                content.Q<VisualElement>(
                    "FoldoutCardContentContainer");

            foldButton.RegisterCallback<ClickEvent>(
                OnFoldButtonClicked);

            header.RegisterCallback<ClickEvent>(
                OnHeaderClicked);

            HeaderActionContainer.RegisterCallback<ClickEvent>(
                OnHeaderActionClicked);

            Subtitle = string.Empty;
            IconText = string.Empty;
            StatusText = string.Empty;
            SetExpanded(false, false);
        }

        /// <summary>
        /// 更新内容区和箭头状态；用户交互时额外通知所属页面保存展开状态。
        /// </summary>
        private void SetExpanded(
            bool expanded,
            bool notify)
        {
            bool changed = isExpanded != expanded;
            isExpanded = expanded;

            UIElementUtility.SetVisible(body, isExpanded);

            foldButton.text = isExpanded ? "▼" : "▶";

            if (notify && changed)
            {
                ExpandedChanged?.Invoke(isExpanded);
            }
        }

        /// <summary>
        /// 折叠按钮单独处理点击并停止冒泡，避免标题栏重复切换状态。
        /// </summary>
        private void OnFoldButtonClicked(
            ClickEvent evt)
        {
            SetExpanded(!isExpanded, true);
            evt.StopPropagation();
        }

        /// <summary>
        /// 点击标题栏空白区域时切换卡片展开状态。
        /// </summary>
        private void OnHeaderClicked(
            ClickEvent evt)
        {
            SetExpanded(!isExpanded, true);
        }

        /// <summary>
        /// 阻止操作按钮的点击继续冒泡到标题栏。
        /// </summary>
        private void OnHeaderActionClicked(
            ClickEvent evt)
        {
            evt.StopPropagation();
        }
    }
}
