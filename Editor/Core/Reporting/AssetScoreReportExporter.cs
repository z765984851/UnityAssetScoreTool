using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 将最终评分报告导出为可供外部工具读取的 JSON 和 Excel 文件。
    /// </summary>
    internal static class AssetScoreReportExporter
    {
        private const string CsvSummaryHeader =
            "根资源路径,汇总分数,汇总等级,最高严重度,自身问题数," +
            "汇总问题数,Blocker问题数,Error问题数,Warning问题数," +
            "Info问题数,关联资源数";

        private const string CsvAssetsHeader =
            "所属根资源,资源名称,资源路径,资源类型,是否入口资源," +
            "直接父资源,直接依赖资源,自身分数,自身等级,自身最高严重度," +
            "自身问题数,汇总分数,汇总等级,汇总最高严重度,汇总问题数";

        private const string CsvIssuesHeader =
            "所属根资源,问题资源路径,资源类型,问题ID,问题名称," +
            "问题严重度,扣分,规则ID,规则名称,具体说明,对象定位";

        private const string CsvDependenciesHeader =
            "所属根资源,父资源路径,子资源路径,父资源类型,子资源类型," +
            "子资源汇总分数,子资源汇总等级,子资源汇总问题数";

        /// <summary>
        /// 将报告导出为包含完整资源关系和问题明细的 JSON 文件。
        /// </summary>
        public static string ExportJson(
            AssetScoreReport report,
            string filePath)
        {
            ValidateExportArguments(report, filePath);

            ExportFileData exportData = BuildExportFileData(report);
            string json = JsonUtility.ToJson(exportData, true);

            WriteTextFile(filePath, json);
            return filePath;
        }

        /// <summary>
        /// 将报告导出为包含扫描摘要、资源总览、问题明细和依赖关系的 Excel 文件。
        /// </summary>
        public static string ExportExcel(
            AssetScoreReport report,
            string filePath)
        {
            ValidateExportArguments(report, filePath);

            ExportFileData exportData = BuildExportFileData(report);
            Dictionary<string, List<string>> rootPathsByAsset =
                BuildRootPathsByAsset(report);
            string excelFilePath = Path.ChangeExtension(filePath, "xlsx");
            List<ExcelSheetData> sheets = new List<ExcelSheetData>
            {
                new ExcelSheetData(
                    "扫描摘要",
                    BuildCsvSummary(report, exportData, rootPathsByAsset)),
                new ExcelSheetData(
                    "资源总览",
                    BuildCsvAssets(exportData, rootPathsByAsset)),
                new ExcelSheetData(
                    "问题明细",
                    BuildCsvIssues(exportData, rootPathsByAsset)),
                new ExcelSheetData(
                    "依赖关系",
                    BuildCsvDependencies(report, rootPathsByAsset))
            };

            WriteExcelFile(excelFilePath, sheets);
            return excelFilePath;
        }

        /// <summary>
        /// 校验导出所需的报告和目标文件路径。
        /// </summary>
        private static void ValidateExportArguments(
            AssetScoreReport report,
            string filePath)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    "导出文件路径不能为空。",
                    nameof(filePath));
            }
        }

        /// <summary>
        /// 依据报告构建共享的导出数据，保证 JSON 与 CSV 字段含义一致。
        /// </summary>
        private static ExportFileData BuildExportFileData(
            AssetScoreReport report)
        {
            ExportFileData exportData = new ExportFileData();
            exportData.schemaVersion = 1;
            exportData.exportedAtUtc = DateTime.UtcNow.ToString(
                "o",
                CultureInfo.InvariantCulture);

            HashSet<string> rootPaths = BuildRootPathSet(
                report.DisplayRootResults,
                exportData.rootAssetPaths);
            Dictionary<string, List<string>> parentPathsByChild =
                BuildParentPaths(report.ResultsByPath);
            List<string> assetPaths = new List<string>(
                report.ResultsByPath.Keys);

            assetPaths.Sort(StringComparer.Ordinal);

            for (int i = 0; i < assetPaths.Count; i++)
            {
                AssetScanResult result =
                    report.ResultsByPath[assetPaths[i]];

                exportData.assets.Add(BuildAssetData(
                    result,
                    rootPaths.Contains(assetPaths[i]),
                    parentPathsByChild));
            }

            return exportData;
        }

        /// <summary>
        /// 收集展示根资源路径，并保持报告中的根节点顺序。
        /// </summary>
        private static HashSet<string> BuildRootPathSet(
            List<AssetScanResult> rootResults,
            List<string> orderedRootPaths)
        {
            HashSet<string> rootPaths = new HashSet<string>();

            if (rootResults == null)
            {
                return rootPaths;
            }

            for (int i = 0; i < rootResults.Count; i++)
            {
                AssetContext context = rootResults[i].Context;
                string assetPath = GetNormalizedAssetPath(context);

                if (string.IsNullOrEmpty(assetPath) ||
                    !rootPaths.Add(assetPath))
                {
                    continue;
                }

                orderedRootPaths.Add(assetPath);
            }

            return rootPaths;
        }

        /// <summary>
        /// 建立每个资源的直接父资源路径列表，以保留依赖树的完整关系。
        /// </summary>
        private static Dictionary<string, List<string>> BuildParentPaths(
            Dictionary<string, AssetScanResult> resultsByPath)
        {
            Dictionary<string, HashSet<string>> parentPathSets =
                new Dictionary<string, HashSet<string>>();

            foreach (KeyValuePair<string, AssetScanResult> pair
                     in resultsByPath)
            {
                string parentPath = GetNormalizedAssetPath(
                    pair.Value.Context);

                if (string.IsNullOrEmpty(parentPath) ||
                    pair.Value.Context.Dependencies == null)
                {
                    continue;
                }

                for (int i = 0;
                     i < pair.Value.Context.Dependencies.Count;
                     i++)
                {
                    string childPath = GetNormalizedAssetPath(
                        pair.Value.Context.Dependencies[i]);

                    if (string.IsNullOrEmpty(childPath))
                    {
                        continue;
                    }

                    HashSet<string> parentPaths;
                    if (!parentPathSets.TryGetValue(
                            childPath,
                            out parentPaths))
                    {
                        parentPaths = new HashSet<string>();
                        parentPathSets.Add(childPath, parentPaths);
                    }

                    parentPaths.Add(parentPath);
                }
            }

            Dictionary<string, List<string>> parentPathsByChild =
                new Dictionary<string, List<string>>();

            foreach (KeyValuePair<string, HashSet<string>> pair
                     in parentPathSets)
            {
                List<string> parentPaths = new List<string>(pair.Value);
                parentPaths.Sort(StringComparer.Ordinal);
                parentPathsByChild.Add(pair.Key, parentPaths);
            }

            return parentPathsByChild;
        }

        /// <summary>
        /// 将单个资源结果及其自身问题转换为导出记录。
        /// </summary>
        private static AssetExportData BuildAssetData(
            AssetScanResult result,
            bool isRoot,
            Dictionary<string, List<string>> parentPathsByChild)
        {
            AssetExportData asset = new AssetExportData();
            AssetContext context = result.Context;
            string assetPath = GetNormalizedAssetPath(context);

            asset.assetPath = assetPath;
            asset.assetName = string.IsNullOrEmpty(assetPath)
                ? string.Empty
                : Path.GetFileName(assetPath);
            asset.isRoot = isRoot;
            asset.assetTypes = GetAssetTypeNames(context);
            asset.parentAssetPaths = GetParentPaths(
                assetPath,
                parentPathsByChild);
            asset.directDependencyPaths = GetDependencyPaths(context);
            asset.selfScore = result.SelfScore;
            asset.selfGrade = result.SelfGrade.ToString();
            asset.selfHighestIssueLevel =
                result.SelfHighestIssueLevel.ToString();
            asset.selfIssueCount = result.SelfIssueCount;
            asset.score = result.Score;
            asset.grade = result.Grade.ToString();
            asset.highestIssueLevel = result.HighestIssueLevel.ToString();
            asset.issueCount = result.IssueCount;

            if (result.Issues != null)
            {
                for (int i = 0; i < result.Issues.Count; i++)
                {
                    asset.issues.Add(BuildIssueData(result.Issues[i]));
                }
            }

            return asset;
        }

        /// <summary>
        /// 转换单条规则问题及其可定位的 Prefab 内部对象信息。
        /// </summary>
        private static IssueExportData BuildIssueData(
            AssetIssueResult issue)
        {
            IssueExportData issueData = new IssueExportData();

            if (issue == null)
            {
                return issueData;
            }

            issueData.ruleId = issue.RuleConfig == null
                ? string.Empty
                : issue.RuleConfig.RuleId;
            issueData.ruleName = issue.RuleConfig == null
                ? string.Empty
                : issue.RuleConfig.DisplayName;

            if (issue.IssueInfo != null)
            {
                issueData.issueId = issue.IssueInfo.issueInfoId;
                issueData.issueName = issue.IssueInfo.displayName;
                issueData.deductScore = issue.IssueInfo.deductScore;
                issueData.issueLevel =
                    issue.IssueInfo.issueLevel.ToString();
                issueData.issueMessage = issue.IssueInfo.issueMessage;
            }

            if (issue.Locations != null)
            {
                for (int i = 0; i < issue.Locations.Count; i++)
                {
                    PrefabIssueLocation location = issue.Locations[i];

                    if (location == null)
                    {
                        continue;
                    }

                    IssueLocationExportData locationData =
                        new IssueLocationExportData();
                    locationData.hierarchyPath = location.HierarchyPath;
                    locationData.description = location.Description;
                    issueData.locations.Add(locationData);
                }
            }

            return issueData;
        }

        /// <summary>
        /// 获取资源配置中可展示的资源类型名称，并按名称稳定排序。
        /// </summary>
        private static List<string> GetAssetTypeNames(
            AssetContext context)
        {
            List<string> typeNames = new List<string>();

            if (context == null || context.AssetTypes == null)
            {
                return typeNames;
            }

            for (int i = 0; i < context.AssetTypes.Count; i++)
            {
                AssetTypeConfig typeConfig = context.AssetTypes[i];

                if (typeConfig == null)
                {
                    continue;
                }

                string typeName = string.IsNullOrEmpty(
                    typeConfig.DisplayName)
                    ? typeConfig.AssetTypeId
                    : typeConfig.DisplayName;

                if (!string.IsNullOrEmpty(typeName))
                {
                    typeNames.Add(typeName);
                }
            }

            typeNames.Sort(StringComparer.Ordinal);
            return typeNames;
        }

        /// <summary>
        /// 获取资源的直接依赖路径，并按路径稳定排序。
        /// </summary>
        private static List<string> GetDependencyPaths(
            AssetContext context)
        {
            List<string> dependencyPaths = new List<string>();

            if (context == null || context.Dependencies == null)
            {
                return dependencyPaths;
            }

            for (int i = 0; i < context.Dependencies.Count; i++)
            {
                string dependencyPath = GetNormalizedAssetPath(
                    context.Dependencies[i]);

                if (!string.IsNullOrEmpty(dependencyPath) &&
                    !dependencyPaths.Contains(dependencyPath))
                {
                    dependencyPaths.Add(dependencyPath);
                }
            }

            dependencyPaths.Sort(StringComparer.Ordinal);
            return dependencyPaths;
        }

        /// <summary>
        /// 获取资源的所有直接父资源路径；根节点返回空列表。
        /// </summary>
        private static List<string> GetParentPaths(
            string assetPath,
            Dictionary<string, List<string>> parentPathsByChild)
        {
            List<string> parentPaths;

            if (!string.IsNullOrEmpty(assetPath) &&
                parentPathsByChild.TryGetValue(
                    assetPath,
                    out parentPaths))
            {
                return new List<string>(parentPaths);
            }

            return new List<string>();
        }

        /// <summary>
        /// 规范化资源路径，避免导出中出现相同资源的不同分隔符形式。
        /// </summary>
        private static string GetNormalizedAssetPath(AssetContext context)
        {
            return context == null ||
                   string.IsNullOrEmpty(context.AssetPath)
                ? string.Empty
                : ScanToolUtility.NormalizePath(context.AssetPath);
        }

        /// <summary>
        /// 构建资源到所有所属根资源的映射，以便关联表可跨根资源筛选。
        /// </summary>
        private static Dictionary<string, List<string>> BuildRootPathsByAsset(
            AssetScoreReport report)
        {
            Dictionary<string, HashSet<string>> rootPathSets =
                new Dictionary<string, HashSet<string>>();

            for (int i = 0; i < report.DisplayRootResults.Count; i++)
            {
                AssetContext rootContext = report.DisplayRootResults[i].Context;
                string rootPath = GetNormalizedAssetPath(rootContext);

                if (string.IsNullOrEmpty(rootPath))
                {
                    continue;
                }

                CollectRootPaths(
                    report,
                    rootContext,
                    rootPath,
                    rootPathSets,
                    new HashSet<string>());
            }

            Dictionary<string, List<string>> rootPathsByAsset =
                new Dictionary<string, List<string>>();

            foreach (KeyValuePair<string, HashSet<string>> pair
                     in rootPathSets)
            {
                List<string> rootPaths = new List<string>(pair.Value);
                rootPaths.Sort(StringComparer.Ordinal);
                rootPathsByAsset.Add(pair.Key, rootPaths);
            }

            return rootPathsByAsset;
        }

        /// <summary>
        /// 递归收集一个根资源可达的资源，避免循环依赖重复记录。
        /// </summary>
        private static void CollectRootPaths(
            AssetScoreReport report,
            AssetContext context,
            string rootPath,
            Dictionary<string, HashSet<string>> rootPathSets,
            HashSet<string> visitedPaths)
        {
            string assetPath = GetNormalizedAssetPath(context);
            AssetScanResult result;

            if (string.IsNullOrEmpty(assetPath) ||
                visitedPaths.Contains(assetPath) ||
                !report.TryGetResult(assetPath, out result))
            {
                return;
            }

            visitedPaths.Add(assetPath);
            HashSet<string> rootPaths;

            if (!rootPathSets.TryGetValue(assetPath, out rootPaths))
            {
                rootPaths = new HashSet<string>();
                rootPathSets.Add(assetPath, rootPaths);
            }

            rootPaths.Add(rootPath);

            if (context.Dependencies == null)
            {
                return;
            }

            for (int i = 0; i < context.Dependencies.Count; i++)
            {
                CollectRootPaths(
                    report,
                    context.Dependencies[i],
                    rootPath,
                    rootPathSets,
                    visitedPaths);
            }
        }

        /// <summary>
        /// 构建每个入口资源的汇总统计表。
        /// </summary>
        private static string BuildCsvSummary(
            AssetScoreReport report,
            ExportFileData exportData,
            Dictionary<string, List<string>> rootPathsByAsset)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(CsvSummaryHeader);

            for (int i = 0; i < exportData.rootAssetPaths.Count; i++)
            {
                string rootPath = exportData.rootAssetPaths[i];
                AssetScanResult rootResult;

                if (!report.TryGetResult(rootPath, out rootResult))
                {
                    continue;
                }

                int blockerCount = 0;
                int errorCount = 0;
                int warningCount = 0;
                int infoCount = 0;

                foreach (KeyValuePair<string, List<string>> pair
                         in rootPathsByAsset)
                {
                    if (!pair.Value.Contains(rootPath))
                    {
                        continue;
                    }

                    AssetScanResult result;
                    if (report.TryGetResult(pair.Key, out result))
                    {
                        CountIssueLevels(
                            result,
                            ref blockerCount,
                            ref errorCount,
                            ref warningCount,
                            ref infoCount);
                    }
                }

                AppendCsvRow(builder, new string[]
                {
                    rootPath,
                    rootResult.Score.ToString(CultureInfo.InvariantCulture),
                    rootResult.Grade.ToString(),
                    rootResult.HighestIssueLevel.ToString(),
                    rootResult.SelfIssueCount.ToString(CultureInfo.InvariantCulture),
                    rootResult.IssueCount.ToString(CultureInfo.InvariantCulture),
                    blockerCount.ToString(CultureInfo.InvariantCulture),
                    errorCount.ToString(CultureInfo.InvariantCulture),
                    warningCount.ToString(CultureInfo.InvariantCulture),
                    infoCount.ToString(CultureInfo.InvariantCulture),
                    CountRootAssets(rootPath, rootPathsByAsset).ToString(
                        CultureInfo.InvariantCulture)
                });
            }

            return builder.ToString();
        }

        /// <summary>
        /// 构建包含所有唯一资源评分和关系字段的资源总览表。
        /// </summary>
        private static string BuildCsvAssets(
            ExportFileData exportData,
            Dictionary<string, List<string>> rootPathsByAsset)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(CsvAssetsHeader);

            for (int i = 0; i < exportData.assets.Count; i++)
            {
                AssetExportData asset = exportData.assets[i];
                AppendCsvRow(builder, new string[]
                {
                    GetRootPathText(asset.assetPath, rootPathsByAsset),
                    asset.assetName,
                    asset.assetPath,
                    JoinValues(asset.assetTypes),
                    asset.isRoot ? "是" : "否",
                    JoinValues(asset.parentAssetPaths),
                    JoinValues(asset.directDependencyPaths),
                    asset.selfScore.ToString(CultureInfo.InvariantCulture),
                    asset.selfGrade,
                    asset.selfHighestIssueLevel,
                    asset.selfIssueCount.ToString(CultureInfo.InvariantCulture),
                    asset.score.ToString(CultureInfo.InvariantCulture),
                    asset.grade,
                    asset.highestIssueLevel,
                    asset.issueCount.ToString(CultureInfo.InvariantCulture)
                });
            }

            return builder.ToString();
        }

        /// <summary>
        /// 构建一行一个规则问题的明细表，保留规则、扣分和对象定位信息。
        /// </summary>
        private static string BuildCsvIssues(
            ExportFileData exportData,
            Dictionary<string, List<string>> rootPathsByAsset)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(CsvIssuesHeader);

            for (int i = 0; i < exportData.assets.Count; i++)
            {
                AssetExportData asset = exportData.assets[i];

                for (int j = 0; j < asset.issues.Count; j++)
                {
                    IssueExportData issue = asset.issues[j];
                    AppendCsvRow(builder, new string[]
                    {
                        GetRootPathText(asset.assetPath, rootPathsByAsset),
                        asset.assetPath,
                        JoinValues(asset.assetTypes),
                        issue.issueId,
                        issue.issueName,
                        issue.issueLevel,
                        issue.deductScore.ToString(CultureInfo.InvariantCulture),
                        issue.ruleId,
                        issue.ruleName,
                        issue.issueMessage,
                        GetIssueLocationText(issue.locations)
                    });
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// 构建一行一条直接依赖的关系表，便于按父子资源筛选。
        /// </summary>
        private static string BuildCsvDependencies(
            AssetScoreReport report,
            Dictionary<string, List<string>> rootPathsByAsset)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(CsvDependenciesHeader);
            List<string> parentPaths = new List<string>(report.ResultsByPath.Keys);
            parentPaths.Sort(StringComparer.Ordinal);

            for (int i = 0; i < parentPaths.Count; i++)
            {
                AssetScanResult parentResult = report.ResultsByPath[parentPaths[i]];
                List<string> childPaths = GetDependencyPaths(parentResult.Context);

                for (int j = 0; j < childPaths.Count; j++)
                {
                    AssetScanResult childResult;
                    bool hasChildResult = report.TryGetResult(
                        childPaths[j],
                        out childResult);
                    AppendCsvRow(builder, new string[]
                    {
                        GetRootPathText(parentPaths[i], rootPathsByAsset),
                        parentPaths[i],
                        childPaths[j],
                        JoinValues(GetAssetTypeNames(parentResult.Context)),
                        !hasChildResult
                            ? string.Empty
                            : JoinValues(GetAssetTypeNames(childResult.Context)),
                        !hasChildResult
                            ? string.Empty
                            : childResult.Score.ToString(CultureInfo.InvariantCulture),
                        !hasChildResult ? string.Empty : childResult.Grade.ToString(),
                        !hasChildResult
                            ? string.Empty
                            : childResult.IssueCount.ToString(CultureInfo.InvariantCulture)
                    });
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// 统计单个资源自身问题的各严重度数量。
        /// </summary>
        private static void CountIssueLevels(
            AssetScanResult result,
            ref int blockerCount,
            ref int errorCount,
            ref int warningCount,
            ref int infoCount)
        {
            if (result.Issues == null)
            {
                return;
            }

            for (int i = 0; i < result.Issues.Count; i++)
            {
                IssueExportData issue = BuildIssueData(result.Issues[i]);

                switch (issue.issueLevel)
                {
                    case "Blocker": blockerCount++; break;
                    case "Error": errorCount++; break;
                    case "Warning": warningCount++; break;
                    case "Info": infoCount++; break;
                }
            }
        }

        /// <summary>
        /// 统计指定根资源在关联映射中可达的唯一资源数量。
        /// </summary>
        private static int CountRootAssets(
            string rootPath,
            Dictionary<string, List<string>> rootPathsByAsset)
        {
            int count = 0;

            foreach (KeyValuePair<string, List<string>> pair
                     in rootPathsByAsset)
            {
                if (pair.Value.Contains(rootPath))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 将多值字段转换为可在单个 CSV 单元格中筛选阅读的分号分隔文本。
        /// </summary>
        private static string JoinValues(List<string> values)
        {
            return values == null || values.Count == 0
                ? string.Empty
                : string.Join(";", values.ToArray());
        }

        /// <summary>
        /// 获取资源所属的根资源路径文本。
        /// </summary>
        private static string GetRootPathText(
            string assetPath,
            Dictionary<string, List<string>> rootPathsByAsset)
        {
            List<string> rootPaths;
            return rootPathsByAsset.TryGetValue(assetPath, out rootPaths)
                ? JoinValues(rootPaths)
                : string.Empty;
        }

        /// <summary>
        /// 将 Prefab 内部对象定位信息合并为单个 CSV 单元格。
        /// </summary>
        private static string GetIssueLocationText(
            List<IssueLocationExportData> locations)
        {
            if (locations == null || locations.Count == 0)
            {
                return string.Empty;
            }

            List<string> locationTexts = new List<string>();

            for (int i = 0; i < locations.Count; i++)
            {
                IssueLocationExportData location = locations[i];

                if (location == null)
                {
                    continue;
                }

                locationTexts.Add(
                    location.hierarchyPath + "：" +
                    location.description);
            }

            return string.Join(" | ", locationTexts.ToArray());
        }

        /// <summary>
        /// 写入经过 RFC 4180 转义的 CSV 行。
        /// </summary>
        private static void AppendCsvRow(
            StringBuilder builder,
            string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append(EscapeCsvValue(values[i]));
            }

            builder.AppendLine();
        }

        /// <summary>
        /// 转义包含逗号、引号或换行的 CSV 单元格值。
        /// </summary>
        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            bool requiresQuotes = value.IndexOfAny(
                new char[] { ',', '"', '\r', '\n' }) >= 0;

            if (!requiresQuotes)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>
        /// 以标准 Open XML 结构写入包含多个工作表的 Excel 文件。
        /// </summary>
        private static void WriteExcelFile(
            string filePath,
            List<ExcelSheetData> sheets)
        {
            string directoryPath = Path.GetDirectoryName(filePath);

            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new ArgumentException(
                    "导出文件路径必须包含目录。",
                    nameof(filePath));
            }

            Directory.CreateDirectory(directoryPath);

            using (FileStream stream = new FileStream(
                       filePath,
                       FileMode.Create,
                       FileAccess.Write))
            using (ZipArchive archive = new ZipArchive(
                       stream,
                       ZipArchiveMode.Create))
            {
                WriteZipXml(archive, "[Content_Types].xml", WriteContentTypes);
                WriteZipXml(archive, "_rels/.rels", WriteRootRelationships);
                WriteZipXml(archive, "xl/workbook.xml", delegate(XmlWriter writer)
                {
                    WriteWorkbook(writer, sheets);
                });
                WriteZipXml(archive, "xl/_rels/workbook.xml.rels", delegate(XmlWriter writer)
                {
                    WriteWorkbookRelationships(writer, sheets.Count);
                });
                WriteZipXml(archive, "xl/styles.xml", WriteStyles);

                for (int i = 0; i < sheets.Count; i++)
                {
                    int sheetIndex = i + 1;
                    ExcelSheetData sheet = sheets[i];
                    WriteZipXml(
                        archive,
                        "xl/worksheets/sheet" + sheetIndex + ".xml",
                        delegate(XmlWriter writer)
                        {
                            WriteWorksheet(writer, sheet);
                        });
                }
            }
        }

        /// <summary>
        /// 向压缩包添加一个 UTF-8 XML 条目。
        /// </summary>
        private static void WriteZipXml(
            ZipArchive archive,
            string entryName,
            Action<XmlWriter> writeAction)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName);
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = new UTF8Encoding(false);
            settings.Indent = false;
            settings.CloseOutput = true;

            using (XmlWriter writer = XmlWriter.Create(entry.Open(), settings))
            {
                writeAction(writer);
            }
        }

        /// <summary>
        /// 写入 Excel 包的内容类型声明。
        /// </summary>
        private static void WriteContentTypes(XmlWriter writer)
        {
            writer.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");
            WriteXmlElementWithAttribute(writer, "Default", "Extension", "rels", "ContentType", "application/vnd.openxmlformats-package.relationships+xml");
            WriteXmlElementWithAttribute(writer, "Default", "Extension", "xml", "ContentType", "application/xml");
            WriteXmlElementWithAttribute(writer, "Override", "PartName", "/xl/workbook.xml", "ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
            WriteXmlElementWithAttribute(writer, "Override", "PartName", "/xl/styles.xml", "ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");
            for (int i = 1; i <= 4; i++)
            {
                WriteXmlElementWithAttribute(writer, "Override", "PartName", "/xl/worksheets/sheet" + i + ".xml", "ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
            }
            writer.WriteEndElement();
        }

        /// <summary>
        /// 写入包根目录到工作簿的关系声明。
        /// </summary>
        private static void WriteRootRelationships(XmlWriter writer)
        {
            writer.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
            writer.WriteStartElement("Relationship");
            writer.WriteAttributeString("Id", "rId1");
            writer.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument");
            writer.WriteAttributeString("Target", "xl/workbook.xml");
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        /// <summary>
        /// 写入工作簿及其工作表名称。
        /// </summary>
        private static void WriteWorkbook(
            XmlWriter writer,
            List<ExcelSheetData> sheets)
        {
            writer.WriteStartElement("workbook", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            writer.WriteAttributeString("xmlns", "r", null, "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            writer.WriteStartElement("sheets");
            for (int i = 0; i < sheets.Count; i++)
            {
                writer.WriteStartElement("sheet");
                writer.WriteAttributeString("name", sheets[i].Name);
                writer.WriteAttributeString("sheetId", (i + 1).ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("r", "id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships", "rId" + (i + 1));
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        /// <summary>
        /// 写入工作簿到样式和工作表 XML 的关系声明。
        /// </summary>
        private static void WriteWorkbookRelationships(XmlWriter writer, int sheetCount)
        {
            writer.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
            for (int i = 1; i <= sheetCount; i++)
            {
                writer.WriteStartElement("Relationship");
                writer.WriteAttributeString("Id", "rId" + i);
                writer.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet");
                writer.WriteAttributeString("Target", "worksheets/sheet" + i + ".xml");
                writer.WriteEndElement();
            }
            writer.WriteStartElement("Relationship");
            writer.WriteAttributeString("Id", "rId" + (sheetCount + 1));
            writer.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles");
            writer.WriteAttributeString("Target", "styles.xml");
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        /// <summary>
        /// 写入表头加粗的最小样式定义。
        /// </summary>
        private static void WriteStyles(XmlWriter writer)
        {
            writer.WriteStartElement("styleSheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            writer.WriteStartElement("fonts"); writer.WriteAttributeString("count", "2");
            writer.WriteStartElement("font"); WriteFontSize(writer); writer.WriteEndElement();
            writer.WriteStartElement("font"); writer.WriteElementString("b", string.Empty); WriteFontSize(writer); writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteStartElement("fills"); writer.WriteAttributeString("count", "2");
            writer.WriteStartElement("fill"); writer.WriteElementString("patternFill", string.Empty); writer.WriteEndElement();
            writer.WriteStartElement("fill"); writer.WriteStartElement("patternFill"); writer.WriteAttributeString("patternType", "gray125"); writer.WriteEndElement(); writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteStartElement("borders"); writer.WriteAttributeString("count", "1"); writer.WriteElementString("border", string.Empty); writer.WriteEndElement();
            writer.WriteStartElement("cellStyleXfs"); writer.WriteAttributeString("count", "1"); writer.WriteElementString("xf", string.Empty); writer.WriteEndElement();
            writer.WriteStartElement("cellXfs"); writer.WriteAttributeString("count", "2");
            writer.WriteStartElement("xf"); writer.WriteAttributeString("fontId", "0"); writer.WriteAttributeString("fillId", "0"); writer.WriteAttributeString("borderId", "0"); writer.WriteAttributeString("xfId", "0"); writer.WriteEndElement();
            writer.WriteStartElement("xf"); writer.WriteAttributeString("fontId", "1"); writer.WriteAttributeString("fillId", "0"); writer.WriteAttributeString("borderId", "0"); writer.WriteAttributeString("xfId", "0"); writer.WriteAttributeString("applyFont", "1"); writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        /// <summary>
        /// 按 Open XML 规范写入字体字号属性。
        /// </summary>
        private static void WriteFontSize(XmlWriter writer)
        {
            writer.WriteStartElement("sz");
            writer.WriteAttributeString("val", "11");
            writer.WriteEndElement();
        }

        /// <summary>
        /// 写入单个冻结表头并启用筛选的工作表。
        /// </summary>
        private static void WriteWorksheet(XmlWriter writer, ExcelSheetData sheet)
        {
            List<string[]> rows = ParseCsvRows(sheet.CsvContent);
            int columnCount = rows.Count == 0 ? 0 : rows[0].Length;
            writer.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            writer.WriteStartElement("sheetViews"); writer.WriteStartElement("sheetView"); writer.WriteAttributeString("workbookViewId", "0");
            writer.WriteStartElement("pane"); writer.WriteAttributeString("ySplit", "1"); writer.WriteAttributeString("topLeftCell", "A2"); writer.WriteAttributeString("activePane", "bottomLeft"); writer.WriteAttributeString("state", "frozen"); writer.WriteEndElement();
            writer.WriteEndElement(); writer.WriteEndElement();
            writer.WriteStartElement("sheetData");
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                writer.WriteStartElement("row"); writer.WriteAttributeString("r", (rowIndex + 1).ToString(CultureInfo.InvariantCulture));
                for (int columnIndex = 0; columnIndex < rows[rowIndex].Length; columnIndex++)
                {
                    writer.WriteStartElement("c"); writer.WriteAttributeString("r", GetExcelColumnName(columnIndex + 1) + (rowIndex + 1)); writer.WriteAttributeString("t", "inlineStr");
                    if (rowIndex == 0) writer.WriteAttributeString("s", "1");
                    writer.WriteStartElement("is"); writer.WriteStartElement("t");
                    if (!string.IsNullOrEmpty(rows[rowIndex][columnIndex]) && (char.IsWhiteSpace(rows[rowIndex][columnIndex][0]) || char.IsWhiteSpace(rows[rowIndex][columnIndex][rows[rowIndex][columnIndex].Length - 1]))) writer.WriteAttributeString("xml", "space", null, "preserve");
                    writer.WriteString(rows[rowIndex][columnIndex] ?? string.Empty);
                    writer.WriteEndElement(); writer.WriteEndElement(); writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            if (rows.Count > 0 && columnCount > 0)
            {
                writer.WriteStartElement("autoFilter"); writer.WriteAttributeString("ref", "A1:" + GetExcelColumnName(columnCount) + rows.Count); writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        /// <summary>
        /// 将已有的 RFC 4180 CSV 文本还原为 Excel 工作表行和单元格。
        /// </summary>
        private static List<string[]> ParseCsvRows(string csvContent)
        {
            List<string[]> rows = new List<string[]>();
            List<string> values = new List<string>();
            StringBuilder value = new StringBuilder();
            bool isQuoted = false;

            for (int i = 0; i < csvContent.Length; i++)
            {
                char character = csvContent[i];

                if (character == '"')
                {
                    if (isQuoted && i + 1 < csvContent.Length &&
                        csvContent[i + 1] == '"')
                    {
                        value.Append(character);
                        i++;
                    }
                    else
                    {
                        isQuoted = !isQuoted;
                    }

                    continue;
                }

                if (!isQuoted && character == ',')
                {
                    values.Add(value.ToString());
                    value.Length = 0;
                    continue;
                }

                if (!isQuoted && (character == '\r' || character == '\n'))
                {
                    if (character == '\r' && i + 1 < csvContent.Length &&
                        csvContent[i + 1] == '\n')
                    {
                        i++;
                    }

                    values.Add(value.ToString());
                    rows.Add(values.ToArray());
                    values.Clear();
                    value.Length = 0;
                    continue;
                }

                value.Append(character);
            }

            if (value.Length > 0 || values.Count > 0)
            {
                values.Add(value.ToString());
                rows.Add(values.ToArray());
            }

            return rows;
        }

        /// <summary>
        /// 将从一开始的列索引转换为 Excel 列名。
        /// </summary>
        private static string GetExcelColumnName(int columnIndex)
        {
            StringBuilder builder = new StringBuilder();

            while (columnIndex > 0)
            {
                columnIndex--;
                builder.Insert(0, (char)('A' + columnIndex % 26));
                columnIndex /= 26;
            }

            return builder.ToString();
        }

        /// <summary>
        /// 写入仅包含两个属性的空 XML 元素。
        /// </summary>
        private static void WriteXmlElementWithAttribute(
            XmlWriter writer,
            string elementName,
            string firstAttributeName,
            string firstAttributeValue,
            string secondAttributeName,
            string secondAttributeValue)
        {
            writer.WriteStartElement(elementName);
            writer.WriteAttributeString(firstAttributeName, firstAttributeValue);
            writer.WriteAttributeString(secondAttributeName, secondAttributeValue);
            writer.WriteEndElement();
        }

        /// <summary>
        /// 以带 BOM 的 UTF-8 编码写入导出文件，并在需要时创建父目录。
        /// </summary>
        private static void WriteTextFile(string filePath, string content)
        {
            string directoryPath = Path.GetDirectoryName(filePath);

            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new ArgumentException(
                    "导出文件路径必须包含目录。",
                    nameof(filePath));
            }

            Directory.CreateDirectory(directoryPath);
            File.WriteAllText(
                filePath,
                content ?? string.Empty,
                new UTF8Encoding(true));
        }

        /// <summary>
        /// 描述一个完整报告导出文件的根对象。
        /// </summary>
        [Serializable]
        private sealed class ExportFileData
        {
            public int schemaVersion;
            public string exportedAtUtc;
            public List<string> rootAssetPaths = new List<string>();
            public List<AssetExportData> assets =
                new List<AssetExportData>();
        }

        /// <summary>
        /// 描述一个 Excel 工作表的名称和待写入的 CSV 格式行数据。
        /// </summary>
        private sealed class ExcelSheetData
        {
            public readonly string Name;
            public readonly string CsvContent;

            /// <summary>
            /// 使用指定名称和表格文本创建工作表数据。
            /// </summary>
            public ExcelSheetData(string name, string csvContent)
            {
                Name = name;
                CsvContent = csvContent ?? string.Empty;
            }
        }

        /// <summary>
        /// 描述一个唯一资源的评分、依赖与自身问题信息。
        /// </summary>
        [Serializable]
        private sealed class AssetExportData
        {
            public string assetPath;
            public string assetName;
            public bool isRoot;
            public List<string> assetTypes = new List<string>();
            public List<string> parentAssetPaths = new List<string>();
            public List<string> directDependencyPaths = new List<string>();
            public int selfScore;
            public string selfGrade;
            public string selfHighestIssueLevel;
            public int selfIssueCount;
            public int score;
            public string grade;
            public string highestIssueLevel;
            public int issueCount;
            public List<IssueExportData> issues =
                new List<IssueExportData>();
        }

        /// <summary>
        /// 描述一条规则命中的具体问题与可选对象定位信息。
        /// </summary>
        [Serializable]
        private sealed class IssueExportData
        {
            public string ruleId;
            public string ruleName;
            public string issueId;
            public string issueName;
            public int deductScore;
            public string issueLevel;
            public string issueMessage;
            public List<IssueLocationExportData> locations =
                new List<IssueLocationExportData>();
        }

        /// <summary>
        /// 描述 Prefab 问题所在的层级路径与补充说明。
        /// </summary>
        [Serializable]
        private sealed class IssueLocationExportData
        {
            public string hierarchyPath;
            public string description;
        }
    }
}
