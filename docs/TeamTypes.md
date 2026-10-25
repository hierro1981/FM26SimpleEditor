# Squad names from team records · v15.7.1

## Finding

The supplied save uses FM26 build `26.3.2+2329565`, game database schema 4000.
Its saved game date is 2028-03-04. Both main v14.6 and the development branch
previously enumerated club team IDs correctly but used only optional names or
list-slot placeholders. The pinned fmsave Team model supplies no age-group name.
That explains why comparing those two readers did not restore U20/U18 labels.
No byte-for-byte older-save comparison was available, so this work does **not**
claim proof that numeric codes changed between saves.

A team record has an owner-linked structural header plus a nearby type code,
subteam indicator and age field. Observed examples in the supplied save:

| Club | Type code | Age field | Result |
|---|---:|---:|---|
| Triestina | 18 | 20 | U20 |
| Triestina | 12 | 18 | U18 |
| Triestina | 22 | 100 | Separate senior/reserve side |
| Fiorentina | 18 / 12 | 20 / 18 | U20 / U18 |
| Juventus | 18 / 12 | 20 / 18 | U20 / U18, despite different list slots |
| Liverpool | 10 / 12 | 21 / 18 | U21 / U18 |

A nearby flag varies between records and is not used as the squad-type code.
Player ages are a secondary sanity check, never the classification input. An
empty reserve team is still identifiable. The age field is **not** applied as a
strict registration rule: actual rules can allow over-age players.

## Structural gates

`team_types.py` runs only for game_db schema 4000. In the inspected format the
owner-linked team ID is preceded by:

- at ID−22: `ff 00`;
- at ID−20: two bytes read as a little-endian type code (native field width and
  meaning are not claimed recovered beyond these observations);
- at ID−18: a 0/1 subteam indicator;
- at ID−17: an age field, with 100 denoting the inspected senior-side context and
  255 missing/unspecified;
- at ID−16: `ff`, a varying flag byte, a four-byte club index, then ten zeros;
- at ID: exact catalogued team ID, positive identity, another four-byte identity
  field, and the observed record kind 10/11.

The club index must resolve through the upstream `uid_by_club_index` to the
exact owner of the known team ID. Appended parent-affiliate entries do not define
ownership. Duplicate matching headers are rejected. A child must also have a
separately linked first-team reference (slot 0, primary type 0, indicator 0).
Unsupported, conflicting or ambiguous records do not gain a guessed name.

## Per-save calibration

For each non-primary type code, collect readable age fields 14–23 from distinct
clubs with validated primary references. A mapping is accepted only when all
readable observations agree and **at least three distinct clubs** support it.
The code value is not fixed to the examples above. Thus a consistently changed
numeric code can still map to U20/U18 from that save's corroborating records.
A missing-age sentinel may reuse an already corroborated mapping; a conflicting
readable age cannot. Primary age-restricted clubs do not train youth mappings.

An explicitly separate non-primary team with the senior age marker 100 gets the
broad label **Reserves**, not an invented B/C affiliate name. Readable existing
team names and distinct affiliate club names retain priority in the roster.
Unknown names retain their slot and ID. The model is per open save, cached once,
and cannot leak a mapping from another career.

This adapts numeric type-code changes within the validated layout. It does not
promise to repair arbitrary future structural/schema changes. A known club is
useful as a manual control, but one club alone is deliberately insufficient for
a new automatic youth mapping. No club name, Triestina UID or team-list slot is
hardcoded as U20/U18.

## Integration and validation

The same names feed the roster selector, own-staff rows, recruitment pool,
planning and readable-current-squad filters. Staff tooltips carry the read code
and age-field evidence. Planning ranks are cached separately for each criterion
and target squad, applying the youth-destination rules to known youth incumbents.

Validation includes synthetic unknown-code calibration, changed flags/slots,
wrong owner, conflicting ages, duplicates, missing reference/quorum, sentinel
ages, schema fallback and cache isolation. Real local checks restored the four
Triestina groups, and read 67 own-staff rows including the two youth squads.
No saves, game binaries, player/staff extracts or screenshots are distributed.
