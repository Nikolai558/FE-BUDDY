# FeBuddy.Wpf

A WPF shell for FE-Buddy 3.0 - a modern re-skin in the style of
[clevelandcenter.org](https://clevelandcenter.org): dark blue-black surfaces, a
single amber accent, hairline cards, big display headings over airy body text.

It started **not wired to anything** and is being grown into the real app one
screen at a time. **`FE-Buddy_3.0_Feedback_Remediation_Plan.md` (repo root) is the
authoritative plan for this work** - where this README and that plan disagree, the
plan wins.

- references `FEBuddyLibrary`; no other NuGet packages - the MVVM helpers
  (`ObservableObject`, `RelayCommand`), the toast store and the value converters
  live in `Infrastructure/`
- **Airways is now a sub-service of AIRAC Service**, not a top-level screen
  (remediation plan rule 1.1). The library code moved to
  `FEBuddyLibrary.Services.Airac.Airways`; the GUI reaches it only through the
  AIRAC Service screen.
- **Foundations landed (Phase 0):** `AppLog` (activity log sink), `UserConfigFile`
  (`%APPDATA%\FE-Buddy\UserConfig.json` read/write + one-step undo), and an
  off-UI-thread launch sequence (`App.xaml.cs` -> `LaunchSequence`: temp clear,
  config read, UTC/internet check, version check; AIRAC pipeline and News are
  stubbed pending Phases 2 and 6.1).
- the Dashboard, AIRAC, Map and Settings screens still show **sample data**
  pending their rebuild phases (6, 7, 8, 12).

## Layout

```
Theme/                design system - the only place colours, type and control
  Palette.xaml          look are defined
  Typography.xaml
  Icons.xaml            Segoe Fluent Icons glyph code-points
  Controls.Buttons.xaml
  Controls.Buttons.xaml  Primary/Ghost/Subtle, Ghost.Toggle, Card.Toggle, caption
  Controls.Inputs.xaml   Pill.Radio, Switch, Field, Progress
  Controls.Surfaces.xaml Card, Divider, Chip, nav row
  Controls.Chrome.xaml   implicit ScrollBar (thin, theme-coloured) + ToolTip
                         (dark rounded popover, soft shadow, fade-in)
  Theme.xaml             merges the above; App.xaml merges only this

Controls/             SectionHeader, StatTile, MapCanvas (all dependency-free)
Infrastructure/       ObservableObject, RelayCommand, converters
Map/                  GeoJSON reader (System.Text.Json), Web-Mercator, layer model
Assets/               bundled sample GeoJSON (us-states, sample-airways)
ViewModels/           ShellViewModel + one per screen
Views/                ShellWindow (custom chrome) + Dashboard, AIRAC, Airways, Map,
                      Settings, Info
```

### Screens

Primary nav is **Services only**: Dashboard, AIRAC Service, Map. `Settings` and
`Info` are system nav. The prototype-only Conversions, GeoJSON Tools, Alias &
Reference and Placeholder screens were deleted in Phase 0.5.

- **AIRAC Service** *(being rebuilt — Phase 7)* - cycle + facility settings, then
  the sub-service list (**Airways only**, rule 1.3), then **Run AIRAC Service**.
  Selecting Airways opens its settings page inside this screen
  (`AIRAC Service › Airways`), never as a top-level screen.
  - **Airways sub-service** *(the AirwaysView/AirwaysViewModel prototype, being
    re-parented — Phase 7)* - configure the settings the Airways pipeline accepts
    (output mode, buffer, `feb.*` properties, alias file, antimeridian split, CRC
    ERAM defaults per altitude class, ROI), run, and see the real result: airways
    built, GeoJSON files with feature counts, the alias file path, and warnings
    grouped by airway. The library entry point is
    `FEBuddyLibrary.Services.Airac.AiracService.RunAsync`.
- **Dashboard** *(sample data — Phase 6)* - becomes News + a live activity-log
  viewer over `AppLog`.
- **Map** *(sample data — Phase 8)* - view user GeoJSON and manage the default ROI;
  everything else in the prototype is removed.
- **Settings** *(sample data — Phase 12)* - Updates, one facility profile, default
  ROI, GeoJSON output preferences.
- **Info** *(Phase 13)* - Manual, Change log, Issues & requests.

### Shell extras

- **Toasts** - `Infrastructure/Toast.cs` is a static store; the shell hosts an
  `ItemsControl` bound to `Toast.Items` bottom-right. Cards slide in and
  auto-dismiss.
- **Zulu clock** in the status bar (`DispatcherTimer`, UTC).
- **Collapsible nav rail** - width animates 232 ⇄ 60; labels hide, tooltips carry
  the names.
- The border chrome (version chip / update window, top-centre AIRAC status) and
  the systems-health widget are reworked in Phase 4.

### The map

`Controls/MapCanvas` is a from-scratch vector map: Web-Mercator projection, a
pan (drag) / zoom (wheel) viewport, and `StreamGeometry` drawn into a couple of
`DrawingVisual`s. **No tiles, no network, no map SDK.** It takes a base
`MapLayer` (US state outlines, bundled) plus overlay layers, renders any standard
GeoJSON (`Map/GeoJsonReader`), can rubber-band a region of interest whose corners
come back through two-way `RoiSouthWest` / `RoiNorthEast`, and has a **ruler**
mode (click two points for great-circle distance + bearing) plus a live cursor
lat/lon read-out. It does **not** give you satellite imagery or street labels -
for that you'd need a real map library and a tile service.

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
the `.ttf` files in `Assets/Fonts/` and change the two `FontFamily` values at the
top of `Theme/Typography.xaml` to e.g.
`pack://application:,,,/Assets/Fonts/#Montserrat`.

## Run

```bash
dotnet run --project FeBuddy.Wpf
```
