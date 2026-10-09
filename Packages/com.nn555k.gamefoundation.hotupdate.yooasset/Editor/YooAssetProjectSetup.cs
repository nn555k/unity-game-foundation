using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using YooAsset.Editor;

namespace GameFoundation.HotUpdate.Providers.YooAsset.Editor
{
    public static class YooAssetProjectSetup
    {
        /// <summary>Creates local collector defaults once; existing project packages and rules are preserved.</summary>
        public static string Prepare(string packageName, string contentRoot)
        {
            ValidateArguments(packageName, contentRoot);
            var settings = LoadSettings();
            Undo.RecordObject(settings, "Initialize YooAsset project defaults");
            if (settings.Packages.Count == 0)
            {
                EnsureFolder(contentRoot);
                ConfigureEmpty(settings, packageName, contentRoot);
            }
            settings.ShowPackageView = true;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            return Validate(packageName, contentRoot);
        }

        /// <summary>Checks settings and real UXML controls so an installed but unusable editor cannot pass readiness.</summary>
        public static string Validate(string packageName, string contentRoot)
        {
            ValidateArguments(packageName, contentRoot);
            var paths = AssetDatabase.FindAssets("t:AssetBundleCollectorSetting");
            if (paths.Length != 1) throw new InvalidOperationException("Expected one YooAsset collector setting; found " + paths.Length);
            var settings = AssetDatabase.LoadAssetAtPath<AssetBundleCollectorSetting>(AssetDatabase.GUIDToAssetPath(paths[0]));
            if (!settings.ShowPackageView || settings.Packages.Count == 0)
                throw new InvalidOperationException("YooAsset has no visible packages. Run Project Setup to initialize collector defaults.");
            settings.CheckAllPackageConfigError();
            var tree = UxmlLoader.LoadWindowUXML<AssetBundleCollectorWindow>().CloneTree();
            if (tree.Q<Toggle>("ShowPackages") == null || tree.Q<ListView>("PackageListView") == null
                || tree.Q<VisualElement>("PackageAddContainer")?.Q<Button>("AddBtn") == null)
                throw new InvalidOperationException("YooAsset Collector UXML is missing essential controls.");
            if (UxmlLoader.LoadWindowUXML<AssetBundleBuilderWindow>().CloneTree().Q("Container") == null)
                throw new InvalidOperationException("YooAsset Builder UXML is missing its content container.");
            return "Collector/Builder layouts ready; visible packages: " + string.Join(", ", settings.Packages.Select(package => package.PackageName));
        }

        /// <summary>Populates only an empty configuration; reruns cannot duplicate or rewrite project collection rules.</summary>
        public static void ConfigureEmpty(AssetBundleCollectorSetting settings, string packageName, string contentRoot)
        {
            ValidateArguments(packageName, contentRoot);
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settings.Packages.Count > 0) return;
            var package = new AssetBundleCollectorPackage { PackageName = packageName };
            var group = new AssetBundleCollectorGroup { GroupName = "Base" };
            group.Collectors.Add(new AssetBundleCollector
            {
                CollectPath = contentRoot,
                CollectorGUID = AssetDatabase.AssetPathToGUID(contentRoot),
                AddressRuleName = nameof(AddressDisable),
                PackRuleName = nameof(PackDirectory),
                FilterRuleName = nameof(CollectAll)
            });
            package.Groups.Add(group);
            settings.Packages.Add(package);
            settings.ShowPackageView = true;
        }

        /// <summary>Creates a project-owned settings asset through Unity APIs, rejecting ambiguous existing configurations.</summary>
        private static AssetBundleCollectorSetting LoadSettings()
        {
            var guids = AssetDatabase.FindAssets("t:AssetBundleCollectorSetting");
            if (guids.Length > 1) throw new InvalidOperationException("Multiple YooAsset collector settings found; choose the project-owned asset first.");
            if (guids.Length == 1) return AssetDatabase.LoadAssetAtPath<AssetBundleCollectorSetting>(AssetDatabase.GUIDToAssetPath(guids[0]));
            EnsureFolder("Assets/Settings/GameFoundation");
            var settings = ScriptableObject.CreateInstance<AssetBundleCollectorSetting>();
            AssetDatabase.CreateAsset(settings, "Assets/Settings/GameFoundation/AssetBundleCollectorSetting.asset");
            return settings;
        }

        /// <summary>Rejects unsafe or overly broad collection roots before creating any assets.</summary>
        private static void ValidateArguments(string packageName, string contentRoot)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(packageName ?? string.Empty, @"^[A-Za-z_][A-Za-z0-9_]*$"))
                throw new ArgumentException("Package name must be a valid identifier.");
            if (string.IsNullOrEmpty(contentRoot) || !contentRoot.StartsWith("Assets/") || contentRoot.Contains("..")
                || contentRoot.Contains(":") || contentRoot.Contains("\\")
                || contentRoot.Split('/').Any(part => part == "Resources" || part == "." || part.Length == 0))
                throw new ArgumentException("Content root must be a dedicated Assets folder outside Resources.");
        }

        /// <summary>Creates missing asset folders without moving or replacing existing project content.</summary>
        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }
    }
}
