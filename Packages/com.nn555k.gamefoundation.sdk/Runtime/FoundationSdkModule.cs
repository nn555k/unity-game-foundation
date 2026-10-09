using System;
using System.Collections.Generic;
using GameFoundation.Core;
using QFramework;

namespace GameFoundation.Sdk
{
    public static class FoundationSdkModule
    {
        /// <summary>
        /// 注册 SDK Host；供应商列表为空时仍提供可查询、可运行的空 Host。
        /// </summary>
        public static void Register(IArchitecture architecture, IEnumerable<ISdkAdapter> adapters = null)
        {
            if (architecture == null)
            {
                throw new ArgumentNullException(nameof(architecture));
            }

            FoundationCoreModule.Register(architecture);
            if (architecture.GetUtility<ISdkHost>() == null)
            {
                architecture.RegisterUtility<ISdkHost>(
                    new SdkHost(adapters, architecture.GetUtility<IFoundationLogger>()));
            }
        }
    }
}
