from __future__ import annotations

import importlib.util
import tempfile
import unittest
from pathlib import Path


MODULE = Path(__file__).resolve().parents[1] / "vharness_fusion.py"
SPEC = importlib.util.spec_from_file_location("vharness_fusion_apply", MODULE)
assert SPEC and SPEC.loader
FUSION = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(FUSION)


class ApplyRepairRollbackTests(unittest.TestCase):
    def make_roots(self, root: Path) -> tuple[Path, Path]:
        assets, target = root / "assets", root / "target"
        (assets / "_System/tools/common").mkdir(parents=True)
        target.mkdir()
        (assets / "_System/tools/common/tool.py").write_text("print('ok')\n", encoding="utf-8")
        return assets, target

    def test_apply_verify_rollback_and_project_content_hash(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            assets, target = self.make_roots(root)
            business = target / "Projects/app/source.txt"
            business.parent.mkdir(parents=True)
            business.write_text("business", encoding="utf-8")
            before = FUSION.sha256(business)
            plan = FUSION.build_plan(assets, target, "repair")
            FUSION.apply_plan(plan)
            report = FUSION.verify_plan(plan)
            self.assertEqual(report["status"], "verified")
            self.assertEqual(FUSION.sha256(business), before)
            FUSION.rollback_plan(plan)
            self.assertFalse((target / "_System/tools/common/tool.py").exists())
            self.assertEqual(FUSION.sha256(business), before)

    def test_input_drift_blocks_apply(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            assets, target = self.make_roots(Path(temp))
            plan = FUSION.build_plan(assets, target, "fusion")
            (assets / "_System/tools/common/tool.py").write_text("changed", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "source drift"):
                FUSION.apply_plan(plan)

    def test_unresolved_conflict_blocks_apply(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            assets, target = self.make_roots(Path(temp))
            existing = target / "_System/tools/common/tool.py"
            existing.parent.mkdir(parents=True)
            existing.write_text("custom", encoding="utf-8")
            plan = FUSION.build_plan(assets, target, "repair")
            with self.assertRaisesRegex(ValueError, "decision_items"):
                FUSION.apply_plan(plan)

    def test_repair_restores_legacy_state_over_template_placeholder(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            assets, target = root / "assets", root / "target"
            source = assets / "_System/memory/current_state.md"
            current = target / "_System/memory/current_state.md"
            legacy = target / "_System/state/current_state.md"
            source.parent.mkdir(parents=True)
            current.parent.mkdir(parents=True)
            legacy.parent.mkdir(parents=True)
            source.write_text("---\nproject: vHarness\n---\n初始化。\n", encoding="utf-8")
            current.write_text("---\nproject: vHarness\n---\n初始化。\n", encoding="utf-8")
            legacy.write_text("Production migration is blocked.\n", encoding="utf-8")
            plan = FUSION.build_plan(assets, target, "repair")
            action = next(item for item in plan["actions"] if item["path"].endswith("current_state.md"))
            self.assertEqual(action["action"], "repair_merge")
            FUSION.apply_plan(plan)
            self.assertIn("Production migration is blocked", current.read_text(encoding="utf-8"))


if __name__ == "__main__":
    unittest.main()
