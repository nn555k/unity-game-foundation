"""Regression cases for partial role ownership and false-negative prevention."""

from pathlib import Path
import tempfile
import unittest

import validate_game_foundation_project as VALIDATOR


class RoleValidationTests(unittest.TestCase):
    """Use isolated fixtures rather than relying on the actual project tree."""

    def check_sources(self, sources):
        """Validate an isolated role tree before its temporary files are removed."""
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            code = root / "Assets/Scripts/Game"
            for relative, source in sources.items():
                path = code / relative
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(source)
            report = VALIDATOR.ValidationReport()
            VALIDATOR.validate_role_sources(root, code, report)
            return report

    def test_partial_system_inherits_only_once(self):
        """Legitimate same-namespace partial files share the role declaration."""
        report = self.check_sources({
            "Systems/Board.cs": "namespace App { public sealed partial class Board : AbstractSystem {} }",
            "Systems/Board.Flow.cs": "namespace App { public sealed partial class Board { void Tick() {} } }",
        })
        self.assertEqual([], report.errors + report.warnings)

    def test_same_name_in_other_namespace_is_not_exempt(self):
        """A helper cannot borrow a system declaration from another namespace."""
        report = self.check_sources({
            "Systems/Board.cs": "namespace App { partial class Board : AbstractSystem {} }",
            "Systems/Helper.cs": "namespace Other { partial class Board {} }",
        })
        self.assertEqual(1, len(report.warnings))

    def test_unrelated_helpers_still_warn(self):
        """One valid class must not hide a non-system class in the same file."""
        report = self.check_sources({
            "Systems/Mixed.cs": "namespace App { partial class Board : AbstractSystem {} class Helper {} }",
        })
        self.assertEqual(1, len(report.warnings))
        self.assertIn("Helper", report.warnings[0])

    def test_comments_and_strings_do_not_claim_a_role(self):
        """Mentioning AbstractSystem in documentation does not satisfy inheritance."""
        report = self.check_sources({
            "Systems/Helper.cs": 'namespace App { /* class Fake : AbstractSystem {} */ class Helper { string hint = "AbstractSystem"; } }',
        })
        self.assertEqual(1, len(report.warnings))

    def test_utility_role_violation_is_still_an_error(self):
        """A real model in Utilities remains blocking."""
        report = self.check_sources({"Utilities/Wrong.cs": "namespace App { class State : AbstractModel {} }"})
        self.assertEqual(1, len(report.errors))

    def test_file_scoped_partial_command_is_supported(self):
        """File-scoped namespaces follow the same partial ownership rules."""
        report = self.check_sources({
            "Commands/Save.cs": "namespace App; partial class Save : AbstractCommand {}",
            "Commands/Save.Payload.cs": "namespace App; partial class Save {}",
        })
        self.assertEqual([], report.warnings)


if __name__ == "__main__":
    unittest.main()
