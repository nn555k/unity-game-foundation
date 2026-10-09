using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameFoundation.UI.Editor
{
    public static class UiPrefabConventionLocator
    {
        /// <summary>
        /// 加载路径排序后的首个项目 Profile；不存在时返回内存默认规则。
        /// </summary>
        public static UiPrefabConventionProfile LoadActiveProfile()
        {
            var path = AssetDatabase.FindAssets("t:UiPrefabConventionProfile")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(assetPath => assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                .OrderBy(assetPath => assetPath, StringComparer.Ordinal)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(path))
            {
                return AssetDatabase.LoadAssetAtPath<UiPrefabConventionProfile>(path);
            }

            var profile = ScriptableObject.CreateInstance<UiPrefabConventionProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;
            return profile;
        }

        /// <summary>
        /// 根据资源根节点后缀推断规则类型，无法识别时按 Component 处理。
        /// </summary>
        public static UiPrefabKind InferKind(GameObject prefabAsset)
        {
            if (!prefabAsset)
            {
                return UiPrefabKind.Component;
            }

            var name = prefabAsset.name;
            if (name.EndsWith("Popup", StringComparison.Ordinal))
            {
                return UiPrefabKind.Popup;
            }

            if (name.EndsWith("Page", StringComparison.Ordinal))
            {
                return UiPrefabKind.Page;
            }

            if (name.EndsWith("Hud", StringComparison.Ordinal) || name.EndsWith("HUD", StringComparison.Ordinal))
            {
                return UiPrefabKind.Hud;
            }

            if (name.EndsWith("Overlay", StringComparison.Ordinal))
            {
                return UiPrefabKind.Overlay;
            }

            if (name.EndsWith("Item", StringComparison.Ordinal) || name.EndsWith("Row", StringComparison.Ordinal))
            {
                return UiPrefabKind.ListItem;
            }

            return UiPrefabKind.Component;
        }
    }
}
