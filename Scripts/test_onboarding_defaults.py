#!/usr/bin/env python3
"""Regression checks for framework-only installation and base readiness."""

import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from install_upm_packages import DEFAULT_SELECTION, resolve_selection, update_manifest
from onboard_project import validate_readiness
import onboard_project
import install_upm_packages
import validate_clean_install
import sync_governance_templates as governance


class OnboardingDefaultsTests(unittest.TestCase):
    def test_default_refs_ship_current_governance(self):
        """Fresh onboarding, direct install and external smoke tests must select the corrected release."""
        for module in (onboard_project, install_upm_packages, validate_clean_install):
            with self.subTest(module=module.__name__):
                arguments = [module.__name__]
                if module is not validate_clean_install:
                    arguments += ["--project", "/tmp/foundation-default-ref-fixture"]
                with patch("sys.argv", arguments):
                    self.assertEqual("v0.7.0", module.parse_arguments().ref)

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

    def test_sdk_governance_is_distributed_with_setup(self):
        """A fresh Setup template tree receives the canonical SDK contract and checklist."""
        with tempfile.TemporaryDirectory(prefix=".sdk-governance-", dir=governance.REPOSITORY_ROOT) as folder:
            with patch.object(governance, "TEMPLATE_ROOT", Path(folder)):
                self.assertEqual(governance.synchronize(False), [])
                for source, target in (
                    ("Docs/SdkInitialization.md", "Docs/Foundation/SdkInitialization.md"),
                    ("Governance/Docs/Integrations/SdkInitializationChecklist.md", "Docs/Integrations/SdkInitializationChecklist.md"),
                    ("Governance/AGENTS.consumer.md", "AGENTS.md"),
                    (".agents/skills/develop-foundation-feature/SKILL.md", "Agents/skills/develop-foundation-feature/SKILL.md"),
                    (".agents/skills/specify-foundation-feature/SKILL.md", "Agents/skills/specify-foundation-feature/SKILL.md"),
                ):
                    self.assertEqual((governance.REPOSITORY_ROOT / source).read_bytes(), (Path(folder) / target).read_bytes())
                self.assertEqual(governance.synchronize(True), [])

    def test_stale_sdk_contract_is_rejected(self):
        """Check-only validation rejects stale distributed instructions without overwriting them."""
        with tempfile.TemporaryDirectory(prefix=".sdk-governance-", dir=governance.REPOSITORY_ROOT) as folder:
            with patch.object(governance, "TEMPLATE_ROOT", Path(folder)):
                self.assertEqual(governance.synchronize(False), [])
                target = Path(folder) / "Docs/Foundation/SdkInitialization.md"
                target.write_text("Outdated SDK contract\n", encoding="utf-8")
                errors = governance.synchronize(True)
                self.assertEqual(len(errors), 1)
                self.assertIn("SdkInitialization.md", errors[0])
                self.assertEqual(target.read_text(encoding="utf-8"), "Outdated SDK contract\n")

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
