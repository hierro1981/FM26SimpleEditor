import struct
import unittest
from types import SimpleNamespace as N
from recovered_staff import retirement_dates, recover_staff, possible_board_reference


def date(day, year=2028):
    return struct.pack('<HH', day, year)


def record(selector, retirement=191):
    return bytes.fromhex('80000002') + struct.pack('<I', selector) + b'\xff'*4 + date(148) + date(retirement) + date(146)


class RecoveredStaffChecks(unittest.TestCase):
    def test_retirement_is_linked_to_selector_not_uid_or_incidental_date(self):
        raw = record(18) + record(581) + date(191)
        self.assertEqual(retirement_dates(raw, {18}), {18:'2028-07-09'})
        self.assertEqual(retirement_dates(raw, {580}), {})
        self.assertEqual(retirement_dates(raw[:-1], {18}), {18:'2028-07-09'})

    def test_invalid_ambiguous_or_unverified_retirement_records_stay_unknown(self):
        self.assertEqual(retirement_dates(record(18)+record(18,192), {18}), {})
        self.assertEqual(retirement_dates(record(18,100), {18}), {})
        self.assertEqual(retirement_dates(record(18)[:-1], {18}), {})
        self.assertEqual(retirement_dates(b'\x81'+record(18)[1:], {18}), {})


    def test_possible_board_reference_requires_signature_and_team_club_match(self):
        index=N(team_to_club={853:(1140,0)},affiliate_team_to_club={},club_by_uid={1140:object()})
        record=struct.pack('<II',1140,853)+bytes.fromhex('00000000ffffffffffffffff0000000001006c07')+bytes([4])
        self.assertEqual(possible_board_reference(bytes(70)+record, 0, index),4)
        self.assertIsNone(possible_board_reference(bytes(70)+record, 0, N(team_to_club={853:(999,0)},affiliate_team_to_club={},club_by_uid={1140:object()})))
        self.assertIsNone(possible_board_reference(bytes(70)+record[:-1]+bytes([16]),0,index))
        self.assertIsNone(possible_board_reference(bytes(70)+record[:-1],0,index))

    def test_discovery_is_gated_to_verified_build(self):
        for game,build in [('FM26','26.3.2'),('FM27','27.0.1+future')]:
            store=N(save=N(info=N(game=game,build=build,section_schemas={})))
            self.assertEqual(recover_staff(store,set()),[])
