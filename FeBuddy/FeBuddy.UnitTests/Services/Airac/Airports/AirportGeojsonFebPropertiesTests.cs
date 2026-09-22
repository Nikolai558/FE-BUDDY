using System.Text.Json;

using FeBuddy.Core.Models.Services.Airac.Airports;
using FeBuddy.Core.Services.Airac.Airports;
using FeBuddy.UnitTests.Services.Airac.Airports.Fixtures;

namespace FeBuddy.UnitTests.Services.Airac.Airports;

/// <summary>
/// Covers which <c>feb.*</c> properties land on which Airports Feature: <c>feb.rwyId</c> on
/// Runways Lines only, as an array lined up with the MultiLineString's LineStrings, and never
/// any airport-level <c>feb.*</c> property on Runways Lines.
/// </summary>
public sealed class AirportGeojsonFebPropertiesTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_AptFeb_" + Guid.NewGuid().ToString("N"));

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

	[Fact]
	public void runway_lines_carry_the_drawable_runway_ids_in_runway_order_when_selected()
	{
		string directory = Generate(true, AirportFebProperty.RwyId);

		JsonElement runways = Assert.Single(Features(directory, "Runways_Lines"));

		JsonElement rwyId = runways.GetProperty("feb.rwyId");
		Assert.Equal(JsonValueKind.Array, rwyId.ValueKind);
		Assert.Equal(new[] { "16R/34L", "16L/34R" }, Strings(rwyId));
		Assert.Equal(new[] { "feb.rwyId" }, FebNames(runways));
	}

	[Fact]
	public void runway_lines_have_one_line_string_per_runway_id()
	{
		string directory = Generate(true, AirportFebProperty.RwyId);

		using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "Runways_Lines.geojson")));
		JsonElement feature = Assert.Single(document.RootElement.GetProperty("features").EnumerateArray().ToList());

		Assert.Equal("MultiLineString", feature.GetProperty("geometry").GetProperty("type").GetString());
		Assert.Equal(
			feature.GetProperty("properties").GetProperty("feb.rwyId").GetArrayLength(),
			feature.GetProperty("geometry").GetProperty("coordinates").GetArrayLength());
	}

	[Fact]
	public void runway_lines_carry_no_feb_properties_when_the_runway_id_is_not_selected()
	{
		string directory = Generate(true, AirportFebProperty.FaaId, AirportFebProperty.Name, AirportFebProperty.TwrType);

		JsonElement runways = Assert.Single(Features(directory, "Runways_Lines"));
		Assert.Empty(FebNames(runways));
	}

	[Fact]
	public void runway_lines_carry_only_the_runway_id_when_other_properties_are_also_selected()
	{
		string directory = Generate(true, AirportFebProperty.FaaId, AirportFebProperty.RwyId, AirportFebProperty.Name);

		JsonElement runways = Assert.Single(Features(directory, "Runways_Lines"));
		Assert.Equal(new[] { "feb.rwyId" }, FebNames(runways));
	}

	[Fact]
	public void symbols_and_text_never_carry_the_runway_id()
	{
		string directory = Generate(true, AirportFebProperty.FaaId, AirportFebProperty.TwrType, AirportFebProperty.RwyId);

		List<JsonElement> symbols = Features(directory, "Airports_Symbols");
		Assert.Equal(2, symbols.Count);
		Assert.All(symbols, properties =>
		{
			Assert.False(properties.TryGetProperty("feb.rwyId", out _));
			Assert.Equal(JsonValueKind.String, properties.GetProperty("feb.faaId").ValueKind);
		});
		Assert.Equal(new[] { "SEA", "BFI" }, symbols.Select(properties => properties.GetProperty("feb.faaId").GetString()!).ToArray());

		List<JsonElement> text = Features(directory, "Airports_Text");
		Assert.Equal(2, text.Count);
		Assert.All(text, properties =>
		{
			Assert.False(properties.TryGetProperty("feb.rwyId", out _));
			Assert.Equal("TWR", properties.GetProperty("feb.twrType").GetString());
		});
	}

	[Fact]
	public void nothing_feb_is_written_when_feb_properties_are_off()
	{
		string directory = Generate(false, AirportFebProperty.FaaId, AirportFebProperty.TwrType, AirportFebProperty.RwyId);

		foreach (string fileStem in new[] { "Airports_Symbols", "Airports_Text", "Runways_Lines" })
		{
			List<JsonElement> features = Features(directory, fileStem);
			Assert.NotEmpty(features);
			Assert.All(features, properties => Assert.Empty(FebNames(properties)));
		}
	}

	/// <summary>
	/// Writes two airports - SEA with two drawable runways around one that has no geometry, and
	/// BFI with no runways at all - and returns the directory the files landed in.
	/// </summary>
	private string Generate(bool includeFeb, params AirportFebProperty[] febProperties)
	{
		AirportRunway first = new()
		{
			RunwayId = "16R/34L",
			Length = 8500,
			FirstEnd = new AirportRunwayEnd("16R", 47.463, -122.318),
			SecondEnd = new AirportRunwayEnd("34L", 47.440, -122.318),
		};
		AirportRunway noGeometry = AirportTestDataBuilder.BuiltRunway("08/26", 3000);
		AirportRunway second = new()
		{
			RunwayId = "16L/34R",
			Length = 11901,
			FirstEnd = new AirportRunwayEnd("16L", 47.464, -122.308),
			SecondEnd = new AirportRunwayEnd("34R", 47.431, -122.308),
		};

		Airport sea = AirportTestDataBuilder.BuiltAirport(faaId: "SEA", runways: new[] { first, noGeometry, second });
		Airport bfi = AirportTestDataBuilder.BuiltAirport(faaId: "BFI", name: "BOEING FIELD/KING COUNTY INTL");

		AirportSettings settings = new()
		{
			OutputDirectory = _outputDirectory,
			GenerateGeojson = true,
			GenerateAliasFile = false,
			IncludeFebCustomProperties = includeFeb,
			FebProperties = febProperties,
			IncludeCrcLineDefaults = false,
			IncludeCrcSymbolDefaults = false,
			IncludeCrcTextDefaults = false,
			Roi = null,
		};

		AirportGeojsonGenerateResult result = AirportGeojsonService.Generate(new[] { sea, bfi }, settings);

		Assert.Equal(3, result.FilesWritten.Count);
		return Path.GetDirectoryName(result.FilesWritten[0])!;
	}

	/// <summary>Reads the <c>properties</c> object of every Feature in <c>&lt;fileStem&gt;.geojson</c>.</summary>
	private static List<JsonElement> Features(string directory, string fileStem)
	{
		using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, $"{fileStem}.geojson")));

		return document.RootElement.GetProperty("features").EnumerateArray()
			.Select(feature => feature.GetProperty("properties").Clone())
			.ToList();
	}

	/// <summary>
	/// The names of every <c>feb.*</c> property on a Feature. A Feature with no attributes may be
	/// serialized with <c>null</c> properties, which has none.
	/// </summary>
	private static string[] FebNames(JsonElement properties) =>
		properties.ValueKind == JsonValueKind.Object
			? properties.EnumerateObject()
				.Select(property => property.Name)
				.Where(name => name.StartsWith("feb.", StringComparison.Ordinal))
				.ToArray()
			: Array.Empty<string>();

	private static string[] Strings(JsonElement array) =>
		array.EnumerateArray().Select(item => item.GetString()!).ToArray();
}
