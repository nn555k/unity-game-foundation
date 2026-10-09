using System.Collections.Generic;
using System.Text;

namespace GameFoundation.Setup.Editor
{
    public sealed class FoundationProjectSetupReport
    {
        private readonly List<string> mCreated = new List<string>();
        private readonly List<string> mUpdated = new List<string>();
        private readonly List<string> mSkipped = new List<string>();
        private readonly List<string> mErrors = new List<string>();

        public IReadOnlyList<string> Created => mCreated;
        public IReadOnlyList<string> Updated => mUpdated;
        public IReadOnlyList<string> Skipped => mSkipped;
        public IReadOnlyList<string> Errors => mErrors;
        public bool Success => mErrors.Count == 0;

        /// <summary>
        /// 记录本次脚手架新建或覆盖的资源路径。
        /// </summary>
        internal void AddCreated(string path)
        {
            mCreated.Add(path);
        }

        /// <summary>
        /// 记录由脚手架安全更新的 Foundation 托管文件或区块。
        /// </summary>
        internal void AddUpdated(string path)
        {
            mUpdated.Add(path);
        }

        /// <summary>
        /// 记录因安全策略而保留的既有资源路径。
        /// </summary>
        internal void AddSkipped(string path)
        {
            mSkipped.Add(path);
        }

        /// <summary>
        /// 记录阻止脚手架完整生成的错误。
        /// </summary>
        internal void AddError(string error)
        {
            mErrors.Add(error);
        }

        /// <summary>
        /// 输出适合 Unity Console 和自动化日志读取的分组报告。
        /// </summary>
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine(Success ? "Game Foundation setup succeeded." : "Game Foundation setup failed.");
            Append(builder, "Created", mCreated);
            Append(builder, "Updated", mUpdated);
            Append(builder, "Skipped", mSkipped);
            Append(builder, "Errors", mErrors);
            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// 将非空报告集合追加到统一文本中。
        /// </summary>
        private static void Append(StringBuilder builder, string title, List<string> values)
        {
            if (values.Count == 0)
            {
                return;
            }

            builder.AppendLine(title + ":");
            for (var index = 0; index < values.Count; index++)
            {
                builder.AppendLine("- " + values[index]);
            }
        }
    }
}
