using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using FeBuddy.Core.Infrastructure.Credentials.Models;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Core.Infrastructure.Credentials;

/// <summary>
/// The user's saved credentials - user names and passwords, GitHub tokens, API keys - for
/// downloading from protected websites. Kept in Windows Credential Manager, never in
/// <c>UserConfig.json</c>, a settings export or any other FE-Buddy file.
/// </summary>
/// <remarks>
/// <para>
/// Everything about a credential - its name, kind, user name, websites and secret - is one
/// Credential Manager entry named <see cref="TargetPrefix"/> plus its id. Settings that use a
/// credential save only that id, which means nothing on another PC.
/// </para>
/// <para>
/// A secret goes in and never comes back out: <see cref="List"/> and <see cref="Find(Guid)"/>
/// return <see cref="CredentialInfo"/>, which has no secret, and the only way to use one is
/// <see cref="Authorize(HttpRequestMessage, Guid)"/>, which adds it to a request - and only to an
/// HTTPS request for one of the credential's own websites (<see cref="CredentialHosts"/>). Nothing
/// here logs a secret, and nothing that uses a credential should either.
/// </para>
/// </remarks>
public sealed class CredentialStore
{
	/// <summary>The start of every FE-Buddy entry's name in Credential Manager. The uninstaller removes entries by it too.</summary>
	public const string TargetPrefix = "FE-Buddy:credential:";

	private const string LogSource = "Credentials";

