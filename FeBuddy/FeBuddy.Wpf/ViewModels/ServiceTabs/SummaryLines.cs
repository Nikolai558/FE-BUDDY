using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// Builds a block's lines. A line with no text (a filter that isn't set) is left out, so each
/// filter can be added the same way whether or not one came before it:
/// <list type="bullet">
/// <item>Unless told otherwise, the first line added has no chip, whatever join it was given.</item>
/// <item>
/// A narrowing (<see cref="SummaryJoin.ButOnly"/> or <see cref="SummaryJoin.AndOnly"/>, either one)
/// reads <c>but only</c> right below the first line and <c>and only</c> anywhere else.
/// </item>
/// </list>
/// </summary>
/// <param name="joinFirstLine">
/// <see langword="true"/> to keep the first line's chip: for "Outputs include", where every line
/// says how it narrows the block above.
/// </param>
public sealed class SummaryLines(bool joinFirstLine = false)
{
	private readonly List<SummaryLine> _lines = [];

	/// <summary>Adds a line, unless <paramref name="text"/> is <see langword="null"/> or blank.</summary>
	/// <param name="join">How it joins the lines above; ignored for the first line unless the builder keeps it.</param>
	/// <param name="text">What it says, or <see langword="null"/> to leave it out.</param>
	/// <returns>This, to add the next.</returns>
	public SummaryLines Add(SummaryJoin join, string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return this;
		}

		if (_lines.Count == 0 && !joinFirstLine)
		{
			join = SummaryJoin.First;
		}
		else if (join is SummaryJoin.ButOnly or SummaryJoin.AndOnly)
		{
			join = _lines.Count == 1 && _lines[0].Join == SummaryJoin.First ? SummaryJoin.ButOnly : SummaryJoin.AndOnly;
		}

		_lines.Add(new SummaryLine(join, text));
		return this;
	}

	/// <summary>Whether no line has been added.</summary>
	public bool IsEmpty => _lines.Count == 0;

	/// <summary>The lines added.</summary>
	/// <returns>The lines, first to last.</returns>
	public IReadOnlyList<SummaryLine> ToList() => [.. _lines];

	/// <summary>
	/// Words joined the way the summary lists them, with the Oxford comma: <c>ZOB</c>,
	/// <c>ZOB and ZNY</c>, <c>ZOB, ZNY, and ZID</c>.
	/// </summary>
	/// <param name="words">The words.</param>
	/// <param name="conjunction"><c>and</c> or <c>or</c>.</param>
	/// <returns>The list, or empty for none.</returns>
	public static string Join(IReadOnlyList<string> words, string conjunction) => words.Count switch
	{
		0 => string.Empty,
		1 => words[0],
		2 => $"{words[0]} {conjunction} {words[1]}",
		_ => $"{string.Join(", ", words.Take(words.Count - 1))}, {conjunction} {words[^1]}",
	};
}
