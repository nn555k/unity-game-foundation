using System;
using System.Collections.Generic;
using QFramework;

namespace GameFoundation.UI
{
    public sealed class UiPopupRouter : IUtility
    {
        private sealed class PopupRequest
        {
            public string PopupId;
            public object Arguments;
        }

        private readonly Dictionary<string, IUiPopupView> mViews = new Dictionary<string, IUiPopupView>(StringComparer.Ordinal);
        private readonly Queue<PopupRequest> mQueue = new Queue<PopupRequest>();
        private IUiPopupView mActiveView;

        public IUiPopupView ActiveView => mActiveView;
        public int QueuedCount => mQueue.Count;

        /// <summary>
        /// 注册由项目创建并拥有生命周期的弹窗视图。
        /// </summary>
        public void Register(IUiPopupView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (string.IsNullOrWhiteSpace(view.PopupId))
            {
                throw new ArgumentException("Popup ID cannot be empty.", nameof(view));
            }

            mViews[view.PopupId] = view;
        }

        /// <summary>
        /// 注销弹窗并清理指向该实例的活动状态。
        /// </summary>
        public void Unregister(IUiPopupView view)
        {
            if (view == null)
            {
                return;
            }

            if (mViews.TryGetValue(view.PopupId, out var registered) && ReferenceEquals(registered, view))
            {
                mViews.Remove(view.PopupId);
            }

            if (ReferenceEquals(mActiveView, view))
            {
                mActiveView = null;
                ShowNextQueued();
            }
        }

        /// <summary>
        /// 按替换或排队模式打开弹窗，不承担具体弹窗业务决策。
        /// </summary>
        public bool Show(string popupId, UiPopupOpenMode mode, object arguments = null)
        {
            if (!mViews.TryGetValue(popupId, out var view))
            {
                return false;
            }

            if (mActiveView == null)
            {
                Open(view, arguments);
                return true;
            }

            if (mode == UiPopupOpenMode.Queue)
            {
                mQueue.Enqueue(new PopupRequest { PopupId = popupId, Arguments = arguments });
                return true;
            }

            var previous = mActiveView;
            mActiveView = null;
            previous.Close();
            Open(view, arguments);
            return true;
        }

        /// <summary>
        /// 接收视图完成关闭的通知，并继续展示队列中的下一个弹窗。
        /// </summary>
        public void NotifyClosed(IUiPopupView view)
        {
            if (!ReferenceEquals(mActiveView, view))
            {
                return;
            }

            mActiveView = null;
            ShowNextQueued();
        }

        /// <summary>
        /// 清空等待队列，可选择同时关闭当前弹窗。
        /// </summary>
        public void Clear(bool closeActive)
        {
            mQueue.Clear();
            if (!closeActive || mActiveView == null)
            {
                return;
            }

            var previous = mActiveView;
            mActiveView = null;
            previous.Close();
        }

        /// <summary>
        /// 激活弹窗并记录为唯一活动视图。
        /// </summary>
        private void Open(IUiPopupView view, object arguments)
        {
            mActiveView = view;
            view.Open(arguments);
        }

        /// <summary>
        /// 跳过已经注销的请求，直至找到可展示弹窗或队列耗尽。
        /// </summary>
        private void ShowNextQueued()
        {
            while (mActiveView == null && mQueue.Count > 0)
            {
                var request = mQueue.Dequeue();
                if (mViews.TryGetValue(request.PopupId, out var view))
                {
                    Open(view, request.Arguments);
                }
            }
        }
    }
}
