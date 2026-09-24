using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

using FeBuddy.Core.Models.Services.General;

namespace FeBuddy.Core.Services.General;

/// <summary>
/// Asks GitHub whether a newer FE-Buddy release exists on the user's chosen channel. The
/// launch sequence runs this only to populate the title-bar tooltip and version chip; the
/// actual update is a new MSI installer the user downloads and runs.
/// </summary>
/// <remarks>
/// Each release's channel comes from its tag (see <see cref="ReleaseChannel"/>): an <c>-alpha</c>
/// tag is Alpha, a <c>-beta</c> or <c>-rc</c> tag is Beta, any other release GitHub flags as a
/// pre-release is Alpha, and the rest are Stable. A user sees releases on their channel and every
/// channel below it - Stable sees Stable, Beta sees Beta and Stable, Alpha sees everything. The
/// check never throws - a network or parse failure yields
/// <see cref="VersionCheckResult.CheckSucceeded"/> <see langword="false"/>.
/// </remarks>
/// <remarks>
/// Always checks the public <c>Nikolai558/FE-BUDDY</c> repo's releases (v2.x and 3.x releases
/// share that one list). The request is always tried
/// unauthenticated first (the normal path for a public repo); only if that fails, and only if
/// <see cref="GitHubAuth.EnvironmentVariableName"/> is set, it retries once with that token
/// attached (see <see cref="GitHubAuth"/> for why this is a fallback rather than always-sent).
/// </remarks>
public static partial class VersionCheck
{
	private const string LogSource = "VersionCheck";
	private const string ReleasesUrl = "https://api.github.com/repos/Nikolai558/FE-BUDDY/releases?per_page=30";

	/// <summary>
	/// Runs the version check. Never throws.
	/// </summary>
	/// <param name="currentVersion">The running application's version (e.g. from the entry assembly).</param>
	/// <param name="channel">The channel to compare against.</param>
	/// <param name="hasInternetConnection">
	/// If <see langword="false"/>, the check is skipped and returns
	/// <see cref="VersionCheckResult.CheckSucceeded"/> <see langword="false"/>.
	/// </param>
	/// <param name="httpClient">An <see cref="HttpClient"/> to use, or <see langword="null"/> to create one. A test seam.</param>
	/// <param name="cancellationToken">Cancels the network call.</param>
	/// <returns>The comparison result.</returns>
	public static async Task<VersionCheckResult> RunAsync(
		string currentVersion,
		UpdateChannel channel,
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

		bool ownsClient = httpClient is null;
		HttpClient client = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

		try
		{
			if (client.DefaultRequestHeaders.UserAgent.Count == 0)
			{
				client.DefaultRequestHeaders.UserAgent.ParseAdd("FE-Buddy");
			}

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

				Version? currentParsed = TryParseVersion(current);
				var candidates = new List<(Version Parsed, ReleaseSummary Release)>();

				foreach (JsonElement release in document.RootElement.EnumerateArray())
				{
					if (release.TryGetProperty("draft", out JsonElement draft) && draft.ValueKind == JsonValueKind.True)
					{
						continue;
					}

					bool isPrerelease = release.TryGetProperty("prerelease", out JsonElement pre) && pre.ValueKind == JsonValueKind.True;
					string? tag = release.TryGetProperty("tag_name", out JsonElement tagElement) ? tagElement.GetString() : null;
					Version? parsed = TryParseVersion(tag);
					if (parsed is null)
					{
						continue;
					}

					UpdateChannel releaseChannel = ReleaseChannel(tag!, isPrerelease);
					if (releaseChannel > channel)
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
						tag!.Trim().TrimStart('v', 'V'), published, releaseChannel != UpdateChannel.Stable, StripInstallInstructions(body), url)));
				}

				if (candidates.Count == 0)
				{
					AppLog.Info(LogSource, $"No comparable release found on the {channel} channel.");
					return new VersionCheckResult(current, null, false, channel, CheckSucceeded: true, "No comparable release found.");
				}

				// On a tie the first one listed wins (GitHub lists newest first).
				(Version best, ReleaseSummary latest) = candidates[0];
				foreach ((Version parsed, ReleaseSummary release) in candidates)
				{
					if (parsed > best)
					{
						(best, latest) = (parsed, release);
					}
				}

				bool updateAvailable = currentParsed is not null && best > currentParsed;
				bool isAheadOfLatest = currentParsed is not null && !updateAvailable && currentParsed > best;

				List<ReleaseSummary> newer = updateAvailable
					? candidates
						.Where(c => c.Parsed > currentParsed!)
						.OrderByDescending(c => c.Parsed)
						.ThenByDescending(c => c.Release.PublishedAt)
						.Select(c => c.Release)
						.ToList()
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
				};
			}
		}
		catch (Exception ex)
		{
			AppLog.Warning(LogSource, $"Version check failed ({ex.Message}). Update state is unknown.");
			return new VersionCheckResult(current, null, false, channel, CheckSucceeded: false, ex.Message);
		}
		finally
		{
			if (ownsClient)
			{
				client.Dispose();
			}
		}
	}

	/// <summary>
	/// The channel a release belongs to, from its tag first and GitHub's pre-release flag second:
	/// <c>-alpha</c> is <see cref="UpdateChannel.Alpha"/>; <c>-beta</c> or <c>-rc</c> is
	/// <see cref="UpdateChannel.Beta"/>; any other flagged pre-release is <see cref="UpdateChannel.Alpha"/>
	/// (only users who asked for everything get an unlabelled pre-release); the rest are
	/// <see cref="UpdateChannel.Stable"/>.
	/// </summary>
	/// <param name="tag">The release tag, e.g. <c>v2.9.1-alpha.1</c>.</param>
	/// <param name="isPrerelease">GitHub's <c>prerelease</c> flag.</param>
	/// <returns>The lowest channel that is offered the release.</returns>
	public static UpdateChannel ReleaseChannel(string tag, bool isPrerelease)
	{
		ArgumentNullException.ThrowIfNull(tag);

		int dash = tag.IndexOf('-', StringComparison.Ordinal);
		string suffix = dash >= 0 ? tag[(dash + 1)..] : string.Empty;

		if (suffix.StartsWith("alpha", StringComparison.OrdinalIgnoreCase))
		{
			return UpdateChannel.Alpha;
		}

		if (suffix.StartsWith("beta", StringComparison.OrdinalIgnoreCase) || suffix.StartsWith("rc", StringComparison.OrdinalIgnoreCase))
		{
			return UpdateChannel.Beta;
		}

		return isPrerelease ? UpdateChannel.Alpha : UpdateChannel.Stable;
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

	/// <summary>
	/// Parses a version string that may carry a leading <c>v</c> and may have fewer than four
	/// components. Returns <see langword="null"/> for anything unparseable (e.g. <c>"dev"</c>).
	/// </summary>
	/// <param name="value">The version text.</param>
	/// <returns>The parsed <see cref="Version"/>, or <see langword="null"/>.</returns>
	private static Version? TryParseVersion(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		string trimmed = value.Trim().TrimStart('v', 'V');
		int dash = trimmed.IndexOf('-');
		if (dash >= 0)
		{
			trimmed = trimmed[..dash];
		}

		return Version.TryParse(trimmed, out Version? parsed) ? parsed : null;
	}

	[GeneratedRegex(@"^ {0,3}(?<hashes>#{1,6})[ \t]+(?<text>.*)$")]
	private static partial Regex HeadingPattern();

	[GeneratedRegex(@"^instructions to install\b", RegexOptions.IgnoreCase)]
	private static partial Regex InstallHeadingPattern();
}
