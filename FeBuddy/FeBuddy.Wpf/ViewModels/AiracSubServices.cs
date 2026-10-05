using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Application.Airac.Airports;
using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Application.Airac.Arrivals;
using FeBuddy.Core.Application.Airac.Departures;
using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Telephony;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The AIRAC Service sub-service catalogue: every data topic the service can produce output for,
/// whether or not its backend exists yet.
/// </summary>
/// <remarks>
/// <para>
/// This list is the only place a sub-service is registered. Adding one means adding an entry here
/// and a tab view-model for it; the General tab's table, the tab rail, the save contract and the
/// Preview Settings tab all pick it up with no further changes. The list is expected to grow to
/// roughly twenty entries.
/// </para>
/// <para>
/// <see cref="SubServiceDescriptor.Key"/> is persisted in <c>UserConfig</c> under
/// <c>Services.AiracService.SelectedSubServices</c> and <c>Services.AiracService.Outputs.&lt;Key&gt;</c>,
/// so a key may not be renamed without migrating those values.
/// <see cref="SubServiceDescriptor.IsImplemented"/> is <see langword="false"/> for a sub-service that
/// has no library code behind it yet: its tab opens and explains itself, and it contributes nothing
/// to a run.
/// </para>
/// </remarks>
public static class AiracSubServices
{
	/// <summary>The Airports sub-service key.</summary>
	public const string AirportsKey = "Airports";

	/// <summary>The Airways sub-service key.</summary>
	public const string AirwaysKey = "Airways";

	/// <summary>The Departures sub-service key.</summary>
	public const string DeparturesKey = "Departures";

	/// <summary>The Arrivals sub-service key.</summary>
	public const string ArrivalsKey = "Arrivals";

	/// <summary>The NAVAIDs sub-service key.</summary>
	public const string NavaidsKey = "Navaids";

	/// <summary>The ARTCC Boundaries sub-service key.</summary>
	public const string ArtccBoundariesKey = "ArtccBoundaries";

	/// <summary>The Fixes sub-service key.</summary>
	public const string FixesKey = "Fixes";

	/// <summary>The Wx Stations sub-service key.</summary>
	public const string WxStationsKey = "WxStations";

	/// <summary>The Procedures sub-service key.</summary>
	public const string ProceduresKey = "Procedures";

	/// <summary>The Telephony sub-service key.</summary>
	public const string TelephonyKey = "Telephony";

	/// <summary>The Concatenate Aliases sub-service key.</summary>
	public const string ConcatenateAliasesKey = "ConcatenateAliases";

	private const SubServiceOutputKinds AliasAndGeojson = SubServiceOutputKinds.Alias | SubServiceOutputKinds.Geojson;

