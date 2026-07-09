using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 负责扫描结果筛选、树形展示、展开状态和资源行选择。
    /// </summary>
    internal sealed class ScanResultListView
    {
        private const int TreeIndentWidth = 16;
        private const string AssetTypeFilterAllText = "全部类型";
        private const string SeverityFilterAllText = "全部严重度";
        private const string SeverityFilterNoneText = "无问题";
        private const string SortByPathText = "路径";
        private const string SortByScoreAscText = "分数低到高";
        private const string SortByScoreDescText = "分数高到低";
        private const string SortByIssueCountDescText = "问题数多到少";
        private const string SortBySeverityDescText = "严重度高到低";

        private static readonly Color ResultRowNormalBackgroundColor =
            new Color(0f, 0f, 0f, 0f);
        private static readonly Color ResultRowSelectedBackgroundColor =
            new Color(0.18f, 0.37f, 0.57f, 0.42f);

        private readonly Action<AssetContext> selectionChanged;
        private readonly DropdownField assetTypeFilterDropdown;
        private readonly DropdownField severityFilterDropdown;
        private readonly DropdownField sortDropdown;
        private readonly ScrollView resultScrollView;

        private AssetScoreReport report;
        private string selectedAssetPath;
        private VisualElement selectedResultRow;

        /// <summary>
        /// 绑定结果区域控件，并将资源行选择通过回调交给窗口组合层。
        /// </summary>
        public ScanResultListView(
            VisualElement windowRoot,
            Action<AssetContext> onSelectionChanged)
        {
            selectionChanged = onSelectionChanged;
            assetTypeFilterDropdown =
                windowRoot.Q<DropdownField>("AssetTypeFilterDropdown");
            severityFilterDropdown =
                windowRoot.Q<DropdownField>("SeverityFilterDropdown");
            sortDropdown = windowRoot.Q<DropdownField>("SortDropdown");
            resultScrollView = windowRoot.Q<ScrollView>("ResultScrollView");

            InitializeFilterDropdowns();
            SetResultToolsEnabled(false);

            assetTypeFilterDropdown.RegisterValueChangedCallback(
                delegate { Refresh(); });
            severityFilterDropdown.RegisterValueChangedCallback(
                delegate { Refresh(); });
            sortDropdown.RegisterValueChangedCallback(
                delegate { Refresh(); });
        }

        /// <summary>
        /// 替换当前报告，清除上一次选择，并刷新完整结果树。
        /// </summary>
        public void SetReport(AssetScoreReport value)
        {
            report = value;
            selectedAssetPath = string.Empty;
            selectedResultRow = null;
            Refresh();
        }

        /// <summary>
        /// 扫描开始时清除旧报告，并显示不可筛选的扫描中状态。
        /// </summary>
        public void ShowScanning()
        {
            report = null;
            selectedAssetPath = string.Empty;
            selectedResultRow = null;
            SetResultToolsEnabled(false);
            resultScrollView.Clear();
            resultScrollView.Add(new Label("扫描中..."));
        }

        /// <summary>
        /// 清除报告、选择和结果树，恢复未扫描状态。
        /// </summary>
        public void Clear()
        {
            SetReport(null);
        }

        /// <summary>
        /// 初始化结果筛选和排序下拉框的固定选项。
        /// </summary>
        private void InitializeFilterDropdowns()
        {
            assetTypeFilterDropdown.choices = new List<string>
            {
                AssetTypeFilterAllText
            };
            assetTypeFilterDropdown.SetValueWithoutNotify(
                AssetTypeFilterAllText);

            severityFilterDropdown.choices = new List<string>
            {
                SeverityFilterAllText,
                SeverityFilterNoneText,
                EIssueLevel.Info.ToString(),
                EIssueLevel.Warning.ToString(),
                EIssueLevel.Error.ToString(),
                EIssueLevel.Blocker.ToString()
            };
            severityFilterDropdown.SetValueWithoutNotify(
                SeverityFilterAllText);

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
        /// 统一切换结果筛选和排序控件的可用状态。
        /// </summary>
        private void SetResultToolsEnabled(bool enabled)
        {
            UIElementUtility.SetEnabled(assetTypeFilterDropdown, enabled);
            UIElementUtility.SetEnabled(severityFilterDropdown, enabled);
            UIElementUtility.SetEnabled(sortDropdown, enabled);
        }

        /// <summary>
        /// 根据报告和当前筛选条件重建左侧结果树。
        /// </summary>
        private void Refresh()
        {
            RefreshAssetTypeChoices();
            selectedResultRow = null;
            resultScrollView.Clear();

            if (report == null || report.DisplayRootResults.Count == 0)
            {
                SetResultToolsEnabled(false);
                resultScrollView.Add(new Label("暂无扫描结果"));
                return;
            }

            SetResultToolsEnabled(true);
            List<AssetScanResult> visibleResults = GetSortedScanResults();

            if (visibleResults.Count == 0)
            {
                resultScrollView.Add(new Label("没有符合筛选条件的资源"));
                selectionChanged?.Invoke(null);
                selectedAssetPath = string.Empty;
                return;
            }

            EnsureSelectedAssetVisible(visibleResults);

            for (int i = 0; i < visibleResults.Count; i++)
            {
                resultScrollView.Add(CreateAssetTreeNode(
                    visibleResults[i].Context,
                    0,
                    new HashSet<string>()));
            }
        }

        /// <summary>
        /// 使用报告提供的筛选和排序入口获取当前可见根结果。
        /// </summary>
        private List<AssetScanResult> GetSortedScanResults()
        {
            bool onlyNoIssues =
                severityFilterDropdown.value == SeverityFilterNoneText;

            return report.GetDisplayRootResults(
                GetSelectedAssetTypeFilter(),
                GetSelectedSelfIssueLevelFilter(),
                onlyNoIssues,
                GetSelectedResultSortMode());
        }

        /// <summary>
        /// 筛选后保证当前详情资源仍可见，否则选择第一棵可见树的根资源。
        /// </summary>
        private void EnsureSelectedAssetVisible(
            List<AssetScanResult> visibleResults)
        {
            if (!string.IsNullOrEmpty(selectedAssetPath))
            {
                for (int i = 0; i < visibleResults.Count; i++)
                {
                    if (IsContextPathInTree(
                            visibleResults[i].Context,
                            selectedAssetPath,
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

                SelectAsset(visibleResults[i].Context);
                return;
            }

            selectionChanged?.Invoke(null);
        }

        /// <summary>
        /// 判断目标资源路径是否存在于指定依赖树，并防止异常循环依赖递归。
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
                    if (IsContextPathInTree(
                            context.Dependencies[i],
                            targetAssetPath,
                            pathStack))
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
        /// 创建资源树节点及其按当前排序方式排列的依赖子节点。
        /// </summary>
        private VisualElement CreateAssetTreeNode(
            AssetContext context,
            int depth,
            HashSet<string> pathStack)
        {
            VisualElement node = new VisualElement();
            node.AddToClassList("scan-result-tree-node");

            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                node.Add(new Label("无效资源"));
                return node;
            }

            string assetPath = ScanToolUtility.NormalizePath(context.AssetPath);

            // UI 层保留循环保护，避免异常依赖数据导致窗口递归卡死。
            if (pathStack.Contains(assetPath))
            {
                Button ignoredButton;
                node.Add(CreateResultRow(
                    context,
                    depth,
                    false,
                    out ignoredButton));
                return node;
            }

            List<AssetContext> dependencies = report.GetSortedDependencies(
                context,
                GetSelectedResultSortMode());
            bool hasChildren = dependencies.Count > 0;
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

            bool isExpanded = ShouldExpandToSelectedAsset(context);
            VisualElement childrenContainer = new VisualElement();
            childrenContainer.AddToClassList("scan-result-children");
            UIElementUtility.SetVisible(childrenContainer, isExpanded);

            pathStack.Add(assetPath);
            for (int i = 0; i < dependencies.Count; i++)
            {
                childrenContainer.Add(CreateAssetTreeNode(
                    dependencies[i],
                    depth + 1,
                    pathStack));
            }
            pathStack.Remove(assetPath);

            expandButton.text = isExpanded ? "▼" : "▶";
            expandButton.clicked +=
                delegate
                {
                    isExpanded = !isExpanded;
                    UIElementUtility.SetVisible(
                        childrenContainer,
                        isExpanded);
                    expandButton.text = isExpanded ? "▼" : "▶";
                };
            node.Add(childrenContainer);
            return node;
        }

        /// <summary>
        /// 判断节点是否需要展开以显示当前选中的依赖资源。
        /// </summary>
        private bool ShouldExpandToSelectedAsset(AssetContext context)
        {
            if (context == null ||
                context.Dependencies == null ||
                string.IsNullOrEmpty(selectedAssetPath))
            {
                return false;
            }

            for (int i = 0; i < context.Dependencies.Count; i++)
            {
                if (IsContextPathInTree(
                        context.Dependencies[i],
                        selectedAssetPath,
                        new HashSet<string>()))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 创建一行资源结果，并绑定选择和评分展示。
        /// </summary>
        private VisualElement CreateResultRow(
            AssetContext context,
            int depth,
            bool hasChildren,
            out Button expandButton)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("scan-result-row");
            row.style.backgroundColor = ResultRowNormalBackgroundColor;

            if (IsSelectedContext(context))
            {
                SetSelectedResultRow(row);
            }

            row.RegisterCallback<ClickEvent>(
                delegate
                {
                    SelectAsset(context);
                    SetSelectedResultRow(row);
                });

            expandButton = CreateExpandButton(hasChildren);
            AddNameCell(row, context, depth, expandButton);

            AssetScanResult result;
            bool hasResult = TryGetResult(context, out result);
            AddRowLabel(row, GetAssetTypeText(context),
                "scan-result-type-cell");

            Label scoreLabel = AddRowLabel(
                row,
                hasResult ? result.Score.ToString() : "-",
                "scan-result-score-cell");
            Label gradeLabel = AddRowLabel(
                row,
                hasResult ? result.Grade.ToString() : "-",
                "scan-result-grade-cell");
            Label severityLabel = AddRowLabel(
                row,
                GetIssueLevelText(hasResult, result),
                "scan-result-severity-cell");

            if (hasResult)
            {
                ApplyQualityColorClass(scoreLabel, result.Grade);
                ApplyQualityColorClass(gradeLabel, result.Grade);
            }

            ApplySeverityColorClass(
                severityLabel,
                hasResult && result.IssueCount > 0,
                hasResult ? result.HighestIssueLevel : EIssueLevel.Info);
            AddRowLabel(
                row,
                hasResult ? result.IssueCount.ToString() : "-",
                "scan-result-issue-count-cell");

            Label pathLabel = new Label();
            pathLabel.AddToClassList("scan-result-path-cell");
            SetupSingleLineLabel(pathLabel, context.AssetPath);
            pathLabel.style.flexGrow = 1;
            row.Add(pathLabel);
            return row;
        }

        /// <summary>
        /// 保存当前选择路径并通知组合层刷新资源详情。
        /// </summary>
        private void SelectAsset(AssetContext context)
        {
            selectedAssetPath = context == null
                ? string.Empty
                : ScanToolUtility.NormalizePath(context.AssetPath);
            selectionChanged?.Invoke(context);
        }

        /// <summary>
        /// 创建保持列宽的树节点展开按钮。
        /// </summary>
        private Button CreateExpandButton(bool hasChildren)
        {
            Button button = new Button();
            button.AddToClassList("scan-result-expand-button");
            button.text = hasChildren ? "▶" : string.Empty;

            if (!hasChildren)
            {
                UIElementUtility.SetEnabled(button, false);
            }

            return button;
        }

        /// <summary>
        /// 创建包含层级缩进、展开按钮和资源名的名称列。
        /// </summary>
        private void AddNameCell(
            VisualElement row,
            AssetContext context,
            int depth,
            Button expandButton)
        {
            VisualElement nameCell = new VisualElement();
            nameCell.AddToClassList("scan-result-name-cell");

            VisualElement indent = new VisualElement();
            indent.AddToClassList("scan-result-indent");
            indent.style.width = depth * TreeIndentWidth;
            indent.style.flexShrink = 0;
            nameCell.Add(indent);
            nameCell.Add(expandButton);

            Label nameLabel = new Label();
            nameLabel.AddToClassList("scan-result-name-label");
            SetupSingleLineLabel(nameLabel, GetAssetName(context));
            nameCell.Add(nameLabel);
            row.Add(nameCell);
        }

        /// <summary>
        /// 从当前报告读取指定资源的最终评分结果。
        /// </summary>
        private bool TryGetResult(
            AssetContext context,
            out AssetScanResult result)
        {
            result = new AssetScanResult();
            return report != null &&
                   context != null &&
                   !string.IsNullOrEmpty(context.AssetPath) &&
                   report.TryGetResult(context.AssetPath, out result);
        }

        /// <summary>
        /// 创建固定列样式的单行文本标签。
        /// </summary>
        private Label AddRowLabel(
            VisualElement row,
            string text,
            string className)
        {
            Label label = new Label();
            label.AddToClassList(className);
            SetupSingleLineLabel(label, text);
            row.Add(label);
            return label;
        }

        /// <summary>
        /// 获取资源路径末尾的资源名称。
        /// </summary>
        private string GetAssetName(AssetContext context)
        {
            return context == null || string.IsNullOrEmpty(context.AssetPath)
                ? string.Empty
                : Path.GetFileName(context.AssetPath);
        }

        /// <summary>
        /// 合并资源匹配到的全部类型显示名称。
        /// </summary>
        private string GetAssetTypeText(AssetContext context)
        {
            if (context == null ||
                context.AssetTypes == null ||
                context.AssetTypes.Count == 0)
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

        /// <summary>
        /// 获取结果行使用的最高问题严重度文本。
        /// </summary>
        private string GetIssueLevelText(
            bool hasResult,
            AssetScanResult result)
        {
            return !hasResult || result.IssueCount <= 0
                ? "-"
                : result.HighestIssueLevel.ToString();
        }

        /// <summary>
        /// 将标签设置为带完整 tooltip 的单行省略显示。
        /// </summary>
        private void SetupSingleLineLabel(Label label, string fullText)
        {
            label.text = fullText;
            label.tooltip = fullText;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
        }

        /// <summary>
        /// 切换左侧结果树的当前选中行高亮。
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
        /// 设置单行选中或普通背景色。
        /// </summary>
        private void SetResultRowSelected(
            VisualElement row,
            bool isSelected)
        {
            row.style.backgroundColor = isSelected
                ? ResultRowSelectedBackgroundColor
                : ResultRowNormalBackgroundColor;
        }

        /// <summary>
        /// 判断资源是否为结果 View 当前保存的选中资源。
        /// </summary>
        private bool IsSelectedContext(AssetContext context)
        {
            return context != null &&
                   !string.IsNullOrEmpty(context.AssetPath) &&
                   !string.IsNullOrEmpty(selectedAssetPath) &&
                   ScanToolUtility.NormalizePath(context.AssetPath) ==
                   selectedAssetPath;
        }

        /// <summary>
        /// 按当前报告刷新资源类型筛选选项并尽量保留原选择。
        /// </summary>
        private void RefreshAssetTypeChoices()
        {
            List<string> choices = new List<string>
            {
                AssetTypeFilterAllText
            };

            if (report != null)
            {
                choices.AddRange(report.GetAssetTypeNames());
            }

            string currentValue =
                string.IsNullOrEmpty(assetTypeFilterDropdown.value)
                    ? AssetTypeFilterAllText
                    : assetTypeFilterDropdown.value;
            assetTypeFilterDropdown.choices = choices;
            assetTypeFilterDropdown.SetValueWithoutNotify(
                choices.Contains(currentValue)
                    ? currentValue
                    : AssetTypeFilterAllText);
        }

        /// <summary>
        /// 将类型筛选文本转换为报告查询参数。
        /// </summary>
        private string GetSelectedAssetTypeFilter()
        {
            return assetTypeFilterDropdown.value == AssetTypeFilterAllText
                ? string.Empty
                : assetTypeFilterDropdown.value;
        }

        /// <summary>
        /// 将严重度筛选文本转换为报告查询参数。
        /// </summary>
        private EIssueLevel? GetSelectedSelfIssueLevelFilter()
        {
            switch (severityFilterDropdown.value)
            {
                case "Info":
                    return EIssueLevel.Info;
                case "Warning":
                    return EIssueLevel.Warning;
                case "Error":
                    return EIssueLevel.Error;
                case "Blocker":
                    return EIssueLevel.Blocker;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 将排序下拉框文本转换为报告排序枚举。
        /// </summary>
        private EAssetScoreReportSortMode GetSelectedResultSortMode()
        {
            switch (sortDropdown.value)
            {
                case SortByScoreAscText:
                    return EAssetScoreReportSortMode.ScoreAscending;
                case SortByScoreDescText:
                    return EAssetScoreReportSortMode.ScoreDescending;
                case SortByIssueCountDescText:
                    return EAssetScoreReportSortMode.IssueCountDescending;
                case SortBySeverityDescText:
                    return EAssetScoreReportSortMode.SeverityDescending;
                default:
                    return EAssetScoreReportSortMode.Path;
            }
        }

        /// <summary>
        /// 按资源等级为结果列应用质量颜色类。
        /// </summary>
        private void ApplyQualityColorClass(
            VisualElement element,
            EAssetGrade grade)
        {
            switch (grade)
            {
                case EAssetGrade.A:
                    element.AddToClassList("scan-quality-a");
                    break;
                case EAssetGrade.B:
                    element.AddToClassList("scan-quality-b");
                    break;
                case EAssetGrade.C:
                    element.AddToClassList("scan-quality-c");
                    break;
                case EAssetGrade.D:
                    element.AddToClassList("scan-quality-d");
                    break;
            }
        }

        /// <summary>
        /// 按问题严重度为结果列应用风险颜色类。
        /// </summary>
        private void ApplySeverityColorClass(
            VisualElement element,
            bool hasIssue,
            EIssueLevel issueLevel)
        {
            if (!hasIssue)
            {
                element.AddToClassList("scan-severity-none");
                return;
            }

            switch (issueLevel)
            {
                case EIssueLevel.Info:
                    element.AddToClassList("scan-severity-info");
                    break;
                case EIssueLevel.Warning:
                    element.AddToClassList("scan-severity-warning");
                    break;
                case EIssueLevel.Error:
                    element.AddToClassList("scan-severity-error");
                    break;
                case EIssueLevel.Blocker:
                    element.AddToClassList("scan-severity-blocker");
                    break;
            }
        }
    }
}
