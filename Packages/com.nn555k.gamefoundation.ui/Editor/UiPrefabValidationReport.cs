using System.Collections.Generic;
using System.Linq;

namespace GameFoundation.UI.Editor
{
    public sealed class UiPrefabValidationReport
    {
        private readonly List<UiPrefabValidationIssue> mIssues = new List<UiPrefabValidationIssue>();

        public string PrefabPath { get; }
        public UiPrefabKind Kind { get; }
        public IReadOnlyList<UiPrefabValidationIssue> Issues => mIssues;
        public bool IsValid => mIssues.All(issue => issue.Severity != UiPrefabValidationSeverity.Error);

        /// <summary>
        /// 创建一个指定资源和规则类型的空校验报告。
        /// </summary>
        public UiPrefabValidationReport(string prefabPath, UiPrefabKind kind)
        {
            PrefabPath = prefabPath ?? string.Empty;
            Kind = kind;
        }

        /// <summary>
        /// 添加校验问题并保持原始发现顺序。
        /// </summary>
        public void Add(UiPrefabValidationSeverity severity, string objectPath, string message)
        {
            mIssues.Add(new UiPrefabValidationIssue(severity, objectPath, message));
        }

        /// <summary>
        /// 输出包含结论和全部问题的多行报告。
        /// </summary>
        public string ToMultilineString()
        {
            var lines = new List<string>
            {
                $"Prefab={PrefabPath}",
                $"Kind={Kind}",
                $"Valid={IsValid}",
                $"Issues={mIssues.Count}"
            };
            lines.AddRange(mIssues.Select(issue => issue.ToString()));
            return string.Join("\n", lines);
        }
    }
}
