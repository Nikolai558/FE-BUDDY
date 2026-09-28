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

Written by **Settings** (except `NewsLastOpen` and `LegacyGitHubTokenNoticeShown`).

| Key | Values | Default | Read by |
|---|---|---|---|
| `UpdateChannel` | `Stable`, `ReleaseCandidate`, `Beta`, `Alpha` (the GUI offers Stable, Beta, Alpha) | `Stable` | launch version check, Settings |
| `NewsLastOpen` | the newest News `PostId` seen, e.g. `2026-08-30.3` | none | launch News check. Written when the user opens News. |
| `PrettyPrintGeojson` | `Y` / `N` | `N` | `OutputFormatting` (every GeoJSON writer) |
| `DefaultOutputDirectory` | a folder path | the Desktop | every run, through `Shell/OutputPreferences`. An AIRAC Service run writes into `AIRAC_<cycle>` inside it; a file conversion into its own folder. |
| `AddFeBuddyOutputFolder` | `Y` / `N` - put the `AIRAC_<cycle>` and conversion folders in a `FE-Buddy_Output` folder | `Y` | every run, through `Shell/OutputPreferences` |
| `FeBuddyGitHub.CredentialId` | the id of the GitHub token credential FE-Buddy's own GitHub requests use | none (no token) | `GitHubAuth`. Kept on this PC: never exported. |
| `LegacyGitHubTokenNoticeShown` | `Y` once the notice that 2.x's `FEBUDDY_GITHUB_TOKEN` variable is still set has been shown | none | `LegacyGitHubTokenNotice` at launch. Written when the notice is shown. Kept on this PC: never exported. |

## Services.AiracService

| Key | Values | Default | Written / read by |
|---|---|---|---|
| `AiracCycleId` | a cycle ID, e.g. `2610` | current cycle | General tab. The ID (not "previous/current/next") is saved; on load it is matched back to one of the three, or falls back to current. |
| `SelectedSubServices` | comma-separated keys: `Airports`, `Airways`, `Departures`, `Arrivals`, `Navaids`, `ArtccBoundaries`, `Fixes`, `WxStations`, `Procedures`, `Telephony`, `VnasAlias` | none | General tab. Keys are stable identifiers - never rename one without migrating this value. |
| `UserArtccId` | an ARTCC ID, e.g. `ZOB` | none | Settings ▸ Facility. Read by Procedures as its `PrimaryFacility` - the facility whose section leads both documents - and by `AiracService` as the run's own `PrimaryFacility`, listed first in `Duplicate_Alias_Commands.txt`. |
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

Each sub-service tab saves its own node, with **Save** on its tab:

| Sub-service | Node |
|---|---|
| Airports | `Services.AiracService.Airports` |
| Airways | `Services.AiracService.Geojson.Airways` |
| Departures | `Services.AiracService.Departures` |
| Arrivals | `Services.AiracService.Arrivals` |
| NAVAIDs | `Services.AiracService.Navaids` |
| ARTCC Boundaries | `Services.AiracService.ArtccBoundaries` |
| Fixes | `Services.AiracService.Fixes` |
| Wx Stations | `Services.AiracService.WxStations` |
| Procedures | `Services.AiracService.Procedures` |
| Telephony | `Services.AiracService.Telephony` |
| vNAS Alias Upload | `Services.AiracService.VnasAlias` |

### Keys every GeoJSON sub-service saves

Written by `GeojsonSubServiceViewModel`, under the sub-service's node.

| Key | Values | Default |
|---|---|---|
| `GenerateAliasFile` | `Y` / `N` - not saved for ARTCC Boundaries, Fixes or Wx Stations, none of which has an alias file | `Y` |
| `EmitLines`, `EmitSymbols`, `EmitText` (Airports: `EmitRunwayLines`, `EmitAirportSymbols`, `EmitAirportText`; NAVAIDs, Fixes and Wx Stations: `EmitSymbols`, `EmitText` only - there is no `EmitLines`; ARTCC Boundaries: none of these - it always writes Lines only, with no choice; Procedures and Telephony: none - neither writes any GeoJSON) | `Y` / `N` | `Y` |
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
| Wx Stations | `Wx_Symbol`, `Wx_Text` - a fixed pair, unlike ARTCC Boundaries and Fixes above, since there is only the one class. There is no `_Line` row: Wx Stations writes no Lines file. |

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

### Wx Stations only

