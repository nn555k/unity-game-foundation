using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GameFoundation.UI;
using UnityEditor;
using UnityEngine;

namespace GameFoundation.Setup.Editor
{
    public static class FoundationProjectScaffolder
    {
        private static readonly Regex Identifier = new Regex(
            @"^[A-Za-z_][A-Za-z0-9_]*$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly string[] RoleFolders =
        {
            "Architecture",
            "Commands",
            "Events",
            "Models",
            "Systems",
            "Utilities",
            "ViewControllers"
        };

        /// <summary>
        /// 生成项目自有架构、角色目录、程序集和 UI Profile，默认不覆盖既有文件。
        /// </summary>
        public static FoundationProjectSetupReport Generate(FoundationProjectSetupOptions options)
        {
            var report = new FoundationProjectSetupReport();
            if (!ValidateOptions(options, report))
            {
                return report;
            }

            FoundationProjectIdentity.Validate(FoundationGovernanceBootstrap.ResolveProjectRoot(), options, report);
            if (!report.Success) return report;

            var codeRoot = NormalizeAssetPath(options.CodeRoot);
            EnsureAssetFolder(codeRoot, report);
            for (var index = 0; index < RoleFolders.Length; index++)
            {
                EnsureAssetFolder(codeRoot + "/" + RoleFolders[index], report);
            }

            EnsureAssetFolder("Assets/Resources/Prefabs/UI", report);
            EnsureAssetFolder("Assets/Resources/Sprites/UI", report);
            EnsureAssetFolder("Assets/Sprites/UI", report);
            EnsureAssetFolder("Assets/Sprites/Prefabs/UI", report);
            EnsureAssetFolder("Assets/Sprites/DesignImports", report);

            WriteTextAsset(
                codeRoot + "/Architecture/" + options.ProjectName + "App.cs",
                BuildArchitectureSource(options),
                options.OverwriteExisting,
                report);
            WriteTextAsset(
                codeRoot + "/Architecture/" + options.ProjectName + "Controller.cs",
                BuildControllerSource(options),
                options.OverwriteExisting,
                report);

            if (options.CreateAssemblyDefinition)
            {
                WriteTextAsset(
                    codeRoot + "/" + options.RootNamespace + ".Game.asmdef",
                    BuildAssemblyDefinition(options),
                    options.OverwriteExisting,
                    report);
            }

            if (options.CreateUiConventionProfile)
            {
                CreateUiProfile(options, report);
            }

            if (options.InstallGovernance)
            {
                FoundationGovernanceBootstrap.Generate(options, report);
            }

            if (options.PrepareInstalledTools && report.Success)
                FoundationProjectOnboarding.Prepare(options, report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return report;
        }

        /// <summary>
        /// 校验名称、路径和功能组合，拒绝可能逃逸 Assets 或生成无效 C# 的输入。
        /// </summary>
        internal static bool ValidateOptions(
            FoundationProjectSetupOptions options,
            FoundationProjectSetupReport report)
        {
            if (options == null)
            {
                report.AddError("Setup options are missing.");
                return false;
            }

            if (!Identifier.IsMatch(options.ProjectName ?? string.Empty))
            {
                report.AddError("ProjectName must be a valid C# identifier.");
            }

            if (!IsValidNamespace(options.RootNamespace))
            {
                report.AddError("RootNamespace must contain valid dot-separated C# identifiers.");
            }

            var codeRoot = NormalizeAssetPath(options.CodeRoot);
            if (!codeRoot.StartsWith("Assets/", StringComparison.Ordinal) || codeRoot.Contains(".."))
            {
                report.AddError("CodeRoot must be a child path under Assets and cannot contain '..'.");
            }

            if (options.CreateUiConventionProfile && !options.IncludeUi)
            {
                report.AddError("UI must be enabled when creating a UI convention profile.");
            }

            var contentRoot = options.ContentRoot ?? string.Empty;
            if (!contentRoot.StartsWith("Assets/", StringComparison.Ordinal) || contentRoot.Contains("..")
                || contentRoot.Contains(":") || contentRoot.Contains("\\")
                || contentRoot.Split('/').Any(part => part == "Resources" || part == "." || part.Length == 0))
                report.AddError("ContentRoot must be a dedicated child folder under Assets, outside Resources.");
            if (!Identifier.IsMatch(options.ContentPackageName ?? string.Empty))
                report.AddError("ContentPackageName must be a valid identifier.");

            return report.Success;
        }

        /// <summary>
        /// 构建只注册所选模块的项目 Architecture 源码。
        /// </summary>
        internal static string BuildArchitectureSource(FoundationProjectSetupOptions options)
        {
            var imports = new List<string> { "GameFoundation.Core" };
            var registrations = new List<string> { "            FoundationCoreModule.Register(this);" };
            AddModule(options.IncludeSave, imports, registrations, "GameFoundation.Save", "FoundationSaveModule");
            AddModule(options.IncludeHotUpdate, imports, registrations, "GameFoundation.HotUpdate", "FoundationHotUpdateModule");
            AddModule(options.IncludeSdk, imports, registrations, "GameFoundation.Sdk", "FoundationSdkModule");
            AddModule(options.IncludeUi, imports, registrations, "GameFoundation.UI", "FoundationUiModule");

            var builder = new StringBuilder();
            for (var index = 0; index < imports.Count; index++)
            {
                builder.Append("using ").Append(imports[index]).AppendLine(";");
            }

            builder.AppendLine("using QFramework;")
                .AppendLine()
                .Append("namespace ").AppendLine(options.RootNamespace)
                .AppendLine("{")
                .Append("    public sealed class ").Append(options.ProjectName).Append("App : Architecture<")
                .Append(options.ProjectName).AppendLine("App>")
                .AppendLine("    {")
                .AppendLine("        /// <summary>")
                .AppendLine("        /// Registers shared infrastructure before project-owned models and systems.")
                .AppendLine("        /// </summary>")
                .AppendLine("        protected override void Init()")
                .AppendLine("        {");
            for (var index = 0; index < registrations.Count; index++)
            {
                builder.AppendLine(registrations[index]);
            }

            builder.AppendLine()
                .AppendLine("            // Register project Utilities, Models, and Systems here.")
                .AppendLine("        }")
                .AppendLine("    }")
                .AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// 构建所有项目 MonoBehaviour Controller 共用的架构入口。
        /// </summary>
        internal static string BuildControllerSource(FoundationProjectSetupOptions options)
        {
            return
                "using QFramework;\n" +
                "using UnityEngine;\n\n" +
                "namespace " + options.RootNamespace + "\n" +
                "{\n" +
                "    public abstract class " + options.ProjectName + "Controller : MonoBehaviour, IController\n" +
                "    {\n" +
                "        /// <summary>\n" +
                "        /// Returns the single project-owned QFramework architecture.\n" +
                "        /// </summary>\n" +
                "        public IArchitecture GetArchitecture()\n" +
                "        {\n" +
                "            return " + options.ProjectName + "App.Interface;\n" +
                "        }\n" +
                "    }\n" +
                "}\n";
        }

        /// <summary>
        /// 构建只引用所选 Foundation 程序集的项目 asmdef。
        /// </summary>
        internal static string BuildAssemblyDefinition(FoundationProjectSetupOptions options)
        {
            var references = new List<string> { "QFramework", "GameFoundation.Core" };
            AddReference(options.IncludeSave, references, "GameFoundation.Save");
            AddReference(options.IncludeHotUpdate, references, "GameFoundation.HotUpdate");
            AddReference(options.IncludeSdk, references, "GameFoundation.Sdk");
            AddReference(options.IncludeUi, references, "GameFoundation.UI");

            var assembly = new AssemblyDefinitionData
            {
                name = options.RootNamespace + ".Game",
                rootNamespace = options.RootNamespace,
                references = references.ToArray(),
                autoReferenced = true
            };
            return JsonUtility.ToJson(assembly, true) + "\n";
        }

        /// <summary>
        /// 向源码生成列表追加一个启用的模块导入与注册语句。
        /// </summary>
        private static void AddModule(
            bool enabled,
            List<string> imports,
            List<string> registrations,
            string moduleNamespace,
            string moduleType)
        {
            if (!enabled)
            {
                return;
            }

            imports.Add(moduleNamespace);
            registrations.Add("            " + moduleType + ".Register(this);");
        }

        /// <summary>
        /// 向 asmdef 引用列表追加一个启用的程序集名称。
        /// </summary>
        private static void AddReference(bool enabled, List<string> references, string assemblyName)
        {
            if (enabled)
            {
                references.Add(assemblyName);
            }
        }

        /// <summary>
        /// 验证点分命名空间的每一段都是合法 C# 标识符。
        /// </summary>
        private static bool IsValidNamespace(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Split('.').All(segment => Identifier.IsMatch(segment));
        }

        /// <summary>
        /// 统一资源路径分隔符并移除首尾空白和末尾斜杠。
        /// </summary>
        private static string NormalizeAssetPath(string path)
        {
            return (path ?? string.Empty).Trim().Replace('\\', '/').TrimEnd('/');
        }

        /// <summary>
        /// 逐级创建缺失的 AssetDatabase 文件夹并记录实际新增路径。
        /// </summary>
        private static void EnsureAssetFolder(string path, FoundationProjectSetupReport report)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                    report.AddCreated(next);
                }

                current = next;
            }
        }

