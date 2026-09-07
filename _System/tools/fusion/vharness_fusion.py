#!/usr/bin/env python3
"""Plan, apply, verify, and roll back vHarness infrastructure fusion."""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import tempfile
import uuid
from datetime import datetime
from pathlib import Path
from typing import Any


SCHEMA_VERSION = 1
MANIFEST = "ASSET_MANIFEST.json"
MANAGED_MARKER_START = "<!-- vharness:managed:start -->"
MANAGED_MARKER_END = "<!-- vharness:managed:end -->"
SEMANTIC_PATHS = {
    "Agent.md",
    "_System/memory/current_state.md",
    "_System/architecture/task_breakdown.md",
}
PROTECTED_PREFIXES = ("_Dev/box",)
PROJECT_CONTENT = ("Tasks", "Projects")
PLACEHOLDER_MARKERS = (
    "current_phase: initialization",
    "next_action: confirm_project_goal",
    "尚无业务变更或半成品",
)


def now() -> str:
    return datetime.now().astimezone().isoformat(timespec="seconds")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(65536), b""):
            digest.update(chunk)
    return digest.hexdigest()


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, value: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps(value, ensure_ascii=False, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )


def normalize(relative: Path | str) -> str:
    return Path(relative).as_posix()


def is_protected(relative: str) -> bool:
    return any(relative == prefix or relative.startswith(prefix + "/") for prefix in PROTECTED_PREFIXES)


def asset_files(assets: Path) -> list[Path]:
    roots = ("Agent.md", "_System", "_Dev", "plugins")
    files: list[Path] = []
    for name in roots:
        candidate = assets / name
        if candidate.is_file():
            files.append(candidate)
        elif candidate.is_dir():
            files.extend(path for path in candidate.rglob("*") if path.is_file())
    return sorted(path for path in files if path.name != MANIFEST and "__pycache__" not in path.parts)


def legacy_candidates(target: Path) -> list[dict[str, str]]:
    aliases = (
        ("_System/state", "_System/memory"),
        ("harness_validator.py", "_System/tools/common/vharness_validator.py"),
    )
    found: list[dict[str, str]] = []
    for legacy, canonical in aliases:
        if (target / legacy).exists():
            found.append({"legacy": legacy, "canonical": canonical})
    return found


def repair_overrides(target: Path) -> dict[str, str]:
    candidates = {
        "_System/memory/current_state.md": "_System/state/current_state.md",
        "_System/architecture/task_breakdown.md": "_System/tasks/task_breakdown.md",
    }
    return {
        canonical: legacy
        for canonical, legacy in candidates.items()
        if (target / legacy).is_file()
    }


def target_fingerprint(path: Path) -> str | None:
    return sha256(path) if path.is_file() else None


def build_plan(assets: Path, target: Path, mode: str) -> dict[str, Any]:
    assets = assets.resolve()
    target = target.resolve()
    if not assets.is_dir() or not target.is_dir() or assets == target:
        raise ValueError("assets and target must be different existing directories")

    actions: list[dict[str, Any]] = []
    decisions: list[dict[str, Any]] = []
    for source in asset_files(assets):
        relative = normalize(source.relative_to(assets))
        if is_protected(relative):
            actions.append({"action": "preserve", "path": relative, "reason": "protected_path"})
            continue
        destination = target / relative
        base = {
            "path": relative,
            "source_sha256": sha256(source),
            "target_sha256": target_fingerprint(destination),
        }
        if not destination.exists():
            actions.append({**base, "action": "adopt"})
        elif not destination.is_file():
            item = {**base, "action": "conflict", "reason": "target_not_file"}
            actions.append(item)
            decisions.append({"path": relative, "reason": "target_not_file"})
        elif base["source_sha256"] == base["target_sha256"]:
            actions.append({**base, "action": "same"})
        elif relative in SEMANTIC_PATHS:
            actions.append({**base, "action": "merge", "strategy": semantic_strategy(relative)})
        else:
            item = {**base, "action": "conflict", "reason": "different_content"}
            actions.append(item)
            decisions.append({"path": relative, "reason": "different_content"})

    preserved = [name for name in PROJECT_CONTENT if (target / name).exists()]
    overrides = repair_overrides(target) if mode == "repair" else {}
    for canonical, legacy in overrides.items():
        for index, action in enumerate(actions):
            if action.get("path") == canonical:
                legacy_file = target / legacy
                actions[index] = {
                    **action,
                    "action": "repair_merge",
                    "legacy_path": legacy,
                    "legacy_sha256": sha256(legacy_file),
                    "strategy": semantic_strategy(canonical),
                }
                decisions[:] = [item for item in decisions if item.get("path") != canonical]
                break
    plan = {
        "schema_version": SCHEMA_VERSION,
        "migration_id": f"{datetime.now().strftime('%Y%m%dT%H%M%S')}-{uuid.uuid4().hex[:8]}",
        "created_at": now(),
        "mode": mode,
        "assets": str(assets),
        "target": str(target),
        "actions": actions,
        "decision_items": decisions,
        "preserved_project_content": preserved,
        "legacy_candidates": legacy_candidates(target) if mode == "repair" else [],
    }
    return plan


