using System.Globalization;

namespace FeBuddy.Wpf.Map.Models;

/// <summary>
/// The user's home view: the point the map centres on and the zoom level it shows it at. Kept
/// as lat/lon and a zoom level (not a box) so it looks the same whatever size the window is.
/// </summary>
/// <param name="Lat">Latitude of the centre.</param>
/// <param name="Lon">Longitude of the centre.</param>
/// <param name="Zoom">The slippy-map zoom level, e.g. 6.5.</param>
public readonly record struct MapHome(double Lat, double Lon, double Zoom)
{
	/// <summary>The saved form, e.g. <c>34.05,-118.25,6.5</c>.</summary>
	/// <returns>The three numbers, comma-separated, in the invariant culture.</returns>
	public string ToConfig() => string.Create(CultureInfo.InvariantCulture, $"{Lat},{Lon},{Zoom}");

	/// <summary>Reads a value written by <see cref="ToConfig"/>.</summary>
	/// <param name="saved">The saved text.</param>
	/// <returns>The home view, or <see langword="null"/> when the text is missing or malformed.</returns>
	public static MapHome? Parse(string? saved)
	{
		string[] parts = saved?.Split(',') ?? [];
		return parts.Length == 3
			&& double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) && lat is >= -85.06 and <= 85.06
			&& double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double lon) && lon is >= -180 and <= 180
			&& double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double zoom) && zoom is >= 0 and <= 18
				? new MapHome(lat, lon, zoom)
				: null;
	}
}
