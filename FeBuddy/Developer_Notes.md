**FE-Buddy 3.0 Development Notes**

# GENERAL

- Used to keep track of overall functionality and development notes for FE-Buddy v3.0.

- DevMode bool will need to be created somewhere. When set to true, certain things will happen in the code to make it easier to troubleshoot problems.

- FE-Buddy will use MVVM to keep GUI and functionality separate as much as possible.

- `NetTopologySuite` and `NetTopologySuite.IO.GeoJSON4STJ` NuGet packages will be used for geojson handling unless a significantly more advanced option is discovered.

- Code comments should be used as much as practical to help new coders understand the code and expand this project.

- Visual Studio Summary XML data is favored by this development team.

- Repetative methods/functions should be isolated as helpers or some other class type whenever practical.

- A user-facing function of FE-Buddy such as "AIRAC Data" or "CONVERSIONS>RVM-GeoJson" or "Geojson Health-Check" will be called "Services" and if/when appropriate, will be referred to and organized into Services in the code.
  - In a SERVICES folder, each Service will have its' own service-named folder and a General folder will house code that applies to multiple services such as certain Helpers.
  - Non-Service code will be organized outside of the SERVICES folder.
  - A MODELS folder can be created outside of the SERVICES folder, housing the SERVICES data, if it makes sense to do so. Otherwise, a MODELS folder can be created within the SERVICES folder.

- Geojson files will be output on single-line format in order to cut down on disk space. `DevMode`=true results in pretty-print output of geojsons.

## USER CONFIGURATION FILE
- Description: A JSON file containing saved data concerning preferences and settings, required for effective App-Functionality.
- File Name: `UserConfig.json`
- Read/Write handled by a `FEBuddyLibrary` `UserConfig` Helper.
  - Method: `ReadAll`
  - Method: `Write`
- On initial install, installer will create a UserConfig.json with blank values except:
  - `General`.`LastWindowState`=`Normal`
- On update, the current data will be saved and then rewritten to the new config file appropriatelly.
- New data saved to file when user selects "save" on a settings field.
- Read on initial launch by `UserConfigFile` class and saved to a `UserConfig` global dictionary.
- Structure:
  - General
    - `DefaultOutputDirectory`=""
    - `LastWindowState`=""
    - `NewsLastOpen`=""
    - `UpdateChannel`=""
  - Services
    - CrcAiracResources
      - `AiracCycleId`=""
      - `UserArtccId`=""
      - DefaultRoi
        - `FilterByRoi`=""
        - DefaultCoordindates
          - `SwLat`=""
          - `SwLon`=""
          - `NeLat`=""
          - `NeLon`=""
      - Geojson
        - Airways
          - `OutputBy`=""
          - `BufferAirwayWaypoints`=""
          - `IncludeFebCustomProperties`=""
          - Roi
            - `FilterByRoi`=""
            - `OverrideDefaultRoi`=""
            - OverrideCoordindates
              - `SwLat`=""
              - `SwLon`=""
              - `NeLat`=""
              - `NeLon`=""
          - CrcEramPropertyDefaults
            - `IncludeCrcEramPropertyDefaults`=""
            - Lines
              - Airway_High_Lines
                - `bcg`=""
                - `filters`=""
                - `style`=""
                - `thickness`=""
              - Airway_Low_Lines
                - `bcg`=""
                - `filters`=""
                - `style`=""
                - `thickness`=""
              - Airway_Other_Lines
                - `bcg`=""
                - `filters`=""
                - `style`=""
                - `thickness`=""
            - Symbols
              - Airway_High_Symbols
                - `bcg`=""
                - `filters`=""
                - `style`=""
                - `size`=""
              - Airway_Low_Symbols
                - `bcg`=""
                - `filters`=""
                - `style`=""
                - `size`=""
              - Airway_Other_Symbols
                - `bcg`=""
                - `filters`=""
                - `style`=""
                - `size`=""
            - Text
              - Airway_High_Text
                - `bcg`=""
                - `filters`=""
                - `text`=""
                - `size`=""
                - `underline`=""
                - `xOffset`=""
                - `yOffset`=""
              - Airway_Low_Text
                - `bcg`=""
                - `filters`=""
                - `text`=""
                - `size`=""
                - `underline`=""
                - `xOffset`=""
                - `yOffset`=""
              - Airway_Other_Text
                - `bcg`=""
                - `filters`=""
                - `text`=""
                - `size`=""
                - `underline`=""
                - `xOffset`=""
                - `yOffset`=""
        - DepartureProcedures
          - `OutputBy`=""
          - `IncludeFebCustomProperties`=""
          - `IncludeOverridingStylePropertyByAptType`=""
          - Roi
            - `FilterByRoi`=""
            - `OverrideDefaultRoi`=""
            - OverrideCoordindates
              - `SwLat`=""
              - `SwLon`=""
              - `NeLat`=""
              - `NeLon`=""
          - CrcEramPropertyDefaults
            - `IncludeCrcEramPropertyDefaults`=""
            - Lines
              - `bcg`=""
              - `filters`=""
              - `style`=""
              - `thickness`=""
            - Symbols
              - `bcg`=""
              - `filters`=""
              - `style`=""
            - Text
              - `bcg`=""
              - `filters`=""
              - `text`=""
              - `size`=""
              - `underline`=""
              - `xOffset`=""
              - `yOffset`=""
              - `size`=""
      - AliasFile
        - Airways
          - Roi
            - `FilterByRoi`=""
            - `OverrideDefaultRoi`=""
            - OverrideCoordindates
              - `SwLat`=""
              - `SwLon`=""
              - `NeLat`=""
              - `NeLon`=""
        - DepartureProcedures
          - Roi
            - `FilterByRoi`=""
            - `OverrideDefaultRoi`=""
            - OverrideCoordindates
              - `SwLat`=""
              - `SwLon`=""
              - `NeLat`=""
              - `NeLon`=""
    - Conversions
      - RvmToGeojson
        - `FileSourceType`=""
        - `SourceDirectory`=""
        - `OutputDirectory`=""
        - `CroppingDistance`=""
      - FaaEramToCrcResources
        - `SourceFilesDirectory`=""
        - `OutputDirectory`=""
        - `OutputFormat`=""

