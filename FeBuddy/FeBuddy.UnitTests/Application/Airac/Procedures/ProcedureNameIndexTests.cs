using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Infrastructure.Dtpp.Models;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Covers <see cref="ProcedureNameIndex"/>: a cycle's d-TPP Metafile indexed in Core for the
/// Procedure Charts tab - airports by FAA or ICAO ID, departures and arrivals by name, root and
/// airport (continuation pages and other chart types left out), the counts the tab shows - built
/// once per metafile.
/// </summary>
public sealed class ProcedureNameIndexTests
{
	private static DtppMetafileDataCollection Metafile() => ProcedureTestData.Dtpp(
		"2611",
		airports: [ProcedureTestData.AirportRow("LAS", "KLAS"), ProcedureTestData.AirportRow("PIT", "KPIT")],
		records:
		[
			ProcedureTestData.RecordRow("LAS", 12, "STR", "SHEAD TWO (RNAV)", "00001SHEAD.PDF"),
			ProcedureTestData.RecordRow("LAS", 10, "STR", "BRWNZ FIVE (RNAV)", "00001BRWNZ.PDF"),
			ProcedureTestData.RecordRow("LAS", 11, "STR", "BRWNZ FIVE (RNAV), CONT.1", "00001BRWNZ_C.PDF"),
			ProcedureTestData.RecordRow("LAS", 20, "IAP", "ILS OR LOC RWY 26L", "00001IL26L.PDF"),
			ProcedureTestData.RecordRow("PIT", 30, "DP", "GRUUB ONE (RNAV)", "00002GRUUB.PDF"),
			ProcedureTestData.RecordRow("PIT", 31, "ODP", "PITT TWO (OBSTACLE)", "00002PITT.PDF"),
		]);

	[Fact]
	public void airports_are_found_by_faa_or_icao_id()
	{
		ProcedureNameIndex index = ProcedureNameIndex.Build(Metafile());

		Assert.Equal("LAS", index.AirportsByIdent["klas"].AptIdent);
		Assert.Equal("PIT", index.AirportsByIdent["PIT"].AptIdent);
		Assert.Equal(2, index.AirportCount);
	}

	/// <summary>The cards list departures and arrivals only, in chart order at an airport, a continuation page not on its own.</summary>
	[Fact]
	public void departures_and_arrivals_are_listed_by_airport_name_and_root()
	{
		ProcedureNameIndex index = ProcedureNameIndex.Build(Metafile());

		Assert.Equal(["BRWNZ FIVE (RNAV)", "SHEAD TWO (RNAV)"], index.NamesByAirport["LAS"]);
		Assert.Equal(["GRUUB ONE (RNAV)", "PITT TWO (OBSTACLE)"], index.NamesByAirport["pit"]);
		Assert.Equal(["BRWNZ FIVE (RNAV)", "GRUUB ONE (RNAV)", "PITT TWO (OBSTACLE)", "SHEAD TWO (RNAV)"], index.NameOptions);
		Assert.Equal("GRUUB ONE (RNAV)", index.NamesByCanonical["gruub one (rnav)"]);
		Assert.Equal(["BRWNZ FIVE (RNAV)"], index.NamesByRoot["BRWNZ"]);
		Assert.Equal(5, index.ProcedureCount);
	}

	[Fact]
	public void an_index_is_built_once_per_metafile_and_the_empty_one_finds_nothing()
	{
		DtppMetafileDataCollection metafile = Metafile();

		Assert.Same(ProcedureNameIndex.For(metafile), ProcedureNameIndex.For(metafile));
		Assert.Empty(ProcedureNameIndex.Empty.AirportsByIdent);
		Assert.Empty(ProcedureNameIndex.Empty.NameOptions);
		Assert.Equal(0, ProcedureNameIndex.Empty.ProcedureCount);
		Assert.Throws<ArgumentNullException>(() => ProcedureNameIndex.For(null!));
		Assert.Throws<ArgumentNullException>(() => ProcedureNameIndex.Build(null!));
	}
}
