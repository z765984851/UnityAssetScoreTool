using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using majsoul.tools.assetscoretool.editor;

namespace majsoul.tools.assetscoretool.editor.spine.tests
{
    /// <summary>
    /// 验证项目真实 Spine SkeletonData 的数值阈值规则与解析数据一致。
    /// </summary>
    public class SpineSkeletonThresholdRuleBehaviorTests
    {
        private const string GeneratedRootPath =
            "Assets/Tools/asset_score_tool/TestAssets/GeneratedSpine";

        private const string GeneratedSkeletonTextPath =
            GeneratedRootPath + "/ThresholdSkeleton.txt";

        private const string GeneratedSkeletonPath =
            GeneratedRootPath + "/ThresholdSkeleton.asset";

        private const string GeneratedAtlasTextPath =
            GeneratedRootPath + "/ThresholdSkeleton.atlas.txt";

        private const string GeneratedAtlasPath =
            GeneratedRootPath + "/ThresholdSkeleton_Atlas.asset";

        private const string GeneratedAtlasMaterialPath =
            GeneratedRootPath + "/ThresholdSkeleton.mat";

        private const string GeneratedAtlasTexturePath =
            GeneratedRootPath + "/ThresholdSkeleton.png";

        private const string SkeletonPath =
            "Assets/MyAssets/ui/activity/extend/2606dp/main/spine/waxizi/" +
            "jiuchaoyan_2_SkeletonData.asset";

        private const int ForcedSlotCount = 401;
        private const int ForcedMeshAttachmentCount = 801;

        private static readonly HashSet<string> ThresholdRuleIds =
            new HashSet<string>
            {
                "basic.spine.slotcount400",
                "basic.spine.slotcount200",
                "basic.spine.attachmentcount800",
                "basic.spine.attachmentcount400",
                "basic.spine.meshattachmentcount100",
                "basic.spine.meshattachmentcount50"
            };

        [TestCase("pc")]
        [TestCase("web")]
        [TestCase("android")]
        [TestCase("ios")]
        public void SkeletonReportsThresholdRulesForProfile(string profileId)
        {
            SkeletonMetrics metrics = GetSkeletonMetrics();

            AssetScanData scanData = ScanSkeleton(profileId).GetAwaiter().GetResult();
            HashSet<string> actualRuleIds = GetThresholdRuleIds(scanData);
            HashSet<string> expectedRuleIds = GetExpectedRuleIds(
                profileId,
                metrics);

            CollectionAssert.AreEquivalent(
                expectedRuleIds,
                actualRuleIds,
                "Spine 数值阈值规则与解析数据不一致，Profile：" +
                profileId + "，Slot：" + metrics.SlotCount +
                "，Attachment：" + metrics.AttachmentCount +
                "，Mesh Attachment：" + metrics.MeshAttachmentCount);
        }

        [TestCase("pc", new[]
        {
            "basic.spine.slotcount400",
            "basic.spine.attachmentcount800",
            "basic.spine.meshattachmentcount100"
        })]
        [TestCase("web", new[]
        {
            "basic.spine.slotcount200",
            "basic.spine.attachmentcount400",
            "basic.spine.meshattachmentcount50"
        })]
        [TestCase("android", new[]
        {
            "basic.spine.slotcount200",
            "basic.spine.attachmentcount400",
            "basic.spine.meshattachmentcount50"
        })]
        [TestCase("ios", new[]
        {
            "basic.spine.slotcount200",
            "basic.spine.attachmentcount400",
            "basic.spine.meshattachmentcount50"
        })]
        public void GeneratedSkeletonReportsAllForcedThresholdRules(
            string profileId,
            string[] expectedRuleIds)
        {
            CreateForcedThresholdSkeleton();
            SkeletonMetrics metrics = GetSkeletonMetrics(GeneratedSkeletonPath);

            Assert.That(metrics.SlotCount, Is.EqualTo(ForcedSlotCount));
            Assert.That(metrics.AttachmentCount,
                Is.EqualTo(ForcedMeshAttachmentCount));
            Assert.That(metrics.MeshAttachmentCount,
                Is.EqualTo(ForcedMeshAttachmentCount));

            AssetScanData scanData = ScanSkeleton(
                profileId,
                GeneratedSkeletonPath).GetAwaiter().GetResult();
            HashSet<string> actualRuleIds = GetThresholdRuleIds(
                scanData,
                GeneratedSkeletonPath);

            CollectionAssert.AreEquivalent(
                expectedRuleIds,
                actualRuleIds,
                "强制超阈值 Spine Skeleton 未命中预期规则，Profile：" +
                profileId);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(GeneratedSkeletonPath);
            DeleteAssetOrFile(GeneratedSkeletonTextPath);
            AssetDatabase.DeleteAsset(GeneratedAtlasPath);
            DeleteAssetOrFile(GeneratedAtlasTextPath);
            DeleteAssetOrFile(GeneratedAtlasMaterialPath);
            DeleteAssetOrFile(GeneratedAtlasTexturePath);
            AssetDatabase.Refresh();
        }

