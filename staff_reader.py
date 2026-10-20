"""Read-only staff at a verified human-managed club; no guessed job titles."""
from operation_progress import report

PERSONALITY = ('adaptability', 'ambition', 'loyalty', 'pressure', 'professionalism',
               'sportsmanship', 'temperament', 'controversy')
PREFERENCES = ('attacking', 'business', 'directness', 'interference', 'patience',
               'trigger_press', 'resources', 'buying_players', 'mind_games',
               'flexibility', 'hardness_of_training', 'squad_rotation', 'tempo', 'width')


def values(record, keys):
    return {key: getattr(record, key) for key in keys
            if record is not None and type(getattr(record, key, None)) is int}


def read_my_staff(store, club_uid, progress=None):
    report(progress, 0, 'Identificazione del club dello staff')
    try:
        if type(club_uid) is not int or not 0 <= club_uid <= 4294967295:
            raise ValueError('ID club non valido')
        managed = {club.club_uid for club in store.save.managed_clubs()}
        if club_uid not in managed:
            return dict(status='none', clubUid=club_uid, staff=[])
        clubs = {club.uid: club for club in store.save.clubs()}
        club = clubs.get(club_uid)
        if club is None:
            raise ValueError('Club gestito non presente nel database')
        associated = {club_uid} | {team.club_uid for team in club.teams}
        report(progress, 10, 'Lettura dei contratti e dei profili dello staff')
        decoded = list(store.save.staff())
        result = []
        for index, person in enumerate(decoded):
            # fmsave assigns staff paid by a parent but listed at an affiliate to
            # the parent club. Include both parent and controlled team clubs.
            if person.club_uid in associated:
                ability = person.ability
                result.append(dict(
                    uid=person.uid, id=person.unique_id, name=person.name or str(person.uid),
                    clubUid=person.club_uid, clubName=person.club_name,
                    listedClubUid=person.listed_club_uid, listedClubName=person.listed_club_name,
                    teamId=person.team_id, age=person.age,
                    birth=person.birth_date.isoformat() if person.birth_date else None,
                    isHumanManager=person.is_human_manager, hasContract=person.has_contract,
                    wage=person.wage,
                    contractStart=person.contract_start.isoformat() if person.contract_start else None,
                    contractEnd=person.contract_end.isoformat() if person.contract_end else None,
                    ca=ability.current if ability else None,
                    pa=ability.potential if ability else None,
                    personality=values(person.personality, PERSONALITY),
                    attributes=values(person.attributes, ('adaptability',)),
                    preferences=values(person.preferences, PREFERENCES)))
            if (index + 1) % 1000 == 0 or index + 1 == len(decoded):
                report(progress, 10 + 90 * (index + 1) / max(1, len(decoded)),
                       'Preparazione elenco dello staff', index + 1, len(decoded), 'persone')
        return dict(status='ready', clubUid=club_uid, staff=result)
    except Exception as exc:
        return dict(status='unavailable', clubUid=club_uid, staff=[], error=str(exc))
