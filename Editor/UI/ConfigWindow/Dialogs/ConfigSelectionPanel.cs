using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    public sealed class ConfigSelectionOption
    {
        public ConfigSelectionOption(
            ScriptableObject value,
            string displayName,
            string assetName)
        {
            Value = value;
            DisplayName = displayName;
            AssetName = assetName;
        }

        public ScriptableObject Value { get; }

        public string DisplayName { get; }

        public string AssetName { get; }
    }

    /// <summary>
    /// 配置选择 Panel。
    /// 外部负责提供可选数据，本类只处理显示、搜索和选择交互。
    /// </summary>
    public sealed class ConfigSelectionPanel : ModalPanel
    {
        private readonly TextField searchField;
        private readonly VisualElement listContainer;
        private readonly Label emptyLabel;
        private readonly Button cancelButton;
        private readonly Button confirmButton;

        private readonly List<ConfigSelectionOption> options =
            new List<ConfigSelectionOption>();

        private readonly HashSet<ScriptableObject> selectedValues =
            new HashSet<ScriptableObject>();

        private ScriptableObject selectedValue;
        private bool allowNone;
        private bool isMultiSelect;
        private string noneText;
        private Action<ScriptableObject> confirmAction;
        private Action<IReadOnlyList<ScriptableObject>> multiConfirmAction;

        public event Action<ScriptableObject> Confirmed;

        public event Action CancelRequested;

        public ConfigSelectionPanel(
            VisualTreeAsset visualTreeAsset)
        {
            if (visualTreeAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(visualTreeAsset));
            }

            AddToClassList("selection-dialog-panel");

            // ModalPanel 负责外层标题栏和关闭按钮，UXML 只提供弹窗的内容区。
            TemplateContainer content =
                visualTreeAsset.Instantiate();

            Add(content);

            searchField =
                content.Q<TextField>(
                    "SelectionSearchField");

            listContainer =
                content.Q<VisualElement>(
                    "SelectionListContainer");

            emptyLabel =
                content.Q<Label>(
                    "SelectionEmptyLabel");

            cancelButton =
                content.Q<Button>(
                    "SelectionCancelButton");

            confirmButton =
                content.Q<Button>(
                    "SelectionConfirmButton");

            // 搜索只重建当前 Panel 的显示列表，不重新加载配置资产。
            searchField.RegisterValueChangedCallback(
                delegate { RefreshList(); });

            cancelButton.clicked +=
                delegate
                {
                    CancelRequested?.Invoke();
                    Close();
                };

            confirmButton.clicked +=
                delegate
                {
                    if (isMultiSelect)
                    {
                        List<ScriptableObject> values =
                            BuildOrderedSelectedValues();

                        Action<IReadOnlyList<ScriptableObject>> multiAction =
                            multiConfirmAction;

                        Close();
                        multiAction?.Invoke(values);
                        return;
                    }

                    Confirmed?.Invoke(selectedValue);
                    Action<ScriptableObject> singleAction = confirmAction;
                    Close();
                    singleAction?.Invoke(selectedValue);
                };

            Closed +=
                delegate
                {
                    confirmAction = null;
                    multiConfirmAction = null;
                    selectedValues.Clear();
                    isMultiSelect = false;
                };
        }

        /// <summary>
        /// 打开配置选择 Panel，并保存本次确认操作。
        /// </summary>
        public void Open(
            VisualElement host,
            string title,
            IReadOnlyList<ConfigSelectionOption> selectionOptions,
            ScriptableObject currentValue,
            bool allowsNone,
            string emptySelectionText,
            Action<ScriptableObject> onConfirmed)
        {
            isMultiSelect = false;
            multiConfirmAction = null;
            selectedValues.Clear();
            confirmAction = onConfirmed;

            SetData(
                title,
                selectionOptions,
                currentValue,
                allowsNone,
                emptySelectionText);

            Show(host);
        }

        /// <summary>
        /// 通过配置窗口弹窗宿主打开选择 Panel，并保存本次确认操作。
        /// </summary>
        internal void Open(
            ConfigWindowDialogHost host,
            string title,
            IReadOnlyList<ConfigSelectionOption> selectionOptions,
            ScriptableObject currentValue,
            bool allowsNone,
            string emptySelectionText,
            Action<ScriptableObject> onConfirmed)
        {
            isMultiSelect = false;
            multiConfirmAction = null;
            selectedValues.Clear();
            confirmAction = onConfirmed;

            SetData(
                title,
                selectionOptions,
                currentValue,
                allowsNone,
                emptySelectionText);

            host.Show(this);
        }

        /// <summary>
        /// 通过配置窗口弹窗宿主打开多选 Panel，并保存本次确认操作。
        /// </summary>
        internal void OpenMultiple(
            ConfigWindowDialogHost host,
            string title,
            IReadOnlyList<ConfigSelectionOption> selectionOptions,
            IReadOnlyList<ScriptableObject> currentValues,
            Action<IReadOnlyList<ScriptableObject>> onConfirmed)
        {
            // 多选状态只属于当前弹窗，不能影响后续单选或配置工作副本。
            Title = title;
            isMultiSelect = true;
            confirmAction = null;
            multiConfirmAction = onConfirmed;
            selectedValue = null;
            allowNone = false;
            noneText = string.Empty;

            options.Clear();

            if (selectionOptions != null)
            {
                for (int i = 0;
                     i < selectionOptions.Count;
                     i++)
                {
                    options.Add(selectionOptions[i]);
                }
            }

            selectedValues.Clear();

            if (currentValues != null)
            {
                for (int i = 0;
                     i < currentValues.Count;
                     i++)
                {
                    ScriptableObject currentValue =
                        currentValues[i];

                    if (currentValue != null)
                    {
                        selectedValues.Add(currentValue);
                    }
                }
            }

            searchField.SetValueWithoutNotify(
                string.Empty);

            RefreshList();
            host.Show(this);
        }

        /// <summary>
        /// 用新的候选数据初始化单选状态并刷新列表。
        /// </summary>
        public void SetData(
            string title,
            IReadOnlyList<ConfigSelectionOption> selectionOptions,
            ScriptableObject currentValue,
            bool allowsNone,
            string emptySelectionText)
        {
            isMultiSelect = false;
            multiConfirmAction = null;
            selectedValues.Clear();

            // 每次打开时由外部重新提供数据，Panel 不缓存配置查询结果。
            Title = title;
            selectedValue = currentValue;
            allowNone = allowsNone;
            noneText = emptySelectionText;

            options.Clear();

            if (selectionOptions != null)
            {
                for (int i = 0;
                     i < selectionOptions.Count;
                     i++)
                {
                    options.Add(selectionOptions[i]);
                }
            }

            searchField.SetValueWithoutNotify(
                string.Empty);

            RefreshList();
        }

        /// <summary>
        /// 根据搜索词和当前选择模式重建可见行及确认状态。
        /// </summary>
        private void RefreshList()
        {
            listContainer.Clear();

            string keyword =
                string.IsNullOrEmpty(searchField.value)
                    ? string.Empty
                    : searchField.value.Trim();

            int visibleCount = 0;

            // 空值也是一个明确的选择项，但只在调用方允许时显示。
            if (!isMultiSelect &&
                allowNone &&
                IsKeywordMatched(
                    noneText,
                    "未选择",
                    keyword))
            {
                listContainer.Add(
                    BuildRow(
                        null,
                        noneText,
                        "未选择"));

                visibleCount++;
            }

            for (int i = 0;
                 i < options.Count;
                 i++)
            {
                ConfigSelectionOption option =
                    options[i];

                if (!IsKeywordMatched(
                        option.DisplayName,
                        option.AssetName,
                        keyword))
                {
                    continue;
                }

                listContainer.Add(
                    BuildRow(
                        option.Value,
                        option.DisplayName,
                        option.AssetName));

                visibleCount++;
            }

            UIElementUtility.SetVisible(emptyLabel, visibleCount == 0);

            UIElementUtility.SetEnabled(
                confirmButton,
                isMultiSelect || allowNone || selectedValue != null);
        }

        /// <summary>
        /// 构建单个候选行并绑定单选或多选切换交互。
        /// </summary>
        private VisualElement BuildRow(
            ScriptableObject value,
            string displayName,
            string assetName)
        {
            Button row = new Button();
            row.AddToClassList("selection-row");

            if (IsSelected(value))
            {
                row.AddToClassList(
                    "selection-row-selected");
            }

            VisualElement textContainer =
                new VisualElement();

            textContainer.AddToClassList(
                "selection-row-text");

            Label displayNameLabel =
                new Label(displayName);

            displayNameLabel.AddToClassList(
                "selection-row-display-name");

            textContainer.Add(displayNameLabel);

            Label assetNameLabel =
                new Label(assetName);

            assetNameLabel.AddToClassList(
                "selection-row-asset-name");

            textContainer.Add(assetNameLabel);
            row.Add(textContainer);

            Label selectedLabel =
                new Label(
                    IsSelected(value)
                        ? "已选择"
                        : string.Empty);

            selectedLabel.AddToClassList(
                "selection-row-state");

            row.Add(selectedLabel);

            row.clicked +=
                delegate
                {
                    // 只更新 Panel 内的临时选择；确认后才通过事件交给调用方处理。
                    if (isMultiSelect)
                    {
                        if (!selectedValues.Add(value))
                        {
                            selectedValues.Remove(value);
                        }
                    }
                    else
                    {
                        selectedValue = value;
                    }

                    RefreshList();
                };

            return row;
        }

        /// <summary>
        /// 按候选项原始顺序生成当前多选结果，并排除无效或重复项。
        /// </summary>
        private List<ScriptableObject> BuildOrderedSelectedValues()
        {
            List<ScriptableObject> orderedValues =
                new List<ScriptableObject>();

            HashSet<ScriptableObject> addedValues =
                new HashSet<ScriptableObject>();

            for (int i = 0; i < options.Count; i++)
            {
                ScriptableObject value = options[i].Value;

                if (value != null &&
                    selectedValues.Contains(value) &&
                    addedValues.Add(value))
                {
                    orderedValues.Add(value);
                }
            }

            return orderedValues;
        }

        /// <summary>
        /// 判断候选值在当前选择模式下是否处于选中状态。
        /// </summary>
        private bool IsSelected(ScriptableObject value)
        {
            return isMultiSelect
                ? value != null && selectedValues.Contains(value)
                : value == selectedValue;
        }

        private bool IsKeywordMatched(
            string displayName,
            string assetName,
            string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return true;
            }

            string text =
                displayName + "\n" + assetName;

            return text.IndexOf(
                keyword,
                StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
