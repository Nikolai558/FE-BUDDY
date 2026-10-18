# RELEASE STEPS QUICK-REFERENCE

For the detailed version, see [Releasing](RELEASING.md).

## RELEASE PREP

### RELEASE PREP - Using Claude

Replace the `???` at the top of this message with the full version name of the next release and send it to Claude.
```
Time to do release-prep for: ???

Only do the following, as many of the build tests, etc... are done by GitHub:

`...\FE-BUDDY\FeBuddy\FeBuddy.Wpf\FeBuddy.Wpf.csproj`
    Update the `<Version>` value to `<full version name>`

`...\FE-BUDDY\ChangeLog.md`
Format the top and leave the UNRELEASED section empty.:
:::::::::::
## Unreleased

---

## <full version name>
:::::::::::

`...\FE-BUDDY\News.md`
Update the news for quick reading by the user.
Reference previous news posts for inspiration.

Then create a Pull Request Title and Message for when I merge this branch into `v3-development`
Respond here with it in code boxes so I may copy and paste it.

```

When Claude is done, push the release prep commit to GH.
Close Claude and [skip to the GITHUB WORK / RELEASE section](#github-work--release) below.

### RELEASE PREP - Without Claude

#### IN VISUAL STUDIO

Open to the branch you have been working on.

Note: If you forgot to start the release on this branch, make a new branch from `v3-development` and update them there and then merge.

##### CHANGE LOG.md

Go to solution explorer > folder view > ChangeLog.md

Make the "unreleased" now under your next version number.

Leave the UNRELEASED section empty:
```
## Unreleased

---

## <full version name>
```

This is what is displayed to the users.

##### NEWS.md

Add a post at the top (see the format comment at the top of News.md).

##### CHANGE APP VERSION NUMBER

Go back to Solution View

Double-Click `FeBuddy.Wpf.csproj`

Find `<Version>` value and update it to next full version name

Save

Commit message: `FullVersionName Release Prep`

Commit/Push

Close Visual Studio

### GITHUB WORK / RELEASE

#### BRING v3-DEVELOPMENT BRANCH UP TO DATE

Go to GITHUB > Pull Requests

Create new Pull Request
- For all branches to go into `v3-development` that should be part of this next release.
- Title: `if you used claude, paste it here, otherwise create a title`
- Message: `if you used claude, paste it here, otherwise create a message`

Create

Allow automated checks to process (3-5min usually)
- Warnings are normal but failed tests are not.
- If a test failed, you can go back to your visual studio and fix the issue and then complete the steps again up to this point, even while the checks are still running.

Once all pass (may require a screen refresh), "Merge Pull Requests"

#### BRING v3-DEVELOPMENT INTO RELEASES BRANCH

Go to Pull Requests and create a new PR
- Base = `releases`
- Compare = `v3-development`

create

- Title: `Releasing v3.x.x-@.x`
  - Example: `Releasing v3.0.0-beta.9`
- Message: (Same as Title)

Create Pull Request

Allow automated checks to process (longer than the previous one by almost double)
- IMPORTANT: When merging or PR into the RELEASES branch, do **NOT** select "UPDATE BRANCH".
- "Merge is still blocked" is normal due to missing signatures. Merge it with a bypass only after the checks PASS without a FAIL.
- Merge with bypass, if needed.

#### AUTOMATED RELEASE PROCESS

GitHub > ACTIONS

Look for "release #<##>" with a pending icon and click that

Wait for the "Draft Release" to be green. (about 2min)
- Warnings are ok but ERRORS are not.

GitHub > Releases page

New version is in Draft status

Edit the draft button (pencil icon)

Things to check:
- Tag should be version number without "v"
- Target: Releases branch
- Releasing title: Version title with "v" included
- Edit the release notes if needed (leave the SHA-256 line alone), then make ChangeLog.md match.
- Look at the preview to make sure it looks good.
- Check .msi is in ASSETS
- Select "release label" as required

Publish

Check to make sure old FEB updates correctly to this release on your physical computer.
