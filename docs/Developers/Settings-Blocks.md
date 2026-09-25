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
- **Only the CRC defaults actually needed are required**: those a file that is being written
  *and* is listed in `CrcDefaultsFor` needs (a file conversion: a kind it writes whose
  `IncludeCrc…Defaults` is `Y`). Other valid CRC keys are accepted and ignored; a key for a class,
  kind or field the sub-service does not have is a warning.

Parsers: `AirportSettingsParser`, `AirwaySettingsParser`, `DepartureSettingsParser`,
`ArrivalSettingsParser`, `DatToGeojsonSettingsParser`, `SctToGeojsonSettingsParser`,
`VeramToGeojsonSettingsParser`. Shared reading: `SubServiceSettingsReader`, `CrcDefaultsReader`,
`ConversionSettingsReader`, `SettingsValueReader` (all in `FeBuddy.Core/Application`).

## Keys every AIRAC sub-service reads

| Key | Values | Default |
|---|---|---|
| `OutputDirectory` | folder path - the folder the run writes into (below) | **required** |
| `CoordinatePrecision` | `0`-`15` decimal places | `6` |
| `GenerateAliasFile` | `Y` / `N` | `Y` |
| `IncludeFebCustomProperties` | `Y` / `N` | `N` |
| `FebProperties` | list of `feb.*` names (below); **required** when the above is `Y` | none |
| `UploadToVnas` | list of file keys (below) to write under `Upload_to_vNAS` | none |
| `CrcDefaultsFor` | list of GeoJSON file keys that get CRC-ERAM defaults; each must also be in `UploadToVnas` | none |
| `FilterByRoi` | `Y` / `N` | `N` |
| `RoiSwLat`, `RoiSwLon`, `RoiNeLat`, `RoiNeLon` | decimal degrees; **required** when `FilterByRoi` is `Y` | none |

### Where files go

`OutputDirectory` is the folder the run writes into; files are laid out inside it by
`AiracOutputPaths`. The GUI does not send it: `AiracService` sets it on every block to the run's
cycle folder, `<output>[\FE-Buddy_Output]\AIRAC_<cycle>` (from `AiracServiceSettings`), so every
sub-service writes into the same folder. The harness and tests pass their own.

