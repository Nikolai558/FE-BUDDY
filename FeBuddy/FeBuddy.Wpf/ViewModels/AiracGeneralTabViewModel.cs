using System.Collections.ObjectModel;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The AIRAC Service <b>General</b> tab: the cycle menu, and the table of sub-services - which are
/// in the run, and which of their outputs each makes.
/// </summary>
/// <remarks>
/// <para>
/// It is a settings tab like any other - it owns the <c>Services.AiracService.General</c> subtree, is
/// dirty until saved, and has the same one-step undo. Its node sits beside the other tabs' nodes, not
/// above them, so its save and undo can't reach their settings. Ticking a sub-service brings its tab back from grey
/// straight away (the user asked for it, so they should see it), but like every other setting on
/// this tab it is only kept once saved.
/// </para>
/// <para>
/// The table owns each sub-service's outputs; its tab reads them (<see cref="ISubServiceOutputs"/>).
/// They are saved under this tab's <c>Outputs</c> node, never inside a sub-service's own node, so
/// saving or undoing a sub-service tab can't change them. Nothing saved means everything included
/// and every output on.
/// </para>
/// </remarks>
public sealed class AiracGeneralTabViewModel : SubServiceSettingsViewModel
{
	private const string Node = $"{UserConfigAreas.AiracNode}.{AiracTabs.General}";
	private const string SelectedSubServicesKey = "SelectedSubServices";
	private const string OutputsNode = "Outputs";
	private const string CyclePositionKey = "CyclePosition";

	private bool _loading;
	private bool _cycleFromSave;
	private bool _isReady;
	private string _waitingMessage = string.Empty;
	private AiracCyclePosition _selectedCyclePosition = AiracCyclePosition.Current;
	private AiracCyclePosition _cyclePositionBeforeReload = AiracCyclePosition.Current;

	/// <summary>Builds the tab and restores the saved cycle and sub-services.</summary>
	public AiracGeneralTabViewModel()
	{
		CycleOptions =
		[
			new(AiracCyclePosition.Previous, p => SelectedCyclePosition = p, OnCyclePicked),
			new(AiracCyclePosition.Current, p => SelectedCyclePosition = p, OnCyclePicked),
			new(AiracCyclePosition.Next, p => SelectedCyclePosition = p, OnCyclePicked),
		];

		SubServices = new ObservableCollection<SubServiceRow>(
			AiracSubServices.All
				.Where(d => d.Outputs != SubServiceOutputKinds.None)
				.Select(d => new SubServiceRow(d, OnSubServicesChanged)));

		LoadFromConfig();
		RefreshCycleOptions();
	}

	/// <summary>
	/// Raised when the user includes or leaves out a sub-service or turns one of its outputs on or
	/// off, so the host can grey out or bring back tabs and follow the files the run will write.
	/// </summary>
	public event EventHandler? SubServicesChanged;

	/// <summary>Raised when the user picks a different cycle, so the host can load that cycle's parsed data.</summary>
	public event EventHandler? CycleChanged;

	/// <summary>
	/// Raised when the user picks the cycle that is already selected and failed, so the host can try
	/// it again: a radio button already checked changes nothing, so <see cref="CycleChanged"/> doesn't fire.
	/// </summary>
	public event EventHandler? CycleRetryRequested;

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "General";

	/// <inheritdoc />
	public override string ConfigPageName => "AIRAC Services General";

	/// <summary>The three selectable cycles with their live cache state.</summary>
	public ObservableCollection<CycleOption> CycleOptions { get; }

	/// <summary>The table: every sub-service that makes output of its own, included or not, in rail order.</summary>
	public ObservableCollection<SubServiceRow> SubServices { get; }

	/// <summary>The included sub-services, in rail order.</summary>
	public IEnumerable<SubServiceRow> IncludedSubServices => SubServices.Where(s => s.IsIncluded);

	/// <summary>A sub-service's row, or <see langword="null"/> for one that isn't in the table (Add Custom Aliases).</summary>
	/// <param name="key">The sub-service key from <see cref="AiracSubServices"/>.</param>
	/// <returns>The row.</returns>
	public SubServiceRow? RowFor(string key) =>
		SubServices.FirstOrDefault(row => string.Equals(row.Key, key, StringComparison.OrdinalIgnoreCase));

