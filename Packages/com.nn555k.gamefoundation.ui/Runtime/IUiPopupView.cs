namespace GameFoundation.UI
{
    public interface IUiPopupView
    {
        string PopupId { get; }
        bool IsOpen { get; }

        /// <summary>
        /// 使用可选参数打开由项目拥有的弹窗。
        /// </summary>
        void Open(object arguments);

        /// <summary>
        /// 请求关闭弹窗，完成后由视图通知 Router。
        /// </summary>
        void Close();
    }
}
