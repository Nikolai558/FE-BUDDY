# FeBuddy.Wpf

A WPF shell for FE-Buddy 3.0 - a modern re-skin in the style of
[clevelandcenter.org](https://clevelandcenter.org): dark blue-black surfaces, a
single amber accent, hairline cards, big display headings over airy body text.

All twelve phases of **`docs/Developers/FE-Buddy_3.0_Feedback_Remediation_Plan.md`**
have landed - where this README and that plan disagree, the plan wins. No screen
shows sample data.

This README lives at `docs/Developers/FeBuddy.Wpf/README.md`; every code path
below is relative to `FeBuddy/FeBuddy.Wpf/` in the repo unless stated otherwise.

- references `FeBuddy.Core`; no other NuGet packages - the MVVM helpers
  (`ObservableObject`, `RelayCommand`, `SubServiceSettingsViewModel`), the tab
  model for a first-tier service screen, the toast store, `Links`,
  `BrowserLauncher` and the value converters live in `Infrastructure/`
- **Airways is a sub-service of AIRAC Service**, not a top-level screen
  (remediation plan rule 1.1). The library code is
  `FeBuddy.Core.Services.Airac.Airways`; the GUI reaches it only as a tab on the
  AIRAC Services screen. It is the only sub-service with a backend (rule 1.3) -
  the catalogue also lists Airports and Departures, which open a tab and produce
  nothing.
- On launch, `App.xaml.cs` starts `AppLog`'s file sink then runs
  `LaunchSequence` off the UI thread: clear `%TEMP%\FE-Buddy`, read
  `UserConfig.json`, UTC/internet check, version check, the AIRAC data pipeline
  (`AiracCycleDataCache` - probe/download/parse previous/current/next), and the
  News check. Results land in `AppEnvironment`; every step narrates itself in the
  Dashboard activity log.

## Layout

```
Theme/                design system - the only place colours, type and control
  Palette.xaml          look are defined
  Typography.xaml
  Icons.xaml            Segoe Fluent Icons glyph code-points
  Controls.Buttons.xaml
  Controls.Buttons.xaml  Primary/Ghost/Subtle, Ghost.Toggle, Card.Toggle, caption
  Controls.Inputs.xaml   Pill.Radio, Switch, Field, Progress; the themed ComboBox
                         is implicit, so every dropdown gets the dark popup and the
                         drop-down wheel scrolling without asking for a style
  Controls.Surfaces.xaml Card, Divider, Chip, nav row
  Controls.Chrome.xaml   implicit ScrollBar (thin, theme-coloured) + ToolTip
                         (dark rounded popover, soft shadow, fade-in)
  Theme.xaml             merges the above; App.xaml merges only this

Controls/             SectionHeader, StatTile, MapCanvas, RoiEditor
Infrastructure/       ObservableObject, RelayCommand, SubServiceSettingsViewModel,
                      the tab model (TabbedServiceViewModel, ServiceTabViewModel,
                      SubServiceDescriptor, SubServiceSelection,
                      ServiceReviewTabViewModel, PlaceholderSubServiceViewModel),
                      Toast, Links, BrowserLauncher, converters
Map/                  GeoJSON reader (System.Text.Json), Web-Mercator, layer model
Assets/               us-states.json (reference geography, not sample data)
ViewModels/           ShellViewModel + one per screen; AiracSubServices is the
                      AIRAC sub-service catalogue
Views/                ShellWindow (custom chrome) + Dashboard, AiracService and
                      its tab views (AiracGeneralTabView, AirwaysView,
                      PlaceholderSubServiceView, ServiceReviewTabView), Map,
                      Settings, Info; UpdateWindow, ConfirmWindow, RoiPickerWindow
```

### Screens

Primary nav is **Services only**: Dashboard, AIRAC Service (◷ U+25F7), Map.
`Settings` and `Info` are system nav. Conversions / GeoJSON Tools / Alias &
Reference / Placeholder were deleted in Phase 0.5; the top-level Airways nav item
was removed in Phase 1.1.

- **AIRAC Services** - a tabbed screen: a vertical tab rail down the left, a
  permanent **General** tab, one tab per selected sub-service, and a **Review** tab
  at the end. Tabs are data (`TabbedServiceViewModel`), not hand-placed XAML - this
  service is expected to reach ~20 sub-services. There is no breadcrumb. The screen
  gates on AIRAC-data readiness (2.5).
  - **General tab** - the cycle menu (Previous / Current / Next, each showing its
    live `AiracCycleDataCache` state), the facility ARTCC dropdown (from the
    cycle's parsed `Apt.AptBase`), and the sub-service picker. Ticking a
    sub-service means "produce data for this" *and* opens its tab; unticking closes
    the tab and leaves its saved `UserConfig` subtree untouched, so re-ticking
    restores it. The selection persists to
    `Services.AiracService.SelectedSubServices`.
  - **Sub-service catalogue** - `ViewModels/AiracSubServices.cs`: Airports,
    Airways, Departures. Only Airways has a backend; the other two open a
    `PlaceholderSubServiceView` stating their settings are not built yet, and they
    contribute nothing to a run. Adding one later is a catalogue entry plus a tab
    view-model.
  - **Airways tab** - a `SubServiceSettingsViewModel` (Save / Undo-last-save /
    dirty). Output mode + per-kind emit toggles, designation include/exclude (from
    the cycle's `AWY_ID`s), buffer, verbatim `feb.*` properties, three stacked CRC
    ERAM blocks, the aliases section (`Airways.txt` + ROI scope), the ROI override
    (shared `RoiEditor`), and the antimeridian toggle. Its result panel shows files
    + feature counts, the alias line count, the excluded-airway count, and messages
    grouped by airway and presented by level (info collapsed behind a count).
  - **Review tab** - present once at least one sub-service is selected: every tab's
    settings as label/value rows, notices naming any unsaved or invalid tab, and the
    single **Run AIRAC Service** button, which goes through `AiracService.RunAsync`.
    The live per-process run feed under that button is planned, not built.
  - **Action bar** - above the tab content and again at the end of it: Previous,
    Next, Review AIRAC Service settings, Undo last save, Save. Previous / Next /
    Review offer to save a dirty tab first; cancelling keeps you where you are. The
    rail dot is amber for unsaved edits and red for a missing or invalid value, and
    an invalid field highlights in place with the validator's message as its
    tool-tip.
- **Dashboard** - the verbatim description box + Discord link + next-cycle line,
  the News feed (from `NewsService`), and a live activity-log viewer over `AppLog`
  (filter chips with counts, minimizable).
- **Map** - load your own GeoJSON (one layer per file, a layer list with
  visibility / count / remove), and manage the one default ROI in place via the
  shared `RoiEditor`. No ruler, no CRC display visualiser, no sample layers.
- **Settings** - Updates (channel + tooltips + "check now" + rollback), Facility
  Profile (one facility from the parsed cycle, default output dir + FE-Buddy_Output
  toggle), Default Region of Interest (`RoiPickerWindow`), GeoJSON Files (feb.*
  description, Maximum Coordinate Precision 5/6/7 dp). Everything persists to
  `UserConfig.json`.
- **Info** - Manual, Change log, Issues & requests as real links (About deleted).

### Shell extras

- **Toasts** - `Infrastructure/Toast.cs` static store; hosted bottom-right.
- **Border chrome** - FE-BUDDY tooltip shows update state only; the version chip is
  a button that opens `UpdateWindow` when an update exists; the AIRAC status
  readout sits top-centre and narrates the launch pipeline.
- **Zulu clock** in the status bar; **collapsible nav rail** (232 ⇄ 60).

### The map

`Controls/MapCanvas` is a from-scratch vector map: Web-Mercator projection, a
pan (drag) / zoom (wheel) viewport, and `StreamGeometry` into `DrawingVisual`s.
**No tiles, no network, no map SDK.** It takes a base `MapLayer` (US state
outlines) plus overlay layers, renders standard GeoJSON (`Map/GeoJsonReader`), can
rubber-band an ROI (corners via two-way `RoiSouthWest` / `RoiNorthEast`), and has
a live cursor lat/lon read-out. `Controls/RoiEditor` composes it with the four
corner boxes and a Set ROI / Cancel pair - the one ROI editor, hosted in place on
the Map screen and inside `RoiPickerWindow` for the Settings and AIRAC dialogs.
The ruler / measure mode is no longer surfaced.

### Conventions

- **Tokens, not literals.** No view or style hard-codes a colour, font or radius;
  they reference `Brush.*`, `Font.*`, `Radius.*`, `Text.*`.
- **`DynamicResource` for tokens inside `Theme/`** (a `StaticResource` from one
  merged dictionary to a sibling silently resolves to `UnsetValue`).
  `StaticResource` is fine from the Views.
- **Navigation** is a `ContentControl` + `DataTemplate` per view-model. Nav rows
  are grouped `RadioButton`s bound to `NavItem.IsActive`.
- Window chrome uses `WindowChrome` (no `AllowsTransparency`) so aero-snap and
  the system shadow still work.

## Fonts

The design uses **Montserrat** (headings) and **Jost** (body); neither ships with
Windows, so the app currently falls back to Segoe UI. To use the real faces, drop
the `.ttf` files in `FeBuddy/FeBuddy.Wpf/Assets/Fonts/` and change the two
`FontFamily` values at the top of `FeBuddy/FeBuddy.Wpf/Theme/Typography.xaml` to e.g.
`pack://application:,,,/Assets/Fonts/#Montserrat`.

## Run

```bash
# from the repo root
dotnet run --project FeBuddy/FeBuddy.Wpf
```
