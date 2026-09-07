from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path


MODULE = Path(__file__).resolve().parents[1] / "vharness_fusion.py"
SPEC = importlib.util.spec_from_file_location("vharness_fusion_semantic", MODULE)
assert SPEC and SPEC.loader
FUSION = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(FUSION)


class SemanticMergeTests(unittest.TestCase):
    def test_agent_merge_preserves_project_rules_and_is_idempotent(self) -> None:
        source = "---\nproject: vHarness\n---\n# Framework\nNever overwrite.\n"
        target = "---\nproject: legacy\n---\n# Project\nRun make test.\n"
        first = FUSION.render_merge("Agent.md", source, target)
        second = FUSION.render_merge("Agent.md", source, first)
        self.assertIn("Run make test.", first)
        self.assertIn("Never overwrite.", first)
        self.assertEqual(first, second)

    def test_host_state_outranks_template_default(self) -> None:
        source = "---\nproject: vHarness\n---\n初始化。\n"
        target = "---\nproject: old\n---\nProduction release is blocked by API tests.\n"
        merged = FUSION.render_merge("_System/memory/current_state.md", source, target)
        self.assertIn("Production release", merged)
        self.assertNotIn("初始化。", merged)


if __name__ == "__main__":
    unittest.main()
