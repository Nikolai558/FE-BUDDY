# CHANGELOG

---
- ## Version 2.6.1
  - New AWY Line Files (High and Low) that feature the following:
    - Airways will start and end 5nm from NAVAIDS and 2nm from FIXES.
  - CRC WX Station Names #136
    - WX station IDs will appear on line 1 and names on line 2.
  - CRC NDB/VOR Types #135
    - NAVAID IDs will appear on line 1 and the names and types on line 2

- ## Version 2.6.0
  - Added feature: CRC AIRAC Data Geojsons
    - The output for this feature will be located in
    FE-BUDDY_Output -> CRC with the following data:
      - APT Symbols and Text 
      - ARTCC Boundaries (High and Low) Lines
      - AWY High Lines, Symbols, and Text
      - AWY Low Lines, Symbols, and Text
      - FIX Symbols and Text
      - NDB Symbols and Text
      - VOR Symbols and Text
      - RUNWAY Lines
      - WX STATIONS Symbols and Text
      - DPs Lines
      - STARs Lines
    - NOTE: AWY Symbols are the only files that have Overriding 
    Properties in the geojson.

- ## Version 2.5.0
  - Added feature: SCT2 to GeoJson
    - Data from the following headers will be included 
    with the conversion:
      - ARTCC
      - ARTCC HIGH
      - ARTCC LOW
      - SID
      - STAR
      - LOW AIRWAY
      - HIGH AIRWAY
      - GEO
      - REGIONS
      - LABELS
    - Each individual SID and STAR diagram will be output to a GeoJson file.

- ## Version 2.4.2
  - BUG FIX #127: Missing Lines when vERAM to Geojson
  conversions using the filter and properties option.

- ## Version 2.4.1
  - BUG FIX #125: Index out of bounds for AIRAC Data error.
  - vERAM conversion to Geojson file now checks for 
  reversed coordinates. Example: IF the start lat/lon
  is the same as the ending of the next coordinate lat/lon.

- ## Version 2.4.0
  - AIRAC Date selection now displays the AIRAC Cycle number
  - vERAM-to-GeoJSON now allows the user to select an option
  to have all elements with identical attributes to be grouped
  together into the same GeoJSON file.

- ## Version 2.3.7
  - High and Low AWY Geomap default line style changed to Solid.
  - Roadmap and Credit buttons link changed to GitHub Development Branch.
  - GeoMapObject-GeoJSON feature now includes a log GeoMapObjects Properties.txt
  that lists all of the default values for the user to more easily manage the update
  to the vNAS website.
  - Fixed an issue where FE-Buddy GeoJSON outputs from vERAM 
  conversions included certain properties from the element 
  defaults such as Line Thickess, Filter assignments, etc... 
  This resulted in the GeoJSON properties overriding what the 
  user input as the defaults in the vNAS Admin site. Now, the 
  only time the converter will include the default properties 
  in the GeoJSON is when the individual element has overriding 
  properties within the element line from the .xml.
    - Note: If you have already done the work to convert your 
    files, upload, and set the vNAS default properties, you can 
    use FE-Buddy to do the conversions again and you only need 
    to ensure the file names match what is already on the vNAS 
    site and use the Batch Upload feature of the vNAS site. 
    This will ensure you don't have to set the default properties 
    again in the vNAS site.

- ## Version 2.3.6
  -  Fixed #105 T and J Airways not in [HIGH AIRWAY].txt file.
  - Note for FE's:
    - Default BCG and Filter values for high airways is 5
    - Default BCG and Filter values for low airways is 15

- ## Version 2.3.5
  - .SCT2 Airways File changes: 
    - Filtered out all non "V, T, Q, J" airways from the Regulatory Airway Data.
    - All Non-Regulatory Airways are placed inside the High Airway file.
    - Kept all data for the find-fix alias command for EVERY Airway.
  - Geomap Airways File changes:
    - Hi and Lo airways for the geomap xml files behave exactly like the sct2 file airways.
    - Two new files are created to replace the AWY_GEOPMAP.XML file.
      - AWY_HI_GEOMAP.xml and AWY_LO_GEOMAP.xml

- ## Version 2.3.4
  - #100: Temporary fix for when a color is not defined inside the vSTARS video map. (Skips "color" property)
  - #101: Temporary fix for labels inside the vSTARS video map. (Ignore labels)
  - Added uninstall batch file to GitHub Repository

- ## Version 2.3.3
  - #97: Fixed the problem where the .id commands for the 3LD were not working.

