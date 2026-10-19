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
using FeBuddy.Core.Application.Airac.Procedures;
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
/// Add Custom Aliases isn't in the table: its tab is greyed out while no included sub-service makes
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
	private string? _loadingCycleId;

	/// <summary>
	/// Each cycle's d-TPP Metafile, read and indexed once, so a refresh of the Procedure Charts tab
	/// reuses it rather than parsing up to 16 MB again. One that faults, or finds no metafile, is
	/// dropped, so the next refresh asks again. Used on the UI thread only.
	/// </summary>
	private readonly Dictionary<string, Task<DtppIndex>> _dtppIndexes = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>Builds the screen, restores the saved tabs, and starts loading the selected cycle.</summary>
	public AiracServiceViewModel()
	{
		_dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

		// Not until the picked cycle's lists are in: a run built from tabs still waiting for them would
		// leave out what they add (per-ARTCC files and their CRC defaults, for one).
		RunCommand = new RelayCommand(async () => await RunAsync(), () => !IsRunning && IsSelectedCycleLoaded);

		_general = new AiracGeneralTabViewModel();
		_general.SubServicesChanged += (_, _) => SyncSubServiceTabs();
		_general.CycleChanged += (_, _) =>
		{
			// The tabs drop the old cycle's data for the one picked, so a cycle loaded earlier is handed
			// over again when it is picked back: the cache and the option lists return what they have.
			if (_parsedCycleId != SelectedCycleId)
			{
				_parsedCycleId = null;
				_parsedForSelectedCycle = null;
			}

			_fileNames.RefreshFiles();
			PushSelectedCycle();
			PushTabReadiness();
			_ = LoadCycleDataAsync(picked: true);
		};
		_general.CycleRetryRequested += (_, _) => _ = LoadCycleDataAsync(picked: true);

		// What the run does with duplicate alias commands is the run's, not any tab's, so it sits on Preview Settings.
		_duplicateAliases = new DuplicateAliasesCardViewModel(
			() => Tabs.OfType<GeojsonSubServiceViewModel>().Any(t => t.IsAvailable && t.GenerateAliasFile));
		_preview = new ServicePreviewTabViewModel("Preview Settings", "Run AIRAC Service", RunCommand, () => Tabs.Where(t => t.IsAvailable))
		{
			RunOptions = _duplicateAliases,
		};

		_fileNames.AttachToService(
			FilesTheRunWrites,
			() => AiracOutputPaths.CycleFolderName(AppEnvironment.GetAiracCycle(_general.SelectedCyclePosition).AiracCycleId));

		AiracCycleDataCache.Instance.StateChanged += (_, _) => _dispatcher.BeginInvoke(RefreshReadiness);

		// The Procedure Charts tab says whether deleted procedures can link to the previous cycle's
		// charts, which can still be on their way when the screen opens.
		AiracCycleDataCache.Instance.StateChanged += (_, entry) =>
		{
			if (entry.State is CycleDataState.Downloaded or CycleDataState.Failed or CycleDataState.NotYetPublished)
			{
				_dispatcher.BeginInvoke(() =>
				{
					if (entry.Cycle.AiracCycleId == PreviousOfSelectedCycleId)
					{
						RefreshProceduresData(previousChanged: true);
					}
				});
			}
		};
		AppEnvironment.Changed += (_, _) => _dispatcher.BeginInvoke(RefreshReadiness);

		// The File Names tab lists every file the other tabs write, and a new one needs a name. Re-read
		// it whenever the user moves between tabs, so its list and its dot in the rail follow edits
		// made elsewhere.
		PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(SelectedTab))
			{
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
	private static AiracCycleReadiness Readiness => AiracCycleDataCache.Instance.ComputeReadiness();

	/// <summary>Whether the service is usable (Ready or Degraded).</summary>
	private bool IsReady => Readiness is AiracCycleReadiness.Ready or AiracCycleReadiness.Degraded;

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

	/// <summary>The Add Custom Aliases tab while it takes part (an alias file is being made), otherwise <see langword="null"/>.</summary>
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
		AiracCycleReadiness.Unavailable => "The current AIRAC cycle couldn't be downloaded or read, so the AIRAC Service is unavailable. " +
			"Check the internet connection (a proxy or firewall can block the FAA's site) and that the PC's clock is right, then restart FE-Buddy. " +
			"The Dashboard activity log says what went wrong.",
		AiracCycleReadiness.NotLoaded => $"Loading AIRAC {AppEnvironment.GetAiracCycle(AiracCyclePosition.Current).AiracCycleId}… This dev launch didn't load it at launch.",
		_ => "Waiting for the current AIRAC cycle to finish downloading and parsing. This service will be available in a moment.",
	};

	/// <summary>
	/// In a dev launch that left the current cycle unloaded (<see cref="AiracCycleReadiness.NotLoaded"/>),
	/// opening this screen loads it: the service needs it. A normal launch never gets here - it
	/// loads the current cycle at launch.
	/// </summary>
	private void LoadCurrentCycleIfLeftUnloaded()
	{
		if (Readiness != AiracCycleReadiness.NotLoaded)
		{
			return;
		}

		string cycleId = AppEnvironment.GetAiracCycle(AiracCyclePosition.Current).AiracCycleId;
		_ = LoadOnDemandAsync(cycleId);
	}

	private static async Task LoadOnDemandAsync(string cycleId)
	{
		try
		{
			await AiracCycleDataCache.Instance.GetAsync(cycleId).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			AppLog.Warning("AiracService", $"Could not load AIRAC {cycleId}: {ex.Message}");
		}
	}

	/// <summary>
	/// Builds every sub-service's tab, once: each takes its outputs from its row on the General tab,
	/// and tells the screen when the files it writes change, which can bring Add Custom Aliases in.
	/// </summary>
	private void BuildSubServiceTabs()
	{
		foreach (SubServiceDescriptor descriptor in AiracSubServices.All)
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
				target.SetReadiness(IsSelectedCycleLoaded);
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

		foreach (SubServiceDescriptor descriptor in AiracSubServices.All)
		{
			ServiceTabViewModel tab = _tabsByKey[descriptor.Key];

			if (_general.RowFor(descriptor.Key) is { } row)
			{
				tab.SetAvailability(row.IsIncluded,
					$"{descriptor.Help?.Summary}\nIt's left out of the run. To include it, tick {descriptor.DisplayName} under Include in the General tab.");
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
		_fileNames.RefreshFiles();
	}

	/// <summary>
	/// Brings Add Custom Aliases in while an included sub-service makes an alias file, and greys it
	/// out otherwise: there is no combined file to add custom aliases to.
	/// </summary>
	private void RefreshConcatenateAliasesAvailability()
	{
		if (!_tabsByKey.TryGetValue(AiracSubServices.ConcatenateAliasesKey, out ServiceTabViewModel? concatenate))
		{
			return;
		}

		bool anyAliasFile = _tabsByKey.Values.Where(t => t.IsAvailable).OfType<GeojsonSubServiceViewModel>().Any(tab => tab.GenerateAliasFile);
		bool wasAvailable = concatenate.IsAvailable;

		concatenate.SetAvailability(anyAliasFile,
			$"{AiracSubServices.All.Single(d => d.Key == AiracSubServices.ConcatenateAliasesKey).Help?.Summary}\n" +
			"No alias file is being made. To make one, tick a sub-service and its Alias box in the General tab.");

		// Left on a tab that just went grey, go back to the start.
		if (wasAvailable && !anyAliasFile && ReferenceEquals(SelectedTab, concatenate))
		{
			SelectedTab = Tabs.FirstOrDefault();
		}
	}

	/// <summary>
	/// Every file the included sub-services' settings write right now, for the File Names tab: each
	/// tab's own files, then the two the run itself writes whenever an alias file is written:
	/// <c>Duplicate_Alias_Commands.txt</c> and <c>Combined_Alias.txt</c>.
	/// </summary>
	/// <returns>The files.</returns>
	private IEnumerable<OutputFileEntry> FilesTheRunWrites()
	{
		GeojsonSubServiceViewModel[] tabs = [.. Tabs.Where(t => t.IsAvailable).OfType<GeojsonSubServiceViewModel>()];

		foreach (OutputFileEntry file in tabs.SelectMany(tab => tab.OutputFileEntries()))
		{
			yield return file;
		}

		if (tabs.Any(tab => tab.GenerateAliasFile))
		{
			yield return OutputFileEntry.Renamable(AiracOutputPaths.DuplicateAliasReportFileName, string.Empty, "AIRAC Service");
			yield return OutputFileEntry.Renamable(AiracOutputPaths.CombinedAliasFileName, AiracOutputPaths.AliasFolder, "AIRAC Service");
		}
	}

	/// <summary>
	/// Tells every sub-service tab which cycle the General tab has picked, for text that names it:
	/// when the screen opens, when the pick changes, and when the date moves the cycles along.
	/// </summary>
	private void PushSelectedCycle()
	{
		AiracCycleInfo cycle = AppEnvironment.GetAiracCycle(_general.SelectedCyclePosition);

		foreach (GeojsonSubServiceViewModel tab in _tabsByKey.Values.OfType<GeojsonSubServiceViewModel>())
		{
			tab.SetSelectedCycle(cycle);
		}
	}

	private void RefreshReadiness()
	{
		LoadCurrentCycleIfLeftUnloaded();
		PushSelectedCycle();

		_general.SetReadiness(IsReady, WaitingMessage);
		PushTabReadiness();

		CommandManager.InvalidateRequerySuggested();

		_ = LoadCycleDataAsync();
	}

	/// <summary>The cycle picked on the General tab.</summary>
	private string SelectedCycleId => AppEnvironment.GetAiracCycle(_general.SelectedCyclePosition).AiracCycleId;

	/// <summary>The cycle whose data is being loaded for the tabs, or <see langword="null"/>. For tests.</summary>
	internal string? LoadingCycleId => _loadingCycleId;

	/// <summary>
	/// Whether the tabs have the selected cycle's data. The service opens once the current cycle is
	/// ready, so the next or previous cycle can still be loading when it's picked.
	/// </summary>
	private bool IsSelectedCycleLoaded => IsReady && _parsedCycleId == SelectedCycleId && _parsedForSelectedCycle is not null;

	/// <summary>
	/// Tells every sub-service tab whether its lists can use the selected cycle yet, and why not when
	/// the cycle won't load: not published, or failed.
	/// </summary>
	private void PushTabReadiness()
	{
		string? unavailable = IsReady && !IsSelectedCycleLoaded && _loadingCycleId != SelectedCycleId
			? UnavailableReason(SelectedCycleId)
			: null;

		foreach (ISubServiceRunTarget target in RunTargets)
		{
			target.SetReadiness(IsSelectedCycleLoaded, unavailable);
		}

		CommandManager.InvalidateRequerySuggested();
	}

	/// <summary>Why a cycle's data won't load, or <see langword="null"/> while it is loading or loaded.</summary>
	/// <param name="cycleId">The cycle.</param>
	/// <returns>What the tabs say instead of "Loading".</returns>
	private static string? UnavailableReason(string cycleId) => AiracCycleDataCache.Instance.GetEntry(cycleId)?.State switch
	{
		CycleDataState.NotYetPublished => $"AIRAC {cycleId} isn't published by the FAA yet. Pick another cycle on the General tab.",
		CycleDataState.Failed => $"AIRAC {cycleId} couldn't be downloaded or read. Pick it again on the General tab to try again, " +
			"or pick another cycle. The Dashboard activity log says what went wrong.",
		_ => null,
	};

	/// <summary>
	/// Hands the selected cycle's parsed data to every tab, waiting for it while it is still loading
	/// (the tabs say <c>Loading AIRAC 2611…</c> meanwhile).
	/// </summary>
	/// <param name="picked">Whether the user just picked the cycle: a failed one is then tried again.</param>
	/// <remarks>
	/// A cycle the FAA hasn't published, or one that failed, isn't asked for again on every cache
	/// change - asking for a failed cycle downloads it again, and each failure is another change. The
	/// tabs say why instead (<see cref="PushTabReadiness"/>).
	/// </remarks>
	private async Task LoadCycleDataAsync(bool picked = false)
	{
		if (!IsReady)
		{
			return;
		}

		string cycleId = SelectedCycleId;

		if ((_parsedCycleId == cycleId && _parsedForSelectedCycle is not null) || _loadingCycleId == cycleId)
		{
			return;
		}

		CycleDataState? state = AiracCycleDataCache.Instance.GetEntry(cycleId)?.State;

		if (state == CycleDataState.NotYetPublished || (state == CycleDataState.Failed && !picked))
		{
			PushTabReadiness();
			return;
		}

		_loadingCycleId = cycleId;

		try
		{
			NasrCsvDataCollection data = await AiracCycleDataCache.Instance.GetAsync(cycleId).ConfigureAwait(false);

			// Every tab's option lists, worked out here, off the UI thread, once per cycle: the UI
			// thread only fills the toggles from them.
			AiracCycleOptionLists lists = await Task.Run(() => AiracCycleOptionListBuilder.For(data)).ConfigureAwait(false);

			await _dispatcher.BeginInvoke(() =>
			{
				// A cycle picked since may be loading in its turn: its mark stays.
				if (_loadingCycleId == cycleId)
				{
					_loadingCycleId = null;
				}

				// Another cycle may have been picked while this one loaded: its own load hands it over.
				if (SelectedCycleId != cycleId)
				{
					return;
				}

				_parsedForSelectedCycle = data;
				_parsedCycleId = cycleId;

				foreach (ISubServiceRunTarget target in RunTargets)
				{
					target.LoadCycleDependentLists(lists);
				}

				PushTabReadiness();
				RefreshProceduresData();
			});
		}
		catch (Exception ex)
		{
			_ = _dispatcher.BeginInvoke(() =>
			{
				if (_loadingCycleId == cycleId)
				{
					_loadingCycleId = null;
				}

				PushTabReadiness();
			});
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
	/// metafile is loaded off the UI thread - it is up to 16 MB and takes about 0.3 s to parse - once
	/// per cycle (<see cref="DtppIndexFor"/>), and handed over only once the selected cycle is loaded:
	/// when the tab doesn't already show its metafile, or the previous cycle's state changed.
	/// </summary>
	/// <param name="previousChanged">Whether the previous cycle's state changed, which the tab reports on.</param>
	private void RefreshProceduresData(bool previousChanged = false)
	{
		if (ProceduresTab is not { } tab || !IsSelectedCycleLoaded)
		{
			return;
		}

		string cycleId = SelectedCycleId;

		if (!previousChanged && tab.DtppCycleId == cycleId)
		{
			return;
		}

		_ = LoadProceduresDataAsync(tab, cycleId, PreviousOfSelectedCycleId, DtppIndexFor(cycleId));
	}

	/// <summary>The cycle before the one picked on the General tab, whose metafile the changes report links to.</summary>
	private string PreviousOfSelectedCycleId =>
		AiracCycleResolver.GetCycle(AiracCyclePosition.Previous, AppEnvironment.GetAiracCycle(_general.SelectedCyclePosition).EffectiveDateUtc).AiracCycleId;

	/// <summary>
	/// A cycle's d-TPP Metafile, indexed for the Procedures tab: read and indexed off the UI thread the
	/// first time it is asked for, and the same task after that (<see cref="_dtppIndexes"/>).
	/// </summary>
	/// <param name="cycleId">The cycle.</param>
	/// <returns>The index (<see langword="null"/> with no metafile) and when the metafile was downloaded.</returns>
	private Task<DtppIndex> DtppIndexFor(string cycleId)
	{
		if (!_dtppIndexes.TryGetValue(cycleId, out Task<DtppIndex>? index))
		{
			index = Task.Run(async () =>
			{
				DtppMetafileDataCollection? dtpp = await AiracCycleDataCache.Instance.GetDtppAsync(cycleId).ConfigureAwait(false);

				DateTime? downloadedLocal = AiracCycleDataCache.Instance.FindDtppFile(cycleId) is { } file
					? File.GetLastWriteTime(file)
					: null;

				return new DtppIndex(dtpp is null ? null : ProcedureNameIndex.For(dtpp), downloadedLocal);
			});

			_dtppIndexes[cycleId] = index;
		}

		return index;
	}

	private async Task LoadProceduresDataAsync(ProceduresViewModel tab, string cycleId, string previousCycleId, Task<DtppIndex> indexTask)
	{
		try
		{
			DtppIndex dtpp = await indexTask.ConfigureAwait(false);

			bool previousAvailable = AiracCycleDataCache.Instance.FindDtppFile(previousCycleId) is not null;

			// Not on disk yet, but on its way: the tab says so rather than that it's missing.
			bool previousLoading = !previousAvailable
				&& AiracCycleDataCache.Instance.GetEntry(previousCycleId) is { CycleDirectory: null } previous
				&& !AiracCycleDataCache.IsSettled(previous);

			await _dispatcher.BeginInvoke(() =>
			{
				// No metafile yet: ask again next time rather than keep the answer.
				if (dtpp.Index is null)
				{
					ForgetDtppIndex(cycleId, indexTask);
				}

				// The user may have picked another cycle while this one was loading; its own load
				// reports in separately, so a stale result must not overwrite it.
				if (SelectedCycleId == cycleId)
				{
					tab.SetDtppData(cycleId, dtpp.Index, previousCycleId, previousAvailable, dtpp.Downloaded, previousLoading);
				}
			});
		}
		catch (Exception ex)
		{
			_ = _dispatcher.BeginInvoke(() => ForgetDtppIndex(cycleId, indexTask));
			AppLog.Warning("AiracService", $"Could not load the d-TPP Metafile for cycle {cycleId}: {ex.Message}");
		}
	}

	/// <summary>Drops a cycle's indexed metafile, unless a newer load has already taken its place.</summary>
	private void ForgetDtppIndex(string cycleId, Task<DtppIndex> indexTask)
	{
		if (_dtppIndexes.TryGetValue(cycleId, out Task<DtppIndex>? kept) && kept == indexTask)
		{
			_dtppIndexes.Remove(cycleId);
		}
	}

	/// <summary>A cycle's d-TPP Metafile, indexed for the Procedures tab, and when it was downloaded.</summary>
	/// <param name="Index">The lookups, or <see langword="null"/> when the cycle has no metafile.</param>
	/// <param name="Downloaded">When the metafile was downloaded, in local time.</param>
	private readonly record struct DtppIndex(ProcedureNameIndex? Index, DateTime? Downloaded);

	/// <summary>
	/// Saves anything unsaved (with the user's blessing), refuses to start while a tab is invalid,
	/// asks what to do with an earlier run's files, then runs every included sub-service that has
	/// a backend.
	/// </summary>
	private async Task RunAsync()
	{
		// Set once the run has begun, so a failure before it (saving the tabs, looking for an earlier
		// run's files) says the run didn't start rather than that it failed.
		string? cycleOutputDirectory = null;
		bool started = false;

		try
		{
			// The File Names tab's validity depends on which files the other tabs write.
			_fileNames.RefreshFiles();

			// The custom alias files on this PC are looked at again, off the UI thread, so one the user
			// has fixed since doesn't stop the run. Running meanwhile, so the Run button can't start a second run.
			if (ConcatenateAliasesTab is { } customAliases)
			{
				IsRunning = true;
				await customAliases.CheckLocalFilesAsync();
				IsRunning = false;
			}

			if (!InstalledUpdate.AllowsRun() || !TrySaveDirtyTabs() || !EnsureNoInvalidTabs())
			{
				return;
			}

			IReadOnlyList<ISubServiceRunTarget> targets = IncludedRunTargets;

			if (targets.Count == 0)
			{
				Toast.Warn("Nothing to run", "Include a sub-service in the General tab first.");
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
				// Settings ▸ Facility: listed first in the duplicate alias report.
				PrimaryFacility = UserConfigFile.GetValue(UserConfigKeys.Facility)?.Trim() is { Length: > 0 } facility
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

			// Running from here, so the Run button can't start a second run while the folder is looked
			// at off the UI thread (it can be on a slow share) or the user answers about it.
			IsRunning = true;

			if (await Task.Run(() => AiracService.HasExistingOutput(settings)))
			{
				if (AskAboutExistingOutput(settings) is not { } existingOutput)
				{
					return;
				}

				settings = settings with { ExistingOutput = existingOutput };
			}

			string[] runningTabTitles = [.. Tabs
				.Where(t => t.IsAvailable && t is ISubServiceRunTarget)
				.Select(t => t.Title)];

			cycleOutputDirectory = settings.CycleOutputDirectory;

			_runReview.BeginRun(runningTabTitles);
			_runReviewShown = true;
			ShowPostRunTab();
			started = true;

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

			string summary = AiracRunSummary.Describe(cycle.AiracCycleId, result);
			SubServiceRunResult[] subServiceResults = [.. targets
				.Select(t => t.DescribeRunResult(result))
				.OfType<SubServiceRunResult>()];

			// A run stopped at the duplicates names no alias file: Core deleted them. The folder is looked
			// at off the UI thread.
			string? outputFolder = await Task.Run(() => ExistingFolder(result.OutputDirectory));

			_runReview.CompleteRun(
				result,
				summary,
				subServiceResults,
				result.FilesWritten,
				outputFolder);

			if (result.StoppedAtDuplicateReview)
			{
				Toast.Warn("AIRAC Service stopped", "You stopped it at the duplicate alias commands, so no alias file was saved.");
			}
			else
			{
				Toast.Success("AIRAC Service complete", summary);
			}
		}
		catch (Exception ex) when (started && cycleOutputDirectory is { } outputDirectory)
		{
			// A run that fails part way may already have written files; offer the folder if so.
			_runReview.FailRun(ex.Message, outputDirectory: await Task.Run(() => ExistingFolder(outputDirectory)));
			Toast.Error("AIRAC Service failed", ex.Message);
		}
		catch (Exception ex)
		{
			AppLog.Error("AiracService", $"The run could not start: {ex.Message}");
			Toast.Error("AIRAC Service didn't start", ex.Message);
		}
		finally
		{
			IsRunning = false;

			// The run replaced the kept Wx Stations and telephony copies when it could download them.
			if (started)
			{
				RefreshDownloadedDataStatus();
			}
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
