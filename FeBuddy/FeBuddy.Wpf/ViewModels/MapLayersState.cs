using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Wpf.Map;
using FeBuddy.Wpf.Map.Models;
using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;

using Microsoft.Win32;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// What is on the map, shared by every map in the app: the Map page and each map popup (the
/// Settings default ROI and every sub-service's "Pick on map…") show the same layers, so a
/// popup is a true replica of the Map page. Holds three kinds of layer, drawn in this order
/// over the US-states outline:
/// <list type="number">
/// <item>live layers built from the parsed AIRAC cycle - ARTCC boundaries, towered airports,
/// VORs (<see cref="AiracToggles"/>);</item>
/// <item>GeoJSON files an earlier run wrote, chosen in the output picker (<see cref="OutputFiles"/>);</item>
/// <item>GeoJSON files the user opened from anywhere (<see cref="UserFiles"/>).</item>
/// </list>
/// The chosen output files, the live-layer switches and the home view are saved under
/// <c>Services.MapService</c>.
/// </summary>
public sealed class MapLayersState : ObservableObject
{
	private const string Node = "Services.MapService";
	private const string OutputKey = Node + ".OutputGeojson";
	private const string AiracKey = Node + ".AiracLayers";
	private const string HomeKey = Node + ".Home";

	// Clear of the live layers' blue, green and purple (AiracMapLayers.Color), so a file never
	// looks like one of them.
	private static readonly Color[] FileColors =
	[
		Color.FromRgb(0xF0, 0xA3, 0x5A), Color.FromRgb(0xF2, 0x87, 0x9B),
		Color.FromRgb(0x6F, 0xE0, 0xD6), Color.FromRgb(0xE3, 0xD8, 0x6A),
		Color.FromRgb(0xD9, 0x9A, 0xE8), Color.FromRgb(0xFF, 0x8A, 0x65),
		Color.FromRgb(0xA5, 0xD6, 0x6F), Color.FromRgb(0xC9, 0xB7, 0x9C),
	];

	private static MapLayersState? _shared;

	private readonly Dispatcher _dispatcher;
	private readonly HashSet<string> _selectedOutputs;
	private readonly Dictionary<string, MapFileItem> _outputItems = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<(string CycleId, AiracLayerKind Kind), IReadOnlyList<MapLayer>> _airacBuilt = [];
	private int _userColorCursor;
	private int _airacLoadVersion;
	private CycleOption? _selectedCycle;
	private string _airacStatus = string.Empty;
	private bool _isAiracLoading;
	private bool _isOutputPickerOpen;
	private string _outputDirectory = string.Empty;
	private string _outputFilter = string.Empty;
	private bool _isPanelOpen = true;
	private MapHome? _home;
	private ICollectionView _outputChoicesView = CollectionViewSource.GetDefaultView(Array.Empty<OutputFileChoice>());
	private IReadOnlyList<AiracOutputGeojsonFile> _listedFiles = [];

