# FE-Buddy 3.0 — Project Structure & Airways Build Plan

**Status:** Authoritative build spec for the Airways work.
**Audience:** An AI coding assistant implementing this repo phase by phase.
**Scope of this document:** Everything needed to finish the **Airways** services in `FEBuddyLibrary`, driven from the `FEBuddyTest` console project. The GUI comes *after* this document's phases are complete.

---

## 0. How to use this document

1. Read §1 (Ground Rules) and §2 (What Already Exists) **before writing any code**. A large amount of working code already exists. Do not rewrite it — extend, move, and refactor it as directed.
2. Work the phases in §9 **in order**. Do not start a phase until the previous phase builds and its acceptance criteria pass.
3. §10 is the Decisions Log. It resolves contradictions found in `FE-Buddy_Dev_Notes.md`. Where the dev notes and this document disagree, **this document wins**.
4. Anything marked `[LATER]` is deliberately out of scope for this build. Do not implement it.

---

## 1. Ground Rules

These apply to every file you write.

### 1.1 Language / platform

- C#, `net10.0-windows`, `ImplicitUsings=enable`, `Nullable=enable`.
- NuGet packages already referenced and to be used: `NetTopologySuite` (2.6.0), `NetTopologySuite.IO.GeoJSON4STJ` (4.0.0), `CsvHelper` (33.1.0).
- Do **not** add new NuGet packages without a stated reason. Do not add `Newtonsoft.Json`; the old repo used it, this repo uses `System.Text.Json` via GeoJSON4STJ.

### 1.2 Code style

- **Visual Studio XML summary comments on every public and internal type and member.** This is a hard requirement from the dev team. Include `<param>`, `<returns>`, and `<remarks>` where they add meaning.
- Inline comments are encouraged. Assume the next reader is a new contributor, not an expert.
- Tabs for indentation (matches existing files in `FEBuddyLibrary`).
- File-scoped namespaces (`namespace X;`) for new files.
- Explicit types over `var` for non-obvious types; the existing generator code is a good style reference.
- Any repeated logic becomes a helper. Do not copy/paste a block into two services.

### 1.3 MVVM

FE-Buddy uses MVVM. Practically, for this phase that means: **`FEBuddyLibrary` must contain zero UI code and zero `Console` calls.** All user-facing output happens in `FEBuddyTest` (and later the GUI). The library communicates via return values, typed result objects, and thrown exceptions.

### 1.4 DevMode

A single global switch changes troubleshooting behavior.

- Location: `FEBuddyLibrary/Configuration/DevMode.cs`, `public static class DevMode { public static bool IsEnabled { get; set; } }`
- Default: `false`.
- Set by the caller (`FEBuddyTest.Program` sets it to `true`).
- **Effects in this phase:**
  - `IsEnabled == true` → GeoJSON is written pretty-printed (`WriteIndented = true`).
  - `IsEnabled == false` → GeoJSON is written single-line (`WriteIndented = false`) to save disk space.
  - `IsEnabled == true` → generator result objects include the full per-airway warning list (see §5.6).

### 1.5 GeoJSON output rules

- RFC 7946. Coordinate order is **`[longitude, latitude]`**. NetTopologySuite `Coordinate.X` = longitude, `.Y` = latitude. This is the single most common bug in this codebase — assert it in tests.
- Single-line output unless `DevMode.IsEnabled`.
- FE-Buddy custom property names are prefixed `feb.` and are always emitted as double-quoted JSON keys (System.Text.Json does this automatically): e.g. `"feb.AwyId"`.
- "Efficient LineString Handling" is a FE-Buddy term meaning: consecutive related points are merged into a single `LineString`, and a single logical object with breaks becomes one Feature with a `MultiLineString`. **One airway = exactly one Feature.**

### 1.6 Naming

- A user-facing capability is a **Service** (e.g. "Airway GeoJSON", "Airway Alias").
- Service code lives under `FEBuddyLibrary/Services/<ServiceName>/`.
- Code shared by two or more services lives under `FEBuddyLibrary/Services/General/`.
- Non-service code (parsers, models, config, cross-cutting handlers) lives **outside** `Services/`.

---

## 2. What Already Exists (do not rewrite)

Inventory as of this document. Paths are relative to `FeBuddy/`.

### 2.1 `FEBuddyLibrary` — working and to be kept

| Path | What it is | Action |
|---|---|---|
| `PARSERS/NASR/CSV/*.cs` (25 files) | Full async NASR CSV parser suite. `NasrCsvParserController.MainAsync(string[] args)` parses every group in parallel and returns `NasrCsvDataCollection`. | **Keep as-is.** Do not touch. |
| `Models/NASR/CSV/*.cs` (24 files) | Typed models for every NASR CSV group, with FAA field documentation in XML comments. | **Keep as-is.** Read `AwyCsvDataModels.cs` carefully; it is the airway data contract. |
| `Handlers/CSV/FebCsvHelper.cs` | `ProcessLines<T>`, `ParseInt`, `ParseNullableInt`, `ParseDouble`, `ParseNullableDouble`. | Keep. |
| `Handlers/CSV/FindWaypointCoordinates.cs` | `GetCoordinates(allNasrCsvData, waypointId, WaypointType?)` → `(lat, lon, foundIn)?`. 5-char IDs search FIX, others search NAVAID then Airport. | Keep, but **must be optimized** — see Phase 1.4. |
| `Handlers/CoordinateHandler.cs` | `IsValidDecimal`, `IsValidDMS`, `ToDMS`, `ToDecimal`, `Distance`, `CrossesAntimeridian`, `Bearing`, `SplitLineSegmentAtAntimeridian`. | Keep, **add one method** — see Phase 1.5. |
| `Handlers/FileHandler.cs` | `_tempDirectory`, `UnzipAllDownloads`, `CreateTempDirectory`, `CleanTempDirectory`. | Keep. |
| `Handlers/UdateHandler.cs` | Legacy in-app updater. | **Deleted** — 3.0 ships as an MSI installer; there is no in-app self-update. |
| `Models/Location/Location.cs` | Dual DMS/decimal point model with validation. | Keep. |
| `Models/General/AiracCycleIdEffectiveDates.cs` | AIRAC cycle ID ↔ effective date lookup (`yyyy-MM-dd` and `dd_MMM_yyyy`). | Keep. Note: currently `internal` — will need to become `public` when the download manager is built `[LATER]`. |
| `Generators/NASR/AWY-Geojsons/AwyGeojsonGenerator*.cs` (5 partials) | Working airway geometry builder: settings parsing, segment normalization (collapses reference-only border points), LineString/MultiLineString assembly with gap handling, unresolved-trailing-waypoint tolerance. | **Move and refactor** into the Services layout — see Phase 2. The *logic* is good; the *organization and outputs* are incomplete. |

