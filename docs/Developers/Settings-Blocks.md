# Settings blocks

A **settings block** is the `Dictionary<string, string>` one sub-service (or one file
conversion) receives for one run. The GUI builds it (`BuildSettingsBlock` on each tab), the
harness writes it by hand (`HarnessSettings.cs`), and the tests build it inline - and all of them
go through the same parser. This page lists every key each parser reads.

## How a block is read

- **Keys are case-insensitive.** Values are strings.
- **Yes/no values are `Y` or `N`** (any case). Anything else is an error.
- **Lists are comma-separated**; blanks are ignored (`"ZOB, ZNY,"` is `ZOB`, `ZNY`).
- **A missing optional key uses its default.** A missing **required** key, or an invalid value,
  throws `ArgumentException` naming the key - the run stops for that sub-service.
- **Unknown keys are warnings**, not errors: the run goes on and the Review tab lists them. This is
  how a retired key in an old harness or config degrades gracefully.
- **Only the CRC defaults actually needed are required**: those a file that is being written
  *and* is listed in `CrcDefaultsFor` needs (a file conversion: a kind it writes whose
  `IncludeCrc…Defaults` is `Y`). Other valid CRC keys are accepted and ignored; a key for a class,
  kind or field the sub-service does not have is a warning.

Parsers: `AirportSettingsParser`, `AirwaySettingsParser`, `DepartureSettingsParser`,
`ArrivalSettingsParser`, `NavaidSettingsParser`, `ArtccBoundarySettingsParser`,
`FixSettingsParser`, `WxStationSettingsParser`, `ProcedureSettingsParser`,
`TelephonySettingsParser`, `VnasAliasSettingsParser`, `DatToGeojsonSettingsParser`, `SctToGeojsonSettingsParser`,
`EramToGeojsonSettingsParser`. Shared reading: `SubServiceSettingsReader`, `CrcDefaultsReader`,
`ConversionSettingsReader`, `SettingsValueReader` (all in `FeBuddy.Core/Application`).

## Keys every AIRAC sub-service reads

| Key | Values | Default |
|---|---|---|
| `OutputDirectory` | folder path - the folder the run writes into (below) | **required** |
| `CoordinatePrecision` | `0`-`15` decimal places | `6` |
| `IncludeFebCustomProperties` | `Y` / `N` | `N` |
| `FebProperties` | list of `feb.*` names (below); **required** when the above is `Y` | none |
| `UploadToVnas` | list of file keys (below) marked for vNAS: GeoJSON written under `Upload_to_vNAS`, an alias file merged into `vNAS_Alias.txt` | none |
| `CrcDefaultsFor` | list of GeoJSON file keys that get CRC-ERAM defaults; each must also be in `UploadToVnas` | none |
| `FilterByRoi` | `Y` / `N` | `N` |
| `RoiSwLat`, `RoiSwLon`, `RoiNeLat`, `RoiNeLon` | decimal degrees; **required** when `FilterByRoi` is `Y` | none |

`GenerateAliasFile` (`Y` / `N`, default `Y`) is not here: it is one of every sub-service's own keys
*except* ARTCC Boundaries, Fixes and Wx Stations, none of which has an alias file or reads it - see
each sub-service's own key table below.

### Where files go

`OutputDirectory` is the folder the run writes into; files are laid out inside it by
`AiracOutputPaths`. The GUI does not send it: `AiracService` sets it on every block to the run's
cycle folder, `<output>[\FE-Buddy_Output]\AIRAC_<cycle>` (from `AiracServiceSettings`), so every
sub-service writes into the same folder. The harness and tests pass their own.

