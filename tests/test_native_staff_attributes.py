import struct
import unittest
from dataclasses import replace
from types import SimpleNamespace as NS

from fmsave._layouts.fm26_26_3_2 import STAFF
from fmsave.readers.staff import _read_ability_block
from staff_reader import NATIVE_ATTRIBUTES, native_staff_attributes


class NativeStaffAttributeTests(unittest.TestCase):
    def fixture(self):
        raw = bytearray(100)
        struct.pack_into('<IIIBB', raw, 0, 7, 12345, 12345, 1, 0)
        struct.pack_into('<HhB', raw, 21, 100, 150, 4)
        raw[26:34] = bytes([3] * 8)
        raw[34] = 12
        raw[35:61] = bytes([10] * 26)
        raw[61:87] = bytes([50] * 26)
        block, prefs_ok, _, further_ok = _read_ability_block(raw, 0, STAFF)
        self.assertTrue(prefs_ok and further_ok)
        person = NS(uid=12345, is_human_manager=False, ability=block.ability,
                    preferences=block.preferences, unknown=block.unknown)
        return raw, person

    def test_twenty_named_attributes_and_native_rounding(self):
        raw, person = self.fixture()
        # Boundary values distinguish nearest-integer conversion from flooring.
        for value, expected in [(1, 1), (7, 1), (8, 2), (52, 10), (53, 11), (98, 20), (100, 20)]:
            raw[61 + 6] = value  # Generic attribute 32 = physiotherapy.
            result = native_staff_attributes(raw, person, STAFF)
            self.assertEqual(len(result), 20)
            self.assertEqual(result['physiotherapy'], expected)
            self.assertEqual(result['coaching_goalkeeping'], 10)
        self.assertEqual(len(NATIVE_ATTRIBUTES), 20)

    def test_identity_kind_ability_marker_preferences_and_range_are_required(self):
        for offset, value in [(8, 0), (12, 2), (21, 99), (34, 11), (35, 9), (61, 0), (61, 101)]:
            raw, person = self.fixture()
            raw[offset] = value
            self.assertEqual(native_staff_attributes(raw, person, STAFF), {}, offset)

    def test_unknown_signature_and_duplicate_matches_are_rejected(self):
        raw, person = self.fixture()
        person.unknown = dict(person.unknown, r4=99)
        self.assertEqual(native_staff_attributes(raw, person, STAFF), {})
        raw, person = self.fixture()
        self.assertEqual(native_staff_attributes(raw + raw, person, STAFF), {})

    def test_human_missing_ability_incompatible_layout_and_truncation(self):
        raw, person = self.fixture()
        person.is_human_manager = True
        self.assertEqual(native_staff_attributes(raw, person, STAFF), {})
        person.is_human_manager = False
        self.assertEqual(native_staff_attributes(raw[:80], person, STAFF), {})
        self.assertEqual(native_staff_attributes(raw, person, replace(STAFF, block_40_count=25)), {})
        person.ability = None
        self.assertEqual(native_staff_attributes(raw, person, STAFF), {})

    def test_entry_list_length_and_nonzero_header_offset(self):
        raw, person = self.fixture()
        # Two seven-byte entries precede the same ability block.
        shifted = raw[:21] + bytes(14) + raw[21:]
        shifted[13] = 2
        person.unknown = dict(person.unknown, entry_count=2)
        before = bytes(37)
        original = bytes(before + shifted)
        self.assertEqual(native_staff_attributes(original, person, STAFF)['judging_potential'], 10)
        self.assertEqual(original, bytes(before + shifted))


if __name__ == '__main__':
    unittest.main()
