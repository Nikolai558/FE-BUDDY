using System.Windows.Input;
using FeBuddy.Wpf.Infrastructure;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Updates channel + default region of interest. Nothing is persisted - this is
/// the UI only.
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private UpdateChannel _channel = UpdateChannel.Stable;
    private bool _checkOnLaunch = true;
    private RoiMode _roiMode = RoiMode.Custom;
    private string _neLat = string.Empty;
    private string _neLon = string.Empty;
    private string _swLat = string.Empty;
    private string _swLon = string.Empty;
    private string? _savedMessage;

    public SettingsViewModel()
    {
        SaveCommand = new RelayCommand(() => SavedMessage = "UI only - nothing was written to UserConfig.json.");
        CheckNowCommand = new RelayCommand(() => SavedMessage = "You're on the latest build (sample).");
    }

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

    public string? SavedMessage
    {
        get => _savedMessage;
        private set => SetProperty(ref _savedMessage, value);
    }

    public ICommand SaveCommand { get; }

    public ICommand CheckNowCommand { get; }
}
