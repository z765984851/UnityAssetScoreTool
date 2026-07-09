using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 负责将配置页面模板实例挂载到窗口页面容器。
    /// </summary>
    internal sealed class ConfigWindowPageHost
    {
        private readonly VisualElement pageContainer;
        private readonly Dictionary<EConfigPage, IConfigPage> pages =
            new Dictionary<EConfigPage, IConfigPage>();

        public IConfigPage CurrentPage { get; private set; }

        /// <summary>
        /// 绑定主 UXML 中已定义的页面容器。
        /// </summary>
        public ConfigWindowPageHost(VisualElement windowRoot)
        {
            pageContainer = windowRoot.Q<VisualElement>("PageContainer");
        }

        /// <summary>
        /// 在指定位置实例化并插入一个页面模板。
        /// </summary>
        public VisualElement InsertPage(
            VisualTreeAsset pageTemplate,
            int index)
        {
            if (pageTemplate == null)
            {
                return null;
            }

            VisualElement pageRoot = pageTemplate.Instantiate();
            pageRoot.style.flexGrow = new StyleFloat(1);
            pageContainer.Insert(index, pageRoot);
            return pageRoot;
        }

        /// <summary>
        /// 在页面容器末尾实例化并添加一个页面模板。
        /// </summary>
        public VisualElement AddPage(VisualTreeAsset pageTemplate)
        {
            if (pageTemplate == null)
            {
                return null;
            }

            VisualElement pageRoot = pageTemplate.Instantiate();
            pageRoot.style.flexGrow = new StyleFloat(1);
            pageContainer.Add(pageRoot);
            return pageRoot;
        }

        /// <summary>
        /// 清除上一轮页面注册状态，供窗口视觉树重建时重新绑定。
        /// </summary>
        public void ClearPages()
        {
            pages.Clear();
            CurrentPage = null;
        }

        /// <summary>
        /// 注册并绑定页面控件，页签点击继续交给窗口协调器。
        /// </summary>
        public void RegisterPage(
            IConfigPage page,
            Action<EConfigPage> pageSelected)
        {
            pages.Add(page.PageType, page);
            page.Bind();
            page.SetVisible(false);
            page.TabButton.clicked +=
                delegate { pageSelected(page.PageType); };
        }

        /// <summary>
        /// 尝试获取指定类型的已注册配置页面。
        /// </summary>
        public bool TryGetPage(
            EConfigPage pageType,
            out IConfigPage page)
        {
            return pages.TryGetValue(pageType, out page);
        }

        /// <summary>
        /// 根据配置资产类型查找负责展示它的页面。
        /// </summary>
        public IConfigPage GetPageByConfigType(Type configType)
        {
            if (configType == null)
            {
                return null;
            }

            foreach (IConfigPage page in pages.Values)
            {
                if (page.ConfigType == configType)
                {
                    return page;
                }
            }

            return null;
        }

        /// <summary>
        /// 隐藏当前页面并显示目标页面，保持可见状态只由页面宿主切换。
        /// </summary>
        public bool ActivatePage(EConfigPage pageType)
        {
            IConfigPage nextPage;

            if (!pages.TryGetValue(pageType, out nextPage))
            {
                return false;
            }

            CurrentPage?.SetVisible(false);
            nextPage.SetVisible(true);
            CurrentPage = nextPage;
            return true;
        }

        /// <summary>
        /// 刷新全部已注册页面，统一维护页面刷新遍历入口。
        /// </summary>
        public void RefreshAll()
        {
            foreach (IConfigPage page in pages.Values)
            {
                page.Fresh();
            }
        }
    }
}
