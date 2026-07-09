using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 在独立模态窗口中承载规则配置字段，并复用规则配置页的字段构建流程。
    /// </summary>
    public sealed class EditRuleConfigPanel : ModalPanel
    {
        private readonly Label displayNameLabel;
        private readonly Label metadataLabel;
        private readonly Label sourceLabel;
        private readonly VisualElement fieldsContainer;
        private ScoreRuleConfig targetRule;
        private bool editable;
        private Action<
            ScoreRuleConfig,
            VisualElement,
            bool> buildFields;
        private Action<
            VisualElement,
            ScriptableObject,
            Action> trackChanges;

        /// <summary>
        /// 实例化规则编辑弹窗模板，并绑定弹窗关闭后的上下文清理。
        /// </summary>
        public EditRuleConfigPanel(
            VisualTreeAsset visualTreeAsset)
        {
            if (visualTreeAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(visualTreeAsset));
            }

            Title = "编辑检测规则配置";
            AddToClassList(
                "edit-rule-config-dialog-panel");

            TemplateContainer content =
                visualTreeAsset.Instantiate();

            Add(content);

            displayNameLabel =
                content.Q<Label>(
                    "EditRuleConfigDisplayName");

            metadataLabel =
                content.Q<Label>(
                    "EditRuleConfigMetadata");

            sourceLabel =
                content.Q<Label>(
                    "EditRuleConfigSourceLabel");

            fieldsContainer =
                content.Q<VisualElement>(
                    "EditRuleConfigFieldsContainer");
            Closed += ClearContext;
        }

        /// <summary>
        /// 打开规则编辑弹窗，并保存本次显示使用的工作副本与字段构建委托。
        /// </summary>
        internal void Open(
            ConfigWindowDialogHost host,
            ScoreRuleConfig ruleConfig,
            string displayName,
            string assetName,
            bool isEditable,
            Action<
                ScoreRuleConfig,
                VisualElement,
                bool> fieldBuilder,
            Action<
                VisualElement,
                ScriptableObject,
                Action> changeTracker)
        {
            if (ruleConfig == null)
            {
                throw new ArgumentNullException(
                    nameof(ruleConfig));
            }

            if (fieldBuilder == null)
            {
                throw new ArgumentNullException(
                    nameof(fieldBuilder));
            }

            targetRule = ruleConfig;
            editable = isEditable;
            buildFields = fieldBuilder;
            trackChanges = changeTracker;

            displayNameLabel.text =
                string.IsNullOrEmpty(displayName)
                    ? assetName
                    : displayName;

            metadataLabel.text =
                assetName +
                (editable
                    ? " · 本地配置 · 修改会计入配置窗口的未保存状态"
                    : " · 包内配置 · 只读预览");

            sourceLabel.text = editable ? "本地" : "包内";
            sourceLabel.RemoveFromClassList("local-badge");
            sourceLabel.RemoveFromClassList("package-badge");
            sourceLabel.AddToClassList(
                editable
                    ? "local-badge"
                    : "package-badge");

            Refresh();
            host.Show(this);
        }

        /// <summary>
        /// 判断弹窗当前是否正在编辑指定规则工作副本。
        /// </summary>
        internal bool IsEditing(
            ScoreRuleConfig ruleConfig)
        {
            return IsOpen && targetRule == ruleConfig;
        }

        /// <summary>
        /// 当规则的问题项或检测脚本结构变化时重建弹窗内容。
        /// </summary>
        internal void RefreshIfEditing(
            ScoreRuleConfig ruleConfig)
        {
            if (IsEditing(ruleConfig))
            {
                Refresh();
            }
        }

        /// <summary>
        /// 使用规则配置页的现有字段构建器重建弹窗内容。
        /// </summary>
        private void Refresh()
        {
            fieldsContainer.Clear();

            if (targetRule == null || buildFields == null)
            {
                return;
            }

            buildFields(
                targetRule,
                fieldsContainer,
                editable);

            if (trackChanges != null)
            {
                trackChanges(
                    fieldsContainer,
                    targetRule,
                    null);
            }
        }

        /// <summary>
        /// 关闭弹窗后释放对工作副本和窗口回调的引用。
        /// </summary>
        private void ClearContext()
        {
            fieldsContainer.Clear();
            targetRule = null;
            buildFields = null;
            trackChanges = null;
        }
    }
}
