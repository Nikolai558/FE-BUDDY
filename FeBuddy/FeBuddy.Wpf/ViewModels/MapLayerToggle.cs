using System.Windows.Media;

using FeBuddy.Wpf.Map.Models;
using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// One built-in layer the Map page can switch on - an ARTCC boundary stratum from the parsed
/// AIRAC cycle. Its <see cref="Layer"/> is built only once the user switches it on.
/// </summary>
/// <param name="key">The name its on/off state is saved under.</param>
/// <param name="name">The name shown in the list.</param>
/// <param name="swatch">The colour it is drawn in.</param>
/// <param name="isVisible">Whether it starts switched on.</param>
/// <param name="onVisibilityChanged">Called when the user switches it on or off.</param>
public sealed class MapLayerToggle(string key, string name, Brush swatch, bool isVisible, Action<MapLayerToggle> onVisibilityChanged) : ObservableObject
{
	private bool _isVisible = isVisible;
	private MapLayer? _layer;

	/// <summary>The name its on/off state is saved under.</summary>
	public string Key { get; } = key;

	/// <summary>The name shown in the list.</summary>
	public string Name { get; } = name;

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

	/// <summary>The layer for the chosen cycle, or <see langword="null"/> until it has been built.</summary>
	public MapLayer? Layer
	{
		get => _layer;
		set => SetProperty(ref _layer, value);
	}
}
