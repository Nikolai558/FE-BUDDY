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
using FeBuddy.Core.Models.Services.Airac.Airports;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.General;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Airports</b> sub-service tab inside the AIRAC Service screen: which outputs to write,
/// which GeoJSON files, which FE-Buddy properties, the CRC ERAM defaults, and the ROI override -
/// plus the run result panel.
/// </summary>
/// <remarks>
/// Save, Undo and navigation come from the tab host's action bar; the run is launched by
/// <b>Run AIRAC Service</b> on the Review tab and arrives back here through
/// <see cref="ISubServiceRunTarget"/>.
/// </remarks>
public sealed class AirportsViewModel : SubServiceSettingsViewModel, ISubServiceRunTarget
{
    private const string Node = "Services.AiracService.Airports";
    private const string PrecisionKey = "Services.AiracService.CoordinatePrecision";

    private const string CrcDefaultsIncompleteMessage =
        "CRC ERAM defaults are on but some values are empty. Fill in the marked boxes, or switch CRC ERAM defaults off.";

    private bool _generateGeojson = true;
    private bool _generateAliasFile = true;
    private bool _emitAirportSymbols = true;
    private bool _emitAirportText = true;
    private bool _emitRunwayLines = true;
    private bool _includeFebCustomProperties;
    private bool _includeCrcEramPropertyDefaults = true;
    private bool _overrideRoi;
    private string _swLat = string.Empty;
    private string _swLon = string.Empty;
    private string _neLat = string.Empty;
    private string _neLon = string.Empty;

    private bool _isCycleReady;
    private bool _isRunning;
    private bool _hasRun;
    private string? _runError;
    private string? _progressText;
    private double _elapsedSeconds;
    private int _airportCount;
    private int _airportsInRoiCount;
    private string? _aliasFilePath;
    private int _aliasCommandCount;
    private bool _isInfoExpanded;
    private Stopwatch? _stopwatch;

    /// <summary>Builds the tab and restores its saved settings.</summary>
    public AirportsViewModel()
    {
        FebProperties = new ObservableCollection<FebPropertyToggle>(
            AirportFebPropertyNames.All.Select(entry =>
                new FebPropertyToggle(entry.Property, entry.Name, entry.Description, MarkDirty)));

        AirportSymbolDefaults = new ObservableCollection<EramClassDefault>
        {
            new("Airports", EramFieldKind.Symbol, MarkDirty)
        };

        AirportTextDefaults = new ObservableCollection<EramClassDefault>
        {
            new("Airports", EramFieldKind.Text, MarkDirty)
        };

        RunwayLineDefaults = new ObservableCollection<EramClassDefault>
        {
            new("Runways", EramFieldKind.Line, MarkDirty)
        };

        OpenOutputCommand = new RelayCommand(OpenOutputFolder, () => LastOutputDirectory is not null);
        PickRoiOnMapCommand = new RelayCommand(PickRoiOnMap);
        ToggleInfoCommand = new RelayCommand(() => IsInfoExpanded = !IsInfoExpanded);

        DefaultRoiStore.Changed += (_, _) => OnPropertyChanged(nameof(RoiFallbackHint));

        LoadFromConfig();
    }

    /// <inheritdoc />
    public override string NodePath => Node;

    /// <inheritdoc />
    public override string Title => "Airports";

    // ================= outputs =================

    /// <summary>Whether this run writes GeoJSON for airports and runways.</summary>
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

    /// <summary>Whether this run writes the <c>Airports.txt</c> alias file.</summary>
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

    /// <summary>Emit <c>Airports_Symbols.geojson</c>.</summary>
    public bool EmitAirportSymbols
    {
        get => _emitAirportSymbols;
        set { if (SetProperty(ref _emitAirportSymbols, value)) MarkDirty(); }
    }

    /// <summary>Emit <c>Airports_Text.geojson</c>.</summary>
    public bool EmitAirportText
    {
        get => _emitAirportText;
        set { if (SetProperty(ref _emitAirportText, value)) MarkDirty(); }
    }

    /// <summary>Emit <c>Runways_Lines.geojson</c>.</summary>
    public bool EmitRunwayLines
    {
        get => _emitRunwayLines;
        set { if (SetProperty(ref _emitRunwayLines, value)) MarkDirty(); }
    }

    /// <inheritdoc />
    protected override int EnabledOutputCount =>
        (GenerateGeojson ? 1 : 0) + (GenerateAliasFile ? 1 : 0);

    // ================= properties =================

