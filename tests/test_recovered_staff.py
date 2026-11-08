import struct
import unittest
from types import SimpleNamespace as N
from recovered_staff import retirement_dates, recover_staff


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

    def test_discovery_is_gated_to_verified_build(self):
        for game,build in [('FM26','26.3.2'),('FM27','27.0.1+future')]:
            store=N(save=N(info=N(game=game,build=build,section_schemas={})))
            self.assertEqual(recover_staff(store,set()),[])
