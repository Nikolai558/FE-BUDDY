# 140 · Unused and test-only code (app code)

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

App code nothing uses, and app code only the tests use (unused test helpers are in [300_Unit-Tests.md](300_Unit-Tests.md)). The NASR models and parsers are not listed: they stay even where unused. [Q5](010_Decisions.md#q5) asks what to do with test-only code. Session [S6](020_Work-Plan.md#s6).

**78 findings** (3 Med, 75 Low). Quote the ID when you comment on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374).

## Summary

| ID | Kind | Sev | Finding | Status | Session |
|---|---|---|---|---|---|
| [C1-6](#c1-6) | Test-only | Med | Launch progress and LaunchResult exist only for tests | Fixed | S6 |
| [C5-2](#c5-2) | Test-only | Med | Location's DMS half and GeoMath's DMS helpers are used only by tests, and they cost a string format for every Location | Fixed | S6 |
| [W5-1](#w5-1) | Unused | Med | ShellWindow maps AirwaysViewModel to AirwaysView, but the shell never shows it | Fixed | S6 |
| [C1-8](#c1-8) | Test-only | Low | AiracCycleDataCache.PrepareCyclesAsync(previous, current, next, ct) is used only by tests | Fixed | S6 |
| [C1-7](#c1-7) | Unused | Low | AiracSharedDataLoader.LoadTelephonyAsync(CancellationToken) has no caller; its 3-arg internal twin is test-only | Fixed | S6 |
| [C2-26](#c2-26) | Test-only | Low | Model members filled by these builders but read only by tests | Fixed | S6 |
| [C2-27](#c2-27) | Test-only | Low | AirwayWaypointBuffer.Buffer's default radii are used only by tests | Fixed | S6 |
| [C3-20](#c3-20) | Test-only | Low | `ArtccBoundaryOutputFiles.ClassFor(string group)` is an identity function only its test calls | Fixed | S6 |
| [C2-24](#c2-24) | Unused | Low | Departures/Arrivals advisory condition is always true | Fixed | S6 |
| [C2-28](#c2-28) | Test-only | Low | Result counts read only by the Harness and tests | Kept | S6 |
| [C1-10](#c1-10) | Test-only | Low | DuplicateAliasReportResult.LineCount is read only by a test | Fixed | S6 |
| [C1-9](#c1-9) | Test-only | Low | OutputFileNames.NewNames is read only by tests | Fixed | S6 |
| [C3-19](#c3-19) | Unused | Low | `NavaidAliasGenerateResult.Messages` is always empty | Fixed | S6 |
| [C4-20](#c4-20) | Test-only | Low | ChartRecallAliasLine.AirportId and ChartCode | Fixed | S6 |
| [T1a-37](#t1a-37) | Unused | Low | Procedures builder and writer `Messages` lists are always empty | Fixed | S6 |
| [C4-19](#c4-19) | Test-only | Low | ProcedureRouteSet.Routes | Fixed | S6 |
| [C4-18](#c4-18) | Test-only | Low | RoutePointArtccResolver.AmbiguousPoints and its list | Fixed | S6 |
| [C3-21](#c3-21) | Test-only | Low | Result counters only the Harness and tests read | Fixed | S6 |
| [C4-21](#c4-21) | Unused | Low | AliasPractice.js exposes window.feBuddyPractice, which nothing reads | Fixed | S6 |
| [C5-21](#c5-21) | Test-only | Low | Small result fields, an out parameter and two defaults that only tests use | Fixed | S6 |
| [C1-13](#c1-13) | Test-only | Low | ServiceResult.Warnings is used only by the Harness and tests | Kept | S6 |
| [C1-12](#c1-12) | Test-only | Low | NewsCheckResult.FromNetwork is read only by tests | Fixed | S6 |
| [T1b-21](#t1b-21) | Test-only | Low | ReadArea's no-`Area` fallback is reached only by tests | Fixed | S6 |
| [C1-11](#c1-11) | Test-only | Low | VersionCheckResult.ParseChannel(string?) (one-argument overload) is used only by a test | Fixed | S6 |
| [C5-6](#c5-6) | Unused | Low | Airway.Segments and AirwayPoint.FoundIn are written but never read; Airway.AwyLocation and Airway.MaxAuthAlt are read only by a test | Fixed | S6 |
| [C6-28](#c6-28) | Test-only | Low | UserConfigPackage.ConfigVersion (and its default) and UserConfigMigrationResult.FromVersion are read only by tests | Fixed | S6 |
| [C6-25](#c6-25) | Test-only | Low | UserConfigFile.ProfileExists | Fixed | S6 |
| [C6-4](#c6-4) | Unused | Low | `v\d+` in LegacyBackupName matches a file no release ever wrote | Fixed | S6 |
| [C6-27](#c6-27) | Test-only | Low | UserConfigMigrationContext.Move, ChangeValue and Values are used only by tests | Kept | S6 |
| [C5-5](#c5-5) | Test-only | Low | EramProperties' custom equality serves only tests; its remark claims a grouping that doesn't happen | Fixed | S6 |
| [C6-26](#c6-26) | Test-only | Low | AppLog.GetLogFilePath is used only by tests and duplicates the writer's own path logic | Fixed | S6 |
| [C7-18](#c7-18) | Test-only | Low | `MarkdownParser.ParseInlines`'s `issueUrlBase` and `linkBase` parameters are used only by tests | Fixed | S6 |
| [C7-21](#c7-21) | Unused | Low | Markdown features no current content uses (recommend keeping them) | Kept | S6 |
| [C7-8](#c7-8) | Unused | Low | `AiracCycleAvailability.ProbeAsync`'s try/catch can never catch; cancellation is logged as a probe error | Fixed | S6 |
| [C7-16](#c7-16) | Test-only | Low | `InstalledProduct.ProductSemVer` is read only by tests | Fixed | S6 |
| [C7-17](#c7-17) | Test-only | Low | `TelephonyHtmlParser.Parse(registerPath, specialCallSignsPath)` is used only by tests | Fixed | S6 |
| [C7-15](#c7-15) | Test-only | Low | Row-model fields that production code never reads (Wx stations, d-TPP) | Fixed | S6 |
| [W6-25](#w6-25) | Unused | Low | AliasGuideDocumentView.Divider always gets the same margin | Fixed | S6 |
| [W6-20](#w6-20) | Unused | Low | BesideOrBelow.HorizontalSpacing is never set | Fixed | S6 |
| [W6-21](#w6-21) | Unused | Low | CommandTablePanel's four spacing dependency properties are set only by a test, to their defaults | Fixed | S6 |
| [W6-22](#w6-22) | Test-only | Low | CommandTablePanel.Rules exists only for the tests | Kept | S6 |
| [W6-19](#w6-19) | Unused | Low | FilterPicker.Minimum and Maximum are never set | Fixed | S6 |
| [W6-23](#w6-23) | Unused | Low | StringToVisibilityConverter's "Invert" parameter is never used | Fixed | S6 |
| [W6-24](#w6-24) | Unused | Low | GeoJsonReader's InvalidOperationException catch can't be reached | Fixed | S6 |
| [W7-27](#w7-27) | Unused | Low | The `--devmode` spelling of the dev-launch argument has no user | Fixed | S6 |
| [W7-15](#w7-15) | Unused | Low | The implicit Calendar and ComboBoxItem styles apply to nothing | Fixed | S6 |
| [W7-14](#w7-14) | Unused | Low | Unused `ctl` namespace in Typography.xaml (also linked into the installer) | Fixed | S6 |
| [W2-15](#w2-15) | Test-only | Low | `AiracGeneralTabViewModel.IsIncluded(string)` is used only by tests | Fixed | S6 |
| [W2-17](#w2-17) | Unused | Low | General tab's `IsReady` setter re-validates a tab that has nothing to validate | Fixed | S6 |
| [W2-16](#w2-16) | Unused | Low | Public members and change notifications nothing binds to | Fixed | S6 |
| [W2-14](#w2-14) | Unused | Low | `SubServiceDescriptor.Order` duplicates the list order | Fixed | S6 |
| [W3-18](#w3-18) | Unused | Low | DashboardViewModel.DiscordUrl is never bound | Fixed | S6 |
| [W3-23](#w3-23) | Unused | Low | Public members only their own class reads | Fixed | S6 |
| [W3-21](#w3-21) | Unused | Low | Unreachable catch in MapLayersState.CurrentCycleId | Fixed | S6 |
| [W4-21](#w4-21) | Test-only | Low | AliasSourceRow.CheckFailed is read only by a test | Fixed | S6 |
| [W1-16](#w1-16) | Unused | Low | Procedure Charts and Telephony have unbound properties and raises nothing listens to | Fixed | S6 |
| [W1-17](#w1-17) | Test-only | Low | DtppPreviousStatus is read only by a test, and two of its three messages are never shown | Fixed | S6 |
| [W4-19](#w4-19) | Unused | Low | ConversionTabViewModel is an extra layer with exactly one subclass | Deferred | After 3.0 |
| [W4-12](#w4-12) | Unused | Low | Unused members on the conversion page base: HasSourceFiles, EmitLines, and the Emit* no-op setters | Fixed | S6 |
| [D1a-17](#d1a-17) | Unused | Low | Procedure Charts and Telephony save four settings nothing reads, and send four the parser ignores | Kept | S6 |
| [W4-13](#w4-13) | Unused | Low | Duplicate properties on GeojsonSubServiceViewModel: WritesAliasFile and HasGeojsonFiles | Fixed | S6 |
| [W4-14](#w4-14) | Unused | Low | Virtual members nobody overrides: GenerateGeojson, IsAreaInUse | Fixed | S6 |
| [W4-22](#w4-22) | Test-only | Low | GeojsonSubServiceViewModel.EveryOutput is only ever used by tabs built in tests | Kept | S6 |
| [W4-20](#w4-20) | Unused | Low | Card interfaces with a single implementer, used only for design-time DataContext | Deferred | After 3.0 |
| [W4-17](#w4-17) | Unused | Low | ISubServiceOutputs.IsIncluded is never read through the interface | Fixed | S6 |
| [W4-9](#w4-9) | Unused | Low | ServicePreviewSection.Note is never set | Fixed | S6 |
| [W4-10](#w4-10) | Unused | Low | SubServiceDescriptor.AliasFileName is set for 7 sub-services and never read | Fixed | S6 |
| [W4-15](#w4-15) | Unused | Low | ServiceTabViewModel's base MarkDirty/ClearDirty never run, and the remark describes behaviour no tab has | Fixed | S6 |
| [W4-16](#w4-16) | Unused | Low | ServiceTabViewModel.IsRunnable only feeds a count that IncludedRunTargets already gives; its doc claims more | Fixed | S6 |
| [W4-11](#w4-11) | Unused | Low | ServiceValidation.HasErrors is unused | Fixed | S6 |
| [W4-18](#w4-18) | Unused | Low | TabbedServiceViewModel's optional parts have one user, which supplies all of them | Deferred | After 3.0 |
| [W3-19](#w3-19) | Unused | Low | SettingsViewModel.IsOnline is never bound or read | Fixed | S6 |
| [W3-22](#w3-22) | Unused | Low | Redundant "no entries" guards before ComputeReadiness | Fixed | S6 |
| [W3-20](#w3-20) | Unused | Low | ShellViewModel.VersionBrushKey and IsOnline are bindable properties nothing binds | Fixed | S6 |
| [W5-17](#w5-17) | Unused | Low | Six x:Name elements are never referenced | Fixed | S6 |
| [W5-18](#w5-18) | Unused | Low | GeojsonFilesCard: Footnote and the three *Label properties are never set | Fixed | S6 |
| [W7-16](#w7-16) | Unused | Low | Five explicit `Themed.ComboBox` references repeat what the implicit style already does | Fixed | S6 |
| [W5-19](#w5-19) | Unused | Low | Dashboard log row trigger for Info sets the default again | Fixed | S6 |

## Med

<a id="c1-6"></a>

### C1-6 · Test-only · Med · Launch progress and LaunchResult exist only for tests

**Status:** Fixed (not yet merged). The launch progress types, `LaunchResult` and the normal-launch `RunAsync` overload are deleted; `RunAsync` returns `Task`, and LaunchSequenceTests assert on `AppEnvironment` and the log · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Launch/Models/LaunchProgress.cs:1-74`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Launch/Models/LaunchProgress.cs#L1-L74); [`FeBuddy/FeBuddy.Core/Application/Launch/Models/LaunchResult.cs:1-12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Launch/Models/LaunchResult.cs#L1-L12); [`FeBuddy/FeBuddy.Core/Application/Launch/LaunchSequence.cs:55-67`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Launch/LaunchSequence.cs#L55-L67) (3-arg `RunAsync`), 60, 75, 297-350

**Problem:** The GUI never passes an `IProgress<LaunchProgress>` and never reads the `LaunchResult`; `LaunchStep`, `LaunchStepStatus`, `LaunchProgress`, `LaunchResult` and the `RunAsync(string, IProgress?, CancellationToken)` overload are used only by LaunchSequence itself and LaunchSequenceTests.cs (20 references). The shell follows launch through `AppEnvironment.Changed` and the cache's `StateChanged` instead. Docs that claim a consumer are wrong: "for the shell" (LaunchSequence.cs:60, 75) and `LaunchStepStatus.Skipped` "(e.g. offline)" (LaunchProgress.cs:64) - only a dev launch ever skips a step; offline steps report Succeeded.

**Proposed:** Either (a) delete `LaunchProgress.cs`, `LaunchResult.cs`, the 3-arg `RunAsync`, the `progress` parameters and `RunStep`'s reporting, return `Task`, and rewrite LaunchSequenceTests to assert on `AppEnvironment` and the log; or (b) keep them as the test seam but say so ("for tests and any future splash screen") and fix the two docs ("for the caller"; Skipped: "a dev launch left the step out").

**Future use:** possible (a launch splash/progress UI), but nothing is planned in docs.

<a id="c5-2"></a>

### C5-2 · Test-only · Med · Location's DMS half and GeoMath's DMS helpers are used only by tests, and they cost a string format for every Location

**Status:** Fixed (not yet merged). `Location` is a plain record of decimal degrees with the range check; the DMS constructor and properties, the custom equality, GeoMath's DMS helpers and their tests are deleted · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Geo/Models/Location.cs:3-31`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Geo/Models/Location.cs#L3-L31),48-68,77,88,92-104; [`FeBuddy/FeBuddy.Core/Domain/Geo/GeoMath.cs:17-21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Geo/GeoMath.cs#L17-L21),31,40-91,213-233

**Problem:** (a) No production code builds a Location from DMS or reads `DmsLat`/`DmsLon`/`Equals`. Every production `new Location` is `(double, double)`: DatFileReader.cs:86, GeoMath.cs:206, AntimeridianSplitter.cs:49-50, AirwayWaypointBuffer.cs:85-108. SctFileReader parses DMS with its own `TryParseDms`. So `Location(string,string)`, `DmsLat`, `DmsLon`, the custom `Equals`/`GetHashCode`, `GeoMath.IsValidDms`/`IsValidDmsPart`/`ToDms`/`ToDecimal`/`MillisecondsPerDegree` and the DMS paragraph of GeoMath's remarks exist only for LocationTests/GeoMathTests. The remark that FE-Buddy reads and writes DMS formats through Location is wrong. (b) Every `Location(double,double)` still formats two DMS strings. AntimeridianSplitter builds two Locations per segment of every line FE-Buddy writes: airways, ARTCC boundaries, and every DAT, SCT and ERAM line through SegmentJoiner. An ERAM Geomaps file of hundreds of MB therefore formats millions of throwaway strings. (c) There is a bug on the test-only path. `IsValidDmsPart` upper-cases the hemisphere (GeoMath.cs:220), but `ToDecimal` tests `dms[0] is 'S' or 'W'` with case (GeoMath.cs:85), so `new Location("s043.31.08.418", "w112.03.50.103")` gives +43.5/+112.06. It also accepts a negative milliseconds part (`int.TryParse("-1")`, line 232) and `N090.30.00.000` (90.5°).

**Proposed:** Make Location a plain value with the range check, e.g. `public sealed record Location(double DecLat, double DecLon)` that validates in its constructor. Delete the DMS constructor, `DmsLat`, `DmsLon`, the custom Equals/GetHashCode, GeoMath `IsValidDms`, `IsValidDmsPart`, `ToDms`, `ToDecimal`, `MillisecondsPerDegree`, and the DMS sentence in GeoMath's summary and remarks. Also delete their tests: the LocationTests DMS cases and the GeoMathTests IsValidDms/ToDms/ToDecimal theories. If DMS is kept, compute it lazily in the getter and fix the hemisphere case check.

**Future use:** possible. A VRC sector-file writer would need `N040.38.23.000` text, but it can be added back then.

**Note:** Also found as [T1c-6](300_Unit-Tests.md#t1c-6).

<a id="w5-1"></a>

### W5-1 · Unused · Med · ShellWindow maps AirwaysViewModel to AirwaysView, but the shell never shows it

**Status:** Fixed (not yet merged). The AirwaysViewModel DataTemplate is deleted from ShellWindow.xaml · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/ShellWindow.xaml:54-56`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ShellWindow.xaml#L54-L56)

**Problem:** `ShellViewModel.Current` is only ever set from a nav item's view-model (`ShellViewModel.cs:866`). The nav items are Dashboard, AIRAC Service, File Conversions, Map, Settings and Info (`ShellViewModel.cs:83-98`). Airways is a tab inside `TabbedServiceView`, which has its own template (`TabbedServiceView.xaml:39-41`). This is left over from when Airways was a screen of its own, and Code-Structure.md says a sub-service is never a screen of its own.

**Proposed:** delete lines 54-56.

**Future use:** unlikely; it goes against the documented design.

## Low

<a id="c1-8"></a>

### C1-8 · Test-only · Low · AiracCycleDataCache.PrepareCyclesAsync(previous, current, next, ct) is used only by tests

**Status:** Fixed (not yet merged). The overload without a plan is deleted; the tests pass `AiracLaunchPlan.Everything` · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs:231-245`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs#L231-L245)

**Problem:** LaunchSequence.cs:268 always passes a plan. The plan-less overload has ~45 test callers (AiracCycleDataCacheTests, LaunchSequenceTests, AiracGeneralTabViewModelTests) and no production caller.

**Proposed:** Keep it but mark it "A normal launch's plan; tests use it." - or delete it and add `AiracLaunchPlan.Everything` at the test call sites (or a test-side extension method).

**Future use:** unlikely.

<a id="c1-7"></a>

### C1-7 · Unused · Low · AiracSharedDataLoader.LoadTelephonyAsync(CancellationToken) has no caller; its 3-arg internal twin is test-only

**Status:** Fixed (not yet merged). Both overloads are deleted; the tests call the internal one with `refreshVirtualAirlineList: null` · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/AiracSharedDataLoader.cs:68-72`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/AiracSharedDataLoader.cs#L68-L72), 113-121

**Problem:** Production calls only `LoadTelephonyAsync(bool, CancellationToken)` (AiracService.cs:156). The public 1-arg overload is called by nothing (not tests, not the Harness). The internal `LoadTelephonyAsync(refresh, nowUtc, ct)` is used by one test (AiracSharedDataLoaderTests.cs:170); the other tests already call the 4-arg overload.

**Proposed:** Delete both overloads; change AiracSharedDataLoaderTests.cs:170 to pass `refreshVirtualAirlineList: null`; change the 4-arg overload's summary cref (line 124) and the deleted 3-arg one's cref to `LoadTelephonyAsync(bool, CancellationToken)` (a cref to the deleted overload would break the doc build).

**Future use:** unlikely; the bool overload covers it.

**Note:** Also found as [T1b-25](#t1b-25).

<a id="c2-26"></a>

### C2-26 · Test-only · Low · Model members filled by these builders but read only by tests

**Status:** Fixed (not yet merged). `Airway.AwyLocation`, `MaxAuthAlt` and `Segments`, both raw routes' `TransitionCode`, and `CodeIdFor`'s `usedFallback` are deleted, with the asserts that read them · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayBuilder.cs:171-174`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayBuilder.cs#L171-L174) (`Airway.AwyLocation`, `MaxAuthAlt`, `Segments`); [`ArrivalBuilder.cs:487`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalBuilder.cs#L487) / [`DepartureBuilder.cs:370`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Departures/DepartureBuilder.cs#L370) (`ArrivalRawRoute.TransitionCode`, `DepartureRawRoute.TransitionCode`); [`ArrivalBuilder.cs:117`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalBuilder.cs#L117) / [`DepartureBuilder.cs:85`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Departures/DepartureBuilder.cs#L85) (`CodeIdFor(..., out _)`). The members are declared in Domain.

**Problem:** No production code reads these. `TransitionCode` serves only as a grouping key inside ReadRoutes, which uses the local tuple and not the property. Both production callers of `CodeIdFor` discard `usedFallback`. Only AirwayBuilderTests, Departure/ArrivalBuilderTests and Departure/ArrivalNamingTests read them.

**Proposed:** drop `Airway.AwyLocation`/`MaxAuthAlt`/`Segments` (adjust AirwayBuilderTests:54-56), drop `TransitionCode` from both raw-route records (adjust the two builder tests), and drop `out bool usedFallback` from both `CodeIdFor`s (tests assert the result instead). Or keep any the owner wants for the planned features.

**Future use:** AwyLocation/MaxAuthAlt plausible for a later "feb.*" property; usedFallback unlikely.

**Note:** See also [C5-6](#c5-6) and [C5-21](#c5-21).

<a id="c2-27"></a>

### C2-27 · Test-only · Low · AirwayWaypointBuffer.Buffer's default radii are used only by tests

**Status:** Fixed (not yet merged). Both radii are required; the tests pass the default radii · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayWaypointBuffer.cs:60-61`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayWaypointBuffer.cs#L60-L61)

**Problem:** The only production caller (AirwayBuilder.cs:151) always passes both. The defaults let a call skip the settings, which is how [C2-3](100_Bugs.md#c2-3)'s alias-only buffering would go unnoticed.

**Proposed:** make both parameters required; pass `AirwayWaypointBuffer.DefaultFixRadiusNm, AirwayWaypointBuffer.DefaultNavaidRadiusNm` in AirwayWaypointBufferTests (lines 38, 58, 76, 93, 142) and AirwayMessageLevelsTests:47.

**Future use:** n/a.

<a id="c3-20"></a>

### C3-20 · Test-only · Low · `ArtccBoundaryOutputFiles.ClassFor(string group)` is an identity function only its test calls

**Status:** Fixed (not yet merged). `ClassFor(string)` and its test are deleted · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/ArtccBoundaries/ArtccBoundaryOutputFiles.cs:50-53`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/ArtccBoundaries/ArtccBoundaryOutputFiles.cs#L50-L53) ; [`FeBuddy/FeBuddy.UnitTests/Application/Airac/ArtccBoundaries/ArtccBoundaryOutputFilesTests.cs:25`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/ArtccBoundaries/ArtccBoundaryOutputFilesTests.cs#L25)

**Problem:** Production uses only the two-argument overload (ArtccBoundaryGeojsonWriter.cs:87, ArtccBoundariesViewModel.cs:342); the remarks (line 20) already say the group name doubles as the class name.

**Proposed:** Delete `ClassFor(string)` and the assertion at ArtccBoundaryOutputFilesTests.cs:25.

**Future use:** unlikely.

**Note:** Also found as [T1b-24](#t1b-24).

<a id="c2-24"></a>

### C2-24 · Unused · Low · Departures/Arrivals advisory condition is always true

**Status:** Fixed (not yet merged). The check is `output.Count == 0` · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Departures/DepartureService.cs:76`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Departures/DepartureService.cs#L76); [`FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalService.cs:76`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalService.cs#L76)

**Problem:** Both parsers throw when both outputs are `N` (DepartureSettingsParser.cs:61-66, ArrivalSettingsParser.cs:61-66), so the second clause is always true.

**Proposed:** `if (output.Count == 0)`.

**Future use:** unlikely.

<a id="c2-28"></a>

### C2-28 · Test-only · Low · Result counts read only by the Harness and tests

**Status:** Kept (not yet merged): the Harness's report prints them, and their docs now say they are for the Harness's report and tests · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Departures/Models/DepartureResults.cs:40-44`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Departures/Models/DepartureResults.cs#L40-L44), 56; [`FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/Models/ArrivalResults.cs:40-44`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/Models/ArrivalResults.cs#L40-L44), 56; [`Airports/Models/AirportResults.cs:41`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airports/Models/AirportResults.cs#L41); [`Airways/Models/AirwayServiceResult.cs:18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/Models/AirwayServiceResult.cs#L18)

**Problem:** Departures/Arrivals `ProcedureCount` and `ProceduresInScopeCount`, and every sub-service's `GeojsonFeatureCountsByFile`, are read only by FeBuddy.Harness/ConsoleReport.cs and unit tests. The GUI's Review tab and AiracService never use them.

**Proposed:** keep them if the Harness report is wanted; otherwise drop them from the results and ConsoleReport.

**Future use:** likely (Harness diagnostics; a richer Review tab).

<a id="c1-10"></a>

### C1-10 · Test-only · Low · DuplicateAliasReportResult.LineCount is read only by a test

**Status:** Fixed (not yet merged). `LineCount` is deleted; the test counts the duplicate's lines itself · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Models/DuplicateAliasModels.cs:115-116`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Models/DuplicateAliasModels.cs#L115-L116)

**Problem:** Only DuplicateAliasReportTests.cs:285 reads it. The `LineCount` the review window binds (DuplicateAliasReviewWindow.xaml:42) is `DuplicateAliasGroupItem.LineCount`, a different property; AiracServiceViewModel uses `Duplicates.Count` only.

**Proposed:** Delete it (and the assertion at DuplicateAliasReportTests.cs:285, or compute the sum in the test).

**Future use:** unlikely.

<a id="c1-9"></a>

### C1-9 · Test-only · Low · OutputFileNames.NewNames is read only by tests

**Status:** Fixed (not yet merged). `NewNames` is deleted; the tests check `FileName(key)` · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Models/OutputFileNames.cs:79-80`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Models/OutputFileNames.cs#L79-L80)

**Problem:** No production code reads it (the class uses `_newNames` directly; the GUI builds its own block). Six test references.

**Proposed:** Make it `internal`, or delete it and have the tests assert through `FileName(key)`.

**Future use:** unlikely.

<a id="c3-19"></a>

### C3-19 · Unused · Low · `NavaidAliasGenerateResult.Messages` is always empty

**Status:** Fixed (not yet merged). `Messages` is gone from the result, the writer and NavaidService · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Navaids/Models/NavaidResults.cs:28-35`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Navaids/Models/NavaidResults.cs#L28-L35) ; [`FeBuddy/FeBuddy.Core/Application/Airac/Navaids/NavaidAliasWriter.cs:57`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Navaids/NavaidAliasWriter.cs#L57),61,104 ; [`FeBuddy/FeBuddy.Core/Application/Airac/Navaids/NavaidService.cs:66-69`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Navaids/NavaidService.cs#L66-L69)

**Problem:** Nothing is ever added; the Airways and Telephony alias results have no `Messages` at all.

**Proposed:** Remove the `Messages` parameter, the local list and NavaidService's `AddRange` (lines 66-69).

**Future use:** unlikely.

<a id="c4-20"></a>

### C4-20 · Test-only · Low · ChartRecallAliasLine.AirportId and ChartCode

**Status:** Fixed (not yet merged). Both are deleted from `ChartRecallAliasLine`, with the test asserts · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/Models/ChartRecallResults.cs:10-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/Models/ChartRecallResults.cs#L10-L11), 15

**Problem:** Production uses only `Text`, which reads Command, Url and Comment (ChartRecallAliasWriter.cs:50). `AirportId` and `ChartCode` are read only by ChartRecallAliasBuilderTests.cs:53-54.

**Proposed:** drop both parameters (and their arguments at ChartRecallAliasBuilder.cs:172), or keep them if a per-airport or per-type split is planned.

**Future use:** possible (for example, splitting the chart recall file by ARTCC), but nothing planned is visible.

<a id="t1a-37"></a>

### T1a-37 · Unused · Low · Procedures builder and writer `Messages` lists are always empty

**Status:** Fixed (not yet merged). `ProcedureBuildResult` loses `Messages`; the two writers return the path written, and their result types and the three `AddRange` calls are gone · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureBuilder.cs:57`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureBuilder.cs#L57), 114; [`ProcedureChangesMarkdownWriter.cs:109`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureChangesMarkdownWriter.cs#L109); [`ProceduresJsonWriter.cs:81`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProceduresJsonWriter.cs#L81); tests asserting it: [`ProcedureChangesMarkdownWriterTests.cs:62`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureChangesMarkdownWriterTests.cs#L62), [`ProceduresJsonWriterTests.cs:69`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProceduresJsonWriterTests.cs#L69)

**Problem:** Nothing is ever added. ProcedureService.cs:146, 211, 222 AddRange empty lists, and the two tests assert `Empty(result.Messages)`.

**Proposed:** Drop `Messages` from ProcedureBuildResult, ProcedureChangesWriteResult and ProceduresJsonWriteResult (or the whole result type when only the path is left), the three AddRange calls, and the two `Assert.Empty(result.Messages)` lines.

**Future use:** unlikely.

<a id="c4-19"></a>

### C4-19 · Test-only · Low · ProcedureRouteSet.Routes

**Status:** Fixed (not yet merged). `Routes` is deleted; the test takes a route through `At` · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureRouteSet.cs:19-20`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureRouteSet.cs#L19-L20)

**Problem:** Production reads only `At(...)` and `Points`. `Routes` is read only by ProceduresWithFixInFacilityTests.cs:190 (`New.Routes[0]`).

**Proposed:** delete the property; the test can keep its own `ProcedureRoute`.

**Future use:** unlikely; the CIFP source would also go through `At`.

**Note:** Also found as [T1a-34](#t1a-34).

<a id="c4-18"></a>

### C4-18 · Test-only · Low · RoutePointArtccResolver.AmbiguousPoints and its list

**Status:** Fixed (not yet merged). `AmbiguousPoints` and its list are deleted; the tests assert on the log line each ambiguous point already writes · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/RoutePointArtccResolver.cs:40`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/RoutePointArtccResolver.cs#L40), 78-79, 121

**Problem:** Nothing in Core, Wpf or the Harness reads it; only RoutePointArtccResolverTests does (lines 83, 97, 114, 131). Production already logs each ambiguous point (lines 122-124).

**Proposed:** delete `_ambiguous`, `AmbiguousPoints` and line 121, and have the tests assert the result of `IsInFacility` (or keep it if the owner wants it for a future run message).

**Future use:** possible (a run message listing ambiguous route points), but the log covers it today.

**Note:** Also found as [T1a-33](#t1a-33).

<a id="c3-21"></a>

### C3-21 · Test-only · Low · Result counters only the Harness and tests read

**Status:** Fixed (not yet merged). `DuplicateCommandCount` is deleted (the advisory carries the count); the counters the Harness prints stay, and their docs say so · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Telephony/Models/TelephonyResults.cs:59-66`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/Models/TelephonyResults.cs#L59-L66) ; [`FeBuddy/FeBuddy.Core/Application/Airac/WxStations/Models/WxStationResults.cs:34-35`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/WxStations/Models/WxStationResults.cs#L34-L35) ; [`FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/Models/ConcatenateAliasesResults.cs:79-84`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/Models/ConcatenateAliasesResults.cs#L79-L84) ; [`ArtccBoundaryResults.cs:37-38`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/ArtccBoundaries/Models/ArtccBoundaryResults.cs#L37-L38), [`FixResults.cs:39-40`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Fixes/Models/FixResults.cs#L39-L40), [`NavaidResults.cs:56-57`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Navaids/Models/NavaidResults.cs#L56-L57), [`WxStationResults.cs:43-44`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/WxStations/Models/WxStationResults.cs#L43-L44) (`GeojsonFeatureCountsByFile`)

**Problem:** Set by the services but read only by FeBuddy.Harness/ConsoleReport.cs and tests (`DuplicateCommandCount`: tests only). The app's Review tab never shows them.

**Proposed:** Keep for the Harness if wanted (say so in their docs), otherwise drop them; `DuplicateCommandCount` can go (the advisory already carries the count).

**Future use:** likely for the Harness counters; unlikely for `DuplicateCommandCount`.

<a id="c4-21"></a>

### C4-21 · Unused · Low · AliasPractice.js exposes window.feBuddyPractice, which nothing reads

**Status:** Fixed (not yet merged). The line is deleted · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/AliasGuide/AliasPractice.js:477`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/AliasGuide/AliasPractice.js#L477)

**Problem:** No page script, test or doc uses it. The tests only search the script text for `function diagnose(input, q)` (AliasPracticeWriterTests.cs:131) and never run it. It puts a global on every facility's exported page.

**Proposed:** delete the line.

**Future use:** unlikely (a console debugging hook).

<a id="c5-21"></a>

### C5-21 · Test-only · Low · Small result fields, an out parameter and two defaults that only tests use

**Status:** Fixed (not yet merged). `LinesRead` and both `RecordsSkipped` are deleted, `CodeIdFor` has no `usedFallback`, `GetCycle` needs a date, and `GeoMath.Distance` always rounds to 6 places · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Conversions/DatToGeojson/Models/DatFileConversion.cs:15-16`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/DatToGeojson/Models/DatFileConversion.cs#L15-L16),21-22; [`FeBuddy/FeBuddy.Core/Application/Conversions/Models/SourceFileConversion.cs:21-22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/Models/SourceFileConversion.cs#L21-L22); [`FeBuddy/FeBuddy.Core/Domain/Arrivals/ArrivalNaming.cs:71-73`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Arrivals/ArrivalNaming.cs#L71-L73); [`FeBuddy/FeBuddy.Core/Domain/Departures/DepartureNaming.cs:70-72`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Departures/DepartureNaming.cs#L70-L72); [`FeBuddy/FeBuddy.Core/Domain/Airac/AiracCycleResolver.cs:33-40`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Airac/AiracCycleResolver.cs#L33-L40); [`FeBuddy/FeBuddy.Core/Domain/Geo/GeoMath.cs:96-99`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Geo/GeoMath.cs#L96-L99)

**Problem:** `LinesRead` and both `RecordsSkipped` are read only by the conversion service tests. The problems are already messages, and FileConversionTabViewModel.cs:294-309,436 shows only counts, failures and FeaturesWritten. Both `CodeIdFor` callers pass `out _` (DepartureBuilder.cs:85, ArrivalBuilder.cs:117). Every production `GetCycle` passes a date (AppEnvironment.cs:57, AiracService.cs:177, AiracServiceViewModel.cs:480), so the machine-clock default, the only clock read in Domain, serves AiracCycleResolverTests.cs:140,143 and AppEnvironmentTests.cs:36. Every production `GeoMath.Distance` passes `round: false`, so the whole-mile default serves only GeoMathTests.

**Proposed:** Delete `LinesRead` and the two `RecordsSkipped` (and their asserts). Drop `usedFallback` (tests can compare against the cleaned name). Make `asOfUtc` required (`DateOnly asOfUtc`). Remove `round` and always round to 6 places, or default it to false.

**Future use:** unlikely; RecordsSkipped could feed a "N records skipped" summary line.

<a id="c1-13"></a>

### C1-13 · Test-only · Low · ServiceResult.Warnings is used only by the Harness and tests

**Status:** Kept (not yet merged): it is the Harness's report, and its doc now says it is for the Harness's report and tests · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Models/ServiceResult.cs:30-38`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Models/ServiceResult.cs#L30-L38)

**Problem:** The GUI is level-aware and never reads it; FeBuddy.Harness/ConsoleReport.cs:64 and ~32 test references do.

**Proposed:** Keep (it is the Harness's report) but say so in its summary ("for the Harness's console report and tests"), or move the projection into ConsoleReport and the tests.

**Future use:** n/a (Harness).

**Note:** Also found as [O1-18](#o1-18).

<a id="c1-12"></a>

### C1-12 · Test-only · Low · NewsCheckResult.FromNetwork is read only by tests

**Status:** Fixed (not yet merged). `FromNetwork` is deleted; the tests read the source from the log, or count the News requests · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/News/Models/NewsPost.cs:76`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/News/Models/NewsPost.cs#L76), 82

**Problem:** NewsService logs from its local `fromNetwork` variable; no production code reads the property (LaunchSequence only constructs it). 13 test references.

**Proposed:** Keep if the owner wants tests to tell the source apart; otherwise drop the parameter and the test asserts (NewsServiceTests, LaunchSequenceTests).

**Future use:** possible (a "News from the bundled copy" hint on the Dashboard), not built.

<a id="t1b-21"></a>

### T1b-21 · Test-only · Low · ReadArea's no-`Area` fallback is reached only by tests

**Status:** Fixed (not yet merged). `SubServiceSettingsReader.ReadArea` requires `Area` (a block without it is refused) and has lost its `artccListKey` and `withNoFilter` parameters; the tests that relied on the fallback give an `Area` · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Settings/SubServiceSettingsReader.cs:47-73`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Settings/SubServiceSettingsReader.cs#L47-L73); tests relying on it include [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportSettingsParserTests.cs:97-102`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportSettingsParserTests.cs#L97-L102), [`Airports/AirportServiceTests.cs:128-143`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportServiceTests.cs#L128-L143), 186-198, [`Navaids/NavaidServiceTests.cs:61-99`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Navaids/NavaidServiceTests.cs#L61-L99), [`Fixes/FixServiceTests.cs:59-71`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Fixes/FixServiceTests.cs#L59-L71), [`Fixes/FixSettingsParserTests.cs:563-577`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Fixes/FixSettingsParserTests.cs#L563-L577), [`WxStations/WxStationServiceTests.cs:66-96`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/WxStations/WxStationServiceTests.cs#L66-L96), [`WxStations/WxStationSettingsParserTests.cs:258-272`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/WxStations/WxStationSettingsParserTests.cs#L258-L272), [`ArtccBoundaries/ArtccBoundarySettingsParserTests.cs:73-82`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/ArtccBoundaries/ArtccBoundarySettingsParserTests.cs#L73-L82), [`ArtccBoundaries/ArtccBoundaryServiceTests.cs:62-84`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/ArtccBoundaries/ArtccBoundaryServiceTests.cs#L62-L84)

**Problem:** Since beta.6 (113218e) the GUI always writes `Area` into the block (GeojsonSubServiceViewModel.cs:729) and saved pre-beta.6 settings are migrated (UserConfigAreas.DefaultFor); the Harness sets `Area` on every area tab (HarnessSettings.cs:68,142,194,257,326,372,420,458). So the fallback serves only tests (and hand-built blocks), and its doc's "the harness, or one written before there was a choice" is stale.

**Proposed:** Owner's call. Keep it (cheap convenience) and reword the doc to `A block without it (one built by hand, as a test does)`; or remove it and add `["Area"] = "Roi"` / `"Artccs"` to the tests listed.

**Future use:** likely kept as a convenience; not needed by any shipped caller.

**Note:** Also found as [T1c-7](300_Unit-Tests.md#t1c-7). Goes with [C6-1](130_Old-Version-Code.md#c6-1), [C4-12](130_Old-Version-Code.md#c4-12) and [T1a-20](130_Old-Version-Code.md#t1a-20); [C1-30](200_Comments-Core.md#c1-30) has the doc wording.

<a id="c1-11"></a>

### C1-11 · Test-only · Low · VersionCheckResult.ParseChannel(string?) (one-argument overload) is used only by a test

**Status:** Fixed (not yet merged). The overload is deleted; its test passes `ReleaseChannel.Stable` · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Updates/Models/VersionCheckResult.cs:90-97`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Updates/Models/VersionCheckResult.cs#L90-L97)

**Problem:** Production uses only `ParseChannel(value, fallback)` (UpdateChannelSetting.cs:45) and `TryParseChannel`. VersionCheckTests.cs:593 is the only caller.

**Proposed:** Delete the overload and its test case (VersionCheckTests.cs:593 can call the two-argument form with `ReleaseChannel.Stable`).

**Future use:** unlikely.

<a id="c5-6"></a>

### C5-6 · Unused · Low · Airway.Segments and AirwayPoint.FoundIn are written but never read; Airway.AwyLocation and Airway.MaxAuthAlt are read only by a test

**Status:** Fixed (not yet merged). `Airway.Segments`, `AirwayPoint.FoundIn` and WaypointLocator's `foundIn` are deleted, and `AwyLocation` and `MaxAuthAlt` with [C2-26](#c2-26) · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Airways/Models/Airway.cs:22-26`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Airways/Models/Airway.cs#L22-L26),31-32; [`FeBuddy/FeBuddy.Core/Domain/Airways/Models/AirwayPoint.cs:15-17`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Airways/Models/AirwayPoint.cs#L15-L17),23; [`FeBuddy/FeBuddy.Core/Infrastructure/Nasr/WaypointLocator.cs:33`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/WaypointLocator.cs#L33),69,127-172

**Problem:** `Segments` is set at AirwayBuilder.cs:174 and never read: the classifier is given `normalizedSegments` directly. `FoundIn` is set at AirwayBuilder.cs:259 and never read. The `foundIn` element of `WaypointLocator.Find`'s tuple is likewise thrown away by every caller (DepartureBuilder.cs:273, ArrivalBuilder.cs:318, DepartureFilter.cs:125, ArrivalFilter.cs:135, AirwayGeometryBuilder.cs:57) and checked only by WaypointLocatorTests. WaypointLocator is not in the NASR Models/Parsers exemption. `Airway.AwyLocation` (set at AirwayBuilder.cs:171) is read only at AirwayBuilderTests.cs:54, and `Airway.MaxAuthAlt` only at AirwayBuilderTests.cs:56.

**Proposed:** Delete `Airway.Segments` and its assignment. Delete `AirwayPoint.FoundIn`, update its positional constructions (AirwayBuilder.cs:254-259, AirwayGeojsonWriterDesignationTests.cs:139-140, AirwayWaypointBufferTests.cs:21), and drop `foundIn` / `CoordinateEntry.FoundIn` from WaypointLocator and its tests. For AwyLocation and MaxAuthAlt, delete them with the two test asserts, unless they are meant to become `feb.*` properties.

**Future use:** Segments and FoundIn unlikely. AwyLocation and MaxAuthAlt possible as future `feb.awyLocation` / `feb.maxAuthAlt` properties.

**Note:** Also found as [C2-25](#c2-25). See also [C2-26](#c2-26).

<a id="c6-28"></a>

### C6-28 · Test-only · Low · UserConfigPackage.ConfigVersion (and its default) and UserConfigMigrationResult.FromVersion are read only by tests

**Status:** Fixed (not yet merged). The `ConfigVersion` default and `FromVersion` are deleted, and the tests pass a layout; `ConfigVersion` stays, as the import tests read the layout a file was taken as · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/Models/UserConfigPackage.cs:14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/Models/UserConfigPackage.cs#L14), 21; [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/Models/UserConfigMigrationResult.cs:5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/Models/UserConfigMigrationResult.cs#L5), 10

**Problem:** `UserConfigTransfer.Package` is the only production constructor, and it always passes `configVersion`. The `= Oldest` default is used only by ImportSettingsViewModelTests.cs:35 and UserConfigTransferTests.cs:1041. No production code reads `.ConfigVersion` or `.FromVersion`. The callers log the version they passed in.

**Proposed:** Drop the `= UserConfigVersion.Oldest` default and pass it in those two tests. Drop `FromVersion` (and its test assert at UserConfigMigrationsTests.cs:34), or keep it if the import window should say which layout a file came from.

**Future use:** unlikely.

<a id="c6-25"></a>

### C6-25 · Test-only · Low · UserConfigFile.ProfileExists

**Status:** Fixed (not yet merged). `ProfileExists` and its asserts are deleted · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs:71-75`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs#L71-L75)

**Problem:** It is used only by `UserConfigProfilesTests.cs:264-265`. The Wpf checks names through `ProfileNameProblem` and `Profiles()` (ProfileNameViewModel, SettingsProfilesViewModel), and Core uses the private `ExistingName`.

**Proposed:** Delete it and the two asserts at UserConfigProfilesTests.cs:264-265.

**Future use:** unlikely.

<a id="c6-4"></a>

### C6-4 · Unused · Low · `v\d+` in LegacyBackupName matches a file no release ever wrote

**Status:** Fixed (not yet merged). `LegacyBackupName` is deleted with [C6-2](130_Old-Version-Code.md#c6-2) · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs:34-35`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs#L34-L35)

**Problem:** The releases that kept files beside `UserConfig.json` (beta.1-beta.3) wrote only `UserConfig.previous.json` and `UserConfig.before-import.json`. `UserConfig.v<N>.json` was named only by unreleased commits 39efa95..27f6777, all first tagged in beta.4. Even those had `Current = 1`, so nothing was ever brought forward and no such file could exist.

**Proposed:** `new(@"^UserConfig\.(previous|before-import)\.json$", ...)` and summary "The backups 3.0.0-beta.3 and earlier kept beside &lt;c>UserConfig.json&lt;/c>." Or delete it with [C6-2](130_Old-Version-Code.md#c6-2).

**Future use:** unlikely.

**Note:** Goes with [D1](010_Decisions.md#d1).

<a id="c6-27"></a>

### C6-27 · Test-only · Low · UserConfigMigrationContext.Move, ChangeValue and Values are used only by tests

**Status:** Kept (not yet merged): the layout framework stays for the layouts after 3.0 ([D1](010_Decisions.md#d1)), and these are the edits a step is made of · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigrationContext.cs:24`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigrationContext.cs#L24), 43-72, 74-85

**Problem:** The only step (`UserConfigAreas.SaveForEveryTab`) uses `Get`, `Set`, `Remove` and `KeysAtOrBelow`. The other three are exercised only by UserConfigMigrationsTests (lines 14-16, 116-175).

**Proposed:** Keep them. They are the toolkit Settings-Reference "Changing the layout" tells the next layout change to use.

**Future use:** likely (any post-3.0 layout change).

**Note:** Proposed: keep (the toolkit for future layout changes).

<a id="c5-5"></a>

### C5-5 · Test-only · Low · EramProperties' custom equality serves only tests; its remark claims a grouping that doesn't happen

**Status:** Fixed (not yet merged). The custom equality is deleted; the reader's tests compare `Filters` by content in a helper of their own · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Eram/Models/EramProperties.cs:55-89`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Eram/Models/EramProperties.cs#L55-L89)

**Problem:** Nothing groups or keys by EramProperties. The writer groups by `FilterKey`, `AttributesName` and `AttributesSignature` strings (EramGeojsonWriter.cs:328,337,365,463). The only production equality check is `IsEmpty` (`this == None`), and default record equality would give the same answer because `None.Filters` is null. Content equality is needed only by EramGeoMapReaderTests, which use `Assert.Equal(new EramProperties{…}, …)` at lines 117, 118, 226, 229, 315-320 and 350, and the hash tests at 493-496.

**Proposed:** Reword the remark: "Compares &lt;see cref="Filters"/> by content, so two property sets read from the same XML compare equal (the reader's tests rely on it)." Or delete the custom Equals/GetHashCode and assert field by field in the tests.

**Future use:** unlikely.

<a id="c6-26"></a>

### C6-26 · Test-only · Low · AppLog.GetLogFilePath is used only by tests and duplicates the writer's own path logic

**Status:** Fixed (not yet merged). `GetLogFilePath`, the writer, the prune pattern and the date parse share one file-name helper · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Logging/AppLog.cs:236-243`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Logging/AppLog.cs#L236-L243), 310

**Problem:** No production caller (3 test uses). The file name pattern is spelled twice (and a third time in `TryParseLogFileDate`'s prefix and `PruneOldLogs`' `FE-Buddy_*.log`).

**Proposed:** Either delete it and its test uses, or keep one private `LogFileName(DateTime utcDate)` helper used by ConsumeAsync and GetLogFilePath, plus a `FileNamePrefix` const used by the prune pattern and TryParseLogFileDate.

**Future use:** possible (an "open today's log" button), so prefer sharing the helper over deleting it.

<a id="c7-18"></a>

### C7-18 · Test-only · Low · `MarkdownParser.ParseInlines`'s `issueUrlBase` and `linkBase` parameters are used only by tests

**Status:** Fixed (not yet merged). `ParseInlines` takes the text alone; the link tests go through `Parse` · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Markdown/MarkdownParser.cs:53-63`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Markdown/MarkdownParser.cs#L53-L63)

**Problem:** The only production caller, InlineMarkdown.cs:55, passes text alone. The parameters are there so the tests (4 calls) can exercise link resolution without building blocks.

**Proposed:** Optional. Keep them as a test convenience, or drop both and test link resolution through `Parse` (which takes the same arguments).

**Future use:** likely (inline Markdown with links in a future card).

<a id="c7-21"></a>

### C7-21 · Unused · Low · Markdown features no current content uses (recommend keeping them)

**Status:** Kept (not yet merged): authors write News and release notes expecting GitHub's Markdown, and a dropped feature would show as raw markup · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Markdown/MarkdownParser.cs:112-118`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Markdown/MarkdownParser.cs#L112-L118) + 160-177 (fenced code), 209-210 (ordered lists' start number), 319-326 (hard breaks), 389-395 (backslash escapes), 420-438 (images), 444-455 (autolinks), 457-468 (bare URLs), 482-501 (`~~strike~~`, `_`/`__` emphasis); [`FeBuddy/FeBuddy.Core/Infrastructure/Markdown/Models/MarkdownBlock.cs:20-22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Markdown/Models/MarkdownBlock.cs#L20-L22), 28-30

**Problem:** All content the parser receives was scanned: root News.md (299 lines), ChangeLog.md (738 lines, whose sections become release notes), .github/release-notes-template.md, and WhatsNewViewModel's strings. None of it uses fenced code, numbered lists, strikethrough, images, `<autolinks>`, hard line breaks, underscore emphasis, or escapes outside code spans. The only bare URL is in the alpha.1 stub, which alpha.1's own parser renders. Features in use: headings, paragraphs, nested bullet lists with continuation lines, rules, a block quote (the template's pre-release line), `**bold**`, `*italic*`, `` `code` ``, `[links](…)`, `#123` issue references, and line-start HTML comments.

**Proposed:** Keep them. Release notes and News are free-form GitHub Markdown that authors write expecting GitHub's rendering, the code is small and fully tested (40 tests), and dropping a feature would make the next post that uses it show raw markup. No change, apart from the summary wording in [C7-19](200_Comments-Core.md#c7-19).

**Future use:** likely (free-form authored content).

<a id="c7-8"></a>

### C7-8 · Unused · Low · `AiracCycleAvailability.ProbeAsync`'s try/catch can never catch; cancellation is logged as a probe error

**Status:** Fixed (not yet merged). A cancelled probe throws rather than being logged as a probe error, with no ranged GET after it; the outer catch is gone, and the remarks say when the GET is tried · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Nasr/AiracCycleAvailability.cs:19`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/AiracCycleAvailability.cs#L19), 56-72, 111-115

**Problem:** `SendAsync` already catches every exception, including `OperationCanceledException`, so the outer catch (lines 68-72) can never run. A cancelled probe is logged as "publication probe error (A task was canceled.). Will retry.", and the ranged GET is then tried anyway. Separately, the class remarks say the ranged GET is used "when HEAD is not allowed", but line 61 falls back on any inconclusive result (405, 5xx, an unfollowed redirect, a network error).

**Proposed:** Delete the outer try/catch, or have `SendAsync` let `OperationCanceledException` through `when (cancellationToken.IsCancellationRequested)` and keep the outer catch only for that. Remarks: "...falling back to a one-byte ranged GET when HEAD gives no clear answer (405, 5xx, a network error)...".

<a id="c7-16"></a>

### C7-16 · Test-only · Low · `InstalledProduct.ProductSemVer` is read only by tests

**Status:** Fixed (not yet merged). `ProductSemVer` is deleted, and `InstallerState.wxs` says which values the app and the setup window read · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledProduct.cs:26-27`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledProduct.cs#L26-L27); (related) [`FeBuddy/FeBuddy.Installer/InstallerState.wxs:6`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer/InstallerState.wxs#L6)

**Problem:** No code in Core, Wpf or the Harness reads it; only InstalledProductTests.cs:27 does. The MSI still writes the value for its own version-policy check (VersionPolicyActions). InstallerState.wxs:6 claims "the app reads InstallLocation, ProductSemVer and ProductCode (FeBuddy.Core InstalledProduct)", which is wrong for ProductSemVer.

**Proposed:** Delete `InstalledProduct.ProductSemVer` and its assertion in InstalledProductTests.cs:23,27, and make InstallerState.wxs:6 say "the app reads InstallLocation and ProductCode". Alternatively keep it and leave the .wxs as is.

**Future use:** unlikely (the app's own version comes from AppVersion, by design).

**Note:** Also found as [T2-11](#t2-11).

<a id="c7-17"></a>

### C7-17 · Test-only · Low · `TelephonyHtmlParser.Parse(registerPath, specialCallSignsPath)` is used only by tests

**Status:** Fixed (not yet merged). The combined `Parse` and its four tests are deleted, and TelephonyDataCollection's summary says AiracSharedDataLoader assembles it · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Telephony/Parsers/TelephonyHtmlParser.cs:40-56`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Telephony/Parsers/TelephonyHtmlParser.cs#L40-L56); [`FeBuddy/FeBuddy.Core/Infrastructure/Telephony/Models/TelephonyDataCollection.cs:5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Telephony/Models/TelephonyDataCollection.cs#L5)

**Problem:** Production calls `ParseRegister` and `ParseSpecialCallSigns` separately: AiracSharedDataLoader.cs:151/163 (each page gets its own fallback message), TelephonyDownloader.cs:60/68 and TelephonyViewModel.cs:184/195. The combined `Parse` is called only in TelephonyHtmlParserTests.cs:217-243. TelephonyDataCollection's summary ("as TelephonyHtmlParser returns them") describes it; in production, AiracSharedDataLoader builds the collection.

**Proposed:** Delete `Parse(string, string?)` and its four tests. Reword TelephonyDataCollection.cs:3-7: "The parsed FAA telephony pages ... and the virtual airline list, as AiracSharedDataLoader assembles them for a run."

**Future use:** unlikely.

**Note:** Also found as [T2-10](#t2-10).

<a id="c7-15"></a>

### C7-15 · Test-only · Low · Row-model fields that production code never reads (Wx stations, d-TPP)

**Status:** Fixed (not yet merged). `NumResults` is deleted; the d-TPP and Wx row fields stay, as complete mirrors of their files like the NASR models · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/WxStations/Models/WxStationDataCollection.cs:12-17`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/WxStations/Models/WxStationDataCollection.cs#L12-L17) (`NumResults`); [`WxStationXmlDataModel.cs:21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/WxStations/Models/WxStationXmlDataModel.cs#L21) (`StationId`), 45 (`FaaId`), 53 (`WmoId`), 91 (`ElevationM`), 107 (`State`); [`FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/Models/DtppMetafileDataCollection.cs:21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/Models/DtppMetafileDataCollection.cs#L21) (`FromEdate`), 27 (`ToEdate`), 39 (`ToEffectiveUtc`); [`DtppMetafileXmlDataModel.cs:38`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/Models/DtppMetafileXmlDataModel.cs#L38) (`StateFullName`), 59 (`Volume`), 211 (`CnFlg`), 225 (`CnSection`), 237 (`CnPage`), 253 (`BvSection`), 262 (`BvPage`), 286 (`TwoColored`)

**Problem:** Each of these is set by its parser and read only by tests. `NumResults` is the clearest case: no code compares it with `Stations.Count`, despite the doc's reason for keeping it.

**Proposed:** Owner's choice. Either (a) treat the Dtpp and WxStations row models like the NASR models (complete mirrors of the source file, kept on purpose) and leave them, or (b) delete `WxStationDataCollection.NumResults` (and its tests/parser line 44), since its doc implies a check that doesn't exist. I recommend (a) for the row fields and (b) for `NumResults`.

**Future use:** likely for the row fields (they mirror the FAA/aviationweather files, and the Procedure Charts work keeps adding fields); unlikely for `NumResults`.

<a id="w6-25"></a>

### W6-25 · Unused · Low · AliasGuideDocumentView.Divider always gets the same margin

**Status:** Fixed (not yet merged). `Divider()` takes no margin and sets only the `Divider` style · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Controls/AliasGuideDocumentView.cs:293`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/AliasGuideDocumentView.cs#L293), 487-492

**Problem:** The only call passes `new Thickness(0)`, the default margin, so the parameter does nothing.

**Proposed:** Drop the parameter: `private static Border Divider()`, which sets only the `Divider` style.

**Future use:** unlikely.

<a id="w6-20"></a>

### W6-20 · Unused · Low · BesideOrBelow.HorizontalSpacing is never set

**Status:** Fixed (not yet merged). `HorizontalSpacing` is a private constant of 16 · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Controls/BesideOrBelow.cs:39-42`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/BesideOrBelow.cs#L39-L42), 63-68

**Problem:** All three uses (FileNamesView.xaml:90, AliasGuidePageView.xaml:41 and 55) and the tests leave it at 16.

**Proposed:** Optional: keep it for symmetry with `VerticalSpacing`, which is set, or replace it with a private const.

**Future use:** plausible; it is a layout knob of a reusable panel.

<a id="w6-21"></a>

### W6-21 · Unused · Low · CommandTablePanel's four spacing dependency properties are set only by a test, to their defaults

**Status:** Fixed (not yet merged). The four spacing properties are private constants with the same values; the test no longer sets them · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Controls/CommandTablePanel.cs:33-51`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/CommandTablePanel.cs#L33-L51), 67-93

**Problem:** The panel is only built in code by AliasGuideDocumentView.cs:225, which sets only `RuleBrush`. CommandTablePanelTests.cs:29 sets the four to their default values. Nothing uses them in XAML.

**Proposed:** Make them private constants (and drop them from the test's initializer), or keep them as deliberate knobs.

**Future use:** unlikely; one caller, in code.

<a id="w6-22"></a>

### W6-22 · Test-only · Low · CommandTablePanel.Rules exists only for the tests

**Status:** Kept (not yet merged): the rules are drawn, not child elements, so `Rules` is the tests' only way to see them; it stays internal, for the tests · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Controls/CommandTablePanel.cs:105-106`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/CommandTablePanel.cs#L105-L106)

**Problem:** Only CommandTablePanelTests reads it.

**Proposed:** None needed if test hooks are acceptable; otherwise test the rendered rules another way.

**Future use:** n/a (test hook).

**Note:** Also found as [T3-23](#t3-23).

<a id="w6-19"></a>

### W6-19 · Unused · Low · FilterPicker.Minimum and Maximum are never set

**Status:** Fixed (not yet merged). `Minimum` and `Maximum` are deleted; the picker offers exactly what `CrcPropertyValidator` accepts · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Controls/FilterPicker.xaml.cs:39-47`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/FilterPicker.xaml.cs#L39-L47), 66-78, 97-98

**Problem:** The only FilterPicker (Views/Cards/CrcDefaultsPanel.xaml:97) sets just `Value` and `FieldState.Error`. The range is always CRC's 0-40, so the two dependency properties and `OnRangeChanged` do nothing.

**Proposed:** Replace them with `CrcPropertyValidator.MinFilter` and `MaxFilter` used directly in `BuildOptions`, `Parse` and `Prompt`, and delete `OnRangeChanged`.

**Future use:** unlikely; CRC's filter range is fixed.

<a id="w6-23"></a>

### W6-23 · Unused · Low · StringToVisibilityConverter's "Invert" parameter is never used

**Status:** Fixed (not yet merged). The "Invert" parameter is deleted · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Converters/StringToVisibilityConverter.cs:9-10`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Converters/StringToVisibilityConverter.cs#L9-L10), 18-21

**Problem:** None of the 31 `StringToVis` uses passes a ConverterParameter.

**Proposed:** Optional: drop the Invert branch and the doc sentence, or keep it for parity with `BoolToVis`, which uses Invert.

**Future use:** likely enough to keep; it costs nothing.

<a id="w6-24"></a>

### W6-24 · Unused · Low · GeoJsonReader's InvalidOperationException catch can't be reached

**Status:** Fixed (not yet merged). The catch and its "Not laid out as GeoJSON" message are deleted from the reader, now `GeojsonFileReader` in Core · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Map/GeoJsonReader.cs:54-61`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Map/GeoJsonReader.cs#L54-L61)

**Problem:** Every `JsonElement` access that could throw `InvalidOperationException` is already guarded by a `ValueKind` check: `TryGetProperty` only on Objects (lines 84-86, 114, 150, 156), `EnumerateArray` only on Arrays (94, 127, 164, 232), and `GetArrayLength`/indexing only after an Array check (240-243). No input reaches the catch, and no test covers that message.

**Proposed:** Optional: delete the try/catch and the "Not laid out as GeoJSON" message, or keep it as a deliberate safety net and say so in a comment.

**Future use:** n/a (defensive code).

**Note:** Also found as [T3-22](#t3-22).

<a id="w7-27"></a>

### W7-27 · Unused · Low · The `--devmode` spelling of the dev-launch argument has no user

**Status:** Fixed (not yet merged). Only `--dev` asks for a dev launch; `--devmode` is now an unknown argument, and the developer README and the XML docs name only `--dev` · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Shell/DevLaunchArgs.cs:4`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Shell/DevLaunchArgs.cs#L4),14,19-21; [`FeBuddy/FeBuddy.UnitTests/Wpf/Shell/DevLaunchArgsTests.cs:16`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/Shell/DevLaunchArgsTests.cs#L16); [`docs/Developers/README.md:68`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/README.md?plain=1#L68)

**Problem:** Everything that starts a dev launch passes `--dev` (`DevLaunchArgs.Argument`: launchSettings.json:8, AppRestart via SettingsViewModel.cs:966); the second spelling is only accepted, tested and mentioned in the dev README.

**Proposed:** Delete the `--devmode` branch, its test case, and "or `--devmode`" in README.md:68 and the two XML docs (lines 4, 14).

**Future use:** unlikely; a second name for a Debug-only switch.

<a id="w7-15"></a>

### W7-15 · Unused · Low · The implicit Calendar and ComboBoxItem styles apply to nothing

**Status:** Fixed (not yet merged). The implicit Calendar and ComboBoxItem styles are deleted, with the header line; the comment names the DatePicker's drop-down as the calendar look's user · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Theme/Controls.Inputs.xaml:26-27`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Theme/Controls.Inputs.xaml#L26-L27),873,899,428

**Problem:** The app has no `<Calendar>`; the only calendars are the two DatePickers' drop-downs, which get `Themed.Calendar.DropDown` through `CalendarStyle` (line 957). Every ComboBox gets `Themed.ComboBoxItem` through `ItemContainerStyle` (line 472), and no `<ComboBoxItem>` is declared anywhere.

**Proposed:** Delete line 899 and its header line 26 ("Calendar -> Themed.Calendar …"), and the line-873 comment's "a bare Calendar anywhere gets this look"; delete line 428. Keep the keyed styles (used by BasedOn).

**Future use:** unlikely; only a safety net for a control the app does not use. Keep if that net is wanted.

<a id="w7-14"></a>

### W7-14 · Unused · Low · Unused `ctl` namespace in Typography.xaml (also linked into the installer)

**Status:** Fixed (not yet merged). The unused `ctl` namespace is deleted · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Theme/Typography.xaml:23`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Theme/Typography.xaml#L23)

**Problem:** No `ctl:` element or type in the file (the only "ctl:" is inside a comment, line 124). The file is also compiled into FeBuddy.Installer.UI (csproj:62), which has no `FeBuddy.Wpf.Controls` namespace.

**Proposed:** Delete the `xmlns:ctl` line (and close the tag on the `xmlns:sys` line).

**Future use:** unlikely.

<a id="w2-15"></a>

### W2-15 · Test-only · Low · `AiracGeneralTabViewModel.IsIncluded(string)` is used only by tests

**Status:** Fixed (not yet merged). `IsIncluded(string)` is deleted; the tests read the row's `IsIncluded` · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs:99-102`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs#L99-L102)

**Problem:** No production caller (AiracServiceViewModel uses `RowFor(...).IsIncluded`).

**Proposed:** Delete it and use `RowFor(key)!.IsIncluded` in AiracGeneralTabViewModelTests (5 asserts).

**Future use:** unlikely.

<a id="w2-17"></a>

### W2-17 · Unused · Low · General tab's `IsReady` setter re-validates a tab that has nothing to validate

**Status:** Fixed (not yet merged). `IsReady`'s setter only sets it · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs:143-153`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs#L143-L153)

**Problem:** The General tab overrides neither `Validate` nor `BuildWhatYoullGet`, so `Revalidate()` changes nothing.

**Proposed:** `private set => SetProperty(ref _isReady, value);`

**Future use:** unlikely.

<a id="w2-16"></a>

### W2-16 · Unused · Low · Public members and change notifications nothing binds to

**Status:** Fixed (not yet merged). `Readiness`, `IsReady`, `StationDataStatus` and `RefreshCycleOptions` are private, without their change notifications, and the two `IncludedSubServices` ones are deleted; `Node` is internal, for the settings layout test · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:133-141`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L133-L141), 367-368; [`WxStationsViewModel.cs:67-68`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/WxStationsViewModel.cs#L67-L68), 85; [`AiracGeneralTabViewModel.cs:172-173`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs#L172-L173), 218, 268; [`DuplicateAliasesCardViewModel.cs:26-27`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DuplicateAliasesCardViewModel.cs#L26-L27)

**Problem:** `AiracServiceViewModel.Readiness`/`IsReady` and `WxStationsViewModel.StationDataStatus` are bound nowhere (only read inside their class), so their notifications are noise; `RefreshCycleOptions` and `DuplicateAliasesCardViewModel.Node` are public but used only in their own file; `OnPropertyChanged(nameof(IncludedSubServices))` has no listener.

**Proposed:** Make `Readiness`, `IsReady`, `StationDataStatus`, `RefreshCycleOptions` and `Node` private and delete the three unused `OnPropertyChanged` calls.

**Future use:** unlikely.

<a id="w2-14"></a>

### W2-14 · Unused · Low · `SubServiceDescriptor.Order` duplicates the list order

**Status:** Fixed (not yet merged). `Order` is deleted: the catalogue's order is the rail's. Its test is deleted · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracSubServices.cs:72-75`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracSubServices.cs#L72-L75) (and every entry's 10..110); [`AiracServiceViewModel.cs:246`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L246), 276; [`AiracGeneralTabViewModel.cs:59`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs#L59)

**Problem:** Both consumers just `OrderBy(d => d.Order)`, and a test (AiracSubServicesTests.each_order_value_is_higher_than_the_last) exists only to keep the two orders equal.

**Proposed:** Remove `Order` from the record, the `.OrderBy(d => d.Order)` calls and that test; reword the summary to "Every sub-service, in the order the General tab's table and the tab rail show them."

**Future use:** unlikely.

<a id="w3-18"></a>

### W3-18 · Unused · Low · DashboardViewModel.DiscordUrl is never bound

**Status:** Fixed (not yet merged). `DiscordUrl` is deleted · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs:100-101`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs#L100-L101)

**Problem:** The description box's link uses `OpenDiscordCommand` (DashboardView.xaml:137); nothing reads `DiscordUrl`.

**Proposed:** Delete the property.

**Future use:** unlikely - the command covers the link.

<a id="w3-23"></a>

### W3-23 · Unused · Low · Public members only their own class reads

**Status:** Fixed (not yet merged). `LevelFilter`, `DescriptionText` and `ReloadAll` are private; `HasCustomHome` is folded into the Reset Home command's CanExecute · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs:153-169`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs#L153-L169) (`LevelFilter` getter); [`FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs:169-170`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs#L169-L170) (`HasCustomHome`); [`FeBuddy/FeBuddy.Wpf/ViewModels/ConfigPages.cs:44`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ConfigPages.cs#L44) (`ReloadAll`); [`FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs:29-34`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs#L29-L34) (`DescriptionText`)

**Problem:** No view, test or other class reads these: the chips bind `FilterIs*`, `HasCustomHome` is only ResetHomeCommand's CanExecute (and is raised for a binding that doesn't exist), `ReloadAll` is only called by `ReloadEverything` (callers must use ReloadEverything so the map and default ROI reload too), `DescriptionText` only feeds `Description`.

**Proposed:** Make them private (or fold `LevelFilter` into the field and `HasCustomHome` into the CanExecute lambda); `ReloadAll` private so nobody calls the half reload.

**Future use:** unlikely.

<a id="w3-21"></a>

### W3-21 · Unused · Low · Unreachable catch in MapLayersState.CurrentCycleId

**Status:** Fixed (not yet merged). The unreachable catch is deleted; `CurrentCycleId` reads the cycle directly · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs:505-515`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs#L505-L515)

**Problem:** `AppEnvironment.GetAiracCycle` is `AiracCycleResolver.GetCycle(position, CheckedUtcDate)`, pure date arithmetic that can only throw ArgumentOutOfRangeException for an unknown position; it never throws InvalidOperationException.

**Proposed:** `private static string CurrentCycleId() => AppEnvironment.GetAiracCycle(AiracCyclePosition.Current).AiracCycleId;`

**Future use:** unlikely.

<a id="w4-21"></a>

### W4-21 · Test-only · Low · AliasSourceRow.CheckFailed is read only by a test

**Status:** Fixed (not yet merged). `CheckFailed` is deleted; the test checks `HasCheckMessage` and `CheckSucceeded` · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/Models/AliasSourceRow.cs:232`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/Models/AliasSourceRow.cs#L232), 249, 255-256

**Problem:** No XAML binds it and no production code reads it (the view uses StatusTone); only ConcatenateAliasesViewModelTests.cs:159 asserts it.

**Proposed:** Delete it and its two OnPropertyChanged calls; change the test to assert `StatusTone == AliasSourceTone.Danger` or `!row.CheckSucceeded`.

**Future use:** unlikely.

<a id="w1-16"></a>

### W1-16 · Unused · Low · Procedure Charts and Telephony have unbound properties and raises nothing listens to

**Status:** Fixed (not yet merged). `IsDtppKnown`, `HasAirports` and `HasProcedureNames` are deleted with their change notifications; `GeneratesDocument`, `DtppStatus` and `TelephonyDataStatus` are private, without theirs. The tests read `DtppCycleId` and the Telephony data preview row · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs:493`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs#L493), 650, 723, 128, 131, 1748, 190, 199, 1750; [`TelephonyViewModel.cs:119`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs#L119), 136

**Problem:** `IsDtppKnown`, `HasAirports` and `HasProcedureNames` are read by nothing: no XAML, no code, no test. The view uses `HasDtppData` and the lists themselves. `GeneratesDocument` is public and raised in `OnOutputsChanged`, but only `Validate` reads it. `DtppStatus` and `TelephonyDataStatus` are raised but bound nowhere; only `BuildPreviewSummary` reads them.

**Proposed:** Delete `IsDtppKnown` and its raise, and drop "Only meaningful once IsDtppKnown is true" from `HasDtppData`'s summary. Delete `HasAirports` and `HasProcedureNames` and the `CollectionChanged` raises at lines 128 and 131 (keep line 132's `RefreshProcedureNameRows`). Make `GeneratesDocument` private and drop its raise. Drop the `OnPropertyChanged(nameof(DtppStatus))` / `(TelephonyDataStatus)` raises, or make those properties private.

**Future use:** unlikely. The cards already show their empty and loading states from other properties.

<a id="w1-17"></a>

### W1-17 · Test-only · Low · DtppPreviousStatus is read only by a test, and two of its three messages are never shown

**Status:** Fixed (not yet merged). `DtppPreviousStatus` and the two messages never shown are deleted; the test checks only that no warning shows while the previous cycle loads · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs:501-502`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs#L501-L502), 624-629

**Problem:** Only AreaTests.cs:473 reads `DtppPreviousStatus`. `DtppWarning` shows `_dtppPreviousStatus` only when `!_previousDtppAvailable`, so "Deleted procedures link to cycle X's charts." and "Cycle X is still loading. Once it has, …" never reach the user. That makes the test's "that's said, not warned about" untrue. The summary also describes a string as "Whether …".

**Proposed:** Either show it (add a `ServicePreviewRow("Previous cycle's d-TPP data", DtppPreviousStatus)` in `BuildPreviewSummary` and change the summary to "What the Preview Settings tab says about the previous cycle's metafile"), or delete the property and the two unused strings, keeping only the "isn't available" text, and have AreaTests check only `DtppWarning`.

**Future use:** likely, if the owner wants the preview row; otherwise unlikely.

<a id="w4-19"></a>

### W4-19 · Unused · Low · ConversionTabViewModel is an extra layer with exactly one subclass

**Status:** Deferred ([Q6](010_Decisions.md#q6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ConversionTabViewModel.cs:21-108`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ConversionTabViewModel.cs#L21-L108); [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/FileConversionTabViewModel.cs:38`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/FileConversionTabViewModel.cs#L38), 250, 290

**Problem:** Only FileConversionTabViewModel derives from ConversionTabViewModel, and it seals the two abstract members ConversionTabViewModel declares; every concrete page (DAT, SCT2, ERAM) derives from FileConversionTabViewModel.

**Proposed:** Merge ConversionTabViewModel into FileConversionTabViewModel (or rename the merged class ConversionTabViewModel) and update FileConversionsViewModel/ConversionChoice, which type their fields as ConversionTabViewModel.

**Future use:** unlikely; Code-Structure's "Adding a file conversion" already says derive from FileConversionTabViewModel.

<a id="w4-12"></a>

### W4-12 · Unused · Low · Unused members on the conversion page base: HasSourceFiles, EmitLines, and the Emit* no-op setters

**Status:** Fixed (not yet merged). `HasSourceFiles` and `EmitLines` are deleted; `EmitSymbols` and `EmitText` are the private `WritesSymbols` and `WritesText` · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/FileConversionTabViewModel.cs:62`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/FileConversionTabViewModel.cs#L62), 146-147, 184-191

**Problem:** `HasSourceFiles` is only announced, never bound or read. `EmitLines` is never read. The class implements no interface that needs `EmitSymbols`/`EmitText` setters (GeojsonFilesCard, which binds them, is only used by AIRAC tabs); they are only read inside the class.

**Proposed:** Delete `HasSourceFiles` and the `OnPropertyChanged(nameof(HasSourceFiles))` line, delete `EmitLines`, and make `EmitSymbols`/`EmitText` private get-only (e.g. `WritesSymbols`/`WritesText`).

**Future use:** unlikely.

<a id="d1a-17"></a>

### D1a-17 · Unused · Low · Procedure Charts and Telephony save four settings nothing reads, and send four the parser ignores

**Status:** Kept (not yet merged): the saved keys stay, as dropping them needs a settings layout step, and a comment says so. The run is no longer sent the four keys the parsers ignore · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs:662-668`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs#L662-L668), 696-715

**Problem:** both tabs derive from `GeojsonSubServiceViewModel` and never show the FE-Buddy properties or CRC cards. Every save still writes `IncludeFebCustomProperties` (`N`), `FebProperties` (empty), `CrcDefaultsScope` (`None`) and `CrcDefaultsFiles` (empty) under their nodes (they are in UserConfig.v2.json). Every run sends `CoordinatePrecision`, `IncludeFebCustomProperties`, `FebProperties` and `CrcDefaultsFor`, which their parsers ignore (see [D1a-4](500_Developer-Docs.md#d1a-4)).

**Proposed:** skip the FE-Buddy properties and CRC lines in `SaveSharedSettings` / `LoadSharedSettings` / `AddSharedSettings` for a tab with no GeoJSON (e.g. a `protected virtual bool HasGeojson`). Dropping the saved keys makes UserConfigLayoutTests report them "no longer read or saved" for the v1/v2 samples, so it needs a layout-3 step that `Remove`s them. Leave as is if a layout change isn't worth it before 3.0.

**Future use:** unlikely.

**Note:** Dropping the saved keys needs a settings layout step.

<a id="w4-13"></a>

### W4-13 · Unused · Low · Duplicate properties on GeojsonSubServiceViewModel: WritesAliasFile and HasGeojsonFiles

**Status:** Fixed (not yet merged). `WritesAliasFile` is deleted and the AIRAC Service reads `GenerateAliasFile`; `HasGeojsonFiles` is private · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs:108`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs#L108), 142-143, 207-212, 463

**Problem:** `WritesAliasFile` is always equal to `GenerateAliasFile` (which already includes HasAliasFile). `HasGeojsonFiles` is public but nothing outside the class reads it; `ShowsCrcDefaultsCard` is the same value.

**Proposed:** Delete `WritesAliasFile` (and its OnPropertyChanged at 463) and use `GenerateAliasFile` in AiracServiceViewModel.cs:81, 313, 342; make `HasGeojsonFiles` private (or drop it and use `CrcFileRows.Count > 0` / `ShowsCrcDefaultsCard`).

**Future use:** unlikely.

<a id="w4-14"></a>

### W4-14 · Unused · Low · Virtual members nobody overrides: GenerateGeojson, IsAreaInUse

**Status:** Fixed (not yet merged). `GenerateGeojson` is not virtual, and its remark says it is off for a sub-service that doesn't offer GeoJSON; `IsAreaInUse` is private · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs:110-112`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs#L110-L112), 428-432

**Problem:** No tab overrides either. `GenerateGeojson`'s remark "Always off for a sub-service with no GeoJSON choice" is true only because SubServiceRow.Geojson returns `_geojson && OffersGeojson`; the property itself doesn't check anything.

**Proposed:** Drop `virtual` from both (IsAreaInUse can be private). Reword the remark to "Turned on and off on the General tab; off for a sub-service that doesn't offer GeoJSON there."

**Future use:** unlikely.

<a id="w4-22"></a>

### W4-22 · Test-only · Low · GeojsonSubServiceViewModel.EveryOutput is only ever used by tabs built in tests

**Status:** Kept (not yet merged): every tab the app builds is attached to the General tab's row, and the default lets a tab built on its own, as most tab tests build one, work without one · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs:73`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs#L73), 100-104, 1009-1021

**Problem:** Every GeoJSON tab descriptor in AiracSubServices has Outputs set, so AiracServiceViewModel.BuildSubServiceTabs always calls AttachOutputs with the General tab's row; the default only matters for a tab constructed on its own (the tests). The docs already say "(a test)".

**Proposed:** None required (a harmless null-object). If the owner wants production code free of test defaults, make the tests attach a stub row and initialise `_outputs` from a constructor argument.

**Future use:** n/a.

<a id="w4-20"></a>

### W4-20 · Unused · Low · Card interfaces with a single implementer, used only for design-time DataContext

**Status:** Deferred ([Q6](010_Decisions.md#q6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/IGeojsonFileChoices.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/IGeojsonFileChoices.cs), [`ICrcDefaultsChoice.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ICrcDefaultsChoice.cs), [`IAreaSettings.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/IAreaSettings.cs), [`IPreviewOptions.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/IPreviewOptions.cs)

**Problem:** IGeojsonFileChoices, ICrcDefaultsChoice and IAreaSettings are implemented only by GeojsonSubServiceViewModel, IPreviewOptions only by DuplicateAliasesCardViewModel; besides that each appears only in a card's `d:DataContext="{d:DesignInstance ...}"`. Code-Structure says "no interface 'just in case'". (ICrcDefaultsSettings, IFebPropertySettings, IRunAction and ISourceFileSettings-by-design have 2+ users or are the card contract for several pages and are fine.)

**Proposed:** Owner's call: keep them as the cards' binding contracts and note that in Code-Structure, or use the concrete type in the cards' DesignInstance (`tabs:GeojsonSubServiceViewModel`, `vm:DuplicateAliasesCardViewModel`) and delete the four interfaces.

**Future use:** plausible if a non-GeoJSON tab ever reuses those cards.

<a id="w4-17"></a>

### W4-17 · Unused · Low · ISubServiceOutputs.IsIncluded is never read through the interface

**Status:** Fixed (not yet merged). `IsIncluded` is deleted from `ISubServiceOutputs` and its default; `SubServiceRow` keeps its own · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ISubServiceOutputs.cs:12-13`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ISubServiceOutputs.cs#L12-L13); [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs:1012`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs#L1012)

**Problem:** Tabs read only Alias/Geojson/ProcedureChanges/ProceduresJson from their `ISubServiceOutputs`; inclusion is applied by AiracServiceViewModel through `SubServiceRow.IsIncluded` directly (greying the tab).

**Proposed:** Remove `IsIncluded` from the interface and from EveryOutput (SubServiceRow keeps its own public property); reword the interface summary to "Which of a sub-service's outputs are on".

**Future use:** unlikely.

<a id="w4-9"></a>

### W4-9 · Unused · Low · ServicePreviewSection.Note is never set

**Status:** Fixed (not yet merged). `Note` and its TextBlock are deleted · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/Models/ServicePreviewSection.cs:6-7`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/Models/ServicePreviewSection.cs#L6-L7); [`FeBuddy/FeBuddy.Wpf/Views/ServicePreviewTabView.xaml:39-41`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ServicePreviewTabView.xaml#L39-L41)

**Problem:** All 14 constructions (every tab's BuildPreviewSummary) pass two arguments; no `Note:` anywhere. The TextBlock bound to `Note` in the Preview Settings section template never shows.

**Proposed:** Delete the `Note` parameter and its `<param>` doc, and the `<TextBlock Text="{Binding Note}" .../>` at ServicePreviewTabView.xaml:39-41.

**Future use:** unlikely; a tab with nothing to add already uses a row (ConcatenateAliasesViewModel.cs:242).

<a id="w4-10"></a>

### W4-10 · Unused · Low · SubServiceDescriptor.AliasFileName is set for 7 sub-services and never read

**Status:** Fixed (not yet merged). `AliasFileName` is deleted, with the seven entries' alias file arguments · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/Models/SubServiceDescriptor.cs:20-23`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/Models/SubServiceDescriptor.cs#L20-L23), 34; [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracSubServices.cs:84`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracSubServices.cs#L84), 92, 100, 107, 114, 127, 136

**Problem:** Nothing reads `.AliasFileName` (alias files come from each tab's `OutputFiles()` / `OutputFileOption.AliasFile`).

**Proposed:** Delete the parameter and its `<param>` doc, and the `XxxOutputFiles.Alias` argument from the 7 AiracSubServices entries.

**Future use:** unlikely; the tabs own their file lists.

**Note:** Also found as [W2-13](#w2-13).

<a id="w4-15"></a>

### W4-15 · Unused · Low · ServiceTabViewModel's base MarkDirty/ClearDirty never run, and the remark describes behaviour no tab has

**Status:** Fixed (not yet merged). `MarkDirty` and `ClearDirty` are on `SubServiceSettingsViewModel` only; the "pessimistic" remark is deleted and the crefs point there · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceTabViewModel.cs:224-244`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceTabViewModel.cs#L224-L244); [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceSettingsViewModel.cs:105-117`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceSettingsViewModel.cs#L105-L117)

**Problem:** The only subclasses are SubServiceSettingsViewModel (overrides both without calling base), ServicePreviewTabViewModel and ServiceRunReviewTabViewModel (never call either). So the base bodies are unreachable and the "pessimistic" mode doesn't exist.

**Proposed:** Move MarkDirty/ClearDirty to SubServiceSettingsViewModel as `protected virtual` (no base versions), keep `IsDirty`'s protected setter, delete the "pessimistic view" remark, and update the crefs that point at `ServiceTabViewModel.MarkDirty`/`ClearDirty` (SubServiceSettingsViewModel.cs:21, GeojsonSubServiceViewModel.cs:596).

**Future use:** unlikely.

<a id="w4-16"></a>

### W4-16 · Unused · Low · ServiceTabViewModel.IsRunnable only feeds a count that IncludedRunTargets already gives; its doc claims more

**Status:** Fixed (not yet merged). `IsRunnable` is deleted with the Review tab's override; a run checks `IncludedRunTargets`, read once · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceTabViewModel.cs:69-73`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceTabViewModel.cs#L69-L73); [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceRunReviewTabViewModel.cs:53`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceRunReviewTabViewModel.cs#L53); [`AiracServiceViewModel.cs:530-537`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L530-L537)

**Problem:** The only read is AiracServiceViewModel's `runnable` array (available, IsRunnable, not General/File Names/Preview), used only for `Length == 0`; that set is exactly `IncludedRunTargets`. Nothing about blocking a run reads it: TrySaveDirtyTabs/EnsureNoInvalidTabs filter on IsAvailable/IsDirty/Status only.

**Proposed:** In RunAsync use `if (IncludedRunTargets.Count == 0)` (computed once and reused as `targets`), then delete `IsRunnable` and the Review tab's override.

**Future use:** unlikely.

<a id="w4-11"></a>

### W4-11 · Unused · Low · ServiceValidation.HasErrors is unused

**Status:** Fixed (not yet merged). `HasErrors` is deleted · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceValidation.cs:17-18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceValidation.cs#L17-L18)

**Problem:** Revalidate reads `Messages` and `FieldErrors`; nothing reads `HasErrors` (the XAML `HasErrors` bindings are the Review tab's own property).

**Proposed:** Delete it.

**Future use:** unlikely.

<a id="w4-18"></a>

### W4-18 · Unused · Low · TabbedServiceViewModel's optional parts have one user, which supplies all of them

**Status:** Deferred ([Q6](010_Decisions.md#q6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/TabbedServiceViewModel.cs:15-28`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/TabbedServiceViewModel.cs#L15-L28), 52-54, 60-64, 129-145, 158-172

**Problem:** AiracServiceViewModel is the only subclass; it always returns a General tab and a Preview tab, so `HasPreviewTab` is always true (yet bound to the Preview button's Visibility, TabbedServiceView.xaml:110), and the null branches for GeneralTab/PreviewTab never run. The remark itself says File Conversions is not tabbed.

**Proposed:** Owner's call. Either keep the generic base (and say in the remark it is kept for a future tabbed service), or fold it into AiracServiceViewModel: make GeneralTab/PreviewTab abstract non-nullable, delete `HasPreviewTab` and its XAML Visibility binding, and simplify RebuildTabs/GoToPreview.

**Future use:** plausible only if a second tabbed service screen is planned.

<a id="w3-19"></a>

### W3-19 · Unused · Low · SettingsViewModel.IsOnline is never bound or read

**Status:** Fixed (not yet merged). `IsOnline` and its change notification are deleted · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:573-574`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L573-L574), 1104

**Problem:** SettingsView.xaml doesn't bind it; the update button's state comes from `CheckNowCommand`'s CanExecute. Its doc ("the update check needs it") describes nothing that uses it.

**Proposed:** Delete the property and the `OnPropertyChanged(nameof(IsOnline))` line.

**Future use:** unlikely.

<a id="w3-22"></a>

### W3-22 · Unused · Low · Redundant "no entries" guards before ComputeReadiness

**Status:** Fixed (not yet merged). Settings and the shell call `ComputeReadiness()` without the "no entries" check, and so does the AIRAC Service's `Readiness` · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:1098-1100`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L1098-L1100); [`FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs:716-718`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs#L716-L718)

**Problem:** `ComputeReadiness` already returns `Waiting` when there is no current entry (AiracCycleDataCache.cs:533-538); `Entries` also copies the list under the lock just to count it. (AiracServiceViewModel.cs:137 has the same guard - not in this scope.)

**Proposed:** `AiracCycleReadiness readiness = AiracCycleDataCache.Instance.ComputeReadiness();`

**Future use:** unlikely.

<a id="w3-20"></a>

### W3-20 · Unused · Low · ShellViewModel.VersionBrushKey and IsOnline are bindable properties nothing binds

**Status:** Fixed (not yet merged). `VersionBrushKey` is deleted: `VersionIsWarning` is worked out from `IsUpdateAvailable` and the declined flag. `IsOnline` is a local · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs:66-67`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs#L66-L67), 218-243, 590-651

**Problem:** ShellWindow.xaml binds `VersionIsWarning`, never `VersionBrushKey` or `IsOnline`. `VersionBrushKey` is only a string flag behind `VersionIsWarning` (set 7 times), so its doc ("Resource key for the version text brush") describes a use that doesn't exist; `IsOnline` only feeds one tooltip line.

**Proposed:** Replace `VersionBrushKey`/`_versionBrushKey` with `public bool VersionIsWarning => IsUpdateAvailable && _updateDeclinedThisSession;` (raise it where `IsUpdateAvailable`/`_updateDeclinedThisSession` change) and drop the 7 assignments; replace `IsOnline` with a local `bool online = AppEnvironment.HasInternetConnection;` in RefreshVersionState.

**Future use:** unlikely.

<a id="w5-17"></a>

### W5-17 · Unused · Low · Six x:Name elements are never referenced

**Status:** Fixed (not yet merged). The six names are deleted · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/AirwaysView.xaml:23`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/AirwaysView.xaml#L23) (`Root`); [`MapWorkspace.xaml:342`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/MapWorkspace.xaml#L342) (`OutputGear`); [`ResetWindow.xaml:38`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ResetWindow.xaml#L38) (`Body`); [`ShellWindow.xaml:300`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ShellWindow.xaml#L300) (`MinimizeButton`), 310 (`CloseButton`), 327 (`NavRail`)

**Problem:** None of them is used by an ElementName, a TargetName, its code-behind, or a test. They generate unused fields. (ImportSettingsWindow's and UninstallWindow's `Body`, and ConcatenateAliasesView's `Root`, are used.)

**Proposed:** remove those six `x:Name` attributes.

**Future use:** unlikely.

<a id="w5-18"></a>

### W5-18 · Unused · Low · GeojsonFilesCard: Footnote and the three *Label properties are never set

**Status:** Fixed (not yet merged). `Footnote`, its TextBlock and the three labels are deleted; the check boxes say Lines, Symbols and Text · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/Cards/GeojsonFilesCard.xaml:8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/Cards/GeojsonFilesCard.xaml#L8),40,45,50,54-57; [`GeojsonFilesCard.xaml.cs:9-10`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/Cards/GeojsonFilesCard.xaml.cs#L9-L10),18-19,27-28,36-37,50-51,59-60,68-69,77-78

**Problem:** No view sets `Footnote`, `LinesLabel`, `SymbolsLabel` or `TextLabel`; all of them are always at their defaults. The Footnote TextBlock is always collapsed. Departures and Arrivals use `Footer` instead.

**Proposed:** delete the Footnote property and its TextBlock (lines 54-57) and the header comment's Footnote sentence. Delete the three *Label properties and put "Lines", "Symbols" and "Text" in the CheckBoxes directly.

**Future use:** unlikely; Footer already covers longer notes.

<a id="w7-16"></a>

### W7-16 · Unused · Low · Five explicit `Themed.ComboBox` references repeat what the implicit style already does

**Status:** Fixed (not yet merged). The five explicit references are deleted; the implicit style still applies · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/ConcatenateAliasesView.xaml:147`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ConcatenateAliasesView.xaml#L147); [`Views/SettingsView.xaml:86`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/SettingsView.xaml#L86),105,289; [`Views/AirwaysView.xaml:122`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/AirwaysView.xaml#L122)

**Problem:** `<Style TargetType="ComboBox" BasedOn="{StaticResource Themed.ComboBox}" />` (Controls.Inputs.xaml:589) gives every ComboBox the same style; the other 18 ComboBoxes rely on it.

**Proposed:** Remove `Style="{StaticResource Themed.ComboBox}"` from those five ComboBoxes.

**Future use:** n/a (redundant attribute).

<a id="w5-19"></a>

### W5-19 · Unused · Low · Dashboard log row trigger for Info sets the default again

**Status:** Fixed (not yet merged). The Info trigger is deleted · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/DashboardView.xaml:72-74`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/DashboardView.xaml#L72-L74)

**Problem:** The `Tick` border already has `Background="{StaticResource Brush.Text.Tertiary}"` (line 54), so the trigger changes nothing.

**Proposed:** delete lines 72-74.

**Future use:** none.

## Reported more than once

Each review slice was read by its own reviewer, so some findings were reported twice. These IDs point at the finding that is kept.

- <a id="c2-25"></a>**C2-25** · AirwayPoint.FoundIn is filled by AirwayBuilder and never read · same as [C5-6](#c5-6).
- <a id="o1-18"></a>**O1-18** · ServiceResult.Warnings is used only by the Harness's Airways printer and tests · same as [C1-13](#c1-13).
- <a id="t1a-33"></a>**T1a-33** · `RoutePointArtccResolver.AmbiguousPoints` is read only by tests · same as [C4-18](#c4-18).
- <a id="t1a-34"></a>**T1a-34** · `ProcedureRouteSet.Routes` is read only by a test · same as [C4-19](#c4-19).
- <a id="t1b-24"></a>**T1b-24** · ArtccBoundaryOutputFiles.ClassFor(string) is an identity function only a test calls · same as [C3-20](#c3-20).
- <a id="t1b-25"></a>**T1b-25** · AiracSharedDataLoader's telephony overloads: one unused, one test-only · same as [C1-7](#c1-7).
- <a id="t2-10"></a>**T2-10** · `TelephonyHtmlParser.Parse` (both pages into one collection) is used only by tests · same as [C7-17](#c7-17).
- <a id="t2-11"></a>**T2-11** · `InstalledProduct.ProductSemVer` is read only by a test · same as [C7-16](#c7-16).
- <a id="t2-12"></a>**T2-12** · `UserConfigFile.ProfileExists`, `AppLog.GetLogFilePath` and `UserConfigMigrationContext.ChangeValue` have no production caller · same as [C6-25](#c6-25), [C6-26](#c6-26) and [C6-27](#c6-27).
- <a id="t3-22"></a>**T3-22** · GeoJsonReader's "Not laid out as GeoJSON" branch looks unreachable and is untested · same as [W6-24](#w6-24).
- <a id="t3-23"></a>**T3-23** · CommandTablePanel.Rules exists only for the tests · same as [W6-22](#w6-22).
- <a id="w2-13"></a>**W2-13** · `SubServiceDescriptor.AliasFileName` is never read · same as [W4-10](#w4-10).
