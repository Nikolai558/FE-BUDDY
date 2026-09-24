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
public sealed class LoadedFile(MapLayer layer, Action onVisibilityChanged, ICommand removeCommand) : ObservableObject
{
	private readonly Action _onVisibilityChanged = onVisibilityChanged;
	private bool _isVisible = true;

	public MapLayer Layer { get; } = layer;

	public string Name => Layer.Name;

	/// <summary>Legend swatch colour (matches the on-map stroke).</summary>
	public Brush Swatch => Layer.Stroke;

	public int GeometryCount => Layer.Geometries.Count;

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

	public ICommand RemoveCommand { get; } = removeCommand;
}
