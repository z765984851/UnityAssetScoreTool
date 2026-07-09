namespace majsoul.tools.assetscoretool.editor
{
    public interface IAssetTypeChecker
    {
        /// <summary>
        /// 资源类型唯一标识。
        /// 和 AssetTypeConfig.AssetTypeId 对应。
        /// </summary>
        string AssetTypeId { get; }

        /// <summary>
        /// 判断当前资源是否属于该资源类型。
        /// </summary>
        bool IsMatch(string assetPath);
    }
}