# GUI

- Internet Connection
  - Assume `bool hasInternetConnection` from FEBuddyLibrary is `true` until a return of `false` proves otherwise.
  - Grey-out or display/hide data that requires internet connection accordingly, for example: program version number (for update checking) and AIRAC Cycle services.
- Saving Settings
  - Settings-dependent operations will be greyed-out until all required settings are entered with the appropriate data or "saved" to the UserConfig.json
  - If the only thing missing is the user not selecting the "save" button for the newly entered data, an execute button push will launch a window warning the user that the newly input data will be saved before execution and allowing the user to select "Cancel" or "Save & Continue".

## TITLE BAR

- Shows the FE-Buddy app name followed by the current version: `FE-Buddy v3.0.0`
  - If the user chooses not to update to the latest version on startup, change the version number to an attention-grabbing color (`hasInternetConnection` dependent).
- Tooltip on hover over the version number (`hasInternetConnection` dependent):
  - `You are running the latest version.`
  - `vX.X.X available! Go to SETTINGS > UPDATES.`

## SETTINGS

### UPDATES

- Allow users to select:
  - Participate in `alpha`, `beta`, or `stable only` version updates.
    - `stable only` is selected by default.
	- Saved as `General`.`UpdateChannel` in config file.
  - Rollback from an alpha or beta version to the latest stable version.
  - Check for updates now (`hasInternetConnection` dependent).
- Save button:
  - Writes settings to the `UserConfig` file.

### UNINSTALL

- User selects to start the uninstall process.
- Warning window should appear informing the user that their user settings will be lost and a "Cancel" or "I understand, please uninstall" options should be provided.

### WINDOW STATE

- User selects how they want the application to always launch.
  - `Normal`, `Minimized`, and `Maximized`
  - `Normal` should be default and written to userconfig on initial install.

## INFO

- Submenus:
  - `About`
    - Opens a small window summarizing FE-Buddy.
	- Aspects such as "Efficient Linestring Handling" and "FE-Buddy custom Geojson Properties" will be discussed here.
  - `Change Log`
    - Opens the latest change log from a GitHub link.
  - `Manual`
    - Opens the FE-Buddy Manual page in a web browser (likely a GitHub Markdown page or website).
  - `Discord`
    - Opens a page describing the Discord server and provides an invite link to the FE-Buddy Discord server.

## NEWS

- Upon opening FE-Buddy, check the latest news PostID. This logic should be in the library, not the GUI (`hasInternetConnection` dependent).
  - If the latest PostID is newer than the saved `NewsLastOpen` ID/date/time, the News icon should indicate that a new News post is available via text and/or by changing the icon color to grab the user's attention.
