# 200 · Code comments and XML docs: Core

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

Comments and XML docs in FeBuddy.Core that are wrong, stale, narrate history, or are minor slips. Grouped by folder. Session [S7](020_Work-Plan.md#s7).

**142 findings** (8 Med, 134 Low). Quote the ID when you comment on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374).

## Summary

| ID | Kind | Sev | Finding | Status | Session |
|---|---|---|---|---|---|
| [C7-2](#c7-2) | History wording | Low | csproj News comment: "keeps the resource name" describes the move, not today's lookup | Open | S7 |
| [C7-3](#c7-3) | Wrong doc | Low | csproj comment says unused usings "are reported", but nothing in the build reports them | Open | S7 |
| [C1-18](#c1-18) | Wrong doc | Low | "Never blocking readiness": the d-TPP Metafile download sits in front of each cycle's parse | Open | S7 |
| [C1-20](#c1-20) | Wrong doc | Low | Shared-data refresh rules overstated in the cache remarks and the load result doc | Open | S7 |
| [C1-34](#c1-34) | Minor | Low | AiracCycleDataCacheEntry: public ParseTask nobody outside uses, and a public data class outside Models/ | Fixed | S6 |
| [T1b-11](#t1b-11) | Wrong doc | Low | PrepareCyclesAsync "Never throws" - the tests show it throws on cancellation | Open | S7 |
| [C1-25](#c1-25) | History wording | Low | Issue numbers that only point at the feature request | Open | S7 |
| [C1-36](#c1-36) | Minor | Low | DuplicateAliasReport.CommandOf: "up to its first space" (tab too) | Open | S7 |
| [C2-19](#c2-19) | Wrong doc | Low | Airports alias: "an .ECHO command per airport"; "an airport with an ICAO ID contributes two" | Open | S7 |
| [C2-17](#c2-17) | Wrong doc | Low | BuildFrequencyLookup names frequency types the sub-service does not use | Open | S7 |
| [C2-18](#c2-18) | Wrong doc | Low | Exception docs name only the main NASR group | Open | S7 |
| [C2-5](#c2-5) | Wrong doc | Low | AirportService remark: "every other type in this folder is a step of this pipeline" | Open | S7 |
| [C2-23](#c2-23) | Minor | Low | AirportFebProperty: remark paragraph outside &lt;para>, and gaps in the enum values | Open | S7 |
| [C2-20](#c2-20) | Minor | Low | "service" used for non-entry-point types, against the naming rule | Fixed | S6 |
| [C2-2](#c2-2) | Wrong doc | Med | AirwayBuilder.BuildAll says airways outside the ROI are excluded | Open | S7 |
| [C2-4](#c2-4) | Stale doc | Low | AirwayAliasWriter says Combined_Alias.txt needs "Concatenate Aliases" | Open | S7 |
| [C2-11](#c2-11) | Wrong doc | Low | "The final point of an airway never appears as a FromPoint" (likely untrue for NASR) | Agreed | S7 |
| [C2-12](#c2-12) | Minor | Low | ResolveOrderedPoints sends border markers to the waypoint lookup | Open | S7 |
| [C2-8](#c2-8) | Wrong doc | Low | AirwayGeometryBuilder does not collect "every unresolved ID" | Open | S7 |
| [C2-10](#c2-10) | Wrong doc | Low | NeedsCrcDefaults remark: designation files' "rest are written as per-feature overrides" | Open | S7 |
| [C2-22](#c2-22) | Minor | Low | AirwayWaypointBuffer.Buffer param doc: misplaced "+/-180" clause | Open | S7 |
| [T1a-35](#t1a-35) | Wrong doc | Low | AirwayWaypointBuffer.Buffer docs contradict its code and its tests | Open | S7 |
| [C2-1](#c2-1) | Wrong doc | Med | AirwayGeojsonOutputBy.Designation says files group by AWY_DESIGNATION | Open | S7 |
| [C2-9](#c2-9) | Wrong doc | Low | AirwayBufferResult says buffered legs are "always disjoint" | Open | S7 |
| [C2-13](#c2-13) | History wording | Low | AliasRoiScope.All "(the historical behaviour)" | Open | S7 |
| [C2-16](#c2-16) | Wrong doc | Low | RoiAirways scope is not "exactly the airways the GeoJSON draws" | Open | S7 |
| [C2-14](#c2-14) | History wording | Low | ArrivalAliasWriter.CommandName "as in FE-Buddy 2.x" | Open | S7 |
| [C2-21](#c2-21) | Minor | Low | Arrivals file-key summaries omit `_STAR_` | Open | S7 |
| [C2-29](#c2-29) | Minor | Low | Arrivals is a copy of Departures - duplicated logic that could be shared | Deferred | After 3.0 |
| [C2-30](#c2-30) | Minor | Low | ArrivalSettings.ArtccFilter is per airport, not per procedure | Open | S7 |
| [C3-9](#c3-9) | Wrong doc | Low | `ArtccBoundaryFilter` / `NavaidFilter` say they match ignoring case; they use the passed collection's own comparison | Open | S7 |
| [C3-22](#c3-22) | Minor | Low | "every other ... service consumes this typed object" - they are types, not services | Fixed | S6 |
| [C3-14](#c3-14) | History wording | Low | "A file from before the end line existed" in CombinedAliasFileWriter | Open | S7 |
| [C3-23](#c3-23) | Minor | Low | `CombinedAliasFileWriter` docs: layout sample and exceptions | Open | S7 |
| [C3-13](#c3-13) | History wording | Low | "as the code still calls it" in ConcatenateAliasesSettingsParser | Open | S7 |
| [C3-25](#c3-25) | Minor | Low | Public members with no caller outside their file | Open | S7 |
| [C3-4](#c3-4) | Wrong doc | Low | `ConcatenateAliasesSettingsParser.IgnoredKeys` says the AIRAC Service sets `OutputDirectory` on this block; it doesn't | Open | S7 |
| [C2-15](#c2-15) | Stale doc | Low | "Efficient Linestring Handling" is a term from the old Developer_Notes planning doc | Open | S7 |
| [C2-7](#c2-7) | Wrong doc | Low | Departures/Arrivals feb.* "in the order below" is not enforced | Open | S7 |
| [C3-11](#c3-11) | Wrong doc | Low | `FixService` comment lists only some reasons a run writes nothing | Open | S7 |
| [C1-22](#c1-22) | Wrong doc | Low | AiracServiceProgress.PercentComplete isn't a percentage; it is only ever 100 for "this step finished" | Open | S7 |
| [C1-29](#c1-29) | Wrong doc | Low | AiracServiceResult.OutputDirectory "exists only if the run wrote something" | Open | S7 |
| [C1-28](#c1-28) | Wrong doc | Low | AiracServiceSettings: the blocks are built from each tab's view-model, not from the saved config | Open | S7 |
| [C1-19](#c1-19) | Wrong doc | Low | CycleDataState.Failed says parses are retried; they aren't | Open | S7 |
| [C3-10](#c3-10) | Wrong doc | Low | `NavaidGeojsonWriter` remarks: the alias file covers every NAVAID, not "every included" one | Open | S7 |
| [C4-1](#c4-1) | Stale doc | Med | ProceduresJsonWriter says the Markdown document de-duplicates shared STARs; it doesn't | Open | S7 |
| [C4-22](#c4-22) | Minor | Low | The help command's note names the guide "the FE-Buddy Command Guide" | Open | S7 |
| [C4-2](#c4-2) | Wrong doc | Low | The Markdown writer's remarks still say every chart brought in by a route fix is labelled (#371) | Open | S7 |
| [C4-23](#c4-23) | Minor | Low | The re-added "Info" line in Procedure_Changes.md: casing and a missing full stop | Open | S7 |
| [C4-24](#c4-24) | Minor | Low | Both document writers' `settings` param docs list too few of the settings they read | Open | S7 |
| [C4-10](#c4-10) | Wrong doc | Low | The AirportProcedures error message gives an approach as its example; the setting takes departures and arrivals only | Open | S7 |
| [C4-13](#c4-13) | History wording | Low | "Feature #367 / #371" task numbers in 12 comments | Open | S7 |
| [C4-16](#c4-16) | Wrong doc | Low | ProcedureSettings.PrimaryFacility describes only the section order | Open | S7 |
| [C4-17](#c4-17) | Wrong doc | Low | ProcedureSettings.JsonFields' list of fields "always written" is off | Open | S7 |
| [C3-12](#c3-12) | History wording | Low | Issue numbers that only narrate history in TelephonyBuilder | Open | S7 |
| [C3-6](#c3-6) | Wrong doc | Low | "The only Telephony type AiracService and FeBuddy.Harness call directly" is not true | Open | S7 |
| [C3-7](#c3-7) | Wrong doc | Low | Telephony and Wx Stations say "every AIRAC Service run" downloads their data | Open | S7 |
| [C3-5](#c3-5) | Wrong doc | Low | `TelephonySettingsParser` remarks: the Telephony tab sends no ROI keys, and `IncludeFebCustomProperties = Y` warns rather than being ignored | Open | S7 |
| [C3-26](#c3-26) | Minor | Low | "VatsimRadar" identifiers name the combined GNG + VATSIM-Radar list | Open | S7 |
| [C3-8](#c3-8) | Wrong doc | Low | Telephony summaries miss the first-operator rule and the list | Open | S7 |
| [C3-24](#c3-24) | Minor | Low | `WxStationSettingsParser.Parse` exception doc misses the both-off throw | Open | S7 |
| [C4-8](#c4-8) | Wrong doc | Med | Alias Command Guide: "Charts with no command" leaves out LOC/NDB parts | Open | S7 |
| [C4-9](#c4-9) | Wrong doc | Low | The guide's shared "Procedure Names" text describes Chart Recall's naming rule, but Data Display uses a different one | Open | S7 |
| [C4-25](#c4-25) | Minor | Low | AliasPractice.js: diagnose's doc calls `answer` a command; it is an index | Open | S7 |
| [C4-3](#c4-3) | Stale doc | Low | PracticeSubject.Help says the help question uses the default name (#370) | Open | S7 |
| [C5-9](#c5-9) | Wrong doc | Low | ReadCrcInclude remark claims callers AND it with "is that file being produced" | Open | S7 |
| [C5-17](#c5-17) | Wrong doc | Low | DAT model and writer docs miss cases | Open | S7 |
| [C5-10](#c5-10) | Wrong doc | Low | "The keys only this conversion reads, on top of … ConversionKeys" includes ConversionKeys | Open | S7 |
| [C5-24](#c5-24) | Minor | Low | DAT's labelSource text can never be shown | Open | S7 |
| [C5-19](#c5-19) | Wrong doc | Low | Rundown "Lines end in CRLF" holds only where Environment.NewLine is CRLF | Open | S7 |
| [C5-20](#c5-20) | History wording | Low | ERAM comments narrate the old ERAM_2_GEOJSON tool's behaviour where it explains nothing current | Open | S7 |
| [C5-28](#c5-28) | Minor | Low | Stray blank lines in EramToGeojsonSettingsParser | Open | S7 |
| [C5-7](#c5-7) | Wrong doc | Low | EramDefaultsSource docs don't match what XmlThenCard and Card do | Open | S7 |
| [C1-21](#c1-21) | Wrong doc | Low | LaunchSequence says all three network steps use the internet flag; the AIRAC pipeline doesn't | Open | S7 |
| [C1-24](#c1-24) | Wrong doc | Low | NewsService.Parse: PostId isn't optional, and posts aren't returned in document order | Open | S7 |
| [C1-30](#c1-30) | Wrong doc | Low | ReadArea says the Harness sends blocks without Area, and keeps a "before there was a choice" note | Open | S7 |
| [C1-31](#c1-31) | Wrong doc | Low | SubServiceSettingsReader is shared with the file conversions too | Open | S7 |
| [C1-37](#c1-37) | Minor | Low | Stray blank line in ReadFebProperties | Open | S7 |
| [C1-23](#c1-23) | Wrong doc | Low | UpdateChannelSetting says the channel is saved in UserConfig.json; it is in Shared.json | Open | S7 |
| [C1-26](#c1-26) | History wording | Low | UpdateInstaller remarks narrate 2.x's updater | Open | S7 |
| [C1-27](#c1-27) | Wrong doc | Low | ReleaseSummary.IsPrerelease says "alpha or beta"; it is any pre-release, rc included | Open | S7 |
| [C1-35](#c1-35) | Minor | Low | DownloadProgress lives in VersionCheckResult.cs | Fixed | S6 |
| [C5-3](#c5-3) | Wrong doc | Med | AiracCycleResolver: "about one year in nine has 14" cycles is wrong | Open | S7 |
| [C5-25](#c5-25) | Minor | Low | Copied code the house rules say to share | Deferred | After 3.0 |
| [C5-12](#c5-12) | Wrong doc | Low | xOffset/yOffset "any integer (CRC spec)" contradicts the linked spec | Agreed | S7 |
| [C5-14](#c5-14) | Wrong doc | Low | CrcSymbolDefaults says "Every one is required" but Style is nullable | Open | S7 |
| [C5-13](#c5-13) | Wrong doc | Low | Crc*Properties: "no filters assigned (via this or a per-feature override)": these records are the per-feature override | Open | S7 |
| [C5-27](#c5-27) | Minor | Low | Time-relative data claims that will go stale | Open | S7 |
| [C5-16](#c5-16) | Wrong doc | Low | LineStringMerger "longest first" means "most points", not distance | Open | S7 |
| [C5-15](#c5-15) | Wrong doc | Low | RadiusFilter inline comment states the wrong radius limit | Open | S7 |
| [C4-27](#c4-27) | Minor | Low | ProcedureChartTypes.Default says it is "every kind of procedure a pilot flies" | Open | S7 |
| [C4-15](#c4-15) | Stale doc | Low | ProcedureNaming's remarks: "both members", and "rather than internal, like the rest of this sub-service" | Open | S7 |
| [C4-5](#c4-5) | Wrong doc | Low | ApproachCodes says the runway is written "exactly as the FAA prints it" | Open | S7 |
| [C4-6](#c4-6) | Wrong doc | Low | The AAUP skip rule is described as "ending in AAUP", but the code matches the word anywhere | Open | S7 |
| [C4-14](#c4-14) | History wording | Low | FE-Buddy 2.x references in the chart recall code | Open | S7 |
| [C4-7](#c4-7) | Wrong doc | Low | SidStarCodes: the trailing-digit fallback also runs when the code doesn't end in the name's version | Open | S7 |
| [C4-4](#c4-4) | Wrong doc | Low | ProcedureAirspaceClass says the highest class is picked with Enumerable.Max; no code does that | Open | S7 |
| [C5-26](#c5-26) | Minor | Low | Domain layer smells (no file, network or settings access found) | Open | S7 |
| [C6-6](#c6-6) | Wrong doc | Med | UserConfigMigrations says moving the settings file "is also a new layout"; the profiles move wasn't | Open | S7 |
| [C6-17](#c6-17) | Wrong doc | Low | DevMode: "from a code constant"; a dev launch turns it on too | Open | S7 |
| [C6-19](#c6-19) | Wrong doc | Low | OutputFormatting remarks miss the reload after a profile switch or import | Open | S7 |
| [C6-32](#c6-32) | Minor | Low | UserConfigAreas.Tabs: "a tab whose only area is the ROI" | Open | S7 |
| [C6-11](#c6-11) | Wrong doc | Low | FlattenInto says nulls are stored as JSON text; they are stored as "" | Open | S7 |
| [C6-36](#c6-36) | Minor | Low | UserConfigFile remarks list 3 of the 7 shared settings as if complete | Open | S7 |
| [C6-37](#c6-37) | Minor | Low | `_stampVersion` starts from UserConfigVersion.Current but is reset everywhere else to UserConfigMigrations.CurrentVersion | Open | S7 |
| [C6-12](#c6-12) | Wrong doc | Low | UserConfigKeys.CoordinatePrecision: 0 means "don't round", not "0 places" | Open | S7 |
| [C6-13](#c6-13) | Wrong doc | Low | UserConfigKeys' stated rule ("keys read in more than one place") doesn't hold either way | Fixed | S7 |
| [C6-9](#c6-9) | Wrong doc | Low | UserConfigMigration's example step is not safe to run twice, and reuses layout 2 | Open | S7 |
| [C6-10](#c6-10) | History wording | Low | Move() example narrates beta.1's VnasAlias rename; profiles summary cites "(issue #324)" | Open | S7 |
| [C6-7](#c6-7) | Stale doc | Low | UserConfigMigrations/UserConfigVersion still describe `UserConfig.json` and "before FE-Buddy has a second layout" | Open | S7 |
| [C6-16](#c6-16) | Wrong doc | Low | UserConfigPortability lists vNAS, VATSIM and VATUSA logins as credentials | Open | S7 |
| [C6-14](#c6-14) | Stale doc | Low | Comments still call the settings file `UserConfig.json` | Open | S7 |
| [C6-15](#c6-15) | Wrong doc | Low | UserConfigTransfer remarks describe only a Replace import | Open | S7 |
| [C6-8](#c6-8) | Wrong doc | Low | "Layout 1 is the one beta.2 and beta.3 saved, before files were stamped": beta.4 and beta.5 saved it too, stamped | Open | S7 |
| [C6-24](#c6-24) | Minor | Low | CredentialStore.TargetPrefix: the uninstaller keeps its own copy, and nothing checks they match | Open | S7 |
| [C7-9](#c7-9) | Wrong doc | Med | "The FAA publishes the d-TPP Metafile 15-18 days before the cycle", but the FAA says 20 days | Agreed, changed | S7 |
| [C7-11](#c7-11) | Wrong doc | Low | The XML parsers throw `XmlException` for malformed files, but docs say `InvalidDataException` | Open | S7 |
| [C7-12](#c7-12) | Minor | Low | DtppDownloader is an instance class with no state; its remark explains why there is nothing to memoize | Open | S7 |
| [C7-13](#c7-13) | Minor | Low | "any amount of whitespace" in the effective-date docs, but the pattern needs at least one | Open | S7 |
| [C5-4](#c5-4) | Wrong doc | Med | EramProperties.IsEmpty is documented backwards | Open | S7 |
| [C5-11](#c5-11) | Wrong doc | Low | EramGeoMapObject.Name is not used in file names | Open | S7 |
| [C6-21](#c6-21) | Wrong doc | Low | AppPaths: "Everything FE-Buddy stores lives under one of them" | Open | S7 |
| [C6-20](#c6-20) | Wrong doc | Low | TempWorkspace: Downloads holds more than "cycle archives ... before extraction" | Open | S7 |
| [C6-22](#c6-22) | Wrong doc | Low | AppDataResetResult.NotDeleted can also hold "saved credentials" | Open | S7 |
| [C6-33](#c6-33) | Minor | Low | FeBuddyHttp.DownloadToFileAsync: the exception doc says "ended before" but any size mismatch throws | Open | S7 |
| [C6-18](#c6-18) | Wrong doc | Low | AppLog.StartFileSink: "Call once from the launch sequence" | Open | S7 |
| [C6-34](#c6-34) | Minor | Low | LogLevel lives in LogEntry.cs | Fixed | S6 |
| [C7-19](#c7-19) | Wrong doc | Low | MarkdownParser class summary: misses the What's New caller, overstates its fit to the content, and two claims are loose | Open | S7 |
| [C7-20](#c7-20) | Stale doc | Low | MarkdownParser `linkBase` example uses the path from before News.md moved to the root | Open | S7 |
| [C7-7](#c7-7) | Wrong doc | Low | `NasrCsvReader.ProcessLines` documents an exception it can't throw | Open | S7 |
| [C7-5](#c7-5) | History wording | Low | "removing the old extract-then-clean step" | Open | S7 |
| [C7-6](#c7-6) | Wrong doc | Low | `EnsureCycleAvailableAsync` docs leave out two exceptions and one progress phase | Open | S7 |
| [C7-28](#c7-28) | Minor | Low | AppVersion remarks: the build reads the version from FE-BUDDY.dll, not the exe | Open | S7 |
| [C7-27](#c7-27) | History wording | Low | LegacySquirrelInstall: "as 2.9.x did" / "(the same as 2.9.x allowed)" | Open | S7 |
| [C5-18](#c5-18) | Wrong doc | Low | SctRegion.Points "(not closed)" but a file may close it | Open | S7 |
| [C7-29](#c7-29) | Minor | Low | SharedDataDownload summary leaves out the virtual airline list | Open | S7 |
| [C7-23](#c7-23) | History wording | Low | TelephonyFiles remarks tell the beta.4 story; keep only the reason | Open | S7 |
| [C7-24](#c7-24) | Minor | Low | The virtual-airline type and property are named after VATSIM-Radar but also hold GNG's rows | Open | S7 |
| [C7-25](#c7-25) | Minor | Low | Invisible literal characters in `TelephonyHtmlParser.CleanText` and its hyphen regex | Open | S7 |
| [C7-14](#c7-14) | Wrong doc | Low | `WxStationXmlParser.ParseAsync` names the wrong caller | Open | S7 |
| [N1-35](#n1-35) | Wrong doc | Low | ProcedureChartTypes: Default is for an absent ChartTypes, not a blank one | Fixed | S14 |
| [N1-36](#n1-36) | Stale doc | Low | Docs still name the Settings page's keys under `Services.AiracService` | Fixed | S14 |

## FeBuddy.Core

<a id="c7-2"></a>

### C7-2 · History wording · Low · csproj News comment: "keeps the resource name" describes the move, not today's lookup

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/FeBuddy.Core.csproj:12-14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/FeBuddy.Core.csproj#L12-L14)

**Says:** `News.md lives at the repository root; LogicalName keeps the resource name NewsService looks for.`

**Problem:** "keeps" describes the name staying the same after the file moved. NewsService also does not look for an exact name: it takes the first manifest resource whose name ends in `News.md` (NewsService.cs:163-164).

**Proposed:** `<!-- News.md from the repository root, built in as the offline fallback (NewsService.GetBundledMarkdown finds it by its News.md ending). -->`

<a id="c7-3"></a>

### C7-3 · Wrong doc · Low · csproj comment says unused usings "are reported", but nothing in the build reports them

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/FeBuddy.Core.csproj:7`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/FeBuddy.Core.csproj#L7) (the same comment is in [`FeBuddy.Wpf.csproj:21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/FeBuddy.Wpf.csproj#L21), [`FeBuddy.Versioning.csproj:17`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Versioning/FeBuddy.Versioning.csproj#L17), [`FeBuddy.Installer.UI.csproj:30`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer.UI/FeBuddy.Installer.UI.csproj#L30), [`FeBuddy.Installer.CustomActions.csproj:22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer.CustomActions/FeBuddy.Installer.CustomActions.csproj#L22))

**Says:** `<!-- Every public member must be documented (CS1591), and unused usings are reported (IDE0005). -->`

**Problem:** `GenerateDocumentationFile` makes CS1591 a warning (FeBuddy.Wpf even sets `TreatWarningsAsErrors` false), and it is what IDE0005 needs in order to run. But .editorconfig:92 sets `dotnet_diagnostic.IDE0005.severity = suggestion`, and no project sets `EnforceCodeStyleInBuild`. A build therefore never reports unused usings; only the IDE shows them, as suggestions.

**Proposed:** `<!-- Writes the XML doc file: a public member without docs warns (CS1591), and the IDE can flag unused usings (IDE0005 needs this file). -->`

**Note:** Re-checked after [#377](https://github.com/Nikolai558/FE-BUDDY/pull/377): still applies in all five projects.

## FeBuddy.Core/Application/Airac

<a id="c1-18"></a>

### C1-18 · Wrong doc · Low · "Never blocking readiness": the d-TPP Metafile download sits in front of each cycle's parse

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs:67-69`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs#L67-L69), 741-747; [`FeBuddy/FeBuddy.Core/Application/Airac/Models/CycleDataState.cs:19-20`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Models/CycleDataState.cs#L19-L20)

**Says:** `... see <see cref="GetDtppAsync"/> - best-effort and never blocking readiness.` / `Never fails or delay-fails the cycle` / `Downloading: The CSV archive is downloading and extracting.`

**Problem:** `DownloadStageAsync` (lines 724-738) awaits `EnsureDtppAsync` before `SetState(Downloaded)`, so the current cycle is not parsed - and the service not ready - until its metafile download finishes or times out (2 min, DtppDownloader.cs:92). It can't *fail* readiness, but it does delay it (the General tab relies on this order: AiracGeneralTabViewModel.cs:443-444). "delay-fails" is unclear. The `Downloading` state also covers the metafile download.

**Proposed:** Summary: "... best-effort: a metafile that can't be downloaded never fails the cycle, though the cycle's parse waits for the attempt." EnsureDtppAsync: "Never fails the cycle: ...". Downloading: "The CSV archive is downloading and extracting, then the cycle's d-TPP Metafile."

<a id="c1-20"></a>

### C1-20 · Wrong doc · Low · Shared-data refresh rules overstated in the cache remarks and the load result doc

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs:78-82`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs#L78-L82); [`FeBuddy/FeBuddy.Core/Application/Airac/Models/AiracSharedDataLoadResult.cs:8-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Models/AiracSharedDataLoadResult.cs#L8-L11)

**Says:** `every AIRAC Service run downloads the latest copy of it into one shared folder` / `a note when it is fresh, an advisory warning when an older copy was used, an error when there is none.`

**Problem:** Only runs that include Wx Stations or Telephony download them (AiracService.cs:138, 148), they go into two folders (`WxStations\`, `Telephony\`), and the virtual airline list is reused for 24 h. A missing *optional* file is an advisory warning, not an error, and a reused copy is an Info note (AiracSharedDataLoader.RefreshMessage, lines 278-304).

**Proposed:** Cache: "... is not kept here at all: each AIRAC Service run that needs it downloads it into its own folder under %APPDATA%\FE-Buddy (see AiracSharedDataLoader)." Result: "a note when it is fresh or a copy under a day old was used, an advisory warning when an older copy was used, and with no copy an error for a required file or an advisory warning for an optional one."

<a id="c1-34"></a>

### C1-34 · Minor · Low · AiracCycleDataCacheEntry: public ParseTask nobody outside uses, and a public data class outside Models/

**Status:** Fixed (not yet merged). `AiracCycleDataCacheEntry` is in `Airac/Models/AiracCycleDataCacheEntry.cs`; `ParseTask` is internal, its doc reworded as proposed · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs:16-59`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs#L16-L59)

**Says:** `/// The in-flight (or completed) parse task. Awaiting this is how a caller gets the parsed data without triggering a second parse.` / `public Task<NasrCsvDataCollection>? ParseTask { get; internal set; }`

**Problem:** Every outside caller uses `GetAsync`; the getter is read only inside the cache (no Wpf/test reads), so the doc describes a use nobody makes. The entry is a public data-only class sharing AiracCycleDataCache.cs, against the "one main type per file" and Models/ rules (Code-Structure.md:8-14).

**Proposed:** Make `ParseTask` `internal` and reword "The parse task, queued, running or done; GetAsync awaits it rather than starting a second parse." Move `AiracCycleDataCacheEntry` to `Airac/Models/AiracCycleDataCacheEntry.cs`.

<a id="t1b-11"></a>

### T1b-11 · Wrong doc · Low · PrepareCyclesAsync "Never throws" - the tests show it throws on cancellation

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs:234`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs#L234); tests [`FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracCycleDataCacheTests.cs:570-592`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/AiracCycleDataCacheTests.cs#L570-L592), 686-696

**Says:** `/// Never throws - a failed cycle is marked <see cref="CycleDataState.Failed"/>.`

**Problem:** Cancellation propagates (lines 309, 754-757, 831-834, 860-863) and null arguments throw (266-269); three tests assert `ThrowsAnyAsync<OperationCanceledException>`.

**Proposed:** `/// A cycle that fails does not throw - it is marked <see cref="CycleDataState.Failed"/>; only cancellation does.`

<a id="c1-25"></a>

### C1-25 · History wording · Low · Issue numbers that only point at the feature request

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/AiracService.cs:555`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/AiracService.cs#L555); [`FeBuddy/FeBuddy.Core/Application/Airac/DuplicateAliasChoices.cs:9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/DuplicateAliasChoices.cs#L9); [`FeBuddy/FeBuddy.Core/Application/Launch/WelcomeNotice.cs:8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Launch/WelcomeNotice.cs#L8)

**Says:** `Makes the user's choices for the run's duplicated alias commands (issue #318).` / `The user's choices for duplicated alias commands (issue #318):` / `The one-time welcome to FE-Buddy 3 (#351), shown ...`

**Problem:** House rule: comments don't point at task numbers; these explain nothing about a bug.

**Proposed:** Delete " (issue [#318](https://github.com/Nikolai558/FE-BUDDY/issues/318))" (both) and " ([#351](https://github.com/Nikolai558/FE-BUDDY/issues/351))".

<a id="c1-36"></a>

### C1-36 · Minor · Low · DuplicateAliasReport.CommandOf: "up to its first space" (tab too)

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/DuplicateAliasReport.cs:150`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/DuplicateAliasReport.cs#L150), 160

**Says:** `/// <summary>A line's command - the text up to its first space - ...` / `trimmed.IndexOfAny([' ', '\t'])`

**Problem:** Also stops at a tab.

**Proposed:** "the text up to its first space or tab".

## FeBuddy.Core/Application/Airac/Airports

<a id="c2-19"></a>

### C2-19 · Wrong doc · Low · Airports alias: "an .ECHO command per airport"; "an airport with an ICAO ID contributes two"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airports/AirportAliasWriter.cs:12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airports/AirportAliasWriter.cs#L12); [`FeBuddy/FeBuddy.Core/Application/Airac/Airports/Models/AirportResults.cs:18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airports/Models/AirportResults.cs#L18)

**Says:** `Generates the Airports.txt alias file: an .ECHO command per airport` / `(an airport with an ICAO ID contributes two)`

**Problem:** An airport whose ICAO ID differs from its FAA ID gets two commands (`.aptSEA`, `.aptKSEA`). One whose ICAO ID equals its FAA ID (`CYAM`) gets one (DistinctIcaoId), and a command another airport already owns is skipped.

**Proposed:** summary: `an .ECHO command per airport identifier - its FAA ID, and its ICAO ID when that differs`. Result: `(an airport whose ICAO ID differs from its FAA ID contributes two)`.

<a id="c2-17"></a>

### C2-17 · Wrong doc · Low · BuildFrequencyLookup names frequency types the sub-service does not use

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airports/AirportBuilder.cs:242-246`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airports/AirportBuilder.cs#L242-L246)

**Says:** `NASR leaves FACILITY null for exactly the facility types this sub-service cares about - CTAF, UNICOM, GCO and AFIS all publish the airport in SERVICED_FACILITY`

**Problem:** The sub-service looks up only `CTAF` and `AWOS`/`ASOS` (lines 99-100). UNICOM, GCO and AFIS are never read.

**Proposed:** `NASR leaves FACILITY null for CTAF (as for UNICOM, GCO and AFIS); those rows publish the airport only in SERVICED_FACILITY, which the FAA documents as always populated.`

<a id="c2-18"></a>

### C2-18 · Wrong doc · Low · Exception docs name only the main NASR group

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airports/AirportBuilder.cs:34-36`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airports/AirportBuilder.cs#L34-L36); [`AirportService.cs:28`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airports/AirportService.cs#L28),33; [`FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalBuilder.cs:47`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalBuilder.cs#L47); [`ArrivalService.cs:33`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalService.cs#L33)

**Says:** `<exception cref="InvalidOperationException">Thrown when allNasrCsvData.Apt has not been parsed.</exception>`

**Problem:** AirportBuilder also calls `RequireFrq()` and `RequireClsArsp()` (lines 84, 88). ArrivalBuilder.ReadProcedures also calls `RequireApt()` (line 417). Each throws the same exception for its own group.

**Proposed:** Airports: `Thrown when APT, FRQ or CLS_ARSP has not been parsed.` (and the param: `APT, FRQ and CLS_ARSP must have been parsed.`). Arrivals: `Thrown when STAR or APT has not been parsed.`

<a id="c2-5"></a>

### C2-5 · Wrong doc · Low · AirportService remark: "every other type in this folder is a step of this pipeline"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airports/AirportService.cs:18-21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airports/AirportService.cs#L18-L21) (same sentence: [`AirwayService.cs:18-21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayService.cs#L18-L21), [`ArrivalService.cs:19-22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalService.cs#L19-L22), [`DepartureService.cs:19-22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Departures/DepartureService.cs#L19-L22))

**Says:** `The only Airports type AiracService and FeBuddy.Harness call directly; every other type in this folder is a step of this pipeline.`

**Problem:** The Map also uses `AirportBuilder.BuildAll` directly (FeBuddy.Wpf/Map/AiracMapLayers.cs:101), so AirportBuilder is not only a step of this pipeline. The app and AiracService also read each sub-service's `*OutputFiles` keys, `AirwaySettingsParser`'s key constants, `AirwayWaypointBuffer`'s limits and `AirwaySettings.DefaultDesignationStrata`.
- Fix (Airports): `The entry point AiracService and FeBuddy.Harness run. The other types here are its steps, though the Map also builds airports with AirportBuilder, and the app reads the file keys in AirportOutputFiles.` For the other three: `The entry point AiracService and FeBuddy.Harness run; the app reads only the file keys and setting constants here.`

**Proposed:** 

## FeBuddy.Core/Application/Airac/Airports/Models

<a id="c2-23"></a>

### C2-23 · Minor · Low · AirportFebProperty: remark paragraph outside &lt;para>, and gaps in the enum values

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airports/Models/AirportFebProperty.cs:17-18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airports/Models/AirportFebProperty.cs#L17-L18), 23-51

**Says:** `/// <see cref="RwyId"/> describes runways, so it is written to the Runways Lines file only,` (no &lt;para>) / `Name = 2, ... Elev = 5,`

**Problem:** The third remark paragraph is not wrapped in `<para>` like the two above it. Values 3-4 are missing because Lat/Lon were removed (commit 7928da7). The explicit numbers have no use: properties are chosen and saved by name (FebProperties.TryParse, AirportFebPropertyOptions).

**Proposed:** wrap the RwyId paragraph in `<para>`; drop the explicit `= n` values (or renumber 0-8).

<a id="c2-20"></a>

### C2-20 · Minor · Low · "service" used for non-entry-point types, against the naming rule

**Status:** Fixed (not yet merged). The comments say "every other Airports type", "the Airways sub-service" and "how the Airways GeoJSON writer groups", as proposed, and the same for Arrivals and Departures; `AirwayServiceResult` says "the top-level Airways sub-service" · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airports/Models/AirportSettings.cs:11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airports/Models/AirportSettings.cs#L11); [`Airways/Models/AirwaySettings.cs:9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/Models/AirwaySettings.cs#L9),12; [`Arrivals/Models/ArrivalSettings.cs:10`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/Models/ArrivalSettings.cs#L10); [`Departures/Models/DepartureSettings.cs:10`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Departures/Models/DepartureSettings.cs#L10); [`Airways/AirwayService.cs:15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayService.cs#L15); [`Airways/Models/AirwayServiceResult.cs:6`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/Models/AirwayServiceResult.cs#L6); [`Airways/Models/AirwayGeojsonOutputBy.cs:4`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/Models/AirwayGeojsonOutputBy.cs#L4)

**Says:** `every other Airports service consumes this typed object` / `Public entry point for the Airways services` / `how the Airways GeoJSON service groups`

**Problem:** Code-Structure.md: "`*Service` - An entry point the app calls. Used for nothing else." The builders and writers are not services, and Airways is one sub-service.

**Proposed:** "every other Airports type", "for the Airways sub-service", "the top-level Airways sub-service", "how the Airways GeoJSON writer groups".

## FeBuddy.Core/Application/Airac/Airways

<a id="c2-2"></a>

### C2-2 · Wrong doc · Med · AirwayBuilder.BuildAll says airways outside the ROI are excluded

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayBuilder.cs:32-33`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayBuilder.cs#L32-L33)

**Says:** `An airway that ends up with no usable geometry (never resolved, or entirely outside the ROI, or every leg dropped as too short to buffer) is excluded from the result.`

**Problem:** Lines 128-145 keep an airway that misses the ROI, with `CrossesRoi = false` and its unclipped line (so the alias file's `All` scope still lists it). Only unresolved airways, airways with no geometry, and fully buffered-away airways are left out.

**Proposed:** `An airway with an unresolved waypoint, no usable geometry, or every leg dropped as too short to buffer is left out of the result. One entirely outside the ROI is kept, marked CrossesRoi false and unclipped, for the alias file.`

<a id="c2-4"></a>

### C2-4 · Stale doc · Low · AirwayAliasWriter says Combined_Alias.txt needs "Concatenate Aliases"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayAliasWriter.cs:12-15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayAliasWriter.cs#L12-L15)

**Says:** `an AIRAC Service run with Concatenate Aliases also copies it into <c>Combined_Alias.txt</c>`

**Problem:** Every run that writes an alias file writes `Combined_Alias.txt` (AiracOutputPaths remarks; Architecture.md "Combined_Alias.txt"). The sub-service is now called Add Custom Aliases, and it only adds the user's own files.

**Proposed:** `The file goes in the output folder's <c>Aliases</c> folder; every AIRAC Service run also copies it into <c>Combined_Alias.txt</c> (see <see cref="AiracOutputPaths"/>).`

<a id="c2-11"></a>

### C2-11 · Wrong doc · Low · "The final point of an airway never appears as a FromPoint" (likely untrue for NASR)

**Status:** Agreed: not a bug. In cycles 2609 to 2611 every airway ends with a terminator row, so use the replacement comment and make no code change ([Q8](010_Decisions.md#q8)) · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayBuilder.cs:190-197`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayBuilder.cs#L190-L197); [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayGeojsonWriter.cs:22-27`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayGeojsonWriter.cs#L22-L27) (also [`Domain/Airways/Models/AirwayPoint.cs:8-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Airways/Models/AirwayPoint.cs#L8-L11), other scope)

**Says:** `The final point of an airway therefore never appears as a FromPoint and is added with a null PointType`

**Problem:** The code's own handling of NASR says each airway ends with a row whose `FROM_POINT` is the last point and whose `TO_POINT` is blank. AirwayNormalizer skips rows with a blank `TO_POINT` (lines 66-70) and comments on the "terminator row". The V6 fixture "as NASR publishes it" (UnitTests/Application/Airac/Airways/AirwayServiceTests.cs:105-108) and the J5 border test both have that row. So the last point is normally picked up with its `FROM_PT_TYPE` by the first loop. A null type happens only when that row is missing. If that ever happens, `uniquePoints.TryAdd` ("first occurrence wins", AirwayGeojsonWriter.cs:239-249) can give a VOR the `airwayIntersections` style in a file where another airway lists it with its type.

**Proposed:** please confirm against a real `AWY_SEG_ALT.csv`. If confirmed: `A row's TO_POINT carries no type. NASR ends each airway with a row for its last point (FROM_POINT set, TO_POINT blank), so every point normally gets its type from a FROM_POINT. The TO_POINT pass only catches a point with no such row, which is added with a null PointType.` Optionally, in GenerateSymbolsAndText, let a later copy with a non-null type replace a stored null-type copy.

<a id="c2-12"></a>

### C2-12 · Minor · Low · ResolveOrderedPoints sends border markers to the waypoint lookup

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayBuilder.cs:216-219`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayBuilder.cs#L216-L219); [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayNormalizer.cs:23-25`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayNormalizer.cs#L23-L25)

**Says:** `foreach (... segment in rawSegments) { TryAddPoint(allNasrCsvData, segment.ToPoint, pointType: null, points, seenIds); }`

**Problem:** AirwayNormalizer says a reference-only point "must never be sent to waypoint coordinate resolution". The TO_POINT pass sends every `TO_POINT`, so `U.S. CANADIAN BORDER-4` goes to `WaypointLocator.Find`. It only comes back empty because no NAV or APT ID matches.

**Proposed:** build `AirwayReferenceOnlyPoints.BuildSet(rawSegments)` once in ResolveOrderedPoints and skip `TO_POINT`s in it.

<a id="c2-8"></a>

### C2-8 · Wrong doc · Low · AirwayGeometryBuilder does not collect "every unresolved ID"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayGeometryBuilder.cs:28-31`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayGeometryBuilder.cs#L28-L31), 57-85, 131-133, 168-200

**Says:** `it is skipped like an airway gap and building carries on, so every unresolved ID is collected.`

**Problem:** (1) When the start of a segment fails, its end is never looked up (line 58). (2) When no later segment has both ends resolvable, the loop `break`s (line 75-78). For A->X, X->Y, Y->B with X and Y missing, only X is reported, so "every unresolved ID lands in the exclusion message rather than only the first" (line 80-81) is false. Since any unresolved ID excludes the airway anyway, `HasResolvableSegmentAhead` only saves a few lookups, and the comment at 131-132 ("still need to be added to the output") describes geometry that is thrown away. Also, each excluded airway gets one Warning per unresolved ID here ("This airway was excluded from all output.") plus AirwayBuilder's own summary Warning (AirwayBuilder.cs:108-110), so the same fault is reported N+1 times.

**Proposed:** remove `HasResolvableSegmentAhead` and the `break`. Look up both ends every time, and record each missing one. Keep the doc as it is (it is then true), and reword line 131 to `// Finish the last run of coordinates.`. Optionally make the per-ID message `Info` (or drop it), since AirwayBuilder's warning already names them all.

<a id="c2-10"></a>

### C2-10 · Wrong doc · Low · NeedsCrcDefaults remark: designation files' "rest are written as per-feature overrides"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwaySettingsParser.cs:182-187`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwaySettingsParser.cs#L182-L187)

**Says:** `A designation file can hold every class (its isDefaults Feature takes the majority class, the rest are written as per-feature overrides), so any designation file of that kind needs all three.`

**Problem:** Only Lines get per-feature overrides (AirwayGeojsonWriter.GenerateLines 207-214). The Symbols and Text files use the majority class for every point, with no override (GenerateSymbols/GenerateText). All three classes are still needed for Symbols and Text, but for another reason: the majority class is not known until the airways are built.

**Proposed:** `A designation file can hold every class. Its isDefaults Feature takes the majority class, which is not known until the airways are built, and a Lines file writes the other classes as per-feature overrides. So any designation file of that kind needs all three.`

<a id="c2-22"></a>

### C2-22 · Minor · Low · AirwayWaypointBuffer.Buffer param doc: misplaced "+/-180" clause

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayWaypointBuffer.cs:44-47`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayWaypointBuffer.cs#L44-L47)

**Says:** `(<paramref name="fixRadiusNm"/> for a 5-character fix, <paramref name="navaidRadiusNm"/> otherwise, including a real fix sitting exactly on +/-180)`

**Problem:** It reads as if a fix on +/-180 gets the NAVAID radius. The code gives it the radius for its ID length (fix radius for 5 characters), matched through NormalizeLongitude.

**Proposed:** `A leg endpoint that matches one of them - even a real waypoint sitting exactly on +/-180 - is buffered (fixRadiusNm for a 5-character fix, navaidRadiusNm otherwise).`

<a id="t1a-35"></a>

### T1a-35 · Wrong doc · Low · AirwayWaypointBuffer.Buffer docs contradict its code and its tests

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayWaypointBuffer.cs:43-50`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayWaypointBuffer.cs#L43-L50), 54

**Says:** `/// <paramref name="navaidRadiusNm"/> otherwise, including a real fix sitting exactly on +/-180).` / `/// <returns>The buffered legs, plus a warning for each leg dropped as too short to buffer.</returns>`

**Problem:** `RadiusFor` picks the radius by ID length only (143), so a 5-character fix on ±180 gets `fixRadiusNm`, as AirwayWaypointBufferTests.cs:29-46 says ("buffered (2.5 NM)"). A dropped leg is `LogLevel.Info` (92-97; AirwayMessageLevelsTests.cs:36-53), not a warning.

**Proposed:** `(<paramref name="fixRadiusNm"/> for a 5-character fix, <paramref name="navaidRadiusNm"/> otherwise, a real waypoint on +/-180 included)`; `<returns>The buffered legs, plus an Info message for each leg dropped as too short to buffer.</returns>`

## FeBuddy.Core/Application/Airac/Airways/Models

<a id="c2-1"></a>

### C2-1 · Wrong doc · Med · AirwayGeojsonOutputBy.Designation says files group by AWY_DESIGNATION

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/Models/AirwayGeojsonOutputBy.cs:15-19`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/Models/AirwayGeojsonOutputBy.cs#L15-L19)

**Says:** `Airways are grouped by their shared <c>AWY_BASE.AWY_DESIGNATION</c> (e.g. all "J" airways in one file, ...)`

**Problem:** The grouping uses `Airway.Designation` (AirwayGeojsonWriter.GroupsOf line 157), which is `AirwayClassifier.DeriveDesignation(awyId)`, the ID's leading letters (AirwayBuilder.cs:74). AirwayClassifier's own remark and Architecture.md both say it is deliberately *never* `AWY_DESIGNATION` (that field files Q/T routes under `RN`). The doc describes exactly the behaviour the code avoids.

**Proposed:** `Airways are grouped by designation, the leading letters of their ID (<see cref="Airway.Designation"/>): every J airway in one file, every V airway in another.`

**Note:** Also found as [D1b-8](#d1b-8).

<a id="c2-9"></a>

### C2-9 · Wrong doc · Low · AirwayBufferResult says buffered legs are "always disjoint"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/Models/AirwayBufferResult.cs:12-15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/Models/AirwayBufferResult.cs#L12-L15)

**Says:** `Always disjoint (no two legs share an endpoint), since each leg has been pulled back from both of its original endpoints.`

**Problem:** Radii may be 0 (`FixBufferNm`/`NavaidBufferNm` accept 0-10, AirwaySettingsParser.cs:175-176). An endpoint that matches no real waypoint is also never pulled back (radius 0, AirwayWaypointBuffer.RadiusFor). With a 0 NM fix radius, two legs meeting at a five-character fix share that endpoint.

**Proposed:** `One two-point LineString per surviving leg, each pulled back from its endpoints by their buffer radius (0 for an ROI-clip or antimeridian vertex, or a radius set to 0).`

<a id="c2-13"></a>

### C2-13 · History wording · Low · AliasRoiScope.All "(the historical behaviour)"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/Models/AliasRoiScope.cs:8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/Models/AliasRoiScope.cs#L8)

**Says:** `<summary>Every airway, whatever the ROI (the historical behaviour).</summary>`

**Problem:** Narrates history.

**Proposed:** `<summary>Every airway, whatever the ROI. The default.</summary>`

<a id="c2-16"></a>

### C2-16 · Wrong doc · Low · RoiAirways scope is not "exactly the airways the GeoJSON draws"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/Models/AliasRoiScope.cs:12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/Models/AliasRoiScope.cs#L12); [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayAliasWriter.cs:40-41`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayAliasWriter.cs#L40-L41)

**Says:** `Exactly the airways the GeoJSON draws: those whose line crosses the ROI`

**Problem:** The scope keeps every airway with `CrossesRoi`. The GeoJSON can still leave some of them out: with HighLow, a designation that has no High/Low file is left out of both (AirwayService advisory). The GeoJSON may also be off altogether.

**Proposed:** `The airways whose line crosses the ROI, even when none of their waypoints is inside it (Airway.CrossesRoi) - the ones the GeoJSON can draw.` Apply the same wording in the AirwayAliasWriter comment.

## FeBuddy.Core/Application/Airac/Arrivals

<a id="c2-14"></a>

### C2-14 · History wording · Low · ArrivalAliasWriter.CommandName "as in FE-Buddy 2.x"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalAliasWriter.cs:91-92`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalAliasWriter.cs#L91-L92)

**Says:** `The same form as a departure's command, as in FE-Buddy 2.x, so a controller types a SID and a STAR the same way.`

**Problem:** Explains by reference to an earlier version, not a bug.

**Proposed:** `The same form as a departure's command, so a controller types a SID and a STAR the same way.`

<a id="c2-21"></a>

### C2-21 · Minor · Low · Arrivals file-key summaries omit `_STAR_`

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalOutputFiles.cs:21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalOutputFiles.cs#L21),24,27

**Says:** `<summary>Every procedure's <c>_Lines.geojson</c> file.</summary>`

**Problem:** Arrival files end `_STAR_Lines.geojson` / `_STAR_Symbols.geojson` / `_STAR_Text.geojson` (line 72).

**Proposed:** `Every procedure's <c>_STAR_Lines.geojson</c> file.` (and Symbols, Text).

<a id="c2-29"></a>

### C2-29 · Minor · Low · Arrivals is a copy of Departures - duplicated logic that could be shared

**Status:** Deferred: share the code after 3.0; S11 adds the TODO.md item ([D7](010_Decisions.md#d7)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** `FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/{ArrivalFilter.cs`, [`ArrivalAliasWriter.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalAliasWriter.cs), [`ArrivalGeojsonWriter.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalGeojsonWriter.cs), [`ArrivalSettingsParser.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalSettingsParser.cs), [`ArrivalBuilder.cs:30-40`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalBuilder.cs#L30-L40), 272-363, 493-513, [`Models/ArrivalAmendmentFilter.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/Models/ArrivalAmendmentFilter.cs), [`Models/ArrivalRoiMode.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/Models/ArrivalRoiMode.cs)} vs the same Departures files

**Says:** `git log`: 3a77d33 "Add the Arrivals (STAR) sub-service, mirroring Departures"

**Problem:** Code-Structure.md: "Something two features share: the lowest layer both can see. Never copy it." Identical apart from type names: `ArrivalAmendmentFilter`/`DepartureAmendmentFilter` and `ArrivalRoiMode`/`DepartureRoiMode` (whole enums); `ByRoi`, `IsRecentlyAmended`, `AmendmentCutoff` (~110 lines); `ParseAmendedOnOrAfter` and the amendment/RoiMode reading in the parsers; the alias writers; the GeoJSON writers except the folder ARTCC and the name property; and in the builders `FixPointTypes`, `NavaidPointTypes`, `Resolve`, `TryLocate`, `DistinctPoints`, `ServedAirports`, `ParseNasrDate` and `KeyComparer`. (Domain's DepartureNaming/ArrivalNaming duplicate `AmendmentNumbers`, `Clean` and `NotAssigned` the same way.) A fix made to one copy can be missed in the other.

**Proposed:** share an enum each (`ProcedureAmendmentFilter`, `ProcedureRoiMode`) in `Application/Airac/Models`. Add an `AmendmentCutoff` helper taking the dates, and a `ProcedureSettingsReader` for the amendment/RoiMode keys. Move the point-type to `WaypointType` mapping into WaypointLocator (e.g. `WaypointLocator.TypeForPointType(string)`), and `ParseNasrDate` into NasrCsvReader. The writers and builders could follow as one generic `ProcedureAliasWriter`/`ProcedureGeojsonWriter` over a small interface. Reasonable to do after 3.0, given the Wpf and test churn (70 Wpf references to the four enums).

**Note:** See also [C5-25](#c5-25). **Kyle:** leave it for after 3.0; add a TODO.md item ([D7](010_Decisions.md#d7)).

## FeBuddy.Core/Application/Airac/Arrivals/Models

<a id="c2-30"></a>

### C2-30 · Minor · Low · ArrivalSettings.ArtccFilter is per airport, not per procedure

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/Models/ArrivalSettings.cs:40-43`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/Models/ArrivalSettings.cs#L40-L43)

**Says:** `The ARTCCs whose procedures are included, or empty for every ARTCC.`

**Problem:** ArrivalFilter.ByProcedure (lines 65-81) keeps the served airports whose `ArtccFor` is listed, so a STAR shared by ZDC and ZNY is kept only for the listed centre's airports.

**Proposed:** `The ARTCCs whose airports' copies of each arrival are included (see ArrivalProcedure.ArtccFor), or empty for every ARTCC.`

## FeBuddy.Core/Application/Airac/ArtccBoundaries

<a id="c3-9"></a>

### C3-9 · Wrong doc · Low · `ArtccBoundaryFilter` / `NavaidFilter` say they match ignoring case; they use the passed collection's own comparison

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/ArtccBoundaries/ArtccBoundaryFilter.cs:12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/ArtccBoundaries/ArtccBoundaryFilter.cs#L12),23 ; [`FeBuddy/FeBuddy.Core/Application/Airac/Navaids/NavaidFilter.cs:17`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Navaids/NavaidFilter.cs#L17),26

**Says:** `<param name="locationFilter">The LocationIds to keep, matched ignoring case.` ... `locationFilter.Contains(ring.Location.LocationId)`

**Problem:** `IReadOnlyCollection<string>.Contains` is ordinal unless the collection is a case-insensitive set. `ArtccBoundarySettingsParser` passes `ReadAreaArtccs`'s `string[]` (SubServiceSettingsReader.cs:118-123), so the ARTCC filter is ordinal and works only because both sides are upper-cased. The tests say so explicitly ("matches ignoring case when given a case-insensitive set", ArtccBoundaryFilterTests.cs:41).

**Proposed:** Either word it as it is ("matched with the collection's own comparison; the parser hands over upper-cased IDs, as the built rings' are" / "the parser's set ignores case") or make it true: `HashSet<string> keep = new(locationFilter, StringComparer.OrdinalIgnoreCase)` (same for NavaidFilter).

**Note:** Also found as [T1b-27](#t1b-27).

## FeBuddy.Core/Application/Airac/ArtccBoundaries/Models

<a id="c3-22"></a>

### C3-22 · Minor · Low · "every other ... service consumes this typed object" - they are types, not services

**Status:** Fixed (not yet merged). ARTCC Boundaries, Fixes and NAVAIDs say "every other ... type", with [C2-20](#c2-20) · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/ArtccBoundaries/Models/ArtccBoundarySettings.cs:10-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/ArtccBoundaries/Models/ArtccBoundarySettings.cs#L10-L11) ; [`FeBuddy/FeBuddy.Core/Application/Airac/Fixes/Models/FixSettings.cs:10-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Fixes/Models/FixSettings.cs#L10-L11) ; [`FeBuddy/FeBuddy.Core/Application/Airac/Navaids/Models/NavaidSettings.cs:10-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Navaids/Models/NavaidSettings.cs#L10-L11)

**Says:** `every other ARTCC Boundaries service consumes this typed object, never the raw dictionary.`

**Problem:** Code-Structure.md reserves `*Service` for entry points; WxStationSettings.cs:10 says "type".

**Proposed:** "every other ARTCC Boundaries type" (same for Fixes, NAVAIDs).

## FeBuddy.Core/Application/Airac/ConcatenateAliases

<a id="c3-14"></a>

### C3-14 · History wording · Low · "A file from before the end line existed" in CombinedAliasFileWriter

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/CombinedAliasFileWriter.cs:41-42`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/CombinedAliasFileWriter.cs#L41-L42)

**Says:** `A file from before the end line existed (FE-Buddy's section was last) loses everything from its start line down.`

**Problem:** Refers to 3.0.0-alpha.2's `vNAS_Alias.txt` layout (the end line came in alpha.3, ChangeLog.md:548-551). The behaviour itself (a start line with no end line after it strips to the end of the file, lines 121-137) is a sensible general rule and should stay.

**Proposed:** "A start line with no end line after it (deleted by hand, say) drops everything from the start line down."

<a id="c3-23"></a>

### C3-23 · Minor · Low · `CombinedAliasFileWriter` docs: layout sample and exceptions

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/CombinedAliasFileWriter.cs:31-33`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/CombinedAliasFileWriter.cs#L31-L33),82

**Says:** `/// (custom alias file 1)` directly followed by `/// (custom alias file 2, ...)`; `<exception cref="IOException">Thrown when an FE-Buddy alias file cannot be read or the file cannot be written.`

**Problem:** The code puts a blank line before every custom file (lines 199-212), not only the first. `File.ReadAllText`/`File.WriteAllText` also throw `UnauthorizedAccessException`, which is not an `IOException`.

**Proposed:** Add a blank `///` line between the two custom-file lines; add `<exception cref="UnauthorizedAccessException">` with the same text.

<a id="c3-13"></a>

### C3-13 · History wording · Low · "as the code still calls it" in ConcatenateAliasesSettingsParser

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/ConcatenateAliasesSettingsParser.cs:13-14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/ConcatenateAliasesSettingsParser.cs#L13-L14)

**Says:** `the Add Custom Aliases sub-service (Concatenate Aliases, as the code still calls it)`

**Problem:** Narrates the rename.

**Proposed:** "the Add Custom Aliases sub-service (`ConcatenateAliases` in code)".

<a id="c3-25"></a>

### C3-25 · Minor · Low · Public members with no caller outside their file

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/ConcatenateAliasesSettingsParser.cs:41`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/ConcatenateAliasesSettingsParser.cs#L41),61 ; [`FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/CombinedAliasFileWriter.cs:61`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/CombinedAliasFileWriter.cs#L61),64 ; [`FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs:62`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs#L62)

**Says:** `public const string SourcesPrefix = "Sources.";`, `public static bool AddsCustomAliasFiles(...)` (tests only outside the file), `public const string FeBuddySectionMarker`, `public const string FeBuddySectionEndMarker`

**Problem:** Nothing outside these files uses them, and the tab keeps its own `private const string SourcesKey = "Sources";` instead of the parser's `SourcesPrefix` (it does use `FilePathKey`/`UrlKey`/`CredentialIdKey`), so the key prefix is defined twice.

**Proposed:** Make `FeBuddySectionMarker`/`FeBuddySectionEndMarker` private (or internal) and `AddsCustomAliasFiles` private; have the tab build its keys from `ConcatenateAliasesSettingsParser.SourcesPrefix` (as TelephonyViewModel does with `VirtualAirlinesPrefix`).

<a id="c3-4"></a>

### C3-4 · Wrong doc · Low · `ConcatenateAliasesSettingsParser.IgnoredKeys` says the AIRAC Service sets `OutputDirectory` on this block; it doesn't

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/ConcatenateAliasesSettingsParser.cs:54-55`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/ConcatenateAliasesSettingsParser.cs#L54-L55)

**Says:** `/// <summary>Keys the AIRAC Service sets on every block, which this sub-service does not need.</summary>`

**Problem:** `AiracService.RunAsync` parses `settings.ConcatenateAliases` as given (AiracService.cs:196); only `RunSubServiceAsync` adds `OutputDirectory` (AiracService.cs:531-534), and the Add Custom Aliases block never goes through it. The tab's `BuildSettingsBlock` doesn't send it either (ConcatenateAliasesViewModel.cs:175-195). Settings-Reference.md:216 and :519 have it right ("not Add Custom Aliases'", "accepted and ignored").

**Proposed:** "`OutputDirectory`, which every other sub-service's block carries: accepted and ignored if sent here (Add Custom Aliases writes nothing of its own)."

## FeBuddy.Core/Application/Airac/Departures

<a id="c2-15"></a>

### C2-15 · Stale doc · Low · "Efficient Linestring Handling" is a term from the old Developer_Notes planning doc

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Departures/DepartureGeometryBuilder.cs:9-10`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Departures/DepartureGeometryBuilder.cs#L9-L10); [`FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalGeometryBuilder.cs:9-10`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalGeometryBuilder.cs#L9-L10)

**Says:** `Builds the single MultiLineString a departure's Lines file holds, using Efficient Linestring Handling so segments shared by several bodies and transitions are drawn once.`

**Problem:** The term is defined nowhere in the code or docs. `git log -S` shows it came from `FeBuddy/Developer_Notes.md`, a planning doc. The rule lives in `LineStringMerger`.

**Proposed:** `..., merging the paths with <see cref="LineStringMerger"/> so segments shared by several bodies and transitions are drawn once.` (same for Arrivals: "transitions and bodies").

## FeBuddy.Core/Application/Airac/Departures/Models

<a id="c2-7"></a>

### C2-7 · Wrong doc · Low · Departures/Arrivals feb.* "in the order below" is not enforced

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Departures/Models/DepartureFebProperty.cs:5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Departures/Models/DepartureFebProperty.cs#L5); [`FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/Models/ArrivalFebProperty.cs:5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/Models/ArrivalFebProperty.cs#L5); [`DepartureGeojsonWriter.cs:180`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Departures/DepartureGeojsonWriter.cs#L180); [`ArrivalGeojsonWriter.cs:180`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Arrivals/ArrivalGeojsonWriter.cs#L180)

**Says:** `each selected value is written as "feb.<name>", in the order below.` / `FebProperties.Add(attributes, ..., settings.FebProperties, property => ...)`

**Problem:** `SubServiceSettingsReader.ReadFebProperties` returns the properties "in the order listed", and these writers pass that list unsorted, so the order is the block's, not the enum's. Only the app happens to send enum order; the Harness or a hand-built block does not have to. Airways sorts explicitly (`settings.FebProperties.OrderBy(p => p)`, AirwayGeojsonWriter.cs:218/309/350), so the four sub-services behave differently.

**Proposed:** in both writers pass `settings.FebProperties.Order()`, as Airways does (and in AirportGeojsonWriter.cs:171/207 for consistency); or drop "in the order below" from both enums.

## FeBuddy.Core/Application/Airac/Fixes

<a id="c3-11"></a>

### C3-11 · Wrong doc · Low · `FixService` comment lists only some reasons a run writes nothing

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Fixes/FixService.cs:80-81`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Fixes/FixService.cs#L80-L81)

**Says:** `Covers every reason the run could end with nothing written: no fixes at all, an empty ROI, or (in the Chart file layout) every chart present having been excluded.`

**Problem:** Also: the FixUse layout with every fix use present excluded (the parser only throws when every *known* use is excluded, FixSettingsParser.cs:88), and ChartAndFixUse with no listed combination having a fix inside the ROI.

**Proposed:** "Covers every reason the run could end with nothing written: no fixes at all, an empty ROI, every group present excluded (FixUse or Chart layout), or no listed combination with a fix in the ROI."

## FeBuddy.Core/Application/Airac/Models

<a id="c1-22"></a>

### C1-22 · Wrong doc · Low · AiracServiceProgress.PercentComplete isn't a percentage; it is only ever 100 for "this step finished"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Models/AiracServiceProgress.cs:9-10`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Models/AiracServiceProgress.cs#L9-L10)

**Says:** `/// <param name="PercentComplete">Overall completion 0-100 when known, otherwise <see langword="null"/>.</param>`

**Problem:** AiracService passes only `100`, at the end of each sub-service or step (AiracService.cs:454, 475, 541); the GUI reads it as a flag (`p.PercentComplete >= 100`, AiracServiceViewModel.cs:597). Nothing reports overall completion.

**Proposed:** Replace with `bool Finished = false` ("Whether the step named by SubService has finished") and update the three call sites and AiracServiceViewModel.cs:597; or at least reword: "100 when the step named by SubService has finished; null while it runs."

<a id="c1-29"></a>

### C1-29 · Wrong doc · Low · AiracServiceResult.OutputDirectory "exists only if the run wrote something"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Models/AiracServiceResult.cs:22-26`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Models/AiracServiceResult.cs#L22-L26)

**Says:** `/// It exists only if the run wrote something.`

**Problem:** With `ExistingOutputAction.Overwrite` an earlier run's folder exists even when this run wrote nothing.

**Proposed:** "It may not exist when the run wrote nothing."

<a id="c1-28"></a>

### C1-28 · Wrong doc · Low · AiracServiceSettings: the blocks are built from each tab's view-model, not from the saved config

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Models/AiracServiceSettings.cs:9-14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Models/AiracServiceSettings.cs#L9-L14)

**Says:** `Each block is the key/value dictionary the GUI builds from the saved <c>UserConfig</c> values - never from what is on screen - so a run always uses what was saved.`

**Problem:** Each `BuildSettingsBlock()` reads the tab's view-model properties (e.g. AirwaysViewModel.cs:275-295). It matches what was saved only because the run first makes the user save every unsaved tab (AiracServiceViewModel.cs:525, 878-905).

**Proposed:** "Each block is the key/value dictionary the GUI builds from its tab, after making the user save every unsaved tab, so a run always uses what was saved."

<a id="c1-19"></a>

### C1-19 · Wrong doc · Low · CycleDataState.Failed says parses are retried; they aren't

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Models/CycleDataState.cs:31-32`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Models/CycleDataState.cs#L31-L32)

**Says:** `/// <summary>Download or parse failed after a retry. A real error.</summary>`

**Problem:** Only the CSV download has one retry (`DownloadWithOneRetryAsync`); a parse failure sets `Failed` at once (`ObserveParseAsync`, AiracCycleDataCache.cs:835-839) and is never retried in the session.

**Proposed:** `/// <summary>The CSV download failed twice, or the parse failed. A real error.</summary>`

## FeBuddy.Core/Application/Airac/Navaids

<a id="c3-10"></a>

### C3-10 · Wrong doc · Low · `NavaidGeojsonWriter` remarks: the alias file covers every NAVAID, not "every included" one

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Navaids/NavaidGeojsonWriter.cs:27-29`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Navaids/NavaidGeojsonWriter.cs#L27-L29)

**Says:** `while <see cref="NavaidAliasWriter"/> covers every included NAVAID regardless of the ROI`

**Problem:** `NavaidService.Run` passes `buildResult.Navaids` to the alias writer (NavaidService.cs:62-64), so it also covers types left out by `ExcludedTypes`; "included" reads as after `ExcludedTypes`. NavaidAliasWriter.cs:21-23 has it right.

**Proposed:** "while `NavaidAliasWriter` covers every NAVAID, whatever `ExcludedTypes` and the ROI say."

**Note:** Also found as [T1b-26](#t1b-26).

## FeBuddy.Core/Application/Airac/Procedures

<a id="c4-1"></a>

### C4-1 · Stale doc · Med · ProceduresJsonWriter says the Markdown document de-duplicates shared STARs; it doesn't

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProceduresJsonWriter.cs:18-21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProceduresJsonWriter.cs#L18-L21)

**Says:** `Unlike the Markdown document, a shared STAR is written under every included airport that serves it - this is per-airport data, not a change report, so there is no de-duplication.`

**Problem:** `Procedure_Changes.md` also lists a shared chart under every included airport. `ProcedureChangesMarkdownWriter.BuildEntries` (lines 358-372) adds an entry for every (airport, procedure) pair, and its own remarks (lines 18-20) say "is listed under each of them, so every airport's list is complete". The only difference is that the Markdown adds an "Also serves" line. User-Guide.md:248 says the same ("A STAR at several airports is listed under each").

**Proposed:** `A shared STAR is written under every included airport that serves it, as the Markdown document lists it; the JSON has no "Also serves" line, since each airport's entry stands alone.`

**Note:** Also found as [T1a-36](#t1a-36).

<a id="c4-22"></a>

### C4-22 · Minor · Low · The help command's note names the guide "the FE-Buddy Command Guide"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ChartRecallHelpCommand.cs:51-53`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ChartRecallHelpCommand.cs#L51-L53)

**Says:** `@"your Alias Command documentation or", @"the FE-Buddy Command Guide.",`

**Problem:** The guide is titled "FE-Buddy Alias Command Guide" (AliasGuideContent.Title, line 42; User-Guide.md:522-524). Controllers see this text in CRC.

**Proposed:** `@"the FE-Buddy Alias Command Guide.",` and update the pinned line in ChartRecallHelpCommandTests.cs:13.

<a id="c4-2"></a>

### C4-2 · Wrong doc · Low · The Markdown writer's remarks still say every chart brought in by a route fix is labelled (#371)

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureChangesMarkdownWriter.cs:21-22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureChangesMarkdownWriter.cs#L21-L22)

**Says:** `A chart included only because its route has a fix in the user's facility says so on a child line, naming the first few fixes.`

**Problem:** Since [#371](https://github.com/Nikolai558/FE-BUDDY/issues/371), `ProcedureSelection.Select` (lines 160-173) sets `IncludedForFacilityFixes` only at airports outside `PrimaryFacility`. A chart at one of the facility's own airports that only its route brought in gets no "In ZDC" line. The writer's inline comment (line 198) and `Procedure.IncludedForFacilityFixes` are already correct; this class remark is not.

**Proposed:** `A chart included only because its route has a fix in the user's facility, at an airport outside that facility, says so on a child line, naming the first five fixes.`

<a id="c4-23"></a>

### C4-23 · Minor · Low · The re-added "Info" line in Procedure_Changes.md: casing and a missing full stop

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureChangesMarkdownWriter.cs:189-190`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureChangesMarkdownWriter.cs#L189-L190)

**Says:** `"was added as a \"New\" procedure in the d-Tpp MetaFile. Check carefully");`

**Problem:** Every other user-facing text writes "d-TPP Metafile" (ProcedureService.cs:202, User-Guide.md), and this sentence has no full stop.

**Proposed:** `... added as a \"New\" procedure in the d-TPP Metafile. Check it carefully.` and update ProcedureChangesMarkdownWriterTests.cs:246.

<a id="c4-24"></a>

### C4-24 · Minor · Low · Both document writers' `settings` param docs list too few of the settings they read

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureChangesMarkdownWriter.cs:40`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureChangesMarkdownWriter.cs#L40); [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProceduresJsonWriter.cs:36`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProceduresJsonWriter.cs#L36)

**Says:** `The parsed settings; <see cref="ProcedureSettings.PrimaryFacility"/> is read.`

**Problem:** Both also read `OutputDirectory` and `FileNames` (lines 103-106 and 75-78). ChartRecallAliasWriter.cs:24-25 lists all of the settings it reads.

**Proposed:** `...; PrimaryFacility, OutputDirectory and FileNames are read.` (the JSON writer adds JsonFields).

<a id="c4-10"></a>

### C4-10 · Wrong doc · Low · The AirportProcedures error message gives an approach as its example; the setting takes departures and arrivals only

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureSettingsParser.cs:227-229`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureSettingsParser.cs#L227-L229); [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/Models/ProcedureAirportPick.cs:5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/Models/ProcedureAirportPick.cs#L5)

**Says:** `"procedure name joined by exactly one '|', e.g. \"PIT|ILS OR LOC RWY 28C\"."` / `<param name="ProcedureName">The procedure's base chart name, as typed.</param>`

**Problem:** Picks take departures and arrivals only, matched by root name (ProcedureSelection.cs:25-29). An approach is left out with a warning, so the user-facing example shows something that won't work. The record's doc says "base chart name", but picks are root names (`GRUUB`); the harness (HarnessSettings.cs) and Settings-Reference.md:436 use `PIT|GRUUB`.

**Proposed:** message `e.g. \"PIT|GRUUB\"`; param `The departure or arrival's name as typed, e.g. <c>GRUUB</c>, matched by its root name.`

**Note:** Also found as [D1a-16](#d1a-16).

## FeBuddy.Core/Application/Airac/Procedures/Models

<a id="c4-13"></a>

### C4-13 · History wording · Low · "Feature #367 / #371" task numbers in 12 comments

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/Models/ProcedureSettings.cs:110`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/Models/ProcedureSettings.cs#L110); [`NasrProcedureRoutes.cs:9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/NasrProcedureRoutes.cs#L9); [`ProcedureChangesMarkdownWriter.cs:198`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureChangesMarkdownWriter.cs#L198); [`ProcedureSelection.cs:37`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureSelection.cs#L37), 159; [`ProcedureService.cs:102`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureService.cs#L102), 154; [`ProceduresJsonWriter.cs:205`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProceduresJsonWriter.cs#L205); [`ProceduresWithFixInFacility.cs:8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProceduresWithFixInFacility.cs#L8); [`RoutePointArtccResolver.cs:8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/RoutePointArtccResolver.cs#L8); [`FeBuddy/FeBuddy.Core/Domain/Procedures/Models/FacilityFixMatch.cs:5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Procedures/Models/FacilityFixMatch.cs#L5); [`Procedure.cs:78`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Procedures/Models/Procedure.cs#L78)

**Says:** `(Feature #367)`, `(Feature #371)`, `(Features #367, #371)`

**Problem:** Code-Structure.md's house rule says comments never point at task numbers. Each of these comments already explains the behaviour in words, so the numbers add only history.

**Proposed:** delete each parenthetical (for example, ProcedureSelection.cs:37 becomes `... as that says nothing new.`). Settings-Reference.md:437 and 458 carry the same numbers.

<a id="c4-16"></a>

### C4-16 · Wrong doc · Low · ProcedureSettings.PrimaryFacility describes only the section order

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/Models/ProcedureSettings.cs:73-77`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/Models/ProcedureSettings.cs#L73-L77)

**Says:** `The facility whose section is listed first in both documents, or null for none (facilities are then all ordered alphabetically).`

**Problem:** The fix-in-facility rule also checks routes against this facility (ProcedureService.cs:113-133), and ProcedureSelection uses it to leave charts at its own airports unlabelled (ProcedureSelection.cs:160-161).

**Proposed:** append `It is also the facility the procedures-with-a-fix-in-my-facility rule checks routes against; a chart that rule brings in at one of its own airports isn't labelled.`

<a id="c4-17"></a>

### C4-17 · Wrong doc · Low · ProcedureSettings.JsonFields' list of fields "always written" is off

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/Models/ProcedureSettings.cs:124-129`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/Models/ProcedureSettings.cs#L124-L129)

**Says:** `on top of the fields always written (<c>cycle</c>, <c>effectiveDate</c>, <c>airportId</c>, a procedure's <c>name</c>)`

**Problem:** `effectiveDate` is written only when the metafile has one (ProceduresJsonWriter.cs:53-56). `includedBecause` and `facilityFixes` are written whatever the fields, on a chart only its route brought in (lines 204-210).

**Proposed:** `(... <c>cycle</c>, <c>effectiveDate</c> when known, <c>airportId</c>, a procedure's <c>name</c>, and <c>includedBecause</c>/<c>facilityFixes</c> on a chart only its route brought in)`.

## FeBuddy.Core/Application/Airac/Telephony

<a id="c3-12"></a>

### C3-12 · History wording · Low · Issue numbers that only narrate history in TelephonyBuilder

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Telephony/TelephonyBuilder.cs:24-25`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/TelephonyBuilder.cs#L24-L25)

**Says:** `Only the first entry for each 3LD (or U.S. special identifier) and telephony is written, whatever its company, country or virtual organization (issues #336, #339):`

**Problem:** The issue numbers don't explain a bug-driven choice; the sentence stands on its own (house rule: no task numbers).

**Proposed:** Drop "(issues [#336](https://github.com/Nikolai558/FE-BUDDY/issues/336), [#339](https://github.com/Nikolai558/FE-BUDDY/issues/339))".

<a id="c3-6"></a>

### C3-6 · Wrong doc · Low · "The only Telephony type AiracService and FeBuddy.Harness call directly" is not true

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Telephony/TelephonyService.cs:18-19`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/TelephonyService.cs#L18-L19)

**Says:** `The only Telephony type <c>AiracService</c> and <c>FeBuddy.Harness</c> call directly; every other type in this folder is a step of this pipeline.`

**Problem:** Both also call `TelephonySettingsParser.IncludesVatsimRadarList` (AiracService.cs:150, FeBuddy.Harness/TelephonyRunner.cs:41), which is not a pipeline step; and `VatsimRadarVirtualAirlines` is also used by the Telephony tab (ReadKeptCopy, FindSame, FindSameCallSign, ListName).

**Proposed:** "The entry point `AiracService` and `FeBuddy.Harness` run (they also ask `TelephonySettingsParser.IncludesVatsimRadarList` beforehand, to know whether to download the virtual airline list); `VatsimRadarVirtualAirlines` also serves the Telephony tab."

<a id="c3-7"></a>

### C3-7 · Wrong doc · Low · Telephony and Wx Stations say "every AIRAC Service run" downloads their data

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Telephony/TelephonyService.cs:20-22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/TelephonyService.cs#L20-L22) ; [`FeBuddy/FeBuddy.Core/Application/Airac/WxStations/WxStationService.cs:19-22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/WxStations/WxStationService.cs#L19-L22)

**Says:** `so every AIRAC Service run downloads the latest copies` / `which every AIRAC Service run downloads into one kept copy`

**Problem:** Only a run that includes the sub-service downloads it (AiracService.cs:138,148: `if (settings.WxStations is not null)` / `if (settings.Telephony is not null)`); Architecture.md:97-98 and WxStationBuilder.cs:50 say "every run that includes ...".

**Proposed:** "every AIRAC Service run that includes Telephony downloads the latest copies" / "which every AIRAC Service run that includes Wx Stations downloads into one kept copy".

<a id="c3-5"></a>

### C3-5 · Wrong doc · Low · `TelephonySettingsParser` remarks: the Telephony tab sends no ROI keys, and `IncludeFebCustomProperties = Y` warns rather than being ignored

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Telephony/TelephonySettingsParser.cs:28-31`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/TelephonySettingsParser.cs#L28-L31)

**Says:** `The shared keys every sub-service tab sends (region of interest, coordinate precision, FE-Buddy properties) are accepted and ignored`

**Problem:** `GeojsonSubServiceViewModel.AddSharedSettings` returns before the area/ROI keys when the tab offers no areas, which Telephony doesn't; it sends `CoordinatePrecision`, `GenerateAliasFile`, `IncludeFebCustomProperties`, `FebProperties`, `CrcDefaultsFor`. Settings-Reference.md:494-495 says "the app saves and sends no Area or ROI keys". And `IncludeFebCustomProperties = Y` is not silently ignored: lines 87-91 add a warning.

**Proposed:** "The shared keys (coordinate precision, `CrcDefaultsFor`, `FebProperties`, and any area or ROI keys) are accepted and ignored: Telephony writes no GeoJSON and is not limited to an area. `IncludeFebCustomProperties = Y` only warns."

<a id="c3-26"></a>

### C3-26 · Minor · Low · "VatsimRadar" identifiers name the combined GNG + VATSIM-Radar list

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Telephony/VatsimRadarVirtualAirlines.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/VatsimRadarVirtualAirlines.cs) ; [`Models/VatsimRadarSelection.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/Models/VatsimRadarSelection.cs) (`VatsimRadarSelection`, `VatsimRadarCopy`) ; [`TelephonyResults.cs:32`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/Models/TelephonyResults.cs#L32),57 (`VatsimRadarVirtualAirlineCount`) ; [`TelephonySettings.cs:27`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/Models/TelephonySettings.cs#L27) ; [`TelephonySettingsParser.cs:118`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/TelephonySettingsParser.cs#L118) (`IncludesVatsimRadarList`) ; [`TelephonyBuilder.cs:70`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/TelephonyBuilder.cs#L70)

**Says:** `public static class VatsimRadarVirtualAirlines` whose summary is "The virtual airline list - GNG's fictional airlines with VATSIM-Radar's own added"

**Problem:** The docs all say "the virtual airline list", but the names say VATSIM-Radar (and `VatsimRadarAirline` also models GNG rows), so a reader expects the VATSIM-Radar part only.

**Proposed:** Optional rename to `VirtualAirlineList`, `VirtualAirlineListSelection`, `VirtualAirlineListCopy`, `VirtualAirlineListCount`, `IncludesVirtualAirlineList`. Keep the settings key `IncludeVatsimRadarVirtualAirlines` (a rename needs a layout migration).

## FeBuddy.Core/Application/Airac/Telephony/Models

<a id="c3-8"></a>

### C3-8 · Wrong doc · Low · Telephony summaries miss the first-operator rule and the list

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Telephony/Models/TelephonySettings.cs:11-13`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/Models/TelephonySettings.cs#L11-L13) ; [`FeBuddy/FeBuddy.Core/Application/Airac/Telephony/TelephonyBuilder.cs:63-65`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/TelephonyBuilder.cs#L63-L65)

**Says:** `every operator in the FAA pages gets its commands, with the user's own virtual airlines added after them.` / `Reads every row into an entry, or counts why it was left out, then adds an entry for each virtual airline.`

**Problem:** Rows with no designator/telephony and expired special call signs get no commands, and `KeepFirstOfEach` (TelephonyBuilder.cs:167-206) drops every later entry with the same 3LD (or identifier) and telephony - FAA repeats included, which are not counted, only named in an Info message - and adds the virtual airline list's after the user's when included.

**Proposed:** TelephonySettings: "every FAA operator with a designator and a telephony gets its commands, then the user's virtual airlines and, when included, the virtual airline list's - only the first for each 3LD and telephony (see `TelephonyBuilder`)." TelephonyBuilder.Read: "Reads every row into an entry, or counts why it was left out, adds the virtual airlines, then keeps only the first entry for each 3LD (or identifier) and telephony."

## FeBuddy.Core/Application/Airac/WxStations

<a id="c3-24"></a>

### C3-24 · Minor · Low · `WxStationSettingsParser.Parse` exception doc misses the both-off throw

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/WxStations/WxStationSettingsParser.cs:46`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/WxStations/WxStationSettingsParser.cs#L46),58-63

**Says:** `<exception cref="ArgumentException">Thrown when a required setting is missing or invalid.</exception>`

**Problem:** It also throws when `EmitSymbols` and `EmitText` are both `N`; FixSettingsParser.cs:51-54 words the same case ("or the combination of output choices would produce no output at all").

**Proposed:** Use FixSettingsParser's wording.

## FeBuddy.Core/Application/AliasGuide

<a id="c4-8"></a>

### C4-8 · Wrong doc · Med · Alias Command Guide: "Charts with no command" leaves out LOC/NDB parts

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/AliasGuide/AliasGuideContent.cs:329-340`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/AliasGuide/AliasGuideContent.cs#L329-L340) (item at 336)

**Says:** `Item("GLS approaches (the other approaches on the same chart still have a command)"),`

**Problem:** `ApproachCodes.NoCommandTypes` skips `LOC/NDB` parts as well as GLS (ApproachCodes.cs:64-68; `ChartRecallSkipReason.LocNdb`). User-Guide.md:271 lists "GLS and LOC/NDB parts", but the controller-facing guide never mentions LOC/NDB. A controller will look for a command for `ILS OR LOC/NDB RWY 10`'s LOC/NDB part that doesn't exist.

**Proposed:** `Item("GLS and LOC/NDB approaches (the other approaches on the same chart still have a command)"),` Check AliasGuideWriterTests for a pinned copy of the list.

<a id="c4-9"></a>

### C4-9 · Wrong doc · Low · The guide's shared "Procedure Names" text describes Chart Recall's naming rule, but Data Display uses a different one

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/AliasGuide/AliasGuideContent.cs:185-198`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/AliasGuide/AliasGuideContent.cs#L185-L198) (used at 182 for `f` and 312 for `c`)

**Says:** `A chart with no computer code is spelled out in full instead, without its version number, bracketed words, or the words RNAV, OBSTACLE and COPTER; spaces and punctuation are also removed.`

**Problem:** (a) Under Data Display (`f`), `Departures.txt` and `Arrivals.txt` name a procedure with `DepartureNaming.CodeIdFor` and `ArrivalNaming`. The fallback there is NASR's `DP_NAME`/arrival name with only non-alphanumerics removed (`Clean`, DepartureNaming.cs:89-90), so `HUDSN (COPTER)` would be `HUDSNCOPTER`, not `HUDSN`. They also fall back to the name when the code's digit doesn't match `AMENDMENT_NO` (DepartureNaming.cs:110-113). Chart Recall instead drops a trailing digit (SidStarCodes.cs:159-162), so the two sections can differ for the same procedure. (b) Under Chart Recall (`c`), the text leaves out two rules: a runway in the name is kept (`TIN CITY FIVE RWY 17` → `TINCITY17`), and a chart named after its own airport uses the airport ID (`.tljTLJc`) (SidStarCodes.cs:24-28, ChartRecallAliasBuilder.cs:31-34).

**Proposed:** give `ProcedureNames` a per-section third bullet. For `f`: "A procedure with no usable computer code is its NASR name with spaces and punctuation removed, e.g. `.{a:anc}{i:TURNAGAIN}f`." For `c`: keep the current bullet and add "A runway in the chart's name stays, and a chart named after its own airport uses the airport's ID." Add a real example only if one can be found in a cycle's files, since the tests check every example.

<a id="c4-25"></a>

### C4-25 · Minor · Low · AliasPractice.js: diagnose's doc calls `answer` a command; it is an index

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/AliasGuide/AliasPractice.js:307-311`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/AliasGuide/AliasPractice.js#L307-L311)

**Says:** `Checks a typed command against a question: { correct, answer, hints }, where answer is the accepted command it matched or is closest to`

**Problem:** `answer` is an index into `q.answers` (lines 349, 360), and it is 0 when nothing is close (line 361). `check()` uses it as `q.answers[result.answer]`.

**Proposed:** `... where answer is the index of the accepted command it matched or is closest to (the first when nothing is close) ...`

## FeBuddy.Core/Application/AliasGuide/Models

<a id="c4-3"></a>

### C4-3 · Stale doc · Low · PracticeSubject.Help says the help question uses the default name (#370)

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/AliasGuide/Models/PracticeQuestion.cs:49-50`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/AliasGuide/Models/PracticeQuestion.cs#L49-L50)

**Says:** `/// <summary>The chart recall help command, by its default name.</summary>  Help,`

**Problem:** Since [#370](https://github.com/Nikolai558/FE-BUDDY/issues/370), `AliasPracticeContent.HelpQuestion(name)` (lines 298-311) asks for `.{name ?? DefaultName}`, which is the user's saved name (`ProceduresViewModel.SavedHelpCommand`). It falls back to the default only when the command or the alias file is off.

**Proposed:** `/// <summary>The chart recall help command: by the user's saved name for it, or by its default name when that is off or not known.</summary>`

## FeBuddy.Core/Application/Conversions

<a id="c5-9"></a>

### C5-9 · Wrong doc · Low · ReadCrcInclude remark claims callers AND it with "is that file being produced"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Conversions/ConversionSettingsReader.cs:83-87`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/ConversionSettingsReader.cs#L83-L87)

**Says:** "The caller still ANDs this with "is that file being produced": asking for defaults on a file that is never written means nothing, and its values are then not required."

**Problem:** No caller does. DAT (DatToGeojsonSettingsParser.cs:55-58) and SCT (SctToGeojsonSettingsParser.cs:46-47, 61-63) require the values whenever the box is ticked, even when the sector file has no labels. ERAM ANDs only with "the defaults source uses the card" (EramToGeojsonSettingsParser.cs:71, 85-93).

**Proposed:** "Each kind is chosen on its own (the Include box on its CRC ERAM Defaults panel). A ticked box makes that kind's values required; ERAM to GeoJSON reads them only when its defaults source uses the card."

## FeBuddy.Core/Application/Conversions/DatToGeojson

<a id="c5-17"></a>

### C5-17 · Wrong doc · Low · DAT model and writer docs miss cases

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Conversions/DatToGeojson/DatGeojsonWriter.cs:15-18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/DatToGeojson/DatGeojsonWriter.cs#L15-L18); [`FeBuddy/FeBuddy.Core/Application/Conversions/DatToGeojson/Models/DatFileConversion.cs:9-13`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/DatToGeojson/Models/DatFileConversion.cs#L9-L13),18; [`FeBuddy/FeBuddy.Core/Infrastructure/Dat/Models/DatFile.cs:11-14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Dat/Models/DatFile.cs#L11-L14)

**Says:** "the lines themselves are kept exactly as the &lt;c>.dat&lt;/c> file drew them."   "or &lt;see langword="null"/> when nothing was: the file could not be read, or cropping left nothing to draw."   "or &lt;see langword="null"/> when the file has none."

**Problem:** (a) The writer gets lines that are already cropped (RadiusFilter) and split at ±180 (AntimeridianSplitter), per its own `<param name="lines">` and DatToGeojsonService.cs:134,147. (b) OutputPath is also null when the file has no lines at all (DatToGeojsonService.cs:111-120). LinesWritten also counts antimeridian pieces. (c) PointOfTangency is also null when the 9900 record could not be read (DatFileReader.cs:85-92).

**Proposed:** (a) "…and the lines are written as given, point for point, with no joining or simplifying." (b) "…the file could not be read, had no lines, or cropping left nothing to draw." and "How many lines were written, after cropping and the antimeridian split (a line cut into pieces counts once per piece)." (c) "…or &lt;see langword="null"/> when the file has none or it could not be read (see Problems)."

<a id="c5-10"></a>

### C5-10 · Wrong doc · Low · "The keys only this conversion reads, on top of … ConversionKeys" includes ConversionKeys

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Conversions/DatToGeojson/DatToGeojsonSettingsParser.cs:22-27`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/DatToGeojson/DatToGeojsonSettingsParser.cs#L22-L27); [`FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramToGeojsonSettingsParser.cs:25-30`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramToGeojsonSettingsParser.cs#L25-L30)

**Says:** "The keys only this conversion reads, on top of &lt;see cref="SubServiceSettingsReader.CommonKeys"/> and &lt;see cref="ConversionSettingsReader.ConversionKeys"/>."   `ConversionSettingsReader.ConversionKeys.Append("CroppingDistance")`

**Problem:** The set is ConversionKeys plus the conversion's own keys, which is what `UnknownKeyWarnings`' `ownKeys` needs. It is not the conversion-only keys.

**Proposed:** "Every key this conversion reads beyond &lt;see cref="SubServiceSettingsReader.CommonKeys"/>: &lt;see cref="ConversionSettingsReader.ConversionKeys"/> plus &lt;c>CroppingDistance&lt;/c>" (ERAM: "plus &lt;c>OutputLayout&lt;/c> and &lt;c>DefaultsSource&lt;/c>").

<a id="c5-24"></a>

### C5-24 · Minor · Low · DAT's labelSource text can never be shown

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Conversions/DatToGeojson/DatToGeojsonSettingsParser.cs:60-62`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/DatToGeojson/DatToGeojsonSettingsParser.cs#L60-L62)

**Says:** `labelSource: "a video map has no labels"`

**Problem:** VideoMap draws only Line (line 33), so any `Crc.VideoMap.Text.*` key stops at "has no Text output" (SubServiceSettingsReader.cs:308-311) before the labelSource branch (318-320).

**Proposed:** Pass any short text, or make `labelSource` optional (`string? labelSource = null`) and omit it here.

## FeBuddy.Core/Application/Conversions/EramToGeojson

<a id="c5-19"></a>

### C5-19 · Wrong doc · Low · Rundown "Lines end in CRLF" holds only where Environment.NewLine is CRLF

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramConsoleCommandControlRundown.cs:29`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramConsoleCommandControlRundown.cs#L29),60-104

**Says:** "Lines end in CRLF; the original mixed LF and CRLF." with every line written by `text.AppendLine(...)`.

**Problem:** `StringBuilder.AppendLine` writes `Environment.NewLine`, so the CRLF promise holds only on Windows. The tests assert `\r\n` (EramConsoleCommandControlRundownTests.cs:85,93) and pass only because CI tests run on windows-2025.

**Proposed:** Either write `"\r\n"` explicitly (a small `Line(text, value)` helper doing `text.Append(value).Append("\r\n")`), or reword to "Lines end in Windows line endings (CRLF)". In both cases drop "the original mixed LF and CRLF" (see [C5-20](#c5-20)).

<a id="c5-20"></a>

### C5-20 · History wording · Low · ERAM comments narrate the old ERAM_2_GEOJSON tool's behaviour where it explains nothing current

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramConsoleCommandControlRundown.cs:28-29`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramConsoleCommandControlRundown.cs#L28-L29); [`FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramGeojsonWriter.cs:58`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramGeojsonWriter.cs#L58),591; [`FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/Models/EramFebProperty.cs:5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/Models/EramFebProperty.cs#L5); [`FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramToGeojsonService.cs:29-31`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramToGeojsonService.cs#L29-L31)

**Says:** "Lines end in CRLF; the original mixed LF and CRLF."   "The conversion's folder inside the output directory: the original tool's &lt;c>E2G_OUTPUT&lt;/c>."   "(the original tool's &lt;c>E2G_*&lt;/c> properties)"

**Problem:** These narrate what the old tool did or was called and explain nothing about today's code. Keep the references that say the layouts and names are the original tool's: that is a deliberate, user-facing compatibility choice (News.md:261).

**Proposed:** Rundown: "Lines end in CRLF." (see [C5-19](#c5-19)). RootFolder: "The conversion's folder inside the output directory." EramFebProperty: drop the parenthesis. EramToGeojsonService.cs:29-31: "Once the file has been read, everything already in &lt;c>ERAM_TO_GEOJSON&lt;/c> is deleted, so it holds only this run's files - the attribute layout's names change with the data, and old files would otherwise pile up beside new ones." EramGeojsonWriter.cs:591: "A name with the characters Windows forbids in a file name taken out."

<a id="c5-28"></a>

### C5-28 · Minor · Low · Stray blank lines in EramToGeojsonSettingsParser

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramToGeojsonSettingsParser.cs:31-32`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramToGeojsonSettingsParser.cs#L31-L32),100

**Says:** two blank lines after `OwnKeys`, and a blank line before the closing brace

**Problem:** Inconsistent with every other file.

**Proposed:** Remove the extra blank lines.

## FeBuddy.Core/Application/Conversions/EramToGeojson/Models

<a id="c5-7"></a>

### C5-7 · Wrong doc · Low · EramDefaultsSource docs don't match what XmlThenCard and Card do

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/Models/EramDefaultsSource.cs:12-19`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/Models/EramDefaultsSource.cs#L12-L19); [`FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramGeojsonWriter.cs:27-29`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramGeojsonWriter.cs#L27-L29)

**Says:** "As &lt;see cref="Xml"/>, but an object with no usable defaults of a kind takes the CRC defaults set on the tab."   "Ignore the XML's display properties and use the CRC defaults set on the tab for everything."

**Problem:** XmlThenCard fills property by property. `Over(AsEram(fromCard), Usable(kind, fromXml))` (EramGeojsonWriter.cs:277) takes each property the XML defaults leave out or give with a value CRC can't draw from the card, including filters. An object with partial defaults is filled in; whole objects are not swapped. The GUI says so ("From the XML, filling gaps from the card", EramToGeojsonView.xaml:168). Card still honours the XML's `DisplaySetting` (EramGeojsonWriter.cs:140 hides those labels in every mode). The class remark "with its own values laid over them" is not true in Card mode (`UseOverrides: settings.DefaultsSource != EramDefaultsSource.Card`, line 297).

**Proposed:** XmlThenCard: "As &lt;see cref="Xml"/>, but each property an object's defaults of a kind leave out, or give with a value CRC can't draw, is taken from the CRC defaults set on the tab." Card: "Use the CRC defaults set on the tab for everything, ignoring the XML's defaults and each element's own values; text ERAM keeps hidden (&lt;c>DisplaySetting&lt;/c> false) is still left out." Writer remark: "…with its own values laid over them (except with &lt;see cref="EramDefaultsSource.Card"/>)…".

## FeBuddy.Core/Application/Launch

<a id="c1-21"></a>

### C1-21 · Wrong doc · Low · LaunchSequence says all three network steps use the internet flag; the AIRAC pipeline doesn't

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Launch/LaunchSequence.cs:30-31`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Launch/LaunchSequence.cs#L30-L31)

**Says:** `then the UTC time / internet check (AIRAC needs the time, and all three network steps use the internet flag)`

**Problem:** Only the version check and News get `time.HasInternetConnection` (lines 190, 200). `PrepareAiracAsync` takes no flag and always probes/downloads (Infrastructure can't see AppEnvironment).

**Proposed:** "... (AIRAC needs the time; the version check and News use the internet flag) ..."

## FeBuddy.Core/Application/News

<a id="c1-24"></a>

### C1-24 · Wrong doc · Low · NewsService.Parse: PostId isn't optional, and posts aren't returned in document order

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/News/NewsService.cs:106-113`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/News/NewsService.cs#L106-L113), 155

**Says:** `A post is a <c>## </c> heading, an optional <c>&lt;!-- PostId: … --&gt;</c> comment ...` / `<returns>The parsed posts in document order (newest first, matching the file convention).</returns>`

**Problem:** A section without a parseable PostId is skipped (lines 134-141), so it is required. The result is sorted by PostId (`posts.OrderByDescending(p => p.Id)`), not kept in document order.

**Proposed:** "A post is a `## ` heading, a `<!-- PostId: … -->` comment, and the text up to the next `---` rule. ..." and `<returns>The parsed posts, newest PostId first.</returns>`

## FeBuddy.Core/Application/Settings

<a id="c1-30"></a>

### C1-30 · Wrong doc · Low · ReadArea says the Harness sends blocks without Area, and keeps a "before there was a choice" note

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Settings/SubServiceSettingsReader.cs:47-53`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Settings/SubServiceSettingsReader.cs#L47-L53)

**Says:** `A block without it (the harness, or one written before there was a choice) gets the area its filters meant`

**Problem:** The Harness sets `Area` in every block (HarnessSettings.cs:68, 142, 194, ...), the GUI always writes it (GeojsonSubServiceViewModel.cs:729) and saved settings get it from migration 2 (UserConfigMigrations.cs:32). Only tests and hand-built blocks omit it - the fallback is still needed for the many tests that build blocks with just `FilterByRoi`, so keep the code.

**Proposed:** "A block without it (a test's, or one built by hand) gets the area its filters meant: ..."

<a id="c1-31"></a>

### C1-31 · Wrong doc · Low · SubServiceSettingsReader is shared with the file conversions too

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Settings/SubServiceSettingsReader.cs:14-18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Settings/SubServiceSettingsReader.cs#L14-L18)

**Says:** `/// Reads the settings every AIRAC sub-service shares - coordinate precision, ...`

**Problem:** The three conversion parsers also use `ReadCoordinatePrecision`, `ReadFebProperties`, `CommonKeys` and `UnknownKeyWarnings` (DatToGeojson/SctToGeojson/EramToGeojson *SettingsParser.cs, ConversionSettingsReader.cs).

**Proposed:** "Reads the settings every AIRAC sub-service shares (and the file conversions reuse some of) - ..."

<a id="c1-37"></a>

### C1-37 · Minor · Low · Stray blank line in ReadFebProperties

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Settings/SubServiceSettingsReader.cs:205-207`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Settings/SubServiceSettingsReader.cs#L205-L207)

**Says:** `foreach (string name in names)` / `{` / (blank) / `if (!FebProperties.TryParse(...`

**Problem:** Formatting slip.

**Proposed:** Remove the blank line 207.

## FeBuddy.Core/Application/Updates

<a id="c1-23"></a>

### C1-23 · Wrong doc · Low · UpdateChannelSetting says the channel is saved in UserConfig.json; it is in Shared.json

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Updates/UpdateChannelSetting.cs:12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Updates/UpdateChannelSetting.cs#L12)

**Says:** `/// The update channel FE-Buddy checks: the one saved in <c>UserConfig.json</c> (<c>General.UpdateChannel</c>).`

**Problem:** `General.UpdateChannel` is a PC-local key shared by every profile, stored in `Shared.json` (UserConfigPortability.cs:40-49, 74; UserConfigFile.cs:23-24). There is no `UserConfig.json` any more (profiles are `UserConfig.<Profile>.json`).

**Proposed:** "... the one saved in &lt;c>Shared.json&lt;/c> (&lt;c>General.UpdateChannel&lt;/c>), shared by every settings profile."

<a id="c1-26"></a>

### C1-26 · History wording · Low · UpdateInstaller remarks narrate 2.x's updater

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Updates/UpdateInstaller.cs:17-18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Updates/UpdateInstaller.cs#L17-L18)

**Says:** `/// The same design as FE-Buddy 2.x's updater: the download goes to the temporary workspace`

**Problem:** Explains the code by reference to old code (rule 3).

**Proposed:** `/// The download goes to the temporary workspace (<c>%TEMP%\FE-Buddy\Updates</c>, cleared on every launch). ...`

**Note:** Re-checked after [#377](https://github.com/Nikolai558/FE-BUDDY/pull/377): still applies (the lines moved down 8).

## FeBuddy.Core/Application/Updates/Models

<a id="c1-27"></a>

### C1-27 · Wrong doc · Low · ReleaseSummary.IsPrerelease says "alpha or beta"; it is any pre-release, rc included

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Updates/Models/VersionCheckResult.cs:132`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Updates/Models/VersionCheckResult.cs#L132)

**Says:** `/// <param name="IsPrerelease"><see langword="true"/> for an alpha or beta release.</param>`

**Problem:** Set from `ProductVersion.IsPrerelease` (VersionCheck.cs:117), true for `-rc.N` too (and any other label); the update window shows "PRE-RELEASE" for release candidates as well.

**Proposed:** "`true` for a pre-release (alpha, beta or release candidate)."

<a id="c1-35"></a>

### C1-35 · Minor · Low · DownloadProgress lives in VersionCheckResult.cs

**Status:** Fixed (not yet merged). `DownloadProgress` is in `Updates/Models/DownloadProgress.cs` · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Updates/Models/VersionCheckResult.cs:155-162`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Updates/Models/VersionCheckResult.cs#L155-L162)

**Says:** `public readonly record struct DownloadProgress(long BytesReceived, long? TotalBytes)`

**Problem:** It belongs to `UpdateInstaller.DownloadAsync`, not to the version check result ("one main type per file").

**Proposed:** Move to `Updates/Models/DownloadProgress.cs`.

## FeBuddy.Core/Domain/Airac

<a id="c5-3"></a>

### C5-3 · Wrong doc · Med · AiracCycleResolver: "about one year in nine has 14" cycles is wrong

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Airac/AiracCycleResolver.cs:17-18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Airac/AiracCycleResolver.cs#L17-L18)

**Says:** "Most years have 13 cycles; about one year in nine has 14 (e.g. 2014, effective 2020-12-31)."

**Problem:** With a 28-day cadence, the first cycle's day of year falls by 1 each year (2 after a leap year). A 14th cycle fits only when the first cycle falls on 1 January (1-2 January in a leap year). That happens about once every 22-23 years. Computed from the class's own reference date (2601 = 2026-01-22), the years with 14 cycles are 1976, 1998, 2020, 2043, 2065. The example (2014 = 2020-12-31) is right.

**Proposed:** "Most years have 13 cycles; about one year in 22 or 23 has 14 (2020's 14th, 2014, took effect 2020-12-31; the next such year is 2043)."

## FeBuddy.Core/Domain/Arrivals

<a id="c5-25"></a>

### C5-25 · Minor · Low · Copied code the house rules say to share

**Status:** Deferred: share the code after 3.0; S11 adds the TODO.md item ([D7](010_Decisions.md#d7)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Arrivals/ArrivalNaming.cs:36-118`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Arrivals/ArrivalNaming.cs#L36-L118); [`FeBuddy/FeBuddy.Core/Domain/Departures/DepartureNaming.cs:35-117`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Departures/DepartureNaming.cs#L35-L117); [`FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramGeojsonWriter.cs:594-605`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramGeojsonWriter.cs#L594-L605) (vs [`ConversionFiles.cs:94-112`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/ConversionFiles.cs#L94-L112))

**Says:** Both naming classes hold the same `AmendmentNumbers` table, `NonAlphanumeric` regex, `Clean`, `NotAssigned` and `FromComputerCode`. The only difference is `code[..dot]` vs `code[(dot + 1)..]`. `EramGeojsonWriter.Unique` repeats `ConversionFiles.UniqueFileName`'s loop.

**Problem:** Code-Structure: "Something two features share: the lowest layer both can see. Never copy it." NasrProcedureRoutes.cs:22 has a third `NotAssigned`.

**Proposed:** Move the shared parts to `Domain/Procedures/ProcedureNaming` (or a new `ProcedureCodes`) as `CodeIdFor(computerCode, amendmentNo, name, bool procedureAfterDot)`, and keep ArrivalNaming/DepartureNaming as one-line wrappers or delete them. Have EramGeojsonWriter call a shared `ConversionFiles.Unique(name, used)` without the safe-name step.

**Note:** See also [C2-29](#c2-29). Part of [D7](010_Decisions.md#d7) (after 3.0).

## FeBuddy.Core/Domain/Crc

<a id="c5-12"></a>

### C5-12 · Wrong doc · Low · xOffset/yOffset "any integer (CRC spec)" contradicts the linked spec

**Status:** Agreed: comment only; CRC_Geojsons.md is outside this repository ([D5](010_Decisions.md#d5)) · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Crc/CrcPropertyValidator.cs:194`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Crc/CrcPropertyValidator.cs#L194); [`FeBuddy/FeBuddy.Core/Domain/Crc/Models/CrcDefaults.cs:95-99`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Crc/Models/CrcDefaults.cs#L95-L99); [`FeBuddy/FeBuddy.Core/Domain/Crc/Models/CrcTextProperties.cs:40-44`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Crc/Models/CrcTextProperties.cs#L40-L44)

**Says:** `// xOffset / yOffset accept any integer (CRC spec), so there is nothing to range-check.`

**Problem:** The spec linked in the same class (KCSanders7070/CRC_GeoJson_Concepts, CRC_Geojsons.md) lists `xOffset`/`yOffset` as "Positive integer" (auto-assigned 0). FE-Buddy writes negative offsets from ERAM (`XPixelOffset = -2` in EramGeoMapReaderTests.cs:320) and accepts them in the card. Either the comment or the spec page is wrong. Every other range (bcg 1-40, filters 0-40, thickness 1-3, symbol size 1-4, text size 0-5) and both style lists match the spec exactly.

**Proposed:** Confirm in CRC. If negatives work, keep the code, fix CRC_Geojsons.md, and change the comment to "xOffset / yOffset accept any integer, negative included, so there is nothing to range-check." If they don't, range-check `>= 0`.

**Note:** **Kyle:** tested in CRC: negative offsets work, so the code is right. Reword the comment ("any integer, negative included") ([D5](010_Decisions.md#d5)).

## FeBuddy.Core/Domain/Crc/Models

<a id="c5-14"></a>

### C5-14 · Wrong doc · Low · CrcSymbolDefaults says "Every one is required" but Style is nullable

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Crc/Models/CrcDefaults.cs:12-13`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Crc/Models/CrcDefaults.cs#L12-L13),40,49-54

**Says:** `/// <summary>The properties of an <c>isSymbolDefaults</c> Feature. Every one is required; see <see cref="CrcLineDefaults"/>.</summary>`   `public required string? Style { get; init; }`

**Problem:** Style may be null (NAVAIDs styled per type), as its own doc says.

**Proposed:** "Every one is required except &lt;see cref="Style"/>, which may be left to each Feature; see &lt;see cref="CrcLineDefaults"/>."

<a id="c5-13"></a>

### C5-13 · Wrong doc · Low · Crc*Properties: "no filters assigned (via this or a per-feature override)": these records are the per-feature override

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Crc/Models/CrcLineProperties.cs:20-24`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Crc/Models/CrcLineProperties.cs#L20-L24); [`FeBuddy/FeBuddy.Core/Domain/Crc/Models/CrcSymbolProperties.cs:19-23`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Crc/Models/CrcSymbolProperties.cs#L19-L23); [`FeBuddy/FeBuddy.Core/Domain/Crc/Models/CrcTextProperties.cs:21-25`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Crc/Models/CrcTextProperties.cs#L21-L25),28-31

**Says:** "a line Feature with no filters assigned (via this or a per-feature override) will not display on an ERAM window."

**Problem:** CrcLineDefaults' remark says "Per-feature overrides use &lt;see cref="CrcLineProperties"/> instead". The other source of filters is the file's isDefaults Feature, not "a per-feature override".

**Proposed:** "(via this or the file's &lt;c>isLineDefaults&lt;/c> Feature)". Use `isSymbolDefaults` / `isTextDefaults` in the other two, for both filters and text.

## FeBuddy.Core/Domain/Fixes/Models

<a id="c5-27"></a>

### C5-27 · Minor · Low · Time-relative data claims that will go stale

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Fixes/Models/Fix.cs:7-9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Fixes/Models/Fix.cs#L7-L9); [`FeBuddy/FeBuddy.Core/Domain/Navaids/Models/Navaid.cs:7-9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Navaids/Models/Navaid.cs#L7-L9)

**Says:** "Duplicate &lt;see cref="FixId"/> values are allowed in real NASR data, even though the current cycle has none"; "about 71 identifiers repeat"

**Problem:** "The current cycle" and the count change each cycle. Unlike ArrivalProcedure/DepartureProcedure ("2026-10 cycle"), these name no cycle.

**Proposed:** Fix: "Duplicate &lt;see cref="FixId"/> values are allowed - every row NASR publishes becomes its own &lt;see cref="Fix"/>, never merged or de-duplicated." Navaid: "…are normal in real NASR data (dozens of identifiers repeat in the 2026-10 cycle, e.g. …)" or drop the count.

## FeBuddy.Core/Domain/Geo

<a id="c5-16"></a>

### C5-16 · Wrong doc · Low · LineStringMerger "longest first" means "most points", not distance

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Geo/LineStringMerger.cs:17-19`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Geo/LineStringMerger.cs#L17-L19),44

**Says:** "Paths are taken longest first (ties keep their original order), so the longest run becomes one unbroken base LineString."   `.OrderByDescending(entry => entry.Path.Count)`

**Problem:** Ordering is by point count after collapsing repeats. A 2-point 300 NM path sorts after a 5-point 10 NM path.

**Proposed:** "Paths are taken in order of most points first (ties keep their original order), so the path with the most points becomes one unbroken base LineString."

<a id="c5-15"></a>

### C5-15 · Wrong doc · Low · RadiusFilter inline comment states the wrong radius limit

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Geo/RadiusFilter.cs:130-131`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Geo/RadiusFilter.cs#L130-L131) (vs 18-21)

**Says:** `// The inside stretch is phase ± halfWidth, give or take a whole turn; with the radius and` / `// the segment both under half a turn, at most one of those lands on this segment.`

**Problem:** Only a radius under a quarter turn guarantees this. Then minimumDot > 0, so halfWidth &lt; π/2 and the inside stretch is under half a turn. A radius between a quarter and half a turn allows halfWidth up to π, and two shifted stretches can both meet a segment. The class remark correctly says "well under a quarter of the Earth's circumference".

**Proposed:** "…with the radius under a quarter turn (so the inside stretch is under half a turn) and the segment at most half a turn, at most one of those lands on this segment."

## FeBuddy.Core/Domain/Procedures

<a id="c4-27"></a>

### C4-27 · Minor · Low · ProcedureChartTypes.Default says it is "every kind of procedure a pilot flies"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Procedures/ProcedureChartTypes.cs:39-44`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Procedures/ProcedureChartTypes.cs#L39-L44)

**Says:** `every kind of procedure a pilot flies, but none of the volume-wide reference sheets`

**Problem:** The default also includes `APD` (the airport diagram) and `DAU` (an AAUP page), which aren't procedures that are flown.

**Proposed:** `every approach, departure and arrival, with the RNAV departure AAUPs and the airport diagram, but none of the volume-wide reference sheets (...)`.

<a id="c4-15"></a>

### C4-15 · Stale doc · Low · ProcedureNaming's remarks: "both members", and "rather than internal, like the rest of this sub-service"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Procedures/ProcedureNaming.cs:14-17`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Procedures/ProcedureNaming.cs#L14-L17)

**Says:** `Public (rather than <see langword="internal"/>, like the rest of this sub-service): the Procedures GUI tab uses both members directly to group the d-TPP Metafile's raw records into the same per-airport procedure lists <c>ProcedureBuilder</c> would produce`

**Problem:** The class has four public members. The tab uses `BaseName`, `RootName` and `IsContinuation` (ProceduresViewModel.cs:581-617, 847, 1146, 1515-1807); `PageNumber` is used only in Core. Most of the sub-service is public (`ProcedureBuilder`, `ProcedureSelection`, `ProcedureChartTypes`, `ChartRecallCodes`...). The tab lists departure and arrival names by root; it doesn't rebuild ProcedureBuilder's lists.

**Proposed:** `Public because the Procedure Charts tab uses BaseName, RootName and IsContinuation to list the metafile's departures and arrivals by name, without building ProcedureAirports.`

<a id="n1-35"></a>

### N1-35 · Wrong doc · Low · ProcedureChartTypes: `Default` is for an absent `ChartTypes`, not a blank one

**Status:** Fixed (not yet merged). The summary now says `Default` is for an absent `ChartTypes` and a blank value means none · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Procedures/ProcedureChartTypes.cs:39-43`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Domain/Procedures/ProcedureChartTypes.cs#L39-L43); [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureSettingsParser.cs:244-261`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureSettingsParser.cs#L244-L261)

**Says:** "absent or blank"

**Problem:** Since the blank-means-none fix (97aab48), the parser returns `Default` only when the key is absent; blank gives no chart types. `Settings-Reference.md` already says "blank for none". The test `ProcedureSettingsParserTests.blank_chart_types_mean_none` covers the code.

**Proposed:** "…when `ChartTypes` is absent; a blank value means none."

## FeBuddy.Core/Domain/Procedures/ChartRecall

<a id="c4-5"></a>

### C4-5 · Wrong doc · Low · ApproachCodes says the runway is written "exactly as the FAA prints it"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Procedures/ChartRecall/ApproachCodes.cs:13-14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Procedures/ChartRecall/ApproachCodes.cs#L13-L14)

**Says:** `A code is the approach type's code, then <c>BC</c> for a back course, then the variant letter, then the runway exactly as the FAA prints it:`

**Problem:** `ExpandRunways` drops a leading zero (line 268, `TrimStart('0')`) and splits `26L/R` into two runways. The table directly below shows `RNAV (GPS) RWY 06` → `R6` and `VOR RWY 26L/R` → `O26L`, `O26R`, which contradicts the sentence.

**Proposed:** `... then the variant letter, then the runway without a leading zero, one code per runway:`

<a id="c4-6"></a>

### C4-6 · Wrong doc · Low · The AAUP skip rule is described as "ending in AAUP", but the code matches the word anywhere

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Procedures/ChartRecall/ApproachCodes.cs:39`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Procedures/ChartRecall/ApproachCodes.cs#L39); [`FeBuddy/FeBuddy.Core/Domain/Procedures/ChartRecall/Models/ChartRecallSkipReason.cs:43`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Procedures/ChartRecall/Models/ChartRecallSkipReason.cs#L43)

**Says:** `... <c>CONVERGING</c>, ending in <c>AAUP</c>, or a numbered approach ...` / `An attention-all-users page: an approach name ending in <c>AAUP</c>`

**Problem:** `AaupWord()` is `\bAAUP\b` with `IsMatch` (ApproachCodes.cs:126, 310-311), so the word can be anywhere in the name.

**Proposed:** replace "ending in AAUP" with "holding the word AAUP" in both places.

<a id="c4-14"></a>

### C4-14 · History wording · Low · FE-Buddy 2.x references in the chart recall code

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Procedures/ChartRecall/SidStarCodes.cs:13-14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Procedures/ChartRecall/SidStarCodes.cs#L13-L14); [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ChartRecallAliasBuilder.cs:23-24`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ChartRecallAliasBuilder.cs#L23-L24); [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/Models/ChartRecallResults.cs:17`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/Models/ChartRecallResults.cs#L17)

**Says:** `the way FE-Buddy 2.x made it:` / `The command syntax is FE-Buddy 2.x's, which controllers already know:` / `two spaces before the <c>;</c>, as FE-Buddy 2.x wrote it.`

**Problem:** These explain the code by reference to an earlier version, not because of a bug (rule 3).

**Proposed:** SidStarCodes: `The code comes from the chart's computer code whenever it has one: a departure's first half ...`. ChartRecallAliasBuilder: `The command syntax: a period, the airport's FAA identifier in lower case, ...`. ChartRecallAliasLine.Text: `The whole line, without a line ending, with two spaces before the <c>;</c>.`

<a id="c4-7"></a>

### C4-7 · Wrong doc · Low · SidStarCodes: the trailing-digit fallback also runs when the code doesn't end in the name's version

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Procedures/ChartRecall/SidStarCodes.cs:18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Procedures/ChartRecall/SidStarCodes.cs#L18)

**Says:** `When the name has no version, a single trailing digit is dropped.`

**Problem:** `FromComputerCode` (lines 155-162) also takes the `else if` branch when the name has a version that the code's half doesn't end in (for example, name `DEVLN TWO` with code `DEVLN1.DEVLN` gives `DEVLN`).

**Proposed:** `When the name has no version, or the code doesn't end in it, a single trailing digit is dropped.`

## FeBuddy.Core/Domain/Procedures/Models

<a id="c4-4"></a>

### C4-4 · Wrong doc · Low · ProcedureAirspaceClass says the highest class is picked with Enumerable.Max; no code does that

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Procedures/Models/ProcedureAirspaceClass.cs:9-10`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Procedures/Models/ProcedureAirspaceClass.cs#L9-L10)

**Says:** `Declaration order also doubles as rank, lowest first, so picking the highest flagged class is a plain <c>Enumerable.Max</c>.`

**Problem:** `ProcedureBuilder.ComputeAirspaceClass` (ProcedureBuilder.cs:308-328) uses four flags and an if-chain. `ProcedureOrdering.AirspaceRank` (ProcedureOrdering.cs:58-64) has its own switch. No code depends on declaration order.

**Proposed:** delete that sentence, or reword it to "Declared lowest first (None, E, D, C, B)." Keep the `ToString` sentence, which is accurate (ProceduresJsonWriter.cs:117).

## FeBuddy.Core/Domain/WxStations/Models

<a id="c5-26"></a>

### C5-26 · Minor · Low · Domain layer smells (no file, network or settings access found)

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/WxStations/Models/WxStation.cs:6`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/WxStations/Models/WxStation.cs#L6); [`FeBuddy/FeBuddy.Core/Domain/Fixes/FixUses.cs:42-45`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Fixes/FixUses.cs#L42-L45),56-65; [`FeBuddy/FeBuddy.Core/Domain/Airac/AiracCycleResolver.cs:40`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Airac/AiracCycleResolver.cs#L40)

**Says:** `<see cref="Application.Airac.WxStations.WxStationBuilder.Read"/>`; `char[] invalidChars = Path.GetInvalidFileNameChars();`; `asOfUtc ?? DateOnly.FromDateTime(DateTime.UtcNow)`

**Problem:** A Domain doc points up into Application. `FixUses.Name` applies a platform file-system rule in Domain. The rule is redundant for files, because `FixTokens.Token` already strips everything but ASCII letters and digits before a name reaches a file or class name (FixGeojsonWriter.cs:91). The clock read is covered in [C5-21](140_Unused-Code.md#c5-21).

**Proposed:** WxStation: "…built from a &lt;c>stations.cache.xml&lt;/c> &lt;c>&lt;Station&gt;&lt;/c> row that passed the Wx Stations selection rules (a US or US-territory station with an ICAO ID…)". FixUses.Name: replace characters that are not ASCII letters, digits, `-` or `/` with a space instead of `Path.GetInvalidFileNameChars()`, and update the `<returns>` text.

## FeBuddy.Core/Infrastructure/Configuration

<a id="c6-6"></a>

### C6-6 · Wrong doc · Med · UserConfigMigrations says moving the settings file "is also a new layout"; the profiles move wasn't

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigrations.cs:18-22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigrations.cs#L18-L22)

**Says:** "A change that moves the settings file itself (a new folder, or one file split into two) is also a new layout: &lt;see cref="UserConfigFile"/> then looks for a file in an older layout where that layout kept it, and the step moves the settings between the parts of the new one."

**Problem:** The only such change was beta.4's move into `User Configurations\` and split into profile + `Shared.json`, and it kept layout 1 (`Current = 1` in beta.4 and beta.5). It was done by `MoveLegacyFileIn`/`AdoptSharedSettings`, not by a step. A step can't move settings between files anyway: `Migrate` sees one flat dictionary, and `Shared.json` is never brought forward ([C6-29](100_Bugs.md#c6-29)). Settings-Reference.md:84-85 ("A profile is not a layout change ... no migration step was needed") and 633-635 ("Moving the settings file ... needs only step 3, as the profiles did") say the opposite.

**Proposed:** Replace the paragraph with: "Moving the settings file while every setting keeps its dotted path is not a new layout: &lt;see cref="UserConfigFile"/> looks for the file where the older version kept it too, as it does for 3.0.0-beta.3's &lt;c>UserConfig.json&lt;/c>." If [C6-2](130_Old-Version-Code.md#c6-2) is taken, drop the "as it does..." clause.

**Note:** Also found as [D1a-2](#d1a-2).

<a id="c6-17"></a>

### C6-17 · Wrong doc · Low · DevMode: "from a code constant"; a dev launch turns it on too

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/DevMode.cs:8-10`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/DevMode.cs#L8-L10)

**Says:** "The host sets &lt;see cref="IsEnabled"/> from a code constant before running anything: &lt;c>App.DevModeEnabled&lt;/c> in the GUI"

**Problem:** FeBuddy.Wpf/App.xaml.cs:52 has `DevMode.IsEnabled = DevModeEnabled || devLaunch;` (a Debug build started with `--dev`). Architecture.md:339 says the same.

**Proposed:** "...before running anything: &lt;c>App.DevModeEnabled&lt;/c>, or a dev launch (&lt;c>--dev&lt;/c>, Debug builds), in the GUI; &lt;c>HarnessSettings.DevMode&lt;/c> in &lt;c>FeBuddy.Harness&lt;/c>."

<a id="c6-19"></a>

### C6-19 · Wrong doc · Low · OutputFormatting remarks miss the reload after a profile switch or import

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/OutputFormatting.cs:8-12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/OutputFormatting.cs#L8-L12)

**Says:** "&lt;c>LaunchSequence&lt;/c> loads it from &lt;c>UserConfig&lt;/c> at start-up, the Settings screen sets it when the user saves, and &lt;c>FeBuddy.Harness&lt;/c> can set it directly."

**Problem:** `ConfigPages` (FeBuddy.Wpf/ViewModels/ConfigPages.cs:72) also calls `LoadFromUserConfig` after a profile switch or an import.

**Proposed:** "...at start-up and &lt;c>ConfigPages&lt;/c> again after a profile switch or an import, the Settings screen sets it when the user saves, ..."

<a id="c6-32"></a>

### C6-32 · Minor · Low · UserConfigAreas.Tabs: "a tab whose only area is the ROI"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigAreas.cs:50-53`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigAreas.cs#L50-L53)

**Says:** "or &lt;see langword="null"/> for a tab whose only area is the ROI."

**Problem:** Airports, Airways, NAVAIDs, Fixes and Wx Stations offer ROI or Everything (ChangeLog beta.6 Feature [#335](https://github.com/Nikolai558/FE-BUDDY/issues/335); `DefaultFor` returns `Everything` for them).

**Proposed:** "or &lt;see langword="null"/> for a tab with no ARTCC list (its areas are the ROI and Everything)."

<a id="c6-11"></a>

### C6-11 · Wrong doc · Low · FlattenInto says nulls are stored as JSON text; they are stored as ""

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:574-577`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L574-L577), 594-596

**Says:** "Arrays and nulls are stored as their JSON text; every other scalar is stored as its string form." vs `case null: dest[path] = string.Empty;`

**Problem:** 

**Proposed:** "Arrays are stored as their JSON text, a null as an empty string, and every other scalar as its string form."

<a id="c6-36"></a>

### C6-36 · Minor · Low · UserConfigFile remarks list 3 of the 7 shared settings as if complete

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:22-25`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L22-L25)

**Says:** "A key in &lt;see cref="UserConfigPortability.SharedByProfiles"/> - the update channel, which News has been read, FE-Buddy's GitHub token - is kept in &lt;c>Shared.json&lt;/c>"

**Problem:** `SharedByProfiles` also has `LegacyGitHubTokenNoticeShown`, `WelcomeShown`, `RememberWindowPlacement` and `MainWindowPlacement` (UserConfigPortability.cs:40-49).

**Proposed:** "- the update channel, which News has been read, FE-Buddy's GitHub token, where the main window was and the like -"

<a id="c6-37"></a>

### C6-37 · Minor · Low · `_stampVersion` starts from UserConfigVersion.Current but is reset everywhere else to UserConfigMigrations.CurrentVersion

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:86`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L86), 155, 559

**Says:** `private static int _stampVersion = UserConfigVersion.Current;` vs `_stampVersion = UserConfigMigrations.CurrentVersion;`

**Problem:** The two differ only while a test stands in its own layout (`UserConfigMigrations.ConfigureForTesting`), but a write before the first ReadAll would use the wrong one. ReadAll and ConfigureForTesting both use `UserConfigMigrations.CurrentVersion`.

**Proposed:** `private static int _stampVersion = UserConfigMigrations.CurrentVersion;`

<a id="c6-12"></a>

### C6-12 · Wrong doc · Low · UserConfigKeys.CoordinatePrecision: 0 means "don't round", not "0 places"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigKeys.cs:58`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigKeys.cs#L58)

**Says:** "How many decimal places GeoJSON coordinates are rounded to, 0 to 15 (6 when unset)."

**Problem:** `GeojsonFileWriter.NoRounding = 0` means do not round (GeojsonFileWriter.cs:24, 41, 79). Settings-Reference.md:113 has "`1`-`15` decimal places, or `0` for Do not round".

**Proposed:** "How many decimal places GeoJSON coordinates are rounded to, 1 to 15, or 0 not to round them (&lt;c>GeojsonFileWriter.NoRounding&lt;/c>); 6 when unset."

<a id="c6-13"></a>

### C6-13 · Wrong doc · Low · UserConfigKeys' stated rule ("keys read in more than one place") doesn't hold either way

**Status:** Fixed (not yet merged), in [S5](020_Work-Plan.md#s5) with the Settings page's keys ([W3-10](120_Logic-In-GUI.md#w3-10)), as proposed: the summary says what `UserConfigKeys` holds; `UserConfigKeys.Facility` and `DefaultRoiFilter` are the facility and default-ROI keys `UserConfigAreas` and `DefaultRoiStore` use; the override key is `UserConfigAreas.OverrideRoiKey`, used by the GeoJSON tabs · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigKeys.cs:3-6`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigKeys.cs#L3-L6); [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigAreas.cs:41-45`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigAreas.cs#L41-L45), 98; [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:84`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L84); [`FeBuddy/FeBuddy.Wpf/Shell/DefaultRoiStore.cs:23`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Shell/DefaultRoiStore.cs#L23); [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs:63`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs#L63)

**Says:** "The &lt;c>UserConfig.json&lt;/c> keys that are read in more than one place, so they are named once here."

**Problem:** The summary's rule fails both ways:
  - Some keys here are used by only one production file: `DuplicateAliasesReview` and `DuplicateAliasesChoices` (DuplicateAliasesCardViewModel only) and `MapOutputGeojson` (MapLayersState only). Their real role is also to be `UserConfigLayoutTests.OtherSettings`' list of keys saved outside a tab, which reads every const here by reflection (UserConfigLayoutTests.cs:125-141).
  - Some keys read in several places are spelled twice instead:
    - `Services.AiracService.UserArtccId`: `UserConfigAreas.FacilityKey`, and `SettingsViewModel.ArtccKey`, which is used by ProceduresViewModel, AiracServiceViewModel, ImportSettingsViewModel and SubServiceSettingsViewModel;
    - `Services.AiracService.DefaultRoi.FilterByRoi`: `UserConfigAreas.DefaultRoiKey` and `DefaultRoiStore.FilterKey`;
    - `Roi.OverrideDefaultRoi`: a literal at UserConfigAreas.cs:98 and `GeojsonSubServiceViewModel.OverrideRoiKey`.
  The file name `UserConfig.json` is also stale ([C6-14](#c6-14)). Every const here is still read or written, so there are no dead keys.

**Proposed:** Summary: "Settings keys used outside the one screen that edits them, read in more than one place or saved outside a sub-service tab's node (UserConfigLayoutTests checks these by name), named once here." Add `UserArtccId` and `DefaultRoiFilterByRoi` constants here, and use them from UserConfigAreas, `SettingsViewModel.ArtccKey` and `DefaultRoiStore.FilterKey`. Expose the override key once (e.g. `UserConfigAreas.OverrideRoiKey`) for GeojsonSubServiceViewModel.

**Note:** See also [D1a-3](500_Developer-Docs.md#d1a-3).

<a id="c6-9"></a>

### C6-9 · Wrong doc · Low · UserConfigMigration's example step is not safe to run twice, and reuses layout 2

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigration.cs:11-20`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigration.cs#L11-L20)

**Says:** `new UserConfigMigration(2, "Airways' buffer distances moved under Buffer", ...` / `settings.ChangeValue("Services.AiracService.Airways.Buffer.Enabled", value => value == "true" ? "Y" : "N");` / "Write a step so it does no harm run twice"

**Problem:** Run twice, that ChangeValue turns an already-converted `Y` into `N`, which breaks the rule stated three lines below it. Layout 2 is also the real Area step. Settings-Reference.md:640-644 has the corrected version.

**Proposed:** `new UserConfigMigration(3, ...)` and `value => value == "true" ? "Y" : value == "false" ? "N" : value`.

**Note:** Also found as [D1a-15](#d1a-15).

<a id="c6-10"></a>

### C6-10 · History wording · Low · Move() example narrates beta.1's VnasAlias rename; profiles summary cites "(issue #324)"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigrationContext.cs:44-46`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigrationContext.cs#L44-L46); [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs:9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs#L9)

**Says:** "or a whole section (&lt;c>Services.AiracService.VnasAlias&lt;/c> to &lt;c>Services.AiracService.ConcatenateAliases&lt;/c>)"; "Settings profiles (issue #324): each a whole set of settings"

**Problem:** `VnasAlias` is beta.1's node name, renamed in beta.2 with no fallback (ChangeLog.md:424-429). The example points at history that no current code handles. The issue number only narrates history.

**Proposed:** "or a whole section (&lt;c>Services.AiracService.OldNode&lt;/c> to &lt;c>Services.AiracService.NewNode&lt;/c>)"; "Settings profiles: each a whole set of settings in its own file, ..."

<a id="c6-7"></a>

### C6-7 · Stale doc · Low · UserConfigMigrations/UserConfigVersion still describe `UserConfig.json` and "before FE-Buddy has a second layout"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigrations.cs:8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigrations.cs#L8), 13-16, 54-55; [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigVersion.cs:5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigVersion.cs#L5), 24

**Says:** "at launch, for &lt;c>UserConfig.json&lt;/c> (&lt;see cref="UserConfigFile.ReadAll"/>)"; "so the bringing-forward of &lt;c>UserConfig.json&lt;/c> and of imports can be tested before FE-Buddy has a second layout"; "Every &lt;c>UserConfig.json&lt;/c> and every settings export is stamped with it"; "The top-level entry each &lt;c>UserConfig.json&lt;/c> carries its layout version in."

**Problem:** Since beta.4 the files are `User Configurations\UserConfig.<Profile>.json` plus `Shared.json` (which is stamped too, line 720). FE-Buddy has had layout 2 since beta.6. The "Changing the layout" remark (13-16) also leaves out the fingerprint that `UserConfigLayoutTests` requires for each sample, which Settings-Reference.md:650-651 does mention.

**Proposed:** - UserConfigMigrations.cs:8: "at launch, for the profile in use (&lt;see cref="UserConfigFile.ReadAll"/>)".
  - UserConfigMigrations.cs:54-55: "Stands in a current layout and its steps of the test's own, for testing how a profile and an import are brought forward. Unit tests only."
  - UserConfigMigrations.cs:13-16: add "...(&lt;c>Fixtures\UserConfig\UserConfig.v&lt;N&gt;.json&lt;/c>, with its fingerprint in &lt;c>UserConfigLayoutTests&lt;/c>)".
  - UserConfigVersion.cs:5: "Every settings profile, &lt;c>Shared.json&lt;/c> and every settings export is stamped with it".
  - UserConfigVersion.cs:24: "The top-level entry each settings file carries its layout version in."

<a id="c6-16"></a>

### C6-16 · Wrong doc · Low · UserConfigPortability lists vNAS, VATSIM and VATUSA logins as credentials

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigPortability.cs:19-21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigPortability.cs#L19-L21)

**Says:** "Credentials (a GitHub PAT, vNAS, VATSIM or VATUSA logins) live outside &lt;c>UserConfig.json&lt;/c>"

**Problem:** FE-Buddy has no such logins. `CredentialKind` is UsernamePassword, GitHubToken or Token for any website, and the only users are Add Custom Aliases and FE-Buddy's GitHub requests.

**Proposed:** "Credentials (GitHub tokens, a website's user name and password, API keys) live in Windows Credential Manager (&lt;see cref="Credentials.CredentialStore"/>), not in the settings files, so an export never sees them."

<a id="c6-14"></a>

### C6-14 · Stale doc · Low · Comments still call the settings file `UserConfig.json`

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigTransfer.cs:17`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigTransfer.cs#L17), 19, 40, 138, 201; [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/Models/UserConfigPackage.cs:6`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/Models/UserConfigPackage.cs#L6), 23; [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/PortablePathTokens.cs:115`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/PortablePathTokens.cs#L115); [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigPortability.cs:19`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigPortability.cs#L19); [`FeBuddy/FeBuddy.Core/Infrastructure/Credentials/CredentialStore.cs:15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Credentials/CredentialStore.cs#L15); [`FeBuddy/FeBuddy.Core/Infrastructure/Credentials/UrlSecrets.cs:6`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Credentials/UrlSecrets.cs#L6)

**Says:** "The file is the same nested JSON as &lt;c>UserConfig.json&lt;/c>"; "or a plain &lt;c>UserConfig.json&lt;/c>"; "never in &lt;c>UserConfig.json&lt;/c>"; "A web address is saved in &lt;c>UserConfig.json&lt;/c>"; example header `"configVersion": 1, "appVersion": "3.0.0"`

**Problem:** Since beta.4 a profile is `User Configurations\UserConfig.<Profile>.json` (`ProfileFilePath`). `UserConfig.json` is only beta.3's legacy name (`LegacyConfigFileName`). A plain-file import accepts any profile file. An export from 3.0.0 carries `configVersion` 2 (`UserConfigMigrations.CurrentVersion`), which Settings-Reference.md:578 shows.

**Proposed:** "a plain &lt;c>UserConfig.json&lt;/c>" becomes "a plain settings profile (&lt;c>UserConfig.&lt;Profile&gt;.json&lt;/c>)". "never in &lt;c>UserConfig.json&lt;/c>" / "saved in &lt;c>UserConfig.json&lt;/c>" / "live outside &lt;c>UserConfig.json&lt;/c>" become "never in a settings profile" / "saved in the settings profile" / "live outside the settings files". UserConfigTransfer.cs:201 comment: "A plain profile copied out of someone's User Configurations folder imports as it is." Example header: `"configVersion": 2`.

<a id="c6-15"></a>

### C6-15 · Wrong doc · Low · UserConfigTransfer remarks describe only a Replace import

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigTransfer.cs:30-41`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigTransfer.cs#L30-L41)

**Says:** "An import makes this PC's settings match the file's exactly: settings the file leaves out go back to their defaults, folders included. Only three things are kept: ..."

**Problem:** With `UserConfigImportMode.Merge`, `Plan` keeps the profile's settings the file leaves out (lines 327-343) and doesn't clear folders (347-349). `ApplyAsNewProfile` writes a new profile instead. The remarks predate Merge (beta.4).

**Proposed:** Begin: "A Replace import (&lt;see cref="UserConfigImportMode.Replace"/>) makes the profile in use match the file exactly: ...". Add: "A Merge keeps the profile's settings the file leaves out, folders included, except a numbered list the file has, which it replaces whole, and any key that would nest under one of the file's or the other way round."

<a id="c6-8"></a>

### C6-8 · Wrong doc · Low · "Layout 1 is the one beta.2 and beta.3 saved, before files were stamped": beta.4 and beta.5 saved it too, stamped

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigVersion.cs:16-17`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigVersion.cs#L16-L17); [`docs/Developers/Settings-Reference.md:662`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Settings-Reference.md?plain=1#L662)

**Says:** "Layout 1 is the one 3.0.0-beta.2 and beta.3 saved, before files were stamped."; table row `| 1 | 3.0.0-beta.2 | (no stamp) |`

**Problem:** Stamping came in beta.4 with `Current = 1`, so beta.4 and beta.5 wrote `"ConfigVersion": 1`. Layout 2 starts at beta.6.

**Proposed:** "Layout 1 is the one 3.0.0-beta.2 to beta.5 saved; beta.2 and beta.3 wrote no stamp, and a file with no stamp is taken to be in it." Table: `| 1 | 3.0.0-beta.2 | (stamped from 3.0.0-beta.4) |`. Both go away if [C6-1](130_Old-Version-Code.md#c6-1) is taken.

**Note:** Also found as [D1a-9](500_Developer-Docs.md#d1a-9).

<a id="n1-36"></a>

### N1-36 · Stale doc · Low · Docs still name the Settings page's keys under `Services.AiracService`

**Status:** Fixed (not yet merged). The four docs name `General.Facility` and `General.DefaultRoi`, as proposed; [W7-8](220_Comments-GUI.md#w7-8) is done with it · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:22`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L22), [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:299`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L299); [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:215`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L215); [`FeBuddy/FeBuddy.Wpf/Shell/DefaultRoiStore.cs:9-10`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Shell/DefaultRoiStore.cs#L9-L10)

**Says:** `(e.g. <c>Services.AiracService.UserArtccId</c>)`; SettingsViewModel: `Persists to <c>Services.AiracService.UserArtccId</c>`; DefaultRoiStore: `(<c>Services.AiracService.DefaultRoi</c> in <c>UserConfig.json</c>)`

**Problem:** The Facility, coordinate precision and default ROI moved to `General` in [S5](020_Work-Plan.md#s5). These docs send a reader to the old node.

**Proposed:** UserConfigFile.cs:22 and :299: `(e.g. <c>General.Facility</c>)`. SettingsViewModel.cs:215: `/// <summary>The selected facility's <c>RespArtccId</c>. Persists to <c>General.Facility</c> (<see cref="UserConfigKeys.Facility"/>).</summary>`. DefaultRoiStore.cs:9: `(<c>General.DefaultRoi</c> in the profile in use)`, which also does [W7-8](220_Comments-GUI.md#w7-8)'s line 9; do W7-8's line 29 too.

## FeBuddy.Core/Infrastructure/Credentials

<a id="c6-24"></a>

### C6-24 · Minor · Low · CredentialStore.TargetPrefix: the uninstaller keeps its own copy, and nothing checks they match

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Credentials/CredentialStore.cs:35`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Credentials/CredentialStore.cs#L35); [`FeBuddy/FeBuddy.Installer.CustomActions/CredentialCleanupActions.cs:24`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer.CustomActions/CredentialCleanupActions.cs#L24)

**Says:** "The start of every FE-Buddy entry's name in Credential Manager. The uninstaller removes entries by it too."

**Problem:** The uninstaller (net472, which can't reference Core) has its own `TargetPrefix = "FE-Buddy:credential:"`. Only its own remarks say the two must stay equal. No test compares them (`grep -rn CredentialCleanupActions FeBuddy.UnitTests` finds nothing). A change on one side would silently leave credentials behind on uninstall.

**Proposed:** "...The uninstaller removes entries by the same prefix: keep &lt;c>CredentialCleanupActions.TargetPrefix&lt;/c> (FeBuddy.Installer.CustomActions) equal to it." Optionally add a `Repository/` test that reads CredentialCleanupActions.cs and compares the literal.

## FeBuddy.Core/Infrastructure/Dtpp

<a id="c7-9"></a>

### C7-9 · Wrong doc · Med · "The FAA publishes the d-TPP Metafile 15-18 days before the cycle", but the FAA says 20 days

**Status:** Agreed, changed: correct the wording to "approximately 20 days prior to" before 3.0; the single Core constant waits until after 3.0 ([D4](010_Decisions.md#d4)) · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/DtppDownloader.cs:29-30`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/DtppDownloader.cs#L29-L30); [`FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/DtppFiles.cs:31-33`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/DtppFiles.cs#L31-L33); [`FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/Models/DtppDownloadOutcome.cs:15-16`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/Models/DtppDownloadOutcome.cs#L15-L16); [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureService.cs:23`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureService.cs#L23), 81; [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs:484-491`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs#L484-L491); [`docs/Developers/Architecture.md:122-123`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Architecture.md?plain=1#L122-L123); [`docs/Users/User-Guide.md:201`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L201); [`docs/Users/Glossary.md:33`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/Glossary.md?plain=1#L33)

**Says:** `The FAA publishes a cycle's metafile only about 15-18 days before its effective date`

**Problem:** The FAA's d-TPP page says "Next edition files will be available 20 days prior to their effective date" (https://www.faa.gov/air_traffic/flight_info/aeronav/digital_products/dtpp/). The 15-18 figure goes back to a 2021 2.x dialog (commit 27901bd, "Added Meta Not Found Form"). Users see the number: the General tab tooltip turns it into dates ("expect it between {-18d} and {-15d}"), and the run advisory repeats it. The window is also a business rule computed in the GUI (AiracGeneralTabViewModel.cs:484-486) instead of Core.

**Proposed:** Check against FE-Buddy's own log: the line "Cycle 2611's d-TPP Metafile downloaded and placed" shows when the 2611 metafile first appeared (2611 takes effect 2026-10-29, so 20 days before is 2026-10-09). Then put the number in one place, e.g. `DtppFiles.PublishedDaysBeforeEffective = 20` with `DtppFiles.ExpectedPublication(AiracCycleInfo)`. Use it in DtppFiles, DtppDownloader, DtppDownloadOutcome, ProcedureService and AiracGeneralTabViewModel (which then stops doing the date maths), and in the three docs. If the log confirms 20: "about 20 days before".

**Note:** Also found as [W2-24](120_Logic-In-GUI.md#w2-24), [T3-8](120_Logic-In-GUI.md#t3-8). **Kyle:** say "approximately 20 days prior to" everywhere, from one Core constant ([D4](010_Decisions.md#d4)).

<a id="c7-11"></a>

### C7-11 · Wrong doc · Low · The XML parsers throw `XmlException` for malformed files, but docs say `InvalidDataException`

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/DtppDownloader.cs:54-57`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/DtppDownloader.cs#L54-L57); [`FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/Parsers/DtppMetafileXmlParser.cs:42-45`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/Parsers/DtppMetafileXmlParser.cs#L42-L45), 50, 68; [`FeBuddy/FeBuddy.Core/Infrastructure/WxStations/Parsers/WxStationXmlParser.cs:20-23`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/WxStations/Parsers/WxStationXmlParser.cs#L20-L23), 30

**Says:** `<exception cref="InvalidDataException">Thrown when the downloaded data does not parse as a d-TPP Metafile, or parses to a different cycle ...`

**Problem:** A payload that isn't well-formed XML throws `System.Xml.XmlException` from `XmlReader`/`XDocument.Load`, not `InvalidDataException`. Examples: an HTML error page served with 200 (its `<!DOCTYPE html>` hits `DtdProcessing.Prohibit`), or a cut-off file. The callers catch every exception (AiracCycleDataCache.EnsureDtppAsync, SharedDataDownload), so nothing breaks; the docs are just wrong. The JSON parsers next to them document `JsonException` (GngAirlineParser.cs:34, VatsimRadarAirlineParser.cs:37).

**Proposed:** In `DtppMetafileXmlParser.Parse` and `WxStationXmlParser.Parse`, catch `XmlException` and rethrow `new InvalidDataException($"... '{xmlPath}' is not well-formed XML: {ex.Message}", ex)`. Then all three docs are true. Otherwise, add `<exception cref="System.Xml.XmlException">Thrown when the file is not well-formed XML.</exception>` to all three.

<a id="c7-12"></a>

### C7-12 · Minor · Low · DtppDownloader is an instance class with no state; its remark explains why there is nothing to memoize

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/DtppDownloader.cs:17-21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/DtppDownloader.cs#L17-L21), 36, 58, 72; [`FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs:166`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs#L166)

**Says:** `- and a <see cref="DtppDownloader"/> instance keeps no state` / `between calls, so there is nothing to memoize.`

**Problem:** House rule: "Classes are mostly static". Every sibling downloader (WxStationDownloader, TelephonyDownloader, NasrCycleDownloader, SharedDataDownload) is a static class. The only reason given for an instance is an argument against memoizing, which reads as a leftover from an earlier design.

**Proposed:** Make it `public static class DtppDownloader` with static methods. Change AiracCycleDataCache.cs:166 to `DtppDownloader.EnsureCycleHasMetafileAsync(...)` and update its tests. In the remarks, drop "- and a DtppDownloader instance keeps no state between calls, so there is nothing to memoize".

## FeBuddy.Core/Infrastructure/Dtpp/Models

<a id="c7-13"></a>

### C7-13 · Minor · Low · "any amount of whitespace" in the effective-date docs, but the pattern needs at least one

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/Models/DtppMetafileDataCollection.cs:31`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/Models/DtppMetafileDataCollection.cs#L31); [`FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/Parsers/DtppMetafileXmlParser.cs:267`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/Parsers/DtppMetafileXmlParser.cs#L267), 298-299

**Says:** `when the text doesn't match HHMMZ followed by any amount of whitespace and MM/DD/YY` / `[GeneratedRegex(@"^(\d{2})(\d{2})Z\s+(\d{2})/(\d{2})/(\d{2})$")]`

**Problem:** `\s+` needs one or more, so `0901Z09/03/26` gives null.

**Proposed:** Replace "any amount of whitespace" with "one or more spaces" in all three places.

## FeBuddy.Core/Infrastructure/Eram/Models

<a id="c5-4"></a>

### C5-4 · Wrong doc · Med · EramProperties.IsEmpty is documented backwards

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Eram/Models/EramProperties.cs:52-53`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Eram/Models/EramProperties.cs#L52-L53)

**Says:** `/// <summary>Whether any property is set.</summary>`   `public bool IsEmpty => this == None;`

**Problem:** IsEmpty is true when no property is set. EramGeoMapReader.cs:319 uses it that way: `properties.IsEmpty ? EramProperties.None : properties`.

**Proposed:** `/// <summary>Whether no property is set: the same values as <see cref="None"/>.</summary>`

<a id="c5-11"></a>

### C5-11 · Wrong doc · Low · EramGeoMapObject.Name is not used in file names

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Eram/Models/EramGeoMapObject.cs:21-25`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Eram/Models/EramGeoMapObject.cs#L21-L25)

**Says:** "What the object is called in file names and messages: &lt;c>&lt;MapObjectType&gt;_&lt;MapGroupId&gt;&lt;/c>"

**Problem:** Only messages use it (EramGeojsonWriter.cs:133, `context`). The By Attributes file names build `_Type {ObjectType}_Group {MapGroupId}` themselves (EramGeojsonWriter.cs:387-389).

**Proposed:** "What the object is called in messages: …"

## FeBuddy.Core/Infrastructure/FileSystem

<a id="c6-21"></a>

### C6-21 · Wrong doc · Low · AppPaths: "Everything FE-Buddy stores lives under one of them"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/FileSystem/AppPaths.cs:4-5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/FileSystem/AppPaths.cs#L4-L5)

**Says:** "The two folders FE-Buddy keeps on the user's machine. Everything FE-Buddy stores lives under one of them"

**Problem:** Credentials live in Windows Credential Manager (`CredentialStore`), and output goes to the user's output folder (`ServiceOutputPaths`).

**Proposed:** "The two folders FE-Buddy keeps for itself on the user's machine. Everything it stores, apart from its output (where the user points it) and saved credentials (Windows Credential Manager), lives under one of them, so this is the one place their locations are spelled out."

<a id="c6-20"></a>

### C6-20 · Wrong doc · Low · TempWorkspace: Downloads holds more than "cycle archives ... before extraction"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/FileSystem/TempWorkspace.cs:6-8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/FileSystem/TempWorkspace.cs#L6-L8), 26

**Says:** "Downloads land here before their useful contents are extracted into the permanent cache"; "Where cycle archives are downloaded before extraction: &lt;c>%TEMP%\FE-Buddy\Downloads&lt;/c>."

**Problem:** Besides `NasrCycleDownloader` (zip), `EnsureDownloadsDirectory` is used by `DtppDownloader.cs:87` (d-TPP Metafile XML) and `SharedDataDownload.cs:117` (the Wx station list, Telephony pages and airline lists). Those are checked and moved, not extracted. `UpdateInstaller.UpdatesDirectory` (UpdateInstaller.cs:29) also puts the update MSI under `%TEMP%\FE-Buddy\Updates`.

**Proposed:** Summary: "Downloads land here first (and the update's MSI, in &lt;c>Updates&lt;/c>); what is kept is moved or extracted into &lt;c>%APPDATA%\FE-Buddy&lt;/c>, and the whole tree is wiped on launch." DownloadsDirectory: "Where downloads land before they are checked and moved or extracted: NASR cycle archives, d-TPP Metafiles, Wx Stations and Telephony data."

## FeBuddy.Core/Infrastructure/FileSystem/Models

<a id="c6-22"></a>

### C6-22 · Wrong doc · Low · AppDataResetResult.NotDeleted can also hold "saved credentials"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/FileSystem/Models/AppDataResetResult.cs:9-12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/FileSystem/Models/AppDataResetResult.cs#L9-L12); [`FeBuddy/FeBuddy.Core/Infrastructure/FileSystem/AppDataReset.cs:125`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/FileSystem/AppDataReset.cs#L125)

**Says:** "The files and folders under &lt;c>%APPDATA%\FE-Buddy&lt;/c> that could not be deleted (in use, say), by name." vs `notDeleted.Add("saved credentials");`

**Problem:** 

**Proposed:** "...by name, and &lt;c>saved credentials&lt;/c> when Windows Credential Manager could not remove them. Empty when everything went." (The remaining AppDataReset comments match the code: everything in the root goes except `User Configurations`, `UserConfig.json` and `DevLaunch.json` when settings are kept. `%TEMP%\FE-Buddy`, the output folder and `%LOCALAPPDATA%\FE-BUDDY` are not touched. The request file is deleted before anything else.)

## FeBuddy.Core/Infrastructure/Http

<a id="c6-33"></a>

### C6-33 · Minor · Low · FeBuddyHttp.DownloadToFileAsync: the exception doc says "ended before" but any size mismatch throws

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Http/FeBuddyHttp.cs:38`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Http/FeBuddyHttp.cs#L38), 62-65

**Says:** `<exception cref="IOException">The body ended before the expected size - a truncated download.</exception>` vs `if (total is long expected && received != expected)`

**Problem:** A body longer than `Content-Length` or than the caller's `expectedBytes` throws too. UpdateInstaller passes the release asset's size.

**Proposed:** "The body's length differs from the expected size: a truncated (or wrong) download."

## FeBuddy.Core/Infrastructure/Logging

<a id="c6-18"></a>

### C6-18 · Wrong doc · Low · AppLog.StartFileSink: "Call once from the launch sequence"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Logging/AppLog.cs:174-176`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Logging/AppLog.cs#L174-L176)

**Says:** "Starts the background file sink and prunes log files older than &lt;paramref name="retentionDays"/> days. Call once from the launch sequence."

**Problem:** The only caller is `App.OnStartup` (FeBuddy.Wpf/App.xaml.cs:60), after `AppDataReset.RunPending` and before `LaunchSequence` starts. LaunchSequence never calls it, and the Harness doesn't either. Entries written before it are queued in the channel and written once it starts. Retention (30) and Debug-only-in-dev-mode match the comments and Architecture.md:303-306.

**Proposed:** "Call once at start-up, after any pending reset (&lt;c>App.OnStartup&lt;/c>); entries written before it are queued and written then. Calling it again is a no-op except that pruning runs again."

## FeBuddy.Core/Infrastructure/Logging/Models

<a id="c6-34"></a>

### C6-34 · Minor · Low · LogLevel lives in LogEntry.cs

**Status:** Fixed (not yet merged). `LogLevel` is in `Logging/Models/LogLevel.cs`; the namespace is unchanged · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Logging/Models/LogEntry.cs:5-30`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Logging/Models/LogEntry.cs#L5-L30)

**Says:** `public enum LogLevel` declared before `public record LogEntry` in LogEntry.cs

**Problem:** The house rule is one main type per file. `LogLevel` is used across Core and Wpf by itself (`ServiceMessage.Level`, about 150 Core references), not just as a part of `LogEntry`.

**Proposed:** Move `LogLevel` to `Logging/Models/LogLevel.cs`. The namespace is unchanged.

## FeBuddy.Core/Infrastructure/Markdown

<a id="c7-19"></a>

### C7-19 · Wrong doc · Low · MarkdownParser class summary: misses the What's New caller, overstates its fit to the content, and two claims are loose

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Markdown/MarkdownParser.cs:9-22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Markdown/MarkdownParser.cs#L9-L22), 344

**Says:** `for the text FE-Buddy shows from GitHub - release notes in the update window and News posts on the Dashboard. It covers what those bodies actually use` / `bare <c>https://</c> URLs` / `HTML comments are dropped, as GitHub does.`

**Problem:** (1) The What's New page (WhatsNewView.xaml, through `InlineMarkdown` → `ParseInlines`) also uses the parser, on built-in text. (2) "covers what those bodies actually use" is not true: it supports much more than any current content uses (see [C7-21](140_Unused-Code.md#c7-21)). (3) `BareUrlPattern` also links `http://`. (4) Only a comment that starts a line is dropped (`IsCommentStart` uses `TrimStart().StartsWith("<!--")`). A comment in the middle of a line stays as text, and text after `-->` on the closing line is dropped (`SkipComment` returns `i + 1`).

**Proposed:** Summary: "A small, forgiving Markdown parser for the Markdown FE-Buddy shows: release notes in the update window, News posts on the Dashboard, and the What's New page's inline text. It covers the GitHub Markdown those texts use or are likely to use, not all of CommonMark:". Bullet 2: "bare &lt;c>http(s)://&lt;/c> URLs". Bullet 3: "An HTML comment that starts a line is dropped, to the end of the line it closes on, as GitHub does."

<a id="c7-20"></a>

### C7-20 · Stale doc · Low · MarkdownParser `linkBase` example uses the path from before News.md moved to the root

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Markdown/MarkdownParser.cs:31-35`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Markdown/MarkdownParser.cs#L31-L35)

**Says:** `When set, a relative link - <c>[User Guide](../../docs/Users/User-Guide.md)</c> - resolves against`

**Problem:** `../../docs/...` is how a link looked from `FeBuddy/FeBuddy.Core/News.md`. The example was added in 52f2988 (2026-09-28 15:48), half an hour before 5b0c397 moved News.md to the root. News.md's own header (line 9) and MarkdownView.LinkBase's doc now use `docs/Users/User-Guide.md`.

**Proposed:** `<c>[User Guide](docs/Users/User-Guide.md)</c>`.

## FeBuddy.Core/Infrastructure/Nasr

<a id="c7-7"></a>

### C7-7 · Wrong doc · Low · `NasrCsvReader.ProcessLines` documents an exception it can't throw

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Nasr/NasrCsvReader.cs:33`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/NasrCsvReader.cs#L33), 47-53, 139-140

**Says:** `<exception cref="InvalidDataException">Thrown when the file has no header row.</exception>`

**Problem:** For a file with no header row, CsvHelper 33.1.0's `ReadHeader()` sets `headerRecord = parser.Record`, which is null, and `ParseNamedIndexes()` throws `CsvHelper.ReaderException("No header record was found.")`. The `?? throw new InvalidDataException` guard therefore never fires; the code comment at lines 50-51 admits this. Lines 139-140 are two stray blank lines before the closing brace.

**Proposed:** `<exception cref="CsvHelper.ReaderException">Thrown when the file has no header row.</exception>` (keep the guard if wanted, but stop advertising it), and delete the blank lines at 139-140.

<a id="c7-5"></a>

### C7-5 · History wording · Low · "removing the old extract-then-clean step"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Nasr/NasrCycleDownloader.cs:251-253`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/NasrCycleDownloader.cs#L251-L253)

**Says:** `... is skipped, saving ~5 MB` / `per cycle and removing the old extract-then-clean step.`

**Problem:** Describes code that no longer exists.

**Proposed:** End the sentence at "... is skipped, saving ~5 MB per cycle."

<a id="c7-6"></a>

### C7-6 · Wrong doc · Low · `EnsureCycleAvailableAsync` docs leave out two exceptions and one progress phase

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Nasr/NasrCycleDownloader.cs:84`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/NasrCycleDownloader.cs#L84), 87, 125, 183-188

**Says:** `<exception cref="HttpRequestException">Thrown when the download fails (network error, 404, etc.).</exception>` / `progress ... (download percentage, then extracting, then complete)`

**Problem:** The method also throws `InvalidOperationException` when the extracted folder is missing a required CSV (line 185), and `InvalidDataException` from `ZipFile.OpenRead` on a corrupt zip. Its first report can also be `AiracDownloadPhase.AlreadyAvailable` (line 125).

**Proposed:** Add `<exception cref="InvalidOperationException">Thrown when the extracted cycle is missing one of the required CSVs.</exception>` and `<exception cref="InvalidDataException">Thrown when the download is not a readable zip.</exception>`. Progress: "(already available; or download percentage, then extracting, then complete)".

## FeBuddy.Core/Infrastructure/Platform

<a id="c7-28"></a>

### C7-28 · Minor · Low · AppVersion remarks: the build reads the version from FE-BUDDY.dll, not the exe

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Platform/AppVersion.cs:12-14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Platform/AppVersion.cs#L12-L14)

**Says:** `The installer records this same` / `string as <c>ProductSemVer</c>, because the build reads it from the built exe`

**Problem:** build.ps1:49-51 and Build-ReleaseAssets.ps1:100-102 read `(Get-Item FE-BUDDY.dll).VersionInfo.ProductVersion`.

**Proposed:** "...because the build reads it from the built FE-BUDDY.dll (see docs/Developers/VERSIONING.md)."

<a id="c7-27"></a>

### C7-27 · History wording · Low · LegacySquirrelInstall: "as 2.9.x did" / "(the same as 2.9.x allowed)"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Platform/LegacySquirrelInstall.cs:23`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Platform/LegacySquirrelInstall.cs#L23), 39

**Says:** `Squirrel's own uninstaller removes it (<c>Update.exe --uninstall</c>), as 2.9.x did - never` / `How long launch waits for Squirrel's uninstaller (the same as 2.9.x allowed).`

**Problem:** Both point back at 2.9.x's code without giving a reason. The real reason is already in the sentence ("since Squirrel also owns the Installed apps entry"). The 2.x mentions that explain why the copy may still be there (lines 12-17) are this COMPAT class's subject and should stay.

**Proposed:** Line 23: drop ", as 2.9.x did". Line 39: `How long launch waits for Squirrel's uninstaller before trying again at the next launch.`

## FeBuddy.Core/Infrastructure/Sct/Models

<a id="c5-18"></a>

### C5-18 · Wrong doc · Low · SctRegion.Points "(not closed)" but a file may close it

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Sct/Models/SctRegion.cs:7`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Sct/Models/SctRegion.cs#L7); [`FeBuddy/FeBuddy.Core/Application/Conversions/SctToGeojson/SctGeojsonWriter.cs:169-173`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/SctToGeojson/SctGeojsonWriter.cs#L169-L173)

**Says:** `/// <param name="Points">Its outline, in order, as written (not closed).</param>`   `// A region already closed on its first point keeps the ring as written.`

**Problem:** The reader keeps the points exactly as written (SctFileReader.cs:409-417,433), which may repeat the first point. The writer handles that case.

**Proposed:** "Its outline, in order, as written; the last point may or may not repeat the first."

## FeBuddy.Core/Infrastructure/SharedData

<a id="c7-29"></a>

### C7-29 · Minor · Low · SharedDataDownload summary leaves out the virtual airline list

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/SharedData/SharedDataDownload.cs:9-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/SharedData/SharedDataDownload.cs#L9-L11)

**Says:** `outside any AIRAC cycle - the` / `Wx Stations list and the FAA telephony pages - replacing the kept copy`

**Problem:** TelephonyDownloader also uses it for both parts of the virtual airline list (lines 116-132), and the third remarks paragraph mentions them.

**Proposed:** "- the Wx Stations list, the FAA telephony pages and the virtual airline list's two parts -".

## FeBuddy.Core/Infrastructure/Telephony

<a id="c7-23"></a>

### C7-23 · History wording · Low · TelephonyFiles remarks tell the beta.4 story; keep only the reason

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Telephony/TelephonyFiles.cs:15-16`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Telephony/TelephonyFiles.cs#L15-L16)

**Says:** `(3.0.0-beta.4 downloaded the` / `merged list from <c>data.vatsim-radar.com</c>, which only takes TLS 1.3 - which Windows 10 can't do.)`

**Problem:** The TLS 1.3 / Windows 10 fact is a bug-driven reason for building the list from two sources, so it stays. "3.0.0-beta.4 downloaded" is history.

**Proposed:** `(VATSIM-Radar's own merged list, at <c>data.vatsim-radar.com</c>, only takes TLS 1.3, which Windows 10 can't do.)`

## FeBuddy.Core/Infrastructure/Telephony/Models

<a id="c7-24"></a>

### C7-24 · Minor · Low · The virtual-airline type and property are named after VATSIM-Radar but also hold GNG's rows

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Telephony/Models/VatsimRadarAirline.cs:3-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Telephony/Models/VatsimRadarAirline.cs#L3-L11); [`FeBuddy/FeBuddy.Core/Infrastructure/Telephony/Models/TelephonyDataCollection.cs:19-24`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Telephony/Models/TelephonyDataCollection.cs#L19-L24); [`FeBuddy/FeBuddy.Core/Infrastructure/Telephony/Parsers/GngAirlineParser.cs:36`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Telephony/Parsers/GngAirlineParser.cs#L36)

**Says:** `/// Every virtual airline on the virtual airline list, in the list's order (see <see cref="Parsers.VatsimRadarAirlineParser"/>)` / `public List<VatsimRadarAirline> VatsimRadarAirlines`

**Problem:** Since beta.5 the list is GNG's rows merged with VATSIM-Radar's virtual airlines (`VatsimRadarVirtualAirlines.Merge`). `GngAirlineParser.Parse` returns `VatsimRadarAirline`s, and the collection doc points at only one of the two parsers. The names come from when VATSIM-Radar was the only source.

**Proposed:** Doc now: "...in the list's order: GNG's airlines merged with VATSIM-Radar's (see VatsimRadarVirtualAirlines.Merge)". Optional rename later (wide rename touching Wpf and tests): `VatsimRadarAirline` → `VirtualAirline`, `TelephonyDataCollection.VatsimRadarAirlines` → `VirtualAirlines`.

## FeBuddy.Core/Infrastructure/Telephony/Parsers

<a id="c7-25"></a>

### C7-25 · Minor · Low · Invisible literal characters in `TelephonyHtmlParser.CleanText` and its hyphen regex

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Telephony/Parsers/TelephonyHtmlParser.cs:179`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Telephony/Parsers/TelephonyHtmlParser.cs#L179), 202

**Says:** `text = text.Replace("­", string.Empty, StringComparison.Ordinal).Replace(' ', ' ');` / `[GeneratedRegex(@"[‐-―−]")]`

**Problem:** Line 179 holds a raw U+00AD (soft hyphen) and U+00A0 (no-break space); they look like `""` and `' '`. Line 202 holds raw U+2010, U+2015 and U+2212. They are hard to see or review, and an editor or formatter can silently turn them into ordinary characters, which changes behaviour. (The NBSP replace is also redundant, because .NET's `\s` in `Whitespace()` matches U+00A0.)

**Proposed:** `text.Replace("­", string.Empty, StringComparison.Ordinal)` (drop the NBSP replace, or write `' '`), and `[GeneratedRegex(@"[‐-―−]")]`.

## FeBuddy.Core/Infrastructure/WxStations/Parsers

<a id="c7-14"></a>

### C7-14 · Wrong doc · Low · `WxStationXmlParser.ParseAsync` names the wrong caller

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/WxStations/Parsers/WxStationXmlParser.cs:48`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/WxStations/Parsers/WxStationXmlParser.cs#L48)

**Says:** `Async wrapper over <see cref="Parse"/>, for callers already on an async pipeline (e.g. <c>AiracCycleDataCache</c>).`

**Problem:** AiracCycleDataCache never touches Wx stations. The only caller is AiracSharedDataLoader.cs:103. The text was copied from DtppMetafileXmlParser.ParseAsync, where it is correct.

**Proposed:** `(e.g. <c>AiracSharedDataLoader</c>)`.

## Reported more than once

Each review slice was read by its own reviewer, so some findings were reported twice. These IDs point at the finding that is kept.

- <a id="d1a-2"></a>**D1a-2** · Code comment contradicts the doc on whether moving the settings file is a new layout · same as [C6-6](#c6-6).
- <a id="d1a-15"></a>**D1a-15** · UserConfigMigration's XML example breaks its own "run twice" rule and reuses layout 2 · same as [C6-9](#c6-9).
- <a id="d1a-16"></a>**D1a-16** · ProcedureSettingsParser's `AirportProcedures` error example is an approach, which is left out · same as [C4-10](#c4-10).
- <a id="d1b-8"></a>**D1b-8** · AirwayGeojsonOutputBy.Designation says the opposite of AirwayClassifier · same as [C2-1](#c2-1).
- <a id="t1a-36"></a>**T1a-36** · ProceduresJsonWriter remark still contrasts with a de-duplicating Markdown document · same as [C4-1](#c4-1).
- <a id="t1b-26"></a>**T1b-26** · NavaidGeojsonWriter says the alias file covers every *included* NAVAID · same as [C3-10](#c3-10).
- <a id="t1b-27"></a>**T1b-27** · ArtccBoundaryFilter claims it matches ignoring case · same as [C3-9](#c3-9).
