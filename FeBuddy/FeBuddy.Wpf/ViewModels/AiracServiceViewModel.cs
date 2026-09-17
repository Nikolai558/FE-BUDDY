using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Windows.Threading;

using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.Views;

using FEBuddyLibrary.Helpers;
using FEBuddyLibrary.Models.NASR.CSV;
using FEBuddyLibrary.Models.Services.Airac;
using FEBuddyLibrary.Models.Services.General;
using FEBuddyLibrary.Services.Airac;
using FEBuddyLibrary.Services.General;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>AIRAC Services</b> screen (remediation plan Phase 7): the cycle menu, the facility
/// selector, the sub-service list (Airways only, rule 1.3), and the single
/// <b>Run AIRAC Service</b> button. Selecting a sub-service opens its settings page inside
/// this screen with an "AIRAC Service › Airways" breadcrumb.
/// </summary>
public sealed class AiracServiceViewModel : ObservableObject
{
    private const string CycleIdKey = "Services.AiracService.AiracCycleId";
    private const string ArtccIdKey = "Services.AiracService.UserArtccId";

    private readonly Dispatcher _dispatcher;

    private AiracCyclePosition _selectedCyclePosition = AiracCyclePosition.Current;
    private string? _selectedArtccId;
    private object? _selectedSubService;
    private bool _isRunning;
    private NasrCsvDataCollection? _parsedForSelectedCycle;
    private string? _parsedCycleId;

    public AiracServiceViewModel()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        Airways = new AirwaysViewModel();
        SubServices = new ObservableCollection<object> { Airways };

        CycleOptions = new ObservableCollection<CycleOption>
        {
            new(AiracCyclePosition.Previous, p => SelectedCyclePosition = p),
            new(AiracCyclePosition.Current, p => SelectedCyclePosition = p),
            new(AiracCyclePosition.Next, p => SelectedCyclePosition = p),
        };

        RunCommand = new RelayCommand(async () => await RunAsync(), () => !IsRunning && IsReady);
        OpenSubServiceCommand = new RelayCommand<object>(vm => SelectedSubService = vm ?? Airways);

        // Restore the persisted selection.
        _selectedArtccId = string.IsNullOrWhiteSpace(UserConfigFile.GetValue(ArtccIdKey)) ? null : UserConfigFile.GetValue(ArtccIdKey);

        AiracCycleDataCache.Instance.StateChanged += (_, _) => _dispatcher.BeginInvoke(RefreshReadiness);
        AppEnvironment.Changed += (_, _) => _dispatcher.BeginInvoke(RefreshReadiness);

