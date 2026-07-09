using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace majsoul.tools.assetscoretool.editor
{
    internal static class AssetScoreToolSpineSettings
    {
        internal const string DefineSymbol = "ASSET_SCORE_TOOL_SPINE";

        private const string SpineUnityAssemblyName = "spine-unity";
        private const string SpineCSharpAssemblyName = "spine-csharp";

        internal static bool IsSpineInstalled
        {
            get
            {
                return HasAssemblyDefinition(SpineUnityAssemblyName) &&
                       HasAssemblyDefinition(SpineCSharpAssemblyName);
            }
        }

        internal static bool IsEnabled
        {
            get
            {
                foreach (BuildTargetGroup group in GetValidBuildTargetGroups())
                {
                    try
                    {
                        if (ContainsDefine(
                                PlayerSettings.GetScriptingDefineSymbolsForGroup(group),
                                DefineSymbol))
                        {
                            return true;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                return false;
            }
        }

        internal static void SetEnabled(bool enabled)
        {
            foreach (BuildTargetGroup group in GetValidBuildTargetGroups())
            {
                try
                {
                    string defines =
                        PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
                    string updatedDefines = UpdateDefine(
                        defines,
                        DefineSymbol,
                        enabled);

                    if (!string.Equals(
                            defines,
                            updatedDefines,
                            StringComparison.Ordinal))
                    {
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(
                            group,
                            updatedDefines);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private static bool HasAssemblyDefinition(string assemblyName)
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:AssemblyDefinitionAsset");

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                if (string.Equals(
                        Path.GetFileNameWithoutExtension(path),
                        assemblyName,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<BuildTargetGroup> GetValidBuildTargetGroups()
        {
            foreach (BuildTargetGroup group in
                     Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown ||
                    (int)group == 15 ||
                    (int)group == 16)
                {
                    continue;
                }

                yield return group;
            }
        }

        private static bool ContainsDefine(string defines, string define)
        {
            string[] values = (defines ?? string.Empty).Split(';');

            for (int i = 0; i < values.Length; i++)
            {
                if (string.Equals(values[i], define, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string UpdateDefine(
            string defines,
            string define,
            bool enabled)
        {
            string[] values = (defines ?? string.Empty).Split(';');
            List<string> updatedValues = new List<string>();

            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrEmpty(values[i]) &&
                    !string.Equals(values[i], define, StringComparison.Ordinal))
                {
                    updatedValues.Add(values[i]);
                }
            }

            if (enabled)
            {
                updatedValues.Add(define);
            }

            return string.Join(";", updatedValues);
        }
    }
}