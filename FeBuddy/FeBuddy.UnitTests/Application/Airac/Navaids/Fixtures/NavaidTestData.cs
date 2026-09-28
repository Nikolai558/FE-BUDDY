using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Domain.Navaids.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

using static FeBuddy.Core.Infrastructure.Nasr.Models.NavCsvDataModel;

namespace FeBuddy.UnitTests.Application.Airac.Navaids.Fixtures;

/// <summary>
/// Builds small, real-looking <see cref="NasrCsvDataCollection"/> instances - and the already-built
/// <see cref="Navaid"/> objects the writer tests need - for NAVAIDs tests, so tests never depend on
/// real FAA CSV files.
/// </summary>
/// <remarks>
/// The sample rows mirror a real corner of NASR: <c>CGT</c> (a plain VORTAC), <c>ABQ</c> published
/// twice (a VORTAC and, separately, a VOT - about 71 identifiers repeat like this in real NASR
/// data), <c>AA</c> published twice as two unrelated NDBs, and <c>ELY</c>/<c>ELO</c> - two
/// different identifiers that share the name <c>ELY</c>, the case <see cref="NavaidAliasWriter"/>'s
/// name-command merging exists for.
/// </remarks>
internal static class NavaidTestData
{
	/// <summary>Builds a <see cref="NasrCsvDataCollection"/> holding only the given NAV_BASE rows.</summary>
	public static NasrCsvDataCollection Build(IEnumerable<NavBase>? rows = null)
	{
		NavCsvDataCollection navCollection = new();
		navCollection.NavBase.AddRange(rows ?? []);

		return new NasrCsvDataCollection { Nav = navCollection };
	}

	/// <summary>Builds one NAV_BASE row. Every field <see cref="NavaidBuilder"/> reads can be overridden.</summary>
	public static NavBase Row(
		string navId,
		string navType,
		string name = "TEST NAVAID",
		double? freq = null,
		double latitude = 40.0,
		double longitude = -100.0,
		string? lowAltArtccId = null,
		string? highAltArtccId = null,
		string navStatus = "OPERATIONAL") =>
		new()
		{
			NavId = navId,
			NavType = navType,
			Name = name,
			Freq = freq,
			LatDecimal = latitude,
			LongDecimal = longitude,
			LowAltArtccId = lowAltArtccId,
			HighAltArtccId = highAltArtccId,
			NavStatus = navStatus,
		};

	// ---- NAV_BASE rows, real-looking identifiers and coordinates ----

	/// <summary>CGT: a plain VORTAC at Chicago Heights, 114.20, ZAU high and low.</summary>
	public static NavBase CgtRow() => Row("CGT", "VORTAC", "CHICAGO HEIGHTS", 114.2, 41.510007, -87.571546, "ZAU", "ZAU");

	/// <summary>ABQ published as a VORTAC, 113.20, ZAB high and low.</summary>
	public static NavBase AbqVortacRow() => Row("ABQ", "VORTAC", "ALBUQUERQUE", 113.2, 35.040154, -106.609861, "ZAB", "ZAB");

	/// <summary>ABQ published a second time as a VOT, 111.00, with no published ARTCC boundaries.</summary>
	public static NavBase AbqVotRow() => Row("ABQ", "VOT", "ALBUQUERQUE", 111.0, 35.040289, -106.609917);

	/// <summary>AA published as the CEDAR NDB, 341 kHz, ZTL.</summary>
	public static NavBase AaCedarRow() => Row("AA", "NDB", "CEDAR", 341, 33.945892, -84.688919, "ZTL", "ZTL");

	/// <summary>AA published a second time as the KENIE NDB, 365 kHz, ZMP.</summary>
	public static NavBase AaKenieRow() => Row("AA", "NDB", "KENIE", 365, 44.880417, -93.216919, "ZMP", "ZMP");

	/// <summary>ELY, a VOR/DME named ELY, 113.95, ZLC.</summary>
	public static NavBase ElyRow() => Row("ELY", "VOR/DME", "ELY", 113.95, 39.276222, -114.844206, "ZLC", "ZLC");

	/// <summary>ELO, a DME also named ELY, 113.45, ZMP - a different identifier sharing ELY's name.</summary>
	public static NavBase EloRow() => Row("ELO", "DME", "ELY", 113.45, 44.281111, -93.973333, "ZMP", "ZMP");