| File | Goes in |
|---|---|
| Alias file | `<OutputDirectory>\Aliases\` |
| GeoJSON | `<OutputDirectory>\Geojson\` (Departures, Arrivals: `…\Geojson\<ARTCC>\<ARPT>\`) |
| Marked for vNAS | GeoJSON: the same, under `<OutputDirectory>\Upload_to_vNAS\` instead. Alias file: still `<OutputDirectory>\Aliases\`, and also merged into `<OutputDirectory>\Upload_to_vNAS\vNAS_Alias.txt` |

After an AIRAC Service run writes at least one alias file, it also checks every alias file the run
wrote together for commands more than one line uses, and writes
`<OutputDirectory>\Duplicate_Alias_Commands.txt` listing them, grouped by ARTCC (`DuplicateAliasReport`)
- see [Architecture](Architecture.md#a-run-end-to-end). This is not driven by a settings-block key:
`AiracService` runs it itself once every selected sub-service has finished. After it, when any
alias file is marked for vNAS or [vNAS Alias Upload](#vnas-alias-upload) is selected, `AiracService`
writes `<OutputDirectory>\Upload_to_vNAS\vNAS_Alias.txt` (`VnasAliasFileWriter`): the custom alias
files, then every marked alias file - vNAS takes one alias file per facility.

### vNAS file keys

A **file key** names one output file: a GeoJSON file's name without `.geojson`, or the alias
file's name. Departures and Arrivals each write thousands of GeoJSON files, so their keys name
every file of one kind. Keys match ignoring case; a key the sub-service does not write, a
`CrcDefaultsFor` key that is not in `UploadToVnas`, or the alias file in `CrcDefaultsFor`, throws.
A key for a file that is not written this run (its kind is switched off, say) is accepted and does
nothing.

| Sub-service | GeoJSON keys | Alias key |
|---|---|---|
| Airports | `Runways_Lines`, `Airports_Symbols`, `Airports_Text` | `Airports.txt` |
| Airways | `Airways_<group>_Lines` / `_Symbols` / `_Text`, where `<group>` is `High`, `Low`, `Other` or a designation (letters only) | `Airways.txt` |
| Departures | `Departures_Lines`, `Departures_Symbols`, `Departures_Text` | `Departures.txt` |
| Arrivals | `Arrivals_Lines`, `Arrivals_Symbols`, `Arrivals_Text` | `Arrivals.txt` |
| NAVAIDs | `NAVAIDs_Symbols`, `NAVAIDs_Text` (`OutputBy=All`), or `NAVAIDs_<Token>s_Symbols` / `_Text` per NAVAID type (`OutputBy=Type`), e.g. `NAVAIDs_VORTACs_Symbols`, `NAVAIDs_VOR-DMEs_Text` | `Navaids.txt` |
| ARTCC Boundaries | `ARTCC-Boundary_High_Lines` / `_Low_Lines` (`OutputBy=HighLow`, an UNLIMITED ring in both), adds `_Unlimited_Lines` (`OutputBy=HighLowUnlimited`), or `ARTCC-Boundary_<LocationId>-<ALTITUDE>_Lines` per ARTCC and altitude (`OutputBy=ArtccAltitude`), e.g. `ARTCC-Boundary_ZOB-HIGH_Lines` | none |
| Fixes | `Fix_Symbols`, `Fix_Text` (`OutputBy=All`), or `Fix_<Group>_Symbols` / `_Text` per fix use, chart, or chart + fix use combination present (`OutputBy=FixUse`/`Chart`/`ChartAndFixUse`), e.g. `Fix_WYPNT_Symbols`, `Fix_ENROUTE-LOW-WYPNT_Text` | none |
| Wx Stations | `Wx_Symbols`, `Wx_Text` | none |
| Procedures | none - writes no GeoJSON | `Faa_Chart_Recall.txt` |
| Telephony | none - writes no GeoJSON | `Telephony.txt` |

CRC-ERAM defaults are only ever written to files marked for vNAS, since CRC reads its maps from
vNAS. The keys are listed in each sub-service's `*OutputFiles` class.

### New file names

Renaming files is not a key in any sub-service's block. The File Names tab sends one more
dictionary, `AiracServiceSettings.FileNames`, for the whole run: each renamed file's key and its new
name **without an extension** - e.g. `Airways_High_Lines` = `ZOB High`, `Airways.txt` = `ZOB Airways`.
`OutputFileNamesParser` reads it before anything is deleted or written, and `AiracService` hands the
result (`OutputFileNames`) to every sub-service's `Run`. A harness or test calling a sub-service
directly can pass one too; it is optional.

- A renamed file keeps its folder and its extension: `ZOB High.geojson`, `ZOB Airways.txt`.
- Every file key in the table above can be renamed except the Departures and Arrivals GeoJSON keys
  (their files are named per procedure from the FAA's data). So can the files that have no vNAS key:
  `Procedure_Changes.md`, `Procedures.json`, `vNAS_Alias.txt` and `Duplicate_Alias_Commands.txt` -
  each one's key is its name.
- An entry for any other key is a warning and is ignored; an entry with a blank name keeps
  FE-Buddy's name.
- A name that can't be used throws `ArgumentException` and the run stops before it starts: one
  with `\ / : * ? " < > |` or a control character in it, ending in a dot or in `.geojson`, `.txt`,
  `.md` or `.json`, a Windows device name (`CON`, `NUL`, `COM1`…), or longer than 100 characters
  (surrounding spaces are trimmed first); or the same new name, with the same extension, for two
  files. `OutputFileNames.Problem` holds the rules for one name; the File Names tab applies the same
  ones, and also refuses a new name that is another file's FE-Buddy name.
- The key still names the file everywhere else: `UploadToVnas`, `CrcDefaultsFor`, which alias file
  is marked for vNAS, and how `Duplicate_Alias_Commands.txt` finds each command's ARTCC.

### CRC defaults

Keys are `Crc.<Class>.<Kind>.<field>`, e.g. `Crc.High.Line.bcg`, `Crc.Airports.Symbol.style`.

| Kind | Fields |
|---|---|
| `Line` | `bcg` (1-40), `filters` (list of 0-40), `style` (`solid`, `shortDashed`, `longDashed`, `longDashShortDash`), `thickness` (1-3) |
| `Symbol` | `bcg`, `filters`, `style` (a CRC symbol: `vor`, `ndb`, `airport`, `rnav`, `airwayIntersections`, … - see `CrcPropertyValidator.ValidSymbolStyles`), `size` (1-4) |
| `Text` | `bcg`, `filters`, `size` (0-5), `underline` (`Y`/`N`), `opaque` (`Y`/`N`), `xOffset`, `yOffset` (whole numbers) |

Values are validated against what CRC can draw; an out-of-range value throws with a message
naming the key.

## Airports

| Key | Values | Default |
|---|---|---|
| `GenerateGeojson` | `Y` / `N` | `Y` |
| `GenerateAliasFile` | `Y` / `N` | `Y` |
| `EmitRunwayLines`, `EmitAirportSymbols`, `EmitAirportText` | `Y` / `N` | `Y` |

