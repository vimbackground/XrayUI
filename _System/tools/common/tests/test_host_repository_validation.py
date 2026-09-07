from __future__ import annotations

import subprocess
import sys
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
TOOLS = ROOT / "_System/tools/common"


class HostRepositoryValidationTests(unittest.TestCase):
    def run_validator(self, script: str, *arguments: str) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [sys.executable, "-B", str(TOOLS / script), *arguments],
            cwd=ROOT,
            text=True,
            capture_output=True,
            check=False,
        )

    def test_xrayui_architecture_profile_passes(self) -> None:
        result = self.run_validator(
            "vharness_validator.py", "--dir", str(ROOT), "--profile", "host"
        )
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    def test_xrayui_document_profile_passes(self) -> None:
        result = self.run_validator(
            "document_validator.py", "--dir", str(ROOT), "--profile", "host"
        )
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
