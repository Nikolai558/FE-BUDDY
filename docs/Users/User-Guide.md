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
| **General** | Which cycle, and which sub-services (Airports, Airways, Departures, Arrivals, NAVAIDs, ARTCC Boundaries, Fixes). Always there. |
| **Airports / Airways / Departures / Arrivals / NAVAIDs / ARTCC Boundaries / Fixes** | One tab per sub-service you ticked, with its settings. |
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

**Outputs** - what the sub-service writes: **GeoJSON files** and/or the **alias file**. At least
one must stay on; to make nothing for a sub-service, untick it on the General tab instead. ARTCC
Boundaries and Fixes have no Outputs card: neither has an alias file, and both always write
GeoJSON.

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
tab's settings will write. Tick the ones you will upload to vNAS; they are written to the cycle's
`Upload_to_vNAS` folder instead of the usual one (see [Output files](#output-files)), so they are
ready to upload. Once a GeoJSON file is ticked, a follow-up question asks whether those files get
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
depending on its File Layout (see the Fixes tab). Each class is its own block of boxes; blocks that
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

- **Outputs:** GeoJSON, and `NAVAIDs.txt` - a `.nav<ID>` command per NAVAID identifier and a
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
- **Upload to vNAS:** *All in one file* - `NAVAIDs_Symbols`, `NAVAIDs_Text`, then `NAVAIDs.txt`.
  *One pair per type* - each included type's `NAVAIDs_<Type>s_Symbols` / `_Text`, then
  `NAVAIDs.txt`.

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
option is on. Every sub-service shares it: all the GeoJSON goes in its `Geojson` folder, and the
alias files sit in the folder itself. The files you marked for vNAS go in `Upload_to_vNAS` instead,
laid out the same way:

```
<output folder>\
└── FE-Buddy_Output\                  (only with "Add a FE-Buddy_Output folder")
    ├── AIRAC_2610\
    │   ├── Airports.txt, Airways.txt, Departures.txt, Arrivals.txt, NAVAIDs.txt
    │   ├── Geojson\
    │   │   ├── Runways_Lines, Airports_Symbols, Airports_Text (.geojson)
    │   │   ├── Airways_<group>_Lines / _Symbols / _Text (.geojson)
    │   │   ├── NAVAIDs_Symbols / _Text (.geojson), or NAVAIDs_<type>s_Symbols / _Text per type
    │   │   ├── ARTCC-Boundary_High/Low/Unlimited_Lines, or _<ARTCC>-<altitude>_Lines per ARTCC
    │   │   ├── Fix_Symbols / _Text, or Fix_<fixUse|chart|chart-fixUse>_Symbols / _Text per group
    │   │   └── <ARTCC>\<airport>\<airport>_<procedure>_Lines / _Symbols / _Text (.geojson)
    │   └── Upload_to_vNAS\           (only the files marked for vNAS)
    │       ├── the alias files marked for vNAS
    │       └── Geojson\              the GeoJSON files marked for vNAS, laid out as above
    ├── DAT to GeoJSON\               <.dat file name>.geojson, one per converted map
    ├── SCT2 to GeoJSON\
    │   └── <sector file name>\       ARTCC, …, GEO, LABELS, REGIONS (.geojson), SID\, STAR\
    └── ERAM to GeoJSON\
        └── <Geomaps file name>\<GeomapId>\   <type>_<group>.geojson, or FILTER nn\ folders
```

The File Conversions do not use the cycle folder: each conversion writes into its own folder, next
to the `AIRAC_<cycle>` folders, and replaces any file of the same name.

Departures and Arrivals share the same `<ARTCC>\<airport>` folder; an Arrivals file's name adds
`_STAR_` before the kind (`LAS_BLAID_STAR_Lines.geojson`) so a SID and a STAR with the same
identifier at one airport never overwrite each other.

A folder is created only when something is written to it. A file that would be empty (nothing
matched) is not written. A run of a cycle that has been run before asks first whether to
overwrite the old files or delete them (see [Preview Settings tab](#preview-settings-tab)).

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
  - **Facility** - your ARTCC, picked from the current cycle's data. Saved for future features;
    today's sub-services do not use it.
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
