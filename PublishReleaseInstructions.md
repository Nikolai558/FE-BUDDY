## Steps to release and push out a new update.
1. Change Program version number.
2. Verify Dev mode is False.
3. Update Changelog (in development branch)
4. Make sure All code is pushed to GitHub to the development branch
5. Create a Pull request Dev -> Releases
6. Run the build.cmd and wait for it to finish
7. Once Finished Edit the "RELEASES" FILE in the releases folder.
    * Remove everything except the current version Delta and Full
    * Save the file.
8. Remove old version .nupkg packages inside the releases folder.
9. Complete the Pull Request you created earlier (Dev -> Releases)
10. Ready to publish the release
    * Go to Releases on GitHub, draft new release.
    * Put in the info.
    * Upload the ALL of the following found in the Releases folder:
        *  FE-BUDDYSetup.exe
        *  FE-BUDDY-x.x.x-full.nupkg
        *  FE-BUDDY-x.x.x-delta.nupkg
        *  RELEASES
    * Publish the release.