        private static SkeletonMetrics GetSkeletonMetrics(
            string skeletonPath = SkeletonPath)
        {
            SkeletonDataAsset skeletonAsset =
                AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(skeletonPath);
            Assert.That(skeletonAsset, Is.Not.Null,
                "未找到 Spine SkeletonData 测试资源：" + skeletonPath);

            SkeletonData skeletonData = skeletonAsset.GetSkeletonData(true);
            Assert.That(skeletonData, Is.Not.Null, "Spine SkeletonData 解析失败。");

            int attachmentCount = 0;
            int meshAttachmentCount = 0;

            for (int skinIndex = 0;
                 skinIndex < skeletonData.Skins.Count;
                 skinIndex++)
            {
                Skin skin = skeletonData.Skins.Items[skinIndex];

                foreach (Skin.SkinEntry entry in skin.Attachments)
                {
                    attachmentCount++;

                    if (entry.Attachment is MeshAttachment)
                    {
                        meshAttachmentCount++;
                    }
                }
            }

            return new SkeletonMetrics(
                skeletonData.Slots.Count,
                attachmentCount,
                meshAttachmentCount);
        }

        private static void CreateForcedThresholdSkeleton()
        {
            AssetDatabase.DeleteAsset(GeneratedSkeletonPath);
            DeleteAssetOrFile(GeneratedSkeletonTextPath);

            string absoluteDirectory = Path.GetFullPath(GeneratedRootPath);

            if (!Directory.Exists(absoluteDirectory))
            {
                Directory.CreateDirectory(absoluteDirectory);
            }

            SpineAtlasAsset atlas = CreateForcedThresholdAtlas();

            // 使用 .txt 作为持久化 TextAsset，避免 .json 触发 Spine 的自动导入器。
            File.WriteAllText(
                Path.GetFullPath(GeneratedSkeletonTextPath),
                CreateForcedThresholdSkeletonJson());
            AssetDatabase.ImportAsset(
                GeneratedSkeletonTextPath,
                ImportAssetOptions.ForceSynchronousImport);

            TextAsset skeletonJson = AssetDatabase.LoadAssetAtPath<TextAsset>(
                GeneratedSkeletonTextPath);
            Assert.That(skeletonJson, Is.Not.Null,
                "强制超阈值 Spine 文本未导入为 TextAsset。");

            SkeletonDataAsset skeletonAsset =
                SkeletonDataAsset.CreateRuntimeInstance(
                    skeletonJson,
                    atlas,
                    false);
            AssetDatabase.CreateAsset(skeletonAsset, GeneratedSkeletonPath);
            AssetDatabase.SaveAssets();
        }

        private static SpineAtlasAsset CreateForcedThresholdAtlas()
        {
            AssetDatabase.DeleteAsset(GeneratedAtlasPath);
            DeleteAssetOrFile(GeneratedAtlasTextPath);
            DeleteAssetOrFile(GeneratedAtlasMaterialPath);
            DeleteAssetOrFile(GeneratedAtlasTexturePath);

            Texture2D texture = new Texture2D(
                1,
                1,
                TextureFormat.RGBA32,
                false);
            texture.SetPixel(0, 0, Color.white);
            File.WriteAllBytes(
                Path.GetFullPath(GeneratedAtlasTexturePath),
                texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(
                GeneratedAtlasTexturePath,
                ImportAssetOptions.ForceSynchronousImport);

            Shader shader = Shader.Find("Spine/Skeleton");
            Assert.That(shader, Is.Not.Null, "未找到 Spine 测试材质 Shader。");

            Material material = new Material(shader);
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture>(
                GeneratedAtlasTexturePath);
            AssetDatabase.CreateAsset(material, GeneratedAtlasMaterialPath);

            File.WriteAllText(
                Path.GetFullPath(GeneratedAtlasTextPath),
                "ThresholdSkeleton.png\n" +
                "size: 1,1\n" +
                "format: RGBA8888\n" +
                "filter: Linear,Linear\n" +
                "repeat: none\n" +
                "region\n" +
                "  rotate: false\n" +
                "  xy: 0,0\n" +
                "  size: 1,1\n" +
                "  orig: 1,1\n" +
                "  offset: 0,0\n" +
                "  index: -1\n");
            AssetDatabase.ImportAsset(
                GeneratedAtlasTextPath,
                ImportAssetOptions.ForceSynchronousImport);

            TextAsset atlasText = AssetDatabase.LoadAssetAtPath<TextAsset>(
                GeneratedAtlasTextPath);
            material = AssetDatabase.LoadAssetAtPath<Material>(
                GeneratedAtlasMaterialPath);
            Assert.That(atlasText, Is.Not.Null, "强制阈值 Atlas 文本未导入。");
            Assert.That(material, Is.Not.Null, "强制阈值 Atlas 材质未导入。");

            SpineAtlasAsset atlas = SpineAtlasAsset.CreateRuntimeInstance(
                atlasText,
                new[] { material },
                false);
            AssetDatabase.CreateAsset(atlas, GeneratedAtlasPath);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(
                GeneratedAtlasPath);
        }

        private static string CreateForcedThresholdSkeletonJson()
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\"skeleton\":{\"hash\":\"\",\"spine\":\"4.2\"},");
            json.Append("\"bones\":[{\"name\":\"root\"}],\"slots\":[");

