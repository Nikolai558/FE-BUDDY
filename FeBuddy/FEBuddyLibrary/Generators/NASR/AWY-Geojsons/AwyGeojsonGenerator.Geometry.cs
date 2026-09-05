using FEBuddyLibrary.Handlers.CSV;
using FEBuddyLibrary.Models.NASR.CSV;
using NetTopologySuite.Geometries;

namespace FEBuddyLibrary.Generators.NASR;

public static partial class AwyGeojsonGenerator
{
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

		List<AirwaySegment> segmentList =
			segments.ToList();

		string? previousSegEndWptId = null;

		for (int i = 0; i < segmentList.Count; i++)
		{
			AirwaySegment segment =
				segmentList[i];

			string segStartWptId =
				segment.StartWptId;

			string segEndWptId =
				segment.EndWptId;

			var segStartCoordinates =
				FindWaypointCoordinates.GetCoordinates(
					allNasrCsvData,
					segStartWptId);

			/*
			 * If the starting waypoint cannot be resolved, determine
			 * whether usable airway geometry exists later.
			 *
			 * If nothing resolvable exists after this point, this segment
			 * belongs to an unresolved trailing portion of the airway.
			 * Stop processing and keep the geometry already built.
			 *
			 * If valid geometry exists later, then the unresolved waypoint
			 * occurs inside the airway and should be treated as an error.
			 */
			if (!segStartCoordinates.HasValue)
			{
				bool hasResolvableSegmentAhead =
					HasResolvableSegmentAhead(
						allNasrCsvData,
						segmentList,
						i + 1);

				if (!hasResolvableSegmentAhead)
				{
					break;
				}

				throw new InvalidOperationException(
					$"Unable to locate coordinates for airway '{awyId}' " +
					$"segment start waypoint '{segStartWptId}'.");
			}

			var segEndCoordinates =
				FindWaypointCoordinates.GetCoordinates(
					allNasrCsvData,
					segEndWptId);

			/*
			 * Apply the same rule to an unresolved ending waypoint.
			 *
			 * If no usable geometry exists later, stop the airway at
			 * the last valid point.
			 *
			 * Example:
			 *
			 *      CFQLS -> CFGFX
			 *      CFGFX -> U.S. CANADIAN BORDER-4
			 *
			 * becomes:
			 *
			 *      CFQLS -> CFGFX
			 */
			if (!segEndCoordinates.HasValue)
			{
				bool hasResolvableSegmentAhead =
					HasResolvableSegmentAhead(
						allNasrCsvData,
						segmentList,
						i + 1);

				if (!hasResolvableSegmentAhead)
				{
					break;
				}

				throw new InvalidOperationException(
					$"Unable to locate coordinates for airway '{awyId}' " +
					$"segment end waypoint '{segEndWptId}'.");
			}

			/*
			 * RFC 7946 GeoJSON coordinate order:
			 *
			 *      [longitude, latitude]
			 *
			 * NetTopologySuite:
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
			 * 1. The previous segment ends at this segment's start point.
			 *
			 * 2. This segment is not marked as an airway gap.
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
			 * First valid segment of the current LineString.
			 */
			if (currentCoordinates.Count == 0)
			{
				currentCoordinates.Add(
					segStartCoordinate);

				currentCoordinates.Add(
					segEndCoordinate);
			}

			/*
			 * Continue the existing LineString.
			 *
			 * The segment's starting coordinate is already the final
			 * coordinate of the previous segment, so only add the endpoint.
			 */
			else if (isContinuous)
			{
				currentCoordinates.Add(
					segEndCoordinate);
			}

			/*
			 * Discontinuity or explicit airway gap.
			 *
			 * Finish the existing LineString and begin a new one.
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

			previousSegEndWptId =
				segEndWptId;
		}

		/*
		 * If processing stopped because of an unresolved trailing portion,
		 * the valid coordinates accumulated before it still need to be
		 * added to the output.
		 */
		if (currentCoordinates.Count >= 2)
		{
			lineStrings.Add(
				GeometryFactory.CreateLineString(
					currentCoordinates.ToArray()));
		}

		return lineStrings;
	}
}