	/// <summary>A FAN MARKER: no CRC symbol style is mapped for it, and it publishes no frequency.</summary>
	public static NavBase FanMarkerRow(string navId = "OM") => Row(navId, "FAN MARKER", "OUTER MARKER");

	/// <summary>A CONSOLAN: an NDB-family type (kHz) with no CRC symbol style mapped.</summary>
	public static NavBase ConsolanRow(string navId = "NY") => Row(navId, "CONSOLAN", "NANTUCKET", 194, 41.0, -70.0);

	/// <summary>A NAV_STATUS of SHUTDOWN: skipped entirely by <see cref="NavaidBuilder"/>.</summary>
	public static NavBase ShutdownRow() => Row("ZZZ", "VOR", "RETIRED VOR", 112.0, 30.0, -90.0, navStatus: "SHUTDOWN");

	/// <summary>A blank NAV_ID: skipped, with one warning, by <see cref="NavaidBuilder"/>.</summary>
	public static NavBase BlankNavIdRow() => Row(" ", "VOR", "NO IDENTIFIER");

	/// <summary>Every sample row above, as NASR would publish them together in one NAV_BASE.csv.</summary>
	public static IReadOnlyList<NavBase> AllSampleRows() =>
	[
		CgtRow(), AbqVortacRow(), AbqVotRow(), AaCedarRow(), AaKenieRow(), ElyRow(), EloRow(),
		FanMarkerRow(), ConsolanRow(), ShutdownRow(), BlankNavIdRow(),
	];

	// ---- already-built Navaid records, for the writer and filter tests ----

	/// <summary>Builds a <see cref="Navaid"/> directly, bypassing NAV_BASE parsing.</summary>
	public static Navaid BuiltNavaid(
		string navId = "CGT",
		string navType = "VORTAC",
		string name = "CHICAGO HEIGHTS",
		double? freq = 114.2,
		double latitude = 41.510007,
		double longitude = -87.571546,
		string lowAltArtccId = "ZAU",
		string highAltArtccId = "ZAU") =>
		new(navId, navType, name, freq, latitude, longitude, lowAltArtccId, highAltArtccId);

	/// <summary>CGT, matching <see cref="CgtRow"/>.</summary>
	public static Navaid Cgt() => BuiltNavaid();

	/// <summary>ABQ as a VORTAC, matching <see cref="AbqVortacRow"/>.</summary>
	public static Navaid AbqVortac() => BuiltNavaid("ABQ", "VORTAC", "ALBUQUERQUE", 113.2, 35.040154, -106.609861, "ZAB", "ZAB");

	/// <summary>ABQ as a VOT, matching <see cref="AbqVotRow"/> - blank ARTCC boundaries.</summary>
	public static Navaid AbqVot() => BuiltNavaid("ABQ", "VOT", "ALBUQUERQUE", 111.0, 35.040289, -106.609917, "", "");

	/// <summary>AA as the CEDAR NDB, matching <see cref="AaCedarRow"/>.</summary>
	public static Navaid AaCedar() => BuiltNavaid("AA", "NDB", "CEDAR", 341, 33.945892, -84.688919, "ZTL", "ZTL");

	/// <summary>AA as the KENIE NDB, matching <see cref="AaKenieRow"/>.</summary>
	public static Navaid AaKenie() => BuiltNavaid("AA", "NDB", "KENIE", 365, 44.880417, -93.216919, "ZMP", "ZMP");

	/// <summary>ELY, matching <see cref="ElyRow"/>.</summary>
	public static Navaid Ely() => BuiltNavaid("ELY", "VOR/DME", "ELY", 113.95, 39.276222, -114.844206, "ZLC", "ZLC");

	/// <summary>ELO, matching <see cref="EloRow"/> - a different identifier named ELY.</summary>
	public static Navaid Elo() => BuiltNavaid("ELO", "DME", "ELY", 113.45, 44.281111, -93.973333, "ZMP", "ZMP");

	/// <summary>A FAN MARKER: no mapped CRC symbol style, no published frequency.</summary>
	public static Navaid FanMarker(string navId = "OM") => BuiltNavaid(navId, "FAN MARKER", "OUTER MARKER", null, 40.0, -100.0, "", "");

	/// <summary>A CONSOLAN: no mapped CRC symbol style.</summary>
	public static Navaid Consolan(string navId = "NY") => BuiltNavaid(navId, "CONSOLAN", "NANTUCKET", 194, 41.0, -70.0, "", "");
}
