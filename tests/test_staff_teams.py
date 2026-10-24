import unittest
from types import SimpleNamespace as NS
from staff_teams import team_index, team_fields


class StaffTeamTests(unittest.TestCase):
    def clubs(self):
        return {10: NS(uid=10, name='Parent', teams=[
            NS(team_id=100, slot=0, club_uid=10, is_affiliate=False),
            NS(team_id=101, slot=1, club_uid=10, is_affiliate=False),
            NS(team_id=900, slot=3, club_uid=20, is_affiliate=True)]),
            20: NS(uid=20, name='Club B', teams=[NS(team_id=900, slot=0, club_uid=20, is_affiliate=False)])}

    def person(self, tid=100, contract=True):
        return NS(team_id=tid, has_contract=contract, listed_club_uid=20)

    def test_own_contract_team_not_listed_club_or_department(self):
        teams=team_index(self.clubs())
        self.assertEqual(team_fields(self.person(),teams),dict(teamSlot=0, teamName=None, teamClubUid=10,teamClubName='Parent'))
        self.assertEqual(team_fields(self.person(101),teams)['teamSlot'],1)
        self.assertIsNone(team_fields(self.person(101),teams)['teamName'])

    def test_affiliate_uses_its_own_slot_not_parent_appended_slot(self):
        fields=team_fields(self.person(900),team_index(self.clubs()))
        self.assertEqual((fields['teamSlot'],fields['teamClubUid'],fields['teamClubName']),(0,20,'Club B'))

    def test_names_are_only_carried_when_readable(self):
        clubs=self.clubs();clubs[10].teams[1].name=' U20 '
        self.assertEqual(team_fields(self.person(101),team_index(clubs))['teamName'],'U20')

    def test_no_contract_missing_team_or_unlinked_team_is_unknown(self):
        teams=team_index(self.clubs())
        for person in [self.person(contract=False),self.person(None),self.person(999),self.person(True)]:
            self.assertEqual(team_fields(person,teams),{})

    def test_ambiguous_team_and_affiliate_without_owner_are_not_guessed(self):
        clubs=self.clubs();clubs[20].teams.append(NS(team_id=101,slot=0,club_uid=20,is_affiliate=False))
        self.assertEqual(team_fields(self.person(101),team_index(clubs)),{})
        del clubs[20]
        self.assertEqual(team_fields(self.person(900),team_index(clubs)),{})
