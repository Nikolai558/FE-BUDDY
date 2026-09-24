using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;

using FeBuddy.Core.Application.News.Models;
using FeBuddy.Core.Application.Updates;
using FeBuddy.Core.Infrastructure.GitHub;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Core.Application.News;

/// <summary>
/// Reads the FE-Buddy News document (<c>FeBuddy.Core/News.md</c>): fetched from GitHub raw
/// when online, falling back to the copy bundled into this assembly offline. Parses the posts
/// for display on the Dashboard and reports how many are newer than the user last saw
/// (remediation plan 6.1).
/// </summary>
/// <remarks>
/// News.md lives in the public <c>Nikolai558/FE-BUDDY</c> repo on the <c>v3-development</c>
/// branch (the repo's default branch, <c>development</c>, is still the v2.x code and has no
/// News.md). The plain unauthenticated raw URL is the normal path; only on failure - and only
/// if <see cref="GitHubAuth.EnvironmentVariableName"/> is set - it retries once via GitHub's
/// Contents API with that token attached (the same fallback-only behavior as
/// <see cref="VersionCheck"/>). The authenticated retry uses the documented API endpoint rather
/// than raw.githubusercontent.com, which doesn't reliably honor a token.
/// </remarks>
public static class NewsService
{
	private const string LogSource = "News";

	// Pinned to v3-development rather than the default branch: while v2.x is still the default
	// (development), a default-branch URL would 404. v3-development becomes the default at the
	// 3.0 release, so these keep working unchanged after the switch.

	/// <summary>The raw News markdown URL on GitHub (used when online, unauthenticated).</summary>
	public const string RawUrl = "https://raw.githubusercontent.com/Nikolai558/FE-BUDDY/v3-development/FeBuddy/FeBuddy.Core/News.md";

	/// <summary>
	/// The authenticated fallback: GitHub's Contents API, which honors a bearer token and returns
	/// the file content when asked for the raw representation.
	/// </summary>
	private const string ContentsApiUrl = "https://api.github.com/repos/Nikolai558/FE-BUDDY/contents/FeBuddy/FeBuddy.Core/News.md?ref=v3-development";

	/// <summary>The human-facing News page the News button opens in a browser.</summary>
	public const string PageUrl = "https://github.com/Nikolai558/FE-BUDDY/blob/v3-development/FeBuddy/FeBuddy.Core/News.md";

	private static readonly Regex PostIdPattern = new(
		@"PostId:\s*(\d{4}-\d{2}-\d{2})\.(\d+)",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);

	/// <summary>
	/// Fetches the News markdown, parses it, and compares it against
	/// <paramref name="lastOpenPostId"/>. Never throws.
	/// </summary>
	/// <param name="lastOpenPostId">The stored <c>General.NewsLastOpen</c> value (may be null/empty/invalid).</param>
	/// <param name="hasInternetConnection">When <see langword="false"/>, the bundled copy is used without a network call.</param>
	/// <param name="httpClient">An <see cref="HttpClient"/> to use, or <see langword="null"/> to create one. A test seam.</param>
	/// <param name="cancellationToken">Cancels the fetch.</param>
	/// <returns>The parsed posts, the newest id, and the count of unseen posts.</returns>
	public static async Task<NewsCheckResult> CheckAsync(
		string? lastOpenPostId,
		bool hasInternetConnection,
		HttpClient? httpClient = null,
		CancellationToken cancellationToken = default)
	{
		(string markdown, bool fromNetwork) = await FetchAsync(hasInternetConnection, httpClient, cancellationToken).ConfigureAwait(false);

		IReadOnlyList<NewsPost> posts;
		try
		{
			posts = Parse(markdown);
		}
		catch (Exception ex)
		{
			AppLog.Warning(LogSource, $"Could not parse the News document: {ex.Message}. NewsLastOpen is left unchanged.");
			return new NewsCheckResult(Array.Empty<NewsPost>(), null, 0, ParseSucceeded: false, fromNetwork);
		}

		if (posts.Count == 0)
		{
			AppLog.Warning(LogSource, "The News document parsed to zero posts. NewsLastOpen is left unchanged.");
			return new NewsCheckResult(posts, null, 0, ParseSucceeded: false, fromNetwork);
		}

		NewsPostId latest = posts[0].Id;
		int newCount;

		if (!NewsPostId.TryParse(lastOpenPostId, out NewsPostId lastSeen))
		{
			// Never checked (or a corrupt stored value): every post is "new".
			newCount = posts.Count;
		}
		else
		{
			newCount = posts.Count(p => p.Id.CompareTo(lastSeen) > 0);
		}

		string source = fromNetwork ? "GitHub" : "the bundled copy (GitHub unreachable)";
		AppLog.Info(LogSource, newCount > 0
			? $"{newCount} unread News post(s) from {source}; newest is {latest}."
			: $"News is up to date (newest {latest}) - from {source}.");

		return new NewsCheckResult(posts, latest, newCount, ParseSucceeded: true, fromNetwork);
	}

