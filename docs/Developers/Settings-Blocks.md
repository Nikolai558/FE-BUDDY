# Settings blocks

A **settings block** is the `Dictionary<string, string>` one sub-service receives for one run.
The GUI builds it (`BuildSettingsBlock` on each tab), the harness writes it by hand
(`HarnessSettings.cs`), and the tests build it inline - and all of them go through the same
parser. This page lists every key each parser reads.

## How a block is read

- **Keys are case-insensitive.** Values are strings.
- **Yes/no values are `Y` or `N`** (any case). Anything else is an error.
- **Lists are comma-separated**; blanks are ignored (`"ZOB, ZNY,"` is `ZOB`, `ZNY`).
- **A missing optional key uses its default.** A missing **required** key, or an invalid value,
  throws `ArgumentException` naming the key - the run stops for that sub-service.
- **Unknown keys are warnings**, not errors: the run goes on and the Review tab lists them. This is
  how a retired key in an old harness or config degrades gracefully.
- **Only the CRC defaults actually needed are required**: those for a file kind that is being
  written *and* whose `IncludeCrc…Defaults` is `Y`. Other valid CRC keys are accepted and ignored;
  a key for a class, kind or field the sub-service does not have is a warning.

Parsers: `AirportSettingsParser`, `AirwaySettingsParser`, `DepartureSettingsParser`. Shared
reading: `SubServiceSettingsReader`, `CrcDefaultsReader`, `SettingsValueReader`
(all in `FeBuddy.Core/Application`).

## Keys every sub-service reads

| Key | Values | Default |
|---|---|---|
| `OutputDirectory` | folder path | **required** |
| `AddFeBuddyOutputFolder` | `Y` / `N` - write into `<OutputDirectory>\FE-Buddy_Output\` | `Y` |
| `CoordinatePrecision` | `0`-`15` decimal places | `6` |
| `GenerateAliasFile` | `Y` / `N` | `Y` |
| `IncludeFebCustomProperties` | `Y` / `N` | `N` |
| `FebProperties` | list of `feb.*` names (below); **required** when the above is `Y` | none |
| `IncludeCrcLineDefaults`, `IncludeCrcSymbolDefaults`, `IncludeCrcTextDefaults` | `Y` / `N` | `Y` |
| `FilterByRoi` | `Y` / `N` | `N` |
| `RoiSwLat`, `RoiSwLon`, `RoiNeLat`, `RoiNeLon` | decimal degrees; **required** when `FilterByRoi` is `Y` | none |

### CRC defaults

Keys are `Crc.<Class>.<Kind>.<field>`, e.g. `Crc.High.Line.bcg`, `Crc.Airports.Symbol.style`.

| Kind | Fields |
|---|---|
| `Line` | `bcg` (1-40), `filters` (list of 0-40), `style` (`solid`, `shortDashed`, `longDashed`, `longDashShortDash`), `thickness` (1-3) |
| `Symbol` | `bcg`, `filters`, `style` (a CRC symbol: `vor`, `ndb`, `airport`, `rnav`, `airwayIntersections`, … - see `CrcPropertyValidator.ValidSymbolStyles`), `size` (1-4) |
| `Text` | `bcg`, `filters`, `size` (0-5), `underline` (`Y`/`N`), `opaque` (`Y`/`N`), `xOffset`, `yOffset` (whole numbers) |

Values are validated against what CRC can draw; an out-of-range value throws with a message
naming the key.

## Airports

| Key | Values | Default |
|---|---|---|
| `GenerateGeojson` | `Y` / `N` | `Y` |
| `EmitRunwayLines`, `EmitAirportSymbols`, `EmitAirportText` | `Y` / `N` | `Y` |

- **CRC classes:** `Runways` (`Line`), `Airports` (`Symbol`, `Text`).
- **`FebProperties`:** `faaId`, `icaoId`, `name`, `elev`, `respArtcc`, `tfcPtrnAlt`, `fssId`,
  `twrType`, `rwyId`.
- `GenerateGeojson` and `GenerateAliasFile` cannot both be `N`, and `GenerateGeojson = Y` needs at
  least one `Emit…`.

## Airways

| Key | Values | Default |
|---|---|---|
| `OutputBy` | `HighLow`, `Designation`, `None` | **required** |
| `EmitLines`, `EmitSymbols`, `EmitText` | `Y` / `N` | `Y` |
| `BufferAirwayWaypoints` | `Y` / `N` | `N` |
| `SplitAtAntimeridian` | `Y` / `N` | `Y` |
| `ExcludedDesignations` | list, e.g. `RN,SL` (upper-cased) | none |
| `AliasRoiScope` | `All`, `RoiAirways` | `All` |

- **CRC classes:** `High`, `Low`, `Other`, each with `Line`, `Symbol` and `Text`.
- **`FebProperties`:** `awyId`, `pointId`, `waypoints`.
- `OutputBy = None` writes no GeoJSON (so no CRC defaults are needed); any other value needs at
  least one `Emit…`.

## Departures

| Key | Values | Default |
|---|---|---|
| `GenerateGeojson` | `Y` / `N` | `Y` |
| `EmitLines`, `EmitSymbols`, `EmitText` | `Y` / `N` | `Y` |
| `IncludeObstacleDepartures` | `Y` / `N` | `Y` |
| `ArtccFilter` | list of ARTCC IDs; empty means every ARTCC | none |
| `AmendmentFilter` | `None`, `Cycles`, `Days`, `Date` | `None` |
| `AmendedWithinCycles` | 1-1000 (1 = the selected cycle); **required** with `Cycles` | - |
| `AmendedWithinDays` | 1-36500, counted back from today; **required** with `Days` | - |
| `AmendedOnOrAfter` | `yyyy-MM-dd`; **required** with `Date` | - |
| `RoiMode` | `Airport` (every departure of an airport inside the ROI), `Waypoint` (any departure with a point inside) | `Airport` |

- **CRC class:** `Departures`, with `Line`, `Symbol` and `Text`.
- **`FebProperties`:** `dpName`, `pointId`, `arptId`, `artcc`, `amendmentNo`, `amendEffDate`,
  `waypoints`.
- `GenerateGeojson` and `GenerateAliasFile` cannot both be `N`, and `GenerateGeojson = Y` needs at
  least one `Emit…`.
- Only the active amendment mode's value is read (and required); values for the other modes are
  ignored.

## An example (Airways, as the harness writes it)

```csharp
new Dictionary<string, string>
{
	{ "OutputDirectory", @"C:\FE-Buddy-Output" },
	{ "OutputBy", "HighLow" },
	{ "GenerateAliasFile", "Y" },
	{ "IncludeFebCustomProperties", "Y" },
	{ "FebProperties", "awyId,pointId,waypoints" },
	{ "IncludeCrcLineDefaults", "Y" },
	{ "Crc.High.Line.bcg", "1" },
	{ "Crc.High.Line.filters", "1,2" },
	{ "Crc.High.Line.style", "solid" },
	{ "Crc.High.Line.thickness", "1" },
	// ...the same for Low and Other, and for Symbol and Text if included
	{ "FilterByRoi", "N" },
};
```

## Adding a key

1. Read it in the sub-service's parser with `SettingsValueReader` (and add it to the parser's
   `OwnKeys`, or `SubServiceSettingsReader.CommonKeys` if every sub-service reads it - otherwise it
   is reported as unknown).
2. Send it from the tab's `BuildSettingsBlock` (or `GeojsonSubServiceViewModel.AddSharedSettings`).
3. Add it to the harness settings and a parser test.
4. List it here, and in [UserConfig.json reference](UserConfig-Reference.md) if the GUI saves it.
