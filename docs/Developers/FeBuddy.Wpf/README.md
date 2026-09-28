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
- **Airports, Airways, Departures, Arrivals, NAVAIDs, ARTCC Boundaries, Fixes, Wx Stations,
  Procedures, Telephony and vNAS Alias Upload are sub-services of AIRAC Service**, not top-level screens. The library code is
  `FeBuddy.Core.Application.Airac.*`; the GUI reaches each one only as a tab on the AIRAC
  Services screen. In the same way, each
  **file conversion** (DAT, SCT2 and ERAM to GeoJSON) is a tab on the File Conversions screen
  (`FeBuddy.Core.Application.Conversions.*`).
- On launch, `App.xaml.cs` starts `AppLog`'s file sink then runs
  `LaunchSequence` off the UI thread: clear `%TEMP%\FE-Buddy`, read
  `UserConfig.json`, look for FE-Buddy 2.x's GitHub token variable, UTC/internet check, version
  check, the AIRAC data pipeline (`AiracCycleDataCache` - probe/download/parse
  previous/current/next), and the News check. Results land in `AppEnvironment`; every step
  narrates itself in the Dashboard activity log. When the token variable is found, `ShellViewModel`
  shows the one-time notice about it once the window is up.

## Layout

```
Theme/                design system - the only place colours, type and control
  Palette.xaml          look are defined
  Typography.xaml
  Icons.xaml            Segoe Fluent Icons glyph code-points
  Controls.Buttons.xaml  Primary/Ghost/Subtle, Card.Toggle, Copy.Button, caption,
                         the icon toggles and the map toolbar's Toggle.Tool
  Controls.Inputs.xaml   Field, Option, Toggle.Number, Progress; the themed ComboBox
                         is implicit, so every dropdown gets the dark popup and the
                         drop-down wheel scrolling without asking for a style
  Controls.Surfaces.xaml Card (and Card.Popup), Divider, Chip (and Chip.State), nav
                         row, and the Map.* styles: overlays, list rows, swatches
  Controls.Chrome.xaml   implicit ScrollBar (thin, theme-coloured) + ToolTip
                         (dark rounded popover, soft shadow, fade-in)
  Controls.Window.xaml   implicit ChromeWindow style: every dialog window's frame
  Theme.xaml             merges the above; App.xaml merges only this

Assets/               us-states.json (reference geography, not sample data)
Behaviors/            attached properties a view opts into: FieldState (validation
                      look), WheelScroll, ComboBoxDropDownFocus, MaximizeToWorkArea
Controls/             reusable controls: Card, SectionHeader, Option, CopyButton,
                      FilterPicker (+ FilterOption), MarkdownView, MapCanvas, and
                      ChromeWindow (the base for every dialog window)
Converters/           one IValueConverter per file
Map/                  GeoJsonReader (System.Text.Json), WebMercator, ProjectedLayer
                      (a layer projected once, then cached), AiracMapLayers (the live
                      layers built from a parsed cycle), BaseMap (the US states)
  Models/               GeoPoint, GeoBounds, MapGeometry(Kind), MapLayer,
                        MapPointShape, MapHome (the home view), MapViewState
Mvvm/                 ObservableObject, RelayCommand
Shell/                app-wide services: Toast, Links, BrowserLauncher,
                      DefaultRoiStore (the one saved default ROI), OutputPreferences
                      (output folder, AIRAC cycle folder and coordinate precision,
                      read at run time)
  Models/               ToastKind
ViewModels/           ShellViewModel + one per screen; AiracSubServices is the
                      AIRAC sub-service catalogue; FileConversionsViewModel lists
                      the conversions. The map: MapViewModel (one map workspace
                      and the ROI it edits, an IRoiTarget - DefaultRoiTarget on
                      the Map page), MapLayersState (the layers every map shares)
                      with MapLayerToggle, OutputFileChoice and MapFileItem.
                      Settings: CredentialsViewModel and CredentialEditorViewModel,
                      and ConfigPages / IConfigPage (every page a settings import
                      has to reload)
  Models/               small item and row view-models and records (HealthRow,
                        FebPropertyToggle, EramClassDefault (StyleFromFeatures
                        / AsksForStyle, for a class whose Symbol style lives on
                        the Features instead of the card), VnasFileToggle,
                        SourceFileItem, AliasSourceRow (a vNAS Alias Upload
                        custom alias file row), ...)
  ServiceTabs/          the framework for a tabbed service screen:
                        TabbedServiceViewModel (the screen), ServiceTabViewModel
                        (a tab), SubServiceSettingsViewModel (a saved settings tab;
                        RemoveSubtree clears a numbered list before it is saved again),
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
                      DeparturesView, ArrivalsView, NavaidsView, ArtccBoundariesView, FixesView,
                      WxStationsView, ProceduresView, TelephonyView, VnasAliasView, DatToGeojsonView, SctToGeojsonView, EramToGeojsonView,
                      ServicePreviewTabView, ServiceRunReviewTabView), MapView (the
                      Map page: just a MapWorkspace), MapWorkspace (the one map
                      screen), Settings, Info; UpdateWindow, ConfirmWindow (Confirm /
                      Cancel, or a third choice between them; a long message
                      scrolls), CredentialEditorWindow, RoiPickerWindow (a
                      MapWorkspace in a window)
  Cards/                the cards every GeoJSON sub-service tab shares, RunCard
                        (the run button at the foot of the Preview Settings tab and
                        of every conversion tab) and SourceFilesCard (a conversion's
                        folder or picked files); each binds to its tab through one
                        ServiceTabs interface. The conversions turn on the CRC ERAM
                        Defaults card's Include boxes (ShowInclude); the sub-services
                        choose on their Upload to vNAS card instead
  Models/               ConfirmChoice
```

