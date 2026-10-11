using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Infrastructure.Configuration;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// Base for a sub-service tab that writes GeoJSON (Airports, Airways, Departures, Arrivals, NAVAIDs,
/// ARTCC Boundaries, Fixes, Wx Stations): everything those tabs share - the alias file, which GeoJSON files are
/// written, the FE-Buddy properties, the area (its ARTCCs, an ROI, or everything), and which files
/// carry CRC ERAM defaults and their values - with its config, settings-block and validation plumbing.
/// </summary>
/// <remarks>
/// <para>
/// Also the base for Procedures and Telephony, which write no GeoJSON at all: they still want the
/// shared alias file and outputs plumbing (and Procedures the area), so they derive from this class
/// too, with <see cref="EmitKeys"/> <c>(null, null, null)</c> and <see cref="OutputFiles"/> listing
/// only the alias file - they never show the What Files Do You Want?, FE-Buddy Properties or CRC
/// ERAM Defaults cards.
/// </para>
/// <para>
/// A derived tab builds <see cref="FebProperties"/> and the three CRC defaults lists in its
/// constructor, lists the files its settings write in <see cref="OutputFiles"/>, calls
/// <see cref="LoadSharedSettings"/> from <see cref="SubServiceSettingsViewModel.LoadFromConfig"/>,
/// <see cref="SaveSharedSettings"/> from <see cref="SubServiceSettingsViewModel.WriteToConfig"/>,
/// <see cref="AddSharedSettings"/> from its settings block, and <see cref="ValidateSharedSettings"/>
/// from <see cref="ServiceTabViewModel.Validate"/>.
/// </para>
/// <para>
/// The area is one choice (<see cref="Area"/>, from <see cref="Areas"/>): only that filter applies.
/// Its ROI is this tab's own when <see cref="OverrideRoi"/> is on, otherwise the shared default ROI
/// (<see cref="DefaultRoiStore"/>). A tab that has never saved an area starts on the one its other
/// settings meant (<see cref="UserConfigAreas.DefaultFor"/>).
/// </para>
/// <para>
/// The CRC-ERAM file choices are kept as a set of file keys, including files the current settings
/// do not write (the Symbols files while Symbols is off, say), so switching a file off and on again
/// keeps its choice. Only the files actually written are shown, sent to a run, and have their CRC
/// values required.
/// </para>
/// </remarks>
public abstract class GeojsonSubServiceViewModel : SubServiceSettingsViewModel,
	IOutputSettings, IGeojsonFileChoices, IFebPropertySettings, ICrcDefaultsChoice, ICrcDefaultsSettings, IAreaSettings
{
	private const string CrcDefaultsIncompleteMessage =
		"Some CRC ERAM default values are empty or invalid. Fix the marked boxes, or take CRC-ERAM defaults off those files at the top of the CRC ERAM Defaults card.";

	private const string NoCrcFilesMessage =
		"CRC-ERAM defaults are set to go on specific files, but none is ticked. Tick at least one, or choose No CRC-ERAM defaults.";

	private const string OverrideRoiKey = "Roi.OverrideDefaultRoi";

	private const string OverrideCornersNode = "Roi.OverrideCorners";

	private const string CrcDefaultsScopeKey = "CrcDefaultsScope";
	private const string CrcFilesKey = "CrcDefaultsFiles";

	private readonly HashSet<string> _crcFiles = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<(string ClassName, EramFieldKind Kind)> _crcRowsInUse = [];

	private ISubServiceOutputs _outputs = new EveryOutput();
	private bool _emitLines = true;
	private bool _emitSymbols = true;
	private bool _emitText = true;
	private bool _includeFebCustomProperties;
	private CrcDefaultsScope _crcDefaultsScope = CrcDefaultsScope.None;
	private string? _filesSignature;
	private SubServiceArea _area = SubServiceArea.Everything;
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
		DefaultRoiStore.Changed += (_, _) => RaiseRoiChanged();
	}

	/// <summary>Raised whenever the files the tab's settings write may have changed, so the screen can follow them.</summary>
	public event EventHandler? FilesChanged;

	// ================= outputs and files =================

	/// <summary>
	/// Which outputs are on, from the General tab's table. A tab built on its own (a test) has
	/// every output on.
	/// </summary>
	public ISubServiceOutputs Outputs => _outputs;

	/// <inheritdoc />
	/// <remarks>Turned on and off on the General tab.</remarks>
	public bool GenerateAliasFile => HasAliasFile && _outputs.Alias;

	/// <inheritdoc />
	/// <remarks>Turned on and off on the General tab. Always off for a sub-service with no GeoJSON choice.</remarks>
	public virtual bool GenerateGeojson => _outputs.Geojson;

	/// <inheritdoc />
	public override SubServiceOutputKinds OutputsOn =>
		(GenerateAliasFile ? SubServiceOutputKinds.Alias : SubServiceOutputKinds.None)
		| (GenerateGeojson ? SubServiceOutputKinds.Geojson : SubServiceOutputKinds.None)
		| (_outputs.ProcedureChanges ? SubServiceOutputKinds.ProcedureChanges : SubServiceOutputKinds.None)
		| (_outputs.ProceduresJson ? SubServiceOutputKinds.ProceduresJson : SubServiceOutputKinds.None);

	/// <summary>Takes the tab's outputs from the General tab's row for it, and follows them.</summary>
	/// <param name="outputs">The row.</param>
	public void AttachOutputs(ISubServiceOutputs outputs)
	{
		ArgumentNullException.ThrowIfNull(outputs);

		_outputs.PropertyChanged -= OnOutputsChanged;
		_outputs = outputs;
		_outputs.PropertyChanged += OnOutputsChanged;
		OnOutputsChanged();
	}

	/// <inheritdoc />
	public bool EmitLines { get => _emitLines; set { if (SetProperty(ref _emitLines, value)) MarkDirty(); } }

	/// <inheritdoc />
	public bool EmitSymbols { get => _emitSymbols; set { if (SetProperty(ref _emitSymbols, value)) MarkDirty(); } }

	/// <inheritdoc />
	public bool EmitText { get => _emitText; set { if (SetProperty(ref _emitText, value)) MarkDirty(); } }

	/// <summary>Whether the tab's current settings write its alias file.</summary>
	public bool WritesAliasFile => HasAliasFile && GenerateAliasFile;

	/// <inheritdoc />
	/// <remarks>
	/// Names GeoJSON, and Alias too for a sub-service with an alias file. Procedures and Telephony,
	/// whose outputs are not these, override it.
	/// </remarks>
	public override string? WhatYoullGetFooter =>
		$"Turn {(HasAliasFile ? "GeoJSON and Alias" : "GeoJSON")} output on or off on the General tab, to the left.";

	/// <summary>
	/// Every file the tab's current settings write, for the File Names tab: its key, the folder it
	/// goes in inside the cycle folder, and FE-Buddy's name for it.
	/// </summary>
	/// <returns>The files, GeoJSON first and the alias file last.</returns>
	public virtual IEnumerable<OutputFileEntry> OutputFileEntries() =>
		OutputFiles().Select(file => OutputFileEntry.Renamable(file.Key, FolderOf(file), Title));

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

	// ================= which files get CRC ERAM defaults =================

	/// <summary>Whether the tab's current settings write any GeoJSON file, so CRC-ERAM defaults apply.</summary>
	public bool HasGeojsonFiles => CrcFileRows.Count > 0;

	/// <inheritdoc />
	/// <remarks>While the tab writes a GeoJSON file.</remarks>
	public bool ShowsCrcDefaultsCard => HasGeojsonFiles;

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
	public ObservableCollection<CrcFileRow> CrcFileRows { get; } = [];

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

	// ================= area =================

	/// <summary>
	/// Which area filter the run uses - one of <see cref="Areas"/>. Setting one the tab doesn't offer
	/// does nothing.
	/// </summary>
	public SubServiceArea Area
	{
		get => _area;
		set
		{
			if (Areas.Contains(value) && SetProperty(ref _area, value))
			{
				RaiseAreaChoices();
				MarkDirty();
			}
		}
	}

	/// <inheritdoc />
	public bool OffersArtccs => Areas.Contains(SubServiceArea.Artccs);

	/// <inheritdoc />
	public bool OffersNone => Areas.Contains(SubServiceArea.None);

	/// <inheritdoc />
	public bool AreaArtccs { get => _area == SubServiceArea.Artccs; set { if (value) Area = SubServiceArea.Artccs; } }

	/// <inheritdoc />
	public bool AreaRoi { get => _area == SubServiceArea.Roi; set { if (value) Area = SubServiceArea.Roi; } }

	/// <inheritdoc />
	public bool AreaEverything { get => _area == SubServiceArea.Everything; set { if (value) Area = SubServiceArea.Everything; } }

	/// <inheritdoc />
	public bool AreaNone { get => _area == SubServiceArea.None; set { if (value) Area = SubServiceArea.None; } }

	/// <summary>
	/// The outputs the area narrows on this tab, for the Area card's tags: the GeoJSON unless the tab
	/// says otherwise. While they are all off the card greys out and the area isn't checked.
	/// </summary>
	public virtual SubServiceOutputKinds AreaOutputs => SubServiceOutputKinds.Geojson;

	/// <inheritdoc />
	public bool UseDefaultRoi { get => !_overrideRoi; set { if (value) OverrideRoi = false; } }

	/// <inheritdoc />
	public bool OverrideRoi
	{
		get => _overrideRoi;
		set
		{
			if (SetProperty(ref _overrideRoi, value))
			{
				OnPropertyChanged(nameof(UseDefaultRoi));
				MarkDirty();
				RaiseRoiChanged();
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
	public string DefaultRoiSummary => DefaultRoiStore.Load() is { } roi
		? $"{Corners(roi)}. Change it in Settings or on the Map page."
		: $"No default ROI is set. Set one in Settings or on the Map page, or pick \"An ROI specific to {Title}\" below.";

	/// <inheritdoc />
	public bool HasDefaultRoi => DefaultRoiStore.Load() is not null;

	/// <summary>Whether the run uses an ROI: the area is the ROI. Which one is <see cref="RoiName"/>.</summary>
	public bool HasRoi => _area == SubServiceArea.Roi;

	/// <summary>
	/// Which ROI the run uses, for a "What You'll Get" line: <c>the default ROI</c>, or this
	/// sub-service's own, e.g. <c>the ROI specific to the Airports sub-service</c>.
	/// </summary>
	protected string RoiName => OverrideRoi ? $"the ROI specific to the {Title} sub-service" : "the default ROI";

	/// <summary>
	/// A "What You'll Get" line about the ROI - what it keeps, naming which ROI it is - or
	/// <see langword="null"/> while the area isn't the ROI, so it leaves nothing out.
	/// </summary>
	/// <param name="keeps">What the ROI keeps, given its name, e.g. <c>roi => $"those inside {roi}"</c>.</param>
	/// <returns>e.g. <c>those inside the default ROI</c>.</returns>
	protected string? RoiLine(Func<string, string> keeps) => HasRoi ? keeps(RoiName) : null;

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
	/// The areas the tab offers on its Area card: <see cref="SubServiceSettingsReader.ArtccOrRoiAreas"/>,
	/// <see cref="SubServiceSettingsReader.RoiAreas"/>, or Procedures' four. None (the default) for a
	/// tab with no area, which then neither saves nor sends one, nor an ROI.
	/// </summary>
	protected virtual IReadOnlyList<SubServiceArea> Areas => [];

	/// <summary>
	/// The ARTCCs ticked for the ARTCCs area (Procedures: the facilities), or - before the cycle's
	/// list is built - the saved ones. The base has none.
	/// </summary>
	/// <returns>Their IDs.</returns>
	protected virtual IEnumerable<string> AreaArtccIds() => [];

	/// <summary>What the ARTCCs area ticks, for its messages: <c>ARTCC</c>, or <c>facility</c> on Procedures.</summary>
	protected virtual string AreaArtccNoun => "ARTCC";

	/// <summary>
	/// Whether the area matters right now: while any output it narrows (<see cref="AreaOutputs"/>) is
	/// on. Only then is it checked.
	/// </summary>
	protected virtual bool IsAreaInUse => (AreaOutputs & OutputsOn) != SubServiceOutputKinds.None;

	/// <summary>After the area changes, for a tab with more that follows from it. The base does nothing.</summary>
	protected virtual void OnAreaChanged()
	{
	}

	/// <summary>
	/// The keys the three GeoJSON file choices are saved and sent under. The same key serves the
	/// config and the settings block. A key is <see langword="null"/> when the sub-service has no
	/// choice for that kind - NAVAIDs has no Lines file; ARTCC Boundaries writes Lines only, so
	/// has no choice at all. Such a choice is always off, and is never saved or sent.
	/// </summary>
	protected virtual (string? Lines, string? Symbols, string? Text) EmitKeys => ("EmitLines", "EmitSymbols", "EmitText");

	/// <summary>
	/// Whether the sub-service has an alias file. When <see langword="false"/> (ARTCC Boundaries),
	/// <see cref="GenerateAliasFile"/> is always off and is never sent.
	/// </summary>
	protected virtual bool HasAliasFile => true;

	/// <summary>
	/// After the General tab turns an output on or off: the properties that follow from the outputs
	/// are announced, the file lists rebuilt and the tab re-checked. The tab isn't marked unsaved:
	/// the outputs are the General tab's to save.
	/// </summary>
	/// <remarks>A tab with outputs of its own to announce (<c>GenerateGeojson</c>) overrides this and calls the base.</remarks>
	protected virtual void OnOutputsChanged()
	{
		OnPropertyChanged(nameof(GenerateAliasFile));
		OnPropertyChanged(nameof(GenerateGeojson));
		OnPropertyChanged(nameof(WritesAliasFile));
		OnPropertyChanged(nameof(OutputsOn));
		RefreshOutputFiles();
		Revalidate();
	}

	/// <summary>
	/// Every file the tab's current settings write, in the order the CRC ERAM Defaults card and the
	/// File Names tab list them; GeoJSON files in the same <see cref="OutputFileOption.Group"/> share
	/// a row on the card.
	/// </summary>
	/// <returns>The files, alias file last.</returns>
	protected abstract IEnumerable<OutputFileOption> OutputFiles();

	/// <summary>
	/// Every file key the user has ticked for CRC-ERAM defaults, including files the current settings
	/// do not write.
	/// </summary>
	protected IEnumerable<string> ChosenCrcFileKeys => _crcFiles;

	/// <summary>The folder a file goes in inside the cycle folder: <c>Geojson</c>, or <c>Aliases</c> for the alias file.</summary>
	/// <param name="file">One of the files <see cref="OutputFiles"/> lists.</param>
	/// <returns>The folder, relative to the cycle folder.</returns>
	protected static string FolderOf(OutputFileOption file)
	{
		ArgumentNullException.ThrowIfNull(file);

		return file.IsGeojson ? AiracOutputPaths.GeojsonFolder : AiracOutputPaths.AliasFolder;
	}

	/// <summary>Where a CRC defaults row is saved under this tab's config node.</summary>
	/// <param name="row">The row.</param>
	/// <returns>The row's key prefix, e.g. <c>CrcEramPropertyDefaults.Airports_Symbol</c>.</returns>
	private static string CrcConfigPrefix(EramClassDefault row) => $"CrcEramPropertyDefaults.{row.ClassName}_{row.Kind}";

	/// <summary>
	/// Adds CRC defaults rows after construction, for a tab whose classes come from the cycle's
	/// data (ARTCC Boundaries: one class per ARTCC and altitude). Each new row is filled from the
	/// saved config first; a row whose class and kind are already there is left alone.
	/// </summary>
	/// <param name="rows">The rows to add, each built with the tab's <c>MarkDirty</c> as its change callback.</param>
	/// <remarks>
	/// Rows are only ever added, never removed, so a class that drops out of a later cycle keeps its
	/// saved values. Call <c>ResyncSavedState</c> afterwards if the tab has no unsaved edits.
	/// </remarks>
	protected void AddCrcRows(IEnumerable<EramClassDefault> rows)
	{
		ArgumentNullException.ThrowIfNull(rows);

		foreach (EramClassDefault row in rows)
		{
			ObservableCollection<EramClassDefault> list = row.Kind switch
			{
				EramFieldKind.Line => LineDefaults,
				EramFieldKind.Symbol => SymbolDefaults,
				_ => TextDefaults,
			};

			if (list.Any(existing => existing.ClassName.Equals(row.ClassName, StringComparison.OrdinalIgnoreCase)))
			{
				continue;
			}

			CrcDefaultsRowIo.Load(row, CrcConfigPrefix(row), Get);
			list.Add(row);
		}
	}

	/// <inheritdoc />
	/// <remarks>Refreshes the CRC ERAM Defaults card first, so validation sees the files as they now are.</remarks>
	protected override void MarkDirty()
	{
		RefreshOutputFiles();
		base.MarkDirty();
	}

	/// <inheritdoc />
	/// <remarks>Refreshes the CRC ERAM Defaults card first, so the snapshot and validation see the files as they now are.</remarks>
	protected override void ClearDirty()
	{
		RefreshOutputFiles();
		base.ClearDirty();
	}

	/// <summary>
	/// Brings the CRC ERAM Defaults card in line with the current settings: the GeoJSON files offered
	/// for CRC-ERAM defaults, and the CRC rows in use. Called on every change, so it rebuilds only what
	/// actually changed - a rebuilt row would take the focus from the box the user is typing in.
	/// </summary>
	/// <remarks>
	/// A derived tab calls this itself only when its file list changes outside a setting - when a
	/// cycle-dependent list arrives while the tab has unsaved edits, say.
	/// </remarks>
	protected void RefreshOutputFiles()
	{
		List<OutputFileOption> files = [.. OutputFiles().Where(file => file.IsGeojson)];
		string filesSignature = string.Join('|', files.Select(file => $"{file.Group}/{file.Key}"));

		if (filesSignature != _filesSignature)
		{
			_filesSignature = filesSignature;
			CrcFileRows.Clear();

			foreach (IGrouping<string, OutputFileOption> group in files.GroupBy(file => file.Group))
			{
				CrcFileRows.Add(new CrcFileRow(group.Key,
				[
					.. group.Select(file => new CrcFileToggle(file, _crcFiles.Contains(file.Key), OnCrcFileToggled)),
				]));
			}

			OnPropertyChanged(nameof(HasGeojsonFiles));
			OnPropertyChanged(nameof(ShowsCrcDefaultsCard));
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

		FilesChanged?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Restores the shared settings from config, without marking the tab dirty. Call from
	/// <see cref="SubServiceSettingsViewModel.LoadFromConfig"/> before <see cref="ServiceTabViewModel.ClearDirty"/>.
	/// </summary>
	protected void LoadSharedSettings()
	{
		_emitLines = EmitKeys.Lines is { } linesKey && GetBool(linesKey, true);
		_emitSymbols = EmitKeys.Symbols is { } symbolsKey && GetBool(symbolsKey, true);
		_emitText = EmitKeys.Text is { } textKey && GetBool(textKey, true);
		_includeFebCustomProperties = GetBool("IncludeFebCustomProperties", false);

		HashSet<string> selected = ParseList(Get("FebProperties"));
		foreach (FebPropertyToggle toggle in FebProperties)
		{
			toggle.IsSelected = selected.Contains(toggle.Name);
		}

		_crcFiles.Clear();
		_crcFiles.UnionWith(ParseList(Get(CrcFilesKey)));

		// By name only; anything else (a number, a typo) falls back to no CRC-ERAM defaults.
		string? savedScope = Get(CrcDefaultsScopeKey)?.Trim();
		_crcDefaultsScope = Enum.GetValues<CrcDefaultsScope>()
			.FirstOrDefault(scope => scope.ToString().Equals(savedScope, StringComparison.OrdinalIgnoreCase));

		// Rebuild the toggles from the reloaded choices on the next refresh.
		_filesSignature = null;

		foreach (EramClassDefault row in AllCrcRows())
		{
			CrcDefaultsRowIo.Load(row, CrcConfigPrefix(row), Get);
		}

		if (Areas.Count > 0)
		{
			_area = SavedArea();
			_overrideRoi = GetBool(OverrideRoiKey, false);
			_swLat = Get($"{OverrideCornersNode}.SwLat") ?? string.Empty;
			_swLon = Get($"{OverrideCornersNode}.SwLon") ?? string.Empty;
			_neLat = Get($"{OverrideCornersNode}.NeLat") ?? string.Empty;
			_neLon = Get($"{OverrideCornersNode}.NeLon") ?? string.Empty;
		}

		foreach (string name in new[]
		{
			nameof(EmitLines), nameof(EmitSymbols), nameof(EmitText),
			nameof(IncludeFebCustomProperties), nameof(CrcDefaultsScope), nameof(IsCrcDefaultsSpecific),
			nameof(OverrideRoi), nameof(UseDefaultRoi), nameof(SwLat), nameof(SwLon), nameof(NeLat), nameof(NeLon),
		})
		{
			OnPropertyChanged(name);
		}

		RaiseAreaChoices();
		RaiseRoiChanged();
	}

	/// <summary>Writes the shared settings. Call from <see cref="SubServiceSettingsViewModel.WriteToConfig"/>.</summary>
	protected void SaveSharedSettings()
	{
		foreach ((string? key, bool emit) in EmitChoices())
		{
			if (key is not null)
			{
				Set(key, YesNo(emit));
			}
		}

		Set("IncludeFebCustomProperties", YesNo(IncludeFebCustomProperties));
		Set("FebProperties", SelectedFebPropertyNames());

		Set(CrcDefaultsScopeKey, CrcDefaultsScope.ToString());

		// Every choice, written or not, so a file switched off and on again keeps it.
		Set(CrcFilesKey, string.Join(',', _crcFiles.Order(StringComparer.OrdinalIgnoreCase)));

		foreach (EramClassDefault row in AllCrcRows())
		{
			CrcDefaultsRowIo.Save(row, CrcConfigPrefix(row), Set);
		}

		// The ARTCCs and the ROI are kept whichever area is picked, so picking it again gets them back.
		if (Areas.Count > 0)
		{
			Set(UserConfigAreas.AreaKey, Area.ToString());
			Set(OverrideRoiKey, YesNo(OverrideRoi));
			Set($"{OverrideCornersNode}.SwLat", SwLat);
			Set($"{OverrideCornersNode}.SwLon", SwLon);
			Set($"{OverrideCornersNode}.NeLat", NeLat);
			Set($"{OverrideCornersNode}.NeLon", NeLon);
		}
	}

	/// <summary>
	/// Adds the settings every GeoJSON sub-service's parser reads the same way: coordinate
	/// precision, alias file, file choices, FE-Buddy properties, the files that get CRC-ERAM defaults
	/// and the values they need, and the area with its ROI. The tab adds its ARTCCs itself. The AIRAC
	/// Service adds the output folder.
	/// </summary>
	/// <param name="settings">The settings block being built.</param>
	protected void AddSharedSettings(Dictionary<string, string> settings)
	{
		settings["CoordinatePrecision"] = OutputPreferences.CoordinatePrecision.ToString(CultureInfo.InvariantCulture);

		if (HasAliasFile)
		{
			settings["GenerateAliasFile"] = YesNo(GenerateAliasFile);
		}

		foreach ((string? key, bool emit) in EmitChoices())
		{
			if (key is not null)
			{
				settings[key] = YesNo(emit);
			}
		}

		settings["IncludeFebCustomProperties"] = YesNo(IncludeFebCustomProperties);
		settings["FebProperties"] = SelectedFebPropertyNames();

		// Only files that are actually written, so the parser sees exactly what the run will do.
		settings["CrcDefaultsFor"] = string.Join(',', CrcFiles().Select(file => file.Key));

		// Only the rows those files need: the parser requires exactly those, and would only
		// ignore any others.
		foreach (EramClassDefault row in AllCrcRows().Where(IsCrcRowInUse))
		{
			CrcDefaultsRowIo.AddToSettingsBlock(row, settings);
		}

		if (Areas.Count == 0)
		{
			return;
		}

		settings[SubServiceSettingsReader.AreaKey] = Area.ToString();

		// The ROI only while the area is the ROI, so the parser sees exactly what the run will do.
		RegionOfInterest? defaultRoi = Area == SubServiceArea.Roi && !OverrideRoi ? DefaultRoiStore.Load() : null;
		settings["FilterByRoi"] = YesNo(Area == SubServiceArea.Roi && (OverrideRoi || defaultRoi is not null));

		if (Area != SubServiceArea.Roi)
		{
			return;
		}

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
	/// defaults actually in use, and the area. Call from <see cref="ServiceTabViewModel.Validate"/>.
	/// </summary>
	/// <param name="validation">The collector to add failures to.</param>
	protected void ValidateSharedSettings(ServiceValidation validation)
	{
		if (IncludeFebCustomProperties && FebProperties.All(p => !p.IsSelected))
		{
			validation.AddArea(ServiceAreas.FebProperties, "FE-Buddy properties are on but none are selected. Pick at least one, or switch them off.");
		}

		if (IsCrcDefaultsSpecific && HasGeojsonFiles && !CrcFiles().Any())
		{
			validation.AddArea(ServiceAreas.CrcDefaults, NoCrcFilesMessage);
		}

		// Only the rows a file that gets CRC-ERAM defaults needs; any other row is never read,
		// so it is not held against the user.
		foreach (EramClassDefault row in AllCrcRows())
		{
			row.IsRequired = IsCrcRowInUse(row);
		}

		if (AllCrcRows().Any(row => row.IsRequired && row.HasMissingValues))
		{
			validation.AddArea(ServiceAreas.CrcDefaults, CrcDefaultsIncompleteMessage);
		}

		ValidateArea(validation);
	}

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

	/// <summary>The area in use, stated plainly for the Preview Settings tab's Area row.</summary>
	/// <returns>e.g. <c>ARTCCs: ZOB, ZNY</c>, <c>The default ROI: SW … / NE …</c> or <c>Everything</c>.</returns>
	protected string DescribeArea()
	{
		string[] artccs = [.. AreaArtccIds()];
		string artccsLabel = OffersNone ? "Facilities" : "ARTCCs";

		return Area switch
		{
			SubServiceArea.Artccs => artccs.Length > 0 ? $"{artccsLabel}: {string.Join(", ", artccs)}" : $"{artccsLabel}: none ticked",
			SubServiceArea.Roi when OverrideRoi => $"The ROI specific to {Title}: SW {SwLat}, {SwLon} / NE {NeLat}, {NeLon}",
			SubServiceArea.Roi => DefaultRoiStore.Load() is { } roi ? $"The default ROI: {Corners(roi)}" : "The default ROI, which isn't set",
			SubServiceArea.None => "None: only what you list",
			_ => "Everything",
		};
	}

	/// <summary>
	/// The ROI the area uses, whether or not the area is the ROI: the tab's own corners
	/// (<see langword="null"/> while they don't all parse), otherwise the default ROI, otherwise
	/// <see langword="null"/>.
	/// </summary>
	protected RegionOfInterest? RoiInUse() => OverrideRoi ? TryReadOverrideCorners() : DefaultRoiStore.Load();

	// ================= private =================

	private IEnumerable<EramClassDefault> AllCrcRows() => LineDefaults.Concat(SymbolDefaults).Concat(TextDefaults);

	/// <summary>Each GeoJSON file choice with its key (<see langword="null"/> when the sub-service has no such choice).</summary>
	private IEnumerable<(string? Key, bool Emit)> EmitChoices() =>
		[(EmitKeys.Lines, EmitLines), (EmitKeys.Symbols, EmitSymbols), (EmitKeys.Text, EmitText)];

	private bool IsCrcRowInUse(EramClassDefault row) => _crcRowsInUse.Contains((row.ClassName, row.Kind));

	/// <summary>The GeoJSON files the current settings write that get CRC-ERAM defaults, as <see cref="CrcDefaultsScope"/> says.</summary>
	private IEnumerable<CrcFileToggle> CrcFiles()
	{
		IEnumerable<CrcFileToggle> files = CrcFileRows.SelectMany(row => row.Files);

		return CrcDefaultsScope switch
		{
			CrcDefaultsScope.AllGeojsonFiles => files,
			CrcDefaultsScope.SpecificFiles => files.Where(file => file.HasCrcDefaults),
			_ => [],
		};
	}

	private static string DescribeFiles(IEnumerable<CrcFileToggle> files)
	{
		string[] names = [.. files.Select(file => file.File.DisplayName)];
		return names.Length > 0 ? string.Join(", ", names) : "None";
	}

	private void OnCrcFileToggled(CrcFileToggle toggle)
	{
		if (toggle.HasCrcDefaults)
		{
			_crcFiles.Add(toggle.Key);
		}
		else
		{
			_crcFiles.Remove(toggle.Key);
		}

		MarkDirty();
	}

	private string SelectedFebPropertyNames() =>
		string.Join(',', FebProperties.Where(p => p.IsSelected).Select(p => p.Name));

	/// <summary>
	/// Checks the area while it matters (<see cref="IsAreaInUse"/>): the ARTCCs area needs one ticked,
	/// and the ROI area an ROI - the default one set, or the tab's own with four valid corners.
	/// </summary>
	private void ValidateArea(ServiceValidation validation)
	{
		if (Areas.Count == 0 || !IsAreaInUse)
		{
			return;
		}

		if (Area == SubServiceArea.Artccs && !AreaArtccIds().Any())
		{
			validation.AddArea(ServiceAreas.Area, $"Tick at least one {AreaArtccNoun} on the Area card, or pick another area.");
		}
		else if (Area == SubServiceArea.Roi && !OverrideRoi && DefaultRoiStore.Load() is null)
		{
			validation.AddArea(
				ServiceAreas.Area,
				$"No default ROI is set. Set one in Settings or on the Map page, or pick \"An ROI specific to {Title}\" on the Area card.");
		}
		else if (Area == SubServiceArea.Roi && OverrideRoi)
		{
			ValidateOwnRoi(validation);
		}
	}

	private void ValidateOwnRoi(ServiceValidation validation)
	{
		// Each corner is reported against its own box so the empty one highlights. The two
		// library checks below look at the set as a whole, so they belong to the card - and they
		// only make sense once all four boxes actually have something in them.
		bool hasAllCorners = validation.RequireValue("SwLat", SwLat, $"Enter the southwest latitude of the ROI specific to {Title}.");
		hasAllCorners &= validation.RequireValue("SwLon", SwLon, $"Enter the southwest longitude of the ROI specific to {Title}.");
		hasAllCorners &= validation.RequireValue("NeLat", NeLat, $"Enter the northeast latitude of the ROI specific to {Title}.");
		hasAllCorners &= validation.RequireValue("NeLon", NeLon, $"Enter the northeast longitude of the ROI specific to {Title}.");

		if (!hasAllCorners)
		{
			return;
		}

		if (!RoiFilter.IsCoordinateValidFormat(SwLat, SwLon, NeLat, NeLon, out string? formatError))
		{
			validation.AddArea(ServiceAreas.Area, $"The ROI specific to {Title}: {formatError}");
			return;
		}

		if (TryReadOverrideCorners() is { } corners
			&& !RoiFilter.IsCoordinatesRelativePositionValid(corners.SwLat, corners.SwLon, corners.NeLat, corners.NeLon, out string? positionError))
		{
			validation.AddArea(ServiceAreas.Area, $"The ROI specific to {Title}: {positionError}");
		}
	}

	/// <summary>
	/// The saved area, or - when none is saved, or it isn't one the tab offers - the one the tab's
	/// other settings mean (<see cref="UserConfigAreas.DefaultFor"/>).
	/// </summary>
	private SubServiceArea SavedArea()
	{
		string tab = NodePath[(NodePath.LastIndexOf('.') + 1)..];

		foreach (string? saved in new[] { Get(UserConfigAreas.AreaKey), UserConfigAreas.DefaultFor(tab, UserConfigFile.GetValue) })
		{
			foreach (SubServiceArea area in Areas)
			{
				if (area.ToString().Equals(saved?.Trim(), StringComparison.OrdinalIgnoreCase))
				{
					return area;
				}
			}
		}

		return Areas[0];
	}

	/// <summary>An ROI's corners as the card and the Preview Settings tab show them.</summary>
	private static string Corners(RegionOfInterest roi) =>
		$"SW {roi.SwLat:0.####}, {roi.SwLon:0.####} / NE {roi.NeLat:0.####}, {roi.NeLon:0.####}";

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
			System.Windows.Application.Current?.MainWindow, TryReadOverrideCorners(), $"{Title}: Its Own ROI", DefaultRoiStore.Load());

		if (picked is { } roi)
		{
			SwLat = roi.SwLat.ToString("0.######", CultureInfo.InvariantCulture);
			SwLon = roi.SwLon.ToString("0.######", CultureInfo.InvariantCulture);
			NeLat = roi.NeLat.ToString("0.######", CultureInfo.InvariantCulture);
			NeLon = roi.NeLon.ToString("0.######", CultureInfo.InvariantCulture);
			OverrideRoi = true;
		}
	}

	private void RaiseRoiChanged()
	{
		OnPropertyChanged(nameof(DefaultRoiSummary));
		OnPropertyChanged(nameof(HasDefaultRoi));
		RaiseWhatYoullGetChanged();
	}

	private void RaiseAreaChoices()
	{
		foreach (string name in new[]
		{
			nameof(Area), nameof(AreaArtccs), nameof(AreaRoi), nameof(AreaEverything), nameof(AreaNone), nameof(HasRoi),
		})
		{
			OnPropertyChanged(name);
		}

		OnAreaChanged();
	}

	private void OnOutputsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => OnOutputsChanged();

	/// <summary>Every output on, never changing: the outputs of a tab not attached to a General tab.</summary>
	private sealed class EveryOutput : ObservableObject, ISubServiceOutputs
	{
		public bool IsIncluded => true;

		public bool Alias => true;

		public bool Geojson => true;

		public bool ProcedureChanges => true;

		public bool ProceduresJson => true;
	}
}
