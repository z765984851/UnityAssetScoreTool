using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 负责配置诊断消息、字段样式和定位请求的 UI 呈现。
    /// </summary>
    internal sealed class ConfigWindowValidationPresenter
    {
        private const string DefaultTemplatePath =
            "Assets/Tools/asset_score_tool/Editor/UI/Common/ValidationMessageView.uxml";

        private readonly VisualElement windowRoot;
        private readonly Func<ScriptableObject, string> getDisplayName;
        private readonly Func<ScriptableObject, ScriptableObject>
            getSourceConfig;

        private readonly List<ConfigValidationIssue> issues =
            new List<ConfigValidationIssue>();

        private readonly Dictionary<EConfigPage, ValidationMessageView>
            messageViews =
                new Dictionary<EConfigPage, ValidationMessageView>();

        private readonly Dictionary<EConfigPage, List<ConfigValidationIssue>>
            pageIssues =
                new Dictionary<EConfigPage, List<ConfigValidationIssue>>();

        public event Action<ConfigValidationIssue> LocateRequested;

        /// <summary>
        /// 绑定配置窗口根节点，并创建各配置页面的诊断消息视图。
        /// </summary>
        public ConfigWindowValidationPresenter(
            VisualElement windowRoot,
            VisualTreeAsset messageTemplate,
            Func<ScriptableObject, string> getDisplayName,
            Func<ScriptableObject, ScriptableObject> getSourceConfig)
        {
            this.windowRoot = windowRoot;
            this.getDisplayName = getDisplayName;
            this.getSourceConfig = getSourceConfig;

            VisualTreeAsset template = messageTemplate;

            if (template == null)
            {
                template =
                    AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                        DefaultTemplatePath);
            }

            if (template == null)
            {
                return;
            }

            BindMessageView(
                EConfigPage.Profile,
                "ProfileValidationHost",
                "检测配置档诊断",
                template);
            BindMessageView(
                EConfigPage.RuleApply,
                "RuleApplyValidationHost",
                "规则应用配置诊断",
                template);
            BindMessageView(
                EConfigPage.Rule,
                "RuleValidationHost",
                "检测规则诊断",
                template);
            BindMessageView(
                EConfigPage.AssetType,
                "AssetTypeValidationHost",
                "资源类型诊断",
                template);
        }

        /// <summary>
        /// 替换当前诊断集合，并重建页面局部索引映射。
        /// </summary>
        public void SetIssues(
            IReadOnlyList<ConfigValidationIssue> validationIssues)
        {
            issues.Clear();

            foreach (List<ConfigValidationIssue> values in
                     pageIssues.Values)
            {
                values.Clear();
            }

            if (validationIssues == null)
            {
                return;
            }

            for (int i = 0; i < validationIssues.Count; i++)
            {
                ConfigValidationIssue issue = validationIssues[i];

                if (issue == null)
                {
                    continue;
                }

                issues.Add(issue);

                List<ConfigValidationIssue> values;

                if (pageIssues.TryGetValue(issue.Page, out values))
                {
                    values.Add(issue);
                }
            }
        }

        /// <summary>
        /// 使用当前诊断集合刷新页面消息与已创建字段的视觉样式。
        /// </summary>
        public void Refresh()
        {
            RefreshMessageViews();
            RefreshFieldStyles();
        }

        /// <summary>
        /// 获取指定工作副本上的阻断错误数量。
        /// </summary>
        public int GetErrorCount(ScriptableObject workingConfig)
        {
            int count = 0;

            for (int i = 0; i < issues.Count; i++)
            {
                ConfigValidationIssue issue = issues[i];

                if (issue.WorkingConfig == workingConfig &&
                    issue.Severity == EConfigValidationSeverity.Error)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 判断复合序列化字段内部是否存在阻断错误。
        /// </summary>
        public bool HasErrorUnderPath(
            ScriptableObject workingConfig,
            string propertyPathPrefix)
        {
            string prefix = propertyPathPrefix + ".";

            for (int i = 0; i < issues.Count; i++)
            {
                ConfigValidationIssue issue = issues[i];

                if (issue.WorkingConfig == workingConfig &&
                    issue.Severity == EConfigValidationSeverity.Error &&
                    issue.PropertyPath.StartsWith(
                        prefix,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 为字段设置稳定定位名称，并应用当前错误或警告样式。
        /// </summary>
        public void ApplyFieldVisual(
            VisualElement element,
            ScriptableObject workingConfig,
            string propertyPath)
        {
            element.name = BuildElementName(
                workingConfig,
                propertyPath);

            ConfigValidationIssue issue = FindFieldIssue(
                workingConfig,
                propertyPath);

            if (issue != null)
            {
                ValidationFieldStyleUtility.Apply(
                    element,
                    issue.Severity ==
                        EConfigValidationSeverity.Warning);
            }
        }

        /// <summary>
        /// 构建工作副本字段在窗口视觉树中的稳定定位名称。
        /// </summary>
        public string BuildElementName(
            ScriptableObject workingConfig,
            string propertyPath)
        {
            ScriptableObject source =
                getSourceConfig(workingConfig);

            return "ValidationField_" +
                   source.GetInstanceID() + "_" +
                   propertyPath.GetHashCode();
        }

        /// <summary>
        /// 在页面刷新完成后滚动并聚焦指定诊断字段。
        /// </summary>
        public void FocusField(
            ConfigValidationIssue issue,
            ScrollView scrollView)
        {
            if (issue == null)
            {
                return;
            }

            windowRoot.schedule.Execute(() =>
            {
                VisualElement field =
                    windowRoot.Q<VisualElement>(
                        BuildElementName(
                            issue.WorkingConfig,
                            issue.PropertyPath));

                if (field == null)
                {
                    return;
                }

                if (scrollView != null)
                {
                    scrollView.ScrollTo(field);
                }

                field.Focus();
            });
        }

        /// <summary>
        /// 在指定页面挂载诊断视图，并绑定页面局部定位索引。
        /// </summary>
        private void BindMessageView(
            EConfigPage page,
            string hostName,
            string title,
            VisualTreeAsset template)
        {
            VisualElement host =
                windowRoot.Q<VisualElement>(hostName);

            if (host == null)
            {
                return;
            }

            ValidationMessageView view =
                new ValidationMessageView(template);

            view.Title = title;
            view.LocateRequested +=
                delegate(int index)
                {
                    RequestLocate(page, index);
                };

            host.Add(view);
            messageViews[page] = view;
            pageIssues[page] = new List<ConfigValidationIssue>();
        }

        /// <summary>
        /// 将页面诊断列表中的局部索引转换为统一诊断对象。
        /// </summary>
        private void RequestLocate(EConfigPage page, int index)
        {
            List<ConfigValidationIssue> values;

            if (!pageIssues.TryGetValue(page, out values) ||
                index < 0 ||
                index >= values.Count)
            {
                return;
            }

            LocateRequested?.Invoke(values[index]);
        }

        /// <summary>
        /// 将各页面诊断转换为 ValidationMessageView 可消费的数据。
        /// </summary>
        private void RefreshMessageViews()
        {
            foreach (KeyValuePair<EConfigPage, ValidationMessageView> pair
                     in messageViews)
            {
                List<ConfigValidationIssue> values =
                    pageIssues[pair.Key];

                List<ValidationMessageData> messages =
                    new List<ValidationMessageData>();

                for (int i = 0; i < values.Count; i++)
                {
                    ConfigValidationIssue issue = values[i];

                    messages.Add(
                        new ValidationMessageData(
                            getDisplayName(issue.WorkingConfig),
                            GetFieldName(issue.PropertyPath),
                            issue.Message,
                            issue.Severity ==
                                EConfigValidationSeverity.Warning
                                ? EValidationMessageSeverity.Warning
                                : EValidationMessageSeverity.Error));
                }

                pair.Value.SetMessages(messages);
            }
        }

        /// <summary>
        /// 重置窗口字段样式，并按当前诊断重新应用错误和警告状态。
        /// </summary>
        private void RefreshFieldStyles()
        {
            ValidationFieldStyleUtility.Clear(windowRoot);

            for (int i = 0; i < issues.Count; i++)
            {
                ConfigValidationIssue issue = issues[i];

                if (issue.WorkingConfig == null ||
                    string.IsNullOrEmpty(issue.PropertyPath))
                {
                    continue;
                }

                VisualElement field =
                    windowRoot.Q<VisualElement>(
                        BuildElementName(
                            issue.WorkingConfig,
                            issue.PropertyPath));

                if (field != null)
                {
                    ValidationFieldStyleUtility.Apply(
                        field,
                        issue.Severity ==
                            EConfigValidationSeverity.Warning);
                }
            }
        }

        /// <summary>
        /// 查找指定字段优先级最高的诊断，错误优先于警告。
        /// </summary>
        private ConfigValidationIssue FindFieldIssue(
            ScriptableObject workingConfig,
            string propertyPath)
        {
            ConfigValidationIssue warning = null;

            for (int i = 0; i < issues.Count; i++)
            {
                ConfigValidationIssue issue = issues[i];

                if (issue.WorkingConfig != workingConfig ||
                    issue.PropertyPath != propertyPath)
                {
                    continue;
                }

                if (issue.Severity == EConfigValidationSeverity.Error)
                {
                    return issue;
                }

                warning = issue;
            }

            return warning;
        }

        /// <summary>
        /// 将序列化路径转换为诊断列表中可读的字段名称。
        /// </summary>
        private string GetFieldName(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
            {
                return string.Empty;
            }

            int separator = propertyPath.LastIndexOf('.');
            string fieldName = separator < 0
                ? propertyPath
                : propertyPath.Substring(separator + 1);

            return fieldName.Replace(
                "Array.data[",
                "[");
        }
    }
}
