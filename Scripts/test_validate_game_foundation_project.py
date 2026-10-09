#!/usr/bin/env python3
"""Unit tests for the consuming-project governance validator."""

from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

import validate_game_foundation_project as validator


class ProjectValidatorTests(unittest.TestCase):
    """Exercise structural and Feature Spec invariants in isolated directories."""

    def setUp(self) -> None:
        """Create a valid minimal governed project for each test."""
        self.temporary = tempfile.TemporaryDirectory()
        self.project = Path(self.temporary.name)
        self.code_root = self.project / "Assets" / "Scripts" / "Game"
        for role in validator.ROLE_FOLDERS:
            (self.code_root / role).mkdir(parents=True, exist_ok=True)
        (self.code_root / "Architecture" / "SampleGameApp.cs").write_text(
            "public sealed class SampleGameApp {}\n", encoding="utf-8"
        )
        (self.code_root / "Architecture" / "SampleGameController.cs").write_text(
            "public abstract class SampleGameController {}\n", encoding="utf-8"
        )
        config_root = self.project / ".gamefoundation"
        config_root.mkdir()
        (config_root / "project.json").write_text(
            json.dumps(
                {
                    "schemaVersion": 1,
                    "projectName": "SampleGame",
                    "rootNamespace": "Sample.Game",
                    "codeRoot": "Assets/Scripts/Game",
                    "architectureFile": "Assets/Scripts/Game/Architecture/SampleGameApp.cs",
                    "controllerFile": "Assets/Scripts/Game/Architecture/SampleGameController.cs",
                    "uiConventionProfile": "Assets/Settings/GameFoundation/SampleGameUiPrefabConvention.asset",
                }
            ),
            encoding="utf-8",
        )
        self._write_governance_files()

    def tearDown(self) -> None:
        """Delete the isolated project tree after each test."""
        self.temporary.cleanup()

    def _write_governance_files(self) -> None:
        """Create the managed files required by the validator without Unity assets."""
        files = {
            "AGENTS.md": "<!-- GAME FOUNDATION MANAGED START -->\nmanaged\n<!-- GAME FOUNDATION MANAGED END -->\n",
            ".agents/skills/specify-foundation-feature/SKILL.md": "skill\n",
            ".agents/skills/develop-foundation-feature/SKILL.md": "skill\n",
            ".agents/skills/normalize-figma-unity-ui/SKILL.md": "skill\n",
            "Docs/Foundation/Architecture.md": "architecture\n",
            "Docs/Features/README.md": "workflow\n",
            "Docs/Features/FEATURE_TEMPLATE.md": "template\n",
            "Scripts/validate_game_foundation_project.py": "validator\n",
            ".github/workflows/game-foundation-governance.yml": "workflow\n",
        }
        for relative, content in files.items():
            path = self.project / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(content, encoding="utf-8")

    def test_valid_minimal_project_passes(self) -> None:
        """Accept generated architecture before the first behavior feature is added."""
        report = validator.validate_project(self.project)
        self.assertEqual([], report.errors)

    def test_nested_role_folder_fails(self) -> None:
        """Reject feature-owned role trees beneath the centralized project roles."""
        (self.code_root / "Systems" / "Economy" / "Models").mkdir(parents=True)
        report = validator.validate_project(self.project)
        self.assertTrue(any("Nested role folder" in error for error in report.errors))

    def test_resources_and_direct_reference_art_can_coexist(self) -> None:
        """Accept separate dynamic and direct-reference files while ignoring editor-only collisions."""
        for relative in ("Assets/Resources/UI/icon.txt", "Assets/Sprites/UI/icon.txt",
                         "Assets/Editor/Resources/UI/icon.txt", "Assets/Resources/UI/icon.txt.meta"):
            path = self.project / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("fixture", encoding="utf-8")
        self.assertEqual([], validator.validate_project(self.project).errors)

    def test_duplicate_resources_key_fails_across_roots_and_extensions(self) -> None:
        """Reject case-insensitive path collisions, even across Resources roots and asset types."""
        for relative in ("Assets/Resources/UI/Settings.txt", "Assets/Feature/Resources/ui/settings.json"):
            path = self.project / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("{}", encoding="utf-8")
        errors = validator.validate_project(self.project).errors
        self.assertTrue(any("Duplicate Resources key 'ui/settings'" in error for error in errors))

    def test_resources_inside_sprites_fails(self) -> None:
        """Prevent direct-reference artwork from accidentally becoming a Resources directory."""
        (self.project / "Assets/Sprites/UI/Resources").mkdir(parents=True)
        errors = validator.validate_project(self.project).errors
        self.assertTrue(any("Sprites tree must not contain Resources" in error for error in errors))

    def test_nested_resources_fails(self) -> None:
        """Reject ambiguous nested Resources roots instead of guessing a load key."""
        path = self.project / "Assets/Resources/UI/Resources/icon.txt"
        path.parent.mkdir(parents=True)
        path.write_text("fixture", encoding="utf-8")
        errors = validator.validate_project(self.project).errors
        self.assertTrue(any("Nested Resources folders" in error for error in errors))

    def test_project_source_outside_code_root_fails(self) -> None:
        """Reject sibling business-code trees that bypass the centralized role folders."""
        sibling = self.project / "Assets" / "Scripts" / "Feature" / "LooseController.cs"
        sibling.parent.mkdir(parents=True)
        sibling.write_text("public sealed class LooseController {}\n", encoding="utf-8")
        report = validator.validate_project(self.project)
        self.assertTrue(any("outside configured codeRoot" in error for error in report.errors))

    def test_verified_spec_requires_complete_evidence(self) -> None:
        """Reject a Verified spec that still contains placeholders or unchecked work."""
        spec = self.project / "Docs" / "Features" / "sample-feature.md"
        spec.write_text(
            "---\nid: sample-feature\ntitle: Sample\nstatus: Verified\nowner: Project\ncreated: 2026-10-08\nupdated: 2026-10-08\n---\n\n"
            + "\n".join(validator.REQUIRED_SPEC_HEADINGS)
            + "\n\n- [ ] TBD\nPending\n",
            encoding="utf-8",
        )
        report = validator.validate_project(self.project)
        self.assertTrue(any("cannot contain TBD" in error for error in report.errors))
        self.assertTrue(any("Pending evidence" in error for error in report.errors))
        self.assertTrue(any("unchecked" in error for error in report.errors))

    def test_governed_change_classification_is_scoped(self) -> None:
        """Require specs for behavior files but not ordinary documentation edits."""
        self.assertTrue(
            validator.governed_change(
                "Assets/Scripts/Game/Commands/BuyCommand.cs",
                "Assets/Scripts/Game",
            )
        )
        self.assertTrue(validator.governed_change("Assets/UI/Shop.prefab", "Assets/Scripts/Game"))
        self.assertTrue(validator.governed_change("Assets/UI/Shop.png", "Assets/Scripts/Game"))
        self.assertTrue(validator.governed_change("Assets/Shaders/Glow.shader", "Assets/Scripts/Game"))
        self.assertTrue(validator.governed_change("ProjectSettings/ProjectSettings.asset", "Assets/Scripts/Game"))
        self.assertFalse(validator.governed_change("README.md", "Assets/Scripts/Game"))


if __name__ == "__main__":
    unittest.main()
