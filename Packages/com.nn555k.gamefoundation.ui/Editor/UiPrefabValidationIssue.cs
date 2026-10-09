namespace GameFoundation.UI.Editor
{
    public enum UiPrefabValidationSeverity
    {
        Warning,
        Error
    }

    public sealed class UiPrefabValidationIssue
    {
        public UiPrefabValidationSeverity Severity { get; }
        public string ObjectPath { get; }
        public string Message { get; }

        /// <summary>
        /// 创建带层级路径的结构校验问题。
        /// </summary>
        public UiPrefabValidationIssue(UiPrefabValidationSeverity severity, string objectPath, string message)
        {
            Severity = severity;
            ObjectPath = objectPath ?? string.Empty;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// 返回适合控制台和 AI 解析的单行描述。
        /// </summary>
        public override string ToString()
        {
            return $"{Severity}|{ObjectPath}|{Message}";
        }
    }
}
