# FE-Buddy 3.0 Development Notes

Used to keep track of overall functionality and development notes for FE-Buddy v3.0.

## GUI

### TITLE BAR

- Shows the FE-Buddy app name followed by the current version: `FE-Buddy v3.0.0`
  - If the user chooses not to update to the latest version on startup, change the version number to an attention-grabbing color.
- Tooltip on hover over the version number:
  - `You are running the latest version.`
  - `vX.X.X available. Go to SETTINGS > UPDATES.`

### SETTINGS

#### Updates

- Allow users to select:
  - Participate in `alpha`, `beta`, or `stable only` version updates.
    - `stable only` is selected by default.
  - Roll back from an alpha or beta version to the latest stable version.
  - Check for updates now.
  - Save button:
    - Writes settings to the `userconfig` file.

#### Default Region of Interest (ROI)

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
  - Change Log
    - Opens the latest change log from a GitHub link.
  - Manual
    - Opens the FE-Buddy Manual page in a web browser (likely a GitHub Markdown page or website).
  - Discord
    - Opens a page describing the Discord server and provides an invite link to the FE-Buddy Discord server.

### NEWS

- Opens the FE-Buddy Manual page in a web browser (likely a GitHub Markdown page or website).
- Save the latest news post ID (date/time, maybe?) to the `userconfig` file when opened.
- Upon opening FE-Buddy, check the latest news post ID. This logic should be in the library, not the GUI.
  - If the latest news post is newer than the saved `lastNewsOpen` date/time, the News icon should indicate that a new News post is available via text and/or by changing the icon color to grab the user's attention.