# TODO

Open work only. When an item is done, delete it - the commit is the record.

## Performance

- **Parse only the NASR groups the sub-services read.** `AiracCycleDataCache` parses every NASR CSV
  group, but the sub-services only read APT, ARB, AWY, CLS_ARSP, DP, FIX, FRQ, NAV and STAR. Parsing
  just those would cut memory and launch time (`TODO (perf)` in `AiracCycleDataCache.cs`).

## Tidy-ups

- **Misspelled config keys.** `DefaultCoordindates` and `OverrideCoordindates` are misspelled in
  every saved `UserConfig.json`. Renaming them needs a one-time migration from the old keys.
- **`DefaultRoi.FilterByRoi` is saved as `true` / `false`**, while every other yes/no setting is
  `Y` / `N`. Harmless, but fold it into the same migration.

## Standards

- **Bring `FeBuddy.Harness` up to the standard.** It doesn't require XML docs yet, and still has
  planning-doc comments ("Phase 3.3-3.7 settings" in `HarnessSettings.cs`).

## Testing

- **Test more of `FeBuddy.Wpf`.** Tested today (`FeBuddy.UnitTests/Wpf`): the map's logic and
  `MapCanvas`, `BesideOrBelow`, `CommandTablePanel`, `InlineCode`, `InlineMarkdown`, and the view-models
  of the Airways, File Names, Telephony and ERAM to GeoJSON tabs, the sub-service order, the Review
  tab, Info, What's New, the Alias Command Guide, the Reset, Uninstall and update windows. The rest of
  the view-model logic - the other tabs' settings blocks, dirty tracking, validation - could be
  tested the same way, without a window.
