import datetime as dt
from types import SimpleNamespace as NS
from unittest.mock import patch
import unittest
from staff_recruitment import read_staff_recruitment
import test_staff_reader as fixture


class RecruitmentTests(unittest.TestCase):
    def store(self):
        helper = fixture.StaffReaderTests()
        people = [helper.person(1,10), helper.person(2,20), helper.person(3,99),
                  helper.person(3,98), helper.person(4,99,human=True), helper.person(5,99)]
        people[-1].has_contract=False
        for p in people: p.nation_id=1
        clubs=[NS(uid=i, name='Club '+str(i), nation_id=1, reputation=5000,
                  teams=[NS(club_uid=10),NS(club_uid=20)] if i==10 else []) for i in [10,20,99,98]]
        return NS(save=NS(info=NS(game_date=dt.date(2028,3,4)),
                         managed_clubs=lambda:[NS(club_uid=10)],clubs=lambda:clubs,staff=lambda:people), raw=b'')

    def test_recruitment_excludes_own_affiliate_human_and_preserves_multiple_employers(self):
        store=self.store()
        with patch('staff_recruitment.indexed_blocks',return_value=({},None)), patch('staff_jobs.StaffJobReader'):
            result=read_staff_recruitment(store,10)
        self.assertEqual(result['status'],'ready')
        self.assertEqual([p['uid'] for p in result['staff']],[3,3,5])
        self.assertEqual([p['employmentCount'] for p in result['staff']],[2,2,1])
        self.assertFalse(result['staff'][-1]['hasContract'])
        self.assertNotIn('unemployed',result['staff'][-1])
        self.assertEqual(result['ownExcluded'],2)
        self.assertEqual(result['clubReputation'],5000)

    def test_catalogue_cached_per_store_without_leaking_destination_mutations(self):
        store=self.store()
        with patch('staff_recruitment.indexed_blocks',return_value=({},None)) as scan, patch('staff_jobs.StaffJobReader'):
            first=read_staff_recruitment(store,10)
            first['staff'][0]['name']='changed'
            second=read_staff_recruitment(store,10)
        scan.assert_called_once()
        self.assertEqual(second['staff'][0]['name'],'Person 3')
        self.assertNotIn('employmentCount',store._recruitment_pool[0])

    def test_invalid_and_unmanaged_destination_never_scans(self):
        with patch('staff_recruitment.recruitment_pool',side_effect=AssertionError('unexpected scan')):
            for uid in [None, True, -1, '10', 2**32]:
                self.assertEqual(read_staff_recruitment(self.store(),uid)['status'],'unavailable')
            self.assertEqual(read_staff_recruitment(self.store(),99)['status'],'none')

    def test_reader_failure_is_explicit_not_partial_recommendations(self):
        with patch('staff_recruitment.indexed_blocks',side_effect=ValueError('unsupported')):
            result=read_staff_recruitment(self.store(),10)
        self.assertEqual(result['status'],'unavailable')
        self.assertEqual(result['error'],'unsupported')
        self.assertEqual(result['staff'],[])


if __name__ == '__main__': unittest.main()
