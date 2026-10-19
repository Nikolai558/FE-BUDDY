namespace FeBuddy.Core.Infrastructure.WxStations.Models;

/// <summary>Every <see cref="WxStationXmlDataModel.Station"/> parsed from <c>stations.cache.xml</c>.</summary>
public sealed class WxStationDataCollection
{
	/// <summary>Every station the file lists, in file order.</summary>
	public List<WxStationXmlDataModel.Station> Stations { get; set; } = [];
}
