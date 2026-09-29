# Releasing FE-Buddy

Making a release is three things: **a change-log entry**, **a version number**, and **two pull
requests**. GitHub Actions does the rest - it checks everything before you can merge, builds the
installer, and drafts the release for you to publish.

Just want the steps? Use the [Release checklist](Release-Checklist.md).

```
feature branch --PR--> v3-development --PR--> releases --(automatic)--> draft release --(you)--> Publish
```

## The branches

- **`v3-development`** - the default branch. All work goes here, by pull request.
- **`releases`** - what has been released. The only thing ever merged into it is
  `v3-development`, by pull request; merging drafts the release. Nothing else can get in: the
  "Release source" check fails any other pull request.

The repository's rulesets protect exactly three branches - `v3-development`, `releases` and 2.x's
`development` - and every tag. Apart from the rulesets' bypass list (the repository admins), nobody
can push to those branches directly, force-push them, delete them or create them again: changes
arrive only by pull request, as a merge commit, once the required checks pass (and `releases` also
needs signed commits). Nobody else can create, move or delete a tag either. Any other branch name
is fine.

## Writing a change-log entry

Every pull request that changes something a user would notice adds **one bullet** under
`## Unreleased` in [`ChangeLog.md`](../../ChangeLog.md). When the release is made, those bullets
become the release notes that users read on GitHub and in FE-Buddy's update window - so write them
for users, not for developers:

- **Say what changed for the user, in one line** (two at most). Not the commit message, not how
  the code works.
- **Bugs:** `Bug #215 - ` then what is fixed. `#215` becomes a link to the issue.
- **Developer-only changes** (refactors, CI, tests) go under one `- (Dev notes)` bullet at the end
  of the section, indented two spaces.
- **Link a doc at the release's tag**, e.g. `blob/3.0.0-alpha.2/docs/...`, never at a branch: a
  branch keeps changing, and the notes don't. The link works once the release is published.
- Past tense or plain statements; no "I" or "we".

| Instead of | Write |
|---|---|
| `- Refactored AirwayBuilder to use the new pipeline` | *(Dev notes)* `- Airways now use the shared build pipeline.` |
| `- fix #215` | `- Bug #215 - Fixed the download bar stalling when the next cycle's d-TPP data isn't out yet.` |
| `- Added ExportSettingsCommand and ImportSettingsCommand to SettingsViewModel` | `- Settings can now be exported to a file and imported on another PC.` |

## Making a release

### 1. Pick the version

