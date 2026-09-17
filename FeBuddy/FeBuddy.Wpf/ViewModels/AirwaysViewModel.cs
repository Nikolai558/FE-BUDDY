using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Input;

using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels.Models;

using FEBuddyLibrary.Helpers;

using FEBuddyLibrary.Models.NASR.CSV;
using FEBuddyLibrary.Models.Services.Airac;
using FEBuddyLibrary.Models.Services.Airac.Airways;
using FEBuddyLibrary.Models.Services.General;
using FEBuddyLibrary.Services.Airac.Airways;
using FEBuddyLibrary.Services.General;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Airways</b> sub-service page, hosted inside the AIRAC Service screen (rule 1.1).
/// Its own settings menu (with the shared Save / Undo contract) plus the run result panel;
/// the run itself is launched by the parent's <b>Run AIRAC Service</b> button
/// (remediation plan Phase 7).
/// </summary>
public sealed class AirwaysViewModel : SubServiceSettingsViewModel
{
    private const string Node = "Services.AiracService.Geojson.Airways";

    private static readonly Regex AirwayIdPattern = new(@"^Airway '([^']+)':", RegexOptions.Compiled);
    private static readonly AirwayAltitudeClass[] AllClasses = { AirwayAltitudeClass.High, AirwayAltitudeClass.Low, AirwayAltitudeClass.Other };

    // ---- settings backing fields ----
    private AirwayGeojsonOutputBy _outputBy = AirwayGeojsonOutputBy.HighLow;
    private bool _emitLines = true;
    private bool _emitSymbols = true;
    private bool _emitText = true;
    private bool _bufferAirwayWaypoints;
    private bool _includeFebCustomProperties;
    private bool _includeAirwayWaypointIds;
    private bool _includeCrcEramPropertyDefaults = true;
    private bool _generateAliasFile = true;
    private bool _aliasRoiAirwaysOnly;
    private bool _splitAtAntimeridian = true;
    private bool _overrideRoi;
    private string _swLat = string.Empty;
    private string _swLon = string.Empty;
    private string _neLat = string.Empty;
    private string _neLon = string.Empty;

    private bool _isCycleReady;

    // ---- run state ----
    private bool _isRunning;
    private bool _hasRun;
    private string? _runError;
    private double _elapsedSeconds;
    private int _airwayCount;
    private int _excludedCount;
    private string? _aliasFilePath;
    private int _aliasLineCount;
    private string? _progressText;
    private bool _isInfoExpanded;
    private Stopwatch? _stopwatch;

    public AirwaysViewModel()
    {
        LineDefaults = BuildClassDefaults(EramFieldKind.Line);
        SymbolDefaults = BuildClassDefaults(EramFieldKind.Symbol);
        TextDefaults = BuildClassDefaults(EramFieldKind.Text);

        ToggleInfoCommand = new RelayCommand(() => IsInfoExpanded = !IsInfoExpanded);
        OpenOutputCommand = new RelayCommand(OpenOutputFolder, () => !string.IsNullOrEmpty(LastOutputDirectory));
        PickRoiOnMapCommand = new RelayCommand(PickRoiOnMap);

        DefaultRoiStore.Changed += (_, _) => OnPropertyChanged(nameof(RoiFallbackHint));

        LoadFromConfig();
    }

    // ================= settings menu =================

    /// <inheritdoc />
    public override string NodePath => Node;

    /// <inheritdoc />
    public override string BreadcrumbTitle => "Airways";

    public IReadOnlyList<AirwayGeojsonOutputBy> OutputByValues { get; } =
        new[] { AirwayGeojsonOutputBy.HighLow, AirwayGeojsonOutputBy.Designation, AirwayGeojsonOutputBy.None };

    public AirwayGeojsonOutputBy OutputBy
    {
        get => _outputBy;
        set { if (SetProperty(ref _outputBy, value)) { MarkDirty(); OnPropertyChanged(nameof(OutputModeHint)); } }
    }

