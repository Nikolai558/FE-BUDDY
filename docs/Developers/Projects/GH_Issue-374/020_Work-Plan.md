# 020 · Work plan

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

## How the work runs

1. The team settles [010_Decisions.md](010_Decisions.md).
2. Each session below becomes one Claude Code brief and one pull request. A big session can be split. A brief is written from these files once the decisions it depends on are in, and covers only findings whose Status is Open or Agreed.
3. When the pull request merges, the Status of every finding it closed is updated, and the session is ticked on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374).

**Order.** S1 first. S3 to S5 move code into Core, and S6 deletes code, so do them before S7 and S8: fixing comments on code that moves or goes is wasted work. S12 comes late because S3 to S5 may change GUI text. S13 is last.

## Sessions

| Session | What | Findings | Files |
|---|---|---:|---|
| [S1](#s1) | Fix-first bugs, plus [#378](https://github.com/Nikolai558/FE-BUDDY/issues/378) | 14 + [#378](https://github.com/Nikolai558/FE-BUDDY/issues/378) | [100](100_Bugs.md) |
| [S2](#s2) | The other bugs | 31 | [100](100_Bugs.md) |
| [S3](#s3) | GUI lag: AIRAC Service and File Conversions | 15 | [110](110_GUI-Lag.md) |
| [S4](#s4) | GUI lag: Map, Dashboard, Settings and startup | 14 | [110](110_GUI-Lag.md) |
| [S5](#s5) | Business logic from the GUI into Core | 24 | [120](120_Logic-In-GUI.md) |
| [S6](#s6) | Old-version code (D1) and unused code | 95 | [130](130_Old-Version-Code.md), [140](140_Unused-Code.md) |
| [S7](#s7) | Comments and XML docs: Core and NASR | 165 | [200](200_Comments-Core.md), [210](210_Comments-NASR.md) |
| [S8](#s8) | Comments and XML docs: GUI | 97 | [220](220_Comments-GUI.md) |
| [S9](#s9) | Unit tests | 101 | [300](300_Unit-Tests.md) |
| [S10](#s10) | Installer, Harness (D6), Versioning, CI and build scripts | 55 | [400](400_Installer-Harness-CI.md) |
| [S11](#s11) | Developer docs and house rules | 36 | [500](500_Developer-Docs.md) |
| [S12](#s12) | User docs and on-screen text | 85 | [600](600_User-Docs-Proposal.md), [610](610_On-Screen-Text.md) |
| [S13](#s13) | Final pass | - | - |

Counts are open findings, duplicates left out. S12 includes the 69 sections of the user-docs proposal.

<a id="s1"></a>

### S1 · Fix-first bugs, plus [#378](https://github.com/Nikolai558/FE-BUDDY/issues/378)

The bugs to fix before anything else, each with a regression test.

| ID | Bug | Status |
|---|---|---|
| [W2-1](100_Bugs.md#w2-1) | The General tab saves and undoes the whole `Services.AiracService` settings node. Saving any other tab (or setting a default ROI on the Map) turns on the General tab's **Undo last save**, and clicking it wipes every saved AIRAC Service setting. See [Q1](010_Decisions.md#q1). | Open |
| [U1-3](100_Bugs.md#u1-3) | Settings are read on a background thread at launch, after the temp folder is cleared. File Conversions is built from settings when the window goes idle, and nothing reloads it. If it's built first it shows defaults, and saving it can overwrite the user's saved settings with them. | Open |
| [W1-14](100_Bugs.md#w1-14), [W2-10](100_Bugs.md#w2-10) | Switching the cycle on the General tab quietly throws away unsaved ticks in the lists built from the cycle. The tab still shows as unsaved. | Open |
| [W1-13](100_Bugs.md#w1-13) | Unticking every Procedures.json field sends `JsonFields=""`, which Core reads as "use the 8 defaults". The run writes the fields the user turned off. | Open |
| [C1-1](100_Bugs.md#c1-1) | If the probe says the *current* cycle is "not yet published" (a proxy answering 403, a PC clock set ahead), the AIRAC Service waits forever with no error. | Open |
| [C5-1](100_Bugs.md#c5-1) | DAT and SCT conversions overwrite each other's output when two sources share a name (`ZOB.sct` and `ZOB.sct2`), with no warning. | Open |
| [C3-18](100_Bugs.md#c3-18) | `Combined_Alias.txt` can be written empty. Uploading it would wipe the facility's aliases. | Open |
| [C3-17](100_Bugs.md#c3-17) | A secret in a non-http address (`ftp://user:token@host/…`) ends up in the error message, the Review tab and the log. | Open |
| [W3-15](100_Bugs.md#w3-15) | The Reset window says settings backups are always deleted. A reset that keeps settings keeps them. | Open |
| [W7-3](100_Bugs.md#w7-3), [W2-11](100_Bugs.md#w2-11), [W7-4](100_Bugs.md#w7-4) | A settings file that can't be written (antivirus lock, read-only folder) crashes the app on several GUI paths. The crash handler only catches UI-thread exceptions, so a background crash leaves no crash file. | Open |
| [O1-1](100_Bugs.md#o1-1) | Pull requests that only change docs skip the Test job, but the tests read `docs/*.md` (link anchors, file encoding). A doc-only PR can break `v3-development` and still merge. | Open |
| [C1-5](100_Bugs.md#c1-5) | After an update, FE-Buddy relaunched **elevated** from inside msiexec, as the admin who approved it. | Fixed ([#377](https://github.com/Nikolai558/FE-BUDDY/pull/377)) |
| [#378](https://github.com/Nikolai558/FE-BUDDY/issues/378) | Declining the administrator prompt during an update makes setup say FE-Buddy is up to date. Comes from the [C1-5](100_Bugs.md#c1-5) fix. See [Related issues](#related-issues). | Open |

<a id="s2"></a>

### S2 · The other bugs

Every other bug in [100_Bugs.md](100_Bugs.md), High and Med first.

**Findings:** [C1-2](100_Bugs.md#c1-2), [C1-3](100_Bugs.md#c1-3), [C1-4](100_Bugs.md#c1-4), [C2-3](100_Bugs.md#c2-3), [C3-16](100_Bugs.md#c3-16), [C4-11](100_Bugs.md#c4-11), [C5-8](100_Bugs.md#c5-8), [C5-22](100_Bugs.md#c5-22), [C5-23](100_Bugs.md#c5-23), [C6-23](100_Bugs.md#c6-23), [C6-29](100_Bugs.md#c6-29), [C7-10](100_Bugs.md#c7-10), [O1-2](100_Bugs.md#o1-2), [O1-23](100_Bugs.md#o1-23), [O1-30](100_Bugs.md#o1-30), [O1-42](100_Bugs.md#o1-42), [T1c-26](100_Bugs.md#t1c-26), [T2-8](100_Bugs.md#t2-8), [T2-9](100_Bugs.md#t2-9), [W1-7](100_Bugs.md#w1-7), [W1-15](100_Bugs.md#w1-15), [W2-5](100_Bugs.md#w2-5), [W2-8](100_Bugs.md#w2-8), [W2-9](100_Bugs.md#w2-9), [W2-37](100_Bugs.md#w2-37), [W3-16](100_Bugs.md#w3-16), [W4-7](100_Bugs.md#w4-7), [W4-8](100_Bugs.md#w4-8), [W5-3](100_Bugs.md#w5-3), [W6-3](100_Bugs.md#w6-3), [W7-22](100_Bugs.md#w7-22)

<a id="s3"></a>

### S3 · GUI lag: AIRAC Service and File Conversions

The AIRAC Service screen and File Conversions: the per-cycle option lists, the d-TPP index, the Telephony list, the Undo check, Add Custom Aliases and the source-folder scans. Build these in Core, off the UI thread, and cache them.

**Findings:** [U1-1](110_GUI-Lag.md#u1-1), [U1-2](110_GUI-Lag.md#u1-2), [C6-30](110_GUI-Lag.md#c6-30), [U1-6](110_GUI-Lag.md#u1-6), [U1-7](110_GUI-Lag.md#u1-7), [W1-1](110_GUI-Lag.md#w1-1), [W1-3](110_GUI-Lag.md#w1-3), [U1-14](110_GUI-Lag.md#u1-14), [U1-15](110_GUI-Lag.md#u1-15), [W1-5](110_GUI-Lag.md#w1-5), [W2-6](110_GUI-Lag.md#w2-6), [W2-27](110_GUI-Lag.md#w2-27), [W4-4](110_GUI-Lag.md#w4-4), [W4-5](110_GUI-Lag.md#w4-5), [W6-6](110_GUI-Lag.md#w6-6)

<a id="s4"></a>

### S4 · GUI lag: Map, Dashboard, Settings and startup

The Map (base map, layers, panning), the Dashboard log, settings saves on every click, Settings and startup.

**Findings:** [C7-4](110_GUI-Lag.md#c7-4), [U1-9](110_GUI-Lag.md#u1-9), [W3-1](110_GUI-Lag.md#w3-1), [W3-3](110_GUI-Lag.md#w3-3), [W7-1](110_GUI-Lag.md#w7-1), [C6-31](110_GUI-Lag.md#c6-31), [T3-6](110_GUI-Lag.md#t3-6), [U1-13](110_GUI-Lag.md#u1-13), [W3-4](110_GUI-Lag.md#w3-4), [W3-5](110_GUI-Lag.md#w3-5), [W3-7](110_GUI-Lag.md#w3-7), [W3-8](110_GUI-Lag.md#w3-8), [W7-2](110_GUI-Lag.md#w7-2), [W7-7](110_GUI-Lag.md#w7-7)

<a id="s5"></a>

### S5 · Business logic from the GUI into Core

Every finding in [120_Logic-In-GUI.md](120_Logic-In-GUI.md). Several overlap S3 and S4: do them together where the same view-model moves.

**Findings:** [U1-17](120_Logic-In-GUI.md#u1-17), [W1-6](120_Logic-In-GUI.md#w1-6), [W3-9](120_Logic-In-GUI.md#w3-9), [C2-6](120_Logic-In-GUI.md#c2-6), [W1-8](120_Logic-In-GUI.md#w1-8), [W1-9](120_Logic-In-GUI.md#w1-9), [W1-10](120_Logic-In-GUI.md#w1-10), [W1-11](120_Logic-In-GUI.md#w1-11), [W1-12](120_Logic-In-GUI.md#w1-12), [W2-7](120_Logic-In-GUI.md#w2-7), [W2-18](120_Logic-In-GUI.md#w2-18), [W2-19](120_Logic-In-GUI.md#w2-19), [W2-20](120_Logic-In-GUI.md#w2-20), [W2-21](120_Logic-In-GUI.md#w2-21), [W2-22](120_Logic-In-GUI.md#w2-22), [W2-23](120_Logic-In-GUI.md#w2-23), [W2-25](120_Logic-In-GUI.md#w2-25), [W3-10](120_Logic-In-GUI.md#w3-10), [W3-12](120_Logic-In-GUI.md#w3-12), [W3-13](120_Logic-In-GUI.md#w3-13), [W3-14](120_Logic-In-GUI.md#w3-14), [W4-6](120_Logic-In-GUI.md#w4-6), [W6-8](120_Logic-In-GUI.md#w6-8), [W7-6](120_Logic-In-GUI.md#w7-6)

<a id="s6"></a>

### S6 · Old-version code (D1) and unused code

Every finding in [130_Old-Version-Code.md](130_Old-Version-Code.md) (as [D1](010_Decisions.md#d1) decides) and [140_Unused-Code.md](140_Unused-Code.md) ([Q5](010_Decisions.md#q5) decides the test-only ones). Includes [D2](010_Decisions.md#d2), deleting `AliasParser.cs`.

<a id="s7"></a>

### S7 · Comments and XML docs: Core and NASR

Every finding in [200_Comments-Core.md](200_Comments-Core.md) and [210_Comments-NASR.md](210_Comments-NASR.md). Mostly mechanical. Probably two pull requests.

<a id="s8"></a>

### S8 · Comments and XML docs: GUI

Every finding in [220_Comments-GUI.md](220_Comments-GUI.md).

<a id="s9"></a>

### S9 · Unit tests

Every finding in [300_Unit-Tests.md](300_Unit-Tests.md). Tests for code removed in S6 go with S6.

<a id="s10"></a>

### S10 · Installer, Harness (D6), Versioning, CI and build scripts

Every finding in [400_Installer-Harness-CI.md](400_Installer-Harness-CI.md), plus the Harness switch ([D6](010_Decisions.md#d6)).

<a id="s11"></a>

### S11 · Developer docs and house rules

Every finding in [500_Developer-Docs.md](500_Developer-Docs.md), the agreed house rules ([010_Decisions.md](010_Decisions.md#proposed-house-rules)), and the [D7](010_Decisions.md#d7) item in `TODO.md`.

<a id="s12"></a>

### S12 · User docs and on-screen text

Shrink the user manuals as [600_User-Docs-Proposal.md](600_User-Docs-Proposal.md) proposes, add the GUI text it lists, and fix [610_On-Screen-Text.md](610_On-Screen-Text.md). The token guide's `## If something goes wrong` heading and the three file names the app links to must survive (a test checks them).

<a id="s13"></a>

### S13 · Final pass

Build, run every test, and search the code for history wording and issue numbers the sessions missed. Then decide what happens to this folder ([000_ReadMe.md](000_ReadMe.md#when-the-project-is-done)).

## Related issues

### [#378](https://github.com/Nikolai558/FE-BUDDY/issues/378) · Setup says "up to date" after the UAC prompt is declined

Opened by Nikolas on 2026-10-16. A side effect of the [C1-5](100_Bugs.md#c1-5) fix ([#377](https://github.com/Nikolai558/FE-BUDDY/pull/377)): Update now starts msiexec unelevated since that fix, so Windows asks for permission partway through setup, after FE-Buddy has closed. Session [S1](#s1).

**Checked in the code** (at `e15a634`): `_installResult` starts at -1 ([`SetupUI.cs:52`](https://github.com/Nikolai558/FE-BUDDY/blob/e15a634aa6915245e558875cd249bccdb8455480/FeBuddy/FeBuddy.Installer.UI/SetupUI.cs#L52)) and only `InstallEnd` sets it. With no `InstallEnd`, neither the Cancelled test (`_cancelSent`, or result 2) nor the Failed test (`_fatalExit`, or result 3 or 0) matches, so `Shutdown` reports Succeeded ([`SetupUI.cs:195-199`](https://github.com/Nikolai558/FE-BUDDY/blob/e15a634aa6915245e558875cd249bccdb8455480/FeBuddy/FeBuddy.Installer.UI/SetupUI.cs#L195-L199)). The issue's diagnosis is right.

**Worth adding to the fix:**

- The same path most likely reports **any** declined prompt as success, not only an update's: a fresh install by double-click, and Uninstall from Settings (`msiexec /x` has run unelevated since before the review), which would show "removed from this PC". Worth testing all three.
- Before [#377](https://github.com/Nikolai558/FE-BUDDY/pull/377), the update window said "Update cancelled - installing needs administrator permission". The setup window's Cancelled page could say the same, and that nothing changed.
- [N1-3](610_On-Screen-Text.md#n1-3) adds a heads-up to the update window, so the prompt isn't a surprise.

### [#379](https://github.com/Nikolai558/FE-BUDDY/issues/379) · FE-Buddy left open in another account keeps running the old version

Opened by Nikolas on 2026-10-16. Low risk: it needs two Windows accounts, one with FE-Buddy left open while the other updates. Nikolas proposes watching `FE-BUDDY.dll` with a `FileSystemWatcher` and showing a Restart notice when its version changes. That is cheap. The watcher's events arrive on a thread-pool thread, so only the notice itself should go to the UI thread. A simpler option: compare the version on disk only when a run starts. Whether it goes in before 3.0 is [Q9](010_Decisions.md#q9).

### [#375](https://github.com/Nikolai558/FE-BUDDY/issues/375) · The installer's shortcuts showed a cached 2.x icon

Fixed by Nikolas ([#376](https://github.com/Nikolai558/FE-BUDDY/pull/376)): both shortcuts now use the MSI's icon. Not part of this review; no finding is about what it changed.
