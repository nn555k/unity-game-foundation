using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameFoundation.HotUpdate
{
    public sealed class NoOpContentUpdateBackend : IContentUpdateBackend
    {
        /// <summary>
        /// 在未配置热更供应商时返回已是最新版本。
        /// </summary>
        public Task<ContentUpdatePlan> CheckAsync(ContentUpdateRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ContentUpdatePlan.UpToDate());
        }

        /// <summary>
        /// 无更新计划时下载阶段不执行任何工作。
        /// </summary>
        public Task DownloadAsync(ContentUpdatePlan plan, IProgress<ContentUpdateProgress> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 无更新计划时校验阶段直接完成。
        /// </summary>
        public Task VerifyAsync(ContentUpdatePlan plan, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 无更新计划时提交阶段直接完成。
        /// </summary>
        public Task CommitAsync(ContentUpdatePlan plan, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 无更新计划时回滚阶段直接完成。
        /// </summary>
        public Task RollbackAsync(ContentUpdatePlan plan, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        /// <summary>
        /// No-op 后端默认认为包内内容可用。
        /// </summary>
        public bool HasUsableLocalContent(ContentUpdateRequest request)
        {
            return true;
        }
    }
}