        RefreshCycleOptions();
        RefreshReadiness();
        _ = LoadCycleDataAsync();
    }

    /// <summary>The Airways sub-service page, hosted inside this screen.</summary>
    public AirwaysViewModel Airways { get; }

    /// <summary>The sub-service view-models, in order. Airways only today (rule 1.3).</summary>
    public ObservableCollection<object> SubServices { get; }

    /// <summary>The three selectable cycles with their live cache state.</summary>
    public ObservableCollection<CycleOption> CycleOptions { get; }

    public ICommand RunCommand { get; }

    /// <summary>Parameter is a sub-service view-model.</summary>
    public ICommand OpenSubServiceCommand { get; }

    /// <summary>Facility ids (<c>RespArtccId</c>) from the selected cycle's parsed airports, sorted.</summary>
    public ObservableCollection<string> FacilityOptions { get; } = new();

    /// <summary>Which cycle (relative to today) the run uses. Persists to <c>Services.AiracService.AiracCycleId</c>.</summary>
    public AiracCyclePosition SelectedCyclePosition
    {
        get => _selectedCyclePosition;
        set
        {
            if (SetProperty(ref _selectedCyclePosition, value))
            {
                foreach (CycleOption option in CycleOptions)
                {
                    option.SyncSelected(value);
                }

                OnPropertyChanged(nameof(SelectedCycleLabel));
                PersistSelectedCycle();
                _ = LoadCycleDataAsync();
            }
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

    /// <summary>The chosen facility ARTCC id. Persists to <c>Services.AiracService.UserArtccId</c>.</summary>
    public string? SelectedArtccId
    {
        get => _selectedArtccId;
        set
        {
            if (SetProperty(ref _selectedArtccId, value) && !string.IsNullOrWhiteSpace(value))
            {
                UserConfigFile.TrySetValue(ArtccIdKey, value!);
                UserConfigFile.Save("Services.AiracService");
            }
        }
    }

    /// <summary>
    /// The sub-service page currently on screen (its settings menu), or <see langword="null"/>
    /// for the landing state before the user picks one from <see cref="SubServices"/>.
    /// </summary>
    public object? SelectedSubService
    {
        get => _selectedSubService;
        set
        {
            if (SetProperty(ref _selectedSubService, value))
            {
                OnPropertyChanged(nameof(Breadcrumb));
                OnPropertyChanged(nameof(HasSelectedSubService));
            }
        }
    }

    /// <summary>Whether a sub-service is open - gates the landing placeholder vs. its page.</summary>
    public bool HasSelectedSubService => SelectedSubService is not null;

    /// <summary>e.g. <c>AIRAC Service › Airways</c>.</summary>
    public string Breadcrumb => SelectedSubService switch
    {
        Infrastructure.SubServiceSettingsViewModel s => $"AIRAC Service  ›  {s.BreadcrumbTitle}",
        _ => "AIRAC Service",
    };

    /// <summary><see langword="true"/> while a run is in progress.</summary>
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// The AIRAC data readiness (remediation plan 2.5). Controls whether the facility list, the
    /// designations list, and <b>Run AIRAC Service</b> are enabled.
    /// </summary>
    public AiracCycleReadiness Readiness =>
        AiracCycleDataCache.Instance.Entries.Count == 0 ? AiracCycleReadiness.Waiting
        : AiracCycleDataCache.Instance.ComputeReadiness();

    /// <summary>Whether the service is usable (Ready or Degraded).</summary>
    public bool IsReady => Readiness is AiracCycleReadiness.Ready or AiracCycleReadiness.Degraded;

    /// <summary>Shown while <see cref="IsReady"/> is false.</summary>
    public string WaitingMessage => Readiness switch
    {
        AiracCycleReadiness.Unavailable => "The current AIRAC cycle failed to download or parse. The AIRAC Service is unavailable — see the Dashboard activity log.",
        _ => "Waiting for AIRAC data to finish downloading and parsing. This service will be available in a moment.",
    };

    private void RefreshCycleOptions()
    {
        foreach (CycleOption option in CycleOptions)
        {
            option.Refresh();
        }

        OnPropertyChanged(nameof(SelectedCycleLabel));
    }

    private void RefreshReadiness()
    {
        RefreshCycleOptions();
        OnPropertyChanged(nameof(Readiness));
        OnPropertyChanged(nameof(IsReady));
        OnPropertyChanged(nameof(WaitingMessage));
        Airways.SetReadiness(IsReady);
        CommandManager.InvalidateRequerySuggested();
    }

    private void PersistSelectedCycle()
    {
        try
        {
            string id = AiracCycleResolver.GetCycle(SelectedCyclePosition).AiracCycleId;
            UserConfigFile.TrySetValue(CycleIdKey, id);
            UserConfigFile.Save("Services.AiracService");
        }
        catch
        {
            // The lookup table may not cover this date yet; nothing to persist.
        }
    }

    private async Task LoadCycleDataAsync()
    {
        if (!IsReady)
        {
            return;
        }

        string cycleId;
        try
        {
            cycleId = AiracCycleResolver.GetCycle(SelectedCyclePosition).AiracCycleId;
        }
        catch
        {
            return;
        }

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
                PopulateFacilityOptions(data);
                Airways.LoadCycleDependentLists(data);
            });
        }
        catch (Exception ex)
        {
            AppLog.Warning("AiracServiceView", $"Could not load parsed data for cycle {cycleId}: {ex.Message}");
        }
    }

    private void PopulateFacilityOptions(NasrCsvDataCollection data)
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
            SelectedArtccId = previous;
        }
    }

    private async Task RunAsync()
    {
        if (Airways.IsDirty)
        {
            bool proceed = ConfirmWindow.Show(
                Application.Current?.MainWindow,
                "Unsaved settings",
                "The Airways settings have unsaved changes. The newly input data will be saved before execution.",
                confirmText: "Save & Continue");

            if (!proceed)
            {
                return;
            }

            if (!Airways.Save())
            {
                return; // validation failed; Airways.Save toasted the reason
            }
        }

        string cycleId;
        AiracCycleInfo cycle;
        try
        {
            cycle = AiracCycleResolver.GetCycle(SelectedCyclePosition);
            cycleId = cycle.AiracCycleId;
        }
        catch (Exception ex)
        {
            Toast.Error("Cannot run", ex.Message);
            return;
        }

        IsRunning = true;
        Airways.BeginRun();

        try
        {
            string outputDir = ResolveOutputDirectory();
            bool addFeBuddyFolder = ResolveAddFeBuddyFolder();

            AiracServiceSettings settings = new()
            {
                SelectedCycle = cycle,
                ArtccId = SelectedArtccId ?? string.Empty,
                OutputDirectory = outputDir,
                AddFeBuddyOutputFolder = addFeBuddyFolder,
                Airways = Airways.BuildSettingsBlock(outputDir, addFeBuddyFolder),
            };

            var progress = new Progress<AiracServiceProgress>(p =>
                _dispatcher.BeginInvoke(() => Airways.ReportProgress(p.Message)));

            AiracServiceResult result = await AiracService.RunAsync(settings, progress);

            Airways.ApplyAiracResult(result);
            Toast.Success("AIRAC Service complete",
                $"Cycle {cycleId}: {result.Airways?.AirwayCount ?? 0:N0} airway(s)"
                + (result.ExcludedAirwayIds.Count > 0 ? $", {result.ExcludedAirwayIds.Count} excluded" : string.Empty) + ".");
        }
        catch (Exception ex)
        {
            Airways.FailRun(ex.Message);
            Toast.Error("AIRAC Service failed", ex.Message);
        }
        finally
        {
            IsRunning = false;
        }
    }

    private static string ResolveOutputDirectory()
    {
        string? saved = UserConfigFile.GetValue("General.DefaultOutputDirectory");
        return string.IsNullOrWhiteSpace(saved)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "FE-Buddy_Output")
            : saved!;
    }

    private static bool ResolveAddFeBuddyFolder()
    {
        string? saved = UserConfigFile.GetValue("General.AddFeBuddyOutputFolder");
        return !string.Equals(saved, "N", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>One cycle row in the cycle menu, with its live cache state.</summary>
    public sealed class CycleOption(AiracCyclePosition position, Action<AiracCyclePosition> onSelected) : ObservableObject
    {
        private string _label = position.ToString();
        private string _state = string.Empty;
        private bool _isSelected = position == AiracCyclePosition.Current;

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
