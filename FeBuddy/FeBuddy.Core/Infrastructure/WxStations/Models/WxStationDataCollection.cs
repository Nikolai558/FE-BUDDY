namespace FeBuddy.Core.Infrastructure.WxStations.Models;

/// <summary>
/// Every <see cref="WxStationXmlDataModel.Station"/> parsed from <c>stations.cache.xml</c>, plus
/// the file's own reported result count.
/// </summary>
public sealed class WxStationDataCollection
{
	/// <summary>Every station the file lists, in file order.</summary>
	public List<WxStationXmlDataModel.Station> Stations { get; set; } = [];

	/// <summary>
	/// The file's own <c>response/data/@num_results</c> value - how many stations the source
	/// reports. Normally equal to <see cref="Stations"/>'s count; kept separately in case the two
	/// ever disagree.
	/// </summary>
	public int NumResults { get; set; }
}
