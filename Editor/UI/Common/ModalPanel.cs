using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 在 EditorWindow 内显示的通用模态面板。
    /// 本类只管理遮罩、标题、内容容器和关闭行为，具体表单由派生类提供。
    /// </summary>
    public class ModalPanel : VisualElement
    {
        private readonly Label titleLabel;
        private readonly VisualElement panelContent;

        public event Action Closed;

        public override VisualElement contentContainer => panelContent;

        public bool IsOpen => parent != null;

        public string Title
        {
            get => titleLabel.text;
            set => titleLabel.text = value ?? string.Empty;
        }

        public ModalPanel()
        {
            AddToClassList("modal-panel");
            focusable = true;

            VisualElement surface = new VisualElement();
            surface.AddToClassList("modal-panel-surface");

            VisualElement header = new VisualElement();
            header.AddToClassList("modal-panel-header");

            titleLabel = new Label();
            titleLabel.AddToClassList("modal-panel-title");

            Button closeButton = new Button(Close)
            {
                text = "关闭",
                tooltip = "关闭"
            };

            closeButton.AddToClassList("modal-panel-close-button");

            panelContent = new VisualElement();
            panelContent.AddToClassList("modal-panel-content");

            header.Add(titleLabel);
            header.Add(closeButton);
            surface.Add(header);
            surface.Add(panelContent);

            // 使用 hierarchy 避开 contentContainer 重定向，将面板外壳添加到遮罩根节点。
            hierarchy.Add(surface);

            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        /// <summary>
        /// 将面板添加到指定容器并提升到当前视觉层级的最上方。
        /// </summary>
        public void Show(VisualElement parentElement)
        {
            if (parentElement == null)
            {
                throw new ArgumentNullException(nameof(parentElement));
            }

            if (parent != parentElement)
            {
                RemoveFromHierarchy();
                parentElement.Add(this);
            }

            UIElementUtility.SetVisible(this, true);
            BringToFront();
            schedule.Execute(Focus).ExecuteLater(0);
        }

        /// <summary>
        /// 从当前 UI 层级移除面板，并通知调用方清理本次弹窗状态。
        /// </summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            RemoveFromHierarchy();
            Closed?.Invoke();
        }

        /// <summary>
        /// 面板获得键盘焦点时，允许用户使用 Escape 关闭当前弹窗。
        /// </summary>
        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape || !IsOpen)
            {
                return;
            }

            Close();
            evt.StopPropagation();
        }
    }
}
