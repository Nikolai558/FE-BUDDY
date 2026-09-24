using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Application.Airac.Airways.Models;

/// <summary>
/// The result of buffering an airway's LineStrings away from its waypoints: the resulting
/// disjoint two-point leg LineStrings, plus any levelled messages.
/// </summary>
/// <param name="LineStrings">
/// One LineString per surviving leg. Always disjoint (no two legs share an endpoint), since
/// each leg has been pulled back from both of its original endpoints.
/// </param>
/// <param name="Messages">
/// Levelled messages raised while buffering. A leg shorter than the combined buffer radius of
/// its two endpoints is dropped and reported at <see cref="LogLevel.Info"/> - it is the
/// buffer doing exactly what it was asked to do, not a problem (remediation plan 3.8).
/// </param>
public sealed record AirwayBufferResult(
	IReadOnlyList<LineString> LineStrings,
	IReadOnlyList<ServiceMessage> Messages);
