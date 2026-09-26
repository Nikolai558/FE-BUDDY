namespace FeBuddy.Core.Domain.ArtccBoundaries.Models;

/// <summary>One point on an ARTCC boundary ring, from a single <c>ARB_SEG</c> row.</summary>
/// <param name="Latitude">Decimal latitude, in degrees (<c>ARB_SEG.LAT_DECIMAL</c>).</param>
/// <param name="Longitude">Decimal longitude, in degrees (<c>ARB_SEG.LONG_DECIMAL</c>).</param>
public sealed record ArtccBoundaryPoint(double Latitude, double Longitude);
