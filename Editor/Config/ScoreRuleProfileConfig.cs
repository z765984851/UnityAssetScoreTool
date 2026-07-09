using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    [CreateAssetMenu(
        fileName = "ScoreRuleProfileConfig",
        menuName = "Catfood Tools/Asset Score Tool/创建检测配置档")]
    public class ScoreRuleProfileConfig : ScriptableObject
    {
        [SerializeField] private string profileId;

        [SerializeField] private string displayName;

        /// <summary>
        /// 检测配置档唯一标识。
        /// </summary>
        public string ProfileId
        {
            get { return profileId; }
        }

        /// <summary>
        /// 编辑器中显示的名称。
        /// </summary>
        public string DisplayName
        {
            get { return displayName; }
        }
    }
}
