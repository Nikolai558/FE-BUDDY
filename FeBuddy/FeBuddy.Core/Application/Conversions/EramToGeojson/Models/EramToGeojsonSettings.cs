using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.Core.Application.Conversions.EramToGeojson.Models;

/// <summary>
/// Fully-parsed, typed settings for the ERAM to GeoJSON conversion. Built once by
/// <c>EramToGeojsonSettingsParser.Parse</c> from the raw <c>Dictionary&lt;string, string&gt;</c>
/// the GUI (or <c>FeBuddy.Harness</c>) supplies.
/// </summary>
public sealed record EramToGeojsonSettings : ConversionSettings
{
	/// <summary>How the files are laid out. Default <see cref="EramOutputLayout.ByObject"/>.</summary>
	public EramOutputLayout OutputLayout { get; init; } = EramOutputLayout.ByObject;

	/// <summary>Where each file's CRC defaults come from. Default <see cref="EramDefaultsSource.Xml"/>.</summary>
	public EramDefaultsSource DefaultsSource { get; init; } = EramDefaultsSource.Xml;

	/// <summary>
	/// The tab's CRC Line defaults, or <see langword="null"/> when none are given - always when
	/// <see cref="DefaultsSource"/> is <see cref="EramDefaultsSource.Xml"/>.
	/// </summary>
	public CrcLineDefaults? LineDefaults { get; init; }

	/// <summary>The tab's CRC Symbol defaults, or <see langword="null"/>; see <see cref="LineDefaults"/>.</summary>
	public CrcSymbolDefaults? SymbolDefaults { get; init; }

	/// <summary>The tab's CRC Text defaults, or <see langword="null"/>; see <see cref="LineDefaults"/>.</summary>
	public CrcTextDefaults? TextDefaults { get; init; }
}
