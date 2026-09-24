using System.Windows;
using System.Windows.Controls;
using FeBuddy.Wpf.Map;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// Code-behind only bridges two view-model events to imperative calls on the map
/// control (framing a loaded file, resetting the view) - both are view concerns.
/// </summary>
public partial class MapView : UserControl
{
	public MapView()
	{
		InitializeComponent();
		DataContextChanged += OnDataContextChanged;
		RoiEditorControl.RoiSet += (_, roi) => (DataContext as MapViewModel)?.SetDefaultRoi(roi);
	}

	private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.OldValue is MapViewModel oldVm)
		{
			oldVm.FrameRequested -= OnFrameRequested;
			oldVm.ResetRequested -= OnResetRequested;
		}

		if (e.NewValue is MapViewModel newVm)
		{
			newVm.FrameRequested += OnFrameRequested;
			newVm.ResetRequested += OnResetRequested;
		}
	}

	private void OnFrameRequested(object? sender, GeoBounds bounds) => Map.FrameBounds(bounds);

	private void OnResetRequested(object? sender, EventArgs e) => Map.ResetView();
}
