# Credentials

How FE-Buddy keeps user names, passwords, GitHub tokens and API keys, and how a feature that
downloads from a protected website uses one.

## Where credentials live

- In **Windows Credential Manager**, never in `UserConfig.json`, a settings export or any other
  FE-Buddy file.
  - Windows encrypts each entry with the signed-in user's key (DPAPI).
  - Only that user, on that PC, can read it. A copy of the files is no use on another PC or to
    another account.
  - Entries use local-machine persistence, so they never roam with a roaming profile.
- **One entry per credential**, named `FE-Buddy:credential:<id>`. It holds everything about the
  credential: name, type, user name, websites, whether FE-Buddy's own GitHub requests use it, and
  the secret.
  - Users can see or delete the entries in Control Panel ▸ Credential Manager ▸ Windows Credentials.
- **Settings save only the credential's id.** On another PC the id matches nothing, so an imported
  or copied config never carries a secret, and the user picks one of their own credentials.
- **Users manage them in Settings ▸ Credentials.**
  - Add, edit and remove. A saved secret is never shown again.
  - "Check" asks GitHub whether a GitHub token still works.
  - "Remove all" clears every FE-Buddy credential.
- **A full uninstall removes them** (custom action `RemoveFeBuddyCredentials`); an upgrade never
  does.

## Types

| Type | Sent as |
|---|---|
| User name and password | `Authorization: Basic …` |
| GitHub personal access token | `Authorization: Bearer …` |
| Token or API key | `Authorization: Bearer …` |

## Websites

**Every credential names the websites it may be sent to, and FE-Buddy sends it nowhere else.**
- A website covers itself and its subdomains: `github.com` covers `api.github.com`, but not
  `github.com.example.net`.
- A GitHub token starts with `github.com, githubusercontent.com`, which covers GitHub's API and its
  raw-file and download hosts.
- A credential is only ever sent over **HTTPS**.

This is what makes sharing settings safe. A settings file pointing a download at someone else's
website can never collect a user's token, because the token is not allowed there.

## Using a credential in a feature

Everything goes through `CredentialStore` (`FeBuddy.Core/Infrastructure/Credentials`). A feature
never reads or stores a secret itself. The vNAS Alias Upload sub-service is the working example:
`VnasAliasViewModel` offers the drop-down and saves `Sources.<n>.CredentialId`, and
`AliasSourceLoader` (`FeBuddy.Core/Application/Airac/VnasAlias`) downloads with it.

```csharp
CredentialStore store = CredentialStore.Default;

// 1. Offer the user's credentials in a drop-down. CredentialInfo has no secret.
IReadOnlyList<CredentialInfo> choices = store.List();          // by name
string label = $"{info.Name} ({info.Kind.DisplayName()})";     // e.g. "ZOB GitHub (GitHub personal access token)"

// 2. Save only the id with the feature's own settings, e.g. "Sources.1.CredentialId" = info.Id.ToString("N").

// 3. When downloading, let the store add the header.
using HttpRequestMessage request = new(HttpMethod.Get, url);
switch (store.Authorize(request, credentialId))
{
    case CredentialUseResult.Applied:        break;   // send it
    case CredentialUseResult.NotFound:       /* the credential was removed, or chosen on another PC: ask the user to pick one */ break;
    case CredentialUseResult.HostNotAllowed: /* the URL is not one of the credential's websites: do not send it anonymously by surprise; tell the user */ break;
    case CredentialUseResult.NotHttps:       /* refuse: the credential would travel unencrypted */ break;
}
using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
```

- **Refresh lists on change.** `store.Changed` is raised whenever a credential is saved or removed,
  so a drop-down can refresh.
- **Redirects are safe.** .NET drops the `Authorization` header when a request is redirected, so a
  download that redirects (GitHub's do) never carries the credential off its website.
- **Downloading from GitHub.**
  - For a file in a private repository, request
    `https://api.github.com/repos/{owner}/{repo}/contents/{path}?ref={branch}` with
    `Accept: application/vnd.github.raw`. `GitHubFileUrl.ToContentsApi` turns a file's `github.com`
    or `raw.githubusercontent.com` address into that one.
  - `raw.githubusercontent.com` does not reliably honour a token (see `NewsService`).
- **Settings key names.** Name a key that holds a credential id so it is plainly an id, e.g.
  `CredentialId`. Keys whose name ends in `Token`, `Password`, `Secret`, `ApiKey` or `Credential`,
  or is `Pat`, are treated as secrets by settings export and import (`UserConfigPortability`) and
  never leave the PC.

## Rules

- **Never log, toast, show or put in an exception a secret or a whole request's headers.** Name the
  credential instead.
- **Never put a secret in a URL.** Query strings end up in logs and browser history.
- **Never copy a secret into `UserConfig`, a file, or an environment variable.**

## FE-Buddy's own GitHub requests

- **What uses it.** The update check, News and the update download try GitHub anonymously first,
  and retry once with a token only if that fails (`GitHubAuth`).
- **Where the token comes from.** The GitHub credential the user marked "use for FE-Buddy's update
  checks, News and update downloads"; only one can be marked.
- **The old environment variable.** FE-Buddy used to read the token from the
  `FEBUDDY_GITHUB_TOKEN` environment variable.
  - It no longer does: Windows keeps environment variables as plain text that every program can
    read.
  - When the variable is set, Settings ▸ Credentials offers to move it into a credential and remove
    the variable.
