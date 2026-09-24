using System.Globalization;
using System.Windows;
using System.Windows.Controls;

using FeBuddy.Wpf.Map;

using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// The one shared ROI editor (remediation plan Phase 11): a <see cref="MapCanvas"/> the user
/// can rubber-band, the four corner lat/lon boxes, and a <b>Set ROI</b> / <b>Cancel</b> pair.
/// Nothing reaches the caller until <b>Set ROI</b> is pressed and the box passes
/// <see cref="RoiFilter"/> validation; <b>Cancel</b> discards everything (including a box the
/// user drew). Hosted in place on the Map screen and inside <c>RoiPickerWindow</c> for the
/// Settings and AIRAC Service ROI dialogs.
/// </summary>
public partial class RoiEditor : UserControl
{
	private bool _syncingFromMap;
	private bool _syncingFromText;

	/// <summary>Initializes the control.</summary>
	public RoiEditor()
	{
		InitializeComponent();
		Loaded += OnLoaded;
	}

	/// <summary>Raised on <b>Set ROI</b> with the validated region.</summary>
	public event EventHandler<RegionOfInterest>? RoiSet;

	/// <summary>Raised on <b>Cancel</b>.</summary>
	public event EventHandler? Cancelled;

	/// <summary><see cref="MapCanvas.BaseLayer"/> - the reference outline (US states).</summary>
	public static readonly DependencyProperty BaseLayerProperty = DependencyProperty.Register(
		nameof(BaseLayer), typeof(MapLayer), typeof(RoiEditor), new PropertyMetadata(null));

	/// <summary>The reference base outline shown behind the ROI.</summary>
	public MapLayer? BaseLayer
	{
		get => (MapLayer?)GetValue(BaseLayerProperty);
		set => SetValue(BaseLayerProperty, value);
	}

	/// <summary>The ROI to seed the editor with, or <see langword="null"/> to start empty.</summary>
	public static readonly DependencyProperty InitialRoiProperty = DependencyProperty.Register(
		nameof(InitialRoi), typeof(RegionOfInterest), typeof(RoiEditor),
		new PropertyMetadata(null, (d, _) => ((RoiEditor)d).SeedFromInitial()));

	/// <summary>The ROI to seed the editor with.</summary>
	public RegionOfInterest? InitialRoi
	{
		get => (RegionOfInterest?)GetValue(InitialRoiProperty);
		set => SetValue(InitialRoiProperty, value);
	}

	private bool _wired;

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (!_wired)
		{
			_wired = true;
			DescriptorFor(MapCanvas.RoiSouthWestProperty)?.AddValueChanged(Map, (_, _) => OnMapRoiChanged());
			DescriptorFor(MapCanvas.RoiNorthEastProperty)?.AddValueChanged(Map, (_, _) => OnMapRoiChanged());
		}

