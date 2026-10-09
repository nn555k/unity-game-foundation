using System.Threading;
using System.Threading.Tasks;

namespace GameFoundation.Sdk
{
    public interface ISdkAdapter
    {
        string Id { get; }
        int Order { get; }
        bool RequiresConsent { get; }

        /// <summary>
        /// 在 Host 管理的超时和取消边界内初始化供应商 SDK。
        /// </summary>
        Task InitializeAsync(SdkInitializationContext context, CancellationToken cancellationToken);

        /// <summary>
        /// 设置或清空供应商使用的项目用户标识。
        /// </summary>
        void SetUserId(string userId);

        /// <summary>
        /// 接收应用焦点生命周期变化。
        /// </summary>
        void OnApplicationFocusChanged(bool hasFocus);

        /// <summary>
        /// 接收应用暂停生命周期变化。
        /// </summary>
        void OnApplicationPauseChanged(bool isPaused);
    }
}
