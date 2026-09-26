using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Core.Application.Airac.Fixes.Models;

/// <summary>
/// Fully-parsed, typed settings for the Fixes sub-service. Built once by
/// <c>FixSettingsParser.Parse</c> from the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or
/// <c>FeBuddy.Harness</c>) supplies; every other Fixes service consumes this typed object, never
/// the raw dictionary.
/// </summary>
/// <remarks>
/// Unlike most other AIRAC sub-services, there is no <c>GenerateGeojson</c> toggle and no alias
/// file: the sub-service always writes GeoJSON.
/// </remarks>
public sealed record FixSettings
{
	/// <summary>
	/// The folder the run writes into - the <c>AIRAC_&lt;cycle&gt;</c> folder when run by the AIRAC
	/// Service. Files are laid out inside it by <see cref="AiracOutputPaths"/>.
	/// </summary>
	public required string OutputDirectory { get; init; }

	/// <summary>Emit a symbol per included fix.</summary>
	public bool EmitSymbols { get; init; } = true;

	/// <summary>Emit a label per included fix.</summary>
	public bool EmitText { get; init; } = true;

	/// <summary>How to group fixes into GeoJSON files. Default <see cref="FixOutputBy.All"/>.</summary>
	public FixOutputBy OutputBy { get; init; } = FixOutputBy.All;

	/// <summary>
	/// Fix use tokens (<see cref="Domain.Fixes.FixUses.Token"/>) to leave out of the
	/// <see cref="FixOutputBy.FixUse"/> file layout. Meaningless in every other layout. Default:
	/// none.
	/// </summary>
	public IReadOnlyCollection<string> ExcludedFixUses { get; init; } = [];

	/// <summary>
	/// Chart tokens (<see cref="Domain.Fixes.FixCharts.Token"/>) to leave out of the
	/// <see cref="FixOutputBy.Chart"/> file layout. Meaningless in every other layout. Default:
	/// none.
	/// </summary>
	public IReadOnlyCollection<string> ExcludedCharts { get; init; } = [];

	/// <summary>
	/// The chart + fix use combinations to write when <see cref="OutputBy"/> is
	/// <see cref="FixOutputBy.ChartAndFixUse"/>, in the order to write them. Default: none.
	/// </summary>
	public IReadOnlyList<FixCombination> Combinations { get; init; } = [];

	/// <summary>
	/// When <see langword="true"/>, fix Features carry the <c>feb.*</c> properties listed in
	/// <see cref="FebProperties"/>.
	/// </summary>
	public required bool IncludeFebCustomProperties { get; init; }

	/// <summary>
	/// Which <c>feb.*</c> properties to write when <see cref="IncludeFebCustomProperties"/> is
	/// <see langword="true"/>. The Text file always omits <see cref="FixFebProperty.FixId"/>,
	/// which its <c>text</c> array already carries.
	/// </summary>
	public IReadOnlyCollection<FixFebProperty> FebProperties { get; init; } = [];

	/// <summary>
	/// Which files go to vNAS, and which of those get CRC-ERAM defaults, by file key (see
	/// <see cref="FixOutputFiles"/>). Default: none.
	/// </summary>
	public VnasFileChoices Vnas { get; init; } = VnasFileChoices.None;

	/// <summary>
	/// The Region of Interest the GeoJSON output is filtered to, or <see langword="null"/> for no
	/// filtering. A fix is in or out on its own coordinates.
	/// </summary>
	public RegionOfInterest? Roi { get; init; }

	/// <summary>Maximum decimal places for coordinates written to GeoJSON. Default 6.</summary>
	public int CoordinatePrecision { get; init; } = 6;

	/// <summary>
	/// CRC symbol property defaults, keyed by class name, case-insensitively:
	/// <see cref="FixOutputFiles.AllClass"/> in <see cref="FixOutputBy.All"/> mode, or each
	/// present group's token in another mode. A dictionary rather than an enum-keyed one because
	/// the per-group classes are entirely data-driven.
	/// </summary>
	public IReadOnlyDictionary<string, CrcSymbolDefaults> SymbolDefaults { get; init; } =
		new Dictionary<string, CrcSymbolDefaults>(StringComparer.OrdinalIgnoreCase);

	/// <summary>CRC text property defaults, keyed the same way as <see cref="SymbolDefaults"/>.</summary>
	public IReadOnlyDictionary<string, CrcTextDefaults> TextDefaults { get; init; } =
		new Dictionary<string, CrcTextDefaults>(StringComparer.OrdinalIgnoreCase);
}
