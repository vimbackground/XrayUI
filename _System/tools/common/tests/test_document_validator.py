from __future__ import annotations

import importlib.util
import tempfile
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "document_validator.py"
SPEC = importlib.util.spec_from_file_location("document_validator", MODULE_PATH)
assert SPEC and SPEC.loader
DOCUMENT_VALIDATOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(DOCUMENT_VALIDATOR)


def document(doc_id: str, *, project: str = "vHarness") -> str:
    return f"""---
id: {doc_id}
title: Test
document_type: guide
status: active
version: 1.0.0
project: {project}
owner: maintainers
audience:
  - agent
scope: repository
created_at: 2026-09-06T00:00:00+08:00
updated_at: 2026-09-06T00:00:00+08:00
tags:
  - test
---

# Test
"""


def scoped_document(
    doc_id: str, scope: str, *, project: str = "vHarness"
) -> str:
    return document(doc_id, project=project).replace(
        "scope: repository", f"scope: {scope}"
    )


class DocumentValidatorTests(unittest.TestCase):
    def test_repository_uses_standard_registry_constraints(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            standards = root / "_System/standards"
            schemas = standards / "schemas"
            schemas.mkdir(parents=True)
            (standards / "naming_rules.json").write_text(
                '{"product_name":"CustomHarness"}', encoding="utf-8"
            )
            (schemas / "document_metadata.schema.json").write_text(
                '{"properties":{"status":{"enum":["ready"]}}}', encoding="utf-8"
            )
            project, statuses = DOCUMENT_VALIDATOR.standard_constraints(root)
            self.assertEqual(project, "CustomHarness")
            self.assertEqual(statuses, {"ready"})

    def test_valid_repository_passes(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "README.md").write_text(document("DOC-ROOT"), encoding="utf-8")
            self.assertEqual(DOCUMENT_VALIDATOR.validate_repository(root), 0)

    def test_missing_frontmatter_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "README.md").write_text("# Missing\n", encoding="utf-8")
            self.assertEqual(DOCUMENT_VALIDATOR.validate_repository(root), 1)

    def test_wrong_project_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "README.md").write_text(
                document("DOC-WRONG", project="legacy"), encoding="utf-8"
            )
            self.assertEqual(DOCUMENT_VALIDATOR.validate_repository(root), 1)

    def test_missing_directory_readme_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "README.md").write_text(document("DOC-ROOT"), encoding="utf-8")
            (root / "_Dev").mkdir()
            self.assertEqual(DOCUMENT_VALIDATOR.validate_repository(root), 1)

    def test_host_profile_ignores_project_content_markdown(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "Agent.md").write_text(
                document("DOC-HOST-AGENT", project="XrayUI"), encoding="utf-8"
            )
            for name in ("_System", "_Dev"):
                managed = root / name
                managed.mkdir()
                (managed / "README.md").write_text(
                    scoped_document(f"DOC-{name}", name, project="vHarness"),
                    encoding="utf-8",
                )
            project_content = root / "Services"
            project_content.mkdir()
            (project_content / "README.md").write_text(
                "# Business documentation without framework metadata\n",
                encoding="utf-8",
            )
            self.assertEqual(
                DOCUMENT_VALIDATOR.validate_repository(root, "host"), 0
            )

    def test_host_profile_requires_managed_root_readme(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "Agent.md").write_text(
                document("DOC-HOST-AGENT", project="XrayUI"), encoding="utf-8"
            )
            (root / "_Dev").mkdir()
            self.assertEqual(
                DOCUMENT_VALIDATOR.validate_repository(root, "host"), 1
            )


if __name__ == "__main__":
    unittest.main()
