#!/usr/bin/env python3
"""Install selected Game Foundation Git packages into a Unity manifest."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


PACKAGE_IDS = {
    "core": "com.nn555k.gamefoundation.core",
    "save": "com.nn555k.gamefoundation.save",
    "hotupdate": "com.nn555k.gamefoundation.hotupdate",
    "yooasset": "com.nn555k.gamefoundation.hotupdate.yooasset",
    "sdk": "com.nn555k.gamefoundation.sdk",
    "ui": "com.nn555k.gamefoundation.ui",
    "setup": "com.nn555k.gamefoundation.setup",
}

YOOASSET_PACKAGE_ID = "com.tuyoogame.yooasset"
YOOASSET_GIT_URL = "https://github.com/tuyoogame/YooAsset.git?path=Assets/YooAsset#2.3.18"
DEFAULT_SELECTION = "core,save,hotupdate,yooasset,sdk,ui,setup"


def parse_arguments() -> argparse.Namespace:
    """Parse project, package selection and Git reference arguments."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", required=True, help="Unity project root")
    parser.add_argument(
        "--include",
        default=DEFAULT_SELECTION,
        help="Comma-separated package aliases",
    )
    parser.add_argument(
        "--repository",
        default="https://github.com/nn555k/unity-game-foundation.git",
        help="Git repository URL",
    )
    parser.add_argument("--ref", default="v0.6.0", help="Git branch, tag or commit")
    parser.add_argument("--dry-run", action="store_true", help="Print without writing")
    return parser.parse_args()


def resolve_selection(raw_selection: str) -> list[str]:
    """Validate aliases and include transitive Git dependencies explicitly."""
    selected = {value.strip().lower() for value in raw_selection.split(",") if value.strip()}
    unknown = sorted(selected.difference(PACKAGE_IDS))
    if unknown:
        raise ValueError("Unknown package aliases: " + ", ".join(unknown))

    selected.add("core")
    if "yooasset" in selected:
        selected.add("hotupdate")
    if "setup" in selected:
        selected.update(("save", "hotupdate", "sdk", "ui"))
    return [alias for alias in PACKAGE_IDS if alias in selected]


def update_manifest(
    project_root: Path,
    aliases: list[str],
    repository: str,
    git_ref: str,
    dry_run: bool,
) -> str:
    """Add deterministic Git package URLs while preserving unrelated dependencies."""
    manifest_path = project_root.resolve() / "Packages" / "manifest.json"
    if not manifest_path.is_file():
        raise FileNotFoundError(f"Unity manifest not found: {manifest_path}")

    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    dependencies = manifest.setdefault("dependencies", {})
    for alias in aliases:
        package_id = PACKAGE_IDS[alias]
        dependencies[package_id] = (
            f"{repository}?path=/Packages/{package_id}#{git_ref}"
        )
    if "yooasset" in aliases:
        dependencies[YOOASSET_PACKAGE_ID] = YOOASSET_GIT_URL

    rendered = json.dumps(manifest, ensure_ascii=False, indent=2) + "\n"
    if not dry_run:
        manifest_path.write_text(rendered, encoding="utf-8")
    return rendered


def main() -> int:
    """Install selected packages and print the resulting manifest path or preview."""
    arguments = parse_arguments()
    aliases = resolve_selection(arguments.include)
    rendered = update_manifest(
        Path(arguments.project),
        aliases,
        arguments.repository,
        arguments.ref,
        arguments.dry_run,
    )
    if arguments.dry_run:
        print(rendered, end="")
    else:
        print(f"Installed {len(aliases)} Game Foundation package references.")
        print("Ensure the project provides an assembly named QFramework before Unity imports them.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
