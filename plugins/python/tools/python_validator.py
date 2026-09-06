#!/usr/bin/env python3
"""Side-effect-free validation for an optional Python vHarness project."""

from __future__ import annotations

import argparse
import ast
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


CONFIG_FILES = (
    "pyproject.toml",
    "setup.cfg",
    "setup.py",
    "requirements.txt",
    "Pipfile",
)

EXCLUDED_DIRS = {
    ".git",
    ".hg",
    ".mypy_cache",
    ".pytest_cache",
    ".ruff_cache",
    ".tox",
    ".venv",
    "__pycache__",
    "_wip",
    "build",
    "dist",
    "node_modules",
    "venv",
}


@dataclass(frozen=True)
class SyntaxIssue:
    path: Path
    line: int
    message: str


def _is_excluded(path: Path, root: Path) -> bool:
    return any(part in EXCLUDED_DIRS for part in path.relative_to(root).parts[:-1])


def find_python_files(root: Path) -> list[Path]:
    return sorted(
        path
        for path in root.rglob("*.py")
        if path.is_file() and not _is_excluded(path, root)
    )


def find_configs(root: Path) -> list[Path]:
    return [root / name for name in CONFIG_FILES if (root / name).is_file()]


def find_test_entries(files: Iterable[Path], root: Path) -> list[Path]:
    entries = []
    for path in files:
        relative = path.relative_to(root)
        if "tests" in relative.parts or path.name.startswith("test_") or path.name.endswith("_test.py"):
            entries.append(path)
    return entries


def validate_syntax(files: Iterable[Path], root: Path) -> list[SyntaxIssue]:
    issues = []
    for path in files:
        try:
            source = path.read_text(encoding="utf-8")
            ast.parse(source, filename=str(path.relative_to(root)))
        except UnicodeDecodeError as error:
            issues.append(SyntaxIssue(path, 0, f"UTF-8 decode error: {error.reason}"))
        except SyntaxError as error:
            issues.append(SyntaxIssue(path, error.lineno or 0, error.msg))
    return issues


def validate_project(root: Path) -> int:
    root = root.resolve()
    if not root.is_dir():
        print(f"[ERROR] Target directory does not exist: {root}")
        return 2

    files = find_python_files(root)
    configs = find_configs(root)
    tests = find_test_entries(files, root)
    issues = validate_syntax(files, root)

    print("=== vHarness Python Validation ===")
    print(f"[*] Python files: {len(files)}")
    print(f"[*] Project configs: {', '.join(path.name for path in configs) if configs else 'none'}")
    print(f"[*] Test entry points: {len(tests)}")

    if not files:
        print("[WARN] No Python files found; nothing to validate.")

    for issue in issues:
        relative = issue.path.relative_to(root)
        location = f"{relative}:{issue.line}" if issue.line else str(relative)
        print(f"[ERROR] {location}: {issue.message}")

    if issues:
        print(f"[FAIL] Python validation found {len(issues)} issue(s).")
        return 1

    print("[OK] Python validation passed.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate a Python vHarness project")
    parser.add_argument("--dir", default=".", help="project root to validate")
    args = parser.parse_args()
    return validate_project(Path(args.dir))


if __name__ == "__main__":
    raise SystemExit(main())
