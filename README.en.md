# FM26 Simple Editor · v9

[Italiano](README.md) | English

FM26 Simple Editor is a local, unofficial save editor for **Football Manager 26**. It lets you find players by name, team or ID, edit their attributes, ability, positions and date of birth when available, and compare players side by side.

It includes a preview using the game's 1–20 attribute scale and charts for exploring attribute development. Changes are saved to a **new, verified copy** of the `.fm` file, preserving the original save.

## Download the Windows application

The **Build Windows executable** workflow creates `FM26SimpleEditor-win-x64.zip` on every push to `main`, on pull requests and on manual runs. Open [Actions](https://github.com/hierro1981/FM26SimpleEditor/actions/workflows/build-windows.yml), select a successful run and download **FM26SimpleEditor-win-x64** under **Artifacts** (GitHub sign-in required; retained for 30 days). Use **Run workflow** to build manually.

When a new release is published from a commit containing this workflow, the ZIP is automatically attached to the [release](https://github.com/hierro1981/FM26SimpleEditor/releases). Existing releases are not rebuilt or updated.

Extract the complete ZIP and run **FM26SimpleEditor.exe**, keeping the scripts and `requirements.txt` in the same folder. The package targets **Windows x64**, includes the .NET runtime and does not require Visual Studio or the SDK. Python and its dependencies are checked and prepared at startup as described below. The license, documentation and corresponding source for the compiled commit are also included.

## Project and technical requirements

A C# Windows Forms project for Visual Studio 2026, with a local Python reader. Building requires Windows, the .NET 8 SDK and the **.NET desktop development** workload. The reader uses Python 3.12 or newer and the dependencies in `requirements.txt`, checked and prepared at startup.

## Previous changes in version 7

- Two search rows: name/ID above, team filter and selector below. Optional filters have a limited-height area with internal scrolling and can be hidden. The results table keeps the available space and horizontal scrolling.
- Position order: GK, SW, DL, DC, DR, WBL, DM, WBR, ML, MC, MR, AML, AMC, AMR, STC. This order applies to position familiarity, position filters, the main-position column and comparison candidates. If all familiarity values are 1 or lower, the main position is unknown and sorts last.
- Attribute-label colours: purple for essential, blue for important, green for useful, and the normal background for other attributes. The reference position can be selected and defaults to automatic. This is the editor's position-based guidance, not official FM weights or a tactical-role assessment. Value-bar colours still indicate the quality of the value.
- CA and PA stars with three reference scales: squad, absolute, and league when available. The absolute scale is `ability / 40`, rounded to half a star and clamped to 0.5–5; zero or unknown PA gives N/A. Relative ratings use `2.5 + (ability − mean CA) / 20`, rounded to half a star and clamped to 0.5–5. Means use the original file values, remaining stable while you edit a player.
- League reference is derived from a readable standings group containing the exact team ID. A conservative division rule requires 18–26 teams and rounds per venue equal to the number of teams minus one. There must be a unique set of teams and at least three players with known CA. The mean uses players assigned to the exact team IDs in that table. Small leagues, ambiguous groups and unreadable data remain N/A. Names may be unavailable; the competition ID or “Divisione senza nome” is then shown. The reader infers the competition association; it is not guaranteed by the game.
- Pitch views keep portrait proportions, with scaled, centred position markers and an adaptive comparison height. Comparison columns fill the available width, with minimum widths and scrolling when needed. The “Migliore” column identifies A/B and the player's name; the information line also identifies the CA and PA winner. Lower injury proneness and dirtiness values are considered better.

## Previous changes in version 8

Internal attribute history from a single supported save, charts with multiple selected attributes, a table with values and Δ for each field, attribute search and bulk selection, and an optional current-value point. See Attribute history for coverage and date limitations.

## Changes in version 9

**Opening and saving:** a progress bar shows a percentage, phase description and available counters. The catalogue reports players processed; compression and writing report MiB; verification reports sections checked. Overall progress uses weighted phase ranges and does not estimate remaining time. It may pause while the reader performs an indivisible operation, such as decoding players. No timer simulates progress. The interface reaches 100% only after checks succeed and the data is ready. Errors are not shown as successful completion. Other short reads without measurable progress remain activity indicators.

**Comparison:** opening the second-player selector reveals a search field inside the menu. Type part of a name, team, public ID or internal ID; results keep their position-based ordering. The existing external filter can be combined with the menu filter. Click or press Enter to confirm; Esc or a click outside cancels without changing the selected player. Open the menu with a click, F4 or Down, and use the arrow keys to navigate. If there are no matches, a message is shown and no nonexistent entry can be confirmed. Typing in the menu does not load a player after every character.

## Getting started

Extract the complete project into a new folder. Open `FM26SimpleEditor.csproj` in Visual Studio, build and run. The Python scripts and `requirements.txt` are automatically copied alongside the executable.

At every startup, a setup window checks the interpreter and libraries before opening the editor:

- **Python missing or too old:** offers **Installa Python 3.13**, using WinGet for the current user. Installation starts only when clicked, requires internet, and accepts the package and WinGet source agreements. Alternatively, **Sito ufficiale Python** opens the official website for manual installation; then click **Riprova** without restarting the editor. Use this alternative if WinGet is unavailable or fails.
- **Python available:** checks the required `fmsave` and `zstandard` versions and their imports, including `backports.zstd` where needed. If everything is ready, the editor opens immediately.
- **Dependencies missing, at different versions, or unusable:** creates or repairs a dedicated environment at `%LOCALAPPDATA%\FM26SimpleEditor\runtimes\python-X.Y` and automatically installs the dependencies pinned in `requirements.txt` from PyPI, including transitive dependencies. Other applications’ global packages are left unchanged. Internet is needed only for installation or repair.

The window displays stages and error details, supports retrying, and allows exiting during setup. Interrupted installations are checked again on the next launch. The editor opens only after final verification and uses the exact interpreter that passed the checks.

The `python` command does not have to be on PATH: discovery also checks the editor’s environments, the Windows registry, local installations and the `py` launcher. To prefer an interpreter, set `FM26_PYTHON` to its full path before starting Visual Studio or the application; discovery continues if it is unusable. Manual installation with `python -m pip install -r requirements.txt` or `Installa_dipendenze.cmd` remains available.

## Search, editing and preview

**Player search** (`Ricerca giocatori`) searches by name, team, public ID or internal ID. In **Filtra squadre**, enter any part of a team name, including a substring in the middle, such as `tri`. The selector on the right lists matching teams. Select one or leave **Tutte le corrispondenze** to search all matching teams. Double-click a row or choose **Apri scheda giocatore** to open a player. At most 2,500 rows are displayed; filtering always covers the entire catalogue, and the count includes all matching players.

### Combinable optional filters

All optional filters start disabled. Enable a range with its **Usa** checkbox; both endpoints are inclusive. Age, expiry dates and contract status use the save's in-game date, displayed above the results.

- **Age:** defaults to 15–50 years and can be set from 0 to 120. An unreadable birth date excludes a player only when the age filter is enabled.
- **Position:** choose a position and minimum familiarity: **Bravo (15+)** or **Può giocare (12+)**. The second includes more accomplished players too. This is a playing position, not a tactical role or duty. Results list positions rated at least 12/20 and their values.
- **CA and PA:** separate ranges defaulting to 0–200. Controls allow values up to 255 for previously boosted data. Unknown or negatively coded potential is excluded only when filtering by PA or growth margin.
- **Contract:** all, no detected contract, expired, expiring within 6 months, within 12 months, active with a known expiry, or unavailable data. Within 12 months includes the first 6 months. These are calendar months, inclusive of today and the deadline; a contract ending today is still active.
- **Height:** centimetres, defaulting to 150–210 and adjustable from 100 to 250.
- **Minimum PA − CA margin:** finds players whose potential exceeds their current ability by at least the chosen amount.
- **Foot:** left dominant, right dominant, or both strong (at least 15/20). Dominant means stronger than the other foot; the criteria can overlap.
- **Loan:** all, on loan, not on loan, or unavailable data.

**No detected contract** requires both the absence of a readable contract and no associated team/club. A player associated with a club but without a readable contract is categorised as **unavailable data**, not contract-free. Missing expiry dates are not invented.

Numeric columns are right-aligned. Results have horizontal and vertical scrollbars when needed, resizable columns, and horizontal scrolling across all columns. Click a header for ascending order and again for descending order. Sorting applies to **all** matches before the 2,500-row display limit: age, CA, PA and IDs are numeric; dates are chronological. Missing values stay last in both directions. The main position is the one with the highest familiarity; ties use the first position in the pitch order. Its column sorts from goalkeeper to striker, with left–centre–right ordering within each line.

**Mostra filtri opzionali** shows or hides controls without changing active criteria. The filter count stays visible. **Azzera filtri** clears the search text, team selection and all optional filters. Searches do not modify the save.

**Player editing** (`Modifica giocatore`) uses its tab for raw attributes and position familiarity. **Etichette in italiano** switches attribute and position labels between Italian and English. Attributes follow the selected language's alphabetical order; positions follow the pitch order. A field's identity in the file does not depend on its label or display order. Translations belong to this editor and may differ from the game's official localisation. This toggle does not translate the entire interface.

Attributes retain their nominal raw 1–100 scale. Previously boosted values up to 125, supported by the reader, are preserved. Positions use 1–20; CA and PA have a nominal 0–200 scale, with support for previously modified values up to 255. Negative PA codes are preserved. Birth dates change only when the dedicated checkbox is enabled and the person's record can be identified unambiguously.

Bars appear above attribute and position values. CA and PA also have indicators. Red represents 1–5, yellow 6–10, green 11–15 and blue 16–20; raw values use the 1–20 conversion for their colour. Injury proneness and dirtiness colours are inverted because lower values are favourable. Bar length represents the actual value relative to the nominal scale. Editing still uses raw values.

**In-game preview** (`Anteprima nel gioco`) is read-only and reflects unsaved changes: grouped and sorted 1–20 attributes, estimated stars and positions on a pitch. Values use `clamp((raw + 2) / 5, 1, 20)` with integer division. Hover over position markers for exact familiarity values. Green means 18–20, yellow 15–17 and orange 12–14; lower values are muted. Hidden attributes are excluded by default and can be enabled in the preview.

### Stars are editor estimates

Football Manager stars depend on staff assessments and context. This project neither reproduces that judgement nor reads official star ratings from the save. It calculates a transparent estimate relative to mean CA for players in the same team, or the same club when a team reference is missing. At least three players with known CA are required; otherwise the result is N/A.

The relative formula is `2.5 + (ability − mean CA) / 20`, rounded to half a star and clamped to 0.5–5. A 20-point difference equals one star. This is not an FM formula. Unknown or negatively coded potential does not receive invented stars. The mean uses the opened file, while the previewed player's values include unsaved changes. Absolute and league estimates are described in the version 7 notes above.

## Player comparison

Every opened player is automatically added to the comparison list without duplicates. Right-click a search result for **Aggiungi all'elenco di confronto** or **Confronta con il giocatore aperto**. The number of entries appears in the **Confronto** tab title.

Open a player, then choose **Confronta…** from the editor or select the fourth tab. Player A is the opened player, including unsaved attributes, positions, CA and PA. Player B can be a teammate or another player in the comparison list, filtered by name or ID. If the team reference is missing, the club is used instead. You can remove the selected entry or clear the list. The selector also provides the searchable popup described in the version 9 notes.

Comparison is read-only: it shows estimated CA/PA stars, two pitch maps and an attribute table with bars, values and **A − B** differences. Both players' stars use A's squad as their reference. Hide the pitch maps to give attributes more space. The table supports raw or 1–20 values and optional hidden attributes. Differences below one displayed point may disappear on the 1–20 scale; enable raw values or hover over the difference to read the raw delta. The comparison list lasts for the session, survives saving a copy, and resets when another save is opened.

## Attribute history — version 8

The fifth tab automatically opens **Storico interno · sperimentale** when it recognises a sequence in the current file. No additional saves are required. The reader is limited to build 26.3.2 with game_db schema 4000, validated against the test saves. It requires at least two samples, a consistent repeated structure and exactly one candidate. Missing data, unsupported formats and ambiguous sequences remain unavailable; the dated-save mode is offered with an explanation.

**47 visible attributes** have been identified in 69-byte sample records. Historical CA/PA, foot values and hidden attributes are not assigned to samples that do not contain them. Coverage and availability can vary between players. The reader is experimental and does not guarantee support for other formats.

**Internal-history horizontal axis:** Sample 1, 2, 3… Individual dates have not been decoded. The cadence appears monthly in the compared saves, but the editor does not invent months or days. Lines connect recorded observations and do not represent measured intermediate values. Actual stored values are preserved without interpolation.

A **searchable checklist** on the left allows multiple attributes to be displayed together. Dribbling, passing and technique are initially selected. **Seleziona visibili** adds all attributes matching the list filter; **Nessuno** clears the entire selection. Filtering hides list entries without deselecting previously chosen attributes. Changing label language preserves selection by field identity.

Each attribute has a colour and line pattern in the scrollable legend, its own curve, and a table column. Hover over points for values, sample/date and source file; overlapping points show their available information together. **Scala grezza** switches between original values and the 1–20 conversion. **Mostra Δ** adds the change from each attribute's previous available observation. There is no arbitrary limit on selected attributes; selecting a small group usually makes charts easier to read.

**Includi valori attuali del file** adds a point and highlighted **Attuali** row for the file's current values. This is a comparison against the file's state, not another historical month, and can be disabled. Unsaved changes are excluded. Columns scroll horizontally, and the chart/table divider can be dragged.

**Confronta salvataggi datati** remains available. **Aggiungi salvataggi…** selects up to 20 additional `.fm` files from the same career and switches to this mode. Each point uses that file's real attributes and in-game date, with CA/PA when available. Multiple attributes can be selected here too. The modes keep separate axes: undated samples are not mixed with dated observations. Multiple versions from the same date remain separate points, without connecting lines or deltas that would imply development over time. Hover over a filename for identity-matching notes; excluded files are reported in the information message.

Cross-save matching checks ID, name and birth date. A changed birth date requires consistent public and internal IDs and is flagged. Without a public ID, the birth date must still match. Reads do not modify saves, and saving a player does not rewrite their historical samples.

## Saving

**Salva copia modificata** creates a new `.fm` file. The verified copy becomes the working file: editing a second player preserves changes already saved for the first. The current original cannot be selected as the output. The application warns about unsaved changes before switching players or files.

Earlier fixes are included: Unicode JSON, optional public IDs, birth-date verification, and rebuilding all index entries, including nested entries. Writing verifies all compressed sections before and after the change. A file with an already inconsistent index is rejected; use a working original save.

## Validation

The Python startup probe has automated tests runnable with `python -m unittest discover -s tests -v`: outdated interpreter, missing or mismatched packages, failed imports, missing pip and invalid requirements. The Windows setup window, interpreter discovery and WinGet installation require testing on Windows.

The reader and protocol have been tested with real saves, including players without public IDs and names containing Unicode characters. Write verification checks field changes and archive integrity, including successive edits to different players. The Windows interface and actual loading in Football Manager still require testing on a Windows computer.
