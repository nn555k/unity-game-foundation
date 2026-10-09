using System;
using System.Threading;
using System.Threading.Tasks;
using QFramework;

namespace GameFoundation.HotUpdate
{
    public interface IContentUpdateBackend : IUtility
    {
        /// <summary>
        /// 检查内容组并返回确定的更新计划。
        /// </summary>
        Task<ContentUpdatePlan> CheckAsync(ContentUpdateRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// 下载计划内容并报告字节级进度。
        /// </summary>
        Task DownloadAsync(ContentUpdatePlan plan, IProgress<ContentUpdateProgress> progress, CancellationToken cancellationToken);

        /// <summary>
        /// 校验已下载内容的完整性与版本。
        /// </summary>
        Task VerifyAsync(ContentUpdatePlan plan, CancellationToken cancellationToken);

        /// <summary>
        /// 原子切换到已验证的新内容。
        /// </summary>
        Task CommitAsync(ContentUpdatePlan plan, CancellationToken cancellationToken);

        /// <summary>
        /// 清理未提交内容并恢复到之前可用状态。
        /// </summary>
        Task RollbackAsync(ContentUpdatePlan plan, CancellationToken cancellationToken);

        /// <summary>
        /// 判断失败后是否仍有可供离线启动的本地内容。
        /// </summary>
        bool HasUsableLocalContent(ContentUpdateRequest request);
    }
}
