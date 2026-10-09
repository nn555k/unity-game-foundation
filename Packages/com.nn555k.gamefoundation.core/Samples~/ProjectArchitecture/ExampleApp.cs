using GameFoundation.Core;
using QFramework;

namespace GameFoundation.Samples
{
    public sealed class ExampleApp : Architecture<ExampleApp>
    {
        /// <summary>
        /// 注册通用基础设施；项目自己的模型和系统应继续在此入口注册。
        /// </summary>
        protected override void Init()
        {
            FoundationCoreModule.Register(this);
        }
    }
}
