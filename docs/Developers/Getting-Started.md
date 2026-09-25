# Developer getting started

Everything you need to build, run, test and package FE-Buddy 3.0. Paths are relative to the
repository root; the solution lives in `FeBuddy/`.

## Prerequisites

- **Windows** (the app is WPF, and the installer is an MSI).
- The **.NET 10 SDK**. That is all the build needs: NuGet restores everything else, including the
  WiX toolset for the installer.
- An editor: Visual Studio, Rider or VS Code all work with `FeBuddy/FeBuddy.sln`.

## Branches

| Branch | Holds |
|---|---|
| `v3-development` | FE-Buddy 3.x - where this code lives. Target pull requests here. CI runs on it. |
| `development` | FE-Buddy 2.x (the repository's default branch until 3.0 ships). |

## Build and run

```bash
dotnet build FeBuddy/FeBuddy.sln
```

```bash
dotnet run --project FeBuddy/FeBuddy.Wpf
```

The app keeps its settings in `%APPDATA%\FE-Buddy\UserConfig.json` and its downloaded FAA data in
`%APPDATA%\FE-Buddy\AiracCycles`, the same as an installed copy.

**Developer mode.** `DevModeEnabled` in `FeBuddy.Wpf/App.xaml.cs` is a code constant, never a user
setting. Turn it on for a troubleshooting build: every GeoJSON file is pretty printed and
Debug-level log entries are recorded. Keep it off in anything you release.

## The harness

`FeBuddy.Harness` runs the services from the console with no GUI, using settings you write in
code - the quickest loop while working on the library.

1. Download and unzip a NASR CSV set (`https://nfdc.faa.gov/webContent/28DaySub/extra/<date>_CSV.zip`,
   or copy a cycle folder out of `%APPDATA%\FE-Buddy\AiracCycles`).
2. Set `NasrSourceDirectory` and `OutputDirectory` in `FeBuddy.Harness/HarnessSettings.cs`, and
   edit the settings blocks there.
3. Run it:

```bash
dotnet run --project FeBuddy/FeBuddy.Harness
```

The settings blocks are the same `key = value` dictionaries the app builds - see
[Settings blocks](Settings-Blocks.md).

## Tests and coverage

```bash
dotnet test FeBuddy/FeBuddy.UnitTests
```

The coverage gate, as CI runs it (it fails below 95% line or 90% branch coverage of `FeBuddy.Core`
and `FeBuddy.Versioning`):

```bash
./FeBuddy/coverage.ps1 -MinimumLineCoverage 95 -MinimumBranchCoverage 90
```

Add `-Open` to open the HTML report (`FeBuddy/TestResults/CoverageReport/index.html`), which shows
every uncovered line. `FeBuddy.Wpf`, the harness and the installer projects are not measured.

## Code standards

One `.editorconfig` (`FeBuddy/.editorconfig`) covers every project. Before you push, these should
report nothing (except in `FeBuddy.Harness`, which is not held to the standard yet - see
[TODO](TODO.md)):

```bash
dotnet format style FeBuddy/FeBuddy.sln --severity info --verify-no-changes
```

```bash
dotnet format whitespace FeBuddy/FeBuddy.sln --verify-no-changes
```

The build must also have **zero warnings**: every public member needs XML docs, and a missing one
is a warning. The rules themselves (layers, the Models/ rule, naming) are in
[FeBuddy.Core structure](FeBuddy.Core-Structure.md#standards-and-checks).

## Build the installer

```bash
./FeBuddy/build.ps1
```

(or double-click `FeBuddy/build.cmd`). It publishes the app self-contained for win-x64, reads the
version from the built `FE-BUDDY.dll`, advances the installer counter, builds the MSI and copies
it to `FeBuddy/releases/FE-BUDDY-<version>.msi`. `publish/` and `releases/` are recreated on every
run and are gitignored.

Every run advances `FeBuddy/FeBuddy.Installer/installer-version-counter.json`. Commit that change
with a release; discard it after a local test build. Why the counter exists:
[MSI version numbering](MSI-VERSION-NUMBERING.md).

## Releasing

1. Bump `<Version>` in `FeBuddy/FeBuddy.Wpf/FeBuddy.Wpf.csproj` - the only place the version
   lives. The rules for MAJOR / MINOR / PATCH and `-alpha` / `-beta` / `-rc` are in
   [Versioning](VERSIONING.md).
2. Run `build.ps1` and test the MSI (install over the previous release too).
3. Commit the version bump and the advanced installer counter.
4. Create a GitHub release tagged with the version (e.g. `3.0.0-beta.1`, marked pre-release when
   it has a tag) and attach the MSI. There is no release workflow yet; this is by hand.
5. Optionally post to News (`FeBuddy/FeBuddy.Core/News.md` - the format is at the top of the
   file); the app reads it from `v3-development` on GitHub.

## Continuous integration

`.github/workflows/ci.yml` runs on every push and pull request to `v3-development`, in three jobs:
**Build** (the solution, Release), **Test** (the coverage gate; the report is a run artifact) and
**Installer** (`build.ps1`; the MSI is a run artifact, kept one day, for testing only). CodeQL runs
separately.

## Handy extras

- **`DEV-CleanBuild.bat`** (repository root) empties every project's `bin\` folder, for when a
  build gets confused.
- **`FEBUDDY_GITHUB_TOKEN`** (optional environment variable) - a GitHub token the version check,
  News and update download fall back to if an anonymous request fails (for example after GitHub's
  60-requests-an-hour limit). Nobody needs it for normal use.
