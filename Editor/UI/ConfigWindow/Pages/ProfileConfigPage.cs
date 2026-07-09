using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检测配置档页的字段 UI。
    /// 复用窗口提供的通用字段构建方法，以保持验证样式和序列化编辑行为一致。
    /// </summary>
    internal sealed class ProfileConfigPage
        : ConfigPage<ScoreRuleProfileConfig>
    {
        private static readonly ConfigPageCreateOptions createOptions =
            new ConfigPageCreateOptions
            {
                Title = "新建检测配置档",
                IdLabel = "配置档 ID *",
                IdHint =
                    "必填。检测配置档的唯一标识，例如 pc.default。" +
                    ConfigIdNamingUtility.RuleHint,
                DisplayNameHint =
                    "必填。编辑器中显示的配置档名称。",
                DefaultAssetName = "ScoreRuleProfile_",
                IdPropertyName = "profileId",
                DefaultLocalFolder =
                    "Assets/Tools/asset_score_tool/Configs/Profiles",
                DefaultFolderProvider =
                    settings => settings == null
                        ? null
                        : settings.ProfileConfigDefaultFolder
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

        public ProfileConfigPage(
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
                "ProfilePage",
                "ProfileScrollView",
                "ProfileTabButton",
                "ProfileTabCountLabel",
                "CreateProfileButton",
                "ProfileSearchField",
                "ProfileListContainer",
                "ProfileEmptyLabel",
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
            EConfigPage.Profile;

        public override string DisplayName => "检测配置档";

        public override ConfigPageCreateOptions CreateOptions =>
            createOptions;

        protected override string GetDisplayName(
            ScoreRuleProfileConfig config)
        {
            return string.IsNullOrEmpty(config.DisplayName)
                ? config.name
                : config.DisplayName;
        }

        protected override string GetSummary(
            ScoreRuleProfileConfig config)
        {
            return "配置档 ID：" + config.ProfileId;
        }

        protected override void BuildTypedFields(
            ScoreRuleProfileConfig config,
            VisualElement container,
            bool editable)
        {
            SerializedObject serializedObject =
                new SerializedObject(config);

            VisualElement section =
                createSection("检测配置档");

            VisualElement body =
                section.Q<VisualElement>("SectionBody");

            addPropertyField(
                body,
                serializedObject,
                "profileId",
                "配置档 ID *",
                editable,
                "检测配置档的唯一标识，例如 pc.default。" +
                ConfigIdNamingUtility.RuleHint,
                true);

            addPropertyField(
                body,
                serializedObject,
                "displayName",
                "显示名称 *",
                editable,
                "编辑器中显示的配置档名称。",
                true);

            container.Add(section);
        }
    }
}
