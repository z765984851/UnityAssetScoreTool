using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 资源类型配置页的字段 UI。
    /// 复用窗口提供的通用字段构建方法，以保持验证样式和序列化编辑行为一致。
    /// </summary>
    internal sealed class AssetTypeConfigPage
        : ConfigPage<AssetTypeConfig>
    {
        private static readonly ConfigPageCreateOptions createOptions =
            new ConfigPageCreateOptions
            {
                Title = "新建资源类型配置",
                IdLabel = "资源类型 ID *",
                IdHint =
                    "必填。资源类型的唯一标识，例如 texture、prefab、material。" +
                    ConfigIdNamingUtility.RuleHint,
                DisplayNameHint =
                    "必填。编辑器中显示的资源类型名称。",
                DefaultAssetName = "AssetType_",
                IdPropertyName = "assetTypeId",
                DefaultLocalFolder =
                    "Assets/Tools/asset_score_tool/Configs/AssetTypes",
                DefaultFolderProvider =
                    settings => settings == null
                        ? null
                        : settings.AssetTypeConfigDefaultFolder
            };

        private readonly Func<string, VisualElement> createSection;

        private readonly Action<
            VisualElement,
            SerializedObject,
            string,
            string,
            bool,
            string,
            bool> addPropertyField;

        public AssetTypeConfigPage(
            VisualElement windowRoot,
            ConfigWindowData configData,
            Func<ScriptableObject, int>
                getValidationErrorCount,
            Action<ScriptableObject> copyConfigToLocal,
            Action<ScriptableObject> deleteConfig,
            Action<
                VisualElement,
                ScriptableObject,
                Action> trackWorkingCopyChanges,
            Action<IConfigPage> createConfigRequested,
            Func<string, VisualElement> createSection,
            Action<
                VisualElement,
                SerializedObject,
                string,
                string,
                bool,
                string,
                bool> addPropertyField,
            VisualElement pageRoot = null)
            : base(
                windowRoot,
                "AssetTypePage",
                "AssetTypeScrollView",
                "AssetTypeTabButton",
                "AssetTypeTabCountLabel",
                "CreateAssetTypeButton",
                "AssetTypeSearchField",
                "AssetTypeListContainer",
                "AssetTypeEmptyLabel",
                configData,
                getValidationErrorCount,
                copyConfigToLocal,
                deleteConfig,
                trackWorkingCopyChanges,
                createConfigRequested,
                pageRoot)
        {
            this.createSection = createSection;
            this.addPropertyField = addPropertyField;
        }

        public override EConfigPage PageType =>
            EConfigPage.AssetType;

        public override string DisplayName => "资源类型配置";

        public override ConfigPageCreateOptions CreateOptions =>
            createOptions;

        protected override string GetDisplayName(
            AssetTypeConfig config)
        {
            return string.IsNullOrEmpty(config.DisplayName)
                ? config.name
                : config.DisplayName;
        }

        protected override string GetSummary(
            AssetTypeConfig config)
        {
            return "资源类型 ID：" + config.AssetTypeId;
        }

        protected override void BuildTypedFields(
            AssetTypeConfig config,
            VisualElement container,
            bool editable)
        {
            SerializedObject serializedObject =
                new SerializedObject(config);

            VisualElement section =
                createSection("资源类型配置");

            VisualElement body =
                section.Q<VisualElement>("SectionBody");

            addPropertyField(
                body,
                serializedObject,
                "assetTypeId",
                "资源类型 ID *",
                editable,
                "资源类型的唯一标识，例如 texture、prefab、material。" +
                ConfigIdNamingUtility.RuleHint,
                true);

            addPropertyField(
                body,
                serializedObject,
                "displayName",
                "显示名称 *",
                editable,
                "编辑器中显示的资源类型名称。",
                true);

            container.Add(section);
        }
    }
}
