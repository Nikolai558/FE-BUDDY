# 300 · Unit tests

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

Unit tests: names and comments that don't match what they test, unused helpers, and tests for code [D1](010_Decisions.md#d1) proposes removing. Grouped by folder. Session [S9](020_Work-Plan.md#s9).

**106 findings** (7 Med, 99 Low). Quote the ID when you comment on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374).

## Summary

| ID | Kind | Sev | Finding | Status | Session |
|---|---|---|---|---|---|
| [T2-14](#t2-14) | Wrong doc | Low | Test csproj: only "the map's logic" in FeBuddy.Wpf is tested | Open | S9 |
| [T2-15](#t2-15) | Wrong doc | Low | "every CSV FE-Buddy reads": the NASR fixtures are every CSV FE-Buddy has a parser for | Open | S9 |
| [T2-30](#t2-30) | Unused | Low | Test csproj repeats Core's package references; GeoJSON4STJ is used by no test | Open | S9 |
| [T1b-10](#t1b-10) | Minor | Low | AiracCycleDataCacheTests summary lists only part of what the class covers | Open | S9 |
| [T1b-7](#t1b-7) | Stale doc | Low | "Wx Stations" section header over the parameterless-constructor tests | Open | S9 |
| [T1b-8](#t1b-8) | Wrong doc | Low | `configure_for_testing_swaps_and_restores_the_shared_instance` asserts it does NOT restore it | Open | S9 |
| [T1b-9](#t1b-9) | Wrong doc | Low | Two "before_download" tests use a cycle that is never downloaded because it is unpublished | Open | S9 |
| [T1b-12](#t1b-12) | Wrong doc | Low | `an_unknown_kind_has_no_suffix` asserts a throw | Open | S9 |
| [T1b-13](#t1b-13) | Unused | Low | Duplicate Wx Stations test in AiracServiceTests | Open | S9 |
| [T1b-14](#t1b-14) | Unused | Low | Five `a_null_X_block_leaves_result_X_null` tests set an already-null block | Open | S9 |
| [T1b-15](#t1b-15) | Wrong doc | Low | `NutiyAtOrf` says ORF publishes a NUTIY arrival | Open | S9 |
| [T1b-16](#t1b-16) | Wrong doc | Low | Renamed-files test summary claims "every message" uses the new names | Open | S9 |
| [T1b-17](#t1b-17) | Minor | Low | AiracServiceTests: a progress test that also tests cancellation; spacing | Open | S9 |
| [T1b-18](#t1b-18) | History wording | Low | Issue numbers in test comments | Open | S9 |
| [T1b-19](#t1b-19) | History wording | Low | Airport card test narrates where the airspace line "moved" from | Open | S9 |
| [T1b-34](#t1b-34) | Minor | Low | Redundant assertion in AirportBuilderTests | Open | S9 |
| [T1b-32](#t1b-32) | Unused | Low | AirportServiceTests duplicates an alias-writer test | Open | S9 |
| [T1b-33](#t1b-33) | Minor | Low | AirportServiceTests holds two AirportSettingsParser tests | Open | S9 |
| [T1b-20](#t1b-20) | History wording | Low | "A block without Area means what it did before" | Open | S9 |
| [T1a-5](#t1a-5) | Wrong doc | Low | "before any geometry work" isn't what the test checks | Open | S9 |
| [T1a-9](#t1a-9) | Unused | Low | Unused array-key branch in AirwayGeojsonFebPropertiesTests.PropertiesById | Open | S9 |
| [T1a-3](#t1a-3) | Wrong doc | Low | Mid-airway test's comment claims something its assertion does not check | Open | S9 |
| [T1a-1](#t1a-1) | Wrong doc | Low | AirwayMessageLevelsTests summary claims a test that does not exist | Open | S9 |
| [T1a-2](#t1a-2) | Unused | Low | Unknown-key warning tested twice for Airways | Open | S9 |
| [T1a-6](#t1a-6) | Wrong doc | Low | Two AirwayServiceTests names promise more than they assert | Open | S9 |
| [T1a-7](#t1a-7) | Wrong doc | Low | AirwayServiceTests summary doesn't cover the file, and two tests don't run the pipeline | Open | S9 |
| [T1a-8](#t1a-8) | Unused | Low | FebProperties.Name tested in three service-test files, including an enum "fallback" that doesn't exist | Open | S9 |
| [T1a-4](#t1a-4) | Wrong doc | Low | `crc_defaults_require_filters_for_every_class_and_kind` tests neither filters nor every class | Open | S9 |
| [T1a-10](#t1a-10) | Unused | Low | `AirwayTestDataBuilder.Segment`'s `gapFlag` parameter is never passed | Open | S9 |
| [T1a-13](#t1a-13) | Unused | Low | Unparseable-amendment Fact duplicates the Cycles row of the "in every mode" Theory | Open | S9 |
| [T1a-12](#t1a-12) | Wrong doc | Low | The "parse in the order given" tests can't tell given order from enum order, and are duplicated | Open | S9 |
| [T1a-15](#t1a-15) | Wrong doc | Low | Arrival/Departure *ServiceTests summaries say "the whole pipeline" but half the tests call writers directly | Open | S9 |
| [T1a-11](#t1a-11) | History wording | Low | "is_now_an_unknown_key" narrates history | Open | S9 |
| [T1a-21](#t1a-21) | History wording | Low | Feature, bug and issue numbers in test comments | Open | S9 |
| [T1a-16](#t1a-16) | Minor | Low | BLAID2 fixes are around Las Vegas, not "SoCal/Utah" | Open | S9 |
| [T1a-18](#t1a-18) | Unused | Low | ArrivalTestData parameters no test passes | Open | S9 |
| [T1a-14](#t1a-14) | Wrong doc | Low | DepartureFilterTests summary leaves out the ROI filter it tests | Open | S9 |
| [T1a-17](#t1a-17) | Minor | Low | DotssBodies doc leaves out the runway end in the tuple | Open | S9 |
| [T1a-19](#t1a-19) | Unused | Low | DepartureTestData parameters no test passes (and obstacle-departure reading is untested) | Open | S9 |
| [T1b-28](#t1b-28) | Wrong doc | Low | FixServiceTests: ExcludedFixUses is not "the only way" to write nothing | Open | S9 |
| [T1b-22](#t1b-22) | Unused | Low | Five unused fix builders in FixTestData | Open | S9 |
| [T1b-35](#t1b-35) | Minor | Low | Test names that don't match their assertions | Open | S9 |
| [T1b-1](#t1b-1) | Wrong doc | Med | NavaidFilterTests says ExcludedTypes narrows every output; it narrows the GeoJSON only | Open | S9 |
| [T1b-2](#t1b-2) | Unused | Med | Four "fan_marker_style_is_not_required_when..." tests are vacuous | Open | S9 |
| [T1b-29](#t1b-29) | Wrong doc | Low | "emit_symbols_or_emit_text_off" tests only turn EmitText off | Open | S9 |
| [T1b-3](#t1b-3) | Wrong doc | Low | NavaidSettingsParserTests summary says SymbolStyleBy/FanMarkerStyle are "required" | Open | S9 |
| [T1a-22](#t1a-22) | Stale doc | Med | ProcedureChangesMarkdownWriterTests summary still promises a de-duplication rule the writer dropped | Open | S9 |
| [T1a-29](#t1a-29) | Wrong doc | Low | ProcedureOrderingTests comment says the identifiers are the same; they're opposite | Open | S9 |
| [T1a-30](#t1a-30) | Wrong doc | Low | ProcedureOutputFilesTests: the names aren't fixed, and there are three | Open | S9 |
| [T1a-27](#t1a-27) | History wording | Low | "the full name an earlier FE-Buddy saved" | Open | S9 |
| [T1a-28](#t1a-28) | Wrong doc | Low | Comment cites the default ChartTypes in a test that overrides them | Open | S9 |
| [T1a-26](#t1a-26) | History wording | Low | "as it was before" / "as before" in ProcedureServiceFixInFacilityTests | Open | S9 |
| [T1a-25](#t1a-25) | Unused | Low | `the_alias_file_goes_in_the_aliases_folder` is a subset of the test above it | Open | S9 |
| [T1a-23](#t1a-23) | Stale doc | Low | ProcedureSettingsParserTests summary predates the Chart Recall alias file | Open | S9 |
| [T1a-24](#t1a-24) | Unused | Low | Default-value tests in ProcedureSettingsParserTests repeat the minimal-block test | Open | S9 |
| [T1a-31](#t1a-31) | Unused | Low | ProcedureTestData parameters no test passes (and the builder's airport fields are untested) | Open | S9 |
| [T1a-32](#t1a-32) | Wrong doc | Low | BuiltProcedure doc: a Deleted procedure has no current PDF | Open | S9 |
| [T1b-30](#t1b-30) | Wrong doc | Low | TelephonySettingsParserTests: two names promise what they don't assert | Open | S9 |
| [T1b-31](#t1b-31) | Wrong doc | Low | VatsimRadarVirtualAirlinesTests: "C, PHENX and TROY" summary leaves out NUL | Open | S9 |
| [T1b-23](#t1b-23) | Unused | Low | WxStationTestData.Bare and Build's numResults parameter are unused | Open | S9 |
| [T1c-21](#t1c-21) | Minor | Low | Redundant assertion in the help-command practice test | Open | S9 |
| [T1c-20](#t1c-20) | Unused | Low | The same synchronous IProgress helper is copied into four test classes | Open | S9 |
| [T1c-22](#t1c-22) | Minor | Low | ERAM test name says CRC auto-assigns everything but filters (Text's `text` isn't either) | Open | S9 |
| [T1c-10](#t1c-10) | History wording | Low | Issue numbers for features (not bugs) in test comments | Open | S9 |
| [T1c-11](#t1c-11) | Wrong doc | Low | LaunchSequenceTests' summary doesn't cover 8 of its tests, which test AiracService and AppEnvironment | Open | S9 |
| [T1c-9](#t1c-9) | History wording | Low | "now actually reaches" narrates an earlier state of the test | Open | S9 |
| [T1c-3](#t1c-3) | Wrong doc | Med | Version check test says a missing current version is never offered an update, but it asserts the opposite | Open | S9 |
| [T1c-16](#t1c-16) | Wrong doc | Low | "No update lists no releases" says it covers a build ahead of every release; it doesn't | Open | S9 |
| [T1c-23](#t1c-23) | Minor | Low | Ambiguous summary in the newer-releases test | Open | S9 |
| [T1c-8](#t1c-8) | History wording | Low | Version check test comments tied to today's release list | Open | S9 |
| [T1c-19](#t1c-19) | Unused | Low | Duplicate airway-classification test | Open | S9 |
| [T1c-1](#t1c-1) | Wrong doc | Med | Arrival test name says the code id comes from the transition side; it comes from the procedure side | Open | S9 |
| [T1c-12](#t1c-12) | Stale doc | Low | CRC validator test comment names AirwaySettingsParser as what normalizes style case | Open | S9 |
| [T1c-14](#t1c-14) | Wrong doc | Low | Antimeridian test says the far side is dropped; it isn't | Open | S9 |
| [T1c-15](#t1c-15) | Wrong doc | Low | Two antimeridian cases never reach the split they claim to test | Open | S9 |
| [T1c-13](#t1c-13) | Wrong doc | Low | GeoMath test summary says DMS coordinates; the test uses decimals | Open | S9 |
| [T1c-25](#t1c-25) | Minor | Low | GeoMathTests: typos, empty and typed `<param>` docs, `; ;`, underscore-prefixed locals | Open | S9 |
| [T1c-18](#t1c-18) | Unused | Low | Unused `Factory` field in LineStringMergerTests | Open | S9 |
| [T1c-24](#t1c-24) | Minor | Low | `HandlerCoordinates` is a leftover name | Open | S9 |
| [T1c-17](#t1c-17) | Wrong doc | Low | Class summaries that leave out what the class also tests | Open | S9 |
| [T1c-2](#t1c-2) | Wrong doc | Med | Chart Recall test says a procedure's code always uses the "right half" of the computer code | Open | S9 |
| [T3-12](#t3-12) | Wrong doc | Low | MapCanvasTests: the pixel scale in `Looking`'s doc is off by about 4x | Open | S9 |
| [T3-16](#t3-16) | Wrong doc | Low | Test class summaries that leave out much of what the class tests | Open | S9 |
| [T3-24](#t3-24) | History wording | Low | Issue and feature numbers in test docs | Open | S9 |
| [T3-9](#t3-9) | Wrong doc | Med | AiracGeneralTabViewModelTests summary says the last output is "kept on" | Open | S9 |
| [T3-19](#t3-19) | Unused | Low | A test that cannot fail: the alias guide's export commands "can be executed" | Open | S9 |
| [T3-25](#t3-25) | History wording | Low | "under its first name ... an earlier off carries over" | Open | S9 |
| [T3-34](#t3-34) | Minor | Low | DevLaunchCardViewModelTests' constructor and Dispose have no docs and Dispose swallows every exception | Open | S9 |
| [T3-20](#t3-20) | Unused | Low | EramToGeojsonViewModelTests sets choices it never checks | Open | S9 |
| [T3-14](#t3-14) | Wrong doc | Low | FileConversionsViewModelTests: Source does not pick its top choice | Open | S9 |
| [T3-32](#t3-32) | Minor | Low | `nothing_is_refused` asserts that null is refused | Open | S9 |
| [T3-11](#t3-11) | Wrong doc | Low | InfoViewModelTests says the guide reads the facility | Open | S9 |
| [T3-27](#t3-27) | History wording | Low | "as before" | Open | S9 |
| [T3-21](#t3-21) | Unused | Low | Unneeded setup in a help-command test | Open | S9 |
| [T3-26](#t3-26) | History wording | Low | "names an earlier FE-Buddy saved in full" | Open | S9 |
| [T3-10](#t3-10) | Wrong doc | Low | ProceduresViewModelTests: a test's summary names the ROI, the test uses "None" | Open | S9 |
| [T3-33](#t3-33) | Minor | Low | ProceduresViewModelTests calls the tab "the Procedures tab" | Open | S9 |
| [T3-13](#t3-13) | Wrong doc | Low | WhatYoullGetTests summary gives the wrong chip for a narrowing | Open | S9 |
| [T3-17](#t3-17) | Wrong doc | Low | The STAR/STR test claims more than it asserts | Open | S9 |
| [T3-18](#t3-18) | Stale doc | Low | WhatsNewViewModelTests section header names an Export that doesn't exist | Open | S9 |
| [T3-35](#t3-35) | Minor | Low | Tests that pin GUI wording word for word (list below) | Open | S9 |
| [N1-38](#n1-38) | History wording | Low | Tests and comments added since the review cite finding IDs and narrate history | Open | S14 |
| [N1-39](#n1-39) | Wrong doc | Low | AiracGeneralTabViewModelTests: the "older build" case uses a key no release saved | Fixed | S14 |
| [N1-40](#n1-40) | Minor | Low | New tests: misplaced summaries, wall-clock limits, an untested Telephony guard | Fixed | S14 |
| [N1-41](#n1-41) | Minor | Low | DeferredSave's timer, close and retry untested; a save-failure test fails only at its first write | Open | S14 |
| [N1-42](#n1-42) | Minor | Low | New tests that can't catch what they guard (update watcher, FieldState, Telephony name) | Open | S14 |

## FeBuddy.UnitTests

<a id="t2-14"></a>

### T2-14 · Wrong doc · Low · Test csproj: only "the map's logic" in FeBuddy.Wpf is tested

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/FeBuddy.UnitTests.csproj:11-12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/FeBuddy.UnitTests.csproj#L11-L12) (same claim in [`FeBuddy/coverlet.runsettings:7-8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/coverlet.runsettings#L7-L8))

**Says:** "The map's logic in FeBuddy.Wpf is tested too (see Wpf/). Coverage is still measured on FeBuddy.Core and FeBuddy.Versioning only"

**Problem:** UT/Wpf/ tests 38 view-model files plus behaviors, controls, converters, shell services and the map. "still" narrates.

**Proposed:** "FeBuddy.Wpf's view-models, controls, behaviors, converters, map and shell services are tested too (see Wpf/). Coverage is measured on FeBuddy.Core and FeBuddy.Versioning only (coverlet.runsettings)." runsettings 7-8: "The tests reach FeBuddy.Wpf too (FeBuddy.UnitTests/Wpf), but it is not measured: most of it is XAML and view code that no unit test runs."

<a id="t2-15"></a>

### T2-15 · Wrong doc · Low · "every CSV FE-Buddy reads": the NASR fixtures are every CSV FE-Buddy has a parser for

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/FeBuddy.UnitTests.csproj:44`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/FeBuddy.UnitTests.csproj#L44); [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Nasr/Parsers/NasrCsvParserMappingTests.cs:27`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Nasr/Parsers/NasrCsvParserMappingTests.cs#L27)

**Says:** "header + first 3 records of every CSV FE-Buddy reads" / "Every NASR file FE-Buddy reads, with the parser call that reads it."

**Problem:** FE-Buddy parses only `NasrCsvGroups.InUse` (9 of 24 groups; NasrCsvGroupParseTests.cs:64-70, Architecture.md:73-77). The 63 fixtures are every file of all 24 groups, from cycle 2609.

**Proposed:** csproj -> "A trimmed real NASR cycle, 2609 (header + first 3 records of every CSV FE-Buddy has a parser for)."; test -> "Every NASR file FE-Buddy has a parser for, with the parser call that reads it."

<a id="t2-30"></a>

### T2-30 · Unused · Low · Test csproj repeats Core's package references; GeoJSON4STJ is used by no test

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/FeBuddy.UnitTests.csproj:23`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/FeBuddy.UnitTests.csproj#L23), 25-26

**Problem:** CsvHelper, NetTopologySuite and NetTopologySuite.IO.GeoJSON4STJ already flow in through the FeBuddy.Core ProjectReference (FeBuddy.Core.csproj:24-26, no PrivateAssets), so each version must be bumped twice; no test uses NetTopologySuite.IO at all.

**Proposed:** Delete the three PackageReferences (at least GeoJSON4STJ).

<a id="n1-38"></a>

### N1-38 · History wording · Low · Tests and comments added since the review cite finding IDs and narrate history

**Status:** Open; found in the review of the S2 to S5 code at `4893e32` · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** Finding IDs: [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportBuilderTests.cs:314-315`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportBuilderTests.cs#L314-L315); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Telephony/TelephonyBuilderTests.cs:469`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Application/Airac/Telephony/TelephonyBuilderTests.cs#L469); [`FeBuddy/FeBuddy.UnitTests/Application/Settings/CrcDefaultsReaderTests.cs:246`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Application/Settings/CrcDefaultsReaderTests.cs#L246); [`FeBuddy/FeBuddy.UnitTests/Application/Updates/VersionCheckResultTests.cs:7`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Application/Updates/VersionCheckResultTests.cs#L7); [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigProfilesTests.cs:259`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigProfilesTests.cs#L259); [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Logging/AppLogTests.cs:259`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Infrastructure/Logging/AppLogTests.cs#L259); [`FeBuddy/FeBuddy.UnitTests/Repository/InstallerVersionScriptTests.cs:7`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Repository/InstallerVersionScriptTests.cs#L7); [`FeBuddy/FeBuddy.UnitTests/Wpf/Behaviors/FieldStateTests.cs:10`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Wpf/Behaviors/FieldStateTests.cs#L10); [`FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/Models/EramClassDefaultTests.cs:6`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/Models/EramClassDefaultTests.cs#L6); [`FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/RunResultSummaryTests.cs:16`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/RunResultSummaryTests.cs#L16); [`FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/UserConfigLayoutTests.cs:143`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/UserConfigLayoutTests.cs#L143); [`FeBuddy/FeBuddy.Installer/Shortcuts.wxs:11`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Installer/Shortcuts.wxs#L11). History: [`FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/UserConfigLayoutTests.cs:142-149`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/UserConfigLayoutTests.cs#L142-L149); [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Platform/InstalledVersionWatcherTests.cs:52-53`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Infrastructure/Platform/InstalledVersionWatcherTests.cs#L52-L53); [`FeBuddy/FeBuddy.UnitTests/Wpf/Behaviors/FieldStateTests.cs:11`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Wpf/Behaviors/FieldStateTests.cs#L11)

**Says:** `(C2-6)`, `(W6-3)`, `(W1-7)`, `(W4-8)`, `(W3-14)`, `(W3-13)`, `(W3-16)`, `(O1-23)`, `(W5-3)`, `(W2-37)`, `(Q1, H5)`, `an upgrade (#375).`; "which moved from … into …"; "so it reported the old version"; "which a local ToolTip … never let happen"

**Problem:** House rules [H2](010_Decisions.md#h2) (no issue, PR or finding numbers in comments) and [H3](010_Decisions.md#h3) (no history). The IDs mean nothing once this review is closed.

**Proposed:** Delete each ID in brackets (and the comma or space before it). UserConfigLayoutTests.cs:142-149: "Settings no layout step brings forward, on purpose: the General tab's, which sit in their own `Services.AiracService.General` node (a file that has them directly under `Services.AiracService` opens the General tab on its defaults), and the Settings page's Facility, coordinate precision and default ROI, which sit under `General` (a file that has them under `Services.AiracService` opens Settings with no facility, 6 decimal places and no default ROI)." InstalledVersionWatcherTests.cs:52-53: "A running FE-Buddy has its own `FE-BUDDY.dll` loaded, and Windows' version information answers from that copy, not the file on disk, so the version is read from the file itself." FieldStateTests.cs:11: "…until an error replaces it; the hint is never set as a local ToolTip, which would always win over the style."

**Note:** The older `(issue #NNN)` references are already findings for S7 to S9 (for example [W6-18](220_Comments-GUI.md#w6-18), [C6-10](200_Comments-Core.md#c6-10)); S13 searches for any left.

## FeBuddy.UnitTests/Application/Airac

<a id="t1b-10"></a>

### T1b-10 · Minor · Low · AiracCycleDataCacheTests summary lists only part of what the class covers

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracCycleDataCacheTests.cs:12-15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracCycleDataCacheTests.cs#L12-L15)

**Says:** `/// prepare order, single-flight parsing, one-retry-then-Failed, and the readiness table.`

**Problem:** Half the file (d-TPP Metafile download/reads, a dev launch's `AiracLaunchPlan`, logging, cancellation, prune) is not mentioned.

**Proposed:** `/// Exercises <see cref="AiracCycleDataCache"/> with injected steps: launch order and overlap, pruning, single-flight parsing, one-retry-then-Failed, the readiness table, the d-TPP Metafile (download, GetDtppAsync, FindDtppFile), a dev launch's plan and loading on demand, logging and cancellation.`

<a id="t1b-7"></a>

### T1b-7 · Stale doc · Low · "Wx Stations" section header over the parameterless-constructor tests

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracCycleDataCacheTests.cs:594`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracCycleDataCacheTests.cs#L594)

**Says:** `// ---- Wx Stations: parameterless constructor ----`

**Problem:** Added in 11e2d0c when the cache's parameterless constructor wired in the Wx Stations data; the cache no longer holds Wx Stations data at all (AiracCycleDataCache.cs:79-82). The two tests below are about the parameterless constructor and `ConfigureForTesting`.

**Proposed:** `// ---- the parameterless constructor and the shared instance ----`

<a id="t1b-8"></a>

### T1b-8 · Wrong doc · Low · `configure_for_testing_swaps_and_restores_the_shared_instance` asserts it does NOT restore it

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracCycleDataCacheTests.cs:606-623`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracCycleDataCacheTests.cs#L606-L623)

**Says:** `Assert.NotSame(original, AiracCycleDataCache.Instance);`

**Problem:** `ConfigureForTesting(null)` puts a new real cache in place (AiracCycleDataCache.cs:199), not the original - which the last assertion checks.

**Proposed:** rename to `configure_for_testing_swaps_in_a_cache_and_null_puts_a_new_real_one_back`.

<a id="t1b-9"></a>

### T1b-9 · Wrong doc · Low · Two "before_download" tests use a cycle that is never downloaded because it is unpublished

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracCycleDataCacheTests.cs:724-747`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracCycleDataCacheTests.cs#L724-L747), 1089-1101

**Says:** `get_dtpp_async_before_download_returns_null_without_calling_the_loader` / `find_dtpp_file_before_download_is_null` with `NotYetPublished` for 2611

**Problem:** The scenario is an unpublished cycle (its comment at 742 says so), not "before download"; a published cycle still downloading is covered separately (`get_dtpp_async_waits_for_a_cycle_still_downloading`) and behaves differently (it waits).

**Proposed:** rename to `get_dtpp_async_for_an_unpublished_cycle_returns_null_without_calling_the_loader` and `find_dtpp_file_for_an_unpublished_cycle_is_null`.

<a id="t1b-12"></a>

### T1b-12 · Wrong doc · Low · `an_unknown_kind_has_no_suffix` asserts a throw

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracOutputPathsTests.cs:63-67`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracOutputPathsTests.cs#L63-L67)

**Says:** `Assert.Throws<ArgumentOutOfRangeException>(() => AiracOutputPaths.FileKindSuffix((CrcFeatureKind)99));`

**Problem:** Name says "has no suffix" (reads like an empty string); the code throws (AiracOutputPaths.cs:113).

**Proposed:** rename to `an_unknown_kind_throws`.

<a id="t1b-13"></a>

### T1b-13 · Unused · Low · Duplicate Wx Stations test in AiracServiceTests

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracServiceTests.cs:727-744`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracServiceTests.cs#L727-L744) (and the double blank line 745-746)

**Problem:** Identical setup and assertions (the earlier test also asserts `Null(result.Fixes)`); it adds nothing.

**Proposed:** delete it (keep the `// ---- supplemental data ----` header for the two tests after it) and the extra blank line.

<a id="t1b-14"></a>

### T1b-14 · Unused · Low · Five `a_null_X_block_leaves_result_X_null` tests set an already-null block

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracServiceTests.cs:429-437`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracServiceTests.cs#L429-L437), 498-506, 556-564, 623-631, 692-700

**Problem:** `AliasOnlySettings` (46-56) sets only Airports and Departures, so each `with { X = null }` is a no-op; "an unselected block leaves its result null" is already asserted for all eight NASR sub-services by `a_procedures_block_alone_does_not_run_any_other_sub_service` (1170-1191) and for Airways by `run_async_no_sub_service_selected_warns_and_does_nothing`.

**Proposed:** delete the five tests.

<a id="t1b-15"></a>

### T1b-15 · Wrong doc · Low · `NutiyAtOrf` says ORF publishes a NUTIY arrival

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracServiceTests.cs:144-148`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracServiceTests.cs#L144-L148)

**Says:** `/// ORF genuinely publishes both a NUTIY departure and a NUTIY arrival (see` / `/// ArrivalAliasWriter.CommandName's remarks)`

**Problem:** The cited remarks say something else: "the FAA publishes ORF's NUTIY and SWOPE departures in the STAR data too" (ArrivalAliasWriter.cs:93-94; Architecture.md "lists ORF's NUTIY and SWOPE departures as STARs too").

**Proposed:** `/// The FAA lists ORF's NUTIY departure in the STAR data too (see ArrivalAliasWriter.CommandName's remarks), so both alias files write ".orfNUTIYf" - exactly the case the duplicate-alias check exists to catch.`

<a id="t1b-16"></a>

### T1b-16 · Wrong doc · Low · Renamed-files test summary claims "every message" uses the new names

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracServiceTests.cs:873`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracServiceTests.cs#L873)

**Says:** `/// <summary>A renamed alias file still goes into the combined file - and that file, the duplicate report and every message use the new names.</summary>`

**Problem:** No message is asserted.

**Proposed:** either add `Assert.Contains(result.Messages, m => m.Text.StartsWith("ZOB Telephony.txt:", StringComparison.Ordinal));` (TelephonyService's summary uses the new name) or reword to `... - and that file and the duplicate report name it as written.`

<a id="t1b-17"></a>

### T1b-17 · Minor · Low · AiracServiceTests: a progress test that also tests cancellation; spacing

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracServiceTests.cs:106-127`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracServiceTests.cs#L106-L127) (125-126), 928-929

**Says:** `run_async_with_airports_and_departures_blocks_runs_both_and_reports_progress` ends with `await Assert.ThrowsAnyAsync<OperationCanceledException>(...)`; line 928 `}` is followed directly by `[Fact]`.

**Problem:** The name does not cover the cancellation assertion; a missing blank line between two tests (and see [T1b-13](#t1b-13) for the double one).

**Proposed:** move lines 125-126 into `run_async_cancelled_before_it_starts_throws`; add a blank line after 928.

<a id="t1b-18"></a>

### T1b-18 · History wording · Low · Issue numbers in test comments

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracServiceTests.cs:199`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracServiceTests.cs#L199); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/DuplicateAliasChoicesTests.cs:7`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/DuplicateAliasChoicesTests.cs#L7); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportAliasWriterTests.cs:241`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportAliasWriterTests.cs#L241); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportSettingsParserTests.cs:59`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportSettingsParserTests.cs#L59); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Telephony/TelephonyBuilderTests.cs:18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Telephony/TelephonyBuilderTests.cs#L18), 321; [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Telephony/TelephonyServiceTests.cs:318`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Telephony/TelephonyServiceTests.cs#L318); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Telephony/TelephonyAliasWriterTests.cs:152`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Telephony/TelephonyAliasWriterTests.cs#L152); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Navaids/NavaidAliasWriterTests.cs:114`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Navaids/NavaidAliasWriterTests.cs#L114)

**Says:** `// ---- choosing for duplicate alias commands (#318) ----`; `as asked for in issue #328.`; `(issues #336, #339)`

**Problem:** Task/issue numbers that only say where a feature came from (house rule: comments don't point at task numbers). None explains a bug-driven choice.

**Proposed:** AiracServiceTests:199 `// ---- choosing for duplicate alias commands ----`; DuplicateAliasChoicesTests:7 drop ` (issue #318)`; AirportAliasWriterTests:241 `/// <summary>The written command for a field with no tower, runway or airspace.</summary>`; AirportSettingsParserTests:59 `// ---- the area: the ROI or everything ----`; TelephonyBuilderTests:17-18 `... And only the first entry for each 3LD and telephony written: a real operator's over any virtual airline, the user's own over the list's.`; TelephonyBuilderTests:321 `/// A virtual airline with a real operator's 3LD and telephony - whatever its`; TelephonyServiceTests:318 drop ` (issue #339)`; TelephonyAliasWriterTests:152 and NavaidAliasWriterTests:114 `/// <summary>The layout spelled out in full.</summary>`. (Production has the same pattern, e.g. AiracService.cs:555 and DuplicateAliasChoices.cs:9 "(issue [#318](https://github.com/Nikolai558/FE-BUDDY/issues/318))", TelephonyBuilder.cs:25 "(issues [#336](https://github.com/Nikolai558/FE-BUDDY/issues/336), [#339](https://github.com/Nikolai558/FE-BUDDY/issues/339))" - outside T1b.)

## FeBuddy.UnitTests/Application/Airac/Airports

<a id="t1b-19"></a>

### T1b-19 · History wording · Low · Airport card test narrates where the airspace line "moved" from

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportAliasWriterTests.cs:211-216`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportAliasWriterTests.cs#L211-L216)

**Says:** `/// The whole card, as it was asked for: ... then` / `/// the airspace (moved down from after PTRN ALT) with its hours`

**Problem:** "as it was asked for" and "moved down from after PTRN ALT" describe the old layout/request, not the card.

**Proposed:** `/// The whole card: a blank line above it, attendance hours after WX, then the airspace with its hours, each extra line lined up under the first value, and a line break after it. The space in <c>ATNDCE HRS:</c> is <c>\s</c>, as in <c>FAC\sTYPE:</c>, so CRC keeps it.`

<a id="t1b-34"></a>

### T1b-34 · Minor · Low · Redundant assertion in AirportBuilderTests

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportBuilderTests.cs:157-158`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportBuilderTests.cs#L157-L158)

**Says:** `Assert.Equal(LogLevel.Info, message.Level);` / `Assert.NotEqual(LogLevel.Warning, message.Level);`

**Problem:** The second line can never fail after the first.

**Proposed:** delete line 158.

<a id="t1b-32"></a>

### T1b-32 · Unused · Low · AirportServiceTests duplicates an alias-writer test

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportServiceTests.cs:214-223`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportServiceTests.cs#L214-L223)

**Problem:** Same call and assertions as `AirportAliasWriterTests.nothing_is_written_when_there_are_no_airports` (AirportAliasWriterTests.cs:353-360); it does not run the service.

**Proposed:** delete it.

<a id="t1b-33"></a>

### T1b-33 · Minor · Low · AirportServiceTests holds two AirportSettingsParser tests

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportServiceTests.cs:186-212`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportServiceTests.cs#L186-L212)

**Says:** `an_invalid_region_of_interest_is_rejected` and `a_crc_key_that_cannot_apply_is_a_warning` call `AirportSettingsParser.Parse`

**Problem:** They test the parser, not "the whole Airports pipeline" the class summary describes; AirportSettingsParserTests has neither case.

**Proposed:** move both into AirportSettingsParserTests.

<a id="t1b-20"></a>

### T1b-20 · History wording · Low · "A block without Area means what it did before"

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportSettingsParserTests.cs:96`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airports/AirportSettingsParserTests.cs#L96)

**Says:** `/// <summary>A block without <c>Area</c> means what it did before: the ROI when FilterByRoi is Y.</summary>`

**Problem:** Narrates the pre-Area (pre-beta.6) behaviour.

**Proposed:** `/// <summary>A block without <c>Area</c> (one built by hand) gets the ROI when FilterByRoi is Y, and everything otherwise.</summary>`

## FeBuddy.UnitTests/Application/Airac/Airways

<a id="t1a-5"></a>

### T1a-5 · Wrong doc · Low · "before any geometry work" isn't what the test checks

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwayBuilderTests.cs:110-123`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwayBuilderTests.cs#L110-L123)

**Says:** `public void an_excluded_designation_is_dropped_before_any_geometry_work()` ... `Assert.Empty(result.Airways);`

**Problem:** V16's waypoints resolve, so the result is the same whether the airway is dropped before or after geometry (AirwayBuilder.cs:82-87). Only "dropped" is asserted.

**Proposed:** Give V16 an unresolvable waypoint (e.g. `"AAAAA", "WP", "MISNG"`) and also assert `Assert.Empty(result.Messages.WarningTexts())` (no geometry warning means geometry never ran), or rename to `an_excluded_designation_is_dropped`.

<a id="t1a-9"></a>

### T1a-9 · Unused · Low · Unused array-key branch in AirwayGeojsonFebPropertiesTests.PropertiesById

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwayGeojsonFebPropertiesTests.cs:151-160`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwayGeojsonFebPropertiesTests.cs#L151-L160)

**Problem:** All three callers (40-41, 53-54, 63-64) key on `feb.awyId` in Lines or `feb.pointId` in Symbols. Both are strings, so the array branch never runs.

**Proposed:** `Features(directory, kind).ToDictionary(properties => properties.GetProperty(keyProperty).GetString()!)` and summary `/// Reads the Features of one file keyed by a string property.`

**Future use:** unlikely.

<a id="t1a-3"></a>

### T1a-3 · Wrong doc · Low · Mid-airway test's comment claims something its assertion does not check

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwayGeometryBuilderTests.cs:133-135`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwayGeometryBuilderTests.cs#L133-L135)

**Says:** `// Processing kept going past the bad record (the CCCCC->DDDDD leg was still built),` / `Assert.NotEmpty(result.LineStrings);`

**Problem:** `AirwayGeometryBuilder.Build` flushes AAAAA-BBBBB into `lineStrings` when it meets NOWHERE (AirwayGeometryBuilder.cs:82), so `NotEmpty` passes even if processing stopped at NOWHERE. The assertion doesn't show the CCCCC->DDDDD leg was built.

**Proposed:** `Assert.Equal(2, result.LineStrings.Count); // AAAAA-BBBBB, and CCCCC-DDDDD after the bad record` (or reword the comment to what is asserted).

<a id="t1a-1"></a>

### T1a-1 · Wrong doc · Low · AirwayMessageLevelsTests summary claims a test that does not exist

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwayMessageLevelsTests.cs:16-21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwayMessageLevelsTests.cs#L16-L21)

**Says:** `/// ... and a run with only Info notices reports zero warnings and mirrors every message to <see cref="AppLog"/>.`

**Problem:** No test runs a run that has only Info notices. `a_leg_too_short_to_buffer_is_reported_as_info_not_warning` (35-53) calls `AirwayWaypointBuffer.Buffer` directly, not a run, and the only AppLog check (91) is for a Warning, in `airway_exclusion_is_a_warning_and_the_run_mirrors_messages_to_applog`.

**Proposed:** `/// Verifies the airway message levels: the buffer's "leg too short" notice is Info (not Warning), airway exclusion and unrecognized-key notices are Warning, and <see cref="AirwayService.Run"/> mirrors its messages to <see cref="AppLog"/>.`

<a id="t1a-2"></a>

### T1a-2 · Unused · Low · Unknown-key warning tested twice for Airways

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwayMessageLevelsTests.cs:55-68`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwayMessageLevelsTests.cs#L55-L68); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwaySettingsParserTests.cs:502-511`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwaySettingsParserTests.cs#L502-L511)

**Problem:** Both parse a minimal block plus one made-up key and assert a warning naming it. The MessageLevels one is strictly stronger (`WarningTexts()` also accepts Error, TestSupport/ServiceMessageExtensions.cs:13). Arrivals and Departures check the level and source in their own parser tests (ArrivalSettingsParserTests.cs:511-521, DepartureSettingsParserTests.cs:427-437).

**Proposed:** Put the `Level == LogLevel.Warning` assertion into `AirwaySettingsParserTests.unrecognized_key_produces_a_warning_instead_of_failing` (as Arrivals/Departures do) and delete `AirwayMessageLevelsTests.an_unrecognized_settings_key_is_a_warning_level_message`; update [T1a-1](#t1a-1)'s summary to drop "unrecognized-key".

**Future use:** unlikely.

<a id="t1a-6"></a>

### T1a-6 · Wrong doc · Low · Two AirwayServiceTests names promise more than they assert

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwayServiceTests.cs:298-313`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwayServiceTests.cs#L298-L313); :214-227

**Says:** `an_airway_id_without_leading_letters_is_grouped_as_unknown_with_one_warning` ... `Settings(("GenerateGeojson", "N"))`; `run_that_buffers_waypoints_shortens_each_leg`

**Problem:** (a) With GeoJSON off nothing is grouped, and with one such airway "one warning" (AirwayBuilder.cs:76-80 warns once per run) is untested; only `Contains` the warning is asserted. (b) Only the first coordinate of the first Lines feature is checked, not each leg.

**Proposed:** (a) add a second ID-less airway ("456") and assert `Assert.Single(result.Warnings, w => w.Contains("has no leading letters"))`, or rename to `an_airway_id_without_leading_letters_is_warned_about`. (b) Rename to `run_that_buffers_waypoints_starts_the_line_short_of_its_first_waypoint` (the next test already checks distances).

<a id="t1a-7"></a>

### T1a-7 · Wrong doc · Low · AirwayServiceTests summary doesn't cover the file, and two tests don't run the pipeline

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwayServiceTests.cs:17-21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwayServiceTests.cs#L17-L21); :338-347

**Says:** `/// Runs the whole Airways pipeline (<see cref="AirwayService.Run"/>) with the options the narrower tests leave off: CRC defaults, a region of interest (clipping, buffering, alias scope), and the advisory ...`

**Problem:** The file also covers the High/Low strata (110-143), High-file defaults (145-168), ID-less airways, unresolved segment starts and renamed files. `settings_reject_a_malformed_roi_and_warn_on_an_unknown_text_property` only calls `AirwaySettingsParser.Parse`. The FebProperties test at 349-353 is [T1a-8](#t1a-8).

**Proposed:** Summary: `/// Runs the whole Airways pipeline (<see cref="AirwayService.Run"/>): which High/Low file each designation goes in and the CRC defaults it gets, a region of interest (clipping, buffering, alias scope), excluded and unresolved airways, renamed files, and the advisory when the filters leave nothing to write.` Move `settings_reject_a_malformed_roi_and_warn_on_an_unknown_text_property` to AirwaySettingsParserTests.

<a id="t1a-8"></a>

### T1a-8 · Unused · Low · FebProperties.Name tested in three service-test files, including an enum "fallback" that doesn't exist

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwayServiceTests.cs:349-353`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwayServiceTests.cs#L349-L353); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/ArrivalServiceTests.cs:201-213`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/ArrivalServiceTests.cs#L201-L213); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Departures/DepartureServiceTests.cs:224-236`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Departures/DepartureServiceTests.cs#L224-L236)

**Problem:** `FebProperties.Name` (Application/Airac/FebProperties.cs:22-26) has no fallback branch. It lower-cases the first character of `ToString()`, so an undefined enum value tests nothing real, and the test name describes a fallback that isn't there. The shared generic helper is tested in each sub-service's *ServiceTests, which don't run it through the service.

**Proposed:** Delete `AirwayServiceTests.feb_property_names_fall_back_to_the_enum_text` and the `(…)999` rows. Optionally move one `feb_property_names_are_camel_case` theory into a new `FeBuddy.UnitTests/Application/Airac/FebPropertiesTests.cs`; the per-enum name rows are still worth keeping because they pin the user-facing `feb.*` names.

**Future use:** unlikely.

<a id="t1a-4"></a>

### T1a-4 · Wrong doc · Low · `crc_defaults_require_filters_for_every_class_and_kind` tests neither filters nor every class

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwaySettingsParserTests.cs:269-277`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/AirwaySettingsParserTests.cs#L269-L277)

**Says:** `public void crc_defaults_require_filters_for_every_class_and_kind()` ... `Assert.Throws<ArgumentException>(() => AirwaySettingsParser.Parse(settings));`

**Problem:** With no `Crc.*` keys the parse throws on the first key read, `Crc.High.Line.bcg` (CrcDefaultsReader.ReadLine reads `bcg` before `filters`, Application/Settings/CrcDefaultsReader.cs:54-55). Nothing about filters or every class/kind is asserted.

**Proposed:** Rename to `crc_defaults_are_required_for_the_files_chosen` and assert `Assert.Contains("Crc.High.Line.bcg", ex.Message)`.

## FeBuddy.UnitTests/Application/Airac/Airways/Fixtures

<a id="t1a-10"></a>

### T1a-10 · Unused · Low · `AirwayTestDataBuilder.Segment`'s `gapFlag` parameter is never passed

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/Fixtures/AirwayTestDataBuilder.cs:77-89`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Airways/Fixtures/AirwayTestDataBuilder.cs#L77-L89)

**Problem:** No caller passes `gapFlag` (AirwayNormalizerTests has its own `Seg` helper that does). Airway gaps are only tested at the normalizer/geometry level.

**Proposed:** Drop the parameter (`AwySegGapFlag = "N"`), or add a builder-level gap test that uses it.

**Future use:** unlikely.

## FeBuddy.UnitTests/Application/Airac/Arrivals

<a id="t1a-13"></a>

### T1a-13 · Unused · Low · Unparseable-amendment Fact duplicates the Cycles row of the "in every mode" Theory

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/ArrivalFilterTests.cs:153-167`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/ArrivalFilterTests.cs#L153-L167) (dup of 278-293); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Departures/DepartureFilterTests.cs:118-132`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Departures/DepartureFilterTests.cs#L118-L132) (dup of 243-258)

**Problem:** The `[MemberData(ActiveAmendmentFilters)]` theory's Cycles row builds the same procedure, the same Cycles/1 settings (`WithAmendmentFilter`) and asserts the same three things.

**Proposed:** Delete the `[Fact] an_unparseable_amendment_date_is_dropped_with_a_warning` in both files.

**Future use:** unlikely.

<a id="t1a-12"></a>

### T1a-12 · Wrong doc · Low · The "parse in the order given" tests can't tell given order from enum order, and are duplicated

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/ArrivalGeojsonPropertiesTests.cs:64-72`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/ArrivalGeojsonPropertiesTests.cs#L64-L72); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/ArrivalSettingsParserTests.cs:450-461`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/ArrivalSettingsParserTests.cs#L450-L461); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Departures/DepartureGeojsonPropertiesTests.cs:63-71`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Departures/DepartureGeojsonPropertiesTests.cs#L63-L71); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Departures/DepartureSettingsParserTests.cs:366-377`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Departures/DepartureSettingsParserTests.cs#L366-L377)

**Says:** `SettingsWith("arrivalName", "pointId", "waypoints")` -> `[ArrivalFebProperty.ArrivalName, ArrivalFebProperty.PointId, ArrivalFebProperty.Waypoints]`

**Problem:** Every list is given in enum order (ArrivalFebProperty/DepartureFebProperty: Name=0, PointId=1 … Waypoints=6), so a parser that sorted would pass too. The two *GeojsonPropertiesTests copies only test the parser, which the *SettingsParserTests already cover.

**Proposed:** In each *SettingsParserTests give an out-of-order list, e.g. `"waypoints,pointId,arrivalName"` -> `[Waypoints, PointId, ArrivalName]`; delete `point_id_and_waypoints_parse_in_the_order_given` from both *GeojsonPropertiesTests.

<a id="t1a-15"></a>

### T1a-15 · Wrong doc · Low · Arrival/Departure *ServiceTests summaries say "the whole pipeline" but half the tests call writers directly

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/ArrivalServiceTests.cs:15-19`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/ArrivalServiceTests.cs#L15-L19); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Departures/DepartureServiceTests.cs:16-20`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Departures/DepartureServiceTests.cs#L16-L20), :291-314

**Says:** `/// Runs the whole Arrivals pipeline (<see cref="ArrivalService.Run"/>) against the BLAID2 fixture and checks what lands on disk ...`

**Problem:** `a_single_point_procedure_gets_symbols_and_text_but_no_lines_file`, `geojson_generate_rejects_null_arguments`, `alias_skips_...`, `alias_with_nothing_to_write_writes_no_file` and `feb_property_names_are_camel_case` call `*GeojsonWriter.Generate`, `*AliasWriter.Generate` or `FebProperties.Name` directly. DepartureServiceTests' `a_transition_that_does_not_continue_any_body_is_drawn_as_its_own_line` tests `DepartureGeometryBuilder.Build`. Its Arrivals twin is in ArrivalGeometryBuilderTests.cs:135-159, but there is no DepartureGeometryBuilderTests.

**Proposed:** Summaries: `... and the warnings when there is nothing to write; plus the GeoJSON and alias writers called directly for the cases a fixture run can't reach.` Move the Departures geometry test into a new `Departures/DepartureGeometryBuilderTests.cs` to mirror Arrivals.

<a id="t1a-11"></a>

### T1a-11 · History wording · Low · "is_now_an_unknown_key" narrates history

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/ArrivalSettingsParserTests.cs:96-106`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/ArrivalSettingsParserTests.cs#L96-L106)

**Says:** `public void include_obstacle_departures_is_now_an_unknown_key_that_produces_a_warning()`

**Problem:** "now" implies Arrivals used to read `IncludeObstacleDepartures`. The test also duplicates `an_unrecognized_key_produces_a_warning_rather_than_failing` (511-521) with a different key.

**Proposed:** Delete it (covered by 511-521), or rename to `a_departures_only_key_is_an_unknown_key_here` and keep the existing comment.

<a id="t1a-21"></a>

### T1a-21 · History wording · Low · Feature, bug and issue numbers in test comments

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/ArrivalSettingsParserTests.cs:176`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/ArrivalSettingsParserTests.cs#L176); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/Fixtures/ProcedureTestData.cs:66`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/Fixtures/ProcedureTestData.cs#L66); [`.../Procedures/ProcedureBuilderTests.cs:637`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureBuilderTests.cs#L637); [`.../Procedures/ProcedureChangesMarkdownWriterTests.cs:14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureChangesMarkdownWriterTests.cs#L14), 279, 520, 595; [`.../Procedures/ProcedureSelectionTests.cs:398`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureSelectionTests.cs#L398), 421; [`.../Procedures/ProcedureServiceFixInFacilityTests.cs:15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureServiceFixInFacilityTests.cs#L15), 132; [`.../Procedures/ProcedureSettingsParserTests.cs:371`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureSettingsParserTests.cs#L371); [`.../Procedures/ProceduresJsonWriterTests.cs:419`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProceduresJsonWriterTests.cs#L419); [`.../Procedures/ProceduresWithFixInFacilityTests.cs:11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProceduresWithFixInFacilityTests.cs#L11); [`.../Procedures/RoutePointArtccResolverTests.cs:12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/RoutePointArtccResolverTests.cs#L12)

**Says:** `// ---- the area: only the filter it names applies (issue #335) ----`; `// ---- every page of a multi-page procedure (Bug #365) ----`; `/// Issue #323: each airport's list is complete, so a STAR shared by BUR and VNY shows under both,`

**Problem:** These are tracker numbers for features ([#323](https://github.com/Nikolai558/FE-BUDDY/issues/323), [#335](https://github.com/Nikolai558/FE-BUDDY/issues/335), [#367](https://github.com/Nikolai558/FE-BUDDY/issues/367), [#371](https://github.com/Nikolai558/FE-BUDDY/issues/371)) and one fixed bug ([#365](https://github.com/Nikolai558/FE-BUDDY/issues/365)). They only label or narrate history and explain no design choice. House rule: "comments ... don't point at planning docs or task numbers". `AirwayServiceTests.cs:96` "(issue #241)" stays: it is the bug-driven KEEP in the instructions.

**Proposed:** Delete the parenthesised numbers, e.g. `// ---- the area: only the filter it names applies ----`, `// ---- every page of a multi-page procedure ----`, `/// Each airport's list is complete, so a STAR shared by BUR and VNY shows under both, each noting the other.` In the summaries, drop " (Feature [#367](https://github.com/Nikolai558/FE-BUDDY/issues/367))" and " (Feature [#371](https://github.com/Nikolai558/FE-BUDDY/issues/371))".

## FeBuddy.UnitTests/Application/Airac/Arrivals/Fixtures

<a id="t1a-16"></a>

### T1a-16 · Minor · Low · BLAID2 fixes are around Las Vegas, not "SoCal/Utah"

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/Fixtures/ArrivalTestData.cs:40`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/Fixtures/ArrivalTestData.cs#L40)

**Says:** `/// <summary>BLAID2's fixes (FIX_BASE points) with invented, distinct SoCal/Utah-area coordinates.</summary>`

**Problem:** AALAN 36.42/-115.35, HOLDM 36.68/-115.52 and BLAID 36.14/-115.09 are all around Las Vegas, Nevada.

**Proposed:** `/// <summary>BLAID2's fixes (FIX_BASE points) with invented, distinct coordinates around Las Vegas.</summary>`

<a id="t1a-18"></a>

### T1a-18 · Unused · Low · ArrivalTestData parameters no test passes

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/Fixtures/ArrivalTestData.cs:168`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/Fixtures/ArrivalTestData.cs#L168) (`Base` rnavFlag), :211-212 (`Body` string overload arptRwyAssoc, pointType), :221 (`Body` tuple overload bodySeq), :232-233 (`Transition` string overload bodySeq, pointType), :243 (`Transition` tuple overload bodySeq), :279 (`Procedure` arrivalName)

**Problem:** No caller passes these. The tuple overloads are only called by `BlaidRoutes` with the defaults, and ArrivalBuilder never reads `RnavFlag` or `ArptRwyAssoc`.

**Proposed:** Remove the listed parameters and use their defaults inline.

**Future use:** unlikely.

## FeBuddy.UnitTests/Application/Airac/Departures

<a id="t1a-14"></a>

### T1a-14 · Wrong doc · Low · DepartureFilterTests summary leaves out the ROI filter it tests

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Departures/DepartureFilterTests.cs:12-14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Departures/DepartureFilterTests.cs#L12-L14)

**Says:** `/// Covers <see cref="DepartureFilter"/>: the obstacle-departure, ARTCC and amendment-date filters.`

**Problem:** Lines 291-374 test `DepartureFilter.ByRoi` (waypoint mode, airport mode, no APT_BASE row, no ROI).

**Proposed:** `/// Covers <see cref="DepartureFilter"/>: the obstacle-departure, ARTCC and amendment-date filters, and the ROI filter.`

## FeBuddy.UnitTests/Application/Airac/Departures/Fixtures

<a id="t1a-17"></a>

### T1a-17 · Minor · Low · DotssBodies doc leaves out the runway end in the tuple

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Departures/Fixtures/DepartureTestData.cs:63-64`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Departures/Fixtures/DepartureTestData.cs#L63-L64)

**Says:** `/// <summary>DOTSS2's bodies as DP_RTE lists them: route name then points in sequence.</summary>` / `(string Name, string RunwayEnd, string[] Points)`

**Problem:** 

**Proposed:** `/// <summary>DOTSS2's bodies as DP_RTE lists them: route name, the runway end DP_APT assigns it, then points in sequence.</summary>`

<a id="t1a-19"></a>

### T1a-19 · Unused · Low · DepartureTestData parameters no test passes (and obstacle-departure reading is untested)

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Departures/Fixtures/DepartureTestData.cs:88`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Departures/Fixtures/DepartureTestData.cs#L88) (`Build` navaids), :181 graphicalDpType, :183 effDate, :184 rnavFlag (`Base`), :226 (`Body` pointType), :237 (`Transition` pointType), :268 (`Procedure` dpName)

**Problem:** Nobody passes these. Because `graphicalDpType` is never `"OBSTACLE"`, `DepartureBuilder`'s `IsObstacleDeparture` read from `GRAPHICAL_DP_TYPE` (DepartureBuilder.cs:97) has no test. DepartureFilterTests builds `Procedure(isObstacleDeparture: true)` directly instead.

**Proposed:** Add a DepartureBuilderTests case `Base(..., graphicalDpType: "OBSTACLE")` -> `Assert.True(procedure.IsObstacleDeparture)` (keeps graphicalDpType); remove navaids, effDate, rnavFlag, pointType (both) and dpName.

**Future use:** unlikely, except graphicalDpType (keep it for the missing test).

## FeBuddy.UnitTests/Application/Airac/Fixes

<a id="t1b-28"></a>

### T1b-28 · Wrong doc · Low · FixServiceTests: ExcludedFixUses is not "the only way" to write nothing

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Fixes/FixServiceTests.cs:78-79`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Fixes/FixServiceTests.cs#L78-L79)

**Says:** `// ExcludedFixUses only narrows the FixUse file layout - the only way for a fix to survive` / `// building but leave the run with nothing written.`

**Problem:** An empty ROI, every chart excluded in the Chart layout, or a combination matching nothing also leave built fixes and no file (FixService.cs:80-81 says so; `a_combination_matching_no_fix_in_the_cycle_warns_and_writes_nothing` does it).

**Proposed:** `// ExcludedFixUses narrows the FixUse file layout only: one way for fixes to survive building and still leave the run with nothing written.`

## FeBuddy.UnitTests/Application/Airac/Fixes/Fixtures

<a id="t1b-22"></a>

### T1b-22 · Unused · Low · Five unused fix builders in FixTestData

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Fixes/Fixtures/FixTestData.cs:103-116`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Fixes/Fixtures/FixTestData.cs#L103-L116) (and header 82)

**Problem:** Never called; the section header also says "for the writer, filter and service tests" but no filter or service test uses a built `Fix` (only FixGeojsonWriterTests does).

**Proposed:** delete the five builders; header `// ---- already-built Fix records, for the writer tests ----`.

## FeBuddy.UnitTests/Application/Airac/Models

<a id="t1b-35"></a>

### T1b-35 · Minor · Low · Test names that don't match their assertions

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Models/CrcDefaultsFilesTests.cs:15-23`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Models/CrcDefaultsFilesTests.cs#L15-L23); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Telephony/TelephonyServiceTests.cs:105-115`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Telephony/TelephonyServiceTests.cs#L105-L115)

**Says:** `keys_match_ignoring_case` also asserts `Assert.Throws<ArgumentNullException>(() => new CrcDefaultsFiles(null!))`; `the_info_summary_message_has_the_exact_documented_text`

**Problem:** The null check is outside the name; no document holds the Telephony summary text (grep docs/ for "show more than one operator" -> nothing).

**Proposed:** move the null check into `a_null_list_is_refused`; rename the second `the_info_summary_message_has_the_exact_text`.

## FeBuddy.UnitTests/Application/Airac/Navaids

<a id="t1b-1"></a>

### T1b-1 · Wrong doc · Med · NavaidFilterTests says ExcludedTypes narrows every output; it narrows the GeoJSON only

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Navaids/NavaidFilterTests.cs:8-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Navaids/NavaidFilterTests.cs#L8-L11)

**Says:** `the settings-driven type exclusion that applies` / `to every NAVAIDs output alike (unlike the ROI, which limits GeoJSON only).`

**Problem:** The opposite is true. `NavaidFilter` (NavaidFilter.cs:5-12) "narrows the NAVAIDs GeoJSON only ... Like the ROI"; `NavaidService.Run` passes the unfiltered `buildResult.Navaids` to `NavaidAliasWriter.Generate` (NavaidService.cs:54-63); and `NavaidServiceTests.excluded_types_and_the_roi_narrow_the_geojson_only` asserts `.navOM` (an excluded FAN MARKER) is still in the alias file (NavaidServiceTests.cs:78).

**Proposed:** `/// Covers <see cref="NavaidFilter.ExcludeTypes"/>: the settings-driven type exclusion, which - like the ROI - narrows the NAVAIDs GeoJSON only; the alias file keeps every NAVAID.`

<a id="t1b-2"></a>

### T1b-2 · Unused · Med · Four "fan_marker_style_is_not_required_when..." tests are vacuous

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Navaids/NavaidSettingsParserTests.cs:256-267`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Navaids/NavaidSettingsParserTests.cs#L256-L267), 269-286, 288-298, 300-311

**Problem:** `FanMarkerStyle` is never required (NavaidSettingsParser.cs:144-162; `fan_marker_style_is_optional_even_when_symbols_get_crc_defaults_and_style_is_by_type`, lines 215-228, proves it), and none of these four tests supplies a `FanMarkerStyle`, so `Assert.Null(parsed.FanMarkerStyle)` passes whatever the parser does - they test nothing about the rule they name (the style is read only when `fanMarkerStyleUsed`). `..._when_symbols_are_not_chosen_for_crc_defaults` (288-298) is also a strict subset of `a_fully_default_settings_block_produces_no_messages` (lines 92-109: same block, asserts `Null(FanMarkerStyle)` and `Empty(SymbolDefaults)`). They have been vacuous since they were added (f9e2e66, where FanMarkerStyle was already optional).

**Proposed:** Delete `fan_marker_style_is_not_required_when_symbols_are_not_chosen_for_crc_defaults` (288-298). In the other three, set `settings["FanMarkerStyle"] = "wavy";` (an invalid value) and rename to `fan_marker_style_is_not_read_when_fan_marker_is_excluded`, `fan_marker_style_is_not_read_when_symbol_style_is_by_file_and_the_style_key_is_required_instead`, `fan_marker_style_is_not_read_in_type_mode` - then "no throw and `Assert.Null(parsed.FanMarkerStyle)`" really proves the key was not read.

<a id="t1b-29"></a>

### T1b-29 · Wrong doc · Low · "emit_symbols_or_emit_text_off" tests only turn EmitText off

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Navaids/NavaidGeojsonWriterTests.cs:99-108`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Navaids/NavaidGeojsonWriterTests.cs#L99-L108); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Fixes/FixGeojsonWriterTests.cs:111-120`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Fixes/FixGeojsonWriterTests.cs#L111-L120)

**Says:** `public void emit_symbols_or_emit_text_off_skips_that_files_kind_only()` ... `Settings() with { EmitText = false }`

**Problem:** The EmitSymbols half is never tested (WxStationGeojsonWriterTests has a test per flag).

**Proposed:** make each a `[Theory]` over `(EmitSymbols, EmitText, expected file)` = `(false, true, "_Text.geojson")`, `(true, false, "_Symbols.geojson")`, or rename to `emit_text_off_skips_the_text_files_only`.

<a id="t1b-3"></a>

### T1b-3 · Wrong doc · Low · NavaidSettingsParserTests summary says SymbolStyleBy/FanMarkerStyle are "required"

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Navaids/NavaidSettingsParserTests.cs:10-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Navaids/NavaidSettingsParserTests.cs#L10-L11)

**Says:** `<c>SymbolStyleBy</c> and <c>FanMarkerStyle</c> are required only when a merged` / `All-mode Symbols file will actually need a per-Feature style`

**Problem:** Neither is ever required: `SymbolStyleBy` defaults to `Type` (NavaidSettingsParser.cs:123,129) and `FanMarkerStyle` is optional even then (lines 144-151, and the test at 215-228).

**Proposed:** `... <c>SymbolStyleBy</c> and <c>FanMarkerStyle</c> are read only for All mode, and <c>FanMarkerStyle</c> only when a merged All-mode Symbols file gives each Feature its own style (optional even then, but checked when given), ...`

## FeBuddy.UnitTests/Application/Airac/Procedures

<a id="t1a-22"></a>

### T1a-22 · Stale doc · Med · ProcedureChangesMarkdownWriterTests summary still promises a de-duplication rule the writer dropped

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureChangesMarkdownWriterTests.cs:14-15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureChangesMarkdownWriterTests.cs#L14-L15)

**Says:** `/// every entry's exact link format, every page of a multi-page procedure (Bug #365), the` / `/// shared-chart de-duplication rule, and the CRLF/UTF-8 (no BOM) output.`

**Problem:** The writer lists a shared chart under every included airport with an "Also serves" line (ProcedureChangesMarkdownWriter.cs:17-21, 317-375), which is the opposite of de-duplicating (ChangeLog.md:252, Feature [#323](https://github.com/Nikolai558/FE-BUDDY/issues/323): "instead of under one airport only"). The tests at 519-593 assert that.

**Proposed:** `/// ... every page of a multi-page procedure, a shared chart listed under every airport it serves with "Also serves" naming the others, the "In ZDC" line for a chart included for its route's fixes, and the CRLF/UTF-8 (no BOM) output.`

<a id="t1a-29"></a>

### T1a-29 · Wrong doc · Low · ProcedureOrderingTests comment says the identifiers are the same; they're opposite

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureOrderingTests.cs:82-84`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureOrderingTests.cs#L82-L84)

**Says:** `// Same identifier ordinally so airspace rank is the only thing that could put "lower" first.` / `BuiltAirport("ZZZ", airspaceClass: higher)` / `BuiltAirport("AAA", airspaceClass: lower)`

**Problem:** The identifiers differ on purpose. "AAA" sorts first ordinally, so ordinal order would put the lower class first, and only airspace rank puts ZZZ first.

**Proposed:** `// The higher class gets the identifier that sorts last, so only airspace rank can put it first.`

<a id="t1a-30"></a>

### T1a-30 · Wrong doc · Low · ProcedureOutputFilesTests: the names aren't fixed, and there are three

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureOutputFilesTests.cs:5-13`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureOutputFilesTests.cs#L5-L13)

**Says:** `/// <summary>Covers <see cref="ProcedureOutputFiles"/>: the two fixed document names.</summary>` / `public void the_fixed_document_names_are_as_documented()`

**Problem:** `Changes` and `Json` are file keys, the default names that the File Names tab can rename (ProcedureOutputFiles.cs:3-7; ProcedureServiceTests.renamed_documents_and_the_alias_file_are_written_under_their_new_names, 301-329). The class has a third key, `Alias` = "Faa_Chart_Recall.txt", which the test doesn't pin.

**Proposed:** Summary `/// <summary>Covers <see cref="ProcedureOutputFiles"/>: its three file keys, which are also the default names.</summary>`, rename the test `the_file_keys_are_the_default_names`, and add `Assert.Equal("Faa_Chart_Recall.txt", ProcedureOutputFiles.Alias);`.

<a id="t1a-27"></a>

### T1a-27 · History wording · Low · "the full name an earlier FE-Buddy saved"

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureSelectionTests.cs:255-258`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureSelectionTests.cs#L255-L258)

**Says:** `/// pick by root name - or the full name an earlier FE-Buddy saved - still finds it.`

**Problem:** This narrates an older version. The GUI already turns a saved full name into its root on load (Wpf/ViewModels/ProceduresViewModel.cs:1142-1148), and Core matches picks by `RootName` whatever their form (ProcedureSelection.cs:75, 233-246), so no separate compat code sits behind these rows.

**Proposed:** `/// The FAA renumbers a departure or arrival at each amendment, and can add or drop its (RNAV): a pick by root name, or by a full name such as the one it was made from, still finds it.` (matches ProcedureSelection.cs:27-29).

<a id="t1a-28"></a>

### T1a-28 · Wrong doc · Low · Comment cites the default ChartTypes in a test that overrides them

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureSelectionTests.cs:493-496`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureSelectionTests.cs#L493-L496)

**Says:** `ProcedureTestData.BuiltProcedure("ONE", chartCode: "MIN"), // not in the default ChartTypes and not picked` ... `chartTypes: ["IAP"]`

**Problem:** 

**Proposed:** `// MIN isn't in ChartTypes (IAP only) and isn't picked`

<a id="t1a-26"></a>

### T1a-26 · History wording · Low · "as it was before" / "as before" in ProcedureServiceFixInFacilityTests

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureServiceFixInFacilityTests.cs:16`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureServiceFixInFacilityTests.cs#L16), 167-169, 184

**Says:** `/// the run with the option off, or no facility set, as it was before;`; `/// <summary>Off, the run writes what it wrote before the rule; ...`; `with_the_option_off_the_documents_are_as_before_and_chart_recall_never_changes`

**Problem:** These narrate the time before the fix-in-facility rule existed. The option-off test compares against no earlier output; it asserts nothing was added or labelled. (At 184 "what it did before" means the earlier run in the same test, which is fine but reads the same way.)

**Proposed:** Summary: `/// the run with the option off, or no facility set, which adds nothing;`. Doc 167: `/// <summary>Off, nothing is added or labelled for its route; Faa_Chart_Recall.txt is the same either way.</summary>`. Rename to `with_the_option_off_nothing_is_added_and_chart_recall_is_unchanged` and `with_no_facility_set_the_run_warns_once_and_writes_what_it_writes_with_the_option_off`.

<a id="t1a-25"></a>

### T1a-25 · Unused · Low · `the_alias_file_goes_in_the_aliases_folder` is a subset of the test above it

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureServiceTests.cs:256-264`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureServiceTests.cs#L256-L264) (subset of 202-215)

**Problem:** `the_alias_file_is_written_to_the_aliases_folder_by_default` runs the same scenario and settings and asserts the same path, plus its contents and counts.

**Proposed:** Delete `the_alias_file_goes_in_the_aliases_folder`.

**Future use:** unlikely.

<a id="t1a-23"></a>

### T1a-23 · Stale doc · Low · ProcedureSettingsParserTests summary predates the Chart Recall alias file

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureSettingsParserTests.cs:8-13`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureSettingsParserTests.cs#L8-L13)

**Says:** `/// ... the "both documents off" and "no inclusion source" guards, ...` / `/// entries, and that Procedures has no GeoJSON/alias/<c>feb.*</c> settings of its own.`

**Problem:** Turning both documents off is now valid (`turning_off_both_documents_with_the_alias_file_on_parses_fine`, 80-92). The guard is all three outputs off (66-78; ProcedureSettingsParser.cs:100-105). Procedures does have alias settings (`GenerateAliasFile`, `IncludeHelpCommand`, `HelpCommandName`, tested at 109-165, 533-552).

**Proposed:** `/// Covers <see cref="ProcedureSettingsParser"/>: every key, the documented defaults, the area, the "all three outputs off" and "no inclusion source" guards, the help command's name, <c>AirportProcedures</c> parsing and validation, unknown <c>ChartTypes</c>/<c>JsonFields</c> entries, and that Procedures has no GeoJSON or <c>feb.*</c> settings of its own.` (Remove "the area (and what a block without one means)" per [T1a-20](130_Old-Version-Code.md#t1a-20).)

<a id="t1a-24"></a>

### T1a-24 · Unused · Low · Default-value tests in ProcedureSettingsParserTests repeat the minimal-block test

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureSettingsParserTests.cs:454-460`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureSettingsParserTests.cs#L454-L460), 488-494, 625-631

**Problem:** `a_minimal_settings_block_produces_no_messages_and_the_documented_defaults` (32-62) already asserts ChartTypes == Default and the full eight JsonFields. `generate_alias_file_is_a_known_key_that_defaults_to_true_with_no_warning` (533-540) already asserts GenerateAliasFile is true, with no messages.

**Proposed:** Delete `chart_types_default_when_absent`, `json_fields_default_when_absent` and `generate_alias_file_defaults_to_on`. Add `Assert.True(result.Settings.GenerateAliasFile)` to the minimal-block test if wanted.

**Future use:** unlikely.

## FeBuddy.UnitTests/Application/Airac/Procedures/Fixtures

<a id="t1a-31"></a>

### T1a-31 · Unused · Low · ProcedureTestData parameters no test passes (and the builder's airport fields are untested)

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/Fixtures/ProcedureTestData.cs:130`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/Fixtures/ProcedureTestData.cs#L130) (`FixRow` useCode), :157-160 (`AirportRow` name, city, state, military)

**Problem:** No caller passes these. As a result `ProcedureBuilder`'s copy of the metafile's name, city and state and its `IsMilitary = Military == "M"` (ProcedureBuilder.cs:101-104) are never asserted. ProcedureBuilderTests only uses `AirportRow(id)` or `(id, icaoIdent)`.

**Proposed:** Add one ProcedureBuilderTests case `AirportRow("AAA", name: "X", city: "Y", state: "Z", military: "M")` asserting Name/City/State/IsMilitary (keeps those parameters). Drop `useCode` from FixRow (RoutePointArtccResolver keys on the route's point type, not FIX_USE_CODE).

**Future use:** unlikely for useCode; keep the AirportRow ones for the missing test.

<a id="t1a-32"></a>

### T1a-32 · Wrong doc · Low · BuiltProcedure doc: a Deleted procedure has no current PDF

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/Fixtures/ProcedureTestData.cs:244-250`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/Fixtures/ProcedureTestData.cs#L244-L250), 284-286

**Says:** `/// ... (a re-added one has a deleted and an added first page), and its current PDF is its first page's.` / `CurrentPdfName = change == ProcedureChange.Deleted ? null : pages.LastOrDefault(...)`

**Problem:** For `ProcedureChange.Deleted` the current PDF is `null`. Otherwise it is the last non-deleted main page, not just "its first page's" (for a re-added one, the added copy).

**Proposed:** `/// ... and its current PDF is its first page's (the added copy of a re-added one; none for a deleted one).`

## FeBuddy.UnitTests/Application/Airac/Telephony

<a id="t1b-30"></a>

### T1b-30 · Wrong doc · Low · TelephonySettingsParserTests: two names promise what they don't assert

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Telephony/TelephonySettingsParserTests.cs:33-40`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Telephony/TelephonySettingsParserTests.cs#L33-L40), 54-63, 161-168

**Says:** `a_minimal_settings_block_produces_no_messages_and_the_documented_defaults` asserts only `OutputDirectory`; `generate_alias_file_y_is_the_default_and_produces_no_warning` sets `"Y"` explicitly.

**Problem:** No default is asserted in the first; the second cannot show "Y" is the default because it sets it. `with_no_virtual_airline_keys_the_list_is_empty` (161-168) then covers a default the first test should.

**Proposed:** in the first add `Assert.Empty(result.Settings.VirtualAirlines); Assert.False(result.Settings.IncludeVatsimRadarVirtualAirlines);` and delete 161-168; rename the second `generate_alias_file_y_is_accepted_without_a_warning`.

<a id="t1b-31"></a>

### T1b-31 · Wrong doc · Low · VatsimRadarVirtualAirlinesTests: "C, PHENX and TROY" summary leaves out NUL

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Telephony/VatsimRadarVirtualAirlinesTests.cs:38`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Telephony/VatsimRadarVirtualAirlinesTests.cs#L38)

**Says:** `/// <summary>Real entries from the list: C, PHENX and TROY have no three-letter designator, so no card.</summary>`

**Problem:** The test also leaves out `NUL` (an invented entry, telephony "!!") and asserts it in `LeftOut`.

**Proposed:** `/// <summary>C, PHENX and TROY (real entries from the list) have no three-letter designator, and NUL's telephony has no letter or digit, so none gets a card.</summary>`

## FeBuddy.UnitTests/Application/Airac/WxStations/Fixtures

<a id="t1b-23"></a>

### T1b-23 · Unused · Low · WxStationTestData.Bare and Build's numResults parameter are unused

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/WxStations/Fixtures/WxStationTestData.cs:14-17`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/WxStations/Fixtures/WxStationTestData.cs#L14-L17), 106-107

**Problem:** `Bare` is never called; no caller passes `numResults`.

**Proposed:** delete `Bare`; `Build(IEnumerable<WxStationXmlDataModel.Station>? rows = null)` with `NumResults = stationList.Count`.

## FeBuddy.UnitTests/Application/AliasGuide

<a id="t1c-21"></a>

### T1c-21 · Minor · Low · Redundant assertion in the help-command practice test

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/AliasGuide/AliasPracticeContentTests.cs:110-111`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/AliasGuide/AliasPracticeContentTests.cs#L110-L111)

**Says:** `Assert.Equal([".helpCHARTRECALL"], help.Answers.Select(Flat));`&lt;br>`Assert.Equal($".{ChartRecallHelpCommand.DefaultName}", Flat(help.Answers[0]));`

**Problem:** Both lines assert the same value (`DefaultName` is `"helpCHARTRECALL"`, and the first line already proves there is exactly that one answer).

**Proposed:** keep one. The `DefaultName` form says why the value is expected: `Assert.Equal([$".{ChartRecallHelpCommand.DefaultName}"], help.Answers.Select(Flat));`

## FeBuddy.UnitTests/Application/Conversions/DatToGeojson

<a id="t1c-20"></a>

### T1c-20 · Unused · Low · The same synchronous IProgress helper is copied into four test classes

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Conversions/DatToGeojson/DatToGeojsonServiceTests.cs:312-316`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Conversions/DatToGeojson/DatToGeojsonServiceTests.cs#L312-L316); [`FeBuddy/FeBuddy.UnitTests/Application/Conversions/EramToGeojson/EramToGeojsonServiceTests.cs:863-867`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Conversions/EramToGeojson/EramToGeojsonServiceTests.cs#L863-L867); [`FeBuddy/FeBuddy.UnitTests/Application/Conversions/SctToGeojson/SctToGeojsonServiceTests.cs:366-370`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Conversions/SctToGeojson/SctToGeojsonServiceTests.cs#L366-L370); [`FeBuddy/FeBuddy.UnitTests/Application/Launch/LaunchSequenceTests.cs:817-820`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Launch/LaunchSequenceTests.cs#L817-L820)

**Problem:** These are four copies of one helper. Other scopes have more: AiracServiceTests.cs:1329 `SynchronousProgress`, NasrCycleDownloaderTests.cs:40 `SyncProgress<T>`. House rule: "Something two features share ... Never copy it."

**Proposed:** add one `TestSupport/SynchronousProgress.cs` (`internal sealed class SynchronousProgress<T>(Action<T> report) : IProgress<T>`, with its summary "Reports straight away on the calling thread, so the order is exactly the order reported.") and delete the copies.

**Future use:** n/a.

## FeBuddy.UnitTests/Application/Conversions/EramToGeojson

<a id="t1c-22"></a>

### T1c-22 · Minor · Low · ERAM test name says CRC auto-assigns everything but filters (Text's `text` isn't either)

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Conversions/EramToGeojson/EramCrcPropertiesTests.cs:158`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Conversions/EramToGeojson/EramCrcPropertiesTests.cs#L158)

**Says:** `public void crc_assigns_its_own_value_to_everything_but_filters(...)`

**Problem:** `CrcPropertyValidator.TextAutoAssigned` says "`filters` and `text` are never auto-assigned", and `AutoAssigned(Text, "text")` returns null too.

**Proposed:** rename to `crc_assigns_its_own_value_to_everything_but_filters_and_text`, and optionally add `[InlineData(EramElementKind.Text, "text", null)]`.

## FeBuddy.UnitTests/Application/Launch

<a id="t1c-10"></a>

### T1c-10 · History wording · Low · Issue numbers for features (not bugs) in test comments

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Launch/LaunchSequenceTests.cs:373`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Launch/LaunchSequenceTests.cs#L373); [`FeBuddy/FeBuddy.UnitTests/Application/Settings/SubServiceSettingsReaderTests.cs:70`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Settings/SubServiceSettingsReaderTests.cs#L70)

**Says:** `/// <summary>Issue #351: the first launch leaves the welcome for the shell; a launch after it doesn't.</summary>`; `// ---- the area (issue #335) ----`

**Problem:** [#351](https://github.com/Nikolai558/FE-BUDDY/issues/351) (the welcome) and [#335](https://github.com/Nikolai558/FE-BUDDY/issues/335) (the Area card, ChangeLog.md:205 "Feature [#335](https://github.com/Nikolai558/FE-BUDDY/issues/335)") are feature requests, not bugs. The numbers only record where the work came from. By contrast, #299 (LaunchSequenceTests.cs:476, UpdateChannelSettingTests.cs:95) and #151 (SegmentJoinerTests.cs:38) name bugs the tests guard against, so they stay.

**Proposed:** "/// <summary>The first launch leaves the welcome for the shell; a launch after it doesn't.</summary>" and "// ---- the area ----". The same "(issue [#335](https://github.com/Nikolai558/FE-BUDDY/issues/335))" tag appears in other scopes' tests: ArrivalSettingsParserTests.cs:176, AirportSettingsParserTests.cs:59, WhatYoullGetTests.cs:100/212/304, AreaTests.cs:16, UserConfigAreasTests.cs:8.

<a id="t1c-11"></a>

### T1c-11 · Wrong doc · Low · LaunchSequenceTests' summary doesn't cover 8 of its tests, which test AiracService and AppEnvironment

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Launch/LaunchSequenceTests.cs:29-34`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Launch/LaunchSequenceTests.cs#L29-L34), 505-815

**Says:** `/// Runs <see cref="LaunchSequence.RunAsync(...)"/> end to end with every outside dependency stubbed`

**Problem:** `airac_service_loads_the_selected_cycle_from_the_cache`, `airac_service_loads_dtpp_and_previous_dtpp_...`, `a_run_on_a_cycle_still_loading_waits_for_it_then_runs`, `a_procedure_charts_run_waits_for_the_previous_cycles_metafile`, `airac_service_downloads_the_latest_wx_station_and_telephony_data_...`, `airac_service_reads_the_custom_alias_files_first_...` and `airac_service_does_not_read_the_custom_alias_files_...` never call `LaunchSequence`. They test `AiracService.RunAsync(AiracServiceSettings, ...)` (the cache-backed overload). `recheck_refreshes_the_online_state_and_version` tests `AppEnvironment.RecheckAsync`.

**Proposed:** move the seven `AiracService` tests to FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracServiceTests.cs (or a new AiracServiceCacheTests.cs there) and `recheck_refreshes_the_online_state_and_version` to AppEnvironmentTests.cs. Or, at least, add to the summary: "...and the cache-backed AiracService.RunAsync overload and AppEnvironment.RecheckAsync, which need the same stubs."

<a id="t1c-9"></a>

### T1c-9 · History wording · Low · "now actually reaches" narrates an earlier state of the test

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Launch/LaunchSequenceTests.cs:559-560`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Launch/LaunchSequenceTests.cs#L559-L560)

**Says:** `// Apt and ClsArsp must be non-null (even empty) for ProcedureBuilder.Build, which the`&lt;br>`// Procedures block below now actually reaches.`

**Problem:** "now actually" refers to an earlier version in which the block didn't reach `ProcedureBuilder.Build`. The facts themselves are right (ProcedureBuilder.cs:47-54 throws when Apt or ClsArsp is null).

**Proposed:** "// Apt and ClsArsp must be non-null (even empty) for ProcedureBuilder.Build, which the Procedures block below reaches."

## FeBuddy.UnitTests/Application/Updates

<a id="t1c-3"></a>

### T1c-3 · Wrong doc · Med · Version check test says a missing current version is never offered an update, but it asserts the opposite

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Updates/VersionCheckTests.cs:538-552`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Updates/VersionCheckTests.cs#L538-L552)

**Says:** `/// <summary>A dev build (unparseable current version) never claims an update is available.</summary>` ... `[InlineData("")]` ... `Assert.Equal(currentVersion == "", result.UpdateAvailable);`

**Problem:** For `""`, `VersionCheck.RunAsync` uses `"0.0.0"` (VersionCheck.cs:68: `string.IsNullOrWhiteSpace(currentVersion) ? "0.0.0"`), which parses, so 3.1.0 is offered and the test asserts `UpdateAvailable == true`. Both the name (`..._unparseable_or_missing_current_version_is_not_offered`) and the summary describe only the `"dev"` row.

**Proposed:** rename to `version_check_unparseable_current_version_is_not_offered_and_a_missing_one_reads_as_0_0_0`, with the summary: "A dev build (an unparseable current version such as `dev`) never claims an update is available; a missing one is read as 0.0.0, so any release is newer." Or split it into two tests.

<a id="t1c-16"></a>

### T1c-16 · Wrong doc · Low · "No update lists no releases" says it covers a build ahead of every release; it doesn't

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Updates/VersionCheckTests.cs:379-390`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Updates/VersionCheckTests.cs#L379-L390)

**Says:** `/// <summary>No update means no release list, including for a build ahead of every release.</summary>` ... `VersionCheck.RunAsync("3.1.0", ...)` against `v3.1.0`

**Problem:** Only the "up to date" case (current == latest) is run. No test asserts `NewerReleases` is empty for a build ahead of the latest release (`version_check_current_ahead_of_latest_release_reports_ahead` doesn't check it).

**Proposed:** make it a theory with `"3.1.0"` and `"3.2.0"` as the current version, or drop ", including for a build ahead of every release".

<a id="t1c-23"></a>

### T1c-23 · Minor · Low · Ambiguous summary in the newer-releases test

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Updates/VersionCheckTests.cs:292-295`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Updates/VersionCheckTests.cs#L292-L295)

**Says:** `/// Every release between the running version and the latest is returned newest first, with`&lt;br>`/// dates, pre-release flags and install instructions stripped; older and equal ones are not.`

**Problem:** It reads as if dates and pre-release flags are stripped. They are kept and asserted (`PublishedAt`, `IsPrerelease`); only the install instructions are stripped.

**Proposed:** "...returned newest first, with its date and pre-release flag and its install instructions stripped; older and equal ones are not."

<a id="t1c-8"></a>

### T1c-8 · History wording · Low · Version check test comments tied to today's release list

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Updates/VersionCheckTests.cs:99-100`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Updates/VersionCheckTests.cs#L99-L100), 443-444

**Says:** `/// A development build newer than every public release (e.g. this branch's 3.0.0 against the`&lt;br>`/// real repo's current 2.9.x releases) reports ...`; `/// Running a pre-release after choosing Stable, the latest stable release - older, a 2.x one`&lt;br>`/// today - is offered to go back to`

**Problem:** Both describe the repository's release list before 3.0 ships ("this branch", "current 2.9.x", "today"). They become false the day 3.0.0 is published.

**Proposed:** line 99-100: "A build newer than every release on its channel (3.0.0 against 2.9.x releases) reports ...". Lines 443-444: "Running a pre-release after choosing Stable, the latest stable release - an older one - is offered to go back to, with its notes and installer."

## FeBuddy.UnitTests/Domain/Airways

<a id="t1c-19"></a>

### T1c-19 · Unused · Low · Duplicate airway-classification test

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Domain/Airways/AirwayClassifierTests.cs:46-54`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Domain/Airways/AirwayClassifierTests.cs#L46-L54) (duplicates 37-44)

**Problem:** It proves the same thing as `classification_uses_the_highest_segment_not_the_first_or_last` (`[Seg(5000), Seg(18000), Seg(9000)]` -> High). "Never split" can't fail anyway, because `Classify` returns one tuple.

**Proposed:** delete `an_airway_is_never_split_it_gets_exactly_one_classification`, or fold its "mixed altitudes, one class" wording into the other test's name.

**Future use:** n/a.

## FeBuddy.UnitTests/Domain/Arrivals

<a id="t1c-1"></a>

### T1c-1 · Wrong doc · Med · Arrival test name says the code id comes from the transition side; it comes from the procedure side

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Domain/Arrivals/ArrivalNamingTests.cs:16`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Domain/Arrivals/ArrivalNamingTests.cs#L16)

**Says:** `the_code_id_comes_from_the_transition_side_of_the_computer_code_with_the_amendment_digit_removed` with rows `("AALAN.BLAID2", "TWO", "BLAID", "BLAID")`, `("FIM.FERN7", ..., "FERN")`

**Problem:** A STAR code reads `TRANSITION.PROCEDURE`, and `ArrivalNaming.FromComputerCode` takes the part after the dot, `code[(dot + 1)..]` (FeBuddy.Core/Domain/Arrivals/ArrivalNaming.cs, `procedurePart`). `AALAN` is the transition and `BLAID` the procedure, so the name says the opposite of what the test asserts. The class summary on line 6 has it right.

**Proposed:** rename to `the_code_id_comes_from_the_procedure_side_of_the_computer_code_with_the_amendment_digit_removed`.

## FeBuddy.UnitTests/Domain/Crc

<a id="t1c-12"></a>

### T1c-12 · Stale doc · Low · CRC validator test comment names AirwaySettingsParser as what normalizes style case

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Domain/Crc/CrcPropertyValidatorTests.cs:57`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Domain/Crc/CrcPropertyValidatorTests.cs#L57)

**Says:** `[InlineData("SOLID", false)] // ValidateLine compares case-sensitively; AirwaySettingsParser is what normalizes case before validation`

**Problem:** Style case is normalized by `SettingsValueReader.NormalizeStyle`, called from `CrcDefaultsReader.ReadLine/ReadSymbol` (every service), `NavaidSettingsParser` and `EramCrcProperties`. AirwaySettingsParser doesn't do it itself.

**Proposed:** "// ValidateLine compares case-sensitively; CrcDefaultsReader normalizes the case (SettingsValueReader.NormalizeStyle) before validation"

## FeBuddy.UnitTests/Domain/Geo

<a id="t1c-14"></a>

### T1c-14 · Wrong doc · Low · Antimeridian test says the far side is dropped; it isn't

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Domain/Geo/AntimeridianSplitterTests.cs:78-85`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Domain/Geo/AntimeridianSplitterTests.cs#L78-L85)

**Says:** `public void a_crossing_whose_far_side_is_a_single_repeated_point_drops_that_side()` ... `Line((179.0, 10.0), (-179.0, 10.0), (-179.0, 10.0))`

**Problem:** Following AntimeridianSplitter.cs:43-90, the first segment crosses and emits `(179,10)-(180,10)`. The next fragment starts as `[(-180,10), (-179,10)]`, the repeated `(-179,10)` is skipped by `AppendIfDistinct`, and `(-180,10)-(-179,10)` is emitted. That gives two lines. The far side is kept (only the repeat is collapsed), and the test only asserts that no line is degenerate.

**Proposed:** rename to `a_crossing_followed_by_a_repeated_point_emits_no_degenerate_linestring`, and optionally `Assert.Equal(2, result.Count)`.

<a id="t1c-15"></a>

### T1c-15 · Wrong doc · Low · Two antimeridian cases never reach the split they claim to test

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Domain/Geo/AntimeridianSplitterTests.cs:34-50`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Domain/Geo/AntimeridianSplitterTests.cs#L34-L50), 52-58

**Says:** `[InlineData(180.0)]` ... `// The endpoint sits exactly on +/-180 - the crossing point the split computes is identical to it`; `repeated_consecutive_coordinates_do_not_create_a_zero_length_linestring() ... Line((10, 10), (10, 10), (11, 11))`

**Problem:** `(175,20)-(180,20.6075)` doesn't cross (|175-180| = 5 &lt;= 180, GeoMath.cs:126-127), so the +180 row returns the input unchanged and never computes a crossing. `(10,10),(10,10),(11,11)` doesn't cross either, so `Split` returns the input itself (AntimeridianSplitter.cs:83-86). `ls.Length > 0` is then true of the input, and repeat handling is never exercised. Both pass without testing anything they describe.

**Proposed:** drop the `[InlineData(180.0)]` row, or say in the comment that +180 is the no-crossing case. For the repeated-coordinates test, make the line cross, e.g. `Line((179, 10), (179, 10), (-179, 11))`, and assert every result `HasTwoDistinctCoordinates`.

<a id="t1c-13"></a>

### T1c-13 · Wrong doc · Low · GeoMath test summary says DMS coordinates; the test uses decimals

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Domain/Geo/GeoMathTests.cs:186-197`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Domain/Geo/GeoMathTests.cs#L186-L197)

**Says:** `/// Check to make sure that the distance between two DMS coordinates is correct when not rounding.` above `distance_not_rounded_between_two_decimal_coords_should_be_correct(double pointALat, ...)`

**Problem:** The test builds both points from decimal degrees (`new(pointALat, pointALon)`).

**Proposed:** "Check that the distance between two decimal coordinates is correct when not rounding." (Also remove the stray `<` in its `<param>` docs on lines 189-190; see [T1c-26](100_Bugs.md#t1c-26).)

<a id="t1c-25"></a>

### T1c-25 · Minor · Low · GeoMathTests: typos, empty and typed `<param>` docs, `; ;`, underscore-prefixed locals

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Domain/Geo/GeoMathTests.cs:14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Domain/Geo/GeoMathTests.cs#L14), 33-34, 80-81, 105, 129, 147-148, 156-157, 166, 168, 178-179, 189-191, 199-200, 208, 210, 212, 227-228, 236, 238, 249-250

**Says:** `/// <param name="lat">string: lattitude</param>`; `/// <param name="pointA">string: First point with Lattitude and Longitude seperated by a space.</param>`; `Location _pointA = new(pointA.Split(' ')[0], pointA.Split(' ')[1]); ;`

**Problem:** "lattitude"/"Lattitude" (15 lines), "seperated" (x2) and "coordintes" (208) are typos. Lines 33-34 have empty `<param>`s, and lines 189-190 end in a stray `<`. Every param doc starts with a type prefix ("string:", "double:", "bool:") that only repeats the signature. Lines 156-157 end in an empty statement (`; ;`), and locals `_pointA`/`_pointB` use the field prefix. The "Check to make sure that ..." summaries also differ from the rest of the suite's style.

**Proposed:** correct to "latitude"/"separated"/"coordinates", fill in or delete the empty `<param>`s, drop the "type:" prefixes and the stray `<`, remove the extra `;`, and rename the locals to `pointA`/`pointB` (in the DMS test, rename the string parameters to `pointAText`/`pointBText`).

<a id="t1c-18"></a>

### T1c-18 · Unused · Low · Unused `Factory` field in LineStringMergerTests

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Domain/Geo/LineStringMergerTests.cs:19`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Domain/Geo/LineStringMergerTests.cs#L19)

**Problem:** Never used. The file builds no geometry itself; `LineStringMerger` uses `Wgs84.Factory`.

**Proposed:** delete line 19 (and the blank line after it).

**Future use:** unlikely.

<a id="t1c-24"></a>

### T1c-24 · Minor · Low · `HandlerCoordinates` is a leftover name

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Domain/Geo/LineStringMergerTests.cs:21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Domain/Geo/LineStringMergerTests.cs#L21), 30, 124, 125, 145

**Says:** `private static readonly Dictionary<string, Coordinate> HandlerCoordinates = new()`

**Problem:** Nothing in the code is a "handler" (house rules ban `Handlers/`). These are just the merger's test points A-D.

**Proposed:** rename to `Points` (5 places).

## FeBuddy.UnitTests/Domain/Procedures

<a id="t1c-17"></a>

### T1c-17 · Wrong doc · Low · Class summaries that leave out what the class also tests

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Domain/Procedures/ProcedureNamingTests.cs:5-9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Domain/Procedures/ProcedureNamingTests.cs#L5-L9), 99-109; [`FeBuddy/FeBuddy.UnitTests/Application/Settings/SettingsValueReaderTests.cs:5-9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Settings/SettingsValueReaderTests.cs#L5-L9); [`FeBuddy/FeBuddy.UnitTests/Domain/Geo/GeoMathTests.cs:5-8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Domain/Geo/GeoMathTests.cs#L5-L8)

**Says:** `/// Covers <see cref="ProcedureNaming"/>: stripping the <c>, CONT.n</c> continuation marker ..., detecting a continuation page, and numbering a chart's pages.`; `/// Covers the SettingsValueReader paths ...: a present-but-out-of-range integer, malformed integer lists, positive decimals, and style normalization.`

**Problem:** ProcedureNamingTests also covers `RootName` (lines 64-97) and a different class, `ProcedureChartTypes.IsDepartureOrArrival` (lines 99-109). SettingsValueReaderTests also covers `DecimalInRange` (lines 53-82). GeoMathTests ("DMS and decimal validation and conversion, distance, bearing, and antimeridian handling") also covers `PointAtDistanceAndBearing` (lines 276-315).

**Proposed:** ProcedureNamingTests: append "the root name a departure or arrival keeps through its amendments, and which chart types are departures or arrivals (ProcedureChartTypes.IsDepartureOrArrival)", or move that last test to a ProcedureChartTypesTests.cs. SettingsValueReaderTests: "...positive decimals, decimals in a range, and style normalization." GeoMathTests: "...distance, bearing, the point at a distance and bearing, and antimeridian handling."

## FeBuddy.UnitTests/Domain/Procedures/ChartRecall

<a id="t1c-2"></a>

### T1c-2 · Wrong doc · Med · Chart Recall test says a procedure's code always uses the "right half" of the computer code

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Domain/Procedures/ChartRecall/ChartRecallCodesTests.cs:257-263`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Domain/Procedures/ChartRecall/ChartRecallCodesTests.cs#L257-L263)

**Says:** `a_procedure_with_a_computer_code_uses_the_right_half_less_its_version` with rows `("DP", "JALEX THREE (RNAV)", "JALEX3.JALEX", "JALEX")`, `("STR", "GRUUB ONE (RNAV)", "BRODE.GRUUB1", "GRUUB")`

**Problem:** `SidStarCodes.FromComputerCode` takes `halves[isStar ? 1 : 0]`, so a DP or ODP uses the first (left) half and only a STAR uses the second. The name is wrong for 4 of the 5 rows. The DP/ODP rows also can't tell the halves apart, because each one's procedure and transition share a name (`JALEX3.JALEX`, `BEARL1.BEARL`, `ABCDE10.ABCDE`, `DEVLN1.DEVLN`). A regression to "right half" would still pass this theory. Only `only_the_version_the_name_spells_is_dropped...` (`L711.LHS`) catches it.

**Proposed:** rename to `a_procedure_with_a_computer_code_uses_its_procedure_half_less_its_version` and add a DP row whose halves differ, e.g. `[InlineData("DP", "SALT LAKE FOUR", "SLC4.TCH", "SLC")]`.

## FeBuddy.UnitTests/Wpf/Controls

<a id="t3-12"></a>

### T3-12 · Wrong doc · Low · MapCanvasTests: the pixel scale in `Looking`'s doc is off by about 4x

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/Controls/MapCanvasTests.cs:352-355`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/Controls/MapCanvasTests.cs#L352-L355)

**Says:** `/// A sized map centred on 40N 99.5W at zoom 10 - about 2.8 pixels to 0.001 degrees of longitude -`

**Problem:** Zoom 10 is 256 x 2^10 = 262,144 px per 360 degrees, so 0.001 degrees of longitude is about 0.73 px (the test at :218 itself says 0.1 degrees is "about 73 pixels").

**Proposed:** "... at zoom 10 - about 73 pixels to 0.1 degrees of longitude - ..."

<a id="t3-16"></a>

### T3-16 · Wrong doc · Low · Test class summaries that leave out much of what the class tests

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/Controls/MapCanvasTests.cs:13-18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/Controls/MapCanvasTests.cs#L13-L18); [`FeBuddy.UnitTests/Wpf/ViewModels/AirwaysViewModelTests.cs:14-19`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/AirwaysViewModelTests.cs#L14-L19); [`FeBuddy.UnitTests/Wpf/ViewModels/SystemHealthTests.cs:11-15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/SystemHealthTests.cs#L11-L15)

**Says:** MapCanvasTests: `/// ... and letting go of a map whose layer list lives on. Each test runs on a WPF thread of its own`; AirwaysViewModelTests: `/// Covers the High and Low Airway Classification card of <see cref="AirwaysViewModel"/>: ...`

**Problem:** MapCanvasTests' summary omits the whole "Ctrl + click" half (hit testing, Inspect, highlight, DistanceToRun - 11 tests), and `the_distance_to_a_ring_includes_its_closing_edge` does not use StaThread. AirwaysViewModelTests also covers the outputs-on tags, the Area card's tags, the File Layout radios and the preview row. SystemHealthTests also covers the GitHub token row, the dev launch's unloaded cycles and `ChecksGitHubTokensAtLaunch`.

**Proposed:** MapCanvasTests: append "; and Ctrl + click: what is hit, in what order, Inspect and the highlight. Each test that builds a map runs on a WPF thread of its own". AirwaysViewModelTests: "Covers &lt;see cref="AirwaysViewModel"/>: the High and Low Airway Classification card (...), the File Layout radios, the output tags of its cards, and the preview row ...". SystemHealthTests: add "the GitHub token row, a dev launch's unloaded cycles, and whether the launch checks GitHub tokens".

<a id="t3-24"></a>

### T3-24 · History wording · Low · Issue and feature numbers in test docs

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`Wpf/Controls/OutputSummaryTests.cs:7`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/Controls/OutputSummaryTests.cs#L7); [`Wpf/Shell/WindowPlacementRulesTests.cs:8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/Shell/WindowPlacementRulesTests.cs#L8); [`Wpf/ViewModels/AreaTests.cs:16`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/AreaTests.cs#L16); [`ConcatenateAliasesViewModelTests.cs:230`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/ConcatenateAliasesViewModelTests.cs#L230); [`DuplicateAliasReviewTests.cs:11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/DuplicateAliasReviewTests.cs#L11); [`MapFileItemTests.cs:8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/MapFileItemTests.cs#L8); [`NavRailTests.cs:6`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/NavRailTests.cs#L6); [`ProcedureFixInFacilityCardTests.cs:13`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/ProcedureFixInFacilityCardTests.cs#L13); [`ProceduresViewModelTests.cs:12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/ProceduresViewModelTests.cs#L12); [`SystemHealthTests.cs:12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/SystemHealthTests.cs#L12); [`TelephonyViewModelTests.cs:995`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/TelephonyViewModelTests.cs#L995); [`WhatYoullGetTests.cs:24`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/WhatYoullGetTests.cs#L24), 100, 210, 212, 304, 718 (all under FeBuddy.UnitTests/)

**Says:** `(issues #314, #333)`, `(Feature #364)`, `(issue #335, ...`, `// ---- the one-line rows (issue #326) ----`, `Issue #335: only the area picked narrows ...`, `Issues #333 and #334: ...`

**Problem:** They point at the issues the work came from (house rule: no task numbers). None explains a bug the test guards against; [#337](https://github.com/Nikolai558/FE-BUDDY/issues/337) (TelephonyViewModelTests:995) is the only one that might - keep it only if [#337](https://github.com/Nikolai558/FE-BUDDY/issues/337) was a bug report.

**Proposed:** Delete each parenthetical; for the "Issue #NNN:" openers in WhatYoullGetTests keep the sentence after the colon (e.g. :100 "Only the area picked narrows - the ARTCCs, or the ROI - and the card is told of each change.").

## FeBuddy.UnitTests/Wpf/Shell

<a id="n1-41"></a>

### N1-41 · Minor · Low · DeferredSave's timer, close and retry have no test; a save-failure test fails only at its first write

**Status:** Open; found in the review of the S2 to S5 code at `4893e32` · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Wpf/Shell/DeferredSaveTests.cs:45-92`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Wpf/Shell/DeferredSaveTests.cs#L45-L92); [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigFileTests.cs:328-344`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigFileTests.cs#L328-L344)

**Problem:** DeferredSaveTests call `SaveNowAsync` directly, so nothing covers the timer, the save at close or a retry. `save_values_that_cannot_be_written…` fails at the snapshot write, before the profile write, so the rollback after a failed profile write is untested.

**Proposed:** Add: `Set`, then wait longer than `Delay`, and the file has the key; `Set`, then shut the dispatcher down, and the file has the key; the retry test from [N1-15](100_Bugs.md#n1-15). In UserConfigFileTests, a case with a read-only profile file: memory is rolled back and `CanUndo` stays false.

## FeBuddy.UnitTests/Wpf/ViewModels

<a id="t3-9"></a>

### T3-9 · Wrong doc · Med · AiracGeneralTabViewModelTests summary says the last output is "kept on"

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/ViewModels/AiracGeneralTabViewModelTests.cs:17-21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/AiracGeneralTabViewModelTests.cs#L17-L21)

**Says:** `/// everything included and on to start, the outputs a sub-service doesn't offer always off, the / last output kept on, and where the choices are saved`

**Problem:** The opposite is tested and implemented: unticking the last output leaves the sub-service out (test `unticking_the_last_output_leaves_the_sub_service_out`, :93-112; SubServiceRow.SetOutput, SubServiceRow.cs:156-172).

**Proposed:** "... the outputs a sub-service doesn't offer always off, unticking the last output leaving the sub-service out, and where the choices are saved ..."

<a id="t3-19"></a>

### T3-19 · Unused · Low · A test that cannot fail: the alias guide's export commands "can be executed"

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/ViewModels/AliasGuideViewModelTests.cs:97-105`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/AliasGuideViewModelTests.cs#L97-L105) (and the summary's "and both exports are there", :12-13)

**Problem:** Both are `new RelayCommand(action)` with no predicate, and `RelayCommand.CanExecute` returns `canExecute?.Invoke() ?? true` (Mvvm/RelayCommand.cs:26) - always true. The test checks nothing about the page.

**Proposed:** Delete `the_guide_and_the_practice_page_can_both_be_exported` and drop "; and both exports are there" from the class summary.

<a id="t3-25"></a>

### T3-25 · History wording · Low · "under its first name ... an earlier off carries over"

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/ViewModels/ConcatenateAliasesViewModelTests.cs:115`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/ConcatenateAliasesViewModelTests.cs#L115)

**Says:** `/// <summary>The choice is saved under its first name, CombineAliasFiles, so an earlier "off" carries over.</summary>`

**Problem:** Narrates the key's history; the test only checks the key the choice is saved under. (Production ConcatenateAliasesViewModel.cs:65-69 has the same history; see [T3-31](130_Old-Version-Code.md#t3-31).)

**Proposed:** "The choice is saved under CombineAliasFiles and read back."

<a id="t3-34"></a>

### T3-34 · Minor · Low · DevLaunchCardViewModelTests' constructor and Dispose have no docs and Dispose swallows every exception

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/ViewModels/DevLaunchCardViewModelTests.cs:20-39`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/DevLaunchCardViewModelTests.cs#L20-L39)

**Says:** `public DevLaunchCardViewModelTests()` / `catch { // Best-effort. }`

**Problem:** Every other test class here documents both ("Points the config and the log at a throwaway folder." / "Restores ...") and catches only IOException.

**Proposed:** Add `/// <summary>Points the dev launch settings and the log at a throwaway folder.</summary>` and `/// <summary>Restores the real dev launch settings and log, and deletes the folder.</summary>`; catch `IOException` with `// Best-effort cleanup.`

<a id="t3-20"></a>

### T3-20 · Unused · Low · EramToGeojsonViewModelTests sets choices it never checks

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/ViewModels/EramToGeojsonViewModelTests.cs:160-179`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/EramToGeojsonViewModelTests.cs#L160-L179)

**Problem:** `saved_choices_come_back` sets the line-defaults row and the two CRC-defaults switches, then asserts only layout, DefaultsFromCard, IncludeFebCustomProperties and the feb property.

**Proposed:** Assert them on `reloaded` (`LineDefaults[0]` values, `IncludeCrcSymbolDefaults`/`IncludeCrcTextDefaults` false), or delete those six lines.

<a id="t3-14"></a>

### T3-14 · Wrong doc · Low · FileConversionsViewModelTests: Source does not pick its top choice

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/ViewModels/FileConversionsViewModelTests.cs:10-15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/FileConversionsViewModelTests.cs#L10-L15)

**Says:** `/// Covers the File Conversions screen: its picker (Source, File, Output, each picking its top / choice until the user picks another)`

**Problem:** The first test asserts `screen.SelectedSource` is null to start; only File and Output pick their top choice once a source is picked.

**Proposed:** "its picker (Source, then File and Output, each of those picking its top choice until the user picks another)".

<a id="t3-32"></a>

### T3-32 · Minor · Low · `nothing_is_refused` asserts that null is refused

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/ViewModels/ImportSettingsViewModelTests.cs:171-180`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/ImportSettingsViewModelTests.cs#L171-L180)

**Says:** `public void nothing_is_refused()` ... `Assert.Throws<ArgumentNullException>(...)` x4

**Problem:** The name reads as "no argument is refused", the opposite of what it checks.

**Proposed:** Rename to `null_arguments_are_refused`.

<a id="t3-11"></a>

### T3-11 · Wrong doc · Low · InfoViewModelTests says the guide reads the facility

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/ViewModels/InfoViewModelTests.cs:10-15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/InfoViewModelTests.cs#L10-L15)

**Says:** `/// browser, so they are not opened here. The guide reads the facility from a throwaway config.`

**Problem:** The guide reads nothing about the facility: `AliasGuideViewModel.Document` is `AliasGuideContent.Build(ProceduresViewModel.SavedHelpCommand())`, which reads Procedure Charts' help-command settings and the General tab's Procedures alias output (ProceduresViewModel.cs:474-488).

**Proposed:** "The guide reads the chart recall help command's saved settings from a throwaway config."

<a id="t3-27"></a>

### T3-27 · History wording · Low · "as before"

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/ViewModels/MapViewModelTests.cs:267`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/MapViewModelTests.cs#L267)

**Says:** `/// <summary>Esc closes the panel first; with it closed, Esc cancels an ROI edit as before.</summary>`

**Problem:** "as before" reads as "as it did before the panel existed".

**Proposed:** "Esc closes the panel first; with it closed, Esc cancels an ROI edit."

<a id="t3-21"></a>

### T3-21 · Unused · Low · Unneeded setup in a help-command test

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/ViewModels/ProcedureHelpCommandCardTests.cs:165-173`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/ProcedureHelpCommandCardTests.cs#L165-L173)

**Problem:** The test only asserts `FieldErrors["HelpCommandName"]`; the saved airport only affects the Area card's validation, which the test does not look at.

**Proposed:** Delete the line (or say in a comment why the tab needs an airport).

<a id="t3-26"></a>

### T3-26 · History wording · Low · "names an earlier FE-Buddy saved in full"

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/ViewModels/ProcedureNameCardsTests.cs:15-16`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/ProcedureNameCardsTests.cs#L15-L16), 132; [`FeBuddy.UnitTests/Wpf/ViewModels/WhatYoullGetTests.cs:773`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/WhatYoullGetTests.cs#L773)

**Says:** `/// ... read names an earlier / FE-Buddy saved in full as their roots`, `public void full_names_an_earlier_fe_buddy_saved_read_as_their_roots_without_unsaved_changes()`, `// Saved by an earlier FE-Buddy as full names: they read as their root names.`

**Problem:** History wording for COMPAT code ([T3-28](130_Old-Version-Code.md#t3-28)). If that code stays, describe the behaviour, not who saved the names.

**Proposed:** Summary: "read a saved full name (BRWNZ FOUR (RNAV)) as its root"; test: `a_saved_full_name_reads_as_its_root_without_unsaved_changes`; WhatYoullGetTests:773: `// Full names read as their root names.` If [T3-28](130_Old-Version-Code.md#t3-28)'s code is removed, delete the test and the comment instead.

<a id="t3-10"></a>

### T3-10 · Wrong doc · Low · ProceduresViewModelTests: a test's summary names the ROI, the test uses "None"

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/ViewModels/ProceduresViewModelTests.cs:94-99`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/ProceduresViewModelTests.cs#L94-L99)

**Says:** `/// <summary>Only the area chosen counts: a facility ticked while the area is the ROI includes nothing.</summary>` ... `procedures.Area = SubServiceArea.None;`

**Problem:** The test sets the area to None, not the ROI.

**Proposed:** "... a facility ticked while the area is None includes nothing." (or set `SubServiceArea.Roi` to match the doc).

<a id="t3-33"></a>

### T3-33 · Minor · Low · ProceduresViewModelTests calls the tab "the Procedures tab"

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/ViewModels/ProceduresViewModelTests.cs:12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/ProceduresViewModelTests.cs#L12)

**Says:** `/// Covers the Procedures tab's Airports card taking a list (issue #309): ...`

**Problem:** The tab is shown as "Procedure Charts" (AiracSubServices.cs:127); the other Procedure tests say "Procedure Charts tab".

**Proposed:** "Covers the Procedure Charts tab's Airports card taking a list: ..."

<a id="t3-13"></a>

### T3-13 · Wrong doc · Low · WhatYoullGetTests summary gives the wrong chip for a narrowing

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/ViewModels/WhatYoullGetTests.cs:23-28`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/WhatYoullGetTests.cs#L23-L28)

**Says:** `/// per filter that is set, each after the first narrowing ("with only") or adding ("along with"),`

**Problem:** A narrowing line reads "but only"/"and only" (SummaryLines.Add, SummaryBlock.cs:164-171, and every assertion in the file); "with only" is only the "Outputs include" chip. "for"/"in" are not mentioned.

**Proposed:** "... each after the first starting with a chip - "for" or "in" a place, "but only"/"and only" a narrowing, "along with" an addition - with "with only" under "Outputs include", ..."

<a id="t3-17"></a>

### T3-17 · Wrong doc · Low · The STAR/STR test claims more than it asserts

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/ViewModels/WhatYoullGetTests.cs:874-881`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/WhatYoullGetTests.cs#L874-L881)

**Says:** `/// <summary>The d-TPP calls a STAR <c>STR</c>: the checkbox and the summary say STAR, and STR is still what is sent.</summary>` ... `Assert.Equal("STARs (STAR)", star.Label);`

**Problem:** Only the checkbox label is asserted; nothing checks the summary or what is sent (the Token is only used to find the toggle).

**Proposed:** Add `Assert.Contains("STR", tab.BuildSettingsBlock()["ChartTypes"]...)` (whatever key carries the types) and a summary check, or trim the doc and name to "the_star_chart_type_is_labelled_star".

<a id="t3-18"></a>

### T3-18 · Stale doc · Low · WhatsNewViewModelTests section header names an Export that doesn't exist

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** [`FeBuddy.UnitTests/Wpf/ViewModels/WhatsNewViewModelTests.cs:136`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/WhatsNewViewModelTests.cs#L136)

**Says:** `// ---- Back, View the guide and Export ----`

**Problem:** WhatsNewViewModel has only BackCommand and OpenGuideCommand (WhatsNewViewModel.cs:55-58); the export lives on the Alias Command Guide page. No Export test is in the section.

**Proposed:** `// ---- Back and View the guide ----`

<a id="n1-39"></a>

### N1-39 · Wrong doc · Low · AiracGeneralTabViewModelTests: the "older build" case uses a key no release saved, and the summaries say Current or Next only

**Status:** Fixed (not yet merged). The theory takes full keys and uses the real old key, `Services.AiracService.AiracCycleId`; both summaries are reworded as proposed · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/AiracGeneralTabViewModelTests.cs:279-281`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/AiracGeneralTabViewModelTests.cs#L279-L281), [`FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/AiracGeneralTabViewModelTests.cs:294-303`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/AiracGeneralTabViewModelTests.cs#L294-L303)

**Says:** "a cycle ID an older build saved"

**Problem:** The `("AiracCycleId", "2609")` row writes `Services.AiracService.General.AiracCycleId`, which only untagged commits ever saved; releases saved `Services.AiracService.AiracCycleId`. Only `CyclePosition` is read, so the row tests the same as "nothing saved". The summary at 294 explains the case by history ([H3](010_Decisions.md#h3)), and the one at 280 says only Current or Next are saved, though Previous is too.

**Proposed:** Drop the row, or have the theory take full keys and use `Services.AiracService.AiracCycleId`, with the summary "Only `CyclePosition` is read; any other value or key opens on Current." Summary at 280: "`Previous`, `Current` or `Next`".

<a id="n1-40"></a>

### N1-40 · Minor · Low · New tests: summaries on the wrong helper, wall-clock limits that can fail on a busy runner, and an untested Telephony guard

**Status:** Fixed (not yet merged). The two summaries are moved. The timing asserts are work counts: headings worked out (at most 2 × 50,000), each command's choices read (at most twice) and a counting `taken` set; VATSIM-Radar compares 3,000 and 30,000 rows, timed in turns, fastest of five each. Telephony's read is `LoadVatsimRadarCopyAsync(read)`. Test: `TelephonyViewModelTests.a_download_finishing_during_the_kept_copy_read_keeps_its_copy` (fails with the guard taken out) · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/TelephonyViewModelTests.cs:794-806`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/TelephonyViewModelTests.cs#L794-L806); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Telephony/VatsimRadarVirtualAirlinesTests.cs:115-118`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Application/Airac/Telephony/VatsimRadarVirtualAirlinesTests.cs#L115-L118), 137-149; [`FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/ServiceRunReviewTabViewModelTests.cs:154-165`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/ServiceRunReviewTabViewModelTests.cs#L154-L165); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/DuplicateAliasChoicesTests.cs:145-163`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Application/Airac/DuplicateAliasChoicesTests.cs#L145-L163); [`FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs:101-105`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs#L101-L105), 113-123

**Problem:** `DownloadFinished` has two summaries, one of them `TabUsing`'s; a `Merge` summary in VatsimRadarVirtualAirlinesTests belongs on the test at 152. Three tests assert wall-clock limits (under 1 or 2 seconds) that include JIT and GC; CI runs test classes in parallel under coverage instrumentation, so they can fail for nothing. Telephony's "unless a download got there first" guard has no test and can't have one: `LoadVatsimRadarCopy` returns when `Application.Current` is null.

**Proposed:** Move the summaries. Replace the timing asserts with work counts (count the group-key calls, ≤ 2 × 50,000; a counting `taken` set); for VatsimRadar, which has no seam, compare n and 10n with a loose ratio. Give the guard a seam (an internal `LoadVatsimRadarCopyAsync(Func<VatsimRadarCopy?> read)` without the `Application.Current` check) and test that a download finishing during the read keeps its copy.

<a id="n1-42"></a>

### N1-42 · Minor · Low · New tests that can't catch what they guard: the update watcher's retry and loaded copy, FieldState, Telephony's file name

**Status:** Open; found in the review of the S2 to S5 code at `4893e32` · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Platform/InstalledVersionWatcherTests.cs:121-145`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Infrastructure/Platform/InstalledVersionWatcherTests.cs#L121-L145), [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Platform/InstalledVersionWatcherTests.cs:51-68`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Infrastructure/Platform/InstalledVersionWatcherTests.cs#L51-L68); [`FeBuddy/FeBuddy.UnitTests/Wpf/Behaviors/FieldStateTests.cs:30-53`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Wpf/Behaviors/FieldStateTests.cs#L30-L53); [`FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/RunResultSummaryTests.cs:80-139`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/RunResultSummaryTests.cs#L80-L139)

**Problem:** The watcher's retry test never deletes the folder, so the copy's own Created event triggers the check: it passes with the retry loop deleted. The loaded-copy test has no control assertion that `FileVersionInfo` really answers with the old version in its setup. FieldStateTests builds its own Style, so it proves WPF's precedence rule, not the app: it can't catch a local `ToolTip` on a box or the theme's setter going missing. RunResultSummaryTests covers renamed files for five sub-services but not Telephony.

**Proposed:** Retry test: move the dll out, delete the folder, recreate it, let its check run and find nothing, then copy. Loaded-copy test: assert the control, `Unload` in a `finally`. FieldState: load `/FE-BUDDY;component/Theme/Controls.Inputs.xaml` with `Application.LoadComponent` and use its styles; add a scan that no XAML element sets both `bhv:FieldState.Error` and a local `ToolTip`. RunResultSummary: a Telephony result with `ZOB_Telephony.txt`.

## see

<a id="t3-35"></a>

### T3-35 · Minor · Low · Tests that pin GUI wording word for word (list below)

**Status:** Open · **Session:** [S9](020_Work-Plan.md#s9)

**Where:** see "GUI-text pins" below (about 260 assertions in 37 files)

**Says:** e.g. `Assert.Equal("Choose High, Low, or Both for Y, ZK on the High and Low Airway Classification card, or untick them under Airway Types to Include.", tab.ValidationError);`

**Problem:** Any wording change (the owner plans to move user-manual explanations into the GUI) breaks these tests although behaviour is unchanged. InfoViewModelTests also pins the version-bound title "What's New in v3.0?", which breaks at 3.1.

**Proposed:** Where the behaviour is the point, assert the parts that carry it (the IDs, counts, cycle numbers, file names, `Contains("ZOB")`, non-empty, which error key is set) instead of whole sentences; where the exact text matters (labels, tooltips), make the string a `const`/`static` on the view-model and assert against that once. Pure formats (coordinates, file names, command syntax, counts) are not listed.

**Note:** The full list of pinned strings is at the end of the On-screen text comment.

## Reported more than once

Each review slice was read by its own reviewer, so some findings were reported twice. These IDs point at the finding that is kept.

- <a id="t1c-6"></a>**T1c-6** · Location's DMS side is used only by tests (and GeoMath's DMS parse and validate with it) · same as [C5-2](140_Unused-Code.md#c5-2).
- <a id="t1c-7"></a>**T1c-7** · `ReadArea`'s "no Area key" inference is reached only by tests, and its doc's "the harness" is wrong · same as [T1b-21](140_Unused-Code.md#t1b-21).
- <a id="t3-15"></a>**T3-15** · FileNamesAsCodeConverter is not only for the Review tab · same as [W6-9](220_Comments-GUI.md#w6-9).
