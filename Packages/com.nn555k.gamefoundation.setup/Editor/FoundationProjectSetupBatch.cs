using System;
using UnityEngine;

namespace GameFoundation.Setup.Editor
{
    public static class FoundationProjectSetupBatch
    {
        /// <summary>
        /// 从 GAME_FOUNDATION_* 环境变量生成项目，供 AI、CI 和 batchmode 使用。
        /// </summary>
        public static void GenerateFromEnvironment()
        {
            var saved = FoundationProjectIdentity.Load(FoundationGovernanceBootstrap.ResolveProjectRoot());
            var projectName = Read("GAME_FOUNDATION_PROJECT_NAME", saved?.ProjectName ?? string.Empty);
            if (string.IsNullOrEmpty(projectName))
                throw new InvalidOperationException("First setup requires GAME_FOUNDATION_PROJECT_NAME. No placeholder project will be generated.");
            var options = new FoundationProjectSetupOptions
            {
                ProjectName = projectName,
                RootNamespace = Read("GAME_FOUNDATION_ROOT_NAMESPACE", saved?.RootNamespace ?? projectName),
                CodeRoot = Read("GAME_FOUNDATION_CODE_ROOT", saved?.CodeRoot ?? "Assets/Scripts/Game"),
                IncludeSave = ReadBool("GAME_FOUNDATION_INCLUDE_SAVE", saved?.IncludeSave ?? true),
                IncludeHotUpdate = ReadBool("GAME_FOUNDATION_INCLUDE_HOTUPDATE", saved?.IncludeHotUpdate ?? true),
                IncludeSdk = ReadBool("GAME_FOUNDATION_INCLUDE_SDK", saved?.IncludeSdk ?? true),
                IncludeUi = ReadBool("GAME_FOUNDATION_INCLUDE_UI", saved?.IncludeUi ?? true),
                CreateAssemblyDefinition = ReadBool("GAME_FOUNDATION_CREATE_ASMDEF", saved?.CreateAssemblyDefinition ?? true),
                CreateUiConventionProfile = ReadBool("GAME_FOUNDATION_CREATE_UI_PROFILE", saved?.CreateUiConventionProfile ?? true),
                InstallGovernance = ReadBool("GAME_FOUNDATION_INSTALL_GOVERNANCE", true),
                PrepareInstalledTools = ReadBool("GAME_FOUNDATION_PREPARE_TOOLS", true),
                ContentPackageName = Read("GAME_FOUNDATION_CONTENT_PACKAGE", saved?.ContentPackageName ?? "DefaultPackage"),
                ContentRoot = Read("GAME_FOUNDATION_CONTENT_ROOT", saved?.ContentRoot ?? "Assets/GameContent"),
                OverwriteExisting = ReadBool("GAME_FOUNDATION_OVERWRITE", false)
            };

            var report = FoundationProjectScaffolder.Generate(options);
            if (!report.Success)
            {
                throw new InvalidOperationException(report.ToString());
            }

            var featureId = Read("GAME_FOUNDATION_FEATURE_ID", string.Empty);
            if (!string.IsNullOrEmpty(featureId))
            {
                var featureReport = FoundationFeatureSpecGenerator.Generate(
                    featureId,
                    Read("GAME_FOUNDATION_FEATURE_TITLE", featureId),
                    Read("GAME_FOUNDATION_FEATURE_SUMMARY", string.Empty));
                if (!featureReport.Success)
                {
                    throw new InvalidOperationException(featureReport.ToString());
                }

                Debug.Log(featureReport.ToString());
            }

            Debug.Log(report.ToString());
            if (Application.isBatchMode) UnityEditor.EditorApplication.Exit(0);
        }

        /// <summary>
        /// 读取非空环境变量，否则使用安全默认值。
        /// </summary>
        private static string Read(string name, string fallback)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        /// <summary>
        /// 读取布尔环境变量，支持 true/false 与 1/0。
        /// </summary>
        private static bool ReadBool(string name, bool fallback)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            if (bool.TryParse(value, out var parsed))
            {
                return parsed;
            }

            return value.Trim() == "1";
        }
    }
}
