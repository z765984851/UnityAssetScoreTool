using System;
using System.Text;
using UnityEngine.UIElements;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 统一配置 ID 的命名校验和编辑器输入过滤规则。
    /// </summary>
    internal static class ConfigIdNamingUtility
    {
        public const string RuleHint =
            "仅允许小写字母、数字、下划线和点；必须以小写字母开头、以小写字母或数字结尾，分隔符不能连续。";

        /// <summary>
        /// 判断字符串是否完整符合配置 ID 命名规范。
        /// </summary>
        public static bool IsValid(string value)
        {
            if (string.IsNullOrEmpty(value) ||
                !IsLowercaseLetter(value[0]))
            {
                return false;
            }

            bool previousWasSeparator = false;

            for (int i = 1; i < value.Length; i++)
            {
                char character = value[i];

                if (IsLowercaseLetter(character) ||
                    IsDigit(character))
                {
                    previousWasSeparator = false;
                    continue;
                }

                if (!IsSeparator(character) ||
                    previousWasSeparator)
                {
                    return false;
                }

                previousWasSeparator = true;
            }

            return !previousWasSeparator;
        }

        /// <summary>
        /// 过滤 ID 输入中的非法字符，并保留尚待补全的末尾分隔符。
        /// </summary>
        public static string FilterInput(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder result = new StringBuilder(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];

                if (result.Length == 0)
                {
                    if (IsLowercaseLetter(character))
                    {
                        result.Append(character);
                    }

                    continue;
                }

                if (IsLowercaseLetter(character) ||
                    IsDigit(character))
                {
                    result.Append(character);
                    continue;
                }

                if (IsSeparator(character) &&
                    !IsSeparator(result[result.Length - 1]))
                {
                    result.Append(character);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// 为 TextField 注册配置 ID 输入限制，并阻止非法值继续传播到绑定属性。
        /// </summary>
        public static void RestrictInput(TextField field)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            field.RegisterValueChangedCallback(
                delegate(ChangeEvent<string> evt)
                {
                    string filtered = FilterInput(evt.newValue);

                    if (string.Equals(
                            filtered,
                            evt.newValue,
                            StringComparison.Ordinal))
                    {
                        return;
                    }

                    evt.StopImmediatePropagation();
                    field.value = filtered;
                });
        }

        /// <summary>
        /// 判断序列化字段名是否属于需要应用统一规则的 ID 字段。
        /// </summary>
        public static bool IsIdPropertyName(string propertyName)
        {
            return propertyName == "profileId" ||
                   propertyName == "ruleApplyId" ||
                   propertyName == "ruleId" ||
                   propertyName == "assetTypeId" ||
                   propertyName == "issueInfoId";
        }

        /// <summary>
        /// 判断字符是否为 ASCII 小写字母。
        /// </summary>
        private static bool IsLowercaseLetter(char character)
        {
            return character >= 'a' && character <= 'z';
        }

        /// <summary>
        /// 判断字符是否为 ASCII 数字。
        /// </summary>
        private static bool IsDigit(char character)
        {
            return character >= '0' && character <= '9';
        }

        /// <summary>
        /// 判断字符是否为允许的 ID 分隔符。
        /// </summary>
        private static bool IsSeparator(char character)
        {
            return character == '_' || character == '.';
        }
    }
}
