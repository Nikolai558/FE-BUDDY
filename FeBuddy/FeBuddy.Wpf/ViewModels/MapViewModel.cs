using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;

using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.Map;

using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Infrastructure.Logging;

using Microsoft.Win32;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The Map Service: view the user's own GeoJSON files and manage the single default ROI.
/// </summary>
public sealed class MapViewModel : ObservableObject
{
	private static readonly Color[] FileColors =
	[
		Color.FromRgb(0x7C, 0xC7, 0xF2), Color.FromRgb(0x5A, 0xD1, 0xA0),
		Color.FromRgb(0xB4, 0x8C, 0xF0), Color.FromRgb(0xF2, 0x87, 0x9B),
		Color.FromRgb(0xF0, 0xA3, 0x5A),
	];

	private int _colorCursor;
	private string? _statusMessage;
	private bool _showDefaultRoi = true;
	private RegionOfInterest? _defaultRoi;

	/// <summary>Creates the view-model with the US-states outline and the saved default ROI.</summary>
	public MapViewModel()
	{
		BaseLayer = BaseMap.UsStates;

		Layers = [];
		LoadedFiles = [];
		LoadedFiles.CollectionChanged += (_, _) =>
		{
			OnPropertyChanged(nameof(HasLoadedFiles));
			OnPropertyChanged(nameof(FilesButtonLabel));
		};

		LoadFilesCommand = new RelayCommand(LoadFiles);
		ClearFilesCommand = new RelayCommand(() => { LoadedFiles.Clear(); SyncLayers(); }, () => HasLoadedFiles);
		ResetViewCommand = new RelayCommand(() => ResetRequested?.Invoke(this, EventArgs.Empty));

		DefaultRoi = DefaultRoiStore.Load();
		DefaultRoiStore.Changed += OnDefaultRoiChanged;
	}

	// View-models live for the whole session (NavItem caches them), so the Map page has to
	// hear when Settings changes or clears the default ROI.
	private void OnDefaultRoiChanged(object? sender, EventArgs e) => DefaultRoi = DefaultRoiStore.Load();

	/// <summary>Raised when the view should zoom to a set of bounds (after a load).</summary>
	public event EventHandler<GeoBounds>? FrameRequested;

	/// <summary>Raised when the view should reset the map to the default extent.</summary>
	public event EventHandler? ResetRequested;

	/// <summary>The reference base outline (US states).</summary>
	public MapLayer? BaseLayer { get; }

	/// <summary>The draw list the map binds to (rebuilt by <see cref="SyncLayers"/>).</summary>
	public ObservableCollection<MapLayer> Layers { get; }

	/// <summary>Every opened file, visible or not.</summary>
	public ObservableCollection<LoadedFile> LoadedFiles { get; }

	/// <summary>Whether any file is open.</summary>
	public bool HasLoadedFiles => LoadedFiles.Count > 0;

	/// <summary>The files button's label, e.g. <c>2 files</c>.</summary>
	public string FilesButtonLabel => LoadedFiles.Count switch
	{
		0 => "No files loaded",
		1 => "1 file",
		var n => $"{n} files",
	};

	/// <summary>The result of the last load, e.g. which files could not be read.</summary>
	public string? StatusMessage
	{
		get => _statusMessage;
		private set => SetProperty(ref _statusMessage, value);
	}

	// ---- default ROI ----

	/// <summary>The saved default ROI, or <see langword="null"/> when none is set.</summary>
	public RegionOfInterest? DefaultRoi
	{
		get => _defaultRoi;
		private set
		{
			if (SetProperty(ref _defaultRoi, value))
			{
				OnPropertyChanged(nameof(HasDefaultRoi));
				OnPropertyChanged(nameof(DefaultRoiSummary));
				OnPropertyChanged(nameof(RoiOnMapSouthWest));
				OnPropertyChanged(nameof(RoiOnMapNorthEast));
			}
		}
	}

	/// <summary>Whether a default ROI is saved.</summary>
	public bool HasDefaultRoi => DefaultRoi is not null;

	/// <summary>The default ROI's corners on one line, or how to set one.</summary>
	public string DefaultRoiSummary => DefaultRoi is { } r
		? $"SW {r.SwLat:0.####}, {r.SwLon:0.####}   ·   NE {r.NeLat:0.####}, {r.NeLon:0.####}"
		: "No default ROI is set. Draw one below and press Set ROI.";

