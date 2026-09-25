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
| **General** | Which cycle, and which sub-services (Airports, Airways, Departures). Always there. |
| **Airports / Airways / Departures** | One tab per sub-service you ticked, with its settings. |
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
one must stay on; to make nothing for a sub-service, untick it on the General tab instead.

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

**CRC ERAM Defaults** - how CRC should draw the file, written as one hidden "defaults" feature at
the top of each file. There is a panel per file (Lines, Symbols, Text); untick **Include** on a
panel to leave that file without defaults. FE-Buddy never guesses these values: every box on an
included panel must be filled in.

| Field | Values | Used on |
|---|---|---|
| BCG | 1-40 | all |
| Filters | one or more of 0-40, picked from a list | all |
| Style | Lines: solid, shortDashed, longDashed, longDashShortDash. Symbols: a CRC symbol name (vor, ndb, airport, rnav, …) | Lines, Symbols |
| Thickness | 1-3 | Lines |
| Size | Symbols 1-4, Text 0-5 | Symbols, Text |
| Underline, Opaque | Yes / No | Text |
| X offset, Y offset | any whole number | Text |

Airways has one column per altitude class (High, Low, Other); Airports and Departures have one.

**Region of Interest** - by default a run uses the **Default Region of Interest** from Settings.
Tick **Override the default ROI for <sub-service>** to give it its own box: type the four corners or
press **Pick on map…**. The note under the checkbox says which region applies when the override
is off. With no region at all, the run covers the whole country.

### Airports tab

- **Outputs:** GeoJSON, and `Airports.txt` - one alias command per airport's FAA ID, and one per
  ICAO ID where there is one. The alias file always covers every open airport; the region only
  limits the GeoJSON.
- **Files:** *Runways - Lines* (each airport's runway centrelines), *Airports - Symbols* (one per
  airport, at its reference point), *Airports - Text* (FAA ID and name).
- **FE-Buddy properties:** `faaId`, `icaoId`, `name`, `elev`, `respArtcc`, `tfcPtrnAlt`, `fssId`,
  `twrType`, `rwyId`.
- **Region:** an airport is included when its reference point is inside the region.

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
- An airway with a waypoint FE-Buddy cannot locate is left out entirely (a half-drawn airway is
  worse than none); the Review tab says which and why.

### Departures tab

- **Outputs:** GeoJSON, and `Departures.txt` - a command per airport and procedure that draws the
  procedure's points.
- **Files:** Lines (each airport's procedure, shared segments drawn once), Symbols (each point),
  Text (each point's identifier). They are written per airport:
  `Departure Procedures\<ARTCC>\<airport>\<airport>_<procedure>_Lines.geojson` (and `_Symbols`,
  `_Text`).
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
- Procedures are named by their FAA computer code without the amendment digit (`DOTSS2.DOTSS` is
  `DOTSS`), or by their name with punctuation removed when there is no code (`O'HARE` is `OHARE`).

### Preview Settings tab

A plain-words summary of every tab: what will be written, what it covers, and which region
applies. It warns if a tab has something to fix (the run is blocked until you do) or unsaved
changes (they are saved when the run starts). **Run AIRAC Service** starts the run.

### Review tab

- **Progress** - a line per sub-service: waiting, working, finished or failed.
- **Errors** - anything that stopped part of the run.
- **Advisories** - output you might expect but will not find, and why (for example "nothing
  matched your filters").
- **Results** - per sub-service, what it produced, with its warnings and routine messages each
  behind a **Show** button.
- **Output** - every file written (collapsed to a count; a Departures run writes thousands) and
  **Open output folder**.

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

### vERAM to GeoJSON tab

Converts vERAM GeoMaps XML files into GeoJSON: a folder per GeoMaps file, named after it, with a
folder inside for each GeoMap.

- **Source Files** - the same as on the other tabs: a remembered folder (every `.xml` in it) or
  files you pick.
- **Output Layout** - how the files are split:
  - **GeoMapObject Description** - a file per GeoMapObject, named after its description. Objects
    that share a description share a file when their defaults agree; otherwise the later one gets
    a numbered file (`VIDEO (2)`).
  - **Filter Index and Similar Attributes** - files grouped by filter, TDM setting, kind and look,
    e.g. `FILTER 05\FILTER 05__TDM F__Line__BCG 3__Style solid__Thickness 1`. An element with
    several filters goes under `MULTI FILTERS\`; one whose look cannot be fully worked out goes
    under `MISSING DEFAULTS\`. Everything in a file draws the same way.
- **CRC ERAM Defaults Source** - where each file's CRC defaults come from:
  - **From the XML** - carry over as much as possible: each object's own Line, Symbol and Text
    defaults, and each element's own overrides.
  - **From the XML, filling gaps from the card** - the same, but an object with no usable defaults
    of its own takes the CRC ERAM Defaults on the tab.
  - **From the card only** - ignore the XML's styling and use the tab's CRC ERAM Defaults for
    everything.
- **CRC ERAM Defaults** - Lines, Symbols and Text panels. They only show, and only need filling
  in, when the card is one of the sources.
- **Convert GeoMaps** - saves any unsaved settings (you are asked first) and runs.

vERAM's style names become CRC's (`Solid` → `solid`, `Vor` → `vor`), and every value is checked
against what CRC can draw. An object whose defaults are missing, incomplete or invalid is listed
on the Review tab; a value CRC cannot draw is left out, so that feature takes its file's default.
Line segments that meet are joined back into lines, including ones written backwards.

## Output files

Everything goes in your output folder (Settings ▸ Default Output Directory), inside a
`FE-Buddy_Output` folder if that option is on:

```
<output folder>\
└── FE-Buddy_Output\            (only with "Add a FE-Buddy_Output folder")
    ├── Airports\
    │   ├── Geojson\    Runways_Lines, Airports_Symbols, Airports_Text (.geojson)
    │   └── Alias\      Airports.txt
    ├── Airways\
    │   ├── Geojson\    Airways_<group>_Lines / _Symbols / _Text (.geojson)
    │   └── Alias\      Airways.txt
    ├── Departure Procedures\
    │   ├── <ARTCC>\<airport>\   <airport>_<procedure>_Lines / _Symbols / _Text (.geojson)
    │   └── Alias\      Departures.txt
    ├── DAT to GeoJSON\ <.dat file name>.geojson, one per converted map
    ├── SCT2 to GeoJSON\
    │   └── <sector file name>\   ARTCC, …, GEO, LABELS, REGIONS (.geojson), SID\, STAR\
    └── vERAM to GeoJSON\
        └── <GeoMaps file name>\<GeoMap>\   <description>.geojson, or FILTER nn\ folders
```

A run overwrites the files it writes. A file that would be empty (nothing matched) is not written.

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

Changes here are saved with the **Save** button at the top (the Default Region of Interest saves
as soon as you set or clear it).

- **Updates**
  - **Channel** - *Stable* (the right choice for almost everyone), *Beta* or *Alpha*. Only pick
    Beta or Alpha if a developer asks you to.
  - **Check for updates now**, and **Get the latest stable installer** (opens the releases page -
    use it to go back to stable from a pre-release).
- **Facility Profile**
  - **Facility** - your ARTCC, picked from the current cycle's data. Saved for future features;
    today's sub-services do not use it.
  - **Default Output Directory** and **Add a FE-Buddy_Output folder inside that directory**.
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
