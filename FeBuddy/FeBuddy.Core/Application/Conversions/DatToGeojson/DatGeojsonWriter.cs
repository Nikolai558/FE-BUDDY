using FeBuddy.Core.Application.Conversions.DatToGeojson.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Geojson;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Application.Conversions.DatToGeojson;

/// <summary>
/// Writes one converted video map: <c>…\DAT to GeoJSON\&lt;source name&gt;.geojson</c>, holding
/// the optional <c>isLineDefaults</c> Feature and one MultiLineString Feature with every line.
/// </summary>
/// <remarks>
/// One Feature for the whole map keeps the file small and lets CRC treat the map as one object;
/// the lines themselves are kept exactly as the <c>.dat</c> file drew them.
/// </remarks>
public static class DatGeojsonWriter
{
	/// <summary>The conversion's folder name inside the output directory.</summary>
	public const string RootFolder = "DAT to GeoJSON";

	/// <summary>The folder every converted file goes in.</summary>
	/// <param name="settings">The parsed settings.</param>
	/// <returns>The directory.</returns>
	public static string OutputDirectory(DatToGeojsonSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		return ServiceOutputPaths.Resolve(settings.OutputDirectory, settings.AddFeBuddyOutputFolder, RootFolder);
	}

	/// <summary>Writes one video map, or nothing when it has no lines left to draw.</summary>
	/// <param name="lines">The lines to write, already cropped and split.</param>
	/// <param name="sourcePath">The <c>.dat</c> file they came from; its name becomes the GeoJSON file's.</param>
	/// <param name="settings">The parsed settings.</param>
	/// <param name="files">The run's file set, which does the writing and remembers what was written.</param>
	/// <returns>The file written, or <see langword="null"/> when <paramref name="lines"/> is empty.</returns>
	public static string? Write(
		IReadOnlyList<LineString> lines,
		string sourcePath,
		DatToGeojsonSettings settings,
		GeojsonFileSet files)
	{
		ArgumentNullException.ThrowIfNull(lines);
		ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(files);

		if (lines.Count == 0)
		{
			return null;
		}

		FeatureCollection collection = [];

		if (settings.IncludeCrcLineDefaults && settings.LineDefaults is { } defaults)
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(defaults));
		}

		collection.Add(new Feature(Wgs84.Factory.CreateMultiLineString([.. lines]), new AttributesTable()));

		// One rendered Feature, so the file set always writes it.
		files.Write(collection, 1, OutputDirectory(settings), $"{Path.GetFileNameWithoutExtension(sourcePath)}.geojson");

		return files.FilesWritten[^1];
	}
}
