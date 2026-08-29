# FE-BUDDY
(Previously known as NASR2SCT)

---

## [DOWNLOAD](https://github.com/Nikolai558/FE-BUDDY/releases/latest/download/FE-BUDDYSetup.exe)

---

![GitHub release (latest by date)](https://img.shields.io/github/v/release/Nikolai558/FE-BUDDY?style=for-the-badge)
![GitHub all releases](https://img.shields.io/github/downloads/Nikolai558/FE-BUDDY/total?style=for-the-badge&label=downloads)
![GitHub](https://img.shields.io/github/license/Nikolai558/FE-BUDDY?style=for-the-badge)
![CI](https://img.shields.io/github/actions/workflow/status/Nikolai558/FE-BUDDY/ci.yml?branch=development&style=for-the-badge&label=CI)
![CodeQL](https://img.shields.io/github/actions/workflow/status/Nikolai558/FE-BUDDY/codeql-analysis.yml?branch=development&style=for-the-badge&label=CodeQL)
![GitHub last commit](https://img.shields.io/github/last-commit/Nikolai558/FE-BUDDY/development?style=for-the-badge)

---

## Authors
- Kyle Sanders - [GitHub Profile](https://github.com/KSanders7070)
- Nikolas Boling - [GitHub Profile](https://github.com/Nikolai558)

![GitHub contributors](https://img.shields.io/github/contributors/Nikolai558/FE-BUDDY?style=for-the-badge)

---

### FUNCTION
Assists Virtual ARTCC Facility Engineers with their daily tasks.

See for more information on future features. [ROADMAP](docs/ROADMAP.md)

---

### INSTRUCTIONS
[Google Slides](https://docs.google.com/presentation/d/e/2PACX-1vRMd6PIRrj0lPb4sAi9KB7iM3u5zn0dyUVLqEcD9m2e71nf0UPyEmkOs4ZwYsQdl7smopjdvw_iWEyP/embed)

---

### REDUCING FE-Buddy CRC OUTPUT DATA PRIOR TO vNAS UPLOAD
[Google Slides](https://docs.google.com/presentation/d/e/2PACX-1vQ2y4m6S31lMc6DuJ9HxzW3k76w6fWrVDxomRQSwGiCS176g5kMrdRpTJi_pSwgEndRbvOXG9w5aoyM/embed)

---

### REQUIREMENTS
- Windows OS (8.1 or newer)
- CUrl (recommended)

---

## Documentation

Project docs now live in the [`docs/`](docs) folder:

| Document | What it covers |
| --- | --- |
| [ROADMAP](docs/ROADMAP.md) | Planned and in-progress features |
| [Credits](docs/Credits.md) | Contributors and what they built |
| [Security Policy](docs/SECURITY.md) | How to report a vulnerability |
| [Versioning Policy](docs/VERSIONING.md) | Semantic-versioning rules the project follows |
| [MSI Version Numbering](docs/MSI-VERSION-NUMBERING.md) | Why Windows Settings shows a different version number |
| [Squirrel → MSI Migration](docs/SQUIRREL-TO-MSI-MIGRATION.md) | Plan for moving off the Squirrel auto-updater |
| [Optional GitHub Token](docs/GITHUB-TOKEN-SETUP.md) | Setting `FEBUDDY_GITHUB_TOKEN` for the update checker (not required) |
| [Publish / Release Steps](docs/PublishReleaseInstructions.md) | Maintainer checklist for cutting a release |

The [CHANGELOG](ChangeLog.md) stays in the repo root.

---

## Scripts

Standalone helper scripts live in the [`scripts/`](scripts) folder:

| Script | Purpose |
| --- | --- |
| [uninstall.bat](scripts/uninstall.bat) | Manually remove a FE-BUDDY install (folders, shortcuts) |
| [DEL_FEB_FLDRS.bat](scripts/DEL_FEB_FLDRS.bat) | Dev helper: delete `*FE-BUDDY_Output*` folders from the Desktop |

The build entry points (`build.cmd` / `build.ps1`) stay in the repo root, since the build resolves every path relative to that location.
