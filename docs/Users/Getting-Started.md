# Getting started

## What FE-Buddy does

Every 28 days the FAA publishes new aeronautical data: new airways, moved fixes, amended procedures.
If you are a facility engineer for a VATSIM ARTCC, your video maps and alias files need to follow.
FE-Buddy downloads the data and writes the files for you:

- **GeoJSON video maps** for CRC - airports and runways, airways, SIDs and STARs, NAVAIDs, fixes,
  ARTCC boundaries and weather stations.
- **Alias files** of dot-commands for controllers - airport, NAVAID and airline information, the
  fixes of an airway or procedure, and FAA chart recall.
- **One `Combined_Alias.txt`**, ready to upload to vNAS, with your facility's own aliases merged in.
- **Procedure change reports** for the airports you care about.

You choose what to make and how it looks, and a **Region of Interest** keeps the maps to your area.
It also converts FAA `.dat` video maps, VRC sector files and ERAM GeoMaps to GeoJSON, and has a map
for checking GeoJSON files.

Some 2.x tools aren't in 3.0 yet; see [Do I still need FE-Buddy 2.x?](FAQ-and-Troubleshooting.md#do-i-still-need-fe-buddy-2x)

## 1. Install

Download `FE-BUDDY-Setup.msi` from the newest release on
[GitHub Releases](https://github.com/Nikolai558/FE-BUDDY/releases) and run it. It upgrades
FE-Buddy 2.9 or later in place, and FE-Buddy removes an older 2.x the first time it starts.

You need Windows 10 or 11 (64-bit) and an internet connection; FE-Buddy brings everything else.

## 2. First launch

The first launch downloads the FAA data for the previous, current and next AIRAC cycles (the next
once the FAA has published it), which can take a few minutes. The top of the window says what it's
doing. Later launches reuse the download and are much quicker.

## 3. Settings - once

Open **Settings**:

1. **Facility Profile** - choose your ARTCC. The tabs that pick ARTCCs start with it ticked.
2. **Default Output Directory** - where your files go: `Desktop\FE-Buddy_Output` to start, with a
   folder per cycle such as `AIRAC_2610`.
3. **Default Region of Interest** - press **Set ROI…**, drag a box a little bigger than your ARTCC,
   and press **Use this ROI**. Skip it and nothing is left out.
4. Press **Save**.

## 4. Your first run

1. Open **AIRAC Service**. On the **General** tab, leave the cycle on **Current**. Every sub-service
   is included to start: untick any you don't want under **Include**, then **Save**.
2. Click each tab with a red dot and fix what the red box at its top lists, then **Save** it. On a
   first run that's usually **Airways**: on **High and Low Files**, choose High, Low or Both for each
   blank airway type, or untick it under **Designations to Include**.
3. Open **Preview Settings**, check what the run will do, and press **Run AIRAC Service**. The
   **Review** tab shows progress, then **Open output folder**.

Before you upload `Combined_Alias.txt`, add your facility's own alias file on the **Concatenate
Aliases** tab, or uploading it removes your facility's aliases from vNAS.

## 5. Every cycle after

Open **AIRAC Service**, set the cycle to **Current** (the General tab keeps the cycle you last
saved, which is now **Previous**), and press **Run AIRAC Service** on Preview Settings.

## Where next

- [User guide](User-Guide.md) - every screen and option.
- [FAQ and troubleshooting](FAQ-and-Troubleshooting.md) - when something looks wrong.
- [Glossary](Glossary.md) - AIRAC, NASR, CRC and the rest.
