using System.Net.Http.Headers;
using System.Security.Cryptography;
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
/// return <see cref="CredentialInfo"/>, which has no secret, and read each entry without ever
/// turning its secret into text. The only way to use one is
/// <see cref="Authorize(HttpRequestMessage, Guid)"/>, which adds it to a request - and only to an
/// HTTPS request for one of the credential's own websites (<see cref="CredentialHosts"/>). Every
/// byte buffer an entry passes through is cleared once read or written. Nothing here logs a secret,
/// and nothing that uses a credential should either.
/// </para>
/// </remarks>
public sealed class CredentialStore
{
	/// <summary>The start of every FE-Buddy entry's name in Credential Manager. The uninstaller removes entries by it too.</summary>
	public const string TargetPrefix = "FE-Buddy:credential:";

	/// <summary>The longest name a credential may have.</summary>
	public const int MaxNameLength = 100;

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
		[.. ReadAllInfo().OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase)];

	/// <summary>One saved credential.</summary>
	/// <param name="id">The credential's id.</param>
	/// <returns>The credential without its secret, or <see langword="null"/> when there is none with that id.</returns>
	public CredentialInfo? Find(Guid id)
	{
		if (_vault.Read(Target(id)) is not { } bytes)
		{
			return null;
		}

		try
		{
			return ReadInfo(id, bytes);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(bytes);
		}
	}

	/// <summary>Checks a draft before <see cref="Save(CredentialDraft)"/>.</summary>
	/// <param name="draft">The credential being added or edited.</param>
	/// <returns>What is wrong with it, for the user; <see langword="null"/> when it can be saved.</returns>
	public string? Validate(CredentialDraft draft) => Check(draft, out _);

	/// <summary>
	/// Adds a new credential or replaces an edited one.
	/// </summary>
	/// <param name="draft">The credential; check it with <see cref="Validate(CredentialDraft)"/> first.</param>
	/// <returns>The saved credential.</returns>
	/// <exception cref="ArgumentException">The draft is not valid.</exception>
	public CredentialInfo Save(CredentialDraft draft)
	{
		string? error = Check(draft, out StoredCredential? stored);

		if (stored is null)
		{
			throw new ArgumentException(error, nameof(draft));
		}

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
		int removed = 0;

		foreach (VaultEntry entry in _vault.Enumerate(TargetPrefix))
		{
			CryptographicOperations.ZeroMemory(entry.Secret);

			if (_vault.Delete(entry.Target))
			{
				removed++;
			}
		}

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

		return ReadOne(id) is { } stored ? Apply(request, stored) : CredentialUseResult.NotFound;
	}

	/// <summary>
	/// Adds a GitHub token to one of FE-Buddy's own GitHub requests - update checks, News and update
	/// downloads - the way <see cref="Authorize(HttpRequestMessage, Guid)"/> does, but only when the
	/// credential is a <see cref="CredentialKind.GitHubToken"/>.
	/// </summary>
	/// <param name="request">The request to send.</param>
	/// <param name="id">The credential the user chose in Settings.</param>
	/// <returns>Whether the token was added.</returns>
	internal bool AuthorizeGitHubToken(HttpRequestMessage request, Guid id)
	{
		ArgumentNullException.ThrowIfNull(request);

		return ReadOne(id) is { Info.Kind: CredentialKind.GitHubToken } stored
			&& Apply(request, stored) == CredentialUseResult.Applied;
	}

	private static string Target(Guid id) => TargetPrefix + id.ToString("N");

	/// <summary>
	/// <see cref="Validate(CredentialDraft)"/>'s checks. When the draft passes,
	/// <paramref name="stored"/> is what <see cref="Save(CredentialDraft)"/> writes.
	/// </summary>
	private string? Check(CredentialDraft draft, out StoredCredential? stored)
	{
		ArgumentNullException.ThrowIfNull(draft);
		stored = null;

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

		if (name.Length > MaxNameLength)
		{
			return $"Keep the name to {MaxNameLength} characters or fewer.";
		}

		if (ReadAllInfo().Any(i => i.Id != draft.Id && string.Equals(i.Name, name, StringComparison.CurrentCultureIgnoreCase)))
		{
			return $"There is already a credential called '{name}'.";
		}

		if (draft.Kind == CredentialKind.UsernamePassword)
		{
			if (string.IsNullOrWhiteSpace(draft.UserName))
			{
				return "Enter the user name.";
			}

			// A Basic sign-in is sent as "user:password", so a colon in the user name would split it in the wrong place.
			if (draft.UserName.Contains(':', StringComparison.Ordinal))
			{
				return "A user name cannot contain a colon (:).";
			}
		}

		if (string.IsNullOrWhiteSpace(draft.Secret) && (existing is null || existing.Info.Kind != draft.Kind))
		{
			return draft.Kind == CredentialKind.UsernamePassword ? "Enter the password." : "Enter the token.";
		}

		if (!TryNormalizeHosts(draft, out IReadOnlyList<string> hosts, out string? hostError))
		{
			return hostError;
		}

		if (hosts.Count == 0)
		{
			return "Name at least one website it may be used with.";
		}

		StoredCredential candidate = ToStored(draft, existing, hosts);
		byte[] payload = Serialize(candidate);
		bool tooLong = payload.Length > WindowsCredentialVault.MaxSecretBytes;
		CryptographicOperations.ZeroMemory(payload);

		if (tooLong)
		{
			return "That is too long to store. Check the token or password, the name and the websites.";
		}

		stored = candidate;
		return null;
	}

	/// <summary>
	/// The draft's websites read the way <see cref="CredentialHosts.TryParse"/> reads what a user
	/// types - bare hosts, lower case, no duplicates - so a credential saved by code meets the same
	/// rules as one saved in the editor: a whole top-level domain such as <c>com</c> is refused.
	/// </summary>
	private static bool TryNormalizeHosts(CredentialDraft draft, out IReadOnlyList<string> hosts, out string? error) =>
		CredentialHosts.TryParse(string.Join(',', draft.Hosts), out hosts, out error);

	private static StoredCredential ToStored(CredentialDraft draft, StoredCredential? existing, IReadOnlyList<string> hosts)
	{
		string secret = string.IsNullOrWhiteSpace(draft.Secret)
			? existing?.Secret ?? string.Empty
			: draft.Kind == CredentialKind.UsernamePassword ? draft.Secret : draft.Secret.Trim();

		CredentialInfo info = new(
			draft.Id ?? Guid.NewGuid(),
			draft.Name.Trim(),
			draft.Kind,
			draft.Kind == CredentialKind.UsernamePassword ? draft.UserName?.Trim() : null,
			hosts);

		return new StoredCredential(info, secret);
	}

	/// <summary>Adds <paramref name="stored"/> to <paramref name="request"/> when it may go there.</summary>
	private static CredentialUseResult Apply(HttpRequestMessage request, StoredCredential stored)
	{
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

	private static byte[] Serialize(StoredCredential stored) =>
		JsonSerializer.SerializeToUtf8Bytes(
			new Payload
			{
				Version = 1,
				Name = stored.Info.Name,
				Kind = stored.Info.Kind,
				UserName = stored.Info.UserName,
				Hosts = stored.Info.Hosts,
				Secret = stored.Secret,
			},
			JsonOptions);

	private void Write(StoredCredential stored)
	{
		byte[] payload = Serialize(stored);

		try
		{
			_vault.Write(Target(stored.Info.Id), stored.Info.Name, payload);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(payload);
		}
	}

	/// <summary>One entry with its secret, for using or editing it.</summary>
	private StoredCredential? ReadOne(Guid id)
	{
		if (_vault.Read(Target(id)) is not { } bytes)
		{
			return null;
		}

		try
		{
			return Deserialize<Payload>(bytes) is { Name.Length: > 0, Secret: not null, Hosts: not null } p && Enum.IsDefined(p.Kind)
				? new StoredCredential(new CredentialInfo(id, p.Name, p.Kind, p.UserName, p.Hosts), p.Secret)
				: Unreadable<StoredCredential>(id);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(bytes);
		}
	}

	/// <summary>Every entry, without the secrets.</summary>
	private List<CredentialInfo> ReadAllInfo()
	{
		List<CredentialInfo> all = [];

		foreach (VaultEntry entry in _vault.Enumerate(TargetPrefix))
		{
			if (Guid.TryParseExact(entry.Target[TargetPrefix.Length..], "N", out Guid id) && ReadInfo(id, entry.Secret) is { } info)
			{
				all.Add(info);
			}

			CryptographicOperations.ZeroMemory(entry.Secret);
		}

		return all;
	}

	/// <summary>One entry without its secret, which is only checked to be there.</summary>
	private static CredentialInfo? ReadInfo(Guid id, byte[] bytes) =>
		Deserialize<PayloadInfo>(bytes) is { Name.Length: > 0, HasSecret: true, Hosts: not null } p && Enum.IsDefined(p.Kind)
			? new CredentialInfo(id, p.Name, p.Kind, p.UserName, p.Hosts)
			: Unreadable<CredentialInfo>(id);

	private static T? Deserialize<T>(byte[] bytes)
		where T : class
	{
		try
		{
			return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	/// <summary>An entry that cannot be read is skipped and logged - by id only, never by content.</summary>
	private static T? Unreadable<T>(Guid id)
		where T : class
	{
		AppLog.Warning(LogSource, $"Skipped the unreadable credential {id:N}.");
		return null;
	}

	/// <summary>A credential with its secret; never leaves this class. A class, not a record, so it has no <c>ToString</c> that prints the secret.</summary>
	private sealed class StoredCredential(CredentialInfo info, string secret)
	{
		public CredentialInfo Info { get; } = info;

		public string Secret { get; } = secret;
	}

	/// <summary>What is stored in each Credential Manager entry. A class, not a record, so it has no <c>ToString</c> that prints the secret.</summary>
	private sealed class Payload
	{
		public int Version { get; init; }

		public string Name { get; init; } = string.Empty;

		public CredentialKind Kind { get; init; }

		public string? UserName { get; init; }

		public IReadOnlyList<string>? Hosts { get; init; }

		public string? Secret { get; init; }
	}

	/// <summary>
	/// An entry read for a list: everything but the secret, which is only checked to be there - the
	/// JSON reader steps over its text without ever turning it into a string.
	/// </summary>
	private sealed class PayloadInfo
	{
		public string Name { get; init; } = string.Empty;

		public CredentialKind Kind { get; init; }

		public string? UserName { get; init; }

		public IReadOnlyList<string>? Hosts { get; init; }

		[JsonPropertyName("secret")]
		[JsonConverter(typeof(PresenceConverter))]
		public bool HasSecret { get; init; }
	}

	/// <summary>Reads whether a JSON value is a string, without reading the string.</summary>
	private sealed class PresenceConverter : JsonConverter<bool>
	{
		public override bool HandleNull => true;

		public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			bool isString = reader.TokenType == JsonTokenType.String;
			reader.Skip();
			return isString;
		}

		public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) =>
			throw new NotSupportedException("A secret's presence is only ever read.");
	}
}
