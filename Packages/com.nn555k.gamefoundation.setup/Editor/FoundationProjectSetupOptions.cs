namespace GameFoundation.Setup.Editor
{
    public sealed class FoundationProjectSetupOptions
    {
        public string ProjectName = string.Empty;
        public string RootNamespace = string.Empty;
        public string CodeRoot = "Assets/Scripts/Game";
        public string IntegrationMode = "generated";
        public string ArchitectureFile = string.Empty;
        public string ArchitectureType = string.Empty;
        public string ControllerFile = string.Empty;
        public string ControllerType = string.Empty;
        public string UiConventionProfilePath = string.Empty;
        public bool IncludeSave = true;
        public bool IncludeHotUpdate = true;
        public bool IncludeSdk = true;
        public bool IncludeUi = true;
        public bool CreateAssemblyDefinition = true;
        public bool CreateUiConventionProfile = true;
        public bool InstallGovernance = true;
        public bool PrepareInstalledTools = true;
        public string ContentPackageName = "DefaultPackage";
        public string ContentRoot = "Assets/GameContent";
        public bool OverwriteExisting;
    }
}
