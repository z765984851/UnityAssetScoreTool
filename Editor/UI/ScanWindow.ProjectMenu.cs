using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    public partial class ScanWindow
    {
        private const string ScanSelectedMenuPath = "Assets/Asset Score Tool/扫描选中资源";

        /// <summary>
        /// Project 面板右键资源或文件夹时，打开扫描窗口并自动填入选中路径。
        /// </summary>
        [MenuItem(ScanSelectedMenuPath)]
        private static void OpenFromProjectSelection()
        {
            string selectedPath = GetSelectedAssetPath();

            if (string.IsNullOrEmpty(selectedPath))
            {
                EditorUtility.DisplayDialog(
                    "Asset Score Tool",
                    "请先在 Project 面板中选择一个资源或文件夹。",
                    "确定");
                return;
            }

            ScanWindow window = GetWindow<ScanWindow>();
            window.titleContent = new GUIContent("ScanWindow");
            window.SetScanPath(selectedPath);
            window.Show();
        }

        /// <summary>
        /// 控制右键菜单是否可用。
        /// 只有选中的对象能转换成 Assets 路径时，菜单才可点击。
        /// </summary>
        [MenuItem(ScanSelectedMenuPath, true)]
        private static bool ValidateOpenFromProjectSelection()
        {
            return !string.IsNullOrEmpty(GetSelectedAssetPath());
        }

        /// <summary>
        /// 获取 Project 面板当前选中的资源或文件夹路径。
        /// 返回值必须是 Unity 资源路径，例如 Assets/Art/Hero.prefab。
        /// </summary>
        private static string GetSelectedAssetPath()
        {
            Object selectedObject = Selection.activeObject;

            if (selectedObject == null)
            {
                return string.Empty;
            }

            string assetPath = AssetDatabase.GetAssetPath(selectedObject);

            if (string.IsNullOrEmpty(assetPath))
            {
                return string.Empty;
            }

            assetPath = ScanToolUtility.NormalizePath(assetPath);

            if (!assetPath.StartsWith("Assets/") && assetPath != "Assets")
            {
                return string.Empty;
            }

            return assetPath;
        }
    }
}