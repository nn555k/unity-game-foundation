using System;
using System.Threading;
using System.Threading.Tasks;
using QFramework;

namespace GameFoundation.HotUpdate
{
    public interface IHotUpdateService : IUtility
    {
        ContentUpdateStage Stage { get; }
        event Action<ContentUpdateProgress> ProgressChanged;

        /// <summary>
        /// 执行一次互斥的内容准备流程并返回可供启动层处理的结果。
        /// </summary>
        Task<ContentUpdateResult> PrepareAsync(ContentUpdateRequest request, CancellationToken cancellationToken = default);
    }
}
