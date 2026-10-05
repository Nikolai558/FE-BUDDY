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
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using DtppAirport = FeBuddy.Core.Infrastructure.Dtpp.Models.DtppMetafileXmlDataModel.Airport;
using DtppRecord = FeBuddy.Core.Infrastructure.Dtpp.Models.DtppMetafileXmlDataModel.Record;

// The base class already has a "FebProperties" member (the FE-Buddy Properties card's toggle
// list), which would otherwise hide Core's static FebProperties class in this file.
using CoreFebProperties = FeBuddy.Core.Application.Airac.FebProperties;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Procedures</b> sub-service tab inside the AIRAC Service screen: which of the two documents
/// and the FAA Chart Recall alias file are on (set on the General tab), which
/// facilities/airports/procedures the documents include, the chart types a whole included airport
/// is limited to, and the optional <c>Procedures.json</c> fields.
/// </summary>
/// <remarks>
/// <para>
/// Like Telephony, Procedures writes no GeoJSON and has no FE-Buddy properties - it still derives
/// from <see cref="GeojsonSubServiceViewModel"/> for the alias file and the Region of Interest
/// override plumbing, but never shows the What Files Do You Want?, FE-Buddy Properties or CRC ERAM
/// Defaults cards: <see cref="EmitKeys"/> is <c>(null, null, null)</c>, and
/// <see cref="OutputFiles"/> offers only the alias file.
/// </para>
/// <para>
/// The alias file covers every airport in the d-TPP Metafile; the facility, airport, procedure and
/// chart type choices pick what the two documents cover, so they are only checked while a document
/// is on.
/// </para>
/// <para>
/// Its data comes from two places besides the selected cycle's NASR data: the FAA d-TPP Metafile
/// (see <see cref="SetDtppData"/>), which may not be published yet, and the facility saved in
/// Settings ▸ Facility Profile (<see cref="SettingsViewModel.ArtccKey"/>), read fresh whenever it
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
	/// The most airports the Airports card holds. Past this many, ticking their ARTCC under
	/// Facilities, or the region of interest, is the better tool, and the list stays readable.
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

	private bool _includeRoiAirports;
	private bool _suppressListChanges;

	private HashSet<string> _savedFacilities = new(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, string> _nasrAirportsByIdent = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, AptCsvDataModel.AptBase> _nasrAirportsByFaaId = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, DtppAirport> _metafileAirportsByIdent = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, List<string>> _procedureNamesByAirport = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _procedureNamesByCanonical = new(StringComparer.OrdinalIgnoreCase);

	private bool _dtppKnown;
	private bool _hasDtppData;
	private string _dtppStatus = string.Empty;
	private string _dtppPreviousStatus = string.Empty;

	private string _newAirportText = string.Empty;
	private string _newProcedureText = string.Empty;

	private bool _isEditingPair;
	private int _editingPairIndex = -1;
	private string _pairAirportText = string.Empty;
	private string? _selectedPairProcedure;
	private IReadOnlyList<string> _pairProcedureOptions = [];

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public ProceduresViewModel()
	{
		Facilities.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFacilities));
		Airports.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAirports));
		ProcedureNames.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasProcedureNames));
		AirportProcedures.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAirportProcedures));

		ChartTypeToggles = [.. ChartTypeOptions
			.Select(o => new ProcedureOptionToggle(o.Code, $"{o.Label} ({o.Code})", isSelected: false, OnListToggleChanged))];

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

		LoadFromConfig();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "Procedures";

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
	}

	/// <inheritdoc />
	protected override string NoRoiEffect =>
		"\"Also include every airport inside the region of interest\" has nothing to select";

	/// <inheritdoc />
	/// <remarks>Procedures writes no GeoJSON at all.</remarks>
	protected override (string? Lines, string? Symbols, string? Text) EmitKeys => (null, null, null);

	/// <inheritdoc />
	/// <remarks>The FAA Chart Recall alias file, <c>Faa_Chart_Recall.txt</c>.</remarks>
	protected override bool HasAliasFile => true;

	// ================= d-TPP data =================

	/// <summary>Whether <see cref="SetDtppData"/> has been called yet for the currently selected cycle.</summary>
	public bool IsDtppKnown => _dtppKnown;

	/// <summary>Whether the selected cycle has a downloaded, parsed d-TPP Metafile. Only meaningful once <see cref="IsDtppKnown"/> is true.</summary>
	public bool HasDtppData => _hasDtppData;

	/// <summary>What the d-TPP Data card and the Preview Settings tab say about the metafile's availability.</summary>
	public string DtppStatus => _dtppKnown ? _dtppStatus : "Chart data appears once the selected cycle's data is loaded.";

	/// <summary>The second d-TPP Data card line, about whether the previous cycle's metafile is available for deleted-procedure links.</summary>
	public string DtppPreviousStatus => _dtppKnown ? _dtppPreviousStatus : string.Empty;

	/// <summary>
	/// Called by the AIRAC Service screen whenever the selected cycle's d-TPP Metafile has been
	/// (re)loaded, to report whether it is available and build the airport/procedure lookups the
	/// Airports, Procedures at Any Airport and Airport + Procedure cards need.
	/// </summary>
	/// <param name="cycleId">The selected cycle's ID.</param>
	/// <param name="dtpp">The selected cycle's parsed metafile, or <see langword="null"/> when it has not downloaded yet.</param>
	/// <param name="previousCycleId">The ID of the cycle immediately before <paramref name="cycleId"/>.</param>
	/// <param name="previousAvailable">Whether the previous cycle's metafile is available, for linking deleted procedures back to their chart.</param>
	/// <param name="downloadedLocal">When the selected cycle's metafile was downloaded, or <see langword="null"/> when it has none.</param>
	/// <remarks>
	/// This card never blocks the run (a missing metafile is reported as an advisory by the Core
	/// run itself) and is not a saved setting, so this never marks the tab dirty - it only
	/// re-validates the airport/procedure pickers, whose Add buttons depend on this data.
	/// </remarks>
	public void SetDtppData(string cycleId, DtppMetafileDataCollection? dtpp, string previousCycleId, bool previousAvailable, DateTime? downloadedLocal)
	{
		ArgumentNullException.ThrowIfNull(cycleId);
		ArgumentNullException.ThrowIfNull(previousCycleId);

		_dtppKnown = true;
		_hasDtppData = dtpp is not null;

		_metafileAirportsByIdent.Clear();
		_procedureNamesByAirport.Clear();
		_procedureNamesByCanonical.Clear();

		if (dtpp is not null)
		{
			foreach (DtppAirport airport in dtpp.Airports)
			{
				_metafileAirportsByIdent.TryAdd(airport.AptIdent, airport);

				if (!string.IsNullOrWhiteSpace(airport.IcaoIdent))
				{
					_metafileAirportsByIdent.TryAdd(airport.IcaoIdent!, airport);
				}
			}

			foreach (IGrouping<string, DtppRecord> group in dtpp.Records
				.Where(r => !ProcedureNaming.IsContinuation(r.ChartName))
				.GroupBy(r => r.AptIdent, StringComparer.OrdinalIgnoreCase))
			{
				List<string> names = [];
				HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

				foreach (DtppRecord record in group.OrderBy(r => r.ChartSeq))
				{
					string baseName = ProcedureNaming.BaseName(record.ChartName);

					if (seen.Add(baseName))
					{
						names.Add(baseName);
						_procedureNamesByCanonical.TryAdd(baseName, baseName);
					}
				}

				_procedureNamesByAirport[group.Key] = names;
			}
		}

		ProcedureNameOptions = [.. _procedureNamesByCanonical.Values.OrderBy(n => n, StringComparer.OrdinalIgnoreCase)];

		int airportCount = dtpp?.Airports.Count ?? 0;
		int procedureCount = dtpp?.Records.Count(r => !ProcedureNaming.IsContinuation(r.ChartName)) ?? 0;

		_dtppStatus = dtpp is not null
			? $"Cycle {cycleId}: {airportCount:N0} airports, {procedureCount:N0} procedures, downloaded {downloadedLocal:d MMM yyyy}."
			: $"Cycle {cycleId}'s metafile isn't published yet (or couldn't be downloaded). FE-Buddy checks again at each launch; until then a run writes none of Procedures' files (Procedure_Changes.md, Procedures.json, Faa_Chart_Recall.txt).";

		_dtppPreviousStatus = previousAvailable
			? $"Deleted procedures link to cycle {previousCycleId}'s charts."
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

	/// <summary>What the Facilities card says about which facility is listed first in the documents.</summary>
	public string PrimaryFacilityCaption
	{
		get
		{
			string? primary = NormalizeFacility(UserConfigFile.GetValue(SettingsViewModel.ArtccKey));

			return primary is null
				? "No facility is set in Settings ▸ Facility Profile, so facilities are listed alphabetically."
				: $"Listed first in the documents: {primary} — your facility in Settings ▸ Facility Profile.";
		}
	}

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
				OnPropertyChanged(nameof(NewAirportHint));
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

	/// <summary>Whether every included airport also includes every airport inside the region of interest set below.</summary>
	public bool IncludeRoiAirports
	{
		get => _includeRoiAirports;
		set { if (SetProperty(ref _includeRoiAirports, value)) MarkDirty(); }
	}

	// ================= procedures at any airport =================

	/// <summary>The procedure base names (canonical spelling) added to the Procedures at Any Airport card, in the order they were added.</summary>
	public ObservableCollection<string> ProcedureNames { get; } = [];

	/// <summary>Whether any procedure name has been added.</summary>
	public bool HasProcedureNames => ProcedureNames.Count > 0;

	/// <summary>The text typed into (or picked in) the Procedures at Any Airport card's combo box.</summary>
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

	/// <summary>Every distinct procedure base name in the metafile, sorted, for the combo box's items.</summary>
	public IReadOnlyList<string> ProcedureNameOptions { get; private set; } = [];

	/// <summary>"Not a procedure in this cycle." or "Already in the list.", or empty when the typed text is fine to add.</summary>
	public string NewProcedureHint
	{
		get
		{
			string trimmed = _newProcedureText.Trim();

			if (trimmed.Length == 0)
			{
				return string.Empty;
			}

			if (!_procedureNamesByCanonical.TryGetValue(trimmed, out string? canonical))
			{
				return "Not a procedure in this cycle.";
			}

			return ProcedureNames.Any(p => p.Equals(canonical, StringComparison.OrdinalIgnoreCase)) ? AlreadyInListHint : string.Empty;
		}
	}

	/// <summary>Adds <see cref="NewProcedureText"/> (canonical spelling) to <see cref="ProcedureNames"/>.</summary>
	public ICommand AddProcedureCommand { get; }

	/// <summary>Clears <see cref="NewProcedureText"/> without adding it. The list is left as it is.</summary>
	public ICommand CancelProcedureCommand { get; }

	/// <summary>Removes a procedure name from <see cref="ProcedureNames"/>.</summary>
	public ICommand DeleteProcedureCommand { get; }

	// ================= airport + procedure =================

	/// <summary>The airport + procedure pairs added to the Airport + Procedure card, in the order they were added.</summary>
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

			if (ResolvePairAirport() is { } airport && _selectedPairProcedure is { } procedure && IsDuplicatePair(airport.AptIdent, procedure))
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
	/// NASR airport lookup the Airports card falls back to before the d-TPP Metafile is loaded.
	/// </remarks>
	public void LoadCycleDependentLists(NasrCsvDataCollection data)
	{
		ArgumentNullException.ThrowIfNull(data);

		bool hasSavedFacilities = Get("Facilities") is not null;
		_savedFacilities = ParseList(Get("Facilities"));

		string[] facilityIds = [.. (data.Apt?.AptBase ?? [])
			.Select(a => (a.RespArtccId ?? string.Empty).Trim().ToUpperInvariant())
			.Where(id => id.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(id => id, StringComparer.OrdinalIgnoreCase)];

		string? settingsFacility = NormalizeFacility(UserConfigFile.GetValue(SettingsViewModel.ArtccKey));

		_suppressListChanges = true;
		try
		{
			Facilities.Clear();

			foreach (string id in facilityIds)
			{
				// When the tab has never been saved, start with the Settings facility ticked
				// instead of nothing.
				bool isSelected = hasSavedFacilities
					? _savedFacilities.Contains(id)
					: id.Equals(settingsFacility, StringComparison.OrdinalIgnoreCase);

				Facilities.Add(new ArtccToggle(id, isSelected, OnListToggleChanged));
			}
		}
		finally
		{
			_suppressListChanges = false;
		}

		_nasrAirportsByIdent.Clear();
		_nasrAirportsByFaaId.Clear();

		foreach (AptCsvDataModel.AptBase row in data.Apt?.AptBase ?? [])
		{
			string faaId = (row.ArptId ?? string.Empty).Trim();

			if (faaId.Length == 0)
			{
				continue;
			}

			_nasrAirportsByIdent.TryAdd(faaId, faaId);
			_nasrAirportsByFaaId.TryAdd(faaId, row);

			string icao = (row.IcaoId ?? string.Empty).Trim();

			if (icao.Length > 0)
			{
				_nasrAirportsByIdent.TryAdd(icao, faaId);
			}
		}

		OnPropertyChanged(nameof(PrimaryFacilityCaption));
		OnPropertyChanged(nameof(NewAirportHint));

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
			["Facilities"] = string.Join(',', SelectedFacilityIds()),
			// Read fresh from Settings rather than this tab's own config - see the class remarks.
			["PrimaryFacility"] = UserConfigFile.GetValue(SettingsViewModel.ArtccKey) ?? string.Empty,
			["IncludeRoiAirports"] = YesNo(_includeRoiAirports),
			["Airports"] = string.Join(',', Airports),
			["Procedures"] = string.Join(',', ProcedureNames),
			["AirportProcedures"] = string.Join(',', AirportProcedures.Select(p => $"{p.Airport}|{p.ProcedureName}")),
			["ChartTypes"] = string.Join(',', ChartTypeToggles.Where(t => t.IsSelected).Select(t => t.Token)),
			["JsonFields"] = string.Join(',', JsonFieldToggles.Where(t => t.IsSelected).Select(t => t.Token)),
		};

		AddSharedSettings(s);
		return s;
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		ServicePreviewRow[] rows =
		[
			new ServicePreviewRow("Documents", DescribeDocuments()),
			new ServicePreviewRow("Alias file",
				GenerateAliasFile ? $"{ProcedureOutputFiles.Alias}, every chart at every airport in the d-TPP metafile - the choices below never limit it" : "No"),
			new ServicePreviewRow("Facilities", DescribeFacilities()),
			new ServicePreviewRow("Airports", Airports.Count > 0 ? string.Join(", ", Airports) : "None"),
			new ServicePreviewRow("Procedures", ProcedureNames.Count > 0 ? string.Join(", ", ProcedureNames) : "None"),
			new ServicePreviewRow("Airport + procedure", AirportProcedures.Count > 0 ? string.Join(", ", AirportProcedures.Select(p => p.Label)) : "None"),
			new ServicePreviewRow("Chart types", DescribeChartTypes()),
			new ServicePreviewRow("Region of interest", _includeRoiAirports ? $"Used for airport inclusion — {DescribeRoi()}" : "Not used for airport inclusion"),
			new ServicePreviewRow("d-TPP data", DtppStatus),
		];

		return [new ServicePreviewSection("Procedures", rows)];
	}

	// ================= save contract =================

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		_suppressListChanges = true;
		try
		{
			_includeRoiAirports = GetBool("IncludeRoiAirports", false);

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

			ProcedureNames.Clear();
			foreach (string name in SplitOrdered(Get("Procedures")))
			{
				ProcedureNames.Add(name);
			}

			AirportProcedures.Clear();
			foreach (ProcedureAirportPickItem item in ParseAirportProcedurePicks(Get("AirportProcedures")))
			{
				AirportProcedures.Add(item);
			}

			string? savedChartTypesRaw = Get("ChartTypes");
			HashSet<string> savedChartTypes = string.IsNullOrWhiteSpace(savedChartTypesRaw)
				? new HashSet<string>(ProcedureChartTypes.Default, StringComparer.OrdinalIgnoreCase)
				: ParseList(savedChartTypesRaw);

			foreach (ProcedureOptionToggle toggle in ChartTypeToggles)
			{
				toggle.IsSelected = savedChartTypes.Contains(toggle.Token);
			}

			string? savedJsonFieldsRaw = Get("JsonFields");
			HashSet<string> savedJsonFields = string.IsNullOrWhiteSpace(savedJsonFieldsRaw)
				? new HashSet<string>(ProcedureSettingsParser.DefaultJsonFields.Select(CoreFebProperties.Name), StringComparer.OrdinalIgnoreCase)
				: ParseList(savedJsonFieldsRaw);

			foreach (ProcedureOptionToggle toggle in JsonFieldToggles)
			{
				toggle.IsSelected = savedJsonFields.Contains(toggle.Token);
			}

			LoadSharedSettings();
		}
		finally
		{
			_suppressListChanges = false;
		}

		RaiseOwnSettingProperties();
		ClearDirty();
	}

	/// <inheritdoc />
	protected override void WriteToConfig()
	{
		Set("Facilities", string.Join(',', SelectedFacilityIds()));
		Set("IncludeRoiAirports", YesNo(_includeRoiAirports));
		Set("Airports", string.Join(',', Airports));
		Set("Procedures", string.Join(',', ProcedureNames));
		Set("AirportProcedures", string.Join(',', AirportProcedures.Select(p => $"{p.Airport}|{p.ProcedureName}")));
		Set("ChartTypes", string.Join(',', ChartTypeToggles.Where(t => t.IsSelected).Select(t => t.Token)));
		Set("JsonFields", string.Join(',', JsonFieldToggles.Where(t => t.IsSelected).Select(t => t.Token)));
		SaveSharedSettings();
	}

	/// <inheritdoc />
	protected override void Validate(ServiceValidation validation)
	{
		// The Add box's hint depends on the facilities, the region and the list, and any change to
		// them re-validates.
		OnPropertyChanged(nameof(NewAirportHint));

		// The choices below only pick what the documents cover; the alias file covers every airport.
		if (GeneratesDocument)
		{
			ValidateDocumentSelection(validation);
		}

		ValidateSharedSettings(validation);
	}

	/// <summary>Checks that the documents' facility, airport, procedure and chart type choices include something.</summary>
	private void ValidateDocumentSelection(ServiceValidation validation)
	{
		bool hasFacility = SelectedFacilityIds().Any();
		bool hasWholeAirportSource = hasFacility || _includeRoiAirports || Airports.Count > 0;
		bool hasInclusionSource = hasWholeAirportSource || ProcedureNames.Count > 0 || AirportProcedures.Count > 0;

		if (!hasInclusionSource)
		{
			validation.AddArea(
				ServiceAreas.DocumentSelection,
				"Pick at least one facility, airport or procedure for the documents to include.");
		}

		if (hasWholeAirportSource && ChartTypeToggles.All(t => !t.IsSelected))
		{
			validation.AddArea(
				ServiceAreas.ChartTypes,
				"No chart types are ticked. Tick at least one chart type below, so the facilities, airports or region you picked actually include something.");
		}

		if (_includeRoiAirports && !HasRoi)
		{
			validation.AddArea(
				ServiceAreas.Roi,
				"Set a region of interest below (or a default one in Settings), or untick "
				+ "'Also include every airport inside the region of interest'.");
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
			if (_metafileAirportsByIdent.TryGetValue(text, out DtppAirport? airport))
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
	/// Works out what Add would do with each entry in the Add box, in order: add it, skip a repeat
	/// of one it adds, or leave it with a reason.
	/// </summary>
	private List<AirportEntry> ReadAirportEntries()
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
		HashSet<string> facilities = new(SelectedFacilityIds(), StringComparer.OrdinalIgnoreCase);
		RegionOfInterest? region = _includeRoiAirports ? RoiInUse() : null;

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
	/// How the run already includes the whole airport, the way the library decides it: its
	/// responsible ARTCC is ticked under Facilities, or it is inside the region of interest while
	/// that box is ticked. <see langword="null"/> when nothing does.
	/// </summary>
	private string? IncludedBy(string faaId, HashSet<string> facilities, RegionOfInterest? region)
	{
		if (!_nasrAirportsByFaaId.TryGetValue(faaId, out AptCsvDataModel.AptBase? row))
		{
			return null;
		}

		string artcc = (row.RespArtccId ?? string.Empty).Trim();

		if (artcc.Length > 0 && facilities.Contains(artcc))
		{
			return $"by {artcc.ToUpperInvariant()} under Facilities";
		}

		return region is not null && RoiFilter.Contains(region, row.BaseLatDecimal, row.BaseLongDecimal)
			? "by the region of interest"
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
		_procedureNamesByCanonical.TryGetValue(_newProcedureText.Trim(), out string? canonical)
		&& !ProcedureNames.Any(p => p.Equals(canonical, StringComparison.OrdinalIgnoreCase));

	private void AddProcedure()
	{
		if (!_procedureNamesByCanonical.TryGetValue(_newProcedureText.Trim(), out string? canonical)
			|| ProcedureNames.Any(p => p.Equals(canonical, StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}

		ProcedureNames.Add(canonical);
		NewProcedureText = string.Empty;
		MarkDirty();
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

		return _metafileAirportsByIdent.TryGetValue(trimmed, out DtppAirport? airport) ? airport : null;
	}

	private void RefreshPairProcedureOptions()
	{
		_pairProcedureOptions = ResolvePairAirport() is { } airport
			&& _procedureNamesByAirport.TryGetValue(airport.AptIdent, out List<string>? names)
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
		SelectedPairProcedure = _pairProcedureOptions.FirstOrDefault(p => p.Equals(item.ProcedureName, StringComparison.OrdinalIgnoreCase)) ?? item.ProcedureName;
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
		ResolvePairAirport() is { } airport && _selectedPairProcedure is { } procedure && !IsDuplicatePair(airport.AptIdent, procedure);

	private void ConfirmPair()
	{
		if (ResolvePairAirport() is not { } airport || _selectedPairProcedure is not { } procedure)
		{
			return;
		}

		string label = $"{airport.AptIdent} — {procedure}";

		if (_editingPairIndex >= 0 && _editingPairIndex < AirportProcedures.Count)
		{
			ProcedureAirportPickItem item = AirportProcedures[_editingPairIndex];
			item.Airport = airport.AptIdent;
			item.ProcedureName = procedure;
			item.Label = label;
		}
		else
		{
			AirportProcedures.Add(new ProcedureAirportPickItem(airport.AptIdent, procedure, label));
		}

		EndPairEdit();
		MarkDirty();
	}

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

	private void RaiseDtppDependentProperties()
	{
		OnPropertyChanged(nameof(IsDtppKnown));
		OnPropertyChanged(nameof(HasDtppData));
		OnPropertyChanged(nameof(DtppStatus));
		OnPropertyChanged(nameof(DtppPreviousStatus));
		OnPropertyChanged(nameof(ProcedureNameOptions));
		OnPropertyChanged(nameof(NewAirportHint));
		OnPropertyChanged(nameof(NewProcedureHint));
		OnPropertyChanged(nameof(PairAirportName));
		OnPropertyChanged(nameof(PairEditorHint));
	}

	private void RaiseOwnSettingProperties()
	{
		OnPropertyChanged(nameof(IncludeRoiAirports));
		OnPropertyChanged(nameof(PrimaryFacilityCaption));
	}

	private IEnumerable<string> SelectedFacilityIds() =>
		Facilities.Count > 0
			? Facilities.Where(f => f.IsSelected).Select(f => f.Artcc)
			: _savedFacilities.OrderBy(a => a, StringComparer.OrdinalIgnoreCase);

	private static string? NormalizeFacility(string? value) =>
		string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

	private static IEnumerable<string> SplitOrdered(string? saved) =>
		(saved ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	private static IEnumerable<ProcedureAirportPickItem> ParseAirportProcedurePicks(string? saved)
	{
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

			yield return new ProcedureAirportPickItem(airport, procedure, $"{airport} — {procedure}");
		}
	}

	private string DescribeDocuments()
	{
		List<string> docs = [];
		if (GenerateChangesDocument) docs.Add("Procedure_Changes.md");
		if (GenerateProceduresJson) docs.Add("Procedures.json");

		return docs.Count > 0 ? string.Join(", ", docs) : "None";
	}

	private string DescribeFacilities()
	{
		string[] selected = [.. SelectedFacilityIds()];

		if (selected.Length == 0)
		{
			return "None";
		}

		string? primary = NormalizeFacility(UserConfigFile.GetValue(SettingsViewModel.ArtccKey));
		string list = string.Join(", ", selected);

		return primary is null ? list : $"{list} ({primary} listed first)";
	}

	private string DescribeChartTypes()
	{
		string[] selected = [.. ChartTypeToggles.Where(t => t.IsSelected).Select(t => t.Token)];
		return selected.Length > 0 ? string.Join(", ", selected) : "None";
	}

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
