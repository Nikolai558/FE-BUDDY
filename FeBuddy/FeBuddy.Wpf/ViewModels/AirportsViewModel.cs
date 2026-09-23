using System.Collections.ObjectModel;
using System.Globalization;
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
/// which GeoJSON files, which FE-Buddy properties, the CRC ERAM defaults, and the ROI override.
/// </summary>
/// <remarks>
/// Save, Undo and navigation come from the tab host's action bar; the run is launched by
/// <b>Run AIRAC Service</b> on the Review tab, and its results are shown there, described by
/// this tab through <see cref="ISubServiceRunTarget"/>.
/// </remarks>
public sealed class AirportsViewModel : SubServiceSettingsViewModel, ISubServiceRunTarget,
    IOutputSettings, IFebPropertySettings, ICrcDefaultsSettings, IRoiOverrideSettings
{
    private const string Node = "Services.AiracService.Airports";
    private const string PrecisionKey = "Services.AiracService.CoordinatePrecision";

    private const string CrcDefaultsIncompleteMessage =
        "Some CRC ERAM default values are empty or invalid. Fix the marked boxes, or untick Include on that panel.";

    private bool _generateGeojson = true;
    private bool _generateAliasFile = true;
    private bool _emitSymbols = true;
    private bool _emitText = true;
    private bool _emitLines = true;
    private bool _includeFebCustomProperties;
    private bool _includeCrcLineDefaults = true;
    private bool _includeCrcSymbolDefaults = true;
    private bool _includeCrcTextDefaults = true;
    private bool _overrideRoi;
    private string _swLat = string.Empty;
    private string _swLon = string.Empty;
    private string _neLat = string.Empty;
    private string _neLon = string.Empty;

    private bool _isCycleReady;

    /// <summary>Builds the tab and restores its saved settings.</summary>
    public AirportsViewModel()
    {
        FebProperties = new ObservableCollection<FebPropertyToggle>(
            AirportFebPropertyNames.All.Select(entry =>
                new FebPropertyToggle(entry.Name, entry.Description, MarkDirty)));

        SymbolDefaults = new ObservableCollection<EramClassDefault>
        {
            new("Airports", EramFieldKind.Symbol, MarkDirty)
        };

        TextDefaults = new ObservableCollection<EramClassDefault>
        {
            new("Airports", EramFieldKind.Text, MarkDirty)
        };

        LineDefaults = new ObservableCollection<EramClassDefault>
        {
            new("Runways", EramFieldKind.Line, MarkDirty)
        };

        PickRoiOnMapCommand = new RelayCommand(PickRoiOnMap);

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
    public bool EmitSymbols
    {
        get => _emitSymbols;
        set { if (SetProperty(ref _emitSymbols, value)) MarkDirty(); }
    }

    /// <summary>Emit <c>Airports_Text.geojson</c>.</summary>
    public bool EmitText
    {
        get => _emitText;
        set { if (SetProperty(ref _emitText, value)) MarkDirty(); }
    }

    /// <summary>Emit <c>Runways_Lines.geojson</c>.</summary>
    public bool EmitLines
    {
        get => _emitLines;
        set { if (SetProperty(ref _emitLines, value)) MarkDirty(); }
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

    /// <summary>One toggle per available <c>feb.*</c> property.</summary>
    public ObservableCollection<FebPropertyToggle> FebProperties { get; }

    // ================= CRC ERAM defaults =================

    /// <summary>Whether the CRC ERAM isDefaults Feature is written into <c>Runways_Lines.geojson</c>.</summary>
    public bool IncludeCrcLineDefaults
    {
        get => _includeCrcLineDefaults;
        set { if (SetProperty(ref _includeCrcLineDefaults, value)) MarkDirty(); }
    }

    /// <summary>Whether the CRC ERAM isDefaults Feature is written into <c>Airports_Symbols.geojson</c>.</summary>
    public bool IncludeCrcSymbolDefaults
    {
        get => _includeCrcSymbolDefaults;
        set { if (SetProperty(ref _includeCrcSymbolDefaults, value)) MarkDirty(); }
    }

    /// <summary>Whether the CRC ERAM isDefaults Feature is written into <c>Airports_Text.geojson</c>.</summary>
    public bool IncludeCrcTextDefaults
    {
        get => _includeCrcTextDefaults;
        set { if (SetProperty(ref _includeCrcTextDefaults, value)) MarkDirty(); }
    }

    /// <summary>CRC symbol defaults for the airport points.</summary>
    public ObservableCollection<EramClassDefault> SymbolDefaults { get; }

    /// <summary>CRC text defaults for the airport labels.</summary>
    public ObservableCollection<EramClassDefault> TextDefaults { get; }

    /// <summary>CRC line defaults for the runway centrelines.</summary>
    public ObservableCollection<EramClassDefault> LineDefaults { get; }

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
    public SubServiceRunResult? DescribeRunResult(AiracServiceResult result)
    {
        if (result.Airports is not { } airports)
        {
            return null;
        }

        string summary = $"{airports.AirportCount:N0} airports, {airports.AirportsInRoiCount:N0} in the ROI";

        if (airports.AliasFilePath is not null)
        {
            summary += $", Airports.txt: {airports.AliasCommandCount:N0} alias command(s)";
        }

        if (airports.GeojsonFilesWritten.Count > 0)
        {
            summary += $", {airports.GeojsonFilesWritten.Count:N0} GeoJSON file(s)";
        }

        return new SubServiceRunResult(Title, summary, airports.Messages);
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
            ["EmitAirportSymbols"] = YesNo(EmitSymbols),
            ["EmitAirportText"] = YesNo(EmitText),
            ["EmitRunwayLines"] = YesNo(EmitLines),
            ["IncludeFebCustomProperties"] = YesNo(IncludeFebCustomProperties),
            ["FebProperties"] = string.Join(',', FebProperties.Where(p => p.IsSelected).Select(p => p.Name)),
            ["IncludeCrcLineDefaults"] = YesNo(IncludeCrcLineDefaults),
            ["IncludeCrcSymbolDefaults"] = YesNo(IncludeCrcSymbolDefaults),
            ["IncludeCrcTextDefaults"] = YesNo(IncludeCrcTextDefaults),
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

        if (GenerateGeojson)
        {
            // Only the blocks whose file is actually being written and whose Include box is
            // ticked; the parser requires exactly those and warns about any others.
            if (IncludeCrcSymbolDefaults && EmitSymbols) WriteCrcRow(s, "Symbol", SymbolDefaults[0]);
            if (IncludeCrcTextDefaults && EmitText) WriteCrcRow(s, "Text", TextDefaults[0]);
            if (IncludeCrcLineDefaults && EmitLines) WriteCrcRow(s, "Line", LineDefaults[0]);
        }

        return s;
    }

    /// <inheritdoc />
    public override IReadOnlyList<ServiceReviewSection> BuildReviewSummary()
    {
        List<string> geojsonFiles = new();
        if (EmitLines) geojsonFiles.Add("Runway lines");
        if (EmitSymbols) geojsonFiles.Add("Symbols");
        if (EmitText) geojsonFiles.Add("Text");

        // Same order and names as the GeoJSON row above.
        List<string> crcDefaults = new();
        if (GenerateGeojson)
        {
            if (IncludeCrcLineDefaults && EmitLines) crcDefaults.Add("Runway lines");
            if (IncludeCrcSymbolDefaults && EmitSymbols) crcDefaults.Add("Symbols");
            if (IncludeCrcTextDefaults && EmitText) crcDefaults.Add("Text");
        }

        string[] selectedProperties = FebProperties.Where(p => p.IsSelected).Select(p => p.Name).ToArray();

        ServiceReviewRow[] rows =
        {
            new ServiceReviewRow("GeoJSON", GenerateGeojson ? string.Join(", ", geojsonFiles) : "No"),
            new ServiceReviewRow("GeoJSON covers", GenerateGeojson ? DescribeGeojsonScope() : "No GeoJSON"),
            new ServiceReviewRow("Alias file",
                GenerateAliasFile ? "Airports.txt, every open airport in NASR - the region never limits the alias file" : "No"),
            new ServiceReviewRow("FE-Buddy properties",
                IncludeFebCustomProperties && selectedProperties.Length > 0
                    ? string.Join(", ", selectedProperties)
                    : "No"),
            new ServiceReviewRow("CRC ERAM defaults", crcDefaults.Count > 0 ? string.Join(", ", crcDefaults) : "None"),
            new ServiceReviewRow("Region of interest", DescribeRoi()),
        };

        return new[] { new ServiceReviewSection("Airports", rows) };
    }

    /// <summary>What the GeoJSON files cover once the region (override or default) is applied.</summary>
    /// <returns>e.g. "Open airports whose reference point is inside the region".</returns>
    private string DescribeGeojsonScope() =>
        OverrideRoi || DefaultRoiStore.Load() is not null
            ? "Open airports whose reference point is inside the region"
            : "Every open airport in NASR";

    /// <summary>
    /// Only the geographic limit, stated plainly for the review. The tab's own hint
    /// (<see cref="RoiFallbackHint"/>) carries the "set one in Settings" advice instead.
    /// </summary>
    /// <returns>The region in use, or that there is none.</returns>
    private string DescribeRoi()
    {
        if (OverrideRoi)
        {
            return $"Override: SW {SwLat}, {SwLon} / NE {NeLat}, {NeLon}";
        }

        return DefaultRoiStore.Load() is { } roi
            ? $"Default ROI: SW {roi.SwLat:0.####}, {roi.SwLon:0.####} / NE {roi.NeLat:0.####}, {roi.NeLon:0.####}"
            : "None set - no geographic limit";
    }

    // ================= save contract =================

    /// <inheritdoc />
    protected override void LoadFromConfig()
    {
        _generateGeojson = GetBool("GenerateGeojson", true);
        _generateAliasFile = GetBool("GenerateAliasFile", true);
        _emitSymbols = GetBool("EmitAirportSymbols", true);
        _emitText = GetBool("EmitAirportText", true);
        _emitLines = GetBool("EmitRunwayLines", true);
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

        LoadCrcRow("Symbol", SymbolDefaults[0]);
        LoadCrcRow("Text", TextDefaults[0]);
        LoadCrcRow("Line", LineDefaults[0]);

        RaiseAllSettingProperties();
        ClearDirty();
    }

    /// <inheritdoc />
    protected override void WriteToConfig()
    {
        Set("GenerateGeojson", YesNo(GenerateGeojson));
        Set("GenerateAliasFile", YesNo(GenerateAliasFile));
        Set("EmitAirportSymbols", YesNo(EmitSymbols));
        Set("EmitAirportText", YesNo(EmitText));
        Set("EmitRunwayLines", YesNo(EmitLines));
        Set("IncludeFebCustomProperties", YesNo(IncludeFebCustomProperties));
        Set("FebProperties", string.Join(',', FebProperties.Where(p => p.IsSelected).Select(p => p.Name)));
        Set("IncludeCrcLineDefaults", YesNo(IncludeCrcLineDefaults));
        Set("IncludeCrcSymbolDefaults", YesNo(IncludeCrcSymbolDefaults));
        Set("IncludeCrcTextDefaults", YesNo(IncludeCrcTextDefaults));
        Set("Roi.OverrideDefaultRoi", YesNo(OverrideRoi));
        Set("Roi.OverrideCoordindates.SwLat", SwLat);
        Set("Roi.OverrideCoordindates.SwLon", SwLon);
        Set("Roi.OverrideCoordindates.NeLat", NeLat);
        Set("Roi.OverrideCoordindates.NeLon", NeLon);

        SaveCrcRow("Symbol", SymbolDefaults[0]);
        SaveCrcRow("Text", TextDefaults[0]);
        SaveCrcRow("Line", LineDefaults[0]);
    }

    /// <inheritdoc />
    protected override void Validate(ServiceValidation validation)
    {
        if (GenerateGeojson && !EmitSymbols && !EmitText && !EmitLines)
        {
            validation.Add(
                "GeoJSON is on but none of its files are selected. Turn on Symbols, Text or Runway lines, "
                + "or switch GeoJSON off.");
        }

        if (IncludeFebCustomProperties && FebProperties.All(p => !p.IsSelected))
        {
            validation.Add("FE-Buddy properties are on but none are selected. Pick at least one, or switch them off.");
        }

        // Only the rows whose file is actually written and whose Include box is ticked are
        // needed; any other row is never read, so it is not held against the user.
        SymbolDefaults[0].IsRequired = GenerateGeojson && EmitSymbols && IncludeCrcSymbolDefaults;
        TextDefaults[0].IsRequired = GenerateGeojson && EmitText && IncludeCrcTextDefaults;
        LineDefaults[0].IsRequired = GenerateGeojson && EmitLines && IncludeCrcLineDefaults;

        if (SymbolDefaults.Concat(TextDefaults).Concat(LineDefaults)
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
        OnPropertyChanged(nameof(EmitSymbols));
        OnPropertyChanged(nameof(EmitText));
        OnPropertyChanged(nameof(EmitLines));
        OnPropertyChanged(nameof(IncludeFebCustomProperties));
        OnPropertyChanged(nameof(IncludeCrcLineDefaults));
        OnPropertyChanged(nameof(IncludeCrcSymbolDefaults));
        OnPropertyChanged(nameof(IncludeCrcTextDefaults));
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
