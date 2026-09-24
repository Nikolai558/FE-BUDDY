# Versioning

FE-Buddy follows [Semantic Versioning 2.0.0](https://semver.org/). This page covers the policy
(how to bump a version), where the version lives in FE-Buddy 3.x, how the app and the installer
use it, and what every 3.x release must do so installed 2.x copies upgrade to it.

Every release version looks like this:

```
MAJOR.MINOR.PATCH[-PRERELEASE]
```

For example `3.0.0`, or `3.0.0-beta.2`.

---

## A version number is not a decimal number

**`2.8.10` is greater than `2.8.9`.** Each segment is its own integer counter, compared
numerically: `2.8.8 → 2.8.9 → 2.8.10 → 2.8.11` is normal. Nothing resets or goes backward at
`9 → 10`.

## MAJOR.MINOR.PATCH - when to bump each

- **MAJOR** (`2.9.4 → 3.0.0`) - a breaking or fundamentally incompatible change: a rewrite of
  a core system (the WPF rewrite that is 3.0), output formats that break VRC/vSTARS/vERAM
  compatibility, removing or replacing a major feature. Reset MINOR and PATCH to `0`.
- **MINOR** (`3.0.3 → 3.1.0`) - new, backwards-compatible functionality: new features, new
  conversions, new data sources. Reset PATCH to `0`.
- **PATCH** (`3.0.3 → 3.0.4`) - backwards-compatible bug fixes only.

Unsure between MINOR and PATCH? New capability → MINOR. Fix to something that should already
have worked → PATCH.

## Pre-release tags and channels

A pre-release ships a version to testers before it is the stable release of that number:

```
MAJOR.MINOR.PATCH-alpha.N     early, unstable, in progress; developers only
MAJOR.MINOR.PATCH-beta.N      feature-complete, being tested; willing testers
MAJOR.MINOR.PATCH-rc.N        believed ready to ship ("release candidate")
```

`N` counts from `1`. The `MAJOR.MINOR.PATCH` part is the version you are **aiming** to release
and stays fixed through the whole chain; only the counter moves until the tag is dropped:

```
3.0.0-alpha.1 < 3.0.0-alpha.2 < 3.0.0-beta.1 < 3.0.0-rc.1 < 3.0.0
```

Found a bug in `3.0.0-rc.1`? Ship `3.0.0-rc.2`, not `3.0.1`.

Two consequences of SemVer precedence matter here:

- Any pre-release is **lower** than its final release: `3.0.0-rc.1 < 3.0.0`, always.
- A pre-release of a future version is **higher** than today's stable: `3.0.0-alpha.1 > 2.9.0`.

The tag decides a release's **channel** (`FeBuddy.Versioning.ProductVersion.Channel`):

| Tag | Channel |
|---|---|
| none | Stable |
| `-rc.N` | ReleaseCandidate |
| `-beta.N` | Beta |
| `-alpha.N`, or any other tag (`-dev`, `-preview`, ...) | Alpha |

A user's update channel (Settings > Updates, stored as `General.UpdateChannel`) is the **lowest**
channel they accept: Stable is offered only stable releases, ReleaseCandidate adds `-rc`, Beta
adds `-beta`, Alpha is offered everything. GitHub's "pre-release" checkbox is not consulted - the
tag alone decides, exactly as in FE-Buddy 2.x's updater. (The Settings page does not offer
ReleaseCandidate yet; the channel exists in `FeBuddy.Versioning` and is honoured if stored.)

---

## Where the version lives

**`<Version>` in `FeBuddy/FeBuddy.Wpf/FeBuddy.Wpf.csproj` is the only place anyone bumps it.**
Everything else is derived from it:

| Where | Value | Used by |
|---|---|---|
| csproj `<Version>` | `3.0.0-alpha.1` | the source |
| exe/dll **Product version** (`AssemblyInformationalVersion`) | `3.0.0-alpha.1` | the app (`AppVersion`), `build.ps1`, Explorer > Properties > Details |
| `AssemblyVersion` / `FileVersion` | `3.0.0.0` | nothing - numeric only, the tag is dropped |
| MSI `ProductSemVer` property, then `HKLM\Software\FE-BUDDY\ProductSemVer` | `3.0.0-alpha.1` | the installer's version policy (next install) |
| MSI `Package/@Version` | a counter, e.g. `3.0.4` | Windows Installer only - see [MSI-VERSION-NUMBERING.md](MSI-VERSION-NUMBERING.md) |
| GitHub release tag | `3.0.0-alpha.1` | the version check (the app's, and 2.x's) |