Look at the [latest release](https://github.com/Nikolai558/FE-BUDDY/releases). The new version
must be **exactly one step** after it - the pre-flight fails anything else:

| Latest release | Next release can be |
|---|---|
| `3.0.0-alpha.1` | `3.0.0-alpha.2`, `3.0.0-beta.1`, `3.0.0-rc.1` or `3.0.0` |
| `3.0.0-beta.3` | `3.0.0-beta.4`, `3.0.0-rc.1` or `3.0.0` |
| `3.0.0` | `3.0.1`, `3.1.0` or `4.0.0` - or any of those with `-alpha.1`, `-beta.1` or `-rc.1` |

Which one? Bug fixes only: PATCH. New features: MINOR. Breaking changes: MAJOR. Testing before
it's official: `-alpha.N`, `-beta.N`, `-rc.N`. Details: [Versioning](VERSIONING.md).

### 2. Prepare it (a pull request into `v3-development`)

In one pull request - its own, or the last feature pull request before the release:

1. **`ChangeLog.md`:** rename `## Unreleased` to `## <version>` (e.g. `## 3.0.0-alpha.2`), and add a
   new, empty `## Unreleased` above it, with a `---` line between them. Sections are separated by
   `---`, with a blank line above and below it; the release notes leave it out. Read the bullets
   through once more - they are the release notes.
2. **`FeBuddy/FeBuddy.Wpf/FeBuddy.Wpf.csproj`:** set `<Version>` to the same version. It is the
   only place the version lives.

Merge it into `v3-development` as usual.

Optional, before the next step - run the checks on your own PC (needs the .NET 10 SDK and the
GitHub CLI, `gh`, signed in):

```bash
powershell -File .github/scripts/Test-ReleaseReadiness.ps1
```

```bash
powershell -File .github/scripts/Build-ReleaseAssets.ps1
```

The second builds the MSI and writes the release notes to `FeBuddy/releases/release-notes.md`.

### 3. Open the release pull request

On GitHub: **Pull requests > New pull request**, **base: `releases`**, **compare:
`v3-development`**. Title it `Release <version>`.

These checks run, and all must pass before it can merge:

| Check | What it checks |
|---|---|
| Build, Test, CodeQL | The same as on every pull request. |
| Release source | The pull request is from `v3-development`. |
| Release pre-flight | The version is a release version and one step after the last release; no release or tag has it yet; `ChangeLog.md` has its section, as the newest, and `## Unreleased` is empty; developer mode is off; the installer-counter token works; the MSI builds, with the right version and 2.x's upgrade identity; the release notes render. |

**Read the release notes before merging:** on the pull request, **Checks > Release checks >
Release pre-flight > Summary** shows every check and exactly what the release notes will say. The
**release-preview** artifact on that page has the MSI, if you want to try the build.

Something failed? The summary says what and how to fix it. Fix it with a pull request into
`v3-development`; the release pull request picks up the change and runs the checks again.

### 4. Merge it

Use **Create a merge commit**. If GitHub says commits need verified signatures, a repository admin
can merge anyway with the bypass option once they have reviewed the changes.

"This branch is out-of-date with the base branch" is expected: each release leaves a merge commit
on `releases` that `v3-development` doesn't have. It doesn't block the merge. Don't click
**Update branch** - that merges `releases` into `v3-development`, and changes only ever go the other
way.

Merging starts the **Release** workflow (the **Actions** tab). In about ten minutes it builds the
MSI, commits the advanced installer counter to `v3-development`, and creates a **draft** release.
Nothing is public yet.

### 5. Review and publish the draft

**Releases** on the repository's front page, then the new **Draft**. Check:

- Title `v<version>`, tag `<version>`, target `releases`.
- The notes read well. Edit them here if needed (and fix `ChangeLog.md` the same way later, so
  they match).
- The asset `FE-BUDDY-Setup.msi` is attached.
- For `-alpha`, `-beta` and `-rc`: **Set as a pre-release** is ticked. For a stable release:
  **Set as the latest release** is ticked instead.

Press **Publish release**. That creates the tag, and FE-Buddy starts offering the update to users
on that release's channel (2.9 and later too). Since the tag ruleset lets only its bypass list
create tags, only a repository admin can publish; anyone else can review the draft.

Don't merge another release pull request while a draft is waiting - publish or delete the draft
first.

## When something goes wrong

- **The Release workflow failed after the merge** - open the failed run to see why. For something
  outside the code (a GitHub outage), re-run it: **Actions > Release > Run workflow**, branch
  `releases`. If it got as far as creating a draft, delete that draft first.
- **A deliberate exception to the one-step rule** (e.g. a skipped number that was never
  released) - add the `skip-version-step` label to the release pull request.
- **"Installer-counter token" failed** - the `RELEASE_COUNTER_PAT` secret has expired or lost
  access. Make a new fine-grained token (repository `Nikolai558/FE-BUDDY`, **Contents: Read and
  write**) on an account that can bypass the `v3-development` ruleset, and save it under
  **Settings > Secrets and variables > Actions > RELEASE_COUNTER_PAT**.
- **A published release was wrong** - tags cannot be moved or deleted (the tag ruleset). Fix it
  and release the next version.

## What does what

| Piece | Does |
|---|---|
| `ChangeLog.md` | Every 3.x release's notes. 2.x's are in the [2.x change log](https://github.com/Nikolai558/FE-BUDDY/blob/2.9.3/ChangeLog.md). |
| `.github/workflows/release-checks.yml` | On pull requests into `releases`: Release source and Release pre-flight. |
| `.github/workflows/release.yml` | On a merge into `releases`: checks again, builds, commits the counter, drafts the release. |
| `.github/scripts/Test-ReleaseReadiness.ps1` | The checks that need no build. |
| `.github/scripts/Build-ReleaseAssets.ps1` | Builds and checks the MSI; writes the release notes. |
| `.github/scripts/Push-InstallerCounter.ps1` | Commits the advanced installer counter to `v3-development` ([why](MSI-VERSION-NUMBERING.md)). |
| `.github/scripts/ReleaseVersion.cs` | The version rules, from `FeBuddy.Versioning` (`ReleaseStep`). |
| `.github/release-notes-template.md` | The release notes' layout: pre-release note, install instructions, change log. |

The installer is always named `FE-BUDDY-Setup.msi`, so
`https://github.com/Nikolai558/FE-BUDDY/releases/latest/download/FE-BUDDY-Setup.msi` downloads the
latest **stable** release (GitHub's "latest" never points at a pre-release).
