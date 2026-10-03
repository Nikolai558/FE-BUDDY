# Getting started

## What FE-Buddy does

Every 28 days the FAA publishes new aeronautical data: new airways, moved fixes, amended procedures.
If you are a facility engineer for a VATSIM ARTCC, your video maps and alias files need to follow.
FE-Buddy downloads the data and writes the files for you:

- **GeoJSON video maps** for CRC - airports and runways, airways, SIDs and STARs, NAVAIDs, fixes,
  ARTCC boundaries and weather stations.
- **Alias files** of dot-commands for controllers - airport, NAVAID and airline information, the
  fixes of an airway or procedure, and FAA chart recall.
- **One `vNAS_Alias.txt`**, ready to upload, with your facility's own aliases merged in.
- **Procedure change reports** for the airports you care about.

You choose what to make and how it looks, and a **Region of Interest** keeps the maps to your area.
FE-Buddy remembers your choices, so each new cycle is a couple of clicks. It also converts FAA `.dat`
video maps, VRC sector files and ERAM GeoMaps to GeoJSON, and has a map for checking GeoJSON files.

Some 2.x tools aren't in 3.0 yet; see [Do I still need FE-Buddy 2.x?](FAQ-and-Troubleshooting.md#do-i-still-need-fe-buddy-2x)

## 1. Install

Download `FE-BUDDY-Setup.msi` from the newest release on
[GitHub Releases](https://github.com/Nikolai558/FE-BUDDY/releases) and run it. It upgrades
FE-Buddy 2.9 or later in place.

You need Windows 10 or 11 (64-bit) and an internet connection; FE-Buddy brings everything else.

## 2. First launch

The window has a menu down the left - **Dashboard**, **AIRAC Service**, **File Conversions**,
**Map**, then **Settings** and **Info** - and a status line across the top.

The first launch downloads the FAA data for the previous, current and next AIRAC cycles (the next
once the FAA has published it), which can take a few minutes. The top of the window says what it's doing and settles on the current cycle
when it's done. Later launches reuse the download and are much quicker.

## 3. Settings - once

Open **Settings**:

1. **Default Output Directory** - where your files go. It starts as your Desktop, with **Add a
   FE-Buddy_Output folder inside that directory** on, so files land in `Desktop\FE-Buddy_Output`. Each cycle gets its own
   folder in there, such as `AIRAC_2610`.
2. **Default Region of Interest** - press **Set ROI…**, drag a box a little bigger than your ARTCC,
   and press **Use this ROI**. You can skip this, but you'll get the whole country.
3. Press **Save**.

## 4. Your first run

1. Open **AIRAC Service**. On the **General** tab, leave the cycle on **Current** and tick
   **Airways** (and anything else you like). Each one you tick gets a tab.
2. Open the **Airways** tab. On **High and Low Files**, choose High, Low or Both for each airway type
   that's still blank (J and Q start in High, V and T in Low), or untick the types you don't want
   under **Designations to Include**; the tab stays red until you do. Near the end, on **Upload to
   vNAS**, tick the files you'll upload to vNAS. If you tick a GeoJSON file, choose whether it gets CRC-ERAM defaults,
   then fill every box on the **CRC ERAM Defaults** card that appears. Press **Save**.
3. Open **Preview Settings**, check what the run will do, and press **Run AIRAC Service**.
4. The **Review** tab shows progress, then what was written. **Open output folder** takes you to
   your files.

To add your facility's own aliases to `vNAS_Alias.txt`, tick **vNAS Alias Upload** on the General
tab and add your files on its tab.

## 5. Every cycle after

Open FE-Buddy, go to **AIRAC Service**, check the cycle, and press **Run AIRAC Service** on the
Preview Settings tab. Your settings are remembered.

## Where next

- [User guide](User-Guide.md) - every screen and option.
- [FAQ and troubleshooting](FAQ-and-Troubleshooting.md) - when something looks wrong.
- [Glossary](Glossary.md) - AIRAC, NASR, CRC and the rest.
