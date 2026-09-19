using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac.Airways;

namespace FeBuddy.Core.Services.Airac.Airways;

/// <summary>
/// Normalizes raw <c>AWY_SEG_ALT</c> records for one airway into the reduced
/// <see cref="AirwaySegment"/> sequence used by <see cref="AirwayGeometryBuilder"/> and
/// <see cref="AirwayClassifier"/>.
/// </summary>
public static class AirwayNormalizer
{
	/// <summary>
	/// Removes reference-only airway points whose <c>FROM_PT_TYPE</c> is null or empty and
	/// collapses the surrounding records into a direct segment.
	/// </summary>
	/// <param name="rawSegments">
	/// Raw <c>AWY_SEG_ALT</c> records for one airway, already ordered by <c>POINT_SEQ</c>.
	/// </param>
	/// <returns>A normalized list of airway segments.</returns>
	/// <remarks>
	/// A record whose <c>FROM_PT_TYPE</c> is empty represents a reference-only point (such as
	/// a border-crossing marker like "U.S. MEXICAN BORDER-2") rather than an actual NASR
	/// waypoint, and must never be sent to waypoint coordinate resolution. For example:
	/// <code>
	///     TIJ -&gt; U.S. MEXICAN BORDER-2
	///     U.S. MEXICAN BORDER-2 -&gt; TEYON
	/// </code>
	/// becomes a single normalized segment:
	/// <code>
	///     TIJ -&gt; TEYON
	/// </code>
	/// If any collapsed record is marked as an airway gap (<c>AWY_SEG_GAP_FLAG = Y</c>), that
	/// flag is preserved on the resulting normalized segment, and the highest
	/// <c>MAX_AUTH_ALT</c> among the collapsed records is carried through so no altitude
	/// information used by <see cref="AirwayClassifier"/> is lost.
	/// </remarks>
	public static List<AirwaySegment> Normalize(IReadOnlyList<AwyCsvDataModel.AwySegAlt> rawSegments)
	{
		ArgumentNullException.ThrowIfNull(rawSegments);

		List<AirwaySegment> normalizedSegments = new();

		// Border crossings (blank FROM_PT_TYPE) are reference-only, not waypoints. NASR closes
		// a border-terminating airway with a terminator row that has a blank TO_POINT, and the
		// look-ahead below can leave the marker as a segment's EndWptId; those segments are
		// dropped after the loop so they never reach coordinate resolution (remediation 3.2b).
		HashSet<string> referenceOnlyPoints = AirwayReferenceOnlyPoints.BuildSet(rawSegments);

		for (int i = 0; i < rawSegments.Count; i++)
		{
			AwyCsvDataModel.AwySegAlt currentSegment = rawSegments[i];

			/*
			 * If this record starts at a point with no FromPtType, the FromPoint is not
			 * treated as a real NASR waypoint. Do not create a separate segment beginning
			 * at that point. A preceding valid segment can consume this record while
			 * looking ahead.
			 */
			if (AirwayReferenceOnlyPoints.IsReferenceOnlyRow(currentSegment))
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
			int? maxAuthAlt = currentSegment.MaxAuthAlt;

			// Look ahead for one or more reference-only points and collapse them in.
			int nextIndex = i + 1;

			while (nextIndex < rawSegments.Count)
			{
				AwyCsvDataModel.AwySegAlt nextSegment = rawSegments[nextIndex];

				bool nextStartMatchesCurrentEnd =
					string.Equals(
						endWptId,
						nextSegment.FromPoint?.Trim(),
						StringComparison.OrdinalIgnoreCase);

				bool nextStartIsReferenceOnly =
					AirwayReferenceOnlyPoints.IsReferenceOnlyRow(nextSegment);

				if (!nextStartMatchesCurrentEnd || !nextStartIsReferenceOnly)
				{
					break;
				}

				if (IsGap(nextSegment.AwySegGapFlag))
				{
					isGap = true;
				}

				maxAuthAlt = HigherOf(maxAuthAlt, nextSegment.MaxAuthAlt);

				if (string.IsNullOrWhiteSpace(nextSegment.ToPoint))
				{
					break;
				}

				endWptId = nextSegment.ToPoint.Trim();
				nextIndex++;
			}

			normalizedSegments.Add(new AirwaySegment(startWptId, endWptId, isGap, maxAuthAlt));
		}

		// Drop any segment left ending at a border marker (the terminator-row case). J5 ending
		// at CFDCT is the correct answer, and there is deliberately no warning - this is normal,
		// expected NASR structure, not a resolution failure (remediation 3.2b).
		if (referenceOnlyPoints.Count > 0)
		{
			normalizedSegments.RemoveAll(segment => referenceOnlyPoints.Contains(segment.EndWptId));
		}

		return normalizedSegments;
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
	/// Returns the higher of two nullable altitudes, treating a null value as "no information"
	/// rather than as zero.
	/// </summary>
	private static int? HigherOf(int? a, int? b)
	{
		if (a is null) return b;
		if (b is null) return a;
		return Math.Max(a.Value, b.Value);
	}
}
