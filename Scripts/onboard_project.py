#!/usr/bin/env python3
"""AI entry point: install, initialize, recompile, and verify a Unity Foundation consumer."""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys

from install_upm_packages import DEFAULT_SELECTION, PACKAGE_IDS, resolve_selection, update_manifest

ROOT = Path(__file__).resolve().parents[1]
EDITOR = "/Applications/Unity/Hub/Editor/2022.3.62f2c1/Unity.app/Contents/MacOS/Unity"


def run_editor(editor: str, project: Path, method: str, stage: str, environment: dict) -> None:
    """Run a bounded, owned batch process and require explicit stage success plus a clean exit."""
    log = project / "TestResults" / f"onboarding-{stage}.log"
    log.parent.mkdir(parents=True, exist_ok=True)
    process = subprocess.Popen([editor, "-batchmode", "-nographics", "-quit", "-projectPath", str(project),
                                "-executeMethod", method, "-logFile", str(log)], env=environment)
    try:
        code = process.wait(timeout=600)
    except subprocess.TimeoutExpired:
        process.terminate()
        try:
            process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()
        raise RuntimeError(f"Unity {stage} timed out. Inspect {log}")
    marker = "Game Foundation setup succeeded." if stage == "setup" else "GAME_FOUNDATION_ONBOARDING_READY"
    content = log.read_text(encoding="utf-8", errors="replace") if log.exists() else ""
    if code != 0 or marker not in content or "error CS" in content:
        raise RuntimeError(f"Unity {stage} failed (exit {code}). Inspect {log}")
    print(f"{stage}: passed ({log})", flush=True)


def validate_readiness(readiness: dict) -> None:
    """Require base setup evidence and reject any reported failure; external design tools are out of scope."""
    checks = {check["name"]: check["status"] for check in readiness["checks"]}
    required = ("yooasset", "uiProfile", "compiledArchitecture")
    if (not readiness["localReady"] or any(checks.get(name) != "ready" for name in required)
            or any(status == "failed" for status in checks.values())):
        raise RuntimeError("Base onboarding checks failed; inspect onboarding-report.json.")


