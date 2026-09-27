using System.IO;
using System.Windows;
using System.Windows.Media;

using FeBuddy.Wpf.Map.Models;

namespace FeBuddy.Wpf.Map;

/// <summary>
/// The reference base outline (contiguous US states), loaded once from the bundled
/// <c>Assets/us-states.json</c> resource and shared by every map: the Map screen and every
/// map popup (<see cref="Views.RoiPickerWindow"/>, for the Settings default ROI and each sub-service's ROI override).
/// </summary>
public static class BaseMap
{
	private static readonly Lazy<MapLayer?> LazyUsStates = new(LoadUsStates);

	/// <summary>
	/// The shared US-states layer, or <see langword="null"/> when the resource
	/// is missing or unreadable.
	/// </summary>
	public static MapLayer? UsStates => LazyUsStates.Value;

	private static MapLayer? LoadUsStates()
	{
		try
		{
			System.Windows.Resources.StreamResourceInfo? info =
				Application.GetResourceStream(new Uri("Assets/us-states.json", UriKind.Relative));
			if (info is null)
			{
				return null;
			}

			using StreamReader reader = new(info.Stream);
			IReadOnlyList<MapGeometry> geometries = GeoJsonReader.Read(reader.ReadToEnd());

			Brush stroke = ThemeBrush("Brush.Stroke.Strong", Color.FromRgb(0x2A, 0x3D, 0x52));
			return new MapLayer("US states", geometries, stroke, thickness: 1.0, pointRadius: 3.5);
		}
		catch
		{
			return null;
		}
	}

	private static Brush ThemeBrush(string key, Color fallback)
	{
		if (Application.Current?.TryFindResource(key) is Brush brush)
		{
			return brush;
		}

		SolidColorBrush solid = new(fallback);
		solid.Freeze();
		return solid;
	}
}
