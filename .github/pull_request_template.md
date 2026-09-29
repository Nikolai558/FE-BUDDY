## What changed

<!-- A sentence or two on what this does and why. "Fixes #123" closes the issue when this merges. -->

## How it was tested

<!-- Unit tests added or updated, and what you checked by hand in the app. -->

## Screenshots

<!-- For UI changes: before and after. Delete this section if nothing visible changed. -->

## Checklist

- [ ] Added one user-facing line under `## Unreleased` in `ChangeLog.md`, or this change isn't something a user would notice
- [ ] Builds, and the tests pass (FeBuddy.Core coverage stays at or above the CI floor)
- [ ] Formatting is clean: `dotnet format style` and `dotnet format whitespace` with `--verify-no-changes` (see `docs/Developers/Getting-Started.md`)
- [ ] Updated the docs and any in-app text this change affects (User Guide, FAQ, tooltips, messages), or none were affected
- [ ] No real FAA source files or excerpts added. Test fixtures are made up.

<!--
  Change-log entries become the release notes users read, so write them for users: what changed
  and why they care, in one line. How: docs/Developers/RELEASING.md#writing-a-change-log-entry

  Making a release? Pull requests into `releases` must come from `v3-development`; the steps are in
  docs/Developers/RELEASING.md.
-->
