using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels.Models;

using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Airways;
using FeBuddy.Core.Domain.Airways.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Airways</b> sub-service tab inside the AIRAC Service screen: its settings menu, with
/// the shared Save / Undo contract. Save, Undo and navigation come from the tab host's action
/// bar; the run is launched by <b>Run AIRAC Service</b> on the Preview Settings tab, and its
/// results are shown on the Review tab, described by this tab through <see cref="ISubServiceRunTarget"/>.
/// </summary>
public sealed class AirwaysViewModel : GeojsonSubServiceViewModel, ISubServiceRunTarget
{
	private const string Node = "Services.AiracService.Geojson.Airways";

	private static readonly Regex AirwayIdPattern = new(@"^Airway '([^']+)':", RegexOptions.Compiled);

	private AirwayGeojsonOutputBy _outputBy = AirwayGeojsonOutputBy.HighLow;
	private bool _bufferAirwayWaypoints;
	private bool _aliasRoiAirwaysOnly;
	private bool _splitAtAntimeridian = true;

	/// <summary>Builds the tab and loads its saved settings.</summary>
	public AirwaysViewModel()
	{
		FebProperties = FebPropertyToggle.ListFor(AirwayFebPropertyOptions.All, MarkDirty);
		LineDefaults = BuildClassDefaults(EramFieldKind.Line);
		SymbolDefaults = BuildClassDefaults(EramFieldKind.Symbol);
		TextDefaults = BuildClassDefaults(EramFieldKind.Text);

		LoadFromConfig();
	}

	// ================= settings menu =================

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "Airways";

	/// <summary>The choices for <see cref="OutputBy"/>, in the order the menu shows them.</summary>
	public IReadOnlyList<AirwayGeojsonOutputBy> OutputByValues { get; } =
		[AirwayGeojsonOutputBy.HighLow, AirwayGeojsonOutputBy.Designation, AirwayGeojsonOutputBy.None];

	/// <summary>How airway GeoJSON is split into files, or <c>None</c> for no GeoJSON.</summary>
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

	/// <summary>A multi-line description of the files the selected <see cref="OutputBy"/> writes.</summary>
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

	/// <summary>Whether each line stops short of the waypoints at its ends, so it does not run through their symbols.</summary>
	public bool BufferAirwayWaypoints { get => _bufferAirwayWaypoints; set { if (SetProperty(ref _bufferAirwayWaypoints, value)) MarkDirty(); } }

	/// <summary>Which airways the alias file covers: <see langword="true"/> for ROI airways only, <see langword="false"/> for every FAA airway.</summary>
	public bool AliasRoiAirwaysOnly { get => _aliasRoiAirwaysOnly; set { if (SetProperty(ref _aliasRoiAirwaysOnly, value)) MarkDirty(); } }

	/// <summary>Whether a line that crosses 180 degrees longitude is split in two there.</summary>
	public bool SplitAtAntimeridian { get => _splitAtAntimeridian; set { if (SetProperty(ref _splitAtAntimeridian, value)) MarkDirty(); } }

	/// <summary>Designation include/exclude toggles, built from the selected cycle's parsed airways. Disabled until the data is ready.</summary>
	public ObservableCollection<DesignationToggle> Designations { get; } = [];

	/// <inheritdoc />
	protected override int EnabledOutputCount =>
		(OutputBy == AirwayGeojsonOutputBy.None ? 0 : 1) + (GenerateAliasFile ? 1 : 0);

	/// <inheritdoc />
	protected override bool WritesGeojson => IsGeojsonOutputOn;

	/// <inheritdoc />
	protected override string NoDefaultRoiHint =>
		"No default ROI is set, so every airway is included. Set one in Settings, or override it here.";

	/// <inheritdoc />
	/// <remarks>Airways keeps one row per altitude class, grouped by kind: <c>CrcEramPropertyDefaults.Lines.Airway_High_Lines</c>.</remarks>
	protected override string CrcConfigPrefix(EramClassDefault row) =>
		$"CrcEramPropertyDefaults.{row.Kind}s.Airway_{row.ClassName}_{row.Kind}s";

	// ================= parent hooks =================

	/// <inheritdoc />
	/// <remarks>Builds the designation toggle list from the cycle's airways.</remarks>
	public void LoadCycleDependentLists(NasrCsvDataCollection data)
	{
		HashSet<string> excluded = ParseExcludedFromConfig();

		string[] designations = [.. (data.Awy?.AwyBase ?? [])
			.Select(a => AirwayClassifier.DeriveDesignation(a.AwyId))
			.Where(d => !string.IsNullOrWhiteSpace(d))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(d => d, StringComparer.OrdinalIgnoreCase)];

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

		if (airways.ExcludedAirwayIds.Count > 0)
		{
			summary += $", {airways.ExcludedAirwayIds.Count:N0} excluded";
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

	/// <inheritdoc />
	/// <remarks>The block goes on <see cref="AiracServiceSettings.Airways"/>.</remarks>
	public IReadOnlyDictionary<string, string> BuildSettingsBlock(string outputDirectory, bool addFeBuddyOutputFolder)
	{
		Dictionary<string, string> s = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputBy"] = OutputBy.ToString(),
			["BufferAirwayWaypoints"] = YesNo(BufferAirwayWaypoints),
			["AliasRoiScope"] = AliasRoiAirwaysOnly ? "RoiAirways" : "All",
			["SplitAtAntimeridian"] = YesNo(SplitAtAntimeridian),
			["ExcludedDesignations"] = string.Join(',', Designations.Where(d => !d.Included).Select(d => d.Designation)),
		};

		AddSharedSettings(s, outputDirectory, addFeBuddyOutputFolder);
		return s;
	}

	// ================= save contract =================

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		_outputBy = Enum.TryParse(Get("OutputBy"), true, out AirwayGeojsonOutputBy by) ? by : AirwayGeojsonOutputBy.HighLow;
		_bufferAirwayWaypoints = GetBool("BufferAirwayWaypoints", false);
		_aliasRoiAirwaysOnly = string.Equals(Get("AliasRoiScope"), "RoiAirways", StringComparison.OrdinalIgnoreCase);
		_splitAtAntimeridian = GetBool("SplitAtAntimeridian", true);
		LoadSharedSettings();

		// Re-apply the excluded set to any already-built designation toggles.
		HashSet<string> excluded = ParseExcludedFromConfig();
		foreach (DesignationToggle toggle in Designations)
		{
			toggle.Included = !excluded.Contains(toggle.Designation);
		}

		foreach (string name in new[]
		{
			nameof(OutputBy), nameof(OutputModeHint), nameof(IsGeojsonOutputOn),
			nameof(BufferAirwayWaypoints), nameof(AliasRoiAirwaysOnly), nameof(SplitAtAntimeridian),
		})
		{
			OnPropertyChanged(name);
		}

		ClearDirty();
	}

