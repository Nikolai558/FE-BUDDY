using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Application.Conversions.SctToGeojson;
using FeBuddy.Core.Application.Conversions.SctToGeojson.Models;
using FeBuddy.Core.Application.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>SCT2 to GeoJSON</b> tab on the File Conversions screen: converts VRC sector files into
/// CRC-ready GeoJSON - a folder per sector file holding its boundaries, airways, GEO, labels,
/// regions and a file per SID and STAR diagram.
/// </summary>
/// <remarks>
/// Everything this tab offers comes from <see cref="FileConversionTabViewModel"/>: the source, and
/// CRC ERAM defaults for lines and for the labels' text.
/// </remarks>
public sealed class SctToGeojsonViewModel : FileConversionTabViewModel
{
	private const string Node = "Services.FileConversions.SctToGeojson";

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public SctToGeojsonViewModel()
		: base(SctToGeojsonSettingsParser.CrcClassName, writesText: true)
	{
		LoadFromConfig();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "SCT2 to GeoJSON";

	/// <inheritdoc />
	public override string RunLabel => "Convert sector files";

	/// <inheritdoc />
	public override string FileTypeLabel => ".sct2 / .sct";

	/// <inheritdoc />
	protected override IReadOnlyList<string> Extensions => SctToGeojsonService.Extensions;

	/// <inheritdoc />
	protected override string FileDescription => "Sector files";

	/// <inheritdoc />
	public override ServiceResult Execute(IReadOnlyDictionary<string, string> settings, Action<string, string, bool> reportStep) =>
		SctToGeojsonService.Run(settings, StepProgress(reportStep));

	/// <inheritdoc />
	protected override string? DescribeDetail(ConversionServiceResult result) =>
		$"{((SctToGeojsonServiceResult)result).Files.Sum(f => f.FeaturesWritten):N0} feature(s)";
}
