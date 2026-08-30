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
/// Drives the map screen: the base + overlay layers, the sample toggle, an
/// open-files command (multi-select), a per-file visibility list, and the ROI
/// the user drags on the map.
/// </summary>
public sealed class MapViewModel : ObservableObject
{
    // Distinct from the slate base outline and the amber sample layer.
    private static readonly Color[] FileColors =
    [
        Color.FromRgb(0x7C, 0xC7, 0xF2), // cyan
        Color.FromRgb(0x5A, 0xD1, 0xA0), // green
        Color.FromRgb(0xB4, 0x8C, 0xF0), // violet
        Color.FromRgb(0xF2, 0x87, 0x9B), // rose
        Color.FromRgb(0xF0, 0xA3, 0x5A), // orange
    ];

    private readonly MapLayer? _sampleLayer;
    private int _colorCursor;

    private bool _showSample = true;
    private bool _pickRoiOnMap;
    private bool _measureOnMap;
    private GeoPoint? _roiSouthWest;
    private GeoPoint? _roiNorthEast;
    private string? _statusMessage;

    public MapViewModel()
    {
        BaseLayer = TryLoadLayer("Assets/us-states.json", "US states",
            ThemeBrush("Brush.Stroke.Strong", Color.FromRgb(0x2A, 0x3D, 0x52)), thickness: 1.0);

        _sampleLayer = TryLoadLayer("Assets/sample-airways.json", "Sample airways",
            ThemeBrush("Brush.Accent", Color.FromRgb(0xF4, 0xB7, 0x40)), thickness: 1.7, pointRadius: 3.5);

        Layers = [];
        LoadedFiles = [];
        LoadedFiles.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasLoadedFiles));
            OnPropertyChanged(nameof(FilesButtonLabel));
        };
        SyncLayers();

        LoadFilesCommand = new RelayCommand(LoadFiles);
        ClearFilesCommand = new RelayCommand(() => { LoadedFiles.Clear(); SyncLayers(); });
        ClearRoiCommand = new RelayCommand(() => { RoiSouthWest = null; RoiNorthEast = null; });
        ResetViewCommand = new RelayCommand(() => ResetRequested?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>Raised when the view should zoom to a set of bounds (after a load).</summary>
    public event EventHandler<GeoBounds>? FrameRequested;

    /// <summary>Raised when the view should reset the map to the default extent.</summary>
    public event EventHandler? ResetRequested;

    public MapLayer? BaseLayer { get; }

    /// <summary>The draw list the map binds to (rebuilt by <see cref="SyncLayers"/>).</summary>
    public ObservableCollection<MapLayer> Layers { get; }

    /// <summary>Every opened file, visible or not.</summary>
    public ObservableCollection<LoadedFile> LoadedFiles { get; }

    public bool HasLoadedFiles => LoadedFiles.Count > 0;

    public string FilesButtonLabel => LoadedFiles.Count switch
    {
        0 => "No files",
        1 => "1 file",
        var n => $"{n} files",
    };

    public bool ShowSample
    {
        get => _showSample;
        set { if (SetProperty(ref _showSample, value)) SyncLayers(); }
    }

    /// <summary>Bound to <c>MapCanvas.RoiEnabled</c>. Mutually exclusive with <see cref="MeasureOnMap"/>.</summary>
    public bool PickRoiOnMap
    {
        get => _pickRoiOnMap;
        set
        {
            if (SetProperty(ref _pickRoiOnMap, value) && value)
            {
                MeasureOnMap = false;
            }
        }
    }

    /// <summary>Bound to <c>MapCanvas.MeasureEnabled</c>. Mutually exclusive with <see cref="PickRoiOnMap"/>.</summary>
    public bool MeasureOnMap
    {
        get => _measureOnMap;
        set
        {
            if (SetProperty(ref _measureOnMap, value) && value)
            {
                PickRoiOnMap = false;
            }
        }
    }

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

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ICommand LoadFilesCommand { get; }

    public ICommand ClearFilesCommand { get; }

    public ICommand ClearRoiCommand { get; }

    public ICommand ResetViewCommand { get; }

    // ----------------------------------------------------------------------

    private void LoadFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open GeoJSON",
            Filter = "GeoJSON (*.geojson;*.json)|*.geojson;*.json|All files (*.*)|*.*",
            Multiselect = true,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var added = 0;
        var failed = new List<string>();
        GeoBounds? combined = null;

        foreach (var path in dialog.FileNames)
        {
            try
            {
                var geometries = GeoJsonReader.Read(File.ReadAllText(path));
                var brush = new SolidColorBrush(FileColors[_colorCursor++ % FileColors.Length]);
                brush.Freeze();

                var layer = new MapLayer(Path.GetFileName(path), geometries, brush,
                    thickness: 1.9, pointRadius: 4.0);

                // Self-referential: the item's own remove command needs the item.
                LoadedFile file = null!;
                file = new LoadedFile(layer, SyncLayers, new RelayCommand(() => RemoveFile(file)));
                LoadedFiles.Add(file);
                added++;

                if (layer.Extent is { } extent)
                {
                    combined = combined is { } c ? Union(c, extent) : extent;
                }
            }
            catch (Exception ex) when (ex is FormatException or IOException or UnauthorizedAccessException)
            {
                failed.Add($"{Path.GetFileName(path)} ({ex.Message})");
            }
        }

        SyncLayers();
        StatusMessage = BuildStatus(added, failed);

        if (combined is { } bounds)
        {
            FrameRequested?.Invoke(this, bounds);
        }
    }

    private void RemoveFile(LoadedFile file)
    {
        LoadedFiles.Remove(file);
        SyncLayers();
    }

    /// <summary>Rebuilds <see cref="Layers"/>: visible files (in load order) then the sample.</summary>
    private void SyncLayers()
    {
        Layers.Clear();

        foreach (var file in LoadedFiles)
        {
            if (file.IsVisible)
            {
                Layers.Add(file.Layer);
            }
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

    private static string BuildStatus(int added, IReadOnlyList<string> failed)
    {
        if (failed.Count == 0)
        {
            return added switch { 0 => string.Empty, 1 => "Loaded 1 file.", _ => $"Loaded {added} files." };
        }

        var head = added > 0 ? $"Loaded {added}; " : string.Empty;
        return head + "couldn't read " + string.Join(", ", failed);
    }

    private static GeoBounds Union(GeoBounds a, GeoBounds b) => new(
        new GeoPoint(Math.Min(a.South, b.South), Math.Min(a.West, b.West)),
        new GeoPoint(Math.Max(a.North, b.North), Math.Max(a.East, b.East)));

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
