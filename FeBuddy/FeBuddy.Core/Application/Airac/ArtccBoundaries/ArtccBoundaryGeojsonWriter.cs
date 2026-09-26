using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;
using FeBuddy.Core.Domain.ArtccBoundaries;
using FeBuddy.Core.Domain.ArtccBoundaries.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.Geojson;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Application.Airac.ArtccBoundaries;

/// <summary>
/// Generates the ARTCC Boundaries GeoJSON output: Lines only, one Feature per
/// <see cref="ArtccBoundaryRing"/>, grouped per <see cref="ArtccBoundarySettings.OutputBy"/>.
/// </summary>
/// <remarks>
/// Each file is named <c>ARTCC-Boundary_&lt;group&gt;_Lines.geojson</c> (its name without the
/// extension is its file key, see <see cref="ArtccBoundaryOutputFiles"/>) and goes in the GeoJSON
/// folder, or the vNAS one when the user marked it for vNAS. Only a file chosen for CRC-ERAM
/// defaults gets an isDefaults Feature.
/// </remarks>
public static class ArtccBoundaryGeojsonWriter
{
	/// <summary>
	/// Generates every Lines file called for by <paramref name="settings"/>.
	/// </summary>
	/// <param name="rings">The rings to render (already built and, if applicable, location-filtered).</param>
	/// <param name="settings">The parsed ARTCC Boundaries settings.</param>
	/// <returns>The files written and how many rendered Features each holds.</returns>
	public static GeojsonFileSet Generate(IReadOnlyList<ArtccBoundaryRing> rings, ArtccBoundarySettings settings)
	{
		ArgumentNullException.ThrowIfNull(rings);
		ArgumentNullException.ThrowIfNull(settings);

		GeojsonFileSet files = new(settings.CoordinatePrecision);

		if (rings.Count == 0)
		{
			return files;
		}

		switch (settings.OutputBy)
		{
			case ArtccBoundaryOutputBy.HighLow:
				GenerateHighLow(rings, settings, files);
				break;

			case ArtccBoundaryOutputBy.HighLowUnlimited:
				GenerateHighLowUnlimited(rings, settings, files);
				break;

			case ArtccBoundaryOutputBy.ArtccAltitude:
				GenerateByLocationAltitude(rings, settings, files);
				break;
		}

		return files;
	}

	/// <summary>An UNLIMITED ring is written into both the High and the Low file.</summary>
	private static void GenerateHighLow(IReadOnlyList<ArtccBoundaryRing> rings, ArtccBoundarySettings settings, GeojsonFileSet files)
	{
		WriteGroup(
			OrderedRings(rings, ring => ring.Altitude is ArtccBoundaryAltitude.High or ArtccBoundaryAltitude.Unlimited),
			ArtccBoundaryOutputFiles.HighClass, settings, files);

		WriteGroup(
			OrderedRings(rings, ring => ring.Altitude is ArtccBoundaryAltitude.Low or ArtccBoundaryAltitude.Unlimited),
			ArtccBoundaryOutputFiles.LowClass, settings, files);
	}

	private static void GenerateHighLowUnlimited(IReadOnlyList<ArtccBoundaryRing> rings, ArtccBoundarySettings settings, GeojsonFileSet files)
	{
		WriteGroup(OrderedRings(rings, ring => ring.Altitude == ArtccBoundaryAltitude.High), ArtccBoundaryOutputFiles.HighClass, settings, files);
		WriteGroup(OrderedRings(rings, ring => ring.Altitude == ArtccBoundaryAltitude.Low), ArtccBoundaryOutputFiles.LowClass, settings, files);
		WriteGroup(OrderedRings(rings, ring => ring.Altitude == ArtccBoundaryAltitude.Unlimited), ArtccBoundaryOutputFiles.UnlimitedClass, settings, files);
	}

	private static void GenerateByLocationAltitude(IReadOnlyList<ArtccBoundaryRing> rings, ArtccBoundarySettings settings, GeojsonFileSet files)
	{
		foreach (IGrouping<(string LocationId, ArtccBoundaryAltitude Altitude), ArtccBoundaryRing> group in rings
			.GroupBy(ring => (ring.Location.LocationId, ring.Altitude))
			.OrderBy(group => group.Key.LocationId, StringComparer.OrdinalIgnoreCase)
			.ThenBy(group => group.Key.Altitude))
		{
			string altitudeToken = ArtccBoundaryAltitudes.NasrToken(group.Key.Altitude);
			string className = ArtccBoundaryOutputFiles.ClassFor(group.Key.LocationId, altitudeToken);
			string fileKey = ArtccBoundaryOutputFiles.KeyFor(group.Key.LocationId, altitudeToken);

			WriteFile([.. group], className, fileKey, settings, files);
		}
	}

	private static List<ArtccBoundaryRing> OrderedRings(IReadOnlyList<ArtccBoundaryRing> rings, Func<ArtccBoundaryRing, bool> predicate) =>
		[.. rings.Where(predicate).OrderBy(ring => ring.Location.LocationId, StringComparer.OrdinalIgnoreCase)];

