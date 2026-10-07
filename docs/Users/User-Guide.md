# User guide

Every screen and option in FE-Buddy 3.0. New here? Start with [Getting started](Getting-Started.md).
Unfamiliar words are in the [glossary](Glossary.md).

**Contents:** [The window](#the-window) · [Dashboard](#dashboard) · [AIRAC Service](#airac-service) ·
[File Conversions](#file-conversions) · [Output files](#output-files) · [Map](#map) ·
[Settings](#settings) · [Info](#info)

---

## The window

- **Menu (left)** - the arrow at its top collapses it to icons.
- **Systems (foot of the menu)** - one dot for the worst of Internet, AIRAC data and Updates; click
  it for a dot each. Green is fine, amber needs a look or is still working, red isn't working.
  A cycle whose d-TPP Metafile isn't out yet turns AIRAC data amber; when that's the only thing, the
  box says *Ready except next d-TPP Metafile*. **Re-check** runs the checks again.
- **Top** - the version (hover it for the update status) and the AIRAC status. When there's an
  update, a red **Update available!** badge appears beside the version: click it, then **Update
  now**. FE-Buddy updates and reopens, keeping your settings.
- **Bottom** - the time in Zulu.

## Dashboard

- **Top card** - what FE-Buddy is, the Discord link, and when the next AIRAC cycle starts.
- **News** - posts from the FE-Buddy team. The button lights up when there's one you haven't seen.
- **Activity log** - what FE-Buddy did this session, folded away to start: a chip opens it, filtered
  by level. The full log is in `%APPDATA%\FE-Buddy\Logs`.

## AIRAC Service

Where you make files, every one ready for vNAS. The tabs run down the left:

| Tab | What it is |
|---|---|
| **General** | The cycle, which sub-services run, and the files each makes. |
| One per sub-service | Its settings. Greyed out while it's left out on General. |
| **File Names** | Every file the run will write, and new names for most of them. |
| **Preview Settings** | What the run will do, and the **Run AIRAC Service** button. |
| **Review** | Appears when you run: progress, then what the run did. |

The screen waits until the AIRAC data has downloaded. It opens at **General** each time you choose it
in the menu; edits on the other tabs are kept.

Each card's title row tags the files its settings change: **Alias**, **GeoJSON**, **Changes**
(`Procedure_Changes.md`) and **JSON** (`Procedures.json`), in the same colours as the General tab's
columns. A tag is struck through while that file is off, and a card whose files are all off is
greyed out. Where a card changes two files differently, a line for each says how.

Each sub-service tab but Concatenate Aliases starts with **What You'll Get**: what its files will
hold, one filter per line. **AND** narrows the lines above, **OR** is another way in, and **PLUS**
adds something on top. Where its files get different things, each file has its own lines.

### Saving

Each tab saves on its own: **Save**, **Undo all changes** (drops unsaved edits) and **Undo last
save** are in the bar above and below it. An **amber** dot beside a tab's name means unsaved edits;
they're saved, after asking, when you run. A **red** dot means something to fix: the tab lists every
problem in a red box at its top and outlines each card concerned in red. Hover a red box for why.

### General tab

- **Cycle** - Previous, Current or Next, with its effective date and status: *ready*, *partial*
  (the [d-TPP Metafile](#procedures-tab) isn't out yet, so Procedures writes nothing; hover the row
  for when to expect it), *failed*, or *not yet published* (the FAA releases a cycle a few weeks
  early).
- **Sub-Services** - tick **Include** for each sub-service to run, and the files it makes:
  **Alias**, **GeoJSON**, and Procedures' **Procedure Changes** and **Procedures JSON**. All are on
  until you first save. Unticking **Include** unticks its files, and ticking it ticks them all; untick
  its last file and it's left out, tick one and it's back in. One left out is greyed out in the list
  to the left and keeps its settings. Concatenate Aliases comes in by itself while an alias file is
  ticked.

### The cards most tabs share

The tabs that write GeoJSON share these cards; Procedures, Telephony and Concatenate Aliases have
their own.

- **Outputs** - which of the tab's files are on; change them on the General tab.
- **What Files Do You Want?** - which of **Lines**, **Symbols** and **Text** to write. Symbols with
  exactly the same properties are written as one feature (a MultiPoint), so Symbols files stay
  small; labels are always one feature each.
- **FE-Buddy Properties** - optional `feb.*` fields that tell you what each object is in a GeoJSON
  viewer. CRC ignores them. One that differs from point to point, such as an ID, keeps each symbol a
  feature of its own.
- **Region of Interest** - Settings' **Default Region of Interest**, unless you tick **Give *tab* its
  own region** and type the corners or **Pick on map…**. With no region (the card says so in amber),
  nothing is left out.
- **CRC ERAM Defaults** - how CRC draws a file, written as a hidden feature at its top. At the top of
  the card, choose which GeoJSON files get them: **No CRC-ERAM defaults** (to start), **Every GeoJSON
  file from this tab**, or **Only the files I tick**. Then fill in every box shown; the lists offer
  only values CRC accepts. Airways, NAVAIDs, ARTCC
  Boundaries and Fixes can ask for a set per class (High or Low, NAVAID type, altitude, fix group),
  so their files can look different.

### ARTCC Boundaries tab

Each ARTCC's boundary as lines. No Symbols, Text or alias file.

- **ARTCCs** - tick the ones you want; none means all.
- **File Layout** - **High and Low** (the default; an UNLIMITED boundary goes in both), **High, Low
  and Unlimited**, or **One file per ARTCC and altitude** (`ARTCC-Boundary_ZOB-HIGH_Lines`, …), to
  style your own ARTCC apart from its neighbours.
- **Split GeoJSON at the Antimeridian** - leave it on: ZAK, ZAN, ZAP and ZOA cross ±180°.
- ZAK, ZAP and ZWY have two overlapping oceanic rings at one altitude; the `feb.type` property tells
  them apart.
- **Region** - a boundary is clipped at the region's edge.

### Airports tab

- **Files** - Runways (lines), Airports symbols, and Airports text (FAA ID and name). An airport is
  included when its reference point is in the region.
- **`Airports.txt`** - an `.apt` command for each FAA ID, and the ICAO ID if it's different, showing
  the airport's card in CRC: name, tower type, ARTCC, longest runway, elevation, CTAF, weather, hours
  and airspace class. It covers every open airport; the region only limits the GeoJSON.

### Airways tab

- **Split into** - **High and Low** (`Airways_High`, `Airways_Low`) or **Airway types** (`Airways_J`,
  `Airways_V`, …). For High and Low, the **High and Low Files** card puts each airway type in High,
  Low or Both, by type rather than altitude: J and Q start in High, V and T in Low, and any other type
  must be chosen. For no Airways GeoJSON, untick its **GeoJSON** box on the General tab.
- **`Airways.txt`** - a command per airway that shows its fixes, e.g. `.J3F`: **Every airway** or
  **Only airways that cross the region**.
- **Airway Types to Include** - untick one to leave it out of the GeoJSON and the alias file.
- **Buffer Airway Waypoints** (off to start) - stops lines short of each waypoint so they don't run
  through the symbols: 2.5 NM around fixes and 5 NM around NAVAIDs to start.
- **Split GeoJSON at the Antimeridian** - leave it on.
- **Region** - an airway is included when its line crosses the region, and is clipped to it.

### Departures and Arrivals tabs

Departures covers SIDs, and ODPs unless you untick **Include obstacle departures (ODPs)**. Arrivals
covers STARs. Otherwise the two tabs work the same.

- **Files** - Lines (shared segments drawn once), Symbols and Text for each procedure, in a folder
  per airport: `Geojson\<ARTCC>\<airport>\<airport>_<procedure>_Lines.geojson`. Arrivals add `STAR`
  (`LAS_BLAID_STAR_Lines.geojson`).
- **Alias file** - `Departures.txt` or `Arrivals.txt`: a command per airport and procedure that draws
  its fixes. A STAR lists its transitions first, then its bodies.
- **Routes** - each airport gets the bodies the FAA's data assigns it (all of them when the
  procedure has no assignments), plus every transition. When the data assigns bodies to some
  airports but not others, a departure's left-out airport gets only the transitions, and a STAR's
  gets every body.
- **ARTCCs** - tick the ones you want; none means all. A STAR serving airports in two ARTCCs goes
  with each airport's own.
- **Amendment Date** - every procedure, or only those amended in the last *N* cycles (counting the
  selected one), the last *N* days, or since a date.
- **Which Departures (or STARs) the Region Keeps** - every procedure for an airport in the region,
  or any procedure with a fix in it. The region limits the alias file too.
- **Names** - the FAA computer code without its version (`DOTSS2.DOTSS` is `DOTSS`; a STAR's runs the
  other way, so `AALAN.BLAID2` is `BLAID`), or the name without punctuation when there's no code
  (`O'HARE` is `OHARE`).
- **CRC ERAM Defaults** - one set for every procedure's Lines, Symbols or Text.

### NAVAIDs tab

- **NAVAID Types** - all on to start; an unticked type is left out of the GeoJSON and the alias
  file. NAVAIDs marked SHUTDOWN are always left out.
- **File Layout** - *All in one file* (`NAVAIDs_Symbols`, `NAVAIDs_Text`) or *One file per NAVAID
  type* (`NAVAIDs_VORTACs_Symbols`, …).
- **Files** - Symbols, and Text: the identifier, then the name and type (`CGT` /
  `CHICAGO HEIGHTS VORTAC`).
- **NAVAID Symbol Style** - with *All in one file* and CRC-ERAM defaults on the Symbols file: **By
  NAVAID type** (you choose the fan markers' symbol), or **The same symbol for every NAVAID**.
- **`Navaids.txt`** - a `.nav` command for each identifier and each name, showing the name, type,
  frequency and ARTCCs. NAVAIDs that share an identifier (`ABQ` is a VORTAC and a VOT) share one
  command. It covers every NAVAID of the ticked types; the region only limits the GeoJSON.

### Fixes tab

A symbol and a label (its identifier) for every NASR fix. No Lines or alias file.

- **File Layout** - all fixes in one file (the default: `Fix_Symbols`, `Fix_Text`), or one file per
  fix use (`Fix_WYPNT_Symbols`), per chart (`Fix_ENROUTE-LOW_Symbols`; a fix on several charts goes
  in each, one on none in `Fix_NO-CHART_Symbols`), or per chart + fix use pair you add under
  **Combinations** (`Fix_ENROUTE-LOW-WYPNT_Symbols`). Untick any fix uses or charts you don't want.
- **Region** - a fix is included when it's inside the region.

### Procedures tab

Built from the FAA's **d-TPP Metafile** - its index of approach plates, SIDs, STARs, airport
diagrams and the rest - not NASR. The FAA posts it only 15-18 days before a cycle starts; until then
the cycle is *partial*, and the run goes ahead without Procedures' files.

- **Outputs** (on or off on the General tab) - `Procedure_Changes.md` (what changed this cycle) and
  `Procedures.json` (every current chart) for the airports you pick, and `Faa_Chart_Recall.txt`
  ([chart recall commands](#faa-chart-recall-commands) for every chart at every airport, not just the
  ones you pick).
- **d-TPP Data** - the cycle's metafile, and whether a deleted chart can be linked to its last copy
  (that needs the previous cycle's metafile).
- **What the documents cover** - these add up: every airport of the ticked **Facilities**; the
  **Airports** you list (FAA or ICAO ID), plus every airport in the region if ticked; **Procedures at
  Any Airport**, by name; and **Airport + Procedure** pairs. **Chart Types** sets which kinds of chart
  a whole airport adds; a procedure picked by name is always included.
- **Airports** takes a list: type or paste up to 100 IDs, separated by spaces, commas or new lines,
  and press Enter or **Add**. Any it can't add stay in the box, with why underneath: not an airport
  this cycle, already listed, or already included by a ticked facility or the region.
- **Procedures.json Fields** - the optional fields the JSON carries.
- **Region of Interest** - only decides which airports *Also every airport inside the region* adds.
  Nothing is clipped.

**`Procedure_Changes.md`** has a section per facility: yours first, then the rest alphabetically,
then "Other". Each lists the airports that changed, Class B first. A changed chart links to the FAA's
comparison PDF, a new one to its chart, and a deleted one to its last chart. A STAR at several
airports is listed under each, with "Also serves" naming the others. **`Procedures.json`** lists the
same airports' current charts.

#### FAA Chart Recall commands

One `.OPENURL` command per page of every current chart:

```
.dtwI22Lc .OPENURL https://aeronav.faa.gov/d-tpp/2609/00058IL22L.PDF  ; DETROIT METRO WAYNE COUNTY-ILS OR LOC RWY 22L
```

A command is `.`, the airport's FAA ID in lower case, the chart's code, then `c`; page 2 onwards adds
the page number (`.dtwHHOWEc2`). An approach's code is its type, variant and runway, one command per
"OR" part (`ILS Y OR LOC Y RWY 22L` is `.dtwIY22Lc` and `.dtwLY22Lc`); a departure's or STAR's is its
name without the version (`JALEX3.JALEX` is `JALEX`); the airport diagram is `APD`. **Info ▸ Alias
Command Guide** has every code, with examples for your controllers.

There's no command for `HI-` and `COPTER` charts, PRM, Category II/III, converging and numbered
(`VOR-1`) approaches, GLS and LOC/NDB parts, AAUP pages and alternate minimums. A chart FE-Buddy
doesn't know yet is named on the Review tab - please report it.

### Telephony tab

Writes `Telephony.txt` for every operator in the FAA's list: an `.id` command for its 3LD and one
for its telephony (`.idAVA`, `.idAVIANCA`; just one when they're the same, `.idNASA`), each showing
the operator's card. Operators that share a command show a card each. The region doesn't apply.

- **Virtual Airlines** - add your facility's own (3LD, telephony and virtual organization). Each gets
  the same two commands and a card marked `--VA--`, after any real operator sharing the command.
  - **Include the VATSIM-Radar Virtual Airline List** adds its ~700 virtual airlines after yours,
    downloaded every run (**Download the latest list** fetches it now). One of yours exactly the same
    as one on the list can't be added; one that differs at all gets its own card.
- **Telephony Data** - FAA Order JO 7340.2, Chapter 3, downloaded every run. Entries with no
  three-letter designator or no telephony, and expired special call signs, are left out.

### Wx Stations tab

A symbol and a label for every US and US-territory station that reports METAR. No Lines or alias
file.

- **What Files Do You Want?** - Symbols, and Text: the ICAO ID, then the IATA ID and site name
  (`KDTW` / `DTW_Detroit/Metro Wayne Cnty`).
- **Station Data** - aviationweather.gov's station list, not NASR, downloaded every run.
- **Region** - a station is included when it's inside the region.

### Concatenate Aliases tab

vNAS takes one alias file per facility, so this tab writes `Aliases\Combined_Alias.txt`: every alias
file the run makes, then your facility's own, which win because CRC uses the last copy of a command.
Upload that one file. Untick **Combine the alias files into one file for vNAS** to leave each alias
file on its own. The tab is greyed out while no sub-service makes an alias file.

- **FE-Buddy Alias Files** - which of FE-Buddy's alias files go in, as the General tab stands.
- **Custom Alias Files** - your own, merged in the order listed. Each is one line - its name, how it
  stands, **Check** and the order buttons - until you click it open for its address and credential:
  - **Add file…** for a file on this PC (it must hold alias commands), or **Add web address**. On
    GitHub, paste the file's page or its Raw link; FE-Buddy shows it as the Raw link. Never put a
    password or token in the address: for a private repository, choose a **Credential**
    ([GitHub token guide](GitHub-Token-Guide.md)).
  - **Check** reads the file now. If GitHub won't show it, Check asks whether the repository is
    private and points you to the token guide - or, with a credential chosen, to its
    [troubleshooting](GitHub-Token-Guide.md#if-something-goes-wrong).
  - Every run reads them fresh. One that can't be read is left out with a warning: don't upload
    until it's fixed. With none at all, the Review tab warns that uploading would remove your
    facility's own aliases from vNAS.
  - Last cycle's `Combined_Alias.txt` works as a custom file: FE-Buddy's section in it is dropped. A
    `.FeUseOnly` line moves to the top.

### File Names tab

Every file the run will write, by folder, following the other tabs' settings.

- To rename some, choose **Yes - rename some files**. Every file starts ticked: untick the ones that
  keep FE-Buddy's name (**Rename none** unticks them all) and type a new name, without the extension,
  for the rest - `ZOB High` becomes `ZOB High.geojson`. A ticked file with no name, such as one a
  later setting adds, turns the tab red.
- A name can't be empty, contain `\ / : * ? " < > |`, end in a dot or an extension, be a name Windows
  reserves (`CON`, `NUL`, …), be over 100 characters, or match another file's.
- Departures and Arrivals GeoJSON files can't be renamed: there's one per procedure.

### Preview Settings tab

What the run will do, tab by tab, each starting with its What You'll Get lines. A red tab blocks
the run; unsaved changes are saved first (you're asked). **Run AIRAC Service** starts it. If the
cycle's folder already has files, choose **Overwrite files** (other old files stay), **Delete all
files** (emptied first, not to the Recycle Bin) or **Cancel**.

**Duplicate Alias Commands** (while an alias file is in the run) - what the run does when more than
one line of the alias files uses a command, since CRC can only run one of them:

- **List them in Duplicate_Alias_Commands.txt** (to start).
- **Stop and let me choose, before the alias files are saved** - the run opens a window with each
  command's lines. Choose **Keep** for one, and **Leave out** or **Rename to** for the others; a new
  command can't be one the run already uses, your custom alias files' included. **Save choices and
  finish the run** carries on; **Stop the run** ends it with no alias file and no
  `Combined_Alias.txt` saved.

Your choices are saved and made in every later run, whichever option is picked, so only a new
duplicate asks again. **Saved Choices** lists them; remove one to be asked again, and the Review tab
says when one no longer matches a duplicate.

### Review tab

Progress, then **Errors**, **Advisories** (output you might expect but won't find, and why),
**Results** for each sub-service (each group of messages has a copy button), and **Output**: how
many files were written, with **Open output folder**.

## File Conversions

Converts files you already have into GeoJSON; it doesn't need the AIRAC data. Pick the **Source**
(for ERAM, then the **File**) and the **Output**, then **Continue** to that conversion's page; the
back arrow returns to the picker. Each page has **Source Files** - every matching file in a folder
(remembered), or files you pick (forgotten when FE-Buddy closes) - and **CRC ERAM Defaults**,
written into every file unless you untick **Include**. **Convert…** at the foot runs it (offering to
save first), and the results show under it. A record that can't be read is skipped and listed there.

### DAT to GeoJSON

Source **FAA Radar Video Map .dat files**: FAA `.dat` RADAR Video Maps, one `.geojson` each, with the
same name. CRC ERAM Defaults: Lines only.

- **Cropping** - keep only what's within this many NM of the map's point of tangency; blank converts
  the whole map. A line crossing the edge is cut there, not dropped. The distance applies to every
  file in the run, so convert maps that need different distances separately.

### SCT2 to GeoJSON

Source **Legacy Sector File (.sct2)**: VRC sector files (`.sct2` or `.sct`), each into a folder of
its own: `ARTCC`, `ARTCC-HIGH`, `ARTCC-LOW`, `LOW-AIRWAY`, `HIGH-AIRWAY`, `GEO`, `LABELS` and
`REGIONS` (as filled areas), plus a file per diagram in `SID\` and `STAR\`. Airports, VORs, NDBs and
fixes aren't written; their names only locate points. Lines are joined back up, so files stay small
and dashes stay dashed. CRC ERAM Defaults has a Lines panel and a Labels panel. VRC colours aren't
carried over.

### ERAM to GeoJSON

Source **FAA ERAM Adaptation Files**, file **Geomaps.xml**: the `Geomaps.xml` of an ERAM adaptation
export, into an `ERAM_TO_GEOJSON` folder. Each map is named after its `GeomapId` and button label:
`CENTER_CENTER-MAP`.

- **Source Files** - `Geomaps.xml`, or a folder (the whole unzipped export works). One per run.
- **Output Layout** (hover each for the files it writes):
  - **By Filters** - a folder per set of filters (`Filter_01\Filter_01_Lines.geojson`). The fewest
    files.
  - **By Attributes** (the default) - a file per look, named after it. The most files, and the
    easiest to rename for your GeoMaps.
  - **Raw** - one file per map, one feature per element, each carrying its own look. A reference to
    check the others against in CRC.
  - **Raw Plus** - Raw, but with far fewer features.

  In every layout but Raw, lines that share every property are joined and such symbols become one
  feature (a MultiPoint); labels stay one feature each.
- **CRC ERAM Defaults Source** - **From the XML**, **From the XML, filling gaps from the card** (what
  gives SAA objects, which have no BCG or filters, a look) or **From the card only**. Hover each for
  an example. The CRC ERAM Defaults card shows only when the card is a source.
- **FE-Buddy Properties** - optional `feb.*` fields saying where each feature came from.
- With the export's `ConsoleCommandControl.xml` beside `Geomaps.xml`, it also writes
  `ConsoleCommandControl.txt`: the console's brightness and filter menus, and the maps on each.
- **Each run empties `ERAM_TO_GEOJSON` first** (it asks before it does). Move out anything you want
  to keep.

A value CRC can't draw (a `DME` symbol, say) is left out, and the results say what CRC will draw
instead. Text ERAM keeps hidden is left out, and ERAM's colours aren't carried over.

## Output files

Every AIRAC Service run of a cycle writes into one `AIRAC_<cycle>` folder in your output folder
(Settings ▸ Default Output Directory, inside `FE-Buddy_Output` if that's on):

```
<output folder>\
└── FE-Buddy_Output\
    ├── AIRAC_2610\
    │   ├── Duplicate_Alias_Commands.txt   (when the run wrote an alias file)
    │   ├── Aliases\                       every alias file, and Combined_Alias.txt
    │   ├── Geojson\                       every GeoJSON file
    │   └── Publication_Docs\              Procedure_Changes.md, Procedures.json
    ├── DAT to GeoJSON\
    ├── SCT2 to GeoJSON\
    └── ERAM_TO_GEOJSON\                   emptied by every ERAM run
```

- Every file is ready for vNAS; for the aliases, upload only `Combined_Alias.txt`.
- A folder is only made when something goes in it, and an empty file isn't written.
- The conversions replace any file of the same name.

**`Duplicate_Alias_Commands.txt`** lists every alias command that more than one line uses across the
run's alias files (CRC runs only one of them), grouped by ARTCC - yours first - then `TELEPHONY` and
`OTHER`. The Review tab warns when there are any. A duplicate your choices settle (see
[Preview Settings](#preview-settings-tab)) isn't listed.

## Map

A map for checking GeoJSON files and setting your Region of Interest. **Set ROI…** in Settings and
**Pick on map…** on a tab open the same map in a window.

- **Toolbar** - hover a button for what it does. A map opens where the last one was left.
- **Zoom** - the box beside the zoom buttons shows the zoom as a percentage of your home view
  (Home is 100%); type one and press Enter. The wheel zooms 25% a notch, or 1% with **Shift** held.
- **Base map** - US states, coastlines and lakes, gridlines, and their opacity. Your choices apply to
  every map.
- **Default Region of Interest** - **Edit ROI**, drag a box (or type its corners), then **Save**.
  **Shift + drag** draws and saves in one go. A box can't cross the 180° meridian. In a map window,
  the button is **Use this ROI**.
- **AIRAC Cycle** - pick a cycle, then:
  - **Live data** - ARTCC boundaries, towered airports and VOR / VORTAC / TACANs, straight from the
    FAA data, no run needed.
  - **Output of the AIRAC run** - the gear picks any GeoJSON file that cycle's run wrote. Pick
    another cycle and the same files from its run are shown.
- **Your Files** - **Load GeoJSON…** opens files from anywhere. One that can't be drawn says why.
  **Show all files** shows or hides them all; right-click a file for **Deselect all files except this
  one**. **Reset** takes them all off the map.
- **Home View** - **Use current view** sets where **Home** takes you (and where the very first map
  opens); **Reset** goes back to the contiguous US.

## Settings

Press **Save** at the top to keep your changes (credentials save at once). **Export…** and
**Import…** share your setup, leaving out this PC's own (update channel, credentials, which News
you've read). An import shows what will change first and keeps your old settings as
`UserConfig.before-import.json`.

- **Facility Profile** - your ARTCC (ticked to start on the tabs that pick ARTCCs, and listed first
  in reports), and the **Default Output Directory** (the Desktop to start, with a `FE-Buddy_Output`
  folder inside unless you untick it).
- **Default Region of Interest** - **Set ROI…** opens the map. Every tab uses it unless it overrides
  it.
- **GeoJSON Files** - **Maximum Coordinate Precision**: 5, 6 (the default) or 7 decimal places, or
  **Do not round**. **File Layout**: single line, or pretty print to read in a text editor.
- **Credentials** - tokens and passwords for protected downloads, such as a private GitHub alias
  file. Kept in Windows Credential Manager and removed when FE-Buddy is uninstalled. See the
  [GitHub token guide](GitHub-Token-Guide.md).
- **FE-Buddy's GitHub Requests** - advanced: a GitHub token for FE-Buddy's own update and News
  checks, to get past GitHub's limit of 60 requests an hour. Most people never need it.
- **Updates** - the update channel (Stable suits almost everyone). It's set when FE-Buddy first
  starts and only changes here - updating never changes it. **Check for updates now** and **Open the
  Releases Page**.
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
    facility's website.
  - **Export Alias Command Practice** saves a web page that quizzes your controllers: they type the
    command for a chart or procedure, and it says what's wrong and how the right one is built.
- Links to this guide, the FAQ, the change log and the issue tracker.
