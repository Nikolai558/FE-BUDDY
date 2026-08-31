using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;
using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The AIRAC cycle build screen. Pick a cycle, choose which output families to
/// generate (each maps to a v2.x generator, now clipped to the ROI), tune the
/// airway sub-options, then run. The run is <b>scripted</b> - steps are built
/// from the enabled families; nothing is written.
/// </summary>
public sealed class AiracViewModel : ObservableObject
{
    private const double StepInterval = 0.5; // seconds per step

    private bool _currentCycle = true;
    private AirwayOutputMode _outputMode = AirwayOutputMode.HighLow;
    private bool _bufferWaypoints;
    private bool _includeFebProperties;
    private bool _dmeCutoff = true;
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
        Families =
        [
            // Glyphs are Segoe Fluent code-points.
            new OutputFamily("", "Airports", "APT symbols + text, runway lines", "3 GeoJSON"),
            new OutputFamily("", "NAVAIDs", "VOR / NDB symbols + text", "2 GeoJSON"),
            new OutputFamily("", "Fixes", "RNAV fix symbols + text", "2 GeoJSON"),
            new OutputFamily("", "ARTCC boundaries", "High / low boundary lines", "2 GeoJSON"),
            new OutputFamily("", "Airways", "V / J + RNAV routes, DME-cutoff variant", "6 GeoJSON"),
            new OutputFamily("", "DP / STAR procedures", "Per-procedure + combined", "42 GeoJSON"),
            new OutputFamily("", "Weather stations", "AWOS / ASOS symbols + text", "2 GeoJSON"),
            new OutputFamily("", "Alias & reference", "AWY alias, ISR, chart recall, telephony", "6 alias", enabled: true),
            new OutputFamily("", "Publications", "Airport info text", "1 text", enabled: false),
        ];

        Airways.PropertyChanged += OnAirwaysChanged;

        _runTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(StepInterval) };
        _runTimer.Tick += OnRunTick;

        GenerateCommand = new RelayCommand(StartRun, () => Phase != GenPhase.Running);
        CancelRunCommand = new RelayCommand(CancelRun, () => Phase == GenPhase.Running);
        DismissRunCommand = new RelayCommand(() => Phase = GenPhase.Idle);
        OpenOutputCommand = new RelayCommand(() => Toast.Info("Preview only", "No output folder is wired up."));
        ResetCommand = new RelayCommand(Reset);
    }

    // ---- cycle (sample data) -----------------------------------------

    public bool CurrentCycle
    {
        get => _currentCycle;
        set => SetProperty(ref _currentCycle, value);
    }

    public string CycleId => CurrentCycle ? "2509" : "2510";

    public string EffectiveLabel => CurrentCycle
        ? "Effective 04 SEP 2025  ·  current"
        : "Effective 02 OCT 2025  ·  18 days out";

    public string ApraNote => "Cross-checked against FAA APRA · d-TPP metafile published";

    // ---- output families -------------------------------------------

    public ObservableCollection<OutputFamily> Families { get; }

    private OutputFamily Airways => Families[4];

    public bool ShowAirwayOptions => Airways.Enabled;

    // ---- airway sub-options ---------------------------------------

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

    /// <summary>Emit the "DME cutoff" line variant (airways trimmed 5 nm / 2 nm from fixes). v2.x #144.</summary>
    public bool DmeCutoff
    {
        get => _dmeCutoff;
        set => SetProperty(ref _dmeCutoff, value);
    }

    // ---- region of interest -------------------------------------

    public bool OverrideRoi
    {
        get => _overrideRoi;
        set => SetProperty(ref _overrideRoi, value);
    }

    public string NeLat { get => _neLat; set => SetProperty(ref _neLat, value); }

    public string NeLon { get => _neLon; set => SetProperty(ref _neLon, value); }

    public string SwLat { get => _swLat; set => SetProperty(ref _swLat, value); }

    public string SwLon { get => _swLon; set => SetProperty(ref _swLon, value); }

    // ---- run panel -----------------------------------------------

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

    private void OnAirwaysChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OutputFamily.Enabled))
        {
            OnPropertyChanged(nameof(ShowAirwayOptions));
        }
    }

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
            Toast.Success("Cycle build complete",
                $"{_fileCount} files written to …\\FE-Buddy\\Output ({CycleId}).");
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
        _fileCount = 0;
        Steps.Add(new RunStep("Downloading FAA NASR subscription"));
        Steps.Add(new RunStep($"Clipping to region of interest{(OverrideRoi ? " (override)" : "")}"));

        foreach (var family in Families.Where(f => f.Enabled))
        {
            if (family.Name == "Airways")
            {
                Steps.Add(new RunStep("Building airway LineStrings (efficient handling)"));
                if (BufferWaypoints)
                {
                    Steps.Add(new RunStep("Buffering airway waypoints (2.5 / 5 nm)"));
                }

                if (IncludeFebProperties)
                {
                    Steps.Add(new RunStep("Applying feb.* properties"));
                }

                Steps.Add(new RunStep(OutputMode switch
                {
                    AirwayOutputMode.HighLow => "Writing Airways_High / _Low / _Other.geojson",
                    AirwayOutputMode.Designation => "Writing Airways_J / V / Q / AT …",
                    _ => "Skipping airway GeoJSON",
                }));

                if (DmeCutoff && OutputMode != AirwayOutputMode.None)
                {
                    Steps.Add(new RunStep("Writing DME-cutoff line variant"));
                }
            }
            else
            {
                Steps.Add(new RunStep($"Generating {family.Name}"));
            }

            _fileCount += CountFor(family);
        }

        Steps.Add(new RunStep("Running alias duplicate check"));
    }

    private int CountFor(OutputFamily family) => family.Name switch
    {
        "Airports" => 3,
        "NAVAIDs" => 2,
        "Fixes" => 2,
        "ARTCC boundaries" => 2,
        "Airways" => OutputMode == AirwayOutputMode.None ? 0 : (DmeCutoff ? 8 : 6),
        "DP / STAR procedures" => 42,
        "Weather stations" => 2,
        "Alias & reference" => 6,
        "Publications" => 1,
        _ => 1,
    };

    private void Reset()
    {
        _runTimer.Stop();
        Steps.Clear();
        Phase = GenPhase.Idle;
        Progress = 0;
        CurrentCycle = true;
        OutputMode = AirwayOutputMode.HighLow;
        BufferWaypoints = false;
        IncludeFebProperties = false;
        DmeCutoff = true;
        OverrideRoi = false;
        NeLat = NeLon = SwLat = SwLon = string.Empty;
        foreach (var family in Families)
        {
            family.Enabled = family.Name != "Publications";
        }
    }
}
