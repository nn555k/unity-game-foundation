using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor.PackageManager;
using UnityEngine;

namespace GameFoundation.Setup.Editor
{
    internal static class FoundationGovernanceBootstrap
    {
        internal const string ManagedStart = "<!-- GAME FOUNDATION MANAGED START -->";
        internal const string ManagedEnd = "<!-- GAME FOUNDATION MANAGED END -->";

        private static readonly string[] TemplateDirectories =
        {
            "Agents",
            "Docs",
            "GitHub",
            "Scripts"
        };

        /// <summary>
        /// Installs project-owned AI instructions, workflow templates, validation, CI, and generated governance configuration.
        /// </summary>
        internal static void Generate(
            FoundationProjectSetupOptions options,
            FoundationProjectSetupReport report)
        {
            var projectRoot = ResolveProjectRoot();
            var templateRoot = ResolveTemplateRoot(report);
            if (string.IsNullOrEmpty(templateRoot))
            {
                return;
            }

            InstallManagedAgents(options, report, projectRoot, templateRoot);
            for (var index = 0; index < TemplateDirectories.Length; index++)
            {
                var sourceName = TemplateDirectories[index];
                var destinationName = DestinationDirectory(sourceName);
                CopyTemplateDirectory(
                    Path.Combine(templateRoot, sourceName),
                    Path.Combine(projectRoot, destinationName),
                    destinationName,
                    options,
                    report);
            }

            PreserveOrCreateProjectConfiguration(projectRoot, options, report);
        }

        /// <summary>Repairs keep the complete saved contract, including project extension and provenance fields.</summary>
        internal static void PreserveOrCreateProjectConfiguration(string projectRoot, FoundationProjectSetupOptions options,
            FoundationProjectSetupReport report)
        {
            WriteProjectFile(
                Path.Combine(projectRoot, ".gamefoundation", "project.json"),
                ".gamefoundation/project.json",
                BuildProjectConfiguration(options),
                false,
                report);
        }

        /// <summary>
        /// Builds the machine-readable contract used by local, Unity, and CI validators.
        /// </summary>
        internal static string BuildProjectConfiguration(FoundationProjectSetupOptions options)
        {
            var codeRoot = NormalizePath(options.CodeRoot);
            var configuration = new FoundationProjectConfigurationData
            {
                schemaVersion = 1,
                generatedByVersion = "0.6.1",
                projectName = options.ProjectName,
                rootNamespace = options.RootNamespace,
                codeRoot = codeRoot,
                integrationMode = options.IntegrationMode,
                architectureFile = FoundationProjectIdentity.ArchitecturePath(options),
                architectureType = FoundationProjectIdentity.ArchitectureTypeName(options),
                controllerFile = FoundationProjectIdentity.ControllerPath(options),
                controllerType = FoundationProjectIdentity.ControllerTypeName(options),
                uiConventionProfile = options.CreateUiConventionProfile
                    ? FoundationProjectIdentity.UiProfilePath(options)
                    : string.Empty,
                contentPackageName = options.ContentPackageName,
                contentRoot = options.ContentRoot
            };
            return JsonUtility.ToJson(configuration, true) + "\n";
        }

        /// <summary>
        /// Replaces only the Foundation-owned AGENTS block while preserving all project-owned instructions.
        /// </summary>
        internal static string MergeManagedAgents(string existing, string managedTemplate)
        {
            var start = managedTemplate.IndexOf(ManagedStart, StringComparison.Ordinal);
            var end = managedTemplate.IndexOf(ManagedEnd, StringComparison.Ordinal);
            if (start < 0 || end < start)
            {
                throw new InvalidOperationException("Governance AGENTS template is missing managed markers.");
            }

            var managedBlock = managedTemplate.Substring(
                start,
                end + ManagedEnd.Length - start);
            if (string.IsNullOrWhiteSpace(existing))
            {
                return managedBlock.TrimEnd() + "\n";
            }

            var existingStart = existing.IndexOf(ManagedStart, StringComparison.Ordinal);
            var existingEnd = existing.IndexOf(ManagedEnd, StringComparison.Ordinal);
            if ((existingStart >= 0) != (existingEnd >= 0)
                || (existingStart >= 0 && existingEnd < existingStart))
            {
                throw new InvalidOperationException(
                    "Existing AGENTS.md has an incomplete Game Foundation managed block.");
            }

            if (existingStart >= 0 && existingEnd >= existingStart)
            {
                return existing.Substring(0, existingStart)
                    + managedBlock
                    + existing.Substring(existingEnd + ManagedEnd.Length);
            }

            return existing.TrimEnd() + "\n\n" + managedBlock.TrimEnd() + "\n";
        }

