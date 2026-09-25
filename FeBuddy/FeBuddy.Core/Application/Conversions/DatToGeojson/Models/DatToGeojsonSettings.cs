using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.Core.Application.Conversions.DatToGeojson.Models;

/// <summary>
/// Fully-parsed, typed settings for the DAT to GeoJSON conversion. Built once by
/// <c>DatToGeojsonSettingsParser.Parse</c> from the raw <c>Dictionary&lt;string, string&gt;</c>
/// the GUI (or <c>FeBuddy.Harness</c>) supplies.
/// </summary>
public sealed record DatToGeojsonSettings : ConversionSettings
{
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
