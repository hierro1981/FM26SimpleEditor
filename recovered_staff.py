"""Conservative FM27 discovery of profiles missing from club employment rows.

No-club-record is evidence of reader coverage, not proof of unemployment.
Only the paired build is scanned. No jobs, national roles or wages are guessed.
"""
import re
import struct
from bisect import bisect_right
from collections import Counter
from save_compatibility import paired_fm27


def retirement_dates(raw, selectors):
    """Verified schema-2 record variant; other variants remain undecoded."""
    from fmsave._scan import decode_date
    dates = {}
    pattern = re.compile(rb'\x80\x00\x00\x02(.{4})\xff\xff\xff\xff(.{12})', re.DOTALL)
    for match in pattern.finditer(raw):
        selector = struct.unpack('<I', match[1])[0]
        if selector not in selectors:
            continue
        announced, retirement, auxiliary = [decode_date(match[2], at) for at in (0, 4, 8)]
        if announced and retirement and auxiliary and auxiliary <= announced <= retirement:
            dates.setdefault(selector, set()).add(retirement.isoformat())
    return {selector: next(iter(values)) for selector, values in dates.items() if len(values) == 1}


def recover_staff(store, employed_uids, progress=None):
    if not paired_fm27(store.save.info):
        return []
    import fmsave.readers.staff as native
    from fmsave.readers.persons import build_person_block_decoder
    from staff_reader import values, PERSONALITY, attributes_at_block, reputation_at_block, preferences_at_block
    from operation_progress import report
    layouts = native.find_staff_layouts(store.save.info.section_schemas, store.save.info.build)
    layout = layouts.staff
    raw = store.raw
    reader = native._header_reader(layout)
    hits = []
    report(progress, 26, 'Ricerca dei profili staff senza club rilevato')
    for match in re.finditer(re.escape(bytes([layout.staff_kind])), raw):
        header = match.start() - layout.kind_offset
        identity = native._header_values(raw, header, reader, layout)
        if identity is None:
            continue
        block, slots, codes, further = native._read_ability_block(raw, header, layout)
        if block is not None and slots and codes and further:
            hits.append((header, identity[0], block))
    counts = Counter(uid for _, uid, _ in hits)
    boundaries = sorted({h for h, _, _ in hits} | {
        at + store.records.layout.pindex_offset for at in store.records.record_offsets})
    decoder = build_person_block_decoder(layouts.person_blocks, store.save._context.name_pools(),
        store.save._context.club_index(), store.save.info.game_date, str(store.path))
    result = []
    selectors = {}
    for header, uid, block in hits:
        if uid in employed_uids or counts[uid] != 1:
            continue
        position = bisect_right(boundaries, header)
        following = boundaries[position] if position < len(boundaries) else None
        person_id = struct.unpack_from('<I', raw, header)[0]
        closing = native._closing_header(raw, person_id, header, uid, following, layout)
        if closing is None:
            continue
        start, end = native._person_block_window(raw, header, None, closing[0], layout)
        person = decoder.decode(raw, start, end)
        if person is None or not person[native._PERSON_NAME]:
            continue
        ability_at = header + layout.ability_base_offset + layout.entry_bytes * raw[header + layout.entry_count_offset]
        attributes = attributes_at_block(raw, (header, ability_at), layout)
        personality = person[native._PERSON_PERSONALITY]
        if personality is not None and 1 <= personality.adaptability <= 20:
            attributes['adaptability'] = personality.adaptability
        birth = person[native._PERSON_BIRTH_DATE]
        result.append(dict(uid=uid, id=closing[1], name=person[native._PERSON_NAME],
            birth=birth.isoformat() if birth else None, age=person[native._PERSON_AGE],
            nationId=person[native._PERSON_NATION_ID], clubUid=0, clubName='',
            hasContract=False, isHumanManager=False, noClubRecord=True,
            ca=block.ability.current, pa=block.ability.potential,
            attributes=attributes, personality=values(personality, PERSONALITY),
            reputation=reputation_at_block(raw, (header, ability_at), layout),
            preferences=preferences_at_block(raw, (header, ability_at), layout)))
        selectors[person_id + 1] = result[-1]
    if store.save.info.section_schemas.get('retirement_manager') == 2:
        with store.save._context.section('retirement_manager') as section:
            for selector, retirement in retirement_dates(section, selectors).items():
                selectors[selector]['retirementDate'] = retirement
    return result
