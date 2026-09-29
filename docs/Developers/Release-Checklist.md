# Release checklist

The short version of [Releasing](RELEASING.md). Follow it from top to bottom. The examples and
screenshots are from `3.0.0-alpha.2`. Use your own version number in its place.

## Part 1 - Get `v3-development` ready

1. **Merge every code change into `v3-development`.**
   Every pull request that belongs in this release is merged into `v3-development`. Check that
   nothing you need is still open:
   [open pull requests into v3-development](https://github.com/Nikolai558/FE-BUDDY/pulls?q=is%3Apr+is%3Aopen+base%3Av3-development).
   See [Figure 1](#figure-1).

2. **Change the version number.**
   In [`FeBuddy/FeBuddy.Wpf/FeBuddy.Wpf.csproj`](https://github.com/Nikolai558/FE-BUDDY/blob/v3-development/FeBuddy/FeBuddy.Wpf/FeBuddy.Wpf.csproj),
   set `<Version>3.0.0-alpha.2</Version>`. It is the only place the version lives.
   It must be exactly one step after the [latest release](https://github.com/Nikolai558/FE-BUDDY/releases)
   ([which versions are allowed](RELEASING.md#1-pick-the-version)). See [Figure 2](#figure-2).

3. **Update the change log.**
   In [`ChangeLog.md`](https://github.com/Nikolai558/FE-BUDDY/blob/v3-development/ChangeLog.md),
   rename `## Unreleased` to `## 3.0.0-alpha.2`. Then, above it, add a new `## Unreleased` with a
   `---` line under it. Keep a blank line above and below every `---`.

   Before:

   ```markdown
   ## Unreleased
   - Bug #251 - Fixed ...

   ---

   ## 3.0.0-alpha.1
   ```

   After:

   ```markdown
   ## Unreleased

   ---

   ## 3.0.0-alpha.2
   - Bug #251 - Fixed ...

   ---

   ## 3.0.0-alpha.1
   ```

   Read the bullets once more. They become the release notes. (The `---` lines stay in the
   change log only. The release notes leave them out.) See [Figure 3](#figure-3).

4. **Merge steps 2 and 3 into `v3-development`.**
   Put both in one pull request into `v3-development`, wait for the checks to be green, and merge
   it. See [Figure 5](#figure-5).
   - Optional: before you push, run the release checks on your own PC. See [Figure 4](#figure-4).

     ```bash
     powershell -ExecutionPolicy Bypass -File .github/scripts/Test-ReleaseReadiness.ps1
     ```

## Part 2 - The release pull request

5. **Open a pull request from `v3-development` into `releases`.**
   [Start the release pull request](https://github.com/Nikolai558/FE-BUDDY/compare/releases...v3-development?expand=1).
   Make sure it says **base: `releases`** and **compare: `v3-development`**. Title it
   `Releasing v3.0.0-alpha.2`. See [Figure 6](#figure-6).

6. **Wait until every check is green.**
   Every check must pass, including **Release source** and **Release pre-flight**. Those two
   must show **Required**. See [Figure 7](#figure-7).
   - **"This branch is out-of-date with the base branch" is normal.** Ignore it, and **never
     click Update branch**: that would merge `releases` back into `v3-development`. The message
     shows up because every release leaves a merge commit on `releases`. It doesn't block the merge.
   - If you want to see the release notes before you merge, open **Checks > Release checks > Release pre-flight > Summary**.
     See [Figure 8](#figure-8).
   - Did a check fail? The summary says why. Fix it with a pull request into `v3-development`.
     The release pull request picks up the fix and runs its checks again on its own.

7. **Merge it.**
   Click **Merge pull request**, then **Confirm merge**. Unsigned commits in the pull request
   will block the merge. That is fine: **once every check is green**, a repository admin can tick
   the bypass box and merge. See [Figure 9](#figure-9).

## Part 3 - Check the draft and publish it

8. **Wait for the draft release.** This takes a few minutes.
   You can watch it under [Actions > Release](https://github.com/Nikolai558/FE-BUDDY/actions/workflows/release.yml).
   When it finishes, the run's summary links to the draft. See [Figure 10](#figure-10) and
   [Figure 11](#figure-11).

9. **Open the draft and edit it.**
   The draft is in [Releases](https://github.com/Nikolai558/FE-BUDDY/releases) with the **Draft**
   label ([Figure 12](#figure-12)). Click the pencil icon and check each of these:

   | Item | Should be |
   |---|---|
   | Tag | `3.0.0-alpha.2` (no `v`) |
   | Target | `releases` |
   | Title | `v3.0.0-alpha.2` |
   | Notes | They read well. If you change them here, fix `ChangeLog.md` to match later. |
   | MSI | `FE-BUDDY-Setup.msi` is listed under **Assets** |
   | Release label | `-alpha`, `-beta` or `-rc`: **Pre-release**. Stable: **Latest**. |

   See [Figure 13](#figure-13) and [Figure 14](#figure-14).

10. **Press Publish release.** That's it: the tag is created, and FE-Buddy starts offering the
    update to users. See [Figure 15](#figure-15).

## Good to know

- After the merge, the release workflow adds a commit to `v3-development` (the installer
  counter). Pull `v3-development` before you start new work.
- While a draft is waiting, don't merge another release pull request. Publish the draft or
  delete it first.
- Something went wrong? See [When something goes wrong](RELEASING.md#when-something-goes-wrong).

## Figures

<a id="figure-1"></a>
**Figure 1 - Nothing left open**

![The pull requests page showing 0 open pull requests](Media/Releasing/01-no-open-pull-requests.png)

<a id="figure-2"></a>
**Figure 2 - The version change in `FeBuddy.Wpf.csproj`**

![A diff of FeBuddy.Wpf.csproj where Version changes from 3.0.0-alpha.1 to 3.0.0-alpha.2](Media/Releasing/02-version-diff.png)

<a id="figure-3"></a>
**Figure 3 - The change in `ChangeLog.md`**

This is the alpha.2 pull request, from before a `---` also went under `## Unreleased`. Follow the "After" example in step 3.

![A diff of ChangeLog.md adding the 3.0.0-alpha.2 heading below Unreleased](Media/Releasing/03-changelog-diff.png)

<a id="figure-4"></a>
**Figure 4 - Optional: the release checks on your own PC**

![Test-ReleaseReadiness.ps1 output with every check passing](Media/Releasing/04-local-readiness-check.png)

<a id="figure-5"></a>
**Figure 5 - The `v3-development` pull request, all green and ready to merge**

![All checks have passed on the pull request into v3-development, with the Merge pull request button](Media/Releasing/05-prep-pull-request-green.png)

<a id="figure-6"></a>
**Figure 6 - Opening the release pull request: base `releases`, compare `v3-development`**

![The Open a pull request page with base releases, compare v3-development and the title filled in](Media/Releasing/06-open-release-pull-request.png)

<a id="figure-7"></a>
**Figure 7 - Every check green. Release pre-flight and Release source are Required. Ignore "out-of-date".**

![The release pull request's checks all passed, with Release pre-flight and Release source marked Required, above the out-of-date notice and the Merge pull request button](Media/Releasing/07-release-checks-green.png)

<a id="figure-8"></a>
**Figure 8 - Release pre-flight > Summary: the release notes preview**

![The release notes preview on the Release pre-flight summary page](Media/Releasing/08-release-notes-preview.png)

<a id="figure-9"></a>
**Figure 9 - The bypass box (admins only)**

It appears whenever something blocks the merge. Tick it only for unsigned commits, and only once every check is green.

![The Merge without waiting for requirements to be met (bypass rules) checkbox above a disabled Merge pull request button](Media/Releasing/09-bypass-box.png)

<a id="figure-10"></a>
**Figure 10 - The Release workflow running after the merge**

![The Actions page with the Release workflow run for the merge into releases queued](Media/Releasing/10-release-workflow-running.png)

<a id="figure-11"></a>
**Figure 11 - The finished run's summary, with the link to the draft**

![The Release run summary saying the draft release is ready, with a link to it](Media/Releasing/11-draft-ready-summary.png)

<a id="figure-12"></a>
**Figure 12 - The draft in Releases**

![The Releases page showing v3.0.0-alpha.2 with the Draft label and the pencil icon](Media/Releasing/12-draft-in-releases.png)

<a id="figure-13"></a>
**Figure 13 - Editing the draft: tag, target and title**

![The Edit release page showing Tag 3.0.0-alpha.2, Target releases and the title v3.0.0-alpha.2](Media/Releasing/13-edit-draft-tag-target-title.png)

<a id="figure-14"></a>
**Figure 14 - Editing the draft: the MSI, the release label and Publish release**

![The Assets list with FE-BUDDY-Setup.msi, the Release label set to Pre-release, and the Publish release button](Media/Releasing/14-edit-draft-msi-and-label.png)

<a id="figure-15"></a>
**Figure 15 - The published release**

![The published v3.0.0-alpha.2 release with the Pre-release label](Media/Releasing/15-published-release.png)
