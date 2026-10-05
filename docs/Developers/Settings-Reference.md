# Settings reference

FE-Buddy's settings live in two places:

- **The settings block** - the `Dictionary<string, string>` one sub-service or file conversion gets
  for one run. The app builds it (each tab's `BuildSettingsBlock`), the harness writes it by hand
  (`FeBuddy.Harness/HarnessSettings.cs`) and the tests build it inline. All of them go through the
  same parser.
- **`UserConfig.json`** (`%APPDATA%\FE-Buddy`) - what the app remembers. Each tab saves its own
  node, mostly under the same key names its block uses.

This page lists both. Where a saved key differs from the block key, the table's **Saved as** column
says so; a blank there means the same name.

**Contents:** [How values are read](#how-values-are-read) · [UserConfig.json](#userconfigjson) ·
[Keys every AIRAC sub-service shares](#keys-every-airac-sub-service-shares) · [File keys](#file-keys) ·
[New file names](#new-file-names) · [CRC defaults](#crc-defaults) · [Airports](#airports) ·
[Airways](#airways) · [Departures and Arrivals](#departures-and-arrivals) · [NAVAIDs](#navaids) ·
[ARTCC Boundaries](#artcc-boundaries) · [Fixes](#fixes) · [Wx Stations](#wx-stations) ·
[Procedures](#procedures) · [Telephony](#telephony) · [Concatenate Aliases](#concatenate-aliases) ·
[File conversions](#file-conversions) · [Settings export and import](#settings-export-and-import) ·
[Adding a setting](#adding-a-setting)

## How values are read

- Values are strings. Keys are case-insensitive as the app and `AiracService` pass them; a plain
  `Dictionary` built by hand (the harness's) needs the exact case.
- Yes/no values are `Y` or `N`, in any case. In a block anything else is an error; a tab's saved
  setting also accepts `true`.
- Lists are comma-separated, and blanks are ignored: `"ZOB, ZNY,"` is `ZOB`, `ZNY`.
- A missing optional key uses its default. A missing **required** key, or a bad value, throws
  `ArgumentException` naming the key, which stops an AIRAC Service run there (the tabs' own checks
  normally prevent it).
- An unknown block key is a warning on the Review tab, not an error.
- A saved key that is missing means "use the default", so adding a setting needs no migration.

Parsers: `AirportSettingsParser`, `AirwaySettingsParser`, `DepartureSettingsParser`,
`ArrivalSettingsParser`, `NavaidSettingsParser`, `ArtccBoundarySettingsParser`, `FixSettingsParser`,
`WxStationSettingsParser`, `ProcedureSettingsParser`, `TelephonySettingsParser`,
`ConcatenateAliasesSettingsParser`, `DatToGeojsonSettingsParser`, `SctToGeojsonSettingsParser` and
`EramToGeojsonSettingsParser`. Shared readers: `SubServiceSettingsReader`, `CrcDefaultsReader`,
`ConversionSettingsReader` and `SettingsValueReader`.

## UserConfig.json

One nested JSON tree. Code reads a value by its dotted path (`General.UpdateChannel`) through
`UserConfigFile`; keys read in more than one place are constants in `UserConfigKeys`. Before a tab's
node is saved, its old state is copied to `UserConfig.previous.json`, which **Undo last save**
restores. Settings' own **Save** writes the whole file, with no undo copy.

### General

Written by Settings, except `NewsLastOpen`, `LegacyGitHubTokenNoticeShown`, and the first
`UpdateChannel`.

| Key | Values | Default |
|---|---|---|
| `UpdateChannel` | `Stable`, `ReleaseCandidate`, `Beta`, `Alpha`. Saved by the first launch (not a `-dev` build's), then changed only in Settings | the running build's channel (`UpdateChannelSetting`) |
| `NewsLastOpen` | the newest News `PostId` the user has seen, e.g. `2026-08-30.3` | none |
| `PrettyPrintGeojson` | `Y` / `N` (`OutputFormatting`) | `N` |
| `DefaultOutputDirectory` | a folder | the Desktop |
| `AddFeBuddyOutputFolder` | `Y` / `N` - put the output in a `FE-Buddy_Output` folder | `Y` |
| `FeBuddyGitHub.CredentialId` | the GitHub token FE-Buddy's own GitHub requests use (`GitHubAuth`) | none |
| `LegacyGitHubTokenNoticeShown` | `Y` once the notice about 2.x's `FEBUDDY_GITHUB_TOKEN` has been shown | none |

### Services.AiracService

| Key | Values | Default |
|---|---|---|
| `AiracCycleId` | a cycle ID, e.g. `2610`, matched back to previous, current or next on load | the current cycle |
| `SelectedSubServices` | the sub-services included on the General tab, comma-separated: `Airports`, `Airways`, `Departures`, `Arrivals`, `Navaids`, `ArtccBoundaries`, `Fixes`, `WxStations`, `Procedures`, `Telephony`. Concatenate Aliases isn't in the table: its tab comes in by itself | every sub-service |
| `Outputs.<sub-service>.<output>` | `Y` / `N`: the General tab's table, e.g. `Outputs.Airports.Geojson`. `<output>` is `Alias`, `Geojson`, `ProcedureChanges` or `ProceduresJson`, and only those the sub-service makes are saved | `Y` |
| `UserArtccId` | the Settings ▸ Facility Profile ARTCC, e.g. `ZOB`. The run's `PrimaryFacility` (first in `Duplicate_Alias_Commands.txt`, and Procedures' leading section), and the ARTCC ticked on Departures, Arrivals, ARTCC Boundaries and Procedures until each is first saved | none |
| `CoordinatePrecision` | `1`-`15` decimal places, or `0` for Do not round (the app offers 5, 6, 7 and Do not round) | `6` |
| `DefaultRoi.FilterByRoi` | `Y` / `N` | `N` |
| `DefaultRoi.Corners.SwLat`, `.SwLon`, `.NeLat`, `.NeLon` | decimal degrees | none |

The default ROI is saved by `DefaultRoiStore`: by the Map as soon as it is set or cleared, by
Settings when **Save** is pressed. Clearing it only sets `FilterByRoi` to `N`, so the corners come
back next time.

### Services.AiracService.FileNames

Saved by the File Names tab. Each file's choice is kept by its [file key](#file-keys), as a
numbered list because a key such as `Airways.txt` has a dot in it. Every save removes the whole
`Files` subtree first, so no stale numbers are left.

| Key | Values | Default |
|---|---|---|
| `RenameFiles` | `Y` / `N` | `N` |
| `Files.<n>.Key` | a file key, e.g. `Airways_High_Lines` or `Airways.txt` | none |
| `Files.<n>.Rename` | `Y` / `N` - ticked for renaming | `Y` |
| `Files.<n>.Name` | the new name, without an extension; kept while unticked | none |

A ticked file with no name is not saved, so the tab flags it until it is named or unticked.

### Services.MapService

Saved by the Map (`MapLayersState`) the moment a choice is made; the Map has no Save button.

| Key | Values | Default |
|---|---|---|
| `OutputGeojson` | the run-output files picked, `\|`-separated, each relative to `AIRAC_<cycle>`, e.g. `Geojson\Airways_High_Lines.geojson`. Relative, so they carry over to any cycle | none |
| `AiracLayers` | live layers switched on: `ArtccBoundaries`, `ToweredAirports`, `Navaids` | none |
| `Home` | the home view, `<lat>,<lon>,<zoom>`, e.g. `34.05,-118.25,6.5` | the contiguous US |
| `BaseMapLayers` | `UsStates`, `Coastlines`; empty draws no base map | both |
| `BaseMapOpacity` | percent, `10`-`100` | `50` |
| `Gridlines` | `Y` / `N` | `Y` |

### Sub-service and conversion nodes

| Tab | Node |
|---|---|
| Airports | `Services.AiracService.Airports` |
| Airways | `Services.AiracService.Airways` |
| Departures | `Services.AiracService.Departures` |
| Arrivals | `Services.AiracService.Arrivals` |
| NAVAIDs | `Services.AiracService.Navaids` |
| ARTCC Boundaries | `Services.AiracService.ArtccBoundaries` |
| Fixes | `Services.AiracService.Fixes` |
| Wx Stations | `Services.AiracService.WxStations` |
| Procedures | `Services.AiracService.Procedures` |
| Telephony | `Services.AiracService.Telephony` |
| Concatenate Aliases | `Services.AiracService.ConcatenateAliases` |
| DAT to GeoJSON | `Services.FileConversions.DatToGeojson` |
| SCT2 to GeoJSON | `Services.FileConversions.SctToGeojson` |
| ERAM to GeoJSON | `Services.FileConversions.EramToGeojson` |

## Keys every AIRAC sub-service shares

Read by `SubServiceSettingsReader`; saved by `GeojsonSubServiceViewModel`. Concatenate Aliases reads
none of them.

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `OutputDirectory` | not saved | the folder the run writes into (below) | **required** |
| `CoordinatePrecision` | `Services.AiracService.CoordinatePrecision` | `1`-`15`, or `0` not to round (`GeojsonFileWriter.NoRounding`) | `6` |
| `IncludeFebCustomProperties` | | `Y` / `N` | `N` |
| `FebProperties` | | list of the sub-service's `feb.*` names; **required** when the above is `Y` | none |
| `CrcDefaultsFor` | `CrcDefaultsScope` (`None`, `AllGeojsonFiles`, `SpecificFiles`) and `CrcDefaultsFiles` | list of the sub-service's GeoJSON [file keys](#file-keys) that get CRC-ERAM defaults | none |
| `FilterByRoi` | `Roi.OverrideDefaultRoi` | `Y` / `N` | `N` |
| `RoiSwLat`, `RoiSwLon`, `RoiNeLat`, `RoiNeLon` | `Roi.OverrideCorners.SwLat`, `.SwLon`, `.NeLat`, `.NeLon` | decimal degrees; **required** with `FilterByRoi = Y` | none |
| `Crc.<Class>.<Kind>.<field>` | `CrcEramPropertyDefaults.<row>.<field>` | see [CRC defaults](#crc-defaults) | none |

- The app sends the override box when a tab overrides the ROI, otherwise the default ROI.
- `CrcDefaultsFiles` keeps every choice, even for files the current settings don't write, so a file
  switched off and on keeps its choice. A run sends only the files written.
- A sub-service's outputs (`GenerateGeojson`, `GenerateAliasFile`, and Procedures' two documents) are
  set on the General tab and saved there, under `Services.AiracService.Outputs` - never in the
  sub-service's own node, so saving or undoing its tab can't change them.

### Where files go

`AiracService` sets `OutputDirectory` on every block to the run's cycle folder,
`<output>[\FE-Buddy_Output]\AIRAC_<cycle>`; the harness and tests pass their own. Inside it,
`AiracOutputPaths` lays the files out:

| File | Goes in |
|---|---|
| Alias file | `Aliases\` |
| `Combined_Alias.txt` | `Aliases\`, when [Concatenate Aliases](#concatenate-aliases) combines the alias files |
| GeoJSON | `Geojson\` (Departures and Arrivals: `Geojson\<ARTCC>\<airport>\`) |
| Procedures' documents | `Publication_Docs\` |
| `Duplicate_Alias_Commands.txt` | the cycle folder itself, whenever the run wrote an alias file |

## File keys

A **file key** names one output file: a GeoJSON file's name without `.geojson`, or the alias file's
name. `CrcDefaultsFor` and the File Names tab use them, matching case-insensitively. A
`CrcDefaultsFor` key that isn't one of the sub-service's GeoJSON keys throws. A key for a file that
isn't written this run does nothing. Each sub-service's keys are in its `*OutputFiles` class.

| Sub-service | GeoJSON keys | Alias key |
|---|---|---|
| Airports | `Runways_Lines`, `Airports_Symbols`, `Airports_Text` | `Airports.txt` |
| Airways | `Airways_<group>_Lines` / `_Symbols` / `_Text`; `<group>` is `High` or `Low` (`OutputBy=HighLow`), or a designation (`OutputBy=Designation`) | `Airways.txt` |
| Departures | `Departures_Lines`, `Departures_Symbols`, `Departures_Text` (every procedure's file of that kind) | `Departures.txt` |
| Arrivals | `Arrivals_Lines`, `Arrivals_Symbols`, `Arrivals_Text` (likewise) | `Arrivals.txt` |
| NAVAIDs | `NAVAIDs_Symbols`, `NAVAIDs_Text`, or `NAVAIDs_<Token>s_Symbols` / `_Text` per type, e.g. `NAVAIDs_VOR-DMEs_Text` | `Navaids.txt` |
| ARTCC Boundaries | `ARTCC-Boundary_High_Lines`, `_Low_Lines`, `_Unlimited_Lines`, or `ARTCC-Boundary_<ARTCC>-<ALTITUDE>_Lines`, e.g. `ARTCC-Boundary_ZOB-HIGH_Lines` | none |
| Fixes | `Fix_Symbols`, `Fix_Text`, or `Fix_<Group>_Symbols` / `_Text`, e.g. `Fix_WYPNT_Symbols`, `Fix_ENROUTE-LOW-WYPNT_Text` | none |
| Wx Stations | `Wx_Symbols`, `Wx_Text` | none |
| Procedures | none | `Faa_Chart_Recall.txt` |
| Telephony | none | `Telephony.txt` |

## New file names

Renaming is not a key in any block. The File Names tab sends one more dictionary for the whole run,
`AiracServiceSettings.FileNames`: file key → new name **without an extension**, e.g.
`Airways_High_Lines` = `ZOB High`. `OutputFileNamesParser` reads it before anything is written, and
every sub-service's `Run` takes the result (`OutputFileNames`); it is optional for the harness and
tests.

- A renamed file keeps its folder and extension: `ZOB High.geojson`.
- Every file key above can be renamed except the Departures and Arrivals GeoJSON keys, whose files
  are named per procedure. So can `Procedure_Changes.md`, `Procedures.json`, `Combined_Alias.txt` and
  `Duplicate_Alias_Commands.txt`, each keyed by its own name.
- Any other key is a warning and is ignored; a blank name keeps FE-Buddy's.
- A bad name throws before the run starts: `\ / : * ? " < > |` or a control character, ending in a
  dot or in `.geojson`, `.txt`, `.md` or `.json`, a Windows device name (`CON`, `NUL`, `COM1`…), more
  than 100 characters after trimming, or the same new name and extension for two files.
  `OutputFileNames.Problem` holds the rules; the File Names tab also refuses another file's
  FE-Buddy name.
- Everything else still knows the file by its key: `CrcDefaultsFor`, the combined alias file and the
  duplicate report.

## CRC defaults

Block keys are `Crc.<Class>.<Kind>.<field>`, e.g. `Crc.High.Line.bcg`. Saved settings keep the same
values as `CrcEramPropertyDefaults.<row>.<field>`.

| Kind | Fields |
|---|---|
| `Line` | `bcg` (1-40), `filters` (a list of at least one, each 0-40), `style` (`solid`, `shortDashed`, `longDashed`, `longDashShortDash`), `thickness` (1-3) |
| `Symbol` | `bcg`, `filters`, `style` (a CRC symbol: `vor`, `ndb`, `airport`, `rnav`, … - `CrcPropertyValidator.ValidSymbolStyles`), `size` (1-4) |
| `Text` | `bcg`, `filters`, `size` (0-5), `underline` (`Y`/`N`), `opaque` (`Y`/`N`), `xOffset`, `yOffset` (whole numbers) |

| Tab | Classes (block) | Rows (saved) |
|---|---|---|
| Airports | `Runways` (Line), `Airports` (Symbol, Text) | `Runways_Line`, `Airports_Symbol`, `Airports_Text` |
| Airways | `High`, `Low`, `Other`, each Line, Symbol, Text | `High_Line`, `High_Symbol`, `High_Text`, … `Other_Text` |
| Departures, Arrivals | `Departures` / `Arrivals`, each Line, Symbol, Text | `Departures_Line`, … / `Arrivals_Line`, … |
| NAVAIDs | `NAVAIDs`, or one per type token (`VOR`, `VORTAC`, `VOR-DME`, `VOT`, `TACAN`, `DME`, `NDB`, `NDB-DME`, `MARINE-NDB`, `MARINE-NDB-DME`, `UHF-NDB`, `FAN-MARKER`, `CONSOLAN`); Symbol, Text | `NAVAIDs_Symbol`, `NAVAIDs_Text`, or `<Token>_Symbol`, `<Token>_Text` |
| ARTCC Boundaries | `High`, `Low` (and `Unlimited`), or one per ARTCC and altitude (`ZOB-HIGH`); Line only | `High_Line`, `Low_Line`, `Unlimited_Line`, or `ZOB-HIGH_Line`, … |
| Fixes | `Fix`, or one per fix use, chart or combination (`WYPNT`, `ENROUTE-LOW`, `ENROUTE-LOW-WYPNT`); Symbol, Text | `Fix_Symbol`, `Fix_Text`, or `<Group>_Symbol`, `<Group>_Text` |
| Wx Stations | `Wx` (Symbol, Text) | `Wx_Symbol`, `Wx_Text` |
| DAT to GeoJSON | `VideoMap` (Line) | `VideoMap_Line` |
| SCT2 to GeoJSON | `SectorFile` (Line, Text) | `SectorFile_Line`, `SectorFile_Text` |
| ERAM to GeoJSON | `GeoMap` (Line, Symbol, Text) | `GeoMap_Line`, `GeoMap_Symbol`, `GeoMap_Text` |

- Only the defaults a file actually needs are required: one being written *and* in `CrcDefaultsFor`
  (for a conversion, a kind it writes whose `IncludeCrc…Defaults` is `Y`). Other valid CRC keys are
  ignored; a key for a class, kind or field the sub-service doesn't have is a warning.
- Values are checked against what CRC can draw; a bad value throws, naming the key.
- ARTCC Boundaries' per-ARTCC classes and Fixes' group classes come from the cycle's own data
  (`AddCrcRows`), not a fixed list.

## Airports

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `GenerateGeojson` | `Outputs.Airports.Geojson` (General tab) | `Y` / `N` | `Y` |
| `GenerateAliasFile` | `Outputs.Airports.Alias` (General tab) | `Y` / `N` | `Y` |
| `EmitRunwayLines`, `EmitAirportSymbols`, `EmitAirportText` | | `Y` / `N` | `Y` |

- **`FebProperties`:** `faaId`, `icaoId`, `name`, `elev`, `respArtcc`, `tfcPtrnAlt`, `fssId`, `twrType`,
  `rwyId`.
- `GenerateGeojson` and `GenerateAliasFile` can't both be `N`, and `GenerateGeojson = Y` needs at
  least one `Emit…`.

## Airways

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `GenerateGeojson` | `Outputs.Airways.Geojson` (General tab) | `Y` / `N` | `Y` |
| `OutputBy` | | `HighLow`, `Designation` | **required** while `GenerateGeojson = Y` (saved: `HighLow`) |
| `EmitLines`, `EmitSymbols`, `EmitText` | | `Y` / `N` | `Y` |
| `GenerateAliasFile` | `Outputs.Airways.Alias` (General tab) | `Y` / `N` | `Y` |
| `AliasRoiScope` | | `All`, `RoiAirways` | `All` |
| `BufferAirwayWaypoints` | | `Y` / `N` | `N` |
| `FixBufferNm` | | NM short of a five-letter fix, `0`-`10` | `2.5` |
| `NavaidBufferNm` | | NM short of any other waypoint, `0`-`10` | `5` |
| `SplitAtAntimeridian` | | `Y` / `N` | `Y` |
| `ExcludedDesignations` | | list, e.g. `RN,SL` | none |
| `HighDesignations`, `LowDesignations`, `BothDesignations` | | lists: the designations in the High file, the Low file, or both | `J,Q` High, `V,T` Low |

- **`FebProperties`:** `awyId`, `pointId`, `waypoints`.
- `GenerateGeojson` and `GenerateAliasFile` can't both be `N`, and `GenerateGeojson = Y` needs at
  least one `Emit…`.
- The buffer distances are read only when `BufferAirwayWaypoints = Y` and `GenerateGeojson = Y`.
- **High and Low files:** with `HighLow`, an airway goes in the file its designation is listed
  for, not by its published altitudes. A designation may be in only one list. One in none is left
  out of both files, with an advisory. With none of the three keys present, J and Q go High and V
  and T Low; once any is present, the lists are taken as given (the app always sends all three).
- **CRC classes:** with `HighLow`, a file needs only its own class (`Airways_High_Lines` needs
  `Crc.High.Line.*`). With `Designation`, one file can hold every altitude class, so it needs all
  three.

## Departures and Arrivals

The same keys, except that Arrivals has no `IncludeObstacleDepartures` (sending it is an
unknown-key warning).

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `GenerateGeojson` | `Outputs.<Departures/Arrivals>.Geojson` (General tab) | `Y` / `N` | `Y` |
| `GenerateAliasFile` | `Outputs.<Departures/Arrivals>.Alias` (General tab) | `Y` / `N` | `Y` |
| `EmitLines`, `EmitSymbols`, `EmitText` | | `Y` / `N` | `Y` |
| `IncludeObstacleDepartures` | | `Y` / `N` - Departures only | `Y` |
| `ArtccFilter` | | list of ARTCC IDs; empty means all | saved: the Facility Profile ARTCC, until first saved |
| `AmendmentFilter` | `Amendment.Filter` | `None`, `Cycles`, `Days`, `Date` | `None` |
| `AmendedWithinCycles` | `Amendment.WithinCycles` | 1-1000 (1 = the selected cycle); **required** with `Cycles` | saved: `1` |
| `AmendedWithinDays` | `Amendment.WithinDays` | 1-36500, back from today; **required** with `Days` | saved: `30` |
| `AmendedOnOrAfter` | `Amendment.OnOrAfter` | `yyyy-MM-dd`; **required** with `Date` | none |
| `RoiMode` | `Roi.Mode` | `Airport` (every procedure of an airport in the ROI), `Waypoint` (any procedure with a point in it) | `Airport` |

- **`FebProperties`:** Departures `dpName`, Arrivals `arrivalName`; both `pointId`, `arptId`, `artcc`,
  `amendmentNo`, `amendEffDate`, `waypoints`.
- `GenerateGeojson` and `GenerateAliasFile` can't both be `N`, and `GenerateGeojson = Y` needs at
  least one `Emit…`.
- Only the chosen amendment mode's value is read.

## NAVAIDs

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `GenerateGeojson` | `Outputs.Navaids.Geojson` (General tab) | `Y` / `N` | `Y` |
| `GenerateAliasFile` | `Outputs.Navaids.Alias` (General tab) | `Y` / `N` | `Y` |
| `EmitSymbols`, `EmitText` | | `Y` / `N` (there is no Lines file) | `Y` |
| `OutputBy` | | `All`, `Type` | `All` |
| `ExcludedTypes` | | list of NASR `NAV_TYPE` names, left out of the GeoJSON and the alias file | none |
| `SymbolStyleBy` | | `Type`, `File` - read only with `OutputBy = All` | `Type` |
| `FanMarkerStyle` | | a CRC symbol style for fan markers, when the merged Symbols file gets CRC defaults styled by type | none |

- **`FebProperties`:** `navId`, `navType`, `name`, `freq`, `lowAltArtccId`, `highAltArtccId`. The Text
  file never carries `navId`, `navType` or `name`; its label already shows them.
- `GenerateGeojson` and `GenerateAliasFile` can't both be `N`; `GenerateGeojson = Y` needs `EmitSymbols`
  or `EmitText`.
- An unknown name in `ExcludedTypes` warns but is still excluded, so a type NASR adds can be
  unticked. Excluding every known type throws.
- A fan marker with no `FanMarkerStyle` gets no style, with a warning; an invalid style throws. The
  app requires one whenever the merged Symbols file gets CRC defaults styled by type and fan markers
  are included.

## ARTCC Boundaries

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `OutputBy` | | `HighLow`, `HighLowUnlimited`, `ArtccAltitude` | `HighLow` |
| `LocationFilter` | | list of ARTCC IDs; empty means every one with boundary data | saved: the Facility Profile ARTCC, until first saved |
| `SplitAtAntimeridian` | | `Y` / `N` | `Y` |

- No `GenerateGeojson`, `GenerateAliasFile` or `Emit…`: it always writes Lines only.
- **`FebProperties`:** `locationId`, `locationName`, `locationType`, `icaoId`, `computerId`, `altitude`,
  `type` (`ARTCC`, `CTA`, `FIR`, `CTA/FIR` or `UTA`), `city`, `countryCode`.

## Fixes

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `EmitSymbols`, `EmitText` | | `Y` / `N`, not both `N` | `Y` |
| `OutputBy` | | `All`, `FixUse`, `Chart`, `ChartAndFixUse` | `All` |
| `ExcludedFixUses` | | list of fix use names; `FixUse` layout only | none |
| `ExcludedCharts` | | list of NASR chart names; `Chart` layout only | none |
| `Combinations` | | list of `<Chart>+<FixUse>`, e.g. `ENROUTE-LOW+WYPNT`; **required** with `ChartAndFixUse` | none |

- No `GenerateGeojson` or `GenerateAliasFile`: Fixes always writes GeoJSON and has no alias file.
- **`FebProperties`:** `fixId`, `fixUseCode`, `charts`. The Text file never carries `fixId`.
- An unknown fix use in `ExcludedFixUses` or `Combinations` warns but is still used. Excluding every
  known fix use throws with `OutputBy = FixUse`, as does a combination that isn't both a chart and a
  fix use.

## Wx Stations

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `EmitSymbols`, `EmitText` | | `Y` / `N`, not both `N` | `Y` |

No other keys of its own, and no `FebProperties`: `IncludeFebCustomProperties = Y` only warns.

## Procedures

Procedures writes no GeoJSON, so it has no `Emit…`, `FebProperties`, `CrcDefaultsFor` or `Crc.*` keys
(`IncludeFebCustomProperties = Y` warns, pointing at `JsonFields`). `IncludeRoiAirports` uses the
tab's ROI override if it has one, otherwise the default ROI.

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `GenerateChangesDocument` | `Outputs.Procedures.ProcedureChanges` (General tab) | `Y` / `N` - `Procedure_Changes.md` | `Y` |
| `GenerateProceduresJson` | `Outputs.Procedures.ProceduresJson` (General tab) | `Y` / `N` - `Procedures.json` | `Y` |
| `GenerateAliasFile` | `Outputs.Procedures.Alias` (General tab) | `Y` / `N` - `Faa_Chart_Recall.txt` | `Y` |
| `Facilities` | | list of ARTCC IDs whose airports are included | saved: the Facility Profile ARTCC, until first saved |
| `PrimaryFacility` | not saved (`Services.AiracService.UserArtccId`) | the ARTCC whose section leads both documents | none |
| `IncludeRoiAirports` | | `Y` / `N` - include every airport inside the ROI; needs a ROI | `N` |
| `Airports` | | list of FAA or ICAO IDs (saved as the FAA ID) | none |
| `Procedures` | | list of procedure names, included wherever charted | none |
| `AirportProcedures` | | list of `<airport>\|<procedure>`, e.g. `PIT\|ILS OR LOC RWY 28C` | none |
| `ChartTypes` | | d-TPP chart codes a whole airport contributes: `IAP`, `STR`, `DP`, `ODP`, `DAU`, `APD`, `MIN`, `HOT`, `LAH` | `IAP,STR,DP,ODP,DAU,APD` |
| `JsonFields` | | optional `Procedures.json` fields (`ProcedureJsonField`) | `icaoId,airportName,responsibleArtcc,airspaceClass,chartType,chartUrl,change,compareUrl` |

- The three `Generate…` keys can't all be `N`.
- At least one of `Facilities`, `IncludeRoiAirports`, `Airports`, `Procedures` or
  `AirportProcedures` is required, but only when a document is on. They add up. The alias file
  ignores them all: it covers every chart at every airport in the d-TPP Metafile.
- An unknown `ChartTypes` code warns but is still used; an unknown `JsonFields` name warns and is
  ignored.
- `JsonFields`: `icaoId`, `airportName`, `city`, `state`, `responsibleArtcc`, `airspaceClass`,
  `military`, `chartType`, `chartUrl`, `change`, `compareUrl`, `amendment`, `amendmentDate`,
  `procedureUid`, `computerCode`, `producer`. An airport's `airportId` and a procedure's `name` are
  always written; a field with no value is left out.
- The chart recall rules are in the XML docs of `ChartRecallCodes`, `ApproachCodes`, `SidStarCodes`,
  `ChartRecallSkipReason` and `ChartRecallAliasBuilder`.

## Telephony

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `GenerateAliasFile` | not saved: always `Y` while Telephony is included | must be `Y`: `Telephony.txt` is Telephony's only output | `Y` |
| `VirtualAirlines.<n>.Designator` | | a virtual airline's three-letter designator (`<n>` from 1) | none |
| `VirtualAirlines.<n>.Telephony` | | its telephony | none |
| `VirtualAirlines.<n>.Organization` | | its virtual organization | none |
| `IncludeVatsimRadarVirtualAirlines` | | `Y` / `N` - also write the VATSIM-Radar Virtual Airline List | `N` |

- No `FebProperties`, `CrcDefaultsFor` or `Crc.*`, and no region: the ROI keys are accepted and
  ignored, and `IncludeFebCustomProperties = Y` warns.
- Virtual airlines are read in number order. Each needs a three-letter `Designator`, a `Telephony`
  with a letter or digit, and an `Organization` (`TelephonySettingsParser.VirtualAirlineProblem`, which
  the tab uses too); otherwise the run throws, naming the number. A number with no fields is
  skipped; a repeat of an earlier one (ignoring case) is written once, with an Info message.
- The tab saves the list whole, renumbered from 1, so a deleted entry leaves no keys behind.
- The AIRAC Service checks `IncludeVatsimRadarVirtualAirlines` before the run
  (`TelephonySettingsParser.IncludesVatsimRadarList`) to know whether to download the list.

## Concatenate Aliases

Whether to combine the run's alias files into `Aliases\Combined_Alias.txt`, then numbered keys, one
group per custom alias file (`<n>` from 1), merged in number order. Saved the same way; every save
removes the `Sources` subtree first. The block is only in a run while an included sub-service makes
an alias file; without it, no combined file is written.

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `CombineAliasFiles` | | `Y` / `N` | `Y` |
| `Sources.<n>.FilePath` | | full path of an alias file on this PC | none |
| `Sources.<n>.Url` | | `http://` or `https://` address, with no sign-in written into it | none |
| `Sources.<n>.CredentialId` | | a saved credential's id (`"N"` GUID format), never the secret | none |

- It reads none of the shared keys; `OutputDirectory` is accepted and ignored. Any other key is a
  warning.
- Each `<n>` needs exactly one of `FilePath` and `Url`. These throw: both; neither, when the group has
  a `CredentialId` (an empty group is skipped); a relative `FilePath`; a `Url` that isn't http(s) or
  has a sign-in in it (`UrlSecrets` - the message never repeats the address); a `Url` group's
  `CredentialId` that isn't a GUID. A `CredentialId` on a `FilePath` is ignored, with an Info message.
- With `CombineAliasFiles = N` the `Sources` keys are not read or checked, the app doesn't send them,
  and no combined file is written; one an earlier run left is deleted when this run rewrites the
  alias files.
- No sources is allowed: `Combined_Alias.txt` then holds FE-Buddy's aliases only, with an advisory
  warning. The tab is invalid only while combining and a listed file has a problem.

## File conversions

Every conversion reads these (`ConversionSettingsReader`), plus `CoordinatePrecision` and `Crc.*`. It
writes into its own folder beside the `AIRAC_<cycle>` folders. Files picked one by one are not saved;
the folder is.

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `OutputDirectory` | not saved (Settings' output folder) | folder | **required** |
| `AddFeBuddyOutputFolder` | not saved (`General.AddFeBuddyOutputFolder`) | `Y` / `N` | `Y` |
| `SourceFolder` | | a folder; every file directly in it with the conversion's extension | - |
| `SourceFiles` | not saved | file paths separated by `\|` | - |
| - | `SourceType` | `Folder`, `Files` | `Folder` |
| `IncludeCrcLineDefaults`, `IncludeCrcSymbolDefaults`, `IncludeCrcTextDefaults` | | `Y` / `N` - write that kind's CRC defaults; read only for kinds the conversion writes | block `N`, saved `Y` |

Exactly one of `SourceFolder` and `SourceFiles` is required. A missing folder throws; an unreadable
file fails that file only.

| Conversion | Extensions | Own keys |
|---|---|---|
| DAT to GeoJSON | `.dat` | `CroppingDistance` - NM from each map's point of tangency, above 0 and at most 1000; blank keeps everything |
| SCT2 to GeoJSON | `.sct2`, `.sct` | none |
| ERAM to GeoJSON | `.xml` - one Geomaps file per run, recognised by its contents, whatever its name | below |

**ERAM to GeoJSON:**

| Block key | Values | Default |
|---|---|---|
| `OutputLayout` | `ByFilters`, `ByAttributes`, `Raw`, `RawPlus` | `ByAttributes` |
| `DefaultsSource` | `Xml`, `XmlThenCard`, `Card` | `Xml` |
| `IncludeFebCustomProperties` | `Y` / `N` | `N` |
| `FebProperties` | `mapObjectType`, `mapGroupId`, `lineObjectId`, `symbolId`, `saaId`; **required** with the above `Y` | none |

- A `SourceFolder` may hold the whole export but only one Geomaps file; `SourceFiles` only one file.
  More throws, and a picked file that isn't a Geomaps file fails.
- `ERAM_TO_GEOJSON` is emptied once the file has been read, before anything is written. A
  `ConsoleCommandControl.xml` beside the Geomaps file is read for `ConsoleCommandControl.txt`
  (`ConversionServiceResult.OtherFilesWritten`); no key turns that off.
- The tab's CRC defaults are read only with `XmlThenCard` or `Card`.

## Settings export and import

Settings ▸ **Export…** writes a file another user can bring in with **Import…** (`UserConfigTransfer`):
the same tree as `UserConfig.json`, under a header.

```json
{ "format": "FE-Buddy.UserConfig", "formatVersion": 1, "appVersion": "3.0.0",
  "exportedUtc": "2026-09-27T12:00:00Z", "settings": { "General": { ... }, "Services": { ... } } }
```

A plain `UserConfig.json` imports too. A file with a newer `formatVersion`, or over 2 MB, is refused.

**Each key travels according to its name** (`UserConfigPortability.Classify`), so a new setting needs
no list:

| Scope | Keys | Export | Import |
|---|---|---|---|
| Secret | under `Secrets`, or ending in `Token`, `Password`, `Secret`, `ApiKey`, `Credential` or `Credentials`, or called `Pat` | never | this PC's is kept |
| Local | `General.UpdateChannel`, `NewsLastOpen`, `FeBuddyGitHub.CredentialId`, `LegacyGitHubTokenNoticeShown`, and anything outside `General` and `Services` | never | this PC's is kept |
| Credential choice | ending in `CredentialId` | never | this PC's is kept only where the settings beside it are unchanged (the same custom alias file at the same address) |
| Machine path | ending in `Folder`, `Directory` or `FilePath` (not `AddFeBuddyOutputFolder`) | your Desktop, Documents or profile become `%DESKTOP%`, `%DOCUMENTS%`, `%USERPROFILE%` | taken only where it works on this PC (below) |
| Shared | everything else under `General` and `Services` | as it is | as it is |

- A machine path is taken when it is an output folder (a name with `Output` in it) on a drive this PC
  has, a folder FE-Buddy reads from that exists, or a file that exists. A network path is refused
  unless it is this user's own Desktop, Documents or profile. Otherwise this PC keeps its own; a
  custom alias file is left out instead, since this PC's entry at that number is a different file.
- **An import makes this PC's settings match the file**: anything the file leaves out goes back to
  its default, except the Local, Secret and credential-choice keys above.
- `UserConfigFile.ReplaceAll` writes the new file and swaps it in with `File.Replace`, keeping the
  old one as `UserConfig.before-import.json`. It deletes `UserConfig.previous.json` (its undo would
  restore pre-import settings), and every open page reloads (`ConfigPages.ReloadAll`,
  `MapLayersState.ReloadFromConfigIfCreated`).

## Adding a setting

1. **Pick its node:** `General` for app-wide preferences, the tab's own node otherwise.
2. **Save and load it** in the tab's `LoadFromConfig` / `WriteToConfig` with `Get` / `Set` (relative
   to the tab's node). If more than one class reads it, add a constant to `UserConfigKeys`.
3. **If Core needs it:** send it from `BuildSettingsBlock` (or
   `GeojsonSubServiceViewModel.AddSharedSettings`), read it in the parser with `SettingsValueReader`,
   and add it to the parser's `OwnKeys` (or `SubServiceSettingsReader.CommonKeys`), or it is reported
   as unknown. Add it to `HarnessSettings.cs` and a parser test.
4. **Mind its name** - it decides how it travels in an export. A name ending in `Folder`,
   `Directory` or `FilePath` is a machine path, `CredentialId` a credential choice, `Token`,
   `Password` and the like a secret. A yes/no setting ending in `Folder` goes in
   `UserConfigPortability`'s `NotFolderKeys`, and a this-PC-only setting in its `LocalKeys`.
5. **List it on this page.**
