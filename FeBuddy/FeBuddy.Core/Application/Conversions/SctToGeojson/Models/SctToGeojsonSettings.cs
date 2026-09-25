using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.Core.Application.Conversions.SctToGeojson.Models;

/// <summary>
/// Fully-parsed, typed settings for the SCT2 to GeoJSON conversion. Built once by
/// <c>SctToGeojsonSettingsParser.Parse</c> from the raw <c>Dictionary&lt;string, string&gt;</c>
/// the GUI (or <c>FeBuddy.Harness</c>) supplies.
/// </summary>
public sealed record SctToGeojsonSettings : ConversionSettings
{
	/// <summary>Whether every lines file starts with an <c>isLineDefaults</c> Feature built from <see cref="LineDefaults"/>.</summary>
	public bool IncludeCrcLineDefaults { get; init; }

	/// <summary>The CRC Line defaults; set exactly when <see cref="IncludeCrcLineDefaults"/> is <see langword="true"/>.</summary>
	public CrcLineDefaults? LineDefaults { get; init; }

	/// <summary>Whether the labels file starts with an <c>isTextDefaults</c> Feature built from <see cref="TextDefaults"/>.</summary>
	public bool IncludeCrcTextDefaults { get; init; }

	/// <summary>The CRC Text defaults; set exactly when <see cref="IncludeCrcTextDefaults"/> is <see langword="true"/>.</summary>
	public CrcTextDefaults? TextDefaults { get; init; }
}
