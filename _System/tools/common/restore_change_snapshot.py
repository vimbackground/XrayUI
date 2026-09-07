#!/usr/bin/env python3
"""Preview or apply restoration from a verified vHarness snapshot."""

from __future__ import annotations

import argparse
import json
import shutil
from datetime import datetime
from pathlib import Path

from verify_change_snapshot import verify


def restore(snapshot: Path, target: Path, apply: bool = False) -> list[str]:
    snapshot, target = snapshot.resolve(), target.resolve()
    errors = verify(snapshot)
    if errors:
        raise ValueError("snapshot integrity verification failed")
    manifest = json.loads((snapshot / "snapshot.json").read_text(encoding="utf-8"))
    changes = []
    backup = target / "_wip" / f"restore-backup-{datetime.now().strftime('%Y%m%d-%H%M%S')}"
    for entry in manifest["files"]:
        relative = Path(entry["path"])
        source, destination = snapshot / "files" / relative, target / relative
        if destination.is_file() and destination.read_bytes() == source.read_bytes():
            continue
        changes.append(relative.as_posix())
        if apply:
            if destination.exists():
                backup_path = backup / relative
                backup_path.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(destination, backup_path)
            destination.parent.mkdir(parents=True, exist_ok=True)
            staging = destination.with_name(destination.name + ".vharness-restore.tmp")
            shutil.copy2(source, staging)
            staging.replace(destination)
    return changes


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--snapshot", required=True)
    parser.add_argument("--target", default=".")
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    changes = restore(Path(args.snapshot), Path(args.target), args.apply)
    mode = "APPLY" if args.apply else "PREVIEW"
    for path in changes:
        print(f"[{mode}] {path}")
    print(f"[{mode}] {len(changes)} file(s)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
