namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// The Map's output picker filter: words, separated by spaces, that every file listed must have in its
/// name or its folder, ignoring case - e.g. <c>ABQ Lines</c>. Split once when the filter is typed, not for
/// every file it is checked against: a Departures run writes thousands.
/// </summary>
public sealed class OutputFileFilter
{
	/// <summary>Creates the filter from what was typed.</summary>
	/// <param name="text">The words, or <see langword="null"/> or blank for none.</param>
	public OutputFileFilter(string? text) =>
		Words = (text ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	/// <summary>The filter of no words: every file matches.</summary>
	public static OutputFileFilter None { get; } = new(null);

	/// <summary>The words, in the order typed.</summary>
	public IReadOnlyList<string> Words { get; }

	/// <summary>Whether there are no words, so every file matches.</summary>
	public bool IsEmpty => Words.Count == 0;

	/// <summary>Whether every word is in <paramref name="name"/> or <paramref name="folder"/>, ignoring case.</summary>
	/// <param name="name">The file's name.</param>
	/// <param name="folder">The folder it is listed under.</param>
	/// <returns><see langword="true"/> when the file stays listed.</returns>
	public bool Matches(string name, string folder)
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(folder);

		foreach (string word in Words)
		{
			if (!name.Contains(word, StringComparison.OrdinalIgnoreCase) && !folder.Contains(word, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}

		return true;
	}
}
