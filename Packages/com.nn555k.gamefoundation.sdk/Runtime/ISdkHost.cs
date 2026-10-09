using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QFramework;

namespace GameFoundation.Sdk
{
    public interface ISdkHost : IUtility
    {
        event Action<SdkAdapterStatus> StatusChanged;
        IReadOnlyList<SdkAdapterStatus> Statuses { get; }

        /// <summary>
        /// 按稳定顺序初始化所有符合授权条件的 Adapter。
        /// </summary>
        Task InitializeAsync(SdkInitializationContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// 向所有已就绪 Adapter 分发用户标识。
        /// </summary>
        void SetUserId(string userId);

        /// <summary>
        /// 向所有已就绪 Adapter 分发焦点变化。
        /// </summary>
        void HandleApplicationFocus(bool hasFocus);

        /// <summary>
        /// 向所有已就绪 Adapter 分发暂停变化。
        /// </summary>
        void HandleApplicationPause(bool isPaused);
    }
}
