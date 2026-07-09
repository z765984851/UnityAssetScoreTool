using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    [CreateAssetMenu(
        fileName = "AssetTypeConfig",
        menuName = "Catfood Tools/Asset Score Tool/创建资源类型")]
    public class AssetTypeConfig : ScriptableObject
    {
        [SerializeField] private string assetTypeId;
        [SerializeField] private string displayName;

        /// <summary>
        /// 资源类型唯一标识。
        /// </summary>
        public string AssetTypeId
        {
            get { return assetTypeId; }
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
