using System.IO;

using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.WxStations;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Wx Stations</b> sub-service tab inside the AIRAC Service screen: which outputs to
/// write, the optional region of interest, and the CRC ERAM defaults for the merged Symbols and
/// Text files.
/// </summary>
/// <remarks>
/// The simplest GeoJSON sub-service tab: there is no file layout choice, no alias file and no
/// FE-Buddy properties - a station's label is always its ICAO ID, then its IATA ID and site name.
/// Unlike every other AIRAC sub-service, its data does not come from the selected cycle's NASR
/// data at all, but from a separately cached <c>stations.cache.xml</c> that may not have
/// downloaded yet - see <see cref="SetStationData"/>. Save, Undo and navigation come from the tab
/// host's action bar; the run is launched by <b>Run AIRAC Service</b> on the Preview Settings tab,
/// and its results are shown on the Review tab, described by this tab through
/// <see cref="ISubServiceRunTarget"/>.
/// </remarks>
public sealed class WxStationsViewModel : GeojsonSubServiceViewModel, ISubServiceRunTarget
{
	private const string Node = "Services.AiracService.WxStations";

	private const string StationDataUnknownMessage =
		"Station data appears once the selected cycle's data is loaded.";

	private string? _stationDataCycleId;
	private bool _hasStationData;
	private string _stationDataStatus = StationDataUnknownMessage;

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public WxStationsViewModel()
	{
		SymbolDefaults = [new(WxStationOutputFiles.AllClass, EramFieldKind.Symbol, MarkDirty)];
		TextDefaults = [new(WxStationOutputFiles.AllClass, EramFieldKind.Text, MarkDirty)];

		LoadFromConfig();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "Wx Stations";

	// ================= outputs =================

	/// <inheritdoc />
	protected override int EnabledOutputCount => 1;

	/// <inheritdoc />
	protected override string NoDefaultRoiHint =>
		"No default ROI is set, so the GeoJSON covers every station. Set one in Settings, or override it here.";

	/// <inheritdoc />
	/// <remarks>Wx Stations has no Lines file: only Symbols and Text are ever written.</remarks>
	protected override (string? Lines, string? Symbols, string? Text) EmitKeys => (null, "EmitSymbols", "EmitText");

	/// <inheritdoc />
	/// <remarks>Wx Stations has no alias file.</remarks>
	protected override bool HasAliasFile => false;

	// ================= station data =================

	/// <summary>
	/// Whether <see cref="SetStationData"/> has been called yet for the currently selected cycle -
	/// that is, whether the selected cycle's data has finished loading.
	/// <see langword="false"/> right after the tab is built, until the parent reports in.
	/// </summary>
	public bool IsStationDataKnown => _stationDataCycleId is not null;

	/// <summary>
	/// Whether the selected cycle has a downloaded <c>stations.cache.xml</c>. Only meaningful once
	/// <see cref="IsStationDataKnown"/> is <see langword="true"/>.
	/// </summary>
	public bool HasStationData => _hasStationData;

	/// <summary>What the Station Data card and the Preview Settings tab say about the data's availability.</summary>
	public string StationDataStatus => _stationDataStatus;

	/// <summary>
	/// Called by the AIRAC Service screen whenever the selected cycle's data is (re)loaded, to
	/// report whether that cycle's Wx Stations data has downloaded.
	/// </summary>
	/// <param name="cycleId">The selected cycle's ID.</param>
	/// <param name="filePath">
	/// The cycle's <c>stations.cache.xml</c> path (see <c>AiracCycleDataCache.FindWxStationsFile</c>),
	/// or <see langword="null"/> when it has not downloaded yet.
	/// </param>
	/// <remarks>
	/// Station data is not a saved setting, so this never marks the tab dirty - it only
	/// re-validates, which is also what lets a missing file block the run (see
	/// <see cref="Validate"/>).
	/// </remarks>
	public void SetStationData(string cycleId, string? filePath)
	{
		ArgumentNullException.ThrowIfNull(cycleId);

		_stationDataCycleId = cycleId;
		_hasStationData = filePath is not null;

		_stationDataStatus = filePath is not null
			? $"Station data for cycle {cycleId}: downloaded {File.GetLastWriteTime(filePath):d MMM yyyy}."
			: $"Station data for cycle {cycleId} isn't downloaded yet. FE-Buddy fetches it at launch: restart FE-Buddy with an internet connection, or deselect Wx Stations on the General tab.";

		OnPropertyChanged(nameof(IsStationDataKnown));
		OnPropertyChanged(nameof(HasStationData));
		OnPropertyChanged(nameof(StationDataStatus));
		Revalidate();
	}

	// ================= parent hooks =================

	/// <inheritdoc />
	/// <remarks>
	/// Does nothing: unlike every other AIRAC sub-service, Wx Stations has no lists built from the
	/// selected cycle's NASR data. Its own data's readiness is reported through
	/// <see cref="SetStationData"/> instead, on its own schedule.
	/// </remarks>
	public void LoadCycleDependentLists(NasrCsvDataCollection data)
	{
	}

	/// <inheritdoc />
	public SubServiceRunResult? DescribeRunResult(AiracServiceResult result)
	{
		if (result.WxStations is not { } wxStations)
		{
			return null;
		}

		string summary = $"{wxStations.StationCount:N0} station(s)";

		if (HasRoi)
		{
			summary += $", {wxStations.GeojsonStationCount:N0} in the region";
		}

		summary += $", {wxStations.GeojsonFilesWritten.Count:N0} GeoJSON file(s)";

		return new SubServiceRunResult(Title, summary, wxStations.Messages);
	}

	/// <inheritdoc />
	/// <remarks>The block goes on <see cref="AiracServiceSettings.WxStations"/>.</remarks>
	public IReadOnlyDictionary<string, string> BuildSettingsBlock()
	{
		Dictionary<string, string> s = new(StringComparer.OrdinalIgnoreCase);
		AddSharedSettings(s);
		return s;
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		ServicePreviewRow[] rows =
		[
			new ServicePreviewRow("GeoJSON files", DescribeGeojsonFiles()),
			new ServicePreviewRow("Station data", StationDataStatus),
			new ServicePreviewRow("Region of interest", DescribeRoi()),
			new ServicePreviewRow("Upload to vNAS", DescribeVnasFiles()),
			new ServicePreviewRow("CRC ERAM defaults", DescribeCrcDefaults()),
		];

		return [new ServicePreviewSection("Wx Stations", rows)];
	}

	// ================= save contract =================

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		LoadSharedSettings();
		ClearDirty();
	}

