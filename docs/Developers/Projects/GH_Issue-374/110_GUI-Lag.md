# 110 · GUI lag: work done on the UI thread

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

Work the GUI does on the UI thread. Timings are estimates: nothing could be profiled. Most of these share a fix: build the per-cycle lists, the d-TPP index, the GeoJSON reading and the map layers in **Core**, off the UI thread, and cache them. Session [S3](020_Work-Plan.md#s3) covers the AIRAC Service and File Conversions screens; [S4](020_Work-Plan.md#s4) covers the Map, Dashboard, Settings and startup.

**29 findings** (2 High, 10 Med, 17 Low). Quote the ID when you comment on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374).

## Summary

| ID | Kind | Sev | Finding | Status | Session |
|---|---|---|---|---|---|
| [U1-1](#u1-1) | GUI lag | High | Every tab's cycle-dependent lists are built on the UI thread on each cycle load/switch | Fixed | S3 |
| [U1-2](#u1-2) | GUI lag | High | Telephony reads and merges the virtual-airline list on the UI thread with O(N²) Core code | Fixed | S3 |
| [C6-30](#c6-30) | GUI lag | Med | UserConfigFile.CanUndo reads and parses a file on every command requery | Fixed | S3 |
| [C7-4](#c7-4) | GUI lag | Med | NasrCycleDownloader has no `ConfigureAwait(false)`, so unzipping a cycle can run on the UI thread | Fixed | S4 |
| [U1-9](#u1-9) | GUI lag | Med | Map panning rebuilds all visible geometry on every mouse move | Fixed | S4 |
| [W7-1](#w7-1) | GUI lag | Med | Base map GeoJSON (1.5 MB) is parsed on the UI thread the first time any map opens | Fixed | S4 |
| [W1-3](#w1-3) | GUI lag | Med | Add Custom Aliases reads and stats custom alias files on every keystroke and validation pass | Fixed | S3 |
| [W3-3](#w3-3) | GUI lag | Med | Dashboard activity log is O(n) per entry and never trimmed | Fixed | S4 |
| [W3-1](#w3-1) | GUI lag | Med | Every map click does a full, synchronous settings-file round trip (and logs a line) | Fixed | S4 |
| [W1-1](#w1-1) | GUI lag | Med | Procedure Charts indexes the whole d-TPP Metafile on the UI thread, again on every General-tab click | Fixed | S3 |
| [U1-6](#u1-6) | GUI lag | Med | File Conversions counts (and opens) source files on the UI thread per keystroke and at startup | Fixed | S3 |
| [U1-7](#u1-7) | GUI lag | Med | Run review message lists are not virtualized and format every message on the UI thread | Fixed | S3 |
| [W7-2](#w7-2) | GUI lag | Low | A pending reset runs on the UI thread before any window: up to 15 s wait plus deleting all app data, with nothing on screen | Fixed | S4 |
| [W6-6](#w6-6) | GUI lag | Low | The Alias Command Guide and News visual trees are rebuilt and re-parsed every time their page opens | Fixed | S3 |
| [W7-7](#w7-7) | GUI lag | Low | Opening any file or folder picker checks folder existence on the UI thread; an offline network folder stalls it | Fixed | S4 |
| [W2-27](#w2-27) | GUI lag | Low | Small synchronous disk touches on the UI thread | Fixed | S3 |
| [W2-6](#w2-6) | GUI lag | Low | One `File.Exists` per written file on the UI thread after a stopped run | Fixed | S3 |
| [W3-4](#w3-4) | GUI lag | Low | News posts are cleared and re-rendered (Markdown re-parsed) on every environment change | Fixed | S4 |
| [U1-14](#u1-14) | GUI lag | Low | Duplicate alias review re-checks all groups against all groups on every keystroke | Fixed | S3 |
| [W3-5](#w3-5) | GUI lag | Low | Output picker filter and listing do per-item work on the UI thread | Fixed | S4 |
| [W3-8](#w3-8) | GUI lag | Low | ROI state properties reload the saved ROI 4-5 times per change | Fixed | S4 |
| [W1-5](#w1-5) | GUI lag | Low | Getters and CanExecute recompute the same lookups several times per keystroke | Fixed | S3 |
| [W4-5](#w4-5) | GUI lag | Low | Review tab adds every written file to an ObservableCollection one by one, but only shows the count | Fixed | S3 |
| [W4-4](#w4-4) | GUI lag | Low | What You'll Get is built twice and its card rebuilt on every edit, even when nothing in it changed | Fixed | S3 |
| [T3-6](#t3-6) | GUI lag | Low | Switching, creating, renaming or deleting a settings profile does file I/O and reloads every page on the UI thread | Fixed | S4 |
| [C6-31](#c6-31) | GUI lag | Low | Settings import reads and plans on the UI thread (Core work, Wpf call site) | Fixed | S4 |
| [U1-13](#u1-13) | GUI lag | Low | Settings facility list runs its 20k-row LINQ on the UI thread when the cycle is already cached | Fixed | S4 |
| [W3-7](#w3-7) | GUI lag | Low | System health refresh hits the disk on every cycle state change | Fixed | S4 |
| [U1-15](#u1-15) | GUI lag | Low | Switching AIRAC Service tabs rebuilds the whole tab view | Fixed | S3 |

## High

<a id="u1-1"></a>

### U1-1 · GUI lag · High · Every tab's cycle-dependent lists are built on the UI thread on each cycle load/switch

**Status:** Fixed (not yet merged), as proposed: Core's `AiracCycleOptionListBuilder` works out every tab's option lists (`AiracCycleOptionLists`) once per cycle and keeps them with its data; `LoadCycleDataAsync` builds them with `Task.Run` before it goes to the UI thread, and `ISubServiceRunTarget.LoadCycleDependentLists` now takes the lists, so the tabs only fill their toggles. Switching back to a cycle reuses its lists. Tests: `AiracCycleOptionListBuilderTests` · **Session:** [S3](020_Work-Plan.md#s3)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:419-440`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L419-L440); [`FeBuddy/FeBuddy.Wpf/ViewModels/FixesViewModel.cs:239-338`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/FixesViewModel.cs#L239-L338); [`FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs:888-952`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs#L888-L952); [`FeBuddy/FeBuddy.Wpf/ViewModels/ArtccBoundariesViewModel.cs:126-168`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ArtccBoundariesViewModel.cs#L126-L168); [`FeBuddy/FeBuddy.Wpf/ViewModels/AirwaysViewModel.cs:209-237`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AirwaysViewModel.cs#L209-L237); [`FeBuddy/FeBuddy.Wpf/ViewModels/NavaidsViewModel.cs:149-179`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/NavaidsViewModel.cs#L149-L179); [`FeBuddy/FeBuddy.Wpf/ViewModels/ArrivalsViewModel.cs:240-260`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ArrivalsViewModel.cs#L240-L260); [`FeBuddy/FeBuddy.Wpf/ViewModels/DeparturesViewModel.cs:243-262`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DeparturesViewModel.cs#L243-L262)

**Problem:** when the AIRAC Service page opens (once the cache is ready) and every time the user picks another cycle on the General tab, all sub-service tabs scan the raw NASR tables inside one UI-thread dispatcher item. Fixes makes two full passes over ~70k FIX_BASE rows (fix-use names + `FixCharts.Parse`, which allocates a List and HashSet per row, then `FixCharts.Token` per chart); Procedures makes two passes over ~20k APT_BASE rows (facility list + two lookup dictionaries); ARTCC Boundaries scans ARB_SEG twice with `Trim().ToUpperInvariant()` per row; Airways, Navaids, STARs and DPs each scan their base table; each tab then runs `RefreshOutputFiles`/`Revalidate`/`ResyncSavedState`. Est. 60-150 ms in one frame (longer on slow PCs), i.e. a visible freeze right when the page appears or the cycle drop-down closes.

**Proposed:** compute the lists off the UI thread. Add Core class `FeBuddy.Core.Application.Airac.AiracCycleOptionLists` with `static AiracCycleOptionLists Build(NasrCsvDataCollection data)` returning: fix-use names, chart names (+ "has a fix with no chart"), procedure facility IDs and the APT ident/FAA-ID lookups, ARB location IDs and (location, altitude) pairs, airway designations, NAVAID types (non-shutdown), STAR ARTCCs, DP ARTCCs. In `LoadCycleDataAsync` call `var lists = await Task.Run(() => AiracCycleOptionLists.Build(data));` before the `BeginInvoke`, and change `ISubServiceRunTarget.LoadCycleDependentLists` to take `(NasrCsvDataCollection data, AiracCycleOptionLists lists)` so each tab only fills its ObservableCollections. Optionally memoize the result per cycle in `AiracCycleDataCache` (the data never changes for a cycle), which also makes switching back to a cycle instant.

**Note:** Also found as [W1-4](#w1-4), [W2-2](#w2-2), [W4-3](#w4-3), [T3-1](#t3-1).

<a id="u1-2"></a>

### U1-2 · GUI lag · High · Telephony reads and merges the virtual-airline list on the UI thread with O(N²) Core code

**Status:** Fixed (not yet merged), as proposed: `Merge` looks the GNG rows up by ICAO in a dictionary and `Select` keeps a set of the airlines it has listed, so both are one pass. The tab reads the kept VATSIM-Radar copy with `Task.Run` and shows it on the UI thread, unless a download has replaced it meanwhile; after a download, and for the "Telephony data" line, the files are read off the UI thread too. Tests: `VatsimRadarVirtualAirlinesTests` (tens of thousands of airlines merge and select at once), `TelephonyViewModelTests` · **Session:** [S3](020_Work-Plan.md#s3)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs:97`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs#L97); [`FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs:821`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs#L821); [`FeBuddy/FeBuddy.Core/Application/Airac/Telephony/VatsimRadarVirtualAirlines.cs:44-67`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/VatsimRadarVirtualAirlines.cs#L44-L67); [`FeBuddy/FeBuddy.Core/Application/Airac/Telephony/VatsimRadarVirtualAirlines.cs:72-95`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/VatsimRadarVirtualAirlines.cs#L72-L95)

**Problem:** the Telephony tab is constructed with the AIRAC Service page (`BuildSubServiceTabs`), and its constructor synchronously reads and parses both kept copies (GNG, requested with `rows=10000` in `TelephonyFiles.cs:25`, plus VATSIM-Radar's list), then `Merge` does a linear `FindIndex` over the GNG rows for every VATSIM-Radar airline (O(G×V)) and `Select` does a linear `FindSame` over the growing result for every airline (O(N²), each comparison trimming 2-6 strings). With thousands of rows that is millions of comparisons: est. 100 ms to over 1 s on the UI thread when the AIRAC Service page first opens, and again after every "Download latest list" (line 821 runs on the UI continuation of the download).

**Proposed:** Core: in `Merge` build a `Dictionary<string,int>` (trimmed ICAO, `StringComparer.OrdinalIgnoreCase`) over the GNG rows once; in `Select` keep a `HashSet<(string,string,string)>` of trimmed (designator, telephony, organization) with an OrdinalIgnoreCase tuple comparer instead of `FindSame`. Both become O(N). GUI: load off the UI thread like `LoadRealOperators` does: `Task.Run(() => VatsimRadarVirtualAirlines.ReadKeptCopy()).ContinueWith(t => _dispatcher.BeginInvoke(() => UseVatsimRadarCopy(t.Result)), OnlyOnRanToCompletion)`, and in `DownloadVatsimRadarListAsync` use `UseVatsimRadarCopy(await Task.Run(() => VatsimRadarVirtualAirlines.ReadKeptCopy(...)))`.

**Note:** Also found as [W1-2](#w1-2), [C3-2](#c3-2).

## Med

<a id="c6-30"></a>

### C6-30 · GUI lag · Med · UserConfigFile.CanUndo reads and parses a file on every command requery

**Status:** Fixed (not yet merged), as proposed: `UserConfigFile` keeps the undo snapshot in memory, read once per profile, and `Save`, `Undo`, `ReplaceAll`, bringing a file forward, renaming the profile and moving an older FE-Buddy's file in keep it up to date. `CanUndo` reads no file. Tests: `UserConfigFileTests` · **Session:** [S3](020_Work-Plan.md#s3)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:486-498`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L486-L498); [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/TabbedServiceViewModel.cs:41-43`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/TabbedServiceViewModel.cs#L41-L43); [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceSettingsViewModel.cs:41`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceSettingsViewModel.cs#L41), 58

**Problem:** `RelayCommand.CanExecuteChanged` is `CommandManager.RequerySuggested` (Mvvm/RelayCommand.cs:19-22), which WPF raises on the UI thread after every key press, click and focus change. The button is bound in TabbedServiceView.xaml:115 and FileConversionsView.xaml:155. So once any tab has been saved, every keystroke in an AIRAC Service tab or a conversion page reads `UserConfig-previous.<Profile>.json` from disk and parses it. That file holds one subtree per saved node, up to the profile's size (about 23 KB in the v2 fixture). Disk or antivirus latency then lands on typing.

**Proposed:** Move it into Core memory. UserConfigFile keeps the parsed snapshot (or just the set of node paths it holds) in a private static field. Load it in ReadAll, update it where Save and Undo write the file, and clear it where ReplaceAll and BringForward delete it. CanUndo then checks in memory with no I/O, and the Wpf needs no change.

**Note:** Also found as [W4-1](#w4-1), [U1-4](#u1-4).

<a id="c7-4"></a>

### C7-4 · GUI lag · Med · NasrCycleDownloader has no `ConfigureAwait(false)`, so unzipping a cycle can run on the UI thread

**Status:** Fixed (not yet merged), as proposed: both awaits in `NasrCycleDownloader` have `ConfigureAwait(false)`, and the cache's download and parse yield to the thread pool (`ConfigureAwaitOptions.ForceYielding`) rather than to the caller's context. Tests: `AiracCycleDataCacheTests` (a cycle loaded on demand from a context standing for the UI thread's downloads and parses on the pool; it fails with the old `Task.Yield`) · **Session:** [S4](020_Work-Plan.md#s4)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Nasr/NasrCycleDownloader.cs:136-148`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/NasrCycleDownloader.cs#L136-L148), 157-165; [`FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs:701`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs#L701), 798; [`FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs:598`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs#L598)

**Problem:** These are the only awaits here without `.ConfigureAwait(false)`. After them, the method deletes the old folder, opens the zip and extracts every CSV (`ExtractCsvEntriesFlattened`, a few hundred MB) on whatever context it started on. The UI thread does reach it: `MapLayersState` calls `AiracCycleDataCache.Instance.GetAsync(cycleId)` on the UI thread. For a cycle in the `Failed` state, the cache retries the download (`RunFlightAsync` → `WaitForDownloadAsync(retryFailed: true)` → `DownloadWhenUnlockedAsync`). That method starts with `await Task.Yield()`, which posts back to the WPF dispatcher. `DownloadStageAsync` then skips the probe (the cycle is already probed) and calls `_download` → `NasrCycleDownloader` synchronously on the UI thread. The HTTP awaits capture that context, so the unzip runs on the dispatcher and the window freezes for several seconds. The same can happen through AiracServiceViewModel.cs:232/418: `GetAsync` itself starts synchronously on the UI thread, so the caller's own `ConfigureAwait(false)` does not help.

**Proposed:** Add `.ConfigureAwait(false)` to both awaits (lines 136-137 and 141-148). For the cache owner: replace `await Task.Yield();` at AiracCycleDataCache.cs:701 and 798 with `await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);`, which yields to the thread pool and not to the caller's SynchronizationContext.

<a id="u1-9"></a>

### U1-9 · GUI lag · Med · Map panning rebuilds all visible geometry on every mouse move

**Status:** Fixed (not yet merged), as proposed, first part: while a drag pans the map, the drawing (and the highlight and the ROI box with it) is moved with one `TranslateTransform` rather than drawn again; it is drawn in full once the drag ends, pauses for 100 ms, or has gone a third of the way across, and at once if the map is zoomed mid-drag. The background, the gridlines and their labels aren't moved: they are few, so they are drawn again for the view on every mouse move, and the labels stay on the map's edges. Not measured: that needs the running app. Caching each layer's geometry per zoom, the longer-term idea, isn't done. Tests: `MapCanvasTests` · **Session:** [S4](020_Work-Plan.md#s4)

**Where:** [`FeBuddy/FeBuddy.Wpf/Controls/MapCanvas.cs:606-613`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/MapCanvas.cs#L606-L613),878-879,882-941,989-1029

**Problem:** each pan step queues a full redraw (coalesced at Render priority): every visible run is re-projected to screen, re-clipped and written into a new `StreamGeometry`, and points/labels are re-placed. Zoomed out, that is every vertex: the base map alone is ~79k vertices (`coastlines.json` 67,028, `us-states.json` 11,977, counted from the assets) plus loaded output layers (tens of MB of GeoJSON for airways/boundaries/video maps). Est. 10-40 ms of UI-thread work per frame plus render-thread tessellation, so panning with several layers on runs well below 60 fps. (Estimate; measure with the WPF Performance Suite before changing.)

**Proposed:** during a drag, move the existing drawing with a `TranslateTransform` on `_worldVisual` and do the full `Redraw` once on mouse-up (or when the pointer pauses ~100 ms); longer-term, cache each layer's geometry per zoom level in world coordinates and pan by changing only the transform.

**Note:** Also found as [W6-5](#w6-5).

<a id="w7-1"></a>

### W7-1 · GUI lag · Med · Base map GeoJSON (1.5 MB) is parsed on the UI thread the first time any map opens

**Status:** Fixed (not yet merged), as proposed: `BaseMap` reads and parses each layer's file on its own (`Read`, any thread) and makes the layer with the theme's brush on the UI thread (`Make`). `BaseMap.Preload` runs with `Task.Run` once FE-Buddy is idle after opening, so the first map finds the shapes parsed; a map that asks meanwhile waits for the same parse. A file that can't be read is logged as a warning. `GeoJsonReader` stays in the Wpf: it reads GeoJSON into the map's own drawing model. Not unit tested: it needs FE-Buddy's resources, which the tests don't load · **Session:** [S4](020_Work-Plan.md#s4)

**Where:** [`FeBuddy/FeBuddy.Wpf/Map/BaseMap.cs:38-65`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Map/BaseMap.cs#L38-L65); [`FeBuddy/FeBuddy.Wpf/ViewModels/BaseMapSettings.cs:40-51`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/BaseMapSettings.cs#L40-L51),126-141; [`FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs:131`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs#L131)

**Problem:** `Assets/BaseMap/coastlines.json` (1,263,054 bytes) and `us-states.json` (231,310 bytes) are embedded `Resource`s (csproj:51) loaded through a `Lazy<>` on first use. First use is `BaseMapSettings`' constructor, built by `MapLayersState.Shared`, which `MapViewModel`'s constructor touches: so the first open of the Map page or of any ROI picker parses ~1.5 MB of JSON and builds every geometry on the UI thread before the window can render. A layer switched on later (`OnLayerToggled`) loads in the click handler, also on the UI thread. A failure is swallowed silently (`catch { return null; }`, line 61) with nothing logged.

**Proposed:** Pre-load both layers off the UI thread once FE-Buddy is idle after opening (ShellWindow already schedules `PrepareFileConversions` at `ApplicationIdle`; add e.g. `_ = Task.Run(BaseMap.Preload)` there). Split `Load` so the background part only reads and parses (`GeoJsonReader.Read`), and the theme brush (`Application.Current.TryFindResource`, UI-thread object) is applied on the UI thread or replaced by a frozen brush. Log the caught exception (`AppLog.Warning("Map", …)`).

**Note:** Also found as [W3-2](#w3-2), [W6-1](#w6-1), [U1-12](#u1-12), [T3-5](#t3-5).

<a id="w1-3"></a>

### W1-3 · GUI lag · Med · Add Custom Aliases reads and stats custom alias files on every keystroke and validation pass

**Status:** Fixed (not yet merged): Core's `AliasSourceLoader.InspectLocalFile` says whether a local file is missing, unreadable, or has alias commands (`LocalAliasFileState`). The tab checks every local file with `Task.Run` when its list loads or changes, and 400 ms after the user stops typing a location, rather than on LostFocus, so a pasted path is checked too; `Validate` and the row hints only read the results. `CheckAsync` loads the sources with `Task.Run`. The Credential Manager list (`RefreshCredentials`) is still read on the UI thread: it is a local call that returns at once, and it runs on construction and when a credential changes, not per keystroke. Tests: `AliasSourceLoaderTests`, `ConcatenateAliasesViewModelTests` · **Session:** [S3](020_Work-Plan.md#s3)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs:346-358`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs#L346-L358), 604-636, 642-656, 691-710, 392-407

**Problem:** `Validate` calls `LocalFileErrorOf`, which does a `FileInfo` stat on every local row. The first time it sees a path, or when the file has changed, it also runs `File.ReadAllText` over the whole file (often last cycle's `Combined_Alias.txt`, hundreds of KB). `RefreshRowHints` does `File.Exists` per row. The location box is `UpdateSourceTrigger=PropertyChanged` (ConcatenateAliasesView.xaml:136), and `AliasSourceRow.Location`'s setter calls `RowChanged` → `RefreshRowHints` + `MarkDirty` → `Validate`. So every keystroke stats every file, and reads any file the typed path now matches. Other triggers: tab construction (first AIRAC Service open, via `LoadFromConfig` → `ClearDirty`), every toggle, and every revalidate. A path on a network share, a disconnected mapped drive or a cloud placeholder can block each call for seconds. `RefreshCredentials` enumerates Credential Manager on the UI thread, at construction and on each store change. `CheckAsync` runs `AliasSourceLoader.ReadFileAsync`'s `File.Exists` before its first await.

**Proposed:** Do the local-file check off the UI thread with a Core helper (e.g. `AliasSourceLoader.CheckLocalFileAsync(path)` returning exists/readable/has-commands). Start it when a location is committed (LostFocus, Browse, Add, load), not per keystroke. `Validate` and `RefreshRowHints` should only read the cached results. Wrap `CheckAsync`'s load in `Task.Run`, and read `_store.List()` in `Task.Run`.

**Note:** Also found as [U1-5](#u1-5), [C3-27](#c3-27), [T3-4](#t3-4).

<a id="w3-3"></a>

### W3-3 · GUI lag · Med · Dashboard activity log is O(n) per entry and never trimmed

**Status:** Fixed (not yet merged), as proposed: entries are queued as they're logged and shown in one dispatcher pass at Background priority; each level's count is kept as entries come and go; the log shows the newest 2,000 (`MaxShownEntries`) - the file and `AppLog.Entries` keep every one. Tests: `DashboardViewModelTests` · **Session:** [S4](020_Work-Plan.md#s4)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs:37`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs#L37), 186-199, 201-206, 305-312

**Problem:** Every AppLog entry is marshalled with its own `BeginInvoke`, inserted at index 0 of an ObservableCollection behind a filtered ListCollectionView, and then 4 `Count()` scans of the whole list run (one per chip). Core writes every ServiceMessage of every sub-service to AppLog (AiracService.cs:321/550/647 and each XxxService), so a full AIRAC run adds hundreds to thousands of entries: O(n^2) work on the UI thread during the run, even while the Dashboard isn't on screen (the view-model lives all session). Nothing trims `_allLog`; in developer mode the Debug entries (e.g. UserConfigFile.GetValue's "Config key ... is not set" on every miss) make it far larger.

**Proposed:** Keep per-level counters (increment on add, zero on Clear) instead of `Count()`; queue incoming entries and flush them in one dispatcher operation at `DispatcherPriority.Background`; cap the displayed list (e.g. newest 2,000 - the file and `AppLog.Entries` keep everything).

**Note:** Also found as [U1-10](#u1-10).

<a id="w3-1"></a>

### W3-1 · GUI lag · Med · Every map click does a full, synchronous settings-file round trip (and logs a line)

**Status:** Fixed (not yet merged), as proposed: the map's settings (`MapLayersState` and `BaseMapSettings`, the opacity included) go through one `DeferredSave`: 400 ms after the last change, the keys that changed are read from the page on the UI thread and saved together with `Task.Run`, one save after another, and a change still waiting when FE-Buddy closes is saved then. Core's new `UserConfigFile.SaveValues` sets the values and saves the node under one lock, logs it at Debug level when asked, and doesn't read the files again afterwards, so a setting another page has set but not saved yet is never dropped; a save chosen in one profile isn't made once another is in use. Set Home saves at once and says so when it's saved. Tests: `UserConfigFileTests`, `DeferredSaveTests` · **Session:** [S4](020_Work-Plan.md#s4)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs:185-186`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs#L185-L186), 433-434, 535-536, 761-765; [`FeBuddy/FeBuddy.Wpf/ViewModels/BaseMapSettings.cs:93-94`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/BaseMapSettings.cs#L93-L94), 126-130, 133-138

**Problem:** Each tick in the output picker, each live-layer switch, each base-map layer or gridline switch, each Set/Reset home calls `UserConfigFile.Save("Services.MapService")` on the UI thread. `Save` (Core UserConfigFile.cs:395-431) reads the profile file, reads and rewrites `UserConfig-previous.<Profile>.json`, rewrites the profile file, then `ReadAll()` re-reads and re-parses Shared.json and the whole profile (which can hold large values such as `DuplicateAliases.Choices`), and finally writes `AppLog.Info("Saved 'Services.MapService'.")`, so every map click also adds a line to the Dashboard activity log. Ticking ten files one by one = ten disk round trips on the UI thread and ten log lines. It also overwrites the Map node's "previous" backup on each click.

**Proposed:** Debounce map-state saves the way the opacity already is (one `DispatcherTimer`, ~400 ms, shared by MapLayersState and BaseMapSettings, flushing `TrySetValue` values with one `Save(Node)`), and run that save with `Task.Run` (UserConfigFile is lock-protected). Consider a Core `UserConfigFile.Save(nodePath, quiet: true)` (or Debug-level log) for UI-state nodes so the activity log isn't filled with "Saved 'Services.MapService'".

**Note:** Also found as [U1-11](#u1-11).

<a id="w1-1"></a>

### W1-1 · GUI lag · Med · Procedure Charts indexes the whole d-TPP Metafile on the UI thread, again on every General-tab click

**Status:** Fixed (not yet merged): Core's `ProcedureNameIndex` indexes the metafile (airports by FAA/ICAO ID, departures and arrivals by name, root and airport, the counts), built with `Task.Run` in `LoadProceduresDataAsync` and kept per metafile; `SetDtppData` takes the index. The screen reads the metafile again only when the cycle or the previous cycle's state changes, not on every General-tab click. Core still parses it fresh per request, as it documents, rather than keep three 16 MB metafiles in memory. Tests: `ProcedureNameIndexTests` · **Session:** [S3](020_Work-Plan.md#s3)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs:553-634`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs#L553-L634)

**Problem:** `SetDtppData` runs inside `_dispatcher.BeginInvoke` (AiracServiceViewModel.cs:499-506), so all of it is UI-thread work. It walks every metafile airport (~3,000) and makes two passes over every chart record (15-20k; the file is up to 16 MB), running the generated regexes in `IsContinuation`, `BaseName` and `RootName` on each one. It also groups and sorts per airport, sorts the whole name list, and rebuilds the name rows. It runs when the screen opens and on each cycle switch. It also runs on every Include or output checkbox click on the General tab: `SubServicesChanged` → `SyncSubServiceTabs` → `RefreshProceduresData` (AiracServiceViewModel.cs:70, 298). Each of those clicks also re-parses the 16 MB XML off-thread, because `GetDtppAsync` doesn't cache.

**Proposed:** Move the lookups into Core, e.g. `Domain/Procedures/ProcedureNameIndex.Build(DtppMetafileDataCollection)`. It would return airports by FAA/ICAO ident, each airport's departure/arrival base names in chartseq order, names by root, the sorted option list and the airport and chart counts. Build it in `AiracServiceViewModel.LoadProceduresDataAsync` before the `BeginInvoke`, and have `SetDtppData` take the index. Also skip the rebuild when the cycle ID and metafile are the ones already shown (and tell the AiracServiceViewModel reviewer about the uncached re-parse).

**Note:** Also found as [W2-3](#w2-3), [U1-8](#u1-8), [T3-2](#t3-2).

<a id="u1-6"></a>

### U1-6 · GUI lag · Med · File Conversions counts (and opens) source files on the UI thread per keystroke and at startup

**Status:** Fixed (not yet merged), as proposed: Core's `ConversionSourceFolder.Count` counts the folder (`SourceFolderCount`). The page runs it with `Task.Run` when it loads, when a folder is picked, and 400 ms after the user stops typing one; a newer count cancels the one before - and typing a folder drops a count already finished but not yet shown - and the page says "Counting files…" and holds the run meanwhile. Also fixes [W4-7](100_Bugs.md#w4-7). Tests: `ConversionSourceFolderTests`, `EramToGeojsonViewModelTests` · **Session:** [S3](020_Work-Plan.md#s3)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/FileConversionTabViewModel.cs:119-130`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/FileConversionTabViewModel.cs#L119-L130),361,464,468-486; [`FeBuddy/FeBuddy.Wpf/Views/Cards/SourceFilesCard.xaml:43`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/Cards/SourceFilesCard.xaml#L43); [`FeBuddy/FeBuddy.Wpf/ViewModels/EramToGeojsonViewModel.cs:77`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/EramToGeojsonViewModel.cs#L77); [`FeBuddy/FeBuddy.Wpf/Views/ShellWindow.xaml.cs:42-60`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ShellWindow.xaml.cs#L42-L60)

**Problem:** `SourceFolder` is bound with `UpdateSourceTrigger=PropertyChanged`, so every keystroke runs `Directory.Exists` and enumerates the folder; for ERAM, `IsSourceFile` is `EramGeoMapReader.IsGeoMapsFile`, which opens every `.xml` to read its root element. It also runs from `LoadFromConfig` (line 361), which `PrepareFileConversions` triggers at `ApplicationIdle` right after the main window appears: a saved folder on a disconnected network drive or a slow share freezes the freshly opened window for seconds (SMB timeouts), and a big adaptation export folder costs one file open per XML (est. 1-5 ms each).

**Proposed:** move the count to Core as `ConversionSourceFolder.Count(string folder, IReadOnlyCollection<string> extensions, Func<string,bool> isSourceFile, CancellationToken ct)`, call it via `Task.Run` with the previous count cancelled, debounced ~300 ms after typing; `FolderSummary` shows "Counting files..." meanwhile.

**Note:** Also found as [W4-2](#w4-2), [W2-4](#w2-4), [T3-3](#t3-3).

<a id="u1-7"></a>

### U1-7 · GUI lag · Med · Run review message lists are not virtualized and format every message on the UI thread

**Status:** Fixed (not yet merged): each sub-service's warnings and routine messages are one flat list, each group's heading then its messages (`SubServiceRunResult.AttentionRows`, `InfoRows`), in a virtualized `ItemsControl` with the same 320 px cap, so only the rows on screen are built and formatted. The group panels look as before (`Radius.Chip.Top`, `Radius.Chip.Bottom`). `FilesWritten` is a count, set once ([W4-5](#w4-5)). The messages are still grouped on the UI thread when the run finishes: it takes milliseconds (50,000 messages in well under a second), and several tabs' `DescribeRunResult` read the tab's own settings, which must not be read from another thread. The inline-code formatting stays in the Wpf: it decides how text looks. Tests: `ServiceRunReviewTabViewModelTests` · **Session:** [S3](020_Work-Plan.md#s3)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/ServiceRunReviewTabView.xaml:78-84`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ServiceRunReviewTabView.xaml#L78-L84),110-122; [`FeBuddy/FeBuddy.Wpf/Behaviors/InlineCode.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Behaviors/InlineCode.cs); [`FeBuddy/FeBuddy.Wpf/Converters/FileNamesAsCodeConverter.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Converters/FileNamesAsCodeConverter.cs); [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceRunReviewTabViewModel.cs:240-243`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceRunReviewTabViewModel.cs#L240-L243),272; [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:605-607`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L605-L607)

**Problem:** expanding a sub-service's Attention/Info list realizes every group and every message at once (ItemsControl inside a ScrollViewer with MaxHeight 320, no virtualization); each message runs the `FileNamesAsCode` regex and `InlineCode` builds Runs (with a FontSize binding per code run). A sub-service with a few thousand messages makes the expand click freeze (est. 200-800 ms per 2,000 messages). Also on the UI thread after each run: `DescribeRunResult` grouping every message (Airways runs a regex per message), and `FilesWritten.Add` one file at a time (thousands of CollectionChanged + `Count` updates) although the view only binds `FilesWritten.Count` (XAML:202).

**Proposed:** flatten group headers + messages into one list shown in an `ItemsControl`/`ListBox` with `VirtualizingStackPanel.IsVirtualizing="True"`, `ScrollViewer.CanContentScroll="True"`, `VirtualizationMode="Recycling"` and the same MaxHeight; precompute the inline segments in Core (e.g. `ServiceMessageText.Split(string)` returning text/code segments) while building `SubServiceRunResult` off the UI thread (`Task.Run` around the `targets.Select(t => t.DescribeRunResult(result))` step); make `FilesWritten` a plain list set once with a `FilesWrittenCount` property.

**Note:** Also found as [W6-7](#w6-7), [W5-4](#w5-4).

## Low

<a id="w7-2"></a>

### W7-2 · GUI lag · Low · A pending reset runs on the UI thread before any window: up to 15 s wait plus deleting all app data, with nothing on screen

**Status:** Fixed (not yet merged), as proposed: when a reset is waiting (`AppDataReset.IsPending`), FE-Buddy's mark is shown with WPF's own `SplashScreen` - drawn without the dispatcher - while the reset runs, then closed. The order is kept: the reset still runs before the log, the settings and the cycles are opened. Tests: `AppDataResetTests` · **Session:** [S4](020_Work-Plan.md#s4)

**Where:** [`FeBuddy/FeBuddy.Wpf/App.xaml.cs:36-37`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/App.xaml.cs#L36-L37),56-57; [`FeBuddy/FeBuddy.Core/Infrastructure/FileSystem/AppDataReset.cs:72-113`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/FileSystem/AppDataReset.cs#L72-L113)

**Problem:** Deliberately first (before the log, settings and cycles open), but `RunPending` waits up to `ResetWaitForExit` (15 s) for the old process, then recursively deletes everything under `%APPDATA%\FE-Buddy`, including three AIRAC cycle folders of extracted NASR CSVs and d-TPP metafiles. All of it runs synchronously in `OnStartup` before `base.OnStartup` creates the window, so after clicking Reset the user sees nothing at all for several seconds and may start FE-Buddy again. Only after a reset; a normal launch returns at once (`TakeRequest() is null`).

**Proposed:** Keep the order, but give feedback: when a reset request file exists (`File.Exists(AppDataReset.RequestFilePath)`), show WPF's native `SplashScreen` (e.g. `new SplashScreen("Assets/Brand/FEB_96.png").Show(autoClose: true)`) before calling `RunPending`; it draws immediately without the dispatcher.

**Note:** Also found as [U1-16](#u1-16).

<a id="w6-6"></a>

### W6-6 · GUI lag · Low · The Alias Command Guide and News visual trees are rebuilt and re-parsed every time their page opens

**Status:** Fixed (not yet merged): `TextOrCommand` parses each cell or bullet once. `MarkdownView` builds once XAML has set its properties (when it is initialized), then once per change, and keeps each text's parsed blocks with the text, so the Dashboard's News posts are parsed once, not on every visit. The first parse is still on the UI thread (News.md is 11 KB); the News feed being rebuilt on every environment change is [W3-4](#w3-4), in S4. The built guide isn't cached per document: it has 12 commands. Tests: `MarkdownViewTests` · **Session:** [S3](020_Work-Plan.md#s3)

**Where:** [`FeBuddy/FeBuddy.Wpf/Controls/AliasGuideDocumentView.cs:42-44`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/AliasGuideDocumentView.cs#L42-L44), 62-84, 304-307; [`FeBuddy/FeBuddy.Wpf/Controls/MarkdownView.cs:27-44`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/MarkdownView.cs#L27-L44), 84-90

**Problem:** Shell and Info pages are DataTemplates in a ContentControl (ShellWindow.xaml:45-63 and 506; InfoView.xaml:22-23), so each visit creates a new view and the control rebuilds its whole tree on the UI thread. For the guide that is every command as pills (a Grid, a Rectangle and a TextBlock per part), plus a `GuideInline.Parse` or `CommandMarkup.Parse` of every string. `TextOrCommand` parses each table cell and bullet twice: once to test it, then again inside `Prose`. `MarkdownView` re-runs `MarkdownParser.Parse` for every News post on every Dashboard visit, and calls `Rebuild` whenever any of its four properties is set. Today the cost is modest (12 commands, an 11 KB News.md; tens of ms), but it grows with content and repeats on every navigation.

**Proposed:** In `TextOrCommand`, parse once and pass the runs to `Prose`. Coalesce `MarkdownView` rebuilds into one queued at Loaded priority, as `Card.QueueCheck` does. Have News posts carry their parsed `MarkdownBlock`s, parsed once in Core or by the VM off the UI thread. Optionally cache the built guide tree per `Document`.

<a id="w7-7"></a>

### W7-7 · GUI lag · Low · Opening any file or folder picker checks folder existence on the UI thread; an offline network folder stalls it

**Status:** Fixed (not yet merged): `OutputPreferences.BrowseDirectory` asks Core's `FolderProbe.Exists`, which gives each folder 300 ms to answer, off the UI thread; one that doesn't answer counts as not there, so the picker opens on the next choice (the Desktop last), and doesn't stall on the offline folder itself either. The callers are unchanged: an offline folder now costs at most 0.3 s each, not the network's timeout. Tests: `FolderProbeTests` · **Session:** [S4](020_Work-Plan.md#s4)

**Where:** [`FeBuddy/FeBuddy.Wpf/Shell/OutputPreferences.cs:39-42`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Shell/OutputPreferences.cs#L39-L42)

**Problem:** Called synchronously from 8 picker commands before the dialog opens. If the saved output folder or the field's folder is on a mapped drive or UNC share that is offline, `Directory.Exists` blocks for the SMB timeout (often 20+ s) with the window frozen.

**Proposed:** Low priority. Either skip the check for UNC/network paths (`new Uri(path).IsUnc` / `DriveInfo.DriveType == Network`) and let the dialog fall back itself, or resolve the folder with `await Task.Run(() => OutputPreferences.BrowseDirectory(...))` in the callers.

<a id="w2-27"></a>

### W2-27 · GUI lag · Low · Small synchronous disk touches on the UI thread

**Status:** Fixed (not yet merged), as proposed: the AIRAC Service run looks for an earlier run's files (`HasExistingOutput`) and for the output folder with `Task.Run`, and counts as running from before the first, so the Run button can't start a second run meanwhile. Wx Stations reads its copy's age with `Task.Run` (`DescribeStationData`). Core's `AiracCycleDataCache` records whether a cycle has its d-TPP Metafile when it learns the cycle's folder and after the download, and `FindDtppFile` answers from that, so the General tab's "partial" (and System health, [W3-7](#w3-7)) never touch the disk. The duplicate alias card's save when its toggle changes is left with the other settings saves in [S4](020_Work-Plan.md#s4). Tests: `AiracCycleDataCacheTests`, `WxStationsViewModelTests` · **Session:** [S3](020_Work-Plan.md#s3)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:568`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L568), 631, 687; [`AiracGeneralTabViewModel.cs:445-448`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs#L445-L448); [`WxStationsViewModel.cs:79-86`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/WxStationsViewModel.cs#L79-L86); [`DuplicateAliasesCardViewModel.cs:55-56`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DuplicateAliasesCardViewModel.cs#L55-L56), 111-115

**Problem:** Each is one stat or a small JSON write and is harmless on a local disk, but blocks on a slow or disconnected network/OneDrive output folder.

**Proposed:** Low priority: move `HasExistingOutput`/`ExistingFolder` and the Wx age read into the awaited part (`await Task.Run(...)`); cache the d-TPP "partial" flag in Core's cache entry instead of probing the disk per refresh.

<a id="w2-6"></a>

### W2-6 · GUI lag · Low · One `File.Exists` per written file on the UI thread after a stopped run

**Status:** Fixed (not yet merged), as proposed, with [W2-5](100_Bugs.md#w2-5): a stopped run's results name no alias file, so the AIRAC Service screen no longer checks each written file, and the Review tab only counts them ([W4-5](#w4-5)). Tests: `AiracServiceTests` · **Session:** [S3](020_Work-Plan.md#s3)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:609-610`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L609-L610)

**Problem:** Departures/Arrivals write thousands of GeoJSON files; after a stopped run every one is stat-ed on the UI thread just to drop the few deleted alias files. The list is then added one by one to an ObservableCollection whose only binding is `.Count` (ServiceRunReviewTabView.xaml:202).

**Proposed:** Fixed by [W2-5](100_Bugs.md#w2-5) (Core clears the deleted paths); also consider passing a count instead of every path to `CompleteRun`.

<a id="w3-4"></a>

### W3-4 · GUI lag · Low · News posts are cleared and re-rendered (Markdown re-parsed) on every environment change

**Status:** Fixed (not yet merged): the posts are put in again only when they differ from those shown (the posts compared, not the result object, so a re-check that finds the same News changes nothing); the News button's highlight and the notices still follow every check. With [W6-6](#w6-6), a post shown again isn't parsed again either. Tests: `DashboardViewModelTests` · **Session:** [S4](020_Work-Plan.md#s4)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs:208-217`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs#L208-L217), 222-238

**Problem:** `RefreshNews` runs on every `AppEnvironment.Changed` - 6 times during launch (LaunchSequence.cs:159-293) and twice per Re-check / "Check for updates now" - even when `AppEnvironment.News` is the same object. Each refill recreates every post's `MarkdownView`, which parses its Markdown on the UI thread (MarkdownView.cs:88). News.md has ~20 posts.

**Proposed:** Keep the last `NewsCheckResult` shown and return early when `ReferenceEquals(news, _shownNews)` (still raise NewsUnavailable/NewsFetching).

<a id="u1-14"></a>

### U1-14 · GUI lag · Low · Duplicate alias review re-checks all groups against all groups on every keystroke

**Status:** Fixed (not yet merged), as proposed: Core's `DuplicateAliasChoices.Problems` checks every command in one pass, counting each new command once, and the review's list is virtualized. Tests: `DuplicateAliasChoicesTests` (2,000 commands checked at once) · **Session:** [S3](020_Work-Plan.md#s3)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/DuplicateAliasReviewViewModel.cs:88-105`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DuplicateAliasReviewViewModel.cs#L88-L105); [`FeBuddy/FeBuddy.Wpf/Views/DuplicateAliasReviewWindow.xaml:36-80`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/DuplicateAliasReviewWindow.xaml#L36-L80)

**Problem:** the Rename box is bound with `UpdateSourceTrigger=PropertyChanged` and `NewCommand`/`Action` changes call `Revalidate`, which rebuilds a HashSet of every other group's lines for each group (O(D²·L)); the window's group list is a plain ItemsControl (not virtualized). Fine for a handful of duplicates, est. 10-50 ms per keystroke and a slow first open with several hundred.

**Proposed:** compute all problems in one pass in Core, e.g. `DuplicateAliasChoices.Problems(IReadOnlyList<DuplicateAliasCommand> commands, IReadOnlyList<DuplicateAliasRule> choices, ISet<string> taken)` (count new-command uses once, then check each group), and virtualize the window's list.

**Note:** Also found as [W2-26](#w2-26).

<a id="w3-5"></a>

### W3-5 · GUI lag · Low · Output picker filter and listing do per-item work on the UI thread

**Status:** Fixed (not yet merged), as proposed: Core's `OutputFileFilter` splits the filter into its words once, when it is typed, and the picker's view filters with it; the picked and matching counts are worked out once per change, not on every read of the line that shows them. Listing the folder was already off the UI thread; making the choices and the grouped view for a new listing is left on it, as one pass. Tests: `OutputFileFilterTests` · **Session:** [S4](020_Work-Plan.md#s4)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs:274-285`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs#L274-L285), 288-304, 673-684, 692-702

**Problem:** Each filter change (TextBox Delay=150) refreshes a grouped ListCollectionView over every output file (a Departures/Arrivals run writes thousands); the predicate re-splits the filter text for every item; `OutputSummary` then enumerates the whole view again to count matches, and also scans `OutputChoices` for `IsSelected` on every tick. When a run changed the folder, `RefreshOutputsAsync` builds thousands of `OutputFileChoice`s and a new grouped view on the UI thread.

**Proposed:** Split the filter into words once in the `OutputFilter` setter (field) and use it in `MatchesOutputFilter`; count matches/selected while filtering (or keep a selected counter) instead of re-enumerating in the getter. Optionally pre-filter into a list off the UI thread.

<a id="w3-8"></a>

### W3-8 · GUI lag · Low · ROI state properties reload the saved ROI 4-5 times per change

**Status:** Fixed (not yet merged), as proposed: `MapViewModel` keeps the target's ROI as last read - when the draft is reset (after a save or cancel) and when the target says it changed - and the ROI card's state is worked out from that. Tests: `MapViewModelTests` · **Session:** [S4](020_Work-Plan.md#s4)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/MapViewModel.cs:179-192`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/MapViewModel.cs#L179-L192), 454-463; [`FeBuddy/FeBuddy.Wpf/ViewModels/DefaultRoiTarget.cs:36`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DefaultRoiTarget.cs#L36)

**Problem:** `RaiseRoiState` (every keystroke in a corner box, every draft change) raises IsRoiDirty, RoiState (which reads IsRoiDirty and Target.Current again), RoiStateKind (RoiState again) and RoiNotSetHint; on the Map page each read of `Target.Current` is `DefaultRoiStore.Load()` = 5 `UserConfigFile.GetValue` + 4 parses. Cheap in a normal launch, but in developer mode each GetValue miss (no default ROI saved) writes a Debug log entry, so typing a corner floods the activity log (see [W3-3](#w3-3)).

**Proposed:** Cache the saved ROI in MapViewModel (refresh it in `OnTargetChanged`/`ResetDraft`/`Finish`) and compute the state properties from the cached value.

<a id="w1-5"></a>

### W1-5 · GUI lag · Low · Getters and CanExecute recompute the same lookups several times per keystroke

**Status:** Fixed (not yet merged), as proposed: Procedures works out the Add box's entries once per change to what they depend on, not on every re-query and hint read; a new default ROI, set without the tab being edited, is such a change (a new `OnRoiChanged` hook on the GeoJSON tabs). The Add Procedure box's root name is worked out again only when its text or the metafile changes. Core's `VirtualAirlineCallSigns` looks the virtual airline list up by 3LD and telephony, built once per list off the UI thread, for Telephony's hint, note, Confirm and list marks. The note still works out the hint itself: with the lookups, that costs nothing worth keeping a copy for. Tests: `VirtualAirlineCallSignsTests`, `ProceduresViewModelTests` · **Session:** [S3](020_Work-Plan.md#s3)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs:672-693`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs#L672-L693), 1223, 1357, 1482-1484, 749-767; [`TelephonyViewModel.cs:402-471`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs#L402-L471), 669-672

**Problem:** `ReadAirportEntries` re-parses the Add box and re-resolves each ID. In ROI mode it also loads the ROI and runs `RoiFilter.Contains` per entry. It runs from the `NewAirportHint` getter (raised by every `Validate`), from `CanAddAirport` on every CommandManager requery (any key, click or focus change), and from `AddAirport`. `CanAddProcedure` and `NewProcedureHint` each run `RootName` (two regexes) on every requery and keystroke. In Telephony, the `VirtualAirlineEditorNote` getter evaluates `VirtualAirlineEditorHint` again. Both getters and `CanConfirmVirtualAirline` scan the whole merged virtual airline list linearly (`FindSame`, `Where(HasSameCallSign)`) on every keystroke and requery. Each call is cheap but they pile up per keystroke.

**Proposed:** Cache `ReadAirportEntries()` in a field, recomputed only when `NewAirportText`, `Airports`, the area, the ROI or the lookups change. Expose a (3LD, telephony) lookup on `VatsimRadarCopy` in Core (built once per copy) and use it for the hint, the note, `CanConfirm` and `RefreshVatsimRadarMatches`. Have the note take the hint as a parameter rather than re-evaluating it.

<a id="w4-5"></a>

### W4-5 · GUI lag · Low · Review tab adds every written file to an ObservableCollection one by one, but only shows the count

**Status:** Fixed (not yet merged), as proposed, with [U1-7](#u1-7): `FilesWrittenCount`, set once in `BeginRun`, `CompleteRun` and `FailRun`, raises `HasFiles`; the view binds it. Tests: `ServiceRunReviewTabViewModelTests` · **Session:** [S3](020_Work-Plan.md#s3)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceRunReviewTabViewModel.cs:45`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceRunReviewTabViewModel.cs#L45), 82-86, 240-243, 270-273; [`FeBuddy/FeBuddy.Wpf/Views/ServiceRunReviewTabView.xaml:193`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ServiceRunReviewTabView.xaml#L193), 202

**Problem:** Departures and Arrivals write one file per procedure per kind, so a large-area run writes thousands of files. Each `Add` raises CollectionChanged, the `HasFiles` PropertyChanged and the `FilesWritten.Count` binding update, while the Review tab is on screen (ShowPostRunTab selects it before the run). Nothing binds the list itself - only its count and HasFiles. No file reading or parsing happens when describing results (each tab's DescribeRunResult only formats counts from the in-memory result; SubServiceRunResult groups messages in memory), so this is the only avoidable UI-thread cost there.

**Proposed:** Replace `ObservableCollection<string> FilesWritten` with `int FilesWrittenCount` (set once in CompleteRun/FailRun/BeginRun, raising HasFiles), bind `FilesWrittenCount` in ServiceRunReviewTabView.xaml:202. If the list is wanted later, assign a new `IReadOnlyList<string>` once.

<a id="w4-4"></a>

### W4-4 · GUI lag · Low · What You'll Get is built twice and its card rebuilt on every edit, even when nothing in it changed

**Status:** Fixed (not yet merged), as proposed: `RaiseWhatYoullGetChanged` builds the summary once and announces it only when it says something new (`SummaryBlock.Same`), so the card isn't rebuilt for an edit it doesn't mention; while it announces, both getters return that one summary. Read at any other time, it is built fresh, so the Preview Settings tab never gets an old one. Tests: `AreaTests` · **Session:** [S3](020_Work-Plan.md#s3)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceTabViewModel.cs:51-55`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceTabViewModel.cs#L51-L55), 188-198, 208-212; [`FeBuddy/FeBuddy.Wpf/Views/Cards/SummaryCard.xaml:22-24`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/Cards/SummaryCard.xaml#L22-L24); [`FeBuddy/FeBuddy.Wpf/Controls/OutputSummary.cs:23-25`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/OutputSummary.cs#L23-L25), 44-54

**Problem:** Every setter calls MarkDirty -> Revalidate -> RaiseWhatYoullGetChanged, which raises both `WhatYoullGet` and `WhatYoullGetOutputs`; the What You'll Get card binds both, so `BuildWhatYoullGet()` (string building, list joins, `GroupedSummaryFiles` over `OutputFiles()`) runs twice per keystroke (four times for OverrideRoi, which also calls RaiseRoiChanged). Each new list makes `OutputSummary.Refresh` replace `Items`, so the card's whole item tree (chips, InlineCode text) is torn down and rebuilt - even when typing a CRC xOffset or an ROI corner that can't change the summary. Small per keystroke, but pure waste on the hot path. For the record, the rest of the per-edit work is cheap: the dirty check (`CaptureCurrentValues` = the tab's `WriteToConfig` into a dictionary, then `SavedStateSnapshot.Matches`) handles at most a few hundred short strings (ARTCC Boundaries in ARTCC/altitude layout and NAVAIDs/Fixes with their per-type CRC rows are the largest), and `Validate` loops only the tab's own CRC rows and toggles - no disk, no NASR data.

**Proposed:** In `Revalidate`, build the blocks once into a field, compare with the previous blocks structurally (block by block: Outputs, Lines/Includes/Files/Notes with SequenceEqual), and raise `WhatYoullGet`/`WhatYoullGetOutputs` only when they differ; make both getters return the cached value (and the `RaiseWhatYoullGetChanged` callers - default ROI change, cycle change - rebuild the cache first).

<a id="t3-6"></a>

### T3-6 · GUI lag · Low · Switching, creating, renaming or deleting a settings profile does file I/O and reloads every page on the UI thread

**Status:** Fixed (not yet merged), as proposed: each profile change's file work - switching, creating, renaming, deleting - runs with `Task.Run`, step by step until one fails; then, on the UI thread, the pages read the profile and the user is told. New, Rename and Delete can't be clicked meanwhile, and choosing another profile waits too. Reading the pages again stays on the UI thread: it is the pages' own work. Tests: `SettingsProfilesViewModelTests` · **Session:** [S4](020_Work-Plan.md#s4)

**Where:** [`FeBuddy.Wpf/ViewModels/SettingsProfilesViewModel.cs:153-180`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsProfilesViewModel.cs#L153-L180)

**Problem:** SettingsProfilesViewModelTests pump the dispatcher and see the switch complete synchronously: the profile file is read/written and the reload callback makes every page re-read its settings, all in one dispatcher callback. Files are small, so this is a short freeze, not a hang.

**Proposed:** Optional: run the `UserConfigFile` call in `Task.Run` and reload pages after it on the dispatcher.

<a id="c6-31"></a>

### C6-31 · GUI lag · Low · Settings import reads and plans on the UI thread (Core work, Wpf call site)

**Status:** Fixed (not yet merged), as proposed: Core's `UserConfigTransfer.ReadAndPlan` reads the file and works out both imports; the Settings page runs it with `Task.Run`, and applies the import the user picks with `Task.Run` too. The windows and the pages reading the import stay on the UI thread, and Import can't be clicked again meanwhile. Tests: `UserConfigTransferTests` · **Session:** [S4](020_Work-Plan.md#s4)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:838-841`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L838-L841); [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigTransfer.cs:142`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigTransfer.cs#L142), 289-290, 515-521

**Problem:** This runs synchronously on the UI thread once per Import. It parses up to 2 MB of JSON (`MaxFileBytes`), runs the migrations, and makes two `Plan` passes. Each pass calls `Directory.Exists`/`File.Exists` for every folder and file key. `IsLocalDrivePath` accepts any drive letter, so a disconnected mapped drive (`Z:\...`) in the file can block the window for seconds. It's a one-off per import, hence Low. The call site is probably also in the Wpf reviewer's scope.

**Proposed:** `var (package, replace, merge) = await Task.Run(() => { var p = UserConfigTransfer.Read(path); return (p, UserConfigTransfer.Plan(p, UserConfigImportMode.Replace), UserConfigTransfer.Plan(p, UserConfigImportMode.Merge)); });`. Optionally add a Core `UserConfigTransfer.ReadAndPlanAsync(path)` returning both plans.

<a id="u1-13"></a>

### U1-13 · GUI lag · Low · Settings facility list runs its 20k-row LINQ on the UI thread when the cycle is already cached

**Status:** Fixed (not yet merged), as proposed: the facility list is Core's (`AiracCycleOptionLists.FacilityNames`, with [U1-17](120_Logic-In-GUI.md#u1-17)), worked out once per cycle and read with `Task.Run`. Tests: `AiracCycleOptionListBuilderTests` · **Session:** [S4](020_Work-Plan.md#s4)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:1124-1137`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L1124-L1137)

**Problem:** `GetAsync` returns an already-completed task once the cycle is parsed (`AiracCycleDataCache.cs:402-404`), and `ConfigureAwait(false)` on a completed task continues synchronously on the calling (UI) thread, so the ~20k-row APT_BASE scan runs while the Settings page opens. Est. 5-15 ms. (Same pattern would bite any future code after `GetAsync(...).ConfigureAwait(false)`.)

**Proposed:** `var options = await Task.Run(() => FacilityOptions.From(data));` with the derivation in Core (see [U1-17](120_Logic-In-GUI.md#u1-17)), or `await Task.Run(async () => ...)` around the whole fetch-and-build.

**Note:** Also found as [W3-6](#w3-6), [W3-17](120_Logic-In-GUI.md#w3-17).

<a id="w3-7"></a>

### W3-7 · GUI lag · Low · System health refresh hits the disk on every cycle state change

**Status:** Fixed (not yet merged), with [W2-27](#w2-27) in S3: `FindDtppFile` answers from what the cache found, so the refresh reads no file. Tests: `AiracCycleDataCacheTests` · **Session:** [S4](020_Work-Plan.md#s4)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs:722-724`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs#L722-L724)

**Problem:** `RefreshSystemHealth` runs on the UI thread for every `AiracCycleDataCache.StateChanged` (many during launch) and every environment change; `FindDtppFile` does a `File.Exists` per cycle (Core AiracCycleDataCache.cs:468-476) in %APPDATA% (which can be a redirected/roaming folder).

**Proposed:** Have the cache record whether a cycle has its metafile on the entry (it knows when DtppDownloader finishes) and read that flag instead of probing the disk.

<a id="u1-15"></a>

### U1-15 · GUI lag · Low · Switching AIRAC Service tabs rebuilds the whole tab view

**Status:** Fixed (not yet merged): not measured (it needs the running app). The tab page is now a `KeptContentHost`: each tab's page is built the first time it is shown and kept, so switching back only shows it again; a page whose tab leaves the rail is dropped at the next switch. Nothing is built up front. Each page's cards take their output tags from their own tab. The File Names refresh on a switch stays: it lists the files in memory and rebuilds its rows only when they changed. Tests: `KeptContentHostTests` · **Session:** [S3](020_Work-Plan.md#s3)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/TabbedServiceView.xaml:174-176`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/TabbedServiceView.xaml#L174-L176); [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:113-119`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L113-L119)

**Problem:** the selected tab is shown through a ContentControl + DataTemplate, so every switch discards and re-creates the full card tree of the tab (Procedures, Fixes and Airways views are large), and the switch also runs `_fileNames.RefreshFiles()` (lists every output file of every included tab). Est. 30-150 ms per switch for the large tabs (estimate; measure first).

**Proposed:** measure; if a switch exceeds ~50 ms, keep realized tab views alive (an ItemsControl over `Tabs` with a Grid panel and `Visibility` bound to "is selected", or a TabControl-style content cache) so a switch only flips visibility.

## Reported more than once

Each review slice was read by its own reviewer, so some findings were reported twice. These IDs point at the finding that is kept.

- <a id="c3-2"></a>**C3-2** · The kept virtual airline list is read and merged on the UI thread · same as [U1-2](#u1-2).
- <a id="c3-27"></a>**C3-27** · Add Custom Aliases validation reads custom alias files on the UI thread · same as [W1-3](#w1-3).
- <a id="t3-1"></a>**T3-1** · Every tab's cycle lists are built from the whole NASR data on the UI thread · same as [U1-1](#u1-1).
- <a id="t3-2"></a>**T3-2** · Procedure Charts indexes the whole d-TPP Metafile on the UI thread · same as [W1-1](#w1-1).
- <a id="t3-3"></a>**T3-3** · Typing in a conversion's source-folder box re-scans the folder (and opens every .xml for ERAM) per keystroke · same as [U1-6](#u1-6).
- <a id="t3-4"></a>**T3-4** · Add Custom Aliases reads whole local alias files during validation and construction · same as [W1-3](#w1-3).
- <a id="t3-5"></a>**T3-5** · The base map's 1.5 MB of GeoJSON is parsed on the UI thread when the first map opens · same as [W7-1](#w7-1).
- <a id="u1-4"></a>**U1-4** · "Undo last save" CanExecute reads and parses a JSON file on every key/mouse/focus event · same as [C6-30](#c6-30).
- <a id="u1-5"></a>**U1-5** · Add Custom Aliases checks files on disk on every keystroke in the Location box · same as [W1-3](#w1-3).
- <a id="u1-8"></a>**U1-8** · Procedure Charts builds its d-TPP name index on the UI thread · same as [W1-1](#w1-1).
- <a id="u1-10"></a>**U1-10** · Dashboard log: one Normal-priority dispatcher item per log entry, counts recomputed by full scans · same as [W3-3](#w3-3).
- <a id="u1-11"></a>**U1-11** · Map and base-map checkboxes save the whole settings file synchronously on each click · same as [W3-1](#w3-1).
- <a id="u1-12"></a>**U1-12** · Base map GeoJSON is loaded and parsed on the UI thread on first Map open · same as [W7-1](#w7-1).
- <a id="u1-16"></a>**U1-16** · A pending app-data reset runs before any window, with up to 15 s wait and no feedback · same as [W7-2](#w7-2).
- <a id="w1-2"></a>**W1-2** · Telephony parses the virtual airline list's JSON on the UI thread · same as [U1-2](#u1-2).
- <a id="w1-4"></a>**W1-4** · LoadCycleDependentLists scans whole NASR tables on the UI thread, all tabs back to back · same as [U1-1](#u1-1).
- <a id="w2-2"></a>**W2-2** · Every tab's cycle lists are built on the UI thread in one dispatcher call · same as [U1-1](#u1-1).
- <a id="w2-3"></a>**W2-3** · Every click in the General tab's table re-parses the d-TPP Metafile and rebuilds Procedures' indexes on the UI thread · same as [W1-1](#w1-1).
- <a id="w2-4"></a>**W2-4** · File Conversions scans the saved source folders on the UI thread after every launch; ERAM opens every .xml in it · same as [U1-6](#u1-6).
- <a id="w2-26"></a>**W2-26** · Duplicate review re-checks every command against every other on each keystroke · same as [U1-14](#u1-14).
- <a id="w3-2"></a>**W3-2** · First map open parses ~1.5 MB of base-map GeoJSON on the UI thread · same as [W7-1](#w7-1).
- <a id="w3-6"></a>**W3-6** · Facility list is built from APT_BASE on the UI thread, sometimes more than once · same as [U1-13](#u1-13).
- <a id="w4-1"></a>**W4-1** · The Undo button's CanExecute reads and parses a JSON file from disk on every command re-query · same as [C6-30](#c6-30).
- <a id="w4-2"></a>**W4-2** · Conversion pages count the source folder on the UI thread on every keystroke (ERAM opens every .xml) · same as [U1-6](#u1-6).
- <a id="w4-3"></a>**W4-3** · ISubServiceRunTarget.LoadCycleDependentLists makes every tab walk the cycle's NASR rows on the UI thread · same as [U1-1](#u1-1).
- <a id="w5-4"></a>**W5-4** · Long run-message and duplicate-review lists are not virtualized, and each line runs a regex converter · same as [U1-7](#u1-7).
- <a id="w6-1"></a>**W6-1** · The base map's JSON is read, parsed and projected on the UI thread the first time any map opens · same as [W7-1](#w7-1).
- <a id="w6-5"></a>**W6-5** · Every pan or zoom frame rebuilds every layer's geometry from scratch · same as [U1-9](#u1-9).
- <a id="w6-7"></a>**W6-7** · Each review message runs the file-name regex and builds its inlines on the UI thread, in lists that aren't virtualized · same as [U1-7](#u1-7).
