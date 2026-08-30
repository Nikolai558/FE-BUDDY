using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Options screen for the Airways -> GeoJSON generator. "Generate" runs a
/// <b>scripted</b> progress sequence (no real work) so the run UI can be seen -
/// the step list is built from the toggles above it, and completion raises a toast.
/// </summary>
public sealed class AiracViewModel : ObservableObject
{
    private const double StepInterval = 0.55; // seconds per step

    private AirwayOutputMode _outputMode = AirwayOutputMode.HighLow;
    private bool _bufferWaypoints;
    private bool _includeFebProperties;
    private bool _overrideRoi;
    private string _neLat = string.Empty;
    private string _neLon = string.Empty;
    private string _swLat = string.Empty;
    private string _swLon = string.Empty;

    private readonly DispatcherTimer _runTimer;
    private GenPhase _phase = GenPhase.Idle;
    private double _progress;
    private double _elapsed;
    private int _stepIndex;
    private int _fileCount;

    public AiracViewModel()
    {
        _runTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(StepInterval) };
        _runTimer.Tick += OnRunTick;

        GenerateCommand = new RelayCommand(StartRun, () => Phase != GenPhase.Running);
        CancelRunCommand = new RelayCommand(CancelRun, () => Phase == GenPhase.Running);
        DismissRunCommand = new RelayCommand(() => Phase = GenPhase.Idle);
        OpenOutputCommand = new RelayCommand(() => Toast.Info("Preview only", "No output folder is wired up."));
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

    public string OutputModeHint => OutputMode switch
    {
        AirwayOutputMode.None => "Airway data will not be written to GeoJSON.",
        AirwayOutputMode.HighLow =>
            "Airways_High (MAA >= 18,000 ft), Airways_Low (0 < MAA < 18,000 ft), Airways_Other (neither).",
        AirwayOutputMode.Designation =>
            "One file per designation - Airways_J.geojson, Airways_V.geojson, Airways_AT.geojson, ...",
        _ => string.Empty,
    };

    public bool BufferWaypoints
    {
        get => _bufferWaypoints;
        set => SetProperty(ref _bufferWaypoints, value);
    }

    public bool IncludeFebProperties
    {
        get => _includeFebProperties;
        set => SetProperty(ref _includeFebProperties, value);
    }

    public bool OverrideRoi
    {
        get => _overrideRoi;
        set => SetProperty(ref _overrideRoi, value);
    }

    public string NeLat { get => _neLat; set => SetProperty(ref _neLat, value); }

    public string NeLon { get => _neLon; set => SetProperty(ref _neLon, value); }

    public string SwLat { get => _swLat; set => SetProperty(ref _swLat, value); }

    public string SwLon { get => _swLon; set => SetProperty(ref _swLon, value); }

    // ---- run panel ------------------------------------------------------

    public ObservableCollection<RunStep> Steps { get; } = [];

    public GenPhase Phase
    {
        get => _phase;
        private set
        {
            if (SetProperty(ref _phase, value))
            {
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(ShowRunPanel));
                OnPropertyChanged(nameof(RunTitle));
            }
        }
    }

    public bool IsRunning => Phase == GenPhase.Running;

    public bool ShowRunPanel => Phase != GenPhase.Idle;

    public string RunTitle => Phase switch
    {
        GenPhase.Running => "GENERATING",
        GenPhase.Complete => "RUN COMPLETE",
        GenPhase.Cancelled => "RUN CANCELLED",
        _ => string.Empty,
    };

    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public string ElapsedText => $"{_elapsed:0.0}s";

    public ICommand GenerateCommand { get; }

    public ICommand CancelRunCommand { get; }

    public ICommand DismissRunCommand { get; }

    public ICommand OpenOutputCommand { get; }

    public ICommand ResetCommand { get; }

    // ------------------------------------------------------------------

    private void StartRun()
    {
        BuildSteps();
        _stepIndex = 0;
        _elapsed = 0;
        Progress = 0;
        Phase = GenPhase.Running;
        OnPropertyChanged(nameof(ElapsedText));

        if (Steps.Count > 0)
        {
            Steps[0].State = StatusKind.Pending;
        }

        _runTimer.Start();
    }

    private void OnRunTick(object? sender, EventArgs e)
    {
        _elapsed += StepInterval;
        OnPropertyChanged(nameof(ElapsedText));

        if (_stepIndex < Steps.Count)
        {
            Steps[_stepIndex].State = StatusKind.Ok;
            _stepIndex++;
        }

        Progress = Steps.Count == 0 ? 100 : Math.Min(100, _stepIndex * 100.0 / Steps.Count);

        if (_stepIndex >= Steps.Count)
        {
            _runTimer.Stop();
            Progress = 100;
            Phase = GenPhase.Complete;
            Toast.Success("GeoJSON generated",
                $"{_fileCount} file{(_fileCount == 1 ? "" : "s")} written to …\\FE-Buddy\\Output.");
            return;
        }

        Steps[_stepIndex].State = StatusKind.Pending;
    }

    private void CancelRun()
    {
        _runTimer.Stop();
        foreach (var step in Steps)
        {
            if (step.State == StatusKind.Pending)
            {
                step.State = StatusKind.Idle;
            }
        }

        Phase = GenPhase.Cancelled;
        Toast.Warn("Run cancelled", "No files were written.");
    }

    private void BuildSteps()
    {
        Steps.Clear();
        Steps.Add(new RunStep("Reading NASR airway records"));
        Steps.Add(new RunStep("Building LineStrings (efficient handling)"));

        if (BufferWaypoints)
        {
            Steps.Add(new RunStep("Buffering waypoints (2.5 / 5 nm)"));
        }

        if (IncludeFebProperties)
        {
            Steps.Add(new RunStep("Applying feb.* properties"));
        }

        switch (OutputMode)
        {
            case AirwayOutputMode.HighLow:
                Steps.Add(new RunStep("Writing Airways_High.geojson"));
                Steps.Add(new RunStep("Writing Airways_Low.geojson"));
                Steps.Add(new RunStep("Writing Airways_Other.geojson"));
                _fileCount = 3;
                break;
            case AirwayOutputMode.Designation:
                Steps.Add(new RunStep("Writing Airways_J / V / Q / AT …"));
                _fileCount = 9;
                break;
            default:
                Steps.Add(new RunStep("Skipping GeoJSON output"));
                _fileCount = 0;
                break;
        }

        Steps.Add(new RunStep("Writing alias commands (.<id>F)"));
        _fileCount += 1;
    }

    private void Reset()
    {
        _runTimer.Stop();
        Steps.Clear();
        Phase = GenPhase.Idle;
        Progress = 0;
        OutputMode = AirwayOutputMode.HighLow;
        BufferWaypoints = false;
        IncludeFebProperties = false;
        OverrideRoi = false;
        NeLat = NeLon = SwLat = SwLon = string.Empty;
    }
}
