using System.Text.Json;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Airways.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Airways;

/// <summary>
/// Runs the whole Airways pipeline (<see cref="AirwayService.Run"/>) with the options the
/// narrower tests leave off: CRC defaults, a region of interest (clipping, buffering, alias
/// scope), and the advisory when the filters leave nothing to write.
/// </summary>
public sealed class AirwayServiceTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_Airways_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_outputDirectory))
		{
			Directory.Delete(_outputDirectory, recursive: true);
		}
	}

	/// <summary>J1: AAAAA -&gt; the ABC VOR/DME -&gt; CCCCC, running north-west.</summary>
	private static NasrCsvDataCollection J1() =>
		AirwayTestDataBuilder.Build(
			fixes: [("AAAAA", 40.0, -80.0), ("CCCCC", 42.0, -82.0)],
			navaids: [("ABC", 41.0, -81.0)],
			awyId: "J1",
			segments:
			[
				AirwayTestDataBuilder.Segment("J1", 10, "AAAAA", "WP", "ABC"),
				AirwayTestDataBuilder.Segment("J1", 20, "ABC", "VOR/DME", "CCCCC"),
				AirwayTestDataBuilder.Segment("J1", 30, "CCCCC", "WP", null),
			]);

	private Dictionary<string, string> Settings(params (string Key, string Value)[] overrides)
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = _outputDirectory,
			["OutputBy"] = "HighLow",
		};

		foreach ((string key, string value) in overrides)
		{
			settings[key] = value;
		}

		return settings;
	}

	/// <summary>A region around AAAAA and ABC that leaves CCCCC outside.</summary>
	private static (string, string)[] SouthEastRoi =>
		[("FilterByRoi", "Y"), ("RoiSwLat", "39.5"), ("RoiSwLon", "-81.5"), ("RoiNeLat", "41.5"), ("RoiNeLon", "-79.5")];

	/// <summary>A region far away from J1.</summary>
	private static (string, string)[] FarAwayRoi =>
		[("FilterByRoi", "Y"), ("RoiSwLat", "30.0"), ("RoiSwLon", "-100.0"), ("RoiNeLat", "31.0"), ("RoiNeLon", "-99.0")];

	private static IEnumerable<(string, string)> CrcDefaults()
	{
		foreach (string cls in new[] { "High", "Low", "Other" })
		{
			yield return ($"Crc.{cls}.Line.bcg", "3");
			yield return ($"Crc.{cls}.Line.filters", "3");
			yield return ($"Crc.{cls}.Line.style", "solid");
			yield return ($"Crc.{cls}.Line.thickness", "1");
			yield return ($"Crc.{cls}.Symbol.bcg", "3");
			yield return ($"Crc.{cls}.Symbol.filters", "3");
			yield return ($"Crc.{cls}.Symbol.style", "airwayIntersections");
			yield return ($"Crc.{cls}.Symbol.size", "1");
			yield return ($"Crc.{cls}.Text.bcg", "3");
			yield return ($"Crc.{cls}.Text.filters", "3");
			yield return ($"Crc.{cls}.Text.size", "1");
			yield return ($"Crc.{cls}.Text.underline", "N");
			yield return ($"Crc.{cls}.Text.opaque", "N");
			yield return ($"Crc.{cls}.Text.xOffset", "0");
			yield return ($"Crc.{cls}.Text.yOffset", "0");
		}
	}

	private static JsonElement[] Features(string path)
	{
		using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
		return [.. document.RootElement.GetProperty("features").EnumerateArray().Select(f => f.Clone())];
	}

	[Fact]
	public void run_with_crc_defaults_and_a_roi_draws_only_the_points_inside_it()
	{
		string everyFile = string.Join(',',
			from cls in new[] { "High", "Low", "Other" }
			from kind in new[] { "Lines", "Symbols", "Text" }
			select $"Airways_{cls}_{kind}");

		AirwayServiceResult result = AirwayService.Run(J1(), Settings(
			[
				.. CrcDefaults(),
				.. SouthEastRoi,
				("UploadToVnas", everyFile),
				("CrcDefaultsFor", everyFile),
				("GenerateAliasFile", "N"),
			]));

		Assert.Equal(1, result.AirwayCount);
		Assert.Empty(result.Warnings);
		Assert.All(result.GeojsonFilesWritten, path =>
			Assert.Equal(Path.Combine(_outputDirectory, "Upload_to_vNAS", "Geojson"), Path.GetDirectoryName(path)));

		string symbols = Assert.Single(result.GeojsonFilesWritten, p => p.EndsWith("_Symbols.geojson", StringComparison.Ordinal));
		JsonElement[] features = Features(symbols);
		Assert.True(features[0].GetProperty("properties").GetProperty("isSymbolDefaults").GetBoolean());

		// AAAAA and ABC are inside the region; CCCCC is not drawn. The VOR/DME gets the VOR symbol.
		Assert.Equal(3, features.Length);
		Assert.Contains(features, f => f.GetProperty("properties").TryGetProperty("style", out JsonElement style) && style.GetString() == "vor");

		Assert.All(
			result.GeojsonFilesWritten,
			path => Assert.StartsWith("is", Features(path)[0].GetProperty("properties").EnumerateObject().First().Name, StringComparison.Ordinal));
	}

	[Fact]
	public void run_that_buffers_waypoints_shortens_each_leg()
	{
		AirwayServiceResult result = AirwayService.Run(J1(), Settings(
			[.. SouthEastRoi, ("BufferAirwayWaypoints", "Y"), ("GenerateAliasFile", "N")]));

		Assert.Equal(1, result.AirwayCount);

		// Buffered, the drawn line starts a buffer's distance along the leg, not on AAAAA itself.
		string lines = Assert.Single(result.GeojsonFilesWritten, p => p.EndsWith("_Lines.geojson", StringComparison.Ordinal));
		JsonElement start = Features(lines)[0].GetProperty("geometry").GetProperty("coordinates")[0][0];
		Assert.NotEqual(-80.0, start[0].GetDouble());
		Assert.NotEqual(40.0, start[1].GetDouble());
	}

	[Fact]
	public void an_airway_whose_legs_are_all_shorter_than_the_buffer_is_dropped()
	{
		NasrCsvDataCollection data = AirwayTestDataBuilder.Build(
			fixes: [("AAAAA", 40.0, -80.0), ("BBBBB", 40.001, -80.0)],
			awyId: "J2",
			segments:
			[
				AirwayTestDataBuilder.Segment("J2", 10, "AAAAA", "WP", "BBBBB"),
				AirwayTestDataBuilder.Segment("J2", 20, "BBBBB", "WP", null),
			]);

		AirwayServiceResult result = AirwayService.Run(data, Settings(
			[.. SouthEastRoi, ("BufferAirwayWaypoints", "Y"), ("GenerateAliasFile", "N")]));

		Assert.Equal(0, result.AirwayCount);
	}

	[Fact]
	public void an_airway_that_leaves_and_reenters_the_roi_is_clipped_into_two_lines()
	{
		// East -> far west (outside) -> back east: the clip is a MultiLineString.
		NasrCsvDataCollection data = AirwayTestDataBuilder.Build(
			fixes: [("AAAAA", 40.0, -80.0), ("WESTT", 40.2, -85.0), ("CCCCC", 40.4, -80.0)],
			awyId: "V1",
			segments:
			[
				AirwayTestDataBuilder.Segment("V1", 10, "AAAAA", "WP", "WESTT"),
				AirwayTestDataBuilder.Segment("V1", 20, "WESTT", "WP", "CCCCC"),
				AirwayTestDataBuilder.Segment("V1", 30, "CCCCC", "WP", null),
			]);

		AirwayServiceResult result = AirwayService.Run(data, Settings(
			[.. SouthEastRoi, ("GenerateAliasFile", "N"), ("EmitSymbols", "N"), ("EmitText", "N")]));

		string lines = Assert.Single(result.GeojsonFilesWritten);
		JsonElement geometry = Features(lines)[0].GetProperty("geometry");
		Assert.Equal("MultiLineString", geometry.GetProperty("type").GetString());
		Assert.Equal(2, geometry.GetProperty("coordinates").GetArrayLength());
	}

	[Theory]
	[InlineData("HighLow", "RoiAirways", "no Airways GeoJSON or alias files were written")]
	[InlineData("HighLow", "All", "no Airways GeoJSON files were written")]
	[InlineData("None", "RoiAirways", "no Airways alias file was written")]
	public void run_whose_filters_leave_nothing_says_which_output_is_missing(string outputBy, string aliasScope, string expected)
	{
		AirwayServiceResult result = AirwayService.Run(J1(), Settings(
			[.. FarAwayRoi, ("OutputBy", outputBy), ("AliasRoiScope", aliasScope)]));

		ServiceMessage advisory = Assert.Single(result.Messages, m => m.IsAdvisory);
		Assert.Contains(expected, advisory.Text, StringComparison.Ordinal);
		Assert.Equal(aliasScope == "All", result.AliasFilePath is not null);
	}

	[Fact]
	public void an_airway_id_without_leading_letters_is_grouped_as_unknown_with_one_warning()
	{
		NasrCsvDataCollection data = AirwayTestDataBuilder.Build(
			fixes: [("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0)],
			awyId: "123",
			segments:
			[
				AirwayTestDataBuilder.Segment("123", 10, "AAAAA", "WP", "BBBBB"),
				AirwayTestDataBuilder.Segment("123", 20, "BBBBB", "WP", null),
			]);

		AirwayServiceResult result = AirwayService.Run(data, Settings(("OutputBy", "None"), ("GenerateAliasFile", "N")));

		Assert.Contains(result.Warnings, w => w.Contains("Airway '123': its ID has no leading letters", StringComparison.Ordinal));
	}

	[Fact]
	public void a_segment_whose_start_cannot_be_located_excludes_the_airway()
	{
		NasrCsvDataCollection data = AirwayTestDataBuilder.Build(
			fixes: [("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0), ("DDDDD", 43.0, -83.0), ("EEEEE", 44.0, -84.0)],
			awyId: "J3",
			segments:
			[
				AirwayTestDataBuilder.Segment("J3", 10, "AAAAA", "WP", "BBBBB"),
				AirwayTestDataBuilder.Segment("J3", 20, "LOSTT", "WP", "LOST2"),
				AirwayTestDataBuilder.Segment("J3", 30, "LOST2", "WP", "DDDDD"),
				AirwayTestDataBuilder.Segment("J3", 40, "DDDDD", "WP", "EEEEE"),
				AirwayTestDataBuilder.Segment("J3", 50, "EEEEE", "WP", null),
			]);

		AirwayServiceResult result = AirwayService.Run(data, Settings(("OutputBy", "None"), ("GenerateAliasFile", "N")));

		Assert.Contains("J3", result.ExcludedAirwayIds);
		// Every unresolved ID is reported, not just the first.
		Assert.Contains(result.Warnings, w => w.Contains("segment start waypoint 'LOSTT'", StringComparison.Ordinal));
		Assert.Contains(result.Warnings, w => w.Contains("segment start waypoint 'LOST2'", StringComparison.Ordinal));
	}

	[Fact]
	public void settings_reject_a_malformed_roi_and_warn_on_an_unknown_text_property()
	{
		Assert.Throws<ArgumentException>(() => AirwaySettingsParser.Parse(Settings(
			("FilterByRoi", "Y"), ("RoiSwLat", "north"), ("RoiSwLon", "-81"), ("RoiNeLat", "41"), ("RoiNeLon", "-79"))));

		AirwaySettingsParseResult result = AirwaySettingsParser.Parse(Settings(("Crc.High.Text.madeUp", "1")));

		Assert.Contains("Unrecognized setting 'Crc.High.Text.madeUp'", Assert.Single(result.Messages).Text, StringComparison.Ordinal);
	}

	[Fact]
	public void feb_property_names_fall_back_to_the_enum_text()
	{
		Assert.Equal("99", FebProperties.Name((AirwayFebProperty)99));
	}
}
