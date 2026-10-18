using System.Collections.ObjectModel;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using DtppAirport = FeBuddy.Core.Infrastructure.Dtpp.Models.DtppMetafileXmlDataModel.Airport;

// The base class already has a "FebProperties" member (the FE-Buddy Helper Properties card's toggle
// list), which would otherwise hide Core's static FebProperties class in this file.
using CoreFebProperties = FeBuddy.Core.Application.Airac.FebProperties;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Procedure Charts</b> sub-service tab inside the AIRAC Service screen: which of the two documents
/// and the FAA Chart Recall alias file are on (set on the General tab), the area the documents cover
/// (the facilities ticked, an ROI, every airport, or none), the airports and procedures added to it,
/// the chart types a whole included airport is limited to, and the optional <c>Procedures.json</c>
/// fields.
/// </summary>
/// <remarks>
/// <para>
/// Like Telephony, Procedures writes no GeoJSON and has no FE-Buddy properties - it still derives
/// from <see cref="GeojsonSubServiceViewModel"/> for the alias file and the area plumbing, but never
/// shows the What Files Do You Want?, FE-Buddy Helper Properties or CRC ERAM Defaults cards:
/// <see cref="EmitKeys"/> is <c>(null, null, null)</c>, and <see cref="OutputFiles"/> offers only
/// the alias file.
/// </para>
/// <para>
/// The alias file covers every airport in the d-TPP Metafile; the area, airport, procedure and
/// chart type choices pick what the two documents cover, so they are only checked while a document
/// is on. The Airports, Additional Airport-Specific Procedures to Include and Additional Procedures to Include cards add to the area,
/// even outside it, except while it is everything (<see cref="AreListsUsed"/>).
/// </para>
/// <para>
/// Its data comes from two places besides the selected cycle's NASR data: the FAA d-TPP Metafile
/// (see <see cref="SetDtppData"/>), which may not be published yet, and the facility saved in
/// Settings ▸ Facility (<see cref="SettingsViewModel.ArtccKey"/>), read fresh whenever it
/// is needed rather than copied into this tab's own config.
/// </para>
/// <para>
/// Save, Undo and navigation come from the tab host's action bar; the run is launched by
/// <b>Run AIRAC Service</b> on the Preview Settings tab, and its results are shown on the Review
/// tab, described by this tab through <see cref="ISubServiceRunTarget"/>.
/// </para>
/// </remarks>
public sealed class ProceduresViewModel : GeojsonSubServiceViewModel, ISubServiceRunTarget
{
	/// <summary>
	/// The most airports the Airports card holds. Past this many, the facilities or an ROI on the
	/// Area card is the better tool, and the list stays readable.
	/// </summary>
	public const int MaxAirports = 100;

	private const string Node = "Services.AiracService.Procedures";

	private const string NotAnAirportHint = "Not an airport in this cycle.";
	private const string AlreadyInListHint = "Already in the list.";

	private static readonly (string Code, string Label)[] ChartTypeOptions =
	[
		(ProcedureChartTypes.Iap, "Instrument approaches"),
		(ProcedureChartTypes.Star, "STARs"),
		(ProcedureChartTypes.Dp, "Departures"),
		(ProcedureChartTypes.Odp, "Obstacle departures"),
		(ProcedureChartTypes.Dau, "RNAV departure AAUPs"),
		(ProcedureChartTypes.Apd, "Airport diagrams"),
		(ProcedureChartTypes.Min, "Takeoff, alternate and radar minimums"),
		(ProcedureChartTypes.Hot, "Hot spots"),
		(ProcedureChartTypes.Lah, "LAHSO"),
	];

	private bool _suppressListChanges;

	private HashSet<string> _savedFacilities = new(StringComparer.OrdinalIgnoreCase);

	private IReadOnlyDictionary<string, string> _nasrAirportsByIdent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private IReadOnlyDictionary<string, AptCsvDataModel.AptBase> _nasrAirportsByFaaId = new Dictionary<string, AptCsvDataModel.AptBase>(StringComparer.OrdinalIgnoreCase);

	/// <summary>The selected cycle's metafile, indexed in Core off the UI thread; empty while it isn't known or has none.</summary>
	private ProcedureNameIndex _dtppIndex = ProcedureNameIndex.Empty;

	// What Add would do with each entry in the Add box, worked out once per change to what it depends
	// on (RaiseNewAirportHint), not on every command re-query and hint read.
	private List<AirportEntry>? _airportEntries;

	// The root name the Add Procedure box resolves to, for the text and the metafile index it was resolved with.
	private (string Text, ProcedureNameIndex Index, string? Root)? _resolvedProcedure;

	private bool _dtppKnown;
	private bool _hasDtppData;
	private string? _dtppCycleId;
	private string _dtppStatus = string.Empty;
	private string _dtppPreviousStatus = string.Empty;
	private bool _previousDtppAvailable = true;

	private string _newAirportText = string.Empty;
	private string _newProcedureText = string.Empty;

	private bool _isEditingPair;
	private int _editingPairIndex = -1;
	private string _pairAirportText = string.Empty;
	private string? _selectedPairProcedure;
	private IReadOnlyList<string> _pairProcedureOptions = [];

	private bool _includeHelpCommand = true;
	private bool _renameHelpCommand;
	private string _helpCommandName = string.Empty;

