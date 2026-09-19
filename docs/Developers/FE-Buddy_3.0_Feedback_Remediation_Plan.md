# FE-Buddy 3.0 — Feedback Remediation Plan (branch `Kickstart`)

> **Audience:** the model/engineer implementing the next round of work.
> **Source:** `FEB Feedback and Further Develpment.md` plus the owner's follow-up answers
> (2026-09-07), which are folded in and marked **[OWNER]** where they resolve an ambiguity.
> **Companion docs (read them; do not contradict them):**
> `FE-Buddy_3.0_Structure_And_Build_Plan.md` (§3 structure, §4 settings contract, §6 CRC rules),
> `FeBuddy/Developer_Notes.md` (UserConfig tree, launch processes, GUI wording),
> `FeBuddy/FeBuddy.Wpf/README.md` (design-system conventions).
>
> Where this plan and those docs disagree, **this plan wins**, and the doc is updated in the
> same commit (each phase names which).

---

## 0. How to use this document

- Phases are dependency-ordered. **Phases 0–3 block the GUI work.** Do not start screens before
  the foundations, the restructure, and the library behaviour corrections land.
- Every task names the files to touch and an acceptance check.
- §15 is a traceability table — every feedback bullet maps to a task. Nothing is silently dropped.
- §16 records every decision the owner has settled. **Nothing is left open** — implement as
  written, and raise anything that turns out ambiguous rather than guessing.

---

## 1. The three rules that govern everything below

### 1.1 Airways lives **inside** AIRAC Service — code and GUI

The single most important structural change this round. Today Airways is top-level in both
layers:

| Today | Must become |
|---|---|
| `FEBuddyLibrary/Services/Airways/` | `FEBuddyLibrary/Services/Airac/Airways/` |
| `FEBuddyLibrary/Models/Services/Airways/` | `FEBuddyLibrary/Models/Services/Airac/Airways/` |
| namespace `FEBuddyLibrary.Services.Airways` | `FEBuddyLibrary.Services.Airac.Airways` |
| namespace `FEBuddyLibrary.Models.Services.Airways` | `FEBuddyLibrary.Models.Services.Airac.Airways` |
| `AirwayService.Run(...)` called directly by the GUI | `AiracService.RunAsync(...)` called by the GUI; it dispatches to sub-services |
| Shell nav item **"Airways"** | **deleted** — Airways is reachable only as an AIRAC Service sub-service |
| `Views/AirwaysView.xaml` as a top-level screen | a sub-service page hosted inside the AIRAC Service screen |

The folder is `Airac/`, not `AiracService/`, and the orchestrator type is `AiracService` — a
namespace segment and a type of the same name in one chain is legal C# but painful at every call
site. The user-facing name stays **"AIRAC Service"**.

Nothing outside `Services/Airac/` may reference an Airways type, except the AIRAC Service
sub-service page in the GUI. No new top-level "Airways" anything, ever.

### 1.2 Production, not prototype

**[OWNER]** `FeBuddy.Wpf` has good prototype ideas; they are now to be *implemented*, not
demoed. Every screen that ships is wired to `FEBuddyLibrary` or it is deleted. A screen that
cannot be made real this round is removed from navigation, not left showing samples. See 0.5 for
the specific list.

### 1.3 Airways is the only sub-service with a backend

> **Amended 17 Sep 2026.** The original rule — *"Airways is the only sub-service that exists"*,
> strip every other reference, no placeholder, no greyed-out entry — is superseded. The owner
> replaced the sub-service *list* model with the tab model (5.2) after the list approach did not
> work out, and a tab model that only ever holds one tab proves nothing.

**[OWNER]** Airways is the only sub-service with a **backend**: the only one that produces
output, and the only one `AiracService.RunAsync` dispatches to.

The catalogue (`FeBuddy.Wpf/ViewModels/AiracSubServices.cs`) nevertheless holds three entries —
**Airports**, **Airways**, **Departures** — so the tab model is genuinely exercised and expansion
stays cheap: one catalogue entry plus a tab view-model, nothing else.

- Selecting an unimplemented sub-service opens its tab. That tab states plainly that its settings
  do not exist yet. It carries no controls, is never dirty, never invalid, never blocks a run and
  contributes nothing to one (`IsRunnable` is false; the Review tab lists it as producing
  nothing).
- Arrival Procedures, Fixes, NAVAIDs and Publications are not in the catalogue at all. They are
  added as entries when someone is ready to build them.
- What survives from the original rule, unchanged: **no fake data, no fake controls, nothing that
  pretends to work.** An unimplemented tab reports its own state; it does not mimic a settings
  menu and it never writes to `UserConfig`.

`AiracServiceSettings` stays shaped so a sub-service can be added later. (`Developer_Notes.md`
keeps its forward-looking spec for them — that is a design doc, not code.)

---

## 2. Current state (verified against the branch, 2026-09-07)

**Real and working — keep:**

- `FEBuddyLibrary/Services/Airways/*` — `AirwayService` → `AirwaySettingsParser` →
  `AirwayBuilder` → `AirwayGeojsonService` / `AirwayAliasService`, plus `AirwayNormalizer`,
  `AirwayGeometryBuilder`, `AirwayClassifier`, `AirwayWaypointBuffer`.
- `FEBuddyLibrary/Services/General/*` — `GeojsonFileWriter`, `CrcEramPropertyHandler`,
  `CrcGeojsonPropertyValidator`, `RoiFilter`, `AntimeridianHandler`, `AiracCycleResolver`,
  `NasrCycleDownloadService`.
- `FEBuddyLibrary/PARSERS/NASR/CSV/*` — `NasrCsvParserController.MainAsync(string[] dirs)`
  parses **all 24 NASR groups** concurrently.
- `UnitTests/Services/**` — real coverage; keep it green through the move.
- `FeBuddy.Wpf/Views/AirwaysView.xaml` + `AirwaysViewModel` — the only genuinely wired screen;
  its result panel (files, feature counts, warnings grouped by airway, collapsible) is good work
  and survives into the sub-service page.
- `FeBuddy.Wpf/Theme/*`, `Controls/*`, `Map/*`, `Infrastructure/*` — the design system.

**Sample data / scripted — must go (0.5):** `DashboardViewModel`, `AiracViewModel` (scripted
`DispatcherTimer` run), `SettingsViewModel`, `InfoViewModel`, `ConversionsViewModel`,
`GeoJsonToolsViewModel`, `AliasReferenceViewModel`, `MapViewModel`'s bundled sample layers,
`ShellViewModel.SystemHealth` / `AiracLabel` / `VersionLabel`, `PlaceholderViewModel`.

**Does not exist yet — the real cost of this round:**

- `UserConfig.json` read/write helper. Nothing persists today.
- Any logging system. No logger, no log file, no in-memory sink.
- News reader; version/update check plumbed into the GUI; UTC-time/internet check.
- A parsed-cycle cache. Every run re-parses from disk, and the GUI holds no parsed data.
- Coordinate-precision rounding — `GeojsonFileWriter` writes full precision, so the Settings
  control is decorative.
- Designation include/exclude filtering; per-kind (Lines/Symbols/Text) output opt-out; alias ROI
  scoping. None of these exist in `AirwaySettings`.
- An ROI picker dialog. `MapCanvas` can rubber-band an ROI, but only inside the Map screen.

### Measured facts (cycles 2608 / 2609 / 2610, already cached on the dev machine)

**Download and extract:**

| Fact | Value |
|---|---|
| Extracted cycle folder | ~110 MB, 114 files |
| Of which `.csv` | 87 files, ~105 MB |
| Non-CSV currently extracted | 27 files — 25 PDFs, a README, and a nested change-report `.zip` |
| CSVs `MainAsync` parses | all 24 groups, ~105 MB |
| CSVs Airways + the ARTCC dropdown actually read (`APT_BASE`, `AWY_BASE`, `AWY_SEG_ALT`, `FIX_BASE`, `NAV_BASE`) | ~24 MB — the size of the future perf refactor in 2.2 |

CSVs sit flat at the archive root — no `CSV_Data` subfolder — so flattening on extract is a
safety net, not a requirement.

**Waypoint resolution** — the airway pipeline was replayed against real cycle data (normalizer,
length-heuristic resolver, and `HasResolvableSegmentAhead` all reproduced faithfully):

| Cycle | Airways | Clean | Mid-route unresolvable (real fault) | Trailing border crossing |
|---|---|---|---|---|
| 2608 | 1466 | 1406 | **0** | 60 (4.1%) |
| 2609 | 1504 | 1444 | **0** | 60 (4.0%) |
| 2610 | 1504 | 1443 | **1** | 60 (4.0%) |

Every ID in that "unresolvable" column is a **border crossing**, not a missing waypoint — 10
distinct strings, 60 occurrences in 2609:

```
23  U.S. CANADIAN BORDER-1      4  U.S. MEXICAN BORDER-1
18  U.S. CANADIAN BORDER-2      3  U.S. CANADIAN BORDER-4
 6  U.S. CANADIAN BORDER-3      2  U.S. CANADIAN BORDER-6
 1  U.S. MEXICAN BORDER-2       1  U.S. CANADIAN BORDER-5
 1  U.S.CANADIAN BORDER-8       1  U.S.CANADIAN BORDER-2   <- note the FAA's missing space
```

