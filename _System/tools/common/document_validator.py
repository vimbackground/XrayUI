#!/usr/bin/env python3
"""Validate vHarness Markdown metadata and directory README coverage."""

from __future__ import annotations

import argparse
import json
import re
from datetime import datetime
from pathlib import Path


REQUIRED_KEYS = {
    "id",
    "title",
    "document_type",
    "status",
    "version",
    "project",
    "owner",
    "audience",
    "scope",
    "created_at",
    "updated_at",
    "tags",
}
ALLOWED_STATUS = {
    "active",
    "archived",
    "deprecated",
    "draft",
    "in_progress",
    "superseded",
    "verified",
}
DEFAULT_PROJECT = "vHarness"
SKIP_DIRS = {".git", "_wip", "__pycache__"}
HOST_MANAGED_ROOTS = ("_System", "_Dev", "plugins")
KEY_PATTERN = re.compile(r"^([A-Za-z_][A-Za-z0-9_-]*):(?:\s*(.*))?$")
VERSION_PATTERN = re.compile(r"^\d+\.\d+\.\d+$")


def is_generated_document(path: Path) -> bool:
    normalized = path.as_posix()
    return any(
        marker in normalized
        for marker in (
            "vharness_upgrade/assets",
            "vharness_template",
            "vharness_upgrade/fusion_toolkit",
        )
    )


def markdown_files(root: Path, profile: str = "repository") -> list[Path]:
    if profile == "host":
        paths: set[Path] = set()
        agent = root / "Agent.md"
        if agent.is_file():
            paths.add(agent)
        for name in HOST_MANAGED_ROOTS:
            managed_root = root / name
            if managed_root.is_dir():
                paths.update(path for path in managed_root.rglob("*.md") if path.is_file())
        return sorted(
            path
            for path in paths
            if not any(part in SKIP_DIRS for part in path.relative_to(root).parts)
        )
    return sorted(
        path
        for path in root.rglob("*.md")
        if path.is_file() and not any(part in SKIP_DIRS for part in path.relative_to(root).parts)
    )


def parse_frontmatter(path: Path) -> tuple[dict[str, str], list[str]]:
    errors: list[str] = []
    try:
        text = path.read_text(encoding="utf-8")
    except UnicodeDecodeError as error:
        return {}, [f"UTF-8 decode error: {error.reason}"]
    lines = text.splitlines()
    if not lines or lines[0] != "---":
        return {}, ["missing opening Frontmatter delimiter"]
    try:
        end = lines.index("---", 1)
    except ValueError:
        return {}, ["missing closing Frontmatter delimiter"]

    data: dict[str, str] = {}
    for line in lines[1:end]:
        match = KEY_PATTERN.match(line)
        if match:
            key, value = match.groups()
            if key in data:
                errors.append(f"duplicate Frontmatter key: {key}")
            data[key] = (value or "").strip().strip('"').strip("'")
    return data, errors


def expected_readme_directories(root: Path, profile: str = "repository") -> set[Path]:
    if profile == "host":
        expected: set[Path] = set()
        for name in HOST_MANAGED_ROOTS:
            managed_root = root / name
            if not managed_root.is_dir():
                continue
            expected.add(managed_root)
            expected.update(path.parent for path in managed_root.rglob("README.md"))
        return expected

    expected: set[Path] = set()
    for child in root.iterdir():
        if child.is_dir() and child.name not in SKIP_DIRS:
            expected.add(child)
            if child.name != "_Dist":
                expected.update(
                    item for item in child.iterdir() if item.is_dir() and item.name not in SKIP_DIRS
                )

    for release_name in ("vharness_template", "vharness_upgrade"):
        release_root = root / "_Dist" / release_name
        if not release_root.is_dir():
            continue
        expected.add(release_root)
        for child in release_root.iterdir():
            if child.is_dir() and child.name not in SKIP_DIRS:
                expected.add(child)
                expected.update(
                    item for item in child.iterdir() if item.is_dir() and item.name not in SKIP_DIRS
                )
    return expected


def standard_constraints(root: Path) -> tuple[str, set[str]]:
    naming = root / "_System/standards/naming_rules.json"
    metadata = root / "_System/standards/schemas/document_metadata.schema.json"
    if not naming.is_file() or not metadata.is_file():
        return DEFAULT_PROJECT, ALLOWED_STATUS
    try:
        naming_data = json.loads(naming.read_text(encoding="utf-8"))
        metadata_data = json.loads(metadata.read_text(encoding="utf-8"))
        project = naming_data["product_name"]
        statuses = set(metadata_data["properties"]["status"]["enum"])
    except (OSError, UnicodeError, json.JSONDecodeError, KeyError, TypeError):
        return DEFAULT_PROJECT, ALLOWED_STATUS
    return project, statuses