	private MapLayersState()
	{
		_dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

		_selectedOutputs = new(Split(UserConfigFile.GetValue(OutputKey), '|'), StringComparer.OrdinalIgnoreCase);
		HashSet<string> airacOn = new(Split(UserConfigFile.GetValue(AiracKey), ','), StringComparer.OrdinalIgnoreCase);
		_home = MapHome.Parse(UserConfigFile.GetValue(HomeKey));

		AiracToggles =
		[
			.. Enum.GetValues<AiracLayerKind>().Select(kind => new MapLayerToggle(
				kind,
				Frozen(AiracMapLayers.Color(kind)),
				airacOn.Contains(kind.ToString()),
				OnAiracToggled)),
		];

		UserFiles.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasUserFiles));
		OutputFiles.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasOutputFiles));

		LoadFilesCommand = new RelayCommand(LoadFiles);
		ClearFilesCommand = new RelayCommand(ClearUserFiles, () => HasUserFiles);
		RefreshOutputsCommand = new RelayCommand(Refresh);
		OpenOutputFolderCommand = new RelayCommand(OpenOutputFolder, () => Directory.Exists(OutputDirectory));
		SelectAllOutputsCommand = new RelayCommand(() => SetAllOutputs(true), () => OutputChoices.Count > 0);
		SelectNoOutputsCommand = new RelayCommand(() => SetAllOutputs(false), () => _selectedOutputs.Count > 0);
		ResetHomeCommand = new RelayCommand(ResetHome, () => HasCustomHome);

		AiracCycleDataCache.Instance.StateChanged += (_, _) => _dispatcher.BeginInvoke(OnCacheStateChanged);

		// Choosing the first cycle loads its output and AIRAC layers; with no cycle yet, report that.
		RebuildCycles();
		if (SelectedCycle is null)
		{
			RefreshOutputs();
			_ = LoadAiracAsync();
		}
	}

	/// <summary>The one instance every map shares. Created on the UI thread on first use.</summary>
	public static MapLayersState Shared => _shared ??= new MapLayersState();

	/// <summary>Raised when the maps should frame some layers (a file just loaded, or its zoom button).</summary>
	public event EventHandler<IReadOnlyList<MapLayer>>? FrameLayersRequested;

	/// <summary>The reference outline (US states), always drawn so there is something to steer by.</summary>
	public MapLayer? BaseLayer => BaseMap.UsStates;

	/// <summary>The draw list every map binds to, rebuilt by <see cref="SyncLayers"/>.</summary>
	public ObservableCollection<MapLayer> Layers { get; } = [];

	/// <summary>Where the last map to close was looking, so the next one opens there.</summary>
	public MapViewState? LastView { get; set; }

	/// <summary>
	/// The user's saved home view (the toolbar's home button and where a map first opens), or
	/// <see langword="null"/> for the default, the contiguous US.
	/// </summary>
	public MapHome? Home
	{
		get => _home;
		private set
		{
			if (SetProperty(ref _home, value))
			{
				OnPropertyChanged(nameof(HomeSummary));
				OnPropertyChanged(nameof(HasCustomHome));
			}
		}
	}

	/// <summary>Whether the user has saved a home view of their own.</summary>
	public bool HasCustomHome => Home is not null;

	/// <summary>The home view on one line, e.g. <c>34.05, -118.25 at zoom 6.5</c>.</summary>
	public string HomeSummary => Home is { } h
		? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{h.Lat:0.###}, {h.Lon:0.###} at zoom {h.Zoom:0.#}")
		: "The contiguous US (default)";

	/// <summary>Puts the home view back to the contiguous US.</summary>
	public ICommand ResetHomeCommand { get; }

	/// <summary>Saves <paramref name="home"/> as the home view.</summary>
	/// <param name="home">The centre and zoom to come back to.</param>
	public void SetHome(MapHome home)
	{
		Home = home;
		UserConfigFile.TrySetValue(HomeKey, home.ToConfig());
		UserConfigFile.Save(Node);
		Toast.Success("Home view saved", "The home button (and every map that opens fresh) now starts here.");
	}

	/// <summary>Whether the side panel (ROI and layers) is shown; hiding it gives the map the room.</summary>
	public bool IsPanelOpen
	{
		get => _isPanelOpen;
		set => SetProperty(ref _isPanelOpen, value);
	}

	// ---- cycle ----

	/// <summary>The cycles to choose from: the ones FE-Buddy has data for, plus any with an output folder.</summary>
	public ObservableCollection<CycleOption> Cycles { get; } = [];

	/// <summary>The cycle whose AIRAC data and output files are shown.</summary>
	public CycleOption? SelectedCycle
	{
		get => _selectedCycle;
		set
		{
			if (SetProperty(ref _selectedCycle, value))
			{
				foreach (MapLayerToggle toggle in AiracToggles)
				{
					toggle.Layers = null;
				}

				RefreshOutputs();
				_ = LoadAiracAsync();
			}
		}
	}

	// ---- AIRAC layers ----

	/// <summary>The live AIRAC layers (ARTCC boundaries, towered airports, VORs), each switched on separately.</summary>
	public IReadOnlyList<MapLayerToggle> AiracToggles { get; }

	/// <summary>Where the AIRAC layers stand, e.g. <c>Loading AIRAC 2610…</c>.</summary>
	public string AiracStatus
	{
		get => _airacStatus;
		private set => SetProperty(ref _airacStatus, value);
	}

	/// <summary>Whether the AIRAC layers are being built.</summary>
	public bool IsAiracLoading
	{
		get => _isAiracLoading;
		private set => SetProperty(ref _isAiracLoading, value);
	}

	// ---- run output ----

	/// <summary>Every GeoJSON file in the chosen cycle's output folder, for the output picker.</summary>
	public IReadOnlyList<OutputFileChoice> OutputChoices { get; private set; } = [];

	/// <summary>The output files the user picked, as listed on the page.</summary>
	public ObservableCollection<MapFileItem> OutputFiles { get; } = [];

	/// <summary>Whether any output file is picked.</summary>
	public bool HasOutputFiles => OutputFiles.Count > 0;

	/// <summary>The run-output section's heading, naming the cycle whose folder it reads, e.g. <c>Output of the AIRAC 2609 run</c>.</summary>
	public string OutputHeader => SelectedCycle is { } cycle ? $"Output of the AIRAC {cycle.Id} run" : "Run output";

	/// <summary>The chosen cycle's output folder.</summary>
	public string OutputDirectory
	{
		get => _outputDirectory;
		private set => SetProperty(ref _outputDirectory, value);
	}

	/// <summary>
	/// <see cref="OutputChoices"/> as the picker shows it: grouped by folder and narrowed by
	/// <see cref="OutputFilter"/>. A Departures or Arrivals run writes thousands of files, so the
	/// picker leans on the filter rather than a wall of check boxes.
	/// </summary>
	public ICollectionView OutputChoicesView
	{
		get => _outputChoicesView;
		private set => SetProperty(ref _outputChoicesView, value);
	}

	/// <summary>Words every listed file's name or folder must contain, e.g. <c>ABQ Lines</c>.</summary>
	public string OutputFilter
	{
		get => _outputFilter;
		set
		{
			if (SetProperty(ref _outputFilter, value ?? string.Empty))
			{
				OutputChoicesView.Refresh();
				OnPropertyChanged(nameof(OutputSummary));
			}
		}
	}

	/// <summary>A line about the output picker's contents, e.g. <c>3 of 42 files shown</c>.</summary>
	public string OutputSummary
	{
		get
		{
			if (OutputChoices.Count == 0)
			{
				return SelectedCycle is { } cycle
					? $"No GeoJSON output for AIRAC {cycle.Id} yet. Run the AIRAC Service, then refresh."
					: "No AIRAC cycle to show output for.";
			}

			string shown = $"{OutputChoices.Count(c => c.IsSelected):N0} of {OutputChoices.Count:N0} files on the map";
			return string.IsNullOrWhiteSpace(OutputFilter)
				? shown
				: $"{shown}  ·  {OutputChoicesView.Cast<object>().Count():N0} match the filter";
		}
	}

	/// <summary>Whether the output picker popup is open. Opening it re-reads the folder.</summary>
	public bool IsOutputPickerOpen
	{
		get => _isOutputPickerOpen;
		set
		{
			if (SetProperty(ref _isOutputPickerOpen, value) && value)
			{
				Refresh();
			}
		}
	}

	// ---- user files ----

	/// <summary>Files the user opened with Load GeoJSON…, in the order opened.</summary>
	public ObservableCollection<MapFileItem> UserFiles { get; } = [];

	/// <summary>Whether the user has opened any file.</summary>
	public bool HasUserFiles => UserFiles.Count > 0;

	// ---- commands ----

	/// <summary>Opens GeoJSON files from anywhere onto the map.</summary>
	public ICommand LoadFilesCommand { get; }

	/// <summary>Removes every file the user opened.</summary>
	public ICommand ClearFilesCommand { get; }

	/// <summary>Re-reads the cycle list and the output folder, reloading any file a newer run replaced.</summary>
	public ICommand RefreshOutputsCommand { get; }

	/// <summary>Opens the chosen cycle's output folder in Explorer.</summary>
	public ICommand OpenOutputFolderCommand { get; }

	/// <summary>Ticks every file in the output picker.</summary>
	public ICommand SelectAllOutputsCommand { get; }

	/// <summary>Unticks every file in the output picker.</summary>
	public ICommand SelectNoOutputsCommand { get; }

	/// <summary>
	/// Re-reads the cycle list and the chosen cycle's output folder. Cheap (a directory listing),
	/// so each map calls it when it opens: a run that finished since shows up without a restart.
	/// </summary>
	public void Refresh()
	{
		RebuildCycles();
		RefreshOutputs();
	}

	private void ResetHome()
	{
		Home = null;
		UserConfigFile.TrySetValue(HomeKey, string.Empty);
		UserConfigFile.Save(Node);
	}

	// ============================ cycles ================================

	private void RebuildCycles()
	{
		Dictionary<string, string> labels = new(StringComparer.Ordinal);
		foreach (AiracCycleDataCacheEntry entry in AiracCycleDataCache.Instance.Entries)
		{
			if (entry.State != CycleDataState.NotYetPublished)
			{
				labels[entry.Cycle.AiracCycleId] = entry.Position.ToString().ToLowerInvariant();
			}
		}

		foreach (string id in AiracOutputCatalog.FindCycleIds(OutputPreferences.Directory, OutputPreferences.AddFeBuddyOutputFolder))
		{
			labels.TryAdd(id, "output only");
		}

		List<CycleOption> options = [.. labels
			.OrderByDescending(pair => pair.Key, StringComparer.Ordinal)
			.Select(pair => new CycleOption(pair.Key, $"AIRAC {pair.Key}  ·  {pair.Value}"))];

		if (options.SequenceEqual(Cycles))
		{
			return;
		}

		string? keep = SelectedCycle?.Id ?? CurrentCycleId();
		Cycles.Clear();
		foreach (CycleOption option in options)
		{
			Cycles.Add(option);
		}

		// Assign the field and notify directly: this is the same cycle, so there is nothing to reload.
		CycleOption? match = options.FirstOrDefault(o => o.Id == keep) ?? options.FirstOrDefault();
		if (match?.Id == _selectedCycle?.Id)
		{
			_selectedCycle = match;
			OnPropertyChanged(nameof(SelectedCycle));
		}
		else
		{
			SelectedCycle = match;
		}
	}

	private static string? CurrentCycleId()
	{
		try
		{
			return AppEnvironment.GetAiracCycle(AiracCyclePosition.Current).AiracCycleId;
		}
		catch (InvalidOperationException)
		{
			return null;
		}
	}

	private void OnCacheStateChanged()
	{
		RebuildCycles();
		if (AiracToggles.Any(t => t.IsVisible && t.Layers is null) && !IsAiracLoading)
		{
			_ = LoadAiracAsync();
		}
	}

	// ============================ AIRAC =================================

	private void OnAiracToggled(MapLayerToggle toggle)
	{
		UserConfigFile.TrySetValue(AiracKey, string.Join(',', AiracToggles.Where(t => t.IsVisible).Select(t => t.Kind)));
		UserConfigFile.Save(Node);

		if (toggle.IsVisible && toggle.Layers is null)
		{
			_ = LoadAiracAsync();
		}
		else
		{
			SyncLayers();
		}
	}

	/// <summary>
	/// Builds whichever switched-on live layers the chosen cycle does not have yet (each is built
	/// once per cycle, then kept), and reports how that is going in <see cref="AiracStatus"/>.
	/// </summary>
	private async Task LoadAiracAsync()
	{
		int version = ++_airacLoadVersion;

		if (SelectedCycle?.Id is not { } cycleId)
		{
			AiracStatus = "No AIRAC cycle is available yet.";
			return;
		}

		foreach (MapLayerToggle toggle in AiracToggles)
		{
			toggle.Layers ??= _airacBuilt.GetValueOrDefault((cycleId, toggle.Kind));
		}

		List<MapLayerToggle> missing = [.. AiracToggles.Where(t => t.IsVisible && t.Layers is null)];
		if (missing.Count == 0)
		{
			AiracStatus = AiracToggles.Any(t => t.IsVisible)
				? $"From the parsed AIRAC {cycleId} data."
				: $"Built from the parsed AIRAC {cycleId} data when switched on.";
			SyncLayers();
			return;
		}

		AiracCycleDataCacheEntry? entry = AiracCycleDataCache.Instance.GetEntry(cycleId);
		if (entry is null)
		{
			AiracStatus = $"AIRAC {cycleId} has output files only; FE-Buddy keeps data for the previous, current and next cycles.";
			return;
		}

		if (entry.State == CycleDataState.NotYetPublished)
		{
			AiracStatus = $"AIRAC {cycleId} is not published yet.";
			return;
		}

		AiracStatus = $"Loading AIRAC {cycleId}…";
		IsAiracLoading = true;
		try
		{
			NasrCsvDataCollection data = await AiracCycleDataCache.Instance.GetAsync(cycleId);
			foreach (MapLayerToggle toggle in missing)
			{
				IReadOnlyList<MapLayer> layers = await Task.Run(() =>
				{
					IReadOnlyList<MapLayer> result = AiracMapLayers.Build(toggle.Kind, data);
					foreach (MapLayer layer in result)
					{
						ProjectedLayer.For(layer);   // project off the UI thread, not on first draw
					}

					return result;
				});

				_airacBuilt[(cycleId, toggle.Kind)] = layers;
				if (version == _airacLoadVersion)
				{
					toggle.Layers = layers;
					SyncLayers();   // each layer appears as soon as it is ready
				}
			}

			if (version == _airacLoadVersion)
			{
				AiracStatus = $"From the parsed AIRAC {cycleId} data.";
			}
		}
		catch (Exception ex)
		{
			AppLog.Warning("Map", $"Could not build the AIRAC {cycleId} map layers: {ex.Message}");
			if (version == _airacLoadVersion)
			{
				AiracStatus = $"Could not load AIRAC {cycleId}: {ex.Message}";
			}
		}
		finally
		{
			if (version == _airacLoadVersion)
			{
				IsAiracLoading = false;
			}
		}
	}

	// ========================== run output ==============================

	private void RefreshOutputs()
	{
		IReadOnlyList<AiracOutputGeojsonFile> files = [];
		if (SelectedCycle?.Id is { } cycleId)
		{
			OutputDirectory = OutputPreferences.CycleDirectory(cycleId);
			files = AiracOutputCatalog.FindGeojsonFiles(OutputDirectory);
		}
		else
		{
			OutputDirectory = string.Empty;
		}

		// The same files as last time (the usual case - every map re-reads the folder when it
		// opens): keep the picker's list and view as they are, just check for rewritten files.
		if (!files.SequenceEqual(_listedFiles))
		{
			_listedFiles = files;
			List<OutputFileChoice> choices = [.. files.Select(f => new OutputFileChoice(f, _selectedOutputs.Contains(f.RelativePath), OnOutputChoiceChanged))];
			OutputChoices = choices;

			// A new view over a new list, rather than thousands of Add calls each regrouping it.
			ListCollectionView view = new(choices) { Filter = MatchesOutputFilter };
			view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(OutputFileChoice.Group)));
			OutputChoicesView = view;
			OnPropertyChanged(nameof(OutputChoices));
		}

		SyncOutputItems(files);
		OnPropertyChanged(nameof(OutputSummary));
		OnPropertyChanged(nameof(OutputHeader));
		CommandManager.InvalidateRequerySuggested();
	}

	private bool MatchesOutputFilter(object item)
	{
		if (string.IsNullOrWhiteSpace(OutputFilter) || item is not OutputFileChoice choice)
		{
			return true;
		}

		return OutputFilter.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(word =>
			choice.Name.Contains(word, StringComparison.OrdinalIgnoreCase)
			|| choice.Group.Contains(word, StringComparison.OrdinalIgnoreCase));
	}

	private void OnOutputChoiceChanged(OutputFileChoice choice)
	{
		if (choice.IsSelected)
		{
			_selectedOutputs.Add(choice.File.RelativePath);
		}
		else
		{
			_selectedOutputs.Remove(choice.File.RelativePath);
		}

		SaveOutputChoices();
		SyncOutputItems(_listedFiles);
		OnPropertyChanged(nameof(OutputSummary));
	}

	/// <summary>
	/// Ticks or unticks every file the filter shows. Ticking is capped: a whole cycle's
	/// procedures at once would bury the map and take a while to read in.
	/// </summary>
	private void SetAllOutputs(bool selected)
	{
		const int MaxAtOnce = 150;
		List<OutputFileChoice> shown = [.. OutputChoicesView.Cast<OutputFileChoice>()];

		if (selected && shown.Count > MaxAtOnce)
		{
			Toast.Warn(
				$"That would put {shown.Count:N0} files on the map",
				$"Narrow the list with the filter first (an airport, \"Airways\", \"Lines\"…) - up to {MaxAtOnce} at a time.");
			return;
		}

		foreach (OutputFileChoice choice in shown)
		{
			choice.SetSelectedSilently(selected);
			if (selected)
			{
				_selectedOutputs.Add(choice.File.RelativePath);
			}
			else
			{
				_selectedOutputs.Remove(choice.File.RelativePath);
			}
		}

		// With no filter, None also drops picks this cycle's output does not have.
		if (!selected && string.IsNullOrWhiteSpace(OutputFilter))
		{
			_selectedOutputs.Clear();
		}

		SaveOutputChoices();
		SyncOutputItems(_listedFiles);
		OnPropertyChanged(nameof(OutputSummary));
	}

	private void SaveOutputChoices()
	{
		UserConfigFile.TrySetValue(OutputKey, string.Join('|', _selectedOutputs.Order(StringComparer.OrdinalIgnoreCase)));
		UserConfigFile.Save(Node);
	}

	/// <summary>
	/// Brings <see cref="OutputFiles"/> in line with the picked paths: adds and loads new picks,
	/// drops unpicked ones, reloads a file a newer run rewrote (or that now comes from another
	/// cycle), and keeps a pick whose file this cycle does not have, marked as missing.
	/// </summary>
	private void SyncOutputItems(IReadOnlyList<AiracOutputGeojsonFile> files)
	{
		Dictionary<string, AiracOutputGeojsonFile> onDisk = files.ToDictionary(f => f.RelativePath, StringComparer.OrdinalIgnoreCase);

		foreach (string gone in _outputItems.Keys.Where(key => !_selectedOutputs.Contains(key)).ToList())
		{
			OutputFiles.Remove(_outputItems[gone]);
			_outputItems.Remove(gone);
		}

		foreach (string relative in _selectedOutputs.Order(StringComparer.OrdinalIgnoreCase))
		{
			_outputItems.TryGetValue(relative, out MapFileItem? item);

			if (onDisk.TryGetValue(relative, out AiracOutputGeojsonFile? file))
			{
				if (item is not null && !string.Equals(item.Path, file.FullPath, StringComparison.OrdinalIgnoreCase))
				{
					ReplaceOutputItem(relative, item, null);
					item = null;
				}

				if (item is null)
				{
					item = NewOutputItem(file.Name, file.FullPath);
					ReplaceOutputItem(relative, null, item);
					_ = LoadIntoAsync(item, file.FullPath, file.LastWriteUtc);
				}
				else if (item.LoadedWriteUtc != file.LastWriteUtc && !item.IsLoading)
				{
					_ = LoadIntoAsync(item, file.FullPath, file.LastWriteUtc);
				}
			}
			else
			{
				string expected = Path.Combine(OutputDirectory, relative);
				if (item is null || !string.Equals(item.Path, expected, StringComparison.OrdinalIgnoreCase))
				{
					MapFileItem missing = NewOutputItem(Path.GetFileNameWithoutExtension(relative), expected);
					ReplaceOutputItem(relative, item, missing);
					item = missing;
				}

				item.SetProblem(SelectedCycle is { } cycle ? $"Not in the AIRAC {cycle.Id} output" : "No cycle chosen");
			}
		}

		SyncLayers();
	}

	private MapFileItem NewOutputItem(string name, string path) =>
		new(name, path, Frozen(FileColors[StableIndex(name)]), SyncLayers, RemoveOutputItem, Zoom);

	private void ReplaceOutputItem(string relative, MapFileItem? old, MapFileItem? replacement)
	{
		int index = old is null ? -1 : OutputFiles.IndexOf(old);
		if (old is not null)
		{
			OutputFiles.Remove(old);
			_outputItems.Remove(relative);
		}

		if (replacement is not null)
		{
			if (index >= 0)
			{
				OutputFiles.Insert(index, replacement);
			}
			else
			{
				OutputFiles.Add(replacement);
			}

			_outputItems[relative] = replacement;
		}
	}

	private void RemoveOutputItem(MapFileItem item)
	{
		string? relative = _outputItems.FirstOrDefault(pair => ReferenceEquals(pair.Value, item)).Key;
		if (relative is null)
		{
			return;
		}

		if (OutputChoices.FirstOrDefault(c => string.Equals(c.File.RelativePath, relative, StringComparison.OrdinalIgnoreCase)) is { } choice)
		{
			choice.IsSelected = false;   // saves and syncs through OnOutputChoiceChanged
			return;
		}

		_selectedOutputs.Remove(relative);
		SaveOutputChoices();
		SyncOutputItems(_listedFiles);
		OnPropertyChanged(nameof(OutputSummary));
	}

	private void OpenOutputFolder()
	{
		try
		{
			Process.Start(new ProcessStartInfo("explorer.exe", $"\"{OutputDirectory}\"") { UseShellExecute = true });
		}
		catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException)
		{
			Toast.Warn("Could not open the folder", ex.Message);
		}
	}

	// ========================== user files ==============================

	private async void LoadFiles()
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

		List<MapFileItem> added = [];
		List<Task> loads = [];
		foreach (string path in dialog.FileNames)
		{
			SolidColorBrush brush = Frozen(FileColors[_userColorCursor++ % FileColors.Length]);
			MapFileItem item = new(Path.GetFileName(path), path, brush, SyncLayers, RemoveUserFile, Zoom);
			UserFiles.Add(item);
			added.Add(item);
			loads.Add(LoadIntoAsync(item, path, File.GetLastWriteTimeUtc(path)));
		}

		await Task.WhenAll(loads);

		List<MapFileItem> failed = [.. added.Where(i => i.HasProblem)];
		if (failed.Count > 0)
		{
			Toast.Warn("Some files could not be loaded", string.Join("; ", failed.Select(f => $"{f.Name} ({f.Detail})")));
		}

		List<MapLayer> loaded = [.. added.Where(i => i.Layer is not null).Select(i => i.Layer!)];
		if (loaded.Count > 0)
		{
			FrameLayersRequested?.Invoke(this, loaded);
		}
	}

	private void RemoveUserFile(MapFileItem item)
	{
		UserFiles.Remove(item);
		SyncLayers();
	}

	private void ClearUserFiles()
	{
		UserFiles.Clear();
		SyncLayers();
	}

	// ============================ shared ================================

	private void Zoom(MapFileItem item)
	{
		if (item.Layer is { } layer)
		{
			FrameLayersRequested?.Invoke(this, [layer]);
		}
	}

	private static async Task LoadIntoAsync(MapFileItem item, string path, DateTime writeUtc)
	{
		item.SetLoading();
		try
		{
			MapLayer layer = await Task.Run(() =>
			{
				IReadOnlyList<MapGeometry> geometries = GeoJsonReader.Read(File.ReadAllText(path));
				MapLayer built = new(item.Name, geometries, item.Swatch, thickness: 1.1, pointRadius: 2.4);
				ProjectedLayer.For(built);   // project off the UI thread, not on first draw
				return built;
			});

			item.LoadedWriteUtc = writeUtc;
			item.SetLoaded(layer);
		}
		catch (Exception ex) when (ex is FormatException or IOException or UnauthorizedAccessException)
		{
			item.LoadedWriteUtc = writeUtc;
			item.SetProblem(ex is FormatException ? "No map features in this file" : "Could not be read");
			AppLog.Warning("Map", $"Could not read '{path}': {ex.Message}");
		}
	}

	/// <summary>Rebuilds <see cref="Layers"/>: AIRAC layers, then output files, then the user's files.</summary>
	private void SyncLayers()
	{
		List<MapLayer> wanted =
		[
			.. AiracToggles.Where(t => t.IsVisible && t.Layers is not null).SelectMany(t => t.Layers!),
			.. OutputFiles.Where(f => f.IsVisible && f.Layer is not null).Select(f => f.Layer!),
			.. UserFiles.Where(f => f.IsVisible && f.Layer is not null).Select(f => f.Layer!),
		];

		if (wanted.SequenceEqual(Layers))
		{
			return;
		}

		Layers.Clear();
		foreach (MapLayer layer in wanted)
		{
			Layers.Add(layer);
		}

		CommandManager.InvalidateRequerySuggested();
	}

	/// <summary>A colour index that stays the same for a file name across runs and restarts.</summary>
	private static int StableIndex(string name)
	{
		int hash = 17;
		foreach (char c in name.ToUpperInvariant())
		{
			hash = unchecked((hash * 31) + c);
		}

		return (int)((uint)hash % (uint)FileColors.Length);
	}

	private static SolidColorBrush Frozen(Color color)
	{
		SolidColorBrush brush = new(color);
		brush.Freeze();
		return brush;
	}

	private static IEnumerable<string> Split(string? saved, char separator) =>
		string.IsNullOrWhiteSpace(saved)
			? []
			: saved.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	/// <summary>One cycle in the cycle picker.</summary>
	/// <param name="Id">The four-digit cycle ID.</param>
	/// <param name="Label">What the picker shows, e.g. <c>AIRAC 2610 · current</c>.</param>
	public sealed record CycleOption(string Id, string Label)
	{
		/// <inheritdoc />
		/// <remarks>The ComboBox's closed box shows this, not DisplayMemberPath.</remarks>
		public override string ToString() => Label;
	}
}