    /// <summary>Whether airport Features carry the selected <c>feb.*</c> properties.</summary>
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
    public ObservableCollection<FebPropertyToggle> FebProperties { get; }

    // ================= CRC ERAM defaults =================

    /// <summary>Whether the CRC ERAM isDefaults Feature is written into each GeoJSON file.</summary>
    public bool IncludeCrcEramPropertyDefaults
    {
        get => _includeCrcEramPropertyDefaults;
        set { if (SetProperty(ref _includeCrcEramPropertyDefaults, value)) MarkDirty(); }
    }

    /// <summary>CRC symbol defaults for the airport points.</summary>
    public ObservableCollection<EramClassDefault> AirportSymbolDefaults { get; }

    /// <summary>CRC text defaults for the airport labels.</summary>
    public ObservableCollection<EramClassDefault> AirportTextDefaults { get; }

    /// <summary>CRC line defaults for the runway centrelines.</summary>
    public ObservableCollection<EramClassDefault> RunwayLineDefaults { get; }

    // ================= region of interest =================

    /// <summary>Whether Airports uses its own ROI instead of the shared default one.</summary>
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

    /// <summary>What happens to the ROI when the override is off.</summary>
    public string RoiFallbackHint => DefaultRoiStore.Load() is { } roi
        ? $"Using the default ROI: SW {roi.SwLat:0.####}, {roi.SwLon:0.####} / NE {roi.NeLat:0.####}, {roi.NeLon:0.####}"
        : "No default ROI is set, so GeoJSON covers every airport. Set one in Settings, or override it here.";

    /// <summary>Opens the shared ROI picker and copies what the user confirms into the four boxes.</summary>
    public ICommand PickRoiOnMapCommand { get; }

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

    /// <summary>How many airports were built.</summary>
    public int AirportCount { get => _airportCount; private set => SetProperty(ref _airportCount, value); }

    /// <summary>How many airports fell inside the ROI, and so reached the GeoJSON output.</summary>
    public int AirportsInRoiCount { get => _airportsInRoiCount; private set => SetProperty(ref _airportsInRoiCount, value); }

    /// <summary>The GeoJSON files the last run wrote.</summary>
    public ObservableCollection<SubServiceOutputFileRow> Files { get; } = new();

    /// <summary>Whether the last run wrote any GeoJSON.</summary>
    public bool HasFiles => Files.Count > 0;

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

    /// <summary>Opens the folder the last run wrote to.</summary>
    public ICommand OpenOutputCommand { get; }

    private string? LastOutputDirectory { get; set; }

    // ================= parent hooks =================

    /// <summary>Called by the parent whenever AIRAC readiness changes.</summary>
    /// <param name="ready">Whether the AIRAC data is ready.</param>
    public void SetReadiness(bool ready) => IsCycleReady = ready;

    /// <summary>
    /// Called by the parent when the selected cycle's parsed data arrives. Airports has no
    /// cycle-dependent option lists today, so this is a no-op kept for symmetry with the other
    /// sub-service tabs.
    /// </summary>
    /// <param name="data">The parsed NASR data for the selected cycle.</param>
    public void LoadCycleDependentLists(NasrCsvDataCollection data)
    {
    }

