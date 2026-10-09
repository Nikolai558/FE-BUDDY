using System.IO;

using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.WxStations;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.WxStations;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Wx Stations</b> sub-service tab inside the AIRAC Service screen: which GeoJSON files to
/// write, the area (an ROI, or everything), and the CRC ERAM defaults for the merged Symbols and
/// Text files.
/// </summary>
/// <remarks>
/// The simplest GeoJSON sub-service tab: there is no file layout choice, no alias file and no
/// FE-Buddy properties - a station's label is always its ICAO ID, then its IATA ID and site name.
/// Like Telephony, its data does not come from the selected cycle's NASR data at all, but from
/// aviationweather.gov's station list, which every run downloads fresh into one kept copy - see
/// <see cref="RefreshStationData"/>. Save, Undo and navigation come from the
/// tab host's action bar; the run is launched by <b>Run AIRAC Service</b> on the Preview Settings
/// tab, and its results are shown on the Review tab, described by this tab through
/// <see cref="ISubServiceRunTarget"/>.
/// </remarks>
public sealed class WxStationsViewModel : GeojsonSubServiceViewModel, ISubServiceRunTarget
{
	private const string Node = "Services.AiracService.WxStations";

	private string _stationDataStatus = string.Empty;

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public WxStationsViewModel()
	{
		SymbolDefaults = [new(WxStationOutputFiles.AllClass, EramFieldKind.Symbol, MarkDirty)];
		TextDefaults = [new(WxStationOutputFiles.AllClass, EramFieldKind.Text, MarkDirty)];

		LoadFromConfig();
		RefreshStationData();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "Wx Stations";

	// ================= outputs =================

	/// <inheritdoc />
	protected override IReadOnlyList<SubServiceArea> Areas => SubServiceSettingsReader.RoiAreas;

	/// <inheritdoc />
	/// <remarks>Wx Stations has no Lines file: only Symbols and Text are ever written.</remarks>
	protected override (string? Lines, string? Symbols, string? Text) EmitKeys => (null, "EmitSymbols", "EmitText");

	/// <inheritdoc />
	/// <remarks>Wx Stations has no alias file.</remarks>
	protected override bool HasAliasFile => false;

	// ================= station data =================

	/// <summary>What the Station Data card and the Preview Settings tab say about FE-Buddy's kept copy of the station list.</summary>
	public string StationDataStatus => _stationDataStatus;

	/// <summary>
	/// Re-reads how old FE-Buddy's kept copy of the station list is
	/// (<see cref="WxStationFiles.SharedFilePath"/>). Called when the tab is built and by the AIRAC
	/// Service screen after every run, which is when the copy is replaced.
	/// </summary>
	/// <remarks>
	/// Never blocks the run: a missing copy is what the run downloads, and a failed download is
	/// reported by the run itself.
	/// </remarks>
	public void RefreshStationData()
	{
		_stationDataStatus = File.Exists(WxStationFiles.SharedFilePath)
			? $"FE-Buddy's copy is from {File.GetLastWriteTime(WxStationFiles.SharedFilePath):d MMM yyyy}. Every run downloads the latest list first, and uses this copy only if it can't."
			: "FE-Buddy has no copy yet. Every run downloads the latest list first, so the first run needs an internet connection.";

		OnPropertyChanged(nameof(StationDataStatus));
	}

	// ================= parent hooks =================

	/// <inheritdoc />
	/// <remarks>
	/// Does nothing: Wx Stations has no lists built from the selected cycle's NASR data. Its own
	/// data is downloaded by the run itself.
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
			summary += $", {wxStations.GeojsonStationCount:N0} inside the ROI";
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
			new ServicePreviewRow("Area", DescribeArea()),
			new ServicePreviewRow("CRC ERAM defaults", DescribeCrcDefaults()),
		];

		return [new ServicePreviewSection("Wx Stations", rows) { WhatYoullGet = WhatYoullGet }];
	}

	/// <inheritdoc />
	protected override IEnumerable<SummaryBlock> BuildWhatYoullGet()
	{
		yield return new SummaryBlock(SubServiceOutputKinds.Geojson, new SummaryLines()
			.Add(SummaryJoin.First, "Every US and US-territory station that reports METARs")
			.Add(SummaryJoin.AndOnly, RoiLine(roi => $"those inside {roi}"))
			.ToList());
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
			validation.AddArea(
				ServiceAreas.GeojsonFiles,
				"Neither Symbols nor Text is selected. Turn at least one back on, or untick Wx Stations under Include on the General tab.");
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
