import contextlib
import io
import json
import struct
import unittest
from types import SimpleNamespace as NS
from unittest.mock import patch
import editor
from operation_progress import ProgressReporter


class InspectProgressTests(unittest.TestCase):
    def store(self):
        store = editor.PlayerStore.__new__(editor.PlayerStore)
        store.layout = NS(attribute_field_order=['passing','technique'], attribute_count=4,
                          left_foot_index=2,right_foot_index=3,attributes_offset=0,
                          position_codes=['MC','GK'],ratings_offset=4,potential_ability_offset=6)
        store.raw = bytes([50,60,10,20,20,1])+struct.pack('<h',150)
        store.player = lambda uid: NS(uid=uid,team_id=1)
        store.offsets = lambda player: (0,8)
        store.birth_offset = lambda player: (0,'')
        store.save = NS(info=NS(game_date=None),league_tables=lambda: [])
        store.league_cache = {}
        store.league_tables_cache = None
        store.rows = []
        store.close = lambda: None
        store.summary = lambda: {}
        return store

    def test_real_phases_counters_and_compatible_result(self):
        store=self.store(); output=io.StringIO()
        with patch.object(editor,'player_row',lambda p,date: {'uid':p.uid}):
            expected=store.inspect(7)
            store.league_cache.clear(); store.league_tables_cache=None
            actual=store.inspect(7,ProgressReporter(output))
        events=[json.loads(line) for line in output.getvalue().splitlines()]
        self.assertEqual(expected,actual)
        self.assertEqual([e['percent'] for e in events],sorted(e['percent'] for e in events))
        self.assertTrue(any(e['phase'].startswith('Verifica dei dati anagrafici') for e in events))
        self.assertTrue(any(e['phase']=='Lettura delle classifiche del salvataggio' for e in events))
        self.assertTrue(any(e['done']==4 and e['total']==4 and e['unit']=='attributi' for e in events))
        self.assertLess(events[-1]['percent'],100)  # final success belongs to JSON response

    def test_cached_league_reports_cache_phase(self):
        store=self.store();store.league_cache[1]={'mean':100,'count':5,'label':'League'}
        output=io.StringIO()
        with patch.object(editor,'player_row',lambda p,date: {}): store.inspect(7,ProgressReporter(output))
        self.assertIn('Campionato recuperato dalla cache',output.getvalue())
        self.assertIsNone(store.league_tables_cache)

    def protocol(self,store):
        output=io.StringIO()
        with patch.object(editor,'PlayerStore',return_value=store),patch.object(editor,'player_row',lambda p,date:{}),patch.object(editor.sys,'stdin',io.StringIO('{"command":"open","source":"save.fm"}\n{"command":"inspect","uid":7}\n')),contextlib.redirect_stdout(output):
            editor.serve()
        return [json.loads(line) for line in output.getvalue().splitlines()]

    def test_protocol_progress_before_final_success(self):
        events=self.protocol(self.store())
        self.assertTrue(events[-1]['ok'])
        self.assertEqual(events[-2]['percent'],100)
        self.assertEqual(events[-2]['phase'],'Dati giocatore pronti')
        self.assertEqual(events[-1]['data']['attributes'][0]['value'],50)

    def test_failure_never_reports_inspect_complete(self):
        store=self.store()
        def fail(player): raise ValueError('invalid record')
        store.birth_offset=fail
        events=self.protocol(store)
        self.assertFalse(events[-1]['ok'])
        self.assertFalse(any(e.get('phase')=='Dati giocatore pronti' for e in events))
        self.assertIn('invalid record',events[-1]['error'])


if __name__ == '__main__': unittest.main()