	private static void WriteGroup(IReadOnlyList<ArtccBoundaryRing> rings, string className, ArtccBoundarySettings settings, GeojsonFileSet files) =>
		WriteFile(rings, className, ArtccBoundaryOutputFiles.KeyFor(className), settings, files);

	private static void WriteFile(
		IReadOnlyList<ArtccBoundaryRing> rings,
		string className,
		string fileKey,
		ArtccBoundarySettings settings,
		GeojsonFileSet files)
	{
		FeatureCollection collection = [];

		if (settings.Vnas.HasCrcDefaults(fileKey))
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(settings.LineDefaults[className]));
		}

		int renderedCount = 0;

		foreach (ArtccBoundaryRing ring in rings)
		{
			if (BuildGeometry(ring, settings) is not { } geometry)
			{
				// Clipped away entirely: no Feature for this ring in this file.
				continue;
			}

			AttributesTable attributes = [];
			FebProperties.Add(attributes, settings.IncludeFebCustomProperties, settings.FebProperties.OrderBy(p => p), property => ValueFor(ring, property));

			collection.Add(new Feature(geometry, attributes));
			renderedCount++;
		}

		string directory = AiracOutputPaths.FileDirectory(settings.OutputDirectory, isGeojson: true, settings.Vnas.IsUploaded(fileKey));
		files.Write(collection, renderedCount, directory, $"{fileKey}.geojson");
	}

	/// <summary>
	/// Builds a ring's rendered geometry: its closed line, split at the antimeridian and clipped
	/// to the ROI as configured. One resulting piece is a LineString, several a MultiLineString;
	/// <see langword="null"/> when clipping removes the ring entirely.
	/// </summary>
	private static Geometry? BuildGeometry(ArtccBoundaryRing ring, ArtccBoundarySettings settings)
	{
		LineString line = Wgs84.Factory.CreateLineString(
			[.. ring.Points.Select(point => new Coordinate(point.Longitude, point.Latitude))]);

		IReadOnlyList<LineString> lineStrings = settings.SplitAtAntimeridian
			? AntimeridianSplitter.Split(line)
			: [line];

		if (settings.Roi is not null)
		{
			lineStrings = RoiFilter.ClipLines(lineStrings, settings.Roi);
		}

		// The splitter and the clipper treat the ring as an open line, so they also cut it at its
		// own starting vertex; the piece ending there runs straight on into the piece starting there.
		lineStrings = JoinAtRingStart(lineStrings, line.StartPoint.Coordinate);

		if (lineStrings.Count == 0)
		{
			return null;
		}

		return lineStrings.Count == 1
			? lineStrings[0]
			: Wgs84.Factory.CreateMultiLineString([.. lineStrings]);
	}

	/// <summary>
	/// Joins the last piece onto the front of the first when they meet at the ring's own starting
	/// vertex: one continuous stretch of boundary that splitting or clipping a closed ring as an
	/// open line cuts in two for no geometric reason. ZAK's rings cross the antimeridian twice, so
	/// they come out as two pieces rather than three.
	/// </summary>
	/// <param name="pieces">The ring's pieces after splitting and clipping, in ring order.</param>
	/// <param name="ringStart">The ring's first (and last) coordinate.</param>
	/// <returns>The pieces, the first and last joined when they meet at <paramref name="ringStart"/>.</returns>
	private static IReadOnlyList<LineString> JoinAtRingStart(IReadOnlyList<LineString> pieces, Coordinate ringStart)
	{
		if (pieces.Count < 2)
		{
			return pieces;
		}

		LineString first = pieces[0];
		LineString last = pieces[^1];

		if (!first.StartPoint.Coordinate.Equals2D(ringStart) || !last.EndPoint.Coordinate.Equals2D(ringStart))
		{
			return pieces;
		}

		LineString joined = Wgs84.Factory.CreateLineString([.. last.Coordinates, .. first.Coordinates.Skip(1)]);
		return [joined, .. pieces.Skip(1).Take(pieces.Count - 2)];
	}

	private static object? ValueFor(ArtccBoundaryRing ring, ArtccBoundaryFebProperty property) => property switch
	{
		ArtccBoundaryFebProperty.LocationId => NullIfBlank(ring.Location.LocationId),
		ArtccBoundaryFebProperty.LocationName => NullIfBlank(ring.Location.LocationName),
		ArtccBoundaryFebProperty.LocationType => NullIfBlank(ring.Location.LocationType),
		ArtccBoundaryFebProperty.IcaoId => NullIfBlank(ring.Location.IcaoId),
		ArtccBoundaryFebProperty.ComputerId => NullIfBlank(ring.Location.ComputerId),
		ArtccBoundaryFebProperty.Altitude => ArtccBoundaryAltitudes.NasrToken(ring.Altitude),
		ArtccBoundaryFebProperty.Type => NullIfBlank(ring.Type),
		ArtccBoundaryFebProperty.City => NullIfBlank(ring.Location.City),
		ArtccBoundaryFebProperty.CountryCode => NullIfBlank(ring.Location.CountryCode),
		_ => null,
	};

	private static string? NullIfBlank(string value) => value.Length == 0 ? null : value;
}
