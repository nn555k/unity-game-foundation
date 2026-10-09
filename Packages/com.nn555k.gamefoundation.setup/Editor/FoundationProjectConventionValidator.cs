using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GameFoundation.Setup.Editor
{
    public static class FoundationProjectConventionValidator
    {
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

        private static readonly string[] RequiredSpecHeadings =
        {
            "## Goal",
            "## User-visible behavior",
            "## Out of scope",
            "## Ownership and architecture",
            "## Acceptance criteria",
            "## Verification matrix",
            "## Definition of Done"
        };

        private static readonly HashSet<string> AllowedStatuses = new HashSet<string>(
            new[] { "Draft", "Ready", "Implementing", "Verifying", "Verified", "Blocked" },
            StringComparer.Ordinal);

        /// <summary>
        /// Runs the project convention gate from the Unity menu and displays blocking errors separately from warnings.
        /// </summary>
        [MenuItem("Game Foundation/Validate Project Conventions")]
        public static void ValidateFromMenu()
        {
            var report = Validate(FoundationGovernanceBootstrap.ResolveProjectRoot());
            if (report.Success)
            {
                Debug.Log(report.ToString());
                EditorUtility.DisplayDialog("Game Foundation Validation", report.ToString(), "OK");
                return;
            }

            Debug.LogError(report.ToString());
            EditorUtility.DisplayDialog("Game Foundation Validation Failed", report.ToString(), "OK");
        }

        /// <summary>
        /// Runs the same convention gate in BatchMode and throws when CI must fail.
        /// </summary>
        public static void ValidateBatch()
        {
            var report = Validate(FoundationGovernanceBootstrap.ResolveProjectRoot());
            if (!report.Success)
            {
                throw new InvalidOperationException(report.ToString());
            }

            Debug.Log(report.ToString());
        }

        /// <summary>
        /// Validates one project root without mutating assets, enabling isolated tests and editor use.
        /// </summary>
        internal static FoundationProjectConventionReport Validate(string projectRoot)
        {
            var report = new FoundationProjectConventionReport();
            var configuration = LoadConfiguration(projectRoot, report);
            ValidateGovernanceFiles(projectRoot, report);
            if (configuration != null)
            {
                ValidateArchitecture(projectRoot, configuration, report);
            }

            ValidateResourceLayout(projectRoot, report);
            ValidateFeatureSpecs(projectRoot, report);
            return report;
        }

        /// <summary>
        /// Loads the generated governance contract and verifies its required identity fields.
        /// </summary>
        private static FoundationProjectConfigurationData LoadConfiguration(
            string projectRoot,
            FoundationProjectConventionReport report)
        {
            var path = Path.Combine(projectRoot, ".gamefoundation", "project.json");
            if (!File.Exists(path))
            {
                report.AddError("Missing .gamefoundation/project.json. Run Game Foundation Project Setup.");
                return null;
            }

            FoundationProjectConfigurationData configuration;
            try
            {
                configuration = JsonUtility.FromJson<FoundationProjectConfigurationData>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                report.AddError("Invalid .gamefoundation/project.json: " + exception.Message);
                return null;
            }

            if (configuration == null
                || string.IsNullOrWhiteSpace(configuration.projectName)
                || string.IsNullOrWhiteSpace(configuration.rootNamespace)
                || string.IsNullOrWhiteSpace(configuration.codeRoot)
                || string.IsNullOrWhiteSpace(configuration.architectureFile)
                || string.IsNullOrWhiteSpace(configuration.controllerFile))
            {
                report.AddError("Project governance configuration has missing required fields.");
                return null;
            }

            return configuration;
        }

        /// <summary>
        /// Confirms that new-project instructions, skills, templates, script, and CI gate are installed.
        /// </summary>
        private static void ValidateGovernanceFiles(
            string projectRoot,
            FoundationProjectConventionReport report)
        {
            var required = new[]
            {
                "AGENTS.md",
                ".agents/skills/specify-foundation-feature/SKILL.md",
                ".agents/skills/develop-foundation-feature/SKILL.md",
                ".agents/skills/normalize-figma-unity-ui/SKILL.md",
                "Docs/Foundation/Architecture.md",
                "Docs/Features/README.md",
                "Docs/Features/FEATURE_TEMPLATE.md",
                "Scripts/validate_game_foundation_project.py",
                ".github/workflows/game-foundation-governance.yml"
            };
            for (var index = 0; index < required.Length; index++)
            {
                if (!File.Exists(ResolveProjectPath(projectRoot, required[index])))
                {
                    report.AddError("Missing governance file: " + required[index]);
                }
            }

            var agentsPath = ResolveProjectPath(projectRoot, "AGENTS.md");
            if (File.Exists(agentsPath))
            {
                var agents = File.ReadAllText(agentsPath);
                if (!agents.Contains(FoundationGovernanceBootstrap.ManagedStart)
                    || !agents.Contains(FoundationGovernanceBootstrap.ManagedEnd))
                {
                    report.AddError("AGENTS.md is missing the Game Foundation managed block.");
                }
            }
        }

        /// <summary>
        /// Enforces the centralized role tree and existence of the configured architecture entry files.
        /// </summary>
        private static void ValidateArchitecture(
            string projectRoot,
            FoundationProjectConfigurationData configuration,
            FoundationProjectConventionReport report)
        {
            var normalizedCodeRoot = NormalizePath(configuration.codeRoot);
            if (!normalizedCodeRoot.StartsWith("Assets/", StringComparison.Ordinal)
                || normalizedCodeRoot.Contains(".."))
            {
                report.AddError("Configured codeRoot must be a non-escaping child path under Assets.");
                return;
            }

            var codeRoot = ResolveProjectPath(projectRoot, normalizedCodeRoot);
            if (!Directory.Exists(codeRoot))
            {
                report.AddError("Configured code root does not exist: " + normalizedCodeRoot);
                return;
            }

            for (var index = 0; index < RoleFolders.Length; index++)
            {
                var rolePath = Path.Combine(codeRoot, RoleFolders[index]);
                if (!Directory.Exists(rolePath))
                {
                    report.AddError("Missing centralized role folder: " + normalizedCodeRoot + "/" + RoleFolders[index]);
                }
            }

            var roleNames = new HashSet<string>(RoleFolders, StringComparer.Ordinal);
            var directories = Directory.GetDirectories(codeRoot, "*", SearchOption.AllDirectories);
            for (var index = 0; index < directories.Length; index++)
            {
                var directory = directories[index];
                if (!roleNames.Contains(Path.GetFileName(directory)))
                {
                    continue;
                }

                if (!string.Equals(Path.GetDirectoryName(directory), codeRoot, StringComparison.Ordinal))
                {
                    report.AddError("Nested role folder is forbidden: " + ProjectRelative(projectRoot, directory));
                }
            }

            ValidateConfiguredSource(projectRoot, configuration.architectureFile, "architectureFile", report);
            ValidateConfiguredSource(projectRoot, configuration.controllerFile, "controllerFile", report);
            ValidateProjectSourceRoot(projectRoot, codeRoot, report);
            ValidateRoleSources(projectRoot, codeRoot, report);
        }

        /// <summary>
        /// Rejects project C# files placed in sibling trees outside the configured role-based code root.
        /// </summary>
        private static void ValidateProjectSourceRoot(
            string projectRoot,
            string codeRoot,
            FoundationProjectConventionReport report)
        {
            var scriptsRoot = Path.Combine(projectRoot, "Assets", "Scripts");
            if (!Directory.Exists(scriptsRoot))
            {
                return;
            }

            var normalizedCodeRoot = Path.GetFullPath(codeRoot)
                .TrimEnd(Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var sources = Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories);
            for (var index = 0; index < sources.Length; index++)
            {
                var normalizedSource = Path.GetFullPath(sources[index]);
                if (!normalizedSource.StartsWith(normalizedCodeRoot, StringComparison.Ordinal))
                {
                    report.AddError(
                        "Project source is outside configured codeRoot: "
                        + ProjectRelative(projectRoot, normalizedSource));
                }
            }
        }

        /// <summary>
        /// Confirms that a generated source path remains inside the project and exists.
        /// </summary>
        private static void ValidateConfiguredSource(
            string projectRoot,
            string relativePath,
            string fieldName,
            FoundationProjectConventionReport report)
        {
            if (string.IsNullOrWhiteSpace(relativePath)
                || relativePath.Contains("..")
                || !File.Exists(ResolveProjectPath(projectRoot, relativePath)))
            {
                report.AddError("Configured " + fieldName + " does not exist: " + relativePath);
            }
        }

        /// <summary>
        /// Blocks high-confidence role violations and warns when a class may be misplaced.
        /// </summary>
        private static void ValidateRoleSources(
            string projectRoot,
            string codeRoot,
            FoundationProjectConventionReport report)
        {
            var utilities = Path.Combine(codeRoot, "Utilities");
            if (Directory.Exists(utilities))
            {
                var utilitySources = Directory.GetFiles(utilities, "*.cs", SearchOption.AllDirectories);
                var forbidden = new[] { "AbstractCommand", "AbstractModel", "AbstractSystem" };
                for (var sourceIndex = 0; sourceIndex < utilitySources.Length; sourceIndex++)
                {
                    var content = File.ReadAllText(utilitySources[sourceIndex]);
                    for (var markerIndex = 0; markerIndex < forbidden.Length; markerIndex++)
                    {
                        if (content.Contains(forbidden[markerIndex]))
                        {
                            report.AddError(
                                "Utility source owns a QFramework role ("
                                + forbidden[markerIndex]
                                + "): "
                                + ProjectRelative(projectRoot, utilitySources[sourceIndex]));
                        }
                    }
                }
            }

            ValidateRoleMarker(projectRoot, codeRoot, "Commands", "AbstractCommand", report);
            ValidateRoleMarker(projectRoot, codeRoot, "Systems", "AbstractSystem", report);
        }

        /// <summary>
        /// Warns on ambiguous role ownership without rejecting legitimate project-specific base classes.
        /// </summary>
        private static void ValidateRoleMarker(
            string projectRoot,
            string codeRoot,
            string role,
            string marker,
            FoundationProjectConventionReport report)
        {
            var roleRoot = Path.Combine(codeRoot, role);
            if (!Directory.Exists(roleRoot))
            {
                return;
            }

            var sources = Directory.GetFiles(roleRoot, "*.cs", SearchOption.AllDirectories);
            for (var index = 0; index < sources.Length; index++)
            {
                var content = File.ReadAllText(sources[index]);
                if (Regex.IsMatch(content, @"\bclass\s+\w+") && !content.Contains(marker))
                {
                    report.AddWarning(
                        "Review " + role + " ownership; class does not mention " + marker + ": "
                        + ProjectRelative(projectRoot, sources[index]));
                }
            }
        }

        /// <summary>
        /// Validates Feature Spec identity, lifecycle, required decisions, and Verified evidence completeness.
        /// </summary>
        private static void ValidateFeatureSpecs(
            string projectRoot,
            FoundationProjectConventionReport report)
        {
            var featureRoot = ResolveProjectPath(projectRoot, "Docs/Features");
            if (!Directory.Exists(featureRoot))
            {
                report.AddError("Missing Docs/Features directory.");
                return;
            }

            var specs = Directory.GetFiles(featureRoot, "*.md", SearchOption.TopDirectoryOnly)
                .Where(path => Path.GetFileName(path) != "README.md"
                    && Path.GetFileName(path) != "FEATURE_TEMPLATE.md")
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            for (var index = 0; index < specs.Length; index++)
            {
                ValidateFeatureSpec(projectRoot, specs[index], report);
            }
        }

        /// <summary>
        /// Checks one Feature Spec without modifying its status or checklist evidence.
        /// </summary>
        private static void ValidateFeatureSpec(
            string projectRoot,
            string path,
            FoundationProjectConventionReport report)
        {
            var content = File.ReadAllText(path);
            var relative = ProjectRelative(projectRoot, path);
            var fields = new[] { "id", "title", "status", "owner", "created", "updated" };
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < fields.Length; index++)
            {
                var match = Regex.Match(
                    content,
                    "^" + Regex.Escape(fields[index]) + @":\s*(.+)$",
                    RegexOptions.Multiline);
                if (!match.Success || string.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    report.AddError("Feature Spec " + relative + " is missing frontmatter field '" + fields[index] + "'.");
                    continue;
                }

                values[fields[index]] = match.Groups[1].Value.Trim().Trim('"');
            }

            if (values.TryGetValue("id", out var featureId))
            {
                if (!Regex.IsMatch(featureId, @"^[a-z0-9]+(?:-[a-z0-9]+)*$"))
                {
                    report.AddError("Feature Spec " + relative + " has invalid id '" + featureId + "'.");
                }

                if (!string.Equals(Path.GetFileNameWithoutExtension(path), featureId, StringComparison.Ordinal))
                {
                    report.AddError("Feature Spec filename must match id '" + featureId + "': " + relative);
                }
            }

            values.TryGetValue("status", out var status);
            if (!string.IsNullOrEmpty(status) && !AllowedStatuses.Contains(status))
            {
                report.AddError("Feature Spec " + relative + " has unsupported status '" + status + "'.");
            }

            for (var index = 0; index < RequiredSpecHeadings.Length; index++)
            {
                if (!content.Contains(RequiredSpecHeadings[index]))
                {
                    report.AddError("Feature Spec " + relative + " is missing heading '" + RequiredSpecHeadings[index] + "'.");
                }
            }

            if ((status == "Ready" || status == "Implementing" || status == "Verifying" || status == "Verified")
                && content.Contains("TBD"))
            {
                report.AddError("Feature Spec " + relative + " cannot contain TBD while status is " + status + ".");
            }

            if (status == "Verified")
            {
                if (content.Contains("Pending"))
                {
                    report.AddError("Verified Feature Spec " + relative + " still contains Pending evidence.");
                }

                if (Regex.IsMatch(content, @"^- \[ \]", RegexOptions.Multiline))
                {
                    report.AddError("Verified Feature Spec " + relative + " still has unchecked Definition of Done items.");
                }
            }
        }

        /// <summary>
        /// Rejects ambiguous Resources keys without moving assets or guessing which references are dynamic.
        /// </summary>
        private static void ValidateResourceLayout(string projectRoot, FoundationProjectConventionReport report)
        {
            var assets = Path.Combine(projectRoot, "Assets");
            if (!Directory.Exists(assets)) return;
            var keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var ignoredExtensions = new HashSet<string>(
                new[] { ".meta", ".cs", ".asmdef", ".asmref", ".dll" }, StringComparer.OrdinalIgnoreCase);
            foreach (var path in Directory.GetFileSystemEntries(assets, "*", SearchOption.AllDirectories)
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                var relative = ProjectRelative(projectRoot, path);
                var parts = relative.Split('/');
                if (parts.Contains("Editor") || parts.Any(part => part.StartsWith(".", StringComparison.Ordinal))) continue;
                if (Directory.Exists(path))
                {
                    if (parts[parts.Length - 1] == "Resources" && relative.StartsWith("Assets/Sprites/", StringComparison.Ordinal))
                        report.AddError("Direct-reference Sprites tree must not contain Resources: " + relative);
                    continue;
                }

                var resourceIndex = Array.IndexOf(parts, "Resources");
                if (resourceIndex < 0 || ignoredExtensions.Contains(Path.GetExtension(path))) continue;
                if (Array.LastIndexOf(parts, "Resources") != resourceIndex)
                {
                    report.AddError("Nested Resources folders are forbidden: " + relative);
                    continue;
                }

                var key = string.Join("/", parts.Skip(resourceIndex + 1));
                var extension = Path.GetExtension(key);
                key = key.Substring(0, key.Length - extension.Length).ToLowerInvariant();
                if (keys.TryGetValue(key, out var previous))
                    report.AddError("Duplicate Resources key '" + key + "': " + previous + " and " + relative);
                else
                    keys.Add(key, relative);
            }
        }

        /// <summary>
        /// Resolves a normalized project-relative path for validation-only file access.
        /// </summary>
        private static string ResolveProjectPath(string projectRoot, string relativePath)
        {
            return Path.Combine(
                projectRoot,
                NormalizePath(relativePath).Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Formats absolute paths as stable project-relative paths in reports.
        /// </summary>
        private static string ProjectRelative(string projectRoot, string fullPath)
        {
            var root = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var normalized = Path.GetFullPath(fullPath);
            return normalized.StartsWith(root, StringComparison.Ordinal)
                ? NormalizePath(normalized.Substring(root.Length))
                : NormalizePath(normalized);
        }

        /// <summary>
        /// Normalizes path separators for configuration and human-readable output.
        /// </summary>
        private static string NormalizePath(string path)
        {
            return (path ?? string.Empty).Trim().Replace('\\', '/').TrimEnd('/');
        }
    }

    public sealed class FoundationProjectConventionReport
    {
        private readonly List<string> mErrors = new List<string>();
        private readonly List<string> mWarnings = new List<string>();

        public IReadOnlyList<string> Errors => mErrors;
        public IReadOnlyList<string> Warnings => mWarnings;
        public bool Success => mErrors.Count == 0;

        /// <summary>
        /// Adds one blocking convention violation.
        /// </summary>
        internal void AddError(string message)
        {
            mErrors.Add(message);
        }

        /// <summary>
        /// Adds one non-blocking ownership review warning.
        /// </summary>
        internal void AddWarning(string message)
        {
            mWarnings.Add(message);
        }

        /// <summary>
        /// Formats validation results for Unity Console, dialogs, and BatchMode logs.
        /// </summary>
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine(Success
                ? "Game Foundation project validation passed."
                : "Game Foundation project validation failed.");
            Append(builder, "Warnings", mWarnings);
            Append(builder, "Errors", mErrors);
            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// Appends one non-empty report section in deterministic order.
        /// </summary>
        private static void Append(StringBuilder builder, string title, List<string> values)
        {
            if (values.Count == 0)
            {
                return;
            }

            builder.AppendLine(title + ":");
            for (var index = 0; index < values.Count; index++)
            {
                builder.AppendLine("- " + values[index]);
            }
        }
    }
}
