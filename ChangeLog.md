# Change log

Every FE-BUDDY 3.x release, newest first. When a release is made, its section here becomes the
"Change log" part of its GitHub release notes, which is also what FE-BUDDY's update window shows.
FE-BUDDY 2.x's history is in the [2.x change log](https://github.com/Nikolai558/FE-BUDDY/blob/2.9.3/ChangeLog.md).

<!--
  Adding an entry: put one bullet under "## Unreleased" in the same pull request as the change,
  under the "### " heading it belongs to (add the heading if it isn't there yet; after a release,
  "## Unreleased" starts empty, with no headings). Headings used so far: Updates, Dashboard,
  AIRAC Service, File Conversions, Info, Settings, Installing and uninstalling, Look and feel,
  Dev notes (always last).
  Write it for users, not developers: what changed and why they care, in one line. Issue numbers
  become links ("Bug #215 - ..."). Put developer-only changes under "### Dev notes". Link
  a doc at the release's tag (blob/<version>/docs/...), never at a branch.
  Sections are separated by a "---" line, with a blank line above and below it (without the blank
  line above, Markdown turns the line before it into a heading). The release notes leave it out.
  Full guide: docs/Developers/RELEASING.md.
-->
## Unreleased

## 3.0.0-beta.1
### Installing and uninstalling
- FE-BUDDY removes an old FE-BUDDY 2.x it finds still installed (one that never moved to 2.9.x's
  installer, or that was there when you installed 3.x by hand), so you no longer end up with two
  FE-BUDDYs in Installed apps, on the Desktop and in the Start menu. It also removes a leftover
  "FE-BUDDY 2.x" entry in Installed apps that could no longer be uninstalled.

## 3.0.0-alpha.5
### Dashboard
- Feature #286 - The activity log has a **Clear** button that empties it on screen and resets its
  counts, so after several runs you can see just the next one. The log file keeps every entry.

### AIRAC Service
- Telephony ▸ **Virtual Airlines**: **Include the VATSIM-Radar Virtual Airline List** adds the
  virtual airlines VATSIM-Radar's community keeps on GitHub (about 250), each with its own `--VA--`
  card after yours; every run downloads the latest list. With it included, a virtual airline you
  add that is exactly the same as one on the list is turned away with a warning; one that differs
  at all is added, with a note, and gets a card of its own.
- The **Upload to vNAS** card now gets the order right: a ticked alias file goes into
  `vNAS_Alias.txt` ahead of your custom alias files, so yours win.

### Info
- The **Alias Command Guide** has its own Info page: read the whole guide in FE-Buddy, colours and
  all, exactly as it exports, with the export button at the top. What's New's card now points to
  it (Info ▸ Alias Command Guide) with **View the guide →**, instead of exporting the guide itself.
- The guide never wraps a command: its Syntax and Example columns are as wide as their longest
  command, a syntax is split into lines only between its parts, and the description takes the
  rest of the width. The web page, the Markdown and the app's page all match, with reworded notes
  and descriptions, a link to FE-Buddy at the top, no About section, and *Page updated on …* at
  the bottom.
- The guide is easier to scan: short points in bold with bullets under them, each example command
  on a line of its own, what each In-Scope Reference card shows as a list, and procedure names
  explained one rule to a bullet. The web page's own source is indented like an outline, so it is
  easier to edit by hand too.
- New **Export Alias Command Practice** on the Alias Command Guide page: a web page that quizzes
  your controllers on real charts, procedures, airways and ISR cards. They type the command; it
  turns green or red, says what looks missing or out of place ("Looks like you forgot to include
  the variant Y and the runway ID suffix R"), then shows the right command and how it's built.

### Dev notes
- The documentation is shorter and checked against the code: fewer pages (Settings blocks and the
  UserConfig reference are one Settings reference; the Core and app structure pages are one Code
  structure page; the release checklist is part of Releasing; MSI version numbering is part of
  Versioning), and the planning archive, the old design notes, the 2.x manual and the root
  `FE-Buddy_3.0_Whats_New.md` are gone.

---

## 3.0.0-alpha.4
### AIRAC Service
- Telephony ▸ **Virtual Airlines**: add your facility's virtual airlines (3LD, telephony and
  virtual organization) and each gets its own `.id` commands and a card marked `--VA--`, shown after
  any real operator that shares the command.

