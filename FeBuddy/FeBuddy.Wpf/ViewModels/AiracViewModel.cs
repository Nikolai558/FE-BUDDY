using System.Windows.Input;
using FeBuddy.Wpf.Infrastructure;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Options screen for the Airways -> GeoJSON generator (the feature currently
/// under development). No generation happens here - the "Generate" button just
/// reports that the real work lives in the library.
/// </summary>
public sealed class AiracViewModel : ObservableObject
{
    private AirwayOutputMode _outputMode = AirwayOutputMode.HighLow;
    private bool _bufferWaypoints;
    private bool _includeFebProperties;
    private bool _overrideRoi;
    private string _neLat = string.Empty;
    private string _neLon = string.Empty;
    private string _swLat = string.Empty;
    private string _swLon = string.Empty;
    private string? _statusMessage;

    public AiracViewModel()
    {
        GenerateCommand = new RelayCommand(
            () => StatusMessage = "Preview only - generation runs in FEBuddyLibrary.");
        ResetCommand = new RelayCommand(Reset);
    }

    /// <summary>Which split the generator applies to its output files.</summary>
    public AirwayOutputMode OutputMode
    {
        get => _outputMode;
        set
        {
            if (SetProperty(ref _outputMode, value))
            {
                OnPropertyChanged(nameof(OutputModeHint));
            }
        }
    }

    /// <summary>One-line explanation that follows the selected <see cref="OutputMode"/>.</summary>
    public string OutputModeHint => OutputMode switch
    {
        AirwayOutputMode.None => "Airway data will not be written to GeoJSON.",
        AirwayOutputMode.HighLow =>
            "Airways_High (MAA >= 18,000 ft), Airways_Low (0 < MAA < 18,000 ft), Airways_Other (neither).",
        AirwayOutputMode.Designation =>
            "One file per designation - Airways_J.geojson, Airways_V.geojson, Airways_AT.geojson, ...",
        _ => string.Empty,
    };

    /// <summary>2.5 nm radius around 5-character fixes, 5 nm around NAVAIDs and others.</summary>
    public bool BufferWaypoints
    {
        get => _bufferWaypoints;
        set => SetProperty(ref _bufferWaypoints, value);
    }

    /// <summary>Emit "feb.AwyId" (and friends) as feature properties.</summary>
    public bool IncludeFebProperties
    {
        get => _includeFebProperties;
        set => SetProperty(ref _includeFebProperties, value);
    }

    /// <summary>Replace the default region of interest for this run only.</summary>
    public bool OverrideRoi
    {
        get => _overrideRoi;
        set => SetProperty(ref _overrideRoi, value);
    }

    public string NeLat { get => _neLat; set => SetProperty(ref _neLat, value); }

    public string NeLon { get => _neLon; set => SetProperty(ref _neLon, value); }

    public string SwLat { get => _swLat; set => SetProperty(ref _swLat, value); }

    public string SwLon { get => _swLon; set => SetProperty(ref _swLon, value); }

    /// <summary>Set after a (mock) generate; shown as a chip in the footer.</summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ICommand GenerateCommand { get; }

    public ICommand ResetCommand { get; }

    private void Reset()
    {
        OutputMode = AirwayOutputMode.HighLow;
        BufferWaypoints = false;
        IncludeFebProperties = false;
        OverrideRoi = false;
        NeLat = NeLon = SwLat = SwLon = string.Empty;
        StatusMessage = null;
    }
}
