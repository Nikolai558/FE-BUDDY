# 020 · Work plan

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

## How the work runs

1. The team settles [010_Decisions.md](010_Decisions.md).
2. Each session below becomes one Claude Code brief and one pull request. A big session can be split. A brief is written from these files once the decisions it depends on are in, and covers only findings whose Status is Agreed or Agreed, changed.
3. When the pull request merges, the Status of every finding it closed is updated, and the session is ticked on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374).

**Order.** S1 first. [S14](#s14), the fixes to the code S2 to S5 added, comes straight after S5, before S6. S3 to S5 move code into Core, and S6 deletes code and does the [H4](010_Decisions.md#h4) renames, so do them before S7 and S8: fixing comments on code that moves, goes or is renamed is wasted work. S12 comes late because S3 to S5 may change GUI text. S13 is last. What was deferred is listed under [After 3.0](#after-30).

## Sessions

| Session | What | Findings | Files |
|---|---|---:|---|
| [S1](#s1) | Fix-first bugs, plus [#378](https://github.com/Nikolai558/FE-BUDDY/issues/378) | Done | [100](100_Bugs.md) |
| [S2](#s2) | The other bugs, plus [#379](https://github.com/Nikolai558/FE-BUDDY/issues/379) and [Q7](010_Decisions.md#q7) | Done | [100](100_Bugs.md) |
| [S3](#s3) | GUI lag: AIRAC Service and File Conversions | Done | [110](110_GUI-Lag.md) |
| [S4](#s4) | GUI lag: Map, Dashboard, Settings and startup | Done | [110](110_GUI-Lag.md) |
| [S5](#s5) | Business logic from the GUI into Core, plus the Settings page's own node ([H5](010_Decisions.md#h5)) | Done | [120](120_Logic-In-GUI.md) |
| [S14](#s14) | **Next:** fixes to the code S2 to S5 added | 23 | [100](100_Bugs.md), [110](110_GUI-Lag.md), [120](120_Logic-In-GUI.md), [200](200_Comments-Core.md), [220](220_Comments-GUI.md), [300](300_Unit-Tests.md), [500](500_Developer-Docs.md) |
| [S6](#s6) | Old-version code (D1), unused code and the H4 renames, plus two S12 fixes for the public beta | 106 | [130](130_Old-Version-Code.md), [140](140_Unused-Code.md) |
| [S7](#s7) | Comments and XML docs: Core and NASR | 158 | [200](200_Comments-Core.md), [210](210_Comments-NASR.md) |
| [S8](#s8) | Comments and XML docs: GUI | 94 | [220](220_Comments-GUI.md) |
| [S9](#s9) | Unit tests | 101 | [300](300_Unit-Tests.md) |
| [S10](#s10) | Installer, Versioning, CI and build scripts | 41 | [400](400_Installer-Harness-CI.md) |
| [S11](#s11) | Developer docs and house rules | 1 | [500](500_Developer-Docs.md) |
| [S12](#s12) | User docs and on-screen text | 83 | [600](600_User-Docs-Proposal.md), [610](610_On-Screen-Text.md) |
| [S13](#s13) | Final pass, and the [3.0.0 release day](#release-day) | 2 | [500](500_Developer-Docs.md) |
| [After 3.0](#after-30) | Deferred by the decisions | 22 | - |

Counts are the findings each session still has to do, duplicates and deferred findings left out; "+ n" is work the decisions added that isn't a finding. S12 includes the 69 sections of the user-docs proposal.

<a id="s1"></a>

### S1 · Fix-first bugs, plus [#378](https://github.com/Nikolai558/FE-BUDDY/issues/378)

The bugs to fix before anything else, each with a regression test.

| ID | Bug | Status |
|---|---|---|
| [N1-4](100_Bugs.md#n1-4) | Found while fixing W7-3: a save that meets a settings file locked for a moment (antivirus, a sync tool) wrote back an almost empty file, wiping every other setting. | Fixed (not yet merged) |
| [W2-1](100_Bugs.md#w2-1) | The General tab saves and undoes the whole `Services.AiracService` settings node. Saving any other tab (or setting a default ROI on the Map) turns on the General tab's **Undo last save**, and clicking it wipes every saved AIRAC Service setting. Decided ([Q1](010_Decisions.md#q1)): the General tab gets its own node, with no settings-layout step, and a test for [H5](010_Decisions.md#h5). | Fixed (not yet merged) |
| [U1-3](100_Bugs.md#u1-3) | Settings are read on a background thread at launch, after the temp folder is cleared. File Conversions is built from settings when the window goes idle, and nothing reloads it. If it's built first it shows defaults, and saving it can overwrite the user's saved settings with them. | Fixed (not yet merged) |
| [W1-14](100_Bugs.md#w1-14), [W2-10](100_Bugs.md#w2-10) | Switching the cycle on the General tab quietly throws away unsaved ticks in the lists built from the cycle. The tab still shows as unsaved. | Fixed (not yet merged), with U1-1 in S3 |
| [W1-13](100_Bugs.md#w1-13) | Unticking every Procedures.json field sends `JsonFields=""`, which Core reads as "use the 8 defaults". The run writes the fields the user turned off. | Fixed (not yet merged); allows "none", agreed by Kyle |
| [C1-1](100_Bugs.md#c1-1) | If the probe says the *current* cycle is "not yet published" (a proxy answering 403, a PC clock set ahead), the AIRAC Service waits forever with no error. | Fixed (not yet merged) |
| [C5-1](100_Bugs.md#c5-1) | DAT and SCT conversions overwrite each other's output when two sources share a name (`ZOB.sct` and `ZOB.sct2`), with no warning. | Fixed (not yet merged) |
| [C3-18](100_Bugs.md#c3-18) | `Combined_Alias.txt` can be written empty. Uploading it would wipe the facility's aliases. | Fixed (not yet merged) |
| [W3-15](100_Bugs.md#w3-15) | The Reset window says settings backups are always deleted. A reset that keeps settings keeps them. | Fixed (not yet merged) |
| [W7-3](100_Bugs.md#w7-3), [W2-11](100_Bugs.md#w2-11), [W7-4](100_Bugs.md#w7-4) | A settings file that can't be written (antivirus lock, read-only folder) crashes the app on several GUI paths. The crash handler only catches UI-thread exceptions, so a background crash leaves no crash file. | Fixed (not yet merged) |
| [O1-1](100_Bugs.md#o1-1) | Pull requests that only change docs skip the Test job, but the tests read `docs/*.md` (link anchors, file encoding). A doc-only PR can break `v3-development` and still merge. | Fixed (not yet merged) |
| [C1-5](100_Bugs.md#c1-5) | After an update, FE-Buddy relaunched **elevated** from inside msiexec, as the admin who approved it. | Fixed ([#377](https://github.com/Nikolai558/FE-BUDDY/pull/377)) |
| [#378](https://github.com/Nikolai558/FE-BUDDY/issues/378) | Declining the administrator prompt during an update makes setup say FE-Buddy is up to date. Comes from the [C1-5](100_Bugs.md#c1-5) fix. See [Related issues](#related-issues). | Fixed (not yet merged); tested on a VM |

[C3-17](100_Bugs.md#c3-17) was deferred and lowered to Low: the GUI rejects a non-http(s) address before a run can reach it.

<a id="s2"></a>

### S2 · The other bugs

Every other bug in [100_Bugs.md](100_Bugs.md), High and Med first.

**Fixed (not yet merged):** [C1-2](100_Bugs.md#c1-2), [C1-3](100_Bugs.md#c1-3), [C4-11](100_Bugs.md#c4-11), [C2-3](100_Bugs.md#c2-3), [C3-16](100_Bugs.md#c3-16), [C5-8](100_Bugs.md#c5-8), [C5-23](100_Bugs.md#c5-23), [C6-23](100_Bugs.md#c6-23), [C6-29](100_Bugs.md#c6-29), [C7-10](100_Bugs.md#c7-10), [O1-2](100_Bugs.md#o1-2), [O1-42](100_Bugs.md#o1-42), [T1c-26](100_Bugs.md#t1c-26), [T2-8](100_Bugs.md#t2-8), [T2-9](100_Bugs.md#t2-9), [W4-7](100_Bugs.md#w4-7) (with [U1-6](110_GUI-Lag.md#u1-6) in S3), [W2-5](100_Bugs.md#w2-5) (with [W2-6](110_GUI-Lag.md#w2-6) in S3), [W6-3](100_Bugs.md#w6-3) (with [C2-6](120_Logic-In-GUI.md#c2-6) in S5), [W1-7](100_Bugs.md#w1-7), [W2-37](100_Bugs.md#w2-37), [W3-16](100_Bugs.md#w3-16), [W4-8](100_Bugs.md#w4-8), [W5-3](100_Bugs.md#w5-3), [W7-22](100_Bugs.md#w7-22), [O1-23](100_Bugs.md#o1-23), [O1-30](100_Bugs.md#o1-30) - every S2 bug.

**Also:**

- [#379](https://github.com/Nikolai558/FE-BUDDY/issues/379) ([Q9](010_Decisions.md#q9)): a restart notice when FE-Buddy is updated from another Windows account. See [Related issues](#related-issues). **Done** (not yet merged).
- Save the General tab's cycle position, not the cycle ID ([Q7](010_Decisions.md#q7), [D2-46](600_User-Docs-Proposal.md#d2-46)), under its new node from S1. **Done** (not yet merged).

[C1-4](100_Bugs.md#c1-4) and [C5-22](100_Bugs.md#c5-22) are deferred ([Q2](010_Decisions.md#q2), [Q3](010_Decisions.md#q3)).

<a id="s3"></a>

### S3 · GUI lag: AIRAC Service and File Conversions

The AIRAC Service screen and File Conversions: the per-cycle option lists, the d-TPP index, the Telephony list, the Undo check, Add Custom Aliases and the source-folder scans. Build these in Core, off the UI thread, and cache them.

**Findings:** [U1-1](110_GUI-Lag.md#u1-1), [U1-2](110_GUI-Lag.md#u1-2), [C6-30](110_GUI-Lag.md#c6-30), [U1-6](110_GUI-Lag.md#u1-6), [U1-7](110_GUI-Lag.md#u1-7), [W1-1](110_GUI-Lag.md#w1-1), [W1-3](110_GUI-Lag.md#w1-3), [U1-14](110_GUI-Lag.md#u1-14), [U1-15](110_GUI-Lag.md#u1-15), [W1-5](110_GUI-Lag.md#w1-5), [W2-6](110_GUI-Lag.md#w2-6), [W2-27](110_GUI-Lag.md#w2-27), [W4-4](110_GUI-Lag.md#w4-4), [W4-5](110_GUI-Lag.md#w4-5), [W6-6](110_GUI-Lag.md#w6-6)

**Done** (not yet merged): [U1-1](110_GUI-Lag.md#u1-1), with the tabs' lists and their rules in Core ([U1-17](120_Logic-In-GUI.md#u1-17), all but the Settings page's facility list), [W1-1](110_GUI-Lag.md#w1-1), [U1-2](110_GUI-Lag.md#u1-2), [W1-3](110_GUI-Lag.md#w1-3), [U1-14](110_GUI-Lag.md#u1-14), [U1-6](110_GUI-Lag.md#u1-6), [C6-30](110_GUI-Lag.md#c6-30), [U1-7](110_GUI-Lag.md#u1-7), [W4-5](110_GUI-Lag.md#w4-5), [W2-6](110_GUI-Lag.md#w2-6), [W2-27](110_GUI-Lag.md#w2-27), [W1-5](110_GUI-Lag.md#w1-5), [W4-4](110_GUI-Lag.md#w4-4), [U1-15](110_GUI-Lag.md#u1-15), [W6-6](110_GUI-Lag.md#w6-6) - every S3 finding - and the cycle-switch bugs [W1-14](100_Bugs.md#w1-14), [W2-10](100_Bugs.md#w2-10), [W2-8](100_Bugs.md#w2-8), [W2-9](100_Bugs.md#w2-9) and [W1-15](100_Bugs.md#w1-15).

<a id="s4"></a>

### S4 · GUI lag: Map, Dashboard, Settings and startup

The Map (base map, layers, panning), the Dashboard log, settings saves on every click, Settings and startup.

**Findings:** [C7-4](110_GUI-Lag.md#c7-4), [U1-9](110_GUI-Lag.md#u1-9), [W3-1](110_GUI-Lag.md#w3-1), [W3-3](110_GUI-Lag.md#w3-3), [W7-1](110_GUI-Lag.md#w7-1), [C6-31](110_GUI-Lag.md#c6-31), [T3-6](110_GUI-Lag.md#t3-6), [U1-13](110_GUI-Lag.md#u1-13), [W3-4](110_GUI-Lag.md#w3-4), [W3-5](110_GUI-Lag.md#w3-5), [W3-7](110_GUI-Lag.md#w3-7), [W3-8](110_GUI-Lag.md#w3-8), [W7-2](110_GUI-Lag.md#w7-2), [W7-7](110_GUI-Lag.md#w7-7)

**Done** (not yet merged): [W3-7](110_GUI-Lag.md#w3-7), with [W2-27](110_GUI-Lag.md#w2-27) in S3; [C7-4](110_GUI-Lag.md#c7-4), [W7-1](110_GUI-Lag.md#w7-1), [U1-13](110_GUI-Lag.md#u1-13), [W3-1](110_GUI-Lag.md#w3-1), [W3-3](110_GUI-Lag.md#w3-3), [W3-4](110_GUI-Lag.md#w3-4), [C6-31](110_GUI-Lag.md#c6-31), [T3-6](110_GUI-Lag.md#t3-6), [W7-2](110_GUI-Lag.md#w7-2), [W7-7](110_GUI-Lag.md#w7-7), [U1-9](110_GUI-Lag.md#u1-9), [W3-5](110_GUI-Lag.md#w3-5), [W3-8](110_GUI-Lag.md#w3-8) - every S4 finding.

<a id="s5"></a>

### S5 · Business logic from the GUI into Core

Every finding in [120_Logic-In-GUI.md](120_Logic-In-GUI.md). Several overlap S3 and S4: do them together where the same view-model moves.

**Findings:** [U1-17](120_Logic-In-GUI.md#u1-17), [W1-6](120_Logic-In-GUI.md#w1-6), [W3-9](120_Logic-In-GUI.md#w3-9), [C2-6](120_Logic-In-GUI.md#c2-6), [W1-8](120_Logic-In-GUI.md#w1-8), [W1-9](120_Logic-In-GUI.md#w1-9), [W1-10](120_Logic-In-GUI.md#w1-10), [W1-11](120_Logic-In-GUI.md#w1-11), [W1-12](120_Logic-In-GUI.md#w1-12), [W2-7](120_Logic-In-GUI.md#w2-7), [W2-18](120_Logic-In-GUI.md#w2-18), [W2-19](120_Logic-In-GUI.md#w2-19), [W2-20](120_Logic-In-GUI.md#w2-20), [W2-21](120_Logic-In-GUI.md#w2-21), [W2-22](120_Logic-In-GUI.md#w2-22), [W2-23](120_Logic-In-GUI.md#w2-23), [W2-25](120_Logic-In-GUI.md#w2-25), [W3-10](120_Logic-In-GUI.md#w3-10), [W3-12](120_Logic-In-GUI.md#w3-12), [W3-13](120_Logic-In-GUI.md#w3-13), [W3-14](120_Logic-In-GUI.md#w3-14), [W4-6](120_Logic-In-GUI.md#w4-6), [W6-8](120_Logic-In-GUI.md#w6-8), [W7-6](120_Logic-In-GUI.md#w7-6)

**Done** (not yet merged): [U1-17](120_Logic-In-GUI.md#u1-17), with S3 and S4; [W2-19](120_Logic-In-GUI.md#w2-19), [W2-20](120_Logic-In-GUI.md#w2-20), [W1-10](120_Logic-In-GUI.md#w1-10), [W7-6](120_Logic-In-GUI.md#w7-6), [W3-12](120_Logic-In-GUI.md#w3-12), [W2-21](120_Logic-In-GUI.md#w2-21), [W2-25](120_Logic-In-GUI.md#w2-25), [W1-6](120_Logic-In-GUI.md#w1-6), [W1-12](120_Logic-In-GUI.md#w1-12), [W2-18](120_Logic-In-GUI.md#w2-18), [W1-9](120_Logic-In-GUI.md#w1-9), [W1-8](120_Logic-In-GUI.md#w1-8), [W4-6](120_Logic-In-GUI.md#w4-6), [W2-7](120_Logic-In-GUI.md#w2-7), [W2-22](120_Logic-In-GUI.md#w2-22), [W1-11](120_Logic-In-GUI.md#w1-11), [W2-23](120_Logic-In-GUI.md#w2-23), [W6-8](120_Logic-In-GUI.md#w6-8), [W3-14](120_Logic-In-GUI.md#w3-14), [W3-13](120_Logic-In-GUI.md#w3-13), [W3-10](120_Logic-In-GUI.md#w3-10), [W3-9](120_Logic-In-GUI.md#w3-9), [C2-6](120_Logic-In-GUI.md#c2-6) - every S5 finding.

**Also:** move the Settings page's keys (the Facility, the coordinate precision and the default ROI) out of `Services.AiracService` into the Settings page's own node ([Q1](010_Decisions.md#q1), [H5](010_Decisions.md#h5)), with no settings-layout step. Do it with [W3-10](120_Logic-In-GUI.md#w3-10), [C6-13](200_Comments-Core.md#c6-13) and [D1a-3](500_Developer-Docs.md#d1a-3), which flag the copies of the Facility key. **Done** (not yet merged): they are under `General` (`General.Facility`, `General.CoordinatePrecision`, `General.DefaultRoi`), with W3-10, C6-13 and D1a-3. A settings file from before keeps the old values where they were, unread: Settings opens with no facility, 6 decimal places and no default ROI. Also fixed: [W6-3](100_Bugs.md#w6-3) (S2), with C2-6.

<a id="s14"></a>

### S14 · Fixes to the S2 to S5 code

On 2026-10-19, after Nikolas's last push (`4893e32`), the code [S2](#s2) to [S5](#s5) added was reviewed the way the original review was, and each finding was checked in the code before it was written up: 39 findings, N1-5 to N1-43. S14 is 38 of them; [N1-34](130_Old-Version-Code.md#n1-34) goes away with [C6-1](130_Old-Version-Code.md#c6-1) in [S6](#s6). Do S14 before S6. Each finding's Problem and Proposed lines say what to change and the test to add.

| Area | Findings |
|---|---|
| AIRAC Service and Procedure Charts | [N1-5](100_Bugs.md#n1-5) (Med), [N1-10](100_Bugs.md#n1-10), [N1-11](100_Bugs.md#n1-11), [N1-12](100_Bugs.md#n1-12), [N1-13](100_Bugs.md#n1-13), [N1-29](110_GUI-Lag.md#n1-29), [N1-35](200_Comments-Core.md#n1-35), [N1-39](300_Unit-Tests.md#n1-39), [N1-43](500_Developer-Docs.md#n1-43) |
| Add Custom Aliases, File Conversions, Wx Stations, Telephony, File Names, Departures and Arrivals, the Map's GeoJSON reader | [N1-6](100_Bugs.md#n1-6) (Med), [N1-14](100_Bugs.md#n1-14), [N1-22](100_Bugs.md#n1-22), [N1-23](100_Bugs.md#n1-23), [N1-33](120_Logic-In-GUI.md#n1-33), [N1-40](300_Unit-Tests.md#n1-40) |
| Settings files, profiles and saves, the Dashboard, folder pickers | [N1-7](100_Bugs.md#n1-7) (Med), [N1-8](100_Bugs.md#n1-8) (Med), [N1-15](100_Bugs.md#n1-15), [N1-16](100_Bugs.md#n1-16), [N1-17](100_Bugs.md#n1-17), [N1-18](100_Bugs.md#n1-18), [N1-19](100_Bugs.md#n1-19), [N1-20](100_Bugs.md#n1-20), [N1-21](100_Bugs.md#n1-21), [N1-30](110_GUI-Lag.md#n1-30), [N1-31](110_GUI-Lag.md#n1-31), [N1-32](110_GUI-Lag.md#n1-32), [N1-36](200_Comments-Core.md#n1-36), [N1-37](220_Comments-GUI.md#n1-37), [N1-41](300_Unit-Tests.md#n1-41) |
| The update watcher ([#379](https://github.com/Nikolai558/FE-BUDDY/issues/379)), the setup window, the build counter | [N1-9](100_Bugs.md#n1-9) (Med), [N1-24](100_Bugs.md#n1-24), [N1-25](100_Bugs.md#n1-25), [N1-26](100_Bugs.md#n1-26), [N1-27](100_Bugs.md#n1-27), [N1-28](100_Bugs.md#n1-28), [N1-42](300_Unit-Tests.md#n1-42) |
| House rules in the new code and tests | [N1-38](300_Unit-Tests.md#n1-38) |

**Fixed (not yet merged):** [N1-5](100_Bugs.md#n1-5), [N1-10](100_Bugs.md#n1-10), [N1-11](100_Bugs.md#n1-11), [N1-12](100_Bugs.md#n1-12), [N1-13](100_Bugs.md#n1-13), [N1-29](110_GUI-Lag.md#n1-29), [N1-35](200_Comments-Core.md#n1-35), [N1-39](300_Unit-Tests.md#n1-39), [N1-43](500_Developer-Docs.md#n1-43), [N1-6](100_Bugs.md#n1-6), [N1-14](100_Bugs.md#n1-14), [N1-22](100_Bugs.md#n1-22), [N1-23](100_Bugs.md#n1-23), [N1-33](120_Logic-In-GUI.md#n1-33), [N1-40](300_Unit-Tests.md#n1-40)

<a id="s6"></a>

### S6 · Old-version code (D1) and unused code

Every finding in [130_Old-Version-Code.md](130_Old-Version-Code.md) (as [D1](010_Decisions.md#d1) decides) and [140_Unused-Code.md](140_Unused-Code.md) ([Q5](010_Decisions.md#q5) decides the test-only ones). Includes [D2](010_Decisions.md#d2), deleting `AliasParser.cs`. [W4-18](140_Unused-Code.md#w4-18) to [W4-20](140_Unused-Code.md#w4-20) are deferred ([Q6](010_Decisions.md#q6)); the News stub ([C7-1](130_Old-Version-Code.md#c7-1)) stays until after 3.0.

**Also:** the [H4](010_Decisions.md#h4) renames and file moves, here rather than in the comment sessions: [C2-20](200_Comments-Core.md#c2-20), [C3-22](200_Comments-Core.md#c3-22), [C1-34](200_Comments-Core.md#c1-34), [C1-35](200_Comments-Core.md#c1-35), [C6-34](200_Comments-Core.md#c6-34), [W6-29](220_Comments-GUI.md#w6-29), [W7-12](220_Comments-GUI.md#w7-12), [W4-36](220_Comments-GUI.md#w4-36), [D1b-10](500_Developer-Docs.md#d1b-10).

**Docs that go with the code:**

- With [C3-1](130_Old-Version-Code.md#c3-1), delete `Architecture.md`'s sentence about the older single `vatsim_radar_airlines.json` ([D1b-4](500_Developer-Docs.md#d1b-4)).
- With [C4-12](130_Old-Version-Code.md#c4-12), delete or reword `Settings-Reference.md`'s `IncludeRoiAirports` lines ([D1a-11](500_Developer-Docs.md#d1a-11)).

**Before the public beta**, pulled forward from [S12](#s12) (Kyle, 2026-10-19): [W5-2](610_On-Screen-Text.md#w5-2), the CRC ERAM Defaults note on every AIRAC tab that names `UserConfig.json`, and [D2-51](600_User-Docs-Proposal.md#d2-51), the FAQ's "Does FE-Buddy send my data anywhere?".

<a id="s7"></a>

### S7 · Comments and XML docs: Core and NASR

Every finding in [200_Comments-Core.md](200_Comments-Core.md) and [210_Comments-NASR.md](210_Comments-NASR.md), apart from the H4 renames (S6) and [C2-29](200_Comments-Core.md#c2-29) and [C5-25](200_Comments-Core.md#c5-25), which are deferred ([D7](010_Decisions.md#d7)). Mostly mechanical, though some findings change code. Probably two pull requests.

**Fixed (not yet merged):** [C6-13](200_Comments-Core.md#c6-13), with the Settings page's keys in [S5](#s5).

<a id="s8"></a>

### S8 · Comments and XML docs: GUI

Every finding in [220_Comments-GUI.md](220_Comments-GUI.md), apart from the H4 renames (S6).

<a id="s9"></a>

### S9 · Unit tests

Every finding in [300_Unit-Tests.md](300_Unit-Tests.md). Tests for code removed in S6 go with S6.

<a id="s10"></a>

### S10 · Installer, Versioning, CI and build scripts

Every finding in [400_Installer-Harness-CI.md](400_Installer-Harness-CI.md) except its `FeBuddy.Harness` section, which is deferred ([D6](010_Decisions.md#d6)).

<a id="s11"></a>

### S11 · Developer docs and house rules

Every finding in [500_Developer-Docs.md](500_Developer-Docs.md) apart from [D1b-1](500_Developer-Docs.md#d1b-1) (S6) and [D1b-10](500_Developer-Docs.md#d1b-10) (S6), the agreed house rules H1 to H5 ([010_Decisions.md](010_Decisions.md#proposed-house-rules)), and a `TODO.md` item for each entry under [After 3.0](#after-30).

**Fixed (not yet merged):** the house rules H1 to H5, written into `Code-Structure.md`; the `TODO.md` items; [D1b-2](500_Developer-Docs.md#d1b-2), [D1b-3](500_Developer-Docs.md#d1b-3), [D1b-9](500_Developer-Docs.md#d1b-9), [W5-22](500_Developer-Docs.md#w5-22), [W7-26](500_Developer-Docs.md#w7-26), [D1b-16](500_Developer-Docs.md#d1b-16), [C1-32](500_Developer-Docs.md#c1-32), [C1-33](500_Developer-Docs.md#c1-33), [D1b-4](500_Developer-Docs.md#d1b-4), [D1b-5](500_Developer-Docs.md#d1b-5), [D1b-6](500_Developer-Docs.md#d1b-6), [D1b-7](500_Developer-Docs.md#d1b-7), [D1a-1](500_Developer-Docs.md#d1a-1), [D1a-4](500_Developer-Docs.md#d1a-4), [D1a-5](500_Developer-Docs.md#d1a-5), [D1a-6](500_Developer-Docs.md#d1a-6), [D1a-7](500_Developer-Docs.md#d1a-7), [D1a-8](500_Developer-Docs.md#d1a-8), [C2-31](500_Developer-Docs.md#c2-31), [C2-32](500_Developer-Docs.md#c2-32), [C3-15](500_Developer-Docs.md#c3-15), [D1a-10](500_Developer-Docs.md#d1a-10), [D1a-13](500_Developer-Docs.md#d1a-13), [D1a-14](500_Developer-Docs.md#d1a-14), [D1a-18](500_Developer-Docs.md#d1a-18), [D1b-13](500_Developer-Docs.md#d1b-13), [D1b-14](500_Developer-Docs.md#d1b-14), [D1b-15](500_Developer-Docs.md#d1b-15), [O1-36](500_Developer-Docs.md#o1-36), [N1-1](500_Developer-Docs.md#n1-1), [D1a-3](500_Developer-Docs.md#d1a-3) (with the Settings page's keys in [S5](#s5))

**Still to do:** [D1a-11](500_Developer-Docs.md#d1a-11) goes with its code in [S6](#s6); [D1b-11](500_Developer-Docs.md#d1b-11) and [D1b-12](500_Developer-Docs.md#d1b-12) wait for the [3.0.0 release day](#release-day).

<a id="s12"></a>

### S12 · User docs and on-screen text

Shrink the user manuals as [600_User-Docs-Proposal.md](600_User-Docs-Proposal.md) proposes, add the GUI text it lists, and fix [610_On-Screen-Text.md](610_On-Screen-Text.md). The token guide's `## If something goes wrong` heading and the three file names the app links to must survive (a test checks them). [W5-2](610_On-Screen-Text.md#w5-2) and [D2-51](600_User-Docs-Proposal.md#d2-51) are done in [S6](#s6), before the public beta.

<a id="release-day"></a>

### On the 3.0.0 release day

Docs that are true only until 3.0.0 is published. Change them in the release's own pull request, with [S13](#s13).

- [D1b-12](500_Developer-Docs.md#d1b-12): `RELEASING.md` becomes "The installer is always `FE-BUDDY-Setup.msi`, so `…/releases/latest/download/FE-BUDDY-Setup.msi` is always the latest **stable** release." In `VERSIONING.md`, drop "(Alpha and Beta now get `3.0.0-beta.N`)", as the finding proposes.
- [D1b-11](500_Developer-Docs.md#d1b-11): in `SECURITY.md`'s support table, every version before 3.0 is unsupported (`:x:`; decided by Kyle). The paragraph becomes "FE-BUDDY 2.9.3 is the last 2.x release. Please move to FE-BUDDY 3.x: download it from [Releases](https://github.com/Nikolai558/FE-BUDDY/releases), or accept the update 2.9.x's updater offers."

<a id="s13"></a>

### S13 · Final pass

Build, run every test, and search the code for history wording and issue numbers the sessions missed. Then decide what happens to this folder ([000_ReadMe.md](000_ReadMe.md#when-the-project-is-done)).

<a id="after-30"></a>

### After 3.0

Deferred by the decisions. Each finding's Status says what was agreed. Each item below now has a line in `TODO.md`'s "After 3.0" section.

| What | Findings | Decision |
|---|---|---|
| The Harness: a switch for its runs, XML docs, no machine-specific paths | [O1-4](400_Installer-Harness-CI.md#o1-4) to [O1-17](400_Installer-Harness-CI.md#o1-17) (400's `FeBuddy.Harness` section, 14 findings) | [D6](010_Decisions.md#d6) |
| Share the Departures/Arrivals code and other copies | [C2-29](200_Comments-Core.md#c2-29), [C5-25](200_Comments-Core.md#c5-25) | [D7](010_Decisions.md#d7) |
| One Core constant for the d-TPP timing, and a look for other values hard-coded in more than one place | [C7-9](200_Comments-Core.md#c7-9) (the wording itself is fixed before 3.0) | [D4](010_Decisions.md#d4) |
| Handle a new AIRAC cycle during a session without a restart | [C1-4](100_Bugs.md#c1-4) | [Q2](010_Decisions.md#q2) |
| A wrong DAT or SCT file fails | [C5-22](100_Bugs.md#c5-22) | [Q3](010_Decisions.md#q3) |
| Keep a secret in a non-http(s) custom alias address out of the error message, if non-web sources are ever allowed | [C3-17](100_Bugs.md#c3-17) | Re-check at `4773137` |
| GUI framework simplifications | [W4-18](140_Unused-Code.md#w4-18), [W4-19](140_Unused-Code.md#w4-19), [W4-20](140_Unused-Code.md#w4-20) | [Q6](010_Decisions.md#q6) |
| Delete the alpha.1 News stub | [C7-1](130_Old-Version-Code.md#c7-1) | [D1](010_Decisions.md#d1) |

## Related issues

### [#378](https://github.com/Nikolai558/FE-BUDDY/issues/378) · Setup says "up to date" after the UAC prompt is declined

Opened by Nikolas on 2026-10-16. A side effect of the [C1-5](100_Bugs.md#c1-5) fix ([#377](https://github.com/Nikolai558/FE-BUDDY/pull/377)): Update now starts msiexec unelevated since that fix, so Windows asks for permission partway through setup, after FE-Buddy has closed. Session [S1](#s1).

**Checked in the code** (at `e15a634`): `_installResult` starts at -1 ([`SetupUI.cs:52`](https://github.com/Nikolai558/FE-BUDDY/blob/e15a634aa6915245e558875cd249bccdb8455480/FeBuddy/FeBuddy.Installer.UI/SetupUI.cs#L52)) and only `InstallEnd` sets it. With no `InstallEnd`, neither the Cancelled test (`_cancelSent`, or result 2) nor the Failed test (`_fatalExit`, or result 3 or 0) matches, so `Shutdown` reports Succeeded ([`SetupUI.cs:195-199`](https://github.com/Nikolai558/FE-BUDDY/blob/e15a634aa6915245e558875cd249bccdb8455480/FeBuddy/FeBuddy.Installer.UI/SetupUI.cs#L195-L199)). The issue's diagnosis is right.

**Worth adding to the fix:**

- The same path most likely reports **any** declined prompt as success, not only an update's: a fresh install by double-click, and Uninstall from Settings (`msiexec /x` has run unelevated since before the review), which would show "removed from this PC". Worth testing all three.
- Before [#377](https://github.com/Nikolai558/FE-BUDDY/pull/377), the update window said "Update cancelled - installing needs administrator permission". The setup window's Cancelled page could say the same, and that nothing changed.
- [N1-3](610_On-Screen-Text.md#n1-3) adds a heads-up to the update window, so the prompt isn't a surprise.

**Fixed** (not yet merged). Whether Windows always sends `InstallEnd` after a successful install couldn't be confirmed: Microsoft's documentation says it is raised at the end of an install, with the result in field 3, but doesn't promise it every time. So the fix doesn't rely on it. A result of 1 is a success as before. With no result, `SetupOutcomeRules` (`FeBuddy.Installer.UI/Setup`) looks at what is installed now: a new version, or FE-Buddy gone after an uninstall, is a success; anything else is a new outcome, **Setup didn't run**, whose page says nothing changed, that the administrator prompt was most likely declined, and to try again and choose Yes when Windows asks (worded after Kyle's review to suit an update or uninstall started from FE-Buddy too). A repair or a reinstall of the same version can't be told from nothing happening, so with no result it reads as didn't run. Tests: `SetupOutcomeRulesTests`, with the rule compiled into the unit tests from source, since the setup window targets .NET Framework. **Tested** by Nikolas on a VM: declining the prompt for an update, a fresh install (double-click) and an uninstall from Settings shows "Setup didn't run", and a normal update still says "up to date".

### [#379](https://github.com/Nikolai558/FE-BUDDY/issues/379) · FE-Buddy left open in another account keeps running the old version

Opened by Nikolas on 2026-10-16. Low risk: it needs two Windows accounts, one with FE-Buddy left open while the other updates. Nikolas proposes watching `FE-BUDDY.dll` with a `FileSystemWatcher` and showing a Restart notice when its version changes. That is cheap. The watcher's events arrive on a thread-pool thread, so only the notice itself should go to the UI thread. A simpler option: compare the version on disk only when a run starts. Decided ([Q9](010_Decisions.md#q9)): before 3.0, in [S2](#s2).

**Fixed** (not yet merged), both ways: Core's `InstalledVersionWatcher` watches FE-Buddy's own `FE-BUDDY.dll` and
reads nothing until Windows reports a change to it. Once it has been quiet for 3 seconds (an install replaces it in
steps), the product version on disk is compared with the running one. When they differ, the main window offers a
restart (`InstalledUpdate`, warning about anything unfinished, as Settings' restarts do), and until FE-Buddy restarts,
starting an AIRAC Service run or a file conversion offers it again instead of running. Tests:
`InstalledVersionWatcherTests`.

**Found on a VM** (two accounts, Nikolas, 2026-10-18): the first version never noticed. The update's own uninstall
of the old version moves the files in use out to `C:\Config.Msi`, deletes the emptied `C:\Program Files\FE-BUDDY`,
then creates it again for the new files, so a watch on the folder was left on the deleted one. The watcher now also
watches the folder above it, sets its watch up again on the new folder, and looks again while the file is missing
mid-install; a test deletes and recreates the folder as Windows Installer does. Testing it in one account doesn't work:
that same uninstall step closes FE-Buddy in the installing account, whatever Restart Manager is told.

**Found next** (the VM retest, then replayed on Nikolas's PC against a running FE-Buddy): the check then ran, but read
the new file as the old version. Windows' version information (`FileVersionInfo`) answers from the copy of
`FE-BUDDY.dll` the process already has loaded, so a running FE-Buddy only ever hears its own version back. The
version is now read from the file's own metadata. Proven on the PC: a running FE-Buddy, its folder emptied, deleted and
recreated with a newer build, logs the update about 3 seconds later and opens the restart offer. The watch also logs
when it starts and what each check finds. **Retest** with two accounts on the VM pending.

**Found in the review of the new code** (2026-10-19): the offer can come while the update is still installing, and an
update that rolls back leaves runs blocked ([N1-9](100_Bugs.md#n1-9)); the watcher can also crash FE-Buddy or stop
looking ([N1-25](100_Bugs.md#n1-25), [N1-26](100_Bugs.md#n1-26)). Fixed in [S14](#s14), before the two-account retest.

### [#375](https://github.com/Nikolai558/FE-BUDDY/issues/375) · The installer's shortcuts showed a cached 2.x icon

Fixed by Nikolas ([#376](https://github.com/Nikolai558/FE-BUDDY/pull/376)): both shortcuts now use the MSI's icon. Not part of this review; no finding is about what it changed.
