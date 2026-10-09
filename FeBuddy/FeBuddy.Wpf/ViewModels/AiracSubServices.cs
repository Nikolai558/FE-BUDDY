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
/// The AIRAC Service sub-service catalogue: every data topic the service can produce output for.
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
/// so renaming a key loses what was saved under the old one.
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
		new SubServiceDescriptor(ArtccBoundariesKey, "ARTCC Boundaries", 10, () => new ArtccBoundariesViewModel(),
			Outputs: SubServiceOutputKinds.Geojson,
			Help: new SubServiceHelp(
				"Each ARTCC's boundary as lines, from the FAA's NASR data. GeoJSON only.",
				Geojson: "Boundary lines in ARTCC-Boundary_High_Lines and _Low_Lines, plus _Unlimited_Lines if you want it, " +
					"or one file per ARTCC and altitude. Pick the area: the ARTCCs you tick, an ROI the lines are cut off at, or every ARTCC.")),
		new SubServiceDescriptor(AirportsKey, "Airports", 20, () => new AirportsViewModel(), AirportOutputFiles.Alias, AliasAndGeojson,
			new SubServiceHelp(
				"Every open airport in the FAA's NASR data: map files of its runways, a symbol and a label, and an .apt command that shows its details in CRC.",
				Alias: "Airports.txt: an .apt command for each airport by FAA ID and by ICAO ID, Ex: .aptDTW or .aptKDTW. It shows the " +
					"name, tower, ARTCC, longest runway, elevation, pattern altitude, FSS, CTAF, weather, hours and airspace. " +
					"Every open airport is included. The area doesn't apply.",
				Geojson: "Runways_Lines, Airports_Symbols and Airports_Text (FAA ID and name). " +
					"With ROI as the area, only the airports with their reference point inside it.")),
		new SubServiceDescriptor(AirwaysKey, "Airways", 30, () => new AirwaysViewModel(), AirwayOutputFiles.Alias, AliasAndGeojson,
			new SubServiceHelp(
				"Every airway in the FAA's NASR data: map files of its line, fixes and labels, and a command that shows its fixes on the scope.",
				Alias: "Airways.txt: a command for each airway that shows its fixes, Ex: .J3F. Every airway, " +
					"or, with ROI as the area, only those that cross it.",
				Geojson: "Airways_High and Airways_Low, or a set per airway type (J, V, Q, T, …), each with Lines, Symbols and Text. " +
					"Pick the airway types, which file each goes in, and whether lines stop short of their fixes. " +
					"With ROI as the area, the lines are cut off at its edge.")),
		new SubServiceDescriptor(ArrivalsKey, "Arrivals", 40, () => new ArrivalsViewModel(), ArrivalOutputFiles.Alias, AliasAndGeojson,
			new SubServiceHelp(
				"Arrivals from the FAA's NASR data: map files for each one in a folder per airport, and a command for each airport and arrival that shows its fixes.",
				Alias: "Arrivals.txt: a command for each airport and arrival that shows its fixes, Ex: .lasBLAIDf. " +
					"The area (ARTCCs, ROI, or everything) and the amendment date narrow it, the same as the GeoJSON.",
				Geojson: "Lines, Symbols and Text for each airport and arrival, in Geojson\\<ARTCC>\\<airport>\\. " +
					"Narrow them by area (ARTCCs, ROI, or everything) and amendment date.")),
		new SubServiceDescriptor(DeparturesKey, "Departures", 50, () => new DeparturesViewModel(), DepartureOutputFiles.Alias, AliasAndGeojson,
			new SubServiceHelp(
				"SIDs, and obstacle departures (ODPs) if you want them, from the FAA's NASR data: map files for each one in a folder per airport, and a command for each airport and departure that shows its fixes.",
				Alias: "Departures.txt: a command for each airport and departure that shows its fixes, Ex: .laxDOTSSf. " +
					"The area (ARTCCs, ROI, or everything) and the amendment date narrow it, the same as the GeoJSON.",
				Geojson: "Lines, Symbols and Text for each airport and departure, in Geojson\\<ARTCC>\\<airport>\\. " +
					"Narrow them by area (ARTCCs, ROI, or everything) and amendment date.")),
		new SubServiceDescriptor(NavaidsKey, "NAVAIDs", 60, () => new NavaidsViewModel(), NavaidOutputFiles.Alias, AliasAndGeojson,
			new SubServiceHelp(
				"NAVAIDs from the FAA's NASR data: a symbol and a label on the map, and .nav commands that show each one's name, type, frequency and ARTCCs.",
				Alias: "Navaids.txt: a .nav command for each identifier and each name, Ex: .navAA or .navCEDAR. " +
					"The NAVAID types you untick are left out. The area doesn't apply.",
				Geojson: "NAVAIDs_Symbols and NAVAIDs_Text, or a pair per NAVAID type. Pick the NAVAID types, the file layout " +
					"and how symbols are drawn. With ROI as the area, only the NAVAIDs inside it.")),
		new SubServiceDescriptor(FixesKey, "Fixes", 70, () => new FixesViewModel(),
			Outputs: SubServiceOutputKinds.Geojson,
			Help: new SubServiceHelp(
				"A symbol and a label for every fix in the FAA's NASR data. GeoJSON only.",
				Geojson: "Fix_Symbols and Fix_Text, or files per fix use, per chart, or per chart and fix use. " +
					"Pick the fix uses or charts. With ROI as the area, only the fixes inside it.")),
		new SubServiceDescriptor(ProceduresKey, "Procedures", 80, () => new ProceduresViewModel(), ProcedureOutputFiles.Alias,
			SubServiceOutputKinds.Alias | SubServiceOutputKinds.ProcedureChanges | SubServiceOutputKinds.ProceduresJson,
			new SubServiceHelp(
				"The FAA's charts, from the d-TPP Metafile: what changed this cycle, a list of every chart, and commands that open a chart.",
				Alias: "Faa_Chart_Recall.txt: a command for each page of every chart in the metafile that opens it from the FAA, " +
					"Ex: .dtwI22Lc. What you pick for the documents doesn't narrow it.",
				ProcedureChanges: "Procedure_Changes.md: every chart added, changed, or deleted this cycle in the area, and at the airports and " +
					"procedures you pick, grouped by facility, with links to the FAA's comparison PDFs and charts.",
				ProceduresJson: "Procedures.json: every current chart at the airports you pick, with the fields you choose, for other tools to read.")),
		new SubServiceDescriptor(TelephonyKey, "Telephony", 90, () => new TelephonyViewModel(), TelephonyOutputFiles.Alias,
			SubServiceOutputKinds.Alias,
			new SubServiceHelp(
				"Operators' call signs from the FAA's telephony list, and your facility's virtual airlines, as .id commands. Alias file only.",
				Alias: "Telephony.txt: an .id command for each operator's 3LD and its telephony, Ex: .idAVA and .idAVIANCA, " +
					"showing who it is. Add your virtual airlines, and the virtual airline list (GNG + VATSIM-Radar) if you want it. Telephony has no area.")),
		new SubServiceDescriptor(WxStationsKey, "Wx Stations", 100, () => new WxStationsViewModel(),
			Outputs: SubServiceOutputKinds.Geojson,
			Help: new SubServiceHelp(
				"A symbol and a label for every US and US-territory station that reports METARs, from aviationweather.gov. GeoJSON only.",
				Geojson: "Wx_Symbols and Wx_Text (the ICAO ID, then the IATA ID and name). With ROI as the area, only the stations inside it.")),
		new SubServiceDescriptor(ConcatenateAliasesKey, "Concatenate Aliases", 110, () => new ConcatenateAliasesViewModel(),
			Help: new SubServiceHelp(
				"Combines every alias file the run makes, then your facility's own alias files, into Combined_Alias.txt, " +
				"since vNAS takes one alias file per facility.")),
	];
}
