namespace FeBuddy.Core.Infrastructure.Eram.Models;

/// <summary>One <c>GeoMapRecord</c> in an ERAM <c>Geomaps.xml</c>: a map made of objects.</summary>
/// <param name="Name">The map's <c>GeomapId</c>, e.g. <c>CENTER</c>.</param>
/// <param name="LabelLine1">The first line of the map's button label (<c>LabelLine1</c>), or <see langword="null"/> when it has none.</param>
/// <param name="LabelLine2">The second line (<c>LabelLine2</c>), or <see langword="null"/> when it has none.</param>
/// <param name="Objects">Its objects, in file order.</param>
public sealed record EramGeoMap(string Name, string? LabelLine1, string? LabelLine2, IReadOnlyList<EramGeoMapObject> Objects)
{
	/// <summary>
	/// The brightness menu the map uses (<c>BCGMenuName</c>), one of the <c>MapBrightnessMenu</c>s in
	/// <c>ConsoleCommandControl.xml</c>; <see langword="null"/> when it names none.
	/// </summary>
	public string? BcgMenuName { get; init; }

	/// <summary>
	/// The filter menu the map uses (<c>FilterMenuName</c>), one of the <c>MapFilterMenu</c>s in
	/// <c>ConsoleCommandControl.xml</c>; <see langword="null"/> when it names none.
	/// </summary>
	public string? FilterMenuName { get; init; }
}
