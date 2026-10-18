# 400 · Installer, Harness, Versioning, CI and build scripts

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

The installer, the setup window, the custom actions, the Harness ([D6](010_Decisions.md#d6)), Versioning, the CI workflows and the build scripts. Grouped by folder. Session [S10](020_Work-Plan.md#s10).

**55 findings** (1 Med, 54 Low). Quote the ID when you comment on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374).

## Summary

| ID | Kind | Sev | Finding | Status | Session |
|---|---|---|---|---|---|
| [O1-41](#o1-41) | Minor | Low | .gitignore: stock template; FE-Buddy's own outputs are ignored only by accident | Open | S10 |
| [O1-43](#o1-43) | Wrong doc | Low | build.ps1 step 4 omits the setup window project | Open | S10 |
| [O1-3](#o1-3) | Unused | Low | FeBuddy/test.md is an empty scratch file | Open | S10 |
| [O1-44](#o1-44) | Minor | Low | PR template: where a change-log line goes | Open | S10 |
| [O1-40](#o1-40) | Minor | Low | Invisible BOM character inside a regex | Open | S10 |
| [D1b-23](#d1b-23) | Minor | Low | Release scripts' own "run it yourself" lines omit -ExecutionPolicy Bypass | Open | S10 |
| [O1-37](#o1-37) | Wrong doc | Low | Test-ReleaseReadiness: skipped step "reported, as a warning" | Open | S10 |
| [O1-38](#o1-38) | Stale doc | Low | Coverage comments: Core only, and "the map logic" | Open | S10 |
| [O1-39](#o1-39) | Minor | Low | Release PRs build the MSI twice | Open | S10 |
| [O1-4](#o1-4) | Unused | Med | Harness runners are switched by commented-out code that no longer compiles when uncommented | Deferred | After 3.0 |
| [O1-13](#o1-13) | Unused | Low | Stopwatches started and never read in three runners | Deferred | After 3.0 |
| [O1-15](#o1-15) | Unused | Low | AirwayAliasRunner / AliasOnlySettings duplicate what the unit tests cover | Deferred | After 3.0 |
| [O1-10](#o1-10) | Stale doc | Low | Harness class summaries: "airway-related output", "all console formatting", "every toggle" | Deferred | After 3.0 |
| [O1-11](#o1-11) | Wrong doc | Low | ConsoleReport printer docs don't match the printing | Deferred | After 3.0 |
| [O1-14](#o1-14) | Unused | Low | Harness references NetTopologySuite packages it never uses | Deferred | After 3.0 |
| [O1-17](#o1-17) | Minor | Low | Harness hard-codes one developer's machine paths and literal file keys | Deferred | After 3.0 |
| [O1-5](#o1-5) | History wording | Low | Planning-doc phase numbers in HarnessSettings (the TODO.md item) | Deferred | After 3.0 |
| [O1-6](#o1-6) | Wrong doc | Low | "omit any of these and the parser uses the same default" is wrong for the designation lists | Deferred | After 3.0 |
| [O1-7](#o1-7) | History wording | Low | History/planning phrases in HarnessSettings | Deferred | After 3.0 |
| [O1-8](#o1-8) | Wrong doc | Low | HarnessSettings summaries say every key is "listed with its default value" | Deferred | After 3.0 |
| [O1-9](#o1-9) | Wrong doc | Low | ARTCC Boundaries "HighLowUnlimited adds a third, Unlimited-only file" | Deferred | After 3.0 |
| [O1-16](#o1-16) | Minor | Low | Harness members without XML docs (the TODO.md standard) | Deferred | After 3.0 |
| [O1-12](#o1-12) | Wrong doc | Low | WxStationRunner: "Unlike every other runner" | Deferred | After 3.0 |
| [O1-20](#o1-20) | History wording | Low | "This is what our previous attempt was missing." | Open | S10 |
| [O1-22](#o1-22) | Wrong doc | Low | Get-InstallerVersion says it counts releases; it counts every build | Open | S10 |
| [O1-19](#o1-19) | Unused | Low | Unused localization string DowngradeError | Open | S10 |
| [O1-32](#o1-32) | Minor | Low | Small inaccuracies in installer comments and one awkward sentence | Open | S10 |
| [N1-2](#n1-2) | Minor | Low | Two comments edited in #377 were not re-wrapped | Open | S10 |
| [O1-21](#o1-21) | Stale doc | Low | CustomActions project described as the version-policy action only | Partly fixed | S10 |
| [O1-31](#o1-31) | History wording | Low | "unchanged" / "did" in the installer UI's comments | Open | S10 |
| [O1-26](#o1-26) | Unused | Low | ProgressTracker.Complete() is never called | Open | S10 |
| [O1-29](#o1-29) | Unused | Low | Card.Accent style is never used | Open | S10 |
| [O1-27](#o1-27) | Unused | Low | SetupViewModel.IsFinished is never used | Open | S10 |
| [O1-28](#o1-28) | Unused | Low | PromptWindow.OnClosing override does nothing | Open | S10 |
| [O1-35](#o1-35) | Stale doc | Low | Versioning csproj lists only two consumers | Open | S10 |
| [O1-33](#o1-33) | Wrong doc | Low | UpdatePolicy "shared by ... the app" | Open | S10 |
| [O1-34](#o1-34) | Stale doc | Low | ReleaseChannel: "stored by name in UserConfig.json" | Open | S10 |
| [T2-25](#t2-25) | History wording | Low | Feature/issue numbers that only narrate history in test summaries | Open | S10 |
| [T2-21](#t2-21) | Stale doc | Low | "With a second layout" stand-in steps read as if FE-Buddy had only one layout | Open | S10 |
| [T2-33](#t2-33) | Minor | Low | Class summaries that leave out half of what the class tests | Open | S10 |
| [T2-13](#t2-13) | Wrong doc | Low | UserConfigMigrationsTests: "every edit can run twice without harm", but its ChangeValue lambda can't | Open | S10 |
| [T2-22](#t2-22) | Stale doc | Low | "UserConfig.json" where the file is now a profile's `UserConfig.<Profile>.json` | Open | S10 |
| [T2-31](#t2-31) | Unused | Low | Two near-meaningless tests | Open | S10 |
| [T2-27](#t2-27) | Unused | Low | The local HTTP test server is copied into five classes; one copy returns a task nobody uses | Open | S10 |
| [T2-24](#t2-24) | Stale doc | Low | DtppMetafileXmlParserTests refers to "the example file", which is not in the repo | Open | S10 |
| [T2-32](#t2-32) | Minor | Low | AppDataResetTests "fills it the way FE-Buddy would" with names FE-Buddy doesn't use | Open | S10 |
| [T2-17](#t2-17) | Wrong doc | Low | The "AppLog" collection's summary covers only AppLog | Open | S10 |
| [T2-26](#t2-26) | History wording | Low | MarkdownParserTests: a test named after the 2.9.0 release notes | Open | S10 |
| [T2-28](#t2-28) | Unused | Low | Duplicate TempWorkspace test inside NasrCycleDownloaderTests | Open | S10 |
| [T2-34](#t2-34) | Minor | Low | Typo in NasrCycleDownloaderTests summary | Open | S10 |
| [T2-19](#t2-19) | Wrong doc | Low | WaypointLocatorTests: test name claims more than it asserts | Open | S10 |
| [T2-16](#t2-16) | Wrong doc | Low | NasrCsvParserMappingTests: `main_async_*` names and a summary the test doesn't match | Open | S10 |
| [T2-18](#t2-18) | Wrong doc | Low | LegacySquirrelShortcutsTests says the shortcuts are 2.8.x's | Open | S10 |
| [T2-23](#t2-23) | Stale doc | Low | TelephonyFilesTests: "the two kept file names" | Open | S10 |
| [T2-29](#t2-29) | Unused | Low | `FindRepositoryRoot` is duplicated in both Repository tests | Open | S10 |

## (repository root)

<a id="o1-41"></a>

### O1-41 · Minor · Low · .gitignore: stock template; FE-Buddy's own outputs are ignored only by accident

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** .gitignore:4, 22-23, 43, 169-170

**Says:** `## Get latest from https://github.com/github/gitignore/blob/master/VisualStudio.gitignore` / `# Click-Once directory` / `publish/`

**Problem:** Nothing FE-Buddy-specific is stale, and no tracked file is ignored (`git ls-files -i -c --exclude-standard` is empty). But it is GitHub's VisualStudio template plus `*.ai`: build.ps1's `FeBuddy/publish/` is ignored by the "Click-Once directory" rule, `FeBuddy/releases/` by `[Rr]eleases/`, coverage's `FeBuddy/TestResults/` by the MSTest rule - trimming the template would silently un-ignore them. Most sections are for tools the repo has never used (Node.js, Python, Azure, SQL Server, BizTalk, LightSwitch, Paket, FAKE, Cake, Chutzpah, TFS, NCrunch, ...). The "Get latest" link points at the old `master` branch.

**Proposed:** add at the end `# FE-Buddy build output (build.ps1, coverage.ps1)` with `FeBuddy/publish/`, `FeBuddy/releases/`, `FeBuddy/TestResults/`; optionally drop the sections for unused tools; change `master` to `main`.

<a id="o1-43"></a>

### O1-43 · Wrong doc · Low · build.ps1 step 4 omits the setup window project

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/build.ps1:12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/build.ps1#L12)

**Says:** `4. Builds FeBuddy.Installer (and its custom action) with both numbers`

**Problem:** Building the wixproj also builds FeBuddy.Installer.UI (ProjectReference, FeBuddy.Installer.wixproj:52), the MSI's embedded setup window, and there are two custom actions.

**Proposed:** `4. Builds FeBuddy.Installer (with its custom actions and its setup window, FeBuddy.Installer.UI) with both numbers ...`

<a id="o1-3"></a>

### O1-3 · Unused · Low · FeBuddy/test.md is an empty scratch file

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/test.md`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/test.md) (0 bytes)

**Problem:** Confirmed empty and referenced nowhere. Note the history is slightly different from the brief: it was added in 23573f6 "Made a test.md to check docs during development" (2026-10-13). Commit 05c433a "test with new setup" (2026-10-14) only removed an empty `TempResources` solution folder from FeBuddy.sln (no remnants left).

**Proposed:** delete FeBuddy/test.md.

**Future use:** unlikely.

**Note:** Also found as [D1b-19](#d1b-19).

## .github

<a id="o1-44"></a>

### O1-44 · Minor · Low · PR template: where a change-log line goes

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`.github/pull_request_template.md:11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/.github/pull_request_template.md?plain=1#L11)

**Says:** `Users will notice this change? Add one line under "## Unreleased" in ChangeLog.md.`

**Problem:** ChangeLog.md's own rule (its header comment) and RELEASING.md put the bullet under the matching `### ` heading inside `## Unreleased`.

**Proposed:** `Users will notice this change? Add one line under "## Unreleased" in ChangeLog.md, under its "### " heading (see the comment at the top of that file).`

## .github/scripts

<a id="o1-40"></a>

### O1-40 · Minor · Low · Invisible BOM character inside a regex

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`.github/scripts/Push-InstallerCounter.ps1:35`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/.github/scripts/Push-InstallerCounter.ps1#L35)

**Says:** `-replace '^<U+FEFF>', ''` (bytes `' ^ EF BB BF '`)

**Problem:** The pattern holds a literal U+FEFF, invisible in every editor, and readable only where the script is decoded as UTF-8 (pwsh 7; Windows PowerShell 5.1 reads a BOM-less .ps1 as ANSI).

**Proposed:** `-replace '^\uFEFF', ''` (a .NET regex escape).

<a id="d1b-23"></a>

### D1b-23 · Minor · Low · Release scripts' own "run it yourself" lines omit -ExecutionPolicy Bypass

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`.github/scripts/Test-ReleaseReadiness.ps1:22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/.github/scripts/Test-ReleaseReadiness.ps1#L22); [`.github/scripts/Build-ReleaseAssets.ps1:19`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/.github/scripts/Build-ReleaseAssets.ps1#L19)

**Says:** "powershell -File .github\scripts\Test-ReleaseReadiness.ps1"

**Problem:** Under Windows' default execution policy, this fails to run an unsigned script. RELEASING.md:79 and build.cmd use `-ExecutionPolicy Bypass`.

**Proposed:** "powershell -ExecutionPolicy Bypass -File .github\scripts\Test-ReleaseReadiness.ps1" (and the same for Build-ReleaseAssets.ps1).

<a id="o1-37"></a>

### O1-37 · Wrong doc · Low · Test-ReleaseReadiness: skipped step "reported, as a warning"

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`.github/scripts/Test-ReleaseReadiness.ps1:26-28`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/.github/scripts/Test-ReleaseReadiness.ps1#L26-L28)

**Says:** `Do not require the one-step rule (still reported, as a warning).`

**Problem:** With -SkipVersionStep a non-step version is recorded as `Skip` (line 118: `Add-CheckResult ... 'Skip'`), printed `[SKIP]` and shown with `:heavy_minus_sign:` - there is no warning level.

**Proposed:** `Do not require the one-step rule (a version that isn't one step is still listed, as a skipped check).`

## .github/workflows

<a id="o1-38"></a>

### O1-38 · Stale doc · Low · Coverage comments: Core only, and "the map logic"

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`.github/workflows/ci.yml:115-116`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/.github/workflows/ci.yml#L115-L116); [`FeBuddy/coverlet.runsettings:7-8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/coverlet.runsettings#L7-L8)

**Says:** `fails if FeBuddy.Core coverage drops below the floor (line / branch percent).` / `The tests do reach the WPF shell's map logic (FeBuddy.UnitTests/Wpf), but it is not measured`

**Problem:** The gate measures FeBuddy.Core and FeBuddy.Versioning (coverlet Include `[FeBuddy.Core]*,[FeBuddy.Versioning]*`; ci.yml's own header line 5 says so). The tests now reach much more of the WPF app than the map (TODO.md lists view-models, controls, windows).

**Proposed:** ci.yml -> `fails if FeBuddy.Core and FeBuddy.Versioning coverage drops below the floor`; coverlet -> `The tests do reach parts of the WPF app (FeBuddy.UnitTests/Wpf), but it is not measured: most of it is XAML and view code that no unit test runs.`

**Note:** Also found as [D1b-22](#d1b-22).

<a id="o1-39"></a>

### O1-39 · Minor · Low · Release PRs build the MSI twice

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`.github/workflows/ci.yml:29-30`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/.github/workflows/ci.yml#L29-L30), 129-156; [`.github/workflows/release-checks.yml:79-84`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/.github/workflows/release-checks.yml#L79-L84)

**Says:** `pull_request: branches: [ v3-development, releases ]` (Installer job, no base-branch condition) / `run: ./.github/scripts/Build-ReleaseAssets.ps1 -PreviousVersion "$env:PREVIOUS_VERSION"`

**Problem:** On a pull request into releases, CI's Installer job runs build.ps1 and Release pre-flight runs it again through Build-ReleaseAssets.ps1 (and uploads the same MSI as "release-preview"). The two checks duplicate each other.

**Proposed:** optional - add `&& github.base_ref != 'releases'` to the Installer job's `if:` (ci.yml:133) and say so in the header comment; Release pre-flight already builds and uploads the MSI for those pull requests. Keep it if Installer is a required check on the releases ruleset.

## FeBuddy.Harness

<a id="o1-4"></a>

### O1-4 · Unused · Med · Harness runners are switched by commented-out code that no longer compiles when uncommented

**Status:** Deferred: the Harness stays, and its cleanup waits until after 3.0 ([D6](010_Decisions.md#d6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Harness/Program.cs:3-6`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/Program.cs#L3-L6), 43-68, 73-74; [`docs/Developers/README.md:114-115`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/README.md?plain=1#L114-L115)

**Problem:** Nine of the ten runners (AirwayGeojsonRunner, AirwayAliasRunner, AirportRunner, DepartureRunner, ArrivalRunner, NavaidRunner, ArtccBoundaryRunner, FixRunner, WxStationRunner, TelephonyRunner) and nine ConsoleReport printers (PrintAirway/Airport/Departure/Arrival/Navaid/ArtccBoundary/Fix/WxStation/TelephonyServiceResult) are reachable only by uncommenting lines, so every analyzer sees them as dead. Worse, the commented lines are not compile-checked: uncommenting the Airports, Departures, Arrivals, NAVAIDs, ARTCC Boundaries, Fixes or Wx Stations line fails with CS0246 because Program.cs has no `using` for `Airports.Models`, `Departures.Models`, etc. Worth replacing: yes - a simple switch keeps every runner compiled, refactor-safe and documented in one place.

**Proposed:** add to HarnessSettings `public static readonly HarnessRun[] Runs = [HarnessRun.Procedures];` with `internal enum HarnessRun { Airways, Airports, Departures, Arrivals, Navaids, ArtccBoundaries, Fixes, WxStations, Procedures, Telephony }`, and in Program.Main replace the commented block with `foreach (HarnessRun run in HarnessSettings.Runs) { switch (run) { case HarnessRun.Airways: ConsoleReport.PrintAirwayServiceResult(..., AirwayGeojsonRunner.Run(allNasrCsvData)); break; ... } }` (Telephony awaits `TelephonyRunner.RunAsync()`). Add the missing model usings. Update docs/Developers/README.md step 2 to "list the sub-services to run in `HarnessSettings.Runs`".

**Future use:** likely (the runners are the harness's purpose) - keep them, but make them reachable.

**Note:** **Kyle:** keep the Harness for big one-off testing, with a simple switch choosing which runs to do ([D6](010_Decisions.md#d6)).

<a id="o1-13"></a>

### O1-13 · Unused · Low · Stopwatches started and never read in three runners

**Status:** Deferred: the Harness stays, and its cleanup waits until after 3.0 ([D6](010_Decisions.md#d6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Harness/AirportRunner.cs:1`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/AirportRunner.cs#L1), 24, 28; [`FeBuddy/FeBuddy.Harness/AirwayAliasRunner.cs:1`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/AirwayAliasRunner.cs#L1), 17, 21; [`FeBuddy/FeBuddy.Harness/AirwayGeojsonRunner.cs:1`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/AirwayGeojsonRunner.cs#L1), 18, 22

**Problem:** The elapsed time is never used; each service already reports `result.Elapsed`, which ConsoleReport prints.

**Proposed:** delete the stopwatch lines and `using System.Diagnostics;`, and make each `Run` a one-line expression like the other runners (`=> AirportService.Run(allNasrCsvData, HarnessSettings.AirportSettings());`).

**Future use:** unlikely.

<a id="o1-15"></a>

### O1-15 · Unused · Low · AirwayAliasRunner / AliasOnlySettings duplicate what the unit tests cover

**Status:** Deferred: the Harness stays, and its cleanup waits until after 3.0 ([D6](010_Decisions.md#d6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Harness/AirwayAliasRunner.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/AirwayAliasRunner.cs) (whole file); [`FeBuddy/FeBuddy.Harness/HarnessSettings.cs:552-562`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/HarnessSettings.cs#L552-L562); [`FeBuddy/FeBuddy.Harness/Program.cs:46-47`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/Program.cs#L46-L47)

**Problem:** A one-off proof from development. AirwayServiceTests (lines 310, 330) and AirwaySettingsParserTests (83, 95) already run Airways with `GenerateGeojson` "N"; in the harness the same is one value in `AirwaySettings()`.

**Proposed:** delete AirwayAliasRunner.cs, `HarnessSettings.AliasOnlySettings` and Program.cs lines 46-47 (and the matching entry in the [O1-4](#o1-4) switch).

**Future use:** unlikely.

<a id="o1-10"></a>

### O1-10 · Stale doc · Low · Harness class summaries: "airway-related output", "all console formatting", "every toggle"

**Status:** Deferred: the Harness stays, and its cleanup waits until after 3.0 ([D6](010_Decisions.md#d6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Harness/ConsoleReport.cs:19-22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/ConsoleReport.cs#L19-L22); [`FeBuddy/FeBuddy.Harness/Program.cs:10-14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/Program.cs#L10-L14); [`FeBuddy/FeBuddy.Harness/HarnessSettings.cs:5-9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/HarnessSettings.cs#L5-L9)

**Says:** `<see cref="Program"/> and the runner classes never call <c>Console.Write*</c> directly for airway-related output.` / `all console formatting lives in <see cref="ConsoleReport"/>` / `Every path and toggle a developer needs to edit ... rather than hunting through <see cref="Program"/>`

**Problem:** ConsoleReport now prints every sub-service, not just airways; Program writes its own header, "Parsing NASR CSV data...", and the FATAL lines (Program.cs:22-27, 31, 40, 78-80); and which runners run is toggled in Program.cs, not HarnessSettings.

**Proposed:** ConsoleReport -> `All console formatting of a run's results. Program writes only the run header and a fatal error.`; Program -> `... all sub-service logic lives in FeBuddy.Core and the result reports in ConsoleReport.`; HarnessSettings stays as is once [O1-4](#o1-4) moves the run list into it (otherwise add "Which runners run is chosen in Program.Main.").

<a id="o1-11"></a>

### O1-11 · Wrong doc · Low · ConsoleReport printer docs don't match the printing

**Status:** Deferred: the Harness stays, and its cleanup waits until after 3.0 ([D6](010_Decisions.md#d6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Harness/ConsoleReport.cs:112-113`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/ConsoleReport.cs#L112-L113), 167-168, 433-436, 460-462

**Says:** `(a handful listed; every path with DevMode on - a full run writes thousands)` / `As with the airway warnings above, only the first few of any group print` / `Warnings and errors are the point of the list, so they always print.`

**Problem:** Departures/Arrivals print file names (`Path.GetFileName(file)`, lines 148, 203), not paths. PrintWarnings is below PrintMessagesByLevel, not above. Warnings and errors are capped at 3 per level unless DevMode (lines 472-476), so they do not "always print".

**Proposed:** 112-113/167-168 -> `(the first six by name; every one with DevMode on - a full run writes thousands)`; 435 -> `As with PrintWarnings, only the first few ...`; 460-462 -> `// Warnings and errors are listed (the first few, or all with DevMode); Info, Success and Debug stay behind their count unless developer mode asks for everything.`

<a id="o1-14"></a>

### O1-14 · Unused · Low · Harness references NetTopologySuite packages it never uses

**Status:** Deferred: the Harness stays, and its cleanup waits until after 3.0 ([D6](010_Decisions.md#d6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Harness/FeBuddy.Harness.csproj:12-13`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/FeBuddy.Harness.csproj#L12-L13)

**Problem:** No Harness file uses NTS; FeBuddy.Core references the same versions, so they flow in transitively anyway. Two more places to bump on every package update.

**Proposed:** delete both PackageReference lines.

**Future use:** unlikely.

<a id="o1-17"></a>

### O1-17 · Minor · Low · Harness hard-codes one developer's machine paths and literal file keys

**Status:** Deferred: the Harness stays, and its cleanup waits until after 3.0 ([D6](010_Decisions.md#d6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Harness/HarnessSettings.cs:13`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/HarnessSettings.cs#L13), 16, 19, 27, 33-35, 137, 209, 272, 322, 370, 417

**Says:** `@"C:\Users\ksand\Downloads\03_Sep_2026_CSV"` / `{ "CrcDefaultsFor", "Runways_Lines,Airports_Symbols,Airports_Text" }`

**Problem:** In a public repository the paths name one person's user folder and fail for anyone else at the first parse. The `CrcDefaultsFor` lists are string literals while the constants exist (AirportOutputFiles.AirportsSymbols, DepartureOutputFiles.Lines, NavaidOutputFiles.Symbols...; WxStationSettings already uses WxStationOutputFiles.Symbols), so a renamed key fails only at run time.

**Proposed:** read the four paths from environment variables with a placeholder fallback, e.g. `Environment.GetEnvironmentVariable("FEBUDDY_HARNESS_NASR") ?? @"C:\path\to\unzipped\NASR_CSV"` (static readonly instead of const), and build the CrcDefaultsFor lists from the *OutputFiles constants.

<a id="o1-5"></a>

### O1-5 · History wording · Low · Planning-doc phase numbers in HarnessSettings (the TODO.md item)

**Status:** Deferred: the Harness stays, and its cleanup waits until after 3.0 ([D6](010_Decisions.md#d6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Harness/HarnessSettings.cs:71-73`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/HarnessSettings.cs#L71-L73), 81, 84, 85

**Says:** `// Phase 3.3-3.7 settings. Defaults shown; omit any of these and the parser uses` / `// e.g. "RN,SL" to drop those designations entirely (3.3)` / `// per-kind output opt-out (3.4)` / `// "All" or "RoiAirways" (3.5)` / `// max decimal places in GeoJSON coords (3.6)`

**Problem:** Task numbers from a planning doc (house rule: comments never point at planning docs or task numbers).

**Proposed:** line 71-72 -> `// Optional keys, shown with their defaults.` (see [O1-6](#o1-6) for the designation keys); drop ` (3.3)`, ` (3.4)`, ` (3.5)`, ` (3.6)` from lines 73, 81, 84, 85. Then delete the TODO.md "Phase 3.3-3.7" sentence once [O1-5](#o1-5)..O1-14 are done.

**Note:** Part of [D6](010_Decisions.md#d6).

<a id="o1-6"></a>

### O1-6 · Wrong doc · Low · "omit any of these and the parser uses the same default" is wrong for the designation lists

**Status:** Deferred: the Harness stays, and its cleanup waits until after 3.0 ([D6](010_Decisions.md#d6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Harness/HarnessSettings.cs:71-72`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/HarnessSettings.cs#L71-L72), 77-79

**Says:** `Defaults shown; omit any of these and the parser uses the same default.` then `{ "HighDesignations", "J,Q" }, { "LowDesignations", "V,T" }, { "BothDesignations", "" }`

**Problem:** AirwaySettingsParser.ReadDesignationStrata (Core AirwaySettingsParser.cs:205-229) applies the J/Q-High, V/T-Low default only when none of the three keys is present. Omitting just one of them (e.g. drop `LowDesignations`) leaves V and T with no file at all. The comment at lines 75-76 states this correctly; lines 71-72 contradict it.

**Proposed:** line 71-72 -> `// Optional keys, shown with their defaults: leaving one out gives the same result, except the three designation lists below, which only default together.`

<a id="o1-7"></a>

### O1-7 · History wording · Low · History/planning phrases in HarnessSettings

**Status:** Deferred: the Harness stays, and its cleanup waits until after 3.0 ([D6](010_Decisions.md#d6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Harness/HarnessSettings.cs:87-88`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/HarnessSettings.cs#L87-L88), 552-555

**Says:** `(all verified working against real NASR data during development), try e.g.:` / `matching the "written even when GenerateGeojson = N" contract.`

**Problem:** The first narrates past testing; the second quotes a "contract" from a planning doc that is not in the repository.

**Proposed:** line 87-88 -> `// To exercise Designation grouping, waypoint buffering or ROI clipping, try e.g.:`; lines 553-554 -> `Settings for the alias-only path: GenerateGeojson "N", alias file on.` (or delete with [O1-15](#o1-15)).

<a id="o1-8"></a>

### O1-8 · Wrong doc · Low · HarnessSettings summaries say every key is "listed with its default value"

**Status:** Deferred: the Harness stays, and its cleanup waits until after 3.0 ([D6](010_Decisions.md#d6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Harness/HarnessSettings.cs:110-112`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/HarnessSettings.cs#L110-L112), 161-163, 223-225, 286-288, 344-346, 389-391, 438-439, 476-478

**Says:** `Every key the Airports settings parser recognizes is listed below with its default value, so any of them can be flipped here`

**Problem:** Most values are the harness's own, not the parser's defaults: `IncludeFebCustomProperties` "Y" (default N), `CrcDefaultsFor` lists files (default none), Departures/Arrivals `Area` "Artccs" + `ArtccFilter` "ZOB", Fixes `OutputBy` "ChartAndFixUse" (parser default "All", FixSettingsParser.cs:73), Procedures `Facilities`/`Airports` filled. Procedures also does not list every key: `IncludeRoiAirports` (ProcedureSettingsParser.cs:48) is missing (it is read only when `Area` is absent, which the harness never does).

**Proposed:** in each of the eight summaries replace "is listed below with its default value" by "is listed below, set to exercise it (not necessarily the parser's default)"; for Procedures add "- all but IncludeRoiAirports, which is read only for a block without Area".

<a id="o1-9"></a>

### O1-9 · Wrong doc · Low · ARTCC Boundaries "HighLowUnlimited adds a third, Unlimited-only file"

**Status:** Deferred: the Harness stays, and its cleanup waits until after 3.0 ([D6](010_Decisions.md#d6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Harness/HarnessSettings.cs:354-357`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/HarnessSettings.cs#L354-L357)

**Says:** `"HighLow" writes one High file and one Low file, an UNLIMITED ring going into both; "HighLowUnlimited" adds a third, Unlimited-only file;`

**Problem:** HighLowUnlimited also takes the UNLIMITED rings out of the High and Low files (ArtccBoundaryGeojsonWriter.cs:72-77: High = High only, Low = Low only, Unlimited = Unlimited only). "adds a third" implies High/Low are unchanged.

**Proposed:** `"HighLowUnlimited" writes one file each for HIGH, LOW and UNLIMITED rings;`

<a id="o1-16"></a>

### O1-16 · Minor · Low · Harness members without XML docs (the TODO.md standard)

**Status:** Deferred: the Harness stays, and its cleanup waits until after 3.0 ([D6](010_Decisions.md#d6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Harness/Program.cs:17`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/Program.cs#L17); [`AirwayAliasRunner.cs:15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/AirwayAliasRunner.cs#L15); [`AirwayGeojsonRunner.cs:16`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/AirwayGeojsonRunner.cs#L16); [`ConsoleReport.cs:25`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/ConsoleReport.cs#L25), 27, 32, 484; [`HarnessSettings.cs:868`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/HarnessSettings.cs#L868)

**Says:** `public static async Task Main()` / `public static AirwayServiceResult Run(...)` / `public static void PrintNasrParseSummary(TimeSpan elapsed)` / `public static void PrintAirwayServiceResult(...)` / `private static void AddCrcDefaults(`

**Problem:** Every other harness member has a `<summary>`; these don't (Program.Main, two Airways runners, PrintNasrParseSummary, PrintAirwayServiceResult, PrintWarnings, AirwayIdPattern, AddCrcDefaults with its 8 parameters).

**Proposed:** add summaries (and `<param>`s for AddCrcDefaults, as its siblings have), then add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to FeBuddy.Harness.csproj so CS1591 keeps it that way, and delete the TODO.md "Standards" item.

**Note:** Part of [D6](010_Decisions.md#d6).

<a id="o1-12"></a>

### O1-12 · Wrong doc · Low · WxStationRunner: "Unlike every other runner"

**Status:** Deferred: the Harness stays, and its cleanup waits until after 3.0 ([D6](010_Decisions.md#d6)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Harness/WxStationRunner.cs:14-17`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Harness/WxStationRunner.cs#L14-L17)

**Says:** `Unlike every other runner, this does not take <c>NasrCsvDataCollection</c>`

**Problem:** TelephonyRunner.RunAsync takes no NASR data either (TelephonyRunner.cs:25-26 says "Unlike most runners").

**Proposed:** `Like TelephonyRunner, and unlike the others, this does not take NasrCsvDataCollection: ...`

## FeBuddy.Installer

<a id="o1-20"></a>

### O1-20 · History wording · Low · "This is what our previous attempt was missing."

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.Installer/FeBuddy.UI.wxs:218-219`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer/FeBuddy.UI.wxs#L218-L219)

**Says:** `<!-- This is what our previous attempt was missing. -->` / `<UIRef Id="WixUI_Common" />`

**Problem:** Narrates a past attempt instead of saying what the reference is for.

**Proposed:** `<!-- WixUI_Common brings in the stock dialogs and strings this dialog set uses (WelcomeDlg, InstallDirDlg, VerifyReadyDlg...). -->`

<a id="o1-22"></a>

### O1-22 · Wrong doc · Low · Get-InstallerVersion says it counts releases; it counts every build

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.Installer/Get-InstallerVersion.ps1:3-5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer/Get-InstallerVersion.ps1#L3-L5), 16-18

**Says:** `advances the persisted release counter` / `bumped by exactly 1 for every single release this script ever produces (alpha, beta, rc, or stable - it does not matter which)`

**Problem:** build.ps1 runs it on every build (local builds, CI's Installer job, the release-checks pre-flight), and each advances the file; only the release workflow's build is committed (Push-InstallerCounter.ps1). VERSIONING.md:126-127, 132-133 says "every build" correctly.

**Proposed:** line 4 -> `advances the persisted counter`; lines 16-18 -> `minor.build together are one counter, bumped by 1 on every build (only a release build's bump is committed, by the release workflow), and reset to 0.0 when major changes.`

**Note:** Also found as [D1b-20](#d1b-20).

<a id="o1-19"></a>

### O1-19 · Unused · Low · Unused localization string DowngradeError

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.Installer/Package.en-us.wxl:6`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer/Package.en-us.wxl#L6)

**Problem:** Nothing uses `!(loc.DowngradeError)`: MajorUpgrade has `AllowDowngrades="yes"` and the version rule's message is `VersionPolicyBlockedError`. Leftover from the WiX template (2.x had it too).

**Proposed:** delete line 6.

**Future use:** unlikely.

<a id="o1-32"></a>

### O1-32 · Minor · Low · Small inaccuracies in installer comments and one awkward sentence

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.Installer/Package.wxs:44-45`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer/Package.wxs#L44-L45); [`FeBuddy/FeBuddy.Installer/FeBuddy.Installer.wixproj:20-21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer/FeBuddy.Installer.wixproj#L20-L21); [`FeBuddy/FeBuddy.Installer/InstallerState.wxs:43`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer/InstallerState.wxs#L43); [`FeBuddy/FeBuddy.Installer.UI/ViewModels/SetupViewModel.cs:106`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer.UI/ViewModels/SetupViewModel.cs#L106), 195

**Says:** `passed in by build.ps1 from the built exe's Product version` / `(e.g. "2.8.4-alpha.1")` / `_ => $"Setup removes {Product} {InstalledVersion} from this PC."`

**Problem:** build.ps1 reads FE-BUDDY.dll's Product version (build.ps1:49-52), not the exe's. 2.8.x never had an MSI install to record a ProductSemVer, so the example is not a value that can be there. With no recorded version, InstalledVersion is "the installed version", so the uninstall page reads "Setup removes FE-BUDDY the installed version from this PC."

**Proposed:** "from the built FE-BUDDY.dll's Product version" (Package.wxs and wixproj); example -> `(e.g. "3.0.0-beta.12")`; uninstall body -> `Context.InstalledVersion is null ? $"Setup removes {Product} from this PC." : $"Setup removes {Product} {Context.InstalledVersion} from this PC."`.

**Note:** Re-checked after [#377](https://github.com/Nikolai558/FE-BUDDY/pull/377): still applies (InstallerState.wxs's example is now at line 51).

## FeBuddy.Installer.CustomActions

<a id="n1-2"></a>

### N1-2 · Minor · Low · Two comments edited in #377 were not re-wrapped

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/UpdateWindowViewModel.cs:22-24`](https://github.com/Nikolai558/FE-BUDDY/blob/e15a634aa6915245e558875cd249bccdb8455480/FeBuddy/FeBuddy.Wpf/ViewModels/UpdateWindowViewModel.cs#L22-L24); [`FeBuddy/FeBuddy.Installer.CustomActions/FeBuddy.Installer.CustomActions.csproj:7-9`](https://github.com/Nikolai558/FE-BUDDY/blob/e15a634aa6915245e558875cd249bccdb8455480/FeBuddy/FeBuddy.Installer.CustomActions/FeBuddy.Installer.CustomActions.csproj#L7-L9)

**Says:** `/// (not elevated: Windows Installer asks for administrator permission itself), and sets <see cref="InstallerStarted"/> so the shell closes FE-Buddy - the MSI` (158 characters)

**Problem:** Comments elsewhere wrap at about 100 characters. These two lines (158 and 106 characters) were extended in place.

**Proposed:** Re-wrap both comments.

<a id="o1-21"></a>

### O1-21 · Stale doc · Low · CustomActions project described as the version-policy action only

**Status:** Partly fixed in [#377](https://github.com/Nikolai558/FE-BUDDY/pull/377): the csproj header now names all three custom actions. Still open: `VersionPolicyActions`' summary says "actions", and `Binary Id="VersionPolicyCA"` now hosts three actions (`LaunchFeBuddy` too). · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.Installer.CustomActions/FeBuddy.Installer.CustomActions.csproj:3-9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer.CustomActions/FeBuddy.Installer.CustomActions.csproj#L3-L9); [`FeBuddy/FeBuddy.Installer.CustomActions/VersionPolicyActions.cs:9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer.CustomActions/VersionPolicyActions.cs#L9); [`FeBuddy/FeBuddy.Installer/Package.wxs:54`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer/Package.wxs#L54), 73

**Says:** `FeBuddy.Installer.CustomActions  -  the MSI's managed custom action (EnforceVersionPolicy). ... It is a thin wrapper: the rule itself is FeBuddy.Versioning.UpdatePolicy` / `/// <summary>The MSI's managed custom actions.</summary>` / `<Binary Id="VersionPolicyCA" ...>` used by `RemoveFeBuddyCredentials`

**Problem:** The project also holds RemoveFeBuddyCredentials (CredentialCleanupActions.cs, with its own Credential Manager P/Invoke logic, not a thin wrapper). VersionPolicyActions holds one action but its summary says "actions". The Binary named VersionPolicyCA now hosts both actions.

**Proposed:** csproj -> `the MSI's managed custom actions: EnforceVersionPolicy (a thin wrapper over FeBuddy.Versioning.UpdatePolicy) and RemoveFeBuddyCredentials (uninstall cleanup of Credential Manager).`; VersionPolicyActions summary -> `The MSI's version-policy custom action.`; rename `Binary Id="VersionPolicyCA"` to `FeBuddyCA` (Package.wxs:54, 56, 73).

## FeBuddy.Installer.UI

<a id="o1-31"></a>

### O1-31 · History wording · Low · "unchanged" / "did" in the installer UI's comments

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.Installer.UI/FeBuddy.Installer.UI.csproj:7-8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer.UI/FeBuddy.Installer.UI.csproj#L7-L8); [`FeBuddy/FeBuddy.Installer.UI/Setup/SetupChoices.cs:13-14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer.UI/Setup/SetupChoices.cs#L13-L14)

**Says:** `The installer is still FE-BUDDY-Setup.msi - the updater, the release workflow and 2.x's updater are unchanged.` / `Sets the session's properties, as the stock dialogs' Publish events did`

**Problem:** Both narrate the change that added the window. The stock dialogs still exist and still do this (the MsiDisableEmbeddedUI fallback, Package.wxs:105-113).

**Proposed:** csproj -> `The installer is FE-BUDDY-Setup.msi either way, so the app's updater, the release workflow and 2.x's updater need nothing from this project.`; SetupChoices -> `..., as the stock dialogs' Publish events do (FeBuddy.UI.wxs and InstallOptionsDlg.wxs, used when embedded UIs are disabled): ...`

## FeBuddy.Installer.UI/Setup

<a id="o1-26"></a>

### O1-26 · Unused · Low · ProgressTracker.Complete() is never called

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.Installer.UI/Setup/ProgressTracker.cs:87-88`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer.UI/Setup/ProgressTracker.cs#L87-L88)

**Problem:** SetupUI never calls it; the finish page sets `ProgressPercent = 100` itself (SetupViewModel.Finish, line 575).

**Proposed:** delete Complete().

**Future use:** unlikely.

## FeBuddy.Installer.UI/Theme

<a id="o1-29"></a>

### O1-29 · Unused · Low · Card.Accent style is never used

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.Installer.UI/Theme/Controls.xaml:18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer.UI/Theme/Controls.xaml#L18), 382-385

**Problem:** No view or code references Card.Accent (Card, Card.Warn and Card.Danger are used).

**Proposed:** delete the style and drop "Card.Accent / " from the header list (line 18).

**Future use:** unlikely.

## FeBuddy.Installer.UI/ViewModels

<a id="o1-27"></a>

### O1-27 · Unused · Low · SetupViewModel.IsFinished is never used

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.Installer.UI/ViewModels/SetupViewModel.cs:455-456`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer.UI/ViewModels/SetupViewModel.cs#L455-L456)

**Problem:** Not bound in SetupWindow.xaml (which binds IsFinishPage) and not read in code. Every other public SetupViewModel member is bound or called by SetupUI/SetupWindow.

**Proposed:** delete IsFinished.

**Future use:** unlikely.

## FeBuddy.Installer.UI/Views

<a id="o1-28"></a>

### O1-28 · Unused · Low · PromptWindow.OnClosing override does nothing

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.Installer.UI/Views/PromptWindow.xaml.cs:85-95`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer.UI/Views/PromptWindow.xaml.cs#L85-L95)

**Problem:** The constructor already sets `_result = request.CloseResult` (line 22) and a button only replaces it with its own result, so `_result` can be None only when CloseResult is None, and the branch then assigns None again.

**Proposed:** delete the OnClosing override (and `using System.ComponentModel;`).

**Future use:** unlikely.

## FeBuddy.Versioning

<a id="o1-35"></a>

### O1-35 · Stale doc · Low · Versioning csproj lists only two consumers

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.Versioning/FeBuddy.Versioning.csproj:6-10`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Versioning/FeBuddy.Versioning.csproj#L6-L10)

**Says:** `this assembly is referenced both by FeBuddy.Core (net10.0-windows) and by the installer's version-policy custom action (FeBuddy.Installer.CustomActions, net472 ...)`

**Problem:** FeBuddy.Installer.UI (net472, the setup window) and .github/scripts/ReleaseVersion.cs (`#:project`) also reference it.

**Proposed:** `... referenced by FeBuddy.Core (net10.0-windows), by the installer's custom actions and setup window (FeBuddy.Installer.CustomActions and FeBuddy.Installer.UI, net472 - WiX's host only loads .NET Framework) and by the release scripts (.github/scripts/ReleaseVersion.cs).`

<a id="o1-33"></a>

### O1-33 · Wrong doc · Low · UpdatePolicy "shared by ... the app"

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.Versioning/UpdatePolicy.cs:5-8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Versioning/UpdatePolicy.cs#L5-L8)

**Says:** `FE-Buddy's one version-transition rule, shared by the installer's version-policy custom action (FeBuddy.Installer.CustomActions) and the app.`

**Problem:** Neither FeBuddy.Core nor FeBuddy.Wpf calls UpdatePolicy. Its callers are VersionPolicyActions (custom action) and SetupContext.UpgradeMode (the installer window, FeBuddy.Installer.UI).

**Proposed:** `..., shared by the installer's version-policy custom action (FeBuddy.Installer.CustomActions) and its setup window (FeBuddy.Installer.UI).`

**Note:** Also found as [T2-20](#t2-20).

## FeBuddy.Versioning/Models

<a id="o1-34"></a>

### O1-34 · Stale doc · Low · ReleaseChannel: "stored by name in UserConfig.json"

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.Versioning/Models/ReleaseChannel.cs:4-5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Versioning/Models/ReleaseChannel.cs#L4-L5)

**Says:** `stored by name in <c>UserConfig.json</c> at <c>General.UpdateChannel</c>.`

**Problem:** Settings are profiles now; `General.UpdateChannel` is one of the keys every profile shares and lives in `User Configurations\Shared.json` (UserConfigFile.cs:22-24, UserConfigPortability.SharedByProfiles).

**Proposed:** `stored by name at <c>General.UpdateChannel</c> (in Shared.json, which every settings profile shares).` Same change in UpdateChannelSetting's summary.

## UT/Infrastructure/Configuration

<a id="t2-25"></a>

### T2-25 · History wording · Low · Feature/issue numbers that only narrate history in test summaries

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigAreasTests.cs:8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigAreasTests.cs#L8); [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigProfilesTests.cs:10`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigProfilesTests.cs#L10); [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigTransferTests.cs:398`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigTransferTests.cs#L398) (production echo: [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs:9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs#L9))

**Says:** "Covers UserConfigAreas (issue #335)" / "Covers settings profiles (issue #324)" / "are this PC's (Feature #364)"

**Problem:** Feature numbers, not bug explanations (ChangeLog.md:51, 205, 291).

**Proposed:** Delete "(issue [#335](https://github.com/Nikolai558/FE-BUDDY/issues/335))", "(issue [#324](https://github.com/Nikolai558/FE-BUDDY/issues/324))", "(Feature [#364](https://github.com/Nikolai558/FE-BUDDY/issues/364))". Keep SharedDataDownloadTests.cs:402 "(issue [#338](https://github.com/Nikolai558/FE-BUDDY/issues/338))" and AptCsvParserTests' summary: both explain a bug the test guards.

<a id="t2-21"></a>

### T2-21 · Stale doc · Low · "With a second layout" stand-in steps read as if FE-Buddy had only one layout

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigFileTests.cs:421`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigFileTests.cs#L421), 473-475; [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigTransferTests.cs:289`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigTransferTests.cs#L289); also [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigrations.cs:53-56`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigrations.cs#L53-L56)

**Says:** "A stand-in second layout: ..." / "With a second layout, a layout 1 file ..." / "so the bringing-forward ... can be tested before FE-Buddy has a second layout"

**Problem:** FE-Buddy has layout 2 (`UserConfigVersion.Current = 2`); the tests use a made-up step (`MoveArtcc`) through `ConfigureForTesting`.

**Proposed:** 421 -> "A made-up layout 2, standing in for the real one: Settings ▸ Facility's ARTCC moved under General."; 474 -> "With a made-up layout 2, a layout 1 file is brought forward ..."; Transfer 289 -> "With a made-up layout 2, an older file's settings are brought forward ..."; production -> "Stands in a current layout and its steps, so bringing UserConfig files and imports forward can be tested with steps of the test's own. Unit tests only."

<a id="t2-33"></a>

### T2-33 · Minor · Low · Class summaries that leave out half of what the class tests

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigFileTests.cs:10-13`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigFileTests.cs#L10-L13); [`FeBuddy/FeBuddy.UnitTests/Infrastructure/SharedData/SharedDataDownloadTests.cs:11-17`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/SharedData/SharedDataDownloadTests.cs#L11-L17)

**Says:** "dictionary round-trip through the nested JSON file, a resilient launch read path, per-node saves, and the one-step per-node undo." / "... and the SharedDataRefreshResult truth table."

**Problem:** UserConfigFileTests also covers Shared.json saves (346-417) and the layout stamp / bringing forward (419-595); SharedDataDownloadTests also covers reusing a young copy (334-398) and `DescribeFailure` (400-443).

**Proposed:** Append ", Shared.json's own saves, and the layout stamp and bringing an older file forward" and ", reusing a copy younger than asked, naming a failure's innermost cause" respectively.

<a id="t2-13"></a>

### T2-13 · Wrong doc · Low · UserConfigMigrationsTests: "every edit can run twice without harm", but its ChangeValue lambda can't

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigMigrationsTests.cs:6-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigMigrationsTests.cs#L6-L11), 16, 156; also [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigrationContext.cs:7-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigrationContext.cs#L7-L11) and [`UserConfigMigration.cs:15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigration.cs#L15)

**Says:** `settings.ChangeValue("General.C", value => value == "true" ? "Y" : "N")` / "a missing or failing step stops it with the input untouched ... and every edit can run twice without harm"

**Problem:** Only `Move` is run twice (lines 133-135). Run twice, this `ChangeValue` lambda turns an already-converted "Y" into "N" - exactly what docs/Developers/Settings-Reference.md:646-648 forbids ("a `ChangeValue` must leave an already-changed value alone", whose example is the idempotent form). The failure tests (61-78) also never check that the input is untouched.

**Proposed:** Both lambdas (16, 156) -> `value => value == "true" ? "Y" : value == "false" ? "N" : value`, plus an assert that a second `ChangeValue` leaves "Y" alone; summary -> "Covers UserConfigMigrations and the edits a step is made of (UserConfigMigrationContext): steps run in order from the file's layout to the current one and never change their input, a missing or failing step stops it, the real steps cover every layout, and a move or change run twice does no harm." Same lambda fix in UserConfigMigration.cs:15; UserConfigMigrationContext.cs:8 -> "Move, Set and Remove can run twice without harm, and so can a ChangeValue that leaves an already-changed value alone, so ...".

<a id="t2-22"></a>

### T2-22 · Stale doc · Low · "UserConfig.json" where the file is now a profile's `UserConfig.<Profile>.json`

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigTransferTests.cs:191`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Configuration/UserConfigTransferTests.cs#L191), 254, 566, 918 (production echoes: [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigTransfer.cs:17`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigTransfer.cs#L17), 40, 138, 201)

**Says:** "here, UserConfig.json held open by another program" / "A plain UserConfig.json copied out of someone's %APPDATA%\FE-Buddy imports as it is." (UserConfigTransfer.cs:201)

**Problem:** Settings live in `%APPDATA%\FE-Buddy\User Configurations\UserConfig.<Profile>.json` (UserConfigFile.cs:114-115, 141); the test at 918 locks `UserConfigFile.ConfigFilePath` = `UserConfig.Default.json`.

**Proposed:** 918 -> "here, the profile's file held open by another program"; 191 -> "A plain settings file from another PC (a profile's UserConfig.&lt;Profile>.json) is accepted as it is."; 254 -> "A plain settings file's stamp is its layout, not a setting to import."; 566 -> "as a plain settings file has them"; UserConfigTransfer.cs:201 -> "A profile's file copied out of someone's User Configurations folder imports as it is." (and the same wording at 17, 40, 138).

## UT/Infrastructure/Credentials

<a id="t2-31"></a>

### T2-31 · Unused · Low · Two near-meaningless tests

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Credentials/CredentialStoreTests.cs:383-386`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Credentials/CredentialStoreTests.cs#L383-L386); [`FeBuddy/FeBuddy.UnitTests/Repository/DocLinkTests.cs:19-21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Repository/DocLinkTests.cs#L19-L21)

**Problem:** The first only restates `Lazy<T>`. The second asserts that the app has at least one anchored doc link - it exists only so the theory's anchor branch runs, and fails with no defect if the owner drops `Links.GitHubTokenGuideTroubleshooting` or that section while trimming docs. (The theory itself pins docs/Users/User-Guide.md, FAQ-and-Troubleshooting.md, GitHub-Token-Guide.md and its heading "If something goes wrong" (line 120) - intended, but a doc trim/rename must update Links.cs in the same change. FileEncodingTests enforces only "UTF-8 without BOM", which matches docs/Developers/README.md:146.)

**Proposed:** Delete `default_is_one_shared_store`; delete `the_app_links_to_a_section_of_its_docs` (the theory already checks every anchor that exists).

## UT/Infrastructure/Dtpp

<a id="t2-27"></a>

### T2-27 · Unused · Low · The local HTTP test server is copied into five classes; one copy returns a task nobody uses

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Dtpp/DtppDownloaderTests.cs:51-112`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Dtpp/DtppDownloaderTests.cs#L51-L112); [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Nasr/NasrCycleDownloaderTests.cs:125-188`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Nasr/NasrCycleDownloaderTests.cs#L125-L188); [`FeBuddy/FeBuddy.UnitTests/Infrastructure/SharedData/SharedDataDownloadTests.cs:56-119`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/SharedData/SharedDataDownloadTests.cs#L56-L119); [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Telephony/TelephonyDownloaderTests.cs:63-122`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Telephony/TelephonyDownloaderTests.cs#L63-L122); [`FeBuddy/FeBuddy.UnitTests/Infrastructure/WxStations/WxStationDownloaderTests.cs:69-132`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/WxStations/WxStationDownloaderTests.cs#L69-L132)

**Problem:** ~60 duplicated lines each; every test repeats the same try/finally Stop/Close.

**Proposed:** One `TestSupport/LocalHttpServer` (`Start(byte[] payload, HttpStatusCode status = OK)`, `Url`, `RequestCount`, `Dispose()` stops and closes) used by all five; drop `ServerTask` from the zip server.

## UT/Infrastructure/Dtpp/Parsers

<a id="t2-24"></a>

### T2-24 · Stale doc · Low · DtppMetafileXmlParserTests refers to "the example file", which is not in the repo

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Dtpp/Parsers/DtppMetafileXmlParserTests.cs:577-580`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Dtpp/Parsers/DtppMetafileXmlParserTests.cs#L577-L580)

**Says:** "The example file's first airport (ADAK): ... copied verbatim from the real 2609 file's shape."

**Problem:** No example metafile exists in the repository.

**Proposed:** "The first airport (ADAK) of the real 2609 Metafile: one state, one city, one airport, its first two MIN records and one IAP record, in the real file's shape."

## UT/Infrastructure/FileSystem

<a id="t2-32"></a>

### T2-32 · Minor · Low · AppDataResetTests "fills it the way FE-Buddy would" with names FE-Buddy doesn't use

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/FileSystem/AppDataResetTests.cs:28`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/FileSystem/AppDataResetTests.cs#L28), 44-45

**Says:** `Write(@"Telephony\telephony.html", "x");` / `Write(@"WxStations\stations.xml", "x");`

**Problem:** FE-Buddy keeps `Telephony\telephony_register.html` and `WxStations\stations.cache.xml`.

**Proposed:** `Path.Combine("Telephony", TelephonyFiles.RegisterFileName)` and `Path.Combine("WxStations", WxStationFiles.FileName)`.

## UT/Infrastructure/Logging

<a id="t2-17"></a>

### T2-17 · Wrong doc · Low · The "AppLog" collection's summary covers only AppLog

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Logging/AppLogTests.cs:7-13`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Logging/AppLogTests.cs#L7-L13)

**Says:** "Shared xUnit collection for tests that touch process-wide static state in AppLog (directly, or indirectly through a component that logs)."

**Problem:** The collection serializes every test that touches a process-wide static: UserConfigFile/UserConfigMigrations, GitHubAuth (TestSupport/TestCredentials.cs:180-181, 195-196 tell callers to join it for that), TempWorkspace, DevMode/OutputFormatting (GeojsonFileWriterTests.cs:16-19; GeojsonFileSetTests never logs), AppDataReset, LegacySquirrel*.

**Proposed:** "Shared xUnit collection for tests that touch process-wide static state - AppLog, UserConfigFile, GitHubAuth, TempWorkspace, DevMode and the like. Parallel execution is disabled so these tests never see each other's settings."

## UT/Infrastructure/Markdown

<a id="t2-26"></a>

### T2-26 · History wording · Low · MarkdownParserTests: a test named after the 2.9.0 release notes

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Markdown/MarkdownParserTests.cs:6-10`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Markdown/MarkdownParserTests.cs#L6-L10), 209-242

**Says:** `parse_the_v290_release_notes_shape` / "Migrated from .NET 6 to .NET 10." / "using the shapes GitHub release notes actually contain"

**Problem:** Ties the sample to a 2.x release. The parser also renders News.md posts (MarkdownView with a link base) and card text (InlineMarkdown), which the class tests too (lines 367-399).

**Proposed:** Rename `parse_release_notes_with_nested_change_log_lists`; summary -> "... using the shapes FE-Buddy's release notes and News posts contain."

## UT/Infrastructure/Nasr

<a id="t2-28"></a>

### T2-28 · Unused · Low · Duplicate TempWorkspace test inside NasrCycleDownloaderTests

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Nasr/NasrCycleDownloaderTests.cs:313-325`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Nasr/NasrCycleDownloaderTests.cs#L313-L325)

**Problem:** Tests `TempWorkspace.ClearOnLaunch`, already covered by TempWorkspaceTests.cs:45-60; the "v2-legacy" folder name hints at 2.x content that plays no part.

**Proposed:** Delete it (or move the Downloads-folder assertion into `TempWorkspaceTests.temp_workspace_clear_on_launch_empties_tree`).

<a id="t2-34"></a>

### T2-34 · Minor · Low · Typo in NasrCycleDownloaderTests summary

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Nasr/NasrCycleDownloaderTests.cs:15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Nasr/NasrCycleDownloaderTests.cs#L15)

**Says:** "and far too large to fix in a unit test"

**Problem:** 

**Proposed:** "and far too large to download in a unit test".

<a id="t2-19"></a>

### T2-19 · Wrong doc · Low · WaypointLocatorTests: test name claims more than it asserts

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Nasr/WaypointLocatorTests.cs:49-56`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Nasr/WaypointLocatorTests.cs#L49-L56)

**Says:** `blank_identifiers_in_the_data_are_never_matched` asserts `Find(data, "alpha")` and `Find(data, "NOPE")`

**Problem:** A blank ID can't be asked for (Find throws, line 35), so the test only shows that rows with blank IDs don't break the lookup, and that it ignores case.

**Proposed:** Rename `rows_with_blank_identifiers_do_not_break_a_lookup_that_ignores_case`.

## UT/Infrastructure/Nasr/Parsers

<a id="t2-16"></a>

### T2-16 · Wrong doc · Low · NasrCsvParserMappingTests: `main_async_*` names and a summary the test doesn't match

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Nasr/Parsers/NasrCsvParserMappingTests.cs:164-198`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Nasr/Parsers/NasrCsvParserMappingTests.cs#L164-L198)

**Says:** `main_async_parses_every_file_into_the_combined_collection`, `main_async_missing_directory_throws` / "A cycle folder missing a file fails the whole parse rather than returning partial data."

**Problem:** There is no `MainAsync` (the method is `NasrCsvParser.ParseAllAsync`). The second test passes a folder that does not exist at all, not a folder missing one file.

**Proposed:** Rename `parse_all_async_parses_every_file_into_the_combined_collection` and `parse_all_async_of_a_missing_folder_throws`; summary -> "A cycle folder that isn't there fails the whole parse." (or copy all fixtures but one into a temp folder to test what the summary says).

## UT/Infrastructure/Platform

<a id="t2-18"></a>

### T2-18 · Wrong doc · Low · LegacySquirrelShortcutsTests says the shortcuts are 2.8.x's

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Platform/LegacySquirrelShortcutsTests.cs:8-10`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Platform/LegacySquirrelShortcutsTests.cs#L8-L10), 54

**Says:** "the Desktop and Start menu shortcuts FE-Buddy 2.8.x left" / "Both of 2.8.x's shortcuts"

**Problem:** Production says Squirrel installed FE-Buddy 2.x up to 2.9.0 (Core/Infrastructure/Platform/LegacySquirrelShortcuts.cs:8-9); Architecture.md:43 says 2.x.

**Proposed:** "2.8.x" -> "2.x" in both places.

## UT/Infrastructure/Telephony

<a id="t2-23"></a>

### T2-23 · Stale doc · Low · TelephonyFilesTests: "the two kept file names"

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Telephony/TelephonyFilesTests.cs:5-8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Telephony/TelephonyFilesTests.cs#L5-L8)

**Says:** "Covers TelephonyFiles: the two kept file names, the FAA URLs they are downloaded from"

**Problem:** TelephonyFiles also names the virtual airline list's two kept copies and URLs (`GngAirlinesFilePath/Url`, `VatsimRadarAirlinesFilePath/Url`) and the older copy; the class tests only the FAA pages.

**Proposed:** "Covers TelephonyFiles' FAA pages: their two kept file names, the URLs they are downloaded from, and that the kept copies land under the shared Telephony folder." (or add asserts for the GNG/VATSIM-Radar names and URLs).

## UT/Repository

<a id="t2-29"></a>

### T2-29 · Unused · Low · `FindRepositoryRoot` is duplicated in both Repository tests

**Status:** Open · **Session:** [S10](020_Work-Plan.md#s10)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Repository/DocLinkTests.cs:55-67`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Repository/DocLinkTests.cs#L55-L67); [`FeBuddy/FeBuddy.UnitTests/Repository/FileEncodingTests.cs:39-51`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Repository/FileEncodingTests.cs#L39-L51)

**Problem:** 

**Proposed:** Move to `TestSupport/RepositoryRoot.Find()` and call it from both.

## Reported more than once

Each review slice was read by its own reviewer, so some findings were reported twice. These IDs point at the finding that is kept.

- <a id="d1b-19"></a>**D1b-19** · Empty tracked FeBuddy/test.md · same as [O1-3](#o1-3).
- <a id="d1b-20"></a>**D1b-20** · Get-InstallerVersion.ps1 says the counter moves per release; it moves per build · same as [O1-22](#o1-22).
- <a id="d1b-22"></a>**D1b-22** · ci.yml comment: the coverage gate covers Versioning too · same as [O1-38](#o1-38).
- <a id="t2-20"></a>**T2-20** · `UpdatePolicy` says the app shares it; only the installer uses it · same as [O1-33](#o1-33).
