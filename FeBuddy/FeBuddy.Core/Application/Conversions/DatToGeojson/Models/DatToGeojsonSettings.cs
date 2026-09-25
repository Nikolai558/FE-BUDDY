using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.Core.Application.Conversions.DatToGeojson.Models;

/// <summary>
/// Fully-parsed, typed settings for the DAT to GeoJSON conversion. Built once by
/// <c>DatToGeojsonSettingsParser.Parse</c> from the raw <c>Dictionary&lt;string, string&gt;</c>
/// the GUI (or <c>FeBuddy.Harness</c>) supplies.
/// </summary>
public sealed record DatToGeojsonSettings
{
	/// <summary>Directory the converted files are written under.</summary>
	public required string OutputDirectory { get; init; }

	/// <summary>Whether to put the output inside a <c>FE-Buddy_Output</c> folder. Default <see langword="true"/>.</summary>
	public bool AddFeBuddyOutputFolder { get; init; } = true;

	/// <summary>Decimal places kept per coordinate, 0-15. Default 6.</summary>
	public int CoordinatePrecision { get; init; } = 6;

	/// <summary>
	/// A folder to convert every <c>.dat</c> file in (not its sub-folders), or
	/// <see langword="null"/> when <see cref="SourceFiles"/> names the files instead.
	/// </summary>
	public string? SourceFolder { get; init; }

	/// <summary>The individual <c>.dat</c> files to convert; empty when <see cref="SourceFolder"/> is used.</summary>
	public IReadOnlyList<string> SourceFiles { get; init; } = [];

	/// <summary>
	/// Keep only what lies within this many nautical miles of each file's point of tangency, or
	/// <see langword="null"/> to keep every line.
	/// </summary>
	public double? CroppingDistanceNm { get; init; }

	/// <summary>Whether each file starts with an <c>isLineDefaults</c> Feature built from <see cref="LineDefaults"/>.</summary>
	public bool IncludeCrcLineDefaults { get; init; }

	/// <summary>The CRC Line defaults; set exactly when <see cref="IncludeCrcLineDefaults"/> is <see langword="true"/>.</summary>
	public CrcLineDefaults? LineDefaults { get; init; }
}
