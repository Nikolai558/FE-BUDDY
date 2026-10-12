# TODO

Open work only. When an item is done, delete it - the commit is the record.

## Standards

- **Bring `FeBuddy.Harness` up to the standard.** It doesn't require XML docs yet, and still has
  planning-doc comments ("Phase 3.3-3.7 settings" in `HarnessSettings.cs`).

## Testing

- **Test more of `FeBuddy.Wpf`.** Tested today (`FeBuddy.UnitTests/Wpf`): the map's logic and
  `MapCanvas`, `BesideOrBelow`, `CommandTablePanel`, `OutputTag`, `OutputSummary`, `InlineCode`,
  `InlineMarkdown`, `FileNamesAsCodeConverter`, the side nav and the Systems drawer, the AIRAC Service
  and File Conversions screens, the view-models of the General, Airways, Procedures, Add Custom
  Aliases, File Names, Telephony and ERAM to GeoJSON tabs, the Area card, What You'll Get, the
  Departures and Arrivals amendment question, the CRC-ERAM file choice, the list of a tab's problems,
  the sub-service order, the Review tab, Info, What's New, the Alias Command Guide, settings profiles,
  Import Settings, the duplicate alias review, and the Reset, Uninstall and update windows. The rest
  of the view-model logic - the
  other tabs' settings blocks, dirty tracking, validation - could be tested the same way, without a
  window.
