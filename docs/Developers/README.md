# Developer guide

Start here. This page explains FE-Buddy's code in a few minutes, then how to build, run, test and
package it. Paths are relative to the repository root; the solution is `FeBuddy/FeBuddy.sln`.

## How it works, in short

1. **At launch** FE-Buddy downloads the FAA's NASR data - CSV files of every airport, airway, fix
   and procedure, published every 28 days - for the previous, current and next AIRAC cycles, and
   reads it into memory.
2. **The user picks** sub-services (Airports, Airways, …) on the AIRAC Service screen and sets their
   options. Each tab turns its options into a flat `key = value` dictionary: the **settings block**.
3. **The library builds.** For each sub-service it parses the block, turns FAA rows into objects (an
   airway with its waypoints in order), and writes GeoJSON and alias files, every one ready for vNAS.
   The alias files are combined, with the facility's own, into one `Combined_Alias.txt`.
4. **A result comes back** - files written, warnings, what was skipped and why - and the app shows
   it on the Review tab.

The library, `FeBuddy.Core`, never shows a window or asks the user anything, so the app, a console
harness and the tests all drive it the same way.

## The projects

| Project | What it is |
|---|---|
| `FeBuddy.Core` | The library: FAA data, the AIRAC sub-services, the file conversions, GeoJSON and alias writing, settings, credentials, logging, updates. |
| `FeBuddy.Wpf` | The app, `FE-BUDDY.exe`: MVVM screens over Core. |
| `FeBuddy.Versioning` | The version number and the upgrade rule, shared by Core and the installer (netstandard2.0). |
| `FeBuddy.Installer` | The MSI (WiX). |
| `FeBuddy.Installer.CustomActions` | The MSI's code: enforce the upgrade rule, and remove saved credentials on uninstall (net472). |
| `FeBuddy.Harness` | A console app that runs the services with settings written in code. |
| `FeBuddy.UnitTests` | xUnit tests for Core, Versioning and the app's view-models and controls. |

## Branches

| Branch | Holds |
|---|---|
| `v3-development` | FE-Buddy 3.x, the default branch. Pull requests go here. |
| `releases` | What has been released. Only `v3-development` is merged into it - see [Releasing](RELEASING.md). |
| `development` | FE-Buddy 2.x. 2.9.3 was its last planned release. |

## Build and run

You need Windows and the **.NET 10 SDK**; NuGet restores everything else, including WiX.

```bash
dotnet build FeBuddy/FeBuddy.sln
```

```bash
dotnet run --project FeBuddy/FeBuddy.Wpf
```

A development build uses `%APPDATA%\FE-Buddy` like an installed copy: `UserConfig.json`, the FAA
data in `AiracCycles`, and the logs.

**Developer mode** is `DevModeEnabled` in `FeBuddy.Wpf/App.xaml.cs`, a code constant. Turn it on
for a troubleshooting build: GeoJSON is pretty printed and Debug log entries are kept. Keep it off
in a release; the release checks fail if it's on.

## The harness

`FeBuddy.Harness` runs services from the console with settings written in code - the quickest loop
while working on the library.

1. Point `NasrSourceDirectory` in `FeBuddy.Harness/HarnessSettings.cs` at an unzipped NASR CSV set
   (`https://nfdc.faa.gov/webContent/28DaySub/extra/<date>_CSV.zip`, or a cycle folder copied out of
   `%APPDATA%\FE-Buddy\AiracCycles`), and set `OutputDirectory`.
2. Edit the settings blocks there ([Settings reference](Settings-Reference.md)), and comment or
   uncomment runners in `Program.cs`.
3. Run it:

```bash
dotnet run --project FeBuddy/FeBuddy.Harness
```

## Checks before you push

```bash
dotnet build FeBuddy/FeBuddy.sln
```

```bash
dotnet format whitespace FeBuddy/FeBuddy.sln --verify-no-changes
```

```bash
dotnet format style FeBuddy/FeBuddy.sln --severity info --verify-no-changes
```

```bash
./FeBuddy/coverage.ps1 -MinimumLineCoverage 95 -MinimumBranchCoverage 90
```

- **Zero warnings.** Every public member needs XML docs; a missing one is a warning.
- **The format checks report no changes.** One `.editorconfig` (`FeBuddy/.editorconfig`) covers
  every project: tabs, file-scoped namespaces, `_camelCase` private fields, snake_case test names.
- **Coverage** of `FeBuddy.Core` and `FeBuddy.Versioning` stays at 95% of lines and 90% of
  branches - CI fails below that. `-Open` opens the report, which shows every uncovered line.
- **UTF-8 without a byte-order mark.** A test fails on any file that has one (Visual Studio sometimes
  adds one to a `.csproj`).

`dotnet test FeBuddy/FeBuddy.UnitTests` runs the tests alone. The Credential Manager tests are
skipped in a session with no Windows sign-in behind it, such as some build agents.

## Building the installer

```bash
./FeBuddy/build.ps1
```

(or double-click `FeBuddy/build.cmd`). It publishes the app self-contained for win-x64, builds the
MSI and copies it to `FeBuddy/releases/FE-BUDDY-Setup.msi`. It also advances
`FeBuddy/FeBuddy.Installer/installer-version-counter.json`: discard that change, since only the
release workflow commits it ([why](VERSIONING.md#the-msis-own-version-number)).

## Continuous integration

`.github/workflows/ci.yml` runs on pushes and pull requests to `v3-development` and pull requests
into `releases`: **Build**, **Test** (the coverage gate) and **Installer** (the MSI, kept a day for
testing). CodeQL runs separately, on pull requests and pushes to both branches, and weekly. A pull
request into `v3-development` that changes only `.md` files outside `FeBuddy/` skips Build, Test
and Installer - except the root `News.md`, which is built into the app.

## Odds and ends

- **`DEV-CleanBuild.bat`** (repository root) empties every project's `bin\` folder, for when a build
  gets confused.
- **News** is `News.md` at the repository root (its format is at the top of the file); the app reads
  it from `v3-development`. `FeBuddy/FeBuddy.Core/News.md` is not News: it is a final notice for
  3.0.0-alpha.1, which reads that path.
- **A change-log entry** goes in `ChangeLog.md` with every change a user would notice - see
  [Releasing](RELEASING.md#writing-a-change-log-entry).

## Where next

| To… | Read |
|---|---|
| understand launch, the data pipeline and a run | [Architecture](Architecture.md) |
| find or add code | [Code structure](Code-Structure.md) |
| look up a settings key | [Settings reference](Settings-Reference.md) |
| use a saved password or token | [Credentials](Credentials.md) |
| make a release | [Releasing](RELEASING.md) and [Versioning](VERSIONING.md) |
| pick something up | [TODO](TODO.md) |