**Zero** real waypoint IDs fail to resolve in any of the three cycles. NASR marks these points
structurally (blank `FROM_PT_TYPE`), so they must never be counted as resolution failures — see
3.2b before touching `AirwayGeometryBuilder` or `AirwayNormalizer`.

**Antimeridian waypoints** — three airway waypoints sit at *exactly* ±180.000000 longitude:

```
ARTOP    -0.87806   180.00000
MAZZA    11.70889  -180.00000
RESEE    20.60750  -180.00000
```

These are the cause of the degenerate zero-length legs seen in the run panel; see 3.9.

**Warning sources today** (the complete list in `Services/Airways/`): the two waypoint-resolution
warnings, `AirwayWaypointBuffer`'s "leg shorter than its combined buffer radius", and
`AirwaySettingsParser`'s unrecognized-key warnings. A real run reported **483 warnings**, and with
resolution warnings firing at most once per cycle, essentially all of them are buffer messages —
which are not warnings at all (3.8).

---

## 3. Phase 0 — Foundations (blocking)

### 0.1 Delete the abandoned front-ends **[OWNER]**

Remove `FeBuddyWPF`, `WPF`, and `WPFUI` — projects, folders, and `FeBuddy.sln` entries.
`FeBuddy.Wpf` is the app. Do this first, in a standalone commit, so nobody edits a dead project
by mistake (this branch already added a `DialogService` to `FeBuddyWPF`).

*Acceptance:* `FeBuddy.sln` contains `FeBuddy.Wpf`, `FEBuddyLibrary`, `FEBuddyTest`, `UnitTests`
and nothing else; solution builds.

### 0.2 `UserConfig` helper — `FEBuddyLibrary/Helpers/UserConfigFile.cs`

Per `Developer_Notes.md` → *USER CONFIGURATION FILE* and *LAUNCH PROCESSES → READ
UserConfig.json*:

- File: `%APPDATA%\FE-Buddy\UserConfig.json`.
- `ReadAll()` (file → global `UserConfig` dictionary), `Write()` (dictionary → file, then
  `ReadAll()`), `GetValue(string dottedPath)`.
- Add `TrySetValue(string dottedPath, string value)` and `Save(string nodePath)` so a
  sub-service menu saves only its own subtree.
- **Undo one step:** before every `Save(nodePath)`, snapshot that subtree into
  `%APPDATA%\FE-Buddy\UserConfig.previous.json` under the same path. Expose
  `bool CanUndo(nodePath)` and `Undo(nodePath)`. Depth is exactly one — a second save overwrites
  the snapshot. Do **not** implement a whole-file undo; saves are per sub-service and a file-level
  swap would clobber unrelated sections.
- Missing file / missing key never throws on the launch read path: return defaults, log a warning.
- **[OWNER]** Node naming is free to change: use `Services.AiracService.*` (Developer_Notes'
  `Services.AiracResources` is renamed in 1.4).

*Acceptance:* unit tests for round-trip, missing file, missing key, undo-after-save,
undo-with-no-snapshot, and per-node isolation (saving Airways does not revert Settings).

### 0.3 Application logging — `FEBuddyLibrary/Services/General/AppLog.cs`

The Dashboard activity log (Phase 6.4) is a view over this; build the sink first.

- `LogLevel { Debug, Info, Success, Warning, Error }`.
- `record LogEntry(DateTime UtcTimestamp, LogLevel Level, string Source, string Message)`.
- Static `AppLog.Write(level, source, message)`; `IReadOnlyList<LogEntry> Entries`;
  `event EventHandler<LogEntry> EntryAdded` (GUI subscribes, marshals to the dispatcher).
- File sink: `%APPDATA%\FE-Buddy\Logs\FE-Buddy_<yyyy-MM-dd>.log`, appended, never blocking the
  caller. Prune logs older than 30 days at launch.
- **Every existing `Warnings` collection also flows here**, and 3.8 replaces those flat string
  lists with levelled messages - do both as one change, not two. `AirwayService`,
  `AirwaySettingsParser`, `AirwayBuilder`, `AirwayGeojsonService`, `AirwayGeometryBuilder`,
  `NasrCycleDownloadService` accumulate warnings into result objects today. Keep returning them
  (the run panel uses them) *and* emit each to `AppLog`.
- `DevMode.IsEnabled` gates `Debug` entries only.

*Acceptance:* a run with warnings shows them in the result **and** the log file **and** raises
`EntryAdded` once per entry.

### 0.4 Launch sequence — `FeBuddy.Wpf/App.xaml.cs` + library launch services

Per `Developer_Notes.md` → *LAUNCH PROCESSES*, off the UI thread, status surfaced in the shell:

1. `TempWorkspace.ClearOnLaunch()` — wipe `%TEMP%\FE-Buddy` (Phase 2.1). Note there is legacy
   v2-era content in that folder on real machines; clearing the whole FE-Buddy temp tree is
   correct and safe — everything under it is disposable.
2. `UserConfigFile.ReadAll()`.
3. UTC time + internet check (timeapi.io → response `Date` header → local clock). Sets
   `bool HasInternetConnection`; all AIRAC/version/news features gate on it.
4. Version check (GitHub API, channel from `General.UpdateChannel`) → title-bar tooltip and
   version chip (Phase 4).
5. **AIRAC data pipeline (Phase 2)** — ensure previous/current/next are downloaded **and
   parsed**, as fast as possible.
6. News check (`NewsService`, Phase 6.1) → the News button's "new post" state.

Every step logs start/success/failure through `AppLog`. A failure never blocks launch; it
degrades the dependent UI and logs a warning.

**Ordering note:** this sequence is the skeleton, and two of its steps are filled in by later
phases — step 5 by Phase 2 and step 6 by 6.1. Build the skeleton with those steps stubbed and
logged, then wire them up in their own phases; do not try to finish Phase 0 by pulling Phase 2
forward.

*Acceptance:* launching in airplane mode neither crashes nor hangs; the log shows the internet
check failing, and dependent features are greyed with explanatory tooltips.

### 0.5 Delete the prototype surface

Delete outright — files, nav registration, and the `DataTemplate` in `ShellWindow.xaml`:
`ConversionsView` + `ConversionsViewModel`, `GeoJsonToolsView` + `GeoJsonToolsViewModel`,
`AliasReferenceView` + `AliasReferenceViewModel`, `PlaceholderView` + `PlaceholderViewModel`.

Gut the scripted run in `AiracViewModel` (`DispatcherTimer`, `BuildSteps`, `CountFor`, `RunStep`
progression) — Phase 7 replaces it with a real `AiracService` run.

Delete every sub-service reference other than Airways (rule 1.3): the `Families` collection, its
glyphs, `CountFor`, and the DP/STAR/Airports/Fixes/NAVAIDs/Publications rows.

In `ViewModels/Models.cs`, keep only what a real screen still binds; `OutputFamily`, `DiffRow`,
`HealthRow`, `OutputFile` and `DisplayItem` are all expected casualties.

*Acceptance:* `grep -ri "sample\|placeholder\|scripted\|not implemented\|UI only" FeBuddy.Wpf`
returns nothing outside comments describing removed behaviour.

---

## 4. Phase 1 — AIRAC Service owns Airways

### 1.1 Move the library code

Pure move + namespace rename, no behaviour change, one commit:

```
FEBuddyLibrary/Services/Airways/*                                -> FEBuddyLibrary/Services/Airac/Airways/*
FEBuddyLibrary/Models/Services/Airways/*                         -> FEBuddyLibrary/Models/Services/Airac/Airways/*
FEBuddyLibrary/Services/General/AiracCycleResolver.cs            -> FEBuddyLibrary/Services/Airac/
FEBuddyLibrary/Services/General/NasrCycleDownloadService.cs      -> FEBuddyLibrary/Services/Airac/
FEBuddyLibrary/Models/Services/General/AiracCycleInfo.cs         -> FEBuddyLibrary/Models/Services/Airac/
FEBuddyLibrary/Models/Services/General/AiracCyclePosition.cs     -> FEBuddyLibrary/Models/Services/Airac/
FEBuddyLibrary/Models/Services/General/AiracDownloadProgress.cs  -> FEBuddyLibrary/Models/Services/Airac/
```

`Services/General/` keeps only genuinely cross-service code: `GeojsonFileWriter`,
`CrcEramPropertyHandler`, `CrcGeojsonPropertyValidator`, `RoiFilter`, `AntimeridianHandler`,
`AppLog` — the future RADAR Video Map Conversion service needs all of them.

Update `FEBuddyTest/*` and `UnitTests/Services/**` in the same commit; test folders mirror source
folders (`UnitTests/Services/Airac/Airways/`).

*Acceptance:* solution builds; every existing unit test passes with no change beyond `using`s.

### 1.2 New orchestrator — `FEBuddyLibrary/Services/Airac/AiracService.cs`

```csharp
public static class AiracService
{
    public static Task<AiracServiceResult> RunAsync(
        AiracServiceSettings settings,
        IProgress<AiracServiceProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
```