	/// <summary>
	/// Every sub-service, in the order the General tab's table and the tab rail show them. The
	/// Concatenate Aliases tab lists FE-Buddy's alias files in this order too, straight from the list,
	/// so keep the <see cref="SubServiceDescriptor.Order"/> values in the same order as the entries.
	/// </summary>
	public static IReadOnlyList<SubServiceDescriptor> All { get; } =
	[
		new SubServiceDescriptor(ArtccBoundariesKey, "ARTCC Boundaries", 10, true, () => new ArtccBoundariesViewModel(),
			Outputs: SubServiceOutputKinds.Geojson,
			Help: new SubServiceHelp(
				"Each ARTCC's boundary from the FAA's NASR data, drawn as lines. GeoJSON only.",
				Geojson: "ARTCC boundary lines: ARTCC-Boundary_High_Lines and _Low_Lines (with _Unlimited_Lines if you like), " +
					"or one file per ARTCC and altitude. Narrow them by ARTCC and by the region of interest, which clips them at its edge.")),
		new SubServiceDescriptor(AirportsKey, "Airports", 20, true, () => new AirportsViewModel(), AirportOutputFiles.Alias, AliasAndGeojson,
			new SubServiceHelp(
				"Every airport in the FAA's NASR data: its runways, a symbol and a label for the map, and an alias command that shows its details in CRC.",
				Alias: "Airports.txt: an .apt command for each airport's FAA ID, and its ICAO ID when that's different, showing its name, " +
					"tower, ARTCC, longest runway, elevation, pattern altitude, FSS, CTAF, weather, hours and airspace. " +
					"It covers every open airport; the region of interest doesn't narrow it.",
				Geojson: "Runways_Lines, Airports_Symbols and Airports_Text (FAA ID and name). Choose which of those files, " +
					"FE-Buddy properties, and the region of interest: an airport is in when its reference point is inside it.")),
		new SubServiceDescriptor(AirwaysKey, "Airways", 30, true, () => new AirwaysViewModel(), AirwayOutputFiles.Alias, AliasAndGeojson,
			new SubServiceHelp(
				"Every airway in the FAA's NASR data: its line, waypoint symbols and labels for the map, and an alias command that draws its fixes on the scope.",
				Alias: "Airways.txt: a command per airway that draws its fixes, e.g. .J3F. It covers every FAA airway, " +
					"or only those in the region of interest.",
				Geojson: "Airways_High and Airways_Low, or a set per designation (J, V, Q, T, …), each with Lines, Symbols and Text. " +
					"Choose the designations, which file each goes in, buffering lines short of their waypoints, " +
					"FE-Buddy properties and the region of interest.")),
		new SubServiceDescriptor(ArrivalsKey, "Arrivals", 40, true, () => new ArrivalsViewModel(), ArrivalOutputFiles.Alias, AliasAndGeojson,
			new SubServiceHelp(
				"STARs from the FAA's NASR data: each one's lines, points and labels in a folder per airport, and an alias command per airport and STAR that draws its fixes.",
				Alias: "Arrivals.txt: a command per airport and STAR, e.g. .lasBLAIDf, that draws its fixes, transitions first. " +
					"The ARTCCs, amendment date and region of interest narrow it, as they do the GeoJSON.",
				Geojson: "Lines, Symbols and Text for each airport and STAR, in Geojson\\<ARTCC>\\<airport>\\. " +
					"Narrow them by ARTCC, amendment date and region of interest; add FE-Buddy properties.")),
		new SubServiceDescriptor(DeparturesKey, "Departures", 50, true, () => new DeparturesViewModel(), DepartureOutputFiles.Alias, AliasAndGeojson,
			new SubServiceHelp(
				"SIDs, and obstacle departures (ODPs) if you like, from the FAA's NASR data: each one's lines, points and labels in a folder per airport, and an alias command per airport and departure that draws its fixes.",
				Alias: "Departures.txt: a command per airport and departure, e.g. .laxDOTSSf, that draws its fixes. " +
					"The ARTCCs, amendment date and region of interest narrow it, as they do the GeoJSON.",
				Geojson: "Lines, Symbols and Text for each airport and departure, in Geojson\\<ARTCC>\\<airport>\\. " +
					"Narrow them by ARTCC, amendment date and region of interest; add FE-Buddy properties.")),
		new SubServiceDescriptor(NavaidsKey, "NAVAIDs", 60, true, () => new NavaidsViewModel(), NavaidOutputFiles.Alias, AliasAndGeojson,
			new SubServiceHelp(
				"NAVAIDs from the FAA's NASR data: a symbol and a label for the map, and alias commands that show each one's name, type, frequency and ARTCCs.",
				Alias: "Navaids.txt: a .nav command for each identifier and each name. The NAVAID types you untick are left out of it too; " +
					"the region of interest doesn't narrow it.",
				Geojson: "NAVAIDs_Symbols and NAVAIDs_Text, or a pair per NAVAID type. Choose the NAVAID types, the file layout, " +
					"how symbols are styled, FE-Buddy properties and the region of interest.")),
		new SubServiceDescriptor(FixesKey, "Fixes", 70, true, () => new FixesViewModel(),
			Outputs: SubServiceOutputKinds.Geojson,
			Help: new SubServiceHelp(
				"A symbol and a label for every fix in the FAA's NASR data. GeoJSON only.",
				Geojson: "Fix_Symbols and Fix_Text, or files per fix use, per chart, or per chart and fix use. " +
					"Choose the fix uses and charts, FE-Buddy properties and the region of interest.")),
		new SubServiceDescriptor(ProceduresKey, "Procedures", 80, true, () => new ProceduresViewModel(), ProcedureOutputFiles.Alias,
			SubServiceOutputKinds.Alias | SubServiceOutputKinds.ProcedureChanges | SubServiceOutputKinds.ProceduresJson,
			new SubServiceHelp(
				"The FAA's terminal procedure charts, from the d-TPP Metafile: what changed this cycle, a list of every chart, and alias commands that open a chart.",
				Alias: "Faa_Chart_Recall.txt: a command for each page of every chart at every airport in the metafile, e.g. .dtwI22Lc, " +
					"that opens it from the FAA. What you pick for the documents doesn't narrow it.",
				ProcedureChanges: "Procedure_Changes.md: every chart added, changed or deleted this cycle at the facilities, airports and " +
					"procedures you pick, grouped by facility, with links to the FAA's comparison PDFs and charts.",
				ProceduresJson: "Procedures.json: every current chart at the airports you pick, with the fields you choose, for other tools to read.")),
		new SubServiceDescriptor(TelephonyKey, "Telephony", 90, true, () => new TelephonyViewModel(), TelephonyOutputFiles.Alias,
			SubServiceOutputKinds.Alias,
			new SubServiceHelp(
				"Operators' call signs from the FAA's telephony list, and your facility's virtual airlines, as alias commands. Alias file only.",
				Alias: "Telephony.txt: an .id command for each operator's designator and its telephony, e.g. .idAVA and .idAVIANCA, " +
					"showing who it is. Add your virtual airlines, and VATSIM-Radar's list if you like; the region of interest doesn't narrow it.")),
		new SubServiceDescriptor(WxStationsKey, "Wx Stations", 100, true, () => new WxStationsViewModel(),
			Outputs: SubServiceOutputKinds.Geojson,
			Help: new SubServiceHelp(
				"A symbol and a label for every US and US-territory station that reports METAR, from aviationweather.gov. GeoJSON only.",
				Geojson: "Wx_Symbols and Wx_Text (the ICAO ID, then the IATA ID and site name). The region of interest narrows them.")),
		new SubServiceDescriptor(ConcatenateAliasesKey, "Concatenate Aliases", 110, true, () => new ConcatenateAliasesViewModel(),
			Help: new SubServiceHelp(
				"Combines every alias file the run makes, then your facility's own alias files, into Combined_Alias.txt: " +
				"vNAS takes one alias file per facility.")),
	];
}
