using System;
using System.Threading;
using System.Threading.Tasks;
using GameFoundation.HotUpdate;

namespace GameFoundation.Samples.HotUpdate
{
    public interface IProjectContentBridge
    {
        /// <summary>
        /// Converts a project provider version check into a Foundation update plan.
        /// </summary>
        Task<ContentUpdatePlan> CheckAsync(ContentUpdateRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Downloads provider content while reporting Foundation progress snapshots.
        /// </summary>
        Task DownloadAsync(ContentUpdatePlan plan, IProgress<ContentUpdateProgress> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Verifies the provider-owned staging content before commit.
        /// </summary>
        Task VerifyAsync(ContentUpdatePlan plan, CancellationToken cancellationToken);

        /// <summary>
        /// Activates the verified provider version.
        /// </summary>
        Task CommitAsync(ContentUpdatePlan plan, CancellationToken cancellationToken);

        /// <summary>
        /// Removes or abandons provider staging content after failure.
        /// </summary>
        Task RollbackAsync(ContentUpdatePlan plan, CancellationToken cancellationToken);

        /// <summary>
        /// Reports whether the project can continue with its previously committed content.
        /// </summary>
        bool HasUsableLocalContent(ContentUpdateRequest request);
    }

    public sealed class ProjectContentUpdateBackend : IContentUpdateBackend
    {
        private readonly IProjectContentBridge mBridge;

        /// <summary>
        /// Creates a backend that keeps vendor packages and CDN configuration project-owned.
        /// </summary>
        public ProjectContentUpdateBackend(IProjectContentBridge bridge)
        {
            mBridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        }

        /// <summary>
        /// Delegates version discovery to the project provider bridge.
        /// </summary>
        public async Task<ContentUpdatePlan> CheckAsync(
            ContentUpdateRequest request,
            CancellationToken cancellationToken)
        {
            var plan = await mBridge.CheckAsync(request, cancellationToken);
            return plan ?? throw new InvalidOperationException("The content bridge returned a null update plan.");
        }

        /// <summary>
        /// Delegates provider download work without polling from Unity Update.
        /// </summary>
        public Task DownloadAsync(
            ContentUpdatePlan plan,
            IProgress<ContentUpdateProgress> progress,
            CancellationToken cancellationToken)
        {
            return mBridge.DownloadAsync(plan, progress, cancellationToken);
        }

        /// <summary>
        /// Delegates integrity and version checks to the provider bridge.
        /// </summary>
        public Task VerifyAsync(ContentUpdatePlan plan, CancellationToken cancellationToken)
        {
            return mBridge.VerifyAsync(plan, cancellationToken);
        }

        /// <summary>
        /// Delegates the provider-specific atomic activation step.
        /// </summary>
        public Task CommitAsync(ContentUpdatePlan plan, CancellationToken cancellationToken)
        {
            return mBridge.CommitAsync(plan, cancellationToken);
        }

        /// <summary>
        /// Delegates staging cleanup while the Foundation service preserves the original error.
        /// </summary>
        public Task RollbackAsync(ContentUpdatePlan plan, CancellationToken cancellationToken)
        {
            return mBridge.RollbackAsync(plan, cancellationToken);
        }

        /// <summary>
        /// Delegates the offline-fallback decision to the provider that owns local content.
        /// </summary>
        public bool HasUsableLocalContent(ContentUpdateRequest request)
        {
            return mBridge.HasUsableLocalContent(request);
        }
    }
}
