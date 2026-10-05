# Credentials

How FE-Buddy keeps user names, passwords, GitHub tokens and API keys, and how a feature that
downloads from a protected website uses one.

## Where credentials live

- **In Windows Credential Manager**, never in `UserConfig.json`, a settings export or any other
  FE-Buddy file. Windows encrypts each entry with the signed-in user's key, so only that user on
  that PC can read it. Entries use local-machine persistence, so they never roam.
- **One entry per credential**, named `FE-Buddy:credential:<id>`, holding its name, type, user name,
  websites and secret. Users can see them in Control Panel ▸ Credential Manager.
- **Settings save only the credential's id.** An id means nothing on another PC, so an export leaves
  it out. An import keeps this PC's own choice for FE-Buddy's GitHub token, and for a custom alias
  file's credential only where that file is unchanged.
- **Users manage them in Settings ▸ Credentials:** add, edit, remove, remove all, and **Check** (asks
  GitHub whether a token still works). A saved secret is never shown again.
- **A full uninstall removes them** (the `RemoveFeBuddyCredentials` custom action, after
  `InstallFinalize`, so a cancelled or failed uninstall keeps them); an upgrade never does. One it
  can't remove is logged, with a warning saying how to delete it by hand.

## Types

| Type | Sent as |
|---|---|
| User name and password | `Authorization: Basic …` |
| GitHub personal access token | `Authorization: Bearer …` |
| Token or API key | `Authorization: Bearer …` |

`CredentialStore.Validate` checks every credential, from the editor or from code: a unique name of
at most 100 characters (`CredentialStore.MaxNameLength`); for a user name and password, no colon in
the user name; a secret when adding one or changing the type; and at least one website.

## Websites

**Every credential names the websites it may be sent to, and FE-Buddy sends it nowhere else.**

- A website covers itself and its subdomains: `github.com` covers `api.github.com`, not
  `github.com.example.net`.
- Websites are saved bare and lower case (`https://Files.Example.com/x` becomes `files.example.com`),
  with at least one dot, so a whole top-level domain can never be allowed.
- A GitHub token starts with `github.com, githubusercontent.com`.
- A credential is only ever sent over **HTTPS**.

That is what makes sharing settings safe: a settings file pointing a download at someone else's
website can never collect a user's token.

## Using a credential in a feature

Everything goes through `CredentialStore` (`FeBuddy.Core/Infrastructure/Credentials`); a feature
never reads or stores a secret itself. Concatenate Aliases is the working example:
`ConcatenateAliasesViewModel` offers the drop-down and saves `Sources.<n>.CredentialId`, and
`AliasSourceLoader` downloads with it.

```csharp
CredentialStore store = CredentialStore.Default;

// 1. Offer the user's credentials. CredentialInfo has no secret.
foreach (CredentialInfo info in store.List())
{
    string label = $"{info.Name} ({info.Kind.DisplayName()})"; // "ZOB GitHub (GitHub personal access token)"
}

// 2. Save only the id with the feature's settings, e.g. "Sources.1.CredentialId" = info.Id.ToString("N").

// 3. When downloading, let the store add the header.
using HttpRequestMessage request = new(HttpMethod.Get, url);
switch (store.Authorize(request, credentialId))
{
    case CredentialUseResult.Applied:        break;   // send it
    case CredentialUseResult.NotFound:       /* removed, or chosen on another PC: ask the user to pick one */ break;
    case CredentialUseResult.HostNotAllowed: /* not one of its websites: don't send it anonymously by surprise */ break;
    case CredentialUseResult.NotHttps:       /* refuse: it would travel unencrypted */ break;
}
```

- **Refresh lists on change:** `store.Changed` is raised whenever a credential is saved or removed.
- **Redirects are safe:** .NET drops the `Authorization` header on a redirect.
- **Private GitHub files:** request
  `https://api.github.com/repos/{owner}/{repo}/contents/{path}?ref={branch}` with
  `Accept: application/vnd.github.raw`; `GitHubFileUrl.ToContentsApi` turns a `github.com` or
  `raw.githubusercontent.com` file address into it. `raw.githubusercontent.com` doesn't reliably
  honour a token.
- **Name settings keys after what they hold.** A key holding a credential id ends in `CredentialId`.
  A key ending in `Token`, `Password`, `Secret`, `ApiKey`, `Credential` or `Credentials`, called
  `Pat`, or under `Secrets` is treated as a secret and never leaves the PC
  ([how keys travel](Settings-Reference.md#settings-export-and-import)).

## Rules

- **Never log, toast, show or put in an exception a secret** or a request's headers. Name the
  credential instead.
- **Never put a secret in a URL.** `UrlSecrets.Describe` finds a user name and password or a token
  in an address the user types; Concatenate Aliases refuses one.
- **Never copy a secret into `UserConfig`, a file or an environment variable.**
- **Never let a record print a secret:** a record's `ToString` prints every member, so one holding a
  secret must leave it out, as `CredentialDraft` does.
- **Keep secrets short-lived:** `CredentialStore` lists credentials without turning secrets into
  text, clears every byte buffer they pass through, and the editor reads its password box once,
  when saving.

## FE-Buddy's own GitHub requests

- The update check, News and the update download (`GitHubAuth`) need no token: the repository is
  public. A token only lifts GitHub's limit of 60 requests an hour.
- Settings ▸ FE-Buddy's GitHub Requests: *Don't use a GitHub token* (the default) or *Use a GitHub
  token for update checks, News and update downloads*. Only the id is saved, in
  `General.FeBuddyGitHub.CredentialId`, and only a GitHub token allowed on `api.github.com` can be
  chosen.
- With one chosen, News goes through the Contents API and the update download through the
  release-assets API, both of which honour a token. Only `GitHubAuth.TryAuthorize` puts it on one of
  these requests, through the store's checks.
- A request with the token that fails in any way is tried once more without it, and a token Windows
  can't read is treated as none, so neither an expired token nor a broken Credential Manager ever
  stops updates.
- FE-Buddy 3 never reads 2.x's `FEBUDDY_GITHUB_TOKEN` variable. At launch it only checks whether the
  name is set (`LegacyGitHubTokenVariable`) and, if so, tells the user once how to delete it.
