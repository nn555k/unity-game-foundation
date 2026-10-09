using System;
using System.IO;
using NUnit.Framework;

namespace GameFoundation.Setup.Editor.Tests
{
    public sealed class FoundationGovernanceTests
    {
        private string mProjectRoot;

        /// <summary>
        /// Creates an isolated directory for filesystem convention checks.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            mProjectRoot = Path.Combine(
                Path.GetTempPath(),
                "game-foundation-governance-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(mProjectRoot);
        }

        /// <summary>
        /// Removes only the test-owned directory after every validation case.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(mProjectRoot))
            {
                Directory.Delete(mProjectRoot, true);
            }
        }

        /// <summary>
        /// Verifies AGENTS regeneration replaces the managed block without losing project instructions.
        /// </summary>
        [Test]
        public void MergeManagedAgentsPreservesProjectContent()
        {
            var existing = "# Project Rules\n\nKeep this.\n\n"
                + FoundationGovernanceBootstrap.ManagedStart
                + "\nold\n"
                + FoundationGovernanceBootstrap.ManagedEnd
                + "\n\nAfter block.\n";
            var template = FoundationGovernanceBootstrap.ManagedStart
                + "\nnew\n"
                + FoundationGovernanceBootstrap.ManagedEnd
                + "\n";

            var merged = FoundationGovernanceBootstrap.MergeManagedAgents(existing, template);

            StringAssert.Contains("Keep this.", merged);
            StringAssert.Contains("new", merged);
            StringAssert.Contains("After block.", merged);
            StringAssert.DoesNotContain("old", merged);
        }

        /// <summary>
        /// Verifies a damaged managed block is rejected instead of appending duplicate instructions.
        /// </summary>
        [Test]
        public void MergeManagedAgentsRejectsIncompleteExistingBlock()
        {
            var existing = "# Project\n" + FoundationGovernanceBootstrap.ManagedStart + "\nbroken\n";
            var template = FoundationGovernanceBootstrap.ManagedStart
                + "\nnew\n"
                + FoundationGovernanceBootstrap.ManagedEnd
                + "\n";

            Assert.Throws<InvalidOperationException>(
                () => FoundationGovernanceBootstrap.MergeManagedAgents(existing, template));
        }

        /// <summary>
        /// Verifies generated governance JSON points to the project-owned App and Controller files.
        /// </summary>
        [Test]
        public void BuildProjectConfigurationUsesSetupIdentity()
        {
            var json = FoundationGovernanceBootstrap.BuildProjectConfiguration(ValidOptions());

            StringAssert.Contains("\"projectName\": \"SampleGame\"", json);
            StringAssert.Contains("Assets/Scripts/Game/Architecture/SampleGameApp.cs", json);
            StringAssert.Contains("Assets/Scripts/Game/Architecture/SampleGameController.cs", json);
            StringAssert.Contains("\"generatedByVersion\": \"0.6.0\"", json);
        }

        /// <summary>Proves a repeated setup cannot silently replace the project identity even when overwrite is requested.</summary>
        [Test]
        public void SetupRejectsDifferentIdentityBeforeWriting()
        {
            CreateValidGovernedProject();
            var before = File.ReadAllText(Path.Combine(mProjectRoot, ".gamefoundation/project.json"));
            var options = ValidOptions();
            options.ProjectName = "NewGame";
            options.OverwriteExisting = true;
            var report = new FoundationProjectSetupReport();
            FoundationProjectIdentity.Validate(mProjectRoot, options, report);
            Assert.That(report.Errors, Has.Some.Contains("identity differs"));
            Assert.That(File.ReadAllText(Path.Combine(mProjectRoot, ".gamefoundation/project.json")), Is.EqualTo(before));
        }

        /// <summary>Detects the duplicate-architecture state left by older Setup releases.</summary>
        [Test]
        public void SetupRejectsDuplicateArchitecture()
        {
            CreateValidGovernedProject();
            Write("Assets/Scripts/Game/Architecture/OtherApp.cs", "class OtherApp : Architecture<OtherApp> {}");
            var report = new FoundationProjectSetupReport();
            FoundationProjectIdentity.Validate(mProjectRoot, ValidOptions(), report);
            Assert.That(report.Errors, Has.Some.Contains("Another project Architecture"));
        }

        /// <summary>
        /// Verifies Feature Spec generation resolves identity tokens but preserves explicit work placeholders.
        /// </summary>
        [Test]
        public void BuildFeatureSpecResolvesRequestTokens()
        {
            const string template = "id: {{FEATURE_ID}}\ntitle: {{FEATURE_TITLE}}\ncreated: {{DATE}}\n{{FEATURE_SUMMARY}}\n- TBD\n";

            var content = FoundationFeatureSpecGenerator.BuildFeatureSpec(
                template,
                "daily-reward",
                "Daily Reward",
                "Show a claimable daily reward.");

            StringAssert.Contains("id: daily-reward", content);
            StringAssert.Contains("title: Daily Reward", content);
            StringAssert.Contains("Show a claimable daily reward.", content);
            StringAssert.Contains("- TBD", content);
            StringAssert.DoesNotContain("{{DATE}}", content);
        }

        /// <summary>
        /// Verifies the Unity convention gate accepts a generated project before its first feature.
        /// </summary>
        [Test]
        public void ConventionValidatorAcceptsGeneratedProject()
        {
            CreateValidGovernedProject();

            var report = FoundationProjectConventionValidator.Validate(mProjectRoot);

            Assert.That(report.Errors, Is.Empty);
        }

        /// <summary>Direct references outside Resources are valid and editor-only files do not create runtime collisions.</summary>
        [Test]
        public void ConventionValidatorAcceptsResourceAndDirectReferenceLayout()
        {
            CreateValidGovernedProject();
            Write("Assets/Resources/UI/icon.txt", "fixture");
            Write("Assets/Sprites/UI/icon.txt", "fixture");
            Write("Assets/Editor/Resources/UI/icon.txt", "fixture");
            Assert.That(FoundationProjectConventionValidator.Validate(mProjectRoot).Errors, Is.Empty);
        }

        /// <summary>Case and extension differences must not hide duplicate keys in separate Resources roots.</summary>
        [Test]
        public void ConventionValidatorRejectsDuplicateResourceKeys()
        {
            CreateValidGovernedProject();
            Write("Assets/Resources/UI/Settings.txt", "fixture");
            Write("Assets/Feature/Resources/ui/settings.json", "{}");
            Assert.That(FoundationProjectConventionValidator.Validate(mProjectRoot).Errors,
                Has.Some.Contains("Duplicate Resources key 'ui/settings'"));
        }

        /// <summary>An empty Resources directory must not turn the direct-reference art tree into dynamic content.</summary>
        [Test]
        public void ConventionValidatorRejectsResourcesUnderSprites()
        {
            CreateValidGovernedProject();
            Directory.CreateDirectory(Path.Combine(mProjectRoot, "Assets/Sprites/UI/Resources"));
            Assert.That(FoundationProjectConventionValidator.Validate(mProjectRoot).Errors,
                Has.Some.Contains("Sprites tree must not contain Resources"));
        }

        /// <summary>Nested Resources roots are invalid instead of selecting an arbitrary runtime key.</summary>
        [Test]
        public void ConventionValidatorRejectsNestedResources()
        {
            CreateValidGovernedProject();
            Write("Assets/Resources/UI/Resources/icon.txt", "fixture");
            Assert.That(FoundationProjectConventionValidator.Validate(mProjectRoot).Errors,
                Has.Some.Contains("Nested Resources folders"));
        }

        /// <summary>
        /// Verifies the Unity convention gate rejects a module-local role tree.
        /// </summary>
        [Test]
        public void ConventionValidatorRejectsNestedRoleFolder()
        {
            CreateValidGovernedProject();
            Directory.CreateDirectory(Path.Combine(
                mProjectRoot,
                "Assets/Scripts/Game/Systems/Economy/Models"));

            var report = FoundationProjectConventionValidator.Validate(mProjectRoot);

            Assert.That(
                report.Errors,
                Has.Some.Contains("Nested role folder is forbidden"));
        }

        /// <summary>
        /// Verifies the Unity convention gate rejects sibling project source outside the configured code root.
        /// </summary>
        [Test]
        public void ConventionValidatorRejectsProjectSourceOutsideCodeRoot()
        {
            CreateValidGovernedProject();
            Write("Assets/Scripts/Feature/LooseController.cs", "public sealed class LooseController {}\n");

            var report = FoundationProjectConventionValidator.Validate(mProjectRoot);

            Assert.That(
                report.Errors,
                Has.Some.Contains("outside configured codeRoot"));
        }

        /// <summary>
        /// Creates the minimal file contract emitted by the governance Bootstrap.
        /// </summary>
        private void CreateValidGovernedProject()
        {
            var options = ValidOptions();
            var codeRoot = Path.Combine(mProjectRoot, "Assets/Scripts/Game");
            var roles = new[]
            {
                "Architecture",
                "Commands",
                "Events",
                "Models",
                "Systems",
                "Utilities",
                "ViewControllers"
            };
            for (var index = 0; index < roles.Length; index++)
            {
                Directory.CreateDirectory(Path.Combine(codeRoot, roles[index]));
            }

            File.WriteAllText(
                Path.Combine(codeRoot, "Architecture/SampleGameApp.cs"),
                "public sealed class SampleGameApp {}\n");
            File.WriteAllText(
                Path.Combine(codeRoot, "Architecture/SampleGameController.cs"),
                "public abstract class SampleGameController {}\n");

            Write(".gamefoundation/project.json", FoundationGovernanceBootstrap.BuildProjectConfiguration(options));
            Write(
                "AGENTS.md",
                FoundationGovernanceBootstrap.ManagedStart
                + "\nmanaged\n"
                + FoundationGovernanceBootstrap.ManagedEnd
                + "\n");
            Write(".agents/skills/specify-foundation-feature/SKILL.md", "skill\n");
            Write(".agents/skills/develop-foundation-feature/SKILL.md", "skill\n");
            Write(".agents/skills/normalize-figma-unity-ui/SKILL.md", "skill\n");
            Write("Docs/Foundation/Architecture.md", "architecture\n");
            Write("Docs/Features/README.md", "workflow\n");
            Write("Docs/Features/FEATURE_TEMPLATE.md", "template\n");
            Write("Scripts/validate_game_foundation_project.py", "validator\n");
            Write(".github/workflows/game-foundation-governance.yml", "workflow\n");
        }

        /// <summary>
        /// Writes one test project file and creates its parent directories.
        /// </summary>
        private void Write(string relativePath, string content)
        {
            var fullPath = Path.Combine(
                mProjectRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, content);
        }

        /// <summary>
        /// Returns one valid Setup option set shared by pure and filesystem tests.
        /// </summary>
        private static FoundationProjectSetupOptions ValidOptions()
        {
            return new FoundationProjectSetupOptions
            {
                ProjectName = "SampleGame",
                RootNamespace = "Sample.Game",
                CodeRoot = "Assets/Scripts/Game",
                IncludeSave = true,
                IncludeHotUpdate = true,
                IncludeSdk = true,
                IncludeUi = true,
                CreateAssemblyDefinition = true,
                CreateUiConventionProfile = true,
                InstallGovernance = true
            };
        }
    }
}
