# Settings blocks

A **settings block** is the `Dictionary<string, string>` one sub-service (or one file
conversion) receives for one run. The GUI builds it (`BuildSettingsBlock` on each tab), the
harness writes it by hand (`HarnessSettings.cs`), and the tests build it inline - and all of them
go through the same parser. This page lists every key each parser reads.

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

Parsers: `AirportSettingsParser`, `AirwaySettingsParser`, `DepartureSettingsParser`,
`DatToGeojsonSettingsParser`. Shared reading: `SubServiceSettingsReader`, `CrcDefaultsReader`,
`SettingsValueReader` (all in `FeBuddy.Core/Application`).

## Keys every AIRAC sub-service reads

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

## Keys every file conversion reads

A file conversion is not an AIRAC sub-service: it has no alias file, `feb.*` properties or ROI.
Every conversion reads these (shared reading: `ConversionSettingsReader`), plus
`CoordinatePrecision` and the `IncludeCrc…Defaults` / `Crc.*` keys as in the table above.

| Key | Values | Default |
|---|---|---|
| `OutputDirectory` | folder path | **required** |
| `AddFeBuddyOutputFolder` | `Y` / `N` | `Y` |
| `SourceFolder` | a folder; every file directly in it with the conversion's extension is converted | - |
| `SourceFiles` | file paths separated by `\|` (a comma is legal in a Windows path; `\|` is not) | - |

- Exactly one of `SourceFolder` and `SourceFiles` is required. A `SourceFolder` that does not
  exist throws; a file in `SourceFiles` that cannot be read fails that file only.

## DAT to GeoJSON (File Conversions)

Extension `.dat`.

| Key | Values | Default |
|---|---|---|
| `CroppingDistance` | NM from each map's point of tangency, greater than 0 and at most 1000; blank keeps every line | none |

- **CRC class:** `VideoMap`, with `Line` only (`Crc.VideoMap.Line.*`).

## SCT2 to GeoJSON (File Conversions)

Extensions `.sct2` and `.sct`. No keys of its own.

- **CRC class:** `SectorFile`, with `Line` (every lines file) and `Text` (the labels file):
  `Crc.SectorFile.Line.*`, `Crc.SectorFile.Text.*`. Regions have no CRC defaults.

## ERAM to GeoJSON (File Conversions)

Extension `.xml`: the `Geomaps.xml` (`Geomaps_Records`) of an ERAM adaptation export. A
`SourceFolder` may hold the whole export; only its Geomaps files are converted and the other XML
files are named in one message. A picked file that is not a Geomaps file fails that file only.

| Key | Values | Default |
|---|---|---|
| `OutputLayout` | `ByObject` (a file per object, named `<MapObjectType>_<MapGroupId>`), `ByFilter` (files by filter index and similar attributes) | `ByObject` |
| `DefaultsSource` | `Xml` (carry over the XML's defaults and element overrides), `XmlThenCard` (the tab's defaults where an object has none), `Card` (the tab's defaults only; the XML's styling is ignored) | `Xml` |

- **CRC class:** `GeoMap`, with `Line`, `Symbol` and `Text`: `Crc.GeoMap.Line.*`,
  `Crc.GeoMap.Symbol.*`, `Crc.GeoMap.Text.*`.
- The tab's CRC defaults are read only when `DefaultsSource` is `XmlThenCard` or `Card`, and then
  only for kinds whose `IncludeCrc…Defaults` is `Y`. With `Xml` they are ignored.
- ERAM text has no opaque background, so Text defaults taken from the XML always have `opaque`
  off. ERAM's `Color` and `DisplaySetting` have no CRC equivalent and are not carried over.

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
