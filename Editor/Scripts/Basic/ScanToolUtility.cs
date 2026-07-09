using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    public class ScanToolUtility
    {
        /// <summary>
        /// 找到项目里所有的规则配置文件
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, ScoreRuleConfig> GetAllRuleConfigs()
        {
            Dictionary<string, ScoreRuleConfig> dictionary = new Dictionary<string, ScoreRuleConfig>();

            List<ScoreRuleConfig> configs = LoadScriptableObject<ScoreRuleConfig>();

            for (int i = 0; i < configs.Count; i++)
            {
                ScoreRuleConfig config = configs[i];

                if (config == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(config.RuleId))
                {
                    Debug.LogWarning("规则配置 RuleId 为空，已跳过：" + AssetDatabase.GetAssetPath(config));
                    continue;
                }

                if (dictionary.ContainsKey(config.RuleId))
                {
                    Debug.LogError(
                        "发现重复的规则配置 RuleId：" + config.RuleId +
                        "\n已存在配置：" + AssetDatabase.GetAssetPath(dictionary[config.RuleId]) +
                        "\n重复配置：" + AssetDatabase.GetAssetPath(config));
                    continue;
                }

                dictionary.Add(config.RuleId, config);
            }

            return dictionary;
        }

        /// <summary>
        /// 获取所有的规则检测器
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, IScoreRuleChecker> GetAllRuleCheckers()
        {
            Dictionary<string, IScoreRuleChecker> dictionary = new Dictionary<string, IScoreRuleChecker>();

            List<IScoreRuleChecker> checkers = LoadInterfaceInstances<IScoreRuleChecker>();

            for (int i = 0; i < checkers.Count; i++)
            {
                IScoreRuleChecker checker = checkers[i];

                if (checker == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(checker.RuleId))
                {
                    Debug.LogWarning("规则检测器 RuleId 为空，已跳过：" + checker.GetType().FullName);
                    continue;
                }

                if (dictionary.ContainsKey(checker.RuleId))
                {
                    Debug.LogError(
                        "发现重复的规则检测器 RuleId：" + checker.RuleId +
                        "\n已存在检测器：" + dictionary[checker.RuleId].GetType().FullName +
                        "\n重复检测器：" + checker.GetType().FullName);
                    continue;
                }

                dictionary.Add(checker.RuleId, checker);
            }

            return dictionary;
        }

        /// <summary>
        /// 获取所有定义的资源类型
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, AssetTypeConfig> GetAllAssetTypeConfigs()
        {
            Dictionary<string, AssetTypeConfig> dictionary = new Dictionary<string, AssetTypeConfig>();

            List<AssetTypeConfig> configs = LoadScriptableObject<AssetTypeConfig>();

            for (int i = 0; i < configs.Count; i++)
            {
                AssetTypeConfig config = configs[i];

                if (config == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(config.AssetTypeId))
                {
                    Debug.LogWarning("资源类型配置 AssetTypeId 为空，已跳过：" + AssetDatabase.GetAssetPath(config));
                    continue;
                }

                if (dictionary.ContainsKey(config.AssetTypeId))
                {
                    Debug.LogError(
                        "发现重复的资源类型配置 AssetTypeId：" + config.AssetTypeId +
                        "\n已存在配置：" + AssetDatabase.GetAssetPath(dictionary[config.AssetTypeId]) +
                        "\n重复配置：" + AssetDatabase.GetAssetPath(config));
                    continue;
                }

                dictionary.Add(config.AssetTypeId, config);
            }

            return dictionary;
        }

        public static Dictionary<string, IAssetTypeChecker> GetAllAssetTypeCheckers()
        {
            Dictionary<string, IAssetTypeChecker> dictionary = new Dictionary<string, IAssetTypeChecker>();

            List<IAssetTypeChecker> checkers = LoadInterfaceInstances<IAssetTypeChecker>();

            for (int i = 0; i < checkers.Count; i++)
            {
                IAssetTypeChecker checker = checkers[i];

                if (checker == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(checker.AssetTypeId))
                {
                    Debug.LogWarning("资源类型检测器 AssetTypeId 为空，已跳过：" + checker.GetType().FullName);
                    continue;
                }

                if (dictionary.ContainsKey(checker.AssetTypeId))
                {
                    Debug.LogError(
                        "发现重复的资源类型检测器 AssetTypeId：" + checker.AssetTypeId +
                        "\n已存在检测器：" + dictionary[checker.AssetTypeId].GetType().FullName +
                        "\n重复检测器：" + checker.GetType().FullName);
                    continue;
                }

                dictionary.Add(checker.AssetTypeId, checker);
            }

            return dictionary;
        }

        public static Dictionary<string, RuleApplyItem> GetAllRuleApplyConfigs()
        {
            Dictionary<string, RuleApplyItem> dictionary = new Dictionary<string, RuleApplyItem>();

            List<RuleApplyConfig> configs = LoadScriptableObject<RuleApplyConfig>();

            for (int i = 0; i < configs.Count; i++)
            {
                RuleApplyConfig config = configs[i];

                if (config == null || config.RuleApplyItems == null)
                {
                    continue;
                }

                string configPath = AssetDatabase.GetAssetPath(config);

                for (int j = 0; j < config.RuleApplyItems.Length; j++)
                {
                    RuleApplyItem item = config.RuleApplyItems[j];

                    if (item == null)
                    {
                        continue;
                    }

                    if (item.RuleConfig == null)
                    {
                        Debug.LogWarning("规则应用配置 RuleConfig 为空，已跳过：" + configPath + " index=" + j);
                        continue;
                    }

                    string ruleId = item.RuleId;

                    if (string.IsNullOrEmpty(ruleId))
                    {
                        Debug.LogWarning("规则应用配置 RuleId 为空，已跳过：" + configPath + " index=" + j);
                        continue;
                    }

                    if (dictionary.ContainsKey(ruleId))
                    {
                        Debug.LogError(
                            "发现重复的规则应用配置 RuleId：" + ruleId +
                            "\n已存在规则配置：" + AssetDatabase.GetAssetPath(dictionary[ruleId].RuleConfig) +
                            "\n重复配置文件：" + configPath +
                            "\n重复 index=" + j);
                        continue;
                    }

                    dictionary.Add(ruleId, item);
                }
            }

            return dictionary;
        }

        /// <summary>
        /// 找到项目里所有的检测配置档。
        /// Key 是 ProfileId。
        /// </summary>
        public static Dictionary<string, ScoreRuleProfileConfig> GetAllProfileConfigs()
        {
            Dictionary<string, ScoreRuleProfileConfig> dictionary = new Dictionary<string, ScoreRuleProfileConfig>();

            List<ScoreRuleProfileConfig> configs = LoadScriptableObject<ScoreRuleProfileConfig>();

            for (int i = 0; i < configs.Count; i++)
            {
                ScoreRuleProfileConfig config = configs[i];

                if (config == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(config.ProfileId))
                {
                    Debug.LogWarning("检测配置档 ProfileId 为空，已跳过：" + AssetDatabase.GetAssetPath(config));
                    continue;
                }

                if (dictionary.ContainsKey(config.ProfileId))
                {
                    Debug.LogError(
                        "发现重复的检测配置档 ProfileId：" + config.ProfileId +
                        "\n已存在配置：" + AssetDatabase.GetAssetPath(dictionary[config.ProfileId]) +
                        "\n重复配置：" + AssetDatabase.GetAssetPath(config));
                    continue;
                }

                dictionary.Add(config.ProfileId, config);
            }

            return dictionary;
        }


        /// <summary>
        /// 搜索项目中的指定类型ScriptableObject
        /// </summary>
        public static List<TConfig> LoadScriptableObject<TConfig>() where TConfig : ScriptableObject
        {
            List<TConfig> list = new List<TConfig>();

            string typeName = typeof(TConfig).Name;
            string[] guids = AssetDatabase.FindAssets("t:" + typeName);

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                TConfig config = AssetDatabase.LoadAssetAtPath<TConfig>(assetPath);

                if (config == null)
                {
                    continue;
                }

                list.Add(config);
            }

            return list;
        }

        /// <summary>
        /// 搜索所有实现指定接口的类，并创建实例。
        /// </summary>
        public static List<T> LoadInterfaceInstances<T>() where T : class
        {
            List<T> list = new List<T>();

            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<T>();

            foreach (Type type in types)
            {
                if (type == null)
                {
                    continue;
                }

                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                if (type.ContainsGenericParameters)
                {
                    continue;
                }

                T checker = null;

                try
                {
                    checker = Activator.CreateInstance(type) as T;
                }
                catch (Exception e)
                {
                    Debug.LogError("创建 Checker 失败：" + type.FullName + "\n" + e);
                    continue;
                }

                if (checker == null)
                {
                    continue;
                }

                list.Add(checker);
            }

            return list;
        }

        /// <summary>
        /// 统一路径格式。
        /// Unity 资源路径建议统一使用 /。
        /// </summary>
        public static string NormalizePath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return string.Empty;
            }

            return assetPath.Replace("\\", "/");
        }
    }
}