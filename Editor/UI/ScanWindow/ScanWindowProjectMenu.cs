using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 提供 Project 面板中打开资源评分窗口的菜单入口。
    /// </summary>
    internal static class ScanWindowProjectMenu
    {
        private const string ScanSelectedMenuPath =
            "Assets/Asset Score Tool/扫描选中资源";

        /// <summary>
        /// 使用当前 Project 选择项打开扫描窗口，并将资源路径交给窗口处理。
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

            ScanWindow window = EditorWindow.GetWindow<ScanWindow>();
            window.titleContent = new GUIContent("ScanWindow");
            window.SetScanPath(selectedPath);
            window.Show();
        }

        /// <summary>
        /// 仅在当前选择项可转换为项目 Assets 路径时启用菜单。
        /// </summary>
        [MenuItem(ScanSelectedMenuPath, true)]
        private static bool ValidateOpenFromProjectSelection()
        {
            return !string.IsNullOrEmpty(GetSelectedAssetPath());
        }

        /// <summary>
        /// 获取当前 Project 选择项对应的规范化 Unity 资源路径。
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

            return assetPath.StartsWith("Assets/") || assetPath == "Assets"
                ? assetPath
                : string.Empty;
        }
    }
}
