using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac.Airports;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Airports</b> sub-service tab inside the AIRAC Service screen: which outputs are on (set
/// on the General tab), which GeoJSON files, which FE-Buddy properties, the CRC ERAM defaults, and
/// the ROI override.
/// </summary>
/// <remarks>
/// Save, Undo and navigation come from the tab host's action bar; the run is launched by
/// <b>Run AIRAC Service</b> on the Preview Settings tab, and its results are shown on the Review
/// tab, described by this tab through <see cref="ISubServiceRunTarget"/>.
/// </remarks>
public sealed class AirportsViewModel : GeojsonSubServiceViewModel, ISubServiceRunTarget
{
	private const string Node = "Services.AiracService.Airports";

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public AirportsViewModel()
	{
		FebProperties = FebPropertyToggle.ListFor(AirportFebPropertyOptions.All, MarkDirty);
		LineDefaults = [new("Runways", EramFieldKind.Line, MarkDirty)];
		SymbolDefaults = [new("Airports", EramFieldKind.Symbol, MarkDirty)];
		TextDefaults = [new("Airports", EramFieldKind.Text, MarkDirty)];

		LoadFromConfig();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "Airports";

	/// <inheritdoc />
	protected override IReadOnlyList<SubServiceArea> Areas => SubServiceSettingsReader.RoiAreas;

	/// <inheritdoc />
	/// <remarks>The files are named for what they hold: <c>Runways_Lines</c>, <c>Airports_Symbols</c>, <c>Airports_Text</c>.</remarks>
	protected override (string? Lines, string? Symbols, string? Text) EmitKeys =>
		("EmitRunwayLines", "EmitAirportSymbols", "EmitAirportText");

	/// <inheritdoc />
	/// <remarks>One GeoJSON row - Runways Lines, Airports Symbols, Airports Text - then the alias file.</remarks>
	protected override IEnumerable<OutputFileOption> OutputFiles()
	{
		if (GenerateGeojson)
		{
			if (EmitLines)
			{
				yield return GeojsonFile(AirportOutputFiles.RunwaysLines, "Runways — Lines", "Runways", EramFieldKind.Line);
			}

			if (EmitSymbols)
			{
				yield return GeojsonFile(AirportOutputFiles.AirportsSymbols, "Airports — Symbols", "Airports", EramFieldKind.Symbol);
			}

			if (EmitText)
			{
				yield return GeojsonFile(AirportOutputFiles.AirportsText, "Airports — Text", "Airports", EramFieldKind.Text);
			}
		}

		if (GenerateAliasFile)
		{
			yield return OutputFileOption.AliasFile(AirportOutputFiles.Alias);
		}
	}

	private static OutputFileOption GeojsonFile(string key, string label, string crcClass, EramFieldKind kind) =>
		new(key, "GeoJSON", label, key, IsGeojson: true, [(crcClass, kind)]);

	// ================= parent hooks =================

	/// <inheritdoc />
	/// <remarks>Airports has no option lists built from the cycle, so this does nothing.</remarks>
	public void LoadCycleDependentLists(NasrCsvDataCollection data)
	{
	}

	/// <inheritdoc />
	public SubServiceRunResult? DescribeRunResult(AiracServiceResult result)
	{
		if (result.Airports is not { } airports)
		{
			return null;
		}

		string summary = $"{airports.AirportCount:N0} airports, {airports.AirportsInRoiCount:N0} in the ROI";

		if (airports.AliasFilePath is not null)
		{
			summary += $", Airports.txt: {airports.AliasCommandCount:N0} alias command(s)";
		}

		if (airports.GeojsonFilesWritten.Count > 0)
		{
			summary += $", {airports.GeojsonFilesWritten.Count:N0} GeoJSON file(s)";
		}

		return new SubServiceRunResult(Title, summary, airports.Messages);
	}

	/// <inheritdoc />
	public IReadOnlyDictionary<string, string> BuildSettingsBlock()
	{
		Dictionary<string, string> s = new(StringComparer.OrdinalIgnoreCase)
		{
			["GenerateGeojson"] = YesNo(GenerateGeojson),
		};

		AddSharedSettings(s);
		return s;
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		List<string> geojsonFiles = [];
		if (EmitLines) geojsonFiles.Add("Runway lines");
		if (EmitSymbols) geojsonFiles.Add("Symbols");
		if (EmitText) geojsonFiles.Add("Text");

		ServicePreviewRow[] rows =
		[
			new ServicePreviewRow("GeoJSON", GenerateGeojson ? string.Join(", ", geojsonFiles) : "No"),
			new ServicePreviewRow("FE-Buddy properties", DescribeFebProperties()),
			new ServicePreviewRow("Area", DescribeArea()),
			new ServicePreviewRow("CRC ERAM defaults", DescribeCrcDefaults()),
		];

		return [new ServicePreviewSection("Airports", rows) { WhatYoullGet = WhatYoullGet }];
	}

	/// <inheritdoc />
	/// <remarks>
	/// The ROI narrows the GeoJSON only: the alias file always has every operational airport, so its
	/// block only describes the file.
	/// </remarks>
	protected override IEnumerable<SummaryBlock> BuildWhatYoullGet()
	{
		List<SummaryFile> geojsonFiles = [];

		if (EmitLines)
		{
			geojsonFiles.Add(new($"{AirportOutputFiles.RunwaysLines}.geojson", "Each airport's runways, drawn as lines."));
		}

		if (EmitSymbols)
		{
			geojsonFiles.Add(new($"{AirportOutputFiles.AirportsSymbols}.geojson", "An airport symbol, drawn at each airport's reference point."));
		}

		if (EmitText)
		{
			geojsonFiles.Add(new($"{AirportOutputFiles.AirportsText}.geojson", "A label with the airport's FAA ID and name, drawn at each airport's reference point."));
		}

		yield return new SummaryBlock(SubServiceOutputKinds.Geojson, new SummaryLines()
			.Add(SummaryJoin.First, "Every operational airport")
			.Add(SummaryJoin.AndOnly, RoiLine(roi => $"airports inside {roi}"))
			.ToList())
		{
			Files = geojsonFiles,
		};

		yield return new SummaryBlock(SubServiceOutputKinds.Alias, [])
		{
			Files =
			[
				new(AirportOutputFiles.Alias,
					"An ISR command, `.apt` and the FAA or ICAO ID, for every operational airport in the NAS. It has details "
					+ "such as the airport's name, tower status, responsible ARTCC, longest runway, and more.\n"
					+ "Example: `.aptDTW` or `.aptKDTW`"),
			],
		};
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
		if (GenerateGeojson && !EmitSymbols && !EmitText && !EmitLines)
		{
			validation.AddArea(
				ServiceAreas.GeojsonFiles,
				"GeoJSON is on but none of its files are selected. Turn on Lines, Symbols, or Text, "
				+ "or turn GeoJSON off for Airports on the General tab.");
		}

		ValidateSharedSettings(validation);
	}
}
