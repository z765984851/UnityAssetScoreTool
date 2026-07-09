using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 负责构建配置页面中可复用的 UI Toolkit 元素。
    /// 配置数据变更和验证规则仍由调用方提供的既有流程处理。
    /// </summary>
    internal sealed class ConfigPageUIBuilder
    {
        private readonly Action<VisualElement, ScriptableObject, string>
            applyValidationVisual;

        private readonly Func<ScriptableObject, string>
            getObjectDisplayName;

        private readonly Func<ScriptableObject, ScriptableObject>
            getSourceConfig;

        /// <summary>
        /// 使用窗口提供的验证与配置显示信息构建公共页面元素。
        /// </summary>
        public ConfigPageUIBuilder(
            Action<VisualElement, ScriptableObject, string>
                applyValidationVisual,
            Func<ScriptableObject, string> getObjectDisplayName,
            Func<ScriptableObject, ScriptableObject> getSourceConfig)
        {
            this.applyValidationVisual = applyValidationVisual;
            this.getObjectDisplayName = getObjectDisplayName;
            this.getSourceConfig = getSourceConfig;
        }

        /// <summary>
        /// 创建带标题、内容区和可选操作按钮的配置分区。
        /// </summary>
        public VisualElement CreateSection(
            string title,
            Button actionButton = null)
        {
            VisualElement section = new VisualElement();
            section.AddToClassList("config-section");

            VisualElement header = new VisualElement();
            header.AddToClassList("config-section-header");

            Label titleLabel = new Label(title);
            titleLabel.AddToClassList("config-section-title");
            header.Add(titleLabel);

            if (actionButton != null)
            {
                header.Add(actionButton);
            }

            section.Add(header);

            VisualElement body = new VisualElement();
            body.name = "SectionBody";
            body.AddToClassList("config-section-body");
            section.Add(body);

            return section;
        }

        /// <summary>
        /// 创建与配置页面 HTML 内容区语义一致的分区。
        /// </summary>
        public VisualElement CreateContentSection(
            string title,
            Button actionButton = null)
        {
            VisualElement section = CreateSection(title, actionButton);
            section.AddToClassList("html-content-section");
            return section;
        }

        /// <summary>
        /// 获取由 CreateSection 创建的内容容器。
        /// </summary>
        public VisualElement GetSectionBody(VisualElement section)
        {
            return section.Q<VisualElement>("SectionBody");
        }

        /// <summary>
        /// 创建字段说明文字，并在需要时标记为必填提示。
        /// </summary>
        public Label CreateFieldHint(
            string hint,
            bool required = false)
        {
            string text = required ? "必填。" + hint : hint;
            Label label = new Label(text);
            label.AddToClassList("field-hint");

            if (required)
            {
                label.AddToClassList("required-field-hint");
            }

            return label;
        }

        /// <summary>
        /// 将根序列化属性作为标准 PropertyField 添加到指定容器。
        /// </summary>
        public void AddPropertyField(
            VisualElement container,
            SerializedObject serializedObject,
            string propertyName,
            string displayName,
            bool editable,
            string hint = null,
            bool required = false)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                return;
            }

            VisualElement field;

            if (property.propertyType == SerializedPropertyType.String &&
                ConfigIdNamingUtility.IsIdPropertyName(property.name))
            {
                TextField idField = new TextField(displayName);
                ConfigIdNamingUtility.RestrictInput(idField);
                idField.BindProperty(property.Copy());
                field = idField;
            }
            else
            {
                PropertyField propertyField =
                    new PropertyField(property.Copy(), displayName);

                propertyField.Bind(serializedObject);
                field = propertyField;
            }

            field.SetEnabled(editable);
            field.AddToClassList("config-property-field");

            applyValidationVisual(
                field,
                serializedObject.targetObject as ScriptableObject,
                property.propertyPath);

            AddFieldBlock(container, field, hint, required);
        }

        /// <summary>
        /// 将根序列化字符串属性作为 TextField 添加到指定容器。
        /// </summary>
        public void AddTextField(
            VisualElement container,
            SerializedObject serializedObject,
            string propertyName,
            string displayName,
            bool editable,
            bool multiline,
            string hint = null,
            bool required = false)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                return;
            }

            TextField field = new TextField(displayName);
            field.multiline = multiline;
            field.BindProperty(property.Copy());
            field.SetEnabled(editable);
            field.AddToClassList(
                multiline ? "multiline-text-field" : "config-text-field");

            applyValidationVisual(
                field,
                serializedObject.targetObject as ScriptableObject,
                property.propertyPath);

            AddFieldBlock(container, field, hint, required);
        }

        /// <summary>
        /// 将相对序列化字符串属性作为 TextField 添加到指定容器。
        /// </summary>
        public void AddRelativeTextField(
            VisualElement container,
            SerializedProperty parent,
            string relativeName,
            string displayName,
            bool editable,
            bool multiline,
            string hint = null,
            bool required = false)
        {
            SerializedProperty property = parent.FindPropertyRelative(relativeName);

            if (property == null)
            {
                return;
            }

            TextField field = new TextField(displayName);
            field.multiline = multiline;
            field.BindProperty(property.Copy());
            field.SetEnabled(editable);
            field.AddToClassList(
                multiline ? "multiline-text-field" : "config-text-field");

            applyValidationVisual(
                field,
                property.serializedObject.targetObject as ScriptableObject,
                property.propertyPath);

            AddFieldBlock(container, field, hint, required);
        }

        /// <summary>
        /// 将相对序列化属性作为标准 PropertyField 添加到指定容器。
        /// </summary>
        public void AddRelativePropertyField(
            VisualElement container,
            ScriptableObject owner,
            SerializedObject serializedObject,
            SerializedProperty parent,
            string relativeName,
            string displayName,
            bool editable,
            string hint = null,
            bool required = false)
        {
            SerializedProperty property = parent.FindPropertyRelative(relativeName);

            if (property == null)
            {
                return;
            }

            VisualElement field;

            if (property.propertyType == SerializedPropertyType.String &&
                ConfigIdNamingUtility.IsIdPropertyName(property.name))
            {
                TextField idField = new TextField(displayName);
                ConfigIdNamingUtility.RestrictInput(idField);
                idField.BindProperty(property.Copy());
                field = idField;
            }
            else
            {
                PropertyField propertyField =
                    new PropertyField(property.Copy(), displayName);

                propertyField.Bind(serializedObject);
                field = propertyField;
            }

            field.SetEnabled(editable);
            field.AddToClassList("config-property-field");

            applyValidationVisual(field, owner, property.propertyPath);
            AddFieldBlock(container, field, hint, required);
        }

        /// <summary>
        /// 创建配置引用的只读显示区域。
        /// </summary>
        public VisualElement CreateReferenceDisplay(ScriptableObject config)
        {
            VisualElement display = new VisualElement();
            display.AddToClassList("object-reference-display");

            Label icon = new Label("◇");
            icon.AddToClassList("object-reference-icon");
            display.Add(icon);

            VisualElement textContainer = new VisualElement();
            textContainer.AddToClassList("object-reference-text");

            Label displayName = new Label(getObjectDisplayName(config));
            displayName.AddToClassList("object-reference-display-name");
            textContainer.Add(displayName);

            ScriptableObject sourceConfig = getSourceConfig(config);
            Label assetName = new Label(
                sourceConfig == null ? "未选择配置" : sourceConfig.name);
            assetName.AddToClassList("object-reference-asset-name");
            textContainer.Add(assetName);

            display.Add(textContainer);
            return display;
        }

        /// <summary>
        /// 将字段及其可选说明文字包装为统一的纵向字段组。
        /// </summary>
        private void AddFieldBlock(
            VisualElement container,
            VisualElement field,
            string hint,
            bool required)
        {
            VisualElement fieldBlock = new VisualElement();
            fieldBlock.AddToClassList("config-field-block");
            fieldBlock.Add(field);

            if (!string.IsNullOrEmpty(hint))
            {
                fieldBlock.Add(CreateFieldHint(hint, required));
            }

            container.Add(fieldBlock);
        }
    }
}
