using FeBuddy.Core.Infrastructure.SharedData;

namespace FeBuddy.Core.Infrastructure.WxStations;

/// <summary>
/// The Wx Stations data file's name, where FE-Buddy keeps it, and where it is downloaded from.
/// </summary>
public static class WxStationFiles
{
	/// <summary>The kept file's name.</summary>
	public const string FileName = "stations.cache.xml";

	/// <summary>
	/// The folder the one kept copy lives in: <c>%APPDATA%\FE-Buddy\WxStations</c>. The station
	/// list is not published per AIRAC cycle, so it is kept outside every cycle folder and refreshed
	/// on every run whichever cycle is run (see <see cref="WxStationDownloader"/>).
	/// </summary>
	public static string SharedDirectory => SharedDataDownload.SharedDataDirectory("WxStations");

	/// <summary>The kept copy's full path: <c>%APPDATA%\FE-Buddy\WxStations\stations.cache.xml</c>.</summary>
	public static string SharedFilePath => Path.Combine(SharedDirectory, FileName);

	/// <summary>
	/// Where the gzip-compressed source is downloaded from: aviationweather.gov's live station
	/// cache, not a 28-day subscription cycle like the NASR data.
	/// </summary>
	public const string DownloadUrl = "https://aviationweather.gov/data/cache/stations.cache.xml.gz";
}
