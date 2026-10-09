using UnityEngine;

namespace GameFoundation.Sdk
{
    [DisallowMultipleComponent]
    public sealed class FoundationSdkLifecycleController : MonoBehaviour
    {
        private ISdkHost mHost;

        /// <summary>
        /// 由项目启动组件注入 SDK Host，避免该组件创建全局单例或猜测架构入口。
        /// </summary>
        public void Bind(ISdkHost host)
        {
            mHost = host;
        }

        /// <summary>
        /// 将 Unity 焦点回调转发给已绑定的 SDK Host。
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            mHost?.HandleApplicationFocus(hasFocus);
        }

        /// <summary>
        /// 将 Unity 暂停回调转发给已绑定的 SDK Host。
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            mHost?.HandleApplicationPause(pauseStatus);
        }
    }
}
