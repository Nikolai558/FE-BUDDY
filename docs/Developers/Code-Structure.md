# Code structure

Where code lives in `FeBuddy.Core` and `FeBuddy.Wpf`, the rules both follow, and where new code
goes. For how the pieces work together, see [Architecture](Architecture.md).

## Rules for Core and the app

- **One main type per file**, named after the file, and namespaces follow folders
  (`FeBuddy.Core.Application.Airac.Airways`). Small types that belong together may share a file: a
  feature's result records (`AirportResults.cs`), a NASR parser and its collection, `RelayCommand`
  and `RelayCommand<T>`.
- **The Models/ rule.** A type that only carries data (a record, an enum, a settings object, a
  result) goes in its feature's `Models/` folder. A class that does work sits at the feature root.
  `Airways/Models/AirwaySettings.cs` is data; `Airways/AirwayBuilder.cs` does work.
- **Folders are features, not kinds of code.** Airway code goes in `Airways/`; there are no
  `Helpers/`, `Handlers/` or `Utilities/` folders.
- **Keep it plain.** Classes are mostly `static`; there is no dependency injection and no interface
  "just in case". Where a test needs to swap something out, the class takes it as a parameter (for
  example `AiracCycleDataCache` takes its download and parse functions).
- **XML docs on every public member.** Comments explain *why*; they don't point at planning docs or
  task numbers.

## FeBuddy.Core

### Three layers

Each layer may use only the layers below it:

