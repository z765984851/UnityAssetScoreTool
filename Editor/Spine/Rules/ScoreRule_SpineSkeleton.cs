using Spine;
using Spine.Unity;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 检查 Spine SkeletonDataAsset 配置的 Atlas 数量是否超过 1。
    /// </summary>
    public class ScoreRule_SpineAtlasCount1 : SpineRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.spine.atlascount1"; }
        }

        public override AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig)
        {
            SkeletonDataAsset skeletonDataAsset;
            return GetIssueIfViolated(
                ruleConfig,
                SpineRuleUtility.TryLoadSkeleton(
                    context,
                    out skeletonDataAsset) &&
                SpineRuleUtility.GetAtlasCount(skeletonDataAsset) > 1);
        }
    }

    /// <summary>
    /// 为 Spine SkeletonDataAsset 的数值统计规则提供统一实现。
    /// </summary>
    public abstract class SpineSkeletonCountRuleCheckerBase
        : SpineRuleCheckerBase
    {
        protected abstract int MaximumCount { get; }

        protected abstract int GetCount(SkeletonData skeletonData);

        public override AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig)
        {
            SkeletonDataAsset skeletonDataAsset;
            SkeletonData skeletonData;

            return GetIssueIfViolated(
                ruleConfig,
                SpineRuleUtility.TryLoadSkeleton(
                    context,
                    out skeletonDataAsset) &&
                SpineRuleUtility.TryGetSkeletonData(
                    skeletonDataAsset,
                    out skeletonData) &&
                GetCount(skeletonData) > MaximumCount);
        }
    }

    /// <summary>
    /// 检查 Spine Slot 数量是否超过 400。
    /// </summary>
    public class ScoreRule_SpineSlotCount400
        : SpineSkeletonCountRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.spine.slotcount400"; }
        }

        protected override int MaximumCount { get { return 400; } }

        protected override int GetCount(SkeletonData skeletonData)
        {
            return skeletonData.Slots == null ? 0 : skeletonData.Slots.Count;
        }
    }

    /// <summary>
    /// 检查 Spine Slot 数量是否超过 200。
    /// </summary>
    public class ScoreRule_SpineSlotCount200
        : SpineSkeletonCountRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.spine.slotcount200"; }
        }

        protected override int MaximumCount { get { return 200; } }

        protected override int GetCount(SkeletonData skeletonData)
        {
            return skeletonData.Slots == null ? 0 : skeletonData.Slots.Count;
        }
    }

    /// <summary>
    /// 为 Spine Attachment 数量统计规则提供统一实现。
    /// </summary>
    public abstract class SpineAttachmentCountRuleCheckerBase
        : SpineSkeletonCountRuleCheckerBase
    {
        protected override int GetCount(SkeletonData skeletonData)
        {
            int attachmentCount;
            int meshAttachmentCount;
            SpineRuleUtility.GetAttachmentCounts(
                skeletonData,
                out attachmentCount,
                out meshAttachmentCount);
            return attachmentCount;
        }
    }

    /// <summary>
    /// 检查 Spine Attachment 数量是否超过 800。
    /// </summary>
    public class ScoreRule_SpineAttachmentCount800
        : SpineAttachmentCountRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.spine.attachmentcount800"; }
        }

        protected override int MaximumCount { get { return 800; } }
    }

    /// <summary>
    /// 检查 Spine Attachment 数量是否超过 400。
    /// </summary>
    public class ScoreRule_SpineAttachmentCount400
        : SpineAttachmentCountRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.spine.attachmentcount400"; }
        }

        protected override int MaximumCount { get { return 400; } }
    }

    /// <summary>
    /// 为 Spine MeshAttachment 数量统计规则提供统一实现。
    /// </summary>
    public abstract class SpineMeshAttachmentCountRuleCheckerBase
        : SpineSkeletonCountRuleCheckerBase
    {
        protected override int GetCount(SkeletonData skeletonData)
        {
            int attachmentCount;
            int meshAttachmentCount;
            SpineRuleUtility.GetAttachmentCounts(
                skeletonData,
                out attachmentCount,
                out meshAttachmentCount);
            return meshAttachmentCount;
        }
    }

    /// <summary>
    /// 检查 Spine MeshAttachment 数量是否超过 100。
    /// </summary>
    public class ScoreRule_SpineMeshAttachmentCount100
        : SpineMeshAttachmentCountRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.spine.meshattachmentcount100"; }
        }

        protected override int MaximumCount { get { return 100; } }
    }

    /// <summary>
    /// 检查 Spine MeshAttachment 数量是否超过 50。
    /// </summary>
    public class ScoreRule_SpineMeshAttachmentCount50
        : SpineMeshAttachmentCountRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.spine.meshattachmentcount50"; }
        }

        protected override int MaximumCount { get { return 50; } }
    }

    /// <summary>
    /// 检查 Spine SkeletonDataAsset 是否存在缺失或无法解析的依赖。
    /// </summary>
    public class ScoreRule_SpineMissingAsset : SpineRuleCheckerBase
    {
        protected override string RuleIdValue
        {
            get { return "basic.spine.missingasset"; }
        }

        public override AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig)
        {
            SkeletonDataAsset skeletonDataAsset;
            return GetIssueIfViolated(
                ruleConfig,
                SpineRuleUtility.TryLoadSkeleton(
                    context,
                    out skeletonDataAsset) &&
                SpineRuleUtility.HasMissingDependencies(skeletonDataAsset));
        }
    }
}