def semantic_strategy(relative: str) -> str:
    if relative == "Agent.md":
        return "project_rules_then_managed_framework_rules"
    if relative.endswith("current_state.md"):
        return "host_state_over_template_default"
    return "host_work_items_over_template_default"


def split_frontmatter(text: str) -> tuple[str, str]:
    lines = text.splitlines()
    if not lines or lines[0] != "---":
        return "", text.strip()
    try:
        end = lines.index("---", 1)
    except ValueError:
        return "", text.strip()
    return "\n".join(lines[: end + 1]), "\n".join(lines[end + 1 :]).strip()


def render_merge(relative: str, source_text: str, target_text: str) -> str:
    source_front, source_body = split_frontmatter(source_text)
    _, target_body = split_frontmatter(target_text)
    front = source_front or "---\nproject: vHarness\n---"
    if relative == "Agent.md":
        if MANAGED_MARKER_START in target_body:
            before = target_body.split(MANAGED_MARKER_START, 1)[0].rstrip()
        else:
            before = target_body.rstrip()
        body = (
            before
            + "\n\n"
            + MANAGED_MARKER_START
            + "\n## vHarness managed framework rules\n\n"
            + source_body
            + "\n"
            + MANAGED_MARKER_END
        )
    else:
        # Host state and work items are facts; template defaults never outrank them.
        body = target_body or source_body
        if any(marker in body for marker in PLACEHOLDER_MARKERS) and source_body:
            body = target_body
    return front.rstrip() + "\n\n" + body.rstrip() + "\n"


def validate_plan_inputs(plan: dict[str, Any]) -> tuple[Path, Path]:
    assets = Path(plan["assets"]).resolve()
    target = Path(plan["target"]).resolve()
    if not assets.is_dir() or not target.is_dir() or assets == target:
        raise ValueError("plan roots are missing or unsafe")
    if plan.get("decision_items"):
        raise ValueError("plan contains unresolved decision_items")
    for action in plan["actions"]:
        if action["action"] not in {"adopt", "merge", "repair_merge"}:
            continue
        source = assets / action["path"]
        destination = target / action["path"]
        if not source.is_file() or sha256(source) != action["source_sha256"]:
            raise ValueError(f"source drift: {action['path']}")
        if target_fingerprint(destination) != action.get("target_sha256"):
            raise ValueError(f"target drift: {action['path']}")
        if action["action"] == "repair_merge":
            legacy = target / action["legacy_path"]
            if not legacy.is_file() or sha256(legacy) != action["legacy_sha256"]:
                raise ValueError(f"legacy source drift: {action['legacy_path']}")
    return assets, target


def migration_root(target: Path, migration_id: str) -> Path:
    return target / "_wip" / "vharness-migrations" / migration_id


def atomic_write(path: Path, content: bytes) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(dir=path.parent, prefix=".vharness-", delete=False) as stream:
        staging = Path(stream.name)
        stream.write(content)
    try:
        staging.replace(path)
    finally:
        if staging.exists():
            staging.unlink()


