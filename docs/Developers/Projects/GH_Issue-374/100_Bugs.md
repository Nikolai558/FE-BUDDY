# 100 · Bugs

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

Things that behave wrongly. The Fix-first ones are session [S1](020_Work-Plan.md#s1); the rest are [S2](020_Work-Plan.md#s2). Each fix should come with a regression test.

**47 findings** (2 High, 11 Med, 34 Low), 28 done. Quote the ID when you comment on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374).

## Summary

| ID | Kind | Sev | Finding | Status | Session |
|---|---|---|---|---|---|
| [W2-1](#w2-1) | Bug | High | General tab's Save and "Undo last save" act on the whole `Services.AiracService` subtree | Fixed | S1 |
| [N1-4](#n1-4) | Bug | High | A save can wipe the settings file when it can't read it for a moment | Fixed | S1 |
| [O1-1](#o1-1) | Bug | Med | Docs-only PRs skip Test and Installer, but the tests and the MSI read docs/*.md and LICENSE.md | Fixed | S1 |
| [C1-1](#c1-1) | Bug | Med | A current cycle the probe calls "not yet published" leaves the AIRAC Service "Waiting" forever | Fixed | S1 |
| [C3-18](#c3-18) | Bug | Med | An empty `Combined_Alias.txt` can be written | Fixed | S1 |
| [C5-1](#c5-1) | Bug | Med | DAT and SCT conversions silently overwrite a file when two sources share a name | Fixed | S1 |
| [U1-3](#u1-3) | Bug | Med | Pages can be built from empty settings before the launch sequence has read them | Fixed | S1 |
| [W7-3](#w7-3) | Bug | Med | A settings file that can't be written crashes FE-Buddy when the default ROI is saved or cleared (and the write runs on the UI thread) | Fixed | S1 |
| [W2-11](#w2-11) | Bug | Med | An exception before the run's `try` crashes the app | Fixed | S1 |
| [W1-14](#w1-14) | Bug | Med | Switching the cycle silently undoes unsaved ticks in the cycle-built lists | Fixed | S1 |
| [W1-13](#w1-13) | Bug | Med | Unticking every Procedures.json field gives the 8 default fields | Fixed | S1 |
| [W3-15](#w3-15) | Bug | Med | Reset window says settings backups are always deleted, but a reset that keeps settings keeps them | Fixed | S1 |
| [C1-5](#c1-5) | Bug | Med | The update's relaunch inherits msiexec's elevation | Fixed | S1 |
| [C3-17](#c3-17) | Bug | Low | A custom alias file's address with a secret is echoed when it isn't http(s) | Deferred | After 3.0 |
| [O1-42](#o1-42) | Bug | Low | DEV-CleanBuild.bat misses FeBuddy.Installer.UI and every obj folder | Fixed | S2 |
| [C1-2](#c1-2) | Bug | Low | A cancelled run can leave a cycle's parse permanently "cancelled" for the session | Fixed | S2 |
| [C1-3](#c1-3) | Bug | Low | Procedure Charts' optional previous-cycle NASR load can fail the whole run; GetAsync's exception doc is incomplete | Fixed | S2 |
| [C2-3](#c2-3) | Bug | Low | Waypoint buffering can remove an airway from Airways.txt, and runs on alias-only runs | Fixed | S2 |
| [C3-16](#c3-16) | Bug | Low | NAVAIDs' "nothing written" advisory: one branch unreachable, wrong text when NASR has no NAVAIDs | Fixed | S2 |
| [C4-11](#c4-11) | Bug | Low | IncludesProceduresWithFixInFacility returns true when the rule can't run, so the run loads (or waits for) the previous cycle's NASR for nothing | Fixed | S2 |
| [C5-8](#c5-8) | Bug | Low | Card-mode warning says elements "carry only their filters", but they carry filter 0 | Fixed | S2 |
| [C1-4](#c1-4) | Bug | Low | AppEnvironment.RecheckAsync moves the AIRAC date, so the GUI can name a cycle the cache doesn't track | Deferred | After 3.0 |
| [C5-23](#c5-23) | Bug | Low | ROI validation accepts "NaN" | Fixed | S2 |
| [C6-29](#c6-29) | Bug | Low | Shared.json's layout stamp is written but never read, so a future step can't reach shared settings | Fixed | S2 |
| [C6-23](#c6-23) | Bug | Low | CredentialStore leaves the Basic sign-in's UTF-8 bytes (user:password) uncleared, contrary to its own rule | Fixed | S2 |
| [C5-22](#c5-22) | Bug | Low | DAT and SCT readers never throw InvalidDataException, so a wrong file is an advisory, not a failed file | Deferred | After 3.0 |
| [C7-10](#c7-10) | Bug | Low | A 404 for the previous cycle is logged as "not published by the FAA yet" | Fixed | S2 |
| [O1-30](#o1-30) | Bug | Low | A failure building the setup window cancels the install instead of falling back | Open | S2 |
| [O1-23](#o1-23) | Bug | Low | Under Windows PowerShell 5.1 (build.cmd) the counter is rewritten with a BOM | Open | S2 |
| [T1c-26](#t1c-26) | Bug | Low | Two tests read the clock twice and can fail at 0000Z | Fixed | S2 |
| [W7-4](#w7-4) | Bug | Low | The "last-chance" crash handler only sees exceptions on the UI thread | Fixed | S1 |
| [W6-3](#w6-3) | Bug | Low | The "Towered Airports" layer would include any airport whose tower code has no display mapping, e.g. `TRACON` | Open | S2 |
| [W7-22](#w7-22) | Bug | Low | Card.Popup casts its shadow upwards, onto the map toolbar for the two map menus | Open | S2 |
| [W2-8](#w2-8) | Bug | Low | A run can start before the selected cycle's lists exist, and silently drops CRC defaults | Fixed | S2 |
| [W2-9](#w2-9) | Bug | Low | Picking a cycle the FAA hasn't published leaves every tab on "Loading AIRAC &lt;id>…" for good | Fixed | S2 |
| [W2-10](#w2-10) | Bug | Low | A cycle switch rebuilds the ARTCC / NAVAID-type toggles from the saved filter, losing unsaved ticks | Fixed | S1 |
| [W2-37](#w2-37) | Bug | Low | Review summaries name the default alias file and always mention the ROI | Open | S2 |
| [W2-5](#w2-5) | Bug | Low | Review tab reports alias files that a run stopped at the duplicate review deleted | Fixed | S2 |
| [W3-16](#w3-16) | Bug | Low | Dashboard can miss log entries written while it is being built | Open | S2 |
| [W4-8](#w4-8) | Bug | Low | CRC defaults rows accept any non-blank bcg/style/thickness/size, so an imported or hand-edited bad value passes the tab and fails the run | Open | S2 |
| [W1-15](#w1-15) | Bug | Low | Procedure Charts keeps the previous cycle's d-TPP lookups after a cycle switch | Fixed | S2 |
| [W4-7](#w4-7) | Bug | Low | A folder that exists but can't be listed is reported as "does not exist" | Fixed | S2 |
| [W1-7](#w1-7) | Bug | Low | Telephony's "real operator" note counts expired U.S. special call signs, which the run drops | Open | S2 |
| [W5-3](#w5-3) | Bug | Low | Error tooltips never show on CRC ERAM Defaults drop-downs or on the fan-marker style box | Open | S2 |
| [O1-2](#o1-2) | Bug | Low | Any CPU never builds FeBuddy.UnitTests | Fixed | S2 |
| [T2-8](#t2-8) | Bug | Low | NasrCsvParserMappingTests computes expected numbers with the PC's culture | Fixed | S2 |
| [T2-9](#t2-9) | Bug | Low | FileEncodingTests walks build.ps1's `publish\` and `releases\` output | Fixed | S2 |

## High

<a id="w2-1"></a>

### W2-1 · Bug · High · General tab's Save and "Undo last save" act on the whole `Services.AiracService` subtree

**Status:** Fixed (not yet merged). The General tab saves under `Services.AiracService.General`, beside the other tabs' nodes, with no settings-layout step: a file with the old keys opens the General tab on its defaults ([Q1](010_Decisions.md#q1)). Tests: `AiracGeneralTabViewModelTests.the_general_tabs_save_and_undo_never_reach_another_tabs_settings`, and `UserConfigLayoutTests.no_settings_node_sits_inside_another` for [H5](010_Decisions.md#h5); both fail on the old node. The layout guard lists the three old keys as left behind on purpose. Settings-Reference.md updated · **Session:** [S1](020_Work-Plan.md#s1)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs:22-23`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs#L22-L23), 36, 76

**Problem:** That node is the parent of every other AIRAC tab's node (`.Airways`, `.Arrivals`, `.FileNames`, ...), of `.DuplicateAliases` (DuplicateAliasesCardViewModel), `.DefaultRoi` (DefaultRoiStore, saved by the Map page), and of Settings' `UserArtccId` and `CoordinatePrecision` (none of them are shared-by-profile keys). `UserConfigFile.CanUndo(nodePath)` is true as soon as **any** child node has a snapshot (`NodeExistsAtPath`, UserConfigFile.cs:929-945). A first save of any child writes `{"Services":{"AiracService":{}}}` into `UserConfig-previous` (SetNodeAtPath with null still creates the parents, UserConfigFile.cs:951-979). So after the user saves e.g. Airways once, or sets a default ROI on the Map, the General tab's **Undo last save** button is enabled, and clicking it runs `UserConfigFile.Undo("Services.AiracService")`, which replaces the whole subtree on disk with that snapshot: every AIRAC tab's settings, the File Names choices, the saved duplicate-alias choices, the Facility, the coordinate precision and the default ROI are wiped (or reverted). Only the General tab is reloaded (`ReloadFromConfig`), so the other tabs keep showing their old values as "saved" until the next launch. A General **Save** also overwrites every child tab's undo snapshot with the current state, so their own Undo then restores nothing.

**Proposed:** Give the General tab a node of its own, e.g. `Services.AiracService.General` (`AiracCycleId`, `SelectedSubServices`, `Outputs.*`), with a `UserConfigMigrations` step that moves the three keys, and update `IsSavedOutputOn`, ProceduresViewModel:476, Settings-Reference.md and the tests' fixtures. Then reword the remark to "It owns `Services.AiracService.General` ...". (Alternative: let `UserConfigFile.Save/Undo/CanUndo` take the child nodes to leave alone, but a separate node is simpler.)

<a id="n1-4"></a>

### N1-4 · Bug · High · A save can wipe the settings file when it can't read it for a moment

**Status:** Fixed (not yet merged); found while fixing [W7-3](#w7-3). Data loss, but it needs the file to be locked at the moment of a save, so it is rare. Tests: `UserConfigFileTests.save_and_undo_never_write_over_a_profile_they_cannot_read`, `write_refuses_after_the_profile_could_not_be_read_at_launch` and `a_locked_re_read_keeps_the_settings_read_before_but_never_another_profiles` · **Session:** [S1](020_Work-Plan.md#s1)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:793-808`](https://github.com/Nikolai558/FE-BUDDY/blob/85991050e44eb6d26e828ba22c12de6e6f9437ff/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L793-L808) (`LoadObjectOrEmpty`), used by `Save` ([:404](https://github.com/Nikolai558/FE-BUDDY/blob/85991050e44eb6d26e828ba22c12de6e6f9437ff/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L404)), `Undo` ([:531](https://github.com/Nikolai558/FE-BUDDY/blob/85991050e44eb6d26e828ba22c12de6e6f9437ff/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L531)) and `UpdateSharedFile` ([:691](https://github.com/Nikolai558/FE-BUDDY/blob/85991050e44eb6d26e828ba22c12de6e6f9437ff/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L691)); `ReadAll` ([:206-212](https://github.com/Nikolai558/FE-BUDDY/blob/85991050e44eb6d26e828ba22c12de6e6f9437ff/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L206-L212)) and `Write` ([:220](https://github.com/Nikolai558/FE-BUDDY/blob/85991050e44eb6d26e828ba22c12de6e6f9437ff/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L220))

**Problem:** `Save` and `Undo` read the profile's file with `LoadObjectOrEmpty`, which gives an empty object when the file is there but can't be read: locked for a moment by antivirus or a sync tool, or no permission. The save then writes that empty object, plus the one node being saved, back over the file: every other setting in the profile is gone. `UpdateSharedFile` did the same to `Shared.json` (every shared setting, and the active profile's name). And a `ReadAll` that met such a lock dropped every setting in memory to its default; a Settings ▸ **Save** (`Write`) after it wrote those defaults over the file.

**Fix:** `Save`, `Undo` and `UpdateSharedFile` read with `LoadObjectToUpdate`, which throws on a file that is there but can't be read (a file that isn't valid JSON is still replaced, as before). `ReadAll` keeps the settings it last read when the same profile is locked on a re-read; at launch or after a profile switch there is nothing to keep, so it uses defaults and `Write` refuses until a read succeeds.

## Med

<a id="o1-1"></a>

### O1-1 · Bug · Med · Docs-only PRs skip Test and Installer, but the tests and the MSI read docs/*.md and LICENSE.md

**Status:** Fixed (not yet merged), as agreed: `ci.yml` has no `Changes` job any more, so Build, Test and Installer run on every pull request; its header comment and the developer README's Continuous integration section say so. No unit test: it is workflow configuration, and the next docs-only pull request shows it. If the rulesets list a `Changes` check as required, take it off · **Session:** [S1](020_Work-Plan.md#s1)

**Where:** [`.github/workflows/ci.yml:12-17`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/.github/workflows/ci.yml#L12-L17), [`.github/workflows/ci.yml:66-73`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/.github/workflows/ci.yml#L66-L73)

**Problem:** News.md is not the only Markdown outside FeBuddy/ that something reads. `FeBuddy.UnitTests/Repository/DocLinkTests.cs` opens every `docs/...md` page the app links to (`Links`) and checks its `#section` headings still exist, and `FileEncodingTests` checks every file in the repository for a BOM. `LICENSE.md` is embedded in the installer window (`FeBuddy.Installer.UI.csproj:71`). A PR that only renames a heading in `docs/Users/User-Guide.md` the app links to, or saves a doc with a BOM, skips Test and merges; the break only shows on the push run after the merge (or the next code PR). The comment's reason is therefore incomplete.

**Proposed:** in the `while` loop, also set `code=true` for `docs/*` and `LICENSE.md` (simplest: skip only for `ChangeLog.md`, `README.md` and `.github/*.md`, i.e. invert the rule to an allow-list of skippable files). Reword the comment: "A pull request into v3-development that changes only ChangeLog.md, README.md or .github/ Markdown skips Build, Test and Installer. Everything else runs them: the tests read docs/ (DocLinkTests) and every file's encoding (FileEncodingTests), News.md is built into the app and LICENSE.md into the installer."

<a id="c1-1"></a>

### C1-1 · Bug · Med · A current cycle the probe calls "not yet published" leaves the AIRAC Service "Waiting" forever

**Status:** Fixed (not yet merged), as proposed: `ComputeReadiness` treats a current cycle that is `NotYetPublished` as `Unavailable`. The AIRAC Service's message now says to check the internet connection (a proxy or firewall) and the PC's clock; the status line and the Systems row say the cycle "couldn't be loaded"/"couldn't be downloaded or read". `Unavailable`'s doc and Architecture.md's readiness table updated. Test: `AiracCycleDataCacheTests.a_current_cycle_that_is_not_published_is_unavailable_not_waiting` · **Session:** [S1](020_Work-Plan.md#s1)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs:540-550`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs#L540-L550); [`FeBuddy/FeBuddy.Core/Infrastructure/Nasr/AiracCycleAvailability.cs:102-105`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/AiracCycleAvailability.cs#L102-L105); [`FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs:695-700`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs#L695-L700)

**Problem:** The probe maps HTTP 404 and 403 to `NotYetPublished`. If that happens to the *current* cycle (a proxy/firewall answering 403, an FAA URL change, or the time check falling back on a PC clock that is set ahead - UtcTimeCheck.cs:58-59), the cycle is settled (`IsSettled` = true, nothing will load it) but `ComputeReadiness` returns `Waiting`, so the shell shows "Waiting for AIRAC data…" for the whole session with no error, and the AIRAC Service stays locked. No test covers a current cycle that is not published. Architecture.md's table also defines `Waiting` only as "still downloading or parsing".

**Proposed:** In `ComputeReadiness`, treat a current cycle that is `NotYetPublished` like a failed one: `if (current.State is CycleDataState.Failed or CycleDataState.NotYetPublished) return AiracCycleReadiness.Unavailable;`. Optionally have the shell's Unavailable text say "the FAA has no data for the current cycle (check the PC clock / network)". Add a test. Update the `Unavailable` doc (CycleDataState.cs:49) and the Architecture.md readiness table to "the current cycle failed or isn't available".

<a id="c3-18"></a>

### C3-18 · Bug · Med · An empty `Combined_Alias.txt` can be written

**Status:** Fixed (not yet merged), as proposed: when the run wrote no alias file and no custom file has a line of its own (an old upload that was only FE-Buddy's section, an empty file, a lone `.FeUseOnly`), nothing is written, an earlier file is deleted, and an advisory says why. Test: `CombinedAliasFileWriterTests.custom_files_with_no_aliases_of_their_own_and_no_run_aliases_write_no_file` (on the old code the file was written) · **Session:** [S1](020_Work-Plan.md#s1)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/CombinedAliasFileWriter.cs:156`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/CombinedAliasFileWriter.cs#L156),167-175,203-216

**Problem:** `custom` counts every readable custom file, even one left with no lines after its FE-Buddy section is stripped (last cycle's combined file that was only FE-Buddy's section). If the run wrote no alias file (e.g. Telephony had no data), the writer writes a 0-byte (or `.FeUseOnly`-only) `Combined_Alias.txt` and logs success, contradicting the remarks ("When there is nothing to merge, no file is written"). Uploading it would empty the facility's aliases.

**Proposed:** Treat it as nothing to merge: `if (feBuddy.Count == 0 && custom.All(f => f.Lines.Count == 0))`.

<a id="c5-1"></a>

### C5-1 · Bug · Med · DAT and SCT conversions silently overwrite a file when two sources share a name

**Status:** Fixed (not yet merged), as agreed: each run keeps the output names it has used, and a clashing source is written as `ZOB (2)` (SCT: its folder) with `ConversionFiles.UniqueFileName`, with an Info message saying so. A file that writes nothing doesn't take a name. Tests: `DatToGeojsonServiceTests.two_maps_with_one_name_are_both_kept`, `SctToGeojsonServiceTests.two_sector_files_with_one_name_get_a_folder_each` · **Session:** [S1](020_Work-Plan.md#s1)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Conversions/DatToGeojson/DatGeojsonWriter.cs:66`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/DatToGeojson/DatGeojsonWriter.cs#L66); [`FeBuddy/FeBuddy.Core/Application/Conversions/SctToGeojson/SctGeojsonWriter.cs:69`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/SctToGeojson/SctGeojsonWriter.cs#L69); [`FeBuddy/FeBuddy.Core/Application/Conversions/SctToGeojson/SctToGeojsonService.cs:26`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/SctToGeojson/SctToGeojsonService.cs#L26)

**Problem:** Output is named only after the source file's name without its extension. `ZOB.sct` and `ZOB.sct2` in one folder (both extensions are converted, SctToGeojsonService.cs:26) both write into `SCT2 to GeoJSON\ZOB\`, and the second run of files overwrites the first, leaving a mix of both. The same happens for DAT/SCT files with the same name picked from two folders: the Files picker keeps adding across browses (FileConversionTabViewModel.cs:502-531), and GeojsonFileWriter.cs:97 uses `File.WriteAllText`. The run reports both files as converted and lists the same path twice. ERAM avoids this only by allowing one file per run (EramToGeojsonService.cs:22-24 says why).

**Proposed:** Keep one `HashSet<string>(StringComparer.OrdinalIgnoreCase)` per run in each service and name each source's output with `ConversionFiles.UniqueFileName(Path.GetFileNameWithoutExtension(source), used)`. The helper already exists and gives `ZOB (2)`. Pass the chosen name into `DatGeojsonWriter.Write` / `SctGeojsonWriter.Write`. Add an Info message when a name was changed.

<a id="u1-3"></a>

### U1-3 · Bug · Med · Pages can be built from empty settings before the launch sequence has read them

**Status:** Fixed (not yet merged), as agreed. The read step (read the settings, the welcome check, the output preferences, the first launch's update channel) is `LaunchSequence.ReadSettings`, which `App.OnStartup` runs on the UI thread before the window is built; it then passes `LaunchOptions.SettingsRead`, so `RunAsync` doesn't read them again. Architecture.md's Launch section updated. Test: `LaunchSequenceTests.settings_read_before_the_window_are_there_at_once_and_not_read_again` · **Session:** [S1](020_Work-Plan.md#s1)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Launch/LaunchSequence.cs:92-120`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Launch/LaunchSequence.cs#L92-L120); [`FeBuddy/FeBuddy.Wpf/App.xaml.cs:81`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/App.xaml.cs#L81); [`FeBuddy/FeBuddy.Wpf/Views/ShellWindow.xaml.cs:42`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ShellWindow.xaml.cs#L42),51-60; [`FeBuddy/FeBuddy.Wpf/ViewModels/FileConversionsViewModel.cs:50-52`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/FileConversionsViewModel.cs#L50-L52)

**Problem:** settings are read on the thread pool, after the temp workspace (old NASR/d-TPP downloads) has been deleted, while the UI thread is already building pages that read settings in their constructors via `UserConfigFile.GetValue`, which returns `null` (= default) until `ReadAll` has filled the dictionary (`UserConfigFile.cs:236-251`). `PrepareFileConversions` builds `FileConversionsViewModel` (all three conversion tabs, each `LoadFromConfig()`) at `ApplicationIdle` right after the window renders, with no wait for the read; AIRAC Service/Map/Settings opened in the first second do the same. Nothing reloads the pages after `ReadAll` (`ConfigPages.ReloadAll` is only called by import/profile switch). A page built early shows defaults all session, its saved-state snapshot is taken from the defaults (so it looks clean), and a later Save of that page writes the defaults over the user's saved subtree. `UserConfigFile.ReadSharedValue`'s own doc (line 433-436) acknowledges the window exists, and `MainWindowPlacement` reads from disk to dodge it.

**Proposed:** read settings before any page can be built: move `UserConfigFile.ReadAll()` (plus `WelcomeNotice.Check`, `OutputFormatting.LoadFromUserConfig`, `UpdateChannelSetting.SaveDefaultIfUnset`) out of the background sequence into `App.OnStartup` before `base.OnStartup(e)` (a few small JSON files, est. 5-20 ms), or at least run that step before `ClearTempWorkspace` and expose a `LaunchSequence.SettingsRead` task that `PrepareFileConversions` and `NavItem`'s factory await. If it must stay async, call `ConfigPages.ReloadAll()` on the UI thread when the step finishes.

<a id="w7-3"></a>

### W7-3 · Bug · Med · A settings file that can't be written crashes FE-Buddy when the default ROI is saved or cleared (and the write runs on the UI thread)

**Status:** Fixed (not yet merged). Every GUI save and undo goes through `SettingsWrites` (`FeBuddy.Wpf/Shell`): a settings file that can't be written or read is logged and shown as an error, never thrown, and the settings are read again so nothing takes the change for saved; the page keeps the user's change and stays unsaved, so saving again tries again. `DefaultRoiStore.Set`/`Clear` return whether they saved and raise `Changed` only then; the Settings page stays unsaved if its write or the ROI fails. The writes still run on the UI thread (that lag is [W3-1](110_GUI-Lag.md#w3-1) and [C6-30](110_GUI-Lag.md#c6-30)). Tests: `SettingsWritesTests` (on the old code the save threw). Fixing it turned up [N1-4](#n1-4) · **Session:** [S1](020_Work-Plan.md#s1)

**Where:** [`FeBuddy/FeBuddy.Wpf/Shell/DefaultRoiStore.cs:56-79`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Shell/DefaultRoiStore.cs#L56-L79); [`FeBuddy/FeBuddy.Wpf/ViewModels/DefaultRoiTarget.cs:42-54`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DefaultRoiTarget.cs#L42-L54); [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:717-729`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L717-L729)

**Problem:** `UserConfigFile.Save` reads two files, writes two or three (`File.WriteAllText`, UserConfigFile.cs:810-814) and re-reads everything (`ReadAll`), synchronously on the UI thread. An `IOException`/`UnauthorizedAccessException` (file briefly locked by antivirus or a sync tool, read-only folder) is not caught anywhere on the Map page's path, reaches `DispatcherUnhandledException`, which does not set `Handled`, so the app exits. The same unguarded `UserConfigFile.Save` pattern is in MapLayersState.cs:186,434,536,764, BaseMapSettings.cs:94,129,137, DashboardViewModel.cs:269, DuplicateAliasesCardViewModel.cs:56,114 (other reviewers' scope; worth one shared fix).

**Proposed:** In `DefaultRoiStore.Set`/`Clear`, catch `IOException or UnauthorizedAccessException`, log it, `Toast.Error("Could not save the default ROI", ex.Message)` and return `false` (or let the caller toast), and only raise `Changed` on success. Same treatment for the other GUI callers of `UserConfigFile.Save`.

**Note:** See also [W2-11](#w2-11) and [W7-4](#w7-4).

<a id="w2-11"></a>

### W2-11 · Bug · Med · An exception before the run's `try` crashes the app

**Status:** Fixed (not yet merged). Both `RunAsync`s (AIRAC Service, File Conversions) have one `try` from their first line: a failure before the run starts is logged and shown as "didn't start" with the reason; once it has started, as "failed", as before. ERAM's `ConfirmRun` is inside it too. No unit test of its own: the run methods open windows. The main way in, a save that throws, is covered by [W7-3](#w7-3)'s tests · **Session:** [S1](020_Work-Plan.md#s1)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:67`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L67), 520-587; [`FeBuddy/FeBuddy.Wpf/ViewModels/FileConversionsViewModel.cs:58`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/FileConversionsViewModel.cs#L58), 238-264; [`EramToGeojsonViewModel.cs:159-162`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/EramToGeojsonViewModel.cs#L159-L162)

**Problem:** `TrySaveDirtyTabs` -> `UserConfigFile.Save` (File.WriteAllText, no catch), `HasExistingOutput` / ERAM `ConfirmRun` (`Directory.EnumerateFileSystemEntries` on a folder the user can't list), `BuildSettingsBlock` all run outside the try. Thrown from an async-void handler they reach `App.OnUnhandled`, which logs but doesn't set `Handled`, so FE-Buddy closes.

**Proposed:** Wrap the pre-run part of both `RunAsync` methods in try/catch that shows `Toast.Error` and returns (and `try/catch (IOException or UnauthorizedAccessException)` in ERAM `ConfirmRun`).

**Note:** See also [W7-3](#w7-3) and [W7-4](#w7-4).

<a id="w1-14"></a>

### W1-14 · Bug · Med · Switching the cycle silently undoes unsaved ticks in the cycle-built lists

**Status:** Fixed (not yet merged), with [U1-1](110_GUI-Lag.md#u1-1) as planned: a rebuilt list keeps what is ticked on screen for every entry the new cycle still has, a new entry starts as saved, and `ResyncSavedState` works out again whether a tab with unsaved edits still differs from its saved settings. Tests: `CycleSwitchTests` · **Session:** [S1](020_Work-Plan.md#s1)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/DeparturesViewModel.cs:245-261`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DeparturesViewModel.cs#L245-L261); [`FixesViewModel.cs:241-303`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/FixesViewModel.cs#L241-L303); [`AirwaysViewModel.cs:211-224`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AirwaysViewModel.cs#L211-L224); [`ProceduresViewModel.cs:892-922`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs#L892-L922)

**Problem:** The ARTCC, fix use, chart, designation and facility toggles are rebuilt from the saved config every time `LoadCycleDependentLists` runs, which is on every cycle switch on the General tab. Unsaved ticks in those lists are thrown away without a word. `ResyncSavedState` does nothing while the tab is dirty, and nothing calls `MarkDirty`, so the tab stays amber "Unsaved". If the ticks were its only edit, it now matches the saved state but still says it differs. A Save then writes the reverted ticks.

**Proposed:** When the list is already built, take each toggle's current state (or the unsaved value from `CaptureCurrentValues()`) rather than `Get(...)`, for IDs the new cycle still has. Use the saved value only for IDs that are new to the list. If the tab is dirty, end with `MarkDirty()` so the flag is re-evaluated.

**Note:** Same bug on the other tabs: [W2-10](#w2-10).

<a id="w1-13"></a>

### W1-13 · Bug · Med · Unticking every Procedures.json field gives the 8 default fields

**Status:** Fixed (not yet merged), allowing "none" as Nikolas leans. Core uses the defaults only when `JsonFields` is absent and reads a blank one as no optional field; the tab reads a saved blank `JsonFields` (and `ChartTypes`) back as blank. The Harness now sends the default list itself, so its runs are unchanged. Kyle agreed to allowing "none". After his review, Core also reads a blank `ChartTypes` as none (the defaults only when absent), as the tab already did, and the Harness sends the default chart types itself; test `ProcedureSettingsParserTests.blank_chart_types_mean_none`. Settings-Reference.md updated. Tests: `ProcedureSettingsParserTests.blank_json_fields_mean_none_of_the_optional_fields`, `ProceduresViewModelTests.every_json_field_unticked_stays_unticked_and_is_sent_blank` · **Session:** [S1](020_Work-Plan.md#s1)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs:991`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs#L991), 1157-1175, 1211

**Problem:** The Procedures.json Fields card says "The FAA airport IDs and procedure names are always included", so ticking no optional field is a legitimate choice, and nothing in `Validate` stops it. The block then sends `JsonFields=""`, which `ProcedureSettingsParser.ParseJsonFields` reads as "use `DefaultJsonFields`" (ProcedureSettingsParser.cs:266-271). The run writes 8 fields the user unticked. After a save, `LoadFromConfig` also turns the saved `""` back into the defaults. `ChartTypes` has the same reload-as-defaults path (line 1158), though validation keeps it from mattering to a run.

**Proposed:** In Core, use the defaults only when `JsonFields` is absent, and read a present but blank value as "none". Update HarnessSettings.cs:519's "blank = default" comment, or have the Harness omit the key. In `LoadFromConfig`, use the defaults only when `Get("JsonFields") is null` (same for `ChartTypes`). If "none" shouldn't be allowed, add a `Validate` rule instead.

<a id="w3-15"></a>

### W3-15 · Bug · Med · Reset window says settings backups are always deleted, but a reset that keeps settings keeps them

**Status:** Fixed (not yet merged), the second option: the backups line is gone from `AlwaysDeleted`, and `SettingsLoss` starts "Every settings profile and its backups (the copies Undo last save and Import keep)". Tests: `ResetViewModelTests.every_reset_lists_what_always_goes`, and `AppDataResetTests.keeping_the_settings_keeps_each_profiles_backups` with the profile backups' real names · **Session:** [S1](020_Work-Plan.md#s1)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ResetViewModel.cs:22-29`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ResetViewModel.cs#L22-L29)

**Problem:** The backups are `User Configurations\UserConfig-previous.<Profile>.json` and `UserConfig-before-import.<Profile>.json` (Core UserConfigFile.Profiles.cs:310-311). `AppDataReset.RunPending` skips the whole `User Configurations` folder when `KeepSettings` (AppDataReset.cs:85-95), so with "Keep settings" (the default) the backups stay. The test that looks like it covers this (AppDataResetTests.keeping_the_settings_empties_everything_else) only creates the pre-profile top-level `UserConfig.previous.json` / `UserConfig.before-import.json`.

**Proposed:** Either make Core delete `User Configurations\UserConfig-*.json` backups when keeping settings (and add a test with real `UserConfig-previous.Default.json`), or drop the line from `AlwaysDeleted` and say "and their backups" in `SettingsLoss`.

<a id="c1-5"></a>

### C1-5 · Bug · Med · The update's relaunch inherits msiexec's elevation

**Status:** Fixed by Nikolas in [#377](https://github.com/Nikolai558/FE-BUDDY/pull/377) (afd1d39), and confirmed on a Windows 11 VM. Follow-up: [#378](https://github.com/Nikolai558/FE-BUDDY/issues/378). · **Session:** [S1](020_Work-Plan.md#s1)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/UpdateWindowViewModel.cs:252-256`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/UpdateWindowViewModel.cs#L252-L256); [`FeBuddy/FeBuddy.Installer.UI/ViewModels/SetupViewModel.cs:768-795`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer.UI/ViewModels/SetupViewModel.cs#L768-L795); [`FeBuddy/FeBuddy.Installer/Package.wxs:86-91`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer/Package.wxs#L86-L91); [`FeBuddy/FeBuddy.Core/Application/Updates/UpdateInstaller.cs:11-15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Updates/UpdateInstaller.cs#L11-L15)

**Problem:** The elevation comments in Updates are accurate (update runs elevated, uninstall deliberately not). But the "relaunch" after an update is started from inside the elevated msiexec (the external setup UI's `LaunchIfChosen`, or the stock ExitDialog's immediate `LaunchFEBUDDY`), so FE-Buddy comes back elevated - and for a standard user who typed an administrator's password, as that administrator, with the administrator's %APPDATA% (settings, AIRAC cache, credentials). This is the same hazard AppUninstall's remark (AppUninstall.cs:8-13) explains for uninstall. It also means an elevated FE-Buddy can't receive drag-and-drop from Explorer.

**Proposed:** Relaunch FE-Buddy un-elevated as the signed-in user, e.g. start it through `explorer.exe "<path>\FE-BUDDY.exe"` from the setup UI, or another de-elevation route; then test "Update now" on a standard account with over-the-shoulder credentials.

## Low

<a id="c3-17"></a>

### C3-17 · Bug · Low · A custom alias file's address with a secret is echoed when it isn't http(s)

**Status:** Deferred, and lowered from Med to Low: partly true. The parser would echo the address, but the GUI rejects a non-http(s) address first and blocks the run, so a user can't reach it (re-checked at `4773137`). Revisit if anyone asks for FTP or other non-web sources; the fix is then to drop `'{url}'` from that one message · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/ConcatenateAliasesSettingsParser.cs:195-206`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/ConcatenateAliases/ConcatenateAliasesSettingsParser.cs#L195-L206)

**Problem:** `ftp://user:token@host/file` (or `sftp://...?...token=`) parses as an absolute URI, fails the scheme check and is echoed in the exception, which reaches the Review tab and the log, so the secret check never runs.

**Proposed:** Check `UrlSecrets.Describe(uri)` first when `Uri.TryCreate` succeeds, and drop `'{url}'` from the non-http(s) message ("...'s web address is not an http:// or https:// address.").

<a id="o1-42"></a>

### O1-42 · Bug · Low · DEV-CleanBuild.bat misses FeBuddy.Installer.UI and every obj folder

**Status:** Fixed (not yet merged), the loop: it deletes every `bin` and `obj` folder under `.\FeBuddy`, however deep, then `publish` and `TestResults`, and leaves `releases` (the MSI) alone. It still echoes what it deletes and pauses. The Developers README says so too ([D1b-18](500_Developer-Docs.md#d1b-18)). No test (a batch file); checked on a copy of the folder layout · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`DEV-CleanBuild.bat:5-13`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/DEV-CleanBuild.bat#L5-L13)

**Problem:** FeBuddy.Installer.UI\bin (added with the setup window) is not cleaned, nor any `obj\` folder (where stale generated XAML/assets usually cause the problems a clean build is for), nor build.ps1's `publish\`/`releases\` or coverage's `TestResults\`.

**Proposed:** add `".\FeBuddy\FeBuddy.Installer.UI\bin"`, the matching `obj` folder of each project, and `".\FeBuddy\publish"`, `".\FeBuddy\releases"`, `".\FeBuddy\TestResults"`; or replace the list with `for /d /r ".\FeBuddy" %%d in (bin obj) do if exist "%%d" rmdir /s /q "%%d"`.

**Note:** Also found as [D1b-18](500_Developer-Docs.md#d1b-18).

<a id="c1-2"></a>

### C1-2 · Bug · Low · A cancelled run can leave a cycle's parse permanently "cancelled" for the session

**Status:** Fixed (not yet merged), as proposed: `RunFlightAsync` starts the shared parse with `CancellationToken.None` and stops waiting with `WaitAsync(cancellationToken)`, so a cancelled run no longer cancels the parse for everyone. Test: `AiracCycleDataCacheTests.a_cancelled_run_never_leaves_the_parse_it_started_cancelled` (fails on the old code) · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs:482-497`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs#L482-L497), 774-800, 825-834

**Problem:** When `GetAsync(cycleId, runToken)` is the one that starts a cycle's parse (always in a dev launch that leaves parsing out; in a normal launch when the run's flight wins the race to `StartParse`), the shared `ParseTask` is created with the run's token. If the user cancels the run while that parse is queued behind another cycle's parse, `ParseTask` becomes a cancelled task and stays on the entry: state stays `Downloaded`, `StartParse` never starts another (`entry.ParseTask is not null`), and every later `GetAsync` for that cycle re-awaits the cancelled task and throws `TaskCanceledException` until FE-Buddy restarts. The General tab keeps showing it as "parsing…".

**Proposed:** Never give the shared parse a caller's token: in `RunFlightAsync` call `StartParse(entry, CancellationToken.None)` and stop waiting with `.WaitAsync(cancellationToken)` (the parse itself already ignores its token, see `ParseInUseAsync`). Or, in `ObserveParseAsync`/`ParseWhenTurnAsync`, clear `entry.ParseTask` under `_gate` when the parse is cancelled before it starts. Add a test: GetAsync with a token cancelled while queued, then GetAsync again succeeds.

<a id="c1-3"></a>

### C1-3 · Bug · Low · Procedure Charts' optional previous-cycle NASR load can fail the whole run; GetAsync's exception doc is incomplete

**Status:** Fixed (not yet merged), as proposed, keeping the previous-cycle check (Kyle, [decision log](010_Decisions.md#decision-log)): `TryGetCachedNasrAsync` catches everything but cancellation, warns, and the run goes on without the data. `GetAsync` and the first `RunAsync` now document the parse's own exception. Test: `AiracServicePreviousNasrTests.a_previous_cycle_whose_parse_fails_while_the_run_waits_only_warns` (fails on the old code) · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/AiracService.cs:223-245`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/AiracService.cs#L223-L245); [`FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs:393`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/AiracCycleDataCache.cs#L393), 482-485

**Problem:** `TryGetCachedNasrAsync` is documented to answer `null` for a cycle that failed, but when the previous cycle is still loading and its parse then fails, `GetAsync` rethrows the parse's own exception (`RunFlightAsync` awaits the in-flight `ParseTask`), and the NASR parsers throw `FormatException`/`InvalidDataException` (Infrastructure/Nasr). Those escape the filter, so the whole AIRAC run fails although this data is optional. `GetAsync`'s `<exception>` also omits that the returned task faults with the parse's exception, or with `InvalidOperationException` "could not be prepared" (line 511). The first `AiracService.RunAsync` (line 116) documents no exceptions at all though it surfaces these and the second overload's.

**Proposed:** `catch (Exception ex) when (ex is not OperationCanceledException)` in `TryGetCachedNasrAsync`. GetAsync doc: "Thrown when the cycle is not tracked or not published. The returned task faults with InvalidOperationException when the cycle couldn't be downloaded, or with the parse's own exception when its parse failed." Add matching `<exception>` lines to the first `RunAsync`.

<a id="c2-3"></a>

### C2-3 · Bug · Low · Waypoint buffering can remove an airway from Airways.txt, and runs on alias-only runs

**Status:** Fixed (not yet merged), as proposed with a new flag: the parser buffers only when GeoJSON is written, and an airway buffering leaves no legs of is kept with `Airway.IsDrawn` false on its unbuffered line, so it stays in Airways.txt (both scopes) and out of the GeoJSON. Tests: `AirwaySettingsParserTests.an_alias_only_run_never_buffers` and `AirwayServiceTests.an_airway_whose_legs_are_all_shorter_than_the_buffer_is_not_drawn_but_stays_in_the_alias_file` (both fail on the old code) · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwaySettingsParser.cs:129-137`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwaySettingsParser.cs#L129-L137); [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayBuilder.cs:147-162`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayBuilder.cs#L147-L162); [`FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayService.cs:53-54`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Airways/AirwayService.cs#L53-L54)

**Problem:** (1) The parser keeps `BufferAirwayWaypoints` as sent even when `GenerateGeojson = N` (only the distances are gated), and the app always sends it (FeBuddy.Wpf/ViewModels/AirwaysViewModel.cs:281). So an alias-only run still buffers every airway, using the default radii. (2) When every leg of an airway is shorter than its combined radius, AirwayBuilder `continue`s, so the airway is left out of the airway list, and so out of `Airways.txt` too, even in `All` scope. Buffering is meant to shape only what is drawn (builder comment line 147; AirwayService line 54 "All really is all"). It is rare (needs a short airway; more likely at the 10 NM maximum), but the alias file silently loses the airway.

**Proposed:** in the parser, `BufferAirwayWaypoints = readsBufferDistances,`. In AirwayBuilder, when buffering leaves no legs, keep the airway but mark it as not drawn (for example `CrossesRoi = false`, or a new `IsDrawn` flag that AirwayService filters on) instead of `continue`.

<a id="c3-16"></a>

### C3-16 · Bug · Low · NAVAIDs' "nothing written" advisory: one branch unreachable, wrong text when NASR has no NAVAIDs

**Status:** Fixed (not yet merged), as proposed: the checks now run no NAVAIDs at all, then none matching the filters, then none in the ROI. Test: `NavaidServiceTests.a_nasr_file_with_no_navaids_says_none_were_found` (fails on the old code); the other two messages keep their tests · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Navaids/NavaidService.cs:73-79`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Navaids/NavaidService.cs#L73-L79)

**Problem:** With `Roi` null, `FilterToRoi` returns `includedNavaids` itself, so `navaidsInRoi.Count == 0 && includedNavaids.Count > 0` implies an ROI: the "No NAVAIDs were found" branch can never run. When the builder found none at all, the user is told "matched the configured filters" instead.

**Proposed:** `buildResult.Navaids.Count == 0 ? "No NAVAIDs were found, ..." : includedNavaids.Count == 0 ? "No NAVAIDs matched the configured filters, ..." : "No NAVAIDs are inside the region of interest, ..."`.

<a id="c4-11"></a>

### C4-11 · Bug · Low · IncludesProceduresWithFixInFacility returns true when the rule can't run, so the run loads (or waits for) the previous cycle's NASR for nothing

**Status:** Fixed (not yet merged), as proposed: it also returns false with the Everything area or with neither document on, reading them as `Parse` does, and its summary says so. Test: `ProcedureSettingsParserTests.includes_procedures_with_a_fix_in_the_facility_needs_a_document_and_an_area_short_of_everything` (fails on the old code) · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureSettingsParser.cs:57-70`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureSettingsParser.cs#L57-L70); [`FeBuddy/FeBuddy.Core/Application/Airac/AiracService.cs:186-189`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/AiracService.cs#L186-L189)

**Problem:** `Parse` turns the rule off when Area is `Everything` (line 141), and `ProcedureService` uses it only when a document is on (lines 90-92). The tab sends `IncludeProceduresWithFixInFacility` only inside `if (AreListsUsed)` (ProceduresViewModel.cs:1009-1015). With Everything the key is therefore absent, defaults to Y, and this returns true. `AiracService` then calls `TryGetCachedNasrAsync` for the previous cycle and awaits it while that cycle is still loading (AiracService.cs:228-239). The same happens when only the alias file is on. The summary ("Whether a Procedures block will use the ... rule: it is on (the default) and names a facility") is wrong in the same way.

**Proposed:** also return false when `ReadArea(procedureSettings) == SubServiceArea.Everything`, or when neither `GenerateChangesDocument` nor `GenerateProceduresJson` is Y. Reword the summary: "it is on (the default), names a facility, a document is on, and the area isn't Everything."

<a id="c5-8"></a>

### C5-8 · Bug · Low · Card-mode warning says elements "carry only their filters", but they carry filter 0

**Status:** Fixed (not yet merged), as proposed: the warning says the elements carry only filter 0 (shown at every filter setting). Test: `EramToGeojsonServiceTests.from_the_card_only_a_kind_without_card_defaults_is_warned_about_once` checks the new text · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramGeojsonWriter.cs:188-195`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/EramToGeojson/EramGeojsonWriter.cs#L188-L195)

**Problem:** In Card mode, element overrides are ignored (`UseOverrides` false, line 154), so the XML filters are dropped. Every element gets `AlwaysShown` = `[0]` (line 161) and shows at every filter setting. The message suggests each keeps its own filters.

**Proposed:** "…so its {plural} carry only filter 0 (shown at every filter setting) and CRC draws them with its own defaults."

<a id="c1-4"></a>

### C1-4 · Bug · Low · AppEnvironment.RecheckAsync moves the AIRAC date, so the GUI can name a cycle the cache doesn't track

**Status:** Deferred: FE-Buddy should handle a new cycle without a restart (closest to B); the approach is still to be worked out ([Q2](010_Decisions.md#q2)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Launch/AppEnvironment.cs:33-38`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Launch/AppEnvironment.cs#L33-L38), 49-57, 107-111

**Problem:** The shell's re-check and Settings' "check for updates now" (ShellViewModel.cs:750, SettingsViewModel.cs:648) overwrite `LaunchUtcNow`. If FE-Buddy has been open across an AIRAC effective date, `GetAiracCycle` then returns cycles the cache never registered: the General tab's new Next row shows "preparing…" forever, and `GetAsync` for it throws "Cycle 'NNNN' is not tracked" (e.g. SettingsViewModel.cs:1128-1129, a run of that cycle). The two docs quoted are wrong after a re-check. (A dev launch without the clock check has the same drift, since `CheckedUtcDate` then follows `DateTime.UtcNow` live.)

**Proposed:** Make RecheckAsync update only `HasInternetConnection` (and `LaunchUtcSource` if wanted), not `LaunchUtcNow`, so the cycles stay the launch's for the session; or, if the date should move, re-run `AiracCycleDataCache.Instance.PrepareCyclesAsync` when the current cycle changes. Then reword `LaunchUtcNow`'s summary ("The UTC time the launch's time check found ...").

<a id="c5-23"></a>

### C5-23 · Bug · Low · ROI validation accepts "NaN"

**Status:** Fixed (not yet merged), as proposed: a value that isn't finite (`NaN`, `Infinity`, `-Infinity`) is "not a valid decimal coordinate". Test: `RoiFilterTests.is_coordinate_valid_format_rejects_a_value_that_is_not_finite`, each value at each corner (fails on the old code) · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Core/Domain/Geo/RoiFilter.cs:158-168`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Domain/Geo/RoiFilter.cs#L158-L168) (and 71-84)

**Problem:** .NET parses `NaN` under InvariantCulture. NaN fails both comparisons, so IsCoordinateValidFormat passes it, and IsCoordinatesRelativePositionValid passes too (`neLat <= swLat` is false). The settings parser (SubServiceSettingsReader.cs:157-167) and the GUI (MapViewModel.cs:348, GeojsonSubServiceViewModel.cs:925) then accept an ROI whose envelope is NaN.

**Proposed:** In TryParseDegrees, add `|| !double.IsFinite(value)` to the parse failure (message "is not a valid decimal coordinate").

<a id="c6-29"></a>

### C6-29 · Bug · Low · Shared.json's layout stamp is written but never read, so a future step can't reach shared settings

**Status:** Fixed (not yet merged), option (b) ([Q4](010_Decisions.md#q4)): `ReadAll` takes `Shared.json`'s stamp, runs the steps from that layout before the shared keys are picked out, and writes it back stamped current. A newer file, or one whose update can't be worked out, is used as it is and keeps its stamp; one whose update can't be written is tried again at the next launch. The `UserConfigMigrations` remarks and Settings-Reference.md "Changing the layout" updated. Tests: `UserConfigFileTests.an_older_shared_file_is_brought_forward_and_written_back` and `a_newer_shared_file_is_left_alone_and_keeps_its_stamp` (both fail on the old code) · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:665-678`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L665-L678), 720, 181-189, 892

**Problem:** `ReadShared` drops the stamp without reading it, and `Shared.json` never goes through `UserConfigMigrations`. `ReadAll` adds the shared values to `Values` before `BringForward`, so a step that renamed a shared key (say `General.UpdateChannel`) would rename it in memory only. `BringForward` then writes `ProfileValues(...)`, which never contains shared keys, and stamps the profile as current. `Shared.json` keeps the old name, which `ReadShared` then drops because it is not in `SharedByProfiles`, so the setting is lost at the next launch. An older FE-Buddy that writes `Shared.json` also re-stamps it with its own version. Nothing in the remarks or Settings-Reference "Changing the layout" warns about this.

**Proposed:** Either (a) add to the UserConfigMigrations remarks and Settings-Reference.md "Changing the layout": "A step can't rename or re-format a setting every profile shares (&lt;see cref="UserConfigPortability.SharedByProfiles"/>): Shared.json is not brought forward." Or (b) bring `Shared.json` forward in `ReadAll`: take its stamp, `Migrate` its keys, and write it back, the way `BringForward` does for the profile.

<a id="c6-23"></a>

### C6-23 · Bug · Low · CredentialStore leaves the Basic sign-in's UTF-8 bytes (user:password) uncleared, contrary to its own rule

**Status:** Fixed (not yet merged), as proposed: the Basic header's bytes are cleared in a `finally`, and the class remarks name `AuthorizeGitHubToken`. Credentials.md already matched. No new test (no seam to see the buffer); the existing tests pass · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Credentials/CredentialStore.cs:24-30`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Credentials/CredentialStore.cs#L24-L30), 304-306; [`docs/Developers/Credentials.md:103-105`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Credentials.md?plain=1#L103-L105)

**Problem:** The byte[] holding `user:password` is never zeroed. Every other buffer in the class is (Find, ReadOne, ReadAllInfo, DeleteAll, Check, Write). The remarks also skip `AuthorizeGitHubToken` (internal, line 182), which is the second way a secret gets used. Strings can't be cleared, but the remarks only promise byte buffers.

**Proposed:** `byte[] basic = Encoding.UTF8.GetBytes($"{stored.Info.UserName}:{stored.Secret}"); try { request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(basic)); } finally { CryptographicOperations.ZeroMemory(basic); }`. Remarks: "The only way to use one is &lt;see cref="Authorize(HttpRequestMessage, Guid)"/> (or, for FE-Buddy's own GitHub requests, &lt;c>AuthorizeGitHubToken&lt;/c>), which adds it to a request..."

<a id="c5-22"></a>

### C5-22 · Bug · Low · DAT and SCT readers never throw InvalidDataException, so a wrong file is an advisory, not a failed file

**Status:** Deferred: agreed that a wrong file fails, as ERAM's does; check a few real DAT and SCT files first ([Q3](010_Decisions.md#q3)) · **Session:** [After 3.0](020_Work-Plan.md#after-30)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Dat/DatFileReader.cs:39-43`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Dat/DatFileReader.cs#L39-L43),66-124; [`FeBuddy/FeBuddy.Core/Infrastructure/Sct/SctFileReader.cs:30-33`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Sct/SctFileReader.cs#L30-L33),66-119; [`FeBuddy/FeBuddy.Core/Application/Conversions/ConversionFiles.cs:114-118`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Conversions/ConversionFiles.cs#L114-L118); [`docs/Developers/Code-Structure.md:268-269`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Code-Structure.md?plain=1#L268-L269)

**Problem:** Only the ERAM readers follow the rule (EramGeoMapReader.cs:106-110, EramConsoleCommandControlReader.cs:89-93). The picker offers "All files (*.*)" (FileConversionTabViewModel.cs:509). A non-.dat file picked by hand becomes "has no lines to draw" (Warning), and a non-sector file becomes "has nothing FE-Buddy converts". Neither counts as failed in the run summary.

**Proposed:** DatFileReader.Parse: throw `InvalidDataException($"{name} is not a RADAR Video Map: it has no LINE record and no 9900 point of tangency.")` when neither was seen. SctFileReader.Parse: throw when no `[SECTION]` header was found. Or, if the advisory is intended, document that DAT/SCT readers never throw it.

<a id="c7-10"></a>

### C7-10 · Bug · Low · A 404 for the previous cycle is logged as "not published by the FAA yet"

**Status:** Fixed (not yet merged), as proposed: the 404 log line says "not published yet, or no longer served", and the `DtppDownloadOutcome` and `DtppDownloader` docs use [D4](010_Decisions.md#d4)'s "approximately 20 days prior to" (the single constant waits until after 3.0). Test: `DtppDownloaderTests.a_404_is_reported_as_not_yet_published_and_places_nothing` now checks the message · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/DtppDownloader.cs:96-100`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/DtppDownloader.cs#L96-L100); [`FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/Models/DtppDownloadOutcome.cs:14-18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Dtpp/Models/DtppDownloadOutcome.cs#L14-L18)

**Problem:** The cache runs `EnsureCycleHasMetafileAsync` for the previous cycle too (AiracCycleDataCache.DownloadStageAsync → EnsureDtppAsync, for every tracked cycle). For a cycle already in effect or expired, for example on a fresh install mid-cycle, a 404 can only mean the FAA no longer serves that file. Yet the log says "not published yet" at Info, and the outcome doc says a 404 is "normal for a cycle whose effective date is more than about 15-18 days away".

**Proposed:** Word the 404 log so it holds for every cycle, e.g. `$"Cycle {cycleId}'s d-TPP Metafile is not on the FAA's server (not published yet, or no longer served)."`. Or pass the cycle's effective date and say which. Outcome doc: "The FAA's server has no metafile for this cycle (the download 404'd): normal for the next cycle until the FAA posts it."

<a id="o1-30"></a>

### O1-30 · Bug · Low · A failure building the setup window cancels the install instead of falling back

**Status:** Open · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Installer.UI/SetupUI.cs:31-34`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer.UI/SetupUI.cs#L31-L34), 91-95, 224-250

**Problem:** RunWindow has no catch. If loading Setup.xaml or creating SetupWindow throws, `_left` is set, so a full-UI Initialize logs "closed before installing" and throws InstallCanceledException (the install is cancelled, not run with Windows Installer's UI), and the exception escapes the background thread unhandled, which ends the process. The remark's fallback only covers the DLL not loading at all.

**Proposed:** catch in RunWindow, log it through a field read by Initialize, and set a `_failed` flag; in Initialize, after `WaitAny`, `if (_failed) return false;` (IEmbeddedUI: false = continue with Windows Installer's own UI). Then the remark is true for both cases.

<a id="o1-23"></a>

### O1-23 · Bug · Low · Under Windows PowerShell 5.1 (build.cmd) the counter is rewritten with a BOM

**Status:** Open · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Installer/Get-InstallerVersion.ps1:81`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer/Get-InstallerVersion.ps1#L81); [`FeBuddy/build.cmd:3`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/build.cmd#L3)

**Problem:** build.cmd starts Windows PowerShell 5.1, where `-Encoding utf8` writes a UTF-8 BOM (and 5.1's ConvertTo-Json uses a different indentation). The tracked installer-version-counter.json then starts with a BOM, which `FileEncodingTests` fails on if the developer runs the tests before discarding the change, and the diff is noisier than one number. CI is unaffected (pwsh 7).

**Proposed:** `` [IO.File]::WriteAllText($CounterFilePath, ($counter | ConvertTo-Json) + "`n", (New-Object Text.UTF8Encoding $false)) ``.

**Note:** Also found as [D1b-21](#d1b-21).

<a id="t1c-26"></a>

### T1c-26 · Bug · Low · Two tests read the clock twice and can fail at 0000Z

**Status:** Fixed (not yet merged), as proposed: both tests read the date before and after the call, check the date falls in that range, and accept the cycle for either date. Test code only · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Launch/AppEnvironmentTests.cs:31-38`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Launch/AppEnvironmentTests.cs#L31-L38); [`FeBuddy/FeBuddy.UnitTests/Domain/Airac/AiracCycleResolverTests.cs:137-144`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Domain/Airac/AiracCycleResolverTests.cs#L137-L144)

**Problem:** Each test reads `DateTime.UtcNow` once in the test and once inside the code under test. If 0000Z passes between the two reads (on any day for the first, or the day before a cycle starts for the second), they see different dates and the test fails. Rare, but a real flake.

**Proposed:** read the date once and compare with a tolerance, e.g. `DateOnly before = Today(); DateOnly actual = AppEnvironment.CheckedUtcDate; DateOnly after = Today(); Assert.InRange(actual, before, after);` (same pattern in the resolver test).

<a id="w7-4"></a>

### W7-4 · Bug · Low · The "last-chance" crash handler only sees exceptions on the UI thread

**Status:** Fixed (not yet merged), the first option. `App` also hooks `AppDomain.CurrentDomain.UnhandledException`, which writes the same crash report (now `CrashReport`, written straight to disk, not through the log's background writer), and logs `TaskScheduler.UnobservedTaskException`. Architecture.md updated. Tests: `CrashReportTests`; the hooks themselves can't be unit-tested · **Session:** [S1](020_Work-Plan.md#s1)

**Where:** [`FeBuddy/FeBuddy.Wpf/App.xaml.cs:18-23`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/App.xaml.cs#L18-L23),54,116-143; [`docs/Developers/Architecture.md:307-308`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Architecture.md?plain=1#L307-L308)

**Problem:** Only `Application.DispatcherUnhandledException` is hooked. An exception thrown on a thread-pool thread (an `async void` continuation after `ConfigureAwait(false)`, a timer callback, a `Thread`) terminates the process without writing `febuddy-wpf-crash.txt`, so the class summary and Architecture.md ("A crash is written to … by a last-chance handler in App") overstate it. Also `AppLog.Error` at line 137 goes through the background file sink, so with the process about to die the entry usually never reaches the day's log.

**Proposed:** Also hook `AppDomain.CurrentDomain.UnhandledException` to write the same file (share the writer with `OnUnhandled`), and optionally `TaskScheduler.UnobservedTaskException` to log. Or, if staying UI-thread only, reword the summary to "keeps a last-chance handler for unhandled exceptions on the UI thread" and Architecture.md likewise.

**Note:** See also [W7-3](#w7-3) and [W2-11](#w2-11).

<a id="w6-3"></a>

### W6-3 · Bug · Low · The "Towered Airports" layer would include any airport whose tower code has no display mapping, e.g. `TRACON`

**Status:** Open · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Wpf/Map/AiracMapLayers.cs:101-102`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Map/AiracMapLayers.cs#L101-L102)

**Problem:** `AirportFieldMaps.MapTowerType` passes an unmapped code through unchanged (AirportFieldMaps.cs:48-53). The FAA's documented values include `TRACON` (AptCsvDataModel.cs:747), which `TowerTypes` doesn't map (lines 25-33). So `TowerType == "TRACON"` counts as towered, as would any code NASR adds later.

**Proposed:** Decide towered from the code in Core, as in [W6-2](120_Logic-In-GUI.md#w6-2): `TWR_TYPE_CODE` starts with `ATCT`, or is in an explicit towered set.

**Note:** See also [C2-6](120_Logic-In-GUI.md#c2-6) (move the rule into Core).

<a id="w7-22"></a>

### W7-22 · Bug · Low · Card.Popup casts its shadow upwards, onto the map toolbar for the two map menus

**Status:** Open · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Wpf/Theme/Controls.Surfaces.xaml:184-196`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Theme/Controls.Surfaces.xaml#L184-L196)

**Problem:** Every other floating surface (ToolTip, ContextMenu, ComboBox and DatePicker drop-downs, Map.Overlay, toasts, FilterPicker) uses `Direction="270"` (down). Card.Popup is also used by the Map's Base Map and Output Files menus (MapWorkspace.xaml:120,365), which open below their button (`Placement="Bottom"`), so their shadow falls up onto the toolbar. The comment names only the Systems flyout.

**Proposed:** `Direction="270"`, and the comment `(the Systems flyout, the map's Base Map and Output Files menus)`. If the upward shadow was wanted for the Systems flyout (it opens above its button), set it on that one Card instead.

<a id="w2-8"></a>

### W2-8 · Bug · Low · A run can start before the selected cycle's lists exist, and silently drops CRC defaults

**Status:** Fixed (not yet merged), as proposed: Run is enabled only once the picked cycle's lists are in (`IsSelectedCycleLoaded`), re-checked whenever the tabs' readiness changes. Test: `CycleSwitchTests.picking_a_cycle_that_wont_load_says_why_and_the_run_waits` · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:67`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L67), 543-566; [`ArtccBoundariesViewModel.cs:325-348`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ArtccBoundariesViewModel.cs#L325-L348)

**Problem:** `IsReady` only means the *current* cycle is ready; the picked next/previous cycle can still be loading (the run itself waits for it). The settings blocks are built from the tabs as they are, so e.g. ARTCC Boundaries in "one file per ARTCC and altitude" mode has no `OutputFiles()` yet: `CrcDefaultsFor` is sent empty and no per-ARTCC CRC rows, so a user who chose "every GeoJSON file" gets files without isDefaults, and validation can't flag it (`HasGeojsonFiles` is false).

**Proposed:** Use `IsSelectedCycleLoaded` in the RunCommand's can-execute (and raise requery when it changes), or have the run wait for the lists before building the blocks.

<a id="w2-9"></a>

### W2-9 · Bug · Low · Picking a cycle the FAA hasn't published leaves every tab on "Loading AIRAC &lt;id>…" for good

**Status:** Fixed (not yet merged): the tabs say why a cycle won't load - not published, or failed - instead of "Loading"; the screen stops asking for it on every cache change (asking for a failed cycle downloaded it again, and each failure was another change); picking a failed cycle again tries it once more. Test: `CycleSwitchTests.picking_a_cycle_that_wont_load_says_why_and_the_run_waits` · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:442-446`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L442-L446)

**Problem:** `GetAsync` throws at once for a `NotYetPublished` cycle (AiracCycleDataCache.cs:413-416) or after a failed retry. The tabs keep `IsCycleReady = false` with `CycleWaitingText` "Loading AIRAC 2612…", and every later cache `StateChanged` retries and logs the same warning again. The General tab still lets the user pick that cycle.

**Proposed:** On failure tell the tabs the cycle is unavailable (a reason string through `SetReadiness`, shown instead of "Loading…"), don't retry until the cycle's state changes, and/or disable the cycle option while its status is `NotYetPublished`/`Failed`.

<a id="w2-10"></a>

### W2-10 · Bug · Low · A cycle switch rebuilds the ARTCC / NAVAID-type toggles from the saved filter, losing unsaved ticks

**Status:** Fixed (not yet merged), with [W1-14](#w1-14): the same change covers Arrivals, NAVAIDs and ARTCC Boundaries. Tests: `CycleSwitchTests` · **Session:** [S1](020_Work-Plan.md#s1)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ArrivalsViewModel.cs:242-259`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ArrivalsViewModel.cs#L242-L259); [`NavaidsViewModel.cs:151-177`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/NavaidsViewModel.cs#L151-L177); [`ArtccBoundariesViewModel.cs:128-167`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ArtccBoundariesViewModel.cs#L128-L167)

**Problem:** Tick ZOB on Arrivals (unsaved), pick another cycle on the General tab: the list is rebuilt from what is saved, so the tick is silently gone, while `ResyncSavedState` is skipped because the tab is dirty, so it keeps saying "Unsaved changes" with nothing to save.

**Proposed:** When the list already has toggles, rebuild from the current selection (`SelectedArtccs()` / `ExcludedTypeNames()` / `SelectedLocationIds()`) and call `MarkDirty()` afterwards instead of relying on `ResyncSavedState`.

**Note:** Same bug on the other tabs: [W1-14](#w1-14).

<a id="w2-37"></a>

### W2-37 · Bug · Low · Review summaries name the default alias file and always mention the ROI

**Status:** Open · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ArrivalsViewModel.cs:280`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ArrivalsViewModel.cs#L280); [`AirportsViewModel.cs:98`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AirportsViewModel.cs#L98), 102; [`NavaidsViewModel.cs:200`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/NavaidsViewModel.cs#L200)

**Problem:** A file renamed on the File Names tab is still called `Arrivals.txt`/`Airports.txt`/`Navaids.txt` on the Review tab; Airports says "N in the ROI" even when the area is Everything (NAVAIDs and Wx Stations check `HasRoi`).

**Proposed:** Use `Path.GetFileName(x.AliasFilePath)`; wrap the Airports ROI part in `if (HasRoi)` like NavaidsViewModel:191-194.

<a id="w2-5"></a>

### W2-5 · Bug · Low · Review tab reports alias files that a run stopped at the duplicate review deleted

**Status:** Fixed (not yet merged), as proposed: when the user stops at the duplicate review, `AiracService.RunAsync` returns every sub-service's result with `AliasFilePath = null`, and the AIRAC Service screen's `File.Exists` filter is gone ([W2-6](110_GUI-Lag.md#w2-6)). Tests: `AiracServiceTests` · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ArrivalsViewModel.cs:278-281`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ArrivalsViewModel.cs#L278-L281); [`AirportsViewModel.cs:100-103`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AirportsViewModel.cs#L100-L103); [`NavaidsViewModel.cs:198-201`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/NavaidsViewModel.cs#L198-L201)

**Problem:** When the user stops at the duplicate review, Core deletes every alias file (AiracService.cs:430-433) but leaves each sub-service result's `AliasFilePath`/`AliasCommandCount` set, so each tab's Review block still says "Arrivals.txt: N alias command(s)". (AiracServiceViewModel works around it for the file list only, line 610.)

**Proposed:** In Core, return the sub-service results with `AliasFilePath = null` when `duplicateOutcome.Stopped` (records: `with { AliasFilePath = null }`), then drop the `File.Exists` filter at AiracServiceViewModel:610.

<a id="w3-16"></a>

### W3-16 · Bug · Low · Dashboard can miss log entries written while it is being built

**Status:** Open · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs:61-67`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs#L61-L67)

**Problem:** The Dashboard is built at startup while LaunchSequence logs from another thread. An entry recorded after the `DisplayEntries` snapshot but before the subscription is in neither, so it never shows on the Dashboard (it is still in the file).

**Proposed:** Subscribe first, then seed from the snapshot and skip entries already queued (e.g. compare by reference / keep the snapshot count), or add a Core `AppLog.SubscribeWithSnapshot` that takes both under `Gate`.

<a id="w4-8"></a>

### W4-8 · Bug · Low · CRC defaults rows accept any non-blank bcg/style/thickness/size, so an imported or hand-edited bad value passes the tab and fails the run

**Status:** Open · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/Models/EramClassDefault.cs:242-248`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/Models/EramClassDefault.cs#L242-L248), 288-289, 382-406

**Problem:** Only blanks (and, for Text, Y/N and whole-number offsets) are checked. A `bcg` of `50`, a `style` of `Dotted`, an out-of-range size, or a malformed `filters` value restored from a hand-edited file or a settings import is kept as-is, the drop-down shows nothing selected, the tab shows valid, and the run then throws in Core's `CrcDefaultsReader` / `CrcPropertyValidator.ThrowIfInvalid` (ArgumentException) for that sub-service or conversion. The Canonical doc implies the tab reports it; nothing in the tab does.

**Proposed:** Treat a non-blank value not in its options list as invalid ("Pick one of the listed values") in `MissingError`/`HasMissingValues`, and validate `Filters` with the same rule Core uses (better: call a Core `CrcPropertyValidator` per-field check so the rule lives in one place). Reword the Canonical remark to say the row then marks it.

<a id="w1-15"></a>

### W1-15 · Bug · Low · Procedure Charts keeps the previous cycle's d-TPP lookups after a cycle switch

**Status:** Fixed (not yet merged), as proposed: picking another cycle drops the old cycle's chart data and NASR airport lookups until the new cycle's arrive; the facility toggles stay. Test: `ProcedureNameCardsTests.picking_another_cycle_drops_the_old_cycles_chart_data` · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs:102`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs#L102), 492-499, 558-565

**Problem:** Nothing resets the tab when the selected cycle changes. Until the new cycle's NASR data loads and its metafile parses (0.3 s at best, minutes for a cycle still downloading), the old cycle's status stays up: the amber `DtppWarning`, the `Cycle 2610: …` rows and the procedure options all still describe it. The Airports and Airport + Procedure editors resolve IDs and names against the old metafile. The `IsDtppKnown` summary ("for the currently selected cycle") describes a reset that doesn't happen.

**Proposed:** Add a `ClearDtppData()` (`_dtppKnown = false`, clear the lookups, refresh the rows) and call it when the selected cycle changes (e.g. from `OnSelectedCycleChanged`). Alternatively, ignore lookups whose `_dtppCycleId` isn't `SelectedCycle.AiracCycleId`.

<a id="w4-7"></a>

### W4-7 · Bug · Low · A folder that exists but can't be listed is reported as "does not exist"

**Status:** Fixed (not yet merged), with [U1-6](110_GUI-Lag.md#u1-6): `SourceFolderCount` keeps why a folder couldn't be read, and the page says "This folder can't be read: &lt;reason>" and "The source folder can't be read: &lt;reason>". Tests: `ConversionSourceFolderTests` · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/FileConversionTabViewModel.cs:134-141`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/FileConversionTabViewModel.cs#L134-L141), 235-238, 472-482

**Problem:** Access denied (or a device error) leaves the count null, which FolderSummary and RunBlocker both word as "does not exist", sending the user looking for a typo.

**Proposed:** Keep a separate state for "can't be read" (e.g. `_folderError` with the exception message) and show "FE-Buddy can't read this folder: &lt;reason>" / "The source folder can't be read." for it.

<a id="w1-7"></a>

### W1-7 · Bug · Low · Telephony's "real operator" note counts expired U.S. special call signs, which the run drops

**Status:** Open · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs:168-207`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs#L168-L207), 451-455, 779

**Problem:** `ReadRealOperators` re-implements `TelephonyBuilder.Read`'s operator rules in the GUI, but leaves out the expiry check. `TelephonyBuilder` skips a special call sign whose expiration date has passed (TelephonyBuilder.cs:122-126, 249-267), so the user's virtual airline with that 3LD and telephony is written. The editor still says "…is a real operator (…), so yours won't be written", and the row shows the same note. This is also business logic (parsing the FAA pages, choosing which rows are operators) living in a view-model.

**Proposed:** Add a Core method, e.g. `TelephonyBuilder.RealOperators(TelephonyDataCollection data, DateOnly today)` or `TelephonyRealOperators.Read(registerPath, specialCallSignsPath, DateOnly today)`. It should apply the same rules as `Read`: `IsThreeLetterDesignator`/`CommandName`, non-blank telephony, not expired, first wins. Delete `ReadRealOperators` from the view-model, call the Core method from `LoadRealOperators`, and repoint its tests.

**Note:** Also found as [C3-3](120_Logic-In-GUI.md#c3-3), [U1-20](120_Logic-In-GUI.md#u1-20), [T3-7](120_Logic-In-GUI.md#t3-7).

<a id="w5-3"></a>

### W5-3 · Bug · Low · Error tooltips never show on CRC ERAM Defaults drop-downs or on the fan-marker style box

**Status:** Open · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/Cards/CrcDefaultsPanel.xaml:100-132`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/Cards/CrcDefaultsPanel.xaml#L100-L132); [`FeBuddy/FeBuddy.Wpf/Views/NavaidsView.xaml:107-109`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/NavaidsView.xaml#L107-L109)

**Problem:** The ComboBox style `Themed.ComboBox` shows the error message as the ToolTip from a style trigger (Controls.Inputs.xaml:575-578). A local `ToolTip` always beats a style trigger, so these 7 boxes (BCG, Style, Thickness, Size, Opaque, Underline, fan marker style) show a red edge but hover still shows the hint. The User Guide says "Hover a red box for why". The same file's own comment (lines 47-49) explains this for the offset boxes and moves their hint into a Style setter, but the drop-downs were missed.

**Proposed:** do what `OffsetBox` does. Add a small style per hint (e.g. `BasedOn="{StaticResource {x:Type ComboBox}}"` with `<Setter Property="ToolTip" Value="..."/>`), or one style whose ToolTip is bound to `Tag`, and drop the local `ToolTip`. Do the same for NavaidsView's fan marker box.

<a id="o1-2"></a>

### O1-2 · Bug · Low · Any CPU never builds FeBuddy.UnitTests

**Status:** Fixed (not yet merged), as proposed: the `Release|Any CPU.Build.0` line is added for FeBuddy.UnitTests; the x64 and x86 platforms stay. `dotnet build FeBuddy/FeBuddy.sln -c Release` now builds the tests. No test (a solution file) · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.sln:37-38`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.sln#L37-L38)

**Problem:** Every other project/config pair has a `Build.0` line; UnitTests has one for Debug|Any CPU, Release|x64 and Release|x86, but not Release|Any CPU. So `dotnet build FeBuddy/FeBuddy.sln -c Release` (ci.yml Build job, line 98) and a Visual Studio Release build silently skip the test project. (It never existed: `git log -S` finds no commit with it.) Also, the Debug/Release x64 and x86 solution platforms only map every project back to Any CPU (or x64 for the two net472 projects, which pin `<Platform>x64</Platform>` themselves) and are not needed.

**Proposed:** add `{12EE2760-36FD-497C-9D5D-35DE0881580F}.Release|Any CPU.Build.0 = Release|Any CPU` after line 37. Optionally drop the x64/x86 solution platforms (lines 24-25, 27-28 and their mappings).

<a id="t2-8"></a>

### T2-8 · Bug · Low · NasrCsvParserMappingTests computes expected numbers with the PC's culture

**Status:** Fixed (not yet merged), as proposed: `Convert` parses with the invariant culture and `NasrCsvReader`'s own number styles. Test code only · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Nasr/Parsers/NasrCsvParserMappingTests.cs:222-251`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Nasr/Parsers/NasrCsvParserMappingTests.cs#L222-L251)

**Problem:** The helper says it mirrors `NasrCsvReader`, which parses with `CultureInfo.InvariantCulture` (Core/Infrastructure/Nasr/NasrCsvReader.cs:102, 113, 126, 137). On a PC whose decimal separator is a comma (de-DE, fr-FR...), the expected value of "40.123456" becomes 40123456 (or null), so `parser_maps_every_property_from_its_own_column` fails although the parsers are right - the very case NasrCsvReaderTests.cs:14-31 guards.

**Proposed:** Use `int.Parse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture)`, `int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)`, and `double.Parse/TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, ...)` (the reader's own styles, NasrCsvReader.cs:20-21).

<a id="t2-9"></a>

### T2-9 · Bug · Low · FileEncodingTests walks build.ps1's `publish\` and `releases\` output

**Status:** Fixed (not yet merged), as proposed: `SkippedFolders` adds `publish` and `releases`, and its summary names them as `build.ps1`'s output. Test code only · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Repository/FileEncodingTests.cs:10-14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Repository/FileEncodingTests.cs#L10-L14)

**Problem:** Summary says build output is skipped, but `FeBuddy/build.ps1` creates `FeBuddy\publish` (a self-contained publish: hundreds of runtime files) and `FeBuddy\releases` (the MSI) (build.ps1:6, 17-18, 27-28), both gitignored (.gitignore:23, 170). After a local installer build, the test reads every published file and fails on any third-party file that starts with a BOM, which is not a repository file.

**Proposed:** Add `"publish", "releases"` to `SkippedFolders`.

## Reported more than once

Each review slice was read by its own reviewer, so some findings were reported twice. These IDs point at the finding that is kept.

- <a id="d1b-21"></a>**D1b-21** · Under Windows PowerShell (build.cmd) the counter file gets a BOM, and the encoding test then fails · same as [O1-23](#o1-23).