- `AiracServiceSettings` (new, `Models/Services/Airac/`): selected cycle, ARTCC ID, output
  directory + "add FE-Buddy_Output folder" preference, default ROI, coordinate precision, and a
  nullable settings block **per sub-service** — today only `AirwaySettings? Airways`. Null means
  "not selected". No other blocks exist yet (rule 1.3).
- `RunAsync` takes the **already-parsed** `NasrCsvDataCollection` for the selected cycle from the
  cycle cache (2.4) - it does not parse. If the cycle is not yet parsed it awaits that
  cycle's parse task rather than starting a second one.
- `AiracServiceResult` aggregates per-sub-service results plus a combined warning list and the
  excluded-airway summary (Phase 3.2).
- `AirwayService.Run` stays public (tests and the harness call it), but the GUI no longer does.

### 1.3 Keep the settings-dictionary contract

`AirwaySettingsParser` keeps taking `Dictionary<string, string>` per build plan §4.2. The GUI
builds that dictionary **from `UserConfig` values**, never from live screen state, so "what was
saved" and "what runs" cannot diverge.

### 1.4 Doc updates, same phase

- `FE-Buddy_3.0_Structure_And_Build_Plan.md` §3 tree and §3.1 namespace table → new paths.
- `Developer_Notes.md` → `Services.AiracResources` renamed `Services.AiracService`; the
  `DepartureProcedures` config block stays as future spec but is explicitly marked unimplemented.
- `FeBuddy.Wpf/README.md` → rewritten screen list.

---

## 5. Phase 2 — AIRAC data pipeline: download **and parse**, all three cycles, ASAP

**[OWNER]** On launch, previous / current / next must be downloaded, parsed, and available as
fast as possible.

### 2.1 Download management (`%temp%`)

Files: `FEBuddyLibrary/Services/Airac/NasrCycleDownloadService.cs`, new
`FEBuddyLibrary/Helpers/TempWorkspace.cs`.

Today the zip lands in `%TEMP%\FE-Buddy\` and `ZipFile.ExtractToDirectory` dumps the whole
archive — 27 PDFs and a nested change-report zip included — into the cycle folder.

1. `TempWorkspace.DownloadsDirectory` = `%TEMP%\FE-Buddy\Downloads`; the zip downloads there as
   `<cycleId>_CSV.zip`.
2. Extract **only `*.csv` entries** into `%APPDATA%\FE-Buddy\AiracCycles\<cycleId>`, flattened:
   `ZipFile.OpenRead` + per-entry `ExtractToFile` for `.csv` entries, named by `entry.Name`.
   Guard duplicate flattened names (keep the first, log a warning); reject entries whose
   `FullName` contains `..`. This drops ~5 MB of PDFs and the nested zip per cycle and removes
   the extract-then-clean step entirely.
3. Delete the zip after a successful extract; leave it on failure for diagnosis.
4. `TempWorkspace.ClearOnLaunch()` empties `%TEMP%\FE-Buddy` recursively, best-effort, never
   throwing — wired into 0.4 step 1.
5. `IsCycleDataComplete` unchanged.
6. A failed download/extract raises a user-visible warning (`AppLog.Write(Warning, …)` + toast),
   per Developer_Notes.

### 2.2 Parse every NASR group **[OWNER]**

> *"Future options that we create are going to be depended on more than just those. So we are
> fine on parsing all, and as it becomes available we will unlock those features. We can refactor
> later to make it more efficient."*

Keep `NasrCsvParserController.MainAsync` as-is: all 24 groups, every cycle. **Do not add
selective parsing** — a later optimization, not this round's work.

Consequences to design around, not to work around:

- Parse **one cycle at a time**, in the priority order below, so peak memory is one in-flight
  parse plus the completed datasets rather than three concurrent parses. Within a cycle the
  existing per-group concurrency stays.
- Hold the parsed `NasrCsvDataCollection` per cycle in the cache (2.4). Three full datasets is
  the memory budget; do not also cache derived copies.
- Leave a `// TODO (perf)` at the parse call noting that a `NasrDataSet` flags overload would cut
  this to the ~24 MB Airways actually needs, so the refactor is easy to find later.

### 2.3 The next cycle may not exist yet — availability probing **[OWNER]**

> *"Download what's available. The meta file for the next will not be available until approx. 19
> days from effective date … If all data is available that is preferred (waiting for all three to
> be done), but if not all data is available then previous and current is fine."*

`AiracCycleResolver` returns the previous/current/next **identity** (ID + effective date) from a
static lookup table, and it will happily name a cycle the FAA has not published yet. Publication
is a separate fact and must be established separately:

- Add `AiracCycleAvailability.ProbeAsync(AiracCycleInfo)` — an HTTP `HEAD` (fall back to a
  ranged `GET`) against the cycle's CSV URL. `200` → published; `404`/`403` → not yet published.
  A network error is neither: it is `Unknown`, and the pipeline retries rather than concluding
  the cycle does not exist.
- **A not-yet-published next cycle is normal, not a failure.** It is logged at `Info`, never
  `Warning`, and never produces an error toast. This is the single most important behavioural
  detail in Phase 2 — the previous implementation would have treated it as a download failure
  every month.
- **Do not derive availability from a date rule.** There is no reliable lead-time constant for
  the NASR CSV subscription, so the hint for an unpublished cycle says
  "not yet published by the FAA" without inventing a date. The probe is the only authority.
- Re-probe on every launch, and expose a manual "check for the next cycle" action so a user who
  leaves the app open across the publication date is not stuck until restart.

#### The 15–18 day rule belongs to d-TPP, not to NASR **[OWNER]**

The "not available until ~15–18 days before the effective date" figure describes the **FAA d-TPP
Metafile**, which feeds **FAA Chart Recall alias commands** — a different data source and a
different feature from the NASR CSV subscription this phase downloads. Chart Recall is a future
sub-service (rule 1.3), so nothing in this round consumes d-TPP. Recorded here so it is not lost,
along with the message v2.x shows users today, to be carried forward when that service is built:

> The parser utilizes the FAA d-TPP Metafile to build the FAA Chart Recall alias commands. The
> d-TPP Metafile is only made available by the FAA 15-18 days prior to an AIRAC Effective Date.
> If you run the program earlier than 15-18 days prior to the next AIRAC Effective Date and you
> select "Next AIRAC" as the data you wish to parse, FAA Chart Recall alias commands will not be
> included with the output.

When Chart Recall is built, its availability check is **separate** from the NASR cycle probe
above: a cycle can be fully downloadable and parseable while its d-TPP metafile does not yet
exist, and that must degrade only the Chart Recall output — never the cycle.

### 2.4 Cycle data cache — `FEBuddyLibrary/Services/Airac/AiracCycleDataCache.cs`

- One entry per cycle: `{ AiracCycleInfo Cycle, CycleDataState State, Task<NasrCsvDataCollection>? ParseTask }`.
- `CycleDataState { NotYetPublished, NotDownloaded, Downloading, Downloaded, Parsing, Ready, Failed }`,
  with a `StateChanged` event the GUI binds to. `NotYetPublished` and `Failed` are different
  states and must be presented differently.
- Launch sequence: probe all three (2.3) → download and parse each **published** cycle in the
  order **current → previous → next**, one parse at a time (2.2). Current goes first because it
  is what the user almost always wants; previous is always published; next may not be.
- `GetAsync(cycleId)` returns the parsed collection, awaiting an in-flight parse rather than
  starting a second one.
- Retry a failed download once before marking `Failed`.
- Every transition logs through `AppLog`, so the Dashboard activity log narrates the whole launch.

### 2.5 Readiness gating in the GUI **[OWNER]**

**The AIRAC Service unlocks when every *available* cycle is downloaded and parsed** — not
per-cycle as each finishes. Availability is decided by 2.3, so a missing next cycle does not hold
the app hostage.

```
Ready       = current is Ready
              AND previous is Ready or Failed
              AND next is Ready, NotYetPublished, or Failed
Waiting     = any of the above still Downloading / Parsing / NotDownloaded
Degraded    = Ready, but previous and/or next is Failed  -> unlock, with a warning
Unavailable = current is Failed -> AIRAC Service stays disabled, error explains why
```

- **The current cycle is mandatory.** Everything else is best-effort: a failed previous or next
  cycle degrades the service (that cycle is unselectable) rather than blocking it.
- A `NotYetPublished` next cycle is shown in the cycle menu as disabled with the hint
  "not yet published by the FAA" - it is not an error and must not be styled as one.

Controls gated on readiness:
- The **ARTCC / facility dropdown** (Phase 9, Settings §2) — sourced from `Apt.AptBase`.
- The **designations list** (3.1 / 7.4) — sourced from `Awy.AwyBase`.
- **Run AIRAC Service**.

While waiting, show the Developer_Notes message — *"Waiting for AIRAC data to finish downloading
and parsing. This service will be available in a moment"* — plus per-cycle progress. The static
border indicator (4.3) shows the same state.

*Acceptance:* update `NasrCycleDownloadServiceTests` (moved to `UnitTests/Services/Airac/`) —
serve a zip containing `.csv`, `.pdf`, `.txt` and a nested folder; assert only `.csv` lands in
the cycle folder, flattened; assert the zip goes to `Downloads` and is removed; assert
`ClearOnLaunch` empties the temp tree. New tests for `AiracCycleAvailability` (200 → published,
404 → not published, network error → `Unknown` and retried) and `AiracCycleDataCache` (order
current → previous → next; no duplicate parse for concurrent `GetAsync` calls; `Failed` after
one retry; readiness `Ready` with next `NotYetPublished`; readiness `Unavailable` when current
fails; `Degraded` when previous fails).

