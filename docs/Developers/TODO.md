# TODO

Open work only. When an item is done, delete it - the commit is the record.

## Performance

- **Parse only the NASR groups the sub-services read.** `AiracCycleDataCache` parses every NASR CSV
  group, but the sub-services only read APT, ARB, AWY, CLS_ARSP, DP, FIX, FRQ, NAV and STAR. Parsing
  just those would cut memory and launch time (`TODO (perf)` in `AiracCycleDataCache.cs`).

## Standards

- **Bring `FeBuddy.Harness` up to the standard.** It doesn't require XML docs yet, and still has
  planning-doc comments ("Phase 3.3-3.7 settings" in `HarnessSettings.cs`).

## Testing

- **Test more of `FeBuddy.Wpf`.** Tested today (`FeBuddy.UnitTests/Wpf`): the map's logic and
  `MapCanvas`, `BesideOrBelow`, `CommandTablePanel`, `InlineCode`, `InlineMarkdown`, the side nav,
  the AIRAC Service and File Conversions screens, the view-models of the General, Airways,
  Concatenate Aliases, File Names, Telephony and ERAM to GeoJSON tabs, the CRC-ERAM file choice, the
  list of a tab's problems, the sub-service order, the Review tab, Info, What's New, the Alias
  Command Guide, the Reset, Uninstall and update windows. The rest of the view-model logic - the
  other tabs' settings blocks, dirty tracking, validation - could be tested the same way, without a
  window.
