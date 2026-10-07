using System.Globalization;
using System.Windows.Threading;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Wpf.Map;
using FeBuddy.Wpf.Map.Models;
using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The background reference every map draws under everything else: which base layers
/// (<see cref="BaseMap"/>) are on and how strongly they are drawn, and whether the lat/lon
/// gridlines are. Set from the map toolbar's base-map menu and saved under
/// <c>Services.MapService</c> the moment they change.
/// </summary>
public sealed class BaseMapSettings : ObservableObject
{
	private const string Node = "Services.MapService";

	// Internal so the unit tests can check every settings layout's keys are still the ones read here.
	internal const string GridlinesKey = Node + ".Gridlines";
	internal const string LayersKey = Node + ".BaseMapLayers";
	internal const string OpacityKey = Node + ".BaseMapOpacity";

	/// <summary>The opacity, in percent, before the user has chosen one: there to steer by, not to look at.</summary>
	internal const int DefaultOpacityPercent = 50;

	/// <summary>The faintest the slider goes: any fainter and the layers may as well be off.</summary>
	public const int MinOpacityPercent = 10;

	private readonly DispatcherTimer _saveOpacity;
	private bool _showGridlines;
	private int _opacityPercent;
	private bool _isMenuOpen;
	private IReadOnlyList<MapLayer> _layers = [];

	/// <summary>Reads the saved choices.</summary>
	/// <param name="dispatcher">The UI thread's dispatcher, for the opacity's delayed save.</param>
	internal BaseMapSettings(Dispatcher dispatcher)
	{
		// A slider drag changes the opacity many times a second: draw each, save once it settles.
		_saveOpacity = new DispatcherTimer(TimeSpan.FromMilliseconds(400), DispatcherPriority.Background, (_, _) => SaveOpacity(), dispatcher);
		_saveOpacity.Stop();

		HashSet<BaseMapLayer> on = ReadLayers();
		Toggles = [.. Enum.GetValues<BaseMapLayer>().Select(layer => new BaseMapToggle(layer, on.Contains(layer), OnLayerToggled))];
		_showGridlines = ReadGridlines();
		_opacityPercent = ReadOpacity();
		Rebuild();
	}

	/// <summary>The layers to pick from, in drawing order. With none picked there is no base map.</summary>
	public IReadOnlyList<BaseMapToggle> Toggles { get; }

	/// <summary>How strongly the layers are drawn, in percent (<see cref="MinOpacityPercent"/> to 100).</summary>
	public int OpacityPercent
	{
		get => _opacityPercent;
		set
		{
			if (SetProperty(ref _opacityPercent, Math.Clamp(value, MinOpacityPercent, 100)))
			{
				OnPropertyChanged(nameof(Opacity));
				OnPropertyChanged(nameof(OpacityText));
				_saveOpacity.Stop();
				_saveOpacity.Start();
			}
		}
	}

	/// <summary><see cref="OpacityPercent"/> as the map takes it, 0.1 to 1.</summary>
	public double Opacity => OpacityPercent / 100.0;

	/// <summary><see cref="OpacityPercent"/> for the menu, e.g. <c>50%</c>.</summary>
	public string OpacityText => OpacityPercent.ToString(CultureInfo.InvariantCulture) + "%";

	/// <summary>The layers to draw: the picked ones.</summary>
	public IReadOnlyList<MapLayer> Layers
	{
		get => _layers;
		private set => SetProperty(ref _layers, value);
	}

	/// <summary>Whether the latitude / longitude gridlines are drawn.</summary>
	public bool ShowGridlines
	{
		get => _showGridlines;
		set
		{
			if (SetProperty(ref _showGridlines, value))
			{
				UserConfigFile.TrySetValue(GridlinesKey, value ? "Y" : "N");
				UserConfigFile.Save(Node);
			}
		}
	}

	/// <summary>Whether the toolbar's base-map menu is open.</summary>
	public bool IsMenuOpen
	{
		get => _isMenuOpen;
		set => SetProperty(ref _isMenuOpen, value);
	}

	/// <summary>Re-reads every choice after a settings import replaced <c>UserConfig</c>, without saving it back.</summary>
	internal void ReloadFromConfig()
	{
		HashSet<BaseMapLayer> on = ReadLayers();
		foreach (BaseMapToggle toggle in Toggles)
		{
			toggle.SetVisibleSilently(on.Contains(toggle.Layer));
		}

		_saveOpacity.Stop();
		SetProperty(ref _showGridlines, ReadGridlines(), nameof(ShowGridlines));
		if (SetProperty(ref _opacityPercent, ReadOpacity(), nameof(OpacityPercent)))
		{
			OnPropertyChanged(nameof(Opacity));
			OnPropertyChanged(nameof(OpacityText));
		}

		Rebuild();
	}

	private void OnLayerToggled()
	{
		UserConfigFile.TrySetValue(LayersKey, string.Join(',', Toggles.Where(t => t.IsVisible).Select(t => t.Layer)));
		UserConfigFile.Save(Node);
		Rebuild();
	}

	private void SaveOpacity()
	{
		_saveOpacity.Stop();
		UserConfigFile.TrySetValue(OpacityKey, OpacityPercent.ToString(CultureInfo.InvariantCulture));
		UserConfigFile.Save(Node);
	}

	private void Rebuild() =>
		Layers = [.. Toggles.Where(t => t.IsVisible).Select(t => BaseMap.Get(t.Layer)).OfType<MapLayer>()];

	private static bool ReadGridlines() =>
		!string.Equals(UserConfigFile.GetValue(GridlinesKey), "N", StringComparison.OrdinalIgnoreCase);

	/// <summary>The picked layers; every one of them until the user has picked.</summary>
	private static HashSet<BaseMapLayer> ReadLayers()
	{
		string? saved = UserConfigFile.GetValue(LayersKey);
		if (saved is null)
		{
			return [.. Enum.GetValues<BaseMapLayer>()];
		}

		HashSet<BaseMapLayer> on = [];
		foreach (string name in saved.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			if (Enum.TryParse(name, ignoreCase: true, out BaseMapLayer layer))
			{
				on.Add(layer);
			}
		}

		return on;
	}

	private static int ReadOpacity() =>
		int.TryParse(UserConfigFile.GetValue(OpacityKey), NumberStyles.Integer, CultureInfo.InvariantCulture, out int percent)
			? Math.Clamp(percent, MinOpacityPercent, 100)
			: DefaultOpacityPercent;
}
