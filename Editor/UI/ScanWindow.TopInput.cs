using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    public partial class ScanWindow
    {
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
                    EditorUtility.DisplayDialog("Asset Score Tool", "扫描器未初始化。", "确定");
                    return;
                }

                if (scanner.IsRunning)
                {
                    CancelCurrentScan();
                    return;
                }

                if (selectedProfile == null)
                {
                    EditorUtility.DisplayDialog("Asset Score Tool", "请先选择 Profile。", "确定");
                    return;
                }

                if (pathTextField == null || string.IsNullOrEmpty(pathTextField.value))
                {
                    EditorUtility.DisplayDialog("Asset Score Tool", "请先选择或输入检测路径。", "确定");
                    return;
                }

                string scanPath = ScanToolUtility.NormalizePath(pathTextField.value);

                SetScanInputEnabled(false);

                scanStopwatch.Restart();
                RefreshStatusBarScanStarted();

                // 新扫描开始前清空旧结果。
                currentReport = null;
                scanResults.Clear();
                RefreshDetailPanelEmpty();
                RefreshResultListScanning();

                // StartScanAsync 会同步执行到第一次 await。
                // 这里先拿到 Task，再刷新按钮状态，就能读到 Scanner 当前阶段。
                Task<AssetScanData> scanTask = scanner.StartScanAsync(
                    scanPath,
                    selectedProfile,
                    OnScanProgress);

                FreshStartScanBtnState();

                AssetScanData scanData = await scanTask;

                currentReport = AssetScoreReportBuilder.Build(scanData);
                scanResults = currentReport.DisplayRootResults;
                RefreshResultList();

                scanStopwatch.Stop();

                RefreshStatusBarCompleted();
            }
            catch (OperationCanceledException)
            {
                currentReport = null;
                scanResults.Clear();
                RefreshResultList();
                RefreshDetailPanelEmpty();

                scanStopwatch.Stop();
                RefreshStatusBarCanceled();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                currentReport = null;
                scanResults.Clear();
                RefreshResultList();
                RefreshDetailPanelEmpty();

                scanStopwatch.Stop();
                RefreshStatusBarFailed();

                EditorUtility.DisplayDialog(
                    "Asset Score Tool",
                    "扫描过程中发生错误，请查看 Console。\n" + exception.Message,
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

            startScanButton.text = text;
            startScanButton.SetEnabled(enabled);
            startScanButton.style.backgroundColor = backgroundColor;
            startScanButton.style.color = ScanButtonTextColor;
        }

        /// <summary>
        /// 设置扫描参数输入区是否可编辑。
        /// 扫描中锁定 Profile、路径和选择按钮，但保留开始按钮可点击，用于取消扫描。
        /// </summary>
        private void SetScanInputEnabled(bool enabled)
        {
            if (profileDropdown != null)
            {
                profileDropdown.SetEnabled(enabled);
            }

            if (pathTextField != null)
            {
                pathTextField.SetEnabled(enabled);
            }

            if (selectFolderButton != null)
            {
                selectFolderButton.SetEnabled(enabled);
            }

            if (selectFileButton != null)
            {
                selectFileButton.SetEnabled(enabled);
            }
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
    }
}