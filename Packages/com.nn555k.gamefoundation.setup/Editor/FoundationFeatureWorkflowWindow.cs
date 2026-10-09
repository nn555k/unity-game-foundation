using System;
using UnityEditor;
using UnityEngine;

namespace GameFoundation.Setup.Editor
{
    public sealed class FoundationFeatureWorkflowWindow : EditorWindow
    {
        private string mFeatureId = string.Empty;
        private string mTitle = string.Empty;
        private string mSummary = string.Empty;

        /// <summary>
        /// Opens the Feature Spec generator used before behavior-changing implementation work.
        /// </summary>
        [MenuItem("Game Foundation/Feature Workflow/New Feature Specification")]
        public static void Open()
        {
            var window = GetWindow<FoundationFeatureWorkflowWindow>(true, "New Feature Specification");
            window.minSize = new Vector2(520f, 330f);
            window.Show();
        }

        /// <summary>
        /// Draws identity and intent fields without generating implementation classes prematurely.
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Creates Docs/Features/<feature-id>.md. Resolve acceptance criteria and ownership before implementation.",
                MessageType.Info);
            mFeatureId = EditorGUILayout.TextField("Feature ID", mFeatureId);
            mTitle = EditorGUILayout.TextField("Title", mTitle);
            EditorGUILayout.LabelField("Goal / Summary");
            mSummary = EditorGUILayout.TextArea(mSummary, GUILayout.MinHeight(120f));

            EditorGUILayout.Space();
            if (GUILayout.Button("Create Feature Specification", GUILayout.Height(38f)))
            {
                Generate();
            }
        }

        /// <summary>
        /// Generates the spec and reports created, skipped, or invalid output to both UI and Console.
        /// </summary>
        private void Generate()
        {
            var report = FoundationFeatureSpecGenerator.Generate(
                (mFeatureId ?? string.Empty).Trim(),
                mTitle,
                mSummary);
            if (report.Success)
            {
                Debug.Log(report.ToString());
                EditorUtility.DisplayDialog("Game Foundation", report.ToString(), "OK");
                return;
            }

            Debug.LogError(report.ToString());
            EditorUtility.DisplayDialog("Feature Specification Failed", report.ToString(), "OK");
        }
    }

    public static class FoundationFeatureSpecBatch
    {
        /// <summary>
        /// Creates a Feature Spec from GAME_FOUNDATION_FEATURE_* variables for AI and CI automation.
        /// </summary>
        public static void GenerateFromEnvironment()
        {
            var featureId = Environment.GetEnvironmentVariable("GAME_FOUNDATION_FEATURE_ID");
            var title = Environment.GetEnvironmentVariable("GAME_FOUNDATION_FEATURE_TITLE");
            var summary = Environment.GetEnvironmentVariable("GAME_FOUNDATION_FEATURE_SUMMARY");
            var report = FoundationFeatureSpecGenerator.Generate(
                (featureId ?? string.Empty).Trim(),
                (title ?? string.Empty).Trim(),
                summary ?? string.Empty);
            if (!report.Success)
            {
                throw new InvalidOperationException(report.ToString());
            }

            Debug.Log(report.ToString());
        }
    }
}
