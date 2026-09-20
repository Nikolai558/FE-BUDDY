using System.Collections.ObjectModel;
using System.Linq;

using FeBuddy.Wpf.Infrastructure;

using FeBuddy.Core.Helpers;
using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac;
using FeBuddy.Core.Services.Airac;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The AIRAC Service <b>General</b> tab: the cycle menu, the facility picker, and the sub-service
/// picker that decides which other tabs exist.
/// </summary>
/// <remarks>
/// It is a settings tab like any other - it owns the <c>Services.AiracService</c> subtree, is dirty
/// until saved, and has the same one-step undo. Ticking a sub-service opens its tab immediately
/// (the user asked for it, so they should see it), but the selection is only persisted on save like
/// every other setting on this tab.
/// </remarks>
public sealed class AiracGeneralTabViewModel : SubServiceSettingsViewModel
{
    private const string Node = "Services.AiracService";
    private const string SelectedSubServicesKey = "SelectedSubServices";

    private bool _loading;
    private bool _isReady;
    private string _waitingMessage = string.Empty;
    private AiracCyclePosition _selectedCyclePosition = AiracCyclePosition.Current;
    private string? _selectedArtccId;

    /// <summary>Builds the tab and restores the saved cycle, facility and sub-service selection.</summary>
    public AiracGeneralTabViewModel()
    {
        CycleOptions = new ObservableCollection<CycleOption>
        {
            new(AiracCyclePosition.Previous, p => SelectedCyclePosition = p),
            new(AiracCyclePosition.Current, p => SelectedCyclePosition = p),
            new(AiracCyclePosition.Next, p => SelectedCyclePosition = p),
        };

        HashSet<string> selectedKeys = ParseSelectedKeysFromConfig();

        SubServices = new ObservableCollection<SubServiceSelection>(
            AiracSubServices.All
                .OrderBy(d => d.Order)
                .Select(d => new SubServiceSelection(d, selectedKeys.Contains(d.Key), OnSubServiceToggled)));

        LoadFromConfig();
        RefreshCycleOptions();
    }

    /// <summary>Raised when the user ticks or unticks a sub-service, so the host can open or close its tab.</summary>
    public event EventHandler? SubServiceSelectionChanged;

    /// <summary>Raised when the user picks a different cycle, so the host can load that cycle's parsed data.</summary>
    public event EventHandler? CycleChanged;

    /// <inheritdoc />
    public override string NodePath => Node;

    /// <inheritdoc />
    public override string Title => "General";

    /// <summary>The three selectable cycles with their live cache state.</summary>
    public ObservableCollection<CycleOption> CycleOptions { get; }

    /// <summary>Facility ids (<c>RespArtccId</c>) from the selected cycle's parsed airports, sorted.</summary>
    public ObservableCollection<string> FacilityOptions { get; } = new();

    /// <summary>Every sub-service the AIRAC Service knows about, ticked or not.</summary>
    public ObservableCollection<SubServiceSelection> SubServices { get; }

