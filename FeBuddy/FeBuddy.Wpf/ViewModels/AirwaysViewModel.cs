using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Threading;

using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels.Models;

using FEBuddyLibrary.Configuration;
using FEBuddyLibrary.Models.NASR.CSV;
using FEBuddyLibrary.Models.Services.Airac.Airways;
using FEBuddyLibrary.Models.Services.General;
using FEBuddyLibrary.Parsers.NASR.CSV;
using FEBuddyLibrary.Services.Airac.Airways;
using FEBuddyLibrary.Services.General;
using FEBuddyLibrary.Services.Airac;
using FEBuddyLibrary.Models.Services.Airac;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The Airways screen: the first screen in this shell that is genuinely wired to
/// <c>FEBuddyLibrary</c> rather than showing sample data. Pick a NASR source
/// folder and an output folder, configure the same settings
/// <c>AirwayService.Run</c> accepts, click Run, and see the real result.
/// </summary>
public sealed class AirwaysViewModel : ObservableObject
{
	private static readonly Regex AirwayIdPattern = new(@"^Airway '([^']+)':", RegexOptions.Compiled);

	private static readonly AirwayAltitudeClass[] AllClasses =
	{
		AirwayAltitudeClass.High, AirwayAltitudeClass.Low, AirwayAltitudeClass.Other
	};

	private readonly Dictionary<(AirwayAltitudeClass Class, EramTab Kind), EramDefault> _eramDefaults;
	private readonly DispatcherTimer _elapsedTimer;
	private Stopwatch? _stopwatch;

	// ---- paths ------------------------------------------------------

	private string _nasrSourceDirectory = string.Empty;
	private string _outputDirectory = string.Empty;

	// ---- AIRAC cycle download -----------------------------------------

	private AiracCyclePosition _selectedCyclePosition = AiracCyclePosition.Current;
	private bool _isDownloadingCycle;
	private string? _downloadStatusText;

	// ---- output settings ---------------------------------------------

	private AirwayGeojsonOutputBy _outputBy = AirwayGeojsonOutputBy.HighLow;
	private bool _bufferAirwayWaypoints;
	private bool _includeFebCustomProperties = true;
	private bool _includeAirwayWaypointIds = true;
	private bool _generateAliasFile = true;
	private bool _splitAtAntimeridian = true;
	private bool _includeCrcEramPropertyDefaults = true;
	private bool _prettyPrintOutput;

	// ---- region of interest -------------------------------------------

	private bool _filterByRoi;
	private string _swLat = string.Empty;
	private string _swLon = string.Empty;
	private string _neLat = string.Empty;
	private string _neLon = string.Empty;

	// ---- CRC ERAM defaults editor -------------------------------------

	private AirwayAltitudeClass _eramClass = AirwayAltitudeClass.High;
	private EramTab _eramTab = EramTab.Lines;

	// ---- run state -----------------------------------------------------

	private GenPhase _phase = GenPhase.Idle;
	private string? _runError;
	private double _elapsedSeconds;
	private int _airwayCount;
	private string? _aliasFilePath;
	private int _aliasLineCount;

	public AirwaysViewModel()
	{
		_eramDefaults = BuildDefaultEramBlocks();

		RunCommand = new RelayCommand(
			async () => await RunAsync(),
			() => Phase != GenPhase.Running &&
			      !string.IsNullOrWhiteSpace(NasrSourceDirectory) &&
			      !string.IsNullOrWhiteSpace(OutputDirectory));

		ResetCommand = new RelayCommand(Reset);
		ToggleWarningsCommand = new RelayCommand(() => IsWarningsExpanded = !IsWarningsExpanded);
		BrowseNasrCommand = new RelayCommand(BrowseNasrSourceDirectory);
		BrowseOutputCommand = new RelayCommand(BrowseOutputDirectory);
		OpenOutputCommand = new RelayCommand(OpenOutputFolder);
		DownloadCycleCommand = new RelayCommand(async () => await DownloadCycleAsync(), () => !IsDownloadingCycle);

		_elapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
		_elapsedTimer.Tick += (_, _) =>
		{
			if (_stopwatch is not null)
			{
				ElapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
			}
		};
	}

	// ---- paths ---------------------------------------------------------

	/// <summary>Directory containing an unzipped NASR 28-day subscription CSV set.</summary>
	public string NasrSourceDirectory
	{
		get => _nasrSourceDirectory;
		set => SetProperty(ref _nasrSourceDirectory, value);
	}

