namespace FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

/// <summary>
/// How a line of a tab's "What You'll Get" summary joins the lines above it: the words on its chip.
/// Read top to bottom, chips and all, a block is one sentence: <c>Every STAR</c> · <c>for</c>
/// <c>airports in ZOB</c> · <c>and only</c> <c>those amended this cycle</c>.
/// </summary>
public enum SummaryJoin
{
	/// <summary>No chip: a block's first line, what the rest narrow down or add to.</summary>
	First = 0,

	/// <summary><c>for</c>: the place the first line is for, e.g. <c>airports in ZOB</c>.</summary>
	For = 1,

	/// <summary><c>in</c>: the place the first line is in, e.g. <c>ZLA and ZOA</c>.</summary>
	In = 2,

	/// <summary>
	/// <c>but only</c>: narrows the first line, right below it. <see cref="SummaryLines"/> words a
	/// narrowing this way or as <see cref="AndOnly"/> by where it falls.
	/// </summary>
	ButOnly = 3,

	/// <summary><c>and only</c>: narrows everything above, below another line that already does.</summary>
	AndOnly = 4,

	/// <summary><c>with only</c>: under "Outputs include", what a file keeps of everything above.</summary>
	WithOnly = 5,

	/// <summary><c>along with</c>: added to what the lines above get, whatever they say.</summary>
	AlongWith = 6,
}

/// <summary>One line of a "What You'll Get" summary.</summary>
/// <param name="Join">How it joins the lines above it.</param>
/// <param name="Text">
/// What it says, e.g. <c>airports in ZOB</c>; a file name between backticks shows as code.
/// </param>
public sealed record SummaryLine(SummaryJoin Join, string Text)
{
	/// <summary>The words on the chip the line starts with, e.g. <c>and only</c>; empty for none.</summary>
	public string JoinWord => Join switch
	{
		SummaryJoin.For => "for",
		SummaryJoin.In => "in",
		SummaryJoin.ButOnly => "but only",
		SummaryJoin.AndOnly => "and only",
		SummaryJoin.WithOnly => "with only",
		SummaryJoin.AlongWith => "along with",
		_ => string.Empty,
	};

	/// <summary>Whether the line starts with a chip.</summary>
	public bool HasJoin => Join != SummaryJoin.First;
}

/// <summary>
/// What one or more of a sub-service's outputs get: the filters that pick it, one per line, then
/// what narrows it further in one file or another, under "Outputs include". A tab whose outputs all
/// get the same has one block; one whose outputs differ (Airports: the region narrows the GeoJSON,
/// not the alias file) has a block each.
/// </summary>
/// <param name="Outputs">The outputs the block is about.</param>
/// <param name="Lines">Its lines, first to last.</param>
public sealed record SummaryBlock(SubServiceOutputKinds Outputs, IReadOnlyList<SummaryLine> Lines)
{
	/// <summary>
	/// The lines under "Outputs include", e.g. <c>`Procedure_Changes.md`: only those added, changed,
	/// or deleted this cycle</c>; none for most blocks.
	/// </summary>
	public IReadOnlyList<SummaryLine> Includes { get; init; } = [];

	/// <summary>
	/// The blocks a tab shows: only those for outputs that are on (each trimmed to them), and
	/// blocks that say exactly the same merged into one.
	/// </summary>
	/// <param name="on">The outputs that are on.</param>
	/// <param name="blocks">Every block, one per output or group of outputs.</param>
	/// <returns>The blocks to show.</returns>
	public static IReadOnlyList<SummaryBlock> ForOutputsOn(SubServiceOutputKinds on, params SummaryBlock[] blocks)
	{
		List<SummaryBlock> shown = [];

		foreach (SummaryBlock block in blocks.Where(block => (block.Outputs & on) != SubServiceOutputKinds.None))
		{
			SubServiceOutputKinds outputs = block.Outputs & on;
			int same = shown.FindIndex(other => other.Lines.SequenceEqual(block.Lines) && other.Includes.SequenceEqual(block.Includes));

			if (same >= 0)
			{
				shown[same] = shown[same] with { Outputs = shown[same].Outputs | outputs };
			}
			else
			{
				shown.Add(block with { Outputs = outputs });
			}
		}

		return shown;
	}
}

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
