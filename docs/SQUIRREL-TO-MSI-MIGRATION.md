# Squirrel → MSI Migration Plan

FE-BUDDY is moving from Clowd.Squirrel (per-user auto-updater) to the new
WiX/MSI-based installer. This doc captures the plan and the constraints
behind it, so the reasoning isn't lost between now and whenever each step
actually gets built. See also [VERSIONING.md](VERSIONING.md) (semver policy)
and [MSI-VERSION-NUMBERING.md](MSI-VERSION-NUMBERING.md) (why the MSI's
internal version number doesn't match the real one).

**Scope:** this migration is a **2.x.x-line concern only**. v3.0 is a
complete rewrite (new WPF UI, per [ROADMAP.md](ROADMAP.md)) and will ship
with no Squirrel code or detection logic at all - by the time it exists,
Squirrel is assumed fully retired. 2.x.x's remaining job, once 3.0 is out,
is just staying relevant to what's already deployed and shipping critical
hotfixes until people are moved off it.

---

## The hard constraint everything else is built around

Every FE-BUDDY install on 2.8.3 or earlier has Clowd.Squirrel's
`GithubSource` baked into it already, and that code cannot be changed
retroactively. Its update-check logic is fixed and unforgiving:

```csharp
var releases = await GetReleases(Prerelease);
Release = releases.OrderByDescending(d => d.PublishedAt).Where(...).First();
```

It grabs the **single most recent matching GitHub release** (by publish
date) and expects a `RELEASES` manifest + nupkg assets to be attached to
*that exact release*. No fallback, no searching backward through history -
if the newest release lacks those assets, the update check throws.

Consequence: the moment any GitHub release newer than the last
Squirrel-compatible one exists, **every remaining Squirrel install's
update check breaks immediately** - not just installs that are many
versions behind. There's no way to fix this after the fact for
already-deployed clients; whatever we do has to work within that exact
"grab the single newest matching release" behavior.

---

## The plan

1. **2.8.3** - last pure-Squirrel release. No changes.

2. **2.8.4** - the handoff release.
   - Still shipped/updatable via Squirrel (so anyone on 2.8.3 or earlier
     can still reach it normally).
   - Also where the new MSI installer + version-policy custom action exist
     and are ready to take over.
   - On first run as 2.8.4, the app should detect it's still
     Squirrel-installed and trigger the MSI install of itself, handing
     control to the new installer going forward. *(Not yet built - see
     Open Questions.)*

3. **2.8.5 through some bounded number of later 2.x.x releases** - a
   **grace window**. Each of these releases also carries the frozen 2.8.4
   `RELEASES` file + nupkg as additional attached assets (unchanged,
   copied forward release to release), purely so straggling Squirrel
   installs that missed 2.8.4 can still find and apply it. Exact number of
   releases in the window is still TBD - see Open Questions.

   **How the copy-forward happens:** the release workflow
   (`.github/workflows/release.yml`, currently `.disabled` - see
   [PublishReleaseInstructions.md](PublishReleaseInstructions.md)) reads a
   repository variable `SQUIRREL_BRIDGE_TAG`, `gh release download`s the
   `*.nupkg` / `RELEASES` / `FE-BUDDYSetup.exe` assets from the release it
   names, and re-attaches them to each new draft. Set the variable to
   `2.8.4` once that release exists.

4. **After the grace window ends** - releases stop carrying any Squirrel
   assets. In practice this is just **unsetting the `SQUIRREL_BRIDGE_TAG`
   variable** (the workflow then skips the download/re-attach step
   entirely). Release notes for that first "clean" release state clearly:
   if you're running a version older than the last grace-window release,
   your app can no longer auto-update - download and run the latest MSI
   installer manually, once.

5. **3.0.0** - clean break. No Squirrel code, no detection, no legacy
   assets, nothing. Anyone still stuck on an ancient Squirrel install by
   this point is out of scope for automatic handling.

### Why a bounded grace window instead of forever, or nothing

- Reattaching the same legacy assets to *every* future release forever
  works technically, but clutters every release's file list with stray
  `.nupkg`/`RELEASES` files that don't belong there and would confuse
  anyone browsing releases normally - rejected for that reason.
- A bounded window catches the actively-used-but-not-constantly-updated
  population without permanent clutter, and after it ends, the ask on
  stragglers ("redownload once") is a normal, low-friction one-time
  action - not unreasonable for a free hobbyist tool going through a
  distribution-mechanism change.

---

## Making the manual fallback painless: Squirrel cleanup (app-side, not MSI-side)

Once a user is past the grace window, the plan is that they just download
and run the current MSI - **no manual uninstall step first**. The leftover
Squirrel install still needs removing, but that is done **by the app, not
by an MSI custom action**. Rationale: this cleanup is a one-time,
transitional concern; baking it into the MSI would leave dead migration
code shipping in every installer indefinitely (and 3.0 drops all Squirrel
awareness anyway). Keeping it in the 2.x app means it disappears naturally
when 2.x is retired.

The end-to-end flow (all in [Program.cs](../FeBuddyWinFormUI/Program.cs)
`CheckForUpdates()`, using
[`SquirrelInstall`](../FeBuddyLibrary/Update/SquirrelInstall.cs)):

1. **2.8.3 -> 2.8.4** - Squirrel updates normally. No change.
2. **2.8.4 running as the Squirrel copy** - `IsCurrentProcessSquirrelInstalled()`
   is true (running from a subfolder of `%LocalAppData%\FE-BUDDY\` with
   `Update.exe` alongside). The app offers, **on every launch until the
   user goes through with it**, to download and run the latest MSI for
   their update channel (`UpdateChecker.GetLatestForChannelAsync` ->
   `UpdateAvailableForm`, which does the download + elevated launch +
   process exit). Decline just falls through to the legacy Squirrel
   updater, unchanged.
3. **MSI installs** FE-BUDDY per-machine into Program Files.
4. **The MSI copy runs** - `InstalledProduct.IsMsiInstalled()` is true.
   If `SquirrelInstall.LeftoverInstallExists()` (i.e. `%LocalAppData%\FE-BUDDY\Update.exe`
   still present), the app invokes `SquirrelInstall.TryUninstall()` ->
   `Update.exe --uninstall`. **Safe now**, because the running process is
   the Program Files copy, not the Squirrel copy `Update.exe` is about to
   delete. Retried on every launch until `Update.exe` is actually gone; a
   failed/timed-out attempt is logged, never fatal.
5. **Shortcuts are put back** - `Update.exe --uninstall` removes shortcuts
   by path, not by target, so it also deletes the "FE-BUDDY.lnk" the MSI
   just created. Immediately after the uninstall,
   [`MsiShortcutRepair.RecreateMissing()`](../FeBuddyLibrary/Update/MsiShortcutRepair.cs)
   recreates a shortcut to the MSI executable in the per-user Start Menu /
   Desktop location, but only where the install's saved preference flags
   (`HKCU\Software\FE-BUDDY\StartMenuShortcut` / `DesktopShortcut`, from
   `Shortcuts.wxs`) say there should be one and none currently exists in
   either the per-user or all-users location.

**Why invoke `Update.exe --uninstall` rather than hand-delete:** a Squirrel
install isn't just files under `%LocalAppData%\FE-BUDDY\` - it also
registers a per-user `Uninstall` registry key (Add/Remove Programs entry)
and its shortcuts route through `Update.exe`. Hand-deleting the folder
leaves an orphaned Add/Remove Programs entry and dangling shortcuts.
`Update.exe --uninstall` removes everything it created (the same mechanism
the `OnAppUninstalled` handler already relies on).

**Known limitations / things to verify in testing:**

- **Multi-user machines:** `SquirrelInstall` only sees the *current*
  user's `%LocalAppData%`. A Squirrel install belonging to a different
  user on the same machine won't be detected or cleaned up by this. Fine
  for the normal single-user case.
- **Shortcut recreation location:** step 5 always recreates in the
  *per-user* Start Menu / Desktop folder (no elevation needed). If the MSI
  originally placed its shortcut in the all-users location and Squirrel
  deleted it from there, the replacement lands per-user instead - working,
  but in a slightly different place than a fresh MSI install would put it.
- **Log directory:** the log lives under `%LocalAppData%\FE-BUDDY\Logs`,
  which `Update.exe --uninstall` deletes along with the rest of the tree.
  `Logger.LogMessage` now tolerates the directory vanishing mid-session
  (recreates + retries); it previously threw `DirectoryNotFoundException`
  straight out of `Main`, which is what made the first migration test
  uninstall Squirrel but never open the app.

---

## Open questions (still need a decision)

- **Grace window length** - how many releases (or how much time) after
  2.8.4 keep carrying the legacy Squirrel assets? Balance: longer window
  = more stragglers rescued automatically, but more releases with the
  extra file-list clutter.
- **Exact cutoff version for the messaging** - once the window ends, the
  release notes need to name the specific version below which auto-update
  no longer works ("if you're on < 2.8.7, update manually" or similar).
- **Does 2.8.3-and-earlier's existing "update check failed" UI message
  get updated** to point people at the Releases page once their check
  starts failing for real? This is a small, low-risk change to
  already-shipped-adjacent behavior (i.e. something that could ship in
  2.8.4 itself) worth deciding alongside the rest.

### Resolved

- ~~Squirrel-detection-and-cleanup as an MSI custom action~~ - **rejected**
  in favour of app-side cleanup (see the section above). Detection marker
  is `Update.exe` under `%LocalAppData%\FE-BUDDY\`; invocation is
  `Update.exe --uninstall` with a 60s wait, logged and non-fatal on
  failure, retried next launch.
- ~~The 2.8.4 "detect I'm still Squirrel-installed, launch MSI" trigger~~ -
  **built.** Fires on startup, prompts every launch until done, asks the
  user first (reuses `UpdateAvailableForm`).
