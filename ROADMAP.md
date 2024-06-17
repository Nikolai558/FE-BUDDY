# FE-BUDDY ROADMAP


**WILL BE INCLUDED IN v3.0:**

- [ ] New UI
  - Utilizing a more robust system (WPF).
  - Will include a larger library of saved user-preferences.
  
- [ ] AIRAC Update
  - Generally the same as v2.x with these improvements:
     - Provides the user more control over which files are output & what each file will be named.
     - Only includes the data within a user-defined bounding-box.
     - Includes DP/STAR Fix Symbols and Text as an optional output file.
  
- [ ] FAA RADAR Video Maps to GeoJSON Conversion (.dat)
  - Generally the same as v2.x with these improvements:
     - Allows the user to select more than one file to be converted at a time.
	 - Option for user to define their own Point of Tangency (POT) rather than taking from the file.
	 - Persistent Distance Setting: UI retains the last entered distance setting throughout the session.
  
- [ ] SCT2 to GeoJSON Converstion
  - Same as v2.x.
  
- [ ] FAA RADAR Video Maps Conversion (.kml/kmz)
  - Generally the same as v2.x except will now allow the user to select more than one file to be converted at a time.

---

**UNDER CONSIDERATION FOR POST-v3.0:**
  
- [ ] FAA RADAR Video Maps to GeoJSON Conversion (.kml/kmz)
  - Ability to convert Google Earth Pro files from FAA.
  
- [ ] FAA GeoMap to GeoJSON Conversions
  - Ability to convert GeoMap files from FAA.
  - Multiple Export Options: Users have the flexibility to choose from various export options, depending on their specific needs:
     - GeoMap Object Description: Users can export data based on GeoMap object descriptions. The output file names reflect these descriptions, with any illegal characters replaced by underscores for clarity.
     - Filter Assignment: Alternatively, users can choose to export data based on filter assignments. The output file names include the filter name and the assigned filter number in a 2-digit format, ensuring an organized data structure.
     - Identical Attributes: The feature offers the option to export data based on identical attributes. The output file names include a string of assigned attributes, separated by double underscores, providing a comprehensive and detailed dataset.
  - Log Files: To keep users informed, the feature generates log files in .txt format for each conversion. These log files provide users with valuable insights into the exported data, ensuring transparency and data integrity.
  
- [ ] Create GeoJSON
  - This feature allows users to build GeoJSON files from scratch, providing full control over the data they want to include in their simulations. Users can define and customize various elements to meet their specific requirements.
  
- [ ] GeoJSON Cleanup
  - GeoJSON Selection: Users can start by selecting a GeoJSON file that they intend to use with CRC. This file serves as the basis for customization and cleanup.
  - UI-Based Default Values: The feature provides a user-friendly interface that allows users to input default values for CRC. These default values can be adjusted to match the specific settings and preferences required for CRC compatibility.
  - Code Cleanup: GeoJSON Cleanup goes beyond the basic setup. It analyzes the GeoJSON code and performs several cleanup tasks to ensure optimal compatibility with CRC:
     - Code Cascading: The feature condenses the GeoJSON code into a single line, simplifying the data structure for CRC.
     - Feature Combination: GeoJSON Cleanup identifies instances where multiple features could be effectively combined. For example, it recognizes line strings that consist of individual segments but could be combined into a single line string collection, enhancing the efficiency of CRC data representation.
  
- [ ] Procedure ISR (In-Scope Reference):
  - The Procedure ISR, short for "In-Scope Reference," is a feature designed to serve as a quick-reference tool for air traffic controllers, providing guidance on the specific actions and instructions to be issued during each phase of flight and procedure. It simplifies the management of procedures, making them easily accessible and customizable.
  - Key Features:
     - Data Collection from d-TPP MetaFile: FEB collects procedure data from the d-TPP (Digital Terminal Procedures Publication) MetaFile, specifically for every airport listed as the responsibility of the selected ARTCC (Air Route Traffic Control Center). This data retrieval process aims to provide comprehensive information for controllers.
     - Pre-Filled Information: The feature leverages automation to pre-fill available information related to each procedure, ensuring accuracy and consistency. This reduces manual data entry and speeds up the reference creation process.
     - Interactive Spreadsheet Display: Users are presented with a spreadsheet-like interface, allowing them to review and select the fields and information they wish to include in the in-scope reference.
     - Customization: FEB enables users to customize their reference by deselecting fields they do not want to include. This ensures that the reference is tailored to the specific needs and preferences of each controller.
     - Guided Interactive Setup: For any fields that couldn't be pre-filled, FEB guides the user through an interactive setup process. This approach ensures that all necessary information is included in the reference, even for elements that require manual input.
     - Progress Tracking: To provide situational awareness, the feature includes a progress tracker. This tracker informs the user of their current status, displaying how much work has been completed and how much remains, ensuring efficient and goal-oriented data entry.
     - Data Saving: Procedure ISR supports saving progress, either to a temporary file or a configuration file. This feature allows users to work on the reference over an extended period, make updates per AIRAC cycles, or revisit and review their data. Saving to a configuration file also facilitates change notifications and automated data management.

---

**v2.x FEATURES THAT WILL NOT BE INCLUDED IN v3.0+:**

- [ ] SCT2 Conversions
  - Other than to GeoJSON.
  - Limited support will be provided for this feature, as we are only keeping it so that neighboring non-US facilities can send FE's their data and it be converted as needed.

- [ ] vSTARS/vERAM and AutoCAD Conversions
  - The current state of the code in v2.x for this is a bit of a mess and we have decided that it is not worth it to convert it over for something that should not be needed anymore.
  - Users will still be able to download the last update to v2.x and utilize that feature if absolutely needed.
