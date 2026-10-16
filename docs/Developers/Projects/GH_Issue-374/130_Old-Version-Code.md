# 130 · Code that only exists for older FE-Buddy versions

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

Code that only exists for older FE-Buddy versions. [D1](010_Decisions.md#d1) proposes removing what only 3.0 alpha/beta testers need, and keeping everything 2.x users need to upgrade. Session [S6](020_Work-Plan.md#s6).

**17 findings** (5 Med, 12 Low). Quote the ID when you comment on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374).

## Summary

| ID | Kind | Sev | Finding | Status | Session |
|---|---|---|---|---|---|
| [C3-1](#c3-1) | Old-version code | Med | The old single `vatsim_radar_airlines.json` (3.0.0-beta.4 and earlier): full inventory, remove it | Open | S6 |
| [C6-2](#c6-2) | Old-version code | Med | beta.1-beta.3's single `%APPDATA%\FE-Buddy\UserConfig.json` moved in as the Default profile | Open | S6 |
| [C6-3](#c6-3) | Old-version code | Med | AdoptSharedSettings only ever meets a beta.2/beta.3 file | Open | S6 |
| [C6-1](#c6-1) | Old-version code | Med | Settings layout 1 -> 2 step (the only UserConfigMigration step): beta.2-beta.5 settings | Open | S6 |
| [T1a-20](#t1a-20) | Old-version code | Med | Tests for settings blocks written before the Area setting (3.0.0-beta.5 and earlier) | Open | S6 |
| [C4-12](#c4-12) | Old-version code | Low | Parser still reads IncludeRoiAirports for a block without Area (3.0.0-beta.5 and earlier) | Open | S6 |
| [C1-16](#c1-16) | Old-version code | Low | 2.x upgrade steps at launch - keep for the public release | Open | S6 |
| [C1-17](#c1-17) | Old-version code | Low | REINSTALLMODE=amus on the update command line only matters for going back to a 2.x MSI - keep | Open | S6 |
| [T3-30](#t3-30) | Old-version code | Low | A tab with no saved area works it out from pre-Area settings | Open | S6 |
| [C6-5](#c6-5) | Old-version code | Low | TempWorkspace deletes the crash report 3.0.0-alpha.1/alpha.2 left in %TEMP% | Open | S6 |
| [C7-1](#c7-1) | Old-version code | Low | `FeBuddy.Core/News.md` stub is read only by 3.0.0-alpha.1. Keep it until 3.0.0 ships, then delete it | Open | S6 |
| [O1-24](#o1-24) | Old-version code | Low | Installer window falls back to the 32-bit registry view no MSI ever wrote | Open | S6 |
| [T1b-6](#t1b-6) | Old-version code | Low | CombinedAliasFileWriterTests cover a combined file uploaded from 3.0.0-alpha.2 or earlier | Open | S6 |
| [T3-31](#t3-31) | Old-version code | Low | Saved keys keep beta names (CombineAliasFiles, ConcatenateAliases, Procedures) | Open | S6 |
| [W1-20](#w1-20) | Old-version code | Low | AddCustomAliasFiles is saved under its 3.0.0-beta.2-to-beta.6 name, and the comment narrates that history | Open | S6 |
| [W1-18](#w1-18) | Old-version code | Low | Saved full procedure names are converted to root names (3.0.0-beta.8 and earlier) | Open | S6 |
| [T2-7](#t2-7) | Old-version code | Low | Credential entry with the old `useForFeBuddyGitHub` marker | Open | S6 |

## Med

<a id="c3-1"></a>

### C3-1 · Old-version code · Med · The old single `vatsim_radar_airlines.json` (3.0.0-beta.4 and earlier): full inventory, remove it

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Telephony/VatsimRadarVirtualAirlines.cs:125-162`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/VatsimRadarVirtualAirlines.cs#L125-L162) ; [`FeBuddy/FeBuddy.Core/Application/Airac/Telephony/Models/VatsimRadarSelection.cs:19-20`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Telephony/Models/VatsimRadarSelection.cs#L19-L20) ; [`FeBuddy/FeBuddy.Core/Infrastructure/Telephony/TelephonyFiles.cs:15-16`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Telephony/TelephonyFiles.cs#L15-L16),45-50,90-91 ; [`FeBuddy/FeBuddy.Core/Infrastructure/Telephony/TelephonyDownloader.cs:85`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Telephony/TelephonyDownloader.cs#L85),93,101-104,110,134-162 ; [`FeBuddy/FeBuddy.Core/Infrastructure/Telephony/Models/VirtualAirlineListRefreshResult.cs:5-24`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Telephony/Models/VirtualAirlineListRefreshResult.cs#L5-L24) ; [`FeBuddy/FeBuddy.Core/Infrastructure/Telephony/Parsers/VatsimRadarAirlineParser.cs:19-29`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Telephony/Parsers/VatsimRadarAirlineParser.cs#L19-L29),58-81 ; [`FeBuddy/FeBuddy.Core/Application/Airac/AiracSharedDataLoader.cs:183`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/AiracSharedDataLoader.cs#L183),200,216-230 ; [`FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs:266-267`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs#L266-L267),300,312-315,821 ; [`docs/Developers/Architecture.md:116-118`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/Architecture.md?plain=1#L116-L118)

**Problem:** Code that only exists so a tester who last ran &lt;= 3.0.0-beta.4 (beta.4's merged object from data.vatsim-radar.com, or beta.3's GitHub array) can still use that file while neither new part (GNG + VATSIM-Radar, introduced in beta.5, ChangeLog.md:225) has a copy, and deletes it once both do. Every piece:
  - Core: `TelephonyFiles.OlderVatsimRadarAirlinesFileName`/`OlderVatsimRadarAirlinesFilePath`; `TelephonyDownloader.RefreshVirtualAirlineListAsync` passing it, `RefreshVirtualAirlineListFromUrlsAsync`'s `olderCopyPath` parameter + remarks + the two return branches, `DeleteOlderCopy`; `VirtualAirlineListRefreshResult.OlderCopyPath`, `OlderCopyDownloadedUtc` and the `|| OlderCopyPath is not null` in `HasCopy` (and its summary's "and - only while ... - the older single copy"); `VatsimRadarAirlineParser.Parse`'s object-with-`virtual`-array branch, `VirtualAirlinesProperty`, the remarks paragraph and the separate `Parse` method itself (kept copies are only ever swapped in after `ParseGitHubList` accepted them, so `AiracSharedDataLoader.cs:208` and `VatsimRadarVirtualAirlines.cs:145` can call `ParseGitHubList`); `AiracSharedDataLoader.AddVirtualAirlineListAsync`'s older-copy branch (lines 216-230), the `WithoutPart` middle case (line 200) and the summary sentence (line 183); `VatsimRadarVirtualAirlines.ReadKeptCopy()`'s third argument, `ReadKeptCopy(gng, vatsimRadar, olderCopyPath)`'s parameter, branch (159-161) and docs (136, 140); `VatsimRadarCopy.IsOlderCopy`.
  - Wpf: `TelephonyViewModel.VatsimRadarListStatus`'s `{ IsOlderCopy: true }` case (266-267), `VatsimRadarListWarning`'s `IsOlderCopy` checks (300, 312-315), `result.OlderCopyPath` (821).
  - Tests: `TelephonyViewModelTests.cs:956,965-966`; `VatsimRadarAirlineParserTests.cs:12-13,56-58 (beta.4 InlineData),73-155` (the `Parse` tests: beta.4 object, beta.3 array, no-array cases); `TelephonyDownloaderTests.cs:269,291,304-337,373-400`; `AiracSharedDataLoaderTests.cs:18,201-215 (OlderCopyJson),314-336,357-371`; `VatsimRadarVirtualAirlinesTests.cs:14,190,193-220,226`.
  - Docs: Architecture.md:116-118 ("The single `vatsim_radar_airlines.json` that 3.0.0-beta.4 and earlier kept ... is read only while neither part has a copy, and deleted once both have one.").

**Proposed:** Delete all of the above (merge `Parse` into `ParseGitHubList`, drop `olderCopyPath`/`OlderCopy*`/`IsOlderCopy` and their tests, drop Architecture.md:116-118). KEEP the reason for the two-part design but drop the version: TelephonyFiles.cs:15-16 -> "(VATSIM-Radar's merged list at `data.vatsim-radar.com` only takes TLS 1.3, which Windows 10 can't do - bug [#338](https://github.com/Nikolai558/FE-BUDDY/issues/338).)" (rule-3 exception: explains the current approach via a found bug).

**Future use:** unlikely. What breaks if removed: a tester still on &lt;= beta.4 whose first run of 3.0 can't download either part runs without the list (Telephony warns and continues, the list is optional) instead of using the stale merged copy, and the stale `%APPDATA%\FE-Buddy\Telephony\vatsim_radar_airlines.json` stays on disk (harmless). Anyone who has run beta.5-beta.12 online with the list included already had it deleted.

**Note:** Also found as [C1-14](#c1-14), [C7-22](#c7-22), [W1-19](#w1-19), [T1b-4](#t1b-4), [T1b-5](#t1b-5), [T2-6](#t2-6). Proposed for removal under [D1](010_Decisions.md#d1).

<a id="c6-2"></a>

### C6-2 · Old-version code · Med · beta.1-beta.3's single `%APPDATA%\FE-Buddy\UserConfig.json` moved in as the Default profile

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs:34-35`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.Profiles.cs#L34-L35), 40-42, 340-377; [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:46-49`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L46-L49), 56, 59-60, 157; [`FeBuddy/FeBuddy.Core/Infrastructure/FileSystem/AppDataReset.cs:83-85`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/FileSystem/AppDataReset.cs#L83-L85); [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigTransfer.cs:446-455`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigTransfer.cs#L446-L455)

**Problem:** This handles the one-file layout of the alphas through beta.3: `UserConfig.json`, `UserConfig.previous.json` and `UserConfig.before-import.json` in `%APPDATA%\FE-Buddy` (`git show 3.0.0-beta.3:...UserConfigFile.cs`). Profiles and `Shared.json` arrived in beta.4 (27f6777/d25bcac, first tag 3.0.0-beta.4). Any launch of beta.4 or later has already moved the file, so only testers whose last launch was beta.2 or beta.3 still have it. beta.1 and alpha files would be moved in too and treated as layout 1, but beta.2's notes told those testers to uninstall.
  If it is removed, such a tester's settings are ignored and FE-Buddy behaves as a first launch:
  - every setting is at its default;
  - `HasSettings` is false, so the Reset/Uninstall windows say there are no settings;
  - the old file stays in `%APPDATA%\FE-Buddy` until a reset that doesn't keep settings deletes it.
  They can still bring it in by hand with Settings > Import..., because the plain-file import path doesn't depend on this code, as long as layout 1 is still accepted ([C6-1](#c6-1) (b)). The pieces to delete together:
  - `LegacyConfigFileName`, `MoveLegacyFileIn` and its call in `ReadAll` (line 157);
  - `LegacyBackupName`;
  - the legacy clause of `HasSettings`;
  - the `LegacyConfigFileName` entry in AppDataReset's kept-settings list (line 85, and its comment at 83-84);
  - the legacy clause of `UserConfigTransfer.IsOwnSettingsFile`;
  - the class-remarks paragraph (UserConfigFile.cs:46-49) and ", and the one an older FE-Buddy's settings are moved into" (line 56).
  `MoveQuietly` stays, because `RenameProfile` uses it.

**Proposed:** Keep, or delete the pieces above plus tests `UserConfigProfilesTests.an_older_fe_buddys_settings_file_is_moved_in_as_the_default_profile`, `an_older_file_that_cannot_be_moved_is_tried_again_next_time` and `an_older_file_is_left_alone_once_there_are_profiles`, and Settings-Reference.md:80-82. Delete it together with [C6-3](#c6-3).

**Future use:** unlikely.

**Note:** Also found as [T2-2](#t2-2). Must go together with [C6-3](#c6-3). Proposed for removal under [D1](010_Decisions.md#d1).

<a id="c6-3"></a>

### C6-3 · Old-version code · Med · AdoptSharedSettings only ever meets a beta.2/beta.3 file

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:181-184`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L181-L184), 723-751

**Problem:** Since beta.4, every write strips shared keys from a profile (`ProfileValues` in Write, ReplaceAll and BringForward; `RemoveSharedSettings` in Save and Undo), and `CreateProfile` filters them out. So only a file from before `Shared.json` holds them, which means beta.2 or beta.3 (moved in by [C6-2](#c6-2), or hand-copied). In beta.3 those keys were `General.UpdateChannel`, `NewsLastOpen`, `FeBuddyGitHub.CredentialId` and `LegacyGitHubTokenNoticeShown` (`git show 3.0.0-beta.3:...UserConfigPortability.cs`).
  If it is removed while [C6-2](#c6-2) stays, the moved-in file's shared settings are lost: the first write of the profile drops them (immediately at launch, when BringForward rewrites the file), and they never reach `Shared.json`. At the next launch:
  - `UpdateChannelSetting.SaveDefaultIfUnset` saves the build's channel, so an Alpha-channel tester silently moves to Beta;
  - every News post shows unread;
  - FE-Buddy's GitHub-token choice is gone;
  - the 2.x token notice shows again.
  [C6-2](#c6-2) and [C6-3](#c6-3) must go together.

**Proposed:** Delete with [C6-2](#c6-2): `AdoptSharedSettings`, its call and comment at 181-183, test `UserConfigProfilesTests.a_profiles_copy_of_a_shared_setting_moves_to_shared_json_unless_it_has_one`, and the sentence "A shared key found in a profile (copied in by hand, say) moves to `Shared.json`, unless that has one already." at Settings-Reference.md:69-70.

**Future use:** unlikely.

**Note:** Must go together with [C6-2](#c6-2). Proposed for removal under [D1](010_Decisions.md#d1).

<a id="c6-1"></a>

### C6-1 · Old-version code · Med · Settings layout 1 -> 2 step (the only UserConfigMigration step): beta.2-beta.5 settings

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigrations.cs:30-33`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigMigrations.cs#L30-L33); [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigAreas.cs:9-15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigAreas.cs#L9-L15), 93-96, 101-120; [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigVersion.cs:16-18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigVersion.cs#L16-L18), 31, 34; [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs:838-855`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigFile.cs#L838-L855) (TakeVersion), 864-909 (BringForward); [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigTransfer.cs:24-25`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigTransfer.cs#L24-L25), 240-242

**Problem:** Full inventory of settings migrations in C6 (none of it is for 2.x: 2.9.3 kept no `UserConfig.json`, `git grep UserConfig.json 2.9.3` finds nothing, so every item is 3.0-pre-release only):
  1. **Step to layout 2** (this finding): `UserConfigMigrations.All` has exactly one step. It upgrades layout 1 to layout 2. For each of the 9 area tabs (`UserConfigAreas.Tabs`) that has any saved key and no `Area`, it saves `Area` = `DefaultFor(tab)`. Then it removes `Services.AiracService.Procedures.IncludeRoiAirports` and the whole `Services.AiracService.Telephony.Roi` node. Layout 1 was written by 3.0.0-beta.2 and beta.3 (unstamped `%APPDATA%\FE-Buddy\UserConfig.json`) and by beta.4 and beta.5 (`User Configurations\UserConfig.<P>.json` stamped `"ConfigVersion": 1`; `git show 3.0.0-beta.4|beta.5:...UserConfigVersion.cs` gives `Current = 1`). Layout 2 starts at beta.6 (commit 113218e; `git show 3.0.0-beta.6` gives `Current = 2`). Also covered: exports from those betas (`configVersion` absent or 1). Anyone who has launched beta.6 to beta.12 already has a layout-2 file, with the old file kept as `UserConfig-v1.<Profile>.json`. So only testers whose last launch was beta.2 to beta.5 still need it.
  2. beta.2/beta.3 single `UserConfig.json` moved in as the Default profile: [C6-2](#c6-2).
  3. Shared settings found in a profile moved to `Shared.json`: [C6-3](#c6-3).
  4. Unstamped file or export = layout 1 (`TakeVersion`, `UserConfigTransfer.cs:240-242`). Only beta.2/beta.3 wrote files or exports without a stamp. This goes with item 1.
  5. Pre-profile backup names, including the never-written `v\d+`: [C6-4](140_Unused-Code.md#c6-4).
  6. alpha.1/alpha.2 crash report in `%TEMP%`: [C6-5](#c6-5).
  What happens to a beta.2-beta.5 tester if the step goes:
  (a) Removing only the `All` entry, with Current 2 and Oldest 1: `Migrate` throws "No step brings settings to layout 2". `BringForward` then logs a warning at every launch, never rewrites the file and keeps saving it with stamp 1 (`_stampVersion = version`, line 874). Importing any beta.2-beta.5 export or plain file is refused (`UserConfigTransfer.Package` turns the error into `UserConfigTransferException`). Not viable.
  (b) Removing the step and setting `Oldest = 2`: a stamped-1 file hits TakeVersion's misleading warning "has a settings layout stamp FE-Buddy can't read (1)" (line 847 needs `version >= Oldest`) and is read as layout 2. Unstamped files are read as layout 2 silently. Each area tab with no saved `Area` still opens on the same area, because `GeojsonSubServiceViewModel.SavedArea` (Wpf, line 946) falls back to `UserConfigAreas.DefaultFor` at load. The differences are small:
  - Procedure Charts that relied on `IncludeRoiAirports = Y` with no facilities opens on None instead of ROI, once DefaultFor's `IncludeRoiAirports` branch (lines 93-96, only meaningful for layout-1 data; no current code saves that key) is also removed.
  - The stale keys `Procedures.IncludeRoiAirports` and `Telephony.Roi.*` stay in the profile for good. No tab removes them: Save writes every in-memory key under the node, and Telephony has no `Areas`, so it never touches `Roi.*`. They are harmless.
  ChangeLog beta.4 (Feature [#324](https://github.com/Nikolai558/FE-BUDDY/issues/324)) promised testers that "settings saved by beta.2 or later are kept".

**Proposed:** Owner's choice. Keeping costs about 25 lines of Core plus a fixture. To remove:
  - Code: delete the `All` entry, `UserConfigAreas.SaveForEveryTab`, UserConfigAreas remarks para 1 ("Before layout 2...") and the `IncludeRoiAirports` branch of `DefaultFor`. Set `UserConfigVersion.Oldest = 2`. Change `TakeVersion` so a readable stamp below `Oldest` is taken as `Oldest` without the "can't read" warning.
  - Tests: delete `FeBuddy.UnitTests/Fixtures/UserConfig/UserConfig.v1.json` and its fingerprint in `UserConfigLayoutTests.SampleFingerprints`. Delete `UserConfigAreasTests.layout_2_saves_the_area_for_every_tab_with_settings_and_drops_what_it_replaces` and the `IncludeRoiAirports` cases in `procedures_starts_on_its_facilities_then_the_roi_it_brought_in_then_none`.
  - Docs: rewrite UserConfigVersion remarks. In Settings-Reference.md, drop the layout-1 row (662), the `IncludeRoiAirports` mention (203) and the beta.2/beta.3 sentence (54).
  - Release notes: say settings from beta.5 or earlier are not carried over. `ProcedureSettingsParser.ReadArea`'s own `IncludeRoiAirports` fallback for blocks without `Area` is covered by [C4-12](#c4-12).

**Future use:** unlikely (the step itself). The framework (UserConfigVersion, Migrations, Context, BringForward) is needed for post-3.0 layout changes and should stay.

**Note:** Also found as [T2-1](#t2-1), [T3-29](#t3-29). Proposed for removal under [D1](010_Decisions.md#d1) (keep the migration framework).

<a id="t1a-20"></a>

### T1a-20 · Old-version code · Med · Tests for settings blocks written before the Area setting (3.0.0-beta.5 and earlier)

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/ArrivalSettingsParserTests.cs:217-228`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Arrivals/ArrivalSettingsParserTests.cs#L217-L228); [`FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureSettingsParserTests.cs:9-10`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/Procedures/ProcedureSettingsParserTests.cs#L9-L10), 271-295

**Problem:** `Area` arrived in 3.0.0-beta.6 (ChangeLog.md:205, Feature [#335](https://github.com/Nikolai558/FE-BUDDY/issues/335)), and config migration v2 saves an Area for every tab and drops `IncludeRoiAirports` (Infrastructure/Configuration/UserConfigMigrations.cs:32). Every block the GUI builds sends `Area` (Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs:729), and so does every Harness block with an area (FeBuddy.Harness/HarnessSettings.cs:68,142,194,257,326,372,420,458,503). The no-`Area` fallback is therefore reached only by these tests. Production code it covers: `SubServiceSettingsReader.ReadArea`'s `text is null` branch (Application/Settings/SubServiceSettingsReader.cs:66-73, whose doc at 48-52 wrongly says the harness relies on it), `ProcedureSettingsParser.ReadArea`'s no-Area branch (ProcedureSettingsParser.cs:184-195), and `"IncludeRoiAirports"` in its OwnKeys (48, remark at 42). Nothing in the app breaks if they go.

**Proposed:** Delete `a_block_without_an_area_that_sets_both_uses_its_artccs`, `a_block_without_an_area_takes_include_roi_airports_as_the_roi` and `a_block_without_an_area_and_only_filter_by_roi_has_no_area`, plus "(and what a block without one means)" in the ProcedureSettingsParserTests summary, together with that production code. Then add `"Area"` to the tests that rely on the fallback today:
  - `Area=Roi`: AirwayServiceTests (SouthEastRoi/FarAwayRoi, settings_reject_a_malformed_roi...); AirwaySettingsParserTests roi_filtering_*; Arrival/DepartureSettingsParserTests `WithRoi` (roi_filtering_*, an_invalid_region_of_interest_is_rejected).
  - `Area=Artccs`: Arrival/DepartureSettingsParserTests `the_artcc_filter_is_trimmed_and_upper_cased`; Arrival/DepartureServiceTests `run_whose_filters_leave_nothing_says_why_nothing_was_written`; ProcedureSettingsParserTests `MinimalValidSettings` (Facilities=ZOB); ProcedureServiceTests/ProcedureServiceFixInFacilityTests `Settings()`; ProceduresJsonWriterTests `the_settings_parsers_default_field_set...`.
  - `Area=None`: ProcedureSettingsParserTests `no_area_and_nothing_listed_throws`, `*_alone_satisfies_the_inclusion_source_requirement`, `alias_only_settings_with_no_inclusion_source_at_all_parses_fine`.

**Future use:** unlikely.

**Note:** Goes with [C6-1](#c6-1), [C4-12](#c4-12) and [T1b-21](140_Unused-Code.md#t1b-21).

## Low

<a id="c4-12"></a>

### C4-12 · Old-version code · Low · Parser still reads IncludeRoiAirports for a block without Area (3.0.0-beta.5 and earlier)

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureSettingsParser.cs:42`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Airac/Procedures/ProcedureSettingsParser.cs#L42), 48, 184-195

**Problem:** Settings migration v2 (3.0.0-beta.6, UserConfigMigrations.cs:32) dropped the saved `IncludeRoiAirports`. The tab always sends `Area` (GeojsonSubServiceViewModel.cs:729), and so does the current Harness block (HarnessSettings.cs:503). Only a hand-built pre-beta.6 block reaches this branch. The doc also narrates history ("before there was a choice", "it was sent").

**Proposed:** drop the `IncludeRoiAirports` branch and the `OwnKeys` entry. Either keep a plain fallback ("A block without `Area` takes the facilities when `Facilities` lists any, otherwise None") or call `SubServiceSettingsReader.ReadArea(procedureSettings, Areas, artccListKey: "Facilities", withNoFilter: SubServiceArea.None)`. Delete the tests `a_block_without_an_area_takes_include_roi_airports_as_the_roi` and `..._only_filter_by_roi_has_no_area` (ProcedureSettingsParserTests.cs:271-293), and the "old `IncludeRoiAirports`" text in Settings-Reference.md:203 and 462-463. If removed, an old hand-built block with `IncludeRoiAirports=Y` and no `Area` would run as `None`, and with no list it would throw the "covers nothing" error.

**Future use:** unlikely; settings blocks aren't persisted.

**Note:** Goes with [C6-1](#c6-1) and [T1a-20](#t1a-20). Proposed for removal under [D1](010_Decisions.md#d1).

<a id="c1-16"></a>

### C1-16 · Old-version code · Low · 2.x upgrade steps at launch - keep for the public release

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Launch/LaunchSequence.cs:122-157`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Launch/LaunchSequence.cs#L122-L157); [`LegacyGitHubTokenNotice.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Launch/LegacyGitHubTokenNotice.cs) (whole); [`Models/LaunchProgress.cs:18-34`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Launch/Models/LaunchProgress.cs#L18-L34); [`Models/OptionalLaunchStep.cs:39-46`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Launch/Models/OptionalLaunchStep.cs#L39-L46); [`OptionalLaunchSteps.cs:21-23`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Launch/OptionalLaunchSteps.cs#L21-L23), 38-40, 58-60

**Problem:** Code that exists only for FE-Buddy 2.x installs: the FEBUDDY_GITHUB_TOKEN notice, the Squirrel uninstall and the dead-shortcut cleanup. Removing it would leave 2.x users who skipped 2.9.x's MSI with two FE-Buddys installed, dead shortcuts, and a plain-text token in an environment variable nobody tells them about. Comments are accurate. Note: `WelcomeNotice` is *not* 2.x compat - it is the first-run welcome for every new FE-Buddy 3 user (WelcomeWindow.xaml:30-33) and should stay as normal code.

**Proposed:** None - keep. (Only the WelcomeNotice issue number, see [C1-25](200_Comments-Core.md#c1-25).)

**Note:** Also found as [C7-26](#c7-26), [O1-25](#o1-25), [T1c-4](#t1c-4), [T1c-5](#t1c-5), [T2-3](#t2-3), [T2-4](#t2-4), [W3-24](#w3-24). Proposed: **keep** - 3.0 is when most 2.x users upgrade.

<a id="c1-17"></a>

### C1-17 · Old-version code · Low · REINSTALLMODE=amus on the update command line only matters for going back to a 2.x MSI - keep

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Application/Updates/UpdateInstaller.cs:114-126`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Updates/UpdateInstaller.cs#L114-L126)

**Problem:** Per VERSIONING.md:102-105, 3.x MSIs set `REINSTALLMODE=amus` themselves; the command-line copy exists for a 3.0 pre-release user on the Stable channel going back to 2.9.x. Once 3.0.0 is published, Stable's latest is 3.x, so the update window can no longer offer a 2.x MSI. Comment is accurate.

**Proposed:** Keep (harmless belt-and-braces). After 3.0.0 is out, optionally shorten the clause to "makes the MSI copy every file, so going back to an older release (the rollback the version policy allows) does not leave files missing."

**Note:** Proposed: **keep**. Re-checked after [#377](https://github.com/Nikolai558/FE-BUDDY/pull/377): still applies (the lines moved down 8).

<a id="t3-30"></a>

### T3-30 · Old-version code · Low · A tab with no saved area works it out from pre-Area settings

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy.Core/Infrastructure/Configuration/UserConfigAreas.cs:67-115`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigAreas.cs#L67-L115); [`FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs:937-956`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs#L937-L956); tests [`AreaTests.cs:156-173`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/AreaTests.cs#L156-L173), [`WhatYoullGetTests.cs:209-228`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/WhatYoullGetTests.cs#L209-L228), 304-313

**Problem:** `DefaultFor` serves both the layout-2 migration step (3.0.0-beta.6) and, at runtime, any tab with no `Area` saved. The facility/ARTCC branch is still current behaviour for a new user, but the Procedures `IncludeRoiAirports` branch can only fire for an unmigrated file (the layout-2 step drops that key), so at runtime it is dead. The tests a_tab_never_given_an_area_starts_on_the_one_its_settings_mean, a_saved_area_the_tab_doesnt_offer_is_worked_out_instead, arrivals_lists_its_artccs_as_inclusive_with_the_amendment and arrivals_with_a_region_by_airport_keeps_those_for_an_airport_inside_it cover this path.

**Proposed:** Keep `DefaultFor`; consider moving the `IncludeRoiAirports` branch into the migration step only (SaveForEveryTab) so the runtime fallback reads only current keys.

**Future use:** the runtime fallback is likely needed; the IncludeRoiAirports branch is not.

**Note:** Goes with [D1](010_Decisions.md#d1) ([C6-1](#c6-1), [T1b-21](140_Unused-Code.md#t1b-21)).

<a id="c6-5"></a>

### C6-5 · Old-version code · Low · TempWorkspace deletes the crash report 3.0.0-alpha.1/alpha.2 left in %TEMP%

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/FileSystem/TempWorkspace.cs:12-13`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/FileSystem/TempWorkspace.cs#L12-L13), 30-36, 49-53, 59-71

**Problem:** The crash report moved to `%APPDATA%\FE-Buddy\Logs` in alpha.3 (commit 4fc8778, first tag 3.0.0-alpha.3; ChangeLog.md:606-608). Only alpha.1 or alpha.2, and only after a crash, wrote `%TEMP%\febuddy-wpf-crash.txt`. If this is removed, such a file stays in `%TEMP%` (a few KB, which Windows' own temp clean-up removes); nothing else changes. Also, ClearOnLaunch's `<returns>` ("top-level entries that could not be removed") is not quite right, because this file's failure is counted too.

**Proposed:** Delete `LegacyCrashReportPath`, lines 59-71, the remarks clause ", along with the crash report 3.0.0 alphas left beside it (&lt;see cref="LegacyCrashReportPath"/>)", and " and deletes &lt;see cref="LegacyCrashReportPath"/>" from the ClearOnLaunch summary. Delete tests `TempWorkspaceTests.temp_workspace_clear_on_launch_deletes_the_legacy_crash_report`, `..._counts_a_legacy_crash_report_it_cannot_delete`, and the `LegacyCrashReportPath` assert at TempWorkspaceTests.cs:116.

**Future use:** unlikely.

**Note:** Also found as [T2-5](#t2-5). Proposed for removal under [D1](010_Decisions.md#d1).

<a id="c7-1"></a>

### C7-1 · Old-version code · Low · `FeBuddy.Core/News.md` stub is read only by 3.0.0-alpha.1. Keep it until 3.0.0 ships, then delete it

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Core/News.md:1-27`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/News.md?plain=1#L1-L27); [`FeBuddy/FeBuddy.Core/FeBuddy.Core.csproj:14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/FeBuddy.Core.csproj#L14); [`FeBuddy/FeBuddy.Core/Application/News/NewsService.cs:27-31`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/News/NewsService.cs#L27-L31); [`docs/Developers/README.md:175-177`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Developers/README.md?plain=1#L175-L177)

**Problem:** Not a defect. These are the facts the decision needs:
  (a) Only 3.0.0-alpha.1 reads this path. At tag 3.0.0-alpha.1, `NewsService.NewsPath = "FeBuddy/FeBuddy.Core/News.md"`, read from `raw.githubusercontent.com/Nikolai558/FE-BUDDY/v3-development/...` and from the Contents API on the same branch. From alpha.2 on (commit 5b0c397, 2026-09-28), every build reads the root `News.md`. FE-Buddy 2.x never read a News file: 2.9.3 only opens `github.com/.../wiki#news` in a browser.
  (b) alpha.1 checks the Stable channel unless Alpha was saved (alpha.1's `VersionCheckResult.ParseChannel` defaults to Stable, the same bug #275 that hit alpha.2). Most alpha.1 users are therefore offered nothing until **3.0.0 stable** is out. The stub's promise ("When the next version is out, the version number ... turns into an update button") comes true with the public release.
  (c) The header comment is accurate. The PostIds alpha.1 users last saw were 2026-09-30.1 (commits 3194a95 and cadff9b), and the stub's 2026-09-30.3 is later, so it shows as new.
  (d) If the stub is deleted, alpha.1 gets a 404. Its NewsService then falls back silently to its own bundled copy. Nothing breaks; alpha.1 users just lose the "please update" nudge.
  (e) Side issue: `<EmbeddedResource Include="..\..\News.md" Link="News.md" .../>` gives the linked root file the same project path as the physical stub, which the SDK also includes as a `None` item. Solution Explorer then shows two `FeBuddy.Core\News.md`, one of them the stub, which makes it easy to edit the wrong one. The stub's own "Post there, not here" suggests this has already happened.

**Proposed:** Keep the stub through the 3.0.0 release. Delete it in the first release after 3.0.0 (3.0.1 or 3.1), together with the alpha.1 sentence in NewsService.cs:28-29 and the stub bullet in docs/Developers/README.md:176-177. Meanwhile, change `Link="News.md"` to something distinct, e.g. `Link="News (repository root).md"`. LogicalName already fixes the resource name, so the Link only affects what Solution Explorer shows.

**Future use:** unlikely. Remove after 3.0.0.

**Note:** Also found as [C1-15](#c1-15), [D1b-17](#d1b-17). Proposed for removal under [D1](010_Decisions.md#d1) (Kyle: remove now rather than after 3.0).

<a id="o1-24"></a>

### O1-24 · Old-version code · Low · Installer window falls back to the 32-bit registry view no MSI ever wrote

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Installer.UI/Setup/InstalledFeBuddy.cs:46-53`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Installer.UI/Setup/InstalledFeBuddy.cs#L46-L53)

**Problem:** Every shipped MSI wrote HKLM\Software\FE-BUDDY in the 64-bit view: 2.x's installer was forced to x64 (commit a81bf19, contained in tag 2.9.0, the first MSI), and 3.x's wixproj has `InstallerPlatform x64` since its first commit. The "older build" was at most an unreleased dev build. The package's own RegistrySearch (InstallerState.wxs) reads only the 64-bit view, so the fallback can only make the window and the package disagree (e.g. show "Update" and a folder the package then ignores).

**Proposed:** read only `RegistryView.Registry64` (drop the loop) and reword the summary: `Reads the key from the 64-bit view, where the 64-bit MSI writes it.`

**Future use:** unlikely.

**Note:** Proposed for removal under [D1](010_Decisions.md#d1).

<a id="t1b-6"></a>

### T1b-6 · Old-version code · Low · CombinedAliasFileWriterTests cover a combined file uploaded from 3.0.0-alpha.2 or earlier

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Application/Airac/ConcatenateAliases/CombinedAliasFileWriterTests.cs:13-14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Application/Airac/ConcatenateAliases/CombinedAliasFileWriterTests.cs#L13-L14) (summary "in either layout"), 105-130 (`an_old_combined_alias_file_with_the_fe_buddy_section_last_loses_it`)

**Problem:** The end line was added in a647e00 (first in 3.0.0-alpha.3), so this covers only a facility file last uploaded from alpha.2 or earlier. There is no dedicated production branch (a start line with no end line just drops the rest, CombinedAliasFileWriter.cs:117-138), but the test and its "in either layout" wording are history; the production remark at CombinedAliasFileWriter.cs:41-42 ("A file from before the end line existed ... loses everything from its start line down") narrates the same. The class summary also says "commands in more than one file are reported", but only those involving a custom file are (see `a_command_only_fe_buddy_files_share_is_not_reported`).

**Proposed:** Delete the test; class summary: `... a <c>.FeUseOnly</c> line stays first; an earlier combined file used as a custom file loses its FE-Buddy section; a command a custom file shares with another merged file is reported; and a custom file that could not be read is left out with a warning.`

**Future use:** unlikely; if the owner wants a "start line but no end line" robustness test, rename it `a_custom_file_with_a_start_line_and_no_end_line_loses_everything_from_it` and drop the history from its summary.

**Note:** Proposed for removal under [D1](010_Decisions.md#d1).

<a id="t3-31"></a>

### T3-31 · Old-version code · Low · Saved keys keep beta names (CombineAliasFiles, ConcatenateAliases, Procedures)

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs:65-70`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs#L65-L70); [`FeBuddy.Wpf/ViewModels/AiracSubServices.cs:55-68`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracSubServices.cs#L55-L68); test [`ConcatenateAliasesViewModelTests.cs:115-126`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Wpf/ViewModels/ConcatenateAliasesViewModelTests.cs#L115-L126)

**Problem:** Not removable code - the keys are live - but the names (from 3.0.0-beta.2, commit 9139ae6) only exist for earlier betas' files. Renaming them now needs a UserConfigMigrations step and a new layout; the test pins `Services.AiracService.ConcatenateAliases.CombineAliasFiles`.

**Proposed:** No code change needed; if the owner wants clean names for 3.0, rename with a layout-3 migration step and update the test. Either way reword the docs to say what the key is, not its history ([T3-25](300_Unit-Tests.md#t3-25)).

**Future use:** n/a (live keys).

**Note:** See also [W1-20](#w1-20).

<a id="w1-20"></a>

### W1-20 · Old-version code · Low · AddCustomAliasFiles is saved under its 3.0.0-beta.2-to-beta.6 name, and the comment narrates that history

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs:65-70`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs#L65-L70)

**Problem:** From 3.0.0-beta.2 (9139ae6) to beta.6, `CombineAliasFiles` turned the whole combined file on and off. Since beta.7 (4b4d810) it means "add the custom files". The config key keeps the old name so an earlier "N" carries over, while the settings block uses `AddCustomAliasFiles`. Settings-Reference.md:514 documents the pair. The comment is history narration (rule 3).

**Proposed:** Either keep the key and reword: "Saved as `CombineAliasFiles` (see Settings-Reference), though the settings block calls it `AddCustomAliasFiles`." Or rename the config key with a `UserConfigMigrations` step and update Settings-Reference.md, the UserConfig.v1/v2 fixtures and ConcatenateAliasesViewModelTests.cs:115-124.

**Future use:** n/a.

**Note:** Proposed: keep the key name (no migration) and only reword the comment.

<a id="w1-18"></a>

### W1-18 · Old-version code · Low · Saved full procedure names are converted to root names (3.0.0-beta.8 and earlier)

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs:1143-1149`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs#L1143-L1149), 1782-1814

**Problem:** Root-name picks arrived in 8a4362c, first released in 3.0.0-beta.9. The `.Select(ProcedureNaming.RootName)` in `LoadFromConfig` and the `RootName` in `ParseAirportProcedurePicks` exist only for configs saved by beta.8 and earlier. Without them, an old `BRWNZ FOUR (RNAV)` would show as its own row saying "Cycle … has no departure or arrival named BRWNZ FOUR (RNAV), so it's left out", although Core still includes it (`ProcedureSelection` takes `RootName` of every pick), and the duplicate check would miss it. Both comments also narrate history (rule 3).

**Proposed:** Owner's call. If kept, reword: "Read by root name: 3.0.0-beta.8 and earlier saved the full name (BRWNZ FOUR (RNAV)). The tab isn't marked changed for it." Do the same in `ParseAirportProcedurePicks`' summary ("each by its root name, as 3.0.0-beta.8 and earlier saved the full name …"). If dropped, delete the `RootName` mapping in both places. Core's own `RootName` on picks still covers runs.

**Future use:** unlikely.

**Note:** Also found as [T3-28](#t3-28). Proposed for removal under [D1](010_Decisions.md#d1).

<a id="t2-7"></a>

### T2-7 · Old-version code · Low · Credential entry with the old `useForFeBuddyGitHub` marker

**Status:** Open · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.UnitTests/Infrastructure/Credentials/CredentialStoreTests.cs:262-273`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.UnitTests/Infrastructure/Credentials/CredentialStoreTests.cs#L262-L273)

**Problem:** Guards entries written by an earlier 3.0 pre-release, and narrates that history. No production code exists for it: System.Text.Json ignores the unknown property (CredentialStore.cs:45-49 options, `Payload` at 411-424 has no such member).

**Proposed:** Either delete the test, or keep it as a generic guard: rename `an_entry_with_a_property_this_version_does_not_know_still_reads`, summary "An entry carrying a property this version does not know still reads."

**Future use:** n/a (no production code).

**Note:** Proposed for removal under [D1](010_Decisions.md#d1).

## Reported more than once

Each review slice was read by its own reviewer, so some findings were reported twice. These IDs point at the finding that is kept.

- <a id="c1-14"></a>**C1-14** · The 3.0.0-beta.4 single virtual-airline-list copy fallback (beta-only; removal candidate) · same as [C3-1](#c3-1).
- <a id="c1-15"></a>**C1-15** · NewsService comment and FeBuddy/FeBuddy.Core/News.md exist only for 3.0.0-alpha.1 (removal candidate) · same as [C7-1](#c7-1).
- <a id="c7-22"></a>**C7-22** · Telephony's "older single copy" exists only for 3.0.0-beta.4 and earlier and can go before the public release · same as [C3-1](#c3-1).
- <a id="c7-26"></a>**C7-26** · Platform's 2.x upgrade code: comments are accurate and all three are still needed for 3.0 · same as [C1-16](#c1-16).
- <a id="d1b-17"></a>**D1b-17** · FeBuddy.Core/News.md exists only for 3.0.0-alpha.1 · same as [C7-1](#c7-1).
- <a id="o1-25"></a>**O1-25** · Uninstall cleanup of 2.x leftovers (keep for 3.0) · same as [C1-16](#c1-16).
- <a id="t1b-4"></a>**T1b-4** · AiracSharedDataLoaderTests cover the beta.4-and-earlier single `vatsim_radar_airlines.json` · same as [C3-1](#c3-1).
- <a id="t1b-5"></a>**T1b-5** · VatsimRadarVirtualAirlinesTests cover the beta.4/beta.3 older single copy · same as [C3-1](#c3-1).
- <a id="t1c-4"></a>**T1c-4** · Tests that exist only for the FE-Buddy 2.x GitHub token notice · same as [C1-16](#c1-16).
- <a id="t1c-5"></a>**T1c-5** · Tests that exist only for removing FE-Buddy 2.x's Squirrel install and shortcuts · same as [C1-16](#c1-16).
- <a id="t2-1"></a>**T2-1** · Tests for bringing 3.0.0-beta.2/beta.3 settings (layout 1) up to layout 2 · same as [C6-1](#c6-1).
- <a id="t2-2"></a>**T2-2** · Tests for moving 3.0.0-beta.3's single `UserConfig.json` in as the Default profile · same as [C6-2](#c6-2).
- <a id="t2-3"></a>**T2-3** · Tests for removing FE-Buddy 2.x's Squirrel install and its dead shortcuts · same as [C1-16](#c1-16).
- <a id="t2-4"></a>**T2-4** · Tests for the 2.x `FEBUDDY_GITHUB_TOKEN` notice · same as [C1-16](#c1-16).
- <a id="t2-5"></a>**T2-5** · Tests for deleting the crash report 3.0.0 alphas left in %TEMP% · same as [C6-5](#c6-5).
- <a id="t2-6"></a>**T2-6** · Tests for the single virtual-airline copy 3.0.0-beta.4 and earlier kept · same as [C3-1](#c3-1).
- <a id="t3-28"></a>**T3-28** · Procedure picks saved as full names (3.0.0-beta.2 to beta.8) are turned into root names on load · same as [W1-18](#w1-18).
- <a id="t3-29"></a>**T3-29** · UserConfigLayoutTests guards the 3.0 beta settings layouts · same as [C6-1](#c6-1).
- <a id="w1-19"></a>**W1-19** · Telephony's "older copy" branches serve only 3.0.0-beta.4-and-earlier's single list file · same as [C3-1](#c3-1).
- <a id="w3-24"></a>**W3-24** · FE-Buddy 2.x GitHub-token notice in the shell · same as [C1-16](#c1-16).
