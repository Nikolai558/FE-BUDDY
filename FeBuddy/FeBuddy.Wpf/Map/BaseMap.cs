using System.IO;
using System.Windows;
using System.Windows.Media;

using FeBuddy.Wpf.Map.Models;

namespace FeBuddy.Wpf.Map;

/// <summary>
/// The background reference layers - US states, and the world's coastlines and lakes - from Natural Earth
/// 1:50m, each loaded once from its bundled <c>Assets/BaseMap/*.json</c> resource and shared by every
/// map: the Map screen and every map popup (<see cref="Views.RoiPickerWindow"/>, for the Settings
/// default ROI and each sub-service's ROI override). <c>FeBuddy/Tools/BuildBaseMap.cs</c> builds the
/// resources.
/// </summary>
public static class BaseMap
{
	private static readonly Dictionary<BaseMapLayer, Lazy<MapLayer?>> Layers = Enum.GetValues<BaseMapLayer>()
		.ToDictionary(layer => layer, layer => new Lazy<MapLayer?>(() => Load(layer)));

	/// <summary>
	/// The shared layer for <paramref name="layer"/>, or <see langword="null"/> when its resource
	/// is missing or unreadable.
	/// </summary>
	/// <param name="layer">Which background layer.</param>
	/// <returns>The layer, loaded on first use.</returns>
	public static MapLayer? Get(BaseMapLayer layer) => Layers[layer].Value;

	/// <summary>The layer's name as the map shows it, e.g. <c>US states</c>.</summary>
	/// <param name="layer">Which background layer.</param>
	/// <returns>A short, sentence-case name.</returns>
	public static string Name(BaseMapLayer layer) => layer switch
	{
		BaseMapLayer.UsStates => "US states",
		_ => "Coastlines & lakes",
	};

	private static MapLayer? Load(BaseMapLayer layer)
	{
		string file = layer switch
		{
			BaseMapLayer.UsStates => "us-states",
			_ => "coastlines",
		};

		try
		{
			System.Windows.Resources.StreamResourceInfo? info =
				Application.GetResourceStream(new Uri($"Assets/BaseMap/{file}.json", UriKind.Relative));
			if (info is null)
			{
				return null;
			}

			using StreamReader reader = new(info.Stream);
			IReadOnlyList<MapGeometry> geometries = GeoJsonReader.Read(reader.ReadToEnd());

			Brush stroke = ThemeBrush("Brush.Stroke.Strong", Color.FromRgb(0x2A, 0x3D, 0x52));
			return new MapLayer(Name(layer), geometries, stroke, thickness: 1.0, pointRadius: 3.5);
		}
		catch
		{
			return null;
		}
	}

	private static Brush ThemeBrush(string key, Color fallback) =>
		Application.Current?.TryFindResource(key) as Brush ?? FrozenBrush.Of(fallback);
}
