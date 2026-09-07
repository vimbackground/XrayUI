from __future__ import annotations

import json
import unittest
from pathlib import Path


class StandardsTests(unittest.TestCase):
    def test_registered_terms_and_paths_are_unique(self) -> None:
        root = Path(__file__).resolve().parents[3]
        standards = root / "standards"
        vocabulary = json.loads((standards / "vocabulary.json").read_text(encoding="utf-8"))
        naming = json.loads((standards / "naming_rules.json").read_text(encoding="utf-8"))
        term_ids = [term["id"] for term in vocabulary["terms"]]
        paths = list(naming["canonical_paths"].values())
        self.assertEqual(len(term_ids), len(set(term_ids)))
        self.assertEqual(len(paths), len(set(paths)))
        self.assertEqual(naming["product_name"], "vHarness")
        self.assertIn("Tasks", naming["non_framework_business_directories"])

    def test_fusion_rules_reference_standard_ids_without_duplicating_terms(self) -> None:
        root = Path(__file__).resolve().parents[1]
        rules = json.loads((root / "fusion_rules.json").read_text(encoding="utf-8"))
        self.assertIn("standards", rules)
        self.assertNotIn("terms", rules)


if __name__ == "__main__":
    unittest.main()
