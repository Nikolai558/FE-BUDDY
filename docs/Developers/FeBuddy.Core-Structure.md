# FeBuddy.Core Structure

`FeBuddy.Core` holds everything FE-Buddy does that isn't UI: it downloads and parses the FAA
NASR data, builds airways, airports and departures from it, and writes GeoJSON and alias files.
`FeBuddy.Wpf` is a thin shell over it. This page explains where code lives and why.

The projects around it follow the same standards (one `.editorconfig`, required XML docs,
the Models/ rule, one type per file):

| Project | What it is |
|---|---|
| `FeBuddy.Wpf` | The desktop app. Its layout is in [FeBuddy.Wpf/README.md](FeBuddy.Wpf/README.md). |
| `FeBuddy.Versioning` | The SemVer version and the update rule (`ProductVersion`, `UpdatePolicy`). netstandard2.0, so both Core and the installer's custom action can use it. See [VERSIONING.md](VERSIONING.md). |
| `FeBuddy.Installer.CustomActions` | The MSI's one managed custom action, a thin wrapper over `UpdatePolicy`. net472, because WiX's custom-action host only loads .NET Framework. Not unit tested (it needs an MSI session). |
| `FeBuddy.UnitTests` | Tests for Core and Versioning, in folders that mirror theirs. |

## Three layers, one project

Core is split into three top-level folders. They are layers, not separate projects, and each one
may only depend on the layers below it:

```
Application  ──►  Infrastructure  ──►  Domain
     └──────────────────────────────────►┘
```

