"""Read squad types from owner-linked FM26 team records, not team-list order.

The current format gates are explicit; age-code associations are calibrated
inside each save from at least three distinct clubs. See docs/TeamTypes.md.
"""
import re
import struct
from collections import defaultdict

_HEADER = re.compile(b'\xff\x00(.{2})([\x00\x01])([\x0e-\x19\x64\xff])\xff(.)'
                     b'(.{4})\x00{10}(.{4})(.{4})(.{4})([\x0a\x0b])', re.S)
MIN_REFERENCE_CLUBS = 3


def decode_team_types(raw, clubs, club_uids):
    owners = {}
    for club in clubs.values():
        for team in getattr(club, 'teams', ()):
            if getattr(team, 'is_affiliate', False):
                continue
            owner = getattr(team, 'club_uid', club.uid)
            if owner != club.uid:
                continue
            tid = getattr(team, 'team_id', None)
            if type(tid) is int:
                value = (club.uid, getattr(team, 'slot', None))
                owners[tid] = value if tid not in owners or owners[tid] == value else None
    candidates = defaultdict(list)
    for hit in _HEADER.finditer(raw):
        code, sub, age, flags, index, tid, uid, other, kind = hit.groups()
        tid, index, uid = (struct.unpack('<I', b)[0] for b in (tid, index, uid))
        owner = owners.get(tid)
        if owner is None or club_uids.get(index) != owner[0] or uid in (0, 0xffffffff):
            continue
        candidates[tid].append(dict(code=struct.unpack('<H', code)[0], sub=sub[0], age=age[0],
                                    owner=owner[0], slot=owner[1]))
    unique = {tid: values[0] for tid, values in candidates.items() if len(values) == 1}
    # A second independent reference links child rows to a readable first-team record.
    references = {r['owner'] for r in unique.values() if r['slot'] == 0 and r['code'] == 0 and r['sub'] == 0}
    observations = defaultdict(lambda: defaultdict(set))
    for row in unique.values():
        if row['owner'] in references and row['sub'] == 1 and row['code'] != 0 and 14 <= row['age'] <= 23:
            observations[row['code']][row['age']].add(row['owner'])
    learned = {code: next(iter(ages)) for code, ages in observations.items()
               if len(ages) == 1 and len(next(iter(ages.values()))) >= MIN_REFERENCE_CLUBS}
    result = {}
    for tid, row in unique.items():
        if row['owner'] not in references:
            continue
        name = None
        method = None
        age = learned.get(row['code'])
        if row['sub'] == 1 and age is not None and row['age'] in (age, 255):
            name, method = f'U{age}', 'cross_club_age'
        elif row['sub'] == 1 and row['age'] == 100 and row['slot'] is not None and row['slot'] > 0:
            # A separate senior subteam, not a claim that it is a B or C affiliate.
            name, method = 'Riserve', 'senior_subteam'
        if name is not None:
            result[tid] = dict(teamName=name, teamTypeCode=row['code'],
                               teamAgeLimit=row['age'], teamTypeMethod=method)
    return result


def read_team_types(store, clubs=None):
    cached = getattr(store, '_team_types', None)
    if cached is not None:
        return cached
    info = getattr(store.save, 'info', None)
    schemas = getattr(info, 'section_schemas', {})
    raw = getattr(store, 'raw', None)
    if schemas.get('game_db') != 4000 or raw is None:
        return {}
    try:
        index = store.save._context.club_index()
        if clubs is None:
            clubs = {club.uid: club for club in store.save.clubs()}
        result = decode_team_types(raw, clubs, index.uid_by_club_index)
    except (AttributeError, ValueError, LookupError, TypeError, struct.error):
        return {}  # Optional squad names must never break roster or staff reading.
    store._team_types = result
    return result