    /// <summary>The ticked sub-services, in rail order.</summary>
    public IEnumerable<SubServiceSelection> SelectedSubServices =>
        SubServices.Where(s => s.IsSelected).OrderBy(s => s.Descriptor.Order);

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
            try
            {
                AiracCycleInfo info = AiracCycleResolver.GetCycle(SelectedCyclePosition);
                return $"Cycle {info.AiracCycleId}  ·  effective {info.EffectiveDateUtc:dd MMM yyyy}";
            }
            catch (Exception ex)
            {
                return $"Unavailable: {ex.Message}";
            }
        }
    }

    /// <summary>The chosen facility ARTCC id.</summary>
    public string? SelectedArtccId
    {
        get => _selectedArtccId;
        set
        {
            if (SetProperty(ref _selectedArtccId, value) && !_loading)
            {
                MarkDirty();
            }
        }
    }

    /// <summary>Whether the AIRAC data is ready enough to use the service (remediation plan 2.5).</summary>
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

    /// <summary>Shown while <see cref="IsReady"/> is false.</summary>
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

    /// <summary>Fills the facility list from the selected cycle's parsed airports, keeping the saved choice.</summary>
    /// <param name="data">The parsed NASR data for the selected cycle.</param>
    public void PopulateFacilityOptions(NasrCsvDataCollection data)
    {
        string? previous = SelectedArtccId;
        FacilityOptions.Clear();

        IEnumerable<string> ids = (data.Apt?.AptBase ?? new())
            .Select(a => a.RespArtccId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase);

        foreach (string id in ids)
        {
            FacilityOptions.Add(id);
        }

        if (previous is not null && FacilityOptions.Contains(previous))
        {
            // Re-assert it without dirtying the tab: this is a restore, not an edit.
            _loading = true;
            SelectedArtccId = previous;
            _loading = false;
        }

        Revalidate();
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
    public override IReadOnlyList<ServiceReviewSection> BuildReviewSummary()
    {
        string[] selected = SelectedSubServices.Select(s => s.DisplayName).ToArray();

        ServiceReviewRow[] rows =
        {
            new ServiceReviewRow("Cycle", SelectedCycleLabel),
            new ServiceReviewRow("Facility", string.IsNullOrWhiteSpace(SelectedArtccId) ? "not selected" : SelectedArtccId!),
            new ServiceReviewRow("Sub-services", selected.Length == 0 ? "none" : string.Join(", ", selected)),
        };

        return new[] { new ServiceReviewSection("General", rows) };
    }

    /// <inheritdoc />
    protected override void Validate(ServiceValidation validation)
    {
        // Only once the data is in: before that the facility list is empty and an empty box is
        // "still loading", not "the user forgot".
        if (IsReady && FacilityOptions.Count > 0)
        {
            validation.RequireValue("ArtccId", SelectedArtccId, "Select your ARTCC / facility.");
        }
    }

    /// <inheritdoc />
    protected override void LoadFromConfig()
    {
        _loading = true;

        try
        {
            _selectedArtccId = Normalize(UserConfigFile.GetValue($"{Node}.UserArtccId"));
            SelectedCyclePosition = ResolveSavedCyclePosition();

            HashSet<string> keys = ParseSelectedKeysFromConfig();
            foreach (SubServiceSelection selection in SubServices)
            {
                selection.IsSelected = keys.Contains(selection.Key);
            }

            OnPropertyChanged(nameof(SelectedArtccId));
            OnPropertyChanged(nameof(SelectedSubServices));
        }
        finally
        {
            _loading = false;
        }

        ClearDirty();
    }

    /// <inheritdoc />
    protected override void WriteToConfig()
    {
        try
        {
            Set("AiracCycleId", AiracCycleResolver.GetCycle(SelectedCyclePosition).AiracCycleId);
        }
        catch
        {
            // The cycle lookup table may not cover this date yet; leave the saved id alone.
        }

        Set("UserArtccId", SelectedArtccId ?? string.Empty);
        Set(SelectedSubServicesKey, string.Join(',', SelectedSubServices.Select(s => s.Key)));
    }

    private void OnSubServiceToggled()
    {
        if (_loading)
        {
            return;
        }

        MarkDirty();
        OnPropertyChanged(nameof(SelectedSubServices));
        SubServiceSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private static HashSet<string> ParseSelectedKeysFromConfig()
    {
        string? saved = UserConfigFile.GetValue($"{Node}.{SelectedSubServicesKey}");

        return string.IsNullOrWhiteSpace(saved)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(
                saved.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                StringComparer.OrdinalIgnoreCase);
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
            try
            {
                if (string.Equals(AiracCycleResolver.GetCycle(position).AiracCycleId, savedId, StringComparison.OrdinalIgnoreCase))
                {
                    return position;
                }
            }
            catch
            {
                // That position is not resolvable right now; try the next one.
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
            try
            {
                AiracCycleInfo info = AiracCycleResolver.GetCycle(Position);
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
            catch
            {
                Label = $"{Position}  —  unavailable";
                State = string.Empty;
            }
        }
    }
}
