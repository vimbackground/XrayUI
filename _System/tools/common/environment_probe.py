#!/usr/bin/env python3
"""Report visible vHarness execution capabilities without guessing."""

from __future__ import annotations

import argparse
import json
import os
import platform
import shutil
import subprocess
from pathlib import Path


MODEL_TIERS = {
    "quick": "fast / economical",
    "implementation": "balanced coding",
    "architecture": "deep reasoning",
    "recovery": "independent strong review",
}


def version(command: str) -> str | None:
    executable = shutil.which(command)
    if not executable:
        return None
    try:
        result = subprocess.run(
            [executable, "--version"],
            capture_output=True,
            text=True,
            timeout=5,
            check=False,
        )
    except (OSError, subprocess.SubprocessError):
        return "available; version unavailable"
    output = (result.stdout or result.stderr).strip().splitlines()
    return output[0] if output else "available; version unavailable"


def probe(root: Path, agent_platform: str, task_type: str) -> dict[str, object]:
    root = root.resolve()
    return {
        "project": "vHarness",
        "agent_platform": agent_platform,
        "platform_confirmation_required": agent_platform == "unknown",
        "recommended_model_tier": MODEL_TIERS[task_type],
        "task_type": task_type,
        "cwd": str(Path.cwd()),
        "target_root": str(root),
        "target_exists": root.is_dir(),
        "is_git_repository": (root / ".git").is_dir(),
        "operating_system": platform.platform(),
        "shell": os.environ.get("SHELL") or os.environ.get("COMSPEC") or "unknown",
        "tools": {
            "python": version("python"),
            "node": version("node"),
            "git": version("git"),
        },
        "network_access": "unknown; must be confirmed by platform or probe",
        "sandbox": "unknown; must be confirmed from current platform context",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Probe visible vHarness capabilities")
    parser.add_argument("--dir", default=".")
    parser.add_argument("--platform", default="unknown")
    parser.add_argument("--task-type", choices=sorted(MODEL_TIERS), default="implementation")
    args = parser.parse_args()
    print(
        json.dumps(
            probe(Path(args.dir), args.platform, args.task_type),
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
