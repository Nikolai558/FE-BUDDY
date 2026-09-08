using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;

using FEBuddyLibrary.Models.Services.General;

namespace FEBuddyLibrary.Services.General;

/// <summary>
/// Reads the FE-Buddy News document (<c>FEBuddyLibrary/News.md</c>): fetched from GitHub raw
/// when online, falling back to the copy bundled into this assembly offline. Parses the posts
/// for display on the Dashboard and reports how many are newer than the user last saw
/// (remediation plan 6.1).
/// </summary>
public static class NewsService
{
	private const string LogSource = "News";

	/// <summary>The raw News markdown URL on GitHub (used when online).</summary>
	public const string RawUrl = "https://raw.githubusercontent.com/Nikolai558/FE-Buddy-DEV/main/FeBuddy/FEBuddyLibrary/News.md";

	/// <summary>The human-facing News page the News button opens in a browser.</summary>
	public const string PageUrl = "https://github.com/Nikolai558/FE-Buddy-DEV/blob/main/FeBuddy/FEBuddyLibrary/News.md";

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

		AppLog.Info(LogSource, newCount > 0
			? $"{newCount} unread News post(s); newest is {latest}."
			: $"News is up to date (newest {latest}).");

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

			string text = await client.GetStringAsync(RawUrl, cancellationToken).ConfigureAwait(false);

			if (!string.IsNullOrWhiteSpace(text))
			{
				return (text, true);
			}
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
}
