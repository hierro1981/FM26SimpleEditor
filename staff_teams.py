"""Contract-team context; slot order is not an age-group mapping."""


def team_index(clubs):
    """Prefer each team's own club record, never its parent's appended affiliate slot."""
    result = {}
    for club in clubs.values():
        for team in getattr(club, 'teams', ()):
            tid, slot = getattr(team, 'team_id', None), getattr(team, 'slot', None)
            owner = getattr(team, 'club_uid', club.uid)
            if (getattr(team, 'is_affiliate', False) or owner != club.uid
                    or type(tid) is not int or not 0 <= tid <= 4294967295
                    or type(slot) is not int or not 0 <= slot <= 255):
                continue
            name = getattr(team, 'name', None)
            entry = dict(teamSlot=slot, teamName=name.strip() if isinstance(name, str) and name.strip() else None,
                         teamClubUid=club.uid, teamClubName=getattr(club, 'name', None))
            if tid in result and result[tid] != entry:
                result[tid] = None  # Contradictory ownership/slot is not an assignment.
            else:
                result[tid] = entry
    return result


def team_fields(person, teams):
    tid = getattr(person, 'team_id', None)
    if not person.has_contract or type(tid) is not int:
        return {}
    # No inference from job title, listed club, department lists or nationality.
    if tid in teams:
        return teams[tid] or {}  # Keep explicit conflicting mappings unknown.
    slot = getattr(person, 'team_slot', None)
    name = getattr(person, 'team_name', None)
    fallback = {}
    if type(slot) is int and 0 <= slot <= 255:
        fallback['teamSlot'] = slot
    if isinstance(name, str) and name.strip():
        fallback['teamName'] = name.strip()
    return fallback
