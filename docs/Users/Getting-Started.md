# Getting started

This walks you from nothing to your first set of files. It takes about ten minutes, most of it
waiting for the FAA data to download the first time.

## 1. Install

1. Download the latest `FE-BUDDY-<version>.msi` from
   [GitHub Releases](https://github.com/Nikolai558/FE-BUDDY/releases).
2. Run it. Windows may ask for administrator permission - FE-Buddy installs for everyone on the
   PC, into `Program Files\FE-BUDDY`, with a Start menu and a desktop shortcut.

**You need:** Windows 10 or 11 (64-bit) and an internet connection. Nothing else - FE-Buddy
carries everything it needs.

Already have FE-Buddy 2.x? The 3.0 installer upgrades it in place.

## 2. First launch

Open FE-Buddy. The window has a menu down the left (**Dashboard**, **AIRAC Service**, **Map**,
then **Settings** and **Info**) and a status line across the top.

The first launch downloads the FAA data for three AIRAC cycles, which can take a few minutes.
The top of the window shows what it is doing (`Downloading cycle 2610…`, `Parsing cycle 2610…`)
and settles on the current cycle when it is done. Later launches reuse what was downloaded and are
much quicker.

## 3. Settings - do these once

Open **Settings**:

1. **Default Output Directory** - where your files go. The default is a `FE-Buddy_Output` folder
   on your Desktop. Leave **Add a FE-Buddy_Output folder inside that directory** on if you point
   it at a folder that has other things in it.
2. **Default Region of Interest** - press **Set ROI…** and drag a box around your ARTCC on the map,
   a little bigger than your boundary. Everything FE-Buddy makes is then limited to that box.
   You can skip this, but you will get the whole country.
3. Press **Save**.

## 4. Your first run

1. Open **AIRAC Service**.
2. On the **General** tab, leave the cycle on **Current** and tick **Airways** (tick others too if
   you like). A tab for each one appears in the rail on the left.
3. Open the **Airways** tab. The defaults are sensible; the one thing you must fill in is the
   **CRC ERAM Defaults** card - every box needs a value (or untick **Include** on that panel if you
   do not want default styles in the file). Press **Save**.
4. Open **Preview Settings**. It spells out what the run will do. Press **Run AIRAC Service**.
5. The **Review** tab shows progress and, when it finishes, what was written. Press
   **Open output folder** to see your files.

## 5. Next cycle

Every 28 days: open FE-Buddy, go to **AIRAC Service**, check the cycle, press **Run AIRAC
Service** on the Preview Settings tab. Your settings are remembered.

## Where next

- The [user guide](User-Guide.md) explains every option.
- Words you don't know are in the [glossary](Glossary.md).
