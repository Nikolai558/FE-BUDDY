using FeBuddy.Core.Domain.Fixes.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

using static FeBuddy.Core.Infrastructure.Nasr.Models.FixCsvDataModel;

namespace FeBuddy.UnitTests.Application.Airac.Fixes.Fixtures;

/// <summary>
/// Builds small, real-looking <see cref="NasrCsvDataCollection"/> instances - and the already-built
/// <see cref="Fix"/> objects the writer and service tests need - for Fixes tests, so tests never
/// depend on real FAA CSV files.
/// </summary>
/// <remarks>
/// The sample rows mirror a real corner of NASR: <c>ACME</c> (a WYPNT depicted on two enroute
/// charts), <c>BRAVO</c> (a COMPUTER-NAV fix with no published chart), <c>CHRLI</c> (a RPRTNG-PNT on
/// one chart), <c>ZULUX</c> (an unrecognized <c>FIX_USE_CODE</c>, which NASR occasionally adds
/// before FE-Buddy's map is updated), and <c>DUPEX</c> published twice as two unrelated fixes.
/// </remarks>
internal static class FixTestData
{
	/// <summary>Builds a <see cref="NasrCsvDataCollection"/> holding only the given FIX_BASE rows.</summary>
	public static NasrCsvDataCollection Build(IEnumerable<FixBase>? rows = null)
	{
		FixCsvDataCollection fixCollection = new();
		fixCollection.FixBase.AddRange(rows ?? []);

		return new NasrCsvDataCollection { Fix = fixCollection };
	}

	/// <summary>Builds one FIX_BASE row. Every field <see cref="Core.Application.Airac.Fixes.FixBuilder"/> reads can be overridden.</summary>
	public static FixBase Row(
		string fixId,
		string fixUseCode = "WP",
		string? charts = null,
		double latitude = 40.0,
		double longitude = -100.0) =>
		new()
		{
			FixId = fixId,
			FixUseCode = fixUseCode,
			Charts = charts,
			LatDecimal = latitude,
			LongDecimal = longitude,
		};

	// ---- FIX_BASE rows, real-looking identifiers and coordinates ----

	/// <summary>ACME: a WYPNT depicted on two enroute charts.</summary>
	public static FixBase AcmeRow() => Row("ACME", "WP", "ENROUTE LOW,ENROUTE HIGH", 40.0, -100.0);

	/// <summary>BRAVO: a COMPUTER-NAV fix with no published chart.</summary>
	public static FixBase BravoRow() => Row("BRAVO", "CN", null, 41.0, -101.0);

	/// <summary>CHRLI: a RPRTNG-PNT on one enroute chart.</summary>
	public static FixBase CharlieRow() => Row("CHRLI", "RP", "ENROUTE LOW", 42.0, -102.0);

	/// <summary>ZULUX: an unrecognized FIX_USE_CODE - still built, its raw code kept literally as its mapped name.</summary>
	public static FixBase ZuluxRow() => Row("ZULUX", "ZQ", null, 43.0, -103.0);

	/// <summary>A blank FIX_ID: skipped, with one warning, by <see cref="Core.Application.Airac.Fixes.FixBuilder"/>.</summary>
	public static FixBase BlankFixIdRow() => Row(" ", "WP");

	/// <summary>A FIX_ID that is a literal null, not just blank - also skipped, with one warning.</summary>
	public static FixBase NullFixIdRow() => new() { FixId = null!, FixUseCode = "WP" };

	/// <summary>A literal null FIX_USE_CODE - treated the same as a blank one.</summary>
	public static FixBase NullFixUseCodeRow(string fixId) => new() { FixId = fixId, FixUseCode = null! };

	/// <summary>DUPEX published as a WYPNT.</summary>
	public static FixBase DupexOneRow() => Row("DUPEX", "WP", null, 44.0, -104.0);

	/// <summary>DUPEX published a second time, as an unrelated RADAR fix - duplicate FIX_IDs are never merged.</summary>
	public static FixBase DupexTwoRow() => Row("DUPEX", "RADAR", null, 45.0, -105.0);

	/// <summary>Every sample row above, as NASR would publish them together in one FIX_BASE.csv.</summary>
	public static IReadOnlyList<FixBase> AllSampleRows() =>
	[
		AcmeRow(), BravoRow(), CharlieRow(), ZuluxRow(), BlankFixIdRow(), DupexOneRow(), DupexTwoRow(),
	];

	// ---- already-built Fix records, for the writer, filter and service tests ----

	/// <summary>Builds a <see cref="Fix"/> directly, bypassing FIX_BASE parsing.</summary>
	public static Fix BuiltFix(
		string fixId = "ACME",
		double latitude = 40.0,
		double longitude = -100.0,
		string fixUseCode = "WP",
		string fixUse = "WYPNT",
		IReadOnlyList<string>? charts = null) =>
		new(fixId, latitude, longitude, fixUseCode, fixUse, charts ?? []);

	/// <summary>ACME, matching <see cref="AcmeRow"/>: a WYPNT on two enroute charts.</summary>
	public static Fix Acme() => BuiltFix(charts: ["ENROUTE LOW", "ENROUTE HIGH"]);

	/// <summary>BRAVO, matching <see cref="BravoRow"/>: a COMPUTER-NAV fix with no chart.</summary>
	public static Fix Bravo() => BuiltFix("BRAVO", 41.0, -101.0, "CN", "COMPUTER-NAV");

	/// <summary>CHRLI, matching <see cref="CharlieRow"/>: a RPRTNG-PNT on one enroute chart.</summary>
	public static Fix Charlie() => BuiltFix("CHRLI", 42.0, -102.0, "RP", "RPRTNG-PNT", ["ENROUTE LOW"]);

	/// <summary>A MIL-RPRTNG-PNT, with no published chart.</summary>
	public static Fix MilReportingPoint(string fixId = "MILRP") => BuiltFix(fixId, 44.0, -104.0, "MR", "MIL-RPRTNG-PNT");

	/// <summary>A MIL-WYPNT, with no published chart.</summary>
	public static Fix MilWaypoint(string fixId = "MILWP") => BuiltFix(fixId, 45.0, -105.0, "MW", "MIL-WYPNT");

	/// <summary>An NRS-WYPNT, with no published chart.</summary>
	public static Fix NrsWaypoint(string fixId = "NRSWP") => BuiltFix(fixId, 46.0, -106.0, "NRS", "NRS-WYPNT");

	/// <summary>A RADAR fix, with no published chart.</summary>
	public static Fix Radar(string fixId = "RADAR1") => BuiltFix(fixId, 47.0, -107.0, "RADAR", "RADAR");

	/// <summary>A VFR-WYPNT, with no published chart.</summary>
	public static Fix VfrWaypoint(string fixId = "VFRWP") => BuiltFix(fixId, 48.0, -108.0, "VFR", "VFR-WYPNT");

	/// <summary>A fix whose FIX_USE_CODE FE-Buddy does not recognize, mapped name kept literally.</summary>
	public static Fix Mystery(string fixId = "ZULUX") => BuiltFix(fixId, 43.0, -103.0, "ZQ", "ZQ");
}