	/// <summary>Directory the Airways services write output under (a <c>FE-Buddy_Output\Airways</c> subtree is created inside it).</summary>
	public string OutputDirectory
	{
		get => _outputDirectory;
		set => SetProperty(ref _outputDirectory, value);
	}

	public ICommand BrowseNasrCommand { get; }

	public ICommand BrowseOutputCommand { get; }

	public ICommand OpenOutputCommand { get; }

	// ---- AIRAC cycle download -----------------------------------------

	/// <summary>Which cycle (relative to today, UTC) the Download button fetches.</summary>
	public AiracCyclePosition SelectedCyclePosition
	{
		get => _selectedCyclePosition;
		set
		{
			if (SetProperty(ref _selectedCyclePosition, value))
			{
				OnPropertyChanged(nameof(SelectedCycleLabel));
			}
		}
	}

	/// <summary>e.g. "Cycle 2610 - effective 01 Oct 2026", recomputed whenever the selected position changes.</summary>
	public string SelectedCycleLabel
	{
		get
		{
			try
			{
				AiracCycleInfo info = AiracCycleResolver.GetCycle(SelectedCyclePosition);
				return $"Cycle {info.AiracCycleId} - effective {info.EffectiveDateUtc:dd MMM yyyy}";
			}
			catch (Exception ex)
			{
				return $"Unavailable: {ex.Message}";
			}
		}
	}

	public bool IsDownloadingCycle
	{
		get => _isDownloadingCycle;
		private set
		{
			if (SetProperty(ref _isDownloadingCycle, value))
			{
				OnPropertyChanged(nameof(CanDownloadCycle));
			}
		}
	}

	/// <summary>Bound directly to the Download button's IsEnabled (no converter needed).</summary>
	public bool CanDownloadCycle => !IsDownloadingCycle;

	public string? DownloadStatusText
	{
		get => _downloadStatusText;
		private set
		{
			if (SetProperty(ref _downloadStatusText, value))
			{
				OnPropertyChanged(nameof(HasDownloadStatus));
			}
		}
	}

	public bool HasDownloadStatus => !string.IsNullOrEmpty(DownloadStatusText);

	public ICommand DownloadCycleCommand { get; }

	// ---- output settings -------------------------------------------

	public AirwayGeojsonOutputBy OutputBy
	{
		get => _outputBy;
		set
		{
			if (SetProperty(ref _outputBy, value))
			{
				OnPropertyChanged(nameof(OutputModeHint));
			}
		}
	}

	public string OutputModeHint => OutputBy switch
	{
		AirwayGeojsonOutputBy.None => "Airway data will not be written to GeoJSON. The alias file, if enabled, is unaffected.",
		AirwayGeojsonOutputBy.HighLow =>
			"Airways_High (highest MAA >= 18,000 ft), Airways_Low (0 < MAA < 18,000 ft), Airways_Other (neither).",
		AirwayGeojsonOutputBy.Designation =>
			"One file per designation - Airways_J.geojson, Airways_V.geojson, Airways_RN.geojson, ...",
		_ => string.Empty,
	};

	public bool BufferAirwayWaypoints
	{
		get => _bufferAirwayWaypoints;
		set => SetProperty(ref _bufferAirwayWaypoints, value);
	}

	public bool IncludeFebCustomProperties
	{
		get => _includeFebCustomProperties;
		set => SetProperty(ref _includeFebCustomProperties, value);
	}

	public bool IncludeAirwayWaypointIds
	{
		get => _includeAirwayWaypointIds;
		set => SetProperty(ref _includeAirwayWaypointIds, value);
	}

	public bool GenerateAliasFile
	{
		get => _generateAliasFile;
		set => SetProperty(ref _generateAliasFile, value);
	}

	public bool SplitAtAntimeridian
	{
		get => _splitAtAntimeridian;
		set => SetProperty(ref _splitAtAntimeridian, value);
	}

	public bool IncludeCrcEramPropertyDefaults
	{
		get => _includeCrcEramPropertyDefaults;
		set => SetProperty(ref _includeCrcEramPropertyDefaults, value);
	}

	/// <summary>Mirrors <c>FEBuddyLibrary.Configuration.DevMode.IsEnabled</c> for this run: pretty-printed GeoJSON instead of single-line.</summary>
	public bool PrettyPrintOutput
	{
		get => _prettyPrintOutput;
		set => SetProperty(ref _prettyPrintOutput, value);
	}

