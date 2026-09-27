using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Domain.Procedures;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Covers <see cref="ProcedureSettingsParser"/>: every key, the documented defaults, the
/// "both documents off" and "no inclusion source" guards, <c>AirportProcedures</c> parsing and
/// validation, unknown <c>ChartTypes</c>/<c>JsonFields</c> entries, and that Procedures has no
/// GeoJSON/alias/<c>feb.*</c> settings of its own.
/// </summary>
public sealed class ProcedureSettingsParserTests
{
	private static Dictionary<string, string> MinimalValidSettings() => new(StringComparer.OrdinalIgnoreCase)
	{
		["OutputDirectory"] = @"C:\Output",
		["Facilities"] = "ZOB",
	};

	[Fact]
	public void parse_rejects_a_null_argument() =>
		Assert.Throws<ArgumentNullException>(() => ProcedureSettingsParser.Parse(null!));

	[Fact]
	public void missing_output_directory_throws() =>
		Assert.Throws<ArgumentException>(() => ProcedureSettingsParser.Parse(new Dictionary<string, string> { ["Facilities"] = "ZOB" }));

	// ---- defaults ----

	[Fact]
	public void a_minimal_settings_block_produces_no_messages_and_the_documented_defaults()
	{
		ProcedureSettingsParseResult result = ProcedureSettingsParser.Parse(MinimalValidSettings());

		Assert.Empty(result.Messages);
		Assert.True(result.Settings.GenerateChangesDocument);
		Assert.True(result.Settings.GenerateProceduresJson);
		Assert.Equal(["ZOB"], result.Settings.Facilities);
		Assert.Null(result.Settings.PrimaryFacility);
		Assert.False(result.Settings.IncludeRoiAirports);
		Assert.Null(result.Settings.Roi);
		Assert.Empty(result.Settings.Airports);
		Assert.Empty(result.Settings.Procedures);
		Assert.Empty(result.Settings.AirportProcedures);
		Assert.Equal(ProcedureChartTypes.Default, result.Settings.ChartTypes);
		Assert.Equal(
		[
			ProcedureJsonField.IcaoId,
			ProcedureJsonField.AirportName,
			ProcedureJsonField.ResponsibleArtcc,
			ProcedureJsonField.AirspaceClass,
			ProcedureJsonField.ChartType,
			ProcedureJsonField.ChartUrl,
			ProcedureJsonField.Change,
			ProcedureJsonField.CompareUrl,
		],
			result.Settings.JsonFields);
	}

	// ---- GenerateChangesDocument / GenerateProceduresJson / GenerateAliasFile ----

