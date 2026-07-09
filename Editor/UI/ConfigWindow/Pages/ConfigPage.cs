using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 四类配置列表页的公共实现。
    /// 具体页面只提供页面数据、摘要和字段内容。
    /// </summary>
    internal abstract class ConfigPage<TConfig> : IConfigPage
        where TConfig : ScriptableObject
    {
        private readonly ConfigWindowData configData;
        private readonly Label tabCountLabel;
        private readonly Button createButton;
        private readonly TextField searchField;
        private readonly VisualElement listContainer;
        private readonly Label emptyLabel;
        private readonly Func<ScriptableObject, int>
            getValidationErrorCount;
        private readonly Action<ScriptableObject>
            copyConfigToLocal;
        private readonly Action<ScriptableObject>
            deleteConfig;
        private readonly Action<
            VisualElement,
            ScriptableObject,
            Action> trackWorkingCopyChanges;
        private readonly Action<IConfigPage>
            createConfigRequested;

        private readonly HashSet<string> expandedConfigPaths =
            new HashSet<string>();

        private bool isBound;

        protected ConfigPage(
            VisualElement windowRoot,
            string pageElementName,
            string scrollViewName,
            string tabButtonName,
            string tabCountLabelName,
            string createButtonName,
            string searchFieldName,
            string listContainerName,
            string emptyLabelName,
            ConfigWindowData data,
            Func<ScriptableObject, int>
                validationErrorCountProvider,
            Action<ScriptableObject> copyAction,
            Action<ScriptableObject> deleteAction,
            Action<
                VisualElement,
                ScriptableObject,
                Action> changeTracker,
            Action<IConfigPage> createAction,
            VisualElement pageRoot = null)
        {
            configData = data;
            VisualElement contentRoot = pageRoot ?? windowRoot;
            Root = pageRoot ??
                   windowRoot.Q<VisualElement>(pageElementName);
            ScrollView =
                contentRoot.Q<ScrollView>(scrollViewName);
            TabButton =
                windowRoot.Q<Button>(tabButtonName);
            tabCountLabel =
                windowRoot.Q<Label>(tabCountLabelName);
            createButton =
                contentRoot.Q<Button>(createButtonName);
            searchField =
                contentRoot.Q<TextField>(searchFieldName);
            listContainer =
                contentRoot.Q<VisualElement>(listContainerName);
            emptyLabel =
                contentRoot.Q<Label>(emptyLabelName);
            getValidationErrorCount =
                validationErrorCountProvider;
            copyConfigToLocal = copyAction;
            deleteConfig = deleteAction;
            trackWorkingCopyChanges = changeTracker;
            createConfigRequested = createAction;
        }

        public abstract EConfigPage PageType { get; }

        public Type ConfigType => typeof(TConfig);

        public abstract string DisplayName { get; }

        public VisualElement Root { get; }

        public ScrollView ScrollView { get; }

        public Button TabButton { get; }

        public abstract ConfigPageCreateOptions CreateOptions
        {
            get;
        }

        public void Bind()
        {
            if (isBound)
            {
                return;
            }

            isBound = true;
            createButton.clicked +=
                delegate { createConfigRequested(this); };
            searchField.RegisterValueChangedCallback(
                delegate { Fresh(); });
        }

        public void Fresh()
        {
            BeforeFresh();

            List<TConfig> configs = LoadConfigs();
            UIElementUtility.SetText(tabCountLabel, configs.Count.ToString());
            BuildConfigList(configs);
        }

        public void SetVisible(bool visible)
        {
            UIElementUtility.SetVisible(Root, visible);
            UIElementUtility.SetClass(TabButton, "selected-tab", visible);
        }

        public void ExpandConfig(string assetPath)
        {
            if (!string.IsNullOrEmpty(assetPath))
            {
                expandedConfigPaths.Add(assetPath);
            }
        }

        public string GetConfigDisplayName(
            ScriptableObject config)
        {
            TConfig typedConfig = config as TConfig;

            return typedConfig == null
                ? config == null
                    ? string.Empty
                    : config.name
                : GetDisplayName(typedConfig);
        }

        public void BuildFields(
            ScriptableObject config,
            VisualElement container,
            bool editable)
        {
            TConfig typedConfig = config as TConfig;

            if (typedConfig != null)
            {
                BuildTypedFields(
                    typedConfig,
                    container,
                    editable);
            }
        }

        protected virtual void BeforeFresh()
        {
        }

        protected abstract string GetDisplayName(
            TConfig config);

        protected abstract string GetSummary(
            TConfig config);

        protected abstract void BuildTypedFields(
            TConfig config,
            VisualElement container,
            bool editable);

        private List<TConfig> LoadConfigs()
        {
            List<TConfig> configs = new List<TConfig>();
            List<ScriptableObject> loadedConfigs =
                configData.LoadConfigsByType(typeof(TConfig));

            for (int i = 0;
                 i < loadedConfigs.Count;
                 i++)
            {
                TConfig config =
                    loadedConfigs[i] as TConfig;

                if (config != null)
                {
                    configs.Add(config);
                }
            }

            configs.Sort(
                delegate(TConfig left, TConfig right)
                {
                    TConfig leftDisplay =
                        configData.GetWorkingConfig(left);
                    TConfig rightDisplay =
                        configData.GetWorkingConfig(right);

                    return string.Compare(
                        GetDisplayName(leftDisplay),
                        GetDisplayName(rightDisplay),
                        StringComparison.OrdinalIgnoreCase);
                });

            return configs;
        }

        private void BuildConfigList(
            List<TConfig> configs)
        {
            listContainer.Clear();

            string keyword =
                string.IsNullOrEmpty(searchField.value)
                    ? string.Empty
                    : searchField.value.Trim();

            int visibleCount = 0;

            for (int i = 0; i < configs.Count; i++)
            {
                TConfig sourceConfig = configs[i];
                TConfig workingConfig =
                    configData.GetWorkingConfig(sourceConfig);

                if (!IsSearchMatched(
                        workingConfig,
                        keyword))
                {
                    continue;
                }

                string assetPath =
                    configData.GetConfigAssetPath(sourceConfig);
                bool expanded =
                    expandedConfigPaths.Contains(assetPath) ||
                    (visibleCount == 0 &&
                     expandedConfigPaths.Count == 0);

                listContainer.Add(
                    BuildConfigCard(
                        sourceConfig,
                        expanded));
                visibleCount++;
            }

            UIElementUtility.SetVisible(emptyLabel, visibleCount == 0);
        }

        private VisualElement BuildConfigCard(
            TConfig sourceConfig,
            bool expanded)
        {
            TConfig config =
                configData.GetWorkingConfig(sourceConfig);
            string assetPath =
                configData.GetConfigAssetPath(sourceConfig);
            bool isPackage =
                ConfigWindowData.IsPackageAsset(assetPath);

            VisualElement card = new VisualElement();
            card.AddToClassList("config-card");

            VisualElement header = new VisualElement();
            header.AddToClassList("config-card-header");

            Label arrowLabel =
                new Label(expanded ? "▼" : "▶");
            arrowLabel.AddToClassList("fold-arrow");
            header.Add(arrowLabel);

            Label iconLabel = new Label("SO");
            iconLabel.AddToClassList("so-icon");
            header.Add(iconLabel);

            VisualElement titleContainer =
                new VisualElement();
            titleContainer.AddToClassList(
                "config-title-container");

            Label displayNameLabel =
                new Label(GetDisplayName(config));
            displayNameLabel.AddToClassList("display-name");
            titleContainer.Add(displayNameLabel);

            Label assetNameLabel =
                new Label(sourceConfig.name);
            assetNameLabel.AddToClassList("asset-name");
            titleContainer.Add(assetNameLabel);
            header.Add(titleContainer);

            Label summaryLabel =
                new Label(GetSummary(config));
            summaryLabel.AddToClassList("config-summary");
            header.Add(summaryLabel);

            int validationErrorCount =
                getValidationErrorCount(config);

            if (validationErrorCount > 0)
            {
                Label errorLabel = new Label(
                    "必填缺失 " + validationErrorCount);
                errorLabel.AddToClassList(
                    "validation-error-badge");
                header.Add(errorLabel);
            }

            Label sourceLabel =
                new Label(isPackage ? "包内" : "本地");
            sourceLabel.AddToClassList(
                isPackage
                    ? "package-badge"
                    : "local-badge");
            header.Add(sourceLabel);

            if (isPackage)
            {
                Button copyButton = new Button(
                    delegate
                    {
                        copyConfigToLocal(sourceConfig);
                    });
                copyButton.text = "复制到本地";
                copyButton.AddToClassList("small-button");
                StopClickPropagation(copyButton);
                header.Add(copyButton);
            }

            Button pingButton = new Button(
                delegate
                {
                    Selection.activeObject = sourceConfig;
                    EditorGUIUtility.PingObject(sourceConfig);
                });
            pingButton.text = "定位";
            pingButton.AddToClassList("small-button");
            StopClickPropagation(pingButton);
            header.Add(pingButton);

            if (!isPackage)
            {
                Button deleteButton = new Button(
                    delegate { deleteConfig(sourceConfig); });
                deleteButton.text = "删除";
                deleteButton.AddToClassList("small-button");
                deleteButton.AddToClassList("danger-button");
                StopClickPropagation(deleteButton);
                header.Add(deleteButton);
            }

            card.Add(header);

            VisualElement body = new VisualElement();
            body.AddToClassList("config-card-body");
            UIElementUtility.SetVisible(body, expanded);

            BuildTypedFields(config, body, !isPackage);

            trackWorkingCopyChanges(
                body,
                config,
                delegate
                {
                    displayNameLabel.text =
                        GetDisplayName(config);
                    summaryLabel.text = GetSummary(config);
                });

            card.Add(body);

            header.RegisterCallback<ClickEvent>(
                delegate
                {
                    bool willExpand = !UIElementUtility.IsVisible(body);
                    UIElementUtility.SetVisible(body, willExpand);
                    arrowLabel.text = willExpand ? "▼" : "▶";

                    if (willExpand)
                    {
                        expandedConfigPaths.Add(assetPath);
                    }
                    else
                    {
                        expandedConfigPaths.Remove(assetPath);
                    }
                });

            return card;
        }

        private bool IsSearchMatched(
            TConfig config,
            string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return true;
            }

            string text = GetSearchText(config);

            return text.IndexOf(
                keyword,
                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 构建当前配置卡片可搜索的文本；派生页面可追加嵌套配置内容。
        /// </summary>
        protected virtual string GetSearchText(
            TConfig config)
        {
            return GetDisplayName(config) +
                   "\n" +
                   config.name +
                   "\n" +
                   GetSummary(config);
        }

        private void StopClickPropagation(
            VisualElement element)
        {
            element.RegisterCallback<ClickEvent>(
                delegate(ClickEvent evt)
                {
                    evt.StopPropagation();
                });
        }
    }
}
