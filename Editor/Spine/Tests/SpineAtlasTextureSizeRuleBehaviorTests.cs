using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using majsoul.tools.assetscoretool.editor;

namespace majsoul.tools.assetscoretool.editor.spine.tests
{
    /// <summary>
    /// 验证 Spine Atlas 贴图尺寸规则在各 Profile 下的扫描结果。
    /// </summary>
    public class SpineAtlasTextureSizeRuleBehaviorTests
    {
        private const string RootPath =
            "Assets/Tools/asset_score_tool/TestAssets/GeneratedSpine";

        private const string AtlasPath = RootPath + "/TestAtlas.asset";
        private const string MaterialPath = RootPath + "/TestAtlas.mat";
        private const string TexturePath = RootPath + "/TestAtlas.png";
        private const string RuleId = "basic.spine.texturesize4096";

        [TestCase("pc")]
        [TestCase("web")]
        [TestCase("android")]
        [TestCase("ios")]
        public void OversizedAtlasTextureReportsRule(
            string profileId)
        {
            CreateAtlas(4097);

            AssetScanData scanData = ScanAtlas(profileId).GetAwaiter().GetResult();

            CollectionAssert.AreEquivalent(
                new[] { RuleId },
                GetIssueRuleIds(scanData),
                "超限 Spine Atlas 贴图未按预期命中规则，Profile：" +
                profileId);
        }

        [TestCase("pc")]
        [TestCase("web")]
        [TestCase("android")]
        [TestCase("ios")]
        public void MaximumAllowedAtlasTextureReportsNoRule(
            string profileId)
        {
            CreateAtlas(4096);

            AssetScanData scanData = ScanAtlas(profileId).GetAwaiter().GetResult();

            Assert.That(GetIssueRuleIds(scanData), Is.Empty,
                "4096 像素 Spine Atlas 贴图不应命中规则，Profile：" +
                profileId);
        }

        [TearDown]
        public void TearDown()
        {
            DeleteAssetOrFile(AtlasPath);
            DeleteAssetOrFile(MaterialPath);
            DeleteAssetOrFile(TexturePath);
            AssetDatabase.Refresh();
        }

        private static void CreateAtlas(int textureWidth)
        {
            DeleteAssetOrFile(AtlasPath);
            DeleteAssetOrFile(MaterialPath);
            DeleteAssetOrFile(TexturePath);

            string absoluteDirectory = Path.GetFullPath(RootPath);

            if (!Directory.Exists(absoluteDirectory))
            {
                Directory.CreateDirectory(absoluteDirectory);
            }

            Texture2D texture = new Texture2D(
                textureWidth,
                1,
                TextureFormat.RGBA32,
                false);
            File.WriteAllBytes(
                Path.GetFullPath(TexturePath),
                texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(
                TexturePath,
                ImportAssetOptions.ForceSynchronousImport);

            TextureImporter textureImporter =
                AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            Assert.That(textureImporter, Is.Not.Null,
                "测试 Atlas 贴图导入失败。");
            textureImporter.maxTextureSize = 8192;
            textureImporter.npotScale = TextureImporterNPOTScale.None;
            textureImporter.SaveAndReimport();

            Shader shader = Shader.Find("Sprites/Default");
            Assert.That(shader, Is.Not.Null, "未找到测试材质 Shader。");

            Material material = new Material(shader);
            material.SetTexture(
                "_MainTex",
                AssetDatabase.LoadAssetAtPath<Texture>(TexturePath));
            AssetDatabase.CreateAsset(material, MaterialPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                MaterialPath,
                ImportAssetOptions.ForceSynchronousImport);

            material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Assert.That(material, Is.Not.Null, "测试材质未重新加载。");

            SpineAtlasAsset atlas = ScriptableObject.CreateInstance<SpineAtlasAsset>();
            AssetDatabase.CreateAsset(atlas, AtlasPath);
            atlas.materials = new[] { material };
            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                AtlasPath,
                ImportAssetOptions.ForceSynchronousImport);

            atlas = AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(AtlasPath);
            Assert.That(atlas, Is.Not.Null, "测试 Atlas 未重新加载。");
            Assert.That(atlas.materials, Has.Length.EqualTo(1));
            Assert.That(atlas.materials[0], Is.Not.Null);
            Assert.That(atlas.materials[0].mainTexture, Is.Not.Null);
            Assert.That(atlas.materials[0].mainTexture.width,
                Is.EqualTo(textureWidth));
        }

        private static async System.Threading.Tasks.Task<AssetScanData> ScanAtlas(
            string profileId)
        {
            Dictionary<string, ScoreRuleProfileConfig> profiles =
                ScanToolUtility.GetAllProfileConfigs();

            Assert.That(profiles.ContainsKey(profileId), Is.True,
                "未找到 Profile：" + profileId);

            AssetScoreToolScanner scanner = new AssetScoreToolScanner();
            scanner.LoadData();

            return await scanner.StartScanAsync(
                AtlasPath,
                profiles[profileId],
                null,
                false);
        }

        private static HashSet<string> GetIssueRuleIds(AssetScanData scanData)
        {
            Assert.That(scanData, Is.Not.Null);

            List<AssetIssueResult> issues = scanData.GetIssues(AtlasPath);
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

        private static void DeleteAssetOrFile(string assetPath)
        {
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
                return;
            }

            string absolutePath = Path.GetFullPath(assetPath);

            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            if (File.Exists(absolutePath + ".meta"))
            {
                File.Delete(absolutePath + ".meta");
            }
        }
    }
}
