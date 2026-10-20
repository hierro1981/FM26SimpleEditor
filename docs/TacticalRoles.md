# FM26 tactical roles and suitability estimates

The embedded [catalogue](../Data/tactical_roles.json) contains **79 unique role/phase combinations**: 40 in possession and 39 out of possession. Position eligibility is merged when a role is available in several positions. Side-specific central roles use the editor's aggregate DC/DM/MC/AMC/STC familiarity; the save reader does not expose separate familiarity for the left/right central slots. SW keeps the generic position profile rather than inventing an FM26 sweeper role.

The UI offers a generic position profile and the named in/out-of-possession roles eligible for each selected position. Italian names are descriptive translations; English names are retained in the dataset. The second selector can use the same position with a different role or phase. Duties from older FM versions are not added: FM26 removed them.

## Evidence and limits

Reviewed on 20 October 2026 against these primary publications:

- [Sports Interactive's FM26 tactical feature](https://www.footballmanager.com/fm26/features/possession-out-possession-fm26s-new-tactical-evolution): official account of the new phase-specific role system. It identifies positional familiarity and role attributes as factors in suitability, but does not give numerical weights or a five-star conversion formula.
- [Guide to Football's FM26 role reference](https://fm.guidetofootball.com/fm26/roles/): the author's own in-game reference checks for build 26.3.2, used to cross-check names, phases and eligible position families. This is an independent community reference, **not an official SI specification**. Its positional cards can repeat one role; this editor merges repeated role/phase entries.
- [Roles & Attributes, by mantorras77 on the SI community forum](https://community.sports-interactive.com/forums/topic/600770-football-manager-26-roles-attributes/): a community transcription consulted as a cross-check. Being hosted on the SI forum does not make it an official formula. Its highlighted attribute tables are not imported as claimed official weights.

No official numerical suitability formula or decoded per-player game suitability rating was found in the consulted sources/current reader. The editor therefore uses the transparent estimates below. No site prose, tactical descriptions, images or hidden game formulas are bundled.

## Original editor scoring

The `focus` and `support` fields in the dataset are **original editorial profiles**, not transcribed official attribute weights or a reconstruction of the match engine. They are deliberately labelled estimates in the UI; the Criteria and sources dialog lists every attribute and weight for the selections in use.

- Generic profile: the existing position report's weights 3/2/1.
- Named role: `focus` fields have weight 3; `support` fields weight 2; remaining attributes from the general position profile weight 1. An attribute appears once; focus takes precedence.
- Score: sum of visible 1–20 attribute values multiplied by their weights, divided by the sum of weights. Values above the normal raw range are clamped to 20 as elsewhere in the editor.
- The candidate must have complete, unique, valid attribute data and familiarity of at least 12/20 in every selected position.
- With two selections, average the two scores equally, including two different roles for the same position. Missing data for either blocks the recommendation. Familiarity remains positional; no separate role familiarity is invented.
- Ties: minimum positional familiarity, name, internal ID. At most five candidates; no unsuitable fillers.
- Stars: `score / 4`, rounded to half a star. These are fixed-scale editor stars, **not FM's player-report stars**.
- CA and PA are displayed for reference and never change the ranking. Unknown PA remains unknown.

The catalogue is embedded in the application DLL; using it never requires a network request. The source links open only when clicked. Staff and save-writing behavior are unaffected.