---

## 6. Phase 3 — Airways pipeline corrections (library behaviour)

These are behaviour changes in `Services/Airac/Airways/`. Land them before the GUI binds to them.

Three of them (3.3, 3.4, 3.5) add settings, and two (3.9, 3.10) change geometry. Whenever a
setting is added, update all three of its homes in the same commit: the typed `AirwaySettings`
record, the key table in `AirwaySettingsParser`, and **`FEBuddyTest/HarnessSettings.cs`** — the
console harness mirrors the settings dictionary and is how these changes get exercised before the
GUI exists. Add the new keys to `FE-Buddy_3.0_Structure_And_Build_Plan.md` §4.2 as well.

### 3.1 Designation comes from `AwyId`, never from `AwyDesignation` **[OWNER]**

> *"Take nothing from AwyDesignations. Even though it is labeled 'designations', it is not what
> we consider an airway designation."*

- Add `Airway.Designation` — the leading characters of `AwyId` before the first digit
  (`^[A-Za-z]+`), upper-cased. `J3` → `J`, `V23` → `V`, `AT1` → `AT`.
- **Remove `Airway.AwyDesignation`** and every read of `AwyBase.AwyDesignation` in the pipeline.
  `AirwayGeojsonService` currently groups `OutputBy = Designation` files by that field; it must
  group by `Airway.Designation`. This also fixes the mismatch where RNAV airways were filed under
  `RN` while their IDs start with `Q`/`T`.
- An `AwyId` with no leading letters (should not occur) → `Designation = "Unknown"`, warn once.
- The same derived value feeds file naming, the exclusion filter (3.3), and the GUI toggle list.

*Acceptance:* unit tests for `J3`/`V23`/`AT1`/`Q100`/`T295` → `J`/`V`/`AT`/`Q`/`T`; a
`Designation` run produces `Airways_J_Lines.geojson` etc. and no `Airways_RN_*`.

### 3.2 An airway with any unresolvable waypoint is excluded entirely **[OWNER]**

> *"If it can't resolve a waypoint on an airway, don't include that airway … we still want to
> warn the user like how we tell them about it now, but we do not want to include a bad airway
> misleading the user when there are segments that were not correctly resolved."*

**Read the measurements in §2 first.** `AirwayGeometryBuilder` handles an unresolvable waypoint
in two different ways, and the real cycle data shows they are not two flavours of the same
problem — they are a real (but currently non-existent) data fault, and a normalizer gap:

| Case | Today | Real-world frequency | Action |
|---|---|---|---|
| **Mid-route** unresolvable point (a resolvable segment still lies ahead) | warns *"This segment was skipped"*, breaks the LineString, keeps building | **0, 0, 1** airways in 2608 / 2609 / 2610 | **Exclude the whole airway** — 3.2a |
| **Trailing border crossing** (a border marker terminates the airway) | silently `break`s, keeps what was built, no warning | 60 airways/cycle — **100 % are border crossings**, not missing data | **Fix the normalizer** — 3.2b |

#### 3.2a Exclude airways with a genuine mid-route resolution failure

Implement exactly as asked. It currently fires on ~0–1 airways per cycle, which is the point:
it is a guard against bad data reaching output, not a routine filter.

- `AirwayGeometryBuildResult` gains `IReadOnlyList<string> UnresolvedWaypointIds`.
- `AirwayBuilder` skips any airway with a non-empty `UnresolvedWaypointIds` — it is never added
  to `airways`, so it disappears from GeoJSON, the alias file, and `AirwayCount` at once.
- Keep the existing per-waypoint warning text (it is familiar) but change the tail from
  *"This segment was skipped."* to *"This airway was excluded from all output."*, and add one
  summary warning per excluded airway:
  `Airway 'J146': excluded from all output - 2 waypoint(s) could not be resolved (XYZ, ABCDE).`
- `AirwayBuildAllResult` / `AirwayServiceResult` gain `IReadOnlyList<string> ExcludedAirwayIds`;
  the run panel and the Dashboard log report the count.

#### 3.2b A border crossing is not an unresolved point **[OWNER]**

> *"If it's blank it's not an unresolved point, it's a border crossing."*

The 60 "unresolvable" airways per cycle are not truncated or broken. Their last NASR point is a
border marker — `U.S. CANADIAN BORDER-4`, `U.S. MEXICAN BORDER-1` — and NASR identifies these
structurally: **a border crossing is any point that appears as a `FROM_POINT` with a blank
`FROM_PT_TYPE`.** Verified across cycle 2609:

| Border marker appears as… | Count |
|---|---|
| `FROM_POINT` with **blank** `FROM_PT_TYPE` | 169 |
| `FROM_POINT` with a populated `FROM_PT_TYPE` | **0** |
| `TO_POINT` (which has no type column at all) | 139 |

So the rule is exact and needs no heuristic: build the set of reference-only IDs for the airway
from its blank-`FROM_PT_TYPE` rows, and treat membership in that set as "border crossing", never
as "unresolved".

**Why the trailing one leaks through today.** `AirwayNormalizer` already collapses these
mid-route (`TIJ → U.S. MEXICAN BORDER-2 → TEYON` becomes `TIJ → TEYON`). But NASR closes a
border-terminating airway with a *terminator row* that has an empty `TO_POINT`:

```
seq=200  FROM='CFJCC'                  FROM_PT_TYPE='CN'  TO='CFDCT'
seq=210  FROM='CFDCT'                  FROM_PT_TYPE='CN'  TO='U.S. CANADIAN BORDER-4'
seq=220  FROM='U.S. CANADIAN BORDER-4' FROM_PT_TYPE=''    TO=''          <- terminator
```

The normalizer's look-ahead reaches seq 220, correctly recognises it as reference-only, then hits
`if (string.IsNullOrWhiteSpace(nextSegment.ToPoint)) break;` — and breaks **while `endWptId` is
still the border marker**. The segment `CFDCT → U.S. CANADIAN BORDER-4` is emitted, fails
resolution downstream, and `AirwayGeometryBuilder` bails out of its loop. The airway comes out
right (J5 ends at CFDCT) but for the wrong reason, and silently.

**The fix, in `AirwayNormalizer`:** after building the segment list, drop any segment whose
`EndWptId` is in the airway's reference-only set. Equivalently, handle the terminator row in the
look-ahead by discarding the in-progress segment instead of keeping the marker as its end. Either
way the segment never reaches coordinate resolution.

- **No warning.** This is normal, expected NASR structure — J5 ending at CFDCT is the correct
  answer. 60 warnings a cycle for correct output is exactly the noise the feedback is removing.
- `AirwayBuilder.ResolveOrderedPoints` already skips blank-`FromPtType` rows; that behaviour stays
  and should now use the same shared helper so there is one definition of "reference-only point".
- After this, `HasResolvableSegmentAhead`'s trailing-tail `break` becomes genuinely exceptional.
  Keep it as a safety net, but an airway reaching it now has a real data problem — route it into
  3.2a's exclusion path rather than a silent `break`.

*Acceptance:* a synthetic airway with a mid-route unresolvable point produces zero features, zero
alias lines, one per-waypoint warning and one exclusion warning. A synthetic airway ending in a
border terminator row produces a clean airway ending at its last real waypoint, with **no**
warning. Real 2608/2609/2610 data produces **zero** resolution warnings, and J5, J13 and J126
still render.

### 3.3 Designation include/exclude

- `AirwaySettings` gains `IReadOnlyCollection<string> ExcludedDesignations`.
- `AirwaySettingsParser` accepts `ExcludedDesignations` (comma-separated, case-insensitive,
  trimmed).
- `AirwayBuilder` drops excluded airways **before** geometry work, so GeoJSON and the alias file
  agree and no time is wasted building geometry that is thrown away.
- Key is `Airway.Designation` from 3.1.

### 3.4 Per-kind output opt-out

`AirwaySettings` gains `EmitLines`, `EmitSymbols`, `EmitText` (all default true), honoured by
`AirwayGeojsonService`. All three false is valid only when `OutputBy = None`; otherwise the
parser throws a clear `ArgumentException` naming the conflict (the GUI blocks it earlier, at
save, per Phase 7.4).

### 3.5 Alias file: name and ROI scope **[OWNER]**

- Output file renamed `Draw_Airway_Points.txt` → **`Airways.txt`**.
- `AirwaySettings` gains `AliasRoiScope { All, RoiAirways }`.
  - `All` — every airway (current behaviour).
  - `RoiAirways` — include an airway if **at least one** of its waypoints falls inside the ROI,
    then include **all** of that airway's waypoints. Uses `RoiFilter` for the point test; it must
    not use the ROI-clipped geometry, since the whole point is that the alias draws the entire
    airway.
- Excluded airways (3.2) and excluded designations (3.3) never reach the alias file.

### 3.6 Coordinate precision

