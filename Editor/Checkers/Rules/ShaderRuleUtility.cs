using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    internal static class ShaderRuleUtility
    {
        private const string OptionalKeywordDirective = "multi_compile";
        private const string OptionalLocalKeywordDirective = "multi_compile_local";
        private const string ShaderFeatureDirective = "shader_feature";
        private const string LocalShaderFeatureDirective = "shader_feature_local";

        private static readonly Regex CommentRegex = new Regex(
            @"//.*?$|/\*.*?\*/",
            RegexOptions.Multiline | RegexOptions.Singleline);

        private static readonly Regex PragmaRegex = new Regex(
            @"^\s*#\s*pragma\s+(?<name>\S+)(?:\s+(?<arguments>.*?))?\s*$",
            RegexOptions.Multiline | RegexOptions.CultureInvariant);

        internal static bool TryGetPragmas(
            AssetContext context,
            out List<ShaderPragma> pragmas)
        {
            pragmas = new List<ShaderPragma>();

            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            Shader shader = AssetDatabase.LoadMainAssetAtPath(context.AssetPath) as Shader;

            if (shader == null ||
                !string.Equals(
                    Path.GetExtension(context.AssetPath),
                    ".shader",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string fullPath = Path.GetFullPath(context.AssetPath);

            if (!File.Exists(fullPath))
            {
                return false;
            }

            string source = File.ReadAllText(fullPath);
            string sourceWithoutComments = CommentRegex.Replace(source, string.Empty);
            MatchCollection matches = PragmaRegex.Matches(sourceWithoutComments);

            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                string arguments = match.Groups["arguments"].Value.Trim();
                string[] tokens = string.IsNullOrEmpty(arguments)
                    ? new string[0]
                    : Regex.Split(arguments, @"\s+");

                pragmas.Add(new ShaderPragma(
                    match.Groups["name"].Value,
                    tokens));
            }

            return true;
        }

        internal static int GetKeywordCount(List<ShaderPragma> pragmas)
        {
            HashSet<string> keywords = new HashSet<string>(StringComparer.Ordinal);

            if (pragmas == null)
            {
                return 0;
            }

            for (int i = 0; i < pragmas.Count; i++)
            {
                ShaderPragma pragma = pragmas[i];

                if (!IsOptionalKeywordDirective(pragma.Name))
                {
                    continue;
                }

                for (int j = 0; j < pragma.Arguments.Length; j++)
                {
                    string keyword = pragma.Arguments[j];

                    if (!IsDisabledKeyword(keyword))
                    {
                        keywords.Add(keyword);
                    }
                }
            }

            return keywords.Count;
        }

        internal static long GetVariantCountUpperBound(List<ShaderPragma> pragmas)
        {
            long variantCount = 1;

            if (pragmas == null)
            {
                return 0;
            }

            for (int i = 0; i < pragmas.Count; i++)
            {
                ShaderPragma pragma = pragmas[i];

                if (!IsOptionalKeywordDirective(pragma.Name) ||
                    pragma.Arguments.Length == 0)
                {
                    continue;
                }

                if (variantCount > long.MaxValue / pragma.Arguments.Length)
                {
                    return long.MaxValue;
                }

                variantCount *= pragma.Arguments.Length;
            }

            return variantCount;
        }

        internal static bool HasPragma(
            List<ShaderPragma> pragmas,
            string pragmaName)
        {
            if (pragmas == null || string.IsNullOrEmpty(pragmaName))
            {
                return false;
            }

            for (int i = 0; i < pragmas.Count; i++)
            {
                if (string.Equals(
                        pragmas[i].Name,
                        pragmaName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasTargetAtLeast(
            List<ShaderPragma> pragmas,
            decimal minimumTarget)
        {
            if (pragmas == null)
            {
                return false;
            }

            for (int i = 0; i < pragmas.Count; i++)
            {
                ShaderPragma pragma = pragmas[i];

                if (!string.Equals(pragma.Name, "target", StringComparison.OrdinalIgnoreCase) ||
                    pragma.Arguments.Length == 0)
                {
                    continue;
                }

                decimal target;

                if (decimal.TryParse(
                        pragma.Arguments[0],
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out target) &&
                    target >= minimumTarget)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsOptionalKeywordDirective(string directive)
        {
            return string.Equals(directive, OptionalKeywordDirective, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(directive, OptionalLocalKeywordDirective, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(directive, ShaderFeatureDirective, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(directive, LocalShaderFeatureDirective, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDisabledKeyword(string keyword)
        {
            return string.Equals(keyword, "_", StringComparison.Ordinal) ||
                   string.Equals(keyword, "__", StringComparison.Ordinal);
        }
    }

    public struct ShaderPragma
    {
        internal readonly string Name;
        internal readonly string[] Arguments;

        internal ShaderPragma(string name, string[] arguments)
        {
            Name = name;
            Arguments = arguments;
        }
    }
}