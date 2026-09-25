using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Arrivals;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Arrivals</b> sub-service tab inside the AIRAC Service screen: which outputs to write,
/// which GeoJSON files, which procedures and ARTCCs, the optional region of interest, which
/// FE-Buddy properties and the CRC ERAM defaults.
/// </summary>
/// <remarks>
/// Save, Undo and navigation come from the tab host's action bar; the run is launched by
/// <b>Run AIRAC Service</b> on the Preview Settings tab, and its results are shown on the Review
/// tab, described by this tab through <see cref="ISubServiceRunTarget"/>.
/// </remarks>
public sealed class ArrivalsViewModel : GeojsonSubServiceViewModel, ISubServiceRunTarget
{
	private const string Node = "Services.AiracService.Arrivals";
	private const string CrcClassName = "Arrivals";
	private const string AmendmentDateFormat = "yyyy-MM-dd";
	private const int MaxAmendedWithinCycles = 1000;
	private const int MaxAmendedWithinDays = 36500;

	private bool _generateGeojson = true;
	private ArrivalRoiMode _roiMode = ArrivalRoiMode.Airport;
	private ArrivalAmendmentFilter _amendmentFilter = ArrivalAmendmentFilter.None;
	private string _amendedWithinCycles = "1";
	private string _amendedWithinDays = "30";
	private DateTime? _amendedOnOrAfter;
	private HashSet<string> _savedArtccFilter = new(StringComparer.OrdinalIgnoreCase);
	private bool _suppressArtccChanges;

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public ArrivalsViewModel()
	{
		FebProperties = FebPropertyToggle.ListFor(ArrivalFebPropertyOptions.All, MarkDirty);
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
	public override string Title => "Arrivals";

	// ================= outputs =================

	/// <summary>Whether this run writes GeoJSON for the arrival procedures.</summary>
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

	/// <inheritdoc />
	protected override int EnabledOutputCount =>
		(GenerateGeojson ? 1 : 0) + (GenerateAliasFile ? 1 : 0);

	/// <inheritdoc />
	protected override string NoDefaultRoiHint =>
		"No default ROI is set, so every arrival procedure is included. Set one in Settings, or override it here.";

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
			yield return OutputFileOption.AliasFile(ArrivalOutputFiles.Alias);
		}
	}

	private static OutputFileOption GeojsonFiles(CrcFeatureKind kind, EramFieldKind field)
	{
		string suffix = AiracOutputPaths.FileKindSuffix(kind);
		return new OutputFileOption(
			ArrivalOutputFiles.KeyFor(kind), "Every procedure", suffix, $"every procedure's {suffix} file",
			IsGeojson: true, [(CrcClassName, field)]);
	}

	// ================= procedures =================

	/// <summary>
	/// One toggle per ARTCC in the selected cycle's <c>STAR_BASE</c>. The selected set is the
	/// filter; none selected means every ARTCC. Empty until the cycle's data is loaded.
	/// </summary>
	public ObservableCollection<ArtccToggle> Artccs { get; } = [];

	/// <summary>Whether the ARTCC list has been built from the selected cycle yet.</summary>
	public bool HasArtccs => Artccs.Count > 0;

	/// <summary>Deselects every ARTCC, which means every ARTCC is included.</summary>
	public ICommand ClearArtccsCommand { get; }

	/// <summary>Whether procedures are kept whatever their amendment date.</summary>
	public bool AmendmentAny
	{
		get => _amendmentFilter == ArrivalAmendmentFilter.None;
		set { if (value) SetAmendmentFilter(ArrivalAmendmentFilter.None); }
	}

	/// <summary>Whether only procedures amended within the last <see cref="AmendedWithinCycles"/> cycles are kept.</summary>
	public bool AmendmentByCycles
	{
		get => _amendmentFilter == ArrivalAmendmentFilter.Cycles;
		set { if (value) SetAmendmentFilter(ArrivalAmendmentFilter.Cycles); }
	}

	/// <summary>Whether only procedures amended within the last <see cref="AmendedWithinDays"/> days are kept.</summary>
	public bool AmendmentByDays
	{
		get => _amendmentFilter == ArrivalAmendmentFilter.Days;
		set { if (value) SetAmendmentFilter(ArrivalAmendmentFilter.Days); }
	}

	/// <summary>Whether only procedures amended on or after <see cref="AmendedOnOrAfter"/> are kept.</summary>
	public bool AmendmentByDate
	{
		get => _amendmentFilter == ArrivalAmendmentFilter.Date;
		set { if (value) SetAmendmentFilter(ArrivalAmendmentFilter.Date); }
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

	/// <summary>Whether the ROI selects every arrival of an airport inside it.</summary>
	public bool RoiModeAirport
	{
		get => _roiMode == ArrivalRoiMode.Airport;
		set { if (value) SetRoiMode(ArrivalRoiMode.Airport); }
	}

	/// <summary>Whether the ROI selects any arrival with a point inside it.</summary>
	public bool RoiModeWaypoint
	{
		get => _roiMode == ArrivalRoiMode.Waypoint;
		set { if (value) SetRoiMode(ArrivalRoiMode.Waypoint); }
	}

	// ================= parent hooks =================

	/// <inheritdoc />
	/// <remarks>
	/// Builds the ARTCC toggle list from the cycle's <c>STAR_BASE</c>. <c>STAR_BASE.ARTCC</c> can
	/// list several centres space-separated (e.g. <c>"ZDC ZNY"</c>) when a STAR is shared between
	/// them, so each value is split on whitespace before the pieces are trimmed, upper-cased,
	/// deduplicated and sorted. A STAR shared by two ARTCCs still follows each airport's own ARTCC
	/// when filtering - this list only decides which toggles to offer.
	/// </remarks>
	public void LoadCycleDependentLists(NasrCsvDataCollection data)
	{
		_savedArtccFilter = ParseList(Get("ArtccFilter"));

		string[] artccs = [.. (data.Star?.StarBase ?? [])
			.SelectMany(s => (s.Artcc ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
			.Select(a => a.Trim().ToUpperInvariant())
			.Where(a => a.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(a => a, StringComparer.OrdinalIgnoreCase)];

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
		if (result.Arrivals is not { } arrivals)
		{
			return null;
		}

		string summary = $"{arrivals.AirportProcedureCount:N0} airport procedure(s), "
			+ $"{arrivals.GeojsonFilesWritten.Count:N0} GeoJSON file(s)";

		if (arrivals.SkippedForMissingPointsCount > 0)
		{
			summary += $", {arrivals.SkippedForMissingPointsCount:N0} skipped for missing points";
		}

		if (arrivals.AliasFilePath is not null)
		{
			summary += $", Arrivals.txt: {arrivals.AliasCommandCount:N0} alias command(s)";
		}

		return new SubServiceRunResult(Title, summary, arrivals.Messages);
	}

	/// <inheritdoc />
	public IReadOnlyDictionary<string, string> BuildSettingsBlock()
	{
		Dictionary<string, string> s = new(StringComparer.OrdinalIgnoreCase)
		{
			["GenerateGeojson"] = YesNo(GenerateGeojson),
			["ArtccFilter"] = string.Join(',', SelectedArtccs()),
			["AmendmentFilter"] = _amendmentFilter.ToString(),
			["RoiMode"] = _roiMode.ToString(),
		};

		// Only the active mode's value; the parser reads that key alone and ignores the others.
		switch (_amendmentFilter)
		{
			case ArrivalAmendmentFilter.Cycles:
				s["AmendedWithinCycles"] = AmendedWithinCycles.Trim();
				break;
			case ArrivalAmendmentFilter.Days:
				s["AmendedWithinDays"] = AmendedWithinDays.Trim();
				break;
			case ArrivalAmendmentFilter.Date when AmendedOnOrAfter is { } onOrAfter:
				s["AmendedOnOrAfter"] = onOrAfter.ToString(AmendmentDateFormat, CultureInfo.InvariantCulture);
				break;
		}

		AddSharedSettings(s);
		return s;
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		List<string> outputs = [];
		if (GenerateGeojson) outputs.Add("GeoJSON");
		if (GenerateAliasFile) outputs.Add("Alias file (Arrivals.txt)");

		List<string> geojsonFiles = [];
		if (EmitLines) geojsonFiles.Add("Lines");
		if (EmitSymbols) geojsonFiles.Add("Symbols");
		if (EmitText) geojsonFiles.Add("Text");

		// Before a cycle is parsed the toggle list is empty; SelectedArtccs falls back to what
		// is saved, so the preview does not claim "All" when a filter is on disk.
		string[] selectedArtccs = [.. SelectedArtccs()];

		ServicePreviewRow[] rows =
		[
			new ServicePreviewRow("Outputs", string.Join(", ", outputs)),
			new ServicePreviewRow("GeoJSON files", GenerateGeojson ? string.Join(", ", geojsonFiles) : "No"),
			new ServicePreviewRow("FE-Buddy properties", DescribeFebProperties()),
			new ServicePreviewRow("Includes", DescribeScope(selectedArtccs)),
			new ServicePreviewRow("Region of interest", DescribeRegion()),
			new ServicePreviewRow("Upload to vNAS", DescribeVnasFiles()),
			new ServicePreviewRow("CRC ERAM defaults", DescribeCrcDefaults()),
		];

		return [new ServicePreviewSection("Arrivals", rows)];
	}

	// ================= save contract =================

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		_generateGeojson = GetBool("GenerateGeojson", true);
		LoadSharedSettings();

		// Both outputs off would leave the tab in a state its own guard forbids; a hand-edited
		// config is the only way to get here, so fall back to the default rather than honour it.
		if (!_generateGeojson && !GenerateAliasFile)
		{
			_generateGeojson = true;
			GenerateAliasFile = true;
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

		_roiMode = string.Equals(Get("Roi.Mode")?.Trim(), nameof(ArrivalRoiMode.Waypoint), StringComparison.OrdinalIgnoreCase)
			? ArrivalRoiMode.Waypoint
			: ArrivalRoiMode.Airport;

		// Filter by name only; anything else (a number, a typo) falls back to no filter.
		string? savedFilter = Get("Amendment.Filter")?.Trim();
		_amendmentFilter = Enum.GetValues<ArrivalAmendmentFilter>()
			.FirstOrDefault(f => f.ToString().Equals(savedFilter, StringComparison.OrdinalIgnoreCase));
		_amendedWithinCycles = Get("Amendment.WithinCycles") ?? "1";
		_amendedWithinDays = Get("Amendment.WithinDays") ?? "30";
		_amendedOnOrAfter = DateTime.TryParseExact(
			Get("Amendment.OnOrAfter")?.Trim(), AmendmentDateFormat, CultureInfo.InvariantCulture,
			DateTimeStyles.None, out DateTime onOrAfter)
			? onOrAfter
			: null;

		RaiseOwnSettingProperties();
		ClearDirty();
	}

	/// <inheritdoc />
	protected override void WriteToConfig()
	{
		Set("GenerateGeojson", YesNo(GenerateGeojson));
		Set("ArtccFilter", string.Join(',', SelectedArtccs()));
		Set("Roi.Mode", _roiMode.ToString());
		Set("Amendment.Filter", _amendmentFilter.ToString());
		Set("Amendment.WithinCycles", AmendedWithinCycles);
		Set("Amendment.WithinDays", AmendedWithinDays);
		Set("Amendment.OnOrAfter", AmendedOnOrAfter?.ToString(AmendmentDateFormat, CultureInfo.InvariantCulture) ?? string.Empty);
		SaveSharedSettings();
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
			case ArrivalAmendmentFilter.Cycles:
				if (!TryParseWholeNumber(AmendedWithinCycles, 1, MaxAmendedWithinCycles, out _))
				{
					validation.AddField(nameof(AmendedWithinCycles),
						$"Enter a whole number of cycles from 1 to {MaxAmendedWithinCycles}.");
				}

				break;

			case ArrivalAmendmentFilter.Days:
				if (!TryParseWholeNumber(AmendedWithinDays, 1, MaxAmendedWithinDays, out _))
				{
					validation.AddField(nameof(AmendedWithinDays),
						$"Enter a whole number of days from 1 to {MaxAmendedWithinDays}.");
				}

				break;

			case ArrivalAmendmentFilter.Date:
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
		return _amendmentFilter switch
		{
			ArrivalAmendmentFilter.Cycles => TryParseWholeNumber(AmendedWithinCycles, 1, MaxAmendedWithinCycles, out int cycles) && cycles == 1
								? ", amended this cycle"
								: $", amended in the last {AmendedWithinCycles.Trim()} cycles",
			ArrivalAmendmentFilter.Days => TryParseWholeNumber(AmendedWithinDays, 1, MaxAmendedWithinDays, out int days) && days == 1
								? ", amended in the last day"
								: $", amended in the last {AmendedWithinDays.Trim()} days",
			ArrivalAmendmentFilter.Date => AmendedOnOrAfter is { } onOrAfter
								? $", amended on or after {onOrAfter.ToString(AmendmentDateFormat, CultureInfo.InvariantCulture)}"
								: ", amended on or after a date not yet picked",
			_ => string.Empty,
		};
	}

	private void SetAmendmentFilter(ArrivalAmendmentFilter filter)
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

	private void SetRoiMode(ArrivalRoiMode mode)
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
	/// ARTCCs and the region together - and which outputs that applies to.
	/// </summary>
	/// <param name="selectedArtccs">The ARTCCs ticked (or saved, before the cycle loads); empty for all.</param>
	/// <returns>e.g. "STARs, for ZOB and ZNY, at airports inside the region, amended this cycle. Applies to the GeoJSON files and the alias file."</returns>
	private string DescribeScope(string[] selectedArtccs)
	{
		string where = selectedArtccs.Length switch
		{
			0 => "for every ARTCC",
			1 => $"for {selectedArtccs[0]}",
			_ => $"for {string.Join(", ", selectedArtccs[..^1])} and {selectedArtccs[^1]}",
		};

		string region = !HasRoi
			? string.Empty
			: _roiMode == ArrivalRoiMode.Waypoint
				? ", with at least one point inside the region"
				: ", at airports inside the region";

		string outputs = (GenerateGeojson, GenerateAliasFile) switch
		{
			(true, true) => " Applies to the GeoJSON files and the alias file.",
			(true, false) => " Applies to the GeoJSON files.",
			(false, true) => " Applies to the alias file.",
			_ => string.Empty,
		};

		return $"STARs, {where}{region}{DescribeAmendmentFilter()}.{outputs}";
	}

	/// <summary>
	/// Only the geographic limit, plus how the region selects procedures. What the run covers
	/// overall - also narrowed by the ARTCC choice - is the "Includes" row's job
	/// (<see cref="DescribeScope"/>).
	/// </summary>
	/// <returns>The region in use and its mode, or that there is none.</returns>
	private string DescribeRegion()
	{
		if (!HasRoi)
		{
			return DescribeRoi();
		}

		string mode = _roiMode == ArrivalRoiMode.Waypoint ? "any point inside" : "airports inside";
		return $"{DescribeRoi()}; {mode}";
	}

	private void RaiseOwnSettingProperties()
	{
		foreach (string name in new[]
		{
			nameof(GenerateGeojson),
			nameof(RoiModeAirport), nameof(RoiModeWaypoint),
			nameof(AmendmentAny), nameof(AmendmentByCycles), nameof(AmendmentByDays), nameof(AmendmentByDate),
			nameof(AmendedWithinCycles), nameof(AmendedWithinDays), nameof(AmendedOnOrAfter),
		})
		{
			OnPropertyChanged(name);
		}
	}
}