### File Conversions
- ERAM to GeoJSON: the warnings about GeoMap defaults CRC can't use now name the value (`DME`, a
  missing BCG) and say what CRC will draw instead (`vor`, BCG 1), without listing every valid style.
- ERAM to GeoJSON ▸ **From the XML, filling gaps from the card** now also replaces an XML default
  CRC can't draw (such as a `DME` symbol style) with the card's, and an element's own value CRC
  can't draw now gives way to its object's default instead of CRC's.
- ERAM to GeoJSON ▸ CRC ERAM Defaults Source: each option has a one-line description, and hovering
  over it explains in plain words what it does, with an example.

### Info
- New **What's New in v3.0?** page: what changed since FE-Buddy 2.x at a glance, and the new chart
  recall commands with their approach type codes and examples.
- **Export FE-Buddy Alias Command Guide** (on the What's New page) saves a guide to every FE-Buddy
  alias command for your controllers, with real examples, as a web page, Markdown or both, easy to
  restyle and post on your facility's website. You only pick the format and the folder.

### Settings
- GeoJSON Files ▸ Maximum Coordinate Precision has a new **Do not round** option: FE-Buddy writes
  every coordinate exactly as it is in the source data, never rounded or changed.

### Look and feel
- Names and values in the Review tab's messages (`vor`, `bcg`) show in the code font.
- Review tab: warnings and routine messages have a gap between each, and each group has a copy
  button that copies all its messages, ready to paste.
- FE-BUDDY is now set in Segoe UI everywhere, the font it has always shown. A PC with the
  Montserrat or Jost fonts installed used to show those instead in places.

---

## 3.0.0-alpha.3
### Updates
- Bug #275 - **On 3.0.0-alpha.2? You won't be offered newer alphas until you open Settings ▸
  Updates, choose Alpha and press Save** (or download the new alpha from the releases page).
  - Why: alpha.2 checks the Stable channel unless Alpha is saved, whatever you chose in 2.x, and
    pressing Save in Settings for any other reason saved Stable too.
  - From this version on, the channel follows the version you're running until you choose one,
    and Save stores the channel only when you change it.
  - If you pressed Save on alpha.2 without choosing Alpha, Stable is still saved after you update,
    so choose Alpha once more.
- Feature #266 - Your update channel now matches the version you're running until you choose one
  (an alpha is on Alpha, a beta on Beta, and so on).
  - Saving a different channel in Settings ▸ Updates checks it straight away.
  - Choosing a more stable channel while on a pre-release offers to take you back to that
    channel's latest release (**Go back now**).
- When an update is available, a red **Update available!** badge appears beside the version at
  the top of the window, taking turns with the new version's number. Click it to update. After
  **Later** it turns amber for the rest of the session. There is still no pop-up.

### AIRAC Service
- Feature #110 - Airways ▸ Buffer Airway Waypoints: choose how far lines stop short of fixes and
  of NAVAIDs (0 to 10 NM each; they start at 2.5 NM and 5 NM, as before).
- `Airports.txt`: each airport's card now ends with:
  - its attendance hours (**ATNDCE HRS**, left blank for an airport with no tower), and
  - its airspace class with the hours that airspace is in effect (**HRS**), one schedule per line.
    This used to be in the middle of the card.
- `vNAS_Alias.txt` now lists FE-BUDDY's aliases first, between a start line and an end line, and
  your custom alias files after them. CRC uses the last copy of a command, so **your commands now
  replace FE-BUDDY's** instead of the other way round.
  - Reusing a `vNAS_Alias.txt` (old layout or new) as your custom file still leaves its FE-BUDDY
    aliases out.
  - The Review tab lists the commands yours replace.
- Procedures ▸ Procedures at Any Airport: the procedure box now shows what you type or pick, and a
  new **Cancel** button clears it.
- The sub-services are in a new order, on the General tab and in the tab rail: ARTCC Boundaries,
  Airports, Airways, Arrivals, Departures, NAVAIDs, Fixes, Procedures, Telephony, Wx Stations,
  vNAS Alias Upload.
- Fixed: on the Review tab, the duplicate-alias-command check stayed on "working..." after a quick
  run had finished.
- Fixed: Airways didn't load the defaults from a hand-edited settings file with no output switched
  on (the other tabs already did).

### File Conversions
- ERAM to GeoJSON now writes the same layouts as the original ERAM_2_GEOJSON tool, with the same
  file names:
  - **By Filters** (`CENTER_CENTER-MAP\Filter_01\Filter_01_Lines.geojson`,
    `Multi-Filter_02_03_08\…`), **By Attributes** (`BCG 01_Filters 01_Type AAV_Group 64_Object
    ZOB3NM_Style Solid_Thick 1_Lines.geojson`) and **Raw** (`CENTER_CENTER-MAP.geojson`). Hover
    over a layout on the tab to see its folders.
  - These replace Object Type, Map Group, Filter Index and Similar Attributes. Saved settings that
    used them load as By Attributes or By Filters.
  - Output goes to `ERAM_TO_GEOJSON` (was `ERAM to GeoJSON\<source file>`). Each run asks before
    emptying it, and converts one Geomaps file.
  - The CRC ERAM Defaults still apply to every layout. Optional `feb.*` properties
    (`feb.mapObjectType`, `feb.lineObjectId`, `feb.symbolId`, …) replace the old tool's `E2G_*`
    ones.
  - Every label on a symbol is converted (before, only the first was), text ERAM keeps hidden is
    left out (it used to be drawn), and an element with no filters shows at every filter setting
    (filter 0).
  - `ConsoleCommandControl.txt` is back: put the export's `ConsoleCommandControl.xml` beside
    `Geomaps.xml` and each run lists the brightness and filter menus - each button's label,
    position and groups, and which maps use each menu - as the old tool did.
- Bug #151 - Lines in sector files and ERAM GeoMaps that are written backwards (each segment ends
  where the one before it starts) are now joined into whole lines instead of one line per segment.
  The files are smaller, and dashed lines no longer restart their pattern at every segment.