- News button opens the FE-Buddy News page in a web browser (likely a GitHub Markdown page or website).
  - If `hasInternetConnection`=true, parse the New.md and save the latest news PostID to the `UserConfig.json` when opened.
- If failed to parse News.md, leave last `NewsLastOpen` as is and provide a warning that News.md could not be parsed.
- Example News.md code:
```md
# FE-Buddy News
<!--
PostId format =  yyyy-mm-dd.#
  - Date/Time is always Zulu (GMT) time.
  - # = sequential number for the number of posts this day. The First post of the day is 1, while the third post is 3.
-->

News concerning all things FE-Buddy will be posted here with the most recent post at the top.

---

## 2026-08-30
<!--
PostId: 2026-08-30.3
-->

**Version 1.4.2 Released**

Third post for the same day.

---

## 2026-08-30
<!--
PostId: 2026-08-30.2
-->

**Version 1.4.1 Released**

Second post for the same day.

```

### SERVICES SECTION

#### CRC AIRAC RESROUCES

- If `UserConfig.DefaultRoi.FilterByRoi`=empty, grey-out this service and have a tooltip pop up advising them to navigate to SETTINGS > DEFAULT ROI and complete that form.
  - Once user completes the form, a trigger will result in GUI reading the config file again and this service should be available again.
- User selects Previous, Current, or Next AIRAC Cycle with the effective date displayed next to it.
  - Note: FE-Buddy will save the unzipped airac downloaded data for up to 3 airac cycles in the appdata, the previous cycle, current, and next/preview cycle. On launch, that appdata folder is checked and the cycles that are older than one cycle back is deleted while the previous, current, and next cycle are downloaded if not already exists.
- User types their ARTCC ID (consider drop menu)
- User selects output directory
- Create AiracSettings dictionary to be passed to Library later.
  - Add GeneralSettings after user saves on this general page.

#### DEFAULT ROI

- Info section:
  - `Region of Interest (ROI): An lat/lon axis-aligned rectangular region defined by southwest (bottom-left corner) and northeast (top-right corner) coordinates (i.e. a box defining the data you are interested in). Depending on the data type and operation, geometries may be clipped to the ROI or included in full when associated with an entity located within the ROI. Create a box that encompasses an acceptable amount of area outside your ARTCC boundaries so that data within that region may still be displayed in your GeoJSON files and, under certain circumstances, in additional resource files. Note: Depending on the operation, you may be given the option to override this ROI with a custom ROI for specific files later.`
- User selects:
  - `Setup and use ROI` (Selected by Default if `UserConfig`.`AiracData`.`DefaultRoi`.`FilterByRoi` has no value)
    - If appropriate data is already detected for `UserConfig`.`AiracData`.`DefaultRoi`.`DefaultCoordindates`, label should just be "Use ROI"
  - `Do not set up ROI; Get all NASR data.`
- If user selects to setup ROI or loads as default or from previous UserConfig preferences, ROI Coordinates Input boxes:
  - `Northeast (top-right corner) Latitude`
  - `Northeast (top-right corner) Longitude`
  - `Southwest (bottom-left corner) Latitude`
  - `Southwest (bottom-left corner) Longitude`
  - If `IncludeRoi`=true
    - If `UserConfig.json`.`General`.`Settings`.`Roi`.`DefaultCoordindates` has values
	  - Load the values into the input boxes.
	  - If values do not exist, provide greyed examples of lat/lon coordinates in the boxes ready for the user to input theirs.
- Save button:
  - Set `UserConfig.json`.`General`.`Settings`.`Roi`.`IncludeRoi`=`true/false`
  - If `IncludeRoi`=true
    - Ensure all `DefaultCoordindates` have values.
    - Send the `DefaultCoordindates` to `FEBuddyLibrary`.`GuiProcessHandler`.`ValideateRoiCoordinates` to validate:
      - Valid decimal values (`IsCoordinateValidFormat`)
      - SW `DefaultCoordindates` are actually southwest of the NE `DefaultCoordindates`. (`IsCoordinatesRelativePositionValid`)
    - Await for validation process to complete with success and then if `IncludeRoi`= `true`
      - Set `UserConfig.json`.`General`.`Settings`.`Roi`.`DefaultCoordindates`
        - .`SwLat`
        - .`SwLon`
        - .`NeLat`
        - .`NeLon`
  - Await for above to complete and then trigger re-read `UserConfig.json` to allow things like services to be selected that were previously unavailable due to a setting form not being filled out yet.

