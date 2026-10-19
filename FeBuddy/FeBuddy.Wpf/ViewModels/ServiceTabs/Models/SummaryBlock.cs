namespace FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

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

/// <summary>One file a "What You'll Get" block writes, and what goes in it.</summary>
/// <param name="Name">Its name, extension and all, e.g. <c>ARTCC-Boundary_High_Lines.geojson</c>; shown as code.</param>
/// <param name="Description">
/// What it holds, e.g. <c>Boundaries labeled "HIGH"</c>, shown under the name; <see langword="null"/>
/// for none. A name between backticks shows as code, and a <c>[text](https://...)</c> link as a link.
/// </param>
public sealed record SummaryFile(string Name, string? Description = null)
{
	/// <summary>The name between backticks, so it shows as code.</summary>
	public string Code => $"`{Name}`";

	/// <summary>Whether the file has a description.</summary>
	public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
}

/// <summary>
/// What one or more of a sub-service's outputs get: the filters that pick it, one per line, then
/// what narrows it further in one file or another, under "Outputs include", then the files it
/// writes, each with what goes in it, then any notes. A tab whose outputs all get the same has one
/// block; one whose outputs differ (Airports: the ROI narrows the GeoJSON, not the alias file) has a
/// block each.
/// </summary>
/// <param name="Outputs">The outputs the block is about.</param>
/// <param name="Lines">Its lines, first to last; none for a block that only lists its files.</param>
public sealed record SummaryBlock(SubServiceOutputKinds Outputs, IReadOnlyList<SummaryLine> Lines)
{
	/// <summary>
	/// The lines under "Outputs include", e.g. <c>`Procedure_Changes.md`: only those added, changed,
	/// or deleted this cycle</c>; none for most blocks.
	/// </summary>
	public IReadOnlyList<SummaryLine> Includes { get; init; } = [];

	/// <summary>
	/// The files the block's outputs write, in order, each with what goes in it; none for a block
	/// that doesn't list them. Under an "outputs" chip below the lines, or straight under the tags
	/// for a block with no lines.
	/// </summary>
	public IReadOnlyList<SummaryFile> Files { get; init; } = [];

	/// <summary>
	/// Notes on the files, e.g. <c>Boundaries labeled "UNLIMITED" go in both files.</c>, each after a
	/// "note" chip below them; none for most blocks.
	/// </summary>
	public IReadOnlyList<string> Notes { get; init; } = [];

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
			int same = shown.FindIndex(other => other.SaysTheSameAs(block));

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

	/// <summary>Whether two summaries say exactly the same: the same blocks, in order, for the same outputs.</summary>
	/// <param name="first">One summary.</param>
	/// <param name="second">The other.</param>
	/// <returns><see langword="true"/> when nothing in them differs.</returns>
	public static bool Same(IReadOnlyList<SummaryBlock> first, IReadOnlyList<SummaryBlock> second) =>
		first.Count == second.Count
		&& first.Zip(second).All(pair => pair.First.Outputs == pair.Second.Outputs && pair.First.SaysTheSameAs(pair.Second));

	/// <summary>Whether this block says exactly what <paramref name="other"/> does, whatever outputs each is for.</summary>
	private bool SaysTheSameAs(SummaryBlock other) =>
		Lines.SequenceEqual(other.Lines) && Includes.SequenceEqual(other.Includes)
		&& Files.SequenceEqual(other.Files) && Notes.SequenceEqual(other.Notes, StringComparer.Ordinal);
}