`GeojsonFileWriter` does no rounding today, so the Settings control is decorative. Add a maximum
decimal-places setting applied at write time (an NTS `PrecisionModel` on the geometry factory, or
coordinate rounding immediately before serialization), threaded `UserConfig` →
`AiracServiceSettings` → writer. Unit-test output at 5, 6 and 7 dp.

### 3.7 Output directory preference

`AirwayGeojsonService` and `AirwayAliasService` hard-code a `FE-Buddy_Output\Airways` subtree
under `OutputDirectory`. Move that behind the "Add FE-Buddy_Output folder" preference (Phase 9
§2) so switching it off writes exactly where the user pointed. The `\Airways` sub-folder stays —
it separates sub-service output — but the `FE-Buddy_Output` wrapper becomes conditional.


### 3.8 Message levels — most of today's "warnings" are not warnings **[OWNER]**

> *"Right now they are all warnings, but if a segment leg is too short to be 'buffered' it should
> just be info, not warning."*

A real run reports **483 warnings**, effectively all of them
`AirwayWaypointBuffer`'s "leg … is N NM long, shorter than its combined waypoint buffer radius of
5.0 NM. This leg was dropped." That is the buffer doing exactly what it was asked to do. Burying
a genuine problem under 483 routine notices is the failure mode here.

Every message the pipeline emits gets an explicit level (`AppLog`, 0.3), and results carry the
level rather than a flat `List<string> Warnings`:

| Message | Level |
|---|---|
| Buffer: leg shorter than its combined buffer radius, leg dropped | **Info** |
| Border crossing normalized away (3.2b) | **not emitted at all** |
| Airway excluded — genuine unresolvable waypoint (3.2a) | **Warning** |
| Degenerate zero-length geometry dropped (3.9) | **Info** once fixed; **Warning** if it still occurs after the fix |
| Unrecognized settings key | **Warning** |
| Missing/invalid required setting | **Error** (already throws) |

Mechanics:

- Replace `IReadOnlyList<string> Warnings` on the result records with
  `IReadOnlyList<ServiceMessage>` where `ServiceMessage = (LogLevel Level, string Source, string Text)`.
  Keep a `Warnings` convenience projection if it keeps the diff small, but the level must survive
  to the GUI.
- The Airways run panel groups by level and **does not** label the whole set "WARNINGS". A run
  with 483 info notices and 0 warnings should read as clean, with the info list collapsed by
  default.
- The Dashboard activity log (6.4) filters on the same levels, so these counts agree everywhere.

*Acceptance:* a real 2609 run with buffering on reports 0 warnings and ~483 info messages, and the
run panel presents it as a successful run rather than a wall of amber.

### 3.9 Degenerate zero-length geometry at the antimeridian (real bug)

The run panel shows legs like
`A450: a leg between (20.60750, -180.00000) and (20.60750, -180.00000) is 0.00 NM long` — the same
point twice. That is not a short leg; it is invalid geometry, and the only reason it is visible is
that the buffer happens to reject it. **With buffering off it is written into the GeoJSON.**

Cause, confirmed: three airway waypoints sit exactly on ±180 (`ARTOP`, `MAZZA`, `RESEE` — see §2).
When a segment ending at one of them crosses the antimeridian, `AntimeridianHandler.Split`
computes a crossing point identical to that endpoint and then starts the next fragment with

```csharp
current = new List<Coordinate> { new(antimeridianEnd.DecLon, antimeridianEnd.DecLat), end };
```

where `antimeridianEnd == end`. The result is a two-point LineString whose points are the same.

Fix in `AntimeridianHandler.Split`:

- Do not start a new fragment when the antimeridian point equals the segment's end coordinate —
  carry `end` alone into `current` instead.
- Before emitting any fragment, require **at least two distinct coordinates**; drop it otherwise.
- Apply the same guard in `AirwayGeometryBuilder.FinishCurrentLineString` so no path can emit a
  degenerate LineString.
- Also skip duplicate consecutive coordinates when building a LineString (the same defect would
  arise from a repeated waypoint in NASR).

*Acceptance:* unit tests for a segment whose endpoint lies exactly on ±180 in each direction, and
for a repeated coordinate; a 2609 run with buffering **off** contains no LineString with fewer
than two distinct coordinates.

### 3.10 Buffer real waypoints only — never synthetic vertices **[OWNER]**

> *"If the waypoint is a ±180 waypoint, buffer it. If the coordinate is artificial because we
> split it at the AM, then do not buffer. True fixes at the AM should be buffered, but if we
> created it because of the AM then don't buffer it."*

`AirwayWaypointBuffer.RadiusFor` looks each leg endpoint up in a coordinate index built from the
airway's resolved waypoints, and **falls back to `OtherRadiusNm` (5.0 NM) when nothing matches.**
Its own XML doc spells out what that fallback catches:

> *"A leg endpoint that does not match any known waypoint (e.g. a synthetic antimeridian-split or
> ROI-clip boundary point) falls back to OtherRadiusNm."*

That is backwards. Today **every artificial vertex is buffered by the maximum radius**, so:

- an airway split at the antimeridian is trimmed 5 NM back from the meridian on each side, and
- **an ROI-clipped airway is trimmed 5 NM inside the ROI boundary** — silently, on every clipped
  airway, for a vertex that is not a waypoint at all. `RoiFilter.ClipLineGeometry` uses
  `geometry.Intersection(roi.ToPolygon())`, which manufactures a vertex at every boundary
  crossing.

**Fix — invert the fallback.** `airwayPoints` contains exactly the airway's real resolved
waypoints, so "not in the index" *is* the definition of synthetic. An unmatched endpoint gets
radius **0** (no buffering); a matched endpoint keeps 2.5 NM (5-character fix) or 5.0 NM
(NAVAID / airport). Update the XML doc to match, since it currently documents the defect as
intended behaviour.

**The ±180 matching nuance — this is what makes the owner's rule work.** A real waypoint at the
antimeridian may be expressed by the split as the *opposite* sign: `RESEE` is stored at
`-180.00000`, but the fragment approaching it from the west ends at `+180.00000`. A naive
coordinate match fails, the endpoint looks synthetic, and the real waypoint silently loses its
buffer — precisely the case the owner called out. So **normalize longitude when building and
querying the index** (treat `-180` and `+180` as the same meridian). Then:

| Leg endpoint | Radius |
|---|---|
| `RESEE` at `-180`, or the same point expressed as `+180` | 2.5 NM — it is a real 5-character fix |
| A vertex the AM split invented where no waypoint exists | **0** — not buffered |
| A vertex ROI clipping invented at the boundary | **0** - not buffered |

**Interaction with 3.9:** both changes are still needed. 3.10 governs how legs are shortened;
3.9 stops a zero-length LineString from being created in the first place, which matters most when
buffering is **off** and nothing else would drop it.

**Knock-on effect:** far fewer legs will be dropped as "shorter than its combined buffer radius",
because synthetic endpoints stop consuming 5 NM each. Expect the ~483 Info messages (3.8) to fall
substantially — a short fragment near the ROI edge is no longer competing against a 5 NM buffer
that should never have been applied.

*Acceptance:* unit tests for (a) a leg ending at a real ±180 waypoint expressed as `+180` while
the waypoint is stored at `-180` → buffered at 2.5 NM; (b) a leg ending at a synthetic AM vertex
→ not buffered; (c) a leg ending at an ROI clip boundary → not buffered; (d) a leg between two
ordinary waypoints → unchanged from today. A 2609 ROI-clipped run with buffering on reaches the
ROI boundary instead of stopping 5 NM inside it.

---

## 7. Phase 4 — Static border (shell chrome)

File: `FeBuddy.Wpf/Views/ShellWindow.xaml` (+ `ShellViewModel`).

**4.1 FE-BUDDY tooltip** (~line 211). Remove the program description and the
`Build 3.0.0-dev · 30 Aug 2025 · .NET 10` line. Content is update state only, wording from
Developer_Notes → TITLE BAR: up to date → `You are running the latest version.`; update available
→ `vX.Y.Z available!`; offline → hidden/greyed with "update state unknown offline".

**4.2 Version chip becomes a button.** Click opens a modal **Update window**: current version,
latest on the user's channel, release notes from the GitHub release, **Download update** /
**Later**. FE-Buddy 3.0 ships as an **MSI installer** (no in-app self-update) —
**Download update** opens the GitHub release page where the new MSI lives; the user runs it to
update, keeping their settings. With no update available the chip is not clickable and says so
on hover. If the user dismisses an available update, colour the version text `Brush.Warn` for
the session (Developer_Notes).

**4.3 AIRAC status indicator → top-centre of the border.** Move the `AiracLabel` readout (now in
the bottom status bar, ~line 460) to the top-centre. It shows the current cycle ID + effective
date, and during launch it narrates the pipeline from 2.4 - `Downloading cycle 2610… 47%` →
`Parsing cycle 2610…` → `Cycle 2610 ready`. Values come from `AiracCycleDataCache` state, never
constants.

**4.4 Kill the oval highlight — universal.** `Theme/Controls.Surfaces.xaml`'s `Chip` uses
`Radius.Pill`; switch chip/badge/highlight styles to `Radius.Control` (or a new `Radius.Chip`
≈ 6 px). Audit every `Radius.Pill` usage; keep it only where a true pill is intended, and if
that is nowhere, delete the token. Covers the Dashboard hero kicker and the Settings facility
highlight the feedback calls out.

