using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFoundation.Validation.Editor
{
    public static class FoundationValidationProjectTools
    {
        public const string ScenePath = "Assets/Validation/Scenes/FoundationValidation.unity";

        /// <summary>
        /// 通过 Unity Editor API 创建并注册最小验证场景。
        /// </summary>
        [MenuItem("Game Foundation/Validation/Ensure Validation Scene")]
        public static void EnsureValidationScene()
        {
            EnsureFolder("Assets/Validation/Scenes");
            if (!File.Exists(ToFullPath(ScenePath)))
            {
                var previousScene = SceneManager.GetActiveScene().path;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("FoundationValidation");
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                {
                    throw new InvalidOperationException("Validation scene could not be saved.");
                }

                if (!string.IsNullOrEmpty(previousScene))
                {
                    EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
                }
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[GameFoundation.Validation] Scene is ready: {ScenePath}");
        }

        /// <summary>
        /// 根据安全环境变量执行 Android、iOS 或桌面平台编译冒烟验证。
        /// </summary>
        public static void BuildFromEnvironment()
        {
            EnsureValidationScene();
            var requestedTarget = Environment.GetEnvironmentVariable("GAME_FOUNDATION_BUILD_TARGET") ?? "StandaloneOSX";
            var outputRoot = Environment.GetEnvironmentVariable("GAME_FOUNDATION_BUILD_OUTPUT") ?? "Builds/Validation";
            var target = ParseTarget(requestedTarget);
            var location = BuildLocation(outputRoot, target);
            Directory.CreateDirectory(Path.GetDirectoryName(location) ?? outputRoot);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = location,
                target = target,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Validation build failed: target={target};errors={report.summary.totalErrors}");
            }

            Debug.Log($"[GameFoundation.Validation] Build passed: target={target};output={location}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>
        /// 将脚本参数转换为明确支持的 Unity BuildTarget。
        /// </summary>
        private static BuildTarget ParseTarget(string value)
        {
            if (string.Equals(value, "Android", StringComparison.OrdinalIgnoreCase))
            {
                return BuildTarget.Android;
            }

            if (string.Equals(value, "iOS", StringComparison.OrdinalIgnoreCase))
            {
                return BuildTarget.iOS;
            }

            if (string.Equals(value, "StandaloneLinux64", StringComparison.OrdinalIgnoreCase))
            {
                return BuildTarget.StandaloneLinux64;
            }

            return BuildTarget.StandaloneOSX;
        }

        /// <summary>
        /// 为不同平台构建正确的文件或目录输出位置。
        /// </summary>
        private static string BuildLocation(string outputRoot, BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return Path.Combine(outputRoot, "Android", "FoundationValidation.apk");
                case BuildTarget.iOS:
                    return Path.Combine(outputRoot, "iOS");
                case BuildTarget.StandaloneLinux64:
                    return Path.Combine(outputRoot, "Linux", "FoundationValidation.x86_64");
                default:
                    return Path.Combine(outputRoot, "macOS", "FoundationValidation.app");
            }
        }

        /// <summary>
        /// 逐级创建 Unity Assets 子目录。
        /// </summary>
        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }

        /// <summary>
        /// 将 AssetDatabase 路径解析为验证工程绝对路径。
        /// </summary>
        private static string ToFullPath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new InvalidOperationException("Unity project root could not be resolved.");
            }

            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
