using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.Views;
using FeBuddy.Wpf.Views.Models;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>AIRAC Services</b> screen: a General tab (cycle, facility, and which sub-services to
/// produce), one tab per selected sub-service, a Preview Settings tab that summarises the lot and
/// runs the service, and a Review tab for the run's outcome.
/// </summary>
/// <remarks>
/// <para>
/// The tab machinery lives in <see cref="TabbedServiceViewModel"/> and is deliberately generic -
/// AIRAC Service is simply the first first-tier service built on it. Everything specific to AIRAC
/// lives here: readiness gating on the cycle cache, loading the selected cycle's parsed data, and
/// the run itself.
/// </para>
/// <para>
/// A sub-service tab is created the first time the user selects it and then kept for the session,
/// so unticking and re-ticking does not throw away what they typed. Its saved settings are never
/// touched by unticking - only the tab goes away.
/// </para>
/// </remarks>
public sealed class AiracServiceViewModel : TabbedServiceViewModel
{
	private readonly Dispatcher _dispatcher;
	private readonly AiracGeneralTabViewModel _general;
	private readonly ServicePreviewTabViewModel _preview;
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
		_general.SubServiceSelectionChanged += (_, _) => SyncSubServiceTabs();
		_general.CycleChanged += (_, _) => _ = LoadCycleDataAsync();

		_preview = new ServicePreviewTabViewModel("Preview Settings", "Run AIRAC Service", RunCommand, () => Tabs);

		AiracCycleDataCache.Instance.StateChanged += (_, _) => _dispatcher.BeginInvoke(RefreshReadiness);
		AppEnvironment.Changed += (_, _) => _dispatcher.BeginInvoke(RefreshReadiness);

		SyncSubServiceTabs();
		RefreshReadiness();
		_ = LoadCycleDataAsync();
	}

	/// <summary>Runs the AIRAC Service for every selected sub-service. Hosted on the Preview Settings tab.</summary>
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

	/// <summary>The Airways tab while it is open, otherwise <see langword="null"/>.</summary>
	private AirwaysViewModel? AirwaysTab => TabFor<AirwaysViewModel>(AiracSubServices.AirwaysKey);

	/// <summary>The Airports tab while it is open, otherwise <see langword="null"/>.</summary>
	private AirportsViewModel? AirportsTab => TabFor<AirportsViewModel>(AiracSubServices.AirportsKey);

	/// <summary>The Departures tab while it is open, otherwise <see langword="null"/>.</summary>
	private DeparturesViewModel? DeparturesTab => TabFor<DeparturesViewModel>(AiracSubServices.DeparturesKey);

	/// <summary>The Arrivals tab while it is open, otherwise <see langword="null"/>.</summary>
	private ArrivalsViewModel? ArrivalsTab => TabFor<ArrivalsViewModel>(AiracSubServices.ArrivalsKey);

	/// <summary>The NAVAIDs tab while it is open, otherwise <see langword="null"/>.</summary>
	private NavaidsViewModel? NavaidsTab => TabFor<NavaidsViewModel>(AiracSubServices.NavaidsKey);

	/// <summary>The open tabs that take part in a run.</summary>
	private IReadOnlyList<ISubServiceRunTarget> RunTargets =>
		[.. Tabs.OfType<ISubServiceRunTarget>()];

	/// <summary>Returns an open sub-service tab of the expected type, or <see langword="null"/>.</summary>
	/// <typeparam name="T">The tab's view-model type.</typeparam>
	/// <param name="key">The sub-service key from <see cref="AiracSubServices"/>.</param>
	/// <returns>The tab, when it is both selected and built.</returns>
	private T? TabFor<T>(string key) where T : class =>
		IsSelected(key) && _tabsByKey.TryGetValue(key, out ServiceTabViewModel? tab)
			? tab as T
			: null;

	private string WaitingMessage => Readiness switch
	{
		AiracCycleReadiness.Unavailable => "The current AIRAC cycle failed to download or parse. The AIRAC Service is unavailable — see the Dashboard activity log.",
		_ => "Waiting for AIRAC data to finish downloading and parsing. This service will be available in a moment.",
	};

	private bool IsSelected(string key) => _general.SelectedSubServices.Any(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Opens a tab for every selected sub-service and closes the rest, building each tab the first
	/// time it is needed and reusing it afterwards.
	/// </summary>
	private void SyncSubServiceTabs()
	{
		List<ServiceTabViewModel> open = [];

		foreach (SubServiceSelection selection in _general.SelectedSubServices)
		{
			if (!_tabsByKey.TryGetValue(selection.Key, out ServiceTabViewModel? tab))
			{
				tab = selection.Descriptor.CreateTab();
				_tabsByKey[selection.Key] = tab;

				if (tab is ISubServiceRunTarget target)
				{
					target.SetReadiness(IsReady);

					if (_parsedForSelectedCycle is { } data)
					{
						target.LoadCycleDependentLists(data);
					}
				}
			}

			open.Add(tab);
		}

		RebuildTabs(open);
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
			});
		}
		catch (Exception ex)
		{
			AppLog.Warning("AiracService", $"Could not load parsed data for cycle {cycleId}: {ex.Message}");
		}
	}

	/// <summary>
	/// Saves anything unsaved (with the user's blessing), refuses to start while a tab is invalid,
	/// asks what to do with an earlier run's files, then runs every selected sub-service that has
	/// a backend.
	/// </summary>
	private async Task RunAsync()
	{
		if (!TrySaveDirtyTabs() || !EnsureNoInvalidTabs())
		{
			return;
		}

		ServiceTabViewModel[] runnable = [.. Tabs.Where(t => t.IsRunnable && !ReferenceEquals(t, GeneralTab) && !ReferenceEquals(t, PreviewTab))];

		if (runnable.Length == 0)
		{
			Toast.Warn("Nothing to run", "Select a sub-service with settings on the General tab first.");
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
			Airways = AirwaysTab?.BuildSettingsBlock(),
			Airports = AirportsTab?.BuildSettingsBlock(),
			Departures = DeparturesTab?.BuildSettingsBlock(),
			Arrivals = ArrivalsTab?.BuildSettingsBlock(),
			Navaids = NavaidsTab?.BuildSettingsBlock(),
		};

		if (AiracService.HasExistingOutput(settings))
		{
			if (AskAboutExistingOutput(settings) is not { } existingOutput)
			{
				return;
			}

			settings = settings with { ExistingOutput = existingOutput };
		}

		IReadOnlyList<ISubServiceRunTarget> targets = RunTargets;
		string[] runningTabTitles = [.. Tabs
			.Where(t => t is ISubServiceRunTarget)
			.Select(t => t.Title)];

		IsRunning = true;

		_runReview.BeginRun(runningTabTitles);
		_runReviewShown = true;
		ShowPostRunTab();

		try
		{
			var progress = new Progress<AiracServiceProgress>(p =>
				_dispatcher.BeginInvoke(() =>
				{
					// The library reports 100 when a sub-service is done with its work.
					_runReview.ReportStep(p.SubService, p.Message, p.PercentComplete >= 100);
				}));

			AiracServiceResult result = await AiracService.RunAsync(settings, progress);

			string summary = SummarizeRun(cycle.AiracCycleId, result);
			string[] files = CollectFilesWritten(result);
			SubServiceRunResult[] subServiceResults = [.. targets
				.Select(t => t.DescribeRunResult(result))
				.OfType<SubServiceRunResult>()];

			_runReview.CompleteRun(
				result,
				summary,
				subServiceResults,
				files,
				ExistingFolder(result.OutputDirectory));

			Toast.Success("AIRAC Service complete", summary);
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
		}
	}

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
			$"Looks like AIRAC cycle {cycleId} has already been run at one point: {settings.CycleOutputDirectory} "
			+ "already has files in it. Select what you would like to happen:"
			+ Environment.NewLine + Environment.NewLine
			+ "Overwrite files - this run's files replace the old ones; any other old file is left as it is."
			+ Environment.NewLine
			+ $"Delete all files - everything in {AiracOutputPaths.CycleFolderName(cycleId)} is permanently deleted first, "
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

		return parts.Count == 0
			? $"Cycle {cycleId}: nothing to produce."
			: $"Cycle {cycleId}: {string.Join(", ", parts)}.";
	}

	/// <summary>Offers to save every dirty tab in one prompt, as the settings-save contract requires.</summary>
	/// <returns><see langword="true"/> when nothing is left unsaved.</returns>
	private bool TrySaveDirtyTabs()
	{
		ServiceTabViewModel[] dirty = [.. Tabs.Where(t => t.IsDirty)];

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

	/// <summary>Blocks the run while any tab still has a validation failure, and shows the first one.</summary>
	/// <returns><see langword="true"/> when every tab is valid.</returns>
	private bool EnsureNoInvalidTabs()
	{
		ServiceTabViewModel? invalid = Tabs.FirstOrDefault(t => t.Status == ServiceTabStatus.Invalid);

		if (invalid is null)
		{
			return true;
		}

		Toast.Warn("Cannot run", $"{invalid.Title} has settings that need fixing.");
		SelectedTab = invalid;
		return false;
	}
}
