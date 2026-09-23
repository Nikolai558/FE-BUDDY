# FE-Buddy 3.0 — session handoff to Claude Code

Written 2026-09-23. Hand this to a new Claude Code session so it can pick up where the Cowork
session left off. Everything below is current as of commit `c1252b1` plus uncommitted working-tree
changes (see "Repo state").

---

## 1. The project

FE-Buddy 3.0 is a from-scratch rewrite of a VATSIM ARTCC Facility Engineer tool
(old repo: https://github.com/Nikolai558/FE-BUDDY). C# / .NET 10, WPF desktop app.

Goals of the rewrite: purpose-built with the end goals in mind, attractive for outside
contributors, far more user control over output files and behaviour, and efficient.

Solution layout (`FE-Buddy-DEV/FeBuddy/FeBuddy.sln`), all projects `net10.0-windows`:

| Project | Role |
|---|---|
| `FeBuddy.Core` | All logic: NASR CSV parsing, services, GeoJSON/alias generation. No UI. |
| `FeBuddy.Wpf` | The app: MVVM view models, views, theme. |
| `FeBuddy.Harness` | Console harness that runs services with a hard-coded settings block. |
| `FeBuddy.UnitTests` | xUnit tests (Core only). |

Repo path on the dev machine: `C:\Users\ksand\Documents\VisualStudioProjects\FE-Buddy-DEV`
User config at runtime: `%APPDATA%\FE-Buddy\UserConfig.json` (+ `UserConfig.previous.json`,
the one-step undo snapshot).

---

## 2. How Kyle wants you to work

From the project instructions, plus how the session actually ran:

- **Act as technical lead.** Value is in architecture, sequencing and review. Push mechanical
  implementation down to sub-agents to save tokens; brief each sub-agent self-contained (file
  paths, exact interfaces, conventions, definition of done) and review what comes back.
- **State the plan before writing code** for anything vital or unclear, and wait for a go-ahead.
- **If a request conflicts with the codebase, say so before building it.**
- **Match existing style, structure and naming.** Read neighbouring files first. No new NuGet
  dependencies without asking. No placeholder or stubbed logic. Handle errors explicitly.
- **Verify by running code/tests.** If you could not verify, say exactly that and say how to test.
- **Lead with the result, then the reasoning. No preamble, no recaps.** Flag risks and tradeoffs
  unprompted.
- **Every piece of work ends with a copy/paste commit comment**, plus only the extra information
  Kyle must act on or could not tell from the commit itself. Keep it short.
- Kyle works item by item: he says "do #3", you do it, report, and wait before starting the next.

---

## 3. Build / test — the reason for the move

The Cowork session had **no .NET SDK**: its shell is an isolated Ubuntu VM on Kyle's laptop with
no network and no access to `C:\`, so `dotnet` could not run. **Nothing described in section 5 was
ever compiled or tested.** That is the single biggest risk in this handoff.

**First thing to do in Claude Code:**

```
cd C:\Users\ksand\Documents\VisualStudioProjects\FE-Buddy-DEV\FeBuddy
dotnet build
dotnet test
```

The dev machine has .NET SDK 10.0.401 (and 9.0.317) at `C:\Program Files\dotnet\dotnet.exe`.
Before this session's work, the suite was 434 passing, 0 warnings. Expect compile errors from the
unverified work; fix them, then re-run. Known "unsure" spots are listed in section 6.

---

## 4. Repo state

- Last commit: `c1252b1 Card header size set in one place: Text.SectionHeader style`.
- **The working tree has uncommitted edits** (at least `Theme/Typography.xaml` and
  `Controls/SectionHeader.xaml` from the last item). Review and commit them.
- **`git status` shows ~330 files modified purely from CRLF differences.** Add a `.gitattributes`
  (e.g. `* text=auto eol=crlf` for this repo's sources) as its own commit before any refactor, or
  diffs will stay unreadable. Until then use `git diff --ignore-cr-at-eol`.
- **A stale `.git\index.lock` may exist** (the Cowork VM could not delete files). If git complains,
  delete `FE-Buddy-DEV\.git\index.lock` and continue.
- File conventions: **CRLF everywhere**, no BOM in most files; **Core/Harness/tests use tabs**,
  **WPF (`FeBuddy.Wpf`) uses spaces**. Preserve per file.

---

## 5. What this session changed (each was its own commit)

In order. Everything is unverified by a compiler.

1. **Per-kind CRC ERAM defaults.** The single `IncludeCrcEramPropertyDefaults` setting became
   three: `IncludeCrcLineDefaults`, `IncludeCrcSymbolDefaults`, `IncludeCrcTextDefaults`, in
   settings records, parsers, GeoJSON services, Harness and WPF. Effective value = asked for AND
   that file is produced AND GeoJSON output is on (Airways: `OutputBy != None`). A kind's
   `Crc.<Class>.<Kind>.*` values are read and required only when effective.
   `CrcDefaultsReader.ReadInclude(settings, kind)` + `IncludeLineKey`/`IncludeSymbolKey`/
   `IncludeTextKey` constants. UI: the card-level "Include" checkbox is gone; each CRC panel has
   its own Include box (unticked = fields greyed but kept), and a panel disappears when its file is
   not produced. The file-picker section is now "WHAT FILES DO YOU WANT?" on every tab, and the
   word "Emit" no longer appears in any user-visible text (internal `Emit*` names unchanged).
2. **GeoJSON-only options grey out** when GeoJSON output is off, on all three tabs (Airports also
   greys its ROI card, which only filters GeoJSON). Airways was restructured to match Airports/
   Departures: GeoJSON Output card holds only the dropdown; FE-Buddy Properties and Buffer Airway
   Waypoints became their own cards; new `IsGeojsonOutputOn` VM property.
3. **Disabled styling.** Disabled control opacity scaled twice (~15% then ~10%): 0.45→0.38→0.34,
   0.40→0.34→0.31, 0.38→0.34→0.29. A disabled `Card` now also sinks to `Brush.Bg.Sunken` with a
   fainter border, and `Text.Display/H2/Body/Label` dim to 0.31 when disabled, so a whole card
   reads as unavailable. Watch for double-dimming if a control's label is itself a styled TextBlock.
4. **Facility picker removed** from the AIRAC General tab (it only fed a log line and a review row
   yet blocked runs). `AiracServiceSettings.ArtccId` deleted from Core. The home facility lives in
   Settings → Facility Profile (same `UserArtccId` key).
5. **Airways FE-Buddy properties** are now user-selectable like Airports/Departures:
   `AirwayFebProperty { AwyId, PointId, Waypoints }`, `FebProperties` setting replaces
   `IncludeAirwayWaypointIds`. Keys are camelCase now: Lines get `feb.awyId` (string) and
   `feb.waypoints` (ordered array); Symbols get `feb.awyId` (sorted array of every airway in that
   file that uses the point) and `feb.pointId`; Text gets `feb.awyId` only.
   Old keys `feb.AwyId` / `feb.AwyWaypoints` are gone.
6. **Departures ROI** matches the other sub-services: the "Use a region of interest" opt-in is gone
   (`Roi.UseRoi` ignored), the Settings default ROI applies unless overridden, new `HasRoi`, and the
   airport/any-point mode selector moved below the override fields and greys out when no ROI
   applies. ROI hint wording aligned across all three tabs.
7. **Airways review "Includes"** names what IS covered ("J, V airways"), never "all except";
   "Excluded designations" is the row directly below it. Before the cycle's designations load it
   says "Waiting for the cycle's airway list".
   **Open question:** in Kyle's screenshot the designation list was empty at review time even though
   the cycle was loaded. Worth confirming whether the Airways tab's "Designations to include" card
   actually populates; if it does and the review still says "Waiting…", there's a bug to chase.
8. **Departures amendment-date filter** (new): `DepartureAmendmentFilter { None, Cycles, Days, Date }`
   with settings `AmendmentFilter`, `AmendedWithinCycles` (1–1000, selected cycle counts as 1),
   `AmendedWithinDays` (1–36500, counts back from today), `AmendedOnOrAfter` (`yyyy-MM-dd`). Only the
   active mode's key is read/required. `DepartureFilter.ByProcedure` takes an optional `today` for
   tests; every mode is a lower bound, so future amendments are kept.
   `SettingsValueReader.RequiredIntInRange` added. UI lives in the Departures Procedures card, with
   UserConfig keys `Amendment.Filter/WithinCycles/WithinDays/OnOrAfter`.
   **Also added a themed WPF `DatePicker` + `Calendar`** (implicit styles in
   `Theme/Controls.Inputs.xaml`) because the stock ones render light. This is the least-verified
   piece in the whole handoff — it is a lot of re-skinned template XAML. One crash was already
   found and fixed there (icons must be `DynamicResource`, not `StaticResource`, inside those
   templates, because `Icons.xaml` is a sibling merged dictionary).
9. **"Nothing matched" advisories.** `ServiceMessage.IsAdvisory` (init-only) marks messages the GUI
   should surface on the Review tab. Departures/Airports/Airways each emit one when filters leave
   them nothing to write, and the Review tab shows them in an amber ADVISORIES card ("The run
   finished with no errors" hides when advisories are present).
10. **Airports `feb.rwyId`** (`AirportFebProperty.RwyId`): on the Runways Lines file only, an array
    of the drawable runways' IDs in the same order as the LineStrings. Runways Lines still carry no
    other `feb.*` properties — Kyle may want the airport's own properties there too; not done.
11. **Run results moved to the Review tab.** The LAST RUN card is gone from all three sub-service
    tabs, with all its state. `ISubServiceRunTarget` lost `BeginRun`/`ReportProgress`/
    `ApplyAiracResult`/`FailRun` and gained `SubServiceRunResult? DescribeRunResult(AiracServiceResult)`.
    New `Infrastructure/SubServiceRunResult.cs` (moved from `ViewModels/Models/SubServiceRunModels.cs`)
    holds the per-sub-service block: name, one-line count summary, warning groups, and info notices
    collapsed behind a toggle. The Review tab renders them in a RESULTS card. Errors and advisories
    keep their own cards and are excluded from the blocks.
12. **Card headers ~10% larger** via a new `Text.SectionHeader` style (11.5pt, based on
    `Text.Label`) in `Theme/Typography.xaml`; `Controls/SectionHeader.xaml` uses it. That style is
    the single place to size every card title.

---

## 6. Specific things to check first when the build runs

- The themed `DatePicker` / `Calendar` / `CalendarItem` / `CalendarDayButton` / `CalendarButton`
  templates (item 8) — required template part names, brush keys, triggers.
- `AirwayGeojsonService`: switch expressions inside lambdas typed as `Func<AirwayFebProperty, object?>`
  mixing `string` and `string[]` arms.
- `TheoryData<DepartureAmendmentFilter>` member data in the new Departures filter tests.
- Tests that were rewritten wholesale: `DepartureFilterTests`, `DepartureSettingsParserTests`,
  `AirwaySettingsParserTests`, `AirportSettingsParserTests`, new `CrcDefaultsReaderTests`,
  `AirwayGeojsonFebPropertiesTests`, `AirportGeojsonFebPropertiesTests`.
- Grep for leftovers of removed members after item 11 (the sub-agent reported none).
- UI smoke test in the app: every tab's CRC panels, the Departures amendment radios + calendar,
  the Review tab RESULTS/ADVISORIES cards, and that disabled cards look right.

---

## 7. What's next (agreed, not started)

1. **Full code review, report first, no code changes.** Kyle's ask, in his words: make sure there
   isn't duplicate code where functions could be combined, and no dirty code, comments or summaries
   left over from all the little tweaking. Deliver a ranked list — duplication (with a proposed
   shared piece for each), stale/incorrect comments and docs, dead code, inconsistencies between
   sub-services, risks — then let Kyle choose what to act on.
   The trigger for this was noticing that (a) the card-header size lived in several places and
   (b) the FE-Buddy Properties UI/parsing differed per sub-service when it should have been one
   template with per-sub-service variables. Likely candidates for a single shared version:
   the feb property picker (toggle model + card + parsing), the CRC defaults card, the ROI card,
   the "What files do you want?" card, Core settings parsing (feb names, ROI, CRC include flags),
   and how each sub-service writes `feb.*`.
2. **Then the refactor**, in small commits, one shared piece at a time, tests green between each.
3. **Then the deferred documentation work** (agreed earlier, still not started):
   - a developer guide `.md` under `docs/` explaining structure, conventions and the reasoning
     behind them — Airways waypoint buffering, DpName/ComputerCode rules, "Efficient Linestring
     Handling", empty CRC defaults, the settings/dirty contract and `SavedStateSnapshot`, etc.;
   - a cleanup of `docs/`: move what's still useful into the guide and flag the rest for Kyle's
     approval before removing anything.

---

## 8. Design decisions worth knowing (so you don't re-litigate them)

- **Settings contract:** every sub-service tab builds a `Dictionary<string,string>` block that the
  Core parser reads; the GUI and the Harness feed the same parsers. Unknown keys produce a warning,
  which is how retired keys degrade gracefully.
- **No legacy fallbacks** were kept for the renamed settings in this session; the old keys simply
  warn as unknown and the user re-picks (Kyle accepted this each time).
- **`feb.*` naming** is camelCase, and a property is only written on the Feature kind it describes
  (e.g. `pointId` on per-point Features, `waypoints`/`rwyId` on the line Feature). No lat/lon
  properties — the geometry already carries them.
- **CRC defaults** are never guessed: every value is required from the user, validated, and written
  as the isDefaults Feature at the head of its file.
- **Dev mode** is a code constant (`App.DevModeEnabled`), never a user setting. Pretty-printed
  GeoJSON is a user setting (`General.PrettyPrintGeojson`), forced on in dev mode.
- **Dirty tracking** compares against a `SavedStateSnapshot` of the saved values, app-wide.
- **The Review tab is now the single place** where a run's results, warnings, advisories, errors and
  file list live.

---

## 9. Reference: sample NASR data used for a sanity check

Kyle's cycle 2609 DP data showed the most recent ZOB SID amendment is 2025-11-27, so his
"ZOB + amended in the last 2 cycles" run legitimately matched 0 procedures and wrote nothing —
that is what prompted the advisories in item 9, not a bug. To see output with ZOB, use ~11 cycles,
~300 days, or on/after 2025-11-01.
