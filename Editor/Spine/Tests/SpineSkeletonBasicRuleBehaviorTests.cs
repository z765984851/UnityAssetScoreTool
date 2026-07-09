using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using majsoul.tools.assetscoretool.editor;

namespace majsoul.tools.assetscoretool.editor.spine.tests
{
    /// <summary>
    /// 验证 Spine Skeleton Atlas 数量与缺失依赖规则。
    /// </summary>
    public class SpineSkeletonBasicRuleBehaviorTests
    {
        private const string RootPath =
            "Assets/Tools/asset_score_tool/TestAssets/GeneratedSpine";

        private const string SkeletonPath = RootPath + "/TestSkeleton.asset";
        private const string AtlasPathPrefix = RootPath + "/TestAtlas_";
        private const string AtlasCountRuleId = "basic.spine.atlascount1";
        private const string MissingAssetRuleId = "basic.spine.missingasset";

        private const string ValidSkeletonPath =
            "Assets/MyAssets/ui/activity/extend/2606dp/main/spine/chimu/" +
            "chimumao_2_SkeletonData.asset";

        [TestCase("pc")]
        [TestCase("web")]
        [TestCase("android")]
        [TestCase("ios")]
        public void MultipleAtlasAssetsReportAtlasCountRule(string profileId)
        {
            CreateSkeleton(2);

            AssetScanData scanData = ScanSkeleton(profileId).GetAwaiter().GetResult();

            Assert.That(GetIssueRuleIds(scanData), Does.Contain(AtlasCountRuleId),
                "多个 Atlas 未命中规则，Profile：" + profileId);
        }

        [TestCase("pc")]
        [TestCase("web")]
        [TestCase("android")]
        [TestCase("ios")]
        public void SingleAtlasAssetDoesNotReportAtlasCountRule(
            string profileId)
        {
            CreateSkeleton(1);

            AssetScanData scanData = ScanSkeleton(profileId).GetAwaiter().GetResult();

            Assert.That(GetIssueRuleIds(scanData),
                Does.Not.Contain(AtlasCountRuleId),
                "单个 Atlas 仍命中数量规则，Profile：" + profileId);
        }

        [TestCase("pc")]
        [TestCase("web")]
        [TestCase("android")]
        [TestCase("ios")]
        public void MissingSkeletonDependenciesReportRule(string profileId)
        {
            CreateSkeleton(0);

            AssetScanData scanData = ScanSkeleton(profileId).GetAwaiter().GetResult();

            Assert.That(GetIssueRuleIds(scanData), Does.Contain(MissingAssetRuleId),
                "缺失 Spine 依赖未命中规则，Profile：" + profileId);
        }

        [TestCase("pc")]
        [TestCase("web")]
        [TestCase("android")]
        [TestCase("ios")]
        public void CompleteProjectSkeletonDoesNotReportMissingAssetRule(
            string profileId)
        {
            AssetScanData scanData = ScanSkeleton(
                profileId,
                ValidSkeletonPath).GetAwaiter().GetResult();

            Assert.That(GetIssueRuleIds(scanData, ValidSkeletonPath),
                Does.Not.Contain(MissingAssetRuleId),
                "完整 Spine SkeletonData 不应命中缺失依赖规则，Profile：" +
                profileId);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(SkeletonPath);

            for (int i = 0; i < 2; i++)
            {
                AssetDatabase.DeleteAsset(AtlasPathPrefix + i + ".asset");
            }

            AssetDatabase.Refresh();
        }

        private static void CreateSkeleton(int atlasCount)
        {
            TearDownAssets();
            List<AtlasAssetBase> atlasAssets = new List<AtlasAssetBase>();

            for (int i = 0; i < atlasCount; i++)
            {
                SpineAtlasAsset atlas = ScriptableObject.CreateInstance<SpineAtlasAsset>();
                AssetDatabase.CreateAsset(atlas, AtlasPathPrefix + i + ".asset");
                atlasAssets.Add(atlas);
            }

            SkeletonDataAsset skeleton = ScriptableObject.CreateInstance<SkeletonDataAsset>();
            skeleton.atlasAssets = atlasAssets.ToArray();
            AssetDatabase.CreateAsset(skeleton, SkeletonPath);
            AssetDatabase.SaveAssets();
        }

        private static void TearDownAssets()
        {
            AssetDatabase.DeleteAsset(SkeletonPath);

            for (int i = 0; i < 2; i++)
            {
                AssetDatabase.DeleteAsset(AtlasPathPrefix + i + ".asset");
            }
        }

        private static async Task<AssetScanData> ScanSkeleton(string profileId)
        {
            return await ScanSkeleton(profileId, SkeletonPath);
        }

        private static async Task<AssetScanData> ScanSkeleton(
            string profileId,
            string skeletonPath)
        {
            Dictionary<string, ScoreRuleProfileConfig> profiles =
                ScanToolUtility.GetAllProfileConfigs();
            Assert.That(profiles.ContainsKey(profileId), Is.True,
                "未找到 Profile：" + profileId);

            AssetScoreToolScanner scanner = new AssetScoreToolScanner();
            scanner.LoadData();
            return await scanner.StartScanAsync(
                skeletonPath,
                profiles[profileId],
                null,
                false);
        }

        private static HashSet<string> GetIssueRuleIds(
            AssetScanData scanData,
            string skeletonPath = SkeletonPath)
        {
            Assert.That(scanData, Is.Not.Null);
            List<AssetIssueResult> issues = scanData.GetIssues(skeletonPath);
            HashSet<string> ruleIds = new HashSet<string>();

            for (int i = 0; i < issues.Count; i++)
            {
                AssetIssueResult issue = issues[i];

                if (issue != null && issue.RuleConfig != null)
                {
                    ruleIds.Add(issue.RuleConfig.RuleId);
                }
            }

            return ruleIds;
        }
    }
}