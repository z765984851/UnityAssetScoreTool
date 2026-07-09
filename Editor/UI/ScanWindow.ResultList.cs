using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    public partial class ScanWindow
    {
        private const int ResultRowHeight = 22;

        private const int ExpandColumnWidth = 24;
        private const int NameColumnWidth = 220;
        private const int TypeColumnWidth = 90;
        private const int ScoreColumnWidth = 50;
        private const int GradeColumnWidth = 50;
        private const int SeverityColumnWidth = 90;
        private const int IssueCountColumnWidth = 60;

        private const int TreeIndentWidth = 16;

        private const string AssetTypeFilterAllText = "全部类型";

        private const string SeverityFilterAllText = "全部严重度";
        private const string SeverityFilterNoneText = "无问题";

        private const string SortByPathText = "路径";
        private const string SortByScoreAscText = "分数低到高";
        private const string SortByScoreDescText = "分数高到低";
        private const string SortByIssueCountDescText = "问题数多到少";
        private const string SortBySeverityDescText = "严重度高到低";

        private static readonly Color ResultRowNormalBackgroundColor = new Color(0f, 0f, 0f, 0f);
        private static readonly Color ResultRowSelectedBackgroundColor = new Color(0.18f, 0.36f, 0.55f, 0.75f);

        /// <summary>
        /// 绑定左侧结果列表区域控件。
        /// </summary>
        private void BindResultListUI()
        {
            assetTypeFilterDropdown = rootVisualElement.Q<DropdownField>("AssetTypeFilterDropdown");
            severityFilterDropdown = rootVisualElement.Q<DropdownField>("SeverityFilterDropdown");
            sortDropdown = rootVisualElement.Q<DropdownField>("SortDropdown");

            resultScrollView = rootVisualElement.Q<ScrollView>("ResultScrollView");

            InitResultFilterDropdowns();
            SetResultToolsEnabled(false);

            assetTypeFilterDropdown.RegisterValueChangedCallback(_ => { RefreshResultList(); });

            severityFilterDropdown.RegisterValueChangedCallback(_ => { RefreshResultList(); });

            sortDropdown.RegisterValueChangedCallback(_ => { RefreshResultList(); });

            SetupResultHeaderLayout();
        }

        /// <summary>
        /// 初始化结果筛选和排序下拉框。
        /// </summary>
        private void InitResultFilterDropdowns()
        {
            assetTypeFilterDropdown.choices = new List<string>
            {
                AssetTypeFilterAllText
            };
            assetTypeFilterDropdown.SetValueWithoutNotify(AssetTypeFilterAllText);

            severityFilterDropdown.choices = new List<string>
            {
                SeverityFilterAllText,
                SeverityFilterNoneText,
                EIssueLevel.Info.ToString(),
                EIssueLevel.Warning.ToString(),
                EIssueLevel.Error.ToString(),
                EIssueLevel.Blocker.ToString()
            };
            severityFilterDropdown.SetValueWithoutNotify(SeverityFilterAllText);

            sortDropdown.choices = new List<string>
            {
                SortByPathText,
                SortByScoreAscText,
                SortByScoreDescText,
                SortByIssueCountDescText,
                SortBySeverityDescText
            };
            sortDropdown.SetValueWithoutNotify(SortByPathText);
        }

        /// <summary>
        /// 设置结果筛选和排序控件是否可用。
        /// </summary>
        private void SetResultToolsEnabled(bool enabled)
        {
            assetTypeFilterDropdown.SetEnabled(enabled);
            severityFilterDropdown.SetEnabled(enabled);
            sortDropdown.SetEnabled(enabled);
        }

        /// <summary>
        /// 统一设置结果表头列宽，保证表头和动态生成的结果行对齐。
        /// </summary>
        private void SetupResultHeaderLayout()
        {
            VisualElement headerRow = rootVisualElement.Q<VisualElement>("ResultHeaderRow");

            if (headerRow != null)
            {
                headerRow.style.flexDirection = FlexDirection.Row;
                headerRow.style.alignItems = Align.Center;
                headerRow.style.height = ResultRowHeight;
            }

            SetHeaderLabelStyle("HeaderExpandLabel", string.Empty, ExpandColumnWidth);
            SetHeaderLabelStyle("HeaderNameLabel", "资源名", NameColumnWidth);
            SetHeaderLabelStyle("HeaderTypeLabel", "类型", TypeColumnWidth);
            SetHeaderLabelStyle("HeaderScoreLabel", "分数", ScoreColumnWidth);
            SetHeaderLabelStyle("HeaderGradeLabel", "等级", GradeColumnWidth);
            SetHeaderLabelStyle("HeaderSeverityLabel", "最高严重度", SeverityColumnWidth);
            SetHeaderLabelStyle("HeaderIssueCountLabel", "问题数", IssueCountColumnWidth);

            Label pathHeader = rootVisualElement.Q<Label>("HeaderPathLabel");

            if (pathHeader != null)
            {
                pathHeader.text = "路径";
                pathHeader.style.flexGrow = 1;
            }
        }

        /// <summary>
        /// 设置固定宽度表头列。
        /// </summary>
        private void SetHeaderLabelStyle(string labelName, string text, int width)
        {
            Label label = rootVisualElement.Q<Label>(labelName);

            SetupSingleLineLabel(label, text);
            label.style.width = width;
            label.style.flexShrink = 0;
        }

        /// <summary>
        /// 扫描开始时刷新左侧列表为空态。
        /// </summary>
        private void RefreshResultListScanning()
        {
            selectedResultRow = null;
            SetResultToolsEnabled(false);

            resultScrollView.Clear();
            resultScrollView.Add(new Label("扫描中..."));
        }

        /// <summary>
        /// 刷新左侧结果树。
        /// 筛选只决定整棵树是否显示，不裁剪树内部子节点。
        /// 排序应用到所有同级节点。
        /// </summary>
        private void RefreshResultList()
        {
            if (resultScrollView == null)
            {
                return;
            }

            RefreshResultFilterChoices();

            selectedResultRow = null;

            resultScrollView.Clear();

            if (currentReport == null || scanResults == null || scanResults.Count == 0)
            {
                SetResultToolsEnabled(false);
                resultScrollView.Add(new Label("暂无扫描结果"));
                RefreshDetailPanelEmpty();
                return;
            }

            SetResultToolsEnabled(true);

            List<AssetScanResult> sortedResults = GetSortedScanResults();
            List<AssetScanResult> visibleResults = new List<AssetScanResult>();

            for (int i = 0; i < sortedResults.Count; i++)
            {
                AssetScanResult result = sortedResults[i];

                // AssetScanResult 是 struct，不能判断 result == null。
                if (result.Context == null)
                {
                    continue;
                }

                if (!DoesTreePassResultFilter(result.Context))
                {
                    continue;
                }

                visibleResults.Add(result);
            }

            if (visibleResults.Count == 0)
            {
                resultScrollView.Add(new Label("没有符合筛选条件的资源"));
                RefreshDetailPanelEmpty();
                return;
            }

            EnsureSelectedDetailVisible(visibleResults);

            for (int i = 0; i < visibleResults.Count; i++)
            {
                VisualElement node = CreateAssetTreeNode(
                    visibleResults[i].Context,
                    0,
                    new HashSet<string>());

                resultScrollView.Add(node);
            }
        }

        /// <summary>
        /// 筛选刷新后，保证右侧详情对应资源仍然在当前可见结果树中。
        /// 如果当前选中资源被筛掉，则自动选中第一棵可见树的根资源。
        /// </summary>
        private void EnsureSelectedDetailVisible(List<AssetScanResult> visibleResults)
        {
            if (!string.IsNullOrEmpty(selectedDetailAssetPath))
            {
                for (int i = 0; i < visibleResults.Count; i++)
                {
                    if (IsContextPathInTree(
                            visibleResults[i].Context,
                            selectedDetailAssetPath,
                            new HashSet<string>()))
                    {
                        return;
                    }
                }
            }

            for (int i = 0; i < visibleResults.Count; i++)
            {
                if (visibleResults[i].Context == null)
                {
                    continue;
                }

                SelectAssetDetail(visibleResults[i].Context);
                return;
            }

            RefreshDetailPanelEmpty();
        }

        /// <summary>
        /// 判断目标路径是否存在于指定资源树中。
        /// </summary>
        private bool IsContextPathInTree(
            AssetContext context,
            string targetAssetPath,
            HashSet<string> pathStack)
        {
            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            string assetPath = ScanToolUtility.NormalizePath(context.AssetPath);
            targetAssetPath = ScanToolUtility.NormalizePath(targetAssetPath);

            if (assetPath == targetAssetPath)
            {
                return true;
            }

            if (pathStack.Contains(assetPath))
            {
                return false;
            }

            pathStack.Add(assetPath);

            if (context.Dependencies != null)
            {
                for (int i = 0; i < context.Dependencies.Count; i++)
                {
                    if (IsContextPathInTree(context.Dependencies[i], targetAssetPath, pathStack))
                    {
                        pathStack.Remove(assetPath);
                        return true;
                    }
                }
            }

            pathStack.Remove(assetPath);
            return false;
        }

        /// <summary>
        /// 创建一个资源树节点。
        /// 节点包含一行资源信息，以及可选的子节点容器。
        /// </summary>
        private VisualElement CreateAssetTreeNode(
            AssetContext context,
            int depth,
            HashSet<string> pathStack)
        {
            VisualElement node = new VisualElement();
            node.style.flexDirection = FlexDirection.Column;

            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                node.Add(new Label("无效资源"));
                return node;
            }

            string assetPath = ScanToolUtility.NormalizePath(context.AssetPath);

            // UI 层再做一次循环保护，避免异常依赖数据导致窗口卡死。
            if (pathStack.Contains(assetPath))
            {
                Button ignoredButton;
                node.Add(CreateResultRow(context, depth, false, out ignoredButton));
                return node;
            }

            List<AssetContext> sortedDependencies = GetSortedDependencies(context);
            bool hasChildren = sortedDependencies.Count > 0;

            Button expandButton;
            VisualElement row = CreateResultRow(
                context,
                depth,
                hasChildren,
                out expandButton);

            node.Add(row);

            if (!hasChildren)
            {
                return node;
            }

            bool shouldExpand = ShouldExpandTreeNodeToSelectedDetail(context);

            VisualElement childrenContainer = new VisualElement();
            childrenContainer.style.flexDirection = FlexDirection.Column;
            childrenContainer.style.display = shouldExpand
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            pathStack.Add(assetPath);

            for (int i = 0; i < sortedDependencies.Count; i++)
            {
                VisualElement childNode = CreateAssetTreeNode(
                    sortedDependencies[i],
                    depth + 1,
                    pathStack);

                childrenContainer.Add(childNode);
            }

            pathStack.Remove(assetPath);

            bool isExpanded = shouldExpand;
            expandButton.text = isExpanded ? "▼" : "▶";

            expandButton.clicked += () =>
            {
                isExpanded = !isExpanded;

                childrenContainer.style.display = isExpanded
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

                expandButton.text = isExpanded ? "▼" : "▶";
            };

            node.Add(childrenContainer);

            return node;
        }

        /// <summary>
        /// 判断当前节点是否需要默认展开到右侧详情正在显示的资源。
        /// 如果选中的是当前节点自身，不需要展开；
        /// 如果选中的是当前节点的依赖子节点，则需要展开当前节点。
        /// </summary>
        private bool ShouldExpandTreeNodeToSelectedDetail(AssetContext context)
        {
            if (context == null || context.Dependencies == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(selectedDetailAssetPath))
            {
                return false;
            }

            for (int i = 0; i < context.Dependencies.Count; i++)
            {
                AssetContext dependency = context.Dependencies[i];

                if (IsContextPathInTree(
                        dependency,
                        selectedDetailAssetPath,
                        new HashSet<string>()))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 创建一行资源结果。
        /// 缩进只发生在资源名列内部，保证类型、分数、等级等列始终和表头对齐。
        /// </summary>
        private VisualElement CreateResultRow(
            AssetContext context,
            int depth,
            bool hasChildren,
            out Button expandButton)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.height = ResultRowHeight;
            row.style.alignItems = Align.Center;
            row.style.flexShrink = 0;
            row.style.backgroundColor = ResultRowNormalBackgroundColor;

            if (IsSelectedDetailContext(context))
            {
                SetSelectedResultRow(row);
            }

            row.RegisterCallback<ClickEvent>(_ =>
            {
                SelectAssetDetail(context);
                SetSelectedResultRow(row);
            });

            expandButton = CreateExpandButton(hasChildren);

            AddNameCell(row, context, depth, expandButton);

            AssetScanResult result;
            bool hasResult = TryGetResult(context, out result);

            AddRowLabel(row, GetAssetTypeText(context), TypeColumnWidth);
            AddRowLabel(row, hasResult ? result.Score.ToString() : "-", ScoreColumnWidth);
            AddRowLabel(row, hasResult ? result.Grade.ToString() : "-", GradeColumnWidth);
            AddRowLabel(row, GetIssueLevelText(hasResult, result), SeverityColumnWidth);
            AddRowLabel(row, hasResult ? result.IssueCount.ToString() : "-", IssueCountColumnWidth);

            Label pathLabel = new Label();
            SetupSingleLineLabel(pathLabel, context.AssetPath);

            pathLabel.style.flexGrow = 1;
            pathLabel.style.flexShrink = 1;
            pathLabel.style.minWidth = 0;

            row.Add(pathLabel);

            return row;
        }

        /// <summary>
        /// 创建展开按钮。
        /// 叶子节点也保留同样宽度的空按钮位，用来保证所有列对齐。
        /// </summary>
        private Button CreateExpandButton(bool hasChildren)
        {
            Button button = new Button();
            button.text = hasChildren ? "▶" : string.Empty;

            button.style.width = ExpandColumnWidth;
            button.style.height = ResultRowHeight;
            button.style.flexShrink = 0;

            button.style.marginLeft = 0;
            button.style.marginRight = 0;
            button.style.marginTop = 0;
            button.style.marginBottom = 0;

            button.style.paddingLeft = 0;
            button.style.paddingRight = 0;
            button.style.paddingTop = 0;
            button.style.paddingBottom = 0;

            if (!hasChildren)
            {
                button.SetEnabled(false);
            }

            return button;
        }

        /// <summary>
        /// 添加资源名列。
        /// </summary>
        private void AddNameCell(
            VisualElement row,
            AssetContext context,
            int depth,
            Button expandButton)
        {
            // 树形列总宽度 = 原来的箭头列宽 + 资源名列宽。
            VisualElement nameCell = new VisualElement();
            nameCell.style.flexDirection = FlexDirection.Row;
            nameCell.style.alignItems = Align.Center;
            nameCell.style.width = ExpandColumnWidth + NameColumnWidth;
            nameCell.style.flexShrink = 0;

            // 层级缩进。
            VisualElement indent = new VisualElement();
            indent.style.width = depth * TreeIndentWidth;
            indent.style.flexShrink = 0;
            nameCell.Add(indent);

            // 展开按钮跟随层级一起缩进。
            nameCell.Add(expandButton);

            string assetName = GetAssetName(context);

            Label nameLabel = new Label();
            SetupSingleLineLabel(nameLabel, assetName);

            nameLabel.style.flexGrow = 1;
            nameLabel.style.minWidth = 0;

            nameCell.Add(nameLabel);

            row.Add(nameCell);
        }

        /// <summary>
        /// 根据 AssetContext 获取最终评分结果。
        /// 结果列表只读取 currentReport，不再直接从 Scanner 获取分数、等级和严重度。
        /// </summary>
        private bool TryGetResult(AssetContext context, out AssetScanResult result)
        {
            result = new AssetScanResult();

            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            if (currentReport == null)
            {
                return false;
            }

            return currentReport.TryGetResult(context.AssetPath, out result);
        }

        /// <summary>
        /// 添加固定宽度文本列。
        /// 文本超出列宽时省略，完整文本通过 tooltip 查看。
        /// </summary>
        private void AddRowLabel(VisualElement row, string text, int width)
        {
            Label label = new Label();
            SetupSingleLineLabel(label, text);

            label.style.width = width;
            label.style.flexShrink = 0;

            row.Add(label);
        }

        private string GetAssetName(AssetContext context)
        {
            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return string.Empty;
            }

            return Path.GetFileName(context.AssetPath);
        }

        private string GetAssetTypeText(AssetContext context)
        {
            if (context == null || context.AssetTypes == null)
            {
                return "-";
            }

            if (context.AssetTypes.Count == 0)
            {
                return "-";
            }

            List<string> names = new List<string>();

            for (int i = 0; i < context.AssetTypes.Count; i++)
            {
                AssetTypeConfig assetType = context.AssetTypes[i];

                if (assetType == null)
                {
                    continue;
                }

                names.Add(string.IsNullOrEmpty(assetType.DisplayName)
                    ? assetType.AssetTypeId
                    : assetType.DisplayName);
            }

            return names.Count > 0 ? string.Join(",", names) : "-";
        }

        private string GetIssueLevelText(bool hasResult, AssetScanResult result)
        {
            if (!hasResult)
            {
                return "-";
            }

            if (result.IssueCount <= 0)
            {
                return "-";
            }

            return result.HighestIssueLevel.ToString();
        }

        /// <summary>
        /// 设置列表文本为单行显示。
        /// 超出列宽时使用省略号，完整内容通过 tooltip 查看。
        /// </summary>
        private void SetupSingleLineLabel(Label label, string fullText)
        {
            if (label == null)
            {
                return;
            }

            label.text = fullText;
            label.tooltip = fullText;

            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
        }

        /// <summary>
        /// 设置当前左侧结果列表选中行。
        /// 这里只通过代码修改背景色，不使用 USS。
        /// </summary>
        private void SetSelectedResultRow(VisualElement row)
        {
            if (selectedResultRow != null)
            {
                SetResultRowSelected(selectedResultRow, false);
            }

            selectedResultRow = row;

            if (selectedResultRow != null)
            {
                SetResultRowSelected(selectedResultRow, true);
            }
        }

        /// <summary>
        /// 设置单行选中背景色。
        /// </summary>
        private void SetResultRowSelected(VisualElement row, bool isSelected)
        {
            if (row == null)
            {
                return;
            }

            row.style.backgroundColor = isSelected
                ? ResultRowSelectedBackgroundColor
                : ResultRowNormalBackgroundColor;
        }

        /// <summary>
        /// 判断当前资源是否为详情面板正在显示的资源。
        /// 刷新列表时用它恢复选中高亮。
        /// </summary>
        private bool IsSelectedDetailContext(AssetContext context)
        {
            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            if (string.IsNullOrEmpty(selectedDetailAssetPath))
            {
                return false;
            }

            string assetPath = ScanToolUtility.NormalizePath(context.AssetPath);

            return assetPath == selectedDetailAssetPath;
        }

        /// <summary>
        /// 根据当前扫描结果刷新资源类型筛选项。
        /// 严重度和排序项是固定选项，只在初始化时设置。
        /// </summary>
        private void RefreshResultFilterChoices()
        {
            List<string> choices = GetAssetTypeFilterChoices();

            string currentValue = string.IsNullOrEmpty(assetTypeFilterDropdown.value)
                ? AssetTypeFilterAllText
                : assetTypeFilterDropdown.value;

            assetTypeFilterDropdown.choices = choices;

            if (choices.Contains(currentValue))
            {
                assetTypeFilterDropdown.SetValueWithoutNotify(currentValue);
            }
            else
            {
                assetTypeFilterDropdown.SetValueWithoutNotify(AssetTypeFilterAllText);
            }
        }

        /// <summary>
        /// 从当前报告中收集资源类型筛选项。
        /// </summary>
        private List<string> GetAssetTypeFilterChoices()
        {
            List<string> choices = new List<string>
            {
                AssetTypeFilterAllText
            };

            if (currentReport == null || currentReport.ResultsByPath == null)
            {
                return choices;
            }

            HashSet<string> typeNames = new HashSet<string>();

            foreach (AssetScanResult result in currentReport.ResultsByPath.Values)
            {
                if (result.Context == null || result.Context.AssetTypes == null)
                {
                    continue;
                }

                for (int i = 0; i < result.Context.AssetTypes.Count; i++)
                {
                    AssetTypeConfig assetType = result.Context.AssetTypes[i];

                    if (assetType == null)
                    {
                        continue;
                    }

                    string typeName = string.IsNullOrEmpty(assetType.DisplayName)
                        ? assetType.AssetTypeId
                        : assetType.DisplayName;

                    if (string.IsNullOrEmpty(typeName))
                    {
                        continue;
                    }

                    typeNames.Add(typeName);
                }
            }

            List<string> sortedTypeNames = new List<string>(typeNames);
            sortedTypeNames.Sort();

            choices.AddRange(sortedTypeNames);

            return choices;
        }

        /// <summary>
        /// 判断整棵树是否命中当前筛选条件。
        /// 筛选只决定整棵树是否显示，不裁剪树内部节点。
        /// </summary>
        private bool DoesTreePassResultFilter(AssetContext rootContext)
        {
            if (rootContext == null || string.IsNullOrEmpty(rootContext.AssetPath))
            {
                return false;
            }

            if (IsNoIssueSeverityFilter())
            {
                return DoesRootMatchNoIssueFilter(rootContext) &&
                       DoesTreeMatchAssetTypeFilter(rootContext, new HashSet<string>());
            }

            return DoesTreeContainContextPassingFilters(
                rootContext,
                new HashSet<string>());
        }

        /// <summary>
        /// 判断树内是否存在任意节点同时命中资源类型筛选和严重度筛选。
        /// </summary>
        private bool DoesTreeContainContextPassingFilters(
            AssetContext context,
            HashSet<string> pathStack)
        {
            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            string assetPath = ScanToolUtility.NormalizePath(context.AssetPath);

            if (pathStack.Contains(assetPath))
            {
                return false;
            }

            pathStack.Add(assetPath);

            if (DoesContextPassResultFilter(context))
            {
                pathStack.Remove(assetPath);
                return true;
            }

            if (context.Dependencies != null)
            {
                for (int i = 0; i < context.Dependencies.Count; i++)
                {
                    if (DoesTreeContainContextPassingFilters(context.Dependencies[i], pathStack))
                    {
                        pathStack.Remove(assetPath);
                        return true;
                    }
                }
            }

            pathStack.Remove(assetPath);
            return false;
        }

        /// <summary>
        /// 判断单个资源自身是否命中当前筛选条件。
        /// </summary>
        private bool DoesContextPassResultFilter(AssetContext context)
        {
            if (!DoesContextMatchAssetTypeFilter(context))
            {
                return false;
            }

            if (!DoesContextMatchSeverityFilter(context))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断整棵树中是否存在任意节点命中资源类型筛选。
        /// </summary>
        private bool DoesTreeMatchAssetTypeFilter(
            AssetContext context,
            HashSet<string> pathStack)
        {
            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            string selectedType = assetTypeFilterDropdown.value;

            if (string.IsNullOrEmpty(selectedType) || selectedType == AssetTypeFilterAllText)
            {
                return true;
            }

            string assetPath = ScanToolUtility.NormalizePath(context.AssetPath);

            if (pathStack.Contains(assetPath))
            {
                return false;
            }

            pathStack.Add(assetPath);

            if (DoesContextMatchAssetTypeFilter(context))
            {
                pathStack.Remove(assetPath);
                return true;
            }

            if (context.Dependencies != null)
            {
                for (int i = 0; i < context.Dependencies.Count; i++)
                {
                    if (DoesTreeMatchAssetTypeFilter(context.Dependencies[i], pathStack))
                    {
                        pathStack.Remove(assetPath);
                        return true;
                    }
                }
            }

            pathStack.Remove(assetPath);
            return false;
        }

        /// <summary>
        /// 判断资源自身是否符合资源类型筛选。
        /// </summary>
        private bool DoesContextMatchAssetTypeFilter(AssetContext context)
        {
            string selectedType = assetTypeFilterDropdown.value;

            if (string.IsNullOrEmpty(selectedType) || selectedType == AssetTypeFilterAllText)
            {
                return true;
            }

            if (context == null || context.AssetTypes == null)
            {
                return false;
            }

            for (int i = 0; i < context.AssetTypes.Count; i++)
            {
                AssetTypeConfig assetType = context.AssetTypes[i];

                if (assetType == null)
                {
                    continue;
                }

                string typeName = string.IsNullOrEmpty(assetType.DisplayName)
                    ? assetType.AssetTypeId
                    : assetType.DisplayName;

                if (typeName == selectedType)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断资源自身是否符合严重度筛选。
        /// 严重度筛选使用自身问题，不使用依赖汇总问题。
        /// </summary>
        private bool DoesContextMatchSeverityFilter(AssetContext context)
        {
            string selectedSeverity = severityFilterDropdown.value;

            if (string.IsNullOrEmpty(selectedSeverity) || selectedSeverity == SeverityFilterAllText)
            {
                return true;
            }

            if (selectedSeverity == SeverityFilterNoneText)
            {
                return false;
            }

            AssetScanResult result;
            if (!TryGetResult(context, out result))
            {
                return false;
            }

            if (result.SelfIssueCount <= 0)
            {
                return false;
            }

            return result.SelfHighestIssueLevel.ToString() == selectedSeverity;
        }

        /// <summary>
        /// 当前严重度筛选是否为“无问题”。
        /// </summary>
        private bool IsNoIssueSeverityFilter()
        {
            return severityFilterDropdown.value == SeverityFilterNoneText;
        }

        /// <summary>
        /// “无问题”筛选使用根节点汇总问题数判断。
        /// 根节点汇总 IssueCount 为 0，表示整棵树没有问题。
        /// </summary>
        private bool DoesRootMatchNoIssueFilter(AssetContext rootContext)
        {
            AssetScanResult result;
            if (!TryGetResult(rootContext, out result))
            {
                return false;
            }

            return result.IssueCount <= 0;
        }

        /// <summary>
        /// 获取排序后的根节点结果列表。
        /// 不直接修改 scanResults，避免排序影响原始报告顺序。
        /// </summary>
        private List<AssetScanResult> GetSortedScanResults()
        {
            List<AssetScanResult> results = new List<AssetScanResult>();

            if (scanResults == null)
            {
                return results;
            }

            for (int i = 0; i < scanResults.Count; i++)
            {
                results.Add(scanResults[i]);
            }

            results.Sort((left, right) => { return CompareContexts(left.Context, right.Context); });

            return results;
        }

        /// <summary>
        /// 获取排序后的直接依赖列表。
        /// 只排序当前节点的直接子节点，不改变父子关系。
        /// </summary>
        private List<AssetContext> GetSortedDependencies(AssetContext context)
        {
            List<AssetContext> dependencies = new List<AssetContext>();

            if (context == null || context.Dependencies == null)
            {
                return dependencies;
            }

            for (int i = 0; i < context.Dependencies.Count; i++)
            {
                AssetContext dependency = context.Dependencies[i];

                if (dependency == null)
                {
                    continue;
                }

                dependencies.Add(dependency);
            }

            dependencies.Sort(CompareContexts);

            return dependencies;
        }

        /// <summary>
        /// 根据当前排序选项比较两个资源。
        /// 排序使用结果列表展示的汇总分数、汇总问题数和汇总最高严重度。
        /// </summary>
        private int CompareContexts(AssetContext left, AssetContext right)
        {
            string sortMode = sortDropdown.value;

            AssetScanResult leftResult;
            bool hasLeftResult = TryGetResult(left, out leftResult);

            AssetScanResult rightResult;
            bool hasRightResult = TryGetResult(right, out rightResult);

            if (sortMode == SortByScoreAscText)
            {
                int result = GetCompareScore(hasLeftResult, leftResult)
                    .CompareTo(GetCompareScore(hasRightResult, rightResult));

                return result != 0 ? result : CompareContextPath(left, right);
            }

            if (sortMode == SortByScoreDescText)
            {
                int result = GetCompareScore(hasRightResult, rightResult)
                    .CompareTo(GetCompareScore(hasLeftResult, leftResult));

                return result != 0 ? result : CompareContextPath(left, right);
            }

            if (sortMode == SortByIssueCountDescText)
            {
                int result = GetCompareIssueCount(hasRightResult, rightResult)
                    .CompareTo(GetCompareIssueCount(hasLeftResult, leftResult));

                return result != 0 ? result : CompareContextPath(left, right);
            }

            if (sortMode == SortBySeverityDescText)
            {
                int result = GetCompareSeverity(hasRightResult, rightResult)
                    .CompareTo(GetCompareSeverity(hasLeftResult, leftResult));

                return result != 0 ? result : CompareContextPath(left, right);
            }

            return CompareContextPath(left, right);
        }

        private int GetCompareScore(bool hasResult, AssetScanResult result)
        {
            return hasResult ? result.Score : 0;
        }

        private int GetCompareIssueCount(bool hasResult, AssetScanResult result)
        {
            return hasResult ? result.IssueCount : 0;
        }

        private int GetCompareSeverity(bool hasResult, AssetScanResult result)
        {
            if (!hasResult || result.IssueCount <= 0)
            {
                return -1;
            }

            switch (result.HighestIssueLevel)
            {
                case EIssueLevel.Info:
                    return 0;

                case EIssueLevel.Warning:
                    return 1;

                case EIssueLevel.Error:
                    return 2;

                case EIssueLevel.Blocker:
                    return 3;

                default:
                    return -1;
            }
        }

        private int CompareContextPath(AssetContext left, AssetContext right)
        {
            string leftPath = left == null ? string.Empty : left.AssetPath;
            string rightPath = right == null ? string.Empty : right.AssetPath;

            return string.Compare(
                leftPath,
                rightPath,
                System.StringComparison.OrdinalIgnoreCase);
        }
    }
}