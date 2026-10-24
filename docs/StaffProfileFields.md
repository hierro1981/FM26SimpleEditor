# Additional saved staff profile fields · v15.3

These fields extend the pinned `fmsave 0.5.11` reader. They are read-only and
do not edit records or assert a recovered transfer-acceptance function.
Independent profile screenshots supplied with the same dated save were used
for comparison. No saves, screenshots or native game binaries are distributed.

## Personal reputation

After a uniquely validated non-human staff header, let `n` be its byte-sized
entry count and `A = header + 21 + 7*n` the verified ability block. The six
bytes immediately before `A` contain three little-endian unsigned 16-bit
values. Every value must be in 1–10000; otherwise the reputation group is
unavailable. The field order is home, current, world, consistent with the
player reputation triplet at the same relative offsets in the pinned reader.

| Independent profile | Entry count | Triplet read | Triplet displayed |
|---|---:|---|---|
| A | 0 | 7445 / 7445 / 3993 | 7445 / 7445 / 3993 |
| B | 2 | 6461 / 6461 / 3365 | 6461 / 6461 / 3365 |

The variable entry list is important: fixed absolute header offsets would read
other fields on profile B. Both examples have equal home and current values,
so they verify both stored values but do not independently distinguish their
order; that order is corroborated by the existing player layout. Another
independent staff profile with different home/current values would strengthen
that distinction. World reputation is independently distinguishable here.

Local validation on the supplied 2028-03-12 save read valid triplets for all
24,025 external employment rows in the recruitment pool. This is structural
coverage on one save, not independent screen verification of every person.

Personal reputation is displayed and exported separately from club reputation
and CA-derived overall stars. It is not directly thresholded against club
reputation: equal numeric ranges do not establish an acceptance rule.

## Saved job affinities (the independent profile's Roles)

The current contract tag must first pass the existing staff-job reader's
person-selector, ownership, team, wage, dates and signature checks. A second
bounded reader then starts from that exact tag `T`:

- A byte-sized variable-list count is at `T+29`; counts above 64 are rejected.
- A fifteen-byte vector starts at `T+35+9*count`.
- The preceding five bytes must be four zero bytes and marker 1.
- All fifteen values must be 1–20.
- Its trailer must be four zero bytes, a plausible packed day/year date,
  a boolean byte, and `01 00 6c 07`. Bounds must stay within the same ownership
  window. A failed optional decode never removes the recognised current job.

The two independent profiles map the first five slots without searching for
an arbitrary run of convenient bytes:

| Slot | Field | Profile A | Profile B |
|---:|---|---:|---:|
| 0 | Manager | 11 | 12 |
| 1 | Assistant manager | 13 | 15 |
| 2 | Coach | 20 | 15 |
| 3 | Physio | 1 | 10 |
| 4 | Scout | 1 | 9 |

Other slots remain unnamed. Many are equal in these samples, so assigning
goalkeeping, fitness, technical-director or analyst names from their apparent
order would not be independently justified. In particular the independent
profile UI order is not the saved vector order. The current job is still read
from native `Cjob`, not from the largest affinity.

The local full-pool check decoded these five affinities for 22,986 of the
24,025 external rows; other rows keep empty values. They are shown on a 1–20
scale, separately from coaching ability, current employment, computed job
quality and willingness to negotiate. No affinity threshold guarantees a job
offer will be accepted. General attacking/defending/possession training
profiles use Coach as the implied proposed job; unmapped specialist job
affinities remain unavailable.

## Eight additional tactical preferences

In the existing 26-byte preference payload at `A+14`, the following slots
match both independent hidden-profile screenshots. These are preferences,
not proficiency attributes, and are not used as extra quality bonuses.

| Slot | Preference | Profile A | Profile B |
|---:|---|---:|---:|
| 5 | Free roles | 10 | 10 |
| 7 | Marking | 9 | 10 |
| 8 | Offside | 6 | 15 |
| 16 | Relaxation | 10 | 5 |
| 17 | Use of playmaker | 9 | 13 |
| 18 | Use of substitutions | 18 | 10 |
| 19 | Intensity | 3 | 9 |
| 20 | Flair | 13 | 9 |

These extend the fourteen named upstream preferences. Slots for discipline,
youngsters and determination remain ability fields as documented in
[StaffAttributes](StaffAttributes.md); the unresolved slot 2 stays unnamed.

## What remains unresolved

The percentage ratings on the independent tool's Rating tab have not been
reconstructed. Two profiles do not establish the many weights, caps and
normalisations that could generate those percentages. They must not be
presented as directly read data or silently substituted for this editor's
explicitly estimated suitability stars.

Actual contract demands, relocation willingness, competing offers and the
game's final acceptance decision also remain unresolved. The new fields add
evidence for future calibration without manufacturing a native formula.

Synthetic tests cover both variable-length structures, missing and out-of-range
values, truncation, ambiguous identities, optional-decoder failure and the
separation between profile facts and interest/quality scores. Real-screen
comparisons were performed locally; only synthetic fixtures are committed.
