using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Input;

using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels.Models;

using FeBuddy.Core.Helpers;
using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac;
using FeBuddy.Core.Models.Services.Airac.Departures;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.General;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Departures</b> sub-service tab inside the AIRAC Service screen: which outputs to write,
/// which GeoJSON files, which procedures and ARTCCs, the optional region of interest, which
/// FE-Buddy properties and the CRC ERAM defaults - plus the run result panel.
/// </summary>
/// <remarks>
/// Save, Undo and navigation come from the tab host's action bar; the run is launched by
/// <b>Run AIRAC Service</b> on the Review tab and arrives back here through
/// <see cref="ISubServiceRunTarget"/>.
/// </remarks>
public sealed class DeparturesViewModel : SubServiceSettingsViewModel, ISubServiceRunTarget
{
    private const string Node = "Services.AiracService.Departures";
    private const string PrecisionKey = "Services.AiracService.CoordinatePrecision";
    private const string CrcClassName = "Departures";
    private const string OutputRootFolderName = "Departure Procedures";

    private const string CrcDefaultsIncompleteMessage =
        "CRC ERAM defaults are on but some values are empty. Fill in the marked boxes, or switch CRC ERAM defaults off.";

    private bool _generateGeojson = true;
    private bool _generateAliasFile = true;
    private bool _emitLines = true;
    private bool _emitSymbols = true;
    private bool _emitText = true;
    private bool _includeObstacleDepartures = true;
    private bool _includeFebCustomProperties;
    private bool _includeCrcEramPropertyDefaults = true;
    private bool _useRoi;
    private DepartureRoiMode _roiMode = DepartureRoiMode.Airport;
    private bool _overrideRoi;
    private string _swLat = string.Empty;
    private string _swLon = string.Empty;
    private string _neLat = string.Empty;
    private string _neLon = string.Empty;
    private HashSet<string> _savedArtccFilter = new(StringComparer.OrdinalIgnoreCase);
    private bool _suppressArtccChanges;

    private bool _isCycleReady;
    private bool _isRunning;
    private bool _hasRun;
    private string? _runError;
    private string? _progressText;
    private double _elapsedSeconds;
    private int _airportProcedureCount;
    private int _skippedForMissingPointsCount;
    private int _geojsonFileCount;
    private string? _aliasFilePath;
    private int _aliasCommandCount;
    private bool _isInfoExpanded;
    private Stopwatch? _stopwatch;

