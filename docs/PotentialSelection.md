# Tactical potential scenario · v15.4

`Best by tactic` displays two independent rankings for the selected slot:
current performance and a conditional full-PA scenario. Both use the same
filtered squad/loan pool, selected phase(s), role weights and **current**
position familiarity. The best-XI pitch continues to use current scores.

## Method

Known CA and PA in 1–200 are required. Let `factor = max(1, PA / CA)`.
For each attribute used by a role:

1. Convert the actual raw attribute to the existing displayed 1–20 value.
2. Multiply that value by `factor` and cap the result at 20.
3. Apply the same `TacticalRoles.Weights` as the current recommendation.

For two phases, average the two projected role scores exactly as for the
current ranking. Stars use the same absolute score-to-half-star conversion.
When PA equals or falls below CA, no growth or decline is assumed. Missing,
coded or out-of-range CA/PA yield a dash; the maximum of a PA range is never
substituted. A player with a valid current profile remains available at the
end of the projected ranking even if the projection is unavailable.

Projection starts from the **full eligible pool**, then sorts complete
projected scores first, score descending, current score descending,
familiarity, name and UID. Only then does it apply the central result limit.
This allows a young player outside today's top ten to appear among the best
potential candidates. Existing eligibility checks continue to reject unknown
roles, missing/duplicate attributes and inadequate familiarity.

## Interpretation and limits

This is an optimistic scenario **if the player reaches PA while broadly
retaining today's attribute profile**. It is not a decoded FM development
formula or a forecast of future attributes. CA is an ability budget, not a
linear attribute average; the proportional assumption is an editor heuristic.
Per-attribute caps prevent a value above 20 but do not establish a realistic
allocation of that budget. High-growth prospects can tie at the cap.

The tool does not predict whether PA will be reached, peak age, development
time, injuries, training effectiveness, personality effects, future position
learning or age-related decline. A veteran with unfulfilled PA can therefore
have a high hypothetical score; age, CA, PA and PA−CA headroom are shown to
help assess the result. It does not prescribe extra minutes regardless of
current readiness or replace the current-performance ranking.

Rows and score tooltips explain the scenario. Both tables have their own
horizontal/vertical scrolling. Open, add-to-comparison and two-player quick
comparison use the real player UID and current profile. No attributes,
positions or save data are modified by the projection.

## Validation

Synthetic model checks cover young prospects outside the current result
limit, both phases, per-attribute saturation, PA=CA/PA<CA, missing and invalid
abilities, current eligibility and source immutability. Windows UI checks
cover both tables, unknown PA rendering, scope/phase refresh, current-data
open/compare actions, localisation, narrow layouts and save clearing.