    /// <summary>Multi-line description; both HIGH/LOW and DESIGNATION also emit <c>_Symbols</c> and <c>_Text</c> alongside <c>_Lines</c> (7.4).</summary>
    public string OutputModeHint => OutputBy switch
    {
        AirwayGeojsonOutputBy.None =>
            "Airway data will not be written to GeoJSON.\nThe alias file, if enabled, is unaffected.",
        AirwayGeojsonOutputBy.HighLow =>
            "One file set by altitude:\n" +
            "  • Airways_High  — highest MAA ≥ 18,000 ft\n" +
            "  • Airways_Low   — 0 < MAA < 18,000 ft\n" +
            "  • Airways_Other — neither\n" +
            "Each set is _Lines + _Symbols + _Text.",
        AirwayGeojsonOutputBy.Designation =>
            "One file set per designation (derived from the AWY_ID, e.g. J / V / Q / T / AT):\n" +
            "  Airways_J, Airways_V, Airways_Q, …\n" +
            "Each set is _Lines + _Symbols + _Text.",
        _ => string.Empty,
    };

    public bool EmitLines { get => _emitLines; set { if (SetProperty(ref _emitLines, value)) MarkDirty(); } }
    public bool EmitSymbols { get => _emitSymbols; set { if (SetProperty(ref _emitSymbols, value)) MarkDirty(); } }
    public bool EmitText { get => _emitText; set { if (SetProperty(ref _emitText, value)) MarkDirty(); } }

    public bool BufferAirwayWaypoints { get => _bufferAirwayWaypoints; set { if (SetProperty(ref _bufferAirwayWaypoints, value)) MarkDirty(); } }

    public bool IncludeFebCustomProperties
    {
        get => _includeFebCustomProperties;
        set { if (SetProperty(ref _includeFebCustomProperties, value)) MarkDirty(); }
    }

    /// <summary>Verbatim FE-Buddy Properties description (remediation plan 7.4).</summary>
    public string FebPropertiesDescription =>
        "Include FE-Buddy Properties, when available.\n\n" +
        "Custom Geojson Property fields that increases file size but can be helpful for debugging or " +
        "viewing data in a geojson viewer in order to identify object. Every FE-Buddy property will be " +
        "prefixed with feb.";

    public bool IncludeAirwayWaypointIds { get => _includeAirwayWaypointIds; set { if (SetProperty(ref _includeAirwayWaypointIds, value)) MarkDirty(); } }

    public bool IncludeCrcEramPropertyDefaults { get => _includeCrcEramPropertyDefaults; set { if (SetProperty(ref _includeCrcEramPropertyDefaults, value)) MarkDirty(); } }

    public bool GenerateAliasFile { get => _generateAliasFile; set { if (SetProperty(ref _generateAliasFile, value)) MarkDirty(); } }

    /// <summary><see langword="true"/> = ROI airways only; <see langword="false"/> = all FAA airways (remediation plan 3.5 / 7.5).</summary>
    public bool AliasRoiAirwaysOnly { get => _aliasRoiAirwaysOnly; set { if (SetProperty(ref _aliasRoiAirwaysOnly, value)) MarkDirty(); } }

    public bool SplitAtAntimeridian { get => _splitAtAntimeridian; set { if (SetProperty(ref _splitAtAntimeridian, value)) MarkDirty(); } }

    public bool OverrideRoi
    {
        get => _overrideRoi;
        set { if (SetProperty(ref _overrideRoi, value)) { MarkDirty(); OnPropertyChanged(nameof(RoiFallbackHint)); } }
    }

    public string SwLat { get => _swLat; set { if (SetProperty(ref _swLat, value)) MarkDirty(); } }
    public string SwLon { get => _swLon; set { if (SetProperty(ref _swLon, value)) MarkDirty(); } }
    public string NeLat { get => _neLat; set { if (SetProperty(ref _neLat, value)) MarkDirty(); } }
    public string NeLon { get => _neLon; set { if (SetProperty(ref _neLon, value)) MarkDirty(); } }