| Layer | Holds | May use |
|---|---|---|
| **Domain/** | Pure aviation and geometry rules: AIRAC cycle math, airway classification, CRC property rules, naming rules, geodesy. No files, network or settings. | nothing else in Core |
| **Infrastructure/** | Everything that touches the outside world: files, HTTP, GitHub, the config file, logging, credentials, the FAA downloads and their parsers, the conversion file readers. | Domain |
| **Application/** | The features: settings in, result out. | Domain, Infrastructure |

If Domain code needs a file path or a setting, it's in the wrong layer. If Infrastructure code
starts deciding *what* to build, it belongs in Application.

### Folder map

```
FeBuddy.Core/
├── Domain/
│   ├── Airac/            AiracCycleResolver: cycle IDs and dates from the 28-day cadence
│   ├── Airports/ Airways/ Arrivals/ Departures/ ArtccBoundaries/ Fixes/ Navaids/ Telephony/ WxStations/
│   │                     each feature's rules (AirwayClassifier, DepartureNaming, ArrivalNaming,
│   │                     NavaidTypes, FixUses, TelephonyNaming, …) and its models
│   ├── Crc/              CRC feature properties and CrcPropertyValidator
│   ├── Geo/              GeoMath, AntimeridianSplitter, LineStringMerger, SegmentJoiner, RoiFilter,
│   │                     RadiusFilter, Wgs84
│   └── Procedures/       ProcedureChartTypes, ProcedureNaming
│       └── ChartRecall/  the FAA Chart Recall command rules: ChartRecallCodes, ApproachCodes,
│                         SidStarCodes, ChartRecallText
├── Infrastructure/
│   ├── Configuration/    UserConfigFile, UserConfigKeys, DevMode, OutputFormatting, and settings
│   │                     export/import: UserConfigTransfer, UserConfigPortability, PortablePathTokens
│   ├── Credentials/      CredentialStore, WindowsCredentialVault, CredentialHosts, UrlSecrets
│   │                     (see Credentials.md)
│   ├── FileSystem/       AppPaths, TempWorkspace, ServiceOutputPaths, AppDataReset
│   ├── Geojson/          CrcFeatureFactory, GeojsonFileWriter, GeojsonFileSet, SymbolFeatureMerger,
│   │                     AttributesSignature
│   ├── GitHub/           GitHubAuth, GitHubRepository, GitHubFileUrl
│   ├── Http/ Logging/ Markdown/   FeBuddyHttp, AppLog, MarkdownParser
│   ├── Platform/         AppVersion, InstalledProduct, UtcTimeCheck, LegacyGitHubTokenVariable,
│   │                     LegacySquirrelInstall, LegacySquirrelShortcuts
│   ├── Nasr/             NasrCycleDownloader, AiracCycleAvailability, NasrCsvReader, WaypointLocator;
│   │                     Models/ and Parsers/ hold one row model and one parser per NASR CSV group
│   ├── SharedData/       SharedDataDownload: download, check and swap in data kept outside the
│   │                     cycle folders
│   ├── WxStations/       aviationweather.gov's station list (WxStationDownloader, WxStationXmlParser)
│   ├── Telephony/        the FAA's telephony pages and the VATSIM-Radar Virtual Airline List
│   │                     (TelephonyDownloader, TelephonyHtmlParser, VatsimRadarAirlineParser)
│   ├── Dtpp/             the FAA d-TPP Metafile (DtppDownloader, DtppMetafileXmlParser)
│   └── Dat/ Sct/ Eram/   the conversions' readers: DatFileReader, SctFileReader, EramGeoMapReader,
│                         EramConsoleCommandControlReader
└── Application/
    ├── Airac/            AiracService (the entry point), AiracCycleDataCache, AiracSharedDataLoader,
    │   │                 AiracOutputPaths, AiracOutputCatalog, DuplicateAliasReport,
    │   │                 OutputFileNamesParser, FebProperties
    │   ├── Airports/ Airways/ Departures/ Arrivals/ Navaids/ ArtccBoundaries/ Fixes/
    │   │   WxStations/ Procedures/ Telephony/
    │   │                 one folder per sub-service (see "Adding a sub-service" below)
    │   └── ConcatenateAliases/
    │                     ConcatenateAliasesSettingsParser, AliasSourceLoader, CombinedAliasFileWriter
    ├── AliasGuide/       the Alias Command Guide (AliasGuideContent, AliasGuideHtmlWriter,
    │                     AliasGuideMarkdownWriter) and the Alias Command Practice page
    │                     (AliasPracticeContent, AliasPracticeWriter, AliasPractice.js, built in)
    ├── Conversions/      ConversionSettingsReader, ConversionFiles, then DatToGeojson/,
    │                     SctToGeojson/ and EramToGeojson/
    ├── Launch/           LaunchSequence, AppEnvironment, LegacyGitHubTokenNotice
    ├── News/             NewsService
    ├── Settings/         SubServiceSettingsReader, CrcDefaultsReader, SettingsValueReader
    ├── Updates/          VersionCheck, UpdateInstaller, UpdateChannelSetting, AppUninstall
    └── Models/           ServiceResult, ServiceMessage
```

### Naming by role

| Suffix | Role | Example |
|---|---|---|
| `*Service` | An entry point the app calls. Used for nothing else. | `AiracService`, `AirwayService`, `NewsService` |
| `*SettingsParser` | Settings block in, typed settings and warnings out | `AirportSettingsParser` |
| `*Builder` | Builds domain objects from FAA rows | `DepartureBuilder` |
| `*GeojsonWriter`, `*AliasWriter`, `*Writer` | Writes output files | `AirwayGeojsonWriter` |
| `*Reader`, `*Parser` | Reads a file, a text format or a settings block | `NasrCsvReader`, `MarkdownParser` |
| `*Filter`, `*Check`, `*Validator` | Narrows, checks or validates; never writes | `RoiFilter`, `VersionCheck` |
| `*OutputFiles` | A sub-service's file keys | `FixOutputFiles` |
| `*Result`, `*Settings`, `*Progress` | Data, in `Models/` | `AiracServiceResult` |

## FeBuddy.Wpf

MVVM, with no packages beyond Core: `ObservableObject` and `RelayCommand` are in `Mvvm/`. Every
sub-service is a tab on the AIRAC Service screen and every file conversion a page of the File
Conversions screen, opened from its picker; neither is ever a screen of its own.

```
FeBuddy.Wpf/
├── App.xaml(.cs)        startup: a pending reset, the log, then LaunchSequence off the UI thread
├── Theme/               the design system - the only place colours, fonts and control looks are
│                        defined (Palette, Typography, Icons, Controls.*.xaml), merged by Theme.xaml
├── Assets/              FE-BUDDY.ico, Brand/ (the logo's sizes) and BaseMap/ (us-states.json,
│                        coastlines.json, from Natural Earth, built by FeBuddy/Tools/BuildBaseMap.cs)
├── Behaviors/           attached properties a view opts into (FieldState, InlineCode, InlineMarkdown,
│                        WheelScroll, ScrollToTop, BringIntoView, ComboBoxDropDownFocus), and
│                        MaximizeToWorkArea, a window hook the chrome windows install from code
├── Controls/            Card, SectionHeader, Option, CopyButton, FilterPicker, MarkdownView,
│                        MapCanvas, AliasGuideDocumentView, BesideOrBelow, CommandTablePanel,
│                        ChromeWindow, BrandMark
├── Converters/          one IValueConverter per file
├── Map/                 GeoJsonReader, WebMercator, ProjectedLayer, AiracMapLayers, BaseMap
├── Mvvm/                ObservableObject, RelayCommand
├── Shell/               app-wide services: Toast, Links, BrowserLauncher, DefaultRoiStore,
│                        OutputPreferences, AppRestart
├── ViewModels/          ShellViewModel and one per screen or tab; AiracSubServices (the sub-service
│   │                    catalogue); the map's MapViewModel and MapLayersState
│   ├── Models/          small rows and records
│   └── ServiceTabs/     the tabbed-screen framework: TabbedServiceViewModel, ServiceTabViewModel,
│                        SubServiceSettingsViewModel, GeojsonSubServiceViewModel,
│                        ConversionTabViewModel and FileConversionTabViewModel, the Preview
│                        Settings and Review tabs, the General tab's SubServiceRow, validation
│                        (ServiceValidation, ServiceAreas), the card interfaces (IOutputSettings, …)
└── Views/               ShellWindow, TabbedServiceView (AIRAC Service), FileConversionsView, one view
    │                    per tab or conversion page, MapWorkspace (every map), the dialog windows
    └── Cards/           the shared cards (Attention, Outputs, What Files Do You Want?, FE-Buddy
                         Properties, Region of Interest, CRC ERAM Defaults, Source Files, Run);
                         CrcFileChoice, the AIRAC tabs' choice of files at the top of CRC ERAM
                         Defaults; and OutputStatusRow, an output's On/Off line
```

### How the screens are built

- **Navigation** is a `ContentControl` with a `DataTemplate` per view-model. Each page's view-model
  is built once and kept; a page with places inside it (`IOpensAtStart`: AIRAC Service, File
  Conversions, Info) goes back to its first tab, picker or main page each time it is chosen in the
  side nav.
- **AIRAC Service is `TabbedServiceView`.** Tabs are data (`TabbedServiceViewModel`), not
  hand-placed XAML.
- **File Conversions** (`FileConversionsView`) opens on a picker - Source, File, Output, a tree of
  `ConversionChoice`s built in `FileConversionsViewModel` - and Continue opens that conversion's page
  in its place, with its last run's results under its Run card.
- **A sub-service tab** derives from `GeojsonSubServiceViewModel`, which brings the shared cards'
  logic: outputs, file choices, `feb.*` properties, ROI override, and which files get CRC defaults.
  A tab without some of them says so (`HasAliasFile` false, a null `EmitKeys` entry). Which outputs
  are on comes from the sub-service's row on the General tab (`SubServiceRow`, read through
  `ISubServiceOutputs`), so the tab's own save and undo never change them. A sub-service left out
  keeps its tab, greyed out (`ServiceTabViewModel.IsAvailable`). Concatenate Aliases is the exception:
  it derives from `SubServiceSettingsViewModel`, uses none of the shared cards and has no row.
- **A conversion page** derives from `FileConversionTabViewModel` (on `ConversionTabViewModel`):
  source files, CRC defaults and its own run button.
- **Saving.** Each tab saves its own config node. "Unsaved" means different from the last save:
  `SubServiceSettingsViewModel` runs the tab's own `WriteToConfig()` into a buffer and compares it
  with the last saved values (`SavedStateSnapshot`). Validation is continuous. A problem belongs to
  a box (`AddField`, shown on it through `FieldState`) or to a whole card (`AddArea` with a
  `ServiceAreas` key, which the card binds as its own `FieldState.Error`). `AttentionCard` lists
  every problem at the top of the tab, and a `Card` holding one is outlined (`Card.NeedsAttention`).
- **One map.** `Views/MapWorkspace` is the Map page and every map window (`RoiPickerWindow`).
  `MapViewModel` holds the box being edited and where it goes (`IRoiTarget`); everything else lives
  in `MapLayersState.Shared`, so every map shows the same layers. `Controls/MapCanvas` is a
  from-scratch Web-Mercator vector map: no tiles, no network, no map SDK.

### Conventions

- **Tokens, not literals.** Views use `Brush.*`, `Font.*`, `Radius.*` and `Text.*`, not colours or
  fonts of their own. (The map's layer colours are set in code.)
- **Inside `Theme/`, use `DynamicResource`** for tokens: a `StaticResource` from one merged
  dictionary to a sibling silently resolves to `UnsetValue`. Views can use `StaticResource`.
- **Code-style text** (a folder, a file name) goes between backticks with the TextBlock's text set
  through `bhv:InlineCode.Text`. `ConfirmWindow` messages and card footnotes already do this.
- **A CheckBox is square and a RadioButton round:** use a CheckBox when any number can be ticked, a
  RadioButton group when only one can. A plain-text label wraps when there's no room, so it can be
  long. A file name can be plain `Content`: the theme's versions have no access keys, so an
  underscore shows as written.
- **A two-way ComboBox in a template** is safest with its items from `x:Static`, as `AirwaysView`'s
  High and Low Files drop-downs do. Whatever it binds to, test switching to another tab and back:
  bug #251 lost the Airways choices that way.
- **Fonts:** Segoe UI throughout (`Font.Display`, `Font.Body`), Cascadia Mono then Consolas for code
  (`Font.Mono`), and Segoe Fluent Icons for glyphs (`Font.Icon`), set in `Theme/Typography.xaml`.
  Body text is 14.5, captions 12.5; a size outside the `Text.*` styles keeps to the same scale.
- **Window chrome** uses `WindowChrome` without `AllowsTransparency`, so snapping and the system
  shadow still work.

## Where new code goes

- **A new aviation or geometry rule:** `Core/Domain/<Feature>/`.
- **A new NASR CSV group:** its row model in `Infrastructure/Nasr/Models/`, its parser in
  `Infrastructure/Nasr/Parsers/`, wired into `NasrCsvParser`.
- **A new config key:** see [Adding a setting](Settings-Reference.md#adding-a-setting).
- **Something two features share:** the lowest layer both can see. Never copy it.
- **A reusable control:** `Wpf/Controls/`, its look in a `Theme/Controls.*.xaml` style. An attached
  property goes in `Behaviors/`, a converter in `Converters/` (created once in the theme, mostly in
  `Theme/Theme.xaml`), a colour, font or glyph in `Theme/`.

### Adding a sub-service

Say, Preferred Routes:

1. **Core:** `Application/Airac/PreferredRoutes/` with `PreferredRouteService`,
   `PreferredRouteSettingsParser`, `PreferredRouteBuilder`, `PreferredRouteGeojsonWriter`,
   `PreferredRouteOutputFiles` and a `Models/` folder. Read the shared keys with
   `SubServiceSettingsReader`, put files where `AiracOutputPaths` says, add its block to
   `AiracServiceSettings` and its run to `AiracService`.
2. **App:** an entry in `ViewModels/AiracSubServices.cs`, with the outputs it offers (its columns on
   the General tab) and its tooltip text; a `PreferredRoutesViewModel` deriving
   from `GeojsonSubServiceViewModel` and implementing `ISubServiceRunTarget`; a `PreferredRoutesView`
   built from the shared cards, with its `DataTemplate` in `Views/TabbedServiceView.xaml`; and, in
   `AiracServiceViewModel`, a tab accessor (`TabFor<PreferredRoutesViewModel>(…)`) and the line that
   puts its `BuildSettingsBlock()` into the run's settings.

Copy from `Fixes` for the usual shape, `WxStations` for data that doesn't come from NASR, `Telephony`
for an alias file and no GeoJSON, or `Procedures` for documents instead of GeoJSON.

### Adding a file conversion

Say, vSTARS video maps:

1. **Core:** `Application/Conversions/VstarsToGeojson/` with its `*Service`, `*SettingsParser`,
   `*GeojsonWriter` and `Models/`, shaped like `SctToGeojson/`: settings derive from
   `ConversionSettings`, the service runs through `ConversionFiles`. The reader goes in
   `Infrastructure/<Format>/`; if it finds a file isn't its format, it should throw
   `InvalidDataException` (as the ERAM readers do), which fails that file only.
2. **App:** a view-model deriving from `FileConversionTabViewModel`, added in
   `FileConversionsViewModel`'s constructor with its place in the picker (an output of a source, or
   of one of its files), and a view built from `SourceFilesCard`, `CrcDefaultsCard` and `RunCard`,
   with its `DataTemplate` in `FileConversionsView.xaml`.

## Tests

`FeBuddy.UnitTests` mirrors Core's folders (a folder appears once it has tests), the app's under
`Wpf/` (the app's internals are visible to it) and `FeBuddy.Versioning`'s under `Versioning/`. It
also has `Repository/` for checks on the repository's own files, `Fixtures/` for sample data and
`TestSupport/` for helpers. A test that creates a WPF control runs its body through
`StaThread.Run`.