- **CRC classes:** `Runways` (`Line`), `Airports` (`Symbol`, `Text`).
- **`FebProperties`:** `faaId`, `icaoId`, `name`, `elev`, `respArtcc`, `tfcPtrnAlt`, `fssId`,
  `twrType`, `rwyId`.
- `GenerateGeojson` and `GenerateAliasFile` cannot both be `N`, and `GenerateGeojson = Y` needs at
  least one `Emit…`.

## Airways

| Key | Values | Default |
|---|---|---|
| `OutputBy` | `HighLow`, `Designation`, `None` | **required** |
| `EmitLines`, `EmitSymbols`, `EmitText` | `Y` / `N` | `Y` |
| `GenerateAliasFile` | `Y` / `N` | `Y` |
| `BufferAirwayWaypoints` | `Y` / `N` | `N` |
| `FixBufferNm` | NM a buffered line stops short of a 5-character fix, `0`-`10` | `2.5` |
| `NavaidBufferNm` | NM a buffered line stops short of any other waypoint (a NAVAID), `0`-`10` | `5` |
| `SplitAtAntimeridian` | `Y` / `N` | `Y` |
| `ExcludedDesignations` | list, e.g. `RN,SL` (upper-cased) | none |
| `HighDesignations` | list of designations written to the `Airways_High` files only, e.g. `J,Q` | `J,Q` (see below) |
| `LowDesignations` | list of designations written to the `Airways_Low` files only, e.g. `V,T` | `V,T` (see below) |
| `BothDesignations` | list of designations written to both the High and the Low files | none |
| `AliasRoiScope` | `All`, `RoiAirways` | `All` |

- **Buffer distances:** `FixBufferNm` and `NavaidBufferNm` are read only when
  `BufferAirwayWaypoints` is `Y` and `OutputBy` is not `None`; otherwise they are ignored, whatever
  they hold. A value outside `0`-`10` throws. A leg shorter than the distances at its two ends
  added together is dropped, with an `Info` message.
- **High and Low files:** with `HighLow`, each airway goes in the file its designation is listed
  for - not by its published altitudes. A designation may be in only one of the three lists (it
  throws otherwise). One in none is left out of both files, with an advisory warning naming it.
  With none of the three keys in the block at all, J and Q go High and V and T Low; once any of
  them is present, the lists are taken as given. The GUI always sends all three. The lists are
  ignored with `Designation` or `None`.
- **CRC classes:** `High`, `Low`, `Other`, each with `Line`, `Symbol` and `Text`. With `HighLow`,
  a file in `CrcDefaultsFor` needs only its own class (`Airways_High_Lines` needs `Crc.High.Line.*`),
  which every airway in it uses whatever its published altitudes; `Other` is never needed. With
  `Designation`, a file can hold every altitude class (the highest published altitude: 18,000 ft
  or more High, below that Low, none Other), so it needs all three of its kind.
- **`FebProperties`:** `awyId`, `pointId`, `waypoints`.
- `OutputBy = None` writes no GeoJSON (so no CRC defaults are needed); any other value needs at
  least one `Emit…`.

## Departures

| Key | Values | Default |
|---|---|---|
| `GenerateGeojson` | `Y` / `N` | `Y` |
| `GenerateAliasFile` | `Y` / `N` | `Y` |
| `EmitLines`, `EmitSymbols`, `EmitText` | `Y` / `N` | `Y` |
| `IncludeObstacleDepartures` | `Y` / `N` | `Y` |
| `ArtccFilter` | list of ARTCC IDs; empty means every ARTCC | none |
| `AmendmentFilter` | `None`, `Cycles`, `Days`, `Date` | `None` |
| `AmendedWithinCycles` | 1-1000 (1 = the selected cycle); **required** with `Cycles` | - |
| `AmendedWithinDays` | 1-36500, counted back from today; **required** with `Days` | - |
| `AmendedOnOrAfter` | `yyyy-MM-dd`; **required** with `Date` | - |
| `RoiMode` | `Airport` (every departure of an airport inside the ROI), `Waypoint` (any departure with a point inside) | `Airport` |

- **CRC class:** `Departures`, with `Line`, `Symbol` and `Text`.
- **`FebProperties`:** `dpName`, `pointId`, `arptId`, `artcc`, `amendmentNo`, `amendEffDate`,
  `waypoints`.
- `GenerateGeojson` and `GenerateAliasFile` cannot both be `N`, and `GenerateGeojson = Y` needs at
  least one `Emit…`.
- Only the active amendment mode's value is read (and required); values for the other modes are
  ignored.

## Arrivals

| Key | Values | Default |
|---|---|---|
| `GenerateGeojson` | `Y` / `N` | `Y` |
| `GenerateAliasFile` | `Y` / `N` | `Y` |
| `EmitLines`, `EmitSymbols`, `EmitText` | `Y` / `N` | `Y` |
| `ArtccFilter` | list of ARTCC IDs; empty means every ARTCC | none |
| `AmendmentFilter` | `None`, `Cycles`, `Days`, `Date` | `None` |
| `AmendedWithinCycles` | 1-1000 (1 = the selected cycle); **required** with `Cycles` | - |
| `AmendedWithinDays` | 1-36500, counted back from today; **required** with `Days` | - |
| `AmendedOnOrAfter` | `yyyy-MM-dd`; **required** with `Date` | - |
| `RoiMode` | `Airport` (every arrival of an airport inside the ROI), `Waypoint` (any arrival with a point inside) | `Airport` |

