using System.Globalization;

namespace FEBuddyLibrary.Models.Services.General;

/// <summary>
/// A News post identifier: a Zulu date plus a per-day sequence number, e.g.
/// <c>2026-08-30.3</c> (see <c>Developer_Notes.md</c> → NEWS).
/// </summary>
/// <param name="Date">The post's date (always Zulu / GMT).</param>
/// <param name="Sequence">The 1-based sequence number for that day.</param>
public readonly record struct NewsPostId(DateOnly Date, int Sequence) : IComparable<NewsPostId>
{
	/// <summary>Compares by date, then by sequence.</summary>
	/// <param name="other">The other id.</param>
	/// <returns>Standard comparison result.</returns>
	public int CompareTo(NewsPostId other)
	{
		int byDate = Date.CompareTo(other.Date);
		return byDate != 0 ? byDate : Sequence.CompareTo(other.Sequence);
	}

	/// <inheritdoc />
	public override string ToString() => $"{Date:yyyy-MM-dd}.{Sequence}";

	/// <summary>
	/// Parses a stored id such as <c>2026-08-30.3</c>.
	/// </summary>
	/// <param name="value">The id text.</param>
	/// <param name="postId">The parsed id on success.</param>
	/// <returns><see langword="true"/> if <paramref name="value"/> parsed.</returns>
	public static bool TryParse(string? value, out NewsPostId postId)
	{
		postId = default;

		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}

		string[] parts = value.Trim().Split('.', 2);
		if (parts.Length != 2
			|| !DateOnly.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
			|| !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sequence)
			|| sequence < 1)
		{
			return false;
		}

		postId = new NewsPostId(date, sequence);
		return true;
	}
}

/// <summary>
/// One rendered News post: its id, its heading date text, a title line, and the body.
/// </summary>
/// <param name="Id">The post's <see cref="NewsPostId"/>.</param>
/// <param name="DateHeading">The <c>## </c> heading text (usually the date).</param>
/// <param name="Title">The first bold/emphasis line of the post, or an empty string.</param>
/// <param name="Body">The remaining post text (markdown), trimmed.</param>
public sealed record NewsPost(NewsPostId Id, string DateHeading, string Title, string Body);

/// <summary>
/// The outcome of checking News on launch (remediation plan 6.1).
/// </summary>
/// <param name="Posts">Every parsed post, newest first.</param>
/// <param name="LatestPostId">The newest post's id, or <see langword="null"/> when there are no posts.</param>
/// <param name="NewPostCount">
/// How many posts are newer than the stored <c>General.NewsLastOpen</c> (all posts when it was
/// never set).
/// </param>
/// <param name="ParseSucceeded">
/// <see langword="false"/> when the News document could not be parsed - the caller must leave
/// <c>General.NewsLastOpen</c> untouched and surface a warning.
/// </param>
/// <param name="FromNetwork"><see langword="true"/> when the document was fetched from GitHub, <see langword="false"/> when the bundled copy was used.</param>
public sealed record NewsCheckResult(
	IReadOnlyList<NewsPost> Posts,
	NewsPostId? LatestPostId,
	int NewPostCount,
	bool ParseSucceeded,
	bool FromNetwork);
