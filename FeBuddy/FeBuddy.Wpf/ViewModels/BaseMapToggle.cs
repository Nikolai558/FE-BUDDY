using FeBuddy.Wpf.Map;
using FeBuddy.Wpf.Map.Models;
using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>One background reference layer the base-map menu can switch on (US states, coastlines).</summary>
/// <param name="layer">Which layer it is; also the name its on/off state is saved under.</param>
/// <param name="isVisible">Whether it starts switched on.</param>
/// <param name="onVisibilityChanged">Called when the user switches it on or off.</param>
public sealed class BaseMapToggle(BaseMapLayer layer, bool isVisible, Action onVisibilityChanged) : ObservableObject
{
	private bool _isVisible = isVisible;

	/// <summary>Which layer it is.</summary>
	public BaseMapLayer Layer { get; } = layer;

	/// <summary>The name shown in the menu.</summary>
	public string Name => BaseMap.Name(Layer);

	/// <summary>Whether the layer is drawn.</summary>
	public bool IsVisible
	{
		get => _isVisible;
		set
		{
			if (SetProperty(ref _isVisible, value))
			{
				onVisibilityChanged();
			}
		}
	}

	/// <summary>Sets <see cref="IsVisible"/> without reporting it as the user's choice (a settings import).</summary>
	/// <param name="value">Whether the layer is drawn.</param>
	internal void SetVisibleSilently(bool value)
	{
		_isVisible = value;
		OnPropertyChanged(nameof(IsVisible));
	}
}
