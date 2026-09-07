#!/usr/bin/env python3
"""Verify every file recorded in a vHarness change snapshot."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def verify(snapshot: Path) -> list[str]:
    snapshot = snapshot.resolve()
    manifest = json.loads((snapshot / "snapshot.json").read_text(encoding="utf-8"))
    errors = []
    for entry in manifest.get("files", []):
        path = snapshot / "files" / entry["path"]
        if not path.is_file():
            errors.append(f"missing: {entry['path']}")
        elif digest(path) != entry["sha256"]:
            errors.append(f"hash mismatch: {entry['path']}")
    return errors


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--snapshot", required=True)
    args = parser.parse_args()
    errors = verify(Path(args.snapshot))
    for error in errors:
        print(f"[FAIL] {error}")
    print(f"[{'PASS' if not errors else 'FAIL'}] snapshot integrity")
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
