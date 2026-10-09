using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Settings;
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
/// As with Telephony, there is no GeoJSON and no <c>feb.*</c> properties. The outputs are two
/// documents, <c>Procedure_Changes.md</c> and <c>Procedures.json</c>, written to
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
	/// The names the user gave files in place of FE-Buddy's, by file key. Not part of the settings
	/// block: <see cref="ProcedureService.Run"/> takes it from the AIRAC Service. Default: none
	/// renamed.
	/// </summary>
	public OutputFileNames FileNames { get; init; } = OutputFileNames.None;

	/// <summary>
	/// Which airports are included as a whole: those in <see cref="Facilities"/>
	/// (<see cref="SubServiceArea.Artccs"/>), those inside <see cref="Roi"/>, every airport, or none
	/// (only the airports and procedures listed). Default <see cref="SubServiceArea.None"/>.
	/// </summary>
	public SubServiceArea Area { get; init; } = SubServiceArea.None;

	/// <summary>
	/// With <see cref="Area"/> <see cref="SubServiceArea.Artccs"/>, every airport whose
	/// <c>ResponsibleArtcc</c> is one of these, trimmed and upper-cased, is included. Otherwise none.
	/// </summary>
	public IReadOnlyCollection<string> Facilities { get; init; } = [];

	/// <summary>
	/// The facility whose section is listed first in both documents, or <see langword="null"/> for
	/// none (facilities are then all ordered alphabetically).
	/// </summary>
	public string? PrimaryFacility { get; init; }

	/// <summary>
	/// With <see cref="Area"/> <see cref="SubServiceArea.Roi"/>, every airport whose NASR coordinates
	/// fall inside it is included. Otherwise <see langword="null"/>.
	/// </summary>
	public RegionOfInterest? Roi { get; init; }

	/// <summary>
	/// Airports (whole) to include whatever <see cref="Area"/> leaves out, matched against the
	/// metafile's FAA or ICAO identifier, ignoring case. None with <see cref="SubServiceArea.Everything"/>,
	/// which already has every airport. Default: none.
	/// </summary>
	public IReadOnlyCollection<string> Airports { get; init; } = [];

	/// <summary>
	/// Procedure base chart names to include at every airport that has one, regardless of
	/// <see cref="ChartTypes"/> or whether the airport itself is otherwise included. None with
	/// <see cref="SubServiceArea.Everything"/>. Default: none.
	/// </summary>
	public IReadOnlyCollection<string> Procedures { get; init; } = [];

	/// <summary>
	/// Specific airport + procedure pairs to include, regardless of <see cref="ChartTypes"/> or
	/// whether the airport itself is otherwise included. None with <see cref="SubServiceArea.Everything"/>.
	/// Default: none.
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
