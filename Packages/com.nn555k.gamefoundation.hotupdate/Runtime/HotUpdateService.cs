using System;
using System.Threading;
using System.Threading.Tasks;
using GameFoundation.Core;

namespace GameFoundation.HotUpdate
{
    public sealed class HotUpdateService : IHotUpdateService
    {
        private const string LogCategory = "Foundation.HotUpdate";

        private readonly IContentUpdateBackend mBackend;
        private readonly IFoundationLogger mLogger;
        private readonly SemaphoreSlim mOperationGate = new SemaphoreSlim(1, 1);

        public ContentUpdateStage Stage { get; private set; } = ContentUpdateStage.Idle;
        public event Action<ContentUpdateProgress> ProgressChanged;

        /// <summary>
        /// 创建由后端实现具体下载机制的通用更新编排器。
        /// </summary>
        public HotUpdateService(IContentUpdateBackend backend, IFoundationLogger logger = null)
        {
            mBackend = backend ?? throw new ArgumentNullException(nameof(backend));
            mLogger = logger;
        }

        /// <summary>
        /// 串行执行检查、下载、校验和提交，任何提交前失败都会尝试回滚。
        /// </summary>
        public async Task<ContentUpdateResult> PrepareAsync(
            ContentUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!await mOperationGate.WaitAsync(0, cancellationToken))
            {
                return new ContentUpdateResult(ContentUpdateResultCode.Busy, string.Empty, "Another content update is running.");
            }

            ContentUpdatePlan plan = null;
            try
            {
                Report(ContentUpdateStage.Checking, 0L, 0L, request.ContentGroup);
                plan = await mBackend.CheckAsync(request, cancellationToken);
                if (plan == null)
                {
                    throw new InvalidOperationException("Content backend returned a null plan.");
                }

                if (!plan.HasUpdate)
                {
                    Report(ContentUpdateStage.Completed, 0L, 0L, plan.CurrentVersion);
                    return new ContentUpdateResult(ContentUpdateResultCode.AlreadyCurrent, plan.CurrentVersion, string.Empty);
                }

                Report(ContentUpdateStage.Downloading, 0L, plan.DownloadBytes, plan.TargetVersion);
                var progress = new Progress<ContentUpdateProgress>(ForwardProgress);
                await mBackend.DownloadAsync(plan, progress, cancellationToken);

                Report(ContentUpdateStage.Verifying, plan.DownloadBytes, plan.DownloadBytes, plan.TargetVersion);
                await mBackend.VerifyAsync(plan, cancellationToken);

                Report(ContentUpdateStage.Committing, plan.DownloadBytes, plan.DownloadBytes, plan.TargetVersion);
                await mBackend.CommitAsync(plan, cancellationToken);

                Report(ContentUpdateStage.Completed, plan.DownloadBytes, plan.DownloadBytes, plan.TargetVersion);
                return new ContentUpdateResult(ContentUpdateResultCode.Updated, plan.TargetVersion, string.Empty);
            }
            catch (OperationCanceledException)
            {
                await TryRollbackAsync(plan);
                Report(ContentUpdateStage.Canceled, 0L, plan?.DownloadBytes ?? 0L, "Canceled");
                return new ContentUpdateResult(ContentUpdateResultCode.Canceled, plan?.CurrentVersion, "Content update was canceled.");
            }
            catch (Exception exception)
            {
                await TryRollbackAsync(plan);
                if (request.AllowOfflineFallback && mBackend.HasUsableLocalContent(request))
                {
                    mLogger?.Warning(LogCategory, $"Using local content after update failure: {exception.Message}");
                    Report(ContentUpdateStage.Completed, 0L, 0L, "Offline fallback");
                    return new ContentUpdateResult(ContentUpdateResultCode.OfflineFallback, plan?.CurrentVersion, exception.Message);
                }

                mLogger?.Error(LogCategory, exception.ToString());
                Report(ContentUpdateStage.Failed, 0L, plan?.DownloadBytes ?? 0L, exception.Message);
                return new ContentUpdateResult(ContentUpdateResultCode.Failed, plan?.CurrentVersion, exception.Message);
            }
            finally
            {
                mOperationGate.Release();
            }
        }

        /// <summary>
        /// 转发后端下载进度，并同步服务当前阶段。
        /// </summary>
        private void ForwardProgress(ContentUpdateProgress progress)
        {
            if (progress == null)
            {
                return;
            }

            Stage = progress.Stage;
            ProgressChanged?.Invoke(progress);
        }

        /// <summary>
        /// 更新当前阶段并向 UI 或诊断层广播进度快照。
        /// </summary>
        private void Report(ContentUpdateStage stage, long downloadedBytes, long totalBytes, string message)
        {
            Stage = stage;
            ProgressChanged?.Invoke(new ContentUpdateProgress(stage, downloadedBytes, totalBytes, message));
        }

        /// <summary>
        /// 在已有计划时尝试回滚，回滚异常仅记录而不覆盖原始失败原因。
        /// </summary>
        private async Task TryRollbackAsync(ContentUpdatePlan plan)
        {
            if (plan == null)
            {
                return;
            }

            try
            {
                Report(ContentUpdateStage.RollingBack, 0L, plan.DownloadBytes, plan.CurrentVersion);
                await mBackend.RollbackAsync(plan, CancellationToken.None);
            }
            catch (Exception exception)
            {
                mLogger?.Error(LogCategory, $"Rollback failed: {exception}");
            }
        }
    }
}
