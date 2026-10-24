# Staff recruitment estimates · v15.2.1

`La mia squadra → Trova staff` / `My squad → Find staff` is read-only.
No offer is sent and no save is changed. It shortlists readable club-linked staff
against the selected human-managed club. The pinned reader does not enumerate
all unemployed people. An unread contract is not proof of unemployment.

## Quality

Job profiles and their explicit weights live in `StaffRecruitment.Roles`.
These are editor heuristics, not recovered native job-suitability rules.
Each score is a weighted mean on 1–20. Stars are score / 4 rounded to the
nearest half star (away from zero), capped to 0.5–5. All required inputs must
be valid 1–20 values. Missing inputs yield a dash and sort after complete
profiles; CA is never substituted for missing job attributes.

The doctor category requires an identified doctor job, but has no quality
formula: CA stars and decoded inputs remain visible without fabricated job
stars. Analyst specialisations remain a category. Other profiles can include
candidates whose current job differs; willingness and eligibility for the new
job are not verified and are explicitly disclosed.

Training profiles reuse `CoachingStars` for all nine categories, including its
half-star thresholds. Training suitability stars therefore match the existing
staff view exactly. The /20 score is the weighted /300 score divided by 15.
See [CoachingStars](CoachingStars.md) for formulas and limits. Best-training
stars mean the person's strongest readable category, not the category selected
in the search. Both labels are explicit. Absolute overall stars remain CA/40.

## Current-job filter

The separate dropdown defaults to **Any** and offers every supported current job,
plus **Not identified**. It filters the decoded job of the displayed employment
row, independently of the desired suitability profile or training category.
Unknown jobs are not classified as unemployed. For multiple jobs the conservative
representative employment is selected before this filter: a lower-prestige second
job cannot bypass the interest calculation. **Ignore interest** does not bypass
this job filter, search or the own-staff exclusions. Selection survives language
changes by job key, not by translated label.

## Interest model v1.1

This is an uncalibrated, limited-confidence index, not a probability, not a
decoded acceptance function, and not a date prediction. It has no personal
staff reputation input. Start at zero, then apply:

| Input | Contribution |
|---|---|
| Current verifiable employment and both club reputations 1–10000 | round((destination reputation − current employer reputation) × 0.006) |
| Reputation gap at least 1000 and valid ambition | sign(gap) × integer((ambition − 1) / 4) |
| Valid loyalty and current employment | −integer((loyalty − 1) / 4) |
| Decoded contract expired before save date | +12, without current-employer reputation comparison |
| Current contract expires in at most 180 days | +8 |
| Explicit assumed wage / current positive wage | below .75: −30; below 1: −12; at least 1.5: +15; at least 1.1: +6; otherwise 0 |
| Current employer and destination in different known league countries, with adaptability ≤5 | −5; club country is a proxy, not proof of residence or willingness to move |
| Current manager considered for another job | −15 |

Clamp the index to −100…100. With no current-employer reputation comparison
and no confirmed expired contract the result is **uncertain**, regardless of
salary. Otherwise ≥10 is **plausibly interested**, −20…9 **borderline**, and
below −20 **unlikely**. These thresholds are editor choices for later
calibration. Missing wages or an unspecified offer do not imply affordability.
Missing personality values add no invented adjustment. Future contracts and
multiple jobs are disclosed as uncertainty.

v15.2 kept these scores and cutoffs unchanged. Borderline scores ≤−10 carry
an explicit **high refusal risk** label while staying in the default pool.
A current-employer prestige gap of at least 1500 with no wage scenario also
adds a cautionary reason. Two user-reported refusals in that context motivated
clearer wording; they are insufficient to calibrate probabilities or justify
person-specific rules. No player/staff names or ID exceptions are used.
Copied feedback now includes current job, club, contract end and wage to make
future comparisons less ambiguous; a zero offer is written as unspecified,
not as an actual zero-wage offer. Scores are formatted to two decimals.

Own-club and controlled-affiliate staff are excluded by person UID across all
employment rows. Human managers are excluded. Multiple employers produce one
candidate, using the largest employer reputation (unknown first, conservatively)
and then the highest wage. The rules do not add concurrent wages or reconstruct
release clauses. They cannot guarantee availability or permitted appointments.

Personal reputation, languages, job preferences, relationships, actual demands,
licences, promises, competing offers and the game decision remain unavailable.
There is no claim of a fixed reputation threshold that guarantees acceptance.

Default ordering is complete quality, quality descending, then interest index,
name and UID. Borderline and uncertain candidates are included by default.
The **Ignore interest (best overall)** checkbox disables all interest filtering and removes interest from quality tie-breaking; switching it restores quality-descending order. Interest estimates stay visible, and own staff / human exclusions remain. Uncheck to restore the plausible, borderline and uncertain pool. The user can sort any displayed column. Output
limits apply after filtering and sorting. The copy action puts only the selected
assessment on the clipboard, with IDs, save date, destination, profile and wage
scenario for reproducible manual feedback; it sends nothing externally and does
not automatically train the model.

## Lateral moves and relocation (v15.2.1)

Additional feedback reported refusals even when employer and destination club
reputations are almost equal. A separate refusal explicitly concerned leaving
where the person lives and works, despite high decoded adaptability. These are
observations, not a recovered native acceptance formula.

For current employment with known reputations, a destination below 110% of the
current employer reputation is conservatively marked **no clear improvement**.
This 10% margin is a tool choice, not an official or statistically calibrated
FM threshold. Borderline cases below the existing high-risk threshold keep that
label; other borderline lateral moves gain a specific label. They remain in the
default pool. The numeric index is unchanged by this flag, but a would-be
**plausibly interested** result is capped to **borderline** when there is no clear
improvement, so an assumed pay rise alone cannot establish willingness to talk.
A confirmed expired contract is not gated as current employment.

Known different employer and destination league countries always add an explicit
relocation-uncertainty note, even with high adaptability. Club country does not
prove actual residence, and adaptability is not a decoded willingness-to-move
flag. The prior −5 low-adaptability adjustment now uses employer country rather
than nationality: a foreign national already working in the destination country
must not be penalised solely for that nationality. Unknown countries are not
assumed to differ. There are no person-name or UID exceptions.

## Reading and validation

The pool is cached only inside the currently open save's backend store. A new
save or a saved modified copy creates a fresh store. One indexed contract pass
locates staff headers, with the same identity, ability, preferences and unknown
signature checks as the local native staff reader. Ambiguous headers do not
produce extra attributes. No full database scan per staff UID is performed.

Synthetic Python tests cover destination validation, optional failure, own club
and affiliate exclusions, human exclusion, multiple employment and cache
isolation. Windows C# checks cover ranking, salary scenarios, uncertainty,
missing inputs, shared training-star equality, localisation and narrow layouts.
Real saves used for local validation are not distributed with the repository.
