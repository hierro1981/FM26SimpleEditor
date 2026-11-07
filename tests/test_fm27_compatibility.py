"""Synthetic edition gates only; private save data are not shipped as fixtures."""
import struct
import unittest
from types import SimpleNamespace as N
from unittest.mock import patch

from save_compatibility import capabilities, paired_fm27, require_writable, FM27_PAIRED_BUILD
from history_reader import find_history, embedded_history
from tactics_reader import compatible_layout


def info(build=FM27_PAIRED_BUILD, schema=4000):
    return N(game='FM27', build=build, section_schemas={'game_db': schema, 'tactics_man': 26})


def history_bytes(marker):
    record=bytes([marker])+bytes([50])*47+bytes(21)
    return bytes(123)+struct.pack('<I',2)+record*2+bytes(8)


class FM27CompatibilityChecks(unittest.TestCase):
    def test_exact_paired_build_has_explicit_experimental_support(self):
        value=capabilities(info())
        self.assertEqual(value,dict(game='FM27',build=FM27_PAIRED_BUILD,experimental=True,canSave=True))
        require_writable(info())

    def test_unknown_build_or_changed_schema_never_authorises_writes(self):
        for value in (info('27.0.1+9999999'),info(schema=4001)):
            self.assertFalse(capabilities(value)['canSave'])
            with self.assertRaises(ValueError):require_writable(value)

    def test_fm26_write_support_is_preserved(self):
        value=N(game='FM26',build='26.3.2+2329565',section_schemas={'game_db':4000})
        self.assertTrue(capabilities(value)['canSave'])
        self.assertFalse(capabilities(value)['experimental'])
        self.assertFalse(paired_fm27(value))

    def test_tactics_use_the_actual_fm27_layout(self):
        layout=compatible_layout(info())
        self.assertEqual(layout.slot_tag,bytes.fromhex('440002'))
        self.assertEqual(layout.in_possession_gap_bytes,12)
        with self.assertRaises(ValueError):compatible_layout(info('27.0.1+9999999'))
        value=info();value.section_schemas['tactics_man']=27
        with self.assertRaises(ValueError):compatible_layout(value)

    def test_history_marker_is_edition_specific(self):
        for marker in (1,2):
            points,_=find_history(history_bytes(marker),record_marker=marker)
            self.assertEqual(len(points),2)
            self.assertEqual(points[0]['attributes']['technique'],50)
            self.assertIsNone(points[0]['ca'])
            self.assertEqual(find_history(history_bytes(marker),record_marker=3-marker)[0],[])

    def test_unknown_fm27_history_stays_unavailable(self):
        store=N(save=N(info=info('27.0.1+9999999')))
        self.assertEqual(embedded_history(store,N())[0],[])

    def test_fm27_marker_selected_from_paired_build_not_only_schema(self):
        store=N(save=N(info=info()),raw=history_bytes(2),path='synthetic.fm',offsets=lambda p:(0,len(history_bytes(2))))
        points,_=embedded_history(store,N(name='Synthetic'))
        self.assertEqual(len(points),2)
        self.assertEqual(points[0]['source'],'synthetic.fm')

    def test_invalid_marker_rejected(self):
        with self.assertRaises(ValueError):find_history(history_bytes(1),record_marker=0)


class FM27StaffScreenChecks(unittest.TestCase):
    def test_screen_verified_titles_keep_recruitment_categories(self):
        from staff_jobs import fm27_job_fields, job_fields
        self.assertEqual(fm27_job_fields(62)['jobKey'],'analyst')
        self.assertEqual(fm27_job_fields(62)['jobTitleKey'],'head_performance_analyst')
        self.assertFalse(fm27_job_fields(62)['jobFamilyOnly'])
        self.assertEqual(fm27_job_fields(60)['jobTitleKey'],'fm27_performance_analyst')
        self.assertEqual(fm27_job_fields(2)['jobKey'],'coach')
        self.assertTrue(job_fields(62)['jobFamilyOnly'])  # FM26 remains category-only.
        self.assertNotIn('jobTitleKey',job_fields(62))

    def test_board_and_medical_roles_are_not_invented_employment(self):
        from staff_jobs import fm27_job_fields, StaffJobReader
        reader=StaffJobReader.__new__(StaffJobReader);reader.fm27=True
        for code,key in [(4,'chairperson'),(6,'director'),(38,'doctor')]:
            person=N(is_human_manager=False,has_contract=False,job=N(raw=code))
            fields=reader.read(person,None)
            self.assertEqual(fields['jobKey'],key)
            self.assertFalse(fields['jobFamilyOnly'])
            self.assertNotIn('wage',fields)
        self.assertEqual(reader.read(N(is_human_manager=False,has_contract=False,job=None),None),{})
        self.assertEqual(fm27_job_fields(254),{})

    def test_no_contract_role_uses_only_an_explicit_catalogued_team(self):
        from staff_teams import team_fields
        context={'teamSlot':0,'teamName':None,'teamClubUid':1,'teamClubName':'Synthetic'}
        person=N(has_contract=False,team_id=100,job=N(raw=6))
        self.assertEqual(team_fields(person,{100:context},allow_role_team=True),dict(context,teamAssignmentSource='role'))
        self.assertEqual(team_fields(person,{100:context}),{})
        self.assertEqual(team_fields(person,{100:None},allow_role_team=True),{})
        self.assertEqual(team_fields(person,{},allow_role_team=True),{})
        person.job=None
        self.assertEqual(team_fields(person,{100:context},allow_role_team=True),{})
