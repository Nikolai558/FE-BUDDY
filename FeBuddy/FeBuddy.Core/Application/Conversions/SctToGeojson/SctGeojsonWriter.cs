using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Application.Conversions.SctToGeojson.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Sct.Models;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Application.Conversions.SctToGeojson;

/// <summary>
/// Writes one converted sector file into <c>…\SCT2 to GeoJSON\&lt;source name&gt;\</c>:
/// <c>ARTCC</c>, <c>ARTCC-HIGH</c>, <c>ARTCC-LOW</c>, <c>LOW-AIRWAY</c>, <c>HIGH-AIRWAY</c>,
/// <c>GEO</c>, <c>LABELS</c> and <c>REGIONS</c> <c>.geojson</c>, and one file per diagram in
/// <c>SID\</c> and <c>STAR\</c>. A section with nothing to draw writes no file.
/// </summary>
/// <remarks>
/// <para>
/// A sector file draws every line as separate two-point segments. They are joined back into
/// lines (<see cref="SegmentJoiner"/>), so the output is smaller and dashed styles stay dashed.
/// </para>
/// <para>
/// In the boundary and airway files each name (<c>ZOB</c>, <c>V14</c>) is one Feature, so a
/// boundary is never joined onto the one beside it.
/// </para>
/// </remarks>
public static class SctGeojsonWriter
{
	/// <summary>The conversion's folder name inside the output directory.</summary>
	public const string RootFolder = "SCT2 to GeoJSON";

	private static readonly IReadOnlyDictionary<SctLineSection, string> LineFileNames = new Dictionary<SctLineSection, string>
	{
		[SctLineSection.Artcc] = "ARTCC",
		[SctLineSection.ArtccHigh] = "ARTCC-HIGH",
		[SctLineSection.ArtccLow] = "ARTCC-LOW",
		[SctLineSection.LowAirway] = "LOW-AIRWAY",
		[SctLineSection.HighAirway] = "HIGH-AIRWAY",
		[SctLineSection.Geo] = "GEO",
	};

	/// <summary>The folder every converted sector file's own folder goes in.</summary>
	/// <param name="settings">The parsed settings.</param>
	/// <returns>The directory.</returns>
	public static string OutputDirectory(ConversionSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		return ServiceOutputPaths.Resolve(settings.OutputDirectory, settings.AddFeBuddyOutputFolder, RootFolder);
	}

	/// <summary>Writes every file one sector file converts to.</summary>
	/// <param name="sctFile">The sector file's content.</param>
	/// <param name="settings">The parsed settings.</param>
	/// <param name="files">The run's file set, which does the writing and remembers what was written.</param>
	/// <returns>The files written for this sector file, and how many rendered Features they hold.</returns>
	public static (IReadOnlyList<string> Paths, int FeatureCount) Write(
		SctFile sctFile,
		SctToGeojsonSettings settings,
		GeojsonFileSet files)
	{
		ArgumentNullException.ThrowIfNull(sctFile);
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(files);

		int before = files.FilesWritten.Count;
		string directory = Path.Combine(OutputDirectory(settings), ConversionFiles.SafeFileName(Path.GetFileNameWithoutExtension(sctFile.SourcePath)));

		foreach ((SctLineSection section, IReadOnlyList<SctSegment> segments) in sctFile.Lines)
		{
			WriteLines(ByName(segments), settings, files, directory, LineFileNames[section]);
		}

		WriteDiagrams(sctFile.Sids, settings, files, Path.Combine(directory, "SID"));
		WriteDiagrams(sctFile.Stars, settings, files, Path.Combine(directory, "STAR"));
		WriteLabels(sctFile.Labels, settings, files, directory);
		WriteRegions(sctFile.Regions, files, directory);

		string[] written = [.. files.FilesWritten.Skip(before)];
		return (written, written.Sum(path => files.RenderedFeatureCountsByFile[path]));
	}

	/// <summary>Groups segments by name, case-insensitively, in the order each name first appears.</summary>
	private static IEnumerable<(string Name, IReadOnlyList<SctSegment> Segments)> ByName(IReadOnlyList<SctSegment> segments) =>
		segments
			.GroupBy(segment => segment.Name, StringComparer.OrdinalIgnoreCase)
			.Select(group => (group.Key, (IReadOnlyList<SctSegment>)[.. group]));

	/// <summary>One file of lines, one MultiLineString Feature per name.</summary>
	private static void WriteLines(
		IEnumerable<(string Name, IReadOnlyList<SctSegment> Segments)> groups,
		SctToGeojsonSettings settings,
		GeojsonFileSet files,
		string directory,
		string name)
	{
		FeatureCollection collection = [];

		if (settings.IncludeCrcLineDefaults && settings.LineDefaults is { } defaults)
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(defaults));
		}

		int rendered = 0;

		foreach ((string _, IReadOnlyList<SctSegment> segments) in groups)
		{
			IReadOnlyList<LineString> lines = SegmentJoiner.Join(segments.Select(segment => (segment.Start, segment.End)));

			if (lines.Count > 0)
			{
				collection.Add(new Feature(Wgs84.Factory.CreateMultiLineString([.. lines]), new AttributesTable()));
				rendered++;
			}
		}

		files.Write(collection, rendered, directory, $"{name}.geojson");
	}

	/// <summary>One file per SID or STAR diagram; diagrams sharing a name share a file.</summary>
	private static void WriteDiagrams(
		IReadOnlyList<SctSegment> segments,
		SctToGeojsonSettings settings,
		GeojsonFileSet files,
		string directory)
	{
		HashSet<string> used = new(StringComparer.OrdinalIgnoreCase);

		foreach ((string name, IReadOnlyList<SctSegment> diagram) in ByName(segments))
		{
			// Two names can differ only in characters a file name cannot hold.
			WriteLines([(name, diagram)], settings, files, directory, ConversionFiles.UniqueFileName(name, used));
		}
	}

	private static void WriteLabels(
		IReadOnlyList<SctLabel> labels,
		SctToGeojsonSettings settings,
		GeojsonFileSet files,
		string directory)
	{
		FeatureCollection collection = [];

		if (settings.IncludeCrcTextDefaults && settings.TextDefaults is { } defaults)
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(defaults));
		}

		foreach (SctLabel label in labels)
		{
			collection.Add(new Feature(
				Wgs84.Point(label.Position.Y, label.Position.X),
				new AttributesTable { { "text", new[] { label.Text } } }));
		}

		files.Write(collection, labels.Count, directory, "LABELS.geojson");
	}

	private static void WriteRegions(IReadOnlyList<SctRegion> regions, GeojsonFileSet files, string directory)
	{
		FeatureCollection collection = [];

		foreach (SctRegion region in regions)
		{
			Coordinate[] ring = [.. region.Points.Select(point => point.Copy()), region.Points[0].Copy()];

			// A region already closed on its first point keeps the ring as written.
			if (region.Points[^1].Equals2D(region.Points[0]))
			{
				ring = ring[..^1];
			}

			collection.Add(new Feature(Wgs84.Factory.CreatePolygon(ring), new AttributesTable()));
		}

		files.Write(collection, regions.Count, directory, "REGIONS.geojson");
	}
}
