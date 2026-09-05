using FEBuddyLibrary.Models.NASR.CSV;

namespace FEBuddyLibrary.Generators.NASR;

public static partial class AwyGeojsonGenerator
{
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
}