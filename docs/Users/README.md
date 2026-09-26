# FE-Buddy in plain English

## What it is

If you are a **Facility Engineer** for a VATSIM ARTCC, part of your job is keeping your
facility's maps and shortcuts in step with the real world. Every 28 days the FAA publishes a new
set of aeronautical data - new airways, moved fixes, amended departure procedures - and your
video maps and alias files need to follow.

FE-Buddy does the tedious part. It downloads the FAA's data for you, lets you pick what you want
and how it should look, and writes the files: **GeoJSON video maps** that CRC can display, and
**alias files** with dot-commands controllers can type.

## What it does today (3.0)

| You get | For |
|---|---|
| **Airports** | A symbol and a label for every airport, a line for every runway, and an alias file of airport commands. |
| **Airways** | Every airway as lines, with waypoint symbols and labels - split by high/low altitude or by designation (J, V, Q, T...) - and an alias file that draws an airway's fixes. |
| **Departures** | Every departure procedure (SIDs, and obstacle departures if you want them), as lines, symbols and labels per airport, and an alias file. |
| **Arrivals** | Every arrival procedure (STARs), as lines, symbols and labels per airport, and an alias file - the same idea as Departures, run the other way. |
| **NAVAIDs** | A symbol and a label for every VOR, NDB, TACAN and the rest of the NASR NAVAID types, plus an alias file of NAVAID commands. |
| **ARTCC Boundaries** | Every ARTCC's boundary as lines, split by high/low altitude, high/low/unlimited, or one file per ARTCC and altitude - no alias file, since a boundary line carries no label. |
| **Fixes** | A symbol and a label for every NASR fix - reporting points, waypoints, military points and the rest - split all in one file, by fix use, by chart, or by chart and fix use - no alias file, since a fix's label is always its own identifier. |
| **Wx Stations** | A symbol and a two-line label for every US (and territory) station that reports METAR - no alias file, since a station's label is always its own ICAO ID and IATA ID/name. Unlike every other sub-service, its data comes from aviationweather.gov's own station list, not the FAA's NASR cycle. |
| **File conversions** | Your FAA `.dat` RADAR Video Maps as GeoJSON video maps, optionally cropped to a distance from each map's centre; your VRC `.sct2` sector files as GeoJSON - boundaries, airways, GEO, SIDs, STARs, labels and regions; and the `Geomaps.xml` from your ERAM adaptation export as GeoJSON - lines, symbols, text and SAAs - keeping its styling as CRC defaults. |
| **A map** | Open any GeoJSON file to check it, and draw your facility's Region of Interest. |

Everything the AIRAC Service makes can be limited to a **Region of Interest** - a box around your
ARTCC - so you only get the data you care about.

Many of FE-Buddy 2.x's tools (chart-recall aliases, SCT2 to DXF, vSTARS and FAA GeoMap
conversions, GeoJSON clean-up, procedure ISRs) are not in 3.0 yet. If you need them, keep using 2.x for now.

## How it works, in five steps

1. **You open FE-Buddy.** In the background it downloads the FAA's data for the current AIRAC
   cycle, the one before it and the one after it (if the FAA has published it yet). The status
   at the top of the window tells you when it is ready.
2. **You pick a cycle and what to make** on the AIRAC Service screen: Airports, Airways,
   Departures, Arrivals, NAVAIDs, ARTCC Boundaries, Fixes, Wx Stations, or any mix.
3. **You choose the settings** on each one's tab: which files, which styles, which area.
   FE-Buddy remembers everything, so next cycle you only press Run.
4. **You check the summary** on the Preview Settings tab and press **Run AIRAC Service**.
5. **You get your files** in your output folder, ready to upload. The Review tab lists what was
   made and anything worth knowing (for example, an airway that could not be drawn and why).

## Where next

- **New to it?** [Getting started](Getting-Started.md) walks you from installing to your first files.
- **Want every detail?** The [user guide](User-Guide.md) covers every screen and option.
- **A word you don't know?** Try the [glossary](Glossary.md).
- **Something wrong?** See [FAQ and troubleshooting](FAQ-and-Troubleshooting.md).
