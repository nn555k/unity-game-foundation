#!/usr/bin/env python3
"""Create a temporary Unity project and validate external local package installation."""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
import tempfile
import time
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
PACKAGE_IDS = (
    "com.nn555k.gamefoundation.core",
    "com.nn555k.gamefoundation.save",
    "com.nn555k.gamefoundation.hotupdate",
    "com.nn555k.gamefoundation.hotupdate.yooasset",
    "com.nn555k.gamefoundation.sdk",
    "com.nn555k.gamefoundation.ui",
    "com.nn555k.gamefoundation.setup",
)
DEFAULT_EDITOR = Path(
    "/Applications/Unity/Hub/Editor/2022.3.62f2c1/Unity.app/Contents/MacOS/Unity"
)
SETUP_SUCCESS_MARKER = "Game Foundation setup succeeded."
TRANSIENT_PACKAGE_ERRORS = (
    "SSL_ERROR_SYSCALL",
    "Could not resolve host",
    "Failed to connect to github.com",
    "Connection reset by peer",
)


def parse_arguments() -> argparse.Namespace:
    """Parse Unity and optional remote Git package source overrides."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--editor",
        default=os.environ.get("GAME_FOUNDATION_UNITY_EDITOR", str(DEFAULT_EDITOR)),
    )
    parser.add_argument(
        "--repository",
        default="",
        help="Optional Git repository URL; defaults to external local file dependencies",
    )
    parser.add_argument("--ref", default="v0.6.0", help="Git branch, tag or commit")
    return parser.parse_args()


def package_dependency(package_id: str, repository: str, git_ref: str) -> str:
    """Build either an external local path or a monorepo Git package URL."""
    if repository:
        return f"{repository}?path=/Packages/{package_id}#{git_ref}"
    return "file:" + (REPOSITORY_ROOT / "Packages" / package_id).as_posix()


def create_project(project_root: Path, repository: str, git_ref: str) -> None:
    """Create the smallest project that consumes every package from outside its root."""
    assets = project_root / "Assets"
    qframework = assets / "ThirdParty" / "QFramework"
    consumer = assets / "Consumer"
    tests = consumer / "Tests" / "Editor"
    packages = project_root / "Packages"
    project_settings = project_root / "ProjectSettings"
    for directory in (qframework, consumer, tests, packages, project_settings):
        directory.mkdir(parents=True, exist_ok=True)

    shutil.copy2(REPOSITORY_ROOT / "Assets/ThirdParty/QFramework/QFramework.cs", qframework)
    shutil.copy2(REPOSITORY_ROOT / "Assets/ThirdParty/QFramework/QFramework.asmdef", qframework)
    imported_samples = assets / "Samples" / "Game Foundation"
    sample_sources = (
        ("Core", "com.nn555k.gamefoundation.core", "ProjectArchitecture"),
        ("Save", "com.nn555k.gamefoundation.save", "VersionedSave"),
        ("HotUpdate", "com.nn555k.gamefoundation.hotupdate", "BackendTemplate"),
        ("SDK", "com.nn555k.gamefoundation.sdk", "AdapterTemplate"),
    )
    for display_name, package_id, sample_name in sample_sources:
        shutil.copytree(
            REPOSITORY_ROOT / "Packages" / package_id / "Samples~" / sample_name,
            imported_samples / display_name,
        )

    dependencies = {
        package_id: package_dependency(package_id, repository, git_ref)
        for package_id in PACKAGE_IDS
    }
    dependencies.update(
        {
            "com.tuyoogame.yooasset": "https://github.com/tuyoogame/YooAsset.git?path=Assets/YooAsset#2.3.18",
            "com.unity.test-framework": "1.1.33",
            "com.unity.ugui": "1.0.0",
            "com.unity.modules.imgui": "1.0.0",
            "com.unity.modules.jsonserialize": "1.0.0",
            "com.unity.modules.ui": "1.0.0",
        }
    )
    manifest = {
        "dependencies": dependencies,
        "testables": list(PACKAGE_IDS[1:]),
    }
    (packages / "manifest.json").write_text(
        json.dumps(manifest, indent=2) + "\n",
        encoding="utf-8",
    )
    (project_settings / "ProjectVersion.txt").write_text(
        "m_EditorVersion: 2022.3.62f2c1\n"
        "m_EditorVersionWithRevision: 2022.3.62f2c1 (92e6e6be66dc)\n",
        encoding="utf-8",
    )
    (consumer / "GameFoundation.CleanConsumer.asmdef").write_text(
        json.dumps(
            {
                "name": "GameFoundation.CleanConsumer",
                "rootNamespace": "GameFoundation.CleanConsumer",
                "references": [
                    "GameFoundation.Core",
                    "GameFoundation.HotUpdate",
                    "GameFoundation.HotUpdate.YooAsset",
                    "GameFoundation.Save",
                    "GameFoundation.Sdk",
                    "GameFoundation.UI",
                    "YooAsset",
                    "QFramework",
                ],
                "autoReferenced": True,
            },
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )
    (consumer / "CleanConsumerApp.cs").write_text(CONSUMER_SOURCE, encoding="utf-8")
    (tests / "GameFoundation.CleanConsumer.EditorTests.asmdef").write_text(
        json.dumps(
            {
                "name": "GameFoundation.CleanConsumer.EditorTests",
                "rootNamespace": "GameFoundation.CleanConsumer.Tests",
                "references": [
                    "GameFoundation.CleanConsumer",
                    "GameFoundation.Core",
                    "GameFoundation.HotUpdate",
                    "GameFoundation.HotUpdate.YooAsset",
                    "GameFoundation.Save",
                    "GameFoundation.Sdk",
                    "GameFoundation.UI",
                    "YooAsset",
                    "QFramework",
                ],
                "includePlatforms": ["Editor"],
                "optionalUnityReferences": ["TestAssemblies"],
                "autoReferenced": False,
            },
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )
    (tests / "CleanConsumerTests.cs").write_text(TEST_SOURCE, encoding="utf-8")


def run_validation(editor: Path, project_root: Path) -> int:
    """Run the temporary consumer EditMode test through the guarded runner."""
    if not editor.is_file():
        raise FileNotFoundError(f"Unity editor not found: {editor}")
    result_path = project_root / "TestResults" / "editmode.xml"
    log_path = project_root / "TestResults" / "editmode.log"
    command = [
        sys.executable,
        str(REPOSITORY_ROOT / "Scripts/run_unity_test_mode.py"),
        "--editor",
        str(editor),
        "--project",
        str(project_root),
        "--mode",
        "EditMode",
        "--result",
        str(result_path),
        "--log",
        str(log_path),
    ]
    completed = subprocess.run(command, check=False)
    if completed.returncode != 0 and log_path.is_file():
        print(log_path.read_text(encoding="utf-8", errors="replace")[-8000:])
    return completed.returncode


def log_contains(log_path: Path, marker: str) -> bool:
    """Check a batch log marker without assuming Unity exited cleanly."""
    return log_path.is_file() and marker in log_path.read_text(
        encoding="utf-8",
        errors="replace",
    )


def has_transient_package_error(log_path: Path) -> bool:
    """Recognize retryable Git transport failures without masking compiler errors."""
    if not log_path.is_file():
        return False
    content = log_path.read_text(encoding="utf-8", errors="replace")
    return any(marker in content for marker in TRANSIENT_PACKAGE_ERRORS)


def stop_owned_process(process: subprocess.Popen[bytes]) -> None:
    """Terminate an owned Unity process that hangs after writing its success marker."""
    process.terminate()
    try:
        process.wait(timeout=5)
    except subprocess.TimeoutExpired:
        process.kill()
        process.wait(timeout=5)


def run_setup_wizard(editor: Path, project_root: Path) -> int:
    """Execute the Editor-only scaffolder and verify its generated asset set exists."""
    log_path = project_root / "TestResults" / "setup.log"
    log_path.parent.mkdir(parents=True, exist_ok=True)
    environment = os.environ.copy()
    environment.update(
        {
            "GAME_FOUNDATION_PROJECT_NAME": "SmokeGame",
            "GAME_FOUNDATION_ROOT_NAMESPACE": "Smoke.Game",
            "GAME_FOUNDATION_CODE_ROOT": "Assets/Scripts/Game",
            "GAME_FOUNDATION_FEATURE_ID": "smoke-feature",
            "GAME_FOUNDATION_FEATURE_TITLE": "Smoke Feature",
            "GAME_FOUNDATION_FEATURE_SUMMARY": "Prove the generated Feature Spec workflow is available.",
        }
    )
    command = [
        str(editor),
        "-batchmode",
        "-nographics",
        "-quit",
        "-projectPath",
        str(project_root),
        "-executeMethod",
        "GameFoundation.Setup.Editor.FoundationProjectSetupBatch.GenerateFromEnvironment",
        "-logFile",
        str(log_path),
    ]
    process = subprocess.Popen(command, env=environment)
    deadline = time.monotonic() + 600
    completed_at: float | None = None
    exit_code: int | None = None
    while time.monotonic() < deadline:
        exit_code = process.poll()
        if exit_code is not None:
            break
        if log_contains(log_path, SETUP_SUCCESS_MARKER):
            completed_at = completed_at or time.monotonic()
            if time.monotonic() - completed_at >= 10:
                stop_owned_process(process)
                exit_code = process.returncode
                break
        time.sleep(0.25)
    else:
        stop_owned_process(process)
        print("Setup Wizard timed out after 600 seconds.")
        return 124

    expected = (
        project_root / "Assets/Scripts/Game/Architecture/SmokeGameApp.cs",
        project_root / "Assets/Scripts/Game/Architecture/SmokeGameController.cs",
        project_root / "Assets/Scripts/Game/Smoke.Game.Game.asmdef",
        project_root / "Assets/Settings/GameFoundation/SmokeGameUiPrefabConvention.asset",
        project_root / ".gamefoundation/project.json",
        project_root / "AGENTS.md",
        project_root / ".agents/skills/specify-foundation-feature/SKILL.md",
        project_root / ".agents/skills/develop-foundation-feature/SKILL.md",
        project_root / ".agents/skills/normalize-figma-unity-ui/SKILL.md",
        project_root / "Docs/Features/FEATURE_TEMPLATE.md",
        project_root / "Docs/Features/smoke-feature.md",
        project_root / "Scripts/validate_game_foundation_project.py",
        project_root / ".github/workflows/game-foundation-governance.yml",
    )
    missing = [str(path) for path in expected if not path.is_file()]
    setup_succeeded = log_contains(log_path, SETUP_SUCCESS_MARKER)
    if not setup_succeeded or missing:
        print(f"Setup Wizard failed or missed assets: {missing}")
        if log_path.is_file():
            print(log_path.read_text(encoding="utf-8", errors="replace")[-8000:])
        return exit_code or 1

    convention_command = [
        sys.executable,
        str(project_root / "Scripts/validate_game_foundation_project.py"),
        "--project",
        str(project_root),
    ]
    convention_result = subprocess.run(
        convention_command,
        check=False,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
    )
    if convention_result.returncode != 0:
        print(convention_result.stdout)
        return convention_result.returncode

    print("Setup Wizard and governance generation passed.")
    return 0


def main() -> int:
    """Create, test and automatically delete an isolated consumer project."""
    arguments = parse_arguments()
    with tempfile.TemporaryDirectory(prefix="game-foundation-clean-install-") as temporary:
        project_root = Path(temporary)
        create_project(project_root, arguments.repository, arguments.ref)
        setup_exit_code = 1
        for attempt in range(1, 4):
            setup_exit_code = run_setup_wizard(Path(arguments.editor), project_root)
            if setup_exit_code == 0:
                break
            setup_log = project_root / "TestResults" / "setup.log"
            if attempt == 3 or not has_transient_package_error(setup_log):
                return setup_exit_code
            print(f"Retrying transient package download failure ({attempt}/3).")
            time.sleep(1)
        exit_code = run_validation(Path(arguments.editor), project_root)
        if exit_code == 0:
            print("Clean external package installation passed.")
        return exit_code


CONSUMER_SOURCE = """using GameFoundation.Core;
using GameFoundation.HotUpdate;
using GameFoundation.Save;
using GameFoundation.Sdk;
using GameFoundation.UI;
using QFramework;

