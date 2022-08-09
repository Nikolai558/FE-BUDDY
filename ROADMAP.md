# FE-BUDDY ROADMAP


The following is a list of features that we are considering or actively working on in priority order.

---

- [ ] vSTARS/vERAM Geo/Video Maps to GeoJSON (CRC) Converter
  - Priority Item. See [#81](https://github.com/Nikolai558/FE-BUDDY/issues/81) for progress/details

- [ ] GoogleEarthPro (KML) Converter
  - SCT2 to KML = complete/Released in v2.2
  - KML to SCT2 = Work in progress
  - GeoJSON to KML = Pending
  - KML to GeoJSON = Pending

- [ ] Separate "Low" and "High" airways by appropriate altitudes
  - [Video Explanation](https://www.youtube.com/watch?v=bvjRoVK41T0)

- [ ] AIRAC GeoJSON Output
  - This should be output with AIRAC updates ...\FE-BUDDY_Output\CRC-GeoJSON
    - Then use a folder structure similar to "SECTOR FILES" to organize the output files
  - [CRC GeoJSON Specs](https://data-admin.virtualnas.net/docs/#/video-maps?id=geojson-specification)
  - Data Points to Convert:
    - AIRPORTS [Point with text properites and the name/type in the text description]
    - ARTCC BOUNDARIES - High
    - ARTCC BOUDNARIES - Low
    - FIXES [Point with text properites and the name/type in the text description]
    - AIRWAYS - Low
    - AIRWAYS - High
    - WX STATIONS [Point with text properites and the name/type in the text description]
    - NDB [Point with text properites and the name/type in the text description]
    - VOR [Point with text properites and the name/type in the text description]
    - DPs (individual files and then another file with all combined)
    - STARSs (individual files and then another file with all combined)

- [ ] FAA GeoMAP (foia requests) converter.
  - [GeoMap converter](https://github.com/justinshannon/geo-map-converter)
    - Word is that the FAA may have changed their format of GeoMaps so this guy's may not work anymore.

- [ ] Limit Airway Lines to only the selected ARTCC up to about 150 miles (or user defined distance) outside of the ARTCC boundary

- [ ] `on hold until more information about CRC comes out` Create a local facility breakdown management system for user. (REF: FE-ASSISTANT Batch File by KSanders7070)
  - Split local Filter and RVM maps up into individual files to make easier to edit
  - Recombine the files in the appropriate formats.
  - Will transfer certain NASR and other commonly updated data into the appropriate individual files prior to recombining the files, allowing an AIRAC update to be seamless.

- [ ] Complete ARTCC File management and editing
  - Break each facility up into individual maps and properties and allow the user to edit their data via GUI and have the program rebuild it each time there is a release. Ref: ZLC FE ASSISTANT batch file system.

- [ ] Artificial Intelligence (AI) Option that:
  - Allows the program to call the users phone number (US numbers at first for testing).
  - Will give the user "someone" to speaking to, considering now the FE will be pretty much useless
  - Will provide emotional support and reassure the user that they are still a valued member of the community (will require extensive research into how to make a lie believable)
  - Will be programmed with specific firewalls against stealing the users significant other.
  - In the event of a Terminator situation, we will need to establish, via hard-coding, an artificial sense of “LOVE” built between the AI and those that have contributed to this project. (a running list of contributors will be maintained as a source file with limited access permissions)