### Settings
- **Reset FE-Buddy** starts over as if FE-BUDDY had just been installed. It deletes the downloaded
  AIRAC, Telephony and Wx Station data, logs and settings backups, and - if you choose - your
  settings (offering to save a copy first) and saved credentials, then restarts.
- GeoJSON Files no longer has an FE-Buddy Properties section: choose them on each tab's FE-Buddy
  Properties card instead.

### Installing and uninstalling
- Feature #267 - **Uninstall FE-Buddy…**, on Settings' Reset FE-Buddy card, removes FE-BUDDY the
  same way Windows does from Installed apps.
  - It first tells you what goes (the program, its data, your settings and saved credentials) and
    what stays (your output folder), and offers to save a copy of your settings.
  - If Windows can't remove a saved credential, it tells you how to delete it yourself.
- Uninstalling now also removes the install folder when an early 2.x build left its
  `FE-BUDDY_LOG.txt` there.
- If you first installed FE-BUDDY 2.8 or earlier, the extra, blank FE-BUDDY shortcut on your
  Desktop and in your Start menu is now deleted when FE-BUDDY starts. It pointed at the old copy
  of FE-BUDDY, which the move to the installer removed.
- Crash reports are now saved with the logs, in `%APPDATA%\FE-Buddy\Logs\febuddy-wpf-crash.txt`,
  instead of in `%TEMP%`, so uninstalling removes them. FE-BUDDY deletes the old `%TEMP%` copy the
  next time it starts.

### Look and feel
- Folder names and paths in descriptions and messages (`Upload_to_vNAS`, `Aliases`, the output
  folder in Settings, …) now show in a code font, like `code` in News posts.
