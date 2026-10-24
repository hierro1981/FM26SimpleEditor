"""Read-only recruitment pool. Missing contracts never mean unemployment."""
from collections import Counter
import warnings
from operation_progress import report
from staff_teams import team_index, team_fields
from staff_reader import (PERSONALITY, PREFERENCES, values, attributes_at_block,
                          reputation_at_block, preferences_at_block)


def indexed_blocks(store):
    """Discover staff headers in one bounded pass, not one database scan per UID."""
    from fmsave.readers.staff import (find_staff_layouts, discover_contracts,
        read_staff_lists, locate_listed_header, _header_reader, _header_values,
        _read_ability_block)
    from fmsave.readers.contracts import build_contract_decoder
    from fmsave.readers.managed import first_human_selector
    layouts = find_staff_layouts(store.save.info.section_schemas, store.save.info.build)
    raw, records = store.raw, store.records
    clubs = store.save._context.club_index()
    decoder = build_contract_decoder(layouts.contracts, clubs, store.save.info.game_date, str(store.path))
    with store.save._context.section('humans') as humans:
        human = first_human_selector(humans, layouts.humans, str(store.path))
    headers = dict(discover_contracts(raw, records, human, decoder, layouts, clubs).header_by_person)
    listed, _, _ = read_staff_lists(raw, clubs, records, layouts.staff)
    for person_id in sorted({pid for _, pid in listed} - headers.keys()):
        header = locate_listed_header(raw, person_id, records, layouts.staff)
        if type(header) is int:
            headers[person_id] = header
    result = {}
    layout = layouts.staff
    reader = _header_reader(layout)
    if (layout.preferences_offset, layout.preference_count, layout.block_40_offset,
            layout.block_40_count, layout.sentinel_value) != (14, 26, 40, 26, 12):
        raise ValueError('Unsupported staff attribute layout')
    for header in headers.values():
        identity = _header_values(raw, header, reader, layout)
        if identity is None or identity[1] != layout.staff_kind:
            continue
        block, preferences_ok, _, further_ok = _read_ability_block(raw, header, layout)
        if block is None or not preferences_ok or not further_ok:
            continue
        ability_at = header + layout.ability_base_offset + layout.entry_bytes * raw[header + layout.entry_count_offset]
        result.setdefault(identity[0], []).append((header, ability_at, block))
    return result, layout


def recruitment_pool(store, progress=None):
    cached = getattr(store, '_recruitment_pool', None)
    if cached is not None:
        return cached
    report(progress, 8, 'Lettura dello staff dei club')
    with warnings.catch_warnings(record=True) as caught:
        warnings.simplefilter('always')
        people = list(store.save.staff())
    store._recruitment_warnings = [str(w.message) for w in caught]
    report(progress, 25, 'Indicizzazione degli attributi dello staff')
    blocks, layout = indexed_blocks(store)
    from staff_jobs import StaffJobReader
    jobs = StaffJobReader(store)
    clubs = {club.uid: club for club in store.save.clubs()}
    teams = team_index(clubs)
    result = []
    for i, person in enumerate(people):
        if person.is_human_manager:
            continue
        matches = [b for b in blocks.get(person.uid, [])
                   if b[2].ability == person.ability and b[2].preferences == person.preferences
                   and all(person.unknown.get(k) == v for k, v in b[2].unknown.items())]
        block = matches[0] if len(matches) == 1 else None
        attributes = values(person.attributes, ('adaptability',))
        if block is not None:
            attributes.update(attributes_at_block(store.raw, block[:2], layout))
        club = clubs.get(person.club_uid)
        ability = person.ability
        result.append(dict(uid=person.uid, id=person.unique_id, name=person.name or str(person.uid),
            clubUid=person.club_uid, clubName=person.club_name,
            listedClubUid=person.listed_club_uid, listedClubName=person.listed_club_name,
            teamId=person.team_id, age=person.age, nationId=person.nation_id,
            clubNationId=getattr(club, 'nation_id', None),
            clubReputation=getattr(club, 'reputation', None),
            birth=person.birth_date.isoformat() if person.birth_date else None,
            isHumanManager=False, hasContract=person.has_contract, wage=person.wage,
            contractStart=person.contract_start.isoformat() if person.contract_start else None,
            contractEnd=person.contract_end.isoformat() if person.contract_end else None,
            ca=ability.current if ability else None, pa=ability.potential if ability else None,
            personality=values(person.personality, PERSONALITY), attributes=attributes,
            preferences={**values(person.preferences, PREFERENCES), **preferences_at_block(store.raw, block, layout)},
            reputation=reputation_at_block(store.raw, block, layout),
            **team_fields(person, teams),
            **(jobs.read(person, block[0]) if block is not None else {})))
        if (i+1) % 500 == 0 or i+1 == len(people):
            report(progress, 40+50*(i+1)/max(1,len(people)), 'Preparazione candidati staff', i+1, len(people), 'persone')
    # Keep all employment rows: interest uses the strongest current employer,
    # and own-club exclusion considers every row of a multiply-employed person.
    store._recruitment_pool = result
    return result


def read_staff_recruitment(store, club_uid, progress=None):
    try:
        if type(club_uid) is not int or not 0 <= club_uid <= 4294967295:
            raise ValueError('ID club non valido')
        if club_uid not in {c.club_uid for c in store.save.managed_clubs()}:
            return dict(status='none', clubUid=club_uid, staff=[])
        clubs = {c.uid: c for c in store.save.clubs()}
        club = clubs.get(club_uid)
        if club is None:
            raise ValueError('Club gestito non presente nel database')
        associated = {club_uid} | {t.club_uid for t in club.teams}
        pool = recruitment_pool(store, progress)
        own_uids = {p['uid'] for p in pool if p['clubUid'] in associated}
        candidates = [dict(p) for p in pool if p['uid'] not in own_uids]
        counts = Counter(p['uid'] for p in candidates)
        for p in candidates:
            p['employmentCount'] = counts[p['uid']]
        return dict(status='ready', clubUid=club_uid, clubName=club.name,
                    clubReputation=club.reputation, clubNationId=club.nation_id,
                    gameDate=store.save.info.game_date.isoformat(), staff=candidates,
                    readerWarnings=getattr(store, '_recruitment_warnings', []),
                    poolPeople=len({p['uid'] for p in pool}), ownExcluded=len(own_uids))
    except Exception as exc:
        return dict(status='unavailable', clubUid=club_uid, staff=[], error=str(exc))
