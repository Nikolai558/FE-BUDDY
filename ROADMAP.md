# FE-BUDDY ROADMAP


The following is a list of features that we are considering or actively working on in priority order.  
For previous Roadmap items, refer to the [FEB Change Log](https://github.com/Nikolai558/FE-BUDDY/blob/development/ChangeLog.md).

---

- [ ] AIRAC GeoJSON Output
  - This should be output with AIRAC updates ...\FE-BUDDY_Output\CRC
  - Output files names:
    - [X] APT_symbols
    - [X] APT_text
    - [X] ARTCC BOUNDARIES-LOW_lines
    - [X] ARTCC BOUNDARIES-HIGH_lines
    - [X] AWY-HIGH_lines
    - [X] AWY-HIGH_symbols
    - [X] AWY-HIGH_text
    - [X] AWY-LOW_lines
    - [X] AWY-LOW_symbols
    - [X] AWY-LOW_text
    - [ ] DP_lines
    - [X] FIX_symbols
    - [X] FIX_text
    - [X] NDB_symbols
    - [X] NDB_text
    - [X] RWY_lines
    - [ ] STARS_lines
    - [X] VOR_symbols
    - [X] VOR_text
    - [X] WX STATIONS_symbols
    - [X] WX STATIONS_text
  - Add option for user to decide if they want all the geojsons in one folder to make "vNAS Batch Upload" easier.

---
**The following will be completed after a code-rewrite/refactoring of FE-Buddy resulting in version 3.0**
---

- [ ] New UI
  - Utilize WPF

- [ ] Remove VRC AIRAC output features?
  - If after the refactor, holding on to this feature does not take up much development time, keep this feature and offer the user the option to turn off this output. It has been stated that some of the neighbors such as canada use FE-Buddy to updated US data for their files that use SCT2 format.

- [ ] Remove SCT2/DXF/KML?
  - These features are not 100% functional and at this point, may be pointless features to have considering the primary editing software will likely be QGIS that accept GeoJSON format.
  - GeoJSON-to-DXF/KML may be considered for later development.

- [ ] vERAM AWY HI/LO Text and Symbol maps.
  - Complete feature this only if vERAM is still in use after completing the above GeoJSON features.
  - Refer to issue: https://github.com/Nikolai558/FE-BUDDY/issues/109

- [ ] AWY Fix/NAVAID Radius cutoff
  -  Refer to issue: https://github.com/Nikolai558/FE-BUDDY/issues/110

- [ ] Limit Airway Lines and other map features to only the selected ARTCC up to about 150 miles (or user defined distance) outside of the ARTCC boundary
  -  If unable to use the ARTCC Boundary for a reference point, have the user define a coordinate set as the "center" and then define a distance for the cutoff point.

- [ ] FAA GeoMAP (foia requests) converter.
  - [GeoMap converter](https://github.com/justinshannon/geo-map-converter)
    - Word is that the FAA may have changed their format of GeoMaps so this guy's may not work anymore.

- [ ] Alias management
  - Refer to issue: https://github.com/Nikolai558/FE-BUDDY/issues/121

- [ ] vNAS API
  - Refer to issue: https://github.com/Nikolai558/FE-BUDDY/issues/122

- [ ] Facility Admin Tools
  - Refer to issue: https://github.com/Nikolai558/FE-BUDDY/issues/123

- [ ] Create more resources to complement the vNAS system.
  - This will require more time to see what resources wohld actually be useful.

- [ ] Artificial Intelligence (AI) Option that:
  - Allows the program to notify FEs concerning upcoming AIRAC changes via phone call (US numbers at first for testing).
  - Will give the user "someone" to speak to, considering now the FE will be pretty much useless
  - Will provide emotional support and reassure the user that they are still a valued member of the community (will require extensive research into how to make a lie believable)
  - Will be programmed with specific firewalls against stealing the users significant other.
  - In the case of a Terminator situation, we will need to establish, via hard-coding, an artificial sense of “LOVE” built between the AI and those that have contributed to this project. (a running list of contributors will be maintained as a source file with limited access permissions)
