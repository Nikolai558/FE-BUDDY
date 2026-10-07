using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Airways;
using FeBuddy.Core.Domain.Airways.Models;
using FeBuddy.Core.Domain.Crc.Models;
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
	private const string Node = "Services.AiracService.Airways";

	private static readonly Regex AirwayIdPattern = new(@"^Airway '([^']+)':", RegexOptions.Compiled);

	private static readonly AirwayAltitudeClass[] AltitudeClasses =
		[AirwayAltitudeClass.High, AirwayAltitudeClass.Low, AirwayAltitudeClass.Other];

	/// <summary>The three kinds of GeoJSON file, as Core names them and as the CRC ERAM panels group them.</summary>
	private static readonly (CrcFeatureKind Kind, EramFieldKind Field)[] FileKinds =
		[(CrcFeatureKind.Line, EramFieldKind.Line), (CrcFeatureKind.Symbol, EramFieldKind.Symbol), (CrcFeatureKind.Text, EramFieldKind.Text)];

	/// <summary>Each High/Low stratum and the key its designations are saved and sent under.</summary>
	private static readonly (AirwayStratum Stratum, string Key)[] StratumKeys =
	[
		(AirwayStratum.High, AirwaySettingsParser.HighDesignationsKey),
		(AirwayStratum.Low, AirwaySettingsParser.LowDesignationsKey),
		(AirwayStratum.Both, AirwaySettingsParser.BothDesignationsKey),
	];

	// Which file each designation goes in: what was saved, plus the user's choices since - kept for
	// designations the cycle does not have or the user has excluded, so they come back as they were.
	private readonly Dictionary<string, AirwayStratum> _strata = new(StringComparer.OrdinalIgnoreCase);

	private AirwayGeojsonOutputBy _outputBy = AirwayGeojsonOutputBy.HighLow;
	private bool _bufferAirwayWaypoints;
	private string _fixBufferNm = DefaultDistance(AirwayWaypointBuffer.DefaultFixRadiusNm);
	private string _navaidBufferNm = DefaultDistance(AirwayWaypointBuffer.DefaultNavaidRadiusNm);
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
	/// <remarks>Static, for the drop-down to bind with <c>x:Static</c> (see <see cref="StratumValues"/>).</remarks>
	public static IReadOnlyList<AirwayGeojsonOutputBy> OutputByValues { get; } =
		[AirwayGeojsonOutputBy.HighLow, AirwayGeojsonOutputBy.Designation];

	/// <summary>How airway GeoJSON is split into files. GeoJSON itself is turned on and off on the General tab.</summary>
	public AirwayGeojsonOutputBy OutputBy
	{
		get => _outputBy;
		set
		{
			if (SetProperty(ref _outputBy, value))
			{
				MarkDirty();
				OnPropertyChanged(nameof(OutputModeHint));
				OnPropertyChanged(nameof(ShowsStrata));
			}
		}
	}

	/// <summary>
	/// Whether the High and Low Files card shows: the tab writes High and Low files, and at least one
	/// designation is included to choose a file for.
	/// </summary>
	public bool ShowsStrata => GenerateGeojson && OutputBy == AirwayGeojsonOutputBy.HighLow && Designations.Any(d => d.Included);

	/// <summary>The files a designation can go in, for each row's drop-down on the High and Low Files card.</summary>
	/// <remarks>
	/// Static so the drop-downs bind it with <c>x:Static</c>: bound through the view's DataContext, it
	/// went null while the view was swapped out for another tab, and each ComboBox then cleared its
	/// selection back into the toggle - losing every saved choice.
	/// </remarks>
	public static IReadOnlyList<AirwayStratum> StratumValues { get; } = [AirwayStratum.High, AirwayStratum.Low, AirwayStratum.Both];

	/// <summary>A multi-line description of the files the selected <see cref="OutputBy"/> writes.</summary>
	public string OutputModeHint => OutputBy switch
	{
		AirwayGeojsonOutputBy.HighLow =>
			"Two file sets:\n" +
			"    • Airways_High\n" +
			"    • Airways_Low\n" +
			"Each airway type goes in either High or Low, as you choose.\n" +
			"Each set is _Lines + _Symbols + _Text.",
		AirwayGeojsonOutputBy.Designation =>
			"One file set per designation, derived from the AWY_ID prefix.\n" +
			"Ex: J / V / Q / T / AT:\n" +
			"    Airways_J, Airways_V, Airways_Q, …\n" +
			"Each set is _Lines + _Symbols + _Text.",
		_ => string.Empty,
	};

	/// <summary>Whether each line stops short of the waypoints at its ends, so it does not run through their symbols.</summary>
	public bool BufferAirwayWaypoints { get => _bufferAirwayWaypoints; set { if (SetProperty(ref _bufferAirwayWaypoints, value)) MarkDirty(); } }

	/// <summary>How far, in NM, a buffered line stops short of a 5-character fix, as typed. Saved.</summary>
	public string FixBufferNm { get => _fixBufferNm; set { if (SetProperty(ref _fixBufferNm, value)) MarkDirty(); } }

	/// <summary>How far, in NM, a buffered line stops short of a NAVAID (any other waypoint), as typed. Saved.</summary>
	public string NavaidBufferNm { get => _navaidBufferNm; set { if (SetProperty(ref _navaidBufferNm, value)) MarkDirty(); } }

	/// <summary>Which airways the alias file covers: <see langword="true"/> for ROI airways only, <see langword="false"/> for every FAA airway.</summary>
	public bool AliasRoiAirwaysOnly
	{
		get => _aliasRoiAirwaysOnly;
		set
		{
			if (SetProperty(ref _aliasRoiAirwaysOnly, value))
			{
				OnPropertyChanged(nameof(RoiOutputs));
				OnPropertyChanged(nameof(RoiAliasEffect));
				MarkDirty();
			}
		}
	}

	/// <summary>
	/// The outputs the region of interest narrows, for its card's tags: the GeoJSON always, and the
	/// alias file only with <see cref="AliasRoiAirwaysOnly"/>.
	/// </summary>
	public SubServiceOutputKinds RoiOutputs =>
		SubServiceOutputKinds.Geojson | (AliasRoiAirwaysOnly ? SubServiceOutputKinds.Alias : SubServiceOutputKinds.None);

	/// <summary>What the region of interest does to the alias file, for its card.</summary>
	public string RoiAliasEffect => AliasRoiAirwaysOnly
		? "Only the airways that cross the region, each with all of its waypoints."
		: "Every FAA airway. To narrow it to the region, choose ROI airways only on the Outputs card.";

	/// <summary>Whether a line that crosses 180 degrees longitude is split in two there.</summary>
	public bool SplitAtAntimeridian { get => _splitAtAntimeridian; set { if (SetProperty(ref _splitAtAntimeridian, value)) MarkDirty(); } }

	/// <summary>Designation include/exclude toggles, built from the selected cycle's parsed airways. Disabled until the data is ready.</summary>
	public ObservableCollection<DesignationToggle> Designations { get; } = [];

	/// <inheritdoc />
	protected override string NoRoiEffect =>
		"every airway is included";

	/// <inheritdoc />
	/// <remarks>
	/// A row per file group - High and Low (each only while an included designation goes in it), or
	/// each included designation - with its Lines, Symbols and Text files, then the alias file. A
	/// High or Low file uses its own class's CRC defaults for every airway in it; a designation file
	/// can hold all three classes.
	/// </remarks>
	protected override IEnumerable<OutputFileOption> OutputFiles()
	{
		if (GenerateGeojson)
		{
			bool[] emitted = [EmitLines, EmitSymbols, EmitText];

			foreach (string group in FileGroups())
			{
				for (int i = 0; i < FileKinds.Length; i++)
				{
					if (!emitted[i])
					{
						continue;
					}

					(CrcFeatureKind kind, EramFieldKind field) = FileKinds[i];
					string key = AirwayOutputFiles.GeojsonKey(group, kind);

					IReadOnlyList<(string, EramFieldKind)> crcRows = OutputBy == AirwayGeojsonOutputBy.HighLow
						? [(group, field)]
						: [.. AltitudeClasses.Select(altitudeClass => (altitudeClass.ToString(), field))];

					yield return new OutputFileOption(key, group, AiracOutputPaths.FileKindSuffix(kind), key, IsGeojson: true, crcRows);
				}
			}
		}

		if (GenerateAliasFile)
		{
			yield return OutputFileOption.AliasFile(AirwayOutputFiles.Alias);
		}
	}


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
			Designations.Add(new DesignationToggle(
				d, included: !excluded.Contains(d), _strata.TryGetValue(d, out AirwayStratum stratum) ? stratum : null, OnDesignationChanged));
		}

		OnPropertyChanged(nameof(ShowsStrata));

		// The files - and so the CRC ERAM Defaults card's rows - come from this list: in Designation mode one
		// set per designation, with High and Low files only the ones its designations go in.
		RefreshOutputFiles();
		Revalidate();

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
	public IReadOnlyDictionary<string, string> BuildSettingsBlock()
	{
		Dictionary<string, string> s = new(StringComparer.OrdinalIgnoreCase)
		{
			["GenerateGeojson"] = YesNo(GenerateGeojson),
			["OutputBy"] = OutputBy.ToString(),
			["BufferAirwayWaypoints"] = YesNo(BufferAirwayWaypoints),
			[AirwaySettingsParser.FixBufferKey] = FixBufferNm.Trim(),
			[AirwaySettingsParser.NavaidBufferKey] = NavaidBufferNm.Trim(),
			["AliasRoiScope"] = AliasRoiAirwaysOnly ? "RoiAirways" : "All",
			["SplitAtAntimeridian"] = YesNo(SplitAtAntimeridian),
			["ExcludedDesignations"] = ExcludedDesignationsValue(),
		};

		foreach ((AirwayStratum stratum, string key) in StratumKeys)
		{
			s[key] = DesignationsIn(stratum);
		}

		AddSharedSettings(s);
		return s;
	}

	// ================= save contract =================

	/// <inheritdoc />
	/// <remarks>The High and Low Files card shows only while GeoJSON is on.</remarks>
	protected override void OnOutputsChanged()
	{
		base.OnOutputsChanged();
		OnPropertyChanged(nameof(ShowsStrata));
	}

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		_outputBy = Enum.TryParse(Get("OutputBy"), true, out AirwayGeojsonOutputBy by) && Enum.IsDefined(by)
			? by
			: AirwayGeojsonOutputBy.HighLow;
		_bufferAirwayWaypoints = GetBool("BufferAirwayWaypoints", false);
		_fixBufferNm = Get(AirwaySettingsParser.FixBufferKey) ?? DefaultDistance(AirwayWaypointBuffer.DefaultFixRadiusNm);
		_navaidBufferNm = Get(AirwaySettingsParser.NavaidBufferKey) ?? DefaultDistance(AirwayWaypointBuffer.DefaultNavaidRadiusNm);
		_aliasRoiAirwaysOnly = string.Equals(Get("AliasRoiScope"), "RoiAirways", StringComparison.OrdinalIgnoreCase);
		_splitAtAntimeridian = GetBool("SplitAtAntimeridian", true);
		LoadSharedSettings();
		LoadStrata();

		// Re-apply the excluded set and the strata to any already-built designation toggles.
		HashSet<string> excluded = ParseExcludedFromConfig();
		foreach (DesignationToggle toggle in Designations)
		{
			toggle.Included = !excluded.Contains(toggle.Designation);
			toggle.Stratum = _strata.TryGetValue(toggle.Designation, out AirwayStratum stratum) ? stratum : null;
		}

		foreach (string name in new[]
		{
			nameof(OutputBy), nameof(OutputModeHint), nameof(ShowsStrata),
			nameof(BufferAirwayWaypoints), nameof(FixBufferNm), nameof(NavaidBufferNm),
			nameof(AliasRoiAirwaysOnly), nameof(RoiOutputs), nameof(RoiAliasEffect), nameof(SplitAtAntimeridian),
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
		Set(AirwaySettingsParser.FixBufferKey, FixBufferNm.Trim());
		Set(AirwaySettingsParser.NavaidBufferKey, NavaidBufferNm.Trim());
		Set("AliasRoiScope", AliasRoiAirwaysOnly ? "RoiAirways" : "All");
		Set("SplitAtAntimeridian", YesNo(SplitAtAntimeridian));
		Set("ExcludedDesignations", ExcludedDesignationsValue());

		foreach ((AirwayStratum stratum, string key) in StratumKeys)
		{
			Set(key, DesignationsIn(stratum));
		}

		SaveSharedSettings();
	}

	/// <inheritdoc />
	/// <remarks>
	/// With High and Low files, every included designation needs a file: one with none yet - any
	/// but J, Q, V and T the first time, or one a new cycle adds - is flagged until the user picks.
	/// </remarks>
	protected override void Validate(ServiceValidation validation)
	{
		if (GenerateGeojson && !EmitLines && !EmitSymbols && !EmitText)
		{
			validation.AddArea(
				ServiceAreas.GeojsonFiles,
				"GeoJSON is on but none of its files are selected. Turn on Lines, Symbols or Text, or turn GeoJSON off for Airways on the General tab.");
		}

		bool needsStrata = GenerateGeojson && OutputBy == AirwayGeojsonOutputBy.HighLow;

		foreach (DesignationToggle toggle in Designations)
		{
			toggle.StratumError = needsStrata && toggle.Included && toggle.Stratum is null ? "Choose High, Low or Both." : null;
		}

		// The distances only matter - and only show - while buffered GeoJSON is written.
		if (GenerateGeojson && BufferAirwayWaypoints)
		{
			ValidateBufferDistance(validation, nameof(FixBufferNm), FixBufferNm, "fixes");
			ValidateBufferDistance(validation, nameof(NavaidBufferNm), NavaidBufferNm, "NAVAIDs");
		}

		string[] unchosen = [.. Designations.Where(d => d.StratumError is not null).Select(d => d.Designation)];

		if (unchosen.Length > 0)
		{
			validation.AddArea(
				ServiceAreas.HighAndLowFiles,
				$"Choose High, Low or Both for {string.Join(", ", unchosen)} on the High and Low Files card, " +
				"or untick them under Designations to Include.");
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

		List<ServicePreviewRow> rows =
		[
			new ServicePreviewRow("GeoJSON output", GenerateGeojson ? OutputBy.ToString() : "No"),
			new ServicePreviewRow("File kinds", fileKinds.Count > 0 ? string.Join(", ", fileKinds) : "none"),
			new ServicePreviewRow("FE-Buddy properties", DescribeFebProperties()),
			new ServicePreviewRow("Buffer waypoints", BufferAirwayWaypoints
				? $"{FixBufferNm.Trim()} NM around fixes, {NavaidBufferNm.Trim()} NM around NAVAIDs"
				: "No"),
			new ServicePreviewRow("Split at antimeridian", SplitAtAntimeridian ? "Yes" : "No"),
			new ServicePreviewRow("Region of interest", DescribeRoi()),
			new ServicePreviewRow("CRC ERAM defaults", DescribeCrcDefaults()),
		];

		if (GenerateGeojson && OutputBy == AirwayGeojsonOutputBy.HighLow)
		{
			rows.Insert(1, new ServicePreviewRow("High and Low files", DescribeStrata()));
		}

		return [new ServicePreviewSection("Airways", rows) { WhatYoullGet = WhatYoullGet }];
	}

	/// <inheritdoc />
	/// <remarks>
	/// The designations narrow both files; the region narrows the GeoJSON, and the alias file only
	/// with ROI airways only. With no region the two blocks are the same, and merge.
	/// </remarks>
	protected override IEnumerable<SummaryBlock> BuildWhatYoullGet()
	{
		// Before a cycle is parsed the toggle list is empty: the saved exclusions stand in.
		string[] excluded = Designations.Count > 0
			? [.. Designations.Where(d => !d.Included).Select(d => d.Designation)]
			: [.. ParseExcludedFromConfig().OrderBy(d => d, StringComparer.OrdinalIgnoreCase)];

		string airways = excluded.Length == 0
			? "every airway"
			: $"every airway except the {SummaryLines.Join(excluded, "and")} airways";

		bool aliasUsesRegion = AliasRoiAirwaysOnly && HasRoi;

		yield return new SummaryBlock(SubServiceOutputKinds.Geojson, new SummaryLines()
			.Add(SummaryJoin.First, airways)
			.Add(SummaryJoin.And, RegionLine("that cross the region, cut off at its edge"))
			.ToList());

		yield return new SummaryBlock(SubServiceOutputKinds.Alias, new SummaryLines()
			.Add(SummaryJoin.First, aliasUsesRegion || !HasRoi ? airways : $"{airways}, whatever the region")
			.Add(SummaryJoin.And, aliasUsesRegion ? RegionLine("that cross the region, each with all of its waypoints") : null)
			.ToList());
	}

	// ================= helpers =================

	private HashSet<string> ParseExcludedFromConfig() => ParseList(Get("ExcludedDesignations"));

	/// <summary>A default buffer distance as the box shows it: <c>2.5</c>, <c>5</c>.</summary>
	private static string DefaultDistance(double nm) => nm.ToString(CultureInfo.InvariantCulture);

	/// <summary>Marks a buffer distance that is not a number from 0 to <see cref="AirwayWaypointBuffer.MaxRadiusNm"/>.</summary>
	private static void ValidateBufferDistance(ServiceValidation validation, string field, string value, string around)
	{
		if (!double.TryParse(value.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double nm)
			|| !(nm >= 0 && nm <= AirwayWaypointBuffer.MaxRadiusNm))
		{
			validation.AddField(field,
				$"Buffer Airway Waypoints: enter a distance around {around} from 0 to {AirwayWaypointBuffer.MaxRadiusNm:0} NM.");
		}
	}

	/// <summary>
	/// The excluded designations as saved and sent: from the toggles, or - before the cycle's list
	/// is built - the saved ones, so a save in the meantime keeps them instead of clearing them.
	/// </summary>
	/// <returns>The comma-separated designations.</returns>
	private string ExcludedDesignationsValue() =>
		Designations.Count > 0
			? string.Join(',', Designations.Where(d => !d.Included).Select(d => d.Designation))
			: string.Join(',', ParseExcludedFromConfig().Order(StringComparer.OrdinalIgnoreCase));

	/// <summary>
	/// The groups the GeoJSON is split into: High and Low - each only while an included designation
	/// goes in it - or each included designation.
	/// </summary>
	/// <returns>The group names, in display order.</returns>
	private IEnumerable<string> FileGroups()
	{
		if (OutputBy == AirwayGeojsonOutputBy.HighLow)
		{
			// Before the cycle's designations load, the saved choices say which files there will be.
			HashSet<string> excludedDesignations = ParseExcludedFromConfig();
			AirwayStratum[] used = Designations.Count > 0
				? [.. Designations.Where(d => d.Included && d.Stratum is not null).Select(d => d.Stratum!.Value)]
				: [.. _strata.Where(pair => !excludedDesignations.Contains(pair.Key)).Select(pair => pair.Value)];

			List<string> groups = [];

			if (used.Any(stratum => stratum is AirwayStratum.High or AirwayStratum.Both))
			{
				groups.Add(nameof(AirwayStratum.High));
			}

			if (used.Any(stratum => stratum is AirwayStratum.Low or AirwayStratum.Both))
			{
				groups.Add(nameof(AirwayStratum.Low));
			}

			return groups;
		}

		if (Designations.Count > 0)
		{
			return Designations.Where(d => d.Included).Select(d => d.Designation);
		}

		// Before the cycle's designations load there is no list to offer, so keep the groups the
		// saved choices name - a save or a run in the meantime must not drop them.
		HashSet<string> excluded = ParseExcludedFromConfig();

		return ChosenCrcFileKeys
			.Where(AirwayOutputFiles.IsGeojsonKey)
			.Select(key => key.Split('_')[1])
			.Where(group => !excluded.Contains(group))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Order(StringComparer.OrdinalIgnoreCase);
	}

	private ObservableCollection<EramClassDefault> BuildClassDefaults(EramFieldKind kind) =>
		[.. AltitudeClasses.Select(altitudeClass => new EramClassDefault(altitudeClass.ToString(), kind, MarkDirty))];

	/// <summary>
	/// Reads which file each designation goes in. With none of the three keys saved - a first run -
	/// J and Q go High and V and T Low, and every other designation waits for the user to choose.
	/// </summary>
	private void LoadStrata()
	{
		_strata.Clear();

		if (StratumKeys.All(entry => Get(entry.Key) is null))
		{
			foreach ((string designation, AirwayStratum stratum) in AirwaySettings.DefaultDesignationStrata)
			{
				_strata[designation] = stratum;
			}

			return;
		}

		foreach ((AirwayStratum stratum, string key) in StratumKeys)
		{
			foreach (string designation in ParseList(Get(key)))
			{
				_strata[designation.ToUpperInvariant()] = stratum;
			}
		}
	}

	/// <summary>The designations that go in one stratum, as saved and sent: every choice, excluded designations' too.</summary>
	/// <param name="stratum">The stratum.</param>
	/// <returns>The comma-separated designations, in name order.</returns>
	private string DesignationsIn(AirwayStratum stratum) =>
		string.Join(',', _strata.Where(pair => pair.Value == stratum).Select(pair => pair.Key).Order(StringComparer.OrdinalIgnoreCase));

	/// <summary>The included designations by file, for the Preview Settings tab.</summary>
	/// <returns>e.g. <c>High: J, Q · Low: V, T · Both: Y</c>, naming any still without a file.</returns>
	private string DescribeStrata()
	{
		if (Designations.Count == 0)
		{
			return "Waiting for the cycle's airway list";
		}

		DesignationToggle[] included = [.. Designations.Where(d => d.Included)];

		List<string> parts =
		[
			.. StratumValues
				.Select(stratum => (Stratum: stratum, Designations: included.Where(d => d.Stratum == stratum).Select(d => d.Designation).ToArray()))
				.Where(entry => entry.Designations.Length > 0)
				.Select(entry => $"{entry.Stratum}: {string.Join(", ", entry.Designations)}"),
		];

		string[] unchosen = [.. included.Where(d => d.Stratum is null).Select(d => d.Designation)];

		if (unchosen.Length > 0)
		{
			parts.Add($"not chosen yet: {string.Join(", ", unchosen)}");
		}

		return parts.Count > 0 ? string.Join(" · ", parts) : "No airways";
	}

	/// <summary>A designation was included, excluded, or given a file: keep the choice, and re-check the tab.</summary>
	private void OnDesignationChanged(DesignationToggle toggle)
	{
		if (toggle.Stratum is { } stratum)
		{
			_strata[toggle.Designation] = stratum;
		}

		OnPropertyChanged(nameof(ShowsStrata));
		MarkDirty();
	}
}
