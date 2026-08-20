using FEBuddyLibrary.Handlers.CSV;
using FEBuddyLibrary.Models.NASR.CSV;
using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using System.Text.Json;

namespace FEBuddyLibrary.Generators.NASR;

/// <summary>
/// Generates GeoJSON airway geometry from parsed NASR AWY CSV data.
/// </summary>
public static class AwyGeojsonGenerator
{
	private static readonly GeometryFactory GeometryFactory =
		NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

	/// <summary>
	/// Generates an RFC 7946 GeoJSON FeatureCollection containing NASR airways.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data.</param>
	/// <param name="outputDirectory">Directory where the GeoJSON file will be written.</param>
	/// <param name="fileName">Name of the generated GeoJSON file.</param>
	/// <returns>The full path to the generated GeoJSON file.</returns>
	public static string Generate(
		NasrCsvDataCollection allNasrCsvData,
		string outputDirectory,
		string fileName = "AWY.geojson")
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);

		if (string.IsNullOrWhiteSpace(outputDirectory))
		{
			throw new ArgumentException(
				"Output directory cannot be null, empty, or whitespace.",
				nameof(outputDirectory));
		}

		if (string.IsNullOrWhiteSpace(fileName))
		{
			throw new ArgumentException(
				"File name cannot be null, empty, or whitespace.",
				nameof(fileName));
		}

		if (allNasrCsvData.Awy is null)
		{
			throw new InvalidOperationException(
				"AWY NASR CSV data has not been parsed.");
		}

		Directory.CreateDirectory(outputDirectory);

		FeatureCollection featureCollection = new();

		/*
         * Build a dictionary of all airway IDs from AWY_BASE.
         *
         * Key:
         *      AwyId
         *
         * Value:
         *      AwyDesignation
         *
         * AwyDesignation is retained for future generator updates,
         * but is not currently written to the GeoJSON properties.
         *
         * GroupBy is used so duplicate AWY_BASE records with the same
         * AwyId do not cause ToDictionary() to throw an exception.
         */
		Dictionary<string, string> airways =
			allNasrCsvData.Awy.AwyBase
				.Where(x => !string.IsNullOrWhiteSpace(x.AwyId))
				.GroupBy(
					x => x.AwyId.Trim(),
					StringComparer.OrdinalIgnoreCase)
				.ToDictionary(
					group => group.Key,
					group => group.First().AwyDesignation,
					StringComparer.OrdinalIgnoreCase);

		/*
         * Group AWY_SEG_ALT records by airway ID once.
         */
		ILookup<string, AwyCsvDataModel.AwySegAlt> airwaySegments =
			allNasrCsvData.Awy.AwySegAlt
				.Where(x => !string.IsNullOrWhiteSpace(x.AwyId))
				.ToLookup(
					x => x.AwyId.Trim(),
					StringComparer.OrdinalIgnoreCase);

		foreach (KeyValuePair<string, string> airway in airways)
		{
			string awyId = airway.Key;

			// Retained for future use.
			string awyType = airway.Value;

			/*
             * POINT_SEQ defines the order of the airway segment records.
             */
			List<AwyCsvDataModel.AwySegAlt> rawSegments =
				airwaySegments[awyId]
					.OrderBy(x => x.PointSeq)
					.ToList();

			/*
             * Normalize the raw AWY_SEG_ALT records before attempting
             * coordinate lookups.
             *
             * Records whose FromPtType is null/empty are treated as
             * reference-only points and are collapsed out.
             */
			List<AirwaySegment> normalizedSegments =
				NormalizeAirwaySegments(rawSegments);

			List<LineString> lineStrings =
				BuildAirwayLineStrings(
					allNasrCsvData,
					awyId,
					normalizedSegments);

			/*
             * No usable segment geometry means there is nothing to
             * write for this airway.
             */
			if (lineStrings.Count == 0)
			{
				continue;
			}

			Geometry geometry =
				lineStrings.Count == 1
					? lineStrings[0]
					: GeometryFactory.CreateMultiLineString(lineStrings.ToArray());

			AttributesTable properties = new();
			properties.Add("feb_AWY-ID", awyId);

			Feature feature = new(
				geometry,
				properties);

			featureCollection.Add(feature);
		}

		JsonSerializerOptions jsonOptions = new()
		{
			WriteIndented = true
		};

		jsonOptions.Converters.Add(
			new GeoJsonConverterFactory());

		string geoJson = JsonSerializer.Serialize(
			featureCollection,
			jsonOptions);

		string outputPath = Path.Combine(
			outputDirectory,
			fileName);

		File.WriteAllText(
			outputPath,
			geoJson);

		return outputPath;
	}

	/// <summary>
	/// Removes reference-only airway points whose FromPtType is null or empty
	/// and collapses the surrounding records into a direct segment.
	/// </summary>
	/// <param name="rawSegments">Raw AWY_SEG_ALT records for one airway.</param>
	/// <returns>A normalized list of airway segments.</returns>
	private static List<AirwaySegment> NormalizeAirwaySegments(
		IReadOnlyList<AwyCsvDataModel.AwySegAlt> rawSegments)
	{
		List<AirwaySegment> normalizedSegments = new();

		for (int i = 0; i < rawSegments.Count; i++)
		{
			AwyCsvDataModel.AwySegAlt currentSegment = rawSegments[i];

			/*
             * If this record starts at a point with no FromPtType,
             * the FromPoint is not treated as a real NASR waypoint.
             *
             * Do not create a separate segment beginning at that point.
             * A preceding valid segment can consume this record while
             * looking ahead.
             */
			if (string.IsNullOrWhiteSpace(currentSegment.FromPtType))
			{
				continue;
			}

			if (string.IsNullOrWhiteSpace(currentSegment.FromPoint) ||
				string.IsNullOrWhiteSpace(currentSegment.ToPoint))
			{
				continue;
			}

			string startWptId = currentSegment.FromPoint.Trim();
			string endWptId = currentSegment.ToPoint.Trim();

			bool isGap = IsGap(currentSegment.AwySegGapFlag);

			/*
             * Look ahead for one or more reference-only points.
             *
             * Example:
             *
             *      TIJ -> U.S. MEXICAN BORDER-2
             *      U.S. MEXICAN BORDER-2 -> TEYON
             *
             * where the second record has an empty FromPtType.
             *
             * This becomes:
             *
             *      TIJ -> TEYON
             *
             * The reference-only waypoint is never sent to
             * FindWaypointCoordinates.
             */
			int nextIndex = i + 1;

			while (nextIndex < rawSegments.Count)
			{
				AwyCsvDataModel.AwySegAlt nextSegment =
					rawSegments[nextIndex];

				bool nextStartMatchesCurrentEnd =
					string.Equals(
						endWptId,
						nextSegment.FromPoint?.Trim(),
						StringComparison.OrdinalIgnoreCase);

				bool nextStartIsReferenceOnly =
					string.IsNullOrWhiteSpace(nextSegment.FromPtType);

				if (!nextStartMatchesCurrentEnd ||
					!nextStartIsReferenceOnly)
				{
					break;
				}

				/*
                 * If any record being collapsed is marked as an airway
                 * gap, preserve that gap on the resulting normalized segment.
                 */
				if (IsGap(nextSegment.AwySegGapFlag))
				{
					isGap = true;
				}

				if (string.IsNullOrWhiteSpace(nextSegment.ToPoint))
				{
					break;
				}

				endWptId = nextSegment.ToPoint.Trim();
				nextIndex++;
			}

			normalizedSegments.Add(
				new AirwaySegment(
					startWptId,
					endWptId,
					isGap));
		}

		return normalizedSegments;
	}

	/// <summary>
	/// Builds all continuous LineStrings belonging to a single airway.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data.</param>
	/// <param name="awyId">The airway identifier being processed.</param>
	/// <param name="segments">Normalized segments belonging to the airway.</param>
	/// <returns>One or more LineStrings representing the airway.</returns>
	private static List<LineString> BuildAirwayLineStrings(
		NasrCsvDataCollection allNasrCsvData,
		string awyId,
		IEnumerable<AirwaySegment> segments)
	{
		List<LineString> lineStrings = new();
		List<Coordinate> currentCoordinates = new();

		string? previousSegEndWptId = null;

		foreach (AirwaySegment segment in segments)
		{
			string segStartWptId = segment.StartWptId;
			string segEndWptId = segment.EndWptId;

			var segStartCoordinates =
				FindWaypointCoordinates.GetCoordinates(
					allNasrCsvData,
					segStartWptId);

			if (!segStartCoordinates.HasValue)
			{
				throw new InvalidOperationException(
					$"Unable to locate coordinates for airway '{awyId}' " +
					$"segment start waypoint '{segStartWptId}'.");
			}

			var segEndCoordinates =
				FindWaypointCoordinates.GetCoordinates(
					allNasrCsvData,
					segEndWptId);

			if (!segEndCoordinates.HasValue)
			{
				throw new InvalidOperationException(
					$"Unable to locate coordinates for airway '{awyId}' " +
					$"segment end waypoint '{segEndWptId}'.");
			}

			/*
             * RFC 7946 / GeoJSON position order:
             *
             *      [longitude, latitude]
             *
             * NetTopologySuite Coordinate:
             *
             *      X = longitude
             *      Y = latitude
             */
			Coordinate segStartCoordinate = new(
				segStartCoordinates.Value.waypointLon,
				segStartCoordinates.Value.waypointLat);

			Coordinate segEndCoordinate = new(
				segEndCoordinates.Value.waypointLon,
				segEndCoordinates.Value.waypointLat);

			/*
             * Continue the current LineString only when:
             *
             * 1. The previous segment's end waypoint equals this
             *    segment's start waypoint.
             *
             * 2. The new segment is not marked as a gap.
             */
			bool isContinuous =
				previousSegEndWptId is not null
				&&
				string.Equals(
					previousSegEndWptId,
					segStartWptId,
					StringComparison.OrdinalIgnoreCase)
				&&
				!segment.IsGap;

			/*
             * First segment of the current LineString.
             */
			if (currentCoordinates.Count == 0)
			{
				currentCoordinates.Add(segStartCoordinate);
				currentCoordinates.Add(segEndCoordinate);
			}

			/*
             * The start point is already the final point of the
             * current LineString, so only append the new endpoint.
             */
			else if (isContinuous)
			{
				currentCoordinates.Add(segEndCoordinate);
			}

			/*
             * The airway is discontinuous here or the new segment is
             * explicitly marked as a gap.
             *
             * Finish the current LineString and start another one.
             */
			else
			{
				lineStrings.Add(
					GeometryFactory.CreateLineString(
						currentCoordinates.ToArray()));

				currentCoordinates = new List<Coordinate>
				{
					segStartCoordinate,
					segEndCoordinate
				};
			}

			previousSegEndWptId = segEndWptId;
		}

		/*
         * Add the final LineString after processing all segments.
         */
		if (currentCoordinates.Count >= 2)
		{
			lineStrings.Add(
				GeometryFactory.CreateLineString(
					currentCoordinates.ToArray()));
		}

		return lineStrings;
	}

	/// <summary>
	/// Determines whether an AWY_SEG_ALT record is marked as an airway gap.
	/// </summary>
	private static bool IsGap(string? awySegGapFlag)
	{
		return string.Equals(
			awySegGapFlag?.Trim(),
			"Y",
			StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Represents a normalized airway segment whose start and end IDs
	/// are expected to resolve to actual NASR waypoint coordinates.
	/// </summary>
	private sealed record AirwaySegment(
		string StartWptId,
		string EndWptId,
		bool IsGap);
}
