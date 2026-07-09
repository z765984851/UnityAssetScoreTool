using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    public partial class ScanWindow
    {
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
            if (scanTimeLabel != null)
            {
                scanTimeLabel.text = "耗时：" + elapsedMilliseconds + " ms";
            }
        }

        private void SetVisibleCountText(int count)
        {
            if (visibleCountLabel != null)
            {
                visibleCountLabel.text = "显示资源：" + count;
            }
        }

        private void SetIssueCountText(int count)
        {
            if (issueCountLabel != null)
            {
                issueCountLabel.text = "问题总数：" + count;
            }
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
    }
}