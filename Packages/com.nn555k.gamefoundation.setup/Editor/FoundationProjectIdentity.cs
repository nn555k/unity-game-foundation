using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GameFoundation.Setup.Editor
{
    internal static class FoundationProjectIdentity
    {
        /// <summary>Loads the saved identity; malformed contracts fail before any project files are written.</summary>
        internal static FoundationProjectSetupOptions Load(string root)
        {
            var path = Path.Combine(root, ".gamefoundation/project.json");
            if (!File.Exists(path)) return null;
            var data = JsonUtility.FromJson<FoundationProjectConfigurationData>(File.ReadAllText(path));
            if (data == null || string.IsNullOrWhiteSpace(data.projectName)
                || string.IsNullOrWhiteSpace(data.rootNamespace) || string.IsNullOrWhiteSpace(data.codeRoot))
                throw new InvalidOperationException("Invalid .gamefoundation/project.json; repair the existing identity before Setup.");
            var options = new FoundationProjectSetupOptions
            {
                ProjectName = data.projectName, RootNamespace = data.rootNamespace, CodeRoot = data.codeRoot,
                CreateUiConventionProfile = !string.IsNullOrEmpty(data.uiConventionProfile),
                ContentPackageName = string.IsNullOrEmpty(data.contentPackageName) ? "DefaultPackage" : data.contentPackageName,
                ContentRoot = string.IsNullOrEmpty(data.contentRoot) ? "Assets/GameContent" : data.contentRoot
            };
            var appPath = Path.Combine(root, data.architectureFile ?? string.Empty);
            if (File.Exists(appPath))
            {
                var source = File.ReadAllText(appPath);
                options.IncludeSave = source.Contains("FoundationSaveModule.Register");
                options.IncludeHotUpdate = source.Contains("FoundationHotUpdateModule.Register");
                options.IncludeSdk = source.Contains("FoundationSdkModule.Register");
                options.IncludeUi = source.Contains("FoundationUiModule.Register");
            }
            options.CreateAssemblyDefinition = File.Exists(Path.Combine(root, data.codeRoot, data.rootNamespace + ".Game.asmdef"));
            return options;
        }

        /// <summary>Rejects identity changes and duplicate architectures before scaffolding, even with overwrite enabled.</summary>
        internal static void Validate(string root, FoundationProjectSetupOptions options, FoundationProjectSetupReport report)
        {
            try
            {
                var existing = Load(root);
                if (existing != null && (existing.ProjectName != options.ProjectName
                    || existing.RootNamespace != options.RootNamespace || existing.CodeRoot != options.CodeRoot
                    || existing.ContentPackageName != options.ContentPackageName || existing.ContentRoot != options.ContentRoot))
                {
                    report.AddError("Setup identity differs from the initialized project. Reuse .gamefoundation/project.json; migration is a separate operation.");
                    return;
                }
                var scripts = Path.Combine(root, "Assets/Scripts");
                if (!Directory.Exists(scripts)) return;
                var expected = Path.GetFullPath(Path.Combine(root, options.CodeRoot, "Architecture", options.ProjectName + "App.cs"));
                foreach (var file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
                {
                    if (Path.GetFullPath(file) != expected
                        && Regex.IsMatch(File.ReadAllText(file), @":\s*(?:QFramework\.)?Architecture\s*<"))
                        report.AddError("Another project Architecture already exists: " + file + ". Adopt or migrate it before Setup.");
                }
            }
            catch (Exception exception) { report.AddError(exception.Message); }
        }
    }
}