### 2.2 `FEBuddyTest` — the GUI stand-in

Currently a single `Program.cs` with hard-coded paths that parses NASR and calls `AwyGeojsonGenerator.Generate`. This project is rebuilt in Phase 5 to be the harness described in §8.

### 2.3 `UnitTests`

`UnitTests/Handlers/CoordinateHandlerTests.cs`, `UnitTests/Library/Models/LocationTests.cs`. Confirm the test framework from `UnitTests.csproj` before adding tests.
`FEBuddyTest/AWY_GEOJSON_RegressionTests.md` contains a written Stage-0 regression plan (tests B01–B15+). Treat it as the source for Phase 5 test cases.

### 2.4 WPF projects — leave alone this phase

`FeBuddy.Wpf`, `FeBuddyWPF`, `WPFUI`, and the out-of-solution `WPF` folder are four parallel unfinished front-ends. `FeBuddy.Wpf` is the intended keeper. **Do not modify any of them during this build.**

### 2.5 `Project-Structure.md` (repo root)

Describes a four-project Clean Architecture layout (Domain / Application / Infrastructure / Desktop). **That layout is aspirational and is NOT what you build now.** It is a post-GUI migration target. Add a note at the top of that file saying so (Phase 0).

---

## 3. Target Project Structure

Only paths marked **NEW** are created by this build. Everything else already exists.

```text
FE-Buddy-DEV/
└── FeBuddy/
    ├── FeBuddy.sln
    │
    ├── FEBuddyLibrary/
    │   ├── Configuration/                              NEW
    │   │   └── DevMode.cs                              NEW
    │   │
    │   ├── Handlers/                                   (existing, cross-cutting)
    │   │   ├── CoordinateHandler.cs                    (+ PointAtDistanceAndBearing)
    │   │   ├── FileHandler.cs
    │   │   ├── UdateHandler.cs                          DELETED (3.0 is MSI-only, no in-app updater)
    │   │   └── CSV/
    │   │       ├── FebCsvHelper.cs
    │   │       └── FindWaypointCoordinates.cs          (+ index-backed lookup)
    │   │
    │   ├── Models/                                     (existing, non-service data)
    │   │   ├── General/
    │   │   ├── Location/
    │   │   ├── NASR/CSV/
    │   │   ├── ...
    │   │   ├── Geojson/                                NEW
    │   │   │   ├── CrcLineProperties.cs                NEW
    │   │   │   ├── CrcSymbolProperties.cs              NEW
    │   │   │   ├── CrcTextProperties.cs                NEW
    │   │   │   ├── CrcFeatureKind.cs                   NEW  (enum: Line, Symbol, Text)
    │   │   │   └── CrcPropertyValidationResult.cs      NEW
    │   │   │
    │   │   └── Services/                               NEW
    │   │       ├── General/
    │   │       │   ├── RegionOfInterest.cs             NEW
    │   │       │   ├── ServiceResult.cs                NEW
    │   │       │   ├── UtcTimeCheckResult.cs           NEW  (Phase 0.4)
    │   │       │   ├── VersionCheckResult.cs           NEW  (Phase 0.4, + UpdateChannel enum)
    │   │       │   └── LaunchProgress.cs               NEW  (Phase 0.4)
    │   │       └── Airac/                              (Phase 1.1 — Airways now lives here)
    │   │           ├── AiracCycleInfo.cs               (moved from General/)
    │   │           ├── AiracCyclePosition.cs           (moved from General/)
    │   │           ├── AiracDownloadProgress.cs        (moved from General/)
    │   │           ├── AiracServiceSettings.cs         NEW  (Phase 1.2)
    │   │           ├── AiracServiceResult.cs           NEW  (Phase 1.2)
    │   │           ├── AiracServiceProgress.cs         NEW  (Phase 1.2)
    │   │           └── Airways/
    │   │               ├── AirwaySettings.cs
    │   │               ├── AirwayGeojsonOutputBy.cs    (enum)
    │   │               ├── AirwayAltitudeClass.cs      (enum: High, Low, Other)
    │   │               ├── Airway.cs
    │   │               ├── AirwayPoint.cs
    │   │               └── AirwaySegment.cs
    │   │
    │   ├── Helpers/                                    NEW
    │   │   ├── UserConfigFile.cs                       NEW  (Phase 0.2)
    │   │   └── TempWorkspace.cs                        NEW  (Phase 0.4)
    │   │
    │   ├── PARSERS/NASR/CSV/                           (existing, untouched)
    │   │
    │   └── Services/                                   NEW
    │       ├── General/
    │       │   ├── GeojsonFileWriter.cs                NEW
    │       │   ├── CrcEramPropertyHandler.cs           NEW
    │       │   ├── CrcGeojsonPropertyValidator.cs      NEW
    │       │   ├── RoiFilter.cs                        NEW
    │       │   ├── AntimeridianHandler.cs              NEW
    │       │   ├── AppLog.cs                           NEW  (Phase 0.3)
    │       │   ├── AppEnvironment.cs                   NEW  (Phase 0.4)
    │       │   ├── UtcTimeCheck.cs                     NEW  (Phase 0.4)
    │       │   ├── VersionCheck.cs                     NEW  (Phase 0.4)
    │       │   └── LaunchSequence.cs                   NEW  (Phase 0.4)
    │       │
    │       └── Airac/                                 (Phase 1.1)
    │           ├── AiracService.cs                     NEW  (Phase 1.2 — AIRAC Service orchestrator)
    │           ├── AiracCycleResolver.cs               (moved from General/)
    │           ├── NasrCycleDownloadService.cs         (moved from General/)
    │           └── Airways/
    │               ├── AirwayService.cs                (public entry point; called by AiracService, tests, harness)
    │               ├── AirwayBuilder.cs                (CSV -> Airway objects)
    │               ├── AirwayNormalizer.cs
    │               ├── AirwayGeometryBuilder.cs
    │               ├── AirwayClassifier.cs             (High/Low/Other + designation)
    │               ├── AirwayWaypointBuffer.cs
    │               ├── AirwaySettingsParser.cs
    │               ├── AirwayGeojsonService.cs
    │               └── AirwayAliasService.cs
    │
    ├── FEBuddyTest/                                    (rebuilt in Phase 5)
    │   ├── Program.cs                                  REWRITTEN
    │   ├── HarnessSettings.cs                          NEW
    │   ├── ConsoleReport.cs                            NEW
    │   ├── AirwayGeojsonRunner.cs                      NEW
    │   └── AirwayAliasRunner.cs                        NEW
    │
    └── UnitTests/                                      (extended in Phase 5)
        ├── Handlers/
        ├── Library/Models/
        └── Services/Airac/                             (Phase 1.1 — Airways, launch + AiracService tests live under here)
```

