using FeBuddy.Core.Infrastructure.Dtpp;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.WxStations.Models;

namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>
/// The data <c>AiracService.RunAsync</c> needs beyond the selected cycle's parsed NASR CSVs: Wx
/// Stations' own live weather station list, and the FAA d-TPP Metafile for the selected cycle and
/// the cycle before it.
/// </summary>
public sealed record AiracSupplementalData
{
	/// <summary>
	/// The selected cycle's parsed Wx station data, or <see langword="null"/> when it has not
	/// downloaded yet. Only read when <see cref="AiracServiceSettings.WxStations"/> is not
	/// <see langword="null"/>.
	/// </summary>
	public WxStationDataCollection? WxStations { get; init; }

	/// <summary>
	/// The selected cycle's parsed FAA d-TPP Metafile, or <see langword="null"/> when it has not
	/// downloaded yet (or the FAA has not published it yet - see <see cref="DtppFiles.DownloadUrl"/>).
	/// Only read when <see cref="AiracServiceSettings.Procedures"/> is not <see langword="null"/>.
	/// </summary>
	public DtppMetafileDataCollection? Dtpp { get; init; }

	/// <summary>
	/// The FAA d-TPP Metafile of the cycle immediately before the selected one, or
	/// <see langword="null"/> when it has not downloaded yet - used to link a procedure the
	/// selected cycle deletes back to the chart it last had, since a deleted
	/// <see cref="DtppMetafileXmlDataModel.Record"/>'s own <c>PdfName</c> is only a placeholder
	/// (<see cref="DtppFiles.DeletedChartPdfName"/> or <see cref="DtppFiles.DeletedFromAirportPdfName"/>).
	/// Only read when <see cref="AiracServiceSettings.Procedures"/> is not <see langword="null"/>.
	/// </summary>
	public DtppMetafileDataCollection? PreviousDtpp { get; init; }
}
