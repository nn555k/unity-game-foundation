namespace GameFoundation.Save
{
    public interface ISaveMigration
    {
        int FromVersion { get; }
        int ToVersion { get; }

        /// <summary>
        /// 将上一版本正文迁移到声明的目标版本正文。
        /// </summary>
        string Migrate(string payload);
    }
}
