using System;
using System.Collections.Generic;
using QFramework;

namespace GameFoundation.UI
{
    public sealed class UiScreenNavigator : IUtility
    {
        private readonly Dictionary<string, IUiScreenView> mViews = new Dictionary<string, IUiScreenView>(StringComparer.Ordinal);
        private readonly Stack<string> mHistory = new Stack<string>();
        private IUiScreenView mCurrent;

        public IUiScreenView Current => mCurrent;

        /// <summary>
        /// 注册由项目拥有的页面视图。
        /// </summary>
        public void Register(IUiScreenView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (string.IsNullOrWhiteSpace(view.ScreenId))
            {
                throw new ArgumentException("Screen ID cannot be empty.", nameof(view));
            }

            mViews[view.ScreenId] = view;
        }

        /// <summary>
        /// 切换到目标页面，并可选择将当前页面压入返回历史。
        /// </summary>
        public bool Navigate(string screenId, object arguments = null, bool rememberCurrent = true)
        {
            if (!mViews.TryGetValue(screenId, out var next))
            {
                return false;
            }

            if (ReferenceEquals(mCurrent, next))
            {
                next.Show(arguments);
                return true;
            }

            if (mCurrent != null)
            {
                if (rememberCurrent)
                {
                    mHistory.Push(mCurrent.ScreenId);
                }

                mCurrent.Hide();
            }

            mCurrent = next;
            mCurrent.Show(arguments);
            return true;
        }

        /// <summary>
        /// 返回最近一个仍处于注册状态的页面。
        /// </summary>
        public bool GoBack()
        {
            while (mHistory.Count > 0)
            {
                var screenId = mHistory.Pop();
                if (mViews.ContainsKey(screenId))
                {
                    return Navigate(screenId, null, false);
                }
            }

            return false;
        }

        /// <summary>
        /// 清空返回历史但不改变当前页面。
        /// </summary>
        public void ClearHistory()
        {
            mHistory.Clear();
        }
    }
}
