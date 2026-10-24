# Tactical growth estimate · v15.4.2

`Best by tactic` shows current recommendations alongside an experimental estimate
of reachable future performance, replacing v15.4's unconditional full-PA scenario.
The v15.4.2 correction also removes multiplicative CA-ratio attribute inflation.
Both tables use the same full eligible squad/loan pool, selected phase(s), role
weights, current familiarity and central result limit. The best-XI pitch remains
based on current scores. Open/Compare always use real player data; no save data
or attributes are changed.

## Evidence and provenance

Sources reviewed on 24 October 2026:

- [Official FM26 youth-development guide](https://www.footballmanager.com/the-dugout/top-tips-youth-development-fm26): development priorities vary by age and attribute family; suitable match experience becomes especially important from 18 onwards.
- [SI manual, player development](https://community.sports-interactive.com/sigames-manual/football-manager-2024/players-r4958/): coaching, facilities, training and playing opportunities influence development. This is older-edition guidance, not a published FM26 formula.
- [SI staff response on late development](https://community.sports-interactive.com/forums/topic/580008-suggestion-breakout-potential/): late developers can exist even around 28/29; PA describes an absolute best-case limit. Age 24 is therefore not treated as a hard stop.

These sources support the qualitative model, **not its numerical coefficients**.
No publicly documented official conversion from age/CA/PA to future CA was found.
The rates, slowdown, scenario multipliers and separate goalkeeper window below
are explicitly editor assumptions, not decoded FM mechanics or fitted statistics.
In particular, the goalkeeper curve is a modelling choice rather than a rate
schedule supplied by the official youth guide.

## Central estimate

Require actual CA and PA in 1–200, age 14–60 at the save's game date, and at least
one unique readable familiar position (12–20). Unknown/coded PA, missing age or
position produces no projection, never a default youth age or maximum PA range.
The current player remains available at the end of the future ranking.

A player uses the goalkeeper curve only if GK familiarity is at least 12 and
at least as high as their other readable positions. Otherwise use the outfield
curve. A secondary GK position cannot grant a longer growth window to an
established outfield player. This classification uses the player's positions,
not the tactic slot currently selected.

The following **assumed annual CA budgets** are linearly interpolated:

| Age | Outfield | Goalkeeper |
| --- | ---: | ---: |
| 15 | 16 | 14 |
| 18 | 14 | 13 |
| 21 | 10 | 10 |
| 24 | 6 | 7 |
| 27 | 2.5 | 4 |
| 30 | 0.8 | 2 |
| 33 | 0 | 0.6 |
| 36 | 0 | 0 |

Starting at today's CA, advance in half-year steps to the final zero-rate age.
Use the midpoint age for interpolation (below 15, hold the first rate).
At each step:

```
ceiling = max(current CA, PA)
gain = 0.5 * scenario * min(age_budget, 0.35 * (ceiling - simulated CA))
simulated CA += gain
```

The central scenario multiplier is 1.0. Cautious and favourable scenarios use
0.7 and 1.3. Round final CA to the nearest integer, midpoint away from zero,
and clamp to [current CA, max(current CA, PA)]. The PA gap limits gains near
the ceiling; a very large PA gap cannot override an older player's tiny budget.
PA <= CA assumes no further growth. Past the growth window, the estimate is
current CA: this predicts a possible future peak, not CA at a fixed future date
and not age-related decline.

Examples, outfield CA 60 / PA 200:

| Current age | Cautious | Central | Favourable |
| --- | ---: | ---: | ---: |
| 18 | 115 | 139 | 163 |
| 34 | 60 | 60 | 60 |

These are illustrative **scenario bounds, not confidence intervals**, a
calibrated prediction or a guarantee that reality lies within them. Poor
conditions can produce no growth or decline; exceptional late bloomers can
outgrow this deliberately tapered estimate.

## Role score and stars: v15.4.2 correction

Multiplying attributes by `future CA / current CA` was incorrect as a projection
model: two players with identical current attributes and identical possible CA
gains received different increases merely because one had lower starting CA.
It inflated lower-CA players and saturated too many prospects at 20. CA is a
budget, not an attribute multiplier; this correction leaves the age model above
unchanged and replaces only the allocation of its additional CA.

For each attribute entering the existing role score:

```
gain = max(0, estimated future CA - current CA)
future attribute = min(20, current displayed attribute + 0.065 * gain)
```

Aggression, determination, flair, natural fitness and both feet are instead
held at their current values. For a player using the goalkeeper growth curve,
free-kick/penalty taking, eccentricity, punching and rushing-out are also held.
These are conservative projection exclusions informed by older CA-budget
research; they do NOT mean these traits can never change through training,
mentoring or age. No spare PA is used to invent gains in them.

Each attribute is capped separately, **without reallocating** unused gain
from capped/fixed attributes. Apply the same role weights and equal phase
averaging as today's ranking. Stars retain the existing absolute conversion.
Theoretical PA, estimated future CA, scenario CA range, age, current role score
and estimated CA gain remain separate values.

### Provisional coefficient and comparison evidence

The additive form is supported by the CA-budget distinction, including the
[original IGE experiments by CAE](https://www.fmscout.com/a-guide-to-current-ability-in-football-manager.html?pg=3)
and [the author's position-weight observations](https://www.fmscout.com/a-guide-to-current-ability-in-football-manager.html?pg=1).
Those experiments concern FM21, not a decoded FM26 formula. Their affine CA
relationship also shows why CA 200 does not imply every attribute is 20.

`0.065` is an **editor calibration choice**, not a published FM or Genie formula.
In four goalkeeper rows of the supplied Genie screenshots, the difference
between current and full-PA best-position ratings, converted from percent to
a 1–20 score and divided by PA−CA, lies approximately between 0.0640 and 0.0663.
The rounded central coefficient 0.065 gives a useful initial magnitude check.
This small sample does not establish universal slopes, position costs, future
attribute allocation or accuracy; no player-name-specific rules are used.
Its applicability outside that sample remains unvalidated. Future evidence
may justify position-specific rates and nonlinear costs at high attributes.

[FM Scout's own guide](https://www.fmscout.com/a-genie-scout-guide.html?e=page)
describes Genie ratings as weighted positional attribute evaluations, not
probabilities of reaching PA. Our selected IP/OOP tactic-role scores do not
use the same weights as Genie's best-position percentages. Do not compare
them as though they were the same metric, or multiply Genie percentages by PA.
The supplied screenshots show a later in-game date than the available save;
small current CA/attribute differences must not be forced to match by fitting.

A private read-only check on the available save reverses the incorrect ordering
of the two young goalkeeper profiles that exposed the denominator bias. It
validates that correction, not equality with every Genie rating or ordering.
No uploaded saves, screenshots or real profile fixtures are shipped.

Sort the entire eligible pool by known future score first, score descending,
current score, familiarity, name and UID, then apply the shared result limit.
Young players outside today's shortlist can therefore emerge independently.

## Interpretation and limits

Assumes adequate training and suitable regular minutes. The current player
payload does not supply verified development personality inputs, future match
minutes, coaching/facilities effects, injury forecasts or an observed CA trend.
Those factors are **not silently inferred** or given invented scores. Existing
attribute-history graphs are not a verified time series of CA and are not used
to manufacture one. Age is in completed years, so estimates can change at a
birthday. Exact peak dates and probability of reaching the forecast are unknown.

CA is a weighted budget, not an attribute average. Uniform additive allocation
is still a simplification: it does not model different physical/mental/technical
trajectories, future training allocation, footedness or position changes. Do not
read the resulting stars as exact future game attributes or native scout stars.
A long-term save/observed-CA dataset would be needed to calibrate numerical rates
and uncertainty. The source code isolates the curves for future calibration.

## Validation

Synthetic C# checks cover the 18/34 contrast, monotonic age taper, goalkeeper
classification, scenario ordering and PA bounds, missing age/position/PA,
PA <= CA, additive-growth invariance to starting CA, fixed traits, per-attribute
caps, both phases, complete-pool sorting, and source
immutability. Windows UI checks cover future-CA/scenario columns, explanatory
localised tooltips, current-data Open/Compare, scope refresh, scrolling and
clearing when a new save is opened.
