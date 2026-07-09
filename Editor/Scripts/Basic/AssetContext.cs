using System.Collections.Generic;

namespace majsoul.tools.assetscoretool.editor
{
    public class AssetContext
    {
        /// <summary>
        /// 资源路径，例如 Assets/Textures/icon.png。
        /// </summary>
        public string AssetPath { get; private set; }

        public List<AssetTypeConfig> AssetTypes { get; private set; }

        /// <summary>
        /// 当前资源直接依赖的资源。
        /// </summary>
        public List<AssetContext> Dependencies { get; private set; }

        public AssetContext(string assetPath)
        {
            AssetPath = assetPath;
            Dependencies = new List<AssetContext>();
            AssetTypes = new List<AssetTypeConfig>();
        }

        public void AddDependency(AssetContext dependency)
        {
            if (dependency == null)
            {
                return;
            }

            if (Dependencies.Contains(dependency))
            {
                return;
            }

            Dependencies.Add(dependency);
        }


        public void SetAssetTypes(List<AssetTypeConfig> assetTypes)
        {
            if (assetTypes == null)
            {
                AssetTypes = new List<AssetTypeConfig>();
                return;
            }

            AssetTypes = assetTypes;
        }
    }
}