	// ---- region of interest -----------------------------------------

	public bool FilterByRoi
	{
		get => _filterByRoi;
		set => SetProperty(ref _filterByRoi, value);
	}

	public string SwLat { get => _swLat; set => SetProperty(ref _swLat, value); }

	public string SwLon { get => _swLon; set => SetProperty(ref _swLon, value); }

	public string NeLat { get => _neLat; set => SetProperty(ref _neLat, value); }

	public string NeLon { get => _neLon; set => SetProperty(ref _neLon, value); }

	// ---- CRC ERAM defaults editor -------------------------------------

	public AirwayAltitudeClass EramClass
	{
		get => _eramClass;
		set
		{
			if (SetProperty(ref _eramClass, value))
			{
				OnPropertyChanged(nameof(CurrentEramDefault));
			}
		}
	}

	public EramTab EramTab
	{
		get => _eramTab;
		set
		{
			if (SetProperty(ref _eramTab, value))
			{
				OnPropertyChanged(nameof(CurrentEramDefault));
				OnPropertyChanged(nameof(EramShowThickness));
				OnPropertyChanged(nameof(EramShowText));
			}
		}
	}

	public EramDefault CurrentEramDefault => _eramDefaults[(EramClass, EramTab)];

	public bool EramShowThickness => EramTab == EramTab.Lines;

	public bool EramShowText => EramTab == EramTab.Text;

	// ---- run state -----------------------------------------------------

	public GenPhase Phase
	{
		get => _phase;
		private set
		{
			if (SetProperty(ref _phase, value))
			{
				OnPropertyChanged(nameof(IsRunning));
				OnPropertyChanged(nameof(ShowRunPanel));
				OnPropertyChanged(nameof(RunSucceeded));
				OnPropertyChanged(nameof(RunFailed));
			}
		}
	}

	public bool IsRunning => Phase == GenPhase.Running;

	public bool ShowRunPanel => Phase != GenPhase.Idle;

	public string? RunError
	{
		get => _runError;
		private set
		{
			if (SetProperty(ref _runError, value))
			{
				OnPropertyChanged(nameof(RunSucceeded));
				OnPropertyChanged(nameof(RunFailed));
			}
		}
	}

	/// <summary>True once a run has finished with no exception. Used to decide between the results panel and the error panel.</summary>
	public bool RunSucceeded => Phase == GenPhase.Complete && RunError is null;

	/// <summary>True once a run has finished with an exception.</summary>
	public bool RunFailed => Phase == GenPhase.Complete && RunError is not null;

	public double ElapsedSeconds
	{
		get => _elapsedSeconds;
		private set
		{
			if (SetProperty(ref _elapsedSeconds, value))
			{
				OnPropertyChanged(nameof(ElapsedText));
			}
		}
	}

	public string ElapsedText => $"{ElapsedSeconds:0.0}s";

	public int AirwayCount
	{
		get => _airwayCount;
		private set => SetProperty(ref _airwayCount, value);
	}

	public ObservableCollection<AirwaysOutputFileRow> Files { get; } = [];

	public bool HasFiles => Files.Count > 0;

	public string? AliasFilePath
	{
		get => _aliasFilePath;
		private set
		{
			if (SetProperty(ref _aliasFilePath, value))
			{
				OnPropertyChanged(nameof(HasAliasFile));
			}
		}
	}

	public bool HasAliasFile => !string.IsNullOrEmpty(AliasFilePath);

	public int AliasLineCount
	{
		get => _aliasLineCount;
		private set => SetProperty(ref _aliasLineCount, value);
	}

	public ObservableCollection<AirwaysWarningGroup> WarningGroups { get; } = [];

	public bool HasWarnings => WarningGroups.Count > 0;

	private bool _isWarningsExpanded;

	/// <summary>
	/// Whether the warnings list is expanded. Starts collapsed on every run so a run with
	/// many warnings (e.g. buffering many short legs) doesn't bury the Run again / Open
	/// output folder / Reset buttons at the bottom of the panel - the warning count is still
	/// visible on the collapsed header.
	/// </summary>
	public bool IsWarningsExpanded
	{
		get => _isWarningsExpanded;
		set
		{
			if (SetProperty(ref _isWarningsExpanded, value))
			{
				OnPropertyChanged(nameof(WarningsToggleLabel));
			}
		}
	}

