using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Infrastructure.Sct.Models;

/// <summary>One line segment from a sector file, with its coordinates already resolved.</summary>
/// <param name="Name">
/// What the segment belongs to: the boundary or airway name in the boundary and airway
/// sections, the diagram name in <c>[SID]</c> / <c>[STAR]</c>, or empty in <c>[GEO]</c>.
/// </param>
/// <param name="Start">Where it starts (NTS order: X = longitude, Y = latitude).</param>
/// <param name="End">Where it ends.</param>
public sealed record SctSegment(string Name, Coordinate Start, Coordinate End);
