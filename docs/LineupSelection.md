# Best XI by saved tactic

The third pitch in **Best for tactic** assigns the filtered squad to the saved
slots. It uses the same position/role scores and eligibility checks as the
individual recommendations. No new Football Manager formula is claimed.

Each slot is matched to at most one player, and each player UID to at most one
slot. The assignment first maximises the number of filled eligible slots, then
the sum of the scores. A versatile player's individually best slot can therefore
go to another player when assigning the versatile player elsewhere improves
the entire lineup.

## Eligibility and scores

- IP or OOP uses that phase's saved position and named subrole.
- Both phases requires both recognised subroles, complete relevant attributes
  and playable familiarity in both positions. The score is their existing mean.
- Unknown roles, missing/ambiguous attributes and inadequate familiarity never
  produce a fallback assignment. Gaps distinguish unknown roles, no eligible
  candidates, and candidates used elsewhere.
- All eligible filtered players are considered, independently of the configured
  length of the individual recommendation list. Squad filters and the outgoing
  loan option also apply to the XI.
- CA and PA are not added to the objective: the existing attribute-based role
  score already measures the particular profile. This avoids double counting.
- Form, injuries, fatigue, suspensions, tactical chemistry and match performance
  are not modelled. These are recommendations from attributes, not a confirmed
  matchday-available team or official game ratings.

## Optimisation

The rectangular Hungarian assignment uses one row per slot, one column per
distinct player, and one dummy column per slot. Eligible edges have cost
`-1000 - score`, dummy edges cost zero and ineligible edges cost 1,000,000.
Scores are between 1 and 20 and there are at most eleven slots, so the coverage
bonus always takes priority; it cancels out between assignments with equal
coverage. The algorithm runs in O(slots² × (players + slots)), without limiting
the player pool or relying on a greedy choice. Slots and UIDs are sorted to
make tied optima repeatable. Familiarity is an eligibility threshold, not a
hidden penalty or extra objective.

The display uses the selected phase's pitch coordinates; for both phases it
uses IP coordinates for the same saved slot numbers. Hovering a name shows its
full name and phase scores; clicking selects the slot's individual rankings.
Hovering the summary explains the objective and limits. The pitches scroll
horizontally and vertically when space is insufficient.

## Verification

Windows checks cover a greedy-selection trap, duplicate players, incomplete
coverage, unknown phases, missing attributes, unusable positions, an XI larger
than the configured recommendation count, and 30 random small problems checked
against exhaustive enumeration. UI checks cover squad/phase/language changes,
clearing a save, rendering and scrolling to the third pitch.

## Staff training stars: remaining prerequisite

The staff extension currently exposes Motivating and named coaching attributes,
but has not validated saved Determination and Level of Discipline. No official
FM26 category-star formula has been established. Historical community formulas
are not sufficient evidence to label new numbers as the game's FM26 stars.
Training-category stars remain unimplemented pending decoding and comparison
against game screenshots for known coaches; CA must not be used as a substitute
for the missing attributes.
