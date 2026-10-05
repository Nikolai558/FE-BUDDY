using System.Collections.ObjectModel;

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
/// It is a settings tab like any other - it owns the <c>Services.AiracService</c> subtree, is dirty
/// until saved, and has the same one-step undo. Ticking a sub-service brings its tab back from grey
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
	private const string Node = "Services.AiracService";
	private const string SelectedSubServicesKey = "SelectedSubServices";
	private const string OutputsNode = "Outputs";

	private bool _loading;
	private bool _isReady;
	private string _waitingMessage = string.Empty;
	private AiracCyclePosition _selectedCyclePosition = AiracCyclePosition.Current;
	private AiracCyclePosition _cyclePositionBeforeReload = AiracCyclePosition.Current;

	/// <summary>Builds the tab and restores the saved cycle and sub-services.</summary>
	public AiracGeneralTabViewModel()
	{
		CycleOptions =
		[
			new(AiracCyclePosition.Previous, p => SelectedCyclePosition = p),
			new(AiracCyclePosition.Current, p => SelectedCyclePosition = p),
			new(AiracCyclePosition.Next, p => SelectedCyclePosition = p),
		];

		SubServices = new ObservableCollection<SubServiceRow>(
			AiracSubServices.All
				.Where(d => d.Outputs != SubServiceOutputKinds.None)
				.OrderBy(d => d.Order)
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

	/// <summary>A sub-service's row, or <see langword="null"/> for one that isn't in the table (Concatenate Aliases).</summary>
	/// <param name="key">The sub-service key from <see cref="AiracSubServices"/>.</param>
	/// <returns>The row.</returns>
	public SubServiceRow? RowFor(string key) =>
		SubServices.FirstOrDefault(row => string.Equals(row.Key, key, StringComparison.OrdinalIgnoreCase));

	/// <summary>Whether a sub-service is in the table and included.</summary>
	/// <param name="key">The sub-service key from <see cref="AiracSubServices"/>.</param>
	/// <returns><see langword="true"/> when it takes part in the run.</returns>
	public bool IsIncluded(string key) => RowFor(key) is { IsIncluded: true };

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
		private set
		{
			if (SetProperty(ref _isReady, value))
			{
				Revalidate();
			}
		}
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
	public void RefreshCycleOptions()
	{
		foreach (CycleOption option in CycleOptions)
		{
			option.Refresh();
		}

		OnPropertyChanged(nameof(SelectedCycleLabel));
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		string[] included = [.. IncludedSubServices.Select(s => $"{s.DisplayName} ({DescribeOutputs(s.OutputsOn)})")];

		ServicePreviewRow[] rows =
		[
			new ServicePreviewRow("Cycle", SelectedCycleLabel),
			new ServicePreviewRow("Sub-services", included.Length == 0 ? "none" : string.Join(", ", included)),
			new ServicePreviewRow("Output folder",
				OutputPreferences.CycleDirectory(AppEnvironment.GetAiracCycle(SelectedCyclePosition).AiracCycleId)),
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

			// Nothing saved yet: everything is in.
			string? savedSelection = Get(SelectedSubServicesKey);
			HashSet<string>? included = savedSelection is null ? null : ParseList(savedSelection);

			foreach (SubServiceRow row in SubServices)
			{
				row.Load(included?.Contains(row.Key) ?? true, ReadOutputs(row));
			}

			OnPropertyChanged(nameof(IncludedSubServices));
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
		Set("AiracCycleId", AppEnvironment.GetAiracCycle(SelectedCyclePosition).AiracCycleId);
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
		OnPropertyChanged(nameof(IncludedSubServices));
		SubServicesChanged?.Invoke(this, EventArgs.Empty);
	}

	private static IEnumerable<SubServiceOutputKinds> OfferedKinds(SubServiceRow row) =>
		Enum.GetValues<SubServiceOutputKinds>().Where(kind => kind != SubServiceOutputKinds.None && row.Descriptor.Outputs.HasFlag(kind));

	/// <summary>A row's saved outputs; an output with nothing saved is on.</summary>
	private SubServiceOutputKinds ReadOutputs(SubServiceRow row)
	{
		SubServiceOutputKinds on = SubServiceOutputKinds.None;

		foreach (SubServiceOutputKinds kind in OfferedKinds(row))
		{
			string? saved = Get($"{OutputsNode}.{row.Key}.{kind}");

			if (saved is null || IsYes(saved))
			{
				on |= kind;
			}
		}

		return on;
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
	/// Maps the saved cycle id back onto previous / current / next. The id is saved rather than the
	/// position because a position means something different every 28 days; if the saved id is no
	/// longer one of the three, the user gets the current cycle.
	/// </summary>
	/// <returns>The cycle position to select.</returns>
	private static AiracCyclePosition ResolveSavedCyclePosition()
	{
		string? savedId = Normalize(UserConfigFile.GetValue($"{Node}.AiracCycleId"));

		if (savedId is null)
		{
			return AiracCyclePosition.Current;
		}

		foreach (AiracCyclePosition position in new[] { AiracCyclePosition.Previous, AiracCyclePosition.Current, AiracCyclePosition.Next })
		{
			if (string.Equals(AppEnvironment.GetAiracCycle(position).AiracCycleId, savedId, StringComparison.OrdinalIgnoreCase))
			{
				return position;
			}
		}

		return AiracCyclePosition.Current;
	}

	private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

	/// <summary>One cycle row in the cycle menu, with its live cache state.</summary>
	/// <param name="position">Which cycle relative to today this row is.</param>
	/// <param name="onSelected">Called when the user picks this row.</param>
	public sealed class CycleOption(AiracCyclePosition position, Action<AiracCyclePosition> onSelected) : ObservableObject
	{
		private string _label = position.ToString();
		private string _state = string.Empty;
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

		/// <summary>Called by the parent when the selection changes elsewhere.</summary>
		/// <param name="selected">The newly selected position.</param>
		public void SyncSelected(AiracCyclePosition selected) => SetProperty(ref _isSelected, Position == selected, nameof(IsSelected));

		/// <summary>e.g. <c>Current — 2610 · eff 01 Oct 2026</c>.</summary>
		public string Label
		{
			get => _label;
			private set => SetProperty(ref _label, value);
		}

		/// <summary><c>ready</c> / <c>parsing…</c> / <c>failed</c> / <c>not yet published</c>.</summary>
		public string State
		{
			get => _state;
			private set => SetProperty(ref _state, value);
		}

		/// <summary>Re-reads this row's label and cache state.</summary>
		public void Refresh()
		{
			AiracCycleInfo info = AppEnvironment.GetAiracCycle(Position);
			Label = $"{Position}  —  {info.AiracCycleId}  ·  eff {info.EffectiveDateUtc:dd MMM yyyy}";

			AiracCycleDataCacheEntry? entry = AiracCycleDataCache.Instance.GetEntry(info.AiracCycleId);
			State = entry?.State switch
			{
				CycleDataState.Ready => "ready",
				CycleDataState.Parsing => "parsing…",
				CycleDataState.Downloading => "downloading…",
				CycleDataState.Downloaded => "parsing…",
				CycleDataState.Failed => "failed",
				CycleDataState.NotYetPublished => "not yet published",
				_ => "preparing…",
			};
		}
	}
}
