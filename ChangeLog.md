
## Pre-V1.0.0
---
- ## Version 0.8.3
  - Cleaned up Backend Code including Typos and Folder/File Renames, #126
  - If user has cURL, it will be used to download the FAA Meta, Telephony, and WX Station files. If the user does not have it, it will still use WebClient for downloading.
    - Fixes #131, #129, #130
    - Reduces File Download Time. 
  - New system to get the WX Station data. NOAA NWS (National Oceanic and Atmospheric Administration's National Weather Service) is used to gather ALL WX Station Data, #104
  - Fixed #132 Random Incorrect Coordinates. 
  - Added App Manifest. This should account for verifing Windows Support and Windows Scaling #128

- ## Version 0.8.2
  - Fixed bugs #120 and #118. There was an update to the telophony website we use. This broke NASR2SCT
  - Modified backend code to account for HTML updates from various websites NASR2SCT uses.

- ## Version 0.8.1
  - Change log on Update question will display all changes from user version and up to the latest release. 
  - FAQ button added. This will take you to a Facts And Questions google slide.
  - Choosing AIRAC cycle phrase/question changed.
  - Fixed bug where program crashes if the user is not connected to the internet. 
  - Fixed bug where program states it's complete when it really is not. This happened when a file download failed.
  - Fixed Credit screen title. 
  - New Functionality to FAA Chart Recal Commands
      - Charts without computer codes will be generated with the first 5 characters (not including spaces) of the chart name. 
  - Gather WX Station data from Vatsim
      - This may cause issues, if so please report in issues section on github. 

- ## Version 0.8.0
  - Fixed Bug #98 META File Causing Crash:
    - If Meta File is not available, NASR2SCT will put a WARN-README.txt file in output directory.
  - Testing Update Function ( For Release of 1.0.0 ) 

- ## Version 0.7.9:
  - 0.7.8 was packaged incorrectly and would not allow fresh install of 0.7.8

- ## Version 0.7.8:
  (WARNING) *** FRESH INSTALL OF THIS VERSION DOES NOT WORK
  - Fixed Facility Id Drop Down Alignment #92
  - Duplicate Airway Alias Commands are now put in the 'OTHER' section of the Duplicate Alias Commands Text file. #94 and #81
  - Added Dialog message box with more information when a user is updating NASR2SCT #93

- ## Version 0.7.7:
  - NEW FEATURE, ARTCC Publication Parser. #83
  - NEW FEATURE, Telephony Alias Commands. #87
  - Uninstall button works properly. #80
  - Facility ID has a function and has been re-enabled.
    - If you leave it as "FAA", ALL ARTCC Publications will be parsed.
    - If you select your individual ARTCC, only the publications for that ARTCC will be parsed.
  - Deleted Job links are ignored. #84
    - This only effects when the FAA removes data. (Chart Recall Alias Commands)
  - Removed "Run Again" button. #86
  - "Checking Alias" (Duplicate Alias Commands) is now twice as fast. #89
  - Default Output directory is the User's Desktop. #90

- ## Version 0.7.6:
  - Uninstall Button Functional #73
  - Fixed Uninstall Hieroglyphics #77
  - Output Duplicate Alias Commands Text File #78
  - NOTE: UPDATING FROM 0.7.5 to 0.7.6 will NOT WORK. Manual installation of 0.7.6 is REQUIRED! 

- ## Version 0.7.5:
  - Now Display the Output Directory #69
  - Uninstall Button ( WORK IN PROGRESS - NOT FUNCTIONAL YET) #73
  - Fixed Chart Recall Commands #75
    - The AIRAC Cycle was Hardcoded in for debugging purposes. Correct Chart Recall Link is now exported.
  - When an Update is available, and the user selects "yes", a message will be displayed telling the user the update is processing. #56

- ## Version 0.7.4:
  - ODP procedures now have the IATA in the command (.{apt iata}{computer code}C)
  - Downloads All Required Files Together
  - Unzips All Required Folders Together
  - Clears %temp%\NASR2SCT directory when "Start" button is clicked. Then continues to Downloading Files.
	  - Previously the %temp%\NASR2SCT directory was cleared before program starts, and after it finishes.
  - IAP Chart Recall Multiple Pages Bug #70 fixed.
    - Only insert page number if IAP has more than 1 page.
    - Normal IAP command with (page #)C {.(apt iata)(scratch pad code)(page #)C}
  - IAP Variant Naming Conventions Bug #71
	  - If there are multiple IAP's in one chart (i.e. "ILS Y *OR* LOC/DME RWY 06" ; "VOR *OR* GPS-B") and one of them has a variant but the other does not, the scratch pad code will be as if both of them had the same variant.

- ## Version 0.7.3:
  - Fixed bug #67: Chart Recall Alias Commands
  - "Processing Update" Screen Will no longer show. (attempting to fix auto updater)

- ## Version 0.7.2:
  - DP and STAR line draw correctly for the individual Airport. #63
  - '&' and '"' are replaced with a '-' in GeoMap Text Files. #61
  - TestAliasFile.txt located in ALIAS output folder has all output alias files combined. 
  - Clean up Temp Folder right after program starts and finishes #62
  - Only Operational Airports are drawn with AIRPORT_TEXT_GEMAP and AIRPORT_GEOMAP. #64

- ## Version 0.7.1
  - Bug #55 Chart Recall with RNP and L/R/C Designator Fixed
  - Outputs Apt Text GeoMap #58
  - Outputs Nav Text GeoMap #59
  - Draw fix commands are now Airport Specific #57
  - Draw line diagrams are now Airport Specific 
      (known issue: individual airport diagrams will draw ALL lines, instead of what is associated with that airport)
  - Chart Recall are now Airport Specific #57

- ## Version 0.7.0
  - Spelling Correction
  - "Change Log" Menu Font Fixed.

- ## Version 0.6.9
  - New update available screen.
  - Facility ID input box is disabled until more functionality is available.
  - Added change log menu.
  - FAA Chart Recall Alias Commands now included with output data.
  - Program will start in the center of Users Current screen.
  - Update screen, Credits Screen, and Main Screen does not allow Maximizing windows.

- ## Version 0.6.8
  - Fixed bug #40
  - Now able to move screen after user clicks start. 

- ## Version 0.6.7
  - Testing Auto Updater

- ## Version 0.6.6
  - Spelling Fixes
  - After update, program will make sure the .exe file is there before trying to call it.

- ## Version 0.6.5
  - Custom font for menu bar fixed.
    - Some users did not have the font and it was causeing the characters to invalid.

- ## Version 0.6.4
  - Added Icon/Logo
  - Added Instructions and Credits Menu
    - Instructions will take you to PowerPoint with more information.
    - Credits will open a new window displaying Developer and Credit information
  - Facility ID will remove any spaces entered in.
  
- ## Version 0.6.0 thru 0.6.3
    - Testing Auto Updater
