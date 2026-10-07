namespace FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

/// <summary>How a line of a tab's "What You'll Get" summary joins the lines above it.</summary>
public enum SummaryJoin
{
	/// <summary>The first line: what the rest narrow down or add to.</summary>
	First = 0,

	/// <summary>Narrows everything above: it must hold too.</summary>
	And = 1,

	/// <summary>Another way in, beside the line above.</summary>
	Or = 2,

	/// <summary>Added on top, whatever the lines above say.</summary>
	Plus = 3,
}

/// <summary>One line of a "What You'll Get" summary.</summary>
/// <param name="Join">How it joins the lines above it.</param>
/// <param name="Text">What it says, e.g. <c>in ZOB or ZNY</c>.</param>
public sealed record SummaryLine(SummaryJoin Join, string Text)
{
	/// <summary>The word in the line's margin: <c>AND</c>, <c>OR</c>, <c>PLUS</c>, or nothing for the first line.</summary>
	public string JoinWord => Join switch
	{
		SummaryJoin.And => "AND",
		SummaryJoin.Or => "OR",
		SummaryJoin.Plus => "PLUS",
		_ => string.Empty,
	};
}

/// <summary>
/// What one or more of a sub-service's outputs get: the filters that pick it, one per line. A tab
/// whose outputs all get the same has one block; one whose outputs differ (Airports: the region
/// narrows the GeoJSON, not the alias file) has a block each.
/// </summary>
/// <param name="Outputs">The outputs the block is about.</param>
/// <param name="Lines">Its lines, first to last.</param>
public sealed record SummaryBlock(SubServiceOutputKinds Outputs, IReadOnlyList<SummaryLine> Lines)
{
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
			int same = shown.FindIndex(other => other.Lines.SequenceEqual(block.Lines));

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
/// Builds a block's lines: the first line added has no join, and a line with no text (a filter
/// that isn't set) is left out.
/// </summary>
public sealed class SummaryLines
{
	private readonly List<SummaryLine> _lines = [];

	/// <summary>Adds a line, unless <paramref name="text"/> is <see langword="null"/>.</summary>
	/// <param name="join">How it joins the lines above; ignored for the first line.</param>
	/// <param name="text">What it says, or <see langword="null"/> to leave it out.</param>
	/// <returns>This, to add the next.</returns>
	public SummaryLines Add(SummaryJoin join, string? text)
	{
		if (!string.IsNullOrWhiteSpace(text))
		{
			_lines.Add(new SummaryLine(_lines.Count == 0 ? SummaryJoin.First : join, text));
		}

		return this;
	}

	/// <summary>The lines added.</summary>
	/// <returns>The lines, first to last.</returns>
	public IReadOnlyList<SummaryLine> ToList() => [.. _lines];

	/// <summary>Words joined the way the summary lists them: <c>ZOB</c>, <c>ZOB or ZNY</c>, <c>ZOB, ZNY or ZID</c>.</summary>
	/// <param name="words">The words.</param>
	/// <param name="conjunction"><c>or</c> or <c>and</c>.</param>
	/// <returns>The list, or empty for none.</returns>
	public static string Join(IReadOnlyList<string> words, string conjunction) => words.Count switch
	{
		0 => string.Empty,
		1 => words[0],
		_ => $"{string.Join(", ", words.Take(words.Count - 1))} {conjunction} {words[^1]}",
	};
}
