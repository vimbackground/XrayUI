#!/usr/bin/env python3
r"""
vHarness 基础架构验证器 (vharness_validator.py)

功能:
1. 验证 vHarness Framework Infrastructure 的核心路径。
2. 扫描常见文本文件，拦截宿主绝对路径并把读取失败视为门禁失败。
3. 按 repository、template 或 assets 档案验证核心结构；不规定宿主业务目录布局。
"""

import argparse
import re
from pathlib import Path


EXCLUDED_DIRS = {
    ".git",
    ".idea",
    ".vs",
    ".vscode",
    "_wip",
    "__pycache__",
    "artifacts",
    "bin",
    "build",
    "node_modules",
    "obj",
    "target",
    "TestResults",
}
PROFILE_REQUIREMENTS = {
    "host": {"Agent.md", "_Dev", "_System"},
    "repository": {"Agent.md", "_Dev", "_Dist", "_System"},
    "template": {
        "Agent.md",
        "_Dev",
        "_System",
        "_User",
        "Transfer",
        "Working",
    },
    "assets": {"Agent.md", "_Dev", "_System"},
}
TEXT_PATTERNS = ("*.md", "*.py", "*.json", "*.sh", "*.toml", "*.yml", "*.yaml", "*.ps1")


def detect_profile(target_dir: Path) -> str:
    if (target_dir / "_Dist").is_dir():
        return "repository"
    if (target_dir / "_User").is_dir() and (target_dir / "Working").is_dir():
        return "template"
    if (target_dir / "Agent.md").is_file() and (target_dir / "_System").is_dir():
        return "host"
    return "assets"


def validate_target_structure(target_dir: Path, profile: str) -> bool:
    print(f"[*] Validating {profile} core structure...")
    missing = sorted(name for name in PROFILE_REQUIREMENTS[profile] if not (target_dir / name).exists())
    if missing:
        print(f"  [ERROR] Missing core paths: {', '.join(missing)}")
        return False
    print("  [OK] Core structure is complete.")
    return True


def validate_host_content_boundary(target_dir: Path) -> bool:
    print("[*] Preserving host Project Content boundaries...")
    print("  [OK] Host business directories are not constrained by vHarness.")
    return True

def validate_no_absolute_paths(target_dir: Path) -> bool:
    print("[*] Scanning for hardcoded absolute paths and unreadable text...")
    # 简单的正则匹配 Windows / Unix 绝对路径的危险写法
    # The negative lookbehind prevents the trailing ``s:/`` in ``https://``
    # from being interpreted as a Windows drive path.
    windows_drive = r'(?<![A-Za-z0-9])[a-zA-Z]:[\\/](?![<])[^\s`"\']+'
    windows_home = r'[a-zA-Z]:\\' + r'[Uu]sers\\[^\s\\]+'
    macos_home = r'/' + r'Users/[^\s/]+'
    linux_home = r'/' + r'home/[^\s/]+'
    abs_path_pattern = re.compile(
        f'(?:{windows_drive})|(?:{windows_home})|(?:{macos_home})|(?:{linux_home})'
    )

    all_passed = True
    seen: set[Path] = set()
    for ext in TEXT_PATTERNS:
        for file in target_dir.rglob(ext):
            if file in seen:
                continue
            seen.add(file)
            relative = file.relative_to(target_dir)
            if any(part in EXCLUDED_DIRS for part in relative.parts):
                continue
            try:
                content = file.read_text(encoding="utf-8")
                matches = abs_path_pattern.findall(content)
                if matches:
                    print(f"  [ERROR] {relative}: hardcoded paths: {matches[:3]}")
                    all_passed = False
            except (OSError, UnicodeError) as error:
                print(f"  [ERROR] {relative}: cannot read as UTF-8: {error}")
                all_passed = False

    if all_passed:
        print("  [OK] No hardcoded paths or unreadable text found.")
    return all_passed


def main() -> int:
    parser = argparse.ArgumentParser(description="vHarness architecture validator")
    parser.add_argument("--dir", default=".", help="需要验证的工作区根目录")
    parser.add_argument("--profile", choices=sorted(PROFILE_REQUIREMENTS), help="validation profile")
    args = parser.parse_args()

    target_dir = Path(args.dir).resolve()
    if not target_dir.is_dir():
        print(f"[ERROR] Target directory does not exist: {target_dir}")
        return 2

    profile = args.profile or detect_profile(target_dir)
    print("=== vHarness Architecture Validation ===")

    v0 = validate_target_structure(target_dir, profile)
    v1 = validate_host_content_boundary(target_dir)
    v2 = validate_no_absolute_paths(target_dir)

    if not (v0 and v1 and v2):
        print("\n[!] Validation Failed! Please fix the architectural violations.")
        return 1

    print("\n[OK] Validation Passed! Architecture is clean.")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