	/// <inheritdoc />
	protected override void WriteToConfig() => SaveSharedSettings();

	/// <inheritdoc />
	protected override void Validate(ServiceValidation validation)
	{
		if (!EmitSymbols && !EmitText)
		{
			validation.Add("Neither Symbols nor Text is selected. Turn at least one back on, or deselect Wx Stations on the General tab.");
		}

		// Blocks the run rather than letting it fail on its last step, once the cycle's data is
		// known to be missing.
		if (IsStationDataKnown && !HasStationData)
		{
			validation.Add(StationDataStatus);
		}

		ValidateSharedSettings(validation);
	}

	/// <inheritdoc />
	/// <remarks>Always one merged Symbols file and one merged Text file; there is no alias file.</remarks>
	protected override IEnumerable<OutputFileOption> OutputFiles()
	{
		if (EmitSymbols)
		{
			yield return new OutputFileOption(WxStationOutputFiles.Symbols, "Every station", "Symbols",
				"the Wx Stations Symbols file", IsGeojson: true, [(WxStationOutputFiles.AllClass, EramFieldKind.Symbol)]);
		}

		if (EmitText)
		{
			yield return new OutputFileOption(WxStationOutputFiles.Text, "Every station", "Text",
				"the Wx Stations Text file", IsGeojson: true, [(WxStationOutputFiles.AllClass, EramFieldKind.Text)]);
		}
	}

	// ================= helpers =================

	private string DescribeGeojsonFiles()
	{
		List<string> kinds = [];
		if (EmitSymbols) kinds.Add("Symbols");
		if (EmitText) kinds.Add("Text");

		return kinds.Count > 0 ? string.Join(", ", kinds) : "None";
	}
}
