# GUI TODO's

> **Status as of 19 Sep 2026.** The AIRAC Service screen is built (General tab,
> one tab per selected sub-service, Review tab). Every item below is done or was
> deliberately dropped; kept as a record. Add new items under GENERAL.

## GENERAL
- ???

---

## GET-AIRAC

### AWY
- ~~Primarily radio buttons, expanding more menus as options are selected.~~ -
  **built**, as the Airways tab's settings menu (`FeBuddy.Wpf/Views/AirwaysView.xaml`).
- ~~Option to select which to output "LOW" and/or "HIGH"~~ - **built** as the GeoJSON
  output mode: `None` / `High/Low` / `Designation`.
  - ~~include/exclude `AwyDesignation` (J, Q, AB, etc...) types~~ - **built** as
    "Designations to include", one toggle per designation found in the cycle. Note the
    designation is derived from `AWY_ID`, never from the NASR `AWY_DESIGNATION` field
    (remediation plan 3.1).
- ~~Option to have airways break `FixBreakInAwy` at all fixes along the route~~ -
  **dropped.** Replaced by Buffer Airway Waypoints, which stops each leg short of its
  endpoints at a fixed radius rather than breaking at every fix with a per-type DME range.

---