- **CRC class:** `Arrivals`, with `Line`, `Symbol` and `Text`.
- **`FebProperties`:** `arrivalName`, `pointId`, `arptId`, `artcc`, `amendmentNo`, `amendEffDate`,
  `waypoints`.
- The same keys as Departures, minus `IncludeObstacleDepartures` - a STAR has no obstacle/SID
  split, so the key is not read; sending it anyway is an unknown-key warning, not an error.
- `GenerateGeojson` and `GenerateAliasFile` cannot both be `N`, and `GenerateGeojson = Y` needs at
  least one `Emit…`.
- Only the active amendment mode's value is read (and required); values for the other modes are
  ignored.

## NAVAIDs

| Key | Values | Default |
|---|---|---|
| `GenerateGeojson` | `Y` / `N` | `Y` |
| `GenerateAliasFile` | `Y` / `N` | `Y` |
| `EmitSymbols`, `EmitText` | `Y` / `N` | `Y` |
| `OutputBy` | `All`, `Type` | `All` |
| `ExcludedTypes` | list of NASR `NAV_TYPE` names (upper-cased) - the types left out of GeoJSON *and* the alias file | none |
| `SymbolStyleBy` | `Type`, `File` - read (and meaningful) only when `OutputBy = All` | `Type` |
| `FanMarkerStyle` | a CRC symbol style; read only when the merged `OutputBy = All` Symbols file gets CRC-ERAM defaults, `SymbolStyleBy = Type`, and `FAN MARKER` is not excluded. Optional: a fan marker written without one gets no style, with a warning (the GUI requires it whenever the cycle has fan markers) | - |

- **CRC classes:** with `OutputBy = All`, `NAVAIDs` (`Symbol`, `Text`); with `OutputBy = Type`, one
  class per NAVAID type present, keyed by its token - `VOR`, `VORTAC`, `VOR-DME`, `VOT`, `TACAN`,
  `DME`, `NDB`, `NDB-DME`, `MARINE-NDB`, `MARINE-NDB-DME`, `UHF-NDB`, `FAN-MARKER`, `CONSOLAN` - each
  with `Symbol` and `Text`. There is no `Line` class; NAVAIDs writes no Lines file.
- **`FebProperties`:** `navId`, `navType`, `name`, `freq`, `lowAltArtccId`, `highAltArtccId` - the
  Text file never writes `navId`, `navType` or `name`; its `text` array already carries all three.
- `GenerateGeojson` and `GenerateAliasFile` cannot both be `N`; `GenerateGeojson = Y` needs at least
  one of `EmitSymbols` / `EmitText` (there is no `EmitLines`).
- `ExcludedTypes` warns about a name that is not a known NAVAID type (it is still excluded, so a
  type NASR adds can be unticked), and excluding every known type throws - NAVAIDs would then have
  nothing to write.
- Data comes from `NAV_BASE` only. A NAVAID with a `NAV_STATUS` of `SHUTDOWN` is always skipped;
  duplicate `NAV_ID`s (e.g. `ABQ`, both a VORTAC and a VOT) are normal and every row is kept.
- With `OutputBy = Type`, CRC defaults are read from whichever type keys are chosen for CRC-ERAM
  defaults rather than from the known type list, so a `NAV_TYPE` FE-Buddy does not recognize still
  gets its own file's defaults.

## ARTCC Boundaries

| Key | Values | Default |
|---|---|---|
| `OutputBy` | `HighLow`, `HighLowUnlimited`, `ArtccAltitude` | `HighLow` |
| `LocationFilter` | list of ARTCC IDs; empty means every one with boundary data | none |
| `SplitAtAntimeridian` | `Y` / `N` | `Y` |

- Unlike every other AIRAC sub-service, there is no `GenerateGeojson`, `GenerateAliasFile` or
  `Emit…` key: ARTCC Boundaries always writes GeoJSON, has no alias file, and writes Lines only.
- **CRC classes:** with `OutputBy = HighLow`, `High` and `Low` (each `Line` only) - an UNLIMITED
  ring is written into both files; with `HighLowUnlimited`, `High`, `Low` and `Unlimited`; with
  `ArtccAltitude`, one class per LocationId and altitude present, e.g. `ZOB-HIGH`, so your own
  ARTCC can be styled apart from its neighbours. There is no `Symbol` or `Text` class.
- **`FebProperties`:** `locationId`, `locationName`, `locationType`, `icaoId`, `computerId`,
  `altitude` (`HIGH`, `LOW` or `UNLIMITED`), `type` (`ARTCC`, `CTA`, `FIR`, `CTA/FIR` or `UTA` -
  tells the overlapping oceanic CTA and FIR rings apart), `city`, `countryCode`.
