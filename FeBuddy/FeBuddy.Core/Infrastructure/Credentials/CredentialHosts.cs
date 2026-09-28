namespace FeBuddy.Core.Infrastructure.Credentials;

/// <summary>
/// The websites a credential may be sent to. Each credential names its hosts, and
/// <see cref="CredentialStore.Authorize(HttpRequestMessage, Guid)"/> refuses any other - so a
/// shared setting that points a download at someone else's site can never collect the user's
/// token or password.
/// </summary>
/// <remarks>
/// A host covers itself and its subdomains: <c>github.com</c> covers <c>api.github.com</c> but not
/// <c>github.com.example.net</c> or <c>notgithub.com</c>. A host needs at least one dot, so a whole
/// top-level domain such as <c>com</c> can never be allowed.
/// </remarks>
public static class CredentialHosts
{
	/// <summary>The hosts a GitHub token starts with: GitHub, its API, and its raw-file and download hosts.</summary>
	public static IReadOnlyList<string> GitHubDefaults { get; } = ["github.com", "githubusercontent.com"];

	/// <summary>The host FE-Buddy's own GitHub requests go to; a token used for them must allow it.</summary>
	public const string GitHubApiHost = "api.github.com";

	private static readonly char[] Separators = [',', ';', ' ', '\t', '\r', '\n'];

	/// <summary>
	/// Reads the hosts a user typed: separated by commas, semicolons or spaces, and each one
	/// either a bare host (<c>github.com</c>) or a web address, of which only the host is kept.
	/// </summary>
	/// <param name="text">What the user typed.</param>
	/// <param name="hosts">The hosts, lower case and without duplicates, when every entry is valid.</param>
	/// <param name="error">Why an entry is not a usable host, for the user; otherwise <see langword="null"/>.</param>
	/// <returns><see langword="true"/> when every entry is a usable host.</returns>
	public static bool TryParse(string? text, out IReadOnlyList<string> hosts, out string? error)
	{
		List<string> parsed = [];

		foreach (string entry in (text ?? string.Empty).Split(Separators, StringSplitOptions.RemoveEmptyEntries))
		{
			if (Normalize(entry) is not { } host)
			{
				hosts = [];
				error = $"'{entry}' is not a website name such as github.com.";
				return false;
			}

			if (!parsed.Contains(host, StringComparer.Ordinal))
			{
				parsed.Add(host);
			}
		}

		hosts = parsed;
		error = null;
		return true;
	}

	/// <summary>Whether a request to <paramref name="host"/> may carry a credential that allows <paramref name="allowed"/>.</summary>
	/// <param name="allowed">The credential's hosts.</param>
	/// <param name="host">The request's host.</param>
	/// <returns><see langword="true"/> when the host is one of them or a subdomain of one.</returns>
	public static bool Allows(IEnumerable<string> allowed, string host)
	{
		ArgumentNullException.ThrowIfNull(allowed);

		string requested = (host ?? string.Empty).TrimEnd('.').ToLowerInvariant();

		return requested.Length > 0
			&& allowed.Any(a => requested == a || requested.EndsWith("." + a, StringComparison.Ordinal));
	}

	/// <summary>One entry as a host, or <see langword="null"/> when it is not one.</summary>
	private static string? Normalize(string entry)
	{
		string text = entry.Trim().TrimEnd('/');

		if (text.Contains("://", StringComparison.Ordinal))
		{
			if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? uri))
			{
				return null;
			}

			text = uri.Host;
		}

		// "*.example.com" means the same as "example.com": subdomains are always covered.
		if (text.StartsWith("*.", StringComparison.Ordinal))
		{
			text = text[2..];
		}

		text = text.TrimEnd('.').ToLowerInvariant();

		return Uri.CheckHostName(text) == UriHostNameType.Dns && text.Contains('.', StringComparison.Ordinal)
			? text
			: null;
	}
}