	private bool _includeProceduresWithFixInFacility = true;

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public ProceduresViewModel()
	{
		Facilities.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFacilities));
		Airports.CollectionChanged += (_, _) =>
		{
			_airportEntries = null;
			OnPropertyChanged(nameof(HasAirports));
		};
		ProcedureNames.CollectionChanged += (_, _) =>
		{
			OnPropertyChanged(nameof(HasProcedureNames));
			RefreshProcedureNameRows();
		};
		AirportProcedures.CollectionChanged += (_, _) =>
		{
			OnPropertyChanged(nameof(HasAirportProcedures));
			RefreshPairDetails();
		};

		// The cards that add to the area name which ROI it is.
		PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(OverrideRoi))
			{
				RaiseCardIntros();
			}
		};

		ChartTypeToggles = [.. ChartTypeOptions
			.Select(o => new ProcedureOptionToggle(o.Code, $"{o.Label} ({ShownCode(o.Code)})", isSelected: false, OnListToggleChanged))];

		JsonFieldToggles = [.. ProcedureJsonFieldOptions.All
			.Select(o => new ProcedureOptionToggle(CoreFebProperties.Name(o.Field), o.Label, isSelected: false, OnListToggleChanged))];

		AddAirportCommand = new RelayCommand(AddAirport, CanAddAirport);
		ClearAirportTextCommand = new RelayCommand(() => NewAirportText = string.Empty, () => _newAirportText.Length > 0);
		DeleteAirportCommand = new RelayCommand<string>(DeleteAirport);

		AddProcedureCommand = new RelayCommand(AddProcedure, CanAddProcedure);
		CancelProcedureCommand = new RelayCommand(() => NewProcedureText = string.Empty, () => _newProcedureText.Length > 0);
		DeleteProcedureCommand = new RelayCommand<string>(DeleteProcedure);

		AddPairCommand = new RelayCommand(BeginAddPair, () => HasDtppData);
		ConfirmPairCommand = new RelayCommand(ConfirmPair, CanConfirmPair);
		CancelPairCommand = new RelayCommand(CancelPairEdit);
		EditPairCommand = new RelayCommand<ProcedureAirportPickItem>(BeginEditPair);
		DeletePairCommand = new RelayCommand<ProcedureAirportPickItem>(DeletePair);

		// The option for other airports' procedures names the facility saved in Settings.
		SettingsViewModel.FacilitySaved += (_, _) => OnFacilityChanged();

		LoadFromConfig();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "Procedure Charts";

	// ================= outputs =================

	/// <summary>Whether <c>Procedure_Changes.md</c> is written. Turned on and off on the General tab.</summary>
	public bool GenerateChangesDocument => Outputs.ProcedureChanges;

	/// <summary>Whether <c>Procedures.json</c> is written. Turned on and off on the General tab; its Fields card shows only while it is on.</summary>
	public bool GenerateProceduresJson => Outputs.ProceduresJson;

	/// <summary>Whether either document is on - the facility, airport, procedure and chart type choices only matter then.</summary>
	public bool GeneratesDocument => GenerateChangesDocument || GenerateProceduresJson;

	/// <inheritdoc />
	/// <remarks>The two documents, and what follows from them: the cards that pick what they cover.</remarks>
	protected override void OnOutputsChanged()
	{
		base.OnOutputsChanged();
		OnPropertyChanged(nameof(GenerateChangesDocument));
		OnPropertyChanged(nameof(GenerateProceduresJson));
		OnPropertyChanged(nameof(GeneratesDocument));
		OnPropertyChanged(nameof(DtppWarning));
		RaiseCardIntros();
	}

	/// <inheritdoc />
	protected override IReadOnlyList<SubServiceArea> Areas { get; } =
		[SubServiceArea.Artccs, SubServiceArea.Roi, SubServiceArea.Everything, SubServiceArea.None];

	/// <inheritdoc />
	protected override IEnumerable<string> AreaArtccIds() => SelectedFacilityIds();

	/// <inheritdoc />
	protected override string AreaArtccNoun => "facility";

	/// <inheritdoc />
	/// <remarks>The two documents: the alias file always has every chart.</remarks>
	public override SubServiceOutputKinds AreaOutputs => SubServiceOutputKinds.ProcedureChanges | SubServiceOutputKinds.ProceduresJson;

	/// <inheritdoc />
	/// <remarks>The cards that add to the area say what they add to, and are used only while it isn't everything.</remarks>
	protected override void OnAreaChanged()
	{
		OnPropertyChanged(nameof(AreListsUsed));
		RaiseNewAirportHint();
		RaiseCardIntros();
		RaiseFixInFacility();
	}

	// ================= External Procedures in Our Airspace: other airports' procedures with a fix in my facility =================

	/// <summary>The External Procedures in Our Airspace card's first footer note, under "The options in this card...".</summary>
	public const string ExternalProceduresScopeNote =
		"only apply to Departures (ODP/SID) and Arrivals (STAR). IAPs may be included in future updates.";

	/// <summary>The card's second footer note, shown while <c>Faa_Chart_Recall.txt</c> is on.</summary>
	public const string ExternalProceduresChartRecallNote = "don't apply to `Faa_Chart_Recall.txt`";

	/// <summary>The facility set in Settings ▸ Facility, e.g. <c>ZDC</c>, or <see langword="null"/> when none is.</summary>
	private static string? SettingsFacility => NormalizeFacility(UserConfigFile.GetValue(SettingsViewModel.ArtccKey));

	/// <summary>Whether a facility is set in Settings ▸ Facility: the option needs one.</summary>
	private static bool HasSettingsFacility => SettingsFacility is not null;

	/// <summary>
	/// Whether to also include, at any other airport, the departures and arrivals whose route has a fix
	/// or NAVAID in the facility set in Settings (Feature #367). On by default; used only while a
	/// facility is set and the area isn't everything.
	/// </summary>
	public bool IncludeProceduresWithFixInFacility
	{
		get => _includeProceduresWithFixInFacility;
		set
		{
			if (SetProperty(ref _includeProceduresWithFixInFacility, value))
			{
				MarkDirty();
			}
		}
	}

	/// <summary>
	/// The option's label, naming the facility as code: <c>Include procedures for non-`ZDC` airports that
	/// have a waypoint inside `ZDC`</c>.
	/// </summary>
	public string IncludeFixInFacilityLabel => SettingsFacility is { } facility
		? $"Include procedures for non-`{facility}` airports that have a waypoint inside `{facility}`"
		: "Include procedures for airports outside your facility that have a waypoint inside it";

	/// <summary>The option's tooltip: what counts as a waypoint inside the facility, and which charts come in.</summary>
	public string IncludeFixInFacilityToolTip =>
		$"A departure or arrival is included when any point on its route, transitions included, is a fix or NAVAID that NASR puts in {SettingsFacility ?? "your facility"}. "
		+ "Only the chart types ticked under Chart Types are included.";

	/// <summary>
	/// Why the option can't be used, shown at the top of the card - the area is everything, or no facility
	/// is set - or <see langword="null"/> when it can.
	/// </summary>
	public string? IncludeFixInFacilityNote =>
		!AreListsUsed ? "\"Everything\" already includes every airport, so this card isn't used."
		: !HasSettingsFacility ? "Set your facility in Settings ▸ Facility to use this card."
		: null;

	/// <summary>Whether the option can be changed: a facility is set in Settings.</summary>
	public bool CanIncludeProceduresWithFixInFacility => HasSettingsFacility;

	/// <summary>Whether the run uses the option: it's on, a facility is set, and the area isn't everything.</summary>
	private bool UsesFixInFacility => IncludeProceduresWithFixInFacility && HasSettingsFacility && AreListsUsed;

	/// <summary>Says the option's label and note again, and checks the page, after the area or the facility changes.</summary>
	private void RaiseFixInFacility()
	{
		foreach (string name in new[]
		{
			nameof(IncludeFixInFacilityLabel), nameof(IncludeFixInFacilityToolTip), nameof(IncludeFixInFacilityNote),
			nameof(CanIncludeProceduresWithFixInFacility),
		})
		{
			OnPropertyChanged(name);
		}
	}

	/// <summary>After Settings saves (the facility may have changed): the option says so, and the page is checked again.</summary>
	private void OnFacilityChanged()
	{
		RaiseFixInFacility();
		Revalidate();
	}

	// ================= what the cards that add to the area say =================

	/// <summary>
	/// Whether the Airports, Additional Airport-Specific Procedures to Include and Additional Procedures to Include cards are used: not
	/// while the area is everything, which already has every airport. Their lists are kept.
	/// </summary>
	public bool AreListsUsed => Area != SubServiceArea.Everything;

	/// <summary>What the cards that add to the area say while they aren't used.</summary>
	public const string ListsUnusedNote =
		"\"Everything\" already includes every airport, so this card isn't used. Its list is kept in case you pick another area.";

	/// <summary>
	/// The note at the foot of each card whose options don't reach the chart recall alias file, shown
	/// while that file is on.
	/// </summary>
	public const string ChartRecallNote = "The options in this card don't apply to `Faa_Chart_Recall.txt`.";

	/// <summary>
	/// The Area card's note while the cards below can add to it: <c>... outside the facilities you
	/// have chosen.</c>; <see langword="null"/> with Everything (they aren't used) or None (they are
	/// all there is).
	/// </summary>
	public string? AreaNote => Area switch
	{
		SubServiceArea.Artccs => "The options available to you after this card will allow you to get data for procedures outside the facilities you have chosen.",
		SubServiceArea.Roi => $"The options available to you after this card will allow you to get data for procedures outside {RoiShortName} you have set.",
		_ => null,
	};

	/// <summary>The Airports card's first line: that its airports are added, even outside the area, and to which documents.</summary>
	public string AirportsIntro => AddedEvenIfOutside("airports");

	/// <summary>The Additional Procedures to Include card's first line.</summary>
	public string ProcedureNamesIntro => AddedEvenIfOutside("departures and arrivals");

	/// <summary>The Additional Airport-Specific Procedures to Include card's first line.</summary>
	public string AirportProceduresIntro => AddedEvenIfOutside("airport-specific departures and arrivals");

	/// <summary>
	/// The Chart Types card's text: which documents the types narrow, then - while the cards that add
	/// to the area are used - that the procedures listed by name are always included.
	/// </summary>
	public string ChartTypesIntro =>
		$"Select the following chart types to be included in {DocumentsNamed("file", "files")}."
		+ (AreListsUsed
			? "\nNote: The departures and arrivals you list in Additional Airport-Specific Procedures to Include and Additional Procedures to Include are always included, even when their type isn't ticked here."
			: string.Empty);

	/// <summary>
	/// A card's line about what it adds: <c>Even if outside the facilities you selected, the airports
	/// you list here will be included in the `Procedures.json` output.</c>, or with no area to be
	/// outside of, just what it adds, capitalized.
	/// </summary>
	private string AddedEvenIfOutside(string what)
	{
		string adds = $"the {what} you list here will be included in {DocumentsNamed("output", "output")}.";

		return Area is SubServiceArea.Artccs or SubServiceArea.Roi
			? $"Even if outside {(Area == SubServiceArea.Roi ? RoiShortName : "the facilities")} you selected, {adds}"
			: char.ToUpperInvariant(adds[0]) + adds[1..];
	}

	/// <summary>The documents that are on, as the cards name them: <c>the `Procedures.json` file</c>.</summary>
	/// <param name="one">The word after one document, e.g. <c>file</c>.</param>
	/// <param name="both">The word after both, e.g. <c>files</c>.</param>
	private string DocumentsNamed(string one, string both) => (GenerateChangesDocument, GenerateProceduresJson) switch
	{
		(true, true) => $"the `Procedure_Changes.md` and `Procedures.json` {both}",
		(true, false) => $"the `Procedure_Changes.md` {one}",
		(false, true) => $"the `Procedures.json` {one}",
		_ => "the documents",
	};

	/// <summary>Which ROI the area is, as the cards name it: <c>the default ROI</c> or <c>the ROI specific to Procedures</c>.</summary>
	private string RoiShortName => OverrideRoi ? $"the ROI specific to {Title}" : "the default ROI";

	/// <summary>Announces what the cards that add to the area say, after the area, the ROI, or the documents that are on change.</summary>
	private void RaiseCardIntros()
	{
		foreach (string name in new[]
		{
			nameof(AreaNote), nameof(AirportsIntro), nameof(ProcedureNamesIntro), nameof(AirportProceduresIntro), nameof(ChartTypesIntro),
		})
		{
			OnPropertyChanged(name);
		}
	}

	/// <inheritdoc />
	/// <remarks>Procedures writes no GeoJSON at all.</remarks>
	protected override (string? Lines, string? Symbols, string? Text) EmitKeys => (null, null, null);

	/// <inheritdoc />
	/// <remarks>The FAA Chart Recall alias file, <c>Faa_Chart_Recall.txt</c>.</remarks>
	protected override bool HasAliasFile => true;

	/// <inheritdoc />
	/// <remarks>None: the Outputs card says where Procedures' outputs are turned on and off.</remarks>
	public override string? WhatYoullGetFooter => null;

	// ================= the chart recall help command =================

	/// <summary>The Chart Recall Help Command card's second question, naming the default command.</summary>
	public const string RenameHelpCommandQuestion =
		$"The command is `.{ChartRecallHelpCommand.DefaultName}` by default. Would you like to rename it?";

	/// <summary>The rename box's placeholder: an example name, without the dot the box shows in front.</summary>
	public const string HelpCommandPlaceholder = "e.g. helpCR";

	/// <summary>
	/// Whether <c>Faa_Chart_Recall.txt</c> starts with the help command: "Yes" to the Chart Recall
	/// Help Command card's question. On by default.
	/// </summary>
	public bool IncludeHelpCommand
	{
		get => _includeHelpCommand;
		set
		{
			if (SetProperty(ref _includeHelpCommand, value))
			{
				MarkDirty();
			}
		}
	}

	/// <summary>"Yes" to renaming the help command: the name typed is used. Off by default.</summary>
	public bool RenameHelpCommand
	{
		get => _renameHelpCommand;
		set
		{
			if (SetProperty(ref _renameHelpCommand, value))
			{
				OnPropertyChanged(nameof(HelpCommand));
				MarkDirty();
			}
		}
	}

	/// <summary>
	/// The help command's new name, as typed, without the dot: the box shows the dot in front, and a
	/// dot typed or pasted there is dropped. Kept when renaming is turned off.
	/// </summary>
	public string HelpCommandName
	{
		get => _helpCommandName;
		set
		{
			if (SetProperty(ref _helpCommandName, (value ?? string.Empty).TrimStart('.')))
			{
				OnPropertyChanged(nameof(HelpCommand));
				MarkDirty();
			}
		}
	}

	/// <summary>The help command's name, without its dot: the one typed while renaming, else the default.</summary>
	public string HelpCommand => ChartRecallHelpCommand.Name(RenameHelpCommand, HelpCommandName);

	/// <summary>
	/// The help command's name as last saved, for Info ▸ Alias Command Guide and its exports:
	/// <see langword="null"/> while the command is off, or the alias file is (General tab); the default
	/// for a new name that can't be used. Unsaved changes on this tab don't count.
	/// </summary>
	/// <returns>The name, without its dot, or <see langword="null"/>.</returns>
	public static string? SavedHelpCommand()
	{
		if (!AiracGeneralTabViewModel.IsSavedOutputOn(AiracSubServices.ProceduresKey, SubServiceOutputKinds.Alias)
			|| !ReadYesNo(UserConfigFile.GetValue($"{Node}.IncludeHelpCommand"), defaultValue: true))
		{
			return null;
		}

		string name = ChartRecallHelpCommand.Name(
			ReadYesNo(UserConfigFile.GetValue($"{Node}.RenameHelpCommand"), defaultValue: false),
			UserConfigFile.GetValue($"{Node}.HelpCommandName"));

		// The tab won't save a name that can't be used, but one edited by hand or saved while unchecked can still be there.
		return ChartRecallHelpCommand.IsValidName(name) ? name : ChartRecallHelpCommand.DefaultName;
	}

	// ================= d-TPP data =================

	/// <summary>Whether <see cref="SetDtppData"/> has been called yet for the currently selected cycle.</summary>
	public bool IsDtppKnown => _dtppKnown;

	/// <summary>The cycle whose metafile the tab shows, or <see langword="null"/> while none is known.</summary>
	public string? DtppCycleId => _dtppKnown ? _dtppCycleId : null;

	/// <summary>Whether the selected cycle has a downloaded, parsed d-TPP Metafile. Only meaningful once <see cref="IsDtppKnown"/> is true.</summary>
	public bool HasDtppData => _hasDtppData;

	/// <summary>What the Preview Settings tab says about the metafile's availability.</summary>
	public string DtppStatus => _dtppKnown ? _dtppStatus : "Chart data appears once the selected cycle's data is loaded.";

	/// <summary>Whether the previous cycle's metafile is available for deleted-procedure links.</summary>
	public string DtppPreviousStatus => _dtppKnown ? _dtppPreviousStatus : string.Empty;

	/// <summary>
	/// What the amber card at the top of the tab says while the metafiles fall short: the selected
	/// cycle's is missing (Procedures writes nothing), or the previous cycle's is (with
	/// <c>Procedure_Changes.md</c> on, deleted procedures go unlinked); <see langword="null"/> while
	/// all is well or before the metafile is known.
	/// </summary>
	public string? DtppWarning
	{
		get
		{
			if (!_dtppKnown)
			{
				return null;
			}

			List<string> lines = [];

			if (!_hasDtppData)
			{
				lines.Add(_dtppStatus);
			}
			else if (!_previousDtppAvailable && GenerateChangesDocument)
			{
				lines.Add(_dtppPreviousStatus);
			}

			return lines.Count > 0 ? string.Join("\n", lines) : null;
		}
	}

	/// <summary>
	/// Called by the AIRAC Service screen whenever the selected cycle's d-TPP Metafile has been
	/// (re)loaded, to report whether it is available and hand over the airport/procedure lookups the
	/// Airports, Additional Airport-Specific Procedures to Include and Additional Procedures to Include
	/// cards need - indexed in Core, off the UI thread (<see cref="ProcedureNameIndex"/>).
	/// </summary>
	/// <param name="cycleId">The selected cycle's ID.</param>
	/// <param name="dtpp">The selected cycle's indexed metafile, or <see langword="null"/> when it has not downloaded yet.</param>
	/// <param name="previousCycleId">The ID of the cycle immediately before <paramref name="cycleId"/>.</param>
	/// <param name="previousAvailable">Whether the previous cycle's metafile is available, for linking deleted procedures back to their chart.</param>
	/// <param name="downloadedLocal">When the selected cycle's metafile was downloaded, or <see langword="null"/> when it has none.</param>
	/// <param name="previousLoading">
	/// Whether the previous cycle is still on its way (FE-Buddy has just started), so its metafile
	/// isn't missing, just not here yet; a run waits for it.
	/// </param>
	/// <remarks>
	/// This card never blocks the run (a missing metafile is reported as an advisory by the Core
	/// run itself) and is not a saved setting, so this never marks the tab dirty - it only
	/// re-validates the airport/procedure pickers, whose Add buttons depend on this data.
	/// </remarks>
	public void SetDtppData(string cycleId, ProcedureNameIndex? dtpp, string previousCycleId, bool previousAvailable, DateTime? downloadedLocal, bool previousLoading = false)
	{
		ArgumentNullException.ThrowIfNull(cycleId);
		ArgumentNullException.ThrowIfNull(previousCycleId);

		_dtppKnown = true;
		_hasDtppData = dtpp is not null;
		_dtppCycleId = cycleId;
		_dtppIndex = dtpp ?? ProcedureNameIndex.Empty;

		ProcedureNameOptions = _dtppIndex.NameOptions;
		RefreshListedNames();

		_dtppStatus = dtpp is not null
			? $"Cycle {cycleId}: {dtpp.AirportCount:N0} airports, {dtpp.ProcedureCount:N0} procedures, downloaded {downloadedLocal:d MMM yyyy}."
			: $"Cycle {cycleId}'s metafile isn't out yet, or couldn't be downloaded. FE-Buddy checks again at each launch. Until then, Procedure Charts writes none of its files.";

		// One still loading isn't a problem to warn about: a run waits for it.
		_previousDtppAvailable = previousAvailable || previousLoading;
		_dtppPreviousStatus = previousAvailable
			? $"Deleted procedures link to cycle {previousCycleId}'s charts."
			: previousLoading
				? $"Cycle {previousCycleId} is still loading. Once it has, deleted procedures link to its charts."
				: $"Cycle {previousCycleId}'s metafile isn't available, so deleted procedures are listed without a link.";

		RefreshPairProcedureOptions();
		RaiseDtppDependentProperties();
		Revalidate();
	}

	// ================= facilities =================

	/// <summary>One toggle per ARTCC in the selected cycle's <c>APT_BASE.RESP_ARTCC_ID</c>. Empty until the cycle's data is loaded.</summary>
	public ObservableCollection<ArtccToggle> Facilities { get; } = [];

	/// <summary>Whether the facility list has been built from the selected cycle yet.</summary>
	public bool HasFacilities => Facilities.Count > 0;

	// ================= airports =================

	/// <summary>The airports (FAA or ICAO ID, as typed) added to the Airports card, stored by their FAA ID, in the order they were added.</summary>
	public ObservableCollection<string> Airports { get; } = [];

	/// <summary>Whether any airport has been added.</summary>
	public bool HasAirports => Airports.Count > 0;

	/// <summary>
	/// The text typed or pasted into the Airports card's Add box: one or more FAA or ICAO IDs,
	/// separated by anything but letters and digits (spaces, commas, new lines).
	/// </summary>
	public string NewAirportText
	{
		get => _newAirportText;
		set
		{
			if (SetProperty(ref _newAirportText, value))
			{
				RaiseNewAirportHint();
			}
		}
	}

	/// <summary>
	/// Why entries in the Add box can't be added, a line per reason with the IDs it applies to -
	/// e.g. <c>Not an airport in this cycle: KXYZ</c> - or empty when every entry can be.
	/// </summary>
	public string NewAirportHint
	{
		get
		{
			List<AirportEntry> entries = ReadAirportEntries();
			List<string> lines = [];

			AddHintLine(lines, "Not an airport in this cycle", entries.Where(e => e.Outcome == AirportEntryOutcome.NotFound));
			AddHintLine(lines, "Already in the list", entries.Where(e => e.Outcome == AirportEntryOutcome.AlreadyListed));

			foreach (IGrouping<string?, AirportEntry> group in entries
				.Where(e => e.Outcome == AirportEntryOutcome.AlreadyIncluded)
				.GroupBy(e => e.IncludedBy))
			{
				AddHintLine(lines, $"Already included {group.Key}", group);
			}

			AddHintLine(lines, $"Over the {MaxAirports}-airport limit", entries.Where(e => e.Outcome == AirportEntryOutcome.OverLimit));

			return string.Join('\n', lines);
		}
	}

	/// <summary>
	/// Adds every airport in <see cref="NewAirportText"/> that can be added, in order, up to
	/// <see cref="MaxAirports"/>. What can't be added stays in the box, with why under it.
	/// </summary>
	public ICommand AddAirportCommand { get; }

	/// <summary>Empties the Add box.</summary>
	public ICommand ClearAirportTextCommand { get; }

	/// <summary>Removes an airport from <see cref="Airports"/>.</summary>
	public ICommand DeleteAirportCommand { get; }

	// ================= procedures at any airport =================

	/// <summary>
	/// The departures and arrivals added to the Additional Procedures to Include card, by root name
	/// (<c>BRWNZ</c> for <c>BRWNZ FOUR (RNAV)</c>; see <see cref="ProcedureNaming.RootName"/>), in the
	/// order they were added. A run includes whatever the FAA has renumbered them to.
	/// </summary>
	public ObservableCollection<string> ProcedureNames { get; } = [];

	/// <summary>
	/// <see cref="ProcedureNames"/> as the card shows them: each with its current names in the selected
	/// cycle, or a note that the cycle has no departure or arrival by that name.
	/// </summary>
	public IReadOnlyList<ProcedureNameRow> ProcedureNameRows { get; private set; } = [];

	/// <summary>Whether any procedure name has been added.</summary>
	public bool HasProcedureNames => ProcedureNames.Count > 0;

	/// <summary>
	/// What the two cards that list procedures by name say about how they keep them: departures and
	/// arrivals only, each by its name without its number.
	/// </summary>
	public const string RenumberNote =
		"Only departures (SIDs and ODPs) and arrivals (STARs) can be added. Each is listed by its name without its number, so it stays included when the FAA renumbers it: pick BRWNZ FOUR (RNAV) and BRWNZ is listed, which brings in BRWNZ FIVE (RNAV) once that replaces it.";

	/// <summary>The text typed into (or picked in) the Additional Procedures to Include card's combo box.</summary>
	public string NewProcedureText
	{
		get => _newProcedureText;
		set
		{
			if (SetProperty(ref _newProcedureText, value))
			{
				OnPropertyChanged(nameof(NewProcedureHint));
			}
		}
	}

	/// <summary>Every distinct departure and arrival name in the metafile, sorted, for the combo box's items.</summary>
	public IReadOnlyList<string> ProcedureNameOptions { get; private set; } = [];

	/// <summary>"Not a departure or arrival in this cycle." or "Already in the list.", or empty when the typed text is fine to add.</summary>
	public string NewProcedureHint
	{
		get
		{
			string trimmed = _newProcedureText.Trim();

			if (trimmed.Length == 0)
			{
				return string.Empty;
			}

			if (NewProcedureRoot() is not { } root)
			{
				return "Not a departure or arrival in this cycle.";
			}

			return ProcedureNames.Any(p => p.Equals(root, StringComparison.OrdinalIgnoreCase)) ? AlreadyInListHint : string.Empty;
		}
	}

	/// <summary>Adds <see cref="NewProcedureText"/>, by its root name, to <see cref="ProcedureNames"/>.</summary>
	public ICommand AddProcedureCommand { get; }

	/// <summary>Clears <see cref="NewProcedureText"/> without adding it. The list is left as it is.</summary>
	public ICommand CancelProcedureCommand { get; }

	/// <summary>Removes a procedure name from <see cref="ProcedureNames"/>.</summary>
	public ICommand DeleteProcedureCommand { get; }

	// ================= airport + procedure =================

	/// <summary>The airport + procedure pairs added to the Additional Airport-Specific Procedures to Include card, in the order they were added.</summary>
	public ObservableCollection<ProcedureAirportPickItem> AirportProcedures { get; } = [];

	/// <summary>Whether any pair has been added.</summary>
	public bool HasAirportProcedures => AirportProcedures.Count > 0;

	/// <summary>Whether the Airport + Procedure editor row is open, either to add or to edit an entry.</summary>
	public bool IsEditingPair
	{
		get => _isEditingPair;
		private set
		{
			if (SetProperty(ref _isEditingPair, value))
			{
				OnPropertyChanged(nameof(ConfirmPairText));
			}
		}
	}

	/// <summary>The airport identifier (FAA or ICAO) typed in the open editor row.</summary>
	public string PairAirportText
	{
		get => _pairAirportText;
		set
		{
			if (SetProperty(ref _pairAirportText, value))
			{
				RefreshPairProcedureOptions();
				OnPropertyChanged(nameof(PairAirportName));
				OnPropertyChanged(nameof(PairEditorHint));
			}
		}
	}

	/// <summary>The resolved airport's name, shown next to <see cref="PairAirportText"/> once it names a known airport; empty otherwise.</summary>
	public string PairAirportName => ResolvePairAirport()?.AirportName ?? string.Empty;

	/// <summary>That airport's procedure base names, in chartseq order, for the editor's procedure combo box.</summary>
	public IReadOnlyList<string> PairProcedureOptions => _pairProcedureOptions;

	/// <summary>The procedure chosen in the open editor row, or <see langword="null"/> until one is picked.</summary>
	public string? SelectedPairProcedure
	{
		get => _selectedPairProcedure;
		set
		{
			if (SetProperty(ref _selectedPairProcedure, value))
			{
				OnPropertyChanged(nameof(PairEditorHint));
			}
		}
	}

	/// <summary>The confirm button's label: "Add" for a new pair, "Save" while editing an existing one.</summary>
	public string ConfirmPairText => _editingPairIndex >= 0 ? "Save" : "Add";

	/// <summary>"Not an airport in this cycle." or "Already in the list.", or empty when the editor's choices are fine to confirm.</summary>
	public string PairEditorHint
	{
		get
		{
			if (_pairAirportText.Trim().Length > 0 && ResolvePairAirport() is null)
			{
				return NotAnAirportHint;
			}

			if (ResolvePairAirport() is { } airport && _selectedPairProcedure is { } procedure
				&& IsDuplicatePair(airport.AptIdent, ProcedureNaming.RootName(procedure)))
			{
				return AlreadyInListHint;
			}

			return string.Empty;
		}
	}

	/// <summary>Opens the editor to add a new pair. Needs the d-TPP Metafile.</summary>
	public ICommand AddPairCommand { get; }

	/// <summary>Adds the new pair, or saves the one being edited.</summary>
	public ICommand ConfirmPairCommand { get; }

	/// <summary>Closes the editor without adding or saving anything.</summary>
	public ICommand CancelPairCommand { get; }

	/// <summary>Opens the editor with an existing pair's values, to change it.</summary>
	public ICommand EditPairCommand { get; }

	/// <summary>Removes a pair from the list.</summary>
	public ICommand DeletePairCommand { get; }

	// ================= chart types =================

	/// <summary>One toggle per known chart type, defaulting to <see cref="ProcedureChartTypes.Default"/> when the tab has never been saved.</summary>
	public ObservableCollection<ProcedureOptionToggle> ChartTypeToggles { get; }

	// ================= Procedures.json fields =================

	/// <summary>One toggle per <see cref="ProcedureJsonField"/>, defaulting to Core's default set when the tab has never been saved.</summary>
	public ObservableCollection<ProcedureOptionToggle> JsonFieldToggles { get; }

	// ================= parent hooks =================

	/// <inheritdoc />
	/// <remarks>
	/// Builds the Facilities toggle list from the cycle's <c>APT_BASE.RESP_ARTCC_ID</c> and the
	/// NASR airport lookup the Airports card falls back to before the d-TPP Metafile is loaded. A
	/// facility already listed keeps what is ticked on screen; a new one starts as saved.
	/// </remarks>
	public void LoadCycleDependentLists(AiracCycleOptionLists lists)
	{
		ArgumentNullException.ThrowIfNull(lists);

		bool hasSavedFacilities = Get("Facilities") is not null;
		_savedFacilities = ParseList(Get("Facilities"));
		Dictionary<string, bool> shown = Facilities.ToDictionary(t => t.Artcc, t => t.IsSelected, StringComparer.OrdinalIgnoreCase);

		string? settingsFacility = NormalizeFacility(UserConfigFile.GetValue(SettingsViewModel.ArtccKey));

		_suppressListChanges = true;
		try
		{
			Facilities.Clear();

			foreach (string id in lists.AirportFacilities)
			{
				// When the tab has never been saved, start with the Settings facility ticked
				// instead of nothing.
				bool isSelected = shown.TryGetValue(id, out bool onScreen)
					? onScreen
					: hasSavedFacilities
						? _savedFacilities.Contains(id)
						: id.Equals(settingsFacility, StringComparison.OrdinalIgnoreCase);

				Facilities.Add(new ArtccToggle(id, isSelected, OnListToggleChanged));
			}
		}
		finally
		{
			_suppressListChanges = false;
		}

		_nasrAirportsByIdent = lists.AirportFaaIdsByIdent;
		_nasrAirportsByFaaId = lists.AirportsByFaaId;

		RaiseNewAirportHint();

		// The list was empty when this tab snapshotted itself at construction; re-take the
		// snapshot now the toggles reflect what is actually saved (or the Settings default).
		ResyncSavedState();
	}

	/// <inheritdoc />
	public SubServiceRunResult? DescribeRunResult(AiracServiceResult result)
	{
		if (result.Procedures is not { } procedures)
		{
			return null;
		}

		List<string> parts = [];

		if (procedures.FilesWritten.Count > 0)
		{
			parts.Add($"{procedures.AirportCount:N0} airport(s), {procedures.NewCount} new, "
				+ $"{procedures.ChangedCount} changed, {procedures.DeletedCount} deleted, {procedures.FilesWritten.Count} document(s)");
		}

		if (procedures.AliasFilePath is not null)
		{
			parts.Add($"{ProcedureOutputFiles.Alias}: {procedures.AliasCommandCount:N0} command(s) for {procedures.AliasAirportCount:N0} airport(s)");
		}

		string summary = parts.Count > 0 ? string.Join("; ", parts) : "Nothing written";

		return new SubServiceRunResult(Title, summary, procedures.Messages);
	}

	/// <inheritdoc />
	/// <remarks>The block goes on <see cref="AiracServiceSettings.Procedures"/>.</remarks>
	public IReadOnlyDictionary<string, string> BuildSettingsBlock()
	{
		Dictionary<string, string> s = new(StringComparer.OrdinalIgnoreCase)
		{
			["GenerateChangesDocument"] = YesNo(GenerateChangesDocument),
			["GenerateProceduresJson"] = YesNo(GenerateProceduresJson),
			// Read fresh from Settings rather than this tab's own config - see the class remarks.
			["PrimaryFacility"] = UserConfigFile.GetValue(SettingsViewModel.ArtccKey) ?? string.Empty,
			["ChartTypes"] = string.Join(',', ChartTypeToggles.Where(t => t.IsSelected).Select(t => t.Token)),
			["JsonFields"] = string.Join(',', JsonFieldToggles.Where(t => t.IsSelected).Select(t => t.Token)),
			["IncludeHelpCommand"] = YesNo(IncludeHelpCommand),
		};

		// The name typed goes only while renaming; without it the run uses the default.
		if (IncludeHelpCommand && RenameHelpCommand)
		{
			s["HelpCommandName"] = HelpCommand;
		}

		// Only what the area uses, so the parser sees exactly what the run will do: the facilities
		// for that area, and the lists that add to it unless it's everything.
		if (Area == SubServiceArea.Artccs)
		{
			s["Facilities"] = string.Join(',', SelectedFacilityIds());
		}

		if (AreListsUsed)
		{
			s["Airports"] = string.Join(',', Airports);
			s["Procedures"] = string.Join(',', ProcedureNames);
			s["AirportProcedures"] = string.Join(',', AirportProcedures.Select(p => $"{p.Airport}|{p.ProcedureName}"));

			// Off with no facility set, as the greyed-out option shows, so the run doesn't warn about it.
			s["IncludeProceduresWithFixInFacility"] = YesNo(UsesFixInFacility);
		}

		AddSharedSettings(s);
		return s;
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		List<ServicePreviewRow> rows = PreviewRows();
		rows.Add(new ServicePreviewRow("Facilities listed first", DescribePrimaryFacility()));
		rows.Add(new ServicePreviewRow("d-TPP data", DtppStatus));

		return [new ServicePreviewSection(Title, rows) { WhatYoullGet = WhatYoullGet }];
	}

	/// <inheritdoc />
	/// <remarks>
	/// Unlike every other tab, the documents' airports come in two ways at once - the area, and the
	/// airports listed, which add to it - and procedures picked by name are added too. Under
	/// "Outputs include", <c>Procedure_Changes.md</c> keeps only what changed, and the chart types
	/// narrow what a whole airport gives, never a procedure picked by name. The alias file never
	/// follows any of it.
	/// </remarks>
	protected override IEnumerable<SummaryBlock> BuildWhatYoullGet()
	{
		string[] facilities = Area == SubServiceArea.Artccs ? [.. SelectedFacilityIds()] : [];
		string[] chartTypes = [.. ChartTypeToggles.Where(t => t.IsSelected).Select(t => ShownCode(t.Token))];

		// With everything, the cards that add to the area aren't used.
		string[] airports = AreListsUsed ? [.. Airports] : [];
		string[] names = AreListsUsed ? [.. ProcedureNames] : [];
		string[] pairs = AreListsUsed ? [.. AirportProcedures.Select(p => p.Label)] : [];

		string? area = Area switch
		{
			SubServiceArea.Artccs when facilities.Length > 0 => $"Charts for airports within {SummaryLines.Join(facilities, "and")}",
			SubServiceArea.Roi => RoiLine(roi => $"Charts for airports within {roi}"),
			SubServiceArea.Everything => "Charts for every airport in the d-TPP metafile",
			_ => null,
		};

		string? listed = airports.Length == 0 ? null
			: airports.Length <= 6 ? SummaryLines.Join(airports, "and")
			: $"the {airports.Length} airports you listed";

		// The area comes first; what the cards list is added to it.
		SummaryLines documents = new();
		documents.Add(SummaryJoin.First, area);
		documents.Add(SummaryJoin.AlongWith, listed is not null && documents.IsEmpty ? $"Charts for {listed}" : listed);

		// Other airports' departures and arrivals that cross the facility: limited to the chart types too.
		string? crossing = UsesFixInFacility ? $"departures and arrivals at other airports whose route has a fix in {SettingsFacility}" : null;
		documents.Add(SummaryJoin.AlongWith, crossing is not null && documents.IsEmpty ? char.ToUpperInvariant(crossing[0]) + crossing[1..] : crossing);

		bool wholeAirports = !documents.IsEmpty;
		bool picked = names.Length > 0 || pairs.Length > 0;

		documents.Add(SummaryJoin.AlongWith, names.Length > 0
			? $"{SummaryLines.Join(names, "and")}, wherever {(names.Length == 1 ? "it's" : "they're")} published"
			: null);
		documents.Add(SummaryJoin.AlongWith, pairs.Length > 0 ? SummaryLines.Join(pairs, "and") : null);
		documents.Add(SummaryJoin.First, wholeAirports || picked ? null : "Nothing yet: pick an area, or list an airport or a procedure");

		string? types = !wholeAirports ? null
			: chartTypes.Length == 0 ? "these types: none yet, so tick one under Chart Types"
			: picked ? $"these types: {SummaryLines.Join(chartTypes, "and")} (the procedures you named are always included)"
			: $"these types: {SummaryLines.Join(chartTypes, "and")}";

		// With nothing picked, there is nothing for a document to keep.
		IReadOnlyList<SummaryLine> includes = new SummaryLines(joinFirstLine: true)
			.Add(SummaryJoin.First, GenerateChangesDocument && (wholeAirports || picked)
				? "`Procedure_Changes.md`: only those that were added, changed, or deleted this cycle"
				: null)
			.Add(SummaryJoin.WithOnly, types)
			.ToList();

		yield return new SummaryBlock(SubServiceOutputKinds.ProcedureChanges | SubServiceOutputKinds.ProceduresJson, documents.ToList()) { Includes = includes };

		string[] documentsOn = [.. new[]
		{
			GenerateChangesDocument ? "`Procedure_Changes.md`" : null,
			GenerateProceduresJson ? "`Procedures.json`" : null,
		}.OfType<string>()];

		yield return new SummaryBlock(SubServiceOutputKinds.Alias, new SummaryLines()
			.Add(SummaryJoin.First, documentsOn.Length == 0
				? "Every chart at every airport in the d-TPP metafile"
				: $"Every chart at every airport in the d-TPP metafile, not just the ones you chose for {SummaryLines.Join(documentsOn, "and")}")
			.Add(SummaryJoin.AlongWith, IncludeHelpCommand ? $"`.{HelpCommand}` at the top: a quick reference for writing a chart recall command" : null)
			.ToList());
	}

	/// <summary>A chart type's code as people know it: the d-TPP's <c>STR</c> is a STAR.</summary>
	/// <param name="code">The d-TPP's code, as saved and sent.</param>
	/// <returns>The code to show.</returns>
	private static string ShownCode(string code) => code == ProcedureChartTypes.Star ? "STAR" : code;

	// ================= save contract =================

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		_suppressListChanges = true;
		try
		{
			bool hasSavedFacilities = Get("Facilities") is not null;
			_savedFacilities = ParseList(Get("Facilities"));

			if (Facilities.Count > 0)
			{
				string? settingsFacility = NormalizeFacility(UserConfigFile.GetValue(SettingsViewModel.ArtccKey));

				foreach (ArtccToggle toggle in Facilities)
				{
					toggle.IsSelected = hasSavedFacilities
						? _savedFacilities.Contains(toggle.Artcc)
						: toggle.Artcc.Equals(settingsFacility, StringComparison.OrdinalIgnoreCase);
				}
			}

			Airports.Clear();
			foreach (string id in SplitOrdered(Get("Airports")))
			{
				Airports.Add(id);
			}

			// By root name: an earlier FE-Buddy saved the full name (BRWNZ FOUR (RNAV)), which reads
			// as BRWNZ. The page saves the root from then on, and isn't marked changed for it.
			ProcedureNames.Clear();
			foreach (string root in SplitOrdered(Get("Procedures")).Select(ProcedureNaming.RootName).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				ProcedureNames.Add(root);
			}

			AirportProcedures.Clear();
			foreach (ProcedureAirportPickItem item in ParseAirportProcedurePicks(Get("AirportProcedures")))
			{
				AirportProcedures.Add(item);
			}

			// The defaults only when nothing is saved: a saved blank list is the user's choice.
			string? savedChartTypesRaw = Get("ChartTypes");
			HashSet<string> savedChartTypes = savedChartTypesRaw is null
				? new HashSet<string>(ProcedureChartTypes.Default, StringComparer.OrdinalIgnoreCase)
				: ParseList(savedChartTypesRaw);

			foreach (ProcedureOptionToggle toggle in ChartTypeToggles)
			{
				toggle.IsSelected = savedChartTypes.Contains(toggle.Token);
			}

			string? savedJsonFieldsRaw = Get("JsonFields");
			HashSet<string> savedJsonFields = savedJsonFieldsRaw is null
				? new HashSet<string>(ProcedureSettingsParser.DefaultJsonFields.Select(CoreFebProperties.Name), StringComparer.OrdinalIgnoreCase)
				: ParseList(savedJsonFieldsRaw);

			foreach (ProcedureOptionToggle toggle in JsonFieldToggles)
			{
				toggle.IsSelected = savedJsonFields.Contains(toggle.Token);
			}

			_includeHelpCommand = GetBool("IncludeHelpCommand", true);
			_renameHelpCommand = GetBool("RenameHelpCommand", false);
			_helpCommandName = (Get("HelpCommandName") ?? string.Empty).TrimStart('.');
			_includeProceduresWithFixInFacility = GetBool("IncludeProceduresWithFixInFacility", true);

			LoadSharedSettings();
		}
		finally
		{
			_suppressListChanges = false;
		}

		foreach (string name in new[]
		{
			nameof(IncludeHelpCommand), nameof(RenameHelpCommand), nameof(HelpCommandName), nameof(HelpCommand),
			nameof(IncludeProceduresWithFixInFacility),
		})
		{
			OnPropertyChanged(name);
		}

		RaiseCardIntros();
		RaiseFixInFacility();
		ClearDirty();
	}

	/// <inheritdoc />
	protected override void WriteToConfig()
	{
		Set("Facilities", string.Join(',', SelectedFacilityIds()));
		Set("Airports", string.Join(',', Airports));
		Set("Procedures", string.Join(',', ProcedureNames));
		Set("AirportProcedures", string.Join(',', AirportProcedures.Select(p => $"{p.Airport}|{p.ProcedureName}")));
		Set("ChartTypes", string.Join(',', ChartTypeToggles.Where(t => t.IsSelected).Select(t => t.Token)));
		Set("JsonFields", string.Join(',', JsonFieldToggles.Where(t => t.IsSelected).Select(t => t.Token)));
		Set("IncludeHelpCommand", YesNo(IncludeHelpCommand));
		Set("RenameHelpCommand", YesNo(RenameHelpCommand));
		Set("HelpCommandName", HelpCommandName.Trim());
		Set("IncludeProceduresWithFixInFacility", YesNo(IncludeProceduresWithFixInFacility));
		SaveSharedSettings();
	}

	/// <inheritdoc />
	protected override void Validate(ServiceValidation validation)
	{
		// The Add box's hint depends on the area and the list, and any change to them re-validates.
		RaiseNewAirportHint();

		// The choices below only pick what the documents cover; the alias file covers every airport.
		// The area itself (a facility ticked, an ROI set) is checked with the shared settings.
		if (GeneratesDocument)
		{
			ValidateDocumentSelection(validation);
		}

		// The new name is used only while the alias file, the help command and renaming are all on.
		if (GenerateAliasFile && IncludeHelpCommand && RenameHelpCommand)
		{
			ValidateHelpCommandName(validation);
		}

		ValidateSharedSettings(validation);
	}

	/// <summary>Checks the help command's new name: typed, and letters and numbers only.</summary>
	private void ValidateHelpCommandName(ServiceValidation validation)
	{
		string name = ChartRecallHelpCommand.CleanName(HelpCommandName);

		if (name.Length == 0)
		{
			validation.AddField(nameof(HelpCommandName),
				$"Type the command's new name, or pick No to keep .{ChartRecallHelpCommand.DefaultName}.");
		}
		else if (!ChartRecallHelpCommand.IsValidName(name))
		{
			validation.AddField(nameof(HelpCommandName), "Use letters and numbers only: no spaces or symbols.");
		}
	}

	/// <summary>
	/// Checks that the documents include something: with no area, at least one airport or procedure
	/// listed, or other airports' procedures with a fix in the facility; and with whole airports or those
	/// procedures coming in, a chart type ticked.
	/// </summary>
	private void ValidateDocumentSelection(ServiceValidation validation)
	{
		bool airportsListed = AreListsUsed && Airports.Count > 0;
		bool proceduresListed = AreListsUsed && (ProcedureNames.Count > 0 || AirportProcedures.Count > 0);
		bool hasWholeAirportSource = Area != SubServiceArea.None || airportsListed;

		if (Area == SubServiceArea.None && !airportsListed && !proceduresListed && !UsesFixInFacility)
		{
			validation.AddArea(
				ServiceAreas.DocumentSelection,
				"With \"None\" picked on the Area card, list at least one airport or procedure for the documents to include, or pick another area.");
		}

		if ((hasWholeAirportSource || UsesFixInFacility) && ChartTypeToggles.All(t => !t.IsSelected))
		{
			validation.AddArea(
				ServiceAreas.ChartTypes,
				"No chart types are ticked, so the area and the airports you list bring in nothing. Tick at least one chart type.");
		}
	}

	/// <inheritdoc />
	/// <remarks>
	/// Only the alias file: with no GeoJSON there is nothing to carry CRC-ERAM defaults.
	/// </remarks>
	protected override IEnumerable<OutputFileOption> OutputFiles()
	{
		if (GenerateAliasFile)
		{
			yield return OutputFileOption.AliasFile(ProcedureOutputFiles.Alias);
		}
	}

	/// <inheritdoc />
	/// <remarks>The two documents as well as the alias file: they can be renamed too.</remarks>
	public override IEnumerable<OutputFileEntry> OutputFileEntries()
	{
		if (GenerateChangesDocument)
		{
			yield return OutputFileEntry.Renamable(ProcedureOutputFiles.Changes, AiracOutputPaths.PublicationDocsFolder, Title);
		}

		if (GenerateProceduresJson)
		{
			yield return OutputFileEntry.Renamable(ProcedureOutputFiles.Json, AiracOutputPaths.PublicationDocsFolder, Title);
		}

		foreach (OutputFileEntry file in base.OutputFileEntries())
		{
			yield return file;
		}
	}

	// ================= helpers =================

	private void OnListToggleChanged()
	{
		if (_suppressListChanges)
		{
			return;
		}

		MarkDirty();
	}

	private bool TryResolveAirportId(string text, out string faaId)
	{
		if (text.Length == 0)
		{
			faaId = string.Empty;
			return false;
		}

		if (_hasDtppData)
		{
			if (_dtppIndex.AirportsByIdent.TryGetValue(text, out DtppAirport? airport))
			{
				faaId = airport.AptIdent;
				return true;
			}

			faaId = string.Empty;
			return false;
		}

		if (_nasrAirportsByIdent.TryGetValue(text, out string? nasrId))
		{
			faaId = nasrId;
			return true;
		}

		faaId = string.Empty;
		return false;
	}

	private bool CanAddAirport() => ReadAirportEntries().Any(e => e.Outcome == AirportEntryOutcome.Add);

	/// <summary>
	/// After something the Add box's entries depend on may have changed - the text, the list, the area,
	/// the facilities ticked, the ROI or the cycle's airports: they're worked out again when next asked
	/// for, and the hint says so.
	/// </summary>
	private void RaiseNewAirportHint()
	{
		_airportEntries = null;
		OnPropertyChanged(nameof(NewAirportHint));
	}

	/// <inheritdoc />
	/// <remarks>The default ROI can change without this tab being edited, and the Add box's entries depend on it.</remarks>
	protected override void OnRoiChanged() => RaiseNewAirportHint();

	private void AddAirport()
	{
		List<AirportEntry> entries = ReadAirportEntries();

		if (!entries.Any(e => e.Outcome == AirportEntryOutcome.Add))
		{
			return;
		}

		foreach (AirportEntry entry in entries.Where(e => e.Outcome == AirportEntryOutcome.Add))
		{
			Airports.Add(entry.FaaId);
		}

		// What wasn't added stays in the box, so the hint says why and a typo can be fixed in place.
		NewAirportText = string.Join(", ", entries
			.Where(e => e.Outcome is not (AirportEntryOutcome.Add or AirportEntryOutcome.Repeat))
			.Select(e => e.Text));

		MarkDirty();
	}

	/// <summary>
	/// What Add would do with each entry in the Add box, in order: add it, skip a repeat of one it adds,
	/// or leave it with a reason. Worked out once per change (<see cref="RaiseNewAirportHint"/>).
	/// </summary>
	private List<AirportEntry> ReadAirportEntries() => _airportEntries ??= WorkOutAirportEntries();

	private List<AirportEntry> WorkOutAirportEntries()
	{
		List<AirportEntry> entries = [];

		// Anything but a letter or digit separates the IDs.
		string[] texts = new string([.. _newAirportText.Select(c => char.IsAsciiLetterOrDigit(c) ? char.ToUpperInvariant(c) : ' ')])
			.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		if (texts.Length == 0)
		{
			return entries;
		}

		HashSet<string> listed = new(Airports, StringComparer.OrdinalIgnoreCase);
		HashSet<string> adding = new(StringComparer.OrdinalIgnoreCase);
		HashSet<string> facilities = Area == SubServiceArea.Artccs
			? new(SelectedFacilityIds(), StringComparer.OrdinalIgnoreCase)
			: new(StringComparer.OrdinalIgnoreCase);
		RegionOfInterest? region = Area == SubServiceArea.Roi ? RoiInUse() : null;

		foreach (string text in texts)
		{
			if (!TryResolveAirportId(text, out string faaId))
			{
				entries.Add(new AirportEntry(text, faaId, AirportEntryOutcome.NotFound));
			}
			else if (listed.Contains(faaId))
			{
				entries.Add(new AirportEntry(text, faaId, AirportEntryOutcome.AlreadyListed));
			}
			else if (adding.Contains(faaId))
			{
				entries.Add(new AirportEntry(text, faaId, AirportEntryOutcome.Repeat));
			}
			else if (IncludedBy(faaId, facilities, region) is { } includedBy)
			{
				entries.Add(new AirportEntry(text, faaId, AirportEntryOutcome.AlreadyIncluded, includedBy));
			}
			else if (listed.Count + adding.Count >= MaxAirports)
			{
				entries.Add(new AirportEntry(text, faaId, AirportEntryOutcome.OverLimit));
			}
			else
			{
				adding.Add(faaId);
				entries.Add(new AirportEntry(text, faaId, AirportEntryOutcome.Add));
			}
		}

		return entries;
	}

	/// <summary>
	/// How the area already includes the whole airport, the way the library decides it: its
	/// responsible ARTCC is a facility ticked, or it is inside the ROI. <see langword="null"/> when
	/// the area doesn't.
	/// </summary>
	/// <param name="faaId">The airport's FAA ID.</param>
	/// <param name="facilities">The facilities ticked, while the area is the facilities; otherwise none.</param>
	/// <param name="region">The ROI, while the area is the ROI; otherwise <see langword="null"/>.</param>
	private string? IncludedBy(string faaId, HashSet<string> facilities, RegionOfInterest? region)
	{
		if (!_nasrAirportsByFaaId.TryGetValue(faaId, out AptCsvDataModel.AptBase? row))
		{
			return null;
		}

		string artcc = (row.RespArtccId ?? string.Empty).Trim();

		if (artcc.Length > 0 && facilities.Contains(artcc))
		{
			return $"by {artcc.ToUpperInvariant()}, a facility ticked on the Area card";
		}

		return region is not null && RoiFilter.Contains(region, row.BaseLatDecimal, row.BaseLongDecimal)
			? $"by {RoiName}"
			: null;
	}

	private static void AddHintLine(List<string> lines, string reason, IEnumerable<AirportEntry> entries)
	{
		string[] texts = [.. entries.Select(e => e.Text)];

		if (texts.Length > 0)
		{
			lines.Add($"{reason}: {string.Join(", ", texts)}");
		}
	}

	private void DeleteAirport(string? airport)
	{
		if (airport is not null && Airports.Remove(airport))
		{
			MarkDirty();
		}
	}

	private bool CanAddProcedure() =>
		NewProcedureRoot() is { } root
		&& !ProcedureNames.Any(p => p.Equals(root, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// <see cref="ResolveRoot"/> of the Add Procedure box, worked out again only when its text or the
	/// metafile changes: the hint and Add's re-query both ask, on every keystroke.
	/// </summary>
	private string? NewProcedureRoot()
	{
		if (_resolvedProcedure is not { } resolved
			|| !string.Equals(resolved.Text, _newProcedureText, StringComparison.Ordinal)
			|| !ReferenceEquals(resolved.Index, _dtppIndex))
		{
			resolved = (_newProcedureText, _dtppIndex, ResolveRoot(_newProcedureText));
			_resolvedProcedure = resolved;
		}

		return resolved.Root;
	}

	private void AddProcedure()
	{
		if (NewProcedureRoot() is not { } root
			|| ProcedureNames.Any(p => p.Equals(root, StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}

		ProcedureNames.Add(root);
		NewProcedureText = string.Empty;
		MarkDirty();
	}

	/// <summary>
	/// The root name a typed or picked departure or arrival is listed by: from its full name
	/// (<c>BRWNZ FOUR (RNAV)</c>) or the root itself (<c>BRWNZ</c>), spelled as the FAA does.
	/// <see langword="null"/> when the selected cycle has no departure or arrival by it.
	/// </summary>
	/// <param name="typed">The text typed or picked.</param>
	/// <returns>The root name, or <see langword="null"/>.</returns>
	private string? ResolveRoot(string typed)
	{
		string trimmed = typed.Trim();

		if (trimmed.Length == 0)
		{
			return null;
		}

		string root = ProcedureNaming.RootName(_dtppIndex.NamesByCanonical.TryGetValue(trimmed, out string? canonical) ? canonical : trimmed);

		return _dtppIndex.NamesByRoot.TryGetValue(root, out IReadOnlyList<string>? names) ? ProcedureNaming.RootName(names[0]) : null;
	}

	/// <summary>The selected cycle's departures and arrivals with this root name, at one airport or at any.</summary>
	/// <param name="root">The root name.</param>
	/// <param name="airport">The airport (FAA or ICAO identifier), or <see langword="null"/> for any.</param>
	/// <returns>Their current names, in chartseq order at one airport or alphabetical at any.</returns>
	private IReadOnlyList<string> CurrentNames(string root, string? airport)
	{
		if (airport is null)
		{
			return _dtppIndex.NamesByRoot.TryGetValue(root, out IReadOnlyList<string>? names) ? [.. names.Order(StringComparer.OrdinalIgnoreCase)] : [];
		}

		return _dtppIndex.AirportsByIdent.TryGetValue(airport, out DtppAirport? metafileAirport)
			&& _dtppIndex.NamesByAirport.TryGetValue(metafileAirport.AptIdent, out IReadOnlyList<string>? atAirport)
				? [.. atAirport.Where(name => ProcedureNaming.RootName(name).Equals(root, StringComparison.OrdinalIgnoreCase))]
				: [];
	}

	/// <summary>After the selected cycle's metafile changes: each listed name's current names, on both cards.</summary>
	private void RefreshListedNames()
	{
		RefreshProcedureNameRows();
		RefreshPairDetails();
	}

	/// <summary>Rebuilds <see cref="ProcedureNameRows"/> from <see cref="ProcedureNames"/> and the selected cycle.</summary>
	private void RefreshProcedureNameRows()
	{
		ProcedureNameRows = [.. ProcedureNames.Select(name =>
		{
			if (!_hasDtppData)
			{
				return new ProcedureNameRow(name, null, IsMissing: false);
			}

			IReadOnlyList<string> current = CurrentNames(name, airport: null);

			return current.Count > 0
				? new ProcedureNameRow(name, $"Cycle {_dtppCycleId}: {string.Join(", ", current)}", IsMissing: false)
				: new ProcedureNameRow(name, $"Cycle {_dtppCycleId} has no departure or arrival named {name}, so it's left out.", IsMissing: true);
		})];

		OnPropertyChanged(nameof(ProcedureNameRows));
	}

	/// <summary>Says, under each airport-specific pick, what it brings in from the selected cycle.</summary>
	private void RefreshPairDetails()
	{
		foreach (ProcedureAirportPickItem item in AirportProcedures)
		{
			if (!_hasDtppData)
			{
				item.Detail = null;
				item.IsMissing = false;
				continue;
			}

			IReadOnlyList<string> current = CurrentNames(item.ProcedureName, item.Airport);

			item.IsMissing = current.Count == 0;
			item.Detail = current.Count > 0
				? $"Cycle {_dtppCycleId}: {string.Join(", ", current)}"
				: $"Cycle {_dtppCycleId} has no departure or arrival named {item.ProcedureName} at {item.Airport}, so it's left out.";
		}
	}

	private void DeleteProcedure(string? name)
	{
		if (name is not null && ProcedureNames.Remove(name))
		{
			MarkDirty();
		}
	}

	private DtppAirport? ResolvePairAirport()
	{
		string trimmed = _pairAirportText.Trim();

		if (trimmed.Length == 0 || !_hasDtppData)
		{
			return null;
		}

		return _dtppIndex.AirportsByIdent.TryGetValue(trimmed, out DtppAirport? airport) ? airport : null;
	}

	private void RefreshPairProcedureOptions()
	{
		_pairProcedureOptions = ResolvePairAirport() is { } airport
			&& _dtppIndex.NamesByAirport.TryGetValue(airport.AptIdent, out IReadOnlyList<string>? names)
				? names
				: [];

		OnPropertyChanged(nameof(PairProcedureOptions));

		if (_selectedPairProcedure is not null && !_pairProcedureOptions.Any(p => p.Equals(_selectedPairProcedure, StringComparison.OrdinalIgnoreCase)))
		{
			SelectedPairProcedure = null;
		}
	}

	private void BeginAddPair()
	{
		_editingPairIndex = -1;
		PairAirportText = string.Empty;
		SelectedPairProcedure = null;
		IsEditingPair = true;
	}

	private void BeginEditPair(ProcedureAirportPickItem? item)
	{
		if (item is null)
		{
			return;
		}

		int index = AirportProcedures.IndexOf(item);

		if (index < 0)
		{
			return;
		}

		_editingPairIndex = index;
		PairAirportText = item.Airport;
		SelectedPairProcedure = _pairProcedureOptions.FirstOrDefault(
			p => ProcedureNaming.RootName(p).Equals(item.ProcedureName, StringComparison.OrdinalIgnoreCase)) ?? item.ProcedureName;
		IsEditingPair = true;
	}

	private void CancelPairEdit() => EndPairEdit();

	private void EndPairEdit()
	{
		_editingPairIndex = -1;
		PairAirportText = string.Empty;
		SelectedPairProcedure = null;
		IsEditingPair = false;
	}

	private bool CanConfirmPair() =>
		ResolvePairAirport() is { } airport && _selectedPairProcedure is { } procedure
		&& !IsDuplicatePair(airport.AptIdent, ProcedureNaming.RootName(procedure));

	private void ConfirmPair()
	{
		if (ResolvePairAirport() is not { } airport || _selectedPairProcedure is not { } procedure)
		{
			return;
		}

		// Listed by its root name, so the pair still finds the procedure once the FAA renumbers it.
		string root = ProcedureNaming.RootName(procedure);
		string label = PairLabel(airport.AptIdent, root);

		if (_editingPairIndex >= 0 && _editingPairIndex < AirportProcedures.Count)
		{
			ProcedureAirportPickItem item = AirportProcedures[_editingPairIndex];
			item.Airport = airport.AptIdent;
			item.ProcedureName = root;
			item.Label = label;
			RefreshPairDetails();
		}
		else
		{
			AirportProcedures.Add(new ProcedureAirportPickItem(airport.AptIdent, root, label));
		}

		EndPairEdit();
		MarkDirty();
	}

	/// <summary>An airport-specific pick as the card and What You'll Get show it, e.g. <c>LAS — BRWNZ</c>.</summary>
	private static string PairLabel(string airport, string procedure) => $"{airport} — {procedure}";

	private void DeletePair(ProcedureAirportPickItem? item)
	{
		if (item is null)
		{
			return;
		}

		int index = AirportProcedures.IndexOf(item);

		if (index < 0)
		{
			return;
		}

		AirportProcedures.RemoveAt(index);

		if (IsEditingPair)
		{
			if (_editingPairIndex == index)
			{
				EndPairEdit();
			}
			else if (_editingPairIndex > index)
			{
				_editingPairIndex--;
			}
		}

		MarkDirty();
	}

	private bool IsDuplicatePair(string airport, string procedure)
	{
		for (int i = 0; i < AirportProcedures.Count; i++)
		{
			if (i == _editingPairIndex)
			{
				continue;
			}

			ProcedureAirportPickItem existing = AirportProcedures[i];

			if (existing.Airport.Equals(airport, StringComparison.OrdinalIgnoreCase)
				&& existing.ProcedureName.Equals(procedure, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	/// <inheritdoc />
	/// <remarks>
	/// Another cycle picked: the old cycle's chart data and NASR airport lookups no longer describe it,
	/// so they go until the new cycle's arrive. Its facility toggles stay, keeping what is ticked.
	/// </remarks>
	protected override void OnSelectedCycleChanged()
	{
		if (_dtppCycleId is null || _dtppCycleId == SelectedCycle?.AiracCycleId)
		{
			return;
		}

		_dtppKnown = false;
		_hasDtppData = false;
		_dtppCycleId = null;
		_dtppIndex = ProcedureNameIndex.Empty;
		_nasrAirportsByIdent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		_nasrAirportsByFaaId = new Dictionary<string, AptCsvDataModel.AptBase>(StringComparer.OrdinalIgnoreCase);
		ProcedureNameOptions = [];

		RefreshListedNames();
		RefreshPairProcedureOptions();
		RaiseDtppDependentProperties();
		Revalidate();
	}

	private void RaiseDtppDependentProperties()
	{
		OnPropertyChanged(nameof(IsDtppKnown));
		OnPropertyChanged(nameof(HasDtppData));
		OnPropertyChanged(nameof(DtppStatus));
		OnPropertyChanged(nameof(DtppPreviousStatus));
		OnPropertyChanged(nameof(DtppWarning));
		OnPropertyChanged(nameof(ProcedureNameOptions));
		RaiseNewAirportHint();
		OnPropertyChanged(nameof(NewProcedureHint));
		OnPropertyChanged(nameof(PairAirportName));
		OnPropertyChanged(nameof(PairEditorHint));
	}

	private IEnumerable<string> SelectedFacilityIds() =>
		Facilities.Count > 0
			? Facilities.Where(f => f.IsSelected).Select(f => f.Artcc)
			: FacilitiesTheListWillTick();

	/// <summary>
	/// Before the cycle's facility list is built - the cycle picked can still be loading when the screen
	/// opens - the ones it will tick: the saved ones, or, when the tab has never saved any, the Settings
	/// facility (as <see cref="LoadCycleDependentLists"/> does). So the Area card isn't flagged for a
	/// moment over a list that hasn't arrived.
	/// </summary>
	private IEnumerable<string> FacilitiesTheListWillTick() =>
		Get("Facilities") is not null
			? _savedFacilities.OrderBy(a => a, StringComparer.OrdinalIgnoreCase)
			: NormalizeFacility(UserConfigFile.GetValue(SettingsViewModel.ArtccKey)) is { } facility ? [facility] : [];

	private static string? NormalizeFacility(string? value) =>
		string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

	private static IEnumerable<string> SplitOrdered(string? saved) =>
		(saved ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	/// <summary>
	/// The saved airport-specific picks, each by its root name - an earlier FE-Buddy saved the full name
	/// (<c>LAS|BRWNZ FOUR (RNAV)</c>), which reads as <c>LAS|BRWNZ</c> - and each pair once.
	/// </summary>
	private static IEnumerable<ProcedureAirportPickItem> ParseAirportProcedurePicks(string? saved)
	{
		HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

		foreach (string entry in SplitOrdered(saved))
		{
			string[] parts = entry.Split('|');

			if (parts.Length != 2)
			{
				continue;
			}

			string airport = parts[0].Trim();
			string procedure = parts[1].Trim();

			if (airport.Length == 0 || procedure.Length == 0)
			{
				continue;
			}

			string root = ProcedureNaming.RootName(procedure);

			if (seen.Add($"{airport}|{root}"))
			{
				yield return new ProcedureAirportPickItem(airport, root, PairLabel(airport, root));
			}
		}
	}

	/// <summary>Which facility the documents list first: the user's own, from Settings ▸ Facility.</summary>
	private static string DescribePrimaryFacility() =>
		NormalizeFacility(UserConfigFile.GetValue(SettingsViewModel.ArtccKey)) is { } primary
			? $"{primary} (Settings ▸ Facility), then the rest alphabetically"
			: "Alphabetically (no facility set in Settings ▸ Facility)";

	/// <summary>What Add does with one entry in the Airports card's Add box.</summary>
	private enum AirportEntryOutcome
	{
		Add,
		Repeat,
		NotFound,
		AlreadyListed,
		AlreadyIncluded,
		OverLimit,
	}

	/// <summary>One entry in the Add box, and what Add does with it.</summary>
	/// <param name="Text">The entry as typed, upper-cased.</param>
	/// <param name="FaaId">Its FAA ID, or empty when it isn't an airport in this cycle.</param>
	/// <param name="Outcome">What Add does with it.</param>
	/// <param name="IncludedBy">For <see cref="AirportEntryOutcome.AlreadyIncluded"/>, e.g. <c>by ZOB under Facilities</c>.</param>
	private readonly record struct AirportEntry(string Text, string FaaId, AirportEntryOutcome Outcome, string? IncludedBy = null);
}
