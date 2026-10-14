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
		Assert.Equal("GRUUB ONE (RNAV)", procedure.Name);
		Assert.Equal("GRUUB1B.PDF", procedure.CurrentPdfName);
	}

	/// <summary>
	/// Only a continuation page was removed or added: the procedure carries on, so it is Changed - not
	/// Deleted (which would drop it from Procedures.json) or New - and its pages say what happened.
	/// </summary>
	[Theory]
	[InlineData("D", DtppFiles.DeletedChartPdfName)]
	[InlineData("A", "GRUUB1B.PDF")]
	public void a_removed_or_added_continuation_with_an_unchanged_main_page_is_changed(string userAction, string pdfName)
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				ProcedureTestData.RecordRow("AAA", 30000, "STR", "GRUUB ONE (RNAV)", "GRUUB1.PDF"),
				ProcedureTestData.RecordRow("AAA", 30001, "STR", "GRUUB ONE (RNAV), CONT.1", pdfName, userAction: userAction),
			]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Equal(ProcedureChange.Changed, procedure.Change);
		Assert.Equal("GRUUB1.PDF", procedure.CurrentPdfName); // the main page persists
		Assert.False(procedure.IsRemovedFromAirportOnly);
	}

	[Fact]
	public void an_unrecognized_user_action_on_a_continuation_with_an_unchanged_main_page_is_classified_none()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				ProcedureTestData.RecordRow("AAA", 30000, "STR", "GRUUB ONE (RNAV)", "GRUUB1.PDF"),
				ProcedureTestData.RecordRow("AAA", 30001, "STR", "GRUUB ONE (RNAV), CONT.1", "GRUUB1B.PDF", userAction: "X"),
			]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Equal(ProcedureChange.None, procedure.Change);
		Assert.Equal("GRUUB1.PDF", procedure.CurrentPdfName);
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
	public void a_changed_continuation_with_an_unchanged_main_page_is_changed_under_the_procedures_name()
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
		Assert.Equal("GRUUB ONE (RNAV)", procedure.Name);
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

	[Fact]
	public void a_previous_page_found_by_proc_uid_is_matched_by_page_number()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2611",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				ProcedureTestData.RecordRow("AAA", 1, "DP", "TEST TWO", DtppFiles.DeletedChartPdfName, userAction: "D", procUid: 3),
				ProcedureTestData.RecordRow("AAA", 2, "DP", "TEST TWO, CONT.1", DtppFiles.DeletedChartPdfName, userAction: "D", procUid: 3),
			]);

		// Renamed since the previous cycle: the main page is listed first, but page 2 is still CONT.1.
		DtppMetafileDataCollection previousDtpp = ProcedureTestData.Dtpp("2610", records:
		[
			ProcedureTestData.RecordRow("AAA", 1, "DP", "TEST ONE", "00100TEST.PDF", procUid: 3),
			ProcedureTestData.RecordRow("AAA", 2, "DP", "TEST ONE, CONT.1", "00100TEST_C.PDF", procUid: 3),
		]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, previousDtpp).Airports[0].Procedures);

		Assert.Equal(["00100TEST.PDF", "00100TEST_C.PDF"], procedure.PreviousPages.Select(page => page.PdfName));
	}

	[Fact]
	public void a_deletion_placeholder_in_the_previous_cycle_is_never_a_previous_page()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2611",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", DtppFiles.DeletedChartPdfName, userAction: "D", procUid: 9)]);
		DtppMetafileDataCollection previousDtpp = ProcedureTestData.Dtpp("2610", records:
		[
			ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", DtppFiles.DeletedChartPdfName, userAction: "D", procUid: 9),
			ProcedureTestData.RecordRow("AAA", 2, "IAP", "ILS Z RWY 1", "OLDILSZ1.PDF", procUid: 9),
		]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, previousDtpp).Airports[0].Procedures);

		Assert.Equal("OLDILSZ1.PDF", procedure.PreviousPdfName);
	}

	// ---- every page of a multi-page procedure (Bug #365) ----

	[Fact]
	public void pages_are_in_page_order_whatever_the_metafiles_order()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2611",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				ProcedureTestData.RecordRow("AAA", 30002, "STR", "GRUUB ONE (RNAV), CONT.2", "GRUUB1_C2.PDF", userAction: "C"),
				ProcedureTestData.RecordRow("AAA", 30000, "STR", "GRUUB ONE (RNAV)", "GRUUB1.PDF", userAction: "C"),
				ProcedureTestData.RecordRow("AAA", 30001, "STR", "GRUUB ONE (RNAV), CONT.1", "GRUUB1_C.PDF", userAction: "C"),
			]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Equal(["GRUUB1.PDF", "GRUUB1_C.PDF", "GRUUB1_C2.PDF"], procedure.Pages.Select(page => page.PdfName));
		Assert.Equal(ProcedureChange.Changed, procedure.Change);
		Assert.Equal("GRUUB1.PDF", procedure.CurrentPdfName);
	}

	[Fact]
	public void a_deleted_multi_page_procedure_finds_every_page_in_the_previous_cycle()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2611",
			airports: [ProcedureTestData.AirportRow("NKX")],
			records:
			[
				ProcedureTestData.RecordRow("NKX", 1, "DP", "LAKEE THREE", DtppFiles.DeletedChartPdfName, userAction: "D"),
				ProcedureTestData.RecordRow("NKX", 2, "DP", "LAKEE THREE, CONT.1", DtppFiles.DeletedChartPdfName, userAction: "D"),
				ProcedureTestData.RecordRow("NKX", 3, "DP", "LAKEE THREE, CONT.2", DtppFiles.DeletedChartPdfName, userAction: "D"),
			]);

		// The previous cycle has no CONT.2: that page is left out.
		DtppMetafileDataCollection previousDtpp = ProcedureTestData.Dtpp("2610", records:
		[
			ProcedureTestData.RecordRow("NKX", 1, "DP", "LAKEE THREE", "00903LAKEE.PDF"),
			ProcedureTestData.RecordRow("NKX", 2, "DP", "LAKEE THREE, CONT.1", "00903LAKEE_C.PDF"),
		]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, previousDtpp).Airports[0].Procedures);

		Assert.Equal(ProcedureChange.Deleted, procedure.Change);
		Assert.Equal(["LAKEE THREE", "LAKEE THREE, CONT.1"], procedure.PreviousPages.Select(page => page.ChartName));
		Assert.Equal(["00903LAKEE.PDF", "00903LAKEE_C.PDF"], procedure.PreviousPages.Select(page => page.PdfName));
		Assert.Equal("00903LAKEE.PDF", procedure.PreviousPdfName);
	}

	[Fact]
	public void a_readded_multi_page_procedure_finds_every_page_in_the_previous_cycle()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2611",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				ProcedureTestData.RecordRow("AAA", 1, "STR", "GRUUB ONE (RNAV)", DtppFiles.DeletedChartPdfName, userAction: "D"),
				ProcedureTestData.RecordRow("AAA", 2, "STR", "GRUUB ONE (RNAV), CONT.1", DtppFiles.DeletedChartPdfName, userAction: "D"),
				ProcedureTestData.RecordRow("AAA", 3, "STR", "GRUUB ONE (RNAV)", "NEWGRUUB1.PDF", userAction: "A"),
				ProcedureTestData.RecordRow("AAA", 4, "STR", "GRUUB ONE (RNAV), CONT.1", "NEWGRUUB1_C.PDF", userAction: "A"),
			]);
		DtppMetafileDataCollection previousDtpp = ProcedureTestData.Dtpp("2610", records:
		[
			ProcedureTestData.RecordRow("AAA", 1, "STR", "GRUUB ONE (RNAV)", "GRUUB1.PDF"),
			ProcedureTestData.RecordRow("AAA", 2, "STR", "GRUUB ONE (RNAV), CONT.1", "GRUUB1_C.PDF"),
		]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, previousDtpp).Airports[0].Procedures);

		Assert.Equal(ProcedureChange.ReAdded, procedure.Change);
		Assert.Equal(
			[("GRUUB ONE (RNAV)", "D"), ("GRUUB ONE (RNAV)", "A"), ("GRUUB ONE (RNAV), CONT.1", "D"), ("GRUUB ONE (RNAV), CONT.1", "A")],
			procedure.Pages.Select(page => (page.ChartName, page.UserAction)));
		Assert.Equal(["GRUUB1.PDF", "GRUUB1_C.PDF"], procedure.PreviousPages.Select(page => page.PdfName));
		Assert.Equal("NEWGRUUB1.PDF", procedure.CurrentPdfName);
		Assert.Equal("GRUUB1.PDF", procedure.PreviousPdfName);
	}

	[Fact]
	public void previous_pages_are_only_found_for_a_procedure_with_a_deleted_page()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2611",
			airports: [ProcedureTestData.AirportRow("SLC")],
			records:
			[
				ProcedureTestData.RecordRow("SLC", 1, "STR", "CARTR TWO (RNAV)", "00365CARTR.PDF", userAction: "C"),
				ProcedureTestData.RecordRow("SLC", 2, "STR", "CARTR TWO (RNAV), CONT.1", "00365CARTR_C.PDF", userAction: "C"),
			]);
		DtppMetafileDataCollection previousDtpp = ProcedureTestData.Dtpp("2610", records:
		[
			ProcedureTestData.RecordRow("SLC", 1, "STR", "CARTR ONE (RNAV)", "00365CARTR.PDF"),
			ProcedureTestData.RecordRow("SLC", 2, "STR", "CARTR ONE (RNAV), CONT.1", "00365CARTR_C.PDF"),
		]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, previousDtpp).Airports[0].Procedures);

		Assert.Empty(procedure.PreviousPages);
		Assert.Null(procedure.PreviousPdfName);
	}

	/// <summary>
	/// MSP in 2611: MINNEAPOLIS NINE was amended to MINNEAPOLIS ONE and lost its second page, so the
	/// metafile lists a deleted "MINNEAPOLIS NINE, CONT.1" with no main page. It joins MINNEAPOLIS ONE
	/// by procuid, and its previous page is the previous cycle's CONT.1 - never its main page.
	/// </summary>
	[Fact]
	public void an_orphan_continuation_page_joins_the_procedure_with_its_proc_uid()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2611",
			airports: [ProcedureTestData.AirportRow("MSP")],
			records:
			[
				ProcedureTestData.RecordRow("MSP", 1, "DP", "MINNEAPOLIS NINE, CONT.1", DtppFiles.DeletedChartPdfName, userAction: "D", procUid: 1300),
				ProcedureTestData.RecordRow("MSP", 2, "DP", "MINNEAPOLIS ONE", "00264MINNEAPOLIS.PDF", userAction: "C", procUid: 1300),
			]);
		DtppMetafileDataCollection previousDtpp = ProcedureTestData.Dtpp("2610", records:
		[
			ProcedureTestData.RecordRow("MSP", 1, "DP", "MINNEAPOLIS NINE", "00264MINNEAPOLIS.PDF", procUid: 1300),
			ProcedureTestData.RecordRow("MSP", 2, "DP", "MINNEAPOLIS NINE, CONT.1", "00264MINNEAPOLIS_C.PDF", procUid: 1300),
		]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, previousDtpp).Airports[0].Procedures);

		Assert.Equal("MINNEAPOLIS ONE", procedure.Name);
		Assert.Equal(ProcedureChange.Changed, procedure.Change);
		Assert.Equal(["MINNEAPOLIS ONE", "MINNEAPOLIS NINE, CONT.1"], procedure.Pages.Select(page => page.ChartName));
		Assert.Equal("00264MINNEAPOLIS.PDF", procedure.CurrentPdfName);
		Assert.Equal("00264MINNEAPOLIS_C.PDF", procedure.PreviousPages.Single(page => page.ChartName.EndsWith("CONT.1", StringComparison.Ordinal)).PdfName);
	}

	[Fact]
	public void an_orphan_continuation_page_with_no_matching_proc_uid_stays_its_own_procedure_under_its_base_name()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2611",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				ProcedureTestData.RecordRow("AAA", 1, "STR", "GRUUB ONE (RNAV), CONT.1", DtppFiles.DeletedChartPdfName, userAction: "D", procUid: 7),
				ProcedureTestData.RecordRow("AAA", 2, "STR", "OTHER ONE", "OTHER1.PDF", userAction: "C", procUid: 8),
			]);
		DtppMetafileDataCollection previousDtpp = ProcedureTestData.Dtpp("2610", records:
		[
			ProcedureTestData.RecordRow("AAA", 1, "STR", "GRUUB ONE (RNAV)", "GRUUB1.PDF", procUid: 7),
			ProcedureTestData.RecordRow("AAA", 2, "STR", "GRUUB ONE (RNAV), CONT.1", "GRUUB1_C.PDF", procUid: 7),
		]);

		IReadOnlyList<Procedure> procedures = ProcedureBuilder.Build(nasr, dtpp, previousDtpp).Airports[0].Procedures;

		Assert.Equal(["GRUUB ONE (RNAV)", "OTHER ONE"], procedures.Select(procedure => procedure.Name));

		Procedure orphan = procedures[0];
		Assert.Equal(ProcedureChange.Deleted, orphan.Change);
		Assert.Equal(["GRUUB1_C.PDF"], orphan.PreviousPages.Select(page => page.PdfName));
		Assert.Null(orphan.PreviousPdfName);
	}

	[Fact]
	public void a_changed_orphan_continuation_page_with_no_procedure_is_changed_with_no_current_chart()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2611",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 1, "STR", "GRUUB ONE (RNAV), CONT.1", "GRUUB1_C.PDF", userAction: "C", procUid: 7)]);

		Procedure procedure = Assert.Single(ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures);

		Assert.Equal("GRUUB ONE (RNAV)", procedure.Name);
		Assert.Equal(ProcedureChange.Changed, procedure.Change);
		Assert.Null(procedure.CurrentPdfName);
		Assert.Equal(7, procedure.ProcUid);
		Assert.False(procedure.IsRemovedFromAirportOnly);
	}

	[Fact]
	public void only_an_orphan_continuation_page_is_matched_by_proc_uid()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr();
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2611",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				ProcedureTestData.RecordRow("AAA", 1, "IAP", "ILS RWY 1", "ILS1.PDF", userAction: "C", procUid: 5),
				ProcedureTestData.RecordRow("AAA", 2, "IAP", "ILS RWY 1 (CAT II)", "ILS1CAT2.PDF", procUid: 5),
				ProcedureTestData.RecordRow("AAA", 3, "IAP", "ILS RWY 1 (CAT II), CONT.1", "ILS1CAT2_C.PDF", procUid: 5),
			]);

		IReadOnlyList<Procedure> procedures = ProcedureBuilder.Build(nasr, dtpp, null).Airports[0].Procedures;

		Assert.Equal(["ILS RWY 1", "ILS RWY 1 (CAT II)"], procedures.Select(procedure => procedure.Name));
		Assert.Single(procedures[0].Pages);
		Assert.Equal(2, procedures[1].Pages.Count);
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
