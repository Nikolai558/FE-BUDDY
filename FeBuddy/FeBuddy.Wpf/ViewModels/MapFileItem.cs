using System.Windows.Input;
using System.Windows.Media;

using FeBuddy.Wpf.Map.Models;
using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// One GeoJSON file on the map - one the user opened, or one from a run's output - with a
/// visibility toggle, zoom-to and remove. The file loads in the background; until it has,
/// <see cref="Layer"/> is <see langword="null"/> and <see cref="Detail"/> says why.
/// </summary>
public sealed class MapFileItem : ObservableObject
{
	private readonly Action _onChanged;
	private bool _isVisible = true;
	private MapLayer? _layer;
	private string _detail = "Loading…";
	private bool _hasProblem;
	private bool _isLoading = true;

	/// <summary>Creates the item, still loading.</summary>
	/// <param name="name">The name shown in the list.</param>
	/// <param name="path">The file's full path, shown as a tooltip.</param>
	/// <param name="swatch">The colour its shapes are drawn in.</param>
	/// <param name="onChanged">Called when the item's layer or visibility changes, to rebuild the draw list.</param>
	/// <param name="remove">Removes the item.</param>
	/// <param name="zoom">Frames the item's layer on the map.</param>
	public MapFileItem(string name, string path, Brush swatch, Action onChanged, Action<MapFileItem> remove, Action<MapFileItem> zoom)
	{
		Name = name;
		Path = path;
		Swatch = swatch;
		_onChanged = onChanged;
		RemoveCommand = new RelayCommand(() => remove(this));
		ZoomCommand = new RelayCommand(() => zoom(this), () => _layer is not null);
	}

	/// <summary>The name shown in the list.</summary>
	public string Name { get; }

	/// <summary>The file's full path.</summary>
	public string Path { get; }

	/// <summary>Legend swatch colour (matches the on-map stroke).</summary>
	public Brush Swatch { get; }

	/// <summary>When the loaded copy of the file was written, to spot a newer run's copy.</summary>
	public DateTime LoadedWriteUtc { get; set; }

	/// <summary>The file's shapes, or <see langword="null"/> while loading or when it could not be read.</summary>
	public MapLayer? Layer
	{
		get => _layer;
		private set
		{
			if (SetProperty(ref _layer, value))
			{
				CommandManager.InvalidateRequerySuggested();
				_onChanged();
			}
		}
	}

	/// <summary>One line under the name: the feature count, or loading / error text.</summary>
	public string Detail
	{
		get => _detail;
		private set => SetProperty(ref _detail, value);
	}

	/// <summary>Whether <see cref="Detail"/> reports a problem (drawn in the warning colour).</summary>
	public bool HasProblem
	{
		get => _hasProblem;
		private set => SetProperty(ref _hasProblem, value);
	}

	/// <summary>Whether the file is being read.</summary>
	public bool IsLoading
	{
		get => _isLoading;
		private set => SetProperty(ref _isLoading, value);
	}

	/// <summary>Whether the file is drawn on the map.</summary>
	public bool IsVisible
	{
		get => _isVisible;
		set
		{
			if (SetProperty(ref _isVisible, value))
			{
				_onChanged();
			}
		}
	}

	/// <summary>Removes the file from the map.</summary>
	public ICommand RemoveCommand { get; }

	/// <summary>Frames the file's shapes.</summary>
	public ICommand ZoomCommand { get; }

	/// <summary>Shows the loaded layer.</summary>
	/// <param name="layer">The file's shapes.</param>
	public void SetLoaded(MapLayer layer)
	{
		Detail = layer.FeatureCount == 1 ? "1 feature" : $"{layer.FeatureCount:N0} features";
		HasProblem = false;
		IsLoading = false;
		Layer = layer;
	}

	/// <summary>Shows that the file is loading again (a newer copy was written).</summary>
	public void SetLoading()
	{
		Detail = "Loading…";
		HasProblem = false;
		IsLoading = true;
	}

	/// <summary>Shows why the file has nothing to draw.</summary>
	/// <param name="message">What went wrong, in a few words.</param>
	public void SetProblem(string message)
	{
		Detail = message;
		HasProblem = true;
		IsLoading = false;
		Layer = null;
	}
}
