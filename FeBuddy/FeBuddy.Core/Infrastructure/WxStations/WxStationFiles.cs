namespace FeBuddy.Core.Infrastructure.WxStations;

/// <summary>
/// The Wx Stations cache file's name and download source.
/// </summary>
public static class WxStationFiles
{
	/// <summary>
	/// The cached file name, placed alongside the NASR CSVs in an AIRAC cycle's folder:
	/// <c>%APPDATA%\FE-Buddy\AiracCycles\&lt;cycleId&gt;\stations.cache.xml</c>.
	/// </summary>
	public const string FileName = "stations.cache.xml";

	/// <summary>
	/// Where the gzip-compressed source is downloaded from: aviationweather.gov's live station
	/// cache, unlike every other AIRAC sub-service's data, which comes from a NASR 28-day
	/// subscription cycle.
	/// </summary>
	public const string DownloadUrl = "https://aviationweather.gov/data/cache/stations.cache.xml.gz";
}
