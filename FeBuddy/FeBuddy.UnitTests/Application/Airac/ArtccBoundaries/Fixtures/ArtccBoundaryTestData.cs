using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

using static FeBuddy.Core.Infrastructure.Nasr.Models.ArbCsvDataModel;

namespace FeBuddy.UnitTests.Application.Airac.ArtccBoundaries.Fixtures;

/// <summary>
/// Builds small, real-looking <see cref="NasrCsvDataCollection"/> instances - and the ARB_BASE /
/// ARB_SEG rows behind them - for ARTCC Boundaries tests, so tests never depend on real FAA CSV
/// files.
/// </summary>
/// <remarks>
/// The sample rows mirror a real corner of NASR, always in file order with <c>POINT_SEQ</c> 10,
/// 20, ...: <c>ZOB</c> (CLEVELAND, a plain domestic ARTCC) with one HIGH ring and one LOW ring;
/// <c>ZAK</c> (OAKLAND OCEANIC), whose UNLIMITED group is two rings - a CTA ring, then a FIR ring
/// whose <c>POINT_SEQ</c> restarts at 10 - both crossing the antimeridian; and <c>ZZZ</c>, a
/// LocationId with <c>ARB_SEG</c> rows but no <c>ARB_BASE</c> row.
/// </remarks>
internal static class ArtccBoundaryTestData
{
	/// <summary>Builds a <see cref="NasrCsvDataCollection"/> holding only the given ARB_BASE/ARB_SEG rows.</summary>
	public static NasrCsvDataCollection Build(IEnumerable<ArbBase>? baseRows = null, IEnumerable<ArbSeg>? segRows = null)
	{
		ArbCsvDataCollection arbCollection = new();
		arbCollection.ArbBase.AddRange(baseRows ?? []);
		arbCollection.ArbSeg.AddRange(segRows ?? []);

		return new NasrCsvDataCollection { Arb = arbCollection };
	}

	/// <summary>Builds one ARB_BASE row. Every field <see cref="Core.Application.Airac.ArtccBoundaries.ArtccBoundaryBuilder"/> reads can be overridden.</summary>
	public static ArbBase BaseRow(
		string locationId,
		string locationName,
		string locationType = "ARTCC",
		string computerId = "",
		string icaoId = "",
		string city = "",
		string countryCode = "US") =>
		new()
		{
			LocationId = locationId,
			LocationName = locationName,
			ComputerId = computerId,
			IcaoId = icaoId,
			LocationType = locationType,
			City = city,
			CountryCode = countryCode,
		};

	/// <summary>Builds one ARB_SEG row. Every field the builder reads can be overridden.</summary>
	public static ArbSeg SegRow(
		string locationId,
		string altitude,
		string type,
		int pointSeq,
		double latitude,
		double longitude,
		string? locationName = null) =>
		new()
		{
			LocationId = locationId,
			LocationName = locationName,
			Altitude = altitude,
			Type = type,
			PointSeq = pointSeq,
			SegLatDecimal = latitude,
			SegLongDecimal = longitude,
		};

	// ---- ZOB: CLEVELAND, a plain domestic ARTCC with a HIGH ring and a LOW ring ----

	/// <summary>ZOB: CLEVELAND, an ARTCC, computer ID ZOB, ICAO KZOB, city CLEVELAND, US.</summary>
	public static ArbBase ZobBaseRow() =>
		BaseRow("ZOB", "CLEVELAND", computerId: "ZOB", icaoId: "KZOB", city: "CLEVELAND", countryCode: "US");

	/// <summary>ZOB's HIGH ring: a triangle NASR already closes itself (its first point is repeated as the last).</summary>
	public static IReadOnlyList<ArbSeg> ZobHighRows() =>
	[
		SegRow("ZOB", "HIGH", "ARTCC", 10, 40.0, -81.0),
		SegRow("ZOB", "HIGH", "ARTCC", 20, 41.0, -82.0),
		SegRow("ZOB", "HIGH", "ARTCC", 30, 40.0, -83.0),
		SegRow("ZOB", "HIGH", "ARTCC", 40, 40.0, -81.0),
	];

