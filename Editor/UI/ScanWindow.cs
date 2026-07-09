using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    public partial class ScanWindow : EditorWindow
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
        /// 当前扫描结果。
        /// 后续左侧列表、右侧详情、底部状态栏都会基于这份数据刷新。
        /// </summary>
        private List<AssetScanResult> scanResults = new List<AssetScanResult>();

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

        /// <summary>
        /// 当前详情面板选中的资源路径。
        /// </summary>
        private string selectedDetailAssetPath;

        /// <summary>
        /// 当前左侧结果列表中被选中的行。
        /// </summary>
        private VisualElement selectedResultRow;

        #region UI

        private DropdownField profileDropdown;
        private TextField pathTextField;
        private ScrollView resultScrollView;

        private Button selectFolderButton;
        private Button selectFileButton;
        private Button startScanButton;

        private DropdownField assetTypeFilterDropdown;
        private DropdownField severityFilterDropdown;
        private DropdownField sortDropdown;

        private Label scanTimeLabel;
        private Label visibleCountLabel;
        private Label issueCountLabel;
        private ProgressBar scanProgressBar;

        private Label detailAssetNameLabel;
        private Label detailAssetPathLabel;
        private Label detailAssetTypeLabel;
        private Label detailProfileLabel;
        private Label detailScoreLabel;
        private Label detailGradeLabel;
        private Label detailIssueCountLabel;

        private VisualElement issueListContainer;
        private VisualElement dependencyListContainer;

        private Button selectCurrentAssetButton;
        private Button openAssetFolderButton;

        #endregion

        [MenuItem("Window/UI Toolkit/ScanWindow")]
        public static void ShowWindow()
        {
            ScanWindow wnd = GetWindow<ScanWindow>();
            wnd.titleContent = new GUIContent("ScanWindow");
        }

        public void CreateGUI()
        {
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
            BindResultListUI();
            BindStatusBarUI();
            BindDetailPanelUI();
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
            RefreshResultList();
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
    }
}