"""Checks for the in-progress French and German resource files."""
import re
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / 'Resources'

def resources(path):
    root = ET.parse(path).getroot()
    values = [(item.attrib['name'], item.findtext('value') or '') for item in root.findall('data')]
    if len(values) != len(dict(values)):
        raise ValueError(f'duplicate resource key in {path}')
    return dict(values)

class TranslationResources(unittest.TestCase):
    def test_partial_resources_keep_keys_and_placeholders(self):
        baseline = resources(ROOT / 'Strings.en.resx')
        for language in ('fr', 'de'):
            with self.subTest(language=language):
                translation = resources(ROOT / f'Strings.{language}.resx')
                self.assertTrue(translation)
                for key, text in translation.items():
                    self.assertIn(key, baseline)
                    self.assertTrue(text.strip(), key)
                    pattern = r'\{\d+(?:[^}]*)\}'
                    self.assertEqual(sorted(re.findall(pattern, text)), sorted(re.findall(pattern, baseline[key])), key)