def validate_document(
    path: Path,
    root: Path,
    *,
    expected_projects: set[str] | None = None,
    allowed_status: set[str] = ALLOWED_STATUS,
) -> tuple[str | None, list[str]]:
    data, errors = parse_frontmatter(path)
    if not data:
        return None, errors

    missing = sorted(REQUIRED_KEYS - data.keys())
    if missing:
        errors.append("missing Frontmatter keys: " + ", ".join(missing))
    expected_projects = expected_projects or {DEFAULT_PROJECT}
    if data.get("project") not in expected_projects:
        errors.append("project must be one of: " + ", ".join(sorted(expected_projects)))
    if data.get("status") not in allowed_status:
        errors.append("invalid status: " + data.get("status", "<missing>"))
    if data.get("version") and not VERSION_PATTERN.fullmatch(data["version"]):
        errors.append("version must use MAJOR.MINOR.PATCH")
    for field in ("created_at", "updated_at"):
        value = data.get(field)
        if value:
            try:
                parsed = datetime.fromisoformat(value)
                if parsed.tzinfo is None:
                    errors.append(f"{field} must include timezone")
            except ValueError:
                errors.append(f"{field} must be ISO 8601")
    scope = path.parent.relative_to(root).as_posix() or "repository"
    valid_scopes = {scope, ".", "repository"}
    for content_root in (
        root / "_Dist" / "vharness_template",
        root / "_Dist" / "vharness_upgrade",
        root / "_Dist" / "vharness_upgrade" / "assets",
    ):
        try:
            local_scope = path.parent.relative_to(content_root).as_posix() or "repository"
            valid_scopes.add(local_scope)
        except ValueError:
            pass
    assets_root = root / "_Dist" / "vharness_upgrade" / "assets"
    try:
        generated_parent = path.parent.relative_to(assets_root)
        template_scope = Path("_Dist/vharness_template") / generated_parent
        valid_scopes.add(template_scope.as_posix())
    except ValueError:
        pass
    fusion_release = root / "_Dist" / "vharness_upgrade" / "fusion_toolkit"
    try:
        generated_parent = path.parent.relative_to(fusion_release)
        source_scope = Path("_System/tools/fusion") / generated_parent
        valid_scopes.add(source_scope.as_posix())
    except ValueError:
        pass
    if data.get("scope") not in valid_scopes:
        errors.append(f"scope does not match a content root: {data.get('scope')}")
    return data.get("id"), errors


def validate_repository(root: Path, profile: str = "repository") -> int:
    root = root.resolve()
    if not root.is_dir():
        print(f"[ERROR] Target directory does not exist: {root}")
        return 2

    issues: list[tuple[Path, str]] = []
    ids: dict[str, Path] = {}
    expected_project, allowed_status = standard_constraints(root)
    allowed_projects = {expected_project}
    if profile == "host":
        agent = root / "Agent.md"
        if agent.is_file():
            agent_data, _ = parse_frontmatter(agent)
            if agent_data.get("project"):
                allowed_projects.add(agent_data["project"])
    documents = markdown_files(root, profile)
    for path in documents:
        doc_id, errors = validate_document(
            path,
            root,
            expected_projects=allowed_projects,
            allowed_status=allowed_status,
        )
        for error in errors:
            issues.append((path, error))
        if doc_id:
            if doc_id in ids:
                first = ids[doc_id]
                if not (is_generated_document(path) or is_generated_document(first)):
                    issues.append((path, f"duplicate id also used by {first.relative_to(root)}"))
            else:
                ids[doc_id] = path

    expected = expected_readme_directories(root, profile)
    missing_readmes = sorted(path for path in expected if not (path / "README.md").is_file())
    for directory in missing_readmes:
        issues.append((directory, "missing README.md"))

    print("=== vHarness Documentation Validation ===")
    print(f"[*] Markdown documents: {len(documents)}")
    print(f"[*] Logical document IDs: {len(ids)}")
    print(f"[*] README directories checked: {len(expected)}")
    for path, message in issues:
        print(f"[ERROR] {path.relative_to(root)}: {message}")
    if issues:
        print(f"[FAIL] Documentation validation found {len(issues)} issue(s).")
        return 1
    print("[OK] Documentation metadata and README coverage passed.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate vHarness documentation")
    parser.add_argument("--dir", default=".", help="repository root")
    parser.add_argument("--profile", choices=("host", "repository"), default="repository")
    args = parser.parse_args()
    return validate_repository(Path(args.dir), args.profile)


if __name__ == "__main__":
    raise SystemExit(main())
