using System.Threading;
using System.Threading.Tasks;

namespace GameFoundation.Sdk
{
    public sealed class NoOpSdkAdapter : ISdkAdapter
    {
        public string Id { get; }
        public int Order { get; }
        public bool RequiresConsent { get; }

        /// <summary>
        /// 创建用于 Editor、测试或未安装供应商 SDK 环境的空实现。
        /// </summary>
        public NoOpSdkAdapter(string id = "noop", int order = 0, bool requiresConsent = false)
        {
            Id = id;
            Order = order;
            RequiresConsent = requiresConsent;
        }

        /// <summary>
        /// 空实现立即完成初始化。
        /// </summary>
        public Task InitializeAsync(SdkInitializationContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 空实现忽略用户标识。
        /// </summary>
        public void SetUserId(string userId)
        {
        }

        /// <summary>
        /// 空实现忽略应用焦点变化。
        /// </summary>
        public void OnApplicationFocusChanged(bool hasFocus)
        {
        }

        /// <summary>
        /// 空实现忽略应用暂停变化。
        /// </summary>
        public void OnApplicationPauseChanged(bool isPaused)
        {
        }
    }
}