	/// <summary>ZOB's LOW ring: a triangle NASR does NOT close itself - the builder must close it.</summary>
	public static IReadOnlyList<ArbSeg> ZobLowRows() =>
	[
		SegRow("ZOB", "LOW", "ARTCC", 10, 40.2, -81.2),
		SegRow("ZOB", "LOW", "ARTCC", 20, 41.2, -82.2),
		SegRow("ZOB", "LOW", "ARTCC", 30, 40.2, -83.2),
	];

	// ---- ZAK: OAKLAND OCEANIC, an UNLIMITED group of two rings crossing the antimeridian ----

	/// <summary>ZAK: OAKLAND OCEANIC, an ARTCC, computer ID ZAK, ICAO PAZA, city OAKLAND, US.</summary>
	public static ArbBase ZakBaseRow() =>
		BaseRow("ZAK", "OAKLAND OCEANIC", computerId: "ZAK", icaoId: "PAZA", city: "OAKLAND", countryCode: "US");

	/// <summary>ZAK's UNLIMITED CTA ring, crossing the antimeridian (170, 179, -179, -170).</summary>
	public static IReadOnlyList<ArbSeg> ZakCtaRows() =>
	[
		SegRow("ZAK", "UNLIMITED", "CTA", 10, 30.0, 170.0),
		SegRow("ZAK", "UNLIMITED", "CTA", 20, 31.0, 179.0),
		SegRow("ZAK", "UNLIMITED", "CTA", 30, 32.0, -179.0),
		SegRow("ZAK", "UNLIMITED", "CTA", 40, 30.0, -170.0),
	];

	/// <summary>
	/// ZAK's UNLIMITED FIR ring: a second ring in the same (LocationId, Altitude) group, told
	/// apart from the CTA ring by its <c>POINT_SEQ</c> restarting at 10. Also crosses the
	/// antimeridian.
	/// </summary>
	public static IReadOnlyList<ArbSeg> ZakFirRows() =>
	[
		SegRow("ZAK", "UNLIMITED", "FIR", 10, 35.0, 170.0),
		SegRow("ZAK", "UNLIMITED", "FIR", 20, 36.0, 179.0),
		SegRow("ZAK", "UNLIMITED", "FIR", 30, 37.0, -179.0),
		SegRow("ZAK", "UNLIMITED", "FIR", 40, 35.0, -170.0),
	];

	// ---- ZZZ: a segment-only LocationId, no ARB_BASE row ----

	/// <summary>ZZZ's HIGH ring: ARB_SEG rows for a LocationId with no matching ARB_BASE row.</summary>
	public static IReadOnlyList<ArbSeg> ZzzRows() =>
	[
		SegRow("ZZZ", "HIGH", "ARTCC", 10, 10.0, -10.0, locationName: "MYSTERY CENTER"),
		SegRow("ZZZ", "HIGH", "ARTCC", 20, 11.0, -11.0, locationName: "MYSTERY CENTER"),
		SegRow("ZZZ", "HIGH", "ARTCC", 30, 10.0, -12.0, locationName: "MYSTERY CENTER"),
	];

	/// <summary>Every sample ARB_BASE row above (ZOB, then ZAK), as NASR would publish ARB_BASE.csv.</summary>
	public static IReadOnlyList<ArbBase> AllBaseRows() => [ZobBaseRow(), ZakBaseRow()];

	/// <summary>Every sample ARB_SEG row above, in file order: ZOB, then ZAK, then ZZZ.</summary>
	public static IReadOnlyList<ArbSeg> AllSegRows() =>
	[
		.. ZobHighRows(), .. ZobLowRows(),
		.. ZakCtaRows(), .. ZakFirRows(),
		.. ZzzRows(),
	];

	/// <summary>Every sample row above, as NASR would publish a whole ARB cycle.</summary>
	public static NasrCsvDataCollection BuildAllSamples() => Build(AllBaseRows(), AllSegRows());
}
