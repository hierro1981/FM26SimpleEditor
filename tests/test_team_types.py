import struct
import unittest
from types import SimpleNamespace as N
from team_types import decode_team_types, read_team_types


def header(index, team, code=0, sub=0, age=100, flag=0, identity=10000):
    return b'\xff\x00'+struct.pack('<HBB',code,sub,age)+b'\xff'+bytes([flag])+struct.pack('<I',index)+bytes(10)+struct.pack('<III',team,identity,123)+b'\x0a'


class TeamTypeTests(unittest.TestCase):
    def fixture(self, code=18):
        clubs={};mapping={};raw=b''
        for index in [1,2,3]:
            uid=index+100;mapping[index]=uid
            clubs[uid]=N(uid=uid,teams=[N(team_id=index*10,slot=0,is_affiliate=False),N(team_id=index*10+1,slot=1,is_affiliate=False)])
            raw+=header(index,index*10)+header(index,index*10+1,code,1,20,flag=index*16)
        return raw,clubs,mapping

    def test_quorum_and_club_identity_not_slot_order(self):
        raw,clubs,mapping=self.fixture()
        self.assertEqual(decode_team_types(raw,clubs,mapping)[11]['teamName'],'U20')
        clubs[101].teams[1].slot=3
        self.assertEqual(decode_team_types(raw,clubs,mapping)[11]['teamName'],'U20')
        mapping[1]=999
        self.assertEqual(decode_team_types(raw,clubs,mapping),{})

    def test_changed_numeric_code_is_calibrated_from_age_and_multiple_clubs(self):
        raw,clubs,mapping=self.fixture(code=149)
        result=decode_team_types(raw,clubs,mapping)
        self.assertEqual((result[11]['teamName'],result[11]['teamTypeCode']),('U20',149))

    def test_one_reference_or_conflicting_age_never_establishes_a_mapping(self):
        raw,clubs,mapping=self.fixture()
        self.assertEqual(decode_team_types(raw,{101:clubs[101]},mapping),{})
        raw=raw.replace(header(3,31,18,1,20,flag=48),header(3,31,18,1,18,flag=48))
        self.assertEqual(decode_team_types(raw,clubs,mapping),{})

    def test_duplicate_header_and_missing_first_team_reference_are_rejected(self):
        raw,clubs,mapping=self.fixture()
        self.assertNotIn(11,decode_team_types(raw+header(1,11,18,1,20),clubs,mapping))
        raw=raw.replace(header(1,10),header(1,10,3,1,100))
        self.assertNotIn(11,decode_team_types(raw,clubs,mapping))

    def test_unclassified_subteam_and_missing_age_sentinel(self):
        raw,clubs,mapping=self.fixture()
        clubs[101].teams.extend([N(team_id=12,slot=2,is_affiliate=False),N(team_id=13,slot=3,is_affiliate=False)])
        result=decode_team_types(raw+header(1,12,22,1,100)+header(1,13,18,1,255),clubs,mapping)
        self.assertEqual(result[12]['teamTypeMethod'],'unclassified_subteam')
        self.assertEqual(result[13]['teamName'],'U20')

    def test_unsupported_schema_is_optional_and_cache_is_per_store(self):
        store=N(save=N(info=N(section_schemas={'game_db':9999})),raw=b'')
        self.assertEqual(read_team_types(store),{})
        raw,clubs,mapping=self.fixture();store=N(save=N(info=N(section_schemas={'game_db':4000}),_context=N(club_index=lambda:N(uid_by_club_index=mapping))),raw=raw)
        result=read_team_types(store,clubs)
        self.assertEqual(result[11]['teamName'],'U20')
        store.raw=b''
        self.assertIs(result,read_team_types(store,clubs))
