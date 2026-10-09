namespace GameFoundation.Core
{
    public sealed class FoundationCoreOptions
    {
        public bool RegisterDefaultClock = true;
        public bool RegisterDefaultLogger = true;

        /// <summary>
        /// 创建适合普通运行环境的默认基础配置。
        /// </summary>
        public static FoundationCoreOptions Default()
        {
            return new FoundationCoreOptions();
        }
    }
}