	public string WarningsToggleLabel => IsWarningsExpanded ? "Hide" : "Show";

	public ICommand ToggleWarningsCommand { get; }

	public ICommand RunCommand { get; }

	public ICommand ResetCommand { get; }

	// ---------------------------------------------------------------------

	private async Task RunAsync()
	{
		RunError = null;
		Phase = GenPhase.Running;
		Files.Clear();
		WarningGroups.Clear();
		IsWarningsExpanded = false;
		OnPropertyChanged(nameof(HasFiles));
		OnPropertyChanged(nameof(HasWarnings));
		AirwayCount = 0;
		AliasFilePath = null;
		AliasLineCount = 0;

		_stopwatch = Stopwatch.StartNew();
		ElapsedSeconds = 0;
		_elapsedTimer.Start();

		try
		{
			// This screen's own switch, not the AirwayService pipeline - it only
			// affects how GeoJsonFileWriter formats output.
			DevMode.IsEnabled = PrettyPrintOutput;

			NasrCsvDataCollection allNasrCsvData =
				await NasrCsvParserController.MainAsync(new[] { NasrSourceDirectory });

			Dictionary<string, string> settings = BuildAirwaySettingsDictionary();

			AirwayServiceResult result = await Task.Run(() => AirwayService.Run(allNasrCsvData, settings));

			ApplyResult(result);

			Phase = GenPhase.Complete;

			Toast.Success(
				"Airways run complete",
				$"{result.AirwayCount:N0} airways" +
				(result.GeojsonFilesWritten.Count > 0 ? $" · {result.GeojsonFilesWritten.Count} GeoJSON file(s)" : string.Empty) +
				(result.AliasFilePath is not null ? " · alias file written" : string.Empty) +
				(result.Warnings.Count > 0 ? $" · {result.Warnings.Count} warning(s)" : string.Empty) + ".");
		}
		catch (Exception ex)
		{
			RunError = ex.Message;
			Phase = GenPhase.Complete;
			Toast.Error("Airways run failed", ex.Message);
		}
		finally
		{
			_stopwatch?.Stop();
			ElapsedSeconds = _stopwatch?.Elapsed.TotalSeconds ?? ElapsedSeconds;
			_elapsedTimer.Stop();
		}
	}

	private void ApplyResult(AirwayServiceResult result)
	{
		AirwayCount = result.AirwayCount;

		foreach (string path in result.GeojsonFilesWritten)
		{
			int count = result.GeojsonFeatureCountsByFile.TryGetValue(path, out int c) ? c : 0;
			Files.Add(new AirwaysOutputFileRow(Path.GetFileName(path), path, count));
		}

		OnPropertyChanged(nameof(HasFiles));

		AliasFilePath = result.AliasFilePath;
		AliasLineCount = result.AliasAirwayLineCount;

		foreach (var group in GroupWarnings(result.Warnings))
		{
			WarningGroups.Add(group);
		}

		OnPropertyChanged(nameof(HasWarnings));
	}

	/// <summary>
	/// Groups warnings by the airway ID named at the start of the message ("Airway
	/// 'J3': ..."), falling back to a "General" bucket for warnings not tied to a
	/// specific airway (e.g. an unrecognized settings key).
	/// </summary>
	private static IEnumerable<AirwaysWarningGroup> GroupWarnings(IReadOnlyList<string> warnings)
	{
		return warnings
			.GroupBy(w =>
			{
				Match match = AirwayIdPattern.Match(w);
				return match.Success ? match.Groups[1].Value : "General";
			})
			.OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
			.Select(g => new AirwaysWarningGroup(g.Key, g.ToList()));
	}