- The Outputs cards' tick box now reads just **Alias file**, and the "At least one output must stay
  on" note is gone (FE-BUDDY still stops you turning off the last output, and says why).
- CRC ERAM Defaults card: each file's panel is darker than the card, files with several types (the
  NAVAID types, Airways High / Low / Other) have a box for each type, and panels side by side line
  up.
- Airways ▸ High and Low Files: each drop-down sits closer to its airway type.
- File Names: the new-name box sits to the right of its file, and moves under it only when the
  window is too narrow.
- Scroll bars can be grabbed anywhere across their width (before, only a sliver at the left edge
  worked).
- Fixed: tick boxes and options hid the first underscore of a file name (`Fix_Symbols.geojson`
  showed as `FixSymbols.geojson`).

### Dev notes
- Every file is UTF-8 without a byte-order mark, and a unit test now fails on any file that has one.

---

## 3.0.0-alpha.2
- Bug #251 - Fixed Airways losing its High and Low Files choices when you switched to another tab,
  which left the tab flagged with errors that you couldn't clear by saving again.
- Bug #256 - Links in News posts on the Dashboard can now be clicked, and posts show their formatting.
- File and folder pickers (Map ▸ Load GeoJSON, File Conversions, Settings, custom alias files) now
  open in your output folder, or on the Desktop if you haven't set one (#253).
