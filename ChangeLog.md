---
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
  - New Github Repository 
  - Name changed from "NASR2SCT" to "FE-BUDDY".
  - GUI Color changed.
  - "Credits" button takes you to a link in the in the github repository. 
  - Added Roadmap button.
  - Updated License to GPLv3
  - Fixed compatibility issue with users that use drives other than "C:"
  - Backend code changes such as comments, c# namespaces, and file structures. 