---

## 8. Phase 5 — Workspace menu, Service/Sub-service model

Files: `ViewModels/ShellViewModel.cs`, `ViewModels/NavItem.cs`, `Views/ShellWindow.xaml`.

### 5.1 Nav content
Primary nav is **Services only**:

- `Dashboard`
- `AIRAC Service` — icon `&#x25F7;` (U+25F7 ◷). **Not** a Segoe Fluent glyph, so it must render
  from a font that has it (Segoe UI Symbol / Segoe UI). Verify visually at both nav widths; if it
  renders as tofu, raise it rather than silently substituting.
- `Map` — **[OWNER]** stays as a Service (Phase 8).

Removed: `Airways` (rule 1.1), `Conversions`, `GeoJSON Tools`, `Alias & Reference` (0.5).
System nav keeps `Settings` and `Info`.

### 5.2 Sub-service navigation model — tabs

> **Amended 17 Sep 2026.** Replaces the hosted-page + `AIRAC Service › Airways` breadcrumb model
> described here before: the owner moved the screen to tabs after the list-and-page approach did
> not work out. **There is no breadcrumb.** `NavItem.SubServices` is left unpopulated — no nav
> item has sub-services; the tab rail does that job.

- A first-tier service screen is a **vertical tab rail down the left** plus one content pane.
  Tabs are data, not hand-placed XAML: `TabbedServiceViewModel` owns
  `ObservableCollection<ServiceTabViewModel> Tabs` and rebuilds it as the selection changes.
  AIRAC Service alone is expected to reach roughly twenty sub-services, which is why the rail is
  generated rather than authored.
- **General** is the permanent first tab: the cycle menu (7.2), the facility picker, and the
  sub-service picker (7.3). Ticking a sub-service means *produce data for this* **and** opens its
  tab. Unticking closes the tab and leaves that sub-service's saved `UserConfig` subtree
  untouched, so re-ticking restores its settings. The selection persists to
  `Services.AiracService.SelectedSubServices` (comma-separated keys).
- One tab per selected sub-service, in catalogue order. An unimplemented sub-service opens a tab
  that says its settings are not built yet and contributes nothing to a run (rule 1.3).
- **Review** is the last tab, present once at least one sub-service is selected. It summarises
  every other tab's settings as label / value rows, names any tab that is invalid (blocking) or
  unsaved (saved before the run), and hosts the single **Run AIRAC Service** button (7.9). The
  space under that button is where the live per-process run feed goes — **planned, not built this
  round.**
- A shared action bar sits above the tab's content and again at the end of it, so neither end of
  a long settings page is far from it: **Previous**, **Next**, **Review AIRAC Service settings**,
  **Undo last save**, **Save** (5.3). Previous / Next / Review offer to save a dirty tab before
  leaving it; **Cancel** keeps the user on that tab with their edits intact.
- Tab status shows in the rail as a dot: **amber** while the tab has unsaved edits, **red** when a
  value is missing or invalid. An invalid field highlights red in place with the validator's
  message as its tool-tip (e.g. an empty ROI override coordinate box).
- The machinery is generic — `TabbedServiceViewModel`, `ServiceTabViewModel`,
  `SubServiceDescriptor`, `ServiceReviewTabViewModel`, `PlaceholderSubServiceViewModel` — and is
  the model for the other first-tier services (file conversions, data viewers, file health) when
  they are built. No placeholders for those exist, and none are to be added.

### 5.3 The settings-save contract (every settings tab)

Unchanged in substance; it is now enforced **per tab**, and its buttons live in the shared action
bar (5.2). The General tab is a settings tab like any other.

- **Save** — validates, then writes only its own `UserConfig` subtree via
  `UserConfigFile.Save(nodePath)`.
- **Undo last save** — enabled while a one-step snapshot exists (0.2); disabled once a newer save
  overrides it.
- A dirty flag. Clicking **Run AIRAC Service** with any tab dirty shows the Developer_Notes
  dialog — *"the newly input data will be saved before execution"* — with **Cancel** /
  **Save & Continue**, naming the tabs. Leaving a dirty tab via Previous / Next / Review shows the
  same dialog for that tab.

---

## 9. Phase 6 — Dashboard

Files: `Views/DashboardView.xaml`, `ViewModels/DashboardViewModel.cs`.

**6.1 News is the primary view.** New `FEBuddyLibrary/Services/General/NewsService.cs`
implementing the `NewsChecker` sample in Developer_Notes (PostId regex
`PostId:\s*(\d{4}-\d{2}-\d{2})\.(\d+)`) plus a reader that returns post content for display.
Source: `FeBuddy/FEBuddyLibrary/News.md`, fetched from GitHub raw when online, falling back to
the bundled copy offline. On launch, parse and render the posts as the Dashboard's main content.
A **News** button opens the GitHub News document in the browser and changes colour when the
newest PostId is newer than `General.NewsLastOpen`; opening it writes that PostId back. A parse
failure leaves `NewsLastOpen` untouched, logs a warning, and surfaces it in the activity log.

**6.2 Description box (top).** Verbatim:

> An application designed to assist VATUSA Facility Engineers with routine, tedious, and
> sometimes complex tasks, including the production and maintenance of AIRAC cycle release
> resources, alias files, and GeoJSON files (including file health checks), ERAM and STARS
> adaptation conversions; and other facility engineering workflows.

Inside the box: a **Discord Server** hyperlink (moved out of Info, Phase 10) and the next-cycle
line — next AIRAC cycle ID, effective date, day counter — at the top or bottom of the box.
**"APRA verified" is deleted everywhere.**

**6.3 Deletions.** The "FE-Buddy at a glance" hero copy; the `OVERVIEW` section and every
`StatTile`; the `Recent Output` list; the `GeneratorStatus` sample rows (real cycle/download
status now lives in the static border, 4.3 — keep only what the border does not show).

**6.4 Activity log viewer (bottom).** A live view over `AppLog` (0.3):
- Filter chips across the top, each showing its count: All / Info / Success / Warning / Error,
  colour-coded (grey / green / amber / red) from `Theme/Palette.xaml` tokens — no hard-coded
  colours.
- Newest first, Zulu timestamps, source-tagged, virtualized.
- **Minimizable**: collapsed shows only the filter chips with counts.
- Includes entries that also went to the log file - one stream, one sink. The launch pipeline
  (2.4) narrating itself here is the main proof the log works.

---

## 10. Phase 7 — AIRAC Service screen (+ the Airways sub-service)

Files: `Views/AiracView.xaml` → `AiracServiceView.xaml`; `AiracViewModel` →
`AiracServiceViewModel`; `AirwaysView` / `AirwaysViewModel` become the Airways **sub-service**
page (rule 1.1), re-parented under the AIRAC Service screen.

**7.1 Header.** Title **"AIRAC Services"**. No per-family icons, no file counts.

**7.2 Cycle menu.** Previous / Current / Next selectable (`AiracCyclePosition` already supports
all three; `AirwaysViewModel.SelectedCyclePosition` and `SelectedCycleLabel` already do this
correctly — lift them up here rather than rewriting). The effective date follows the selection.
Remove the APRA row and `ApraNote`. Each option shows its cache state from 2.4 (`ready` /
`parsing…` / `failed`). Selection persists to `Services.AiracService.AiracCycleId`.

**7.3 Sub-service picker.** On the **General** tab (5.2): one tick per catalogue entry —
Airports, Airways, Departures (rule 1.3). Ticking one opens its tab; only Airways has a backend.
No "Alias & Reference" family — alias output is chosen inside the Airways tab (7.5).

**7.4 Airway options — corrections.**
- **HIGH/LOW and DESIGNATION descriptions**: multi-line for readability, and state that both
  modes also emit `_Symbols` and `_Text` files alongside `_Lines`.
- **FE-Buddy Properties description** — verbatim:
  - "Include FE-Buddy Properties, when available."
  - "Custom Geojson Property fields that increases file size but can be helpful for debugging or
    viewing data in a geojson viewer in order to identify object. Every FE-Buddy property will be
    prefixed with `feb.`"
- **Remove DME-Cutoff** — redundant with Buffer Airway Waypoints. GUI-only: there is no
  `DmeCutoff` in `AirwaySettings`; delete `AiracViewModel.DmeCutoff` and its step/count logic.
- **Remove "Break airways at fixes along route"** — `BreakAtFixes`, `BreakVor`, `BreakDme`,
  `BreakFix`, `BreakApt`.
- **Designations to include** — bound to the real list (library side is Phase 3.1/3.3):
  1. From the selected cycle's parsed `Awy.AwyBase`, take each `AwyId`.
  2. Derive the designation (leading letters before the first digit), distinct, sorted.
  3. One toggle per designation, **all on by default**, except those recorded as deselected in
     `UserConfig`.
  4. On save, write the *deselected* set to
     `Services.AiracService.Geojson.Airways.ExcludedDesignations`.
  5. Disabled until readiness is reached (2.5).
