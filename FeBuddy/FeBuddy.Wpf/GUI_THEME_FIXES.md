# FeBuddy.Wpf — GUI / theme remediation

Hand-off for the model doing the code changes. Scope: **`FeBuddy.Wpf` only** (the
`Kickstart` branch). This is a visual-consistency pass — no behavior changes, no
new features. Every screen was walked: Dashboard, AIRAC Service + Airways
sub-service, Map, Settings, Info, the Systems fly-out, collapsed nav, and the
ROI picker window.

## Context: the theme is good — use it, don't reinvent it

The design system already exists and is well-factored:

| File | What's in it |
|---|---|
| `Theme/Palette.xaml` | all colour tokens (`Brush.*`, `Color.*`), radii |
| `Theme/Typography.xaml` | type ramp (`Text.Display/H1/H2/Body/Body.Strong/Label/Caption/Mono`) |
| `Theme/Controls.Inputs.xaml` | `Field` (TextBox), `Pill.Radio`, `Pill.Toggle`, `Segmented`, `Switch`, `Progress` |
| `Theme/Controls.Buttons.xaml` | `Button.Primary`, `Button.Ghost`, `Button.Subtle`, `Ghost.Toggle`, `Card.Toggle`, caption buttons |
| `Theme/Controls.Surfaces.xaml` | `Card`, `Divider`, `Chip.*`, `Nav.Item` |
| `Theme/Controls.Chrome.xaml` | `ScrollBar` + `ToolTip` implicit styles |

`App.xaml` merges only `Theme/Theme.xaml`, which merges the rest in order.

**The root problem:** every style is `x:Key`'d, there are **no implicit styles**
for `ComboBox` / `CheckBox` / `RadioButton` / `TextBox`, and several views drop
raw controls in without referencing a style. So they fall back to the OS theme
(white fills, Aero borders, blue selection) on top of the dark navy palette.

Keep all new colour references pointing at existing `Brush.*` tokens. Per the
comment in `Palette.xaml`, nothing hard-codes colour.
**Gotcha (from the project notes):** a `StaticResource` ref from one merged
dictionary to a *sibling* merged dictionary resolves to `UnsetValue`. Inside
`Theme/*.xaml` use `DynamicResource` for token refs; Views may use `StaticResource`.

---

## Priority 1 — Style the native controls (biggest visual win)

### 1a. ComboBox — no style exists at all

Call sites (all currently unstyled):
- `Views/AiracServiceView.xaml:57` — facility picker
- `Views/AirwaysView.xaml:107` — GeoJSON output mode
- `Views/SettingsView.xaml:63` — facility picker

Add an **implicit** `Style TargetType="ComboBox"` (and a matching
`ComboBoxItem` style + dark popup) to `Theme/Controls.Inputs.xaml`. It should
match `Field`:

- closed box: `Brush.Bg.Sunken` fill, `Brush.Stroke.Strong` 1px border,
  `Radius.Control`, `Brush.Text.Primary` text, `11,8` padding
- toggle chevron: a `Font.Icon` glyph (`&#xE70D;`) in `Brush.Text.Tertiary`
- keyboard-focus / drop-open: border → `Brush.Accent` (mirror the `Field` trigger)
- popup `Border`: `Brush.Panel` bg, `Brush.Stroke.Strong` border, `Radius.Control`,
  soft `DropShadowEffect` (copy the one from the `ToolTip` template)
- `ComboBoxItem`: transparent; hover → `Brush.Overlay`; selected →
  `Brush.Accent.Soft` bg + `Brush.Accent.Text` foreground. **Kill the system
  blue `HighlightBrushKey`.**
- disabled: `Opacity 0.45` on the root (same as `Field`)

If an editable combo is ever needed, template `PART_EditableTextBox` too, but
today all three are non-editable — a read-only template is fine.

### 1b. CheckBox — no style exists

~18 call sites, heaviest in `Views/AirwaysView.xaml` (lines 115–232), plus
`Views/MapView.xaml:35,79` and `Views/SettingsView.xaml:76`.

Add an implicit `Style TargetType="CheckBox"`:

