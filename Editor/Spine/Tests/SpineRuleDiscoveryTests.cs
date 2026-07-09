using System.Collections.Generic;
using NUnit.Framework;
using majsoul.tools.assetscoretool.editor;

namespace majsoul.tools.assetscoretool.editor.spine.tests
{
    /// <summary>
    /// 验证启用 Spine 时所有 Spine 规则检测器均能被扫描器发现。
    /// </summary>
    public class SpineRuleDiscoveryTests
    {
        [Test]
        public void AllSpineRuleCheckersAreDiscoverable()
        {
            Dictionary<string, IScoreRuleChecker> checkers =
                ScanToolUtility.GetAllRuleCheckers();

            CollectionAssert.IsSubsetOf(new[]
            {
                "basic.spine.atlascount1",
                "basic.spine.texturesize4096",
                "basic.spine.materialcount4",
                "basic.spine.materialcount2",
                "basic.spine.slotcount400",
                "basic.spine.slotcount200",
                "basic.spine.attachmentcount800",
                "basic.spine.attachmentcount400",
                "basic.spine.meshattachmentcount100",
                "basic.spine.meshattachmentcount50",
                "basic.spine.missingasset"
            }, checkers.Keys);
        }
    }
}