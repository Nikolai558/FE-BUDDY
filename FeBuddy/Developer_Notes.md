# FE-Buddy 3.0 Development Notes

Used to keep track of overall functionality and development notes for FE-Buddy v3.0.

## GUI

- Internet Connection
  - Assume `bool hasInternetConnection` from FEBuddyLibrary is `true` until a return of `false` proves otherwise.
  - Grey-out or display/hide data that requires internet connection accordingly, for example: version numbers and AIRAC Cycle services.

### TITLE BAR

- Shows the FE-Buddy app name followed by the current version: `FE-Buddy v3.0.0`
  - If the user chooses not to update to the latest version on startup, change the version number to an attention-grabbing color (`hasInternetConnection` dependent).
- Tooltip on hover over the version number (`hasInternetConnection` dependent):
  - `You are running the latest version.`
  - `vX.X.X available! Go to SETTINGS > UPDATES.`

### SETTINGS

#### Updates

- Allow users to select:
  - Participate in `alpha`, `beta`, or `stable only` version updates.
    - `stable only` is selected by default.
  - Roll back from an alpha or beta version to the latest stable version.
  - Check for updates now (`hasInternetConnection` dependent).
  - Save button:
    - Writes settings to the `userconfig` file.

#### Default ROI

- Info section:
  - `Region of Interest (ROI): A rectangular geographic region defined by southwest (bottom-left corner) and northeast (top-right corner) coordinates. Depending on the data type and operation, geometries may be clipped to the ROI or included in full when associated with an entity located within the ROI. Create a box that encompasses an acceptable amount of area outside your ARTCC boundaries so that data within that region may still be displayed in your GeoJSON files and, under certain circumstances, in additional resource files. Note: Depending on the operation, you may be given the option to override this ROI with a custom ROI for specific files later.`
- User input boxes:
  - `Southwest (bottom-left corner) Latitude:` — `user-input box showing example of lat`
  - `Southwest (bottom-left corner) Longitude:` — `user-input box showing example of lon`
  - `Northeast (top-right corner) Latitude:` — `user-input box showing example of lat`
  - `Northeast (top-right corner) Longitude:` — `user-input box showing example of lon`
- Consider including a graphic of a box with the input areas positioned near the bottom-left and top-right corners of the generic box.
- Save button:
  - Upon action, validates input:
    - Coordinates are valid decimal values.
    - SW coordinates are actually southwest of the NE coordinates.
  - After validation, saves the values to the `userconfig` file.

### INFO

- Submenus:
  - About
    - Opens a small window summarizing FE-Buddy.
	- Aspects such as "Efficient Linestring Handling" and "FE-Buddy custom Geojson Properties" will be discussed here.
  - Change Log
    - Opens the latest change log from a GitHub link.
  - Manual
    - Opens the FE-Buddy Manual page in a web browser (likely a GitHub Markdown page or website).
  - Discord
    - Opens a page describing the Discord server and provides an invite link to the FE-Buddy Discord server.

### NEWS

- Opens the FE-Buddy News page in a web browser (likely a GitHub Markdown page or website).
- If `hasInternetConnection` true, save the latest news post ID (date/time, maybe?) to the `userconfig` file when opened.
- Upon opening FE-Buddy, check the latest news post ID. This logic should be in the library, not the GUI (`hasInternetConnection` dependent).
  - If the latest news post is newer than the saved `lastNewsOpen` date/time, the News icon should indicate that a new News post is available via text and/or by changing the icon color to grab the user's attention.

#### SERVICES SECTION/MENUS

##### AIRAC CYCLE

- User selects current or preview AIRAC Cycle with the effective date displayed next to it.
- User types their ARTCC ID (consider drop menu)

###### Airways

####### Geojson Files
- Description area:
  - `Airway data from the FAA NASR .csv files may be used to generate Gojson files for ERAM maps along with alias file commands (example: .<airwayId>F).`
  - `One airway per Geojson Feature.`
  - `Airways with breaks/gaps will be constructed within the same Feature but will utilize MultiLineString to properly handle breaks in LineStrings.`
- User selects Geojson Output by:
  - `None`
    - Tool tip or description:
	  - `Geojson files will not be generated from Airway data.`
  - `High/Low`
    - Tool tip or description:
	  - `Airways_High.geojson = Airways that have a Maximum Authorized altitude of 18,000' or greater`
	  - `Airways_Low.geojson = Airways that have a Maximum Authorized altitude greater than 0' but less than 18,000'`
	  - `Airways_Other.geojson = Airways that do not meet the criteria of High/Low.`
  - `Designation`
    - Tool tip or description:
	  - `Airways that share the same designation will be placed in the same file.`
	    - `Examples:`
		  - `Airways_J.geojson`
		  - `Airways_V.geojson`
		  - `Airways_AT.geojson`
- User selects:
  - `Include the Airway IDs in the FE-Buddy Custom Properties?`
    - yes no option
- User selects:
  - `Do you wish to include FE-Buddy Custom Properties, providing the airway ID in the each feature?`
    - yes no option

####### Alias Files
- Description area:
  - `Airway data from the FAA NASR .csv files may be used to generate Alias commands (example: .<airwayId>F .ff <all airway waypoint IDs>).`


---


## CODE LIBRARY

### LAUNCH PROCESSES

#### Read `userConfig` file

- Read and load `userConfig` file data into appropriate dictionary.

#### Date/Time + Internet Check
 
- Get date / time on launch to ensure we calculate airac cycle correctly.
- Get UTC time and base everything off of that to ensure consistency.
- Use a realiable API like [THIS](https://timeapi.io/api/Time/current/zone?timeZone=UTC)
  - If API failed, we can fallback to getting the date/time header from a reliable server provider such as CloudFlare or Google.
- User could have incorrect/corrupt date/time settings on their computer so we should only use that as a fall-back.
  - If the API and header call fails, use users computer date/time and deal with any issues that arrise from that later.
- Use a failure to get Date/Time from API or fallback as a trigger to a "Check Internet Status" process.
  - Outside of this process, set a `bool hasInternetConnection` as true and is only set to false after this process ends with a failure.
- GUI needs `hasInternetConnection` result after process completion.

#### Version

- On launch or when "check updates now" is signaled
  - Consider allowing user option to check only once every 12hrs vs on every start
- Utilizes GitHub API
- Any checks against versioning number policy and Wix compliance
- GUI needs results after process completion.

#### Latest News Post

- Check for latest post date/time ID and compare against `userconfig` file
  - If the latest news post is newer than the saved `lastNewsOpen` date/time, the News icon should indicate that a new News post is available via text and/or by changing the icon color to grab the user's attention.
- GUI needs result after process completion.

### GUI PROCESS HANDLERS

#### 