    /// <summary>
    /// What the run will actually use when this sub-service isn't overriding the ROI: the
    /// shared Settings ▸ Default Region of Interest if one is set, otherwise a note that the
    /// run will include every airway. Shown under the override checkbox so the fallback isn't a
    /// silent surprise.
    /// </summary>
    public string RoiFallbackHint => DefaultRoiStore.Load() is { } r
        ? $"Not overridden — uses the Settings ▸ Default Region of Interest (SW {r.SwLat:0.####}, {r.SwLon:0.####}  ·  NE {r.NeLat:0.####}, {r.NeLon:0.####})."
        : "Not overridden and no Settings ▸ Default Region of Interest is set — the run will include every airway.";

    /// <summary>Designation include/exclude toggles, built from the selected cycle's parsed airways (7.4). Disabled until readiness.</summary>
    public ObservableCollection<DesignationToggle> Designations { get; } = new();

    public bool IsCycleReady
    {
        get => _isCycleReady;
        private set => SetProperty(ref _isCycleReady, value);
    }

    /// <summary>CRC ERAM Line defaults, one row per altitude class (7.4 - three stacked blocks, no selector).</summary>
    public ObservableCollection<EramClassDefault> LineDefaults { get; }
    public ObservableCollection<EramClassDefault> SymbolDefaults { get; }
    public ObservableCollection<EramClassDefault> TextDefaults { get; }

    public ICommand ToggleInfoCommand { get; }
    public ICommand OpenOutputCommand { get; }
    public ICommand PickRoiOnMapCommand { get; }

    // ================= run result panel =================

    public bool IsRunning { get => _isRunning; private set { if (SetProperty(ref _isRunning, value)) OnPropertyChanged(nameof(ShowPanel)); } }
    public bool HasRun { get => _hasRun; private set { if (SetProperty(ref _hasRun, value)) { OnPropertyChanged(nameof(ShowPanel)); OnPropertyChanged(nameof(RunSucceeded)); OnPropertyChanged(nameof(RunFailed)); } } }
    public bool ShowPanel => IsRunning || HasRun;

    public string? RunError
    {
        get => _runError;
        private set { if (SetProperty(ref _runError, value)) { OnPropertyChanged(nameof(RunSucceeded)); OnPropertyChanged(nameof(RunFailed)); } }
    }

    public bool RunSucceeded => HasRun && RunError is null;
    public bool RunFailed => HasRun && RunError is not null;

    public string? ProgressText { get => _progressText; private set => SetProperty(ref _progressText, value); }

    public double ElapsedSeconds { get => _elapsedSeconds; private set { if (SetProperty(ref _elapsedSeconds, value)) OnPropertyChanged(nameof(ElapsedText)); } }
    public string ElapsedText => $"{ElapsedSeconds:0.0}s";

    public int AirwayCount { get => _airwayCount; private set => SetProperty(ref _airwayCount, value); }
    public int ExcludedCount { get => _excludedCount; private set { if (SetProperty(ref _excludedCount, value)) OnPropertyChanged(nameof(HasExcluded)); } }
    public bool HasExcluded => ExcludedCount > 0;

    public ObservableCollection<AirwaysOutputFileRow> Files { get; } = new();
    public bool HasFiles => Files.Count > 0;

    public string? AliasFilePath { get => _aliasFilePath; private set { if (SetProperty(ref _aliasFilePath, value)) OnPropertyChanged(nameof(HasAliasFile)); } }
    public bool HasAliasFile => !string.IsNullOrEmpty(AliasFilePath);
    public int AliasLineCount { get => _aliasLineCount; private set => SetProperty(ref _aliasLineCount, value); }

    /// <summary>Run messages grouped by airway, each carrying its highest level (remediation plan 3.8).</summary>
    public ObservableCollection<AirwaysMessageGroup> MessageGroups { get; } = new();
    public bool HasWarnings => MessageGroups.Any(g => g.Level >= LogLevel.Warning);
    public bool HasInfoOnly => MessageGroups.Count > 0 && MessageGroups.All(g => g.Level < LogLevel.Warning);
    public int WarningGroupCount => MessageGroups.Count(g => g.Level >= LogLevel.Warning);
    public int InfoGroupCount => MessageGroups.Count(g => g.Level < LogLevel.Warning);

    public bool IsInfoExpanded { get => _isInfoExpanded; set { if (SetProperty(ref _isInfoExpanded, value)) OnPropertyChanged(nameof(InfoToggleLabel)); } }
    public string InfoToggleLabel => IsInfoExpanded ? "Hide info messages" : $"Show {InfoGroupCount} info group(s)";

