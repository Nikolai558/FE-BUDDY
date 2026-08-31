using System.Collections.ObjectModel;
using System.Windows.Input;
using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels.Models;
using Microsoft.Win32;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Facility profiles, region of interest, output preferences, display scheme,
/// data source and updates. Nothing is persisted - this is the UI only.
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private string _selectedProfile = "Cleveland ARTCC (ZOB)";
    private string _facilityName = "Cleveland ARTCC";
    private string _artccId = "ZOB";
    private string _outputDir = @"C:\Users\you\Documents\FE-Buddy\ZOB\CRC";
    private bool _singleLineGeoJson = true;
    private bool _febPropsByDefault;
    private CoordPrecision _precision = CoordPrecision.Six;
    private NasrSource _dataSource = NasrSource.Faa;
    private string _customUrl = "https://nfdc.faa.gov/webContent/28DaySub/28DaySubscription_Effective_2025-09-04.zip";
    private string _localNasrPath = string.Empty;
    private UpdateChannel _channel = UpdateChannel.Stable;
    private bool _checkOnLaunch = true;
    private RoiMode _roiMode = RoiMode.Custom;
    private string _neLat = string.Empty;
    private string _neLon = string.Empty;
    private string _swLat = string.Empty;
    private string _swLon = string.Empty;

    public SettingsViewModel()
    {
        Profiles = ["Cleveland ARTCC (ZOB)", "Indianapolis ARTCC (ZID)", "Training (sandbox)"];

        SchemeBcg =
        [
            new DisplayItem(1, "Boundaries"), new DisplayItem(2, "Airways hi"),
            new DisplayItem(3, "Airways lo"), new DisplayItem(4, "Fixes"),
            new DisplayItem(5, "NAVAIDs"), new DisplayItem(6, "Airports"),
            new DisplayItem(7, "Procedures"), new DisplayItem(8, "Text"),
        ];
        SchemeFilters =
        [
            new DisplayItem(1, "Always on"), new DisplayItem(2, "Hi sectors"),
            new DisplayItem(3, "Lo sectors"), new DisplayItem(4, "Approach"),
            new DisplayItem(5, "Ground"),
        ];

        SaveCommand = new RelayCommand(() =>
            Toast.Success("Settings saved", "UI only - nothing was written to UserConfig.json."));
        CheckNowCommand = new RelayCommand(() =>
            Toast.Warn("Update available", "v3.0.1 is ready on the dev channel."));
        NewProfileCommand = new RelayCommand(() => Toast.Info("New profile", "A blank profile would be created here."));
        ExportProfileCommand = new RelayCommand(() => Toast.Info("Exported", $"{SelectedProfile}.febprofile.json (sample)."));
        ImportProfileCommand = new RelayCommand(() => Toast.Info("Import profile", "Pick a .febprofile.json to load."));
        AddBcgCommand = new RelayCommand(() => SchemeBcg.Add(new DisplayItem(SchemeBcg.Count + 1, "New group")));
        AddFilterCommand = new RelayCommand(() => SchemeFilters.Add(new DisplayItem(SchemeFilters.Count + 1, "New filter")));
        RemoveSchemeItemCommand = new RelayCommand<DisplayItem>(RemoveSchemeItem);
        ExportLegendCommand = new RelayCommand(() =>
            Toast.Success("Legend exported", "ISR_DISPLAY_LEGEND.txt (sample)."));
        TestSourceCommand = new RelayCommand(() =>
            Toast.Success("Source reachable", $"{DataSourceLabel} responded 200 OK (sample)."));
        BrowseNasrCommand = new RelayCommand(BrowseNasr);
    }

    // ---- facility profiles (replace v2.x's hard-coded ARTCC list + Desktop output) ----

    public ObservableCollection<string> Profiles { get; }

    public string SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (!SetProperty(ref _selectedProfile, value))
            {
                return;
            }

            // Sample: switching a profile loads its fields.
            (FacilityName, ArtccId, OutputDir) = value switch
            {
                "Indianapolis ARTCC (ZID)" =>
                    ("Indianapolis ARTCC", "ZID", @"C:\Users\you\Documents\FE-Buddy\ZID\CRC"),
                "Training (sandbox)" =>
                    ("Training", "ZZZ", @"C:\Users\you\Documents\FE-Buddy\Training\CRC"),
                _ => ("Cleveland ARTCC", "ZOB", @"C:\Users\you\Documents\FE-Buddy\ZOB\CRC"),
            };
        }
    }

    public string FacilityName { get => _facilityName; set => SetProperty(ref _facilityName, value); }

    public string ArtccId { get => _artccId; set => SetProperty(ref _artccId, value); }

    public string OutputDir { get => _outputDir; set => SetProperty(ref _outputDir, value); }

    // ---- output preferences ----

    public bool SingleLineGeoJson { get => _singleLineGeoJson; set => SetProperty(ref _singleLineGeoJson, value); }

    public bool FebPropsByDefault { get => _febPropsByDefault; set => SetProperty(ref _febPropsByDefault, value); }

    public CoordPrecision Precision { get => _precision; set => SetProperty(ref _precision, value); }

    // ---- display scheme (BCG groups + filters, exported as an ISR legend) ----

    public ObservableCollection<DisplayItem> SchemeBcg { get; }

    public ObservableCollection<DisplayItem> SchemeFilters { get; }

    // ---- data source (v2.x has broken repeatedly on FAA URL changes) ----

    public NasrSource DataSource
    {
        get => _dataSource;
        set
        {
            if (SetProperty(ref _dataSource, value))
            {
                OnPropertyChanged(nameof(IsCustomUrl));
                OnPropertyChanged(nameof(IsLocalFile));
                OnPropertyChanged(nameof(DataSourceLabel));
            }
        }
    }

    public bool IsCustomUrl => DataSource == NasrSource.CustomUrl;

    public bool IsLocalFile => DataSource == NasrSource.LocalFile;

    public string DataSourceLabel => DataSource switch
    {
        NasrSource.CustomUrl => "custom URL",
        NasrSource.LocalFile => "local file",
        _ => "nfdc.faa.gov",
    };

    public string CustomUrl { get => _customUrl; set => SetProperty(ref _customUrl, value); }

    public string LocalNasrPath { get => _localNasrPath; set => SetProperty(ref _localNasrPath, value); }

    // ---- updates ----

    public UpdateChannel Channel { get => _channel; set => SetProperty(ref _channel, value); }

    public bool CheckOnLaunch { get => _checkOnLaunch; set => SetProperty(ref _checkOnLaunch, value); }

    public RoiMode RoiMode
    {
        get => _roiMode;
        set { if (SetProperty(ref _roiMode, value)) OnPropertyChanged(nameof(UseCustomRoi)); }
    }

    public bool UseCustomRoi => RoiMode == RoiMode.Custom;

    public string NeLat { get => _neLat; set => SetProperty(ref _neLat, value); }

    public string NeLon { get => _neLon; set => SetProperty(ref _neLon, value); }

    public string SwLat { get => _swLat; set => SetProperty(ref _swLat, value); }

    public string SwLon { get => _swLon; set => SetProperty(ref _swLon, value); }

    // ---- commands ----

    public ICommand SaveCommand { get; }

    public ICommand CheckNowCommand { get; }

    public ICommand NewProfileCommand { get; }

    public ICommand ExportProfileCommand { get; }

    public ICommand ImportProfileCommand { get; }

    public ICommand AddBcgCommand { get; }

    public ICommand AddFilterCommand { get; }

    public ICommand RemoveSchemeItemCommand { get; }

    public ICommand ExportLegendCommand { get; }

    public ICommand TestSourceCommand { get; }

    public ICommand BrowseNasrCommand { get; }

    private void RemoveSchemeItem(DisplayItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (!SchemeBcg.Remove(item))
        {
            SchemeFilters.Remove(item);
        }
    }

    private void BrowseNasr()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select a local NASR subscription zip",
            Filter = "NASR subscription (*.zip)|*.zip|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog() == true)
        {
            LocalNasrPath = dialog.FileName;
        }
    }
}
