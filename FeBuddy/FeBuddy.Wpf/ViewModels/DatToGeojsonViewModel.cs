using System.Globalization;

using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Conversions.DatToGeojson;
using FeBuddy.Core.Application.Conversions.DatToGeojson.Models;
using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Application.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>DAT to GeoJSON</b> tab on the File Conversions screen: converts FAA <c>.dat</c> RADAR
/// Video Maps into CRC-ready GeoJSON, one file per map, optionally cropped to a distance from
/// each map's point of tangency.
/// </summary>
/// <remarks>
/// The source, CRC ERAM defaults (a video map is lines only), save contract and run description
/// come from <see cref="FileConversionTabViewModel"/>; this tab adds the cropping distance.
/// </remarks>
public sealed class DatToGeojsonViewModel : FileConversionTabViewModel
{
	private const string Node = "Services.FileConversions.DatToGeojson";

	private string _croppingDistance = string.Empty;

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public DatToGeojsonViewModel()
		: base(DatToGeojsonSettingsParser.CrcClassName, writesSymbols: false, writesText: false)
	{
		LoadFromConfig();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "DAT to GeoJSON";

	/// <inheritdoc />
	public override string RunLabel => "Convert DAT files";

	/// <inheritdoc />
	public override string FileTypeLabel => ".dat";

	/// <inheritdoc />
	protected override IReadOnlyList<string> Extensions => DatToGeojsonService.Extensions;

	/// <inheritdoc />
	protected override string FileDescription => "FAA video maps";

	/// <summary>
	/// Keep only what lies within this many nautical miles of each map's point of tangency, as
	/// typed; blank keeps every line. Saved.
	/// </summary>
	public string CroppingDistance
	{
		get => _croppingDistance;
		set { if (SetProperty(ref _croppingDistance, value)) MarkDirty(); }
	}

	/// <inheritdoc />
	public override ServiceResult Execute(IReadOnlyDictionary<string, string> settings, Action<string, string, bool> reportStep) =>
		DatToGeojsonService.Run(settings, StepProgress(reportStep));

	/// <inheritdoc />
	protected override string? DescribeDetail(ConversionServiceResult result) =>
		$"{((DatToGeojsonServiceResult)result).Files.Sum(f => f.LinesWritten):N0} line(s)";

	/// <inheritdoc />
	protected override void LoadOwnSettings()
	{
		_croppingDistance = Get("CroppingDistance") ?? string.Empty;
		OnPropertyChanged(nameof(CroppingDistance));
	}

	/// <inheritdoc />
	protected override void SaveOwnSettings() => Set("CroppingDistance", CroppingDistance);

	/// <inheritdoc />
	protected override void AddOwnSettings(Dictionary<string, string> settings) =>
		settings["CroppingDistance"] = CroppingDistance.Trim();

	/// <inheritdoc />
	protected override void ValidateOwnSettings(ServiceValidation validation)
	{
		string distance = CroppingDistance.Trim();

		if (distance.Length > 0
			&& (!double.TryParse(distance, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double nm)
				|| nm <= 0
				|| nm > DatToGeojsonSettingsParser.MaxCroppingDistanceNm))
		{
			validation.AddField(nameof(CroppingDistance),
				$"Enter a distance in NM greater than 0 and at most {DatToGeojsonSettingsParser.MaxCroppingDistanceNm:0}, or leave it blank to keep every line.");
		}
	}
}
