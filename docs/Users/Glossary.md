# Glossary

The words FE-Buddy and its documentation use, in plain terms.

**AIRAC cycle** - The FAA (and every other aviation authority) updates its aeronautical data on a
fixed 28-day schedule called AIRAC (Aeronautical Information Regulation And Control). Each
28-day period is a *cycle*, named by year and number: `2610` is the 10th cycle of 2026. The
*current* cycle is the one in effect today; the *previous* one just ended; the *next* one starts
on its *effective date*.

**Alias file** - A text file of dot-commands for CRC. A controller types a short command
(`.J3F`) and CRC expands it into something longer (the list of fixes on airway J3). FE-Buddy
writes `Airports.txt`, `Airways.txt`, `Departures.txt`, `Arrivals.txt` and `NAVAIDs.txt`.

**Antimeridian** - The line of ±180° longitude, on the far side of the world from Greenwich. A
line crossing it has to be split in two or it draws the long way round, across the whole map.

**ARTCC** - Air Route Traffic Control Center: the facility that controls a large area of
airspace, like Cleveland Center (ZOB). On VATSIM, each ARTCC has its own facility files.

**BCG** - Brightness Control Group: a CRC setting (1-40) that decides which brightness knob on
the scope controls a map element.

**CRC** - The radar client VATSIM controllers use (Consolidated Radar Client). It displays
GeoJSON video maps and runs alias commands.

**CRC ERAM defaults** - The styles (BCG, filters, line style, thickness, symbol, size, text
options) CRC should use for everything in a GeoJSON file, stored in one hidden feature at the top
of the file. A feature can still override them individually. FE-Buddy writes them only into files
marked for vNAS.

**Designation** - The letters at the front of an airway ID: `J` in J3, `V` in V23, `Q` in Q100.
Roughly, J and Q are high altitude, V and T are low - but FE-Buddy classifies by the published
altitudes, not the letter.

**Effective date** - The day a cycle takes over from the one before.

**FE-Buddy properties (`feb.*`)** - Extra fields FE-Buddy can add to each GeoJSON feature, such
as the airway ID or the airport name. Useful for checking a file; CRC ignores them.

**Filters** - A CRC setting: the numbered map filters (0-40) an element belongs to, so a
controller can turn groups of map elements on and off.

**Fix / waypoint** - A named point used for navigation, like `DOTSS`. Five-letter names are
*fixes* (intersections); NAVAIDs (VORs, NDBs) and airports are points too.

**GeoJSON** - A standard file format for map shapes: points, lines and polygons with
properties. CRC's video maps are GeoJSON files.

**NASR** - The FAA's National Airspace System Resources data: every airport, runway, airway,
fix, NAVAID and procedure in the US, published as a set of CSV files every AIRAC cycle.
FE-Buddy downloads it from the FAA and builds everything from it.

**NAVAID** - Navigational aid: a ground-based transmitter a pilot navigates by, such as a VOR,
VORTAC or NDB. FE-Buddy writes a symbol, a label and alias commands for each NAVAID NASR publishes
(except ones marked SHUTDOWN), for the NAVAID types you tick.

**ODP** - Obstacle Departure Procedure: a departure procedure that exists to keep aircraft clear
of terrain and obstacles, as opposed to a SID.

**Output folder** - Where FE-Buddy writes your files (Settings ▸ Default Output Directory). Each
run of a cycle writes into its own `AIRAC_<cycle>` folder there.

**Region of Interest (ROI)** - A box on the map, set by its south-west and north-east corners.
FE-Buddy only writes data inside (or crossing) it. Make it a little bigger than your ARTCC.

**Run** - One press of **Run AIRAC Service**: every ticked sub-service makes its files.

**SID** - Standard Instrument Departure: a published departure route from an airport.

**STAR** - Standard Terminal Arrival: a published arrival route into an airport.

**Sub-service** - One kind of data the AIRAC Service can produce: Airports, Airways, Departures,
Arrivals or NAVAIDs. Each has its own tab and settings.

**Video map** - The map background on a controller's scope: airways, airports, boundaries.
In CRC, these are GeoJSON files.

**vNAS** - VATSIM's system for ARTCC facility data. You upload the files FE-Buddy makes to vNAS
for CRC to use. The files you mark for vNAS on a sub-service tab are written to an
`Upload_to_vNAS` folder, ready to upload.