namespace GameFoundation.CleanConsumer
{
    public sealed class CleanConsumerApp : Architecture<CleanConsumerApp>
    {
        /// <summary>
        /// Registers all Foundation modules in a project-owned architecture.
        /// </summary>
        protected override void Init()
        {
            FoundationCoreModule.Register(this);
            FoundationSaveModule.Register(this, new MemorySaveStore());
            FoundationHotUpdateModule.Register(this);
            FoundationSdkModule.Register(this);
            FoundationUiModule.Register(this);
        }
    }
}
"""


TEST_SOURCE = """using GameFoundation.Core;
using GameFoundation.HotUpdate;
using GameFoundation.HotUpdate.Providers.YooAsset;
using GameFoundation.Save;
using GameFoundation.Sdk;
using GameFoundation.UI;
using NUnit.Framework;

namespace GameFoundation.CleanConsumer.Tests
{
    public sealed class CleanConsumerTests
    {
        /// <summary>
        /// Proves packages resolve and compose from paths outside the consumer project.
        /// </summary>
        [Test]
        public void ExternalPackagesCompileAndRegister()
        {
            var architecture = CleanConsumerApp.Interface;
            Assert.That(architecture.GetUtility<IFoundationClock>(), Is.Not.Null);
            Assert.That(architecture.GetUtility<ISaveStore>(), Is.Not.Null);
            Assert.That(architecture.GetUtility<IHotUpdateService>(), Is.Not.Null);
            Assert.That(architecture.GetUtility<ISdkHost>(), Is.Not.Null);
            Assert.That(architecture.GetUtility<UiPopupRouter>(), Is.Not.Null);
        }

        /// <summary>
        /// Proves optional vendor packages expose usable APIs without contacting external services.
        /// </summary>
        [Test]
        public void OptionalYooAssetPackageCompiles()
        {
            var options = YooAssetContentUpdateOptions.Host(
                "CleanInstall",
                "https://cdn.example.com/content/",
                downloadTags: new[] { "base", "base", "" });
            Assert.That(options.RemoteMainUrl, Is.EqualTo("https://cdn.example.com/content"));
            Assert.That(options.DownloadTags, Is.EqualTo(new[] { "base" }));

        }
    }
}
"""


if __name__ == "__main__":
    raise SystemExit(main())
