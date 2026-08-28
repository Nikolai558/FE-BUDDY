# Optional GitHub Token (`FEBUDDY_GITHUB_TOKEN`)

FE-BUDDY's update checker can optionally use a GitHub personal access token from an
environment variable named `FEBUDDY_GITHUB_TOKEN`. **This is not required for normal
use.** The vast majority of users will never need to touch this - FE-BUDDY's releases
live in a public GitHub repo, and the update checker works completely unauthenticated by
default, exactly as it always has.

This doc explains why the option exists, when you'd actually want it, and how to set it
up if you do.

## Why it might be needed

Two real reasons, neither of which apply to most people:

1. **GitHub's unauthenticated rate limit.** Unauthenticated API requests are capped at
   60 per hour, and that limit is shared by IP address - meaning an entire household or
   office network shares the same 60/hour, not 60 per person. If you're on a network
   where that limit gets hit (heavy dev/testing use, a shared office, etc.), setting a
   token raises it substantially.
2. **Testing against a private repo.** If you're developing or testing FE-BUDDY itself
   and pointing the update checker at a private repo (e.g. a throwaway test/dev repo),
   GitHub requires authentication to even see that the repo exists - a token is the only
   way to make that work.

If neither of those applies to you, there's nothing to do here.

## Why it's optional, not required

FE-BUDDY's real releases are, and are expected to remain, in a public repo. Public repos
don't need authentication to check for releases or download assets - that's the whole
point of them being public. The token exists purely as a fallback for the two edge cases
above, not as part of the normal update-check path.

## How the fallback actually works

The update checker **always tries the request unauthenticated first**. It only looks for
`FEBUDDY_GITHUB_TOKEN` if that first attempt fails, and only then retries once, with the
token attached.

This is deliberate, not an accident of implementation: if the token were sent on every
request regardless of need, a token left over in your environment for some *other* tool
(GitHub CLI, a CI runner, an old project) could end up silently breaking a request that
would otherwise have worked fine. Trying unauthenticated first means the token can only
ever *rescue* a failed request - it can never interfere with one that was already
succeeding. That's also why the variable is named `FEBUDDY_GITHUB_TOKEN` specifically,
rather than a generic name like `GITHUB_TOKEN` that other tools commonly use - it avoids
ever picking up a token that has nothing to do with FE-BUDDY.

See [`FeBuddyLibrary/Update/GitHubAuth.cs`](FeBuddyLibrary/Update/GitHubAuth.cs),
[`UpdateChecker.cs`](FeBuddyLibrary/Update/UpdateChecker.cs), and
[`UpdateInstaller.cs`](FeBuddyLibrary/Update/UpdateInstaller.cs) for the actual
implementation.

## Setting it up

### 1. Create the token on GitHub

1. Go to **github.com → Settings → Developer settings → Personal access tokens →
   Fine-grained tokens → Generate new token**.
2. Under **Repository access**, choose **Only select repositories** and pick the
   specific repo you need access to (don't grant access to more repos than necessary).
3. Under **Permissions → Repository permissions**, set **Contents** to **Read-only**.
   That's the only permission needed - it covers both listing releases and downloading
   release assets.
4. Generate it and copy the value immediately - GitHub only shows it once.

Treat this token like a password. Don't commit it to any file in the repo, don't paste
it into chat/issues/PRs, and don't share it. A leaked token can be revoked from the same
Developer settings page it was created in.

### 2. Set the environment variable on Windows

In PowerShell:

```bash
setx FEBUDDY_GITHUB_TOKEN "paste-your-token-here"
```

`setx` sets it permanently for your Windows user account - but it only takes effect in
*new* processes started after you run it, not the terminal/IDE you ran it in. Close and
reopen Visual Studio (or whatever terminal you're using) before testing.

**Verify it's set** (in a freshly-opened PowerShell window):

```bash
echo $env:FEBUDDY_GITHUB_TOKEN
```

### 3. Remove or rotate it later

```bash
setx FEBUDDY_GITHUB_TOKEN ""
```

Or delete it via **Windows Settings → System → About → Advanced system settings →
Environment Variables**. If you ever suspect a token has leaked, revoke it on GitHub
first (Developer settings → the token → Delete), then generate a new one if still needed.
