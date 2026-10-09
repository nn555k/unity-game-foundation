using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace GameFoundation.Setup.Editor
{
    [InitializeOnLoad]
    public static class FoundationOptionalToolInstaller
    {
        private const string InstallQueueKey = "GameFoundation.OptionalToolInstallQueue";
        private const string FoundationRepository = "https://github.com/nn555k/unity-game-foundation.git";
        private const string FoundationVersion = "v0.6.0";
        private const string YooAssetPackageId = "com.tuyoogame.yooasset";
        private const string YooAssetUrl = "https://github.com/tuyoogame/YooAsset.git?path=Assets/YooAsset#2.3.18";
        private static readonly PackageReference YooAssetBackend = new PackageReference(
            "com.nn555k.gamefoundation.hotupdate.yooasset",
            FoundationPackageUrl("com.nn555k.gamefoundation.hotupdate.yooasset"));
        private static AddRequest sAddRequest;

        /// <summary>
        /// Resumes an interrupted install queue after Unity reloads assemblies between package additions.
        /// </summary>
        static FoundationOptionalToolInstaller()
        {
            if (ReadQueue().Count > 0)
            {
                EditorApplication.update += ProcessQueue;
            }
        }

        /// <summary>
        /// Installs the pinned YooAsset dependency and its Foundation backend in dependency order.
        /// </summary>
        [MenuItem("Game Foundation/Install Optional Tools/YooAsset Backend")]
        public static void InstallYooAsset()
        {
            StartInstall(new[]
            {
                new PackageReference(YooAssetPackageId, YooAssetUrl),
                YooAssetBackend
            });
        }

        /// <summary>
        /// Installs the default content-update toolchain; external design import tools are not distributed here.
        /// </summary>
        [MenuItem("Game Foundation/Install Optional Tools/Recommended All")]
        public static void InstallRecommended()
        {
            InstallYooAsset();
        }

        /// <summary>Disables completed installation actions while retaining setup/status access.</summary>
        [MenuItem("Game Foundation/Install Optional Tools/YooAsset Backend", true)]
        private static bool CanInstallYooAsset() => PackageInfo.FindForPackageName(YooAssetBackend.Id) == null || PackageInfo.FindForPackageName(YooAssetPackageId) == null;

        /// <summary>Only the supported content-update provider affects recommended installation readiness.</summary>
        [MenuItem("Game Foundation/Install Optional Tools/Recommended All", true)]
        private static bool CanInstallRecommended() => CanInstallYooAsset();

        /// <summary>
        /// Persists an idempotent queue before invoking UPM because each package can trigger a domain reload.
        /// </summary>
        private static void StartInstall(IEnumerable<PackageReference> packages)
        {
            var queue = packages
                .Where(package => UnityEditor.PackageManager.PackageInfo.FindForPackageName(package.Id) == null)
                .ToList();
            if (queue.Count == 0)
            {
                Debug.Log("[Game Foundation] Optional tools are already installed.");
                return;
            }

            WriteQueue(queue);
            EditorApplication.update -= ProcessQueue;
            EditorApplication.update += ProcessQueue;
        }

        /// <summary>
        /// Advances one UPM request at a time and keeps the remaining queue recoverable across reloads.
        /// </summary>
        private static void ProcessQueue()
        {
            var queue = ReadQueue();
            while (queue.Count > 0 && UnityEditor.PackageManager.PackageInfo.FindForPackageName(queue[0].Id) != null)
            {
                queue.RemoveAt(0);
                WriteQueue(queue);
            }

            if (queue.Count == 0)
            {
                FinishInstall();
                return;
            }

            if (sAddRequest == null)
            {
                Debug.Log($"[Game Foundation] Installing {queue[0].Id}...");
                sAddRequest = Client.Add(queue[0].Url);
                return;
            }

            if (!sAddRequest.IsCompleted)
            {
                return;
            }

            if (sAddRequest.Status == StatusCode.Failure)
            {
                var error = sAddRequest.Error?.message ?? "Unknown UPM error.";
                Debug.LogError($"[Game Foundation] Failed to install {queue[0].Id}: {error}");
                SessionState.EraseString(InstallQueueKey);
                EditorApplication.update -= ProcessQueue;
                sAddRequest = null;
                return;
            }

            queue.RemoveAt(0);
            WriteQueue(queue);
            sAddRequest = null;
        }

        /// <summary>
        /// Clears transient state after all requested packages are visible to Package Manager.
        /// </summary>
        private static void FinishInstall()
        {
            SessionState.EraseString(InstallQueueKey);
            EditorApplication.update -= ProcessQueue;
            sAddRequest = null;
            Debug.Log("[Game Foundation] Optional tooling installation completed.");
            var options = FoundationProjectIdentity.Load(FoundationGovernanceBootstrap.ResolveProjectRoot());
            if (options != null)
            {
                var report = new FoundationProjectSetupReport();
                FoundationProjectOnboarding.Prepare(options, report);
                if (report.Success) Debug.Log(report.ToString());
                else Debug.LogError(report.ToString());
            }
        }

        /// <summary>
        /// Serializes package IDs and URLs into SessionState without creating project files.
        /// </summary>
        private static void WriteQueue(IReadOnlyCollection<PackageReference> queue)
        {
            var value = string.Join("\n", queue.Select(package => $"{package.Id}\t{package.Url}"));
            SessionState.SetString(InstallQueueKey, value);
        }

        /// <summary>
        /// Parses the reload-safe queue and drops malformed or no-longer-supported package entries after upgrades.
        /// </summary>
        private static List<PackageReference> ReadQueue()
        {
            var value = SessionState.GetString(InstallQueueKey, string.Empty);
            var result = new List<PackageReference>();
            foreach (var line in value.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var fields = line.Split(new[] { '\t' }, 2);
                if (fields.Length == 2 && !string.IsNullOrWhiteSpace(fields[1])
                    && (fields[0] == YooAssetPackageId || fields[0] == YooAssetBackend.Id))
                {
                    result.Add(new PackageReference(fields[0], fields[1]));
                }
            }

            return result;
        }

        /// <summary>
        /// Builds a monorepo subpath URL pinned to the same Foundation release as this installer.
        /// </summary>
        private static string FoundationPackageUrl(string packageId)
        {
            return $"{FoundationRepository}?path=/Packages/{packageId}#{FoundationVersion}";
        }

        private readonly struct PackageReference
        {
            public string Id { get; }
            public string Url { get; }

            /// <summary>
            /// Stores one deterministic UPM package target for the reload-safe queue.
            /// </summary>
            public PackageReference(string id, string url)
            {
                Id = id;
                Url = url;
            }
        }
    }
}