### 3.1 Namespaces

| Folder | Namespace |
|---|---|
| `Configuration/` | `FEBuddyLibrary.Configuration` |
| `Helpers/` | `FEBuddyLibrary.Helpers` |
| `Models/Geojson/` | `FEBuddyLibrary.Models.Geojson` |
| `Models/Services/General/` | `FEBuddyLibrary.Models.Services.General` |
| `Models/Services/Airac/` | `FEBuddyLibrary.Models.Services.Airac` |
| `Models/Services/Airac/Airways/` | `FEBuddyLibrary.Models.Services.Airac.Airways` |
| `Services/General/` | `FEBuddyLibrary.Services.General` |
| `Services/Airac/` | `FEBuddyLibrary.Services.Airac` |
| `Services/Airac/Airways/` | `FEBuddyLibrary.Services.Airac.Airways` |

> **Phase 1.1 (remediation plan):** Airways moved *inside* AIRAC Service. The old
> `FEBuddyLibrary.Services.Airways` / `FEBuddyLibrary.Models.Services.Airways`
> namespaces and folders no longer exist; nothing outside `Services/Airac/` may
> reference an Airways type. `AiracCycleResolver`, `NasrCycleDownloadService`, and
> the `AiracCycle*` / `AiracDownloadProgress` models moved from `…/General/` to
> `…/Airac/` in the same change.

Existing namespaces (`FEBuddyLibrary.Handlers`, `FEBuddyLibrary.Parsers.NASR.CSV`, `FEBuddyLibrary.Models.NASR.CSV`, …) are unchanged.

---

## 4. Settings Contract

The GUI (and today, `FEBuddyTest`) hands the library a `Dictionary<string, string>`. The library immediately converts it to a typed record. **Only the parser touches the dictionary; the rest of the library uses the typed object.**

### 4.1 `AirwaySettings` (typed)

```csharp
public sealed record AirwaySettings
{
    public required string OutputDirectory { get; init; }
    public required AirwayGeojsonOutputBy OutputBy { get; init; }   // None | HighLow | Designation
    public required bool BufferAirwayWaypoints { get; init; }
    public required bool IncludeFebCustomProperties { get; init; }
    public required bool IncludeAirwayWaypointIds { get; init; }
    public required bool GenerateAliasFile { get; init; }
    public required bool SplitAtAntimeridian { get; init; }
    public required bool IncludeCrcEramPropertyDefaults { get; init; }
    public RegionOfInterest? Roi { get; init; }                     // null = no ROI filtering
    public IReadOnlyDictionary<AirwayAltitudeClass, CrcLineProperties>   LineDefaults   { get; init; }
    public IReadOnlyDictionary<AirwayAltitudeClass, CrcSymbolProperties> SymbolDefaults { get; init; }
    public IReadOnlyDictionary<AirwayAltitudeClass, CrcTextProperties>   TextDefaults   { get; init; }
}
```

### 4.2 Dictionary keys accepted by `AirwaySettingsParser.Parse`

