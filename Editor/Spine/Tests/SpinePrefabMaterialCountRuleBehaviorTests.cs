using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using majsoul.tools.assetscoretool.editor;

namespace majsoul.tools.assetscoretool.editor.spine.tests
{
    /// <summary>
    /// 验证 Spine Prefab Atlas 材质数量规则在各 Profile 下的扫描结果。
    /// </summary>
    public class SpinePrefabMaterialCountRuleBehaviorTests
    {
        private const string RootPath =
            "Assets/Tools/asset_score_tool/TestAssets/GeneratedSpine";

        private const string PrefabPath = RootPath + "/MaterialCount.prefab";
        private const string SkeletonPath = RootPath + "/MaterialCountSkeleton.asset";
        private const string AtlasPath = RootPath + "/MaterialCountAtlas.asset";
        private const string MaterialPathPrefix = RootPath + "/MaterialCount_";

        [TestCase("pc", "basic.spine.materialcount4")]
        [TestCase("web", "basic.spine.materialcount2")]
        [TestCase("android", "basic.spine.materialcount2")]
        [TestCase("ios", "basic.spine.materialcount2")]
        public void ExcessiveMaterialsReportProfileRule(
            string profileId,
            string expectedRuleId)
        {
            CreatePrefab(5);

            AssetScanData scanData = ScanPrefab(profileId).GetAwaiter().GetResult();

            CollectionAssert.AreEquivalent(
                new[] { expectedRuleId },
                GetIssueRuleIds(scanData),
                "Spine Prefab 材质数量未命中预期规则，Profile：" +
                profileId);
        }

        [TestCase("pc")]
        [TestCase("web")]
        [TestCase("android")]
        [TestCase("ios")]
        public void SingleMaterialReportsNoRule(string profileId)
        {
            CreatePrefab(1);

            AssetScanData scanData = ScanPrefab(profileId).GetAwaiter().GetResult();

            Assert.That(GetIssueRuleIds(scanData), Is.Empty,
                "单材质 Spine Prefab 不应命中规则，Profile：" + profileId);
        }

        [TearDown]
        public void TearDown()
        {
            DeleteAssets();
            AssetDatabase.Refresh();
        }

        private static void CreatePrefab(int materialCount)
        {
            DeleteAssets();
            Directory.CreateDirectory(Path.GetFullPath(RootPath));

            Shader shader = Shader.Find("Sprites/Default");
            Assert.That(shader, Is.Not.Null, "未找到测试材质 Shader。");
            Material[] materials = new Material[materialCount];

            for (int i = 0; i < materialCount; i++)
            {
                Material material = new Material(shader);
                AssetDatabase.CreateAsset(
                    material,
                    MaterialPathPrefix + i + ".mat");
                materials[i] = material;
            }

            SpineAtlasAsset atlas = ScriptableObject.CreateInstance<SpineAtlasAsset>();
            atlas.materials = materials;
            AssetDatabase.CreateAsset(atlas, AtlasPath);

            SkeletonDataAsset skeleton =
                ScriptableObject.CreateInstance<SkeletonDataAsset>();
            skeleton.atlasAssets = new AtlasAssetBase[] { atlas };
            AssetDatabase.CreateAsset(skeleton, SkeletonPath);

            GameObject root = new GameObject("SpineMaterialCountRoot");
            SkeletonAnimation animation = root.AddComponent<SkeletonAnimation>();
            animation.skeletonDataAsset = skeleton;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
        }

        private static async Task<AssetScanData> ScanPrefab(string profileId)
        {
            Dictionary<string, ScoreRuleProfileConfig> profiles =
                ScanToolUtility.GetAllProfileConfigs();
            Assert.That(profiles.ContainsKey(profileId), Is.True,
                "未找到 Profile：" + profileId);

            AssetScoreToolScanner scanner = new AssetScoreToolScanner();
            scanner.LoadData();
            return await scanner.StartScanAsync(
                PrefabPath,
                profiles[profileId],
                null,
                false);
        }

        private static HashSet<string> GetIssueRuleIds(AssetScanData scanData)
        {
            Assert.That(scanData, Is.Not.Null);
            List<AssetIssueResult> issues = scanData.GetIssues(PrefabPath);
            HashSet<string> ruleIds = new HashSet<string>();

            for (int i = 0; i < issues.Count; i++)
            {
                AssetIssueResult issue = issues[i];

                Assert.That(issue, Is.Not.Null);
                Assert.That(issue.RuleConfig, Is.Not.Null);
                Assert.That(issue.IssueInfo, Is.Not.Null);
                ruleIds.Add(issue.RuleConfig.RuleId);
            }

            return ruleIds;
        }

        private static void DeleteAssets()
        {
            AssetDatabase.DeleteAsset(PrefabPath);
            AssetDatabase.DeleteAsset(SkeletonPath);
            AssetDatabase.DeleteAsset(AtlasPath);

            for (int i = 0; i < 5; i++)
            {
                AssetDatabase.DeleteAsset(MaterialPathPrefix + i + ".mat");
            }
        }
    }
}