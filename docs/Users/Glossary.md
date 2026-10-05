# Glossary

The words FE-Buddy and its documentation use, in plain terms.

**AIRAC cycle** - The fixed 28-day schedule on which the FAA (and every aviation authority) updates
its aeronautical data. Each cycle is named by year and number: `2610` is the 10th cycle of 2026. The
*current* cycle is in effect today; the *next* one starts on its *effective date*.

**Alias file** - A text file of dot-commands for CRC. A controller types a short command (`.J3F`) and
CRC runs what it stands for (draw the fixes of airway J3).

**Antimeridian** - The ±180° line of longitude. A line crossing it has to be split in two, or it
draws the long way round the whole map.

**ARTCC** - Air Route Traffic Control Center: the facility controlling a large area of airspace, like
Cleveland Center (ZOB).

**BCG** - Brightness Control Group: the CRC setting (1-40) for which brightness knob controls a map
element.

**`Combined_Alias.txt`** - The one alias file a facility uploads to vNAS: every alias file the run
made, then your facility's own. Written by the Concatenate Aliases tab.

**CRC** - Consolidated Radar Client, the radar client VATSIM controllers use. It draws GeoJSON video
maps and runs alias commands.

**CRC ERAM defaults** - The look (BCG, filters, line style, symbol, size and so on) CRC gives a
GeoJSON file's features, usually stored in one hidden feature at the top of the file. Set on each
tab's CRC ERAM Defaults card.

**d-TPP Metafile** - The FAA's index of every chart in the Digital Terminal Procedures Publication -
approach plates, SIDs, STARs, airport diagrams. Published each cycle, but only 15-18 days before it
starts. Procedures is built from it.

**Designation** - The letters at the front of an airway ID: `J` in J3, `V` in V23.

**FAA Chart Recall** - `Faa_Chart_Recall.txt`, Procedures' alias file: commands that open the FAA's
current charts, a page each, such as `.dtwI22Lc` for DTW's ILS OR LOC RWY 22L.

**FE-Buddy properties (`feb.*`)** - Optional extra fields on each GeoJSON feature, such as an airway's
ID. Handy for checking a file; CRC ignores them.

**Filters** - The CRC setting (0-40) that groups map elements, so a controller can turn groups on and
off.

**Fix** - A named point used for navigation, such as `DOTSS`.

**GeoJSON** - A standard file format for map shapes - points, lines and areas - with properties.
CRC's video maps are GeoJSON.

**METAR** - A routine hourly weather report from an airport or weather station.

**NASR** - The FAA's National Airspace System Resources data: every US airport, runway, airway, fix,
NAVAID and procedure, published as CSV files each AIRAC cycle. Most of FE-Buddy is built from it.

**NAVAID** - A ground-based radio aid a pilot navigates by, such as a VOR, VORTAC or NDB.

**ODP** - Obstacle Departure Procedure: a departure that keeps aircraft clear of terrain and
obstacles, as opposed to a SID.

**Output folder** - Where FE-Buddy writes your files (Settings ▸ Default Output Directory). Each cycle
gets its own `AIRAC_<cycle>` folder there.

**Region of Interest (ROI)** - A box on the map, set by its south-west and north-east corners.
FE-Buddy's maps keep only what is inside or crossing it. Make it a little bigger than your ARTCC.

**SID** - Standard Instrument Departure: a published departure route.

**STAR** - Standard Terminal Arrival: a published arrival route.

**Sub-service** - One kind of output the AIRAC Service makes: ARTCC Boundaries, Airports, Airways,
Arrivals, Departures, NAVAIDs, Fixes, Procedures, Telephony, Wx Stations or Concatenate Aliases. Each
has its own tab. See the [user guide](User-Guide.md#airac-service).

**Telephony** - An airline or operator's spoken call sign, such as `DELTA`. Also the sub-service
that writes `.id` commands for each operator's designator and telephony.

**Video map** - The map background on a controller's scope: airways, airports, boundaries. In CRC,
these are GeoJSON files.

**vNAS** - VATSIM's system for ARTCC facility data. You upload FE-Buddy's GeoJSON files and
`Combined_Alias.txt` to vNAS for CRC to use.
