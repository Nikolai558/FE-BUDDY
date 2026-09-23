using System.Collections.ObjectModel;
using System.Globalization;
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
/// FE-Buddy properties and the CRC ERAM defaults.
/// </summary>
/// <remarks>
/// Save, Undo and navigation come from the tab host's action bar; the run is launched by
/// <b>Run AIRAC Service</b> on the Review tab, and its results are shown there, described by
/// this tab through <see cref="ISubServiceRunTarget"/>.
/// </remarks>
public sealed class DeparturesViewModel : SubServiceSettingsViewModel, ISubServiceRunTarget,
    IFebPropertySettings, ICrcDefaultsSettings, IRoiOverrideSettings
{
    private const string Node = "Services.AiracService.Departures";
    private const string PrecisionKey = "Services.AiracService.CoordinatePrecision";
    private const string CrcClassName = "Departures";
    private const string AmendmentDateFormat = "yyyy-MM-dd";
    private const int MaxAmendedWithinCycles = 1000;
    private const int MaxAmendedWithinDays = 36500;

    private const string CrcDefaultsIncompleteMessage =
        "Some CRC ERAM default values are empty or invalid. Fix the marked boxes, or untick Include on that panel.";

    private bool _generateGeojson = true;
    private bool _generateAliasFile = true;
    private bool _emitLines = true;
    private bool _emitSymbols = true;
    private bool _emitText = true;
    private bool _includeObstacleDepartures = true;
    private bool _includeFebCustomProperties;
    private bool _includeCrcLineDefaults = true;
    private bool _includeCrcSymbolDefaults = true;
    private bool _includeCrcTextDefaults = true;
    private DepartureRoiMode _roiMode = DepartureRoiMode.Airport;
    private bool _overrideRoi;
    private string _swLat = string.Empty;
    private string _swLon = string.Empty;
    private string _neLat = string.Empty;
    private string _neLon = string.Empty;
    private DepartureAmendmentFilter _amendmentFilter = DepartureAmendmentFilter.None;
    private string _amendedWithinCycles = "1";
    private string _amendedWithinDays = "30";
    private DateTime? _amendedOnOrAfter;
    private HashSet<string> _savedArtccFilter = new(StringComparer.OrdinalIgnoreCase);
    private bool _suppressArtccChanges;

    private bool _isCycleReady;

    /// <summary>Builds the tab and restores its saved settings.</summary>
    public DeparturesViewModel()
    {
        FebProperties = new ObservableCollection<FebPropertyToggle>(
            DepartureFebPropertyNames.All.Select(entry =>
                new FebPropertyToggle(entry.Name, entry.Description, MarkDirty)));

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

        PickRoiOnMapCommand = new RelayCommand(PickRoiOnMap);
        ClearArtccsCommand = new RelayCommand(ClearArtccs, () => Artccs.Any(a => a.IsSelected));

        // The default ROI is part of this tab's validation (ROI on, no override, nothing to fall
        // back to), so a change made in Settings or on the Map page re-checks the tab as well.
        DefaultRoiStore.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(RoiFallbackHint));
            OnPropertyChanged(nameof(HasRoi));
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

    /// <summary>Whether procedures are kept whatever their amendment date.</summary>
    public bool AmendmentAny
    {
        get => _amendmentFilter == DepartureAmendmentFilter.None;
        set { if (value) SetAmendmentFilter(DepartureAmendmentFilter.None); }
    }

    /// <summary>Whether only procedures amended within the last <see cref="AmendedWithinCycles"/> cycles are kept.</summary>
    public bool AmendmentByCycles
    {
        get => _amendmentFilter == DepartureAmendmentFilter.Cycles;
        set { if (value) SetAmendmentFilter(DepartureAmendmentFilter.Cycles); }
    }

    /// <summary>Whether only procedures amended within the last <see cref="AmendedWithinDays"/> days are kept.</summary>
    public bool AmendmentByDays
    {
        get => _amendmentFilter == DepartureAmendmentFilter.Days;
        set { if (value) SetAmendmentFilter(DepartureAmendmentFilter.Days); }
    }

    /// <summary>Whether only procedures amended on or after <see cref="AmendedOnOrAfter"/> are kept.</summary>
    public bool AmendmentByDate
    {
        get => _amendmentFilter == DepartureAmendmentFilter.Date;
        set { if (value) SetAmendmentFilter(DepartureAmendmentFilter.Date); }
    }

    /// <summary>How many cycles back the amendment may be; 1 means amended in the selected cycle.</summary>
    public string AmendedWithinCycles { get => _amendedWithinCycles; set { if (SetProperty(ref _amendedWithinCycles, value)) MarkDirty(); } }

    /// <summary>How many days back from today the amendment may be.</summary>
    public string AmendedWithinDays { get => _amendedWithinDays; set { if (SetProperty(ref _amendedWithinDays, value)) MarkDirty(); } }

    /// <summary>The earliest effective date a procedure's current amendment may have.</summary>
    public DateTime? AmendedOnOrAfter
    {
        get => _amendedOnOrAfter;
        set { if (SetProperty(ref _amendedOnOrAfter, value?.Date)) MarkDirty(); }
    }

    // ================= region of interest =================

    /// <summary>
    /// Whether a region limits the output: this tab's override, or else the shared default ROI.
    /// With neither, every departure in NASR is included.
    /// </summary>
    public bool HasRoi => OverrideRoi || DefaultRoiStore.Load() is not null;

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
        set { if (SetProperty(ref _overrideRoi, value)) { MarkDirty(); OnPropertyChanged(nameof(RoiFallbackHint)); OnPropertyChanged(nameof(HasRoi)); } }
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
        : "No default ROI is set, so every departure procedure is included. Set one in Settings, or override it here.";

    /// <summary>Opens the shared ROI picker and copies what the user confirms into the four boxes.</summary>
    public ICommand PickRoiOnMapCommand { get; }

    // ================= properties =================

    /// <summary>Whether departure Features carry the selected <c>feb.*</c> properties.</summary>
    public bool IncludeFebCustomProperties
    {
        get => _includeFebCustomProperties;
        set { if (SetProperty(ref _includeFebCustomProperties, value)) MarkDirty(); }
    }

    /// <summary>One toggle per available <c>feb.*</c> property.</summary>
    public ObservableCollection<FebPropertyToggle> FebProperties { get; }

    // ================= CRC ERAM defaults =================

    /// <summary>Whether the CRC ERAM isDefaults Feature is written into each <c>_Lines</c> file.</summary>
    public bool IncludeCrcLineDefaults
    {
        get => _includeCrcLineDefaults;
        set { if (SetProperty(ref _includeCrcLineDefaults, value)) MarkDirty(); }
    }

    /// <summary>Whether the CRC ERAM isDefaults Feature is written into each <c>_Symbols</c> file.</summary>
    public bool IncludeCrcSymbolDefaults
    {
        get => _includeCrcSymbolDefaults;
        set { if (SetProperty(ref _includeCrcSymbolDefaults, value)) MarkDirty(); }
    }

    /// <summary>Whether the CRC ERAM isDefaults Feature is written into each <c>_Text</c> file.</summary>
    public bool IncludeCrcTextDefaults
    {
        get => _includeCrcTextDefaults;
        set { if (SetProperty(ref _includeCrcTextDefaults, value)) MarkDirty(); }
    }

    /// <summary>CRC line defaults for the procedure lines.</summary>
    public ObservableCollection<EramClassDefault> LineDefaults { get; }

    /// <summary>CRC symbol defaults for the procedure points.</summary>
    public ObservableCollection<EramClassDefault> SymbolDefaults { get; }

    /// <summary>CRC text defaults for the point labels.</summary>
    public ObservableCollection<EramClassDefault> TextDefaults { get; }

    // ================= readiness =================

    /// <summary>Whether the AIRAC data is ready; the run and the cycle-dependent lists gate on it.</summary>
    public bool IsCycleReady
    {
        get => _isCycleReady;
        private set => SetProperty(ref _isCycleReady, value);
    }

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
    public SubServiceRunResult? DescribeRunResult(AiracServiceResult result)
    {
        if (result.Departures is not { } departures)
        {
            return null;
        }

        string summary = $"{departures.AirportProcedureCount:N0} airport procedure(s), "
            + $"{departures.GeojsonFilesWritten.Count:N0} GeoJSON file(s)";

        if (departures.SkippedForMissingPointsCount > 0)
        {
            summary += $", {departures.SkippedForMissingPointsCount:N0} skipped for missing points";
        }

        if (departures.AliasFilePath is not null)
        {
            summary += $", Departures.txt: {departures.AliasCommandCount:N0} alias command(s)";
        }

        return new SubServiceRunResult(Title, summary, departures.Messages);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> BuildSettingsBlock(string outputDirectory, bool addFeBuddyOutputFolder)
    {
        // Same as every sub-service: an explicit override wins, otherwise the shared default ROI,
        // otherwise no geographic limit.
        RegionOfInterest? fallbackRoi = OverrideRoi ? null : DefaultRoiStore.Load();

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
            ["AmendmentFilter"] = _amendmentFilter.ToString(),
            ["IncludeFebCustomProperties"] = YesNo(IncludeFebCustomProperties),
            ["FebProperties"] = string.Join(',', FebProperties.Where(p => p.IsSelected).Select(p => p.Name)),
            ["IncludeCrcLineDefaults"] = YesNo(IncludeCrcLineDefaults),
            ["IncludeCrcSymbolDefaults"] = YesNo(IncludeCrcSymbolDefaults),
            ["IncludeCrcTextDefaults"] = YesNo(IncludeCrcTextDefaults),
            ["FilterByRoi"] = YesNo(OverrideRoi || fallbackRoi is not null),
            ["RoiMode"] = _roiMode.ToString(),
            ["CoordinatePrecision"] = ResolveCoordinatePrecision().ToString(CultureInfo.InvariantCulture),
            ["AddFeBuddyOutputFolder"] = YesNo(addFeBuddyOutputFolder),
        };

        // Only the active mode's value; the parser reads that key alone and warns about any other.
        switch (_amendmentFilter)
        {
            case DepartureAmendmentFilter.Cycles:
                s["AmendedWithinCycles"] = AmendedWithinCycles.Trim();
                break;
            case DepartureAmendmentFilter.Days:
                s["AmendedWithinDays"] = AmendedWithinDays.Trim();
                break;
            case DepartureAmendmentFilter.Date when AmendedOnOrAfter is { } onOrAfter:
                s["AmendedOnOrAfter"] = onOrAfter.ToString(AmendmentDateFormat, CultureInfo.InvariantCulture);
                break;
        }

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

        if (GenerateGeojson)
        {
            // Only the blocks whose file is actually being written and whose Include box is
            // ticked; the parser requires exactly those and warns about any others.
            if (IncludeCrcLineDefaults && EmitLines) WriteCrcRow(s, "Line", LineDefaults[0]);
            if (IncludeCrcSymbolDefaults && EmitSymbols) WriteCrcRow(s, "Symbol", SymbolDefaults[0]);
            if (IncludeCrcTextDefaults && EmitText) WriteCrcRow(s, "Text", TextDefaults[0]);
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

        List<string> crcDefaults = new();
        if (GenerateGeojson)
        {
            if (IncludeCrcLineDefaults && EmitLines) crcDefaults.Add("Lines");
            if (IncludeCrcSymbolDefaults && EmitSymbols) crcDefaults.Add("Symbols");
            if (IncludeCrcTextDefaults && EmitText) crcDefaults.Add("Text");
        }

        // Before a cycle is parsed the toggle list is empty; SelectedArtccs falls back to what
        // is saved, so the review does not claim "All" when a filter is on disk.
        string[] selectedArtccs = SelectedArtccs().ToArray();
        string[] selectedProperties = FebProperties.Where(p => p.IsSelected).Select(p => p.Name).ToArray();

        ServiceReviewRow[] rows =
        {
            new ServiceReviewRow("Outputs", string.Join(", ", outputs)),
            new ServiceReviewRow("GeoJSON files", GenerateGeojson ? string.Join(", ", geojsonFiles) : "No"),
            new ServiceReviewRow("FE-Buddy properties",
                IncludeFebCustomProperties && selectedProperties.Length > 0
                    ? string.Join(", ", selectedProperties)
                    : "No"),
            new ServiceReviewRow("CRC ERAM defaults", crcDefaults.Count > 0 ? string.Join(", ", crcDefaults) : "None"),
            new ServiceReviewRow("Includes", DescribeScope(selectedArtccs)),
            new ServiceReviewRow("Region of interest", DescribeRoi()),
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
        _includeCrcLineDefaults = GetBool("IncludeCrcLineDefaults", true);
        _includeCrcSymbolDefaults = GetBool("IncludeCrcSymbolDefaults", true);
        _includeCrcTextDefaults = GetBool("IncludeCrcTextDefaults", true);

        // Both outputs off would leave the tab in a state its own guard forbids; a hand-edited
        // config is the only way to get here, so fall back to the default rather than honour it.
        if (!_generateGeojson && !_generateAliasFile)
        {
            _generateGeojson = true;
            _generateAliasFile = true;
        }

        HashSet<string> selected = ParseList(Get("FebProperties"));
        foreach (FebPropertyToggle toggle in FebProperties)
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

        _roiMode = string.Equals(Get("Roi.Mode")?.Trim(), nameof(DepartureRoiMode.Waypoint), StringComparison.OrdinalIgnoreCase)
            ? DepartureRoiMode.Waypoint
            : DepartureRoiMode.Airport;
        _overrideRoi = GetBool("Roi.OverrideDefaultRoi", false);
        _swLat = Get("Roi.OverrideCoordindates.SwLat") ?? string.Empty;
        _swLon = Get("Roi.OverrideCoordindates.SwLon") ?? string.Empty;
        _neLat = Get("Roi.OverrideCoordindates.NeLat") ?? string.Empty;
        _neLon = Get("Roi.OverrideCoordindates.NeLon") ?? string.Empty;

        // Filter by name only; anything else (a number, a typo) falls back to no filter.
        string? savedFilter = Get("Amendment.Filter")?.Trim();
        _amendmentFilter = Enum.GetValues<DepartureAmendmentFilter>()
            .FirstOrDefault(f => f.ToString().Equals(savedFilter, StringComparison.OrdinalIgnoreCase));
        _amendedWithinCycles = Get("Amendment.WithinCycles") ?? "1";
        _amendedWithinDays = Get("Amendment.WithinDays") ?? "30";
        _amendedOnOrAfter = DateTime.TryParseExact(
            Get("Amendment.OnOrAfter")?.Trim(), AmendmentDateFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime onOrAfter)
            ? onOrAfter
            : null;

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
        Set("IncludeCrcLineDefaults", YesNo(IncludeCrcLineDefaults));
        Set("IncludeCrcSymbolDefaults", YesNo(IncludeCrcSymbolDefaults));
        Set("IncludeCrcTextDefaults", YesNo(IncludeCrcTextDefaults));
        Set("Roi.Mode", _roiMode.ToString());
        Set("Roi.OverrideDefaultRoi", YesNo(OverrideRoi));
        Set("Roi.OverrideCoordindates.SwLat", SwLat);
        Set("Roi.OverrideCoordindates.SwLon", SwLon);
        Set("Roi.OverrideCoordindates.NeLat", NeLat);
        Set("Roi.OverrideCoordindates.NeLon", NeLon);
        Set("Amendment.Filter", _amendmentFilter.ToString());
        Set("Amendment.WithinCycles", AmendedWithinCycles);
        Set("Amendment.WithinDays", AmendedWithinDays);
        Set("Amendment.OnOrAfter", AmendedOnOrAfter?.ToString(AmendmentDateFormat, CultureInfo.InvariantCulture) ?? string.Empty);

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

        // Only the rows whose file is actually written and whose Include box is ticked are
        // needed; any other row is never read, so it is not held against the user.
        LineDefaults[0].IsRequired = GenerateGeojson && EmitLines && IncludeCrcLineDefaults;
        SymbolDefaults[0].IsRequired = GenerateGeojson && EmitSymbols && IncludeCrcSymbolDefaults;
        TextDefaults[0].IsRequired = GenerateGeojson && EmitText && IncludeCrcTextDefaults;

        if (LineDefaults.Concat(SymbolDefaults).Concat(TextDefaults)
            .Any(row => row.IsRequired && row.HasMissingValues))
        {
            validation.Add(CrcDefaultsIncompleteMessage);
        }

        ValidateAmendmentFilter(validation);

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

    /// <summary>Checks only the active amendment mode's input; the others are kept but never read.</summary>
    /// <param name="validation">The validation being built.</param>
    private void ValidateAmendmentFilter(ServiceValidation validation)
    {
        switch (_amendmentFilter)
        {
            case DepartureAmendmentFilter.Cycles:
                if (!TryParseWholeNumber(AmendedWithinCycles, 1, MaxAmendedWithinCycles, out _))
                {
                    validation.AddField(nameof(AmendedWithinCycles),
                        $"Enter a whole number of cycles from 1 to {MaxAmendedWithinCycles}.");
                }

                break;

            case DepartureAmendmentFilter.Days:
                if (!TryParseWholeNumber(AmendedWithinDays, 1, MaxAmendedWithinDays, out _))
                {
                    validation.AddField(nameof(AmendedWithinDays),
                        $"Enter a whole number of days from 1 to {MaxAmendedWithinDays}.");
                }

                break;

            case DepartureAmendmentFilter.Date:
                if (AmendedOnOrAfter is null)
                {
                    validation.AddField(nameof(AmendedOnOrAfter),
                        "Pick the date the amendment must be on or after.");
                }

                break;
        }
    }

    private static bool TryParseWholeNumber(string? text, int min, int max, out int value) =>
        int.TryParse(text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value)
        && value >= min
        && value <= max;

    /// <summary>The amendment part of the "Includes" sentence; empty when there is no amendment filter.</summary>
    /// <returns>e.g. ", amended in the last 4 cycles".</returns>
    private string DescribeAmendmentFilter()
    {
        switch (_amendmentFilter)
        {
            case DepartureAmendmentFilter.Cycles:
                return TryParseWholeNumber(AmendedWithinCycles, 1, MaxAmendedWithinCycles, out int cycles) && cycles == 1
                    ? ", amended this cycle"
                    : $", amended in the last {AmendedWithinCycles.Trim()} cycles";

            case DepartureAmendmentFilter.Days:
                return TryParseWholeNumber(AmendedWithinDays, 1, MaxAmendedWithinDays, out int days) && days == 1
                    ? ", amended in the last day"
                    : $", amended in the last {AmendedWithinDays.Trim()} days";

            case DepartureAmendmentFilter.Date:
                return AmendedOnOrAfter is { } onOrAfter
                    ? $", amended on or after {onOrAfter.ToString(AmendmentDateFormat, CultureInfo.InvariantCulture)}"
                    : ", amended on or after a date not yet picked";

            default:
                return string.Empty;
        }
    }

    private void SetAmendmentFilter(DepartureAmendmentFilter filter)
    {
        if (_amendmentFilter == filter)
        {
            return;
        }

        _amendmentFilter = filter;
        OnPropertyChanged(nameof(AmendmentAny));
        OnPropertyChanged(nameof(AmendmentByCycles));
        OnPropertyChanged(nameof(AmendmentByDays));
        OnPropertyChanged(nameof(AmendmentByDate));
        MarkDirty();
    }

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

        string region = !HasRoi
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

        return $"{kinds}, {where}{region}{DescribeAmendmentFilter()}.{outputs}";
    }

    private string DescribeRoi()
    {
        // Only the geographic limit. What the run covers overall - which is also narrowed by the
        // ARTCC and procedure-type choices - is the "Includes" row's job (DescribeScope).
        if (!HasRoi)
        {
            return "None set - no geographic limit";
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
            return "None set - no geographic limit";
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
            settings[$"{prefix}.underline"] = row.Underline;
            settings[$"{prefix}.opaque"] = row.Opaque;
            settings[$"{prefix}.xOffset"] = row.XOffset.Trim();
            settings[$"{prefix}.yOffset"] = row.YOffset.Trim();
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

        if (kind is "Text")
        {
            row.Underline = Get($"{prefix}.underline") ?? row.Underline;
            row.Opaque = Get($"{prefix}.opaque") ?? row.Opaque;
            row.XOffset = Get($"{prefix}.xOffset") ?? row.XOffset;
            row.YOffset = Get($"{prefix}.yOffset") ?? row.YOffset;
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

        if (kind is "Text")
        {
            Set($"{prefix}.underline", row.Underline);
            Set($"{prefix}.opaque", row.Opaque);
            Set($"{prefix}.xOffset", row.XOffset);
            Set($"{prefix}.yOffset", row.YOffset);
        }
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
        OnPropertyChanged(nameof(IncludeCrcLineDefaults));
        OnPropertyChanged(nameof(IncludeCrcSymbolDefaults));
        OnPropertyChanged(nameof(IncludeCrcTextDefaults));
        OnPropertyChanged(nameof(HasRoi));
        OnPropertyChanged(nameof(RoiModeAirport));
        OnPropertyChanged(nameof(RoiModeWaypoint));
        OnPropertyChanged(nameof(OverrideRoi));
        OnPropertyChanged(nameof(SwLat));
        OnPropertyChanged(nameof(SwLon));
        OnPropertyChanged(nameof(NeLat));
        OnPropertyChanged(nameof(NeLon));
        OnPropertyChanged(nameof(RoiFallbackHint));
        OnPropertyChanged(nameof(AmendmentAny));
        OnPropertyChanged(nameof(AmendmentByCycles));
        OnPropertyChanged(nameof(AmendmentByDays));
        OnPropertyChanged(nameof(AmendmentByDate));
        OnPropertyChanged(nameof(AmendedWithinCycles));
        OnPropertyChanged(nameof(AmendedWithinDays));
        OnPropertyChanged(nameof(AmendedOnOrAfter));
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
