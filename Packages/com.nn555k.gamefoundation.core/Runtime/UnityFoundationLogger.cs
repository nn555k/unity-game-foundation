using UnityEngine;

namespace GameFoundation.Core
{
    public sealed class UnityFoundationLogger : IFoundationLogger
    {
        /// <summary>
        /// 输出带统一分类前缀的普通运行日志。
        /// </summary>
        public void Info(string category, string message)
        {
            Debug.Log(Format(category, message));
        }

        /// <summary>
        /// 输出带统一分类前缀的警告日志。
        /// </summary>
        public void Warning(string category, string message)
        {
            Debug.LogWarning(Format(category, message));
        }

        /// <summary>
        /// 输出带统一分类前缀的错误日志。
        /// </summary>
        public void Error(string category, string message)
        {
            Debug.LogError(Format(category, message));
        }

        /// <summary>
        /// 规范化日志分类与正文，空分类时保持输出简洁。
        /// </summary>
        private static string Format(string category, string message)
        {
            return string.IsNullOrWhiteSpace(category)
                ? message ?? string.Empty
                : $"[{category}] {message}";
        }
    }
}
