using FeBuddy.Wpf.Map.Models;

namespace FeBuddy.UnitTests.Wpf.Map.Models;

/// <summary>
/// Covers <see cref="MapFeature"/>: a file's properties shown one per row, each value as text, and
/// copied as JSON; a live layer's as given, blanks left out, and copied as lines.
/// </summary>
public sealed class MapFeatureTests
{
	[Fact]
	public void a_files_values_are_shown_as_text()
	{
		MapFeature feature = MapFeature.FromGeoJson("Point", 4, """
			{ "text": "CLE", "size": 1.50, "on": true, "off": false, "gone": null,
			  "lines": ["A", "B°"], "style": { "colour": "#FFF" } }
			""");

		Assert.Equal(
			[
				new MapProperty("text", "CLE"),
				new MapProperty("size", "1.50"),
				new MapProperty("on", "true"),
				new MapProperty("off", "false"),
				new MapProperty("gone", "null"),
				new MapProperty("lines", """["A","B°"]"""),
				new MapProperty("style", """{"colour":"#FFF"}"""),
			],
			feature.Properties());
	}

	/// <summary>The copy is the file's properties object, indented, ready to paste into a file.</summary>
	[Fact]
	public void a_files_properties_copy_as_indented_json()
	{
		MapFeature feature = MapFeature.FromGeoJson("Point", null, """{"style":"solid","size":2}""");

		Assert.Equal($"{{{Environment.NewLine}  \"style\": \"solid\",{Environment.NewLine}  \"size\": 2{Environment.NewLine}}}", feature.CopyText());
	}

	[Fact]
	public void a_file_feature_without_an_object_of_properties_has_none()
	{
		MapFeature none = MapFeature.FromGeoJson("Point", null, null);

		Assert.Empty(none.Properties());
		Assert.Equal(string.Empty, none.CopyText());
		Assert.Empty(MapFeature.FromGeoJson("Point", null, "[1, 2]").Properties());
	}

	/// <summary>JSON the reader didn't check can't break the panel: it shows nothing, and copies as it is.</summary>
	[Fact]
	public void broken_json_shows_nothing_and_copies_as_it_is()
	{
		MapFeature feature = MapFeature.FromGeoJson("Point", null, "{ not json");

		Assert.Empty(feature.Properties());
		Assert.Equal("{ not json", feature.CopyText());
	}

	[Fact]
	public void a_live_layers_properties_leave_out_blanks_and_copy_as_lines()
	{
		MapFeature feature = MapFeature.FromData("Airport", [new("FAA ID", "CLE"), new("ICAO ID", " "), new("Name", "Cleveland-Hopkins Intl")]);

		Assert.False(feature.IsFromFile);
		Assert.Equal("Airport", feature.Kind);
		Assert.Null(feature.Number);
		Assert.Equal([new MapProperty("FAA ID", "CLE"), new MapProperty("Name", "Cleveland-Hopkins Intl")], feature.Properties());
		Assert.Equal($"FAA ID: CLE{Environment.NewLine}Name: Cleveland-Hopkins Intl", feature.CopyText());
	}
}
