# 120 · Business logic living in the GUI

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

Business rules that live in view-models today and belong in Core: Procedure Charts' area and name matching, Telephony's real-operator rule (which has already drifted, see [W1-7](100_Bugs.md#w1-7)), Fixes' combination parsing, parser limits, file naming, and the map's GeoJSON reader and AIRAC layer catalog. Session [S5](020_Work-Plan.md#s5); much of it overlaps [S3](020_Work-Plan.md#s3) and [S4](020_Work-Plan.md#s4).

**25 findings** (3 Med, 22 Low). Quote the ID when you comment on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374).

## Summary

| ID | Kind | Sev | Finding | Status | Session |
|---|---|---|---|---|---|
| [U1-17](#u1-17) | Logic in GUI | Med | NASR-derived option lists and their rules live in the tab view models | Fixed | S5 |
| [W3-9](#w3-9) | Logic in GUI | Med | GeoJSON-to-geometry reading and the live AIRAC layer catalog live in the GUI | Fixed | S5 |
| [W1-6](#w1-6) | Logic in GUI | Med | Procedure Charts re-implements Core's selection and name-matching rules | Fixed | S5 |
| [W6-8](#w6-8) | Logic in GUI | Low | The GUI guesses which words in Core's messages are file names | Fixed | S5 |
| [C2-6](#c2-6) | Logic in GUI | Low | The Map's "towered" test copies AirportFieldMaps' display string | Fixed | S5 |
| [W7-6](#w7-6) | Logic in GUI | Low | The coordinate-precision rule (0-15, default 6) is restated in the GUI twice | Fixed | S5 |
| [W2-7](#w2-7) | Logic in GUI | Low | Per-sub-service result knowledge lives in the GUI three times | Fixed | S5 |
| [W2-23](#w2-23) | Logic in GUI | Low | Sub-service keys, node paths and order are defined in the GUI and again in Core | Fixed | S5 |
| [W1-11](#w1-11) | Logic in GUI | Low | Airways parses Core's message text and file keys, and spells Core's enum as string literals | Fixed | S5 |
| [W2-18](#w2-18) | Logic in GUI | Low | Arrivals copies Core's parser limits, ARTCC splitting and file naming | Fixed | S5 |
| [W2-20](#w2-20) | Logic in GUI | Low | ARTCC Boundaries repeats Core's altitude vocabulary and builds the per-ARTCC file list itself | Fixed | S5 |
| [W1-8](#w1-8) | Logic in GUI | Low | Add Custom Aliases keeps its own copy of the parser's and loader's rules, and its doc says they're the same | Fixed | S5 |
| [W1-9](#w1-9) | Logic in GUI | Low | Numbered-key lists are parsed by hand in two tabs, beside Core's parsers | Fixed | S5 |
| [W2-21](#w2-21) | Logic in GUI | Low | DAT cropping-distance validation copies Core's parse rule | Fixed | S5 |
| [W1-12](#w1-12) | Logic in GUI | Low | Departures repeats the parser's limits, date format and file layout | Fixed | S5 |
| [W2-25](#w2-25) | Logic in GUI | Low | ERAM's "would this run delete anything" check parses the settings block in the GUI | Fixed | S5 |
| [W2-22](#w2-22) | Logic in GUI | Low | The "no two files share a name" rule is only half in Core | Fixed | S5 |
| [W1-10](#w1-10) | Logic in GUI | Low | Fixes re-implements combination parsing and file naming | Fixed | S5 |
| [W2-19](#w2-19) | Logic in GUI | Low | NAVAIDs repeats NavaidBuilder's SHUTDOWN rule | Fixed | S5 |
| [W3-12](#w3-12) | Logic in GUI | Low | Profile-name "already taken" rule duplicated from Core | Fixed | S5 |
| [W4-6](#w4-6) | Logic in GUI | Low | Which folder files a conversion reads is decided twice, in the page and in Core | Fixed | S5 |
| [W3-10](#w3-10) | Logic in GUI | Low | Facility key duplicated as a literal; doc names only one reader | Fixed | S5 |
| [W3-13](#w3-13) | Logic in GUI | Low | Settings backup before reset/uninstall is file logic in the view-model | Fixed | S5 |
| [W3-14](#w3-14) | Logic in GUI | Low | Version-check status is worded by three separate ladders (and a failed check is called "offline") | Fixed | S5 |
| [N1-33](#n1-33) | Logic in GUI | Low | Departures and Arrivals build their GeoJSON folder instead of using Core's pattern | Open | S14 |

## Med

<a id="u1-17"></a>

### U1-17 · Logic in GUI · Med · NASR-derived option lists and their rules live in the tab view models

**Status:** Fixed (not yet merged), with [U1-1](110_GUI-Lag.md#u1-1): the tabs' lists and their rules - normalizing, STAR_BASE.ARTCC split, shut-down NAVAIDs (`NavaidBuilder.IsShutDown`), the known fix-use and NAVAID-type order, the boundary altitude order (`ArtccBoundaryAltitude`) - are in `AiracCycleOptionListBuilder`, and the GUI copies (`KnownFixUseOrder`, `KnownTypeOrder`, `ShutdownStatus`, `AltitudeOrder`) are gone. The Settings page's facility list followed with [U1-13](110_GUI-Lag.md#u1-13) in S4 (`FacilityNames`) · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/FixesViewModel.cs:244-282`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/FixesViewModel.cs#L244-L282),611; [`FeBuddy/FeBuddy.Wpf/ViewModels/NavaidsViewModel.cs:34`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/NavaidsViewModel.cs#L34),153-159,438; [`FeBuddy/FeBuddy.Wpf/ViewModels/ArtccBoundariesViewModel.cs:150-157`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ArtccBoundariesViewModel.cs#L150-L157),367; [`FeBuddy/FeBuddy.Wpf/ViewModels/ArrivalsViewModel.cs:244-249`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ArrivalsViewModel.cs#L244-L249); [`FeBuddy/FeBuddy.Wpf/ViewModels/DeparturesViewModel.cs:247-251`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DeparturesViewModel.cs#L247-L251); [`FeBuddy/FeBuddy.Wpf/ViewModels/AirwaysViewModel.cs:213-217`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AirwaysViewModel.cs#L213-L217); [`FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs:895-899`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs#L895-L899); [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:1131-1136`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L1131-L1136)

**Problem:** which NASR values become toggles, how they are normalized (trim/upper, STAR_BASE.ARTCC split on whitespace), what is excluded (shut-down NAVAIDs), and their display order (`KnownFixUseOrder`, `KnownTypeOrder`, `AltitudeOrder`) are data rules re-implemented in the GUI next to the builders that apply them. If a builder's rule changes, the toggles silently drift from what a run does, and the Harness cannot reuse them.

**Proposed:** put the derivations in Core in the class proposed in [U1-1](110_GUI-Lag.md#u1-1) (`AiracCycleOptionLists.Build`), reusing the builders' own constants/parsers (make `NavaidBuilder.ShutdownStatus` internal/public or expose `NavaidBuilder.IsShutDown(row)`, use `ArtccBoundaryAltitudes` parsing and order, `ArrivalBuilder`'s ARTCC split), with the Settings facility list (`FacilityOption`) as one more member.

<a id="w3-9"></a>

### W3-9 · Logic in GUI · Med · GeoJSON-to-geometry reading and the live AIRAC layer catalog live in the GUI

**Status:** Fixed (not yet merged), as proposed: the reader is Core's `GeojsonFileReader` (`Infrastructure/Geojson`), with `GeoPoint`, `MapGeometry`, `MapGeometryKind` and `MapFeature` in `Domain/Map/Models` and `WebMercator` in `Domain/Geo`. It reads a file as it streams in (`ReadFile`, `Read(Stream)`), so a big output file isn't held twice; the base map's resources use the same stream read. It parses synchronously, as it already runs off the UI thread. A file must now be UTF-8, as GeoJSON's own spec says; a UTF-16 file was read before. The layer catalog is `Application/Map/AiracMapData`: the kinds, names, descriptions and each layer's shapes. The map keeps colours, `MapLayer` and its styling, `ProjectedLayer` and `FrozenBrush`. Tests: `GeojsonFileReaderTests`, `AiracMapDataTests`, and `WebMercatorTests` and `MapFeatureTests`, moved to Core's test folders · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs:601-610`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs#L601-L610), 980-991 (uses [`FeBuddy.Wpf/Map/GeoJsonReader.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Map/GeoJsonReader.cs), [`Map/AiracMapLayers.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Map/AiracMapLayers.cs))

**Problem:** Parsing GeoJSON files (format rules: CRC isDefaults features skipped, coordinate checks) and choosing/building map data from NASR (which airports count as towered, which NAVAID types, boundary rings) are data rules, not presentation; they sit in FeBuddy.Wpf/Map and can't be used by the Harness or tested without WPF. `GeoJsonReader` and `MapGeometry`/`MapFeature`/`GeoPoint` have no WPF dependency; only `MapLayer` (Brush) does. `File.ReadAllText` + `JsonDocument.Parse(string)` also holds the file twice in memory (UTF-16 string + document) for large output files.

**Proposed:** Move the reader to Core as `Infrastructure/Geojson/GeojsonFileReader` (stream-based: `JsonDocument.ParseAsync(FileStream)`) returning plain geometry models, and the layer catalog to Core as e.g. `Application/Map/AiracMapData` (kinds, names, descriptions, Build -> plain geometries); keep colours, `MapLayer`, `ProjectedLayer` and `FrozenBrush` in Wpf. MapLayersState then only wraps Core results into `MapLayer`s.

**Note:** Also found as [W6-2](#w6-2), [W6-4](#w6-4).

<a id="w1-6"></a>

### W1-6 · Logic in GUI · Med · Procedure Charts re-implements Core's selection and name-matching rules

**Status:** Fixed (not yet merged), as proposed: `ProcedureSelection.AreaIncludes` is the run's rule for taking an airport whole, used by `Select` and by the tab's "Already included by…" hint; `ProcedureNameIndex.ResolveRoot` and `NamesFor` are the name lookups the tab calls. The airport ID lookup was already Core's, from [U1-1](110_GUI-Lag.md#u1-1). Tests: `ProcedureSelectionTests`, `ProcedureNameIndexTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs:1437-1462`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs#L1437-L1462), 1327-1355, 1499-1535, 567-611

**Problem:** `IncludedBy` copies `ProcedureSelection.Select`'s whole-airport rule (ProcedureSelection.cs:88-96): `RESP_ARTCC_ID` in the ticked facilities, or `RoiFilter.Contains` on APT_BASE's lat/lon. `TryResolveAirportId` copies `MatchesAirport` (FAA or ICAO ident). `ResolveRoot`/`CurrentNames` and `SetDtppData`'s filter (not a continuation, departure or arrival, `BaseName`, `RootName`) copy Core's root-name matching (ProcedureSelection.cs:122-155, 233-246). If Core's rule changes, the tab's "Already included by…" hints and "Cycle 2611: …" rows drift from what the run does.

**Proposed:** Add `ProcedureSelection.AreaIncludes(SubServiceArea area, IReadOnlySet<string> facilities, RegionOfInterest? roi, string? respArtcc, double? lat, double? lon)` and use it in both `Select` and `IncludedBy`. Build the name lookups in the Core `ProcedureNameIndex` proposed in [W1-1](110_GUI-Lag.md#w1-1), with `ResolveRoot`/`NamesFor(root, airport?)` methods that the tab calls.

**Note:** Also found as [U1-18](#u1-18).

## Low

<a id="w6-8"></a>

### W6-8 · Logic in GUI · Low · The GUI guesses which words in Core's messages are file names

**Status:** Fixed (not yet merged), the second way proposed: the converter stays, as Core's messages are plain text for the log and the Harness, but its extensions are Core's (`FileNameExtensions.All`, built from the services' own lists), so a new format is picked out as soon as a service reads or writes it. Tests: `FileNameExtensionsTests`, `FileNamesAsCodeConverterTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/Converters/FileNamesAsCodeConverter.cs:10-18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Converters/FileNamesAsCodeConverter.cs#L10-L18), 46-49

**Problem:** Core builds these messages from `OutputFileNames` and knows which parts are file names. The GUI works them out again with a regex and a hard-coded extension list that has to follow every format Core reads or writes; a new extension would silently show as plain text.

**Proposed:** Have Core put file names between backticks where it builds messages. `InlineCode` already renders them, and backticks are harmless in the log and the Harness. Then delete the converter and FileNamesAsCodeConverterTests. If the converter stays, take the extension list from Core.

<a id="c2-6"></a>

### C2-6 · Logic in GUI · Low · The Map's "towered" test copies AirportFieldMaps' display string

**Status:** Fixed (not yet merged), the first way proposed: `Airport.IsTowered`, set by `AirportBuilder` from `TWR_TYPE_CODE`, and `AiracMapData.ToweredAirports` filters on it. The rule is [W6-3](100_Bugs.md#w6-3)'s, fixed with it: the code is `ATCT` or starts with it, so a plain `TRACON` has no tower. The layer still builds the whole Airports model, which is the run's own builder, so the map shows what a run works from. Tests: `AirportBuilderTests`, `AiracMapDataTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/Map/AiracMapLayers.cs:101-102`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Map/AiracMapLayers.cs#L101-L102); [`FeBuddy/FeBuddy.Core/Application/Airac/Airports/AirportFieldMaps.cs:28`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airports/AirportFieldMaps.cs#L28)

**Problem:** Whether an airport has a tower is an Airports rule, but the GUI decides it by matching the display text that AirportFieldMaps produces for `NON-ATCT`. If that wording changes, the Towered Airports layer quietly shows every airport. AirportFieldMaps' own remark says there is "exactly one definition of each" mapping. The layer also builds the whole Airports model (runways, FRQ, CLS_ARSP) just to filter it.

**Proposed:** add `Airport.IsTowered` (set in AirportBuilder from `TWR_TYPE_CODE` != `NON-ATCT`, the rule `IsNonTowered` already applies) or `AirportFieldMaps.IsTowered(string? towerTypeCode)`, and use it in AiracMapLayers.

**Note:** See also [W6-3](100_Bugs.md#w6-3) (the bug this causes).

<a id="w7-6"></a>

### W7-6 · Logic in GUI · Low · The coordinate-precision rule (0-15, default 6) is restated in the GUI twice

**Status:** Fixed (not yet merged), as proposed: `GeojsonFileWriter.DefaultPrecision` and `MaxPrecision` sit beside `NoRounding`, and `SubServiceSettingsReader.CoordinatePrecisionOrDefault` reads a saved value for `OutputPreferences` and the Settings page; `ReadCoordinatePrecision` uses the same constants. Tests: `SubServiceSettingsReaderTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/Shell/OutputPreferences.cs:20-21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Shell/OutputPreferences.cs#L20-L21),48-56; [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:758`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L758); [`FeBuddy/FeBuddy.Core/Application/Settings/SubServiceSettingsReader.cs:135-136`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Settings/SubServiceSettingsReader.cs#L135-L136)

**Problem:** Core owns the rule (SubServiceSettingsReader, and `UserConfigKeys.CoordinatePrecision`'s doc), but OutputPreferences and SettingsViewModel each hard-code the same range and default; a change in Core would silently disagree with what the GUI sends.

**Proposed:** Add public constants in Core (e.g. `GeojsonFileWriter.DefaultPrecision = 6`, `MaxPrecision = 15`, next to `NoRounding`) or a `CoordinatePrecision.Read(string?)` helper, use it in SubServiceSettingsReader, OutputPreferences and SettingsViewModel, and delete `OutputPreferences.DefaultCoordinatePrecision`.

**Note:** Also found as [W3-11](#w3-11).

<a id="w2-7"></a>

### W2-7 · Logic in GUI · Low · Per-sub-service result knowledge lives in the GUI three times

**Status:** Fixed (not yet merged): every file a run wrote is Core's `AiracServiceResult.FilesWritten` (a stopped run names no alias file, with [W2-5](100_Bugs.md#w2-5)), and the run's one line is `AiracRunSummary.Describe`; the AIRAC Service screen only shows them. Each tab's Review block (`DescribeRunResult`) and Core's progress lines still word their own counts: they say different things, at different times. Tests: `AiracRunSummaryTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:689-787`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L689-L787) (`CollectFilesWritten`), 793-874 (`SummarizeRun`)

**Problem:** Which files each sub-service wrote and how to summarise its counts is result knowledge; it is hand-written here per sub-service, again in each tab's `DescribeRunResult`, and again in Core's progress summaries (AiracService.cs:336-372). Adding a sub-service means editing all three.

**Proposed:** Add `AiracServiceResult.FilesWritten` (computed in Core, already excluding deleted alias files) and use it here; let `SummarizeRun` take each target's part from `ISubServiceRunTarget` (e.g. a `ToastPart` on `SubServiceRunResult`) instead of switching on result properties.

<a id="w2-23"></a>

### W2-23 · Logic in GUI · Low · Sub-service keys, node paths and order are defined in the GUI and again in Core

**Status:** Fixed (not yet merged), as proposed: Core's `AiracTabs` spells every AIRAC tab's key once, with `NodeFor` and `TabOf`; `UserConfigAreas.Tabs`, the GUI's `AiracSubServices` and each tab's `Node` are built from it, and the area lookup reads a tab's key with `TabOf` rather than slicing its node. The run order in `AiracService.RunAsync` is left as it is. Tests: `AiracTabsTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracSubServices.cs:31-68`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracSubServices.cs#L31-L68); [`ArrivalsViewModel.cs:34`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ArrivalsViewModel.cs#L34); [`NavaidsViewModel.cs:33`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/NavaidsViewModel.cs#L33); [`ArtccBoundariesViewModel.cs:32`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ArtccBoundariesViewModel.cs#L32); [`AirportsViewModel.cs:24`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AirportsViewModel.cs#L24); [`WxStationsViewModel.cs:32`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/WxStationsViewModel.cs#L32); [`FileNamesViewModel.cs:43`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/FileNamesViewModel.cs#L43); [`AiracGeneralTabViewModel.cs:36`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs#L36)

**Problem:** The same names live in Core's `UserConfigAreas.Tabs`/`ProceduresTab`/`AiracNode` (Infrastructure/Configuration/UserConfigAreas.cs:38-64), and `GeojsonSubServiceViewModel.SavedArea` maps between them by slicing `NodePath`. The run order is hard-coded separately in AiracService.RunAsync.

**Proposed:** Put the sub-service keys and a `NodeFor(key)` helper in Core (next to UserConfigAreas/UserConfigKeys), reference them from AiracSubServices and each tab's `Node`.

<a id="w1-11"></a>

### W1-11 · Logic in GUI · Low · Airways parses Core's message text and file keys, and spells Core's enum as string literals

**Status:** Fixed (not yet merged), as proposed: `ServiceMessage.Subject` says what a message is about; the four Airways messages that name an airway set it, and the Review tab groups by it. `AirwayOutputFiles.TryParseGeojsonKey` reads a key back; the tab spells the alias scope with `AliasRoiScope`. Tests: `AirwayOutputFilesTests`, `AirwayMessageLevelsTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AirwaysViewModel.cs:34`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AirwaysViewModel.cs#L34), 264-270, 570-575, 284, 317, 350

**Problem:** The Review tab's grouping depends on the exact wording of four Core messages (AirwayBuilder.cs:79, 109; AirwayGeometryBuilder.cs:70; AirwayWaypointBuffer.cs:94). `FileGroups` assumes `AirwayOutputFiles`' key shape (`Airways_<group>_<Kind>`). `"RoiAirways"`/`"All"` repeat `AliasRoiScope`'s names (Models/AliasRoiScope.cs) in three places.

**Proposed:** Give `ServiceMessage` an optional `Subject` (e.g. the airway ID) set by the Airways builders, and group by it. Add `AirwayOutputFiles.TryParseGeojsonKey(key, out string group, out CrcFeatureKind kind)` (as `FixOutputFiles.TryParseGroupKey` does). Use `nameof(AliasRoiScope.RoiAirways)` / `AliasRoiScope.All.ToString()`, or parse with `Enum.TryParse<AliasRoiScope>`.

**Note:** Also found as [U1-19](#u1-19).

<a id="w2-18"></a>

### W2-18 · Logic in GUI · Low · Arrivals copies Core's parser limits, ARTCC splitting and file naming

**Status:** Fixed (not yet merged), as proposed, with [W1-12](#w1-12): the same `ProcedureAmendmentFilter`, and `ArrivalOutputFiles.GeojsonFileNamePattern`/`GeojsonFolderPattern`. The ARTCC split left the tab with [U1-1](110_GUI-Lag.md#u1-1); Core's option lists now split STAR_BASE.ARTCC with `ArrivalBuilder.SplitArtccs`, as the run does. Tests: `ProcedureAmendmentFilterTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ArrivalsViewModel.cs:36-44`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ArrivalsViewModel.cs#L36-L44), 131-135, 244-249, 497-500

**Problem:** The limits and date format repeat ArrivalSettingsParser.cs:90, 94, 168; the ARTCC split repeats ArrivalBuilder.SplitArtccs (private); the file/folder patterns repeat ArrivalOutputFiles.GeojsonFileName/GeojsonDirectory - and the tab spells the pattern two ways (`<Airport ID>_<Arrival ID>_STAR_Lines.geojson` in What You'll Get, `<airport>_<procedure>_STAR_Lines.geojson` on File Names). DeparturesViewModel has the same copies.

**Proposed:** Expose `ArrivalSettingsParser.MaxAmendedWithinCycles/Days` and `AmendmentDateFormat` (as DatToGeojsonSettingsParser.MaxCroppingDistanceNm already is), `ArrivalBuilder.ArtccIds(NasrCsvDataCollection)` and a `ArrivalOutputFiles.FileNamePattern`/`FolderPattern` display string; use them here and in DeparturesViewModel.

<a id="w2-20"></a>

### W2-20 · Logic in GUI · Low · ARTCC Boundaries repeats Core's altitude vocabulary and builds the per-ARTCC file list itself

**Status:** Fixed (not yet merged), with [U1-1](110_GUI-Lag.md#u1-1) in S3: the ARTCCs and their altitudes come from Core's `AiracCycleOptionListBuilder`, which uses `ArtccBoundaryAltitudes`, off the UI thread; the tab no longer walks ARB_SEG · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ArtccBoundariesViewModel.cs:130-157`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ArtccBoundariesViewModel.cs#L130-L157), 367-372

**Problem:** Repeats `ArtccBoundaryAltitudes.TryParse`/`NasrToken` (Domain/ArtccBoundaries/ArtccBoundaryAltitudes.cs) and walks ARB_SEG twice on the UI thread.

**Proposed:** Add `ArtccBoundaryBuilder.LocationAltitudes(NasrCsvDataCollection)` returning ordered (LocationId, NASR token) pairs, using `ArtccBoundaryAltitudes`; derive `Locations` from it.

<a id="w1-8"></a>

### W1-8 · Logic in GUI · Low · Add Custom Aliases keeps its own copy of the parser's and loader's rules, and its doc says they're the same

**Status:** Fixed (not yet merged), as proposed: `AliasSourceRules.Problem` is every row rule - the full path, http or https, not a GitHub page, no secret in the address, no credential sent to plain http - used by the parser and the tab, so a run now refuses the two the tab alone had. `AliasSourceRules.NoAliasCommands` is the loader's message and the tab's. Tests: `AliasSourceRulesTests`, `ConcatenateAliasesSettingsParserTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs:557-597`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs#L557-L597), 599-636, 72-73

**Problem:** `ErrorOf` duplicates `ConcatenateAliasesSettingsParser.ParseSource` (full path, http/https, `UrlSecrets`). It also adds two rules the parser doesn't have (`GitHubFileUrl.IsPageButNotFile`, and a credential only to https unless on GitHub), so the summary's "the same rules" is wrong. `LocalFileErrorOf`/`NoAliasCommandsMessage` duplicates `AliasSourceLoader.Checked`'s "no alias commands" rule (AliasSourceLoader.cs:282-284) with different wording.

**Proposed:** Move the row rules to Core, e.g. `AliasSourceRules.Problem(AliasSourceKind kind, string location, Guid? credentialId)`, and call it from both `ParseSource` and `ErrorOf`. Reuse the loader's message for the no-commands check (see [W1-3](110_GUI-Lag.md#w1-3)). Until then, fix the summary: "Why a row cannot be saved: the parser's rules, plus a GitHub page that isn't a file and a credential sent to a plain-http address; or null."

<a id="w1-9"></a>

### W1-9 · Logic in GUI · Low · Numbered-key lists are parsed by hand in two tabs, beside Core's parsers

**Status:** Fixed (not yet merged), as proposed: `NumberedSettings` (`TrySplit`, `Read`) reads `<Prefix><n>.<Field>` lists - numbers from 1 - for both parsers and both tabs, and the tabs' `SourcesKey`/`VirtualAirlinesKey` copies are gone. Tests: `NumberedSettingsTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs:62`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs#L62), 274-313, 539-555; [`TelephonyViewModel.cs:64`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs#L64), 877-921

**Problem:** Both tabs copy the whole config (`SnapshotValues`) and re-implement the `<Prefix>.<n>.<Field>` grouping that `ConcatenateAliasesSettingsParser.TrySplitSourceKey` and `TelephonySettingsParser.ReadVirtualAirlines` already do. The GUI's version skips the parser's `number < 1` check. `SourcesKey = "Sources"` and `VirtualAirlinesKey = "VirtualAirlines"` repeat Core's `SourcesPrefix`/`VirtualAirlinesPrefix`.

**Proposed:** Add one Core reader (e.g. `Application/Settings/NumberedSettings.Read(IEnumerable<KeyValuePair<string,string>> values, string prefix)` → `SortedDictionary<int, Dictionary<string,string>>`). Use it from both parsers and both tabs, and drop the GUI constants in favour of `SourcesPrefix.TrimEnd('.')` / `VirtualAirlinesPrefix.TrimEnd('.')`, or Core constants without the dot.

<a id="w2-21"></a>

### W2-21 · Logic in GUI · Low · DAT cropping-distance validation copies Core's parse rule

**Status:** Fixed (not yet merged), as proposed: `SettingsValueReader.TryParsePositiveDecimal`, used by `OptionalPositiveDecimal` and the DAT page. Tests: `SettingsValueReaderTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/DatToGeojsonViewModel.cs:85-97`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DatToGeojsonViewModel.cs#L85-L97)

**Problem:** Same rule as `SettingsValueReader.OptionalPositiveDecimal` (Core/Application/Settings/SettingsValueReader.cs:111-127).

**Proposed:** Add `SettingsValueReader.TryParsePositiveDecimal(string, double maximum, out double)` used by both.

<a id="w1-12"></a>

### W1-12 · Logic in GUI · Low · Departures repeats the parser's limits, date format and file layout

**Status:** Fixed (not yet merged), as proposed, with [W2-18](#w2-18): `ProcedureAmendmentFilter` holds the limits (1,000 cycles, 36,500 days) and the date format for both parsers and both tabs; `DepartureOutputFiles.GeojsonFileNamePattern` and `GeojsonFolderPattern` spell the layout one way for What You'll Get and the File Names tab (`<Airport ID>_<Departure ID>`, `<ARTCC ID>\<Airport ID>`). Tests: `ProcedureAmendmentFilterTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/DeparturesViewModel.cs:37-45`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DeparturesViewModel.cs#L37-L45), 133-137

**Problem:** `DepartureSettingsParser` hard-codes the same numbers (DepartureSettingsParser.cs:92, 96: `maximum: 1000`, `maximum: 36500`) and the same `yyyy-MM-dd` format, so the two can drift apart. `FileNamePattern`/`FolderPattern` (What You'll Get) and `OutputFileEntries` (File Names tab) spell the same layout two different ways: `<Airport ID>_<Departure ID>` / `Geojson\<ARTCC ID>\<Airport ID>\` versus `<airport>_<procedure>` / `<ARTCC>\<airport>`. Both re-spell `DepartureOutputFiles.GeojsonDirectory`/`GeojsonFileName`.

**Proposed:** Make the limits and the format public constants in Core (e.g. `DepartureSettingsParser.MaxAmendedWithinCycles`, `MaxAmendedWithinDays`, `AmendedOnOrAfterFormat`, or one shared amendment-filter class that Arrivals uses too). Use them in the parser and the tab. Add a Core `DepartureOutputFiles.FileNamePattern`/`FolderPattern` and use one placeholder spelling in both places.

<a id="w2-25"></a>

### W2-25 · Logic in GUI · Low · ERAM's "would this run delete anything" check parses the settings block in the GUI

**Status:** Fixed (not yet merged), as proposed: `EramToGeojsonService.ExistingOutput(settings)` names the folder a run would empty when it has anything in it, reading the block with `SettingsValueReader`; the page only asks. Tests: `EramToGeojsonServiceTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/EramToGeojsonViewModel.cs:159-165`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/EramToGeojsonViewModel.cs#L159-L165)

**Problem:** Reads raw block keys and repeats the Y/N rule; `EramGeojsonWriter.OutputDirectory(ConversionSettings)` exists in Core.

**Proposed:** Add `EramToGeojsonService.HasExistingOutput(IReadOnlyDictionary<string,string>)` (as `AiracService.HasExistingOutput`) and call it.

<a id="w2-22"></a>

### W2-22 · Logic in GUI · Low · The "no two files share a name" rule is only half in Core

**Status:** Fixed (not yet merged), as proposed: `OutputFileNames.Clashes` checks each new name against every other file's in the same folder (`OutputFileNames.FolderOf`) - renamed too, or keeping FE-Buddy's name - for the File Names tab and for a run, whose parser checks against every file but the GeoJSON; so a settings-block run can no longer write Airways renamed "Airports" over `Airports.txt`, and `Aliases\X.txt` beside the cycle folder's `Duplicate_Alias_Commands.txt` is no clash. Tests: `OutputFileNamesTests`, `FileNamesViewModelTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/FileNamesViewModel.cs:268-280`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/FileNamesViewModel.cs#L268-L280), 300-301

**Problem:** The GUI also rejects a new name equal to an **unrenamed** file's name; Core's `OutputFileNames` only checks renamed against renamed (OutputFileNames.cs:68), so a Harness/settings-block run can overwrite e.g. `Airports.txt` with Airways renamed "Airports". The GUI check also ignores folders, so `Aliases\X.txt` vs the cycle folder's `Duplicate_Alias_Commands.txt` is reported as a clash though nothing collides.

**Proposed:** Move the full rule into Core (`OutputFileNames` checks each new name against every key's default name in the same folder) and have the tab call it.

<a id="w1-10"></a>

### W1-10 · Logic in GUI · Low · Fixes re-implements combination parsing and file naming

**Status:** Fixed (not yet merged), as proposed: Core's `FixCombination.TryParse` reads a `Combinations` entry for both `FixSettingsParser` and the Fixes tab, and the tab names a combination's files with `FixOutputFiles.GroupKey`. The fix use order went with [U1-1](110_GUI-Lag.md#u1-1). Tests: `FixSettingsParserTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/FixesViewModel.cs:973-1007`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/FixesViewModel.cs#L973-L1007), 924, 611-622

**Problem:** `ParseCombinationsFromConfig` duplicates `FixSettingsParser.ParseCombinations` (split on `+`, `FixCharts.Token`, `FixUses.Token`, de-duplicate). `CombinationFileNames` re-spells `FixOutputFiles.GroupKey`. `KnownFixUseOrder` re-implements an index lookup into `FixUses.All`.

**Proposed:** Add `FixCombination.TryParse(string entry, out FixCombination combination)` in Core and use it in both places. Build the label from `FixOutputFiles.GroupKey(group, CrcFeatureKind.Symbol)` and `…Text`. Add `FixUses.DisplayOrder(name)` (or sort in the Core catalogue from [W1-4](110_GUI-Lag.md#w1-4)).

<a id="w2-19"></a>

### W2-19 · Logic in GUI · Low · NAVAIDs repeats NavaidBuilder's SHUTDOWN rule

**Status:** Fixed (not yet merged), with [U1-1](110_GUI-Lag.md#u1-1) in S3: the NAVAID types come from Core's `AiracCycleOptionListBuilder`, which uses `NavaidBuilder.IsShutDown`; the tab's copy is gone · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/NavaidsViewModel.cs:34`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/NavaidsViewModel.cs#L34), 153-159

**Problem:** Same constant and test as NavaidBuilder.cs:25, 59; if Core's rule changes the type list drifts.

**Proposed:** Add `NavaidBuilder.TypesIn(NasrCsvDataCollection)` (or `NavaidBuilder.IsShutdown(row)`) in Core and call it.

<a id="w3-12"></a>

### W3-12 · Logic in GUI · Low · Profile-name "already taken" rule duplicated from Core

**Status:** Fixed (not yet merged), as proposed: Core's `UserConfigFile.NewProfileNameProblem` is the rule, used by `CheckNewName` and by the name windows (`ProfileNameViewModel`, and so the import's). One wording now: "There is already a profile called X." Tests: `UserConfigProfilesTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ProfileNameViewModel.cs:143-155`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ProfileNameViewModel.cs#L143-L155)

**Problem:** Core's private `UserConfigFile.CheckNewName` (UserConfigFile.Profiles.cs:274-289) applies exactly this rule (ProfileNameProblem, then any-case clash except the profile being renamed) with different wording ("There is already a settings profile called 'X'." vs "There is already a profile called X.").

**Proposed:** Expose it from Core, e.g. `public static string? NewProfileNameProblem(string? name, IReadOnlyCollection<string> profiles, string? renaming)`, use it from `CheckNewName` and from `ProfileNameViewModel.ProblemWith` (and so ImportSettingsViewModel.NameProblem/SuggestName).

<a id="w4-6"></a>

### W4-6 · Logic in GUI · Low · Which folder files a conversion reads is decided twice, in the page and in Core

**Status:** Fixed (not yet merged), as proposed: each service counts its own folder (`DatToGeojsonService.CountSourceFiles` and the others), with the extension rule its run uses (`ConversionFiles.HasExtension`) and ERAM's closer look (`EramToGeojsonService.IsSourceFile`, which `Resolve` uses too); the pages call it, off the UI thread, and their `IsSourceFile` is gone. `Extensions` stays on the pages for the file picker. Tests: `ConversionSourceFolderTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/FileConversionTabViewModel.cs:83-96`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/FileConversionTabViewModel.cs#L83-L96), 474-477 (Core: [`Application/Conversions/ConversionFiles.cs:51-60`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/ConversionFiles.cs#L51-L60))

**Problem:** The page re-implements ConversionFiles.Resolve's rule (top directory only, extension match ignoring case, then the service's closer look) so its folder count matches what the run converts; `IsSourceFile` has to be kept in step with each service's `isSource` argument by hand (ERAM's comment says "as its service does").

**Proposed:** Add a public Core method (e.g. `ConversionFiles.FindSourceFiles(folder, extensions, isSource)` used by Resolve, exposed per service as `DatToGeojsonService.CountSourceFiles(folder)` etc.) and have the page call it (off the UI thread, see [W4-2](110_GUI-Lag.md#w4-2)); drop `Extensions`/`IsSourceFile` from the page if nothing else needs them.

<a id="w3-10"></a>

### W3-10 · Logic in GUI · Low · Facility key duplicated as a literal; doc names only one reader

**Status:** Fixed (not yet merged), as proposed, with the key's move ([Q1](010_Decisions.md#q1)): `UserConfigKeys.Facility` (now `General.Facility`) is the one name; `UserConfigAreas.FacilityKey` uses it, `SettingsViewModel.ArtccKey` is gone, and Settings, Procedures, the AIRAC Service, a settings import and the tabs' settings read Core's constant. `UserConfigLayoutTests` covers it through `UserConfigKeys` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:79-84`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L79-L84); [`FeBuddy/FeBuddy.Wpf/ViewModels/ImportSettingsViewModel.cs:252`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ImportSettingsViewModel.cs#L252)

**Problem:** Core already defines the same key as `UserConfigAreas.FacilityKey` (UserConfigAreas.cs:42) and Core tests repeat the literal; the GUI keeps its own copy. The doc says it is internal for the Procedures tab, but AiracServiceViewModel.cs:549 and ImportSettingsViewModel.cs:252 read it too.

**Proposed:** Add `UserConfigKeys.Facility = "Services.AiracService.UserArtccId"` in Core (have `UserConfigAreas.FacilityKey` use it), delete `SettingsViewModel.ArtccKey`, and use the Core constant in SettingsViewModel, ProceduresViewModel, AiracServiceViewModel, ImportSettingsViewModel and UserConfigLayoutTests (the `UserConfigKeys` reflection there then covers it).

<a id="w3-13"></a>

### W3-13 · Logic in GUI · Low · Settings backup before reset/uninstall is file logic in the view-model

**Status:** Fixed (not yet merged), as proposed: `UserConfigFile.CopyProfilesTo(path)` copies the profile in use and every other one beside it, and refuses a path under `AppDataReset.RootDirectory`; Settings keeps the dialog and the toasts. Tests: `UserConfigProfilesTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:1031-1094`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L1031-L1094)

**Problem:** Which files make a full backup, how the other profiles are named beside it, and the "not inside %APPDATA%\FE-Buddy" rule are settings/file-system rules; they can't be used by the Harness or tested without a dialog.

**Proposed:** Move to Core, e.g. `UserConfigFile.CopyProfilesTo(string path)` returning the count and throwing on a path under `AppDataReset.RootDirectory`; the view-model keeps only the dialog and toasts.

<a id="w3-14"></a>

### W3-14 · Logic in GUI · Low · Version-check status is worded by three separate ladders (and a failed check is called "offline")

**Status:** Fixed (not yet merged), as proposed: `VersionCheckResult.Status` (`VersionCheckStatus`) is decided once, and the title bar, System health and Settings' "Check now" switch on it; `DisplayVersion` is the one "dev" rule, used by the update window too. A failed check says "Could not check for updates" ("could not check" in System health) unless FE-Buddy is offline. Tests: `VersionCheckResultTests` · **Session:** [S5](020_Work-Plan.md#s5)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs:604-652`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs#L604-L652), 727-736; [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:655-693`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L655-L693); [`FeBuddy/FeBuddy.Wpf/ViewModels/UpdateWindowViewModel.cs:69`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/UpdateWindowViewModel.cs#L69)

**Problem:** The same VersionCheckResult states (skipped / failed / update / running a pre-release / ahead / latest) are decided in three places, in different orders, plus two different "dev" rules (`CurrentVersion is "0.0.0" or ""` in Shell, `IsNullOrWhiteSpace` in UpdateWindowViewModel). A check that failed while online (GitHub rate limit, HTTP error) is labelled "offline" in the tooltip and health row.

**Proposed:** Add a `VersionCheckStatus` (enum or `Status` property) and `DisplayVersion` to Core's VersionCheckResult and switch on it in all three places; word the failed state as "could not check" with `Message`, and keep "offline" for `!AppEnvironment.HasInternetConnection`.

**Note:** Re-checked after [#377](https://github.com/Nikolai558/FE-BUDDY/pull/377): still applies.

<a id="n1-33"></a>

### N1-33 · Logic in GUI · Low · Departures and Arrivals build their GeoJSON folder themselves instead of using Core's pattern

**Status:** Open; found in the review of the S2 to S5 code at `4893e32` · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/DeparturesViewModel.cs:124-128`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/DeparturesViewModel.cs#L124-L128); [`FeBuddy/FeBuddy.Wpf/ViewModels/ArrivalsViewModel.cs:122-126`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/ArrivalsViewModel.cs#L122-L126); Core: [`FeBuddy/FeBuddy.Core/Application/Airac/Departures/DepartureOutputFiles.cs:55`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Application/Airac/Departures/DepartureOutputFiles.cs#L55), [`FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalOutputFiles.cs:56`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalOutputFiles.cs#L56)

**Problem:** `OutputFileEntries` uses `Path.Combine(FolderOf(file), "<ARTCC ID>", "<Airport ID>")`, a copy of Core's `GeojsonFolderPattern`, which the same view-models already use for What You'll Get. Same result today; a change to Core's pattern would leave the File Names tab showing the wrong folder.

**Proposed:** Use `DepartureOutputFiles.GeojsonFolderPattern` and `ArrivalOutputFiles.GeojsonFolderPattern`. Test: the `Departures_Lines` / `Arrivals_Lines` entry's folder equals the pattern.

## Reported more than once

Each review slice was read by its own reviewer, so some findings were reported twice. These IDs point at the finding that is kept.

- <a id="c3-3"></a>**C3-3** · The tab re-implements Telephony's "first operator per 3LD and telephony" FAA rules, without the expiry rule · same as [W1-7](100_Bugs.md#w1-7).
- <a id="t3-7"></a>**T3-7** · Which virtual airlines clash with a real operator is decided in the view-model · same as [W1-7](100_Bugs.md#w1-7).
- <a id="t3-8"></a>**T3-8** · The FAA's d-TPP posting window (15-18 days) is computed only in the General tab · same as [C7-9](200_Comments-Core.md#c7-9).
- <a id="u1-18"></a>**U1-18** · Procedure Charts re-implements the library's area rule and procedure-name canonicalization · same as [W1-6](#w1-6).
- <a id="u1-19"></a>**U1-19** · Airways run messages are grouped by parsing Core's message text with a regex · same as [W1-11](#w1-11).
- <a id="u1-20"></a>**U1-20** · Telephony tab parses the FAA register and decides which real operators count · same as [W1-7](100_Bugs.md#w1-7).
- <a id="w2-24"></a>**W2-24** · The d-TPP publication window is computed in the GUI · same as [C7-9](200_Comments-Core.md#c7-9).
- <a id="w3-11"></a>**W3-11** · Settings re-implements the precision and pretty-print reading rules · same as [W7-6](#w7-6).
- <a id="w3-17"></a>**W3-17** · The facility list is a NASR rule built in the GUI · same as [U1-13](110_GUI-Lag.md#u1-13).
- <a id="w6-2"></a>**W6-2** · AiracMapLayers holds airport, NAVAID and boundary rules that belong in Core · same as [W3-9](#w3-9).
- <a id="w6-4"></a>**W6-4** · GeoJsonReader and most map models are WPF-free parsing and geometry code · same as [W3-9](#w3-9).