    /// <inheritdoc />
    public void BeginRun()
    {
        RunError = null;
        HasRun = false;
        IsRunning = true;
        ProgressText = "Starting…";
        Files.Clear();
        MessageGroups.Clear();
        AirportCount = 0;
        AirportsInRoiCount = 0;
        AliasFilePath = null;
        AliasCommandCount = 0;
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

        if (result.Airports is { } airports)
        {
            AirportCount = airports.AirportCount;
            AirportsInRoiCount = airports.AirportsInRoiCount;

            foreach (string path in airports.GeojsonFilesWritten)
            {
                int count = airports.GeojsonFeatureCountsByFile.TryGetValue(path, out int c) ? c : 0;
                Files.Add(new SubServiceOutputFileRow(Path.GetFileName(path), path, count));
                LastOutputDirectory = Path.GetDirectoryName(path);
            }

            AliasFilePath = airports.AliasFilePath;
            AliasCommandCount = airports.AliasCommandCount;

            if (airports.AliasFilePath is not null)
            {
                LastOutputDirectory ??= Path.GetDirectoryName(airports.AliasFilePath);
            }

            foreach (SubServiceMessageGroup group in GroupByLevel(airports.Messages))
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
        // Same ROI precedence as Airways: an explicit override wins, otherwise the shared
        // default ROI, otherwise no filtering at all.
        RegionOfInterest? fallbackRoi = OverrideRoi ? null : DefaultRoiStore.Load();
        bool filterByRoi = OverrideRoi || fallbackRoi is not null;

        Dictionary<string, string> s = new(StringComparer.OrdinalIgnoreCase)
        {
            ["OutputDirectory"] = outputDirectory,
            ["GenerateGeojson"] = YesNo(GenerateGeojson),
            ["GenerateAliasFile"] = YesNo(GenerateAliasFile),
            ["EmitAirportSymbols"] = YesNo(EmitAirportSymbols),
            ["EmitAirportText"] = YesNo(EmitAirportText),
            ["EmitRunwayLines"] = YesNo(EmitRunwayLines),
            ["IncludeFebCustomProperties"] = YesNo(IncludeFebCustomProperties),
            ["FebProperties"] = string.Join(',', FebProperties.Where(p => p.IsSelected).Select(p => p.Name)),
            ["IncludeCrcEramPropertyDefaults"] = YesNo(IncludeCrcEramPropertyDefaults),
            ["AddFeBuddyOutputFolder"] = YesNo(addFeBuddyOutputFolder),
            ["FilterByRoi"] = YesNo(filterByRoi),
            ["CoordinatePrecision"] = ResolveCoordinatePrecision().ToString(CultureInfo.InvariantCulture),
        };

        if (OverrideRoi)
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
            if (EmitAirportSymbols) WriteCrcRow(s, "Symbol", AirportSymbolDefaults[0]);
            if (EmitAirportText) WriteCrcRow(s, "Text", AirportTextDefaults[0]);
            if (EmitRunwayLines) WriteCrcRow(s, "Line", RunwayLineDefaults[0]);
        }

        return s;
    }

    /// <inheritdoc />
    public override IReadOnlyList<ServiceReviewSection> BuildReviewSummary()
    {
        List<string> geojsonFiles = new();
        if (EmitAirportSymbols) geojsonFiles.Add("Symbols");
        if (EmitAirportText) geojsonFiles.Add("Text");
        if (EmitRunwayLines) geojsonFiles.Add("Runway lines");

        string[] selectedProperties = FebProperties.Where(p => p.IsSelected).Select(p => p.Name).ToArray();

        ServiceReviewRow[] rows =
        {
            new ServiceReviewRow("GeoJSON", GenerateGeojson ? string.Join(", ", geojsonFiles) : "No"),
            new ServiceReviewRow("Alias file", GenerateAliasFile ? "Airports.txt, every airport" : "No"),
            new ServiceReviewRow("FE-Buddy properties",
                IncludeFebCustomProperties && selectedProperties.Length > 0
                    ? string.Join(", ", selectedProperties)
                    : "No"),
            new ServiceReviewRow("CRC ERAM defaults", IncludeCrcEramPropertyDefaults ? "Yes" : "No"),
            new ServiceReviewRow("Region of interest",
                OverrideRoi
                    ? $"Override: SW {SwLat}, {SwLon} / NE {NeLat}, {NeLon}"
                    : RoiFallbackHint),
        };

        return new[] { new ServiceReviewSection("Airports", rows) };
    }

    // ================= save contract =================

    /// <inheritdoc />
    protected override void LoadFromConfig()
    {
        _generateGeojson = GetBool("GenerateGeojson", true);
        _generateAliasFile = GetBool("GenerateAliasFile", true);
        _emitAirportSymbols = GetBool("EmitAirportSymbols", true);
        _emitAirportText = GetBool("EmitAirportText", true);
        _emitRunwayLines = GetBool("EmitRunwayLines", true);
        _includeFebCustomProperties = GetBool("IncludeFebCustomProperties", false);
        _includeCrcEramPropertyDefaults = GetBool("IncludeCrcEramPropertyDefaults", true);

        // Both outputs off would leave the tab in a state its own guard forbids; a hand-edited
        // config is the only way to get here, so fall back to the default rather than honour it.
        if (!_generateGeojson && !_generateAliasFile)
        {
            _generateGeojson = true;
            _generateAliasFile = true;
        }

        string? savedProperties = Get("FebProperties");
        HashSet<string> selected = string.IsNullOrWhiteSpace(savedProperties)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(
                savedProperties.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                StringComparer.OrdinalIgnoreCase);

        foreach (FebPropertyToggle toggle in FebProperties)
        {
            toggle.IsSelected = selected.Contains(toggle.Name);
        }

        _overrideRoi = GetBool("Roi.OverrideDefaultRoi", false);
        _swLat = Get("Roi.OverrideCoordindates.SwLat") ?? string.Empty;
        _swLon = Get("Roi.OverrideCoordindates.SwLon") ?? string.Empty;
        _neLat = Get("Roi.OverrideCoordindates.NeLat") ?? string.Empty;
        _neLon = Get("Roi.OverrideCoordindates.NeLon") ?? string.Empty;

        LoadCrcRow("Symbol", AirportSymbolDefaults[0]);
        LoadCrcRow("Text", AirportTextDefaults[0]);
        LoadCrcRow("Line", RunwayLineDefaults[0]);

        RaiseAllSettingProperties();
        ClearDirty();
    }