	/// <summary>Which cycle (relative to today) the run uses.</summary>
	public AiracCyclePosition SelectedCyclePosition
	{
		get => _selectedCyclePosition;
		set
		{
			if (!SetProperty(ref _selectedCyclePosition, value))
			{
				return;
			}

			foreach (CycleOption option in CycleOptions)
			{
				option.SyncSelected(value);
			}

			OnPropertyChanged(nameof(SelectedCycleLabel));

			if (_loading)
			{
				return;
			}

			_cycleFromSave = false;
			MarkDirty();
			CycleChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>e.g. <c>Cycle 2610 · effective 01 Oct 2026</c> for the current selection.</summary>
	public string SelectedCycleLabel
	{
		get
		{
			AiracCycleInfo info = AppEnvironment.GetAiracCycle(SelectedCyclePosition);
			return $"Cycle {info.AiracCycleId}  ·  effective {info.EffectiveDateUtc:dd MMM yyyy}";
		}
	}

	/// <summary>Whether the AIRAC data is ready enough to use the service.</summary>
	public bool IsReady
	{
		get => _isReady;
		private set => SetProperty(ref _isReady, value);
	}

	/// <summary>Shown while <see cref="IsReady"/> is <see langword="false"/>.</summary>
	public string WaitingMessage
	{
		get => _waitingMessage;
		private set => SetProperty(ref _waitingMessage, value);
	}

	/// <summary>Pushes the host's readiness state onto this tab.</summary>
	/// <param name="ready">Whether the AIRAC data is ready.</param>
	/// <param name="waitingMessage">What to show while it is not.</param>
	public void SetReadiness(bool ready, string waitingMessage)
	{
		WaitingMessage = waitingMessage;
		IsReady = ready;
		RefreshCycleOptions();
	}

	/// <summary>Re-reads each cycle row's label and cache state.</summary>
	private void RefreshCycleOptions()
	{
		foreach (CycleOption option in CycleOptions)
		{
			option.Refresh();
		}

		OnPropertyChanged(nameof(SelectedCycleLabel));
		OpenOnCurrentWhileSavedNextIsUnpublished();
	}

	/// <summary>
	/// A saved Next cycle the FAA hasn't published yet opens on Current instead, and says why. The saved
	/// choice stays as it is, so the tab doesn't read as unsaved. A Next picked this session is left alone.
	/// </summary>
	private void OpenOnCurrentWhileSavedNextIsUnpublished()
	{
		if (!_cycleFromSave
			|| _selectedCyclePosition != AiracCyclePosition.Next
			|| CycleOptions.Single(option => option.Position == AiracCyclePosition.Next).Status != CycleStatus.NotYetPublished)
		{
			return;
		}

		bool wasClean = !IsDirty;
		string next = AppEnvironment.GetAiracCycle(AiracCyclePosition.Next).AiracCycleId;

		SelectedCyclePosition = AiracCyclePosition.Current;

		if (wasClean)
		{
			ClearDirty();
		}

		Toast.Info("Opened on the current AIRAC cycle",
			$"You last used the next cycle, {next}, but the FAA hasn't published it yet.");
	}

	/// <summary>
	/// A click on a cycle row, whether or not it changed the selection: picking the selected cycle again
	/// while it has failed asks the host to try it again.
	/// </summary>
	private void OnCyclePicked(AiracCyclePosition position)
	{
		if (position == _selectedCyclePosition
			&& AiracCycleDataCache.Instance.GetEntry(AppEnvironment.GetAiracCycle(position).AiracCycleId)?.State == CycleDataState.Failed)
		{
			CycleRetryRequested?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		string[] included = [.. IncludedSubServices.Select(s => $"{s.DisplayName} ({DescribeOutputs(s.OutputsOn)})")];

		ServicePreviewRow[] rows =
		[
			new ServicePreviewRow("Cycle", SelectedCycleLabel),
			new ServicePreviewRow("Sub-services", included.Length == 0 ? "None" : string.Join("\n", included)),
			new ServicePreviewRow("Output folder",
				$"`{OutputPreferences.CycleDirectory(AppEnvironment.GetAiracCycle(SelectedCyclePosition).AiracCycleId)}`"),
		];

		return [new ServicePreviewSection("General", rows)];
	}

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		_cyclePositionBeforeReload = _selectedCyclePosition;
		_loading = true;

		try
		{
			SelectedCyclePosition = ResolveSavedCyclePosition();
			_cycleFromSave = true;

			// A reload (Discard, Undo, an import, a profile switch) opens an unpublished saved Next on
			// Current too, as launch does; while loading, that raises no CycleChanged and no unsaved mark.
			OpenOnCurrentWhileSavedNextIsUnpublished();

			// Nothing saved yet: everything is in.
			string? savedSelection = Get(SelectedSubServicesKey);
			HashSet<string>? included = savedSelection is null ? null : ParseList(savedSelection);

			foreach (SubServiceRow row in SubServices)
			{
				row.Load(included?.Contains(row.Key) ?? true, ReadOutputs(row));
			}
		}
		finally
		{
			_loading = false;
		}

		ClearDirty();
	}

	/// <inheritdoc />
	/// <remarks>
	/// This tab's settings drive the rest of the screen: the included sub-services decide which
	/// tabs are greyed out, their outputs what the tabs write, and the cycle which data is loaded.
	/// They are restored with their events suppressed, so discarding changes here has to
	/// re-announce them or the rail keeps showing what the user just took back.
	/// </remarks>
	protected override void OnReloadedFromConfig()
	{
		SubServicesChanged?.Invoke(this, EventArgs.Empty);

		if (_selectedCyclePosition != _cyclePositionBeforeReload)
		{
			CycleChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <inheritdoc />
	protected override void WriteToConfig()
	{
		Set(CyclePositionKey, SelectedCyclePosition.ToString());
		Set(SelectedSubServicesKey, string.Join(',', IncludedSubServices.Select(s => s.Key)));

		foreach (SubServiceRow row in SubServices)
		{
			foreach (SubServiceOutputKinds kind in OfferedKinds(row))
			{
				Set($"{OutputsNode}.{row.Key}.{kind}", YesNo(row.OutputsOn.HasFlag(kind)));
			}
		}
	}

	private void OnSubServicesChanged()
	{
		if (_loading)
		{
			return;
		}

		MarkDirty();
		SubServicesChanged?.Invoke(this, EventArgs.Empty);
	}

	private static IEnumerable<SubServiceOutputKinds> OfferedKinds(SubServiceRow row) =>
		Enum.GetValues<SubServiceOutputKinds>().Where(kind => kind != SubServiceOutputKinds.None && row.Descriptor.Outputs.HasFlag(kind));

	/// <summary>A row's saved outputs; an output with nothing saved is on.</summary>
	private static SubServiceOutputKinds ReadOutputs(SubServiceRow row)
	{
		SubServiceOutputKinds on = SubServiceOutputKinds.None;

		foreach (SubServiceOutputKinds kind in OfferedKinds(row))
		{
			if (IsSavedOutputOn(row.Key, kind))
			{
				on |= kind;
			}
		}

		return on;
	}

	/// <summary>
	/// Whether a sub-service's output is on as last saved, e.g. Procedure Charts' alias file; an output
	/// with nothing saved is on. Unsaved changes on this tab don't count.
	/// </summary>
	/// <param name="subServiceKey">The sub-service's key, e.g. <see cref="AiracSubServices.ProceduresKey"/>.</param>
	/// <param name="kind">The output.</param>
	/// <returns><see langword="true"/> when it is on.</returns>
	public static bool IsSavedOutputOn(string subServiceKey, SubServiceOutputKinds kind)
	{
		string? saved = UserConfigFile.GetValue($"{Node}.{OutputsNode}.{subServiceKey}.{kind}");

		return saved is null || IsYes(saved);
	}


	private static bool IsYes(string value) =>
		value.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) || value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);

	/// <summary>e.g. <c>Alias, GeoJSON</c>.</summary>
	private static string DescribeOutputs(SubServiceOutputKinds on)
	{
		List<string> names = [];
		if (on.HasFlag(SubServiceOutputKinds.Alias)) names.Add("Alias");
		if (on.HasFlag(SubServiceOutputKinds.Geojson)) names.Add("GeoJSON");
		if (on.HasFlag(SubServiceOutputKinds.ProcedureChanges)) names.Add("Procedure Changes");
		if (on.HasFlag(SubServiceOutputKinds.ProceduresJson)) names.Add("Procedures JSON");
		return string.Join(", ", names);
	}

	/// <summary>
	/// The cycle to open on: the position last saved, so the tab reopens on the same kind of cycle
	/// whichever cycles those are now. A saved Next opens on Next; anything else opens on Current - a
	/// saved Previous too, so last cycle's data is never reopened without the user picking it.
	/// </summary>
	/// <returns>The cycle position to select.</returns>
	private AiracCyclePosition ResolveSavedCyclePosition() =>
		string.Equals(Get(CyclePositionKey)?.Trim(), nameof(AiracCyclePosition.Next), StringComparison.OrdinalIgnoreCase)
			? AiracCyclePosition.Next
			: AiracCyclePosition.Current;

	/// <summary>What a cycle row's state means, for its colour.</summary>
	public enum CycleStatus
	{
		/// <summary>Still being checked, downloaded or parsed.</summary>
		Working,

		/// <summary>Everything is ready.</summary>
		Ready,

		/// <summary>The NASR data is ready but the d-TPP Metafile isn't.</summary>
		Partial,

		/// <summary>The cycle could not be prepared.</summary>
		Failed,

		/// <summary>The FAA hasn't published the cycle yet.</summary>
		NotYetPublished,
	}

	/// <summary>One cycle row in the cycle menu, with its live cache state.</summary>
	/// <param name="position">Which cycle relative to today this row is.</param>
	/// <param name="onSelected">Called when the user picks this row.</param>
	/// <param name="onPicked">Called on every click on this row, even one that leaves the selection as it was.</param>
	public sealed class CycleOption(AiracCyclePosition position, Action<AiracCyclePosition> onSelected, Action<AiracCyclePosition> onPicked)
		: ObservableObject
	{
		private string _label = position.ToString();
		private string _state = string.Empty;
		private CycleStatus _status;
		private string? _dtppNote;
		private string? _toolTip;
		private bool _isSelected = position == AiracCyclePosition.Current;

		/// <summary>Which cycle relative to today this row is.</summary>
		public AiracCyclePosition Position { get; } = position;

		/// <summary>Two-way bound to the cycle radio button. Selecting one drives the parent's selection.</summary>
		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (SetProperty(ref _isSelected, value) && value)
				{
					onSelected(Position);
				}
			}
		}

		/// <summary>
		/// Bound to the cycle radio button's command, which runs on every click: a click on the row that is
		/// already selected changes nothing in <see cref="IsSelected"/>, but can still ask for a retry.
		/// </summary>
		public ICommand PickCommand { get; } = new RelayCommand(() => onPicked(position));

		/// <summary>Called by the parent when the selection changes elsewhere.</summary>
		/// <param name="selected">The newly selected position.</param>
		public void SyncSelected(AiracCyclePosition selected) => SetProperty(ref _isSelected, Position == selected, nameof(IsSelected));

		/// <summary>e.g. <c>Current — 2610 · eff 01 Oct 2026</c>.</summary>
		public string Label
		{
			get => _label;
			private set => SetProperty(ref _label, value);
		}

		/// <summary><c>ready</c> / <c>partial</c> / <c>preparing…</c> / <c>downloading…</c> / <c>parsing…</c> / <c>failed</c> / <c>not yet published</c>.</summary>
		public string State
		{
			get => _state;
			private set => SetProperty(ref _state, value);
		}

		/// <summary>What <see cref="State"/> means, for its colour.</summary>
		public CycleStatus Status
		{
			get => _status;
			private set => SetProperty(ref _status, value);
		}

		/// <summary>The red note beside a <see cref="CycleStatus.Partial"/> cycle; <see langword="null"/> otherwise.</summary>
		public string? DtppNote
		{
			get => _dtppNote;
			private set => SetProperty(ref _dtppNote, value);
		}

		/// <summary>
		/// The row's tooltip while it is <see cref="CycleStatus.Partial"/>: what the missing metafile
		/// is, when to expect it, and what can't be made until then. <see langword="null"/> otherwise.
		/// </summary>
		public string? ToolTip
		{
			get => _toolTip;
			private set => SetProperty(ref _toolTip, value);
		}

		/// <summary>Re-reads this row's label and cache state.</summary>
		public void Refresh()
		{
			AiracCycleInfo info = AppEnvironment.GetAiracCycle(Position);
			Label = $"{Position}  —  {info.AiracCycleId}  ·  eff {info.EffectiveDateUtc:dd MMM yyyy}";

			// The metafile is fetched before a cycle is parsed, so once the cycle is ready, a
			// missing metafile stays missing until the next launch.
			AiracCycleDataCache cache = AiracCycleDataCache.Instance;
			AiracCycleDataCacheEntry? entry = cache.GetEntry(info.AiracCycleId);
			bool missingDtpp = entry?.State == CycleDataState.Ready
				&& cache.FindDtppFile(info.AiracCycleId) is null;

			// A dev launch can leave a cycle unloaded until something asks for it: say so, rather
			// than that it's on its way. A normal launch never does.
			bool leftUnloaded = entry is not null && cache.LoadsOnDemand && !cache.IsLoading(entry);

			State = entry?.State switch
			{
				CycleDataState.Ready => missingDtpp ? "partial" : "ready",
				CycleDataState.Parsing => "parsing…",
				CycleDataState.Downloading => "downloading…",
				CycleDataState.Downloaded => leftUnloaded ? "downloaded, not parsed (dev launch)" : "parsing…",
				CycleDataState.Failed => "failed",
				CycleDataState.NotYetPublished => "not yet published",
				CycleDataState.NotDownloaded when leftUnloaded => "not loaded (dev launch)",
				_ => "preparing…",
			};

			Status = entry?.State switch
			{
				CycleDataState.Ready => missingDtpp ? CycleStatus.Partial : CycleStatus.Ready,
				CycleDataState.Failed => CycleStatus.Failed,
				CycleDataState.NotYetPublished => CycleStatus.NotYetPublished,
				_ => CycleStatus.Working,
			};

			DtppNote = missingDtpp ? "d-TPP Metafile not available yet" : null;
			ToolTip = missingDtpp ? DescribeMissingDtpp(info, AppEnvironment.CheckedUtcDate) : null;
		}

		/// <summary>The tooltip for a cycle whose NASR data is ready but whose d-TPP Metafile isn't.</summary>
		/// <param name="cycle">The cycle.</param>
		/// <param name="today">Today's UTC date, to say whether the metafile is still to come or overdue.</param>
		/// <returns>The tooltip text.</returns>
		internal static string DescribeMissingDtpp(AiracCycleInfo cycle, DateOnly today)
		{
			// The FAA posts the metafile 15-18 days before the cycle takes effect.
			DateOnly from = cycle.EffectiveDateUtc.AddDays(-18);
			DateOnly to = cycle.EffectiveDateUtc.AddDays(-15);

			string when = to >= today
				? $"The FAA posts it 15-18 days before the cycle takes effect, so expect it between {from:dd MMM} and {to:dd MMM yyyy}. " +
					"FE-Buddy looks for it at every launch, so restart FE-Buddy after that."
				: $"The FAA usually posts it 15-18 days before the cycle takes effect ({from:dd MMM} to {to:dd MMM yyyy}), so it " +
					"should be out by now and the download may have failed. FE-Buddy tries again at every launch, so restart FE-Buddy to try now.";

			return
				$"Cycle {cycle.AiracCycleId}'s NASR data is ready, but its d-TPP Metafile isn't. The metafile is the FAA's index of " +
				"charts (approach plates, SIDs, STARs, airport diagrams); the Procedure Charts sub-service is built from it." +
				Environment.NewLine + Environment.NewLine +
				when +
				Environment.NewLine + Environment.NewLine +
				"Until then, with this cycle selected:" + Environment.NewLine +
				"• Procedure Charts writes none of its files: Procedure_Changes.md, Procedures.json and Faa_Chart_Recall.txt " +
				"(so Combined_Alias.txt has no chart recall commands)." + Environment.NewLine +
				"• In the Procedure Charts tab, you can't add procedures or airport-specific procedures." + Environment.NewLine +
				"Every other sub-service runs as normal.";
		}
	}
}
