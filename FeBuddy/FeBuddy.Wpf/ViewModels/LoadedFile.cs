using System.Windows.Input;
using System.Windows.Media;
using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.Map;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// One GeoJSON file the user has opened on the map: its rendered layer, a
/// visibility toggle, and a remove command. Toggling <see cref="IsVisible"/>
/// calls back into the owning view-model so it can rebuild the draw list.
/// </summary>
/// <param name="layer">The file's geometry, ready to draw.</param>
/// <param name="onVisibilityChanged">Called when <see cref="IsVisible"/> changes.</param>
/// <param name="removeCommand">Removes this file from the map.</param>
public sealed class LoadedFile(MapLayer layer, Action onVisibilityChanged, ICommand removeCommand) : ObservableObject
{
	private readonly Action _onVisibilityChanged = onVisibilityChanged;
	private bool _isVisible = true;

	/// <summary>The file's geometry, ready to draw.</summary>
	public MapLayer Layer { get; } = layer;

	/// <summary>The file name shown in the list.</summary>
	public string Name => Layer.Name;

	/// <summary>Legend swatch colour (matches the on-map stroke).</summary>
	public Brush Swatch => Layer.Stroke;

	/// <summary>How many geometries the file holds.</summary>
	public int GeometryCount => Layer.Geometries.Count;

	/// <summary>Whether the file is drawn on the map.</summary>
	public bool IsVisible
	{
		get => _isVisible;
		set
		{
			if (SetProperty(ref _isVisible, value))
			{
				_onVisibilityChanged();
			}
		}
	}

	/// <summary>Removes this file from the map.</summary>
	public ICommand RemoveCommand { get; } = removeCommand;
}