##### AIRWAYS

###### GEOJSON FILES

- Description area:
  - `Airway data from the FAA NASR .csv files may be used to generate Gojson files for ERAM maps along with alias file commands (example: .<airwayId>F).`
  - `One airway per Geojson Feature.`
  - `Airways with breaks/gaps will be constructed within the same Feature but will utilize MultiLineString to properly handle breaks in LineStrings.`
- User selects Geojson Output by:
  - `None`
    - Tool tip or description:
	  - `Geojson files will not be generated from Airway data.`
  - `High/Low` (default)
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
  - `Create a radius around airway waypoints? (2.5nm around 5 character fixes, 5nm around others such as NAVAIDS)`
	- `Y` or `N` (default `N`)
- User selects:
  - `Include the Airway IDs in the FE-Buddy Custom Properties?`
	- `Y` or `N` (default `N`)
- Collect, convert and add to `AiracSettings` > `AirwaysSettings`
  - `AirwayGeojsonOutputBy`
    - `None`
    - `HighLow`
    - `Designation`
  - `BufferAirwayWaypoints`
    - `Y` or `N` converted to `true` or `false`
  - `IncludeFebCustomProperties`
    - `Y` or `N` converted to `true` or `false`
- ROI Override
  - ???
- CRC Properties
  - ERAM
    - ???
  - STARS
    - ???