def main() -> int:
    """Complete local onboarding; credentials and production vendor choices stay explicit in the report."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", required=True)
    parser.add_argument("--project-name")
    parser.add_argument("--namespace", dest="namespace")
    parser.add_argument("--editor", default=EDITOR)
    parser.add_argument("--repository", default="https://github.com/nn555k/unity-game-foundation.git")
    parser.add_argument("--ref", default="v0.6.0")
    parser.add_argument("--local-packages", action="store_true", help="Use this checkout for development verification")
    parser.add_argument("--content-root", default=None)
    parser.add_argument("--content-package", default=None)
    args = parser.parse_args()
    project = Path(args.project).resolve()
    contract = project / ".gamefoundation/project.json"
    saved = json.loads(contract.read_text()) if contract.exists() else {}
    name = args.project_name or saved.get("projectName")
    namespace = args.namespace or saved.get("rootNamespace") or name
    content_root = args.content_root or saved.get("contentRoot") or "Assets/GameContent"
    package = args.content_package or saved.get("contentPackageName") or "DefaultPackage"
    if not name or not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", name):
        raise ValueError("First onboarding requires a valid --project-name; no NewGame placeholder is generated.")
    if not namespace or any(not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", part) for part in namespace.split('.')):
        raise ValueError("--namespace must be a valid C# namespace.")
    if saved and (name != saved["projectName"] or namespace != saved["rootNamespace"]):
        raise ValueError("Existing project identity differs; rerun without identity overrides.")
    if not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", package):
        raise ValueError("--content-package must be a valid identifier.")
    parts = content_root.split('/')
    if (len(parts) < 2 or parts[0] != "Assets" or any(part in ("", ".", "..", "Resources") for part in parts)
            or ':' in content_root or '\\' in content_root):
        raise ValueError("--content-root must be a dedicated Assets folder outside Resources.")
    if saved and (content_root != (saved.get("contentRoot") or "Assets/GameContent")
                  or package != (saved.get("contentPackageName") or "DefaultPackage")):
        raise ValueError("Existing content identity differs; configuration migration is separate from onboarding.")
    expected_app = project / (saved.get("architectureFile") or f"Assets/Scripts/Game/Architecture/{name}App.cs")
    for source in (project / "Assets/Scripts").rglob("*.cs"):
        if source != expected_app and re.search(r":\s*(?:QFramework\.)?Architecture\s*<", source.read_text(encoding="utf-8-sig")):
            raise ValueError(f"Another Architecture exists: {source}. Adopt or migrate it before onboarding.")
    if not Path(args.editor).is_file():
        raise FileNotFoundError(args.editor)
    # Do not copy a second QFramework into existing projects.
    assets = project / "Assets"
    frameworks = list(assets.rglob("QFramework.cs")) if assets.exists() else []
    assemblies = list(assets.rglob("*.asmdef")) if assets.exists() else []
    has_assembly = any(json.loads(path.read_text(encoding="utf-8-sig")).get("name") == "QFramework" for path in assemblies)
    if frameworks and not has_assembly:
        raise ValueError("Existing QFramework source needs a QFramework asmdef before onboarding.")
    manifest = project / "Packages/manifest.json"
    manifest.parent.mkdir(parents=True, exist_ok=True)
    if not manifest.exists():
        manifest.write_text(json.dumps({"dependencies": {"com.unity.test-framework": "1.1.33",
            "com.unity.modules.imgui": "1.0.0", "com.unity.modules.jsonserialize": "1.0.0", "com.unity.modules.ui": "1.0.0"}}, indent=2) + "\n")
    version = project / "ProjectSettings/ProjectVersion.txt"
    if not version.exists():
        version.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(ROOT / "ProjectSettings/ProjectVersion.txt", version)
    if not (project / ".gitignore").exists():
        shutil.copyfile(ROOT / ".gitignore", project / ".gitignore")
    if not has_assembly:
        target = assets / "ThirdParty/QFramework"
        target.mkdir(parents=True, exist_ok=True)
        for filename in ("QFramework.cs", "QFramework.asmdef"):
            shutil.copyfile(ROOT / "Assets/ThirdParty/QFramework" / filename, target / filename)
    aliases = resolve_selection(DEFAULT_SELECTION)
    update_manifest(project, aliases, args.repository, args.ref, False)
    if args.local_packages:
        data = json.loads(manifest.read_text())
        for alias in aliases:
            package_id = PACKAGE_IDS[alias]
            data["dependencies"][package_id] = "file:" + (ROOT / "Packages" / package_id).as_posix()
        manifest.write_text(json.dumps(data, indent=2) + "\n")
    environment = os.environ.copy()
    # Clear stale setup overrides so callers cannot accidentally overwrite generated project code.
    for key in tuple(environment):
        if key.startswith("GAME_FOUNDATION_"):
            environment.pop(key)
    environment.update({"GAME_FOUNDATION_PROJECT_NAME": name, "GAME_FOUNDATION_ROOT_NAMESPACE": namespace,
                        "GAME_FOUNDATION_CONTENT_ROOT": content_root, "GAME_FOUNDATION_CONTENT_PACKAGE": package})
    run_editor(args.editor, project, "GameFoundation.Setup.Editor.FoundationProjectSetupBatch.GenerateFromEnvironment", "setup", environment)
    run_editor(args.editor, project, "GameFoundation.Setup.Editor.FoundationProjectOnboarding.ValidateBatch", "verify", environment)
    readiness = json.loads((project / ".gamefoundation/onboarding-report.json").read_text())
    validate_readiness(readiness)
    subprocess.run([sys.executable, str(project / "Scripts/validate_game_foundation_project.py"), "--project", str(project)], check=True)
    print(f"Local onboarding complete. Read {project / '.gamefoundation/onboarding-report.json'} for external configuration tasks.")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (ValueError, RuntimeError, OSError, subprocess.CalledProcessError) as error:
        print(str(error), file=sys.stderr)
        sys.exit(1)