Namespaces follow folders (`FeBuddy.Wpf.ViewModels.ServiceTabs`), and every file
holds one type. A type that only carries data (a record, an enum, a small row)
goes in the nearest `Models/` folder; a class that does work sits at the folder
root - the same rule as `FeBuddy.Core`.

### Where do I put...

- **A new sub-service** (say, Preferred Routes): an entry in `ViewModels/AiracSubServices.cs`,
  a `PreferredRoutesViewModel` in `ViewModels/` deriving from `GeojsonSubServiceViewModel` and
  implementing `ISubServiceRunTarget` (its `OutputFiles` lists the files its settings
  write, by the file keys Core's `*OutputFiles` class names), a `PreferredRoutesView` in `Views/`
  built from the shared cards, and its settings block on `AiracServiceSettings` in Core. `FixesViewModel`
  / `FixesView` are a real example of this shape to copy from.
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
- **A test for the app's logic:** `FeBuddy.UnitTests/Wpf/`, in folders mirroring these (the app's
  internals are visible to the tests). A test that creates a control runs its body through
  `StaThread.Run`, since WPF controls need a thread of their own. The map's math, GeoJSON reader,
  home view, ROI view-model and `MapCanvas` are covered today.

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
    Airways, Departures, Arrivals, NAVAIDs, ARTCC Boundaries, Fixes, Wx Stations, Procedures,
    Telephony, vNAS Alias Upload. Adding one is a catalogue entry plus a tab view-model; one whose backend is not built
    yet opens a `PlaceholderSubServiceView` and contributes nothing to a run.
  - **Sub-service tabs** - each is a `GeojsonSubServiceViewModel` (Save /
    Undo-last-save / dirty, plus the outputs, file choices, `feb.*` properties, ROI
    override, vNAS files and CRC ERAM defaults every GeoJSON sub-service shares). The
    cards run: Outputs, What Files, FE-Buddy Properties, the tab's own cards, Region of
    Interest, then **Upload to vNAS** (a box per file the tab writes, from its
    `OutputFiles`, and which of those get CRC-ERAM defaults) and **CRC ERAM Defaults**,
    which shows only the panels and class columns the chosen files need. The Airways
    tab adds: Output mode + per-kind emit toggles, designation include/exclude (from
    the cycle's `AWY_ID`s), buffer, the aliases section (`Airways.txt` + ROI scope),
    and the antimeridian toggle. Its result panel shows files + feature counts, the
    alias line count, the excluded-airway count, and messages grouped by airway and
    presented by level (info collapsed behind a count). The NAVAIDs tab adds: NAVAID
    Types (one tick per type present in the cycle, all on by default - unticking one
    drops it from GeoJSON and the alias file alike), File Layout (merged, or one
    Symbols/Text pair per type), and, once the Symbols file is chosen for CRC-ERAM
    defaults under the merged layout, NAVAID Symbol Style (style each NAVAID by its
    type - with a Fan marker style drop-down - or one style for the whole file).
    NAVAIDs is also the first sub-service with no Lines file at all:
    `GeojsonSubServiceViewModel.EmitKeys.Lines` is nullable so a sub-service can say
    it has no Lines kind, and `GeojsonFilesCard.ShowLines` hides the Lines box for it.
    The ARTCC Boundaries tab adds: ARTCCs (one tick per ARTCC with boundary data in
    the cycle, none ticked means all - the same pattern as the Departures/Arrivals
    ARTCC filter) and File Layout (High and Low, High/Low/Unlimited, or one file per
    ARTCC and altitude), plus the antimeridian toggle. It is the first sub-service
    with no alias file at all: `GeojsonSubServiceViewModel.HasAliasFile` is virtual
    and `false` here, so `GenerateAliasFile` is never shown, saved or sent, and every
    one of its `EmitKeys` is null too, since it always writes Lines only with no
    choice to make. Its CRC ERAM Defaults classes come from the cycle's own data
    (one per ARTCC and altitude, under the per-ARTCC layout) rather than a fixed
    list, added after construction with `AddCrcRows`. The Fixes tab adds: File
    Layout (All, by fix use, by chart, or by chart and fix use), a Fix Uses
    tick-list (one per fix use present, `FixUse` layout only), a Charts tick-list
    (one per NASR chart present, `Chart` layout only), and a Combinations list -
    add, edit and delete a chart + fix use pair - for the `ChartAndFixUse` layout.
    Like NAVAIDs it has no Lines file (`EmitKeys.Lines` is null); like ARTCC
    Boundaries it has no alias file at all (`HasAliasFile` is `false`), so
    `GenerateAliasFile` is never shown, saved or sent either. Its CRC ERAM Defaults
    classes also come from the cycle's own data (one per fix use, chart, or chart +
    fix use combination present, depending on File Layout), added the same way with
    `AddCrcRows`. The Wx Stations tab adds: a Station Data card - where the list
    comes from (aviationweather.gov's own station list, not the NASR cycle), which
    stations are included, and a status line showing FE-Buddy's kept copy's date, or that there is
    no copy yet (every run downloads the latest list first, whichever cycle you run, and only falls
    back to the kept copy if that fails; missing data no longer blocks the run). Like NAVAIDs and
    Fixes it has no Lines file; like ARTCC Boundaries and Fixes it has no alias file at all. Unlike
    Fixes, its CRC ERAM Defaults have a single fixed class (`Wx`), never rows added from
    the cycle's own data - there is no File Layout choice to split it by.
  - **The Procedures tab** writes no GeoJSON at all, but does have an alias file, so it still builds
    on `GeojsonSubServiceViewModel` for the alias-file plumbing and its region-of-interest card:
    every `EmitKeys` entry is null (there is nothing to emit) but `HasAliasFile` is `true`, and the
    view has no GeoJSON Files, FE-Buddy Properties or CRC ERAM Defaults cards - nothing it writes
    carries CRC-ERAM defaults. Its own cards: **Outputs** (which of `Procedure_Changes.md`,
    `Procedures.json` and the alias file, `Faa_Chart_Recall.txt`, to write - at least one; the
    facility/airport/procedure/chart-type choices below never limit the alias file, only the
    documents), **d-TPP Data** (where
    the data comes from, the selected cycle's d-TPP Metafile status - airports, procedures,
    downloaded date, or "not published yet" - and whether a deleted procedure can be linked to the
    previous cycle; like the Wx Stations tab's Station Data card, this never blocks the run),
    **Facilities** (a tick per ARTCC, the same pattern as Departures/Arrivals/ARTCC Boundaries, but
    for whole-airport inclusion rather than a narrowing filter), **Airports** (list by FAA/ICAO ID,
    plus "Also include every airport inside the region of interest"), **Procedures at Any Airport**
    (a procedure name, included wherever it is charted), **Airport + Procedure** (add/edit/delete
    pairs), **Chart Types** (which d-TPP `chart_code` values a whole included airport contributes -
    every kind but the volume-wide minimums, hot spot and LAHSO sheets is on by default),
    **Procedures.json Fields** (shown only while Procedures.json is on), and **Region of Interest**
    (the same override-or-default box as every other sub-service, but used here as an inclusion
    source rather than a clip - Procedures writes no GeoJSON to clip). The Facilities, Airports,
    Procedures at Any Airport, Airport + Procedure, Chart Types and Region of Interest cards all
    grey out while neither document is on (`GeneratesDocument`), since the alias file needs no
    selection at all; its own **Upload to vNAS** card (the alias file only) shows only while the
    alias file is on. It is the ninth sub-service in the catalogue. Its Review tab result names the
    alias file's own command and airport counts
    (`ProcedureServiceResult.AliasCommandCount`/`AliasAirportCount`), alongside the documents'
    airport/new/changed/deleted counts.
  - **The Telephony tab** writes no GeoJSON either, but like Procedures it keeps `HasAliasFile`
    `true`: `EmitKeys` is `(null, null, null)` and `EnabledOutputCount` is a fixed `1`, so its
    **Outputs** card just names `Telephony.txt` as the tab's one output, with nothing to turn on or
    off. Its own **Telephony Data** card names where the FAA pages come from (JO 7340.2, Chapter 3,
    Sections 1 and 4) and shows FE-Buddy's kept copies' date - the same idea as the Wx Stations tab's
    Station Data card, downloaded fresh by every run, and missing data doesn't block the run here
    either. There is no Region of Interest card at all (`NoDefaultRoiHint` explains why, though
    nothing in the tab ever shows it): Telephony covers every operator regardless of area. Its only
    other card is **Upload to vNAS**, for `Telephony.txt`. It is the tenth sub-service in the
    catalogue. Its Review tab result names the alias file's command count and how many show more
    than one operator (`TelephonyServiceResult.AliasCommandCount`/`MergedCommandCount`).
  - **The vNAS Alias Upload tab** (`VnasAliasViewModel`, key `VnasAlias`) is not a
    `GeojsonSubServiceViewModel`: it derives from `SubServiceSettingsViewModel` and implements
    `ISubServiceRunTarget` itself, and uses none of the shared cards. **Outputs** names
    `Upload_to_vNAS\vNAS_Alias.txt` and how it is laid out. **FE-Buddy Alias Files** lists every
    sub-service that can write an alias file (`SubServiceDescriptor.AliasFileName` in the
    catalogue) and whether it goes into `vNAS_Alias.txt` as the other tabs stand now: not selected,
    its alias file turned off, not ticked on its Upload to vNAS card, or added - with **Open tab**.
    `AiracServiceViewModel` hands it the other tabs (`AttachToService`) and has it re-read them
    (`RefreshFeBuddyAliasFiles`) whenever another tab is shown or the selection changes, and
    before a run. **Custom Alias Files** lists the
    facility's own alias files (`AliasSourceRow`), merged in order - move up/down, remove, **Add
    file…** / **Browse…** for a file on this PC, **Add web address** for one on the web. A web
    address has a credential drop-down ("None" plus every saved credential, refreshed on
    `CredentialStore.Changed`) and **New credential…**; a row with none is offered "Use
    <credential>, like file N" when an earlier row's credential is allowed on the same website (for
    GitHub, `api.github.com` - `GitHubFileUrl.ToContentsApi`). **Check** reads the file now through
    `AliasSourceLoader` and shows its command count or the problem; a result that arrives after the
    row's address or credential changed is dropped, and anything unexpected is shown on the row
    rather than thrown. Validation mirrors `VnasAliasSettingsParser` (a full path; an http(s)
    address with no sign-in written into it - `UrlSecrets`; not a GitHub repository or folder page;
    a credential only to https, or to a GitHub file address, which is downloaded over https), and
    the tab is invalid only when there is nothing to merge; a file or credential not on this PC, or
    a Credential Manager that cannot be read, is a non-blocking notice. `WriteToConfig` removes the `Sources` subtree before writing the list
    again. It is the eleventh and last sub-service in the catalogue. Its Review tab result gives
    `vNAS_Alias.txt`'s custom and FE-Buddy command counts (`AiracServiceResult.VnasAlias`).
  - **Preview Settings tab** - present once at least one sub-service is selected:
    every tab's settings as label/value rows (the General section names the run's
    `AIRAC_<cycle>` folder), notices naming any unsaved or invalid tab, and the single
    **Run AIRAC Service** button, which goes through `AiracService.RunAsync` - after
    asking, if the cycle folder already has files, whether to overwrite them or delete
    them first.
  - **Review tab** - appears once a run starts: the live step feed, errors,
    advisories, each sub-service's results, the files written, and **Open output
    folder** for the run's cycle folder. The rail sets it apart from the settings tabs
    with a divider (`IsSetApart`, on every tabbed screen).
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
  input files are not saved settings. The action bar hides Previous / Next here
  (`HasStepNavigation` is off), since the tabs are standalone rather than steps.
  Nothing here waits for AIRAC data.
  - **Conversion tabs** - each is a `FileConversionTabViewModel`: source (a saved
    folder, or files picked one or several at a time and not saved) and the CRC ERAM
    defaults for the kinds it writes.
  - **DAT to GeoJSON tab** - the Lines panel only, plus the cropping distance. Goes
    through `DatToGeojsonService.Run`.
  - **SCT2 to GeoJSON tab** - Lines and Labels panels, nothing of its own. Goes through
    `SctToGeojsonService.Run`.
  - **ERAM to GeoJSON tab** - reads an ERAM adaptation export's `Geomaps.xml`; its folder
    summary counts only Geomaps files (`IsSourceFile`), so the whole unzipped export can be the
    source folder. The output layout (Object Type and Map Group / Filter Index and Similar
    Attributes) and the CRC defaults source (XML / XML then card / card). Lines, Symbols and Text
    panels, shown only while the card is a source (`UsesCrcDefaults`); with the XML as the only
    source nothing on the card is required or sent. Goes through `EramToGeojsonService.Run`.
