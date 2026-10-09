#!/usr/bin/env python3
"""Validate package identity, dependency boundaries and repository hygiene."""

from __future__ import annotations

import json
import re
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
PACKAGE_ROOT = REPOSITORY_ROOT / "Packages"
PACKAGE_IDS = (
    "com.nn555k.gamefoundation.core",
    "com.nn555k.gamefoundation.save",
    "com.nn555k.gamefoundation.hotupdate",
    "com.nn555k.gamefoundation.hotupdate.yooasset",
    "com.nn555k.gamefoundation.sdk",
    "com.nn555k.gamefoundation.ui",
    "com.nn555k.gamefoundation.setup",
)
CORE_PACKAGE = "com.nn555k.gamefoundation.core"
BASE_RUNTIME_PACKAGES = {
    CORE_PACKAGE,
    "com.nn555k.gamefoundation.save",
    "com.nn555k.gamefoundation.hotupdate",
    "com.nn555k.gamefoundation.sdk",
    "com.nn555k.gamefoundation.ui",
}
ALLOWED_FOUNDATION_DEPENDENCIES = {
    CORE_PACKAGE: set(),
    "com.nn555k.gamefoundation.save": {CORE_PACKAGE},
    "com.nn555k.gamefoundation.hotupdate": {CORE_PACKAGE},
    "com.nn555k.gamefoundation.hotupdate.yooasset": {
        "com.nn555k.gamefoundation.hotupdate"
    },
    "com.nn555k.gamefoundation.sdk": {CORE_PACKAGE},
    "com.nn555k.gamefoundation.ui": {CORE_PACKAGE},
    "com.nn555k.gamefoundation.setup": BASE_RUNTIME_PACKAGES,
}
FORBIDDEN_SOURCE = re.compile(
    r"\bTurngrid\b|UnityEngine\.Purchasing|\bIAP\b|\bIap[A-Z]|"
    r"InAppPurchase|WeeklyRank|Economy(Model|System)|\bGameplay\b"
)


def validate_package(package_id: str, errors: list[str]) -> str:
    """Validate one package manifest, assembly presence and excluded source tokens."""
    package_path = PACKAGE_ROOT / package_id
    manifest_path = package_path / "package.json"
    if not manifest_path.is_file():
        errors.append(f"Missing package manifest: {manifest_path}")
        return ""

    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if manifest.get("name") != package_id:
        errors.append(f"Package name mismatch: {package_id}")

    if manifest.get("license") != "UNLICENSED":
        errors.append(f"Proprietary package license must be UNLICENSED: {package_id}")

    version = str(manifest.get("version", ""))
    if not re.fullmatch(r"\d+\.\d+\.\d+", version):
        errors.append(f"Invalid semantic version: {package_id}={version}")

    if not list(package_path.glob("*/*.asmdef")):
        errors.append(f"Missing assembly definition: {package_id}")

    for sample in manifest.get("samples", []):
        sample_path = package_path / str(sample.get("path", ""))
        if not sample_path.is_dir():
            errors.append(f"Missing package sample: {package_id}={sample_path}")

    for source_path in package_path.rglob("*.cs"):
        source = source_path.read_text(encoding="utf-8-sig")
        if FORBIDDEN_SOURCE.search(source):
            errors.append(f"Excluded project/business token: {source_path}")

    serialized_assets = [
        path
        for path in package_path.rglob("*")
        if path.suffix.lower() in {".asset", ".prefab", ".unity"}
    ]
    if serialized_assets:
        errors.append(
            f"Serialized project assets found in shared package: {serialized_assets[0]}"
        )

    return version


def validate_dependencies(errors: list[str]) -> None:
    """Enforce sibling runtime isolation and the Editor-only Setup exception."""
    for package_id in PACKAGE_IDS:
        manifest_path = PACKAGE_ROOT / package_id / "package.json"
        if not manifest_path.is_file():
            continue
        dependencies = json.loads(manifest_path.read_text(encoding="utf-8")).get("dependencies", {})
        foundation_dependencies = {name for name in dependencies if name in PACKAGE_IDS}
        expected = ALLOWED_FOUNDATION_DEPENDENCIES[package_id]
        if foundation_dependencies != expected:
            errors.append(
                f"Foundation dependency mismatch: {package_id}="
                f"{sorted(foundation_dependencies)} expected={sorted(expected)}"
            )


def validate_skills(errors: list[str]) -> None:
    """Check project Skills have valid minimal frontmatter and matching folder names."""
    skill_root = REPOSITORY_ROOT / ".agents" / "skills"
    for skill_path in sorted(path for path in skill_root.iterdir() if path.is_dir()):
        definition = skill_path / "SKILL.md"
        if not definition.is_file():
            errors.append(f"Missing SKILL.md: {skill_path}")
            continue
        content = definition.read_text(encoding="utf-8")
        match = re.match(r"^---\nname:\s*([^\n]+)\ndescription:\s*([^\n]+)\n---", content)
        if not match or match.group(1).strip() != skill_path.name or not match.group(2).strip():
            errors.append(f"Invalid Skill frontmatter: {definition}")


def main() -> int:
    """Run all repository checks and return a process-friendly status code."""
    errors: list[str] = []
    for package_path in PACKAGE_ROOT.glob("com.nn555k.gamefoundation.*"):
        if package_path.is_dir() and package_path.name not in PACKAGE_IDS:
            errors.append(f"Unsupported Foundation package in distribution: {package_path.name}")
    versions = {validate_package(package_id, errors) for package_id in PACKAGE_IDS}
    versions.discard("")
    if len(versions) > 1:
        errors.append("Foundation packages must share one release version.")
    validate_dependencies(errors)
    validate_skills(errors)

    if errors:
        for error in errors:
            print(f"ERROR {error}")
        print(f"Repository validation failed with {len(errors)} issue(s).")
        return 1

    version = next(iter(versions), "unknown")
    print(f"Repository validation passed: packages={len(PACKAGE_IDS)} version={version}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
