namespace FeBuddy.Core.Infrastructure.Eram.Models;

/// <summary>One <c>GeoMapRecord</c> in an ERAM <c>Geomaps.xml</c>: a map made of objects.</summary>
/// <param name="Name">The map's <c>GeomapId</c>, e.g. <c>CENTER</c>.</param>
/// <param name="Objects">Its objects, in file order.</param>
public sealed record EramGeoMap(string Name, IReadOnlyList<EramGeoMapObject> Objects);
