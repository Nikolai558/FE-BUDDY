## Steps to release and push out a new update.

> **Note - partial automation is planned.** A GitHub Actions workflow at
> `.github/workflows/release.yml.disabled` is written to take over steps 6-10
> (build, then create a **draft** release with the MSI + carried-forward Squirrel
> bridge assets, notes pulled from `ChangeLog.md`). It is deliberately disabled
> (renamed with a `.disabled` suffix so Actions ignores it) until **2.8.4** is
> released manually and the `SQUIRREL_BRIDGE_TAG` repo variable is set - see
> [SQUIRREL-TO-MSI-MIGRATION.md](SQUIRREL-TO-MSI-MIGRATION.md). Until then, the
> manual steps below are authoritative. The workflow never publishes - a
> maintainer still reviews and publishes the draft. It also hard-fails if
> `GlobalConfig.DEVMODE` is not `false` (step 2) or if `ChangeLog.md` has no
> `- ## Version <ver>` section (step 3).

1. Change Program version number.
2. Verify Dev mode is False.
3. Update Changelog (in development branch)
4. Make sure All code is pushed to GitHub to the development branch
5. Create a Pull request Dev -> Releases
6. Run `build.cmd` (in the repo root) and wait for it to finish
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
