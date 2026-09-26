# UserConfig.json reference

Every setting FE-Buddy saves, where it lives, and which code reads it.

**The file:** `%APPDATA%\FE-Buddy\UserConfig.json` - one nested JSON tree. Code addresses a value
by its dotted path (`General.UpdateChannel`) through `UserConfigFile`, and constants for the keys
read in more than one place are in `UserConfigKeys`. Before a node is saved, its previous state
goes to `UserConfig.previous.json` (the one-step **Undo last save**).

**Values are strings.** Yes/no settings are `Y` / `N` (the readers also accept `true`); a blank or
missing value means "use the default". Adding a key needs no migration: an old file simply lacks it.

The settings below map closely, but not exactly, to the [settings blocks](Settings-Blocks.md) a
tab hands Core at run time: `UserConfig.json` is what the GUI remembers, the settings block is
what one run uses.

## General

Written by **Settings** (except `NewsLastOpen`).

| Key | Values | Default | Read by |
|---|---|---|---|
| `UpdateChannel` | `Stable`, `ReleaseCandidate`, `Beta`, `Alpha` (the GUI offers Stable, Beta, Alpha) | `Stable` | launch version check, Settings |
| `NewsLastOpen` | the newest News `PostId` seen, e.g. `2026-08-30.3` | none | launch News check. Written when the user opens News. |
| `PrettyPrintGeojson` | `Y` / `N` | `N` | `OutputFormatting` (every GeoJSON writer) |
| `DefaultOutputDirectory` | a folder path | the Desktop | every run, through `Shell/OutputPreferences`. An AIRAC Service run writes into `AIRAC_<cycle>` inside it; a file conversion into its own folder. |
| `AddFeBuddyOutputFolder` | `Y` / `N` - put the `AIRAC_<cycle>` and conversion folders in a `FE-Buddy_Output` folder | `Y` | every run, through `Shell/OutputPreferences` |

## Services.AiracService

| Key | Values | Default | Written / read by |
|---|---|---|---|
| `AiracCycleId` | a cycle ID, e.g. `2610` | current cycle | General tab. The ID (not "previous/current/next") is saved; on load it is matched back to one of the three, or falls back to current. |
| `SelectedSubServices` | comma-separated keys: `Airports`, `Airways`, `Departures`, `Arrivals`, `Navaids`, `ArtccBoundaries`, `Fixes` | none | General tab. Keys are stable identifiers - never rename one without migrating this value. |
| `UserArtccId` | an ARTCC ID, e.g. `ZOB` | none | Settings ▸ Facility. Not read by any sub-service yet. |
| `CoordinatePrecision` | `0`-`15` (the GUI offers 5, 6, 7) | `6` | Settings; sent by every tab that writes GeoJSON, AIRAC and File Conversions alike. |

### Services.AiracService.DefaultRoi

The shared default Region of Interest, written by `DefaultRoiStore` (Settings and the Map page) as
soon as it is set or cleared.

| Key | Values | Default |
|---|---|---|
| `FilterByRoi` | `true` / `false` - note: not `Y`/`N` | `false` |
| `DefaultCoordindates.SwLat`, `.SwLon`, `.NeLat`, `.NeLon` | decimal degrees | none |

Clearing the default ROI only sets `FilterByRoi` to `false`; the corners stay, so re-enabling it
restores the last box. (`Coordindates` is misspelled in every saved file, so the key keeps the
spelling - see [TODO](TODO.md).)

## Sub-service nodes

Each GeoJSON sub-service tab saves its own node, with **Save** on its tab:

| Sub-service | Node |
|---|---|
| Airports | `Services.AiracService.Airports` |
| Airways | `Services.AiracService.Geojson.Airways` |
| Departures | `Services.AiracService.Departures` |
| Arrivals | `Services.AiracService.Arrivals` |
| NAVAIDs | `Services.AiracService.Navaids` |
| ARTCC Boundaries | `Services.AiracService.ArtccBoundaries` |
| Fixes | `Services.AiracService.Fixes` |

### Keys every GeoJSON sub-service saves

Written by `GeojsonSubServiceViewModel`, under the sub-service's node.

