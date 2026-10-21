# Staff training stars: an explicit estimate

In **Staff**, select a person and choose **Training stars (estimate)** from the
detail selector. The nine categories show half stars, an index out of 300 and
the attribute calculation. Missing or invalid required inputs show a dash.
This is a read-only assessment, not an edit of staff or training assignments.

## Model and provenance

The plugin establishes the saved coaching attributes and the mixed scales of
Determination, Authority/Discipline and Motivating; see [StaffAttributes.md](StaffAttributes.md).
It has **not** established the complete FM26 training-star function. The stars
shown here are a documented editor model and must not be described as decoded
FM26 ratings, general staff stars, or an exact reproduction of Genie Scout.

Let DDM be the sum of Determination, Authority/Discipline and Motivating, each
on a 1–20 scale. The model uses these weights:

| Category | Index out of 300 |
| --- | --- |
| Attacking · Tactical | 6 × Attacking + 3 × Tactical + 2 × DDM |
| Attacking · Technical | 6 × Attacking + 3 × Technical + 2 × DDM |
| Defending · Tactical | 6 × Defending + 3 × Tactical + 2 × DDM |
| Defending · Technical | 6 × Defending + 3 × Technical + 2 × DDM |
| Possession · Tactical | 6 × Possession + 3 × Tactical + 2 × DDM |
| Possession · Technical | 6 × Possession + 3 × Technical + 2 × DDM |
| Goalkeeping | 9 × general Goalkeeping + 2 × DDM |
| Fitness | 9 × Fitness + 2 × DDM |
| Set Pieces | 9 × Set Pieces + 2 × DDM |

The 6/3/2 and 9/2 weighting patterns and the half-star bands come from the
[historical community discussion on SI's forum](https://community.sports-interactive.com/forums/topic/439305-coaches-and-their-star-ratings/).
That discussion is not official confirmation of an FM26 formula and contains
corrections and cautions. Applying the mixed pattern to the six modern
categories and the single-speciality pattern to Set Pieces is an editor model,
cross-checked against the supplied observations, not a recovered native rule.

The bands are 0–29: 0.5 stars, 30–59: 1, 60–89: 1.5, 90–119: 2,
120–149: 2.5, 150–179: 3, 180–209: 3.5, 210–239: 4,
240–269: 4.5, 270–300: 5.

CA and PA do not enter the calculation. No other attribute is substituted for
missing DDM inputs. Human-manager records remain unsupported because they use
another layout. The model assesses a hypothetical single training category;
it does not account for workload, coaching multiple categories, actual
assignments, or other contextual adjustments. It is not the general star
rating shown in the staff list.

## Observed agreement and remaining uncertainty

Across the two uploaded Genie Scout profiles, 17 of the 18 displayed category
ratings match this model. The fitness coach's goalkeeper rating is **2.0** in
the editor model and **2.5** in the supplied Genie Scout screenshot. This
discrepancy is retained and documented rather than fitting an extra constant
to two examples. The actual in-game training-category screen was not present
among the supplied images: the supposed training image showed the chat.
General in-game staff stars cannot validate a per-category training formula.

The individual saved inputs are verified separately from these estimated stars.
Game binaries, save files, screenshots and extracted staff fixtures are not
distributed in the repository. Tests use synthetic profiles, cover the nine
weighted calculations, each half-star boundary, missing/invalid attributes,
unsupported human profiles, CA independence, and UI rendering/localisation.
