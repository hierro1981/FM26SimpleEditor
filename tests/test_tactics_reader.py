"""Synthetic tactics only: no uploaded save records or player data are fixtures."""
import dataclasses
import json
from pathlib import Path
import struct
from types import SimpleNamespace as NS
import unittest
from unittest.mock import patch

from fmsave.readers.tactics import _read_tactic_record
import tactics_reader as reader


class TacticsReaderTests(unittest.TestCase):
    def setUp(self):
        self.info = NS(build=reader.SUPPORTED_BUILD, section_schemas={'tactics_man': 26}, file_name='synthetic.fm')
        self.layout = reader.compatible_layout(self.info)
        self.codes = reader.read_codes()
        self.catalogue = json.loads((Path(__file__).parents[1]/'Data/tactical_roles.json').read_text())
        self.by_id = {r['id']:r for r in self.catalogue['roles']}

    def value(self, role):
        return next(r['roleCode'] for r in self.codes['roles'] if r['catalogueRoleId'] == role) | (1 << 22)

    def block(self, position, role, index=None, header=1):
        value = self.value(role)
        unit = bytes.fromhex('010202') + b'\x02' + b'\0'*7 + b'\xff' + b'\0'*8 + struct.pack('<I',value & 0xffffffff)
        return (self.layout.slot_tag + struct.pack('<I',position) + self.layout.slot_constant + struct.pack('<IQ',1,header)
                + unit + struct.pack('<I',value >> 32) + (bytes([index]) if index is not None else b''))

    def record(self, style_size=1):
        # Three central defenders deliberately test distinct left/centre/right slots.
        positions=[1,4,16|0x200000,16,16|0x100000,8,128,1024,2048,4096,16384]
        ip=['goalkeeper','full-back','centre-back','centre-back','centre-back','full-back','defensive-midfielder','central-midfielder','winger','winger','centre-forward']
        op=['goalkeeper','full-back','centre-back','centre-back','centre-back','full-back','defensive-midfielder','central-midfielder','winger','winger','centre-forward']
        name=b'Synthetic';style=b'x'*style_size
        b=self.layout.user_signature+struct.pack('<I',len(name))+name+b'\0'*12+b'\0'*19+struct.pack('<I',len(style))+style+b'\0'*4
        for i,mask in enumerate(positions):b+=self.block(mask,'ip_'+ip[i])+self.block(mask,'op_'+op[i],i)
        return b

    def tactic(self, raw=None):
        b=self.record() if raw is None else raw
        return _read_tactic_record(b,0,len(b),self.layout,'synthetic')

    def test_synthetic_record_reads_11_pairs_with_both_phases_and_central_sides(self):
        result=reader.decode_tactic(self.tactic(),0)
        self.assertEqual(len(result['slots']),11)
        self.assertEqual([result['slots'][i]['inPossession']['side'] for i in [2,3,4]],['right','centre','left'])
        self.assertEqual(result['slots'][10]['inPossession']['catalogueRoleId'],'ip_centre-forward')
        self.assertEqual(result['slots'][10]['outOfPossession']['catalogueRoleId'],'op_centre-forward')

    def test_all_79_metadata_roles_decode_even_with_a_different_base_header(self):
        for r in self.codes['roles']:
            role=self.by_id[r['catalogueRoleId']]
            position=role['positions'][0];bit=next(int(k) for k,v in self.codes['positionBits'].items() if v==position)
            raw=self.block(1<<bit,r['catalogueRoleId'],0)
            from fmsave.readers.tactics import _read_slot_block
            block=_read_slot_block(raw,0,len(raw),self.layout,with_index_byte=True)
            prefix='in_possession' if r['phase']=='ip' else 'out_of_possession'
            slot=NS(**{prefix+'_mask':block.mask,prefix+'_settings':block.settings},unknown={prefix+'_role_bits':1,prefix+'_trail_bits':block.trail_bits})
            actual=reader.decode_phase(slot,r['phase'])
            self.assertEqual(actual['catalogueRoleId'],role['id'])
            self.assertEqual(actual['internalRoleId'],r['internalId'])
            self.assertEqual(actual['duty'],'support')
            self.assertFalse(actual['controlledValidation'])

    def test_goalkeeper_and_forward_names_are_not_inferred_from_base_roles(self):
        for id in ['op_goalkeeper','op_sweeper-keeper','ip_channel-forward','ip_deep-lying-forward']:
            r=next(r for r in self.codes['roles'] if r['catalogueRoleId']==id)
            self.assertNotEqual(r['roleCode'],0)
        self.assertEqual(self.value('ip_channel-forward'),0x80400000)
        self.assertEqual(self.value('ip_deep-lying-forward'),0x400400)
        self.assertEqual(self.by_id['ip_channel-forward']['italian'],'Attaccante di movimento')
        self.assertEqual(self.by_id['ip_deep-lying-forward']['italian'],'Attaccante di manovra')

    def test_unknown_bits_legacy_position_missing_or_ambiguous_duty_stay_unknown(self):
        slot=self.tactic().slots[0]
        for mask in [2,12,0x400004,0x300010]:
            self.assertIsNone(reader.decode_phase(dataclasses.replace(slot,in_possession_mask=mask),'ip')['position'])
        self.assertEqual(reader.decode_phase(dataclasses.replace(slot,in_possession_settings=()),'ip')['status'],'unknown')
        last=slot.in_possession_settings[-1]
        for field in ['second_bits_low','second_bits_middle','second_bits_high']:
            settings=slot.in_possession_settings[:-1]+(dataclasses.replace(last,**{field:123}),)
            self.assertEqual(reader.decode_phase(dataclasses.replace(slot,in_possession_settings=settings),'ip')['status'],'unknown')
        for value in [1,1|(1<<21)|(1<<22),1|(1<<22)|(1<<30)]:
            altered=dataclasses.replace(slot,in_possession_settings=(dataclasses.replace(last,second_bits_high=value),))
            self.assertEqual(reader.decode_phase(altered,'ip')['status'],'unknown')

    def test_metadata_enum_uniqueness_and_catalogue_phase_links(self):
        roles=self.codes['roles']
        self.assertEqual(len(roles),79)
        self.assertEqual(len({(r['phase'],r['roleCode']) for r in roles}),79)
        self.assertEqual({r['catalogueRoleId'] for r in roles},set(self.by_id))
        duty=sum(int(k) for k in self.codes['dutyBits'])
        for r in roles:
            self.assertTrue(r['roleCode']>0 and r['roleCode']&(r['roleCode']-1)==0)
            self.assertEqual(r['roleCode']&duty,0)
            self.assertEqual(self.by_id[r['catalogueRoleId']]['phase'],r['phase'])

    def test_truncation_invalid_phase_and_permutation_are_rejected(self):
        raw=self.record()
        with self.assertRaises(ValueError):reader.decode_tactic(self.tactic(raw[:-10]),0)
        with self.assertRaises(ValueError):reader.decode_tactic(dataclasses.replace(self.tactic(),oop_index_permutation=False),0)
        with self.assertRaises(ValueError):reader.decode_phase(self.tactic().slots[0],'invalid')
        with self.assertRaises(ValueError):reader.compatible_layout(NS(build='26.4.0',section_schemas={'tactics_man':26}))
        with self.assertRaises(ValueError):reader.compatible_layout(NS(build=reader.SUPPORTED_BUILD,section_schemas={'tactics_man':99}))

    def section(self,count=3,foreign_copy=True):
        def block(team):
            b=struct.pack('<I',team)+b'\x09\x03'+struct.pack('<I',0)+b'\xff'*104+b'\x42\x00'+b'\0'*8+b'\x02'+b'\xff'*4
            b+=b'\x0b\x05\x02'+b'\0'*40+b'\x08'+b'\0'*32+b'\0'+struct.pack('<IH',0,count)
            return b+self.record()+self.record(style_size=275)+self.record()
        header=bytearray(19);header[10]=13;struct.pack_into('<II',header,11,77,2 if foreign_copy else 1)
        return bytes(header)+block(11)+(block(22) if foreign_copy else b'')

    def store(self,managed=True):
        team=lambda id,slot:NS(team_id=id,slot=slot,is_affiliate=False)
        club=NS(uid=1,teams=[team(11,0),team(22,1)])
        return NS(save=NS(info=self.info,_container_index=object(),managed_clubs=lambda:[NS(club_uid=1)] if managed else [],clubs=lambda:[club]))

    def test_bounded_reader_accepts_long_style_and_marker_but_excludes_youth_copies(self):
        humans=bytearray(14);struct.pack_into('<HI',humans,8,1,77)
        with patch.object(reader,'read_section',side_effect=lambda index,name:self.section() if name=='tactics_man' else bytes(humans)):
            result=reader.read_my_tactics(self.store(),1)
        self.assertEqual(result['status'],'ready',result)
        self.assertEqual(result['teamId'],11);self.assertEqual(len(result['tactics']),3)
        self.assertIsNone(result['selectedTacticIndex'])

    def test_wrong_human_and_count_fail_without_guessing(self):
        humans=bytearray(14);struct.pack_into('<HI',humans,8,1,77)
        for selector,count in [(77,4),(999,3)]:
            section=bytearray(self.section(count=count));struct.pack_into('<I',section,11,selector)
            with patch.object(reader,'read_section',side_effect=lambda index,name:bytes(section) if name=='tactics_man' else bytes(humans)):
                result=reader.read_my_tactics(self.store(),1)
            self.assertEqual(result['status'],'unavailable');self.assertEqual(result['tactics'],[])

    def test_no_managed_club_never_reads_tactics(self):
        with patch.object(reader,'read_section',side_effect=AssertionError('must not read')):
            self.assertEqual(reader.read_my_tactics(self.store(False),1)['status'],'none')
            self.assertEqual(reader.read_my_tactics(self.store(),2)['status'],'none')
            self.assertEqual(reader.read_my_tactics(self.store(),True)['status'],'unavailable')

if __name__=='__main__': unittest.main()
