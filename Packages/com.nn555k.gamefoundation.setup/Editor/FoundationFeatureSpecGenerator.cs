using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GameFoundation.Setup.Editor
{
    public static class FoundationFeatureSpecGenerator
    {
        private static readonly Regex FeatureId = new Regex(
            @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// Creates a project-owned Feature Spec from the canonical template without overwriting existing work by default.
        /// </summary>
        public static FoundationProjectSetupReport Generate(
            string featureId,
            string title,
            string summary,
            bool overwriteExisting = false)
        {
            var report = new FoundationProjectSetupReport();
            if (!Validate(featureId, title, report))
            {
                return report;
            }

            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                Assembly.GetExecutingAssembly());
            if (package == null || string.IsNullOrEmpty(package.resolvedPath))
            {
                report.AddError("Game Foundation Setup package path could not be resolved.");
                return report;
            }

            var templatePath = Path.Combine(
                package.resolvedPath,
                "Templates~",
                "Governance",
                "Docs",
                "Features",
                "FEATURE_TEMPLATE.md");
            if (!File.Exists(templatePath))
            {
                report.AddError("Feature Spec template is missing from the Setup package.");
                return report;
            }

            var projectRoot = FoundationGovernanceBootstrap.ResolveProjectRoot();
            var relativePath = "Docs/Features/" + featureId + ".md";
            var fullPath = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var existed = File.Exists(fullPath);
            if (existed && !overwriteExisting)
            {
                report.AddSkipped(relativePath);
                return report;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            var content = BuildFeatureSpec(
                File.ReadAllText(templatePath),
                featureId,
                title.Trim(),
                summary);
            File.WriteAllText(fullPath, content, new UTF8Encoding(false));
            if (existed)
            {
                report.AddUpdated(relativePath);
            }
            else
            {
                report.AddCreated(relativePath);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return report;
        }

        /// <summary>
        /// Applies identity, date, and request summary tokens while leaving the remaining checklist explicit.
        /// </summary>
        internal static string BuildFeatureSpec(
            string template,
            string featureId,
            string title,
            string summary)
        {
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var normalizedSummary = string.IsNullOrWhiteSpace(summary)
                ? "Describe the user-visible result and why it is needed."
                : summary.Trim();
            return template
                .Replace("{{FEATURE_ID}}", featureId)
                .Replace("{{FEATURE_TITLE}}", title)
                .Replace("{{FEATURE_SUMMARY}}", normalizedSummary)
                .Replace("{{DATE}}", date);
        }

        /// <summary>
        /// Rejects unsafe identifiers and empty titles before any project file is created.
        /// </summary>
        internal static bool Validate(
            string featureId,
            string title,
            FoundationProjectSetupReport report)
        {
            if (!FeatureId.IsMatch(featureId ?? string.Empty))
            {
                report.AddError("Feature ID must be lowercase kebab-case.");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                report.AddError("Feature title is required.");
            }

            return report.Success;
        }
    }
}