- box: 16×16, `Radius.Small` (or 4), `Brush.Bg.Sunken` fill,
  `Brush.Stroke.Strong` border
- checked: `Brush.Accent` fill + `Brush.Text.OnAccent` check glyph
  (`Font.Icon` `&#xE73E;`), border → `Brush.Accent`
- hover: border → `Brush.Accent.Line`
- focus: `Brush.FocusRing` ring
- **label:** `ContentPresenter` with `TextElement.Foreground` =
  `Brush.Text.Primary`, `Font.Body`, `13`, `8` gap from the box, vertically
  centred. Right now the label text is darker than the box.
- disabled: `Opacity 0.4`
- honour `FlowDirection` and the access-key underline (`_Lines` etc. rely on it)

### 1c. RadioButton (non-nav) — no style exists

Call sites: `Views/AiracServiceView.xaml:41` (cycle Previous/Current/Next),
`Views/SettingsView.xaml:33-39` (Stable/Beta/Alpha),
`Views/SettingsView.xaml:113-119` (5/6/7 dp),
`Views/AirwaysView.xaml:191-193` (alias scope).

Two clean options — **pick one and use it consistently:**

1. **Classic radio**, styled to match 1b: 16×16 disc, `Brush.Bg.Sunken` /
   `Brush.Stroke.Strong`, checked = `Brush.Accent` dot + accent ring, label
   `Brush.Text.Primary`. Add as implicit `Style TargetType="RadioButton"`.
2. **Reuse `Pill.Radio`** (already in `Controls.Inputs.xaml`) for the
   Settings/alias groups since they're short mutually-exclusive option sets —
   visually consistent with the Airways output-mode pickers.

Recommendation: classic radio as the implicit default (cycle list reads better
as a vertical list than as pills), `Pill.Radio` where a view explicitly opts in.
Do **not** leave any bare `<RadioButton>` on the OS default.
`Nav.Item` and the Dashboard `LogChip` already have their own templates — don't
disturb those (an implicit style won't hit them because they set `Template`, but
verify).

### 1d. TextBox — style exists (`Field`), just not applied

`Field` is defined in `Controls.Inputs.xaml` but must be referenced explicitly.
Unstyled call sites:
- `Views/AirwaysView.xaml:37-47` — BCG / Filters / Style / Thickness / Size
- `Views/AirwaysView.xaml:212-218` — NE/SW lat/lon
- `Views/SettingsView.xaml:74` — output directory
- `Controls/RoiEditor.xaml:44-51` — NE/SW lat/lon (shared ROI editor,
  used by the Map page inline **and** the ROI picker window)

**Simplest fix:** make `Field` the implicit `TextBox` style — add
`<Style TargetType="TextBox" BasedOn="{StaticResource Field}"/>` (or just drop
the `x:Key`) in `Controls.Inputs.xaml`. Then every `TextBox` is themed and the
placeholder-via-`Tag` feature still works. Add `Style="{StaticResource Field}"`
explicitly only where a view needs a variant.
Check no `TextBox` relies on the OS look (none seen).

### 1e. While here — the small unstyled glyph boxes

On every map canvas (Map page, Settings mini-map, ROI window) there's a tiny
empty square top-left and bottom-right — zoom / reset-view controls whose glyph
isn't resolving. See `Controls/MapCanvas.cs` / `Controls/RoiEditor.xaml`. Give
them `Button.Subtle` or `Caption.Button` styling and a real `Font.Icon` glyph
(`&#xE8A3;` zoom-in, `&#xE71E;` / `&#xE72C;` etc.), or remove them if dead.

---

## Priority 2 — Text contrast ("too dark to read")

### 2a. Tokens

In `Palette.xaml`:

```
Color.Text.Secondary  #9FB1C0   -> bump to ~#B7C6D3  (body copy must clear AA on cards)
Color.Text.Tertiary   #68798A   -> bump to ~#8B9DAD  (only ~3.3:1 on #0E1621 today)
```

