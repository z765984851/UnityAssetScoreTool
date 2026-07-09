using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 负责配置窗口模态面板的统一挂载与显示，关闭行为由 ModalPanel 维护。
    /// </summary>
    internal sealed class ConfigWindowDialogHost
    {
        private readonly VisualElement host;

        /// <summary>
        /// 绑定配置窗口中已定义的弹窗挂载节点。
        /// </summary>
        public ConfigWindowDialogHost(VisualElement windowRoot)
        {
            host = windowRoot.Q<VisualElement>("DialogHost");
            host.pickingMode = PickingMode.Ignore;
        }

        /// <summary>
        /// 将模态面板挂载到统一节点并提升到最上层。
        /// </summary>
        public void Show(ModalPanel panel)
        {
            panel.Show(host);
        }

    }
}
