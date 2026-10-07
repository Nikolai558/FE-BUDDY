using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.Views;
using FeBuddy.Wpf.Views.Models;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Domain.Airac;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>AIRAC Services</b> screen: a General tab (the cycle, and the table of sub-services and
/// their outputs), a tab per sub-service, a File Names tab to rename any of the files they write, a
/// Preview Settings tab that summarises the lot and runs the service, and a Review tab for the
/// run's outcome.
/// </summary>
/// <remarks>
/// <para>
/// The tab machinery lives in <see cref="TabbedServiceViewModel"/> and is deliberately generic -
/// AIRAC Service is simply the first first-tier service built on it. Everything specific to AIRAC
/// lives here: readiness gating on the cycle cache, loading the selected cycle's parsed data, and
/// the run itself.
/// </para>
/// <para>
/// Every sub-service has a tab in the rail from the start, each reading its outputs from its row
/// on the General tab. A sub-service left out there is greyed out (<see cref="ServiceTabViewModel.IsAvailable"/>)
/// and skipped by the run, its checks and the Preview Settings tab, but keeps what was typed on it.
/// Concatenate Aliases isn't in the table: its tab is greyed out while no included sub-service makes
/// an alias file.
/// </para>
/// </remarks>
public sealed class AiracServiceViewModel : TabbedServiceViewModel
{
	private readonly Dispatcher _dispatcher;
	private readonly AiracGeneralTabViewModel _general;
	private readonly FileNamesViewModel _fileNames = new();
	private readonly ServicePreviewTabViewModel _preview;
	private readonly DuplicateAliasesCardViewModel _duplicateAliases;
	private readonly ServiceRunReviewTabViewModel _runReview = new();
	private readonly Dictionary<string, ServiceTabViewModel> _tabsByKey = new(StringComparer.OrdinalIgnoreCase);

	private bool _runReviewShown;
	private NasrCsvDataCollection? _parsedForSelectedCycle;
	private string? _parsedCycleId;

	/// <summary>Builds the screen, restores the saved tabs, and starts loading the selected cycle.</summary>
	public AiracServiceViewModel()
	{
		_dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

		RunCommand = new RelayCommand(async () => await RunAsync(), () => !IsRunning && IsReady);

		_general = new AiracGeneralTabViewModel();
		_general.SubServicesChanged += (_, _) => SyncSubServiceTabs();
		_general.CycleChanged += (_, _) =>
		{
			_fileNames.RefreshFiles();
			_ = LoadCycleDataAsync();
		};

		// What the run does with duplicate alias commands is the run's, not any tab's, so it sits on Preview Settings.
		_duplicateAliases = new DuplicateAliasesCardViewModel(
			() => Tabs.OfType<GeojsonSubServiceViewModel>().Any(t => t.IsAvailable && t.WritesAliasFile));
		_preview = new ServicePreviewTabViewModel("Preview Settings", "Run AIRAC Service", RunCommand, () => Tabs.Where(t => t.IsAvailable))
		{
			RunOptions = _duplicateAliases,
		};

		_fileNames.AttachToService(
			FilesTheRunWrites,
			() => AiracOutputPaths.CycleFolderName(AppEnvironment.GetAiracCycle(_general.SelectedCyclePosition).AiracCycleId));

		AiracCycleDataCache.Instance.StateChanged += (_, _) => _dispatcher.BeginInvoke(RefreshReadiness);
		AppEnvironment.Changed += (_, _) => _dispatcher.BeginInvoke(RefreshReadiness);

		// The Concatenate Aliases tab lists what the other tabs put into Combined_Alias.txt; the File
		// Names tab lists every file they write, and a new one needs a name. Re-read both whenever the
		// user moves between tabs, so their lists and their dots in the rail follow edits made elsewhere.
		PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(SelectedTab))
			{
				ConcatenateAliasesTab?.RefreshFeBuddyAliasFiles();
				_fileNames.RefreshFiles();
			}
		};

		BuildSubServiceTabs();
		SyncSubServiceTabs();
		RefreshReadiness();
		_ = LoadCycleDataAsync();
	}

	/// <summary>Runs the AIRAC Service for every included sub-service. Hosted on the Preview Settings tab.</summary>
	public ICommand RunCommand { get; }

	/// <inheritdoc />
	public override string ScreenTitle => "AIRAC Services";

	/// <summary>
	/// The AIRAC data readiness. Controls whether the cycle-dependent lists and the run are enabled.
	/// </summary>
	public AiracCycleReadiness Readiness =>
		AiracCycleDataCache.Instance.Entries.Count == 0 ? AiracCycleReadiness.Waiting
		: AiracCycleDataCache.Instance.ComputeReadiness();

	/// <summary>Whether the service is usable (Ready or Degraded).</summary>
	public bool IsReady => Readiness is AiracCycleReadiness.Ready or AiracCycleReadiness.Degraded;

	/// <inheritdoc />
	protected override ServiceTabViewModel GeneralTab => _general;

	/// <inheritdoc />
	protected override ServicePreviewTabViewModel PreviewTab => _preview;

	/// <summary>
	/// The run-review tab, once a run has started. Held back until then so the rail does not
	/// carry an empty tab about a run that has not happened.
	/// </summary>
	protected override ServiceTabViewModel? PostRunTab => _runReviewShown ? _runReview : null;

	/// <summary>The Airways tab while it is included, otherwise <see langword="null"/>.</summary>
	private AirwaysViewModel? AirwaysTab => TabFor<AirwaysViewModel>(AiracSubServices.AirwaysKey);

	/// <summary>The Airports tab while it is included, otherwise <see langword="null"/>.</summary>
	private AirportsViewModel? AirportsTab => TabFor<AirportsViewModel>(AiracSubServices.AirportsKey);

	/// <summary>The Departures tab while it is included, otherwise <see langword="null"/>.</summary>
	private DeparturesViewModel? DeparturesTab => TabFor<DeparturesViewModel>(AiracSubServices.DeparturesKey);

	/// <summary>The Arrivals tab while it is included, otherwise <see langword="null"/>.</summary>
	private ArrivalsViewModel? ArrivalsTab => TabFor<ArrivalsViewModel>(AiracSubServices.ArrivalsKey);

	/// <summary>The NAVAIDs tab while it is included, otherwise <see langword="null"/>.</summary>
	private NavaidsViewModel? NavaidsTab => TabFor<NavaidsViewModel>(AiracSubServices.NavaidsKey);

	/// <summary>The ARTCC Boundaries tab while it is included, otherwise <see langword="null"/>.</summary>
	private ArtccBoundariesViewModel? ArtccBoundariesTab => TabFor<ArtccBoundariesViewModel>(AiracSubServices.ArtccBoundariesKey);

	/// <summary>The Fixes tab while it is included, otherwise <see langword="null"/>.</summary>
	private FixesViewModel? FixesTab => TabFor<FixesViewModel>(AiracSubServices.FixesKey);

	/// <summary>The Wx Stations tab while it is included, otherwise <see langword="null"/>.</summary>
	private WxStationsViewModel? WxStationsTab => TabFor<WxStationsViewModel>(AiracSubServices.WxStationsKey);

	/// <summary>The Procedures tab while it is included, otherwise <see langword="null"/>.</summary>
	private ProceduresViewModel? ProceduresTab => TabFor<ProceduresViewModel>(AiracSubServices.ProceduresKey);

	/// <summary>The Telephony tab while it is included, otherwise <see langword="null"/>.</summary>
	private TelephonyViewModel? TelephonyTab => TabFor<TelephonyViewModel>(AiracSubServices.TelephonyKey);

	/// <summary>The Concatenate Aliases tab while it takes part (an alias file is being made), otherwise <see langword="null"/>.</summary>
	private ConcatenateAliasesViewModel? ConcatenateAliasesTab => TabFor<ConcatenateAliasesViewModel>(AiracSubServices.ConcatenateAliasesKey);

	/// <summary>Every sub-service tab, included or not: for readiness and the cycle's lists, which every tab follows.</summary>
	private IReadOnlyList<ISubServiceRunTarget> RunTargets =>
		[.. Tabs.OfType<ISubServiceRunTarget>()];

	/// <summary>The sub-service tabs that take part in a run: the included ones.</summary>
	private IReadOnlyList<ISubServiceRunTarget> IncludedRunTargets =>
		[.. Tabs.Where(t => t.IsAvailable).OfType<ISubServiceRunTarget>()];

	/// <summary>Returns an included sub-service's tab of the expected type, or <see langword="null"/>.</summary>
	/// <typeparam name="T">The tab's view-model type.</typeparam>
	/// <param name="key">The sub-service key from <see cref="AiracSubServices"/>.</param>
	/// <returns>The tab, while it takes part in the run.</returns>
	private T? TabFor<T>(string key) where T : class =>
		_tabsByKey.TryGetValue(key, out ServiceTabViewModel? tab) && tab.IsAvailable
			? tab as T
			: null;

	private string WaitingMessage => Readiness switch
	{
		AiracCycleReadiness.Unavailable => "The current AIRAC cycle failed to download or parse. The AIRAC Service is unavailable — see the Dashboard activity log.",
		_ => "Waiting for AIRAC data to finish downloading and parsing. This service will be available in a moment.",
	};

	/// <summary>
	/// Builds every sub-service's tab, once: each takes its outputs from its row on the General tab,
	/// and tells the screen when the files it writes change, which can bring Concatenate Aliases in.
	/// </summary>
	private void BuildSubServiceTabs()
	{
		foreach (SubServiceDescriptor descriptor in AiracSubServices.All.OrderBy(d => d.Order))
		{
			ServiceTabViewModel tab = descriptor.CreateTab();
			_tabsByKey[descriptor.Key] = tab;

			if (tab is GeojsonSubServiceViewModel subService)
			{
				if (_general.RowFor(descriptor.Key) is { } row)
				{
					subService.AttachOutputs(row);
				}

				subService.FilesChanged += (_, _) => RefreshConcatenateAliasesAvailability();
			}

			if (tab is ISubServiceRunTarget target)
			{
				target.SetReadiness(IsReady);
			}

			if (tab is ConcatenateAliasesViewModel concatenate)
			{
				concatenate.AttachToService(
					d => TabFor<ServiceTabViewModel>(d.Key),
					shown => SelectedTab = shown);
			}
		}
	}

	/// <summary>
	/// Puts every sub-service's tab in the rail, greying out the ones left out on the General tab.
	/// The File Names tab follows them while any is included.
	/// </summary>
	private void SyncSubServiceTabs()
	{
		List<ServiceTabViewModel> tabs = [];

		foreach (SubServiceDescriptor descriptor in AiracSubServices.All.OrderBy(d => d.Order))
		{
			ServiceTabViewModel tab = _tabsByKey[descriptor.Key];

			if (_general.RowFor(descriptor.Key) is { } row)
			{
				tab.SetAvailability(row.IsIncluded,
					$"{descriptor.Help?.Summary}\nIt's left out of the run. To include it, tick {descriptor.DisplayName} under Include on the General tab.");
			}

			tabs.Add(tab);
		}

		RefreshConcatenateAliasesAvailability();

		if (_general.IncludedSubServices.Any())
		{
			tabs.Add(_fileNames);
		}

		RebuildTabs(tabs);
		RefreshDownloadedDataStatus();
		RefreshProceduresData();
		ConcatenateAliasesTab?.RefreshFeBuddyAliasFiles();
		_fileNames.RefreshFiles();
	}

	/// <summary>
	/// Brings Concatenate Aliases in while an included sub-service makes an alias file, and greys it
	/// out otherwise: it has nothing of FE-Buddy's to combine.
	/// </summary>
	private void RefreshConcatenateAliasesAvailability()
	{
		if (!_tabsByKey.TryGetValue(AiracSubServices.ConcatenateAliasesKey, out ServiceTabViewModel? concatenate))
		{
			return;
		}

		bool anyAliasFile = _tabsByKey.Values.Where(t => t.IsAvailable).OfType<GeojsonSubServiceViewModel>().Any(tab => tab.WritesAliasFile);
		bool wasAvailable = concatenate.IsAvailable;

		concatenate.SetAvailability(anyAliasFile,
			$"{AiracSubServices.All.Single(d => d.Key == AiracSubServices.ConcatenateAliasesKey).Help?.Summary}\n" +
			"No alias file is being made. To make one, tick a sub-service and its Alias box on the General tab.");

		// Left on a tab that just went grey, go back to the start.
		if (wasAvailable && !anyAliasFile && ReferenceEquals(SelectedTab, concatenate))
		{
			SelectedTab = Tabs.FirstOrDefault();
		}
	}

	/// <summary>
	/// Every file the included sub-services' settings write right now, for the File Names tab: each
	/// tab's own files, then the two the run itself writes whenever an alias file is written -
	/// <c>Duplicate_Alias_Commands.txt</c>, and <c>Combined_Alias.txt</c> while Concatenate Aliases
	/// combines them.
	/// </summary>
	/// <returns>The files.</returns>
	private IEnumerable<OutputFileEntry> FilesTheRunWrites()
	{
		GeojsonSubServiceViewModel[] tabs = [.. Tabs.Where(t => t.IsAvailable).OfType<GeojsonSubServiceViewModel>()];

		foreach (OutputFileEntry file in tabs.SelectMany(tab => tab.OutputFileEntries()))
		{
			yield return file;
		}

		if (tabs.Any(tab => tab.WritesAliasFile))
		{
			yield return OutputFileEntry.Renamable(AiracOutputPaths.DuplicateAliasReportFileName, string.Empty, "AIRAC Service");
		}

		if (ConcatenateAliasesTab is { CombineAliasFiles: true } combine)
		{
			yield return OutputFileEntry.Renamable(AiracOutputPaths.CombinedAliasFileName, AiracOutputPaths.AliasFolder, combine.Title);
		}
	}

	private void RefreshReadiness()
	{
		OnPropertyChanged(nameof(Readiness));
		OnPropertyChanged(nameof(IsReady));

		_general.SetReadiness(IsReady, WaitingMessage);

		foreach (ISubServiceRunTarget target in RunTargets)
		{
			target.SetReadiness(IsReady);
		}

		CommandManager.InvalidateRequerySuggested();

		_ = LoadCycleDataAsync();
	}

	private async Task LoadCycleDataAsync()
	{
		if (!IsReady)
		{
			return;
		}

		string cycleId = AppEnvironment.GetAiracCycle(_general.SelectedCyclePosition).AiracCycleId;

		if (_parsedCycleId == cycleId && _parsedForSelectedCycle is not null)
		{
			return;
		}

		try
		{
			NasrCsvDataCollection data = await AiracCycleDataCache.Instance.GetAsync(cycleId).ConfigureAwait(false);
			_parsedForSelectedCycle = data;
			_parsedCycleId = cycleId;

			await _dispatcher.BeginInvoke(() =>
			{
				foreach (ISubServiceRunTarget target in RunTargets)
				{
					target.LoadCycleDependentLists(data);
				}

				RefreshProceduresData();
			});
		}
		catch (Exception ex)
		{
			AppLog.Warning("AiracService", $"Could not load parsed data for cycle {cycleId}: {ex.Message}");
		}
	}

	/// <summary>
	/// Tells the included Wx Stations and Telephony tabs how old FE-Buddy's kept copies of their data
	/// are. That data is not part of any AIRAC cycle - every run downloads the latest copy - so it
	/// is re-read when the sub-services change on the General tab and after every run, not when the
	/// selected cycle changes.
	/// </summary>
	private void RefreshDownloadedDataStatus()
	{
		WxStationsTab?.RefreshStationData();
		TelephonyTab?.RefreshTelephonyData();
	}

	/// <summary>
	/// Tells the included Procedures tab about the selected cycle's FAA d-TPP Metafile (and the
	/// previous cycle's, for linking deleted procedures back to their chart). Unlike the NASR
	/// cycle data <see cref="ISubServiceRunTarget.LoadCycleDependentLists"/> hands out, the
	/// metafile is loaded here, off the UI thread - it is up to 16 MB and takes about 0.3 s to
	/// parse.
	/// </summary>
	private void RefreshProceduresData()
	{
		if (ProceduresTab is not { } tab || _parsedCycleId is not { } cycleId)
		{
			return;
		}

		AiracCycleInfo selectedCycle = AppEnvironment.GetAiracCycle(_general.SelectedCyclePosition);
		string previousCycleId = AiracCycleResolver.GetCycle(AiracCyclePosition.Previous, selectedCycle.EffectiveDateUtc).AiracCycleId;

		_ = LoadProceduresDataAsync(tab, cycleId, previousCycleId);
	}

	private async Task LoadProceduresDataAsync(ProceduresViewModel tab, string cycleId, string previousCycleId)
	{
		try
		{
			DtppMetafileDataCollection? dtpp = await AiracCycleDataCache.Instance.GetDtppAsync(cycleId).ConfigureAwait(false);

			bool previousAvailable = AiracCycleDataCache.Instance.FindDtppFile(previousCycleId) is not null;

			DateTime? downloadedLocal = AiracCycleDataCache.Instance.FindDtppFile(cycleId) is { } file
				? File.GetLastWriteTime(file)
				: null;

			await _dispatcher.BeginInvoke(() =>
			{
				// The user may have picked another cycle while this one was parsing; its own load
				// reports in separately, so a stale result must not overwrite it.
				if (_parsedCycleId == cycleId)
				{
					tab.SetDtppData(cycleId, dtpp, previousCycleId, previousAvailable, downloadedLocal);
				}
			});
		}
		catch (Exception ex)
		{
			AppLog.Warning("AiracService", $"Could not load the d-TPP Metafile for cycle {cycleId}: {ex.Message}");
		}
	}

	/// <summary>
	/// Saves anything unsaved (with the user's blessing), refuses to start while a tab is invalid,
	/// asks what to do with an earlier run's files, then runs every included sub-service that has
	/// a backend.
	/// </summary>
	private async Task RunAsync()
	{
		// The Concatenate Aliases tab lists the alias files the other tabs make, and the File Names
		// tab's validity depends on which files they write.
		ConcatenateAliasesTab?.RefreshFeBuddyAliasFiles();
		_fileNames.RefreshFiles();

		if (!TrySaveDirtyTabs() || !EnsureNoInvalidTabs())
		{
			return;
		}

		ServiceTabViewModel[] runnable = [.. Tabs.Where(t => t.IsAvailable && t.IsRunnable
			&& !ReferenceEquals(t, GeneralTab) && !ReferenceEquals(t, _fileNames) && !ReferenceEquals(t, PreviewTab))];

		if (runnable.Length == 0)
		{
			Toast.Warn("Nothing to run", "Include a sub-service on the General tab first.");
			return;
		}

		AiracCycleInfo cycle = AppEnvironment.GetAiracCycle(_general.SelectedCyclePosition);

		// The only sub-service-specific lines in the run: which block each tab's settings belong
		// to. Everything else goes through ISubServiceRunTarget.
		AiracServiceSettings settings = new()
		{
			SelectedCycle = cycle,
			OutputDirectory = OutputPreferences.Directory,
			AddFeBuddyOutputFolder = OutputPreferences.AddFeBuddyOutputFolder,
			// Settings ▸ Facility Profile: listed first in the duplicate alias report.
			PrimaryFacility = UserConfigFile.GetValue(SettingsViewModel.ArtccKey)?.Trim() is { Length: > 0 } facility
				? facility.ToUpperInvariant()
				: null,
			FileNames = _fileNames.BuildFileNamesBlock(),
			ReviewDuplicateAliases = DuplicateAliasesCardViewModel.LoadReview(),
			DuplicateAliasChoices = DuplicateAliasesCardViewModel.LoadChoices(),
			Airways = AirwaysTab?.BuildSettingsBlock(),
			Airports = AirportsTab?.BuildSettingsBlock(),
			Departures = DeparturesTab?.BuildSettingsBlock(),
			Arrivals = ArrivalsTab?.BuildSettingsBlock(),
			Navaids = NavaidsTab?.BuildSettingsBlock(),
			ArtccBoundaries = ArtccBoundariesTab?.BuildSettingsBlock(),
			Fixes = FixesTab?.BuildSettingsBlock(),
			WxStations = WxStationsTab?.BuildSettingsBlock(),
			Procedures = ProceduresTab?.BuildSettingsBlock(),
			Telephony = TelephonyTab?.BuildSettingsBlock(),
			ConcatenateAliases = ConcatenateAliasesTab?.BuildSettingsBlock(),
		};

		if (AiracService.HasExistingOutput(settings))
		{
			if (AskAboutExistingOutput(settings) is not { } existingOutput)
			{
				return;
			}

			settings = settings with { ExistingOutput = existingOutput };
		}

		IReadOnlyList<ISubServiceRunTarget> targets = IncludedRunTargets;
		string[] runningTabTitles = [.. Tabs
			.Where(t => t.IsAvailable && t is ISubServiceRunTarget)
			.Select(t => t.Title)];

		IsRunning = true;

		_runReview.BeginRun(runningTabTitles);
		_runReviewShown = true;
		ShowPostRunTab();

		try
		{
			// Made here, on the UI thread, Progress<T> hands each report to the UI thread itself: one
			// hop, queued ahead of this method's own continuation, so every report lands before
			// CompleteRun. A second hop (a BeginInvoke in the handler) let the last report of a quick
			// run - "Checking the alias files for duplicate commands" - land after CompleteRun and
			// leave its step "working..." for good. The library reports 100 when a sub-service is done.
			var progress = new Progress<AiracServiceProgress>(p =>
				_runReview.ReportStep(p.SubService, p.Message, p.PercentComplete >= 100));

			AiracServiceResult result = await AiracService.RunAsync(settings, progress, reviewDuplicates: ReviewDuplicatesAsync);

			// Saved before anything else can fail: the user made these choices to be kept.
			_duplicateAliases.SaveChoices(result.DuplicateAliasChoicesMade);

			string summary = SummarizeRun(cycle.AiracCycleId, result);
			SubServiceRunResult[] subServiceResults = [.. targets
				.Select(t => t.DescribeRunResult(result))
				.OfType<SubServiceRunResult>()];

			// A run stopped at the duplicates deleted its alias files.
			string[] files = [.. CollectFilesWritten(result).Where(file => !result.StoppedAtDuplicateReview || File.Exists(file))];

			_runReview.CompleteRun(
				result,
				summary,
				subServiceResults,
				files,
				ExistingFolder(result.OutputDirectory));

			if (result.StoppedAtDuplicateReview)
			{
				Toast.Warn("AIRAC Service stopped", "You stopped it at the duplicate alias commands, so no alias file was saved.");
			}
			else
			{
				Toast.Success("AIRAC Service complete", summary);
			}
		}
		catch (Exception ex)
		{
			// A run that fails part way may already have written files; offer the folder if so.
			_runReview.FailRun(ex.Message, outputDirectory: ExistingFolder(settings.CycleOutputDirectory));
			Toast.Error("AIRAC Service failed", ex.Message);
		}
		finally
		{
			IsRunning = false;

			// The run replaced the kept Wx Stations and telephony copies when it could download them.
			RefreshDownloadedDataStatus();
		}
	}

	/// <summary>
	/// Opens the duplicate alias commands window, when the run stops for the user's choices. The run
	/// calls this off the UI thread, so the window opens on it.
	/// </summary>
	/// <param name="review">What the run asks.</param>
	/// <param name="cancellationToken">Unused: the window is modal, and the run waits for it.</param>
	/// <returns>The choices, or <see langword="null"/> to stop the run.</returns>
	private Task<IReadOnlyList<DuplicateAliasRule>?> ReviewDuplicatesAsync(DuplicateAliasReview review, CancellationToken cancellationToken) =>
		_dispatcher.InvokeAsync(() => DuplicateAliasReviewWindow.Review(Application.Current?.MainWindow, review)).Task;

	/// <summary>
	/// Asks what to do with the files an earlier run of this cycle left in its folder. Overwrite is
	/// the default.
	/// </summary>
	/// <param name="settings">The run's settings.</param>
	/// <returns>The user's choice, or <see langword="null"/> when they cancelled the run.</returns>
	private static ExistingOutputAction? AskAboutExistingOutput(AiracServiceSettings settings)
	{
		string cycleId = settings.SelectedCycle.AiracCycleId;

		ConfirmChoice choice = ConfirmWindow.ShowChoice(
			Application.Current?.MainWindow,
			$"AIRAC cycle {cycleId} already run",
			$"Looks like AIRAC cycle {cycleId} has already been run at one point: `{settings.CycleOutputDirectory}` "
			+ "already has files in it. Select what you would like to happen:"
			+ Environment.NewLine + Environment.NewLine
			+ "Overwrite files - this run's files replace the old ones; any other old file is left as it is."
			+ Environment.NewLine
			+ $"Delete all files - everything in `{AiracOutputPaths.CycleFolderName(cycleId)}` is permanently deleted first, "
			+ "so it holds only this run's files.",
			confirmText: "Overwrite files",
			alternativeText: "Delete all files");

		return choice switch
		{
			ConfirmChoice.Confirm => ExistingOutputAction.Overwrite,
			ConfirmChoice.Alternative => ExistingOutputAction.DeleteExisting,
			_ => null,
		};
	}

	/// <summary>The folder the Review tab's "Open output folder" opens, once the run has written into it.</summary>
	/// <param name="directory">The run's cycle folder.</param>
	/// <returns>The folder, or <see langword="null"/> when nothing was written.</returns>
	private static string? ExistingFolder(string directory) => Directory.Exists(directory) ? directory : null;

	/// <summary>Every file the run wrote, across sub-services.</summary>
	/// <param name="result">The aggregated result.</param>
	/// <returns>The paths, in sub-service order.</returns>
	private static string[] CollectFilesWritten(AiracServiceResult result)
	{
		List<string> files = [];

		if (result.Airways is { } airways)
		{
			files.AddRange(airways.GeojsonFilesWritten);

			if (airways.AliasFilePath is { } airwayAlias)
			{
				files.Add(airwayAlias);
			}
		}

		if (result.Airports is { } airports)
		{
			files.AddRange(airports.GeojsonFilesWritten);

			if (airports.AliasFilePath is { } airportAlias)
			{
				files.Add(airportAlias);
			}
		}

		if (result.Departures is { } departures)
		{
			files.AddRange(departures.GeojsonFilesWritten);

			if (departures.AliasFilePath is { } departureAlias)
			{
				files.Add(departureAlias);
			}
		}

		if (result.Arrivals is { } arrivals)
		{
			files.AddRange(arrivals.GeojsonFilesWritten);

			if (arrivals.AliasFilePath is { } arrivalAlias)
			{
				files.Add(arrivalAlias);
			}
		}

		if (result.Navaids is { } navaids)
		{
			files.AddRange(navaids.GeojsonFilesWritten);

			if (navaids.AliasFilePath is { } navaidAlias)
			{
				files.Add(navaidAlias);
			}
		}

		if (result.ArtccBoundaries is { } artccBoundaries)
		{
			files.AddRange(artccBoundaries.GeojsonFilesWritten);
		}

		if (result.Fixes is { } fixes)
		{
			files.AddRange(fixes.GeojsonFilesWritten);
		}

		if (result.WxStations is { } wxStations)
		{
			files.AddRange(wxStations.GeojsonFilesWritten);
		}

		if (result.Procedures is { } procedures)
		{
			files.AddRange(procedures.FilesWritten);

			if (procedures.AliasFilePath is { } chartRecallAlias)
			{
				files.Add(chartRecallAlias);
			}
		}

		if (result.Telephony?.AliasFilePath is { } telephonyAlias)
		{
			files.Add(telephonyAlias);
		}

		if (result.DuplicateAliasReport is { } duplicateAliasReport)
		{
			files.Add(duplicateAliasReport.FilePath);
		}

		if (result.CombinedAlias?.FilePath is { } combinedAlias)
		{
			files.Add(combinedAlias);
		}

		return [.. files];
	}

	/// <summary>Builds the one-line "what the run produced" summary for the completion toast.</summary>
	/// <param name="cycleId">The cycle that was run.</param>
	/// <param name="result">The aggregated result.</param>
	/// <returns>The toast message.</returns>
	private static string SummarizeRun(string cycleId, AiracServiceResult result)
	{
		List<string> parts = [];

		if (result.Airways is { } airways)
		{
			parts.Add($"{airways.AirwayCount:N0} airway(s)"
				+ (airways.ExcludedAirwayIds.Count > 0 ? $", {airways.ExcludedAirwayIds.Count} excluded" : string.Empty));
		}

		if (result.Airports is { } airports)
		{
			parts.Add($"{airports.AirportCount:N0} airport(s)");
		}

		if (result.Departures is { } departures)
		{
			parts.Add($"{departures.AirportProcedureCount:N0} airport departure(s)"
				+ (departures.SkippedForMissingPointsCount > 0 ? $", {departures.SkippedForMissingPointsCount} skipped" : string.Empty));
		}

		if (result.Arrivals is { } arrivals)
		{
			parts.Add($"{arrivals.AirportProcedureCount:N0} airport arrival(s)"
				+ (arrivals.SkippedForMissingPointsCount > 0 ? $", {arrivals.SkippedForMissingPointsCount} skipped" : string.Empty));
		}

		if (result.Navaids is { } navaids)
		{
			parts.Add($"{navaids.NavaidCount:N0} NAVAID(s)");
		}

		if (result.ArtccBoundaries is { } b)
		{
			parts.Add($"{b.RingCount:N0} ARTCC boundary line(s)");
		}

		if (result.Fixes is { } fixes)
		{
			parts.Add($"{fixes.FixCount:N0} fix(es)");
		}

		if (result.WxStations is { } wxStations)
		{
			parts.Add($"{wxStations.StationCount:N0} weather station(s)");
		}

		if (result.Procedures is { } procedures)
		{
			if (procedures.FilesWritten.Count > 0)
			{
				parts.Add($"{procedures.AirportCount:N0} airport(s) with procedure changes"
					+ (procedures.NewCount + procedures.ChangedCount + procedures.DeletedCount > 0
						? $", {procedures.NewCount + procedures.ChangedCount + procedures.DeletedCount} change(s)"
						: string.Empty));
			}

			if (procedures.AliasFilePath is not null)
			{
				parts.Add($"{procedures.AliasCommandCount:N0} FAA Chart Recall command(s)");
			}
		}

		if (result.Telephony is { AliasFilePath: not null } telephony)
		{
			parts.Add($"{telephony.AliasCommandCount:N0} telephony command(s)");
		}

		if (result.CombinedAlias is { FilePath: not null } combinedAlias)
		{
			parts.Add($"{Path.GetFileName(combinedAlias.FilePath)} ({combinedAlias.CustomCommandCount + combinedAlias.FeBuddyCommandCount:N0} command(s))");
		}

		if (result.DuplicateAliasReport is { Duplicates.Count: > 0 } duplicateAliasReport)
		{
			parts.Add($"{duplicateAliasReport.Duplicates.Count:N0} duplicate alias command(s)");
		}

		return parts.Count == 0
			? $"Cycle {cycleId}: nothing to produce."
			: $"Cycle {cycleId}: {string.Join(", ", parts)}.";
	}

	/// <summary>Offers to save every dirty tab that takes part in one prompt, as the settings-save contract requires.</summary>
	/// <returns><see langword="true"/> when nothing is left unsaved.</returns>
	private bool TrySaveDirtyTabs()
	{
		ServiceTabViewModel[] dirty = [.. Tabs.Where(t => t.IsAvailable && t.IsDirty)];

		if (dirty.Length == 0)
		{
			return true;
		}

		bool proceed = ConfirmWindow.Show(
			Application.Current?.MainWindow,
			"Unsaved settings",
			$"{string.Join(", ", dirty.Select(t => t.Title))} {(dirty.Length == 1 ? "has" : "have")} unsaved changes. "
			+ "The newly input data will be saved before execution.",
			confirmText: "Save & Continue");

		if (!proceed)
		{
			return false;
		}

		foreach (ServiceTabViewModel tab in dirty)
		{
			if (!tab.Save())
			{
				SelectedTab = tab;   // Save has already explained why; show them the tab it failed on.
				return false;
			}
		}

		return true;
	}

	/// <summary>Blocks the run while any tab that takes part still has a validation failure, and shows the first such tab.</summary>
	/// <returns><see langword="true"/> when every tab is valid.</returns>
	private bool EnsureNoInvalidTabs()
	{
		ServiceTabViewModel? invalid = Tabs.FirstOrDefault(t => t.IsAvailable && t.Status == ServiceTabStatus.Invalid);

		if (invalid is null)
		{
			return true;
		}

		Toast.Warn("Cannot run", $"{invalid.Title} has settings that need fixing.");
		SelectedTab = invalid;
		return false;
	}
}
