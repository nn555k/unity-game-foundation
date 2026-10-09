using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI.Editor
{
    public static class UiPrefabValidator
    {
        private static readonly Regex GeneratedFigmaName = new Regex(
            @"^(Frame|Group|Rectangle|Ellipse|Vector|Instance|Text)[ _-]*\d+$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// 加载 Prefab 内容并按指定 Profile 和类型完成只读校验。
        /// </summary>
        public static UiPrefabValidationReport Validate(
            string prefabPath,
            UiPrefabConventionProfile profile,
            UiPrefabKind kind)
        {
            var report = new UiPrefabValidationReport(prefabPath, kind);
            if (profile == null)
            {
                report.Add(UiPrefabValidationSeverity.Error, string.Empty, "Convention profile is missing.");
                return report;
            }

            if (string.IsNullOrWhiteSpace(prefabPath) || !prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                report.Add(UiPrefabValidationSeverity.Error, prefabPath, "Target must be a prefab asset path.");
                return report;
            }

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                ValidateLoadedRoot(root, prefabPath, profile, kind, report);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return report;
        }

        /// <summary>
        /// 校验已加载根节点，供规范化工具保存前后复用同一套判定逻辑。
        /// </summary>
        internal static void ValidateLoadedRoot(
            GameObject root,
            string prefabPath,
            UiPrefabConventionProfile profile,
            UiPrefabKind kind,
            UiPrefabValidationReport report)
        {
            if (!root)
            {
                report.Add(UiPrefabValidationSeverity.Error, prefabPath, "Prefab root could not be loaded.");
                return;
            }

            var rule = profile.FindRule(kind);
            if (rule == null)
            {
                report.Add(UiPrefabValidationSeverity.Error, root.name, $"No convention rule is configured for {kind}.");
                return;
            }

            ValidateRootName(root, prefabPath, rule, report);
            ValidateRuleStructure(root.transform, profile, rule, report);
            ValidateNames(root.transform, profile, report);
        }

        /// <summary>
        /// 校验 Prefab 文件名、根节点名和类型后缀的一致性。
        /// </summary>
        private static void ValidateRootName(
            GameObject root,
            string prefabPath,
            UiPrefabRule rule,
            UiPrefabValidationReport report)
        {
            var fileName = Path.GetFileNameWithoutExtension(prefabPath);
            if (!string.Equals(root.name, fileName, StringComparison.Ordinal))
            {
                report.Add(UiPrefabValidationSeverity.Error, root.name, $"Root name must match prefab file name '{fileName}'.");
            }

            if (!string.IsNullOrEmpty(rule.RootSuffix) && !root.name.EndsWith(rule.RootSuffix, StringComparison.Ordinal))
            {
                report.Add(UiPrefabValidationSeverity.Error, root.name, $"Root name must end with '{rule.RootSuffix}'.");
            }
        }

        /// <summary>
        /// 校验必需根节点、弹窗遮罩属性和未声明的根级节点。
        /// </summary>
        private static void ValidateRuleStructure(
            Transform root,
            UiPrefabConventionProfile profile,
            UiPrefabRule rule,
            UiPrefabValidationReport report)
        {
            var allowedRootNames = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrEmpty(rule.ContentRootName))
            {
                allowedRootNames.Add(rule.ContentRootName);
                if (!FindDirectChild(root, rule.ContentRootName))
                {
                    report.Add(UiPrefabValidationSeverity.Error, root.name, $"Missing required root child '{rule.ContentRootName}'.");
                }
            }

            if (rule.RequireDimBackground)
            {
                allowedRootNames.Add("DimBackground");
                ValidateDimBackground(root, profile, report);
            }

            if (rule.OptionalRootChildren != null)
            {
                for (var index = 0; index < rule.OptionalRootChildren.Length; index++)
                {
                    if (!string.IsNullOrWhiteSpace(rule.OptionalRootChildren[index]))
                    {
                        allowedRootNames.Add(rule.OptionalRootChildren[index]);
                    }
                }
            }

            if (!rule.MoveUnknownRootChildrenIntoContent || string.IsNullOrEmpty(rule.ContentRootName))
            {
                return;
            }

            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (!allowedRootNames.Contains(child.name))
                {
                    report.Add(UiPrefabValidationSeverity.Error, PathOf(child), $"Root child must move under '{rule.ContentRootName}' or be declared optional.");
                }
            }
        }

        /// <summary>
        /// 校验遮罩铺满、颜色、透明度和射线阻挡要求。
        /// </summary>
        private static void ValidateDimBackground(
            Transform root,
            UiPrefabConventionProfile profile,
            UiPrefabValidationReport report)
        {
            var dim = FindDirectChild(root, "DimBackground");
            if (!dim)
            {
                report.Add(UiPrefabValidationSeverity.Error, root.name, "Missing required root child 'DimBackground'.");
                return;
            }

            var rect = dim as RectTransform;
            if (!rect)
            {
                report.Add(UiPrefabValidationSeverity.Error, PathOf(dim), "DimBackground must use RectTransform.");
            }
            else if (!Approximately(rect.anchorMin, Vector2.zero) ||
                     !Approximately(rect.anchorMax, Vector2.one) ||
                     !Approximately(rect.offsetMin, Vector2.zero) ||
                     !Approximately(rect.offsetMax, Vector2.zero))
            {
                report.Add(UiPrefabValidationSeverity.Error, PathOf(dim), "DimBackground must stretch to the full parent rect with zero offsets.");
            }

            var image = dim.GetComponent<Image>();
            if (!image)
            {
                report.Add(UiPrefabValidationSeverity.Error, PathOf(dim), "DimBackground requires an Image component.");
                return;
            }

            if (!image.raycastTarget)
            {
                report.Add(UiPrefabValidationSeverity.Error, PathOf(dim), "DimBackground Image must block raycasts.");
            }

            var color = image.color;
            if (!Mathf.Approximately(color.r, 0f) || !Mathf.Approximately(color.g, 0f) || !Mathf.Approximately(color.b, 0f) ||
                Mathf.Abs(color.a - profile.DimBackgroundAlpha) > 0.005f)
            {
                report.Add(UiPrefabValidationSeverity.Error, PathOf(dim), $"DimBackground color must be black with alpha {profile.DimBackgroundAlpha:0.###}.");
            }
        }

        /// <summary>
        /// 递归校验同级重名和未经语义命名的 Figma 默认节点。
        /// </summary>
        private static void ValidateNames(
            Transform root,
            UiPrefabConventionProfile profile,
            UiPrefabValidationReport report)
        {
            if (profile.RejectGeneratedFigmaNames && GeneratedFigmaName.IsMatch(root.name))
            {
                report.Add(UiPrefabValidationSeverity.Error, PathOf(root), "Replace generated Figma name with a semantic PascalCase name.");
            }

            var siblingNames = profile.RequireUniqueSiblingNames
                ? new HashSet<string>(StringComparer.Ordinal)
                : null;
            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (siblingNames != null && !siblingNames.Add(child.name))
                {
                    report.Add(UiPrefabValidationSeverity.Error, PathOf(child), "Sibling names must be unique.");
                }

                ValidateNames(child, profile, report);
            }
        }

        /// <summary>
        /// 查找指定名称的直接子节点，不跨越结构边界匹配深层节点。
        /// </summary>
        internal static Transform FindDirectChild(Transform parent, string name)
        {
            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>
        /// 构建不依赖场景名称的层级路径用于报告定位。
        /// </summary>
        internal static string PathOf(Transform transform)
        {
            if (!transform)
            {
                return string.Empty;
            }

            var path = transform.name;
            var current = transform.parent;
            while (current)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        /// <summary>
        /// 使用适合序列化浮点误差的阈值比较 UI 二维值。
        /// </summary>
        private static bool Approximately(Vector2 left, Vector2 right)
        {
            return (left - right).sqrMagnitude <= 0.000001f;
        }
    }
}