	/// <summary>
	/// Parses News markdown into posts, newest first. A post is a <c>## </c> heading, an
	/// optional <c>&lt;!-- PostId: … --&gt;</c> comment, and the text up to the next <c>---</c>
	/// rule. Posts without a parseable PostId are skipped.
	/// </summary>
	/// <param name="markdown">The News document text.</param>
	/// <returns>The parsed posts in document order (newest first, matching the file convention).</returns>
	public static IReadOnlyList<NewsPost> Parse(string markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
		{
			return Array.Empty<NewsPost>();
		}

		string[] sections = Regex.Split(markdown, @"(?m)^\s*---\s*$");
		List<NewsPost> posts = new();

		foreach (string section in sections)
		{
			Match heading = Regex.Match(section, @"(?m)^##\s+(.+?)\s*$");
			if (!heading.Success)
			{
				continue;
			}

			Match idMatch = PostIdPattern.Match(section);
			if (!idMatch.Success
				|| !NewsPostId.TryParse($"{idMatch.Groups[1].Value}.{idMatch.Groups[2].Value}", out NewsPostId id))
			{
				continue;
			}

			// Everything after the heading line, with the HTML comment block removed.
			string afterHeading = section[(heading.Index + heading.Length)..];
			string body = Regex.Replace(afterHeading, @"<!--.*?-->", string.Empty, RegexOptions.Singleline).Trim();

			string title = body
				.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(line => line.Trim('*', ' ', '#'))
				.FirstOrDefault(line => line.Length > 0) ?? string.Empty;

			posts.Add(new NewsPost(id, heading.Groups[1].Value.Trim(), title, body));
		}

		return posts
			.OrderByDescending(p => p.Id)
			.ToArray();
	}

	/// <summary>The News markdown bundled into this assembly, used offline or when the fetch fails.</summary>
	/// <returns>The bundled markdown, or an empty string if the embedded resource is missing.</returns>
	public static string GetBundledMarkdown()
	{
		Assembly assembly = typeof(NewsService).Assembly;
		string? name = assembly.GetManifestResourceNames()
			.FirstOrDefault(n => n.EndsWith("News.md", StringComparison.OrdinalIgnoreCase));

		if (name is null)
		{
			return string.Empty;
		}

		using Stream? stream = assembly.GetManifestResourceStream(name);
		if (stream is null)
		{
			return string.Empty;
		}

		using StreamReader reader = new(stream);
		return reader.ReadToEnd();
	}

	private static async Task<(string Markdown, bool FromNetwork)> FetchAsync(
		bool hasInternetConnection,
		HttpClient? httpClient,
		CancellationToken cancellationToken)
	{
		if (!hasInternetConnection)
		{
			return (GetBundledMarkdown(), false);
		}

		bool ownsClient = httpClient is null;
		HttpClient client = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

		try
		{
			if (client.DefaultRequestHeaders.UserAgent.Count == 0)
			{
				client.DefaultRequestHeaders.UserAgent.ParseAdd("FE-Buddy");
			}

			(string? text, string? failureReason) = await TryFetchAsync(client, RawUrl, token: null, cancellationToken).ConfigureAwait(false);

			if (text is null)
			{
				string? token = GitHubAuth.GetOptionalToken();
				if (token is not null)
				{
					AppLog.Info(LogSource, $"Unauthenticated News fetch failed ({failureReason}); retrying with {GitHubAuth.EnvironmentVariableName}.");
					(text, failureReason) = await TryFetchAsync(client, ContentsApiUrl, token, cancellationToken).ConfigureAwait(false);
				}
			}

			if (!string.IsNullOrWhiteSpace(text))
			{
				return (text, true);
			}

			AppLog.Info(LogSource, $"Could not fetch News from GitHub ({failureReason}); using the bundled copy.");
		}
		catch (Exception ex)
		{
			AppLog.Info(LogSource, $"Could not fetch News from GitHub ({ex.Message}); using the bundled copy.");
		}
		finally
		{
			if (ownsClient)
			{
				client.Dispose();
			}
		}

		return (GetBundledMarkdown(), false);
	}

	/// <summary>
	/// Sends one News fetch attempt. Never throws on a non-success status - returns
	/// <see langword="null"/> markdown and a short reason instead, so the caller can decide
	/// whether to retry.
	/// </summary>
	private static async Task<(string? Markdown, string? FailureReason)> TryFetchAsync(
		HttpClient client, string url, string? token, CancellationToken cancellationToken)
	{
		using HttpRequestMessage request = new(HttpMethod.Get, url);
		if (token is not null)
		{
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			// Contents API returns JSON metadata (base64 content) unless explicitly asked for
			// the raw file body this way.
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.raw+json"));
		}

		using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			return (null, $"{(int)response.StatusCode} {response.ReasonPhrase}");
		}

		string text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		return string.IsNullOrWhiteSpace(text) ? (null, "empty response") : (text, null);
	}
}
