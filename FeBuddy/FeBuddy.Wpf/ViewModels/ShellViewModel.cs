using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.Views;

using FEBuddyLibrary.Models.Services.Airac;
using FEBuddyLibrary.Models.Services.General;
using FEBuddyLibrary.Services.Airac;
using FEBuddyLibrary.Services.General;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Backs <c>ShellWindow</c>: navigation, the status-bar Zulu clock, the top-centre AIRAC
/// status indicator, and the version chip / update state. Everything but navigation is driven
/// by <see cref="AppEnvironment"/> and <see cref="AiracCycleDataCache"/> - no sample data
/// (remediation plan Phase 4).
/// </summary>
public sealed class ShellViewModel : ObservableObject
{
    // Segoe Fluent Icons code-points (see Theme/Icons.xaml for the same set in XAML).
    private const string GlyphDashboard = ""; // Home
    private const string GlyphAiracService = ""; // Calendar (MDL2) - AIRAC cycle; U+25F7 was not a font glyph
    private const string GlyphMap = ""; // MapPin
    private const string GlyphSettings = ""; // Setting
    private const string GlyphInfo = ""; // Info

    private readonly Dispatcher _dispatcher;

    private object? _current;
    private string _zuluClock = string.Empty;
    private bool _navCollapsed;
    private bool _updateDeclinedThisSession;

    private string _airacStatus = "Preparing AIRAC data…";
    private string _versionText = "…";
    private bool _isUpdateAvailable;
    private bool _isOnline = true;
    private string _versionBrushKey = "Brush.Accent.Text";
    private string _updateTooltipTitle = "Checking for updates…";
    private string _updateTooltipBody = string.Empty;

    public ShellViewModel()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        PrimaryNav =
        [
            Nav("Dashboard",      GlyphDashboard,     () => new DashboardViewModel()),
            Nav("AIRAC Service",  GlyphAiracService,  () => new AiracServiceViewModel()),
            Nav("Map",            GlyphMap,           () => new MapViewModel()),
        ];

        SystemNav =
        [
            Nav("Settings", GlyphSettings, () => new SettingsViewModel()),
            Nav("Info",     GlyphInfo,     () => new InfoViewModel()),
        ];

        SystemHealth = new ObservableCollection<HealthRow>();

        RecheckCommand = new RelayCommand(RecheckNow);
        ToggleNavCommand = new RelayCommand(() => NavCollapsed = !NavCollapsed);
        OpenUpdateWindowCommand = new RelayCommand(OpenUpdateWindow, () => IsUpdateAvailable);

        PrimaryNav[0].IsActive = true;

        // Zulu clock, ticked once a second on the UI thread.
        UpdateZulu();
        var clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        clock.Tick += (_, _) => UpdateZulu();
        clock.Start();

        // Live launch/pipeline state.
        AppEnvironment.Changed += OnEnvironmentChanged;
        AiracCycleDataCache.Instance.StateChanged += OnCycleStateChanged;