| Key | Values | Required | Default |
|---|---|---|---|
| `OutputDirectory` | non-empty path | yes | — |
| `OutputBy` | `None` \| `HighLow` \| `Designation` (case-insensitive) | yes | — |
| `BufferAirwayWaypoints` | `Y` \| `N` | no | `N` |
| `IncludeFebCustomProperties` | `Y` \| `N` | no | `N` |
| `IncludeAirwayWaypointIds` | `Y` \| `N` | no | `N` |
| `GenerateAliasFile` | `Y` \| `N` | no | `Y` |
| `SplitAtAntimeridian` | `Y` \| `N` | no | `Y` |
| `IncludeCrcEramPropertyDefaults` | `Y` \| `N` | no | `N` |
| `FilterByRoi` | `Y` \| `N` | no | `N` |
| `RoiSwLat`, `RoiSwLon`, `RoiNeLat`, `RoiNeLon` | decimal degrees | required when `FilterByRoi=Y` | — |
| `ExcludedDesignations` | comma-separated designations (from `AWY_ID`), case-insensitive, trimmed | no | *(none)* |
| `EmitLines` \| `EmitSymbols` \| `EmitText` | `Y` \| `N` | no | `Y` |
| `AliasRoiScope` | `All` \| `RoiAirways` (case-insensitive) | no | `All` |
| `CoordinatePrecision` | int `0`–`15` | no | `6` |
| `AddFeBuddyOutputFolder` | `Y` \| `N` | no | `Y` |
| `Crc.<Class>.Line.<prop>` | see §6 | required when `IncludeCrcEramPropertyDefaults=Y` | — |
| `Crc.<Class>.Symbol.<prop>` | see §6 | " | — |
| `Crc.<Class>.Text.<prop>` | see §6 | " | — |

`<Class>` is `High`, `Low`, or `Other`. Example key: `Crc.High.Line.thickness`.

> **Phase 3.3–3.7 (remediation plan):**
> - `ExcludedDesignations` — airways whose derived designation is listed are dropped **before**
>   geometry work, so GeoJSON and the alias file agree.
> - `EmitLines`/`EmitSymbols`/`EmitText` — all three `N` throws unless `OutputBy=None`.
> - `AliasRoiScope=RoiAirways` — an airway is kept if **any** waypoint is inside the ROI, then
>   **all** its waypoints are written (point-in-ROI test, never the clipped geometry).
> - `CoordinatePrecision` — rounds every coordinate in the GeoJSON at write time.
> - `AddFeBuddyOutputFolder=N` — output goes straight into `<OutputDirectory>\Airways\…`
>   (the `FE-Buddy_Output` wrapper is dropped; the `Airways` sub-folder stays).

Parsing rules:
- Every `Y`/`N` parse is case-insensitive and trimmed. Anything else throws `ArgumentException` naming the setting.
- An unknown dictionary key is ignored, but recorded as a warning on the result.
- **Breaking change from existing code:** the current parser accepts `OutputBy` values `HighLow` and `Type`, and the key `WaypointBuffer`. Rename to `Designation` and `BufferAirwayWaypoints`. Update `FEBuddyTest` accordingly.

### 4.3 `RegionOfInterest`

```csharp
public sealed record RegionOfInterest(double SwLat, double SwLon, double NeLat, double NeLon)
{
    public Envelope ToEnvelope();   // NTS Envelope(minX=SwLon, maxX=NeLon, minY=SwLat, maxY=NeLat)
}
```

