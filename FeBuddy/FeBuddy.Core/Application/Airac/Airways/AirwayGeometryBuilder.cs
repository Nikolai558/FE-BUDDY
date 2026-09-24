using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Airways.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Application.Airac.Airways;

/// <summary>
/// Builds the rendered LineString geometry for a single airway from its normalized segments.
/// </summary>
public static class AirwayGeometryBuilder
{
	/// <summary>
	/// Builds all continuous LineStrings belonging to a single airway.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data.</param>
	/// <param name="awyId">The airway identifier being processed (used only in warning text).</param>
	/// <param name="segments">Normalized segments belonging to the airway.</param>
	/// <returns>
	/// The airway's LineStrings, a warning per unresolved waypoint, and the unresolved IDs.
	/// </returns>
	/// <remarks>
	/// A segment whose waypoint cannot be found is reported, not thrown: it is skipped like an
	/// airway gap and building carries on, so every unresolved ID is collected. Any unresolved
	/// ID makes <see cref="AirwayBuilder"/> exclude the whole airway, so the geometry built
	/// around the fault is never drawn.
	/// </remarks>
	public static AirwayGeometryBuildResult Build(
		NasrCsvDataCollection allNasrCsvData,
		string awyId,
		IEnumerable<AirwaySegment> segments)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);
		ArgumentNullException.ThrowIfNull(segments);

		List<LineString> lineStrings = [];
		List<ServiceMessage> messages = [];
		List<string> unresolvedWaypointIds = [];
		List<Coordinate> currentCoordinates = [];

		List<AirwaySegment> segmentList = [.. segments];

		string? previousSegEndWptId = null;

		for (int i = 0; i < segmentList.Count; i++)
		{
			AirwaySegment segment = segmentList[i];

			string segStartWptId = segment.StartWptId;
			string segEndWptId = segment.EndWptId;

			var segStartCoordinates = WaypointLocator.Find(allNasrCsvData, segStartWptId);
			var segEndCoordinates = segStartCoordinates.HasValue
				? WaypointLocator.Find(allNasrCsvData, segEndWptId)
				: null;

			if (segStartCoordinates is null || segEndCoordinates is null)
			{
				(string unresolvedId, string whichEnd) = segStartCoordinates is null
					? (segStartWptId, "start")
					: (segEndWptId, "end");

				unresolvedWaypointIds.Add(unresolvedId);
				messages.Add(new ServiceMessage(LogLevel.Warning, "AirwayGeometryBuilder",
					$"Airway '{awyId}': unable to locate coordinates for segment {whichEnd} waypoint '{unresolvedId}'. This airway was excluded from all output."));

				// Border crossings were normalized away upstream, so this is a real data fault and
				// AirwayBuilder will exclude the airway. With nothing resolvable left there is
				// nothing more to learn, so stop.
				if (!HasResolvableSegmentAhead(allNasrCsvData, segmentList, i + 1))
				{
					break;
				}

				// Otherwise treat the segment as a gap and keep going, so every unresolved ID
				// lands in the exclusion message rather than only the first.
				FinishCurrentLineString(lineStrings, currentCoordinates);
				currentCoordinates = [];
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
				currentCoordinates = [segStartCoordinate, segEndCoordinate];
			}

			previousSegEndWptId = segEndWptId;
		}

		// If processing stopped because of an unresolved trailing portion, the valid
		// coordinates accumulated before it still need to be added to the output.
		FinishCurrentLineString(lineStrings, currentCoordinates);

		return new AirwayGeometryBuildResult(lineStrings, messages, unresolvedWaypointIds);
	}

	/// <summary>
	/// Appends the accumulated coordinates as a LineString, after collapsing consecutive
	/// duplicate coordinates and only when at least two <b>distinct</b> positions remain. This
	/// stops a degenerate zero-length LineString (e.g. from a repeated NASR waypoint, or a
	/// waypoint sitting exactly on the antimeridian) from ever being emitted.
	/// </summary>
	private static void FinishCurrentLineString(List<LineString> lineStrings, List<Coordinate> currentCoordinates)
	{
		if (currentCoordinates.Count < 2)
		{
			return;
		}

		List<Coordinate> deduped = new(currentCoordinates.Count) { currentCoordinates[0] };

		for (int i = 1; i < currentCoordinates.Count; i++)
		{
			Coordinate coordinate = currentCoordinates[i];
			if (coordinate.X != deduped[^1].X || coordinate.Y != deduped[^1].Y)
			{
				deduped.Add(coordinate);
			}
		}

		if (deduped.Count >= 2)
		{
			lineStrings.Add(Wgs84.Factory.CreateLineString([.. deduped]));
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
				WaypointLocator.Find(allNasrCsvData, segment.StartWptId);

			var endCoordinates =
				WaypointLocator.Find(allNasrCsvData, segment.EndWptId);

			if (startCoordinates.HasValue && endCoordinates.HasValue)
			{
				return true;
			}
		}

		return false;
	}
}
