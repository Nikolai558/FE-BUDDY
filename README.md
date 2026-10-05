# FE-BUDDY

<p align="center">
  <img width="500" height="500" alt="FE-Buddy" src="https://github.com/user-attachments/assets/949c76bc-d20d-4a89-874c-c6d74757c0f8" />
</p>

---

## [DOWNLOAD](https://github.com/Nikolai558/FE-BUDDY/releases)

FE-Buddy 3.x installs from `FE-BUDDY-Setup.msi` on the [Releases](https://github.com/Nikolai558/FE-BUDDY/releases)
page. It upgrades an existing 2.9 or later install in place, and removes an older 2.x the first time
it starts. What changed in each release: [ChangeLog.md](ChangeLog.md).

---

![GitHub release (latest, including pre-releases)](https://img.shields.io/github/v/release/Nikolai558/FE-BUDDY?include_prereleases&style=for-the-badge)
![GitHub all releases](https://img.shields.io/github/downloads/Nikolai558/FE-BUDDY/total?style=for-the-badge&label=downloads)
![GitHub](https://img.shields.io/github/license/Nikolai558/FE-BUDDY?style=for-the-badge)
![CI](https://img.shields.io/github/actions/workflow/status/Nikolai558/FE-BUDDY/ci.yml?branch=v3-development&style=for-the-badge&label=CI)
![CodeQL](https://img.shields.io/github/actions/workflow/status/Nikolai558/FE-BUDDY/codeql-analysis.yml?branch=v3-development&style=for-the-badge&label=CodeQL)
![GitHub last commit](https://img.shields.io/github/last-commit/Nikolai558/FE-BUDDY/v3-development?style=for-the-badge)
![GitHub contributors](https://img.shields.io/github/contributors/Nikolai558/FE-BUDDY?style=for-the-badge)

---

### FUNCTION

Assists VATSIM ARTCC Facility Engineers with their routine, tedious and sometimes complex tasks.
Every 28 days the FAA publishes new aeronautical data; FE-Buddy downloads it and turns it into the
files a facility needs - **GeoJSON video maps** for CRC and **alias files** of dot-commands - limited
to your facility's area and styled the way you choose.

FE-Buddy 3.0 is a from-scratch rewrite. It makes:

| | GeoJSON | Alias file |
|---|---|---|
| **ARTCC Boundaries** | Every ARTCC's boundary, by high/low altitude, high/low/unlimited, or one file per ARTCC and altitude | - |
| **Airports** | Airport symbols and labels, runway lines | `Airports.txt` |
| **Airways** | Airway lines, waypoint symbols and labels - by high/low altitude or by designation | `Airways.txt` |
| **Arrivals** | STARs, per airport | `Arrivals.txt` |
| **Departures** | SIDs (and ODPs if you want them), per airport | `Departures.txt` |
| **NAVAIDs** | VOR, NDB, TACAN and every other NASR NAVAID type | `Navaids.txt` |
| **Fixes** | Every NASR fix, in one file, by fix use, by chart, or by chart and fix use | - |
| **Procedures** | - (`Procedure_Changes.md` and `Procedures.json` instead, from the FAA's d-TPP Metafile) | `Faa_Chart_Recall.txt` |
| **Telephony** | - | `Telephony.txt` - operators by designator and by spoken telephony, plus virtual airlines |
| **Wx Stations** | Every US and US-territory station that reports METAR, from aviationweather.gov | - |
| **Concatenate Aliases** | - | `Combined_Alias.txt` - every alias file the run makes, then your facility's own, as one file for vNAS |

It also converts FAA `.dat` video maps, VRC sector files and ERAM GeoMaps to GeoJSON, has a map to
check GeoJSON files and set your Region of Interest, and exports an Alias Command Guide and practice
quiz for your controllers. Some 2.x tools aren't in 3.0 yet - see
[Do I still need 2.x?](docs/Users/FAQ-and-Troubleshooting.md#do-i-still-need-fe-buddy-2x)

---

### INSTRUCTIONS

- **New to FE-Buddy?** [Getting started](docs/Users/Getting-Started.md).
- **Every screen and option:** the [user guide](docs/Users/User-Guide.md).
- **Something wrong?** [FAQ and troubleshooting](docs/Users/FAQ-and-Troubleshooting.md).

FE-Buddy 2.x: [instructions](https://docs.google.com/presentation/d/e/2PACX-1vRMd6PIRrj0lPb4sAi9KB7iM3u5zn0dyUVLqEcD9m2e71nf0UPyEmkOs4ZwYsQdl7smopjdvw_iWEyP/embed)
and [reducing CRC output before vNAS upload](https://docs.google.com/presentation/d/e/2PACX-1vQ2y4m6S31lMc6DuJ9HxzW3k76w6fWrVDxomRQSwGiCS176g5kMrdRpTJi_pSwgEndRbvOXG9w5aoyM/embed)
(Google Slides).

---

### REQUIREMENTS

- Windows 10 or 11 (64-bit)
- An internet connection (to download the FAA data)

Nothing else - FE-Buddy carries its own .NET runtime.

---

## Documentation

Everything is in [`docs/`](docs/README.md). Users: [Getting started](docs/Users/Getting-Started.md) ·
[User guide](docs/Users/User-Guide.md) · [FAQ](docs/Users/FAQ-and-Troubleshooting.md) ·
[Glossary](docs/Users/Glossary.md). Developers: [Developer guide](docs/Developers/README.md).

---

## For developers

FE-Buddy 3.x is C# / .NET 10 (WPF) on the `v3-development` branch; 2.x lives on `development`.

```bash
dotnet build FeBuddy/FeBuddy.sln
dotnet run --project FeBuddy/FeBuddy.Wpf
dotnet test FeBuddy/FeBuddy.UnitTests
```

The harness, the checks, building the installer and releasing are in the
[Developer guide](docs/Developers/README.md).

---

## Report issues or request features

Open an [issue](https://github.com/Nikolai558/FE-BUDDY/issues), or ask on the FE-Buddy Discord
(the link is on the app's Dashboard). For a crash, attach the files listed in
[FE-Buddy crashed](docs/Users/FAQ-and-Troubleshooting.md#fe-buddy-crashed).

---

## Authors

- [Nikolas Boling](https://github.com/Nikolai558) - Primary Developer and Programmer
- [Kyle Sanders](https://github.com/KCSanders7070) - Concept Design and Original Author

## Acknowledgments

- [Kyle Rodgers](https://github.com/misterrodg) - Developer and Antimeridian Split Logic
- John Lewis - Icon and Logo Design
- Chris James - Program Name
- [Cian Ormond](https://github.com/wiggleforlife) - .NET 6 Conversion Assistance
- [Caelan Sayler](https://github.com/caesay) - .NET 6 Conversion Assistance
- Ian Drake - FAA FOIA RVM Conversion source code reference
- Jon Galad - v3.0 icon and logo design
- Nick Shuster - v3.0 icon and logo design
- [Natural Earth](https://www.naturalearthdata.com) - The map's base layers (US states and coastlines, public domain)

If your name is listed above and you'd like a different link attached to it, or if your name should
be listed here but isn't, please let us know via a pull request or on our Discord. We want to make
sure everyone gets the credit they're due.

---

## License

GNU General Public License v3.0 - see [LICENSE.md](LICENSE.md).
