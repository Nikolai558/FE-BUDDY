# FE-BUDDY ROADMAP


The following is a list of features that we are considering or actively working on in priority order.

---

- [ ] SCT2 2 AutoCAD Converter
  - Hard to find a GH repo of someone doing this already but maybe we can reverse engineer the sct_2_dxf.exe & dxf_2_sct.exe
  - See: https://docs.fileformat.com/cad/dxf/
  - See Also: https://ezdxf.mozman.at/docs/introduction.html

- [ ] GeoJSON Output
  - In preparation for CRC compatibility along with general usability of files
  - For now, create in the format to work with https://geojson.io/
  - The user should be able to select this option on or off.
  - Data Points to Convert:
    - AIRPORTS [Point with text properites and the name/type in the text description]
    - ARTCC BOUNDARIES - High
    - ARTCC BOUDNARIES - Low
    - FIXES [Point with text properites and the name/type in the text description]
    - AIRWAYS (all in one file at first)
    - WX STATIONS [Point with text properites and the name/type in the text description]
    - NDB [Point with text properites and the name/type in the text description]
    - VOR [Point with text properites and the name/type in the text description]
    - DPs (individual files and then another file with all combined)
    - STARSs (individual files and then another file with all combined)

- [ ] FAA RVM .DAT (foia requests) converter.
  - See: https://github.com/glott/DAT2Sector
  - See also: https://drakedesignlabs.com/converter/source.txt

- [ ] GoogleEarthPro Converter (for airport diagrams and other like-constructs)
  - See: https://github.com/VATSIM-UK/UK-Sector-File

- [ ] FAA GeoMAP (foia requests) converter.
  - See: https://github.com/justinshannon/geo-map-converter
    - Word is that the FAA may have changed their format of GeoMaps so this guy's may not work anymore.

- [ ] Hygieia by dhawton
  - See: https://github.com/dhawton/hygieia

- [ ] Limit Airway Lines to only the selected ARTCC up to about 150 miles (or user defined distance) outside of the ARTCC boundary

- [ ] `on hold until more information about CRC comes out` Create a local facility breakdown management system for user. (REF: FE-ASSISTANT Batch File by KSanders7070)
  - Split local Filter and RVM maps up into individual files to make easier to edit
  - Recombine the files in the appropriate formats.
  - Will transfer certain NASR and other commonly updated data into the appropriate individual files prior to recombining the files, allowing an AIRAC update to be seamless.

- [ ] Prepare program for CRC formats.

- [ ] Separate "Low" and "High" airways by appropriate altitudes

- [ ] Complete ARTCC File management and editing
  - Break each facility up into individual maps and properties and allow the user to edit their data via GUI and have the program rebuild it each time there is a release. Ref: ZLC FE ASSISTANT batch file system.

- [ ] Artificial Intelligence (AI) Option that:
  - Allows the program to call the users phone number (US numbers at first for testing).
  - Will give the user "someone" to speaking to, considering now the FE will be pretty much useless
  - Will provide emotional support and reassure the user that they are still a valued member of the community (will require extensive research into how to make a lie believable)
  - Will be programmed with specific firewalls against stealing the users significant other.
  - In the event of a Terminator situation, we will need to establish, via hard-coding, an artificial sense of “LOVE” built between the AI and those that have contributed to this project. (a running list of contributors will be maintained as a source file with limited access permissions)
