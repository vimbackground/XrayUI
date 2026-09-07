from __future__ import annotations

import importlib.util
import tempfile
import unittest
from pathlib import Path


MODULE = Path(__file__).resolve().parents[1] / "vharness_fusion.py"
SPEC = importlib.util.spec_from_file_location("vharness_fusion", MODULE)
assert SPEC and SPEC.loader
FUSION = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(FUSION)


class InventoryPlanningTests(unittest.TestCase):
    def test_project_content_is_preserved_and_conflict_requires_decision(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            assets, target = root / "assets", root / "target"
            (assets / "_Dev/SOP").mkdir(parents=True)
            (target / "_Dev/SOP").mkdir(parents=True)
            (target / "Tasks/business").mkdir(parents=True)
            (assets / "_Dev/SOP/rule.md").write_text("new", encoding="utf-8")
            (target / "_Dev/SOP/rule.md").write_text("old", encoding="utf-8")
            plan = FUSION.build_plan(assets, target, "fusion")
            self.assertEqual(plan["preserved_project_content"], ["Tasks"])
            self.assertEqual(plan["decision_items"][0]["path"], "_Dev/SOP/rule.md")

    def test_repair_detects_legacy_state_path(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            assets, target = root / "assets", root / "target"
            assets.mkdir()
            (target / "_System/state").mkdir(parents=True)
            plan = FUSION.build_plan(assets, target, "repair")
            self.assertEqual(plan["legacy_candidates"][0]["legacy"], "_System/state")


if __name__ == "__main__":
    unittest.main()
