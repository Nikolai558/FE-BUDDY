using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Domain.Procedures.Models;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Covers <see cref="ProcedureChangesMarkdownWriter.Generate"/>: the exact title, note and
/// "no changes" text, the per-facility sections (flat list vs. grouped Changed/Deleted/New),
/// every entry's exact link format, every page of a multi-page procedure (Bug #365), the
/// shared-chart de-duplication rule, and the CRLF/UTF-8 (no BOM) output.
/// </summary>
public sealed class ProcedureChangesMarkdownWriterTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_ProcedureChangesMarkdownWriter_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_outputDirectory))
		{
			Directory.Delete(_outputDirectory, recursive: true);
		}
	}

	private ProcedureSettings Settings(string? primaryFacility = null) => new()
	{
		OutputDirectory = _outputDirectory,
		Facilities = ["ZOB"],
		PrimaryFacility = primaryFacility,
	};

	private static string ExpectedContent(params string[] lines) => string.Join("\r\n", lines) + "\r\n";

	private string Generate(IReadOnlyList<ProcedureAirport> airports, DtppMetafileDataCollection dtpp, DtppMetafileDataCollection? previousDtpp = null, string? primaryFacility = null)
	{
		string path = ProcedureChangesMarkdownWriter.Generate(airports, Settings(primaryFacility), dtpp, previousDtpp);
		return File.ReadAllText(path);
	}

	[Fact]
	public void generate_rejects_null_arguments()
	{
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609");

		Assert.Throws<ArgumentNullException>(() => ProcedureChangesMarkdownWriter.Generate(null!, Settings(), dtpp, null));
		Assert.Throws<ArgumentNullException>(() => ProcedureChangesMarkdownWriter.Generate([], null!, dtpp, null));
		Assert.Throws<ArgumentNullException>(() => ProcedureChangesMarkdownWriter.Generate([], Settings(), null!, null));
	}

	[Fact]
	public void generate_creates_the_publication_docs_folder_and_returns_its_path()
	{
		string path = ProcedureChangesMarkdownWriter.Generate([], Settings(), ProcedureTestData.Dtpp("2609"), null);

		Assert.Equal(Path.Combine(_outputDirectory, "Publication_Docs", "Procedure_Changes.md"), path);
		Assert.True(File.Exists(path));
	}

	[Fact]
	public void the_title_includes_the_formatted_effective_date()
	{
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609", fromEffectiveUtc: new DateTime(2026, 9, 3, 9, 1, 0, DateTimeKind.Utc));

		string content = Generate([], dtpp);

		Assert.StartsWith("# AIRAC 2609 (03SEP2026)\r\n", content, StringComparison.Ordinal);
	}

	[Fact]
	public void the_title_falls_back_to_just_the_cycle_without_an_effective_date()
	{
		string content = Generate([], ProcedureTestData.Dtpp("2609"));

		Assert.StartsWith("# AIRAC 2609\r\n", content, StringComparison.Ordinal);
	}

	[Fact]
	public void the_note_line_is_always_present()
	{
		string content = Generate([], ProcedureTestData.Dtpp("2609"));

		Assert.Contains(
			"Note: In some cases, the link will return a 404 Error. This is because the FAA does not have a comparative document. " +
			"This is common with Military facilities.",
			content);
	}

	[Fact]
	public void no_changes_at_all_prints_the_no_changes_message_and_nothing_else()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
		[
			ProcedureTestData.BuiltProcedure("UNCHANGED", change: ProcedureChange.None),
		]);

		string content = Generate([airport], ProcedureTestData.Dtpp("2609", fromEffectiveUtc: new DateTime(2026, 9, 3, 9, 1, 0, DateTimeKind.Utc)));

		Assert.Equal(
			ExpectedContent(
				"# AIRAC 2609 (03SEP2026)",
				"",
				"Note: In some cases, the link will return a 404 Error. This is because the FAA does not have a comparative document. This is common with Military facilities.",
				"",
				"No procedure changes this AIRAC for the selected airports and procedures."),
			content);
	}

	[Fact]
	public void a_facility_with_no_changes_prints_its_own_no_changes_line_while_other_facilities_show_theirs()
	{
		ProcedureAirport zobAirport = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
		[
			ProcedureTestData.BuiltProcedure("ILS RWY 1", change: ProcedureChange.New, pdfName: "00100ILS1.PDF"),
		]);
		ProcedureAirport znyAirport = ProcedureTestData.BuiltAirport("BBB", responsibleArtcc: "ZNY", procedures:
		[
			ProcedureTestData.BuiltProcedure("VOR RWY 2", change: ProcedureChange.None),
		]);

		string content = Generate([zobAirport, znyAirport], ProcedureTestData.Dtpp("2609"));

		Assert.Equal(
			ExpectedContent(
				"# AIRAC 2609",
				"",
				"Note: In some cases, the link will return a 404 Error. This is because the FAA does not have a comparative document. This is common with Military facilities.",
				"",
				"## ZNY",
				"- No procedure changes this AIRAC.",
				"",
				"## ZOB",
				"- AAA",
				"  - New:",
				"    - [ILS RWY 1](https://aeronav.faa.gov/d-tpp/2609/00100ILS1.PDF)"),
			content);
	}

	[Fact]
	public void an_airport_whose_entries_are_all_changed_or_readded_uses_a_flat_list()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
		[
			ProcedureTestData.BuiltProcedure("PROC A", change: ProcedureChange.Changed, pdfName: "00100A.PDF"),
			ProcedureTestData.BuiltProcedure("PROC B", change: ProcedureChange.Changed, pdfName: "00100B.PDF"),
		]);

		string content = Generate([airport], ProcedureTestData.Dtpp("2609"));

		Assert.Contains(
			"- AAA\r\n" +
			"  - [PROC A](https://aeronav.faa.gov/d-tpp/2609/compare_pdf/00100A_cmp.pdf)\r\n" +
			"  - [PROC B](https://aeronav.faa.gov/d-tpp/2609/compare_pdf/00100B_cmp.pdf)\r\n",
			content);
	}

	[Fact]
	public void an_airport_with_mixed_change_types_groups_them_changed_then_deleted_then_new()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
		[
			ProcedureTestData.BuiltProcedure("NEW PROC", chartSeq: 10, change: ProcedureChange.New, pdfName: "00100NEW.PDF"),
			ProcedureTestData.BuiltProcedure("DELETED PROC", chartSeq: 20, change: ProcedureChange.Deleted),
			ProcedureTestData.BuiltProcedure("CHANGED PROC", chartSeq: 30, change: ProcedureChange.Changed, pdfName: "00100CHG.PDF"),
		]);

		string content = Generate([airport], ProcedureTestData.Dtpp("2609"));

		Assert.Contains(
			"- AAA\r\n" +
			"  - Changed:\r\n" +
			"    - [CHANGED PROC](https://aeronav.faa.gov/d-tpp/2609/compare_pdf/00100CHG_cmp.pdf)\r\n" +
			"  - Deleted:\r\n" +
			"    - DELETED PROC (previous chart not available)\r\n" +
			"  - New:\r\n" +
			"    - [NEW PROC](https://aeronav.faa.gov/d-tpp/2609/00100NEW.PDF)\r\n",
			content);
	}

	[Fact]
	public void a_deleted_entry_links_to_the_previous_cycle_chart_when_available()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
		[
			ProcedureTestData.BuiltProcedure("ILS RWY 1", change: ProcedureChange.Deleted, previousPdfName: "00100OLD.PDF"),
		]);

		string content = Generate([airport], ProcedureTestData.Dtpp("2609"), ProcedureTestData.Dtpp("2608"));

		Assert.Contains(
			"  - Deleted:\r\n" +
			"    - [ILS RWY 1](https://aeronav.faa.gov/d-tpp/2608/00100OLD.PDF)\r\n",
			content);
	}

	[Fact]
	public void a_deleted_entry_without_a_previous_pdf_says_previous_chart_not_available()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
		[
			ProcedureTestData.BuiltProcedure("ILS RWY 1", change: ProcedureChange.Deleted, previousPdfName: null),
		]);

		string content = Generate([airport], ProcedureTestData.Dtpp("2609"));

		Assert.Contains("    - ILS RWY 1 (previous chart not available)\r\n", content);
	}

	[Fact]
	public void a_deleted_entry_removed_from_the_airport_only_gets_an_info_line()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
		[
			ProcedureTestData.BuiltProcedure(
				"ILS RWY 1", change: ProcedureChange.Deleted, previousPdfName: null, isRemovedFromAirportOnly: true),
		]);

		string content = Generate([airport], ProcedureTestData.Dtpp("2609"));

		Assert.Contains(
			"    - ILS RWY 1 (previous chart not available)\r\n" +
			"      - Info: The FAA removed it from this airport's list; the procedure itself may still be in use elsewhere.\r\n",
			content);
	}

	[Fact]
	public void a_readded_entry_shows_old_and_new_links_plus_the_info_line()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
		[
			ProcedureTestData.BuiltProcedure(
				"ILS RWY 1", change: ProcedureChange.ReAdded,
				previousPdfName: "00100OLD.PDF", pdfName: "00100NEW.PDF"),
		]);

		string content = Generate([airport], ProcedureTestData.Dtpp("2609"), ProcedureTestData.Dtpp("2608"));

		Assert.Contains(
			"- AAA\r\n" +
			"  - ILS RWY 1 [Old](https://aeronav.faa.gov/d-tpp/2608/00100OLD.PDF) -> [New](https://aeronav.faa.gov/d-tpp/2609/00100NEW.PDF)\r\n" +
			"    - Info: This procedure was initially listed by the FAA as \"Deleted\" but then was added as a \"New\" procedure in the d-Tpp MetaFile. Check carefully\r\n",
			content);
	}

	[Fact]
	public void a_readded_entry_without_a_previous_pdf_omits_the_old_link()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
		[
			ProcedureTestData.BuiltProcedure(
				"ILS RWY 1", change: ProcedureChange.ReAdded, previousPdfName: null, pdfName: "00100NEW.PDF"),
		]);

		string content = Generate([airport], ProcedureTestData.Dtpp("2609"));

		Assert.Contains("  - ILS RWY 1 [New](https://aeronav.faa.gov/d-tpp/2609/00100NEW.PDF)\r\n", content);
	}

	[Fact]
	public void a_readded_entry_without_a_current_pdf_says_chart_not_available()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
		[
			ProcedureTestData.BuiltProcedure(
				"ILS RWY 1", change: ProcedureChange.ReAdded, previousPdfName: null,
				pages: [new("ILS RWY 1", "DELETED_JOB.PDF", "D")]),
		]);

		string content = Generate([airport], ProcedureTestData.Dtpp("2609"));

		Assert.Contains("  - ILS RWY 1 [New](chart not available)\r\n", content);
	}

	// ---- every page of a multi-page procedure (Bug #365) ----

	/// <summary>Writes one airport, under ZOB, with one procedure, for cycle 2611 (previous 2610).</summary>
	private string GenerateOne(string aptIdent, Procedure procedure) =>
		Generate(
			[ProcedureTestData.BuiltAirport(aptIdent, responsibleArtcc: "ZOB", procedures: [procedure])],
			ProcedureTestData.Dtpp("2611"),
			ProcedureTestData.Dtpp("2610"));

	/// <summary>Builds the airports from metafile records, as the sub-service does, and writes them.</summary>
	private string GenerateBuilt(
		string aptIdent,
		IEnumerable<DtppMetafileXmlDataModel.Record> records,
		IEnumerable<DtppMetafileXmlDataModel.Record> previousRecords)
	{
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2611", airports: [ProcedureTestData.AirportRow(aptIdent)], records: records);
		DtppMetafileDataCollection previousDtpp = ProcedureTestData.Dtpp("2610", records: previousRecords);
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr([ProcedureTestData.AptBaseRow(aptIdent, respArtccId: "ZOB")]);

		return Generate(ProcedureBuilder.Build(nasr, dtpp, previousDtpp).Airports, dtpp, previousDtpp);
	}

	[Fact]
	public void every_changed_page_links_to_its_compare_pdf_in_page_order()
	{
		string content = GenerateOne("SLC", ProcedureTestData.BuiltProcedure("CARTR TWO (RNAV)", chartCode: "STR", change: ProcedureChange.Changed, pages:
		[
			new("CARTR TWO (RNAV)", "00365CARTR.PDF", "C"),
			new("CARTR TWO (RNAV), CONT.1", "00365CARTR_C.PDF", "C"),
		]));

		Assert.Contains(
			"- SLC\r\n" +
			"  - [CARTR TWO (RNAV)](https://aeronav.faa.gov/d-tpp/2611/compare_pdf/00365CARTR_cmp.pdf)\r\n" +
			"    - More pages: [Page 2](https://aeronav.faa.gov/d-tpp/2611/compare_pdf/00365CARTR_C_cmp.pdf)\r\n",
			content);
	}

	[Fact]
	public void an_unchanged_page_links_to_its_chart()
	{
		string content = GenerateOne("ORD", ProcedureTestData.BuiltProcedure("OHARE NINE", chartCode: "DP", change: ProcedureChange.Changed, pages:
		[
			new("OHARE NINE", "00166OHARE.PDF", "C"),
			new("OHARE NINE, CONT.1", "00166OHARE_C.PDF", null),
		]));

		Assert.Contains(
			"  - [OHARE NINE](https://aeronav.faa.gov/d-tpp/2611/compare_pdf/00166OHARE_cmp.pdf)\r\n" +
			"    - More pages: [Page 2 (unchanged)](https://aeronav.faa.gov/d-tpp/2611/00166OHARE_C.PDF)\r\n",
			content);
	}

	[Fact]
	public void a_page_added_to_a_changed_procedure_is_a_new_page()
	{
		string content = GenerateOne("OAK", ProcedureTestData.BuiltProcedure("CNDEL SIX (RNAV)", chartCode: "DP", change: ProcedureChange.Changed, pages:
		[
			new("CNDEL SIX (RNAV)", "00227CNDEL.PDF", "C"),
			new("CNDEL SIX (RNAV), CONT.1", "00227CNDEL_C.PDF", "A"),
		]));

		Assert.Contains(
			"  - [CNDEL SIX (RNAV)](https://aeronav.faa.gov/d-tpp/2611/compare_pdf/00227CNDEL_cmp.pdf)\r\n" +
			"    - More pages: [Page 2 (new page)](https://aeronav.faa.gov/d-tpp/2611/00227CNDEL_C.PDF)\r\n",
			content);
	}

	[Fact]
	public void a_new_procedures_changed_page_links_to_its_compare_pdf()
	{
		string content = GenerateOne("GGE", ProcedureTestData.BuiltProcedure("BRDWK ONE (RNAV)", chartCode: "STR", change: ProcedureChange.New, pages:
		[
			new("BRDWK ONE (RNAV)", "05214BRDWK.PDF", "A"),
			new("BRDWK ONE (RNAV), CONT.1", "05214BRDWK_C.PDF", "C"),
		]));

		Assert.Contains(
			"  - New:\r\n" +
			"    - [BRDWK ONE (RNAV)](https://aeronav.faa.gov/d-tpp/2611/05214BRDWK.PDF)\r\n" +
			"      - More pages: [Page 2](https://aeronav.faa.gov/d-tpp/2611/compare_pdf/05214BRDWK_C_cmp.pdf)\r\n",
			content);
	}

	[Fact]
	public void a_new_procedures_added_pages_are_not_called_new_pages()
	{
		string content = GenerateOne("AAA", ProcedureTestData.BuiltProcedure("GRUUB ONE (RNAV)", chartCode: "STR", change: ProcedureChange.New, pages:
		[
			new("GRUUB ONE (RNAV)", "GRUUB1.PDF", "A"),
			new("GRUUB ONE (RNAV), CONT.1", "GRUUB1_C.PDF", "A"),
		]));

		Assert.Contains("      - More pages: [Page 2](https://aeronav.faa.gov/d-tpp/2611/GRUUB1_C.PDF)\r\n", content);
	}

	[Fact]
	public void pages_are_listed_in_page_order_whatever_order_they_come_in()
	{
		string content = GenerateOne("AAA", ProcedureTestData.BuiltProcedure("GRUUB ONE (RNAV)", chartCode: "STR", change: ProcedureChange.Changed, pages:
		[
			new("GRUUB ONE (RNAV), CONT.2", "GRUUB1_C2.PDF", "C"),
			new("GRUUB ONE (RNAV)", "GRUUB1.PDF", "C"),
			new("GRUUB ONE (RNAV), CONT.1", "GRUUB1_C.PDF", null),
		]));

		Assert.Contains(
			"  - [GRUUB ONE (RNAV)](https://aeronav.faa.gov/d-tpp/2611/compare_pdf/GRUUB1_cmp.pdf)\r\n" +
			"    - More pages: [Page 2 (unchanged)](https://aeronav.faa.gov/d-tpp/2611/GRUUB1_C.PDF), " +
			"[Page 3](https://aeronav.faa.gov/d-tpp/2611/compare_pdf/GRUUB1_C2_cmp.pdf)\r\n",
			content);
	}

	/// <summary>Only page 2 changed (THRNE FOUR at BUR in 2609): the entry is the procedure's own name, its first page unchanged.</summary>
	[Fact]
	public void when_only_a_continuation_page_changed_the_entry_is_the_procedures_name()
	{
		string content = GenerateOne("BUR", ProcedureTestData.BuiltProcedure("THRNE FOUR (RNAV)", chartCode: "STR", change: ProcedureChange.Changed, pages:
		[
			new("THRNE FOUR (RNAV)", "00228THRNE.PDF", null),
			new("THRNE FOUR (RNAV), CONT.1", "00228THRNE_C.PDF", "C"),
		]));

		Assert.Contains(
			"  - [THRNE FOUR (RNAV)](https://aeronav.faa.gov/d-tpp/2611/00228THRNE.PDF) (unchanged)\r\n" +
			"    - More pages: [Page 2](https://aeronav.faa.gov/d-tpp/2611/compare_pdf/00228THRNE_C_cmp.pdf)\r\n",
			content);
		Assert.DoesNotContain("CONT.1", content);
	}

	[Fact]
	public void every_page_of_a_deleted_procedure_links_to_the_previous_cycle()
	{
		string content = GenerateOne("NKX", ProcedureTestData.BuiltProcedure(
			"LAKEE THREE", chartCode: "DP", change: ProcedureChange.Deleted, previousPdfName: "00903LAKEE.PDF",
			pages:
			[
				new("LAKEE THREE", "DELETED_JOB.PDF", "D"),
				new("LAKEE THREE, CONT.1", "DELETED_JOB.PDF", "D"),
				new("LAKEE THREE, CONT.2", "DELETED_JOB.PDF", "D"),
			],
			previousPages:
			[
				new("LAKEE THREE", "00903LAKEE.PDF", null),
				new("LAKEE THREE, CONT.1", "00903LAKEE_C.PDF", null),
			]));

		Assert.Contains(
			"  - Deleted:\r\n" +
			"    - [LAKEE THREE](https://aeronav.faa.gov/d-tpp/2610/00903LAKEE.PDF)\r\n" +
			"      - More pages: [Page 2](https://aeronav.faa.gov/d-tpp/2610/00903LAKEE_C.PDF), Page 3 (previous chart not available)\r\n",
			content);
	}

	[Fact]
	public void every_page_of_a_readded_procedure_shows_old_and_new()
	{
		string content = GenerateOne("AAA", ProcedureTestData.BuiltProcedure(
			"GRUUB ONE (RNAV)", chartCode: "STR", change: ProcedureChange.ReAdded, previousPdfName: "GRUUB1.PDF",
			pages:
			[
				new("GRUUB ONE (RNAV)", "DELETED_JOB.PDF", "D"),
				new("GRUUB ONE (RNAV)", "NEWGRUUB1.PDF", "A"),
				new("GRUUB ONE (RNAV), CONT.1", "DELETED_JOB.PDF", "D"),
				new("GRUUB ONE (RNAV), CONT.1", "NEWGRUUB1_C.PDF", "A"),
				new("GRUUB ONE (RNAV), CONT.2", "NEWGRUUB1_C2.PDF", null),
			],
			previousPages:
			[
				new("GRUUB ONE (RNAV)", "GRUUB1.PDF", null),
				new("GRUUB ONE (RNAV), CONT.1", "GRUUB1_C.PDF", null),
			]));

		Assert.Contains(
			"  - GRUUB ONE (RNAV) [Old](https://aeronav.faa.gov/d-tpp/2610/GRUUB1.PDF) -> [New](https://aeronav.faa.gov/d-tpp/2611/NEWGRUUB1.PDF)\r\n" +
			"    - More pages: Page 2 [Old](https://aeronav.faa.gov/d-tpp/2610/GRUUB1_C.PDF) -> [New](https://aeronav.faa.gov/d-tpp/2611/NEWGRUUB1_C.PDF), " +
			"Page 3 [New](https://aeronav.faa.gov/d-tpp/2611/NEWGRUUB1_C2.PDF)\r\n" +
			"    - Info: This procedure was initially listed by the FAA as \"Deleted\"",
			content);
	}

	[Fact]
	public void a_removed_page_without_a_previous_chart_says_so()
	{
		string content = GenerateOne("AAA", ProcedureTestData.BuiltProcedure("GRUUB ONE (RNAV)", chartCode: "STR", change: ProcedureChange.Changed, pages:
		[
			new("GRUUB ONE (RNAV)", "GRUUB1.PDF", "C"),
			new("GRUUB ONE (RNAV), CONT.1", "DELETED_JOB.PDF", "D"),
		]));

		Assert.Contains("    - More pages: Page 2 (removed, previous chart not available)\r\n", content);
	}

	/// <summary>
	/// MSP in 2611: the deleted "MINNEAPOLIS NINE, CONT.1" is MINNEAPOLIS ONE's removed second page,
	/// linked to the previous cycle's page 2 - not listed on its own, and never linked to page 1.
	/// </summary>
	[Fact]
	public void an_orphan_continuation_page_is_listed_as_a_removed_page_of_the_procedure_with_its_proc_uid()
	{
		string content = GenerateBuilt(
			"MSP",
			[
				ProcedureTestData.RecordRow("MSP", 1, "DP", "MINNEAPOLIS NINE, CONT.1", "DELETED_JOB.PDF", userAction: "D", procUid: 1300),
				ProcedureTestData.RecordRow("MSP", 2, "DP", "MINNEAPOLIS ONE", "00264MINNEAPOLIS.PDF", userAction: "C", procUid: 1300),
			],
			[
				ProcedureTestData.RecordRow("MSP", 1, "DP", "MINNEAPOLIS NINE", "00264MINNEAPOLIS.PDF", procUid: 1300),
				ProcedureTestData.RecordRow("MSP", 2, "DP", "MINNEAPOLIS NINE, CONT.1", "00264MINNEAPOLIS_C.PDF", procUid: 1300),
			]);

		Assert.Contains(
			"- MSP\r\n" +
			"  - [MINNEAPOLIS ONE](https://aeronav.faa.gov/d-tpp/2611/compare_pdf/00264MINNEAPOLIS_cmp.pdf)\r\n" +
			"    - More pages: [Page 2 (removed)](https://aeronav.faa.gov/d-tpp/2610/00264MINNEAPOLIS_C.PDF)\r\n",
			content);
		Assert.DoesNotContain("MINNEAPOLIS NINE", content);
	}

	[Fact]
	public void an_orphan_continuation_page_with_no_procedure_is_listed_under_its_base_name()
	{
		string content = GenerateBuilt(
			"AAA",
			[ProcedureTestData.RecordRow("AAA", 1, "STR", "GRUUB ONE (RNAV), CONT.1", "DELETED_JOB.PDF", userAction: "D", procUid: 7)],
			[
				ProcedureTestData.RecordRow("AAA", 1, "STR", "GRUUB ONE (RNAV)", "GRUUB1.PDF", procUid: 7),
				ProcedureTestData.RecordRow("AAA", 2, "STR", "GRUUB ONE (RNAV), CONT.1", "GRUUB1_C.PDF", procUid: 7),
			]);

		Assert.Contains(
			"- AAA\r\n" +
			"  - Deleted:\r\n" +
			"    - GRUUB ONE (RNAV)\r\n" +
			"      - Pages: [Page 2](https://aeronav.faa.gov/d-tpp/2610/GRUUB1_C.PDF)\r\n",
			content);
	}

	// ---- a chart shared by several airports ----

	/// <summary>
	/// Issue #323: each airport's list is complete, so a STAR shared by BUR and VNY shows under both,
	/// each noting the other.
	/// </summary>
	[Fact]
	public void a_shared_chart_is_listed_under_every_airport_it_serves_with_the_others_noted()
	{
		ProcedureAirport aaa = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", alnum: 100, procedures:
		[
			ProcedureTestData.BuiltProcedure("GRUUB ONE (RNAV)", change: ProcedureChange.Changed, pdfName: "00200SHARED.PDF"),
		]);
		ProcedureAirport bbb = ProcedureTestData.BuiltAirport("BBB", responsibleArtcc: "ZOB", alnum: 200, procedures:
		[
			ProcedureTestData.BuiltProcedure("GRUUB ONE (RNAV)", change: ProcedureChange.Changed, pdfName: "00200SHARED.PDF"),
		]);

		string content = Generate([aaa, bbb], ProcedureTestData.Dtpp("2609"));

		Assert.Contains(
			"- AAA\r\n" +
			"  - [GRUUB ONE (RNAV)](https://aeronav.faa.gov/d-tpp/2609/compare_pdf/00200SHARED_cmp.pdf)\r\n" +
			"    - Also serves: BBB\r\n",
			content);
		Assert.Contains(
			"- BBB\r\n" +
			"  - [GRUUB ONE (RNAV)](https://aeronav.faa.gov/d-tpp/2609/compare_pdf/00200SHARED_cmp.pdf)\r\n" +
			"    - Also serves: AAA\r\n",
			content);
	}

	/// <summary>
	/// A regional takeoff minimums page is one PDF listed twice at each airport it covers (TAKEOFF
	/// MINIMUMS and DIVERSE VECTOR AREA): both are listed, and the note names the other airport once,
	/// never the airport itself.
	/// </summary>
	[Fact]
	public void the_note_names_each_other_airport_once_and_never_the_airport_itself()
	{
		ProcedureAirport aaa = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
		[
			ProcedureTestData.BuiltProcedure("TAKEOFF MINIMUMS", change: ProcedureChange.Changed, pdfName: "SE4TO.PDF"),
			ProcedureTestData.BuiltProcedure("DIVERSE VECTOR AREA", change: ProcedureChange.Changed, pdfName: "SE4TO.PDF"),
		]);
		ProcedureAirport bbb = ProcedureTestData.BuiltAirport("BBB", responsibleArtcc: "ZOB", procedures:
		[
			ProcedureTestData.BuiltProcedure("TAKEOFF MINIMUMS", change: ProcedureChange.Changed, pdfName: "SE4TO.PDF"),
			ProcedureTestData.BuiltProcedure("DIVERSE VECTOR AREA", change: ProcedureChange.Changed, pdfName: "SE4TO.PDF"),
		]);

		string content = Generate([aaa, bbb], ProcedureTestData.Dtpp("2609"));
		string[] lines = content.Split("\r\n");

		Assert.Equal(2, lines.Count(line => line == "    - Also serves: BBB"));
		Assert.Equal(2, lines.Count(line => line == "    - Also serves: AAA"));
		Assert.Equal(4, lines.Count(line => line.StartsWith("    - Also serves:", StringComparison.Ordinal)));
	}

	/// <summary>A regional minimums page can serve a hundred airports: the note names ten and counts the rest.</summary>
	[Fact]
	public void a_note_with_more_than_ten_other_airports_names_ten_and_counts_the_rest()
	{
		ProcedureAirport[] airports = [.. Enumerable.Range(1, 13).Select(i =>
			ProcedureTestData.BuiltAirport($"A{i:D2}", responsibleArtcc: "ZOB", procedures:
			[
				ProcedureTestData.BuiltProcedure("ALTERNATE MINIMUMS", change: ProcedureChange.Changed, pdfName: "SW3ALT.PDF"),
			]))];

		string content = Generate(airports, ProcedureTestData.Dtpp("2609"));

		Assert.Contains(
			"- A01\r\n" +
			"  - [ALTERNATE MINIMUMS](https://aeronav.faa.gov/d-tpp/2609/compare_pdf/SW3ALT_cmp.pdf)\r\n" +
			"    - Also serves: A02, A03, A04, A05, A06, A07, A08, A09, A10, A11 and 2 more\r\n",
			content);
	}

	// ---- a chart included for its route's fixes in the facility (Feature #367) ----

	[Fact]
	public void a_chart_included_only_for_its_routes_fixes_says_so_naming_them()
	{
		ProcedureAirport phl = ProcedureTestData.BuiltAirport("PHL", responsibleArtcc: "ZNY", procedures:
		[
			ProcedureTestData.BuiltProcedure("JIIMS FOUR (RNAV)", chartCode: "STR", change: ProcedureChange.Changed, pdfName: "00178JIIMS.PDF",
				includedForFacilityFixes: new FacilityFixMatch("ZDC", ["DUCKY", "OTTTO"])),
			ProcedureTestData.BuiltProcedure("ILS RWY 9R", change: ProcedureChange.Changed, pdfName: "00178IL9R.PDF"),
		]);

		string content = Generate([phl], ProcedureTestData.Dtpp("2611"));

		Assert.Contains(
			"  - [JIIMS FOUR (RNAV)](https://aeronav.faa.gov/d-tpp/2611/compare_pdf/00178JIIMS_cmp.pdf)\r\n" +
			"    - In ZDC: its route has a fix there (DUCKY, OTTTO)\r\n" +
			"  - [ILS RWY 9R](https://aeronav.faa.gov/d-tpp/2611/compare_pdf/00178IL9R_cmp.pdf)\r\n",
			content);
	}

	[Fact]
	public void the_fix_line_names_five_fixes_and_counts_the_rest_before_also_serves()
	{
		FacilityFixMatch match = new("ZDC", ["JIIMS", "RANSM", "WEVVE", "ERNYY", "WOJIK", "PSOUT", "DUCKY"]);
		ProcedureAirport[] airports = [.. new[] { "PHL", "TTN" }.Select(id =>
			ProcedureTestData.BuiltAirport(id, responsibleArtcc: "ZNY", procedures:
			[
				ProcedureTestData.BuiltProcedure("JIIMS FOUR (RNAV)", chartCode: "STR", change: ProcedureChange.Changed, pdfName: "00178JIIMS.PDF",
					includedForFacilityFixes: match),
			]))];

		string content = Generate(airports, ProcedureTestData.Dtpp("2611"));

		Assert.Contains(
			"    - In ZDC: its route has a fix there (JIIMS, RANSM, WEVVE, ERNYY, WOJIK and 2 more)\r\n" +
			"    - Also serves: TTN\r\n",
			content);
	}

	[Fact]
	public void a_chart_with_no_fix_match_has_no_fix_line()
	{
		ProcedureAirport aaa = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
			[ProcedureTestData.BuiltProcedure("ILS RWY 1", change: ProcedureChange.Changed)]);

		Assert.DoesNotContain("its route has a fix there", Generate([aaa], ProcedureTestData.Dtpp("2611")), StringComparison.Ordinal);
	}

	// ---- facility ordering ----

	[Fact]
	public void the_primary_facility_section_comes_before_alphabetically_earlier_facilities()
	{
		ProcedureAirport zob = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
		[
			ProcedureTestData.BuiltProcedure("ONE", change: ProcedureChange.New),
		]);
		ProcedureAirport zny = ProcedureTestData.BuiltAirport("BBB", responsibleArtcc: "ZNY", procedures:
		[
			ProcedureTestData.BuiltProcedure("TWO", change: ProcedureChange.New),
		]);

		string content = Generate([zob, zny], ProcedureTestData.Dtpp("2609"), primaryFacility: "ZOB");

		Assert.True(content.IndexOf("## ZOB", StringComparison.Ordinal) < content.IndexOf("## ZNY", StringComparison.Ordinal));
	}

	// ---- CRLF / UTF-8 without a BOM ----

	[Fact]
	public void output_is_crlf_and_utf8_without_a_bom()
	{
		string path = ProcedureChangesMarkdownWriter.Generate([], Settings(), ProcedureTestData.Dtpp("2609"), null);

		byte[] bytes = File.ReadAllBytes(path);
		bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
		Assert.False(hasBom);

		string content = File.ReadAllText(path);
		string withoutCrlf = content.Replace("\r\n", string.Empty, StringComparison.Ordinal);
		Assert.DoesNotContain('\n', withoutCrlf);
		Assert.DoesNotContain('\r', withoutCrlf);
	}
}
