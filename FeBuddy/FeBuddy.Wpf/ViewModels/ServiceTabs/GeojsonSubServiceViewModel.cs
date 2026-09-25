using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels.Models;

using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Infrastructure.Configuration;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// Base for a sub-service tab that writes GeoJSON (Airports, Airways, Departures): everything
/// those tabs share - the alias file, which GeoJSON files are written, the FE-Buddy properties,
/// the CRC ERAM defaults, the Region of Interest override - with its config, settings-block and
/// validation plumbing.
/// </summary>
/// <remarks>
/// <para>
/// A derived tab builds <see cref="FebProperties"/> and the three CRC defaults lists in its
/// constructor, calls <see cref="LoadSharedSettings"/> from <see cref="SubServiceSettingsViewModel.LoadFromConfig"/>,
/// <see cref="SaveSharedSettings"/> from <see cref="SubServiceSettingsViewModel.WriteToConfig"/>,
/// <see cref="AddSharedSettings"/> from its settings block, and <see cref="ValidateSharedSettings"/>
/// from <see cref="ServiceTabViewModel.Validate"/>.
/// </para>
/// <para>
/// The ROI rule is the same for every sub-service: this tab's override wins, otherwise the
/// shared default ROI (<see cref="DefaultRoiStore"/>), otherwise no geographic limit.
/// </para>
/// </remarks>
public abstract class GeojsonSubServiceViewModel : SubServiceSettingsViewModel,
	IOutputSettings, IFebPropertySettings, ICrcDefaultsSettings, IRoiOverrideSettings
{
	private const string CrcDefaultsIncompleteMessage =
		"Some CRC ERAM default values are empty or invalid. Fix the marked boxes, or untick Include on that panel.";

	private const string OverrideRoiKey = "Roi.OverrideDefaultRoi";

	// "Coordindates" is misspelled in every saved config, so the key keeps the spelling.
	private const string OverrideCornersNode = "Roi.OverrideCoordindates";

	private bool _generateAliasFile = true;
	private bool _emitLines = true;
	private bool _emitSymbols = true;
	private bool _emitText = true;
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

	/// <summary>Wires the ROI picker and follows the shared default ROI.</summary>
	protected GeojsonSubServiceViewModel()
	{
		PickRoiOnMapCommand = new RelayCommand(PickRoiOnMap);
		DefaultRoiStore.Changed += (_, _) => RaiseRoiFallback();
	}

	// ================= outputs and files =================

	/// <inheritdoc />
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
	public bool EmitLines { get => _emitLines; set { if (SetProperty(ref _emitLines, value)) MarkDirty(); } }

	/// <inheritdoc />
	public bool EmitSymbols { get => _emitSymbols; set { if (SetProperty(ref _emitSymbols, value)) MarkDirty(); } }

	/// <inheritdoc />
	public bool EmitText { get => _emitText; set { if (SetProperty(ref _emitText, value)) MarkDirty(); } }

	// ================= FE-Buddy properties =================

	/// <inheritdoc />
	public bool IncludeFebCustomProperties
	{
		get => _includeFebCustomProperties;
		set { if (SetProperty(ref _includeFebCustomProperties, value)) MarkDirty(); }
	}

	/// <inheritdoc />
	/// <remarks>Built by the derived tab's constructor with <see cref="FebPropertyToggle.ListFor"/>.</remarks>
	public ObservableCollection<FebPropertyToggle> FebProperties { get; protected init; } = [];

	// ================= CRC ERAM defaults =================

	/// <inheritdoc />
	public bool IncludeCrcLineDefaults
	{
		get => _includeCrcLineDefaults;
		set { if (SetProperty(ref _includeCrcLineDefaults, value)) MarkDirty(); }
	}

	/// <inheritdoc />
	public bool IncludeCrcSymbolDefaults
	{
		get => _includeCrcSymbolDefaults;
		set { if (SetProperty(ref _includeCrcSymbolDefaults, value)) MarkDirty(); }
	}

	/// <inheritdoc />
	public bool IncludeCrcTextDefaults
	{
		get => _includeCrcTextDefaults;
		set { if (SetProperty(ref _includeCrcTextDefaults, value)) MarkDirty(); }
	}

	/// <inheritdoc />
	public ObservableCollection<EramClassDefault> LineDefaults { get; protected init; } = [];

	/// <inheritdoc />
	public ObservableCollection<EramClassDefault> SymbolDefaults { get; protected init; } = [];

	/// <inheritdoc />
	public ObservableCollection<EramClassDefault> TextDefaults { get; protected init; } = [];

	// ================= region of interest =================

	/// <inheritdoc />
	public bool OverrideRoi
	{
		get => _overrideRoi;
		set
		{
			if (SetProperty(ref _overrideRoi, value))
			{
				MarkDirty();
				RaiseRoiFallback();
			}
		}
	}

	/// <inheritdoc />
	public string SwLat { get => _swLat; set { if (SetProperty(ref _swLat, value)) MarkDirty(); } }

	/// <inheritdoc />
	public string SwLon { get => _swLon; set { if (SetProperty(ref _swLon, value)) MarkDirty(); } }

	/// <inheritdoc />
	public string NeLat { get => _neLat; set { if (SetProperty(ref _neLat, value)) MarkDirty(); } }

	/// <inheritdoc />
	public string NeLon { get => _neLon; set { if (SetProperty(ref _neLon, value)) MarkDirty(); } }

	/// <inheritdoc />
	/// <remarks>The shared default ROI's corners if one is set, otherwise <see cref="NoDefaultRoiHint"/>.</remarks>
	public string RoiFallbackHint => DefaultRoiStore.Load() is { } roi
		? $"Using the default ROI: SW {roi.SwLat:0.####}, {roi.SwLon:0.####} / NE {roi.NeLat:0.####}, {roi.NeLon:0.####}"
		: NoDefaultRoiHint;

	/// <summary>
	/// Whether a region limits the output: this tab's override, or else the shared default ROI.
	/// With neither, the run covers everything.
	/// </summary>
	public bool HasRoi => OverrideRoi || DefaultRoiStore.Load() is not null;

	/// <inheritdoc />
	public ICommand PickRoiOnMapCommand { get; }

	// ================= readiness =================

	/// <summary>Whether the AIRAC data is ready; lists built from the cycle stay disabled until it is.</summary>
	public bool IsCycleReady
	{
		get => _isCycleReady;
		private set => SetProperty(ref _isCycleReady, value);
	}

	/// <summary>Called by the parent whenever AIRAC readiness changes (see <see cref="ISubServiceRunTarget"/>).</summary>
	/// <param name="ready">Whether the AIRAC data is ready.</param>
	public void SetReadiness(bool ready) => IsCycleReady = ready;

	// ================= for the derived tab =================

	/// <summary>Whether this tab writes any GeoJSON at all; CRC defaults only apply when it does.</summary>
	protected abstract bool WritesGeojson { get; }

	/// <summary>
	/// What <see cref="RoiFallbackHint"/> says when no default ROI is set: what the run covers
	/// instead, e.g. that every airway is included.
	/// </summary>
	protected abstract string NoDefaultRoiHint { get; }

	/// <summary>
	/// The keys the three GeoJSON file choices are saved and sent under. The same key serves the
	/// config and the settings block.
	/// </summary>
	protected virtual (string Lines, string Symbols, string Text) EmitKeys => ("EmitLines", "EmitSymbols", "EmitText");

	/// <summary>Where a CRC defaults row is saved under this tab's config node.</summary>
	/// <param name="row">The row.</param>
	/// <returns>The row's key prefix, e.g. <c>CrcEramPropertyDefaults.Airports_Symbol</c>.</returns>
	protected virtual string CrcConfigPrefix(EramClassDefault row) => $"CrcEramPropertyDefaults.{row.ClassName}_{row.Kind}";

	/// <summary>
	/// Restores the shared settings from config, without marking the tab dirty. Call from
	/// <see cref="SubServiceSettingsViewModel.LoadFromConfig"/> before <see cref="ServiceTabViewModel.ClearDirty"/>.
	/// </summary>
	protected void LoadSharedSettings()
	{
		_generateAliasFile = GetBool("GenerateAliasFile", true);
		_emitLines = GetBool(EmitKeys.Lines, true);
		_emitSymbols = GetBool(EmitKeys.Symbols, true);
		_emitText = GetBool(EmitKeys.Text, true);
		_includeFebCustomProperties = GetBool("IncludeFebCustomProperties", false);
		_includeCrcLineDefaults = GetBool("IncludeCrcLineDefaults", true);
		_includeCrcSymbolDefaults = GetBool("IncludeCrcSymbolDefaults", true);
		_includeCrcTextDefaults = GetBool("IncludeCrcTextDefaults", true);

		HashSet<string> selected = ParseList(Get("FebProperties"));
		foreach (FebPropertyToggle toggle in FebProperties)
		{
			toggle.IsSelected = selected.Contains(toggle.Name);
		}

		foreach (EramClassDefault row in AllCrcRows())
		{
			LoadCrcRow(row);
		}

		_overrideRoi = GetBool(OverrideRoiKey, false);
		_swLat = Get($"{OverrideCornersNode}.SwLat") ?? string.Empty;
		_swLon = Get($"{OverrideCornersNode}.SwLon") ?? string.Empty;
		_neLat = Get($"{OverrideCornersNode}.NeLat") ?? string.Empty;
		_neLon = Get($"{OverrideCornersNode}.NeLon") ?? string.Empty;

		foreach (string name in new[]
		{
			nameof(GenerateAliasFile), nameof(EmitLines), nameof(EmitSymbols), nameof(EmitText),
			nameof(IncludeFebCustomProperties),
			nameof(IncludeCrcLineDefaults), nameof(IncludeCrcSymbolDefaults), nameof(IncludeCrcTextDefaults),
			nameof(OverrideRoi), nameof(SwLat), nameof(SwLon), nameof(NeLat), nameof(NeLon),
		})
		{
			OnPropertyChanged(name);
		}

		RaiseRoiFallback();
	}

	/// <summary>Writes the shared settings. Call from <see cref="SubServiceSettingsViewModel.WriteToConfig"/>.</summary>
	protected void SaveSharedSettings()
	{
		Set("GenerateAliasFile", YesNo(GenerateAliasFile));
		Set(EmitKeys.Lines, YesNo(EmitLines));
		Set(EmitKeys.Symbols, YesNo(EmitSymbols));
		Set(EmitKeys.Text, YesNo(EmitText));
		Set("IncludeFebCustomProperties", YesNo(IncludeFebCustomProperties));
		Set("FebProperties", SelectedFebPropertyNames());
		Set("IncludeCrcLineDefaults", YesNo(IncludeCrcLineDefaults));
		Set("IncludeCrcSymbolDefaults", YesNo(IncludeCrcSymbolDefaults));
		Set("IncludeCrcTextDefaults", YesNo(IncludeCrcTextDefaults));

		foreach (EramClassDefault row in AllCrcRows())
		{
			SaveCrcRow(row);
		}

		Set(OverrideRoiKey, YesNo(OverrideRoi));
		Set($"{OverrideCornersNode}.SwLat", SwLat);
		Set($"{OverrideCornersNode}.SwLon", SwLon);
		Set($"{OverrideCornersNode}.NeLat", NeLat);
		Set($"{OverrideCornersNode}.NeLon", NeLon);
	}

	/// <summary>
	/// Adds the settings every GeoJSON sub-service's parser reads the same way: the output
	/// folder, coordinate precision, alias file, file choices, FE-Buddy properties, CRC defaults
	/// and region of interest.
	/// </summary>
	/// <param name="settings">The settings block being built.</param>
	/// <param name="outputDirectory">The run's resolved output directory.</param>
	/// <param name="addFeBuddyOutputFolder">Whether to wrap output in a <c>FE-Buddy_Output</c> folder.</param>
	protected void AddSharedSettings(Dictionary<string, string> settings, string outputDirectory, bool addFeBuddyOutputFolder)
	{
		settings["OutputDirectory"] = outputDirectory;
		settings["AddFeBuddyOutputFolder"] = YesNo(addFeBuddyOutputFolder);
		settings["CoordinatePrecision"] = SavedCoordinatePrecision().ToString(CultureInfo.InvariantCulture);
		settings["GenerateAliasFile"] = YesNo(GenerateAliasFile);
		settings[EmitKeys.Lines] = YesNo(EmitLines);
		settings[EmitKeys.Symbols] = YesNo(EmitSymbols);
		settings[EmitKeys.Text] = YesNo(EmitText);
		settings["IncludeFebCustomProperties"] = YesNo(IncludeFebCustomProperties);
		settings["FebProperties"] = SelectedFebPropertyNames();
		settings["IncludeCrcLineDefaults"] = YesNo(IncludeCrcLineDefaults);
		settings["IncludeCrcSymbolDefaults"] = YesNo(IncludeCrcSymbolDefaults);
		settings["IncludeCrcTextDefaults"] = YesNo(IncludeCrcTextDefaults);

		// Only the rows whose file is written and whose Include box is ticked: the parser requires
		// exactly those, and would only ignore any others.
		foreach (EramClassDefault row in AllCrcRows().Where(IsCrcRowNeeded))
		{
			string prefix = $"Crc.{row.ClassName}.{row.Kind}";
			settings[$"{prefix}.bcg"] = row.Bcg;
			settings[$"{prefix}.filters"] = row.Filters;

			if (row.ShowStyle)
			{
				settings[$"{prefix}.style"] = row.Style;
			}

			if (row.ShowThickness)
			{
				settings[$"{prefix}.thickness"] = row.Thickness;
			}

			if (row.ShowSize)
			{
				settings[$"{prefix}.size"] = row.Size;
			}

			if (row.ShowTextOptions)
			{
				settings[$"{prefix}.underline"] = row.Underline;
				settings[$"{prefix}.opaque"] = row.Opaque;
				settings[$"{prefix}.xOffset"] = row.XOffset.Trim();
				settings[$"{prefix}.yOffset"] = row.YOffset.Trim();
			}
		}

		RegionOfInterest? defaultRoi = OverrideRoi ? null : DefaultRoiStore.Load();
		settings["FilterByRoi"] = YesNo(OverrideRoi || defaultRoi is not null);

		if (OverrideRoi)
		{
			settings["RoiSwLat"] = SwLat;
			settings["RoiSwLon"] = SwLon;
			settings["RoiNeLat"] = NeLat;
			settings["RoiNeLon"] = NeLon;
		}
		else if (defaultRoi is { } roi)
		{
			settings["RoiSwLat"] = roi.SwLat.ToString(CultureInfo.InvariantCulture);
			settings["RoiSwLon"] = roi.SwLon.ToString(CultureInfo.InvariantCulture);
			settings["RoiNeLat"] = roi.NeLat.ToString(CultureInfo.InvariantCulture);
			settings["RoiNeLon"] = roi.NeLon.ToString(CultureInfo.InvariantCulture);
		}
	}

	/// <summary>
	/// Validates the shared settings: the FE-Buddy properties, the CRC defaults that are
	/// actually needed, and the ROI override. Call from <see cref="ServiceTabViewModel.Validate"/>.
	/// </summary>
	/// <param name="validation">The collector to add failures to.</param>
	protected void ValidateSharedSettings(ServiceValidation validation)
	{
		if (IncludeFebCustomProperties && FebProperties.All(p => !p.IsSelected))
		{
			validation.Add("FE-Buddy properties are on but none are selected. Pick at least one, or switch them off.");
		}

		// Only the rows whose file is written and whose Include box is ticked are needed; any
		// other row is never read, so it is not held against the user.
		foreach (EramClassDefault row in AllCrcRows())
		{
			row.IsRequired = IsCrcRowNeeded(row);
		}

		if (AllCrcRows().Any(row => row.IsRequired && row.HasMissingValues))
		{
			validation.Add(CrcDefaultsIncompleteMessage);
		}

		ValidateRoiOverride(validation);
	}

	/// <summary>The CRC ERAM defaults that apply, by file kind, for the Preview Settings tab.</summary>
	/// <param name="linesName">What to call the Lines file, e.g. <c>Runway lines</c>.</param>
	/// <returns>e.g. <c>Lines, Symbols</c>, or <c>None</c>.</returns>
	protected string DescribeCrcDefaults(string linesName = "Lines")
	{
		List<string> kinds = [];
		if (WritesGeojson && IncludeCrcLineDefaults && EmitLines) kinds.Add(linesName);
		if (WritesGeojson && IncludeCrcSymbolDefaults && EmitSymbols) kinds.Add("Symbols");
		if (WritesGeojson && IncludeCrcTextDefaults && EmitText) kinds.Add("Text");
		return kinds.Count > 0 ? string.Join(", ", kinds) : "None";
	}

	/// <summary>The selected FE-Buddy properties, for the Preview Settings tab.</summary>
	/// <returns>e.g. <c>faaId, name</c>, or <c>No</c> when they are off or none is selected.</returns>
	protected string DescribeFebProperties()
	{
		string[] selected = [.. FebProperties.Where(p => p.IsSelected).Select(p => p.Name)];
		return IncludeFebCustomProperties && selected.Length > 0 ? string.Join(", ", selected) : "No";
	}

	/// <summary>The region in use, stated plainly for the Preview Settings tab.</summary>
	/// <returns>The override's or the default ROI's corners, or that there is no geographic limit.</returns>
	protected string DescribeRoi()
	{
		if (OverrideRoi)
		{
			return $"Override: SW {SwLat}, {SwLon} / NE {NeLat}, {NeLon}";
		}

		return DefaultRoiStore.Load() is { } roi
			? $"Default ROI: SW {roi.SwLat:0.####}, {roi.SwLon:0.####} / NE {roi.NeLat:0.####}, {roi.NeLon:0.####}"
			: "None set - no geographic limit";
	}

	// ================= private =================

	private IEnumerable<EramClassDefault> AllCrcRows() => LineDefaults.Concat(SymbolDefaults).Concat(TextDefaults);

	private bool IsCrcRowNeeded(EramClassDefault row) => WritesGeojson && row.Kind switch
	{
		EramFieldKind.Line => EmitLines && IncludeCrcLineDefaults,
		EramFieldKind.Symbol => EmitSymbols && IncludeCrcSymbolDefaults,
		_ => EmitText && IncludeCrcTextDefaults,
	};

	private string SelectedFebPropertyNames() =>
		string.Join(',', FebProperties.Where(p => p.IsSelected).Select(p => p.Name));

	private void LoadCrcRow(EramClassDefault row)
	{
		string prefix = CrcConfigPrefix(row);

		row.Bcg = Get($"{prefix}.bcg") ?? row.Bcg;
		row.Filters = Get($"{prefix}.filters") ?? row.Filters;

		if (row.ShowStyle)
		{
			row.Style = Get($"{prefix}.style") ?? row.Style;
		}

		if (row.ShowThickness)
		{
			row.Thickness = Get($"{prefix}.thickness") ?? row.Thickness;
		}

		if (row.ShowSize)
		{
			row.Size = Get($"{prefix}.size") ?? row.Size;
		}

		if (row.ShowTextOptions)
		{
			row.Underline = Get($"{prefix}.underline") ?? row.Underline;
			row.Opaque = Get($"{prefix}.opaque") ?? row.Opaque;
			row.XOffset = Get($"{prefix}.xOffset") ?? row.XOffset;
			row.YOffset = Get($"{prefix}.yOffset") ?? row.YOffset;
		}
	}

	private void SaveCrcRow(EramClassDefault row)
	{
		string prefix = CrcConfigPrefix(row);

		Set($"{prefix}.bcg", row.Bcg);
		Set($"{prefix}.filters", row.Filters);

		if (row.ShowStyle)
		{
			Set($"{prefix}.style", row.Style);
		}

		if (row.ShowThickness)
		{
			Set($"{prefix}.thickness", row.Thickness);
		}

		if (row.ShowSize)
		{
			Set($"{prefix}.size", row.Size);
		}

		if (row.ShowTextOptions)
		{
			Set($"{prefix}.underline", row.Underline);
			Set($"{prefix}.opaque", row.Opaque);
			Set($"{prefix}.xOffset", row.XOffset);
			Set($"{prefix}.yOffset", row.YOffset);
		}
	}

	private void ValidateRoiOverride(ServiceValidation validation)
	{
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

		if (TryReadOverrideCorners() is { } corners
			&& !RoiFilter.IsCoordinatesRelativePositionValid(corners.SwLat, corners.SwLon, corners.NeLat, corners.NeLon, out string? positionError))
		{
			validation.Add($"ROI override: {positionError}");
		}
	}

	private RegionOfInterest? TryReadOverrideCorners() =>
		double.TryParse(SwLat, NumberStyles.Float, CultureInfo.InvariantCulture, out double swLat)
		&& double.TryParse(SwLon, NumberStyles.Float, CultureInfo.InvariantCulture, out double swLon)
		&& double.TryParse(NeLat, NumberStyles.Float, CultureInfo.InvariantCulture, out double neLat)
		&& double.TryParse(NeLon, NumberStyles.Float, CultureInfo.InvariantCulture, out double neLon)
			? new RegionOfInterest(swLat, swLon, neLat, neLon)
			: null;

	private void PickRoiOnMap()
	{
		RegionOfInterest? picked = Views.RoiPickerWindow.Pick(
			System.Windows.Application.Current?.MainWindow, TryReadOverrideCorners(), Map.BaseMap.UsStates);

		if (picked is { } roi)
		{
			SwLat = roi.SwLat.ToString("0.######", CultureInfo.InvariantCulture);
			SwLon = roi.SwLon.ToString("0.######", CultureInfo.InvariantCulture);
			NeLat = roi.NeLat.ToString("0.######", CultureInfo.InvariantCulture);
			NeLon = roi.NeLon.ToString("0.######", CultureInfo.InvariantCulture);
			OverrideRoi = true;
		}
	}

	private void RaiseRoiFallback()
	{
		OnPropertyChanged(nameof(RoiFallbackHint));
		OnPropertyChanged(nameof(HasRoi));
	}

	private static int SavedCoordinatePrecision() =>
		int.TryParse(UserConfigFile.GetValue(UserConfigKeys.CoordinatePrecision), NumberStyles.Integer, CultureInfo.InvariantCulture, out int saved)
		&& saved is >= 0 and <= 15
			? saved
			: 6;
}
