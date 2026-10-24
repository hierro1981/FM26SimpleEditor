import datetime as dt
import io
import json
import unittest
from types import SimpleNamespace as NS
from staff_reader import read_my_staff
from operation_progress import ProgressReporter


class StaffReaderTests(unittest.TestCase):
    def person(self, uid, club=10, human=False):
        return NS(uid=uid, unique_id=uid+100, name='Person '+str(uid), club_uid=club,
                  club_name='Club '+str(club), listed_club_uid=20, listed_club_name='B team',
                  team_id=100, age=45, birth_date=dt.date(1980,1,2),
                  is_human_manager=human, has_contract=True, wage=0,
                  contract_start=dt.date(2025,7,1), contract_end=None,
                  ability=None if human else NS(current=130,potential=None),
                  personality=NS(adaptability=10,professionalism=15),
                  attributes=NS(adaptability=10),
                  preferences=None if human else NS(attacking=18,tempo=16),
                  unknown={'r4':7})

    def store(self):
        people=[self.person(1),self.person(2,20),self.person(3,99),self.person(4,human=True)]
        club=NS(uid=10,teams=[NS(club_uid=10),NS(club_uid=20)])
        return NS(save=NS(info=NS(game_date=dt.date(2028,3,28)),managed_clubs=lambda:[NS(club_uid=10)],clubs=lambda:[club],staff=lambda:people))

    def test_parent_and_affiliate_only_not_other_clubs(self):
        result=read_my_staff(self.store(),10)
        self.assertEqual(result['status'],'ready')
        self.assertEqual([p['uid'] for p in result['staff']],[1,2,4])
        self.assertEqual(result['staff'][0]['listedClubName'],'B team')

    def test_missing_values_and_zero_pay_are_preserved_without_role_guesses(self):
        person=read_my_staff(self.store(),10)['staff'][0]
        self.assertEqual(person['wage'],0)
        self.assertIsNone(person['pa'])
        self.assertIsNone(person['contractEnd'])
        self.assertEqual(person['birth'],'1980-01-02')
        self.assertEqual(person['preferences'],{'attacking':18,'tempo':16})
        self.assertEqual(person['personality'],{'adaptability':10,'professionalism':15})
        self.assertNotIn('role',person)
        self.assertNotIn('unknown',person)

    def test_human_manager_has_no_invented_ability_or_preferences(self):
        person=read_my_staff(self.store(),10)['staff'][-1]
        self.assertTrue(person['isHumanManager'])
        self.assertIsNone(person['ca'])
        self.assertIsNone(person['pa'])
        self.assertEqual(person['preferences'],{})
        self.assertEqual(person['attributes'],{'adaptability':10})

    def test_nonmanaged_club_does_not_trigger_staff_reader(self):
        store=self.store();store.save.staff=lambda:self.fail('Unexpected staff scan')
        self.assertEqual(read_my_staff(store,99)['status'],'none')
        store.save.managed_clubs=lambda:[]
        self.assertEqual(read_my_staff(store,10)['staff'],[])

    def test_errors_are_optional_and_invalid_ids_are_not_accepted(self):
        store=self.store()
        def fail(): raise ValueError('Unreadable staff')
        store.save.staff=fail
        result=read_my_staff(store,10)
        self.assertEqual(result['status'],'unavailable')
        self.assertEqual(result['staff'],[])
        self.assertEqual(result['error'],'Unreadable staff')
        for invalid in [None,True,-1,'10',2**32]:
            self.assertEqual(read_my_staff(store,invalid)['status'],'unavailable')

    def test_empty_staff_is_a_success_and_progress_is_monotonic(self):
        store=self.store();store.save.staff=lambda:[]
        self.assertEqual(read_my_staff(store,10),dict(status='ready',clubUid=10,staff=[],gameDate='2028-03-28'))
        output=io.StringIO();read_my_staff(self.store(),10,ProgressReporter(output))
        events=[json.loads(line) for line in output.getvalue().splitlines()]
        self.assertEqual([e['percent'] for e in events],sorted(e['percent'] for e in events))
        self.assertEqual((events[-1]['done'],events[-1]['total']),(4,4))


if __name__=='__main__': unittest.main()