| Key | Values | Default |
|---|---|---|
| `GenerateAliasFile` | `Y` / `N` - not saved for ARTCC Boundaries or Fixes, neither of which has an alias file | `Y` |
| `EmitLines`, `EmitSymbols`, `EmitText` (Airports: `EmitRunwayLines`, `EmitAirportSymbols`, `EmitAirportText`; NAVAIDs and Fixes: `EmitSymbols`, `EmitText` only - there is no `EmitLines`; ARTCC Boundaries: none of these - it always writes Lines only, with no choice) | `Y` / `N` | `Y` |
| `IncludeFebCustomProperties` | `Y` / `N` | `N` |
| `FebProperties` | comma-separated property names, e.g. `awyId,pointId` | none |
| `Vnas.UploadFiles` | comma-separated file keys marked for vNAS (see [Settings blocks](Settings-Blocks.md#vnas-file-keys)), e.g. `Airways_High_Lines,Airways.txt` | none |
| `Vnas.CrcDefaults` | which vNAS GeoJSON files get CRC-ERAM defaults: `None`, `AllVnasFiles`, `SpecificFiles` | `None` |
| `Vnas.CrcFiles` | comma-separated file keys ticked for CRC-ERAM defaults; read with `SpecificFiles` | none |
| `CrcEramPropertyDefaults.<row>.<field>` | see below | none (the user must fill the rows in use) |
| `Roi.OverrideDefaultRoi` | `Y` / `N` | `N` |
| `Roi.OverrideCoordindates.SwLat`, `.SwLon`, `.NeLat`, `.NeLon` | decimal degrees, as typed | none |

**CRC defaults rows.** `<row>` is one per file kind and class:

| Sub-service | Rows |
|---|---|
| Airports | `Runways_Line`, `Airports_Symbol`, `Airports_Text` |
| Airways | `Lines.Airway_<Class>_Lines`, `Symbols.Airway_<Class>_Symbols`, `Texts.Airway_<Class>_Texts`, for `<Class>` = `High`, `Low`, `Other` |
| Departures | `Departures_Line`, `Departures_Symbol`, `Departures_Text` |
| Arrivals | `Arrivals_Line`, `Arrivals_Symbol`, `Arrivals_Text` |
| NAVAIDs | `NAVAIDs_Symbol`, `NAVAIDs_Text` (`OutputBy=All`), or `<Token>_Symbol`, `<Token>_Text` per NAVAID type present (`OutputBy=Type`) - `<Token>` is `VOR`, `VORTAC`, `VOR-DME`, `VOT`, `TACAN`, `DME`, `NDB`, `NDB-DME`, `MARINE-NDB`, `MARINE-NDB-DME`, `UHF-NDB`, `FAN-MARKER` or `CONSOLAN`. There is no `_Line` row: NAVAIDs writes no Lines file. |
| ARTCC Boundaries | `High_Line`, `Low_Line` (`OutputBy=HighLow`), adds `Unlimited_Line` (`OutputBy=HighLowUnlimited`), or `<LocationId>-<ALTITUDE>_Line` per ARTCC and altitude present (`OutputBy=ArtccAltitude`), e.g. `ZOB-HIGH_Line`. These rows come from the cycle's own data (`AddCrcRows`), not a fixed list. There is no `_Symbol` or `_Text` row: ARTCC Boundaries writes Lines only. |
| Fixes | `Fix_Symbol`, `Fix_Text` (`OutputBy=All`), or `<Group>_Symbol`, `<Group>_Text` per fix use, chart, or chart + fix use combination present (`OutputBy=FixUse`/`Chart`/`ChartAndFixUse`), e.g. `WYPNT_Symbol`, `ENROUTE-LOW-WYPNT_Text`. These rows come from the cycle's own data (`AddCrcRows`), not a fixed list. There is no `_Line` row: Fixes writes no Lines file. |

and `<field>` depends on the kind: **Line** `bcg`, `filters`, `style`, `thickness`; **Symbol** `bcg`,
`filters`, `style`, `size`; **Text** `bcg`, `filters`, `size`, `underline` (`Y`/`N`), `opaque`
(`Y`/`N`), `xOffset`, `yOffset`. Allowed values are in the [user guide](../Users/User-Guide.md#the-cards-every-sub-service-tab-shares).

**vNAS choices.** `Vnas.UploadFiles` and `Vnas.CrcFiles` keep every choice, including files the
tab's current settings do not write, so a file switched off and on again keeps its choice; a run
sends only the files actually written. Configs saved before these keys still hold
`IncludeCrcLineDefaults`, `IncludeCrcSymbolDefaults` and `IncludeCrcTextDefaults`; no sub-service
tab reads them any more (the [file conversion nodes](#file-conversion-nodes) still use their own).

### Airports only

| Key | Values | Default |
|---|---|---|
| `GenerateGeojson` | `Y` / `N` | `Y` |

### Airways only

| Key | Values | Default |
|---|---|---|
| `OutputBy` | `HighLow`, `Designation`, `None` | `HighLow` |
| `BufferAirwayWaypoints` | `Y` / `N` | `N` |
| `AliasRoiScope` | `All`, `RoiAirways` | `All` |
| `SplitAtAntimeridian` | `Y` / `N` | `Y` |
| `ExcludedDesignations` | comma-separated designations, e.g. `RN,SL` | none |

### Departures only

| Key | Values | Default |
|---|---|---|
| `GenerateGeojson` | `Y` / `N` | `Y` |
| `IncludeObstacleDepartures` | `Y` / `N` | `Y` |
| `ArtccFilter` | comma-separated ARTCC IDs; empty means all | none |
| `Roi.Mode` | `Airport`, `Waypoint` | `Airport` |
| `Amendment.Filter` | `None`, `Cycles`, `Days`, `Date` | `None` |
| `Amendment.WithinCycles` | whole number, 1-1000 | `1` |
| `Amendment.WithinDays` | whole number, 1-36500 | `30` |
| `Amendment.OnOrAfter` | `yyyy-MM-dd` | none |

### Arrivals only

| Key | Values | Default |
|---|---|---|
| `GenerateGeojson` | `Y` / `N` | `Y` |
| `ArtccFilter` | comma-separated ARTCC IDs; empty means all | none |
| `Roi.Mode` | `Airport`, `Waypoint` | `Airport` |
| `Amendment.Filter` | `None`, `Cycles`, `Days`, `Date` | `None` |
| `Amendment.WithinCycles` | whole number, 1-1000 | `1` |
| `Amendment.WithinDays` | whole number, 1-36500 | `30` |
| `Amendment.OnOrAfter` | `yyyy-MM-dd` | none |

The same keys as Departures, minus `IncludeObstacleDepartures` - a STAR has no obstacle/SID split.

### NAVAIDs only

| Key | Values | Default |
|---|---|---|
| `GenerateGeojson` | `Y` / `N` | `Y` |
| `OutputBy` | `All`, `Type` | `All` |
| `ExcludedTypes` | comma-separated NASR NAVAID types - the unticked boxes on the NAVAID Types card | none |
| `SymbolStyleBy` | `Type`, `File` | `Type` |
| `FanMarkerStyle` | a CRC symbol style, saved when fan markers need one (see [Settings blocks](Settings-Blocks.md#navaids)) | none |

There is no `Roi.Mode` or `Amendment.*`: NAVAIDs has no ARTCC filter or amendment filter - every
NAVAID NASR publishes (other than a `SHUTDOWN` one) is a candidate.

### ARTCC Boundaries only

| Key | Values | Default |
|---|---|---|
| `OutputBy` | `HighLow`, `HighLowUnlimited`, `ArtccAltitude` | `HighLow` |
| `LocationFilter` | comma-separated ARTCC IDs; empty means every one with boundary data | none |
| `SplitAtAntimeridian` | `Y` / `N` | `Y` |

There is no `GenerateGeojson`: ARTCC Boundaries always writes GeoJSON. There is also no `Roi.Mode`
or `Amendment.*`: `LocationFilter` is its only filter, and there is no amendment date to filter by.

### Fixes only

| Key | Values | Default |
|---|---|---|
| `OutputBy` | `All`, `FixUse`, `Chart`, `ChartAndFixUse` | `All` |
| `ExcludedFixUses` | comma-separated fix use names - the unticked boxes on the Fix Uses card | none |
| `ExcludedCharts` | comma-separated NASR chart names - the unticked boxes on the Charts card | none |
| `Combinations` | comma-separated `<Chart>+<FixUse>` pairs, e.g. `ENROUTE-LOW+WYPNT,IAP+RPRTNG-PNT` | none |

There is no `GenerateGeojson`: Fixes always writes GeoJSON. There is also no `Roi.Mode` or
`Amendment.*`: Fixes has no ARTCC filter or amendment filter - every fix NASR publishes is a
candidate.

## File conversion nodes

Each conversion tab on the File Conversions screen saves its own node, with **Save** on its tab
(written by `FileConversionTabViewModel`). Files picked one by one are deliberately not saved;
the folder is.

| Conversion | Node | CRC defaults rows |
|---|---|---|
| DAT to GeoJSON | `Services.FileConversions.DatToGeojson` | `VideoMap_Line` |
| SCT2 to GeoJSON | `Services.FileConversions.SctToGeojson` | `SectorFile_Line`, `SectorFile_Text` |
| ERAM to GeoJSON | `Services.FileConversions.EramToGeojson` | `GeoMap_Line`, `GeoMap_Symbol`, `GeoMap_Text` |

| Key | Values | Default |
|---|---|---|
| `SourceType` | `Folder`, `Files` | `Folder` |
| `SourceFolder` | a folder path | none |
| `IncludeCrcLineDefaults` | `Y` / `N` | `Y` |
| `IncludeCrcSymbolDefaults` | `Y` / `N` - only a conversion that writes symbols (ERAM) | `Y` |
| `IncludeCrcTextDefaults` | `Y` / `N` - only a conversion that writes text (SCT2, ERAM) | `Y` |
| `CrcEramPropertyDefaults.<row>.<field>` | the fields for the row's kind, as for the sub-services | none (the user must fill them) |
| `CroppingDistance` | DAT only: NM, as typed; blank means no cropping | none |
| `OutputLayout` | ERAM only: `ByObject`, `ByFilter` | `ByObject` |
| `DefaultsSource` | ERAM only: `Xml`, `XmlThenCard`, `Card` | `Xml` |

## Adding a setting

1. Pick its node: `General` for app-wide preferences, the sub-service's node for its own options.
2. Read and write it in the owning view-model (a sub-service tab's `LoadFromConfig` /
   `WriteToConfig` with `Get` / `Set`, which are relative to the tab's node).
3. If more than one class reads it, add a constant to `UserConfigKeys`.
4. If it is sent to Core, add it to the tab's `BuildSettingsBlock` and to the parser - and list it
   here and in [Settings blocks](Settings-Blocks.md).