	private static readonly Lazy<CredentialStore> DefaultStore = new(() => new CredentialStore(new WindowsCredentialVault()));

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { new JsonStringEnumConverter() },
	};

	private readonly ICredentialVault _vault;

	/// <summary>Creates a store over <paramref name="vault"/>.</summary>
	/// <param name="vault">Where the entries live.</param>
	public CredentialStore(ICredentialVault vault)
	{
		ArgumentNullException.ThrowIfNull(vault);
		_vault = vault;
	}

	/// <summary>The store over this user's Windows Credential Manager.</summary>
	public static CredentialStore Default => DefaultStore.Value;

	/// <summary>Raised after a credential is saved or removed, so lists of credentials can refresh.</summary>
	public event EventHandler? Changed;

	/// <summary>Every saved credential, by name.</summary>
	/// <returns>The credentials, without their secrets.</returns>
	public IReadOnlyList<CredentialInfo> List() =>
		[.. ReadAll().Select(s => s.Info).OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase)];

	/// <summary>One saved credential.</summary>
	/// <param name="id">The credential's id.</param>
	/// <returns>The credential without its secret, or <see langword="null"/> when there is none with that id.</returns>
	public CredentialInfo? Find(Guid id) => ReadOne(id)?.Info;

	/// <summary>Checks a draft before <see cref="Save(CredentialDraft)"/>.</summary>
	/// <param name="draft">The credential being added or edited.</param>
	/// <returns>What is wrong with it, for the user; <see langword="null"/> when it can be saved.</returns>
	public string? Validate(CredentialDraft draft)
	{
		ArgumentNullException.ThrowIfNull(draft);

		string name = draft.Name?.Trim() ?? string.Empty;
		StoredCredential? existing = draft.Id is { } id ? ReadOne(id) : null;

		if (draft.Id is not null && existing is null)
		{
			return "This credential no longer exists. It may have been removed.";
		}

		if (name.Length == 0)
		{
			return "Give the credential a name.";
		}

		if (ReadAll().Any(s => s.Info.Id != draft.Id && string.Equals(s.Info.Name, name, StringComparison.CurrentCultureIgnoreCase)))
		{
			return $"There is already a credential called '{name}'.";
		}

		if (draft.Kind == CredentialKind.UsernamePassword && string.IsNullOrWhiteSpace(draft.UserName))
		{
			return "Enter the user name.";
		}

		if (string.IsNullOrWhiteSpace(draft.Secret) && (existing is null || existing.Info.Kind != draft.Kind))
		{
			return draft.Kind == CredentialKind.UsernamePassword ? "Enter the password." : "Enter the token.";
		}

		if (draft.Hosts.Count == 0)
		{
			return "Name at least one website it may be used with.";
		}

		return Serialize(ToStored(draft, existing)).Length > WindowsCredentialVault.MaxSecretBytes
			? "That is too long to store. Check the token or password, the name and the websites."
			: null;
	}

	/// <summary>
	/// Adds a new credential or replaces an edited one.
	/// </summary>
	/// <param name="draft">The credential; check it with <see cref="Validate(CredentialDraft)"/> first.</param>
	/// <returns>The saved credential.</returns>
	/// <exception cref="ArgumentException">The draft is not valid.</exception>
	public CredentialInfo Save(CredentialDraft draft)
	{
		if (Validate(draft) is { } error)
		{
			throw new ArgumentException(error, nameof(draft));
		}

		StoredCredential stored = ToStored(draft, draft.Id is { } id ? ReadOne(id) : null);

		Write(stored);
		AppLog.Info(LogSource, $"Saved the credential '{stored.Info.Name}'.");
		Changed?.Invoke(this, EventArgs.Empty);
		return stored.Info;
	}

	/// <summary>Removes one credential.</summary>
	/// <param name="id">The credential's id.</param>
	/// <returns><see langword="true"/> if it existed.</returns>
	public bool Delete(Guid id)
	{
		bool removed = _vault.Delete(Target(id));

		if (removed)
		{
			AppLog.Info(LogSource, "Removed a credential.");
			Changed?.Invoke(this, EventArgs.Empty);
		}

		return removed;
	}

	/// <summary>Removes every FE-Buddy credential from this PC.</summary>
	/// <returns>How many were removed.</returns>
	public int DeleteAll()
	{
		int removed = _vault.Enumerate(TargetPrefix).Count(entry => _vault.Delete(entry.Target));

		AppLog.Info(LogSource, $"Removed all {removed} credential(s).");
		Changed?.Invoke(this, EventArgs.Empty);
		return removed;
	}

	/// <summary>
	/// Adds a credential to a request as its <c>Authorization</c> header - Basic for a user name
	/// and password, Bearer for a token - but only when the request is HTTPS and goes to one of
	/// the credential's websites. Otherwise the request is left untouched.
	/// </summary>
	/// <remarks>
	/// .NET drops the <c>Authorization</c> header when a request is redirected, so a download that
	/// redirects elsewhere (as GitHub's do) never carries the credential off its website.
	/// </remarks>
	/// <param name="request">The request to send.</param>
	/// <param name="id">The credential's id.</param>
	/// <returns>Whether the credential was added, and why not when it was not.</returns>
	public CredentialUseResult Authorize(HttpRequestMessage request, Guid id)
	{
		ArgumentNullException.ThrowIfNull(request);

		if (ReadOne(id) is not { } stored)
		{
			return CredentialUseResult.NotFound;
		}

		if (request.RequestUri is not { IsAbsoluteUri: true } uri || uri.Scheme != Uri.UriSchemeHttps)
		{
			return CredentialUseResult.NotHttps;
		}

		if (!CredentialHosts.Allows(stored.Info.Hosts, uri.Host))
		{
			return CredentialUseResult.HostNotAllowed;
		}

		request.Headers.Authorization = stored.Info.Kind == CredentialKind.UsernamePassword
			? new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{stored.Info.UserName}:{stored.Secret}")))
			: new AuthenticationHeaderValue("Bearer", stored.Secret);

		return CredentialUseResult.Applied;
	}

	/// <summary>
	/// A GitHub token's secret, for FE-Buddy's own GitHub requests - update checks, News and update
	/// downloads, which only ever call GitHub's API. Only a <see cref="CredentialKind.GitHubToken"/>
	/// whose websites include GitHub's API gives one.
	/// </summary>
	/// <param name="id">The credential the user chose in Settings.</param>
	/// <returns>The token, or <see langword="null"/> when there is no such GitHub token.</returns>
	internal string? GetGitHubToken(Guid id) =>
		ReadOne(id) is { Info.Kind: CredentialKind.GitHubToken } stored
			&& CredentialHosts.Allows(stored.Info.Hosts, CredentialHosts.GitHubApiHost)
			? stored.Secret
			: null;

	private static string Target(Guid id) => TargetPrefix + id.ToString("N");

	private static StoredCredential ToStored(CredentialDraft draft, StoredCredential? existing)
	{
		string secret = string.IsNullOrWhiteSpace(draft.Secret)
			? existing?.Secret ?? string.Empty
			: draft.Kind == CredentialKind.UsernamePassword ? draft.Secret : draft.Secret.Trim();

		CredentialInfo info = new(
			draft.Id ?? Guid.NewGuid(),
			draft.Name.Trim(),
			draft.Kind,
			draft.Kind == CredentialKind.UsernamePassword ? draft.UserName?.Trim() : null,
			[.. draft.Hosts]);

		return new StoredCredential(info, secret);
	}

	private static byte[] Serialize(StoredCredential stored) =>
		JsonSerializer.SerializeToUtf8Bytes(
			new Payload(1, stored.Info.Name, stored.Info.Kind, stored.Info.UserName, stored.Info.Hosts, stored.Secret),
			JsonOptions);

	private void Write(StoredCredential stored) =>
		_vault.Write(Target(stored.Info.Id), stored.Info.Name, Serialize(stored));

	private StoredCredential? ReadOne(Guid id) =>
		_vault.Read(Target(id)) is { } bytes ? Deserialize(id, bytes) : null;

	private List<StoredCredential> ReadAll()
	{
		List<StoredCredential> all = [];

		foreach (VaultEntry entry in _vault.Enumerate(TargetPrefix))
		{
			if (Guid.TryParseExact(entry.Target[TargetPrefix.Length..], "N", out Guid id) && Deserialize(id, entry.Secret) is { } stored)
			{
				all.Add(stored);
			}
		}

		return all;
	}

	/// <summary>Reads one entry; an entry that cannot be read is skipped and logged - by id only, never by content.</summary>
	private static StoredCredential? Deserialize(Guid id, byte[] bytes)
	{
		try
		{
			if (JsonSerializer.Deserialize<Payload>(bytes, JsonOptions) is { Name.Length: > 0, Secret: not null, Hosts: not null } p
				&& Enum.IsDefined(p.Kind))
			{
				return new StoredCredential(new CredentialInfo(id, p.Name, p.Kind, p.UserName, p.Hosts), p.Secret);
			}
		}
		catch (JsonException)
		{
			// Logged below.
		}

		AppLog.Warning(LogSource, $"Skipped the unreadable credential {id:N}.");
		return null;
	}

	/// <summary>A credential with its secret; never leaves this class.</summary>
	private sealed record StoredCredential(CredentialInfo Info, string Secret);

	/// <summary>What is stored in each Credential Manager entry.</summary>
	private sealed record Payload(
		int Version,
		string Name,
		CredentialKind Kind,
		string? UserName,
		IReadOnlyList<string> Hosts,
		string Secret);
}
