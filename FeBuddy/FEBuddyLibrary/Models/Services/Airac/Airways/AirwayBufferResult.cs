using NetTopologySuite.Geometries;

namespace FEBuddyLibrary.Models.Services.Airac.Airways;

/// <summary>
/// The result of buffering an airway's LineStrings away from its waypoints: the resulting
/// disjoint two-point leg LineStrings, plus any legs dropped for being too short.
/// </summary>
/// <param name="LineStrings">
/// One LineString per surviving leg. Always disjoint (no two legs share an endpoint), since
/// each leg has been pulled back from both of its original endpoints.
/// </param>
/// <param name="Warnings">
/// Non-fatal problems encountered while buffering (e.g. a leg shorter than the combined
/// buffer radius of its two endpoints, which was dropped).
/// </param>
public sealed record AirwayBufferResult(
	IReadOnlyList<LineString> LineStrings,
	IReadOnlyList<string> Warnings);
