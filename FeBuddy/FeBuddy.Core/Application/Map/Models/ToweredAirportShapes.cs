using FeBuddy.Core.Domain.Map.Models;

namespace FeBuddy.Core.Application.Map.Models;

/// <summary>The towered airports layer's shapes (<see cref="AiracMapData.ToweredAirports"/>), drawn as two map layers.</summary>
/// <param name="Runways">Each runway with both ends' coordinates, as a line from end to end.</param>
/// <param name="Airports">Each airport as a point, labelled with its FAA ID.</param>
public sealed record ToweredAirportShapes(IReadOnlyList<MapGeometry> Runways, IReadOnlyList<MapGeometry> Airports);
