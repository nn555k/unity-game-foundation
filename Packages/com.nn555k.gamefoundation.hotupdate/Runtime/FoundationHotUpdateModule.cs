using System;
using GameFoundation.Core;
using QFramework;

namespace GameFoundation.HotUpdate
{
    public static class FoundationHotUpdateModule
    {
        /// <summary>
        /// 注册热更后端和编排服务，未提供后端时使用可离线运行的 No-op 实现。
        /// </summary>
        public static void Register(IArchitecture architecture, IContentUpdateBackend backend = null)
        {
            if (architecture == null)
            {
                throw new ArgumentNullException(nameof(architecture));
            }

            FoundationCoreModule.Register(architecture);
            var resolvedBackend = backend ?? architecture.GetUtility<IContentUpdateBackend>() ?? new NoOpContentUpdateBackend();
            if (architecture.GetUtility<IContentUpdateBackend>() == null)
            {
                architecture.RegisterUtility<IContentUpdateBackend>(resolvedBackend);
            }

            if (architecture.GetUtility<IHotUpdateService>() == null)
            {
                architecture.RegisterUtility<IHotUpdateService>(
                    new HotUpdateService(resolvedBackend, architecture.GetUtility<IFoundationLogger>()));
            }
        }
    }
}
