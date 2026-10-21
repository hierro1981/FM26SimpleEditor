# Staff jobs: contract field and native categories

The staff job is read from the employment contract, not the staff ability block.
This is a read-only extension for the pinned FM26 26.3.2 contract layout.
It does not infer jobs from a name, attributes, nationality or department list.

## Native evidence

The plugin fingerprint is documented in [StaffAttributes.md](StaffAttributes.md).
These addresses are **RVAs in that binary**, not saved-file offsets:

- The `Cjob` property handler at `0x2093710` writes native contract byte
  `+0x26`. The corresponding getter at `0x125fe0` reads that byte.
- The basic-contract writer at `0x2093600` writes the person reference, team
  reference, wage, job byte and a two-byte field, in that order. The contract
  writer at `0x184d1b0` calls it. With the validated fmsave layout this places
  the job at **contract-chain tag +21**, immediately after the four-byte wage
  at +17. The person selector is at +5 and the team reference at +9.
- The current job-category function at `0x3374b10` masks the low bit and uses
  table `0x465b5dc`. Its result indexes the same job categories named by the
  text parser at `0x34b9d60`. Examples: 16 → category 0 (Manager), 20 → 1
  (Assistant Manager), 2 → 2 (Coach), 34 → 5 (Gk Coach), 26 → 6 (Fitness
  Coach), 88 → 14 (Technical Director), 22 → 15 (Set Piece Coach).
- The parser explicitly names categories Doctor (11), Sports Scientist (12),
  Head of Player Development (13), Technical Director (14) and Set Piece
  Coach (15). The category function maps 86 to 13, 88 to 14 and 22 to 15.
- The job formatter at `0x15105a0` also masks the low bit for the job family
  and handles the player component separately. A combined player/staff job
  therefore retains both parts in the editor.

Historical enum tables were used as a cross-check for established titles,
including Head of Physiotherapy (50) and Chief Scout (44), not as a current
FM26 lookup table. Their obsolete labels are demonstrably unsafe: 22 used to
be Media Pundit and 48 used to be Youth Coach, while the native FM26 categories
identify Set Piece Coach and Sports Scientist. See the original
[FMScoutFramework contract enum](https://github.com/ThanosSiopoudis/FMScoutFramework/blob/cb4ae1d581944453355434cb52c1655b9ddcc1aa/Entities/Ingame/Contract.cs)
and the [extended historical enum](https://github.com/AppCakeLtd/FMScoutFramework/blob/master/Entities/Ingame/Persons/Sub-Entities/Contract.cs).
Only numeric field facts are used; the implementation is independent.

## Implemented labels

| Base code | Editor label | Detail |
| --- | --- | --- |
| 2 | Coach | Specific title |
| 10 | Director of football | Specific title |
| 12 | Physiotherapist | Specific title |
| 14 | Scout | Specific title |
| 16 | Manager | Team is joined separately |
| 20 | Assistant manager | Team is joined separately |
| 22 | Set piece coach | Current native meaning |
| 26 | Fitness coach | Specific title |
| 34 | Goalkeeping coach | Specific title |
| 38, 46 | Doctor | Category only; seniority not established |
| 40 | Head of sports science | Specific title |
| 44 | Chief scout | Specific title |
| 48 | Sports scientist | Current native meaning |
| 50 | Head of physiotherapy | Specific title |
| 58, 60, 62 | Analyst | Category only; specialisation/seniority not established |
| 64 | Head of youth development | Specific title |
| 86 | Head of player development | Current native meaning |
| 88 | Technical director | Current native meaning |

Unknown codes stay unidentified. In particular the extension does not import
old U18/U21 manager codes or assume that code 0 means the person is unemployed.
Category-only labels are marked explicitly in the UI and have an explanatory
tooltip. The human manager keeps the existing independently recognised label.

## Contract ownership and validation

The extension reuses the uniquely validated staff header used for attributes.
The first word of that header is the **person index**, which differs from the
staff UID. Contract lookup uses that person index, not the UID or public ID.

The search is bounded by the next indexed player's header and the pinned
ownership-search cap. A candidate must match the person selector, team, wage,
contract dates, full tail signature and owning header. It must have a parsed
tail and be the only matching record. Known future or expired employment is
not presented as a current job. A second national-team contract does not
override the club's job. Missing or ambiguous data leaves the job unknown
without removing the profile's other data.

The uploaded Triestina save contains 66 non-human staff rows: 65 with contracts
and one without. The regression reads all 65 contract jobs: 59 specific titles
and six category-only analysts. Examples include Roberto Ripa (88, Technical
Director), Pepe (20, Assistant Manager), René Stam (34, Goalkeeping Coach),
Lieven Maesschalck (50, Head of Physiotherapy) and Jonny Evans (22, Set Piece
Coach). The input SHA-256 remains unchanged. The upstream reader's existing
`staff_unowned_tailed_contracts=1` warning remains; this does not validate every
contract elsewhere in the save.

Tests cover old enum traps, dual jobs, category-only labels, joins to the club
contract, wrong selectors, duplicate candidates, wage/date/signature mismatch,
missing tails, bounds, unreadable records and future/expired employment.
The Windows UI checks cover localisation, unknown keys, search and sorting.
No game binaries or original save data are distributed with the project.
