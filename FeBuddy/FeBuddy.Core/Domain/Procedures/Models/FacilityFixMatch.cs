namespace FeBuddy.Core.Domain.Procedures.Models;

/// <summary>
/// Why a departure or arrival at an airport outside the user's facility is included: its route has a
/// fix in that facility (Feature #367).
/// </summary>
/// <param name="Facility">The facility, e.g. <c>ZDC</c>.</param>
/// <param name="Fixes">The route's fixes and NAVAIDs in it, in route order, each once, e.g. <c>DUCKY</c>, <c>OTTTO</c>.</param>
public sealed record FacilityFixMatch(string Facility, IReadOnlyList<string> Fixes);
