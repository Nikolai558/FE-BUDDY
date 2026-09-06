# FeBuddy.Wpf

A WPF shell for FE-Buddy 3.0 - a modern re-skin in the style of
[clevelandcenter.org](https://clevelandcenter.org): dark blue-black surfaces, a
single amber accent, hairline cards, big display headings over airy body text.

It started **not wired to anything** and is being grown into the real app one
screen at a time:

- no NuGet packages - the MVVM helpers (`ObservableObject`, `RelayCommand`), the
  toast store and the value converters live in `Infrastructure/`
- **Airways is the first real screen**: it references `FEBuddyLibrary` and
  calls `AirwayService.Run` for real - pick an actual NASR CSV folder and
  output folder, and it writes actual GeoJSON and alias files.
- every other screen still shows **sample data**; e.g. AIRAC's *Generate*
  runs a scripted progress panel and raises a toast, but writes nothing

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
ViewModels/           ShellViewModel + one per screen (Airways is real; the rest are sample data)
Views/                ShellWindow (custom chrome) + Dashboard, AIRAC, Airways, Map,
                      Conversions, GeoJSON Tools, Alias & Reference, Settings, Info
```

### Screens

Shaped by the v2.x → 3.0 carry-forward map:

- **Airways** (real, wired to `FEBuddyLibrary`) - pick a NASR CSV folder and an
  output folder, configure the same settings `AirwayService.Run` accepts
  (output mode, buffer, feb.* properties, alias file, antimeridian split, CRC
  ERAM defaults per altitude class, ROI clipping), click Run, and see the
  actual result: airways built, GeoJSON files with real feature counts, the
  alias file path, and any warnings (e.g. an unresolvable NASR waypoint),
  grouped by airway.
- **AIRAC** *(sample data)* - pick a cycle (APRA-verified), toggle the output families (each = one
  v2.x generator), tune the airway sub-options: output mode, buffer, feb.*,
  DME-cutoff variant, **designation include/exclude chips**, **break-at-fixes with
  a DME range per fix type**, and a **CRC ERAM defaults editor** (Lines/Symbols/Text
  → BCG / filters / style / thickness / size / underline / offsets). Set an ROI
  override, opt into a **cycle-diff report** (sample preview), run the scripted build.
- **Conversions** *(sample data)* - `.DAT` / `.KML` / `.SCT2` import to CRC GeoJSON, multi-file,
  per-format options. vSTARS/vERAM and DXF are called out as retired.
- **GeoJSON Tools** *(sample data)* - validate a file against the ERAM/STARS schema (BCG/filter
  warnings, precision, self-intersection, mergeable features), or run a clean-up
  pass. Logic salvaged from v2.x's `GeoJson.cs`.
- **Alias & Reference** *(sample data)* - the `ALIAS/` text outputs with samples (each line has a
  copy button), plus a cross-file duplicate-command check.
- **Map** *(sample data)* - adds a **Display** drop-down that toggles which BCG groups / filters
  are "on" (the CRC display visualiser), and copy buttons on the cursor / ruler /
  ROI read-outs.
- **Settings** *(sample data)* - **multiple named facility profiles** (switch / new / import /
  export), default ROI, output preferences, a **display-scheme editor** (name the
  BCG groups + filters once, export an ISR legend), a **NASR data-source** override
  (FAA / custom URL / local file for offline builds), updates.
- `Controls/CopyButton` - a shared tiny copy-to-clipboard icon (flips to a check
  for ~1 s); `Copy.Button` style.

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
