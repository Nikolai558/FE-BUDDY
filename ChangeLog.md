# Change log

Every FE-BUDDY 3.x release, newest first. When a release is made, its section here becomes the
"Change log" part of its GitHub release notes, which is also what FE-BUDDY's update window shows.
FE-BUDDY 2.x's history is in the [2.x change log](https://github.com/Nikolai558/FE-BUDDY/blob/2.9.3/ChangeLog.md).

<!--
  Adding an entry: put one bullet under "## Unreleased" in the same pull request as the change.
  Write it for users, not developers: what changed and why they care, in one line. Issue numbers
  become links ("Bug #215 - ..."). Put developer-only changes under a "(Dev notes)" bullet.
  Full guide: docs/Developers/RELEASING.md.
-->

## Unreleased

## 3.0.0-alpha.1
- FE-BUDDY 3.0 is a from-scratch rewrite with a new interface. This first alpha is for testers:
  expect rough edges, and keep 2.x handy - not every 2.x tool is in 3.0 yet
  ([Do I still need 2.x?](https://github.com/Nikolai558/FE-BUDDY/blob/v3-development/docs/Users/FAQ-and-Troubleshooting.md#do-i-still-need-fe-buddy-2x)).
- New AIRAC Service: downloads the FAA's data for a cycle and builds GeoJSON video maps and alias
  files for your facility, limited to your Region of Interest and styled the way you choose.
  - Sub-services: Airports, Airways, Departures, Arrivals, NAVAIDs, ARTCC Boundaries, Fixes,
    Wx Stations, Procedures (d-TPP change briefings), Telephony and vNAS Alias Upload.
  - Output goes into one `AIRAC_<cycle>` folder, with an `Upload_to_vNAS` folder ready to upload.
- New File Conversions: DAT, SCT2 and vERAM files to GeoJSON.
- New Map: check GeoJSON files, draw and edit your Region of Interest, and view live AIRAC layers.
- Settings: import and export your settings, pick your update channel, and store a GitHub token
  securely in Windows Credential Manager (with a step-by-step token guide).
- The update window shows the notes for every release you are missing, then downloads and runs
  the installer for you.
- Installs with the same Windows Installer (MSI) as 2.9 and upgrades an existing 2.9 install in place.
- (Dev notes)
  - Rewritten in WPF on .NET 10, with the logic in a separate, unit-tested FeBuddy.Core library.
  - Semantic versioning (alpha, beta, rc) shared by the app and the installer.
  - Automated release pipeline: pre-flight checks on the pull request, then a drafted release.