    private string? LastOutputDirectory { get; set; }

    // ================= parent hooks =================

    /// <summary>Called by the parent whenever AIRAC readiness changes.</summary>
    /// <param name="ready">Whether the AIRAC data is ready.</param>
    public void SetReadiness(bool ready) => IsCycleReady = ready;

    /// <summary>Builds the designation toggle list from the selected cycle's parsed airways (7.4).</summary>
    /// <param name="data">The parsed NASR data for the selected cycle.</param>
    public void LoadCycleDependentLists(NasrCsvDataCollection data)
    {
        HashSet<string> excluded = ParseExcludedFromConfig();

        string[] designations = (data.Awy?.AwyBase ?? new())
            .Select(a => AirwayClassifier.DeriveDesignation(a.AwyId))
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Designations.Clear();
        foreach (string d in designations)
        {
            Designations.Add(new DesignationToggle(d, included: !excluded.Contains(d), MarkDirty));
        }
    }

    /// <summary>Resets the result panel for a new run.</summary>
    public void BeginRun()
    {
        RunError = null;
        HasRun = false;
        IsRunning = true;
        ProgressText = "Starting…";
        Files.Clear();
        MessageGroups.Clear();
        AirwayCount = 0;
        ExcludedCount = 0;
        AliasFilePath = null;
        AliasLineCount = 0;
        IsInfoExpanded = false;
        RaisePanelCounts();

        _stopwatch = Stopwatch.StartNew();
        ElapsedSeconds = 0;
    }

    /// <summary>Updates the in-panel progress line.</summary>
    /// <param name="message">The progress message.</param>
    public void ReportProgress(string message) => ProgressText = message;

    /// <summary>Renders a finished <see cref="AiracServiceResult"/> into the panel.</summary>
    /// <param name="result">The aggregated AIRAC Service result.</param>
    public void ApplyAiracResult(AiracServiceResult result)
    {
        _stopwatch?.Stop();
        ElapsedSeconds = _stopwatch?.Elapsed.TotalSeconds ?? 0;
        IsRunning = false;
        HasRun = true;
        ProgressText = null;

        ExcludedCount = result.ExcludedAirwayIds.Count;

        AirwayServiceResult? airways = result.Airways;
        if (airways is not null)
        {
            AirwayCount = airways.AirwayCount;

            foreach (string path in airways.GeojsonFilesWritten)
            {
                int count = airways.GeojsonFeatureCountsByFile.TryGetValue(path, out int c) ? c : 0;
                Files.Add(new AirwaysOutputFileRow(Path.GetFileName(path), path, count));
                LastOutputDirectory = Path.GetDirectoryName(path);
            }

            AliasFilePath = airways.AliasFilePath;
            AliasLineCount = airways.AliasAirwayLineCount;
            if (airways.AliasFilePath is not null)
            {
                LastOutputDirectory ??= Path.GetDirectoryName(airways.AliasFilePath);
            }
        }

        foreach (AirwaysMessageGroup group in GroupMessages(result.Messages))
        {
            MessageGroups.Add(group);
        }

        RaisePanelCounts();
    }

    /// <summary>Marks the run failed with an error message.</summary>
    /// <param name="error">The failure message.</param>
    public void FailRun(string error)
    {
        _stopwatch?.Stop();
        ElapsedSeconds = _stopwatch?.Elapsed.TotalSeconds ?? 0;
        IsRunning = false;
        HasRun = true;
        RunError = error;
        ProgressText = null;
    }

