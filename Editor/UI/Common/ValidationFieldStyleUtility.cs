using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 提供配置页面验证字段的公共样式重置与应用能力。
    /// </summary>
    internal static class ValidationFieldStyleUtility
    {
        /// <summary>
        /// 移除页面中已应用的验证错误与警告样式。
        /// </summary>
        public static void Clear(VisualElement root)
        {
            root.Query<VisualElement>().ForEach(
                element =>
                {
                    element.RemoveFromClassList("validation-error-field");
                    element.RemoveFromClassList("validation-warning-field");
                });
        }

        /// <summary>
        /// 将字段标记为错误或警告。
        /// </summary>
        public static void Apply(VisualElement field, bool isWarning)
        {
            field.AddToClassList(
                isWarning
                    ? "validation-warning-field"
                    : "validation-error-field");
        }
    }
}
