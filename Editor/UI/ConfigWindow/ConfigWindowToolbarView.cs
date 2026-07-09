using System;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 负责配置窗口工具栏控件的查询与命令事件绑定。
    /// 工具栏的业务命令仍由窗口协调器提供。
    /// </summary>
    internal sealed class ConfigWindowToolbarView
    {
        /// <summary>
        /// 绑定工具栏中的控件，并将用户操作转交给窗口回调。
        /// </summary>
        public ConfigWindowToolbarView(
            VisualElement windowRoot,
            Action saveRequested,
            Action revertRequested,
            Action refreshRequested,
            Action openFolderRequested)
        {
            SaveButton = windowRoot.Q<Button>("SaveAllButton");
            RevertButton = windowRoot.Q<Button>("RevertButton");
            RefreshButton = windowRoot.Q<Button>("RefreshButton");
            OpenFolderButton = windowRoot.Q<Button>("OpenConfigFolderButton");
            StatusLabel = windowRoot.Q<Label>("StatusLabel");
            DirtyCountLabel = windowRoot.Q<Label>("DirtyCountLabel");

            SaveButton.clicked += saveRequested;
            RevertButton.clicked += revertRequested;
            RefreshButton.clicked += refreshRequested;
            OpenFolderButton.clicked += openFolderRequested;
        }

        /// <summary>
        /// 获取保存全部按钮，供窗口根据验证结果更新状态。
        /// </summary>
        public Button SaveButton { get; }

        /// <summary>
        /// 获取放弃修改按钮，供窗口根据脏状态更新状态。
        /// </summary>
        public Button RevertButton { get; }

        /// <summary>
        /// 获取刷新按钮。
        /// </summary>
        public Button RefreshButton { get; }

        /// <summary>
        /// 获取打开配置目录按钮。
        /// </summary>
        public Button OpenFolderButton { get; }

        /// <summary>
        /// 获取工具栏状态文本。
        /// </summary>
        public Label StatusLabel { get; }

        /// <summary>
        /// 获取未保存修改数量文本。
        /// </summary>
        public Label DirtyCountLabel { get; }

        /// <summary>
        /// 根据窗口已计算的脏状态刷新未保存数量和放弃按钮。
        /// </summary>
        public void SetDirtyState(int dirtyCount, bool hasChanges)
        {
            DirtyCountLabel.text = "未保存修改：" + dirtyCount;
            UIElementUtility.SetEnabled(RevertButton, hasChanges);
        }

        /// <summary>
        /// 根据窗口已完成的校验结果刷新保存按钮状态和提示。
        /// </summary>
        public void SetSaveState(bool hasUnsavedChanges, bool isValid)
        {
            UIElementUtility.SetEnabled(SaveButton, hasUnsavedChanges);
            SaveButton.tooltip = isValid
                ? "保存全部未保存修改。"
                : "存在不能保存的配置错误；点击后会定位到第一个错误。";
        }

        /// <summary>
        /// 显示窗口协调器提供的当前操作状态。
        /// </summary>
        public void SetStatus(string status)
        {
            StatusLabel.text = status ?? string.Empty;
        }
    }
}
