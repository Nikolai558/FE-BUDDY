using FeBuddy.Core.Application.Airac.ConcatenateAliases.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Dtpp;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Telephony.Models;
using FeBuddy.Core.Infrastructure.WxStations.Models;

namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>
/// The data <c>AiracService.RunAsync</c> needs beyond the selected cycle's parsed NASR CSVs: the
/// Wx Stations list and the FAA telephony pages (not published per cycle - every run downloads the
/// latest), and the FAA d-TPP Metafile for the selected cycle and the cycle before it.
/// </summary>
public sealed record AiracSupplementalData
{
	/// <summary>
	/// The parsed Wx station data, or <see langword="null"/> when FE-Buddy has no usable copy. Only
	/// read when <see cref="AiracServiceSettings.WxStations"/> is not <see langword="null"/>.
	/// </summary>
	public WxStationDataCollection? WxStations { get; init; }

	/// <summary>
	/// The parsed FAA telephony pages, or <see langword="null"/> when FE-Buddy has no usable copy of
	/// the register. Only read when <see cref="AiracServiceSettings.Telephony"/> is not
	/// <see langword="null"/>.
	/// </summary>
	public TelephonyDataCollection? Telephony { get; init; }

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

	/// <summary>
	/// The user's custom alias files as they were read (or why each could not be), in merge order.
	/// Only read when <see cref="AiracServiceSettings.ConcatenateAliases"/> is not <see langword="null"/>;
	/// <see langword="null"/> merges none.
	/// </summary>
	public IReadOnlyList<AliasSourceLoad>? CustomAliasFiles { get; init; }

	/// <summary>
	/// What getting this data produced for the run's Review tab - whether each download was fresh,
	/// fell back on an older copy (and how old), or found none. Added to the run's messages as they
	/// are.
	/// </summary>
	public IReadOnlyList<ServiceMessage> Messages { get; init; } = [];
}
