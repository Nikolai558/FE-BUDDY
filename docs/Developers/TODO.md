# TODO

Open work only. When an item is done, delete it from this list - the commit that did it is the
record. Add new items to the section they belong to.

## Features

- **Bundle the design fonts.** The theme is designed for Montserrat and Jost, neither of which
  ships with Windows, so the app falls back to Segoe UI. See the "Fonts" section of
  [FeBuddy.Wpf/README.md](FeBuddy.Wpf/README.md).

## Performance

- **Parse only the NASR groups the sub-services read.** `AiracCycleDataCache` parses every NASR
  CSV group, but Airports, Airways, Departures, Arrivals, NAVAIDs, ARTCC Boundaries, Fixes and
  Procedures only read APT, ARB, AWY, CLS_ARSP, DP, FIX, FRQ, NAV and STAR. Parsing just those would
  cut memory and launch time. (Marked `TODO (perf)` in `AiracCycleDataCache.cs`.)

## Tidy-ups

- **Misspelled config keys.** `DefaultCoordindates` and `OverrideCoordindates` in
  `UserConfig.json` are misspelled, and every saved config holds them that way. Renaming them
  needs a one-time migration that copies the old keys to the new ones.
- **`DefaultRoi.FilterByRoi` is saved as `true` / `false`**, while every other yes/no setting is
  `Y` / `N`. Harmless (both are read correctly), but inconsistent; fold it into the same migration.

## Standards

- **Bring `FeBuddy.Harness` to the standard.** Every other project requires XML docs and has no
  planning-doc references; the harness does not yet (today: no XML docs are required there, and
  comments like "Phase 3.3-3.7 settings").

## Testing

- **Most of `FeBuddy.Wpf` has no automated tests.** Only the map's logic, the File Names and
  Airways tabs' view-models, the sub-service order, the Reset window's view-model, `BesideOrBelow`
  and `InlineCode` are tested (`FeBuddy.UnitTests/Wpf`). The rest of its view-model logic (the
  settings blocks each other tab builds, dirty tracking, validation, the settings import wording)
  could be tested the same way, without a window. Today the only check is running the app.
