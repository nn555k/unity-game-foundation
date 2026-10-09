namespace GameFoundation.Sdk
{
    public sealed class SdkAdapterStatus
    {
        public string AdapterId { get; }
        public SdkAdapterState State { get; }
        public string Message { get; }

        /// <summary>
        /// 创建可供调试面板和自动化读取的 SDK 状态快照。
        /// </summary>
        public SdkAdapterStatus(string adapterId, SdkAdapterState state, string message = "")
        {
            AdapterId = adapterId ?? string.Empty;
            State = state;
            Message = message ?? string.Empty;
        }
    }
}
