using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Domain.Procedures.Models;
using FeBuddy.Core.Infrastructure.Dtpp.Models;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Covers <see cref="ProcedureChangesMarkdownWriter.Generate"/>: the exact title, note and
/// "no changes" text, the per-facility sections (flat list vs. grouped Changed/Deleted/New),
/// every entry's exact link format, the shared-chart de-duplication rule, and the CRLF/UTF-8
/// (no BOM) output.
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
		ProcedureChangesWriteResult result = ProcedureChangesMarkdownWriter.Generate(airports, Settings(primaryFacility), dtpp, previousDtpp);
		return File.ReadAllText(result.FilePath!);
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
		ProcedureChangesWriteResult result = ProcedureChangesMarkdownWriter.Generate([], Settings(), ProcedureTestData.Dtpp("2609"), null);

		Assert.Equal(Path.Combine(_outputDirectory, "Publication_Docs", "Procedure_Changes.md"), result.FilePath);
		Assert.True(File.Exists(result.FilePath));
		Assert.Empty(result.Messages);
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
			ProcedureTestData.BuiltProcedure("ILS RWY 1", change: ProcedureChange.New, reportedPdfName: "00100ILS1.PDF"),
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
			ProcedureTestData.BuiltProcedure("PROC A", change: ProcedureChange.Changed, reportedPdfName: "00100A.PDF"),
			ProcedureTestData.BuiltProcedure("PROC B", change: ProcedureChange.Changed, reportedPdfName: "00100B.PDF"),
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
			ProcedureTestData.BuiltProcedure("NEW PROC", chartSeq: 10, change: ProcedureChange.New, reportedPdfName: "00100NEW.PDF"),
			ProcedureTestData.BuiltProcedure("DELETED PROC", chartSeq: 20, change: ProcedureChange.Deleted, reportedPdfName: "DELETED_JOB.PDF"),
			ProcedureTestData.BuiltProcedure("CHANGED PROC", chartSeq: 30, change: ProcedureChange.Changed, reportedPdfName: "00100CHG.PDF"),
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
				previousPdfName: "00100OLD.PDF", currentPdfName: "00100NEW.PDF"),
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
				"ILS RWY 1", change: ProcedureChange.ReAdded, previousPdfName: null, currentPdfName: "00100NEW.PDF"),
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
				"ILS RWY 1", change: ProcedureChange.ReAdded, previousPdfName: null, currentPdfName: null),
		]);

		string content = Generate([airport], ProcedureTestData.Dtpp("2609"));

		Assert.Contains("  - ILS RWY 1 [New](chart not available)\r\n", content);
	}

	// ---- shared-chart de-duplication ----

	[Fact]
	public void a_shared_chart_is_owned_by_the_airport_whose_alnum_matches_the_pdf_prefix()
	{
		ProcedureAirport aaa = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", alnum: 100, procedures:
		[
			ProcedureTestData.BuiltProcedure("GRUUB ONE (RNAV)", change: ProcedureChange.Changed, reportedPdfName: "00200SHARED.PDF"),
		]);
		ProcedureAirport bbb = ProcedureTestData.BuiltAirport("BBB", responsibleArtcc: "ZOB", alnum: 200, procedures:
		[
			ProcedureTestData.BuiltProcedure("GRUUB ONE (RNAV)", change: ProcedureChange.Changed, reportedPdfName: "00200SHARED.PDF"),
		]);

		string content = Generate([aaa, bbb], ProcedureTestData.Dtpp("2609"));

		Assert.Contains(
			"- BBB\r\n" +
			"  - [GRUUB ONE (RNAV)](https://aeronav.faa.gov/d-tpp/2609/compare_pdf/00200SHARED_cmp.pdf)\r\n" +
			"    - Also serves: AAA\r\n",
			content);
		Assert.DoesNotContain(content.Split("\r\n"), line => line == "- AAA");
	}

	[Fact]
	public void a_shared_chart_falls_back_when_the_identity_is_too_short_to_carry_an_alnum_prefix()
	{
		// A PDF name shorter than 5 characters cannot carry a leading zero-padded alnum, so the
		// owner lookup must bail out before even checking whether the prefix is numeric.
		ProcedureAirport aaa = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", alnum: 100, procedures:
		[
			ProcedureTestData.BuiltProcedure("SHARED STAR", change: ProcedureChange.Changed, reportedPdfName: "X"),
		]);
		ProcedureAirport bbb = ProcedureTestData.BuiltAirport("BBB", responsibleArtcc: "ZOB", alnum: 200, procedures:
		[
			ProcedureTestData.BuiltProcedure("SHARED STAR", change: ProcedureChange.Changed, reportedPdfName: "X"),
		]);

		string content = Generate([aaa, bbb], ProcedureTestData.Dtpp("2609"));

		Assert.Contains("    - Also serves: BBB\r\n", content);
		Assert.DoesNotContain(content.Split("\r\n"), line => line == "- BBB");
	}

	[Fact]
	public void a_shared_chart_falls_back_to_the_first_airport_in_document_order_when_no_owner_matches()
	{
		ProcedureAirport aaa = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", alnum: 100, procedures:
		[
			ProcedureTestData.BuiltProcedure("SHARED STAR", change: ProcedureChange.Changed, reportedPdfName: "SHARED.PDF"),
		]);
		ProcedureAirport bbb = ProcedureTestData.BuiltAirport("BBB", responsibleArtcc: "ZOB", alnum: 200, procedures:
		[
			ProcedureTestData.BuiltProcedure("SHARED STAR", change: ProcedureChange.Changed, reportedPdfName: "SHARED.PDF"),
		]);

		string content = Generate([aaa, bbb], ProcedureTestData.Dtpp("2609"));

		Assert.Contains(
			"- AAA\r\n" +
			"  - [SHARED STAR](https://aeronav.faa.gov/d-tpp/2609/compare_pdf/SHARED_cmp.pdf)\r\n" +
			"    - Also serves: BBB\r\n",
			content);
		Assert.DoesNotContain(content.Split("\r\n"), line => line == "- BBB");
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
		ProcedureChangesWriteResult result = ProcedureChangesMarkdownWriter.Generate([], Settings(), ProcedureTestData.Dtpp("2609"), null);

		byte[] bytes = File.ReadAllBytes(result.FilePath!);
		bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
		Assert.False(hasBom);

		string content = File.ReadAllText(result.FilePath!);
		string withoutCrlf = content.Replace("\r\n", string.Empty, StringComparison.Ordinal);
		Assert.DoesNotContain('\n', withoutCrlf);
		Assert.DoesNotContain('\r', withoutCrlf);
	}
}