- Data comes from `ARB_BASE` (the location) and `ARB_SEG` (the boundary points). Only the
  LocationIds with `ARB_SEG` rows draw a boundary - the Canadian, foreign and CERAP entries
  `ARB_BASE` also publishes have none. Within one LocationId and altitude, a new ring starts
  wherever `POINT_SEQ` drops back to a low value - ZAK, ZAP and ZWY each have a CTA ring and a FIR
  ring - and after any point described as running "TO POINT OF BEGINNING" (ZOA's UNLIMITED group
  is four UTA rings, ZMA's two CTA/FIR sectors, all in one `POINT_SEQ` run). Each ring is closed
  back to its own first point.
- `LocationFilter` limits the GeoJSON only; there is no alias file for it to limit.

## Fixes

| Key | Values | Default |
|---|---|---|
| `EmitSymbols`, `EmitText` | `Y` / `N` | `Y` |
| `OutputBy` | `All`, `FixUse`, `Chart`, `ChartAndFixUse` | `All` |
| `ExcludedFixUses` | list of fix use names (below); only used in the `FixUse` layout | none |
| `ExcludedCharts` | list of NASR chart names; only used in the `Chart` layout | none |
| `Combinations` | list of `<Chart>+<FixUse>` pairs, e.g. `ENROUTE-LOW+WYPNT,IAP+RPRTNG-PNT`; **required** (at least one) when `OutputBy = ChartAndFixUse` | none |

- Unlike every other AIRAC sub-service except ARTCC Boundaries, there is no `GenerateGeojson` or
  `GenerateAliasFile` key: Fixes always writes GeoJSON and has no alias file.
- **CRC classes:** with `OutputBy = All`, `Fix` (`Symbol`, `Text`); with `OutputBy = FixUse`, one
  class per fix use present, keyed by its token - `COMPUTER-NAV`, `MIL-RPRTNG-PNT`, `MIL-WYPNT`,
  `NRS-WYPNT`, `RADAR`, `RPRTNG-PNT`, `VFR-WYPNT`, `WYPNT`, or any other NASR code kept as written -
  with `OutputBy = Chart`, one class per chart token present (a run of non-alphanumeric characters
  becomes `-`, e.g. `ENROUTE LOW` → `ENROUTE-LOW`), or `NO-CHART` for a fix with no chart; with
  `OutputBy = ChartAndFixUse`, one class per listed combination, e.g. `ENROUTE-LOW-WYPNT`. Each with
  `Symbol` and `Text`. There is no `Line` class; Fixes writes no Lines file.
- **`FebProperties`:** `fixId`, `fixUseCode`, `charts` - the Text file never writes `fixId`; its
  `text` array already carries it.
- `EmitSymbols` and `EmitText` cannot both be `N`.
- `ExcludedFixUses` warns about a name that is not a known fix use (it is still excluded, so a fix
  use NASR adds can be unticked), and excluding every known fix use in the `FixUse` layout throws.
- A `Combinations` entry naming an unrecognized fix use is still used, with a warning; an entry
  that does not tokenize to both a chart and a fix use throws. `OutputBy = ChartAndFixUse` with no
  `Combinations` throws. A combination matching no fix in the cycle is a run warning, and writes no
  files.
- Data comes from `FIX_BASE` only.

## Wx Stations

| Key | Values | Default |
|---|---|---|
| `EmitSymbols`, `EmitText` | `Y` / `N` | `Y` |

- The simplest AIRAC sub-service settings: there is no `GenerateGeojson`, `GenerateAliasFile` or
  `OutputBy` key - Wx Stations always writes GeoJSON, has no alias file, and writes one merged
  Symbols/Text pair, never split into groups.
- **CRC class:** `Wx` (`Symbol`, `Text`). There is no `Line` class; Wx Stations writes no Lines
  file.
- There are no `FebProperties`: a station's label is always its ICAO ID, then its IATA ID and
  site name. `IncludeFebCustomProperties = Y` is accepted but only warns, since there is nothing
  for it to add.
- `EmitSymbols` and `EmitText` cannot both be `N`.
- Data comes from aviationweather.gov's `stations.cache.xml`, not a NASR CSV group, and not
  published per AIRAC cycle at all. Every AIRAC Service run that includes Wx Stations downloads the
  latest copy first, whichever cycle is run, falling back on FE-Buddy's one kept copy
  (`%APPDATA%\FE-Buddy\WxStations\stations.cache.xml`) when that fails (see
  [Architecture](Architecture.md#the-airac-data-pipeline)). With no copy at all,
  `WxStationService.Run` accepts a `null` collection and just writes nothing, with a warning,
  rather than throwing; the rest of the run still completes.
- A station is included only when all hold: its country is `US` or a US territory (`PR`, `VI`,
  `GU`, `MP`, `AS`, `UM`); it has an ICAO ID; `METAR` is among its site types; and it has usable
  coordinates - present, finite, and within -90..90 / -180..180 (the feed's `-99.99, -99.99`
  placeholder is left out, with an Info message naming the station).

## Procedures

| Key | Values | Default |
|---|---|---|
| `GenerateChangesDocument` | `Y` / `N` | `Y` |
| `GenerateProceduresJson` | `Y` / `N` | `Y` |
| `GenerateAliasFile` | `Y` / `N` - whether to write `Faa_Chart_Recall.txt` | `Y` |
| `Facilities` | list of ARTCC IDs; every included airport's procedures (of the chart types below) are included for each one | none |
| `PrimaryFacility` | an ARTCC ID; its section leads both documents when it ends up with any included airport | none |
| `IncludeRoiAirports` | `Y` / `N` - also include every airport whose NASR coordinates fall inside the Region of Interest; **required** to have a ROI set (`FilterByRoi = Y` plus its four corners) when `Y` | `N` |
| `Airports` | list of FAA or ICAO airport identifiers to include (whole), regardless of `Facilities` or the ROI | none |
| `Procedures` | list of procedure base chart names to include at every airport that has one, regardless of `ChartTypes` or whether the airport is otherwise included | none |
| `AirportProcedures` | list of `<Airport>\|<Procedure name>` pairs to include, e.g. `PIT\|ILS OR LOC RWY 28C` | none |
| `ChartTypes` | list of d-TPP chart codes (`IAP`, `STR`, `DP`, `ODP`, `DAU`, `APD`, `MIN`, `HOT`, `LAH`) a whole included airport's procedures are limited to; a name picked by `Procedures` or `AirportProcedures` is included regardless | `IAP,STR,DP,ODP,DAU,APD` |
| `JsonFields` | list of `Procedures.json` optional field names (below); only meaningful with `GenerateProceduresJson = Y` | `icaoId,airportName,responsibleArtcc,airspaceClass,chartType,chartUrl,change,compareUrl` |
| `UploadToVnas` | file keys to merge into `Upload_to_vNAS\vNAS_Alias.txt`; the only one Procedures ever writes is `Faa_Chart_Recall.txt` | none |

- Unlike every other AIRAC sub-service, Procedures writes no GeoJSON at all: there is no `Emit…`,
  `FebProperties`, `CrcDefaultsFor` or `Crc.*` key - nothing it writes carries CRC-ERAM defaults. It
  does have an alias file, `Faa_Chart_Recall.txt` (built by `ChartRecallAliasBuilder`): one
  `.OPENURL` command per page of every current chart at every airport in the d-TPP Metafile,
  whatever `Facilities`, `Airports`, `Procedures`, `AirportProcedures`, `ChartTypes` and
  `IncludeRoiAirports` say - those settings pick only what the two documents cover.
  `IncludeFebCustomProperties = Y` is accepted but only warns, pointing at `JsonFields` instead,
  since that is where Procedures' own optional fields live.
- `GenerateChangesDocument`, `GenerateProceduresJson` and `GenerateAliasFile` cannot all three be
  `N` - Procedures would then produce nothing.
- At least one of `Facilities`, `IncludeRoiAirports` (with a Region of Interest set), `Airports`,
  `Procedures` or `AirportProcedures` is required **only when a document is on**
  (`GenerateChangesDocument` or `GenerateProceduresJson`) - the alias file needs no inclusion
  source, since it covers every airport regardless. The filters add up: every airport of the ticked
  facilities, plus (optionally) every airport inside the ROI, plus explicitly listed airports, plus
  procedures picked by name at any airport, plus specific airport + procedure pairs.
- Data comes from the FAA's d-TPP Metafile (`d-tpp_Metafile.xml`), not a NASR CSV group - downloaded
  once per cycle and cached alongside the NASR CSVs (see
  [Architecture](Architecture.md#the-airac-data-pipeline)) - joined to NASR `APT_BASE` (each
  airport's responsible ARTCC and coordinates) and `CLS_ARSP` (its highest class airspace: B, C, D
  or E). The FAA posts a cycle's metafile only 15-18 days before its effective date, so the next
  cycle's is often not yet published; a run then writes no Procedures output at all, with an
  advisory saying why, but is not blocked. The previous cycle's metafile, when cached, links a
  procedure this cycle deleted to the last chart it had.
- `JsonFields` values (`ProcedureJsonField`) - airport-level: `icaoId`, `airportName`, `city`,
  `state`, `responsibleArtcc`, `airspaceClass`, `military`; procedure-level: `chartType`,
  `chartUrl`, `change`, `compareUrl`, `amendment`, `amendmentDate`, `procedureUid`, `computerCode`,
  `producer`. An airport's `airportId` and a procedure's `name` are always written, regardless of
  `JsonFields`; a field with no value for a given airport or procedure is left out rather than
  written null.
- The FAA Chart Recall command rules (how a chart's type, runway and computer code become its
  command, and what gets no command) are documented in the XML doc comments of `ChartRecallCodes`,
  `ApproachCodes`, `SidStarCodes` and `ChartRecallSkipReason`
  (`FeBuddy.Core/Domain/Procedures/ChartRecall/`) and `ChartRecallAliasBuilder`
  (`FeBuddy.Core/Application/Airac/Procedures/`); for the controller-facing summary, see the
  [user guide](../Users/User-Guide.md#faa-chart-recall-commands).

## Telephony

| Key | Values | Default |
|---|---|---|
| `GenerateAliasFile` | `Y` / `N` - must stay `Y`; the alias file is Telephony's only output, so `N` throws | `Y` |
| `UploadToVnas` | file key to merge into `Upload_to_vNAS\vNAS_Alias.txt`; the only one Telephony ever writes is `Telephony.txt` | none |

- Unlike every other AIRAC sub-service, there is no `FebProperties`, `CrcDefaultsFor` or `Crc.*`
  key, and no region of interest: Telephony writes no GeoJSON and covers every operator regardless
  of area. The shared keys every sub-service block reads (`FilterByRoi` and its four corners,
  `IncludeFebCustomProperties`) are accepted and ignored, with a warning for
  `IncludeFebCustomProperties = Y`.
- Data comes from FAA Order JO 7340.2, Chapter 3: Section 1 (the ICAO register) and Section 4 (U.S.
  special call signs) - not a NASR CSV group, and not published per AIRAC cycle at all. Every AIRAC
  Service run that includes Telephony downloads the latest copy of both pages first (see
  [Architecture](Architecture.md#the-airac-data-pipeline)); the register is required - with no
  usable copy, `TelephonyService.Run` writes nothing, with a warning - and the U.S. special call
  signs page is optional: without a copy of it, Telephony still writes `Telephony.txt` from the
  register alone, with a warning.
- `TelephonyBuilder.Read` turns the parsed pages into cards, leaving out a register row with no
  three-letter designator (the FAA prints `...` or `--`) or no telephony, and a U.S. special call
  sign that has expired; one whose expiration date FE-Buddy can't read is kept, with a warning.
- `TelephonyAliasWriter.Generate` writes two commands per operator - `.id` plus its designator or
  identifier, and `.id` plus its telephony reduced to letters and digits - one command when the two
  are the same (e.g. `NASA`). When several operators land on the same command (a telephony that
  spells another operator's designator, or two telephonies that only differ by spacing), the
  command shows every one of their cards, separated by `\n---`, the command's own operator first.
  Commands are written in alphabetical order. For the controller-facing command and card rules, see
  the [user guide](../Users/User-Guide.md#telephony-tab).

## vNAS Alias Upload

Numbered keys, one group per custom alias file (`<n>` from 1); files are merged in number order.

| Key | Values | Default |
|---|---|---|
| `Sources.<n>.FilePath` | full path of an alias file on this PC | none |
| `Sources.<n>.Url` | `http://` or `https://` address of an alias file, with no sign-in written into it | none |
| `Sources.<n>.CredentialId` | id of a saved credential (`CredentialStore`, `"N"` GUID format) to download `Url` with; only ever the id, never a secret | none |

- Reads none of the keys every other AIRAC sub-service reads (it writes no GeoJSON and has no
  alias file of its own); `OutputDirectory` is accepted and ignored. Any other key, including an
  unknown field under `Sources.<n>.`, is a warning.
- Each `<n>` needs exactly one of `FilePath` and `Url`. These throw: both; neither, when the
  group has a `CredentialId` (a group with nothing at all is skipped); a `FilePath` that is not a
  full path; a `Url` that is not `http`/`https`, or that has a sign-in written into it - a user name
  and password, or a token such as `?token=` (`UrlSecrets`; the message never repeats the address);
  a `CredentialId` that is not a GUID. A `CredentialId` on a `FilePath` is ignored, with an Info
  message. No sources is fine (an Info message) - `vNAS_Alias.txt` then holds only FE-Buddy's
  aliases. The GUI tab is only invalid when there is nothing to merge at all: no source and no
  FE-Buddy alias file marked for vNAS.
- `AliasSourceLoader` reads every source before the sub-services run, with a 30-second timeout
  each. A GitHub file address (`github.com/{owner}/{repo}/blob|raw/{branch}/{path}`,
  `raw.githubusercontent.com/{owner}/{repo}/{branch}/{path}`, and either with `refs/heads/{branch}`
  or `refs/tags/{tag}` - GitHub's own Raw button gives `…/raw/refs/heads/{branch}/{path}`) is
  fetched through `https://api.github.com/repos/{owner}/{repo}/contents/{path}?ref={branch}` with
  `Accept: application/vnd.github.raw` (`GitHubFileUrl`; the branch is escaped for the query), so
  a private repository works with a token; a GitHub repository or folder page is refused. Any other
  address is fetched as given.
- The credential is applied only through `CredentialStore.Authorize`. When it is not on this PC,
  the address is not `https`, or the credential is not allowed on that website, the file is not
  downloaded at all - never anonymously instead. An `http://github.com` file address may still
  carry a credential, since the download goes to GitHub's API over https. 401; 403 (GitHub's
  hourly limit without a token, a short-term limit with `Retry-After`, a token not authorized for
  an organization's SSO, and a fine-grained token without Contents: Read-only); 429; 404; an HTML
  page instead of a file; a download that is not readable text; a file with no alias commands; an
  unreachable site; a timeout; and a Credential Manager that cannot be read each get their own
  message.
- A source that can't be read never stops the run, and the loader never throws for one - only
  cancelling does: it is left out of `vNAS_Alias.txt` with an
  advisory warning, and the file is written from the rest. For the file's layout see
  [Architecture](Architecture.md#a-run-end-to-end); for the user's view, the
  [user guide](../Users/User-Guide.md#vnas-alias-upload-tab).

## Keys every file conversion reads

A file conversion is not an AIRAC sub-service: it has no alias file, `feb.*` properties, ROI or
vNAS files. Every conversion reads these (shared reading: `ConversionSettingsReader`), plus
`CoordinatePrecision` and the `Crc.*` keys as in the tables above. It writes into its own folder
next to the `AIRAC_<cycle>` folders, `<OutputDirectory>[\FE-Buddy_Output]\<conversion>`.

| Key | Values | Default |
|---|---|---|
| `OutputDirectory` | folder path | **required** |
| `AddFeBuddyOutputFolder` | `Y` / `N` | `Y` |
| `IncludeCrcLineDefaults`, `IncludeCrcSymbolDefaults`, `IncludeCrcTextDefaults` | `Y` / `N` - write that kind's CRC-ERAM defaults (the Include box on its panel); only a kind the conversion writes is read | `N` |
| `SourceFolder` | a folder; every file directly in it with the conversion's extension is converted | - |
| `SourceFiles` | file paths separated by `\|` (a comma is legal in a Windows path; `\|` is not) | - |

- Exactly one of `SourceFolder` and `SourceFiles` is required. A `SourceFolder` that does not
  exist throws; a file in `SourceFiles` that cannot be read fails that file only.

## DAT to GeoJSON (File Conversions)

Extension `.dat`.

| Key | Values | Default |
|---|---|---|
| `CroppingDistance` | NM from each map's point of tangency, greater than 0 and at most 1000; blank keeps every line | none |

- **CRC class:** `VideoMap`, with `Line` only (`Crc.VideoMap.Line.*`).

## SCT2 to GeoJSON (File Conversions)

Extensions `.sct2` and `.sct`. No keys of its own.

- **CRC class:** `SectorFile`, with `Line` (every lines file) and `Text` (the labels file):
  `Crc.SectorFile.Line.*`, `Crc.SectorFile.Text.*`. Regions have no CRC defaults.

## ERAM to GeoJSON (File Conversions)

Extension `.xml`: the `Geomaps.xml` (`Geomaps_Records`) of an ERAM adaptation export. **One per
run**: a `SourceFolder` may hold the whole export, but only one Geomaps file - its other XML files
are named in one message - and a `SourceFiles` list only one file; more throws. A picked file that
is not a Geomaps file fails. Output goes to `ERAM_TO_GEOJSON` (inside `FE-Buddy_Output` with
`AddFeBuddyOutputFolder`), which is emptied once the file has been read, before anything is written.
A `ConsoleCommandControl_Records` file beside the Geomaps file (either source) is read too, never
converted, and its map menus listed in `ERAM_TO_GEOJSON\ConsoleCommandControl.txt`; no key turns
this on or off. The result's `OtherFilesWritten` holds that file.

| Key | Values | Default |
|---|---|---|
| `OutputLayout` | `ByFilters` (`<map>\Filter_01\Filter_01_Lines.geojson`, `Multi-Filter_02_03_08\…`), `ByAttributes` (`<map>\BCG 01_Filters 01_Type AAV_Group 64_Object ZOB3NM_Style Solid_Thick 1_Lines.geojson`), `Raw` (`<map>.geojson`, every Feature carrying its own look). `ByFilter` and `ByObject`, from before, are read as `ByFilters` and `ByAttributes` with an Info note | `ByAttributes` |
| `DefaultsSource` | `Xml` (carry over the XML's defaults and element values), `XmlThenCard` (the tab's defaults fill whatever the XML's leave out), `Card` (the tab's defaults only; the XML's styling is ignored) | `Xml` |
| `IncludeFebCustomProperties` | `Y` / `N` | `N` |
| `FebProperties` | comma-separated: `mapObjectType`, `mapGroupId`, `lineObjectId`, `symbolId`, `saaId` | none (required when `IncludeFebCustomProperties` is `Y`) |

- `<map>` is `<GeomapId>_<LabelLine1>-<LabelLine2>` with the characters Windows forbids taken out,
  `LL1` / `LL2` for a missing label line, and ` (2)` added when two maps would share a name.
- **CRC class:** `GeoMap`, with `Line`, `Symbol` and `Text`: `Crc.GeoMap.Line.*`,
  `Crc.GeoMap.Symbol.*`, `Crc.GeoMap.Text.*`.
- The tab's CRC defaults are read only when `DefaultsSource` is `XmlThenCard` or `Card`, and then
  only for kinds whose `IncludeCrc…Defaults` is `Y`. With `Xml` they are ignored.
- An element with no filters from its object or itself gets filter `0` (shown at every setting).
- ERAM text has no opaque background, so Text defaults taken from the XML always have `opaque`
  off. ERAM's `Color` has no CRC equivalent and is not carried over; text whose `DisplaySetting` is
  false is left out.

## An example (Airways, as the harness writes it)

```csharp
new Dictionary<string, string>
{
	{ "OutputDirectory", @"C:\FE-Buddy-Output" },
	{ "OutputBy", "HighLow" },
	{ "GenerateAliasFile", "Y" },
	{ "IncludeFebCustomProperties", "Y" },
	{ "FebProperties", "awyId,pointId,waypoints" },
	{ "UploadToVnas", "Airways_High_Lines,Airways_Low_Lines,Airways.txt" },
	{ "CrcDefaultsFor", "Airways_High_Lines" },
	{ "Crc.High.Line.bcg", "1" },
	{ "Crc.High.Line.filters", "1,2" },
	{ "Crc.High.Line.style", "solid" },
	{ "Crc.High.Line.thickness", "1" },
	// ...a class and kind for every other file in CrcDefaultsFor
	{ "FilterByRoi", "N" },
};
```

## Adding a key

1. Read it in the sub-service's parser with `SettingsValueReader` (and add it to the parser's
   `OwnKeys`, or `SubServiceSettingsReader.CommonKeys` if every sub-service reads it - otherwise it
   is reported as unknown).
2. Send it from the tab's `BuildSettingsBlock` (or `GeojsonSubServiceViewModel.AddSharedSettings`).
3. Add it to the harness settings and a parser test.
4. List it here, and in [UserConfig.json reference](UserConfig-Reference.md) if the GUI saves it.
