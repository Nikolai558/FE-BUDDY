namespace FeBuddy.Core.Infrastructure.Veram.Models;

/// <summary>One <c>GeoMap</c> in a vERAM GeoMaps file: a named map made of objects.</summary>
/// <param name="Name">The map's name, e.g. <c>CENTER</c>.</param>
/// <param name="Objects">Its objects, in file order.</param>
public sealed record VeramGeoMap(string Name, IReadOnlyList<VeramGeoMapObject> Objects);