No keys of its own beyond the ones every GeoJSON sub-service saves. There is no `GenerateGeojson`
or `OutputBy`: Wx Stations always writes GeoJSON, as one merged Symbols/Text pair. There is also no
`Roi.Mode` or `Amendment.*`: it has no ARTCC filter or amendment filter, and its data is not a NASR
cycle at all - see [Settings blocks](Settings-Blocks.md#wx-stations).

### Procedures only

Procedures writes no GeoJSON, but does write an alias file, `Faa_Chart_Recall.txt` (see
[Settings blocks](Settings-Blocks.md#procedures)). Its tab is still a `GeojsonSubServiceViewModel`,
so it saves the ["keys every GeoJSON sub-service saves"](#keys-every-geojson-sub-service-saves) above
except the `Emit…` keys, which no GeoJSON sub-service needs here; `Vnas.UploadFiles` can only ever
hold `Faa_Chart_Recall.txt`, it has no `CrcEramPropertyDefaults.*` rows (nothing it writes carries
CRC-ERAM defaults), and `IncludeFebCustomProperties` stays `N`. The ROI override (`Roi.*`) is the
region `IncludeRoiAirports` uses.

| Key | Values | Default |
|---|---|---|
| `GenerateChangesDocument` | `Y` / `N` | `Y` |
| `GenerateProceduresJson` | `Y` / `N` | `Y` |
| `GenerateAliasFile` | `Y` / `N` - whether to write `Faa_Chart_Recall.txt` | `Y` |
| `Facilities` | comma-separated ARTCC IDs - the ticked boxes on the Facilities card | your Settings ▸ Facility Profile facility, until the tab is first saved |
| `IncludeRoiAirports` | `Y` / `N` | `N` |
| `Airports` | comma-separated FAA airport identifiers (an ICAO ID typed on the tab is saved as the FAA ID) | none |
| `Procedures` | comma-separated procedure base chart names | none |
| `AirportProcedures` | comma-separated `<FAA airport ID>\|<Procedure name>` pairs, e.g. `PIT\|ILS OR LOC RWY 28C` | none |
| `ChartTypes` | comma-separated d-TPP chart codes - the ticked boxes on the Chart Types card | `IAP,STR,DP,ODP,DAU,APD` |
| `JsonFields` | comma-separated `Procedures.json` optional field names - the ticked boxes on the Procedures.json Fields card | see [Settings blocks](Settings-Blocks.md#procedures) |

There is no `Roi.Mode` or `Amendment.*`: `Facilities` plays the ARTCC-filter role Departures and
Arrivals give `ArtccFilter`, and there is no amendment date to filter procedures by. The facility
whose section leads both documents is not saved on this node at all - it is
`Services.AiracService.UserArtccId` from Settings ▸ Facility Profile (see above).

### Telephony only

No keys of its own. Telephony writes no GeoJSON at all - just its alias file, `Telephony.txt` (see
[Settings blocks](Settings-Blocks.md#telephony)), which can't be turned off: a hand-edited
`GenerateAliasFile = N` is not honoured, and the tab falls back to `Y` on load. Its tab is still a
`GeojsonSubServiceViewModel`, so it saves the ["keys every GeoJSON sub-service saves"](#keys-every-geojson-sub-service-saves)
above except the `Emit…` keys (there is nothing to emit); it has no `CrcEramPropertyDefaults.*` rows
and `IncludeFebCustomProperties` stays `N`. `Vnas.UploadFiles` can only ever hold `Telephony.txt`.
The ROI keys (`Roi.OverrideDefaultRoi` and its corners) are saved like every other sub-service's,
but Telephony has no Region of Interest card to set them from and its parser ignores them - every
operator gets a card regardless of area.

### vNAS Alias Upload only

Not a `GeojsonSubServiceViewModel` (`VnasAliasViewModel` derives from `SubServiceSettingsViewModel`),
so it saves none of the keys above - only its list of custom alias files, as numbered keys, the
same keys its settings block sends (see [Settings blocks](Settings-Blocks.md#vnas-alias-upload)).
Each save first removes the whole `Sources` subtree (`RemoveSubtree`, backed by
`UserConfigFile.RemoveValues`), so a list saved again with fewer files leaves no stale numbers.

| Key | Values | Default |
|---|---|---|
| `Sources.<n>.FilePath` | full path of a custom alias file on this PC | none |
| `Sources.<n>.Url` | web address of a custom alias file | none |
| `Sources.<n>.CredentialId` | id of a saved credential (Settings ▸ Credentials), `"N"` GUID format - never the secret | none |

In a settings export, `FilePath` is a machine path (`UserConfigPortability`: any key ending in
`FilePath`, like one ending in `Folder` or `Directory`): it is tokenized like a folder, and an
import takes it only if the file exists on the importing PC (`UserConfigPortability.IsFile`); the
import summary calls it "Custom alias file <n>". `Url` and `CredentialId` are shared as they
are - a credential id matches nothing on another PC, so the user picks one of their own.

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