	/// <summary>
	/// Converts the current screen state into the raw settings dictionary
	/// <c>AirwayService.Run</c> accepts. Mirrors <c>FEBuddyTest.HarnessSettings</c>.
	/// </summary>
	private Dictionary<string, string> BuildAirwaySettingsDictionary()
	{
		Dictionary<string, string> settings = new()
		{
			["OutputDirectory"] = OutputDirectory,
			["OutputBy"] = OutputBy.ToString(),
			["BufferAirwayWaypoints"] = YesNo(BufferAirwayWaypoints),
			["IncludeFebCustomProperties"] = YesNo(IncludeFebCustomProperties),
			["IncludeAirwayWaypointIds"] = YesNo(IncludeAirwayWaypointIds),
			["GenerateAliasFile"] = YesNo(GenerateAliasFile),
			["SplitAtAntimeridian"] = YesNo(SplitAtAntimeridian),
			["IncludeCrcEramPropertyDefaults"] = YesNo(IncludeCrcEramPropertyDefaults),
			["FilterByRoi"] = YesNo(FilterByRoi),
		};

		if (FilterByRoi)
		{
			settings["RoiSwLat"] = SwLat;
			settings["RoiSwLon"] = SwLon;
			settings["RoiNeLat"] = NeLat;
			settings["RoiNeLon"] = NeLon;
		}

		if (IncludeCrcEramPropertyDefaults)
		{
			foreach (AirwayAltitudeClass cls in AllClasses)
			{
				EramDefault line = _eramDefaults[(cls, EramTab.Lines)];
				settings[$"Crc.{cls}.Line.bcg"] = line.Bcg;
				settings[$"Crc.{cls}.Line.filters"] = line.Filters;
				settings[$"Crc.{cls}.Line.style"] = line.Style;
				settings[$"Crc.{cls}.Line.thickness"] = line.Thickness;

				EramDefault symbol = _eramDefaults[(cls, EramTab.Symbols)];
				settings[$"Crc.{cls}.Symbol.bcg"] = symbol.Bcg;
				settings[$"Crc.{cls}.Symbol.filters"] = symbol.Filters;
				settings[$"Crc.{cls}.Symbol.style"] = symbol.Style;
				settings[$"Crc.{cls}.Symbol.size"] = symbol.Size;

				EramDefault text = _eramDefaults[(cls, EramTab.Text)];
				settings[$"Crc.{cls}.Text.bcg"] = text.Bcg;
				settings[$"Crc.{cls}.Text.filters"] = text.Filters;
				settings[$"Crc.{cls}.Text.size"] = text.Size;
				settings[$"Crc.{cls}.Text.underline"] = YesNo(text.Underline);
				settings[$"Crc.{cls}.Text.xOffset"] = text.XOffset;
				settings[$"Crc.{cls}.Text.yOffset"] = text.YOffset;
			}
		}

		return settings;
	}

	private static string YesNo(bool value) => value ? "Y" : "N";

	private static Dictionary<(AirwayAltitudeClass, EramTab), EramDefault> BuildDefaultEramBlocks() => new()
	{
		[(AirwayAltitudeClass.High, EramTab.Lines)] = new EramDefault { Bcg = "3", Filters = "3", Style = "solid", Thickness = "1" },
		[(AirwayAltitudeClass.High, EramTab.Symbols)] = new EramDefault { Bcg = "3", Filters = "3", Style = "vor", Size = "1" },
		[(AirwayAltitudeClass.High, EramTab.Text)] = new EramDefault { Bcg = "3", Filters = "3", Size = "1" },

		[(AirwayAltitudeClass.Low, EramTab.Lines)] = new EramDefault { Bcg = "2", Filters = "2", Style = "shortDashed", Thickness = "1" },
		[(AirwayAltitudeClass.Low, EramTab.Symbols)] = new EramDefault { Bcg = "2", Filters = "2", Style = "vor", Size = "1" },
		[(AirwayAltitudeClass.Low, EramTab.Text)] = new EramDefault { Bcg = "2", Filters = "2", Size = "1" },

		[(AirwayAltitudeClass.Other, EramTab.Lines)] = new EramDefault { Bcg = "1", Filters = "1", Style = "longDashed", Thickness = "1" },
		[(AirwayAltitudeClass.Other, EramTab.Symbols)] = new EramDefault { Bcg = "1", Filters = "1", Style = "otherWaypoints", Size = "1" },
		[(AirwayAltitudeClass.Other, EramTab.Text)] = new EramDefault { Bcg = "1", Filters = "1", Size = "1" },
	};

