using System.Windows.Media;

using FeBuddy.Wpf.Map;
using FeBuddy.Wpf.Map.Models;
using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// One live AIRAC layer the Map screen can switch on (ARTCC boundaries, towered airports,
/// VORs). Its <see cref="Layers"/> are built from the chosen cycle only once it is switched on.
/// </summary>
/// <param name="kind">Which layer it is; also the name its on/off state is saved under.</param>
/// <param name="swatch">The colour it is drawn in.</param>
/// <param name="isVisible">Whether it starts switched on.</param>
/// <param name="onVisibilityChanged">Called when the user switches it on or off.</param>
public sealed class MapLayerToggle(AiracLayerKind kind, Brush swatch, bool isVisible, Action<MapLayerToggle> onVisibilityChanged) : ObservableObject
{
	private bool _isVisible = isVisible;
	private IReadOnlyList<MapLayer>? _layers;

	/// <summary>Which layer it is.</summary>
	public AiracLayerKind Kind { get; } = kind;

	/// <summary>The name shown in the list.</summary>
	public string Name => AiracMapLayers.Name(Kind);

	/// <summary>What the layer shows, for its tooltip.</summary>
	public string Description => AiracMapLayers.Description(Kind);

	/// <summary>Legend swatch colour.</summary>
	public Brush Swatch { get; } = swatch;

	/// <summary>Whether the layer is drawn.</summary>
	public bool IsVisible
	{
		get => _isVisible;
		set
		{
			if (SetProperty(ref _isVisible, value))
			{
				onVisibilityChanged(this);
			}
		}
	}

	/// <summary>The map layers for the chosen cycle, or <see langword="null"/> until they have been built.</summary>
	public IReadOnlyList<MapLayer>? Layers
	{
		get => _layers;
		set => SetProperty(ref _layers, value);
	}
}
