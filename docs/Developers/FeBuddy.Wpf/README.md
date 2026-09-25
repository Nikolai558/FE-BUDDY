# FeBuddy.Wpf

A WPF shell for FE-Buddy 3.0 - a modern re-skin in the style of
[clevelandcenter.org](https://clevelandcenter.org): dark blue-black surfaces, a
single amber accent, hairline cards, big display headings over airy body text.

This README describes the app as it is. (The plans it was built from are in
[Archive](../Archive/README.md), for history.) No screen shows sample data. For what each screen
does from a user's point of view, see the [user guide](../../Users/User-Guide.md).

This README lives at `docs/Developers/FeBuddy.Wpf/README.md`; every code path
below is relative to `FeBuddy/FeBuddy.Wpf/` in the repo unless stated otherwise.

- references `FeBuddy.Core`; no other NuGet packages - the MVVM helpers
  (`ObservableObject`, `RelayCommand`) are hand-rolled in `Mvvm/`
- **Airports, Airways and Departures are sub-services of AIRAC Service**, not
  top-level screens. The library code is `FeBuddy.Core.Application.Airac.*`; the GUI
  reaches each one only as a tab on the AIRAC Services screen. In the same way, each
  **file conversion** (DAT to GeoJSON, SCT2 to GeoJSON) is a tab on the File Conversions screen
  (`FeBuddy.Core.Application.Conversions.*`).
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
  Controls.Buttons.xaml  Primary/Ghost/Subtle, Card.Toggle, Copy.Button, caption
  Controls.Inputs.xaml   Field, Option, Toggle.Number, Progress; the themed ComboBox
                         is implicit, so every dropdown gets the dark popup and the
                         drop-down wheel scrolling without asking for a style
  Controls.Surfaces.xaml Card, Divider, Chip, nav row
  Controls.Chrome.xaml   implicit ScrollBar (thin, theme-coloured) + ToolTip
                         (dark rounded popover, soft shadow, fade-in)
  Theme.xaml             merges the above; App.xaml merges only this

Assets/               us-states.json (reference geography, not sample data)
Behaviors/            attached properties a view opts into: FieldState (validation
                      look), WheelScroll, ComboBoxDropDownFocus, MaximizeToWorkArea
Controls/             reusable controls: Card, SectionHeader, Option, CopyButton,
                      FilterPicker, MarkdownView, MapCanvas, RoiEditor, and
                      ChromeWindow (the base for every dialog window)
Converters/           one IValueConverter per file
Map/                  GeoJSON reader (System.Text.Json), Web-Mercator, BaseMap
  Models/               GeoPoint, GeoBounds, MapGeometry, MapLayer
Mvvm/                 ObservableObject, RelayCommand
Shell/                app-wide services: Toast, Links, BrowserLauncher,
                      DefaultRoiStore (the one saved default ROI), OutputPreferences
                      (output folder + coordinate precision, read at run time)
  Models/               ToastKind
ViewModels/           ShellViewModel + one per screen; AiracSubServices is the
                      AIRAC sub-service catalogue; FileConversionsViewModel lists
                      the conversions
  Models/               small item and row view-models and records (HealthRow,
                        FebPropertyToggle, EramClassDefault, SourceFileItem, ...)
  ServiceTabs/          the framework for a tabbed service screen:
                        TabbedServiceViewModel (the screen), ServiceTabViewModel
                        (a tab), SubServiceSettingsViewModel (a saved settings tab),
                        GeojsonSubServiceViewModel (what the GeoJSON sub-service tabs
                        share), ConversionTabViewModel (a conversion tab with its own
                        run button), FileConversionTabViewModel (what the file-to-GeoJSON
                        conversion tabs share: source, CRC defaults, save contract, run
                        summary), CrcDefaultsRowIo (a CRC defaults row in and out
                        of config), the Preview Settings and Review tabs, the card
                        interfaces (IOutputSettings, ...), ISubServiceRunTarget
    Models/               SubServiceDescriptor, ServicePreviewRow/Section, ...
Views/                ShellWindow (custom chrome) + Dashboard, TabbedServiceView
                      (the AIRAC Services and File Conversions screens) and their
                      tab views (AiracGeneralTabView, AirportsView, AirwaysView,
                      DeparturesView, DatToGeojsonView, SctToGeojsonView, VeramToGeojsonView,
                      ServicePreviewTabView,
                      ServiceRunReviewTabView), Map, Settings, Info; UpdateWindow,
                      ConfirmWindow, RoiPickerWindow
  Cards/                the cards every GeoJSON sub-service tab shares, RunCard
                        (the run button at the foot of the Preview Settings tab and
                        of every conversion tab) and SourceFilesCard (a conversion's
                        folder or picked files); each binds to its tab through one
                        ServiceTabs interface
```

Namespaces follow folders (`FeBuddy.Wpf.ViewModels.ServiceTabs`), and every file
holds one type. A type that only carries data (a record, an enum, a small row)
goes in the nearest `Models/` folder; a class that does work sits at the folder
root - the same rule as `FeBuddy.Core`.

### Where do I put...

- **A new sub-service** (say, STARs): an entry in `ViewModels/AiracSubServices.cs`,
  a `StarsViewModel` in `ViewModels/` deriving from `GeojsonSubServiceViewModel` and
  implementing `ISubServiceRunTarget`, a `StarsView` in `Views/` built from the
  shared cards, and its settings block on `AiracServiceSettings` in Core.
- **A new file conversion:** a view-model in `ViewModels/` deriving from
  `FileConversionTabViewModel` (it only adds its own settings and says which service
  to call), added to the list in `FileConversionsViewModel`'s constructor, and a view
  in `Views/` built from the shared cards (`SourceFilesCard`, `CrcDefaultsCard`,
  `RunCard`), with its DataTemplate in `TabbedServiceView.xaml`. The screen runs it and
  shows the outcome on its Review tab.
- **A reusable control:** `Controls/`, with its look in a `Theme/Controls.*.xaml` style.
- **An attached property** a view sets (`bhv:Something.Enable="True"`): `Behaviors/`.
- **A converter:** its own file in `Converters/`, instantiated once in `Theme/Theme.xaml`.
- **Something every screen can use** (a store, a launcher, a notification): `Shell/`.
- **A colour, font, radius or glyph:** `Theme/` - never a literal in a view.

### Screens

Primary nav is **Services only**: Dashboard, AIRAC Service, File Conversions, Map.
`Settings` and `Info` are system nav.

Both tabbed screens are one view, `TabbedServiceView`: the heading, tab rail, action
bar and page scroller are shared, and each screen's view-model says what differs
(its title, and whether it has General and Preview Settings tabs).

- **AIRAC Services** - a tabbed screen: a vertical tab rail down the left, a
  permanent **General** tab, one tab per selected sub-service, a **Preview
  Settings** tab, and - once a run has started - a **Review** tab at the end. Tabs
  are data (`TabbedServiceViewModel`), not hand-placed XAML - this service is
  expected to reach ~20 sub-services. The screen gates on AIRAC-data readiness.
  - **General tab** - the cycle menu (Previous / Current / Next, each showing its
    live `AiracCycleDataCache` state) and the sub-service picker. Ticking a
    sub-service means "produce data for this" *and* opens its tab; unticking closes
    the tab and leaves its saved `UserConfig` subtree untouched, so re-ticking
    restores it. The selection persists to
    `Services.AiracService.SelectedSubServices`.
  - **Sub-service catalogue** - `ViewModels/AiracSubServices.cs`: Airports,
    Airways, Departures. Adding one is a catalogue entry plus a tab view-model; one
    whose backend is not built yet opens a `PlaceholderSubServiceView` and
    contributes nothing to a run.
  - **Sub-service tabs** - each is a `GeojsonSubServiceViewModel` (Save /
    Undo-last-save / dirty, plus the outputs, file choices, `feb.*` properties, CRC
    ERAM defaults and ROI override every GeoJSON sub-service shares). The Airways tab: Output mode + per-kind emit toggles, designation include/exclude (from
    the cycle's `AWY_ID`s), buffer, verbatim `feb.*` properties, three stacked CRC
    ERAM blocks, the aliases section (`Airways.txt` + ROI scope), the ROI override
    (shared `RoiEditor`), and the antimeridian toggle. Its result panel shows files
    + feature counts, the alias line count, the excluded-airway count, and messages
    grouped by airway and presented by level (info collapsed behind a count).
  - **Preview Settings tab** - present once at least one sub-service is selected:
    every tab's settings as label/value rows, notices naming any unsaved or invalid
    tab, and the single **Run AIRAC Service** button, which goes through
    `AiracService.RunAsync`.
  - **Review tab** - appears once a run starts: the live step feed, errors,
    advisories, each sub-service's results, and the files written.
  - **Action bar** - above the tab content and again at the end of it: Previous,
    Next, Preview settings, Undo all changes, Undo last save, Save. Previous / Next /
    Preview settings offer to save a dirty tab first; cancelling keeps you where you are. The
    rail dot is amber for unsaved edits and red for a missing or invalid value, and
    an invalid field highlights in place with the validator's message as its
    tool-tip.
- **File Conversions** - the same tabbed screen with no General or Preview Settings
  tab: every conversion is always on the rail, and each runs on its own from the
  button at the foot of its tab (`ConversionTabViewModel.RunCommand`). The screen saves
  a dirty tab first (the user confirms), blocks an invalid one, runs the conversion on
  a background thread and fills the shared Review tab. A conversion's `RunBlocker`
  (e.g. "no files picked") keeps the button off without blocking Save, because picked
  input files are not saved settings. Nothing here waits for AIRAC data.
  - **Conversion tabs** - each is a `FileConversionTabViewModel`: source (a saved
    folder, or files picked one or several at a time and not saved) and the CRC ERAM
    defaults for the kinds it writes.
  - **DAT to GeoJSON tab** - the Lines panel only, plus the cropping distance. Goes
    through `DatToGeojsonService.Run`.
  - **SCT2 to GeoJSON tab** - Lines and Labels panels, nothing of its own. Goes through
    `SctToGeojsonService.Run`.
  - **vERAM to GeoJSON tab** - the output layout (GeoMapObject Description / Filter Index and
    Similar Attributes) and the CRC defaults source (XML / XML then card / card). Lines,
    Symbols and Text panels, shown only while the card is a source (`UsesCrcDefaults`); with the
    XML as the only source nothing on the card is required or sent. Goes through
    `VeramToGeojsonService.Run`.
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

- **Toasts** - `Shell/Toast.cs` static store; hosted bottom-right.
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
