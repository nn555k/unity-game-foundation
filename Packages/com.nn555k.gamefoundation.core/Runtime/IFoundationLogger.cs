using QFramework;

namespace GameFoundation.Core
{
    public interface IFoundationLogger : IUtility
    {
        /// <summary>
        /// 记录普通诊断信息。
        /// </summary>
        void Info(string category, string message);

        /// <summary>
        /// 记录可降级处理的警告。
        /// </summary>
        void Warning(string category, string message);

        /// <summary>
        /// 记录需要排查的错误。
        /// </summary>
        void Error(string category, string message);
    }
}