        /// <summary>Hashes only the managed block so project-owned text outside it never prevents a safe update.</summary>
        internal static string ManagedAgentsHash(string content)
        {
            var start = content.IndexOf(ManagedStart, StringComparison.Ordinal);
            var end = content.IndexOf(ManagedEnd, StringComparison.Ordinal);
            if (start < 0 && end < 0) return string.Empty;
            if (start < 0 || end < start || content.IndexOf(ManagedStart, start + ManagedStart.Length, StringComparison.Ordinal) >= 0
                || content.IndexOf(ManagedEnd, end + ManagedEnd.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("Existing AGENTS.md has incomplete or duplicate managed markers.");
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(
                    content.Substring(start, end + ManagedEnd.Length - start)))).Replace("-", string.Empty).ToLowerInvariant();
        }

        /// <summary>Unknown legacy or locally modified managed text is preserved for an explicit manual merge.</summary>
        internal static bool CanUpdateManagedAgents(string existing, string template, string installedHash)
        {
            var current = ManagedAgentsHash(existing);
            return current.Length == 0 || current == ManagedAgentsHash(template)
                || (!string.IsNullOrWhiteSpace(installedHash) && current == installedHash.Trim());
        }

        /// <summary>
        /// Resolves the project root from Unity's Assets directory without relying on the process working directory.
        /// </summary>
        internal static string ResolveProjectRoot()
        {
            var parent = Directory.GetParent(Application.dataPath);
            if (parent == null)
            {
                throw new InvalidOperationException("Unity project root could not be resolved.");
            }

            return parent.FullName;
        }

        /// <summary>
        /// Locates the governance template tree inside either a local or Package Cache installation.
        /// </summary>
        private static string ResolveTemplateRoot(FoundationProjectSetupReport report)
        {
            var package = PackageInfo.FindForAssembly(Assembly.GetExecutingAssembly());
            if (package == null || string.IsNullOrEmpty(package.resolvedPath))
            {
                report.AddError("Game Foundation Setup package path could not be resolved.");
                return string.Empty;
            }

            var templateRoot = Path.Combine(package.resolvedPath, "Templates~", "Governance");
            if (!Directory.Exists(templateRoot))
            {
                report.AddError("Governance templates are missing from the Setup package.");
                return string.Empty;
            }

            return templateRoot;
        }

        /// <summary>
        /// Merges the managed AGENTS section with project-specific tokens and records the resulting file.
        /// </summary>
        private static void InstallManagedAgents(
            FoundationProjectSetupOptions options,
            FoundationProjectSetupReport report,
            string projectRoot,
            string templateRoot)
        {
            var sourcePath = Path.Combine(templateRoot, "AGENTS.md");
            if (!File.Exists(sourcePath))
            {
                report.AddError("Governance AGENTS template is missing.");
                return;
            }

            var targetPath = Path.Combine(projectRoot, "AGENTS.md");
            var template = ApplyProjectTokens(File.ReadAllText(sourcePath), options);
            var existing = File.Exists(targetPath) ? File.ReadAllText(targetPath) : string.Empty;
            var receiptPath = Path.Combine(projectRoot, ".gamefoundation", "managed-agents.sha256");
            var installedHash = File.Exists(receiptPath) ? File.ReadAllText(receiptPath) : string.Empty;
            if (!CanUpdateManagedAgents(existing, template, installedHash))
            {
                WriteProjectFile(Path.Combine(projectRoot, ".gamefoundation", "AGENTS.managed.proposed.md"),
                    ".gamefoundation/AGENTS.managed.proposed.md", template, true, report);
                report.AddSkipped("AGENTS.md: locally modified or legacy managed block preserved; review .gamefoundation/AGENTS.managed.proposed.md.");
                return;
            }
            var merged = MergeManagedAgents(existing, template);
            WriteProjectFile(targetPath, "AGENTS.md", merged, true, report);
            WriteProjectFile(receiptPath, ".gamefoundation/managed-agents.sha256", ManagedAgentsHash(template) + "\n", true, report);
        }

