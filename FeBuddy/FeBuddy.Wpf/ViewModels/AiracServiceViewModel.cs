using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.Views;

using FeBuddy.Core.Helpers;
using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac;
using FeBuddy.Core.Services.Airac;
using FeBuddy.Core.Services.General;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>AIRAC Services</b> screen: a General tab (cycle, facility, and which sub-services to
/// produce), one tab per selected sub-service, and a Review tab that summarises the lot and runs
/// the service.
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
    private readonly ServiceReviewTabViewModel _review;
    private readonly Dictionary<string, ServiceTabViewModel> _tabsByKey = new(StringComparer.OrdinalIgnoreCase);

    private bool _isRunning;
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

        _review = new ServiceReviewTabViewModel("Review", "Run AIRAC Service", RunCommand, () => Tabs);

        AiracCycleDataCache.Instance.StateChanged += (_, _) => _dispatcher.BeginInvoke(RefreshReadiness);
        AppEnvironment.Changed += (_, _) => _dispatcher.BeginInvoke(RefreshReadiness);

        SyncSubServiceTabs();
        RefreshReadiness();
        _ = LoadCycleDataAsync();
    }

    /// <summary>Runs the AIRAC Service for every selected sub-service. Hosted on the Review tab.</summary>
    public ICommand RunCommand { get; }

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
    /// designations list, and the run are enabled.
    /// </summary>
    public AiracCycleReadiness Readiness =>
        AiracCycleDataCache.Instance.Entries.Count == 0 ? AiracCycleReadiness.Waiting
        : AiracCycleDataCache.Instance.ComputeReadiness();

    /// <summary>Whether the service is usable (Ready or Degraded).</summary>
    public bool IsReady => Readiness is AiracCycleReadiness.Ready or AiracCycleReadiness.Degraded;

    /// <inheritdoc />
    protected override ServiceTabViewModel GeneralTab => _general;

    /// <inheritdoc />
    protected override ServiceReviewTabViewModel ReviewTab => _review;

    /// <summary>The Airways tab while it is open, otherwise <see langword="null"/>.</summary>
    private AirwaysViewModel? AirwaysTab =>
        IsSelected(AiracSubServices.AirwaysKey) && _tabsByKey.TryGetValue(AiracSubServices.AirwaysKey, out ServiceTabViewModel? tab)
            ? tab as AirwaysViewModel
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
        List<ServiceTabViewModel> open = new();

        foreach (SubServiceSelection selection in _general.SelectedSubServices)
        {
            if (!_tabsByKey.TryGetValue(selection.Key, out ServiceTabViewModel? tab))
            {
                tab = selection.Descriptor.CreateTab();
                _tabsByKey[selection.Key] = tab;

                if (tab is AirwaysViewModel airways)
                {
                    airways.SetReadiness(IsReady);

                    if (_parsedForSelectedCycle is { } data)
                    {
                        airways.LoadCycleDependentLists(data);
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
        AirwaysTab?.SetReadiness(IsReady);

        CommandManager.InvalidateRequerySuggested();

        _ = LoadCycleDataAsync();
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
            cycleId = AiracCycleResolver.GetCycle(_general.SelectedCyclePosition).AiracCycleId;
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
                _general.PopulateFacilityOptions(data);
                AirwaysTab?.LoadCycleDependentLists(data);
            });
        }
        catch (Exception ex)
        {
            AppLog.Warning("AiracServiceView", $"Could not load parsed data for cycle {cycleId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves anything unsaved (with the user's blessing), refuses to start while a tab is invalid,
    /// then runs every selected sub-service that has a backend.
    /// </summary>
    private async Task RunAsync()
    {
        if (!TrySaveDirtyTabs() || !EnsureNoInvalidTabs())
        {
            return;
        }

        ServiceTabViewModel[] runnable = Tabs
            .Where(t => t.IsRunnable && !ReferenceEquals(t, GeneralTab) && !ReferenceEquals(t, ReviewTab))
            .ToArray();

        if (runnable.Length == 0)
        {
            Toast.Warn("Nothing to run", "Select a sub-service with settings on the General tab first.");
            return;
        }

        AiracCycleInfo cycle;
        try
        {
            cycle = AiracCycleResolver.GetCycle(_general.SelectedCyclePosition);
        }
        catch (Exception ex)
        {
            Toast.Error("Cannot run", ex.Message);
            return;
        }

        AirwaysViewModel? airways = AirwaysTab;

        IsRunning = true;
        airways?.BeginRun();

        try
        {
            string outputDir = ResolveOutputDirectory();
            bool addFeBuddyFolder = ResolveAddFeBuddyFolder();

            AiracServiceSettings settings = new()
            {
                SelectedCycle = cycle,
                ArtccId = _general.SelectedArtccId ?? string.Empty,
                OutputDirectory = outputDir,
                AddFeBuddyOutputFolder = addFeBuddyFolder,
                Airways = airways?.BuildSettingsBlock(outputDir, addFeBuddyFolder),
            };

            var progress = new Progress<AiracServiceProgress>(p =>
                _dispatcher.BeginInvoke(() => airways?.ReportProgress(p.Message)));

            AiracServiceResult result = await AiracService.RunAsync(settings, progress);

            airways?.ApplyAiracResult(result);
            Toast.Success("AIRAC Service complete",
                $"Cycle {cycle.AiracCycleId}: {result.Airways?.AirwayCount ?? 0:N0} airway(s)"
                + (result.ExcludedAirwayIds.Count > 0 ? $", {result.ExcludedAirwayIds.Count} excluded" : string.Empty) + ".");
        }
        catch (Exception ex)
        {
            airways?.FailRun(ex.Message);
            Toast.Error("AIRAC Service failed", ex.Message);
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>Offers to save every dirty tab in one prompt, as the settings-save contract requires.</summary>
    /// <returns><see langword="true"/> when nothing is left unsaved.</returns>
    private bool TrySaveDirtyTabs()
    {
        ServiceTabViewModel[] dirty = Tabs.Where(t => t.IsDirty).ToArray();

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
}
