using System.Windows.Media;

using FeBuddy.Wpf.Map.Models;

using FeBuddy.Core.Application.Map;
using FeBuddy.Core.Application.Map.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Wpf.Map;

/// <summary>
/// How the Map screen draws the live AIRAC layers: their colours, line weights, point shapes and the
/// zooms each part shows at. What each layer holds is Core's (<see cref="AiracMapData"/>).
/// Safe to call off the UI thread: every brush is frozen.
/// </summary>
internal static class AiracMapLayers
{
	/// <summary>The colour a layer is drawn in; the legend swatch uses it too.</summary>
	/// <param name="kind">The layer.</param>
	/// <returns>Its colour.</returns>
	public static Color Color(AiracLayerKind kind) => kind switch
	{
		AiracLayerKind.ArtccBoundaries => System.Windows.Media.Color.FromRgb(0x7C, 0xC7, 0xF2),
		AiracLayerKind.ToweredAirports => System.Windows.Media.Color.FromRgb(0x5A, 0xD1, 0xA0),
		_ => System.Windows.Media.Color.FromRgb(0xB4, 0x8C, 0xF0),
	};

	/// <summary>Builds one live layer (it can be several map layers, e.g. airports and their runways).</summary>
	/// <param name="kind">The layer to build.</param>
	/// <param name="data">The parsed cycle.</param>
	/// <returns>The map layers, bottom first.</returns>
	public static IReadOnlyList<MapLayer> Build(AiracLayerKind kind, NasrCsvDataCollection data) => kind switch
	{
		AiracLayerKind.ArtccBoundaries => BuildArtccBoundaries(data),
		AiracLayerKind.ToweredAirports => BuildToweredAirports(data),
		_ => BuildNavaids(data),
	};

	private static IReadOnlyList<MapLayer> BuildArtccBoundaries(NasrCsvDataCollection data) =>
	[
		new MapLayer(AiracMapData.Name(AiracLayerKind.ArtccBoundaries), AiracMapData.ArtccBoundaries(data), Frozen(AiracLayerKind.ArtccBoundaries), thickness: 1.3)
		{
			LabelMinZoom = 4.5,
		},
	];

	private static IReadOnlyList<MapLayer> BuildToweredAirports(NasrCsvDataCollection data)
	{
		ToweredAirportShapes shapes = AiracMapData.ToweredAirports(data);
		Brush brush = Frozen(AiracLayerKind.ToweredAirports);

		MapLayer runways = new("Runways", shapes.Runways, brush, thickness: 2.0)
		{
			MinZoom = 9.0,
			QuietBelowMinZoom = true,
		};

		MapLayer airports = new(AiracMapData.Name(AiracLayerKind.ToweredAirports), shapes.Airports, brush, pointRadius: 2.5)
		{
			LabelMinZoom = 6.0,
			LabelBesideSymbol = true,
		};

		return [runways, airports];
	}

	private static IReadOnlyList<MapLayer> BuildNavaids(NasrCsvDataCollection data) =>
	[
		new MapLayer(AiracMapData.Name(AiracLayerKind.Navaids), AiracMapData.Navaids(data), Frozen(AiracLayerKind.Navaids), pointRadius: 4.5)
		{
			MinZoom = 4.5,
			LabelMinZoom = 6.5,
			PointShape = MapPointShape.Hexagon,
			LabelBesideSymbol = true,
		},
	];

	private static SolidColorBrush Frozen(AiracLayerKind kind) => FrozenBrush.Of(Color(kind));
}
