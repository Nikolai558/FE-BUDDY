using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.Map;
using Microsoft.Win32;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Drives the map screen: owns the base + overlay layers, the sample toggle, the
/// "open a file" command, and the ROI the user drags on the map.
/// </summary>
public sealed class MapViewModel : ObservableObject
{
    private readonly MapLayer? _sampleLayer;
    private MapLayer? _fileLayer;

    private bool _showSample = true;
    private bool _pickRoiOnMap;
    private GeoPoint? _roiSouthWest;
    private GeoPoint? _roiNorthEast;
    private string? _statusMessage;
    private string _fileLayerName = "No file loaded";

    public MapViewModel()
    {
        BaseLayer = TryLoadLayer("Assets/us-states.json", "US states",
            ThemeBrush("Brush.Stroke.Strong", Color.FromRgb(0x2A, 0x3D, 0x52)), thickness: 1.0);

        _sampleLayer = TryLoadLayer("Assets/sample-airways.json", "Sample airways",
            ThemeBrush("Brush.Accent", Color.FromRgb(0xF4, 0xB7, 0x40)), thickness: 1.7, pointRadius: 3.5);

        Layers = [];
        SyncLayers();

        LoadFileCommand = new RelayCommand(LoadFile);
        ClearRoiCommand = new RelayCommand(() => { RoiSouthWest = null; RoiNorthEast = null; });
        ResetViewCommand = new RelayCommand(() => ResetRequested?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>Raised when the view should zoom to a set of bounds (after a file load).</summary>
    public event EventHandler<GeoBounds>? FrameRequested;

    /// <summary>Raised when the view should reset the map to the default extent.</summary>
    public event EventHandler? ResetRequested;

    public MapLayer? BaseLayer { get; }

    public ObservableCollection<MapLayer> Layers { get; }

    public bool ShowSample
    {
        get => _showSample;
        set { if (SetProperty(ref _showSample, value)) SyncLayers(); }
    }

    /// <summary>Bound to <c>MapCanvas.RoiEnabled</c>.</summary>
    public bool PickRoiOnMap
    {
        get => _pickRoiOnMap;
        set => SetProperty(ref _pickRoiOnMap, value);
    }

    // Bound two-way to the map. Setters refresh the read-outs.
    public GeoPoint? RoiSouthWest
    {
        get => _roiSouthWest;
        set { if (SetProperty(ref _roiSouthWest, value)) RaiseRoiText(); }
    }

    public GeoPoint? RoiNorthEast
    {
        get => _roiNorthEast;
        set { if (SetProperty(ref _roiNorthEast, value)) RaiseRoiText(); }
    }

    public bool HasRoi => RoiSouthWest is not null && RoiNorthEast is not null;

    public string RoiSummary => HasRoi
        ? "Region of interest selected — corners below."
        : "Turn on “Pick ROI on map”, then drag a box. Right-click clears it.";

    public string RoiNeLat => Fmt(RoiNorthEast?.Lat);
    public string RoiNeLon => Fmt(RoiNorthEast?.Lon);
    public string RoiSwLat => Fmt(RoiSouthWest?.Lat);
    public string RoiSwLon => Fmt(RoiSouthWest?.Lon);

    public string FileLayerName
    {
        get => _fileLayerName;
        private set => SetProperty(ref _fileLayerName, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ICommand LoadFileCommand { get; }

    public ICommand ClearRoiCommand { get; }

    public ICommand ResetViewCommand { get; }

    // ----------------------------------------------------------------------

    private void LoadFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open GeoJSON",
            Filter = "GeoJSON (*.geojson;*.json)|*.geojson;*.json|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var geometries = GeoJsonReader.Read(File.ReadAllText(dialog.FileName));
            _fileLayer = new MapLayer(
                Path.GetFileName(dialog.FileName), geometries,
                ThemeBrush("Brush.Info", Color.FromRgb(0x7C, 0xC7, 0xF2)), thickness: 1.9, pointRadius: 4.0);

            FileLayerName = _fileLayer.Name;
            StatusMessage = $"Loaded {geometries.Count} geometr{(geometries.Count == 1 ? "y" : "ies")}.";
            SyncLayers();

            if (_fileLayer.Extent is { } extent)
            {
                FrameRequested?.Invoke(this, extent);
            }
        }
        catch (Exception ex) when (ex is FormatException or IOException or UnauthorizedAccessException)
        {
            StatusMessage = "Couldn't read that file: " + ex.Message;
        }
    }

    /// <summary>Rebuilds <see cref="Layers"/> from the current toggles.</summary>
    private void SyncLayers()
    {
        Layers.Clear();
        if (_fileLayer is not null)
        {
            Layers.Add(_fileLayer);
        }

        if (_showSample && _sampleLayer is not null)
        {
            Layers.Add(_sampleLayer);
        }
    }

    private void RaiseRoiText()
    {
        OnPropertyChanged(nameof(HasRoi));
        OnPropertyChanged(nameof(RoiSummary));
        OnPropertyChanged(nameof(RoiNeLat));
        OnPropertyChanged(nameof(RoiNeLon));
        OnPropertyChanged(nameof(RoiSwLat));
        OnPropertyChanged(nameof(RoiSwLon));
    }

    private static string Fmt(double? v) => v is { } d ? d.ToString("0.####") : "—";

    private static MapLayer? TryLoadLayer(string relativeUri, string name, Brush stroke,
        double thickness = 1.4, double pointRadius = 3.5)
    {
        try
        {
            var info = Application.GetResourceStream(new Uri(relativeUri, UriKind.Relative));
            if (info is null)
            {
                return null;
            }

            using var reader = new StreamReader(info.Stream);
            var geometries = GeoJsonReader.Read(reader.ReadToEnd());
            return new MapLayer(name, geometries, stroke, thickness, pointRadius);
        }
        catch
        {
            // Best-effort asset load (also keeps the XAML designer happy).
            return null;
        }
    }

    private static Brush ThemeBrush(string key, Color fallback)
    {
        if (Application.Current?.TryFindResource(key) is Brush brush)
        {
            return brush;
        }

        var solid = new SolidColorBrush(fallback);
        solid.Freeze();
        return solid;
    }
}
