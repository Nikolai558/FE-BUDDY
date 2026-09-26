using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Core.Application.Airac.WxStations.Models;

/// <summary>
/// Fully-parsed, typed settings for the Wx Stations sub-service. Built once by
/// <c>WxStationSettingsParser.Parse</c> from the raw <c>Dictionary&lt;string, string&gt;</c> the
/// GUI (or <c>FeBuddy.Harness</c>) supplies; every other Wx Stations type consumes this typed
/// object, never the raw dictionary.
/// </summary>
/// <remarks>
/// The simplest AIRAC sub-service settings: there is no <c>OutputBy</c> (one merged file pair), no
/// alias file, and no <c>feb.*</c> properties.
/// </remarks>
public sealed record WxStationSettings
{
	/// <summary>
	/// The folder the run writes into - the <c>AIRAC_&lt;cycle&gt;</c> folder when run by the AIRAC
	/// Service. Files are laid out inside it by <see cref="AiracOutputPaths"/>.
	/// </summary>
	public required string OutputDirectory { get; init; }

	/// <summary>Emit a symbol per included station.</summary>
	public bool EmitSymbols { get; init; } = true;

	/// <summary>Emit a label per included station.</summary>
	public bool EmitText { get; init; } = true;

	/// <summary>
	/// Which files go to vNAS, and which of those get CRC-ERAM defaults, by file key (see
	/// <see cref="WxStationOutputFiles"/>). Default: none.
	/// </summary>
	public VnasFileChoices Vnas { get; init; } = VnasFileChoices.None;

	/// <summary>
	/// The Region of Interest the GeoJSON output is filtered to, or <see langword="null"/> for no
	/// filtering. A station is in or out on its own coordinates.
	/// </summary>
	public RegionOfInterest? Roi { get; init; }

	/// <summary>Maximum decimal places for coordinates written to GeoJSON. Default 6.</summary>
	public int CoordinatePrecision { get; init; } = 6;

	/// <summary>
	/// CRC symbol property defaults, keyed by class name, case-insensitively. Only ever holds
	/// <see cref="WxStationOutputFiles.AllClass"/> - a dictionary for the same shape every other
	/// sub-service's settings use, even though Wx Stations has just the one class.
	/// </summary>
	public IReadOnlyDictionary<string, CrcSymbolDefaults> SymbolDefaults { get; init; } =
		new Dictionary<string, CrcSymbolDefaults>(StringComparer.OrdinalIgnoreCase);

	/// <summary>CRC text property defaults, keyed the same way as <see cref="SymbolDefaults"/>.</summary>
	public IReadOnlyDictionary<string, CrcTextDefaults> TextDefaults { get; init; } =
		new Dictionary<string, CrcTextDefaults>(StringComparer.OrdinalIgnoreCase);
}
