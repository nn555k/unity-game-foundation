namespace GameFoundation.HotUpdate
{
    public enum ContentUpdateResultCode
    {
        Updated,
        AlreadyCurrent,
        OfflineFallback,
        Failed,
        Canceled,
        Busy
    }

    public sealed class ContentUpdateResult
    {
        public ContentUpdateResultCode Code { get; }
        public string Version { get; }
        public string Message { get; }
        public bool IsSuccess => Code == ContentUpdateResultCode.Updated ||
                                 Code == ContentUpdateResultCode.AlreadyCurrent ||
                                 Code == ContentUpdateResultCode.OfflineFallback;

        /// <summary>
        /// 创建可供业务分支处理的热更结果。
        /// </summary>
        public ContentUpdateResult(ContentUpdateResultCode code, string version, string message)
        {
            Code = code;
            Version = version ?? string.Empty;
            Message = message ?? string.Empty;
        }
    }
}
