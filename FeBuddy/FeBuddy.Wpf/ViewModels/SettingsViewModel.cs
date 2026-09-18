using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.Views;

using FeBuddy.Core.Helpers;
using FeBuddy.Core.Models.Services.Airac;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.Airac;
using FeBuddy.Core.Services.General;

using Microsoft.Win32;

using LibUpdateChannel = FeBuddy.Core.Models.Services.General.UpdateChannel;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// SYSTEM ▸ Settings (remediation plan Phase 9). Section order: Updates, Facility Profile,
/// Default Region of Interest, GeoJSON Files. Every value persists to <c>UserConfig.json</c>.
/// Multi-profile support, the display-scheme editor and the NASR data-source override are
/// gone.
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private const string ChannelKey = "General.UpdateChannel";
    private const string OutputDirKey = "General.DefaultOutputDirectory";
    private const string AddFolderKey = "General.AddFeBuddyOutputFolder";
    private const string ArtccKey = "Services.AiracService.UserArtccId";
    private const string PrecisionKey = "Services.AiracService.CoordinatePrecision";

    private readonly Dispatcher _dispatcher;

    private LibUpdateChannel _channel;
    private string? _selectedFacility;
    private string _outputDir = string.Empty;
    private bool _addFeBuddyFolder = true;
    private int _coordinatePrecision = 6;
    private RegionOfInterest? _defaultRoi;
    private bool _isDirty;

    public SettingsViewModel()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        _channel = VersionCheckResult.ParseChannel(UserConfigFile.GetValue(ChannelKey));
        _selectedFacility = Blank(UserConfigFile.GetValue(ArtccKey));
        _outputDir = Blank(UserConfigFile.GetValue(OutputDirKey)) ?? DefaultOutputDirectory;
        _addFeBuddyFolder = !string.Equals(UserConfigFile.GetValue(AddFolderKey), "N", StringComparison.OrdinalIgnoreCase);
        _coordinatePrecision = int.TryParse(UserConfigFile.GetValue(PrecisionKey), out int p) && p is >= 0 and <= 15 ? p : 6;

        _defaultRoi = DefaultRoiStore.Load();
        DefaultRoiStore.Changed += OnDefaultRoiChanged;

        SaveCommand = new RelayCommand(Save);
        CheckNowCommand = new RelayCommand(() => { Toast.Info("Checking…", "Contacting the version service."); _ = AppEnvironment.RecheckAsync(); },
            () => AppEnvironment.HasInternetConnection);
        RollbackCommand = new RelayCommand(() =>
            BrowserLauncher.Open("https://github.com/Nikolai558/FE-Buddy-DEV/releases"));
        BrowseOutputCommand = new RelayCommand(BrowseOutput);
        SetPrecisionCommand = new RelayCommand<string>(p => { if (int.TryParse(p, out int n)) CoordinatePrecision = n; });
        EditRoiCommand = new RelayCommand(EditRoi);
        ClearRoiCommand = new RelayCommand(ClearRoi, () => DefaultRoi is not null);

        AiracCycleDataCache.Instance.StateChanged += (_, _) => _dispatcher.BeginInvoke(RefreshFacilities);
        RefreshFacilities();
    }

    /// <summary><see langword="true"/> when a saved setting has been edited since the last Save.</summary>
    public bool IsDirty
    {
        get => _isDirty;
        private set => SetProperty(ref _isDirty, value);
    }

    /// <summary>Marks the page dirty. Call from every setter whose value Save() persists.</summary>
    private void MarkDirty() => IsDirty = true;

    // ================= 1. UPDATES =================

    public IReadOnlyList<LibUpdateChannel> Channels { get; } =
        new[] { LibUpdateChannel.Stable, LibUpdateChannel.Beta, LibUpdateChannel.Alpha };

    /// <summary>The update channel. <see cref="LibUpdateChannel.Stable"/> unless the developers tell you otherwise.</summary>
    public LibUpdateChannel Channel
    {
        get => _channel;
        set { if (SetProperty(ref _channel, value)) MarkDirty(); }
    }

    public bool IsOnline => AppEnvironment.HasInternetConnection;

    public ICommand CheckNowCommand { get; }

    public ICommand RollbackCommand { get; }

    // ================= 2. FACILITY PROFILE =================

    /// <summary>Facilities from the current cycle's parsed airports, as <c>ArtccName (RespArtccId)</c>.</summary>
    public ObservableCollection<FacilityOption> Facilities { get; } = new();

    /// <summary>The selected facility's <c>RespArtccId</c>. Persists to <c>Services.AiracService.UserArtccId</c>.</summary>
    public string? SelectedFacility
    {
        get => _selectedFacility;
        set { if (SetProperty(ref _selectedFacility, value)) MarkDirty(); }
    }

    public bool FacilitiesReady { get; private set; }

    public string FacilityWaitingMessage =>
        "Waiting for AIRAC data to finish downloading and parsing. The facility list will be available in a moment.";

    /// <summary>The default output directory: <c>%USERPROFILE%\Desktop\FE-Buddy_Output</c>.</summary>
    public static string DefaultOutputDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "FE-Buddy_Output");

    public string OutputDirectory
    {
        get => _outputDir;
        set { if (SetProperty(ref _outputDir, value)) MarkDirty(); }
    }

    /// <summary>When on (default), output is written under a <c>FE-Buddy_Output</c> folder; off means straight to the chosen directory.</summary>
    public bool AddFeBuddyOutputFolder
    {
        get => _addFeBuddyFolder;
        set { if (SetProperty(ref _addFeBuddyFolder, value)) MarkDirty(); }
    }

    public ICommand BrowseOutputCommand { get; }

    // ================= 3. DEFAULT REGION OF INTEREST =================

    public const string RoiExplainer =
        "Region of Interest (ROI): a lat/lon axis-aligned rectangular region defined by southwest " +
        "(bottom-left) and northeast (top-right) corners - a box defining the data you are interested in. " +
        "Depending on the data type and operation, geometries may be clipped to the ROI or included in " +
        "full when associated with an entity inside it. Make the box a little larger than your ARTCC " +
        "boundary so nearby data still appears. Some operations let you override this ROI for specific " +
        "files later.";

    public RegionOfInterest? DefaultRoi
    {
        get => _defaultRoi;
        private set
        {
            if (SetProperty(ref _defaultRoi, value))
            {
                OnPropertyChanged(nameof(DefaultRoiSummary));
                OnPropertyChanged(nameof(HasDefaultRoi));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool HasDefaultRoi => DefaultRoi is not null;

    public string DefaultRoiSummary => DefaultRoi is { } r
        ? $"SW {r.SwLat:0.####}, {r.SwLon:0.####}    ·    NE {r.NeLat:0.####}, {r.NeLon:0.####}"
        : "No default ROI is set.";

    public ICommand EditRoiCommand { get; }

    public ICommand ClearRoiCommand { get; }

    // ================= 4. GEOJSON FILES =================

    public const string FebPropertiesDescription =
        "Include FE-Buddy Properties, when available. Custom GeoJSON property fields that increase file " +
        "size but can be helpful for debugging or viewing data in a GeoJSON viewer in order to identify " +
        "an object. Every FE-Buddy property is prefixed with feb.";

    public const string CoordinatePrecisionDescription =
        "Will round all coordinates in GeoJSON files to a maximum number of decimal points in order to " +
        "save space but retain your desired level of accuracy.";

    public int CoordinatePrecision
    {
        get => _coordinatePrecision;
        private set
        {
            if (SetProperty(ref _coordinatePrecision, value))
            {
                OnPropertyChanged(nameof(IsPrecision5));
                OnPropertyChanged(nameof(IsPrecision6));
                OnPropertyChanged(nameof(IsPrecision7));
                MarkDirty();
            }
        }
    }

    public bool IsPrecision5 => CoordinatePrecision == 5;
    public bool IsPrecision6 => CoordinatePrecision == 6;
    public bool IsPrecision7 => CoordinatePrecision == 7;

    /// <summary>Parameter is <c>"5"</c>, <c>"6"</c> or <c>"7"</c>.</summary>
    public ICommand SetPrecisionCommand { get; }

    // ================= save =================

    public ICommand SaveCommand { get; }

    private void Save()
    {
        UserConfigFile.TrySetValue(ChannelKey, Channel.ToString());
        UserConfigFile.TrySetValue(OutputDirKey, OutputDirectory);
        UserConfigFile.TrySetValue(AddFolderKey, AddFeBuddyOutputFolder ? "Y" : "N");
        UserConfigFile.TrySetValue(PrecisionKey, CoordinatePrecision.ToString(CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(SelectedFacility))
        {
            UserConfigFile.TrySetValue(ArtccKey, SelectedFacility!);
        }

        UserConfigFile.Write();
        IsDirty = false;
        Toast.Success("Settings saved", "Written to UserConfig.json.");
    }

    private void RefreshFacilities()
    {
        AiracCycleReadiness readiness = AiracCycleDataCache.Instance.Entries.Count == 0
            ? AiracCycleReadiness.Waiting
            : AiracCycleDataCache.Instance.ComputeReadiness();

        FacilitiesReady = readiness is AiracCycleReadiness.Ready or AiracCycleReadiness.Degraded;
        OnPropertyChanged(nameof(FacilitiesReady));
        OnPropertyChanged(nameof(IsOnline));
        CommandManager.InvalidateRequerySuggested();

        if (!FacilitiesReady || Facilities.Count > 0)
        {
            return;
        }

        _ = LoadFacilitiesAsync();
    }

    private async Task LoadFacilitiesAsync()
    {
        try
        {
            AiracCycleInfo current = AiracCycleResolver.GetCycle(AiracCyclePosition.Current);
            var data = await AiracCycleDataCache.Instance.GetAsync(current.AiracCycleId).ConfigureAwait(false);

            var options = (data.Apt?.AptBase ?? new())
                .Where(a => !string.IsNullOrWhiteSpace(a.RespArtccId))
                .Select(a => new FacilityOption(a.RespArtccId.Trim(), string.IsNullOrWhiteSpace(a.ArtccName) ? a.RespArtccId.Trim() : a.ArtccName.Trim()))
                .DistinctBy(o => o.ArtccId, StringComparer.OrdinalIgnoreCase)
                .OrderBy(o => o.Display, StringComparer.OrdinalIgnoreCase)
                .ToList();

            await _dispatcher.BeginInvoke(() =>
            {
                Facilities.Clear();
                foreach (FacilityOption o in options)
                {
                    Facilities.Add(o);
                }
            });
        }
        catch (Exception ex)
        {
            AppLog.Warning("Settings", $"Could not load the facility list: {ex.Message}");
        }
    }

    private void BrowseOutput()
    {
        OpenFolderDialog dialog = new()
        {
            Title = "Select the default output directory",
            InitialDirectory = Directory.Exists(OutputDirectory) ? OutputDirectory : null,
        };

        if (dialog.ShowDialog() == true)
        {
            OutputDirectory = dialog.FolderName;
        }
    }

    private void EditRoi()
    {
        RegionOfInterest? picked = RoiPickerWindow.Pick(
            Application.Current?.MainWindow, DefaultRoi, Map.BaseMap.UsStates);
        if (picked is not null)
        {
            DefaultRoiStore.Set(picked);
            DefaultRoi = picked;
            Toast.Success("Default ROI saved", "Written to UserConfig.json.");
        }
    }

    private void ClearRoi()
    {
        DefaultRoiStore.Clear();
        DefaultRoi = null;
    }

    // View-models live for the whole session (NavItem caches them), so without this the
    // Settings page would keep showing whatever ROI was current when it was first opened, even
    // after the Map page's inline editor changes or clears it.
    private void OnDefaultRoiChanged(object? sender, EventArgs e) =>
        _dispatcher.BeginInvoke(() => DefaultRoi = DefaultRoiStore.Load());

    private static string? Blank(string? v) => string.IsNullOrWhiteSpace(v) ? null : v;

    /// <summary>One facility choice: its <c>RespArtccId</c> and a display label.</summary>
    /// <param name="ArtccId">The facility's ARTCC id.</param>
    /// <param name="Name">The facility's name.</param>
    public sealed record FacilityOption(string ArtccId, string Name)
    {
        /// <summary>e.g. <c>Cleveland ARTCC (ZOB)</c>.</summary>
        public string Display => $"{Name} ({ArtccId})";
    }
}
