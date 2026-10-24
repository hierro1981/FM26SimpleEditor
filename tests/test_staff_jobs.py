import datetime as dt
import struct
import unittest
from types import SimpleNamespace as NS

from fmsave._errors import CorruptSaveError
from fmsave._layouts.fm26_26_3_2 import STAFF
from staff_jobs import StaffJobReader, job_fields, job_preferences


class StaffJobTests(unittest.TestCase):
    def fixture(self, code=34):
        raw = bytearray(500)
        struct.pack_into('<IIIBB', raw, 0, 7, 12345, 12345, 1, 0)
        tag = 200
        raw[tag:tag + 4] = bytes.fromhex('01006c07')
        struct.pack_into('<I', raw, tag + 5, 8)  # person index + 1, not UID.
        raw[tag + 21] = code
        start, end = dt.date(2025, 7, 1), dt.date(2028, 6, 30)
        record = (10, 'Club', 100, 123, start, end, True, 4, 0, (), 1,
                  {'e37': 0, 'e38': 0, 'e39': 0})
        person = NS(is_human_manager=False, has_contract=True, team_id=100,
                    wage=123, contract_start=start, contract_end=end,
                    unknown={'contract_e36': 4, 'contract_e37': 0,
                             'contract_e38': 0, 'contract_e39': 0, 'contract_type': 1})
        decoder = NS(_find_chain_records=lambda *args: ([tag], [record]))
        reader = StaffJobReader.__new__(StaffJobReader)
        reader.layout = STAFF
        reader.clock = dt.date(2026, 10, 21)
        reader.decoder = decoder
        reader.store = NS(raw=raw, records=NS(pindexes=[8], record_offsets=[400],
                                             layout=NS(pindex_offset=0)))
        return reader, person, record, tag

    def test_current_native_codes_override_obsolete_enum_names(self):
        self.assertEqual(job_fields(22)['jobKey'], 'set_piece_coach')
        self.assertEqual(job_fields(48)['jobKey'], 'sports_scientist')
        self.assertEqual(job_fields(88)['jobKey'], 'technical_director')
        self.assertEqual(job_fields(86)['jobKey'], 'head_player_development')
        self.assertEqual(job_fields(3)['jobKey'], 'coach')
        self.assertTrue(job_fields(3)['isPlayerJob'])
        self.assertTrue(job_fields(60)['jobFamilyOnly'])
        for value in [None, True, -1, 0, 1, 18, 144, 254, 256, '34']:
            self.assertEqual(job_fields(value), {}, value)

    def test_contract_job_read_is_read_only_and_ignores_r4(self):
        reader, person, _, _ = self.fixture()
        person.unknown['r4'] = 99
        original = bytes(reader.store.raw)
        self.assertEqual(reader.read(person, 0)['jobKey'], 'goalkeeping_coach')
        self.assertEqual(bytes(reader.store.raw), original)

    def test_other_club_old_future_wrong_pay_signature_and_untailed_do_not_match(self):
        reader, person, record, tag = self.fixture()
        for index, value in [(2, 101), (3, 999), (4, dt.date(2030, 1, 1)),
                             (5, None), (6, False), (7, 99)]:
            other = list(record); other[index] = value
            reader.decoder._find_chain_records = lambda *args, r=other: ([tag], [r])
            self.assertEqual(reader.read(person, 0), {}, index)

    def test_matching_contract_is_selected_instead_of_first_or_other_team(self):
        reader, person, record, tag = self.fixture()
        other = list(record); other[2] = 999
        reader.decoder._find_chain_records = lambda *args: ([tag, tag], [other, record])
        self.assertEqual(reader.read(person, 0)['jobCode'], 34)

    def test_duplicates_wrong_selector_and_boundary_overrun_are_rejected(self):
        reader, person, record, tag = self.fixture()
        reader.decoder._find_chain_records = lambda *args: ([tag, tag], [record, record])
        self.assertEqual(reader.read(person, 0), {})
        reader.decoder._find_chain_records = lambda *args: ([tag], [record])
        struct.pack_into('<I', reader.store.raw, tag + 5, 12346)
        self.assertEqual(reader.read(person, 0), {})
        struct.pack_into('<I', reader.store.raw, tag + 5, 8)
        reader.store.records.record_offsets = [tag + 22]
        self.assertEqual(reader.read(person, 0), {})

    def test_missing_human_and_unreadable_contracts_leave_job_unknown(self):
        reader, person, _, _ = self.fixture()
        def fail(*args): raise CorruptSaveError('Truncated contract')
        reader.decoder._find_chain_records = fail
        self.assertEqual(reader.read(person, 0), {})
        person.is_human_manager = True
        self.assertEqual(reader.read(person, 0), {})
        person.is_human_manager = False; person.has_contract = False
        self.assertEqual(reader.read(person, 0), {})

    def test_future_and_expired_employment_are_not_presented_as_current_jobs(self):
        reader, person, _, _ = self.fixture()
        person.contract_start = dt.date(2030, 1, 1)
        self.assertEqual(reader.read(person, 0), {})
        person.contract_start = dt.date(2020, 1, 1)
        person.contract_end = dt.date(2025, 1, 1)
        self.assertEqual(reader.read(person, 0), {})

    def test_saved_job_affinities_follow_variable_list_and_do_not_replace_current_job(self):
        for count, ratings in [(0,[11,13,20,1,1]+[1]*10),(2,[12,15,15,10,9,1,1,1,4,20,10,4,1,1,1])]:
            reader,person,_,tag=self.fixture(64)
            raw=reader.store.raw;raw[tag+29]=count;at=tag+35+9*count
            raw[at-5:at]=bytes(4)+b'\1';raw[at:at+15]=bytes(ratings)
            struct.pack_into('<HH',raw,at+19,171,2026);raw[at+24:at+28]=bytes.fromhex('01006c07')
            result=reader.read(person,0)
            self.assertEqual(result['jobKey'],'head_youth_development')
            self.assertEqual(result['jobPreferences'],dict(zip(('manager','assistant_manager','coach','physio','scout'),ratings[:5])))
            original=bytes(raw)
            for offset,value in [(at-1,0),(at,0),(at,21),(at+15,1),(at+24,0),(tag+29,255)]:
                raw[:]=original;raw[offset]=value
                self.assertNotIn('jobPreferences',reader.read(person,0))
                self.assertEqual(reader.read(person,0)['jobKey'],'head_youth_development')
            raw[:]=original
            self.assertEqual(job_preferences(raw,tag,at+27),{})


if __name__ == '__main__':
    unittest.main()
