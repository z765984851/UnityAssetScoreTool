using System.Collections.Generic;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 一次扫描产生的原始数据。
    /// 
    /// 注意：
    /// 这里不保存分数、等级、最高严重度、汇总问题数。
    /// Scanner 只负责产出这些原始扫描数据；
    /// 后续评分、汇总、展示根节点筛选交给 ReportBuilder 处理。
    /// </summary>
    public class AssetScanData
    {
        /// <summary>
        /// 本次扫描的入口资源。
        /// 如果扫描的是文件夹，这里会包含文件夹下的所有入口资源。
        /// </summary>
        public readonly List<AssetContext> RootContexts = new List<AssetContext>();

        /// <summary>
        /// 本次扫描涉及到的所有资源上下文。
        /// Key 是 Unity 资源路径，例如 Assets/Art/Hero.prefab。
        /// </summary>
        public readonly Dictionary<string, AssetContext> ContextsByPath =
            new Dictionary<string, AssetContext>();

        /// <summary>
        /// 每个资源自身检测出的问题。
        /// 这里只保存资源自身问题，不包含依赖资源的问题。
        /// </summary>
        public readonly Dictionary<string, List<AssetIssueResult>> IssuesByPath =
            new Dictionary<string, List<AssetIssueResult>>();

        /// <summary>
        /// 清空扫描数据。
        /// 后续 Scanner 每次 StartScan 开始时调用。
        /// </summary>
        public void Clear()
        {
            RootContexts.Clear();
            ContextsByPath.Clear();
            IssuesByPath.Clear();
        }

        /// <summary>
        /// 添加入口资源。
        /// 入口资源同时也会被加入 ContextsByPath。
        /// </summary>
        public void AddRootContext(AssetContext context)
        {
            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return;
            }

            string assetPath = ScanToolUtility.NormalizePath(context.AssetPath);

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            AddContext(context);

            for (int i = 0; i < RootContexts.Count; i++)
            {
                AssetContext rootContext = RootContexts[i];

                if (rootContext == null)
                {
                    continue;
                }

                string rootPath = ScanToolUtility.NormalizePath(rootContext.AssetPath);

                if (rootPath == assetPath)
                {
                    return;
                }
            }

            RootContexts.Add(context);
        }

        /// <summary>
        /// 添加资源上下文。
        /// 同一个资源路径只保留一份上下文。
        /// </summary>
        public void AddContext(AssetContext context)
        {
            if (context == null || string.IsNullOrEmpty(context.AssetPath))
            {
                return;
            }

            string assetPath = ScanToolUtility.NormalizePath(context.AssetPath);

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            if (!ContextsByPath.ContainsKey(assetPath))
            {
                ContextsByPath.Add(assetPath, context);
            }
        }

        /// <summary>
        /// 保存某个资源自身的问题列表。
        /// </summary>
        public void SetIssues(string assetPath, List<AssetIssueResult> issues)
        {
            assetPath = ScanToolUtility.NormalizePath(assetPath);

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            IssuesByPath[assetPath] = issues ?? new List<AssetIssueResult>();
        }

        /// <summary>
        /// 尝试根据资源路径获取资源上下文。
        /// </summary>
        public bool TryGetContext(string assetPath, out AssetContext context)
        {
            context = null;

            assetPath = ScanToolUtility.NormalizePath(assetPath);

            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            return ContextsByPath.TryGetValue(assetPath, out context);
        }

        /// <summary>
        /// 获取某个资源自身的问题列表。
        /// 如果没有问题，返回空列表。
        /// </summary>
        public List<AssetIssueResult> GetIssues(string assetPath)
        {
            assetPath = ScanToolUtility.NormalizePath(assetPath);

            if (string.IsNullOrEmpty(assetPath))
            {
                return new List<AssetIssueResult>();
            }

            List<AssetIssueResult> issues;
            if (IssuesByPath.TryGetValue(assetPath, out issues))
            {
                return issues;
            }

            return new List<AssetIssueResult>();
        }
    }
}
