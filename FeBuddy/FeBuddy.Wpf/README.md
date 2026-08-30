# FeBuddy.Wpf

A **UI-only** WPF shell for FE-Buddy 3.0 - a modern re-skin in the style of
[clevelandcenter.org](https://clevelandcenter.org): dark blue-black surfaces, a
single amber accent, hairline cards, big display headings over airy body text.

It is deliberately **not wired to anything**:

- no reference to `FEBuddyLibrary` (or any project)
- no NuGet packages - the tiny MVVM helpers (`ObservableObject`, `RelayCommand`)
  and value converters live in `Infrastructure/`
- every screen shows **sample data**; buttons like *Generate GeoJSON* only set a
  status string

## Layout

```
Theme/                design system - the only place colours, type and control
  Palette.xaml          look are defined
  Typography.xaml
  Icons.xaml            Segoe Fluent Icons glyph code-points
  Controls.Buttons.xaml
  Controls.Inputs.xaml  Pill.Radio, Switch, Field
  Controls.Surfaces.xaml Card, Divider, Chip, nav row
  Theme.xaml            merges the above; App.xaml merges only this

Controls/             SectionHeader, StatTile (reusable UserControls with DPs)
Infrastructure/       ObservableObject, RelayCommand, converters
ViewModels/           ShellViewModel + one per screen, all with sample data
Views/                ShellWindow (custom chrome) + Dashboard / Airac / Settings
                      / Info / Placeholder
```

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
