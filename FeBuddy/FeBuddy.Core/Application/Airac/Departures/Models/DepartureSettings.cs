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
	/// <summary>Directory the Departures services write their output under.</summary>
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
	public IReadOnlyCollection<string> ArtccFilter { get; init; } = Array.Empty<string>();

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
	public IReadOnlyCollection<DepartureFebProperty> FebProperties { get; init; } = Array.Empty<DepartureFebProperty>();

	/// <summary>
	/// Whether the Line defaults (<see cref="LineDefaults"/>) are written as an isLineDefaults
	/// Feature. <see langword="true"/> only when the user asked for them and the file they belong
	/// to is being produced.
	/// </summary>
	public required bool IncludeCrcLineDefaults { get; init; }

	/// <summary>
	/// Whether the Symbol defaults (<see cref="SymbolDefaults"/>) are written as an isSymbolDefaults
	/// Feature. <see langword="true"/> only when the user asked for them and the file they belong
	/// to is being produced.
	/// </summary>
	public required bool IncludeCrcSymbolDefaults { get; init; }

	/// <summary>
	/// Whether the Text defaults (<see cref="TextDefaults"/>) are written as an isTextDefaults
	/// Feature. <see langword="true"/> only when the user asked for them and the file they belong
	/// to is being produced.
	/// </summary>
	public required bool IncludeCrcTextDefaults { get; init; }

	/// <summary>Maximum decimal places for coordinates written to GeoJSON. Default 6.</summary>
	public int CoordinatePrecision { get; init; } = 6;

	/// <summary>
	/// When <see langword="true"/> (default), output is written under a <c>FE-Buddy_Output</c>
	/// folder inside <see cref="OutputDirectory"/>; when <see langword="false"/>, straight into
	/// <see cref="OutputDirectory"/> (the <c>Departure Procedures</c> sub-folder is kept either way).
	/// </summary>
	public bool AddFeBuddyOutputFolder { get; init; } = true;

	/// <summary>CRC line property defaults. Populated when <see cref="IncludeCrcLineDefaults"/> is <see langword="true"/>.</summary>
	public IReadOnlyDictionary<DepartureCrcClass, CrcLineDefaults> LineDefaults { get; init; } =
		new Dictionary<DepartureCrcClass, CrcLineDefaults>();

	/// <summary>CRC symbol property defaults. Populated when <see cref="IncludeCrcSymbolDefaults"/> is <see langword="true"/>.</summary>
	public IReadOnlyDictionary<DepartureCrcClass, CrcSymbolDefaults> SymbolDefaults { get; init; } =
		new Dictionary<DepartureCrcClass, CrcSymbolDefaults>();

	/// <summary>CRC text property defaults. Populated when <see cref="IncludeCrcTextDefaults"/> is <see langword="true"/>.</summary>
	public IReadOnlyDictionary<DepartureCrcClass, CrcTextDefaults> TextDefaults { get; init; } =
		new Dictionary<DepartureCrcClass, CrcTextDefaults>();
}
