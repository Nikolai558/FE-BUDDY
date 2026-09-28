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
| `v3-development` | FE-Buddy 3.x - the repository's default branch, where this code lives. Target pull requests here. CI runs on it. |
| `releases` | What has been released. Only `v3-development` is merged into it, by a release pull request - see [Releasing](RELEASING.md). |
| `development` | FE-Buddy 2.x. 2.9.3 was its last planned release. |

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
every uncovered line. `FeBuddy.Wpf`, the harness and the installer projects are not measured -
though the map's logic in `FeBuddy.Wpf` is tested (`FeBuddy.UnitTests/Wpf`).

The Windows Credential Manager tests are reported as skipped in a session with no Windows sign-in
behind it (a service account, some build agents), which has no Credential Manager.

## Code standards

One `.editorconfig` (`FeBuddy/.editorconfig`) covers every project. Before you push, these should
report nothing (`FeBuddy.Harness` is not yet held to the rest of the standard - see
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
it to `FeBuddy/releases/FE-BUDDY-Setup.msi` (always that name, whatever the version).
`publish/` and `releases/` are recreated on every run and are gitignored.

Every run advances `FeBuddy/FeBuddy.Installer/installer-version-counter.json`. Discard that change
after a local build - only the release workflow commits it. Why the counter exists:
[MSI version numbering](MSI-VERSION-NUMBERING.md).

## Releasing

See [Releasing](RELEASING.md): add a `ChangeLog.md` entry with every user-facing change, and to
release, bump `<Version>` and open a pull request from `v3-development` into `releases`. GitHub
Actions checks it, builds the MSI and drafts the release to publish.

Optionally post to News as well (`FeBuddy/FeBuddy.Core/News.md` - the format is at the top of the
file); the app reads it from `v3-development` on GitHub.

## Continuous integration

`.github/workflows/ci.yml` runs on every push and pull request to `v3-development` (and on pull
requests into `releases`), in three jobs: **Build** (the solution, Release), **Test** (the
coverage gate; the report is a run artifact) and **Installer** (`build.ps1`; the MSI is a run
artifact, kept one day, for testing only). CodeQL runs separately. Pull requests into `releases`
also run the release checks - see [Releasing](RELEASING.md).

A pull request into `v3-development` that changes only Markdown (`.md`) files outside `FeBuddy/`
skips Build, Test and Installer. They show as skipped, which counts as passed, so the pull request
can still merge. `News.md` is inside `FeBuddy/` and still runs everything, because it is built into
the app and the tests read it. CodeQL runs on every pull request regardless: the code scanning
rule blocks a merge until CodeQL has results.

## Handy extras

- **`DEV-CleanBuild.bat`** (repository root) empties every project's `bin\` folder, for when a
  build gets confused.
- **A GitHub token** (optional) - in Settings ▸ FE-Buddy's GitHub Requests, choose "Use a GitHub
  token" and pick or add one. Update checks, News and update downloads are then sent with it (to
  get past GitHub's 60-requests-an-hour limit), and tried once more without it if that fails in
  any way. Nobody needs it for normal use, and the
  `FEBUDDY_GITHUB_TOKEN` environment variable is never read (FE-Buddy only tells a 2.x user once
  that it is still set). See [Credentials](Credentials.md).