`IncludeSourceRevisionInInformationalVersion` is `false` in the csproj so the Product version
stays exactly `<Version>`, with no `+<commit hash>` appended.

An unreleased build carries `-dev` (`3.0.0-dev`). It outranks every 2.x release, so the version
check reports it as "ahead of the latest release", and `-dev` counts as the Alpha channel.

## How the app uses it

- **The running version** is `FeBuddy.Core.Services.General.AppVersion.Current` - the entry
  assembly's Product version. Never `Assembly.GetName().Version`: it is numeric-only and does not
  compare correctly with a tag.
- **The version check** (`VersionCheck`) reads the repo's last 30 releases, parses each tag as
  strict SemVer (a leading `v` is allowed; anything else is skipped), keeps the releases on the
  user's channel, and compares by SemVer precedence (`FeBuddy.Versioning.ProductVersion`).
- **Installed or dev build** - `InstalledProduct.IsMsiInstalled` compares the running folder with
  the `InstallLocation` the installer recorded under `HKLM\Software\FE-BUDDY`. A copy the MSI did
  not install shows `- DEV` after its version in the title bar, as 2.x does.

## How the installer uses it

`FeBuddy/build.ps1` publishes the app, reads the Product version from the built `FE-BUDDY.dll`,
and builds the MSI with it as `ProductSemVer` (plus the internal counter as `Package/@Version`).
Before installing, the MSI's `EnforceVersionPolicy` custom action compares the installed
`ProductSemVer` (2.x or 3.x) with its own using `FeBuddy.Versioning.UpdatePolicy`:

- Equal or newer → allowed (`2.9.0 → 3.0.0`, `3.0.0-rc.1 → 3.0.0`).
- Older → allowed only when the installed version is a pre-release, i.e. opting out of a
  pre-release back to stable (`3.0.0-alpha.1 → 2.9.0`).
- Stable → older stable is blocked (`3.0.0 → 2.9.0`).

**Rolling back** (the allowed downgrade) needs every file copied even though the installed ones
are newer; otherwise Windows Installer skips them and then deletes them with the old product.
3.x MSIs set `REINSTALLMODE=amus` for this, so rolling back to any 3.x release just works. Rolling
back to a **2.x** release uses 2.x's MSI, which does not set it: run it as
`msiexec /i FE-BUDDY-2.9.0.msi REINSTALLMODE=amus`, or uninstall 3.x first (2.x's "Revert to
Latest Stable" does the latter). A plain `msiexec /i` of the 2.x MSI over 3.x leaves the install
without `FE-BUDDY.exe`.

The rule lives in `FeBuddy.Versioning` (netstandard2.0), shared by the app and by the custom
action (net472, because WiX's custom-action host only loads .NET Framework). Its tests are in
`FeBuddy.UnitTests/Versioning`, and `FeBuddy.Versioning` is part of the CI coverage gate.

---

## 2.x → 3.x upgrades

FE-Buddy 2.9 and later are installed by an MSI and check GitHub for updates themselves. Their
updater cannot be changed, so every 3.x release must fit what it expects:

- **Tag** - strict SemVer, e.g. `3.0.0` or `3.0.0-beta.1` (a leading `v` is fine). 2.x skips any
  tag it cannot parse.
- **Asset** - the release must have the `.msi` attached (`FE-BUDDY-<version>.msi` from
  `build.ps1`). 2.x only offers a release that has one.
- **Channel** - comes from the tag, so 2.x users on Alpha are offered `3.0.0-alpha.N`, and
  Stable users only `3.0.0`. Because `3.0.0` outranks any `2.9.x`, a later 2.x hotfix does not
  hold Stable users back from 3.x.

The 3.x MSI then replaces 2.x in place because it keeps 2.x's `UpgradeCode`, package identity,
`HKLM\Software\FE-BUDDY` registry values, install folder and `FE-BUDDY.exe` name (so 2.x's
shortcuts and the installer's "Launch FE-BUDDY" still point at the right file). None of those
may change.

## Quick reference

- Bug fix → **PATCH** (`3.0.9 → 3.0.10`).
- New feature, nothing breaks → **MINOR**, reset PATCH (`3.0.9 → 3.1.0`).
- Breaking change → **MAJOR**, reset MINOR and PATCH.
- Testing before it is official → `-alpha.N` / `-beta.N` / `-rc.N` on the version you are
  **aiming** to release.
- Bump only `<Version>` in `FeBuddy.Wpf.csproj`.
