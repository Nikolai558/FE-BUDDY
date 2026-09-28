# Change log

Every FE-BUDDY 3.x release, newest first. When a release is made, its section here becomes the
"Change log" part of its GitHub release notes, which is also what FE-BUDDY's update window shows.
FE-BUDDY 2.x's history is in the [2.x change log](https://github.com/Nikolai558/FE-BUDDY/blob/2.9.3/ChangeLog.md).

<!--
  Adding an entry: put one bullet under "## Unreleased" in the same pull request as the change.
  Write it for users, not developers: what changed and why they care, in one line. Issue numbers
  become links ("Bug #215 - ..."). Put developer-only changes under a "(Dev notes)" bullet. Link
  a doc at the release's tag (blob/<version>/docs/...), never at a branch.
  Full guide: docs/Developers/RELEASING.md.
-->

## Unreleased
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
