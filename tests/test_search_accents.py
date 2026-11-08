import unittest
from editor import search_fold

class AccentChecks(unittest.TestCase):
    def test_accent_and_case_are_ignored(self):
        self.assertIn(search_fold("HERNANDEZ"), search_fold("Daniel Hernández"))
        self.assertIn(search_fold("Andre"), search_fold("André"))
        self.assertEqual(search_fold("e\u0301"), search_fold("é"))
        self.assertNotIn(search_fold("moreno"), search_fold("Hernández"))
