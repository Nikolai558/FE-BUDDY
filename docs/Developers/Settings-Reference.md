# Settings reference

FE-Buddy's settings live in two places:

- **The settings block** - the `Dictionary<string, string>` one sub-service or file conversion gets
  for one run. The app builds it (each tab's `BuildSettingsBlock`), the harness writes it by hand
  (`FeBuddy.Harness/HarnessSettings.cs`) and the tests build it inline. All of them go through the
  same parser.
- **The settings profile** (`%APPDATA%\FE-Buddy\User Configurations\UserConfig.<Profile>.json`) -
  what the app remembers. Each tab saves its own node, mostly under the same key names its block
  uses.

This page lists both. Where a saved key differs from the block key, the table's **Saved as** column
says so; a blank there means the same name.

**Contents:** [How values are read](#how-values-are-read) · [The settings files](#the-settings-files) ·
[Settings profiles](#settings-profiles) · [Saved settings outside the tabs](#saved-settings-outside-the-tabs) ·
[Keys every AIRAC sub-service shares](#keys-every-airac-sub-service-shares) · [File keys](#file-keys) ·
[New file names](#new-file-names) · [CRC defaults](#crc-defaults) · [Airports](#airports) ·
[Airways](#airways) · [Departures and Arrivals](#departures-and-arrivals) · [NAVAIDs](#navaids) ·
[ARTCC Boundaries](#artcc-boundaries) · [Fixes](#fixes) · [Wx Stations](#wx-stations) ·
[Procedure Charts](#procedure-charts) · [Telephony](#telephony) · [Add Custom Aliases](#add-custom-aliases) ·
[File conversions](#file-conversions) · [Settings export and import](#settings-export-and-import) ·
[Adding a setting](#adding-a-setting) · [Changing the layout](#changing-the-layout)

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

## The settings files

Each profile is one nested JSON tree. Code reads a value by its dotted path (`General.UpdateChannel`)
through `UserConfigFile`; keys read in more than one place are constants in `UserConfigKeys`. Before
a tab's node is saved, its old state is copied to `UserConfig-previous.<Profile>.json`, which **Undo
last save** restores. Settings' own **Save** writes the whole profile, with no undo copy.

Each file starts with `"ConfigVersion"`, its layout (`UserConfigVersion`), which every write stamps
and which is never read as a setting. A file with no stamp, or one older than
`UserConfigVersion.Oldest`, is read as the oldest layout. At launch, every profile and `Shared.json`
in an older layout are brought forward together (`UserConfigMigrations`) and written back, each old
file kept as `UserConfig-v<old layout>.<Profile>.json` or `Shared-v<old layout>.json`. A file a newer
FE-Buddy saved is set aside instead. See [Changing the layout](#changing-the-layout).

## Settings profiles

Everything is in `%APPDATA%\FE-Buddy\User Configurations`:

| File | Holds |
|---|---|
| `UserConfig.<Profile>.json` | One profile: every setting but the shared ones. The name follows Windows' rules for a file name, up to 64 characters; names differing only in case are the same profile. |
| `Shared.json` | `ActiveProfile` (the profile in use), and the settings every profile shares: `UserConfigPortability.SharedByProfiles` (the update channel, `NewsLastOpen`, `FeBuddyGitHub.CredentialId`, `LegacyGitHubTokenNoticeShown`, `WelcomeShown`, `RememberWindowPlacement`, `MainWindowPlacement`). |
| `UserConfig-<kind>.<Profile>.json` | A profile's backups: `previous` (undo), `before-import`, `v<N>` (brought forward), `damaged` (the profile as it was when it couldn't be read as settings, kept before a save wrote over it). They move and go with it. |
| `Shared-v<N>.json` | `Shared.json` as it was in layout N, kept when it was brought forward. |

Settings a newer FE-Buddy saved that this one can't read go to `%APPDATA%\FE-Buddy\Reverted`
([Going back](#going-back-to-an-older-release)).

- In memory the two files are one dictionary, so a caller never knows which a key is in. `Save`
  writes a shared key under its node to `Shared.json`, and every other key to the profile.
  `SaveShared` writes shared keys to `Shared.json` alone, and `ReadSharedValue` reads one straight
  from it: the main window's placement uses both, as the window opens before the launch reads the
  settings.
- `CreateProfile`, `SwitchProfile`, `RenameProfile` and `DeleteProfile` are in
  `UserConfigFile.Profiles.cs`. The profile in use can't be deleted; Settings switches away first.
- After a switch or an import every page reads the settings again (`ConfigPages.ReloadEverything`).
- A dev launch's choice of launch steps is not a setting: it's in `%APPDATA%\FE-Buddy\DevLaunch.json`,
  beside `User Configurations`, so no profile, import or export ever has it (see
  [Dev launch](README.md#dev-launch)).

## Saved settings outside the tabs

### General

Written by Settings, except `NewsLastOpen`, `LegacyGitHubTokenNoticeShown`, `WelcomeShown`,
`MainWindowPlacement` (as the main window closes), and the first `UpdateChannel`.

| Key | Values | Default |
|---|---|---|
| `UpdateChannel` | `Stable`, `ReleaseCandidate`, `Beta`, `Alpha`. Saved by the first launch (not a `-dev` build's), then changed only in Settings | the running build's channel; `Stable` for a `-dev` build (`UpdateChannelSetting.DefaultFor`) |
| `NewsLastOpen` | the newest News `PostId` the user has seen, e.g. `2026-08-30.3` | none |
| `PrettyPrintGeojson` | `Y` / `N` (`OutputFormatting`) | `N` |
| `DefaultOutputDirectory` | a folder | the Desktop |
| `AddFeBuddyOutputFolder` | `Y` / `N` - put the output in a `FE-Buddy_Output` folder | `Y` |
| `FeBuddyGitHub.CredentialId` | the id (`"N"` GUID) of the saved GitHub token credential FE-Buddy's own GitHub requests use (`GitHubAuth`); blank for none | none |
| `LegacyGitHubTokenNoticeShown` | `Y` once the notice about 2.x's `FEBUDDY_GITHUB_TOKEN` has been shown | none |
| `WelcomeShown` | `Y` once the first-launch welcome to FE-Buddy 3 has been shown (`WelcomeNotice`) | none |
| `RememberWindowPlacement` | `Y` / `N` - the main window reopens where it was closed. Saved at once by Settings ▸ Main Window; `N` also removes `MainWindowPlacement` | `Y` |
| `MainWindowPlacement` | `left,top,width,height,dpi,state` in physical screen pixels, e.g. `2120,140,1800,1170,144,Maximized` (`SavedWindowPlacement`). Checked against the screens connected at each launch (`WindowPlacementRules`); anything unreadable means the default | none: 1200 x 780, centered on the main screen |
| `Facility` | the Settings ▸ Facility ARTCC, e.g. `ZOB` (`UserConfigKeys.Facility`). The run's `PrimaryFacility` (first in `Duplicate_Alias_Commands.txt`, and Procedure Charts' leading section), and the ARTCC ticked on Departures, Arrivals, ARTCC Boundaries and Procedure Charts until each is first saved | none |
| `CoordinatePrecision` | `1`-`15` decimal places, or `0` for Do not round (the app offers 5, 6, 7 and Do not round) | `6` |
| `DefaultRoi.FilterByRoi` | `Y` / `N` | `N` |
| `DefaultRoi.Corners.SwLat`, `.SwLon`, `.NeLat`, `.NeLon` | decimal degrees, as a tab's own ROI's [corners](#keys-every-airac-sub-service-shares) (across the AM too) | none |

The default ROI is saved by `DefaultRoiStore`, under its own `General.DefaultRoi` node: by the Map as
soon as it is set or cleared, by Settings when **Save** is pressed. Clearing it only sets `FilterByRoi`
to `N`; the corners stay in the file but aren't read until a new ROI replaces them.

No settings-layout step brings these keys in from `Services.AiracService` (`UserArtccId`,
`CoordinatePrecision`, `DefaultRoi.*`): a file that has them there opens Settings with no facility,
6 decimal places and no default ROI.

### Services.AiracService.General

The AIRAC Service's General tab. Its node sits beside the other tabs' nodes, so its save and
**Undo last save** touch only these keys.

| Key | Values | Default |
|---|---|---|
| `CyclePosition` | `Current`, `Next` or `Previous`: the cycle last saved. A saved `Next` reopens on Next unless the FAA hasn't published it; anything else opens on Current | `Current` |
| `SelectedSubServices` | the sub-services included on the General tab, comma-separated: `Airports`, `Airways`, `Departures`, `Arrivals`, `Navaids`, `ArtccBoundaries`, `Fixes`, `WxStations`, `Procedures`, `Telephony`. Add Custom Aliases isn't in the table: its tab comes in by itself | every sub-service |
| `Outputs.<sub-service>.<output>` | `Y` / `N`: the General tab's table, e.g. `Outputs.Airports.Geojson`. `<output>` is `Alias`, `Geojson`, `ProcedureChanges` or `ProceduresJson`, and only those the sub-service makes are saved | `Y` |
| `ExportIndividualAliasFiles` | `Y` / `N`: also save each sub-service's own alias file (`Airports.txt`, `Faa_Chart_Recall.txt`, …) in `Aliases\`. `N` puts their commands only in `Combined_Alias.txt` ([Where files go](#where-files-go)). Read as saved by the run (`AiracServiceSettings.ExportIndividualAliasFiles`) | `N` |

### Services.AiracService.DuplicateAliases

Saved by the Duplicate Alias Commands card on Preview Settings as soon as either changes, and the
choices also after a run in which the user made some (`DuplicateAliasesCardViewModel`).

| Key | Values | Default |
|---|---|---|
| `Review` | `Y` - a duplicate no saved choice settles stops the run for the user to choose; `N` - it is listed in `Duplicate_Alias_Commands.txt` | `N` |
| `Choices` | one choice per line, `FileKey\|Command\|Occurrence\|Action\|NewCommand`: the alias file's [key](#file-keys), the duplicated command, which of that file's lines with it (from `1`), `Keep` / `Ignore` / `Rename`, and the new command for `Rename` (`DuplicateAliasChoices`). A line that isn't a whole choice is skipped | none |

Every run makes the saved choices, whichever way `Review` is set.

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

Saved by the Map as each choice is made, with no Save button: `MapLayersState` saves the files,
live layers and home view, and its `BaseMapSettings` saves the base map and gridlines (the opacity
once the slider has been still for 0.4 s).

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
| Procedure Charts | `Services.AiracService.Procedures` |
| Telephony | `Services.AiracService.Telephony` |
| Add Custom Aliases | `Services.AiracService.ConcatenateAliases` |
| DAT to GeoJSON | `Services.FileConversions.DatToGeojson` |
| SCT2 to GeoJSON | `Services.FileConversions.SctToGeojson` |
| ERAM to GeoJSON | `Services.FileConversions.EramToGeojson` |

## Keys every AIRAC sub-service shares

Read by `SubServiceSettingsReader`; saved by `GeojsonSubServiceViewModel`. Of these, Procedure
Charts reads only `OutputDirectory`, `Area` and the ROI keys, Telephony only `OutputDirectory`, and
Add Custom Aliases none. The app still sends the rest to Procedure Charts and Telephony, and both
accept and ignore them.

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `OutputDirectory` | not saved | the folder the run writes into (below) | **required** |
| `CoordinatePrecision` | `General.CoordinatePrecision` | `1`-`15`, or `0` not to round (`GeojsonFileWriter.NoRounding`) | `6` |
| `IncludeFebCustomProperties` | | `Y` / `N` | `N` |
| `FebProperties` | | list of the sub-service's `feb.*` names; **required** when the above is `Y` | none |
| `CrcDefaultsFor` | `CrcDefaultsScope` (`None`, `AllGeojsonFiles`, `SpecificFiles`) and `CrcDefaultsFiles` | list of the sub-service's GeoJSON [file keys](#file-keys) that get CRC-ERAM defaults | none |
| `Area` | `Area` | which one area filter the run uses (`SubServiceArea`): `Artccs` (ARTCC Boundaries, Departures, Arrivals; Procedure Charts' facilities), `Roi`, `Everything`, or Procedure Charts' `None`. Only that filter is read | see below |
| `FilterByRoi` | not saved: sent `Y` while `Area = Roi` and an ROI is set (the tab saves its own-or-default choice as `Roi.OverrideDefaultRoi`) | `Y` / `N`; **required** `Y` with `Area = Roi` | `N` |
| `RoiSwLat`, `RoiSwLon`, `RoiNeLat`, `RoiNeLon` | `Roi.OverrideCorners.SwLat`, `.SwLon`, `.NeLat`, `.NeLon` | decimal degrees: latitudes `-90`-`90`, longitudes `-360`-`360`, south below north, west below east, less than 360° wide (`RoiFilter`); **required** with `Area = Roi` (which also needs `FilterByRoi = Y`) | none |
| `Crc.<Class>.<Kind>.<field>` | `CrcEramPropertyDefaults.<row>.<field>` | see [CRC defaults](#crc-defaults) | none |

- The app sends `Area`, and only the filter it names: the tab's ARTCCs with `Artccs`; with `Roi`,
  `FilterByRoi = Y` and the tab's own corners, or the default ROI's. The ARTCCs and the ROI are saved
  whichever area is picked.
- An ROI that crosses the antimeridian (AM), the 180° meridian, has one longitude past ±180, either
  way round: `RoiSwLon = -206` and `RoiNeLon = -124`, or `154` and `236`. A Southwest Longitude greater
  than the Northeast one (`154` and `-124`) is refused. The app asks whether such typed corners cross
  the AM, and saves a yes as drawing the box on the Map does (`-206` and `-124`). Output coordinates
  stay within -180..180, and the lines an ROI across the AM clips are split there, whatever
  `SplitAtAntimeridian` says.
- A tab that has never saved an area starts on what its other settings mean
  (`UserConfigAreas.DefaultFor`): its ARTCCs (or, never saved, the Settings ▸ Facility ARTCC) if it has
  any, otherwise the ROI if it has its own or the default ROI is on, otherwise `Everything`
  (Procedure Charts: `None`).
- A block without `Area` is refused (`SubServiceSettingsReader.ReadArea`); Procedure Charts' is the
  exception (see [Procedure Charts](#procedure-charts)).
- `CrcDefaultsFiles` keeps every choice, even for files the current settings don't write, so a file
  switched off and on keeps its choice. A run sends only the files written.
- A sub-service's outputs (`GenerateGeojson`, `GenerateAliasFile`, and Procedure Charts' two documents) are
  set on the General tab and saved there, under `Services.AiracService.General.Outputs` - never in the
  sub-service's own node, so saving or undoing its tab can't change them.
- Where a sub-service has both, `GenerateGeojson` and `GenerateAliasFile` can't both be `N`, and
  `GenerateGeojson = Y` needs at least one `Emit…`.

### Where files go

`AiracService` sets `OutputDirectory` on every sub-service's block (not Add Custom Aliases') to the run's cycle folder,
`<output>[\FE-Buddy_Output]\AIRAC_<cycle>`; the harness and tests pass their own. Inside it,
`AiracOutputPaths` lays the files out:

| File | Goes in |
|---|---|
| Alias file | `Aliases\`, while the General tab's `ExportIndividualAliasFiles` is on. Off (the default), it is written to the run's folder in FE-Buddy's scratch area, under FE-Buddy's name, and is only a section of `Combined_Alias.txt`; any an earlier run left in `Aliases\` is deleted |
| `Combined_Alias.txt` | `Aliases\`, whenever the run wrote an alias file or [Add Custom Aliases](#add-custom-aliases) is in it, unless the user stops the run at the duplicate review |
| GeoJSON | `Geojson\` (Departures and Arrivals: `Geojson\<ARTCC>\<airport>\`) |
| Procedure Charts' documents | `Publication_Docs\` |
| `Duplicate_Alias_Commands.txt` | the cycle folder itself, whenever the run wrote an alias file |

## File keys

A **file key** names one output file: a GeoJSON file's name without `.geojson`, or the alias file's
name. `CrcDefaultsFor` and the File Names tab use them, matching case-insensitively. A
`CrcDefaultsFor` key that isn't one of the sub-service's GeoJSON keys throws. A key for a file that
isn't written this run is ignored by most sub-services, but ARTCC Boundaries, Fixes' and NAVAIDs'
per-group layouts still require the `Crc.*` values of every listed key that isn't excluded, and
Airways reads them whenever the kind is written: with `Designation`, any listed key of a kind needs
all three classes of that kind; with `HighLow`, each listed High or Low key needs its class, whether
or not any designation goes there. Each sub-service's keys are in its `*OutputFiles` class.

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
| Procedure Charts | none | `Faa_Chart_Recall.txt` |
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
- A sub-service's own alias file keeps FE-Buddy's name while `ExportIndividualAliasFiles` is off: it
  isn't exported, so its new name is kept but not used (`OutputFileNames.WithUnexportedAliasFilesIn`).
  A new name for `Combined_Alias.txt` is always used.

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
  (but see [File keys](#file-keys) for the sub-services that read every listed key; for a
  conversion, a kind it writes whose `IncludeCrc…Defaults` is `Y`, and for ERAM only with
  `DefaultsSource` `XmlThenCard` or `Card`). Other valid CRC keys are
  ignored; a key for a class, kind or field the sub-service doesn't have is a warning.
- Values are checked against what CRC can draw; a bad value throws, naming the key.
- ARTCC Boundaries' per-ARTCC classes, and Fixes' chart, combination and unknown fix-use classes,
  come from the cycle's own data (`AddCrcRows`); Fixes' known fix uses are a fixed list.

## Airports

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `GenerateGeojson` | `Outputs.Airports.Geojson` (General tab) | `Y` / `N` | `Y` |
| `GenerateAliasFile` | `Outputs.Airports.Alias` (General tab) | `Y` / `N` | `Y` |
| `EmitRunwayLines`, `EmitAirportSymbols`, `EmitAirportText` | | `Y` / `N` | `Y` |

- **`FebProperties`:** `faaId`, `icaoId`, `name`, `elev`, `respArtcc`, `tfcPtrnAlt`, `fssId`, `twrType`,
  `rwyId`.

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
| `SplitAtAntimeridian` | | `Y` / `N`; an ROI that crosses the AM splits the lines it clips either way | `Y` |
| `ExcludedDesignations` | | list of designations (an airway ID's leading letters), e.g. `A,G` | none |
| `HighDesignations`, `LowDesignations`, `BothDesignations` | | lists: the designations in the High file, the Low file, or both | `J,Q` High, `V,T` Low |

- **`FebProperties`:** `awyId`, `pointId`, `waypoints`.
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
| `ArtccFilter` | | list of ARTCC IDs; read only with `Area = Artccs`, which needs one | saved: the Settings ▸ Facility ARTCC, until first saved |
| `AmendmentFilter` | `Amendment.Filter` | `None`, `Cycles`, `Days`, `Date` | `None` |
| `AmendedWithinCycles` | `Amendment.WithinCycles` | 1-1000 (1 = the selected cycle); **required** with `Cycles` | saved: `1` |
| `AmendedWithinDays` | `Amendment.WithinDays` | 1-36500, back from today; **required** with `Days` | saved: `30` |
| `AmendedOnOrAfter` | `Amendment.OnOrAfter` | `yyyy-MM-dd`; **required** with `Date` | none |
| `RoiMode` | `Roi.Mode` | `Airport` (every procedure of an airport in the ROI), `Waypoint` (any procedure with a point in it); used with `Area = Roi` | `Airport` |

- **`FebProperties`:** Departures `dpName`, Arrivals `arrivalName`; both `pointId`, `arptId`, `artcc`,
  `amendmentNo`, `amendEffDate`, `waypoints`.
- Only the chosen amendment mode's value is read.

## NAVAIDs

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `GenerateGeojson` | `Outputs.Navaids.Geojson` (General tab) | `Y` / `N` | `Y` |
| `GenerateAliasFile` | `Outputs.Navaids.Alias` (General tab) | `Y` / `N` | `Y` |
| `EmitSymbols`, `EmitText` | | `Y` / `N` (there is no Lines file) | `Y` |
| `OutputBy` | | `All`, `Type` | `All` |
| `ExcludedTypes` | | list of NASR `NAV_TYPE` names, left out of the GeoJSON (the alias file always has every type) | none |
| `SymbolStyleBy` | | `Type`, `File` - read only with `OutputBy = All` | `Type` |
| `FanMarkerStyle` | | a CRC symbol style for fan markers, when the merged Symbols file gets CRC defaults styled by type | none |

- **`FebProperties`:** `navId`, `navType`, `name`, `freq`, `lowAltArtccId`, `highAltArtccId`. The Text
  file never carries `navId`, `navType` or `name`; its label already shows them.
- An unknown name in `ExcludedTypes` warns but is still excluded, so a type NASR adds can be
  unticked. Excluding every known type throws while `GenerateGeojson` is `Y`.
- A fan marker with no `FanMarkerStyle` gets no style, with a warning; an invalid style throws. The
  app requires one whenever the merged Symbols file gets CRC defaults styled by type and fan markers
  are included.
- With `OutputBy = All` and `SymbolStyleBy = Type`, the merged Symbols file's defaults carry no
  style: `Crc.NAVAIDs.Symbol.style` isn't read (or sent), and each NAVAID is styled by its type
  (`FanMarkerStyle` for fan markers).

## ARTCC Boundaries

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `OutputBy` | | `HighLow`, `HighLowUnlimited`, `ArtccAltitude` | `HighLow` |
| `LocationFilter` | | list of ARTCC IDs; read only with `Area = Artccs`, which needs one | saved: the Settings ▸ Facility ARTCC, until first saved |
| `SplitAtAntimeridian` | | `Y` / `N`; an ROI that crosses the AM splits the lines it clips either way | `Y` |

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
  known fix use throws with `OutputBy = FixUse`. A `Combinations` entry that isn't
  `<chart>+<fix use>` throws, whatever the layout.

## Wx Stations

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `EmitSymbols`, `EmitText` | | `Y` / `N`, not both `N` | `Y` |

No other keys of its own, and no `FebProperties`: `IncludeFebCustomProperties = Y` only warns.

## Procedure Charts

Procedure Charts writes no GeoJSON, so it has no `Emit…`, `CoordinatePrecision`, `FebProperties`, `CrcDefaultsFor` or `Crc.*` keys
(`IncludeFebCustomProperties = Y` warns, pointing at `JsonFields`). Its `Area` is `Artccs` (the
facilities), `Roi` (every airport inside it), `Everything` (every airport in the metafile) or `None`.

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `GenerateChangesDocument` | `Outputs.Procedures.ProcedureChanges` (General tab) | `Y` / `N` - `Procedure_Changes.md` | `Y` |
| `GenerateProceduresJson` | `Outputs.Procedures.ProceduresJson` (General tab) | `Y` / `N` - `Procedures.json` | `Y` |
| `GenerateAliasFile` | `Outputs.Procedures.Alias` (General tab) | `Y` / `N` - `Faa_Chart_Recall.txt` | `Y` |
| `IncludeHelpCommand` | | `Y` / `N` - the help command as `Faa_Chart_Recall.txt`'s first line | `Y` |
| `HelpCommandName` | | the help command's name, letters and numbers only; a dot in front is dropped. Sent only while `IncludeHelpCommand` and `RenameHelpCommand` are `Y` | `helpCHARTRECALL` |
| - | `RenameHelpCommand` | `Y` / `N` - use `HelpCommandName` rather than the default name; saved, not sent | `N` |
| `Facilities` | | list of ARTCC IDs whose airports are included; read only with `Area = Artccs`, which needs one | saved: the Settings ▸ Facility ARTCC, until first saved |
| `PrimaryFacility` | not saved (`General.Facility`) | the ARTCC whose section leads both documents | none |
| `Airports` | | list of FAA or ICAO IDs (saved as the FAA ID) | none |
| `Procedures` | | list of departure and arrival root names, included wherever charted, e.g. `BRWNZ` | none |
| `AirportProcedures` | | list of `<airport>\|<departure or arrival root name>`, e.g. `PIT\|GRUUB` | none |
| `IncludeProceduresWithFixInFacility` | | `Y` / `N` - also include, at any airport, the departures and arrivals whose route has a fix in `PrimaryFacility`. The tab sends `N` while no facility is set | `Y` |
| `ChartTypes` | | d-TPP chart codes a whole airport contributes: `IAP`, `STR`, `DP`, `ODP`, `DAU`, `APD`, `MIN`, `HOT`, `LAH`; blank for none | `IAP,STR,DP,ODP,DAU,APD` |
| `JsonFields` | | optional `Procedures.json` fields (`ProcedureJsonField`); blank for none of them, so only the fields always written | `icaoId,airportName,responsibleArtcc,airspaceClass,chartType,chartUrl,change,compareUrl` |

- The three `Generate…` keys can't all be `N`.
- `Procedures` and `AirportProcedures` take departures and arrivals only (d-TPP `DP`, `ODP`, `STR`),
  matched by root name (`ProcedureNaming.RootName`: the name without its trailing qualifiers, then
  its number), so `BRWNZ` matches `BRWNZ FOUR (RNAV)` and `BRWNZ FIVE`. A full name in a block
  matches the same way (`BRWNZ FOUR (RNAV)` is `BRWNZ`); the tab saves root names. A name of any
  other chart (an approach) is left out with a warning.
- `Airports`, `Procedures` and `AirportProcedures` add to the area, even outside it; with
  `Everything` they aren't read. With `None`, at least one of them is required. The area's own
  checks (a facility, a ROI) and that one apply only when a document is on. The alias file ignores
  them all: it covers every chart at every airport in the d-TPP Metafile.
- `IncludeProceduresWithFixInFacility` adds to the area like the lists: not read with `Everything`,
  and with `None` it counts as something to include when there is a `PrimaryFacility`. On without
  one, the run warns and skips it. A chart brought in only by it is labeled in both documents,
  except at an airport whose responsible ARTCC is `PrimaryFacility`. How a procedure qualifies, and
  which route counts, is in the XML docs of `RoutePointArtccResolver`, `ProceduresWithFixInFacility`,
  `NasrProcedureRoutes` and `ProcedureSelection`. The previous cycle's routes are used for deleted
  and changed procedures, when that cycle's NASR data is loaded (`AiracSupplementalData.PreviousNasr`).
- A block without `Area` takes `Facilities` as the area when it lists any, otherwise `None`.
- An unknown `ChartTypes` code warns but is still used; an unknown `JsonFields` name warns and is
  ignored.
- `JsonFields`: `icaoId`, `airportName`, `city`, `state`, `responsibleArtcc`, `airspaceClass`,
  `military`, `chartType`, `chartUrl`, `change`, `compareUrl`, `amendment`, `amendmentDate`,
  `procedureUid`, `computerCode`, `producer`. An airport's `airportId` and a procedure's `name` are
  always written; a field with no value is left out. `chartUrl` also writes `continuationUrls`, the
  other pages' charts. `compareUrl` is the first page's, only when that page changed, and it also
  writes `continuationCompareUrls`, the changed continuation pages' (the FAA publishes a compare
  PDF only for a page it marks changed).
- The chart recall rules are in the XML docs of `ChartRecallCodes`, `ApproachCodes`, `SidStarCodes`,
  `ChartRecallSkipReason` and `ChartRecallAliasBuilder`.
- The help command (`ChartRecallHelpCommand`) is a `.note` line written before the chart commands,
  and isn't counted as one. `IncludeHelpCommand` and `HelpCommandName` are read only with
  `GenerateAliasFile = Y`; a blank name is the default, and anything but letters and numbers is an
  error (`ChartRecallHelpCommand.IsValidName`).
- Info ▸ Alias Command Guide, its exports and the practice page show the help command by its saved
  name (`ProceduresViewModel.SavedHelpCommand`; a blank or invalid name is the default). With
  `Outputs.Procedures.Alias` or `IncludeHelpCommand` at `N`, they show the default name and say a
  facility may have renamed it or left it out.

## Telephony

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `GenerateAliasFile` | `Outputs.Telephony.Alias` (General tab; unticking it leaves Telephony out of the run) | must be `Y`: `Telephony.txt` is Telephony's only output | `Y` |
| `VirtualAirlines.<n>.Designator` | | a virtual airline's three-letter designator (`<n>` from 1) | none |
| `VirtualAirlines.<n>.Telephony` | | its telephony | none |
| `VirtualAirlines.<n>.Organization` | | its virtual organization | none |
| `IncludeVatsimRadarVirtualAirlines` | | `Y` / `N` - also write the virtual airline list (GNG + VATSIM-Radar) | `N` |

- No `CoordinatePrecision`, `FebProperties`, `CrcDefaultsFor` or `Crc.*`, and no area: the app saves and sends no `Area`
  or ROI keys (any sent are accepted and ignored), and `IncludeFebCustomProperties = Y` warns.
- Virtual airlines are read in number order. Each needs a three-letter `Designator`, a `Telephony`
  with a letter or digit, and an `Organization` (`TelephonySettingsParser.VirtualAirlineProblem`, which
  the tab uses too); otherwise the run throws, naming the number. A number with no fields is
  skipped; a repeat of an earlier one (ignoring case) is written once, with an Info message.
- The tab saves the list whole, renumbered from 1, so a deleted entry leaves no keys behind.
- The AIRAC Service checks `IncludeVatsimRadarVirtualAirlines` before the run
  (`TelephonySettingsParser.IncludesVatsimRadarList`) to know whether to download the list.

## Add Custom Aliases

Whether to add the user's own alias files to `Aliases\Combined_Alias.txt`, then numbered keys, one
group per custom alias file (`<n>` from 1), added in number order. Saved the same way; every save
removes the `Sources` subtree first. The block is only in a run while an included sub-service makes
an alias file. Every run that writes an alias file writes `Combined_Alias.txt`, with the block or
without it.

| Block key | Saved as | Values | Default |
|---|---|---|---|
| `AddCustomAliasFiles` | `CombineAliasFiles` | `Y` / `N` | `Y` |
| `Sources.<n>.FilePath` | | full path of an alias file on this PC | none |
| `Sources.<n>.Url` | | `http://` or `https://` address, with no sign-in written into it | none |
| `Sources.<n>.CredentialId` | | a saved credential's id (`"N"` GUID format), never the secret | none |

- It reads none of the shared keys; `OutputDirectory` is accepted and ignored. Any other key is a
  warning.
- Each `<n>` needs exactly one of `FilePath` and `Url`. These throw: both; neither, when the group has
  a `CredentialId` (an empty group is skipped); a relative `FilePath`; a `Url` that isn't http(s) or
  has a sign-in in it (`UrlSecrets` - the message never repeats the address); a `Url` group's
  `CredentialId` that isn't a GUID. A `CredentialId` on a `FilePath` is ignored, with an Info message.
- With `AddCustomAliasFiles = N` the `Sources` keys are not read or checked, and the app doesn't
  send them.
- No sources is allowed: `Combined_Alias.txt` then holds FE-Buddy's aliases only, with an advisory
  warning. The tab is invalid only while adding custom files and a listed file has a problem.
- A GitHub token that downloads a custom file and expires within 57 days adds an advisory warning
  (`GitHubTokenExpiry`).

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

Saved under the same names, as is `CroppingDistance`.

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

Settings ▸ **Export…** writes the profile in use to a file another user can bring in with
**Import…** (`UserConfigTransfer`): the same tree as a profile, under a header.

```json
{ "format": "FE-Buddy.UserConfig", "formatVersion": 1, "configVersion": 2, "appVersion": "3.0.0",
  "exportedUtc": "2026-09-27T12:00:00Z", "settings": { "General": { ... }, "Services": { ... } } }
```

A plain settings file with no header (a profile's `UserConfig.<Profile>.json`) imports too. `formatVersion` is the header's; `configVersion` is the
layout of the settings inside (none, or one older than `UserConfigVersion.Oldest`, means the oldest
layout). A file in an older layout is brought forward before the import is planned. A file with a newer `formatVersion` or layout, or over 2 MB, is refused.

**Each key travels according to its name** (`UserConfigPortability.Classify`), so a new setting needs
no list:

| Scope | Keys | Export | Import |
|---|---|---|---|
| Secret | under `Secrets`, or ending in `Token`, `Password`, `Secret`, `ApiKey`, `Credential` or `Credentials`, or called `Pat` | never | this PC's is kept |
| Local | `General.UpdateChannel`, `NewsLastOpen`, `FeBuddyGitHub.CredentialId`, `LegacyGitHubTokenNoticeShown`, `WelcomeShown`, `RememberWindowPlacement`, `MainWindowPlacement`, and anything outside `General` and `Services` | never | this PC's is kept |
| Credential choice | ending in `CredentialId` | never | this PC's is kept only where the settings beside it are unchanged (the same custom alias file at the same address) |
| Machine path | ending in `Folder`, `Directory` or `FilePath` (not `AddFeBuddyOutputFolder`) | your Desktop, Documents or profile become `%DESKTOP%`, `%DOCUMENTS%`, `%USERPROFILE%` | taken only where it works on this PC (below) |
| Shared | everything else under `General` and `Services` | as it is | as it is |

- A machine path is taken when it is an output folder (a name with `Output` in it) on a drive this PC
  has, a folder FE-Buddy reads from that exists, or a file that exists. A network path is refused
  unless it is this user's own Desktop, Documents or profile. Otherwise this PC keeps its own; a
  custom alias file is left out instead, since this PC's entry at that number is a different file.
- **The user chooses where the settings go** (`ImportDestination`; Replace and Merge are
  `UserConfigImportMode`):
  - **A new profile** (`ApplyAsNewProfile`) gets the file's settings, and FE-Buddy switches to it.
  - **Replace** makes the profile in use match the file: anything the file leaves out goes back to
    its default, except the Local, Secret and credential-choice keys above.
  - **Merge** keeps the profile's settings the file leaves out, folders included. A numbered list
    the file has (`...Sources.<n>...`) replaces the profile's whole list, and a profile key that
    would nest under one of the file's, or the other way round, gives way.
- `UserConfigFile.ReplaceAll` writes the new profile and swaps it in with `File.Replace`, keeping the
  old one as `UserConfig-before-import.<Profile>.json`. It deletes `UserConfig-previous.<Profile>.json`
  (its undo would restore pre-import settings), and every open page reloads
  (`ConfigPages.ReloadEverything`). `Shared.json` is never touched.

## Adding a setting

1. **Pick its node:** `General` for app-wide preferences, the tab's own node otherwise. A page or tab
   writes only its own node ([Code Structure](Code-Structure.md#rules-for-core-and-the-app)).
2. **Save and load it** in the tab's `LoadFromConfig` / `WriteToConfig` with `Get` / `Set` (relative
   to the tab's node; a file conversion tab: `LoadOwnSettings` / `SaveOwnSettings`). If more than one
   class reads it, add a constant to `UserConfigKeys`.
3. **If Core needs it:** send it from `BuildSettingsBlock` (or
   `GeojsonSubServiceViewModel.AddSharedSettings`; a file conversion tab: `AddOwnSettings`), read it
   in the parser with `SettingsValueReader`, and add it to the parser's `OwnKeys` (or
   `SubServiceSettingsReader.CommonKeys`, or `ConversionSettingsReader.ConversionKeys`), or it is
   reported as unknown. SCT2 to GeoJSON has no `OwnKeys` of its own. Add Custom Aliases, and the
   fields of a numbered `Sources.<n>` or `VirtualAirlines.<n>` entry, are checked by their parser's
   own field lists. Add it to `HarnessSettings.cs` where the harness has a block for that
   sub-service, and to a parser test.
4. **Mind its name** - it decides how it travels in an export. A name ending in `Folder`,
   `Directory` or `FilePath` is a machine path, `CredentialId` a credential choice, `Token`,
   `Password` and the like a secret. A yes/no setting ending in `Folder` goes in
   `UserConfigPortability`'s `NotFolderKeys`. A this-PC-only setting under `General` or `Services`
   goes in its `LocalKeys` as a `UserConfigKeys` constant. That also makes every profile share it,
   since it is saved in `Shared.json` rather than in the profile (`SharedByProfiles`).
5. **List it on this page.**

## Changing the layout

Testers keep their settings from one version to the next, so a saved setting is never just renamed.

- **Every rename, move, re-format or removal of a saved setting is a new layout with its own step**,
  in a pre-release too.
- **A layout number is never used twice**, not even one that never shipped.
- **`UserConfigVersion.Oldest` never goes up**: a user who skipped releases would lose their
  settings. Keep the old steps instead. `UserConfigVersionTests` checks it.

Moving the settings file while every setting keeps its dotted path is not a layout change: it needs
only step 3.

1. **Bump `UserConfigVersion.Current`**, and add the layout to `UserConfigVersion.Layouts` and to the
   table below, with the first release that saves it. `UserConfigVersionTests` checks that the two
   match and that this version reads the current layout.
2. **Add the step** that turns the old layout into the new one to `UserConfigMigrations.All`:
   ```csharp
   new UserConfigMigration(3, "Airways' buffer distances moved under Buffer", settings =>
   {
       settings.Move("Services.AiracService.Airways.FixBufferNm", "Services.AiracService.Airways.Buffer.FixNm");
       settings.ChangeValue("Services.AiracService.Airways.Buffer.Enabled", value => value == "true" ? "Y" : value == "false" ? "N" : value);
   })
   ```
   A step edits the settings by dotted path (`Move`, `ChangeValue`, `Set`, `Remove`). It may meet a
   file that already has some of the new layout: `Move`, `Set` and `Remove` can run twice without
   harm, and a `ChangeValue` must leave an already-changed value alone.

   Every file is brought forward together, one layout at a time
   (`UserConfigMigrations.MigrateTogether`). A step runs on each profile still behind it, the one in
   use first, then on `Shared.json`, then its `ThenShared` part on `Shared.json` alone. So a step that
   renames a shared setting renames it there too, and a step can move a setting between the files:
   - `MoveToShared(from, to)` moves a profile's setting into `Shared.json`. The profile in use runs
     first, so its value is the one kept.
   - `CopyFromShared(from, to)` gives each profile a copy of a shared setting, keeping one it already
     has. `ThenShared = shared => shared.Remove(...)` then takes it out of `Shared.json`.

   A profile behind `Shared.json` (one copied in by hand, say) sees `Shared.json` as it is now: a
   setting already handed out and removed gets its default.
3. **For a file that moves**, have `UserConfigFile` look for it where the old layout kept it too.
4. **Add a sample** of the new layout: `FeBuddy.UnitTests\Fixtures\UserConfig\UserConfig.v<N>.json`,
   a full file as the new version saves it, with its fingerprint in `UserConfigLayoutTests`.

`UserConfigLayoutTests` brings every sample forward and has every settings page read it. It fails if
a setting in any layout's sample is no longer saved with the same value. It also fails if a
released sample changes. A setting saved outside the tabs (Settings, the default ROI, the map) is
checked by its key constant, listed in the test's `OtherSettings`.

The layouts so far:

| Layout | First saved by | Change |
|---|---|---|
| 2 | 3.0.0-beta.6 | The oldest FE-Buddy reads (`UserConfigVersion.Oldest`) |

### Going back to an older release

An older FE-Buddy can't read a file in a newer layout. At launch (`UserConfigFile.CheckLayouts`), and
whenever the profile in use is read, it moves such a file, with the profile's undo copy, to
`%APPDATA%\FE-Buddy\Reverted\<UTC time>\`. Each check gets a new folder, and nothing there is deleted
(a reset that keeps settings keeps it too). In the file's place it puts the newest `v<N>` or
`Shared-v<N>.json` backup it can read, or a file with the defaults, so the profile keeps its place.
The app then says what it did, once.

Before **Go back now** downloads, the update window asks about the profiles that release can't read
(`UserConfigFile.UnreadableBy`, from `UserConfigVersion.Layouts`): **Export…** writes one file per
profile (`UserConfigTransfer.ExportProfiles`) for **Import…** after updating again. Nothing is moved
then. `Shared.json` is never exported: its settings are all this PC's, and the Reverted copy keeps
them.
