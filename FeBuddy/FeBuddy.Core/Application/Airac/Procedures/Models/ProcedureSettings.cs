using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Core.Application.Airac.Procedures.Models;

/// <summary>
/// Fully-parsed, typed settings for the Procedures sub-service. Built once by
/// <c>ProcedureSettingsParser.Parse</c> from the raw <c>Dictionary&lt;string, string&gt;</c> the GUI
/// (or <c>FeBuddy.Harness</c>) supplies; every other Procedures type consumes this typed object,
/// never the raw dictionary.
/// </summary>
/// <remarks>
/// <para>
/// Unlike every other AIRAC sub-service, there is no GeoJSON and no <c>feb.*</c> properties. The
/// outputs are two documents, <c>Procedure_Changes.md</c> and <c>Procedures.json</c>, written to
/// <see cref="AiracOutputPaths.PublicationDocsFolder"/>, and the FAA Chart Recall alias file,
/// <c>Faa_Chart_Recall.txt</c>, written where every alias file goes.
/// </para>
/// <para>
/// The selection settings - facilities, airports, procedures, chart types - pick what the two
/// documents cover. The alias file ignores them: it covers every chart at every airport in the
/// metafile.
/// </para>
/// </remarks>
public sealed record ProcedureSettings
{
	/// <summary>
	/// The folder the run writes into - the <c>AIRAC_&lt;cycle&gt;</c> folder when run by the AIRAC
	/// Service. Both documents go in its <see cref="AiracOutputPaths.PublicationDocsFolder"/> folder.
	/// </summary>
	public required string OutputDirectory { get; init; }

	/// <summary>Whether to write <c>Procedure_Changes.md</c>. Default <see langword="true"/>.</summary>
	public bool GenerateChangesDocument { get; init; } = true;

	/// <summary>Whether to write <c>Procedures.json</c>. Default <see langword="true"/>.</summary>
	public bool GenerateProceduresJson { get; init; } = true;

	/// <summary>
	/// Whether to write the FAA Chart Recall alias file, <c>Faa_Chart_Recall.txt</c>. Default
	/// <see langword="true"/>.
	/// </summary>
	public bool GenerateAliasFile { get; init; } = true;

	/// <summary>
	/// Whether the alias file goes to vNAS: copied into <c>Upload_to_vNAS\vNAS_Alias.txt</c> as well as
	/// written to the <c>Aliases</c> folder. Its only possible file key is <c>Faa_Chart_Recall.txt</c>;
	/// nothing Procedures writes carries CRC-ERAM defaults.
	/// </summary>
	public VnasFileChoices Vnas { get; init; } = VnasFileChoices.None;

	/// <summary>
	/// The names the user gave files in place of FE-Buddy's, by file key. Not part of the settings
	/// block: <see cref="ProcedureService.Run"/> takes it from the AIRAC Service. Default: none
	/// renamed.
	/// </summary>
	public OutputFileNames FileNames { get; init; } = OutputFileNames.None;

	/// <summary>
	/// Every included airport's every procedure is included whenever its
	/// <c>ResponsibleArtcc</c> is one of these, trimmed and upper-cased. Default: none.
	/// </summary>
	public IReadOnlyCollection<string> Facilities { get; init; } = [];

	/// <summary>
	/// The facility whose section is listed first in both documents, or <see langword="null"/> for
	/// none (facilities are then all ordered alphabetically).
	/// </summary>
	public string? PrimaryFacility { get; init; }

	/// <summary>
	/// When <see langword="true"/>, every airport whose NASR coordinates fall inside <see cref="Roi"/>
	/// is included, the same as a <see cref="Facilities"/> match. Default <see langword="false"/>.
	/// </summary>
	public bool IncludeRoiAirports { get; init; }

	/// <summary>
	/// The Region of Interest airports are tested against when <see cref="IncludeRoiAirports"/> is
	/// <see langword="true"/> (the same shared ROI every sub-service reads); otherwise unused.
	/// </summary>
	public RegionOfInterest? Roi { get; init; }

	/// <summary>
	/// Airports (whole) to include regardless of <see cref="Facilities"/> or <see cref="Roi"/>,
	/// matched against the metafile's FAA or ICAO identifier, ignoring case. Default: none.
	/// </summary>
	public IReadOnlyCollection<string> Airports { get; init; } = [];

	/// <summary>
	/// Procedure base chart names to include at every airport that has one, regardless of
	/// <see cref="ChartTypes"/> or whether the airport itself is otherwise included. Default: none.
	/// </summary>
	public IReadOnlyCollection<string> Procedures { get; init; } = [];

	/// <summary>
	/// Specific airport + procedure pairs to include, regardless of <see cref="ChartTypes"/> or
	/// whether the airport itself is otherwise included. Default: none.
	/// </summary>
	public IReadOnlyList<ProcedureAirportPick> AirportProcedures { get; init; } = [];

	/// <summary>
	/// The chart types (d-TPP Metafile <c>chart_code</c>) a whole included airport's procedures are
	/// limited to; a procedure named by <see cref="Procedures"/> or <see cref="AirportProcedures"/>
	/// is included regardless. Default <see cref="Domain.Procedures.ProcedureChartTypes.Default"/>.
	/// </summary>
	public IReadOnlyCollection<string> ChartTypes { get; init; } = Domain.Procedures.ProcedureChartTypes.Default;

	/// <summary>
	/// Which optional fields <c>Procedures.json</c> writes, on top of the fields always written
	/// (<c>cycle</c>, <c>effectiveDate</c>, <c>airportId</c>, a procedure's <c>name</c>). Default:
	/// see <c>ProcedureSettingsParser</c>.
	/// </summary>
	public IReadOnlyCollection<ProcedureJsonField> JsonFields { get; init; } = [];
}
