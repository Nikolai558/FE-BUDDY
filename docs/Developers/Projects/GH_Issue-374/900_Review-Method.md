# 900 · How the review was done

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

## Method

The repository was reviewed at [`dc08390`](https://github.com/Nikolai558/FE-BUDDY/tree/dc08390d05e3ce3f2be53b3106e9c5854380b236) (3.0.0-beta.12). It was split into 25 slices: Core by feature area, the GUI by layer, the tests, the installer/Harness/CI, and the docs. An AI reviewer (Claude) read every file in its slice in full. A separate pass (U1) followed UI-thread work across the whole GUI, into the Core methods it calls.

- Every "unused" claim was checked with a whole-repository search: XAML bindings, reflection, tests, the installer and CI included.
- Every finding cites the lines it's about, so each can be checked by hand.
- Every Fix-first bug, and a sample of the rest, was checked against the code again afterwards.
- Findings reported by two reviewers were merged; each file lists the duplicate IDs at the end.
- Kyle's decisions D1 to D7 were applied to the proposals before publishing.

## Limits

Nothing was built or run: the review ran on Linux, without .NET. Timings in the GUI-lag findings are estimates, and test pass/fail claims come from reading the code. Treat a finding as a lead to check, not a verdict.

## Slices

| Prefix | What it read | Findings |
|---|---|---:|
| C1 | Core: Application/Airac root and models, Launch, Settings, News, Updates | 37 |
| C2 | Core: Airports, Airways, Arrivals, Departures | 32 |
| C3 | Core: ARTCC Boundaries, Concatenate Aliases, Fixes, NAVAIDs, Telephony, Wx Stations | 27 |
| C4 | Core: Procedures (with Chart Recall) and the Alias Guide | 27 |
| C5 | Core: Conversions (DAT, SCT, ERAM), the DAT/SCT/ERAM readers, Domain | 28 |
| C6 | Core: Configuration (settings), Credentials, GitHub, FileSystem, Logging, Http | 37 |
| C7 | Core: d-TPP, GeoJSON, Markdown, the NASR download and cache, Telephony and Wx data, Platform, the csproj | 29 |
| C8 | Core: NASR models and parsers (docs only: the code stays) | 26 |
| W1 | GUI: the Procedures, Telephony, Add Custom Aliases, Fixes, Airways and Departures view-models | 29 |
| W2 | GUI: the AIRAC Service screen and its other tabs, the duplicate-alias review, File Conversions | 38 |
| W3 | GUI: the other view-models in `ViewModels/` (Dashboard, Map, Settings, Shell, Update, Reset and more) | 39 |
| W4 | GUI: `ViewModels/ServiceTabs` and `ViewModels/Models` | 37 |
| W5 | GUI: Views (XAML and code-behind); every binding checked by script | 36 |
| W6 | GUI: Controls, Behaviors, Converters, Mvvm, Map | 29 |
| W7 | GUI: App, csproj, manifest, Assets, Shell, Theme (every theme key checked) | 28 |
| U1 | GUI: a sweep of the whole GUI for UI-thread work and business rules, following each call into Core | 20 |
| O1 | Installer, setup window, custom actions, Harness, Versioning, CI, `.github`, build scripts, root files | 44 |
| T1a | Tests: Airways, Arrivals, Departures, Procedures | 37 |
| T1b | Tests: the AIRAC root and models, Airports, Telephony, NAVAIDs, Fixes, ARTCC Boundaries, Wx Stations, Concatenate Aliases | 35 |
| T1c | Tests: Alias Guide, Conversions, Launch, News, Settings, Updates, Domain | 26 |
| T2 | Tests: Infrastructure, Repository, TestSupport, Versioning, the csproj, fixtures | 34 |
| T3 | Tests: everything under `Wpf/` | 35 |
| D1a | `docs/Developers/Settings-Reference.md` | 18 |
| D1b | `README.md`, `docs/README.md`, `SECURITY.md` and the other developer docs | 24 |
| D2 | The user manuals (`docs/Users`) | 69 |
| N1 | Code merged after the review: [#376](https://github.com/Nikolai558/FE-BUDDY/pull/376) ([#375](https://github.com/Nikolai558/FE-BUDDY/issues/375)) and [#377](https://github.com/Nikolai558/FE-BUDDY/pull/377) (the [C1-5](100_Bugs.md#c1-5) fix) | 3 |

Counts include duplicates. D2's count is the sections of the user manuals it reviewed.

## Re-checked after the review

Two pull requests merged into `v3-B13-Dev` after the review, and are in `e15a634` ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)'s working base on `v3-B13-Dev-Kyle`): [#376](https://github.com/Nikolai558/FE-BUDDY/pull/376) ([#375](https://github.com/Nikolai558/FE-BUDDY/issues/375), the shortcut icon) and [#377](https://github.com/Nikolai558/FE-BUDDY/pull/377) (the C1-5 fix). They changed 12 files. Every finding that cites one of them was checked again at `e15a634`:

- [C1-5](100_Bugs.md#c1-5): fixed.
- [O1-21](400_Installer-Harness-CI.md#o1-21): partly fixed (its Status says what's left).
- [C1-17](130_Old-Version-Code.md#c1-17), [C1-26](200_Comments-Core.md#c1-26), [C7-3](200_Comments-Core.md#c7-3), [O1-32](400_Installer-Harness-CI.md#o1-32), [W3-14](120_Logic-In-GUI.md#w3-14), [W3-31](220_Comments-GUI.md#w3-31), [W3-39](220_Comments-GUI.md#w3-39): still apply. Some lines moved; the links still show the reviewed lines at `dc08390`.
- Findings that only mention one of the 12 files in passing were unaffected.

The new code ([`UnelevatedLauncher.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/e15a634aa6915245e558875cd249bccdb8455480/FeBuddy/FeBuddy.Installer.UI/Setup/UnelevatedLauncher.cs), [`LaunchActions.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/e15a634aa6915245e558875cd249bccdb8455480/FeBuddy/FeBuddy.Installer.CustomActions/LaunchActions.cs) and the edited comments and docs) was reviewed the same way: [N1-1](500_Developer-Docs.md#n1-1), [N1-2](400_Installer-Harness-CI.md#n1-2) and [N1-3](610_On-Screen-Text.md#n1-3). Its comments are accurate.

On 2026-10-19, once Nikolas had pushed his last work (`4893e32`), the code [S2](020_Work-Plan.md#s2) to [S5](020_Work-Plan.md#s5) added was reviewed the same way, commit by commit, in five parts: AIRAC Service and Procedure Charts; the GUI-lag work on the other pages; Settings, profiles and saves; the code moved into Core; and the update watcher, the setup window and the build counter. A second pass checked each finding in the code. 39 findings: [N1-5](100_Bugs.md#n1-5) to [N1-43](500_Developer-Docs.md#n1-43), fixed in [S14](020_Work-Plan.md#s14). Their links show the code at `4893e32`.

## Checked and fine

Not everything is a finding. These match their docs:

- Theme resources: every key is used.
- The GeoJSON rules: rounding, layout, isDefaults, symbol grouping.
- `Credentials.md`, apart from one buffer that isn't cleared ([C6-23](100_Bugs.md#c6-23)).
- The NASR group plumbing; every test fixture file is used.
- The launch steps and readiness table (apart from [C1-1](100_Bugs.md#c1-1)).
- Every XAML binding resolves to a real property.
- `Settings-Reference.md` lists every settings key the parsers read, with the right name, values and default.
- The duplicate report rules.
- The dirty-tracking snapshot, which is cheap.
- `README.md` and `docs/README.md`.