| File | Goes in |
|---|---|
| Alias file | `<OutputDirectory>\` |
| GeoJSON | `<OutputDirectory>\Geojson\` (Departures, Arrivals: `…\Geojson\<ARTCC>\<ARPT>\`) |
| Marked for vNAS | the same, under `<OutputDirectory>\Upload_to_vNAS\` instead |

### vNAS file keys

A **file key** names one output file: a GeoJSON file's name without `.geojson`, or the alias
file's name. Departures and Arrivals each write thousands of GeoJSON files, so their keys name
every file of one kind. Keys match ignoring case; a key the sub-service does not write, a
`CrcDefaultsFor` key that is not in `UploadToVnas`, or the alias file in `CrcDefaultsFor`, throws.
A key for a file that is not written this run (its kind is switched off, say) is accepted and does
nothing.

| Sub-service | GeoJSON keys | Alias key |
|---|---|---|
| Airports | `Runways_Lines`, `Airports_Symbols`, `Airports_Text` | `Airports.txt` |
| Airways | `Airways_<group>_Lines` / `_Symbols` / `_Text`, where `<group>` is `High`, `Low`, `Other` or a designation (letters only) | `Airways.txt` |
| Departures | `Departures_Lines`, `Departures_Symbols`, `Departures_Text` | `Departures.txt` |
| Arrivals | `Arrivals_Lines`, `Arrivals_Symbols`, `Arrivals_Text` | `Arrivals.txt` |

CRC-ERAM defaults are only ever written to files marked for vNAS, since CRC reads its maps from
vNAS. The keys are listed in each sub-service's `*OutputFiles` class.

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

- **CRC classes:** `High`, `Low`, `Other`, each with `Line`, `Symbol` and `Text`. With `HighLow`,
  a file in `CrcDefaultsFor` needs only its own class (`Airways_High_Lines` needs `Crc.High.Line.*`);
  with `Designation`, a file can hold every class, so it needs all three of its kind.
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

## Arrivals

| Key | Values | Default |
|---|---|---|
| `GenerateGeojson` | `Y` / `N` | `Y` |
| `EmitLines`, `EmitSymbols`, `EmitText` | `Y` / `N` | `Y` |
| `ArtccFilter` | list of ARTCC IDs; empty means every ARTCC | none |
| `AmendmentFilter` | `None`, `Cycles`, `Days`, `Date` | `None` |
| `AmendedWithinCycles` | 1-1000 (1 = the selected cycle); **required** with `Cycles` | - |
| `AmendedWithinDays` | 1-36500, counted back from today; **required** with `Days` | - |
| `AmendedOnOrAfter` | `yyyy-MM-dd`; **required** with `Date` | - |
| `RoiMode` | `Airport` (every arrival of an airport inside the ROI), `Waypoint` (any arrival with a point inside) | `Airport` |

- **CRC class:** `Arrivals`, with `Line`, `Symbol` and `Text`.
- **`FebProperties`:** `arrivalName`, `pointId`, `arptId`, `artcc`, `amendmentNo`, `amendEffDate`,
  `waypoints`.
- The same keys as Departures, minus `IncludeObstacleDepartures` - a STAR has no obstacle/SID
  split, so the key is not read; sending it anyway is an unknown-key warning, not an error.
- `GenerateGeojson` and `GenerateAliasFile` cannot both be `N`, and `GenerateGeojson = Y` needs at
  least one `Emit…`.
- Only the active amendment mode's value is read (and required); values for the other modes are
  ignored.

## Keys every file conversion reads

A file conversion is not an AIRAC sub-service: it has no alias file, `feb.*` properties, ROI or
vNAS files. Every conversion reads these (shared reading: `ConversionSettingsReader`), plus
`CoordinatePrecision` and the `Crc.*` keys as in the tables above. It writes into its own folder
next to the `AIRAC_<cycle>` folders, `<OutputDirectory>[\FE-Buddy_Output]\<conversion>`.

| Key | Values | Default |
|---|---|---|
| `OutputDirectory` | folder path | **required** |
| `AddFeBuddyOutputFolder` | `Y` / `N` | `Y` |
| `IncludeCrcLineDefaults`, `IncludeCrcSymbolDefaults`, `IncludeCrcTextDefaults` | `Y` / `N` - write that kind's CRC-ERAM defaults (the Include box on its panel); only a kind the conversion writes is read | `N` |
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

## vERAM to GeoJSON (File Conversions)

Extension `.xml` (a vERAM GeoMaps file; any other XML fails that file only).

| Key | Values | Default |
|---|---|---|
| `OutputLayout` | `ByObject` (a file per GeoMapObject description), `ByFilter` (files by filter index and similar attributes) | `ByObject` |
| `DefaultsSource` | `Xml` (carry over the XML's defaults and element overrides), `XmlThenCard` (the tab's defaults where an object has none), `Card` (the tab's defaults only; the XML's styling is ignored) | `Xml` |

- **CRC class:** `GeoMap`, with `Line`, `Symbol` and `Text`: `Crc.GeoMap.Line.*`,
  `Crc.GeoMap.Symbol.*`, `Crc.GeoMap.Text.*`.
- The tab's CRC defaults are read only when `DefaultsSource` is `XmlThenCard` or `Card`, and then
  only for kinds whose `IncludeCrc…Defaults` is `Y`. With `Xml` they are ignored.

## An example (Airways, as the harness writes it)

```csharp
new Dictionary<string, string>
{
	{ "OutputDirectory", @"C:\FE-Buddy-Output" },
	{ "OutputBy", "HighLow" },
	{ "GenerateAliasFile", "Y" },
	{ "IncludeFebCustomProperties", "Y" },
	{ "FebProperties", "awyId,pointId,waypoints" },
	{ "UploadToVnas", "Airways_High_Lines,Airways_Low_Lines,Airways.txt" },
	{ "CrcDefaultsFor", "Airways_High_Lines" },
	{ "Crc.High.Line.bcg", "1" },
	{ "Crc.High.Line.filters", "1,2" },
	{ "Crc.High.Line.style", "solid" },
	{ "Crc.High.Line.thickness", "1" },
	// ...a class and kind for every other file in CrcDefaultsFor
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