            for (int slotIndex = 0;
                 slotIndex < ForcedSlotCount;
                 slotIndex++)
            {
                if (slotIndex > 0)
                {
                    json.Append(',');
                }

                json.Append("{\"name\":\"slot_")
                    .Append(slotIndex)
                    .Append("\",\"bone\":\"root\"}");
            }

            json.Append("],\"skins\":[{\"name\":\"default\",\"attachments\":{");
            json.Append("\"slot_0\":{");

            for (int attachmentIndex = 0;
                 attachmentIndex < ForcedMeshAttachmentCount;
                 attachmentIndex++)
            {
                if (attachmentIndex > 0)
                {
                    json.Append(',');
                }

                json.Append("\"mesh_")
                    .Append(attachmentIndex)
                    .Append("\":{\"type\":\"mesh\",")
                    .Append("\"path\":\"region\",")
                    .Append("\"uvs\":[0,0,1,0,1,1],")
                    .Append("\"triangles\":[0,1,2],")
                    .Append("\"vertices\":[0,0,1,0,1,1]}");
            }

            json.Append("}}}]}");
            return json.ToString();
        }

        private static HashSet<string> GetExpectedRuleIds(
            string profileId,
            SkeletonMetrics metrics)
        {
            bool isPc = profileId == "pc";
            int slotLimit = isPc ? 400 : 200;
            int attachmentLimit = isPc ? 800 : 400;
            int meshAttachmentLimit = isPc ? 100 : 50;
            HashSet<string> ruleIds = new HashSet<string>();

            if (metrics.SlotCount > slotLimit)
            {
                ruleIds.Add(isPc
                    ? "basic.spine.slotcount400"
                    : "basic.spine.slotcount200");
            }

            if (metrics.AttachmentCount > attachmentLimit)
            {
                ruleIds.Add(isPc
                    ? "basic.spine.attachmentcount800"
                    : "basic.spine.attachmentcount400");
            }

            if (metrics.MeshAttachmentCount > meshAttachmentLimit)
            {
                ruleIds.Add(isPc
                    ? "basic.spine.meshattachmentcount100"
                    : "basic.spine.meshattachmentcount50");
            }

            return ruleIds;
        }

        private static async Task<AssetScanData> ScanSkeleton(
            string profileId,
            string skeletonPath = SkeletonPath)
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

        private static HashSet<string> GetThresholdRuleIds(
            AssetScanData scanData,
            string skeletonPath = SkeletonPath)
        {
            Assert.That(scanData, Is.Not.Null);
            List<AssetIssueResult> issues = scanData.GetIssues(skeletonPath);
            HashSet<string> ruleIds = new HashSet<string>();

            for (int i = 0; i < issues.Count; i++)
            {
                AssetIssueResult issue = issues[i];

                if (issue != null && issue.RuleConfig != null &&
                    ThresholdRuleIds.Contains(issue.RuleConfig.RuleId))
                {
                    ruleIds.Add(issue.RuleConfig.RuleId);
                }
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

        private struct SkeletonMetrics
        {
            public readonly int SlotCount;
            public readonly int AttachmentCount;
            public readonly int MeshAttachmentCount;

            public SkeletonMetrics(
                int slotCount,
                int attachmentCount,
                int meshAttachmentCount)
            {
                SlotCount = slotCount;
                AttachmentCount = attachmentCount;
                MeshAttachmentCount = meshAttachmentCount;
            }
        }
    }
}