- **Dashboard** - the verbatim description box + Discord link + next-cycle line,
  the News feed (from `NewsService`), and a live activity-log viewer over `AppLog`
  (filter chips with counts, minimizable).
- **Map** - `MapView`, which is just a `MapWorkspace` editing the saved default ROI; see
  [The map](#the-map). No ruler, no CRC display visualiser, no sample layers.
- **Settings** - Import… / Export… beside Save (`UserConfigTransfer`: a plan shown in
  `ConfirmWindow` first, then every open `IConfigPage` reloads - `ConfigPages`), then the cards:
  Facility Profile (one facility from the parsed cycle, default output dir - the Desktop until
  one is saved - + FE-Buddy_Output toggle, with the cycle folder a run would write to), Default
  Region of Interest (`RoiPickerWindow`), GeoJSON Files (feb.* description, Maximum Coordinate
  Precision 5/6/7 dp, File Layout), Credentials (`CredentialsViewModel`, `CredentialEditorWindow`),
  FE-Buddy's GitHub Requests (the GitHub token FE-Buddy's own requests use), and Updates (the four
  channels with their tooltips, "check now" and "get the latest stable installer"). Everything but
  Credentials persists to `UserConfig.json` with the page's Save; credentials live in Windows
  Credential Manager and are saved as they change (see [Credentials](../Credentials.md)).