`Color.Text.Tertiary` on `Color.Card` (`#0E1621`) is currently below WCAG AA for
text. After the bump, re-check `Text.Label` (10.5px bold) still reads as a quiet
overline and isn't competing with `Text.Body`.

### 2b. Call sites using Tertiary for real prose (fix regardless of the token bump)

- **`Views/DashboardView.xaml:64`** — the activity-log **message** column is
  `Style="{StaticResource Text.Caption}"` (→ Tertiary, 11.5px). This is the
  single worst offender: a whole scrolling log of unreadable grey. Change the
  message `TextBlock` to `Text.Body` (Secondary). Keep the timestamp, the `Z`,
  and the tick bar on Tertiary — they're meant to recede.
- **`Views/DashboardView.xaml:41`** the `NextCycleLine` caption + mono — fine to
  stay quiet but verify after the token bump.
- **`Views/AirwaysView.xaml`** — the explanatory paragraphs under
  "GEOJSON OUTPUT" ("One file set by altitude…", the `Airways_High/Low/Other`
  bullets, "Custom Geojson Property fields…") and the `ready` status text on the
  cycle rows: audit for `Text.Caption` / `Brush.Text.Tertiary` and move genuine
  sentences to `Text.Body`.
- **`Views/AiracServiceView.xaml`** — "Cycle 2609 · effective 03 Sep 2026",
  "Your ARTCC / facility, from the selected cycle's parsed airports.", the
  `AIRAC Service › Airways` breadcrumb — same audit.
- **`Views/MapView.xaml`** — "Load one or more GeoJSON files…", "No default ROI
  is set. Draw one below…", "Drag a box on the map…", "No files loaded".
- **`Views/SettingsView.xaml`** — the long ROI explanation paragraph,
  "Include FE-Buddy Properties, when available…".
- **Systems fly-out** (in `Views/ShellWindow.xaml` / its popup) — the sub-labels
  "connected" / "previous / current / next ready" / "state unknown (offline)".

Rule of thumb to apply everywhere: **`Text.Tertiary` is for ≤11px uppercase
labels and de-emphasised metadata only. Any full sentence the user is expected
to read = `Text.Body` (Secondary) or `Text.Body.Strong`.**

---

## Priority 3 — Scroll architecture (nested ScrollViewers)

`Views/ShellWindow.xaml:432` hosts pages in a bare `ContentControl` (no outer
scroll — good). Each view supplies its own root `ScrollViewer`. Problems:

### 3a. AIRAC Services page has two stacked ScrollViewers

`Views/AiracServiceView.xaml:18` wraps the page in a `ScrollViewer`, and it
embeds `AirwaysView`, which **also** opens with a root `ScrollViewer`
(`Views/AirwaysView.xaml:86`). Wheel events land in the inner one, which has a
limited extent → the page appears stuck and the content below
"DESIGNATIONS TO INCLUDE" is unreachable.

**Fix:** `AirwaysView` (and any sub-service view hosted inside
`AiracServiceView`) must **not** have its own `ScrollViewer`. Remove it; let the
single page-level `ScrollViewer` in `AiracServiceView` own vertical scrolling.
If `AirwaysView` is ever shown standalone, wrap it at *that* host instead.

### 3b. Dashboard has three scroll regions on one page

`Views/DashboardView.xaml`: the page grid + the News `ScrollViewer` (line 158) +
the log `ListBox` with `MaxHeight="240"` and its own scroll (line 224). The
wheel does different things depending on where the pointer is.

**Fix options (pick per panel):**
- Let the News list and the log flow in the page scroll — drop the inner
  `ScrollViewer` / `MaxHeight`, or
- Keep them bounded but add `PreviewMouseWheel` on each inner scroller that
  re-raises an unhandled `MouseWheelEvent` to the parent when the inner scroll
  is already at its top/bottom edge (standard WPF "bubble the wheel" handler).
  Put the helper in `Infrastructure/` and attach via an attached property so it's
  reusable.

### 3c. Wheel step feels tiny everywhere

