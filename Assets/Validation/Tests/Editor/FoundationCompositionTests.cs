using GameFoundation.Core;
using GameFoundation.HotUpdate;
using GameFoundation.Save;
using GameFoundation.Sdk;
using GameFoundation.UI;
using NUnit.Framework;

namespace GameFoundation.Validation.Tests
{
    public sealed class FoundationCompositionTests
    {
        /// <summary>
        /// 验证独立项目可以在同一个 QFramework 架构中组合全部默认模块。
        /// </summary>
        [Test]
        public void ConsumerArchitectureRegistersAllFoundationModules()
        {
            var architecture = FoundationValidationApp.Interface;

            Assert.That(architecture.GetUtility<IFoundationClock>(), Is.Not.Null);
            Assert.That(architecture.GetUtility<ISaveStore>(), Is.Not.Null);
            Assert.That(architecture.GetUtility<IHotUpdateService>(), Is.Not.Null);
            Assert.That(architecture.GetUtility<ISdkHost>(), Is.Not.Null);
            Assert.That(architecture.GetUtility<UiPopupRouter>(), Is.Not.Null);
            Assert.That(architecture.GetUtility<UiScreenNavigator>(), Is.Not.Null);
        }
    }
}
