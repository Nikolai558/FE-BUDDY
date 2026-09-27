using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Domain.Procedures.Models;
using FeBuddy.Core.Infrastructure.Dtpp;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Covers <see cref="ProcedureBuilder.Build"/>: the missing-data guards, the NASR join (ARTCC,
/// ICAO fallback, coordinates, airspace class), continuation pages folding into their base
/// procedure, classification against <c>useraction</c>, the previous-cycle chart lookup, the
/// <c>DEL_APT_SERVED.PDF</c> flag, amendment date parsing, and field normalization.
/// </summary>
public sealed class ProcedureBuilderTests
{
	// ---- guards ----

	[Fact]
	public void build_rejects_null_arguments()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609");

		Assert.Throws<ArgumentNullException>(() => ProcedureBuilder.Build(null!, dtpp, null));
		Assert.Throws<ArgumentNullException>(() => ProcedureBuilder.Build(nasr, null!, null));
	}

	[Fact]
	public void build_throws_when_apt_was_never_parsed()
	{
		NasrCsvDataCollection nasr = new() { ClsArsp = new ClsArspCsvDataCollection() };

		Assert.Throws<InvalidOperationException>(() => ProcedureBuilder.Build(nasr, ProcedureTestData.Dtpp("2609"), null));
	}

	[Fact]
	public void build_throws_when_cls_arsp_was_never_parsed()
	{
		NasrCsvDataCollection nasr = new() { Apt = new AptCsvDataCollection() };

		Assert.Throws<InvalidOperationException>(() => ProcedureBuilder.Build(nasr, ProcedureTestData.Dtpp("2609"), null));
	}

	// ---- NASR join ----

	[Fact]
	public void responsible_artcc_comes_from_nasr_apt_base()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr([ProcedureTestData.AptBaseRow("AAA", respArtccId: "ZOB")]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609", airports: [ProcedureTestData.AirportRow("AAA")]);

		ProcedureAirport airport = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports);

		Assert.Equal("ZOB", airport.ResponsibleArtcc);
	}

	[Fact]
	public void an_airport_missing_from_nasr_gets_a_null_artcc_and_no_coordinates()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr(); // no APT_BASE rows at all
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609", airports: [ProcedureTestData.AirportRow("AAA")]);

		ProcedureAirport airport = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports);

		Assert.Null(airport.ResponsibleArtcc);
		Assert.Null(airport.Latitude);
		Assert.Null(airport.Longitude);
	}

	[Fact]
	public void icao_ident_prefers_the_metafiles_own_value_over_nasr()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr([ProcedureTestData.AptBaseRow("AAA", icaoId: "KZZZ")]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609", airports: [ProcedureTestData.AirportRow("AAA", icaoIdent: "KAAA")]);

		ProcedureAirport airport = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports);

		Assert.Equal("KAAA", airport.IcaoIdent);
	}

	[Fact]
	public void icao_ident_falls_back_to_nasr_when_the_metafiles_is_blank()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr([ProcedureTestData.AptBaseRow("AAA", icaoId: "KAAA")]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609", airports: [ProcedureTestData.AirportRow("AAA", icaoIdent: null)]);

		ProcedureAirport airport = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports);

		Assert.Equal("KAAA", airport.IcaoIdent);
	}

	[Fact]
	public void icao_ident_is_null_when_neither_the_metafile_nor_nasr_has_one()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr([ProcedureTestData.AptBaseRow("AAA", icaoId: null)]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609", airports: [ProcedureTestData.AirportRow("AAA", icaoIdent: null)]);

		ProcedureAirport airport = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports);

		Assert.Null(airport.IcaoIdent);
	}

	[Fact]
	public void coordinates_come_from_nasr_apt_base()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr([ProcedureTestData.AptBaseRow("AAA", latitude: 41.5, longitude: -81.5)]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609", airports: [ProcedureTestData.AirportRow("AAA")]);

		ProcedureAirport airport = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports);

		Assert.Equal(41.5, airport.Latitude);
		Assert.Equal(-81.5, airport.Longitude);
	}

	[Fact]
	public void a_duplicate_nasr_arpt_id_keeps_the_first_row()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr(
		[
			ProcedureTestData.AptBaseRow("AAA", respArtccId: "ZOB"),
			ProcedureTestData.AptBaseRow("AAA", respArtccId: "ZNY"),
		]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609", airports: [ProcedureTestData.AirportRow("AAA")]);

		ProcedureAirport airport = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports);

		Assert.Equal("ZOB", airport.ResponsibleArtcc);
	}

	[Fact]
	public void an_apt_base_row_with_a_null_arpt_id_is_ignored_for_the_nasr_join()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr(
		[
			new AptCsvDataModel.AptBase { ArptId = null!, RespArtccId = "ZZZ" },
			ProcedureTestData.AptBaseRow("AAA", respArtccId: "ZOB"),
		]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609", airports: [ProcedureTestData.AirportRow("AAA")]);

		ProcedureAirport airport = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports);

		Assert.Equal("ZOB", airport.ResponsibleArtcc);
	}

	[Fact]
	public void a_cls_arsp_row_with_a_null_arpt_id_is_ignored_for_the_airspace_lookup()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr(
			clsArsp: [new ClsArspCsvDataModel.ClsArsp { ArptId = null!, ClassBAirspace = "Y" }]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609", airports: [ProcedureTestData.AirportRow("AAA")]);

		ProcedureAirport airport = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports);

		Assert.Equal(ProcedureAirspaceClass.None, airport.AirspaceClass);
	}

	[Fact]
	public void airports_are_returned_in_metafile_file_order()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609", airports:
		[
			ProcedureTestData.AirportRow("CCC"),
			ProcedureTestData.AirportRow("AAA"),
			ProcedureTestData.AirportRow("BBB"),
		]);

		IReadOnlyList<ProcedureAirport> airports = ProcedureBuilder.Build(nasr, dtpp, null).Airports;

		Assert.Equal(["CCC", "AAA", "BBB"], airports.Select(a => a.AptIdent));
	}

	// ---- airspace class ----

	[Theory]
	[InlineData(null, null, null, null, ProcedureAirspaceClass.None)]
	[InlineData(null, null, null, "Y", ProcedureAirspaceClass.E)]
	[InlineData(null, null, "Y", "Y", ProcedureAirspaceClass.D)]
	[InlineData(null, "Y", "Y", "Y", ProcedureAirspaceClass.C)]
	[InlineData("Y", "Y", "Y", "Y", ProcedureAirspaceClass.B)]
	[InlineData("Y", null, null, null, ProcedureAirspaceClass.B)]
	public void airspace_class_is_the_highest_flagged_class(
		string? classB, string? classC, string? classD, string? classE, ProcedureAirspaceClass expected)
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr(
			clsArsp: [ProcedureTestData.ClassAirspaceRow("AAA", classB, classC, classD, classE)]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609", airports: [ProcedureTestData.AirportRow("AAA")]);

		ProcedureAirport airport = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports);

		Assert.Equal(expected, airport.AirspaceClass);
	}

	[Fact]
	public void no_cls_arsp_rows_produce_no_airspace_class()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609", airports: [ProcedureTestData.AirportRow("AAA")]);

		ProcedureAirport airport = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports);

		Assert.Equal(ProcedureAirspaceClass.None, airport.AirspaceClass);
	}

	[Fact]
	public void a_non_y_flag_does_not_count_towards_the_airspace_class()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr(
			clsArsp: [ProcedureTestData.ClassAirspaceRow("AAA", classB: "N")]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609", airports: [ProcedureTestData.AirportRow("AAA")]);

		ProcedureAirport airport = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports);

		Assert.Equal(ProcedureAirspaceClass.None, airport.AirspaceClass);
	}

	[Fact]
	public void the_highest_class_is_taken_across_multiple_cls_arsp_rows_for_the_same_airport()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr(clsArsp:
		[
			ProcedureTestData.ClassAirspaceRow("AAA", classD: "Y"),
			ProcedureTestData.ClassAirspaceRow("AAA", classC: "Y"),
		]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609", airports: [ProcedureTestData.AirportRow("AAA")]);

		ProcedureAirport airport = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports);

		Assert.Equal(ProcedureAirspaceClass.C, airport.AirspaceClass);
	}

	// ---- continuation folding ----

	[Fact]
	public void continuation_pages_fold_into_the_base_procedures_pages()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				ProcedureTestData.RecordRow("AAA", 30000, "STR", "GRUUB ONE (RNAV)", "GRUUB1.PDF"),
				ProcedureTestData.RecordRow("AAA", 30001, "STR", "GRUUB ONE (RNAV), CONT.1", "GRUUB1B.PDF"),
				ProcedureTestData.RecordRow("AAA", 30002, "STR", "GRUUB ONE (RNAV), CONT.2", "GRUUB1C.PDF"),
			]);

		ProcedureAirport airport = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports);
		Procedure procedure = Assert.Single(airport.Procedures);

		Assert.Equal("GRUUB ONE (RNAV)", procedure.Name);
		Assert.Equal(3, procedure.Pages.Count);
		Assert.Equal(["GRUUB ONE (RNAV)", "GRUUB ONE (RNAV), CONT.1", "GRUUB ONE (RNAV), CONT.2"], procedure.Pages.Select(p => p.ChartName));
	}

	[Theory]
	[InlineData("GRUUB ONE (RNAV), CONT.1")]
	[InlineData("GRUUB ONE (RNAV),CONT.1")]
	[InlineData("GRUUB ONE (RNAV) , CONT.1")]
	[InlineData("GRUUB ONE (RNAV), cont.1")]
	public void continuation_pages_fold_regardless_of_spacing_or_case_variants(string continuationChartName)
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				ProcedureTestData.RecordRow("AAA", 30000, "STR", "GRUUB ONE (RNAV)", "GRUUB1.PDF"),
				ProcedureTestData.RecordRow("AAA", 30001, "STR", continuationChartName, "GRUUB1B.PDF"),
			]);

		ProcedureAirport airport = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports);
		Procedure procedure = Assert.Single(airport.Procedures);

		Assert.Equal(2, procedure.Pages.Count);
	}

	[Fact]
	public void procedures_are_returned_in_first_seen_chartseq_order()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				ProcedureTestData.RecordRow("AAA", 50750, "IAP", "ILS RWY 1", "ILS1.PDF"),
				ProcedureTestData.RecordRow("AAA", 90100, "DP", "TEST ONE", "DP1.PDF"),
			]);

		IReadOnlyList<Procedure> procedures = ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures;

		Assert.Equal(["ILS RWY 1", "TEST ONE"], procedures.Select(p => p.Name));
	}

	[Fact]
	public void a_continuation_only_group_with_no_main_page_is_classified_none_using_its_first_page()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 30001, "STR", "GRUUB ONE (RNAV), CONT.1", "GRUUB1B.PDF")]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Equal(ProcedureChange.None, procedure.Change);
		Assert.Equal("GRUUB ONE (RNAV), CONT.1", procedure.ReportedName);
		Assert.Equal("GRUUB1B.PDF", procedure.CurrentPdfName);
	}

	[Fact]
	public void a_deleted_continuation_with_an_unchanged_main_page_reports_the_continuation_as_deleted()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				ProcedureTestData.RecordRow("AAA", 30000, "STR", "GRUUB ONE (RNAV)", "GRUUB1.PDF"),
				ProcedureTestData.RecordRow("AAA", 30001, "STR", "GRUUB ONE (RNAV), CONT.1", DtppFiles.DeletedChartPdfName, userAction: "D"),
			]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Equal(ProcedureChange.Deleted, procedure.Change);
		Assert.Equal("GRUUB ONE (RNAV), CONT.1", procedure.ReportedName);
		Assert.Equal("GRUUB1.PDF", procedure.CurrentPdfName); // the main page persists
	}

	// ---- classification ----

	[Fact]
	public void an_added_main_page_is_classified_new()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", "ILS1.PDF", userAction: "A")]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Equal(ProcedureChange.New, procedure.Change);
		Assert.Equal("ILS RWY 1", procedure.ReportedName);
		Assert.Equal("ILS1.PDF", procedure.ReportedPdfName);
		Assert.Equal("ILS1.PDF", procedure.CurrentPdfName);
	}

	[Fact]
	public void a_changed_main_page_is_classified_changed()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", "ILS1.PDF", userAction: "C")]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Equal(ProcedureChange.Changed, procedure.Change);
		Assert.Equal("ILS1.PDF", procedure.CurrentPdfName);
	}

	[Fact]
	public void a_deleted_main_page_is_classified_deleted_with_no_current_pdf()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", DtppFiles.DeletedChartPdfName, userAction: "D")]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Equal(ProcedureChange.Deleted, procedure.Change);
		Assert.Null(procedure.CurrentPdfName);
	}

	[Fact]
	public void no_useraction_anywhere_is_classified_none()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", "ILS1.PDF")]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Equal(ProcedureChange.None, procedure.Change);
		Assert.Equal("ILS1.PDF", procedure.CurrentPdfName);
	}

	[Fact]
	public void an_unrecognized_user_action_is_classified_none()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", "ILS1.PDF", userAction: "X")]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Equal(ProcedureChange.None, procedure.Change);
		Assert.Equal("ILS1.PDF", procedure.CurrentPdfName);
	}

	[Fact]
	public void a_deleted_then_readded_main_page_is_classified_readded_using_the_added_pages_pdf()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", DtppFiles.DeletedChartPdfName, userAction: "D"),
				ProcedureTestData.RecordRow("AAA", 2, "IAP", "ILS RWY 1", "NEWILS1.PDF", userAction: "A"),
			]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Equal(ProcedureChange.ReAdded, procedure.Change);
		Assert.Equal("NEWILS1.PDF", procedure.CurrentPdfName);
		Assert.Equal("NEWILS1.PDF", procedure.ReportedPdfName);
	}

	[Fact]
	public void a_readded_records_removed_from_airport_flag_is_taken_from_its_deleted_page()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", DtppFiles.DeletedFromAirportPdfName, userAction: "D"),
				ProcedureTestData.RecordRow("AAA", 2, "IAP", "ILS RWY 1", "NEWILS1.PDF", userAction: "A"),
			]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.True(procedure.IsRemovedFromAirportOnly);
	}

	[Fact]
	public void a_changed_continuation_with_an_unchanged_main_page_reports_the_continuation()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				ProcedureTestData.RecordRow("AAA", 30000, "STR", "GRUUB ONE (RNAV)", "GRUUB1.PDF"),
				ProcedureTestData.RecordRow("AAA", 30001, "STR", "GRUUB ONE (RNAV), CONT.1", "GRUUB1B.PDF", userAction: "C"),
			]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Equal(ProcedureChange.Changed, procedure.Change);
		Assert.Equal("GRUUB ONE (RNAV), CONT.1", procedure.ReportedName);
		Assert.Equal("GRUUB1B.PDF", procedure.ReportedPdfName);
		Assert.Equal("GRUUB1.PDF", procedure.CurrentPdfName); // the main page persists
	}

	// ---- previous chart lookup ----

	[Fact]
	public void a_deleted_procedures_previous_pdf_is_found_by_matching_base_name_at_the_same_airport()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", DtppFiles.DeletedChartPdfName, userAction: "D")]);
		DtppMetafileDataCollection previousDtpp = ProcedureTestData.Dtpp("2608",
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", "OLDILS1.PDF")]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, previousDtpp).Airports[0].Procedures);

		Assert.Equal("OLDILS1.PDF", procedure.PreviousPdfName);
	}

	[Fact]
	public void a_previous_chart_lookup_falls_back_to_proc_uid_when_the_name_changed()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1 RENAMED", DtppFiles.DeletedChartPdfName, userAction: "D", procUid: 99)]);
		DtppMetafileDataCollection previousDtpp = ProcedureTestData.Dtpp("2608",
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", "OLDILS1.PDF", procUid: 99)]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, previousDtpp).Airports[0].Procedures);

		Assert.Equal("OLDILS1.PDF", procedure.PreviousPdfName);
	}

	[Fact]
	public void a_previous_chart_lookup_is_null_when_nothing_matches()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1 RENAMED", DtppFiles.DeletedChartPdfName, userAction: "D", procUid: 99)]);
		DtppMetafileDataCollection previousDtpp = ProcedureTestData.Dtpp("2608",
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "SOMETHING ELSE", "OTHER.PDF", procUid: 5)]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, previousDtpp).Airports[0].Procedures);

		Assert.Null(procedure.PreviousPdfName);
	}

	[Fact]
	public void a_previous_chart_lookup_is_null_when_procuid_is_absent_and_the_name_does_not_match()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", DtppFiles.DeletedChartPdfName, userAction: "D", procUid: null)]);
		DtppMetafileDataCollection previousDtpp = ProcedureTestData.Dtpp("2608",
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "SOMETHING ELSE", "OTHER.PDF", procUid: 5)]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, previousDtpp).Airports[0].Procedures);

		Assert.Null(procedure.PreviousPdfName);
	}

	[Fact]
	public void a_continuation_page_in_the_previous_cycle_is_not_used_for_the_name_based_lookup()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", DtppFiles.DeletedChartPdfName, userAction: "D")]);

		// The previous cycle only ever published a continuation page under this base name - never a
		// main page - so the name-based lookup must not treat it as a match.
		DtppMetafileDataCollection previousDtpp = ProcedureTestData.Dtpp("2608",
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1, CONT.1", "OLDCONT1.PDF")]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, previousDtpp).Airports[0].Procedures);

		Assert.Null(procedure.PreviousPdfName);
	}

	[Fact]
	public void a_previous_chart_lookup_is_null_when_there_is_no_previous_metafile()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", DtppFiles.DeletedChartPdfName, userAction: "D")]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Null(procedure.PreviousPdfName);
	}

	[Fact]
	public void a_readded_procedures_previous_pdf_comes_from_the_previous_cycle_not_the_deleted_record_in_this_cycle()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				// This cycle's "D" record's own PDF is a placeholder, never the answer.
				ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", DtppFiles.DeletedChartPdfName, userAction: "D"),
				ProcedureTestData.RecordRow("AAA", 2, "IAP", "ILS RWY 1", "NEWILS1.PDF", userAction: "A"),
			]);
		DtppMetafileDataCollection previousDtpp = ProcedureTestData.Dtpp("2608",
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", "OLDILS1.PDF")]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, previousDtpp).Airports[0].Procedures);

		Assert.Equal("OLDILS1.PDF", procedure.PreviousPdfName);
	}

	// ---- DEL_APT_SERVED ----

	[Fact]
	public void a_deleted_record_whose_pdf_is_del_apt_served_flags_removed_from_airport_only()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", DtppFiles.DeletedFromAirportPdfName, userAction: "D")]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.True(procedure.IsRemovedFromAirportOnly);
	}

	[Fact]
	public void a_deleted_record_with_the_ordinary_placeholder_does_not_flag_removed_from_airport_only()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", DtppFiles.DeletedChartPdfName, userAction: "D")]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.False(procedure.IsRemovedFromAirportOnly);
	}

	// ---- amendment date and other field normalization ----

	[Theory]
	[InlineData("12/05/2019", 2019, 12, 5)]
	[InlineData("01/01/2026", 2026, 1, 1)]
	public void a_valid_amendment_date_parses(string raw, int year, int month, int day)
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", "ILS1.PDF", amdtDate: raw)]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Equal(new DateOnly(year, month, day), procedure.AmdtDate);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("not-a-date")]
	[InlineData("2019/12/05")]
	public void an_invalid_or_missing_amendment_date_reads_as_null(string? raw)
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", "ILS1.PDF", amdtDate: raw)]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Null(procedure.AmdtDate);
	}

	[Fact]
	public void blank_civil_amendment_number_and_computer_code_normalize_to_null()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", "ILS1.PDF", civil: "  ", amdtNum: "", faanfd18: "   ")]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Null(procedure.Civil);
		Assert.Null(procedure.AmdtNum);
		Assert.Null(procedure.ComputerCode);
	}

	[Fact]
	public void civil_amendment_number_and_computer_code_are_trimmed_and_carried_through()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				ProcedureTestData.RecordRow(
					"AAA", 1, "STR", "GRUUB ONE (RNAV)", "GRUUB1.PDF",
					procUid: 42, civil: " C ", amdtNum: " 1A ", faanfd18: " JALEX3.JALEX "),
			]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Equal(42, procedure.ProcUid);
		Assert.Equal("C", procedure.Civil);
		Assert.Equal("1A", procedure.AmdtNum);
		Assert.Equal("JALEX3.JALEX", procedure.ComputerCode);
	}
}
