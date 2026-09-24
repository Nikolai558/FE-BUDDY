using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Core.Infrastructure.GitHub;
using FeBuddy.Core.Infrastructure.Http;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Platform;
using FeBuddy.Versioning;

namespace FeBuddy.Core.Application.Updates;

/// <summary>
/// Asks GitHub whether a newer FE-Buddy release exists on the user's chosen channel. The
/// launch sequence runs this only to populate the title-bar tooltip and version chip; the
/// actual update is a new MSI installer the user downloads and runs.
/// </summary>
/// <remarks>
/// <para>
/// Versions are the real SemVer versions (<see cref="ProductVersion"/>): release tags are parsed
/// as strict SemVer (an optional leading <c>v</c> is allowed; anything else is skipped) and
/// compared by SemVer precedence, so <c>3.0.0-rc.1 &lt; 3.0.0</c>. Each release's channel comes
/// from its tag alone (<see cref="ProductVersion.Channel"/>) - GitHub's pre-release flag is not
/// consulted, the same rule FE-Buddy 2.x's updater applies. The user's channel is the lowest one
/// they accept: Stable sees stable releases, ReleaseCandidate adds <c>-rc</c>, Beta adds
/// <c>-beta</c>, Alpha sees everything. The check never throws - a network or parse failure
/// yields <see cref="VersionCheckResult.CheckSucceeded"/> <see langword="false"/>.
/// </para>
/// <para>
/// Releases come from the public repository (<see cref="GitHubRepository"/>), where 2.x and
/// 3.x releases share one list. The request is tried unauthenticated first; only if that fails,
/// and only if <see cref="GitHubAuth.EnvironmentVariableName"/> is set, it retries once with
/// that token (see <see cref="GitHubAuth"/> for why the token is a fallback).
/// </para>
/// </remarks>
public static partial class VersionCheck
{
	private const string LogSource = "VersionCheck";
	private const string ReleasesUrl = GitHubRepository.ApiUrl + "/releases?per_page=30";

	// SemVer precedence, for sorting releases newest first.
	private static readonly Comparer<ProductVersion> Precedence = Comparer<ProductVersion>.Create((a, b) => a.ComparePrecedenceTo(b));

	/// <summary>
	/// Runs the version check. Never throws.
	/// </summary>
	/// <param name="currentVersion">The running application's real version (see <see cref="AppVersion"/>).</param>
	/// <param name="channel">The lowest channel the user accepts.</param>
	/// <param name="hasInternetConnection">
	/// If <see langword="false"/>, the check is skipped and returns
	/// <see cref="VersionCheckResult.CheckSucceeded"/> <see langword="false"/>.
	/// </param>
	/// <param name="httpClient">An <see cref="HttpClient"/> to use, or <see langword="null"/> to create one. A test seam.</param>
	/// <param name="cancellationToken">Cancels the network call.</param>
	/// <returns>The comparison result.</returns>
	public static async Task<VersionCheckResult> RunAsync(
		string currentVersion,
		ReleaseChannel channel,
		bool hasInternetConnection,
		HttpClient? httpClient = null,
		CancellationToken cancellationToken = default)
	{
		string current = string.IsNullOrWhiteSpace(currentVersion) ? "0.0.0" : currentVersion.Trim();

		if (!hasInternetConnection)
		{
			AppLog.Info(LogSource, "Offline - skipping the version check. Update state is unknown.");
			return new VersionCheckResult(current, null, false, channel, CheckSucceeded: false, "Offline; update state unknown.");
		}

		using HttpClient? owned = httpClient is null ? FeBuddyHttp.CreateClient(TimeSpan.FromSeconds(10)) : null;
		HttpClient client = httpClient ?? owned!;

		try
		{
			HttpResponseMessage response = await SendReleasesRequestAsync(client, token: null, cancellationToken).ConfigureAwait(false);

			if (!response.IsSuccessStatusCode)
			{
				string? token = GitHubAuth.GetOptionalToken();
				if (token is not null)
				{
					AppLog.Info(LogSource, $"Unauthenticated release check returned {(int)response.StatusCode}; retrying with {GitHubAuth.EnvironmentVariableName}.");
					response.Dispose();
					response = await SendReleasesRequestAsync(client, token, cancellationToken).ConfigureAwait(false);
				}
			}

			using (response)
			{
				if (!response.IsSuccessStatusCode)
				{
					AppLog.Warning(LogSource, $"GitHub releases API returned {(int)response.StatusCode}. Update state is unknown.");
					return new VersionCheckResult(current, null, false, channel, CheckSucceeded: false, $"GitHub API returned {(int)response.StatusCode}.");
				}

				await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
				using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

				ProductVersion.TryParseTag(current, out ProductVersion? currentParsed);
				var candidates = new List<(ProductVersion Parsed, ReleaseSummary Release, ReleaseInstaller? Installer)>();

				foreach (JsonElement release in document.RootElement.EnumerateArray())
				{
					if (release.TryGetProperty("draft", out JsonElement draft) && draft.ValueKind == JsonValueKind.True)
					{
						continue;
					}

					string? tag = release.TryGetProperty("tag_name", out JsonElement tagElement) ? tagElement.GetString() : null;
					if (!ProductVersion.TryParseTag(tag, out ProductVersion? parsed) || parsed!.Channel < channel)
					{
						continue;
					}

					string? body = release.TryGetProperty("body", out JsonElement bodyElement) ? bodyElement.GetString() : null;
					string? url = release.TryGetProperty("html_url", out JsonElement urlElement) ? urlElement.GetString() : null;
					DateTimeOffset? published = release.TryGetProperty("published_at", out JsonElement publishedElement)
						&& publishedElement.ValueKind == JsonValueKind.String
						&& publishedElement.TryGetDateTimeOffset(out DateTimeOffset publishedAt)
							? publishedAt
							: null;

					candidates.Add((parsed, new ReleaseSummary(
						parsed.ToString(), published, parsed.IsPrerelease, StripInstallInstructions(body), url), FindInstaller(release)));
				}

				if (candidates.Count == 0)
				{
					AppLog.Info(LogSource, $"No comparable release found on the {channel} channel.");
					return new VersionCheckResult(current, null, false, channel, CheckSucceeded: true, "No comparable release found.");
				}

				// On a tie the first one listed wins (GitHub lists newest first).
				(ProductVersion best, ReleaseSummary latest, ReleaseInstaller? latestInstaller) = candidates[0];
				foreach ((ProductVersion parsed, ReleaseSummary release, ReleaseInstaller? installer) in candidates)
				{
					if (parsed.ComparePrecedenceTo(best) > 0)
					{
						(best, latest, latestInstaller) = (parsed, release, installer);
					}
				}

				int currentVsBest = currentParsed?.ComparePrecedenceTo(best) ?? 0;
				bool updateAvailable = currentParsed is not null && currentVsBest < 0;
				bool isAheadOfLatest = currentParsed is not null && currentVsBest > 0;

				List<ReleaseSummary> newer = updateAvailable
					? [.. candidates
						.Where(c => c.Parsed.ComparePrecedenceTo(currentParsed!) > 0)
						.OrderByDescending(c => c.Parsed, Precedence)
						.ThenByDescending(c => c.Release.PublishedAt)
						.Select(c => c.Release)]
					: [];

				string message = updateAvailable
					? $"v{latest.Version} available on the {channel} channel ({newer.Count} newer release(s))."
					: isAheadOfLatest
						? $"Running a development build ahead of the latest {channel} release (v{latest.Version})."
						: "You are running the latest version.";

				AppLog.Info(LogSource, message);
				return new VersionCheckResult(
					current, latest.Version, updateAvailable, channel, CheckSucceeded: true, message,
					LatestReleaseUrl: latest.Url, IsAheadOfLatestRelease: isAheadOfLatest)
				{
					NewerReleases = newer,
					LatestInstaller = latestInstaller,
				};
			}
		}
		catch (Exception ex)
		{
			AppLog.Warning(LogSource, $"Version check failed ({ex.Message}). Update state is unknown.");
			return new VersionCheckResult(current, null, false, channel, CheckSucceeded: false, ex.Message);
		}
	}

