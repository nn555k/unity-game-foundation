using System;
using GameFoundation.Core;
using QFramework;

namespace GameFoundation.UI
{
    public static class FoundationUiModule
    {
        /// <summary>
        /// 注册不包含具体页面、弹窗或业务流程的通用 UI 导航服务。
        /// </summary>
        public static void Register(IArchitecture architecture)
        {
            if (architecture == null)
            {
                throw new ArgumentNullException(nameof(architecture));
            }

            FoundationCoreModule.Register(architecture);
            if (architecture.GetUtility<UiPopupRouter>() == null)
            {
                architecture.RegisterUtility(new UiPopupRouter());
            }

            if (architecture.GetUtility<UiScreenNavigator>() == null)
            {
                architecture.RegisterUtility(new UiScreenNavigator());
            }
        }
    }
}