		SeedFromInitial();
	}

	private static System.ComponentModel.DependencyPropertyDescriptor? DescriptorFor(DependencyProperty dp) =>
		System.ComponentModel.DependencyPropertyDescriptor.FromProperty(dp, typeof(MapCanvas));

	private void SeedFromInitial()
	{
		if (!IsLoaded)
		{
			return;
		}

		if (InitialRoi is { } roi)
		{
			_syncingFromText = true;
			NeLatBox.Text = roi.NeLat.ToString("0.######", CultureInfo.InvariantCulture);
			NeLonBox.Text = roi.NeLon.ToString("0.######", CultureInfo.InvariantCulture);
			SwLatBox.Text = roi.SwLat.ToString("0.######", CultureInfo.InvariantCulture);
			SwLonBox.Text = roi.SwLon.ToString("0.######", CultureInfo.InvariantCulture);
			_syncingFromText = false;

			Map.RoiSouthWest = new GeoPoint(roi.SwLat, roi.SwLon);
			Map.RoiNorthEast = new GeoPoint(roi.NeLat, roi.NeLon);
			Map.FrameBounds(new GeoBounds(new GeoPoint(roi.SwLat, roi.SwLon), new GeoPoint(roi.NeLat, roi.NeLon)));
		}

		RefreshCopyValue();
	}

	/// <summary>
	/// Puts the current corners on the copy button as one line, and disables it while any of
	/// the four boxes is empty or unparseable - copying half an ROI is never what the user
	/// wanted.
	/// </summary>
	private void RefreshCopyValue()
	{
		if (CopyCornersButton is null)
		{
			return;   // called from a TextChanged that fired during InitializeComponent
		}

		if (TryReadCoords(out double swLat, out double swLon, out double neLat, out double neLon))
		{
			CopyCornersButton.Value = string.Create(
				CultureInfo.InvariantCulture,
				$"SW {swLat:0.######}, {swLon:0.######} / NE {neLat:0.######}, {neLon:0.######}");
			CopyCornersButton.IsEnabled = true;
		}
		else
		{
			CopyCornersButton.Value = string.Empty;
			CopyCornersButton.IsEnabled = false;
		}
	}

	private void OnMapRoiChanged()
	{
		if (_syncingFromText)
		{
			return;
		}

		_syncingFromMap = true;
		if (Map.RoiNorthEast is { } ne)
		{
			NeLatBox.Text = ne.Lat.ToString("0.######", CultureInfo.InvariantCulture);
			NeLonBox.Text = ne.Lon.ToString("0.######", CultureInfo.InvariantCulture);
		}

		if (Map.RoiSouthWest is { } sw)
		{
			SwLatBox.Text = sw.Lat.ToString("0.######", CultureInfo.InvariantCulture);
			SwLonBox.Text = sw.Lon.ToString("0.######", CultureInfo.InvariantCulture);
		}

		_syncingFromMap = false;
	}

	private void OnCoordTextChanged(object sender, TextChangedEventArgs e)
	{
		// Before the early-outs below: every path that changes a corner - typing, the map
		// rubber-band, and seeding from InitialRoi - writes to these boxes, so refreshing here
		// keeps the copy button in step with all three without a hook in each.
		RefreshCopyValue();

		if (_syncingFromMap || !IsLoaded)
		{
			return;
		}

		if (TryReadCoords(out double swLat, out double swLon, out double neLat, out double neLon))
		{
			_syncingFromText = true;
			Map.RoiSouthWest = new GeoPoint(swLat, swLon);
			Map.RoiNorthEast = new GeoPoint(neLat, neLon);
			_syncingFromText = false;
		}
	}

	private void OnSetRoi(object sender, RoutedEventArgs e)
	{
		ErrorText.Visibility = Visibility.Collapsed;

		string swLat = SwLatBox.Text.Trim();
		string swLon = SwLonBox.Text.Trim();
		string neLat = NeLatBox.Text.Trim();
		string neLon = NeLonBox.Text.Trim();

		if (!RoiFilter.IsCoordinateValidFormat(swLat, swLon, neLat, neLon, out string? formatError))
		{
			ShowError(formatError);
			return;
		}

		double sLat = double.Parse(swLat, CultureInfo.InvariantCulture);
		double sLon = double.Parse(swLon, CultureInfo.InvariantCulture);
		double nLat = double.Parse(neLat, CultureInfo.InvariantCulture);
		double nLon = double.Parse(neLon, CultureInfo.InvariantCulture);

		if (!RoiFilter.IsCoordinatesRelativePositionValid(sLat, sLon, nLat, nLon, out string? positionError))
		{
			ShowError(positionError);
			return;
		}

		RoiSet?.Invoke(this, new RegionOfInterest(sLat, sLon, nLat, nLon));
	}

	private void OnCancel(object sender, RoutedEventArgs e) => Cancelled?.Invoke(this, EventArgs.Empty);

	private void ShowError(string? message)
	{
		ErrorText.Text = message ?? "Invalid Region of Interest.";
		ErrorText.Visibility = Visibility.Visible;
	}

	private bool TryReadCoords(out double swLat, out double swLon, out double neLat, out double neLon)
	{
		swLat = swLon = neLat = neLon = 0;
		return double.TryParse(SwLatBox.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out swLat)
			&& double.TryParse(SwLonBox.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out swLon)
			&& double.TryParse(NeLatBox.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out neLat)
			&& double.TryParse(NeLonBox.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out neLon);
	}
}
