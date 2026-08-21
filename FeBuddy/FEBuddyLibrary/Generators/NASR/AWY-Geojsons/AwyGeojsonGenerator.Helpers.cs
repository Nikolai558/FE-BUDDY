using FEBuddyLibrary.Handlers.CSV;
using FEBuddyLibrary.Models.NASR.CSV;

namespace FEBuddyLibrary.Generators.NASR;

public static partial class AwyGeojsonGenerator
{
	/// <summary>
	/// Determines whether any later airway segment has both a resolvable
	/// starting waypoint and ending waypoint.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data.</param>
	/// <param name="segments">The normalized airway segments.</param>
	/// <param name="startIndex">The first segment index to examine.</param>
	/// <returns>
	/// True if a later segment has resolvable coordinates for both endpoints;
	/// otherwise false.
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
				FindWaypointCoordinates.GetCoordinates(
					allNasrCsvData,
					segment.StartWptId);

			var endCoordinates =
				FindWaypointCoordinates.GetCoordinates(
					allNasrCsvData,
					segment.EndWptId);

			if (startCoordinates.HasValue &&
				endCoordinates.HasValue)
			{
				return true;
			}
		}

		return false;
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