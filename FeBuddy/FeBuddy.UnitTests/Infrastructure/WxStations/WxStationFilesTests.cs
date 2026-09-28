using FeBuddy.Core.Infrastructure.WxStations;

namespace FeBuddy.UnitTests.Infrastructure.WxStations;

/// <summary>
/// Covers <see cref="WxStationFiles"/>: the one kept copy of the station list lives in its own
/// folder under <c>%APPDATA%\FE-Buddy</c>, outside every AIRAC cycle folder, and is downloaded from
/// aviationweather.gov.
/// </summary>
public sealed class WxStationFilesTests
{
	[Fact]
	public void the_kept_copy_lives_in_its_own_folder_outside_the_cycle_folders()
	{
		Assert.EndsWith(Path.Combine("FE-Buddy", "WxStations"), WxStationFiles.SharedDirectory, StringComparison.OrdinalIgnoreCase);
		Assert.Equal(Path.Combine(WxStationFiles.SharedDirectory, "stations.cache.xml"), WxStationFiles.SharedFilePath);
		Assert.DoesNotContain("AiracCycles", WxStationFiles.SharedFilePath, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void the_source_is_the_aviationweather_gov_station_cache() =>
		Assert.Equal("https://aviationweather.gov/data/cache/stations.cache.xml.gz", WxStationFiles.DownloadUrl);
}
