using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Infrastructure.Sct.Models;

/// <summary>One <c>[REGIONS]</c> entry: a filled area.</summary>
/// <param name="Name">What the file calls it - usually its colour name.</param>
/// <param name="Points">Its outline, in order, as written (not closed).</param>
public sealed record SctRegion(string Name, IReadOnlyList<Coordinate> Points);