Validation (used now by the parser, and later by the GUI's save button):
- `IsCoordinateValidFormat` — all four parse as `double`; lat in `[-90, 90]`, lon in `[-180, 180]`.
- `IsCoordinatesRelativePositionValid` — `NeLat > SwLat` **and** `NeLon > SwLon`.
- An ROI that crosses the antimeridian (`SwLon > NeLon`) is **rejected** in this phase with a clear error message. `[LATER]` support if a user needs it.

Expose both checks as `public static` methods on `FEBuddyLibrary/Services/General/RoiFilter.cs` so the GUI can call them directly.

---

## 5. Airways Domain Model

### 5.1 `Airway`

```csharp
public sealed class Airway
{
    public required string AwyId { get; init; }              // AWY_BASE.AWY_ID, e.g. "J3"
    public required string AwyDesignation { get; init; }     // AWY_BASE.AWY_DESIGNATION, e.g. "J"
    public required string AwyLocation { get; init; }        // A | H | C
    public int? MaxAuthAlt { get; init; }                    // highest MAX_AUTH_ALT across segments
    public required AirwayAltitudeClass AltitudeClass { get; init; }
    public required IReadOnlyList<AirwaySegment> Segments { get; init; }
    public required IReadOnlyList<AirwayPoint> Points { get; init; }   // ordered, de-duplicated
    public required Geometry Geometry { get; init; }         // LineString or MultiLineString
    public IReadOnlyList<string> Warnings { get; init; }
}
```

### 5.2 `AirwayPoint`

```csharp
public sealed record AirwayPoint(
    string PointId,          // FROM_POINT / TO_POINT
    string? PointType,       // FROM_PT_TYPE (null for reference-only points, which are excluded)
    double Latitude,
    double Longitude,
    string FoundIn);         // "fix" | "navaid" | "airport"
```

### 5.3 `AirwaySegment`

Keep the existing shape, promoted to a public record and extended:

```csharp
public sealed record AirwaySegment(
    string StartWptId,
    string EndWptId,
    bool IsGap,
    int? MaxAuthAlt);
```

### 5.4 Altitude classification (`AirwayClassifier`)

**Decision (see §10.2):** classify by the **highest** `MAX_AUTH_ALT` found across all of the airway's `AWY_SEG_ALT` records.

```
maxAlt = max(MaxAuthAlt) over all segments where MaxAuthAlt.HasValue

maxAlt >= 18000               -> AirwayAltitudeClass.High
maxAlt >  0 && maxAlt < 18000 -> AirwayAltitudeClass.Low
no segment has a value, or maxAlt <= 0 -> AirwayAltitudeClass.Other
```

One airway lands in exactly one class. Do **not** split an airway across the High and Low files.

### 5.5 Designation grouping

When `OutputBy = Designation`, the group key is `AWY_BASE.AWY_DESIGNATION` trimmed and upper-cased (`J`, `V`, `AT`, `RN`, `PA`, …). An airway whose designation is blank groups under `Unknown`. The airway's `AltitudeClass` is still computed, because CRC default properties are keyed by altitude class in every output mode (§6.4).

### 5.6 Warnings, not crashes

The existing generator **throws** `InvalidOperationException` when a waypoint mid-airway cannot be resolved. For a service that runs unattended over a full NASR cycle this is too brittle.

**Change:** collect the problem as a warning on the airway, skip that airway's unresolvable portion, and continue. `AirwayServiceResult.Warnings` carries every message. `FEBuddyTest` prints them; the GUI will surface them later.

Keep the *existing* behavior for the trailing-unresolved case (stop the airway at the last resolvable point — this correctly handles `CFQLS -> CFGFX -> U.S. CANADIAN BORDER-4`).

---

## 6. CRC ERAM GeoJSON Properties

Source of truth: <https://github.com/KCSanders7070/CRC_GeoJson_Concepts/blob/main/CRC_Geojsons.md>

### 6.1 The three-tier model

1. **Default Override** (highest) — properties written on an individual Feature.
2. **isDefaults** (middle) — a non-rendered `Point` Feature at coordinates `[90.0, 180.0]` carrying `"isLineDefaults": true` / `"isSymbolDefaults": true` / `"isTextDefaults": true`. Applies to every Feature in that file whose `properties` is `{}`.
3. **CRC auto-assign** (lowest) — CRC's own fallbacks.

`filters` and `text` are **never** auto-assigned. A file without them will not draw on an ERAM window. Treat both as required.

The isDefaults Feature is written **first** in the `FeatureCollection`.

### 6.2 Valid values — enforce these in `CrcGeojsonPropertyValidator`

**All feature kinds**

| Property | Type | Valid range | Auto-assigned if absent |
|---|---|---|---|
| `bcg` | int | 1–40 | 1 |
| `filters` | int[] | each 0–40, at least one entry | **none — required** |

**Line features** (`LineString` / `MultiLineString`)

| Property | Type | Valid values | Auto |
|---|---|---|---|
| `style` | string | `solid`, `shortDashed`, `longDashed`, `longDashShortDash` | `solid` |
| `thickness` | int | 1–3 | 1 |

**Symbol features** (`Point`)

| Property | Type | Valid values | Auto |
|---|---|---|---|
| `style` | string | `obstruction1`, `obstruction2`, `heliport`, `nuclear`, `emergencyAirport`, `radar`, `iaf`, `rnavOnlyWaypoint`, `rnav`, `airwayIntersections`, `ndb`, `vor`, `otherWaypoints`, `airport`, `satelliteAirport`, `tacan` | `vor` |
| `size` | int | 1–4 | 1 |

**Text features** (`Point`)

| Property | Type | Valid values | Auto |
|---|---|---|---|
| `text` | string[] | ≥1 entry; each entry is one rendered line | **none — required** |
| `size` | int | 0–5 | 1 |
| `underline` | bool | `true` / `false` | `false` |
| `xOffset` | int | ≥ 0 | 0 |
| `yOffset` | int | ≥ 0 | 0 |
| `opaque` | bool | `true` / `false` | `false` |

Style value comparison is **case-sensitive on output** (emit exactly as spelled above) but **case-insensitive on user input** (normalize what the user typed).

### 6.3 `CrcEramPropertyHandler`

```csharp
public static class CrcEramPropertyHandler
{
    /// Builds the non-rendered isDefaults Point Feature for a file.
    public static Feature CreateDefault(CrcFeatureKind kind, object properties);

    /// Builds an AttributesTable for a single rendered feature (an "Overriding Property" set).
    public static AttributesTable CreateFeatureProperty(CrcFeatureKind kind, object properties);
}
```

- `CreateDefault` always emits the geometry `Point(90.0, 180.0)` — note this is `x=90, y=180`, which is what the CRC doc and ERAM_2_GEOJSON both use; it is intentionally an out-of-range point so viewers ignore it.
- `CreateDefault` for `Text` always includes `opaque` (default `false`), matching ERAM_2_GEOJSON behavior.
- Both methods call the validator first and throw `ArgumentException` listing every invalid property, not just the first.

### 6.4 Which defaults apply to which file

Property defaults are keyed by `AirwayAltitudeClass` (`High` / `Low` / `Other`) in **all** output modes.

- `OutputBy = HighLow`: file ↔ class is 1:1. Straightforward.
- `OutputBy = Designation`: a designation file may contain airways of mixed classes. In that case the file's isDefaults come from the class of the **majority** of airways in that file, and any airway whose class differs gets its properties written as per-feature Default Overrides. (§10.4)

---

## 7. Service Specifications

### 7.1 `AirwayService` — the public entry point

```csharp
public static class AirwayService
{
    public static AirwayServiceResult Run(
        NasrCsvDataCollection allNasrCsvData,
        Dictionary<string, string> airwaySettings);
}
```

Steps:
1. `AirwaySettingsParser.Parse` → `AirwaySettings`.
2. Guard: `allNasrCsvData.Awy` is not null, else `InvalidOperationException`.
3. `AirwayBuilder.BuildAll(...)` → `IReadOnlyList<Airway>`.
4. If `Roi != null`, `RoiFilter.Apply(...)`.
5. If `OutputBy != None`, `AirwayGeojsonService.Generate(...)`.
6. If `GenerateAliasFile`, `AirwayAliasService.Generate(...)`.
7. Return `AirwayServiceResult { FilesWritten, AirwayCount, FeatureCounts, Warnings, Elapsed }`.

### 7.2 `AirwayBuilder`

Combines the logic currently spread across `AwyGeojsonGenerator.cs`, `.Normalization.cs`, `.Geometry.cs`, `.Helpers.cs`.

1. Build a dictionary of airways from `Awy.AwyBase`, keyed on trimmed `AwyId` (`GroupBy` first — duplicate `AWY_BASE` rows exist and will otherwise throw).
2. `ILookup` the `Awy.AwySegAlt` rows by trimmed `AwyId`; order each group by `PointSeq`.
3. `AirwayNormalizer.Normalize(rawSegments)` — **port the existing logic verbatim.** It removes reference-only points (rows whose `FROM_PT_TYPE` is null/empty, such as `U.S. MEXICAN BORDER-2`) by collapsing `TIJ -> BORDER-2` and `BORDER-2 -> TEYON` into `TIJ -> TEYON`, preserving `AWY_SEG_GAP_FLAG = Y` from any collapsed row. Also carry `MAX_AUTH_ALT` through onto the normalized segment.
4. Resolve every endpoint via `FindWaypointCoordinates`; build the ordered, de-duplicated `Points` list (each waypoint appears once even though it is both a `ToPoint` and the next `FromPoint`).
5. `AirwayGeometryBuilder.Build(...)` — **port the existing logic verbatim.** Start a new `LineString` on a gap flag or a discontinuity; otherwise append only the segment end coordinate. One `LineString` → `LineString`; more than one → `MultiLineString`.
6. If `SplitAtAntimeridian`, run each `LineString` through `AntimeridianHandler`.
7. If `BufferAirwayWaypoints`, run `AirwayWaypointBuffer` (§7.5).
8. `AirwayClassifier.Classify(...)` → `AltitudeClass`.

### 7.3 `AirwayGeojsonService`

Produces **three files per group**, matching old FE-Buddy's structure:

| `OutputBy` | Files |
|---|---|
| `HighLow` | `Airways_High_Lines.geojson`, `Airways_High_Symbols.geojson`, `Airways_High_Text.geojson`, and the same trio for `Low` and `Other` |
| `Designation` | `Airways_<DESIG>_Lines.geojson`, `_Symbols.geojson`, `_Text.geojson` per designation (`Airways_J_Lines.geojson`, `Airways_V_Lines.geojson`, `Airways_AT_Lines.geojson`, …) |
| `None` | no GeoJSON files |

**A file with zero rendered features is not written.** (Old FE-Buddy's `SerializeToFile` did this; keep it.)

Output directory: `<OutputDirectory>/FE-Buddy_Output/Airways/Geojson/` (§10.5).

**Lines file** — one Feature per airway:
- geometry: the airway's `LineString` / `MultiLineString`
- properties: `{}` when `IncludeCrcEramPropertyDefaults` is on and no per-feature override is needed; otherwise the override set.
- FEB custom properties, when `IncludeFebCustomProperties`:
  - `"feb.AwyId"` : `"J3"`
  - `"feb.AwyWaypoints"` : `["OAK","RBL","LKV","IMB","GEG"]` — only when `IncludeAirwayWaypointIds` is also on. This is the unique ordered list of every `FromPoint`/`ToPoint` on the airway.

**Symbols file** — one `Point` Feature per unique airway waypoint. `style` maps from `FROM_PT_TYPE`:

| `FROM_PT_TYPE` | `style` |
|---|---|
| `NDB`, `NDB/DME`, `MARINE NDB`, `MARINE NDB/DME`, `UHF/NDB` | `ndb` |
| `VOR`, `VOR/DME`, `VORTAC`, `DME`, `TACAN`, `VOT`, `CONSOLAN` | `vor` |
| everything else (`WP`, `CN`, `RP`, `MR`, `MW`, `NRS`, `RADAR`, `VFR`, `FAN MARKER`, null) | `airwayIntersections` |

**Text file** — one `Point` Feature per unique airway waypoint, `"text": ["<PointId>"]`, at the same coordinates as the symbol.

De-duplicate symbols and text **across airways within a file**: a fix shared by J3 and J7 gets one symbol Feature and one text Feature in that file, not two.

### 7.4 `AirwayAliasService`

- Output: `<OutputDirectory>/FE-Buddy_Output/Airways/Alias/Draw_Airway_Points.txt`
- One line per airway: `.{AwyId}F .FF {pt1} {pt2} {pt3} …`
- Example: `.J3F .FF OAK RBL LKV IMB GEG`
- Points are the airway's ordered de-duplicated `Points` list.
- Written even when `OutputBy = None` (the alias file is an independent user choice).
- Lines sorted by `AwyId` using `StringComparer.OrdinalIgnoreCase`, so the file diffs cleanly cycle to cycle.

### 7.5 `AirwayWaypointBuffer`

Stops each airway leg short of its endpoints so waypoint symbols are legible.

- 5-character point IDs (fixes): **2.5 NM** radius.
- All other point IDs (NAVAIDs, airports): **5 NM** radius.
- For each consecutive coordinate pair, move the start point `startRadius` NM along the bearing toward the end point, and move the end point `endRadius` NM along the bearing toward the start point. Emit that shortened pair as its own `LineString`.
- **Consequence:** a buffered airway is always a `MultiLineString` of disjoint two-point legs. That is intended and matches old FE-Buddy's `(DME Cutoff)` output.
- If a leg is shorter than `startRadius + endRadius`, drop the leg and record a warning.
- Buffering runs **after** antimeridian splitting and **after** ROI clipping.

### 7.6 `RoiFilter`

**Decision (§10.3): clip.**

- Build an NTS `Envelope` from the ROI and a rectangular `Polygon` from it.
- `clipped = airway.Geometry.Intersection(roiPolygon)`.
- Drop the airway entirely if `clipped.IsEmpty`.
- Normalize the result: an intersection can return `LineString`, `MultiLineString`, `Point`, or `GeometryCollection`. Keep only line components; if exactly one remains, emit `LineString`, otherwise `MultiLineString`. If none remain, drop the airway.
- Symbols and Text are emitted only for waypoints whose coordinates fall **inside** the ROI envelope.
- The alias file is filtered to airways that survive the clip, but each surviving airway keeps its **full** point list (a controller typing `.J3F` wants the whole airway drawn).
- Clipping runs **after** antimeridian splitting and **before** waypoint buffering.

### 7.7 `AntimeridianHandler`

Wrap the existing `CoordinateHandler.CrossesAntimeridian` and `CoordinateHandler.SplitLineSegmentAtAntimeridian`. For each consecutive coordinate pair in a `LineString`, if it crosses, end the current line at ±180 and start a new one at ∓180. Returns `IReadOnlyList<LineString>`.

### 7.8 `GeojsonFileWriter`

```csharp
public static class GeojsonFileWriter
{
    public static string? Write(FeatureCollection collection, string directory, string fileName);
}
```

- `WriteIndented = DevMode.IsEnabled`.
- Registers `new GeoJsonConverterFactory()`.
- Creates the directory if missing.
- Returns the full path written.
- Returns `null` and writes nothing if the collection holds no rendered features.

---

## 8. `FEBuddyTest` — the GUI stand-in

One `.cs` per service; `Program.cs` only orchestrates.

**`HarnessSettings.cs`** — every path and toggle in one place, at the top of the file, clearly marked as the thing a developer edits:

```csharp
internal static class HarnessSettings
{
    public const string NasrSourceDirectory = @"D:\NASR\03_Sep_2026_CSV";
    public const string OutputDirectory     = @"D:\FE-Buddy-Output";
    public const bool   DevMode             = true;

    public static Dictionary<string, string> AirwaySettings() => new()
    {
        { "OutputDirectory",                OutputDirectory },
        { "OutputBy",                       "HighLow" },
        { "BufferAirwayWaypoints",          "N" },
        { "IncludeFebCustomProperties",     "Y" },
        { "IncludeAirwayWaypointIds",       "Y" },
        { "GenerateAliasFile",              "Y" },
        { "SplitAtAntimeridian",            "Y" },
        { "IncludeCrcEramPropertyDefaults", "Y" },
        { "FilterByRoi",                    "N" },
        { "Crc.High.Line.bcg",              "3" },
        { "Crc.High.Line.filters",          "3" },
        { "Crc.High.Line.style",            "solid" },
        { "Crc.High.Line.thickness",        "1" },
        // ... Low / Other, and the Symbol / Text sets
    };
}
```

**`AirwayGeojsonRunner.cs`** — `internal static AirwayServiceResult Run(NasrCsvDataCollection data)`: calls `AirwayService.Run`, times it, returns the result.
**`AirwayAliasRunner.cs`** — same shape for the alias-only path, used to exercise `OutputBy = None`.
**`ConsoleReport.cs`** — all `Console.WriteLine` formatting: elapsed times, per-file feature counts, warnings grouped by airway.
**`Program.cs`** — set `DevMode.IsEnabled`, parse NASR (timed), call each runner, print the report, exit non-zero on unhandled exception.

`FEBuddyTest` must not contain any airway logic. If you find yourself computing geometry in the harness, it belongs in the library.

---

## 9. Build Phases

Each phase must compile and leave the solution green before the next begins.

### Phase 0 — Prep
- Add the "aspirational, post-GUI" note to the top of `Project-Structure.md`.
- Add `Configuration/DevMode.cs`.
- Fix the `FEBuddyLibrary.csproj` `EditorConfigFiles` items — they contain a stale absolute path (`C:\Users\Nikolas\GitHubRepos\...`) that does not exist on this machine. Replace with a relative `.editorconfig` reference or remove both `ItemGroup`s.
- Confirm the solution builds.

**Acceptance:** `dotnet build FeBuddy.sln` succeeds with no new warnings.

### Phase 1 — Shared foundation
1. `Models/Geojson/*` — the three CRC property records, `CrcFeatureKind`, `CrcPropertyValidationResult`.
2. `Services/General/CrcGeojsonPropertyValidator.cs` — every rule in §6.2, returning **all** failures.
3. `Services/General/CrcEramPropertyHandler.cs` — `CreateDefault`, `CreateFeatureProperty`.
4. `Handlers/CSV/FindWaypointCoordinates.cs` — **performance fix.** The current implementation does a `FirstOrDefault` linear scan of `FixBase`/`NavBase`/`AptBase` for every lookup, and the airway generator calls it several times per segment plus again inside `HasResolvableSegmentAhead`. Add a lazily-built, cached `Dictionary<string, (double lat, double lon, string foundIn)>` per source keyed `OrdinalIgnoreCase`, built once per `NasrCsvDataCollection` instance. Public behavior must not change.
5. `Handlers/CoordinateHandler.cs` — add `public static Location PointAtDistanceAndBearing(Location origin, double bearingDegrees, double distanceNm)` (great-circle destination point). Port from old FE-Buddy's `LatLonHelpers.GetNewPoint`.
6. `Services/General/GeojsonFileWriter.cs`, `AntimeridianHandler.cs`, `RoiFilter.cs`.
7. `Models/Services/General/RegionOfInterest.cs`, `ServiceResult.cs`.

**Acceptance:** unit tests cover every validator boundary (bcg 0/1/40/41, thickness 0/1/3/4, text size -1/0/5/6, each style string, empty `filters`), plus `PointAtDistanceAndBearing` against a known bearing/distance pair.

### Phase 2 — Airway domain
1. Create `Models/Services/Airways/*`.
2. Create `Services/Airways/AirwaySettingsParser.cs` from `AwyGeojsonGenerator.Settings.cs`, extended per §4.2.
3. Create `AirwayNormalizer.cs` from `AwyGeojsonGenerator.Normalization.cs` (logic unchanged, plus `MaxAuthAlt` carried through).
4. Create `AirwayGeometryBuilder.cs` from `AwyGeojsonGenerator.Geometry.cs`, converting the two mid-airway `throw`s into warnings per §5.6.
5. Create `AirwayClassifier.cs` per §5.4.
6. Create `AirwayBuilder.cs` per §7.2.
7. **Delete** `Generators/NASR/AWY-Geojsons/` once everything is ported and the harness produces the same line geometry it did before.

**Acceptance:** `AirwayBuilder.BuildAll` over a real NASR cycle returns a plausible airway count with zero unhandled exceptions; the `TIJ / U.S. MEXICAN BORDER-2 / TEYON` and `CFQLS / CFGFX / U.S. CANADIAN BORDER-4` cases behave as the existing code does.

### Phase 3 — Airway GeoJSON service
`AirwayWaypointBuffer.cs`, then `AirwayGeojsonService.cs`, then `AirwayService.cs`.

**Acceptance:** all three output modes produce the file sets in §7.3; every file opens in a GeoJSON viewer; `_Lines` files carry exactly one Feature per airway; isDefaults is Feature index 0; single-line output when `DevMode` is false.

### Phase 4 — Alias service
`AirwayAliasService.cs`, wired into `AirwayService.Run`.

**Acceptance:** `Draw_Airway_Points.txt` contains one sorted line per airway in the `.J3F .FF OAK RBL LKV IMB GEG` format.

### Phase 5 — Harness and tests
1. Rebuild `FEBuddyTest` per §8.
2. Add `UnitTests/Services/Airways/` and implement the Category A baseline tests from `FEBuddyTest/AWY_GEOJSON_RegressionTests.md`, updated for the new type names.
3. Add Category B contract tests for grouping, antimeridian, ROI clipping, and waypoint buffering — these are now real features, so they should pass.
4. Use synthetic fixtures. Do **not** commit or depend on full FAA CSV files in unit tests.

**Acceptance:** `dotnet test` green; harness runs end to end against a real cycle and prints timings, counts, and warnings.

### Phase 6 — `[LATER]`, after the GUI exists
Not part of this build: `UserConfig.json` read/write, launch processes (UTC time API + internet check, version check, AIRAC download manager, News), Departure Procedures service, RVM/GeoMap conversions, uninstall/update settings, and the Clean Architecture migration.

---

## 10. Decisions Log

The dev notes are an idea bin and contain contradictions. These are the resolutions.

**10.1 — Project layout.** The notes describe a `SERVICES` folder; `Project-Structure.md` describes Clean Architecture. **Resolution:** build the `SERVICES` layout now (§3); Clean Architecture is a post-GUI migration target and `Project-Structure.md` is marked aspirational.

**10.2 — High/Low classification.** Notes say classify by Maximum Authorized Altitude; old FE-Buddy checked whether the airway ID contained `Q` or `J`. **Resolution:** highest `MAX_AUTH_ALT` across the airway's segments, `>= 18000` High, `> 0 && < 18000` Low, otherwise Other. One airway, one file.

**10.3 — ROI behavior.** Notes describe both clipping and whole-entity inclusion. **Resolution:** clip line geometry to the ROI rectangle; emit symbols/text only for waypoints inside the ROI; the alias file keeps full point lists for surviving airways.

**10.4 — CRC defaults under `Designation` grouping.** The notes define CRC property defaults only as `Airway_High_*` / `Airway_Low_*` / `Airway_Other_*`, which does not map onto per-designation files. **Resolution:** §6.4 — majority class supplies the file's isDefaults; off-class airways get per-feature overrides.

**10.5 — Output paths.** The notes never state one. **Resolution:** `<OutputDirectory>/FE-Buddy_Output/Airways/Geojson/` and `<OutputDirectory>/FE-Buddy_Output/Airways/Alias/`.

**10.6 — File naming.** The notes give both `Airways_High.geojson` (Code Library section) and `Airways_High_Lines.geojson` (GUI section), and `Airways_Other.geojson` without a suffix. **Resolution:** always the three-file `_Lines` / `_Symbols` / `_Text` suffix pattern, matching old FE-Buddy.

**10.7 — Custom property name.** The existing generator emits `"feb_AWY-ID"`; the notes specify `feb.AwyId`. **Resolution:** `"feb.AwyId"`. This changes existing output — update any downstream expectations.

**10.8 — `UserConfig` News path.** The `NewsChecker` sample reads `"General.Settings.News.NewsLastOpen"` but the documented config tree is `General.NewsLastOpen`. **Resolution:** `General.NewsLastOpen`. Relevant only in Phase 6.

**10.9 — Setting name changes.** `OutputBy` value `Type` → `Designation`; `WaypointBuffer` → `BufferAirwayWaypoints`. The old names were in the working generator; the new ones match the notes and the eventual GUI labels.

**10.10 — Mid-airway unresolved waypoints.** Existing code throws. **Resolution:** warn and continue (§5.6). A single bad NASR row must not abort a full-cycle run.

**10.11 — `xOffset` / `yOffset` sign.** The CRC doc says "positive integers", which would forbid offsetting text left or below its point. **Resolution:** validate as `>= 0` per the doc, and record this as an open question to raise with the CRC doc author before the GUI exposes these fields.

---

## 11. Open Questions

Answer before or during the phase noted.

1. **(Phase 3)** When `BufferAirwayWaypoints` is on, should FE-Buddy write the buffered lines *instead of* the full lines, or write both (old FE-Buddy wrote a separate `(DME Cutoff)` file)? This spec assumes **instead of**.
2. **(Phase 3)** Should `Airways_Other` be written at all when it contains only airways with no MAA data, or suppressed as noise?
3. **(Phase 6)** `AiracCycleIdEffectiveDates` is a hard-coded lookup table. How far forward is it populated, and what should happen when a user runs FE-Buddy past the last entry?
4. **(Phase 3)** Do `AwyLocation` values `A` (Alaska) and `H` (Hawaii) need separate output files, or is the ROI the intended mechanism for excluding them?
