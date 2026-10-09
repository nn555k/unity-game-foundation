using NUnit.Framework;

namespace GameFoundation.UI.Tests
{
    public sealed class UiPopupRouterTests
    {
        private sealed class TestPopup : IUiPopupView
        {
            public string PopupId { get; }
            public bool IsOpen { get; private set; }

            /// <summary>
            /// 创建具有稳定 ID 的测试弹窗。
            /// </summary>
            public TestPopup(string popupId)
            {
                PopupId = popupId;
            }

            /// <summary>
            /// 记录弹窗已经打开。
            /// </summary>
            public void Open(object arguments)
            {
                IsOpen = true;
            }

            /// <summary>
            /// 记录弹窗已经关闭。
            /// </summary>
            public void Close()
            {
                IsOpen = false;
            }
        }

        /// <summary>
        /// 验证排队弹窗只会在当前弹窗通知关闭后打开。
        /// </summary>
        [Test]
        public void QueueOpensAfterActivePopupCloses()
        {
            var router = new UiPopupRouter();
            var first = new TestPopup("first");
            var second = new TestPopup("second");
            router.Register(first);
            router.Register(second);

            router.Show(first.PopupId, UiPopupOpenMode.Replace);
            router.Show(second.PopupId, UiPopupOpenMode.Queue);

            Assert.That(first.IsOpen, Is.True);
            Assert.That(second.IsOpen, Is.False);
            first.Close();
            router.NotifyClosed(first);
            Assert.That(second.IsOpen, Is.True);
        }
    }
}
