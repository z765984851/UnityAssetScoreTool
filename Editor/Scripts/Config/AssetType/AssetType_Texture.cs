using UnityEditor;

namespace majsoul.tools.assetscoretool.editor
{
    public class AssetType_Texture : IAssetTypeChecker
    {
        private const string AssetTypeIdValue = "texture";

        public string AssetTypeId
        {
            get { return AssetTypeIdValue; }
        }

        public bool IsMatch(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            return AssetImporter.GetAtPath(assetPath) is TextureImporter;
        }
    }
}