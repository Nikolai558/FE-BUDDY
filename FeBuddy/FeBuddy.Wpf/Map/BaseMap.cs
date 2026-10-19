using System.IO;
using System.Windows;
using System.Windows.Media;

using FeBuddy.Wpf.Map.Models;

using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Wpf.Map;

/// <summary>
/// The background reference layers - US states, and the world's coastlines and lakes - from Natural Earth
/// 1:50m, each loaded once from its bundled <c>Assets/BaseMap/*.json</c> resource and shared by every
/// map: the Map screen and every map popup (<see cref="Views.RoiPickerWindow"/>, for the Settings
/// default ROI and each sub-service's ROI override). <c>FeBuddy/Tools/BuildBaseMap.cs</c> builds the
/// resources.
/// </summary>
/// <remarks>
/// The files are 1.5 MB of GeoJSON, read and parsed off the UI thread once FE-Buddy is idle after
/// opening (<see cref="Preload"/>), so the first map opens without parsing them. Only the layer itself,
/// with the theme's brush, is made on the UI thread.
/// </remarks>
public static class BaseMap
{
	// Each layer's shapes: parsed once, on whichever thread asks first - normally Preload's.
	private static readonly Dictionary<BaseMapLayer, Lazy<IReadOnlyList<MapGeometry>?>> Shapes = Enum.GetValues<BaseMapLayer>()
		.ToDictionary(layer => layer, layer => new Lazy<IReadOnlyList<MapGeometry>?>(() => Read(layer)));

	// Each layer, made on the UI thread from its shapes and the theme's brush.
	private static readonly Dictionary<BaseMapLayer, Lazy<MapLayer?>> Layers = Enum.GetValues<BaseMapLayer>()
		.ToDictionary(layer => layer, layer => new Lazy<MapLayer?>(() => Make(layer)));

	/// <summary>
	/// Reads and parses every layer's file, so the first map that opens doesn't. Call it off the UI
	/// thread; a layer a map asks for meanwhile waits for it rather than parsing it again.
	/// </summary>
	public static void Preload()
	{
		foreach (Lazy<IReadOnlyList<MapGeometry>?> shapes in Shapes.Values)
		{
			_ = shapes.Value;
		}
	}

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

	/// <summary>The layer, from its shapes and the theme's brush; on the UI thread, where the brush is.</summary>
	private static MapLayer? Make(BaseMapLayer layer)
	{
		if (Shapes[layer].Value is not { } geometries)
		{
			return null;
		}

		Brush stroke = ThemeBrush("Brush.Stroke.Strong", Color.FromRgb(0x2A, 0x3D, 0x52));
		return new MapLayer(Name(layer), geometries, stroke, thickness: 1.0, pointRadius: 3.5);
	}

	/// <summary>Reads and parses the layer's bundled file; on any thread.</summary>
	/// <returns>The shapes, or <see langword="null"/> when the file is missing or can't be read.</returns>
	private static IReadOnlyList<MapGeometry>? Read(BaseMapLayer layer)
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
				AppLog.Warning("Map", $"The base map's {Name(layer)} layer isn't in FE-Buddy ({file}.json), so it can't be drawn.");
				return null;
			}

			using StreamReader reader = new(info.Stream);
			return GeoJsonReader.Read(reader.ReadToEnd());
		}
		catch (Exception ex)
		{
			AppLog.Warning("Map", $"The base map's {Name(layer)} layer ({file}.json) couldn't be read, so it isn't drawn: {ex.Message}");
			return null;
		}
	}

	private static Brush ThemeBrush(string key, Color fallback) =>
		Application.Current?.TryFindResource(key) as Brush ?? FrozenBrush.Of(fallback);
}
