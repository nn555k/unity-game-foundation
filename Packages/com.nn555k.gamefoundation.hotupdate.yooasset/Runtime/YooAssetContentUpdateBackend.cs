using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameFoundation.HotUpdate.Providers.YooAsset
{
    public sealed class YooAssetContentUpdateBackend : IContentUpdateBackend
    {
        private readonly YooAssetContentUpdateOptions mOptions;
        private global::YooAsset.ResourcePackage mPackage;

        /// <summary>
        /// Creates a concrete Foundation backend while leaving package name, CDN and encryption policy project-owned.
        /// </summary>
        public YooAssetContentUpdateBackend(YooAssetContentUpdateOptions options)
        {
            mOptions = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Initializes the package, requests the remote version and builds a non-active pre-download plan.
        /// </summary>
        public async Task<ContentUpdatePlan> CheckAsync(
            ContentUpdateRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            cancellationToken.ThrowIfCancellationRequested();
            await EnsureInitializedAsync(cancellationToken);
            var currentVersion = mPackage.PackageValid ? mPackage.GetPackageVersion() : string.Empty;
            if (mOptions.PlayMode != FoundationYooAssetPlayMode.Host)
            {
                return ContentUpdatePlan.UpToDate(currentVersion);
            }

            var versionOperation = mPackage.RequestPackageVersionAsync(
                true,
                mOptions.RequestTimeoutSeconds);
            await AwaitSuccessfulOperationAsync(versionOperation, "request package version", cancellationToken);

            var targetVersion = versionOperation.PackageVersion ?? string.Empty;
            var preDownloadOperation = mPackage.PreDownloadContentAsync(
                targetVersion,
                mOptions.RequestTimeoutSeconds);
            await AwaitSuccessfulOperationAsync(preDownloadOperation, "pre-download manifest", cancellationToken);

            var downloader = mOptions.DownloadTags.Length == 0
                ? preDownloadOperation.CreateResourceDownloader(
                    mOptions.DownloadingMaxNumber,
                    mOptions.FailedTryAgain)
                : preDownloadOperation.CreateResourceDownloader(
                    mOptions.DownloadTags,
                    mOptions.DownloadingMaxNumber,
                    mOptions.FailedTryAgain);
            var context = new YooAssetUpdateContext(
                this,
                currentVersion,
                targetVersion,
                downloader);
            var hasUpdate = downloader.TotalDownloadCount > 0 ||
                            !string.Equals(currentVersion, targetVersion, StringComparison.Ordinal);
            return new ContentUpdatePlan(
                hasUpdate,
                currentVersion,
                targetVersion,
                downloader.TotalDownloadBytes,
                context);
        }

        /// <summary>
        /// Starts the YooAsset downloader once and forwards byte-level progress to the Foundation UI contract.
        /// </summary>
        public async Task DownloadAsync(
            ContentUpdatePlan plan,
            IProgress<ContentUpdateProgress> progress,
            CancellationToken cancellationToken)
        {
            var context = RequireContext(plan);
            cancellationToken.ThrowIfCancellationRequested();
            if (context.Downloader.TotalDownloadCount == 0)
            {
                context.DownloadSucceeded = true;
                progress?.Report(new ContentUpdateProgress(
                    ContentUpdateStage.Downloading,
                    0L,
                    0L,
                    context.TargetVersion));
                return;
            }

            context.Downloader.DownloadUpdateCallback = data =>
                progress?.Report(new ContentUpdateProgress(
                    ContentUpdateStage.Downloading,
                    data.CurrentDownloadBytes,
                    data.TotalDownloadBytes,
                    context.TargetVersion));

            using (cancellationToken.Register(context.Downloader.CancelDownload))
            {
                context.Downloader.BeginDownload();
                await AwaitSuccessfulOperationAsync(
                    context.Downloader,
                    "download content",
                    cancellationToken);
            }

            context.DownloadSucceeded = true;
        }

        /// <summary>
        /// Confirms YooAsset completed its per-file download verification before manifest activation.
        /// </summary>
        public Task VerifyAsync(ContentUpdatePlan plan, CancellationToken cancellationToken)
        {
            var context = RequireContext(plan);
            cancellationToken.ThrowIfCancellationRequested();
            var downloaderFailed = context.Downloader.TotalDownloadCount > 0 &&
                                   context.Downloader.Status != global::YooAsset.EOperationStatus.Succeed;
            if (!context.DownloadSucceeded || downloaderFailed)
            {
                throw new InvalidOperationException(
                    "YooAsset downloader did not finish successfully, so the content cannot be committed.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Activates the already downloaded target manifest as the single commit point.
        /// </summary>
        public async Task CommitAsync(ContentUpdatePlan plan, CancellationToken cancellationToken)
        {
            var context = RequireContext(plan);
            cancellationToken.ThrowIfCancellationRequested();
            var operation = mPackage.UpdatePackageManifestAsync(
                context.TargetVersion,
                mOptions.RequestTimeoutSeconds);
            await AwaitSuccessfulOperationAsync(operation, "commit package manifest", CancellationToken.None);
            context.Committed = true;
        }

        /// <summary>
        /// Cancels incomplete downloads or restores the previous manifest if commit was externally rolled back.
        /// </summary>
        public async Task RollbackAsync(ContentUpdatePlan plan, CancellationToken cancellationToken)
        {
            var context = RequireContext(plan);
            if (!context.Downloader.IsDone)
            {
                context.Downloader.CancelDownload();
            }

            if (!context.Committed || string.IsNullOrWhiteSpace(context.CurrentVersion))
            {
                return;
            }

            var operation = mPackage.UpdatePackageManifestAsync(
                context.CurrentVersion,
                mOptions.RequestTimeoutSeconds);
            await AwaitSuccessfulOperationAsync(operation, "restore previous package manifest", cancellationToken);
            context.Committed = false;
        }

        /// <summary>
        /// Reports fallback availability only when YooAsset has an active local manifest.
        /// </summary>
        public bool HasUsableLocalContent(ContentUpdateRequest request)
        {
            return request != null && mPackage != null && mPackage.PackageValid;
        }

        /// <summary>
        /// Initializes YooAsset exactly once and reuses the named package when another project system created it first.
        /// </summary>
        private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            if (!global::YooAsset.YooAssets.Initialized)
            {
                global::YooAsset.YooAssets.Initialize();
            }

            mPackage = global::YooAsset.YooAssets.TryGetPackage(mOptions.PackageName) ??
                       global::YooAsset.YooAssets.CreatePackage(mOptions.PackageName);
            if (mPackage.InitializeStatus == global::YooAsset.EOperationStatus.Succeed)
            {
                return;
            }

            var operation = mPackage.InitializeAsync(CreateInitializeParameters());
            await AwaitSuccessfulOperationAsync(operation, "initialize package", cancellationToken);
        }

        /// <summary>
        /// Builds only the file-system configuration for the selected mode and never embeds project URLs or keys.
        /// </summary>
        private global::YooAsset.InitializeParameters CreateInitializeParameters()
        {
            switch (mOptions.PlayMode)
            {
                case FoundationYooAssetPlayMode.EditorSimulate:
                    return new global::YooAsset.EditorSimulateModeParameters
                    {
                        EditorFileSystemParameters =
                            global::YooAsset.FileSystemParameters.CreateDefaultEditorFileSystemParameters(
                                mOptions.EditorSimulatePackageRoot)
                    };
                case FoundationYooAssetPlayMode.Offline:
                    return new global::YooAsset.OfflinePlayModeParameters
                    {
                        BuildinFileSystemParameters =
                            global::YooAsset.FileSystemParameters.CreateDefaultBuildinFileSystemParameters(
                                mOptions.DecryptionServices)
                    };
                case FoundationYooAssetPlayMode.Host:
                    var remoteServices = new RemoteServices(
                        mOptions.RemoteMainUrl,
                        mOptions.RemoteFallbackUrl);
                    return new global::YooAsset.HostPlayModeParameters
                    {
                        BuildinFileSystemParameters =
                            global::YooAsset.FileSystemParameters.CreateDefaultBuildinFileSystemParameters(
                                mOptions.DecryptionServices),
                        CacheFileSystemParameters =
                            global::YooAsset.FileSystemParameters.CreateDefaultCacheFileSystemParameters(
                                remoteServices,
                                mOptions.DecryptionServices)
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Rejects plans produced by another backend instance to prevent cross-package state corruption.
        /// </summary>
        private YooAssetUpdateContext RequireContext(ContentUpdatePlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (!(plan.BackendContext is YooAssetUpdateContext context) ||
                !ReferenceEquals(context.Owner, this))
            {
                throw new InvalidOperationException(
                    "The content plan was not created by this YooAsset backend instance.");
            }

            return context;
        }

        /// <summary>
        /// Converts YooAsset's status-bearing Task into the exception semantics expected by Foundation orchestration.
        /// </summary>
        private static async Task AwaitSuccessfulOperationAsync(
            global::YooAsset.AsyncOperationBase operation,
            string stage,
            CancellationToken cancellationToken)
        {
            var cancellation = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => cancellation.TrySetResult(true)))
            {
                var completed = await Task.WhenAny(operation.Task, cancellation.Task);
                if (completed != operation.Task)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                await operation.Task;
            }

            if (operation.Status != global::YooAsset.EOperationStatus.Succeed)
            {
                throw new InvalidOperationException(
                    $"YooAsset failed to {stage}: {operation.Error}");
            }
        }

        private sealed class YooAssetUpdateContext
        {
            public YooAssetContentUpdateBackend Owner { get; }
            public string CurrentVersion { get; }
            public string TargetVersion { get; }
            public global::YooAsset.ResourceDownloaderOperation Downloader { get; }
            public bool DownloadSucceeded { get; set; }
            public bool Committed { get; set; }

            /// <summary>
            /// Keeps vendor operation handles private to the backend that created the Foundation plan.
            /// </summary>
            public YooAssetUpdateContext(
                YooAssetContentUpdateBackend owner,
                string currentVersion,
                string targetVersion,
                global::YooAsset.ResourceDownloaderOperation downloader)
            {
                Owner = owner;
                CurrentVersion = currentVersion ?? string.Empty;
                TargetVersion = targetVersion ?? string.Empty;
                Downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
            }
        }

        private sealed class RemoteServices : global::YooAsset.IRemoteServices
        {
            private readonly string mMainUrl;
            private readonly string mFallbackUrl;

            /// <summary>
            /// Stores normalized base URLs once so download callbacks allocate only their final path strings.
            /// </summary>
            public RemoteServices(string mainUrl, string fallbackUrl)
            {
                mMainUrl = mainUrl;
                mFallbackUrl = string.IsNullOrEmpty(fallbackUrl) ? mainUrl : fallbackUrl;
            }

            /// <summary>
            /// Returns the primary CDN URL for one YooAsset file.
            /// </summary>
            public string GetRemoteMainURL(string fileName)
            {
                return $"{mMainUrl}/{fileName}";
            }

            /// <summary>
            /// Returns the fallback origin URL for one YooAsset file.
            /// </summary>
            public string GetRemoteFallbackURL(string fileName)
            {
                return $"{mFallbackUrl}/{fileName}";
            }
        }
    }
}
