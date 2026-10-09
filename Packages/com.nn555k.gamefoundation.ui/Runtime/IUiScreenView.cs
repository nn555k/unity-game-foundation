namespace GameFoundation.UI
{
    public interface IUiScreenView
    {
        string ScreenId { get; }
        bool IsVisible { get; }

        /// <summary>
        /// 使用可选参数显示项目页面。
        /// </summary>
        void Show(object arguments);

        /// <summary>
        /// 隐藏项目页面但不决定业务导航目标。
        /// </summary>
        void Hide();
    }
}
