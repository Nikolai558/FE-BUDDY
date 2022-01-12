## Steps to release and push out a new update.

1. Change program Assembly info inside of Visual Studios.
	* This is located in FeBuddyWinFormUI under Properties
2. Change Program version info inside of GlobalConfig.
	* This is located in FeBuddyLibrary
3. Clean Solution, Build Solution
	* Be sure Releases is checked and NOT debug.
4. Publish program with Visual Studio
	* With FeBuddyWinFormUI selected, go to Build > Publish Selection in the top menu
    * Create a new/use an existing Folder profile with the following settings
        + Configuration - Release | Any CPU
        + Target Framework - net6.0-windows
        + Deployment Mode - Self-contained
        + Target Runtime - win-x86
        + Produce Single File - Yes
        + Enable ReadyToRun Compression - No
    * Publish using this profile
5. Pack using Squirrel
    * Download and extract the ZIP archive from the latest release of [Clowd.Squirrel](https://github.com/clowd/clowd.squirrel)
    * Move the entire `FeBuddyWinFormUI\bin\Release\net6.0-windows\publish\Releases\win-x86` to the folder you extracted the ZIP archive in
    * Run the following command in PowerShell while your working directory is the folder you extracted the ZIP archive in
        + `.\Squirrel pack --packName "FE-BUDDY" --packVersion "1.1.0" --packAuthors "Kyle Sanders" --packDirectory win-x86`. I have the author set to Kyle Sanders because of the current Start Menu folder.
6. Go to GitHub
    * Publish source code on the DEVELOPMENT branch
        + NEVER PUBLISH SOURCE CODE TO RELEASE BRANCH. THIS MUST BE DONE THROUGH A MERGE.
    * Create a pull request for RELEASE BRANCH
    * Merge DEVELOPMENT into the RELEASE Branch.
7. Update Change Log
    * Go to GitHub / Changelog and update for new release.
8. Ready to publish the release
    * Go to Releases on GitHub, draft new release.
    * Put in the info.
    * Upload the FE-BUDDYSetup.exe found in the Releases folder where you extracted the ZIP archive, as well as the .nuget package.
    * Publish the release.
