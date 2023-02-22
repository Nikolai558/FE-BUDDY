## Steps to release and push out a new update.
1. Change Program version number.
2. Update Changelog (in development branch)
3. Make sure All code is pushed to GitHub to the development branch
4. Create a Pull request Dev -> Releases
5. Run the build.cmd and wait for it to finish
6. Once Finished Edit the "RELEASES" FILE in the releases folder.
    * Remove everything except the current version Delta and Full
    * Save the file.
7. Remove old version .nupkg packages inside the releases folder.
8. Complete the Pull Request you created earlier (Dev -> Releases)
9. Ready to publish the release
    * Go to Releases on GitHub, draft new release.
    * Put in the info.
    * Upload the ALL of the following found in the Releases folder:
        *  FE-BUDDYSetup.exe
        *  FE-BUDDY-x.x.x-full.nupkg
        *  FE-BUDDY-x.x.x-delta.nupkg
        *  RELEASES
    * Publish the release.
