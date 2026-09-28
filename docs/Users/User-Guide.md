# User guide

Every screen and every option in FE-Buddy 3.0. New to FE-Buddy? Start with
[Getting started](Getting-Started.md); unfamiliar words are in the [glossary](Glossary.md).

**Contents:** [The window](#the-window) · [Dashboard](#dashboard) ·
[AIRAC Service](#airac-service) · [File Conversions](#file-conversions) ·
[Output files](#output-files) · [Map](#map) · [Settings](#settings) · [Info](#info) · [Updating FE-Buddy](#updating-fe-buddy)

---

## The window

- **Menu (left).** *Workspace*: Dashboard, AIRAC Service, File Conversions, Map. *System*:
  Settings, Info. The arrow collapses the menu to icons.
- **Systems (bottom of the menu).** One line each for **Internet**, **AIRAC data** and
  **Updates**, with a coloured dot: green is fine, amber needs a look or is still working, red is
  not working. **Re-check** runs the checks again. When the menu is collapsed, just the dot shows.
- **Top of the window.** On the left, **FE-BUDDY** and the version: hover it for the update
  status, and it becomes a button when an update is available. In the middle, the **AIRAC
  status**: it narrates the downloads at launch, then shows the current cycle and its effective
  date.
- **Bottom of the window.** The time in Zulu.
- **Pop-up messages** appear bottom-right and fade after a few seconds.

## Dashboard

- **Description box** (top) - what FE-Buddy is, a link to the Discord, and the **next AIRAC cycle**
  with how many days until it takes effect.
- **News** - posts from the FE-Buddy team. The News button is highlighted when there is a post
  you have not seen; opening News marks them read.
- **Activity log** - what FE-Buddy has been doing this session: downloads, runs, anything that
  went wrong. The chips (All, Info, Success, Warning, Error) filter it and show a count each. It
  starts collapsed; picking a chip opens it.

## AIRAC Service

The screen where you make files. It is a set of tabs down the left:

| Tab | What it is |
|---|---|
| **General** | Which cycle, and which sub-services (Airports, Airways, Departures, Arrivals, NAVAIDs, ARTCC Boundaries, Fixes, Wx Stations, Procedures, Telephony, vNAS Alias Upload). Always there. |
| **Airports / Airways / Departures / Arrivals / NAVAIDs / ARTCC Boundaries / Fixes / Wx Stations / Procedures / Telephony / vNAS Alias Upload** | One tab per sub-service you ticked, with its settings. |
| **Preview Settings** | Everything the run will do, in plain words, and the **Run AIRAC Service** button. Appears once a sub-service is ticked. |
| **Review** | What happened in the last run. Appears once you run. |

The screen is unavailable until the AIRAC data has finished downloading; it says so while you wait.

### Saving and the action bar

Every tab saves separately. The bar above (and again below) each tab has:

- **Previous / Next / Preview settings** - move between tabs. If the tab has unsaved edits you are
  asked to save them first.
- **Save** - saves this tab.
- **Undo all changes** - throws away this tab's unsaved edits.
- **Undo last save** - steps this tab's saved settings back to the save before.

A tab's name in the rail turns **amber** when it has unsaved edits and **red** when something is
missing or invalid. A red box inside the tab shows exactly which field; hover it for the reason.
Changing a value and changing it back clears the amber - "unsaved" means "different from what
is saved".

### General tab

- **Cycle** - **Previous**, **Current** or **Next**, each with its cycle number, effective date and
  state: *ready*, *downloading…*, *parsing…*, *not yet published* (the FAA releases the next
  cycle's data a few weeks early, so it may not exist yet) or *failed*.
- **Sub-Services** - tick what you want this cycle to produce. Each ticked item opens its own tab.
  Unticking closes the tab but keeps its saved settings for next time.

### The cards every sub-service tab shares

Procedures and Telephony do not use the What Files, FE-Buddy Properties or CRC ERAM Defaults cards
below at all - neither writes any GeoJSON. Procedures' own Outputs card covers its two documents and
its alias file instead of the generic one described here, and its own Upload to vNAS card offers the
alias file only; it does share the Region of Interest card, though the box means something different
there. Telephony's own Outputs card just names its one output, `Telephony.txt`, with nothing to turn
on or off, and it has no Region of Interest card at all - it covers every operator regardless of
area. vNAS Alias Upload uses none of these cards. See each one's own tab, further down.

**Outputs** - what the sub-service writes: **GeoJSON files** and/or the **alias file**. At least
one must stay on; to make nothing for a sub-service, untick it on the General tab instead. ARTCC
Boundaries, Fixes and Wx Stations have no Outputs card: none has an alias file, and all three
always write GeoJSON.

**What Files Do You Want?** - the three GeoJSON files each sub-service can write:

| File | Holds |
|---|---|
| **Lines** | The lines (runways, airways, procedure routes). |
| **Symbols** | A symbol at each point (airport, waypoint, fix). |
| **Text** | A label at each point. |

**FE-Buddy Properties** - extra fields written into each GeoJSON feature, all starting with
`feb.` (for example `feb.awyId`). They make files bigger, but help when checking a file in a
GeoJSON viewer. Tick **Include FE-Buddy Properties** and then the ones you want; each has a tooltip
saying what it holds.

**Region of Interest** - by default a run uses the **Default Region of Interest** from Settings.
Tick **Override the default ROI for <sub-service>** to give it its own box: type the four corners or
press **Pick on map…**. The note under the checkbox says which region applies when the override
is off. With no region at all, the run covers the whole country.

**Upload to vNAS** - near the end of the tab, once the files are set up: a box for every file the
tab's settings will write. Tick the ones you will upload to vNAS. A ticked GeoJSON file is written
to the cycle's `Upload_to_vNAS` folder instead of the usual one (see [Output files](#output-files)),
so it is ready to upload. vNAS takes only one alias file, so a ticked alias file stays in `Aliases`
and is also added to `Upload_to_vNAS\vNAS_Alias.txt` - under your own custom alias files when
[vNAS Alias Upload](#vnas-alias-upload-tab) is ticked. Once a GeoJSON file is ticked, a follow-up question asks whether those files get
**CRC-ERAM Default Properties**: *No CRC-ERAM defaults*, *Every GeoJSON file going to vNAS*, or
*Specific files* (then tick which).

**CRC ERAM Defaults** - how CRC should draw a file, written as one hidden "defaults" feature at
the top of it. Only files going to vNAS get them - CRC reads its maps from vNAS - so this card
appears only once the Upload to vNAS card has files chosen for CRC-ERAM defaults, and shows only
the panels and columns those files need. FE-Buddy never guesses these values: every box shown must
be filled in.

| Field | Values | Used on |
|---|---|---|
| BCG | 1-40 | all |
| Filters | one or more of 0-40, picked from a list | all |
| Style | Lines: solid, shortDashed, longDashed, longDashShortDash. Symbols: a CRC symbol name (vor, ndb, airport, rnav, …) | Lines, Symbols |
| Thickness | 1-3 | Lines |
| Size | Symbols 1-4, Text 0-5 | Symbols, Text |
| Underline, Opaque | Yes / No | Text |
| X offset, Y offset | any whole number | Text |

Airways has a column per altitude class (High, Low, Other) - with High/Low files, just the class of
each file chosen; with designation files, all three, since one file can hold airways of every
class. Airports, Departures and Arrivals have one. NAVAIDs has one column with *All in one file*,
or one column per NAVAID type - style included - with *one pair per NAVAID type* (see the NAVAIDs
tab). ARTCC Boundaries has a column per class - High and Low, or High, Low and Unlimited - or,
with *one file per ARTCC and altitude*, one column per ARTCC and altitude (`ZOB-HIGH`, `ZOB-LOW`,
…), so your own ARTCC can be styled apart from its neighbours (see the ARTCC Boundaries tab). Fixes
has one column for *All*, or one column per fix use, chart, or chart + fix use combination present,
depending on its File Layout (see the Fixes tab). Wx Stations has one column, since there is only
the one class. Each class is its own block of boxes; blocks that
do not fit across the window move to the next line, so every box stays on screen however narrow the
window is.

### Airports tab

- **Outputs:** GeoJSON, and `Airports.txt` - one alias command per airport's FAA ID, and one per
  ICAO ID where there is one. The alias file always covers every open airport; the region only
  limits the GeoJSON.
- **Files:** *Runways - Lines* (each airport's runway centrelines), *Airports - Symbols* (one per
  airport, at its reference point), *Airports - Text* (FAA ID and name).
- **FE-Buddy properties:** `faaId`, `icaoId`, `name`, `elev`, `respArtcc`, `tfcPtrnAlt`, `fssId`,
  `twrType`, `rwyId`.
- **Region:** an airport is included when its reference point is inside the region.
- **Upload to vNAS:** each of the four files on its own - Runways Lines, Airports Symbols,
  Airports Text, `Airports.txt`.

### Airways tab

- **GeoJSON files** - how airways are split into files:
  - **HighLow** - `Airways_High`, `Airways_Low`, `Airways_Other`, by the highest published
    altitude on the airway: 18,000 ft or more is High, below that is Low, none is Other. An
    airway is never split between files.
  - **Designation** - one set per designation taken from the airway ID: `Airways_J`,
    `Airways_V`, `Airways_Q`, …
  - **None** - no GeoJSON (the alias file only).
- **Alias file** `Airways.txt` - a command per airway that draws its fixes on an ERAM or STARS
  window, e.g. `.J3F`. Choose **All FAA airways** or **ROI airways only** (the airways whose
  line crosses the region).
- **Designations to Include** - one checkbox per designation in the chosen cycle. Untick one to
  leave it out of everything. The list appears once the cycle's data is loaded.
- **Buffer Airway Waypoints** - lines stop a short distance before each waypoint (2.5 NM at a
  five-letter fix, 5 NM elsewhere) so they do not run through the symbols.
- **Split GeoJSON at the Antimeridian** - a line that crosses ±180° longitude is split in two so
  it does not wrap across the whole map. Leave it on.
- **FE-Buddy properties:** `awyId`, `pointId`, `waypoints`.
- **Region:** an airway is included when its line crosses the region, and its GeoJSON is clipped
  to the region.
- **Upload to vNAS:** a row per file group - High, Low and Other, or each included designation -
  with its Lines, Symbols and Text files, then `Airways.txt`. With **Designation**, the rows appear
  once the cycle's data is loaded.
- An airway with a waypoint FE-Buddy cannot locate is left out entirely (a half-drawn airway is
  worse than none); the Review tab says which and why.

### Departures tab

- **Outputs:** GeoJSON, and `Departures.txt` - a command per airport and procedure that draws the
  procedure's points.
- **Files:** Lines (each airport's procedure, shared segments drawn once), Symbols (each point),
  Text (each point's identifier). They are written per airport, in the cycle's GeoJSON folder:
  `Geojson\<ARTCC>\<airport>\<airport>_<procedure>_Lines.geojson` (and `_Symbols`, `_Text`).
- **Procedures:**
  - **Include obstacle departures (ODPs)** - on by default; off gives SIDs only.
  - **ARTCCs** - tick the ARTCCs you want; none ticked means all. **Clear** unticks them all.
  - **Amendment Date** - keep every procedure, or only those amended within the last *N* cycles
    (1 = this cycle), within the last *N* days, or on or after a date.
- **How the Region Selects Departures:** *every departure for an airport inside the region*, or
  *any departure with a point inside the region*. The region limits both the GeoJSON and the alias
  file.
- **FE-Buddy properties:** `dpName`, `pointId`, `arptId`, `artcc`, `amendmentNo`, `amendEffDate`,
  `waypoints`.
- **Upload to vNAS:** by kind, since a run writes thousands of files - every procedure's Lines,
  Symbols or Text files - then `Departures.txt`. The `<ARTCC>\<airport>` folders are kept under
  `Upload_to_vNAS\Geojson` too.
- Procedures are named by their FAA computer code without the amendment digit (`DOTSS2.DOTSS` is
  `DOTSS`), or by their name with punctuation removed when there is no code (`O'HARE` is `OHARE`).

### Arrivals tab

The same shape as Departures, for STARs instead of SIDs, with no obstacle/SID split - so there is
no "Include obstacle departures" equivalent here.

- **Outputs:** GeoJSON, and `Arrivals.txt` - a command per airport and procedure that draws the
  procedure's points.
- **Files:** Lines (each airport's procedure, shared segments drawn once), Symbols (each point),
  Text (each point's identifier). They are written per airport, in the same
  `Geojson\<ARTCC>\<airport>\` folder as that airport's Departures files, but named with `STAR` in
  them - `<airport>_<procedure>_STAR_Lines.geojson` (and `_Symbols`, `_Text`) - so a SID and a STAR
  that share an identifier at one airport never overwrite each other.
- **Procedures:**
  - **ARTCCs** - tick the ARTCCs you want; none ticked means all. **Clear** unticks them all. A
    STAR shared by two ARTCCs (rare) follows each airport's own ARTCC: ARLFT serves airports in
    both ZDC and ZNY, so ticking only ZNY gives you ARLFT at its ZNY airport and nowhere else.
  - **Amendment Date** - keep every procedure, or only those amended within the last *N* cycles
    (1 = this cycle), within the last *N* days, or on or after a date.
- **How the Region Selects Arrivals:** *every arrival for an airport inside the region*, or *any
  arrival with a point inside the region*. The region limits both the GeoJSON and the alias file.
- **FE-Buddy properties:** `arrivalName`, `pointId`, `arptId`, `artcc`, `amendmentNo`,
  `amendEffDate`, `waypoints`.
- **Upload to vNAS:** by kind, since a run writes thousands of files - every procedure's Lines,
  Symbols or Text files - then `Arrivals.txt`. The `<ARTCC>\<airport>` folders are kept under
  `Upload_to_vNAS\Geojson` too.
- Procedures are named by their FAA computer code without the amendment digit, read in the
  opposite order from a departure's code (`AALAN.BLAID2` is `BLAID`), or by their name with
  punctuation removed when there is no code.
- A STAR is flown transition to body, so its points - and the alias command's fix list - list
  transitions first, then bodies (the reverse of a departure's order).

### NAVAIDs tab

- **Outputs:** GeoJSON, and `Navaids.txt` - a `.nav<ID>` command per NAVAID identifier and a
  `.nav<name>` command per name (letters and digits only), each an `.echo` that prints the
  NAVAID's identifier, name, type, frequency, and its ARTCC high and low boundaries. Duplicate
  identifiers are normal in NASR data (`ABQ` is both a VORTAC and a VOT; `AA` is two NDBs) - a
  command shared by several NAVAIDs is written once, listing each of them in turn. The alias file
  always covers every ticked-type NAVAID; the region only limits the GeoJSON.
- **NAVAID Types** - a tick box per NAVAID type found in the cycle (VOR, VORTAC, VOR/DME, VOT,
  TACAN, DME, NDB, FAN MARKER and so on), all ticked by default. Unticking a type leaves it out of
  the GeoJSON *and* the alias file. NAVAIDs NASR marks SHUTDOWN are always left out.
- **File Layout** - *All in one file* (`NAVAIDs_Symbols`, `NAVAIDs_Text`) or *one pair per NAVAID
  type* (`NAVAIDs_<Type>s_Symbols`, `NAVAIDs_<Type>s_Text` - e.g. `NAVAIDs_VORTACs_Symbols.geojson`,
  `NAVAIDs_VOR-DMEs_Text.geojson`; a `/` or space in the type becomes a `-`). Files go straight in
  the Geojson folder - there are no per-airport sub-folders.
- **Files:** *Symbols* (one per NAVAID, at its published coordinates), *Text* (its identifier, then
  its name and type, e.g. `CGT` and `CHICAGO HEIGHTS VORTAC`). There is no Lines file.
- **NAVAID Symbol Style** - appears once the Symbols file gets CRC-ERAM defaults, but only with
  *All in one file* (with *one pair per type*, each type's own file already draws one kind of
  NAVAID, styled from its own CRC ERAM Defaults column). Choose **Style each NAVAID by its type** -
  VOR, VORTAC, VOR/DME and VOT draw as `vor`; TACAN and DME as `tacan`; NDB, NDB/DME, MARINE NDB,
  MARINE NDB/DME and UHF/NDB as `ndb`; fan markers use the **Fan marker style** dropdown, shown
  once fan markers are ticked; CONSOLAN has no style and gets a warning - or **One style for the
  whole file**, set on the CRC ERAM Defaults card. With **Style each NAVAID by its type**, the
  NAVAIDs Symbols defaults have no style box of their own.
- **FE-Buddy properties:** `navId`, `navType`, `name`, `freq`, `lowAltArtccId`, `highAltArtccId`.
  The Text file never carries `navId`, `navType` or `name` - its label already shows all three.
- **Region:** a NAVAID is included in the GeoJSON when its own coordinates are inside the region;
  the alias file is never limited by it, the same as Airports.
- **Upload to vNAS:** *All in one file* - `NAVAIDs_Symbols`, `NAVAIDs_Text`, then `Navaids.txt`.
  *One pair per type* - each included type's `NAVAIDs_<Type>s_Symbols` / `_Text`, then
  `Navaids.txt`.

### ARTCC Boundaries tab

- **Outputs:** GeoJSON Lines only, always written - there is no alias file and no Symbols or Text
  file, so the tab has no Outputs or file-choice card: every ring is drawn as a line.
- **ARTCCs** - a tick box per ARTCC with boundary data in the cycle, none ticked means all - the
  same as the Departures and Arrivals ARTCC filter. NASR also publishes Canadian, foreign and
  CERAP entries with no boundary lines of their own, so they are not offered.
- **File Layout** - **High and Low** (the default) - `ARTCC-Boundary_High_Lines` and
  `ARTCC-Boundary_Low_Lines`; an UNLIMITED ring is written into both. **High, Low and Unlimited** -
  adds `ARTCC-Boundary_Unlimited_Lines`; each of the three files then holds one altitude only.
  **One file per ARTCC and altitude** - `ARTCC-Boundary_<ARTCC>-<altitude>_Lines`, e.g.
  `ARTCC-Boundary_ZOB-HIGH_Lines`, so your own ARTCC can be styled apart from its neighbours. Files
  go straight in the Geojson folder - there are no per-airport sub-folders.
- **Files:** *Lines* only - one closed line per boundary ring, running through its points in
  published order back to its start. ZAK, ZAP and ZWY each have two rings sharing an altitude - a
  CTA ring and a FIR ring.
- **Split GeoJSON at the Antimeridian** - on by default, the same as Airways: ZAK, ZAN, ZAP and the
  oceanic part of ZOA cross ±180° longitude, so a ring that does becomes a MultiLineString instead
  of running off the edge of the map.
- **FE-Buddy properties:** `locationId`, `locationName`, `locationType`, `icaoId`, `computerId`,
  `altitude` (`HIGH`, `LOW` or `UNLIMITED`), `type` (`ARTCC`, `CTA`, `FIR`, `CTA/FIR` or `UTA` -
  tells the overlapping oceanic CTA and FIR rings apart), `city`, `countryCode`.
- **Region:** a ring is clipped at the region's edge, the same as Airways; a ring entirely outside
  the region is left out.
- **Upload to vNAS:** *High and Low* - `ARTCC-Boundary_High_Lines`, `ARTCC-Boundary_Low_Lines`.
  *High, Low and Unlimited* - adds `ARTCC-Boundary_Unlimited_Lines`. *One file per ARTCC and
  altitude* - each ARTCC-and-altitude file. There is no alias file to upload.

### Fixes tab

- **Outputs:** GeoJSON Symbols and Text only, always written - there is no alias file, so the tab
  has no Outputs card. Tick **Symbols** and/or **Text** under GeoJSON files; at least one must
  stay on.
- **Files:** *Symbols* (one point per fix, styled from the file's CRC ERAM defaults), *Text*
  (each fix's identifier). There is no Lines file.
- **File Layout:**
  - **All fixes in one file** (the default) - `Fix_Symbols`, `Fix_Text`.
  - **One file per fix use** - one pair per fix use present in the cycle, e.g. `Fix_WYPNT_Symbols` /
    `_Text`. A **Fix Uses** tick-list appears, all ticked by default; untick one to leave it out.
  - **One file per chart** - one pair per NASR chart present, e.g. `Fix_ENROUTE-LOW_Symbols` / `_Text` (a
    run of spaces or punctuation in the chart's name becomes a hyphen). A fix shown on several
    charts goes into each of their files; one shown on none goes into `Fix_NO-CHART_Symbols` /
    `_Text`. A **Charts** tick-list appears, all ticked by default; untick one to leave it out.
  - **One file per chart + fix use combination** - one pair per chart + fix use combination you list, e.g.
    `Fix_ENROUTE-LOW-WYPNT_Symbols` / `_Text`; a fix needs both the chart and the fix use to be
    included. A **Combinations** card appears: **Add combination** picks a chart and a fix use
    from two drop-downs and adds the pair; each listed combination has **Edit** and **Delete**. A
    combination that matches no fix in the cycle gives a warning and writes no files.

  Files go straight in the Geojson folder - there are no per-airport sub-folders.
- **Fix use names:** COMPUTER-NAV, MIL-RPRTNG-PNT, MIL-WYPNT, NRS-WYPNT, RADAR, RPRTNG-PNT,
  VFR-WYPNT and WYPNT, mapped from NASR's raw code (`CN`, `MR`, `MW`, `NRS`, `RADAR`, `RP`, `VFR`,
  `WP`). Any other code NASR publishes is kept as written - characters not allowed in a file name
  become spaces, and a blank code becomes `UNKNOWN`.
- **FE-Buddy properties:** `fixId`, `fixUseCode` (the fix use name, e.g. `WYPNT`), `charts` (the
  NASR chart names the fix is depicted on, left out for a fix on no charts). The Text file never
  carries `fixId` - its label is always the fix's own identifier.
- **Region:** a fix is included when its own coordinates are inside the region.
- **Upload to vNAS:** a box per file the chosen File Layout writes - `Fix_Symbols` / `Fix_Text`
  for *All fixes in one file*, or each group's pair otherwise. There is no alias file to upload.

### Wx Stations tab

- **Outputs:** GeoJSON Symbols and Text only, always written - there is no alias file, so the tab
  has no Outputs card. Tick **Symbols** and/or **Text** under GeoJSON files; at least one must
  stay on.
- **Files:** *Symbols* (one point per station, styled from the file's CRC ERAM defaults), *Text*
  (the station's ICAO ID, then its IATA ID and site name where it has one, e.g. `KDTW` /
  `DTW_Detroit/Metro Wayne Cnty`, otherwise just the site name). There is no Lines file.
- **Station Data** - where the list comes from: aviationweather.gov's own station list, not the
  NASR cycle. Every run downloads the latest list first, whichever cycle you run, and only falls
  back to FE-Buddy's kept copy if that fails. It shows which stations are included (a US or
  US-territory station with an ICAO ID that reports METAR and has usable coordinates) and the kept
  copy's date, or that FE-Buddy has no copy yet - the first run then needs an internet connection.
- **Region:** a station is included when its own coordinates are inside the region.
- **Upload to vNAS:** `Wx_Symbols` and/or `Wx_Text`, whichever the tab writes. There is no alias
  file to upload.

### Procedures tab

Builds from the FAA's **d-TPP Metafile** - the index behind the Digital Terminal Procedures
Publication (approach plates, SIDs, STARs, airport diagrams and the rest) - instead of the NASR
cycle. The FAA posts a cycle's copy only 15-18 days before its effective date, so the next cycle's
is often not there yet; that never blocks a run; it just means Procedures has nothing to build from
until it is.

- **Outputs:** **Procedure Changes document** (`Procedure_Changes.md`) and/or **Procedures.json**,
  written into the cycle's `Publication_Docs` folder, and/or the **alias file**
  (`Faa_Chart_Recall.txt`, see [FAA Chart Recall commands](#faa-chart-recall-commands) below) -
  there is no GeoJSON. At least one of the three must stay on. The Facilities, Airports, Procedures
  at Any Airport, Airport + Procedure and Chart Types cards below (and the Region of Interest card,
  used as below) only pick what the two documents cover and grey out while neither is on - the
  alias file always covers every current chart at every airport in the metafile, whatever they say.
- **d-TPP Data** - where the data comes from, and the selected cycle's status: how many airports and
  procedures its d-TPP Metafile has and when it was downloaded, or **Not published yet**. It also
  says whether a deleted procedure can be linked to its last chart, which needs the previous cycle's
  metafile too. Like the Wx Stations tab's Station Data card, this never blocks the run - a cycle
  with no metafile yet still runs, it just writes no Procedures output this time, and the Review
  tab carries an advisory saying why.
- **Facilities** - tick the ARTCCs to include every one of their airports' procedures (of the chart
  types below) - the same idea as the ARTCC filter on Departures and Arrivals. Your Settings ▸
  Facility Profile facility, if it ends up with any included airports, leads both documents; the
  rest of the facilities follow alphabetically, and airports with no responsible ARTCC land in a
  final "Other" section.
- **Airports** - list airports by FAA or ICAO ID to include every one of their procedures, plus
  **Also include every airport inside the region of interest**, which pulls in every airport whose
  NASR coordinates fall inside the box on the Region of Interest card below.
- **Procedures at Any Airport** - name a procedure - a STAR flown into several airports, say - to
  include it wherever the FAA charts it, whatever its chart type and whether or not that airport is
  otherwise included.
- **Airport + Procedure** - specific airport-and-procedure pairs, for one chart at one airport and
  nothing else there; **Add**, **Edit** and **Delete** manage the list.
- **Chart Types** - which kinds of chart a whole included airport (from Facilities, the region, or
  Airports above) contributes: approach plates (IAP), STARs (STR), departure procedures (DP),
  obstacle departures (ODP), RNAV DP AAUPs (DAU) and airport diagrams (APD) are on by default; the
  volume-wide takeoff/alternate/radar minimums (MIN), hot spot (HOT) and LAHSO (LAH) sheets are off
  by default. A procedure named under Procedures at Any Airport or Airport + Procedure is always
  included, whatever its chart type.
- **Procedures.json Fields** - which extra fields the JSON document carries, shown only while
  **Procedures.json** is on. The airport's ID and a procedure's name are always written; everything
  here is optional on top of that.
- **Region of Interest** - the same Default Region of Interest as every other sub-service, or your
  own box with **Override the default ROI for Procedures**. Unlike everywhere else it appears, it
  never clips a file - Procedures writes no GeoJSON - it only decides which airports **Also include
  every airport inside the region of interest** (above) adds.
- **Upload to vNAS** - the alias file only, shown once **Alias file** is ticked on the Outputs card
  above.

**`Procedure_Changes.md`** lists every included airport with a procedure that changed this cycle,
grouped into one section per facility (see Facilities above for the section order). A section with
no changes says so; otherwise it lists only the airports that changed, ordered by airspace class
(Class B first, then C, then D, then everything else), then IDs without digits before IDs with
digits, then alphabetically. An airport whose changes are all "changed" charts lists them directly
under its line; one with a mix of new, changed and deleted charts groups them under `Changed:`,
`Deleted:` and `New:` headings instead. A changed chart links to the FAA's compare PDF, a new one to
its chart, and a deleted one to the previous cycle's chart (or says the previous chart isn't
available) - some compare links 404, which the note at the top says is common with military
facilities. A multi-page chart is one entry even when only a continuation page changed. A STAR
charted at several included airports is listed once, under the airport that owns it, with an "Also
serves" line naming the others; one the FAA marked Deleted and then Added again in the same metafile
is reported as a change instead, `NAME [Old](...) -> [New](...)`, with a note to check it carefully;
and one the FAA simply dropped from a single airport's served list (without deleting the chart
itself) gets an Info line saying so. For example:

```markdown
# AIRAC 2609 (03SEP2026)

Note: In some cases, the link will return a 404 Error. This is because the FAA does not have a
comparative document. This is common with Military facilities.

## ZOB
- CAK
  - Changed:
    - [ILS OR LOC RWY 23](https://aeronav.faa.gov/d-tpp/2609/compare_pdf/05620IL23_cmp.pdf)
  - Deleted:
    - NDB RWY 5 (previous chart not available)
      - Info: The FAA removed it from this airport's list; the procedure itself may still be in
        use elsewhere.
- CLE
  - No procedure changes this AIRAC.
- PHD
  - [RNAV (GPS) RWY 24](https://aeronav.faa.gov/d-tpp/2609/compare_pdf/05620R24_cmp.pdf)
    - Also serves: BJJ, YNG

## Other
- No procedure changes this AIRAC.
```

**Procedures.json** lists every included airport's current charts (a deleted one is left out - it
belongs in the changes document instead), airports in the same order as the changes document. A
STAR shared by several included airports is repeated under every one of them, since this is
per-airport data, not a change report.

#### FAA Chart Recall commands

**`Faa_Chart_Recall.txt`** is one `.OPENURL` command per page of every current chart at every
airport in the FAA's d-TPP Metafile - not just the airports and chart types picked above, which only
limit the two documents. For example:

```
.dtwI22Lc .OPENURL https://aeronav.faa.gov/d-tpp/2609/00058IL22L.PDF  ; DETROIT METRO WAYNE COUNTY-ILS OR LOC RWY 22L
```

A command is a period, the airport's FAA identifier in lower case, the chart's own code (below),
then a lower-case `c`; a chart's second and later pages add the page number after the `c`
(`.dtwHHOWEc`, then `.dtwHHOWEc2`). A chart the FAA deleted this cycle gets no command, and a chart
type or approach type FE-Buddy does not yet recognize gets none either, with a warning on the
Review tab naming it - please report it, so it can be taught to FE-Buddy.

**Approach charts (IAP)** - the approach type's own code, then `BC` for a back course, then any
variant letter, then the runway exactly as the FAA prints it:

| Type | Code | Type | Code |
|---|---|---|---|
| RNAV (any bracket, e.g. RNAV (GPS)) | R | LDA | D |
| ILS | I | LDA/DME | DD |
| LOC | L | GPS (not RNAV (GPS)) | G |
| LOC/DME | LD | TACAN | T |
| VOR | O | NDB | N |
| VOR/DME | OD | NDB/DME | ND |

For example, `ILS OR LOC RWY 22L` is `.dtwI22Lc` and `.dtwL22Lc` - one command per "OR" part.
`ILS Y OR LOC Y RWY 22L` is `.dtwIY22Lc` and `.dtwLY22Lc`; a variant the FAA prints on only one part
of an "OR" chart still applies to every part of it. `RWY 30L/R` gets one command per runway. A
circling approach keeps its letter (`VOR-A` is code `OA`). A charted visual approach is a lower-case
`v`, its name with spaces and punctuation removed, then its runway: `RIVER VISUAL RWY 19` is
`.aaavRIVER19c`.

No command at all for: a name starting `HI-` or `COPTER` (the whole chart), one holding the word
`PRM`, a bracket holding `CAT` (a Category II/III or special-authorization approach),
`CONVERGING`, an attention-all-users page (a name ending `AAUP`, or an RNAV DP AAUP), or a numbered
approach such as `VOR-1`. A GLS approach, or the GLS or LOC/NDB part of an "OR" chart, gets no
command either, though the chart's other parts still do.

**Other chart types:**

| Chart | Command |
|---|---|
| Airport diagram | `APD` |
| Takeoff minimums, diverse vector area, radar minimums | `TM`, `DVA`, `RM` - each opens straight to the airport's own page of the shared, multi-airport PDF |
| Alternate minimums | no command |
| Hot spots | `HS` |
| LAHSO | `LAHSO` |

**Departures, obstacle departures and STARs** use the chart's own FAA computer code when it has
one: a departure's command is the code's first half, a STAR's the second half, less the version
number the chart's name spells out - `JALEX3.JALEX` is `JALEX`, `BRODE.GRUUB1` is `GRUUB`. With no
usable computer code, the chart's name is spelled out instead, with the version, every bracketed
word, and the words `RNAV`, `OBSTACLE` and `COPTER` dropped - `KNIK THREE` is `KNIK`. When that name
is the airport's own name, the airport's identifier is used instead of it -
`TATALINA FOUR (OBSTACLE) (RNAV)` at TATALINA LRRS is `.tljTLJc` - unless two charts at the airport
would then share it, or another chart's code there is already the identifier.

The Review tab names how many commands `Faa_Chart_Recall.txt` holds and for how many airports.

### Telephony tab

- **Outputs:** `Telephony.txt` only - Telephony's only output, and it can't be turned off.
- **Telephony Data** - where the cards come from: FAA Order JO 7340.2, Chapter 3 - the ICAO
  register (Section 1) and the U.S. special call signs (Section 4). Like Wx Stations, this is not
  part of the AIRAC cycle, so every run downloads the latest pages first, whichever cycle you run,
  and falls back to FE-Buddy's kept copies only if it can't. It shows the kept copies' date, or that
  FE-Buddy has no copy yet. Left out: a register entry with no three-letter designator or no
  telephony, and a U.S. special call sign that has expired (one with a date FE-Buddy can't read is
  kept instead, with a warning).
- **Commands** - each operator gets an `.id` command for its three-letter designator (ICAO
  register) or identifier (U.S. special call sign), and another for its telephony reduced to
  letters and digits, e.g. `.idAVA` and `.idAVIANCA` for AVIANCA - one command when the two are the
  same, e.g. `.idNASA`. Typing either the designator/identifier from the data block or the
  telephony the pilot said works. Both are a lower-case `.id`, and the command is a lower-case
  `.echo`; the card it prints is upper case: an ICAO operator's card shows its three-letter
  designator, telephony, company and country; a U.S. special call sign's shows its identifier,
  telephony, operating agency and expiration date. When a telephony spells another operator's
  designator, or two operators' telephony only differ by spacing, one command shows every one of
  their cards, separated by `---`, the command's own operator first. Commands are listed
  alphabetically.
- **Region:** none - Telephony is not limited to a region; every operator in the FAA's pages gets a
  card.
- **Upload to vNAS:** `Telephony.txt`, the only file there is.

### vNAS Alias Upload tab

vNAS takes one alias file per facility, so your facility's own aliases and FE-Buddy's have to be
merged into one file before every upload. This tab does that: it writes
`Upload_to_vNAS\vNAS_Alias.txt` with your own alias files first, then every FE-Buddy alias file
ticked on its tab's **Upload to vNAS** card.

- **Outputs** - `Upload_to_vNAS\vNAS_Alias.txt` only.
- **FE-Buddy Alias Files** - every alias file FE-Buddy can write, and whether it goes in with the
  other tabs' settings as they are now: *Added to vNAS_Alias.txt*, *Not ticked on its Upload to vNAS
  card*, *Alias file turned off on its Outputs card*, or *Not selected on the General tab*. **Open
  tab** jumps to that sub-service to change it. If none go in, the card says so: `vNAS_Alias.txt`
  will hold only your custom aliases.
- **Custom Alias Files** - your facility's own alias files, merged in the order listed; the arrows
  move a file up or down, and the cross removes it. Optional: with none, `vNAS_Alias.txt` holds only
  the FE-Buddy alias files ticked for vNAS. The tab needs *something* to put in the file, though -
  with no custom alias file and no FE-Buddy alias file ticked for vNAS, it asks you to add one or
  untick vNAS Alias Upload on the General tab.
  - **Add file…** - a file on this PC; **Browse…** picks a different one. The full path is saved.
  - **Add web address** - a file on the web, starting with `https://`. On GitHub, paste the address
    of the file's own page (it has `/blob/` in it) or its Raw link - not the repository's or a
    folder's page. An address with a sign-in written into it (`https://user:password@…`, or the
    `?token=…` GitHub adds when you view a private file's raw text) is refused, because addresses
    are saved in FE-Buddy's settings and exports: use the plain address and choose a credential.
  - **Credential** - for a file in a private GitHub repository, choose a GitHub token that can read
    it, or **New credential…** to add one. Credentials are kept in Settings ▸ Credentials, never in
    FE-Buddy's settings. One token can serve several files: a web address with no credential, on
    the same website as an earlier file that has one, is offered **Use <credential>, like file N**.
    A fine-grained GitHub token needs access to the repository, with *Contents: Read-only*.
  - **Check** - reads the file now, the way the run will, and shows how many alias commands it has
    or what went wrong.
  - A file that isn't on this PC, or a credential that isn't (the settings came from another PC,
    say), is flagged under the row; choose your own.
  - Credential choices are never included in a settings export. Importing settings keeps your
    credential for a web address the import leaves as it was; any other web address needs its
    credential chosen again.
- **Every run reads the files fresh**, so an edit on GitHub is picked up next time.
- **A file that can't be read is left out, not the whole run.** `vNAS_Alias.txt` is still written
  from the rest, and the Review tab warns which file was left out and why. Uploading that
  `vNAS_Alias.txt` would remove the missing file's aliases from vNAS, so fix the problem and run
  again first. If nothing at all can go in, no `vNAS_Alias.txt` is written, and one an earlier run
  left in `Upload_to_vNAS` is deleted so it can't be uploaded by mistake.
- **How the file is laid out:** a `.FeUseOnly` line, if any of your files has one, goes first (only
  the first one found is kept); then each of your files, separated by a blank line; then the
  line `; ===== FE-Buddy aliases (AIRAC <cycle>) start here. FE-Buddy replaces everything below
  this line every cycle. =====`; then each ticked FE-Buddy alias file under a `; ----- <name> -----`
  heading.
- **Reusing last cycle's upload:** you can keep the `vNAS_Alias.txt` you uploaded last cycle as your
  custom file. Everything from that marker line down is left out, so last cycle's FE-Buddy aliases
  are replaced rather than added twice - just keep your own aliases above the line.
- **Duplicates:** a command from your custom files that another merged file has too gets a warning
  on the Review tab (the first ten are named), since CRC can only run one of each. Commands only
  FE-Buddy's own files share are listed in `Duplicate_Alias_Commands.txt` instead.

Without this tab ticked, `vNAS_Alias.txt` is still written whenever an FE-Buddy alias file is ticked
for vNAS - it then holds FE-Buddy's aliases only, and the Review tab warns that uploading it would
remove your facility's own aliases from vNAS.

### Preview Settings tab

A plain-words summary of every tab: the folder the run writes to, what will be written, what it
covers, which region applies, and which files go to vNAS and get CRC-ERAM defaults. It warns if a
tab has something to fix (the run is blocked until you do) or unsaved changes (they are saved when
the run starts). **Run AIRAC Service** starts the run.

If the cycle has been run before - its `AIRAC_<cycle>` folder already has files in it - FE-Buddy
asks what to do first:

- **Overwrite files** (the default) - this run's files replace the old ones; any old file this run
  does not write is left as it is.
- **Delete all files** - everything in the `AIRAC_<cycle>` folder is permanently deleted (not sent
  to the Recycle Bin) first, so afterwards it holds only this run's files.
- **Cancel** - nothing runs.

### Review tab

- **Progress** - a line per sub-service: waiting, working, finished or failed.
- **Errors** - anything that stopped part of the run.
- **Advisories** - output you might expect but will not find, and why (for example "nothing
  matched your filters").
- **Results** - per sub-service, what it produced, with its warnings and routine messages each
  behind a **Show** button.
- **Output** - every file written (collapsed to a count; a Departures or Arrivals run writes
  thousands) and **Open output folder**, which opens the run's `AIRAC_<cycle>` folder.

## File Conversions

Turns files you already have into files CRC can use. It works like the AIRAC Service screen -
tabs down the left, the same action bar, the same **Review** tab - with two differences: every
conversion is always on the rail (there is nothing to tick), and each one runs on its own, from
the button at the bottom of its own tab. Because no tab leads to another, the action bar has no
**Previous** / **Next**; pick a conversion on the left. Nothing here needs the AIRAC data, so the
screen is ready as soon as FE-Buddy opens.

### DAT to GeoJSON tab

Converts FAA `.dat` RADAR Video Maps (RVMs) into GeoJSON video maps: one `.geojson` per `.dat`,
with the same name.

- **Source Files** - either **every .dat file in a folder** (FE-Buddy remembers the folder; files
  in its sub-folders are left out) or **files I pick**: add one or several at a time, and remove
  any you did not mean to. Picked files are forgotten when FE-Buddy closes.
- **CRC ERAM Defaults** - the Lines panel only, since a video map is all lines. Tick **Include**
  and fill it in to give every converted map the same look; untick it to leave the look to CRC.
- **Cropping** - keep only what lies within this many nautical miles of the map's **point of
  tangency** (the centre the `.dat` file itself defines). Leave it blank to convert the whole
  map. A line that crosses the distance is cut exactly where it meets it, not dropped. A map
  without a point of tangency cannot be cropped; the Review tab says which.
- **Convert DAT files** - saves any unsaved settings (you are asked first) and runs. It stays
  off, with the reason beside it, until there is something to convert.

A record in a `.dat` file that FE-Buddy cannot read is skipped and listed on the Review tab; the
rest of the map still converts. Coordinates are read as north and west unless the file says
otherwise, and a line crossing the 180° meridian (Guam, for example) is split so it draws
correctly.

### SCT2 to GeoJSON tab

Converts VRC sector files (`.sct2` or `.sct`) into GeoJSON: a folder per sector file, named after
it, holding:

| File | From |
|---|---|
| `ARTCC`, `ARTCC-HIGH`, `ARTCC-LOW` | the boundary sections - one feature per boundary name |
| `LOW-AIRWAY`, `HIGH-AIRWAY` | the airway sections - one feature per airway |
| `GEO` | `[GEO]` |
| `SID\<diagram>`, `STAR\<diagram>` | one file per SID and STAR diagram, named after it |
| `LABELS` | `[LABELS]`, as text |
| `REGIONS` | `[REGIONS]`, as filled areas |

A section with nothing in it writes no file. Airports, VORs, NDBs and fixes are not written, but a
coordinate given as one of their names (`DJB DJB`) is found and used. Lines that meet are joined
back together and a segment drawn twice is written once, so the files are small and dashed styles
stay dashed.

- **Source Files** - the same as on the DAT tab: a remembered folder (every `.sct2` and `.sct` in
  it) or files you pick.
- **CRC ERAM Defaults** - a **Lines** panel for every lines file (boundaries, airways, GEO, SIDs
  and STARs) and a **Labels** panel for the labels file. Regions have no CRC defaults.
- **Convert sector files** - saves any unsaved settings (you are asked first) and runs.

A record FE-Buddy cannot read - a mistyped coordinate, a name that is not defined anywhere in the
file - is skipped and listed on the Review tab; the rest of the file still converts. VRC colours
are not carried over: CRC styles lines through the CRC defaults instead.

### ERAM to GeoJSON tab

Converts the `Geomaps.xml` of an ERAM adaptation export into GeoJSON: a folder per Geomaps file,
named after it, with a folder inside for each GeoMap (named after its `GeomapId`). Lines, symbols
(and their labels), text and SAA boundaries and labels are all converted.

- **Source Files** - `Geomaps.xml` itself, or a remembered folder. The folder can be the whole
  unzipped export: only its Geomaps file is converted, and the other files are listed on the
  Review tab as left alone.
- **Output Layout** - how the files are split:
  - **Object Type and Map Group** - a file per map object, named after its type and map group
    (`AIRWAY_3`, `SECTOR_12`, `SAA_53`). Objects that share a name share a file when their
    defaults agree; otherwise the later one gets a numbered file (`AIRWAY_3 (2)`).
  - **Filter Index and Similar Attributes** - files grouped by filter, kind and look, e.g.
    `FILTER 05\FILTER 05__Line__BCG 3__Style solid__Thickness 1`. An element with several filters
    goes under `MULTI FILTERS\`; one whose look cannot be fully worked out goes under
    `MISSING DEFAULTS\`. Everything in a file draws the same way.
- **CRC ERAM Defaults Source** - where each file's CRC defaults come from:
  - **From the XML** - carry over as much as possible: each object's own Line, Symbol and Text
    defaults, and each element's own overrides.
  - **From the XML, filling gaps from the card** - the same, but an object with no usable defaults
    of its own takes the CRC ERAM Defaults on the tab. SAA objects carry no BCG or filters of
    their own, so this is the choice that gives them a look in CRC.
  - **From the card only** - ignore the XML's styling and use the tab's CRC ERAM Defaults for
    everything.
- **CRC ERAM Defaults** - Lines, Symbols and Text panels. They only show, and only need filling
  in, when the card is one of the sources.
- **Convert GeoMaps** - saves any unsaved settings (you are asked first) and runs.

ERAM's style names become CRC's (`Solid` → `solid`, `RNAVOnlyWaypoint` → `rnavOnlyWaypoint`), and
every value is checked against what CRC can draw - `DME` symbols, for example, have no CRC style.
An object whose defaults are missing, incomplete or invalid is listed on the Review tab; a value
CRC cannot draw is left out, so that feature takes its file's default. ERAM text has no opaque
background, so its text is never opaque; ERAM's colours and display settings are not carried
over. Line segments that meet are joined back into lines, including ones written backwards.

## Output files

Every run of a cycle writes into one folder, `AIRAC_<cycle>` (for example `AIRAC_2610`), in your
output folder (Settings ▸ Default Output Directory) - inside a `FE-Buddy_Output` folder if that
option is on. Every sub-service shares it: all the GeoJSON goes in its `Geojson` folder, every
alias file goes in its `Aliases` folder, and Procedures' two documents go in its
`Publication_Docs` folder. The GeoJSON files you marked for vNAS go in `Upload_to_vNAS\Geojson`
instead of `Geojson`, and the alias files you marked for vNAS are merged into
`Upload_to_vNAS\vNAS_Alias.txt` (see [vNAS Alias Upload tab](#vnas-alias-upload-tab)). If the run wrote at least one alias file, it also
writes `Duplicate_Alias_Commands.txt` straight in the cycle folder - see
[Why does the same alias command show up in both Departures.txt and Arrivals.txt?](FAQ-and-Troubleshooting.md#why-does-the-same-alias-command-show-up-in-both-departurestxt-and-arrivalstxt)
for an example of what it catches:

```
<output folder>\
└── FE-Buddy_Output\                  (only with "Add a FE-Buddy_Output folder")
    ├── AIRAC_2610\
    │   ├── Duplicate_Alias_Commands.txt  (only when the run wrote at least one alias file)
    │   ├── Aliases\
    │   │   └── Airports.txt, Airways.txt, Departures.txt, Arrivals.txt, Navaids.txt, Faa_Chart_Recall.txt, Telephony.txt
    │   ├── Geojson\
    │   │   ├── Runways_Lines, Airports_Symbols, Airports_Text (.geojson)
    │   │   ├── Airways_<group>_Lines / _Symbols / _Text (.geojson)
    │   │   ├── NAVAIDs_Symbols / _Text (.geojson), or NAVAIDs_<type>s_Symbols / _Text per type
    │   │   ├── ARTCC-Boundary_High/Low/Unlimited_Lines, or _<ARTCC>-<altitude>_Lines per ARTCC
    │   │   ├── Fix_Symbols / _Text, or Fix_<fixUse|chart|chart-fixUse>_Symbols / _Text per group
    │   │   ├── Wx_Symbols / _Text (.geojson)
    │   │   └── <ARTCC>\<airport>\<airport>_<procedure>_Lines / _Symbols / _Text (.geojson)
    │   ├── Publication_Docs\
    │   │   └── Procedure_Changes.md, Procedures.json
    │   └── Upload_to_vNAS\           (only the files marked for vNAS)
    │       ├── vNAS_Alias.txt        your custom alias files, then the alias files marked for vNAS
    │       └── Geojson\              the GeoJSON files marked for vNAS, laid out as above
    ├── DAT to GeoJSON\               <.dat file name>.geojson, one per converted map
    ├── SCT2 to GeoJSON\
    │   └── <sector file name>\       ARTCC, …, GEO, LABELS, REGIONS (.geojson), SID\, STAR\
    └── ERAM to GeoJSON\
        └── <Geomaps file name>\<GeomapId>\   <type>_<group>.geojson, or FILTER nn\ folders
```

A GeoJSON file marked for vNAS goes to `Upload_to_vNAS` instead of, not as well as, `Geojson`. An
alias file marked for vNAS is different: it always stays in `Aliases`, and is copied into
`vNAS_Alias.txt` as well.
The File Conversions do not use the cycle folder: each conversion writes into its own folder, next
to the `AIRAC_<cycle>` folders, and replaces any file of the same name.

Departures and Arrivals share the same `<ARTCC>\<airport>` folder; an Arrivals file's name adds
`_STAR_` before the kind (`LAS_BLAID_STAR_Lines.geojson`) so a SID and a STAR with the same
identifier at one airport never overwrite each other.

A folder is created only when something is written to it. A file that would be empty (nothing
matched) is not written. A run of a cycle that has been run before asks first whether to
overwrite the old files or delete them (see [Preview Settings tab](#preview-settings-tab)).

**`Duplicate_Alias_Commands.txt`** lists every alias command used by more than one line across all
the alias files the run wrote - CRC can only run one of them - grouped by the ARTCC responsible for
each line's airport: your own facility (Settings ▸ Facility Profile) first, other ARTCCs
alphabetically, then `TELEPHONY` for a `Telephony.txt` command (it belongs to an operator, not an
airport), then `OTHER` for any other command (an airway or a NAVAID) that names no airport. It says
so when there are no duplicates, and the Review tab carries an advisory warning when there are.

## Map

- **Load GeoJSON…** - open one or more `.geojson` files to check them. Each gets its own colour in
  the **Layers** list, where you can hide or remove it. A broken or empty file is skipped and
  reported; the others still load. **Clear all** removes them; **Reset view** zooms back to the
  US.
- **Default Region of Interest** - the same default region as in Settings, edited in place: drag
  a box on the small map or type the corners, then **Set ROI**. **Show default ROI** draws it on
  the main map.
- **Using the map:** drag to pan, scroll to zoom. The cursor's lat/lon shows in the corner.

## Settings

Changes here - the Default Region of Interest included - are saved with the **Save** button at the
top. While something is unsaved, "Unsaved changes" shows beside Save,
**Settings** in the side menu gets an amber dot, and Save is live; once saved - or changed back -
all three clear.

- **Updates**
  - **Channel** - *Stable* (the right choice for almost everyone), *Beta* or *Alpha*. Only pick
    Beta or Alpha if a developer asks you to.
  - **Check for updates now**, and **Get the latest stable installer** (opens the releases page -
    use it to go back to stable from a pre-release).
- **Facility Profile**
  - **Facility** - your ARTCC, picked from the current cycle's data. Procedures uses it as the
    facility whose section leads both of its documents, and every AIRAC Service run lists it first
    in `Duplicate_Alias_Commands.txt`.
  - **Default Output Directory** (your Desktop until you choose one) and **Add a FE-Buddy_Output
    folder inside that directory** (on by default). The line under them shows the folder a run of
    the current cycle would write to.
- **Default Region of Interest** - **Set ROI…** opens the map picker; **Clear** turns it off.
  Every sub-service uses it unless its own tab overrides it.
- **GeoJSON Files**
  - **Maximum Coordinate Precision** - 5, 6 or 7 decimal places. 6 (about 10 cm) suits most
    files; 7 is for high-precision airport tracing; 5 keeps files smallest.
  - **File Layout** - *Single line* (smallest, the default) or *Pretty print* (readable in a
    text editor).
- **Credentials** - sign-ins FE-Buddy uses to download from protected websites, such as a GitHub
  token for a private repository that holds your custom alias file. They are kept in Windows
  Credential Manager, never in FE-Buddy's settings or an export, and a saved token is never shown
  again. **Add credential…**, **Edit…**, **Remove**, **Remove all**, and **Check** (asks GitHub
  whether a GitHub token still works). Changes here are saved straight away, not with Save.
  Uninstalling FE-Buddy removes them; updating never does.
  To make a GitHub token with just the access FE-Buddy needs, follow
  [Creating a GitHub token for FE-Buddy](GitHub-Token-Guide.md) (also linked in the editor).
- **FE-Buddy's GitHub Requests** (advanced - most people never need it) - whether FE-Buddy's own
  update checks, News and update downloads use a GitHub token. *Don't use a GitHub token* is the
  default and works for everyone. *Use a GitHub token* lets you pick one of your GitHub tokens (or
  **New GitHub token…**), to get past GitHub's limit of 60 requests an hour. If a request with the
  token fails, or Windows can't read the token, FE-Buddy goes ahead without it.

## Info

Links to the manual, the change log, and the issue tracker. (The manual link still opens the
2.x manual; this guide is the 3.0 one.)

## Updating FE-Buddy

When an update is available the version at the top of the window becomes a button. It opens the
update window: your version, the latest, and the release notes for everything in between.

- **Update now** downloads the installer and runs it. FE-Buddy closes while it installs and opens
  again afterwards; your settings are kept. If you have unsaved changes or a run in progress, you
  are asked first.
- **Later** closes the window; the version stays amber for the rest of the session as a reminder.
