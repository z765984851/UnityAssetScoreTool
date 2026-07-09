using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 管理配置窗口使用的配置资产与可编辑工作副本。
    /// 该类不依赖具体窗口和 UI，只负责数据生命周期与统一保存行为。
    /// </summary>
    internal sealed class ConfigWindowData : IDisposable
    {
        private sealed class ConfigEditSession
        {
            public ScriptableObject Source;
            public ScriptableObject WorkingCopy;
            public string InitialJson;
            public string AssetPath;
            public bool IsPendingCreate;
            public bool IsPendingDelete;
        }

        private sealed class PendingCheckerScript
        {
            public ScoreRuleConfig RuleConfig;
            public string ClassName;
            public string ScriptPath;
        }

        private readonly List<ConfigEditSession> editSessions =
            new List<ConfigEditSession>();

        private readonly List<PendingCheckerScript> pendingCheckerScripts =
            new List<PendingCheckerScript>();

        public IEnumerable<ScriptableObject> WorkingCopies
        {
            get
            {
                for (int i = 0; i < editSessions.Count; i++)
                {
                    if (!editSessions[i].IsPendingDelete)
                    {
                        yield return editSessions[i].WorkingCopy;
                    }
                }
            }
        }

        public int DirtyCount
        {
            get
            {
                int dirtyCount = 0;

                for (int i = 0; i < editSessions.Count; i++)
                {
                    if (IsSessionDirty(editSessions[i]))
                    {
                        dirtyCount++;
                    }
                }

                return dirtyCount + pendingCheckerScripts.Count;
            }
        }

        public bool HasUnsavedChanges
        {
            get { return DirtyCount > 0; }
        }

        /// <summary>
        /// 重新发现本地配置资产，并为每个资产创建独立工作副本。
        /// </summary>
        public void Initialize()
        {
            Dispose();
            Sync();
        }

        /// <summary>
        /// 补充新创建或新复制的配置资产，不替换已有工作副本。
        /// </summary>
        public void Sync()
        {
            RegisterEditSessions<ScoreRuleProfileConfig>();
            RegisterEditSessions<RuleApplyConfig>();
            RegisterEditSessions<ScoreRuleConfig>();
            RegisterEditSessions<AssetTypeConfig>();
            RegisterEditSessions<UserSettingConfig>();
        }

        /// <summary>
        /// 按实际配置类型读取当前编辑数据。
        /// Package 配置和待创建配置会返回，待删除配置会被排除。
        /// </summary>
        public List<ScriptableObject> LoadConfigsByType(
            Type configType)
        {
            List<ScriptableObject> result =
                new List<ScriptableObject>();

            if (configType == null)
            {
                return result;
            }

            string[] guids =
                AssetDatabase.FindAssets(
                    "t:" + configType.Name);

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath =
                    AssetDatabase.GUIDToAssetPath(guids[i]);

                ScriptableObject config =
                    AssetDatabase.LoadAssetAtPath(
                        assetPath,
                        configType) as ScriptableObject;

                ConfigEditSession session =
                    FindEditSession(config);

                if (config != null &&
                    (session == null ||
                     !session.IsPendingDelete))
                {
                    result.Add(config);
                }
            }

            for (int i = 0; i < editSessions.Count; i++)
            {
                ConfigEditSession session = editSessions[i];

                if (session.IsPendingCreate &&
                    !session.IsPendingDelete &&
                    session.WorkingCopy != null &&
                    session.WorkingCopy.GetType() == configType)
                {
                    result.Add(session.WorkingCopy);
                }
            }

            return result;
        }

        public void RegisterEditSession(
            ScriptableObject source)
        {
            if (source == null ||
                IsPackageAsset(
                    AssetDatabase.GetAssetPath(source)) ||
                FindEditSession(source) != null)
            {
                return;
            }

            ScriptableObject workingCopy =
                CreateWorkingCopy(source);

            ConfigEditSession session =
                new ConfigEditSession();

            session.Source = source;
            session.WorkingCopy = workingCopy;
            session.AssetPath = AssetDatabase.GetAssetPath(source);
            session.InitialJson =
                EditorJsonUtility.ToJson(workingCopy);

            editSessions.Add(session);
        }

        public TConfig GetWorkingConfig<TConfig>(
            TConfig config)
            where TConfig : ScriptableObject
        {
            ConfigEditSession session =
                FindEditSession(config);

            return session == null
                ? config
                : session.WorkingCopy as TConfig;
        }

        public ScriptableObject GetWorkingConfig(
            ScriptableObject config)
        {
            ConfigEditSession session =
                FindEditSession(config);

            return session == null
                ? config
                : session.WorkingCopy;
        }

        public ScriptableObject GetSourceConfig(
            ScriptableObject config)
        {
            ConfigEditSession session =
                FindEditSession(config);

            return session == null
                ? config
                : session.Source == null
                    ? session.WorkingCopy
                    : session.Source;
        }

        /// <summary>
        /// 返回配置的真实路径或尚未保存配置的计划路径。
        /// </summary>
        public string GetConfigAssetPath(
            ScriptableObject config)
        {
            ConfigEditSession session =
                FindEditSession(config);

            return session == null
                ? AssetDatabase.GetAssetPath(config)
                : session.AssetPath;
        }

        public bool IsWorkingCopy(
            ScriptableObject config)
        {
            ConfigEditSession session =
                FindEditSession(config);

            return session != null &&
                   session.WorkingCopy == config;
        }

        /// <summary>
        /// 提交全部待创建、待修改和待删除操作，并在成功后重建工作副本。
        /// </summary>
        public bool CommitWorkingCopies(
            out string errorMessage)
        {
            if (!ValidatePendingPaths(out errorMessage))
            {
                return false;
            }

            try
            {
                List<ConfigEditSession> createdSessions =
                    new List<ConfigEditSession>();

                for (int i = 0; i < editSessions.Count; i++)
                {
                    ConfigEditSession session = editSessions[i];

                    if (!session.IsPendingCreate ||
                        session.IsPendingDelete)
                    {
                        continue;
                    }

                    ScriptableObject createdAsset =
                        session.WorkingCopy;

                    createdAsset.hideFlags = HideFlags.None;
                    AssetDatabase.CreateAsset(
                        createdAsset,
                        session.AssetPath);

                    session.Source = createdAsset;
                    session.IsPendingCreate = false;
                    createdSessions.Add(session);
                }

                // 全部待创建对象先取得持久化身份，再保存彼此之间的引用。
                for (int i = 0;
                     i < createdSessions.Count;
                     i++)
                {
                    ConfigEditSession session =
                        createdSessions[i];

                    EditorUtility.SetDirty(session.Source);
                    session.WorkingCopy =
                        CreateWorkingCopy(session.Source);
                    session.InitialJson =
                        EditorJsonUtility.ToJson(
                            session.WorkingCopy);
                }

                for (int i = 0; i < editSessions.Count; i++)
                {
                    ConfigEditSession session = editSessions[i];

                    if (session.IsPendingDelete ||
                        !IsSessionDirty(session))
                    {
                        continue;
                    }

                    Undo.RecordObject(
                        session.Source,
                        "保存配置");

                    EditorUtility.CopySerialized(
                        session.WorkingCopy,
                        session.Source);

                    EditorUtility.SetDirty(session.Source);
                }

                WritePendingCheckerScripts();

                for (int i = 0; i < editSessions.Count; i++)
                {
                    ConfigEditSession session = editSessions[i];

                    if (session.IsPendingDelete &&
                        !string.IsNullOrEmpty(session.AssetPath))
                    {
                        if (!AssetDatabase.DeleteAsset(
                                session.AssetPath))
                        {
                            throw new InvalidOperationException(
                                "无法删除配置文件：" +
                                session.AssetPath);
                        }
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Initialize();
                errorMessage = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                errorMessage =
                    "保存配置时发生异常：" + exception.Message;
                return false;
            }
        }

        /// <summary>
        /// 丢弃工作副本内容并重新从真实配置资产创建副本。
        /// </summary>
        public void ResetWorkingCopies()
        {
            Initialize();
        }

        /// <summary>
        /// 将配置标记为待删除，并立即清理当前编辑数据中的已知关联。
        /// </summary>
        public bool MarkConfigForDeletion(
            ScriptableObject config)
        {
            ConfigEditSession session =
                FindEditSession(config);

            if (session == null)
            {
                return false;
            }

            session.IsPendingDelete = true;
            CleanupReferencesToConfig(session);
            RemovePendingCheckerScripts(session);

            if (session.IsPendingCreate)
            {
                DestroyWorkingCopy(session.WorkingCopy);
                editSessions.Remove(session);
            }

            return true;
        }

        /// <summary>
        /// 清理所有规则应用配置中对指定规则问题项的白名单引用。
        /// </summary>
        public void CleanupReferencesToIssue(
            ScoreRuleConfig ruleConfig,
            string issueInfoId)
        {
            ConfigEditSession ruleSession =
                FindEditSession(ruleConfig);

            if (ruleSession == null ||
                string.IsNullOrEmpty(issueInfoId) ||
                ContainsIssueId(ruleConfig, issueInfoId))
            {
                return;
            }

            for (int i = 0; i < editSessions.Count; i++)
            {
                ConfigEditSession ownerSession = editSessions[i];

                if (ownerSession.IsPendingDelete ||
                    !(ownerSession.WorkingCopy is RuleApplyConfig))
                {
                    continue;
                }

                CleanupWhiteListIssueReferences(
                    ownerSession.WorkingCopy,
                    ruleSession,
                    issueInfoId);
            }
        }

        /// <summary>
        /// 判断规则当前是否仍包含指定问题 ID。
        /// </summary>
        private bool ContainsIssueId(
            ScoreRuleConfig ruleConfig,
            string issueInfoId)
        {
            IssueInfo[] issueInfos = ruleConfig.IssueInfos;

            if (issueInfos == null)
            {
                return false;
            }

            for (int i = 0; i < issueInfos.Length; i++)
            {
                if (issueInfos[i] != null &&
                    string.Equals(
                        issueInfos[i].issueInfoId,
                        issueInfoId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断配置是否仍属于当前待保存的数据集合。
        /// </summary>
        public bool IsConfigAvailable(
            ScriptableObject config)
        {
            if (config == null)
            {
                return false;
            }

            ConfigEditSession session =
                FindEditSession(config);

            if (session != null)
            {
                return !session.IsPendingDelete;
            }

            return !string.IsNullOrEmpty(
                AssetDatabase.GetAssetPath(config));
        }

        /// <summary>
        /// 向 RuleApplyConfig 工作副本追加规则应用项。
        /// </summary>
        public void AddRuleApplyItem(
            RuleApplyConfig owner,
            string arrayPath,
            bool isEnabled,
            ScoreRuleConfig ruleConfig,
            AssetTypeConfig[] assetTypeConfigs,
            ScoreRuleProfileConfig[] profiles)
        {
            Undo.RecordObject(owner, "添加规则应用项");

            SerializedObject serializedObject =
                new SerializedObject(owner);

            SerializedProperty arrayProperty =
                serializedObject.FindProperty(arrayPath);

            int index = arrayProperty.arraySize;
            arrayProperty.InsertArrayElementAtIndex(index);

            SerializedProperty item =
                arrayProperty.GetArrayElementAtIndex(index);

            item.FindPropertyRelative("isEnabled").boolValue =
                isEnabled;
            item.FindPropertyRelative("ruleConfig")
                .objectReferenceValue = ruleConfig;

            SerializedProperty assetTypes =
                item.FindPropertyRelative("assetTypeConfigs");

            assetTypes.arraySize = assetTypeConfigs.Length;

            for (int i = 0; i < assetTypeConfigs.Length; i++)
            {
                assetTypes.GetArrayElementAtIndex(i)
                    .objectReferenceValue = assetTypeConfigs[i];
            }

            SerializedProperty profileProperty =
                item.FindPropertyRelative("profiles");

            profileProperty.arraySize = profiles.Length;

            for (int i = 0; i < profiles.Length; i++)
            {
                profileProperty.GetArrayElementAtIndex(i)
                    .objectReferenceValue = profiles[i];
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 向检测规则工作副本追加问题定义。
        /// </summary>
        public void AddIssueInfo(
            ScoreRuleConfig owner,
            string arrayPath,
            string issueInfoId,
            string displayName,
            int deductScore,
            int issueLevelIndex,
            string issueMessage)
        {
            Undo.RecordObject(owner, "添加问题项");

            SerializedObject serializedObject =
                new SerializedObject(owner);

            SerializedProperty arrayProperty =
                serializedObject.FindProperty(arrayPath);

            int index = arrayProperty.arraySize;
            arrayProperty.InsertArrayElementAtIndex(index);

            SerializedProperty item =
                arrayProperty.GetArrayElementAtIndex(index);

            item.FindPropertyRelative("issueInfoId").stringValue =
                issueInfoId;
            item.FindPropertyRelative("displayName").stringValue =
                displayName;
            item.FindPropertyRelative("deductScore").intValue =
                deductScore;
            item.FindPropertyRelative("issueLevel").enumValueIndex =
                issueLevelIndex;
            item.FindPropertyRelative("issueMessage").stringValue =
                issueMessage;

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 向 RuleApplyConfig 工作副本追加白名单项。
        /// </summary>
        public void AddWhiteListItem(
            RuleApplyConfig owner,
            string assetPath,
            ScoreRuleConfig ruleConfig,
            string issueInfoId,
            string remark)
        {
            Undo.RecordObject(owner, "添加白名单");

            SerializedObject serializedObject =
                new SerializedObject(owner);

            SerializedProperty arrayProperty =
                serializedObject.FindProperty("whiteListItems");

            int index = arrayProperty.arraySize;
            arrayProperty.InsertArrayElementAtIndex(index);

            SerializedProperty item =
                arrayProperty.GetArrayElementAtIndex(index);

            item.FindPropertyRelative("assetPath").stringValue =
                assetPath;
            item.FindPropertyRelative("ruleConfig")
                .objectReferenceValue = ruleConfig;
            item.FindPropertyRelative("issueInfoId").stringValue =
                issueInfoId;
            item.FindPropertyRelative("remark").stringValue = remark;

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 创建待保存配置，并将其纳入当前工作副本会话。
        /// </summary>
        public ScriptableObject CreatePendingConfig(
            Type configType,
            string idPropertyName,
            string assetName,
            string configId,
            string displayName,
            string saveFolder)
        {
            ScriptableObject config =
                ScriptableObject.CreateInstance(configType);

            SetStringProperty(
                config,
                idPropertyName,
                configId);
            SetStringProperty(
                config,
                "displayName",
                displayName);

            config.name = assetName;
            config.hideFlags =
                HideFlags.HideInHierarchy |
                HideFlags.DontSave;

            ConfigEditSession session =
                new ConfigEditSession();

            session.WorkingCopy = config;
            session.AssetPath = GenerateUniquePendingPath(
                saveFolder.Replace("\\", "/") +
                "/" + assetName + ".asset");
            session.InitialJson = string.Empty;
            session.IsPendingCreate = true;

            editSessions.Add(session);

            return config;
        }

        /// <summary>
        /// 创建指定配置的待保存本地副本。
        /// </summary>
        public ScriptableObject CreatePendingConfigCopy(
            ScriptableObject config,
            string targetPath)
        {
            ScriptableObject displayConfig =
                GetWorkingConfig(config);

            if (displayConfig == null)
            {
                return null;
            }

            ScriptableObject workingCopy =
                UnityEngine.Object.Instantiate(displayConfig);

            workingCopy.name =
                Path.GetFileNameWithoutExtension(targetPath);
            workingCopy.hideFlags =
                HideFlags.HideInHierarchy |
                HideFlags.DontSave;

            ConfigEditSession session =
                new ConfigEditSession();

            session.WorkingCopy = workingCopy;
            session.AssetPath = GenerateUniquePendingPath(
                targetPath.Replace("\\", "/"));
            session.InitialJson = string.Empty;
            session.IsPendingCreate = true;

            editSessions.Add(session);

            return workingCopy;
        }

        /// <summary>
        /// 修改工作副本中的对象引用字段。
        /// </summary>
        public void SetObjectReference(
            ScriptableObject owner,
            string propertyPath,
            UnityEngine.Object value)
        {
            Undo.RecordObject(owner, "修改配置引用");

            SerializedObject serializedObject =
                new SerializedObject(owner);

            SerializedProperty property =
                serializedObject.FindProperty(propertyPath);

            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 修改工作副本中的布尔字段。
        /// </summary>
        public void SetBoolean(
            ScriptableObject owner,
            string propertyPath,
            bool value)
        {
            Undo.RecordObject(owner, "修改配置开关");

            SerializedObject serializedObject =
                new SerializedObject(owner);

            SerializedProperty property =
                serializedObject.FindProperty(propertyPath);

            if (property == null)
            {
                return;
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 登记与指定规则关联的待创建 CheckRule 脚本。
        /// </summary>
        public bool QueueCheckerScript(
            ScoreRuleConfig ruleConfig,
            string className,
            string saveFolder,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (ruleConfig == null)
            {
                errorMessage = "没有可关联的检测规则配置。";
                return false;
            }

            className = SanitizeCheckerClassName(className);
            saveFolder = saveFolder.Replace("\\", "/");

            if (!AssetDatabase.IsValidFolder(saveFolder) ||
                !(saveFolder == "Assets" ||
                  saveFolder.StartsWith(
                      "Assets/",
                      StringComparison.Ordinal)))
            {
                errorMessage =
                    "检测脚本必须创建在 Assets 目录下的有效文件夹中。";
                return false;
            }

            string scriptPath =
                saveFolder + "/" + className + ".cs";

            string projectRoot =
                Directory.GetParent(Application.dataPath).FullName;

            string absolutePath =
                Path.GetFullPath(
                    Path.Combine(projectRoot, scriptPath));

            if (File.Exists(absolutePath) ||
                HasPendingCheckerScript(scriptPath))
            {
                errorMessage = "目标脚本已经存在。";
                return false;
            }

            PendingCheckerScript pendingScript =
                new PendingCheckerScript();

            pendingScript.RuleConfig = ruleConfig;
            pendingScript.ClassName = className;
            pendingScript.ScriptPath = scriptPath;

            pendingCheckerScripts.Add(pendingScript);
            return true;
        }

        /// <summary>
        /// 判断指定规则是否已有尚未保存的检测脚本创建操作。
        /// </summary>
        public bool HasPendingCheckerForRule(
            ScoreRuleConfig ruleConfig)
        {
            ConfigEditSession ruleSession =
                FindEditSession(ruleConfig);

            for (int i = 0;
                 i < pendingCheckerScripts.Count;
                 i++)
            {
                ScoreRuleConfig pendingRule =
                    pendingCheckerScripts[i].RuleConfig;

                if ((ruleSession != null &&
                     MatchesConfigReference(
                         pendingRule,
                         ruleSession)) ||
                    pendingRule == ruleConfig)
                {
                    return true;
                }
            }

            return false;
        }

        public string BuildCheckerClassName(string ruleId)
        {
            string baseName =
                string.IsNullOrEmpty(ruleId)
                    ? "NewRule"
                    : ruleId;

            if (!baseName.EndsWith(
                    "Checker",
                    StringComparison.Ordinal))
            {
                baseName += "Checker";
            }

            return SanitizeCheckerClassName(baseName);
        }

        public void Dispose()
        {
            for (int i = 0; i < editSessions.Count; i++)
            {
                ConfigEditSession session = editSessions[i];

                // 提交异常时，已创建资产可能暂时同时是 Source 和 WorkingCopy。
                if (session.WorkingCopy != session.Source)
                {
                    DestroyWorkingCopy(session.WorkingCopy);
                }
            }

            editSessions.Clear();
            pendingCheckerScripts.Clear();
        }

        public static bool IsPackageAsset(
            string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath) &&
                   assetPath.StartsWith(
                       "Packages/",
                       StringComparison.Ordinal);
        }

        private void RegisterEditSessions<TConfig>()
            where TConfig : ScriptableObject
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    "t:" + typeof(TConfig).Name);

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath =
                    AssetDatabase.GUIDToAssetPath(guids[i]);

                if (IsPackageAsset(assetPath))
                {
                    continue;
                }

                RegisterEditSession(
                    AssetDatabase.LoadAssetAtPath<TConfig>(assetPath));
            }
        }

        private ScriptableObject CreateWorkingCopy(
            ScriptableObject source)
        {
            ScriptableObject workingCopy =
                UnityEngine.Object.Instantiate(source);

            workingCopy.name = source.name;

            // HideAndDontSave 包含 NotEditable，不能用于可编辑工作副本。
            workingCopy.hideFlags =
                HideFlags.HideInHierarchy |
                HideFlags.DontSave;

            return workingCopy;
        }

        private ConfigEditSession FindEditSession(
            ScriptableObject config)
        {
            if (config == null)
            {
                return null;
            }

            for (int i = 0; i < editSessions.Count; i++)
            {
                ConfigEditSession session =
                    editSessions[i];

                if (session.Source == config ||
                    session.WorkingCopy == config)
                {
                    return session;
                }
            }

            return null;
        }

        private bool IsSessionDirty(
            ConfigEditSession session)
        {
            if (session.IsPendingCreate ||
                session.IsPendingDelete)
            {
                return true;
            }

            string currentJson =
                EditorJsonUtility.ToJson(
                    session.WorkingCopy);

            return !string.Equals(
                currentJson,
                session.InitialJson,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// 检查所有待创建路径在提交时仍然可用。
        /// </summary>
        private bool ValidatePendingPaths(
            out string errorMessage)
        {
            HashSet<string> paths =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < editSessions.Count; i++)
            {
                ConfigEditSession session = editSessions[i];

                if (!session.IsPendingCreate ||
                    session.IsPendingDelete)
                {
                    continue;
                }

                if (!paths.Add(session.AssetPath) ||
                    !IsValidAssetParentFolder(
                        session.AssetPath) ||
                    AssetDatabase.LoadMainAssetAtPath(
                        session.AssetPath) != null ||
                    File.Exists(ToAbsoluteProjectPath(
                        session.AssetPath)))
                {
                    errorMessage =
                        "配置保存路径已经存在：" +
                        session.AssetPath;
                    return false;
                }
            }

            for (int i = 0;
                 i < pendingCheckerScripts.Count;
                 i++)
            {
                string scriptPath =
                    pendingCheckerScripts[i].ScriptPath;

                if (!paths.Add(scriptPath) ||
                    !IsValidAssetParentFolder(scriptPath) ||
                    File.Exists(ToAbsoluteProjectPath(scriptPath)))
                {
                    errorMessage =
                        "检测脚本保存路径已经存在：" +
                        scriptPath;
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// 判断待创建文件的父目录是否仍是有效 Unity 资源目录。
        /// </summary>
        private bool IsValidAssetParentFolder(
            string assetPath)
        {
            string folder = Path.GetDirectoryName(assetPath);

            return !string.IsNullOrEmpty(folder) &&
                   AssetDatabase.IsValidFolder(
                       folder.Replace("\\", "/"));
        }

        /// <summary>
        /// 将全部待创建检测脚本写入各自的计划路径。
        /// </summary>
        private void WritePendingCheckerScripts()
        {
            for (int i = 0;
                 i < pendingCheckerScripts.Count;
                 i++)
            {
                PendingCheckerScript pendingScript =
                    pendingCheckerScripts[i];

                ScoreRuleConfig workingRule =
                    GetWorkingConfig(
                        pendingScript.RuleConfig)
                    as ScoreRuleConfig;

                string ruleId = workingRule == null
                    ? string.Empty
                    : workingRule.RuleId;

                File.WriteAllText(
                    ToAbsoluteProjectPath(
                        pendingScript.ScriptPath),
                    BuildCheckerScriptContent(
                        pendingScript.ClassName,
                        ruleId),
                    new UTF8Encoding(false));
            }
        }

        /// <summary>
        /// 为待创建配置生成不会与磁盘资产或其他待创建配置冲突的路径。
        /// </summary>
        private string GenerateUniquePendingPath(
            string requestedPath)
        {
            string candidate =
                AssetDatabase.GenerateUniqueAssetPath(
                    requestedPath);

            string directory =
                Path.GetDirectoryName(candidate)
                    .Replace("\\", "/");
            string fileName =
                Path.GetFileNameWithoutExtension(candidate);
            string extension = Path.GetExtension(candidate);
            int suffix = 1;

            while (HasPendingAssetPath(candidate))
            {
                candidate =
                    directory + "/" + fileName + " " +
                    suffix + extension;
                candidate =
                    AssetDatabase.GenerateUniqueAssetPath(
                        candidate);
                suffix++;
            }

            return candidate;
        }

        /// <summary>
        /// 判断指定资产路径是否已被其他待创建配置占用。
        /// </summary>
        private bool HasPendingAssetPath(string assetPath)
        {
            for (int i = 0; i < editSessions.Count; i++)
            {
                ConfigEditSession session = editSessions[i];

                if (session.IsPendingCreate &&
                    !session.IsPendingDelete &&
                    string.Equals(
                        session.AssetPath,
                        assetPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断指定脚本路径是否已经登记为待创建。
        /// </summary>
        private bool HasPendingCheckerScript(
            string scriptPath)
        {
            for (int i = 0;
                 i < pendingCheckerScripts.Count;
                 i++)
            {
                if (string.Equals(
                        pendingCheckerScripts[i].ScriptPath,
                        scriptPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 清理所有工作副本中对待删除配置的已知引用。
        /// </summary>
        private void CleanupReferencesToConfig(
            ConfigEditSession targetSession)
        {
            for (int i = 0; i < editSessions.Count; i++)
            {
                ConfigEditSession ownerSession = editSessions[i];

                if (ownerSession == targetSession ||
                    ownerSession.IsPendingDelete)
                {
                    continue;
                }

                if (ownerSession.WorkingCopy is RuleApplyConfig)
                {
                    CleanupRuleApplyReferences(
                        ownerSession.WorkingCopy,
                        targetSession);
                }
                else if (ownerSession.WorkingCopy is UserSettingConfig &&
                         targetSession.WorkingCopy is RuleApplyConfig)
                {
                    CleanupProjectSettingsReference(
                        ownerSession.WorkingCopy,
                        targetSession);
                }
            }
        }

        /// <summary>
        /// 清理规则应用配置中对待删除配置的规则、类型、配置档和白名单引用。
        /// </summary>
        private void CleanupRuleApplyReferences(
            ScriptableObject owner,
            ConfigEditSession targetSession)
        {
            SerializedObject serializedObject =
                new SerializedObject(owner);
            bool changed = false;

            SerializedProperty ruleApplyItems =
                serializedObject.FindProperty("ruleApplyItems");

            if (targetSession.WorkingCopy is ScoreRuleConfig)
            {
                changed |= RemoveArrayItemsReferencing(
                    ruleApplyItems,
                    "ruleConfig",
                    targetSession);

                changed |= RemoveArrayItemsReferencing(
                    serializedObject.FindProperty("whiteListItems"),
                    "ruleConfig",
                    targetSession);
            }
            else if (targetSession.WorkingCopy is AssetTypeConfig)
            {
                changed |= RemoveNestedReferences(
                    ruleApplyItems,
                    "assetTypeConfigs",
                    targetSession);
            }
            else if (targetSession.WorkingCopy is
                     ScoreRuleProfileConfig)
            {
                changed |= RemoveNestedReferences(
                    ruleApplyItems,
                    "profiles",
                    targetSession);
            }

            if (changed)
            {
                Undo.RecordObject(
                    owner,
                    "清理已删除配置引用");
                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// 清理项目设置中对待删除规则应用配置的引用。
        /// </summary>
        private void CleanupProjectSettingsReference(
            ScriptableObject owner,
            ConfigEditSession targetSession)
        {
            SerializedObject serializedObject =
                new SerializedObject(owner);
            SerializedProperty property =
                serializedObject.FindProperty(
                    "activeRuleApplyConfig");

            if (property == null ||
                !MatchesConfigReference(
                    property.objectReferenceValue,
                    targetSession))
            {
                return;
            }

            Undo.RecordObject(
                owner,
                "清理已删除配置引用");
            property.objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 删除白名单中引用指定规则问题项的完整条目。
        /// </summary>
        private void CleanupWhiteListIssueReferences(
            ScriptableObject owner,
            ConfigEditSession ruleSession,
            string issueInfoId)
        {
            SerializedObject serializedObject =
                new SerializedObject(owner);
            SerializedProperty whiteListItems =
                serializedObject.FindProperty("whiteListItems");

            if (whiteListItems == null ||
                !whiteListItems.isArray)
            {
                return;
            }

            bool changed = false;

            for (int i = whiteListItems.arraySize - 1;
                 i >= 0;
                 i--)
            {
                SerializedProperty item =
                    whiteListItems.GetArrayElementAtIndex(i);
                SerializedProperty ruleProperty =
                    item.FindPropertyRelative("ruleConfig");
                SerializedProperty issueProperty =
                    item.FindPropertyRelative("issueInfoId");

                if (ruleProperty != null &&
                    issueProperty != null &&
                    MatchesConfigReference(
                        ruleProperty.objectReferenceValue,
                        ruleSession) &&
                    string.Equals(
                        issueProperty.stringValue,
                        issueInfoId,
                        StringComparison.Ordinal))
                {
                    DeleteArrayElement(whiteListItems, i);
                    changed = true;
                }
            }

            if (changed)
            {
                Undo.RecordObject(
                    owner,
                    "清理已删除问题项引用");
                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// 从对象数组中删除引用目标配置的完整元素。
        /// </summary>
        private bool RemoveArrayItemsReferencing(
            SerializedProperty arrayProperty,
            string referencePropertyName,
            ConfigEditSession targetSession)
        {
            if (arrayProperty == null ||
                !arrayProperty.isArray)
            {
                return false;
            }

            bool changed = false;

            for (int i = arrayProperty.arraySize - 1;
                 i >= 0;
                 i--)
            {
                SerializedProperty item =
                    arrayProperty.GetArrayElementAtIndex(i);
                SerializedProperty reference =
                    item.FindPropertyRelative(
                        referencePropertyName);

                if (reference != null &&
                    MatchesConfigReference(
                        reference.objectReferenceValue,
                        targetSession))
                {
                    DeleteArrayElement(arrayProperty, i);
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// 从规则应用项的嵌套引用数组中移除目标配置。
        /// </summary>
        private bool RemoveNestedReferences(
            SerializedProperty ownerArray,
            string nestedPropertyName,
            ConfigEditSession targetSession)
        {
            if (ownerArray == null || !ownerArray.isArray)
            {
                return false;
            }

            bool changed = false;

            for (int i = 0; i < ownerArray.arraySize; i++)
            {
                SerializedProperty item =
                    ownerArray.GetArrayElementAtIndex(i);
                SerializedProperty nestedArray =
                    item.FindPropertyRelative(
                        nestedPropertyName);

                if (nestedArray == null || !nestedArray.isArray)
                {
                    continue;
                }

                for (int j = nestedArray.arraySize - 1;
                     j >= 0;
                     j--)
                {
                    SerializedProperty reference =
                        nestedArray.GetArrayElementAtIndex(j);

                    if (MatchesConfigReference(
                            reference.objectReferenceValue,
                            targetSession))
                    {
                        DeleteArrayElement(nestedArray, j);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        /// <summary>
        /// 判断对象引用是否指向指定编辑会话的真实配置或工作副本。
        /// </summary>
        private bool MatchesConfigReference(
            UnityEngine.Object reference,
            ConfigEditSession targetSession)
        {
            return reference != null &&
                   (reference == targetSession.Source ||
                    reference == targetSession.WorkingCopy);
        }

        /// <summary>
        /// 删除数组元素，并兼容 Unity 对对象引用数组的两阶段删除行为。
        /// </summary>
        private void DeleteArrayElement(
            SerializedProperty arrayProperty,
            int index)
        {
            int oldSize = arrayProperty.arraySize;
            arrayProperty.DeleteArrayElementAtIndex(index);

            if (arrayProperty.arraySize == oldSize &&
                index < arrayProperty.arraySize)
            {
                arrayProperty.DeleteArrayElementAtIndex(index);
            }
        }

        /// <summary>
        /// 取消与待删除规则关联的尚未保存脚本创建操作。
        /// </summary>
        private void RemovePendingCheckerScripts(
            ConfigEditSession targetSession)
        {
            if (!(targetSession.WorkingCopy is ScoreRuleConfig))
            {
                return;
            }

            for (int i = pendingCheckerScripts.Count - 1;
                 i >= 0;
                 i--)
            {
                if (MatchesConfigReference(
                        pendingCheckerScripts[i].RuleConfig,
                        targetSession))
                {
                    pendingCheckerScripts.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 将 Unity 资源路径转换为当前项目中的绝对文件路径。
        /// </summary>
        private string ToAbsoluteProjectPath(
            string assetPath)
        {
            string projectRoot =
                Directory.GetParent(
                    Application.dataPath).FullName;

            return Path.GetFullPath(
                Path.Combine(projectRoot, assetPath));
        }

        /// <summary>
        /// 修改工作副本中的字符串字段。
        /// </summary>
        public void SetStringProperty(
            ScriptableObject owner,
            string propertyPath,
            string value)
        {
            Undo.RecordObject(owner, "修改配置文本");

            SerializedObject serializedObject =
                new SerializedObject(owner);

            SerializedProperty property =
                serializedObject.FindProperty(propertyPath);

            if (property == null)
            {
                return;
            }

            property.stringValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        private string SanitizeCheckerClassName(string className)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < className.Length; i++)
            {
                char character = className[i];

                if (char.IsLetterOrDigit(character) ||
                    character == '_')
                {
                    builder.Append(character);
                }
            }

            if (builder.Length == 0)
            {
                builder.Append("NewRuleChecker");
            }

            if (char.IsDigit(builder[0]))
            {
                builder.Insert(0, '_');
            }

            return builder.ToString();
        }

        private string BuildCheckerScriptContent(
            string className,
            string ruleId)
        {
            string escapedRuleId =
                string.IsNullOrEmpty(ruleId)
                    ? string.Empty
                    : ruleId
                        .Replace("\\", "\\\\")
                        .Replace("\"", "\\\"");

            return
                "namespace majsoul.tools.assetscoretool.editor\n" +
                "{\n" +
                "    public class " + className +
                " : IScoreRuleChecker\n" +
                "    {\n" +
                "        public string RuleId\n" +
                "        {\n" +
                "            get { return \"" +
                escapedRuleId + "\"; }\n" +
                "        }\n\n" +
                "        public bool ShouldCheck(" +
                "AssetContext context)\n" +
                "        {\n" +
                "            return true;\n" +
                "        }\n\n" +
                "        public AssetIssueResult Check(\n" +
                "            AssetContext context,\n" +
                "            ScoreRuleConfig ruleConfig)\n" +
                "        {\n" +
                "            return null;\n" +
                "        }\n" +
                "    }\n" +
                "}\n";
        }

        private void DestroyWorkingCopy(
            ScriptableObject workingCopy)
        {
            if (workingCopy != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    workingCopy);
            }
        }
    }
}
