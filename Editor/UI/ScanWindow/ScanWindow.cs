using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    public class ScanWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset visualTreeAsset = default;

        /// <summary>
        /// 当前窗口的扫描器。
        /// </summary>
        private AssetScoreToolScanner scanner = null;

        /// <summary>
        /// 当前加载到的所有 Profile。
        /// Dropdown 只保存显示文本，这里保存真实配置对象，保证 index 可以映射回 Profile。
        /// </summary>
        private List<ScoreRuleProfileConfig> profiles = new List<ScoreRuleProfileConfig>();

        /// <summary>
        /// 当前选中的 Profile。
        /// 点击“开始评分”时会传给 scanner.StartScanAsync。
        /// </summary>
        private ScoreRuleProfileConfig selectedProfile;

        /// <summary>
        /// 从 Project 右键菜单传入的待扫描路径。
        /// 如果窗口 UI 尚未创建，先缓存到这里，CreateGUI 后再写入 PathTextField。
        /// </summary>
        private string pendingScanPath;

        /// <summary>
        /// 当前扫描生成的最终评分报告。
        /// UI、详情面板、导出功能都应该读取这份 Report，而不是直接读取 Scanner。
        /// </summary>
        private AssetScoreReport currentReport;

        /// <summary>
        /// 当前扫描计时器。
        /// 用于底部状态栏显示扫描耗时。
        /// </summary>
        private System.Diagnostics.Stopwatch scanStopwatch = new System.Diagnostics.Stopwatch();

        private ScanResultListView resultListView;
        private ScanDetailPanelView detailPanelView;

        #region UI

        private DropdownField profileDropdown;
        private TextField pathTextField;

        private Button selectFolderButton;
        private Button selectFileButton;
        private Button startScanButton;
        private Button exportJsonButton;
        private Button exportExcelButton;
        private Button openExportFolderButton;

        private Label scanTimeLabel;
        private Label visibleCountLabel;
        private Label issueCountLabel;
        private ProgressBar scanProgressBar;

        #endregion

        /// <summary>
        /// 打开资源评分窗口并设置窗口标题。
        /// </summary>
        [MenuItem("Tools/Asset Score Tool/资源评分")]
        public static void ShowWindow()
        {
            ScanWindow wnd = GetWindow<ScanWindow>();
            wnd.titleContent = new GUIContent("ScanWindow");
        }

        public void CreateGUI()
        {
            RuleApplyConfigResolver.EnsureProjectConfig();

            VisualElement root = rootVisualElement;
            root.style.flexGrow = new StyleFloat(1);

            VisualElement uxml = visualTreeAsset.Instantiate();
            uxml.style.flexGrow = new StyleFloat(1);
            root.Add(uxml);

            InitScanner();
            BindUI();
            FreshUI();
        }

        /// <summary>
        /// 初始化扫描器。
        /// 当前检测页面不提供刷新配置入口，窗口打开时加载一次配置。
        /// </summary>
        private void InitScanner()
        {
            scanner = new AssetScoreToolScanner();
            scanner.LoadData();
        }

        /// <summary>
        /// 统一绑定 UI 控件。
        /// 每个区域的绑定逻辑放到对应 partial 文件里。
        /// </summary>
        private void BindUI()
        {
            BindTopInputUI();
            BindStatusBarUI();

            detailPanelView = new ScanDetailPanelView(
                rootVisualElement,
                ScanDetailPanelView.SelectAssetInProject,
                ScanDetailPanelView.OpenAssetFolder,
                ScanDetailPanelView.LocatePrefabIssue);
            resultListView = new ScanResultListView(
                rootVisualElement,
                detailPanelView.ShowAsset);
        }

        /// <summary>
        /// 刷新整个窗口 UI。
        /// </summary>
        /// <summary>
        /// 刷新整个窗口 UI。
        /// </summary>
        private void FreshUI()
        {
            RefreshProfileDropdown();
            RefreshStatusBarIdle();
            detailPanelView.SetReport(
                currentReport,
                GetSelectedProfileText());
            resultListView.SetReport(currentReport);
        }

        /// <summary>
        /// 从外部设置扫描路径。
        /// 用于 Project 面板右键资源或文件夹后，打开窗口并自动填充检测路径。
        /// </summary>
        public void SetScanPath(string assetPath)
        {
            pendingScanPath = ScanToolUtility.NormalizePath(assetPath);

            if (pathTextField != null)
            {
                pathTextField.value = pendingScanPath;
            }
        }

        /// <summary>
        /// 窗口关闭或脚本重载时，取消未完成的扫描。
        /// 避免异步扫描完成后继续访问已经关闭的窗口。
        /// </summary>
        private void OnDisable()
        {
            if (scanner != null)
            {
                scanner.CancelScan();
            }

            SetScanInputEnabled(true);
            FreshStartScanBtnState();
        }
        private static readonly Color StartScanButtonColor = new Color(0.20f, 0.60f, 0.25f);
        private static readonly Color CancelScanButtonColor = new Color(0.75f, 0.18f, 0.16f);
        private static readonly Color CancelingScanButtonColor = new Color(0.55f, 0.12f, 0.10f);
        private static readonly Color ScanButtonTextColor = Color.white;

        /// <summary>
        /// 绑定顶部输入区控件。
        /// 这里包含 Profile、检测路径、选择文件夹、选择文件和开始评分。
        /// </summary>
        private void BindTopInputUI()
        {
            profileDropdown = rootVisualElement.Q<DropdownField>("ProfileDropdown");
            profileDropdown.RegisterValueChangedCallback(OnProfileChanged);

            pathTextField = rootVisualElement.Q<TextField>("PathTextField");

            selectFolderButton = rootVisualElement.Q<Button>("SelectFolderButton");
            selectFolderButton.clicked += OnSelectFolderClicked;

            selectFileButton = rootVisualElement.Q<Button>("SelectFileButton");
            selectFileButton.clicked += OnSelectFileClicked;

            startScanButton = rootVisualElement.Q<Button>("StartScanButton");
            startScanButton.clicked += OnStartScanClicked;

            exportJsonButton = rootVisualElement.Q<Button>(
                "ExportJsonButton");
            exportJsonButton.clicked += OnExportJsonClicked;

            exportExcelButton = rootVisualElement.Q<Button>(
                "ExportExcelButton");
            exportExcelButton.clicked += OnExportExcelClicked;

            openExportFolderButton = rootVisualElement.Q<Button>(
                "OpenExportFolderButton");
            openExportFolderButton.clicked += OnOpenExportFolderClicked;
            SetExportButtonsEnabled(false);

            FreshStartScanBtnState();
            ApplyPendingScanPath();
        }

        /// <summary>
        /// 刷新 Profile 下拉框。
        /// Profile 数据来自 AssetScoreToolScanner，不在 UI 层直接扫描配置资产。
        /// </summary>
        private void RefreshProfileDropdown()
        {
            if (scanner == null || profileDropdown == null)
            {
                return;
            }

            profiles.Clear();

            List<ScoreRuleProfileConfig> loadedProfiles = scanner.GetProfiles();
            List<string> choices = new List<string>();

            for (int i = 0; i < loadedProfiles.Count; i++)
            {
                ScoreRuleProfileConfig profile = loadedProfiles[i];

                if (profile == null)
                {
                    continue;
                }

                string displayName = string.IsNullOrEmpty(profile.DisplayName)
                    ? profile.ProfileId
                    : profile.DisplayName;

                profiles.Add(profile);
                choices.Add(displayName);
            }

            profileDropdown.choices = choices;

            if (choices.Count > 0)
            {
                profileDropdown.index = 0;
                selectedProfile = profiles[0];
            }
            else
            {
                profileDropdown.value = string.Empty;
                selectedProfile = null;
            }
        }

        private void OnProfileChanged(ChangeEvent<string> evt)
        {
            int index = profileDropdown.index;

            if (index < 0 || index >= profiles.Count)
            {
                selectedProfile = null;
                return;
            }

            selectedProfile = profiles[index];
        }

        /// <summary>
        /// 选择 Unity 项目 Assets 目录下的文件夹。
        /// 文件夹路径会写入 PathTextField，后续 scanner 会递归扫描该文件夹下的资源。
        /// </summary>
        private void OnSelectFolderClicked()
        {
            string selectedPath = EditorUtility.OpenFolderPanel(
                "选择检测文件夹",
                Application.dataPath,
                string.Empty);

            if (string.IsNullOrEmpty(selectedPath))
            {
                return;
            }

            string unityAssetPath;
            if (!TryConvertToUnityAssetPath(selectedPath, out unityAssetPath))
            {
                EditorUtility.DisplayDialog(
                    "Asset Score Tool",
                    "请选择当前项目 Assets 目录下的文件夹。",
                    "确定");
                return;
            }

            if (pathTextField != null)
            {
                pathTextField.value = unityAssetPath;
            }
        }

        /// <summary>
        /// 选择 Unity 项目 Assets 目录下的单个资源文件。
        /// 文件路径会写入 PathTextField，后续 scanner 会只扫描这个资源及其依赖。
        /// </summary>
        private void OnSelectFileClicked()
        {
            string selectedPath = EditorUtility.OpenFilePanel(
                "选择检测资源",
                Application.dataPath,
                string.Empty);

            if (string.IsNullOrEmpty(selectedPath))
            {
                return;
            }

            string unityAssetPath;
            if (!TryConvertToUnityAssetPath(selectedPath, out unityAssetPath))
            {
                EditorUtility.DisplayDialog(
                    "Asset Score Tool",
                    "请选择当前项目 Assets 目录下的资源文件。",
                    "确定");
                return;
            }

            if (pathTextField != null)
            {
                pathTextField.value = unityAssetPath;
            }
        }

        /// <summary>
        /// 把系统绝对路径转换成 Unity 资源路径。
        /// 例如：D:/Project/Game/Assets/Art/a.png -> Assets/Art/a.png。
        /// scanner 只接受 Assets 开头的 Unity 资源路径。
        /// </summary>
        private bool TryConvertToUnityAssetPath(string absolutePath, out string unityAssetPath)
        {
            unityAssetPath = string.Empty;

            if (string.IsNullOrEmpty(absolutePath))
            {
                return false;
            }

            absolutePath = absolutePath.Replace("\\", "/");

            string assetsPath = Application.dataPath.Replace("\\", "/");

            if (!absolutePath.StartsWith(assetsPath))
            {
                return false;
            }

            unityAssetPath = "Assets" + absolutePath.Substring(assetsPath.Length);
            unityAssetPath = ScanToolUtility.NormalizePath(unityAssetPath);

            return true;
        }

        /// <summary>
        /// 开始或取消资产评分扫描。
        ///
        /// 按钮有三种状态：
        /// 1. 开始评分：启动扫描。
        /// 2. 取消评分：请求取消扫描。
        /// 3. 取消中...：等待 Scanner 响应取消。
        /// </summary>
        private async void OnStartScanClicked()
        {
            try
            {
                if (scanner == null)
                {
                    EditorUtility.DisplayDialog(
                        "Asset Score Tool",
                        "扫描器未初始化。",
                        "确定");

                    return;
                }

                if (scanner.IsRunning)
                {
                    CancelCurrentScan();
                    return;
                }

                if (selectedProfile == null)
                {
                    EditorUtility.DisplayDialog(
                        "Asset Score Tool",
                        "请先选择 Profile。",
                        "确定");

                    return;
                }

                if (pathTextField == null ||
                    string.IsNullOrEmpty(
                        pathTextField.value))
                {
                    EditorUtility.DisplayDialog(
                        "Asset Score Tool",
                        "请先选择或输入检测路径。",
                        "确定");

                    return;
                }

                string scanPath =
                    ScanToolUtility.NormalizePath(
                        pathTextField.value);

                SetScanInputEnabled(false);

                scanStopwatch.Restart();
                RefreshStatusBarScanStarted();

                currentReport = null;
                SetExportButtonsEnabled(false);
                detailPanelView.SetReport(
                    null,
                    GetSelectedProfileText());
                resultListView.ShowScanning();

                Task<AssetScanData> scanTask =
                    scanner.StartScanAsync(
                        scanPath,
                        selectedProfile,
                        OnScanProgress);

                FreshStartScanBtnState();

                AssetScanData scanData =
                    await scanTask;

                currentReport =
                    AssetScoreReportBuilder.Build(
                        scanData,
                        scanner.GetRuleApplyConfig());

                SetExportButtonsEnabled(true);
                detailPanelView.SetReport(
                    currentReport,
                    GetSelectedProfileText());
                resultListView.SetReport(currentReport);

                scanStopwatch.Stop();
                RefreshStatusBarCompleted();
            }
            catch (OperationCanceledException)
            {
                currentReport = null;
                SetExportButtonsEnabled(false);
                detailPanelView.SetReport(
                    null,
                    GetSelectedProfileText());
                resultListView.Clear();

                scanStopwatch.Stop();
                RefreshStatusBarCanceled();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                currentReport = null;
                SetExportButtonsEnabled(false);
                detailPanelView.SetReport(
                    null,
                    GetSelectedProfileText());
                resultListView.Clear();

                scanStopwatch.Stop();
                RefreshStatusBarFailed();

                EditorUtility.DisplayDialog(
                    "Asset Score Tool",
                    "扫描过程中发生错误，请查看 Console。\n" +
                    exception.Message,
                    "确定");
            }
            finally
            {
                SetScanInputEnabled(true);
                FreshStartScanBtnState();
            }
        }

        /// <summary>
        /// 取消当前正在执行的扫描。
        /// 当前按钮在扫描中会从“取消评分”切换为“取消中”。
        /// </summary>
        private void CancelCurrentScan()
        {
            if (scanner == null)
            {
                return;
            }

            scanner.CancelScan();
            FreshStartScanBtnState();
            RefreshStatusBarCanceling();
        }

        /// <summary>
        /// 根据 Scanner 当前状态刷新开始按钮。
        /// </summary>
        private void FreshStartScanBtnState()
        {
            if (startScanButton == null)
            {
                return;
            }

            if (scanner == null)
            {
                SetStartScanButtonVisual("开始评分", StartScanButtonColor, true);
                return;
            }

            switch (scanner.CurrentStage)
            {
                case EScanStage.BuildDependencies:
                case EScanStage.CheckRules:
                    SetStartScanButtonVisual("取消评分", CancelScanButtonColor, true);
                    break;

                case EScanStage.Canceling:
                    SetStartScanButtonVisual("取消中...", CancelingScanButtonColor, false);
                    break;

                case EScanStage.Idle:
                case EScanStage.Finished:
                default:
                    SetStartScanButtonVisual("开始评分", StartScanButtonColor, true);
                    break;
            }
        }

        /// <summary>
        /// 设置开始评分按钮的文本、颜色和启用状态。
        /// </summary>
        private void SetStartScanButtonVisual(
            string text,
            Color backgroundColor,
            bool enabled)
        {
            if (startScanButton == null)
            {
                return;
            }

            UIElementUtility.SetText(startScanButton, text);
            UIElementUtility.SetEnabled(startScanButton, enabled);
            startScanButton.style.backgroundColor = backgroundColor;
            startScanButton.style.color = ScanButtonTextColor;
        }

        /// <summary>
        /// 设置扫描参数输入区是否可编辑。
        /// 扫描中锁定 Profile、路径和选择按钮，但保留开始按钮可点击，用于取消扫描。
        /// </summary>
        private void SetScanInputEnabled(bool enabled)
        {
            UIElementUtility.SetEnabled(profileDropdown, enabled);
            UIElementUtility.SetEnabled(pathTextField, enabled);
            UIElementUtility.SetEnabled(selectFolderButton, enabled);
            UIElementUtility.SetEnabled(selectFileButton, enabled);
            UIElementUtility.SetEnabled(openExportFolderButton, enabled);
        }

        /// <summary>
        /// 根据当前报告可用状态启用或禁用两种报告导出按钮。
        /// </summary>
        private void SetExportButtonsEnabled(bool enabled)
        {
            UIElementUtility.SetEnabled(exportJsonButton, enabled);
            UIElementUtility.SetEnabled(exportExcelButton, enabled);
        }

        /// <summary>
        /// 导出当前报告为保留资源依赖关系和问题明细的 JSON 文件。
        /// </summary>
        private void OnExportJsonClicked()
        {
            ExportCurrentReport(
                "JSON",
                "json",
                AssetScoreReportExporter.ExportJson);
        }

        /// <summary>
        /// 导出当前报告为包含多个工作表的 Excel 文件。
        /// </summary>
        private void OnExportExcelClicked()
        {
            ExportCurrentReport(
                "Excel",
                "xlsx",
                AssetScoreReportExporter.ExportExcel);
        }

        /// <summary>
        /// 在系统文件管理器中打开当前报告的默认导出目录。
        /// </summary>
        private void OnOpenExportFolderClicked()
        {
            OpenExportDirectory(GetExportDirectory());
        }

        /// <summary>
        /// 打开保存对话框并将当前最终报告写入用户选择的文件。
        /// </summary>
        private void ExportCurrentReport(
            string formatName,
            string extension,
            Func<AssetScoreReport, string, string> exportAction)
        {
            if (currentReport == null)
            {
                EditorUtility.DisplayDialog(
                    "Asset Score Tool",
                    "请先完成一次扫描后再导出报告。",
                    "确定");
                return;
            }

            string defaultDirectory = GetExportDirectory();

            string defaultFileName =
                "AssetScoreReport_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filePath = EditorUtility.SaveFilePanel(
                "导出 " + formatName + " 报告",
                defaultDirectory,
                defaultFileName,
                extension);

            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            try
            {
                string exportedFilePaths = exportAction(
                    currentReport,
                    filePath);
                int selectedButton = EditorUtility.DisplayDialogComplex(
                    "Asset Score Tool",
                    formatName + " 报告已导出到：\n" + exportedFilePaths,
                    "打开存放文件夹",
                    "确定",
                    string.Empty);

                if (selectedButton == 0)
                {
                    OpenExportDirectory(Path.GetDirectoryName(filePath));
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Asset Score Tool",
                    formatName + " 报告导出失败，请查看 Console。\n" +
                    exception.Message,
                    "确定");
            }
        }

        /// <summary>
        /// 获取有效的导出目录；自定义目录不可用时回退到项目根目录的 docs 文件夹。
        /// </summary>
        private string GetExportDirectory()
        {
            UserSettingConfig projectConfig =
                RuleApplyConfigResolver.GetProjectConfig();
            string exportDirectory = projectConfig == null
                ? string.Empty
                : projectConfig.ExportDefaultFolderPath;

            if (!Directory.Exists(exportDirectory))
            {
                string projectRoot = Directory.GetParent(
                    Application.dataPath).FullName;
                exportDirectory = Path.Combine(projectRoot, "docs");
                Directory.CreateDirectory(exportDirectory);
            }

            return exportDirectory;
        }

        /// <summary>
        /// 在系统文件管理器中打开指定的有效导出目录。
        /// </summary>
        private void OpenExportDirectory(string exportDirectory)
        {
            if (string.IsNullOrEmpty(exportDirectory) ||
                !Directory.Exists(exportDirectory))
            {
                return;
            }

            EditorUtility.OpenWithDefaultApp(exportDirectory);
        }

        /// <summary>
        /// 扫描进度变化。
        /// </summary>
        private void OnScanProgress(string stageText, int currentCount, int totalCount)
        {
            RefreshStatusBarProgress(stageText, currentCount, totalCount);
        }

        /// <summary>
        /// 如果窗口是通过 Project 右键菜单打开的，这里把外部传入的路径写入检测路径输入框。
        /// </summary>
        private void ApplyPendingScanPath()
        {
            if (string.IsNullOrEmpty(pendingScanPath))
            {
                return;
            }

            if (pathTextField != null)
            {
                pathTextField.value = pendingScanPath;
            }
        }
        private const float BuildDependencyProgressEnd = 50f;
        private const float CheckRuleProgressStart = 50f;
        private int scanProgressCurrentCount;
        private int scanProgressTotalCount;
        private float scanProgressValue;

        private void BindStatusBarUI()
        {
            scanProgressBar = rootVisualElement.Q<ProgressBar>("ScanProgressBar");
            scanTimeLabel = rootVisualElement.Q<Label>("ScanTimeLabel");
            visibleCountLabel = rootVisualElement.Q<Label>("VisibleCountLabel");
            issueCountLabel = rootVisualElement.Q<Label>("IssueCountLabel");

            InitProgressBar();
        }

        private void InitProgressBar()
        {
            if (scanProgressBar == null)
            {
                return;
            }

            scanProgressBar.lowValue = 0;
            scanProgressBar.highValue = 100;
            scanProgressBar.value = 0;
            scanProgressBar.title = "0%";
        }

        private void RefreshStatusBarIdle()
        {
            scanProgressValue = 0f;
            scanProgressCurrentCount = 0;
            scanProgressTotalCount = 0;

            SetProgressBar(0, "未扫描    0%");
            SetScanTimeText(0);
            SetVisibleCountText(0);
            SetIssueCountText(0);
        }

        private void RefreshStatusBarProgress(
            string stageText,
            int currentCount,
            int totalCount)
        {
            float progress = CalculateOverallProgress(
                stageText,
                currentCount,
                totalCount);

            // 依赖构建阶段 totalCount 会动态增长，可能导致百分比回退。
            // 这里保证进度条只前进，不倒退。
            if (progress < scanProgressValue)
            {
                progress = scanProgressValue;
            }

            scanProgressValue = progress;
            scanProgressCurrentCount = currentCount;
            scanProgressTotalCount = totalCount;

            string progressBarTitle = GetProgressBarTitle(
                "扫描中 - " + stageText,
                currentCount,
                totalCount,
                progress);

            SetProgressBar(progress, progressBarTitle);
            SetScanTimeText(scanStopwatch.ElapsedMilliseconds);

            SetVisibleCountText(0);
            SetIssueCountText(0);
        }

        /// <summary>
        /// 获取进度条显示文本。
        /// 显示状态、当前阶段实际数量，以及整体扫描百分比。
        /// </summary>
        private string GetProgressBarTitle(
            string statusText,
            int currentCount,
            int totalCount,
            float progress)
        {
            int percent = Mathf.RoundToInt(progress);

            if (totalCount > 0)
            {
                return statusText + "    " + currentCount + " / " + totalCount + "    " + percent + "%";
            }

            return statusText + "    " + percent + "%";
        }

        private void RefreshStatusBarCompleted()
        {
            scanProgressValue = 100f;

            SetProgressBar(100f, "扫描完成    100%");
            SetScanTimeText(scanStopwatch.ElapsedMilliseconds);
            SetVisibleCountText(GetCurrentVisibleAssetCount());
            SetIssueCountText(GetCurrentIssueCount());
        }

        private void RefreshStatusBarCanceled()
        {
            SetProgressBar(
                scanProgressValue,
                GetProgressBarTitle(
                    "扫描已取消",
                    scanProgressCurrentCount,
                    scanProgressTotalCount,
                    scanProgressValue));

            SetScanTimeText(scanStopwatch.ElapsedMilliseconds);
        }

        private void RefreshStatusBarFailed()
        {
            SetProgressBar(
                scanProgressValue,
                GetProgressBarTitle(
                    "扫描失败",
                    scanProgressCurrentCount,
                    scanProgressTotalCount,
                    scanProgressValue));

            SetScanTimeText(scanStopwatch.ElapsedMilliseconds);
        }

        private void RefreshStatusBarCanceling()
        {
            SetProgressBar(
                scanProgressValue,
                GetProgressBarTitle(
                    "取消中...",
                    scanProgressCurrentCount,
                    scanProgressTotalCount,
                    scanProgressValue));

            SetScanTimeText(scanStopwatch.ElapsedMilliseconds);
        }

        private void SetProgressBar(float value, string title)
        {
            if (scanProgressBar == null)
            {
                return;
            }

            scanProgressBar.value = value;
            scanProgressBar.title = title;
        }

        private void SetScanTimeText(long elapsedMilliseconds)
        {
            UIElementUtility.SetText(
                scanTimeLabel,
                "耗时：" + elapsedMilliseconds + " ms");
        }

        private void SetVisibleCountText(int count)
        {
            UIElementUtility.SetText(
                visibleCountLabel,
                "显示资源：" + count);
        }

        private void SetIssueCountText(int count)
        {
            UIElementUtility.SetText(
                issueCountLabel,
                "问题总数：" + count);
        }

        private int GetCurrentVisibleAssetCount()
        {
            if (currentReport == null || currentReport.ResultsByPath == null)
            {
                return 0;
            }

            return currentReport.ResultsByPath.Count;
        }

        private int GetCurrentIssueCount()
        {
            if (currentReport == null || currentReport.ResultsByPath == null)
            {
                return 0;
            }

            int issueCount = 0;

            foreach (AssetScanResult result in currentReport.ResultsByPath.Values)
            {
                // 用 SelfIssueCount，避免依赖汇总问题重复统计。
                issueCount += result.SelfIssueCount;
            }

            return issueCount;
        }

        private float CalculateOverallProgress(
            string stageText,
            int currentCount,
            int totalCount)
        {
            if (stageText == "准备扫描")
            {
                return 0f;
            }

            if (stageText == "构建依赖")
            {
                float stageProgress = GetStageProgress01(currentCount, totalCount);
                return stageProgress * BuildDependencyProgressEnd;
            }

            if (stageText == "检测规则")
            {
                float stageProgress = GetStageProgress01(currentCount, totalCount);
                return CheckRuleProgressStart +
                       stageProgress * (100f - CheckRuleProgressStart);
            }

            if (stageText == "完成")
            {
                return 100f;
            }

            return scanProgressValue;
        }

        private float GetStageProgress01(int currentCount, int totalCount)
        {
            if (totalCount <= 0)
            {
                return 0f;
            }

            float value = currentCount / (float)totalCount;

            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }

        private void RefreshStatusBarScanStarted()
        {
            scanProgressValue = 0f;
            scanProgressCurrentCount = 0;
            scanProgressTotalCount = 0;

            SetProgressBar(0f, "扫描中 - 准备扫描    0%");
            SetScanTimeText(0);
            SetVisibleCountText(0);
            SetIssueCountText(0);
        }

        /// <summary>
        /// 获取当前选中 Profile 的详情展示文本。
        /// </summary>
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
    }
}
