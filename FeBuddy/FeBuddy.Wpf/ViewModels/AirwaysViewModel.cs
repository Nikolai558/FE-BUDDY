using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Input;

using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels.Models;

using FeBuddy.Core.Helpers;

using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac;
using FeBuddy.Core.Models.Services.Airac.Airways;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.Airac.Airways;
using FeBuddy.Core.Services.General;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Airways</b> sub-service tab inside the AIRAC Service screen: its settings menu, with
/// the shared Save / Undo contract. Save, Undo and navigation come from the tab host's action
/// bar; the run is launched by <b>Run AIRAC Service</b> on the Review tab, and its results are
/// shown there, described by this tab through <see cref="ISubServiceRunTarget"/>.
/// </summary>
public sealed class AirwaysViewModel : SubServiceSettingsViewModel, ISubServiceRunTarget,
    IFebPropertySettings, ICrcDefaultsSettings, IRoiOverrideSettings
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
    private bool _includeCrcLineDefaults = true;
    private bool _includeCrcSymbolDefaults = true;
    private bool _includeCrcTextDefaults = true;
    private bool _generateAliasFile = true;
    private bool _aliasRoiAirwaysOnly;
    private bool _splitAtAntimeridian = true;
    private bool _overrideRoi;
    private string _swLat = string.Empty;
    private string _swLon = string.Empty;
    private string _neLat = string.Empty;
    private string _neLon = string.Empty;

    private bool _isCycleReady;

    public AirwaysViewModel()
    {
        FebProperties = new ObservableCollection<FebPropertyToggle>(
            AirwayFebPropertyNames.All.Select(entry =>
                new FebPropertyToggle(entry.Name, entry.Description, MarkDirty)));

        LineDefaults = BuildClassDefaults(EramFieldKind.Line);
        SymbolDefaults = BuildClassDefaults(EramFieldKind.Symbol);
        TextDefaults = BuildClassDefaults(EramFieldKind.Text);

        PickRoiOnMapCommand = new RelayCommand(PickRoiOnMap);

        DefaultRoiStore.Changed += (_, _) => OnPropertyChanged(nameof(RoiFallbackHint));

        LoadFromConfig();
    }

    // ================= settings menu =================

    /// <inheritdoc />
    public override string NodePath => Node;

    /// <inheritdoc />
    public override string Title => "Airways";

    public IReadOnlyList<AirwayGeojsonOutputBy> OutputByValues { get; } =
        new[] { AirwayGeojsonOutputBy.HighLow, AirwayGeojsonOutputBy.Designation, AirwayGeojsonOutputBy.None };

    public AirwayGeojsonOutputBy OutputBy
    {
        get => _outputBy;
        set
        {
            // Choosing "None" switches the GeoJSON output off, so it is guarded like any other
            // output: it cannot be the one that leaves this sub-service producing nothing.
            if (value == AirwayGeojsonOutputBy.None && !GenerateAliasFile && !CanTurnOffOutput())
            {
                RestoreRejectedToggle(nameof(OutputBy));
                return;
            }

            if (SetProperty(ref _outputBy, value))
            {
                MarkDirty();
                OnPropertyChanged(nameof(OutputModeHint));
                OnPropertyChanged(nameof(IsGeojsonOutputOn));
            }
        }
    }

    /// <summary>Whether any GeoJSON is written, i.e. <see cref="OutputBy"/> is not <c>None</c>.</summary>
    public bool IsGeojsonOutputOn => OutputBy != AirwayGeojsonOutputBy.None;

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

    /// <summary>One toggle per available <c>feb.*</c> property.</summary>
    public ObservableCollection<FebPropertyToggle> FebProperties { get; }

    /// <summary>Whether the CRC ERAM isDefaults Feature is written into each <c>_Lines</c> file.</summary>
    public bool IncludeCrcLineDefaults { get => _includeCrcLineDefaults; set { if (SetProperty(ref _includeCrcLineDefaults, value)) MarkDirty(); } }

    /// <summary>Whether the CRC ERAM isDefaults Feature is written into each <c>_Symbols</c> file.</summary>
    public bool IncludeCrcSymbolDefaults { get => _includeCrcSymbolDefaults; set { if (SetProperty(ref _includeCrcSymbolDefaults, value)) MarkDirty(); } }

    /// <summary>Whether the CRC ERAM isDefaults Feature is written into each <c>_Text</c> file.</summary>
    public bool IncludeCrcTextDefaults { get => _includeCrcTextDefaults; set { if (SetProperty(ref _includeCrcTextDefaults, value)) MarkDirty(); } }

    public bool GenerateAliasFile
    {
        get => _generateAliasFile;
        set
        {
            if (!value && !CanTurnOffOutput())
            {
                // The value never changed, but the control already did - put it back.
                RestoreRejectedToggle(nameof(GenerateAliasFile));
                return;
            }

            if (SetProperty(ref _generateAliasFile, value))
            {
                MarkDirty();
            }
        }
    }

    /// <inheritdoc />
    protected override int EnabledOutputCount =>
        (OutputBy == AirwayGeojsonOutputBy.None ? 0 : 1) + (GenerateAliasFile ? 1 : 0);

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
        ? $"Using the default ROI: SW {r.SwLat:0.####}, {r.SwLon:0.####} / NE {r.NeLat:0.####}, {r.NeLon:0.####}"
        : "No default ROI is set, so every airway is included. Set one in Settings, or override it here.";

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

    public ICommand PickRoiOnMapCommand { get; }

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

        // The list was empty when this tab snapshotted itself at construction, so the snapshot
        // says "nothing excluded" while the config may well exclude several. Re-take it now the
        // toggles reflect what is actually saved.
        ResyncSavedState();
    }

    /// <inheritdoc />
    public SubServiceRunResult? DescribeRunResult(AiracServiceResult result)
    {
        if (result.Airways is not { } airways)
        {
            return null;
        }

        string summary = $"{airways.AirwayCount:N0} airways";

        if (result.ExcludedAirwayIds.Count > 0)
        {
            summary += $", {result.ExcludedAirwayIds.Count:N0} excluded";
        }

        if (airways.AliasFilePath is not null)
        {
            summary += $", Airways.txt: {airways.AliasAirwayLineCount:N0} alias line(s)";
        }

        if (airways.GeojsonFilesWritten.Count > 0)
        {
            summary += $", {airways.GeojsonFilesWritten.Count:N0} GeoJSON file(s)";
        }

        // Grouped by the airway each message names ("Airway 'T312': ..."), so a run does not
        // read as one flat wall of text; anything that names no airway goes under "General".
        return new SubServiceRunResult(Title, summary, airways.Messages, m =>
        {
            Match match = AirwayIdPattern.Match(m.Text);
            return match.Success ? match.Groups[1].Value : "General";
        });
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
            ["FebProperties"] = string.Join(',', FebProperties.Where(p => p.IsSelected).Select(p => p.Name)),
            ["IncludeCrcLineDefaults"] = YesNo(IncludeCrcLineDefaults),
            ["IncludeCrcSymbolDefaults"] = YesNo(IncludeCrcSymbolDefaults),
            ["IncludeCrcTextDefaults"] = YesNo(IncludeCrcTextDefaults),
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

        // Same rule as the parser: only the kinds being written, with their Include box ticked,
        // carry defaults.
        if (IsGeojsonOutputOn)
        {
            if (IncludeCrcLineDefaults && EmitLines) WriteCrcBlock(s, "Line", LineDefaults);
            if (IncludeCrcSymbolDefaults && EmitSymbols) WriteCrcBlock(s, "Symbol", SymbolDefaults);
            if (IncludeCrcTextDefaults && EmitText) WriteCrcBlock(s, "Text", TextDefaults);
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
        _includeCrcLineDefaults = GetBool("IncludeCrcLineDefaults", true);
        _includeCrcSymbolDefaults = GetBool("IncludeCrcSymbolDefaults", true);
        _includeCrcTextDefaults = GetBool("IncludeCrcTextDefaults", true);
        _generateAliasFile = GetBool("GenerateAliasFile", true);
        _aliasRoiAirwaysOnly = string.Equals(Get("AliasRoiScope"), "RoiAirways", StringComparison.OrdinalIgnoreCase);
        _splitAtAntimeridian = GetBool("SplitAtAntimeridian", true);
        _overrideRoi = GetBool("Roi.OverrideDefaultRoi", false);
        _swLat = Get("Roi.OverrideCoordindates.SwLat") ?? string.Empty;
        _swLon = Get("Roi.OverrideCoordindates.SwLon") ?? string.Empty;
        _neLat = Get("Roi.OverrideCoordindates.NeLat") ?? string.Empty;
        _neLon = Get("Roi.OverrideCoordindates.NeLon") ?? string.Empty;

        HashSet<string> selectedFebProperties = (Get("FebProperties") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (FebPropertyToggle toggle in FebProperties)
        {
            toggle.IsSelected = selectedFebProperties.Contains(toggle.Name);
        }

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
        ClearDirty();
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
        Set("FebProperties", string.Join(',', FebProperties.Where(p => p.IsSelected).Select(p => p.Name)));
        Set("IncludeCrcLineDefaults", YesNo(IncludeCrcLineDefaults));
        Set("IncludeCrcSymbolDefaults", YesNo(IncludeCrcSymbolDefaults));
        Set("IncludeCrcTextDefaults", YesNo(IncludeCrcTextDefaults));
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
    protected override void Validate(ServiceValidation validation)
    {
        if (OutputBy != AirwayGeojsonOutputBy.None && !EmitLines && !EmitSymbols && !EmitText)
        {
            validation.Add("Lines, Symbols and Text are all off, but Output is not \"None\". Turn at least one back on, or set Output to \"None\".");
        }

        if (IncludeFebCustomProperties && FebProperties.All(p => !p.IsSelected))
        {
            validation.Add("FE-Buddy properties are on but none are selected. Pick at least one, or switch them off.");
        }

        // Only the blocks whose file is actually written and whose Include box is ticked are
        // needed; any other block is never read, so it is not held against the user.
        MarkRequired(LineDefaults, IsGeojsonOutputOn && EmitLines && IncludeCrcLineDefaults);
        MarkRequired(SymbolDefaults, IsGeojsonOutputOn && EmitSymbols && IncludeCrcSymbolDefaults);
        MarkRequired(TextDefaults, IsGeojsonOutputOn && EmitText && IncludeCrcTextDefaults);

        if (LineDefaults.Concat(SymbolDefaults).Concat(TextDefaults).Any(row => row.IsRequired && row.HasMissingValues))
        {
            validation.Add("Some CRC ERAM default values are empty or invalid. Fix the marked boxes, or untick Include on that panel.");
        }

        if (!OverrideRoi)
        {
            return;
        }

        // Each corner is reported against its own box so the empty one highlights. The two
        // library checks below look at the set as a whole, so they stay tab-level messages -
        // and they only make sense once all four boxes actually have something in them.
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

    // ================= review =================

    /// <summary>
    /// This tab's contribution to the Review tab: one section spelling out, in plain words, what
    /// the current settings will actually produce, so the user can check the run without walking
    /// back through every control.
    /// </summary>
    /// <returns>A single <b>Airways</b> section, its rows in display order.</returns>
    public override IReadOnlyList<ServiceReviewSection> BuildReviewSummary()
    {
        List<string> fileKinds = new();
        if (EmitLines) fileKinds.Add("Lines");
        if (EmitSymbols) fileKinds.Add("Symbols");
        if (EmitText) fileKinds.Add("Text");

        List<string> crcDefaults = new();
        if (IsGeojsonOutputOn)
        {
            if (IncludeCrcLineDefaults && EmitLines) crcDefaults.Add("Lines");
            if (IncludeCrcSymbolDefaults && EmitSymbols) crcDefaults.Add("Symbols");
            if (IncludeCrcTextDefaults && EmitText) crcDefaults.Add("Text");
        }

        string[] selectedFebProperties = FebProperties.Where(p => p.IsSelected).Select(p => p.Name).ToArray();
        string febProperties = IncludeFebCustomProperties && selectedFebProperties.Length > 0
            ? string.Join(", ", selectedFebProperties)
            : "No";

        string aliasFile = GenerateAliasFile
            ? AliasRoiAirwaysOnly ? "Airways.txt, ROI airways only" : "Airways.txt, all FAA airways"
            : "No";

        // Before a cycle is parsed the toggle list is empty, which would make the review claim
        // "none excluded" when the user has exclusions saved. Fall back to what is on disk.
        string excluded = Designations.Count > 0
            ? string.Join(", ", Designations.Where(d => !d.Included).Select(d => d.Designation))
            : string.Join(", ", ParseExcludedFromConfig().OrderBy(d => d, StringComparer.OrdinalIgnoreCase));

        // Only the geographic limit; the "Includes" row states what is covered once the
        // designation exclusions are applied as well.
        RegionOfInterest? defaultRoi = DefaultRoiStore.Load();
        bool roiActive = OverrideRoi || defaultRoi is not null;

        string regionOfInterest = OverrideRoi
            ? $"Override: SW {SwLat}, {SwLon} / NE {NeLat}, {NeLon}"
            : defaultRoi is { } roi
                ? $"Default ROI: SW {roi.SwLat:0.####}, {roi.SwLon:0.####} / NE {roi.NeLat:0.####}, {roi.NeLon:0.####}"
                : "None set - no geographic limit";

        // Name what is covered - never "all except"; the exclusions have their own row. The
        // designations come from the parsed cycle, so until it is loaded there is nothing to name.
        string[] included = Designations.Where(d => d.Included).Select(d => d.Designation).ToArray();
        string covered = Designations.Count == 0
            ? "Waiting for the cycle's airway list"
            : included.Length > 0 ? $"{string.Join(", ", included)} airways" : "No airways";

        string includes = covered
            + (roiActive ? ". GeoJSON: only the airways crossing the region, clipped to it." : ".");

        ServiceReviewRow[] rows =
        {
            new ServiceReviewRow("GeoJSON output", OutputBy.ToString()),
            new ServiceReviewRow("File kinds", fileKinds.Count > 0 ? string.Join(", ", fileKinds) : "none"),
            new ServiceReviewRow("FE-Buddy properties", febProperties),
            new ServiceReviewRow("CRC ERAM defaults", crcDefaults.Count > 0 ? string.Join(", ", crcDefaults) : "None"),
            new ServiceReviewRow("Includes", includes),
            new ServiceReviewRow("Excluded designations", string.IsNullOrEmpty(excluded) ? "none" : excluded),
            new ServiceReviewRow("Buffer waypoints", BufferAirwayWaypoints ? "Yes" : "No"),
            new ServiceReviewRow("Alias file", aliasFile),
            new ServiceReviewRow("Split at antimeridian", SplitAtAntimeridian ? "Yes" : "No"),
            new ServiceReviewRow("Region of interest", regionOfInterest),
        };

        return new[] { new ServiceReviewSection("Airways", rows) };
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

    private static string YesNo(bool value) => value ? "Y" : "N";



    private bool GetBool(string key, bool fallback)
    {
        string? v = Get(key);
        return v switch
        {
            null or "" => fallback,
            _ => v.Equals("Y", StringComparison.OrdinalIgnoreCase) || v.Equals("true", StringComparison.OrdinalIgnoreCase),
        };
    }



    private HashSet<string> ParseExcludedFromConfig() =>
        (Get("ExcludedDesignations") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(d => d.ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private ObservableCollection<EramClassDefault> BuildClassDefaults(EramFieldKind kind)
    {
        ObservableCollection<EramClassDefault> rows = new();
        foreach (AirwayAltitudeClass altitudeClass in AllClasses)
        {
            rows.Add(new EramClassDefault(altitudeClass.ToString(), kind, MarkDirty));
        }

        return rows;
    }

    private static void MarkRequired(IEnumerable<EramClassDefault> rows, bool required)
    {
        foreach (EramClassDefault row in rows)
        {
            row.IsRequired = required;
        }
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
            if (kind is "Text")
            {
                row.Underline = Get($"{p}.underline") ?? row.Underline;
                row.Opaque = Get($"{p}.opaque") ?? row.Opaque;
                row.XOffset = Get($"{p}.xOffset") ?? row.XOffset;
                row.YOffset = Get($"{p}.yOffset") ?? row.YOffset;
            }
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
            if (kind is "Text")
            {
                Set($"{p}.size", row.Size);
                Set($"{p}.underline", row.Underline);
                Set($"{p}.opaque", row.Opaque);
                Set($"{p}.xOffset", row.XOffset);
                Set($"{p}.yOffset", row.YOffset);
            }
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
            if (kind is "Text")
            {
                s[$"{p}.size"] = row.Size;
                s[$"{p}.underline"] = row.Underline;
                s[$"{p}.opaque"] = row.Opaque;
                s[$"{p}.xOffset"] = row.XOffset.Trim();
                s[$"{p}.yOffset"] = row.YOffset.Trim();
            }
        }
    }

    private void RaiseAllSettingProperties()
    {
        foreach (string name in new[]
        {
            nameof(OutputBy), nameof(OutputModeHint), nameof(IsGeojsonOutputOn), nameof(EmitLines), nameof(EmitSymbols), nameof(EmitText),
            nameof(BufferAirwayWaypoints), nameof(IncludeFebCustomProperties),
            nameof(IncludeCrcLineDefaults), nameof(IncludeCrcSymbolDefaults), nameof(IncludeCrcTextDefaults),
            nameof(GenerateAliasFile), nameof(AliasRoiAirwaysOnly),
            nameof(SplitAtAntimeridian), nameof(OverrideRoi), nameof(SwLat), nameof(SwLon), nameof(NeLat), nameof(NeLon),
        })
        {
            OnPropertyChanged(name);
        }
    }

}
