using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Departures;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Airac;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Infrastructure.Configuration;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Departures</b> sub-service tab inside the AIRAC Service screen: which outputs are on (set
/// on the General tab), which GeoJSON files, the area (the ARTCCs ticked, an ROI, or every
/// departure), obstacle departures and the amendment date, which FE-Buddy properties and the CRC
/// ERAM defaults.
/// </summary>
/// <remarks>
/// Save, Undo and navigation come from the tab host's action bar; the run is launched by
/// <b>Run AIRAC Service</b> on the Preview Settings tab, and its results are shown on the Review
/// tab, described by this tab through <see cref="ISubServiceRunTarget"/>.
/// </remarks>
public sealed class DeparturesViewModel : GeojsonSubServiceViewModel, ISubServiceRunTarget
{
	private const string Node = $"{UserConfigAreas.AiracNode}.{AiracTabs.Departures}";
	private const string CrcClassName = "Departures";

	private bool _includeObstacleDepartures = true;
	private DepartureRoiMode _roiMode = DepartureRoiMode.Airport;
	private DepartureAmendmentFilter _amendmentFilter = DepartureAmendmentFilter.None;
	private DepartureAmendmentFilter _lastAmendmentLimit = DepartureAmendmentFilter.Cycles;
	private string _amendedWithinCycles = "1";
	private string _amendedWithinDays = "30";
	private DateTime? _amendedOnOrAfter;
	private HashSet<string> _savedArtccFilter = new(StringComparer.OrdinalIgnoreCase);
	private bool _suppressArtccChanges;

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public DeparturesViewModel()
	{
		FebProperties = FebPropertyToggle.ListFor(DepartureFebPropertyOptions.All, MarkDirty);
		LineDefaults = [new(CrcClassName, EramFieldKind.Line, MarkDirty)];
		SymbolDefaults = [new(CrcClassName, EramFieldKind.Symbol, MarkDirty)];
		TextDefaults = [new(CrcClassName, EramFieldKind.Text, MarkDirty)];

		Artccs.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasArtccs));
		ClearArtccsCommand = new RelayCommand(ClearArtccs, () => Artccs.Any(a => a.IsSelected));

		LoadFromConfig();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "Departures";

	// ================= outputs =================

	/// <inheritdoc />
	protected override IReadOnlyList<SubServiceArea> Areas => SubServiceSettingsReader.ArtccOrRoiAreas;

	/// <inheritdoc />
	protected override IEnumerable<string> AreaArtccIds() => SelectedArtccs();

	/// <inheritdoc />
	/// <remarks>The area narrows the alias file and the GeoJSON alike.</remarks>
	public override SubServiceOutputKinds AreaOutputs => SubServiceOutputKinds.Alias | SubServiceOutputKinds.Geojson;

	/// <inheritdoc />
	/// <remarks>
	/// A run writes a set of files per airport and procedure - often thousands - so they are
	/// chosen by kind: one row for every procedure's Lines, Symbols and Text, then the alias file.
	/// </remarks>
	protected override IEnumerable<OutputFileOption> OutputFiles()
	{
		if (GenerateGeojson)
		{
			if (EmitLines)
			{
				yield return GeojsonFiles(CrcFeatureKind.Line, EramFieldKind.Line);
			}

			if (EmitSymbols)
			{
				yield return GeojsonFiles(CrcFeatureKind.Symbol, EramFieldKind.Symbol);
			}

			if (EmitText)
			{
				yield return GeojsonFiles(CrcFeatureKind.Text, EramFieldKind.Text);
			}
		}

		if (GenerateAliasFile)
		{
			yield return OutputFileOption.AliasFile(DepartureOutputFiles.Alias);
		}
	}

	private static OutputFileOption GeojsonFiles(CrcFeatureKind kind, EramFieldKind field)
	{
		string suffix = AiracOutputPaths.FileKindSuffix(kind);
		return new OutputFileOption(
			DepartureOutputFiles.KeyFor(kind), "Every procedure", suffix, $"every procedure's {suffix} file",
			IsGeojson: true, [(CrcClassName, field)]);
	}

	/// <inheritdoc />
	/// <remarks>
	/// Each procedure's GeoJSON files are named from the FAA's data, so they are listed by the pattern
	/// their names follow, and can't be renamed. The alias file can.
	/// </remarks>
	public override IEnumerable<OutputFileEntry> OutputFileEntries() =>
		OutputFiles().Select(file => file.IsGeojson
			? new OutputFileEntry(
				file.Key, DepartureOutputFiles.GeojsonFolderPattern, $"{DepartureOutputFiles.GeojsonFileNamePattern}_{file.Label}.geojson", Title, CanRename: false)
			: OutputFileEntry.Renamable(file.Key, FolderOf(file), Title));

	// ================= procedures =================

	/// <summary>Whether obstacle departures (ODPs) are written alongside the SIDs.</summary>
	public bool IncludeObstacleDepartures
	{
		get => _includeObstacleDepartures;
		set { if (SetProperty(ref _includeObstacleDepartures, value)) MarkDirty(); }
	}

	/// <summary>
	/// One toggle per ARTCC in the selected cycle's <c>DP_BASE</c>: the ARTCCs area's ARTCCs.
	/// Empty until the cycle's data is loaded.
	/// </summary>
	public ObservableCollection<ArtccToggle> Artccs { get; } = [];

	/// <summary>Whether the ARTCC list has been built from the selected cycle yet.</summary>
	public bool HasArtccs => Artccs.Count > 0;

	/// <summary>Deselects every ARTCC.</summary>
	public ICommand ClearArtccsCommand { get; }

	/// <summary>
	/// The amendment date's question, naming the selected cycle: <c>... within a certain time frame
	/// from the 2610 cycle?</c>
	/// </summary>
	public string AmendmentQuestion =>
		"Do you want to reduce the number of files created by FE-Buddy by having it only export files for procedures "
		+ $"that were amended by the FAA within a certain time frame from the {SelectedCycle?.AiracCycleId ?? "selected"} cycle?";

	/// <summary>What 1 means for <see cref="AmendedWithinCycles"/>: <c>(1 = only procedures amended in 2610)</c>.</summary>
	public string AmendmentCyclesCaption =>
		$"(1 = only procedures amended in {(SelectedCycle is { } cycle ? cycle.AiracCycleId : "the selected cycle")})";

	/// <summary>"No" to the amendment date's question: procedures are kept whatever their amendment date.</summary>
	public bool AmendmentAny
	{
		get => _amendmentFilter == DepartureAmendmentFilter.None;
		set { if (value) SetAmendmentFilter(DepartureAmendmentFilter.None); }
	}

	/// <summary>
	/// "Yes" to the amendment date's question: only procedures amended recently are kept, by the way
	/// picked under it - the one picked last time, or by cycles to start.
	/// </summary>
	public bool AmendmentLimited
	{
		get => _amendmentFilter != DepartureAmendmentFilter.None;
		set { if (value && _amendmentFilter == DepartureAmendmentFilter.None) SetAmendmentFilter(_lastAmendmentLimit); }
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

	// ================= area =================

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

	// ================= parent hooks =================

	/// <inheritdoc />
	/// <remarks>
	/// Builds the ARTCC toggle list from the cycle's <c>DP_BASE</c>. An ARTCC already listed keeps
	/// what is ticked on screen; a new one starts as saved (or as the Settings facility).
	/// </remarks>
	public void LoadCycleDependentLists(AiracCycleOptionLists lists)
	{
		ArgumentNullException.ThrowIfNull(lists);

		_savedArtccFilter = ParseArtccListOrFacility(Get("ArtccFilter"));
		Dictionary<string, bool> shown = Artccs.ToDictionary(t => t.Artcc, t => t.IsSelected, StringComparer.OrdinalIgnoreCase);

		Artccs.Clear();
		foreach (string artcc in lists.DepartureArtccs)
		{
			bool selected = shown.TryGetValue(artcc, out bool onScreen) ? onScreen : _savedArtccFilter.Contains(artcc);
			Artccs.Add(new ArtccToggle(artcc, selected, OnArtccToggled));
		}

		// The list was empty when this tab snapshotted itself at construction; re-take the
		// snapshot now the toggles reflect what is actually saved (or the Settings facility).
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
			summary += $", {Path.GetFileName(departures.AliasFilePath)}: {departures.AliasCommandCount:N0} alias command(s)";
		}

		return new SubServiceRunResult(Title, summary, departures.Messages);
	}

	/// <inheritdoc />
	public IReadOnlyDictionary<string, string> BuildSettingsBlock()
	{
		Dictionary<string, string> s = new(StringComparer.OrdinalIgnoreCase)
		{
			["GenerateGeojson"] = YesNo(GenerateGeojson),
			["IncludeObstacleDepartures"] = YesNo(IncludeObstacleDepartures),
			["AmendmentFilter"] = _amendmentFilter.ToString(),
		};

		// Only what the area uses, so the parser sees exactly what the run will do.
		if (Area == SubServiceArea.Artccs)
		{
			s["ArtccFilter"] = string.Join(',', SelectedArtccs());
		}
		else if (Area == SubServiceArea.Roi)
		{
			s["RoiMode"] = _roiMode.ToString();
		}

		// Only the active mode's value; the parser reads that key alone and ignores the others.
		switch (_amendmentFilter)
		{
			case DepartureAmendmentFilter.Cycles:
				s["AmendedWithinCycles"] = AmendedWithinCycles.Trim();
				break;
			case DepartureAmendmentFilter.Days:
				s["AmendedWithinDays"] = AmendedWithinDays.Trim();
				break;
			case DepartureAmendmentFilter.Date when AmendedOnOrAfter is { } onOrAfter:
				s["AmendedOnOrAfter"] = onOrAfter.ToString(ProcedureAmendmentFilter.DateFormat, CultureInfo.InvariantCulture);
				break;
		}

		AddSharedSettings(s);
		return s;
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary() =>
		[new ServicePreviewSection("Departures", PreviewRows()) { WhatYoullGet = WhatYoullGet }];

	/// <inheritdoc />
	/// <remarks>
	/// The filters narrow the alias file and the GeoJSON alike, so there is one block: the area
	/// first, then the amendment date, then the files of whichever outputs are on.
	/// </remarks>
	protected override IEnumerable<SummaryBlock> BuildWhatYoullGet()
	{
		// Before a cycle is parsed the toggle list is empty; SelectedArtccs falls back to what is saved.
		string[] artccs = Area == SubServiceArea.Artccs ? [.. SelectedArtccs()] : [];
		bool noArtccs = Area == SubServiceArea.Artccs && artccs.Length == 0;

		string departures = noArtccs ? "No departures yet: tick an ARTCC on the Area card"
			: IncludeObstacleDepartures ? "Every SID and obstacle departure"
			: "Every SID (no obstacle departures)";

		List<SummaryFile> files = [];

		if (!noArtccs && GenerateGeojson)
		{
			if (EmitLines)
			{
				files.Add(new($"{DepartureOutputFiles.GeojsonFileNamePattern}_Lines.geojson", "Each departure's routes, drawn as lines."));
			}

			if (EmitSymbols)
			{
				files.Add(new($"{DepartureOutputFiles.GeojsonFileNamePattern}_Symbols.geojson", "Each departure's waypoint symbols."));
			}

			if (EmitText)
			{
				files.Add(new($"{DepartureOutputFiles.GeojsonFileNamePattern}_Text.geojson", "Each departure's waypoint IDs."));
			}
		}

		if (!noArtccs && GenerateAliasFile)
		{
			files.Add(new(DepartureOutputFiles.Alias,
				"A command for each departure at each airport that draws its waypoints on a CRC STARS or ERAM window.\n"
				+ "Example: `.laxGMNf .FF GMN VNY COREZ AVE EHF`\n"
				+ "See the Alias Command Guide, under Info, for more examples and exceptions."));
		}

		yield return new SummaryBlock(SubServiceOutputKinds.Alias | SubServiceOutputKinds.Geojson, new SummaryLines()
			.Add(SummaryJoin.First, departures)
			.Add(SummaryJoin.In, artccs.Length == 0 ? null : SummaryLines.Join(artccs, "and"))
			.Add(SummaryJoin.AndOnly, RoiLine(roi => _roiMode == DepartureRoiMode.Waypoint
				? $"those with at least one fix inside {roi}"
				: $"those from an airport inside {roi}"))
			.Add(SummaryJoin.AndOnly, noArtccs ? null : DescribeAmendmentFilter())
			.ToList())
		{
			Files = files,
			Notes = !noArtccs && GenerateGeojson ? [$"Each airport's GeoJSON files go in `{DepartureOutputFiles.GeojsonFolderPattern}\\`."] : [],
		};
	}

	/// <inheritdoc />
	/// <remarks>The amendment date's question and its cycles caption name the cycle.</remarks>
	protected override void OnSelectedCycleChanged()
	{
		OnPropertyChanged(nameof(AmendmentQuestion));
		OnPropertyChanged(nameof(AmendmentCyclesCaption));
	}

	// ================= save contract =================

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		_includeObstacleDepartures = GetBool("IncludeObstacleDepartures", true);
		LoadSharedSettings();

		// Re-apply the saved ARTCC filter to any already-built toggles, without a dirty check
		// per toggle; ClearDirty below re-takes the snapshot once.
		_savedArtccFilter = ParseArtccListOrFacility(Get("ArtccFilter"));
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

		// Filter by name only; anything else (a number, a typo) falls back to no filter.
		string? savedFilter = Get("Amendment.Filter")?.Trim();
		_amendmentFilter = Enum.GetValues<DepartureAmendmentFilter>()
			.FirstOrDefault(f => f.ToString().Equals(savedFilter, StringComparison.OrdinalIgnoreCase));
		_lastAmendmentLimit = _amendmentFilter == DepartureAmendmentFilter.None ? DepartureAmendmentFilter.Cycles : _amendmentFilter;
		_amendedWithinCycles = Get("Amendment.WithinCycles") ?? "1";
		_amendedWithinDays = Get("Amendment.WithinDays") ?? "30";
		_amendedOnOrAfter = DateTime.TryParseExact(
			Get("Amendment.OnOrAfter")?.Trim(), ProcedureAmendmentFilter.DateFormat, CultureInfo.InvariantCulture,
			DateTimeStyles.None, out DateTime onOrAfter)
			? onOrAfter
			: null;

		RaiseOwnSettingProperties();
		ClearDirty();
	}

	/// <inheritdoc />
	protected override void WriteToConfig()
	{
		Set("IncludeObstacleDepartures", YesNo(IncludeObstacleDepartures));
		Set("ArtccFilter", string.Join(',', SelectedArtccs()));
		Set("Roi.Mode", _roiMode.ToString());
		Set("Amendment.Filter", _amendmentFilter.ToString());
		Set("Amendment.WithinCycles", AmendedWithinCycles);
		Set("Amendment.WithinDays", AmendedWithinDays);
		Set("Amendment.OnOrAfter", AmendedOnOrAfter?.ToString(ProcedureAmendmentFilter.DateFormat, CultureInfo.InvariantCulture) ?? string.Empty);
		SaveSharedSettings();
	}

	/// <inheritdoc />
	protected override void Validate(ServiceValidation validation)
	{
		if (GenerateGeojson && !EmitLines && !EmitSymbols && !EmitText)
		{
			validation.AddArea(
				ServiceAreas.GeojsonFiles,
				"GeoJSON is on but none of its files are selected. Turn on Lines, Symbols, or Text, "
				+ "or turn GeoJSON off for Departures in the General tab.");
		}

		ValidateAmendmentFilter(validation);
		ValidateSharedSettings(validation);
	}

	// ================= helpers =================

	/// <summary>Checks only the active amendment mode's input; the others are kept but never read.</summary>
	/// <param name="validation">The validation being built.</param>
	private void ValidateAmendmentFilter(ServiceValidation validation)
	{
		switch (_amendmentFilter)
		{
			case DepartureAmendmentFilter.Cycles:
				if (!TryParseWholeNumber(AmendedWithinCycles, 1, ProcedureAmendmentFilter.MaxWithinCycles, out _))
				{
					validation.AddField(nameof(AmendedWithinCycles),
						$"Enter a whole number of cycles from 1 to {ProcedureAmendmentFilter.MaxWithinCycles}.");
				}

				break;

			case DepartureAmendmentFilter.Days:
				if (!TryParseWholeNumber(AmendedWithinDays, 1, ProcedureAmendmentFilter.MaxWithinDays, out _))
				{
					validation.AddField(nameof(AmendedWithinDays),
						$"Enter a whole number of days from 1 to {ProcedureAmendmentFilter.MaxWithinDays}.");
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

	/// <summary>The amendment filter as a What You'll Get line; <see langword="null"/> when there is none.</summary>
	/// <returns>e.g. <c>those amended in the last 4 cycles (since cycle 2607)</c>.</returns>
	private string? DescribeAmendmentFilter()
	{
		return _amendmentFilter switch
		{
			DepartureAmendmentFilter.Cycles => DescribeCyclesFilter(),
			DepartureAmendmentFilter.Days => TryParseWholeNumber(AmendedWithinDays, 1, ProcedureAmendmentFilter.MaxWithinDays, out int days) && days == 1
								? "those amended in the last day, counting back from today"
								: $"those amended in the last {AmendedWithinDays.Trim()} days, counting back from today",
			DepartureAmendmentFilter.Date => AmendedOnOrAfter is { } onOrAfter
								? $"those amended on or after {onOrAfter.ToString(ProcedureAmendmentFilter.DateFormat, CultureInfo.InvariantCulture)}"
								: "those amended on or after a date not yet picked",
			_ => null,
		};
	}

	/// <summary>
	/// The cycles filter as a What You'll Get line, naming the oldest cycle it keeps once the selected
	/// cycle is known: 1 is the selected cycle only, 3 with 2610 selected is 2608 to 2610.
	/// </summary>
	/// <returns>e.g. <c>those amended in cycle 2610</c> or <c>those amended in the last 3 cycles (since cycle 2608)</c>.</returns>
	private string DescribeCyclesFilter()
	{
		if (!TryParseWholeNumber(AmendedWithinCycles, 1, ProcedureAmendmentFilter.MaxWithinCycles, out int cycles))
		{
			return $"those amended in the last {AmendedWithinCycles.Trim()} cycles";
		}

		if (cycles == 1)
		{
			return SelectedCycle is { } cycle ? $"those amended in cycle {cycle.AiracCycleId}" : "those amended in the selected cycle";
		}

		string since = SelectedCycle is { } selected
			? $" (since cycle {AiracCycleResolver.GetCycleBefore(selected, cycles - 1).AiracCycleId})"
			: string.Empty;

		return $"those amended in the last {cycles} cycles{since}";
	}

	private void SetAmendmentFilter(DepartureAmendmentFilter filter)
	{
		if (_amendmentFilter == filter)
		{
			return;
		}

		_amendmentFilter = filter;

		if (filter != DepartureAmendmentFilter.None)
		{
			_lastAmendmentLimit = filter;
		}

		OnPropertyChanged(nameof(AmendmentAny));
		OnPropertyChanged(nameof(AmendmentLimited));
		OnPropertyChanged(nameof(AmendmentByCycles));
		OnPropertyChanged(nameof(AmendmentByDays));
		OnPropertyChanged(nameof(AmendmentByDate));
		MarkDirty();
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

	private void RaiseOwnSettingProperties()
	{
		foreach (string name in new[]
		{
			nameof(IncludeObstacleDepartures),
			nameof(RoiModeAirport), nameof(RoiModeWaypoint),
			nameof(AmendmentAny), nameof(AmendmentLimited), nameof(AmendmentByCycles), nameof(AmendmentByDays), nameof(AmendmentByDate),
			nameof(AmendedWithinCycles), nameof(AmendedWithinDays), nameof(AmendedOnOrAfter),
		})
		{
			OnPropertyChanged(name);
		}
	}
}
