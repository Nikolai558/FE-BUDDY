using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Core.Application.Airac.Departures.Models;

/// <summary>
/// Fully-parsed, typed settings for the Departures sub-service. Built once by
/// <c>DepartureSettingsParser.Parse</c> from the raw <c>Dictionary&lt;string, string&gt;</c> the
/// GUI (or <c>FeBuddy.Harness</c>) supplies; every other Departures service consumes this typed
/// object, never the raw dictionary.
/// </summary>
/// <remarks>
/// Unlike Airports, every filter here (obstacle departures, ARTCC, amendment date, ROI) applies
/// to the alias file as well as the GeoJSON output.
/// </remarks>
public sealed record DepartureSettings
{
	/// <summary>
	/// The folder the run writes into - the <c>AIRAC_&lt;cycle&gt;</c> folder when run by the AIRAC
	/// Service. Files are laid out inside it by <see cref="DepartureOutputFiles"/>.
	/// </summary>
	public required string OutputDirectory { get; init; }

	/// <summary>Whether to write GeoJSON at all. <see langword="false"/> means alias output only.</summary>
	public required bool GenerateGeojson { get; init; }

	/// <summary>Emit <c>&lt;ARPT&gt;_&lt;CODE&gt;_Lines.geojson</c> - one MultiLineString per airport and procedure.</summary>
	public bool EmitLines { get; init; } = true;

	/// <summary>Emit <c>&lt;ARPT&gt;_&lt;CODE&gt;_Symbols.geojson</c> - one Point per procedure point.</summary>
	public bool EmitSymbols { get; init; } = true;

	/// <summary>Emit <c>&lt;ARPT&gt;_&lt;CODE&gt;_Text.geojson</c> - one labelled Point per procedure point.</summary>
	public bool EmitText { get; init; } = true;

	/// <summary>Whether to write the <c>Departures.txt</c> alias file.</summary>
	public required bool GenerateAliasFile { get; init; }

	/// <summary>Whether obstacle departures (ODPs) are included alongside SIDs. Default <see langword="true"/>.</summary>
	public bool IncludeObstacleDepartures { get; init; } = true;

	/// <summary>
	/// The ARTCCs whose procedures are included, or empty for every ARTCC.
	/// </summary>
	public IReadOnlyCollection<string> ArtccFilter { get; init; } = [];

	/// <summary>
	/// Which amendment-date filter applies. <see cref="DepartureAmendmentFilter.None"/> (the
	/// default) keeps every procedure. Each other mode reads exactly one of
	/// <see cref="AmendedWithinCycles"/>, <see cref="AmendedWithinDays"/> or
	/// <see cref="AmendedOnOrAfter"/>; the other two are ignored.
	/// </summary>
	public DepartureAmendmentFilter AmendmentFilter { get; init; } = DepartureAmendmentFilter.None;

	/// <summary>
	/// Read only when <see cref="AmendmentFilter"/> is <see cref="DepartureAmendmentFilter.Cycles"/>:
	/// keep only procedures whose current amendment became effective within this many cycles,
	/// counting the selected cycle as the first.
	/// </summary>
	/// <remarks>
	/// <c>1</c> means "amended this cycle"; <c>4</c> means "amended in this cycle or any of the
	/// three before it". Cycles are counted back from each row's own <c>EFF_DATE</c> in
	/// 28-day steps.
	/// </remarks>
	public int AmendedWithinCycles { get; init; }

	/// <summary>
	/// Read only when <see cref="AmendmentFilter"/> is <see cref="DepartureAmendmentFilter.Days"/>:
	/// keep only procedures whose current amendment became effective on or after the run's local
	/// date minus this many days.
	/// </summary>
	public int AmendedWithinDays { get; init; }

	/// <summary>
	/// Read only when <see cref="AmendmentFilter"/> is <see cref="DepartureAmendmentFilter.Date"/>:
	/// keep only procedures whose current amendment became effective on or after this date.
	/// </summary>
	public DateOnly? AmendedOnOrAfter { get; init; }

	/// <summary>
	/// The Region of Interest, or <see langword="null"/> for the whole NASR database of
	/// departure procedures.
	/// </summary>
	public RegionOfInterest? Roi { get; init; }

	/// <summary>How <see cref="Roi"/> decides what is in scope. Ignored when <see cref="Roi"/> is <see langword="null"/>.</summary>
	public DepartureRoiMode RoiMode { get; init; } = DepartureRoiMode.Airport;

	/// <summary>When <see langword="true"/>, Features carry the <c>feb.*</c> properties listed in <see cref="FebProperties"/>.</summary>
	public required bool IncludeFebCustomProperties { get; init; }

	/// <summary>Which <c>feb.*</c> properties to write when <see cref="IncludeFebCustomProperties"/> is <see langword="true"/>.</summary>
	public IReadOnlyCollection<DepartureFebProperty> FebProperties { get; init; } = [];

	/// <summary>
	/// Which kinds of file go to vNAS, and which of those get CRC-ERAM defaults, by file key (see
	/// <see cref="DepartureOutputFiles"/>). Default: none.
	/// </summary>
	public VnasFileChoices Vnas { get; init; } = VnasFileChoices.None;

	/// <summary>Maximum decimal places for coordinates written to GeoJSON. Default 6.</summary>
	public int CoordinatePrecision { get; init; } = 6;

	/// <summary>CRC line property defaults. Populated when <c>Departures_Lines</c> gets CRC-ERAM defaults.</summary>
	public IReadOnlyDictionary<DepartureCrcClass, CrcLineDefaults> LineDefaults { get; init; } =
		new Dictionary<DepartureCrcClass, CrcLineDefaults>();

	/// <summary>CRC symbol property defaults. Populated when <c>Departures_Symbols</c> gets CRC-ERAM defaults.</summary>
	public IReadOnlyDictionary<DepartureCrcClass, CrcSymbolDefaults> SymbolDefaults { get; init; } =
		new Dictionary<DepartureCrcClass, CrcSymbolDefaults>();

	/// <summary>CRC text property defaults. Populated when <c>Departures_Text</c> gets CRC-ERAM defaults.</summary>
	public IReadOnlyDictionary<DepartureCrcClass, CrcTextDefaults> TextDefaults { get; init; } =
		new Dictionary<DepartureCrcClass, CrcTextDefaults>();
}
