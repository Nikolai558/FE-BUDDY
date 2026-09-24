# Why Windows Settings shows a different FE-Buddy version number

In **Settings → Apps → Installed apps → FE-BUDDY**, the version shown does **not** match the real
FE-Buddy version (the one on GitHub Releases and in the app). This is intentional.

## Where to find the real version

- **In the app** - the title bar.
- **File Explorer** - right-click `FE-BUDDY.exe` → **Properties** → **Details** →
  **Product version**, including any `-alpha`/`-beta`/`-rc` tag (e.g. `3.0.0-alpha.1`).

## Why Settings shows something else

FE-Buddy follows [Semantic Versioning](VERSIONING.md), which allows versions like
`3.0.0-alpha.1`. Windows Installer's own version field, `ProductVersion`, holds only three plain
numbers (`major.minor.build`); it cannot store a pre-release tag, and Settings always displays
that field - there is no supported way to show a different string.

So the installer keeps two numbers:

1. **The real version** (e.g. `3.0.0-alpha.1`) - used by the app, Explorer's Properties dialog,
   GitHub Releases and all of FE-Buddy's update logic. It is passed to the MSI as
   `ProductSemVer` and recorded at `HKLM\Software\FE-BUDDY\ProductSemVer`.
2. **An internal counter** (what Settings shows) - a disposable number whose only job is to be
   different from whatever is installed, so Windows Installer agrees to replace the files. Its
   major number matches the real major (a `3.x.x` in Settings is a 3.x install); minor.build is
   a running count of every build under that major, bumped by one each time, with no relation to
   the real minor/patch/pre-release. It restarts at `0.0` when the real major changes.

The counter carries no ordering meaning. The MSI allows "downgrades" of it
(`MajorUpgrade AllowDowngrades="yes"`), and the real upgrade/downgrade rule is enforced on the
real version by the `EnforceVersionPolicy` custom action - see [VERSIONING.md](VERSIONING.md).

## Where this lives in the repo

- The counter is computed by `FeBuddy/FeBuddy.Installer/Get-InstallerVersion.ps1`, called from
  `FeBuddy/build.ps1`. It is stored in `FeBuddy/FeBuddy.Installer/installer-version-counter.json`,
  which the build advances on every run - **nobody hand-edits it**. Commit the advanced file with
  each release so the next release gets a new number.
- The real version is `<Version>` in `FeBuddy/FeBuddy.Wpf/FeBuddy.Wpf.csproj`, the single place
  anyone bumps it. `build.ps1` reads it back from the built `FE-BUDDY.dll`'s Product version.
- The upgrade/downgrade rule is `FeBuddy/FeBuddy.Versioning/UpdatePolicy.cs`, run at install time
  by `FeBuddy/FeBuddy.Installer.CustomActions/VersionPolicyActions.cs`.
