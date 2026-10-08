using FeBuddy.Core.Infrastructure.SharedData;

namespace FeBuddy.Core.Infrastructure.Telephony;

/// <summary>
/// The two FAA telephony pages FE-Buddy reads - both from FAA Order JO 7340.2, Chapter 3 - and the
/// two parts of the virtual airline list a user can choose to add: where FE-Buddy keeps its copies of
/// them, and where they are downloaded from.
/// </summary>
/// <remarks>
/// The virtual airline list is the one VATSIM-Radar shows, built the way VATSIM-Radar builds it
/// (<c>server/routes/airlines/all.ts</c> in VATSIM-Radar/data) from the same two sources: GNG's
/// fictional airlines (<see cref="GngAirlinesUrl"/>) and VATSIM-Radar's own additions
/// (<see cref="VatsimRadarAirlinesUrl"/>). Each is kept in a copy of its own and merged when read,
/// so one part that can't be downloaded never cuts the other short. (3.0.0-beta.4 downloaded the
/// merged list from <c>data.vatsim-radar.com</c>, which only takes TLS 1.3 - which Windows 10 can't do.)
/// </remarks>
public static class TelephonyFiles
{
	/// <summary>
	/// Where GNG's fictional airlines are downloaded from: the JSON behind the table on GNG's page, every
	/// row on one page - <c>{ "records", "rows": [{ "icao", "airline", "callsign", ... }] }</c>.
	/// </summary>
	public const string GngAirlinesUrl =
		"https://gng.aero-nav.com/AERONAV/icao_fhairlines?action=get&oper=grid&_search=false&rows=10000&page=1&sidx=icao&sord=asc";

	/// <summary>
	/// Where VATSIM-Radar's own airline list is downloaded from, on GitHub: one JSON array of
	/// <c>{ "icao", "name", "callsign", "virtual" }</c>, whose virtual airlines are added to GNG's.
	/// </summary>
	public const string VatsimRadarAirlinesUrl = "https://raw.githubusercontent.com/VATSIM-Radar/data/main/custom-data/airlines.json";

	/// <summary>What the log and the run call GNG's part of the list.</summary>
	public const string GngAirlinesDescription = "GNG fictional airline list";

	/// <summary>What the log and the run call VATSIM-Radar's part of the list.</summary>
	public const string VatsimRadarAirlinesDescription = "VATSIM-Radar airline list";

	/// <summary>The kept copy's name of GNG's fictional airlines (<see cref="GngAirlinesUrl"/>).</summary>
	public const string GngAirlinesFileName = "gng_fhairlines.json";

	/// <summary>The kept copy's name of VATSIM-Radar's own airline list (<see cref="VatsimRadarAirlinesUrl"/>).</summary>
	public const string VatsimRadarAirlinesFileName = "vatsim_radar_custom_airlines.json";

	/// <summary>
	/// The name of the one copy of the list FE-Buddy 3.0.0-beta.4 and earlier kept: beta.4's merged list
	/// from <c>data.vatsim-radar.com</c>, or, from beta.3 and earlier, VATSIM-Radar's GitHub list. Read
	/// only while there is no copy of either part, and deleted once there is a copy of both.
	/// </summary>
	public const string OlderVatsimRadarAirlinesFileName = "vatsim_radar_airlines.json";

	/// <summary>
	/// The kept copy's name of Chapter 3, Section 1, "Aircraft Company/Telephony/Three-Letter
	/// Designator Encode" - the whole ICAO register: every company, its country, its telephony and
	/// its three-letter designator.
	/// </summary>
	public const string RegisterFileName = "telephony_register.html";

	/// <summary>
	/// The kept copy's name of Chapter 3, Section 4, "U.S. Special Telephony/Call Signs": call signs
	/// the FAA assigns in the U.S. outside the ICAO register, each with its own identifier.
	/// </summary>
	public const string SpecialCallSignsFileName = "us_special_call_signs.html";

	/// <summary>Where Section 1 (the register) is downloaded from.</summary>
	public const string RegisterUrl = "https://www.faa.gov/air_traffic/publications/atpubs/cnt_html/chap3_section_1.html";

	/// <summary>Where Section 4 (the U.S. special call signs) is downloaded from.</summary>
	public const string SpecialCallSignsUrl = "https://www.faa.gov/air_traffic/publications/atpubs/cnt_html/chap3_section_4.html";

	/// <summary>
	/// The folder the kept copies live in: <c>%APPDATA%\FE-Buddy\Telephony</c>. Telephony is not
	/// published per AIRAC cycle, so it is kept outside every cycle folder and refreshed on every run
	/// whichever cycle is run (see <see cref="TelephonyDownloader"/>).
	/// </summary>
	public static string SharedDirectory => SharedDataDownload.SharedDataDirectory("Telephony");

	/// <summary>The register's kept copy: <c>%APPDATA%\FE-Buddy\Telephony\telephony_register.html</c>.</summary>
	public static string RegisterFilePath => Path.Combine(SharedDirectory, RegisterFileName);

	/// <summary>The U.S. special call signs' kept copy: <c>%APPDATA%\FE-Buddy\Telephony\us_special_call_signs.html</c>.</summary>
	public static string SpecialCallSignsFilePath => Path.Combine(SharedDirectory, SpecialCallSignsFileName);

	/// <summary>GNG's list's kept copy: <c>%APPDATA%\FE-Buddy\Telephony\gng_fhairlines.json</c>.</summary>
	public static string GngAirlinesFilePath => Path.Combine(SharedDirectory, GngAirlinesFileName);

	/// <summary>VATSIM-Radar's list's kept copy: <c>%APPDATA%\FE-Buddy\Telephony\vatsim_radar_custom_airlines.json</c>.</summary>
	public static string VatsimRadarAirlinesFilePath => Path.Combine(SharedDirectory, VatsimRadarAirlinesFileName);

	/// <summary>The older single copy (<see cref="OlderVatsimRadarAirlinesFileName"/>): <c>%APPDATA%\FE-Buddy\Telephony\vatsim_radar_airlines.json</c>.</summary>
	public static string OlderVatsimRadarAirlinesFilePath => Path.Combine(SharedDirectory, OlderVatsimRadarAirlinesFileName);
}