The page `ScrollViewer`s scroll in small increments. Set
`ScrollViewer.CanContentScroll="False"` (pixel-based) is already effective; also
consider a modest `PanningMode="VerticalOnly"` and verify
`VirtualizingStackPanel` isn't forcing item-based steps on the plain content
panels. Target ~3× current step.

---

## Priority 4 — Sticky page headers

On AIRAC Services and Settings the big page title ("AIRAC Services",
"Settings") **and** the top-right primary action ("Save") sit *inside* the
view's `ScrollViewer`, so they scroll away and clip under the window title bar.

**Fix:** in each page view, split the layout into a fixed header row +
scrolling body:

```
<Grid>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>   <!-- title + page action, OUTSIDE the ScrollViewer -->
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <!-- header -->
  <ScrollViewer Grid.Row="1"> ... body ... </ScrollViewer>
</Grid>
```

Give the header the page background and an optional 1px `Divider` at its bottom
so content scrolling under it reads cleanly. Do this for `AiracServiceView`,
`SettingsView`, `InfoView`, `MapView` (Map's toolbar row should stay pinned),
and `AirwaysView`'s "Save / Undo last save" bar (that bar should pin to the
sub-service section, not scroll).

---

## Priority 5 — Buttons

### 5a. AIRAC "Save" looks muddy brown

`Views/AirwaysView.xaml:93` — it *is* `Button.Primary`. What renders is
`Button.Primary`'s **disabled** state: `Opacity 0.38` on `Brush.Accent` = a
dead amber-brown. `SaveCommand.CanExecute` is false when there's nothing to
save, which is correct behavior but reads as a broken button.

**Fix:** improve the disabled affordance in `Controls.Buttons.xaml`
`Button.Primary` — instead of `Opacity 0.38` on the fill, use a flat
`Brush.Bg.Sunken` fill + `Brush.Text.Tertiary` text + `Brush.Stroke.Strong`
1px border so "disabled" looks intentional and on-theme. Apply the same to
`Button.Ghost` / `Ghost.Toggle` disabled states for consistency.

### 5b. `Button.Subtle` used where a `Button.Ghost` belongs

