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

4. **After the grace window ends** - releases stop carrying any Squirrel
   assets. Release notes for that first "clean" release state clearly:
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

## Making the manual fallback painless: MSI-side Squirrel cleanup

Once a user is past the grace window, the plan is that they just download
and run the current MSI - **no manual uninstall step first**. For that to
work cleanly, the MSI needs to detect a leftover Squirrel install and
clean it up itself, likely as an early custom action (similar in shape to
`EnforceVersionPolicy`, see [FE-BUDDY.Installer.CustomActions](../FE-BUDDY.Installer.CustomActions)).

Important constraint discovered while thinking this through: **don't
hand-delete Squirrel's files.** A Squirrel install isn't just files under
`%LocalAppData%\FE-BUDDY\` - it also registers itself in the per-user
`Uninstall` registry key (so it shows up in Add/Remove Programs) and its
shortcuts route through `Update.exe`, not the app directly. Deleting the
folder by hand would leave a broken, orphaned "FE-BUDDY" entry in Add/
Remove Programs and dangling shortcuts. The correct approach is to invoke
Squirrel's own uninstaller - `%LocalAppData%\FE-BUDDY\Update.exe
--uninstall` - which already knows how to remove everything it created
(the same mechanism the existing `OnAppUninstalled` handler in
[Program.cs](../FeBuddyWinFormUI/Program.cs) relies on today) - and only then
let the MSI proceed with its own install.

**Known limitation, not yet resolved:** Squirrel installs per-user; the
MSI's cleanup custom action likely runs elevated (per-machine install).
It needs to detect and clean up the Squirrel install belonging to
whichever user is actually running the installer - fine in the normal
single-user-on-their-own-machine case, but worth being aware this isn't
automatically correct in a shared/multi-user-machine scenario.

---

## Open questions (need a decision before implementation)

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
- **The Squirrel-detection-and-cleanup custom action itself** isn't built
  yet - needs its own design pass (exactly what marks "this is a Squirrel
  install," exact `Update.exe --uninstall` invocation and error handling,
  what happens if that uninstall fails partway).
- **The 2.8.4 "detect I'm still Squirrel-installed, launch MSI" trigger**
  on the app side isn't built yet either - needs to decide exactly when/
  how it fires (on startup? one-time? does it ask the user first?).
