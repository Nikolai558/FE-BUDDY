# Release checklist

The short version of [Releasing](RELEASING.md). Follow it from top to bottom. The examples use
`3.0.0-alpha.2`. Use your own version number in its place.

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
   In [`ChangeLog.md`](https://github.com/Nikolai558/FE-BUDDY/blob/v3-development/ChangeLog.md):
   - rename `## Unreleased` to `## 3.0.0-alpha.2`
   - add a new, empty `## Unreleased` above it
   - read the bullets once more. They become the release notes.

   See [Figure 3](#figure-3).

4. **Merge steps 2 and 3 into `v3-development`.**
   Put both in one pull request into `v3-development` and merge it.

## Part 2 - The release pull request

5. **Open a pull request from `v3-development` into `releases`.**
   [Start the release pull request](https://github.com/Nikolai558/FE-BUDDY/compare/releases...v3-development?expand=1).
   Make sure it says **base: `releases`** and **compare: `v3-development`**. Title it
   `Release 3.0.0-alpha.2`. See [Figure 4](#figure-4).

6. **Wait until every check is green.**
   All of these must pass: **Build**, **Test**, **CodeQL**, **Release source** and **Release pre-flight**.
   **Release source** and **Release pre-flight** should both show **Required**. See [Figure 5](#figure-5).
   - If you want to see the release notes before you merge, open **Checks > Release checks > Release pre-flight > Summary**.
     See [Figure 6](#figure-6).
   - Did a check fail? The summary says why. Fix it with a pull request into `v3-development`.
     The release pull request picks up the fix and runs its checks again on its own.

7. **Merge it.**
   Choose **Create a merge commit**. Unsigned commits in the pull request will block the merge.
   That is fine: **once every check is green**, a repository admin can tick the bypass box and
   merge. See [Figure 7](#figure-7).

## Part 3 - Check the draft and publish it

8. **Wait for the draft release.** This takes about 10 minutes.
   You can watch it under [Actions > Release](https://github.com/Nikolai558/FE-BUDDY/actions/workflows/release.yml),
   or come back later and look in [Releases](https://github.com/Nikolai558/FE-BUDDY/releases).
   The new release shows the **Draft** label. See [Figure 8](#figure-8).

9. **Edit the draft** with the pencil icon and check each of these:

   | Item | Should be |
   |---|---|
   | Tag | `3.0.0-alpha.2` (no `v`) |
   | Target | `releases` |
   | Title | `v3.0.0-alpha.2` |
   | Notes | They read well. If you change them here, fix `ChangeLog.md` to match later. |
   | MSI | `FE-BUDDY-Setup.msi` is listed under the assets |
   | Label | `-alpha`, `-beta` or `-rc`: **Set as a pre-release** is ticked. Stable: **Set as the latest release** is ticked. |

   See [Figure 9](#figure-9) and [Figure 10](#figure-10).

10. **Press Publish release.** That's it: the tag is created, and FE-Buddy starts offering the
    update to users. See [Figure 11](#figure-11).

## Good to know

- After the merge, the release workflow adds a commit to `v3-development` (the installer
  counter). Pull `v3-development` before you start new work.
- While a draft is waiting, don't merge another release pull request. Publish the draft or
  delete it first.
- Something went wrong? See [When something goes wrong](RELEASING.md#when-something-goes-wrong).

## Figures

<a id="figure-1"></a>
**Figure 1 - Nothing left open into `v3-development`**
*Screenshot coming (alpha.2).*

<a id="figure-2"></a>
**Figure 2 - `<Version>` in `FeBuddy.Wpf.csproj`**
*Screenshot coming (alpha.2).*

<a id="figure-3"></a>
**Figure 3 - `ChangeLog.md`: a new empty `## Unreleased` above `## 3.0.0-alpha.2`**
*Screenshot coming (alpha.2).*

<a id="figure-4"></a>
**Figure 4 - New pull request: base `releases`, compare `v3-development`**
*Screenshot coming (alpha.2).*

<a id="figure-5"></a>
**Figure 5 - Every check green, with Release source and Release pre-flight shown as Required**
*Screenshot coming (alpha.2).*

<a id="figure-6"></a>
**Figure 6 - The Release pre-flight summary, with the release notes preview**
*Screenshot coming (alpha.2).*

<a id="figure-7"></a>
**Figure 7 - Merging with the bypass box ticked**
*Screenshot coming (alpha.2).*

<a id="figure-8"></a>
**Figure 8 - The draft release in Releases (or the finished run in Actions)**
*Screenshot coming (alpha.2).*

<a id="figure-9"></a>
**Figure 9 - Editing the draft: tag, target and title**
*Screenshot coming (alpha.2).*

<a id="figure-10"></a>
**Figure 10 - Editing the draft: notes, `FE-BUDDY-Setup.msi` and the pre-release tick box**
*Screenshot coming (alpha.2).*

<a id="figure-11"></a>
**Figure 11 - The published release**
*Screenshot coming (alpha.2).*
