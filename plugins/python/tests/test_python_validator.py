from __future__ import annotations

import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


VALIDATOR = Path(__file__).resolve().parents[1] / "tools" / "python_validator.py"


class PythonValidatorTests(unittest.TestCase):
    def run_validator(self, root: Path) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [sys.executable, str(VALIDATOR), "--dir", str(root)],
            capture_output=True,
            text=True,
            check=False,
        )

    def test_valid_project_reports_config_and_tests(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            (root / "pyproject.toml").write_text("[project]\nname = 'demo'\n", encoding="utf-8")
            (root / "app.py").write_text("def answer():\n    return 42\n", encoding="utf-8")
            tests = root / "tests"
            tests.mkdir()
            (tests / "test_app.py").write_text("def test_answer():\n    assert True\n", encoding="utf-8")

            result = self.run_validator(root)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn("Project configs: pyproject.toml", result.stdout)
            self.assertIn("Test entry points: 1", result.stdout)
            self.assertIn("[OK] Python validation passed.", result.stdout)

    def test_syntax_error_fails_validation(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            (root / "broken.py").write_text("def broken(:\n    pass\n", encoding="utf-8")

            result = self.run_validator(root)

            self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
            self.assertIn("[ERROR] broken.py:1", result.stdout)
            self.assertIn("[FAIL] Python validation found 1 issue(s).", result.stdout)

    def test_empty_project_passes_with_warning(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            result = self.run_validator(Path(temp_dir))

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn("[WARN] No Python files found", result.stdout)
            self.assertIn("[OK] Python validation passed.", result.stdout)


if __name__ == "__main__":
    unittest.main()