- Upon SAVE
  - `ValidateCrcGeojsonProperties` values against [THESE](https://github.com/KCSanders7070/CRC_GeoJson_Concepts/blob/main/CRC_Geojsons.md) values.
  - Show error message/color for values outside of range or validity.
  - Once validated, add `AirwaysSettings` to `AirwaysSettings`
```cs
["AirwaysSettings"] = new Dictionary<string, object>
{
	["AirwayGeojsonOutputBy"] = HighLow,
	["BufferAirwayWaypoints"] = true,
	["IncludeFebCustomProperties"] = true
},
```

###### ALIAS FILES

- Description area:
  - `Airway data from the FAA NASR .csv files may be used to generate Alias commands (example: .<airwayId>F .ff <all airway waypoint IDs>).`

---


# CODE LIBRARY

- Geojsons
  - Output without indenting (single line output to save space)
  - Custom properties, if included in the output, Field Names will be prefixed with "feb." to reduce conflicts with other programs.
    - Example: `feb.AwyId`
  - Properties Field Names should be double-quoted to reduce issues with geojson readers, especially with custom FEB properties having a point in the field name.
    - Example: `"feb.AwyId"`

## LAUNCH PROCESSES

### READ UserConfig.json

- Read/Write handled by: `FEBuddyLibrary`.HELPERS.`UserConfigFile`
  - Method: .`ReadAll`
    - Reads file and writes to a `UserConfig` dictionary
  - Method: .`Write`
    - Reads `UserConfig` dictionary, writes `UserConfig.json`, runs the `ReadAll` to get latest info into the `UserConfig` dictionary
  - Method: .`GetValue`
    - Method recieves address of the desired value, runs the `ReadAll` to get latest info from `UserConfig` dictionary, and then returns value or throws error.

### DATE/TIME + INTERNET CHECK
 
- Get date / time on launch to ensure we calculate airac cycle correctly.
- Get UTC time and base everything off of that to ensure consistency.
- Use a realiable API like [THIS](https://timeapi.io/api/Time/current/zone?timeZone=UTC)
  - If API failed, we can fallback to getting the date/time header from a reliable server provider such as CloudFlare or Google.
- User could have incorrect/corrupt date/time settings on their computer so we should only use that as a fall-back.
  - If the API and header call fails, use users computer date/time and deal with any issues that arrise from that later.
- Use a failure to get Date/Time from API or fallback as a trigger to a "Check Internet Status" process.
  - Outside of this process, set a `bool hasInternetConnection` as true and is only set to false after this process ends with a failure.
- GUI needs `hasInternetConnection` result after process completion.

### FE-BUDDY VERSION

- On launch or when "check updates now" is signaled
  - Consider allowing user option to check only once every 12hrs vs on every start
- Utilizes GitHub API
- Any checks against versioning number policy and Wix compliance
- GUI needs results after process completion.

### LATEST NEWS POST

- Check for latest post PostId and compare against `UserConfig.json`.`General`.`Settings`.`News`.`NewsLastOpen`
  - If the latest news post is newer than the saved `NewsLastOpen`, the News icon should indicate that a new News post is available via text and/or by changing the icon color to grab the user's attention.
- GUI needs result after process completion.
```cs
using System.Text.RegularExpressions;

public static class NewsChecker
{
    public static int CheckForNews(string newsPath)
    {
        try
        {
            string? NewsLastOpen =
                UserConfig.GetValue("General.Settings.News.NewsLastOpen");

            List<NewsPostId> posts = GetNewsPosts(newsPath);

            // Empty NewsLastOpen means the user has never checked News.
            if (string.IsNullOrWhiteSpace(NewsLastOpen))
            {
                return posts.Count;
            }

            // Invalid stored PostId.
            if (!TryParsePostId(NewsLastOpen, out NewsPostId lastSeen))
            {
                return -1;
            }

            return posts.Count(post => post.CompareTo(lastSeen) > 0);
        }
        catch
        {
            // Error reading/parsing News.md, etc.
            return -1;
        }
    }

    private static List<NewsPostId> GetNewsPosts(string newsPath)
    {
        string markdown = File.ReadAllText(newsPath);

        // PostId Format: yyyy-mm-dd.#
		// 		# = Indicates the sequence number for the post that day.
		// 		For example, the thrid post on 30AUG2026 would be: 2026-08-30.3
		Regex postIdRegex = new(
            @"PostId:\s*(\d{4}-\d{2}-\d{2})\.(\d+)",
            RegexOptions.IgnoreCase);

        List<NewsPostId> posts = new();

        foreach (Match match in postIdRegex.Matches(markdown))
        {
            string postIdText =
                $"{match.Groups[1].Value}.{match.Groups[2].Value}";

            if (!TryParsePostId(postIdText, out NewsPostId postId))
            {
                return new List<NewsPostId>();
            }

            posts.Add(postId);
        }

        return posts;
    }

    private static bool TryParsePostId(
        string value,
        out NewsPostId postId)
    {
        postId = default;

        Match match = Regex.Match(
            value.Trim(),
            @"^(\d{4}-\d{2}-\d{2})\.(\d+)$");

        if (!match.Success)
            return false;

        if (!DateOnly.TryParseExact(
                match.Groups[1].Value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly date))
        {
            return false;
        }

        if (!int.TryParse(
                match.Groups[2].Value,
                out int sequence))
        {
            return false;
        }

        if (sequence < 1)
            return false;

        postId = new NewsPostId(date, sequence);
        return true;
    }

    private readonly record struct NewsPostId(
        DateOnly Date,
        int Sequence) : IComparable<NewsPostId>
    {
        public int CompareTo(NewsPostId other)
        {
            int dateComparison = Date.CompareTo(other.Date);

            if (dateComparison != 0)
                return dateComparison;

            return Sequence.CompareTo(other.Sequence);
        }
    }
}
```

## GUI PROCESS HANDLERS

### VALIDATE ROI COORDINATES

- `ValideateRoiCoordinates`
  - `IsCoordinateValidFormat`
  - `IsCoordinatesRelativePositionValid`

## SERVICES

- Helpers
  - Will need a `CrcEramPropertyHandler` class 
    - Method: `CreateDefault`
      - Values are passed in and a Geojson Point Feature is created as a CRC ERAM `is*Default` and returned for the FeatureCollection.
    - Method: `CreateFeatureProperty`
      - Values are passed in and a Geojson properties section is created and returned for the feature, also known as a CRC ERAM `Overriding Property`.

### AIRAC DATA

#### AIRWAYS

- General
  - Efficient Line String Handling
  - Output is single-Line

- Output By:
  - `None`
    - continue
  - `High/Low`
	- `Airways_High.geojson
	  - Airways that have a Maximum Authorized altitude of 18,000' or greater
	- `Airways_Low.geojson
	  - Airways that have a Maximum Authorized altitude greater than 0' but less than 18,000'
	- `Airways_Other.geojson
	  - Airways that do not meet the criteria of High/Low.
  - `Designation`
    - `Airways that share the same designation will be placed in the same file.`
      - `Examples:`
        - `Airways_J.geojson`
        - `Airways_V.geojson`
        - `Airways_AT.geojson`
- FEB Custom Properties
  - If `includeCustomProperties`==True
    - Each feature will including the `AwyId` (one AwyId per feature)
	  - "feb.AwyId": `AwyId`
- User selects:
  - `Do you wish to include FE-Buddy Custom Properties, providing the airway ID in the each feature?`
    - yes no option