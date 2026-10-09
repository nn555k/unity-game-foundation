using System;
using QFramework;

namespace GameFoundation.Core
{
    public static class FoundationCoreModule
    {
        /// <summary>
        /// 将通用时钟与日志能力注册到调用方拥有的 QFramework 架构中。
        /// </summary>
        public static void Register(IArchitecture architecture, FoundationCoreOptions options = null)
        {
            if (architecture == null)
            {
                throw new ArgumentNullException(nameof(architecture));
            }

            options = options ?? FoundationCoreOptions.Default();
            if (options.RegisterDefaultClock && architecture.GetUtility<IFoundationClock>() == null)
            {
                architecture.RegisterUtility<IFoundationClock>(new SystemFoundationClock());
            }

            if (options.RegisterDefaultLogger && architecture.GetUtility<IFoundationLogger>() == null)
            {
                architecture.RegisterUtility<IFoundationLogger>(new UnityFoundationLogger());
            }
        }
    }
}