- ## Version 2.3.2
  - #95: Elements inside a ASDEX VideoMap that are not a xsi:type of "Path" will not be included in the geojson output
  (CRC will not render anything other than Polygon's / Paths inside of an ASDEX file)

- ## Version 2.3.1
  - #92: Corrected ASDEX properties in the geojson output files
  - #93: Default property settings are not included in "feature-level" properties

- ## Version 2.3.0
  - New Feature: vSTARS/vERAM Maps to GeoJson (CRC) format

- ## Version 2.2.0
  - Created GUI for .SCT2 to .KML Conversions
    - Only .sct2 to .kml conversions are possible right now. This feature is still being developed.
  - Error messages will now be displayed to the user that were previously hidden from users by Windows.
  - Fixed Bugs #72 and #73 - AIRAC processing screens.
  - FE-Buddy now runs as 64bit instead of 32bit.

- ## Version 2.1.1
  - Fixed #69 - WX station website was not downloading on certain computers, this has been fixed.
  - Fixed #68 - .id commands using the 3ld will appear before the ones that use the name.
  - Fixed #67 - Checks for '&' characters in all necessary items 
  - Fixed #65 - Changed the landing page button text.
  - Started work on the new feature for SCT2 to KML and vice versa

- ## Version 2.1.0
  - Added Feature - Convert DAT files into SCT2 files.
  - Added Feature - Convert DXF files into SCT2 and vice versa.
  - Fixed uninstall prompt ending message.
  - Changed GUI - New landing page to select the different features available.
  - Fixed #60 - Menu Fonts not displaying correctly.
  - Changed Discord button Name from "Buddies" to "Discord"

- ## Version 2.0.1
  ## FOR USERS:
    - Added #42 - Added button ("Buddies") that links to the FE-BUDDY Discord server.
    - Fixed #52 - Navaid Geomap for vERAM has no data
    - Fixed #55 - Reading SCT2 Fixes in Place of Coordinates
    - Fixed #58 - Changed Uninstall Prompt Message
    - Fixed #54 - .ID alias commands are now added to the AliasTestFile
    - Fixed #49 - Fixed the Star/DP Name Alignment in the SCT Files
    - Fixed #45 - All comments in the SCT file should follow the same format.
  ## FOR DEVELOPERS:
    - Fixed #41 - Menu display Unhandled Exception
    - Backend code cleanup and standardized.
    - Updated Squirrel to latest stable (Squirrel is the auto updater system)
    - Updated/Corrected logging messages
    - Started development on SCT to DXF file conversions.
      - This option is currently disabled as it is not fully functioning.
    - Modified GUI - Different options for future updates.
  
- ## Version 2.0.0
  ### FOR USERS:
    - Modified menu presentation
    - Fixed frequency formatting in [VOR].SCT2; All frequencies will be formatted as xxx.xx
    - Added ASOS/AWOS frequencies to APT_ISR.txt
  ### FOR DEVELOPERS:
    - Conversion from .Net Framework 4.7 to .Net Core 6
    - Connected Squirrel Logging to FE-Buddy Logging
    - Updater now uses Delta Nuget Packages when updating. Install will use Full Nuget Package
    - New landing page and winforms added (commented out) in preparation for future feature releases.
    - Backend code file and function name changes/refactoring
    - Added feature-not-implemented popup
    - Refactored parsing controls (backend)
    - Added aircraft data alias command output (AircraftDataInfo.txt)
      - Commented out until we can figure out how to get updated C/D and SRS data in an automated way
    - Added Error Handling on various functions

---
- ## Version 1.0.3
  - #26 KASE and many other airports were effected by a bug causing some airports and runways to be missing from the Airports.xml file.

- ## Version 1.0.2
  - In the Publications Output, if a procedure is Added (A) or Deleted (D), the link is NOT generated and instead will display a quick explanation. #20
  - Restructured Menu Buttons
  - New Menu button for "Reporting Issues" #23
  - Fixed Bug #22 - Menu Fonts Changing Randomly
  - Fixed Bug #19 - Clearing the FAC ID field
  - Fixed Bug #21 - Browser not opening on Menu Button click

- ## Version 1.0.1
  - The Main Window has a background image instead of a solid color.
  - Changed the Update view background colors to match new main window colors.
  - "Start" and "Exit" Buttons display a different color when clicking them.
  - The color of the Hover state of all buttons has changed.
  - Implemented a simple Logger.
    - This log file will be included in the output folder.
  - Misc. code clean-up and function design changes.

- ## Version 1.0.0
  - New GitHub Repository
  - Name changed from "NASR2SCT" to "FE-BUDDY".
  - GUI Color changed.
  - "Credits" button takes you to a link in the in the GitHub repository.
  - Added Roadmap button.
  - Updated License to GPLv3
  - Fixed compatibility issue with users that use drives other than "C:"
  - Backend code changes such as comments, c# namespaces, and file structures.

