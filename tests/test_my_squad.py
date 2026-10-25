import io
import json
import unittest
from types import SimpleNamespace as NS
from unittest.mock import patch
from my_squad import read_my_squad
from operation_progress import ProgressReporter


class MySquadTests(unittest.TestCase):
    def store(self, managed=True):
        manager = NS(club_uid=10, manager_name='Human')
        teams = [NS(team_id=100, slot=0, is_affiliate=False),
                 NS(team_id=101, slot=1, is_affiliate=False),
                 NS(team_id=900, slot=2, is_affiliate=True)]
        club = NS(uid=10, name='Club', teams=teams)
        rows = [dict(uid=1, teamId=100, positions={'MC':20}),
                dict(uid=2, teamId=101, positions={'MC':15}),
                dict(uid=3, teamId=900, positions={'MC':12}),
                # Outgoing loan: owner club must not override current squad.
                dict(uid=4, teamId=500, ownerTeam='Club', positions={'MC':20}),
                dict(uid=5, teamId=None, team='Club', positions={'MC':20})]
        layout = NS(attribute_field_order=['passing','technique'], attribute_count=4,
                    left_foot_index=2, right_foot_index=3, attributes_offset=0)
        return NS(save=NS(managed_clubs=lambda:[manager] if managed else [], clubs=lambda:[club]),
                  rows=rows, layout=layout, raw=bytes([50,75,10,20]),
                  player=lambda uid:NS(uid=uid), offsets=lambda p:(0,4))

    def test_exact_team_ids_include_affiliate_but_exclude_loans_and_name_matches(self):
        result=read_my_squad(self.store())
        self.assertEqual(result['status'],'ready')
        self.assertEqual(result['clubs'][0],dict(clubUid=10,clubName='Club',managerName='Human',firstTeamId=100,teamIds=[100,101,900],teams=[dict(teamId=100,slot=0,name=None),dict(teamId=101,slot=1,name=None),dict(teamId=900,slot=2,name=None)]))
        self.assertEqual([p['player']['uid'] for p in result['players']],[1,2,3])
        self.assertTrue(result['firstHumanOnly'])

    def test_outgoing_loans_are_loaded_by_confirmed_owner_id_only(self):
        store = self.store()
        store.rows[3].update(onLoan=True, ownerClubUid=10)
        store.rows.append(dict(uid=6, teamId=500, onLoan=True, ownerClubUid=99, ownerTeam='Club', positions={'MC':20}))
        result = read_my_squad(store)
        self.assertEqual(result['status'], 'ready')
        self.assertEqual([p['player']['uid'] for p in result['players']], [1,2,3,4])

    def test_squad_names_only_use_readable_names_not_slot_guesses(self):
        store=self.store();club=store.save.clubs()[0]
        club.teams[2].club_uid=50
        store.save.clubs=lambda:[club,NS(uid=50,name='Club B')]
        teams=read_my_squad(store)['clubs'][0]['teams']
        self.assertEqual(teams[2]['name'],'Club B')
        self.assertIsNone(teams[1]['name'])  # An unknown internal slot is not invented U20.
        self.assertEqual(teams[1]['teamId'],101)

    def test_decoded_type_names_reach_selector_without_guessing_slot(self):
        with patch('my_squad.read_team_types',return_value={101:dict(teamName='U20',teamTypeMethod='cross_club_age')}):
            team=read_my_squad(self.store())['clubs'][0]['teams'][1]
        self.assertEqual((team['teamId'],team['name'],team['typeMethod']),(101,'U20','cross_club_age'))

    def test_attributes_and_positions_keep_raw_values_without_loading_edit_details(self):
        result=read_my_squad(self.store())
        player=result['players'][0]
        self.assertEqual([(a['key'],a['value']) for a in player['attributes']], [('passing',50),('technique',75),('left_foot',10),('right_foot',20)])
        self.assertEqual(player['positions'],[dict(key='MC',value=20)])

    def test_unemployed_manager_does_not_fall_back_to_arbitrary_club(self):
        store=self.store(managed=False)
        store.save.clubs=lambda: self.fail('No club search should be needed')
        self.assertEqual(read_my_squad(store)['status'],'none')

    def test_reader_failure_is_an_explicit_optional_feature_error(self):
        store=self.store()
        def fail(): raise ValueError('Unsupported layout')
        store.save.managed_clubs=fail
        result=read_my_squad(store)
        self.assertEqual(result['status'],'unavailable')
        self.assertEqual(result['players'],[])
        self.assertEqual(result['error'],'Unsupported layout')

    def test_ambiguous_first_team_is_not_guessed(self):
        store=self.store(); club=store.save.clubs()[0]
        club.teams.append(NS(team_id=102,slot=0,is_affiliate=False))
        store.save.clubs=lambda:[club]
        self.assertEqual(read_my_squad(store)['status'],'unavailable')

    def test_progress_is_monotonic_and_counts_squad_members(self):
        out=io.StringIO();read_my_squad(self.store(),ProgressReporter(out))
        events=[json.loads(line) for line in out.getvalue().splitlines()]
        self.assertEqual([e['percent'] for e in events],sorted(e['percent'] for e in events))
        self.assertEqual((events[-1]['done'],events[-1]['total']),(3,3))


if __name__=='__main__': unittest.main()
