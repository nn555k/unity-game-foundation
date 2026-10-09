#!/usr/bin/env python3
"""Regression checks for framework-only installation and base readiness."""

import json
from pathlib import Path
import tempfile
import unittest

from install_upm_packages import DEFAULT_SELECTION, resolve_selection, update_manifest
from onboard_project import validate_readiness


class OnboardingDefaultsTests(unittest.TestCase):
    def test_default_selection_excludes_figma(self):
        """Default installation keeps all seven base/provider packages but never opts into Figma."""
        selected = resolve_selection(DEFAULT_SELECTION)
        self.assertEqual(len(selected), 7)
        self.assertIn("yooasset", selected)
        self.assertNotIn("figma", selected)

    def test_removed_figma_alias_is_rejected(self):
        """Removed tooling must fail before writing an unresolvable package reference."""
        with self.assertRaises(ValueError):
            resolve_selection("figma")

    def test_default_install_preserves_existing_figma_reference(self):
        """A project-owned Figma version is not removed or upgraded by normal onboarding."""
        with tempfile.TemporaryDirectory(prefix="foundation-defaults-") as folder:
            root = Path(folder)
            manifest = root / "Packages/manifest.json"
            manifest.parent.mkdir()
            original = "file:../DeveloperFigma"
            external_package = "com.example.design-tools"
            manifest.write_text(json.dumps({"dependencies": {external_package: original}}))
            result = json.loads(update_manifest(root, resolve_selection(DEFAULT_SELECTION), "https://example.invalid/framework.git", "test-ref", False))
            self.assertEqual(result["dependencies"][external_package], original)

    def test_missing_figma_does_not_block_ready_project(self):
        """Absent manual tools are distinct from missing required setup evidence."""
        validate_readiness(self.report())

    def test_missing_required_check_is_rejected(self):
        """A localReady boolean alone cannot conceal missing compile or provider evidence."""
        report = self.report()
        report["checks"] = [check for check in report["checks"] if check["name"] != "compiledArchitecture"]
        with self.assertRaises(RuntimeError):
            validate_readiness(report)

    def test_reported_provider_failure_is_not_silently_accepted(self):
        """Readiness must not conceal an actual failed provider check."""
        report = self.report()
        report["checks"].append({"name": "extraProvider", "status": "failed"})
        with self.assertRaises(RuntimeError):
            validate_readiness(report)

    @staticmethod
    def report():
        """Build a minimal evidence report for a fully initialized project without Figma."""
        return {"localReady": True, "checks": [
            {"name": name, "status": "ready"} for name in ("yooasset", "uiProfile", "compiledArchitecture")
        ]}


if __name__ == "__main__":
    unittest.main()
