using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

using FEBuddyLibrary.Models.Services.General;

namespace FEBuddyLibrary.Services.General;

/// <summary>
/// Asks GitHub whether a newer FE-Buddy release exists on the user's chosen channel. The
/// launch sequence runs this only to populate the title-bar tooltip and version chip; the
/// actual update is performed by the Squirrel plumbing in <c>Handlers/UdateHandler.cs</c>.
/// </summary>
/// <remarks>
/// GitHub's release API exposes a single <c>prerelease</c> flag, so <see cref="UpdateChannel.Beta"/>
/// and <see cref="UpdateChannel.Alpha"/> are treated the same here (both include pre-releases);
/// <see cref="UpdateChannel.Stable"/> ignores them. The check never throws - a network or parse
/// failure yields <see cref="VersionCheckResult.CheckSucceeded"/> <see langword="false"/>.
/// </remarks>
public static class VersionCheck
{
	private const string LogSource = "VersionCheck";
	private const string ReleasesUrl = "https://api.github.com/repos/Nikolai558/FE-Buddy-DEV/releases?per_page=30";

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

			using HttpRequestMessage request = new(HttpMethod.Get, ReleasesUrl);
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

			using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

			if (!response.IsSuccessStatusCode)
			{
				AppLog.Warning(LogSource, $"GitHub releases API returned {(int)response.StatusCode}. Update state is unknown.");
				return new VersionCheckResult(current, null, false, channel, CheckSucceeded: false, $"GitHub API returned {(int)response.StatusCode}.");
			}

			await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
			using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

			Version? currentParsed = TryParseVersion(current);
			Version? best = null;
			string? bestTag = null;
			string? bestNotes = null;
			string? bestUrl = null;

			foreach (JsonElement release in document.RootElement.EnumerateArray())
			{
				if (release.TryGetProperty("draft", out JsonElement draft) && draft.ValueKind == JsonValueKind.True)
				{
					continue;
				}

				bool isPrerelease = release.TryGetProperty("prerelease", out JsonElement pre) && pre.ValueKind == JsonValueKind.True;

				if (isPrerelease && channel == UpdateChannel.Stable)
				{
					continue;
				}

				string? tag = release.TryGetProperty("tag_name", out JsonElement tagElement) ? tagElement.GetString() : null;
				Version? parsed = TryParseVersion(tag);

				if (parsed is not null && (best is null || parsed > best))
				{
					best = parsed;
					bestTag = tag;
					bestNotes = release.TryGetProperty("body", out JsonElement body) ? body.GetString() : null;
					bestUrl = release.TryGetProperty("html_url", out JsonElement url) ? url.GetString() : null;
				}
			}

			if (best is null)
			{
				AppLog.Info(LogSource, $"No comparable release found on the {channel} channel.");
				return new VersionCheckResult(current, null, false, channel, CheckSucceeded: true, "No comparable release found.");
			}

			bool updateAvailable = currentParsed is not null && best > currentParsed;
			string message = updateAvailable
				? $"v{bestTag?.TrimStart('v', 'V')} available on the {channel} channel."
				: "You are running the latest version.";

			AppLog.Info(LogSource, message);
			return new VersionCheckResult(
				current, bestTag?.TrimStart('v', 'V'), updateAvailable, channel, CheckSucceeded: true, message,
				LatestReleaseNotes: bestNotes, LatestReleaseUrl: bestUrl);
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
}