        /// <summary>
        /// 写入文本资源；既有文件默认跳过，只有显式允许时才覆盖。
        /// </summary>
        private static void WriteTextAsset(
            string assetPath,
            string content,
            bool overwrite,
            FoundationProjectSetupReport report)
        {
            var fullPath = ToFullPath(assetPath);
            if (File.Exists(fullPath) && !overwrite)
            {
                report.AddSkipped(assetPath);
                return;
            }

            File.WriteAllText(fullPath, content, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            report.AddCreated(assetPath);
        }

        /// <summary>
        /// 创建由项目持有的 UI 结构规则资产，既有 Profile 默认保留。
        /// </summary>
        private static void CreateUiProfile(
            FoundationProjectSetupOptions options,
            FoundationProjectSetupReport report)
        {
            const string folder = "Assets/Settings/GameFoundation";
            EnsureAssetFolder(folder, report);
            var assetPath = folder + "/" + options.ProjectName + "UiPrefabConvention.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UiPrefabConventionProfile>(assetPath);
            if (existing && !options.OverwriteExisting)
            {
                report.AddSkipped(assetPath);
                return;
            }

            if (existing)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            var profile = ScriptableObject.CreateInstance<UiPrefabConventionProfile>();
            AssetDatabase.CreateAsset(profile, assetPath);
            report.AddCreated(assetPath);
        }

        /// <summary>
        /// 将 Assets 相对路径解析为当前 Unity 项目的绝对文件路径。
        /// </summary>
        private static string ToFullPath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new InvalidOperationException("Unity project root could not be resolved.");
            }

            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        [Serializable]
        private sealed class AssemblyDefinitionData
        {
            public string name;
            public string rootNamespace;
            public string[] references;
            public bool autoReferenced;
        }
    }
}
