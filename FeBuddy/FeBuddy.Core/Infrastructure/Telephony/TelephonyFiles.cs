using FeBuddy.Core.Infrastructure.SharedData;

namespace FeBuddy.Core.Infrastructure.Telephony;

/// <summary>
/// The two FAA telephony pages FE-Buddy reads - both from FAA Order JO 7340.2, Chapter 3 - and the
/// VATSIM-Radar Virtual Airline List a user can choose to add: where FE-Buddy keeps its copies of
/// them, and where they are downloaded from.
/// </summary>
public static class TelephonyFiles
{
	/// <summary>
	/// The kept copy's name of the VATSIM-Radar Virtual Airline List: the airlines VATSIM-Radar shows,
	/// its virtual airlines among them (see <see cref="VatsimRadarAirlinesUrl"/>).
	/// </summary>
	public const string VatsimRadarAirlinesFileName = "vatsim_radar_airlines.json";

	/// <summary>
	/// Where the VATSIM-Radar Virtual Airline List is downloaded from: VATSIM-Radar's own data service,
	/// whose <c>virtual</c> array is every virtual airline VATSIM-Radar shows. (Up to 3.0.0-beta.3,
	/// FE-Buddy downloaded <c>custom-data/airlines.json</c> from GitHub, which has only some of them.)
	/// </summary>
	public const string VatsimRadarAirlinesUrl = "https://data.vatsim-radar.com/airlines/all";

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

	/// <summary>The VATSIM-Radar list's kept copy: <c>%APPDATA%\FE-Buddy\Telephony\vatsim_radar_airlines.json</c>.</summary>
	public static string VatsimRadarAirlinesFilePath => Path.Combine(SharedDirectory, VatsimRadarAirlinesFileName);
}
