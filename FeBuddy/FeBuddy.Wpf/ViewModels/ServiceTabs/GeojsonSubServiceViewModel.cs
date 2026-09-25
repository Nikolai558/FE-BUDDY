using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels.Models;

using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// Base for a sub-service tab that writes GeoJSON (Airports, Airways, Departures, Arrivals, NAVAIDs):
/// everything those tabs share - the alias file, which GeoJSON files are written, the FE-Buddy properties,
/// the Region of Interest override, the files to upload to vNAS and the CRC ERAM defaults they
/// carry - with its config, settings-block and validation plumbing.
/// </summary>
/// <remarks>
/// <para>
/// A derived tab builds <see cref="FebProperties"/> and the three CRC defaults lists in its
/// constructor, lists the files its settings write in <see cref="OutputFiles"/>, calls
/// <see cref="LoadSharedSettings"/> from <see cref="SubServiceSettingsViewModel.LoadFromConfig"/>,
/// <see cref="SaveSharedSettings"/> from <see cref="SubServiceSettingsViewModel.WriteToConfig"/>,
/// <see cref="AddSharedSettings"/> from its settings block, and <see cref="ValidateSharedSettings"/>
/// from <see cref="ServiceTabViewModel.Validate"/>.
/// </para>
/// <para>
/// The ROI rule is the same for every sub-service: this tab's override wins, otherwise the
/// shared default ROI (<see cref="DefaultRoiStore"/>), otherwise no geographic limit.
/// </para>
/// <para>
/// The vNAS choices are kept as sets of file keys, including files the current settings do not
/// write (the Symbols files while Symbols is off, say), so switching a file off and on again
/// keeps its choice. Only the files actually written are shown, sent to a run, and have their CRC
/// values required.
/// </para>
/// </remarks>
public abstract class GeojsonSubServiceViewModel : SubServiceSettingsViewModel,
	IOutputSettings, IGeojsonFileChoices, IFebPropertySettings, IVnasUploadSettings, ICrcDefaultsSettings, IRoiOverrideSettings
{
	private const string CrcDefaultsIncompleteMessage =
		"Some CRC ERAM default values are empty or invalid. Fix the marked boxes, or take CRC-ERAM defaults off those files on the Upload to vNAS card.";

	private const string NoCrcFilesMessage =
		"CRC-ERAM defaults are set to go on specific files, but none is ticked. Tick at least one, or choose No CRC-ERAM defaults.";

	private const string OverrideRoiKey = "Roi.OverrideDefaultRoi";

	// "Coordindates" is misspelled in every saved config, so the key keeps the spelling.
	private const string OverrideCornersNode = "Roi.OverrideCoordindates";

	private const string VnasFilesKey = "Vnas.UploadFiles";
	private const string CrcDefaultsScopeKey = "Vnas.CrcDefaults";
	private const string CrcFilesKey = "Vnas.CrcFiles";

	private readonly HashSet<string> _vnasFiles = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _crcFiles = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<(string ClassName, EramFieldKind Kind)> _crcRowsInUse = [];

	private bool _generateAliasFile = true;
	private bool _emitLines = true;
	private bool _emitSymbols = true;
	private bool _emitText = true;
	private bool _includeFebCustomProperties;
	private CrcDefaultsScope _crcDefaultsScope = CrcDefaultsScope.None;
	private string? _vnasFilesSignature;
	private string? _crcFilesSignature;
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

	// ================= upload to vNAS =================

	/// <inheritdoc />
	public ObservableCollection<VnasFileRow> VnasFileRows { get; } = [];

	/// <inheritdoc />
	public bool HasOutputFiles => VnasFileRows.Count > 0;

	/// <inheritdoc />
	public bool HasVnasGeojsonFiles => UploadedFiles().Any(file => file.IsGeojson);

	/// <inheritdoc />
	public CrcDefaultsScope CrcDefaultsScope
	{
		get => _crcDefaultsScope;
		set
		{
			if (SetProperty(ref _crcDefaultsScope, value))
			{
				OnPropertyChanged(nameof(IsCrcDefaultsSpecific));
				MarkDirty();
			}
		}
	}

	/// <inheritdoc />
	public bool IsCrcDefaultsSpecific => CrcDefaultsScope == CrcDefaultsScope.SpecificFiles;

	/// <inheritdoc />
	public ObservableCollection<VnasFileRow> CrcFileRows { get; } = [];

	// ================= CRC ERAM defaults =================

	/// <summary>The Lines defaults: one row per class, in use or not. Built by the derived tab's constructor.</summary>
	public ObservableCollection<EramClassDefault> LineDefaults { get; protected init; } = [];

	/// <summary>The Symbols defaults: one row per class, in use or not. Built by the derived tab's constructor.</summary>
	public ObservableCollection<EramClassDefault> SymbolDefaults { get; protected init; } = [];

	/// <summary>The Text defaults: one row per class, in use or not. Built by the derived tab's constructor.</summary>
	public ObservableCollection<EramClassDefault> TextDefaults { get; protected init; } = [];

	/// <inheritdoc />
	public bool HasCrcDefaultsInUse => _crcRowsInUse.Count > 0;

	/// <inheritdoc />
	public IReadOnlyList<EramClassDefault> LineDefaultsInUse => [.. LineDefaults.Where(IsCrcRowInUse)];

	/// <inheritdoc />
	public IReadOnlyList<EramClassDefault> SymbolDefaultsInUse => [.. SymbolDefaults.Where(IsCrcRowInUse)];

	/// <inheritdoc />
	public IReadOnlyList<EramClassDefault> TextDefaultsInUse => [.. TextDefaults.Where(IsCrcRowInUse)];

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

	/// <summary>
	/// What <see cref="RoiFallbackHint"/> says when no default ROI is set: what the run covers
	/// instead, e.g. that every airway is included.
	/// </summary>
	protected abstract string NoDefaultRoiHint { get; }

	/// <summary>
	/// The keys the three GeoJSON file choices are saved and sent under. The same key serves the
	/// config and the settings block. <c>Lines</c> is <see langword="null"/> for a sub-service
	/// with no Lines file (NAVAIDs): <see cref="EmitLines"/> is then always off and is never
	/// saved or sent.
	/// </summary>
	protected virtual (string? Lines, string Symbols, string Text) EmitKeys => ("EmitLines", "EmitSymbols", "EmitText");

	/// <summary>
	/// Every file the tab's current settings write, in the order the Upload to vNAS card lists
	/// them; files in the same <see cref="OutputFileOption.Group"/> share a row.
	/// </summary>
	/// <returns>The files, alias file last.</returns>
	protected abstract IEnumerable<OutputFileOption> OutputFiles();

	/// <summary>
	/// Every file key the user has marked for vNAS or CRC-ERAM defaults, including files the
	/// current settings do not write.
	/// </summary>
	protected IEnumerable<string> ChosenVnasFileKeys => _vnasFiles.Union(_crcFiles, StringComparer.OrdinalIgnoreCase);

	/// <summary>Where a CRC defaults row is saved under this tab's config node.</summary>
	/// <param name="row">The row.</param>
	/// <returns>The row's key prefix, e.g. <c>CrcEramPropertyDefaults.Airports_Symbol</c>.</returns>
	protected virtual string CrcConfigPrefix(EramClassDefault row) => $"CrcEramPropertyDefaults.{row.ClassName}_{row.Kind}";

	/// <inheritdoc />
	/// <remarks>Refreshes the vNAS and CRC cards first, so validation sees the files as they now are.</remarks>
	protected override void MarkDirty()
	{
		RefreshVnasFiles();
		base.MarkDirty();
	}

	/// <inheritdoc />
	/// <remarks>Refreshes the vNAS and CRC cards first, so the snapshot and validation see the files as they now are.</remarks>
	protected override void ClearDirty()
	{
		RefreshVnasFiles();
		base.ClearDirty();
	}

	/// <summary>
	/// Brings the Upload to vNAS and CRC ERAM Defaults cards in line with the current settings:
	/// the files listed, the vNAS GeoJSON files offered for CRC-ERAM defaults, and the CRC rows in
	/// use. Called on every change, so it rebuilds only what actually changed - a rebuilt row
	/// would take the focus from the box the user is typing in.
	/// </summary>
	/// <remarks>
	/// A derived tab calls this itself only when its file list changes outside a setting - when a
	/// cycle-dependent list arrives while the tab has unsaved edits, say.
	/// </remarks>
	protected void RefreshVnasFiles()
	{
		List<OutputFileOption> files = [.. OutputFiles()];
		string filesSignature = string.Join('|', files.Select(file => $"{file.Group}/{file.Key}"));
		bool rebuilt = filesSignature != _vnasFilesSignature;

		if (rebuilt)
		{
			_vnasFilesSignature = filesSignature;
			VnasFileRows.Clear();

			foreach (IGrouping<string, OutputFileOption> group in files.GroupBy(file => file.Group))
			{
				VnasFileRows.Add(new VnasFileRow(group.Key,
				[
					.. group.Select(file => new VnasFileToggle(
						file, _vnasFiles.Contains(file.Key), _crcFiles.Contains(file.Key), OnVnasFileToggled)),
				]));
			}

			OnPropertyChanged(nameof(HasOutputFiles));
		}

		// The CRC choices are the vNAS GeoJSON files, in the same rows and the same toggles.
		List<VnasFileRow> crcRows = [.. VnasFileRows
			.Select(row => new VnasFileRow(row.Label, [.. row.Files.Where(file => file.IsUploaded && file.IsGeojson)]))
			.Where(row => row.Files.Count > 0)];
		string crcSignature = string.Join('|', crcRows.SelectMany(row => row.Files).Select(file => file.Key));

		if (rebuilt || crcSignature != _crcFilesSignature)
		{
			_crcFilesSignature = crcSignature;
			CrcFileRows.Clear();

			foreach (VnasFileRow row in crcRows)
			{
				CrcFileRows.Add(row);
			}

			OnPropertyChanged(nameof(HasVnasGeojsonFiles));
		}

		HashSet<(string ClassName, EramFieldKind Kind)> inUse = [.. CrcFiles().SelectMany(file => file.File.CrcRows)];

		if (!inUse.SetEquals(_crcRowsInUse))
		{
			_crcRowsInUse.Clear();
			_crcRowsInUse.UnionWith(inUse);

			OnPropertyChanged(nameof(HasCrcDefaultsInUse));
			OnPropertyChanged(nameof(LineDefaultsInUse));
			OnPropertyChanged(nameof(SymbolDefaultsInUse));
			OnPropertyChanged(nameof(TextDefaultsInUse));
		}
	}

	/// <summary>
	/// Restores the shared settings from config, without marking the tab dirty. Call from
	/// <see cref="SubServiceSettingsViewModel.LoadFromConfig"/> before <see cref="ServiceTabViewModel.ClearDirty"/>.
	/// </summary>
	protected void LoadSharedSettings()
	{
		_generateAliasFile = GetBool("GenerateAliasFile", true);
		_emitLines = EmitKeys.Lines is { } linesKey && GetBool(linesKey, true);
		_emitSymbols = GetBool(EmitKeys.Symbols, true);
		_emitText = GetBool(EmitKeys.Text, true);
		_includeFebCustomProperties = GetBool("IncludeFebCustomProperties", false);

		HashSet<string> selected = ParseList(Get("FebProperties"));
		foreach (FebPropertyToggle toggle in FebProperties)
		{
			toggle.IsSelected = selected.Contains(toggle.Name);
		}

		_vnasFiles.Clear();
		_vnasFiles.UnionWith(ParseList(Get(VnasFilesKey)));
		_crcFiles.Clear();
		_crcFiles.UnionWith(ParseList(Get(CrcFilesKey)));

		// By name only; anything else (a number, a typo) falls back to no CRC-ERAM defaults.
		string? savedScope = Get(CrcDefaultsScopeKey)?.Trim();
		_crcDefaultsScope = Enum.GetValues<CrcDefaultsScope>()
			.FirstOrDefault(scope => scope.ToString().Equals(savedScope, StringComparison.OrdinalIgnoreCase));

		// Rebuild the toggles from the reloaded choices on the next refresh.
		_vnasFilesSignature = null;

		foreach (EramClassDefault row in AllCrcRows())
		{
			CrcDefaultsRowIo.Load(row, CrcConfigPrefix(row), Get);
		}

		_overrideRoi = GetBool(OverrideRoiKey, false);
		_swLat = Get($"{OverrideCornersNode}.SwLat") ?? string.Empty;
		_swLon = Get($"{OverrideCornersNode}.SwLon") ?? string.Empty;
		_neLat = Get($"{OverrideCornersNode}.NeLat") ?? string.Empty;
		_neLon = Get($"{OverrideCornersNode}.NeLon") ?? string.Empty;

		foreach (string name in new[]
		{
			nameof(GenerateAliasFile), nameof(EmitLines), nameof(EmitSymbols), nameof(EmitText),
			nameof(IncludeFebCustomProperties), nameof(CrcDefaultsScope), nameof(IsCrcDefaultsSpecific),
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

		if (EmitKeys.Lines is { } linesKey)
		{
			Set(linesKey, YesNo(EmitLines));
		}

		Set(EmitKeys.Symbols, YesNo(EmitSymbols));
		Set(EmitKeys.Text, YesNo(EmitText));
		Set("IncludeFebCustomProperties", YesNo(IncludeFebCustomProperties));
		Set("FebProperties", SelectedFebPropertyNames());

		// Every choice, written or not, so a file switched off and on again keeps it.
		Set(VnasFilesKey, string.Join(',', _vnasFiles.Order(StringComparer.OrdinalIgnoreCase)));
		Set(CrcDefaultsScopeKey, CrcDefaultsScope.ToString());
		Set(CrcFilesKey, string.Join(',', _crcFiles.Order(StringComparer.OrdinalIgnoreCase)));

		foreach (EramClassDefault row in AllCrcRows())
		{
			CrcDefaultsRowIo.Save(row, CrcConfigPrefix(row), Set);
		}

		Set(OverrideRoiKey, YesNo(OverrideRoi));
		Set($"{OverrideCornersNode}.SwLat", SwLat);
		Set($"{OverrideCornersNode}.SwLon", SwLon);
		Set($"{OverrideCornersNode}.NeLat", NeLat);
		Set($"{OverrideCornersNode}.NeLon", NeLon);
	}

	/// <summary>
	/// Adds the settings every GeoJSON sub-service's parser reads the same way: coordinate
	/// precision, alias file, file choices, FE-Buddy properties, the vNAS files, the CRC defaults
	/// they need, and the region of interest. The AIRAC Service adds the output folder itself.
	/// </summary>
	/// <param name="settings">The settings block being built.</param>
	protected void AddSharedSettings(Dictionary<string, string> settings)
	{
		settings["CoordinatePrecision"] = OutputPreferences.CoordinatePrecision.ToString(CultureInfo.InvariantCulture);
		settings["GenerateAliasFile"] = YesNo(GenerateAliasFile);

		if (EmitKeys.Lines is { } linesKey)
		{
			settings[linesKey] = YesNo(EmitLines);
		}

		settings[EmitKeys.Symbols] = YesNo(EmitSymbols);
		settings[EmitKeys.Text] = YesNo(EmitText);
		settings["IncludeFebCustomProperties"] = YesNo(IncludeFebCustomProperties);
		settings["FebProperties"] = SelectedFebPropertyNames();

		// Only files that are actually written, so the parser sees exactly what the run will do.
		settings["UploadToVnas"] = string.Join(',', UploadedFiles().Select(file => file.Key));
		settings["CrcDefaultsFor"] = string.Join(',', CrcFiles().Select(file => file.Key));

		// Only the rows those files need: the parser requires exactly those, and would only
		// ignore any others.
		foreach (EramClassDefault row in AllCrcRows().Where(IsCrcRowInUse))
		{
			CrcDefaultsRowIo.AddToSettingsBlock(row, settings);
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
	/// Validates the shared settings: the FE-Buddy properties, the CRC-ERAM choice, the CRC
	/// defaults actually in use, and the ROI override. Call from <see cref="ServiceTabViewModel.Validate"/>.
	/// </summary>
	/// <param name="validation">The collector to add failures to.</param>
	protected void ValidateSharedSettings(ServiceValidation validation)
	{
		if (IncludeFebCustomProperties && FebProperties.All(p => !p.IsSelected))
		{
			validation.Add("FE-Buddy properties are on but none are selected. Pick at least one, or switch them off.");
		}

		if (IsCrcDefaultsSpecific && HasVnasGeojsonFiles && !CrcFiles().Any())
		{
			validation.Add(NoCrcFilesMessage);
		}

		// Only the rows a file that gets CRC-ERAM defaults needs; any other row is never read,
		// so it is not held against the user.
		foreach (EramClassDefault row in AllCrcRows())
		{
			row.IsRequired = IsCrcRowInUse(row);
		}

		if (AllCrcRows().Any(row => row.IsRequired && row.HasMissingValues))
		{
			validation.Add(CrcDefaultsIncompleteMessage);
		}

		ValidateRoiOverride(validation);
	}

	/// <summary>The files going to vNAS, for the Preview Settings tab.</summary>
	/// <returns>e.g. <c>Airways_High_Lines, Airways.txt</c>, or <c>None</c>.</returns>
	protected string DescribeVnasFiles() => DescribeFiles(UploadedFiles());

	/// <summary>The files that get CRC-ERAM defaults, for the Preview Settings tab.</summary>
	/// <returns>e.g. <c>Airways_High_Lines</c>, or <c>None</c>.</returns>
	protected string DescribeCrcDefaults() => DescribeFiles(CrcFiles());

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

	private bool IsCrcRowInUse(EramClassDefault row) => _crcRowsInUse.Contains((row.ClassName, row.Kind));

	/// <summary>The files marked for vNAS that the current settings actually write.</summary>
	private IEnumerable<VnasFileToggle> UploadedFiles() =>
		VnasFileRows.SelectMany(row => row.Files).Where(file => file.IsUploaded);

	/// <summary>The files that actually get CRC-ERAM defaults: vNAS GeoJSON files, as <see cref="CrcDefaultsScope"/> says.</summary>
	private IEnumerable<VnasFileToggle> CrcFiles() => CrcDefaultsScope switch
	{
		CrcDefaultsScope.AllVnasFiles => UploadedFiles().Where(file => file.IsGeojson),
		CrcDefaultsScope.SpecificFiles => UploadedFiles().Where(file => file.IsGeojson && file.HasCrcDefaults),
		_ => [],
	};

	private static string DescribeFiles(IEnumerable<VnasFileToggle> files)
	{
		string[] names = [.. files.Select(file => file.File.DisplayName)];
		return names.Length > 0 ? string.Join(", ", names) : "None";
	}

	private void OnVnasFileToggled(VnasFileToggle toggle)
	{
		SetMembership(_vnasFiles, toggle.Key, toggle.IsUploaded);
		SetMembership(_crcFiles, toggle.Key, toggle.HasCrcDefaults);
		MarkDirty();
	}

	private static void SetMembership(HashSet<string> set, string key, bool isMember)
	{
		if (isMember)
		{
			set.Add(key);
		}
		else
		{
			set.Remove(key);
		}
	}

	private string SelectedFebPropertyNames() =>
		string.Join(',', FebProperties.Where(p => p.IsSelected).Select(p => p.Name));

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
}
