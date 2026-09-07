from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path

COMMON = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(COMMON))


def load(name: str):
    spec = importlib.util.spec_from_file_location(name, COMMON / f"{name}.py")
    assert spec and spec.loader
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


CREATE = load("create_change_snapshot")
VERIFY = load("verify_change_snapshot")
RESTORE = load("restore_change_snapshot")


class RecoveryToolTests(unittest.TestCase):
    def test_create_damage_preview_restore_verify(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            base = Path(temp)
            project, snapshot = base / "project", base / "snapshot"
            (project / "docs").mkdir(parents=True)
            source = project / "docs" / "state.md"
            source.write_text("healthy\n", encoding="utf-8")
            CREATE.create_snapshot(project, snapshot)
            self.assertEqual(VERIFY.verify(snapshot), [])
            source.write_text("damaged\n", encoding="utf-8")
            self.assertEqual(RESTORE.restore(snapshot, project), ["docs/state.md"])
            self.assertEqual(source.read_text(encoding="utf-8"), "damaged\n")
            RESTORE.restore(snapshot, project, apply=True)
            self.assertEqual(source.read_text(encoding="utf-8"), "healthy\n")
            self.assertEqual(VERIFY.verify(snapshot), [])

    def test_box_is_excluded(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            base = Path(temp)
            project, snapshot = base / "project", base / "snapshot"
            (project / "_Dev" / "box").mkdir(parents=True)
            (project / "_Dev" / "box" / "private.txt").write_text("private", encoding="utf-8")
            CREATE.create_snapshot(project, snapshot)
            self.assertFalse((snapshot / "files" / "_Dev" / "box" / "private.txt").exists())


if __name__ == "__main__":
    unittest.main()
