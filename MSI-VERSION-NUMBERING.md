# Why Windows Settings shows a different FE-BUDDY version number

If you go to **Settings → Apps → Installed apps → FE-BUDDY**, the version number shown
there will **not** match the real FE-BUDDY version (the one in the ChangeLog, on GitHub
Releases, or in the app itself). This is intentional. This doc explains why, and where to
find the real version instead.

## Where to find the real version

- **In the app itself** - the real version is always shown correctly there.
- **File Explorer** - right-click `FE-BUDDY.exe` → **Properties** → **Details** tab →
  **Product version**. This always shows the real version, including any `-alpha`/`-beta`/
  `-rc` tag (e.g. `2.8.4-alpha.1`).

## Why Settings shows something else

FE-BUDDY follows [Semantic Versioning](VERSIONING.md), which allows version strings like
`2.8.4-alpha.1`. Windows Installer (the `.msi` engine, `msiexec`) was designed decades
before SemVer existed, and its internal version field - `ProductVersion` - can **only**
hold three plain numbers (`major.minor.build`, e.g. `2.8.4`). It cannot store a `-alpha.1`
style tag at all, and there is no supported way to make Windows Settings display a
different, friendlier string in its place - we checked; the "Add/Remove Programs" display
version is hard-wired to come directly from `ProductVersion`, with no override.

So instead of losing the ability to ship alpha/beta/rc builds (or silently mangling the
real version to fit), FE-BUDDY's installer keeps two separate numbers:

1. **The real version** (e.g. `2.8.4-alpha.1`) - used everywhere that matters: the app
   itself, File Explorer's Properties dialog, GitHub Releases, the ChangeLog, and all of
   FE-BUDDY's own update-checking logic. This is never encoded, truncated, or guessed at -
   it's the exact string from the release.

2. **An internal MSI counter** (what Settings shows) - a disposable number with no
   meaning beyond "is this build different from what's currently installed." Its major
   number always matches the real version's major number (so at least the big picture is
   right - a "3.x.x" in Settings does mean a real major-3 install), but the minor/build
   portion is just a running count of every release ever built under that major version,
   bumped by one each time, with no relationship to the real minor/patch/pre-release
   numbers. It resets to `0.0` whenever the real major version changes.

This split is deliberate, not a workaround we didn't get around to fixing: it lets
FE-BUDDY support proper alpha/beta/rc pre-release builds and a real "roll back to stable"
path, which Windows Installer's own numeric-only version field simply cannot represent.

## Where this lives in the repo

- The internal counter is computed by
  [`FE-BUDDY.Installer/Get-InstallerVersion.ps1`](FE-BUDDY.Installer/Get-InstallerVersion.ps1),
  called from [`build.ps1`](build.ps1). **Nobody should hand-edit
  `FE-BUDDY.Installer/installer-version-counter.json`** - the build script owns it
  entirely and advances it automatically on every build.
- The real version is what's set in
  [`FeBuddyWinFormUI/FeBuddyWinFormUI.csproj`](FeBuddyWinFormUI/FeBuddyWinFormUI.csproj)'s
  `<Version>` - the single place anyone bumps a version number. Everything else (the
  internal MSI counter, the registry-stored install record, the version-policy check
  described below) is derived from that one value automatically.
- The actual upgrade/downgrade rule enforced at install time (block stable → older
  stable; allow rolling back off a pre-release to the last stable) is implemented against
  the *real* version, not the internal counter - see
  [`FeBuddy.Versioning/UpdatePolicy.cs`](FeBuddy.Versioning/UpdatePolicy.cs) and
  [`FE-BUDDY.Installer.CustomActions/VersionPolicyActions.cs`](FE-BUDDY.Installer.CustomActions/VersionPolicyActions.cs).
