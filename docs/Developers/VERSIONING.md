# Versioning

FE-Buddy follows [Semantic Versioning 2.0.0](https://semver.org/): `MAJOR.MINOR.PATCH[-PRERELEASE]`,
for example `3.0.0` or `3.0.0-beta.2`. Each part is its own counter, so `2.8.10` comes after `2.8.9`.

**Contents:** [Which number to bump](#which-number-to-bump) · [Pre-releases and channels](#pre-releases-and-channels) ·
[Where the version lives](#where-the-version-lives) · [How the app uses it](#how-the-app-uses-it) ·
[How the installer uses it](#how-the-installer-uses-it) · [The MSI's own version number](#the-msis-own-version-number) ·
[2.x to 3.x upgrades](#2x-to-3x-upgrades)

## Which number to bump

- **MAJOR** (`2.9.4 → 3.0.0`) - a breaking change: a rewrite, output that breaks compatibility, a
  major feature removed. MINOR and PATCH go back to `0`.
- **MINOR** (`3.0.3 → 3.1.0`) - new features that break nothing. PATCH goes back to `0`.
- **PATCH** (`3.0.3 → 3.0.4`) - bug fixes only.

Each release is exactly one step after the last (`3.0.0-alpha.1` → `alpha.2`, `beta.1`, `rc.1` or
`3.0.0`); the release pre-flight enforces it ([Releasing](RELEASING.md)).

## Pre-releases and channels

```
3.0.0-alpha.N    early and unstable; developers only
3.0.0-beta.N     every planned feature in, being tested
3.0.0-rc.N       believed ready to ship
```

`N` starts at `1`. The `3.0.0` part is the version being aimed at and stays the same through the
chain: `3.0.0-alpha.1 < 3.0.0-alpha.2 < 3.0.0-beta.1 < 3.0.0-rc.1 < 3.0.0`. A bug in `3.0.0-rc.1`
means `3.0.0-rc.2`, not `3.0.1`. Any pre-release is lower than its release, and higher than every
earlier version (`3.0.0-alpha.1 > 2.9.0`).

The tag decides a release's **channel** (`ProductVersion.Channel`):

| Tag | Channel |
|---|---|
| none | Stable |
| `-rc.N` | ReleaseCandidate |
| `-beta.N` | Beta |
| `-alpha.N`, or any other tag (`-dev`, `-preview`, …) | Alpha |

The user's channel (Settings ▸ Updates, `General.UpdateChannel`) is the least stable they accept:
Stable gets only stable releases, Release Candidate adds `-rc`, Beta adds `-beta`, Alpha gets
everything. GitHub's pre-release checkbox is never consulted. The first launch saves the running
build's channel (`UpdateChannelSetting.SaveDefaultIfUnset`): an alpha is on Alpha, a beta on Beta,
an rc on Release Candidate, a stable release on Stable. A `-dev` build saves nothing. After that only
Settings changes it, so an update never does: an alpha tester stays on Alpha through 3.0.0, and
someone on Beta who installs an alpha by hand stays on Beta.

## Where the version lives

**`<Version>` in `FeBuddy/FeBuddy.Wpf/FeBuddy.Wpf.csproj` is the only place anyone bumps it.**
Everything else comes from it:

| Where | Example | Used by |
|---|---|---|
| csproj `<Version>` | `3.0.0-alpha.1` | the source |
| exe/dll **Product version** (`AssemblyInformationalVersion`) | `3.0.0-alpha.1` | the app (`AppVersion`), `build.ps1`, Explorer ▸ Properties ▸ Details |
| `AssemblyVersion` / `FileVersion` | `3.0.0.0` | nothing - numbers only |
| MSI `ProductSemVer`, then `HKLM\Software\FE-BUDDY\ProductSemVer` | `3.0.0-alpha.1` | the installer's version rule at the next install |
| MSI `Package/@Version` | a counter, e.g. `3.0.4` | Windows Installer only ([below](#the-msis-own-version-number)) |
| GitHub release tag | `3.0.0-alpha.1` | the version check (3.x's, and 2.x's) |

`IncludeSourceRevisionInInformationalVersion` is `false`, so the Product version is exactly
`<Version>`, with no `+<commit>` on the end. Between releases it stays at the last release's version
(step 2 of [Releasing](RELEASING.md#part-1---get-v3-development-ready) changes it). A `-dev` version
(`3.0.0-dev`) is ahead of every 2.x release, and its tag counts as Alpha, though its default update
channel is Stable.

## How the app uses it

- **The running version** is `AppVersion.Current`, the Product version - never
  `Assembly.GetName().Version`, which drops the tag.
- **The version check** (`VersionCheck`) reads the repository's last 100 releases, parses each tag
  as strict SemVer (a leading `v` is fine; anything else is skipped), keeps those on the user's
  channel, and compares by SemVer (`ProductVersion`).
- **Update now** downloads the release's `.msi` to `%TEMP%\FE-Buddy\Updates` (`UpdateInstaller`),
  runs `msiexec /i <msi> REINSTALLMODE=amus` elevated, and closes FE-Buddy; the MSI relaunches it
  from its last page. It warns first if closing would lose a run or unsaved edits. A development
  build or a release with no `.msi` opens the release page instead; after a failed download,
  **Open release page** is offered.
- **Going back.** Saving a more stable channel in Settings while running a pre-release offers that
  channel's latest release, which is older (`VersionCheckResult.CanGoBack`); **Go back now**
  installs it the same way. Only from Settings, never at launch.
- **Installed or development build.** `InstalledProduct.IsMsiInstalled` compares the running folder
  with the `InstallLocation` the installer recorded under `HKLM\Software\FE-BUDDY`. A copy the MSI
  didn't install shows `- DEV` after its version.

## How the installer uses it

`FeBuddy/build.ps1` publishes the app, reads the Product version back from the built `FE-BUDDY.dll`,
and builds the MSI with it as `ProductSemVer`. Before installing, the MSI's `EnforceVersionPolicy`
custom action compares it with the installed `ProductSemVer` (2.x or 3.x) using `UpdatePolicy`:

- **Same or newer:** allowed (`2.9.0 → 3.0.0`, `3.0.0-rc.1 → 3.0.0`).
- **Older, from a pre-release:** allowed - leaving a pre-release for stable
  (`3.0.0-alpha.1 → 2.9.0`).
- **Older stable over stable:** blocked (`3.0.0 → 2.9.0`).

**Rolling back** needs every file copied even though the installed ones are newer, or Windows
Installer skips them and then removes them with the old product. 3.x MSIs set `REINSTALLMODE=amus`
for this. A 2.x MSI doesn't, so FE-Buddy's own **Go back now** passes it on the command line. By
hand, run `msiexec /i FE-BUDDY-2.9.0.msi REINSTALLMODE=amus`, or uninstall 3.x first; a plain
`msiexec /i` of a 2.x MSI over 3.x leaves no `FE-BUDDY.exe`.

The rule is in `FeBuddy.Versioning` (netstandard2.0), shared by the app and the custom action
(net472, the only kind WiX's custom-action host loads). Its tests are in
`FeBuddy.UnitTests/Versioning`, and it is part of the coverage gate.

## The MSI's own version number

Windows Settings ▸ Apps shows a different version for FE-Buddy than the real one, on purpose.
Windows Installer's `ProductVersion` holds only three numbers, so it can't store
`3.0.0-alpha.1` - and Settings always shows that field. So the MSI carries two numbers:

1. **The real version**, passed as `ProductSemVer`, used by the app, Explorer's Properties, GitHub
   and all of FE-Buddy's update logic.
2. **A counter** as `Package/@Version`, whose only job is to differ from what is installed so
   Windows Installer agrees to replace it. Its major matches the real major (a `3.x.x` in Settings
   is a 3.x install); minor.build count every build under that major and restart at `0.0` with a new
   major. It means nothing else.

The counter carries no order: the MSI allows "downgrades" of it (`MajorUpgrade AllowDowngrades="yes"`),
and the real rule is the custom action's, above.

`FeBuddy/FeBuddy.Installer/Get-InstallerVersion.ps1` (called by `build.ps1`) works it out and
advances `installer-version-counter.json` on every build. **Nobody edits that file by hand**:
discard the change a local build makes. The release workflow commits it to `v3-development` after
each release build (`releases` gets it with the next release).

## 2.x to 3.x upgrades

FE-Buddy 2.9 and later install from an MSI and check GitHub for updates. Their updater can't change,
so every 3.x release must fit it:

- **Tag** - strict SemVer (a leading `v` is fine); 2.x skips any tag it can't parse.
- **Asset** - an `.msi` attached (`FE-BUDDY-Setup.msi`); 2.x offers only a release that has one.
- **Channel** - from the tag, so 2.x users on Alpha get `3.0.0-alpha.N` and Stable users only
  `3.0.0`, which outranks any later `2.9.x`.

The 3.x MSI replaces 2.x in place because it keeps 2.x's `UpgradeCode`, package identity,
`HKLM\Software\FE-BUDDY` values, install folder and `FE-BUDDY.exe` name, so 2.x's shortcuts still
work. None of those may change.
