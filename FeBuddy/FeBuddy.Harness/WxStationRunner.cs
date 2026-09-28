using FeBuddy.Core.Application.Airac.WxStations;
using FeBuddy.Core.Application.Airac.WxStations.Models;
using FeBuddy.Core.Infrastructure.WxStations.Models;
using FeBuddy.Core.Infrastructure.WxStations.Parsers;

namespace FeBuddy.Harness;

/// <summary>
/// Exercises the Wx Stations service exactly the way the GUI "Run" button will: parse
/// <see cref="HarnessSettings.WxStationsSourceFile"/>, build the settings dictionary, call the one
/// public entry point, and hand the result back for reporting. Contains no Wx Stations logic of
/// its own.
/// </summary>
/// <remarks>
/// Unlike every other runner, this does not take <c>NasrCsvDataCollection</c>: Wx Stations' data
/// comes from its own source file, never from a NASR cycle.
/// </remarks>
internal static class WxStationRunner
{
	/// <summary>
	/// Parses <see cref="HarnessSettings.WxStationsSourceFile"/> and runs the Wx Stations
	/// sub-service against it using the toggles in <see cref="HarnessSettings.WxStationSettings"/>.
	/// </summary>
	/// <returns>What the service built and wrote, for <see cref="ConsoleReport"/> to print.</returns>
	public static WxStationServiceResult Run()
	{
		WxStationDataCollection wxStationData = WxStationXmlParser.Parse(HarnessSettings.WxStationsSourceFile);
		return WxStationService.Run(wxStationData, HarnessSettings.WxStationSettings());
	}
}
