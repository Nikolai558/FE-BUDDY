# CHANGELOG

---
- ## Version 2.1.1
  - Created GUI for .SCT2 to .KML Conversions
    - NOTE: Only .sct2 to .kml conversions are possible right now. This feature is still being developed.
  - Error messages will now be displayed to the user that were previously hidden from users by Windows.
  - Fixed Bugs #72 and #73 - AIRAC processing screens.

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

