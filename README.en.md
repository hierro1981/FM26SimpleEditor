# FM26 Simple Editor · v15.9.1 (in development)

[![Total downloads](https://img.shields.io/github/downloads/hierro1981/FM26SimpleEditor/total.svg)](https://github.com/hierro1981/FM26SimpleEditor/releases)

[Italiano](README.md)

🌐 **Project website:** [English](https://hierro1981.github.io/FM26SimpleEditor/en/) · [Italiano](https://hierro1981.github.io/FM26SimpleEditor/)

A free **Football Manager 26** editor for Windows. Open a save, find a player and edit attributes, ability, positions and date of birth. You can also compare players, track their progress and find the best candidates for your squad’s positions and tactics. For staff, search candidates by job or training category and assess renewals for expiring contracts.

## Download and launch

Download the latest version from [Releases](https://github.com/hierro1981/FM26SimpleEditor/releases). The current version is **v15.7.4**.

- **Setup, when available in the release:** guided installation, with options to install Python and the .NET runtime if needed. Additional installations require your consent and an Internet connection.
- **Portable ZIP:** extract the complete package into a folder and run **FM26SimpleEditor.exe**. Keep all package files together.

Requires **Windows x64**, **.NET 8 Desktop Runtime x64** and **Python 3.12 or newer**. For the ZIP, install the runtime from the [official .NET 8 page](https://dotnet.microsoft.com/download/dotnet/8.0), choosing **.NET Desktop Runtime → Windows → x64**. Visual Studio and development tools are not required.

At startup, the editor checks Python and its required libraries. It offers Python installation when missing and automatically prepares missing libraries. If preparation fails, you can retry from the same window. After the initial setup, Internet is needed only for installation, repair and update checks.

## Quick start

1. Open a **copy of a working local save** with **Open save**.
2. Search for a player and double-click the result.
3. Edit the player’s data and check **In-game preview**.
4. Choose **Save changes** and a name for the new `.fm` copy.
5. Load the copy in Football Manager to check the result.

The new copy becomes the working file: later changes retain earlier saved edits. The editor alerts you to unsaved changes before switching player or file.

## What you can do

### Find and edit players

Search by name, club, public ID or internal ID. Team filtering accepts part of a name, such as `tri`. Combine filters for age, position and familiarity, CA, PA, contract and expiry, height, preferred foot, loan status and potential growth.

Loan players are highlighted. Choose whether to list them under their playing club or parent club. Columns can be sorted and resized. Search covers the entire save and displays up to 2,500 results at a time; narrow the filters when there are more matches.

Edit attributes, positional familiarity, current ability and potential ability. Date-of-birth editing is enabled when the value can be read unambiguously. Copy buttons provide the player’s name and identifiers.

Editing uses the original **1–100** attribute scale; the preview uses the game’s **1–20** scale. Bars and colours help show values and attribute importance for the selected position.

### Compare two players

Select two players with **Ctrl+click**, then choose quick comparison from the context menu. This works in search, your squad and the best-by-position or best-by-tactic rankings.

Players can also be added to the comparison list; every opened player is added automatically. Comparison displays attributes, differences, positions and estimated stars. The **position report** describes strengths, limitations and a preference for the selected position.

### Explore your squad

**My squad** identifies the club managed by the save’s first recognised human manager. It defaults to the first team; select another available squad or all club players when needed.

- **Squad list:** initially sorted by position, then current ability.
- **Best by position:** select a position and tactical role to see a configurable number of candidates (10 by default). **Ctrl/Shift+click** on the pitch requests two positions; a second selector also supports different roles in the same position.
- **Best by tactic:** choose a first-team tactic and a slot on the paired pitches. Assess candidates for in possession, out of possession or both, using the tactic’s actual roles. The squad filter also applies to these recommendations.
- **Staff:** view readable jobs, identity, contracts, ability, personality, preferences and 20 additional professional attributes on a 1–20 scale. You can also search by job. This is read only; “category” indicates that the specific title is not decoded yet.

Recommendations require complete data and sufficient familiarity in every requested position. Selecting both phases requires players to cover both. An unrecognised role does not produce a generic recommendation in its place.

### Find new staff

In **My squad → Find staff**, choose a job or one of nine training categories. Rankings show estimated suitability stars, absolute CA stars, best training category and key attributes. Select a candidate to see the reasons behind estimated interest alongside all training stars.

Borderline and insufficient-information candidates remain included; you can also show unlikely targets. Search by name, club, job or ID, choose the number of results and optionally simulate a weekly wage. **Copy assessment for feedback** prepares a summary to accompany the response received in game.

Interest is an experimental estimate, not a probability or a guarantee of acceptance. The assumed wage sends no offer to the game. Search covers readable staff linked to clubs, excludes your own staff and does not necessarily include all unemployed staff. An unread contract does not mean the person is free. Job stars are tool estimates; no suitability stars are invented for doctors.

### Track progress

**History** displays several attributes together, with position presets, colours, hover details and changes between observations.

Where available, internal history works from the open save alone. Reconstructed months are labelled as such and are not decoded exact dates. You can also add up to 20 saves from the same career to compare values at their actual in-game dates. Unsaved changes are excluded from history.

### Language and updates

Open **Options** to choose **Italiano** or **English** and the number of position/tactic recommendations (10 by default). Your choice is remembered automatically. **About** shows the editor version and lets you check for updates with a link to the release.

## Compatibility and limitations

- Use working local saves and keep an original copy. Support depends on the game version and save contents; unavailable features are explained in the interface.
- Automatic tactic reading has been checked on FM26 **26.3.2**. Careers with several human managers use only the first recognised manager.
- Stars, scores and recommendations are **editor estimates**. They do not reproduce the game staff’s official assessment or predict match performance. **Criteria and sources** explains the basis of each recommendation.
- Creating players, moving them between clubs, editing staff or tactics, and live editing are not available.

## Changes, newest first

### v15.9.1 (in development)

- **Staff planning:** combine column sorts with **Ctrl/Shift+click** in the current staff and alternatives lists. Header numbers show sort priority; a regular click returns to a single column.
- Filter current staff by **job** and **job squad** (first team, U20, U18 and readable squads). Filters combine with search and expiry and survive language changes.
- Attribute comparisons end with five-star visualisations for both people: attribute value divided by four, separate from training ratings.
- More compact role and job-squad columns in the alternatives, with wrapping text.

### v15.9.0 (in development)

- **Edit player** now contains **Edit**, **In-game preview**, **Comparison** and **History** subtabs. Existing comparison shortcuts open the matching subtab.
- **CA history:** select current ability alongside attributes to see a separate chart on its real 1–200 scale, with values and dates from compared saves. Use **Add saves** to build the trend. CA deltas are available in the table. Historic CA in internal attribute samples remains undecoded; no past values are inferred.

### v15.8.1 (in development)

- **Youth staff alternatives:** staff already working in youth teams appear before other subteams, unknown assignments and first-team staff, even when limiting results to 50.
- **Prioritise current subteam staff** is on by default for subteam vacancies, both in planning and staff search. Columns sort within each priority group; turn it off for a global ranking. Current assignments remain visible; scores and stars are unchanged.
- Fixed the alternatives heading, which incorrectly claimed to prioritise interest.

### v15.8.0 (in development)

- **Staff planning:** contract filters for expired / fewer than 3 days left, fewer than 6 months (default), fewer than 12 months, or all. Dates use the save calendar.
- Better alternatives start in descending score order; click headers to sort columns.
- **CA and PA** columns for current staff and better alternatives, with numeric sorting; missing values appear as a dash.
- Compact details appear only when hovering over the **i** icon beside the name, keeping tables readable.

### v15.7.4

This release brings together staff search and renewal planning, youth-team filters, squad-type recognition and new future Best XI scenarios.

- **Analysts:** planning now assesses the identified category, showing scores, stars and alternatives when attributes are complete.
- Renewal alternatives require the same contract variant. The **Find staff** shortcut preserves that filter; search can also show all variants.
- Exact specialisation remains explicitly unidentified. Missing required attributes are named in the assessment.

### v15.7.3

- **Staff planning:** subteam comparisons show **Working with youngsters** first, with current and alternative values and a coloured difference. Scores and stars still use the selected criterion.

### v15.7.2

- **Staff planning:** more compact columns sized to content, wrapping headers and **Comparison criterion** moved to the end.
- Double-click a renewal to open **Find staff** with the same filters as the dedicated button.

### v15.7.1

- Restored **U20, U18** and other identifiable squad labels in the roster selector and staff views. Unidentified additional groups are shown as **Additional squad**, with their code in the tooltip.
- Recognition validates team records against several clubs in the same save, rather than guessing age from slot order. Consistent numeric type-code changes can be calibrated from the save.
- Unverifiable cases retain generic labels. Planning alternatives account for the youth destination when identified.

### v15.7

- **Find staff:** choose the destination squad, for example Manager + U20, Assistant manager + U20 or Coach + U18.
- Adjustable **Working with youngsters** requirement for youth managers and coaches (default at least 12/20), also visible in the list. Training star formulas are unchanged.
- Separate **current squad** filter where identifiable: first team, subteams, youth groups and reserves/B.
- Moving from a first-team job to a subteam is not automatically considered a promotion: estimated interest is capped at borderline.
- Restored the contract reader’s squad-slot fallback when no team can be matched in the club catalogue. Conflicting records remain unidentified.

### v15.6.3

- Added **Job squad** to Staff, Find staff and Staff planning, including proposed alternatives and copied assessments.
- Identifies first team, subteam or affiliate from the contract. Readable names are preserved; unnamed groups use their slot and team ID rather than guessed U20/U18 labels.
- Staff text search also includes the squad; tooltips expose the contract reference.

### v15.6.2

- **Staff planning:** removed the club column from the renewal list and added stars beside the proposed alternative’s name.
- **Find staff using this criterion** opens search with the same job or training category, current-job filter and assumed salary offer. Interest filtering stays active and previous search text is cleared.

### v15.6.1

- **Current job stars** replace the former CA-based overall stars: specialists are assessed for the job they perform.
- Coaches use the relevant training category; other jobs use job-specific attributes. Tooltips identify the criterion and insufficient data remains unrated.
- Shared calculation across the staff list, search and planning default. Numeric CA remains visible.

### v15.6

- New **My squad → Staff planning** tab: lists contracts expiring within six months and suggests renewal or assessing a replacement.
- Compares your staff with stronger alternatives for the same job or training category, showing stars, attributes and estimated interest. Change the comparison criterion or try an assumed salary offer.
- Borderline alternatives still need verification: check a replacement’s availability before giving up a renewal.
- More conservative interest estimates for staff still contracted to a more prestigious club: imminent expiry and an assumed pay rise cannot override the refusal risk.
- Removed the generic closing warning from staff assessments.

### v15.5.1

- Italian staff labels now use the game’s terminology for **Sports Scientist** and **Head of Sports Science**, making those jobs easier to find in recruitment and staff lists.

### v15.5

- Switch the **Best XI** between current performance and future potential with **Future**. Both modes find the best combination for the entire tactic, using each player once.
- More list space in **Best by tactic**: a wider future table, compact familiarity columns, and explanations moved into a dedicated tab in **Criteria and sources**.
- The future XI uses the ranking’s experimental growth estimate: individual peaks may occur in different seasons. Missing data never produce invented values.

### v15.4.2

- Corrected future tactic projections: low starting CA no longer artificially multiplies every attribute. Growth is added from the estimated CA gain, retaining the age and position model.
- Reduced inflated future ratings; traits such as flair and determination no longer increase automatically because of spare PA.
- Clarified the difference between the selected tactical-role score and FM Genie Scout’s best-position percentage. Future table headings wrap to distinguish estimated CA from alternative scenarios.

### v15.4.1

- The future ranking in **Best by tactic** now accounts for age and uses separate goalkeeper/outfield growth curves, instead of assuming every player reaches full PA.
- Shows **estimated future CA**, remaining growth and cautious/favourable scenarios. Future role scores and stars use the new estimate; PA remains visible as the theoretical ceiling.
- This experimental estimate assumes adequate training and playing time; development is not guaranteed. Missing age or abilities are not invented.

### v15.4

- **Best by tactic** now places a potential ranking alongside today’s best players for the selected slot and phase, using the same squad and loan filters.
- Future score and stars show a conditional full-PA scenario, with current score, age and PA−CA headroom for comparison. Unknown PA remains unavailable.
- Both tables support opening players and quick comparison; those actions always use real current data. The best-XI pitch remains based on current performance.

### v15.3

- Read personal staff reputation (home, current and world), separately from club reputation and CA stars.
- Show five saved job affinities: manager, assistant, coach, physio and scout. Available in staff details, recruitment columns and copied assessments; they do not guarantee interest in an offer.
- Eight additional tactical preferences, including marking, offside and use of substitutions. Unmapped jobs and the independent tool’s percentage-rating formula remain explicitly unavailable.

### v15.2.1

- Lateral moves with no clear club-reputation improvement carry a specific warning; higher assumed wages alone do not establish interest in negotiations.
- International relocation is flagged using current employer country. High adaptability does not imply willingness to relocate, and nationality alone no longer adds a relocation penalty.

### v15.2

- Current-job filter with **Any**, recognised jobs and **Not identified**. Works alongside the desired job or training category.
- Clearer warning for borderline candidates at higher refusal risk; they remain visible for feedback.
- Copied assessments now include current job, contract and wage, with shorter score formatting.

### v15.1

- Current club reputation column and a copy icon beside each candidate’s name; overall and best-training stars moved to the end.

- **Ignore interest (best overall)** disables the interest filter and orders staff by quality, including candidates likely to refuse. Estimated interest remains visible.

### v15.0

- New **Find staff** section, by job or training category, with rankings, estimated stars and key attributes.
- Estimated interest in the human-managed club, with explanations, borderline cases and an optional wage scenario.
- Text search, sortable columns, side-by-side details and assessment copying for feedback from the game.

### v14.6

- **Clearer staff view:** attributes and training stars always side by side, with independent scrollbars. The staff list also includes sortable overall stars estimated from CA.

- **Training stars (estimate):** staff attributes and nine training categories are always shown side by side with stars, an index and the attributes used. Determination, Authority/Discipline, People management, Working with youngsters and Set Pieces are now readable too. Possession and Set Pieces weights refined against the available comparisons; stars remain indicative and may differ from the game.

- **Best XI per tactic:** a third pitch shows the lineup with the highest combined role scores, using each player once. It follows squad filters and the selected phase, and highlights slots that cannot be filled.

- The **Staff** tab displays 25 additional attributes on a 1–20 scale, including coaching, judging ability and potential, motivating, physiotherapy and sports science.
- Staff jobs are read from contracts and can be searched and sorted. Recognised jobs include goalkeeping coaches, assistant managers, technical directors and set piece coaches; analysts display their general category where the specialisation is not decoded yet.
- Staff information remains read-only; unidentified fields are not displayed.

### v14.5

- **Options** window for language and number of recommendations (10 by default); preferences are remembered.
- Include players loaned out in your squad and position/tactic recommendations.
- More accurate tactical flank placement, compact role labels and readable tabs in narrow windows.

### v14.4

- Improved tactics layout in small windows, with scrolling and fewer fixed help paragraphs; coloured tab headers make navigation clearer.
- READMEs reorganised around installation, everyday use and editor features; configuration and development details moved to dedicated documentation.
- Improved recognition of clubs playing in another nation’s league and links to their affiliated teams.

### v14.3

- Quick comparison of two selected players from the lists.
- New **Best by tactic** tab, with IP/OOP pitches and five candidates per slot.
- Italian names for all 79 tactical roles aligned with the game, including **AdM = Channel Forward** and **Amn = Deep-Lying Forward**.

### v14.1

- Dedicated staff view for your club.
- Improved sub-squad selection and default squad sorting.
- Five position candidates with stars and PA; support for two positions and their tactical roles.

### v14

- New **My squad** area with a squad list and position recommendations.

### v13.1.1

- Total download badge and version notes ordered newest first.

### v13.1

- Clearer history tooltips showing attribute, value and date in the series colours.

### v13

- Italian and English interface, with language choice remembered between launches.
- About window and update check.
- Copy player name and IDs; history presets and interactive details.

### v12

- Clearer comparison, directional difference arrows and position reports with commentary.
- Multi-position abbreviations and better filter layout when resizing the window.
- Smaller Windows package; an installed .NET runtime is required.

### v11

- Highlighted loan players and a choice of which club lists them.
- **Save changes** and unsaved-change status beside the file commands.

### v10

- Guided startup checks and preparation of Python and required libraries.

### v9

- Detailed progress during opening and saving.
- Text search inside the comparison-player menu.

### v8

- Attribute history with multiple curves and comparisons between saves.

### v7

- Improvements to search, sorting, preview, comparison and star-rating references.

## Issues and contributions

Report problems in [Issues](https://github.com/hierro1981/FM26SimpleEditor/issues), including the editor version, FM version and displayed message.

The C# Windows Forms project supports **Visual Studio 2026**. For building, configuration, tests and dependencies, see the [development guide](docs/Development.md). The [role documentation](docs/TacticalRoles.md) explains recommendation methods and limitations; [SavedTactics](docs/SavedTactics.md) covers tactic reading.

## Licence

Distributed under [GNU GPL v3](LICENSE). Football Manager is a Sports Interactive and SEGA trademark. This is an independent, unofficial project.
