# Creating a GitHub token for FE-Buddy

FE-Buddy needs a GitHub token only to read a file from a **private** GitHub repository - usually
your facility's custom alias file, on the Concatenate Aliases tab. A public repository needs no token.

This guide makes a **fine-grained personal access token** that can do as little as possible: read
the files of the repositories you pick, for a limited time. FE-Buddy never writes to GitHub.

## The short version

| Setting on GitHub | Choose |
|---|---|
| Token type | **Fine-grained** (not "classic") |
| Token name | Something you'll recognise, e.g. `FE-Buddy - ZOB alias file` |
| Resource owner | The account or **organization** that owns the repository |
| Expiration | **90 days**, or whatever your facility prefers - never "No expiration" |
| Repository access | **Only select repositories**, then just the one(s) with your alias files |
| Repository permissions | **Contents: Read-only**, nothing else |
| Account permissions | None |

Then paste the token into FE-Buddy (Settings ▸ Credentials, or **New credential…** on the Concatenate
Aliases tab) and press **Check**.

## Step by step

### 1. Open GitHub's new-token page

In FE-Buddy's credential editor, choose **GitHub personal access token** as the type and click
**Create a token on GitHub**. GitHub's page for a new fine-grained token opens; sign in if asked.

Without FE-Buddy: your profile picture ▸ **Settings** ▸ **Developer settings** ▸ **Personal access
tokens** ▸ **Fine-grained tokens** ▸ **Generate new token**.

> Use **Fine-grained tokens**, not **Tokens (classic)**. A classic token with the `repo` scope can
> read *and change* every repository you can reach.

### 2. Name, owner and expiry

- **Token name** - anything that tells you later what it's for. The description is optional.
- **Resource owner** - your own account if the repository is under your name, or your facility's
  **organization** (for example `vZOB`). An organization is only listed if it allows fine-grained
  tokens; if yours isn't, ask one of its owners.
- **Expiration** - **90 days**, or a custom date up to a year. An expiry limits the damage if the
  token ever leaks. GitHub emails you before it expires; see [When the token expires](#when-the-token-expires).

![GitHub's new fine-grained token page with the token name, description, resource owner and a 90-day expiration filled in](Media/GitHub-Token/01-name-owner-expiration.png)

### 3. Repository access

Choose **Only select repositories** and pick only the repository (or repositories) with your custom
alias files.

![Repository access set to Only select repositories, with one repository selected](Media/GitHub-Token/02-repository-access.png)

Don't choose **All repositories**. **Public repositories** is only for a token in Settings ▸
FE-Buddy's GitHub Requests, which just lifts GitHub's limit of 60 requests an hour for FE-Buddy's
update checks.

### 4. Permissions: Contents, read-only

Under **Permissions**, on the **Repositories** tab, click **Add permissions**, type `Contents`, and
tick **Contents**.

![The Add permissions list, filtered to Contents](Media/GitHub-Token/03-add-contents-permission.png)

Close the list. You should see exactly two repository permissions:

- **Contents** - **Access: Read-only**. If it says *Read and write*, change it.
- **Metadata** - **Required** and read-only. GitHub adds it itself; it's harmless.

Add nothing else, and nothing on the **Account** tab.

![Contents set to Read-only, Metadata required and read-only, and the Generate token button](Media/GitHub-Token/04-contents-read-only.png)

> On older versions of the page, open **Repository permissions**, set **Contents** to **Read-only**,
> and leave everything else at *No access*.

### 5. Generate and copy the token

Click **Generate token** (confirm the summary if GitHub shows one). GitHub shows the token - it
starts with `github_pat_` - **once only**, so copy it straight away.

> If the repository belongs to an organization, the token may be **Pending** until an owner
> approves it. Until then FE-Buddy reports that the file wasn't found.

### 6. Save it in FE-Buddy

In FE-Buddy's credential editor (Settings ▸ Credentials ▸ **Add credential…**, or **New credential…**
on the Concatenate Aliases tab):

![FE-Buddy's Add Credential window, set to GitHub personal access token](Media/GitHub-Token/05-fe-buddy-credential-editor.png)

1. **Name** - e.g. `ZOB GitHub`.
2. **Type** - **GitHub personal access token**.
3. **Personal access token** - paste the token.
4. **Use only with these websites** - leave it as `github.com, githubusercontent.com`. FE-Buddy never
   sends the token anywhere else.
5. **Save**, then **Check** next to the credential. *GitHub accepted this token* means it's valid.

FE-Buddy keeps the token in Windows Credential Manager, encrypted with your Windows sign-in. It's
never written to FE-Buddy's settings or exports, and never shown again.

Now choose it for your file on the Concatenate Aliases tab and press **Check** there: it should say
how many alias commands it read. One token can serve several files - a later GitHub file with no
credential is offered, for example, **Use ZOB GitHub, like file 1**.

## When the token expires

GitHub emails you first. Once it has expired, FE-Buddy says *GitHub refused the credential* for the
files that use it.

1. On GitHub, open **Settings** ▸ **Developer settings** ▸ **Personal access tokens** ▸
   **Fine-grained tokens**, click the token, and choose **Regenerate token**. It keeps the same
   repositories and permissions.
2. Copy the new token.
3. In FE-Buddy, Settings ▸ Credentials ▸ **Edit…**, paste it into **Personal access token**, and
   **Save**. Everything that used the credential keeps using it.

## If something goes wrong

What **Check** says, and what to do:

| FE-Buddy says | Most likely | Fix |
|---|---|---|
| GitHub could not find it … if the repository is private, choose a GitHub credential | No credential chosen for a private repository | Choose your GitHub token for that file |
| GitHub could not find it, or the credential … cannot see it | The repository isn't in the token's list; the token is still **Pending**; or the wrong **Resource owner** | Add the repository to the token, ask an organization owner to approve it, or make a new token owned by the organization |
| GitHub refused the credential … mistyped, expired or revoked | The token expired, was deleted, or wasn't pasted in full | Regenerate it and paste the new one into the credential |
| GitHub does not let the credential … read it … Contents: Read-only | The token has no **Contents** permission | Edit the token on GitHub: Contents, Read-only |
| GitHub needs the credential … authorized for this organization's single sign-on (SSO) | The organization uses SAML single sign-on | Authorize the token for the organization on GitHub, then **Check** again |
| GitHub is limiting how often it can be asked right now | Too many requests in a short time | Wait a few minutes and **Check** again |
| This address has a sign-in token (token=) in it | The address was copied while viewing a private file's raw text | Use the file's page or its Raw link without the `token=`, and choose your token as its credential |
| … a GitHub page, not a file | The address is a repository's front page or a folder | Open the alias file itself on GitHub and copy its address, or its Raw link |

Without any token, a 403 can also mean GitHub's hourly limit for anonymous downloads was reached:
choose a token, or try again in an hour.

## Keeping the token safe

- Treat it like a password: don't paste it into Discord, an email or a shared document.
- If it may have leaked, delete it on GitHub (the token's page ▸ **Delete**) and make a new one. A
  deleted token stops working at once.
- One token per purpose is easiest - for example one for your alias file and, only if you need it, a
  public-repositories-only one for FE-Buddy's update checks.
