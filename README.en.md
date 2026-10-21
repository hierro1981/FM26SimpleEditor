# FM26 Simple Editor · v14.5

[![Total downloads](https://img.shields.io/github/downloads/hierro1981/FM26SimpleEditor/total.svg)](https://github.com/hierro1981/FM26SimpleEditor/releases)

[Italiano](README.md)

A free **Football Manager 26** editor for Windows. Open a save, find a player and edit attributes, ability, positions and date of birth. You can also compare players, track their progress and find the best candidates for your squad’s positions and tactics.

## Download and launch

Download the latest version from [Releases](https://github.com/hierro1981/FM26SimpleEditor/releases). The v14.5 changes described here are in development and will be available in that release.

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
- **Staff:** view readable identity, contracts, ability, personality and preferences. This is read only; job titles and many professional attributes are not yet available.

Recommendations require complete data and sufficient familiarity in every requested position. Selecting both phases requires players to cover both. An unrecognised role does not produce a generic recommendation in its place.

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

### v14.5 · in development

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
