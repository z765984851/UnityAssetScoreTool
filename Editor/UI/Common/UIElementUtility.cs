using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 提供配置界面和扫描界面共用的基础 UI 状态操作。
    /// </summary>
    internal static class UIElementUtility
    {
        /// <summary>
        /// 设置元素的显示状态。
        /// </summary>
        public static void SetVisible(VisualElement element, bool visible)
        {
            if (element == null)
            {
                return;
            }

            element.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        /// <summary>
        /// 判断元素当前是否处于可见状态。
        /// </summary>
        public static bool IsVisible(VisualElement element)
        {
            return element != null &&
                   element.resolvedStyle.display != DisplayStyle.None;
        }

        /// <summary>
        /// 设置文本控件的显示文本。
        /// </summary>
        public static void SetText(TextElement textElement, string text)
        {
            if (textElement != null)
            {
                textElement.text = text ?? string.Empty;
            }
        }

        /// <summary>
        /// 设置控件是否可用。
        /// </summary>
        public static void SetEnabled(VisualElement element, bool enabled)
        {
            if (element != null)
            {
                element.SetEnabled(enabled);
            }
        }

        /// <summary>
        /// 根据状态切换元素上的样式 class。
        /// </summary>
        public static void SetClass(VisualElement element, string className, bool enabled)
        {
            if (element == null || string.IsNullOrEmpty(className))
            {
                return;
            }

            if (enabled)
            {
                element.AddToClassList(className);
            }
            else
            {
                element.RemoveFromClassList(className);
            }
        }
    }
}
