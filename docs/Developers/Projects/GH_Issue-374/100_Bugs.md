# 100 · Bugs

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

Things that behave wrongly. The Fix-first ones are session [S1](020_Work-Plan.md#s1); the rest are [S2](020_Work-Plan.md#s2), and N1-5 onward, found in the review of the S2 to S5 code, are [S14](020_Work-Plan.md#s14). Each fix should come with a regression test.

**71 findings** (2 High, 16 Med, 53 Low), 62 done. Quote the ID when you comment on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374).

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
| [N1-5](#n1-5) | Bug | Med | Going back to an already-loaded cycle leaves Procedure Charts with no chart data | Fixed | S14 |
| [N1-6](#n1-6) | Bug | Med | Add Custom Aliases keeps a "no alias commands" error after the file is fixed | Fixed | S14 |
| [N1-7](#n1-7) | Bug | Med | A save, map change or import made during a profile switch or import can land in the wrong profile | Fixed | S14 |
| [N1-8](#n1-8) | Bug | Med | A settings write cut short can lose a whole profile | Fixed | S14 |
| [N1-9](#n1-9) | Bug | Med | The restart is offered while the update is still installing; a rolled-back update blocks runs | Open | S14 |
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
| [O1-30](#o1-30) | Bug | Low | A failure building the setup window cancels the install instead of falling back | Fixed | S2 |
| [O1-23](#o1-23) | Bug | Low | Under Windows PowerShell 5.1 (build.cmd) the counter is rewritten with a BOM | Fixed | S2 |
| [T1c-26](#t1c-26) | Bug | Low | Two tests read the clock twice and can fail at 0000Z | Fixed | S2 |
| [W7-4](#w7-4) | Bug | Low | The "last-chance" crash handler only sees exceptions on the UI thread | Fixed | S1 |
| [W6-3](#w6-3) | Bug | Low | The "Towered Airports" layer would include any airport whose tower code has no display mapping, e.g. `TRACON` | Fixed | S2 |
| [W7-22](#w7-22) | Bug | Low | Card.Popup casts its shadow upwards, onto the map toolbar for the two map menus | Fixed | S2 |
| [W2-8](#w2-8) | Bug | Low | A run can start before the selected cycle's lists exist, and silently drops CRC defaults | Fixed | S2 |
| [W2-9](#w2-9) | Bug | Low | Picking a cycle the FAA hasn't published leaves every tab on "Loading AIRAC &lt;id>…" for good | Fixed | S2 |
| [W2-10](#w2-10) | Bug | Low | A cycle switch rebuilds the ARTCC / NAVAID-type toggles from the saved filter, losing unsaved ticks | Fixed | S1 |
| [W2-37](#w2-37) | Bug | Low | Review summaries name the default alias file and always mention the ROI | Fixed | S2 |
| [W2-5](#w2-5) | Bug | Low | Review tab reports alias files that a run stopped at the duplicate review deleted | Fixed | S2 |
| [W3-16](#w3-16) | Bug | Low | Dashboard can miss log entries written while it is being built | Fixed | S2 |
| [W4-8](#w4-8) | Bug | Low | CRC defaults rows accept any non-blank bcg/style/thickness/size, so an imported or hand-edited bad value passes the tab and fails the run | Fixed | S2 |
| [W1-15](#w1-15) | Bug | Low | Procedure Charts keeps the previous cycle's d-TPP lookups after a cycle switch | Fixed | S2 |
| [W4-7](#w4-7) | Bug | Low | A folder that exists but can't be listed is reported as "does not exist" | Fixed | S2 |
| [W1-7](#w1-7) | Bug | Low | Telephony's "real operator" note counts expired U.S. special call signs, which the run drops | Fixed | S2 |
| [W5-3](#w5-3) | Bug | Low | Error tooltips never show on CRC ERAM Defaults drop-downs or on the fan-marker style box | Fixed | S2 |
| [O1-2](#o1-2) | Bug | Low | Any CPU never builds FeBuddy.UnitTests | Fixed | S2 |
| [T2-8](#t2-8) | Bug | Low | NasrCsvParserMappingTests computes expected numbers with the PC's culture | Fixed | S2 |
| [T2-9](#t2-9) | Bug | Low | FileEncodingTests walks build.ps1's `publish\` and `releases\` output | Fixed | S2 |
| [N1-10](#n1-10) | Bug | Low | Procedure Charts can still get the previous cycle's d-TPP data while a new cycle loads | Fixed | S14 |
| [N1-11](#n1-11) | Bug | Low | A reload reopens the General tab on an unpublished saved Next | Fixed | S14 |
| [N1-12](#n1-12) | Bug | Low | An overtaken cycle load clears the newer load's "loading" mark | Fixed | S14 |
| [N1-13](#n1-13) | Bug | Low | "Pick it again to try again" can't work: clicking the selected cycle does nothing | Fixed | S14 |
| [N1-14](#n1-14) | Bug | Low | Background reads on File Conversions, Wx Stations and Telephony fail silently; an older read can win | Fixed | S14 |
| [N1-15](#n1-15) | Bug | Low | A map change whose save failed is lost unless the same setting is changed again | Fixed | S14 |
| [N1-16](#n1-16) | Bug | Low | An unexpected failure during a profile change or an import is never reported | Fixed | S14 |
| [N1-17](#n1-17) | Bug | Low | "Home view saved" is shown even when nothing was saved | Fixed | S14 |
| [N1-18](#n1-18) | Bug | Low | An undo file locked at its first read turns Undo off until the next save | Fixed | S14 |
| [N1-19](#n1-19) | Bug | Low | The cached undo snapshot can disagree with the file; Undo stays on and does nothing | Fixed | S14 |
| [N1-20](#n1-20) | Bug | Low | Marking the News read saves the whole General node | Fixed | S14 |
| [N1-21](#n1-21) | Bug | Low | Copying the profiles shows every argument error as "Choose another folder" | Fixed | S14 |
| [N1-22](#n1-22) | Bug | Low | The File Names tab and the run check name clashes against different lists | Fixed | S14 |
| [N1-23](#n1-23) | Bug | Low | The Map rejects a UTF-16 or ANSI GeoJSON file with a misleading reason | Fixed | S14 |
| [N1-24](#n1-24) | Bug | Low | If the setup window crashes mid-install, msiexec can wait forever | Open | S14 |
| [N1-25](#n1-25) | Bug | Low | The update watcher can crash FE-Buddy if the folder disappears mid-check | Open | S14 |
| [N1-26](#n1-26) | Bug | Low | The update watcher stops looking after 5 minutes without the dll | Open | S14 |
| [N1-27](#n1-27) | Bug | Low | The update watcher logs "changed on disk" when nothing changed | Open | S14 |
| [N1-28](#n1-28) | Bug | Low | Get-InstallerVersion.ps1 writes CRLF; the repository stores LF | Open | S14 |

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

<a id="n1-5"></a>

### N1-5 · Bug · Med · Going back to a cycle that was already loaded leaves Procedure Charts with no chart data

**Status:** Fixed (not yet merged). Picking a cycle that isn't the one loaded clears the screen's loaded cycle, so picking a loaded cycle back hands its data over again; Procedure Charts also drops its NASR lookups when the cycle changes before the metafile arrives. Test: `CycleSwitchTests.going_back_to_a_loaded_cycle_brings_procedure_charts_data_back` (fails on the old code) · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:74-80`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L74-L80), [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:438-443`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L438-L443) (the early return at 440), [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:463-483`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L463-L483); [`FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs:1684-1703`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs#L1684-L1703)

**Problem:** Pick Next on the General tab while it isn't published, failed or is still loading, then go back to Current. Procedure Charts stays on "Chart data appears once the selected cycle's data is loaded", with no procedure lookups and no NASR airport lookups (so the Area's "already included" hint is gone too). Picking Next cleared all of it; picking Current again returns early in `LoadCycleDataAsync` (`_parsedCycleId` is still Current), so nothing hands the data back. Toggling a General row brings back the d-TPP data only.

**Proposed:** In the `CycleChanged` handler, before `LoadCycleDataAsync(picked: true)`, set `_parsedCycleId` and `_parsedForSelectedCycle` to null when `_parsedCycleId` isn't the selected cycle, so going back takes the normal hand-over. It is cheap: the cache and `AiracCycleOptionListBuilder` return what they already have. Optionally drop `_dtppCycleId is null ||` at ProceduresViewModel.cs:1686, so the NASR lookups are cleared too when the cycle changes before the metafile arrives. Test: `CycleSwitchTests.going_back_to_a_loaded_cycle_brings_procedure_charts_data_back` (Next not published; wait until `IsDtppKnown`; pick Next, then Current; `IsDtppKnown` and `DtppCycleId == "2610"`; today false and null).

<a id="n1-6"></a>

### N1-6 · Bug · Med · Add Custom Aliases keeps a "no alias commands" error after the file is fixed, blocking Save and Run

**Status:** Fixed (not yet merged). **Check** on a file row, and a run, look at the files on this PC again, and a look a newer one has overtaken shows nothing, so a fixed file stops blocking Save and Run. Tests: `ConcatenateAliasesViewModelTests.checking_a_fixed_file_clears_its_no_alias_commands_error` and `an_older_check_of_the_files_that_finishes_last_is_not_shown` (both fail on the old code); the run's look has no test, as no unit test drives a run · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs:77-79`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs#L77-L79) (the cache), [`FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs:336-348`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs#L336-L348) (`Validate`), [`FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs:372-404`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs#L372-L404) (no sequence guard), [`FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs:424-467`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs#L424-L467) (`CheckAsync`), [`FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs:591-606`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs#L591-L606); refreshed only at 315, 107-111, 360-363 and 541-548; blocking at [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceSettingsViewModel.cs:79-87`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceSettingsViewModel.cs#L79-L87) and [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:589`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L589), 767-814

**Problem:** Once a file on this PC has been found to have no alias commands, the error stays until the path or the list is edited or the tab reloads. The user fixes the file and clicks **Check**: the row says "Read N alias command(s)", but the red error remains, **Save** refuses, and **Run** stops with "Add Custom Aliases has settings that need fixing". Two checks running at once (on load and after typing) can also finish out of order, and the older result wins.

**Proposed:** `CheckAsync` re-checks the local files (`await CheckLocalFilesAsync()`) for a file row; `RunAsync` does too, when the tab exists, before saving the tabs; `CheckLocalFilesAsync` keeps a generation counter and applies only the latest check. Test: as `a_file_on_this_pc_with_no_alias_commands_cannot_be_saved`, but after fixing the file call `CheckAsync(tab.Sources[0])`; `Error` is null and `Save()` is true.

<a id="n1-7"></a>

### N1-7 · Bug · Med · A page save, a map change or an import made during a profile switch or import can land in the wrong profile

**Status:** Fixed (not yet merged). One shared flag, `SettingsBusy`, is set while a profile change or an import runs: the profile list, Import, New, Rename and Delete are off, and `SettingsWrites` refuses a page's save with a message to wait. `DeferredSave` saves into the profile a change was made in, a profile change or an import saves the map's waiting changes first, and the comment on `Run` says so; `ReplaceAll` takes no profile argument (optional). Tests: `DeferredSaveTests.a_change_made_before_a_switch_is_not_saved_into_the_new_profile`, `SettingsProfilesViewModelTests.the_list_and_import_are_off_while_a_profile_change_or_an_import_runs` and `SettingsWritesTests.a_save_while_a_profile_change_runs_is_refused` (each fails on the old code) · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/SettingsView.xaml:86-88`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Views/SettingsView.xaml#L86-L88) (the profile list has no `IsEnabled`), 42 (Import); [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsProfilesViewModel.cs:161-167`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsProfilesViewModel.cs#L161-L167), [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsProfilesViewModel.cs:359-399`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsProfilesViewModel.cs#L359-L399) (`_busy` only gates the profile card); [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:829-848`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L829-L848), [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:871`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L871), 893, 898-908 (`_importing` only gates Import); [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigTransfer.cs:304-305`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigTransfer.cs#L304-L305), 430-440, 451-459; [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:397-437`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L397-L437) (`ReplaceAll`); [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs:146-162`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs#L146-L162) (`SwitchProfile`); [`FeBuddy/FeBuddy.Wpf/Shell/DeferredSave.cs:57-62`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Shell/DeferredSave.cs#L57-L62), [`FeBuddy/FeBuddy.Wpf/Shell/DeferredSave.cs:82`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Shell/DeferredSave.cs#L82); [`FeBuddy/FeBuddy.Wpf/Shell/SettingsWrites.cs:25-40`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Shell/SettingsWrites.cs#L25-L40)

**Problem:** Since profile changes and imports run off the UI thread, the GUI stays live while they run, which on a OneDrive or network folder takes seconds. Meanwhile the user can pick another profile, start Import, use New, Rename or Delete during an import, change the map, or press Save on any page. `SwitchProfile` sets the new profile in one lock section and reads its settings in a second one, so a page save that gets the lock in between writes the old profile's values into the new profile's file (a Settings **Save** writes all of them). An import applied after a switch lands in the new profile, with the old profile's PC-only values, and its message names the wrong profile. `DeferredSave` reads the active profile when it saves, not when the change was made, so a map click from before a switch is saved into the new profile, which defeats `SaveValues`' own profile check.

**Proposed:** One shared "settings busy" flag (static, with a changed event), set while a profile change or an import runs. Bind the profile list's `IsEnabled` to it; add it to Import, New, Rename and Delete's `CanExecute`; `SettingsWrites.Try*` refuse with a toast while it is set. `DeferredSave.Set` records the profile with the first change after `Forget`, and the save uses that profile. A profile change saves the map's waiting changes (`SaveNowAsync`) before its file step. Optionally give `ReplaceAll` a profile argument, as `SaveValues` has. Tests: `DeferredSaveTests.a_change_made_before_a_switch_is_not_saved_into_the_new_profile` (create `ZNY`; on Default, `Set` a map key; `SwitchProfile("ZNY")`; `SaveNowAsync()`; ZNY's file must not have the key; today it does), and a view-model test that the profile list and Import are disabled while either runs.

**Note:** [T3-6](110_GUI-Lag.md#t3-6)'s Status was corrected with this review: a profile chosen while another change runs is refused (the list goes back to the profile in use), not queued. The comment at `SettingsProfilesViewModel.cs:355` ("Nothing else can change the profiles meanwhile") is untrue while an import runs; fix it with this one.

<a id="n1-8"></a>

### N1-8 · Bug · Med · A settings write cut short (closing FE-Buddy mid-switch, a crash, power loss) can lose a whole profile

**Status:** Fixed (not yet merged). Part 3: every settings write (a profile, `Shared.json`, the undo snapshot, a new profile) goes beside the file first, flushed to disk, then moves over it, and a profile that can't be read as settings is kept as `UserConfig-damaged.<Profile>.json` (a `damaged` backup kind, in Settings-Reference); tests `UserConfigFileTests.a_damaged_profile_is_kept` and `a_write_that_stops_part_way_leaves_the_file_as_it_was` (both fail on the old code). Part 4: closing waits up to 5 seconds for a profile change or an import still writing its files (`App.OnExit`); test `SettingsBusyTests.closing_waits_for_the_file_work_running`, of the new wait · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:1107-1111`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L1107-L1111) (`WriteObject`, a plain `File.WriteAllText`, used for the profile at 588, 701 and 1220, `Shared.json` at 990, the undo snapshot at 572 and 705, and `CreateProfile`, [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs:131`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs#L131)); [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:260-267`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L260-L267) and 1071-1088 (an unreadable file); [`FeBuddy/FeBuddy.Wpf/App.xaml.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/App.xaml.cs) and [`FeBuddy/FeBuddy.Wpf/Views/ShellWindow.xaml.cs:80-89`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Views/ShellWindow.xaml.cs#L80-L89) (nothing waits at close). Already safe: `ReplaceAll` ([`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:405-418`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L405-L418), a temp file and `File.Replace`)

**Problem:** Settings files are written in place, so a write that is cut short leaves a truncated file. A truncated profile reads as invalid JSON: FE-Buddy opens it on defaults, and the next save reads it as empty and writes back only the node being saved, so every other setting in the profile is gone. A truncated `Shared.json` loses the shared settings and the active profile's name. FE-Buddy can also close in the middle of such a write: nothing waits for a profile change (`Working`) or an import at close. A plain switch is mostly safe, but New, Delete and import-as-a-new-profile have later steps that aren't.

**Proposed:** `WriteObject` writes `<file>.tmp`, flushes it to disk (`Flush(true)`), then `File.Move(tmp, path, overwrite: true)`, as `ReplaceAll` already does. Before anything writes over a profile that couldn't be read as JSON, keep a copy as `UserConfig-damaged.<Profile>.json` (a `damaged` `BackupKind`). At close, wait up to 5 seconds for a profile change or import that is running. Tests: `UserConfigFileTests.a_damaged_profile_is_kept` (write `{"General":{"Facility":"ZOB"` as the profile, `ReadAll`, save a map key; the damaged text survives in `UserConfig-damaged.Default.json`; today it is gone), and a test that a failed write leaves the previous file whole.

**Note:** Keep Nik's read and write safety rules (`_profileUnreadable`, `_profileInMemory`, `LoadObjectToUpdate`) as they are; [N1-4](#n1-4) covers a file that is locked, this one a file that is damaged.

<a id="n1-9"></a>

### N1-9 · Bug · Med · The other account's FE-Buddy offers the restart while the update is still installing, and a rolled-back update blocks runs

**Status:** Open; found in the review of the S2 to S5 code at `4893e32` · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs:207-223`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs#L207-L223) (only `FE-BUDDY.dll` events start the settle), 184-187 (not recursive), [`FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs:242-281`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs#L242-L281) (`Check`: 260-264 re-arm and compare in one pass; 264-277 `_raised`, both watches off), 76; [`FeBuddy/FeBuddy.Wpf/Shell/InstalledUpdate.cs:56-61`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Shell/InstalledUpdate.cs#L56-L61), 69-78 (`NewVersion` never cleared); docs: [`FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs:12-14`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs#L12-L14), 84, [`FeBuddy/FeBuddy.Wpf/Shell/InstalledUpdate.cs:16`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Shell/InstalledUpdate.cs#L16), and [020's #379 notes](020_Work-Plan.md#related-issues)

**Problem:** During an update the recreated folder isn't watched, so the first poll that finds the new `FE-BUDDY.dll` raises at once, 0 to 3 seconds after that one file lands, however many of the other files are still to come. Restarting then starts FE-Buddy from a half-written folder, and the new process can lock files the install still needs. If the install then fails or is cancelled and rolls back, nothing looks again (`_raised` is set, both watches are off), and `NewVersion` stays set: every run is refused until a restart, for an update that never happened. The docs promise the offer comes "as soon as the update has finished".

**Proposed:** At the top of `Check`, while Windows Installer's `Global\_MSIExecute` mutex exists (an install is running, rollback included), schedule another check and return. Open it with `Mutex.TryOpenExisting` and dispose it; treat `UnauthorizedAccessException` as "held". Inject the check for tests (an internal `Func<bool> installRunning`). After re-arming on a new folder, schedule a check instead of comparing in the same pass. Reword the docs once true. Tests in `InstalledVersionWatcherTests`: `an_update_is_not_reported_while_an_install_is_running` (no `Updated` while `installRunning` is true; `Updated` once it turns false) and `a_rolled_back_update_is_not_reported`.

**Note:** The two-account VM check of [#379](https://github.com/Nikolai558/FE-BUDDY/issues/379) comes after this fix. Any product's install holds the mutex; that only delays the offer.

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

**Status:** Fixed (not yet merged), as proposed: `RunWindow` catches a failure and notes it, and `Initialize`, still waiting, logs why and returns `false`, so Windows Installer carries on with its own UI at the level asked for (the stock dialogs, or basic progress). A failure later, while installing, no longer ends msiexec: messages and `Shutdown` skip the window. The remark says both. Not unit-tested (it needs a Windows Installer session); built only · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Installer.UI/SetupUI.cs:31-34`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer.UI/SetupUI.cs#L31-L34), 91-95, 224-250

**Problem:** RunWindow has no catch. If loading Setup.xaml or creating SetupWindow throws, `_left` is set, so a full-UI Initialize logs "closed before installing" and throws InstallCanceledException (the install is cancelled, not run with Windows Installer's UI), and the exception escapes the background thread unhandled, which ends the process. The remark's fallback only covers the DLL not loading at all.

**Proposed:** catch in RunWindow, log it through a field read by Initialize, and set a `_failed` flag; in Initialize, after `WaitAny`, `if (_failed) return false;` (IEmbeddedUI: false = continue with Windows Installer's own UI). Then the remark is true for both cases.

<a id="o1-23"></a>

### O1-23 · Bug · Low · Under Windows PowerShell 5.1 (build.cmd) the counter is rewritten with a BOM

**Status:** Fixed (not yet merged), much as proposed: `Get-InstallerVersion.ps1` writes the counter itself, as UTF-8 without a BOM, laid out as the tracked file is, so Windows PowerShell 5.1 and PowerShell 7 write the same bytes and a build changes one number. Tests: `InstallerVersionScriptTests`, which run the script under Windows PowerShell 5.1 · **Session:** [S2](020_Work-Plan.md#s2)

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

**Status:** Fixed (not yet merged), in [S5](020_Work-Plan.md#s5) with [C2-6](120_Logic-In-GUI.md#c2-6): Core decides it from the code (`Airport.IsTowered`, set by `AirportBuilder`): `TWR_TYPE_CODE` is `ATCT` or starts with it. A plain `TRACON`, `NON-ATCT`, a blank and any code NASR adds later aren't towered. Tests: `AirportBuilderTests` · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Wpf/Map/AiracMapLayers.cs:101-102`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Map/AiracMapLayers.cs#L101-L102)

**Problem:** `AirportFieldMaps.MapTowerType` passes an unmapped code through unchanged (AirportFieldMaps.cs:48-53). The FAA's documented values include `TRACON` (AptCsvDataModel.cs:747), which `TowerTypes` doesn't map (lines 25-33). So `TowerType == "TRACON"` counts as towered, as would any code NASR adds later.

**Proposed:** Decide towered from the code in Core, as in [W6-2](120_Logic-In-GUI.md#w6-2): `TWR_TYPE_CODE` starts with `ATCT`, or is in an explicit towered set.

**Note:** See also [C2-6](120_Logic-In-GUI.md#c2-6) (move the rule into Core).

<a id="w7-22"></a>

### W7-22 · Bug · Low · Card.Popup casts its shadow upwards, onto the map toolbar for the two map menus

**Status:** Fixed (not yet merged), as proposed, both ways: `Card.Popup` casts its shadow down, for the map's two menus, and the Systems flyout, which opens above its button, keeps its upward shadow on a variant of its own, `Card.Popup.Above` · **Session:** [S2](020_Work-Plan.md#s2)

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

**Status:** Fixed (not yet merged), as proposed, and on Departures, Airways and Telephony too, which had the same default name: each summary names the alias file the run wrote, and Airports counts the airports in the ROI only when the tab uses one. Tests: `RunResultSummaryTests` · **Session:** [S2](020_Work-Plan.md#s2)

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

**Status:** Fixed (not yet merged), the second way proposed: `AppLog.FollowDisplay` subscribes and takes the display's entries under one lock, and `Write` takes the subscribers with the entry, so an entry logged meanwhile is in the snapshot or the callbacks, exactly once. The Dashboard seeds and follows through it. Tests: `AppLogTests` · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs:61-67`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs#L61-L67)

**Problem:** The Dashboard is built at startup while LaunchSequence logs from another thread. An entry recorded after the `DisplayEntries` snapshot but before the subscription is in neither, so it never shows on the Dashboard (it is still in the file).

**Proposed:** Subscribe first, then seed from the snapshot and skip entries already queued (e.g. compare by reference / keep the snapshot count), or add a Core `AppLog.SubscribeWithSnapshot` that takes both under `Gate`.

<a id="w4-8"></a>

### W4-8 · Bug · Low · CRC defaults rows accept any non-blank bcg/style/thickness/size, so an imported or hand-edited bad value passes the tab and fails the run

**Status:** Fixed (not yet merged), the better way proposed: `CrcDefaultsReader.IsValidValue(kind, property, value)` parses each value as the readers do and holds it to `CrcPropertyValidator`'s ranges and styles. A row marks a value it refuses ("Pick one of the listed values.", or the filters' rule) whether or not it is required, and counts it as not filled in. The Canonical remark says so. Tests: `CrcDefaultsReaderTests`, `EramClassDefaultTests` · **Session:** [S2](020_Work-Plan.md#s2)

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

**Status:** Fixed (not yet merged), as proposed: `TelephonyBuilder.RealOperators` gives the operators a run writes - the rows `Read` keeps, expired U.S. special call signs left out, the first of each - from the parsed pages or from FE-Buddy's kept copies, on the run's date. The tab's own reader is gone. The note now names the company as the card prints it, upper case. Tests: `TelephonyBuilderTests` · **Session:** [S2](020_Work-Plan.md#s2)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs:168-207`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs#L168-L207), 451-455, 779

**Problem:** `ReadRealOperators` re-implements `TelephonyBuilder.Read`'s operator rules in the GUI, but leaves out the expiry check. `TelephonyBuilder` skips a special call sign whose expiration date has passed (TelephonyBuilder.cs:122-126, 249-267), so the user's virtual airline with that 3LD and telephony is written. The editor still says "…is a real operator (…), so yours won't be written", and the row shows the same note. This is also business logic (parsing the FAA pages, choosing which rows are operators) living in a view-model.

**Proposed:** Add a Core method, e.g. `TelephonyBuilder.RealOperators(TelephonyDataCollection data, DateOnly today)` or `TelephonyRealOperators.Read(registerPath, specialCallSignsPath, DateOnly today)`. It should apply the same rules as `Read`: `IsThreeLetterDesignator`/`CommandName`, non-blank telephony, not expired, first wins. Delete `ReadRealOperators` from the view-model, call the Core method from `LoadRealOperators`, and repoint its tests.

**Note:** Also found as [C3-3](120_Logic-In-GUI.md#c3-3), [U1-20](120_Logic-In-GUI.md#u1-20), [T3-7](120_Logic-In-GUI.md#t3-7).

<a id="w5-3"></a>

### W5-3 · Bug · Low · Error tooltips never show on CRC ERAM Defaults drop-downs or on the fan-marker style box

**Status:** Fixed (not yet merged), one style for every box rather than one per hint: a new `bhv:FieldState.Hint` is the tool-tip the themed TextBox and ComboBox styles show, as a setter, so their error trigger replaces it. The seven boxes, and the offset boxes (their `OffsetBox` style is gone), set it instead of `ToolTip`; no other box in the app had both. Tests: `FieldStateTests` · **Session:** [S2](020_Work-Plan.md#s2)

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

<a id="n1-10"></a>

### N1-10 · Bug · Low · Procedure Charts can still get the previous cycle's d-TPP data while a new cycle loads

**Status:** Fixed (not yet merged). The Procedures tab's metafile is handed over only while the selected cycle is loaded, and a result lands only if its cycle is still the one selected. Test: `CycleSwitchTests.a_cycle_that_hasnt_loaded_never_gets_the_last_cycles_metafile` (fails on the old code) · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:517-530`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L517-L530) (519; 529 pairs it with `PreviousOfSelectedCycleId`), [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:556-564`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L556-L564) (560)

**Problem:** [W1-15](#w1-15)'s fix is partly undone: these lines use `_parsedCycleId`, the last cycle loaded, not the one selected. With a new cycle picked that hasn't loaded, toggling a sub-service or an output on the General tab loads the old cycle's metafile and applies it, and a metafile load still running at the switch lands the same way. Procedure Charts then says "Cycle 2610: … airports" and resolves picks against 2610 while 2611 is selected.

**Proposed:** At 519: return unless the selected cycle is loaded, and use `SelectedCycleId`. At 560: compare with `SelectedCycleId`. Test: in `CycleSwitchTests`, wait until `DtppCycleId == "2610"`, pick an unpublished Next, untick a General row; `DtppCycleId` is null (today "2610").

<a id="n1-11"></a>

### N1-11 · Bug · Low · Discard, Undo, an import or a profile switch reopens the General tab on an unpublished saved Next

**Status:** Fixed (not yet merged). `LoadFromConfig` opens an unpublished saved Next on Current, as launch does, with the same "Opened on the current AIRAC cycle" message, no `CycleChanged` and no unsaved mark. Test: `AiracGeneralTabViewModelTests.a_reload_keeps_an_unpublished_saved_next_on_current` (fails on the old code) · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs:232-259`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs#L232-L259) (239-240), [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs:268-276`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs#L268-L276), 176-213; [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceSettingsViewModel.cs:149-183`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceSettingsViewModel.cs#L149-L183); [`FeBuddy/FeBuddy.Wpf/Shell/ConfigPages.cs:44-62`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Shell/ConfigPages.cs#L44-L62)

**Problem:** With `CyclePosition=Next` saved and Next not published, the screen opens on Current with a message, as [Q7](010_Decisions.md#q7) decided. But a reload (**Discard**, **Undo last save**, a settings import, a profile switch) goes to Next: every tab says "AIRAC 2611 isn't published… Pick another cycle" and Run is off, until some unrelated cache event runs the fallback and jumps back to Current with the message.

**Proposed:** In `LoadFromConfig`, call `OpenOnCurrentWhileSavedNextIsUnpublished()` right after `_cycleFromSave = true;` (`_loading` is set, so no `CycleChanged` and no unsaved mark); line 66 still covers the first load. Optionally skip the message on a reload. Test: `AiracGeneralTabViewModelTests.a_reload_keeps_an_unpublished_saved_next_on_current` (save Next, 2611 not published; untick a row, `RevertChanges()`; Current, not dirty, no `CycleChanged`).

<a id="n1-12"></a>

### N1-12 · Bug · Low · A cycle load that was overtaken clears the "loading" mark of the one that overtook it

**Status:** Fixed (not yet merged). A finished load clears the "loading" mark only when it is its own. Test: `CycleSwitchTests.an_overtaken_load_leaves_the_newer_pick_loading` (fails on the old code) · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:465`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L465), [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:489-490`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L489-L490) (the guard at 440)

**Problem:** Pick B, then C while B is loading. When B finishes it sets `_loadingCycleId` to null while C is still loading, so every later cache state change starts another load of C. The download and parse are shared, but each load hands over again: every tab rebuilds its lists and the metafile is parsed again ([N1-29](110_GUI-Lag.md#n1-29)). A stale failure also redoes the tabs' readiness as if C weren't loading.

**Proposed:** At both places: `if (_loadingCycleId == cycleId) _loadingCycleId = null;`. Test: expose `LoadingCycleId` internally; pick Next, then Previous, with Next's download held; release it; `LoadingCycleId` is still Previous (today null).

<a id="n1-13"></a>

### N1-13 · Bug · Low · "Pick it again on the General tab to try again" can't work: clicking the selected cycle does nothing

**Status:** Fixed (not yet merged). Each cycle row has a `PickCommand` on its radio button, run on every click; picking the selected cycle again while it has failed raises `CycleRetryRequested`, which loads it again, so "Pick it again on the General tab to try again" now works. Test: `CycleSwitchTests.picking_a_failed_cycle_again_tries_it_again` (fails on the old code) · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:413-419`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L413-L419) (416); [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs:399-409`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs#L399-L409), [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs:108-116`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs#L108-L116); [`FeBuddy/FeBuddy.Wpf/Views/AiracGeneralTabView.xaml:100-102`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Views/AiracGeneralTabView.xaml#L100-L102)

**Problem:** Clicking a radio button that is already checked changes nothing, so no retry happens. The only way is to pick another cycle and come back, which runs into [N1-5](#n1-5) when the other cycle is the loaded one.

**Proposed:** Give `CycleOption` a `PickCommand` bound on the radio button (a command runs on every click). When the cycle picked is already selected and failed, raise a `CycleRetryRequested` event, which `AiracServiceViewModel` handles with `LoadCycleDataAsync(picked: true)`. Test: a 2611 download that always throws; pick Next, then `PickCommand.Execute`; a third download attempt (today none).

<a id="n1-14"></a>

### N1-14 · Bug · Low · Background reads on File Conversions, Wx Stations and Telephony aren't reported when they fail, and an older read can win

**Status:** Fixed (not yet merged). A folder count that throws shows "This folder can't be read: …" and logs a warning; Wx Stations' and Telephony's looks at their kept copies log a failure and show "FE-Buddy couldn't read its copy: …", and an overtaken look shows nothing; Telephony's real-operator and virtual airline list reads log a failure too. Tests: `FileConversionTabViewModelTests.a_count_that_fails_says_the_folder_cant_be_read`, `WxStationsViewModelTests.a_look_that_fails_says_so_and_an_older_one_finishing_last_doesnt_win` and `TelephonyViewModelTests.an_older_look_at_the_faa_pages_copy_finishing_last_doesnt_win_and_a_failed_one_says_why` (each fails on the old code); the real-operator read has no test, as it reads nothing outside the app · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/FileConversionTabViewModel.cs:499-529`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/FileConversionTabViewModel.cs#L499-L529) (516; 155, 260); [`FeBuddy/FeBuddy.Wpf/ViewModels/WxStationsViewModel.cs:79`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/WxStationsViewModel.cs#L79), 86-90; [`FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs:106-127`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs#L106-L127), 173-188, 204-217; [`FeBuddy/FeBuddy.Wpf/App.xaml.cs:164-165`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/App.xaml.cs#L164-L165)

**Problem:** These run without anyone awaiting them. A folder count that throws anything but a cancellation leaves "Counting files…" on screen and blocks Run until the folder changes (Core catches the usual IO errors, so this needs something unexpected). A failed Wx Stations or Telephony read leaves the status empty or stale, logged only when the garbage collector finds the task. An older Telephony status or real-operator read that finishes last (after a run's refresh, say) overwrites the newer one; Wx Stations has no ordering guard either.

**Proposed:** `CountFolderAsync`: a last `catch (Exception)` that, if this is still the current count, shows "This folder can't be read: …", clears `_countingFolder` and logs a warning. Wx Stations and Telephony: try/catch with a warning and a "couldn't read its copy: …" status, and a version number per refresh checked before applying the result. Tests: a count that throws "boom" shows "This folder can't be read: boom"; with Telephony's describe step injectable (as `RefreshVatsimRadarList` is), an older refresh released last doesn't win.

<a id="n1-15"></a>

### N1-15 · Bug · Low · A map change whose save failed is lost unless the same setting is changed again

**Status:** Fixed (not yet merged). A failed save's values are kept, under a lock, and saved with the next save - after any change, or as FE-Buddy closes - unless the same setting changed since; `Forget` drops them, and the timer isn't restarted for them. The remark says so. Test: `DeferredSaveTests.a_failed_save_is_tried_again_with_the_next_change` (fails on the old code) · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Wpf/Shell/DeferredSave.cs:21-22`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Shell/DeferredSave.cs#L21-L22) (the remark), [`FeBuddy/FeBuddy.Wpf/Shell/DeferredSave.cs:89-99`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Shell/DeferredSave.cs#L89-L99) (`TakeChanges` empties the queue), [`FeBuddy/FeBuddy.Wpf/Shell/DeferredSave.cs:101-116`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Shell/DeferredSave.cs#L101-L116), 118-128; [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:537-552`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L537-L552) (`SaveValues` rolls memory back)

**Problem:** When a map save fails (a locked file), the change stays on screen, but it is no longer queued and memory has been rolled back. A later change to another map setting saves only that one, and closing FE-Buddy saves nothing for it, so the choice is gone at the next launch. The remark says "the next change tries again"; only changing the same setting again does.

**Proposed:** Keep the failed values (a lock-protected `_failed` dictionary). `TakeChanges` adds back the failed keys not changed since; `Forget` clears them. Don't restart the timer for them. Reword the remark to say what happens. Test: `DeferredSaveTests.a_failed_save_is_tried_again_with_the_next_change` (make `PreviousFilePath` a folder so the save fails; `Set` Gridlines, `SaveNowAsync` is false; remove the folder; `Set` the opacity, `SaveNowAsync` is true; the file has Gridlines too).

<a id="n1-16"></a>

### N1-16 · Bug · Low · An unexpected failure during a profile change or an import is never reported

**Status:** Fixed (not yet merged). `Run`'s continuation reads `work.Exception` rather than `work.Result` and catches a failure as the pages follow; `ImportAsync` has a last `catch (Exception)`. Both log an error and show it. Test: `SettingsProfilesViewModelTests.a_failure_after_the_switch_is_reported` (fails on the old code); the import's catch has no test, as the import opens file dialogs · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsProfilesViewModel.cs:380-394`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsProfilesViewModel.cs#L380-L394) (`work.Result` rethrows a step's exception; `then` runs `_reload`, [`FeBuddy/FeBuddy.Wpf/Shell/ConfigPages.cs:72-75`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Shell/ConfigPages.cs#L72-L75), outside any try); [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:137`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L137) (`_ = ImportAsync()`), [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:850-934`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L850-L934) (only `UserConfigTransferException` is caught; e.g. `UserConfigTransfer.cs:153` can throw `FileNotFoundException`); [`FeBuddy/FeBuddy.Wpf/App.xaml.cs:164-166`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/App.xaml.cs#L164-L166)

**Problem:** Any exception these steps don't expect is stored in a task nobody awaits, so there is no message and no refresh, and the profile list can keep showing a profile that isn't in use. The only trace is an "unobserved task exception" log line, written whenever the garbage collector gets to it.

**Proposed:** In `Run`'s continuation use `work.Exception?.GetBaseException()` instead of `work.Result`, and wrap `then(done)` in a try/catch that logs and shows an error. Give `ImportAsync` a last `catch (Exception)` that logs and shows an error. Test: `SettingsProfilesViewModelTests.a_failure_after_the_switch_is_reported` (`reload` throws; choose `ZNY`; an Error toast and an Error log entry).

**Note:** The same pattern on the AIRAC tabs is [N1-14](#n1-14).

<a id="n1-17"></a>

### N1-17 · Bug · Low · "Home view saved" is shown even when nothing was saved

**Status:** Fixed (not yet merged). `DeferredSave.Save` returns what `SaveValues` returns, so `SaveNowAsync` is `false` once another profile is in use and "Home view saved" doesn't show. Test: `DeferredSaveTests.a_change_made_before_a_switch_is_not_saved_into_the_new_profile` checks the `false` (fails on the old code) · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs:452-459`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs#L452-L459); [`FeBuddy/FeBuddy.Wpf/Shell/DeferredSave.cs:77-80`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Shell/DeferredSave.cs#L77-L80), [`FeBuddy/FeBuddy.Wpf/Shell/DeferredSave.cs:106-107`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Shell/DeferredSave.cs#L106-L107)

**Problem:** `DeferredSave.Save` ignores `SaveValues`' `false` (it refused, e.g. the profile changed), and returns `true`; with nothing waiting, `SaveNowAsync` also returns `true`. So the Map's "Home view saved" message can show when the home view wasn't saved.

**Proposed:** `Save` returns what `SaveValues` returns; the message shows only on `true`. Test: a `DeferredSaveTests` case where `SaveValues` refuses returns `false`.

<a id="n1-18"></a>

### N1-18 · Bug · Low · An undo file locked the first time it is read turns "Undo last save" off until the next save

**Status:** Fixed (not yet merged). A missing or invalid snapshot still reads as empty; one that is there but can't be read is left unread, so the next `CanUndo` reads it again, and a save fails rather than write it back with one node. Test: `UserConfigFileTests.a_snapshot_locked_at_first_ask_is_read_again` (fails on the old code) · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:1090-1105`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L1090-L1105), [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:1114-1125`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L1114-L1125) (the cached snapshot), [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:569`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L569)

**Problem:** The first `CanUndo` reads the undo snapshot file with `LoadObjectOrEmpty`, which turns any error into an empty object, and caches it. If the file is locked at that moment (antivirus, a sync tool), every **Undo last save** stays greyed out until a save, undo or profile switch, although the snapshot is on disk. At 569 the same read treats a locked snapshot as empty and rewrites it with one node, losing the other pages' snapshots.

**Proposed:** A missing file or invalid JSON still caches an empty snapshot. On `IOException` or `UnauthorizedAccessException`, answer empty for this call only and leave `_undoSnapshot` null, so a later call reads again. In `WriteNode`, let that error through (the save then fails and says so, as [N1-4](#n1-4) does for the profile). Test: `UserConfigFileTests.a_snapshot_locked_at_first_ask_is_read_again` (save a node twice; re-read; with the snapshot opened `FileShare.None`, `CanUndo` is false; after closing it, `CanUndo` is true; today it stays false).

<a id="n1-19"></a>

### N1-19 · Bug · Low · The cached undo snapshot can disagree with the file: Undo stays on and does nothing

**Status:** Fixed (not yet merged). `Undo`'s early return keeps what is on disk, a failed save forgets the cache (and puts the snapshot back once it was written), `DeleteProfile` forgets it, and the doc is fixed. Test: `UserConfigFileTests.undo_with_no_snapshot_on_disk_turns_undo_off` (fails on the old code) · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:684-694`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L684-L694) (`Undo`'s early returns), [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:569-573`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L569-L573), 585-588, [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:108-112`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L108-L112) (the doc), 642-646; [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs:206-226`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs#L206-L226) (`DeleteProfile`); [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceSettingsViewModel.cs:161-170`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceSettingsViewModel.cs#L161-L170)

**Problem:** When the snapshot file is gone, `Undo` returns early without updating the cache, so **Undo last save** stays on and silently does nothing. `WriteNode` truncates the snapshot file before writing it (572); if that write fails, the cache no longer matches the disk. `DeleteProfile` doesn't forget the cache (harmless today: only a profile not in use can be deleted, and the cache is keyed by path). The doc at 108-112 says the cache always matches the file.

**Proposed:** In `Undo`'s early returns, update the cache to what is on disk; the page asks `CanUndo` again after a `false`. In `WriteNode`, forget the cache if a write fails ([N1-8](#n1-8)'s write through a temp file also removes the truncation). Call `ForgetUndoSnapshot()` in `DeleteProfile`. Fix the doc. Test: `UserConfigFileTests.undo_with_no_snapshot_on_disk_turns_undo_off` (save twice; delete the snapshot file; `Undo` is false, then `CanUndo` is false; today it stays true).

<a id="n1-20"></a>

### N1-20 · Bug · Low · Marking the News read saves the whole `General` node, over the default ROI's undo snapshot

**Status:** Fixed (not yet merged). The Dashboard's new `MarkNewsRead` saves `NewsLastOpen` alone through the new `SettingsWrites.TrySaveShared`; `SaveShared`'s doc lists its IO errors. Test: `SettingsWritesTests.marking_the_news_read_saves_only_that_setting` (fails on the old code) · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs:317-330`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs#L317-L330) (327: `TrySave("General", …)`); [`FeBuddy/FeBuddy.Wpf/Shell/DefaultRoiStore.cs:22`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Shell/DefaultRoiStore.cs#L22) (`General.DefaultRoi`); [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:564-589`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L564-L589), [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:621-636`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L621-L636) (`SaveShared`); [`FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/UserConfigLayoutTests.cs:161-178`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/UserConfigLayoutTests.cs#L161-L178)

**Problem:** The Dashboard saves the whole `General` node to record that the News was read, though that key (`NewsLastOpen`) lives only in `Shared.json`. The save rewrites the profile and every shared `General.*` key, and replaces the undo snapshot's `General` entry, dropping the default ROI's (`General.DefaultRoi`). It breaks [H5](010_Decisions.md#h5) (one settings node per page), and the layout test doesn't list `General`, so it doesn't notice.

**Proposed:** Save the key alone with `UserConfigFile.SaveShared(UserConfigKeys.NewsLastOpen)` through a new `SettingsWrites.TrySaveShared` (`SaveShared` can throw IO errors, whatever its doc says; fix the doc). Test: move the save into an internal `MarkNewsRead`; set the default ROI twice, then `MarkNewsRead`; the snapshot file is unchanged (today it changes) and `Shared.json` has `NewsLastOpen`.

<a id="n1-21"></a>

### N1-21 · Bug · Low · Copying the profiles to a folder: every argument error is shown as "Choose another folder"

**Status:** Fixed (not yet merged). New `UserConfigFile.IsInsideAppData`, checked before the copy (`CopyProfilesTo` still refuses one too); any other argument error is logged and shown as a failed copy. Test: `UserConfigFileTests.a_copy_inside_fe_buddys_folder_is_recognised`, of the new check; the page's catch opens a file dialog, so it has no test · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:1096-1107`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L1096-L1107); [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs:318-348`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs#L318-L348) (327 is the intended throw; 322-323, 331 and 342 can throw `ArgumentException` too)

**Problem:** The page catches every `ArgumentException` from `CopyProfilesTo` as "the folder is inside FE-Buddy's own, choose another", and logs nothing. Rare: a path from the save dialog almost never reaches the other throws.

**Proposed:** Add `UserConfigFile.IsInsideAppData(path)` in Core and check it before copying (keep 327 as a guard); any other `ArgumentException` goes to the logged error branch. Test: `UserConfigFileTests.a_copy_inside_fe_buddys_folder_is_recognised`.

<a id="n1-22"></a>

### N1-22 · Bug · Low · The File Names tab and the run check name clashes against different lists

**Status:** Fixed (not yet merged). `OutputFileNamesParser.KeptFileKeys` is public, and the File Names tab checks a new name against it as the run does; the run's comment is reworded, and `a_run_refuses_a_name_another_file_keeps` is kept. Test: `FileNamesViewModelTests.a_name_another_file_keeps_is_refused_even_when_this_run_doesnt_write_it` (fails on the old code) · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/OutputFileNamesParser.cs:32-40`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Application/Airac/OutputFileNamesParser.cs#L32-L40) (`OtherFileKeys`), 73-80; [`FeBuddy/FeBuddy.Wpf/ViewModels/FileNamesViewModel.cs:252`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/FileNamesViewModel.cs#L252), 268-271; [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:338-352`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L338-L352); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Models/OutputFileNamesTests.cs:156-164`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Application/Airac/Models/OutputFileNamesTests.cs#L156-L164)

**Problem:** The tab checks a new name against the files this run writes; the run checks it against every alias file FE-Buddy can write, always. So with Airways included and Airports not, renaming `Airways.txt` to "Airports" shows no error on the tab, and Run then stops with "Airports.txt is already the name of another file". The test `a_run_refuses_a_name_another_file_keeps` asserts that. The other way round (the tab refusing what the run allows) can't be reached today.

**Proposed:** Make the tab use the run's rule (every run of a cycle writes into the same folder, so a name FE-Buddy uses for another file there is unsafe even if this run doesn't write it): expose `OutputFileNamesParser.KeptFileKeys` and have `FileNamesViewModel.Validate` pass `rows.Select(r => r.Key).Concat(KeptFileKeys)` to `Clashes`. Fix the comment at `OutputFileNamesParser.cs:73-75`. Test: `FileNamesViewModelTests` (only Airways listed; rename `Airways.txt` to "Airports"; the row shows the clash error).

<a id="n1-23"></a>

### N1-23 · Bug · Low · The Map rejects a UTF-16 or ANSI GeoJSON file with a misleading reason

**Status:** Fixed (not yet merged). The Map still reads UTF-8 only, and says so: a UTF-16 or UTF-32 file (its byte-order mark, or a zero in its first two bytes) fails with "Not UTF-8 text (saved as UTF-16) - save it as UTF-8", an ANSI one with "Not UTF-8 text - save it as UTF-8"; `ReadFile`'s doc is reworded as proposed. Tests: `GeojsonFileReaderTests.a_file_not_saved_as_utf8_says_to_save_it_as_utf8` and `text_saved_as_utf16_or_utf32_is_named` (fail on the old code) · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Geojson/GeojsonFileReader.cs:43-45`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Geojson/GeojsonFileReader.cs#L43-L45) (the doc), 56-57, 65-69, [`FeBuddy/FeBuddy.Core/Infrastructure/Geojson/GeojsonFileReader.cs:78-81`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Geojson/GeojsonFileReader.cs#L78-L81) ("Not valid JSON"), [`FeBuddy/FeBuddy.Core/Infrastructure/Geojson/GeojsonFileReader.cs:92-95`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Geojson/GeojsonFileReader.cs#L92-L95) ("Not laid out as GeoJSON"), 125, 153, 155, 172, 197-200; shown at [`FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs:1014`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs#L1014), 1027

**Problem:** Since [S5](020_Work-Plan.md#s5) the Map reads GeoJSON as UTF-8 only, as the GeoJSON standard says (Nik, on purpose). But the message doesn't say so. A UTF-16 file (Notepad's "Unicode", Windows PowerShell 5's `>`) fails with "Not valid JSON"; an ANSI file with a non-ASCII character ("Café") fails with "Not laid out as GeoJSON". Both loaded before. The doc says the file is parsed "as it streams in"; `JsonDocument.Parse(Stream)` reads it all first (it still saves the string copy, which is the point).

**Proposed:** Keep UTF-8 only, and say so: on a UTF-16 or UTF-32 byte-order mark, or a zero second byte, throw `FormatException("Not UTF-8 text (saved as UTF-16) - save it as UTF-8")`; catch `InvalidOperationException` whose inner exception is a `DecoderFallbackException` as "Not UTF-8 text - save it as UTF-8". Keep "Not laid out as GeoJSON" for anything else. Doc: "Reads the file straight from its UTF-8 bytes, without decoding it to a string first: a run's output can be tens of MB." Tests in `GeojsonFileReaderTests`: a Latin-1 file with "Caf\xE9" and a file written with `Encoding.Unicode` each fail with the UTF-8 message.

<a id="n1-24"></a>

### N1-24 · Bug · Low · If the setup window crashes mid-install, msiexec can wait for it forever

**Status:** Open; found in the review of the S2 to S5 code at `4893e32` · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Installer.UI/SetupUI.cs:276-282`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Installer.UI/SetupUI.cs#L276-L282), [`FeBuddy/FeBuddy.Installer.UI/SetupUI.cs:314-332`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Installer.UI/SetupUI.cs#L314-L332) (`Ask`), 195-199 (FatalExit), 238-247 (`Shutdown`); the remarks at [`FeBuddy/FeBuddy.Installer.UI/SetupUI.cs:39-40`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Installer.UI/SetupUI.cs#L39-L40)

**Problem:** When an exception escapes the window thread's `app.Run` (for example from a progress update posted with `BeginInvoke`), it is caught, but the window's dispatcher is never shut down. A call msiexec's thread has already queued with `Dispatcher.Invoke` (a question, FatalExit, `Shutdown`) then never runs or returns. The install never finishes, Windows Installer's lock stays held, and every other install says another installation is in progress until msiexec is killed in Task Manager. The remarks say the install goes on without the window.

**Proposed:** In the catch at 276-282, after setting `_windowFailed`, call `Dispatcher.CurrentDispatcher.InvokeShutdown()` (in a try/catch): queued and later `Invoke`s then abort at once. In `Ask`, FatalExit and `Shutdown`, catch `OperationCanceledException` and answer `MessageResult.None` (skip, in `Shutdown`). Test: move the window-thread call into a helper linked into FeBuddy.UnitTests the way `SetupOutcomeRules` is; an STA thread whose loop dies; the helper returns None within 5 seconds. Otherwise check it on a VM with a debug build that throws on purpose.

<a id="n1-25"></a>

### N1-25 · Bug · Low · The update watcher can crash FE-Buddy if the install folder disappears mid-check, and leaks a watch if it can't start

**Status:** Open; found in the review of the S2 to S5 code at `4893e32` · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs:69`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs#L69) (the timer callback), [`FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs:181-204`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs#L181-L204) (only `ArgumentException` caught; `EnableRaisingEvents` at 200 throws `FileNotFoundException`), [`FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs:73-81`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs#L73-L81), 279; [`FeBuddy/FeBuddy.Wpf/Shell/InstalledUpdate.cs:44-52`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Wpf/Shell/InstalledUpdate.cs#L44-L52)

**Problem:** If the install folder vanishes between creating the watch and turning it on, the exception escapes on a timer thread and FE-Buddy in the other account exits, losing unfinished work. If the parent watch can't be created in the constructor, the folder watch and the timer already made are never disposed.

**Proposed:** In `WatchFolder`, one try around creating and turning on the watch: catch `ArgumentException`, `IOException` and `UnauthorizedAccessException`, dispose it and return. Wrap the timer callback in a try/catch that logs a warning. In the constructor, build the parent watch in a local, and on any exception dispose everything and rethrow. Test: with an internal watcher factory, a factory that throws `FileNotFoundException` once doesn't stop later checks.

<a id="n1-26"></a>

### N1-26 · Bug · Low · After 5 minutes without `FE-BUDDY.dll`, or a second install in one session, the update watcher stops looking

**Status:** Open; found in the review of the S2 to S5 code at `4893e32` · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs:249-261`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs#L249-L261) (252 stops after the limit; 260, the only reset, and 261, the only re-arm, follow a successful read), 31 (`MaxMissingChecks`), 76-79

**Problem:** The new folder is watched only once the dll has been read in it; until then only polling finds it, and polling stops for good after 100 misses (5 minutes). A stalled install, or an uninstall followed later by an install, is never seen: no restart is offered and runs aren't blocked, which is [#379](https://github.com/Nikolai558/FE-BUDDY/issues/379) again.

**Proposed:** In the parent watch's Created handler, call `WatchFolder()`, reset `_missingChecks` and schedule a check, so the new dll's own events start the check and polling is only a fallback. Test: `a_folder_recreated_after_the_missing_checks_ran_out_is_still_followed` (an internal `maxMissingChecks` of 3).

<a id="n1-27"></a>

### N1-27 · Bug · Low · The update watcher logs "changed on disk" when nothing changed, and reads the file three times

**Status:** Open; found in the review of the S2 to S5 code at `4893e32` · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs:249`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs#L249), 262, 264, [`FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs:156-157`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Core/Infrastructure/Platform/InstalledVersionWatcher.cs#L156-L157)

**Problem:** Every check that finds the file logs "FE-BUDDY.dll changed on disk: vX there, vY running", even when X is Y (a repair, a rollback). The file is read three times per check, so the logged version can differ from the one reported, and if it vanishes between reads the check neither raises nor tries again.

**Proposed:** Read once, compare that, log "now vX on disk, vY running" or "written again, still vY", and raise with that version. Test (optional): same version written, no "changed on disk" line.

<a id="n1-28"></a>

### N1-28 · Bug · Low · `Get-InstallerVersion.ps1` writes the counter with CRLF, but the repository stores it with LF

**Status:** Open; found in the review of the S2 to S5 code at `4893e32` · **Session:** [S14](020_Work-Plan.md#s14)

**Where:** [`FeBuddy/FeBuddy.Installer/Get-InstallerVersion.ps1:81-88`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.Installer/Get-InstallerVersion.ps1#L81-L88), 50-53; [`FeBuddy/FeBuddy.UnitTests/Repository/InstallerVersionScriptTests.cs:5-9`](https://github.com/Nikolai558/FE-BUDDY/blob/4893e322252685be1754b22451a6da3f2dd4e76d/FeBuddy/FeBuddy.UnitTests/Repository/InstallerVersionScriptTests.cs#L5-L9), 27-28, 64, 67, 76-83

**Problem:** The repository has no `.gitattributes` and stores `installer-version-counter.json` with LF (`git ls-files --eol`: `i/lf w/lf`). With `core.autocrlf=false` the checkout is LF and the script writes CRLF, so a build shows every line changed and a commit would store CRLF. The comment, the test's summary and [O1-23](#o1-23)'s Status say it is "laid out as the tracked file is", which is true only of a CRLF checkout. CI is unaffected (`Push-InstallerCounter.ps1` converts to LF).

**Proposed:** Write the line ending the file already has (`\r\n` if the text read at 52 has one, otherwise `\n`; a new file gets `\n`). Reword 81-83, the test summary and O1-23's Status. Test: LF in gives LF out; CRLF in gives CRLF out; no file gives LF.

## Reported more than once

Each review slice was read by its own reviewer, so some findings were reported twice. These IDs point at the finding that is kept.

- <a id="d1b-21"></a>**D1b-21** · Under Windows PowerShell (build.cmd) the counter file gets a BOM, and the encoding test then fails · same as [O1-23](#o1-23).
