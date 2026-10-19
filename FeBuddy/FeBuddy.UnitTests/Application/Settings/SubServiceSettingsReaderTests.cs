using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Infrastructure.Geojson;

namespace FeBuddy.UnitTests.Application.Settings;

/// <summary>
/// Covers <see cref="SubServiceSettingsReader.ReadCrcDefaultsFiles"/>: the list defaults to empty,
/// and only a sub-service's own GeoJSON files are accepted. And the area
/// (<see cref="SubServiceSettingsReader.ReadArea"/>): only an area the sub-service offers, what a
/// block without one means, and that only its own filter - the ARTCCs or the ROI - is read.
/// </summary>
public sealed class SubServiceSettingsReaderTests
{
	private static bool IsGeojson(string key) => key is "Things_Lines" or "Things_Text";

	private static Dictionary<string, string> Settings(params (string Key, string Value)[] entries) =>
		entries.ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase);

	private static CrcDefaultsFiles Read(params (string Key, string Value)[] entries) =>
		SubServiceSettingsReader.ReadCrcDefaultsFiles(
			Settings(entries), IsGeojson, example: "Things_Lines, Things_Text");

	[Fact]
	public void no_file_gets_crc_defaults_when_the_key_is_absent_or_blank()
	{
		Assert.Empty(Read().Files);
		Assert.Empty(Read(("CrcDefaultsFor", " ")).Files);
		Assert.Empty(Read(("CrcDefaultsFor", "")).Files);
	}

	[Fact]
	public void the_list_is_trimmed_and_blanks_ignored()
	{
		CrcDefaultsFiles files = Read(("CrcDefaultsFor", " Things_Lines , ,Things_Text,"));

		Assert.Equal(2, files.Files.Count);
		Assert.True(files.HasCrcDefaults("Things_Lines"));
		Assert.True(files.HasCrcDefaults("Things_Text"));
	}

	/// <summary>Only a GeoJSON file the sub-service writes can get CRC-ERAM defaults: not another one's, not an alias file, not junk.</summary>
	[Theory]
	[InlineData("Other_Lines")]
	[InlineData("Things.txt")]
	[InlineData("junk")]
	public void a_file_that_is_not_a_geojson_file_of_the_sub_service_is_rejected_with_an_example(string key)
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => Read(("CrcDefaultsFor", $"Things_Lines,{key}")));

		Assert.Contains($"'{key}'", ex.Message, StringComparison.Ordinal);
		Assert.Contains("'CrcDefaultsFor'", ex.Message, StringComparison.Ordinal);
		Assert.Contains("Things_Lines, Things_Text", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void crc_defaults_for_is_a_setting_every_sub_service_understands()
	{
		IReadOnlyList<ServiceMessage> warnings = SubServiceSettingsReader.UnknownKeyWarnings(
			Settings(("CrcDefaultsFor", "Things_Lines")),
			new HashSet<string>(),
			new Dictionary<string, CrcFeatureKind[]>(),
			source: "ThingsSettingsParser",
			labelSource: "each thing is labelled with its own ID");

		Assert.Empty(warnings);
	}

	// ---- the area (issue #335) ----

	private static readonly (string Key, string Value)[] Roi =
		[("FilterByRoi", "Y"), ("RoiSwLat", "32.5"), ("RoiSwLon", "-120"), ("RoiNeLat", "37"), ("RoiNeLon", "-114")];

	[Theory]
	[InlineData("Artccs", SubServiceArea.Artccs)]
	[InlineData("roi", SubServiceArea.Roi)]
	[InlineData(" EVERYTHING ", SubServiceArea.Everything)]
	public void the_area_is_read_by_name_ignoring_case(string text, SubServiceArea expected)
	{
		Assert.Equal(expected, SubServiceSettingsReader.ReadArea(Settings(("Area", text)), SubServiceSettingsReader.ArtccOrRoiAreas));
	}

	/// <summary>Only an area the sub-service offers is accepted, by name: not a number, not another tab's.</summary>
	[Theory]
	[InlineData("None")]
	[InlineData("1")]
	[InlineData("Region")]
	public void an_area_the_sub_service_doesnt_offer_is_rejected_naming_those_it_does(string text)
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() =>
			SubServiceSettingsReader.ReadArea(Settings(("Area", text)), SubServiceSettingsReader.ArtccOrRoiAreas));

		Assert.Contains($"\"{text}\"", ex.Message, StringComparison.Ordinal);
		Assert.Contains("\"Artccs\", \"Roi\", \"Everything\"", ex.Message, StringComparison.Ordinal);
	}

	/// <summary>A block without <c>Area</c> is refused, whatever its filters say.</summary>
	[Fact]
	public void a_block_without_an_area_is_refused()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() =>
			SubServiceSettingsReader.ReadArea(Settings([("ArtccFilter", "ZOB"), .. Roi]), SubServiceSettingsReader.ArtccOrRoiAreas));

		Assert.Contains("'Area'", ex.Message, StringComparison.Ordinal);
		Assert.Throws<ArgumentException>(() => SubServiceSettingsReader.ReadArea(Settings(("Area", " ")), SubServiceSettingsReader.RoiAreas));
	}

	[Fact]
	public void the_roi_is_read_only_for_the_roi_area_and_then_required()
	{
		Dictionary<string, string> withRoi = Settings(Roi);

		Assert.Equal(32.5, SubServiceSettingsReader.ReadAreaRoi(withRoi, SubServiceArea.Roi)!.SwLat);
		Assert.Null(SubServiceSettingsReader.ReadAreaRoi(withRoi, SubServiceArea.Artccs));
		Assert.Null(SubServiceSettingsReader.ReadAreaRoi(withRoi, SubServiceArea.Everything));

		ArgumentException ex = Assert.Throws<ArgumentException>(() => SubServiceSettingsReader.ReadAreaRoi(Settings(), SubServiceArea.Roi));
		Assert.Contains("'FilterByRoi'", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void the_artccs_are_read_only_for_the_artccs_area_upper_cased_once_each_and_then_required()
	{
		Dictionary<string, string> listed = Settings(("ArtccFilter", "zob, ZNY, Zob"));

		Assert.Equal(["ZOB", "ZNY"], SubServiceSettingsReader.ReadAreaArtccs(listed, SubServiceArea.Artccs, "ArtccFilter"));
		Assert.Empty(SubServiceSettingsReader.ReadAreaArtccs(listed, SubServiceArea.Roi, "ArtccFilter"));

		ArgumentException ex = Assert.Throws<ArgumentException>(() =>
			SubServiceSettingsReader.ReadAreaArtccs(Settings(("ArtccFilter", " ")), SubServiceArea.Artccs, "ArtccFilter"));
		Assert.Contains("'ArtccFilter' lists none", ex.Message, StringComparison.Ordinal);
	}

	/// <summary>A saved precision the Settings page and every run use, or the default for one that isn't 0 to 15.</summary>
	[Theory]
	[InlineData("8", 8)]
	[InlineData("0", GeojsonFileWriter.NoRounding)]
	[InlineData("15", GeojsonFileWriter.MaxPrecision)]
	[InlineData("16", GeojsonFileWriter.DefaultPrecision)]
	[InlineData("-1", GeojsonFileWriter.DefaultPrecision)]
	[InlineData("six", GeojsonFileWriter.DefaultPrecision)]
	[InlineData(null, GeojsonFileWriter.DefaultPrecision)]
	public void a_saved_precision_or_the_default(string? saved, int precision) =>
		Assert.Equal(precision, SubServiceSettingsReader.CoordinatePrecisionOrDefault(saved));
}