    /// <inheritdoc />
    protected override void WriteToConfig()
    {
        Set("GenerateGeojson", YesNo(GenerateGeojson));
        Set("GenerateAliasFile", YesNo(GenerateAliasFile));
        Set("EmitAirportSymbols", YesNo(EmitAirportSymbols));
        Set("EmitAirportText", YesNo(EmitAirportText));
        Set("EmitRunwayLines", YesNo(EmitRunwayLines));
        Set("IncludeFebCustomProperties", YesNo(IncludeFebCustomProperties));
        Set("FebProperties", string.Join(',', FebProperties.Where(p => p.IsSelected).Select(p => p.Name)));
        Set("IncludeCrcEramPropertyDefaults", YesNo(IncludeCrcEramPropertyDefaults));
        Set("Roi.OverrideDefaultRoi", YesNo(OverrideRoi));
        Set("Roi.OverrideCoordindates.SwLat", SwLat);
        Set("Roi.OverrideCoordindates.SwLon", SwLon);
        Set("Roi.OverrideCoordindates.NeLat", NeLat);
        Set("Roi.OverrideCoordindates.NeLon", NeLon);

        SaveCrcRow("Symbol", AirportSymbolDefaults[0]);
        SaveCrcRow("Text", AirportTextDefaults[0]);
        SaveCrcRow("Line", RunwayLineDefaults[0]);
    }

    /// <inheritdoc />
    protected override void Validate(ServiceValidation validation)
    {
        if (GenerateGeojson && !EmitAirportSymbols && !EmitAirportText && !EmitRunwayLines)
        {
            validation.Add(
                "GeoJSON is on but none of its files are selected. Turn on Symbols, Text or Runway lines, "
                + "or switch GeoJSON off.");
        }

        if (IncludeFebCustomProperties && FebProperties.All(p => !p.IsSelected))
        {
            validation.Add("FE-Buddy properties are on but none are selected. Pick at least one, or switch them off.");
        }

        // Only the rows whose file is actually written are needed; a row for a file that is
        // switched off is never read, so it is not held against the user.
        bool crcActive = IncludeCrcEramPropertyDefaults && GenerateGeojson;
        AirportSymbolDefaults[0].IsRequired = crcActive && EmitAirportSymbols;
        AirportTextDefaults[0].IsRequired = crcActive && EmitAirportText;
        RunwayLineDefaults[0].IsRequired = crcActive && EmitRunwayLines;

        if (AirportSymbolDefaults.Concat(AirportTextDefaults).Concat(RunwayLineDefaults)
            .Any(row => row.IsRequired && row.HasMissingValues))
        {
            validation.Add(CrcDefaultsIncompleteMessage);
        }

        if (!OverrideRoi)
        {
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
        OnPropertyChanged(nameof(HasAttentionMessages));
        OnPropertyChanged(nameof(HasInfoOnly));
        OnPropertyChanged(nameof(InfoToggleLabel));
    }

    private void RaiseAllSettingProperties()
    {
        OnPropertyChanged(nameof(GenerateGeojson));
        OnPropertyChanged(nameof(GenerateAliasFile));
        OnPropertyChanged(nameof(EmitAirportSymbols));
        OnPropertyChanged(nameof(EmitAirportText));
        OnPropertyChanged(nameof(EmitRunwayLines));
        OnPropertyChanged(nameof(IncludeFebCustomProperties));
        OnPropertyChanged(nameof(IncludeCrcEramPropertyDefaults));
        OnPropertyChanged(nameof(OverrideRoi));
        OnPropertyChanged(nameof(SwLat));
        OnPropertyChanged(nameof(SwLon));
        OnPropertyChanged(nameof(NeLat));
        OnPropertyChanged(nameof(NeLon));
        OnPropertyChanged(nameof(RoiFallbackHint));
    }

    private static string YesNo(bool value) => value ? "Y" : "N";



    private bool GetBool(string key, bool defaultValue)
    {
        string? value = Get(key);

        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase);
    }


}
