using UnityEditor;
using UnityEngine;

namespace GameFoundation.Setup.Editor
{
    public sealed class FoundationProjectSetupWindow : EditorWindow
    {
        private FoundationProjectSetupOptions mOptions = new FoundationProjectSetupOptions();
        private bool mInitialized;
        private string mLoadError;
        private Vector2 mScroll;

        /// <summary>
        /// 打开新项目初始化窗口。
        /// </summary>
        [MenuItem("Game Foundation/Project Setup")]
        public static void Open()
        {
            var window = GetWindow<FoundationProjectSetupWindow>(true, "Game Foundation Setup");
            window.minSize = new Vector2(460f, 520f);
            window.Show();
        }

        /// <summary>Restores the existing project identity so opening Setup never defaults back to NewGame.</summary>
        private void OnEnable()
        {
            try
            {
                var saved = FoundationProjectIdentity.Load(FoundationGovernanceBootstrap.ResolveProjectRoot());
                mInitialized = saved != null;
                mOptions = saved ?? new FoundationProjectSetupOptions { ProjectName = string.Empty, RootNamespace = string.Empty };
                mLoadError = null;
            }
            catch (System.Exception exception) { mLoadError = exception.Message; }
        }

        /// <summary>
        /// 绘制项目命名、模块选择和安全覆盖选项。
        /// </summary>
        private void OnGUI()
        {
            mScroll = EditorGUILayout.BeginScrollView(mScroll);
            if (!string.IsNullOrEmpty(mLoadError))
            {
                EditorGUILayout.HelpBox(mLoadError, MessageType.Error);
                EditorGUILayout.EndScrollView();
                return;
            }
            EditorGUILayout.HelpBox(
                mInitialized ? "Project initialized. Repair fills missing files and prepares installed tools. Project identity and existing code are preserved."
                    : "AI onboarding generates architecture, governance, UI conventions, and installed tool defaults. Use the real project name.",
                MessageType.Info);

            EditorGUILayout.LabelField("Project", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(mInitialized))
            {
                mOptions.ProjectName = EditorGUILayout.TextField("Project Name", mOptions.ProjectName);
                mOptions.RootNamespace = EditorGUILayout.TextField("Root Namespace", mOptions.RootNamespace);
                mOptions.CodeRoot = EditorGUILayout.TextField("Code Root", mOptions.CodeRoot);
                mOptions.ContentPackageName = EditorGUILayout.TextField("Content Package", mOptions.ContentPackageName);
                mOptions.ContentRoot = EditorGUILayout.TextField("Content Root", mOptions.ContentRoot);
            }

            EditorGUI.BeginDisabledGroup(mInitialized);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Modules", EditorStyles.boldLabel);
            mOptions.IncludeSave = EditorGUILayout.ToggleLeft("Save", mOptions.IncludeSave);
            mOptions.IncludeHotUpdate = EditorGUILayout.ToggleLeft("Hot Update", mOptions.IncludeHotUpdate);
            mOptions.IncludeSdk = EditorGUILayout.ToggleLeft("SDK Host", mOptions.IncludeSdk);
            mOptions.IncludeUi = EditorGUILayout.ToggleLeft("UI", mOptions.IncludeUi);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generated Assets", EditorStyles.boldLabel);
            mOptions.CreateAssemblyDefinition = EditorGUILayout.ToggleLeft(
                "Create project assembly definition",
                mOptions.CreateAssemblyDefinition);
            using (new EditorGUI.DisabledScope(!mOptions.IncludeUi))
            {
                mOptions.CreateUiConventionProfile = EditorGUILayout.ToggleLeft(
                    "Create UI convention profile",
                    mOptions.CreateUiConventionProfile && mOptions.IncludeUi);
            }

            mOptions.InstallGovernance = EditorGUILayout.ToggleLeft(
                "Install AI governance, Feature Specs, validation, and CI",
                mOptions.InstallGovernance);

            EditorGUI.EndDisabledGroup();
            mOptions.OverwriteExisting = false;
            mOptions.PrepareInstalledTools = EditorGUILayout.ToggleLeft("Prepare installed YooAsset tooling", mOptions.PrepareInstalledTools);

            EditorGUILayout.Space();
            if (GUILayout.Button(mInitialized ? "Repair / Complete Onboarding" : "Initialize Project", GUILayout.Height(38f)))
            {
                Generate();
            }
            if (mInitialized && GUILayout.Button("Validate Local Readiness"))
                FoundationProjectOnboarding.ValidateBatch();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 执行脚手架并用对话框和 Console 同时返回结果。
        /// </summary>
        private void Generate()
        {
            var report = FoundationProjectScaffolder.Generate(mOptions);
            if (report.Success)
            {
                mInitialized = true;
                Debug.Log(report.ToString());
                EditorUtility.DisplayDialog("Game Foundation", report.ToString(), "OK");
                return;
            }

            Debug.LogError(report.ToString());
            EditorUtility.DisplayDialog("Game Foundation Setup Failed", report.ToString(), "OK");
        }
    }
}
