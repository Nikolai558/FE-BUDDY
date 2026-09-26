# How FE-Buddy works

This page explains FE-Buddy's code in plain terms first, then points you to the detailed pages.
You do not need to know C# to read the first half.

## The short version

Think of FE-Buddy as a small factory with a control panel.

1. **Raw material comes in.** Every 28 days the FAA publishes a zip of CSV spreadsheets (the
   *NASR* data): every airport, runway, airway, fix and procedure in the US. At launch, FE-Buddy
   downloads the zips for three cycles (last, this, next), unpacks them into a cache on disk,
   and reads every spreadsheet into memory.
2. **The user fills in an order form.** On the AIRAC Service screen they tick what they want
   (Airports, Airways, Departures, Arrivals, NAVAIDs, ARTCC Boundaries, Fixes) and set the options -
   which files, which styles, which area.
   Each tab turns its options into a simple list of `key = value` settings, the same format the
   test harness uses.
3. **The factory builds.** For each ticked item, the library checks the settings, finds the right
   rows in the FAA data, and turns them into real objects - an airway with its waypoints in order,
   an airport with its runways - fixing the FAA's quirks along the way (border markers that are
   not real waypoints, procedures named by an amendment code, and so on).
4. **The goods are packaged.** Writers turn those objects into GeoJSON files (map shapes CRC can
   draw, with the styling CRC needs) and alias files (dot-commands), and put them in the user's
   output folder.
5. **A receipt comes back.** Everything that happened - files written, warnings, things skipped
   and why - is returned to the app, which shows it on the Review tab and in the activity log.

The **library** (`FeBuddy.Core`) is the factory: it has no windows and never asks the user
anything. The **app** (`FeBuddy.Wpf`) is the control panel: it shows screens, saves settings, and
hands the library a settings list. Keeping them apart means the same factory can be driven by the
app, by the console **harness** (`FeBuddy.Harness`) and by the **tests**.

## The projects

All in `FeBuddy/FeBuddy.sln`:

| Project | Is | Depends on |
|---|---|---|
| `FeBuddy.Core` | The library: FAA data, the AIRAC services, GeoJSON and alias writing, config, logging, updates. | Versioning, NetTopologySuite, CsvHelper |
| `FeBuddy.Wpf` | The desktop app (`FE-BUDDY.exe`): screens, view-models, the design system. | Core |
| `FeBuddy.Versioning` | FE-Buddy's version number and the upgrade rule, shared with the installer. netstandard2.0. | Semver |
| `FeBuddy.Installer` | The MSI (WiX). | the published app, CustomActions |
| `FeBuddy.Installer.CustomActions` | The MSI's one piece of code: enforce the upgrade rule. net472. | Versioning |
| `FeBuddy.Harness` | A console app that runs the services with hard-coded settings - handy while developing. | Core |
| `FeBuddy.UnitTests` | xUnit tests for Core and Versioning. | Core, Versioning |

## Where next

| To... | Read |
|---|---|
| build, run and test it | [Getting started](Getting-Started.md) |
| see the detailed picture (launch, the data pipeline, a run end to end, design decisions) | [Architecture](Architecture.md) |
| find or add code in the library | [FeBuddy.Core structure](FeBuddy.Core-Structure.md) |
| find or add code in the app | [FeBuddy.Wpf](FeBuddy.Wpf/README.md) |
| know every settings key a sub-service reads | [Settings blocks](Settings-Blocks.md) |
| know every saved setting | [UserConfig.json reference](UserConfig-Reference.md) |
| cut a release | [Versioning](VERSIONING.md) |
| pick something up | [TODO](TODO.md) |

The standards every project follows (one `.editorconfig`, required XML docs, the Models/ rule,
one type per file, the coverage gate) are listed at the end of the
[Core structure page](FeBuddy.Core-Structure.md#standards-and-checks).
