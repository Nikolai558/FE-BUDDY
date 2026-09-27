using System.Text.Json.Nodes;

using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Domain.Procedures.Models;
using FeBuddy.Core.Infrastructure.Dtpp.Models;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Covers <see cref="ProceduresJsonWriter.Generate"/>: the always-present fields, every optional
/// field toggling on/off independently, nulls/blanks being omitted, <c>continuationUrls</c>,
/// <c>change</c>/<c>changeNote</c>/<c>compareUrl</c> conditions, deleted procedures being excluded,
/// a shared chart listed under every airport (no de-duplication), facility/airport ordering
/// matching the Markdown writer, and the producer mapping.
/// </summary>
public sealed class ProceduresJsonWriterTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_ProceduresJsonWriter_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_outputDirectory))
		{
			Directory.Delete(_outputDirectory, recursive: true);
		}
	}

	private ProcedureSettings Settings(params ProcedureJsonField[] fields) => new()
	{
		OutputDirectory = _outputDirectory,
		Facilities = ["ZOB"],
		JsonFields = fields,
	};

	private JsonObject GenerateRoot(IReadOnlyList<ProcedureAirport> airports, ProcedureSettings settings, DtppMetafileDataCollection dtpp)
	{
		ProceduresJsonWriteResult result = ProceduresJsonWriter.Generate(airports, settings, dtpp);
		return JsonNode.Parse(File.ReadAllText(result.FilePath!))!.AsObject();
	}

	private JsonObject GenerateFirstAirport(ProcedureAirport airport, ProcedureSettings settings, string cycle = "2609") =>
		GenerateRoot([airport], settings, ProcedureTestData.Dtpp(cycle))["airports"]!.AsArray()[0]!.AsObject();

	private JsonObject GenerateFirstProcedure(ProcedureAirport airport, ProcedureSettings settings, string cycle = "2609") =>
		GenerateFirstAirport(airport, settings, cycle)["procedures"]!.AsArray()[0]!.AsObject();

	[Fact]
	public void generate_rejects_null_arguments()
	{
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609");

		Assert.Throws<ArgumentNullException>(() => ProceduresJsonWriter.Generate(null!, Settings(), dtpp));
		Assert.Throws<ArgumentNullException>(() => ProceduresJsonWriter.Generate([], null!, dtpp));
		Assert.Throws<ArgumentNullException>(() => ProceduresJsonWriter.Generate([], Settings(), null!));
	}

	[Fact]
	public void generate_creates_the_publication_docs_folder_and_returns_its_path()
	{
		ProceduresJsonWriteResult result = ProceduresJsonWriter.Generate([], Settings(), ProcedureTestData.Dtpp("2609"));

		Assert.Equal(Path.Combine(_outputDirectory, "Publication_Docs", "Procedures.json"), result.FilePath);
		Assert.True(File.Exists(result.FilePath));
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void output_has_no_byte_order_mark()
	{
		ProceduresJsonWriteResult result = ProceduresJsonWriter.Generate([], Settings(), ProcedureTestData.Dtpp("2609"));

		byte[] bytes = File.ReadAllBytes(result.FilePath!);
		Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
	}

	// ---- cycle / effective date ----

	[Fact]
	public void cycle_and_effective_date_are_written_in_iso_format()
	{
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609", fromEffectiveUtc: new DateTime(2026, 9, 3, 9, 1, 0, DateTimeKind.Utc));

		JsonObject root = GenerateRoot([], Settings(), dtpp);

		Assert.Equal("2609", root["cycle"]!.GetValue<string>());
		Assert.Equal("2026-09-03", root["effectiveDate"]!.GetValue<string>());
	}

	[Fact]
	public void effective_date_is_omitted_when_the_metafile_has_none()
	{
		JsonObject root = GenerateRoot([], Settings(), ProcedureTestData.Dtpp("2609"));

		Assert.False(root.ContainsKey("effectiveDate"));
	}

	// ---- always-present fields ----

	[Fact]
	public void with_no_fields_selected_only_the_always_present_keys_are_written()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport(
			"AAA", icaoIdent: "KAAA", name: "AIRPORT NAME", city: "CITY", state: "OH",
			responsibleArtcc: "ZOB", airspaceClass: ProcedureAirspaceClass.C, isMilitary: true,
			procedures: [ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.New, procUid: 1, amdtNum: "1A", civil: "C")]);

		JsonObject airportObject = GenerateFirstAirport(airport, Settings());
		JsonObject procedureObject = airportObject["procedures"]!.AsArray()[0]!.AsObject();

		Assert.Equal(["airportId", "procedures"], airportObject.Select(kv => kv.Key));
		Assert.Equal(["name"], procedureObject.Select(kv => kv.Key));
		Assert.Equal("AAA", airportObject["airportId"]!.GetValue<string>());
		Assert.Equal("PROC", procedureObject["name"]!.GetValue<string>());
	}

	// ---- airport-level optional fields ----

	[Theory]
	[InlineData(ProcedureJsonField.IcaoId, "icaoId")]
	[InlineData(ProcedureJsonField.AirportName, "airportName")]
	[InlineData(ProcedureJsonField.City, "city")]
	[InlineData(ProcedureJsonField.State, "state")]
	[InlineData(ProcedureJsonField.ResponsibleArtcc, "responsibleArtcc")]
	public void an_airport_level_string_field_is_written_only_when_selected(ProcedureJsonField field, string jsonKey)
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport(
			"AAA", icaoIdent: "KAAA", name: "AIRPORT NAME", city: "CITY", state: "OH", responsibleArtcc: "ZOB",
			procedures: [ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.New)]);

		Assert.True(GenerateFirstAirport(airport, Settings(field)).ContainsKey(jsonKey));
		Assert.False(GenerateFirstAirport(airport, Settings()).ContainsKey(jsonKey));
	}

	[Fact]
	public void airspace_class_is_written_only_when_selected_and_not_none()
	{
		ProcedureAirport withClass = ProcedureTestData.BuiltAirport(
			"AAA", airspaceClass: ProcedureAirspaceClass.C, procedures: [ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.New)]);
		ProcedureAirport noClass = ProcedureTestData.BuiltAirport(
			"AAA", airspaceClass: ProcedureAirspaceClass.None, procedures: [ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.New)]);

		Assert.Equal("C", GenerateFirstAirport(withClass, Settings(ProcedureJsonField.AirspaceClass))["airspaceClass"]!.GetValue<string>());
		Assert.False(GenerateFirstAirport(withClass, Settings()).ContainsKey("airspaceClass"));
		Assert.False(GenerateFirstAirport(noClass, Settings(ProcedureJsonField.AirspaceClass)).ContainsKey("airspaceClass"));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void military_is_written_only_when_selected_regardless_of_its_value(bool isMilitary)
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport(
			"AAA", isMilitary: isMilitary, procedures: [ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.New)]);

		Assert.Equal(isMilitary, GenerateFirstAirport(airport, Settings(ProcedureJsonField.Military))["military"]!.GetValue<bool>());
		Assert.False(GenerateFirstAirport(airport, Settings()).ContainsKey("military"));
	}

	[Fact]
	public void a_null_or_blank_optional_airport_string_is_omitted_even_when_selected()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport(
			"AAA", icaoIdent: null, responsibleArtcc: "", procedures: [ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.New)]);

		JsonObject airportObject = GenerateFirstAirport(airport, Settings(ProcedureJsonField.IcaoId, ProcedureJsonField.ResponsibleArtcc));

		Assert.False(airportObject.ContainsKey("icaoId"));
		Assert.False(airportObject.ContainsKey("responsibleArtcc"));
	}

	// ---- procedure-level optional fields ----

	[Fact]
	public void chart_type_is_written_only_when_selected()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("PROC", chartCode: "STR", change: ProcedureChange.New)]);

		Assert.Equal("STR", GenerateFirstProcedure(airport, Settings(ProcedureJsonField.ChartType))["chartType"]!.GetValue<string>());
		Assert.False(GenerateFirstProcedure(airport, Settings()).ContainsKey("chartType"));
	}

	[Fact]
	public void amendment_and_computer_code_are_written_only_when_selected_and_present()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.New, amdtNum: "1A", computerCode: "JALEX3.JALEX")]);

		JsonObject withFields = GenerateFirstProcedure(airport, Settings(ProcedureJsonField.Amendment, ProcedureJsonField.ComputerCode));
		Assert.Equal("1A", withFields["amendment"]!.GetValue<string>());
		Assert.Equal("JALEX3.JALEX", withFields["computerCode"]!.GetValue<string>());

		JsonObject withoutFields = GenerateFirstProcedure(airport, Settings());
		Assert.False(withoutFields.ContainsKey("amendment"));
		Assert.False(withoutFields.ContainsKey("computerCode"));
	}

	[Fact]
	public void amendment_date_is_written_in_iso_format_only_when_selected_and_present()
	{
		ProcedureAirport withDate = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.New, amdtDate: new DateOnly(2019, 12, 5))]);
		ProcedureAirport withoutDate = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.New, amdtDate: null)]);

		Assert.Equal("2019-12-05", GenerateFirstProcedure(withDate, Settings(ProcedureJsonField.AmendmentDate))["amendmentDate"]!.GetValue<string>());
		Assert.False(GenerateFirstProcedure(withDate, Settings()).ContainsKey("amendmentDate"));
		Assert.False(GenerateFirstProcedure(withoutDate, Settings(ProcedureJsonField.AmendmentDate)).ContainsKey("amendmentDate"));
	}

	[Fact]
	public void procedure_uid_is_written_only_when_selected_and_present()
	{
		ProcedureAirport withUid = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.New, procUid: 42)]);
		ProcedureAirport withoutUid = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.New, procUid: null)]);

		Assert.Equal(42, GenerateFirstProcedure(withUid, Settings(ProcedureJsonField.ProcedureUid))["procedureUid"]!.GetValue<int>());
		Assert.False(GenerateFirstProcedure(withoutUid, Settings(ProcedureJsonField.ProcedureUid)).ContainsKey("procedureUid"));
	}

	// ---- chartUrl / continuationUrls ----

	[Fact]
	public void chart_url_is_written_only_when_selected_and_a_current_pdf_exists()
	{
		ProcedureAirport withPdf = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.New, currentPdfName: "00100A.PDF")]);
		ProcedureAirport withoutPdf = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.ReAdded, currentPdfName: null)]);

		Assert.Equal(
			"https://aeronav.faa.gov/d-tpp/2609/00100A.PDF",
			GenerateFirstProcedure(withPdf, Settings(ProcedureJsonField.ChartUrl))["chartUrl"]!.GetValue<string>());
		Assert.False(GenerateFirstProcedure(withPdf, Settings()).ContainsKey("chartUrl"));
		Assert.False(GenerateFirstProcedure(withoutPdf, Settings(ProcedureJsonField.ChartUrl)).ContainsKey("chartUrl"));
	}

	[Fact]
	public void continuation_urls_list_only_the_non_placeholder_continuation_pages()
	{
		ProcedurePage[] pages =
		[
			new("GRUUB ONE (RNAV)", "00100A.PDF", null),
			new("GRUUB ONE (RNAV), CONT.1", "00100B.PDF", null),
			new("GRUUB ONE (RNAV), CONT.2", "DELETED_JOB.PDF", null),
		];
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("GRUUB ONE (RNAV)", change: ProcedureChange.New, currentPdfName: "00100A.PDF", pages: pages)]);

		JsonObject procedure = GenerateFirstProcedure(airport, Settings(ProcedureJsonField.ChartUrl));

		JsonArray continuationUrls = procedure["continuationUrls"]!.AsArray();
		string url = Assert.Single(continuationUrls)!.GetValue<string>();
		Assert.Equal("https://aeronav.faa.gov/d-tpp/2609/00100B.PDF", url);
	}

	[Fact]
	public void continuation_urls_are_omitted_when_there_are_no_continuation_pages()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.New, currentPdfName: "00100A.PDF")]);

		JsonObject procedure = GenerateFirstProcedure(airport, Settings(ProcedureJsonField.ChartUrl));

		Assert.False(procedure.ContainsKey("continuationUrls"));
	}

	// ---- change / changeNote / compareUrl ----

	[Fact]
	public void change_is_written_only_when_the_procedure_actually_changed()
	{
		ProcedureAirport changed = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.New)]);
		ProcedureAirport unchanged = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.None)]);

		Assert.Equal("New", GenerateFirstProcedure(changed, Settings(ProcedureJsonField.Change))["change"]!.GetValue<string>());
		Assert.False(GenerateFirstProcedure(unchanged, Settings(ProcedureJsonField.Change)).ContainsKey("change"));
	}

	[Fact]
	public void a_readded_procedure_is_reported_as_changed_with_a_change_note()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.ReAdded, currentPdfName: "00100A.PDF")]);

		JsonObject procedure = GenerateFirstProcedure(airport, Settings(ProcedureJsonField.Change));

		Assert.Equal("Changed", procedure["change"]!.GetValue<string>());
		Assert.Equal("Deleted and re-added in this cycle's d-TPP metafile", procedure["changeNote"]!.GetValue<string>());
	}

	[Fact]
	public void compare_url_is_written_only_for_changed_not_for_readded()
	{
		ProcedureAirport changed = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.Changed, reportedPdfName: "00100A.PDF")]);
		ProcedureAirport readded = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.ReAdded, reportedPdfName: "00100A.PDF", currentPdfName: "00100A.PDF")]);

		Assert.Equal(
			"https://aeronav.faa.gov/d-tpp/2609/compare_pdf/00100A_cmp.pdf",
			GenerateFirstProcedure(changed, Settings(ProcedureJsonField.CompareUrl))["compareUrl"]!.GetValue<string>());
		Assert.False(GenerateFirstProcedure(readded, Settings(ProcedureJsonField.CompareUrl)).ContainsKey("compareUrl"));
	}

	// ---- deleted procedures ----

	[Fact]
	public void deleted_procedures_are_excluded_entirely()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures:
		[
			ProcedureTestData.BuiltProcedure("KEPT", change: ProcedureChange.New),
			ProcedureTestData.BuiltProcedure("GONE", change: ProcedureChange.Deleted, currentPdfName: null),
		]);

		JsonObject airportObject = GenerateFirstAirport(airport, Settings());

		JsonArray procedures = airportObject["procedures"]!.AsArray();
		Assert.Equal(["KEPT"], procedures.Select(p => p!["name"]!.GetValue<string>()));
	}

	[Fact]
	public void an_airport_with_every_procedure_deleted_is_excluded_entirely()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("GONE", change: ProcedureChange.Deleted, currentPdfName: null)]);

		JsonObject root = GenerateRoot([airport], Settings(), ProcedureTestData.Dtpp("2609"));

		Assert.Empty(root["airports"]!.AsArray());
	}

	// ---- shared chart, no de-duplication ----

	[Fact]
	public void a_shared_chart_is_listed_under_every_airport_with_no_deduplication()
	{
		ProcedureAirport aaa = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
			[ProcedureTestData.BuiltProcedure("SHARED", change: ProcedureChange.New, currentPdfName: "SHARED.PDF")]);
		ProcedureAirport bbb = ProcedureTestData.BuiltAirport("BBB", responsibleArtcc: "ZOB", procedures:
			[ProcedureTestData.BuiltProcedure("SHARED", change: ProcedureChange.New, currentPdfName: "SHARED.PDF")]);

		JsonObject root = GenerateRoot([aaa, bbb], Settings(), ProcedureTestData.Dtpp("2609"));

		JsonArray airports = root["airports"]!.AsArray();
		Assert.Equal(2, airports.Count);
		Assert.All(airports, a => Assert.Equal("SHARED", a!["procedures"]!.AsArray()[0]!["name"]!.GetValue<string>()));
	}

	// ---- ordering ----

	[Fact]
	public void airports_are_ordered_the_same_way_as_the_markdown_writer()
	{
		ProcedureSettings settings = Settings() with { PrimaryFacility = "ZOB" };

		ProcedureAirport zny = ProcedureTestData.BuiltAirport("CCC", responsibleArtcc: "ZNY", procedures:
			[ProcedureTestData.BuiltProcedure("ONE", change: ProcedureChange.New)]);
		ProcedureAirport zobB = ProcedureTestData.BuiltAirport("BBB", responsibleArtcc: "ZOB", procedures:
			[ProcedureTestData.BuiltProcedure("TWO", change: ProcedureChange.New)]);
		ProcedureAirport zobA = ProcedureTestData.BuiltAirport("AAA", responsibleArtcc: "ZOB", procedures:
			[ProcedureTestData.BuiltProcedure("THREE", change: ProcedureChange.New)]);
		ProcedureAirport other = ProcedureTestData.BuiltAirport("DDD", responsibleArtcc: null, procedures:
			[ProcedureTestData.BuiltProcedure("FOUR", change: ProcedureChange.New)]);

		JsonObject root = GenerateRoot([zny, zobB, zobA, other], settings, ProcedureTestData.Dtpp("2609"));

		// Primary facility (ZOB) first, its airports ordinal ("AAA" before "BBB"), then the other
		// facilities alphabetically (ZNY), then "Other" (no ARTCC) last.
		Assert.Equal(
			["AAA", "BBB", "CCC", "DDD"],
			root["airports"]!.AsArray().Select(a => a!["airportId"]!.GetValue<string>()));
	}

	// ---- producer mapping ----

	[Theory]
	[InlineData("C", "FAA")]
	[InlineData("D", "FAA (joint use)")]
	[InlineData("N", "NGA")]
	[InlineData("H", "NGA (high altitude)")]
	public void producer_maps_from_the_civil_code(string civil, string expectedProducer)
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.New, civil: civil)]);

		Assert.Equal(expectedProducer, GenerateFirstProcedure(airport, Settings(ProcedureJsonField.Producer))["producer"]!.GetValue<string>());
	}

	[Theory]
	[InlineData(null)]
	[InlineData("X")]
	public void producer_is_omitted_for_an_unknown_or_missing_civil_code(string? civil)
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport("AAA", procedures:
			[ProcedureTestData.BuiltProcedure("PROC", change: ProcedureChange.New, civil: civil)]);

		Assert.False(GenerateFirstProcedure(airport, Settings(ProcedureJsonField.Producer)).ContainsKey("producer"));
	}

	// ---- end-to-end with the settings parser's default field set ----

	[Fact]
	public void the_settings_parsers_default_field_set_writes_exactly_the_documented_optional_fields()
	{
		ProcedureAirport airport = ProcedureTestData.BuiltAirport(
			"AAA", icaoIdent: "KAAA", name: "AIRPORT NAME", city: "CITY", state: "OH",
			responsibleArtcc: "ZOB", airspaceClass: ProcedureAirspaceClass.C, isMilitary: true,
			procedures:
			[
				ProcedureTestData.BuiltProcedure(
					"PROC", change: ProcedureChange.Changed, reportedPdfName: "00100A.PDF", currentPdfName: "00100A.PDF",
					procUid: 1, amdtNum: "1A", amdtDate: new DateOnly(2020, 1, 1), computerCode: "TEST.TEST", civil: "C"),
			]);

		ProcedureSettingsParseResult parsed = ProcedureSettingsParser.Parse(new Dictionary<string, string>
		{
			["OutputDirectory"] = _outputDirectory,
			["Facilities"] = "ZOB",
		});

		JsonObject airportObject = GenerateFirstAirport(airport, parsed.Settings);
		JsonObject procedureObject = airportObject["procedures"]!.AsArray()[0]!.AsObject();

		Assert.True(airportObject.ContainsKey("icaoId"));
		Assert.True(airportObject.ContainsKey("airportName"));
		Assert.True(airportObject.ContainsKey("responsibleArtcc"));
		Assert.True(airportObject.ContainsKey("airspaceClass"));
		Assert.False(airportObject.ContainsKey("city"));
		Assert.False(airportObject.ContainsKey("state"));
		Assert.False(airportObject.ContainsKey("military"));

		Assert.True(procedureObject.ContainsKey("chartType"));
		Assert.True(procedureObject.ContainsKey("chartUrl"));
		Assert.True(procedureObject.ContainsKey("change"));
		Assert.True(procedureObject.ContainsKey("compareUrl"));
		Assert.False(procedureObject.ContainsKey("amendment"));
		Assert.False(procedureObject.ContainsKey("amendmentDate"));
		Assert.False(procedureObject.ContainsKey("procedureUid"));
		Assert.False(procedureObject.ContainsKey("computerCode"));
		Assert.False(procedureObject.ContainsKey("producer"));
	}
}
