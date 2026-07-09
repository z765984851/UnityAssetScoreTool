using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace majsoul.tools.assetscoretool.editor
{
    public enum EScanStage
    {
        Idle,
        BuildDependencies,
        CheckRules,
        Canceling,
        Finished
    }

    public class AssetScoreToolScanner
    {
        /// <summary>
        /// 每帧最多推进的资源扫描步骤数。
        /// </summary>
        private const int ScanStepsPerFrame = 10;

        /// <summary>
        /// 规则检查器字典，Key 是 RuleId。
        /// </summary>
        private Dictionary<string, IScoreRuleChecker> ruleCheckersDic;

        /// <summary>
        /// 资源类型配置字典，Key 是 AssetTypeId。
        /// </summary>
        private Dictionary<string, AssetTypeConfig> assetTypeConfigsDic;

        /// <summary>
        /// 资源类型检查器字典，Key 是 AssetTypeId。
        /// </summary>
        private Dictionary<string, IAssetTypeChecker> assetTypeCheckersDic;

        /// <summary>
        /// 所有 Profile 配置，Key 是 ProfileId。
        /// </summary>
        private Dictionary<string, ScoreRuleProfileConfig> profileConfigsDic;

        /// <summary>
        /// 当前生效的规则应用配置。
        /// </summary>
        private RuleApplyConfig ruleApplyConfig;

        /// <summary>
        /// 当前生效配置中已启用的规则应用项，Key 是 RuleId。
        /// </summary>
        private Dictionary<string, RuleApplyItem> ruleApplyConfigsDic;

        /// <summary>
        /// 是否已经加载过配置。
        /// </summary>
        private bool isLoad = false;

        #region 扫描流程使用的字段

        /// <summary>
        /// 当前扫描使用的 Profile。
        /// </summary>
        private ScoreRuleProfileConfig profile;

        /// <summary>
        /// 当前扫描产生的原始数据。
        /// Scanner 只写入入口资源、资源上下文和资源自身问题。
        /// </summary>
        private AssetScanData scanData;

        /// <summary>
        /// 当前扫描阶段。
        /// </summary>
        private EScanStage scanStage = EScanStage.Idle;

        /// <summary>
        /// 等待构建直接依赖的资源路径队列。
        /// 每一步只处理一个资源路径。
        /// </summary>
        private Queue<string> pendingDependencyPaths = new Queue<string>();

        /// <summary>
        /// 已经进入过依赖构建队列的资源路径。
        /// 用于避免同一个资源重复入队。
        /// </summary>
        private HashSet<string> queuedDependencyPathSet = new HashSet<string>();

        /// <summary>
        /// 已经完成直接依赖构建的资源路径。
        /// </summary>
        private HashSet<string> builtDependencyPathSet = new HashSet<string>();

        /// <summary>
        /// 本次扫描中所有资源路径对应的 AssetContext。
        /// 保证同一个资源路径只对应一个 AssetContext 实例。
        /// </summary>
        private Dictionary<string, AssetContext> contextMap =
            new Dictionary<string, AssetContext>();

        /// <summary>
        /// 依赖构建完成后，等待执行规则检测的资源列表。
        /// 每一步检测一个资源的全部规则。
        /// </summary>
        private List<AssetContext> ruleCheckContexts = new List<AssetContext>();

        /// <summary>
        /// 当前规则检测进度。
        /// </summary>
        private int ruleCheckIndex = 0;

        /// <summary>
        /// 当前扫描的取消令牌。
        /// Scanner 自己持有取消状态，UI 只负责调用 CancelScan。
        /// </summary>
        private CancellationTokenSource scanCancellationTokenSource;

        #endregion

        public AssetScoreToolScanner()
        {
            scanData = new AssetScanData();
        }

        public void LoadData()
        {
            isLoad = true;

            ruleApplyConfig =
                RuleApplyConfigResolver.GetActiveConfig();

            ruleApplyConfigsDic =
                ScanToolUtility.GetEnabledRuleApplyItems(
                    ruleApplyConfig);

            ruleCheckersDic =
                ScanToolUtility.GetAllRuleCheckers();

            assetTypeConfigsDic =
                ScanToolUtility.GetReferencedAssetTypeConfigs(
                    ruleApplyConfigsDic.Values);

            assetTypeCheckersDic =
                ScanToolUtility.GetAllAssetTypeCheckers();

            profileConfigsDic =
                ScanToolUtility.GetReferencedProfileConfigs(
                    ruleApplyConfigsDic.Values);
        }

        /// <summary>
        /// 开始扫描。
        /// 
        /// 这个方法不会在同一帧内完成全部扫描，
        /// 而是通过 await Task.Yield 主线程分帧推进。
        /// 
        /// 注意：
        /// 这里没有使用 Task.Run，
        /// 所以 AssetDatabase 等 Unity Editor API 仍然在主线程执行。
        /// </summary>
        public async Task<AssetScanData> StartScanAsync(
            string path,
            ScoreRuleProfileConfig profileConfig,
            Action<string, int, int> onProgress = null,
            bool yieldBetweenSteps = true)
        {
            DisposeScanCancellationTokenSource();
            scanCancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = scanCancellationTokenSource.Token;

            try
            {
                if (!isLoad)
                {
                    LoadData();
                }

                scanData.Clear();
                profile = profileConfig;

                pendingDependencyPaths.Clear();
                queuedDependencyPathSet.Clear();
                builtDependencyPathSet.Clear();
                contextMap.Clear();
                ruleCheckContexts.Clear();
                ruleCheckIndex = 0;

                scanStage = EScanStage.Idle;

                List<string> rootAssetPaths = GetScanAssetPaths(path);

                for (int i = 0; i < rootAssetPaths.Count; i++)
                {
                    string rootPath = ScanToolUtility.NormalizePath(rootAssetPaths[i]);

                    if (string.IsNullOrEmpty(rootPath))
                    {
                        continue;
                    }

                    AssetContext rootContext = GetOrCreateContext(rootPath);

                    if (rootContext == null)
                    {
                        continue;
                    }

                    scanData.AddRootContext(rootContext);
                    EnqueueDependencyPath(rootPath);
                }

                scanStage = pendingDependencyPaths.Count > 0
                    ? EScanStage.BuildDependencies
                    : EScanStage.Finished;

                onProgress?.Invoke(ScanStageText, CurrentStepCount, TotalStepCount);

                while (IsRunning)
                {
                    for (int i = 0; i < ScanStepsPerFrame && IsRunning; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (IsScanning)
                        {
                            ScanNext();
                            onProgress?.Invoke(ScanStageText, CurrentStepCount, TotalStepCount);
                        }
                    }

                    if (yieldBetweenSteps)
                    {
                        await Task.Yield();
                    }
                }

                return scanData;
            }
            catch (OperationCanceledException)
            {
                scanStage = EScanStage.Idle;
                throw;
            }
            catch
            {
                scanStage = EScanStage.Idle;
                throw;
            }
            finally
            {
                DisposeScanCancellationTokenSource();
            }
        }

        /// <summary>
        /// 推进一次扫描。
        /// 
        /// 依赖阶段：一次构建一个资源的直接依赖。
        /// 规则阶段：一次检测一个资源的全部规则。
        /// 
        /// 返回 true 表示后续还有扫描工作；
        /// 返回 false 表示扫描已经完成。
        /// </summary>
        private bool ScanNext()
        {
            if (scanStage == EScanStage.BuildDependencies)
            {
                if (pendingDependencyPaths.Count > 0)
                {
                    BuildDependencyNext();
                }

                if (pendingDependencyPaths.Count == 0)
                {
                    PrepareRuleCheck();
                }

                return IsScanning;
            }

            if (scanStage == EScanStage.CheckRules)
            {
                if (ruleCheckIndex < ruleCheckContexts.Count)
                {
                    CheckRuleNext();
                }

                if (ruleCheckIndex >= ruleCheckContexts.Count)
                {
                    scanStage = EScanStage.Finished;
                }

                return IsScanning;
            }

            return false;
        }

        /// <summary>
        /// 构建一个资源的直接依赖。
        /// 
        /// 这里只获取当前资源的一层直接依赖。
        /// 间接依赖会继续加入 pendingDependencyPaths，后续分步处理。
        /// </summary>
        private void BuildDependencyNext()
        {
            string assetPath = pendingDependencyPaths.Dequeue();
            assetPath = ScanToolUtility.NormalizePath(assetPath);

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            if (builtDependencyPathSet.Contains(assetPath))
            {
                return;
            }

            AssetContext context = GetOrCreateContext(assetPath);

            if (context == null)
            {
                return;
            }

            scanData.AddContext(context);

            // false 表示只获取直接依赖。
            // 间接依赖通过队列继续展开，避免递归构建整棵依赖树。
            string[] dependencyPaths = AssetDatabase.GetDependencies(assetPath, false);

            for (int i = 0; i < dependencyPaths.Length; i++)
            {
                string dependencyPath = ScanToolUtility.NormalizePath(dependencyPaths[i]);

                if (!IsValidDependencyPath(assetPath, dependencyPath))
                {
                    continue;
                }

                AssetContext dependencyContext = GetOrCreateContext(dependencyPath);

                if (dependencyContext == null)
                {
                    continue;
                }

                // 避免依赖图形成环，防止后续 UI 和 ReportBuilder 递归时出现异常。
                if (WouldCreateDependencyCycle(context, dependencyContext))
                {
                    continue;
                }

                context.AddDependency(dependencyContext);

                // 依赖资源进入队列，后续再构建它自己的直接依赖。
                EnqueueDependencyPath(dependencyPath);
            }

            builtDependencyPathSet.Add(assetPath);
        }

        /// <summary>
        /// 依赖构建完成后，准备进入规则检测阶段。
        /// </summary>
        private void PrepareRuleCheck()
        {
            ruleCheckContexts.Clear();

            foreach (AssetContext context in contextMap.Values)
            {
                if (context == null || string.IsNullOrEmpty(context.AssetPath))
                {
                    continue;
                }

                ruleCheckContexts.Add(context);
            }

            ruleCheckIndex = 0;

            scanStage = ruleCheckContexts.Count > 0
                ? EScanStage.CheckRules
                : EScanStage.Finished;
        }

        /// <summary>
        /// 检测一个资源的全部规则。
        /// 
        /// 注意：
        /// 这里的“一步”是一个资源，不是一条规则。
        /// </summary>
        private void CheckRuleNext()
        {
            AssetContext context = ruleCheckContexts[ruleCheckIndex];
            ruleCheckIndex++;

            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return;
            }

            string assetPath = ScanToolUtility.NormalizePath(context.AssetPath);

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            List<AssetIssueResult> issues = CheckRules(context);
            scanData.SetIssues(assetPath, issues);
        }

        /// <summary>
        /// 把资源路径加入依赖构建队列。
        /// 同一个资源路径只会入队一次。
        /// </summary>
        private void EnqueueDependencyPath(string assetPath)
        {
            assetPath = ScanToolUtility.NormalizePath(assetPath);

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            if (queuedDependencyPathSet.Contains(assetPath))
            {
                return;
            }

            if (builtDependencyPathSet.Contains(assetPath))
            {
                return;
            }

            queuedDependencyPathSet.Add(assetPath);
            pendingDependencyPaths.Enqueue(assetPath);
        }

        /// <summary>
        /// 对一个资源执行全部适用规则。
        /// </summary>
        private List<AssetIssueResult> CheckRules(AssetContext context)
        {
            List<AssetIssueResult> results = new List<AssetIssueResult>();

            if (context == null)
            {
                return results;
            }

            if (ruleApplyConfigsDic == null || ruleCheckersDic == null)
            {
                return results;
            }

            foreach (RuleApplyItem ruleApplyItem in ruleApplyConfigsDic.Values)
            {
                if (ruleApplyItem == null)
                {
                    continue;
                }

                if (!IsProfileMatched(ruleApplyItem))
                {
                    continue;
                }

                ScoreRuleConfig ruleConfig = ruleApplyItem.RuleConfig;

                if (ruleConfig == null)
                {
                    continue;
                }

                string ruleId = ruleConfig.RuleId;

                if (string.IsNullOrEmpty(ruleId))
                {
                    continue;
                }

                if (!IsAssetTypeMatched(context, ruleApplyItem))
                {
                    continue;
                }

                IScoreRuleChecker checker;
                if (!ruleCheckersDic.TryGetValue(ruleId, out checker))
                {
                    continue;
                }

                if (checker == null)
                {
                    continue;
                }

                if (!checker.ShouldCheck(context))
                {
                    continue;
                }

                AssetIssueResult issueResult = checker.Check(
                    context,
                    ruleConfig);

                if (issueResult == null || issueResult.IssueInfo == null)
                {
                    continue;
                }

                issueResult.Context = context;
                issueResult.RuleConfig = ruleConfig;

                results.Add(issueResult);
            }

            return results;
        }

        /// <summary>
        /// 获取已有的 AssetContext，或者创建新的 AssetContext。
        /// 
        /// contextMap 是本次扫描级别的缓存，
        /// 保证同一个资源路径只对应同一个 AssetContext 实例。
        /// </summary>
        private AssetContext GetOrCreateContext(string assetPath)
        {
            assetPath = ScanToolUtility.NormalizePath(assetPath);

            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            AssetContext context;
            if (contextMap.TryGetValue(assetPath, out context))
            {
                return context;
            }

            context = new AssetContext(assetPath);

            // 创建 Context 时立即解析资源类型。
            // 后续 CheckRules 会根据 AssetTypes 判断规则是否适用。
            context.SetAssetTypes(GetAssetTypes(assetPath));

            contextMap.Add(assetPath, context);

            return context;
        }

        /// <summary>
        /// 判断新增 ownerContext -> dependencyContext 这条依赖关系是否会形成环。
        /// </summary>
        private bool WouldCreateDependencyCycle(
            AssetContext ownerContext,
            AssetContext dependencyContext)
        {
            if (ownerContext == null || dependencyContext == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(ownerContext.AssetPath))
            {
                return false;
            }

            return HasDependencyPath(
                dependencyContext,
                ownerContext.AssetPath,
                new HashSet<string>());
        }

        /// <summary>
        /// 判断 context 的依赖链中是否已经包含 targetAssetPath。
        /// </summary>
        private bool HasDependencyPath(
            AssetContext context,
            string targetAssetPath,
            HashSet<string> visitedPaths)
        {
            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return false;
            }

            string assetPath = ScanToolUtility.NormalizePath(context.AssetPath);
            targetAssetPath = ScanToolUtility.NormalizePath(targetAssetPath);

            if (assetPath == targetAssetPath)
            {
                return true;
            }

            if (visitedPaths.Contains(assetPath))
            {
                return false;
            }

            visitedPaths.Add(assetPath);

            if (context.Dependencies == null)
            {
                return false;
            }

            for (int i = 0; i < context.Dependencies.Count; i++)
            {
                if (HasDependencyPath(
                        context.Dependencies[i],
                        targetAssetPath,
                        visitedPaths))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取资源对应的资源类型。
        /// </summary>
        private List<AssetTypeConfig> GetAssetTypes(string assetPath)
        {
            List<AssetTypeConfig> types = new List<AssetTypeConfig>();

            if (assetTypeConfigsDic == null || assetTypeCheckersDic == null)
            {
                return types;
            }

            foreach (AssetTypeConfig assetType in assetTypeConfigsDic.Values)
            {
                if (assetType == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(assetType.AssetTypeId))
                {
                    continue;
                }

                IAssetTypeChecker checker;
                if (!assetTypeCheckersDic.TryGetValue(assetType.AssetTypeId, out checker))
                {
                    continue;
                }

                if (checker == null)
                {
                    continue;
                }

                if (checker.IsMatch(assetPath))
                {
                    types.Add(assetType);
                }
            }

            return types;
        }

        /// <summary>
        /// 根据传入路径获取本次扫描的入口资源列表。
        /// 
        /// 如果传入的是文件夹，会递归获取文件夹下所有资源。
        /// 如果传入的是单个资源，会直接返回该资源。
        /// </summary>
        private List<string> GetScanAssetPaths(string path)
        {
            List<string> result = new List<string>();

            if (string.IsNullOrEmpty(path))
            {
                return result;
            }

            HashSet<string> pathSet = new HashSet<string>();

            path = ScanToolUtility.NormalizePath(path);

            // 传入的是文件夹：扫描文件夹下所有资源。
            if (AssetDatabase.IsValidFolder(path))
            {
                string[] guids = AssetDatabase.FindAssets(string.Empty, new string[] { path });

                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    assetPath = ScanToolUtility.NormalizePath(assetPath);

                    if (!IsValidScanAssetPath(assetPath))
                    {
                        continue;
                    }

                    if (pathSet.Contains(assetPath))
                    {
                        continue;
                    }

                    pathSet.Add(assetPath);
                    result.Add(assetPath);
                }

                return result;
            }

            // 传入的是单个资源：直接加入扫描列表。
            if (IsValidScanAssetPath(path))
            {
                pathSet.Add(path);
                result.Add(path);
            }

            return result;
        }

        /// <summary>
        /// 判断一个资源路径是否可以作为扫描入口。
        /// </summary>
        private bool IsValidScanAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            assetPath = ScanToolUtility.NormalizePath(assetPath);

            return IsScorableAssetPath(assetPath);
        }

        /// <summary>
        /// 判断一个依赖路径是否需要加入依赖结构。
        /// </summary>
        private bool IsValidDependencyPath(string ownerAssetPath, string dependencyPath)
        {
            ownerAssetPath = ScanToolUtility.NormalizePath(ownerAssetPath);
            dependencyPath = ScanToolUtility.NormalizePath(dependencyPath);

            if (string.IsNullOrEmpty(dependencyPath))
            {
                return false;
            }

            // Unity 的依赖查询有时会返回自己，必须过滤。
            if (dependencyPath == ownerAssetPath)
            {
                return false;
            }

            return IsScorableAssetPath(dependencyPath);
        }

        /// <summary>
        /// 判断路径是否属于当前项目且能匹配本次扫描生效资源类型。
        /// 依赖图只保留可参与评分的项目资源，避免脚本和 Package 内容进入结果树。
        /// </summary>
        private bool IsScorableAssetPath(string assetPath)
        {
            assetPath = ScanToolUtility.NormalizePath(assetPath);

            if (string.IsNullOrEmpty(assetPath) ||
                !assetPath.StartsWith("Assets/"))
            {
                return false;
            }

            // 文件夹不作为检测资源。
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return false;
            }

            return GetAssetTypes(assetPath).Count > 0;
        }

        /// <summary>
        /// 判断当前资源类型是否满足规则应用配置。
        /// </summary>
        private bool IsAssetTypeMatched(AssetContext context, RuleApplyItem ruleApplyItem)
        {
            if (context == null || ruleApplyItem == null)
            {
                return false;
            }

            if (context.AssetTypes == null || context.AssetTypes.Count == 0)
            {
                return false;
            }

            AssetTypeConfig[] ruleAssetTypes = ruleApplyItem.AssetTypeConfigs;

            if (ruleAssetTypes == null || ruleAssetTypes.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < context.AssetTypes.Count; i++)
            {
                AssetTypeConfig contextAssetType = context.AssetTypes[i];

                if (contextAssetType == null)
                {
                    continue;
                }

                string contextAssetTypeId = contextAssetType.AssetTypeId;

                if (string.IsNullOrEmpty(contextAssetTypeId))
                {
                    continue;
                }

                for (int j = 0; j < ruleAssetTypes.Length; j++)
                {
                    AssetTypeConfig ruleAssetType = ruleAssetTypes[j];

                    if (ruleAssetType == null)
                    {
                        continue;
                    }

                    if (contextAssetTypeId == ruleAssetType.AssetTypeId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 判断当前扫描 Profile 是否满足规则应用配置。
        /// 
        /// 当前规则：
        /// 1. 扫描时必须传入 profile。
        /// 2. RuleApplyItem 必须配置 Profiles。
        /// 3. 当前 profile.ProfileId 命中 RuleApplyItem.Profiles 中任意一个，才允许执行。
        /// </summary>
        private bool IsProfileMatched(RuleApplyItem ruleApplyItem)
        {
            if (ruleApplyItem == null)
            {
                return false;
            }

            if (profile == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(profile.ProfileId))
            {
                return false;
            }

            ScoreRuleProfileConfig[] profiles = ruleApplyItem.Profiles;

            if (profiles == null || profiles.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < profiles.Length; i++)
            {
                ScoreRuleProfileConfig itemProfile = profiles[i];

                if (itemProfile == null)
                {
                    continue;
                }

                if (profile.ProfileId == itemProfile.ProfileId)
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// 当前扫描是否已经完成。
        /// </summary>
        public bool IsFinished
        {
            get { return scanStage == EScanStage.Finished; }
        }

        /// <summary>
        /// 当前扫描阶段显示文本。
        /// </summary>
        public string ScanStageText
        {
            get
            {
                switch (scanStage)
                {
                    case EScanStage.BuildDependencies:
                        return "构建依赖";
                    case EScanStage.CheckRules:
                        return "检测规则";
                    case EScanStage.Finished:
                        return "完成";
                    case EScanStage.Canceling:
                        return "取消中";
                    default:
                        return "空闲";
                }
            }
        }

        /// <summary>
        /// 当前阶段已完成数量。
        /// </summary>
        public int CurrentStepCount
        {
            get
            {
                if (scanStage == EScanStage.BuildDependencies)
                {
                    return builtDependencyPathSet.Count;
                }

                if (scanStage == EScanStage.CheckRules)
                {
                    return ruleCheckIndex;
                }

                return 0;
            }
        }

        /// <summary>
        /// 当前阶段总数量。
        /// 依赖构建阶段会随着新依赖发现而动态增长。
        /// </summary>
        public int TotalStepCount
        {
            get
            {
                if (scanStage == EScanStage.BuildDependencies)
                {
                    return queuedDependencyPathSet.Count;
                }

                if (scanStage == EScanStage.CheckRules)
                {
                    return ruleCheckContexts.Count;
                }

                return 0;
            }
        }

        /// <summary>
        /// 获取当前扫描产生的原始数据。
        /// 后续评分、等级、严重度和展示结构都应该基于这份数据在 Scanner 外部生成。
        /// </summary>
        public AssetScanData GetScanData()
        {
            return scanData;
        }

        /// <summary>
        /// 获取当前 Scanner 已加载的规则应用配置。
        /// 报告白名单必须使用与扫描规则相同的配置。
        /// </summary>
        public RuleApplyConfig GetRuleApplyConfig()
        {
            if (!isLoad)
            {
                LoadData();
            }

            return ruleApplyConfig;
        }

        /// <summary>
        /// 获取当前已加载的 Profile 列表。
        /// 如果还未加载配置，会先执行 LoadData。
        /// </summary>
        public List<ScoreRuleProfileConfig> GetProfiles()
        {
            if (!isLoad)
            {
                LoadData();
            }

            if (profileConfigsDic == null)
            {
                return new List<ScoreRuleProfileConfig>();
            }

            return new List<ScoreRuleProfileConfig>(profileConfigsDic.Values);
        }

        /// <summary>
        /// 请求取消当前扫描。
        /// 真正停止会在下一次分帧推进前响应 CancellationToken。
        /// </summary>
        public void CancelScan()
        {
            if (!IsScanning)
            {
                return;
            }

            scanStage = EScanStage.Canceling;

            if (scanCancellationTokenSource != null)
            {
                scanCancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        /// 当前是否处于扫描生命周期中。
        /// 包含正在扫描和取消中。
        /// </summary>
        public bool IsRunning
        {
            get { return IsScanning || scanStage == EScanStage.Canceling; }
        }


        /// <summary>
        /// 当前是否正在扫描。
        /// </summary>
        public bool IsScanning
        {
            get
            {
                return scanStage == EScanStage.BuildDependencies ||
                       scanStage == EScanStage.CheckRules;
            }
        }

        /// <summary>
        /// 当前扫描阶段。
        /// </summary>
        public EScanStage CurrentStage
        {
            get { return scanStage; }
        }

        private void DisposeScanCancellationTokenSource()
        {
            if (scanCancellationTokenSource == null)
            {
                return;
            }

            scanCancellationTokenSource.Dispose();
            scanCancellationTokenSource = null;
        }
    }
}
