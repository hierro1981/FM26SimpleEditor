"""Read-only staff at a verified human-managed club; no guessed job titles."""
from operation_progress import report
from staff_teams import team_index, team_fields
import struct

PERSONALITY = ('adaptability', 'ambition', 'loyalty', 'pressure', 'professionalism',
               'sportsmanship', 'temperament', 'controversy')
PREFERENCES = ('attacking', 'business', 'directness', 'interference', 'patience',
               'trigger_press', 'resources', 'buying_players', 'mind_games',
               'flexibility', 'hardness_of_training', 'squad_rotation', 'tempo', 'width')

# These preferences (not coaching ability) match both supplied independent
# profile screenshots. Unresolved slots 2, 4, 12 and 13 are not relabelled.
EXTRA_PREFERENCES = {5: 'free_roles', 7: 'marking', 8: 'offside',
                     16: 'relaxation', 17: 'use_playmaker',
                     18: 'use_substitutions', 19: 'intensity', 20: 'tactical_flair'}


def reputation_at_block(raw, block, layout):
    if block is None or (layout.ability_base_offset, layout.entry_bytes, layout.entry_count_offset) != (21, 7, 13):
        return {}
    header, ability = block[:2]
    if header < 0 or header + layout.entry_count_offset >= len(raw):
        return {}
    expected = header + 21 + 7 * raw[header + layout.entry_count_offset]
    if ability != expected or ability - 6 < header or ability > len(raw):
        return {}
    ratings = struct.unpack_from('<3H', raw, ability - 6)
    return dict(zip(('home', 'current', 'world'), ratings)) if all(1 <= n <= 10000 for n in ratings) else {}


def preferences_at_block(raw, block, layout):
    if block is None or layout.preferences_offset != 14 or layout.preference_count != 26:
        return {}
    at = block[1] + layout.preferences_offset
    if at < 0 or at + 26 > len(raw):
        return {}
    return {key: raw[at + index] for index, key in EXTRA_PREFERENCES.items()
            if 1 <= raw[at + index] <= 20}

# Indices of the native generic non-player attribute enum, not the separate
# job-dependent getter enum. See docs/StaffAttributes.md for the evidence.
NATIVE_ATTRIBUTES = {
    4: 'discipline', 12: 'working_with_youngsters', 13: 'determination',
    26: 'coaching', 27: 'coaching_goalkeeping', 28: 'judging_ability',
    29: 'judging_potential', 30: 'man_management', 31: 'motivating', 32: 'physiotherapy',
    33: 'tactical_knowledge', 34: 'coaching_attacking', 35: 'coaching_defending',
    36: 'coaching_fitness', 37: 'coaching_possession', 38: 'coaching_technical',
    39: 'coaching_tactical', 41: 'goalkeeping_handling',
    42: 'goalkeeping_distribution', 44: 'analysing_data', 47: 'sports_science',
    48: 'eccentricity', 49: 'negotiating', 50: 'judging_staff_ability', 51: 'coaching_set_pieces',
}


def native_staff_block(raw, person, layout):
    """Locate a uniquely identified, validated staff block; never modify it.

    UID hits alone do not establish a record. Identity words, kind, complete
    ability/preferences and all 26 further bytes must pass the pinned reader.
    An ambiguous match yields no additional attributes.
    """
    from fmsave.readers.staff import _header_reader, _header_values, _read_ability_block

    if person.is_human_manager or person.ability is None:
        return None
    if (layout.preferences_offset, layout.preference_count, layout.block_40_offset,
            layout.block_40_count, layout.sentinel_value) != (14, 26, 40, 26, 12):
        return None
    if type(person.uid) is not int or not 0 < person.uid < 0xFFFFFFFF:
        return None
    needle = struct.pack('<I', person.uid)
    result = None
    start = 0
    reader = _header_reader(layout)
    while (hit := raw.find(needle, start)) >= 0:
        start = hit + 1
        header = hit - layout.uid_offset
        if _header_values(raw, header, reader, layout) != (person.uid, layout.staff_kind):
            continue
        block, preferences_ok, _, further_ok = _read_ability_block(raw, header, layout)
        if (block is None or not preferences_ok or not further_ok
                or block.ability != person.ability or block.preferences != person.preferences):
            continue
        if any(person.unknown.get(key) != value for key, value in block.unknown.items()):
            continue
        if result is not None:
            return None
        ability_at = header + layout.ability_base_offset + layout.entry_bytes * raw[header + layout.entry_count_offset]
        result = (header, ability_at)
    return result


def attributes_at_block(raw, block, layout):
    if block is None:
        return {}
    # The serializer copies native bytes +0x10..+0x43 after marker 12.
    # Generic indices >= 26 use the native (raw + 2) / 5 conversion.
    attributes_at = block[1] + layout.preferences_offset
    result = {}
    for index, key in NATIVE_ATTRIBUTES.items():
        value = raw[attributes_at + index]
        scaled = index >= 26 or index == 13
        # The first block mixes preferences and abilities. In particular, zero
        # or an unknown sentinel must not become an invented 1/20 rating.
        if 1 <= value <= (100 if scaled else 20):
            result[key] = max(1, min(20, (value + 2) // 5)) if scaled else value
    return result


def native_staff_attributes(raw, person, layout):
    return attributes_at_block(raw, native_staff_block(raw, person, layout), layout)


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
        teams = team_index(clubs)
        decoded = list(store.save.staff())
        native_layout = None
        job_reader = None
        if getattr(store, 'raw', None) is not None:
            try:
                from fmsave.readers.staff import find_staff_layouts
                native_layout = find_staff_layouts(store.save.info.section_schemas, store.save.info.build).staff
            except (AttributeError, ValueError, LookupError):
                pass  # Optional extension: keep the ordinary staff data available.
            try:
                from staff_jobs import StaffJobReader
                job_reader = StaffJobReader(store)
            except (AttributeError, ValueError, LookupError):
                pass
        result = []
        for index, person in enumerate(decoded):
            # fmsave assigns staff paid by a parent but listed at an affiliate to
            # the parent club. Include both parent and controlled team clubs.
            if person.club_uid in associated:
                ability = person.ability
                attributes = values(person.attributes, ('adaptability',))
                preferences = values(person.preferences, PREFERENCES)
                reputation = {}
                job = {}
                if native_layout is not None:
                    block = native_staff_block(store.raw, person, native_layout)
                    attributes.update(attributes_at_block(store.raw, block, native_layout))
                    reputation = reputation_at_block(store.raw, block, native_layout)
                    preferences.update(preferences_at_block(store.raw, block, native_layout))
                    if block is not None and job_reader is not None:
                        job = job_reader.read(person, block[0])
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
                    attributes=attributes,
                    preferences=preferences, reputation=reputation, **team_fields(person, teams), **job))
            if person.club_uid in associated or (index + 1) % 1000 == 0 or index + 1 == len(decoded):
                report(progress, 10 + 90 * (index + 1) / max(1, len(decoded)),
                       'Preparazione elenco dello staff', index + 1, len(decoded), 'persone')
        return dict(status='ready', clubUid=club_uid, staff=result,
                    gameDate=store.save.info.game_date.isoformat())
    except Exception as exc:
        return dict(status='unavailable', clubUid=club_uid, staff=[], error=str(exc))