	/// <summary>
	/// Resolves the selected AIRAC cycle, downloads and extracts it if it isn't already
	/// cached (see <c>NasrCycleDownloadService</c>), points <see cref="NasrSourceDirectory"/>
	/// at the result, and prunes any cached cycle that is no longer the previous, current, or
	/// next cycle - matching the dev notes' "keep 3 cycles" rule.
	/// </summary>
	private async Task DownloadCycleAsync()
	{
		IsDownloadingCycle = true;
		DownloadStatusText = "Resolving cycle...";

		try
		{
			AiracCycleInfo cycle = AiracCycleResolver.GetCycle(SelectedCyclePosition);

			Progress<AiracDownloadProgress> progress = new(p =>
			{
				DownloadStatusText = p.Phase switch
				{
					AiracDownloadPhase.AlreadyAvailable => $"Cycle {cycle.AiracCycleId} already downloaded.",
					AiracDownloadPhase.Downloading => p.PercentComplete.HasValue
						? $"Downloading cycle {cycle.AiracCycleId}... {p.PercentComplete:0}%"
						: $"Downloading cycle {cycle.AiracCycleId}...",
					AiracDownloadPhase.Extracting => $"Extracting cycle {cycle.AiracCycleId}...",
					AiracDownloadPhase.Complete => $"Cycle {cycle.AiracCycleId} ready.",
					_ => DownloadStatusText
				};
			});

			string folder = await NasrCycleDownloadService.EnsureCycleAvailableAsync(cycle, progress: progress);

			NasrSourceDirectory = folder;

			// Keep at most 3 cycles cached: previous, current, next. Pruning is a
			// nice-to-have - never let a pruning failure hide a successful download.
			try
			{
				string[] cycleIdsToKeep =
				{
					AiracCycleResolver.GetCycle(AiracCyclePosition.Previous).AiracCycleId,
					AiracCycleResolver.GetCycle(AiracCyclePosition.Current).AiracCycleId,
					AiracCycleResolver.GetCycle(AiracCyclePosition.Next).AiracCycleId,
				};

				NasrCycleDownloadService.PruneStaleCycles(cycleIdsToKeep);
			}
			catch
			{
				// Ignored - see remarks above.
			}

			Toast.Success("Cycle downloaded", $"Cycle {cycle.AiracCycleId} is ready at {folder}.");
		}
		catch (Exception ex)
		{
			DownloadStatusText = null;
			Toast.Error("Download failed", ex.Message);
		}
		finally
		{
			IsDownloadingCycle = false;
		}
	}

	private void BrowseNasrSourceDirectory()
	{
		Microsoft.Win32.OpenFolderDialog dialog = new()
		{
			Title = "Select the unzipped NASR CSV folder",
			InitialDirectory = Directory.Exists(NasrSourceDirectory) ? NasrSourceDirectory : null,
		};

		if (dialog.ShowDialog() == true)
		{
			NasrSourceDirectory = dialog.FolderName;
		}
	}

	private void BrowseOutputDirectory()
	{
		Microsoft.Win32.OpenFolderDialog dialog = new()
		{
			Title = "Select the Airways output folder",
			InitialDirectory = Directory.Exists(OutputDirectory) ? OutputDirectory : null,
		};

		if (dialog.ShowDialog() == true)
		{
			OutputDirectory = dialog.FolderName;
		}
	}

	private void OpenOutputFolder()
	{
		string airwaysOutput = Path.Combine(OutputDirectory, "FE-Buddy_Output", "Airways");
		string target = Directory.Exists(airwaysOutput) ? airwaysOutput : OutputDirectory;

		if (!Directory.Exists(target))
		{
			Toast.Warn("Nothing to open", "That output folder doesn't exist yet - run Airways first.");
			return;
		}

		Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
	}

	private void Reset()
	{
		_elapsedTimer.Stop();
		Phase = GenPhase.Idle;
		RunError = null;
		ElapsedSeconds = 0;
		Files.Clear();
		WarningGroups.Clear();
		IsWarningsExpanded = false;
		OnPropertyChanged(nameof(HasFiles));
		OnPropertyChanged(nameof(HasWarnings));
		AirwayCount = 0;
		AliasFilePath = null;
		AliasLineCount = 0;

		OutputBy = AirwayGeojsonOutputBy.HighLow;
		BufferAirwayWaypoints = false;
		IncludeFebCustomProperties = true;
		IncludeAirwayWaypointIds = true;
		GenerateAliasFile = true;
		SplitAtAntimeridian = true;
		IncludeCrcEramPropertyDefaults = true;
		PrettyPrintOutput = false;
		FilterByRoi = false;
		SwLat = SwLon = NeLat = NeLon = string.Empty;
		EramClass = AirwayAltitudeClass.High;
		EramTab = EramTab.Lines;
		SelectedCyclePosition = AiracCyclePosition.Current;
		DownloadStatusText = null;
	}
}
