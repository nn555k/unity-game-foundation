using System.Collections.Generic;

namespace GameFoundation.UI.Editor
{
    public sealed class UiPrefabNormalizationReport
    {
        private readonly List<string> mActions = new List<string>();

        public string PrefabPath { get; }
        public UiPrefabKind Kind { get; }
        public IReadOnlyList<string> Actions => mActions;
        public UiPrefabValidationReport Validation { get; internal set; }

        /// <summary>
        /// 创建指定 Prefab 的规范化预览或执行报告。
        /// </summary>
        public UiPrefabNormalizationReport(string prefabPath, UiPrefabKind kind)
        {
            PrefabPath = prefabPath ?? string.Empty;
            Kind = kind;
        }

        /// <summary>
        /// 记录一个确定性的结构变更动作。
        /// </summary>
        public void AddAction(string action)
        {
            mActions.Add(action ?? string.Empty);
        }

        /// <summary>
        /// 输出适合 AI 在执行前后比较的多行报告。
        /// </summary>
        public string ToMultilineString()
        {
            var lines = new List<string>
            {
                $"Prefab={PrefabPath}",
                $"Kind={Kind}",
                $"Actions={mActions.Count}"
            };
            lines.AddRange(mActions);
            if (Validation != null)
            {
                lines.Add(Validation.ToMultilineString());
            }

            return string.Join("\n", lines);
        }
    }
}
