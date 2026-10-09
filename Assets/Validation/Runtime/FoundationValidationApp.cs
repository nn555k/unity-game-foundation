using GameFoundation.Core;
using GameFoundation.HotUpdate;
using GameFoundation.Save;
using GameFoundation.Sdk;
using GameFoundation.UI;
using QFramework;
using UnityEngine;

namespace GameFoundation.Validation
{
    public sealed class FoundationValidationApp : Architecture<FoundationValidationApp>
    {
        /// <summary>
        /// 注册所有无供应商依赖的 Foundation 默认实现。
        /// </summary>
        protected override void Init()
        {
            FoundationCoreModule.Register(this);
            FoundationSaveModule.Register(this, new MemorySaveStore());
            FoundationHotUpdateModule.Register(this);
            FoundationSdkModule.Register(this);
            FoundationUiModule.Register(this);
        }

        /// <summary>
        /// Player 启动时验证组合根可建立，供平台构建后的烟雾运行检查使用。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void VerifyRuntimeComposition()
        {
            var architecture = Interface;
            if (architecture.GetUtility<IFoundationClock>() == null ||
                architecture.GetUtility<ISaveStore>() == null ||
                architecture.GetUtility<IHotUpdateService>() == null ||
                architecture.GetUtility<ISdkHost>() == null ||
                architecture.GetUtility<UiPopupRouter>() == null)
            {
                Debug.LogError("[GameFoundation.Validation] Runtime composition is incomplete.");
                return;
            }

            Debug.Log("[GameFoundation.Validation] Runtime composition passed.");
        }
    }
}
