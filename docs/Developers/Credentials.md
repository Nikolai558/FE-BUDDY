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
  credential: name, type, user name, websites and the secret.
  - Users can see or delete the entries in Control Panel ▸ Credential Manager ▸ Windows Credentials.
- **Settings save only the credential's id.** On another PC the id matches nothing, so an imported
  or copied config never carries a secret, and the user picks one of their own credentials.
- **Users manage them in Settings ▸ Credentials.**
  - Add, edit and remove. A saved secret is never shown again.
  - "Check" asks GitHub whether a GitHub token still works.
  - "Remove all" clears every FE-Buddy credential.
- **A full uninstall removes them** (custom action `RemoveFeBuddyCredentials`); an upgrade never
  does. The action runs after `InstallFinalize`, once the uninstall has succeeded, so an uninstall
  that is cancelled or fails keeps them.

## Types

| Type | Sent as |
|---|---|
| User name and password | `Authorization: Basic …` |
| GitHub personal access token | `Authorization: Bearer …` |
| Token or API key | `Authorization: Bearer …` |

`CredentialStore.Validate` checks every credential, however it is saved - from the editor or from
code:
- A name, of at most 100 characters (`CredentialStore.MaxNameLength`), not used by another credential.
- For a user name and password, a user name without a colon: Basic sends `user:password`, so a
  colon would split it in the wrong place.
- The secret, when adding one or changing an existing credential's type.
- At least one website, each one read the same way as in the editor (see below).

## Websites

**Every credential names the websites it may be sent to, and FE-Buddy sends it nowhere else.**
- A website covers itself and its subdomains: `github.com` covers `api.github.com`, but not
  `github.com.example.net`.
- Websites are saved bare and lower case (`https://Files.Example.com/x` becomes
  `files.example.com`), and each needs at least one dot, so a whole top-level domain such as `com`
  can never be allowed.
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
  `CredentialId`. Keys whose name ends in `Token`, `Password`, `Secret`, `ApiKey`, `Credential` or
  `Credentials`, is `Pat`, or sits under `Secrets`, are treated as secrets by settings export and
  import (`UserConfigPortability`) and never leave the PC.

## Rules

- **Never log, toast, show or put in an exception a secret or a whole request's headers.** Name the
  credential instead.
- **Never put a secret in a URL.** Query strings end up in logs and browser history.
- **Never copy a secret into `UserConfig`, a file, or an environment variable.**
- **Never let a record print a secret.** A record's generated `ToString` prints every member, so a
  record that holds one must leave it out, as `CredentialDraft` does. `CredentialStore` keeps its
  own secret-holding types as plain classes for the same reason.
- **Keep secrets short-lived.** `CredentialStore` lists credentials without turning their secrets
  into text, clears every byte buffer an entry passes through, and the credential editor reads its
  password box once, when saving.

## FE-Buddy's own GitHub requests

- **What uses it.** The update check, News and the update download (`GitHubAuth`). None of them
  needs a token: the repository is public.
- **Whether to use one.** An advanced setting, Settings ▸ FE-Buddy's GitHub Requests: "Don't use a
  GitHub token" (the default) or "Use a GitHub token" and which one.
  - Only the credential's id is saved, in `General.FeBuddyGitHub.CredentialId`. It is a local
    setting, so a settings export leaves it out.
  - Only a GitHub personal access token whose websites cover `api.github.com` (`github.com` does)
    can be chosen.
- **How it is used.** With a token chosen, the requests are sent with it: News through the Contents
  API and the update download through the release-assets API, since both honour a token.
  - Only `GitHubAuth.TryAuthorize` puts the token on a request, through the store's own checks
    (a GitHub token, HTTPS, one of its websites). Nothing reads the token out.
  - A request with the token that fails in any way (refused, cut short, no answer) is tried once
    more without it.
  - A token Windows Credential Manager cannot read is logged and treated as no token.
  - So neither an expired token nor a broken Credential Manager ever stops updates.
- **No environment variable.** FE-Buddy 3 never reads `FEBUDDY_GITHUB_TOKEN`, which 2.x told users
  to set. At launch it only checks whether that name is set (`LegacyGitHubTokenVariable` reads the
  registry's variable names, never the value) and, if it is, tells the user once how to delete it
  (`LegacyGitHubTokenNotice`, then `General.LegacyGitHubTokenNoticeShown` is saved).
