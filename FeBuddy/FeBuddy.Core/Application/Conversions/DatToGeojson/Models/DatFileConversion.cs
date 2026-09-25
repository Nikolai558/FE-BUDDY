namespace FeBuddy.Core.Application.Conversions.DatToGeojson.Models;

/// <summary>What happened to one <c>.dat</c> file in a DAT to GeoJSON run.</summary>
public sealed record DatFileConversion
{
	/// <summary>The <c>.dat</c> file that was read.</summary>
	public required string SourcePath { get; init; }

	/// <summary>
	/// The GeoJSON file written, or <see langword="null"/> when nothing was: the file could not
	/// be read, or cropping left nothing to draw.
	/// </summary>
	public string? OutputPath { get; init; }

	/// <summary>How many lines the <c>.dat</c> file drew.</summary>
	public int LinesRead { get; init; }

	/// <summary>How many lines were written, after cropping (a line that leaves and re-enters the circle counts once per piece).</summary>
	public int LinesWritten { get; init; }

	/// <summary>How many of the file's records could not be used and were skipped.</summary>
	public int RecordsSkipped { get; init; }

	/// <summary>Why the file was not converted, or <see langword="null"/> when it was.</summary>
	public string? Error { get; init; }
}
