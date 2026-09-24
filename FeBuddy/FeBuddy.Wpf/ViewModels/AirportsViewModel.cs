using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels.Models;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Airports</b> sub-service tab inside the AIRAC Service screen: which outputs to write,
/// which GeoJSON files, which FE-Buddy properties, the CRC ERAM defaults, and the ROI override.
/// </summary>
/// <remarks>
/// Save, Undo and navigation come from the tab host's action bar; the run is launched by
/// <b>Run AIRAC Service</b> on the Preview Settings tab, and its results are shown on the Review
/// tab, described by this tab through <see cref="ISubServiceRunTarget"/>.
/// </remarks>
public sealed class AirportsViewModel : GeojsonSubServiceViewModel, ISubServiceRunTarget
{
	private const string Node = "Services.AiracService.Airports";

	private bool _generateGeojson = true;

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

	/// <summary>Whether this run writes GeoJSON for airports and runways.</summary>
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
	protected override bool WritesGeojson => GenerateGeojson;

	/// <inheritdoc />
	protected override string NoDefaultRoiHint =>
		"No default ROI is set, so GeoJSON covers every airport. Set one in Settings, or override it here.";

	/// <inheritdoc />
	/// <remarks>The files are named for what they hold: <c>Runways_Lines</c>, <c>Airports_Symbols</c>, <c>Airports_Text</c>.</remarks>
	protected override (string Lines, string Symbols, string Text) EmitKeys =>
		("EmitRunwayLines", "EmitAirportSymbols", "EmitAirportText");

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
	public IReadOnlyDictionary<string, string> BuildSettingsBlock(string outputDirectory, bool addFeBuddyOutputFolder)
	{
		Dictionary<string, string> s = new(StringComparer.OrdinalIgnoreCase)
		{
			["GenerateGeojson"] = YesNo(GenerateGeojson),
		};

		AddSharedSettings(s, outputDirectory, addFeBuddyOutputFolder);
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
			new ServicePreviewRow("GeoJSON covers", GenerateGeojson ? DescribeGeojsonScope() : "No GeoJSON"),
			new ServicePreviewRow("Alias file",
				GenerateAliasFile ? "Airports.txt, every open airport in NASR - the region never limits the alias file" : "No"),
			new ServicePreviewRow("FE-Buddy properties", DescribeFebProperties()),
			new ServicePreviewRow("CRC ERAM defaults", DescribeCrcDefaults("Runway lines")),
			new ServicePreviewRow("Region of interest", DescribeRoi()),
		];

		return [new ServicePreviewSection("Airports", rows)];
	}

	/// <summary>What the GeoJSON files cover once the region (override or default) is applied.</summary>
	/// <returns>e.g. "Open airports whose reference point is inside the region".</returns>
	private string DescribeGeojsonScope() =>
		HasRoi
			? "Open airports whose reference point is inside the region"
			: "Every open airport in NASR";

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

		OnPropertyChanged(nameof(GenerateGeojson));
		ClearDirty();
	}

	/// <inheritdoc />
	protected override void WriteToConfig()
	{
		Set("GenerateGeojson", YesNo(GenerateGeojson));
		SaveSharedSettings();
	}

	/// <inheritdoc />
	protected override void Validate(ServiceValidation validation)
	{
		if (GenerateGeojson && !EmitSymbols && !EmitText && !EmitLines)
		{
			validation.Add(
				"GeoJSON is on but none of its files are selected. Turn on Symbols, Text or Runway lines, "
				+ "or switch GeoJSON off.");
		}

		ValidateSharedSettings(validation);
	}
}
