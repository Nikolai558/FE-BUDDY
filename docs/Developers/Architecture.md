# Architecture

The detailed picture: how the projects fit, what happens at launch, how FAA data becomes files,
and the decisions behind it. For the plain-terms version read [How FE-Buddy works](README.md)
first; for where code lives read [FeBuddy.Core structure](FeBuddy.Core-Structure.md) and
[FeBuddy.Wpf](FeBuddy.Wpf/README.md).

**Contents:** [Projects](#projects) · [Launch](#launch) · [The AIRAC data pipeline](#the-airac-data-pipeline) ·
[A run, end to end](#a-run-end-to-end) · [Settings and persistence](#settings-and-persistence) ·
[GeoJSON output](#geojson-output) · [Rules the data needs](#rules-the-data-needs) ·
[Messages and logging](#messages-and-logging) · [Updates and News](#updates-and-news) ·
[Design decisions](#design-decisions)

---

## Projects

```
            FeBuddy.Wpf  (FE-BUDDY.exe)         FeBuddy.Harness   FeBuddy.UnitTests
                  │                                    │                 │
                  └───────────────┬────────────────────┴─────────────────┘
                                  ▼
                            FeBuddy.Core ─────────► FeBuddy.Versioning ◄───── FeBuddy.Installer.CustomActions
                     (NetTopologySuite, CsvHelper)      (Semver)                        ▲
                                                                                        │
                                                          FeBuddy.Installer (WiX) ──────┘
                                                          packs the published app + the custom action
```

- **`FeBuddy.Core`** has three layers (Domain → Infrastructure → Application) and no UI. Everything
  the app does that is not a screen lives here, so it can be tested and driven by the harness.
- **`FeBuddy.Wpf`** is MVVM: views bind to view-models, view-models call Core. It never parses FAA
  data or writes output files itself.
- **`FeBuddy.Versioning`** is netstandard2.0 so both Core (.NET 10) and the MSI's custom action
  (.NET Framework 4.7.2, all WiX's host can load) share one version rule.

## Launch

`App.OnStartup` starts the log file sink, then runs `LaunchSequence.RunAsync` off the UI thread.
A step that fails is logged and degrades only the feature that needs it; launch never stops.

```
1. Clear %TEMP%\FE-Buddy            ─┐ first: the rest need the config,
2. Read UserConfig.json             ─┘ and the AIRAC download uses the temp folder
3. UTC time + internet check           the AIRAC step needs the date; every network step needs the internet flag
4. ┌ Version check (GitHub releases)
   ├ AIRAC data (below)                 in parallel - none needs another
   └ News (News.md from GitHub)
```

Results are published on `AppEnvironment` (`HasInternetConnection`, `Version`, `News`, `Changed`
event) and the AIRAC cache raises `StateChanged` as each cycle moves on. The view-models listen to
both, so the window fills in as launch progresses: the top-centre status narrates the downloads,
the Systems box turns green, the AIRAC Service screen unlocks.

## The AIRAC data pipeline

`AiracCycleDataCache` owns the FAA data for the session.

1. **Which cycles.** `AiracCycleResolver` calculates the previous, current and next cycle from the
   28-day cadence (no lookup table). Cycle IDs are `YYNN`: the year the cycle takes effect, then its
   number within that year.
2. **Prune.** Cached cycles other than those three are deleted from
   `%APPDATA%\FE-Buddy\AiracCycles`.
3. **Probe.** A cheap HTTP HEAD per cycle against the FAA's
   `https://nfdc.faa.gov/webContent/28DaySub/extra/<date>_CSV.zip` tells whether the FAA has
   published it. The next cycle often is not yet - that is `NotYetPublished`, not an error.
4. **Download and parse**, in the order current → previous → next. Downloads (network-bound) and
   parses (CPU- and memory-bound) overlap: while one cycle parses the next downloads. Each is one at
   a time - parsing two at once would double the peak memory. A cycle already on disk skips the
   download. A failure gets one retry.
5. **Readiness.** Each cycle moves through `NotDownloaded → Downloading → Downloaded → Parsing →
   Ready` (or `Failed`, or `NotYetPublished`). The service's overall readiness is:

| Readiness | When | The AIRAC Service |
|---|---|---|
| `Waiting` | a required cycle is still in progress | locked, "waiting" message |
| `Ready` | current ready, previous and next settled | open |
| `Degraded` | current ready, but previous and/or next failed | open; the failed cycle can't be picked |
| `Unavailable` | the current cycle failed | locked, "failed" message |

Asking for a cycle's data while it is still parsing waits for that parse rather than starting a
second one.

**Wx Stations rides along, but is not part of the pipeline above.** Its data is not a NASR CSV
group at all: it comes from aviationweather.gov's own station list, and `WxStationDownloader`
downloads it at most once per launch and copies it into every cycle folder that does not already
have it - previous, current and next alike - next to that cycle's NASR CSVs. A folder that already
has it is never re-downloaded, so its station data is only as current as the day it was first
filled in; a failed download never holds up the rest of the cycle, and is retried at the next
launch. `AiracCycleDataCache.GetWxStationsAsync` reads a cycle's copy fresh from disk on every call
(it is small, unlike the NASR data `GetAsync` memoizes) and returns `null` when the file is not
there yet - which the Wx Stations tab reports as missing and blocks the run on.

## A run, end to end

```
AIRAC Service screen                                   FeBuddy.Core
────────────────────                                   ────────────
Preview Settings ▸ Run AIRAC Service
  save any dirty tab (the user confirms)
  block if any tab is invalid
  each tab: BuildSettingsBlock()  ── Dictionary<string,string>
  AiracService.HasExistingOutput?  ── AIRAC_<cycle> has files: ask Overwrite / Delete all / Cancel
                                   ── AiracServiceSettings ─────►  AiracService.RunAsync(settings)
                                                                       gets the cycle's parsed data from the cache
                                                                       deletes AIRAC_<cycle> first, if asked
                                                                       for each block present, OutputDirectory = AIRAC_<cycle>:
                                                                         XxxService.Run(nasrData, block)
                                                                           1. XxxSettingsParser.Parse   block → typed settings (+ warnings)
                                                                           2. XxxBuilder                NASR rows → domain objects
                                                                           3. XxxGeojsonWriter          → .geojson files
                                                                           4. XxxAliasWriter            → alias .txt
                                                                         → XxxServiceResult
  Review tab ◄── progress reports, then AiracServiceResult ───────────
    each tab: DescribeRunResult(result)
```

ARTCC Boundaries has no step 4: `ArtccBoundaryService` stops after `ArtccBoundaryGeojsonWriter`,
since it has no alias file. Fixes likewise has no step 4: `FixService` stops after
`FixGeojsonWriter`, since it too has no alias file. Wx Stations has no step 4 either
(`WxStationService` stops after `WxStationGeojsonWriter`), and its step 1 input is not `nasrData`:
`AiracService` loads `wxStationData` from `AiracCycleDataCache.GetWxStationsAsync` only when Wx
Stations is selected, and hands it to `WxStationService.Run` instead.

- **The settings block is the contract.** Every tab (and the harness) hands Core a flat
  `Dictionary<string, string>`. Core never sees view-models, and the GUI never sees typed settings.
  The keys are listed in [Settings blocks](Settings-Blocks.md).
- A missing or invalid required setting throws `ArgumentException` (the tab's own validation
  normally stops that happening). Everything else - a waypoint that can't be located, an unknown
  key, a filter that matched nothing - is a `ServiceMessage` in the result, so one problem never
  loses a whole run.
- Progress arrives through `IProgress<AiracServiceProgress>`; each sub-service reports starting
  and finishing.
- **One folder per cycle.** A run writes everything into `<output>[\FE-Buddy_Output]\AIRAC_<cycle>`
  (`AiracServiceSettings.CycleOutputDirectory`): alias files in the folder itself, every GeoJSON
  file in its `Geojson` folder, and the files marked for vNAS under `Upload_to_vNAS` instead, laid
  out the same way (`AiracOutputPaths`). A sub-service only knows the folder it is given
  (`OutputDirectory`), so it can be run on its own by the harness and the tests. If the folder
  already has files, the GUI asks before the run: overwrite them, or have the service
  permanently delete the folder first (`ExistingOutputAction`).

## Settings and persistence

**`UserConfig.json`** (`%APPDATA%\FE-Buddy`) is one nested JSON tree, read into memory at launch
and addressed by dotted paths (`Services.AiracService.Geojson.Airways.OutputBy`). Every key is in
[UserConfig.json reference](UserConfig-Reference.md).

- **Save one node at a time.** A sub-service tab saves only its own subtree
  (`UserConfigFile.Save(nodePath)`), leaving every other section untouched.
- **One-step undo.** Before a node is saved, its previous state is snapshotted to
  `UserConfig.previous.json`; **Undo last save** restores it.
- **Dirty means "different from saved".** Each settings screen keeps a `SavedStateSnapshot` of what
  it last saved or loaded, and compares its current values against it on every change. Change a
  value and change it back, and the tab is clean again. The snapshot is taken by running the tab's
  own `WriteToConfig()` into a buffer, so there is no second list of fields to keep in step.
- **Validation is continuous**, not save-time: a tab turns red the moment a value goes missing, and
  the offending box carries the message (`FieldState.Error`).

## GeoJSON output

- **CRC's three tiers.** CRC decides how a feature looks from, in order: the feature's own
  properties, the file's **isDefaults** feature, then CRC's built-in fallback. FE-Buddy writes one
  isDefaults feature at the head of a file from the user's **CRC ERAM Defaults**
  (`CrcFeatureFactory`), and only the properties that differ on individual features. Every value is
  validated (`CrcPropertyValidator`) first, so FE-Buddy never writes a value CRC cannot draw. See
  [CRC GeoJSON concepts](https://github.com/KCSanders7070/CRC_GeoJson_Concepts/blob/main/CRC_Geojsons.md).
- **CRC defaults only go on vNAS files.** CRC reads its maps from vNAS, so a file gets an
  isDefaults feature only when the user marked it for vNAS and chose it for CRC-ERAM defaults
  (`VnasFileChoices`, from the `UploadToVnas` and `CrcDefaultsFor` keys).
- **Defaults are never guessed.** A CRC value a chosen file needs that is empty is a validation
  error, not a silent default.
- **`feb.*` properties** are FE-Buddy's own, camelCase, opt-in, and written only on the kind of
  feature they describe (`pointId` on points; `waypoints` and `rwyId` on lines). No lat/lon
  properties - the geometry carries them.
- **A symbol's style can live on the Feature instead of the file's defaults.**
  `CrcSymbolDefaults.Style` is nullable - `null` means every Symbol Feature in the file carries its
  own `style` rather than one shared default - and `CrcDefaultsReader.ReadSymbol` takes a
  `readStyle` flag so a caller can skip the style box entirely when the Features will carry it.
  NAVAIDs is the first sub-service to use this: a merged `OutputBy = All` Symbols file with
  `SymbolStyleBy = Type` gives each Feature its own style from `NavaidTypes.SymbolStyleFor` (or the
  chosen fan marker style), so the file's own CRC defaults carry none; a type with no mapped style
  (`CONSOLAN`, or one FE-Buddy does not recognize) is left with none, and a warning is reported once
  per such type.
- **Coordinates** are rounded to the user's precision (0-15 decimal places, default 6).
- **Layout.** Single-line by default (smaller files); pretty printed when the user chooses it or in
  developer mode.
- **Empty files are not written.** A file whose features were all filtered out is skipped, and an
  advisory says so.

## Rules the data needs

The FAA's data has quirks; these rules handle them. Each lives in one class.

- **Airway classification** (`AirwayClassifier`). High, Low or Other from the **highest published
  altitude** on the airway (≥ 18,000 ft is High), not the name's letter - the letter is only a
  convention. An airway is never split between files.
- **Designation** comes from the airway ID's leading letters (`J` in `J3`), never from NASR's
  `AWY_DESIGNATION` column (a `Q` airway can be listed as `RN`).
- **Border markers** (`AirwayReferenceOnlyPoints`, `AirwayNormalizer`). NASR lists points like
  `U.S. CANADIAN BORDER-4` to describe a route; they are not navigable. They are recognised by a
  blank `FROM_PT_TYPE` and collapsed out, so the airway runs straight between the real waypoints.
- **Unresolvable waypoints exclude the whole airway.** An airway with a real waypoint that can't be
  located is left out entirely, with a warning naming it - a half-drawn airway is worse than none.
- **Waypoint buffer** (`AirwayWaypointBuffer`). Optionally stops each leg 2.5 NM short of a
  five-letter fix and 5 NM short of anything else, so lines don't run through symbols. It only
  buffers real waypoints, never the vertices an antimeridian split or ROI clip creates.
- **Antimeridian** (`AntimeridianSplitter`). A line crossing ±180° is split into two so it does not
  draw across the whole map; the split never produces a zero-length line.
- **Efficient LineString handling** (`LineStringMerger`). Paths that share segments (every body of a
  departure runs through the same trunk) are merged into the fewest, longest LineStrings that draw
  each segment exactly once - smaller files, and dashed styles stay dashed.
- **Departure naming** (`DepartureNaming`). A procedure is named by its FAA computer code with the
  amendment digit removed (`DOTSS2.DOTSS` → `DOTSS`), matched against `AMENDMENT_NO` rather than cut
  at the first digit (`1U71.LUNDI` → `1U7`). With no usable code, the published name with
  everything but letters and digits removed (`O'HARE` → `OHARE`).
- **Arrival naming** (`ArrivalNaming`). The same rule as a departure, but a STAR's computer code
  reads `TRANSITION.PROCEDURE` - the reverse order - so `AALAN.BLAID2` → `BLAID` and `FIM.FERN7`
  (published as `FERNANDO`) → `FERN`.
- **Arrival direction.** A STAR is flown transition → body, the reverse of a departure's body →
  transition order, so an arrival's points (Symbols, Text, and the alias command's fix list) list
  every transition before the bodies, and the Lines file joins each transition to the body that
  starts where it ends.
- **A STAR shared by two ARTCCs** (`ArrivalProcedure.ArtccFor`). `STAR_BASE.ARTCC` can list two
  centres (e.g. `ZDC ZNY` for ARLFT, serving 33N/DOV/ILG). Each served airport's copy belongs to
  whichever of the listed ARTCCs is that airport's `APT_BASE.RESP_ARTCC_ID` (the first listed
  ARTCC when neither matches) - which decides that copy's output folder, its `feb.artcc`, and
  which ARTCC filter tick includes it, so ticking ZNY gives ARLFT at ILG only.
- **STARs and SIDs can collide.** In the cycle effective 2026-09-03 the FAA's STAR data also lists
  ORF's NUTIY and SWOPE departures. The GeoJSON files never clash - an arrival's file name always
  carries `STAR` - but both alias files end up with the same command (`.orfNUTIYf`,
  `.orfSWOPEf`). Printed as duplicates on purpose for now; see
  [FAQ](../Users/FAQ-and-Troubleshooting.md#why-does-the-same-alias-command-show-up-in-both-departurestxt-and-arrivalstxt).
- **NAVAID data** (`NavaidBuilder`). Built from `NAV_BASE` only; a NAVAID with a `NAV_STATUS` of
  `SHUTDOWN` is always skipped. Duplicate `NAV_ID`s are normal in real NASR data and are never
  merged - `ABQ` is both a VORTAC and a VOT, `AA` is two NDBs - every row NASR publishes becomes its
  own NAVAID.
- **ARTCC boundary rings** (`ArtccBoundaryBuilder`). Built from `ARB_BASE` (the location) and
  `ARB_SEG` (the points), grouped by LocationId and altitude; within a group, a new ring starts at
  the first row and again wherever `POINT_SEQ` is not greater than the previous row's - the FAA's
  own signal for a new ring, e.g. ZAK's UNLIMITED group is a CTA ring followed by a FIR ring. A ring
  is closed by repeating its first point when NASR's own last point differs, then skipped (with a
  message) if it still has fewer than two distinct points. Only the LocationIds with `ARB_SEG` rows
  draw anything - the Canadian, foreign and CERAP entries `ARB_BASE` also lists have none.
- **NAVAID alias commands** (`NavaidAliasWriter`). Each NAVAID contributes a `.nav<NavId>` command
  and, when its name yields a different one, a `.nav<name, letters and digits only>` command, each
  an `.echo` printing the NAVAID's identifier, name, type, frequency (two decimals for a VHF/UHF
  facility, e.g. `114.20`; whole kHz for an NDB-family one, e.g. `365`; blank when NASR publishes
  none) and its ARTCC high/low boundaries. A command already seen is not overwritten: its new block
  is appended, joined by a `\n---` line, so `.navABQ` lists both the VORTAC and the VOT, and
  `.navAA` both NDBs:

  ```
  .navCGT .echo \nNAVAID:\t\t\sCGT\s-\sCHICAGO HEIGHTS\n\t\t\t\tVORTAC\nFREQ:\t\t\s\s\s114.20\nARTCC\sHIGH:\t\sZAU\nARTCC\sLOW:\t\s\sZAU
  .navCHICAGOHEIGHTS .echo \nNAVAID:\t\t\sCGT\s-\sCHICAGO HEIGHTS\n\t\t\t\tVORTAC\nFREQ:\t\t\s\s\s114.20\nARTCC\sHIGH:\t\sZAU\nARTCC\sLOW:\t\s\sZAU
  ```

  Column alignment and line breaks are literal `\t`, `\n` and `\s` escapes - never real tabs,
  newlines or spaces - because CRC tokenizes an alias's replacement text on whitespace and rejoins
  it with single spaces; `AirportAliasWriter` follows the same convention. The alias file is never
  ROI-filtered, the same as every other sub-service's.
- **Fix use and chart tokens** (`FixTokens`, shared by `FixUses.Token` and `FixCharts.Token`).
  NASR's `FIX_USE_CODE` maps to a display name (`WP` → `WYPNT`); any other code keeps its own text,
  sanitized into a file-safe token (`UNKNOWN` when blank). NASR's `CHARTS` is split on commas into
  the chart names it lists; each becomes a token by collapsing every run of non-alphanumeric
  characters to a single `-` (`ENROUTE LOW` → `ENROUTE-LOW`), and a fix with no charts is grouped
  under `NO-CHART`. A fix on several charts belongs to every one of their groups.

## Messages and logging

- **`ServiceMessage`** carries a level (`Debug`, `Info`, `Success`, `Warning`, `Error`), a source
  and a text. Levels are honest: a routine notice (a buffer leg too short to shorten) is `Info`,
  so a clean run reads clean. `IsAdvisory` marks the few messages that change what the user will
  find on disk; the Review tab shows those in their own card.
- **`AppLog`** is the one log for the process. Every entry goes to the Dashboard's activity log and
  to `%APPDATA%\FE-Buddy\Logs\FE-Buddy_<date>.log` (kept 30 days). `Debug` entries are recorded
  only in developer mode.
- A last-chance handler in `App` writes any unhandled exception to `%TEMP%\febuddy-wpf-crash.txt`.

## Updates and News

- **Version check** (`VersionCheck`). Reads the GitHub releases, keeps those on the user's channel or
  above (Stable ⊂ ReleaseCandidate ⊂ Beta ⊂ Alpha), and compares by SemVer precedence. The rules
  are in [Versioning](VERSIONING.md).
- **Update** (`UpdateInstaller`). Downloads the release's MSI and runs it elevated; FE-Buddy closes,
  the MSI replaces it and relaunches it. A copy the MSI did not install (a dev build) opens the
  release page instead.
- **Installer policy.** The MSI's custom action applies `UpdatePolicy`: forward is always allowed; a
  downgrade only off a pre-release. The MSI's own version number is a disposable counter - see
  [MSI version numbering](MSI-VERSION-NUMBERING.md).
- **News** (`NewsService`). `FeBuddy/FeBuddy.Core/News.md` on `v3-development`, fetched from GitHub
  (the bundled copy when offline). Posts carry a `PostId` (`yyyy-mm-dd.N`); the newest is compared
  with `General.NewsLastOpen` to light up the News button.
- Every GitHub request works anonymously; `FEBUDDY_GITHUB_TOKEN` is only a fallback after an
  anonymous request fails.

## Design decisions

Decisions that were argued out once and should not be re-litigated without a reason:

- **One library, three layers, no DI.** Folders, not projects; static classes; no interfaces "just
  in case". Swappable pieces take a delegate or a constructor parameter where a test needs it.
- **The settings block is a plain dictionary**, read by the same parsers whether the GUI, the harness
  or a test builds it. Unknown keys warn instead of failing, which is how retired keys degrade
  gracefully - no legacy fallbacks are kept for renamed settings.
- **Production, not prototype.** No screen shows sample data; a feature that isn't built isn't shown.
- **CRC defaults are never guessed** - every value comes from the user.
- **Developer mode is a code constant**, never a user setting. Pretty printing is a user setting,
  forced on in developer mode.
- **Dirty tracking compares against a snapshot**, app-wide.
- **The Review tab is the one place** a run's results, warnings, advisories, errors and files live.
- **Every sub-service is a tab of the AIRAC Service**, never a top-level screen, and every GeoJSON
  sub-service tab is built from the same shared cards - except the alias-file ones, which ARTCC
  Boundaries, Fixes and Wx Stations opt out of (`GeojsonSubServiceViewModel.HasAliasFile`). Likewise every file
  conversion is a tab of File Conversions; the two screens share one tabbed view and differ only in
  what their view-models say (File Conversions has no General or Preview Settings tab - each
  conversion runs from its own tab).
- **Output locations are laid out in one place** (`ServiceOutputPaths`), so AIRAC output and
  converted files sit side by side under the same `FE-Buddy_Output` folder.
