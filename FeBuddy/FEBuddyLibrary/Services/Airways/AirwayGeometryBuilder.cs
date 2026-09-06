using FEBuddyLibrary.Handlers.CSV;
using FEBuddyLibrary.Models.NASR.CSV;
using FEBuddyLibrary.Models.Services.Airways;

using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace FEBuddyLibrary.Services.Airways;

/// <summary>
/// Builds the rendered LineString geometry for a single airway from its normalized segments.
/// </summary>
public static class AirwayGeometryBuilder
{
	/// <summary>
	/// Shared geometry factory (SRID 4326 / WGS84) used across the Airways services, so
	/// antimeridian splitting, ROI clipping, and waypoint buffering all operate on geometry
	/// built from the same factory.
	/// </summary>
	public static readonly GeometryFactory GeometryFactory =
		NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

	/// <summary>
	/// Builds all continuous LineStrings belonging to a single airway.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data.</param>
	/// <param name="awyId">The airway identifier being processed (used only in warning text).</param>
	/// <param name="segments">Normalized segments belonging to the airway.</param>
	/// <returns>The airway's LineStrings, plus any warnings encountered while resolving waypoints.</returns>
	/// <remarks>
	/// Unlike the original implementation this was ported from, an unresolvable waypoint that
	/// occurs in the middle of an airway (i.e. one where usable geometry exists later in the
	/// segment list) no longer aborts the entire airway with an exception. It is recorded as
	/// a warning, the problem segment is skipped, and geometry building continues - treating
	/// the break the same way an explicit airway gap is treated. This keeps one bad NASR
	/// record from aborting processing of an otherwise-good airway (see the build plan's
	/// "warnings, not crashes" rule).
	///
	/// An unresolvable waypoint that occurs at the *trailing end* of an airway (nothing
	/// resolvable exists after it) is not a warning - it is the normal, expected case of an
	/// airway continuing past the edge of available NASR waypoint data (e.g. a segment ending
	/// at "U.S. CANADIAN BORDER-4"). That case still simply stops the airway at the last
	/// resolvable point, unchanged from the original behavior.
	/// </remarks>
	public static AirwayGeometryBuildResult Build(
		NasrCsvDataCollection allNasrCsvData,
		string awyId,
		IEnumerable<AirwaySegment> segments)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);
		ArgumentNullException.ThrowIfNull(segments);

		List<LineString> lineStrings = new();
		List<string> warnings = new();
		List<Coordinate> currentCoordinates = new();

		List<AirwaySegment> segmentList = segments.ToList();

		string? previousSegEndWptId = null;

		for (int i = 0; i < segmentList.Count; i++)
		{
			AirwaySegment segment = segmentList[i];

			string segStartWptId = segment.StartWptId;
			string segEndWptId = segment.EndWptId;

			var segStartCoordinates =
				FindWaypointCoordinates.GetCoordinates(allNasrCsvData, segStartWptId);

			if (!segStartCoordinates.HasValue)
			{
				bool hasResolvableSegmentAhead =
					HasResolvableSegmentAhead(allNasrCsvData, segmentList, i + 1);

				if (!hasResolvableSegmentAhead)
				{
					// Normal trailing-tail truncation: stop here, keep what was already built.
					break;
				}

				// A genuinely unresolvable point occurs mid-airway. Warn, skip this segment
				// as if it were a gap, and keep processing the rest of the airway.
				warnings.Add(
					$"Airway '{awyId}': unable to locate coordinates for segment start " +
					$"waypoint '{segStartWptId}'. This segment was skipped.");

				FinishCurrentLineString(lineStrings, currentCoordinates);
				currentCoordinates = new List<Coordinate>();
				previousSegEndWptId = null;
				continue;
			}

			var segEndCoordinates =
				FindWaypointCoordinates.GetCoordinates(allNasrCsvData, segEndWptId);

			if (!segEndCoordinates.HasValue)
			{
				bool hasResolvableSegmentAhead =
					HasResolvableSegmentAhead(allNasrCsvData, segmentList, i + 1);

				if (!hasResolvableSegmentAhead)
				{
					// Normal trailing-tail truncation: stop here, keep what was already built.
					break;
				}

				// A genuinely unresolvable point occurs mid-airway. Warn, skip this segment
				// as if it were a gap, and keep processing the rest of the airway.
				warnings.Add(
					$"Airway '{awyId}': unable to locate coordinates for segment end " +
					$"waypoint '{segEndWptId}'. This segment was skipped.");

				FinishCurrentLineString(lineStrings, currentCoordinates);
				currentCoordinates = new List<Coordinate>();
				previousSegEndWptId = null;
				continue;
			}

			// RFC 7946 GeoJSON coordinate order: [longitude, latitude].
			// NetTopologySuite: X = longitude, Y = latitude.
			Coordinate segStartCoordinate = new(
				segStartCoordinates.Value.waypointLon,
				segStartCoordinates.Value.waypointLat);

			Coordinate segEndCoordinate = new(
				segEndCoordinates.Value.waypointLon,
				segEndCoordinates.Value.waypointLat);

			/*
			 * Continue the current LineString only when:
			 * 1. The previous segment ends at this segment's start point.
			 * 2. This segment is not marked as an airway gap.
			 */
			bool isContinuous =
				previousSegEndWptId is not null &&
				string.Equals(previousSegEndWptId, segStartWptId, StringComparison.OrdinalIgnoreCase) &&
				!segment.IsGap;

			if (currentCoordinates.Count == 0)
			{
				// First valid segment of the current LineString.
				currentCoordinates.Add(segStartCoordinate);
				currentCoordinates.Add(segEndCoordinate);
			}
			else if (isContinuous)
			{
				// The segment's starting coordinate is already the final coordinate of the
				// previous segment, so only add the endpoint.
				currentCoordinates.Add(segEndCoordinate);
			}
			else
			{
				// Discontinuity or explicit airway gap: finish the existing LineString and
				// begin a new one.
				FinishCurrentLineString(lineStrings, currentCoordinates);
				currentCoordinates = new List<Coordinate> { segStartCoordinate, segEndCoordinate };
			}

			previousSegEndWptId = segEndWptId;
		}

		// If processing stopped because of an unresolved trailing portion, the valid
		// coordinates accumulated before it still need to be added to the output.
		FinishCurrentLineString(lineStrings, currentCoordinates);

		return new AirwayGeometryBuildResult(lineStrings, warnings);
	}

	/// <summary>
	/// Appends the accumulated coordinates as a LineString, when there are enough of them to
	/// form one.
	/// </summary>
	private static void FinishCurrentLineString(List<LineString> lineStrings, List<Coordinate> currentCoordinates)
	{
		if (currentCoordinates.Count >= 2)
		{
			lineStrings.Add(GeometryFactory.CreateLineString(currentCoordinates.ToArray()));
		}
	}

	/// <summary>
	/// Determines whether any later airway segment has both a resolvable starting waypoint
	/// and ending waypoint.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data.</param>
	/// <param name="segments">The normalized airway segments.</param>
	/// <param name="startIndex">The first segment index to examine.</param>
	/// <returns>
	/// True if a later segment has resolvable coordinates for both endpoints; otherwise false.
	/// </returns>
	private static bool HasResolvableSegmentAhead(
		NasrCsvDataCollection allNasrCsvData,
		IReadOnlyList<AirwaySegment> segments,
		int startIndex)
	{
		for (int i = startIndex; i < segments.Count; i++)
		{
			AirwaySegment segment = segments[i];

			var startCoordinates =
				FindWaypointCoordinates.GetCoordinates(allNasrCsvData, segment.StartWptId);

			var endCoordinates =
				FindWaypointCoordinates.GetCoordinates(allNasrCsvData, segment.EndWptId);

			if (startCoordinates.HasValue && endCoordinates.HasValue)
			{
				return true;
			}
		}

		return false;
	}
}
