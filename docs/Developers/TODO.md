# TODO

Open work only. When an item is done, delete it from this list - the commit that did it is the
record. Add new items to the section they belong to.

## Features

- **A 3.0 user manual behind the app's Manual link.** `Links.Manual` (SYSTEM ▸ Info) still opens
  the 2.x HTML manual in `docs/Users/Manual HTML/`. The 3.0 guide now exists at
  [docs/Users/User-Guide.md](../Users/User-Guide.md); point the link there when you are happy
  with it.
- **A release workflow.** CI builds, tests and produces a test MSI on every push to
  `v3-development`, but there is no workflow that publishes a GitHub release yet
  (see `.github/workflows/ci.yml`). Releases are cut by hand with `build.ps1`.
- **Bundle the design fonts.** The theme is designed for Montserrat and Jost, neither of which
  ships with Windows, so the app falls back to Segoe UI. See the "Fonts" section of
  [FeBuddy.Wpf/README.md](FeBuddy.Wpf/README.md).

## Performance

- **Parse only the NASR groups the sub-services read.** `AiracCycleDataCache` parses every NASR
  CSV group, but Airports, Airways and Departures only read APT, AWY, CLS_ARSP, DP, FIX, FRQ and
  NAV. Parsing just those would cut memory and launch time. (Marked `TODO (perf)` in
  `AiracCycleDataCache.cs`.)

## Tidy-ups

- **The default output folder nests `FE-Buddy_Output` twice.** With no output directory saved,
  runs use `Desktop\FE-Buddy_Output`, and "Add a FE-Buddy_Output folder" defaults to on, so
  files land in `Desktop\FE-Buddy_Output\FE-Buddy_Output\...`. Default the directory to the
  Desktop itself, or default the option to off.
- **The Facility setting is saved but unused.** Settings ▸ Facility Profile ▸ Facility writes
  `Services.AiracService.UserArtccId`, but no sub-service reads it yet. Use it (e.g. to
  pre-select the Departures ARTCC filter) or remove it.
- **Misspelled config keys.** `DefaultCoordindates` and `OverrideCoordindates` in
  `UserConfig.json` are misspelled, and every saved config holds them that way. Renaming them
  needs a one-time migration that copies the old keys to the new ones.
- **`DefaultRoi.FilterByRoi` is saved as `true` / `false`**, while every other yes/no setting is
  `Y` / `N`. Harmless (both are read correctly), but inconsistent; fold it into the same migration.

## Standards

- **Bring `FeBuddy.Harness` to the standard.** Every other project follows the `.editorconfig`,
  requires XML docs and has no planning-doc references; the harness does not yet (today:
  one unused `using`, and comments like "Phase 3.3-3.7 settings").

## Testing

- **`FeBuddy.Wpf` has no automated tests.** Its view-model logic (the settings blocks each tab
  builds, dirty tracking, validation) could be unit tested without a window. Today the only check
  is running the app.