- **CRC ERAM Defaults** — restructure:
  - No LINE / TEXT / SYMBOLS selector button. Three sub-headed blocks, stacked, fields visible.
  - Each block gets an on/off toggle (`EmitLines` / `EmitSymbols` / `EmitText`, Phase 3.4). All
    three off is valid only when `OutputBy = None`; otherwise block the save with an inline error.
  - Every value box gets a tooltip stating that field's rule, worded from
    `CrcGeojsonPropertyValidator` and build plan §6.2 so tooltip and validator cannot drift.

**7.5 Aliases section.** Replaces the old top-level "Alias & Reference".
- Description with syntax, from Developer_Notes → ALIAS FILES: `.<awyId>F`, e.g.
  `.J3F .FF OAK RBL LKV IMB GEG`, phrased as "Provides a command to draw all airway waypoints on
  an ERAM or STARS window."
- Toggle: include the **`Airways.txt`** alias file — on by default.
- When on, a second toggle: **all FAA airways** vs **ROI airways only** (Phase 3.5).

**7.6 Cycle diffs.** Remove the section entirely (`CycleDiff`, `DiffRow`, `ProduceCycleDiff`).

**7.7 Region of interest.** Keep the "override the default ROI" toggle. When overriding, a button
opens the **ROI map picker** (Phase 11); values apply only on **Set ROI**, and cancelling changes
nothing. Manual lat/lon entry stays, validated by `RoiFilter.IsCoordinateValidFormat` /
`IsCoordinatesRelativePositionValid`.

**7.8 Split GeoJSON at antimeridian.** Its own section: a short description of what splitting at
the antimeridian means and why it matters, plus a toggle, **default on**, persisted to
`UserConfig`. (`SplitAtAntimeridian` already exists in `AirwaySettings`.)

**7.9 Build Cycle.** The single **Run AIRAC Service** button, on the **Review** tab (5.2) under
the settings rundown.
Pre-flight: dirty-settings dialog (5.3) → validation → `AiracService.RunAsync` against the cached
parse. Progress and results report through `AppLog` *and* the in-screen result panel — keep
`AirwaysViewModel`'s existing panel (files written, feature counts, messages grouped by airway,
collapsible), present it by level per 3.8, and add the excluded-airway count from 3.2a. Settings are written to
`UserConfig` after validation passes.

---

## 11. Phase 8 — Map Service **[OWNER]**

**[OWNER]** The Map Service does exactly two things: view GeoJSON, and manage the default ROI.
Everything else in the prototype goes.

Files: `Views/MapView.xaml`, `ViewModels/MapViewModel.cs`, `Controls/MapCanvas.cs`,
`Map/GeoJsonReader.cs`.

**Keep / build:**

- **Load and view GeoJSON** — a file-open dialog (multi-select), each file becoming a layer.
  `Map/GeoJsonReader` already parses standard GeoJSON via `System.Text.Json`; wire it to real
  files instead of the bundled `Assets/sample-airways.json`.
- **Layer list** — per-layer visibility toggle, feature count, remove, clear all.
- **Bad input is handled, not crashed on**: a malformed file, an unsupported geometry type, or an
  empty FeatureCollection warns through `AppLog` plus a toast; the other layers still load.
- **Default ROI, in place** (the same `RoiEditor` control as Phase 11):
  - If a default ROI exists in `UserConfig`, a toggle shows it on the map.
  - The user can draw a new one, or edit the existing one.
  - **Set / Save default ROI** writes `Services.AiracService.DefaultRoi.*` — the same node
    Settings writes, with the same validation and the same one-step undo. There is exactly one
    default ROI in the app, reachable from two places.
- Pan / zoom and the cursor lat/lon read-out stay — setting an ROI by eye without a coordinate
  read-out is guesswork. Keep the copy button on the ROI read-out.
- Keep `Assets/us-states.json` as the base outline layer — reference geography, not sample data.
  Note that in a comment so a future cleanup does not mistake it for a prototype leftover.

**Delete:**

- The bundled `Assets/sample-airways.json` layer and every other sample layer.
- The **ruler / measure** mode (`MeasureOnMap`, great-circle distance + bearing) and its read-out.
- The **Display drop-down / CRC BCG-and-filter visualiser** in its entirety.
- Copy buttons other than the ROI one.

---

## 12. Phase 9 — SYSTEM ▸ Settings

Files: `Views/SettingsView.xaml`, `ViewModels/SettingsViewModel.cs`. Section order matters.

**1. UPDATES (first).** Tooltips explaining `Stable`, `Beta`, `Alpha`, each carrying the warning
not to select anything but Stable unless instructed by the developers or you are one. Channel
persists to `General.UpdateChannel`, default `Stable`. "Check for updates now" (internet-gated)
and rollback to latest stable, per Developer_Notes.

**2. FACILITY PROFILE.**
- **Delete multi-profile support** — `Profiles`, `SelectedProfile`, `NewProfileCommand`,
  `ImportProfileCommand`, `ExportProfileCommand` and their UI. One facility, in `UserConfig`.
- Facility selection is a **dropdown**: from the selected cycle's parsed `Apt.AptBase`, take
  `RespArtccId` + `ArtccName`, de-duplicate, sort alphabetically, display as
  `ArtccName (RespArtccId)`. Persist `RespArtccId` to `Services.AiracService.UserArtccId`.
  **Disabled until readiness is reached** (2.5), with the waiting message.
- Default output directory `%USERPROFILE%\Desktop\FE-Buddy_Output`:
  - Checkbox **"Add FE-Buddy_Output folder"**, default on. Off means files go exactly where the
    user pointed (library side: Phase 3.7).
  - A directory and preference already in `UserConfig` win over the default.
- Soft-cornered rectangle highlight, not an oval (4.4).

**3. DEFAULT REGION OF INTEREST.** A button opens the ROI map picker (Phase 11); values apply
only on **Set ROI**. Keep the Developer_Notes ROI explainer text. Persists to
`Services.AiracService.DefaultRoi.*`.

**4. GEOJSON FILES** (renamed from "Output").
- FE-Buddy Properties description: the verbatim text from 7.4.
- **"Maximum Coordinate Precision"** (renamed), description: "Will round all coordinates in
  geojson files to a maximum number of decimal points in order to save space but retain your
  desired level of accuracy." Tooltips — 5dp: "General object references"; 6dp: "Airport layout
  tracing / CAB-like files"; 7dp: "High-precision navigation and airport tracing". Backed by
  Phase 3.6.

**Deleted sections:** `DISPLAY SCHEME` entirely (`SchemeBcg`, `SchemeFilters`,
`ExportLegendCommand`, `DisplayItem`) and `NASR DATA SOURCE` entirely (`CustomUrl`,
`LocalNasrPath`, `TestSourceCommand`, `BrowseNasrCommand`) — the FAA URL is the only source.

---

## 13. Phase 10 — SYSTEM ▸ Info

- **Delete the About menu** — redundant with the Dashboard description box.
- **Discord** → renamed "Discord Server", removed from Info, rendered as a hyperlink in the
  Dashboard description box (6.2).
- Keep Manual, Change log, Issues & requests — as real links, not sample rows.

---

## 14. Phase 11 — Shared ROI editor

**One control, three hosts.** Build `Controls/RoiEditor` once — `MapCanvas` with `RoiEnabled`,
the four lat/lon boxes, and a **Set ROI** / **Cancel** pair — and host it in:

1. the **Map Service** screen, in place (Phase 8),
2. **Settings ▸ Default Region of Interest** (Phase 9, Settings §3), as a modal `RoiPickerWindow`,
3. **AIRAC Service ▸ ROI override** (7.7), as the same modal.

Rules that apply to all three:

- Draw, then edit the numbers, then confirm. **Nothing reaches the caller until "Set ROI" is
  pressed**; Cancel discards, including a box the user drew.
- Validation on confirm via `RoiFilter.IsCoordinateValidFormat` /
  `IsCoordinatesRelativePositionValid`; an antimeridian-crossing ROI is rejected with the build
  plan's message.
- Hosts 1 and 2 write the **same** `UserConfig` node, `Services.AiracService.DefaultRoi.*`, and
  share its one-step undo. Host 3 writes the Airways override node instead and never touches the
  default.
- Delete `Infrastructure/DefaultRoiStore.cs` — the static one-slot store existed only because the
  Map screen and the Settings screen could not talk to each other. With one control writing one
  config node, it is dead code.

---

## 15. Traceability — feedback item → task