        /// <summary>
        /// Copies one template directory recursively while mapping package-safe folder names to project dot-folders.
        /// </summary>
        private static void CopyTemplateDirectory(
            string sourceRoot,
            string destinationRoot,
            string destinationPrefix,
            FoundationProjectSetupOptions options,
            FoundationProjectSetupReport report)
        {
            if (!Directory.Exists(sourceRoot))
            {
                report.AddError("Governance template directory is missing: " + sourceRoot);
                return;
            }

            var sourceFiles = Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories);
            Array.Sort(sourceFiles, StringComparer.Ordinal);
            for (var index = 0; index < sourceFiles.Length; index++)
            {
                var sourcePath = sourceFiles[index];
                var relative = sourcePath.Substring(sourceRoot.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var targetPath = Path.Combine(destinationRoot, relative);
                var reportPath = NormalizePath(Path.Combine(destinationPrefix, relative));
                var content = ApplyProjectTokens(File.ReadAllText(sourcePath), options);
                WriteProjectFile(
                    targetPath,
                    reportPath,
                    content,
                    options.OverwriteExisting,
                    report);
            }
        }

        /// <summary>
        /// Converts package-safe template directory names into their project-root destinations.
        /// </summary>
        private static string DestinationDirectory(string sourceName)
        {
            if (sourceName == "Agents")
            {
                return ".agents";
            }

            if (sourceName == "GitHub")
            {
                return ".github";
            }

            return sourceName;
        }

        /// <summary>
        /// Replaces only documented project tokens so Feature Spec placeholders remain intact.
        /// </summary>
        private static string ApplyProjectTokens(
            string content,
            FoundationProjectSetupOptions options)
        {
            return content
                .Replace("{{PROJECT_NAME}}", options.ProjectName)
                .Replace("{{ROOT_NAMESPACE}}", options.RootNamespace)
                .Replace("{{ARCHITECTURE_TYPE}}", FoundationProjectIdentity.ArchitectureTypeName(options))
                .Replace("{{CONTROLLER_TYPE}}", FoundationProjectIdentity.ControllerTypeName(options))
                .Replace("{{CODE_ROOT}}", NormalizePath(options.CodeRoot));
        }

        /// <summary>
        /// Writes a project-root text file with UTF-8 and explicit overwrite policy.
        /// </summary>
        private static void WriteProjectFile(
            string fullPath,
            string reportPath,
            string content,
            bool overwrite,
            FoundationProjectSetupReport report)
        {
            var exists = File.Exists(fullPath);
            if (exists && !overwrite)
            {
                report.AddSkipped(reportPath);
                return;
            }

            var parent = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            File.WriteAllText(fullPath, content, new UTF8Encoding(false));
            if (exists)
            {
                report.AddUpdated(reportPath);
            }
            else
            {
                report.AddCreated(reportPath);
            }
        }

        /// <summary>
        /// Normalizes generated paths for Unity, JSON, and cross-platform validation output.
        /// </summary>
        private static string NormalizePath(string path)
        {
            return (path ?? string.Empty).Trim().Replace('\\', '/').TrimEnd('/');
        }
    }

    [Serializable]
    internal sealed class FoundationProjectConfigurationData
    {
        public int schemaVersion;
        public string generatedByVersion;
        public string projectName;
        public string rootNamespace;
        public string codeRoot;
        public string architectureFile;
        public string integrationMode;
        public string architectureType;
        public string controllerFile;
        public string controllerType;
        public string uiConventionProfile;
        public string contentPackageName;
        public string contentRoot;
    }
}
