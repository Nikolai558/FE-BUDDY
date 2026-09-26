using System.Text.Json;

using FeBuddy.Core.Application.Airac.ArtccBoundaries;
using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.ArtccBoundaries.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Infrastructure.Geojson;

using FeBuddy.UnitTests.Application.Airac.ArtccBoundaries.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.ArtccBoundaries;

/// <summary>
/// Covers <see cref="ArtccBoundaryGeojsonWriter"/>: how <see cref="ArtccBoundaryOutputBy"/> groups
/// rings into files, that a closed ring is written as a LineString, the antimeridian split, ROI
/// clipping, CRC Line defaults, <c>feb.*</c> properties, and vNAS folder routing.
/// </summary>
public sealed class ArtccBoundaryGeojsonWriterTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_ArtccGeojson_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(_outputDirectory))
			{
				Directory.Delete(_outputDirectory, recursive: true);
			}
		}
		catch
		{
			// Best-effort.
		}
	}

	private ArtccBoundarySettings Settings() => new()
	{
		OutputDirectory = _outputDirectory,
		SplitAtAntimeridian = true,
		IncludeFebCustomProperties = false,
	};

	private static IReadOnlyList<ArtccBoundaryRing> ZobRings() =>
		ArtccBoundaryBuilder.Read(ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.ZobBaseRow()],
			[.. ArtccBoundaryTestData.ZobHighRows(), .. ArtccBoundaryTestData.ZobLowRows()])).Rings;

	private static IReadOnlyList<ArtccBoundaryRing> ZakRings() =>
		ArtccBoundaryBuilder.Read(ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.ZakBaseRow()],
			[.. ArtccBoundaryTestData.ZakCtaRows(), .. ArtccBoundaryTestData.ZakFirRows()])).Rings;

	private static IReadOnlyList<ArtccBoundaryRing> ZzzRings() =>
		ArtccBoundaryBuilder.Read(ArtccBoundaryTestData.Build(baseRows: null, segRows: ArtccBoundaryTestData.ZzzRows())).Rings;

	private static List<JsonElement> Features(string path)
	{
		using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
		return [.. document.RootElement.GetProperty("features").EnumerateArray().Select(f => f.Clone())];
	}

	private static string FileNamed(GeojsonFileSet files, string suffix) =>
		Assert.Single(files.FilesWritten, f => f.EndsWith(suffix, StringComparison.Ordinal));

	[Fact]
	public void generate_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => ArtccBoundaryGeojsonWriter.Generate(null!, Settings()));
		Assert.Throws<ArgumentNullException>(() => ArtccBoundaryGeojsonWriter.Generate([], null!));
	}

	[Fact]
	public void generate_writes_nothing_when_there_are_no_rings() =>
		Assert.Empty(ArtccBoundaryGeojsonWriter.Generate([], Settings()).FilesWritten);

	// ---- OutputBy grouping ----

	[Fact]
	public void high_low_mode_puts_unlimited_rings_in_both_the_high_and_the_low_file()
	{
		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate(
			[.. ZobRings(), .. ZakRings()], Settings() with { OutputBy = ArtccBoundaryOutputBy.HighLow });

		string highFile = FileNamed(result, "ARTCC-Boundary_High_Lines.geojson");
		string lowFile = FileNamed(result, "ARTCC-Boundary_Low_Lines.geojson");

		Assert.Equal(3, Features(highFile).Count); // ZOB High + ZAK CTA + ZAK FIR
		Assert.Equal(3, Features(lowFile).Count); // ZOB Low + ZAK CTA + ZAK FIR
	}

	[Fact]
	public void high_low_unlimited_mode_writes_one_file_per_altitude_present()
	{
		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate(
			[.. ZobRings(), .. ZakRings()], Settings() with { OutputBy = ArtccBoundaryOutputBy.HighLowUnlimited });

		Assert.Equal(3, result.FilesWritten.Count);
		Assert.Single(Features(FileNamed(result, "ARTCC-Boundary_High_Lines.geojson")));
		Assert.Single(Features(FileNamed(result, "ARTCC-Boundary_Low_Lines.geojson")));
		Assert.Equal(2, Features(FileNamed(result, "ARTCC-Boundary_Unlimited_Lines.geojson")).Count);
	}

	[Fact]
	public void artcc_altitude_mode_writes_one_file_per_location_and_altitude_present()
	{
		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate(
			[.. ZobRings(), .. ZakRings()], Settings() with { OutputBy = ArtccBoundaryOutputBy.ArtccAltitude });

		Assert.Equal(3, result.FilesWritten.Count);
		Assert.Single(Features(FileNamed(result, "ARTCC-Boundary_ZOB-HIGH_Lines.geojson")));
		Assert.Single(Features(FileNamed(result, "ARTCC-Boundary_ZOB-LOW_Lines.geojson")));
		Assert.Equal(2, Features(FileNamed(result, "ARTCC-Boundary_ZAK-UNLIMITED_Lines.geojson")).Count);
	}

	// ---- geometry ----

	[Fact]
	public void a_closed_ring_is_written_as_a_linestring()
	{
		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate(
			ZobRings(), Settings() with { OutputBy = ArtccBoundaryOutputBy.HighLowUnlimited });

		JsonElement geometry = Features(FileNamed(result, "ARTCC-Boundary_High_Lines.geojson"))[0].GetProperty("geometry");
		Assert.Equal("LineString", geometry.GetProperty("type").GetString());

		JsonElement coordinates = geometry.GetProperty("coordinates");
		Assert.Equal(
			coordinates[0].EnumerateArray().Select(c => c.GetDouble()),
			coordinates[coordinates.GetArrayLength() - 1].EnumerateArray().Select(c => c.GetDouble()));
	}

	[Fact]
	public void with_split_at_antimeridian_a_ring_crossing_it_becomes_a_multilinestring_whose_parts_never_jump_more_than_180_degrees()
	{
		ArtccBoundarySettings settings = Settings() with { OutputBy = ArtccBoundaryOutputBy.HighLowUnlimited, SplitAtAntimeridian = true };

		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate(ZakRings(), settings);

		foreach (JsonElement feature in Features(FileNamed(result, "ARTCC-Boundary_Unlimited_Lines.geojson")))
		{
			JsonElement geometry = feature.GetProperty("geometry");
			Assert.Equal("MultiLineString", geometry.GetProperty("type").GetString());

			foreach (JsonElement part in geometry.GetProperty("coordinates").EnumerateArray())
			{
				double[] longitudes = [.. part.EnumerateArray().Select(p => p[0].GetDouble())];
				for (int i = 1; i < longitudes.Length; i++)
				{
					Assert.True(Math.Abs(longitudes[i] - longitudes[i - 1]) <= 180.0);
				}
			}
		}
	}

	[Fact]
	public void a_ring_crossing_the_antimeridian_twice_splits_into_exactly_two_arcs_not_three()
	{
		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate(
			ZakRings(), Settings() with { OutputBy = ArtccBoundaryOutputBy.HighLowUnlimited });

		JsonElement geometry = Features(FileNamed(result, "ARTCC-Boundary_Unlimited_Lines.geojson"))[0].GetProperty("geometry");

		// ZAK's CTA ring crosses +/-180 exactly twice: once in its interior (179 -> -179) and once
		// on its closing edge (-170 back to its own first point, 170). Two real crossings should
		// split a closed ring into two arcs, not three - the piece either side of the ring's
		// arbitrary starting vertex is one continuous arc and must not be split there.
		Assert.Equal(2, geometry.GetProperty("coordinates").GetArrayLength());
	}

	[Fact]
	public void without_split_at_antimeridian_the_ring_stays_one_linestring()
	{
		ArtccBoundarySettings settings = Settings() with { OutputBy = ArtccBoundaryOutputBy.HighLowUnlimited, SplitAtAntimeridian = false };

		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate(ZakRings(), settings);

		Assert.All(
			Features(FileNamed(result, "ARTCC-Boundary_Unlimited_Lines.geojson")),
			feature => Assert.Equal("LineString", feature.GetProperty("geometry").GetProperty("type").GetString()));
	}

	// ---- ROI ----

	[Fact]
	public void roi_clips_a_ring_that_crosses_its_edge()
	{
		// ZOB's HIGH ring is a closed triangle at lon -81/-82/-83; an ROI cut at -81.5 leaves only
		// the -81 vertex inside, so the ring must be cut down to a short line, not kept whole.
		RegionOfInterest roi = new(39.0, -81.5, 42.0, -79.0);
		ArtccBoundarySettings settings = Settings() with { OutputBy = ArtccBoundaryOutputBy.HighLowUnlimited, Roi = roi };

		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate(ZobRings(), settings);

		string highFile = FileNamed(result, "ARTCC-Boundary_High_Lines.geojson");
		JsonElement geometry = Features(highFile)[0].GetProperty("geometry");
		string type = geometry.GetProperty("type").GetString()!;
		JsonElement coordinates = geometry.GetProperty("coordinates");

		IEnumerable<JsonElement> points = type == "MultiLineString"
			? coordinates.EnumerateArray().SelectMany(part => part.EnumerateArray())
			: coordinates.EnumerateArray();

		// Cut, not kept whole: every remaining point is near the ROI's western edge, and neither
		// of the ring's other two vertices (at -82 and -83) survives.
		List<double> longitudes = [.. points.Select(p => p[0].GetDouble())];
		Assert.NotEmpty(longitudes);
		Assert.All(longitudes, lon => Assert.True(lon >= -81.51));
	}

	[Fact]
	public void a_ring_entirely_outside_the_roi_writes_no_feature_for_it()
	{
		// An ROI around ZOB only: ZAK's rings (far away, near the antimeridian) are entirely outside it.
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);
		ArtccBoundarySettings settings = Settings() with { OutputBy = ArtccBoundaryOutputBy.HighLow, Roi = roi };

		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate([.. ZobRings(), .. ZakRings()], settings);

		// Only ZOB's own ring is drawn in each file; ZAK's Unlimited rings are clipped away entirely.
		Assert.Single(Features(FileNamed(result, "ARTCC-Boundary_High_Lines.geojson")));
		Assert.Single(Features(FileNamed(result, "ARTCC-Boundary_Low_Lines.geojson")));
	}

	[Fact]
	public void a_file_left_with_no_features_after_roi_clipping_is_not_written()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0); // around ZOB only
		ArtccBoundarySettings settings = Settings() with { OutputBy = ArtccBoundaryOutputBy.HighLowUnlimited, Roi = roi };

		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate(ZakRings(), settings); // ZAK only, entirely outside

		Assert.Empty(result.FilesWritten);
	}

	[Fact]
	public void a_file_with_crc_defaults_but_no_surviving_rings_after_roi_clipping_is_still_not_written()
	{
		// The Unlimited file would get an isLineDefaults Feature, but every real ring is clipped
		// away: the file must still count as empty (an isDefaults-only file draws nothing).
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0); // around ZOB only; ZAK is entirely outside
		ArtccBoundarySettings settings = Settings() with
		{
			OutputBy = ArtccBoundaryOutputBy.HighLowUnlimited,
			Roi = roi,
			Vnas = new VnasFileChoices(
				uploadFiles: [ArtccBoundaryOutputFiles.KeyFor(ArtccBoundaryOutputFiles.UnlimitedClass)],
				crcDefaultsFiles: [ArtccBoundaryOutputFiles.KeyFor(ArtccBoundaryOutputFiles.UnlimitedClass)]),
			LineDefaults = new Dictionary<string, CrcLineDefaults>(StringComparer.OrdinalIgnoreCase)
			{
				[ArtccBoundaryOutputFiles.UnlimitedClass] = LineDefaults(3),
			},
		};

		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate(ZakRings(), settings);

		Assert.Empty(result.FilesWritten);
	}

	// ---- CRC Line defaults ----

	private static CrcLineDefaults LineDefaults(int bcg) => new() { Bcg = bcg, Filters = [bcg], Style = "solid", Thickness = 1 };

	[Fact]
	public void is_line_defaults_feature_is_first_only_for_a_file_chosen_for_crc_defaults()
	{
		ArtccBoundarySettings settings = Settings() with
		{
			OutputBy = ArtccBoundaryOutputBy.HighLowUnlimited,
			Vnas = new VnasFileChoices(
				uploadFiles: [ArtccBoundaryOutputFiles.KeyFor(ArtccBoundaryOutputFiles.HighClass), ArtccBoundaryOutputFiles.KeyFor(ArtccBoundaryOutputFiles.LowClass)],
				crcDefaultsFiles: [ArtccBoundaryOutputFiles.KeyFor(ArtccBoundaryOutputFiles.HighClass)]),
			LineDefaults = new Dictionary<string, CrcLineDefaults>(StringComparer.OrdinalIgnoreCase)
			{
				[ArtccBoundaryOutputFiles.HighClass] = LineDefaults(3),
			},
		};

		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate(ZobRings(), settings);

		JsonElement[] highFeatures = [.. Features(FileNamed(result, "ARTCC-Boundary_High_Lines.geojson"))];
		Assert.True(highFeatures[0].GetProperty("properties").GetProperty("isLineDefaults").GetBoolean());
		Assert.Equal(2, highFeatures.Length); // the defaults Feature plus the one real ring

		JsonElement[] lowFeatures = [.. Features(FileNamed(result, "ARTCC-Boundary_Low_Lines.geojson"))];
		Assert.False(lowFeatures[0].GetProperty("properties").TryGetProperty("isLineDefaults", out _));
		Assert.Single(lowFeatures);
	}

	// ---- feb.* properties ----

	private static readonly ArtccBoundaryFebProperty[] AllFebProperties =
	[
		ArtccBoundaryFebProperty.LocationId, ArtccBoundaryFebProperty.LocationName, ArtccBoundaryFebProperty.LocationType,
		ArtccBoundaryFebProperty.IcaoId, ArtccBoundaryFebProperty.ComputerId, ArtccBoundaryFebProperty.Altitude,
		ArtccBoundaryFebProperty.Type, ArtccBoundaryFebProperty.City, ArtccBoundaryFebProperty.CountryCode,
	];

	private ArtccBoundarySettings SettingsWithAllFebProperties() => Settings() with
	{
		OutputBy = ArtccBoundaryOutputBy.HighLowUnlimited,
		IncludeFebCustomProperties = true,
		FebProperties = AllFebProperties,
	};

	[Fact]
	public void a_ring_with_a_full_arb_base_row_carries_every_feb_property()
	{
		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate(ZobRings(), SettingsWithAllFebProperties());

		JsonElement properties = Features(FileNamed(result, "ARTCC-Boundary_High_Lines.geojson"))[0].GetProperty("properties");

		Assert.Equal("ZOB", properties.GetProperty("feb.locationId").GetString());
		Assert.Equal("CLEVELAND", properties.GetProperty("feb.locationName").GetString());
		Assert.Equal("ARTCC", properties.GetProperty("feb.locationType").GetString());
		Assert.Equal("KZOB", properties.GetProperty("feb.icaoId").GetString());
		Assert.Equal("ZOB", properties.GetProperty("feb.computerId").GetString());
		Assert.Equal("HIGH", properties.GetProperty("feb.altitude").GetString());
		Assert.Equal("ARTCC", properties.GetProperty("feb.type").GetString());
		Assert.Equal("CLEVELAND", properties.GetProperty("feb.city").GetString());
		Assert.Equal("US", properties.GetProperty("feb.countryCode").GetString());
	}

	[Fact]
	public void a_ring_with_no_arb_base_row_omits_the_blank_location_fields()
	{
		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate(ZzzRings(), SettingsWithAllFebProperties());

		JsonElement properties = Features(FileNamed(result, "ARTCC-Boundary_High_Lines.geojson"))[0].GetProperty("properties");

		Assert.Equal("ZZZ", properties.GetProperty("feb.locationId").GetString());
		Assert.Equal("MYSTERY CENTER", properties.GetProperty("feb.locationName").GetString());
		Assert.False(properties.TryGetProperty("feb.locationType", out _));
		Assert.False(properties.TryGetProperty("feb.icaoId", out _));
		Assert.False(properties.TryGetProperty("feb.computerId", out _));
		Assert.False(properties.TryGetProperty("feb.city", out _));
		Assert.False(properties.TryGetProperty("feb.countryCode", out _));
	}

	[Fact]
	public void a_zak_rings_type_distinguishes_cta_from_fir()
	{
		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate(ZakRings(), SettingsWithAllFebProperties());

		JsonElement[] features = [.. Features(FileNamed(result, "ARTCC-Boundary_Unlimited_Lines.geojson"))];
		Assert.Equal(["CTA", "FIR"], features.Select(f => f.GetProperty("properties").GetProperty("feb.type").GetString()));
	}

	[Fact]
	public void no_feb_properties_are_written_when_they_are_off()
	{
		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate(
			ZobRings(), Settings() with { OutputBy = ArtccBoundaryOutputBy.HighLowUnlimited });

		foreach (string file in result.FilesWritten)
		{
			Assert.All(Features(file), feature =>
				Assert.DoesNotContain(
					feature.GetProperty("properties").EnumerateObject(),
					property => property.Name.StartsWith("feb.", StringComparison.Ordinal)));
		}
	}

	// ---- vNAS folder routing ----

	[Fact]
	public void a_file_marked_for_vnas_goes_under_upload_to_vnas_while_others_do_not()
	{
		ArtccBoundarySettings settings = Settings() with
		{
			OutputBy = ArtccBoundaryOutputBy.HighLowUnlimited,
			Vnas = new VnasFileChoices([ArtccBoundaryOutputFiles.KeyFor(ArtccBoundaryOutputFiles.HighClass)], []),
		};

		GeojsonFileSet result = ArtccBoundaryGeojsonWriter.Generate(ZobRings(), settings);

		Assert.Contains(result.FilesWritten, f => f.Contains("Upload_to_vNAS", StringComparison.Ordinal) && f.EndsWith("ARTCC-Boundary_High_Lines.geojson", StringComparison.Ordinal));
		Assert.Contains(result.FilesWritten, f => !f.Contains("Upload_to_vNAS", StringComparison.Ordinal) && f.EndsWith("ARTCC-Boundary_Low_Lines.geojson", StringComparison.Ordinal));
	}
}
