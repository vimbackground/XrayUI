from __future__ import annotations

import importlib.util
import tempfile
import unittest
from pathlib import Path

MODULE_PATH = Path(__file__).resolve().parents[1] / "environment_probe.py"
SPEC = importlib.util.spec_from_file_location("environment_probe", MODULE_PATH)
assert SPEC and SPEC.loader
PROBE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PROBE)


class EnvironmentProbeTests(unittest.TestCase):
    def test_unknown_platform_requires_confirmation(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            result = PROBE.probe(Path(temp), "unknown", "architecture")
        self.assertTrue(result["platform_confirmation_required"])
        self.assertEqual(result["recommended_model_tier"], "deep reasoning")
        self.assertEqual(result["project"], "vHarness")

    def test_named_platform_is_reported(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            result = PROBE.probe(Path(temp), "Codex", "implementation")
        self.assertFalse(result["platform_confirmation_required"])
        self.assertEqual(result["agent_platform"], "Codex")


if __name__ == "__main__":
    unittest.main()
