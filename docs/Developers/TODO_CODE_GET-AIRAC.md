# GET-AIRAC TODO's

> **Status as of 19 Sep 2026.** Everything originally listed here is done or was
> deliberately dropped. Kept as a record; add new items under GENERAL.

## GENERAL
- ???

---

## AWY
- ~~Complete basic straight output (with efficient line string handling)~~ - **done.**
  One airway is one Feature; breaks become a MultiLineString
  (`FeBuddy.Core/Services/Airac/Airways/AirwayGeometryBuilder.cs`).
- ~~Add Dme-cutoff handling with bool option for on/off.~~ - **dropped as redundant.**
  "Buffer Airway Waypoints" does this: 2.5 NM around 5-character fixes, 5 NM around
  NAVAIDs and airports (`AirwayWaypointBuffer.cs`). See the remediation plan 7.4.

---