	/// <summary>When on, the saved default ROI box is drawn on the main map.</summary>
	public bool ShowDefaultRoi
	{
		get => _showDefaultRoi;
		set
		{
			if (SetProperty(ref _showDefaultRoi, value))
			{
				OnPropertyChanged(nameof(RoiOnMapSouthWest));
				OnPropertyChanged(nameof(RoiOnMapNorthEast));
			}
		}
	}

	/// <summary>SW corner shown on the main map (<see langword="null"/> hides the box).</summary>
	public GeoPoint? RoiOnMapSouthWest => ShowDefaultRoi && DefaultRoi is { } r ? new GeoPoint(r.SwLat, r.SwLon) : null;

	/// <summary>NE corner shown on the main map (<see langword="null"/> hides the box).</summary>
	public GeoPoint? RoiOnMapNorthEast => ShowDefaultRoi && DefaultRoi is { } r ? new GeoPoint(r.NeLat, r.NeLon) : null;

	/// <summary>Opens GeoJSON files onto the map.</summary>
	public ICommand LoadFilesCommand { get; }

	/// <summary>Removes every opened file.</summary>
	public ICommand ClearFilesCommand { get; }

	/// <summary>Resets the map to the contiguous US.</summary>
	public ICommand ResetViewCommand { get; }

	/// <summary>
	/// Called by the view when the embedded <c>RoiEditor</c> confirms a box. Saves it as the one
	/// default ROI, the same one Settings edits.
	/// </summary>
	/// <param name="roi">The confirmed region.</param>
	public void SetDefaultRoi(RegionOfInterest roi)
	{
		DefaultRoiStore.Set(roi);
		Toast.Success("Default ROI saved", "Settings ▸ Default Region of Interest now uses this box.");
	}

	private void LoadFiles()
	{
		OpenFileDialog dialog = new()
		{
			Title = "Open GeoJSON",
			Filter = "GeoJSON (*.geojson;*.json)|*.geojson;*.json|All files (*.*)|*.*",
			Multiselect = true,
		};

		if (dialog.ShowDialog() != true)
		{
			return;
		}

		int added = 0;
		List<string> failed = [];
		GeoBounds? combined = null;

		foreach (string path in dialog.FileNames)
		{
			try
			{
				IReadOnlyList<MapGeometry> geometries = GeoJsonReader.Read(File.ReadAllText(path));

				if (geometries.Count == 0)
				{
					failed.Add($"{Path.GetFileName(path)} (no supported features)");
					AppLog.Warning("Map", $"'{Path.GetFileName(path)}' had no supported GeoJSON features.");
					continue;
				}

				SolidColorBrush brush = new(FileColors[_colorCursor++ % FileColors.Length]);
				brush.Freeze();

				MapLayer layer = new(Path.GetFileName(path), geometries, brush, thickness: 1.9, pointRadius: 4.0);

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
				AppLog.Warning("Map", $"Could not read '{Path.GetFileName(path)}': {ex.Message}");
			}
		}

		SyncLayers();
		StatusMessage = BuildStatus(added, failed);

		if (failed.Count > 0)
		{
			Toast.Warn("Some files could not be loaded", string.Join("; ", failed));
		}

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

	/// <summary>Rebuilds <see cref="Layers"/> from the visible loaded files, in load order.</summary>
	private void SyncLayers()
	{
		Layers.Clear();
		foreach (LoadedFile file in LoadedFiles)
		{
			if (file.IsVisible)
			{
				Layers.Add(file.Layer);
			}
		}

		OnPropertyChanged(nameof(HasLoadedFiles));
		OnPropertyChanged(nameof(FilesButtonLabel));
		CommandManager.InvalidateRequerySuggested();
	}

	private static string BuildStatus(int added, IReadOnlyList<string> failed)
	{
		if (failed.Count == 0)
		{
			return added switch { 0 => string.Empty, 1 => "Loaded 1 file.", _ => $"Loaded {added} files." };
		}

		string head = added > 0 ? $"Loaded {added}; " : string.Empty;
		return head + "couldn't read " + string.Join(", ", failed);
	}

	private static GeoBounds Union(GeoBounds a, GeoBounds b) => new(
		new GeoPoint(Math.Min(a.South, b.South), Math.Min(a.West, b.West)),
		new GeoPoint(Math.Max(a.North, b.North), Math.Max(a.East, b.East)));
}
