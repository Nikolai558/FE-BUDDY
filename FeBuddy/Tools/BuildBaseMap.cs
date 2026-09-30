// Builds the map's background outline - FeBuddy.Wpf/Assets/BaseMap/*.json, one file per layer the
// user can switch on - from Natural Earth (public domain, https://www.naturalearthdata.com).
// Run from anywhere in the repository:
//
//     dotnet run FeBuddy/Tools/BuildBaseMap.cs
//
// It downloads the pinned release's 1:50m layers, keeps only the lines the map draws, rounds the
// coordinates and writes one small GeoJSON file per layer. Change the release or the filters below
// and run it again to rebuild the assets.

using System.Globalization;
using System.Text;
using System.Text.Json;

const string Release = "v5.1.2";
const string SourceUrl = "https://raw.githubusercontent.com/nvkelso/natural-earth-vector/" + Release + "/geojson/";

// Four decimal places is about 10 m - far finer than 1:50m data is accurate to.
const int Decimals = 4;

// The US territories, drawn with the states (Natural Earth lists them as countries of their own).
string[] territories = ["PRI", "VIR", "GUM", "MNP", "ASM"];

// Lakes: only the largest (Natural Earth's own "show from zoom 2" set, which has the Great Lakes).
const double LakesMaxMinZoom = 2.0;

(string Output, (string File, Func<JsonElement, bool> Keep)[] Sources)[] layers =
[
	// Whole state outlines, coast included, so the layer stands on its own. The "lakes" edition
	// leaves the Great Lakes out of the states, so no state line runs through them.
	("us-states",
	[
		("ne_50m_admin_1_states_provinces_lakes", p => p.GetProperty("adm0_a3").GetString() == "USA"),
		("ne_50m_admin_0_countries", p => territories.Contains(p.GetProperty("ADM0_A3").GetString())),
	]),
	("coastlines",
	[
		("ne_50m_coastline", _ => true),
		("ne_50m_lakes", p => p.GetProperty("min_zoom").GetDouble() <= LakesMaxMinZoom),
	]),
];

string folder = Path.Combine(FindRepositoryRoot(), "FeBuddy", "FeBuddy.Wpf", "Assets", "BaseMap");
Directory.CreateDirectory(folder);

using HttpClient http = new();
foreach ((string name, (string File, Func<JsonElement, bool> Keep)[] sources) in layers)
{
	StringBuilder json = new();
	json.Append("{\"type\":\"FeatureCollection\",")
		.Append("\"source\":\"Natural Earth 1:50m ").Append(Release)
		.Append(" (public domain). Rebuild with: dotnet run FeBuddy/Tools/BuildBaseMap.cs\",")
		.Append("\"features\":[\n");

	int totalLines = 0, totalPoints = 0;
	for (int s = 0; s < sources.Length; s++)
	{
		(string file, Func<JsonElement, bool> keep) = sources[s];
		Console.WriteLine($"Downloading {file}...");
		using JsonDocument doc = JsonDocument.Parse(await http.GetStringAsync(SourceUrl + file + ".geojson"));

		List<List<(double Lon, double Lat)>> lines = [];
		foreach (JsonElement feature in doc.RootElement.GetProperty("features").EnumerateArray())
		{
			if (keep(feature.GetProperty("properties")))
			{
				AddLines(feature.GetProperty("geometry"), lines);
			}
		}

		json.Append("{\"type\":\"Feature\",\"properties\":{\"layer\":\"").Append(file)
			.Append("\"},\"geometry\":{\"type\":\"MultiLineString\",\"coordinates\":[\n");
		for (int i = 0; i < lines.Count; i++)
		{
			json.Append('[');
			for (int j = 0; j < lines[i].Count; j++)
			{
				json.Append(j == 0 ? "[" : ",[")
					.Append(lines[i][j].Lon.ToString(CultureInfo.InvariantCulture)).Append(',')
					.Append(lines[i][j].Lat.ToString(CultureInfo.InvariantCulture)).Append(']');
			}

			json.Append(i == lines.Count - 1 ? "]\n" : "],\n");
		}

		json.Append(s == sources.Length - 1 ? "]}}\n" : "]}},\n");

		int points = lines.Sum(l => l.Count);
		Console.WriteLine($"  {lines.Count} lines, {points} points");
		totalLines += lines.Count;
		totalPoints += points;
	}

	json.Append("]}\n");
	string output = Path.Combine(folder, name + ".json");
	File.WriteAllText(output, json.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
	Console.WriteLine($"Wrote {output}: {totalLines} lines, {totalPoints} points, {new FileInfo(output).Length / 1024} KB");
}

// Every line and polygon ring in a geometry, rounded, as a list of lines to stroke.
static void AddLines(JsonElement geometry, List<List<(double Lon, double Lat)>> into)
{
	JsonElement coordinates = geometry.GetProperty("coordinates");
	switch (geometry.GetProperty("type").GetString())
	{
		case "LineString":
			AddLine(coordinates, into);
			break;
		case "MultiLineString" or "Polygon":
			foreach (JsonElement line in coordinates.EnumerateArray())
			{
				AddLine(line, into);
			}

			break;
		case "MultiPolygon":
			foreach (JsonElement polygon in coordinates.EnumerateArray())
			{
				foreach (JsonElement ring in polygon.EnumerateArray())
				{
					AddLine(ring, into);
				}
			}

			break;
		default:
			throw new InvalidDataException($"Unexpected geometry type {geometry.GetProperty("type")}");
	}
}

// One line, rounded, without the repeated points rounding leaves; dropped if nothing is left to draw.
static void AddLine(JsonElement positions, List<List<(double Lon, double Lat)>> into)
{
	List<(double Lon, double Lat)> line = [];
	foreach (JsonElement position in positions.EnumerateArray())
	{
		(double Lon, double Lat) point = (
			Math.Round(position[0].GetDouble(), Decimals),
			Math.Round(position[1].GetDouble(), Decimals));
		if (line.Count == 0 || line[^1] != point)
		{
			line.Add(point);
		}
	}

	if (line.Count >= 2)
	{
		into.Add(line);
	}
}

// The folder holding FeBuddy/FeBuddy.sln, found by walking up from the current folder.
static string FindRepositoryRoot()
{
	for (DirectoryInfo? folder = new(Directory.GetCurrentDirectory()); folder is not null; folder = folder.Parent)
	{
		if (File.Exists(Path.Combine(folder.FullName, "FeBuddy", "FeBuddy.sln")))
		{
			return folder.FullName;
		}
	}

	throw new DirectoryNotFoundException("Run this from inside the FE-BUDDY repository.");
}
