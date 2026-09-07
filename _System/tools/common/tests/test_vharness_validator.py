from __future__ import annotations

import importlib.util
import tempfile
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "vharness_validator.py"
SPEC = importlib.util.spec_from_file_location("vharness_validator", MODULE_PATH)
assert SPEC and SPEC.loader
VALIDATOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(VALIDATOR)


class VHarnessValidatorTests(unittest.TestCase):
    def make_repository(self, root: Path) -> None:
        (root / "Agent.md").write_text("# Agent\n", encoding="utf-8")
        for name in ("_Dev", "_Dist", "_System"):
            (root / name).mkdir()

    def make_host(self, root: Path) -> None:
        (root / "Agent.md").write_text("# Agent\n", encoding="utf-8")
        for name in ("_Dev", "_System"):
            (root / name).mkdir()

    def test_valid_repository_structure_passes(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self.make_repository(root)
            self.assertTrue(VALIDATOR.validate_target_structure(root, "repository"))
            self.assertTrue(VALIDATOR.validate_host_content_boundary(root))
            self.assertTrue(VALIDATOR.validate_no_absolute_paths(root))

    def test_missing_core_structure_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            self.assertFalse(
                VALIDATOR.validate_target_structure(Path(temp), "repository")
            )

    def test_host_structure_does_not_require_dist(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self.make_host(root)
            self.assertEqual(VALIDATOR.detect_profile(root), "host")
            self.assertTrue(VALIDATOR.validate_target_structure(root, "host"))

    def test_host_business_directories_are_not_constrained(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "Tasks" / "demo").mkdir(parents=True)
            self.assertTrue(VALIDATOR.validate_host_content_boundary(root))

    def test_invalid_utf8_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "bad.md").write_bytes(bytes([255, 254, 253]))
            self.assertFalse(VALIDATOR.validate_no_absolute_paths(root))

    def test_windows_absolute_path_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "bad.py").write_text(
                "path = " + chr(34) + "Q:" + "/private/file.txt" + chr(34) + "\n",
                encoding="utf-8",
            )
            self.assertFalse(VALIDATOR.validate_no_absolute_paths(root))

    def test_https_url_is_not_a_windows_path(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "README.md").write_text(
                "See https://example.com/docs\n", encoding="utf-8"
            )
            self.assertTrue(VALIDATOR.validate_no_absolute_paths(root))

    def test_generated_output_is_excluded(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "obj").mkdir()
            (root / "obj/project.assets.json").write_text(
                '{"path":"' + "Q:" + '/generated/cache"}\n', encoding="utf-8"
            )
            self.assertTrue(VALIDATOR.validate_no_absolute_paths(root))


if __name__ == "__main__":
    unittest.main()
