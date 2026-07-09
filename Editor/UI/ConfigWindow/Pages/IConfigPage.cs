using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    internal enum EConfigPage
    {
        Profile,
        RuleApply,
        Rule,
        AssetType,
        ProjectSettings
    }

    internal sealed class ConfigPageCreateOptions
    {
        public string Title;
        public string IdLabel;
        public string IdHint;
        public string IdPlaceholder;
        public string DisplayNameHint;
        public string DefaultAssetName;
        public string IdPropertyName;
        public string DefaultLocalFolder;
        public Func<UserSettingConfig, DefaultAsset>
            DefaultFolderProvider;
    }

    /// <summary>
    /// 配置中心页面的公共入口。
    /// 页面自身负责页面数据、控件绑定和内容刷新。
    /// </summary>
    internal interface IConfigPage
    {
        EConfigPage PageType { get; }

        Type ConfigType { get; }

        string DisplayName { get; }

        VisualElement Root { get; }

        ScrollView ScrollView { get; }

        Button TabButton { get; }

        ConfigPageCreateOptions CreateOptions { get; }

        void Bind();

        void Fresh();

        void SetVisible(bool visible);

        void ExpandConfig(string assetPath);

        string GetConfigDisplayName(
            ScriptableObject config);

        void BuildFields(
            ScriptableObject config,
            VisualElement container,
            bool editable);
    }
}