def apply_plan(plan: dict[str, Any]) -> dict[str, Any]:
    assets, target = validate_plan_inputs(plan)
    run_root = migration_root(target, plan["migration_id"])
    backup_root = run_root / "backup"
    if run_root.exists():
        raise ValueError("migration_id has already been used")
    backup_manifest: dict[str, str] = {}
    applied: list[dict[str, Any]] = []
    run_root.mkdir(parents=True)
    write_json(run_root / "plan.json", plan)
    try:
        for action in plan["actions"]:
            kind = action["action"]
            if kind not in {"adopt", "merge", "repair_merge"}:
                continue
            relative = action["path"]
            source = assets / relative
            destination = target / relative
            if destination.is_file():
                backup = backup_root / relative
                backup.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(destination, backup)
                backup_manifest[relative] = sha256(backup)
            if kind == "repair_merge":
                content = render_merge(
                    relative,
                    source.read_text(encoding="utf-8"),
                    (target / action["legacy_path"]).read_text(encoding="utf-8"),
                ).encode("utf-8")
            elif kind == "merge":
                content = render_merge(
                    relative,
                    source.read_text(encoding="utf-8"),
                    destination.read_text(encoding="utf-8"),
                ).encode("utf-8")
            else:
                content = source.read_bytes()
            atomic_write(destination, content)
            applied.append({"path": relative, "action": kind, "sha256": sha256(destination)})
    except Exception:
        rollback_actions(target, applied, backup_root, backup_manifest)
        raise
    write_json(run_root / "backup_manifest.json", {"files": backup_manifest})
    report = {
        "schema_version": SCHEMA_VERSION,
        "migration_id": plan["migration_id"],
        "status": "applied",
        "applied_at": now(),
        "applied": applied,
        "verified": [],
    }
    write_json(run_root / "report.json", report)
    return report


def rollback_actions(
    target: Path,
    applied: list[dict[str, Any]],
    backup_root: Path,
    backup_manifest: dict[str, str],
) -> None:
    for entry in reversed(applied):
        destination = target / entry["path"]
        backup = backup_root / entry["path"]
        if entry["path"] in backup_manifest:
            atomic_write(destination, backup.read_bytes())
        elif destination.is_file():
            destination.unlink()


def verify_plan(plan: dict[str, Any]) -> dict[str, Any]:
    target = Path(plan["target"]).resolve()
    run_root = migration_root(target, plan["migration_id"])
    report = read_json(run_root / "report.json")
    verified: list[dict[str, Any]] = []
    for entry in report["applied"]:
        path = target / entry["path"]
        ok = path.is_file() and sha256(path) == entry["sha256"]
        verified.append({"path": entry["path"], "ok": ok})
    preserved = all((target / name).exists() for name in plan.get("preserved_project_content", []))
    if not all(item["ok"] for item in verified) or not preserved:
        raise ValueError("verification contract failed")
    report["status"] = "verified"
    report["verified_at"] = now()
    report["verified"] = verified
    report["project_content_preserved"] = preserved
    write_json(run_root / "report.json", report)
    return report


def rollback_plan(plan: dict[str, Any]) -> dict[str, Any]:
    target = Path(plan["target"]).resolve()
    run_root = migration_root(target, plan["migration_id"])
    report = read_json(run_root / "report.json")
    manifest_path = run_root / "backup_manifest.json"
    manifest = read_json(manifest_path).get("files", {}) if manifest_path.is_file() else {}
    for entry in report["applied"]:
        current = target / entry["path"]
        if not current.is_file() or sha256(current) != entry["sha256"]:
            raise ValueError(f"post-apply drift blocks rollback: {entry['path']}")
    rollback_actions(target, report["applied"], run_root / "backup", manifest)
    report["status"] = "rolled_back"
    report["rolled_back_at"] = now()
    write_json(run_root / "report.json", report)
    return report


def command_plan(args: argparse.Namespace, mode: str) -> int:
    plan = build_plan(Path(args.assets), Path(args.target), mode)
    write_json(Path(args.output), plan)
    print(f"[PLAN] {args.output}")
    print(f"[ACTIONS] {len(plan['actions'])}; [DECISIONS] {len(plan['decision_items'])}")
    return 1 if plan["decision_items"] else 0


def main() -> int:
    parser = argparse.ArgumentParser(description="vHarness deep fusion and repair")
    subparsers = parser.add_subparsers(dest="command", required=True)
    for name in ("plan", "repair"):
        sub = subparsers.add_parser(name)
        sub.add_argument("--assets", required=True)
        sub.add_argument("--target", required=True)
        sub.add_argument("--output", required=True)
    for name in ("apply", "verify", "rollback"):
        sub = subparsers.add_parser(name)
        sub.add_argument("--plan", required=True)
    args = parser.parse_args()
    try:
        if args.command in {"plan", "repair"}:
            return command_plan(args, "repair" if args.command == "repair" else "fusion")
        plan = read_json(Path(args.plan))
        result = {
            "apply": apply_plan,
            "verify": verify_plan,
            "rollback": rollback_plan,
        }[args.command](plan)
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0
    except (OSError, UnicodeError, ValueError, KeyError, json.JSONDecodeError) as error:
        print(f"[ERROR] {error}")
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
