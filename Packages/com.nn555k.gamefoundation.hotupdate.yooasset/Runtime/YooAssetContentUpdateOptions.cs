using System;

namespace GameFoundation.HotUpdate.Providers.YooAsset
{
    public enum FoundationYooAssetPlayMode
    {
        EditorSimulate,
        Offline,
        Host
    }

    public sealed class YooAssetContentUpdateOptions
    {
        public string PackageName { get; }
        public FoundationYooAssetPlayMode PlayMode { get; }
        public string RemoteMainUrl { get; }
        public string RemoteFallbackUrl { get; }
        public string EditorSimulatePackageRoot { get; }
        public string[] DownloadTags { get; }
        public int DownloadingMaxNumber { get; }
        public int FailedTryAgain { get; }
        public int RequestTimeoutSeconds { get; }
        public global::YooAsset.IDecryptionServices DecryptionServices { get; }

        /// <summary>
        /// Stores immutable YooAsset initialization and download policy without owning project CDN credentials.
        /// </summary>
        private YooAssetContentUpdateOptions(
            string packageName,
            FoundationYooAssetPlayMode playMode,
            string remoteMainUrl,
            string remoteFallbackUrl,
            string editorSimulatePackageRoot,
            string[] downloadTags,
            int downloadingMaxNumber,
            int failedTryAgain,
            int requestTimeoutSeconds,
            global::YooAsset.IDecryptionServices decryptionServices)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                throw new ArgumentException("YooAsset package name cannot be empty.", nameof(packageName));
            }

            if (downloadingMaxNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(downloadingMaxNumber));
            }

            if (failedTryAgain < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(failedTryAgain));
            }

            if (requestTimeoutSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestTimeoutSeconds));
            }

            PackageName = packageName.Trim();
            PlayMode = playMode;
            RemoteMainUrl = NormalizeBaseUrl(remoteMainUrl);
            RemoteFallbackUrl = NormalizeBaseUrl(
                string.IsNullOrWhiteSpace(remoteFallbackUrl) ? remoteMainUrl : remoteFallbackUrl);
            EditorSimulatePackageRoot = editorSimulatePackageRoot?.Trim() ?? string.Empty;
            DownloadTags = NormalizeTags(downloadTags);
            DownloadingMaxNumber = downloadingMaxNumber;
            FailedTryAgain = failedTryAgain;
            RequestTimeoutSeconds = requestTimeoutSeconds;
            DecryptionServices = decryptionServices;

            if (PlayMode == FoundationYooAssetPlayMode.Host && string.IsNullOrEmpty(RemoteMainUrl))
            {
                throw new ArgumentException("Host mode requires a remote main URL.", nameof(remoteMainUrl));
            }

            if (PlayMode == FoundationYooAssetPlayMode.EditorSimulate && string.IsNullOrEmpty(EditorSimulatePackageRoot))
            {
                throw new ArgumentException(
                    "Editor simulate mode requires the package root returned by the YooAsset editor simulation build.",
                    nameof(editorSimulatePackageRoot));
            }
        }

        /// <summary>
        /// Creates host-mode settings that check a remote version and pre-download content before manifest activation.
        /// </summary>
        public static YooAssetContentUpdateOptions Host(
            string packageName,
            string remoteMainUrl,
            string remoteFallbackUrl = null,
            string[] downloadTags = null,
            int downloadingMaxNumber = 8,
            int failedTryAgain = 2,
            int requestTimeoutSeconds = 60,
            global::YooAsset.IDecryptionServices decryptionServices = null)
        {
            return new YooAssetContentUpdateOptions(
                packageName,
                FoundationYooAssetPlayMode.Host,
                remoteMainUrl,
                remoteFallbackUrl,
                null,
                downloadTags,
                downloadingMaxNumber,
                failedTryAgain,
                requestTimeoutSeconds,
                decryptionServices);
        }

        /// <summary>
        /// Creates offline settings for content shipped entirely inside the player build.
        /// </summary>
        public static YooAssetContentUpdateOptions Offline(
            string packageName,
            global::YooAsset.IDecryptionServices decryptionServices = null)
        {
            return new YooAssetContentUpdateOptions(
                packageName,
                FoundationYooAssetPlayMode.Offline,
                null,
                null,
                null,
                null,
                1,
                0,
                60,
                decryptionServices);
        }

        /// <summary>
        /// Creates editor-simulate settings without adding UnityEditor dependencies to the runtime assembly.
        /// </summary>
        public static YooAssetContentUpdateOptions EditorSimulate(
            string packageName,
            string packageRoot)
        {
            return new YooAssetContentUpdateOptions(
                packageName,
                FoundationYooAssetPlayMode.EditorSimulate,
                null,
                null,
                packageRoot,
                null,
                1,
                0,
                60,
                null);
        }

        /// <summary>
        /// Removes trailing separators so all provider-generated URLs have one deterministic slash.
        /// </summary>
        private static string NormalizeBaseUrl(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().TrimEnd('/');
        }

        /// <summary>
        /// Removes empty and duplicate tags once, avoiding repeated allocations during content checks.
        /// </summary>
        private static string[] NormalizeTags(string[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return Array.Empty<string>();
            }

            var normalized = new string[tags.Length];
            var count = 0;
            for (var i = 0; i < tags.Length; i++)
            {
                var tag = tags[i]?.Trim();
                if (string.IsNullOrEmpty(tag))
                {
                    continue;
                }

                var duplicate = false;
                for (var existingIndex = 0; existingIndex < count; existingIndex++)
                {
                    if (!string.Equals(normalized[existingIndex], tag, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    duplicate = true;
                    break;
                }

                if (!duplicate)
                {
                    normalized[count++] = tag;
                }
            }

            if (count == normalized.Length)
            {
                return normalized;
            }

            var result = new string[count];
            Array.Copy(normalized, result, count);
            return result;
        }
    }
}
