using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using FeBuddy.Wpf.Map.Models;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// The one map screen, hosted by the Map page and by every map popup. See MapWorkspace.xaml.
/// Its code-behind only does what is purely about the map control: the toolbar's zoom buttons,
/// framing on request, handing a Shift + drag box to the view-model, and carrying the view
/// (centre and zoom) from one map to the next so a popup opens where the Map page was looking.
/// </summary>
public partial class MapWorkspace : UserControl
{
	private MapViewModel? _vm;

	/// <summary>Creates the view.</summary>
	public MapWorkspace()
	{
		InitializeComponent();
		Map.RoiQuickDrawn += (_, box) => _vm?.OnQuickDrawn(box);
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		Attach(DataContext as MapViewModel);
		DataContextChanged += OnDataContextChanged;
		_vm?.State.Refresh();

		// After layout, so the map knows its size and the framing lands where it should.
		Dispatcher.BeginInvoke(DispatcherPriority.Loaded, ApplyInitialView);
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (_vm is not null && Map.GetView() is { } view)
		{
			_vm.State.LastView = view;
		}

		DataContextChanged -= OnDataContextChanged;
		Attach(null);
	}

	private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) => Attach(e.NewValue as MapViewModel);

	/// <summary>
	/// Hooks this view to a view-model's events, unhooking the last one. The layer state is
	/// shared and outlives every popup, so a closed popup must let go of it.
	/// </summary>
	private void Attach(MapViewModel? vm)
	{
		if (_vm is not null)
		{
			_vm.FrameRequested -= OnFrameRequested;
			_vm.State.FrameLayersRequested -= OnFrameLayersRequested;
		}

		_vm = vm;

		if (_vm is not null)
		{
			_vm.FrameRequested += OnFrameRequested;
			_vm.State.FrameLayersRequested += OnFrameLayersRequested;
		}
	}

	/// <summary>
	/// A popup opened to edit an existing box frames that box; otherwise the map opens where the
	/// last one closed, or the first time, on the user's home view (the contiguous US by default).
	/// </summary>
	private void ApplyInitialView()
	{
		if (_vm is null)
		{
			return;
		}

		if (_vm.Target.StartsEditing && _vm.DraftRoi is { } box)
		{
			FrameAround(box);
		}
		else if (_vm.State.LastView is { } view)
		{
			Map.SetView(view);
		}
		else
		{
			GoHome();
		}
	}

	private void GoHome()
	{
		if (_vm?.State.Home is { } home)
		{
			Map.GoTo(home);
		}
		else
		{
			Map.ResetView();
		}
	}

	private void OnFrameRequested(object? sender, GeoBounds box) => FrameAround(box);

	private void OnFrameLayersRequested(object? sender, IReadOnlyList<MapLayer> layers) => Map.FrameLayers(layers);

	/// <summary>Frames a box with room around it, so its edges and handles are easy to grab.</summary>
	private void FrameAround(GeoBounds box)
	{
		double padLat = Math.Max((box.North - box.South) * 0.35, 0.05);
		double padLon = Math.Max((box.East - box.West) * 0.35, 0.05);
		Map.FrameBounds(new GeoBounds(
			new GeoPoint(box.South - padLat, box.West - padLon),
			new GeoPoint(box.North + padLat, box.East + padLon)));
	}

	private void OnZoomIn(object sender, RoutedEventArgs e) => Map.ZoomBy(2.0);

	private void OnZoomOut(object sender, RoutedEventArgs e) => Map.ZoomBy(0.5);

	private void OnHome(object sender, RoutedEventArgs e) => GoHome();

	private void OnSetHome(object sender, RoutedEventArgs e)
	{
		if (_vm is not null && Map.GetHome() is { } home)
		{
			_vm.State.SetHome(home);
		}
	}

	private void OnFitLayers(object sender, RoutedEventArgs e)
	{
		if (_vm is null || !Map.FrameLayers(_vm.State.Layers))
		{
			GoHome();
		}
	}
}