- **Info** - Manual, Change log, Issues & requests as real links (About deleted).

### Shell extras

- **Toasts** - `Shell/Toast.cs` static store; hosted bottom-right.
- **Border chrome** - FE-BUDDY tooltip shows update state only; the version chip is
  a button that opens `UpdateWindow` when an update exists; the AIRAC status
  readout sits top-centre and narrates the launch pipeline.
- **Zulu clock** in the status bar; **collapsible nav rail** (232 ⇄ 60).

### The map

**One map screen.** `Views/MapWorkspace` is the map on the Map page (`MapView`) and in every map
popup (`RoiPickerWindow`, for Settings' default ROI and each sub-service's "Pick on map…"). Its
view-model, `MapViewModel`, holds the ROI being edited and where a saved one goes - an
`IRoiTarget`: `DefaultRoiTarget` on the Map page (saved at once, through `DefaultRoiStore`), the
picker's own target in a popup (handed back to the caller as the popup closes). Everything else on
the map lives in `MapLayersState.Shared`, one instance for the whole app, so a popup shows the same
layers as the Map page: the live AIRAC layers (`AiracMapLayers`, built from the parsed cycle when
switched on), the run-output files picked with the output picker (`AiracOutputCatalog` lists a
cycle's folder, off the UI thread), the user's own files, and the home view. Its choices are saved
under `Services.MapService` (see [UserConfig.json reference](../UserConfig-Reference.md#servicesmapservice)).
A map that closes leaves its view in `MapLayersState.LastView`, so the next one opens there.

**The control.** `Controls/MapCanvas` is a from-scratch vector map: Web-Mercator projection
(`Map/WebMercator`), a pan (drag) / zoom (wheel) viewport, and `StreamGeometry` into
`DrawingVisual`s. **No tiles, no network, no map SDK.** It takes a base `MapLayer` (the US state
outlines) plus the shared layer list, which it follows weakly (`CollectionChangedEventManager`) so
a closed popup's map is not kept alive by it. The world repeats side by side: every layer is
projected once into world units (`Map/ProjectedLayer`, lines unwrapped across the 180th meridian)
and drawn once per copy of the world in view, and framing covers shapes on both sides of 180° the
short way round (`ProjectedLayer.Covering`). A layer below its `MinZoom`, or with more points or
labels in view than can usefully be drawn, waits until the user zooms in (`DensityHint`). The ROI
box is two-way (`Roi`): drawn, moved and resized while `RoiEditing` is on, or with Shift + drag at
any time (`RoiQuickDrawn`). Every file on the map, from a run's output or the user's own, is read
by `Map/GeoJsonReader`, which says why a file cannot be drawn.

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