	// The release's first .msi asset with a usable name and download URL, if any.
	private static ReleaseInstaller? FindInstaller(JsonElement release)
	{
		if (!release.TryGetProperty("assets", out JsonElement assets) || assets.ValueKind != JsonValueKind.Array)
		{
			return null;
		}

		foreach (JsonElement asset in assets.EnumerateArray())
		{
			string? name = asset.TryGetProperty("name", out JsonElement nameElement) ? nameElement.GetString() : null;
			string? downloadUrl = asset.TryGetProperty("browser_download_url", out JsonElement urlElement) ? urlElement.GetString() : null;

			if (name is null
				|| downloadUrl is null
				|| !name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)
				|| name != Path.GetFileName(name))
			{
				continue;
			}

			long id = asset.TryGetProperty("id", out JsonElement idElement) && idElement.TryGetInt64(out long parsedId) ? parsedId : 0;
			long size = asset.TryGetProperty("size", out JsonElement sizeElement) && sizeElement.TryGetInt64(out long parsedSize) ? parsedSize : 0;
			return new ReleaseInstaller(name, downloadUrl, id, size);
		}

		return null;
	}

	/// <summary>
	/// Removes the "Instructions to install" section release bodies start with - the heading and
	/// everything under it, up to the next heading of the same or a higher level. Anyone reading
	/// the notes in the update window already has FE-Buddy installed.
	/// </summary>
	/// <param name="notes">The release body (Markdown).</param>
	/// <returns>The trimmed body without that section, or <see langword="null"/> when nothing is left.</returns>
	public static string? StripInstallInstructions(string? notes)
	{
		if (string.IsNullOrWhiteSpace(notes))
		{
			return null;
		}

		var kept = new List<string>();
		int skippingLevel = 0;

		foreach (string line in notes.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
		{
			Match heading = HeadingPattern().Match(line);
			if (heading.Success)
			{
				int level = heading.Groups["hashes"].Length;
				if (skippingLevel > 0 && level <= skippingLevel)
				{
					skippingLevel = 0;
				}

				if (skippingLevel == 0 && InstallHeadingPattern().IsMatch(heading.Groups["text"].Value))
				{
					skippingLevel = level;
				}
			}

			if (skippingLevel == 0)
			{
				kept.Add(line);
			}
		}

		string result = string.Join('\n', kept).Trim();
		return result.Length == 0 ? null : result;
	}

	/// <summary>
	/// Sends the GitHub releases request, optionally with a bearer token attached. Never throws
	/// on a non-success status - the caller inspects <see cref="HttpResponseMessage.IsSuccessStatusCode"/>.
	/// </summary>
	private static async Task<HttpResponseMessage> SendReleasesRequestAsync(
		HttpClient client, string? token, CancellationToken cancellationToken)
	{
		using HttpRequestMessage request = new(HttpMethod.Get, ReleasesUrl);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
		if (token is not null)
		{
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
		}

		return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
	}

	[GeneratedRegex(@"^ {0,3}(?<hashes>#{1,6})[ \t]+(?<text>.*)$")]
	private static partial Regex HeadingPattern();

	[GeneratedRegex(@"^instructions to install\b", RegexOptions.IgnoreCase)]
	private static partial Regex InstallHeadingPattern();
}
