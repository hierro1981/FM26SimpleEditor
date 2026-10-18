"""Regression tests for the playing/owner team view of confirmed loans."""
import copy
import datetime as dt
from types import SimpleNamespace as Record
import unittest

from editor import POSITION_ORDER, player_row, search_rows

CLOCK = dt.date(2027, 11, 7)


def player(uid, name, team="Zulu Playing", loan=False, owner=None):
    return Record(uid=uid, unique_id=1000 + uid, name=name,
                  club_name=team, team_club_name=None, team_club_uid=None,
                  loan_parent_club_name=owner, birth_date=dt.date(2005, 1, 1),
                  ability=Record(current=100, potential=150, potential_range_code=None),
                  team_id=20, club_uid=2, age=22, height_cm=180,
                  left_foot=10, right_foot=20, on_loan=loan,
                  positions=Record(**{key.lower(): 20 if key == "MC" else 1 for key in POSITION_ORDER}),
                  contract=Record(end=dt.date(2028, 6, 30)))


class LoanSearchTests(unittest.TestCase):
    def setUp(self):
        self.rows = [player_row(p, CLOCK) for p in (
            player(1, "Ada", loan=True, owner="Alpha Parent"),
            player(2, "Beto", team="Alpha Parent"),
            player(3, "Ciro"),
            player(4, "Dino", loan=None, owner="Unconfirmed Parent"),
            player(5, "Enea", loan=True),
        )]

    def search(self, **request):
        return search_rows(self.rows, request, CLOCK)

    def test_default_uses_playing_club(self):
        self.assertEqual(self.search(teamExact="Zulu Playing")["uids"], [1, 3, 4, 5])
        self.assertEqual(self.search(teamExact="Alpha Parent")["uids"], [2])

    def test_owner_view_moves_only_confirmed_loans(self):
        self.assertEqual(self.search(teamExact="Alpha Parent", loanTeam="owner")["uids"], [1, 2])
        self.assertEqual(self.search(teamExact="Zulu Playing", loanTeam="owner")["uids"], [3, 4])

    def test_unknown_loan_status_does_not_use_stale_parent(self):
        self.assertEqual(self.rows[3]["ownerTeam"], "Zulu Playing")
        self.assertEqual(self.search(teamExact="Unconfirmed Parent", loanTeam="owner")["total"], 0)
        self.assertEqual(self.search(loanTeam="owner", loan="unknown")["uids"], [4])

    def test_unknown_owner_is_not_replaced_by_borrower(self):
        self.assertEqual(self.rows[4]["ownerTeam"], "")
        self.assertEqual(self.search(text="Enea", loanTeam="owner")["uids"], [5])
        self.assertNotIn(5, self.search(teamExact="Zulu Playing", loanTeam="owner")["uids"])

    def test_substring_filter_and_text_search_use_selected_view(self):
        self.assertEqual(self.search(teamContains="pArEnT", loanTeam="owner")["uids"], [1, 2])
        self.assertEqual(self.search(text="pArEnT", loanTeam="owner")["uids"], [1, 2])
        self.assertEqual(self.search(text="parent")["uids"], [2])

    def test_loan_filter_combines_with_team_and_attribute_filters(self):
        self.assertEqual(self.search(loan="yes", teamExact="Alpha Parent", loanTeam="owner", ageMin=20, caMin=90)["uids"], [1])
        self.assertEqual(self.search(loan="no", teamExact="Alpha Parent", loanTeam="owner")["uids"], [2])
        self.assertEqual(self.search(loan="yes", teamExact="Zulu Playing")["uids"], [1, 5])

    def test_team_sort_uses_selected_view_and_unknown_owners_sort_last(self):
        self.assertEqual(self.search(sort="team")["uids"], [2, 1, 3, 4, 5])
        self.assertEqual(self.search(sort="team", loanTeam="owner")["uids"], [1, 2, 3, 4, 5])
        self.assertEqual(self.search(sort="team", loanTeam="owner", descending=True)["uids"], [3, 4, 1, 2, 5])

    def test_switching_view_preserves_catalog_and_squad_identifiers(self):
        original = copy.deepcopy(self.rows)
        for mode in ("owner", "playing", "owner"):
            self.search(loanTeam=mode, sort="team")
        self.assertEqual(self.rows, original)
        self.assertEqual(self.rows[0]["team"], "Zulu Playing")
        self.assertEqual(self.rows[0]["teamId"], 20)
        self.assertEqual(self.rows[0]["clubUid"], 2)

    def test_invalid_team_view_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "prestiti"):
            self.search(loanTeam="anything")

    def test_result_count_and_limit_apply_after_owner_filtering(self):
        self.rows = [dict(self.rows[0], uid=index, id=index) for index in range(3000)]
        result = self.search(teamExact="Alpha Parent", loanTeam="owner", sort="uid", descending=True)
        self.assertEqual(result["total"], 3000)
        self.assertEqual(len(result["uids"]), 2500)
        self.assertEqual(result["uids"][0], 2999)
        self.assertEqual(result["uids"][-1], 500)


if __name__ == "__main__":
    unittest.main()
