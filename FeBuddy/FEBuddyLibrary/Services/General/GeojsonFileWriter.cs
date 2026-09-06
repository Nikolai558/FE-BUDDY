using System.Text.Json;

using FEBuddyLibrary.Configuration;

using NetTopologySuite.Features;
using NetTopologySuite.IO.Converters;

namespace FEBuddyLibrary.Services.General;

/// <summary>
/// Writes a GeoJSON <see cref="FeatureCollection"/> to disk following FE-Buddy's output
/// rules: single-line output by default, pretty-printed only when
/// <see cref="DevMode.IsEnabled"/> is <see langword="true"/>, and no file at all when there
/// is nothing worth writing.
/// </summary>
public static class GeojsonFileWriter
{
	/// <summary>
	/// Serializes <paramref name="collection"/> to RFC 7946 GeoJSON and writes it to
	/// <paramref name="directory"/>/<paramref name="fileName"/>.
	/// </summary>
	/// <param name="collection">The FeatureCollection to write.</param>
	/// <param name="renderedFeatureCount">
	/// How many of the Features in <paramref name="collection"/> are actually rendered
	/// content, as opposed to a non-rendered isDefaults Feature. Passed explicitly (rather
	/// than inferred from <c>collection.Count</c>) so a file containing only an isDefaults
	/// Feature and no real content is correctly treated as empty and not written, matching
	/// old FE-Buddy's behavior of skipping empty output files.
	/// </param>
	/// <param name="directory">The directory to write into. Created if it does not exist.</param>
	/// <param name="fileName">The file name to write, including extension.</param>
	/// <returns>
	/// The full path written, or <see langword="null"/> when
	/// <paramref name="renderedFeatureCount"/> is zero or less (nothing was written).
	/// </returns>
	public static string? Write(
		FeatureCollection collection,
		int renderedFeatureCount,
		string directory,
		string fileName)
	{
		ArgumentNullException.ThrowIfNull(collection);

		if (string.IsNullOrWhiteSpace(directory))
		{
			throw new ArgumentException(
				"Output directory cannot be null, empty, or whitespace.",
				nameof(directory));
		}

		if (string.IsNullOrWhiteSpace(fileName))
		{
			throw new ArgumentException(
				"File name cannot be null, empty, or whitespace.",
				nameof(fileName));
		}

		// A file with zero rendered features is not written, matching old FE-Buddy's
		// SerializeToFile behavior. An isDefaults-only file would draw nothing on ERAM and
		// is just noise on disk.
		if (renderedFeatureCount <= 0)
		{
			return null;
		}

		Directory.CreateDirectory(directory);

		JsonSerializerOptions jsonOptions = new()
		{
			// Single-line output saves disk space; DevMode trades that for readability.
			WriteIndented = DevMode.IsEnabled
		};

		jsonOptions.Converters.Add(new GeoJsonConverterFactory());

		string geoJson = JsonSerializer.Serialize(collection, jsonOptions);

		string outputPath = Path.Combine(directory, fileName);

		File.WriteAllText(outputPath, geoJson);

		return outputPath;
	}
}
