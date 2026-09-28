using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.Telephony;

/// <summary>
/// Parses the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>)
/// supplies for the Telephony sub-service into a typed, validated <see cref="TelephonySettings"/>.
/// </summary>
/// <remarks>
/// Telephony's only output is its alias file, so there is almost nothing to read: where to write,
/// and whether the file goes to vNAS. The shared keys every sub-service tab sends (region of
/// interest, coordinate precision, FE-Buddy properties) are accepted and ignored - Telephony writes
/// no GeoJSON and is not limited to a region.
/// </remarks>
public static class TelephonySettingsParser
{
	private const string LogSource = "TelephonySettingsParser";

	/// <summary>The keys only Telephony reads, on top of <see cref="SubServiceSettingsReader.CommonKeys"/>.</summary>
	private static readonly IReadOnlySet<string> OwnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"GenerateAliasFile",
	};

	/// <summary>Telephony writes no GeoJSON, so it draws from no CRC-ERAM defaults class at all.</summary>
	private static readonly IReadOnlyDictionary<string, CrcFeatureKind[]> CrcKindsByClass =
		new Dictionary<string, CrcFeatureKind[]>(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Parses and validates <paramref name="telephonySettings"/> into a typed <see cref="TelephonySettings"/>.
	/// </summary>
	/// <param name="telephonySettings">The raw settings dictionary.</param>
	/// <returns>The typed settings plus any non-fatal parsing messages.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a required setting is missing or invalid, or the alias file - the only output - is
	/// turned off.
	/// </exception>
	public static TelephonySettingsParseResult Parse(IReadOnlyDictionary<string, string> telephonySettings)
	{
		ArgumentNullException.ThrowIfNull(telephonySettings);

		string outputDirectory = SettingsValueReader.RequiredString(telephonySettings, "OutputDirectory");

		if (!SettingsValueReader.YesNo(telephonySettings, "GenerateAliasFile", defaultValue: true))
		{
			throw new ArgumentException(
				"'GenerateAliasFile' is \"N\", but the alias file is the Telephony sub-service's only output, so it would produce " +
				"nothing. Turn it back on, or deselect Telephony.");
		}

		List<ServiceMessage> messages = [];

		if (SettingsValueReader.YesNo(telephonySettings, "IncludeFebCustomProperties", defaultValue: false))
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				"'IncludeFebCustomProperties' is \"Y\", but Telephony writes no GeoJSON, so it has no FE-Buddy properties to add; it was ignored."));
		}

		// Nothing Telephony writes is GeoJSON, so the alias file is the only key UploadToVnas may
		// name, and CrcDefaultsFor none at all.
		VnasFileChoices vnas = SubServiceSettingsReader.ReadVnasFiles(
			telephonySettings, TelephonyOutputFiles.Alias, isGeojsonFileKey: _ => false, example: TelephonyOutputFiles.Alias);

		messages.AddRange(SubServiceSettingsReader.UnknownKeyWarnings(
			telephonySettings, OwnKeys, CrcKindsByClass, LogSource,
			labelSource: "Telephony writes no GeoJSON, so it has no per-feature CRC output"));

		TelephonySettings settings = new()
		{
			OutputDirectory = outputDirectory,
			Vnas = vnas,
		};

		return new TelephonySettingsParseResult(settings, messages);
	}
}