    /// <summary>Builds the raw Airways settings block for <see cref="AiracServiceSettings.Airways"/>.</summary>
    /// <param name="outputDirectory">The resolved output directory.</param>
    /// <param name="addFeBuddyOutputFolder">Whether to wrap output in a <c>FE-Buddy_Output</c> folder.</param>
    /// <returns>The settings dictionary.</returns>
    public IReadOnlyDictionary<string, string> BuildSettingsBlock(string outputDirectory, bool addFeBuddyOutputFolder)
    {
        // Precedence: an explicit override wins; otherwise fall back to Settings' shared
        // Default ROI; otherwise no ROI filtering at all. Previously a run only ever looked at
        // OverrideRoi, so the Default ROI silently did nothing unless the user re-entered the
        // same box under "Override the default ROI for Airways".
        RegionOfInterest? fallbackRoi = OverrideRoi ? null : DefaultRoiStore.Load();
        bool filterByRoi = OverrideRoi || fallbackRoi is not null;

        Dictionary<string, string> s = new(StringComparer.OrdinalIgnoreCase)
        {
            ["OutputDirectory"] = outputDirectory,
            ["OutputBy"] = OutputBy.ToString(),
            ["EmitLines"] = YesNo(EmitLines),
            ["EmitSymbols"] = YesNo(EmitSymbols),
            ["EmitText"] = YesNo(EmitText),
            ["BufferAirwayWaypoints"] = YesNo(BufferAirwayWaypoints),
            ["IncludeFebCustomProperties"] = YesNo(IncludeFebCustomProperties),
            ["IncludeAirwayWaypointIds"] = YesNo(IncludeAirwayWaypointIds),
            ["IncludeCrcEramPropertyDefaults"] = YesNo(IncludeCrcEramPropertyDefaults),
            ["GenerateAliasFile"] = YesNo(GenerateAliasFile),
            ["AliasRoiScope"] = AliasRoiAirwaysOnly ? "RoiAirways" : "All",
            ["SplitAtAntimeridian"] = YesNo(SplitAtAntimeridian),
            ["AddFeBuddyOutputFolder"] = YesNo(addFeBuddyOutputFolder),
            ["FilterByRoi"] = YesNo(filterByRoi),
            ["ExcludedDesignations"] = string.Join(',', Designations.Where(d => !d.Included).Select(d => d.Designation)),
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

        if (IncludeCrcEramPropertyDefaults)
        {
            WriteCrcBlock(s, "Line", LineDefaults);
            WriteCrcBlock(s, "Symbol", SymbolDefaults);
            WriteCrcBlock(s, "Text", TextDefaults);
        }

        return s;
    }

    // ================= save contract =================

    /// <inheritdoc />
    protected override void LoadFromConfig()
    {
        _outputBy = Enum.TryParse(Get("OutputBy"), true, out AirwayGeojsonOutputBy by) ? by : AirwayGeojsonOutputBy.HighLow;
        _emitLines = GetBool("EmitLines", true);
        _emitSymbols = GetBool("EmitSymbols", true);
        _emitText = GetBool("EmitText", true);
        _bufferAirwayWaypoints = GetBool("BufferAirwayWaypoints", false);
        _includeFebCustomProperties = GetBool("IncludeFebCustomProperties", false);
        _includeAirwayWaypointIds = GetBool("IncludeAirwayWaypointIds", false);
        _includeCrcEramPropertyDefaults = GetBool("IncludeCrcEramPropertyDefaults", true);
        _generateAliasFile = GetBool("GenerateAliasFile", true);
        _aliasRoiAirwaysOnly = string.Equals(Get("AliasRoiScope"), "RoiAirways", StringComparison.OrdinalIgnoreCase);
        _splitAtAntimeridian = GetBool("SplitAtAntimeridian", true);
        _overrideRoi = GetBool("Roi.OverrideDefaultRoi", false);
        _swLat = Get("Roi.OverrideCoordindates.SwLat") ?? string.Empty;
        _swLon = Get("Roi.OverrideCoordindates.SwLon") ?? string.Empty;
        _neLat = Get("Roi.OverrideCoordindates.NeLat") ?? string.Empty;
        _neLon = Get("Roi.OverrideCoordindates.NeLon") ?? string.Empty;

        LoadCrcBlock("Line", LineDefaults);
        LoadCrcBlock("Symbol", SymbolDefaults);
        LoadCrcBlock("Text", TextDefaults);

        // Re-apply the excluded set to any already-built designation toggles.
        HashSet<string> excluded = ParseExcludedFromConfig();
        foreach (DesignationToggle toggle in Designations)
        {
            toggle.Included = !excluded.Contains(toggle.Designation);
        }

        RaiseAllSettingProperties();
        IsDirty = false;
    }

    /// <inheritdoc />
    protected override void WriteToConfig()
    {
        Set("OutputBy", OutputBy.ToString());
        Set("EmitLines", YesNo(EmitLines));
        Set("EmitSymbols", YesNo(EmitSymbols));
        Set("EmitText", YesNo(EmitText));
        Set("BufferAirwayWaypoints", YesNo(BufferAirwayWaypoints));
        Set("IncludeFebCustomProperties", YesNo(IncludeFebCustomProperties));
        Set("IncludeAirwayWaypointIds", YesNo(IncludeAirwayWaypointIds));
        Set("IncludeCrcEramPropertyDefaults", YesNo(IncludeCrcEramPropertyDefaults));
        Set("GenerateAliasFile", YesNo(GenerateAliasFile));
        Set("AliasRoiScope", AliasRoiAirwaysOnly ? "RoiAirways" : "All");
        Set("SplitAtAntimeridian", YesNo(SplitAtAntimeridian));
        Set("ExcludedDesignations", string.Join(',', Designations.Where(d => !d.Included).Select(d => d.Designation)));
        Set("Roi.OverrideDefaultRoi", YesNo(OverrideRoi));
        Set("Roi.OverrideCoordindates.SwLat", SwLat);
        Set("Roi.OverrideCoordindates.SwLon", SwLon);
        Set("Roi.OverrideCoordindates.NeLat", NeLat);
        Set("Roi.OverrideCoordindates.NeLon", NeLon);

        SaveCrcBlock("Line", LineDefaults);
        SaveCrcBlock("Symbol", SymbolDefaults);
        SaveCrcBlock("Text", TextDefaults);
    }

    /// <inheritdoc />
    protected override string? Validate()
    {
        if (OutputBy != AirwayGeojsonOutputBy.None && !EmitLines && !EmitSymbols && !EmitText)
        {
            return "Lines, Symbols and Text are all off, but Output is not \"None\". Turn at least one back on, or set Output to \"None\".";
        }

        if (OverrideRoi)
        {
            if (!RoiFilter.IsCoordinateValidFormat(SwLat, SwLon, NeLat, NeLon, out string? formatError))
            {
                return $"ROI override: {formatError}";
            }

            if (double.TryParse(SwLat, NumberStyles.Float, CultureInfo.InvariantCulture, out double swLat)
                && double.TryParse(SwLon, NumberStyles.Float, CultureInfo.InvariantCulture, out double swLon)
                && double.TryParse(NeLat, NumberStyles.Float, CultureInfo.InvariantCulture, out double neLat)
                && double.TryParse(NeLon, NumberStyles.Float, CultureInfo.InvariantCulture, out double neLon)
                && !RoiFilter.IsCoordinatesRelativePositionValid(swLat, swLon, neLat, neLon, out string? positionError))
            {
                return $"ROI override: {positionError}";
            }
        }

        return null;
    }

    // ================= helpers =================

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

    private static string YesNo(bool value) => value ? "Y" : "N";

    private string? Get(string key) => UserConfigFile.GetValue($"{Node}.{key}");

    private bool GetBool(string key, bool fallback)
    {
        string? v = Get(key);
        return v switch
        {
            null or "" => fallback,
            _ => v.Equals("Y", StringComparison.OrdinalIgnoreCase) || v.Equals("true", StringComparison.OrdinalIgnoreCase),
        };
    }

    private void Set(string key, string value) => UserConfigFile.TrySetValue($"{Node}.{key}", value);

    private HashSet<string> ParseExcludedFromConfig() =>
        (Get("ExcludedDesignations") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(d => d.ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private ObservableCollection<EramClassDefault> BuildClassDefaults(EramFieldKind kind)
    {
        (string bcg, string filters, string style)[] seed = kind switch
        {
            EramFieldKind.Line => new[] { ("3", "3", "solid"), ("2", "2", "shortDashed"), ("1", "1", "longDashed") },
            EramFieldKind.Symbol => new[] { ("3", "3", "vor"), ("2", "2", "vor"), ("1", "1", "otherWaypoints") },
            _ => new[] { ("3", "3", ""), ("2", "2", ""), ("1", "1", "") },
        };

        ObservableCollection<EramClassDefault> rows = new();
        for (int i = 0; i < AllClasses.Length; i++)
        {
            rows.Add(new EramClassDefault(AllClasses[i].ToString(), kind, seed[i].bcg, seed[i].filters, seed[i].style, MarkDirty));
        }

        return rows;
    }

    private void LoadCrcBlock(string kind, ObservableCollection<EramClassDefault> rows)
    {
        foreach (EramClassDefault row in rows)
        {
            string p = $"CrcEramPropertyDefaults.{kind}s.Airway_{row.ClassName}_{kind}s";
            row.Bcg = Get($"{p}.bcg") ?? row.Bcg;
            row.Filters = Get($"{p}.filters") ?? row.Filters;
            row.Style = Get($"{p}.style") ?? row.Style;
            row.Thickness = Get($"{p}.thickness") ?? row.Thickness;
            row.Size = Get($"{p}.size") ?? row.Size;
        }
    }

    private void SaveCrcBlock(string kind, ObservableCollection<EramClassDefault> rows)
    {
        foreach (EramClassDefault row in rows)
        {
            string p = $"CrcEramPropertyDefaults.{kind}s.Airway_{row.ClassName}_{kind}s";
            Set($"{p}.bcg", row.Bcg);
            Set($"{p}.filters", row.Filters);
            if (kind is "Line") Set($"{p}.style", row.Style);
            if (kind is "Symbol") { Set($"{p}.style", row.Style); Set($"{p}.size", row.Size); }
            if (kind is "Line") Set($"{p}.thickness", row.Thickness);
            if (kind is "Text") { Set($"{p}.size", row.Size); }
        }
    }

    private static void WriteCrcBlock(Dictionary<string, string> s, string kind, ObservableCollection<EramClassDefault> rows)
    {
        foreach (EramClassDefault row in rows)
        {
            string p = $"Crc.{row.ClassName}.{kind}";
            s[$"{p}.bcg"] = row.Bcg;
            s[$"{p}.filters"] = row.Filters;
            if (kind is "Line") { s[$"{p}.style"] = row.Style; s[$"{p}.thickness"] = row.Thickness; }
            if (kind is "Symbol") { s[$"{p}.style"] = row.Style; s[$"{p}.size"] = row.Size; }
            if (kind is "Text") { s[$"{p}.size"] = row.Size; }
        }
    }

    private void RaiseAllSettingProperties()
    {
        foreach (string name in new[]
        {
            nameof(OutputBy), nameof(OutputModeHint), nameof(EmitLines), nameof(EmitSymbols), nameof(EmitText),
            nameof(BufferAirwayWaypoints), nameof(IncludeFebCustomProperties), nameof(IncludeAirwayWaypointIds),
            nameof(IncludeCrcEramPropertyDefaults), nameof(GenerateAliasFile), nameof(AliasRoiAirwaysOnly),
            nameof(SplitAtAntimeridian), nameof(OverrideRoi), nameof(SwLat), nameof(SwLon), nameof(NeLat), nameof(NeLon),
        })
        {
            OnPropertyChanged(name);
        }
    }

    private void RaisePanelCounts()
    {
        foreach (string name in new[]
        {
            nameof(HasFiles), nameof(HasWarnings), nameof(HasInfoOnly), nameof(WarningGroupCount),
            nameof(InfoGroupCount), nameof(InfoToggleLabel), nameof(HasAliasFile), nameof(HasExcluded),
        })
        {
            OnPropertyChanged(name);
        }
    }

    private static IEnumerable<AirwaysMessageGroup> GroupMessages(IReadOnlyList<ServiceMessage> messages) =>
        messages
            .GroupBy(m =>
            {
                Match match = AirwayIdPattern.Match(m.Text);
                return match.Success ? match.Groups[1].Value : "General";
            })
            .Select(g => new AirwaysMessageGroup(
                g.Key,
                g.Max(m => m.Level),
                g.Select(m => m.Text).ToList()))
            .OrderByDescending(g => g.Level)
            .ThenBy(g => g.AirwayId, StringComparer.OrdinalIgnoreCase);

}
