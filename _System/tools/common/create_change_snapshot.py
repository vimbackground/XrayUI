#!/usr/bin/env python3
"""Create a content-addressed snapshot of project files."""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

# Generated outputs are reproducible from tracked sources and can dwarf a snapshot.
# Keep recovery snapshots focused on authored content, configuration and documentation.
EXCLUDED_PARTS = {".git", "_wip", "_Dist", "bin", "obj", "target", "__pycache__", ".dart_tool", "build"}
EXCLUDED_PREFIX = Path("_Dev/box")


def digest(path: Path) -> str:
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(chunk)
    return value.hexdigest()


def eligible(relative: Path) -> bool:
    return not any(part in EXCLUDED_PARTS for part in relative.parts) and not relative.is_relative_to(EXCLUDED_PREFIX)


def create_snapshot(root: Path, output: Path) -> Path:
    root = root.resolve()
    output = output.resolve()
    if not root.is_dir():
        raise ValueError(f"project root does not exist: {root}")
    if output == root or root in output.parents and output.name == "":
        raise ValueError("snapshot output must be a dedicated directory")
    files_root = output / "files"
    files_root.mkdir(parents=True, exist_ok=False)
    entries = []
    for source in sorted(path for path in root.rglob("*") if path.is_file()):
        relative = source.relative_to(root)
        if not eligible(relative):
            continue
        target = files_root / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
        entries.append({"path": relative.as_posix(), "sha256": digest(target), "size": target.stat().st_size})
    manifest = {
        "format": "vharness-change-snapshot-v1",
        "created_at": datetime.now(timezone.utc).isoformat(),
        "project": "vHarness",
        "files": entries,
    }
    (output / "snapshot.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return output


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--dir", default=".")
    parser.add_argument("--output")
    args = parser.parse_args()
    root = Path(args.dir).resolve()
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    output = Path(args.output) if args.output else root / "_wip" / "change-snapshots" / stamp
    print(create_snapshot(root, output))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
