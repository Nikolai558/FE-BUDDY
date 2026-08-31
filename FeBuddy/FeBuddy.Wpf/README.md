# FeBuddy.Wpf

A **UI-only** WPF shell for FE-Buddy 3.0 - a modern re-skin in the style of
[clevelandcenter.org](https://clevelandcenter.org): dark blue-black surfaces, a
single amber accent, hairline cards, big display headings over airy body text.

It is deliberately **not wired to anything**:

- no reference to `FEBuddyLibrary` (or any project)
- no NuGet packages - the MVVM helpers (`ObservableObject`, `RelayCommand`), the
  toast store and the value converters live in `Infrastructure/`
- every screen shows **sample data**; e.g. *Generate GeoJSON* runs a scripted
  progress panel and raises a toast, but writes nothing

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
ViewModels/           ShellViewModel + one per screen, all with sample data
Views/                ShellWindow (custom chrome) + Dashboard, AIRAC, Map,
                      Conversions, GeoJSON Tools, Alias & Reference, Settings, Info
```

### Screens (all sample data / view-only)

Shaped by the v2.x → 3.0 carry-forward map:

- **AIRAC** - pick a cycle (APRA-verified), toggle the output families (each = one
  v2.x generator: airports, NAVAIDs, fixes, boundaries, airways, DP/STAR, weather,
  alias, publications), tune the airway sub-options (output mode, buffer, feb.*,
  DME-cutoff variant), set an ROI override, run the scripted build.
- **Conversions** - `.DAT` / `.KML` / `.SCT2` import to CRC GeoJSON, multi-file,
  per-format options. vSTARS/vERAM and DXF are called out as retired.
- **GeoJSON Tools** - validate a file against the ERAM/STARS schema (BCG/filter
  warnings, precision, self-intersection, mergeable features), or run a clean-up
  pass. Logic salvaged from v2.x's `GeoJson.cs`.
- **Alias & Reference** - the `ALIAS/` text outputs (AWY alias, ISR, chart recall,
  telephony) with samples, plus a cross-file duplicate-command check.
- **Settings** - facility profile (name / ARTCC ID / output dir - replaces the
  hard-coded list), default ROI, output preferences (single-line, feb.*,
  coordinate precision + the non-US-region "." invariant), updates.

### Shell extras (all sample data / view-only)

- **Toasts** - `Infrastructure/Toast.cs` is a static store; the shell hosts an
  `ItemsControl` bound to `Toast.Items` bottom-right. Cards slide in and
  auto-dismiss.
- **Zulu clock** in the status bar (`DispatcherTimer`, UTC).
- **Systems-health popover** - the nav's bottom widget opens a list of endpoints
  with green/amber/red dots.
- **Collapsible nav rail** - width animates 232 ⇄ 60; labels hide, tooltips carry
  the names.
- **Scripted generation run** - the AIRAC screen's *Generate* builds a step list
  from the toggles and advances it on a timer, with a progress bar and elapsed
  timer, then toasts.

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