| Layer | Holds | May use |
|---|---|---|
| **Domain/** | Pure aviation and geometry rules: AIRAC cycle math, airway classification, CRC property rules, geodesy. No files, no network, no settings. | Nothing else in Core |
| **Infrastructure/** | Anything that touches the outside world: files and folders, HTTP, GitHub, the user config file, logging, the NASR download and its CSV parsers. | Domain |
| **Application/** | The features: settings in, result out. They orchestrate Domain rules over Infrastructure data. | Domain, Infrastructure |

If Domain code needs a file path or a setting, it's in the wrong layer. If Infrastructure
code starts deciding *what* to build, it belongs in Application.

We keep this pragmatic (YAGNI): classes are mostly `static`, and there is no dependency injection
and no interfaces "just in case". Where a test needs to swap something out, the class takes it
as a constructor or method parameter (for example `AiracCycleDataCache` accepts its
download/parse functions).

## Folder map

```
FeBuddy.Core/
├── Domain/
│   ├── Airac/        AiracCycleResolver: cycle ids and dates from the 28-day cadence
│   ├── Airports/     Airport, AirportRunway models
│   ├── Airways/      AirwayClassifier and the Airway/segment/point models
│   ├── Arrivals/     ArrivalNaming and procedure models
│   ├── ArtccBoundaries/  ArtccBoundaryAltitudes (the ALTITUDE vocabulary) and the boundary
│   │                     location/point/ring models
│   ├── Crc/          CRC feature properties and CrcPropertyValidator
│   ├── Departures/   DepartureNaming and procedure models
│   ├── Fixes/        FixUses, FixCharts, FixTokens (the FIX_USE_CODE and CHARTS vocabularies, and
│   │                 the shared file-naming/CRC-class token rule) and the Fix model
│   ├── Geo/          GeoMath, antimeridian splitting, line merging and segment joining, ROI and
│   │                 radius clipping, Wgs84
│   ├── Navaids/      NavaidTypes (the NAV_TYPE vocabulary, file-naming tokens, CRC symbol styles,
│   │                 frequency formatting) and the Navaid model
│   └── WxStations/   WxStationCountries (the included US/territory codes), WxStationLabels (the
│                     Text second-line rule) and the WxStation model
├── Infrastructure/
│   ├── Configuration/  UserConfigFile, UserConfigKeys, DevMode, OutputFormatting
│   ├── Dat/            DatFileReader: FAA .dat RADAR Video Maps
│   ├── FileSystem/     AppPaths, TempWorkspace, ServiceOutputPaths (the FE-Buddy_Output layout)
│   ├── Geojson/        CrcFeatureFactory, GeojsonFileWriter, GeojsonFileSet
│   ├── GitHub/  Http/  Logging/  Markdown/  Platform/
│   ├── Nasr/           Download, availability, CSV reading, WaypointLocator
│   │   ├── Models/     One row-model file per NASR CSV group
│   │   └── Parsers/    One parser per group + NasrCsvParser (parses them all)
│   ├── WxStations/     WxStationDownloader, WxStationFiles - aviationweather.gov's own station
│   │   │               list, not NASR, downloaded once per launch and cached alongside the NASR
│   │   │               CSVs in each cycle's folder
│   │   ├── Models/     WxStationXmlDataModel, WxStationDataCollection
│   │   └── Parsers/    WxStationXmlParser
│   ├── Eram/           EramGeoMapReader: an ERAM adaptation export's Geomaps.xml (streamed)
│   └── Sct/            SctFileReader: VRC .sct2 / .sct sector files
└── Application/
    ├── Airac/          AiracService (entry point), AiracCycleDataCache, AiracOutputPaths, FebProperties
    │   ├── Airways/    One folder per sub-service, all shaped the same way
    │   ├── Airports/
    │   ├── Departures/
    │   ├── Arrivals/
    │   ├── Navaids/
    │   ├── ArtccBoundaries/  No alias file, so no *AliasWriter and no GenerateAliasFile key
    │   ├── Fixes/            No alias file either, so no *AliasWriter and no GenerateAliasFile key
    │   └── WxStations/       No alias file either; its data comes from Infrastructure/WxStations,
    │                         not a NASR CSV group
    ├── Conversions/    ConversionSettingsReader and ConversionFiles (what every conversion
    │   │               shares), then one folder per file conversion
    │   ├── DatToGeojson/
    │   ├── EramToGeojson/
    │   └── SctToGeojson/
    ├── Launch/         LaunchSequence, AppEnvironment
    ├── News/           NewsService
    ├── Settings/       Shared readers for the string settings dictionaries
    ├── Updates/        VersionCheck, UpdateInstaller
    └── Models/         ServiceResult, ServiceMessage (shared by every service)
```

Inside a layer, folders are **features**, not kinds of code. Airway things go in `Airways/`,
not in `Helpers/`, `Handlers/` or `Utilities/`; we don't have those folders.

## The Models/ rule

A type that only **carries data** (a record, an enum, a settings object, a result) goes in its
feature's `Models/` subfolder. A class that **does work** sits at the feature root.

- `Application/Airac/Airways/Models/AirwaySettings.cs`: data, so it's in Models/.
- `Application/Airac/Airways/AirwayBuilder.cs`: does work, so it's at the root.

Namespaces follow the folders exactly (`FeBuddy.Core.Application.Airac.Airways.Models`).

## Naming by role

A class's suffix tells you what it does:

| Suffix | Role | Example |
|---|---|---|
| `*Service` | **Entry point** a UI calls. Reserved for these only. | `AiracService`, `AirwayService`, `NewsService` |
| `*SettingsParser` | Turns the raw `string → string` settings into a typed settings object plus warnings | `AirportSettingsParser` |
| `*Builder` | Builds domain objects from NASR rows | `DepartureBuilder`, `AirwayGeometryBuilder` |
| `*GeojsonWriter` / `*AliasWriter` / `*Writer` | Writes output files | `AirwayGeojsonWriter` |
| `*Reader` / `*Parser` | Reads a file or text format | `NasrCsvReader`, `MarkdownParser` |
| `*Filter` / `*Check` / `*Validator` | Narrows, checks or validates, and never writes | `RoiFilter`, `VersionCheck`, `CrcPropertyValidator` |
| `*Factory` | Creates configured objects | `CrcFeatureFactory` |
| `*Result` / `*Settings` / `*Progress` | Data carriers, found in `Models/` | `AiracServiceResult` |

## How a run flows

**At launch**, `LaunchSequence.RunAsync` clears the temp folder, reads the config, checks UTC
time and internet access, then runs three steps concurrently: the version check, the News
fetch, and the AIRAC data step. The AIRAC step works out the previous, current and next cycles
(`AiracCycleResolver`). It then has `AiracCycleDataCache` download and parse whichever of them the
FAA has published (`NasrCycleDownloader` → `NasrCsvParser.ParseAllAsync`) and prune every
other cached cycle. A failing step is logged and degrades only its
own feature; launch never stops.

**When the user runs AIRAC**, the UI builds an `AiracServiceSettings` (the selected cycle, the
output folder, plus one settings block per sub-service) and calls `AiracService.RunAsync`. It gets
the parsed cycle from the cache (waiting for an in-flight parse rather than starting another) and
runs each sub-service whose settings block isn't null, pointing every one at the same
`AIRAC_<cycle>` folder (`AiracOutputPaths`). Every sub-service follows the same pipeline:

```
AirwayService.Run(nasrData, settings)
  1. AirwaySettingsParser.Parse   string settings  → AirwaySettings + warnings
  2. AirwayBuilder.BuildAll       NASR rows        → Airway domain objects
  3. AirwayGeojsonWriter.Generate airways in ROI   → GeojsonFileSet
  4. AirwayAliasWriter.Generate   all airways      → alias .txt (if enabled)
  → AirwayServiceResult (files written, ServiceMessages, timing)
```

Airports, Departures, Arrivals and NAVAIDs have the same shape (NAVAIDs' writer just skips the
Lines step - it has no Lines file). ARTCC Boundaries, Fixes and Wx Stations differ more: none has
step 4 at all - `ArtccBoundaryService` stops after `ArtccBoundaryGeojsonWriter`, `FixService` stops
after `FixGeojsonWriter`, and `WxStationService` stops after `WxStationGeojsonWriter` - since none
has an alias file. ARTCC Boundaries' writer produces Lines only; Fixes' and Wx Stations' each
produce Symbols and Text only, the same Lines-skipping shape as NAVAIDs. Wx Stations also breaks
the `nasrData` pattern at step 1: its input is a `WxStationDataCollection` parsed from
aviationweather.gov's own station list (`Infrastructure/WxStations`), not the NASR cycle - see
[Architecture](Architecture.md#the-airac-data-pipeline). Problems are
reported as `ServiceMessage`s (warnings or errors) in the result instead of being thrown, so one
bad setting doesn't lose the whole run. The only exception is a missing required setting, which
throws `ArgumentException`.

**When the user runs a file conversion**, the UI builds one settings block and calls that
conversion's service directly - there is no cycle data and no aggregate. The pipeline has the
same shape, one input file at a time:

```
DatToGeojsonService.Run(settings, progress)
  1. DatToGeojsonSettingsParser.Parse  string settings → DatToGeojsonSettings + warnings
  for each .dat file (a folder's, or the ones named):
  2. DatFileReader.Read                .dat file       → point of tangency + LineStrings
  3. RadiusFilter.ClipLines            (if cropping)   → the pieces within the distance
  4. AntimeridianSplitter.Split                        → no line wraps round the map
  5. DatGeojsonWriter.Write                            → <name>.geojson
  → DatToGeojsonServiceResult (one DatFileConversion per file, ServiceMessages, timing)
```

A file that cannot be read or cropped is an `Error` message and a failed `DatFileConversion`;
the other files still convert. SCT2 to GeoJSON (`SctFileReader` → `SctGeojsonWriter`) and ERAM
to GeoJSON (`EramGeoMapReader` → `EramGeojsonWriter`, with `EramCrcProperties` turning ERAM's
styling into validated CRC defaults and overrides) have the same shape. `ConversionFiles` takes an
optional check on a folder's files, so ERAM picks `Geomaps.xml` out of a whole adaptation export
(`EramGeoMapReader.IsGeoMapsFile`). Both write lines through
`Domain/Geo/SegmentJoiner`, which joins two-point segments back into lines, merges repeats with
`LineStringMerger` and splits them with `AntimeridianSplitter`.

What every conversion shares lives in `Application/Conversions/`, never copied into each one:
`ConversionSettingsReader` (the source and output keys), `ConversionFiles` (finding a folder's
files, the per-file loop that reports progress and turns an unreadable file into one failed file,
and file-safe and unique file names), and in `Models/` the `ConversionSettings`,
`ConversionServiceResult` and `ConversionProgress` bases each conversion's own types derive from,
plus `SourceFileConversion` / `SourceFilesConversionResult` for the conversions that write several
files per source (SCT2, ERAM).

## Where do I put…

- **A new aviation or geometry rule** (no I/O): `Domain/<Feature>/`.
- **A new NASR file**: its row model in `Infrastructure/Nasr/Models/`, its parser in
  `Infrastructure/Nasr/Parsers/`, and wire it into `NasrCsvParser`.
- **A new AIRAC output** (say, Preferred Routes): `Application/Airac/PreferredRoutes/` with
  `PreferredRouteService`, `PreferredRouteSettingsParser`, `PreferredRouteBuilder`,
  `PreferredRouteGeojsonWriter`, `PreferredRouteOutputFiles` (its file keys) and a `Models/` folder.
  Add its settings block to `AiracServiceSettings` and one `RunSubServiceAsync` call to
  `AiracService`. Reuse `Application/Settings/SubServiceSettingsReader` for the common keys
  (precision, ROI, FEB properties, the vNAS files), and put each file where
  `AiracOutputPaths.FileDirectory` says. `Application/Airac/Fixes/` is a real example of this shape
  to copy from - or `Application/Airac/WxStations/` for one whose data doesn't come from a NASR CSV
  group at all.
- **A new file conversion** (say, vSTARS video maps): `Application/Conversions/VstarsToGeojson/`
  with its `*Service`, `*SettingsParser`, `*GeojsonWriter` and a `Models/` folder, shaped like
  `SctToGeojson/`: its settings derive from `ConversionSettings`, its result is a
  `SourceFilesConversionResult` (or derives from `ConversionServiceResult`), and its service runs
  through `ConversionFiles`. A reader that finds the content is not its format throws
  `InvalidDataException`, which fails that one file. The code that reads
  the source format goes in `Infrastructure/<Format>/`.
- **A new config key**: a constant in `Infrastructure/Configuration/UserConfigKeys`.
- **Something shared by two features**: the lowest layer that both can see. Never copy it.

## Standards and checks

- **One `.editorconfig`** at `FeBuddy/.editorconfig` covers every project: tabs, file-scoped
  namespaces, `PascalCase` for constants and `static readonly` fields, `_camelCase` for other
  private fields, and snake_case test method names.
- **XML docs are required.** `GenerateDocumentationFile` is on, so any undocumented public
  member is a build warning (CS1591). Write a `<summary>`, plus `<param>` and `<returns>` where
  they apply. Comments explain *why*; don't point at planning docs or task numbers.
- **Tests** in `FeBuddy.UnitTests` mirror Core's folders one-to-one.

Before you commit, run these from `FeBuddy/`:

```bash
dotnet build FeBuddy.sln
```

```bash
dotnet format style FeBuddy.Core/FeBuddy.Core.csproj --severity info --verify-no-changes
```

```bash
dotnet format whitespace FeBuddy.Core/FeBuddy.Core.csproj --verify-no-changes
```

```bash
./coverage.ps1 -MinimumLineCoverage 95 -MinimumBranchCoverage 90
```

The build should have zero warnings, the format checks should report no changes, and coverage
must stay above the CI floor of 95% line and 90% branch.
