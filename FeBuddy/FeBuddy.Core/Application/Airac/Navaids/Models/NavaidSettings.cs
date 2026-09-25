using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Core.Application.Airac.Navaids.Models;

/// <summary>
/// Fully-parsed, typed settings for the NAVAIDs sub-service (GeoJSON generation and alias file
/// generation). Built once by <c>NavaidSettingsParser.Parse</c> from the raw
/// <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>) supplies; every
/// other NAVAIDs service consumes this typed object, never the raw dictionary.
/// </summary>
public sealed record NavaidSettings
{
	/// <summary>
	/// The folder the run writes into - the <c>AIRAC_&lt;cycle&gt;</c> folder when run by the AIRAC
	/// Service. Files are laid out inside it by <see cref="AiracOutputPaths"/>.
	/// </summary>
	public required string OutputDirectory { get; init; }

	/// <summary>Whether to write GeoJSON at all. <see langword="false"/> means alias output only.</summary>
	public required bool GenerateGeojson { get; init; }

	/// <summary>Emit a symbol per included NAVAID. There is no NAVAIDs Lines file.</summary>
	public bool EmitSymbols { get; init; } = true;

	/// <summary>Emit a label per included NAVAID.</summary>
	public bool EmitText { get; init; } = true;

	/// <summary>Whether to write the <c>NAVAIDs.txt</c> alias file.</summary>
	public required bool GenerateAliasFile { get; init; }

	/// <summary>How to group NAVAIDs into GeoJSON files. Default <see cref="NavaidOutputBy.All"/>.</summary>
	public NavaidOutputBy OutputBy { get; init; } = NavaidOutputBy.All;

	/// <summary>
	/// NASR <c>NAV_TYPE</c> names to leave out of every output (GeoJSON and the alias file),
	/// trimmed and upper-cased. Default: none.
	/// </summary>
	public IReadOnlyCollection<string> ExcludedTypes { get; init; } = [];

	/// <summary>
	/// How a merged <see cref="NavaidOutputBy.All"/> Symbols file assigns each NAVAID's CRC symbol
	/// style. Read (and meaningful) only when <see cref="OutputBy"/> is <see cref="NavaidOutputBy.All"/>.
	/// Default <see cref="NavaidSymbolStyleBy.Type"/>.
	/// </summary>
	public NavaidSymbolStyleBy SymbolStyleBy { get; init; } = NavaidSymbolStyleBy.Type;

	/// <summary>
	/// The CRC symbol style fan markers are drawn with when <see cref="SymbolStyleBy"/> is
	/// <see cref="NavaidSymbolStyleBy.Type"/>, or <see langword="null"/> when none was chosen or it
	/// is not used (see <c>NavaidSettingsParser</c> for exactly when it is read). A fan marker
	/// written without one gets no style, and the run says so.
	/// </summary>
	public string? FanMarkerStyle { get; init; }

	/// <summary>
	/// When <see langword="true"/>, NAVAID Features carry the <c>feb.*</c> properties listed in
	/// <see cref="FebProperties"/>.
	/// </summary>
	public required bool IncludeFebCustomProperties { get; init; }

	/// <summary>
	/// Which <c>feb.*</c> properties to write when <see cref="IncludeFebCustomProperties"/> is
	/// <see langword="true"/>. The Text file always omits <see cref="NavaidFebProperty.NavId"/>,
	/// <see cref="NavaidFebProperty.NavType"/> and <see cref="NavaidFebProperty.Name"/>, which its
	/// <c>text</c> array already carries.
	/// </summary>
	public IReadOnlyCollection<NavaidFebProperty> FebProperties { get; init; } = [];

	/// <summary>
	/// Which files go to vNAS, and which of those get CRC-ERAM defaults, by file key (see
	/// <see cref="NavaidOutputFiles"/>). Default: none.
	/// </summary>
	public VnasFileChoices Vnas { get; init; } = VnasFileChoices.None;

	/// <summary>
	/// The Region of Interest the GeoJSON output is filtered to, or <see langword="null"/> for
	/// no filtering. A NAVAID is in or out on its own coordinates; the alias file ignores this
	/// entirely and always covers every included NAVAID.
	/// </summary>
	public RegionOfInterest? Roi { get; init; }

	/// <summary>Maximum decimal places for coordinates written to GeoJSON. Default 6.</summary>
	public int CoordinatePrecision { get; init; } = 6;

	/// <summary>
	/// CRC symbol property defaults, keyed by class name, case-insensitively:
	/// <see cref="NavaidOutputFiles.AllClass"/> in <see cref="NavaidOutputBy.All"/> mode, or each
	/// present type's token (<c>Domain.Navaids.NavaidTypes.Token</c>) in
	/// <see cref="NavaidOutputBy.Type"/> mode. A dictionary rather than an enum-keyed one because
	/// the per-type classes are entirely data-driven.
	/// </summary>
	public IReadOnlyDictionary<string, CrcSymbolDefaults> SymbolDefaults { get; init; } =
		new Dictionary<string, CrcSymbolDefaults>(StringComparer.OrdinalIgnoreCase);

	/// <summary>CRC text property defaults, keyed the same way as <see cref="SymbolDefaults"/>.</summary>
	public IReadOnlyDictionary<string, CrcTextDefaults> TextDefaults { get; init; } =
		new Dictionary<string, CrcTextDefaults>(StringComparer.OrdinalIgnoreCase);
}
