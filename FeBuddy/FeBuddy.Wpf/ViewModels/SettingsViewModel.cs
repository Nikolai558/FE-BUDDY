using System.Windows.Input;
using FeBuddy.Wpf.Infrastructure;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Updates channel + default region of interest. Nothing is persisted - this is
/// the UI only.
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private string _facilityName = "Cleveland ARTCC";
    private string _artccId = "ZOB";
    private string _outputDir = @"C:\Users\you\Documents\FE-Buddy\Output";
    private bool _singleLineGeoJson = true;
    private bool _febPropsByDefault;
    private CoordPrecision _precision = CoordPrecision.Six;
    private UpdateChannel _channel = UpdateChannel.Stable;
    private bool _checkOnLaunch = true;
    private RoiMode _roiMode = RoiMode.Custom;
    private string _neLat = string.Empty;
    private string _neLon = string.Empty;
    private string _swLat = string.Empty;
    private string _swLon = string.Empty;

    public SettingsViewModel()
    {
        SaveCommand = new RelayCommand(() =>
            Toast.Success("Settings saved", "UI only — nothing was written to UserConfig.json."));
        CheckNowCommand = new RelayCommand(() =>
            Toast.Warn("Update available", "v3.0.1 is ready on the dev channel."));
    }

    // ---- facility profile (replaces v2.x's hard-coded ARTCC list + Desktop output) ----

    public string FacilityName { get => _facilityName; set => SetProperty(ref _facilityName, value); }

    public string ArtccId { get => _artccId; set => SetProperty(ref _artccId, value); }

    public string OutputDir { get => _outputDir; set => SetProperty(ref _outputDir, value); }

    // ---- output preferences ----

    public bool SingleLineGeoJson { get => _singleLineGeoJson; set => SetProperty(ref _singleLineGeoJson, value); }

    public bool FebPropsByDefault { get => _febPropsByDefault; set => SetProperty(ref _febPropsByDefault, value); }

    public CoordPrecision Precision { get => _precision; set => SetProperty(ref _precision, value); }

    // ---- updates ----

    public UpdateChannel Channel
    {
        get => _channel;
        set => SetProperty(ref _channel, value);
    }

    public bool CheckOnLaunch
    {
        get => _checkOnLaunch;
        set => SetProperty(ref _checkOnLaunch, value);
    }

    public RoiMode RoiMode
    {
        get => _roiMode;
        set
        {
            if (SetProperty(ref _roiMode, value))
            {
                OnPropertyChanged(nameof(UseCustomRoi));
            }
        }
    }

    /// <summary>True when the coordinate fields should be shown.</summary>
    public bool UseCustomRoi => RoiMode == RoiMode.Custom;

    public string NeLat { get => _neLat; set => SetProperty(ref _neLat, value); }

    public string NeLon { get => _neLon; set => SetProperty(ref _neLon, value); }

    public string SwLat { get => _swLat; set => SetProperty(ref _swLat, value); }

    public string SwLon { get => _swLon; set => SetProperty(ref _swLon, value); }

    public ICommand SaveCommand { get; }

    public ICommand CheckNowCommand { get; }
}