- The Info screen's Manual link is now **User Guide** and opens the 3.0 guide. A new
  **FAQ / Troubleshooting** link opens the FAQ (#254).
- Bug #259 - News posts on the Dashboard no longer show their title twice.
- Bug #258 - Fixed a News post not showing when its PostId was invalid.
- (Dev notes)
  - News moved to `News.md` at the repository root (#260). `FeBuddy/FeBuddy.Core/News.md` now holds
    only a final "please update" post for 3.0.0-alpha.1, which still reads that path.
  - A News post with a missing or invalid PostId is logged as skipped instead of disappearing
    silently, and a test now fails the build if any post in `News.md` has one.

---

## 3.0.0-alpha.1
- FE-BUDDY 3.0 is a from-scratch rewrite with a new interface. This first alpha is for testers:
  expect rough edges, and keep 2.x handy.

### Output
- Everything for a cycle goes into one `AIRAC_<cycle>` folder: `Aliases`, `Geojson`,
  `Publication_Docs` and `Upload_to_vNAS`. File names say what they hold, e.g.
  `Airports_Symbols.geojson`, `Airports.txt` (was `ISR_APT.txt`), `Navaids.txt` (was `ISR_NAVAID.txt`).
- Every output has options: pick which files to write, how to split them, and what goes in them.
- The File Names tab lists every file a run will write, by folder, and lets you give any of them
  your own name (the Departures and Arrivals per-procedure files keep theirs). A file a changed
  setting adds is flagged until you name it or choose to keep FE-BUDDY's name.
- Maps can be limited to a Region of Interest (draw it on the Map).
- Mark any file for vNAS and it goes into `Upload_to_vNAS`, with CRC-ERAM style defaults you set
  (brightness group, filters, line style, symbol, text size). Marked alias files and your own custom alias
  files are combined into one `vNAS_Alias.txt`, each under a heading naming where it came from.
- Coordinates are rounded rather than cut off, to 6 decimals by default (adjustable). Features can
  also carry extra information for your own tools (`feb.*` properties) if you turn it on.

### Aliases
- Airport (`.aptLAX`), NAVAID (`.navABQ`) and telephony (`.idAAL`) commands show a labelled,
  multi-line card. Airports add the facility type, longest runway, elevation, pattern altitude,
  airspace class, FSS, CTAF and weather frequency.
- Chart recall commands use the full runway (`.laxI24Lc` for RWY 24L; 2.x wrote `.LAXI4LC`, so
  different runways could share one command), and visual approaches are spelled out per runway
  (`.sfovQUIETBRIDGE28Rc`).
- An identifier used by more than one NAVAID or airline is one command showing every match,
  instead of duplicate commands. The duplicate-command report lists each duplicate's file and ARTCC.
- NAVAIDs cover every FAA type, including VOTs, fan markers and marine NDBs (untick the ones you
  don't want), and leave out NAVAIDs the FAA lists as shut down.
- Telephony adds U.S. special call signs (e.g. `.idNASA`).
- Departure and arrival fix commands are split into `Departures.txt` and `Arrivals.txt`, and each
  airport's list has only the fixes of procedures that serve it.
- The airway alias file can cover every FAA airway or only those in your Region of Interest.

### Video maps (GeoJSON)
- Airways: one symbol and one label per point, instead of one per airway where airways share a
  point (on a low-altitude map, about half the labels were stacked duplicates).
  - Split them High / Low, choosing which file each airway type goes in (High, Low or Both; J and
    Q start in High, V and T in Low), or one file per designation (J, V, Q, T, ...), and leave out
    designations you don't need.
  - The DME-cutoff airway files are replaced by the Buffer Airway Waypoints option: lines stop short
    of each waypoint so they don't run through the symbols.
- Runways: draws runways 2.x left out (about a third more in a typical area), including water
  runways.
- Airport labels show the FAA ID (`BFL`) instead of the ICAO ID (`KBFL`), and names with `&` are
  no longer written as `&amp;`.
- NAVAIDs: one map for every type, or one map per type, replacing the separate VOR and NDB maps.
- Fixes: one map, or split by fix use (waypoint, reporting point, VFR waypoint, ...), by chart,
  or by chosen chart + fix use combinations.
- ARTCC boundaries: split High / Low, High / Low / Unlimited, or one file per ARTCC and altitude
  (so your own ARTCC can be styled apart from its neighbours), for every ARTCC or only the ones
  you pick.
- Wx station labels no longer end in a stray space.

### SIDs and STARs
- Each airport's procedures are in their own folder (`Geojson\<ARTCC>\<airport>\`), and each
  procedure can have lines, fix symbols and fix labels (2.x drew lines only).
- Pick which procedures to write: by ARTCC, by Region of Interest, and only those amended recently
  if you like. Obstacle departures are optional.
- Each segment is drawn once. 2.x repeated segments that transitions share, drawing about half
  again as many lines.
- Each airport's STAR map has only its own runway transitions (2.x also drew other airports',
  e.g. SNA's DSNEE drew LGB's).

### Procedure changes
- One `Procedure_Changes.md` for all your facilities, with New, Changed and Deleted sections and a
  link to every chart (and to the previous cycle's chart for deletions). A STAR serving several
  airports is listed once, and continuation pages are folded into their chart.
- Choose what it covers: whole facilities, airports in your Region of Interest, chosen airports or
  single procedures, and which chart types (minimums, hot spots and LAHSO charts are off by default).
- Catches deleted charts 2.x missed.
- `Procedures.json` has the same information for other tools, with the fields you choose.

### Everything else
- File Conversions: DAT, SCT2 and vERAM files to GeoJSON.
- Map: check GeoJSON files, draw and edit your Region of Interest, and view the AIRAC layers.
- Settings: import and export your settings, pick your update channel, and store a GitHub token
  securely in Windows Credential Manager (with a step-by-step token guide).
- If you set `FEBUDDY_GITHUB_TOKEN` for FE-BUDDY 2.x, delete it: 3.0 doesn't use it, and Windows
  keeps it as plain text. FE-BUDDY tells you once if it is still set
  ([how to delete it](https://github.com/Nikolai558/FE-BUDDY/blob/3.0.0-alpha.1/docs/Users/FAQ-and-Troubleshooting.md#i-set-febuddy_github_token-for-fe-buddy-2x)).
- The update window shows the notes for every release you are missing, then downloads and runs
  the installer for you.
- Installs with the same Windows Installer (MSI) as 2.9 and upgrades an existing 2.9 install in place.
- (Dev notes)
  - Rewritten in WPF on .NET 10, with the logic in a separate, unit-tested FeBuddy.Core library.
  - Semantic versioning (alpha, beta, rc) shared by the app and the installer.
  - Automated release pipeline: pre-flight checks on the pull request, then a drafted release.

---