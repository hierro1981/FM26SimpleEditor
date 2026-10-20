"""Read-only roster of the human-managed club, using validated team IDs."""
from operation_progress import report


def read_my_squad(store, progress=None):
    # fmsave 0.5.10 resolves the first human manager only. Never guess a club.
    report(progress, 0, 'Identificazione della squadra allenata')
    try:
        managed = list(store.save.managed_clubs())
        if not managed:
            return dict(status='none', clubs=[], players=[], firstHumanOnly=True)
        clubs = {club.uid: club for club in store.save.clubs()}
        result_clubs = []
        wanted = set()
        for manager in managed:
            club = clubs.get(manager.club_uid)
            if club is None:
                raise ValueError('Club gestito non presente nel database')
            teams = list(club.teams)
            first = [team.team_id for team in teams if team.slot == 0 and not team.is_affiliate]
            if len(first) != 1:
                raise ValueError('Prima squadra non identificata univocamente')
            ids = sorted({team.team_id for team in teams})
            choices = []
            for team in sorted(teams, key=lambda team: (team.slot, team.team_id)):
                owner = clubs.get(getattr(team, 'club_uid', club.uid))
                name = getattr(team, 'name', None)
                if not name and owner is not None and owner.uid != club.uid:
                    name = owner.name
                choices.append(dict(teamId=team.team_id, slot=team.slot, name=name))
            wanted.update(ids)
            result_clubs.append(dict(clubUid=club.uid, clubName=club.name,
                                     managerName=manager.manager_name,
                                     firstTeamId=first[0], teamIds=ids, teams=choices))
        # Registration in an affiliated B team is included when that team belongs
        # to the managed club's own team list. Outgoing loans have different team IDs.
        members = [row for row in store.rows if row['teamId'] in wanted]
        report(progress, 15, 'Lettura della rosa e degli attributi', 0, len(members), 'giocatori')
        players = []
        layout = store.layout
        for index, row in enumerate(members):
            player = store.player(row['uid'])
            start, _ = store.offsets(player)
            names = iter(layout.attribute_field_order)
            attributes = []
            for at in range(layout.attribute_count):
                key = 'left_foot' if at == layout.left_foot_index else 'right_foot' if at == layout.right_foot_index else next(names)
                attributes.append(dict(index=at, key=key, value=store.raw[start + layout.attributes_offset + at]))
            players.append(dict(player=row, attributes=attributes,
                                positions=[dict(key=key, value=value) for key, value in row['positions'].items()]))
            report(progress, 15 + 85 * (index + 1) / max(1, len(members)),
                   'Lettura della rosa e degli attributi', index + 1, len(members), 'giocatori')
        return dict(status='ready', clubs=result_clubs, players=players, firstHumanOnly=True)
    except Exception as exc:
        # Failure of this optional reader must not break player search/editing.
        return dict(status='unavailable', clubs=[], players=[], firstHumanOnly=True, error=str(exc))
