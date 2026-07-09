using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 负责扫描资源详情、问题、依赖和 Prefab 问题定位交互。
    /// </summary>
    internal sealed class ScanDetailPanelView
    {
        private const string FoldoutCardViewAssetPath =
            "Assets/Tools/asset_score_tool/Editor/UI/Common/FoldoutCardView.uxml";

        private readonly Action<string> selectAssetRequested;
        private readonly Action<string> openFolderRequested;
        private readonly Action<string, string> locatePrefabIssueRequested;
        private readonly Label detailAssetNameLabel;
        private readonly Label detailAssetPathLabel;
        private readonly Label detailAssetTypeLabel;
        private readonly Label detailProfileLabel;
        private readonly Label detailScoreLabel;
        private readonly Label detailGradeLabel;
        private readonly Label detailIssueCountLabel;
        private readonly VisualElement issueListContainer;
        private readonly VisualElement dependencyListContainer;

        private AssetScoreReport report;
        private string profileText;
        private string selectedAssetPath;

        /// <summary>
        /// 绑定详情控件，并保存资源选择、目录打开和 Prefab 定位回调。
        /// </summary>
        public ScanDetailPanelView(
            VisualElement windowRoot,
            Action<string> onSelectAssetRequested,
            Action<string> onOpenFolderRequested,
            Action<string, string> onLocatePrefabIssueRequested)
        {
            selectAssetRequested = onSelectAssetRequested;
            openFolderRequested = onOpenFolderRequested;
            locatePrefabIssueRequested = onLocatePrefabIssueRequested;

            detailAssetNameLabel =
                windowRoot.Q<Label>("DetailAssetNameLabel");
            detailAssetPathLabel =
                windowRoot.Q<Label>("DetailAssetPathLabel");
            detailAssetTypeLabel =
                windowRoot.Q<Label>("DetailAssetTypeLabel");
            detailProfileLabel =
                windowRoot.Q<Label>("DetailProfileLabel");
            detailScoreLabel = windowRoot.Q<Label>("DetailScoreLabel");
            detailGradeLabel = windowRoot.Q<Label>("DetailGradeLabel");
            detailIssueCountLabel =
                windowRoot.Q<Label>("DetailIssueCountLabel");

            VisualTreeAsset foldoutTemplate =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    FoldoutCardViewAssetPath);
            FoldoutCardView issueFoldout =
                new FoldoutCardView(foldoutTemplate);
            issueFoldout.Title = "扣分详情";
            issueFoldout.IsExpanded = true;
            FoldoutCardView dependencyFoldout =
                new FoldoutCardView(foldoutTemplate);
            dependencyFoldout.Title = "关联资源 / 依赖";
            dependencyFoldout.IsExpanded = true;

            windowRoot.Q<VisualElement>("IssueFoldoutHost")
                .Add(issueFoldout);
            windowRoot.Q<VisualElement>("DependencyFoldoutHost")
                .Add(dependencyFoldout);
            issueListContainer = issueFoldout.ContentContainer;
            dependencyListContainer = dependencyFoldout.ContentContainer;

            Button pingMainAssetButton =
                windowRoot.Q<Button>("PingMainAssetButton");
            pingMainAssetButton.text = "选中资源";
            pingMainAssetButton.clicked +=
                delegate
                {
                    if (!string.IsNullOrEmpty(selectedAssetPath))
                    {
                        selectAssetRequested?.Invoke(selectedAssetPath);
                    }
                };

            Button openFolderButton =
                windowRoot.Q<Button>("OpenFolderButton");
            openFolderButton.text = "打开文件夹";
            openFolderButton.clicked +=
                delegate
                {
                    if (!string.IsNullOrEmpty(selectedAssetPath))
                    {
                        openFolderRequested?.Invoke(selectedAssetPath);
                    }
                };
        }

        /// <summary>
        /// 替换详情面板读取的报告与 Profile 文本，并清除旧选择。
        /// </summary>
        public void SetReport(
            AssetScoreReport value,
            string selectedProfileText)
        {
            report = value;
            profileText = string.IsNullOrEmpty(selectedProfileText)
                ? "-"
                : selectedProfileText;
            Clear();
        }

        /// <summary>
        /// 展示指定资源的报告结果、自身问题和直接依赖。
        /// </summary>
        public void ShowAsset(AssetContext context)
        {
            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                Clear();
                return;
            }

            selectedAssetPath =
                ScanToolUtility.NormalizePath(context.AssetPath);
            AssetScanResult result;
            bool hasResult = TryGetResult(context, out result);

            detailAssetNameLabel.text =
                "资源名：" + GetAssetName(context);
            detailAssetPathLabel.text = "路径：" + context.AssetPath;
            detailAssetTypeLabel.text =
                "类型：" + GetAssetTypeText(context);
            detailProfileLabel.text = "Profile：" + profileText;
            detailScoreLabel.text =
                "分数：" + (hasResult ? result.Score.ToString() : "-");
            detailGradeLabel.text =
                "等级：" + (hasResult ? result.Grade.ToString() : "-");

            if (hasResult)
            {
                ApplyQualityColorClass(detailScoreLabel, result.Grade);
                ApplyQualityColorClass(detailGradeLabel, result.Grade);
                detailIssueCountLabel.text =
                    "问题数：自身 " + result.SelfIssueCount +
                    " / 汇总 " + result.IssueCount;
            }
            else
            {
                ClearQualityColorClasses(detailScoreLabel);
                ClearQualityColorClasses(detailGradeLabel);
                detailIssueCountLabel.text = "问题数：-";
            }

            RefreshIssueList(context);
            RefreshDependencyList(context);
        }

        /// <summary>
        /// 清除当前资源，并恢复详情面板的空状态内容。
        /// </summary>
        public void Clear()
        {
            selectedAssetPath = string.Empty;
            detailAssetNameLabel.text = "资源名：未选择";
            detailAssetPathLabel.text = "路径：-";
            detailAssetTypeLabel.text = "类型：-";
            detailProfileLabel.text = "Profile：-";
            detailScoreLabel.text = "分数：-";
            detailGradeLabel.text = "等级：-";
            ClearQualityColorClasses(detailScoreLabel);
            ClearQualityColorClasses(detailGradeLabel);
            detailIssueCountLabel.text = "问题数：0";

            issueListContainer.Clear();
            issueListContainer.Add(CreateDetailMessage("暂无问题"));
            dependencyListContainer.Clear();
            dependencyListContainer.Add(CreateDetailMessage("暂无依赖"));
        }

        /// <summary>
        /// 刷新资源自身问题列表，不重复展开依赖问题。
        /// </summary>
        private void RefreshIssueList(AssetContext context)
        {
            issueListContainer.Clear();
            List<AssetIssueResult> issues = report == null
                ? new List<AssetIssueResult>()
                : report.GetSelfIssues(context);

            if (issues.Count == 0)
            {
                AssetScanResult result;
                bool hasResult = TryGetResult(context, out result);
                issueListContainer.Add(CreateDetailMessage(
                    hasResult && result.IssueCount > 0
                        ? "该资源自身暂无问题，问题来自依赖资源。请在左侧展开依赖查看。"
                        : "暂无问题"));
                return;
            }

            for (int i = 0; i < issues.Count; i++)
            {
                issueListContainer.Add(CreateIssueElement(issues[i]));
            }
        }

        /// <summary>
        /// 刷新当前资源的直接依赖列表。
        /// </summary>
        private void RefreshDependencyList(AssetContext context)
        {
            dependencyListContainer.Clear();
            if (context.Dependencies == null ||
                context.Dependencies.Count == 0)
            {
                dependencyListContainer.Add(
                    CreateDetailMessage("暂无依赖"));
                return;
            }

            for (int i = 0; i < context.Dependencies.Count; i++)
            {
                AssetContext dependency = context.Dependencies[i];
                if (dependency == null ||
                    string.IsNullOrEmpty(dependency.AssetPath))
                {
                    continue;
                }

                dependencyListContainer.Add(
                    CreateDependencyElement(dependency));
            }
        }

        /// <summary>
        /// 创建详情折叠区中的空状态提示。
        /// </summary>
        private Label CreateDetailMessage(string message)
        {
            Label label = new Label(message);
            label.AddToClassList("scan-detail-empty");
            return label;
        }

        /// <summary>
        /// 创建显示依赖路径和汇总问题数的直接依赖条目。
        /// </summary>
        private VisualElement CreateDependencyElement(AssetContext dependency)
        {
            VisualElement root = new VisualElement();
            root.AddToClassList("scan-dependency-item");

            Label pathLabel = new Label(dependency.AssetPath);
            pathLabel.tooltip = dependency.AssetPath;
            pathLabel.AddToClassList("scan-dependency-path");
            root.Add(pathLabel);

            AssetScanResult result;
            Label issueCountLabel = new Label(
                TryGetResult(dependency, out result)
                    ? "问题数：" + result.IssueCount
                    : "问题数：-");
            issueCountLabel.AddToClassList(
                "scan-dependency-issue-count");
            root.Add(issueCountLabel);
            return root;
        }

        /// <summary>
        /// 创建包含标题、规则信息、说明和定位项的问题条目。
        /// </summary>
        private VisualElement CreateIssueElement(
            AssetIssueResult issueResult)
        {
            VisualElement root = new VisualElement();
            root.AddToClassList("scan-issue-item");

            Label titleLabel = new Label(GetIssueTitle(issueResult));
            titleLabel.AddToClassList("scan-issue-title");
            ApplySeverityColorClass(
                titleLabel,
                issueResult != null && issueResult.IssueInfo != null,
                issueResult != null && issueResult.IssueInfo != null
                    ? issueResult.IssueInfo.issueLevel
                    : EIssueLevel.Info);
            root.Add(titleLabel);

            Label metadataLabel = new Label(GetIssueMetadata(issueResult));
            metadataLabel.AddToClassList("scan-issue-metadata");
            root.Add(metadataLabel);

            Label messageLabel = new Label(GetIssueMessage(issueResult));
            messageLabel.AddToClassList("scan-issue-message");
            root.Add(messageLabel);

            AddIssueLocationElements(root, issueResult);
            return root;
        }

        /// <summary>
        /// 在问题详情下方添加可展开的 Prefab 内部对象定位项。
        /// </summary>
        private void AddIssueLocationElements(
            VisualElement root,
            AssetIssueResult issueResult)
        {
            if (issueResult == null ||
                issueResult.Locations == null ||
                issueResult.Locations.Count == 0 ||
                issueResult.Context == null ||
                string.IsNullOrEmpty(issueResult.Context.AssetPath))
            {
                return;
            }

            VisualElement locationList = new VisualElement();
            locationList.AddToClassList("scan-issue-location-list");
            UIElementUtility.SetVisible(locationList, false);
            bool isExpanded = false;
            Button toggleButton = new Button();
            toggleButton.AddToClassList("scan-issue-location-toggle");
            toggleButton.text =
                "▶ 定位对象（" + issueResult.Locations.Count + "）";
            toggleButton.clicked +=
                delegate
                {
                    isExpanded = !isExpanded;
                    toggleButton.text =
                        (isExpanded ? "▼ " : "▶ ") +
                        "定位对象（" + issueResult.Locations.Count + "）";
                    UIElementUtility.SetVisible(locationList, isExpanded);
                };
            root.Add(toggleButton);
            root.Add(locationList);

            string assetPath = issueResult.Context.AssetPath;
            for (int i = 0; i < issueResult.Locations.Count; i++)
            {
                PrefabIssueLocation location = issueResult.Locations[i];
                if (location == null)
                {
                    continue;
                }

                string hierarchyPath = location.HierarchyPath;
                Button locationButton = new Button(
                    delegate
                    {
                        locatePrefabIssueRequested?.Invoke(
                            assetPath,
                            hierarchyPath);
                    });
                locationButton.tooltip = hierarchyPath;
                locationButton.AddToClassList(
                    "scan-issue-location-button");

                Label pathLabel = new Label(
                    string.IsNullOrEmpty(hierarchyPath)
                        ? "Prefab 根节点"
                        : hierarchyPath);
                pathLabel.AddToClassList("scan-issue-location-path");
                locationButton.Add(pathLabel);

                Label descriptionLabel = new Label(
                    string.IsNullOrEmpty(location.Description)
                        ? "-"
                        : location.Description);
                descriptionLabel.AddToClassList(
                    "scan-issue-location-description");
                locationButton.Add(descriptionLabel);
                locationList.Add(locationButton);
            }
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
        /// 获取问题条目的严重度和名称标题。
        /// </summary>
        private string GetIssueTitle(AssetIssueResult issueResult)
        {
            if (issueResult == null || issueResult.IssueInfo == null)
            {
                return "未知问题";
            }

            IssueInfo issueInfo = issueResult.IssueInfo;
            string displayName = string.IsNullOrEmpty(issueInfo.displayName)
                ? issueInfo.issueInfoId
                : issueInfo.displayName;
            return issueInfo.issueLevel + " | " + displayName;
        }

        /// <summary>
        /// 获取问题条目的扣分与规则标识文本。
        /// </summary>
        private string GetIssueMetadata(AssetIssueResult issueResult)
        {
            if (issueResult == null || issueResult.IssueInfo == null)
            {
                return "扣分：-    规则：-";
            }

            string ruleId = issueResult.RuleConfig == null
                ? "-"
                : issueResult.RuleConfig.RuleId;
            return "扣分：" + issueResult.IssueInfo.deductScore +
                   "    规则：" + ruleId;
        }

        /// <summary>
        /// 获取问题条目的具体说明文本。
        /// </summary>
        private string GetIssueMessage(AssetIssueResult issueResult)
        {
            return issueResult == null ||
                   issueResult.IssueInfo == null ||
                   string.IsNullOrEmpty(issueResult.IssueInfo.issueMessage)
                ? "-"
                : issueResult.IssueInfo.issueMessage;
        }

        /// <summary>
        /// 按资源等级为详情标签应用质量颜色类。
        /// </summary>
        private void ApplyQualityColorClass(
            VisualElement element,
            EAssetGrade grade)
        {
            ClearQualityColorClasses(element);
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
        /// 移除详情标签上一次资源留下的质量颜色类。
        /// </summary>
        private void ClearQualityColorClasses(VisualElement element)
        {
            element.RemoveFromClassList("scan-quality-a");
            element.RemoveFromClassList("scan-quality-b");
            element.RemoveFromClassList("scan-quality-c");
            element.RemoveFromClassList("scan-quality-d");
        }

        /// <summary>
        /// 按问题严重度为问题标题应用风险颜色类。
        /// </summary>
        private void ApplySeverityColorClass(
            VisualElement element,
            bool hasIssue,
            EIssueLevel issueLevel)
        {
            element.RemoveFromClassList("scan-severity-none");
            element.RemoveFromClassList("scan-severity-info");
            element.RemoveFromClassList("scan-severity-warning");
            element.RemoveFromClassList("scan-severity-error");
            element.RemoveFromClassList("scan-severity-blocker");

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

        /// <summary>
        /// 在 Unity Project 面板中选中并 Ping 指定资源。
        /// </summary>
        internal static void SelectAssetInProject(string assetPath)
        {
            UnityEngine.Object asset =
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    ScanToolUtility.NormalizePath(assetPath));
            if (asset == null)
            {
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        /// <summary>
        /// 在系统文件管理器中打开指定资源所在目录。
        /// </summary>
        internal static void OpenAssetFolder(string assetPath)
        {
            assetPath = ScanToolUtility.NormalizePath(assetPath);
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            string projectRootPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, ".."));
            string absoluteAssetPath = Path.GetFullPath(
                Path.Combine(projectRootPath, assetPath));
            string absoluteFolderPath = File.Exists(absoluteAssetPath)
                ? Path.GetDirectoryName(absoluteAssetPath)
                : absoluteAssetPath;

            if (!string.IsNullOrEmpty(absoluteFolderPath) &&
                Directory.Exists(absoluteFolderPath))
            {
                EditorUtility.OpenWithDefaultApp(absoluteFolderPath);
            }
        }

        /// <summary>
        /// 打开问题所在 Prefab，并延迟选中对应层级对象。
        /// </summary>
        internal static void LocatePrefabIssue(
            string assetPath,
            string hierarchyPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                assetPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog(
                    "定位对象",
                    "无法加载 Prefab：" + assetPath,
                    "确定");
                return;
            }

            AssetDatabase.OpenAsset(prefab);
            EditorApplication.delayCall +=
                delegate
                {
                    SelectPrefabIssueLocation(
                        assetPath,
                        hierarchyPath);
                };
        }

        /// <summary>
        /// 在已打开的 Prefab Stage 中选中指定层级路径对象。
        /// </summary>
        private static void SelectPrefabIssueLocation(
            string assetPath,
            string hierarchyPath)
        {
            PrefabStage prefabStage =
                PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null || prefabStage.assetPath != assetPath)
            {
                EditorUtility.DisplayDialog(
                    "定位对象",
                    "未能打开目标 Prefab：" + assetPath,
                    "确定");
                return;
            }

            Transform target = FindPrefabTransform(
                prefabStage.prefabContentsRoot.transform,
                hierarchyPath);
            if (target == null)
            {
                EditorUtility.DisplayDialog(
                    "定位对象",
                    "扫描后的 Prefab 已变化，找不到对象：" + hierarchyPath,
                    "确定");
                return;
            }

            Selection.activeGameObject = target.gameObject;
            EditorGUIUtility.PingObject(target.gameObject);
        }

        /// <summary>
        /// 依据包含同级索引的相对层级路径查找 Prefab Stage 对象。
        /// </summary>
        private static Transform FindPrefabTransform(
            Transform prefabRoot,
            string hierarchyPath)
        {
            if (prefabRoot == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(hierarchyPath))
            {
                return prefabRoot;
            }

            string[] segments = hierarchyPath.Split('/');
            Transform current = prefabRoot;
            for (int i = 0; i < segments.Length; i++)
            {
                int bracketIndex = segments[i].LastIndexOf('[');
                int closeBracketIndex = segments[i].LastIndexOf(']');
                if (bracketIndex < 0 ||
                    closeBracketIndex <= bracketIndex + 1)
                {
                    return null;
                }

                int siblingIndex;
                if (!int.TryParse(
                        segments[i].Substring(
                            bracketIndex + 1,
                            closeBracketIndex - bracketIndex - 1),
                        out siblingIndex) ||
                    siblingIndex < 0 ||
                    siblingIndex >= current.childCount)
                {
                    return null;
                }

                current = current.GetChild(siblingIndex);
            }

            return current;
        }
    }
}
