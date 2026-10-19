# 600 · User docs: proposal

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)). Session [S12](020_Work-Plan.md#s12).

The goal: user manuals so short that nobody needs to read them, with anything that isn't common sense shown in the GUI. Every section of every manual was checked against the app: is it accurate, is it already on screen, is it common sense?

**Result:** almost all of the User Guide is already on screen, as card text, tooltips, What You'll Get lines or dialogs, and it is accurate. **936 lines in 5 files could become about 160 lines in 3.** Only five things need a document: installing, the first-run order, where files go on disk, GitHub's token page, and what to attach to a crash report. 13 small facts are on no screen yet; each has a proposed line of GUI text below.

**69 sections reviewed:** 42 cut, 13 move into the GUI, 11 keep (shortened), 3 fix. The most important: [D2-46](#d2-46), which [Q7](010_Decisions.md#q7) asks about.

## Summary

| ID | Verdict | Section | Status |
|---|---|---|---|
| [D2-1](#d2-1) | Cut | Title, intro and contents (User-Guide.md:1-8) | Open |
| [D2-2](#d2-2) | Cut | The window (User-Guide.md:12-22) | Open |
| [D2-3](#d2-3) | Move into the GUI | Dashboard (User-Guide.md:24-29) | Open |
| [D2-4](#d2-4) | Cut | AIRAC Service intro and tab table (User-Guide.md:31-44) | Open |
| [D2-5](#d2-5) | Cut | Output tags and how to read What You'll Get (User-Guide.md:46-59) | Open |
| [D2-6](#d2-6) | Cut | Saving (User-Guide.md:61-66) | Open |
| [D2-7](#d2-7) | Move into the GUI | General tab ▸ AIRAC Cycle (User-Guide.md:68-73) | Open |
| [D2-8](#d2-8) | Cut | General tab ▸ Sub-Services (User-Guide.md:74-79) | Open |
| [D2-9](#d2-9) | Cut | Shared cards intro, What Files Do You Want? (User-Guide.md:83-89) | Open |
| [D2-10](#d2-10) | Move into the GUI | FE-Buddy Helper Properties (User-Guide.md:90-92) | Open |
| [D2-11](#d2-11) | Cut | Area card (User-Guide.md:93-98) | Open |
| [D2-12](#d2-12) | Cut | CRC ERAM Defaults card (User-Guide.md:99-105) | Open |
| [D2-13](#d2-13) | Cut | ARTCC Boundaries tab (User-Guide.md:107-119) | Open |
| [D2-14](#d2-14) | Cut | Airports tab (User-Guide.md:121-127) | Open |
| [D2-15](#d2-15) | Cut | Airways tab (User-Guide.md:129-144) | Open |
| [D2-16](#d2-16) | Cut | Departures and Arrivals (all but Routes) (User-Guide.md:146-156, 161-171) | Open |
| [D2-17](#d2-17) | Move into the GUI | Departures/Arrivals ▸ Routes (User-Guide.md:157-160) | Open |
| [D2-18](#d2-18) | Cut | NAVAIDs tab (User-Guide.md:173-185) | Open |
| [D2-19](#d2-19) | Cut | Fixes tab (User-Guide.md:187-196) | Open |
| [D2-20](#d2-20) | Cut | Procedure Charts: metafile, outputs, area, cards, help command (User-Guide.md:198-240) | Open |
| [D2-21](#d2-21) | Move into the GUI | What Procedure_Changes.md and Procedures.json contain (User-Guide.md:242-252) | Open |
| [D2-22](#d2-22) | Cut | FAA Chart Recall commands (User-Guide.md:254-272) | Open |
| [D2-23](#d2-23) | Cut | Telephony tab (User-Guide.md:274-291) | Open |
| [D2-24](#d2-24) | Cut | Wx Stations tab (User-Guide.md:293-300) | Open |
| [D2-25](#d2-25) | Move into the GUI | Add Custom Aliases tab (User-Guide.md:302-322) | Open |
| [D2-26](#d2-26) | Cut | File Names tab (User-Guide.md:324-334) | Open |
| [D2-27](#d2-27) | Cut | Preview Settings, Duplicate Alias Commands, Review (User-Guide.md:336-362) | Open |
| [D2-28](#d2-28) | Cut | File Conversions intro (User-Guide.md:364-371) | Open |
| [D2-29](#d2-29) | Cut | DAT to GeoJSON (User-Guide.md:373-380) | Open |
| [D2-30](#d2-30) | Move into the GUI | SCT2 to GeoJSON (User-Guide.md:382-389) | Open |
| [D2-31](#d2-31) | Cut | ERAM to GeoJSON (User-Guide.md:391-419) | Open |
| [D2-32](#d2-32) | Keep (shortened) | Output files (folder tree) (User-Guide.md:421-441) | Open |
| [D2-33](#d2-33) | Cut | Duplicate_Alias_Commands.txt (User-Guide.md:443-446) | Open |
| [D2-34](#d2-34) | Move into the GUI | Map (User-Guide.md:448-476) | Open |
| [D2-35](#d2-35) | Cut | Settings ▸ Save, Export, Import (User-Guide.md:478-484) | Open |
| [D2-36](#d2-36) | Move into the GUI | Settings Profile (User-Guide.md:486-491) | Open |
| [D2-37](#d2-37) | Move into the GUI | Settings ▸ Facility and Default Output Directory (User-Guide.md:492-494) | Open |
| [D2-38](#d2-38) | Cut | Settings ▸ Default ROI, GeoJSON Files, Credentials, GitHub Requests, Updates, Main Window (User-Guide.md:495-511) | Open |
| [D2-39](#d2-39) | Cut | Reset / Uninstall (User-Guide.md:512-517) | Open |
| [D2-40](#d2-40) | Cut | Info (User-Guide.md:519-532) | Open |
| [D2-41](#d2-41) | Cut | What FE-Buddy does (Getting-Started.md:3-18) | Open |
| [D2-42](#d2-42) | Keep (shortened) | Install (Getting-Started.md:20-26) | Open |
| [D2-43](#d2-43) | Cut | First launch (Getting-Started.md:28-35) | Open |
| [D2-44](#d2-44) | Cut | Settings - once (Getting-Started.md:37-47) | Open |
| [D2-45](#d2-45) | Keep (shortened) | Your first run (Getting-Started.md:49-60) | Open |
| [D2-46](#d2-46) | Move into the GUI | Every cycle after (Getting-Started.md:62-65) | Fixed |
| [D2-47](#d2-47) | Cut | Where next (Getting-Started.md:67-71) | Open |
| [D2-48](#d2-48) | Cut | How often do I run FE-Buddy? (FAQ-and-Troubleshooting.md:5-8) | Open |
| [D2-49](#d2-49) | Keep (shortened) | Where are my files? (FAQ-and-Troubleshooting.md:10-14) | Open |
| [D2-50](#d2-50) | Keep (shortened) | Why does Windows show a different version number? (FAQ-and-Troubleshooting.md:16-19) | Open |
| [D2-51](#d2-51) | Fix | Does FE-Buddy send my data anywhere? (FAQ-and-Troubleshooting.md:21-28) | Open |
| [D2-52](#d2-52) | Cut | I set FEBUDDY_GITHUB_TOKEN for FE-Buddy 2.x (FAQ-and-Troubleshooting.md:30-34) | Open |
| [D2-53](#d2-53) | Cut | What if Wx Stations or Telephony can't download? (FAQ-and-Troubleshooting.md:36-41) | Open |
| [D2-54](#d2-54) | Cut | Same command in Departures.txt and Arrivals.txt (FAQ-and-Troubleshooting.md:43-48) | Open |
| [D2-55](#d2-55) | Move into the GUI | AIRAC Service waiting / current cycle failed (FAQ-and-Troubleshooting.md:52-55) | Open |
| [D2-56](#d2-56) | Cut | Next cycle says "not yet published" (FAQ-and-Troubleshooting.md:57-59) | Open |
| [D2-57](#d2-57) | Cut | Red tab, "partial", no CRC ERAM defaults, missing file, custom alias file unreadable (FAQ-and-Troubleshooting.md:61-87) | Open |
| [D2-58](#d2-58) | Move into the GUI | The installer won't replace what's installed (FAQ-and-Troubleshooting.md:89-93) | Open |
| [D2-59](#d2-59) | Keep (shortened) | FE-Buddy crashed (FAQ-and-Troubleshooting.md:95-100) | Open |
| [D2-60](#d2-60) | Fix | I want to start fresh (FAQ-and-Troubleshooting.md:102-105) | Open |
| [D2-61](#d2-61) | Keep (shortened) | Still stuck? (FAQ-and-Troubleshooting.md:107-110) | Open |
| [D2-62](#d2-62) | Cut | Whole file (Glossary.md:1-83) | Open |
| [D2-63](#d2-63) | Keep (shortened) | Intro and "The short version" (GitHub-Token-Guide.md:1-22) | Open |
| [D2-64](#d2-64) | Keep (shortened) | Steps 1-5 (with screenshots 01-04) (GitHub-Token-Guide.md:24-83) | Open |
| [D2-65](#d2-65) | Cut | Step 6: Save it in FE-Buddy (+ screenshot 05) (GitHub-Token-Guide.md:85-104) | Open |
| [D2-66](#d2-66) | Keep (shortened) | When the token expires (GitHub-Token-Guide.md:106-118) | Open |
| [D2-67](#d2-67) | Keep (shortened) | If something goes wrong (GitHub-Token-Guide.md:120-134) | Open |
| [D2-68](#d2-68) | Cut | Keeping the token safe (GitHub-Token-Guide.md:136-140) | Open |
| [D2-69](#d2-69) | Fix | Text that describes the guides and must change with the trim ((code)) | Open |

## Proposed layout

Today: 936 lines in 5 files. Proposed: about 160 lines in 3 files. All three keep their file names (the app,
its tests, README and the issue templates link to them).

**User-Guide.md** (~50 lines; absorbs Getting-Started.md)
- `# User guide` - one line: everything else is explained on screen; hover anything for help.
- `## Install` - `FE-BUDDY-Setup.msi` from Releases; upgrades 2.9+ in place, removes older 2.x; Windows 10/11 64-bit + internet. ([D2-42](#d2-42))
- `## Your first run` - the 5-step checklist from [D2-45](#d2-45) (incl. "add your facility's alias file before uploading").
- `## Every cycle` - one line: pick Current on General, then Run (drop once [D2-46](#d2-46) is in the GUI).
- `## Where your files go` - the folder tree from [D2-32](#d2-32), plus "upload only `Aliases\Combined_Alias.txt`".
- `## Where FE-Buddy keeps its own data` - `%APPDATA%\FE-Buddy` (`User Configurations`, `Logs`), credentials in Windows Credential Manager. (2-3 lines)

**FAQ-and-Troubleshooting.md** (~35 lines)
- `### Why does Windows show a different version number?` ([D2-50](#d2-50))
- `### Does FE-Buddy send my data anywhere?` ([D2-51](#d2-51) wording)
- `### The installer won't replace what's installed` (drop once [D2-58](#d2-58)'s installer text ships)
- `### FE-Buddy won't open` / start fresh ([D2-60](#d2-60) wording)
- `### FE-Buddy crashed` - heading text unchanged ([D2-59](#d2-59))
- `## Still stuck?` ([D2-61](#d2-61))

**GitHub-Token-Guide.md** (~75 lines incl. 4 screenshots)
- Intro + `## The short version` table (as is).
- `## Step by step` - steps 1-5 trimmed ([D2-64](#d2-64)), step 6 as one line ([D2-65](#d2-65)).
- `## When the token expires` - regenerate steps + the leak line ([D2-66](#d2-66), [D2-68](#d2-68)).
- `## If something goes wrong` - heading text unchanged, table cut to 4 rows ([D2-67](#d2-67)).

**Getting-Started.md** - delete; Install and the first-run checklist move into the User Guide.

**Glossary.md** - delete ([D2-62](#d2-62)).

**Media/GitHub-Token/05-fe-buddy-credential-editor.png** - delete with step 6; retake or edit 01 ([D2-64](#d2-64)).

## Move into the GUI

<a id="d2-3"></a>

### D2-3 · Dashboard

**Status:** Open

**Doc:** [`docs/Users/User-Guide.md:24-29`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L24-L29)

**Today:** partly - DashboardView.xaml:128-143, :164-176 ("New post — open News"), :201-257. The log folder is named nowhere (the Clear tooltip, :203, only says "The log file is unchanged").

**Proposed:** Activity log card, beside Clear: button **Open log folder**, tooltip "FE-Buddy's full log, one file a day, kept 30 days." Then cut the section.

<a id="d2-7"></a>

### D2-7 · General tab ▸ AIRAC Cycle

**Status:** Open

**Doc:** [`docs/Users/User-Guide.md:68-73`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L68-L73)

**Today:** partly - "partial" has a full tooltip (AiracGeneralTabViewModel.cs:482-505), but the row tooltip is enabled only for Partial (AiracGeneralTabView.xaml:72-79); "not yet published" and "failed" explain nothing (the FAQ carries both, FAQ:52-59).

**Proposed:** enable the row tooltip for these two states too. Not yet published: "The FAA hasn't published this cycle yet. It usually appears a few weeks before it takes effect; FE-Buddy checks at every launch." Failed: "This cycle couldn't be downloaded or read. Check your internet connection and restart FE-Buddy; the Dashboard activity log says what went wrong."

<a id="d2-10"></a>

### D2-10 · FE-Buddy Helper Properties

**Status:** Open

**Doc:** [`docs/Users/User-Guide.md:90-92`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L90-L92)

**Today:** partly - Views/Cards/FebPropertiesCard.xaml:25 ("…ignored by CRC and may increase file size"). The reason (MultiPoint grouping lost) is not shown.

**Proposed:** FebPropertiesCard description, last line → "These are ignored by CRC and make files larger: a property that differs per object (such as an ID) also stops identical symbols being written as one feature."

<a id="d2-17"></a>

### D2-17 · Departures/Arrivals ▸ Routes

**Status:** Open

**Doc:** [`docs/Users/User-Guide.md:157-160`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L157-L160)

**Today:** no (no "body"/"transition" text in DeparturesViewModel / ArrivalsViewModel).

**Proposed:** a What You'll Get note on each tab. Departures: "Each airport gets the bodies the FAA assigns it (all of them when none are assigned), then every transition." Arrivals: "Each airport gets every transition, then the bodies the FAA assigns it (all of them when it has none assigned)."

<a id="d2-21"></a>

### D2-21 · What Procedure_Changes.md and Procedures.json contain

**Status:** Open

**Doc:** [`docs/Users/User-Guide.md:242-252`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L242-L252)

**Today:** the Markdown document explains itself when opened (cut). The two JSON keys are on no screen, and `Procedures.json` is meant for scripts (ProceduresView.xaml:52).

**Proposed:** External Procedures in Our Airspace card, one more bullet under "The options in this card…": "In `Procedures.json` they are marked `\"includedBecause\": \"routeFixInFacility\"`, with the `facilityFixes` that brought them in."

<a id="d2-25"></a>

### D2-25 · Add Custom Aliases tab

**Status:** Open

**Doc:** [`docs/Users/User-Guide.md:302-322`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L302-L322)

**Today:** yes except `.FeUseOnly` - ConcatenateAliasesView.xaml:61-92 (incl. "Last-One-Wins", the no-files warning), :104-188 (Check tooltip, credential, private/public help, guide buttons).

**Proposed:** Combine Alias Files card, append to the Note at :87: "A `.FeUseOnly` line is moved to the top of the combined file (only the first one is kept)."

<a id="d2-30"></a>

### D2-30 · SCT2 to GeoJSON

**Status:** Open

**Doc:** [`docs/Users/User-Guide.md:382-389`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L382-L389)

**Today:** partly - SctToGeojsonView.xaml:22-23 lists what is written, not what isn't.

**Proposed:** append to the page intro: "Airports, VORs, NDBs and fixes only place other points; they aren't written. VRC colours aren't carried over."

<a id="d2-34"></a>

### D2-34 · Map

**Status:** Open

**Doc:** [`docs/Users/User-Guide.md:448-476`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L448-L476)

**Today:** yes except one fact - MapWorkspace.xaml:89-115 tooltips, :153 editing banner, :307 Shift tip, :440, :452, :473, :488 side-panel hint. "A polygon is picked at its outline" is not shown.

**Proposed:** MapWorkspace.xaml:488 hint → "Ctrl + click a line, dot, label or an area's outline to see its properties. …" (rest unchanged).

<a id="d2-36"></a>

### D2-36 · Settings Profile

**Status:** Open

**Doc:** [`docs/Users/User-Guide.md:486-491`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L486-L491)

**Today:** partly - SettingsView.xaml:64-88. Its "Shared across profiles" list (:72-73) leaves out the window size/position, which is shared (SettingsView.xaml:365-366 comment, Shared.json).

**Proposed:** add a line to that list: "Main window size and position".

<a id="d2-37"></a>

### D2-37 · Settings ▸ Facility and Default Output Directory

**Status:** Open

**Doc:** [`docs/Users/User-Guide.md:492-494`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L492-L494)

**Today:** output folder yes (example line, checkbox). Facility: no description at all (SettingsView.xaml:96-115).

**Proposed:** caption under the Facility box: "Ticked to start on tabs that pick ARTCCs, listed first in Procedure_Changes.md and Duplicate_Alias_Commands.txt, and used by External Procedures in Our Airspace."

<a id="d2-46"></a>

### D2-46 · Every cycle after

**Status:** Fixed (not yet merged), option A ([Q7](010_Decisions.md#q7)): the General tab saves `CyclePosition` (Current or Next), not the cycle ID, so it opens on the cycle in effect. A saved Previous, or a cycle ID an older build saved, opens on Current; a saved Next the FAA hasn't published yet opens on Current with a note, keeping the saved choice. Getting-Started's step 5 and Settings-Reference.md updated. Tests: `AiracGeneralTabViewModelTests.the_cycle_is_saved_as_its_position_and_reopens_on_it`, `anything_but_a_saved_next_opens_on_current`, `a_saved_next_cycle_not_yet_published_opens_on_current`

**Doc:** [`docs/Users/Getting-Started.md:62-65`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/Getting-Started.md?plain=1#L62-L65)

**Today:** no. A real trap: a user who only reads the screen reruns the old cycle.

**Proposed:** amber line under the AIRAC Cycle card and on Preview Settings while Previous is picked: "This is the previous cycle (2609). The cycle in effect now is 2610." Then drop the doc line. (Also worth asking whether the saved choice should be the position, not the ID.)

<a id="d2-55"></a>

### D2-55 · AIRAC Service waiting / current cycle failed

**Status:** Open

**Doc:** [`docs/Users/FAQ-and-Troubleshooting.md:52-55`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/FAQ-and-Troubleshooting.md?plain=1#L52-L55)

**Today:** partly - waiting text yes; the failed text (AiracServiceViewModel.cs:207) says "see the Dashboard activity log" but not what to do.

**Proposed:** AiracServiceViewModel.cs:207 → "The current AIRAC cycle couldn't be downloaded or read. Check your internet connection and restart FE-Buddy; the Dashboard activity log says what went wrong."

<a id="d2-58"></a>

### D2-58 · The installer won't replace what's installed

**Status:** Open

**Doc:** [`docs/Users/FAQ-and-Troubleshooting.md:89-93`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/FAQ-and-Troubleshooting.md?plain=1#L89-L93)

**Today:** partly - the installer's message (FeBuddy.Installer/Package.en-us.wxl:8) says "opt out to the latest stable release instead" without saying how.

**Proposed:** wxl text → "This FE-BUDDY version can't replace what's installed: a stable release can't be replaced by an older one. To leave a pre-release, open FE-Buddy ▸ Settings ▸ Updates / Update Channel, choose Stable, press Save, then Go back now." Then cut the FAQ entry.

## Fix

<a id="d2-51"></a>

### D2-51 · Does FE-Buddy send my data anywhere?

**Status:** Open

**Doc:** [`docs/Users/FAQ-and-Troubleshooting.md:21-28`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/FAQ-and-Troubleshooting.md?plain=1#L21-L28)

**Problem:** mostly - GNG's part comes from GNG's own site, not GitHub (Core/Infrastructure/Telephony/TelephonyFiles.cs:25, `gng.aero-nav.com`). Time: timeapi.io, falling back to a cloudflare.com request (Core/Infrastructure/Platform/UtcTimeCheck.cs:23-24) - "a public time service" is fine.

**Proposed:** "No. It only downloads: the FAA's data, aviationweather.gov's station list, the virtual airline list (GNG's site and VATSIM-Radar's GitHub, if you tick it), the time from a public time service, updates and News from GitHub, and any custom alias file you point it at. Your settings stay in `%APPDATA%\FE-Buddy`; tokens and passwords stay in Windows Credential Manager and go only to the websites you allow."

<a id="d2-60"></a>

### D2-60 · I want to start fresh

**Status:** Open

**Doc:** [`docs/Users/FAQ-and-Troubleshooting.md:102-105`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/FAQ-and-Troubleshooting.md?plain=1#L102-L105)

**Problem:** partly - deleting that folder only resets settings; the downloaded data, logs and backups stay (Reset deletes those too, ResetViewModel.cs:23-29).

**Proposed:** "FE-Buddy won't open? Close it and delete `%APPDATA%\FE-Buddy\User Configurations` to go back to default settings (your credentials stay in Windows Credential Manager). Otherwise use Settings ▸ Reset FE-Buddy…"

<a id="d2-69"></a>

### D2-69 · Text that describes the guides and must change with the trim

**Status:** Open

**Doc:** (code)

**Problem:** true today, wrong after the trim (the guide will no longer describe each tool or the alias rules).

**Proposed:** InfoViewModel/config.yml blurb → "Installing, your first run, and where your files go."; delete the AliasGuideContent sentence; docs/README.md: drop the Getting started and Glossary rows, User guide row → "Installing, first run, where files go."

---

## Keep (shortened)

- <a id="d2-32"></a>**D2-32** Output files (folder tree) ([`docs/Users/User-Guide.md:421-441`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L421-L441)): keep the tree only (it answers FAQ "Where are my files?" too). Drop bullets 439-441 (RunCard notes and the Combine card already say them).
- <a id="d2-42"></a>**D2-42** Install ([`docs/Users/Getting-Started.md:20-26`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/Getting-Started.md?plain=1#L20-L26)): move into the User Guide as 3 lines, as is.
- <a id="d2-45"></a>**D2-45** Your first run ([`docs/Users/Getting-Started.md:49-60`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/Getting-Started.md?plain=1#L49-L60)): a 5-line checklist: 1. Settings: pick your Facility, Set ROI…, Save. 2. AIRAC Service ▸ General: pick the cycle and sub-services, Save. 3. Fix any tab with a red dot. 4. Add Custom Aliases: add your facility's own alias file. 5. Preview Settings ▸ Run AIRAC Service; upload `Aliases\Combined_Alias.txt`.
- <a id="d2-49"></a>**D2-49** Where are my files? ([`docs/Users/FAQ-and-Troubleshooting.md:10-14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/FAQ-and-Troubleshooting.md?plain=1#L10-L14)): fold into the User Guide's "Where your files go" tree ([D2-32](#d2-32)); no FAQ entry needed.
- <a id="d2-50"></a>**D2-50** Why does Windows show a different version number? ([`docs/Users/FAQ-and-Troubleshooting.md:16-19`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/FAQ-and-Troubleshooting.md?plain=1#L16-L19)): keep as is (3 lines).
- <a id="d2-59"></a>**D2-59** FE-Buddy crashed ([`docs/Users/FAQ-and-Troubleshooting.md:95-100`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/FAQ-and-Troubleshooting.md?plain=1#L95-L100)): keep, heading text unchanged (anchor `#fe-buddy-crashed`). Optional: "`FE-Buddy_<date>.log` (date in UTC)".
- <a id="d2-61"></a>**D2-61** Still stuck? ([`docs/Users/FAQ-and-Troubleshooting.md:107-110`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/FAQ-and-Troubleshooting.md?plain=1#L107-L110)): keep (2 lines).
- <a id="d2-63"></a>**D2-63** Intro and "The short version" ([`docs/Users/GitHub-Token-Guide.md:1-22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/GitHub-Token-Guide.md?plain=1#L1-L22)): keep as is.
- <a id="d2-64"></a>**D2-64** Steps 1-5 (with screenshots 01-04) ([`docs/Users/GitHub-Token-Guide.md:24-83`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/GitHub-Token-Guide.md?plain=1#L24-L83)): keep; two trims: step 1 "choose GitHub personal access token as the type" can go (it is the default type, CredentialEditorViewModel.cs:37); the "older versions of the page" note (74-75) can go. Screenshot nit: 01-name-owner-expiration.png's Description field reads "…our custom alias file for vNAS_Alias.txt" - the 2.x file name; today it's `Combined_Alias.txt` (AiracOutputPaths.cs:46). Retake or edit that field.
- <a id="d2-66"></a>**D2-66** When the token expires ([`docs/Users/GitHub-Token-Guide.md:106-118`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/GitHub-Token-Guide.md?plain=1#L106-L118)): keep steps 1-3 only (GitHub ▸ token ▸ Regenerate token; copy; Settings ▸ Credentials ▸ Edit…, paste, Save). Cut the warning paragraph (108-111).
- <a id="d2-67"></a>**D2-67** If something goes wrong ([`docs/Users/GitHub-Token-Guide.md:120-134`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/GitHub-Token-Guide.md?plain=1#L120-L134)): keep the heading text exactly (anchor `#if-something-goes-wrong`, used by Links.cs:35 and tested). Trim the table to the 4 rows whose fix is on GitHub's side: not found (no credential), not found / can't see (Pending, repository not in the token, wrong resource owner), refused (regenerate), no Contents permission.

## Cut (already on screen, or common sense)

| ID | Section | Where the app already says it |
|---|---|---|
| <a id="d2-1"></a>D2-1 | Title, intro and contents ([`docs/Users/User-Guide.md:1-8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L1-L8)) | replace with the new outline (see layout). Drop the Glossary/Getting-started links (both files go). |
| <a id="d2-2"></a>D2-2 | The window ([`docs/Users/User-Guide.md:12-22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L12-L22)) | yes - Wpf/Views/ShellWindow.xaml:365 (collapse tooltip), :434-485 (Systems popup: one row per system with its detail, note and Re-check), :201-210 and :246-257 (version/badge tooltips); UpdateWindowViewModel.cs:147 ("Update now downloads the installer… your settings are kept"). |
| <a id="d2-4"></a>D2-4 | AIRAC Service intro and tab table ([`docs/Users/User-Guide.md:31-44`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L31-L44)) | yes - TabbedServiceView.xaml:144-146 (rail); greyed tab tooltip from AiracServiceViewModel.cs:318; AiracGeneralTabView.xaml:53-56 (waiting card, AiracServiceViewModel.cs:205-210); ServicePreviewTabView.xaml:54-55. |
| <a id="d2-5"></a>D2-5 | Output tags and how to read What You'll Get ([`docs/Users/User-Guide.md:46-59`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L46-L59)) | yes - every tag has a tooltip ("Affects the alias file, which is off in the General tab", OutputTag.cs:42-46); the greyed card says why (OutputTag.OffNote, :63-77); footer "Turn GeoJSON and Alias output on or off in the General tab" (GeojsonSubServiceViewModel.cs:150-151). The chips read as a sentence on their own. |
| <a id="d2-6"></a>D2-6 | Saving ([`docs/Users/User-Guide.md:61-66`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L61-L66)) | yes - TabbedServiceView.xaml:97-118 ("Unsaved changes", Undo tooltips); Theme/Controls.Surfaces.xaml:657-666 (dot tooltips "Unsaved changes" / "Something here needs fixing"); AttentionCard + ServiceTabViewModel.cs:150-152 ("…outlined in red below"); ServicePreviewTabView.xaml:64-75 (unsaved notice, "Unsaved settings are saved before the run starts"). |
| <a id="d2-8"></a>D2-8 | General tab ▸ Sub-Services ([`docs/Users/User-Guide.md:74-79`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L74-L79)) | yes - AiracGeneralTabView.xaml:120-121 ("Hover over an option for a quick description…"), :170-179; every box has a tooltip (SubServiceRow.cs:112-126); Add Custom Aliases greyed tooltip "No alias file is being made. To make one, tick a sub-service and its Alias box in the General tab." (AiracServiceViewModel.cs:318). |
| <a id="d2-9"></a>D2-9 | Shared cards intro, What Files Do You Want? ([`docs/Users/User-Guide.md:83-89`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L83-L89)) | yes for the files (Views/Cards/GeojsonFilesCard.xaml:37-57 with each tab's file names and descriptions). MultiPoint: no - see [D2-10](#d2-10). |
| <a id="d2-11"></a>D2-11 | Area card ([`docs/Users/User-Guide.md:93-98`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L93-L98)) | yes - Views/Cards/AreaCard.xaml:33-103 (each choice with its own description, "Which ROI", corners, Pick on map…, default-ROI summary or amber "not set" hint). The start rule is visible (the tab opens on it). |
| <a id="d2-12"></a>D2-12 | CRC ERAM Defaults card ([`docs/Users/User-Guide.md:99-105`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L99-L105)) | yes - Views/Cards/CrcDefaultsCard.xaml.cs:21-25 (default Note, same vNAS link and "one-time setup"), CrcFileChoice.xaml:28-33, CrcDefaultsPanel.xaml:102-132 (range tooltips). |
| <a id="d2-13"></a>D2-13 | ARTCC Boundaries tab ([`docs/Users/User-Guide.md:107-119`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L107-L119)) | yes - ArtccBoundariesView.xaml:30-89; feb.type tooltip "…tells the overlapping oceanic CTA and FIR lines apart" (ViewModels/Models/ArtccBoundaryFebPropertyOptions.cs:24). |
| <a id="d2-14"></a>D2-14 | Airports tab ([`docs/Users/User-Guide.md:121-127`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L121-L127)) | yes - AirportsView.xaml:29-41 (footer "Every operational airport, whichever you pick above"); General tab tooltips AiracSubServices.cs:86-91. |
| <a id="d2-15"></a>D2-15 | Airways tab ([`docs/Users/User-Guide.md:129-144`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L129-L144)) | yes - AirwaysView.xaml:33-178 (incl. "J and Q start as High, and V and T as Low…", "Allowed range: 0 to 10 NM", "A leg too short…", "Which Airways the Alias File Gets"); footer GeojsonSubServiceViewModel.cs:150. |
| <a id="d2-16"></a>D2-16 | Departures and Arrivals (all but Routes) ([`docs/Users/User-Guide.md:146-156`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L146-L156), 161-171) | yes - DeparturesView.xaml:31-135, ArrivalsView.xaml:31-132 (folder, name format and examples, "Which Departures the ROI Keeps", "(number of days from today, not the cycle effective date)"); AiracSubServices.cs:100-113; name/command rules in Info ▸ Alias Command Guide (Core/Application/AliasGuide/AliasGuideContent.cs, AliasPracticeContent.cs:94-98). |
| <a id="d2-18"></a>D2-18 | NAVAIDs tab ([`docs/Users/User-Guide.md:173-185`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L173-L185)) | yes - NavaidsView.xaml:31-112 ("These types apply to the GeoJSON only. The alias file always has every type.", "Every operational NAVAID…", symbol-style captions); AiracSubServices.cs:116-120. |
| <a id="d2-19"></a>D2-19 | Fixes tab ([`docs/Users/User-Guide.md:187-196`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L187-L196)) | yes - FixesView.xaml:31-182 (layout examples and the NO-CHART note at :55). |
| <a id="d2-20"></a>D2-20 | Procedure Charts: metafile, outputs, area, cards, help command ([`docs/Users/User-Guide.md:198-240`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L198-L240)) | yes - ProceduresView.xaml:36-461; ProceduresViewModel.cs:231-235, :264-280, :317-355, :412, :729-730 (RenumberNote word for word), :69 (MaxAirports 100); partial tooltip AiracGeneralTabViewModel.cs:482-505. |
| <a id="d2-22"></a>D2-22 | FAA Chart Recall commands ([`docs/Users/User-Guide.md:254-272`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L254-L272)) | yes - Info ▸ Alias Command Guide (AliasGuideContent.cs:280 runways, :329-339 "Charts with no command"); Review message "Please report it to the FE-Buddy developers…" (ChartRecallAliasBuilder.cs:259, :267). |
| <a id="d2-23"></a>D2-23 | Telephony tab ([`docs/Users/User-Guide.md:274-291`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L274-L291)) | yes - TelephonyView.xaml:36-66 (incl. the button tooltip "Runs download each part of the list at most once a day…"); What You'll Get links both FAA pages and names the expiry rule (TelephonyViewModel.cs:559-565); Preview row "Telephony data" (:133-134). |
| <a id="d2-24"></a>D2-24 | Wx Stations tab ([`docs/Users/User-Guide.md:293-300`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L293-L300)) | yes - WxStationsView.xaml:28-36; WxStationsViewModel.cs:150-155 (note "…not the cycle's FAA data. Every run downloads the latest list."), :82-83 (Preview row). |
| <a id="d2-26"></a>D2-26 | File Names tab ([`docs/Users/User-Guide.md:324-334`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L324-L334)) | yes - FileNamesView.xaml:35-113 (button tooltips, "New name, without the extension" placeholder, per-row error); FileNameRow.cs:38 ("one file per procedure, named from the FAA's data, so these keep their names"). |
| <a id="d2-27"></a>D2-27 | Preview Settings, Duplicate Alias Commands, Review ([`docs/Users/User-Guide.md:336-362`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L336-L362)) | yes - ServicePreviewTabView.xaml:54-75; AiracServiceViewModel.cs:663-674 (overwrite dialog explains both); DuplicateAliasesCard.xaml:22-44; DuplicateAliasReviewWindow.xaml:31-92; ServiceRunReviewTabView.xaml:132-207. |
| <a id="d2-28"></a>D2-28 | File Conversions intro ([`docs/Users/User-Guide.md:364-371`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L364-L371)) | yes - FileConversionsView.xaml:79-129; SourceFilesCard.xaml:48, :101-104; FileConversionsViewModel.cs:307-311; RunCard notes. |
| <a id="d2-29"></a>D2-29 | DAT to GeoJSON ([`docs/Users/User-Guide.md:373-380`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L373-L380)) | yes - DatToGeojsonView.xaml:24-62 (incl. the "Be careful:" box). |
| <a id="d2-31"></a>D2-31 | ERAM to GeoJSON ([`docs/Users/User-Guide.md:391-419`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L391-L419)) | yes - EramToGeojsonView.xaml:50-239 (layout and source tooltips incl. the DME example, run note "Everything already in ERAM_TO_GEOJSON is deleted first"); SourceFilesCard one-file wording. Only "labels ERAM keeps hidden are left out" is missing; optional one-liner in the page intro. Colours: not worth saying (always white). |
| <a id="d2-33"></a>D2-33 | Duplicate_Alias_Commands.txt ([`docs/Users/User-Guide.md:443-446`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L443-L446)) | yes - DuplicateAliasesCard.xaml:22-31; the file is self-describing. |
| <a id="d2-35"></a>D2-35 | Settings ▸ Save, Export, Import ([`docs/Users/User-Guide.md:478-484`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L478-L484)) | yes - SettingsView.xaml:38-51 (Import/Export tooltips), :224 ("Changes here are saved straight away"); ImportSettingsWindow.xaml:44-68. |
| <a id="d2-38"></a>D2-38 | Settings ▸ Default ROI, GeoJSON Files, Credentials, GitHub Requests, Updates, Main Window ([`docs/Users/User-Guide.md:495-511`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L495-L511)) | yes - SettingsViewModel.cs:284-291 (RoiExplainer), :334-342, :406-409, :517-571 (channel texts and warning); SettingsView.xaml:213-224 (Credential Manager, never exported), :302, :311-316, :369-375. |
| <a id="d2-39"></a>D2-39 | Reset / Uninstall ([`docs/Users/User-Guide.md:512-517`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L512-L517)) | yes - ResetWindow.xaml + ResetViewModel.cs:23-36; UninstallWindow.xaml + UninstallViewModel.cs:18-41. |
| <a id="d2-40"></a>D2-40 | Info ([`docs/Users/User-Guide.md:519-532`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L519-L532)) | yes - InfoViewModel.cs:34-41; AliasGuidePageView.xaml:45. |
| <a id="d2-41"></a>D2-41 | What FE-Buddy does ([`docs/Users/Getting-Started.md:3-18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/Getting-Started.md?plain=1#L3-L18)) | yes - Dashboard description (DashboardViewModel.cs:30-34); README.md:27-51 says the same with a fuller table. |
| <a id="d2-43"></a>D2-43 | First launch ([`docs/Users/Getting-Started.md:28-35`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/Getting-Started.md?plain=1#L28-L35)) | yes - ShellWindow.xaml:292-296 (AiracStatus), WelcomeWindow.xaml:30-44. |
| <a id="d2-44"></a>D2-44 | Settings - once ([`docs/Users/Getting-Started.md:37-47`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/Getting-Started.md?plain=1#L37-L47)) | yes - SettingsViewModel.cs:321-323 (RoiNotSetHint gives these exact steps incl. "press Save"), :274 output example. |
| <a id="d2-47"></a>D2-47 | Where next ([`docs/Users/Getting-Started.md:67-71`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/Getting-Started.md?plain=1#L67-L71)) | - (file is merged away). |
| <a id="d2-48"></a>D2-48 | How often do I run FE-Buddy? ([`docs/Users/FAQ-and-Troubleshooting.md:5-8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/FAQ-and-Troubleshooting.md?plain=1#L5-L8)) | yes - DashboardView.xaml:133-135 (NextCycleLine); common sense for a facility engineer. |
| <a id="d2-52"></a>D2-52 | I set FEBUDDY_GITHUB_TOKEN for FE-Buddy 2.x ([`docs/Users/FAQ-and-Troubleshooting.md:30-34`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/FAQ-and-Troubleshooting.md?plain=1#L30-L34)) | yes - the one-time notice says all of it (Wpf/ViewModels/ShellViewModel.cs:556-562). Also legacy (2.x) wording. |
| <a id="d2-53"></a>D2-53 | What if Wx Stations or Telephony can't download? ([`docs/Users/FAQ-and-Troubleshooting.md:36-41`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/FAQ-and-Troubleshooting.md?plain=1#L36-L41)) | yes - Preview rows "Station data" / "Telephony data" (WxStationsViewModel.cs:82-83, TelephonyViewModel.cs:133-134: "…uses this copy only if it can't"), VA list status/warning (TelephonyViewModel.cs:263-314), Review messages. |
| <a id="d2-54"></a>D2-54 | Same command in Departures.txt and Arrivals.txt ([`docs/Users/FAQ-and-Troubleshooting.md:43-48`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/FAQ-and-Troubleshooting.md?plain=1#L43-L48)) | yes in general - DuplicateAliasesCard.xaml:22, `Duplicate_Alias_Commands.txt`, the review window. |
| <a id="d2-56"></a>D2-56 | Next cycle says "not yet published" ([`docs/Users/FAQ-and-Troubleshooting.md:57-59`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/FAQ-and-Troubleshooting.md?plain=1#L57-L59)) | no today; covered by the tooltip proposed in [D2-7](#d2-7). |
| <a id="d2-57"></a>D2-57 | Red tab, "partial", no CRC ERAM defaults, missing file, custom alias file unreadable ([`docs/Users/FAQ-and-Troubleshooting.md:61-87`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/FAQ-and-Troubleshooting.md?plain=1#L61-L87)) | yes - AttentionCard (ServiceTabViewModel.cs:150-152); partial tooltip (AiracGeneralTabViewModel.cs:482-505); CRC card note (CrcDefaultsCard.xaml.cs:21-25) and DAT/SCT2 notes "Untick Include…" (DatToGeojsonView.xaml:35, SctToGeojsonView.xaml:33) and ERAM source tooltips; Review Advisories/Results; Add Custom Aliases Check + private/credential help + "Open the guide's troubleshooting" (ConcatenateAliasesView.xaml:165-188). |
| <a id="d2-62"></a>D2-62 | Whole file ([`docs/Users/Glossary.md:1-83`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/Glossary.md?plain=1#L1-L83)) | the FE-Buddy terms are explained where they appear: ROI (SettingsViewModel.cs:284-291), CRC ERAM defaults (CrcDefaultsCard.xaml.cs:21-25), BCG/filters/sizes (CrcDefaultsPanel.xaml:102-120), `feb.*` (FebPropertiesCard.xaml:25), `Combined_Alias.txt` (ConcatenateAliasesView.xaml:63), d-TPP Metafile (AiracGeneralTabViewModel.cs:495-496), antimeridian (ArtccBoundariesView.xaml:86), sub-service (General tab tooltips), output folder (SettingsViewModel.cs:274). The rest (AIRAC, ARTCC, SID, STAR, ODP, NAVAID, fix, METAR, NASR, GeoJSON, CRC, vNAS, video map, telephony) is everyday vocabulary for a VATSIM facility engineer. |
| <a id="d2-65"></a>D2-65 | Step 6: Save it in FE-Buddy (+ screenshot 05) ([`docs/Users/GitHub-Token-Guide.md:85-104`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/GitHub-Token-Guide.md?plain=1#L85-L104)) | yes - CredentialEditorWindow.xaml:24-58 (placeholder "e.g. ZOB GitHub", SecretHint "Once saved, the token is never shown again.", HostsDescription CredentialEditorViewModel.cs:155-157); Settings Credentials card (SettingsView.xaml:213-224); suggestion button "Use ZOB GitHub, like file 1" (AliasSourceRow.cs:322). |
| <a id="d2-68"></a>D2-68 | Keeping the token safe ([`docs/Users/GitHub-Token-Guide.md:136-140`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/GitHub-Token-Guide.md?plain=1#L136-L140)) | n/a; common sense. |

## Links into these docs that must survive the trim

Checked by a test - must keep working:
- `Links.UserGuide` → `docs/Users/User-Guide.md` (Wpf/Shell/Links.cs:14; Info card InfoViewModel.cs:38).
- `Links.FaqAndTroubleshooting` → `docs/Users/FAQ-and-Troubleshooting.md` (Links.cs:17; InfoViewModel.cs:39).
- `Links.GitHubTokenGuide` → `docs/Users/GitHub-Token-Guide.md` (Links.cs:32; CredentialEditorViewModel.cs:44, AliasSourceRow.cs:71).
- `Links.GitHubTokenGuideTroubleshooting` → `GitHub-Token-Guide.md#if-something-goes-wrong` (Links.cs:35; AliasSourceRow.cs:72). The heading `## If something goes wrong` must keep this exact text.
- FeBuddy.UnitTests/Repository/DocLinkTests.cs checks each Links.cs URL under `blob/v3-development/`: the page must exist and any `#anchor` must match a heading; `the_app_links_to_a_section_of_its_docs` fails if no Links.cs URL has a `#` (today only the token-guide anchor does).

Not tested - update by hand:
- README.md:58 (Getting-Started.md, deleted → point to User-Guide.md#install or drop), :59 (User-Guide.md), :60 (FAQ), :61 (Glossary.md, deleted), :91 `FAQ-and-Troubleshooting.md#fe-buddy-crashed` (keep the "FE-Buddy crashed" heading).
- docs/README.md:7-11 (rows for all five pages; drop two, reword User guide).
- .github/ISSUE_TEMPLATE/config.yml:4-5 (User-Guide.md + its "field by field" blurb), :7 (FAQ).
- Links between the user docs themselves: User-Guide.md:3-4, :71, :205-206, :315-318, :446, :502; FAQ:14, :48, :87; Getting-Started.md:69-71; Glossary.md:74 (`User-Guide.md#airac-service`); GitHub-Token-Guide.md:44 (`#when-the-token-expires`). All go or are rewritten with the trim.

Not affected:
- ChangeLog.md:730 → FAQ `#i-set-febuddy_github_token-for-fe-buddy-2x`, pinned to tag `3.0.0-alpha.1`.
- News.md:9 (an example inside an HTML comment), MarkdownParser.cs:32 and MarkdownView.cs:64 (XML-doc examples), MarkdownParserTests.cs:368 and InlineCodeTests.cs:67 (strings only; the file is never opened).
