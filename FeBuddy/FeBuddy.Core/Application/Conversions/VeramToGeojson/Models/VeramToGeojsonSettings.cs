using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.Core.Application.Conversions.VeramToGeojson.Models;

/// <summary>
/// Fully-parsed, typed settings for the vERAM to GeoJSON conversion. Built once by
/// <c>VeramToGeojsonSettingsParser.Parse</c> from the raw <c>Dictionary&lt;string, string&gt;</c>
/// the GUI (or <c>FeBuddy.Harness</c>) supplies.
/// </summary>
public sealed record VeramToGeojsonSettings : ConversionSettings
{
	/// <summary>How the files are laid out. Default <see cref="VeramOutputLayout.ByObject"/>.</summary>
	public VeramOutputLayout OutputLayout { get; init; } = VeramOutputLayout.ByObject;

	/// <summary>Where each file's CRC defaults come from. Default <see cref="VeramDefaultsSource.Xml"/>.</summary>
	public VeramDefaultsSource DefaultsSource { get; init; } = VeramDefaultsSource.Xml;

	/// <summary>
	/// The tab's CRC Line defaults, or <see langword="null"/> when none are given - always when
	/// <see cref="DefaultsSource"/> is <see cref="VeramDefaultsSource.Xml"/>.
	/// </summary>
	public CrcLineDefaults? LineDefaults { get; init; }

	/// <summary>The tab's CRC Symbol defaults, or <see langword="null"/>; see <see cref="LineDefaults"/>.</summary>
	public CrcSymbolDefaults? SymbolDefaults { get; init; }

	/// <summary>The tab's CRC Text defaults, or <see langword="null"/>; see <see cref="LineDefaults"/>.</summary>
	public CrcTextDefaults? TextDefaults { get; init; }
}
