namespace GameFoundation.HotUpdate
{
    public sealed class ContentUpdatePlan
    {
        public bool HasUpdate { get; }
        public string CurrentVersion { get; }
        public string TargetVersion { get; }
        public long DownloadBytes { get; }
        public object BackendContext { get; }

        /// <summary>
        /// 保存后端检查结果，BackendContext 只由创建它的后端解释。
        /// </summary>
        public ContentUpdatePlan(
            bool hasUpdate,
            string currentVersion,
            string targetVersion,
            long downloadBytes,
            object backendContext = null)
        {
            HasUpdate = hasUpdate;
            CurrentVersion = currentVersion ?? string.Empty;
            TargetVersion = targetVersion ?? string.Empty;
            DownloadBytes = downloadBytes < 0 ? 0 : downloadBytes;
            BackendContext = backendContext;
        }

        /// <summary>
        /// 创建无需下载的检查结果。
        /// </summary>
        public static ContentUpdatePlan UpToDate(string version = "")
        {
            return new ContentUpdatePlan(false, version, version, 0L);
        }
    }
}
