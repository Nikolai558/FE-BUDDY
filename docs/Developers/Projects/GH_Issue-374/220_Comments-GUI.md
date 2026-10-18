# 220 · Code comments and XML docs: GUI

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

Comments and XML docs in FeBuddy.Wpf. Grouped by folder. Session [S8](020_Work-Plan.md#s8).

**97 findings** (2 Med, 95 Low). Quote the ID when you comment on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374).

## Summary

| ID | Kind | Sev | Finding | Status | Session |
|---|---|---|---|---|---|
| [W7-5](#w7-5) | Minor | Low | The launch sequence's catch-all logs only the message, losing what threw and where | Open | S8 |
| [W7-13](#w7-13) | Wrong doc | Low | csproj's InternalsVisibleTo comment names only the map; the tests use internals all over the app | Open | S8 |
| [W7-28](#w7-28) | Minor | Low | csproj restates two SDK defaults | Open | S8 |
| [W6-15](#w6-15) | Minor | Low | BringIntoView says "Convert card"; everywhere else calls it the Run card | Open | S8 |
| [W6-27](#w6-27) | Minor | Low | Two helpers are written out more than once | Open | S8 |
| [W6-12](#w6-12) | Wrong doc | Low | ChromeWindow names three dialogs, but eleven windows derive from it | Open | S8 |
| [W6-11](#w6-11) | Wrong doc | Low | FilterOption.SetSilently does report the change to bindings | Open | S8 |
| [W6-17](#w6-17) | History wording | Low | FilterPicker's remarks describe the control as a replacement for something earlier | Open | S8 |
| [W6-18](#w6-18) | History wording | Low | "(issue #327)" in two doc comments points at an issue number | Open | S8 |
| [W6-13](#w6-13) | Wrong doc | Low | The MapCanvas Ctrl+click comment says a Ctrl+drag always pans | Open | S8 |
| [W6-14](#w6-14) | Wrong doc | Low | The MapCanvas summary says every held-back layer is named in DensityHint | Open | S8 |
| [W6-28](#w6-28) | Minor | Low | Some public members of internal types have no XML docs | Open | S8 |
| [W6-9](#w6-9) | Wrong doc | Low | FileNamesAsCodeConverter's doc says it is only for the Review tab's run messages | Open | S8 |
| [W6-10](#w6-10) | Wrong doc | Low | StringToVisibilityConverter says "non-empty" but treats whitespace as empty | Open | S8 |
| [W6-29](#w6-29) | Minor | Low | The AiracLayerKind enum is not in Map/Models | Agreed | S6 |
| [W6-26](#w6-26) | Minor | Low | BaseMap.Load hides why a base layer failed to load | Open | S8 |
| [W6-16](#w6-16) | Minor | Low | GeoJsonReader.Read's `<returns>` leaves out the Multi* types and skipped shapes | Open | S8 |
| [W7-8](#w7-8) | Stale doc | Low | DefaultRoiStore names a settings file that no longer exists, and `Changed`'s doc misses `NotifyReloaded` | Open | S8 |
| [W7-9](#w7-9) | Wrong doc | Low | Links' summary lists only three places; the links are used in six | Open | S8 |
| [W7-10](#w7-10) | History wording | Low | Feature number in two summaries | Open | S8 |
| [W7-12](#w7-12) | Minor | Low | Five window-placement data records sit in a file named after no type, outside Shell/Models/ | Agreed | S6 |
| [W7-11](#w7-11) | Minor | Low | Comment on the showCmd constants says "minimized" but one is the maximized value | Open | S8 |
| [W7-21](#w7-21) | Wrong doc | Low | Controls.Buttons.xaml header: "three intents", Button.Icon.Small missing, Toggle.Icon's size | Open | S8 |
| [W7-17](#w7-17) | History wording | Low | "now" in the ComboBox header narrates an earlier state | Open | S8 |
| [W7-18](#w7-18) | Wrong doc | Low | TextBox comment promises explicit `Field` call sites that don't exist, and that would lose the error look | Open | S8 |
| [W7-19](#w7-19) | Wrong doc | Low | Controls.Surfaces.xaml header lists a style that doesn't exist and gives Nav.Item the wrong type | Open | S8 |
| [W7-20](#w7-20) | Wrong doc | Low | Controls.Window.xaml header: not implicit, stale dialog list, wrong titlebar colour claim | Open | S8 |
| [W7-23](#w7-23) | Wrong doc | Low | Palette says no colour is hard-coded anywhere else; several are | Open | S8 |
| [W7-24](#w7-24) | History wording | Low | Typography header narrates the fonts the theme was first drawn with | Open | S8 |
| [W2-12](#w2-12) | Wrong doc | Med | Duplicate review claims CRC runs the first copy of a command | Agreed | S8 |
| [W2-31](#w2-31) | History wording | Low | "every included sub-service that has a backend" | Open | S8 |
| [W2-32](#w2-32) | Wrong doc | Low | "The only sub-service-specific lines in the run" / "the only place a sub-service is registered" | Open | S8 |
| [W2-38](#w2-38) | Minor | Low | Naming and small tidy-ups | Open | S8 |
| [W2-34](#w2-34) | History wording | Low | Key comments narrate the sub-services' earlier names | Open | S8 |
| [W2-30](#w2-30) | Stale doc | Low | Airports summary still names the "ROI override" | Open | S8 |
| [W1-24](#w1-24) | Wrong doc | Low | Airways' resync comment misdescribes the construction snapshot | Open | S8 |
| [W1-28](#w1-28) | Minor | Low | Airways and Departures hard-code their alias file names in the run summary | Open | S8 |
| [W2-29](#w2-29) | Stale doc | Low | ARTCC Boundaries says it alone has "no Outputs card" | Open | S8 |
| [W1-22](#w1-22) | History wording | Low | "Concatenate Aliases, as the code still calls it" | Open | S8 |
| [W1-27](#w1-27) | Stale doc | Low | "UserConfig.json" in Add Custom Aliases' remarks | Open | S8 |
| [W3-32](#w3-32) | Wrong doc | Low | Odd and inaccurate comment in CredentialsViewModel | Open | S8 |
| [W3-33](#w3-33) | Stale doc | Low | Credentials card doc names the old single settings file | Open | S8 |
| [W3-27](#w3-27) | Wrong doc | Low | Dashboard comment says a Dashboard is made per navigation | Open | S8 |
| [W2-35](#w2-35) | History wording | Low | Issue number in class summaries | Open | S8 |
| [W2-36](#w2-36) | History wording | Low | ERAM page justifies behaviour by the "original tool" | Open | S8 |
| [W2-28](#w2-28) | Stale doc | Low | File Names remarks: when the list is read, and a stale comparison | Open | S8 |
| [W1-23](#w1-23) | Stale doc | Low | Fixes remarks say having no Outputs card is unusual | Open | S8 |
| [W3-36](#w3-36) | Wrong doc | Low | IConfigPage mentions only imports | Open | S8 |
| [W3-29](#w3-29) | Wrong doc | Low | Output picker docs: summary example and All/None scope | Open | S8 |
| [W3-25](#w3-25) | History wording | Low | Issue numbers that only narrate history | Open | S8 |
| [W3-37](#w3-37) | Minor | Low | Small doc mismatches in NavItem and InfoViewModel | Open | S8 |
| [W1-21](#w1-21) | History wording | Low | Issue and feature numbers that only narrate history | Open | S8 |
| [W1-26](#w1-26) | Wrong doc | Low | Small mismatches in Procedure Charts' docs and texts | Open | S8 |
| [W1-29](#w1-29) | Minor | Low | Procedure Charts reads and normalises the Settings facility in five places | Open | S8 |
| [W3-35](#w3-35) | Wrong doc | Low | Reset/Uninstall "settings" lists leave out the window placement | Open | S8 |
| [W3-30](#w3-30) | Wrong doc | Low | FacilityOption.Display example doesn't match NASR names | Open | S8 |
| [W3-34](#w3-34) | Wrong doc | Low | Settings page summary/remarks slightly off | Open | S8 |
| [W3-26](#w3-26) | History wording | Low | "As in 2.x" explains the DEV suffix by old behaviour | Open | S8 |
| [W3-28](#w3-28) | Wrong doc | Low | DescribeUnfinishedWork comment names only the update | Open | S8 |
| [W3-38](#w3-38) | Minor | Low | Shell doc examples don't match the output; status dates use the current culture | Open | S8 |
| [W7-25](#w7-25) | Minor | Low | Nav glyphs are hard-coded in ShellViewModel instead of Icons.xaml | Open | S8 |
| [W1-25](#w1-25) | Wrong doc | Low | RefreshTelephonyData's summary lists too few callers and leaves out what it does | Open | S8 |
| [W3-31](#w3-31) | Wrong doc | Low | Update window docs assume updating, not going back | Open | S8 |
| [W3-39](#w3-39) | Minor | Low | Release page URL hard-coded again in UpdateWindowViewModel | Open | S8 |
| [W2-33](#w2-33) | Wrong doc | Low | Two small doc gaps (callers, states) | Open | S8 |
| [W4-32](#w4-32) | History wording | Low | Feature issue numbers that only narrate history | Open | S8 |
| [W4-24](#w4-24) | Stale doc | Low | "None ticked means every ARTCC" predates the Area card | Open | S8 |
| [W4-31](#w4-31) | Wrong doc | Low | EramClassDefault.ClassName lists the classes but misses several | Open | S8 |
| [W4-30](#w4-30) | Wrong doc | Low | FebPropertyToggle's list of option lists is incomplete | Open | S8 |
| [W4-23](#w4-23) | Wrong doc | Low | FixGroupToggle and NavaidTypeToggle say the selected set is saved; the unselected set is | Open | S8 |
| [W4-37](#w4-37) | Minor | Low | SavedDuplicateChoiceRow.Line example doesn't match the text it builds | Open | S8 |
| [W4-27](#w4-27) | Wrong doc | Low | FileConversionTabViewModel docs leave out ERAM and the Symbols flag | Open | S8 |
| [W4-29](#w4-29) | Wrong doc | Low | OnOutputsChanged remark gives the wrong example | Open | S8 |
| [W4-35](#w4-35) | Minor | Low | CRC-row config plumbing duplicated in the two base classes | Open | S8 |
| [W4-33](#w4-33) | History wording | Low | ISubServiceRunTarget remark narrates an earlier run method | Open | S8 |
| [W4-26](#w4-26) | Wrong doc | Low | Tab/run docs describe only the AIRAC screen | Open | S8 |
| [W4-25](#w4-25) | Wrong doc | Low | SubServiceSettingsViewModel's class docs: who derives from it, and how WriteToConfig writes | Open | S8 |
| [W4-34](#w4-34) | Minor | Low | "menu" for what everything else calls a tab | Open | S8 |
| [W4-28](#w4-28) | Wrong doc | Low | SubServiceDescriptor claims to be the whole registration surface | Open | S8 |
| [W4-36](#w4-36) | Minor | Low | Files outside the Models/ rule in ServiceTabs | Agreed | S6 |
| [W5-31](#w5-31) | Minor | Low | Esc doesn't close most dialogs | Open | S8 |
| [W5-26](#w5-26) | Wrong doc | Low | InfoView header leaves out the FAQ card | Open | S8 |
| [W5-32](#w5-32) | Minor | Low | MapView calls the Map page "the Map Service page" | Open | S8 |
| [W5-25](#w5-25) | Wrong doc | Low | RoiPickerWindow.Pick's `title` example doesn't match either caller | Open | S8 |
| [W5-36](#w5-36) | Minor | Low | Preview Settings comment uses a planning term | Open | S8 |
| [W5-28](#w5-28) | Wrong doc | Low | Review tab comment says the "no errors" card is for a run that "reported nothing" | Open | S8 |
| [W5-27](#w5-27) | Wrong doc | Low | SettingsView header says everything else goes to the profile | Open | S8 |
| [W5-34](#w5-34) | Minor | Low | "Open the Releases Page" is bound to a command named RollbackCommand | Open | S8 |
| [W5-29](#w5-29) | Wrong doc | Low | ShellWindow header calls the collapsible nav rail "fixed" | Open | S8 |
| [W5-33](#w5-33) | Minor | Low | UpdateWindow.xaml has no header comment | Open | S8 |
| [D1b-24](#d1b-24) | Stale doc | Med | "UserConfig.json" is still named as the settings file, including in GUI text | Agreed | S8 |
| [W5-23](#w5-23) | Wrong doc | Low | AreaCard's Outputs and RoiContent docs miss Procedures and Airways | Open | S8 |
| [W5-24](#w5-24) | Wrong doc | Low | CrcDefaultsPanel header lists too few tabs with several classes | Open | S8 |
| [W5-20](#w5-20) | History wording | Low | Issue numbers in comments that only label a feature | Open | S8 |
| [W5-21](#w5-21) | Stale doc | Low | FebPropertiesCard's usage example binds IsEnabled, which no view does | Open | S8 |
| [W5-35](#w5-35) | Minor | Low | SourceFilesCard uses a literal ✕ glyph instead of the icon token | Open | S8 |
| [W5-30](#w5-30) | Wrong doc | Low | SummaryCard header says "every AIRAC sub-service tab" | Open | S8 |

## FeBuddy.Wpf

<a id="w7-5"></a>

### W7-5 · Minor · Low · The launch sequence's catch-all logs only the message, losing what threw and where

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/App.xaml.cs:91-96`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/App.xaml.cs#L91-L96)

**Says:** `AppLog.Error("Launch", $"Launch sequence threw unexpectedly: {ex.Message}");`

**Problem:** The comment says this catch exists for "a bug there"; the message alone (e.g. "Object reference not set to an instance of an object.") gives no type or stack to find it.

**Proposed:** `AppLog.Error("Launch", $"Launch sequence threw unexpectedly: {ex}");` (or `{ex.GetType().Name}: {ex.Message}` plus the stack at Debug).

<a id="w7-13"></a>

### W7-13 · Wrong doc · Low · csproj's InternalsVisibleTo comment names only the map; the tests use internals all over the app

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/FeBuddy.Wpf.csproj:61-62`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/FeBuddy.Wpf.csproj#L61-L62)

**Says:** `<!-- The map's internal math (WebMercator, ProjectedLayer, ...) is unit tested directly. -->`

**Problem:** Tests also reach `DefaultRoiStore.FilterKey/SwLatKey/…` (UserConfigLayoutTests.cs:131-135), `BaseMapSettings`' keys, `ShellViewModel`'s `DescribeAiracData`/`SummarizeHealth`, `MapCanvas.HitTest`, and more.

**Proposed:** `<!-- The unit tests reach the app's internals: the map's math (WebMercator, ProjectedLayer), settings keys, and view-model helpers. -->`

<a id="w7-28"></a>

### W7-28 · Minor · Low · csproj restates two SDK defaults

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/FeBuddy.Wpf.csproj:44-46`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/FeBuddy.Wpf.csproj#L44-L46)

**Says:** `<TreatWarningsAsErrors>false</TreatWarningsAsErrors>` / `<EnableNETAnalyzers>true</EnableNETAnalyzers>`

**Problem:** The comment matches the settings, but both are already the defaults for net10.0, so the lines change nothing (no other project in the solution sets them). Note the line-21 comment "unused usings are reported (IDE0005)" is true only in the IDE: `.editorconfig` sets IDE0005 to `suggestion` and no project sets `EnforceCodeStyleInBuild`, so a build never reports it.

**Proposed:** Either delete lines 44-46, or keep them as documentation. Optionally reword line 21: `<!-- Every public member must be documented (CS1591); also lets the IDE flag unused usings (IDE0005). -->`

## FeBuddy.Wpf/Behaviors

<a id="w6-15"></a>

### W6-15 · Minor · Low · BringIntoView says "Convert card"; everywhere else calls it the Run card

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Behaviors/BringIntoView.cs:11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Behaviors/BringIntoView.cs#L11)

**Says:** `A conversion's results appear under its Convert card`

**Problem:** The card is `cards:RunCard`. FileConversionsView.xaml:170 says "under its Run card", and Code-Structure.md says "Run card". "Convert" is only the button's label.

**Proposed:** `A conversion's results appear under its Run card, ...`

<a id="w6-27"></a>

### W6-27 · Minor · Low · Two helpers are written out more than once

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Behaviors/InlineCode.cs:216-223`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Behaviors/InlineCode.cs#L216-L223); [`FeBuddy/FeBuddy.Wpf/Controls/AliasGuideDocumentView.cs:366-373`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/AliasGuideDocumentView.cs#L366-L373); [`FeBuddy/FeBuddy.Wpf/Controls/MarkdownView.cs:302-306`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/MarkdownView.cs#L302-L306); [`FeBuddy/FeBuddy.Wpf/Controls/MapCanvas.cs:1408-1409`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/MapCanvas.cs#L1408-L1409)

**Says:** Three copies of `new Hyperlink(...) { ToolTip = url }; link.SetResourceReference(TextElement.ForegroundProperty, "Brush.Accent"); link.Click += (_, _) => BrowserLauncher.Open(url);`. `MapCanvas.Format(lat, lon)` duplicates `MapFeatureCard.Format(GeoPoint)` (same `0.00000` format; that doc even says "as the map's cursor read-out shows one").

**Problem:** Code-Structure's "Never copy it": the link look, or the coordinate format, can drift between the copies.

**Proposed:** Make `InlineCode.Link(Inline content, string url)` internal and call it from AliasGuideDocumentView and MarkdownView.BuildInline. Use one coordinate formatter, e.g. `MapFeatureCard.Format(new GeoPoint(lat, lon))` from MapCanvas, or a `GeoPoint.ToDisplayString()`.

## FeBuddy.Wpf/Controls

<a id="w6-12"></a>

### W6-12 · Wrong doc · Low · ChromeWindow names three dialogs, but eleven windows derive from it

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Controls/ChromeWindow.cs:14-15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/ChromeWindow.cs#L14-L15)

**Says:** `The child dialogs (ROI picker, Confirm, Update) derive from this so they match the shell.`

**Problem:** The window types that derive from it are AliasGuideFormat, Confirm, CredentialEditor, DuplicateAliasReview, ImportSettings, ProfileName, Reset, RoiPicker, Uninstall, Update and Welcome. The sentence reads as a complete list.

**Proposed:** `Every child window (Confirm, Update, the ROI picker, ...) derives from this so it matches the shell.`

<a id="w6-11"></a>

### W6-11 · Wrong doc · Low · FilterOption.SetSilently does report the change to bindings

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Controls/FilterOption.cs:31-33`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/FilterOption.cs#L31-L33)

**Says:** `/// <summary>Sets the state without reporting the change, used while syncing from the value.</summary>`

**Problem:** `SetProperty` raises `PropertyChanged` (so the toggle updates); it only skips the `onChanged` callback to the picker.

**Proposed:** `Sets the state without calling back to the picker (the toggle still updates), used while syncing from the value.`

<a id="w6-17"></a>

### W6-17 · History wording · Low · FilterPicker's remarks describe the control as a replacement for something earlier

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Controls/FilterPicker.xaml.cs:18-20`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/FilterPicker.xaml.cs#L18-L20)

**Says:** `<see cref="Value"/> is the comma-separated string the settings block and <c>UserConfig</c> already use, so nothing downstream changes: the control only replaces how it is typed.`

**Problem:** "already use / nothing downstream changes / only replaces how it is typed" describes a change from an earlier text box, not the current design.

**Proposed:** `<see cref="Value"/> is the comma-separated string the settings block and <c>UserConfig</c> use. Values come back sorted and de-duplicated, which also makes a saved config diff cleanly.`

<a id="w6-18"></a>

### W6-18 · History wording · Low · "(issue #327)" in two doc comments points at an issue number

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Controls/MapCanvas.Inspect.cs:10`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/MapCanvas.Inspect.cs#L10); [`FeBuddy/FeBuddy.Wpf/Map/Models/MapFeature.cs:7`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Map/Models/MapFeature.cs#L7)

**Says:** `/// Ctrl + click on the map (issue #327): what is under the pointer, ...`

**Problem:** The issue number only says which issue asked for the feature; it doesn't explain a design choice made because of a bug. House rule: comments don't point at planning or task numbers.

**Proposed:** Delete ` (issue #327)` in each.

<a id="w6-13"></a>

### W6-13 · Wrong doc · Low · The MapCanvas Ctrl+click comment says a Ctrl+drag always pans

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Controls/MapCanvas.cs:695`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/MapCanvas.cs#L695)

**Says:** `// A Ctrl + click, not a Ctrl + drag (which pans, as any drag does).`

**Problem:** While `RoiEditing` is on, a Ctrl+left-drag goes through `BeginLeftDrag` and draws, moves or resizes the box (lines 561-576). Only Shift is checked before that.

**Proposed:** `// A Ctrl + click, not a Ctrl + drag (which pans, or edits the box while RoiEditing is on, as any drag does).`

<a id="w6-14"></a>

### W6-14 · Wrong doc · Low · The MapCanvas summary says every held-back layer is named in DensityHint

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Controls/MapCanvas.cs:35-37`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/MapCanvas.cs#L35-L37)

**Says:** `a layer below its <see cref="MapLayer.MinZoom"/> ... is held back and named in <see cref="DensityHint"/>`

**Problem:** A layer with `QuietBelowMinZoom` (the AIRAC Runways layer) is held back below its MinZoom but not named (lines 953-960).

**Proposed:** Append `(unless it is <see cref="MapLayer.QuietBelowMinZoom"/>)` after "below its MinZoom".

<a id="w6-28"></a>

### W6-28 · Minor · Low · Some public members of internal types have no XML docs

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Controls/MapCanvas.cs:1453-1458`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Controls/MapCanvas.cs#L1453-L1458) (`WorldRect`'s parameters, `Shifted`, `Normalized`); [`FeBuddy/FeBuddy.Wpf/Map/ProjectedLayer.cs:279`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Map/ProjectedLayer.cs#L279) (`ProjectedRun.From`); [`FeBuddy/FeBuddy.Wpf/Behaviors/InlineCode.cs:228`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Behaviors/InlineCode.cs#L228) (`CodeSize.Instance`)

**Says:** `public WorldRect Shifted(int worlds) => ...` / `public static ProjectedRun From(IReadOnlyList<GeoPoint> run, bool closed, int geometry)`

**Problem:** House rule: XML docs on every public member. `WorldRect` and `ProjectedRun` are internal and used by the tests.

**Proposed:** Add one-line summaries: `Shifted`: "The same box &lt;paramref name="worlds"/> whole worlds east (west when negative)." `Normalized`: "The box with its corners ordered and y kept inside the world." `From`: "Projects one line or ring, unwrapping it across the 180th meridian." Add `<param>` lines for `WorldRect`'s X0, X1, Y0 and Y1.

## FeBuddy.Wpf/Converters

<a id="w6-9"></a>

### W6-9 · Wrong doc · Low · FileNamesAsCodeConverter's doc says it is only for the Review tab's run messages

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Converters/FileNamesAsCodeConverter.cs:13`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Converters/FileNamesAsCodeConverter.cs#L13)

**Says:** `For the run messages on the Review tab, which FE-Buddy's library writes as plain text.`

**Problem:** It is also used for the Preview Settings tab's row values (Views/ServicePreviewTabView.xaml:29), and on the Review tab for progress step details and summaries, not only messages.

**Proposed:** `For text FE-Buddy's library writes as plain text: the Review tab's run messages, step details and summaries, and the Preview Settings tab's values.`

**Note:** Also found as [T3-15](300_Unit-Tests.md#t3-15).

<a id="w6-10"></a>

### W6-10 · Wrong doc · Low · StringToVisibilityConverter says "non-empty" but treats whitespace as empty

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Converters/StringToVisibilityConverter.cs:8-9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Converters/StringToVisibilityConverter.cs#L8-L9), 17

**Says:** `Visible when the string is non-empty` vs `!string.IsNullOrWhiteSpace(value as string)`

**Problem:** A string of only spaces is Collapsed.

**Proposed:** `Visible when the string has any non-whitespace text, Collapsed otherwise.`

## FeBuddy.Wpf/Map

<a id="w6-29"></a>

### W6-29 · Minor · Low · The AiracLayerKind enum is not in Map/Models

**Status:** Agreed: done with the other renames in S6 ([H4](010_Decisions.md#h4)) · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Map/AiracMapLayers.cs:16-27`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Map/AiracMapLayers.cs#L16-L27)

**Says:** `public enum AiracLayerKind { ArtccBoundaries, ToweredAirports, Navaids }` declared above `AiracMapLayers`

**Problem:** The Models/ rule (a data-only type goes in Models/) and one-main-type-per-file. Its sibling `BaseMapLayer` is already in Map/Models/BaseMapLayer.cs.

**Proposed:** Move it to `FeBuddy/FeBuddy.Wpf/Map/Models/AiracLayerKind.cs` (namespace `FeBuddy.Wpf.Map.Models`).

<a id="w6-26"></a>

### W6-26 · Minor · Low · BaseMap.Load hides why a base layer failed to load

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Map/BaseMap.cs:61-64`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Map/BaseMap.cs#L61-L64)

**Says:** `catch { return null; }`

**Problem:** Any failure (a resource missing from the build, malformed JSON, or the cross-thread brush lookup if loading moves off the UI thread per [W6-1](110_GUI-Lag.md#w6-1)) makes the base map silently vanish, and `Lazy` caches the `null` for the session. Nothing is logged.

**Proposed:** `catch (Exception ex) when (ex is FormatException or IOException or InvalidOperationException) { AppLog.Warning("Map", $"Could not load the {Name(layer)} base map: {ex.Message}"); return null; }`

<a id="w6-16"></a>

### W6-16 · Minor · Low · GeoJsonReader.Read's `<returns>` leaves out the Multi* types and skipped shapes

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Map/GeoJsonReader.cs:32`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Map/GeoJsonReader.cs#L32)

**Says:** `<returns>Every Point, LineString and Polygon geometry found, in file order.</returns>`

**Problem:** MultiPoint, MultiLineString and MultiPolygon are returned too, each as one `MapGeometry`. Shapes left with nothing drawable, and CRC's isDefaults features, are not returned.

**Proposed:** `<returns>Each drawable geometry (a Multi* geometry as one shape), in file order; CRC defaults features and shapes with nothing left to draw are left out.</returns>`

## FeBuddy.Wpf/Shell

<a id="w7-8"></a>

### W7-8 · Stale doc · Low · DefaultRoiStore names a settings file that no longer exists, and `Changed`'s doc misses `NotifyReloaded`

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Shell/DefaultRoiStore.cs:9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Shell/DefaultRoiStore.cs#L9),29

**Says:** `(<c>Services.AiracService.DefaultRoi</c> in <c>UserConfig.json</c>)` / `Raised after <see cref="Set"/> or <see cref="Clear"/> writes to disk.`

**Problem:** Settings are per profile: `User Configurations\UserConfig.<Profile>.json` (Architecture.md:209). `Changed` is also raised by `NotifyReloaded` (line 71) without any write.

**Proposed:** Line 9: `(<c>Services.AiracService.DefaultRoi</c> in the profile in use)`. Line 29: `<summary>Raised after <see cref="Set"/> or <see cref="Clear"/> saves, and by <see cref="NotifyReloaded"/> after a settings import.</summary>`

<a id="w7-9"></a>

### W7-9 · Wrong doc · Low · Links' summary lists only three places; the links are used in six

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Shell/Links.cs:3-7`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Shell/Links.cs#L3-L7)

**Says:** `External links surfaced in the UI (Dashboard description box, Info screen, the first-launch welcome). Kept in one place so the Discord link the Dashboard and the welcome show and the Info screen's rows never drift.`

**Problem:** Also used by Settings (`ChangeLog` for rollback, SettingsViewModel.cs:136), the credential editor (`GitHubNewToken`, `GitHubTokenGuide`), Add Custom Aliases' source rows (`GitHubTokenGuide`, `GitHubTokenGuideTroubleshooting`, AliasSourceRow.cs:71-72) and the Dashboard and Update window (`IssueUrlBase`). The second sentence is hard to parse. The repo URL and `v3-development` are typed out four times although `GitHubRepository.WebUrl` and `.Branch` (Core) exist and the comment even points at the latter.

**Proposed:** `/// <summary>Every external link the app opens (Dashboard, Info, Settings, the welcome, the credential editor, Add Custom Aliases, the update window), in one place so no two screens drift apart.</summary>`. Optionally build the doc links as `GitHubRepository.WebUrl + "/blob/" + GitHubRepository.Branch + "/docs/Users/…"` and `Issues`/`ChangeLog` from `WebUrl` (DocLinkTests keeps checking them).

<a id="w7-10"></a>

### W7-10 · History wording · Low · Feature number in two summaries

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Shell/MainWindowPlacement.cs:13`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Shell/MainWindowPlacement.cs#L13); [`FeBuddy/FeBuddy.Wpf/Shell/WindowPlacementRules.cs:6`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Shell/WindowPlacementRules.cs#L6)

**Says:** `launch to the next (Feature #364), and puts it back…` / `Decides where FE-Buddy's main window opens (Feature #364): where it was…`

**Problem:** A task/issue number that only says where the work came from (house rule: comments never point at task numbers).

**Proposed:** Delete ` (Feature #364)` in both places.

<a id="w7-12"></a>

### W7-12 · Minor · Low · Five window-placement data records sit in a file named after no type, outside Shell/Models/

**Status:** Agreed: done with the other renames in S6 ([H4](010_Decisions.md#h4)) · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Shell/WindowPlacementModels.cs:1-138`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Shell/WindowPlacementModels.cs#L1-L138)

**Says:** `PixelRect`, `MonitorArea`, `WindowPlacementPlan`, `WindowSizes`, `SavedWindowPlacement` in one file.

**Problem:** Code-Structure's Models/ rule puts data-only types in the feature's `Models/` folder (Shell already has `Shell/Models/ToastKind.cs`), and the one-main-type-per-file rule wants the file named after its type.

**Proposed:** Move them to `Shell/Models/` (namespace `FeBuddy.Wpf.Shell.Models`), either one file per record or keep them together as `Shell/Models/WindowPlacementModels.cs` if the owner accepts it as "small types that belong together"; update the usings in MainWindowPlacement, WindowPlacementRules, NativeMonitors, ChromeWindow and the two test files.

<a id="w7-11"></a>

### W7-11 · Minor · Low · Comment on the showCmd constants says "minimized" but one is the maximized value

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Shell/WindowPlacementRules.cs:31-36`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Shell/WindowPlacementRules.cs#L31-L36)

**Says:** `// GetWindowPlacement's showCmd for a minimized window, and its flag for one that restores maximized.` above `SwShowMaximized = 3`.

**Problem:** `SwShowMaximized` (3) is the showCmd of a maximized window (used at line 111). The `Wpf` in `WpfRestoreToMaximized` is Win32's `WPF_RESTORETOMAXIMIZED` (WindowPlacement Flag), easy to misread as WPF.

**Proposed:** `// GetWindowPlacement's showCmd values (maximized, and the three minimized ones), and its WPF_RESTORETOMAXIMIZED flag: a minimized window that restores maximized.`

## FeBuddy.Wpf/Theme

<a id="w7-21"></a>

### W7-21 · Wrong doc · Low · Controls.Buttons.xaml header: "three intents", Button.Icon.Small missing, Toggle.Icon's size

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Theme/Controls.Buttons.xaml:4-17`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Theme/Controls.Buttons.xaml#L4-L17),259-262

**Says:** `Button vocabulary. Three intents plus the window-caption buttons.` / `Toggle.Icon      Button.Icon as a ToggleButton    - opens a popup (gear)`

**Problem:** Four button intents are listed (Primary, Ghost, Ghost.Danger, Subtle) plus five more styles; `Button.Icon.Small` (line 253, used 7 times) is not listed. `Toggle.Icon` is 28x28 at 14.5, not Button.Icon's 32x32 at 15.5, so in the map toolbar (MapWorkspace.xaml:111,144) it sits smaller than the Button.Icons beside it.

**Proposed:** `Button vocabulary: the button intents, icon buttons and toggles, and the window-caption buttons.`; add `Button.Icon.Small  26x26 Button.Icon - list rows (zoom-to, remove)`; fix "list rows" off `Button.Icon`'s line; either give Toggle.Icon `Width/Height 32` and `FontSize 15.5` or describe it as "a 28x28 icon ToggleButton" in both comments.

<a id="w7-17"></a>

### W7-17 · History wording · Low · "now" in the ComboBox header narrates an earlier state

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Theme/Controls.Inputs.xaml:19-23`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Theme/Controls.Inputs.xaml#L19-L23)

**Says:** `The keyed name still works for an explicit reference; a bare ComboBox now picks it up on its own.`

**Problem:** Describes the change, not the current design.

**Proposed:** `ComboBox -> Themed.ComboBox (dark sunken box + dark popup, kills the system blue bar, and carries the drop-down wheel-scroll behaviour; IsEditable="True" gets a text box to type into). Every ComboBox gets it; the key is for BasedOn.`

<a id="w7-18"></a>

### W7-18 · Wrong doc · Low · TextBox comment promises explicit `Field` call sites that don't exist, and that would lose the error look

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Theme/Controls.Inputs.xaml:158-159`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Theme/Controls.Inputs.xaml#L158-L159)

**Says:** `Every bare TextBox is Field; explicit Style="{StaticResource Field}" still works for the few call sites that want to be obvious about it.`

**Problem:** No TextBox in FeBuddy.Wpf uses `Field` explicitly (CrcDefaultsPanel.xaml:55 defines its own local `Field` for HeaderedContentControl; the installer's SetupWindow uses its own Controls.xaml `Field`). An explicit `Field` would also drop the `FieldState.HasError` trigger, which lives only in the implicit style (164-172), as `Field.Number` shows by basing on `{x:Type TextBox}` instead.

**Proposed:** `<!-- Every bare TextBox gets Field plus the validation look below. Base a TextBox style on {x:Type TextBox}, not on Field, so it keeps that look (as Field.Number does). -->`

<a id="w7-19"></a>

### W7-19 · Wrong doc · Low · Controls.Surfaces.xaml header lists a style that doesn't exist and gives Nav.Item the wrong type

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Theme/Controls.Surfaces.xaml:2-27`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Theme/Controls.Surfaces.xaml#L2-L27)

**Says:** `Nav.List        ListBox - the side navigation` / `Nav.Item        ListBoxItem - one nav row (icon + label + active bar)`

**Problem:** There is no `Nav.List` key (the nav is two ItemsControls, ShellWindow.xaml:495,500). `Nav.Item` targets `RadioButton` (line 557). The header also omits `Chip.Warn`, `Chip.State`, `Chip.State.Text`, `Map.Overlay`, `.Chip`, `.Accent`, `.Divider`, `Map.ListRow`, `Map.ScrollList` and `Map.Swatch`.

**Proposed:** Delete the `Nav.List` line; change Nav.Item to `Nav.Item        RadioButton - one nav row (icon + label + active bar + unsaved dot)`; change Chip line to `Chip / Chip.Warn / Chip.State   Border - small status badge (pair with Chip.Text / Chip.State.Text)`; add `Map.*           Border / ScrollViewer / Rectangle - map overlays, sidebar rows, scrolling file lists, layer swatches`.

<a id="w7-20"></a>

### W7-20 · Wrong doc · Low · Controls.Window.xaml header: not implicit, stale dialog list, wrong titlebar colour claim

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Theme/Controls.Window.xaml:4-8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Theme/Controls.Window.xaml#L4-L8)

**Says:** `Implicit style for ctl:ChromeWindow — the app-styled frame for the child dialogs (ROI picker, Confirm, Update). Matches the shell: Brush.Panel titlebar, …`

**Problem:** The style is keyed (`x:Key="ChromeWindowStyle"`, line 18) and applied by `SetResourceReference(StyleProperty, "ChromeWindowStyle")` (ChromeWindow.cs:37), precisely because an implicit Window style isn't found (ChromeWindow.cs:22-28). 11 dialogs derive from ChromeWindow (also Welcome, Uninstall, Reset, Import Settings, Profile Name, Credential Editor, Alias Guide Format, Duplicate Alias Review). The shell's title bar is `Brush.Bg.Base` (ShellWindow.xaml:176), not `Brush.Panel`.

**Proposed:** `Keyed style for ctl:ChromeWindow, applied by ChromeWindow's constructor (SetResourceReference): the app-styled frame for every dialog. Brush.Panel title bar, Caption.Button / Caption.Close, Brush.Bg.Base body, a 1px Brush.Stroke.Strong window border.`

<a id="w7-23"></a>

### W7-23 · Wrong doc · Low · Palette says no colour is hard-coded anywhere else; several are

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Theme/Palette.xaml:4-6`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Theme/Palette.xaml#L4-L6); [`FeBuddy/FeBuddy.Wpf/Theme/Controls.Inputs.xaml:581-586`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Theme/Controls.Inputs.xaml#L581-L586)

**Says:** `Nothing else in the app hard-codes a colour. Views and control styles only ever reference these keys, so re-skinning (or adding a light theme later) is a one-file change.`

**Problem:** Controls.Inputs.xaml:583-585 repeats three palette values as literals (`#24FDC518` = Brush.Accent.Soft, `#FFE7A0` = Color.Accent.Text, `#0F1A26` = Color.Panel); drop shadows hard-code `#000000` in 8 places; the map's colours are in code (MapLayersState.cs:54-57, AiracMapLayers.cs:69-71, MapCanvas.cs:1174-1176,1271,1355,1383, MapCanvas.Inspect.cs:244; Code-Structure.md:199 says so).

**Proposed:** Reword: `Every colour the views and control styles use comes from here, except shadow black and the map's layer colours (set in code).` In Inputs.xaml:583-585 use `Color="{DynamicResource Color.Accent}"`-style references (or drop the block if the full template leaves nothing system-drawn).

<a id="w7-24"></a>

### W7-24 · History wording · Low · Typography header narrates the fonts the theme was first drawn with

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Theme/Typography.xaml:7-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Theme/Typography.xaml#L7-L11)

**Says:** `(The theme was first drawn for Montserrat and Jost; FE-Buddy always fell back to Segoe UI, and that look was kept.)`

**Problem:** History, not a reason tied to a bug; the sentences before already say why Segoe UI.

**Proposed:** Delete the parenthesis; keep "The two keys stay separate so headings can take a face of their own later. Arial is only a last resort."

## FeBuddy.Wpf/ViewModels

<a id="w2-12"></a>

### W2-12 · Wrong doc · Med · Duplicate review claims CRC runs the first copy of a command

**Status:** Agreed ([D3](010_Decisions.md#d3)) · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/DuplicateAliasReviewViewModel.cs:18-20`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DuplicateAliasReviewViewModel.cs#L18-L20)

**Says:** "the first line of a command with no line kept keeps it ... so finishing straight away does what CRC would have done anyway: run the first."

**Problem:** Everywhere else FE-Buddy says CRC uses the **last** copy of a command (CombinedAliasFileWriter.cs:46, 196, 236, 286; Architecture.md:198; User-Guide.md:306). So the default "keep the first line" is not what CRC would have done.

**Proposed:** Either reword to "Otherwise the first line of a command with no line kept keeps it, and the rest are left out." (drop the CRC claim), or, if the CRC claim is the intent, make the default keep the **last** line (`NewGroup`, lines 72-81) and say so.

**Note:** **Kyle:** CRC reads an alias file top to bottom and the **last** duplicate wins, so the window is what's wrong: its default should keep the last line, and the comment should say so ([D3](010_Decisions.md#d3)).

<a id="w2-31"></a>

### W2-31 · History wording · Low · "every included sub-service that has a backend"

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:515-518`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L515-L518)

**Says:** "...then runs every included sub-service that has a backend."

**Problem:** Leftover from when some tabs had no Core service; every sub-service has one.

**Proposed:** "...then runs every included sub-service."

<a id="w2-32"></a>

### W2-32 · Wrong doc · Low · "The only sub-service-specific lines in the run" / "the only place a sub-service is registered"

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:541-542`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L541-L542); [`AiracSubServices.cs:18-22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracSubServices.cs#L18-L22)

**Says:** "The only sub-service-specific lines in the run: which block each tab's settings belong to." / "Adding one means adding an entry here and a tab view-model for it; ... all pick it up with no further changes. The list is expected to grow to roughly twenty entries."

**Problem:** `CollectFilesWritten` and `SummarizeRun` are sub-service-specific too, and adding a sub-service also needs the `TabFor` accessor and settings-block line here, a DataTemplate in TabbedServiceView.xaml and Core's AiracServiceSettings/AiracService (Code-Structure.md:241-256 lists them). "Roughly twenty" is a roadmap guess that will go stale.

**Proposed:** AiracSubServices: "This list is where a sub-service is registered: the General tab's table, the tab rail and the Preview Settings tab follow it. See Code-Structure.md, Adding a sub-service, for the rest." Delete the "roughly twenty" sentence. AiracServiceViewModel:541: "Which block each tab's settings belong to; everything else about a tab goes through ISubServiceRunTarget." (or fix [W2-7](120_Logic-In-GUI.md#w2-7) first).

<a id="w2-38"></a>

### W2-38 · Minor · Low · Naming and small tidy-ups

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs:27`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracServiceViewModel.cs#L27), 35, 131; [`AiracGeneralTabViewModel.cs:82`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs#L82), 304-307; [`EramToGeojsonViewModel.cs:39-40`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/EramToGeojsonViewModel.cs#L39-L40); [`FileConversionsViewModel.cs:63`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/FileConversionsViewModel.cs#L63), 165

**Says:** `ScreenTitle => "AIRAC Services"`; "AIRAC Service is simply the first first-tier service built on it."; `private static bool IsYes(string value) => ...`; double blank lines; `"FAA Radar Video Map .dat files", "FAA RADAR Video Maps (RVMs)..."`; "Every conversion page, built or not yet opened."

**Problem:** The heading says "AIRAC Services" while the nav, News and the User Guide say "AIRAC Service" (ConfigPageName "AIRAC Services General" shows in import messages); AIRAC Service is the only screen on TabbedServiceViewModel; `IsYes` duplicates the base `ReadYesNo`; stray blank lines; "Radar"/"RADAR" in one picker entry; all pages are built in the constructor.

**Proposed:** Use "AIRAC Service" for `ScreenTitle`, the summary and `ConfigPageName` ("AIRAC Service General"); "AIRAC Service is the screen built on it."; `IsSavedOutputOn` -> `ReadYesNo(UserConfigFile.GetValue(...), true)` and delete `IsYes`; remove the extra blank lines; "FAA RADAR Video Map .dat files"; "Every conversion page, opened yet or not."

<a id="w2-34"></a>

### W2-34 · History wording · Low · Key comments narrate the sub-services' earlier names

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracSubServices.cs:55-59`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracSubServices.cs#L55-L59), 64-68

**Says:** "The Procedure Charts sub-service key: its first name, Procedures, kept so its saved settings carry over."

**Problem:** History narration; the keys themselves are COMPAT with 3.0 betas' saved settings (renaming would lose `Services.AiracService.Procedures`/`.ConcatenateAliases` and the `SelectedSubServices`/`Outputs` entries without a migration), so keep the values.

**Proposed:** "The Procedure Charts sub-service key. It names saved settings (see the remarks above), so it stays `Procedures`." and "The Add Custom Aliases sub-service key. It names saved settings, so it stays `ConcatenateAliases`."

<a id="w2-30"></a>

### W2-30 · Stale doc · Low · Airports summary still names the "ROI override"

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AirportsViewModel.cs:13-15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AirportsViewModel.cs#L13-L15)

**Says:** "...which FE-Buddy properties, the CRC ERAM defaults, and the ROI override."

**Problem:** The tab has an Area card (an ROI or everything), like NAVAIDs and Wx Stations.

**Proposed:** "...which outputs are on (set on the General tab), the area (an ROI, or everything), which GeoJSON files, which FE-Buddy properties and the CRC ERAM defaults."

<a id="w1-24"></a>

### W1-24 · Wrong doc · Low · Airways' resync comment misdescribes the construction snapshot

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AirwaysViewModel.cs:233-236`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AirwaysViewModel.cs#L233-L236)

**Says:** `// The list was empty when this tab snapshotted itself at construction, so the snapshot` / `// says "nothing excluded" while the config may well exclude several.`

**Problem:** Before the list is built, `ExcludedDesignationsValue()` (line 526-529) falls back to the saved exclusions, so the construction snapshot already holds them. The resync matters because the rebuilt list keeps only this cycle's designations, dropping saved exclusions the cycle doesn't have, and the file groups and CRC rows follow it.

**Proposed:** "The snapshot taken at construction read the saved exclusions; the list now holds only this cycle's designations, and the files and CRC rows follow it, so re-take the snapshot now."

<a id="w1-28"></a>

### W1-28 · Minor · Low · Airways and Departures hard-code their alias file names in the run summary

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AirwaysViewModel.cs:256`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AirwaysViewModel.cs#L256); [`DeparturesViewModel.cs:282`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DeparturesViewModel.cs#L282)

**Says:** `summary += $", Airways.txt: {airways.AliasAirwayLineCount:N0} alias line(s)";` / `summary += $", Departures.txt: {departures.AliasCommandCount:N0} alias command(s)";`

**Problem:** Procedure Charts and Telephony use `ProcedureOutputFiles.Alias` / `TelephonyOutputFiles.Alias` for the same line.

**Proposed:** Use `{AirwayOutputFiles.Alias}` and `{DepartureOutputFiles.Alias}`.

<a id="w2-29"></a>

### W2-29 · Stale doc · Low · ARTCC Boundaries says it alone has "no Outputs card"

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ArtccBoundariesViewModel.cs:23-25`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ArtccBoundariesViewModel.cs#L23-L25)

**Says:** "Unlike every other AIRAC sub-service, ARTCC Boundaries writes GeoJSON Lines only: there is no Outputs card, no file-kind choice and no alias file."

**Problem:** Outputs are on the General tab now; only Procedure Charts and Telephony have an Outputs card. Fixes and Wx Stations have no alias file either.

**Proposed:** "ARTCC Boundaries writes GeoJSON Lines only: it has no What Files Do You Want? card and no alias file."

<a id="w1-22"></a>

### W1-22 · History wording · Low · "Concatenate Aliases, as the code still calls it"

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs:33-34`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs#L33-L34)

**Says:** `/// The <b>Add Custom Aliases</b> sub-service tab inside the AIRAC Service screen (Concatenate Aliases,` / `/// as the code still calls it): ...`

**Problem:** "still" narrates the rename.

**Proposed:** "(ConcatenateAliases in the code and its settings keys)".

<a id="w1-27"></a>

### W1-27 · Stale doc · Low · "UserConfig.json" in Add Custom Aliases' remarks

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs:47-50`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs#L47-L50)

**Says:** `/// A web address may name a saved credential (Settings ▸ Credentials). Only its id is saved with the` / `/// tab, so the secret never reaches <c>UserConfig.json</c>, and a settings export leaves even the id`

**Problem:** Settings are now per-profile files (`UserConfig.<Profile>.json`). `UserConfig.json` survives only as `UserConfigFile.LegacyConfigFileName`.

**Proposed:** "…so the secret never reaches a settings file, and a settings export leaves even the id out…". The same stale name is in CredentialsViewModel.cs:22, DefaultRoiStore.cs:9, CrcDefaultsCard.xaml.cs:25 (user-facing text), UrlSecrets.cs:6 and CredentialStore.cs:15, all outside this scope.

<a id="w3-32"></a>

### W3-32 · Wrong doc · Low · Odd and inaccurate comment in CredentialsViewModel

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/CredentialsViewModel.cs:45`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/CredentialsViewModel.cs#L45)

**Says:** `// Kyle's features, or another window, may add or remove credentials too.`

**Problem:** Names a person instead of the code; the other writers are Settings' "New GitHub token..." (SettingsViewModel.cs:495) and Add Custom Aliases' new-credential button (ConcatenateAliasesViewModel.cs:460).

**Proposed:** `// Settings' New GitHub token…, Add Custom Aliases' new credential, or another window may add or remove credentials too.`

<a id="w3-33"></a>

### W3-33 · Stale doc · Low · Credentials card doc names the old single settings file

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/CredentialsViewModel.cs:21-22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/CredentialsViewModel.cs#L21-L22)

**Says:** `credentials live in Windows Credential Manager, not <c>UserConfig.json</c>`

**Problem:** Settings are per profile now (`User Configurations\UserConfig.<Profile>.json` plus Shared.json); `UserConfig.json` is only the pre-profile file Core moves in.

**Proposed:** "credentials live in Windows Credential Manager, not in a settings profile, so they have nothing to do with the page's Save."

<a id="w3-27"></a>

### W3-27 · Wrong doc · Low · Dashboard comment says a Dashboard is made per navigation

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs:298-299`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs#L298-L299)

**Says:** `// AppLog remembers the clear, so the next Dashboard (one is made per navigation) seeds from after it.`

**Problem:** NavItem builds each page's view-model once and keeps it (NavItem.cs:41), so there is one Dashboard per session; `ClearDisplay` matters for a restart of the view-model, not per navigation.

**Proposed:** `// AppLog remembers the clear too, so anything else showing DisplayEntries starts after it. Every entry is cleared, whatever the filter shows; the filter stays.`

<a id="w2-35"></a>

### W2-35 · History wording · Low · Issue number in class summaries

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/DuplicateAliasReviewViewModel.cs:12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DuplicateAliasReviewViewModel.cs#L12); [`DuplicateAliasesCardViewModel.cs:15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DuplicateAliasesCardViewModel.cs#L15)

**Says:** "...when it stops at duplicated alias commands (issue #318): ..."

**Problem:** Points at a task number (house rule: comments don't point at planning docs or task numbers); it explains nothing about the code.

**Proposed:** Delete " (issue [#318](https://github.com/Nikolai558/FE-BUDDY/issues/318))" in both.

<a id="w2-36"></a>

### W2-36 · History wording · Low · ERAM page justifies behaviour by the "original tool"

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/EramToGeojsonViewModel.cs:19-21`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/EramToGeojsonViewModel.cs#L19-L21), 151-153

**Says:** "in the original ERAM_2_GEOJSON tool's three layouts ... with its names" / "Each run empties `ERAM_TO_GEOJSON`, as the original tool emptied its output folder;"

**Problem:** Explains current behaviour by reference to the tool FE-Buddy replaces (Core uses the same wording; flag for its reviewer).

**Proposed:** Summary: "...into CRC-ready GeoJSON in `ERAM_TO_GEOJSON`, laid out By Filters, By Attributes, Raw or Raw Plus, plus ..."; remark: "Each run empties `ERAM_TO_GEOJSON`, so it holds only this run's files; with anything in it, the user says so first."

<a id="w2-28"></a>

### W2-28 · Stale doc · Low · File Names remarks: when the list is read, and a stale comparison

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/FileNamesViewModel.cs:23-25`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/FileNamesViewModel.cs#L23-L25)

**Says:** "The list is read from the other tabs' settings as they are now, each time the tab is shown (...), as the Add Custom Aliases tab reads its alias files."

**Problem:** `RefreshFiles` runs on every tab change, cycle change, General-table change, preview build and run (AiracServiceViewModel:73, 117, 299, 523; this file:178); and Add Custom Aliases does not read the run's alias files at all (ConcatenateAliasesViewModel has no such code).

**Proposed:** "The list is read from the other tabs' settings as they are now, whenever the user moves between tabs and before a run (see &lt;see cref="GeojsonSubServiceViewModel.OutputFileEntries"/>)."

<a id="w1-23"></a>

### W1-23 · Stale doc · Low · Fixes remarks say having no Outputs card is unusual

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/FixesViewModel.cs:30-32`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/FixesViewModel.cs#L30-L32)

**Says:** `/// tab, described by this tab through <see cref="ISubServiceRunTarget"/>. Unlike most other AIRAC` / `/// sub-services, it has no alias file and no Outputs card, and its settings block has no`

**Problem:** Only Procedure Charts and Telephony have an Outputs card (ProceduresView.xaml:45, TelephonyView.xaml:36). Airports, Airways, Departures and the rest have none, so Fixes is like most.

**Proposed:** "Unlike most other AIRAC sub-services, it has no alias file, and its settings block has no `GenerateGeojson`: while included, the sub-service always writes GeoJSON."

<a id="w3-36"></a>

### W3-36 · Wrong doc · Low · IConfigPage mentions only imports

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/IConfigPage.cs:4-5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/IConfigPage.cs#L4-L5)

**Says:** `/// re-read them when a settings import replaces the file underneath it`

**Problem:** Pages also re-read on a profile switch, new profile or delete (SettingsProfilesViewModel.AfterSwitch -> ConfigPages.ReloadEverything).

**Proposed:** "...so it has to re-read them when a settings import or a switch to another settings profile replaces them (see ConfigPages)."

<a id="w3-29"></a>

### W3-29 · Wrong doc · Low · Output picker docs: summary example and All/None scope

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs:287`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/MapLayersState.cs#L287), 352-356

**Says:** `/// <summary>A line about the output picker's contents, e.g. <c>3 of 42 files shown</c>.</summary>` / `/// <summary>Ticks every file in the output picker.</summary>`

**Problem:** The text is "3 of 42 files on the map" (line 299). All/None act on the files the filter shows, and All refuses more than 150 (SetAllOutputs).

**Proposed:** "e.g. &lt;c>3 of 42 files on the map&lt;/c>"; "Ticks every file the filter shows, up to 150 at once."; "Unticks every file the filter shows (with no filter, every pick)."

<a id="w3-25"></a>

### W3-25 · History wording · Low · Issue numbers that only narrate history

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/MapViewModel.cs:24`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/MapViewModel.cs#L24); [`FeBuddy/FeBuddy.Wpf/ViewModels/MapFeatureCard.cs:9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/MapFeatureCard.cs#L9); [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsProfilesViewModel.cs:17`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsProfilesViewModel.cs#L17); [`FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs:319`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs#L319), 870; [`FeBuddy/FeBuddy.Wpf/ViewModels/WelcomeViewModel.cs:9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/WelcomeViewModel.cs#L9)

**Says:** `/// Properties (issue #327): a <b>Ctrl + click</b> ...` / `The Settings Profile card, first on Settings (issue #324)` / `(#351, <see cref=...WelcomeNotice/>)`

**Problem:** Each number only says which request added the feature; none explains a choice forced by a bug (house rule: no task numbers in comments).

**Proposed:** Delete the references: "Properties: a Ctrl + click ..."; "(Ctrl + click): the layer it is in ..."; "The Settings Profile card, first on Settings: ..."; "...which the General tab shows as &lt;i>partial&lt;/i>."; "The nav rail when a section opens: Map collapses it ..."; "The one-time welcome to FE-Buddy 3 (&lt;see cref=...WelcomeNotice/>): ...".

<a id="w3-37"></a>

### W3-37 · Minor · Low · Small doc mismatches in NavItem and InfoViewModel

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/NavItem.cs:9-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/NavItem.cs#L9-L11), 19, 37; [`FeBuddy/FeBuddy.Wpf/ViewModels/InfoViewModel.cs:10-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/InfoViewModel.cs#L10-L11)

**Says:** `/// <param name="glyph">Icon glyph from <c>Icons.xaml</c>.</param>` / `creates it lazily the first time the user opens that section, so switching tabs keeps its state.`

**Problem:** Nav glyphs are ShellViewModel's own `Glyph*` constants (E8AB and E707 aren't in Theme/Icons.xaml at all); ShellWindow builds File Conversions' view-model at idle before it is opened; "tabs" means sections. InfoViewModel's summary lists "the manual, the change log and the issue tracker" but omits the FAQ / Troubleshooting card.

**Proposed:** glyph: "A Segoe Fluent Icons glyph (ShellViewModel's Glyph constants)."; summary: "...creates it the first time it is needed (usually when the user opens that section) and keeps it, so a section keeps its state."; Info: "...links to the User Guide, FAQ / Troubleshooting, the change log and the issue tracker."

<a id="w1-21"></a>

### W1-21 · History wording · Low · Issue and feature numbers that only narrate history

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs:245`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs#L245); [`TelephonyViewModel.cs:274`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs#L274)

**Says:** `/// or NAVAID in the facility set in Settings (Feature #367). On by default; ...` / `/// What went wrong with the list, shown in amber under the status (issue #337): a part that couldn't`

**Problem:** Neither number explains a choice driven by a bug that was found; they only record where the feature came from.

**Proposed:** Delete " (Feature [#367](https://github.com/Nikolai558/FE-BUDDY/issues/367))" and " (issue [#337](https://github.com/Nikolai558/FE-BUDDY/issues/337))".

<a id="w1-26"></a>

### W1-26 · Wrong doc · Low · Small mismatches in Procedure Charts' docs and texts

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs:382-383`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs#L382-L383), 1837, 549, 620, 269-271, 30-36

**Says:** `/// <summary>Which ROI the area is, as the cards name it: <c>the default ROI</c> or <c>the ROI specific to Procedures</c>.</summary>` / `e.g. <c>by ZOB under Facilities</c>` / `{procedureCount:N0} procedures`

**Problem:** - `RoiShortName` actually gives "the ROI specific to Procedure Charts" (Title). `IncludedBy` on the same tab uses `RoiName` instead, which reads "the ROI specific to the Procedure Charts sub-service".
  - `AirportEntry`'s `IncludedBy` example doesn't match line 1456 ("by ZOB, a facility ticked on the Area card").
  - `SetDtppData`'s remarks begin "This card never blocks the run", but it's a method.
  - `_dtppStatus`'s "N procedures" counts every non-continuation chart: airport diagrams, minimums, hot spots and so on.
  - `IncludeFixInFacilityToolTip` says only a fix or NAVAID counts. With the area "None", Core also counts a route point that names an airport in the facility (ProcedureService.cs:133, `useAirports: settings.Area == SubServiceArea.None`).
  - The class summary leaves out the External Procedures in Our Airspace and Chart Recall Help Command cards.

**Proposed:** - `RoiShortName`: change the example to `the ROI specific to Procedure Charts`, and use `RoiShortName` in `IncludedBy`.
  - `AirportEntry`: change the example to `by ZOB, a facility ticked on the Area card`.
  - `SetDtppData`: "The metafile never blocks the run …".
  - `_dtppStatus`: "{procedureCount:N0} charts".
  - Tooltip: add "(with the area None, an airport on the route in {facility} counts too)".
  - Class summary: add "other airports' procedures with a fix in the facility, and the chart recall help command".

<a id="w1-29"></a>

### W1-29 · Minor · Low · Procedure Charts reads and normalises the Settings facility in five places

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs:238`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ProceduresViewModel.cs#L238), 901, 1127, 1774, 1818

**Says:** `NormalizeFacility(UserConfigFile.GetValue(SettingsViewModel.ArtccKey))` (five times)

**Problem:** The same expression appears in `SettingsFacility`, `LoadCycleDependentLists`, `LoadFromConfig`, `FacilitiesTheListWillTick` and `DescribePrimaryFacility`. `SettingsViewModel.ArtccKey` (Wpf) also duplicates Core's `UserConfigAreas.FacilityKey`, which the tests use.

**Proposed:** Use `SettingsFacility` in all five places. Read the key through `UserConfigAreas.FacilityKey` (or have `SettingsViewModel.ArtccKey` alias it).

<a id="w3-35"></a>

### W3-35 · Wrong doc · Low · Reset/Uninstall "settings" lists leave out the window placement

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ResetViewModel.cs:32-36`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ResetViewModel.cs#L32-L36); [`FeBuddy/FeBuddy.Wpf/ViewModels/UninstallViewModel.cs:18-22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/UninstallViewModel.cs#L18-L22)

**Says:** `"... and your update channel, which GitHub token FE-Buddy uses, and which News posts you have read."`

**Problem:** Deleting settings removes Shared.json, which also holds "Remember window size and position" and the saved placement (Settings' Main Window card); the lists name the other shared settings but not this one.

**Proposed:** Append ", and where the main window opens" before the closing sentence in both.

<a id="w3-30"></a>

### W3-30 · Wrong doc · Low · FacilityOption.Display example doesn't match NASR names

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:1210`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L1210)

**Says:** `/// <summary>e.g. <c>Cleveland ARTCC (ZOB)</c>.</summary>`

**Problem:** `Name` is APT_BASE's ARTCC_NAME, which is just the upper-case city ("ATLANTA", "MEMPHIS" in the fixture), so it reads `CLEVELAND (ZOB)`.

**Proposed:** `e.g. <c>CLEVELAND (ZOB)</c>`.

<a id="w3-34"></a>

### W3-34 · Wrong doc · Low · Settings page summary/remarks slightly off

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:40-42`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L40-L42), 59-62, 215

**Says:** `/// SYSTEM ▸ Settings. Section order: Settings Profile, Facility, ... Reset / Uninstall.` / `Export and Import move every setting in the app (not only this page's)`

**Problem:** The Developer: Launch Steps card (dev launch) is missing from the order. Export writes only the profile in use's settings that can leave this PC (shared settings such as the update channel, read News, GitHub token id and window placement are left out). The section comment says "FACILITY PROFILE" while the card is "Facility".

**Proposed:** "... Main Window, Reset / Uninstall, and in a dev launch Developer: Launch Steps."; "Export and Import move every setting of the profile in use that can leave this PC (not only this page's) ..."; `// ================= 1. FACILITY =================`.

<a id="w3-26"></a>

### W3-26 · History wording · Low · "As in 2.x" explains the DEV suffix by old behaviour

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs:606`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs#L606)

**Says:** `// As in 2.x: a copy the MSI did not install (a dev build, or one run from elsewhere) says so.`

**Problem:** Narrates the old version rather than the reason.

**Proposed:** `// A copy the MSI did not install (a dev build, or one run from elsewhere) says so, so it is never mistaken for an installed release.`

<a id="w3-28"></a>

### W3-28 · Wrong doc · Low · DescribeUnfinishedWork comment names only the update

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs:786-787`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs#L786-L787)

**Says:** `// What closing FE-Buddy for an update would lose: a run in progress and unsaved edits on any page opened this session.`

**Problem:** It is also handed to SettingsViewModel (line 97) for Reset, Uninstall and the dev-launch restart.

**Proposed:** `// What closing FE-Buddy now (an update, a reset, an uninstall or a dev-launch restart) would lose: ...`

<a id="w3-38"></a>

### W3-38 · Minor · Low · Shell doc examples don't match the output; status dates use the current culture

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs:186-191`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs#L186-L191), 198, 677; [`FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs:256`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/DashboardViewModel.cs#L256)

**Says:** `<c>AIRAC 2610 · eff 01 OCT 2026</c>` vs `$"... eff {current.Cycle.EffectiveDateUtc:dd MMM yyyy}"`

**Problem:** The month prints as "Oct" (and is localised: e.g. "Okt." on a German Windows) while the Zulu clock beside it uses InvariantCulture; VersionText's doc omits the " - DEV" suffix (line 609).

**Proposed:** Format both dates with `CultureInfo.InvariantCulture` (`string.Create(CultureInfo.InvariantCulture, ...)`) and fix the example to `eff 01 Oct 2026`; VersionText doc: "e.g. &lt;c>v3.0.0&lt;/c>, &lt;c>v3.0.0 - DEV&lt;/c> for a copy the MSI didn't install, or &lt;c>dev&lt;/c>."

<a id="w7-25"></a>

### W7-25 · Minor · Low · Nav glyphs are hard-coded in ShellViewModel instead of Icons.xaml

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs:41-46`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ShellViewModel.cs#L41-L46); [`FeBuddy/FeBuddy.Wpf/Theme/Icons.xaml:31`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Theme/Icons.xaml#L31),36,45,53

**Says:** `private const string GlyphDashboard = ""; // Home` (written as the literal private-use character)

**Problem:** Code-Structure.md:230 puts "a colour, font or glyph in Theme/". Four of the six (E80F Home, E787 Calendar, E713 Settings, E946 Info) duplicate `Icon.Home`, `Icon.Calendar`, `Icon.Settings`, `Icon.Info`; E8AB and E707 exist nowhere else. The invisible literal characters are also hard to review.

**Proposed:** Add `Icon.Switch` (E8AB) and `Icon.MapPin` (E707) to Icons.xaml and have NavItem carry a resource key that the NavRow template resolves (`DynamicResource` via a key binding), or at least write the constants as `""` escapes with a comment naming the Icons.xaml key.

<a id="w1-25"></a>

### W1-25 · Wrong doc · Low · RefreshTelephonyData's summary lists too few callers and leaves out what it does

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs:121-129`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/TelephonyViewModel.cs#L121-L129)

**Says:** `/// (<see cref="TelephonyFiles.RegisterFilePath"/>). Called when the tab is built and by the AIRAC` / `/// Service screen after every run, which is when the copies are replaced.`

**Problem:** It is also called from `SyncSubServiceTabs` (AiracServiceViewModel.cs:297), which runs when the screen is built (so twice on first open) and on every General-tab Include/output change (line 70). It also restarts `LoadRealOperators`, re-parsing both FAA pages.

**Proposed:** "Re-reads how old FE-Buddy's copy of the FAA telephony register is, and the real operators in its copies of both pages (off the UI thread). Called when the tab is built, when the General tab's sub-services change, and after every run, which is when the copies are replaced." (See [W1-2](110_GUI-Lag.md#w1-2) for not re-parsing unchanged files.)

<a id="w3-31"></a>

### W3-31 · Wrong doc · Low · Update window docs assume updating, not going back

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/UpdateWindowViewModel.cs:18-20`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/UpdateWindowViewModel.cs#L18-L20), 27-28, 126, 188

**Says:** `/// <summary>One entry per release newer than <see cref="CurrentVersion"/>, newest first.</summary>` / `Later dismisses (and colours the version text amber for the session)`

**Problem:** Going back, `Releases` holds the one older release (`LatestRelease`, since `NewerReleases` is empty unless UpdateAvailable), and Later colours nothing (ShellViewModel.cs:779 skips `IsGoingBack`); `UserDeclined`'s doc says the shell colours the text either way.

**Proposed:** Releases: "The releases to show, newest first: each one newer than CurrentVersion, or - going back - the release to go back to." Remarks: "Later dismisses (and, for an update, colours the version text amber for the session)". UserDeclined: "...the shell then colours the version text as a warning for the session, unless it was going back."

**Note:** Re-checked after [#377](https://github.com/Nikolai558/FE-BUDDY/pull/377): still applies.

<a id="w3-39"></a>

### W3-39 · Minor · Low · Release page URL hard-coded again in UpdateWindowViewModel

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/UpdateWindowViewModel.cs:37`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/UpdateWindowViewModel.cs#L37)

**Says:** `private const string ReleasesPage = "https://github.com/Nikolai558/FE-BUDDY/releases/latest";`

**Problem:** The repository URL is defined in Core (`GitHubRepository.WebUrl`) and the releases page in `Links.ChangeLog`; this third copy would drift if the repo moves.

**Proposed:** `private const string ReleasesPage = GitHubRepository.WebUrl + "/releases/latest";` (and ideally `Links.ChangeLog = GitHubRepository.WebUrl + "/releases"`).

**Note:** Re-checked after [#377](https://github.com/Nikolai558/FE-BUDDY/pull/377): still applies.

<a id="w2-33"></a>

### W2-33 · Wrong doc · Low · Two small doc gaps (callers, states)

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/WxStationsViewModel.cs:70-74`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/WxStationsViewModel.cs#L70-L74); [`AiracGeneralTabViewModel.cs:406`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracGeneralTabViewModel.cs#L406)

**Says:** "Called when the tab is built and by the AIRAC Service screen after every run" / `<c>ready</c> / <c>partial</c> / <c>preparing…</c> / <c>downloading…</c> / <c>parsing…</c> / <c>failed</c> / <c>not yet published</c>`

**Problem:** `RefreshStationData` is also called when the General tab's sub-services change (AiracServiceViewModel:297, 455-459); `State` can also be "downloaded, not parsed (dev launch)" and "not loaded (dev launch)" (lines 459, 462).

**Proposed:** "Called when the tab is built, when the sub-services change on the General tab, and after every run, which is when the copy is replaced." / add "or, in a dev launch, `downloaded, not parsed (dev launch)` / `not loaded (dev launch)`".

## FeBuddy.Wpf/ViewModels/Models

<a id="w4-32"></a>

### W4-32 · History wording · Low · Feature issue numbers that only narrate history

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/Models/AliasSourceRow.cs:28-29`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/Models/AliasSourceRow.cs#L28-L29); [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceRow.cs:15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceRow.cs#L15)

**Says:** `/// short (issue #326); ...` / `/// Include always matches "at least one output on" (issue #308): ...`

**Problem:** [#326](https://github.com/Nikolai558/FE-BUDDY/issues/326) and [#308](https://github.com/Nikolai558/FE-BUDDY/issues/308) are feature requests (ChangeLog.md:269, 318: "Feature [#326](https://github.com/Nikolai558/FE-BUDDY/issues/326)", "Feature [#308](https://github.com/Nikolai558/FE-BUDDY/issues/308)"), not bugs that explain the design; the sentences already say why.

**Proposed:** Delete " (issue [#326](https://github.com/Nikolai558/FE-BUDDY/issues/326))" and " (issue [#308](https://github.com/Nikolai558/FE-BUDDY/issues/308))".

<a id="w4-24"></a>

### W4-24 · Stale doc · Low · "None ticked means every ARTCC" predates the Area card

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/Models/ArtccToggle.cs:6-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/Models/ArtccToggle.cs#L6-L11); [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceSettingsViewModel.cs:251-255`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceSettingsViewModel.cs#L251-L255)

**Says:** `/// the ARTCC Boundaries location filter (none selected means every ARTCC)` / `/// A saved empty list stays empty: the user chose every ARTCC.`

**Problem:** Every ARTCC is now the Everything area. With the ARTCCs area, none ticked is a validation error (GeojsonSubServiceViewModel.cs:894-897, "Tick at least one ARTCC on the Area card, or pick another area"), and with another area the list is kept but unused.

**Proposed:** ArtccToggle: "One ARTCC toggle, reused by several sub-service tabs: the ARTCCs of the Departures/Arrivals and ARTCC Boundaries Area card, and the Procedures facilities (selected means included). What it persists to is up to the tab." ParseArtccListOrFacility: "A saved empty list stays empty: the user unticked every ARTCC."

<a id="w4-31"></a>

### W4-31 · Wrong doc · Low · EramClassDefault.ClassName lists the classes but misses several

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/Models/EramClassDefault.cs:55-63`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/Models/EramClassDefault.cs#L55-L63)

**Says:** `/// The class this row is for: an airway altitude class (High / Low / Other), a sub-service's single class (Airports, Runways, Departures, Arrivals), or - for NAVAIDs - ...`

**Problem:** Reads as the full list but leaves out Fixes (a fix-use token, a chart, or a chart + fix use group, FixesViewModel.cs:601-607), Wx Stations (`WxStationOutputFiles.AllClass`) and the conversions' classes (each parser's `CrcClassName`).

**Proposed:** Start with "e.g." and add "Fixes' fix-use, chart or combination group (e.g. WYPNT), Wx Stations' single class, or a conversion's class (its parser's CrcClassName)".

<a id="w4-30"></a>

### W4-30 · Wrong doc · Low · FebPropertyToggle's list of option lists is incomplete

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/Models/FebPropertyToggle.cs:12-18`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/Models/FebPropertyToggle.cs#L12-L18)

**Says:** `/// Every sub-service uses this one type; what differs is the list it is built from (AirportFebPropertyOptions, AirwayFebPropertyOptions, DepartureFebPropertyOptions, ArrivalFebPropertyOptions, NavaidFebPropertyOptions, ArtccBoundaryFebPropertyOptions)`

**Problem:** Fixes (FixFebPropertyOptions) and the ERAM to GeoJSON page (EramFebPropertyOptions) build theirs with it too; ERAM is a conversion, not a sub-service.

**Proposed:** "Every sub-service tab with the card, and the ERAM to GeoJSON page, uses this one type; what differs is the list it is built from (the `*FebPropertyOptions` classes) through ListFor."

<a id="w4-23"></a>

### W4-23 · Wrong doc · Low · FixGroupToggle and NavaidTypeToggle say the selected set is saved; the unselected set is

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/Models/FixGroupToggle.cs:5-8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/Models/FixGroupToggle.cs#L5-L8); [`FeBuddy/FeBuddy.Wpf/ViewModels/Models/NavaidTypeToggle.cs:5-9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/Models/NavaidTypeToggle.cs#L5-L9)

**Says:** `/// ... The <b>selected</b> set is what gets persisted as included; an unselected entry is left out of the matching file layout.`

**Problem:** FixesViewModel.WriteToConfig saves `ExcludedFixUses`/`ExcludedCharts` (the unticked tokens, FixesViewModel.cs:479-480, 634-637); NavaidsViewModel saves `ExcludedTypes` (NavaidsViewModel.cs:214, 311). DesignationToggle states it correctly.

**Proposed:** FixGroupToggle: "The &lt;b>unselected&lt;/b> set is what gets persisted (`ExcludedFixUses` / `ExcludedCharts`), so a fix use or chart new to a cycle starts included; an unselected entry is left out of the matching file layout." NavaidTypeToggle: "The &lt;b>unselected&lt;/b> set is what gets persisted (`ExcludedTypes`), so a type new to a cycle starts included; an unselected type is left out of both the GeoJSON and the alias file."

<a id="w4-37"></a>

### W4-37 · Minor · Low · SavedDuplicateChoiceRow.Line example doesn't match the text it builds

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/Models/SavedDuplicateChoiceRow.cs:28-31`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/Models/SavedDuplicateChoiceRow.cs#L28-L31)

**Says:** `/// ... or <c>... (line 2)</c> for a later line of the same file.` vs `$"{Choice.Command} in {Choice.FileKey} (line {Choice.Occurrence} with it)"`

**Problem:** The text is "(line 2 with it)".

**Proposed:** "... or &lt;c>... (line 2 with it)&lt;/c> for a later line of the same file that uses it."

## FeBuddy.Wpf/ViewModels/ServiceTabs

<a id="w4-27"></a>

### W4-27 · Wrong doc · Low · FileConversionTabViewModel docs leave out ERAM and the Symbols flag

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/FileConversionTabViewModel.cs:19-28`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/FileConversionTabViewModel.cs#L19-L28)

**Says:** `/// Base for a conversion page that turns source files into GeoJSON (DAT to GeoJSON, SCT2 to GeoJSON)` / `/// A derived page passes its CRC class and whether it writes text to the constructor`

**Problem:** EramToGeojsonViewModel derives from it too (and the remarks below cite ERAM). The constructor takes `writesSymbols` as well as `writesText`.

**Proposed:** "(DAT to GeoJSON, SCT2 to GeoJSON, ERAM to GeoJSON)" and "passes its CRC class and whether it writes symbols and text to the constructor".

<a id="w4-29"></a>

### W4-29 · Wrong doc · Low · OnOutputsChanged remark gives the wrong example

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs:458`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs#L458)

**Says:** `/// <remarks>A tab with outputs of its own to announce (<c>GenerateGeojson</c>) overrides this and calls the base.</remarks>`

**Problem:** The base already announces GenerateGeojson (line 462). The three overrides announce what follows from the outputs: Procedures' documents and card intros, Airways' ShowsStrata, NAVAIDs' symbol-style visibility.

**Proposed:** "A tab with more that follows from the outputs (Procedures' documents, a card shown only while GeoJSON is on) overrides this and calls the base."

<a id="w4-35"></a>

### W4-35 · Minor · Low · CRC-row config plumbing duplicated in the two base classes

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs:496`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs#L496), 839; [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/FileConversionTabViewModel.cs:440`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/FileConversionTabViewModel.cs#L440), 449

**Says:** `private static string CrcConfigPrefix(EramClassDefault row) => $"CrcEramPropertyDefaults.{row.ClassName}_{row.Kind}";` (identical in both)

**Problem:** The config key layout for a CRC row is defined twice; CrcDefaultsRowIo says it is "the one way every tab with a CRC ERAM Defaults card does it" but takes the prefix from its callers.

**Proposed:** Move the prefix into CrcDefaultsRowIo (`Load(row, get)` / `Save(row, set)` computing `CrcEramPropertyDefaults.{ClassName}_{Kind}` themselves) and delete both private copies.

<a id="w4-33"></a>

### W4-33 · History wording · Low · ISubServiceRunTarget remark narrates an earlier run method

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ISubServiceRunTarget.cs:10-15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ISubServiceRunTarget.cs#L10-L15)

**Says:** `/// ... so adding the next sub-service does not mean editing the run method again - the only sub-service-specific line left there is which settings block ...`

**Problem:** "again" / "left there" describe what the method used to need.

**Proposed:** "The AIRAC Service screen drives the run through this interface rather than each sub-service's own type, so the run method has one sub-service-specific line per sub-service: which settings block on AiracServiceSettings its block goes in."

<a id="w4-26"></a>

### W4-26 · Wrong doc · Low · Tab/run docs describe only the AIRAC screen

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceTabViewModel.cs:7-11`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceTabViewModel.cs#L7-L11); [`TabbedServiceViewModel.cs:79`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/TabbedServiceViewModel.cs#L79); [`RunStep.cs:6-10`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/RunStep.cs#L6-L10); [`ServiceRunReviewTabViewModel.cs:58`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/ServiceRunReviewTabViewModel.cs#L58)

**Says:** `/// One tab inside a first-tier service screen ...: the General tab, one tab per sub-service, the Preview Settings tab, and ... the Review tab.` / `/// One row per sub-service in the run, in the order they run.`

**Problem:** ServiceTabViewModel is also the base of the File Names tab and each File Conversions page, and the Review view-model is reused under a conversion's Run card. For a conversion, BeginRun gets no names and ReportStep adds one step per source file (FileConversionsViewModel.cs:263-269), so a RunStep is a file's line there. The Tabs doc omits the File Names tab between the sub-services and Preview Settings.

**Proposed:** ServiceTabViewModel: add "...; File Conversions' pages are built on it too." RunStep: "One line in the run feed: a sub-service (AIRAC Service) or a source file (a conversion)...". Steps: "One row per sub-service in the run (per source file for a conversion), in the order they run." Tabs: "General, the sub-services (greyed out while left out), File Names, Preview Settings, then Review after a run."

<a id="w4-25"></a>

### W4-25 · Wrong doc · Low · SubServiceSettingsViewModel's class docs: who derives from it, and how WriteToConfig writes

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceSettingsViewModel.cs:10-24`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceSettingsViewModel.cs#L10-L24)

**Says:** `/// Base class for every sub-service settings tab.` / `///   <item>implements <see cref="WriteToConfig"/> (push fields into <see cref="UserConfigFile"/> via <c>TrySetValue</c>)`

**Problem:** It is also the base of the General tab, the File Names tab and every File Conversions page (through ConversionTabViewModel). WriteToConfig implementations must use `Set` (the method's own remark at 180-186 says so, and every override does); calling `TrySetValue` directly would break the dirty check. BuildPreviewSummary is already implemented for conversion pages by ConversionTabViewModel.

**Proposed:** Summary: "Base class for every tab and page that saves its own settings node (the AIRAC Service's General, sub-service and File Names tabs, and each File Conversions page)". Item: "implements WriteToConfig, writing every field with Set".

<a id="w4-34"></a>

### W4-34 · Minor · Low · "menu" for what everything else calls a tab

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceSettingsViewModel.cs:12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceSettingsViewModel.cs#L12), 16, 51, 60, 74, 176, 178, 199, 203, 215, 216, 220, 221, 284, 287

**Says:** `/// Validates, then writes only this menu's <c>UserConfig</c> subtree.`

**Problem:** The rest of the app and docs say tab (or page for conversions); "menu" is the old UI's word.

**Proposed:** Replace "menu" with "tab" in these 15 comments ("A derived tab:", "this tab's node", ...).

## FeBuddy.Wpf/ViewModels/ServiceTabs/Models

<a id="w4-28"></a>

### W4-28 · Wrong doc · Low · SubServiceDescriptor claims to be the whole registration surface

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/Models/SubServiceDescriptor.cs:7-12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/Models/SubServiceDescriptor.cs#L7-L12)

**Says:** `/// A catalogue of these is the whole registration surface for a sub-service. Adding the next one ... is one entry here plus its tab view-model; nothing in the shell, the rail, the save contract or the Preview Settings and Review tabs changes.`

**Problem:** A new sub-service also needs its view and DataTemplate (TabbedServiceView.xaml), a tab accessor and the settings-block line in AiracServiceViewModel.RunAsync (AiracServiceViewModel.cs:541-566), and a block on Core's AiracServiceSettings (Code-Structure "Adding a sub-service" lists them).

**Proposed:** "A catalogue entry is how a sub-service gets its row on the General tab, its place in the rail and its tab. Adding the next one - AIRAC Service is expected to reach roughly twenty - also needs its view, and in AiracServiceViewModel the line that puts its settings block on the run's settings; nothing in the shell, the rail, the save contract or the Preview Settings and Review tabs changes."

<a id="w4-36"></a>

### W4-36 · Minor · Low · Files outside the Models/ rule in ServiceTabs

**Status:** Agreed: done with the other renames in S6 ([H4](010_Decisions.md#h4)) · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/Models/SummaryBlock.cs:134-198`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/Models/SummaryBlock.cs#L134-L198); [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/RunStep.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/RunStep.cs); [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceRunResult.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/SubServiceRunResult.cs)

**Says:** `public sealed class SummaryLines(bool joinFirstLine = false)` (a builder) inside Models/SummaryBlock.cs, with SummaryJoin, SummaryLine, SummaryFile and SummaryBlock

**Problem:** Code-Structure: data in Models/, classes that do work at the feature root, one main type per file. SummaryLines (a builder with rules) sits in Models/ in another type's file, while the row models RunStep and SubServiceRunResult sit at the root next to their sibling SubServiceMessageGroup, which is in Models/.

**Proposed:** Move SummaryLines to ServiceTabs/SummaryLines.cs (and SummaryJoin to Models/SummaryJoin.cs if the owner wants one type per file); move RunStep.cs and SubServiceRunResult.cs to ServiceTabs/Models/ (namespace ...ServiceTabs.Models).

## FeBuddy.Wpf/Views

<a id="w5-31"></a>

### W5-31 · Minor · Low · Esc doesn't close most dialogs

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`ConfirmWindow.xaml:42`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ConfirmWindow.xaml#L42); [`AliasGuideFormatWindow.xaml:43`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/AliasGuideFormatWindow.xaml#L43); [`ImportSettingsWindow.xaml:72`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ImportSettingsWindow.xaml#L72); [`ProfileNameWindow.xaml:48`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ProfileNameWindow.xaml#L48); [`ResetWindow.xaml:102`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ResetWindow.xaml#L102); [`UninstallWindow.xaml:95`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/UninstallWindow.xaml#L95); [`UpdateWindow.xaml:122`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/UpdateWindow.xaml#L122); [`WelcomeWindow.xaml:41`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/WelcomeWindow.xaml#L41)

**Says:** `<Button Content="Cancel" Style="{StaticResource Button.Ghost}" Command="{Binding CancelCommand}" .../>` (no `IsCancel`)

**Problem:** Only CredentialEditorWindow and DuplicateAliasReviewWindow set `IsCancel="True"`, and ChromeWindow doesn't handle Esc. In the other eight modals, Esc does nothing.

**Proposed:** add `IsCancel="True"` to the Cancel/Close/Later button of each.

<a id="w5-26"></a>

### W5-26 · Wrong doc · Low · InfoView header leaves out the FAQ card

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/InfoView.xaml:2-3`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/InfoView.xaml#L2-L3)

**Says:** `and links to the manual, change log and issue tracker`

**Problem:** InfoViewModel also has "FAQ / Troubleshooting" (InfoViewModel.cs:39).

**Proposed:** "...links to the manual, FAQ, change log and issue tracker".

<a id="w5-32"></a>

### W5-32 · Minor · Low · MapView calls the Map page "the Map Service page"

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/MapView.xaml:2`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/MapView.xaml#L2); [`MapView.xaml.cs:5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/MapView.xaml.cs#L5)

**Says:** `MapView  -  the Map Service page`

**Problem:** The nav item, MapWorkspace and Code-Structure all call it "the Map page".

**Proposed:** "the Map page".

<a id="w5-25"></a>

### W5-25 · Wrong doc · Low · RoiPickerWindow.Pick's `title` example doesn't match either caller

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/RoiPickerWindow.xaml.cs:44`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/RoiPickerWindow.xaml.cs#L44)

**Says:** `e.g. <c>Airways ROI override</c>`

**Problem:** The callers pass `"Default Region of Interest"` (SettingsViewModel.cs:1176) and `$"{Title}: Its Own ROI"` (GeojsonSubServiceViewModel.cs:975).

**Proposed:** `e.g. <c>Default Region of Interest</c>` (or the new wording from [W5-12](610_On-Screen-Text.md#w5-12)).

<a id="w5-36"></a>

### W5-36 · Minor · Low · Preview Settings comment uses a planning term

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/ServicePreviewTabView.xaml:63`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ServicePreviewTabView.xaml#L63)

**Says:** `<!-- something is unsaved: the run saves it first (settings-save contract) -->`

**Problem:** "settings-save contract" is not defined in the docs (Code-Structure's "Saving" section doesn't use it). It reads like a pointer to a planning document.

**Proposed:** `<!-- something is unsaved: the run saves it first, after asking -->`

<a id="w5-28"></a>

### W5-28 · Wrong doc · Low · Review tab comment says the "no errors" card is for a run that "reported nothing"

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/ServiceRunReviewTabView.xaml:164`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ServiceRunReviewTabView.xaml#L164)

**Says:** `<!-- the run finished and reported nothing: say so rather than showing an empty review -->`

**Problem:** The card shows whenever `HasRun && !HasErrors && !HasAdvisories` (lines 170-176), even with warnings and results below it. Its text, "The run finished with no errors.", is correct.

**Proposed:** `<!-- the run finished with no errors or advisories: say so -->`

<a id="w5-27"></a>

### W5-27 · Wrong doc · Low · SettingsView header says everything else goes to the profile

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/SettingsView.xaml:2-6`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/SettingsView.xaml#L2-L6)

**Says:** `Everything persists to the settings profile in use but the credentials, ..., the Main Window card, which goes to Shared.json at once, and the Developer card, which goes to DevLaunch.json.`

**Problem:** The update channel and FE-Buddy's GitHub token are saved to `Shared.json` (UserConfigPortability.cs:42-44). The Settings Profile card takes effect at once and is not saved to a profile (comment at line 61). The list also gives "Updates / Update Channel", which is one card, as two.

**Proposed:** "Saved on Save to the profile in use, except: the update channel and FE-Buddy's GitHub token (Shared.json, on Save), the Settings Profile card (takes effect at once), credentials (Windows Credential Manager, at once), Main Window (Shared.json, at once) and Developer (DevLaunch.json, at once)." Change the card list to "Updates / Update Channel".

<a id="w5-34"></a>

### W5-34 · Minor · Low · "Open the Releases Page" is bound to a command named RollbackCommand

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/SettingsView.xaml:358-359`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/SettingsView.xaml#L358-L359); [`FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs:136`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/SettingsViewModel.cs#L136),592-593

**Says:** `<Button ... Content="Open the Releases Page" Command="{Binding RollbackCommand}" />` and `RollbackCommand = new RelayCommand(() => BrowserLauncher.Open(Links.ChangeLog));`

**Problem:** The command only opens the releases page. The name "Rollback" suggests something else, and `Links.ChangeLog` holds the releases URL.

**Proposed:** rename it to `OpenReleasesPageCommand` (view and view-model).

<a id="w5-29"></a>

### W5-29 · Wrong doc · Low · ShellWindow header calls the collapsible nav rail "fixed"

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/ShellWindow.xaml:6-7`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ShellWindow.xaml#L6-L7)

**Says:** `The body is a fixed nav rail plus a ContentControl`

**Problem:** The rail's width animates between 232 and 68 (lines 325-360).

**Proposed:** "The body is a collapsible nav rail plus a ContentControl ...".

<a id="w5-33"></a>

### W5-33 · Minor · Low · UpdateWindow.xaml has no header comment

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/UpdateWindow.xaml:1`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/UpdateWindow.xaml#L1)

**Says:** the file starts with `<ctl:ChromeWindow x:Class="FeBuddy.Wpf.Views.UpdateWindow"`

**Problem:** Every other view and window starts with a comment saying what it is and what backs it.

**Proposed:** add `<!-- UpdateWindow - the modal update window: installed vs latest version, release notes, and Update / Later. Opened from the title bar's version chip and update badge, or from Settings after a channel change. Backs onto UpdateWindowViewModel. -->`

## FeBuddy.Wpf/Views/Cards

<a id="d1b-24"></a>

### D1b-24 · Stale doc · Med · "UserConfig.json" is still named as the settings file, including in GUI text

**Status:** Agreed ([H1](010_Decisions.md#h1)) · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/Cards/CrcDefaultsCard.xaml.cs:25`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/Cards/CrcDefaultsCard.xaml.cs#L25); [`FeBuddy/FeBuddy.Core/Application/Updates/UpdateChannelSetting.cs:12`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Application/Updates/UpdateChannelSetting.cs#L12); [`FeBuddy/FeBuddy.Versioning/Models/ReleaseChannel.cs:5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Versioning/Models/ReleaseChannel.cs#L5); [`FeBuddy/FeBuddy.Core/Infrastructure/Credentials/CredentialStore.cs:15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Credentials/CredentialStore.cs#L15); [`FeBuddy/FeBuddy.Core/Infrastructure/Credentials/UrlSecrets.cs:6`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Credentials/UrlSecrets.cs#L6); [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigPortability.cs:19`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigPortability.cs#L19); [`FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigKeys.cs:4`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Configuration/UserConfigKeys.cs#L4); [`FeBuddy/FeBuddy.Wpf/Shell/DefaultRoiStore.cs:9`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Shell/DefaultRoiStore.cs#L9); [`FeBuddy/FeBuddy.Wpf/ViewModels/CredentialsViewModel.cs:22`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/CredentialsViewModel.cs#L22); [`FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs:48`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ConcatenateAliasesViewModel.cs#L48)

**Says:** CrcDefaultsCard: "Note: This should be a one-time setup, as these values are saved to your `UserConfig.json` file."; UpdateChannelSetting: "the one saved in &lt;c>UserConfig.json&lt;/c> (&lt;c>General.UpdateChannel&lt;/c>)"

**Problem:** Settings now live in `User Configurations\UserConfig.<Profile>.json` plus `Shared.json` (Architecture.md:209-212, UserConfigFile.cs:13-15). `UserConfig.json` is only beta.3's legacy file (`LegacyConfigFileName`). The CrcDefaultsCard line is shown to users. The channel is not in a profile at all: `General.UpdateChannel` is in `UserConfigPortability.LocalKeys`, so it lives in `Shared.json`. Found while checking Credentials.md and VERSIONING.md. The owning code reviewers may report the individual lines too.

**Proposed:** CrcDefaultsCard: "... as these values are saved in your settings." UpdateChannelSetting and ReleaseChannel: "saved in &lt;c>Shared.json&lt;/c> (&lt;c>General.UpdateChannel&lt;/c>), shared by every settings profile". The others: "the settings profile" / "a settings file" in place of `UserConfig.json`.

**Note:** Individual fixes: [W5-2](610_On-Screen-Text.md#w5-2) (on screen), [C1-23](200_Comments-Core.md#c1-23), [C6-7](200_Comments-Core.md#c6-7), [C6-14](200_Comments-Core.md#c6-14), [O1-34](400_Installer-Harness-CI.md#o1-34), [T2-22](400_Installer-Harness-CI.md#t2-22), [W1-27](#w1-27), [W3-33](#w3-33), [W7-8](#w7-8).

<a id="w5-23"></a>

### W5-23 · Wrong doc · Low · AreaCard's Outputs and RoiContent docs miss Procedures and Airways

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/Cards/AreaCard.xaml.cs:54-57`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/Cards/AreaCard.xaml.cs#L54-L57),113

**Says:** `the GeoJSON on most tabs, the alias file too on Departures, Arrivals and Airways.` and `(Departures and Arrivals: which procedures it keeps)`

**Problem:** On Procedure Charts the area narrows `ProcedureChanges | ProceduresJson` (ProceduresViewModel.cs:216), not GeoJSON. Airways also fills `RoiContent` (AirwaysView.xaml:37-48: which airways the alias file gets).

**Proposed:** Outputs: "...the alias file too on Departures, Arrivals and Airways, and the two documents on Procedure Charts." RoiContent: "(Departures and Arrivals: which procedures it keeps; Airways: which airways the alias file gets)."

<a id="w5-24"></a>

### W5-24 · Wrong doc · Low · CrcDefaultsPanel header lists too few tabs with several classes

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/Cards/CrcDefaultsPanel.xaml:4-6`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/Cards/CrcDefaultsPanel.xaml#L4-L6),76-77

**Says:** `Airports, Departures and Arrivals have one class, so one block; Airways can have High / Low / Other, and NAVAIDs split by type one per NAVAID type.`

**Problem:** ARTCC Boundaries (High/Low/Unlimited, or one per ARTCC and altitude) and Fixes (one per fix use, chart or combination) also get several classes (ArtccBoundaryOutputFiles.cs:30-37; FixOutputFiles.cs:17-20). The User Guide (line 104) says the same.

**Proposed:** "...Airways can have High / Low / Other, ARTCC Boundaries one per altitude (or ARTCC and altitude), Fixes one per file group, and NAVAIDs split by type one per NAVAID type." Change line 76-77 likewise.

<a id="w5-20"></a>

### W5-20 · History wording · Low · Issue numbers in comments that only label a feature

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`Cards/DuplicateAliasesCard.xaml:3`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/Cards/DuplicateAliasesCard.xaml#L3); [`DuplicateAliasReviewWindow.xaml:3`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/DuplicateAliasReviewWindow.xaml#L3); [`ConcatenateAliasesView.xaml:97-98`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ConcatenateAliasesView.xaml#L97-L98); [`MapWorkspace.xaml:164`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/MapWorkspace.xaml#L164),434; [`WelcomeWindow.xaml:2`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/WelcomeWindow.xaml#L2); [`TelephonyView.xaml:60`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/TelephonyView.xaml#L60); [`ProceduresView.xaml:97`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ProceduresView.xaml#L97)

**Says:** `(issue #318)`, `(issue #326)`, `(issue #327)`, `(issue #312)`, `(#351)`, `(issue #337)`, `(Feature #367)`

**Problem:** These only narrate where a feature came from; none of them explains a choice made because of a bug. House rule: comments don't point at task numbers. (Kept, because they explain a bug-driven choice: [#354](https://github.com/Nikolai558/FE-BUDDY/issues/354) in ResetWindow, [#349](https://github.com/Nikolai558/FE-BUDDY/issues/349) in InfoView/WhatsNewView, [#332](https://github.com/Nikolai558/FE-BUDDY/issues/332) in UpdateWindow, [#321](https://github.com/Nikolai558/FE-BUDDY/issues/321) in CredentialEditorWindow.)

**Proposed:** delete the parenthesised references. For example, ProceduresView.xaml:97 becomes `external procedures in our airspace: other airports' procedures with a fix in my facility`.

<a id="w5-21"></a>

### W5-21 · Stale doc · Low · FebPropertiesCard's usage example binds IsEnabled, which no view does

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/Cards/FebPropertiesCard.xaml:7-8`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/Cards/FebPropertiesCard.xaml#L7-L8)

**Says:** `<cards:FebPropertiesCard IsEnabled="{Binding GenerateGeojson}" />`

**Problem:** Every use is `<cards:FebPropertiesCard />`. The card greys itself out through `ctl:Card Outputs="Geojson"`.

**Proposed:** change the example to `<cards:FebPropertiesCard />`.

<a id="w5-35"></a>

### W5-35 · Minor · Low · SourceFilesCard uses a literal ✕ glyph instead of the icon token

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/Cards/SourceFilesCard.xaml:86`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/Cards/SourceFilesCard.xaml#L86)

**Says:** `<Button DockPanel.Dock="Right" Style="{StaticResource Button.Subtle}" Content="✕" Padding="6,2" ...`

**Problem:** Every other remove button uses `Button.Icon.Small` with `{StaticResource Icon.Close}` (DuplicateAliasesCard.xaml:42, ConcatenateAliasesView.xaml:111). Code-Structure: "Tokens, not literals".

**Proposed:** `Style="{StaticResource Button.Icon.Small}" Content="{StaticResource Icon.Close}"`.

<a id="w5-30"></a>

### W5-30 · Wrong doc · Low · SummaryCard header says "every AIRAC sub-service tab"

**Status:** Open · **Session:** [S8](020_Work-Plan.md#s8)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/Cards/SummaryCard.xaml:2-3`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/Cards/SummaryCard.xaml#L2-L3); [`SummaryCard.xaml.cs:5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/Cards/SummaryCard.xaml.cs#L5)

**Says:** `the top card of every AIRAC sub-service tab`

**Problem:** Add Custom Aliases (ConcatenateAliasesView) has no SummaryCard. It is also not the top card: AttentionCard (and on Procedure Charts the d-TPP warning card) sit above it.

**Proposed:** "the card under the Attention card on every AIRAC sub-service tab but Add Custom Aliases".
