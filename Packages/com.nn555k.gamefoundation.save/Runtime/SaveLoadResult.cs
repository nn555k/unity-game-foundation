namespace GameFoundation.Save
{
    public enum SaveLoadStatus
    {
        Success,
        Missing,
        Corrupt,
        MigrationMissing,
        MigrationFailed,
        DeserializeFailed
    }

    public sealed class SaveLoadResult<T>
    {
        public SaveLoadStatus Status { get; }
        public T Value { get; }
        public bool UsedBackup { get; }
        public string Error { get; }
        public bool IsSuccess => Status == SaveLoadStatus.Success;

        /// <summary>
        /// 保存一次读取结果，供业务决定使用默认数据还是提示恢复失败。
        /// </summary>
        public SaveLoadResult(SaveLoadStatus status, T value, bool usedBackup, string error)
        {
            Status = status;
            Value = value;
            UsedBackup = usedBackup;
            Error = error ?? string.Empty;
        }
    }
}
