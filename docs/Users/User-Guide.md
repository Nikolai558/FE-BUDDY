# User guide

Every screen and option in FE-Buddy 3.0. New here? Start with [Getting started](Getting-Started.md).
Unfamiliar words are in the [glossary](Glossary.md).

**Contents:** [The window](#the-window) · [Dashboard](#dashboard) · [AIRAC Service](#airac-service) ·
[File Conversions](#file-conversions) · [Output files](#output-files) · [Map](#map) ·
[Settings](#settings) · [Info](#info)

---

## The window

- **Menu (left)** - Dashboard, AIRAC Service, File Conversions and Map, then Settings and Info. The
  arrow collapses it to icons.
- **Systems (foot of the menu)** - one dot for the worst of Internet, AIRAC data and Updates; click
  it for a dot each. Green is fine, amber needs a look or is still working, red isn't working.
  **Re-check** runs the checks again.
- **Top** - FE-Buddy's version (hover it for the update status) and the AIRAC status, which follows
  the downloads at launch and then shows the current cycle. When there's an update, a red **Update
  available!** badge appears beside the version: click it for the release notes and **Update now**.
  FE-Buddy closes, updates and reopens, keeping your settings.
- **Bottom** - the time in Zulu.

## Dashboard

- **About** - what FE-Buddy is, the Discord link, and when the next AIRAC cycle starts.
- **News** - posts from the FE-Buddy team. The button lights up when there's one you haven't seen.
- **Activity log** - what FE-Buddy has done this session. It starts folded away; the chips open it
  and filter it by level. **Clear** empties the list on screen; the log file in
  `%APPDATA%\FE-Buddy\Logs` keeps everything.

## AIRAC Service

Where you make files. The tabs run down the left:

| Tab | What it is |
|---|---|
| **General** | The cycle, and which sub-services to run. |
| One per sub-service | That sub-service's settings. Appears when you tick it. |
| **File Names** | Every file the run will write, and new names for any of them. |
| **Preview Settings** | What the run will do, and the **Run AIRAC Service** button. |
| **Review** | What the last run did. |

The screen waits until the AIRAC data has downloaded.

### Saving

Each tab saves on its own. The bar above and below each tab has **Previous**, **Next** and
**Preview settings**, **Save**, **Undo all changes** (drops unsaved edits) and **Undo last save**
(goes back to the save before). A tab gets an **amber** dot beside its name with unsaved edits, and
a **red** dot when something is missing or invalid: the field is outlined in red, and hovering it
says why.

### General tab

- **Cycle** - Previous, Current or Next, each with its effective date and status. The next cycle
  says *not yet published* until the FAA releases it, a few weeks early.
- **Sub-Services** - tick what to make: ARTCC Boundaries, Airports, Airways, Arrivals, Departures,
  NAVAIDs, Fixes, Procedures, Telephony, Wx Stations, vNAS Alias Upload. Unticking one keeps its
  settings for next time.

### The cards most tabs share

The tabs that write GeoJSON are built from the same cards. Procedures, Telephony and vNAS Alias
Upload have their own; see their sections.

- **Outputs** - GeoJSON files, the alias file, or both. (To make nothing, untick the sub-service.)
  ARTCC Boundaries, Fixes and Wx Stations have no alias file, so no Outputs card.
- **What Files Do You Want?** - which of **Lines**, **Symbols** and **Text** to write.
- **FE-Buddy Properties** - extra `feb.*` fields on each feature, such as an airway's ID, handy when
  checking a file. Each has a tooltip. CRC ignores them.
- **Region of Interest** - Settings' **Default Region of Interest**, unless you tick **Override the
  default ROI** and give the tab its own box (type the corners or **Pick on map…**). With no region,
  the run covers the whole country.
- **Upload to vNAS** - tick the files you'll upload. A ticked GeoJSON file is written to
  `Upload_to_vNAS\Geojson` instead of `Geojson`. vNAS takes only one alias file, so a ticked alias file stays
  in `Aliases` and is also merged into `Upload_to_vNAS\vNAS_Alias.txt`. Then choose which ticked
  GeoJSON files get **CRC-ERAM Default Properties**: none, all of them, or specific files.
- **CRC ERAM Defaults** - how CRC draws a file, written as one hidden defaults feature at its top.
  Only files going to vNAS get them (CRC reads its maps from vNAS), so the card appears once you've
  chosen files for them, showing only the boxes those files need. FE-Buddy never guesses: every box
  shown must be filled in.

| Field | Values | For |
|---|---|---|
| BCG | 1-40 | everything |
| Filters | one or more of 0-40 | everything |
| Style | Lines: solid, shortDashed, longDashed, longDashShortDash. Symbols: a CRC symbol (vor, ndb, airport, rnav, …) | Lines, Symbols |
| Thickness | 1-3 | Lines |
| Size | Symbols 1-4, Text 0-5 | Symbols, Text |
| Underline, Opaque | Yes / No | Text |
| X offset, Y offset | a whole number | Text |

Airports, Departures, Arrivals and Wx Stations have one set of boxes. The others have one set per
class, so different files can look different:

| Tab | Sets |
|---|---|
| Airways | High and/or Low, for HighLow files. High, Low and Other for Designation files, since one file can hold every class (18,000 ft or more is High, below that Low, none Other). |
| NAVAIDs | one, or one per NAVAID type with *One file per NAVAID type* |
| ARTCC Boundaries | one per altitude, or one per ARTCC and altitude (`ZOB-HIGH`, `ZOB-LOW`, …) |
| Fixes | one, or one per fix use, chart or combination, following File Layout |

### ARTCC Boundaries tab

Each ARTCC's boundary as lines. No Symbols, Text or alias file.

- **ARTCCs** - tick the ones you want; none means all. Only ARTCCs with boundary lines in the cycle
  are listed. Your Facility Profile ARTCC is ticked until you first save the tab.
- **File Layout:**
  - **High and Low** (the default) - `ARTCC-Boundary_High_Lines` and `ARTCC-Boundary_Low_Lines`. An
    UNLIMITED boundary goes in both.
  - **High, Low and Unlimited** - adds `ARTCC-Boundary_Unlimited_Lines`.
  - **One file per ARTCC and altitude** - e.g. `ARTCC-Boundary_ZOB-HIGH_Lines`, so your own ARTCC can
    be styled apart from its neighbours.
- **Split GeoJSON at the Antimeridian** - on by default. ZAK, ZAN, ZAP and part of ZOA cross ±180°;
  this keeps them from drawing across the whole map.
- ZAK, ZAP and ZWY have two overlapping oceanic rings at one altitude; the `feb.type` property (tick
  it under FE-Buddy Properties) tells them apart.
- **Region** - a boundary is clipped at the region's edge.

### Airports tab

- **Files** - Runways (lines), Airports symbols, and Airports text (FAA ID and name). An airport is
  included when its reference point is in the region.
- **`Airports.txt`** - a command for each airport's FAA ID, and its ICAO ID if that's different, showing
  its card in CRC: identifiers and name, tower type, ARTCC, longest runway, elevation, pattern
  altitude, FSS, CTAF, weather, attended hours, and its airspace class with the hours it's in effect.
  It covers every open airport; the region only limits the GeoJSON.

### Airways tab

- **GeoJSON files:**
  - **HighLow** - `Airways_High` and `Airways_Low`. The **High and Low Files** card says which file
    each airway type goes in: High, Low or Both. J and Q start in High and V and T in Low; any other
    type starts blank, and the tab stays red until you choose. It goes by type, not by published
    altitude.
  - **Designation** - one set per designation: `Airways_J`, `Airways_V`, `Airways_Q`, …
  - **None** - no GeoJSON, just the alias file.
- **`Airways.txt`** - a command per airway that draws its fixes, e.g. `.J3F`. Choose **All FAA
  airways** or **ROI airways only**.
- **Designations to Include** - untick one to leave it out of everything.
- **Buffer Airway Waypoints** (off to start) - stops lines short of each waypoint so they don't run
  through the symbols: **Around fixes** (2.5 NM to start) and **Around NAVAIDs** (5 NM), each 0-10 NM. A leg too
  short for both gaps isn't drawn.
- **Split GeoJSON at the Antimeridian** - leave it on.
- **Region** - an airway is included when its line crosses the region, and is clipped to it.
- An airway with a waypoint FE-Buddy can't locate is left out, and the Review tab says which.

### Departures and Arrivals tabs

Departures covers SIDs, and ODPs unless you untick **Include obstacle departures (ODPs)**. Arrivals
covers STARs. Otherwise the two tabs work the same.

- **Files** - Lines (shared segments drawn once), Symbols and Text for each procedure, in a folder
  per airport: `Geojson\<ARTCC>\<airport>\<airport>_<procedure>_Lines.geojson`. Arrivals add `STAR`
  (`LAS_BLAID_STAR_Lines.geojson`), so a SID and a STAR with the same name never overwrite each other.
- **Alias file** - `Departures.txt` or `Arrivals.txt`: a command per airport and procedure that draws
  its fixes. A STAR lists its transitions first, then its bodies.
- **ARTCCs** - tick the ones you want; none means all. Your facility from Settings ▸ Facility Profile
  is ticked until you first save the tab. A STAR serving airports in two ARTCCs goes with each
  airport's own ARTCC.
- **Amendment Date** - every procedure, or only those amended in the last *N* cycles, the last *N*
  days, or since a date. Cycles count back from the selected cycle, which is the first: with 2610
  selected, the last 4 cycles are 2607 to 2610.
- **How the Region Selects…** - every procedure of an airport in the region, or any procedure with
  a point in it. The region limits the alias file too.
- **Names** - a procedure is named by its FAA computer code without the version number
  (`DOTSS2.DOTSS` is `DOTSS`; a STAR's code runs the other way, so `AALAN.BLAID2` is `BLAID`), or by
  its name without punctuation when it has no code (`O'HARE` is `OHARE`).
- **Upload to vNAS** - by kind (every procedure's Lines, Symbols or Text), since a run writes
  thousands of files.

### NAVAIDs tab

- **NAVAID Types** - one tick per type in the cycle (VOR, VORTAC, VOR/DME, VOT, TACAN, DME, NDB, FAN
  MARKER, …), all on to start. An unticked type is left out of the GeoJSON and the alias file.
  NAVAIDs marked SHUTDOWN are always left out.
- **File Layout** - *All in one file* (`NAVAIDs_Symbols`, `NAVAIDs_Text`) or *One file per NAVAID
  type* (`NAVAIDs_VORTACs_Symbols`, `NAVAIDs_VOR-DMEs_Text`, …).
- **Files** - Symbols, and Text: the identifier, then the name and type (`CGT` /
  `CHICAGO HEIGHTS VORTAC`). No Lines.
- **NAVAID Symbol Style** - with *All in one file* and CRC-ERAM defaults on the Symbols file: style
  each NAVAID by its type (VORs as `vor`, TACANs and DMEs as `tacan`, NDBs as `ndb`, fan markers as
  you choose; CONSOLAN has no CRC style and gets a warning), or one style for the whole file.
- **`Navaids.txt`** - a `.nav` command for each identifier and each name, showing the identifier,
  name, type, frequency and ARTCCs. When several NAVAIDs share an identifier (`ABQ` is a VORTAC and a
  VOT), one command lists them all. It covers every NAVAID of the ticked types; the region only
  limits the GeoJSON.

### Fixes tab

A symbol and a label (its identifier) for every NASR fix. No Lines or alias file.

- **File Layout:**
  - **All fixes in one file** (the default) - `Fix_Symbols`, `Fix_Text`.
  - **One file per fix use** - e.g. `Fix_WYPNT_Symbols`, with a **Fix Uses** list to untick any.
  - **One file per chart** - e.g. `Fix_ENROUTE-LOW_Symbols`, with a **Charts** list. A fix on several
    charts goes in each file; one on none goes in `Fix_NO-CHART_Symbols`.
  - **One file per chart + fix use combination** - the pairs you add on the **Combinations** card,
    e.g. `Fix_ENROUTE-LOW-WYPNT_Symbols`. A combination with no fixes in the cycle gives a warning.
- **Fix uses** - COMPUTER-NAV, MIL-RPRTNG-PNT, MIL-WYPNT, NRS-WYPNT, RADAR, RPRTNG-PNT, VFR-WYPNT and
  WYPNT (from NASR's `CN`, `MR`, `MW`, `NRS`, `RADAR`, `RP`, `VFR` and `WP`). Any other code is kept
  as written.
- **Region** - a fix is included when it's inside the region.

### Procedures tab

Built from the FAA's **d-TPP Metafile** - the index of approach plates, SIDs, STARs, airport
diagrams and the rest - not NASR. The FAA posts it only 15-18 days before a cycle starts, so the next
cycle's is often missing; the run still goes ahead, just without Procedures' files.

- **Outputs** - any of:
  - **Procedure Changes document** (`Procedure_Changes.md`) - what changed this cycle at the airports
    you pick.
  - **Procedures.json** - every current chart at the airports you pick.
  - **Alias file** (`Faa_Chart_Recall.txt`) - [chart recall commands](#faa-chart-recall-commands) for
    every chart at every airport, whatever you pick below.
- **d-TPP Data** - the cycle's metafile, and whether a deleted chart can be linked to its last copy
  (that needs the previous cycle's metafile).
- **What the documents cover** - these add up:
  - **Facilities** - every airport of the ticked ARTCCs.
  - **Airports** - airports you list by FAA or ICAO ID, plus, if ticked, every airport inside the
    region.
  - **Procedures at Any Airport** - a procedure by name, wherever it's charted.
  - **Airport + Procedure** - one chart at one airport.
  - **Chart Types** - which kinds of chart a whole airport adds. On to start: approaches (IAP), STARs
    (STR), departures (DP), obstacle departures (ODP), RNAV DP AAUPs (DAU) and airport diagrams
    (APD). Off: minimums (MIN), hot spots (HOT) and LAHSO (LAH). A procedure picked by name is always
    included.
- **Procedures.json Fields** - the optional fields the JSON carries.
- **Region of Interest** - only decides which airports *Also include every airport inside the region
  of interest* adds. Nothing is clipped.

**`Procedure_Changes.md`** has a section per facility: your Settings ▸ Facility Profile facility
first, then the rest alphabetically, then "Other". Each lists the airports that changed, Class B
first, then C, D and the rest. A changed chart links to the FAA's comparison PDF, a new one to its
chart, and a deleted one to its last chart. A STAR at several airports is listed once, with "Also
serves". A facility with no changes says so under its heading. For example:

```markdown
# AIRAC 2609 (03SEP2026)

Note: In some cases, the link will return a 404 Error. This is because the FAA does not have a comparative document. This is common with Military facilities.

## ZOB
- CAK
  - Changed:
    - [ILS OR LOC RWY 23](https://aeronav.faa.gov/d-tpp/2609/compare_pdf/05620IL23_cmp.pdf)
  - Deleted:
    - NDB RWY 5 (previous chart not available)
- PHD
  - [RNAV (GPS) RWY 24](https://aeronav.faa.gov/d-tpp/2609/compare_pdf/05620R24_cmp.pdf)
    - Also serves: BJJ, YNG

## Other
- No procedure changes this AIRAC.
```

**`Procedures.json`** lists the same airports' current charts. A STAR shared by several airports is
repeated under each.

#### FAA Chart Recall commands

One `.OPENURL` command per page of every current chart:

```
.dtwI22Lc .OPENURL https://aeronav.faa.gov/d-tpp/2609/00058IL22L.PDF  ; DETROIT METRO WAYNE COUNTY-ILS OR LOC RWY 22L
```

A command is `.`, the airport's FAA ID in lower case, the chart's code, then `c`. Page 2 onwards adds
the page number after the `c`: `.dtwHHOWEc2`.

- **Approaches** - the type's code, `BC` for a back course, any variant letter, then the runway as
  printed. `ILS Y OR LOC Y RWY 22L` is `.dtwIY22Lc` and `.dtwLY22Lc`: one command per "OR" part,
  and a variant on one part applies to all of them. `RWY 30L/R` gets one per runway. A circling
  approach keeps its letter (`VOR-A` is `OA`). A charted visual is `v`, its name without spaces or
  punctuation, then the runway: `RIVER VISUAL RWY 19` is `.aaavRIVER19c`.

  | Type | Code | Type | Code |
  |---|---|---|---|
  | RNAV (any kind) | R | LDA | D |
  | ILS | I | LDA/DME | DD |
  | LOC | L | GPS | G |
  | LOC/DME | LD | TACAN | T |
  | VOR | O | NDB | N |
  | VOR/DME | OD | NDB/DME | ND |

- **Departures and STARs** - the computer code's procedure name without the version
  (`JALEX3.JALEX` is `JALEX`, `BRODE.GRUUB1` is `GRUUB`), or, with no code, the chart's name without
  its version or bracketed words (`KNIK THREE` is `KNIK`).
- **Other charts** - airport diagram `APD`, takeoff minimums `TM`, diverse vector area `DVA`, radar
  minimums `RM`, hot spots `HS`, LAHSO `LAHSO`.
- **No command** - `HI-` and `COPTER` charts, PRM, Category II/III and converging approaches,
  numbered approaches (`VOR-1`), GLS and LOC/NDB approaches (on an "OR" chart, the other parts still
  get commands), AAUP pages, alternate minimums, and any chart or approach type FE-Buddy doesn't
  know yet (the Review tab names it - please report it).

**Info ▸ Alias Command Guide** explains every command for your controllers.

### Telephony tab

Writes `Telephony.txt`, with every operator in the FAA's list; it isn't limited by region.

- **Telephony Data** - FAA Order JO 7340.2, Chapter 3: the ICAO register and the U.S. special call
  signs. Every run downloads the latest, falling back on FE-Buddy's kept copy if it can't. Left out:
  an entry with no three-letter designator or no telephony, and an expired special call sign.
- **Outputs** - the alias file. Each operator gets an `.id` command for its designator and another
  for its telephony: `.idAVA` and `.idAVIANCA` (just one when they're the same, `.idNASA`). The card
  shows the designator, telephony, company and country - for a special call sign, the identifier,
  telephony, agency and expiry. When operators share a command, it shows each card.
- **Virtual Airlines** - add your facility's virtual airlines: 3LD, telephony and virtual
  organization. Each gets the same two commands and a card marked `--VA--`, after the real operators
  that share the command: with DVA / DELTA added, `.idDELTA` shows Delta Air Lines, then yours.
  - **Include the VATSIM-Radar Virtual Airline List** also writes the virtual airlines on
    VATSIM-Radar's community list (about 250), after yours. Every run downloads the latest list;
    ticking the box with no copy downloads it straight away, and **Download the latest list**
    fetches it again. An entry without a three-letter 3LD, a usable telephony and an organization is
    left out.
  - With the list included, a virtual airline exactly the same as one on it (3LD, telephony and
    organization, ignoring case) can't be added. One that differs at all can, and gets its own card,
    so a command can show three or more. One of yours that's already on the list is marked and
    written once.

### Wx Stations tab

A symbol and a label for every US and US-territory station that reports METAR. No Lines or alias
file.

- **What Files Do You Want?** - Symbols, and Text: the ICAO ID, then the IATA ID and site name
  (`KDTW` / `DTW_Detroit/Metro Wayne Cnty`).
- **Station Data** - aviationweather.gov's station list, not NASR. Every run downloads the latest,
  falling back on FE-Buddy's kept copy; the very first run needs an internet connection.
- **Region** - a station is included when it's inside the region.

### vNAS Alias Upload tab

vNAS takes one alias file per facility. This tab writes `Upload_to_vNAS\vNAS_Alias.txt`: the
FE-Buddy alias files ticked for vNAS, then your facility's own. CRC uses the last copy of a command,
so yours win.

- **FE-Buddy Alias Files** - whether each FE-Buddy alias file goes in, as the other tabs stand.
  **Open tab** jumps there to change it.
- **Custom Alias Files** - your own files, merged in the order listed:
  - **Add file…** for a file on this PC, **Add web address** for one on the web (`https://`). On
    GitHub, use the file's own page (with `/blob/` in it) or its Raw link. An address with a password
    or token written into it is refused - choose a credential instead.
  - **Credential** - for a private GitHub repository, a GitHub token that can read it
    ([how to make one](GitHub-Token-Guide.md)).
  - **Check** reads the file now and says how many commands it has, or what's wrong.
- Every run reads the files fresh. A file that can't be read is left out, with a warning: don't
  upload until it's fixed, or its aliases disappear from vNAS.
- A `.FeUseOnly` line in your files moves to the top.
- **Reusing last cycle's upload** as your custom file works: FE-Buddy's section, between its start
  and end lines, is dropped, so only your own aliases carry over.
- A command in your files that another file also has is noted on the Review tab.

Without this sub-service, `vNAS_Alias.txt` is still written whenever an alias file is ticked for
vNAS - with FE-Buddy's aliases only, and a warning.

### File Names tab

Every file the run will write, by folder, with a way to rename any of them.

- Set **Rename Files** to **Yes**, tick a file and type its new name without the extension: `ZOB High`
  becomes `ZOB High.geojson`.
- The list follows the other tabs' settings. A file that a changed setting adds starts ticked with an
  empty name, so the tab turns red until you name or untick it.
- A name can't be empty, contain `\ / : * ? " < > |`, end in a dot or an extension, be a name Windows
  reserves (`CON`, `NUL`, …), be over 100 characters, or match another file's.
- Departures and Arrivals GeoJSON files can't be renamed: there's one per procedure.
- The Review tab, `vNAS_Alias.txt` and `Duplicate_Alias_Commands.txt` use the new names.

### Preview Settings tab

A plain-words summary of the run: the folder, what will be written, the region, and which files go
to vNAS. A tab with a problem blocks the run; unsaved changes are saved when it starts (you're asked
first). **Run AIRAC Service** starts it.

If the cycle has been run before, choose **Overwrite files** (old files this run doesn't write
stay), **Delete all files** (the folder is emptied first - not to the Recycle Bin) or **Cancel**.

### Review tab

Progress, then **Errors**, **Advisories** (output you might expect but won't find, and why),
**Results** for each sub-service (each group of messages has a copy button, handy for a bug report),
and **Output**: how many files were written, with **Open output folder**.

## File Conversions

Converts files you already have into GeoJSON. It looks like AIRAC Service, but every conversion is
always on the rail, and each runs from the button on its own tab. It doesn't need the AIRAC data.

Each tab has **Source Files** - every matching file in a folder (remembered), or files you pick
(forgotten when FE-Buddy closes) - and **CRC ERAM Defaults**. The run button offers to save unsaved
settings first. A record that can't be read is skipped and listed on the Review tab; the rest converts.

### DAT to GeoJSON

FAA `.dat` RADAR Video Maps, one `.geojson` each, with the same name.

- **CRC ERAM Defaults** - Lines only. **Include** (on to start) gives every map the same look;
  untick it to leave the look to CRC.
- **Cropping** - keep only what's within this many NM of the map's point of tangency (its centre).
  Blank converts the whole map. A line crossing the edge is cut there, not dropped. The distance
  applies to every file in the run, so convert maps that need different distances separately.

### SCT2 to GeoJSON

VRC sector files (`.sct2` or `.sct`), each into a folder of its own:

| File | From |
|---|---|
| `ARTCC`, `ARTCC-HIGH`, `ARTCC-LOW` | the boundary sections |
| `LOW-AIRWAY`, `HIGH-AIRWAY` | the airway sections |
| `GEO` | `[GEO]` |
| `SID\<diagram>`, `STAR\<diagram>` | one file per SID and STAR diagram |
| `LABELS` | `[LABELS]` |
| `REGIONS` | `[REGIONS]`, as filled areas |

Airports, VORs, NDBs and fixes aren't written, but their names are used to find coordinates. Lines
are joined back up and repeats dropped, so files stay small and dashes stay dashed. CRC ERAM
Defaults has a Lines panel and a Labels panel. VRC colours aren't carried over.

### ERAM to GeoJSON

The `Geomaps.xml` of an ERAM adaptation export, into an `ERAM_TO_GEOJSON` folder, laid out as the
original ERAM_2_GEOJSON tool did. Each map is named after its `GeomapId` and button label:
`CENTER_CENTER-MAP`.

- **Source Files** - `Geomaps.xml`, or a folder (the whole unzipped export works). One Geomaps file
  per run.
- **Output Layout:**
  - **By Filters** - a folder per set of filters: `Filter_01\Filter_01_Lines.geojson`,
    `Multi-Filter_02_03_08\…`. The fewest files.
  - **By Attributes** (the default) - a file per look, named after it. The most files, and the
    easiest to pick apart and rename for your GeoMaps.
  - **Raw** - one file per map, every feature carrying its own look. A reference to check the others
    against in CRC.
- **CRC ERAM Defaults Source** - **From the XML**, **From the XML, filling gaps from the card**, or
  **From the card only**. Hover each for an example. SAA objects have no BCG or filters of their own,
  so filling gaps from the card is what gives them a look. The CRC ERAM Defaults card only shows
  when it is one of the sources.
- **FE-Buddy Properties** - optional `feb.*` fields saying where each feature came from.
- With the export's `ConsoleCommandControl.xml` beside `Geomaps.xml`, it also writes
  `ConsoleCommandControl.txt`: the console's brightness and filter menus, and the maps on each.
- **Each run empties `ERAM_TO_GEOJSON` first** (it asks before it does). Move out anything you want
  to keep.

A value CRC can't draw (a `DME` symbol, say) is left out, and the Review tab says what CRC will draw
instead. Text ERAM keeps hidden is left out, and ERAM's colours aren't carried over.

## Output files

Every AIRAC Service run of a cycle writes into one `AIRAC_<cycle>` folder in your output folder
(Settings ▸ Default Output Directory, inside `FE-Buddy_Output` if that's on):

```
<output folder>\
└── FE-Buddy_Output\
    ├── AIRAC_2610\
    │   ├── Duplicate_Alias_Commands.txt   (when the run wrote an alias file)
    │   ├── Aliases\                       Airports.txt, Airways.txt, Arrivals.txt, Departures.txt,
    │   │                                  Navaids.txt, Faa_Chart_Recall.txt, Telephony.txt
    │   ├── Geojson\                       every GeoJSON file; Departures and Arrivals in
    │   │                                  <ARTCC>\<airport>\ folders
    │   ├── Publication_Docs\              Procedure_Changes.md, Procedures.json
    │   └── Upload_to_vNAS\
    │       ├── vNAS_Alias.txt             the alias files marked for vNAS, then your own
    │       └── Geojson\                   the GeoJSON files marked for vNAS
    ├── DAT to GeoJSON\                    one .geojson per map
    ├── SCT2 to GeoJSON\                   a folder per sector file
    └── ERAM_TO_GEOJSON\                   emptied by every ERAM run
```

- A GeoJSON file marked for vNAS goes to `Upload_to_vNAS` *instead of* `Geojson`. An alias file
  marked for vNAS stays in `Aliases` and is copied into `vNAS_Alias.txt` *as well*.
- A folder is only made when something goes in it, and an empty file isn't written.
- The conversions replace any file of the same name.

**`Duplicate_Alias_Commands.txt`** lists every alias command used by more than one line across the
run's alias files - CRC can only run one of them - grouped by ARTCC: your Facility Profile ARTCC
first, the others alphabetically, then `TELEPHONY`, then `OTHER` (airways, NAVAIDs, and anything
with no known ARTCC). The Review tab warns when there are any.

## Map

A map for checking GeoJSON files and setting your Region of Interest. **Set ROI…** in Settings and
**Pick on map…** on a tab open the same map in a window.

- **Toolbar** - Edit ROI, zoom, Home, make this view your home, fit every layer, zoom to the ROI, the
  base map, and the side panel. A map opens where the last one was left.
- **Base map** - US states, coastlines and lakes, gridlines, and their opacity. Your choices apply to
  every map.
- **Default Region of Interest** - **Edit ROI**, drag a box (or type its corners), then **Save**.
  **Shift + drag** draws and saves in one go. A box can't cross the 180° meridian. In a map window,
  the button is **Use this ROI**.
- **AIRAC Cycle** - pick a cycle, then:
  - **Live data** - ARTCC boundaries, towered airports and VOR / VORTAC / TACANs, straight from the
    FAA data, no run needed.
  - **Output of the AIRAC run** - tick any GeoJSON file that cycle's run wrote. Pick another cycle
    and the same files from its run are shown.
- **Your Files** - **Load GeoJSON…** opens files from anywhere. One that can't be drawn says why.
- **Home View** - **Use current view** sets where **Home** takes you (and where the very first map
  opens); **Reset** goes back to the contiguous US.

## Settings

Press **Save** at the top to keep your changes. **Export…** and **Import…** share your setup with
someone else - every setting except this PC's own (update channel, credentials, which News you've
read). An import shows what will change first, and keeps your old settings as
`UserConfig.before-import.json`.

- **Facility Profile** - your ARTCC (ticked to start on the tabs that pick ARTCCs, and listed first
  by Procedures and the duplicate report), and the
  **Default Output Directory** - the Desktop to start - with **Add a FE-Buddy_Output folder inside
  that directory** (on).
- **Default Region of Interest** - **Set ROI…** opens the map. Every tab uses it unless it overrides
  it.
- **GeoJSON Files** - **Maximum Coordinate Precision**: 5, 6 (the default) or 7 decimal places, or
  **Do not round**. **File Layout**: single line, or pretty print to read in a text editor.
- **Credentials** - tokens and passwords for protected downloads, such as a private GitHub alias
  file. Kept in Windows Credential Manager, saved as soon as you change them, and removed when
  FE-Buddy is uninstalled. See the [GitHub token guide](GitHub-Token-Guide.md).
- **FE-Buddy's GitHub Requests** - advanced: a GitHub token for FE-Buddy's own update and News
  checks, to get past GitHub's limit of 60 requests an hour. Most people never need it.
- **Updates** - the update channel (Stable is right for almost everyone), **Check for updates now**,
  and the Releases page.
- **Reset FE-Buddy…** - start over as if newly installed. Downloaded data, logs and settings backups
  always go; you choose whether to keep your settings and credentials. Your output folder is never
  touched.
- **Uninstall FE-Buddy…** (installed copies only) - the same as uninstalling it from Windows
  Settings, with the option to save a copy of your settings first.

## Info

- **What's New in v3.0?** - what changed since FE-Buddy 2.x, including the new chart recall commands.
- **Alias Command Guide** - every alias command FE-Buddy makes, explained for controllers with real
  examples.
  - **Export FE-Buddy Alias Command Guide** saves it as a web page, Markdown or both, for your
    facility's website. The web page's colours and fonts are variables at the top of its style
    sheet, and each section can be deleted on its own.
  - **Export Alias Command Practice** saves a web page that quizzes your controllers: it names a
    chart or procedure and what to do with it, they type the command, and it turns green or red,
    says what looks wrong (*Looks like you forgot to include the full runway number 16*), and shows
    how the right command is built.
- Links to this guide, the FAQ, the change log and the issue tracker.