| Feedback item | Task |
|---|---|
| General: no prototype/placeholder data | 0.5, and every phase |
| General: focus on AIRAC ▸ Airways | 1.1, 1.2, 5.1 |
| **[OWNER]** prev/current/next downloaded, parsed, available ASAP | 2.1–2.5 |
| **[OWNER]** parse all NASR groups; optimize later | 2.2 |
| **[OWNER]** next cycle may not be published yet - download what is available | 2.3, 2.5 |
| **[OWNER]** blank `FROM_PT_TYPE` = border crossing, not an unresolved point | 3.2b |
| **[OWNER]** buffer "leg too short" is Info, not Warning | 3.8 |
| Zero-length legs at ±180 (found while tracing the 483 warnings) | 3.9 |
| **[OWNER]** buffer real ±180 waypoints, never synthetic split vertices | 3.10 |
| ROI-clipped airways trimmed 5 NM inside the boundary (same defect) | 3.10 |
| d-TPP 15–18 day rule recorded for the future Chart Recall service | 2.3 |
| Download: `%temp%\FE-Buddy\Downloads` | 2.1 |
| Download: unzip only `.csv` into AppData | 2.1 |
| Download: clean temp on launch | 0.4 (step 1), 2.1 |
| Border: tooltip drops ".NET 10" + description | 4.1 |
| Border: tooltip shows update availability | 4.1 |
| Border: version click opens update window | 4.2 |
| Workspace: AIRAC icon U+25F7 | 5.1 |
| Workspace: drop Conversions / GeoJSON tools / Alias & Reference | 0.5, 5.1 |
| **[OWNER]** Map stays a Service: view GeoJSON + manage default ROI | 5.1, Phase 8, Phase 11 |
| Workspace: Service ▸ Sub-service model | 5.2 |
| Workspace: RADAR Video Map Conversion as a future sub-service-less Service | 5.1 (noted, not built) |
| Workspace: per-sub-service settings, save, one-step undo | 0.2, 5.3 |
| Workspace: final "Get AIRAC Resources" run button | 7.9 |
| Workspace: unsaved-settings warning before running | 5.3, 7.9 |
| Dashboard: cycle badge shape + top-centre placement | 4.3, 4.4 |
| Dashboard: remove "FE-Buddy at a glance" | 6.3 |
| Dashboard: description box (verbatim) | 6.2 |
| Dashboard: activity log — filters, counts, colours, minimize | 0.3, 6.4 |
| Dashboard: keep cycle + download status indicators | 4.3 |
| Dashboard: remove Recent Output | 6.3 |
| Dashboard: News primary + GitHub button + new-post colour | 6.1 |
| Dashboard: remove APRA; keep next-cycle + day counter | 6.2 |
| Dashboard: remove Overview | 6.3 |
| AIRAC: rename to "AIRAC Services" | 7.1 |
| AIRAC: remove icons and file counts | 7.1 |
| AIRAC: previous cycle selectable; effective date follows | 7.2 |
| AIRAC: remove APRA verification | 7.2 |
| **[OWNER]** Airways is the only sub-service; strip the rest | 1.3, 0.5, 7.3 |
| **[OWNER]** AIRAC Service screen rebuilt as tabs; catalogue keeps unimplemented sub-services (17 Sep 2026) | 1.3 (amended), 5.2, 7.3, 7.9 |
| AIRAC: no "Alias & Reference" output family | 7.3, 7.5 |
| AIRAC: multiline HIGH/LOW + DESIGNATION descriptions | 7.4 |
| AIRAC: feb.* properties description (verbatim) | 7.4 |
| AIRAC: remove DME-Cutoff | 7.4 |
| **[OWNER]** designations from `AwyId`, never `AwyDesignation` | 3.1, 3.3, 7.4 |
| AIRAC: remove break-at-fixes | 7.4 |
| AIRAC: CRC ERAM defaults — subheaders, per-kind opt-out, tooltips | 3.4, 7.4 |
| AIRAC: Aliases section + ROI scope toggle | 3.5, 7.5 |
| **[OWNER]** alias file named `Airways.txt` | 3.5 |
| **[OWNER]** exclude any airway with an unresolvable waypoint | 3.2 |
| AIRAC: remove cycle diffs | 7.6 |
| AIRAC: ROI override via map picker with "Set ROI" | 7.7, Phase 11 |
| AIRAC: antimeridian split section | 7.8 |
| AIRAC: Build Cycle + save settings | 7.9 |
| Settings: soft-corner highlight (universal) | 4.4 |
| Settings: drop facility profiles | 12 §2 |
| Settings: Desktop\FE-Buddy_Output + opt-out + saved prefs | 3.7, 12 §2 |
| Settings: ARTCC dropdown from `Apt.AptBase` | 12 §2 |
| **[OWNER]** parser-dependent options wait for the parse | 2.4, 12 §2 |
| Settings: drop NASR data source | 12 |
| Settings: drop display scheme | 12 |
| Settings: Updates first, channel tooltips + warning | 12 §1 |
| Settings: default ROI via map picker | 12 §3, Phase 11 |
| Settings: rename Output → GeoJSON Files | 12 §4 |
| Settings: Maximum Coordinate Precision + tooltips | 3.6, 12 §4 |
| Info: remove About | 13 |
| Info: Discord renamed + moved to Dashboard hyperlink | 6.2, 13 |
| **[OWNER]** delete FeBuddyWPF / WPF / WPFUI | 0.1 |
| **[OWNER]** UserConfig tree may be renamed | 0.2, 1.4 |

---

## 16. Decisions log

Every decision below is settled. Where one contradicts an earlier draft, this table is right.

### Settled by the owner (2026-09-07)

| Was | Decision |
|---|---|
| Selective vs. full NASR parse | **Parse all 24 groups.** Unlock features as data becomes available; optimize later (2.2) |
| Designation source | **Never `AwyDesignation`.** Derive from `AwyId` everywhere (3.1) |
| UserConfig node names | **Free to change** → `Services.AiracService.*` (0.2, 1.4) |
| Map screen | **Stays a Service**: view GeoJSON + manage the default ROI; everything else deleted (Phase 8) |
| Alias file name | **`Airways.txt`** (3.5) |
| FeBuddyWPF / WPF / WPFUI | **Delete** (0.1) |
| Other sub-services | **Airways is the only backend**; the catalogue also lists Airports and Departures as tabs that produce nothing (1.3, amended 17 Sep 2026) |
| Parser-dependent controls | **Wait for the parse** (2.5) |
| Cycle readiness | **Wait for all *available* cycles**, not per-cycle unlock (2.5) |
| Border crossings | **Blank `FROM_PT_TYPE` = border crossing, not an unresolved point** (3.2b) |
| Cycle failure bar | **Current mandatory**, previous/next best-effort (2.5) |
| Buffer "leg too short" messages | **Info, not Warning** (3.8) |
| The 15–18 day rule | Describes the **d-TPP metafile / Chart Recall aliases**, not NASR (2.3) |
| Degenerate ±180 geometry | **Fix it this round** (3.9) |
| What gets buffered | **Real waypoints yes — including those at ±180; synthetic vertices no** (3.10) |
| Artificial coordinates generally | **Same rule regardless of origin** — AM split *and* ROI clip boundaries are never buffer anchors (3.10) |
| Run panel message display | Warnings/errors expanded, Info collapsed behind a count; full stream in the Dashboard log (3.8) |

### Open questions

**None.** Every question raised during planning has been answered. If something in this plan
turns out to be ambiguous during implementation, raise it rather than guessing — particularly anything
touching rule 1.1 (Airways stays inside AIRAC Service) or rule 1.3 (Airways is the only
sub-service with a backend), since both are easy to erode accidentally.

---

## 17. Definition of done for this round

1. Solution builds with four projects; `UnitTests` green, including new tests for
   `UserConfigFile`, `AppLog`, csv-only extraction, `AiracCycleAvailability`,
   `AiracCycleDataCache`, designation derivation, airway exclusion, border-crossing
   normalization, degenerate antimeridian geometry, message levels, designation filtering,
   alias ROI scope, and coordinate precision.
2. No `FeBuddy.Wpf` type outside the AIRAC Service screen references an Airways library type, and
   no nav item named "Airways" exists. The AIRAC Service screen is tabbed (5.2): General, one tab
   per selected sub-service, Review. Unimplemented sub-services are visible as tabs that state
   they are not built and contribute nothing to a run (1.3).
3. Launch on a clean machine probes, downloads and parses every **published** cycle among
   previous/current/next, narrates each step in the Dashboard activity log, and unlocks the
   service once all available cycles are ready. A next cycle that is not yet published is shown
   as such and never reported as an error.
4. Launching offline, launching with an empty `%APPDATA%\FE-Buddy`, launching with a populated
   config, and launching when the next cycle is unpublished all work without a crash.
5. A full run works end to end: pick cycle → configure Airways → Save → Build Cycle → real files
   at the configured output path, with airways containing genuine unresolvable waypoints excluded
   and reported, and every step visible in the activity log and the log file.
6. Against real 2608/2609/2610 data, a run with buffering on reports **0 warnings** — border
   crossings are normalized away rather than reported, and the ~483 buffer messages are Info —
   and J5 / J13 / J126 render, ending at their last real waypoint.
7. A run with buffering **off** contains no zero-length LineString; the Pacific airways through
   `ARTOP` / `MAZZA` / `RESEE` split cleanly at the antimeridian.
8. With buffering **on**: real waypoints are buffered (including `RESEE` when the split expresses
   it as `+180`), synthetic antimeridian and ROI-boundary vertices are not, and an ROI-clipped
   airway reaches the ROI edge rather than stopping 5 NM inside it.
9. Every shipped screen is real: the Dashboard renders News from `News.md` and a live activity
   log over `AppLog`; the Map Service loads a user's own GeoJSON and sets the default ROI;
   Settings persists to `UserConfig.json` and reloads on next launch; no screen shows sample data.
10. `FE-Buddy_3.0_Structure_And_Build_Plan.md`, `Developer_Notes.md`, and
   `FeBuddy.Wpf/README.md` describe what the code actually does.