    /// <summary>Builds the tab and restores its saved settings.</summary>
    public DeparturesViewModel()
    {
        FebProperties = new ObservableCollection<DepartureFebPropertyToggle>(
            DepartureFebPropertyNames.All.Select(entry =>
                new DepartureFebPropertyToggle(entry.Property, entry.Name, entry.Description, MarkDirty)));

        LineDefaults = new ObservableCollection<EramClassDefault>
        {
            new(CrcClassName, EramFieldKind.Line, MarkDirty)
        };

        SymbolDefaults = new ObservableCollection<EramClassDefault>
        {
            new(CrcClassName, EramFieldKind.Symbol, MarkDirty)
        };

        TextDefaults = new ObservableCollection<EramClassDefault>
        {
            new(CrcClassName, EramFieldKind.Text, MarkDirty)
        };

        Artccs.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasArtccs));

        OpenOutputCommand = new RelayCommand(OpenOutputFolder, () => LastOutputDirectory is not null);
        PickRoiOnMapCommand = new RelayCommand(PickRoiOnMap);
        ToggleInfoCommand = new RelayCommand(() => IsInfoExpanded = !IsInfoExpanded);
        ClearArtccsCommand = new RelayCommand(ClearArtccs, () => Artccs.Any(a => a.IsSelected));

        // The default ROI is part of this tab's validation (ROI on, no override, nothing to fall
        // back to), so a change made in Settings or on the Map page re-checks the tab as well.
        DefaultRoiStore.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(RoiFallbackHint));
            Revalidate();
        };

        LoadFromConfig();
    }

    /// <inheritdoc />
    public override string NodePath => Node;

    /// <inheritdoc />
    public override string Title => "Departures";

    // ================= outputs =================

    /// <summary>Whether this run writes GeoJSON for the departure procedures.</summary>
    public bool GenerateGeojson
    {
        get => _generateGeojson;
        set
        {
            if (!value && !CanTurnOffOutput())
            {
                // The value never changed, but the control already did - put it back.
                RestoreRejectedToggle(nameof(GenerateGeojson));
                return;
            }

            if (SetProperty(ref _generateGeojson, value))
            {
                MarkDirty();
            }
        }
    }

    /// <summary>Whether this run writes the <c>Departures.txt</c> alias file.</summary>
    public bool GenerateAliasFile
    {
        get => _generateAliasFile;
        set
        {
            if (!value && !CanTurnOffOutput())
            {
                RestoreRejectedToggle(nameof(GenerateAliasFile));
                return;
            }

            if (SetProperty(ref _generateAliasFile, value))
            {
                MarkDirty();
            }
        }
    }

    /// <summary>Emit <c>&lt;airport&gt;_&lt;procedure&gt;_Lines.geojson</c>.</summary>
    public bool EmitLines
    {
        get => _emitLines;
        set { if (SetProperty(ref _emitLines, value)) MarkDirty(); }
    }

    /// <summary>Emit <c>&lt;airport&gt;_&lt;procedure&gt;_Symbols.geojson</c>.</summary>
    public bool EmitSymbols
    {
        get => _emitSymbols;
        set { if (SetProperty(ref _emitSymbols, value)) MarkDirty(); }
    }

    /// <summary>Emit <c>&lt;airport&gt;_&lt;procedure&gt;_Text.geojson</c>.</summary>
    public bool EmitText
    {
        get => _emitText;
        set { if (SetProperty(ref _emitText, value)) MarkDirty(); }
    }

    /// <inheritdoc />
    protected override int EnabledOutputCount =>
        (GenerateGeojson ? 1 : 0) + (GenerateAliasFile ? 1 : 0);

    // ================= procedures =================

    /// <summary>Whether obstacle departures (ODPs) are written alongside the SIDs.</summary>
    public bool IncludeObstacleDepartures
    {
        get => _includeObstacleDepartures;
        set { if (SetProperty(ref _includeObstacleDepartures, value)) MarkDirty(); }
    }

    /// <summary>
    /// One toggle per ARTCC in the selected cycle's <c>DP_BASE</c>. The selected set is the
    /// filter; none selected means every ARTCC. Empty until the cycle's data is loaded.
    /// </summary>
    public ObservableCollection<ArtccToggle> Artccs { get; } = new();

    /// <summary>Whether the ARTCC list has been built from the selected cycle yet.</summary>
    public bool HasArtccs => Artccs.Count > 0;

    /// <summary>Deselects every ARTCC, which means every ARTCC is included.</summary>
    public ICommand ClearArtccsCommand { get; }

    // ================= region of interest =================

    /// <summary>
    /// Whether a region of interest limits the output at all. Off means every departure in NASR -
    /// unlike Airports, Departures does not fall back to the default ROI on its own.
    /// </summary>
    public bool UseRoi
    {
        get => _useRoi;
        set { if (SetProperty(ref _useRoi, value)) MarkDirty(); }
    }

    /// <summary>Whether the ROI selects every departure of an airport inside it.</summary>
    public bool RoiModeAirport
    {
        get => _roiMode == DepartureRoiMode.Airport;
        set { if (value) SetRoiMode(DepartureRoiMode.Airport); }
    }

    /// <summary>Whether the ROI selects any departure with a point inside it.</summary>
    public bool RoiModeWaypoint
    {
        get => _roiMode == DepartureRoiMode.Waypoint;
        set { if (value) SetRoiMode(DepartureRoiMode.Waypoint); }
    }

    /// <summary>Whether Departures uses its own ROI instead of the shared default one.</summary>
    public bool OverrideRoi
    {
        get => _overrideRoi;
        set { if (SetProperty(ref _overrideRoi, value)) { MarkDirty(); OnPropertyChanged(nameof(RoiFallbackHint)); } }
    }

    /// <summary>Southwest corner latitude of the override ROI.</summary>
    public string SwLat { get => _swLat; set { if (SetProperty(ref _swLat, value)) MarkDirty(); } }

    /// <summary>Southwest corner longitude of the override ROI.</summary>
    public string SwLon { get => _swLon; set { if (SetProperty(ref _swLon, value)) MarkDirty(); } }

    /// <summary>Northeast corner latitude of the override ROI.</summary>
    public string NeLat { get => _neLat; set { if (SetProperty(ref _neLat, value)) MarkDirty(); } }

    /// <summary>Northeast corner longitude of the override ROI.</summary>
    public string NeLon { get => _neLon; set { if (SetProperty(ref _neLon, value)) MarkDirty(); } }

    /// <summary>What the ROI is when the override is off.</summary>
    public string RoiFallbackHint => DefaultRoiStore.Load() is { } roi
        ? $"Using the default ROI: SW {roi.SwLat:0.####}, {roi.SwLon:0.####} / NE {roi.NeLat:0.####}, {roi.NeLon:0.####}"
        : "No default ROI is set. Set one in Settings, or override it here.";

    /// <summary>Opens the shared ROI picker and copies what the user confirms into the four boxes.</summary>
    public ICommand PickRoiOnMapCommand { get; }

    // ================= properties =================

    /// <summary>Whether departure Features carry the selected <c>feb.*</c> properties.</summary>
    public bool IncludeFebCustomProperties
    {
        get => _includeFebCustomProperties;
        set { if (SetProperty(ref _includeFebCustomProperties, value)) MarkDirty(); }
    }

    /// <summary>The verbatim explanation of what the FE-Buddy properties are (remediation plan 7.4).</summary>
    public string FebPropertiesDescription =>
        "Custom Geojson Property fields that increases file size but can be helpful for debugging or "
        + "viewing data in a geojson viewer in order to identify object. Every FE-Buddy property will be "
        + "prefixed with \"feb.\"";

    /// <summary>One toggle per available <c>feb.*</c> property.</summary>
    public ObservableCollection<DepartureFebPropertyToggle> FebProperties { get; }

    // ================= CRC ERAM defaults =================

    /// <summary>Whether the CRC ERAM isDefaults Feature is written into each GeoJSON file.</summary>
    public bool IncludeCrcEramPropertyDefaults
    {
        get => _includeCrcEramPropertyDefaults;
        set { if (SetProperty(ref _includeCrcEramPropertyDefaults, value)) MarkDirty(); }
    }

    /// <summary>CRC line defaults for the procedure lines.</summary>
    public ObservableCollection<EramClassDefault> LineDefaults { get; }

    /// <summary>CRC symbol defaults for the procedure points.</summary>
    public ObservableCollection<EramClassDefault> SymbolDefaults { get; }

    /// <summary>CRC text defaults for the point labels.</summary>
    public ObservableCollection<EramClassDefault> TextDefaults { get; }

    // ================= run panel =================

    /// <summary>Whether the AIRAC data is ready; the run and the cycle-dependent lists gate on it.</summary>
    public bool IsCycleReady
    {
        get => _isCycleReady;
        private set => SetProperty(ref _isCycleReady, value);
    }

    /// <summary>Whether a run is in progress.</summary>
    public bool IsRunning
    {
        get => _isRunning;
        private set { if (SetProperty(ref _isRunning, value)) OnPropertyChanged(nameof(ShowPanel)); }
    }

    /// <summary>Whether a run has finished in this session.</summary>
    public bool HasRun
    {
        get => _hasRun;
        private set
        {
            if (SetProperty(ref _hasRun, value))
            {
                OnPropertyChanged(nameof(ShowPanel));
                OnPropertyChanged(nameof(RunSucceeded));
                OnPropertyChanged(nameof(RunFailed));
            }
        }
    }

    /// <summary>Whether the result panel is shown at all.</summary>
    public bool ShowPanel => IsRunning || HasRun;

    /// <summary>The failure message from the last run, or <see langword="null"/>.</summary>
    public string? RunError
    {
        get => _runError;
        private set
        {
            if (SetProperty(ref _runError, value))
            {
                OnPropertyChanged(nameof(RunSucceeded));
                OnPropertyChanged(nameof(RunFailed));
            }
        }
    }

    /// <summary>Whether the last run completed.</summary>
    public bool RunSucceeded => HasRun && RunError is null;

    /// <summary>Whether the last run failed.</summary>
    public bool RunFailed => HasRun && RunError is not null;

    /// <summary>The in-panel progress line while a run is going.</summary>
    public string? ProgressText { get => _progressText; private set => SetProperty(ref _progressText, value); }

    /// <summary>How long the last run took.</summary>
    public double ElapsedSeconds
    {
        get => _elapsedSeconds;
        private set { if (SetProperty(ref _elapsedSeconds, value)) OnPropertyChanged(nameof(ElapsedText)); }
    }

    /// <summary>The elapsed time, formatted.</summary>
    public string ElapsedText => $"{ElapsedSeconds:0.0}s";

    /// <summary>How many airport + procedure pairs were output.</summary>
    public int AirportProcedureCount { get => _airportProcedureCount; private set => SetProperty(ref _airportProcedureCount, value); }

    /// <summary>How many airport + procedure pairs were left out because a point could not be found.</summary>
    public int SkippedForMissingPointsCount
    {
        get => _skippedForMissingPointsCount;
        private set { if (SetProperty(ref _skippedForMissingPointsCount, value)) OnPropertyChanged(nameof(HasSkipped)); }
    }

    /// <summary>Whether any airport + procedure pair was left out.</summary>
    public bool HasSkipped => SkippedForMissingPointsCount > 0;

    /// <summary>
    /// How many GeoJSON files the last run wrote. Only the count is shown: a full run writes
    /// thousands of files, far too many to list.
    /// </summary>
    public int GeojsonFileCount
    {
        get => _geojsonFileCount;
        private set { if (SetProperty(ref _geojsonFileCount, value)) OnPropertyChanged(nameof(HasFiles)); }
    }

    /// <summary>Whether the last run wrote any GeoJSON.</summary>
    public bool HasFiles => GeojsonFileCount > 0;

    /// <summary>The alias file the last run wrote, or <see langword="null"/>.</summary>
    public string? AliasFilePath
    {
        get => _aliasFilePath;
        private set { if (SetProperty(ref _aliasFilePath, value)) OnPropertyChanged(nameof(HasAliasFile)); }
    }

    /// <summary>Whether the last run wrote an alias file.</summary>
    public bool HasAliasFile => AliasFilePath is not null;

    /// <summary>How many alias commands were written.</summary>
    public int AliasCommandCount { get => _aliasCommandCount; private set => SetProperty(ref _aliasCommandCount, value); }

    /// <summary>The last run's messages, grouped by severity.</summary>
    public ObservableCollection<SubServiceMessageGroup> MessageGroups { get; } = new();

    /// <summary>Whether any group needs the user's attention.</summary>
    public bool HasAttentionMessages => MessageGroups.Any(g => g.IsAttentionLevel);

    /// <summary>Whether the run produced only routine messages.</summary>
    public bool HasInfoOnly => MessageGroups.Count > 0 && !HasAttentionMessages;

    /// <summary>Whether the routine messages are expanded.</summary>
    public bool IsInfoExpanded
    {
        get => _isInfoExpanded;
        set { if (SetProperty(ref _isInfoExpanded, value)) OnPropertyChanged(nameof(InfoToggleLabel)); }
    }

    /// <summary>The label on the routine-messages toggle.</summary>
    public string InfoToggleLabel => IsInfoExpanded
        ? "Hide routine messages"
        : $"Show {MessageGroups.Where(g => !g.IsAttentionLevel).Sum(g => g.Count)} routine message(s)";

    /// <summary>Expands or collapses the routine messages.</summary>
    public ICommand ToggleInfoCommand { get; }

    /// <summary>Opens the <c>Departure Procedures</c> folder the last run wrote to.</summary>
    public ICommand OpenOutputCommand { get; }

    private string? LastOutputDirectory { get; set; }

    // ================= parent hooks =================

    /// <summary>Called by the parent whenever AIRAC readiness changes.</summary>
    /// <param name="ready">Whether the AIRAC data is ready.</param>
    public void SetReadiness(bool ready) => IsCycleReady = ready;

    /// <summary>Builds the ARTCC toggle list from the selected cycle's <c>DP_BASE</c>.</summary>
    /// <param name="data">The parsed NASR data for the selected cycle.</param>
    public void LoadCycleDependentLists(NasrCsvDataCollection data)
    {
        _savedArtccFilter = ParseList(Get("ArtccFilter"));

        string[] artccs = (data.Dp?.DpBase ?? new())
            .Select(d => d.Artcc?.Trim().ToUpperInvariant() ?? string.Empty)
            .Where(a => a.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(a => a, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Artccs.Clear();
        foreach (string artcc in artccs)
        {
            Artccs.Add(new ArtccToggle(artcc, _savedArtccFilter.Contains(artcc), OnArtccToggled));
        }

        // The list was empty when this tab snapshotted itself at construction; re-take the
        // snapshot now the toggles reflect what is actually saved.
        ResyncSavedState();
    }

    /// <inheritdoc />
    public void BeginRun()
    {
        RunError = null;
        HasRun = false;
        IsRunning = true;
        ProgressText = "Starting…";
        MessageGroups.Clear();
        AirportProcedureCount = 0;
        SkippedForMissingPointsCount = 0;
        GeojsonFileCount = 0;
        AliasFilePath = null;
        AliasCommandCount = 0;
        LastOutputDirectory = null;
        IsInfoExpanded = false;
        RaisePanelCounts();

        _stopwatch = Stopwatch.StartNew();
        ElapsedSeconds = 0;
    }

    /// <inheritdoc />
    public void ReportProgress(string message) => ProgressText = message;

    /// <inheritdoc />
    public void ApplyAiracResult(AiracServiceResult result)
    {
        _stopwatch?.Stop();
        ElapsedSeconds = _stopwatch?.Elapsed.TotalSeconds ?? 0;
        IsRunning = false;
        HasRun = true;
        ProgressText = null;

        if (result.Departures is { } departures)
        {
            AirportProcedureCount = departures.AirportProcedureCount;
            SkippedForMissingPointsCount = departures.SkippedForMissingPointsCount;
            GeojsonFileCount = departures.GeojsonFilesWritten.Count;
            AliasFilePath = departures.AliasFilePath;
            AliasCommandCount = departures.AliasCommandCount;

            // Every file sits somewhere under "Departure Procedures" (GeoJSON in <ARTCC>\<airport>,
            // the alias file in Alias), so that folder is the one worth opening.
            string? anyWrittenFile = departures.GeojsonFilesWritten.Count > 0
                ? departures.GeojsonFilesWritten[0]
                : departures.AliasFilePath;

            if (anyWrittenFile is not null)
            {
                LastOutputDirectory = FindOutputRoot(anyWrittenFile);
            }

            foreach (SubServiceMessageGroup group in GroupByLevel(departures.Messages))
            {
                MessageGroups.Add(group);
            }
        }

        RaisePanelCounts();
    }

    /// <inheritdoc />
    public void FailRun(string error)
    {
        _stopwatch?.Stop();
        ElapsedSeconds = _stopwatch?.Elapsed.TotalSeconds ?? 0;
        IsRunning = false;
        HasRun = true;
        RunError = error;
        ProgressText = null;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> BuildSettingsBlock(string outputDirectory, bool addFeBuddyOutputFolder)
    {
        // Departures is opt-in: with the ROI off nothing is filtered, even when a default ROI is
        // set. With it on, an explicit override wins, otherwise the shared default ROI.
        RegionOfInterest? fallbackRoi = UseRoi && !OverrideRoi ? DefaultRoiStore.Load() : null;

        Dictionary<string, string> s = new(StringComparer.OrdinalIgnoreCase)
        {
            ["OutputDirectory"] = outputDirectory,
            ["GenerateGeojson"] = YesNo(GenerateGeojson),
            ["EmitLines"] = YesNo(EmitLines),
            ["EmitSymbols"] = YesNo(EmitSymbols),
            ["EmitText"] = YesNo(EmitText),
            ["GenerateAliasFile"] = YesNo(GenerateAliasFile),
            ["IncludeObstacleDepartures"] = YesNo(IncludeObstacleDepartures),
            ["ArtccFilter"] = string.Join(',', SelectedArtccs()),
            // The amendment-date filter is built in Core; the UI for it is deliberately deferred,
            // so the run always asks for every amendment.
            ["AmendedWithinCycles"] = "0",
            ["IncludeFebCustomProperties"] = YesNo(IncludeFebCustomProperties),
            ["FebProperties"] = string.Join(',', FebProperties.Where(p => p.IsSelected).Select(p => p.Name)),
            ["IncludeCrcEramPropertyDefaults"] = YesNo(IncludeCrcEramPropertyDefaults),
            ["FilterByRoi"] = YesNo(UseRoi),
            ["RoiMode"] = _roiMode.ToString(),
            ["CoordinatePrecision"] = ResolveCoordinatePrecision().ToString(CultureInfo.InvariantCulture),
            ["AddFeBuddyOutputFolder"] = YesNo(addFeBuddyOutputFolder),
        };

        if (UseRoi && OverrideRoi)
        {
            s["RoiSwLat"] = SwLat;
            s["RoiSwLon"] = SwLon;
            s["RoiNeLat"] = NeLat;
            s["RoiNeLon"] = NeLon;
        }
        else if (fallbackRoi is { } defaultRoi)
        {
            s["RoiSwLat"] = defaultRoi.SwLat.ToString(CultureInfo.InvariantCulture);
            s["RoiSwLon"] = defaultRoi.SwLon.ToString(CultureInfo.InvariantCulture);
            s["RoiNeLat"] = defaultRoi.NeLat.ToString(CultureInfo.InvariantCulture);
            s["RoiNeLon"] = defaultRoi.NeLon.ToString(CultureInfo.InvariantCulture);
        }

        if (IncludeCrcEramPropertyDefaults && GenerateGeojson)
        {
            // Only the blocks whose file is actually being written; the parser requires exactly
            // those and warns about any others.
            if (EmitLines) WriteCrcRow(s, "Line", LineDefaults[0]);
            if (EmitSymbols) WriteCrcRow(s, "Symbol", SymbolDefaults[0]);
            if (EmitText) WriteCrcRow(s, "Text", TextDefaults[0]);
        }

        return s;
    }

    /// <inheritdoc />
    public override IReadOnlyList<ServiceReviewSection> BuildReviewSummary()
    {
        List<string> outputs = new();
        if (GenerateGeojson) outputs.Add("GeoJSON");
        if (GenerateAliasFile) outputs.Add("Alias file (Departures.txt)");

        List<string> geojsonFiles = new();
        if (EmitLines) geojsonFiles.Add("Lines");
        if (EmitSymbols) geojsonFiles.Add("Symbols");
        if (EmitText) geojsonFiles.Add("Text");

        // Before a cycle is parsed the toggle list is empty; SelectedArtccs falls back to what
        // is saved, so the review does not claim "All" when a filter is on disk.
        string[] selectedArtccs = SelectedArtccs().ToArray();
        string[] selectedProperties = FebProperties.Where(p => p.IsSelected).Select(p => p.Name).ToArray();

        ServiceReviewRow[] rows =
        {
            new ServiceReviewRow("Outputs", string.Join(", ", outputs)),
            new ServiceReviewRow("GeoJSON files", GenerateGeojson ? string.Join(", ", geojsonFiles) : "No"),
            new ServiceReviewRow("Includes", DescribeScope(selectedArtccs)),
            new ServiceReviewRow("Region of interest", DescribeRoi()),
            new ServiceReviewRow("FE-Buddy properties",
                IncludeFebCustomProperties && selectedProperties.Length > 0
                    ? string.Join(", ", selectedProperties)
                    : "No"),
            new ServiceReviewRow("CRC ERAM defaults", IncludeCrcEramPropertyDefaults ? "Yes" : "No"),
        };

        return new[] { new ServiceReviewSection("Departures", rows) };
    }

    // ================= save contract =================

    /// <inheritdoc />
    protected override void LoadFromConfig()
    {
        _generateGeojson = GetBool("GenerateGeojson", true);
        _generateAliasFile = GetBool("GenerateAliasFile", true);
        _emitLines = GetBool("EmitLines", true);
        _emitSymbols = GetBool("EmitSymbols", true);
        _emitText = GetBool("EmitText", true);
        _includeObstacleDepartures = GetBool("IncludeObstacleDepartures", true);
        _includeFebCustomProperties = GetBool("IncludeFebCustomProperties", false);
        _includeCrcEramPropertyDefaults = GetBool("IncludeCrcEramPropertyDefaults", true);

        // Both outputs off would leave the tab in a state its own guard forbids; a hand-edited
        // config is the only way to get here, so fall back to the default rather than honour it.
        if (!_generateGeojson && !_generateAliasFile)
        {
            _generateGeojson = true;
            _generateAliasFile = true;
        }

        HashSet<string> selected = ParseList(Get("FebProperties"));
        foreach (DepartureFebPropertyToggle toggle in FebProperties)
        {
            toggle.IsSelected = selected.Contains(toggle.Name);
        }

        // Re-apply the saved ARTCC filter to any already-built toggles, without a dirty check
        // per toggle; ClearDirty below re-takes the snapshot once.
        _savedArtccFilter = ParseList(Get("ArtccFilter"));
        _suppressArtccChanges = true;
        try
        {
            foreach (ArtccToggle toggle in Artccs)
            {
                toggle.IsSelected = _savedArtccFilter.Contains(toggle.Artcc);
            }
        }
        finally
        {
            _suppressArtccChanges = false;
        }

        _useRoi = GetBool("Roi.UseRoi", false);
        _roiMode = string.Equals(Get("Roi.Mode")?.Trim(), nameof(DepartureRoiMode.Waypoint), StringComparison.OrdinalIgnoreCase)
            ? DepartureRoiMode.Waypoint
            : DepartureRoiMode.Airport;
        _overrideRoi = GetBool("Roi.OverrideDefaultRoi", false);
        _swLat = Get("Roi.OverrideCoordindates.SwLat") ?? string.Empty;
        _swLon = Get("Roi.OverrideCoordindates.SwLon") ?? string.Empty;
        _neLat = Get("Roi.OverrideCoordindates.NeLat") ?? string.Empty;
        _neLon = Get("Roi.OverrideCoordindates.NeLon") ?? string.Empty;

        LoadCrcRow("Line", LineDefaults[0]);
        LoadCrcRow("Symbol", SymbolDefaults[0]);
        LoadCrcRow("Text", TextDefaults[0]);

        RaiseAllSettingProperties();
        ClearDirty();
    }

    /// <inheritdoc />
    protected override void WriteToConfig()
    {
        Set("GenerateGeojson", YesNo(GenerateGeojson));
        Set("GenerateAliasFile", YesNo(GenerateAliasFile));
        Set("EmitLines", YesNo(EmitLines));
        Set("EmitSymbols", YesNo(EmitSymbols));
        Set("EmitText", YesNo(EmitText));
        Set("IncludeObstacleDepartures", YesNo(IncludeObstacleDepartures));
        Set("ArtccFilter", string.Join(',', SelectedArtccs()));
        Set("IncludeFebCustomProperties", YesNo(IncludeFebCustomProperties));
        Set("FebProperties", string.Join(',', FebProperties.Where(p => p.IsSelected).Select(p => p.Name)));
        Set("IncludeCrcEramPropertyDefaults", YesNo(IncludeCrcEramPropertyDefaults));
        Set("Roi.UseRoi", YesNo(UseRoi));
        Set("Roi.Mode", _roiMode.ToString());
        Set("Roi.OverrideDefaultRoi", YesNo(OverrideRoi));
        Set("Roi.OverrideCoordindates.SwLat", SwLat);
        Set("Roi.OverrideCoordindates.SwLon", SwLon);
        Set("Roi.OverrideCoordindates.NeLat", NeLat);
        Set("Roi.OverrideCoordindates.NeLon", NeLon);

        SaveCrcRow("Line", LineDefaults[0]);
        SaveCrcRow("Symbol", SymbolDefaults[0]);
        SaveCrcRow("Text", TextDefaults[0]);
    }

    /// <inheritdoc />
    protected override void Validate(ServiceValidation validation)
    {
        if (GenerateGeojson && !EmitLines && !EmitSymbols && !EmitText)
        {
            validation.Add(
                "GeoJSON is on but none of its files are selected. Turn on Lines, Symbols or Text, "
                + "or switch GeoJSON off.");
        }

        if (IncludeFebCustomProperties && FebProperties.All(p => !p.IsSelected))
        {
            validation.Add("FE-Buddy properties are on but none are selected. Pick at least one, or switch them off.");
        }

        // Only the rows whose file is actually written are needed; a row for a file that is
        // switched off is never read, so it is not held against the user.
        bool crcActive = IncludeCrcEramPropertyDefaults && GenerateGeojson;
        LineDefaults[0].IsRequired = crcActive && EmitLines;
        SymbolDefaults[0].IsRequired = crcActive && EmitSymbols;
        TextDefaults[0].IsRequired = crcActive && EmitText;

        if (LineDefaults.Concat(SymbolDefaults).Concat(TextDefaults)
            .Any(row => row.IsRequired && row.HasMissingValues))
        {
            validation.Add(CrcDefaultsIncompleteMessage);
        }

        if (!UseRoi)
        {
            return;
        }

        if (!OverrideRoi)
        {
            if (DefaultRoiStore.Load() is null)
            {
                validation.Add("Region of interest is on, but no default ROI is set. Set one in Settings, or override it here.");
            }

            return;
        }

        bool hasAllCorners = validation.RequireValue("SwLat", SwLat, "Southwest latitude is required when overriding the ROI.");
        hasAllCorners &= validation.RequireValue("SwLon", SwLon, "Southwest longitude is required when overriding the ROI.");
        hasAllCorners &= validation.RequireValue("NeLat", NeLat, "Northeast latitude is required when overriding the ROI.");
        hasAllCorners &= validation.RequireValue("NeLon", NeLon, "Northeast longitude is required when overriding the ROI.");

        if (!hasAllCorners)
        {
            return;
        }

        if (!RoiFilter.IsCoordinateValidFormat(SwLat, SwLon, NeLat, NeLon, out string? formatError))
        {
            validation.Add($"ROI override: {formatError}");
            return;
        }

        if (double.TryParse(SwLat, NumberStyles.Float, CultureInfo.InvariantCulture, out double swLat)
            && double.TryParse(SwLon, NumberStyles.Float, CultureInfo.InvariantCulture, out double swLon)
            && double.TryParse(NeLat, NumberStyles.Float, CultureInfo.InvariantCulture, out double neLat)
            && double.TryParse(NeLon, NumberStyles.Float, CultureInfo.InvariantCulture, out double neLon)
            && !RoiFilter.IsCoordinatesRelativePositionValid(swLat, swLon, neLat, neLon, out string? positionError))
        {
            validation.Add($"ROI override: {positionError}");
        }
    }

    // ================= helpers =================

    private static IEnumerable<SubServiceMessageGroup> GroupByLevel(IReadOnlyList<ServiceMessage> messages) =>
        messages
            .GroupBy(m => m.Level)
            .OrderByDescending(g => g.Key)
            .Select(g => new SubServiceMessageGroup(g.Key, g.Select(m => m.Text).ToArray()));

    private static int ResolveCoordinatePrecision()
    {
        string? saved = UserConfigFile.GetValue(PrecisionKey);

        return int.TryParse(saved, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            && parsed is >= 0 and <= 15
            ? parsed
            : 6;
    }

    /// <summary>
    /// The ARTCCs the filter holds, sorted. Before the cycle's list is built there are no toggles
    /// to read, so the saved filter stands in - otherwise a save or a run made before the data
    /// arrives would quietly drop the user's filter.
    /// </summary>
    /// <returns>The selected ARTCC identifiers.</returns>
    private IEnumerable<string> SelectedArtccs() =>
        Artccs.Count > 0
            ? Artccs.Where(a => a.IsSelected).Select(a => a.Artcc)
            : _savedArtccFilter.OrderBy(a => a, StringComparer.OrdinalIgnoreCase);

    private void OnArtccToggled()
    {
        if (_suppressArtccChanges)
        {
            return;
        }

        MarkDirty();
    }

    private void ClearArtccs()
    {
        if (Artccs.All(a => !a.IsSelected))
        {
            return;
        }

        // One dirty check for the whole clear rather than one per toggle.
        _suppressArtccChanges = true;
        try
        {
            foreach (ArtccToggle toggle in Artccs)
            {
                toggle.IsSelected = false;
            }
        }
        finally
        {
            _suppressArtccChanges = false;
        }

        MarkDirty();
    }

    private void SetRoiMode(DepartureRoiMode mode)
    {
        if (_roiMode == mode)
        {
            return;
        }

        _roiMode = mode;
        OnPropertyChanged(nameof(RoiModeAirport));
        OnPropertyChanged(nameof(RoiModeWaypoint));
        MarkDirty();
    }

    /// <summary>
    /// One sentence saying what the run will actually cover once every filter is applied -
    /// procedure type, ARTCCs and the region together - and which outputs that applies to.
    /// </summary>
    /// <param name="selectedArtccs">The ARTCCs ticked (or saved, before the cycle loads); empty for all.</param>
    /// <returns>e.g. "SIDs only, for ZOB and ZNY, at airports inside the region. Applies to the GeoJSON files and the alias file."</returns>
    private string DescribeScope(string[] selectedArtccs)
    {
        string kinds = IncludeObstacleDepartures ? "SIDs and obstacle departures" : "SIDs only (no obstacle departures)";

        string where = selectedArtccs.Length switch
        {
            0 => "for every ARTCC",
            1 => $"for {selectedArtccs[0]}",
            _ => $"for {string.Join(", ", selectedArtccs[..^1])} and {selectedArtccs[^1]}",
        };

        string region = !UseRoi
            ? string.Empty
            : _roiMode == DepartureRoiMode.Waypoint
                ? ", with at least one point inside the region"
                : ", at airports inside the region";

        string outputs = (GenerateGeojson, GenerateAliasFile) switch
        {
            (true, true) => " Applies to the GeoJSON files and the alias file.",
            (true, false) => " Applies to the GeoJSON files.",
            (false, true) => " Applies to the alias file.",
            _ => string.Empty,
        };

        return $"{kinds}, {where}{region}.{outputs}";
    }

    private string DescribeRoi()
    {
        // Only the geographic limit. What the run covers overall - which is also narrowed by the
        // ARTCC and procedure-type choices - is the "Includes" row's job (DescribeScope).
        if (!UseRoi)
        {
            return "Off - no geographic limit";
        }

        string region;

        if (OverrideRoi)
        {
            region = $"Override: SW {SwLat}, {SwLon} / NE {NeLat}, {NeLon}";
        }
        else if (DefaultRoiStore.Load() is { } roi)
        {
            region = $"Default ROI: SW {roi.SwLat:0.####}, {roi.SwLon:0.####} / NE {roi.NeLat:0.####}, {roi.NeLon:0.####}";
        }
        else
        {
            region = "Default ROI: none set";
        }

        string mode = _roiMode == DepartureRoiMode.Waypoint ? "any point inside" : "airports inside";

        return $"{region}; {mode}";
    }

    private void PickRoiOnMap()
    {
        RegionOfInterest? initial = null;

        if (double.TryParse(SwLat, NumberStyles.Float, CultureInfo.InvariantCulture, out double swLat)
            && double.TryParse(SwLon, NumberStyles.Float, CultureInfo.InvariantCulture, out double swLon)
            && double.TryParse(NeLat, NumberStyles.Float, CultureInfo.InvariantCulture, out double neLat)
            && double.TryParse(NeLon, NumberStyles.Float, CultureInfo.InvariantCulture, out double neLon))
        {
            initial = new RegionOfInterest(swLat, swLon, neLat, neLon);
        }

        RegionOfInterest? picked = Views.RoiPickerWindow.Pick(
            System.Windows.Application.Current?.MainWindow, initial, Map.BaseMap.UsStates);

        if (picked is { } roi)
        {
            SwLat = roi.SwLat.ToString("0.######", CultureInfo.InvariantCulture);
            SwLon = roi.SwLon.ToString("0.######", CultureInfo.InvariantCulture);
            NeLat = roi.NeLat.ToString("0.######", CultureInfo.InvariantCulture);
            NeLon = roi.NeLon.ToString("0.######", CultureInfo.InvariantCulture);
            OverrideRoi = true;
        }
    }

    private void OpenOutputFolder()
    {
        if (string.IsNullOrEmpty(LastOutputDirectory) || !Directory.Exists(LastOutputDirectory))
        {
            Toast.Warn("Nothing to open", "Run the AIRAC Service first.");
            return;
        }

        Process.Start(new ProcessStartInfo(LastOutputDirectory) { UseShellExecute = true });
    }

    /// <summary>
    /// Walks up from a written file to the <c>Departure Procedures</c> folder. Walking up by name
    /// rather than a fixed number of levels keeps it right for both the GeoJSON files
    /// (<c>&lt;ARTCC&gt;\&lt;airport&gt;</c>) and the alias file (<c>Alias</c>).
    /// </summary>
    /// <param name="filePath">Any file the run wrote.</param>
    /// <returns>The <c>Departure Procedures</c> folder, or the file's own folder if none is found.</returns>
    private static string? FindOutputRoot(string filePath)
    {
        DirectoryInfo? directory = new FileInfo(filePath).Directory;

        while (directory is not null)
        {
            if (directory.Name.Equals(OutputRootFolderName, StringComparison.OrdinalIgnoreCase))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Path.GetDirectoryName(filePath);
    }

    private static void WriteCrcRow(Dictionary<string, string> settings, string kind, EramClassDefault row)
    {
        string prefix = $"Crc.{row.ClassName}.{kind}";

        settings[$"{prefix}.bcg"] = row.Bcg;
        settings[$"{prefix}.filters"] = row.Filters;

        if (kind is "Line")
        {
            settings[$"{prefix}.style"] = row.Style;
            settings[$"{prefix}.thickness"] = row.Thickness;
        }

        if (kind is "Symbol")
        {
            settings[$"{prefix}.style"] = row.Style;
            settings[$"{prefix}.size"] = row.Size;
        }

        if (kind is "Text")
        {
            settings[$"{prefix}.size"] = row.Size;
        }
    }

    private void LoadCrcRow(string kind, EramClassDefault row)
    {
        string prefix = $"CrcEramPropertyDefaults.{row.ClassName}_{kind}";

        row.Bcg = Get($"{prefix}.bcg") ?? row.Bcg;
        row.Filters = Get($"{prefix}.filters") ?? row.Filters;

        if (kind is "Line" or "Symbol")
        {
            row.Style = Get($"{prefix}.style") ?? row.Style;
        }

        if (kind is "Line")
        {
            row.Thickness = Get($"{prefix}.thickness") ?? row.Thickness;
        }

        if (kind is "Symbol" or "Text")
        {
            row.Size = Get($"{prefix}.size") ?? row.Size;
        }
    }

    private void SaveCrcRow(string kind, EramClassDefault row)
    {
        string prefix = $"CrcEramPropertyDefaults.{row.ClassName}_{kind}";

        Set($"{prefix}.bcg", row.Bcg);
        Set($"{prefix}.filters", row.Filters);

        if (kind is "Line" or "Symbol")
        {
            Set($"{prefix}.style", row.Style);
        }

        if (kind is "Line")
        {
            Set($"{prefix}.thickness", row.Thickness);
        }

        if (kind is "Symbol" or "Text")
        {
            Set($"{prefix}.size", row.Size);
        }
    }

    private void RaisePanelCounts()
    {
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(HasAliasFile));
        OnPropertyChanged(nameof(HasSkipped));
        OnPropertyChanged(nameof(HasAttentionMessages));
        OnPropertyChanged(nameof(HasInfoOnly));
        OnPropertyChanged(nameof(InfoToggleLabel));
    }

    private void RaiseAllSettingProperties()
    {
        OnPropertyChanged(nameof(GenerateGeojson));
        OnPropertyChanged(nameof(GenerateAliasFile));
        OnPropertyChanged(nameof(EmitLines));
        OnPropertyChanged(nameof(EmitSymbols));
        OnPropertyChanged(nameof(EmitText));
        OnPropertyChanged(nameof(IncludeObstacleDepartures));
        OnPropertyChanged(nameof(IncludeFebCustomProperties));
        OnPropertyChanged(nameof(IncludeCrcEramPropertyDefaults));
        OnPropertyChanged(nameof(UseRoi));
        OnPropertyChanged(nameof(RoiModeAirport));
        OnPropertyChanged(nameof(RoiModeWaypoint));
        OnPropertyChanged(nameof(OverrideRoi));
        OnPropertyChanged(nameof(SwLat));
        OnPropertyChanged(nameof(SwLon));
        OnPropertyChanged(nameof(NeLat));
        OnPropertyChanged(nameof(NeLon));
        OnPropertyChanged(nameof(RoiFallbackHint));
    }

    private static HashSet<string> ParseList(string? saved) =>
        string.IsNullOrWhiteSpace(saved)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(
                saved.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                StringComparer.OrdinalIgnoreCase);

    private static string YesNo(bool value) => value ? "Y" : "N";

    private bool GetBool(string key, bool defaultValue)
    {
        string? value = Get(key);

        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase);
    }
}
