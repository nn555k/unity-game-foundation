using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI.Editor
{
    public static class FigmaPrefabNormalizer
    {
        /// <summary>
        /// 在不保存资源的前提下列出结构规范化动作和当前校验结果。
        /// </summary>
        public static UiPrefabNormalizationReport Preview(
            string prefabPath,
            UiPrefabConventionProfile profile,
            UiPrefabKind kind)
        {
            return Normalize(prefabPath, profile, kind, false);
        }

        /// <summary>
        /// 通过 PrefabUtility 修改并保存单个 Prefab，不直接编辑 YAML。
        /// </summary>
        public static UiPrefabNormalizationReport Apply(
            string prefabPath,
            UiPrefabConventionProfile profile,
            UiPrefabKind kind)
        {
            return Normalize(prefabPath, profile, kind, true);
        }

        /// <summary>
        /// 复用同一套规划逻辑执行预览或真实修改，并在结束时重新校验。
        /// </summary>
        private static UiPrefabNormalizationReport Normalize(
            string prefabPath,
            UiPrefabConventionProfile profile,
            UiPrefabKind kind,
            bool apply)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var rule = profile.FindRule(kind);
            if (rule == null)
            {
                throw new InvalidOperationException($"No convention rule is configured for {kind}.");
            }

            var report = new UiPrefabNormalizationReport(prefabPath, kind);
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                NormalizeRootName(root, prefabPath, report, apply);
                var content = EnsureContentRoot(root.transform, rule, report, apply);
                EnsureDimBackground(root.transform, profile, rule, report, apply);
                NormalizeRootChildren(root.transform, content, rule, report, apply);
                if (apply)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            report.Validation = UiPrefabValidator.Validate(prefabPath, profile, kind);
            return report;
        }

        /// <summary>
        /// 使根节点名称与 Prefab 文件名一致，同时保持资源 GUID 不变。
        /// </summary>
        private static void NormalizeRootName(
            GameObject root,
            string prefabPath,
            UiPrefabNormalizationReport report,
            bool apply)
        {
            var expectedName = Path.GetFileNameWithoutExtension(prefabPath);
            if (string.Equals(root.name, expectedName, StringComparison.Ordinal))
            {
                return;
            }

            report.AddAction($"Rename root '{root.name}' -> '{expectedName}'.");
            if (apply)
            {
                root.name = expectedName;
            }
        }

        /// <summary>
        /// 复用规定内容根；Popup 遗留 Container 可安全重命名，其他缺失情况新建全屏根。
        /// </summary>
        private static Transform EnsureContentRoot(
            Transform root,
            UiPrefabRule rule,
            UiPrefabNormalizationReport report,
            bool apply)
        {
            if (string.IsNullOrEmpty(rule.ContentRootName))
            {
                return null;
            }

            var content = UiPrefabValidator.FindDirectChild(root, rule.ContentRootName);
            if (content)
            {
                return content;
            }

            var legacyContainer = UiPrefabValidator.FindDirectChild(root, "Container");
            if (legacyContainer && string.Equals(rule.ContentRootName, "PopupContent", StringComparison.Ordinal))
            {
                report.AddAction($"Rename root child 'Container' -> '{rule.ContentRootName}'.");
                if (apply)
                {
                    legacyContainer.name = rule.ContentRootName;
                }

                return legacyContainer;
            }

            report.AddAction($"Create stretched root child '{rule.ContentRootName}'.");
            if (!apply)
            {
                return null;
            }

            var contentObject = new GameObject(rule.ContentRootName, typeof(RectTransform));
            var contentRect = (RectTransform)contentObject.transform;
            contentRect.SetParent(root, false);
            Stretch(contentRect);
            return contentRect;
        }

        /// <summary>
        /// 创建或修正弹窗遮罩的层级、尺寸、颜色与射线配置。
        /// </summary>
        private static void EnsureDimBackground(
            Transform root,
            UiPrefabConventionProfile profile,
            UiPrefabRule rule,
            UiPrefabNormalizationReport report,
            bool apply)
        {
            if (!rule.RequireDimBackground)
            {
                return;
            }

            var dim = UiPrefabValidator.FindDirectChild(root, "DimBackground");
            if (!dim)
            {
                report.AddAction("Create full-screen raycast DimBackground.");
                if (!apply)
                {
                    return;
                }

                var dimObject = new GameObject("DimBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                dim = dimObject.transform;
                dim.SetParent(root, false);
            }
            else
            {
                report.AddAction("Normalize DimBackground stretch, color, raycast, and sibling order.");
            }

            if (!apply)
            {
                return;
            }

            var rect = dim as RectTransform;
            if (!rect)
            {
                throw new InvalidOperationException("DimBackground must use RectTransform before it can be normalized.");
            }

            Stretch(rect);
            var image = dim.GetComponent<Image>();
            if (!image)
            {
                image = dim.gameObject.AddComponent<Image>();
            }

            image.color = new Color(0f, 0f, 0f, profile.DimBackgroundAlpha);
            image.raycastTarget = true;
            dim.SetSiblingIndex(0);
        }

        /// <summary>
        /// 将未声明的根级视觉节点移动到内容根，保留可选项目扩展节点。
        /// </summary>
        private static void NormalizeRootChildren(
            Transform root,
            Transform content,
            UiPrefabRule rule,
            UiPrefabNormalizationReport report,
            bool apply)
        {
            if (!rule.MoveUnknownRootChildrenIntoContent || string.IsNullOrEmpty(rule.ContentRootName))
            {
                return;
            }

            var allowed = new HashSet<string>(StringComparer.Ordinal)
            {
                rule.ContentRootName
            };
            if (rule.RequireDimBackground)
            {
                allowed.Add("DimBackground");
            }

            if (rule.OptionalRootChildren != null)
            {
                for (var index = 0; index < rule.OptionalRootChildren.Length; index++)
                {
                    allowed.Add(rule.OptionalRootChildren[index]);
                }
            }

            var toMove = new List<Transform>();
            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (!allowed.Contains(child.name))
                {
                    toMove.Add(child);
                }
            }

            for (var index = 0; index < toMove.Count; index++)
            {
                var child = toMove[index];
                report.AddAction($"Move '{child.name}' under '{rule.ContentRootName}'.");
                if (apply)
                {
                    child.SetParent(content, false);
                }
            }

            if (apply && content)
            {
                content.SetAsLastSibling();
            }
        }

        /// <summary>
        /// 将 RectTransform 设置为父节点全屏拉伸且偏移归零。
        /// </summary>
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
