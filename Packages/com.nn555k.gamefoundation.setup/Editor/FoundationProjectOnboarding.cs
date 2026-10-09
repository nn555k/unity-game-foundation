using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace GameFoundation.Setup.Editor
{
    public static class FoundationProjectOnboarding
    {
        private const string YooType = "GameFoundation.HotUpdate.Providers.YooAsset.Editor.YooAssetProjectSetup";

        /// <summary>Prepares installed providers and writes a machine-readable report without adding hidden scene objects.</summary>
        internal static void Prepare(FoundationProjectSetupOptions options, FoundationProjectSetupReport report)
        {
            var result = Inspect(options, true);
            WriteReport(result);
            foreach (var check in result.checks)
            {
                if (check.status == "failed") report.AddError(check.detail);
                else report.AddUpdated(check.name + ": " + check.detail);
            }
        }

        /// <summary>Rechecks generated code after domain reload, including real provider assets and editor UI layouts.</summary>
        public static void ValidateBatch()
        {
            var root = FoundationGovernanceBootstrap.ResolveProjectRoot();
            FoundationProjectSetupOptions options;
            try
            {
                options = FoundationProjectIdentity.Load(root);
                if (options == null) throw new InvalidOperationException("Project Setup has not run.");
                var conventions = FoundationProjectConventionValidator.Validate(root);
                var report = new FoundationProjectSetupReport();
                FoundationProjectIdentity.Validate(root, options, report);
                if (!conventions.Success || !report.Success)
                    throw new InvalidOperationException(conventions + "\n" + report);
            }
            catch (Exception exception)
            {
                var failed = new OnboardingReport { stage = "failed", localReady = false };
                failed.checks.Add(new OnboardingCheck("projectContract", "failed", exception.Message));
                WriteReport(failed);
                throw;
            }
            var result = Inspect(options, false);
            var appType = FindType(options.RootNamespace + "." + options.ProjectName + "App");
            result.checks.Add(new OnboardingCheck("compiledArchitecture", appType == null ? "failed" : "ready",
                appType == null ? "Generated architecture did not compile." : appType.FullName));
            result.localReady = result.checks.TrueForAll(check => check.status != "failed");
            if (!result.localReady) result.stage = "failed";
            WriteReport(result);
            if (!result.localReady) throw new InvalidOperationException(JsonUtility.ToJson(result, true));
            Debug.Log("GAME_FOUNDATION_ONBOARDING_READY\n" + JsonUtility.ToJson(result, true));
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Checks optional package capabilities through explicit provider entry points, never by type-presence alone.</summary>
        private static OnboardingReport Inspect(FoundationProjectSetupOptions options, bool prepare)
        {
            var report = new OnboardingReport { projectName = options.ProjectName, stage = prepare ? "prepared_awaiting_compile" : "verified" };
            CheckProvider(report, "yooasset", "com.nn555k.gamefoundation.hotupdate.yooasset", YooType,
                prepare ? "Prepare" : "Validate", new object[] { options.ContentPackageName, options.ContentRoot });
            if (options.CreateUiConventionProfile)
            {
                var path = "Assets/Settings/GameFoundation/" + options.ProjectName + "UiPrefabConvention.asset";
                var profile = AssetDatabase.LoadAssetAtPath<GameFoundation.UI.UiPrefabConventionProfile>(path);
                report.checks.Add(new OnboardingCheck("uiProfile", profile == null ? "failed" : "ready", path));
            }
            report.manualConfiguration = new[]
            {
                "Design import tools are external to Foundation; developers integrate and validate them separately when needed.",
                "CDN/Host mode and release asset bundles: project-specific; runtime keeps its safe NoOp backend until injected.",
                "SDK keys, consent policy and device verification: configure when a vendor is selected.",
                "Actual content assets and production prefabs: add to the configured project directories before content builds."
            };
            report.localReady = !prepare && report.checks.TrueForAll(check => check.status != "failed");
            if (report.checks.Exists(check => check.status == "failed")) report.stage = "failed";
            return report;
        }

        /// <summary>Reports absent optional packages separately and converts invocation failures into actionable setup errors.</summary>
        private static void CheckProvider(OnboardingReport report, string name, string packageId,
            string typeName, string methodName, object[] arguments)
        {
            if (PackageInfo.FindForPackageName(packageId) == null)
            {
                var detail = packageId + " is optional; install it before using this integration.";
                report.checks.Add(new OnboardingCheck(name, "not_installed", detail));
                return;
            }
            try
            {
                var method = FindType(typeName)?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (method == null) throw new InvalidOperationException("Provider readiness API missing: " + typeName + ". Upgrade provider and Setup together.");
                var detail = method.Invoke(null, arguments) as string;
                report.checks.Add(new OnboardingCheck(name, "ready", detail ?? "Validated"));
            }
            catch (Exception exception)
            {
                report.checks.Add(new OnboardingCheck(name, "failed", (exception.InnerException ?? exception).Message));
            }
        }

        /// <summary>Resolves a known editor integration only during setup, leaving runtime packages independent.</summary>
        private static Type FindType(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(name, false);
                if (type != null) return type;
            }
            return null;
        }

        /// <summary>Persists non-secret local readiness evidence for AI handoff and later diagnostics.</summary>
        private static void WriteReport(OnboardingReport report)
        {
            var folder = Path.Combine(FoundationGovernanceBootstrap.ResolveProjectRoot(), ".gamefoundation");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "onboarding-report.json"), JsonUtility.ToJson(report, true) + "\n");
        }

        [Serializable]
        private sealed class OnboardingReport
        {
            public string projectName;
            public string stage;
            public bool localReady;
            public List<OnboardingCheck> checks = new List<OnboardingCheck>();
            public string[] manualConfiguration;
        }

        [Serializable]
        private sealed class OnboardingCheck
        {
            public string name;
            public string status;
            public string detail;
            /// <summary>Stores one verifiable capability outcome without credential values.</summary>
            public OnboardingCheck(string name, string status, string detail)
            {
                this.name = name; this.status = status; this.detail = detail;
            }
        }
    }
}
