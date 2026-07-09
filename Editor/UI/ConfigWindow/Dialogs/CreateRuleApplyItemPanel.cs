using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 新增规则应用项表单 Panel。
    /// Panel 管理临时选择状态，配置查询和工作副本写入由窗口处理。
    /// </summary>
    public sealed class CreateRuleApplyItemPanel : ModalPanel
    {
        private readonly VisualTreeAsset referenceVisualTreeAsset;
        private readonly Func<ScriptableObject, string> displayNameProvider;

        private readonly Action<
            string,
            Type,
            ScriptableObject,
            bool,
            string,
            IReadOnlyCollection<string>,
            Action<ScriptableObject>> singleSelectionRequester;

        private readonly Action<
            string,
            Type,
            IReadOnlyList<ScriptableObject>,
            Action<IReadOnlyList<ScriptableObject>>> multiSelectionRequester;

        private readonly Toggle enabledToggle;
        private readonly VisualElement ruleContainer;
        private readonly VisualElement assetTypeList;
        private readonly VisualElement profileList;
        private readonly Button addAssetTypeButton;
        private readonly Button addProfileButton;
        private readonly Button cancelButton;
        private readonly Button confirmButton;

        private readonly List<AssetTypeConfig> assetTypes =
            new List<AssetTypeConfig>();

        private readonly List<ScoreRuleProfileConfig> profiles =
            new List<ScoreRuleProfileConfig>();

        private ScoreRuleConfig ruleConfig;
        private ConfigWindowData configData;
        private RuleApplyConfig owner;
        private string arrayPath;
        private Action committed;
        private readonly HashSet<string> excludedRuleIds =
            new HashSet<string>(StringComparer.Ordinal);

        public event Action<ScoreRuleConfig> RuleSelectionRequested;

        public event Action AssetTypeSelectionRequested;

        public event Action ProfileSelectionRequested;

        public event Action<
            bool,
            ScoreRuleConfig,
            AssetTypeConfig[],
            ScoreRuleProfileConfig[]> Confirmed;

        public event Action CancelRequested;

        /// <summary>
        /// 创建规则应用项新增面板，并配置单选和多选配置资产的请求入口。
        /// </summary>
        public CreateRuleApplyItemPanel(
            VisualTreeAsset visualTreeAsset,
            VisualTreeAsset objectReferenceVisualTreeAsset,
            Func<ScriptableObject, string> referenceDisplayNameProvider,
            Action<
                string,
                Type,
                ScriptableObject,
                bool,
                string,
                IReadOnlyCollection<string>,
                Action<ScriptableObject>> requestSelection,
            Action<
                string,
                Type,
                IReadOnlyList<ScriptableObject>,
                Action<IReadOnlyList<ScriptableObject>>> requestMultiSelection)
        {
            if (visualTreeAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(visualTreeAsset));
            }

            if (objectReferenceVisualTreeAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(objectReferenceVisualTreeAsset));
            }

            if (referenceDisplayNameProvider == null)
            {
                throw new ArgumentNullException(
                    nameof(referenceDisplayNameProvider));
            }

            referenceVisualTreeAsset =
                objectReferenceVisualTreeAsset;
            displayNameProvider =
                referenceDisplayNameProvider;
            singleSelectionRequester = requestSelection;
            multiSelectionRequester = requestMultiSelection;

            Title = "添加规则应用项";
            AddToClassList("item-create-dialog-panel");

            TemplateContainer content =
                visualTreeAsset.Instantiate();

            Add(content);

            enabledToggle =
                content.Q<Toggle>(
                    "CreateRuleApplyItemEnabledToggle");

            ruleContainer =
                content.Q<VisualElement>(
                    "CreateRuleApplyItemRuleContainer");

            assetTypeList =
                content.Q<VisualElement>(
                    "CreateRuleApplyItemAssetTypeList");

            profileList =
                content.Q<VisualElement>(
                    "CreateRuleApplyItemProfileList");

            addAssetTypeButton =
                content.Q<Button>(
                    "CreateRuleApplyItemAddAssetTypeButton");

            addProfileButton =
                content.Q<Button>(
                    "CreateRuleApplyItemAddProfileButton");

            cancelButton =
                content.Q<Button>(
                    "CreateRuleApplyItemCancelButton");

            confirmButton =
                content.Q<Button>(
                    "CreateRuleApplyItemConfirmButton");

            addAssetTypeButton.clicked +=
                delegate
                {
                    AssetTypeSelectionRequested?.Invoke();
                    multiSelectionRequester(
                        "选择资源类型配置",
                        typeof(AssetTypeConfig),
                        assetTypes,
                        selected =>
                        {
                            assetTypes.Clear();

                            for (int i = 0; i < selected.Count; i++)
                            {
                                AssetTypeConfig assetType =
                                    selected[i] as AssetTypeConfig;

                                if (assetType != null)
                                {
                                    assetTypes.Add(assetType);
                                }
                            }

                            RefreshAssetTypes();
                        });
                };

            addProfileButton.clicked +=
                delegate
                {
                    ProfileSelectionRequested?.Invoke();
                    multiSelectionRequester(
                        "选择检测配置档",
                        typeof(ScoreRuleProfileConfig),
                        profiles,
                        selected =>
                        {
                            profiles.Clear();

                            for (int i = 0; i < selected.Count; i++)
                            {
                                ScoreRuleProfileConfig profile =
                                    selected[i] as ScoreRuleProfileConfig;

                                if (profile != null)
                                {
                                    profiles.Add(profile);
                                }
                            }

                            RefreshProfiles();
                        });
                };

            cancelButton.clicked +=
                delegate
                {
                    CancelRequested?.Invoke();
                    Close();
                };

            confirmButton.clicked += Confirm;
            Closed += ClearContext;
        }

        /// <summary>
        /// 打开新增规则应用项 Panel，并保存本次编辑所需的数据上下文。
        /// </summary>
        internal void Open(
            ConfigWindowDialogHost host,
            ConfigWindowData data,
            RuleApplyConfig target,
            string targetArrayPath,
            Action onCommitted)
        {
            configData = data;
            owner = target;
            arrayPath = targetArrayPath;
            committed = onCommitted;
            RebuildExcludedRuleIds(target);

            ResetForm();
            host.Show(this);
        }

        private void RebuildExcludedRuleIds(
            RuleApplyConfig target)
        {
            excludedRuleIds.Clear();

            if (target == null ||
                target.RuleApplyItems == null)
            {
                return;
            }

            for (int i = 0; i < target.RuleApplyItems.Length; i++)
            {
                RuleApplyItem item = target.RuleApplyItems[i];

                if (item != null &&
                    item.RuleConfig != null &&
                    !string.IsNullOrEmpty(item.RuleId))
                {
                    excludedRuleIds.Add(item.RuleId);
                }
            }
        }

        public void ResetForm()
        {
            enabledToggle.SetValueWithoutNotify(true);
            ruleConfig = null;
            assetTypes.Clear();
            profiles.Clear();

            RefreshRule();
            RefreshAssetTypes();
            RefreshProfiles();

            // 规则应用项所有字段均可留空。
            UIElementUtility.SetEnabled(confirmButton, true);
        }

        public void SetRuleConfig(
            ScoreRuleConfig value)
        {
            ruleConfig = value;
            RefreshRule();
        }

        private void RefreshRule()
        {
            ruleContainer.Clear();

            ObjectReferenceSelector selector =
                CreateReferenceSelector(
                    ruleConfig);

            selector.SelectButtonVisible = true;
            selector.SelectButtonText = "选择规则";
            selector.ClearButtonVisible = ruleConfig != null;
            selector.ClearButtonText = "清除";

            selector.SelectRequested +=
                delegate
                {
                    RuleSelectionRequested?.Invoke(ruleConfig);
                    singleSelectionRequester(
                        "选择规则配置",
                        typeof(ScoreRuleConfig),
                        ruleConfig,
                        false,
                        string.Empty,
                        excludedRuleIds,
                        selected => SetRuleConfig(
                            selected as ScoreRuleConfig));
                };

            selector.ClearRequested +=
                delegate
                {
                    ruleConfig = null;
                    RefreshRule();
                };

            ruleContainer.Add(selector);
        }

        private void RefreshAssetTypes()
        {
            assetTypeList.Clear();

            for (int i = 0; i < assetTypes.Count; i++)
            {
                int index = i;

                assetTypeList.Add(
                    CreateReferenceChip(
                        assetTypes[i],
                        "config-reference-chip-asset-type",
                        delegate
                        {
                            assetTypes.RemoveAt(index);
                            RefreshAssetTypes();
                        }));
            }
        }

        private void RefreshProfiles()
        {
            profileList.Clear();

            for (int i = 0; i < profiles.Count; i++)
            {
                int index = i;

                profileList.Add(
                    CreateReferenceChip(
                        profiles[i],
                        "config-reference-chip-profile",
                        delegate
                        {
                            profiles.RemoveAt(index);
                            RefreshProfiles();
                        }));
            }
        }

        /// <summary>
        /// 创建与规则应用配置页一致的可移除配置引用标签。
        /// </summary>
        private VisualElement CreateReferenceChip(
            ScriptableObject value,
            string typeClass,
            Action removeAction)
        {
            VisualElement chip = new VisualElement();
            chip.AddToClassList("config-reference-chip");
            chip.AddToClassList(typeClass);

            Label label = new Label(
                value == null ? "未选择" : displayNameProvider(value));
            label.AddToClassList("config-reference-chip-label");
            chip.Add(label);

            Button removeButton = new Button(removeAction);
            removeButton.text = "×";
            removeButton.AddToClassList("config-reference-chip-remove");
            chip.Add(removeButton);

            return chip;
        }

        private ObjectReferenceSelector CreateReferenceSelector(
            ScriptableObject value)
        {
            ObjectReferenceSelector selector =
                new ObjectReferenceSelector(
                    referenceVisualTreeAsset);

            selector.LabelText = string.Empty;
            selector.DisplayName =
                value == null
                    ? "未选择"
                    : displayNameProvider(value);
            selector.Metadata =
                value == null
                    ? "未选择配置资产"
                    : value.name;
            selector.PingButtonVisible = false;
            selector.EditButtonVisible = false;

            return selector;
        }

        private void Confirm()
        {
            configData.AddRuleApplyItem(
                owner,
                arrayPath,
                enabledToggle.value,
                ruleConfig,
                assetTypes.ToArray(),
                profiles.ToArray());

            Confirmed?.Invoke(
                enabledToggle.value,
                ruleConfig,
                assetTypes.ToArray(),
                profiles.ToArray());

            committed?.Invoke();
            Close();
        }

        private void ClearContext()
        {
            configData = null;
            owner = null;
            arrayPath = string.Empty;
            committed = null;
            excludedRuleIds.Clear();
        }
    }
}