`Button.Subtle` is transparent + `Text.Secondary` + no border — correct for
truly inline links, wrong for standalone actions on a busy surface, where it
reads as disabled text. Re-point these to `Button.Ghost`:
- `Views/MapView.xaml` toolbar — "Reset view", "Clear all" (keep disabled-when-empty, but with 5a's treatment)
- `Views/AirwaysView.xaml:91` — "Undo last save"
- `Views/AirwaysView.xaml:220,273` — "Pick on map…" and the other subtle buttons
- ROI window "Cancel"
- Settings "Check for updates now", "Get the latest stable installer", "Browse…", "Clear"
- Systems fly-out "Re-check"
- Dashboard log "Minimize"

Keep `Button.Subtle` only for genuinely inline text actions inside a sentence.

### 5c. Dashboard log filter chips

`Views/DashboardView.xaml:81` `LogChip` — unchecked chips are
`Brush.Overlay` bg with default foreground, so "Info 27" etc. nearly vanish.
Give unchecked chips `Brush.Text.Secondary` text + a `Brush.Stroke.Strong`
hairline border; checked keeps `Brush.Accent.Soft`. Render the count as a
`Chip`/badge or at least `Brush.Text.Tertiary` so label vs count read distinctly.

---

## Priority 6 — Child windows use the native OS title bar

`ShellWindow.xaml` has the custom `WindowChrome` + caption buttons
(`Caption.Button` / `Caption.Close`). These do **not**:
- `Views/RoiPickerWindow.xaml`
- `Views/ConfirmWindow.xaml`
- `Views/UpdateWindow.xaml`

They render with the stock Windows chrome (light bar, default buttons) — jarring
against the app. Factor the `WindowChrome` + titlebar grid from `ShellWindow`
into a reusable style / `HeaderedWindow` base (or a `ControlTemplate` for
`Window`) and apply to all three. Match: `Brush.Panel` titlebar, drag region,
`Caption.Button`/`Caption.Close`, `Brush.Bg.Base` body,
`Brush.Stroke.Strong` 1px window border, `ResizeMode` per window.

Also: the ROI picker's map area renders **solid black** — the US-states
geometry that draws fine on the Map page and the Settings mini-map isn't loading
in that window. Check how `RoiEditor` / `MapCanvas` resolves
`Assets/us-states.json` (`Resource` include in the `.csproj`) when hosted in a
separate `Window` — likely a `pack://` URI / resource-assembly lookup that
assumes the main window's context.

---

## Priority 7 — Icons

- **Nav "AIRAC Service" icon** renders as a thin vertical rectangle (expanded
  *and* collapsed rail). The glyph key in `Theme/Icons.xaml` for that nav item is
  wrong / points at a codepoint the font doesn't have. `Font.Icon` is
  `"Segoe Fluent Icons, Segoe MDL2 Assets"` — pick a codepoint present in
  **MDL2** (the safe floor on Win10), e.g. `&#xE81E;` (map-ish) / `&#xEC92;` /
  `&#xE787;` (calendar, fits "AIRAC cycle").
- Audit every entry in `Icons.xaml` against MDL2, not just Fluent.
- The map canvas corner controls (Priority 1e).

---

## Priority 8 — Smaller items

- **News feed prints literal `**markdown**`.** `Views/DashboardView.xaml:30`
  binds `NewsPost.Body` straight into a `TextBlock`. Either strip/convert a
  minimal markdown subset (bold, links, line breaks) into `Inline`s in the
  view-model, or change the upstream news content. At minimum strip stray `**`.
- **Info page is a ~280px card in a large empty canvas** (`Views/InfoView.xaml`).
  Give the page a consistent max content width (match the other pages — Dashboard
  uses `MaxWidth="1180"`), and either widen the Resources card to that column or
  centre it and add the remaining real links. Right now it looks unfinished.
- **Panel borders barely visible.** `Card` uses `Brush.Stroke` (`#1E2C3B`) on
  `Brush.Bg.Base` (`#0B0F14`). Consider `Brush.Stroke.Strong` for card borders,
  or a 1px inner top highlight (`#0A layered white`) for a subtle raised edge.
  Taste call — confirm with the owner, but every screen currently reads as one
  flat field.
- **Collapsed nav** drops the Systems health widget entirely — no indicator when
  collapsed. Show the status dot (coloured) on the rail even when collapsed, as a
  `ToolTip`-on-hover affordance.
- **Systems fly-out panel** barely separates from the page and overlaps the
  "Systems / 1 needs attention" pill oddly. Give the popup `Brush.Panel` bg +
  `Brush.Stroke.Strong` border + the shared shadow, and offset it clear of the
  trigger.
- **`_` access-key artifact:** `_Lines` / `_Symbols` / `_Text` on
  `Views/AirwaysView.xaml:115-117` show literal underscores if the parent
  doesn't enable access keys / `RecognizesAccessKey`. Confirm they render as
  "Lines / Symbols / Text" with the underline only on Alt.

---

## Acceptance checklist

- [x] No `ComboBox`, `CheckBox`, `RadioButton`, or `TextBox` anywhere renders in
      the OS default look — open every dropdown, in every screen and the ROI window
- [x] Open ComboBox popup is dark (no white list, no blue selection bar)
- [ ] Dashboard activity-log message text is comfortably readable
- [ ] `Text.Tertiary` no longer used for any full-sentence copy
- [ ] AIRAC Services page scrolls smoothly from top to the last section; no dead zone
- [ ] Dashboard wheel scroll behaves the same wherever the pointer is
- [ ] Page titles + primary actions stay pinned while the body scrolls
- [ ] Disabled primary/ghost buttons look intentional, not broken
- [ ] ROI picker / Confirm / Update windows wear the app's custom title bar
- [ ] ROI picker map draws the states outline
- [ ] Nav "AIRAC Service" icon is a real glyph, expanded and collapsed
- [ ] News feed shows no literal `**`
- [ ] Light/dark: N/A (app is dark-only) — but verify no new hard-coded colours;
      everything routes through `Brush.*`
- [ ] `dotnet build -c Debug` clean; launch and click through all six screens
