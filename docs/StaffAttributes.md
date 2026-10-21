# Staff attribute decoding

This extension reads saved attributes only. It does not edit staff, infer job
titles from attributes, reproduce job-dependent effective ratings, or calculate
coach stars. Contract-based job decoding is documented in [StaffJobs.md](StaffJobs.md).

## Evidence and layout

Static inspection used the user-provided FM26 native plugin, SHA-256
`5de88e91e8c63f7ca1502b44313891d1eed85336c90316b900f8df7a37ad9091`.
Addresses below are RVAs in that exact build, not saved-file offsets. Neither
the plugin nor save files are distributed with the project.

- The non-player writer at `0x149ff50` passes the primary object plus `0x08`
  to the attribute serializer at `0x2a9a020`.
- That serializer writes marker **12**, then **52 bytes** from its argument
  plus `0x08`: primary-object bytes `0x10` through `0x43` inclusive.
- The generic attribute getter at `0x14a08d0` reads primary-object byte
  `0x10 + index`. Indices below 26, except 13, return their stored scale.
  Index 13 and indices 26 onward support conversion to 1–20.
- The native command-name dispatch at `0x22bd350` supplies the named generic
  indices below. Its first 26 indices align with the preference block,
  including all 14 preferences already verified by the pinned upstream reader
  and the different scale of preference 13.
- The separate getter at `0x14a10a0` uses another enum and includes conditions
  based on the staff member's job. Its numeric case indices must **not** be
  reused as saved attribute indices.

For the supported layout, fmsave locates the ability block at
`header + 21 + 7 * entry_count`. The marker is at ability-block offset 13;
the 52-byte attribute payload follows at offset 14. Thus the additional 26
bytes start at ability-block offset 40. Conversion is
`max(1, min(20, (raw + 2) // 5))`, matching the native integer arithmetic for
stored values 1–100.

| Generic index | Additional block slot | Native name |
| --- | --- | --- |
| 26 | 0 | Coaching |
| 27 | 1 | Coaching Goalkeeping |
| 28 | 2 | Judgement |
| 29 | 3 | Judging Potential |
| 31 | 5 | Motivating |
| 32 | 6 | Physiotherapy |
| 33 | 7 | Tactics |
| 34 | 8 | Coaching Attacking |
| 35 | 9 | Coaching Defending |
| 36 | 10 | Fitness |
| 37 | 11 | Possession |
| 38 | 12 | Technical |
| 39 | 13 | Tactical |
| 41 | 15 | Coaching Gks Handling |
| 42 | 16 | Coaching Gks Distribution |
| 44 | 18 | Analysing Data |
| 47 | 21 | Sports Science |
| 48 | 22 | Eccentricity |
| 49 | 23 | Negotiating |
| 50 | 24 | Judging Staff Ability |

Indices 30, 40, 43, 45, 46 and 51 are intentionally omitted. Native names such
as general Coaching and Goalkeeping are kept distinct from the more specific
coaching categories; legacy fields are not claimed to appear on every FM screen.

## Validation and limits

The uploaded Triestina save yielded one validated record for each of 66
non-human staff members. Identity words, object kind, CA/PA, all decoded
preferences, the reader's unknown signature fields and the entire additional
block are checked before exposing values. Duplicate candidate records,
unsupported layouts, incomplete blocks and out-of-range bytes yield no extra
attributes. Human-manager records use another layout and are excluded.

The real-save regression returned all 20 additional attributes for all 66
records and confirmed that the input file hash was unchanged. The upstream
reader reported its existing `staff_unowned_tailed_contracts=1` warning;
this is not a successful validation of every staff contract in the save.

Synthetic tests cover rounding boundaries, entries of variable length, shifted
headers, duplicate matches, mismatched identity, CA/PA, preferences, signatures,
invalid markers, invalid ranges and truncation. No original save is written.

The extension relies on private APIs of pinned `fmsave 0.5.11`; changing that
dependency requires rerunning these checks and a real-save regression. The
native field correspondence is established for this analyzed build; other
versions require additional evidence. `r4` remains unidentified and is not
used as a job title.
