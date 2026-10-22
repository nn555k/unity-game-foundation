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
                IntegrationMode = string.IsNullOrEmpty(data.integrationMode) ? "generated" : data.integrationMode,
                ArchitectureFile = data.architectureFile, ArchitectureType = data.architectureType,
                ControllerFile = data.controllerFile, ControllerType = data.controllerType,
                UiConventionProfilePath = data.uiConventionProfile,
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
                    || existing.ContentPackageName != options.ContentPackageName || existing.ContentRoot != options.ContentRoot
                    || existing.IntegrationMode != options.IntegrationMode
                    || ArchitecturePath(existing) != ArchitecturePath(options) || ControllerPath(existing) != ControllerPath(options)
                    || ArchitectureTypeName(existing) != ArchitectureTypeName(options) || ControllerTypeName(existing) != ControllerTypeName(options)
                    || existing.CreateUiConventionProfile != options.CreateUiConventionProfile
                    || (existing.CreateUiConventionProfile && UiProfilePath(existing) != UiProfilePath(options))))
                {
                    report.AddError("Setup identity differs from the initialized project. Reuse .gamefoundation/project.json; migration is a separate operation.");
                    return;
                }
                var scripts = Path.Combine(root, "Assets/Scripts");
                var expected = Path.GetFullPath(Path.Combine(root, ArchitecturePath(options)));
                if (options.IntegrationMode == "existing-architecture")
                {
                    if (!File.Exists(expected) || !File.Exists(Path.Combine(root, ControllerPath(options))))
                        report.AddError("Existing architecture and Controller files must exist before adoption.");
                    else if (!Regex.IsMatch(FoundationProjectConventionValidator.CSharpDeclarations(File.ReadAllText(expected)),
                        @":\s*(?:QFramework\.)?Architecture\s*<"))
                        report.AddError("Configured architectureFile does not declare an Architecture<T>.");
                }
                if (!Directory.Exists(scripts)) return;
                foreach (var file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
                {
                    if (Path.GetFullPath(file) != expected
                        && Regex.IsMatch(FoundationProjectConventionValidator.CSharpDeclarations(File.ReadAllText(file)), @":\s*(?:QFramework\.)?Architecture\s*<"))
                        report.AddError("Another project Architecture already exists: " + file + ". Adopt or migrate it before Setup.");
                }
            }
            catch (Exception exception) { report.AddError(exception.Message); }
        }

        /// <summary>Resolves the configured architecture source without renaming adopted project types.</summary>
        internal static string ArchitecturePath(FoundationProjectSetupOptions options)
        {
            return string.IsNullOrEmpty(options.ArchitectureFile)
                ? options.CodeRoot + "/Architecture/" + options.ProjectName + "App.cs" : options.ArchitectureFile;
        }

        /// <summary>Resolves the configured Controller source independently of new-project naming.</summary>
        internal static string ControllerPath(FoundationProjectSetupOptions options)
        {
            return string.IsNullOrEmpty(options.ControllerFile)
                ? options.CodeRoot + "/Architecture/" + options.ProjectName + "Controller.cs" : options.ControllerFile;
        }

        /// <summary>Older contracts can infer conventional type names from their explicitly recorded source files.</summary>
        internal static string ArchitectureTypeName(FoundationProjectSetupOptions options)
        {
            return string.IsNullOrEmpty(options.ArchitectureType)
                ? options.RootNamespace + "." + Path.GetFileNameWithoutExtension(ArchitecturePath(options)) : options.ArchitectureType;
        }

        /// <summary>Resolves the adopted Controller type for generated instructions and post-compile checks.</summary>
        internal static string ControllerTypeName(FoundationProjectSetupOptions options)
        {
            return string.IsNullOrEmpty(options.ControllerType)
                ? options.RootNamespace + "." + Path.GetFileNameWithoutExtension(ControllerPath(options)) : options.ControllerType;
        }

        /// <summary>Preserves a project's serialized UI profile path while retaining new-project defaults.</summary>
        internal static string UiProfilePath(FoundationProjectSetupOptions options)
        {
            return string.IsNullOrEmpty(options.UiConventionProfilePath)
                ? "Assets/Settings/GameFoundation/" + options.ProjectName + "UiPrefabConvention.asset" : options.UiConventionProfilePath;
        }
    }
}
