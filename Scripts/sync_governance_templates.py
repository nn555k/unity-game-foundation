#!/usr/bin/env python3
"""Synchronize new-project governance sources into the Setup package template tree."""

from __future__ import annotations

import argparse
import filecmp
import shutil
import sys
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
TEMPLATE_ROOT = (
    REPOSITORY_ROOT
    / "Packages"
    / "com.nn555k.gamefoundation.setup"
    / "Templates~"
    / "Governance"
)


def parse_arguments() -> argparse.Namespace:
    """Parse check-only mode used by repository validation and CI."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--check",
        action="store_true",
        help="Fail when packaged templates differ instead of updating them",
    )
    return parser.parse_args()


def source_mappings() -> list[tuple[Path, Path]]:
    """Return every canonical source and its package-safe template destination."""
    mappings: list[tuple[Path, Path]] = [
        (
            REPOSITORY_ROOT / "Governance" / "AGENTS.consumer.md",
            TEMPLATE_ROOT / "AGENTS.md",
        ),
        (
            REPOSITORY_ROOT / "Scripts" / "validate_game_foundation_project.py",
            TEMPLATE_ROOT / "Scripts" / "validate_game_foundation_project.py",
        ),
    ]
    for name in (
        "Architecture.md",
        "NewProjectSetup.md",
        "FeatureGovernance.md",
        "FigmaUiWorkflow.md",
        "IntegrationAdapters.md",
        "ResourceLoading.md",
    ):
        mappings.append(
            (
                REPOSITORY_ROOT / "Docs" / name,
                TEMPLATE_ROOT / "Docs" / "Foundation" / name,
            )
        )
    mappings.extend(directory_mappings(REPOSITORY_ROOT / "Governance" / "Docs", TEMPLATE_ROOT / "Docs"))
    mappings.extend(directory_mappings(REPOSITORY_ROOT / "Governance" / "GitHub", TEMPLATE_ROOT / "GitHub"))
    for skill_name in (
        "specify-foundation-feature",
        "develop-foundation-feature",
        "normalize-figma-unity-ui",
    ):
        mappings.extend(
            directory_mappings(
                REPOSITORY_ROOT / ".agents" / "skills" / skill_name,
                TEMPLATE_ROOT / "Agents" / "skills" / skill_name,
            )
        )
    return sorted(mappings, key=lambda pair: pair[1].as_posix())


def directory_mappings(source_root: Path, destination_root: Path) -> list[tuple[Path, Path]]:
    """Map all regular files beneath one source tree while preserving relative paths."""
    return [
        (path, destination_root / path.relative_to(source_root))
        for path in source_root.rglob("*")
        if path.is_file()
    ]


def synchronize(check_only: bool) -> list[str]:
    """Copy changed templates or return human-readable drift errors in check-only mode."""
    errors: list[str] = []
    for source, destination in source_mappings():
        if not source.is_file():
            errors.append(f"Missing governance source: {source.relative_to(REPOSITORY_ROOT)}")
            continue
        matches = destination.is_file() and filecmp.cmp(source, destination, shallow=False)
        if matches:
            continue
        if check_only:
            errors.append(
                "Template drift: "
                + destination.relative_to(REPOSITORY_ROOT).as_posix()
                + " != "
                + source.relative_to(REPOSITORY_ROOT).as_posix()
            )
            continue
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, destination)
    return errors


def main() -> int:
    """Synchronize templates or fail CI when checked-in copies are stale."""
    arguments = parse_arguments()
    errors = synchronize(arguments.check)
    if errors:
        for error in errors:
            print(error)
        return 1
    if arguments.check:
        print("Governance template synchronization check passed.")
    else:
        print("Governance templates synchronized into the Setup package.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
