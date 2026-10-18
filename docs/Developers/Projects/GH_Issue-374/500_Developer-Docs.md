# 500 · Developer docs

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

Developer docs: `docs/Developers`, `README.md` and `docs/SECURITY.md`. Session [S11](020_Work-Plan.md#s11).

**36 findings** (1 High, 4 Med, 31 Low). Quote the ID when you comment on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374).

## Summary

| ID | Kind | Sev | Finding | Status | Session |
|---|---|---|---|---|---|
| [D1b-11](#d1b-11) | Wrong doc | Med | SECURITY.md: 2.x Beta channel already gets 3.0 betas; the support table goes stale at 3.0.0 | Open | S11 |
| [D1b-2](#d1b-2) | Wrong doc | Med | "A new NASR CSV group" steps miss the Group() arm, the Code arm, the parse method and the test fixtures | Open | S11 |
| [D1b-3](#d1b-3) | Wrong doc | Med | "Adding a sub-service" misses the steps that wire a new sub-service's files into the run | Open | S11 |
| [D1a-1](#d1a-1) | Wrong doc | Med | "Adding a setting" step 4: LocalKeys also makes a setting shared by every profile | Open | S11 |
| [C1-32](#c1-32) | Wrong doc | Low | Architecture.md: "one left unloaded is NotLoaded" mixes up a cycle's state and the service's readiness | Open | S11 |
| [C1-33](#c1-33) | Wrong doc | Low | Architecture.md's launch steps leave out the GitHub token check | Open | S11 |
| [D1b-4](#d1b-4) | History wording | Low | Architecture narrates 3.0.0-beta.4's single airline file | Open | S11 |
| [D1b-5](#d1b-5) | History wording | Low | "still parses all 24" | Open | S11 |
| [D1b-6](#d1b-6) | Wrong doc | Low | Output layout bullet leaves out Publication_Docs and the duplicate report | Open | S11 |
| [D1b-7](#d1b-7) | Minor | Low | Designation rule names no class in a list where "each rule lives in one class" | Open | S11 |
| [D1b-10](#d1b-10) | Minor | Low | Naming table says `*Settings` is data in Models/; DevLaunchSettings is a static I/O class at the feature root | Agreed | S6 |
| [D1b-9](#d1b-9) | Stale doc | Low | Code-Structure's folder maps miss some major types | Open | S11 |
| [W5-22](#w5-22) | Stale doc | Low | Code-Structure's folder map leaves out Views/Models | Open | S11 |
| [W7-26](#w7-26) | Stale doc | Low | Code-Structure's Shell/ list misses the window-placement files | Open | S11 |
| [N1-1](#n1-1) | Stale doc | Low | The developer README describes the custom-actions project without its new launch action | Open | S11 |
| [D1b-15](#d1b-15) | Minor | Low | RELEASING-QuickRef.md: wrong snippet under NEWS.md, wrong branch and version casing, typos, and not linked from anywhere | Open | S11 |
| [D1b-12](#d1b-12) | Stale doc | Low | Release-day staleness in RELEASING.md and VERSIONING.md | Open | S11 |
| [D1b-13](#d1b-13) | History wording | Low | Figure 3's caption says the screenshot is from an older format | Open | S11 |
| [D1b-14](#d1b-14) | Minor | Low | RELEASING.md: branch-protection paragraph contradicts itself; "What does what" and the check table are incomplete | Open | S11 |
| [C2-31](#c2-31) | Stale doc | Low | Settings-Reference: `ExcludedDesignations` example `RN,SL` is not a designation FE-Buddy produces | Open | S11 |
| [C2-32](#c2-32) | Wrong doc | Low | Settings-Reference: Airways CRC keys for unwritten or excluded files | Open | S11 |
| [C3-15](#c3-15) | History wording | Low | Settings-Reference: "the key keeps its old name" | Open | S11 |
| [D1a-10](#d1a-10) | History wording | Low | Procedure lists: "saved before 3.0.0-beta.9" | Open | S11 |
| [D1a-11](#d1a-11) | History wording | Low | "the old `IncludeRoiAirports`", "before there was a choice", "never meant" | Open | S11 |
| [D1a-13](#d1a-13) | History wording | Low | "as the profiles did" / "no migration step was needed" (duplicated) | Open | S11 |
| [D1a-14](#d1a-14) | History wording | Low | Import: "beta.3 `UserConfig.json`" | Open | S11 |
| [D1a-18](#d1a-18) | Minor | Low | Readability: duplication and placement | Open | S11 |
| [D1a-3](#d1a-3) | Wrong doc | Low | "keys read in more than one place are constants in UserConfigKeys" is not true for the facility and the default ROI | Open | S11 |
| [D1a-4](#d1a-4) | Wrong doc | Low | Procedure Charts and Telephony don't read `CoordinatePrecision`, `FebProperties` or `CrcDefaultsFor`, though the app sends them | Open | S11 |
| [D1a-5](#d1a-5) | Wrong doc | Low | MapService: the base-map settings are saved by BaseMapSettings, and the opacity is saved only after the slider settles | Open | S11 |
| [D1a-6](#d1a-6) | Wrong doc | Low | `HelpCommandName` is sent only while `IncludeHelpCommand` is also `Y`, and `RenameHelpCommand` has no row | Open | S11 |
| [D1a-7](#d1a-7) | Wrong doc | Low | NAVAIDs: missing that the merged Symbols file's `style` isn't read while styled by type | Open | S11 |
| [D1a-8](#d1a-8) | Wrong doc | Low | "Adding a setting" step 3 doesn't fit Add Custom Aliases, SCT2, the numbered lists, or the harness | Open | S11 |
| [D1b-16](#d1b-16) | Stale doc | Low | TODO.md: Harness item still open; the "tested today" list misses newer Wpf tests | Open | S11 |
| [O1-36](#o1-36) | Wrong doc | Low | VERSIONING.md: the version check keeps only "the user's channel" | Open | S11 |
| [D1b-1](#d1b-1) | Unused | High | Remove CRC's AliasParser.cs from the repository before the public release | Agreed, changed | S6 |

## docs

<a id="d1b-11"></a>

### D1b-11 · Wrong doc · Med · SECURITY.md: 2.x Beta channel already gets 3.0 betas; the support table goes stale at 3.0.0

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/SECURITY.md:52-60`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/SECURITY.md?plain=1#L52-L60)

**Says:** "| 2.9.x | :warning: Critical security fixes only, until 3.0.0 is released |" ... "2.9.x's updater offers it once 3.0.0 is out, or now on its Alpha channel."

**Problem:** 2.x's channels match 3.x's (ReleaseChannel.cs:12-13): Beta accepts -beta, -rc and stable. So 2.9.x users on Beta are offered 3.0.0-beta.N today, as VERSIONING.md:144-145 says ("Alpha and Beta now get 3.0.0-beta.N"). On the day 3.0.0 ships, the "until 3.0.0 is released" row and the "or now on its Alpha channel" clause are both out of date in a public policy file.

**Proposed:** For the 3.0.0 release, change the 2.9.x row to `| 2.9.x | :x: |` (or ":warning: Critical security fixes only, until &lt;date>" if that is the plan). Replace 58-60 with: "FE-BUDDY 2.9.3 is the last 2.x release. Please move to FE-BUDDY 3.x: download it from [Releases](https://github.com/Nikolai558/FE-BUDDY/releases), or accept the update 2.9.x's updater offers." Before 3.0.0, at least change "on its Alpha channel" to "on its Alpha or Beta channel".

## docs/Developers

<a id="d1b-2"></a>

### D1b-2 · Wrong doc · Med · "A new NASR CSV group" steps miss the Group() arm, the Code arm, the parse method and the test fixtures

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Code-Structure.md:220-223`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Code-Structure.md?plain=1#L220-L223)

**Says:** "its row model in `Infrastructure/Nasr/Models/`, its parser in `Infrastructure/Nasr/Parsers/`, a `NasrCsvGroup` value, a `NasrCsvDataCollection` property and `Require…` method, and a line in `NasrCsvParser.ParseGroupAsync`."

**Problem:** Following these steps leaves (a) no arm in `NasrCsvDataCollection.Group()` (NasrCsvDataCollection.cs:194-221), whose `_` arm throws `ArgumentOutOfRangeException`. `ParsedGroups()` walks `NasrCsvGroups.All` through `Group()`, and the cycle cache calls it after every parse (AiracCycleDataCache.cs:807), so every launch's parse would fail. (b) No `NasrCsvGroups.Code` arm (NasrCsvGroups.cs:39-44), which a code with an underscore needs (`CLS_ARSP`, `MIL_OPS`), or `NotParsed`'s message names the wrong files. (c) No `Parse<Group>Async` method in NasrCsvParser, which `ParseGroupAsync`'s arm calls. (d) No fixture CSVs: `parse_all_async_still_fills_all_24_groups` (NasrCsvGroupParseTests.cs:40-46) parses every group from `Fixtures/Nasr` and asserts the count of 24. The steps also don't say that the group's `XyzCsvDataCollection` lives in the parser's file.

**Proposed:** Replace the bullet with:
  ```
  - **A new NASR CSV group** (FAA code `XYZ`):
    1. Its row models in `Infrastructure/Nasr/Models/XyzCsvDataModels.cs`, and its parser in
       `Infrastructure/Nasr/Parsers/XyzCsvParser.cs` with the group's `XyzCsvDataCollection` in the same file.
    2. A `NasrCsvGroup.Xyz` value. If the FAA's code isn't the value's name in capitals (it has an
       underscore, like `CLS_ARSP`), an arm in `NasrCsvGroups.Code`.
    3. In `NasrCsvDataCollection`: an `Xyz` property, a `RequireXyz()` method and an arm in `Group()`.
       Without that arm, `ParsedGroups()` throws, and the cycle cache calls it after every parse.
    4. In `NasrCsvParser`: a `ParseXyzAsync` method and an arm in `ParseGroupAsync`.
    5. Sample files in `FeBuddy.UnitTests/Fixtures/Nasr` (the parser tests read every group from there),
       and the new count in `NasrCsvGroupParseTests` (and in Architecture's "9 of the 24").
    The cycle cache parses it once it's in `NasrCsvGroups.InUse`.
  ```

**Note:** Also found as [C8-24](210_Comments-NASR.md#c8-24).

<a id="d1b-3"></a>

### D1b-3 · Wrong doc · Med · "Adding a sub-service" misses the steps that wire a new sub-service's files into the run

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Code-Structure.md:236-240`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Code-Structure.md?plain=1#L236-L240)

**Says:** "Read the shared keys with `SubServiceSettingsReader`, put files where `AiracOutputPaths` says, add its block to `AiracServiceSettings` and its run to `AiracService`."

**Problem:** "its run to `AiracService`" isn't enough. `AiracService.RunAsync` also lists every block in `anySelected` (AiracService.cs:299-303). A run with only the new sub-service would then warn "no sub-service selected" and skip "Delete all files". Its alias file must also go in the `aliasOutputs` array (AiracService.cs:390-399), or it is left out of the duplicate report and `Combined_Alias.txt`. The result needs a property on `AiracServiceResult` (AiracService.cs:487-507). And `OutputFileNamesParser.CanRename` / `OtherFileKeys` (OutputFileNamesParser.cs:34-40, 79-86) lists every renamable key by sub-service, so a rename of the new files on the File Names tab is dropped with "it is not a file FE-Buddy writes".

**Proposed:** Replace the last sentence of step 1 with: "Read the shared keys with `SubServiceSettingsReader` and put files where `AiracOutputPaths` says. Add its block to `AiracServiceSettings`, and in `AiracService` add its run, its block to `anySelected`, its alias file (if any) to `aliasOutputs`, and its result to `AiracServiceResult`. Add its file keys to `OutputFileNamesParser.CanRename` so the File Names tab can rename them, and its keys to the `[Settings reference](Settings-Reference.md)`."

<a id="d1a-1"></a>

### D1a-1 · Wrong doc · Med · "Adding a setting" step 4: LocalKeys also makes a setting shared by every profile

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Settings-Reference.md:625-628`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Settings-Reference.md?plain=1#L625-L628); [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigPortability.cs:40-49`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigPortability.cs#L40-L49),74

**Says:** "A yes/no setting ending in `Folder` goes in `UserConfigPortability`'s `NotFolderKeys`, and a this-PC-only setting in its `LocalKeys`."

**Problem:** `SharedByProfiles => LocalKeys` (UserConfigPortability.cs:74). Adding a key to `LocalKeys` also takes it out of the profile file and saves it in `Shared.json` for every profile (`UserConfigFile.ProfileValues` :633-635, `Save` :417-423, `ReadShared` :676). The step doesn't say so, so a developer adding a "this-PC-only" setting would change where it is stored without knowing. `LocalKeys` also holds full dotted paths, all `UserConfigKeys` constants. A key outside `General`/`Services` is classified Local without being listed, but it stays in the profile (Classify :94).

**Proposed:** replace the last sentence of step 4 with: "A yes/no setting ending in `Folder` goes in `UserConfigPortability`'s `NotFolderKeys`. A this-PC-only setting under `General` or `Services` goes in its `LocalKeys` as a `UserConfigKeys` constant. That also makes every profile share it, since it is saved in `Shared.json` rather than in the profile (`SharedByProfiles`)."

<a id="c1-32"></a>

### C1-32 · Wrong doc · Low · Architecture.md: "one left unloaded is NotLoaded" mixes up a cycle's state and the service's readiness

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Architecture.md:55-57`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Architecture.md?plain=1#L55-L57)

**Says:** `The three cycles are always tracked; one left unloaded is \`NotLoaded\` until something asks for it (\`GetAsync\`), which probes, downloads and parses it then.`

**Problem:** `NotLoaded` is an `AiracCycleReadiness` and follows only the current cycle (AiracCycleDataCache.cs:545-550). A cycle left unloaded keeps the state `NotDownloaded` or `Downloaded`; a previous/next left unloaded never makes anything `NotLoaded`. GetAsync probes only a cycle not probed yet.

**Proposed:** "The three cycles are always tracked; one left unloaded loads when something asks for it (`GetAsync`), probing first if launch didn't. While that is the current cycle, the service's readiness is `NotLoaded`."

<a id="c1-33"></a>

### C1-33 · Wrong doc · Low · Architecture.md's launch steps leave out the GitHub token check

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Architecture.md:32-45`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Architecture.md?plain=1#L32-L45)

**Says:** steps 1-6 list temp/settings, token variable, Squirrel, shortcuts, time/internet, then "In parallel: the version check, the AIRAC data (below) and News."

**Problem:** `OptionalLaunchStep.CheckGitHubTokens` is a launch step too, but LaunchSequence doesn't run it: the shell starts it once launch has completed and while online (ShellViewModel.cs:414-436). Readers of the launch section won't find it.

**Proposed:** Add "7. Once launch is done and online, the shell checks the saved GitHub tokens (`OptionalLaunchStep.CheckGitHubTokens`)." (Or move that check into LaunchSequence so every launch step lives in Core.)

<a id="d1b-4"></a>

### D1b-4 · History wording · Low · Architecture narrates 3.0.0-beta.4's single airline file

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Architecture.md:116-118`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Architecture.md?plain=1#L116-L118)

**Says:** "The single `vatsim_radar_airlines.json` that 3.0.0-beta.4 and earlier kept (beta.4's from `data.vatsim-radar.com`, which only takes TLS 1.3 - Windows 10 can't) is read only while neither part has a copy, and deleted once both have one."

**Problem:** This is history narration about earlier betas. The one bug-driven reason in it (data.vatsim-radar.com needs TLS 1.3, which Windows 10 can't do) explains why the list is built from GNG and GitHub, and is worth keeping as a reason. The fallback it describes is COMPAT code for beta testers' kept copies (`TelephonyFiles.OlderVatsimRadarAirlinesFileName`, read in VatsimRadarVirtualAirlines.cs:131 and TelephonyDownloader.cs:93). Its owners in C-scope decide whether it stays.

**Proposed:** Replace 116-118 ("The single ... both have one.") with: "VATSIM-Radar's own merged list (`data.vatsim-radar.com`) isn't used: it only takes TLS 1.3, which Windows 10 can't do." If the old-file fallback code is kept, add one sentence: "An older single copy, `vatsim_radar_airlines.json`, is read only while neither part has a copy, and deleted once both have one." If the code is removed, drop that too.

<a id="d1b-5"></a>

### D1b-5 · History wording · Low · "still parses all 24"

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Architecture.md:76-77`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Architecture.md?plain=1#L76-L77)

**Says:** "`NasrCsvParser.ParseAllAsync` still parses all 24, for the Harness and tests."

**Problem:** "still" narrates the time before `InUse`. Its callers are the Harness (Program.cs:36) and tests only.

**Proposed:** "`NasrCsvParser.ParseAllAsync` parses all 24, for the Harness and tests."

<a id="d1b-6"></a>

### D1b-6 · Wrong doc · Low · Output layout bullet leaves out Publication_Docs and the duplicate report

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Architecture.md:186-187`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Architecture.md?plain=1#L186-L187)

**Says:** "**Every file is ready for vNAS**, so nothing is marked for upload: GeoJSON goes in `Geojson`, alias files in `Aliases`."

**Problem:** The cycle folder also has `Publication_Docs\` (Procedure Charts' `Procedure_Changes.md` and `Procedures.json`) and `Duplicate_Alias_Commands.txt` at its root (AiracOutputPaths.cs:15-22, 49, 55). "nothing is marked for upload" only makes sense against an older layout.

**Proposed:** "**Every file is ready for vNAS.** In the cycle folder (`AiracOutputPaths`), GeoJSON goes in `Geojson`, alias files in `Aliases`, Procedure Charts' documents in `Publication_Docs`, and `Duplicate_Alias_Commands.txt` sits at the top."

<a id="d1b-7"></a>

### D1b-7 · Minor · Low · Designation rule names no class in a list where "each rule lives in one class"

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Architecture.md:245`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Architecture.md?plain=1#L245), 252-253

**Says:** "Each rule lives in one class." ... "**Designation** comes from the airway ID's leading letters (`J` in `J3`), never NASR's `AWY_DESIGNATION`"

**Problem:** Every other bullet names its class. This rule is `AirwayClassifier.DeriveDesignation` (AirwayClassifier.cs:45-54).

**Proposed:** "**Designation** (`AirwayClassifier.DeriveDesignation`) comes from ..."

<a id="d1b-10"></a>

### D1b-10 · Minor · Low · Naming table says `*Settings` is data in Models/; DevLaunchSettings is a static I/O class at the feature root

**Status:** Agreed: done with the other renames in S6 ([H4](010_Decisions.md#h4)) · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`docs/Developers/Code-Structure.md:111`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Code-Structure.md?plain=1#L111); [`FeBuddy/FeBuddy.Core/Application/Launch/DevLaunchSettings.cs:24`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Launch/DevLaunchSettings.cs#L24)

**Says:** "| `*Result`, `*Settings`, `*Progress` | Data, in `Models/` | `AiracServiceResult` |" vs `public static class DevLaunchSettings` (Read/Save of DevLaunch.json)

**Problem:** It is the only `*Result`/`*Settings`/`*Progress` type outside a Models/ folder, and it does work. The house rule and the code disagree.

**Proposed:** Rename the class to `DevLaunchFile` (it reads and writes `DevLaunch.json`), along with its uses in App.xaml.cs, DevLaunchCardViewModel, tests, and Architecture.md:52 and Code-Structure.md:93. Or add "(`DevLaunchSettings`, which reads and writes a file, is the one exception)" to the table.

<a id="d1b-9"></a>

### D1b-9 · Stale doc · Low · Code-Structure's folder maps miss some major types

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Code-Structure.md:54-59`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Code-Structure.md?plain=1#L54-L59), 73-74, 79-81, 95, 130-137

**Says:** "Airac/  AiracService (the entry point), AiracCycleDataCache, ... DuplicateAliasReport, OutputFileNamesParser, FebProperties"; "Shell/  app-wide services: Toast, Links, BrowserLauncher, DefaultRoiStore, OutputPreferences, AppRestart, DevLaunchArgs"

**Problem:** Every listed type exists where the map says. Some major types are missing, though: Application/Airac `DuplicateAliasChoices` (Architecture's run diagram names it) and `EchoCards`; Application/Settings `SubServiceArea`; Infrastructure/Configuration `UserConfigAreas` (the migration step), `UserConfigMigration`, `UserConfigMigrationContext`; Infrastructure/Credentials `ICredentialVault` (what tests swap) and `CredentialKindNames`; Infrastructure/Telephony `GngAirlineParser` (half of the "GNG + VATSIM-Radar" list the line names). In Wpf: Shell's window-placement feature (`MainWindowPlacement`, `WindowPlacementRules`, `NativeMonitors`, `WindowPlacementModels`, `ToastItem`), `Controls/FilterOption`, `Map/FrozenBrush` and `Map/Models/`.

**Proposed:** Add them. Airac line: "... DuplicateAliasReport, DuplicateAliasChoices, OutputFileNamesParser, FebProperties, EchoCards". Credentials: "CredentialStore, ICredentialVault, WindowsCredentialVault, CredentialHosts, CredentialKindNames, UrlSecrets". Telephony: "(TelephonyDownloader, TelephonyHtmlParser, GngAirlineParser, VatsimRadarAirlineParser)". Configuration: "... the settings layout (UserConfigVersion, UserConfigMigrations and their steps, such as UserConfigAreas)". Settings: "SubServiceSettingsReader, SubServiceArea, CrcDefaultsReader, SettingsValueReader". Shell: "Toast, Links, BrowserLauncher, DefaultRoiStore, OutputPreferences, AppRestart, DevLaunchArgs, and the main window's placement (MainWindowPlacement, WindowPlacementRules, NativeMonitors)". Map: "GeoJsonReader, WebMercator, ProjectedLayer, AiracMapLayers, BaseMap; Models/ for the map's records".

<a id="w5-22"></a>

### W5-22 · Stale doc · Low · Code-Structure's folder map leaves out Views/Models

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Code-Structure.md:146-156`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Code-Structure.md?plain=1#L146-L156)

**Says:** `└── Views/ ... └── Cards/ ...`

**Problem:** `FeBuddy.Wpf/Views/Models/ConfirmChoice.cs` exists, and AiracServiceViewModel uses it.

**Proposed:** add `└── Models/   ConfirmChoice (which button closed a ConfirmWindow)` under Views/.

<a id="w7-26"></a>

### W7-26 · Stale doc · Low · Code-Structure's Shell/ list misses the window-placement files

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Code-Structure.md:136-137`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Code-Structure.md?plain=1#L136-L137)

**Says:** `Shell/  app-wide services: Toast, Links, BrowserLauncher, DefaultRoiStore, OutputPreferences, AppRestart, DevLaunchArgs`

**Problem:** Shell/ also holds MainWindowPlacement, WindowPlacementRules, NativeMonitors, WindowPlacementModels (PixelRect, MonitorArea, WindowPlacementPlan, WindowSizes, SavedWindowPlacement) and ToastItem / Models/ToastKind.

**Proposed:** `Shell/  app-wide services: Toast, Links, BrowserLauncher, DefaultRoiStore, OutputPreferences, AppRestart, DevLaunchArgs, and the main window's placement (MainWindowPlacement, WindowPlacementRules, NativeMonitors)`

<a id="n1-1"></a>

### N1-1 · Stale doc · Low · The developer README describes the custom-actions project without its new launch action

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/README.md:31`](https://github.com/Nikolai558/FE-BUDDY/blob/e15a634aa6915245e558875cd249bccdb8455480/docs/Developers/README.md?plain=1#L31)

**Says:** `| FeBuddy.Installer.CustomActions | The MSI's code: enforce the upgrade rule, and remove saved credentials on uninstall (net472). |`

**Problem:** Since [#377](https://github.com/Nikolai558/FE-BUDDY/pull/377) the project also holds `LaunchFeBuddy` ([`LaunchActions.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/e15a634aa6915245e558875cd249bccdb8455480/FeBuddy/FeBuddy.Installer.CustomActions/LaunchActions.cs)), the stock dialogs' "Launch FE-BUDDY", and compiles in `UnelevatedLauncher.cs` from FeBuddy.Installer.UI.

**Proposed:** `The MSI's code: enforce the upgrade rule, remove saved credentials on uninstall, and open FE-Buddy as the signed-in user from the stock dialogs' Finish (net472).`

<a id="d1b-15"></a>

### D1b-15 · Minor · Low · RELEASING-QuickRef.md: wrong snippet under NEWS.md, wrong branch and version casing, typos, and not linked from anywhere

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/RELEASING-QuickRef.md:3`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/RELEASING-QuickRef.md?plain=1#L3), 19, 25, 32, 46, 54, 58-67, 93-94, 99-102, 108-114, 120-122, 143-146

**Says:** "##### NEWS.md / Update the news as needed / ```## Unreleased --- ## `<full version name>` ```"; "Base = `Releases` / Compare = `v3-Development`"; "Example: `Releasing v3.0.0-Beta.9`"; "Check .msi is in ASSESTS"

**Problem:** (a) Lines 58-67: the ChangeLog snippet sits under "NEWS.md". It belongs under "CHANGE LOG.md" (lines 48-56). (b) Branch names are `v3-development` and `releases`, all lower case (lines 32, 46, 109-110). Pre-release labels are lower case too (`-beta.9`): ReleaseStep only accepts `alpha|beta|rc`. (c) In the ChangeLog snippets (25, 66), the backticks in "## `<full version name>`" read as part of the heading. A literal "## `3.0.0`" fails the pre-flight's section match (Get-ChangeLogSections compares the raw heading text). (d) "UNRELEASED tab" should be "section" (19, 54). Other typos: "ASSESTS" (145), "If failed tested" (100). (e) The 4-space-indented lines after a paragraph (16, 93-94, 99-100, 114, 120-122, 131, 140-146) are joined onto the line above when rendered. (f) Line 143 doesn't say to fix ChangeLog.md after editing the notes, which RELEASING.md:127 requires. (g) Line 3 names the main guide as code instead of a link, and nothing links to this page (not docs/README.md, not Developers/README.md "Where next").

**Proposed:** Move the snippet under CHANGE LOG.md, and under NEWS.md write "Add a post at the top (see the format comment at the top of News.md)." Use `v3-development`, `releases` and `Releasing v3.0.0-beta.9`. Write the heading as `## <full version name>` with no backticks. Replace "tab" with "section". Fix "ASSETS" and "If a test failed". Turn each indented block into a bullet list. Line 143: "Edit the release notes if needed (leave the SHA-256 line alone), then make ChangeLog.md match." Line 3: "For the detailed version, see `[Releasing](RELEASING.md)`." Add a link from RELEASING.md's intro: "Short checklist: `[Release quick reference](RELEASING-QuickRef.md)`."

<a id="d1b-12"></a>

### D1b-12 · Stale doc · Low · Release-day staleness in RELEASING.md and VERSIONING.md

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/RELEASING.md:142-144`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/RELEASING.md?plain=1#L142-L144); [`docs/Developers/VERSIONING.md:144-145`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/VERSIONING.md?plain=1#L144-L145)

**Says:** "from the first stable 3.x release on, ... is always the latest **stable** release. Until then GitHub's Latest is 2.9.3, which has no such file." / "(Alpha and Beta now get `3.0.0-beta.N`)"

**Problem:** Both are true only until 3.0.0 is published, which is the release this review is for.

**Proposed:** RELEASING.md: "The installer is always `FE-BUDDY-Setup.msi`, so `https://github.com/Nikolai558/FE-BUDDY/releases/latest/download/FE-BUDDY-Setup.msi` is always the latest **stable** release." VERSIONING.md: "- **Channel** - from the tag, so 2.x users get the 3.x pre-releases their channel takes, and Stable users only stable releases, which outrank any later `2.9.x`."

<a id="d1b-13"></a>

### D1b-13 · History wording · Low · Figure 3's caption says the screenshot is from an older format

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/RELEASING.md:196-201`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/RELEASING.md?plain=1#L196-L201)

**Says:** "From before a `---` also went under `## Unreleased` and the `###` headings were added; follow the example in step 3."

**Problem:** This narrates history to excuse an out-of-date screenshot. 03-changelog-diff.png has no `---` under `## Unreleased` and no `###` heading, so it contradicts step 3.

**Proposed:** Retake 03-changelog-diff.png from a release that follows step 3, and delete the caption line. If you don't retake it, reword: "The screenshot has no `---` under `## Unreleased` and no `###` headings; follow the example in step 3."

<a id="d1b-14"></a>

### D1b-14 · Minor · Low · RELEASING.md: branch-protection paragraph contradicts itself; "What does what" and the check table are incomplete

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/RELEASING.md:96`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/RELEASING.md?plain=1#L96), 163-168, 172-181

**Says:** "The one exception is the installer-counter commit ... with `RELEASE_COUNTER_PAT`. Nobody can push, force-push, delete or re-create them. Only the admins can create, move or delete a tag. Other" (line 167 is one unwrapped long line); "| Build, Test, CodeQL | The same as every pull request. |"

**Problem:** "Nobody can push ... them" straight after describing a push that is allowed, and "them" now points at the commit, not the branches. `.github/scripts/ReleaseCommon.ps1`, which the other three scripts dot-source, is missing from the table. CI's Installer job also runs on pull requests into `releases` (ci.yml:29-30, 129-156), so the check table leaves it out.

**Proposed:** Line 167: "Nobody can force-push to, delete or re-create those branches. Only the admins can create, move or delete a tag." (Rewrap to the file's width.) Add a row: "| `.github/scripts/ReleaseCommon.ps1` | Shared helpers the release scripts dot-source: the version, ChangeLog.md's sections, the check summary. |". Line 96: "| Build, Test, Installer, CodeQL | The same as every pull request. |"

<a id="c2-31"></a>

### C2-31 · Stale doc · Low · Settings-Reference: `ExcludedDesignations` example `RN,SL` is not a designation FE-Buddy produces

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Settings-Reference.md:328`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Settings-Reference.md?plain=1#L328) (the same example is in [`FeBuddy/FeBuddy.Harness/HarnessSettings.cs:73`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/HarnessSettings.cs#L73), other scope)

**Says:** `| ExcludedDesignations | | list, e.g. RN,SL | none |`

**Problem:** Designations are the leading letters of the airway ID (AirwaySettings.ExcludedDesignations doc; AirwayClassifier.DeriveDesignation). `RN` is an `AWY_DESIGNATION` value, the route category the code deliberately ignores (AirwayClassifier remark: it "filed RNAV airways under RN"), so excluding `RN` excludes nothing.

**Proposed:** `list of designations (an airway ID's leading letters), e.g. A,G`.

<a id="c2-32"></a>

### C2-32 · Wrong doc · Low · Settings-Reference: Airways CRC keys for unwritten or excluded files

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Settings-Reference.md:232-235`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Settings-Reference.md?plain=1#L232-L235)

**Says:** `but ARTCC Boundaries, Fixes' and NAVAIDs' per-group layouts and Airways' Designation layout still require the Crc.* values of every listed key that isn't excluded.`

**Problem:** For Airways, nothing is "excluded" at this step. With `Designation`, any listed key of a kind (even `Airways_RN_Lines`, or one whose designation is in `ExcludedDesignations`) requires all three classes of that kind (AirwaySettingsParser.NeedsCrcDefaults, line 195). With `HighLow`, a listed `Airways_High_*` key is required even when no designation goes High, so that file can't be written.

**Proposed:** replace the Airways part with: `and Airways reads them whenever the kind is written: with Designation, any listed key of a kind needs all three classes of that kind; with HighLow, each listed High or Low key needs its class, whether or not any designation goes there.`

<a id="c3-15"></a>

### C3-15 · History wording · Low · Settings-Reference: "the key keeps its old name"

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Settings-Reference.md:492`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Settings-Reference.md?plain=1#L492)

**Says:** `Y / N - also write the virtual airline list (GNG + VATSIM-Radar; the key keeps its old name)`

**Problem:** Narrates the rename of the list. The key name itself must stay (renaming needs a layout migration).

**Proposed:** "`Y` / `N` - also write the virtual airline list (GNG + VATSIM-Radar)".

**Note:** Also found as [D1a-12](#d1a-12).

<a id="d1a-10"></a>

### D1a-10 · History wording · Low · Procedure lists: "saved before 3.0.0-beta.9"

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Settings-Reference.md:444-446`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Settings-Reference.md?plain=1#L444-L446); [`FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs:1143-1144`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs#L1143-L1144)

**Says:** "A full name matches the same way, so lists saved before 3.0.0-beta.9 still work; the tab reads them as root names and saves them so."

**Problem:** this narrates a beta change. The behaviour itself (a full name matches by its root, and the tab reduces a saved full name to its root) is current (ProceduresViewModel.cs:1146 `Select(ProcedureNaming.RootName)`). The code comment says the same ("an earlier FE-Buddy saved the full name").
- Fix (doc): "A full name matches the same way (`BRWNZ FOUR (RNAV)` is `BRWNZ`), and the tab reads a saved full name as its root name and saves it so." Code comment: "By root name: a full name (BRWNZ FOUR (RNAV)) reads as BRWNZ, and is saved as the root from then on without marking the page changed."

**Proposed:** 

<a id="d1a-11"></a>

### D1a-11 · History wording · Low · "the old `IncludeRoiAirports`", "before there was a choice", "never meant"

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Settings-Reference.md:203`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Settings-Reference.md?plain=1#L203), 204, 462-463; [`FeBuddy/FeBuddy.Core/Application/Settings/SubServiceSettingsReader.cs:49`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Settings/SubServiceSettingsReader.cs#L49); [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureSettingsParser.cs:185-188`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureSettingsParser.cs#L185-L188)

**Says:** "(Procedure Charts: the ROI only with the old `IncludeRoiAirports`, otherwise `None`)" / "A block without `Area` (one written by hand, or before there was a choice)" / "otherwise the ROI with the old `IncludeRoiAirports = Y`, otherwise `None`. `FilterByRoi` alone never meant the ROI here."

**Problem:** these narrate history. Settings blocks are built per run and never stored, the app always sends `Area` (GeojsonSubServiceViewModel.cs:729), and so does every harness block (9 `"Area"` entries in HarnessSettings.cs). A block without `Area` therefore only comes from tests or by hand. The code comments' "(the harness, or one written before there was a choice)" is wrong about the harness too.
- Fix (doc): 203: "(Procedure Charts: the ROI if it has `IncludeRoiAirports = Y`, otherwise `None`)". 204: "A block without `Area` (one written by hand, as some tests are) means the same: ...". 462-463: "...otherwise the ROI with `IncludeRoiAirports = Y`, otherwise `None`. `FilterByRoi` alone doesn't select the ROI here." Code comments: replace "(the harness, or one written before there was a choice)" with "(one written by hand)", and in ProcedureSettingsParser.cs:187-188 replace "FilterByRoi alone never meant the ROI here: it was sent whenever a default ROI was set" with "FilterByRoi alone doesn't select the ROI here."

**Proposed:** 

**Note:** Goes with [D1](010_Decisions.md#d1) ([C4-12](130_Old-Version-Code.md#c4-12), [T1b-21](140_Unused-Code.md#t1b-21)).

<a id="d1a-13"></a>

### D1a-13 · History wording · Low · "as the profiles did" / "no migration step was needed" (duplicated)

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Settings-Reference.md:84-85`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Settings-Reference.md?plain=1#L84-L85), 634-635

**Says:** "A profile is not a layout change: each setting keeps its dotted path, so no migration step was needed." / "...needs only step 3, as the profiles did."

**Problem:** history narration, and lines 84-85 repeat the rule in 634-635.

**Proposed:** delete lines 84-85. Make 634-635: "Moving the settings file while every setting keeps its dotted path is not a layout change: it needs only step 3."

<a id="d1a-14"></a>

### D1a-14 · History wording · Low · Import: "beta.3 `UserConfig.json`"

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Settings-Reference.md:582`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Settings-Reference.md?plain=1#L582)

**Says:** "A plain profile or beta.3 `UserConfig.json` imports too."

**Problem:** any JSON object without a `format` header that has `General` or `Services` imports, whatever wrote it (UserConfigTransfer.cs:199-206). The beta name adds nothing.

**Proposed:** "A plain settings file with no header (a profile's `UserConfig.<Profile>.json`) imports too."

<a id="d1a-18"></a>

### D1a-18 · Minor · Low · Readability: duplication and placement

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Settings-Reference.md:87-178`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Settings-Reference.md?plain=1#L87-L178), 525-528, 556-563, 437, 451-461, 466-472

**Says:** "### General" ... "### Sub-service and conversion nodes" sit under "## Settings profiles"

**Problem:** - (a) The saved-node subsections (General, Services.AiracService, DuplicateAliases, FileNames, MapService, node table) describe what is saved, not the profiles, yet are nested under "## Settings profiles", and Contents has no entry for them.
  - (b) Lines 525-526 and 527-528 both say `Combined_Alias.txt` then "holds FE-Buddy's aliases only".
  - (c) The ERAM table (556-563) and the conversions' "Own keys" table have no "Saved as" column, against the convention stated at lines 13-14 (every key is saved under the same name).
  - (d) The Procedure Charts section is 64 lines. The route-matching detail at 451-461 and the field semantics at 466-472 repeat what the cited XML docs hold.
  - (e) "(Feature [#367](https://github.com/Nikolai558/FE-BUDDY/issues/367))" and "(Feature [#371](https://github.com/Nikolai558/FE-BUDDY/issues/371))" (437, 458) tell the reader nothing.

**Proposed:** - (a) Move lines 87-178 under a new "## Saved settings outside the tabs" heading after "Settings profiles", and add it to Contents.
  - (b) Delete "and `Combined_Alias.txt` holds FE-Buddy's aliases only" from 525-526.
  - (c) Add under the ERAM heading: "Saved under the same names, as is `CroppingDistance`."
  - (d) Cut 453-461 after "...is the facility" to "The rest of the rule is in the XML docs of `RoutePointArtccResolver`, `ProceduresWithFixInFacility`, `NasrProcedureRoutes` and `ProcedureSelection`."
  - (e) Drop both feature numbers.
  - Length is otherwise right for a developer reference.

<a id="d1a-3"></a>

### D1a-3 · Wrong doc · Low · "keys read in more than one place are constants in UserConfigKeys" is not true for the facility and the default ROI

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Settings-Reference.md:48-49`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Settings-Reference.md?plain=1#L48-L49), 618-619

**Says:** "keys read in more than one place are constants in `UserConfigKeys`"

**Problem:** `Services.AiracService.UserArtccId` is read by ProceduresViewModel (5 places), AiracServiceViewModel:549, SubServiceSettingsViewModel:265, ImportSettingsViewModel:252 and UserConfigAreas, but it is `SettingsViewModel.ArtccKey` (SettingsViewModel.cs:84), duplicated as `UserConfigAreas.FacilityKey` (UserConfigAreas.cs:42). `DefaultRoi.FilterByRoi` is both `DefaultRoiStore.FilterKey` (DefaultRoiStore.cs:23) and `UserConfigAreas.DefaultRoiKey` (UserConfigAreas.cs:45). Neither is in `UserConfigKeys`.

**Proposed:** preferred is a code fix that makes the doc true: add `UserArtccId` and the five `DefaultRoi.*` keys to `UserConfigKeys`, and have `SettingsViewModel.ArtccKey`, `UserConfigAreas.FacilityKey`/`DefaultRoiKey` and `DefaultRoiStore`'s keys use them. Otherwise reword line 49 to: "most keys read in more than one place are constants in `UserConfigKeys` (the facility is `SettingsViewModel.ArtccKey`, the default ROI's are `DefaultRoiStore`'s)".

**Note:** See also [C6-13](200_Comments-Core.md#c6-13) and [W3-10](120_Logic-In-GUI.md#w3-10).

<a id="d1a-4"></a>

### D1a-4 · Wrong doc · Low · Procedure Charts and Telephony don't read `CoordinatePrecision`, `FebProperties` or `CrcDefaultsFor`, though the app sends them

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Settings-Reference.md:182-183`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Settings-Reference.md?plain=1#L182-L183), 421-423, 494-495

**Says:** "Read by `SubServiceSettingsReader`; saved by `GeojsonSubServiceViewModel`. Add Custom Aliases reads none of them." / "Procedure Charts writes no GeoJSON, so it has no `Emit…`, `FebProperties`, `CrcDefaultsFor` or `Crc.*` keys"

**Problem:** line 182 implies every other sub-service reads the shared keys. `GeojsonSubServiceViewModel.AddSharedSettings` (:696-715) sends `CoordinatePrecision`, `IncludeFebCustomProperties`, `FebProperties` and `CrcDefaultsFor` to Procedure Charts and Telephony too. Their parsers accept these as `CommonKeys` and never read them: ProcedureSettingsParser.cs:90-182 reads only `OutputDirectory`, `Area`, the ROI keys and `IncludeFebCustomProperties` (to warn), and TelephonySettingsParser.cs:72-109 only `OutputDirectory` and `IncludeFebCustomProperties`. Neither section mentions `CoordinatePrecision`.

**Proposed:** lines 182-183: "Read by `SubServiceSettingsReader`; saved by `GeojsonSubServiceViewModel`. Of these, Procedure Charts reads only `OutputDirectory`, `Area` and the ROI keys, Telephony only `OutputDirectory`, and Add Custom Aliases none. The app still sends the rest to Procedure Charts and Telephony, and both accept and ignore them." Line 421: "...so it has no `Emit…`, `CoordinatePrecision`, `FebProperties`, `CrcDefaultsFor` or `Crc.*` keys..." Line 494: "No `CoordinatePrecision`, `FebProperties`, `CrcDefaultsFor` or `Crc.*`..."

<a id="d1a-5"></a>

### D1a-5 · Wrong doc · Low · MapService: the base-map settings are saved by BaseMapSettings, and the opacity is saved only after the slider settles

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Settings-Reference.md:150`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Settings-Reference.md?plain=1#L150)

**Says:** "Saved by the Map (`MapLayersState`) the moment a choice is made, with no Save button."

**Problem:** `BaseMapLayers`, `BaseMapOpacity` and `Gridlines` are written by `BaseMapSettings` (BaseMapSettings.cs:93-94, 128-129, 136-137), which `MapLayersState` owns (:93). The opacity is saved by a 400 ms `DispatcherTimer` once a slider drag settles (:43, :66-67).

**Proposed:** "Saved by the Map as each choice is made, with no Save button: `MapLayersState` saves the files, live layers and home view, and its `BaseMapSettings` saves the base map and gridlines (the opacity once the slider has been still for 0.4 s)."

<a id="d1a-6"></a>

### D1a-6 · Wrong doc · Low · `HelpCommandName` is sent only while `IncludeHelpCommand` is also `Y`, and `RenameHelpCommand` has no row

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Settings-Reference.md:431`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Settings-Reference.md?plain=1#L431)

**Says:** "| `HelpCommandName` | sent only while `RenameHelpCommand` (saved, not sent) is `Y` | the help command's name, ..."

**Problem:** ProceduresViewModel.cs:996 sends it only when `IncludeHelpCommand && RenameHelpCommand`. `RenameHelpCommand` is a saved-only key (Set :1213, `GetBool("RenameHelpCommand", false)` :1178) and is crammed into the "Saved as" cell. Elsewhere the page gives such keys their own "-" row (the conversions' `SourceType`, line 544).

**Proposed:** replace the row with two rows:
  `| `HelpCommandName` | | the help command's name, letters and numbers only; a dot in front is dropped. Sent only while `IncludeHelpCommand` and `RenameHelpCommand` are `Y` | `helpCHARTRECALL` |`
  `| - | `RenameHelpCommand` | `Y` / `N` - use `HelpCommandName` rather than the default name; saved, not sent | `N` |`

<a id="d1a-7"></a>

### D1a-7 · Wrong doc · Low · NAVAIDs: missing that the merged Symbols file's `style` isn't read while styled by type

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Settings-Reference.md:372-381`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Settings-Reference.md?plain=1#L372-L381)

**Says:** (absent) the section only covers `FanMarkerStyle`.

**Problem:** with `OutputBy = All`, `Crc.NAVAIDs.Symbol.style` is read only when `SymbolStyleBy = File` (NavaidSettingsParser.cs:135-136, `readStyle: symbolStyleBy == NavaidSymbolStyleBy.File`). The tab doesn't send it otherwise (`CrcDefaultsRowIo.AddToSettingsBlock` :108 `AsksForStyle`). This is the one CRC field a "required" default can leave out, and the CRC defaults section (lines 276-302) doesn't mention it.

**Proposed:** add a bullet: "With `OutputBy = All` and `SymbolStyleBy = Type`, the merged Symbols file's defaults carry no style: `Crc.NAVAIDs.Symbol.style` isn't read (or sent), and each NAVAID is styled by its type (`FanMarkerStyle` for fan markers)."

<a id="d1a-8"></a>

### D1a-8 · Wrong doc · Low · "Adding a setting" step 3 doesn't fit Add Custom Aliases, SCT2, the numbered lists, or the harness

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/Settings-Reference.md:620-624`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Settings-Reference.md?plain=1#L620-L624)

**Says:** "add it to the parser's `OwnKeys` (or `SubServiceSettingsReader.CommonKeys`, or `ConversionSettingsReader.ConversionKeys`), or it is reported as unknown. Add it to `HarnessSettings.cs` and a parser test."

**Problem:** `ConcatenateAliasesSettingsParser` has no `OwnKeys`. It warns on anything but `AddCustomAliasFiles`, `OutputDirectory` and `Sources.<n>.*` (:88-115), and a new `Sources` field also needs `ParseSource`'s field list (:154-160). `TelephonySettingsParser`'s `VirtualAirlines.<n>.*` fields are checked the same way (:195-201). `SctToGeojsonSettingsParser` passes `ConversionKeys` directly and has no `OwnKeys` (:50). The harness has no Add Custom Aliases, file-conversion or File Names block (grep of HarnessSettings.cs).

**Proposed:** "...and add it to the parser's `OwnKeys` (or `SubServiceSettingsReader.CommonKeys`, or `ConversionSettingsReader.ConversionKeys`), or it is reported as unknown. SCT2 to GeoJSON has no `OwnKeys` of its own. Add Custom Aliases, and the fields of a numbered `Sources.<n>` or `VirtualAirlines.<n>` entry, are checked by their parser's own field lists. Add it to `HarnessSettings.cs` where the harness has a block for that sub-service, and to a parser test."

<a id="d1b-16"></a>

### D1b-16 · Stale doc · Low · TODO.md: Harness item still open; the "tested today" list misses newer Wpf tests

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/TODO.md:7-8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/TODO.md?plain=1#L7-L8), 12-22

**Says:** "Tested today (`FeBuddy.UnitTests/Wpf`): the map's logic and `MapCanvas`, ... and the Reset, Uninstall and update windows."

**Problem:** Item 1 is still open: FeBuddy.Harness.csproj has no `<GenerateDocumentationFile>`, and HarnessSettings.cs:71 still says "Phase 3.3-3.7 settings" (line 73 also ends in "(3.3)"). Item 2's list is missing tests that now exist: `Shell/DevLaunchArgsTests` and `ViewModels/DevLaunchCardViewModelTests` (dev launch and Settings' Developer card), `Shell/MainWindowPlacementTests` and `Shell/WindowPlacementRulesTests` (Settings ▸ Main Window and window placement), `ViewModels/WelcomeViewModelTests` (the welcome window), and `ViewModels/UserConfigLayoutTests` (every settings page reads and saves every released settings layout). Untested with no test reference at all: `CredentialsViewModel`, `CredentialEditorViewModel`, `DashboardViewModel`, `DatToGeojsonViewModel`. The Airports, Departures, Arrivals, NAVAIDs, ARTCC Boundaries, Fixes, Wx Stations and SCT2 tabs have no tests of their own.

**Proposed:** Item 1: add "and the "(3.3)" on `ExcludedDesignations`". Item 2's list: after "the Alias Command Guide," add "the welcome window, the dev launch and Settings' Developer card, the main window's placement (Settings ▸ Main Window), every saved settings layout through every settings page,". Replace "The rest of the view-model logic - the other tabs' settings blocks, dirty tracking, validation - could be tested the same way" with: "Not yet tested: the Credentials page and editor, the Dashboard, the DAT to GeoJSON and SCT2 to GeoJSON pages, and the Airports, Departures, Arrivals, NAVAIDs, ARTCC Boundaries, Fixes and Wx Stations tabs' settings blocks, dirty tracking and validation. They can be tested the same way, without a window."

**Note:** Part of [D6](010_Decisions.md#d6) (Harness item) - and add a TODO for [D7](010_Decisions.md#d7).

<a id="o1-36"></a>

### O1-36 · Wrong doc · Low · VERSIONING.md: the version check keeps only "the user's channel"

**Status:** Open · **Session:** [S11](020_Work-Plan.md#s11)

**Where:** [`docs/Developers/VERSIONING.md:75-77`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/VERSIONING.md?plain=1#L75-L77)

**Says:** `parses each tag as strict SemVer (a leading v is fine; anything else is skipped), keeps those on the user's channel, and compares by SemVer`

**Problem:** VersionCheck keeps every release on the user's channel or a more stable one (VersionCheck.cs:103 skips only `parsed.Channel < channel`); the same file's channel section (lines 43-45) and Architecture.md:312-313 say so.

**Proposed:** `... keeps those on the user's channel or a more stable one, and compares by SemVer (ProductVersion).`

## docs/Developers/Reference

<a id="d1b-1"></a>

### D1b-1 · Unused · High · Remove CRC's AliasParser.cs from the repository before the public release

**Status:** Agreed, changed: delete it because nothing needs it any more. The CRC developer gave permission to host it, so it is not a licensing problem, and git history is left alone ([D2](010_Decisions.md#d2), [Q10](010_Decisions.md#q10)) · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`docs/Developers/Reference/AliasParser.cs:1-554`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Reference/AliasParser.cs#L1-L554); [`docs/Developers/Reference/README.md:1-15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Reference/README.md?plain=1#L1-L15); [`docs/README.md:25`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/README.md?plain=1#L25)

**Problem:** This is CRC's own alias parser (the closed-source vNAS client, `Vatsim.Nas.Crc.*` namespaces, its EventBus, session manager, METAR and flight-plan repositories) - not FE-Buddy code and not 2.x code. It has no licence header, and CRC is not open source, so publishing it in this GPL-3 repository redistributes someone else's code without permission, and LICENSE.md looks as if it covers it. Nothing uses it: no project compiles it (SDK projects glob only their own folders under FeBuddy/), no code or doc refers to it except the docs/README.md index row, and FE-Buddy uses none of CRC's dynamic variables (`$metar`, `$squawk`, ... appear nowhere else in the repo). The parser facts FE-Buddy relies on are already written down where they are used: AirportAliasWriter.cs:19-31 (tokenised and rejoined with single spaces, so `\t`/`\n` escapes), DepartureAliasWriter.cs:20 and ArrivalAliasWriter.cs:20 (`\.\w+` command names), Architecture.md:198 and 295-296 (last copy of a command wins; whitespace collapses). It is also an undated snapshot of a moving target.

**Proposed:** Delete `docs/Developers/Reference/AliasParser.cs` and `docs/Developers/Reference/README.md` (the folder goes with them), and delete the `| [Reference](Developers/Reference/README.md) | CRC's alias parser, kept for reading. |` row from docs/README.md:25. Keep a private copy outside the repository if it is still wanted for reading. It was added in commit 7928da7 (2026-09-22); if that commit has already been pushed to the public repo, decide whether to ask CRC's author for permission or rewrite history - deleting it from the branch alone leaves it in the public history.

**Future use:** unlikely - a fresh copy can be read from CRC whenever needed, and FE-Buddy's own comments already record the behaviour it depends on.

**Note:** **Kyle:** delete it ([D2](010_Decisions.md#d2)). Whether to purge it from git history is a separate call.

## Reported more than once

Each review slice was read by its own reviewer, so some findings were reported twice. These IDs point at the finding that is kept.

- <a id="d1a-9"></a>**D1a-9** · Layout table: layout 1 was also saved stamped, by beta.4 and beta.5 · same as [C6-8](200_Comments-Core.md#c6-8).
- <a id="d1a-12"></a>**D1a-12** · Telephony: "the key keeps its old name" · same as [C3-15](#c3-15).
- <a id="d1b-18"></a>**D1b-18** · DEV-CleanBuild.bat doesn't empty "every project's bin" · same as [O1-42](100_Bugs.md#o1-42).
