namespace FeBuddy.Core.Models.Services.Airac.Airways;

/// <summary>
/// One normalized point-to-point segment of an airway, after reference-only points
/// (records whose <c>FROM_PT_TYPE</c> is null/empty) have been collapsed out by
/// <c>AirwayNormalizer</c>.
/// </summary>
/// <param name="StartWptId">The starting waypoint identifier (trimmed <c>FROM_POINT</c>).</param>
/// <param name="EndWptId">The ending waypoint identifier (trimmed <c>TO_POINT</c>).</param>
/// <param name="IsGap">
/// <see langword="true"/> when this segment (or any reference-only record collapsed into it)
/// was marked with <c>AWY_SEG_GAP_FLAG = Y</c>, meaning the airway is discontinued here and
/// geometry building must start a new LineString rather than continuing the current one.
/// </param>
/// <param name="MaxAuthAlt">
/// This segment's <c>MAX_AUTH_ALT</c>, carried through from the raw AWY_SEG_ALT record (or
/// the highest of any records collapsed into this segment). Used by <c>AirwayClassifier</c>.
/// </param>
public sealed record AirwaySegment(
	string StartWptId,
	string EndWptId,
	bool IsGap,
	int? MaxAuthAlt);
