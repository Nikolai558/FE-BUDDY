using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Runs the whole Procedures pipeline (<see cref="ProcedureService.Run"/>): the missing-metafile
/// advisory (nothing written, no throw), the missing-previous-metafile info message, where the
/// documents land, that only the chosen documents are written, the reported counts, that settings
/// and builder errors propagate, and the FAA Chart Recall alias file: where it lands, that it covers
/// every airport whatever the documents select, and when it is not written.
/// </summary>
public sealed class ProcedureServiceTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_ProcedureService_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_outputDirectory))
		{
			Directory.Delete(_outputDirectory, recursive: true);
		}
	}

	private Dictionary<string, string> Settings(params (string Key, string Value)[] overrides)
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = _outputDirectory,
			["Facilities"] = "ZOB",
		};

		foreach ((string key, string value) in overrides)
		{
			settings[key] = value;
		}

		return settings;
	}

	/// <summary>One airport with a new and a deleted procedure, plus an unchanged airport, both in ZOB.</summary>
	private static (NasrCsvDataCollection Nasr, DtppMetafileDataCollection Dtpp) TwoAirportScenario()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr([ProcedureTestData.AptBaseRow("AAA"), ProcedureTestData.AptBaseRow("BBB")]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA", alnum: 100), ProcedureTestData.AirportRow("BBB", alnum: 200)],
			records:
			[
				ProcedureTestData.RecordRow("AAA", 10, "IAP", "ILS RWY 1", "00100ILS1.PDF", userAction: "A"),
				ProcedureTestData.RecordRow("AAA", 20, "IAP", "VOR RWY 2", "DELETED_JOB.PDF", userAction: "D"),
				ProcedureTestData.RecordRow("BBB", 10, "IAP", "RNAV RWY 3", "00200RNAV3.PDF"),
			]);

		return (nasr, dtpp);
	}

	[Fact]
	public void run_rejects_null_arguments()
	{
		(NasrCsvDataCollection nasr, DtppMetafileDataCollection dtpp) = TwoAirportScenario();

		Assert.Throws<ArgumentNullException>(() => ProcedureService.Run(null!, dtpp, null, Settings()));
		Assert.Throws<ArgumentNullException>(() => ProcedureService.Run(nasr, dtpp, null, null!));
	}

	[Fact]
	public void a_null_metafile_produces_an_advisory_warning_and_writes_nothing()
	{
		ProcedureServiceResult result = ProcedureService.Run(new NasrCsvDataCollection(), null, null, Settings());

		Assert.Empty(result.FilesWritten);
		Assert.Equal(0, result.AirportCount);
		Assert.Contains(result.Messages, m => m.IsAdvisory && m.Text.Contains("has not published the d-TPP metafile", StringComparison.Ordinal));
		Assert.False(Directory.Exists(_outputDirectory));
	}

	[Fact]
	public void a_missing_previous_metafile_with_a_deleted_procedure_adds_an_info_message()
	{
		(NasrCsvDataCollection nasr, DtppMetafileDataCollection dtpp) = TwoAirportScenario();

		ProcedureServiceResult result = ProcedureService.Run(nasr, dtpp, null, Settings());

		Assert.Contains(result.Messages, m =>
			m.Text.Contains("previous cycle's d-TPP Metafile is not available", StringComparison.Ordinal));
	}

	[Fact]
	public void a_present_previous_metafile_does_not_add_the_info_message()
	{
		(NasrCsvDataCollection nasr, DtppMetafileDataCollection dtpp) = TwoAirportScenario();
		DtppMetafileDataCollection previousDtpp = ProcedureTestData.Dtpp("2608",
			records: [ProcedureTestData.RecordRow("AAA", 20, "IAP", "VOR RWY 2", "00100VOR2.PDF")]);

		ProcedureServiceResult result = ProcedureService.Run(nasr, dtpp, previousDtpp, Settings());

		Assert.DoesNotContain(result.Messages, m =>
			m.Text.Contains("previous cycle's d-TPP Metafile is not available", StringComparison.Ordinal));
	}

	[Fact]
	public void files_land_in_the_publication_docs_folder()
	{
		(NasrCsvDataCollection nasr, DtppMetafileDataCollection dtpp) = TwoAirportScenario();

		ProcedureServiceResult result = ProcedureService.Run(nasr, dtpp, null, Settings());

		string publicationDocs = Path.Combine(_outputDirectory, "Publication_Docs");
		Assert.Equal(2, result.FilesWritten.Count);
		Assert.All(result.FilesWritten, f => Assert.Equal(publicationDocs, Path.GetDirectoryName(f)));
		Assert.True(File.Exists(Path.Combine(publicationDocs, "Procedure_Changes.md")));
		Assert.True(File.Exists(Path.Combine(publicationDocs, "Procedures.json")));
	}

	[Fact]
	public void only_the_chosen_documents_are_written()
	{
		(NasrCsvDataCollection nasr, DtppMetafileDataCollection dtpp) = TwoAirportScenario();

		ProcedureServiceResult markdownOnly = ProcedureService.Run(nasr, dtpp, null, Settings(("GenerateProceduresJson", "N")));
		Assert.Equal(["Procedure_Changes.md"], markdownOnly.FilesWritten.Select(Path.GetFileName));

		Directory.Delete(_outputDirectory, recursive: true);

		ProcedureServiceResult jsonOnly = ProcedureService.Run(nasr, dtpp, null, Settings(("GenerateChangesDocument", "N")));
		Assert.Equal(["Procedures.json"], jsonOnly.FilesWritten.Select(Path.GetFileName));
	}

	[Fact]
	public void the_result_counts_airports_procedures_and_each_kind_of_change()
	{
		(NasrCsvDataCollection nasr, DtppMetafileDataCollection dtpp) = TwoAirportScenario();

		ProcedureServiceResult result = ProcedureService.Run(nasr, dtpp, null, Settings());

		Assert.Equal(2, result.AirportCount);
		Assert.Equal(3, result.ProcedureCount);
		Assert.Equal(1, result.NewCount);
		Assert.Equal(0, result.ChangedCount);
		Assert.Equal(1, result.DeletedCount);
		Assert.Equal(0, result.ReAddedCount);
	}

	[Fact]
	public void a_changed_procedure_increments_the_changed_count()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr([ProcedureTestData.AptBaseRow("AAA")]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records: [ProcedureTestData.RecordRow("AAA", 10, "IAP", "ILS RWY 1", "00100ILS1.PDF", userAction: "C")]);

		ProcedureServiceResult result = ProcedureService.Run(nasr, dtpp, null, Settings());

		Assert.Equal(1, result.ChangedCount);
		Assert.Equal(0, result.NewCount);
		Assert.Equal(0, result.DeletedCount);
		Assert.Equal(0, result.ReAddedCount);
	}

	[Fact]
	public void a_readded_procedure_counts_as_both_readded_and_changed()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr([ProcedureTestData.AptBaseRow("AAA")]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			airports: [ProcedureTestData.AirportRow("AAA")],
			records:
			[
				ProcedureTestData.RecordRow("AAA", 10, "IAP", "ILS RWY 1", "DELETED_JOB.PDF", userAction: "D"),
				ProcedureTestData.RecordRow("AAA", 20, "IAP", "ILS RWY 1", "NEW.PDF", userAction: "A"),
			]);

		ProcedureServiceResult result = ProcedureService.Run(nasr, dtpp, null, Settings());

		Assert.Equal(1, result.ReAddedCount);
		Assert.Equal(1, result.ChangedCount);
	}

	[Fact]
	public void settings_errors_propagate_from_run()
	{
		(NasrCsvDataCollection nasr, DtppMetafileDataCollection dtpp) = TwoAirportScenario();

		Assert.Throws<ArgumentException>(() => ProcedureService.Run(nasr, dtpp, null, Settings(("Facilities", ""))));
	}

	[Fact]
	public void missing_nasr_data_propagates_from_the_builder_when_a_metafile_is_present()
	{
		(_, DtppMetafileDataCollection dtpp) = TwoAirportScenario();

		Assert.Throws<InvalidOperationException>(() => ProcedureService.Run(new NasrCsvDataCollection(), dtpp, null, Settings()));
	}

	// ---- the FAA Chart Recall alias file ----

	[Fact]
	public void the_alias_file_is_written_to_the_aliases_folder_by_default()
	{
		(NasrCsvDataCollection nasr, DtppMetafileDataCollection dtpp) = TwoAirportScenario();

		ProcedureServiceResult result = ProcedureService.Run(nasr, dtpp, null, Settings());

		string expectedPath = Path.Combine(_outputDirectory, "Aliases", "Faa_Chart_Recall.txt");
		Assert.Equal(expectedPath, result.AliasFilePath);
		Assert.Equal([".aaaI1c", ".bbbR3c"], File.ReadAllLines(expectedPath).Select(line => line.Split(' ')[0]));
		Assert.Equal(2, result.AliasCommandCount);
		Assert.Equal(2, result.AliasAirportCount);
		Assert.Contains(result.Messages, m => m.Text.StartsWith("Faa_Chart_Recall.txt: 2 command(s) for 2 airport(s)", StringComparison.Ordinal));
	}

	[Fact]
	public void the_alias_file_covers_every_airport_whatever_the_documents_select()
	{
		(NasrCsvDataCollection nasr, DtppMetafileDataCollection dtpp) = TwoAirportScenario();

		// Only AAA is picked for the documents; the alias file still covers BBB.
		ProcedureServiceResult result = ProcedureService.Run(nasr, dtpp, null, Settings(("Facilities", ""), ("Airports", "AAA")));

		Assert.Equal(1, result.AirportCount);
		Assert.Equal(2, result.AliasAirportCount);
	}

	[Fact]
	public void an_alias_only_run_needs_no_selection_and_no_nasr_airport_data()
	{
		(_, DtppMetafileDataCollection dtpp) = TwoAirportScenario();

		ProcedureServiceResult result = ProcedureService.Run(new NasrCsvDataCollection(), dtpp, null, Settings(
			("Facilities", ""), ("GenerateChangesDocument", "N"), ("GenerateProceduresJson", "N")));

		Assert.Empty(result.FilesWritten);
		Assert.Equal(0, result.AirportCount);
		Assert.NotNull(result.AliasFilePath);
		Assert.False(Directory.Exists(Path.Combine(_outputDirectory, "Publication_Docs")));
	}

	[Fact]
	public void an_alias_file_marked_for_vnas_still_goes_in_the_aliases_folder()
	{
		(NasrCsvDataCollection nasr, DtppMetafileDataCollection dtpp) = TwoAirportScenario();

		ProcedureServiceResult result = ProcedureService.Run(nasr, dtpp, null, Settings(("UploadToVnas", "Faa_Chart_Recall.txt")));

		Assert.Equal(Path.Combine(_outputDirectory, "Aliases", "Faa_Chart_Recall.txt"), result.AliasFilePath);
	}

	[Fact]
	public void turning_the_alias_file_off_writes_no_alias_file()
	{
		(NasrCsvDataCollection nasr, DtppMetafileDataCollection dtpp) = TwoAirportScenario();

		ProcedureServiceResult result = ProcedureService.Run(nasr, dtpp, null, Settings(("GenerateAliasFile", "N")));

		Assert.Null(result.AliasFilePath);
		Assert.Equal(0, result.AliasCommandCount);
		Assert.Equal(0, result.AliasAirportCount);
		Assert.False(Directory.Exists(Path.Combine(_outputDirectory, "Aliases")));
	}

	[Fact]
	public void a_metafile_with_no_current_chart_writes_no_alias_file()
	{
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2609",
			records: [ProcedureTestData.RecordRow("AAA", 10, "IAP", "VOR RWY 2", "DELETED_JOB.PDF", userAction: "D")]);

		ProcedureServiceResult result = ProcedureService.Run(new NasrCsvDataCollection(), dtpp, null, Settings(
			("GenerateChangesDocument", "N"), ("GenerateProceduresJson", "N")));

		Assert.Null(result.AliasFilePath);
		Assert.Equal(0, result.AliasAirportCount);
	}

	[Fact]
	public void a_null_metafile_says_the_alias_file_was_not_written_either()
	{
		ProcedureServiceResult result = ProcedureService.Run(new NasrCsvDataCollection(), null, null, Settings());

		Assert.Null(result.AliasFilePath);
		Assert.Contains(result.Messages, m => m.IsAdvisory && m.Text.Contains("FAA Chart Recall alias file", StringComparison.Ordinal));
	}

	[Fact]
	public void renamed_documents_and_the_alias_file_are_written_under_their_new_names()
	{
		(NasrCsvDataCollection nasr, DtppMetafileDataCollection dtpp) = TwoAirportScenario();

		OutputFileNames fileNames = new(new Dictionary<string, string>
		{
			["Procedure_Changes.md"] = "ZOB Changes",
			["Procedures.json"] = "ZOB Procedures",
			["Faa_Chart_Recall.txt"] = "ZOB Chart Recall",
		});

		ProcedureServiceResult result = ProcedureService.Run(nasr, dtpp, null, Settings(), fileNames);

		string publicationDocs = Path.Combine(_outputDirectory, "Publication_Docs");
		Assert.Equal(
			[Path.Combine(publicationDocs, "ZOB Changes.md"), Path.Combine(publicationDocs, "ZOB Procedures.json")],
			result.FilesWritten);
		Assert.All(result.FilesWritten, f => Assert.True(File.Exists(f)));
		Assert.False(File.Exists(Path.Combine(publicationDocs, "Procedure_Changes.md")));
		Assert.False(File.Exists(Path.Combine(publicationDocs, "Procedures.json")));

		Assert.Equal(Path.Combine(_outputDirectory, "Aliases", "ZOB Chart Recall.txt"), result.AliasFilePath);
		Assert.True(File.Exists(result.AliasFilePath!));
		Assert.False(File.Exists(Path.Combine(_outputDirectory, "Aliases", "Faa_Chart_Recall.txt")));

		// The summary names the file as it was written.
		Assert.Contains(result.Messages, m => m.Text.StartsWith("ZOB Chart Recall.txt:", StringComparison.Ordinal));
	}
}
