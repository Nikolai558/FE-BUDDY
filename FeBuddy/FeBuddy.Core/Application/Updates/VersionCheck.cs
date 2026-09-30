using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Core.Infrastructure.GitHub;
using FeBuddy.Core.Infrastructure.Http;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Platform;
using FeBuddy.Versioning;
using FeBuddy.Versioning.Models;

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
/// 3.x releases share one list. The request is sent with the GitHub token the user chose
/// (<see cref="GitHubAuth"/>), if any; a request with it that fails in any way is tried once more
/// without it.
/// </para>
/// </remarks>
public static partial class VersionCheck
{
	private const string LogSource = "VersionCheck";
	// GitHub's largest page. 2.x and 3.x releases share the list, so a smaller one could hold only
	// pre-releases after a long run of them, and a Stable user would find no release at all.
	private const string ReleasesUrl = GitHubRepository.ApiUrl + "/releases?per_page=100";

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
			using HttpResponseMessage response = await SendReleasesRequestAsync(client, cancellationToken).ConfigureAwait(false);

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
				AppLog.Info(LogSource, $"No comparable release found on the {channel.DisplayName()} channel.");
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

			// Ahead because the user moved to a more stable channel while on one of its published
			// pre-releases (running 3.1.0-rc.1, now on Stable) - not a development build.
			ReleaseChannel? preReleaseChannel = isAheadOfLatest && IsPublishedPreRelease(currentParsed!) && currentParsed!.Channel < channel
				? currentParsed.Channel
				: null;

			List<ReleaseSummary> newer = updateAvailable
				? [.. candidates
						.Where(c => c.Parsed.ComparePrecedenceTo(currentParsed!) > 0)
						.OrderByDescending(c => c.Parsed, Precedence)
						.ThenByDescending(c => c.Release.PublishedAt)
						.Select(c => c.Release)]
				: [];

			string message = updateAvailable
				? $"v{latest.Version} available on the {channel.DisplayName()} channel ({newer.Count} newer release(s))."
				: preReleaseChannel is { } running
					? $"Running a {running.DisplayName()} pre-release ahead of the latest {channel.DisplayName()} release (v{latest.Version})."
				: isAheadOfLatest
					? $"Running a development build ahead of the latest {channel.DisplayName()} release (v{latest.Version})."
					: "You are running the latest version.";

			AppLog.Info(LogSource, message);
			return new VersionCheckResult(
				current, latest.Version, updateAvailable, channel, CheckSucceeded: true, message,
				LatestReleaseUrl: latest.Url, IsAheadOfLatestRelease: isAheadOfLatest)
			{
				NewerReleases = newer,
				LatestRelease = latest,
				LatestInstaller = latestInstaller,
				RunningPreReleaseChannel = preReleaseChannel,
			};
		}
		catch (Exception ex)
		{
			AppLog.Warning(LogSource, $"Version check failed ({ex.Message}). Update state is unknown.");
			return new VersionCheckResult(current, null, false, channel, CheckSucceeded: false, ex.Message);
		}
	}

	/// <summary>
	/// Whether a pre-release carries one of the labels releases are published with - <c>alpha</c>,
	/// <c>beta</c> or <c>rc</c> - rather than a development label such as <c>dev</c>.
	/// </summary>
	internal static bool IsPublishedPreRelease(ProductVersion version) =>
		version.IsPrerelease
		&& version.SemVersion.PrereleaseIdentifiers[0].Value.ToUpperInvariant() is "ALPHA" or "BETA" or "RC";

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
	/// Sends the GitHub releases request: with the chosen GitHub token, if any, and - when that
	/// fails in any way - once more without it, since a stale token must never hide an update.
	/// Never throws on a non-success status - the caller inspects
	/// <see cref="HttpResponseMessage.IsSuccessStatusCode"/>.
	/// </summary>
	private static async Task<HttpResponseMessage> SendReleasesRequestAsync(HttpClient client, CancellationToken cancellationToken)
	{
		using (HttpRequestMessage request = NewReleasesRequest())
		{
			if (!GitHubAuth.TryAuthorize(request))
			{
				return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
			}

			string failure;

			try
			{
				HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

				if (response.IsSuccessStatusCode)
				{
					return response;
				}

				failure = $"GitHub answered {(int)response.StatusCode}";
				response.Dispose();
			}
			catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
			{
				failure = ex.Message;
			}

			AppLog.Warning(LogSource, $"The release check with {GitHubAuth.TokenDescription} failed ({failure}); trying again without it.");
		}

		using HttpRequestMessage anonymous = NewReleasesRequest();
		return await client.SendAsync(anonymous, cancellationToken).ConfigureAwait(false);
	}

	private static HttpRequestMessage NewReleasesRequest()
	{
		HttpRequestMessage request = new(HttpMethod.Get, ReleasesUrl);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
		return request;
	}

	[GeneratedRegex(@"^ {0,3}(?<hashes>#{1,6})[ \t]+(?<text>.*)$")]
	private static partial Regex HeadingPattern();

	[GeneratedRegex(@"^instructions to install\b", RegexOptions.IgnoreCase)]
	private static partial Regex InstallHeadingPattern();
}