	/// <inheritdoc />
	protected override void WriteToConfig()
	{
		Set("OutputBy", OutputBy.ToString());
		Set("BufferAirwayWaypoints", YesNo(BufferAirwayWaypoints));
		Set("AliasRoiScope", AliasRoiAirwaysOnly ? "RoiAirways" : "All");
		Set("SplitAtAntimeridian", YesNo(SplitAtAntimeridian));
		Set("ExcludedDesignations", string.Join(',', Designations.Where(d => !d.Included).Select(d => d.Designation)));
		SaveSharedSettings();
	}

	/// <inheritdoc />
	protected override void Validate(ServiceValidation validation)
	{
		if (IsGeojsonOutputOn && !EmitLines && !EmitSymbols && !EmitText)
		{
			validation.Add("Lines, Symbols and Text are all off, but Output is not \"None\". Turn at least one back on, or set Output to \"None\".");
		}

		ValidateSharedSettings(validation);
	}

	// ================= preview =================

	/// <summary>
	/// This tab's contribution to the Preview Settings tab: one section spelling out, in plain
	/// words, what the current settings will actually produce, so the user can check the run
	/// without walking back through every control.
	/// </summary>
	/// <returns>A single <b>Airways</b> section, its rows in display order.</returns>
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		List<string> fileKinds = [];
		if (EmitLines) fileKinds.Add("Lines");
		if (EmitSymbols) fileKinds.Add("Symbols");
		if (EmitText) fileKinds.Add("Text");

		string aliasFile = GenerateAliasFile
			? AliasRoiAirwaysOnly ? "Airways.txt, ROI airways only" : "Airways.txt, all FAA airways"
			: "No";

		// Before a cycle is parsed the toggle list is empty, which would make the preview claim
		// "none excluded" when the user has exclusions saved. Fall back to what is on disk.
		string excluded = Designations.Count > 0
			? string.Join(", ", Designations.Where(d => !d.Included).Select(d => d.Designation))
			: string.Join(", ", ParseExcludedFromConfig().OrderBy(d => d, StringComparer.OrdinalIgnoreCase));

		// Name what is covered - never "all except"; the exclusions have their own row. The
		// designations come from the parsed cycle, so until it is loaded there is nothing to name.
		string[] included = [.. Designations.Where(d => d.Included).Select(d => d.Designation)];
		string covered = Designations.Count == 0
			? "Waiting for the cycle's airway list"
			: included.Length > 0 ? $"{string.Join(", ", included)} airways" : "No airways";

		string includes = covered
			+ (HasRoi ? ". GeoJSON: only the airways crossing the region, clipped to it." : ".");

		ServicePreviewRow[] rows =
		[
			new ServicePreviewRow("GeoJSON output", OutputBy.ToString()),
			new ServicePreviewRow("Alias file", aliasFile),
			new ServicePreviewRow("File kinds", fileKinds.Count > 0 ? string.Join(", ", fileKinds) : "none"),
			new ServicePreviewRow("FE-Buddy properties", DescribeFebProperties()),
			new ServicePreviewRow("CRC ERAM defaults", DescribeCrcDefaults()),
			new ServicePreviewRow("Includes", includes),
			new ServicePreviewRow("Excluded designations", string.IsNullOrEmpty(excluded) ? "none" : excluded),
			new ServicePreviewRow("Buffer waypoints", BufferAirwayWaypoints ? "Yes" : "No"),
			new ServicePreviewRow("Split at antimeridian", SplitAtAntimeridian ? "Yes" : "No"),
			new ServicePreviewRow("Region of interest", DescribeRoi()),
		];

		return [new ServicePreviewSection("Airways", rows)];
	}

	// ================= helpers =================

	private HashSet<string> ParseExcludedFromConfig() => ParseList(Get("ExcludedDesignations"));

	private ObservableCollection<EramClassDefault> BuildClassDefaults(EramFieldKind kind) =>
		[.. new[] { AirwayAltitudeClass.High, AirwayAltitudeClass.Low, AirwayAltitudeClass.Other }
			.Select(altitudeClass => new EramClassDefault(altitudeClass.ToString(), kind, MarkDirty))];
}
