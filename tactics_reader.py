"""Read-only FM26 tactics reader using build-specific managed role enums.

Position layout is validated against saved tactics. Effective role/duty flags are
read from the terminal 64-bit field, never guessed from players or base headers.
"""
import argparse
from dataclasses import replace
import json
from pathlib import Path
from types import SimpleNamespace

from fmsave._container import read_section
from fmsave.readers.managed import find_managed_club_layouts, first_human_selector
from fmsave.readers.tactics import find_tactics_layout, read_tactics_header, walk_tactic_blocks
from operation_progress import report

CODE_FILE = Path(__file__).with_name('Data') / 'saved_tactic_codes.json'
SUPPORTED_BUILD = '26.3.2+2329565'


def read_codes():
    return json.loads(CODE_FILE.read_text(encoding='utf-8'))


def compatible_layout(info):
    if info.build != SUPPORTED_BUILD or info.section_schemas.get('tactics_man') != 26:
        raise ValueError('Tactic decoding is only validated for FM26 26.3.2+2329565, schema 26')
    layout = find_tactics_layout(26, info.build)
    # Two concrete incompatibilities in the attached save: a 0b0502 taker marker
    # and a 275-byte, valid UTF-8 style label. Keep all structural/bound checks.
    return replace(layout, name_length_range=(0, 4096),
                   taker_markers=tuple(dict.fromkeys(layout.taker_markers + (bytes.fromhex('0b0502'),))))


def decode_phase(slot, phase, codes=None):
    if phase not in {'ip', 'op'}:
        raise ValueError('Unknown tactic phase')
    codes = read_codes() if codes is None else codes
    prefix = 'in_possession' if phase == 'ip' else 'out_of_possession'
    mask = getattr(slot, prefix + '_mask')
    base = mask & 0x7fff
    bit = base.bit_length() - 1 if base > 0 and base & (base - 1) == 0 else None
    position = codes['positionBits'].get(str(bit)) if bit is not None else None
    flags = mask & ~0x7fff
    if flags not in (0, 0x100000, 0x200000) or (flags and position not in {'DC', 'DM', 'MC'}):
        position = None
    side = None if position is None else 'left' if flags == 0x100000 or position.endswith('L') else \
        'right' if flags == 0x200000 or position.endswith('R') else 'centre'
    settings = getattr(slot, prefix + '_settings')
    terminal = settings[-1] if settings else None
    header = slot.unknown.get(prefix + '_role_bits')
    trail = slot.unknown.get(prefix + '_trail_bits')
    high = terminal.second_bits_high if terminal is not None else None
    # The final low/high words form the effective 64-bit role + duty mask.
    # The earlier header contains a base role and may differ from the selected role.
    value = high | (trail << 32) if high is not None and trail is not None else None
    duty_bits = {int(k): v for k, v in codes['dutyBits'].items()}
    duty_mask = sum(duty_bits)
    duties = [label for flag, label in duty_bits.items() if value is not None and value & flag]
    role_value = value & ~duty_mask if value is not None else None
    matches = [r for r in codes['roles'] if position is not None and terminal is not None and
               terminal.second_bits_low == 0 and terminal.second_bits_middle == 0 and
               len(duties) == 1 and r['phase'] == phase and r['roleCode'] == role_value]
    match = matches[0] if len(matches) == 1 else None
    return dict(position=position, side=side,
                catalogueRoleId=match['catalogueRoleId'] if match else None,
                internalRoleId=match['internalId'] if match else None,
                duty=duties[0] if match else None,
                status='metadata-mapped' if match else 'unknown',
                raw=dict(positionMask=mask, headerBits=header, terminalHigh=high, trailBits=trail,
                         roleCode=role_value, roleAndDuty=value),
                controlledValidation=False)


def decode_tactic(tactic, index, codes=None):
    indices = [s.unknown.get('out_of_possession_position_index') for s in tactic.slots]
    if not tactic.walk_complete or len(tactic.slots) != 11 or not tactic.oop_index_permutation or set(indices) != set(range(11)):
        raise ValueError('Incomplete tactic slots or invalid phase-slot permutation')
    codes = read_codes() if codes is None else codes
    slots = []
    for slot in tactic.slots:
        if slot.number != len(slots):
            raise ValueError('Invalid tactic slot order')
        slots.append(dict(number=slot.number,
                          outOfPossessionIndex=slot.unknown.get('out_of_possession_position_index'),
                          inPossession=decode_phase(slot, 'ip', codes),
                          outOfPossession=decode_phase(slot, 'op', codes)))
    return dict(index=index, name=tactic.name, slots=slots)


def read_my_tactics(store, club_uid, progress=None):
    report(progress, 0, 'Reading managed team tactics')
    try:
        if type(club_uid) is not int or not 0 <= club_uid <= 4294967295:
            raise ValueError('Invalid club ID')
        save = store.save
        if club_uid not in {m.club_uid for m in save.managed_clubs()}:
            return dict(status='none', clubUid=club_uid, tactics=[], firstHumanOnly=True)
        clubs = {c.uid: c for c in save.clubs()}
        club = clubs.get(club_uid)
        if club is None:
            raise ValueError('Managed club not found')
        first = [t.team_id for t in club.teams if t.slot == 0 and not t.is_affiliate]
        if len(first) != 1:
            raise ValueError('First team is not uniquely identified')
        layout = compatible_layout(save.info)
        section = read_section(save._container_index, 'tactics_man')
        selector, _ = read_tactics_header(section, layout, save.info.file_name)
        humans = read_section(save._container_index, 'humans')
        human_layout = find_managed_club_layouts(save.info.section_schemas, save.info.build).humans
        if selector != first_human_selector(humans, human_layout, save.info.file_name):
            raise ValueError('Tactics do not belong to the identified human manager')
        report(progress, 25, 'Reading tactic position slots')
        blocks, counts = walk_tactic_blocks(section, [t.team_id for t in club.teams], layout, save.info.file_name)
        if counts.tactic_blocks != counts.tactic_blocks_count_matching:
            raise ValueError('Stored tactic count does not match decoded records')
        matching = [b for b in blocks if b.team_id == first[0]]
        if len(matching) != 1:
            raise ValueError('First-team tactic block is missing or ambiguous')
        codes = read_codes()
        tactics = [decode_tactic(t, i, codes) for i, t in enumerate(matching[0].tactics)]
        report(progress, 100, 'Tactic slots decoded')
        return dict(status='ready', clubUid=club_uid, teamId=first[0], tactics=tactics,
                    firstHumanOnly=True, build=save.info.build, mapping='metadata-enums',
                    selectedTacticIndex=None)
    except Exception as exc:
        # An optional reader must never prevent opening/editing/saving a player.
        return dict(status='unavailable', clubUid=club_uid, tactics=[], firstHumanOnly=True, error=str(exc))


def main():
    import fmsave
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('save', type=Path)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    if args.output and (args.output.suffix.lower() == '.fm' or args.output.resolve() == args.save.resolve()):
        parser.error('Output must be a JSON file, never a save file')
    with fmsave.open(args.save) as save:
        managed = list(save.managed_clubs())
        result = read_my_tactics(SimpleNamespace(save=save), managed[0].club_uid) if managed else dict(status='none', tactics=[])
    text = json.dumps(result, ensure_ascii=False, indent=2) + '\n'
    if args.output:
        args.output.write_text(text, encoding='utf-8')
    else:
        print(text, end='')
    return 0 if result['status'] != 'unavailable' else 1


if __name__ == '__main__':
    raise SystemExit(main())
