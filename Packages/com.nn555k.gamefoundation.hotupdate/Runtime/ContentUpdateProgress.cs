namespace GameFoundation.HotUpdate
{
    public sealed class ContentUpdateProgress
    {
        public ContentUpdateStage Stage { get; }
        public long DownloadedBytes { get; }
        public long TotalBytes { get; }
        public float Normalized { get; }
        public string Message { get; }

        /// <summary>
        /// 创建一次可直接用于 UI 展示的不可变进度快照。
        /// </summary>
        public ContentUpdateProgress(
            ContentUpdateStage stage,
            long downloadedBytes,
            long totalBytes,
            string message = "")
        {
            Stage = stage;
            DownloadedBytes = downloadedBytes < 0 ? 0 : downloadedBytes;
            TotalBytes = totalBytes < 0 ? 0 : totalBytes;
            Normalized = TotalBytes <= 0
                ? 0f
                : UnityEngine.Mathf.Clamp01((float)DownloadedBytes / TotalBytes);
            Message = message ?? string.Empty;
        }
    }
}
