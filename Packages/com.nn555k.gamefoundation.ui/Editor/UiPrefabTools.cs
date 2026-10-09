using UnityEditor;
using UnityEngine;

namespace GameFoundation.UI.Editor
{
    public static class UiPrefabTools
    {
        /// <summary>
        /// 校验 Project 视图当前选中的 Prefab 并把机器可读报告写入 Console。
        /// </summary>
        [MenuItem("Game Foundation/UI/Validate Selected Prefab")]
        public static void ValidateSelectedPrefab()
        {
            if (!TryResolveSelection(out var path, out var prefab))
            {
                return;
            }

            var profile = UiPrefabConventionLocator.LoadActiveProfile();
            var kind = UiPrefabConventionLocator.InferKind(prefab);
            var report = UiPrefabValidator.Validate(path, profile, kind);
            LogReport(report.ToMultilineString(), report.IsValid, prefab);
        }

        /// <summary>
        /// 预览选中 Prefab 的结构调整，不保存任何修改。
        /// </summary>
        [MenuItem("Game Foundation/UI/Preview Selected Prefab Normalization")]
        public static void PreviewSelectedPrefabNormalization()
        {
            if (!TryResolveSelection(out var path, out var prefab))
            {
                return;
            }

            var profile = UiPrefabConventionLocator.LoadActiveProfile();
            var kind = UiPrefabConventionLocator.InferKind(prefab);
            var report = FigmaPrefabNormalizer.Preview(path, profile, kind);
            Debug.Log(report.ToMultilineString(), prefab);
        }

        /// <summary>
        /// 经用户确认后规范化选中 Prefab，AI 自动化应直接调用无弹窗的 Apply API。
        /// </summary>
        [MenuItem("Game Foundation/UI/Normalize Selected Prefab")]
        public static void NormalizeSelectedPrefab()
        {
            if (!TryResolveSelection(out var path, out var prefab))
            {
                return;
            }

            var profile = UiPrefabConventionLocator.LoadActiveProfile();
            var kind = UiPrefabConventionLocator.InferKind(prefab);
            var preview = FigmaPrefabNormalizer.Preview(path, profile, kind);
            if (!EditorUtility.DisplayDialog(
                    "Normalize UI Prefab",
                    preview.ToMultilineString(),
                    "Apply",
                    "Cancel"))
            {
                return;
            }

            var report = FigmaPrefabNormalizer.Apply(path, profile, kind);
            LogReport(report.ToMultilineString(), report.Validation != null && report.Validation.IsValid, prefab);
        }

        /// <summary>
        /// 解析 Project 视图选中的 Prefab 资源并拒绝场景实例或其他资源类型。
        /// </summary>
        private static bool TryResolveSelection(out string path, out GameObject prefab)
        {
            prefab = Selection.activeObject as GameObject;
            path = prefab ? AssetDatabase.GetAssetPath(prefab) : string.Empty;
            if (!prefab || string.IsNullOrEmpty(path) || !path.EndsWith(".prefab"))
            {
                Debug.LogError("Select one prefab asset in the Project window.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 根据报告结论选择普通日志或错误日志级别。
        /// </summary>
        private static void LogReport(string report, bool success, Object context)
        {
            if (success)
            {
                Debug.Log(report, context);
                return;
            }

            Debug.LogError(report, context);
        }
    }
}
