"""Read current staff jobs from validated contract records (FM26 26.3.2).

The saved job is not staff r4, a department-list index, or an attribute estimate.
See docs/StaffJobs.md for the native serializer and category evidence.
"""
import struct
from bisect import bisect_right

# Native job codes; the low bit adds the player/employee dual role.
# Keep the family label where the native category is known but the detailed
# current title has not been established. In particular do not import obsolete
# names (e.g. 22 = Media Pundit, 48 = Youth Coach) from old FM enum tables.
JOBS = {
    2: ('coach', False),
    10: ('director_of_football', False),
    12: ('physio', False),
    14: ('scout', False),
    16: ('manager', False),
    20: ('assistant_manager', False),
    22: ('set_piece_coach', False),
    26: ('fitness_coach', False),
    34: ('goalkeeping_coach', False),
    38: ('doctor', True),
    40: ('head_sports_science', False),
    44: ('chief_scout', False),
    46: ('doctor', True),
    48: ('sports_scientist', False),
    50: ('head_physio', False),
    58: ('analyst', True),
    60: ('analyst', True),
    62: ('analyst', True),
    64: ('head_youth_development', False),
    86: ('head_player_development', False),
    88: ('technical_director', False),
}


def job_preferences(raw, tag, end):
    """Five independently matched saved 'Roles' fields, not Genie percentages.

    The variable list between contract ownership and the role vector must be
    bounded, followed by its zero word, marker 1, fifteen 1..20 bytes and a
    structurally valid trailer. No searching for a convenient run of bytes.
    Other slots remain unnamed pending discriminating profile samples.
    """
    if tag < 0 or tag + 35 > min(end, len(raw)):
        return {}
    count = raw[tag + 29]
    if count > 64:
        return {}
    at = tag + 35 + count * 9
    if at + 28 > min(end, len(raw)) or raw[at-5:at] != b'\0\0\0\0\1':
        return {}
    ratings = raw[at:at+15]
    if not all(1 <= value <= 20 for value in ratings) or raw[at+15:at+19] != bytes(4):
        return {}
    day, year = struct.unpack_from('<HH', raw, at+19)
    if not (0 <= day <= 366 and 1900 <= year <= 2200 and raw[at+23] in (0, 1)
            and raw[at+24:at+28] == bytes.fromhex('01006c07')):
        return {}
    return dict(zip(('manager', 'assistant_manager', 'coach', 'physio', 'scout'), ratings[:5]))


def job_fields(code):
    if type(code) is not int or not 0 <= code <= 255:
        return {}
    known = JOBS.get(code & 0xFE)
    if known is None:
        return {}  # Unknown is not unemployed.
    return dict(jobCode=code, jobKey=known[0], jobFamilyOnly=known[1],
                isPlayerJob=bool(code & 1))


class StaffJobReader:
    def __init__(self, store):
        from fmsave.readers.staff import find_staff_layouts
        from fmsave.readers.contracts import build_contract_decoder
        self.store = store
        self.clock = store.save.info.game_date
        layouts = find_staff_layouts(store.save.info.section_schemas, store.save.info.build)
        self.layout = layouts.staff
        contract = layouts.contracts
        # The native basic-contract writer places Cjob immediately after wage.
        if (contract.tag, contract.selector_offset, contract.team_id_offset,
                contract.wage_offset) != (bytes.fromhex('01006c07'), 5, 9, 17):
            raise ValueError('Unsupported staff job contract layout')
        self.decoder = build_contract_decoder(contract, store.save._context.club_index(),
                                              store.save.info.game_date, str(store.path))

    def read(self, person, header):
        from fmsave._errors import CorruptSaveError
        if person.is_human_manager or not person.has_contract or person.team_id is None:
            return {}
        if (person.contract_start is not None and person.contract_start > self.clock
                or person.contract_end is not None and person.contract_end < self.clock):
            return {}
        try:
            return self._read(person, header)
        except (CorruptSaveError, ValueError, IndexError, struct.error):
            return {}  # An unavailable job must not remove the rest of the profile.

    def _read(self, person, header):
        from fmsave.readers.staff import _staff_unknown, locate_contracted_header
        from fmsave.readers.contracts import (
            CHAIN_RECORD_TEAM_ID, CHAIN_RECORD_WAGE, CHAIN_RECORD_START,
            CHAIN_RECORD_END, CHAIN_RECORD_HAS_TERMS,
        )
        raw = self.store.raw
        if header < 0 or header + 14 > len(raw):
            return {}
        person_id = struct.unpack_from('<I', raw, header)[0]
        # Staff person-index and UID are different identifiers. Bound the search
        # by the next indexed player and by the upstream ownership-search cap.
        records = self.store.records
        position = bisect_right(records.pindexes, person_id)
        end = min(len(raw), header + self.layout.contract_header_search_bytes)
        if position < len(records.record_offsets):
            end = min(end, records.record_offsets[position] + records.layout.pindex_offset)
        if end <= header:
            return {}
        found = self.decoder._find_chain_records(raw, header, end, True, person_id)
        if found is None:
            return {}
        matches = []
        for tag, record in zip(*found):
            if (tag < header or tag + 24 > end
                    or struct.unpack_from('<I', raw, tag + 5)[0] != person_id + 1):
                continue
            # Join to exactly the contract fmsave chose for this club row,
            # including the date and signature. Never choose the first record
            # or take a national-team/old/future contract belonging to this ID.
            if (record[CHAIN_RECORD_TEAM_ID] != person.team_id
                    or record[CHAIN_RECORD_WAGE] != person.wage
                    or record[CHAIN_RECORD_START] != person.contract_start
                    or record[CHAIN_RECORD_END] != person.contract_end
                    or not record[CHAIN_RECORD_HAS_TERMS]):
                continue
            signature = _staff_unknown(None, record)
            if any(person.unknown.get(key) != value for key, value in signature.items()):
                continue
            if locate_contracted_header(raw, person_id, tag, self.layout) != header:
                continue
            matches.append((raw[tag + 21], tag))
        if len(matches) != 1:
            return {}
        code, tag = matches[0]
        fields = job_fields(code)
        preferences = job_preferences(raw, tag, end)
        if preferences:
            fields['jobPreferences'] = preferences
        return fields
