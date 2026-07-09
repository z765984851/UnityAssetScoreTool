using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    public partial class ScanWindow
    {
        /// <summary>
        /// 绑定右侧详情面板。
        /// 这里只持有后续刷新详情时需要反复访问的控件。
        /// 按钮只在绑定时注册事件，不作为字段保存。
        /// </summary>
        private void BindDetailPanelUI()
        {
            detailAssetNameLabel = rootVisualElement.Q<Label>("DetailAssetNameLabel");
            detailAssetPathLabel = rootVisualElement.Q<Label>("DetailAssetPathLabel");
            detailAssetTypeLabel = rootVisualElement.Q<Label>("DetailAssetTypeLabel");
            detailProfileLabel = rootVisualElement.Q<Label>("DetailProfileLabel");
            detailScoreLabel = rootVisualElement.Q<Label>("DetailScoreLabel");
            detailGradeLabel = rootVisualElement.Q<Label>("DetailGradeLabel");
            detailIssueCountLabel = rootVisualElement.Q<Label>("DetailIssueCountLabel");

            issueListContainer = rootVisualElement.Q<VisualElement>("IssueListContainer");
            dependencyListContainer = rootVisualElement.Q<VisualElement>("DependencyListContainer");

            Button pingMainAssetButton = rootVisualElement.Q<Button>("PingMainAssetButton");
            pingMainAssetButton.text = "选中资源";
            pingMainAssetButton.clicked += OnSelectCurrentAssetClicked;

            Button openFolderButton = rootVisualElement.Q<Button>("OpenFolderButton");
            openFolderButton.text = "打开文件夹";
            openFolderButton.clicked += OnOpenAssetFolderClicked;
        }

        /// <summary>
        /// 清空详情面板。
        /// </summary>
        private void RefreshDetailPanelEmpty()
        {
            selectedDetailAssetPath = string.Empty;

            detailAssetNameLabel.text = "资源名：未选择";
            detailAssetPathLabel.text = "路径：-";
            detailAssetTypeLabel.text = "类型：-";
            detailProfileLabel.text = "Profile：-";
            detailScoreLabel.text = "分数：-";
            detailGradeLabel.text = "等级：-";
            detailIssueCountLabel.text = "问题数：0";

            issueListContainer.Clear();
            issueListContainer.Add(new Label("暂无问题"));

            dependencyListContainer.Clear();
            dependencyListContainer.Add(new Label("暂无依赖"));
        }

        /// <summary>
        /// 选中一个资源并刷新详情。
        /// </summary>
        private void SelectAssetDetail(AssetContext context)
        {
            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                RefreshDetailPanelEmpty();
                return;
            }

            selectedDetailAssetPath = ScanToolUtility.NormalizePath(context.AssetPath);

            AssetScanResult result;
            bool hasResult = TryGetResult(context, out result);

            detailAssetNameLabel.text = "资源名：" + GetAssetName(context);
            detailAssetPathLabel.text = "路径：" + context.AssetPath;
            detailAssetTypeLabel.text = "类型：" + GetAssetTypeText(context);
            detailProfileLabel.text = "Profile：" + GetSelectedProfileText();
            detailScoreLabel.text = "分数：" + (hasResult ? result.Score.ToString() : "-");
            detailGradeLabel.text = "等级：" + (hasResult ? result.Grade.ToString() : "-");

            if (hasResult)
            {
                detailIssueCountLabel.text =
                    "问题数：自身 " +
                    result.SelfIssueCount +
                    " / 汇总 " +
                    result.IssueCount;
            }
            else
            {
                detailIssueCountLabel.text = "问题数：-";
            }

            RefreshDetailIssueList(context);
            RefreshDetailDependencyList(context);
        }

        /// <summary>
        /// 扫描完成后默认选中第一条结果。
        /// </summary>
        private void SelectFirstResultDetail()
        {
            if (scanResults == null || scanResults.Count == 0)
            {
                RefreshDetailPanelEmpty();
                return;
            }

            for (int i = 0; i < scanResults.Count; i++)
            {
                AssetScanResult result = scanResults[i];
                if (result.Context == null)
                {
                    continue;
                }

                SelectAssetDetail(result.Context);
                return;
            }

            RefreshDetailPanelEmpty();
        }

        /// <summary>
        /// 刷新资源自身问题列表。
        /// 这里只显示自身问题，不展开依赖资源的问题，避免和汇总分重复。
        /// </summary>
        private void RefreshDetailIssueList(AssetContext context)
        {
            issueListContainer.Clear();

            List<AssetIssueResult> issues = GetSelfIssues(context);

            if (issues.Count == 0)
            {
                AssetScanResult result;
                bool hasResult = TryGetResult(context, out result);

                if (hasResult && result.IssueCount > 0)
                {
                    issueListContainer.Add(new Label("该资源自身暂无问题，问题来自依赖资源。请在左侧展开依赖查看。"));
                }
                else
                {
                    issueListContainer.Add(new Label("暂无问题"));
                }

                return;
            }

            for (int i = 0; i < issues.Count; i++)
            {
                issueListContainer.Add(CreateIssueElement(issues[i]));
            }
        }

        /// <summary>
        /// 刷新直接依赖列表。
        /// 这里只显示直接依赖，并标记依赖是否存在问题。
        /// 不在详情面板里递归展开依赖问题，避免和左侧树形结果重复。
        /// </summary>
        private void RefreshDetailDependencyList(AssetContext context)
        {
            dependencyListContainer.Clear();

            if (context.Dependencies == null || context.Dependencies.Count == 0)
            {
                dependencyListContainer.Add(new Label("暂无依赖"));
                return;
            }

            for (int i = 0; i < context.Dependencies.Count; i++)
            {
                AssetContext dependency = context.Dependencies[i];

                if (dependency == null || string.IsNullOrEmpty(dependency.AssetPath))
                {
                    continue;
                }

                Label label = new Label(GetDependencyDisplayText(dependency));
                label.tooltip = dependency.AssetPath;
                label.style.whiteSpace = WhiteSpace.NoWrap;
                label.style.overflow = Overflow.Hidden;
                label.style.textOverflow = TextOverflow.Ellipsis;

                dependencyListContainer.Add(label);
            }
        }

        /// <summary>
        /// 获取依赖资源在详情面板中的显示文本。
        /// </summary>
        private string GetDependencyDisplayText(AssetContext dependency)
        {
            if (dependency == null || string.IsNullOrEmpty(dependency.AssetPath))
            {
                return "-";
            }

            AssetScanResult result;
            if (!TryGetResult(dependency, out result))
            {
                return dependency.AssetPath;
            }

            if (result.IssueCount <= 0)
            {
                return dependency.AssetPath;
            }

            return dependency.AssetPath + "    问题数：" + result.IssueCount;
        }

        private List<AssetIssueResult> GetSelfIssues(AssetContext context)
        {
            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return new List<AssetIssueResult>();
            }

            if (currentReport == null || currentReport.SourceData == null)
            {
                return new List<AssetIssueResult>();
            }

            return currentReport.SourceData.GetIssues(context.AssetPath);
        }

        private VisualElement CreateIssueElement(AssetIssueResult issueResult)
        {
            VisualElement root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;
            root.style.marginBottom = 6;
            root.style.paddingLeft = 4;
            root.style.paddingRight = 4;
            root.style.paddingTop = 4;
            root.style.paddingBottom = 4;

            Label titleLabel = new Label(GetIssueTitle(issueResult));
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(titleLabel);

            Label messageLabel = new Label(GetIssueMessage(issueResult));
            messageLabel.style.whiteSpace = WhiteSpace.Normal;
            root.Add(messageLabel);

            return root;
        }

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

            string ruleId = issueResult.RuleConfig == null
                ? "-"
                : issueResult.RuleConfig.RuleId;

            return issueInfo.issueLevel +
                   " | " +
                   displayName +
                   " | 扣分：" +
                   issueInfo.deductScore +
                   " | 规则：" +
                   ruleId;
        }

        private string GetIssueMessage(AssetIssueResult issueResult)
        {
            if (issueResult == null || issueResult.IssueInfo == null)
            {
                return "-";
            }

            return string.IsNullOrEmpty(issueResult.IssueInfo.issueMessage)
                ? "-"
                : issueResult.IssueInfo.issueMessage;
        }

        private string GetSelectedProfileText()
        {
            if (selectedProfile == null)
            {
                return "-";
            }

            return string.IsNullOrEmpty(selectedProfile.DisplayName)
                ? selectedProfile.ProfileId
                : selectedProfile.DisplayName;
        }

        /// <summary>
        /// 在 Unity Project 面板中选中当前详情资源。
        /// </summary>
        private void OnSelectCurrentAssetClicked()
        {
            string assetPath = ScanToolUtility.NormalizePath(selectedDetailAssetPath);

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

            if (asset == null)
            {
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        /// <summary>
        /// 在系统文件管理器中打开当前详情资源所在文件夹。
        /// 如果当前选中的是文件，则打开文件所在目录。
        /// 如果当前选中的是文件夹，则直接打开该文件夹。
        /// </summary>
        private void OnOpenAssetFolderClicked()
        {
            string assetPath = ScanToolUtility.NormalizePath(selectedDetailAssetPath);

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            string projectRootPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, ".."));

            string absoluteAssetPath = Path.GetFullPath(
                Path.Combine(projectRootPath, assetPath));

            string absoluteFolderPath = absoluteAssetPath;

            if (File.Exists(absoluteAssetPath))
            {
                absoluteFolderPath = Path.GetDirectoryName(absoluteAssetPath);
            }

            if (string.IsNullOrEmpty(absoluteFolderPath))
            {
                return;
            }

            if (!Directory.Exists(absoluteFolderPath))
            {
                return;
            }

            EditorUtility.OpenWithDefaultApp(absoluteFolderPath);
        }
    }
}