        RefreshVersionState();
        RefreshAiracStatus();
        RefreshSystemHealth();
    }

    public ObservableCollection<NavItem> PrimaryNav { get; }

    public ObservableCollection<NavItem> SystemNav { get; }

    public ObservableCollection<HealthRow> SystemHealth { get; }

    public ICommand RecheckCommand { get; }

    public ICommand ToggleNavCommand { get; }

    /// <summary>Opens the modal update window. Enabled only when an update is available (4.2).</summary>
    public ICommand OpenUpdateWindowCommand { get; }

    /// <summary>Icons-only nav rail when true.</summary>
    public bool NavCollapsed
    {
        get => _navCollapsed;
        set => SetProperty(ref _navCollapsed, value);
    }

    /// <summary>View-model of the section currently on screen.</summary>
    public object? Current
    {
        get => _current;
        private set => SetProperty(ref _current, value);
    }

    /// <summary>
    /// The top-centre AIRAC status readout: narrates the launch pipeline
    /// (<c>Downloading cycle 2610…</c> → <c>Parsing cycle 2610…</c> →
    /// <c>AIRAC 2610 · eff 01 OCT 2026</c>), then settles on the current cycle. Never a
    /// constant - it reads <see cref="AiracCycleDataCache"/> state (4.3).
    /// </summary>
    public string AiracStatus
    {
        get => _airacStatus;
        private set => SetProperty(ref _airacStatus, value);
    }

    /// <summary>The running version, e.g. <c>v3.0.0</c> or <c>dev</c>.</summary>
    public string VersionText
    {
        get => _versionText;
        private set => SetProperty(ref _versionText, value);
    }

    /// <summary>True when a newer version exists on the user's channel - makes the version chip a button (4.2).</summary>
    public bool IsUpdateAvailable
    {
        get => _isUpdateAvailable;
        private set
        {
            if (SetProperty(ref _isUpdateAvailable, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>Whether the machine currently has internet - gates the update tooltip (4.1).</summary>
    public bool IsOnline
    {
        get => _isOnline;
        private set => SetProperty(ref _isOnline, value);
    }

    /// <summary>
    /// Resource key for the version text brush: <c>Brush.Accent.Text</c> normally, or
    /// <c>Brush.Warn</c> for the rest of the session once the user declines an available
    /// update (Developer_Notes TITLE BAR).
    /// </summary>
    public string VersionBrushKey
    {
        get => _versionBrushKey;
        private set
        {
            if (SetProperty(ref _versionBrushKey, value))
            {
                OnPropertyChanged(nameof(VersionIsWarning));
            }
        }
    }

    /// <summary>True once the user has declined an available update this session - the version text turns amber.</summary>
    public bool VersionIsWarning => _versionBrushKey == "Brush.Warn";

    /// <summary>Title line of the FE-BUDDY caption tooltip - update state only (4.1).</summary>
    public string UpdateTooltipTitle
    {
        get => _updateTooltipTitle;
        private set => SetProperty(ref _updateTooltipTitle, value);
    }

    /// <summary>Body line of the FE-BUDDY caption tooltip.</summary>
    public string UpdateTooltipBody
    {
        get => _updateTooltipBody;
        private set => SetProperty(ref _updateTooltipBody, value);
    }

    /// <summary>e.g. <c>1543Z  ·  Tue 30 Aug</c>.</summary>
    public string ZuluClock
    {
        get => _zuluClock;
        private set => SetProperty(ref _zuluClock, value);
    }

    /// <summary>Worst state across <see cref="SystemHealth"/> - drives the nav dot colour.</summary>
    public StatusKind HealthWorst =>
        SystemHealth.Any(h => h.Kind == StatusKind.Down) ? StatusKind.Down :
        SystemHealth.Any(h => h.Kind == StatusKind.Warn) ? StatusKind.Warn :
        StatusKind.Ok;

    public string HealthSummary
    {
        get
        {
            var issues = SystemHealth.Count(h => h.Kind is StatusKind.Warn or StatusKind.Down);
            return issues switch
            {
                0 => "All systems nominal",
                1 => "1 needs attention",
                _ => $"{issues} need attention",
            };
        }
    }

    private void OnEnvironmentChanged(object? sender, EventArgs e) =>
        _dispatcher.BeginInvoke(() =>
        {
            RefreshVersionState();
            RefreshSystemHealth();
        });

    private void OnCycleStateChanged(object? sender, AiracCycleDataCacheEntry e) =>
        _dispatcher.BeginInvoke(() =>
        {
            RefreshAiracStatus();
            RefreshSystemHealth();
        });

    private void RefreshVersionState()
    {
        IsOnline = AppEnvironment.HasInternetConnection;

        VersionCheckResult? version = AppEnvironment.Version;

        if (version is null)
        {
            VersionText = AppEnvironment.LaunchCompleted ? "dev" : "…";
            IsUpdateAvailable = false;
            UpdateTooltipTitle = IsOnline ? "Checking for updates…" : "Update state unknown offline";
            UpdateTooltipBody = string.Empty;
            VersionBrushKey = "Brush.Accent.Text";
            return;
        }

        VersionText = version.CurrentVersion is "0.0.0" or "" ? "dev" : $"v{version.CurrentVersion.TrimStart('v', 'V')}";
        IsUpdateAvailable = version.UpdateAvailable && version.LatestVersion is not null;

        if (!version.CheckSucceeded)
        {
            UpdateTooltipTitle = "Update state unknown offline";
            UpdateTooltipBody = version.Message ?? string.Empty;
            VersionBrushKey = "Brush.Accent.Text";
        }
        else if (IsUpdateAvailable)
        {
            UpdateTooltipTitle = $"v{version.LatestVersion} available!";
            UpdateTooltipBody = $"You are on v{version.CurrentVersion.TrimStart('v', 'V')}. Click the version to review and update.";
            VersionBrushKey = _updateDeclinedThisSession ? "Brush.Warn" : "Brush.Accent.Text";
        }
        else
        {
            UpdateTooltipTitle = "You are running the latest version.";
            UpdateTooltipBody = string.Empty;
            VersionBrushKey = "Brush.Accent.Text";
        }
    }

    private void RefreshAiracStatus()
    {
        IReadOnlyList<AiracCycleDataCacheEntry> entries = AiracCycleDataCache.Instance.Entries;

        if (entries.Count == 0)
        {
            AiracStatus = AppEnvironment.LaunchCompleted ? "AIRAC data not loaded" : "Preparing AIRAC data…";
            return;
        }

        AiracCycleDataCacheEntry? downloading = entries.FirstOrDefault(x => x.State == CycleDataState.Downloading);
        if (downloading is not null)
        {
            AiracStatus = $"Downloading cycle {downloading.Cycle.AiracCycleId}…";
            return;
        }

        AiracCycleDataCacheEntry? parsing = entries.FirstOrDefault(x => x.State == CycleDataState.Parsing);
        if (parsing is not null)
        {
            AiracStatus = $"Parsing cycle {parsing.Cycle.AiracCycleId}…";
            return;
        }

        AiracCycleReadiness readiness = AiracCycleDataCache.Instance.ComputeReadiness();
        AiracCycleDataCacheEntry? current = entries.FirstOrDefault(x => x.Position == AiracCyclePosition.Current);

        AiracStatus = readiness switch
        {
            AiracCycleReadiness.Unavailable => "AIRAC data unavailable — current cycle failed",
            AiracCycleReadiness.Waiting => "Waiting for AIRAC data…",
            _ when current is not null =>
                $"AIRAC {current.Cycle.AiracCycleId}  ·  eff {current.Cycle.EffectiveDateUtc:dd MMM yyyy}"
                + (readiness == AiracCycleReadiness.Degraded ? "  ·  degraded" : string.Empty),
            _ => "Waiting for AIRAC data…",
        };
    }

    private void RefreshSystemHealth()
    {
        SystemHealth.Clear();

        SystemHealth.Add(AppEnvironment.HasInternetConnection
            ? new HealthRow("Internet", "connected", StatusKind.Ok)
            : new HealthRow("Internet", "offline — online features are disabled", StatusKind.Down));

        AiracCycleReadiness readiness = AiracCycleDataCache.Instance.Entries.Count == 0
            ? AiracCycleReadiness.Waiting
            : AiracCycleDataCache.Instance.ComputeReadiness();

        SystemHealth.Add(readiness switch
        {
            AiracCycleReadiness.Ready => new HealthRow("AIRAC data", "previous / current / next ready", StatusKind.Ok),
            AiracCycleReadiness.Degraded => new HealthRow("AIRAC data", "ready — a best-effort cycle failed", StatusKind.Warn),
            AiracCycleReadiness.Unavailable => new HealthRow("AIRAC data", "current cycle failed to download or parse", StatusKind.Down),
            _ => new HealthRow("AIRAC data", "downloading and parsing…", StatusKind.Warn),
        });

        VersionCheckResult? version = AppEnvironment.Version;
        SystemHealth.Add(version switch
        {
            null => new HealthRow("Updates", "checking…", StatusKind.Warn),
            { CheckSucceeded: false } => new HealthRow("Updates", "state unknown (offline)", StatusKind.Warn),
            { UpdateAvailable: true } v => new HealthRow("Updates", $"v{v.LatestVersion} available", StatusKind.Warn),
            _ => new HealthRow("Updates", "latest version", StatusKind.Ok),
        });

        OnPropertyChanged(nameof(HealthWorst));
        OnPropertyChanged(nameof(HealthSummary));
    }

    private void RecheckNow()
    {
        Toast.Info("Re-checking", "Contacting the time and version services…");
        _ = AppEnvironment.RecheckAsync();
    }

    private void OpenUpdateWindow()
    {
        VersionCheckResult? version = AppEnvironment.Version;
        if (version is null || !version.UpdateAvailable)
        {
            return;
        }

        UpdateWindowViewModel vm = new(version);
        UpdateWindow window = new()
        {
            DataContext = vm,
            Owner = Application.Current?.MainWindow,
        };

        window.ShowDialog();

        if (vm.UserDeclined)
        {
            _updateDeclinedThisSession = true;
            RefreshVersionState();
        }
    }

    private void UpdateZulu()
        => ZuluClock = DateTime.UtcNow.ToString("HHmm'Z'  ·  ddd dd MMM", CultureInfo.InvariantCulture);

    private NavItem Nav(string title, string glyph, Func<object> factory)
        => new(title, glyph, factory, OnActivated);

    private void OnActivated(NavItem item)
    {
        foreach (var other in PrimaryNav.Concat(SystemNav))
        {
            if (!ReferenceEquals(other, item))
            {
                other.IsActive = false;
            }
        }

        Current = item.ViewModel;
    }
}
