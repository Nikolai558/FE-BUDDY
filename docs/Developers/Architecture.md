# Architecture

How FE-Buddy works: what happens at launch, how FAA data becomes files, and the decisions behind it.
For where the code lives, see [Code structure](Code-Structure.md).

**Contents:** [Launch](#launch) · [The AIRAC data](#the-airac-data) · [A run](#a-run) ·
[Settings](#settings) · [GeoJSON output](#geojson-output) · [Rules the data needs](#rules-the-data-needs) ·
[Messages and logging](#messages-and-logging) · [Updates and News](#updates-and-news) ·
[Design decisions](#design-decisions)

```
FeBuddy.Wpf (FE-BUDDY.exe)   FeBuddy.Harness   FeBuddy.UnitTests
            └──────────────────────┼──────────────────┘
                                   ▼
                             FeBuddy.Core ──────► FeBuddy.Versioning ◄── FeBuddy.Installer.CustomActions
                                                                                   ▲
                                                        FeBuddy.Installer (WiX) ───┘
```

`FeBuddy.UnitTests` also references `FeBuddy.Wpf`, to test the app's view-models and controls.

## Launch

`App.OnStartup` carries out a pending reset (`AppDataReset.RunPending`), starts the log file, then
runs `LaunchSequence.RunAsync` off the UI thread. A step that fails is logged and only disables what
needs it; launch never stops.

1. Clear `%TEMP%\FE-Buddy`, read `UserConfig.json` and, at the first launch, save the update
   channel (`UpdateChannelSetting.SaveDefaultIfUnset`;
   [Versioning](VERSIONING.md#pre-releases-and-channels)).
2. Look for FE-Buddy 2.x's `FEBUDDY_GITHUB_TOKEN` variable - its name only, never its value - and,
   if it is set, show a one-time notice (`LegacyGitHubTokenNotice`).
3. Uninstall a copy of FE-Buddy 2.x that Squirrel installed in `%LOCALAPPDATA%\FE-BUDDY`, by running
   its own `Update.exe --uninstall`, or remove its Installed apps entry if `Update.exe` is gone
   (`LegacySquirrelInstall`). It never runs from inside that folder, and a failure is tried again at
   the next launch.
4. Delete FE-Buddy 2.x's dead Desktop and Start menu shortcuts (`LegacySquirrelShortcuts`).
5. Get the UTC time and check the internet connection.
6. In parallel: the version check, the AIRAC data (below) and News.

Results land on `AppEnvironment`, and the AIRAC cache raises `StateChanged` as each cycle moves on.
The view-models listen, so the window fills in as launch goes: the status narrates the downloads,
the Systems box turns green, and the AIRAC Service screen unlocks.

## The AIRAC data

`AiracCycleDataCache` owns the FAA's NASR data for the session.

1. **Which cycles.** `AiracCycleResolver` works out the previous, current and next cycle from the
   28-day cadence. Cycle IDs are `YYNN`: the year it takes effect, then its number in that year.
2. **Prune.** Any other cycle in `%APPDATA%\FE-Buddy\AiracCycles` is deleted.
3. **Probe.** An HTTP HEAD on `https://nfdc.faa.gov/webContent/28DaySub/extra/<date>_CSV.zip` says
   whether the FAA has published the cycle. The next cycle often isn't yet: that is
   `NotYetPublished`, not an error.
4. **Download and parse**, current first, then previous, then next. One download and one parse run
   at a time, overlapping - two parses at once would double the peak memory. A cycle already on disk
   isn't downloaded again; a failed download gets one retry.

| Readiness | When | AIRAC Service |
|---|---|---|
| `Waiting` | a required cycle is still in progress | locked |
| `Ready` | current ready, previous and next settled | open |
| `Degraded` | current ready, previous or next failed | open; the failed cycle shows *failed* |
| `Unavailable` | the current cycle failed | locked |

Asking for a cycle while it parses waits for that parse rather than starting another.

### Data that isn't NASR

| Data | From | Kept | Fetched |
|---|---|---|---|
| Wx Stations' station list | aviationweather.gov | `WxStations\stations.cache.xml` | every run that includes Wx Stations |
| Telephony's ICAO register and U.S. special call signs | FAA Order JO 7340.2, Chapter 3 | `Telephony\telephony_register.html`, `us_special_call_signs.html` | every run that includes Telephony |
| The VATSIM-Radar Virtual Airline List | GitHub (`VATSIM-Radar/data`) | `Telephony\vatsim_radar_airlines.json` | every run whose Telephony block includes it |
| The d-TPP Metafile (Procedures) | the FAA, one per cycle | `d-tpp_Metafile.xml` in the cycle's folder | at launch, once per cycle |

- **Wx Stations and Telephony** aren't published per cycle, so one copy is kept under
  `%APPDATA%\FE-Buddy` and refreshed by every run that needs it (`AiracSharedDataLoader`, through
  `WxStationDownloader` and `TelephonyDownloader`). `SharedDataDownload.RefreshAsync` parses a new
  download before it replaces the kept copy, so a bad download never overwrites a good one. A
  failed download falls back on the kept copy with an advisory naming its age. With no copy at all,
  the sub-service writes nothing and the run carries an error; the rest of the run completes. The
  U.S. special call signs and the VATSIM-Radar list are optional: without them, Telephony goes on
  with a warning.
- **The d-TPP Metafile** is keyed by cycle, so each cycle folder gets its own
  (`DtppDownloader.EnsureCycleHasMetafileAsync`). The FAA posts it only 15-18 days before the cycle
  starts; a 404 is `NotYetPublished`, retried at the next launch, and the General tab shows the
  cycle as *partial*. `AiracCycleDataCache.GetDtppAsync` returns `null` when there is no copy, and
  Procedures treats that as an advisory, not an error.

## A run

```
Preview Settings ▸ Run AIRAC Service
  save any unsaved tab (the user confirms); stop if any tab is invalid
  each tab's BuildSettingsBlock() → Dictionary<string,string>
  the File Names tab's new names
  AIRAC_<cycle> has files? ask: Overwrite / Delete all / Cancel
        │  AiracServiceSettings
        ▼
AiracService.RunAsync
  the cycle's parsed data from the cache; the shared data the included sub-services need
  delete AIRAC_<cycle> first, if asked
  for each included sub-service, OutputDirectory = AIRAC_<cycle>:
      XxxService.Run(data, block, fileNames)
        1. XxxSettingsParser.Parse    block → typed settings + warnings
        2. XxxBuilder                 FAA rows → domain objects
        3. XxxGeojsonWriter           → .geojson files
        4. XxxAliasWriter             → alias .txt
  DuplicateAliasReport.Write          → Duplicate_Alias_Commands.txt
  CombinedAliasFileWriter.Write       → Aliases\Combined_Alias.txt
        │  AiracServiceResult
        ▼
Review tab: each tab's DescribeRunResult(result)
```

Not every sub-service has all four steps:

| Sub-service | Data | GeoJSON | Alias file |
|---|---|---|---|
| Airports, Airways, Departures, Arrivals | NASR | yes | yes |
| NAVAIDs | NASR | Symbols and Text | yes |
| ARTCC Boundaries | NASR | Lines | no |
| Fixes | NASR | Symbols and Text | no |
| Wx Stations | aviationweather.gov | Symbols and Text | no |
| Procedures | d-TPP Metafile, joined to NASR | no - writes `Procedure_Changes.md` and `Procedures.json` | `Faa_Chart_Recall.txt`, for every airport in the metafile |
| Telephony | FAA telephony pages (and virtual airlines) | no | `Telephony.txt` |
| Concatenate Aliases | the run's alias files and the user's custom alias files | no | combines them into `Combined_Alias.txt` |

- **The settings block is the contract.** Every tab, and the harness, hands Core a flat
  `Dictionary<string, string>` ([Settings reference](Settings-Reference.md)). Core never sees a
  view-model; the app never sees typed settings.
- **Problems don't lose the run.** A missing or invalid required setting throws `ArgumentException`
  (the tab's own validation normally prevents it). Anything else - a waypoint that can't be found, a
  filter that matched nothing - is a `ServiceMessage` in the result.
- **One folder per cycle.** Everything goes in `<output>[\FE-Buddy_Output]\AIRAC_<cycle>`
  (`AiracOutputPaths`). A sub-service only knows the folder it is given, so the harness and tests can
  run it on its own.
- **Files are known by their key.** A renamed file is written under its new name
  (`OutputFileNames.FileName(key)`), but everything else still refers to it by its key.
- **The duplicate report** runs once every sub-service is done. `DuplicateAliasReport` reads back
  every alias file the run wrote and lists each command used by more than one line - CRC can only
  run one - grouped by the ARTCC of the line's airport: the user's own facility first, the rest
  alphabetically, then `TELEPHONY`, then `OTHER` (airways, NAVAIDs, and anything with no known
  ARTCC). It is written whenever an
  alias file was written, even with no duplicates, so an old report never misleads.
- **Every file is ready for vNAS**, so nothing is marked for upload: GeoJSON goes in `Geojson`,
  alias files in `Aliases`.
- **`Combined_Alias.txt`** is written last, into `Aliases`, while Concatenate Aliases is in the run
  and combining (`CombineAliasFiles`, on by default). vNAS takes one alias file per facility, so
  `CombinedAliasFileWriter` writes:
  1. the first `.FeUseOnly` line any custom file has;
  2. a start line, `; ===== FE-Buddy aliases (AIRAC <cycle>) start here. …`;
  3. each alias file the run wrote, under `; ----- <name> -----`;
  4. an end line, `; ===== End of FE-Buddy aliases. …`;
  5. each custom alias file, in order.

  CRC uses the last copy of a command, so the facility's own commands win. A custom file holding
  FE-Buddy's section (last cycle's upload, reused) loses everything from the start line to the end
  line. An unreadable custom file is left out with an advisory. When no combined file is written but
  the run rewrote alias files, an old one is deleted, so it can't be uploaded by mistake.
- **File conversions** run one service each (`DatToGeojsonService`, `SctToGeojsonService`,
  `EramToGeojsonService`), one source file at a time through `ConversionFiles`: an unreadable file
  fails alone. Each writes into its own folder beside the `AIRAC_<cycle>` folders
  (`ServiceOutputPaths`).

## Settings

- **`UserConfig.json`** is one JSON tree, read at launch and addressed by dotted paths
  (`Services.AiracService.Airways.OutputBy`).
- **Each tab saves only its own node** (`UserConfigFile.Save(nodePath)`). Before it does, the node's
  old state goes to `UserConfig.previous.json` for **Undo last save**.
- **An import replaces the whole file** (`UserConfigFile.ReplaceAll`), keeping the old one as
  `UserConfig.before-import.json`. How each key travels is decided by its name; see
  [Settings export and import](Settings-Reference.md#settings-export-and-import).
- **Credentials** are never in `UserConfig.json`: they live in Windows Credential Manager, and
  settings hold only their id. See [Credentials](Credentials.md).

## GeoJSON output

- **CRC's three tiers.** CRC styles a feature from its own properties, then the file's
  **isDefaults** feature, then its built-in fallback. FE-Buddy writes one isDefaults feature at the
  head of a file from the user's CRC ERAM Defaults (`CrcFeatureFactory`), and only what differs on
  each feature. Every value is checked first (`CrcPropertyValidator`), so FE-Buddy never writes a
  value CRC can't draw. See
  [CRC GeoJSON concepts](https://github.com/KCSanders7070/CRC_GeoJson_Concepts/blob/main/CRC_Geojsons.md).
- **In the AIRAC Service, the top of each tab's CRC ERAM Defaults card picks the files** that get
  defaults - none (the default), every GeoJSON file, or specific files - sent as `CrcDefaultsFor`.
  A file conversion writes them when its panel's **Include** is ticked.
- **Defaults are never guessed.** An empty value a chosen file needs is a validation error.
- **A symbol's style can live on each feature.** `CrcSymbolDefaults.Style` may be `null`: NAVAIDs'
  merged Symbols file styled by type gives each feature its own style (`NavaidTypes.SymbolStyleFor`).
- **`feb.*` properties** are FE-Buddy's own, opt-in, and only on the kind of feature they describe.
  No lat/lon properties - the geometry has them.
- **Coordinates** are rounded to the user's precision (default 6 places) just before writing, or
  not at all (`GeojsonFileWriter.NoRounding`): each is then the shortest text that reads back to the
  same value.
- **Layout** is single-line unless the user picks pretty print, or developer mode is on.
- **Empty files aren't written**, with an advisory.

## Rules the data needs

The FAA's data has quirks. Each rule lives in one class.

- **Airway class** (`AirwayClassifier`): High, Low or Other from the **highest published altitude**
  (18,000 ft or more is High). It sets each airway's CRC defaults in a designation file. It doesn't
  pick the High and Low files: those go by designation, as the user chooses, because NASR publishes
  some V airways under one ID in both the contiguous U.S. and Hawaii, and Hawaii's 45,000 ft would put
  the mainland airway on the high map (issue #241).
- **Designation** comes from the airway ID's leading letters (`J` in `J3`), never NASR's
  `AWY_DESIGNATION` (a `Q` airway can be listed as `RN`).
- **Border markers** (`AirwayReferenceOnlyPoints`, `AirwayNormalizer`): points like
  `U.S. CANADIAN BORDER-4` aren't navigable. They have a blank `FROM_PT_TYPE` and are dropped, so the
  airway runs straight between real waypoints.
- **An airway with a waypoint that can't be found is left out entirely**, with a warning - half an
  airway is worse than none.
- **Waypoint buffer** (`AirwayWaypointBuffer`): optionally stops each leg short of a five-letter fix
  and of anything else (2.5 and 5 NM to start, 0-10 NM each). Only real waypoints are buffered, never
  the points an antimeridian split or ROI clip adds.
- **Antimeridian** (`AntimeridianSplitter`): a line crossing ±180° is split in two.
- **Shared segments** (`LineStringMerger`): paths that share segments are merged into the fewest,
  longest lines that draw each segment once - smaller files, and dashes stay dashed.
- **Symbol groups** (`SymbolFeatureMerger`, run by `GeojsonFileSet` on every file): symbols whose
  properties match exactly (`AttributesSignature`) become one MultiPoint Feature in the place of the
  first, a repeated point drawn once. Labels and isDefaults Features are never grouped. Only ERAM's
  Raw layout opts out, so it stays one Feature per element.
- **Procedure names** (`DepartureNaming`, `ArrivalNaming`): the FAA computer code without its version
  digit, matched against `AMENDMENT_NO` rather than cut at the first digit (`DOTSS2.DOTSS` → `DOTSS`,
  `1U71.LUNDI` → `1U7`). A STAR's code reads `TRANSITION.PROCEDURE`, so `AALAN.BLAID2` → `BLAID`. With
  no usable code, the name without punctuation (`O'HARE` → `OHARE`).
- **STAR direction:** a STAR is flown transition first, so its points and alias fix list put every
  transition before the bodies.
- **A STAR shared by two ARTCCs** (`ArrivalProcedure.ArtccFor`): `STAR_BASE.ARTCC` can list two
  (`ZDC ZNY` for ARLFT). Each airport's copy goes with that airport's own ARTCC
  (`APT_BASE.RESP_ARTCC_ID`), or the first listed when neither matches - its folder, `feb.artcc` and
  ARTCC filter.
- **STARs and SIDs can collide:** the cycle effective 2026-09-03 lists ORF's NUTIY and SWOPE
  departures as STARs too. The GeoJSON never clashes (arrival files carry `STAR`), but both alias
  files get the same commands. Left as the FAA has it; the duplicate report flags them.
- **NAVAIDs** (`NavaidBuilder`): from `NAV_BASE`, skipping `SHUTDOWN`. Duplicate `NAV_ID`s are normal
  (`ABQ` is a VORTAC and a VOT) and every row is kept; their alias command lists each, joined by
  `\n---`.
- **ARTCC boundary rings** (`ArtccBoundaryBuilder`): from `ARB_BASE` and `ARB_SEG`, grouped by
  location and altitude. A new ring starts wherever `POINT_SEQ` doesn't increase (ZAK's CTA then FIR
  ring) and after a point described "POINT OF BEGINNING" (ZOA's four UTA rings in one run). A ring is
  closed back to its first point. A location with no `ARB_SEG` rows - the Canadian, foreign and
  CERAP entries - draws nothing.
- **Fix use and chart names** (`FixUses`, `FixCharts`): `FIX_USE_CODE` maps to a name (`WP` →
  `WYPNT`); `CHARTS` splits on commas, each name collapsing punctuation and spaces to `-`
  (`ENROUTE LOW` → `ENROUTE-LOW`); a fix on no chart is `NO-CHART`.
- **Wx stations** (`WxStationBuilder`): a US or US-territory station with an ICAO ID that reports
  METAR and has real coordinates (the feed's `-99.99` placeholder is left out).
- **Alias text uses `\t`, `\n` and `\s`**, never real tabs, newlines or spaces: CRC splits an alias on
  whitespace and rejoins it with single spaces.

## Messages and logging

- **`ServiceMessage`** has a level (`Debug`, `Info`, `Success`, `Warning`, `Error`), a source and a
  text. Levels are honest: a routine notice is `Info`, so a clean run reads clean. `IsAdvisory` marks
  the few that change what the user will find on disk; the Review tab shows them in their own card.
- **`AppLog`** is the one log: the Dashboard's activity log and
  `%APPDATA%\FE-Buddy\Logs\FE-Buddy_<date>.log`, kept 30 days. `Debug` entries are recorded only in
  developer mode. The Dashboard's **Clear** clears `DisplayEntries`; `Entries` and the file keep
  everything.
- **A crash** is written to `%APPDATA%\FE-Buddy\Logs\febuddy-wpf-crash.txt` by a last-chance handler
  in `App`.

## Updates and News

- **Version check** (`VersionCheck`): reads GitHub's releases, keeps those on the user's channel or
  more stable, and compares by SemVer. See [Versioning](VERSIONING.md).
- **Update** (`UpdateInstaller`): downloads the release's MSI, which the update window runs
  elevated; FE-Buddy closes, and the MSI replaces it and relaunches it from its last page. A copy the
  MSI didn't install opens the release page instead.
- **Uninstall** (`AppUninstall`) starts `msiexec /x` **not elevated**, as Windows Settings does, so
  Windows Installer asks for administrator rights itself. Started elevated, a standard user typing
  an administrator's password would run the cleanup as that administrator, clearing the wrong
  account's data and credentials.
- **News** (`NewsService`): `News.md` at the repository root on `v3-development`, or the copy built
  into FeBuddy.Core when offline. The newest post's `PostId` (`yyyy-mm-dd.N`) is compared with
  `General.NewsLastOpen` to light up the News button.
- **GitHub requests work without a token.** When the user picks one (Settings ▸ FE-Buddy's GitHub
  Requests), the update check, News and update download use it, then try once more without it if
  that fails (`GitHubAuth`).

## Design decisions

Argued out once; don't re-open them without a reason.

- **One library, three layers, no DI.** Folders, not projects. A swappable piece takes a delegate or
  parameter where a test needs it.
- **The settings block is a plain dictionary**, read by the same parsers whoever builds it. Unknown
  keys warn rather than fail. No fallbacks are kept for renamed settings.
- **Nothing is shown that isn't built.** No screen shows sample data.
- **CRC defaults are never guessed.**
- **Developer mode is a code constant** (`App.DevModeEnabled`), never a user setting. Pretty
  printing is a user setting, forced on in developer mode.
- **"Unsaved" means different from the last save**, everywhere.
- **A run's results, warnings and output folder are in one place**: the Review tab, or, for a file
  conversion, under its Run card.
- **Every sub-service is a tab of the AIRAC Service**, and every file conversion a page of File
  Conversions, built from the same shared cards where they apply.
- **Output folders are laid out in one place** (`ServiceOutputPaths`), so AIRAC output and
  conversions sit side by side.
