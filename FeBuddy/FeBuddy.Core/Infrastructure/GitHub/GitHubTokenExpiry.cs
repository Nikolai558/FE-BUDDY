using System.ComponentModel;
using System.Globalization;

using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;
using FeBuddy.Core.Infrastructure.GitHub.Models;
using FeBuddy.Core.Infrastructure.Http;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Core.Infrastructure.GitHub;

/// <summary>
/// When a GitHub token expires, and the warning FE-Buddy gives in the <see cref="WarningDays"/> days
/// before. GitHub puts the date on every answer to a request made with a token that has one, in the
/// <c>GitHub-Authentication-Token-Expiration</c> header.
/// </summary>
/// <remarks>
/// The warning shows in an AIRAC run that downloads a custom alias file with the token, in
/// Settings ▸ Credentials ▸ <b>Check</b>, and in the shell's Systems popover, which checks every
/// saved GitHub token in the background once launch is done (<see cref="FindTokensNeedingAttentionAsync"/>).
/// </remarks>
public static class GitHubTokenExpiry
{
	/// <summary>How many days before a token expires FE-Buddy starts warning about it.</summary>
	public const int WarningDays = 57;

	/// <summary>The header GitHub puts the token's expiry date in.</summary>
	internal const string HeaderName = "GitHub-Authentication-Token-Expiration";

	private const string LogSource = "GitHub";

	/// <summary>When the token a request was sent with expires, from GitHub's answer.</summary>
	/// <param name="response">GitHub's answer.</param>
	/// <returns>The expiry, or <see langword="null"/> when the answer doesn't give one.</returns>
	public static DateTimeOffset? Read(HttpResponseMessage response)
	{
		ArgumentNullException.ThrowIfNull(response);

		return response.Headers.TryGetValues(HeaderName, out IEnumerable<string>? values)
			? Parse(values.FirstOrDefault())
			: null;
	}

	/// <summary>
	/// Reads the header's value, e.g. <c>2026-12-01 05:00:00 UTC</c> or <c>2026-12-01 05:00:00 -0800</c>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The date, or <see langword="null"/> when it isn't one.</returns>
	internal static DateTimeOffset? Parse(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		string text = value.Trim();

		if (text.EndsWith(" UTC", StringComparison.OrdinalIgnoreCase))
		{
			text = text[..^4] + " +00:00";
		}

		return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset date)
			? date
			: null;
	}

	/// <summary>How many days are left before <paramref name="expiresAt"/>, counted in calendar days on this PC.</summary>
	/// <param name="expiresAt">When the token expires.</param>
	/// <param name="now">Now.</param>
	/// <returns>The days; 0 on the day it expires, and less once it has.</returns>
	public static int DaysLeft(DateTimeOffset expiresAt, DateTimeOffset now) =>
		(expiresAt.ToLocalTime().Date - now.ToLocalTime().Date).Days;

	/// <summary>Whether a token expiring at <paramref name="expiresAt"/> is within <see cref="WarningDays"/> days of it.</summary>
	/// <param name="expiresAt">When the token expires, or <see langword="null"/> when it doesn't.</param>
	/// <param name="now">Now.</param>
	/// <returns>Whether to warn about it.</returns>
	public static bool IsSoon(DateTimeOffset? expiresAt, DateTimeOffset now) =>
		expiresAt is { } expires && DaysLeft(expires, now) <= WarningDays;

	/// <summary>When the token expires, in words, e.g. <c>expires in 12 days, on 23 Oct 2026</c>.</summary>
	/// <param name="expiresAt">When the token expires.</param>
	/// <param name="now">Now.</param>
	/// <returns>The words.</returns>
	public static string DescribeWhen(DateTimeOffset expiresAt, DateTimeOffset now)
	{
		int days = DaysLeft(expiresAt, now);
		string date = expiresAt.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture);

		return days switch
		{
			< 0 => $"expired on {date}",
			0 => "expires today",
			1 => "expires tomorrow",
			_ => $"expires in {days} days, on {date}",
		};
	}

	/// <summary>
	/// The warning for a token that expires within <see cref="WarningDays"/> days, telling the user what
	/// to do; <see langword="null"/> for one that doesn't.
	/// </summary>
	/// <param name="credentialName">The credential's name.</param>
	/// <param name="expiresAt">When the token expires, or <see langword="null"/> when it doesn't.</param>
	/// <param name="now">Now.</param>
	/// <returns>The warning, or <see langword="null"/>.</returns>
	public static string? Warning(string credentialName, DateTimeOffset? expiresAt, DateTimeOffset now) =>
		expiresAt is { } expires && IsSoon(expires, now)
			? $"The GitHub token '{credentialName}' {DescribeWhen(expires, now)}. Make a new token on GitHub and paste it into '{credentialName}' in Settings ▸ Credentials."
			: null;

	/// <summary>
	/// Checks every saved GitHub token with GitHub, one after another, and returns the ones GitHub
	/// rejected or that expire within <see cref="WarningDays"/> days. A token not allowed on GitHub's
	/// API is skipped. Never throws for Windows Credential Manager or the network: what can't be
	/// checked is logged and left out.
	/// </summary>
	/// <param name="store">The saved credentials.</param>
	/// <param name="httpClient">The client to use; <see langword="null"/> creates one.</param>
	/// <param name="cancellationToken">Cancels the checks.</param>
	/// <returns>The tokens that need attention, by name.</returns>
	public static async Task<IReadOnlyList<GitHubTokenStatus>> FindTokensNeedingAttentionAsync(
		CredentialStore store,
		HttpClient? httpClient = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(store);

		IReadOnlyList<CredentialInfo> saved;

		try
		{
			saved = store.List();
		}
		catch (Win32Exception ex)
		{
			AppLog.Warning(LogSource, $"Could not read Windows Credential Manager to check the GitHub tokens: {ex.Message}");
			return [];
		}

		CredentialInfo[] tokens = [.. saved.Where(info => info.Kind == CredentialKind.GitHubToken && CredentialHosts.Allows(info.Hosts, CredentialHosts.GitHubApiHost))];

		if (tokens.Length == 0)
		{
			return [];
		}

		using HttpClient? owned = httpClient is null ? FeBuddyHttp.CreateClient(TimeSpan.FromSeconds(10)) : null;
		HttpClient client = httpClient ?? owned!;
		List<GitHubTokenStatus> found = [];

		foreach (CredentialInfo token in tokens)
		{
			CredentialCheck check;

			try
			{
				check = await GitHubAuth.CheckTokenAsync(store, token.Id, client, cancellationToken).ConfigureAwait(false);
			}
			catch (Win32Exception ex)
			{
				AppLog.Warning(LogSource, $"Could not read the credential '{token.Name}' to check it: {ex.Message}");
				continue;
			}

			if (check.IsRejected || check.ExpiresSoon)
			{
				found.Add(new GitHubTokenStatus(token.Name, check.ExpiresAt, check.IsRejected));
			}
		}

		return found;
	}
}