	[Fact]
	public void turning_off_all_three_generate_flags_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateChangesDocument"] = "N";
		settings["GenerateProceduresJson"] = "N";
		settings["GenerateAliasFile"] = "N";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => ProcedureSettingsParser.Parse(settings));
		Assert.Contains("GenerateChangesDocument", ex.Message);
		Assert.Contains("GenerateProceduresJson", ex.Message);
		Assert.Contains("GenerateAliasFile", ex.Message);
	}

	[Fact]
	public void turning_off_both_documents_with_the_alias_file_on_parses_fine()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateChangesDocument"] = "N";
		settings["GenerateProceduresJson"] = "N";

		ProcedureSettings parsed = ProcedureSettingsParser.Parse(settings).Settings;

		Assert.False(parsed.GenerateChangesDocument);
		Assert.False(parsed.GenerateProceduresJson);
		Assert.True(parsed.GenerateAliasFile);
	}

	[Theory]
	[InlineData("N", "Y")]
	[InlineData("Y", "N")]
	public void turning_off_only_one_generate_flag_is_fine(string generateChanges, string generateJson)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateChangesDocument"] = generateChanges;
		settings["GenerateProceduresJson"] = generateJson;

		ProcedureSettings parsed = ProcedureSettingsParser.Parse(settings).Settings;

		Assert.Equal(generateChanges == "Y", parsed.GenerateChangesDocument);
		Assert.Equal(generateJson == "Y", parsed.GenerateProceduresJson);
	}

	// ---- Facilities / PrimaryFacility ----

	[Fact]
	public void facilities_are_trimmed_upper_cased_and_deduplicated()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["Facilities"] = " zob , ZOB, zny ";

		ProcedureSettings parsed = ProcedureSettingsParser.Parse(settings).Settings;

		Assert.Equal(["ZOB", "ZNY"], parsed.Facilities);
	}

	[Fact]
	public void primary_facility_is_upper_cased()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["PrimaryFacility"] = "zob";

		ProcedureSettings parsed = ProcedureSettingsParser.Parse(settings).Settings;

		Assert.Equal("ZOB", parsed.PrimaryFacility);
	}

	// ---- IncludeRoiAirports / Roi ----

	[Fact]
	public void include_roi_airports_without_a_region_of_interest_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeRoiAirports"] = "Y";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => ProcedureSettingsParser.Parse(settings));
		Assert.Contains("IncludeRoiAirports", ex.Message);
	}

	[Fact]
	public void include_roi_airports_with_a_region_of_interest_parses()
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = @"C:\Output",
			["IncludeRoiAirports"] = "Y",
			["FilterByRoi"] = "Y",
			["RoiSwLat"] = "40.0",
			["RoiSwLon"] = "-89.0",
			["RoiNeLat"] = "43.0",
			["RoiNeLon"] = "-86.0",
		};

		ProcedureSettings parsed = ProcedureSettingsParser.Parse(settings).Settings;

		Assert.True(parsed.IncludeRoiAirports);
		Assert.NotNull(parsed.Roi);
		Assert.Equal(40.0, parsed.Roi!.SwLat);
	}

	// ---- inclusion source guard ----

	[Fact]
	public void no_inclusion_source_throws()
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase) { ["OutputDirectory"] = @"C:\Output" };

		ArgumentException ex = Assert.Throws<ArgumentException>(() => ProcedureSettingsParser.Parse(settings));
		Assert.Contains("inclusion source", ex.Message);
	}

	[Fact]
	public void airports_alone_satisfies_the_inclusion_source_requirement()
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = @"C:\Output",
			["Airports"] = "PIT",
		};

		ProcedureSettingsParseResult result = ProcedureSettingsParser.Parse(settings);

		Assert.Equal(["PIT"], result.Settings.Airports);
	}

	[Fact]
	public void procedures_alone_satisfies_the_inclusion_source_requirement()
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = @"C:\Output",
			["Procedures"] = "ILS OR LOC RWY 28C",
		};

		ProcedureSettingsParseResult result = ProcedureSettingsParser.Parse(settings);

		Assert.Equal(["ILS OR LOC RWY 28C"], result.Settings.Procedures);
	}

	[Fact]
	public void airport_procedures_alone_satisfies_the_inclusion_source_requirement()
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = @"C:\Output",
			["AirportProcedures"] = "PIT|ILS OR LOC RWY 28C",
		};

		ProcedureSettingsParseResult result = ProcedureSettingsParser.Parse(settings);

		ProcedureAirportPick pick = Assert.Single(result.Settings.AirportProcedures);
		Assert.Equal("PIT", pick.Airport);
		Assert.Equal("ILS OR LOC RWY 28C", pick.ProcedureName);
	}

	// ---- AirportProcedures parsing ----

	[Theory]
	[InlineData("PITILS OR LOC RWY 28C")] // no '|'
	[InlineData("PIT|ILS OR LOC RWY 28C|EXTRA")] // two '|'
	[InlineData("|ILS OR LOC RWY 28C")] // empty airport side
	[InlineData("PIT|")] // empty procedure side
	public void a_malformed_airport_procedures_entry_throws_naming_the_entry(string entry)
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["AirportProcedures"] = entry;

		ArgumentException ex = Assert.Throws<ArgumentException>(() => ProcedureSettingsParser.Parse(settings));
		Assert.Contains(entry, ex.Message);
	}

	[Fact]
	public void airport_procedures_entries_are_trimmed()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["AirportProcedures"] = " PIT | ILS OR LOC RWY 28C ";

		ProcedureAirportPick pick = Assert.Single(ProcedureSettingsParser.Parse(settings).Settings.AirportProcedures);

		Assert.Equal("PIT", pick.Airport);
		Assert.Equal("ILS OR LOC RWY 28C", pick.ProcedureName);
	}

	// ---- ChartTypes ----

	[Fact]
	public void chart_types_default_when_absent()
	{
		ProcedureSettings parsed = ProcedureSettingsParser.Parse(MinimalValidSettings()).Settings;

		Assert.Equal(ProcedureChartTypes.Default, parsed.ChartTypes);
	}

	[Fact]
	public void chart_types_are_upper_cased_and_deduplicated()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["ChartTypes"] = " iap, IAP, str ";

		ProcedureSettings parsed = ProcedureSettingsParser.Parse(settings).Settings;

		Assert.Equal(["IAP", "STR"], parsed.ChartTypes);
	}

	[Fact]
	public void an_unknown_chart_type_warns_but_is_still_used()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["ChartTypes"] = "IAP,BOGUS";

		ProcedureSettingsParseResult result = ProcedureSettingsParser.Parse(settings);

		string warning = Assert.Single(result.Messages.WarningTexts());
		Assert.Contains("BOGUS", warning);
		Assert.Contains("BOGUS", result.Settings.ChartTypes);
	}

	// ---- JsonFields ----

	[Fact]
	public void json_fields_default_when_absent()
	{
		ProcedureSettings parsed = ProcedureSettingsParser.Parse(MinimalValidSettings()).Settings;

		Assert.Equal(8, parsed.JsonFields.Count);
	}

	[Fact]
	public void json_fields_parse_known_names_and_dedupe()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["JsonFields"] = "icaoId,city,icaoId";

		ProcedureSettings parsed = ProcedureSettingsParser.Parse(settings).Settings;

		Assert.Equal([ProcedureJsonField.IcaoId, ProcedureJsonField.City], parsed.JsonFields);
	}

	[Fact]
	public void an_unknown_json_field_warns_and_is_ignored()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["JsonFields"] = "icaoId,bogus";

		ProcedureSettingsParseResult result = ProcedureSettingsParser.Parse(settings);

		string warning = Assert.Single(result.Messages.WarningTexts());
		Assert.Contains("bogus", warning);
		Assert.Equal([ProcedureJsonField.IcaoId], result.Settings.JsonFields);
	}

	// ---- no GeoJSON / feb.* properties ----

	[Fact]
	public void include_feb_custom_properties_warns_that_procedures_has_none()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["IncludeFebCustomProperties"] = "Y";

		ProcedureSettingsParseResult result = ProcedureSettingsParser.Parse(settings);

		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains("IncludeFebCustomProperties") && w.Contains("JsonFields"));
	}

	[Fact]
	public void generate_alias_file_is_a_known_key_that_defaults_to_true_with_no_warning()
	{
		ProcedureSettingsParseResult result = ProcedureSettingsParser.Parse(MinimalValidSettings());

		Assert.True(result.Settings.GenerateAliasFile);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void generate_alias_file_n_is_honoured()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["GenerateAliasFile"] = "N";

		ProcedureSettingsParseResult result = ProcedureSettingsParser.Parse(settings);

		Assert.False(result.Settings.GenerateAliasFile);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void an_unrecognized_key_warns_and_does_not_throw()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["TotallyMadeUpKey"] = "x";

		ProcedureSettingsParseResult result = ProcedureSettingsParser.Parse(settings);

		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains("TotallyMadeUpKey"));
	}

	// ---- alias-only runs (no document generated) ----

	[Fact]
	public void alias_only_settings_with_no_inclusion_source_at_all_parses_fine()
	{
		// Neither document is generated, so none of Facilities/Airports/Procedures/
		// AirportProcedures/IncludeRoiAirports is needed: the alias file covers every airport
		// in the metafile regardless.
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = @"C:\Output",
			["GenerateChangesDocument"] = "N",
			["GenerateProceduresJson"] = "N",
		};

		ProcedureSettingsParseResult result = ProcedureSettingsParser.Parse(settings);

		Assert.True(result.Settings.GenerateAliasFile);
		Assert.Empty(result.Settings.Facilities);
		Assert.Empty(result.Settings.Airports);
		Assert.Empty(result.Settings.Procedures);
		Assert.Empty(result.Settings.AirportProcedures);
		Assert.False(result.Settings.IncludeRoiAirports);
	}

	[Fact]
	public void alias_only_settings_with_include_roi_airports_and_no_region_of_interest_parses_fine()
	{
		// The "IncludeRoiAirports without ROI" guard only applies when a document is generated.
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = @"C:\Output",
			["GenerateChangesDocument"] = "N",
			["GenerateProceduresJson"] = "N",
			["IncludeRoiAirports"] = "Y",
		};

		ProcedureSettings parsed = ProcedureSettingsParser.Parse(settings).Settings;

		Assert.True(parsed.IncludeRoiAirports);
		Assert.Null(parsed.Roi);
	}

	// ---- UploadToVnas / CrcDefaultsFor ----

	[Fact]
	public void upload_to_vnas_may_name_the_alias_file()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["UploadToVnas"] = "FAA_CHART_RECALL.txt";

		ProcedureSettings parsed = ProcedureSettingsParser.Parse(settings).Settings;

		Assert.True(parsed.Vnas.IsUploaded(ProcedureOutputFiles.Alias));
	}

	[Fact]
	public void upload_to_vnas_naming_any_other_file_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["UploadToVnas"] = "Procedures.json";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => ProcedureSettingsParser.Parse(settings));
		Assert.Contains("Procedures.json", ex.Message);
	}

	[Fact]
	public void crc_defaults_for_naming_the_alias_file_throws()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["UploadToVnas"] = "FAA_CHART_RECALL.txt";
		settings["CrcDefaultsFor"] = "FAA_CHART_RECALL.txt";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => ProcedureSettingsParser.Parse(settings));
		Assert.Contains("FAA_CHART_RECALL.txt", ex.Message);
	}

	[Fact]
	public void generate_alias_file_and_vnas_default_to_on_and_nothing_uploaded()
	{
		ProcedureSettings parsed = ProcedureSettingsParser.Parse(MinimalValidSettings()).Settings;

		Assert.True(parsed.GenerateAliasFile);
		Assert.Empty(parsed.Vnas.UploadFiles);
		Assert.False(parsed.Vnas.IsUploaded(ProcedureOutputFiles.Alias));
	}

	// ---- ROI reuse ----

	[Fact]
	public void roi_is_parsed_when_filter_by_roi_is_set()
	{
		Dictionary<string, string> settings = MinimalValidSettings();
		settings["FilterByRoi"] = "Y";
		settings["RoiSwLat"] = "40.0";
		settings["RoiSwLon"] = "-89.0";
		settings["RoiNeLat"] = "43.0";
		settings["RoiNeLon"] = "-86.0";

		ProcedureSettings parsed = ProcedureSettingsParser.Parse(settings).Settings;

		Assert.NotNull(parsed.Roi);
		Assert.Equal(40.0, parsed.Roi!.SwLat);
	}
}
